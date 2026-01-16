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



    public override string Topic => cameraType == CameraType.Front ? ROSSettings.Instance.FrontCameraTopic : ROSSettings.Instance.DownCameraTopic;

    private int resolutionWidth = 640;
    private int resolutionHeight = 480;

    private ImageMsg message;
    private RenderTexture renderTexture;
    
    // JPEG encoding
    private Texture2D encodingTexture;
    private byte[] cachedRawBuffer;
    private CompressedImageMsg compressedMessage;
    private string compressedTopic;

    // Camera Info
    private CameraInfoMsg cameraInfoMsg;
    private string cameraInfoTopic;

    protected override void Start()
    {
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
        
        InitializeTexture();
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
            PublishRate = SimulationSettings.Instance.FrontCamRate;
            cam.fieldOfView = SimulationSettings.Instance.FrontCamFOV;
        }
        else
        {
            resolutionWidth = SimulationSettings.Instance.DownCamWidth;
            resolutionHeight = SimulationSettings.Instance.DownCamHeight;
            PublishRate = SimulationSettings.Instance.DownCamRate;
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

        message = new ImageMsg();
        string currentFrameId = cameraType == CameraType.Front ? ROSSettings.Instance.FrontCamFrameId : ROSSettings.Instance.DownCamFrameId;
        message.header = new HeaderMsg { frame_id = currentFrameId };
        message.encoding = "rgb8";
        message.width = (uint)resolutionWidth;
        message.height = (uint)resolutionHeight;
        message.step = (uint)(resolutionWidth * 3);
        
        // Initialize compressed message
        compressedMessage = new CompressedImageMsg();
        compressedMessage.header = new HeaderMsg { frame_id = currentFrameId };
        compressedMessage.format = "jpeg";
        
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
            var stamp = ROSClock.GetROSTimestamp();
            
            isReading = true;
            
            // Async GPU Readback - doesn't stall CPU waiting for GPU
            UnityEngine.Rendering.AsyncGPUReadback.Request(renderTexture, 0, TextureFormat.RGB24, 
                req => OnReadbackComplete(req, stamp));
        }
    }
    
    private void OnReadbackComplete(UnityEngine.Rendering.AsyncGPUReadbackRequest req, RosMessageTypes.BuiltinInterfaces.TimeMsg stamp)
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
        
        if (useJPEG)
        {
            // JPEG encoding path - publish to /compressed topic using CompressedImageMsg
            if (cachedRawBuffer == null || cachedRawBuffer.Length != rawData.Length)
            {
                cachedRawBuffer = new byte[rawData.Length];
            }
            rawData.CopyTo(cachedRawBuffer);
            
            // Load into texture and encode
            if (encodingTexture != null)
            {
                encodingTexture.LoadRawTextureData(cachedRawBuffer);
                encodingTexture.Apply();
                
                int quality = SimulationSettings.Instance.JPEGQuality;
                compressedMessage.data = encodingTexture.EncodeToJPG(quality);
                compressedMessage.header.stamp = stamp;
                ros.Publish(compressedTopic, compressedMessage);
            }
        }
        else
        {
            // Raw RGB8 path - publish to main topic using ImageMsg
            if (message.data == null || message.data.Length != rawData.Length)
            {
                message.data = new byte[rawData.Length];
            }
            rawData.CopyTo(message.data);
            message.header.stamp = stamp;
            ros.Publish(Topic, message);
        }

        // Publish Camera Info with synced timestamp
        cameraInfoMsg.header.stamp = stamp;
        ros.Publish(cameraInfoTopic, cameraInfoMsg);
    }

    public override void PublishMessage()
    {
        // No-op: Publishing is now handled asynchronously in OnReadbackComplete
    }


    protected virtual void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
        
        if (encodingTexture != null)
        {
            Destroy(encodingTexture);
        }
    }
}