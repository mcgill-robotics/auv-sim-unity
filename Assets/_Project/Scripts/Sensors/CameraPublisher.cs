using UnityEngine;
using UnityEngine.Rendering;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;

public class CameraPublisher : ROSPublisher
{
    public enum CameraType { Front, Down }
    
    [Header("Camera Configuration")]
    [Tooltip("Camera type determines which settings and topic to use")]
    public CameraType cameraType;
    
    [Tooltip("Unity Camera component to capture from. Must have a target RenderTexture")]
    public Camera cam;

    [Header("Underwater Optics & Refraction")]
    [Tooltip("Simulate flat-port water refraction (Snell's Law)")]
    public bool simulateRefraction = true;

    [Tooltip("Refraction scale / index for Snell's law (typically ~1.333 for water, 1.0 = disabled)")]
    [Range(1.0f, 2.0f)]
    public float refractionScale = 1.33333f;

    [Tooltip("Custom shader for flat-port Snell's law refraction warp")]
    public Shader refractionShader;
    private Material refractionMaterial;
    private RenderTexture rawRT;

    public RenderTexture RefractedTexture => renderTexture;

    public float RefractionScale
    {
        get => refractionScale;
        set
        {
            refractionScale = Mathf.Clamp(value, 1.0f, 2.0f);
            if (refractionMaterial != null && cam != null) UpdateRefractionMaterial();
        }
    }

    public override string Topic => cameraType == CameraType.Front ? ROSSettings.Instance.FrontCameraTopic : ROSSettings.Instance.DownCameraTopic;

    private int resolutionWidth = 640;
    private int resolutionHeight = 480;

    private ImageMsg message;
    private RenderTexture renderTexture;
    
    // JPEG encoding
    private Texture2D encodingTexture;
    private byte[] cachedRawBuffer;
    private byte[] cachedFlippedBuffer;
    private CompressedImageMsg[] compressedMsgPool;
    private int compressedPoolIdx = 0;
    private ImageMsg[] rawImgMsgPool;
    private int rawImgPoolIdx = 0;
    private string compressedTopic;

    // Camera Info
    private CameraInfoMsg cameraInfoMsg;
    private CameraInfoMsg[] infoMsgPool;
    private int infoPoolIdx = 0;
    private string cameraInfoTopic;

    // Cached AsyncGPUReadback delegate & timestamp
    private System.Action<UnityEngine.Rendering.AsyncGPUReadbackRequest> readbackCallback;
    private RosMessageTypes.BuiltinInterfaces.TimeMsg pendingStamp;

    private static readonly System.Collections.Generic.List<CameraPublisher> activePublishers = new System.Collections.Generic.List<CameraPublisher>();

    /// <summary>
    /// Returns the enabled publisher that captures from the given camera, or null.
    /// The publisher usually lives on a different GameObject than its camera, so
    /// camera.GetComponent&lt;CameraPublisher&gt;() is not reliable.
    /// </summary>
    public static CameraPublisher ForCamera(Camera camera)
    {
        if (camera == null) return null;
        foreach (var publisher in activePublishers)
        {
            if (publisher.cam == camera) return publisher;
        }
        return null;
    }

