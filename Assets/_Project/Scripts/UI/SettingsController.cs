using UnityEngine;
using UnityEngine.UIElements;
using Unity.Robotics.ROSTCPConnector;

/// <summary>
/// Handles the Config drawer UI: sensor toggles, camera parameters, quality settings, save button.
/// Extracted from SimulatorHUD for better separation of concerns.
/// </summary>
public class SettingsController
{
    // UI Elements - Settings Toggles
    private Toggle togglePublishROS;
    private Toggle toggleSimObjects;
    private Toggle toggleStreamZED;
    private Toggle toggleNoWater;
    private Toggle toggleFreeCamera;
    private Toggle toggleDVL;
    private Toggle toggleIMU;
    private Toggle toggleDepth;
    private Toggle togglePressure;
    private Toggle toggleHydro;
    private Toggle toggleFrontCam;
    private Toggle toggleDownCam;
    
    // UI Elements - Camera Parameters
    private IntegerField inputFrontRate;
    private IntegerField inputDownRate;
    private IntegerField inputFrontW;
    private IntegerField inputFrontH;
    private IntegerField inputDownW;
    private IntegerField inputDownH;
    
    // UI Elements - Dropdowns & Buttons
    private DropdownField dropdownQuality;
    private DropdownField dropdownScreenRes;
    private Button btnSave;
    
    private ROSConnection ros;
    private System.Action<string> logCallback;
    
    public SettingsController(VisualElement root, ROSConnection rosConnection, System.Action<string> log)
    {
        ros = rosConnection;
        logCallback = log;
        QueryElements(root);
        RegisterCallbacks(root);
    }
    
    private void QueryElements(VisualElement root)
    {
        // Settings toggles
        togglePublishROS = root.Q<Toggle>("Toggle-PublishROS");
        toggleSimObjects = root.Q<Toggle>("Toggle-SimObjects");
        toggleStreamZED = root.Q<Toggle>("Toggle-StreamZED");
        toggleNoWater = root.Q<Toggle>("Toggle-NoWater");
        toggleFreeCamera = root.Q<Toggle>("Toggle-FreeCamera");
        toggleDVL = root.Q<Toggle>("Toggle-DVL");
        toggleIMU = root.Q<Toggle>("Toggle-IMU");
        toggleDepth = root.Q<Toggle>("Toggle-Depth");
        togglePressure = root.Q<Toggle>("Toggle-Pressure");
        toggleHydro = root.Q<Toggle>("Toggle-Hydro");
        toggleFrontCam = root.Q<Toggle>("Toggle-FrontCam");
        toggleDownCam = root.Q<Toggle>("Toggle-DownCam");
        
        // Camera parameters
        inputFrontRate = root.Q<IntegerField>("Input-FrontRate");
        inputDownRate = root.Q<IntegerField>("Input-DownRate");
        inputFrontW = root.Q<IntegerField>("Input-FrontW");
        inputFrontH = root.Q<IntegerField>("Input-FrontH");
        inputDownW = root.Q<IntegerField>("Input-DownW");
        inputDownH = root.Q<IntegerField>("Input-DownH");
        
        // Dropdowns and buttons
        dropdownQuality = root.Q<DropdownField>("Dropdown-Quality");
        dropdownScreenRes = root.Q<DropdownField>("Dropdown-ScreenRes");
        btnSave = root.Q<Button>("Btn-Save");
    }
    
    private void RegisterCallbacks(VisualElement root)
    {
        // No Water toggle - immediate effect
        if (toggleNoWater != null)
        {
            toggleNoWater.RegisterValueChangedCallback(evt => {
                if (SimulationSettings.Instance != null)
                {
                    SimulationSettings.Instance.NoWaterMode = evt.newValue;
                    SimulationSettings.Instance.ApplyNoWaterMode();
                }
            });
        }
        
        // Free Camera toggle - immediate effect
        if (toggleFreeCamera != null)
        {
            toggleFreeCamera.RegisterValueChangedCallback(evt => {
                if (CameraManager.Instance != null)
                {
                    if (evt.newValue)
                        CameraManager.Instance.ActivateFreeCam();
                    else
                        CameraManager.Instance.ActivateFollowCam();
                }
            });
        }
        
        // Sensor publishing toggles - immediate effect (no restart needed)
        if (togglePublishROS != null)
        {
            togglePublishROS.RegisterValueChangedCallback(evt => {
                SimulationSettings.Instance.PublishROS = evt.newValue;
            });
        }
        
        if (toggleDVL != null)
        {
            toggleDVL.RegisterValueChangedCallback(evt => {
                SimulationSettings.Instance.PublishDVL = evt.newValue;
            });
        }
        
        if (toggleIMU != null)
        {
            toggleIMU.RegisterValueChangedCallback(evt => {
                SimulationSettings.Instance.PublishIMU = evt.newValue;
            });
        }
        
        if (toggleDepth != null)
        {
            toggleDepth.RegisterValueChangedCallback(evt => {
                SimulationSettings.Instance.PublishDepth = evt.newValue;
            });
        }
        
        if (togglePressure != null)
        {
            togglePressure.RegisterValueChangedCallback(evt => {
                SimulationSettings.Instance.PublishPressure = evt.newValue;
            });
        }
        
        if (toggleHydro != null)
        {
            toggleHydro.RegisterValueChangedCallback(evt => {
                SimulationSettings.Instance.PublishHydrophones = evt.newValue;
            });
        }
        
        if (toggleFrontCam != null)
        {
            toggleFrontCam.RegisterValueChangedCallback(evt => {
                SimulationSettings.Instance.PublishFrontCam = evt.newValue;
            });
        }
        
        if (toggleDownCam != null)
        {
            toggleDownCam.RegisterValueChangedCallback(evt => {
                SimulationSettings.Instance.PublishDownCam = evt.newValue;
            });
        }
        
        // Save button
        if (btnSave != null)
        {
            btnSave.clicked += OnSaveSettings;
        }
    }
    
