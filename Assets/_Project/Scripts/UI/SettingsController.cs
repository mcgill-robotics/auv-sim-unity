using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Handles the Config drawer: sensor toggles, camera parameters, graphics options and the
/// SAVE / RESET buttons.
///
/// Every control writes its value into <see cref="SimulationSettings"/> as soon as it changes
/// (applying it live where the sim supports that) and marks the settings unsaved. Nothing is
/// written to disk until SAVE is pressed. Fields labelled "(restart)" are only read when the
/// scene starts, so they need SAVE and a restart.
/// </summary>
public class SettingsController : IDisposable
{
    private const string SaveLabel = "SAVE SETTINGS";
    private const string SaveLabelUnsaved = "SAVE SETTINGS *";

    // Settings toggles
    private Toggle togglePublishROS;
    private Toggle toggleGTObjectMap;
    private Toggle toggleStreamZED;
    private Toggle toggleWater;
    private Toggle toggleShadows;
    private Toggle toggleFreeCamera;
    private Toggle toggleDVL;
    private Toggle toggleIMU;
    private Toggle toggleDepth;
    private Toggle toggleHydro;
    private Toggle toggleFrontCam;
    private Toggle toggleDownCam;
    private Toggle toggleDepthCamera;

    // Camera parameters
    private IntegerField inputFrontRate;
    private IntegerField inputDownRate;
    private IntegerField inputFrontW;
    private IntegerField inputFrontH;
    private IntegerField inputDownW;
    private IntegerField inputDownH;

    // Dropdowns and buttons
    private DropdownField dropdownQuality;
    private DropdownField dropdownScreenRes;
    private Button btnSave;
    private Button btnReset;

    private readonly ROSConnection ros;
    private readonly Action<string> logCallback;

    // Undo actions for every callback registered on the UI or on other objects, run in Dispose.
    private readonly List<Action> unregister = new List<Action>();

    public SettingsController(VisualElement root, ROSConnection rosConnection, Action<string> log)
    {
        ros = rosConnection;
        logCallback = log;
        QueryElements(root);
        PopulateDropdowns();
        RegisterCallbacks();
    }

    private void QueryElements(VisualElement root)
    {
        togglePublishROS = root.Q<Toggle>(HudIds.TogglePublishROS);
        toggleGTObjectMap = root.Q<Toggle>(HudIds.ToggleGTObjectMap);
        toggleStreamZED = root.Q<Toggle>(HudIds.ToggleStreamZED);
        toggleWater = root.Q<Toggle>(HudIds.ToggleWater);
        toggleShadows = root.Q<Toggle>(HudIds.ToggleShadows);
        toggleFreeCamera = root.Q<Toggle>(HudIds.ToggleFreeCamera);
        toggleDVL = root.Q<Toggle>(HudIds.ToggleDVL);
        toggleIMU = root.Q<Toggle>(HudIds.ToggleIMU);
        toggleDepth = root.Q<Toggle>(HudIds.ToggleDepth);
        toggleHydro = root.Q<Toggle>(HudIds.ToggleHydro);
        toggleFrontCam = root.Q<Toggle>(HudIds.ToggleFrontCam);
        toggleDownCam = root.Q<Toggle>(HudIds.ToggleDownCam);
        toggleDepthCamera = root.Q<Toggle>(HudIds.ToggleDepthCamera);

        inputFrontRate = root.Q<IntegerField>(HudIds.InputFrontRate);
        inputDownRate = root.Q<IntegerField>(HudIds.InputDownRate);
        inputFrontW = root.Q<IntegerField>(HudIds.InputFrontW);
        inputFrontH = root.Q<IntegerField>(HudIds.InputFrontH);
        inputDownW = root.Q<IntegerField>(HudIds.InputDownW);
        inputDownH = root.Q<IntegerField>(HudIds.InputDownH);

        dropdownQuality = root.Q<DropdownField>(HudIds.DropdownQuality);
        dropdownScreenRes = root.Q<DropdownField>(HudIds.DropdownScreenRes);
        btnSave = root.Q<Button>(HudIds.BtnSave);
        btnReset = root.Q<Button>(HudIds.BtnResetDefaults);
    }

