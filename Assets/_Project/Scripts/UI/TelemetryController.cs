using System;
using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Handles the Telemetry drawer: shows the AUV's own state estimate from the state pose topic
/// (position in metres, roll/pitch/yaw in degrees, ROS FLU).
/// ROS-TCP-Connector runs subscriber callbacks from its Update on the main thread, so the labels
/// can be written directly.
/// </summary>
public class TelemetryController : IDisposable
{
    private Label textPosX, textPosY, textPosZ;
    private Label textRotX, textRotY, textRotZ;

    private readonly ROSConnection ros;
    private string subscribedTopic;

    public TelemetryController(VisualElement root, ROSConnection rosConnection)
    {
        ros = rosConnection;
        QueryElements(root);
    }

    private void QueryElements(VisualElement root)
    {
        textPosX = root.Q<Label>(HudIds.TextPosX);
        textPosY = root.Q<Label>(HudIds.TextPosY);
        textPosZ = root.Q<Label>(HudIds.TextPosZ);
        textRotX = root.Q<Label>(HudIds.TextRotX);
        textRotY = root.Q<Label>(HudIds.TextRotY);
        textRotZ = root.Q<Label>(HudIds.TextRotZ);
    }

    /// <summary>
    /// Subscribe to the AUV's state pose for telemetry display.
    /// </summary>
    public void SubscribeToTelemetry()
    {
        if (ros == null || ROSSettings.Instance == null || subscribedTopic != null) return;

        subscribedTopic = ROSSettings.Instance.StatePoseTopic;
        SharedRosSubscription.Add<PoseStampedMsg>(ros, subscribedTopic, OnStatePose);
    }

    private void OnStatePose(PoseStampedMsg msg)
    {
        var p = msg.pose.position;
        SetText(textPosX, HudFormat.Num(p.x, "F2") + " m");
        SetText(textPosY, HudFormat.Num(p.y, "F2") + " m");
        SetText(textPosZ, HudFormat.Num(p.z, "F2") + " m");

        var o = msg.pose.orientation;
        Vector3 rpy = HudFormat.RollPitchYawDegrees(new Quaternion((float)o.x, (float)o.y, (float)o.z, (float)o.w));
        SetText(textRotX, HudFormat.Num(rpy.x, "F1") + "°");
        SetText(textRotY, HudFormat.Num(rpy.y, "F1") + "°");
        SetText(textRotZ, HudFormat.Num(rpy.z, "F1") + "°");
    }

    private static void SetText(Label label, string text)
    {
        if (label != null) label.text = text;
    }

    /// <summary>Stops listening to the state pose topic.</summary>
    public void Dispose()
    {
        if (subscribedTopic == null) return;
        SharedRosSubscription.Remove<PoseStampedMsg>(subscribedTopic, OnStatePose);
        subscribedTopic = null;
    }
}
