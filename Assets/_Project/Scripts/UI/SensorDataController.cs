using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Handles the Sensors drawer UI: DVL/IMU/Pressure data display and visualization toggles.
/// Extracted from SimulatorHUD for better separation of concerns.
/// </summary>
public class SensorDataController
{
    // DVL Labels
    private Label textDVLVx, textDVLVy, textDVLVz;
    private Label textDVLAlt, textDVLLock;
    private Label labelDVL;
    private Toggle toggleDVLViz;
    
    // IMU Labels
    private Label textIMUAx, textIMUAy, textIMUAz;
    private Label textIMUWx, textIMUWy, textIMUWz;
    private Label labelIMU;
    private Toggle toggleIMUViz;
    
    // Pressure Labels
    private Label textPressureDepth;
    private Label textPressureValue;
    private Label labelPressure;
    private Toggle togglePressureViz;
    
    // Publisher References
    private DVLPublisher dvlPublisher;
    private IMUPublisher imuPublisher;
    private PressurePublisher pressurePublisher;
    
    public SensorDataController(VisualElement root)
    {
        QueryElements(root);
        RegisterCallbacks();
    }
    
    private void QueryElements(VisualElement root)
    {
        // DVL
        textDVLVx = root.Q<Label>("Text-DVLVx");
        textDVLVy = root.Q<Label>("Text-DVLVy");
        textDVLVz = root.Q<Label>("Text-DVLVz");
        textDVLAlt = root.Q<Label>("Text-DVLAlt");
        textDVLLock = root.Q<Label>("Text-DVLLock");
        toggleDVLViz = root.Q<Toggle>("Toggle-DVLViz");
        
        // IMU
        textIMUAx = root.Q<Label>("Text-IMUAx");
        textIMUAy = root.Q<Label>("Text-IMUAy");
        textIMUAz = root.Q<Label>("Text-IMUAz");
        textIMUWx = root.Q<Label>("Text-IMUWx");
        textIMUWy = root.Q<Label>("Text-IMUWy");
        textIMUWz = root.Q<Label>("Text-IMUWz");
        toggleIMUViz = root.Q<Toggle>("Toggle-IMUViz");
        
        // Pressure
        textPressureDepth = root.Q<Label>("Text-PressureDepth");
        textPressureValue = root.Q<Label>("Text-PressureValue");
        togglePressureViz = root.Q<Toggle>("Toggle-PressureViz");
        
        labelDVL = root.Q<Label>("Label-DVL");
        labelIMU = root.Q<Label>("Label-IMU");
        labelPressure = root.Q<Label>("Label-Pressure");
    }
    
    private void RegisterCallbacks()
    {
        if (toggleDVLViz != null)
        {
            toggleDVLViz.RegisterValueChangedCallback(evt => {
                if (dvlPublisher != null)
                {
                    dvlPublisher.SetVisualizationActive(evt.newValue);
                }
                SimulationSettings.Instance.VisualizeDVL = evt.newValue;
                SimulationSettings.Instance.SaveSettings();
            });
        }
        
        if (toggleIMUViz != null)
        {
            toggleIMUViz.RegisterValueChangedCallback(evt => {
                if (imuPublisher != null)
                {
                    imuPublisher.SetVisualizationActive(evt.newValue);
                }
                SimulationSettings.Instance.VisualizeIMU = evt.newValue;
                SimulationSettings.Instance.SaveSettings();
            });
        }
        
        if (togglePressureViz != null)
        {
            togglePressureViz.RegisterValueChangedCallback(evt => {
                if (pressurePublisher != null)
                {
                    pressurePublisher.SetVisualizationActive(evt.newValue);
                }
                SimulationSettings.Instance.VisualizePressure = evt.newValue;
                SimulationSettings.Instance.SaveSettings();
            });
        }
    }
    