    private void PopulateDropdowns()
    {
        if (dropdownQuality != null)
        {
            dropdownQuality.choices = QualitySettings.names.ToList();
        }
    }

    private void RegisterCallbacks()
    {
        // "Enable Water" is the inverse of NoWaterMode.
        BindToggle(toggleWater, (s, v) => { s.NoWaterMode = !v; s.ApplyNoWaterMode(); });
        BindToggle(toggleShadows, (s, v) => { s.EnableShadows = v; s.ApplyShadows(); });
        BindToggle(togglePublishROS, (s, v) => { s.PublishROS = v; UpdateROSConnectionState(); });
        BindToggle(toggleGTObjectMap, (s, v) => s.PublishGTObjectMap = v);
        BindToggle(toggleStreamZED, (s, v) => s.StreamZEDCamera = v);
        BindToggle(toggleDVL, (s, v) => s.PublishDVL = v);
        BindToggle(toggleIMU, (s, v) => s.PublishIMU = v);
        BindToggle(toggleDepth, (s, v) => s.PublishDepth = v);
        BindToggle(toggleHydro, (s, v) => s.PublishHydrophones = v);
        BindToggle(toggleFrontCam, (s, v) => s.PublishFrontCam = v);
        BindToggle(toggleDownCam, (s, v) => s.PublishDownCam = v);
        BindToggle(toggleDepthCamera, (s, v) => s.PublishDepthCamera = v);

        BindInt(inputFrontRate, nameof(SimulationSettings.FrontCamRate), (s, v) => s.FrontCamRate = v);
        BindInt(inputDownRate, nameof(SimulationSettings.DownCamRate), (s, v) => s.DownCamRate = v);
        BindInt(inputFrontW, nameof(SimulationSettings.FrontCamWidth), (s, v) => s.FrontCamWidth = v);
        BindInt(inputFrontH, nameof(SimulationSettings.FrontCamHeight), (s, v) => s.FrontCamHeight = v);
        BindInt(inputDownW, nameof(SimulationSettings.DownCamWidth), (s, v) => s.DownCamWidth = v);
        BindInt(inputDownH, nameof(SimulationSettings.DownCamHeight), (s, v) => s.DownCamHeight = v);

        if (dropdownQuality != null)
        {
            EventCallback<ChangeEvent<string>> cb = evt => OnQualityChanged();
            dropdownQuality.RegisterValueChangedCallback(cb);
            unregister.Add(() => dropdownQuality.UnregisterValueChangedCallback(cb));
        }

        if (dropdownScreenRes != null)
        {
            EventCallback<ChangeEvent<string>> cb = evt => OnScreenResolutionChanged(evt.newValue);
            dropdownScreenRes.RegisterValueChangedCallback(cb);
            unregister.Add(() => dropdownScreenRes.UnregisterValueChangedCallback(cb));
        }

        // The free camera is a view choice, not a setting: it switches live and is not saved.
        if (toggleFreeCamera != null)
        {
            EventCallback<ChangeEvent<bool>> cb = evt =>
            {
                if (CameraManager.Instance == null) return;
                if (evt.newValue) CameraManager.Instance.ActivateFreeCam();
                else CameraManager.Instance.ActivateFollowCam();
            };
            toggleFreeCamera.RegisterValueChangedCallback(cb);
            unregister.Add(() => toggleFreeCamera.UnregisterValueChangedCallback(cb));
        }

        CameraManager cameraManager = CameraManager.Instance;
        if (cameraManager != null)
        {
            // Keeps the toggle in sync when the camera is switched with the keyboard.
            Action<bool> onCameraChanged = isFree => toggleFreeCamera?.SetValueWithoutNotify(isFree);
            cameraManager.FreeCameraChanged += onCameraChanged;
            unregister.Add(() => { if (cameraManager != null) cameraManager.FreeCameraChanged -= onCameraChanged; });
        }

        SimulationSettings settings = SimulationSettings.Instance;
        if (settings != null)
        {
            Action<bool> onUnsaved = RefreshSaveButton;
            Action onReloaded = LoadSettingsToUI;
            settings.UnsavedChangesChanged += onUnsaved;
            settings.SettingsReloaded += onReloaded;
            unregister.Add(() =>
            {
                if (settings == null) return;
                settings.UnsavedChangesChanged -= onUnsaved;
                settings.SettingsReloaded -= onReloaded;
            });
        }

        if (btnSave != null)
        {
            btnSave.clicked += OnSaveSettings;
            unregister.Add(() => btnSave.clicked -= OnSaveSettings);
        }

        if (btnReset != null)
        {
            btnReset.clicked += OnResetToDefaults;
            unregister.Add(() => btnReset.clicked -= OnResetToDefaults);
        }
    }

