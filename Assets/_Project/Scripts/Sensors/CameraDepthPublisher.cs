using UnityEngine;
using UnityEngine.Rendering;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;

/// <summary>
/// Publishes depth data from the front camera's depth texture.
/// Uses Unity's built-in depth buffer instead of a separate camera.
/// </summary>
public class CameraDepthPublisher : ROSPublisher
{
    protected override string Topic => ROSSettings.Instance.DepthCameraTopic;

    [Header("Camera Reference")]
    [Tooltip("The front/left camera to extract depth from")]
    public Camera sourceCamera;

    [Header("Settings")]


    private int publishWidth;
    private int publishHeight;
    private Texture2D depthTexture;
    private RenderTexture depthRT;
    public RenderTexture DepthTexture => depthRT;
    private ImageMsg imageMsg;
    private Material depthMaterial;
    private Material visualizerMaterial;
    private RenderTexture visualizationRT;
    public RenderTexture VisualizationTexture => visualizationRT;

    // Shader to convert depth buffer to linear depth


    protected override void Start()
    {
        base.Start();
        
        // Disable depth publishing when ZED is active (ZED SDK computes depth from stereo)
        if (SimulationSettings.Instance != null && SimulationSettings.Instance.StreamZEDCamera)
        {
            enabled = false;
            Debug.Log("[CameraDepthPublisher] Disabled (ZED streaming active - ZED SDK computes depth)");
            return;
        }
        
        if (sourceCamera == null)
        {
            Debug.LogError("[CameraDepthPublisher] Source camera not assigned!");
            enabled = false;
            return;
        }

        Initialize();
        
        // Disable automatic rendering
        if (sourceCamera != null) sourceCamera.enabled = false;
    }

    private CommandBuffer cmdBuffer;

    private void Initialize()
    {
        // Get resolution from settings
        publishRate = SimulationSettings.Instance.FrontCamRate;
        publishWidth = SimulationSettings.Instance.FrontCamWidth;
        publishHeight = SimulationSettings.Instance.FrontCamHeight;

        // Enable depth texture on source camera
        sourceCamera.depthTextureMode = DepthTextureMode.Depth;

        // Create depth extraction material
        Shader depthShader = Shader.Find("Hidden/DepthExtractor");
        if (depthShader == null) depthShader = Resources.Load<Shader>("Shaders/DepthExtractor");
        
        if (depthShader == null)
        {
            Debug.LogError("[CameraDepthPublisher] Could not find 'Hidden/DepthExtractor' shader!");
            enabled = false;
            return;
        }

        depthMaterial = new Material(depthShader);
        depthMaterial.SetFloat("_Near", sourceCamera.nearClipPlane);
        depthMaterial.SetFloat("_Far", sourceCamera.farClipPlane);

        // Create visualization material
        Shader visualizerShader = Shader.Find("Hidden/DepthVisualizer");
        if (visualizerShader == null) visualizerShader = Resources.Load<Shader>("Shaders/DepthVisualizer");
        
        if (visualizerShader != null)
        {
            visualizerMaterial = new Material(visualizerShader);
            visualizerMaterial.SetFloat("_MaxDepth", 20.0f); // Adjust max depth for visualization
        }
        else
        {
            Debug.LogWarning("[CameraDepthPublisher] Could not find 'Hidden/DepthVisualizer' shader. Visualization might be incorrect.");
        }

        // Create textures
        depthRT = new RenderTexture(publishWidth, publishHeight, 0, RenderTextureFormat.RFloat);
        depthRT.enableRandomWrite = true;
        depthRT.Create();
        
        visualizationRT = new RenderTexture(publishWidth, publishHeight, 0, RenderTextureFormat.ARGB32);
        visualizationRT.Create();

        depthTexture = new Texture2D(publishWidth, publishHeight, TextureFormat.RFloat, false);

        // Setup ROS message
        imageMsg = new ImageMsg
        {
            header = new HeaderMsg { frame_id = ROSSettings.Instance.DepthCamFrameId },
            encoding = "32FC1",
            width = (uint)publishWidth,
            height = (uint)publishHeight,
            step = (uint)(publishWidth * sizeof(float))
        };

        // --- Setup Command Buffer ---
        cmdBuffer = new CommandBuffer();
        cmdBuffer.name = "Depth Extraction";
        
        // Blit to Depth RT (Raw)
        cmdBuffer.Blit(null, depthRT, depthMaterial);
        
        // Blit to Visualization RT (Normalized)
        if (visualizerMaterial != null)
        {
            cmdBuffer.Blit(null, visualizationRT, visualizerMaterial);
        }

        // Attach to camera
        sourceCamera.AddCommandBuffer(CameraEvent.AfterEverything, cmdBuffer);
    }

    protected override void RegisterPublisher()
    {
        ros.RegisterPublisher<ImageMsg>(Topic);
    }

    public bool ForceCaptureForUI { get; set; } = false;
    private float lastCaptureTime = 0f;

    protected override void FixedUpdate()
    {
        // 1. If ROS publishing is enabled, let the base class handle the timing and calling PublishMessage
        if (SimulationSettings.Instance.PublishDepth)
        {
            base.FixedUpdate();
        }
        // 2. If ROS is disabled but UI needs the feed
        else if (ForceCaptureForUI)
        {
            // Simple rate limiting to match the configured rate
            float interval = 1.0f / publishRate;
            if (Time.time - lastCaptureTime >= interval)
            {
                Capture();
                lastCaptureTime = Time.time;
            }
        }
    }

    protected override void PublishMessage()
    {
        // Called by base.FixedUpdate when it's time to publish
        Capture();
        RequestReadback();
        lastCaptureTime = Time.time;
    }

    private void Capture()
    {
        // Manual Render to ensure depth buffer is updated and CommandBuffer executes
        if (sourceCamera) sourceCamera.Render();
    }

    private void RequestReadback()
    {
        // Request async readback
        AsyncGPUReadback.Request(depthRT, 0, TextureFormat.RFloat, OnDepthReadback);
    }

    private void OnDepthReadback(AsyncGPUReadbackRequest request)
    {
        if (request.hasError)
        {
            Debug.LogWarning("[CameraDepthPublisher] GPU Readback error detected.");
            return;
        }

        // Get raw float data directly from the request
        imageMsg.data = request.GetData<byte>().ToArray();

        imageMsg.header.stamp = ROSClock.GetROSTimestamp();
        ros.Publish(Topic, imageMsg);
    }

    private void OnDestroy()
    {
        if (sourceCamera != null && cmdBuffer != null)
        {
            sourceCamera.RemoveCommandBuffer(CameraEvent.AfterEverything, cmdBuffer);
        }
        
        if (cmdBuffer != null) cmdBuffer.Release();

        if (depthRT != null) depthRT.Release();
        if (visualizationRT != null) visualizationRT.Release();
        if (depthMaterial != null) Destroy(depthMaterial);
        if (visualizerMaterial != null) Destroy(visualizerMaterial);
    }
}
