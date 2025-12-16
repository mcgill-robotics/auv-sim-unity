using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using RosMessageTypes.Sensor;
using RosMessageTypes.Auv;

public class SimulatorHUD : MonoBehaviour
{
    public static SimulatorHUD Instance { get; private set; }

    // Public property to check if user is typing
    public bool IsInputFocused { get; private set; }

    [Header("UI Configuration")]
    [Tooltip("Reference to the UI Toolkit UIDocument component")]
    public UIDocument uiDocument;

    [Space(10)]
    [Header("Camera Sources for HUD Display")]
    [Tooltip("Front-left camera for stereo display")]
    public Camera frontLeftCamera;
    
    [Tooltip("Front-right camera for stereo display")]
    public Camera frontRightCamera;
    
    [Tooltip("Downward-facing camera")]
    public Camera downCamera;
    
    // Publishers (Auto-discovered)
    private CameraDepthPublisher depthPublisher;
    private CameraPublisher frontPub;
    private CameraPublisher downPub;
    
    // Sensor Publishers for data display
    private DVLPublisher dvlPublisher;
    private IMUPublisher imuPublisher;

    // UI Elements - Settings
    private Toggle togglePublishROS;
    private Toggle toggleSimObjects;
    private Toggle toggleStreamZED;
    private Toggle toggleNoWater;
    private Toggle toggleFreeCamera;
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
    private DropdownField dropdownScreenRes;
    private Button btnSave;
    private Button btnSaveImage;

    // UI Elements - Telemetry
    private Label textPosX, textPosY, textPosZ;
    private Label textRotX, textRotY, textRotZ;
    private Label textMissionStatus;
    
    // UI Elements - Sensor Data
    private Label textDVLVx, textDVLVy, textDVLVz, textDVLAlt, textDVLLock;
    private Label textIMUAx, textIMUAy, textIMUAz;
    private Label textIMUWx, textIMUWy, textIMUWz;
    private Toggle toggleDVLViz;
    private Toggle toggleIMUViz;

    // UI Elements - Competition
    private Label textTimer;
    private Label textScore;
    private DropdownField dropdownTask;
    private Button btnStartComp;

    // UI Elements - Camera
    private DropdownField dropdownCamTopic;
    private Image cameraFeedImage;
    private Image fullscreenCameraBackground;
    private Button btnFullscreen;
    private bool isFullscreenCamera = false;
    private Label textLog;
    private Label logToggle;
    private ScrollView logScrollView;

    // UI Elements - Drawers
    private VisualElement configDrawer;
    private VisualElement controlsDrawer;
    private VisualElement telemetryDrawer;
    private VisualElement sensorsDrawer;
    private VisualElement cameraDrawer;
    private VisualElement logPanel;
    
    // Drawer Indicators (updated when collapsed)
    private Label configIndicator;
    private Label telemetryIndicator;
    private Label sensorsIndicator;
    private Label cameraIndicator;

    // Internal State
    private ROSConnection ros;
    private const string DRAWER_OPEN_CLASS = "drawer-open";
    private const string DRAWER_COLLAPSED_CLASS = "drawer-collapsed";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        // --- Disable Keyboard Navigation ---
        // Prevent WASD/Arrow keys from moving focus between elements.
        // We register with TrickleDown to catch the event before it reaches children.
        root.RegisterCallback<NavigationMoveEvent>(evt => {
            evt.PreventDefault();
            evt.StopImmediatePropagation();
        }, TrickleDown.TrickleDown);

        // Ensure the root container itself cannot be focused
        root.focusable = false;

