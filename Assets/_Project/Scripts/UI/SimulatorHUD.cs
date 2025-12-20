using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Robotics.ROSTCPConnector;
using System.Collections.Generic;

/// <summary>
/// Main HUD coordinator. Manages drawer system, logging, competition UI, and delegates
/// specific functionality to specialized controllers.
/// </summary>
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
    
    // Controllers (initialized in OnEnable)
    private SettingsController settingsController;
    private TelemetryController telemetryController;
    private CameraFeedController cameraFeedController;
    private SensorDataController sensorDataController;
    
    // UI Update Throttling (Performance Optimization)
    private float lastUIUpdateTime;
    private const float UI_UPDATE_INTERVAL = 0.1f; // 10Hz

    // UI Elements - Competition
    private Label textTimer;
    private Label textScore;
    private DropdownField dropdownTask;
    private Button btnStartComp;

    // UI Elements - Logging
    private Label textLog;
    private Label logToggle;
    private ScrollView logScrollView;
    private VisualElement logPanel;
    private List<string> logLines = new List<string>();
    private const int MAX_LOG_LINES = 50;

    // UI Elements - Drawers
    private VisualElement configDrawer;
    private VisualElement controlsDrawer;
    private VisualElement telemetryDrawer;
    private VisualElement sensorsDrawer;
    private VisualElement cameraDrawer;
    
    // Drawer Indicators
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
        if (uiDocument == null) return;
        
        var root = uiDocument.rootVisualElement;
        
        root.focusable = false;
        
        // Block ALL keyboard events from affecting UI unless we're typing in a text field.
        // This prevents WASD/arrow keys from scrolling UI elements while allowing normal text input.
        EventCallback<KeyDownEvent> blockKeyboardEvents = evt => {
            bool isTyping = root.focusController.focusedElement is TextInputBaseField<string> || 
                            root.focusController.focusedElement is TextField ||
                            root.focusController.focusedElement is IntegerField ||
                            root.focusController.focusedElement is FloatField ||
                            root.focusController.focusedElement is DoubleField;
            if (!isTyping)
            {
                evt.PreventDefault();
                evt.StopImmediatePropagation();
            }
        };
        
        EventCallback<KeyUpEvent> blockKeyUpEvents = evt => {
            bool isTyping = root.focusController.focusedElement is TextInputBaseField<string> || 
                            root.focusController.focusedElement is TextField ||
                            root.focusController.focusedElement is IntegerField ||
                            root.focusController.focusedElement is FloatField ||
                            root.focusController.focusedElement is DoubleField;
            if (!isTyping)
            {
                evt.PreventDefault();
                evt.StopImmediatePropagation();
            }
        };
        
        EventCallback<NavigationMoveEvent> blockNavigationEvents = evt => {
            bool isTyping = root.focusController.focusedElement is TextInputBaseField<string> || 
                            root.focusController.focusedElement is TextField ||
                            root.focusController.focusedElement is IntegerField ||
                            root.focusController.focusedElement is FloatField ||
                            root.focusController.focusedElement is DoubleField;
            if (!isTyping)
            {
                evt.PreventDefault();
                evt.StopImmediatePropagation();
            }
        };
        
        // Also block NavigationSubmitEvent (triggered by Space/Enter on buttons)
        EventCallback<NavigationSubmitEvent> blockNavigationSubmit = evt => {
            bool isTyping = root.focusController.focusedElement is TextInputBaseField<string> || 
                            root.focusController.focusedElement is TextField ||
                            root.focusController.focusedElement is IntegerField ||
                            root.focusController.focusedElement is FloatField ||
                            root.focusController.focusedElement is DoubleField;
            if (!isTyping)
            {
                evt.PreventDefault();
                evt.StopImmediatePropagation();
            }
        };
        
        root.RegisterCallback<KeyDownEvent>(blockKeyboardEvents, TrickleDown.TrickleDown);
        root.RegisterCallback<KeyUpEvent>(blockKeyUpEvents, TrickleDown.TrickleDown);
        root.RegisterCallback<NavigationMoveEvent>(blockNavigationEvents, TrickleDown.TrickleDown);
        root.RegisterCallback<NavigationSubmitEvent>(blockNavigationSubmit, TrickleDown.TrickleDown);

        // Query drawer elements
        configDrawer = root.Q<VisualElement>("ConfigDrawer");
        controlsDrawer = root.Q<VisualElement>("ControlsDrawer");
        telemetryDrawer = root.Q<VisualElement>("TelemetryDrawer");
        sensorsDrawer = root.Q<VisualElement>("SensorsDrawer");
        cameraDrawer = root.Q<VisualElement>("CameraDrawer");
        
        // Drawer indicators
        configIndicator = root.Q<Label>("ConfigDrawer-Indicator");
        telemetryIndicator = root.Q<Label>("TelemetryDrawer-Indicator");
        sensorsIndicator = root.Q<Label>("SensorsDrawer-Indicator");
        cameraIndicator = root.Q<Label>("CameraDrawer-Indicator");
        
        // Competition elements
        textTimer = root.Q<Label>("Text-Timer");
        textScore = root.Q<Label>("Text-Score");
        dropdownTask = root.Q<DropdownField>("Dropdown-Task");
        btnStartComp = root.Q<Button>("Btn-StartComp");
        
        // Logging elements
        textLog = root.Q<Label>("Text-Log");
        logToggle = root.Q<Label>("LogPanel-Toggle");
        logPanel = root.Q<VisualElement>("LogPanel");
        logScrollView = root.Q<ScrollView>("LogScroll");
        
        // Initialize drawers
        InitializeDrawers();
        
        // Click-to-blur handler
        root.RegisterCallback<PointerDownEvent>(evt => {
            if (evt.target == root)
            {
                root.focusController.focusedElement?.Blur();
            }
        });
    }
    
    // --- Drawer System ---
    
    private void InitializeDrawers()
    {
        var root = uiDocument.rootVisualElement;
        
        RegisterDrawerTab("ConfigDrawer-Tab", configDrawer, "Config");
        RegisterDrawerTab("ControlsDrawer-Tab", controlsDrawer, "Controls");
        RegisterDrawerTab("TelemetryDrawer-Tab", telemetryDrawer, "Telemetry");
        RegisterDrawerTab("SensorsDrawer-Tab", sensorsDrawer, "Sensors");
        RegisterDrawerTab("CameraDrawer-Tab", cameraDrawer, "Camera");
        
        // Load saved drawer states
        ApplyDrawerState(configDrawer, SimulationSettings.Instance.DrawerConfigOpen);
        ApplyDrawerState(controlsDrawer, SimulationSettings.Instance.DrawerControlsOpen);
        ApplyDrawerState(telemetryDrawer, SimulationSettings.Instance.DrawerTelemetryOpen);
        ApplyDrawerState(sensorsDrawer, SimulationSettings.Instance.DrawerSensorsOpen);
        ApplyDrawerState(cameraDrawer, SimulationSettings.Instance.DrawerCameraOpen);
        
        // Log panel toggle
        if (logToggle != null && logPanel != null)
        {
            logToggle.RegisterCallback<PointerDownEvent>(evt => {
                ToggleLogPanel();
                evt.StopPropagation();
            });
        }
    }
    
    private void ApplyDrawerState(VisualElement drawer, bool isOpen)
    {
        if (drawer == null) return;
        
        if (isOpen)
        {
            drawer.RemoveFromClassList(DRAWER_COLLAPSED_CLASS);
            drawer.AddToClassList(DRAWER_OPEN_CLASS);
        }
        else
        {
            drawer.RemoveFromClassList(DRAWER_OPEN_CLASS);
            drawer.AddToClassList(DRAWER_COLLAPSED_CLASS);
        }
    }
    
    private void RegisterDrawerTab(string tabName, VisualElement drawer, string drawerName)
    {
        if (drawer == null) return;
        
        var root = uiDocument.rootVisualElement;
        var tab = root.Q<VisualElement>(tabName);
        
        if (tab != null)
        {
            tab.RegisterCallback<PointerDownEvent>(evt => {
                ToggleDrawer(drawer, drawerName);
                evt.StopPropagation();
            });
        }
    }
    
    private void ToggleDrawer(VisualElement drawer, string drawerName)
    {
        if (drawer == null) return;
        
        bool isOpen = drawer.ClassListContains(DRAWER_OPEN_CLASS);
        bool newState = !isOpen;
        
        if (isOpen)
        {
            drawer.RemoveFromClassList(DRAWER_OPEN_CLASS);
            drawer.AddToClassList(DRAWER_COLLAPSED_CLASS);
        }
        else
        {
            drawer.RemoveFromClassList(DRAWER_COLLAPSED_CLASS);
            drawer.AddToClassList(DRAWER_OPEN_CLASS);
        }
        
        SaveDrawerState(drawerName, newState);
    }
    
    private void SaveDrawerState(string drawerName, bool isOpen)
    {
        if (SimulationSettings.Instance == null) return;
        
        switch (drawerName)
        {
            case "Config": SimulationSettings.Instance.DrawerConfigOpen = isOpen; break;
            case "Controls": SimulationSettings.Instance.DrawerControlsOpen = isOpen; break;
            case "Telemetry": SimulationSettings.Instance.DrawerTelemetryOpen = isOpen; break;
            case "Sensors": SimulationSettings.Instance.DrawerSensorsOpen = isOpen; break;
            case "Camera": SimulationSettings.Instance.DrawerCameraOpen = isOpen; break;
        }
        
        SimulationSettings.Instance.SaveSettings();
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

    // --- Initialization ---
    
    private IEnumerator Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        var root = uiDocument.rootVisualElement;
        
        // Initialize controllers
        settingsController = new SettingsController(root, ros, Log);
        telemetryController = new TelemetryController(root, ros);
        sensorDataController = new SensorDataController(root);
        cameraFeedController = new CameraFeedController(root, frontLeftCamera, downCamera, Log);
        
        // Load settings to UI
        settingsController.LoadSettingsToUI();
        
        // Subscribe to ROS telemetry
        if (ros != null)
        {
            telemetryController.SubscribeToTelemetry();
            settingsController.UpdateROSConnectionState();
        }
        
        // Wait for camera textures to initialize
        yield return new WaitForEndOfFrame();
        
        // Find publishers and initialize camera dropdown
        sensorDataController.FindPublishers();
        cameraFeedController.InitializeCameraDropdown(sensorDataController.GetDepthPublisher());
        
        Log("HUD initialized successfully.");
    }

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
            // Maintain a rolling list of log lines for performance
            logLines.Add($"> {message}");
            if (logLines.Count > MAX_LOG_LINES)
            {
                logLines.RemoveAt(0);
            }

            textLog.text = string.Join("\n", logLines);
            
            // Auto-scroll to bottom
            if (logScrollView != null)
            {
                logScrollView.schedule.Execute(() => {
                    logScrollView.scrollOffset = new Vector2(0, logScrollView.contentContainer.layout.height);
                });
            }
        }
    }

    // --- Update Loop ---

    private void Update()
    {
        if (uiDocument == null) return;
        
        // Input focus detection
        var focusedElement = uiDocument.rootVisualElement.focusController.focusedElement;
        
        IsInputFocused = focusedElement is TextInputBaseField<string> || 
                         focusedElement is TextField ||
                         focusedElement is IntegerField ||
                         focusedElement is FloatField ||
                         focusedElement is DoubleField;

        if (IsInputFocused && Input.GetKeyDown(KeyCode.Escape))
        {
            focusedElement?.Blur();
            IsInputFocused = false;
        }

        // Camera shortcuts (only if not typing)
        if (!IsInputFocused && InputManager.Instance != null)
        {
            if (Input.GetKeyDown(InputManager.Instance.GetKey("cameraSnapshotKeybind", KeyCode.P)))
            {
                cameraFeedController?.OnSaveImage();
            }
            if (Input.GetKeyDown(InputManager.Instance.GetKey("cameraCycleKeybind", KeyCode.V)))
            {
                cameraFeedController?.CycleCamera();
            }
        }
        
        // Throttled sensor data updates (10Hz)
        if (Time.time - lastUIUpdateTime > UI_UPDATE_INTERVAL)
        {
            sensorDataController?.UpdateSensorDataDisplay();
            lastUIUpdateTime = Time.time;
        }
    }

    private void OnDestroy()
    {
        // Controllers don't own any resources that need cleanup
    }
}
