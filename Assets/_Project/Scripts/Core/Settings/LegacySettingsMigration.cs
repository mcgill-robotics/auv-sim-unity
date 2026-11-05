using UnityEngine;

/// <summary>
/// Older versions of the sim kept every setting in PlayerPrefs, as strings written with the
/// machine's culture. This imports those values once into the JSON settings and then deletes
/// the old keys, so people keep their setup after updating.
/// </summary>
public static class LegacySettingsMigration
{
    private static readonly string[] BoolKeys =
    {
        "PublishROSToggle", "DisplaySimToggle", "PublishGTObjectMap", "NoWaterModeToggle", "EnableShadowsToggle",
        "PublishDVLToggle", "PublishIMUToggle", "PublishDepthToggle", "PublishHydrophonesToggle",
        "PublishFrontCamToggle", "PublishDownCamToggle", "PublishDepthCameraToggle", "StreamZEDCameraToggle",
        "VisualizeDVLToggle", "VisualizeIMUToggle", "VisualizeDepthToggle",
        "EnableDVLNoiseToggle", "EnableIMUNoiseToggle", "EnableDepthNoiseToggle",
        "SimulateWaterRefraction", "UseJPEGCompression",
    };

    private static readonly string[] DrawerKeys =
    {
        "DrawerConfigOpen", "DrawerControlsOpen", "DrawerTelemetryOpen", "DrawerSensorsOpen", "DrawerCameraOpen",
    };

    private static readonly string[] OtherKeys =
    {
        "HydrophonesNumberOption", "frontCamRate", "downCamRate", "frontCamWidth", "frontCamHeight", "frontCamFOV",
        "WaterRefractionIndex", "downCamWidth", "downCamHeight", "downCamFOV", "downCamRefractionScale",
        "JPEGQuality", "poseRate", "qualityLevel", "SnapshotSavePath",
    };

    /// <summary>True if any of the old PlayerPrefs settings keys is present.</summary>
    public static bool HasLegacyKeys()
    {
        foreach (string k in BoolKeys) if (PlayerPrefs.HasKey(k)) return true;
        foreach (string k in DrawerKeys) if (PlayerPrefs.HasKey(k)) return true;
        foreach (string k in OtherKeys) if (PlayerPrefs.HasKey(k)) return true;
        return false;
    }

