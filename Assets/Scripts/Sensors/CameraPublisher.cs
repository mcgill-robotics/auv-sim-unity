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

        if (cam.targetTexture != null)
        {
            cam.targetTexture.Release();
        }
        renderTexture = new RenderTexture(resolutionWidth, resolutionHeight, 24);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();
        cam.targetTexture = renderTexture;
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

    protected override void FixedUpdate()
    {
        if (cameraType == CameraType.Front && !SimulationSettings.Instance.PublishFrontCam) return;
        if (cameraType == CameraType.Down && !SimulationSettings.Instance.PublishDownCam) return;
        
        base.FixedUpdate();
    }

    protected override void PublishMessage()
    {
        // Render the camera
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = renderTexture;
        cam.Render();
        texture2D.ReadPixels(rect, 0, 0);
        texture2D.Apply();
        RenderTexture.active = currentRT;

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
}