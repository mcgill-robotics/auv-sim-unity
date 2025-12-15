using UnityEngine;

/// <summary>
/// Centralized manager that controls camera rendering for the simulation.
/// Checks all relevant settings (ROS publishing, UI selection, ZED streaming)
/// and renders cameras at the appropriate frame rates.
/// Other scripts (CameraPublisher, CameraDepthPublisher) become consumers
/// that just read from the textures rather than calling Render() themselves.
/// </summary>
[DefaultExecutionOrder(-50)]
public class CameraRenderManager : MonoBehaviour
{
    public static CameraRenderManager Instance { get; private set; }

    [Header("Camera References")]
    [Tooltip("Front/Left camera used for RGB, depth, and ZED left eye")]
    public Camera frontCamera;
    
    [Tooltip("Front/Right camera for ZED stereo (optional, only needed for ZED streaming)")]
    public Camera frontRightCamera;
    
    [Tooltip("Downward-facing camera")]
    public Camera downCamera;

    [Header("Frame Rate Settings")]
    [Tooltip("Front camera rate comes from SimulationSettings.FrontCamRate")]
    public float frontCameraRate = 30f;
    
    [Tooltip("Down camera rate comes from SimulationSettings.DownCamRate")]
    public float downCameraRate = 30f;

    // UI selection flags - set by SimulatorHUD
    [HideInInspector] public bool frontCameraUINeeded;
    [HideInInspector] public bool frontDepthUINeeded;
    [HideInInspector] public bool downCameraUINeeded;

    // Timing
    private float frontCameraLastRender;
    private float downCameraLastRender;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Disable cameras - we control rendering manually
        if (frontCamera != null) frontCamera.enabled = false;
        if (frontRightCamera != null) frontRightCamera.enabled = false;
        if (downCamera != null) downCamera.enabled = false;
    }

    private void Start()
    {
        // Get rates from settings
        if (SimulationSettings.Instance != null)
        {
            frontCameraRate = SimulationSettings.Instance.FrontCamRate;
            downCameraRate = SimulationSettings.Instance.DownCamRate;
        }
    }

    private void FixedUpdate()
    {
        // Render front camera if needed
        if (NeedsFrontCamera())
        {
            float interval = 1.0f / frontCameraRate;
            if (Time.time - frontCameraLastRender >= interval)
            {
                RenderFrontCamera();
                frontCameraLastRender = Time.time;
            }
        }

        // Render down camera if needed
        if (NeedsDownCamera())
        {
            float interval = 1.0f / downCameraRate;
            if (Time.time - downCameraLastRender >= interval)
            {
                RenderDownCamera();
                downCameraLastRender = Time.time;
            }
        }
    }

    /// <summary>
    /// Determines if front camera needs to be rendered this frame.
    /// </summary>
    private bool NeedsFrontCamera()
    {
        if (frontCamera == null) return false;
        
        var s = SimulationSettings.Instance;
        if (s == null) return false;
        
        // ROS publishing (front camera or depth - both use front camera)
        bool rosNeeded = s.PublishROS && (s.PublishFrontCam || s.PublishDepth);
        
        // ZED streaming needs the camera rendered
        bool zedNeeded = s.StreamZEDCamera;
        
        // UI display selection
        bool uiNeeded = frontCameraUINeeded || frontDepthUINeeded;
        
        return rosNeeded || zedNeeded || uiNeeded;
    }

    /// <summary>
    /// Determines if down camera needs to be rendered this frame.
    /// </summary>
    private bool NeedsDownCamera()
    {
        if (downCamera == null) return false;
        
        var s = SimulationSettings.Instance;
        if (s == null) return false;
        
        // ROS publishing
        bool rosNeeded = s.PublishROS && s.PublishDownCam;
        
        // UI display selection
        bool uiNeeded = downCameraUINeeded;
        
        return rosNeeded || uiNeeded;
    }

    private void RenderFrontCamera()
    {
        if (frontCamera != null)
        {
            frontCamera.Render();
        }
        
        // Also render right camera for ZED stereo streaming
        if (frontRightCamera != null && SimulationSettings.Instance.StreamZEDCamera)
        {
            frontRightCamera.Render();
        }
    }

    private void RenderDownCamera()
    {
        if (downCamera != null)
        {
            downCamera.Render();
        }
    }
}
