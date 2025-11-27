using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using RosMessageTypes.Sensor;
using RosMessageTypes.Auv;

public class SimulatorHUD : MonoBehaviour
{
    public static SimulatorHUD Instance { get; private set; }

    [Header("Configuration")]
    public UIDocument uiDocument;

    [Header("Camera Sources")]
    public Camera frontLeftCamera;
    public Camera frontRightCamera;
    public Camera downCamera;
    public CameraDepthPublisher depthPublisher;

    // UI Elements - Settings
    private Toggle togglePublishROS;
    private Toggle toggleSimObjects;
    private Toggle toggleStreamZED;
    private Toggle toggleDVL;
    private Toggle toggleIMU;
    private Toggle toggleDepth;
    private Toggle toggleHydro;
    private Toggle toggleFrontCam;
    private Toggle toggleDownCam;
    private IntegerField inputFrontRate;
    private IntegerField inputDownRate;
    private IntegerField inputFrontW;
    private IntegerField inputFrontH;
    private IntegerField inputDownW;
    private IntegerField inputDownH;
    private DropdownField dropdownQuality;
    private Button btnSave;

    // UI Elements - Telemetry
    private Label textPosX, textPosY, textPosZ;
    private Label textRotX, textRotY, textRotZ;
    private Label textMissionStatus;
    private Label statusDVL, statusIMU, statusDepth, statusHydro, statusActuators, statusFrontCam, statusDownCam;

    // UI Elements - Competition
    private Label textTimer;
    private Label textScore;
    private DropdownField dropdownTask;
    private Button btnStartComp;

    // UI Elements - Camera
    private DropdownField dropdownCamTopic;
    private Image cameraFeedImage;
    private Label textLog;

    // Internal State
    private ROSConnection ros;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        // --- Query Elements ---
        
        // Settings
        togglePublishROS = root.Q<Toggle>("Toggle-PublishROS");
        toggleSimObjects = root.Q<Toggle>("Toggle-SimObjects");
        toggleStreamZED = root.Q<Toggle>("Toggle-StreamZED");
        toggleDVL = root.Q<Toggle>("Toggle-DVL");
        toggleIMU = root.Q<Toggle>("Toggle-IMU");
        toggleDepth = root.Q<Toggle>("Toggle-Depth");
        toggleHydro = root.Q<Toggle>("Toggle-Hydro");
        toggleFrontCam = root.Q<Toggle>("Toggle-FrontCam");
        toggleDownCam = root.Q<Toggle>("Toggle-DownCam");
        
        inputFrontRate = root.Q<IntegerField>("Input-FrontRate");
        inputDownRate = root.Q<IntegerField>("Input-DownRate");
        inputFrontW = root.Q<IntegerField>("Input-FrontW");
        inputFrontH = root.Q<IntegerField>("Input-FrontH");
        inputDownW = root.Q<IntegerField>("Input-DownW");
        inputDownH = root.Q<IntegerField>("Input-DownH");
        
        dropdownQuality = root.Q<DropdownField>("Dropdown-Quality");
        btnSave = root.Q<Button>("Btn-Save");

        // Telemetry
        textPosX = root.Q<Label>("Text-PosX");
        textPosY = root.Q<Label>("Text-PosY");
        textPosZ = root.Q<Label>("Text-PosZ");
        textRotX = root.Q<Label>("Text-RotX");
        textRotY = root.Q<Label>("Text-RotY");
        textRotZ = root.Q<Label>("Text-RotZ");
        textMissionStatus = root.Q<Label>("Text-MissionStatus");

        // Status
        statusDVL = root.Q<Label>("Status-DVL");
        statusIMU = root.Q<Label>("Status-IMU");
        statusDepth = root.Q<Label>("Status-Depth");
        statusHydro = root.Q<Label>("Status-Hydro");
        statusActuators = root.Q<Label>("Status-Actuators");
        statusFrontCam = root.Q<Label>("Status-FrontCam");
        statusDownCam = root.Q<Label>("Status-DownCam");

        // Competition
        textTimer = root.Q<Label>("Text-Timer");
        textScore = root.Q<Label>("Text-Score");
        dropdownTask = root.Q<DropdownField>("Dropdown-Task");
        btnStartComp = root.Q<Button>("Btn-StartComp");

