using UnityEngine;

[ExecuteAlways]
public class SimulationSettings : MonoBehaviour
{
    public static SimulationSettings Instance { get; private set; }

    [Header("Global Toggles")]
    [Tooltip("Enable ROS TCP connection and publishing")]
    public bool PublishROS = false;
    
    [Tooltip("Show simulation objects (gates, buoys, etc.) in scene")]
    public bool DisplaySimObjects = false;
    
    [Tooltip("Number of hydrophones option (0-4)")]
    [Range(0, 4)]
    public int HydrophonesNumberOption = 0;

    [Space(10)]
    [Header("Sensor Publisher Toggles")]
    [Tooltip("Publish DVL velocity and altitude data")]
    public bool PublishDVL = false;
    
    [Tooltip("Publish IMU orientation, gyroscope, and accelerometer data")]
    public bool PublishIMU = false;
    
    [Tooltip("Publish depth sensor data")]
    public bool PublishDepth = false;
    
    [Tooltip("Publish hydrophone bearing data")]
    public bool PublishHydrophones = false;
    
    [Tooltip("Publish front camera images")]
    public bool PublishFrontCam = false;
    
    [Tooltip("Publish down camera images")]
    public bool PublishDownCam = false;
    
    [Tooltip("Stream ZED stereo camera via ZED SDK (disables front camera ROS publishing)")]
    public bool StreamZEDCamera = false;

    [Space(10)]
    [Header("Pinger Bearing Visualization")]
    [Tooltip("Show bearing visualization for pinger 1")]
    public bool VisualizeBearing1 = false;
    
    [Tooltip("Show bearing visualization for pinger 2")]
    public bool VisualizeBearing2 = false;
    
    [Tooltip("Show bearing visualization for pinger 3")]
    public bool VisualizeBearing3 = false;
    
    [Tooltip("Show bearing visualization for pinger 4")]
    public bool VisualizeBearing4 = false;

    [Space(10)]
    [Header("Camera Settings")]
    [Tooltip("Front camera publish rate (Hz)")]
    [Range(1, 60)]
    public int FrontCamRate = 10;
    
    [Tooltip("Down camera publish rate (Hz)")]
    [Range(1, 60)]
    public int DownCamRate = 10;
    
    [Space(5)]
    [Tooltip("Front camera resolution width (ZED X native: 960)")]
    [Range(320, 1920)]
    public int FrontCamWidth = 960;
    
    [Tooltip("Front camera resolution height (ZED X native: 600)")]
    [Range(240, 1200)]
    public int FrontCamHeight = 600;
    
    [Tooltip("Front camera vertical FOV in degrees (ZED X: 77.9°)")]
    [Range(30f, 120f)]
    public float FrontCamFOV = 77.9f;
    
    [Space(5)]
    [Tooltip("Down camera resolution width")]
    [Range(320, 1920)]
    public int DownCamWidth = 640;
    
    [Tooltip("Down camera resolution height")]
    [Range(240, 1080)]
    public int DownCamHeight = 480;

    [Space(10)]
    [Header("Other Settings")]
    [Tooltip("AUV state publish rate (Hz)")]
    [Range(10, 100)]
    public int PoseRate = 50;
    
    [Tooltip("Unity quality level (0=Very Low, 5=Very High)")]
    [Range(0, 5)]
    public int QualityLevel = 3;
    
    [Tooltip("Target framerate for physics simulation")]
    [Range(30, 120)]
    public int SimulationTargetFrameRate = 60;

