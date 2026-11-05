/// <summary>
/// Element names used in SimulatorHUD.uxml. Every controller queries elements through these
/// constants, so renaming an element means changing it here and in the UXML only.
/// </summary>
public static class HudIds
{
    // Drawers
    public const string ConfigDrawer = "ConfigDrawer";
    public const string ControlsDrawer = "ControlsDrawer";
    public const string TelemetryDrawer = "TelemetryDrawer";
    public const string SensorsDrawer = "SensorsDrawer";
    public const string CameraDrawer = "CameraDrawer";
    public const string TabSuffix = "-Tab";

    // Log panel
    public const string LogPanel = "LogPanel";
    public const string LogToggle = "LogPanel-Toggle";
    public const string LogScroll = "LogScroll";
    public const string LogText = "Text-Log";

    // Config drawer
    public const string ToggleWater = "Toggle-Water";
    public const string ToggleShadows = "Toggle-Shadows";
    public const string DropdownQuality = "Dropdown-Quality";
    public const string DropdownScreenRes = "Dropdown-ScreenRes";
    public const string TogglePublishROS = "Toggle-PublishROS";
    public const string ToggleGTObjectMap = "Toggle-GTObjectMap";
    public const string ToggleStreamZED = "Toggle-StreamZED";
    public const string ToggleFreeCamera = "Toggle-FreeCamera";
    public const string ToggleDVL = "Toggle-DVL";
    public const string ToggleIMU = "Toggle-IMU";
    public const string ToggleDepth = "Toggle-Depth";
    public const string ToggleHydro = "Toggle-Hydro";
    public const string ToggleFrontCam = "Toggle-FrontCam";
    public const string ToggleDownCam = "Toggle-DownCam";
    public const string ToggleDepthCamera = "Toggle-DepthCamera";
    public const string InputFrontRate = "Input-FrontRate";
    public const string InputDownRate = "Input-DownRate";
    public const string InputFrontW = "Input-FrontW";
    public const string InputFrontH = "Input-FrontH";
    public const string InputDownW = "Input-DownW";
    public const string InputDownH = "Input-DownH";
    public const string BtnSave = "Btn-Save";
    public const string BtnResetDefaults = "Btn-ResetDefaults";

    // Telemetry drawer
    public const string TextPosX = "Text-PosX";
    public const string TextPosY = "Text-PosY";
    public const string TextPosZ = "Text-PosZ";
    public const string TextRotX = "Text-RotX";
    public const string TextRotY = "Text-RotY";
    public const string TextRotZ = "Text-RotZ";

    // Sensors drawer
    public const string LabelDVL = "Label-DVL";
    public const string TextDVLVx = "Text-DVLVx";
    public const string TextDVLVy = "Text-DVLVy";
    public const string TextDVLVz = "Text-DVLVz";
    public const string TextDVLAlt = "Text-DVLAlt";
    public const string TextDVLLock = "Text-DVLLock";
    public const string TextDVLPosX = "Text-DVLPosX";
    public const string TextDVLPosY = "Text-DVLPosY";
    public const string TextDVLPosZ = "Text-DVLPosZ";
    public const string ToggleDVLViz = "Toggle-DVLViz";
    public const string ToggleDVLNoise = "Toggle-DVLNoise";
    public const string LabelIMU = "Label-IMU";
    public const string TextIMUAx = "Text-IMUAx";
    public const string TextIMUAy = "Text-IMUAy";
    public const string TextIMUAz = "Text-IMUAz";
    public const string TextIMUWx = "Text-IMUWx";
    public const string TextIMUWy = "Text-IMUWy";
    public const string TextIMUWz = "Text-IMUWz";
    public const string TextIMURoll = "Text-IMURoll";
    public const string TextIMUPitch = "Text-IMUPitch";
    public const string TextIMUYaw = "Text-IMUYaw";
    public const string ToggleIMUViz = "Toggle-IMUViz";
    public const string ToggleIMUNoise = "Toggle-IMUNoise";
    public const string LabelDepth = "Label-Depth";
    public const string TextDepthValue = "Text-DepthValue";
    public const string ToggleDepthViz = "Toggle-DepthViz";
    public const string ToggleDepthNoise = "Toggle-DepthNoise";

    // Camera drawer (used by CameraFeedController)
    public const string DropdownCamTopic = "Dropdown-CamTopic";
    public const string BtnFullscreen = "Btn-Fullscreen";
    public const string BtnSaveImage = "Btn-SaveImage";
    public const string CameraFeedImage = "CameraFeedImage";
    public const string FullscreenCameraBackground = "FullscreenCameraBackground";
}