        // --- Prevent Space Bar Propagation ---
        // Stop Space bar from triggering UI elements (like Buttons) unless we are typing in a text field.
        // --- Prevent Space Bar Propagation ---
        // Stop Space bar from triggering UI elements (like Buttons).
        // We handle KeyDown, KeyUp, and NavigationSubmit to be thorough.
        EventCallback<EventBase> stopSpacePropagation = evt => {
            var keyEvt = evt as IKeyboardEvent;
            if (keyEvt != null && keyEvt.keyCode == KeyCode.Space)
            {
                // Check if a text field is currently focused
                bool isTyping = root.focusController.focusedElement is TextInputBaseField<string> || 
                                root.focusController.focusedElement is TextInputBaseField<int> ||
                                root.focusController.focusedElement is TextField ||
                                root.focusController.focusedElement is IntegerField ||
                                root.focusController.focusedElement is FloatField ||
                                root.focusController.focusedElement is DoubleField;

                if (!isTyping)
                {
                    evt.PreventDefault();
                    evt.StopImmediatePropagation();
                }
            }
            else if (evt is NavigationSubmitEvent)
            {
                 // NavigationSubmit is triggered by Space or Enter.
                 // Since this event doesn't have keyCode, we check the Input state directly.
                 if (Input.GetKey(KeyCode.Space))
                 {
                    // Check if a text field is currently focused
                    bool isTyping = root.focusController.focusedElement is TextInputBaseField<string> || 
                                    root.focusController.focusedElement is TextInputBaseField<int> ||
                                    root.focusController.focusedElement is TextField ||
                                    root.focusController.focusedElement is IntegerField ||
                                    root.focusController.focusedElement is FloatField ||
                                    root.focusController.focusedElement is DoubleField;

                    if (!isTyping)
                    {
                        evt.PreventDefault();
                        evt.StopImmediatePropagation();
                    }
                 }
            }
        };

        root.RegisterCallback<KeyDownEvent>(evt => stopSpacePropagation(evt), TrickleDown.TrickleDown);
        root.RegisterCallback<KeyUpEvent>(evt => stopSpacePropagation(evt), TrickleDown.TrickleDown);
        root.RegisterCallback<NavigationSubmitEvent>(evt => stopSpacePropagation(evt), TrickleDown.TrickleDown);

        // --- Query Elements ---
        
        // Settings
        togglePublishROS = root.Q<Toggle>("Toggle-PublishROS");
        toggleSimObjects = root.Q<Toggle>("Toggle-SimObjects");
        toggleStreamZED = root.Q<Toggle>("Toggle-StreamZED");
        toggleNoWater = root.Q<Toggle>("Toggle-NoWater");
        toggleNoWater.RegisterValueChangedCallback(evt => {
            if (SimulationSettings.Instance != null)
            {
                SimulationSettings.Instance.NoWaterMode = evt.newValue;
                SimulationSettings.Instance.ApplyNoWaterMode();
            }
        });
        toggleFreeCamera = root.Q<Toggle>("Toggle-FreeCamera");
        toggleFreeCamera.RegisterValueChangedCallback(evt => {
            if (CameraManager.Instance != null)
            {
                if (evt.newValue)
                    CameraManager.Instance.ActivateFreeCam();
                else
                    CameraManager.Instance.ActivateFollowCam();
            }
        });
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
        dropdownScreenRes = root.Q<DropdownField>("Dropdown-ScreenRes");
        btnSave = root.Q<Button>("Btn-Save");

        // Telemetry
        textPosX = root.Q<Label>("Text-PosX");
        textPosY = root.Q<Label>("Text-PosY");
        textPosZ = root.Q<Label>("Text-PosZ");
        textRotX = root.Q<Label>("Text-RotX");
        textRotY = root.Q<Label>("Text-RotY");
        textRotZ = root.Q<Label>("Text-RotZ");
        textMissionStatus = root.Q<Label>("Text-MissionStatus");

        // Sensor Data
        textDVLVx = root.Q<Label>("Text-DVLVx");
        textDVLVy = root.Q<Label>("Text-DVLVy");
        textDVLVz = root.Q<Label>("Text-DVLVz");
        textDVLAlt = root.Q<Label>("Text-DVLAlt");
        textDVLLock = root.Q<Label>("Text-DVLLock");
        textIMUAx = root.Q<Label>("Text-IMUAx");
        textIMUAy = root.Q<Label>("Text-IMUAy");
        textIMUAz = root.Q<Label>("Text-IMUAz");
        textIMUWx = root.Q<Label>("Text-IMUWx");
        textIMUWy = root.Q<Label>("Text-IMUWy");
        textIMUWz = root.Q<Label>("Text-IMUWz");
        
        // Sensor Visualization Toggles
        toggleDVLViz = root.Q<Toggle>("Toggle-DVLViz");
        toggleIMUViz = root.Q<Toggle>("Toggle-IMUViz");
        