    /// <summary>
    /// Copies every old key that exists and parses cleanly into <paramref name="data"/> and
    /// <paramref name="layout"/>. Values that fail to parse are skipped, so the scene default stays.
    /// </summary>
    public static void Import(SimulationSettingsData data, HudLayoutState layout)
    {
        data.PublishROS = Bool("PublishROSToggle", data.PublishROS);
        data.PublishGTObjectMap = Bool("PublishGTObjectMap", data.PublishGTObjectMap);
        data.NoWaterMode = Bool("NoWaterModeToggle", data.NoWaterMode);
        data.EnableShadows = Bool("EnableShadowsToggle", data.EnableShadows);
        if (PlayerPrefs.HasKey("HydrophonesNumberOption"))
        {
            data.HydrophonesNumberOption = PlayerPrefs.GetInt("HydrophonesNumberOption", data.HydrophonesNumberOption);
        }

        data.PublishDVL = Bool("PublishDVLToggle", data.PublishDVL);
        data.PublishIMU = Bool("PublishIMUToggle", data.PublishIMU);
        data.PublishDepth = Bool("PublishDepthToggle", data.PublishDepth);
        data.PublishHydrophones = Bool("PublishHydrophonesToggle", data.PublishHydrophones);
        data.PublishFrontCam = Bool("PublishFrontCamToggle", data.PublishFrontCam);
        data.PublishDownCam = Bool("PublishDownCamToggle", data.PublishDownCam);
        data.PublishDepthCamera = Bool("PublishDepthCameraToggle", data.PublishDepthCamera);
        data.StreamZEDCamera = Bool("StreamZEDCameraToggle", data.StreamZEDCamera);

        data.VisualizeDVL = Bool("VisualizeDVLToggle", data.VisualizeDVL);
        data.VisualizeIMU = Bool("VisualizeIMUToggle", data.VisualizeIMU);
        data.VisualizeDepth = Bool("VisualizeDepthToggle", data.VisualizeDepth);

        data.EnableDVLNoise = Bool("EnableDVLNoiseToggle", data.EnableDVLNoise);
        data.EnableIMUNoise = Bool("EnableIMUNoiseToggle", data.EnableIMUNoise);
        data.EnableDepthNoise = Bool("EnableDepthNoiseToggle", data.EnableDepthNoise);

        data.FrontCamRate = Int("frontCamRate", data.FrontCamRate);
        data.DownCamRate = Int("downCamRate", data.DownCamRate);
        data.FrontCamWidth = Int("frontCamWidth", data.FrontCamWidth);
        data.FrontCamHeight = Int("frontCamHeight", data.FrontCamHeight);
        data.FrontCamFOV = Float("frontCamFOV", data.FrontCamFOV);
        data.SimulateWaterRefraction = Bool("SimulateWaterRefraction", data.SimulateWaterRefraction);
        data.WaterRefractionIndex = Float("WaterRefractionIndex", data.WaterRefractionIndex);
        data.DownCamWidth = Int("downCamWidth", data.DownCamWidth);
        data.DownCamHeight = Int("downCamHeight", data.DownCamHeight);
        data.DownCamFOV = Float("downCamFOV", data.DownCamFOV);
        data.DownCamRefractionScale = Float("downCamRefractionScale", data.DownCamRefractionScale);
        data.UseJPEGCompression = Bool("UseJPEGCompression", data.UseJPEGCompression);
        data.JPEGQuality = Int("JPEGQuality", data.JPEGQuality);

        data.PoseRate = Int("poseRate", data.PoseRate);
        data.QualityLevel = Int("qualityLevel", data.QualityLevel);
        if (PlayerPrefs.HasKey("SnapshotSavePath"))
        {
            data.SnapshotSavePath = PlayerPrefs.GetString("SnapshotSavePath", data.SnapshotSavePath);
        }

        if (layout != null)
        {
            layout.ConfigOpen = Bool("DrawerConfigOpen", layout.ConfigOpen);
            layout.ControlsOpen = Bool("DrawerControlsOpen", layout.ControlsOpen);
            layout.TelemetryOpen = Bool("DrawerTelemetryOpen", layout.TelemetryOpen);
            layout.SensorsOpen = Bool("DrawerSensorsOpen", layout.SensorsOpen);
            layout.CameraOpen = Bool("DrawerCameraOpen", layout.CameraOpen);
        }
    }

    /// <summary>Deletes the old settings keys. Key bindings and the clock state are left alone.</summary>
    public static void DeleteLegacyKeys()
    {
        foreach (string k in BoolKeys) PlayerPrefs.DeleteKey(k);
        foreach (string k in DrawerKeys) PlayerPrefs.DeleteKey(k);
        foreach (string k in OtherKeys) PlayerPrefs.DeleteKey(k);
        PlayerPrefs.Save();
    }

    private static bool Bool(string key, bool fallback)
    {
        if (!PlayerPrefs.HasKey(key)) return fallback;
        // bool.ToString() is "True"/"False" in every culture, so a plain TryParse is enough.
        return bool.TryParse(PlayerPrefs.GetString(key, string.Empty), out bool v) ? v : fallback;
    }

    private static int Int(string key, int fallback)
    {
        if (!PlayerPrefs.HasKey(key)) return fallback;
        return SettingsJson.TryParseInt(PlayerPrefs.GetString(key, string.Empty), out int v) ? v : fallback;
    }

    private static float Float(string key, float fallback)
    {
        if (!PlayerPrefs.HasKey(key)) return fallback;
        return SettingsJson.TryParseFloat(PlayerPrefs.GetString(key, string.Empty), out float v) ? v : fallback;
    }
}
