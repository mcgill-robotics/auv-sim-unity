using System;

/// <summary>
/// The user-tunable part of <see cref="SimulationSettings"/>, as a plain serializable class so it
/// can be written to and read from JSON with JsonUtility.
///
/// Field names must match the <see cref="SimulationSettings"/> fields they mirror, because they are
/// also the JSON keys in sim_settings.json. Keep this class flat (only bool, int, float and string
/// fields): <see cref="SettingsJson.ToOverridesJson"/> relies on one field per line.
/// Scene references (Rigidbody, cameras, lights) are never stored here.
/// </summary>
[Serializable]
public class SimulationSettingsData
{
    public bool PublishROS;
    public bool PublishGTObjectMap;
    public bool NoWaterMode;
    public bool EnableShadows;
    public int HydrophonesNumberOption;

    public bool PublishDVL;
    public bool PublishIMU;
    public bool PublishDepth;
    public bool PublishHydrophones;
    public bool PublishFrontCam;
    public bool PublishDownCam;
    public bool PublishDepthCamera;
    public bool StreamZEDCamera;

    public bool VisualizeDVL;
    public bool VisualizeIMU;
    public bool VisualizeDepth;

    public bool EnableDVLNoise;
    public bool EnableIMUNoise;
    public bool EnableDepthNoise;

    public int FrontCamRate;
    public int DownCamRate;
    public int FrontCamWidth;
    public int FrontCamHeight;
    public float FrontCamFOV;
    public bool SimulateWaterRefraction;
    public float WaterRefractionIndex;
    public int DownCamWidth;
    public int DownCamHeight;
    public float DownCamFOV;
    public float DownCamRefractionScale;
    public bool UseJPEGCompression;
    public int JPEGQuality;

    public int PoseRate;
    public int QualityLevel;
    public int SimulationTargetFrameRate;
    public string ScreenResolution;
    public string SnapshotSavePath;

    /// <summary>Copies the current values out of a settings component.</summary>
    public static SimulationSettingsData Capture(SimulationSettings s)
    {
        return new SimulationSettingsData
        {
            PublishROS = s.PublishROS,
            PublishGTObjectMap = s.PublishGTObjectMap,
            NoWaterMode = s.NoWaterMode,
            EnableShadows = s.EnableShadows,
            HydrophonesNumberOption = s.HydrophonesNumberOption,

            PublishDVL = s.PublishDVL,
            PublishIMU = s.PublishIMU,
            PublishDepth = s.PublishDepth,
            PublishHydrophones = s.PublishHydrophones,
            PublishFrontCam = s.PublishFrontCam,
            PublishDownCam = s.PublishDownCam,
            PublishDepthCamera = s.PublishDepthCamera,
            StreamZEDCamera = s.StreamZEDCamera,

            VisualizeDVL = s.VisualizeDVL,
            VisualizeIMU = s.VisualizeIMU,
            VisualizeDepth = s.VisualizeDepth,

            EnableDVLNoise = s.EnableDVLNoise,
            EnableIMUNoise = s.EnableIMUNoise,
            EnableDepthNoise = s.EnableDepthNoise,

            FrontCamRate = s.FrontCamRate,
            DownCamRate = s.DownCamRate,
            FrontCamWidth = s.FrontCamWidth,
            FrontCamHeight = s.FrontCamHeight,
            FrontCamFOV = s.FrontCamFOV,
            SimulateWaterRefraction = s.SimulateWaterRefraction,
            WaterRefractionIndex = s.WaterRefractionIndex,
            DownCamWidth = s.DownCamWidth,
            DownCamHeight = s.DownCamHeight,
            DownCamFOV = s.DownCamFOV,
            DownCamRefractionScale = s.DownCamRefractionScale,
            UseJPEGCompression = s.UseJPEGCompression,
            JPEGQuality = s.JPEGQuality,

            PoseRate = s.PoseRate,
            QualityLevel = s.QualityLevel,
            SimulationTargetFrameRate = s.SimulationTargetFrameRate,
            ScreenResolution = s.ScreenResolution,
            SnapshotSavePath = s.SnapshotSavePath,
        };
    }

    /// <summary>Writes these values back into a settings component.</summary>
    public void ApplyTo(SimulationSettings s)
    {
        s.PublishROS = PublishROS;
        s.PublishGTObjectMap = PublishGTObjectMap;
        s.NoWaterMode = NoWaterMode;
        s.EnableShadows = EnableShadows;
        s.HydrophonesNumberOption = HydrophonesNumberOption;

        s.PublishDVL = PublishDVL;
        s.PublishIMU = PublishIMU;
        s.PublishDepth = PublishDepth;
        s.PublishHydrophones = PublishHydrophones;
        s.PublishFrontCam = PublishFrontCam;
        s.PublishDownCam = PublishDownCam;
        s.PublishDepthCamera = PublishDepthCamera;
        s.StreamZEDCamera = StreamZEDCamera;

        s.VisualizeDVL = VisualizeDVL;
        s.VisualizeIMU = VisualizeIMU;
        s.VisualizeDepth = VisualizeDepth;

        s.EnableDVLNoise = EnableDVLNoise;
        s.EnableIMUNoise = EnableIMUNoise;
        s.EnableDepthNoise = EnableDepthNoise;

        s.FrontCamRate = FrontCamRate;
        s.DownCamRate = DownCamRate;
        s.FrontCamWidth = FrontCamWidth;
        s.FrontCamHeight = FrontCamHeight;
        s.FrontCamFOV = FrontCamFOV;
        s.SimulateWaterRefraction = SimulateWaterRefraction;
        s.WaterRefractionIndex = WaterRefractionIndex;
        s.DownCamWidth = DownCamWidth;
        s.DownCamHeight = DownCamHeight;
        s.DownCamFOV = DownCamFOV;
        s.DownCamRefractionScale = DownCamRefractionScale;
        s.UseJPEGCompression = UseJPEGCompression;
        s.JPEGQuality = JPEGQuality;

        s.PoseRate = PoseRate;
        s.QualityLevel = QualityLevel;
        s.SimulationTargetFrameRate = SimulationTargetFrameRate;
        s.ScreenResolution = ScreenResolution;
        s.SnapshotSavePath = SnapshotSavePath;
    }

    /// <summary>Returns an independent copy (used to keep the scene defaults untouched).</summary>
    public SimulationSettingsData Clone() => (SimulationSettingsData)MemberwiseClone();
}
