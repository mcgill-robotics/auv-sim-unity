using UnityEngine;

[ExecuteAlways]
[DefaultExecutionOrder(-100)]
public class SimulationSettings : MonoBehaviour
{
    public static SimulationSettings Instance { get; private set; }

    [Header("Global Toggles")]
    [Tooltip("Enable ROS TCP connection and publishing")]
    public bool PublishROS = false;
    
    [Tooltip("Show simulation objects (gates, buoys, etc.) in scene")]
    public bool DisplaySimObjects = false;
    
    [Tooltip("Disable water volume and rendering for performance (when vision isn't needed)")]
    public bool NoWaterMode = false;

    [Tooltip("Enable or disable shadows for better performance")]
    public bool EnableShadows = true;

    [Tooltip("The main directional light (Sun) to control shadows for")]
    public Light sunLight;
    
    [Tooltip("Water surface GameObject to disable in No Water Mode")]
    public GameObject waterSurfaceObject;
    
    [Tooltip("Optional: Pool post-processing volume to disable in No Water Mode")]
    public GameObject poolPostProcessingVolume;
    
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
    
    [Tooltip("Publish pressure sensor data")]
    public bool PublishPressure = false;
    
    [Tooltip("Publish front camera images")]
    public bool PublishFrontCam = false;
    
    [Tooltip("Publish down camera images")]
    public bool PublishDownCam = false;
    
    [Tooltip("Stream ZED stereo camera via ZED SDK (disables front camera ROS publishing)")]
    public bool StreamZEDCamera = false;
    
    [Space(10)]
    [Header("Sensor Visualization")]
    [Tooltip("Show DVL beam visualization")]
    public bool VisualizeDVL = true;
    
    [Tooltip("Show IMU acceleration/angular velocity arrows")]
    public bool VisualizeIMU = true;
    
    [Tooltip("Show Pressure sensor depth line")]
    public bool VisualizePressure = true;
    
    [Space(10)]
    [Header("UI Drawer States")]
    [Tooltip("Config drawer open state")]
    public bool DrawerConfigOpen = true;
    
    [Tooltip("Controls drawer open state")]
    public bool DrawerControlsOpen = true;
    
    [Tooltip("Telemetry drawer open state")]
    public bool DrawerTelemetryOpen = true;
    
    [Tooltip("Sensors drawer open state")]
    public bool DrawerSensorsOpen = true;
    
    [Tooltip("Camera drawer open state")]
    public bool DrawerCameraOpen = true;

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
    [Range(30, 300)]
    public int SimulationTargetFrameRate = 60;

    [Space(10)]
    [Header("Snapshot Settings")]
    [Tooltip("Path to save camera snapshots")]
    public string SnapshotSavePath;

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
    
    private void Start()
    {
        if (Application.isPlaying && NoWaterMode)
        {
            ApplyNoWaterMode();
        }
    }
    
    /// <summary>
    /// Applies No Water Mode - can be called at runtime from UI.
    /// </summary>
    public void ApplyNoWaterMode()
    {
        // Disable/Enable assigned water surface object
        if (waterSurfaceObject != null)
        {
            waterSurfaceObject.SetActive(!NoWaterMode);
        }
        
        // Disable/Enable pool post-processing volume
        if (poolPostProcessingVolume != null)
        {
            poolPostProcessingVolume.SetActive(!NoWaterMode);
        }
        
        if (NoWaterMode)
        {
            Debug.Log("[SimulationSettings] No Water Mode enabled");
        }
    }

    /// <summary>
    /// Applies shadow settings - called at runtime.
    /// </summary>
    public void ApplyShadows()
    {
        if (sunLight != null)
        {
            sunLight.shadows = EnableShadows ? LightShadows.Soft : LightShadows.None;
        }
    }

    public void LoadSettings()
    {
        PublishROS = bool.Parse(PlayerPrefs.GetString("PublishROSToggle", "false"));
        DisplaySimObjects = bool.Parse(PlayerPrefs.GetString("DisplaySimToggle", "false"));
        NoWaterMode = bool.Parse(PlayerPrefs.GetString("NoWaterModeToggle", "false"));
        EnableShadows = bool.Parse(PlayerPrefs.GetString("EnableShadowsToggle", "true"));
        HydrophonesNumberOption = PlayerPrefs.GetInt("HydrophonesNumberOption", 0);

        PublishDVL = bool.Parse(PlayerPrefs.GetString("PublishDVLToggle", "false"));
        PublishIMU = bool.Parse(PlayerPrefs.GetString("PublishIMUToggle", "false"));
        PublishDepth = bool.Parse(PlayerPrefs.GetString("PublishDepthToggle", "false"));
        PublishPressure = bool.Parse(PlayerPrefs.GetString("PublishPressureToggle", "false"));
        PublishHydrophones = bool.Parse(PlayerPrefs.GetString("PublishHydrophonesToggle", "false"));
        PublishFrontCam = bool.Parse(PlayerPrefs.GetString("PublishFrontCamToggle", "false"));
        PublishDownCam = bool.Parse(PlayerPrefs.GetString("PublishDownCamToggle", "false"));
        StreamZEDCamera = bool.Parse(PlayerPrefs.GetString("StreamZEDCameraToggle", "false"));

        VisualizeDVL = bool.Parse(PlayerPrefs.GetString("VisualizeDVLToggle", "true"));
        VisualizeIMU = bool.Parse(PlayerPrefs.GetString("VisualizeIMUToggle", "true"));
        VisualizePressure = bool.Parse(PlayerPrefs.GetString("VisualizePressureToggle", "true"));

        DrawerConfigOpen = bool.Parse(PlayerPrefs.GetString("DrawerConfigOpen", "true"));
        DrawerControlsOpen = bool.Parse(PlayerPrefs.GetString("DrawerControlsOpen", "true"));
        DrawerTelemetryOpen = bool.Parse(PlayerPrefs.GetString("DrawerTelemetryOpen", "true"));
        DrawerSensorsOpen = bool.Parse(PlayerPrefs.GetString("DrawerSensorsOpen", "true"));
        DrawerCameraOpen = bool.Parse(PlayerPrefs.GetString("DrawerCameraOpen", "true"));

        FrontCamRate = int.Parse(PlayerPrefs.GetString("frontCamRate", "10"));
        DownCamRate = int.Parse(PlayerPrefs.GetString("downCamRate", "10"));
        FrontCamWidth = int.Parse(PlayerPrefs.GetString("frontCamWidth", "960"));
        FrontCamHeight = int.Parse(PlayerPrefs.GetString("frontCamHeight", "600"));
        FrontCamFOV = float.Parse(PlayerPrefs.GetString("frontCamFOV", "77.9"));
        DownCamWidth = int.Parse(PlayerPrefs.GetString("downCamWidth", "640"));
        DownCamHeight = int.Parse(PlayerPrefs.GetString("downCamHeight", "480"));

        PoseRate = int.Parse(PlayerPrefs.GetString("poseRate", "50"));
        QualityLevel = int.Parse(PlayerPrefs.GetString("qualityLevel", "3"));
        
        string defaultPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Recordings"));
        SnapshotSavePath = PlayerPrefs.GetString("SnapshotSavePath", defaultPath);
        
        // Validate and possibly correct the path
        SnapshotSavePath = ValidateAndGetSavePath(SnapshotSavePath);
        
        QualitySettings.SetQualityLevel(QualityLevel);
        ApplyShadows();
    }

