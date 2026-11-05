using System;
using System.IO;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Scene-wide simulation settings (sensor toggles, camera parameters, graphics options) plus the
/// main scene references (AUV, cameras) that other scripts read through <see cref="Instance"/>.
///
/// Where the values come from:
/// - Defaults are whatever is serialized on this component in the scene, so they live in git.
/// - A user can override them from the HUD. Pressing SAVE writes only the values that differ from
///   the defaults to <see cref="SettingsFilePath"/> (sim_settings.json in persistentDataPath).
/// - At startup (play mode only) the file is read and the keys it contains are applied on top of
///   the defaults. Nothing is loaded or saved in edit mode, so the scene file never drifts.
/// - "Reset to defaults" deletes the file and restores the values captured at startup.
/// </summary>
[DefaultExecutionOrder(-100)]
public class SimulationSettings : MonoBehaviour
{
    /// <summary>Value of <see cref="ScreenResolution"/> that means "use the display's resolution".</summary>
    public const string NativeResolution = "Native";

    private static SimulationSettings instance;

    /// <summary>
    /// The active settings. Set in Awake during play. In the editor outside play mode it falls back
    /// to the first SimulationSettings in the open scene, so editor tools can still read its values.
    /// </summary>
    public static SimulationSettings Instance
    {
        get
        {
#if UNITY_EDITOR
            if (instance == null && !Application.isPlaying)
            {
                return FindFirstObjectByType<SimulationSettings>();
            }
#endif
            return instance;
        }
        private set => instance = value;
    }

    /// <summary>Full path of the user overrides file.</summary>
    public static string SettingsFilePath => Path.Combine(Application.persistentDataPath, "sim_settings.json");

    [Header("AUV References")]
    [Tooltip("The AUV's Rigidbody - used by physics and sensor scripts")]
    public Rigidbody AUVRigidbody;

    [Tooltip("The AUV's root Transform")]
    public Transform AUVTransform;

    [Tooltip("Front camera (ZED left or mono front camera)")]
    public Camera FrontCamera;

    [Tooltip("Down-facing camera")]
    public Camera DownCamera;

    [Header("Global Toggles")]
    [Tooltip("Enable ROS TCP connection and publishing")]
    public bool PublishROS = true;

    // Nothing reads this any more; it is kept so existing scenes and scripts still compile and load.
    [HideInInspector]
    public bool DisplaySimObjects = false;

    [Tooltip("Publish the ground-truth object map on the vision object map topic. Turn this off when running the real vision stack, which publishes the same topic")]
    public bool PublishGTObjectMap = true;

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
    public bool PublishDVL = true;

    [Tooltip("Publish IMU orientation, gyroscope, and accelerometer data")]
    public bool PublishIMU = true;

    [Tooltip("Publish depth sensor data")]
    public bool PublishDepth = true;

    [Tooltip("Publish hydrophone bearing data")]
    public bool PublishHydrophones = false;

    [Tooltip("Publish front camera images")]
    public bool PublishFrontCam = false;

    [Tooltip("Publish down camera images")]
    public bool PublishDownCam = false;

    [Tooltip("Publish depth camera images (depth map from front camera)")]
    public bool PublishDepthCamera = false;

    [Tooltip("Stream ZED stereo camera via ZED SDK (disables front camera ROS publishing)")]
    public bool StreamZEDCamera = false;

    [Space(10)]
    [Header("Sensor Visualization")]
    [Tooltip("Show DVL beam visualization")]
    public bool VisualizeDVL = true;

    [Tooltip("Show IMU acceleration/angular velocity arrows")]
    public bool VisualizeIMU = true;

    [Tooltip("Show Depth sensor line")]
    public bool VisualizeDepth = true;

    [Space(10)]
    [Header("Sensor Noise Settings")]
    [Tooltip("Enable simulated noise/bias for DVL")]
    public bool EnableDVLNoise = true;

    [Tooltip("Enable simulated noise/bias for IMU")]
    public bool EnableIMUNoise = true;

    [Tooltip("Enable simulated noise/bias for Depth sensor")]
    public bool EnableDepthNoise = true;

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
    [Tooltip("Front camera resolution width (ZED 2i VGA: 672)")]
    [Range(320, 2208)]
    public int FrontCamWidth = 672;

    [Tooltip("Front camera resolution height (ZED 2i VGA: 376)")]
    [Range(240, 1242)]
    public int FrontCamHeight = 376;