    private void BindToggle(Toggle toggle, Action<SimulationSettings, bool> apply)
    {
        if (toggle == null) return;
        EventCallback<ChangeEvent<bool>> cb = evt =>
        {
            var s = SimulationSettings.Instance;
            if (s == null) return;
            apply(s, evt.newValue);
            s.MarkUnsaved();
        };
        toggle.RegisterValueChangedCallback(cb);
        unregister.Add(() => toggle.UnregisterValueChangedCallback(cb));
    }

    /// <summary>
    /// Binds a number field to a settings field and keeps it inside that field's [Range].
    /// The field only reports a value when the user presses Enter or leaves it, so the check
    /// does not fight half-typed numbers.
    /// </summary>
    private void BindInt(IntegerField field, string settingName, Action<SimulationSettings, int> apply)
    {
        if (field == null) return;
        field.isDelayed = true;

        bool hasRange = SimulationSettings.TryGetRange(settingName, out float min, out float max);
        int lo = hasRange ? Mathf.CeilToInt(min) : 1;
        int hi = hasRange ? Mathf.FloorToInt(max) : int.MaxValue;
        field.tooltip = hasRange ? $"{lo} to {hi}" : "Must be greater than 0";

        EventCallback<ChangeEvent<int>> cb = evt =>
        {
            var s = SimulationSettings.Instance;
            if (s == null) return;

            int value = Mathf.Clamp(evt.newValue, Mathf.Max(lo, 1), hi);
            if (value != evt.newValue)
            {
                field.SetValueWithoutNotify(value);
                logCallback?.Invoke($"{settingName} must be between {Mathf.Max(lo, 1)} and {hi}; using {value}.");
            }
            apply(s, value);
            s.MarkUnsaved();
        };
        field.RegisterValueChangedCallback(cb);
        unregister.Add(() => field.UnregisterValueChangedCallback(cb));
    }

    private void OnQualityChanged()
    {
        var s = SimulationSettings.Instance;
        if (s == null || dropdownQuality == null || dropdownQuality.index < 0) return;

        s.QualityLevel = dropdownQuality.index;
        s.ApplyQualityLevel();
        UpdateThrusterQuality(s.QualityLevel);
        s.MarkUnsaved();
    }

    private void OnScreenResolutionChanged(string value)
    {
        var s = SimulationSettings.Instance;
        if (s == null || string.IsNullOrEmpty(value)) return;

        s.ScreenResolution = value;
        s.ApplyScreenResolution();
        s.MarkUnsaved();
    }

    private static void UpdateThrusterQuality(int level)
    {
        // Thrusters caches the quality level to decide whether to play motor sounds.
        var thrusters = UnityEngine.Object.FindFirstObjectByType<Thrusters>();
        if (thrusters != null)
        {
            thrusters.UpdateQualityLevel(level);
        }
    }