        if (toggleDVLViz != null)
        {
            toggleDVLViz.RegisterValueChangedCallback(evt => {
                if (dvlPublisher != null)
                {
                    dvlPublisher.enableVisualization = evt.newValue;
                    dvlPublisher.SetVisualizationActive(evt.newValue);
                }
            });
        }
        
        if (toggleIMUViz != null)
        {
            toggleIMUViz.RegisterValueChangedCallback(evt => {
                if (imuPublisher != null)
                {
                    imuPublisher.enableVisualization = evt.newValue;
                    imuPublisher.SetVisualizationActive(evt.newValue);
                }
            });
        }

        // Competition
        textTimer = root.Q<Label>("Text-Timer");
        textScore = root.Q<Label>("Text-Score");
        dropdownTask = root.Q<DropdownField>("Dropdown-Task");
        btnStartComp = root.Q<Button>("Btn-StartComp");

        // Camera
        dropdownCamTopic = root.Q<DropdownField>("Dropdown-CamTopic");
        cameraFeedImage = root.Q<Image>("CameraFeedImage");
        btnSaveImage = root.Q<Button>("Btn-SaveImage");
        fullscreenCameraBackground = root.Q<Image>("FullscreenCameraBackground");
        btnFullscreen = root.Q<Button>("Btn-Fullscreen");
        
        if (btnFullscreen != null)
        {
            btnFullscreen.clicked += ToggleFullscreenCamera;
        }

        // Log
        textLog = root.Q<Label>("Text-Log");
        logToggle = root.Q<Label>("LogPanel-Toggle");
        logPanel = root.Q<VisualElement>("LogPanel");
        logScrollView = root.Q<ScrollView>("LogScroll");

        // Drawers
        configDrawer = root.Q<VisualElement>("ConfigDrawer");
        controlsDrawer = root.Q<VisualElement>("ControlsDrawer");
        telemetryDrawer = root.Q<VisualElement>("TelemetryDrawer");
        sensorsDrawer = root.Q<VisualElement>("SensorsDrawer");
        cameraDrawer = root.Q<VisualElement>("CameraDrawer");
        
        // Drawer Indicators
        configIndicator = root.Q<Label>("ConfigDrawer-Indicator");
        telemetryIndicator = root.Q<Label>("TelemetryDrawer-Indicator");
        sensorsIndicator = root.Q<Label>("SensorsDrawer-Indicator");
        cameraIndicator = root.Q<Label>("CameraDrawer-Indicator");
        
        // Initialize Drawer Interactions
        InitializeDrawers();