    [Tooltip("Front camera vertical FOV in degrees (ZED 2i narrow: 40.9°)")]
    [Range(30f, 120f)]
    public float FrontCamFOV = 40.9f;

    [Space(5)]
    [Header("Underwater Optics")]
    [Tooltip("Simulate flat-port water refraction (Snell's Law) on ZED 2i and down cameras")]
    public bool SimulateWaterRefraction = true;

    [Tooltip("Index of refraction for water (standard water is ~1.33333)")]
    [Range(1.0f, 1.6f)]
    public float WaterRefractionIndex = 1.33333f;

    [Space(5)]
    [Tooltip("Down camera resolution width")]
    [Range(320, 1920)]
    public int DownCamWidth = 640;

    [Tooltip("Down camera resolution height")]
    [Range(240, 1080)]
    public int DownCamHeight = 480;

    [Tooltip("Down camera vertical FOV in degrees (ELP OV5640 60° diagonal lens: ~38.2°)")]
    [Range(20f, 120f)]
    public float DownCamFOV = 38.2f;

    [Tooltip("Down camera refraction scale / index for Snell's law (typically ~1.333, 1.0 = disabled)")]
    [Range(1.0f, 2.0f)]
    public float DownCamRefractionScale = 1.33333f;

    [Space(5)]
    [Tooltip("Use JPEG compression for camera images (reduces bandwidth ~10x)")]
    public bool UseJPEGCompression = true;

    [Tooltip("JPEG compression quality (1-100, higher = better quality but larger size)")]
    [Range(1, 100)]
    public int JPEGQuality = 75;

    [Space(10)]
    [Header("Other Settings")]
    [Tooltip("AUV state publish rate (Hz)")]
    [Range(10, 100)]
    public int PoseRate = 50;

    [Tooltip("Index into Project Settings > Quality (QualitySettings.names). 0 is the first, highest-fidelity level; clamped to the levels that exist")]
    [Min(0)]
    public int QualityLevel = 3;

    [Tooltip("Application.targetFrameRate, applied at startup")]
    [Range(30, 300)]
    public int SimulationTargetFrameRate = 60;

    [Tooltip("Window resolution as WIDTHxHEIGHT (for example 1920x1080), or Native for the display's own resolution")]
    public string ScreenResolution = NativeResolution;

    [Space(10)]
    [Header("Snapshot Settings")]
    [Tooltip("Folder for camera snapshots. Empty means <project>/Recordings; falls back to persistentDataPath if it is not writable")]
    public string SnapshotSavePath;

    // Copy of the scene values taken in Awake, before any user overrides were applied.
    private SimulationSettingsData sceneDefaults;

    /// <summary>True when a HUD change has been applied but not yet saved with SAVE.</summary>
    public bool HasUnsavedChanges { get; private set; }

    /// <summary>Raised when <see cref="HasUnsavedChanges"/> flips.</summary>
    public event Action<bool> UnsavedChangesChanged;

    /// <summary>Raised after the values were replaced wholesale (load or reset), so the HUD can refresh.</summary>
    public event Action SettingsReloaded;

    /// <summary>Config drawer open state. Drawer state now lives in <see cref="HudLayoutState"/>.</summary>
    [Obsolete("Drawer state moved to HudLayoutState.Current.ConfigOpen")]
    public bool DrawerConfigOpen { get => HudLayoutState.Current.ConfigOpen; set => HudLayoutState.Current.ConfigOpen = value; }

    /// <summary>Controls drawer open state. Drawer state now lives in <see cref="HudLayoutState"/>.</summary>
    [Obsolete("Drawer state moved to HudLayoutState.Current.ControlsOpen")]
    public bool DrawerControlsOpen { get => HudLayoutState.Current.ControlsOpen; set => HudLayoutState.Current.ControlsOpen = value; }

    /// <summary>Telemetry drawer open state. Drawer state now lives in <see cref="HudLayoutState"/>.</summary>
    [Obsolete("Drawer state moved to HudLayoutState.Current.TelemetryOpen")]
    public bool DrawerTelemetryOpen { get => HudLayoutState.Current.TelemetryOpen; set => HudLayoutState.Current.TelemetryOpen = value; }

    /// <summary>Sensors drawer open state. Drawer state now lives in <see cref="HudLayoutState"/>.</summary>
    [Obsolete("Drawer state moved to HudLayoutState.Current.SensorsOpen")]
    public bool DrawerSensorsOpen { get => HudLayoutState.Current.SensorsOpen; set => HudLayoutState.Current.SensorsOpen = value; }