    /// <summary>
    /// Load current settings values to UI elements.
    /// </summary>
    public void LoadSettingsToUI()
    {
        if (SimulationSettings.Instance == null) return;

        if (togglePublishROS != null) togglePublishROS.value = SimulationSettings.Instance.PublishROS;
        if (toggleSimObjects != null) toggleSimObjects.value = SimulationSettings.Instance.DisplaySimObjects;
        if (toggleStreamZED != null) toggleStreamZED.value = SimulationSettings.Instance.StreamZEDCamera;
        if (toggleNoWater != null) toggleNoWater.value = SimulationSettings.Instance.NoWaterMode;
        if (toggleDVL != null) toggleDVL.value = SimulationSettings.Instance.PublishDVL;
        if (toggleIMU != null) toggleIMU.value = SimulationSettings.Instance.PublishIMU;
        if (toggleDepth != null) toggleDepth.value = SimulationSettings.Instance.PublishDepth;
        if (togglePressure != null) togglePressure.value = SimulationSettings.Instance.PublishPressure;
        if (toggleHydro != null) toggleHydro.value = SimulationSettings.Instance.PublishHydrophones;
        if (toggleFrontCam != null) toggleFrontCam.value = SimulationSettings.Instance.PublishFrontCam;
        if (toggleDownCam != null) toggleDownCam.value = SimulationSettings.Instance.PublishDownCam;

        if (inputFrontRate != null) inputFrontRate.value = SimulationSettings.Instance.FrontCamRate;
        if (inputDownRate != null) inputDownRate.value = SimulationSettings.Instance.DownCamRate;
        if (inputFrontW != null) inputFrontW.value = SimulationSettings.Instance.FrontCamWidth;
        if (inputFrontH != null) inputFrontH.value = SimulationSettings.Instance.FrontCamHeight;
        if (inputDownW != null) inputDownW.value = SimulationSettings.Instance.DownCamWidth;
        if (inputDownH != null) inputDownH.value = SimulationSettings.Instance.DownCamHeight;
        
        if (dropdownQuality != null) dropdownQuality.index = SimulationSettings.Instance.QualityLevel;
    }
    
    private void OnSaveSettings()
    {
        if (SimulationSettings.Instance == null) return;

        // Transfer UI values to settings
        if (togglePublishROS != null) SimulationSettings.Instance.PublishROS = togglePublishROS.value;
        if (toggleSimObjects != null) SimulationSettings.Instance.DisplaySimObjects = toggleSimObjects.value;
        if (toggleStreamZED != null) SimulationSettings.Instance.StreamZEDCamera = toggleStreamZED.value;
        if (toggleNoWater != null) SimulationSettings.Instance.NoWaterMode = toggleNoWater.value;
        if (toggleDVL != null) SimulationSettings.Instance.PublishDVL = toggleDVL.value;
        if (toggleIMU != null) SimulationSettings.Instance.PublishIMU = toggleIMU.value;
        if (toggleDepth != null) SimulationSettings.Instance.PublishDepth = toggleDepth.value;
        if (togglePressure != null) SimulationSettings.Instance.PublishPressure = togglePressure.value;
        if (toggleHydro != null) SimulationSettings.Instance.PublishHydrophones = toggleHydro.value;
        if (toggleFrontCam != null) SimulationSettings.Instance.PublishFrontCam = toggleFrontCam.value;
        if (toggleDownCam != null) SimulationSettings.Instance.PublishDownCam = toggleDownCam.value;

        if (inputFrontRate != null) SimulationSettings.Instance.FrontCamRate = inputFrontRate.value;
        if (inputDownRate != null) SimulationSettings.Instance.DownCamRate = inputDownRate.value;
        if (inputFrontW != null) SimulationSettings.Instance.FrontCamWidth = inputFrontW.value;
        if (inputFrontH != null) SimulationSettings.Instance.FrontCamHeight = inputFrontH.value;
        if (inputDownW != null) SimulationSettings.Instance.DownCamWidth = inputDownW.value;
        if (inputDownH != null) SimulationSettings.Instance.DownCamHeight = inputDownH.value;
        
        if (dropdownQuality != null)
        {
            SimulationSettings.Instance.QualityLevel = dropdownQuality.index;
            QualitySettings.SetQualityLevel(SimulationSettings.Instance.QualityLevel);
        }
        
        // Apply screen resolution
        if (dropdownScreenRes != null)
        {
            string resSelection = dropdownScreenRes.value;
            if (resSelection != "Native")
            {
                string[] parts = resSelection.Split('x');
                if (parts.Length == 2 && int.TryParse(parts[0], out int width) && int.TryParse(parts[1], out int height))
                {
                    Screen.SetResolution(width, height, Screen.fullScreenMode);
                }
            }
        }
        
        // Update Thrusters cached quality level
        var thrusters = Object.FindFirstObjectByType<Thrusters>();
        if (thrusters != null)
        {
            thrusters.UpdateQualityLevel(SimulationSettings.Instance.QualityLevel);
        }

        SimulationSettings.Instance.SaveSettings();
        logCallback?.Invoke("Settings saved. Restart may be required for some changes.");
    }
    
    /// <summary>
    /// Ensure ROS connection state matches settings.
    /// </summary>
    public void UpdateROSConnectionState()
    {
        if (ros == null) return;
        
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
}
