using UnityEngine;

public class SimulationSettings : MonoBehaviour
{
    public static SimulationSettings Instance { get; private set; }

    [Header("Global Toggles")]
    public bool PublishROS = false;
    public bool DisplaySimObjects = false;
    public int HydrophonesNumberOption = 0;

    [Header("Publisher Toggles")]
    public bool PublishDVL = false;
    public bool PublishIMU = false;
    public bool PublishDepth = false;
    public bool PublishHydrophones = false;
    public bool PublishFrontCam = false;
    public bool PublishDownCam = false;
    public bool StreamZEDCamera = false;

    [Header("Visualization Toggles")]
    public bool VisualizeBearing1 = false;
    public bool VisualizeBearing2 = false;
    public bool VisualizeBearing3 = false;
    public bool VisualizeBearing4 = false;

    [Header("Camera Settings")]
    public int FrontCamRate = 10;
    public int DownCamRate = 10;
    // ZED X native resolution (16:10 aspect ratio)
    public int FrontCamWidth = 960;
    public int FrontCamHeight = 600;
    public float FrontCamFOV = 77.9f; // ZED X vertical FOV
    public int DownCamWidth = 640;
    public int DownCamHeight = 480;

    [Header("Other Settings")]
    public int PoseRate = 50;
    public int QualityLevel = 3;



    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Always load settings to ensure we have valid data
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
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
        SaveSettings();
    }
#endif
}