    /// <summary>
    /// Find sensor publishers in the scene and apply saved visualization settings.
    /// </summary>
    public void FindPublishers()
    {
        dvlPublisher = Object.FindFirstObjectByType<DVLPublisher>();
        imuPublisher = Object.FindFirstObjectByType<IMUPublisher>();
        pressurePublisher = Object.FindFirstObjectByType<PressurePublisher>();
        
        // Apply saved visualization settings
        if (dvlPublisher != null)
        {
            dvlPublisher.SetVisualizationActive(SimulationSettings.Instance.VisualizeDVL);
            if (toggleDVLViz != null)
                toggleDVLViz.SetValueWithoutNotify(SimulationSettings.Instance.VisualizeDVL);
        }
        
        if (imuPublisher != null)
        {
            imuPublisher.SetVisualizationActive(SimulationSettings.Instance.VisualizeIMU);
            if (toggleIMUViz != null)
                toggleIMUViz.SetValueWithoutNotify(SimulationSettings.Instance.VisualizeIMU);
        }
        
        if (pressurePublisher != null)
        {
            pressurePublisher.SetVisualizationActive(SimulationSettings.Instance.VisualizePressure);
            if (togglePressureViz != null)
                togglePressureViz.SetValueWithoutNotify(SimulationSettings.Instance.VisualizePressure);
        }

        // Apply colors to UI labels
        UpdateLabelColors();
    }

    /// <summary>
    /// Syncs the HUD sensor names with the publishers' visualization colors.
    /// </summary>
    private void UpdateLabelColors()
    {
        if (dvlPublisher != null && labelDVL != null)
            labelDVL.style.color = new StyleColor(dvlPublisher.visualizationColor);
            
        if (imuPublisher != null && labelIMU != null)
            labelIMU.style.color = new StyleColor(imuPublisher.visualizationColor);
            
        if (pressurePublisher != null && labelPressure != null)
            labelPressure.style.color = new StyleColor(pressurePublisher.visualizationColor);
    }
    
    /// <summary>
    /// Update sensor value displays. Call this periodically (e.g., 10Hz) from Update().
    /// Displays values in sensor frame (exactly what is sent to ROS).
    /// </summary>
    public void UpdateSensorDataDisplay()
    {
        // DVL Data - Using RosVelocity (FRD frame: X=Forward, Y=Right, Z=Down)
        if (dvlPublisher != null)
        {
            var vel = dvlPublisher.RosVelocity;
            
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
        
        // IMU Data - Using ROS accessors (FLU frame: X=Forward, Y=Left, Z=Up)
        if (imuPublisher != null)
        {
            var accel = imuPublisher.RosAcceleration;
            var angVel = imuPublisher.RosAngularVelocity;
            
            if (textIMUAx != null) textIMUAx.text = $"{accel.x:+0.0;-0.0}";
            if (textIMUAy != null) textIMUAy.text = $"{accel.y:+0.0;-0.0}";
            if (textIMUAz != null) textIMUAz.text = $"{accel.z:+0.0;-0.0}";
            
            if (textIMUWx != null) textIMUWx.text = $"{angVel.x:+0.00;-0.00}";
            if (textIMUWy != null) textIMUWy.text = $"{angVel.y:+0.00;-0.00}";
            if (textIMUWz != null) textIMUWz.text = $"{angVel.z:+0.00;-0.00}";
        }
        
        // Pressure Data (scalar, no frame conversion needed)
        if (pressurePublisher != null)
        {
            if (textPressureDepth != null)
                textPressureDepth.text = $"{pressurePublisher.LastDepth:0.00} m";
            if (textPressureValue != null)
                textPressureValue.text = $"{pressurePublisher.LastPressure / 1000.0:0.0} kPa";
        }
    }
    
    /// <summary>
    /// Get depth publisher reference for camera feed controller.
    /// </summary>
    public CameraDepthPublisher GetDepthPublisher()
    {
        return Object.FindFirstObjectByType<CameraDepthPublisher>();
    }
}
