using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Handles the Sensors drawer UI: DVL/IMU/Pressure data display and visualization toggles.
/// Toggles apply immediately and mark the settings unsaved; only the Config drawer's SAVE button
/// writes them to disk.
/// </summary>
public class SensorDataController : IDisposable
{
    // DVL Labels
    private Label textDVLVx, textDVLVy, textDVLVz;
    private Label textDVLAlt, textDVLLock;
    private Label labelDVL;
    
    // DVL DR Labels (Optional, if present in UXML)
    private Label textDVLPosX, textDVLPosY, textDVLPosZ;
    
    private Toggle toggleDVLViz;
    
    // IMU Labels
    private Label textIMUAx, textIMUAy, textIMUAz;
    private Label textIMUWx, textIMUWy, textIMUWz;
    private Label textIMURoll;
    private Label textIMUPitch;
    private Label textIMUYaw;
    private Label labelIMU;
    private Toggle toggleIMUViz;
    
    // Depth Labels
    private Label textDepthValue;
    private Label labelDepth;
    private Toggle toggleDepthViz;
    
    // Noise Toggles
    private Toggle toggleDVLNoise;
    private Toggle toggleIMUNoise;
    private Toggle toggleDepthNoise;
    
    // Publisher References
    private DVLPublisher dvlPublisher;
    private IMUPublisher imuPublisher;
    private DepthPublisher depthPublisher;
    
    // Undo actions for every callback registered on the UI, run in Dispose.
    private readonly List<Action> unregister = new List<Action>();

    public SensorDataController(VisualElement root)
    {
        QueryElements(root);
        RegisterCallbacks();
    }
    
    private void QueryElements(VisualElement root)
    {
        // DVL
        textDVLVx = root.Q<Label>(HudIds.TextDVLVx);
        textDVLVy = root.Q<Label>(HudIds.TextDVLVy);
        textDVLVz = root.Q<Label>(HudIds.TextDVLVz);
        textDVLAlt = root.Q<Label>(HudIds.TextDVLAlt);
        textDVLLock = root.Q<Label>(HudIds.TextDVLLock);
        
        // DR Pos Labels - allow them to be null if not in UXML yet
        textDVLPosX = root.Q<Label>(HudIds.TextDVLPosX);
        textDVLPosY = root.Q<Label>(HudIds.TextDVLPosY);
        textDVLPosZ = root.Q<Label>(HudIds.TextDVLPosZ);
        
        toggleDVLViz = root.Q<Toggle>(HudIds.ToggleDVLViz);
        toggleDVLNoise = root.Q<Toggle>(HudIds.ToggleDVLNoise);
        
        // IMU
        textIMUAx = root.Q<Label>(HudIds.TextIMUAx);
        textIMUAy = root.Q<Label>(HudIds.TextIMUAy);
        textIMUAz = root.Q<Label>(HudIds.TextIMUAz);
        textIMUWx = root.Q<Label>(HudIds.TextIMUWx);
        textIMUWy = root.Q<Label>(HudIds.TextIMUWy);
        textIMUWz = root.Q<Label>(HudIds.TextIMUWz);
        textIMURoll = root.Q<Label>(HudIds.TextIMURoll);
        textIMUPitch = root.Q<Label>(HudIds.TextIMUPitch);
        textIMUYaw = root.Q<Label>(HudIds.TextIMUYaw);
        toggleIMUViz = root.Q<Toggle>(HudIds.ToggleIMUViz);
        toggleIMUNoise = root.Q<Toggle>(HudIds.ToggleIMUNoise);
        
        // Depth
        textDepthValue = root.Q<Label>(HudIds.TextDepthValue);
        toggleDepthViz = root.Q<Toggle>(HudIds.ToggleDepthViz);
        toggleDepthNoise = root.Q<Toggle>(HudIds.ToggleDepthNoise);
        
        labelDVL = root.Q<Label>(HudIds.LabelDVL);
        labelIMU = root.Q<Label>(HudIds.LabelIMU);
        labelDepth = root.Q<Label>(HudIds.LabelDepth);
    }
    