    private bool isLoaded = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }
            
            // Always load settings to ensure we have valid data
            // In Edit Mode, this syncs Inspector with PlayerPrefs
            LoadSettings();
            isLoaded = true;
        }
        else
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
        }
        
        if (Application.isPlaying)
        {
            Application.targetFrameRate = SimulationTargetFrameRate;
        }
    }

    public void LoadSettings()
    {
        PublishROS = bool.Parse(PlayerPrefs.GetString("PublishROSToggle", "false"));
        DisplaySimObjects = bool.Parse(PlayerPrefs.GetString("DisplaySimToggle", "false"));
        HydrophonesNumberOption = PlayerPrefs.GetInt("HydrophonesNumberOption", 0);

        PublishDVL = bool.Parse(PlayerPrefs.GetString("PublishDVLToggle", "false"));
        PublishIMU = bool.Parse(PlayerPrefs.GetString("PublishIMUToggle", "false"));
        PublishDepth = bool.Parse(PlayerPrefs.GetString("PublishDepthToggle", "false"));
        PublishHydrophones = bool.Parse(PlayerPrefs.GetString("PublishHydrophonesToggle", "false"));
        PublishFrontCam = bool.Parse(PlayerPrefs.GetString("PublishFrontCamToggle", "false"));
        PublishDownCam = bool.Parse(PlayerPrefs.GetString("PublishDownCamToggle", "false"));
        StreamZEDCamera = bool.Parse(PlayerPrefs.GetString("StreamZEDCameraToggle", "false"));

        FrontCamRate = int.Parse(PlayerPrefs.GetString("frontCamRate", "10"));
        DownCamRate = int.Parse(PlayerPrefs.GetString("downCamRate", "10"));
        FrontCamWidth = int.Parse(PlayerPrefs.GetString("frontCamWidth", "960"));
        FrontCamHeight = int.Parse(PlayerPrefs.GetString("frontCamHeight", "600"));
        FrontCamFOV = float.Parse(PlayerPrefs.GetString("frontCamFOV", "77.9"));
        DownCamWidth = int.Parse(PlayerPrefs.GetString("downCamWidth", "640"));
        DownCamHeight = int.Parse(PlayerPrefs.GetString("downCamHeight", "480"));

        PoseRate = int.Parse(PlayerPrefs.GetString("poseRate", "50"));
        QualityLevel = int.Parse(PlayerPrefs.GetString("qualityLevel", "3"));
        
        QualitySettings.SetQualityLevel(QualityLevel);
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetString("PublishROSToggle", PublishROS.ToString());
        PlayerPrefs.SetString("DisplaySimToggle", DisplaySimObjects.ToString());
        PlayerPrefs.SetInt("HydrophonesNumberOption", HydrophonesNumberOption);

        PlayerPrefs.SetString("PublishDVLToggle", PublishDVL.ToString());
        PlayerPrefs.SetString("PublishIMUToggle", PublishIMU.ToString());
        PlayerPrefs.SetString("PublishDepthToggle", PublishDepth.ToString());
        PlayerPrefs.SetString("PublishHydrophonesToggle", PublishHydrophones.ToString());
        PlayerPrefs.SetString("PublishFrontCamToggle", PublishFrontCam.ToString());
        PlayerPrefs.SetString("PublishDownCamToggle", PublishDownCam.ToString());
        PlayerPrefs.SetString("StreamZEDCameraToggle", StreamZEDCamera.ToString());

        PlayerPrefs.SetString("frontCamRate", FrontCamRate.ToString());
        PlayerPrefs.SetString("downCamRate", DownCamRate.ToString());
        PlayerPrefs.SetString("frontCamWidth", FrontCamWidth.ToString());
        PlayerPrefs.SetString("frontCamHeight", FrontCamHeight.ToString());
        PlayerPrefs.SetString("frontCamFOV", FrontCamFOV.ToString());
        PlayerPrefs.SetString("downCamWidth", DownCamWidth.ToString());
        PlayerPrefs.SetString("downCamHeight", DownCamHeight.ToString());

        PlayerPrefs.SetString("poseRate", PoseRate.ToString());
        PlayerPrefs.SetString("qualityLevel", QualityLevel.ToString());

        PlayerPrefs.Save();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Automatically save settings to PlayerPrefs when changed in Inspector
        // This ensures that when we press Play, the LoadSettings() call in Awake
        // loads the values we just set in the Inspector.
        if (isLoaded)
        {
            SaveSettings();
        }
    }
#endif
}
