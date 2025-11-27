using UnityEngine;

public class ROSSettings : MonoBehaviour
{
    public static ROSSettings Instance { get; private set; }

    [Header("Actuators")]
    public string ThrustersTopic = "/auv/thrusters";
    public string ThrusterForcesTopic = "/auv/thruster_forces";
    public string DropperTopic = "/auv/dropper";

    [Header("Sensors")]
    public string DVLTopic = "/sensors/dvl/data";
    public string IMUTopic = "/sensor/imu/data";
    public string DepthTopic = "/sensors/depth";
    public string HydrophonesTopic = "/sensors/hydrophones/data";
    public string FrontCameraTopic = "/sensors/camera/front/image_raw";
    public string DownCameraTopic = "/down_cam/image_raw";
    public string DepthCameraTopic = "/sensors/camera/depth/image_raw";
    public string ClockTopic = "/clock";

    [Header("Control")]
    public string PIDEnableTopic = "/auv/pid/enable";
    public string PIDSetpointTopic = "/auv/pid/setpoint";
    
    // PID Individual Axis Topics
    public string PidXEnableTopic = "/auv/pid/x/enable";
    public string PidYEnableTopic = "/auv/pid/y/enable";
    public string PidZEnableTopic = "/auv/pid/z/enable";
    public string PidQuatEnableTopic = "/auv/pid/quat/enable";
    
    public string PidXSetpointTopic = "/auv/pid/x/setpoint";
    public string PidYSetpointTopic = "/auv/pid/y/setpoint";
    public string PidZSetpointTopic = "/auv/pid/z/setpoint";
    public string PidQuatSetpointTopic = "/auv/pid/quat/setpoint";

    public string StateTopic = "/auv/state";
    
    // State Components
    public string StateXTopic = "/auv/state/x";
    public string StateYTopic = "/auv/state/y";
    public string StateZTopic = "/auv/state/z";
    public string StateThetaXTopic = "/auv/state/theta_x";
    public string StateThetaYTopic = "/auv/state/theta_y";
    public string StateThetaZTopic = "/auv/state/theta_z";

    [Header("Competition")]
    public string PingerBearingTopic = "/sensors/hydrophones/pinger_bearing";
    public string VisionDetectionFrameTopic = "/vision/detection_frame";
    public string VisionObjectMapTopic = "/vision/object_map";

    [Header("Frames")]
    public string WorldFrameId = "world";
    public string BaseLinkFrameId = "base_link";
    public string ImuFrameId = "imu_link";
    public string DvlFrameId = "dvl_link";
    public string FrontCamFrameId = "zed_left_camera_optical_frame";
    public string DownCamFrameId = "down_camera_optical_frame";
    public string DepthCamFrameId = "zed_left_camera_optical_frame";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