    private void RegisterCallbacks()
    {
        Bind(toggleDVLViz, v =>
        {
            if (dvlPublisher != null) dvlPublisher.SetVisualizationActive(v);
            SimulationSettings.Instance.VisualizeDVL = v;
        });
        Bind(toggleIMUViz, v =>
        {
            if (imuPublisher != null) imuPublisher.SetVisualizationActive(v);
            SimulationSettings.Instance.VisualizeIMU = v;
        });
        Bind(toggleDepthViz, v =>
        {
            if (depthPublisher != null) depthPublisher.SetVisualizationActive(v);
            SimulationSettings.Instance.VisualizeDepth = v;
        });

        Bind(toggleDVLNoise, v =>
        {
            if (dvlPublisher != null) dvlPublisher.enableNoise = v;
            SimulationSettings.Instance.EnableDVLNoise = v;
        });
        Bind(toggleIMUNoise, v =>
        {
            if (imuPublisher != null) imuPublisher.enableNoise = v;
            SimulationSettings.Instance.EnableIMUNoise = v;
        });
        Bind(toggleDepthNoise, v =>
        {
            if (depthPublisher != null) depthPublisher.enableNoise = v;
            SimulationSettings.Instance.EnableDepthNoise = v;
        });
    }

    private void Bind(Toggle toggle, Action<bool> apply)
    {
        if (toggle == null) return;
        EventCallback<ChangeEvent<bool>> cb = evt =>
        {
            if (SimulationSettings.Instance == null) return;
            apply(evt.newValue);
            SimulationSettings.Instance.MarkUnsaved();
        };
        toggle.RegisterValueChangedCallback(cb);
        unregister.Add(() => toggle.UnregisterValueChangedCallback(cb));
    }

    /// <summary>Removes every UI callback this controller registered.</summary>
    public void Dispose()
    {
        foreach (Action a in unregister) a();
        unregister.Clear();
    }

    /// <summary>
    /// Find sensor publishers in the scene and apply saved visualization settings.
    /// </summary>
    public void FindPublishers()
    {
        if (SimulationSettings.Instance == null) return;

        dvlPublisher = UnityEngine.Object.FindFirstObjectByType<DVLPublisher>();
        imuPublisher = UnityEngine.Object.FindFirstObjectByType<IMUPublisher>();
        depthPublisher = UnityEngine.Object.FindFirstObjectByType<DepthPublisher>();
        
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
        
        if (depthPublisher != null)
        {
            depthPublisher.SetVisualizationActive(SimulationSettings.Instance.VisualizeDepth);
            if (toggleDepthViz != null)
                toggleDepthViz.SetValueWithoutNotify(SimulationSettings.Instance.VisualizeDepth);
        }
        // Apply saved Noise Settings (Source of Truth is SimulationSettings)
        if (dvlPublisher != null) 
            dvlPublisher.enableNoise = SimulationSettings.Instance.EnableDVLNoise;
            
        if (toggleDVLNoise != null)
            toggleDVLNoise.SetValueWithoutNotify(SimulationSettings.Instance.EnableDVLNoise);
            
        if (imuPublisher != null)
            imuPublisher.enableNoise = SimulationSettings.Instance.EnableIMUNoise;
            
        if (toggleIMUNoise != null)
            toggleIMUNoise.SetValueWithoutNotify(SimulationSettings.Instance.EnableIMUNoise);
            
        if (depthPublisher != null)
            depthPublisher.enableNoise = SimulationSettings.Instance.EnableDepthNoise;
            
        if (toggleDepthNoise != null)
            toggleDepthNoise.SetValueWithoutNotify(SimulationSettings.Instance.EnableDepthNoise);
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
            
        if (depthPublisher != null && labelDepth != null)
            labelDepth.style.color = new StyleColor(depthPublisher.visualizationColor);
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
            
            if (textDVLVx != null) textDVLVx.text = HudFormat.Num(vel.x, "+0.00;-0.00");
            if (textDVLVy != null) textDVLVy.text = HudFormat.Num(vel.y, "+0.00;-0.00");
            if (textDVLVz != null) textDVLVz.text = HudFormat.Num(vel.z, "+0.00;-0.00");
            
            if (textDVLAlt != null)
                textDVLAlt.text = dvlPublisher.IsValid ? HudFormat.Num(dvlPublisher.LastAltitude, "0.00") + " m" : "--- m";
            
            if (textDVLLock != null)
            {
                textDVLLock.text = $"{dvlPublisher.ValidBeamCount}/4 {(dvlPublisher.IsValid ? "LOCK" : "LOST")}";
                textDVLLock.RemoveFromClassList("status-ok");
                textDVLLock.RemoveFromClassList("status-bad");
                textDVLLock.AddToClassList(dvlPublisher.IsValid ? "status-ok" : "status-bad");
            }
            
            // DVL Dead Reckoning Position (Odom Frame)
            var pose = dvlPublisher.RosDeadReckoningPosition;
            if (textDVLPosX != null) textDVLPosX.text = HudFormat.Num(pose.x, "+0.00;-0.00");
            if (textDVLPosY != null) textDVLPosY.text = HudFormat.Num(pose.y, "+0.00;-0.00");
            if (textDVLPosZ != null) textDVLPosZ.text = HudFormat.Num(pose.z, "+0.00;-0.00");
        }
        
