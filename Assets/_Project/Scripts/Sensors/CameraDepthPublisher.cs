using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;

/// <summary>
/// Publishes depth images from the front camera with actual metric distances.
/// HDRP compatible - uses CustomPass for depth capture, creates all resources at runtime.
/// </summary>
public class CameraDepthPublisher : ROSPublisher
{
    public override string Topic => ROSSettings.Instance != null ? ROSSettings.Instance.DepthCameraTopic : null;

    [Header("Camera Reference")]
    [Tooltip("The front/left camera to extract depth from")]
    public Camera sourceCamera;
    
    [Header("Depth Settings")]
    [Tooltip("Maximum depth distance in meters for visualization")]
    [Range(1f, 100f)]
    public float maxDepth = 20f;
    
    [Tooltip("Minimum depth distance in meters for visualization")]
    [Range(0.1f, 5f)]
    public float minDepth = 0.3f;
    
    [Tooltip("Encoding format for ROS image: '32FC1' for float meters, '16UC1' for uint16 millimeters")]
    public string encoding = "32FC1";

    // Runtime-created resources
    private int publishWidth = 672;
    private int publishHeight = 376;
    private RenderTexture depthRT;           // Linear depth in meters (for ROS)
    private RenderTexture visualizationRT;   // Heatmap for UI display
    private Material linearDepthMaterial;
    private Material heatmapMaterial;
    private Texture2D depthTexture2D;
    
    // CustomPass references
    private CustomPassVolume customPassVolume;
    private DepthCapturePass depthCapturePass;
    private DepthCapturePass heatmapCapturePass;
    
    // ROS messages
    private ImageMsg depthMsg;
    private CameraInfoMsg cameraInfoMsg;
    private string cameraInfoTopic;
    
    // Double buffer for zero-allocation publishing
    private byte[][] depthBuffers = new byte[2][];
    private int depthBufIdx = 0;
    private ImageMsg[] depthMsgPool;
    private int depthMsgPoolIdx = 0;
    private CameraInfoMsg[] infoMsgPool;
    private int infoPoolIdx = 0;

    // Cached AsyncGPUReadback delegate & timestamp
    private System.Action<AsyncGPUReadbackRequest> readbackCallback;
    private RosMessageTypes.BuiltinInterfaces.TimeMsg pendingStamp;
    
    // For UI display
    public RenderTexture VisualizationTexture => visualizationRT;
    
    private float lastCaptureTime;

    protected override void Start()
    {
        readbackCallback = OnReadbackComplete;
        pendingStamp = new RosMessageTypes.BuiltinInterfaces.TimeMsg();

        // Initialize resources FIRST (before base.Start which calls RegisterPublisher)
        Initialize();
        
        // Now call base.Start which will call RegisterPublisher
        base.Start();
        useBaseRateLimiting = false;
    }

    private void Initialize()
    {
        Debug.Log("[CameraDepthPublisher] Starting initialization...");
        
        // Get resolution from settings
        if (SimulationSettings.Instance != null)
        {
            PublishRate = SimulationSettings.Instance.FrontCamRate;
            publishWidth = SimulationSettings.Instance.FrontCamWidth;
            publishHeight = SimulationSettings.Instance.FrontCamHeight;
        }
        
        Debug.Log($"[CameraDepthPublisher] Resolution: {publishWidth}x{publishHeight}, Rate: {PublishRate}Hz");

        // Create RenderTextures at runtime
        CreateRenderTextures();
        
        // Create materials at runtime
        CreateMaterials();
        
        // Setup CustomPass (runs even if materials failed - we check inside)
        SetupCustomPass();
        
        // Create readback texture for ROS
        depthTexture2D = new Texture2D(publishWidth, publishHeight, TextureFormat.RFloat, false);
        
        // Initialize ROS messages
        InitializeMessages();
        
        Debug.Log("[CameraDepthPublisher] Initialization complete.");
    }

    private void CreateRenderTextures()
    {
        // Linear depth RT (RFloat for metric depth values) - no AA, point filtering for precision
        depthRT = new RenderTexture(publishWidth, publishHeight, 0, RenderTextureFormat.RFloat);
        depthRT.name = "DepthRT_Linear";
        depthRT.filterMode = FilterMode.Point; // Exact values, no interpolation
        depthRT.Create();
        Debug.Log($"[CameraDepthPublisher] Created depthRT: {depthRT.name}");
        
        // Visualization RT (ARGB32 for heatmap display) - with AA for smooth edges
        visualizationRT = new RenderTexture(publishWidth, publishHeight, 0, RenderTextureFormat.ARGB32);
        visualizationRT.name = "DepthRT_Heatmap";
        visualizationRT.antiAliasing = 4;
        visualizationRT.filterMode = FilterMode.Bilinear;
        visualizationRT.Create();
        Debug.Log($"[CameraDepthPublisher] Created visualizationRT: {visualizationRT.name}");
    }