    /// <summary>Camera drawer open state. Drawer state now lives in <see cref="HudLayoutState"/>.</summary>
    [Obsolete("Drawer state moved to HudLayoutState.Current.CameraOpen")]
    public bool DrawerCameraOpen { get => HudLayoutState.Current.CameraOpen; set => HudLayoutState.Current.CameraOpen = value; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            // Only remove this component: the GameObject also carries ROSSettings, InputManager, etc.
            Debug.LogWarning("[SimulationSettings] A second SimulationSettings was found; removing the duplicate component.");
            Destroy(this);
            return;
        }
        Instance = this;

        sceneDefaults = SimulationSettingsData.Capture(this);

        MigrateLegacyPlayerPrefs();
        LoadSettings();

        Application.targetFrameRate = SimulationTargetFrameRate;
        // At startup only an explicit size is applied; "Native" leaves the player's window alone.
        if (TryParseResolution(ScreenResolution, out _, out _))
        {
            ApplyScreenResolution();
        }
    }

    private void OnDestroy()
    {
        if (instance == this) Instance = null;
    }

    private void Start()
    {
        if (NoWaterMode)
        {
            ApplyNoWaterMode();
        }
    }

    /// <summary>
    /// Applies No Water Mode - can be called at runtime from UI.
    /// </summary>
    public void ApplyNoWaterMode()
    {
        if (waterSurfaceObject != null)
        {
            waterSurfaceObject.SetActive(!NoWaterMode);
        }

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

    /// <summary>Clamps <see cref="QualityLevel"/> to the existing quality levels and applies it.</summary>
    public void ApplyQualityLevel()
    {
        QualityLevel = ClampQualityLevel(QualityLevel);
        QualitySettings.SetQualityLevel(QualityLevel);
    }

    /// <summary>
    /// Applies <see cref="ScreenResolution"/>. "Native" restores the display's own resolution.
    /// </summary>
    public void ApplyScreenResolution()
    {
        if (TryParseResolution(ScreenResolution, out int width, out int height))
        {
            Screen.SetResolution(width, height, Screen.fullScreenMode);
        }
        else if (Screen.width != Display.main.systemWidth || Screen.height != Display.main.systemHeight)
        {
            Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, Screen.fullScreenMode);
        }
    }

    /// <summary>Parses "WIDTHxHEIGHT". Returns false for "Native" or anything malformed.</summary>
    public static bool TryParseResolution(string text, out int width, out int height)
    {
        width = height = 0;
        if (string.IsNullOrEmpty(text)) return false;
        string[] parts = text.Split('x');
        return parts.Length == 2
            && SettingsJson.TryParseInt(parts[0], out width)
            && SettingsJson.TryParseInt(parts[1], out height)
            && width > 0 && height > 0;
    }

    /// <summary>Records that a value was changed from the HUD and not saved yet.</summary>
    public void MarkUnsaved()
    {
        SetUnsaved(true);
    }

    private void SetUnsaved(bool unsaved)
    {
        if (HasUnsavedChanges == unsaved) return;
        HasUnsavedChanges = unsaved;
        UnsavedChangesChanged?.Invoke(unsaved);
    }

    /// <summary>
    /// Restores the scene defaults, then applies the keys present in the user overrides file.
    /// Called once in Awake; only does anything in play mode.
    /// </summary>
    public void LoadSettings()
    {
        if (!Application.isPlaying || sceneDefaults == null) return;

        SimulationSettingsData data = sceneDefaults.Clone();
        string path = SettingsFilePath;
        if (File.Exists(path))
        {
            try
            {
                // FromJsonOverwrite only touches the keys that are in the file.
                JsonUtility.FromJsonOverwrite(File.ReadAllText(path), data);
                Debug.Log($"[SimulationSettings] Loaded user settings from {path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SimulationSettings] Could not read '{path}': {e.Message}. Using scene defaults.");
                data = sceneDefaults.Clone();
            }
        }
        data.ApplyTo(this);

        ApplyLoadedValues();
        SetUnsaved(false);
        SettingsReloaded?.Invoke();
    }

    /// <summary>
    /// Writes the values that differ from the scene defaults to <see cref="SettingsFilePath"/>.
    /// Only the HUD's SAVE button should call this.
    /// </summary>
    public void SaveSettings()
    {
        if (!Application.isPlaying || sceneDefaults == null) return;

        string path = SettingsFilePath;
        try
        {
            string json = SettingsJson.ToOverridesJson(SimulationSettingsData.Capture(this), sceneDefaults);
            File.WriteAllText(path, json);
            SetUnsaved(false);
            Debug.Log($"[SimulationSettings] Saved user settings to {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SimulationSettings] Could not write '{path}': {e.Message}");
        }
    }

    /// <summary>
    /// Deletes the user overrides file and restores the values serialized in the scene.
    /// </summary>
    public void ResetToDefaults()
    {
        if (!Application.isPlaying || sceneDefaults == null) return;

        string path = SettingsFilePath;
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SimulationSettings] Could not delete '{path}': {e.Message}");
        }

        sceneDefaults.Clone().ApplyTo(this);
        ApplyLoadedValues();
        ApplyNoWaterMode();
        ApplyScreenResolution();
        SetUnsaved(false);
        SettingsReloaded?.Invoke();
    }

    /// <summary>
    /// Returns the [Range] limits declared on a settings field, so the HUD can validate input
    /// against the same numbers the Inspector uses.
    /// </summary>
    public static bool TryGetRange(string fieldName, out float min, out float max)
    {
        min = float.MinValue;
        max = float.MaxValue;
        FieldInfo field = typeof(SimulationSettings).GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
        var range = field?.GetCustomAttribute<RangeAttribute>();
        if (range == null) return false;
        min = range.min;
        max = range.max;
        return true;
    }

    private void ApplyLoadedValues()
    {
        // ZED streaming needs the ZED SDK, which only exists on Windows and Linux.
#if !UNITY_EDITOR_LINUX && !UNITY_STANDALONE_LINUX && !UNITY_EDITOR_WIN && !UNITY_STANDALONE_WIN
        StreamZEDCamera = false;
#endif
        SnapshotSavePath = ValidateAndGetSavePath(SnapshotSavePath);
        ApplyQualityLevel();
        ApplyShadows();
    }

    private void MigrateLegacyPlayerPrefs()
    {
        if (File.Exists(SettingsFilePath) || !LegacySettingsMigration.HasLegacyKeys()) return;

        SimulationSettingsData data = sceneDefaults.Clone();
        HudLayoutState layout = HudLayoutState.FileExists ? null : HudLayoutState.Current;
        LegacySettingsMigration.Import(data, layout);

        try
        {
            File.WriteAllText(SettingsFilePath, SettingsJson.ToOverridesJson(data, sceneDefaults));
            if (layout != null) HudLayoutState.Save();
            LegacySettingsMigration.DeleteLegacyKeys();
            Debug.Log($"[SimulationSettings] Imported old PlayerPrefs settings into {SettingsFilePath}");
        }
        catch (Exception e)
        {
            // Keep the old keys so the import can be retried next time.
            Debug.LogError($"[SimulationSettings] Could not import old settings: {e.Message}");
        }
    }

    private static int ClampQualityLevel(int level)
    {
        int count = QualitySettings.names.Length;
        return count == 0 ? 0 : Mathf.Clamp(level, 0, count - 1);
    }

    /// <summary>
    /// Returns <paramref name="path"/> if it can be created and written to, otherwise a folder
    /// under persistentDataPath. An empty path means &lt;project&gt;/Recordings.
    /// </summary>
    public string ValidateAndGetSavePath(string path)
    {
        string validPath = path;
        bool useFallback = false;

        if (string.IsNullOrWhiteSpace(validPath))
        {
            validPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Recordings"));
        }

        try
        {
            if (!Directory.Exists(validPath))
            {
                Directory.CreateDirectory(validPath);
            }

            // Test write permission by creating a temporary file
            string testFile = Path.Combine(validPath, ".test_write_perm");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SimulationSettings] Cannot write to configured path '{validPath}': {e.Message}. Falling back to persistentDataPath.");
            useFallback = true;
        }

        if (useFallback)
        {
            validPath = Path.Combine(Application.persistentDataPath, "Recordings");
            try
            {
                if (!Directory.Exists(validPath))
                {
                    Directory.CreateDirectory(validPath);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SimulationSettings] Failed to create fallback directory '{validPath}': {e.Message}");
            }
        }

        return validPath;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Only keeps the Inspector value in range; nothing is saved from edit mode.
        QualityLevel = ClampQualityLevel(QualityLevel);
    }
#endif
}
