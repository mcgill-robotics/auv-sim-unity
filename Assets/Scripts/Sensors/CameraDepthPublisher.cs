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
        if (depthShader == null)
        {
            depthShader = Resources.Load<Shader>("Shaders/DepthExtractor");
        }
        
        if (depthShader == null)
        {
            Debug.LogError("[CameraDepthPublisher] Could not find 'Hidden/DepthExtractor' shader! Make sure it is in Resources/Shaders/.");
            enabled = false;
            return;
        }

        depthMaterial = new Material(depthShader);
        depthMaterial.SetFloat("_Near", sourceCamera.nearClipPlane);
        depthMaterial.SetFloat("_Far", sourceCamera.farClipPlane);

        // Create textures
        depthRT = new RenderTexture(publishWidth, publishHeight, 0, RenderTextureFormat.RFloat);
        depthRT.enableRandomWrite = true;
        depthRT.Create();
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
    }

    protected override void RegisterPublisher()
    {
        ros.RegisterPublisher<ImageMsg>(Topic);
    }

    protected override void FixedUpdate()
    {
        if (!SimulationSettings.Instance.PublishDepth) return;
        base.FixedUpdate();
    }

    protected override void PublishMessage()
    {
        CaptureAndPublish();
    }

    private void CaptureAndPublish()
    {
        // Manual Render to ensure depth buffer is updated
        if (sourceCamera) sourceCamera.Render();

        // Blit depth texture using our material
        Graphics.Blit(null, depthRT, depthMaterial);

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
        if (depthRT != null) depthRT.Release();
        if (depthMaterial != null) Destroy(depthMaterial);
    }
}