    private void CreateMaterials()
    {
        // Find shaders
        Shader linearDepthShader = Shader.Find("Hidden/LinearDepthResampler");
        Shader heatmapShader = Shader.Find("Hidden/DepthHeatmap");
        
        if (linearDepthShader == null)
        {
            Debug.LogError("[CameraDepthPublisher] Could not find Hidden/LinearDepthResampler shader! Make sure the shader is in the project and not stripped.");
        }
        else
        {
            linearDepthMaterial = new Material(linearDepthShader);
            linearDepthMaterial.name = "LinearDepthMaterial_Runtime";
            Debug.Log("[CameraDepthPublisher] Created linearDepthMaterial");
        }
        
        if (heatmapShader == null)
        {
            Debug.LogError("[CameraDepthPublisher] Could not find Hidden/DepthHeatmap shader! Make sure the shader is in the project and not stripped.");
        }
        else
        {
            heatmapMaterial = new Material(heatmapShader);
            heatmapMaterial.name = "DepthHeatmapMaterial_Runtime";
            heatmapMaterial.SetFloat("_MinDist", minDepth);
            heatmapMaterial.SetFloat("_MaxDist", maxDepth);
            Debug.Log("[CameraDepthPublisher] Created heatmapMaterial");
        }
    }

    private void SetupCustomPass()
    {
        if (sourceCamera == null)
        {
            Debug.LogError("[CameraDepthPublisher] sourceCamera is not assigned!");
            return;
        }
        
        Debug.Log($"[CameraDepthPublisher] Setting up CustomPass on camera: {sourceCamera.gameObject.name}");
        
        // Find or create CustomPassVolume on the camera
        customPassVolume = sourceCamera.GetComponent<CustomPassVolume>();
        if (customPassVolume == null)
        {
            Debug.Log("[CameraDepthPublisher] CustomPassVolume not found, creating new one...");
            customPassVolume = sourceCamera.gameObject.AddComponent<CustomPassVolume>();
            Debug.Log("[CameraDepthPublisher] Created CustomPassVolume");
        }
        else
        {
            Debug.Log("[CameraDepthPublisher] Found existing CustomPassVolume");
        }
        
        // Configure CustomPassVolume for Camera mode
        customPassVolume.isGlobal = false;
        customPassVolume.targetCamera = sourceCamera;  // Assign target camera
        customPassVolume.injectionPoint = CustomPassInjectionPoint.AfterPostProcess;
        
        // Add DepthCapturePass for linear depth (ROS) - only if material exists
        if (linearDepthMaterial != null && depthRT != null)
        {
            depthCapturePass = customPassVolume.AddPassOfType<DepthCapturePass>() as DepthCapturePass;
            if (depthCapturePass != null)
            {
                depthCapturePass.name = "DepthCapture_Linear";
                depthCapturePass.linearDepthMaterial = linearDepthMaterial;
                depthCapturePass.outputRenderTexture = depthRT;
                depthCapturePass.targetColorBuffer = CustomPass.TargetBuffer.Camera;
                Debug.Log("[CameraDepthPublisher] Added DepthCapturePass for linear depth");
            }
            else
            {
                Debug.LogError("[CameraDepthPublisher] Failed to create DepthCapturePass for linear depth!");
            }
        }
        
        // Add DepthCapturePass for heatmap (UI visualization) - only if material exists
        if (heatmapMaterial != null && visualizationRT != null)
        {
            heatmapCapturePass = customPassVolume.AddPassOfType<DepthCapturePass>() as DepthCapturePass;
            if (heatmapCapturePass != null)
            {
                heatmapCapturePass.name = "DepthCapture_Heatmap";
                heatmapCapturePass.linearDepthMaterial = heatmapMaterial;
                heatmapCapturePass.outputRenderTexture = visualizationRT;
                heatmapCapturePass.targetColorBuffer = CustomPass.TargetBuffer.Camera;
                Debug.Log("[CameraDepthPublisher] Added DepthCapturePass for heatmap");
            }
            else
            {
                Debug.LogError("[CameraDepthPublisher] Failed to create DepthCapturePass for heatmap!");
            }
        }
    }

