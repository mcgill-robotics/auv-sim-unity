using UnityEngine;
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



    protected override string Topic => cameraType == CameraType.Front ? ROSSettings.Instance.FrontCameraTopic : ROSSettings.Instance.DownCameraTopic;

    private int resolutionWidth = 640;
    private int resolutionHeight = 480;
    
    // Rendering Loop
    private float timeBetweenCaptures;
    private float timeSinceLastCapture;

    private ImageMsg message;
    private Texture2D texture2D;
    private RenderTexture renderTexture;
    private Rect rect;

    // Camera Info
    private CameraInfoMsg cameraInfoMsg;
    private string cameraInfoTopic;

    protected override void Start()
    {
        // Disable automatic rendering to save performance
        if (cam != null) cam.enabled = false;
        
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
        InitializeTexture();
        InitializeCameraInfo();
        
        // Initialize timing
        timeBetweenCaptures = 1f / publishRate;
        timeSinceLastCapture = 0f;
    }

    protected override void RegisterPublisher()
    {
        ros.RegisterPublisher<ImageMsg>(Topic);
        
        // Register Camera Info Topic
        cameraInfoTopic = Topic.Replace("image_raw", "camera_info");
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
        double cx = resolutionWidth / 2.0;
        double cy = resolutionHeight / 2.0;

        // K Matrix
        cameraInfoMsg.K = new double[] { f, 0, cx, 0, f, cy, 0, 0, 1 };
        // P Matrix
        cameraInfoMsg.P = new double[] { f, 0, cx, 0,  0, f, cy, 0,  0, 0, 1, 0 };
        // R Matrix
        cameraInfoMsg.R = new double[] { 1, 0, 0, 0, 1, 0, 0, 0, 1 };
    }

    private void InitializeTexture()
    {
        if (cameraType == CameraType.Front)
        {
            resolutionWidth = SimulationSettings.Instance.FrontCamWidth;
            resolutionHeight = SimulationSettings.Instance.FrontCamHeight;
            publishRate = SimulationSettings.Instance.FrontCamRate;
            cam.fieldOfView = SimulationSettings.Instance.FrontCamFOV;
        }
        else
        {
            resolutionWidth = SimulationSettings.Instance.DownCamWidth;
            resolutionHeight = SimulationSettings.Instance.DownCamHeight;
            publishRate = SimulationSettings.Instance.DownCamRate;
        }


        // Texture Safety: Reuse existing if valid, otherwise create new
        if (cam.targetTexture != null && 
            cam.targetTexture.width == resolutionWidth && 
            cam.targetTexture.height == resolutionHeight)
        {
            // Reuse existing texture (likely created by SimulatorHUD or previous run)
            renderTexture = cam.targetTexture;
        }
        else
        {
            // Create new
            if (cam.targetTexture != null)
            {
                cam.targetTexture.Release();
            }
            renderTexture = new RenderTexture(resolutionWidth, resolutionHeight, 24);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();
            cam.targetTexture = renderTexture;
        }

        texture2D = new Texture2D(resolutionWidth, resolutionHeight, TextureFormat.RGB24, false);
        rect = new Rect(0, 0, resolutionWidth, resolutionHeight);
        message = new ImageMsg();
        string currentFrameId = cameraType == CameraType.Front ? ROSSettings.Instance.FrontCamFrameId : ROSSettings.Instance.DownCamFrameId;
        message.header = new HeaderMsg { frame_id = currentFrameId };
        message.encoding = "rgb8";
        message.width = (uint)resolutionWidth;
        message.height = (uint)resolutionHeight;
        message.step = (uint)(resolutionWidth * 3);
    }

    public bool ForceCaptureForUI { get; set; } = false;

    protected override void FixedUpdate()
    {
        // Determine if we should render
        bool shouldRender = false;
        bool shouldPublishROS = false;

        // 1. Check ROS Publishing
        if (SimulationSettings.Instance.PublishROS)
        {
            if (cameraType == CameraType.Front && SimulationSettings.Instance.PublishFrontCam) shouldPublishROS = true;
            if (cameraType == CameraType.Down && SimulationSettings.Instance.PublishDownCam) shouldPublishROS = true;
        }

        // 2. Check ZED Streaming (Front Only)
        // Removed: ZED logic handled by disabling component in Start

        // 3. Combine Conditions
        if (shouldPublishROS || ForceCaptureForUI)
        {
            shouldRender = true;
        }

        if (!shouldRender) return;
        
        // Rendering Loop
        timeSinceLastCapture += Time.fixedDeltaTime;
        if (timeSinceLastCapture >= timeBetweenCaptures)
        {
            timeSinceLastCapture = 0f;

            // Safety Check: Ensure textures are still valid before rendering
            if (cam == null || texture2D == null || renderTexture == null || !renderTexture.IsCreated()) return;
            
            // 1. Render Camera
            RenderTexture currentRT = RenderTexture.active;
            RenderTexture.active = renderTexture;
            cam.Render();
            texture2D.ReadPixels(rect, 0, 0);
            texture2D.Apply();
            RenderTexture.active = currentRT;
            
            // 2. Publish to ROS (if enabled)
            if (shouldPublishROS)
            {
                PublishMessage();
            }
        }
    }

    protected override void PublishMessage()
    {
        // Note: Rendering is now done in FixedUpdate loop
        
        // Publish Image
        message.data = texture2D.GetRawTextureData();
        message.header.stamp = ROSClock.GetROSTimestamp();
        ros.Publish(Topic, message);

        // Publish Camera Info
        cameraInfoMsg.header.stamp = message.header.stamp; // Sync timestamps
        ros.Publish(cameraInfoTopic, cameraInfoMsg);
    }
    
    // Helper for flipping texture (used by Depth Publisher)
    public static byte[] FlipTextureVertically(Texture2D original, int step)
    {
        return original.GetRawTextureData(); 
    }

    protected virtual void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
        if (texture2D != null)
        {
            Destroy(texture2D);
        }
    }
}