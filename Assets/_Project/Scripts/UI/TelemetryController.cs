using UnityEngine;
using UnityEngine.UIElements;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

/// <summary>
/// Handles the Telemetry drawer UI: subscribes to ROS state topics and updates position/rotation labels.
/// Extracted from SimulatorHUD for better separation of concerns.
/// </summary>
public class TelemetryController
{
    // UI Elements - Position & Orientation
    private Label textPosX, textPosY, textPosZ;
    private Label textRotX, textRotY, textRotZ;
    private Label textMissionStatus;
    
    private ROSConnection ros;
    
    public TelemetryController(VisualElement root, ROSConnection rosConnection)
    {
        ros = rosConnection;
        QueryElements(root);
    }
    
    private void QueryElements(VisualElement root)
    {
        textPosX = root.Q<Label>("Text-PosX");
        textPosY = root.Q<Label>("Text-PosY");
        textPosZ = root.Q<Label>("Text-PosZ");
        textRotX = root.Q<Label>("Text-RotX");
        textRotY = root.Q<Label>("Text-RotY");
        textRotZ = root.Q<Label>("Text-RotZ");
        textMissionStatus = root.Q<Label>("Text-MissionStatus");
    }
    
    /// <summary>
    /// Subscribe to ROS state topics for telemetry display.
    /// </summary>
    public void SubscribeToTelemetry()
    {
        if (ros == null) return;
        
        ros.Subscribe<Float64Msg>(ROSSettings.Instance.StateXTopic, msg => UpdateLabel(textPosX, $"{msg.data:F2} m"));
        ros.Subscribe<Float64Msg>(ROSSettings.Instance.StateYTopic, msg => UpdateLabel(textPosY, $"{msg.data:F2} m"));
        ros.Subscribe<Float64Msg>(ROSSettings.Instance.StateZTopic, msg => UpdateLabel(textPosZ, $"{msg.data:F2} m"));
        
        ros.Subscribe<Float64Msg>(ROSSettings.Instance.StateThetaXTopic, msg => UpdateLabel(textRotX, $"{msg.data:F1}°"));
        ros.Subscribe<Float64Msg>(ROSSettings.Instance.StateThetaYTopic, msg => UpdateLabel(textRotY, $"{msg.data:F1}°"));
        ros.Subscribe<Float64Msg>(ROSSettings.Instance.StateThetaZTopic, msg => UpdateLabel(textRotZ, $"{msg.data:F1}°"));
    }
    
    private void UpdateLabel(Label label, string text)
    {
        // UI Toolkit is not thread safe, must run on main thread
        UnityMainThreadDispatcher.Instance().Enqueue(() => {
            if (label != null) label.text = text;
        });
    }
    
    /// <summary>
    /// Update mission status text.
    /// </summary>
    public void SetMissionStatus(string status)
    {
        if (textMissionStatus != null)
        {
            textMissionStatus.text = status;
        }
    }
}