    public string ValidateAndGetSavePath(string path)
    {
        string validPath = path;
        bool useFallback = false;

        if (string.IsNullOrWhiteSpace(validPath))
        {
            validPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Recordings"));
        }

        try
        {
            if (!System.IO.Directory.Exists(validPath))
            {
                System.IO.Directory.CreateDirectory(validPath);
            }

            // Test write permission by creating a temporary file
            string testFile = System.IO.Path.Combine(validPath, ".test_write_perm");
            System.IO.File.WriteAllText(testFile, "test");
            System.IO.File.Delete(testFile);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SimulationSettings] Cannot write to configured path '{validPath}': {e.Message}. Falling back to persistentDataPath.");
            useFallback = true;
        }

        if (useFallback)
        {
            validPath = System.IO.Path.Combine(Application.persistentDataPath, "Recordings");
            try
            {
                if (!System.IO.Directory.Exists(validPath))
                {
                    System.IO.Directory.CreateDirectory(validPath);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SimulationSettings] Failed to create fallback directory '{validPath}': {e.Message}");
            }
        }

        return validPath;
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetString("PublishROSToggle", PublishROS.ToString());
        PlayerPrefs.SetString("DisplaySimToggle", DisplaySimObjects.ToString());
        PlayerPrefs.SetString("NoWaterModeToggle", NoWaterMode.ToString());
        PlayerPrefs.SetString("EnableShadowsToggle", EnableShadows.ToString());
        PlayerPrefs.SetInt("HydrophonesNumberOption", HydrophonesNumberOption);

        PlayerPrefs.SetString("PublishDVLToggle", PublishDVL.ToString());
        PlayerPrefs.SetString("PublishIMUToggle", PublishIMU.ToString());
        PlayerPrefs.SetString("PublishDepthToggle", PublishDepth.ToString());
        PlayerPrefs.SetString("PublishPressureToggle", PublishPressure.ToString());
        PlayerPrefs.SetString("PublishHydrophonesToggle", PublishHydrophones.ToString());
        PlayerPrefs.SetString("PublishFrontCamToggle", PublishFrontCam.ToString());
        PlayerPrefs.SetString("PublishDownCamToggle", PublishDownCam.ToString());
        PlayerPrefs.SetString("StreamZEDCameraToggle", StreamZEDCamera.ToString());

        PlayerPrefs.SetString("VisualizeDVLToggle", VisualizeDVL.ToString());
        PlayerPrefs.SetString("VisualizeIMUToggle", VisualizeIMU.ToString());
        PlayerPrefs.SetString("VisualizePressureToggle", VisualizePressure.ToString());

        PlayerPrefs.SetString("DrawerConfigOpen", DrawerConfigOpen.ToString());
        PlayerPrefs.SetString("DrawerControlsOpen", DrawerControlsOpen.ToString());
        PlayerPrefs.SetString("DrawerTelemetryOpen", DrawerTelemetryOpen.ToString());
        PlayerPrefs.SetString("DrawerSensorsOpen", DrawerSensorsOpen.ToString());
        PlayerPrefs.SetString("DrawerCameraOpen", DrawerCameraOpen.ToString());

        PlayerPrefs.SetString("frontCamRate", FrontCamRate.ToString());
        PlayerPrefs.SetString("downCamRate", DownCamRate.ToString());
        PlayerPrefs.SetString("frontCamWidth", FrontCamWidth.ToString());
        PlayerPrefs.SetString("frontCamHeight", FrontCamHeight.ToString());
        PlayerPrefs.SetString("frontCamFOV", FrontCamFOV.ToString());
        PlayerPrefs.SetString("downCamWidth", DownCamWidth.ToString());
        PlayerPrefs.SetString("downCamHeight", DownCamHeight.ToString());

        PlayerPrefs.SetString("poseRate", PoseRate.ToString());
        PlayerPrefs.SetString("qualityLevel", QualityLevel.ToString());
        PlayerPrefs.SetString("SnapshotSavePath", SnapshotSavePath);

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
            ApplyShadows();
        }
    }
#endif
}