    /// <summary>
    /// Copies the current settings into the UI without triggering the change callbacks.
    /// </summary>
    public void LoadSettingsToUI()
    {
        var s = SimulationSettings.Instance;
        if (s == null) return;

        togglePublishROS?.SetValueWithoutNotify(s.PublishROS);
        toggleGTObjectMap?.SetValueWithoutNotify(s.PublishGTObjectMap);
        if (toggleStreamZED != null)
        {
#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            toggleStreamZED.SetValueWithoutNotify(s.StreamZEDCamera);
            toggleStreamZED.style.display = DisplayStyle.Flex;
#else
            toggleStreamZED.SetValueWithoutNotify(false);
            toggleStreamZED.style.display = DisplayStyle.None;
#endif
        }
        toggleWater?.SetValueWithoutNotify(!s.NoWaterMode);
        toggleShadows?.SetValueWithoutNotify(s.EnableShadows);
        toggleDVL?.SetValueWithoutNotify(s.PublishDVL);
        toggleIMU?.SetValueWithoutNotify(s.PublishIMU);
        toggleDepth?.SetValueWithoutNotify(s.PublishDepth);
        toggleHydro?.SetValueWithoutNotify(s.PublishHydrophones);
        toggleFrontCam?.SetValueWithoutNotify(s.PublishFrontCam);
        toggleDownCam?.SetValueWithoutNotify(s.PublishDownCam);
        toggleDepthCamera?.SetValueWithoutNotify(s.PublishDepthCamera);
        toggleFreeCamera?.SetValueWithoutNotify(CameraManager.Instance != null && CameraManager.Instance.IsFreeCameraActive);

        inputFrontRate?.SetValueWithoutNotify(s.FrontCamRate);
        inputDownRate?.SetValueWithoutNotify(s.DownCamRate);
        inputFrontW?.SetValueWithoutNotify(s.FrontCamWidth);
        inputFrontH?.SetValueWithoutNotify(s.FrontCamHeight);
        inputDownW?.SetValueWithoutNotify(s.DownCamWidth);
        inputDownH?.SetValueWithoutNotify(s.DownCamHeight);

        if (dropdownQuality != null && s.QualityLevel >= 0 && s.QualityLevel < dropdownQuality.choices.Count)
        {
            dropdownQuality.SetValueWithoutNotify(dropdownQuality.choices[s.QualityLevel]);
        }

        if (dropdownScreenRes != null)
        {
            string res = string.IsNullOrEmpty(s.ScreenResolution) ? SimulationSettings.NativeResolution : s.ScreenResolution;
            if (!dropdownScreenRes.choices.Contains(res))
            {
                // A size typed into the settings file that is not in the list still shows up.
                var choices = new List<string>(dropdownScreenRes.choices) { res };
                dropdownScreenRes.choices = choices;
            }
            dropdownScreenRes.SetValueWithoutNotify(res);
        }

        RefreshSaveButton(s.HasUnsavedChanges);
    }

    private void RefreshSaveButton(bool unsaved)
    {
        if (btnSave != null) btnSave.text = unsaved ? SaveLabelUnsaved : SaveLabel;
    }

    private void OnSaveSettings()
    {
        var s = SimulationSettings.Instance;
        if (s == null) return;

        s.SaveSettings();
        logCallback?.Invoke($"Settings saved to {SimulationSettings.SettingsFilePath}. Fields marked (restart) apply next time the scene starts.");
    }

    private void OnResetToDefaults()
    {
        var s = SimulationSettings.Instance;
        if (s == null) return;

        // ResetToDefaults raises SettingsReloaded, which refreshes the UI through LoadSettingsToUI.
        s.ResetToDefaults();
        UpdateThrusterQuality(s.QualityLevel);
        UpdateROSConnectionState();
        logCallback?.Invoke("Settings reset to the scene defaults. Fields marked (restart) apply next time the scene starts.");
    }

    /// <summary>
    /// Ensure ROS connection state matches settings.
    /// </summary>
    public void UpdateROSConnectionState()
    {
        if (ros == null || SimulationSettings.Instance == null) return;

        if (SimulationSettings.Instance.PublishROS)
        {
            if (!ros.HasConnectionThread)
            {
                ros.Connect(ros.RosIPAddress, ros.RosPort);
                logCallback?.Invoke("Connecting to ROS...");
            }
        }
        else
        {
            if (ros.HasConnectionThread)
            {
                ros.Disconnect();
                logCallback?.Invoke("Disconnected from ROS.");
            }
        }
    }

    /// <summary>Removes every callback this controller registered.</summary>
    public void Dispose()
    {
        foreach (Action a in unregister) a();
        unregister.Clear();
    }
}