        // IMU Data - Using ROS accessors (FLU frame: X=Forward, Y=Left, Z=Up)
        if (imuPublisher != null)
        {
            var accel = imuPublisher.RosAcceleration;
            var angVel = imuPublisher.RosAngularVelocity;
            
            if (textIMUAx != null) textIMUAx.text = HudFormat.Num(accel.x, "+0.0;-0.0");
            if (textIMUAy != null) textIMUAy.text = HudFormat.Num(accel.y, "+0.0;-0.0");
            if (textIMUAz != null) textIMUAz.text = HudFormat.Num(accel.z, "+0.0;-0.0");
            
            if (textIMUWx != null) textIMUWx.text = HudFormat.Num(angVel.x, "+0.00;-0.00");
            if (textIMUWy != null) textIMUWy.text = HudFormat.Num(angVel.y, "+0.00;-0.00");
            if (textIMUWz != null) textIMUWz.text = HudFormat.Num(angVel.z, "+0.00;-0.00");
            
            if (textIMURoll != null || textIMUPitch != null || textIMUYaw != null)
            {
                bool hasOrientation = imuPublisher.publishOrientation;
                Vector3 rpy = hasOrientation ? HudFormat.RollPitchYawDegrees(imuPublisher.RosOrientation) : Vector3.zero;
                if (textIMURoll != null) textIMURoll.text = hasOrientation ? HudFormat.Num(rpy.x, "F1") + "°" : "---";
                if (textIMUPitch != null) textIMUPitch.text = hasOrientation ? HudFormat.Num(rpy.y, "F1") + "°" : "---";
                if (textIMUYaw != null) textIMUYaw.text = hasOrientation ? HudFormat.Num(rpy.z, "F1") + "°" : "---";
            }
        }
        
        // Depth Data
        if (depthPublisher != null)
        {
            if (textDepthValue != null)
                textDepthValue.text = HudFormat.Num(depthPublisher.LastDepth, "0.00") + " m";
        }
    }
    
    /// <summary>
    /// Get depth publisher reference for camera feed controller.
    /// </summary>
    public CameraDepthPublisher GetDepthPublisher()
    {
        return UnityEngine.Object.FindFirstObjectByType<CameraDepthPublisher>();
    }
}