        // Global Click Handler (Click-to-Blur)
        // Register on the root so clicks on background clear focus
        root.RegisterCallback<PointerDownEvent>(evt => {
            // If the target is the root visual element itself (background), blur any focused element
            if (evt.target == root)
            {
                root.focusController.focusedElement?.Blur();
            }
        });
    }

    // --- Drawer System ---
    
    private void InitializeDrawers()
    {
        // Register click handlers on drawer tabs
        RegisterDrawerTab("ConfigDrawer-Tab", configDrawer);
        RegisterDrawerTab("ControlsDrawer-Tab", controlsDrawer);
        RegisterDrawerTab("TelemetryDrawer-Tab", telemetryDrawer);
        RegisterDrawerTab("SensorsDrawer-Tab", sensorsDrawer);
        RegisterDrawerTab("CameraDrawer-Tab", cameraDrawer);
        
        // Log panel toggle
        if (logToggle != null && logPanel != null)
        {
            logToggle.RegisterCallback<PointerDownEvent>(evt => {
                ToggleLogPanel();
                evt.StopPropagation();
            });
        }
    }
    
    private void RegisterDrawerTab(string tabName, VisualElement drawer)
    {
        if (drawer == null) return;
        
        var root = uiDocument.rootVisualElement;
        var tab = root.Q<VisualElement>(tabName);
        
        if (tab != null)
        {
            tab.RegisterCallback<PointerDownEvent>(evt => {
                ToggleDrawer(drawer);
                evt.StopPropagation();
            });
        }
    }
    
    private void ToggleDrawer(VisualElement drawer)
    {
        if (drawer == null) return;
        
        bool isOpen = drawer.ClassListContains(DRAWER_OPEN_CLASS);
        
        if (isOpen)
        {
            // Close drawer
            drawer.RemoveFromClassList(DRAWER_OPEN_CLASS);
            drawer.AddToClassList(DRAWER_COLLAPSED_CLASS);
        }
        else
        {
            // Open drawer
            drawer.RemoveFromClassList(DRAWER_COLLAPSED_CLASS);
            drawer.AddToClassList(DRAWER_OPEN_CLASS);
        }
    }
    
    private void ToggleLogPanel()
    {
        if (logPanel == null) return;
        
        bool isCollapsed = logPanel.ClassListContains("log-overlay-collapsed");
        
        if (isCollapsed)
        {
            logPanel.RemoveFromClassList("log-overlay-collapsed");
            if (logToggle != null) logToggle.text = "▼ LOGS";
        }
        else
        {
            logPanel.AddToClassList("log-overlay-collapsed");
            if (logToggle != null) logToggle.text = "▲ LOGS";
        }
    }
    
    private void ToggleFullscreenCamera()
    {
        if (fullscreenCameraBackground == null) return;
        
        isFullscreenCamera = !isFullscreenCamera;
        
        if (isFullscreenCamera)
        {
            // Show fullscreen background
            fullscreenCameraBackground.style.display = DisplayStyle.Flex;
            if (btnFullscreen != null) btnFullscreen.text = "✕";
        }
        else
        {
            // Hide fullscreen background
            fullscreenCameraBackground.style.display = DisplayStyle.None;
            if (btnFullscreen != null) btnFullscreen.text = "⛶";
        }
    }
    
    private IEnumerator Start()
    {
        // Initialize ROS
        ros = ROSConnection.GetOrCreateInstance();

        // Load settings from PlayerPrefs
        LoadSettingsToUI();

        // Register button callbacks
        if (btnSave != null) btnSave.clicked += OnSaveSettings;
        if (btnSaveImage != null) btnSaveImage.clicked += OnSaveImage;

        // Subscribe to ROS topics for telemetry
        if (ros != null)
        {
            SubscribeToTelemetry();
            UpdateROSConnectionState(); // Ensure connection state matches settings
        }

        // Wait for end of frame to ensure other scripts (CameraPublisher, ZED2iSimSender)
        // have initialized their textures.
        yield return new WaitForEndOfFrame();

        // Initialize camera dropdown
        InitializeCameraDropdown();
        
        // Debug: verify log system is working
        Log("HUD initialized successfully.");
        Debug.Log($"[SimulatorHUD] textLog is {(textLog != null ? "found" : "NULL")}");
    }

    // --- Settings Logic ---

    private void LoadSettingsToUI()
    {
        if (SimulationSettings.Instance == null) return;

        togglePublishROS.value = SimulationSettings.Instance.PublishROS;
        toggleSimObjects.value = SimulationSettings.Instance.DisplaySimObjects;
        toggleStreamZED.value = SimulationSettings.Instance.StreamZEDCamera;
        toggleNoWater.value = SimulationSettings.Instance.NoWaterMode;
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
        SimulationSettings.Instance.NoWaterMode = toggleNoWater.value;
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
            // Note: "Native" keeps current resolution
        }
        
        // Update Thrusters cached quality level
        var thrusters = FindFirstObjectByType<Thrusters>();
        if (thrusters != null)
        {
            thrusters.UpdateQualityLevel(SimulationSettings.Instance.QualityLevel);
        }

        SimulationSettings.Instance.SaveSettings();
        Log("Settings saved. Please restart the game for changes to take effect.");
    }

    private void UpdateROSConnectionState()
    {
        if (ros != null)
        {
            if (SimulationSettings.Instance.PublishROS)
            {
                if (!ros.HasConnectionThread)
                {
                    ros.Connect(ros.RosIPAddress, ros.RosPort);
                    Log("Connecting to ROS...");
                }
            }
            else
            {
                if (ros.HasConnectionThread)
                {
                    ros.Disconnect();
                    Log("Disconnected from ROS.");
                }
            }
        }
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

    private void FindPublishers()
    {
        var publishers = FindObjectsByType<CameraPublisher>(FindObjectsSortMode.None);
        foreach (var pub in publishers)
        {
            if (pub.cameraType == CameraPublisher.CameraType.Front) frontPub = pub;
            else if (pub.cameraType == CameraPublisher.CameraType.Down) downPub = pub;
        }

        if (depthPublisher == null) depthPublisher = FindFirstObjectByType<CameraDepthPublisher>();
        if (dvlPublisher == null) dvlPublisher = FindFirstObjectByType<DVLPublisher>();
        if (imuPublisher == null) imuPublisher = FindFirstObjectByType<IMUPublisher>();
        
        // Initialize sensor viz toggle values from publishers
        if (toggleDVLViz != null && dvlPublisher != null)
            toggleDVLViz.SetValueWithoutNotify(dvlPublisher.enableVisualization);
        if (toggleIMUViz != null && imuPublisher != null)
            toggleIMUViz.SetValueWithoutNotify(imuPublisher.enableVisualization);
    }

    private void InitializeCameraDropdown()
    {
        FindPublishers();

        var choices = new List<string>();
        choices.Add("None"); // Allow disabling camera feed for performance
        if (frontLeftCamera != null) choices.Add("Front Left");
        if (downCamera != null) choices.Add("Down");
        if (depthPublisher != null) choices.Add("Front Depth");

        dropdownCamTopic.choices = choices;
        dropdownCamTopic.index = 0; // Start with None selected

        dropdownCamTopic.RegisterValueChangedCallback(evt => UpdateCameraFeed(evt.newValue));
        
        // Initial update
        UpdateCameraFeed(choices[0]);
    }

    private void UpdateCameraFeed(string selection)
    {
        // Reset all feeds
        cameraFeedImage.image = null;
        if (fullscreenCameraBackground != null) fullscreenCameraBackground.image = null;

        // Reset CameraRenderManager UI flags
        if (CameraRenderManager.Instance != null)
        {
            CameraRenderManager.Instance.frontCameraUINeeded = false;
            CameraRenderManager.Instance.frontDepthUINeeded = false;
            CameraRenderManager.Instance.downCameraUINeeded = false;
        }
        
        // Exit early if None selected (no camera feed)
        if (selection == "None")
        {
            // Disable fullscreen mode if active
            if (isFullscreenCamera)
            {
                ToggleFullscreenCamera();
            }
            return;
        }

        // Update CameraRenderManager UI flags for selected camera
        if (CameraRenderManager.Instance != null)
        {
            CameraRenderManager.Instance.frontCameraUINeeded = (selection == "Front Left");
            CameraRenderManager.Instance.frontDepthUINeeded = (selection == "Front Depth");
            CameraRenderManager.Instance.downCameraUINeeded = (selection == "Down");
        }

        Texture selectedTexture = null;
        
        if (selection == "Front Left" && frontLeftCamera != null)
        {
            selectedTexture = frontLeftCamera.targetTexture;
        }
        else if (selection == "Down" && downCamera != null)
        {
            selectedTexture = downCamera.targetTexture;
        }
        else if (selection == "Front Depth" && depthPublisher != null)
        {
            selectedTexture = depthPublisher.VisualizationTexture;
        }
        
        // Set both preview and fullscreen background
        cameraFeedImage.image = selectedTexture;
        if (fullscreenCameraBackground != null)
        {
            fullscreenCameraBackground.image = selectedTexture;
        }
    }

    private void OnSaveImage()
    {
        if (cameraFeedImage.image == null)
        {
            Log("No camera feed to save.");
            return;
        }

        Texture sourceTexture = cameraFeedImage.image;
        RenderTexture renderTexture = sourceTexture as RenderTexture;

        if (renderTexture == null)
        {
            Log("Cannot save this type of texture.");
            return;
        }

        // Create a new Texture2D with the same dimensions
        Texture2D texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);

        // Remember currently active render texture
        RenderTexture currentActiveRT = RenderTexture.active;

        // Set the supplied RenderTexture as the active one
        RenderTexture.active = renderTexture;

        // Read the pixels from the RenderTexture to the Texture2D
        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture2D.Apply();

        // Restore previously active render texture
        RenderTexture.active = currentActiveRT;

        // Encode texture into PNG
        byte[] bytes = texture2D.EncodeToPNG();
        Destroy(texture2D);

        // Construct filename
        string feedName = dropdownCamTopic.value.Replace(" ", "");
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string filename = $"{feedName}_{renderTexture.width}x{renderTexture.height}_{timestamp}.png";

        // Ensure directory exists
        string savePath = SimulationSettings.Instance.SnapshotSavePath;
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        string fullPath = Path.Combine(savePath, filename);
        File.WriteAllBytes(fullPath, bytes);

        Log($"Snapshot saved to: {fullPath}");
    }

    // Removed OnCameraImageReceived as we now use local textures directly

    private void OnDestroy()
    {
        // SimulatorHUD is a passive viewer now. 
        // CameraPublisher (or the Camera itself) owns the RenderTexture lifecycle.
        // We do not destroy the textures here to avoid race conditions on exit.
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
        Debug.Log($"[HUD Log] {message}");
        
        if (textLog != null)
        {
            textLog.text += $"\n> {message}";
            
            // Auto-scroll to bottom
            if (logScrollView != null)
            {
                logScrollView.schedule.Execute(() => {
                    logScrollView.scrollOffset = new Vector2(0, logScrollView.contentContainer.layout.height);
                });
            }
        }
        else
        {
            Debug.LogWarning("[SimulatorHUD] textLog is null - cannot log to UI");
        }
    }

    private void Update()
    {
        if (uiDocument == null) return;
        
        // 1. Check Focus State
        var focusedElement = uiDocument.rootVisualElement.focusController.focusedElement;
        
        // Check if the focused element is a text input field (or part of one)
        // TextInputBaseField is the base class for TextField, IntegerField, etc.
        // Note: The focused element might be the internal text input, so we check types.
        IsInputFocused = focusedElement is TextInputBaseField<string> || 
                         focusedElement is TextInputBaseField<int> ||
                         focusedElement is TextField ||
                         focusedElement is IntegerField ||
                         focusedElement is FloatField ||
                         focusedElement is DoubleField;

        // 2. Handle Escape to Blur
        if (IsInputFocused && Input.GetKeyDown(KeyCode.Escape))
        {
            focusedElement?.Blur();
            IsInputFocused = false;
        }
        
        // 3. Update Sensor Data Display
        UpdateSensorDataDisplay();
    }
    
    private void UpdateSensorDataDisplay()
    {
        // DVL Data
        if (dvlPublisher != null)
        {
            var vel = dvlPublisher.LastVelocity;
            if (textDVLVx != null) textDVLVx.text = $"{vel.x:+0.00;-0.00}";
            if (textDVLVy != null) textDVLVy.text = $"{vel.y:+0.00;-0.00}";
            if (textDVLVz != null) textDVLVz.text = $"{vel.z:+0.00;-0.00}";
            
            if (textDVLAlt != null)
                textDVLAlt.text = dvlPublisher.IsValid ? $"{dvlPublisher.LastAltitude:0.00} m" : "--- m";
            
            if (textDVLLock != null)
            {
                textDVLLock.text = $"{dvlPublisher.ValidBeamCount}/4 {(dvlPublisher.IsValid ? "LOCK" : "LOST")}";
                textDVLLock.RemoveFromClassList("status-ok");
                textDVLLock.RemoveFromClassList("status-bad");
                textDVLLock.AddToClassList(dvlPublisher.IsValid ? "status-ok" : "status-bad");
            }
        }
        
        // IMU Data
        if (imuPublisher != null)
        {
            var accel = imuPublisher.LastAcceleration;
            var angVel = imuPublisher.LastAngularVelocity;
            
            if (textIMUAx != null) textIMUAx.text = $"{accel.x:+0.0;-0.0}";
            if (textIMUAy != null) textIMUAy.text = $"{accel.y:+0.0;-0.0}";
            if (textIMUAz != null) textIMUAz.text = $"{accel.z:+0.0;-0.0}";
            
            if (textIMUWx != null) textIMUWx.text = $"{angVel.x:+0.00;-0.00}";
            if (textIMUWy != null) textIMUWy.text = $"{angVel.y:+0.00;-0.00}";
            if (textIMUWz != null) textIMUWz.text = $"{angVel.z:+0.00;-0.00}";
        }
    }
}