        // Camera
        dropdownCamTopic = root.Q<DropdownField>("Dropdown-CamTopic");
        cameraFeedImage = root.Q<Image>("CameraFeedImage");

        // Log
        textLog = root.Q<Label>("Text-Log");
    }

    private IEnumerator Start()
    {
        // Initialize ROS
        ros = ROSConnection.GetOrCreateInstance();

        // Load settings from PlayerPrefs
        LoadSettingsToUI();

        // Register button callbacks
        if (btnSave != null) btnSave.clicked += OnSaveSettings;

        // Subscribe to ROS topics for telemetry
        if (ros != null)
        {
            SubscribeToTelemetry();
        }

        // Wait for end of frame to ensure other scripts (CameraPublisher, ZED2iSimSender)
        // have initialized their textures.
        yield return new WaitForEndOfFrame();

        // Initialize camera dropdown
        InitializeCameraDropdown();
    }

    // --- Settings Logic ---

    private void LoadSettingsToUI()
    {
        if (SimulationSettings.Instance == null) return;

        togglePublishROS.value = SimulationSettings.Instance.PublishROS;
        toggleSimObjects.value = SimulationSettings.Instance.DisplaySimObjects;
        toggleStreamZED.value = SimulationSettings.Instance.StreamZEDCamera;
        toggleDVL.value = SimulationSettings.Instance.PublishDVL;
        toggleIMU.value = SimulationSettings.Instance.PublishIMU;
        toggleDepth.value = SimulationSettings.Instance.PublishDepth;
        toggleHydro.value = SimulationSettings.Instance.PublishHydrophones;
        toggleFrontCam.value = SimulationSettings.Instance.PublishFrontCam;
        toggleDownCam.value = SimulationSettings.Instance.PublishDownCam;

        inputFrontRate.value = SimulationSettings.Instance.FrontCamRate;
        inputDownRate.value = SimulationSettings.Instance.DownCamRate;
        inputFrontW.value = SimulationSettings.Instance.FrontCamWidth;
        inputFrontH.value = SimulationSettings.Instance.FrontCamHeight;
        inputDownW.value = SimulationSettings.Instance.DownCamWidth;
        inputDownH.value = SimulationSettings.Instance.DownCamHeight;
        
        dropdownQuality.index = SimulationSettings.Instance.QualityLevel;
    }

    private void OnSaveSettings()
    {
        if (SimulationSettings.Instance == null) return;

        SimulationSettings.Instance.PublishROS = togglePublishROS.value;
        SimulationSettings.Instance.DisplaySimObjects = toggleSimObjects.value;
        SimulationSettings.Instance.StreamZEDCamera = toggleStreamZED.value;
        SimulationSettings.Instance.PublishDVL = toggleDVL.value;
        SimulationSettings.Instance.PublishIMU = toggleIMU.value;
        SimulationSettings.Instance.PublishDepth = toggleDepth.value;
        SimulationSettings.Instance.PublishHydrophones = toggleHydro.value;
        SimulationSettings.Instance.PublishFrontCam = toggleFrontCam.value;
        SimulationSettings.Instance.PublishDownCam = toggleDownCam.value;

        SimulationSettings.Instance.FrontCamRate = inputFrontRate.value;
        SimulationSettings.Instance.DownCamRate = inputDownRate.value;
        SimulationSettings.Instance.FrontCamWidth = inputFrontW.value;
        SimulationSettings.Instance.FrontCamHeight = inputFrontH.value;
        SimulationSettings.Instance.DownCamWidth = inputDownW.value;
        SimulationSettings.Instance.DownCamHeight = inputDownH.value;
        
        SimulationSettings.Instance.QualityLevel = dropdownQuality.index;
        QualitySettings.SetQualityLevel(SimulationSettings.Instance.QualityLevel);

        SimulationSettings.Instance.SaveSettings();
        Log("Configuration Saved.");
    }

    // --- Telemetry Logic ---

    private void SubscribeToTelemetry()
    {
        ros.Subscribe<Float64Msg>(ROSSettings.Instance.StateXTopic, msg => UpdateLabel(textPosX, $"{msg.data:F2} m"));
        ros.Subscribe<Float64Msg>(ROSSettings.Instance.StateYTopic, msg => UpdateLabel(textPosY, $"{msg.data:F2} m"));
        ros.Subscribe<Float64Msg>(ROSSettings.Instance.StateZTopic, msg => UpdateLabel(textPosZ, $"{msg.data:F2} m"));
        
        ros.Subscribe<Float64Msg>(ROSSettings.Instance.StateThetaXTopic, msg => UpdateLabel(textRotX, $"{msg.data:F1}°"));
        ros.Subscribe<Float64Msg>(ROSSettings.Instance.StateThetaYTopic, msg => UpdateLabel(textRotY, $"{msg.data:F1}°")); // Heading
        ros.Subscribe<Float64Msg>(ROSSettings.Instance.StateThetaZTopic, msg => UpdateLabel(textRotZ, $"{msg.data:F1}°"));
    }

    private void UpdateLabel(Label label, string text)
    {
        // UI Toolkit is not thread safe, must run on main thread
        UnityMainThreadDispatcher.Instance().Enqueue(() => {
            if (label != null) label.text = text;
        });
    }

    // --- Camera Logic ---

    private void InitializeCameraDropdown()
    {
        var choices = new List<string>();
        if (frontLeftCamera != null) choices.Add("Front Left");
        if (frontRightCamera != null) choices.Add("Front Right");
        if (downCamera != null) choices.Add("Down");
        if (depthPublisher != null) choices.Add("Front Depth");

        dropdownCamTopic.choices = choices;
        if (choices.Count > 0) dropdownCamTopic.index = 0;

        dropdownCamTopic.RegisterValueChangedCallback(evt => UpdateCameraFeed(evt.newValue));
        
        // Initial update
        if (choices.Count > 0) UpdateCameraFeed(choices[0]);
    }

    private void UpdateCameraFeed(string selection)
    {
        // Reset
        cameraFeedImage.image = null;

        if (selection == "Front Left" && frontLeftCamera != null)
        {
            cameraFeedImage.image = GetOrCreateTargetTexture(frontLeftCamera);
        }
        else if (selection == "Front Right" && frontRightCamera != null)
        {
            cameraFeedImage.image = GetOrCreateTargetTexture(frontRightCamera);
        }
        else if (selection == "Down" && downCamera != null)
        {
            cameraFeedImage.image = GetOrCreateTargetTexture(downCamera);
        }
        else if (selection == "Front Depth" && depthPublisher != null)
        {
            cameraFeedImage.image = depthPublisher.DepthTexture;
        }
    }

    private RenderTexture GetOrCreateTargetTexture(Camera cam)
    {
        if (cam.targetTexture != null) return cam.targetTexture;

        // Create a render texture if none exists
        // Use settings resolution if available, else default
        int width = SimulationSettings.Instance != null ? SimulationSettings.Instance.FrontCamWidth : 640;
        int height = SimulationSettings.Instance != null ? SimulationSettings.Instance.FrontCamHeight : 480;
        
        RenderTexture rt = new RenderTexture(width, height, 24);
        rt.enableRandomWrite = true;
        rt.Create();
        rt.name = $"{cam.name}_RT";
        cam.targetTexture = rt;
        return rt;
    }

    // Removed OnCameraImageReceived as we now use local textures directly

    private void OnDestroy()
    {
        // Cleanup render textures to prevent memory leaks
        if (frontLeftCamera != null && frontLeftCamera.targetTexture != null)
        {
            frontLeftCamera.targetTexture.Release();
            Destroy(frontLeftCamera.targetTexture);
        }
        if (frontRightCamera != null && frontRightCamera.targetTexture != null)
        {
            frontRightCamera.targetTexture.Release();
            Destroy(frontRightCamera.targetTexture);
        }
        if (downCamera != null && downCamera.targetTexture != null)
        {
            downCamera.targetTexture.Release();
            Destroy(downCamera.targetTexture);
        }
    }

    // Removed CPU Helper Methods (Normalize, SwitchBlueAndRedChannels, BrightenTexture) as they are now in Shader

    // --- Competition Logic ---

    public void UpdateTimer(string time)
    {
        if (textTimer != null) textTimer.text = time;
    }

    public void UpdateScore(string score)
    {
        if (textScore != null) textScore.text = score;
    }

    // --- Logging ---

    public void Log(string message)
    {
        if (textLog != null) textLog.text += $"\n> {message}";
    }
}