    private void OnEnable()
    {
        activePublishers.Add(this);
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    private void OnDisable()
    {
        activePublishers.Remove(this);
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }

    private void OnEndCameraRendering(ScriptableRenderContext context, Camera renderedCam)
    {
        if (renderedCam != cam) return;
        ApplyRefraction();
    }

    protected override void Start()
    {
        if (cam == null) cam = GetComponent<Camera>();

        // Disable front camera ROS publishing if ZED streaming is active
        if (cameraType == CameraType.Front && 
            SimulationSettings.Instance != null && 
            SimulationSettings.Instance.StreamZEDCamera)
        {
            enabled = false;
            Debug.Log("[CameraPublisher] Front camera disabled (ZED streaming active)");
            return;
        }

        base.Start();
        
        // Camera uses custom rendering logic, not base class rate limiting
        useBaseRateLimiting = false;
        
        readbackCallback = OnReadbackComplete;
        pendingStamp = new RosMessageTypes.BuiltinInterfaces.TimeMsg();

        InitializeTexture();
        InitializeRefraction();
        InitializeCameraInfo();
    }

    protected override void RegisterPublisher()
    {
        ros.RegisterPublisher<ImageMsg>(Topic);
        
        // Register compressed image topic (ROS convention: topic/compressed)
        compressedTopic = Topic + "/compressed";
        ros.RegisterPublisher<CompressedImageMsg>(compressedTopic);
        
        // Register Camera Info Topic
        int lastSlashIndex = Topic.LastIndexOf('/');
        if (lastSlashIndex >= 0)
        {
            cameraInfoTopic = Topic.Substring(0, lastSlashIndex + 1) + "camera_info";
        }
        else
        {
            cameraInfoTopic = "camera_info";
        }
        
        ros.RegisterPublisher<CameraInfoMsg>(cameraInfoTopic);
    }

    private void InitializeCameraInfo()
    {
        cameraInfoMsg = new CameraInfoMsg();
        string currentFrameId = cameraType == CameraType.Front ? ROSSettings.Instance.FrontCamFrameId : ROSSettings.Instance.DownCamFrameId;
        cameraInfoMsg.header = new HeaderMsg { frame_id = currentFrameId };
        cameraInfoMsg.width = (uint)resolutionWidth;
        cameraInfoMsg.height = (uint)resolutionHeight;
        cameraInfoMsg.distortion_model = "plumb_bob";
        cameraInfoMsg.D = new double[] { 0, 0, 0, 0, 0 };

        // Calculate Focal Length (fx, fy)
        // f = (height / 2) / tan(FOV / 2)
        double f = (resolutionHeight / 2.0) / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        if (simulateRefraction)
        {
            f *= refractionScale;
        }
        double cx = resolutionWidth / 2.0;
        double cy = resolutionHeight / 2.0;

        // K Matrix
        cameraInfoMsg.K = new double[] { f, 0, cx, 0, f, cy, 0, 0, 1 };
        // P Matrix
        cameraInfoMsg.P = new double[] { f, 0, cx, 0,  0, f, cy, 0,  0, 0, 1, 0 };
        // R Matrix
        cameraInfoMsg.R = new double[] { 1, 0, 0, 0, 1, 0, 0, 0, 1 };

        infoMsgPool = new CameraInfoMsg[2];
        for (int i = 0; i < 2; i++)
        {
            infoMsgPool[i] = new CameraInfoMsg
            {
                header = new HeaderMsg { frame_id = currentFrameId, stamp = new RosMessageTypes.BuiltinInterfaces.TimeMsg() },
                width = (uint)resolutionWidth,
                height = (uint)resolutionHeight,
                distortion_model = "plumb_bob",
                D = cameraInfoMsg.D,
                K = cameraInfoMsg.K,
                P = cameraInfoMsg.P,
                R = cameraInfoMsg.R
            };
        }
    }

    private void InitializeTexture()
    {
        if (cameraType == CameraType.Front)
        {
            if (SimulationSettings.Instance != null)
            {
                resolutionWidth = SimulationSettings.Instance.FrontCamWidth;
                resolutionHeight = SimulationSettings.Instance.FrontCamHeight;
                PublishRate = SimulationSettings.Instance.FrontCamRate;
                if (cam != null) cam.fieldOfView = SimulationSettings.Instance.FrontCamFOV;
            }
            else
            {
                resolutionWidth = 672;
                resolutionHeight = 376;
                PublishRate = 10;
                if (cam != null) cam.fieldOfView = 40.9f;
            }
        }
        else
        {
            if (SimulationSettings.Instance != null)
            {
                resolutionWidth = SimulationSettings.Instance.DownCamWidth;
                resolutionHeight = SimulationSettings.Instance.DownCamHeight;
                PublishRate = SimulationSettings.Instance.DownCamRate;
                if (cam != null) cam.fieldOfView = SimulationSettings.Instance.DownCamFOV;
            }
            else
            {
                resolutionWidth = 640;
                resolutionHeight = 480;
                PublishRate = 10;
                if (cam != null) cam.fieldOfView = 38.2f;
            }
        }


        // 1. Raw camera target texture (with depth 24 for 3D camera rendering)
        if (rawRT != null) rawRT.Release();
        rawRT = new RenderTexture(resolutionWidth, resolutionHeight, 24, RenderTextureFormat.ARGB32)
        {
            useMipMap = false,
            enableRandomWrite = true
        };
        rawRT.Create();
        if (cam != null) cam.targetTexture = rawRT;

        // 2. Refracted / output texture (color buffer for ROS readback and UI feed)
        if (renderTexture != null) renderTexture.Release();
        renderTexture = new RenderTexture(resolutionWidth, resolutionHeight, 0, RenderTextureFormat.ARGB32)
        {
            useMipMap = false,
            enableRandomWrite = true
        };
        renderTexture.Create();

        string currentFrameId = cameraType == CameraType.Front ? ROSSettings.Instance.FrontCamFrameId : ROSSettings.Instance.DownCamFrameId;

        // Initialize pooled raw ImageMsg
        rawImgMsgPool = new ImageMsg[2]
        {
            new ImageMsg { header = new HeaderMsg { frame_id = currentFrameId, stamp = new RosMessageTypes.BuiltinInterfaces.TimeMsg() }, encoding = "rgb8", width = (uint)resolutionWidth, height = (uint)resolutionHeight, step = (uint)(resolutionWidth * 3) },
            new ImageMsg { header = new HeaderMsg { frame_id = currentFrameId, stamp = new RosMessageTypes.BuiltinInterfaces.TimeMsg() }, encoding = "rgb8", width = (uint)resolutionWidth, height = (uint)resolutionHeight, step = (uint)(resolutionWidth * 3) }
        };
        message = rawImgMsgPool[0];
        
        // Initialize pooled compressed message
        compressedMsgPool = new CompressedImageMsg[2]
        {
            new CompressedImageMsg { header = new HeaderMsg { frame_id = currentFrameId, stamp = new RosMessageTypes.BuiltinInterfaces.TimeMsg() }, format = "jpeg" },
            new CompressedImageMsg { header = new HeaderMsg { frame_id = currentFrameId, stamp = new RosMessageTypes.BuiltinInterfaces.TimeMsg() }, format = "jpeg" }
        };
        
        // Create encoding texture for JPEG compression
        encodingTexture = new Texture2D(resolutionWidth, resolutionHeight, TextureFormat.RGB24, false);
    }

    private bool isReading = false;  // Prevent queueing too many async requests
    
    protected override void FixedUpdate()
    {
        // Check if we should publish to ROS
        bool shouldPublish = false;
        if (SimulationSettings.Instance.PublishROS)
        {
            if (cameraType == CameraType.Front && SimulationSettings.Instance.PublishFrontCam) shouldPublish = true;
            if (cameraType == CameraType.Down && SimulationSettings.Instance.PublishDownCam) shouldPublish = true;
        }

        if (!shouldPublish || isReading) return;
        
        // Rate limiting
        timeSinceLastPublish += Time.fixedDeltaTime;
        if (timeSinceLastPublish >= timeBetweenPublishes)
        {
            timeSinceLastPublish = 0f;

            // Safety Check
            if (cam == null || renderTexture == null || !renderTexture.IsCreated()) return;
            
            // Capture timestamp NOW (when frame was rendered), not when readback completes
            ROSClock.PopulateROSTimestamp(pendingStamp);
            
            isReading = true;
            
            // Async GPU Readback - doesn't stall CPU waiting for GPU
            UnityEngine.Rendering.AsyncGPUReadback.Request(renderTexture, 0, TextureFormat.RGB24, readbackCallback);
        }
    }
    
    private void OnReadbackComplete(UnityEngine.Rendering.AsyncGPUReadbackRequest req)
    {
        isReading = false;
        
        if (req.hasError)
        {
            Debug.LogWarning($"[CameraPublisher] AsyncGPUReadback failed for {cameraType}");
            return;
        }
        
        // Get raw data as NativeArray (no allocation)
        var rawData = req.GetData<byte>();
        
        // Check if JPEG compression is enabled
        bool useJPEG = SimulationSettings.Instance != null && SimulationSettings.Instance.UseJPEGCompression;
        
        string currentFrameId = cameraType == CameraType.Front ? ROSSettings.Instance.FrontCamFrameId : ROSSettings.Instance.DownCamFrameId;

        if (useJPEG)
        {
            // Direct native JPEG encoding path - zero managed heap allocations.
            // EncodeNativeArrayToJPG natively expects Unity's GPU readback buffer layout
            // and generates standard upright JPEG files automatically.
            int quality = SimulationSettings.Instance.JPEGQuality;
            int rowBytes = resolutionWidth * 3;

            // Encode directly to native JPEG memory (0 managed GC alloc)
            var jpegNative = ImageConversion.EncodeNativeArrayToJPG(
                rawData,
                UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8_SRGB,
                (uint)resolutionWidth,
                (uint)resolutionHeight,
                (uint)rowBytes,
                quality
            );

            int jpegLength = jpegNative.Length;
            if (cachedRawBuffer == null || cachedRawBuffer.Length < jpegLength)
            {
                cachedRawBuffer = new byte[Mathf.Max(jpegLength * 2, 256 * 1024)];
            }

            Unity.Collections.NativeArray<byte>.Copy(jpegNative, 0, cachedRawBuffer, 0, jpegLength);
            jpegNative.Dispose();

            var compMsg = compressedMsgPool[compressedPoolIdx];
            compressedPoolIdx = 1 - compressedPoolIdx;

            compMsg.header.stamp.sec = pendingStamp.sec;
            compMsg.header.stamp.nanosec = pendingStamp.nanosec;
            compMsg.header.frame_id = currentFrameId;
            compMsg.format = "jpeg";
            compMsg.data = cachedRawBuffer;
            compMsg.dataLength = jpegLength;

            Publish(compressedTopic, compMsg);
        }
        else
        {
            // Raw RGB8 path - publish to main topic using ImageMsg
            // Unity readback buffer is bottom-up; ROS REP 118 requires row 0 at the top
            int rowBytes = resolutionWidth * 3;
            if (cachedFlippedBuffer == null || cachedFlippedBuffer.Length != rawData.Length)
            {
                cachedFlippedBuffer = new byte[rawData.Length];
            }
            for (int y = 0; y < resolutionHeight; y++)
            {
                int srcOffset = y * rowBytes;
                int dstOffset = (resolutionHeight - 1 - y) * rowBytes;
                Unity.Collections.NativeArray<byte>.Copy(rawData, srcOffset, cachedFlippedBuffer, dstOffset, rowBytes);
            }

            var imgMsg = rawImgMsgPool[rawImgPoolIdx];
            rawImgPoolIdx = 1 - rawImgPoolIdx;

            imgMsg.header.stamp.sec = pendingStamp.sec;
            imgMsg.header.stamp.nanosec = pendingStamp.nanosec;
            imgMsg.header.frame_id = currentFrameId;
            imgMsg.data = cachedFlippedBuffer;

            Publish(Topic, imgMsg);
        }

        // Publish Camera Info with synced timestamp
        if (infoMsgPool != null)
        {
            var infoMsg = infoMsgPool[infoPoolIdx];
            infoPoolIdx = 1 - infoPoolIdx;

            infoMsg.header.stamp.sec = pendingStamp.sec;
            infoMsg.header.stamp.nanosec = pendingStamp.nanosec;
            infoMsg.header.frame_id = currentFrameId;

            Publish(cameraInfoTopic, infoMsg);
        }
    }

    public override void PublishMessage()
    {
        // No-op: Publishing is now handled asynchronously in OnReadbackComplete
    }

    private int lastRefractionFrame = -1;

    private void OnValidate()
    {
        refractionScale = Mathf.Clamp(refractionScale, 1.0f, 2.0f);
        if (refractionMaterial != null && cam != null)
        {
            UpdateRefractionMaterial();
        }
    }

    private void EnsureRefractionResources()
    {
        if (refractionShader == null)
        {
            refractionShader = Shader.Find("Hidden/UnderwaterRefraction");
        }

        if (refractionMaterial == null && refractionShader != null)
        {
            refractionMaterial = new Material(refractionShader);
        }

        if (rawRT == null || rawRT.width != resolutionWidth || rawRT.height != resolutionHeight || !rawRT.IsCreated())
        {
            if (rawRT != null) rawRT.Release();
            rawRT = new RenderTexture(resolutionWidth, resolutionHeight, 24, RenderTextureFormat.ARGB32)
            {
                useMipMap = false,
                enableRandomWrite = true
            };
            rawRT.Create();
            if (cam != null) cam.targetTexture = rawRT;
        }

        if (renderTexture == null || renderTexture.width != resolutionWidth || renderTexture.height != resolutionHeight || !renderTexture.IsCreated())
        {
            if (renderTexture != null) renderTexture.Release();
            renderTexture = new RenderTexture(resolutionWidth, resolutionHeight, 0, RenderTextureFormat.ARGB32)
            {
                useMipMap = false,
                enableRandomWrite = true
            };
            renderTexture.Create();
        }
    }

    private void InitializeRefraction()
    {
        if (SimulationSettings.Instance != null)
        {
            simulateRefraction = SimulationSettings.Instance.SimulateWaterRefraction;
            if (cameraType == CameraType.Down)
            {
                refractionScale = SimulationSettings.Instance.DownCamRefractionScale;
            }
            else
            {
                refractionScale = SimulationSettings.Instance.WaterRefractionIndex;
            }
        }

        EnsureRefractionResources();

        if (refractionMaterial != null)
        {
            UpdateRefractionMaterial();
            Debug.Log($"[CameraPublisher] {cameraType} camera refraction initialized (scale={refractionScale:F4}, enabled={simulateRefraction}).");
        }
        else
        {
            Debug.LogWarning($"[CameraPublisher] Hidden/UnderwaterRefraction shader not found for {cameraType} camera!");
        }
    }

    public void ApplyRefraction()
    {
        if (Application.isPlaying && Time.frameCount == lastRefractionFrame) return;
        lastRefractionFrame = Time.frameCount;

        EnsureRefractionResources();
        if (cam == null || rawRT == null || renderTexture == null) return;

        bool simulate = simulateRefraction;
        if (SimulationSettings.Instance != null && !SimulationSettings.Instance.SimulateWaterRefraction)
        {
            simulate = false;
        }

        if (simulate && refractionScale > 1.0001f && refractionMaterial != null)
        {
            UpdateRefractionMaterial();
            Graphics.Blit(rawRT, renderTexture, refractionMaterial);
        }
        else
        {
            Graphics.Blit(rawRT, renderTexture);
        }
    }

    private void UpdateRefractionMaterial()
    {
        if (refractionMaterial == null || cam == null) return;

        bool simulate = simulateRefraction;
        if (SimulationSettings.Instance != null && !SimulationSettings.Instance.SimulateWaterRefraction)
        {
            simulate = false;
        }

        float fy = (resolutionHeight * 0.5f) / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float fx = fy;

        refractionMaterial.SetVector("_Resolution", new Vector4(resolutionWidth, resolutionHeight, 0, 0));
        refractionMaterial.SetVector("_FocalLength", new Vector4(fx, fy, 0, 0));
        refractionMaterial.SetVector("_PrincipalPoint", new Vector4(0.5f, 0.5f, 0, 0));
        refractionMaterial.SetFloat("_RefractionIndex", refractionScale);
        refractionMaterial.SetFloat("_Enabled", simulate ? 1.0f : 0.0f);
        refractionMaterial.SetFloat("_FlipY", 0.0f);
    }

    protected virtual void OnDestroy()
    {
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;

        if (cam != null && cam.targetTexture == rawRT)
        {
            cam.targetTexture = null;
        }

        if (rawRT != null)
        {
            rawRT.Release();
            Destroy(rawRT);
        }

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }

        if (refractionMaterial != null)
        {
            Destroy(refractionMaterial);
        }

        if (encodingTexture != null)
        {
            Destroy(encodingTexture);
        }
    }
}