    private void InitializeMessages()
    {
        depthMsg = new ImageMsg();
        depthMsg.header = new HeaderMsg { frame_id = ROSSettings.Instance.DepthCamFrameId };
        depthMsg.width = (uint)publishWidth;
        depthMsg.height = (uint)publishHeight;
        depthMsg.encoding = encoding;
        depthMsg.is_bigendian = 0;
        
        if (encoding == "32FC1")
        {
            depthMsg.step = (uint)(publishWidth * 4);
        }
        else if (encoding == "16UC1")
        {
            depthMsg.step = (uint)(publishWidth * 2);
        }

        cameraInfoMsg = new CameraInfoMsg();
        cameraInfoMsg.header = new HeaderMsg { frame_id = ROSSettings.Instance.DepthCamFrameId };
        cameraInfoMsg.width = (uint)publishWidth;
        cameraInfoMsg.height = (uint)publishHeight;
        cameraInfoMsg.distortion_model = "plumb_bob";
        cameraInfoMsg.D = new double[] { 0, 0, 0, 0, 0 };

        if (sourceCamera != null)
        {
            double f = (publishHeight / 2.0) / Mathf.Tan(sourceCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            double cx = publishWidth / 2.0;
            double cy = publishHeight / 2.0;

            cameraInfoMsg.K = new double[] { f, 0, cx, 0, f, cy, 0, 0, 1 };
            cameraInfoMsg.P = new double[] { f, 0, cx, 0, 0, f, cy, 0, 0, 0, 1, 0 };
            cameraInfoMsg.R = new double[] { 1, 0, 0, 0, 1, 0, 0, 0, 1 };
        }

        int lastSlashIndex = Topic.LastIndexOf('/');
        if (lastSlashIndex >= 0)
        {
            cameraInfoTopic = Topic.Substring(0, lastSlashIndex + 1) + "camera_info";
        }
        else
        {
            cameraInfoTopic = "camera_info";
        }

        depthMsgPool = new ImageMsg[2];
        for (int i = 0; i < 2; i++)
        {
            depthMsgPool[i] = new ImageMsg
            {
                header = new HeaderMsg { frame_id = ROSSettings.Instance.DepthCamFrameId, stamp = new RosMessageTypes.BuiltinInterfaces.TimeMsg() },
                height = (uint)publishHeight,
                width = (uint)publishWidth,
                encoding = encoding,
                is_bigendian = 0,
                step = (uint)(publishWidth * (encoding == "32FC1" ? 4 : 2))
            };
        }

        infoMsgPool = new CameraInfoMsg[2];
        for (int i = 0; i < 2; i++)
        {
            infoMsgPool[i] = new CameraInfoMsg
            {
                header = new HeaderMsg { frame_id = ROSSettings.Instance.DepthCamFrameId, stamp = new RosMessageTypes.BuiltinInterfaces.TimeMsg() },
                height = (uint)publishHeight,
                width = (uint)publishWidth,
                distortion_model = cameraInfoMsg.distortion_model,
                D = cameraInfoMsg.D,
                K = cameraInfoMsg.K,
                P = cameraInfoMsg.P,
                R = cameraInfoMsg.R
            };
        }
    }

    protected override void RegisterPublisher()
    {
        if (string.IsNullOrEmpty(Topic))
        {
            Debug.LogError("[CameraDepthPublisher] Topic is null or empty! Check ROSSettings.Instance.");
            return;
        }
        
        if (string.IsNullOrEmpty(cameraInfoTopic))
        {
            int lastSlashIndex = Topic.LastIndexOf('/');
            if (lastSlashIndex >= 0)
            {
                cameraInfoTopic = Topic.Substring(0, lastSlashIndex + 1) + "camera_info";
            }
            else
            {
                cameraInfoTopic = "camera_info";
            }
        }
        
        ros.RegisterPublisher<ImageMsg>(Topic);
        ros.RegisterPublisher<CameraInfoMsg>(cameraInfoTopic);
    }

    protected override void FixedUpdate()
    {
        if (sourceCamera == null || depthRT == null) return;

        // Check if we should publish
        if (SimulationSettings.Instance == null || !SimulationSettings.Instance.PublishROS || !SimulationSettings.Instance.PublishDepthCamera)
        {
            return;
        }

        // Rate limiting
        float interval = 1f / PublishRate;
        if (Time.time - lastCaptureTime >= interval)
        {
            // Update heatmap material parameters
            if (heatmapMaterial != null)
            {
                heatmapMaterial.SetFloat("_MinDist", minDepth);
                heatmapMaterial.SetFloat("_MaxDist", maxDepth);
            }
            
            // CameraRenderManager handles rendering - CustomPasses run when camera renders
            // We just need to read back the data for ROS publishing
            ReadbackAndPublish();
            
            lastCaptureTime = Time.time;
        }
    }

    private bool isReading = false;  // Prevent overlapping async reads
    
    private void ReadbackAndPublish()
    {
        if (depthRT == null || depthTexture2D == null || isReading) return;
        
        isReading = true;
        ROSClock.PopulateROSTimestamp(pendingStamp);
        
        // Use async GPU readback to avoid stalling main thread
        AsyncGPUReadback.Request(depthRT, 0, TextureFormat.RFloat, readbackCallback);
    }
    
    private void OnReadbackComplete(AsyncGPUReadbackRequest request)
    {
        isReading = false;
        
        if (request.hasError)
        {
            Debug.LogWarning("[CameraDepthPublisher] AsyncGPUReadback failed");
            return;
        }
        
        // Get raw data as NativeArray<byte> (no allocation)
        var data = request.GetData<byte>();
        if (data.Length == 0) return;
        
        // Double-buffer: allocate buffers ONLY if null or size changed (typically once at startup)
        if (depthBuffers[0] == null || depthBuffers[0].Length != data.Length)
        {
            depthBuffers[0] = new byte[data.Length];
            depthBuffers[1] = new byte[data.Length];
        }
        
        // Toggle buffer slot to prevent racing with background ROS TCP sender
        depthBufIdx = 1 - depthBufIdx;
        var targetBuf = depthBuffers[depthBufIdx];

        // Fast copy from NativeArray to managed array (no allocation)
        // Note: Vertical flip is handled in LinearDepthResampler.shader for zero CPU cost
        data.CopyTo(targetBuf);
        
        // Update Texture2D only if needed for visualization/debugging
        if (depthTexture2D != null)
        {
            depthTexture2D.LoadRawTextureData(targetBuf);
            depthTexture2D.Apply();
        }
        
        // Publish with cached data
        PublishOptimized(targetBuf);
    }
    
    /// <summary>
    /// Optimized publish path that avoids GetRawTextureData() allocation.
    /// </summary>
    private void PublishOptimized(byte[] publishData)
    {
        if (publishData == null) return;
        
        // Handle 16UC1 conversion (fallback to original method for 16UC1)
        if (encoding == "16UC1")
        {
            PublishMessage();
            return;
        }

        var pubDepthMsg = depthMsgPool[depthMsgPoolIdx];
        depthMsgPoolIdx = 1 - depthMsgPoolIdx;

        pubDepthMsg.header.stamp.sec = pendingStamp.sec;
        pubDepthMsg.header.stamp.nanosec = pendingStamp.nanosec;
        pubDepthMsg.data = publishData;

        ros.Publish(Topic, pubDepthMsg);
        
        if (infoMsgPool != null)
        {
            var pubInfoMsg = infoMsgPool[infoPoolIdx];
            infoPoolIdx = 1 - infoPoolIdx;

            pubInfoMsg.header.stamp.sec = pendingStamp.sec;
            pubInfoMsg.header.stamp.nanosec = pendingStamp.nanosec;

            ros.Publish(cameraInfoTopic, pubInfoMsg);
        }
    }

    public override void PublishMessage()
    {
        if (depthTexture2D == null) return;

        byte[] depthData = depthTexture2D.GetRawTextureData();
        
        // Convert to millimeters if using 16UC1 encoding
        if (encoding == "16UC1")
        {
            float[] floatData = new float[publishWidth * publishHeight];
            System.Buffer.BlockCopy(depthData, 0, floatData, 0, depthData.Length);
            
            ushort[] millimeterData = new ushort[floatData.Length];
            for (int i = 0; i < floatData.Length; i++)
            {
                millimeterData[i] = (ushort)Mathf.Clamp(floatData[i] * 1000f, 0, 65535);
            }
            
            depthData = new byte[millimeterData.Length * 2];
            System.Buffer.BlockCopy(millimeterData, 0, depthData, 0, depthData.Length);
        }

        depthMsg.data = depthData;
        depthMsg.header.stamp = ROSClock.GetROSTimestamp();
        ros.Publish(Topic, depthMsg);

        cameraInfoMsg.header.stamp = depthMsg.header.stamp;
        ros.Publish(cameraInfoTopic, cameraInfoMsg);
    }

    private void OnDestroy()
    {
        // Cleanup CustomPass
        if (customPassVolume != null)
        {
            if (depthCapturePass != null) customPassVolume.customPasses.Remove(depthCapturePass);
            if (heatmapCapturePass != null) customPassVolume.customPasses.Remove(heatmapCapturePass);
        }
        
        // Cleanup RenderTextures
        if (depthRT != null)
        {
            depthRT.Release();
            Destroy(depthRT);
        }
        if (visualizationRT != null)
        {
            visualizationRT.Release();
            Destroy(visualizationRT);
        }
        
        // Cleanup Materials
        if (linearDepthMaterial != null) Destroy(linearDepthMaterial);
        if (heatmapMaterial != null) Destroy(heatmapMaterial);
        
        // Cleanup Textures
        if (depthTexture2D != null) Destroy(depthTexture2D);
    }
}
