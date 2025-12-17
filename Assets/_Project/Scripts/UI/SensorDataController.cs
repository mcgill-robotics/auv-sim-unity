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
    private Toggle toggleDVLViz;
    
    // IMU Labels
    private Label textIMUAx, textIMUAy, textIMUAz;
    private Label textIMUWx, textIMUWy, textIMUWz;
    private Toggle toggleIMUViz;
    
    // Pressure Labels
    private Label textPressureDepth;
    private Label textPressureValue;
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
    }
    
    /// <summary>
    /// Update sensor value displays. Call this periodically (e.g., 10Hz) from Update().
    /// </summary>
    public void UpdateSensorDataDisplay()
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
        
        // Pressure Data
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
