using UnityEngine;

[DefaultExecutionOrder(-100)]
public class ROSSettings : MonoBehaviour
{
    public static ROSSettings Instance { get; private set; }

    [Header("Actuator Topics")]
    [Tooltip("Topic for thruster command messages (legacy)")]
    public string ThrustersTopic = "/auv/thrusters";
    
    [Tooltip("Topic for individual thruster force commands (8 thrusters)")]
    public string ThrusterForcesTopic = "/auv/thruster_forces";
    
    [Tooltip("Topic for dropper trigger command (Bool)")]
    public string DropperTopic = "/auv/dropper";

    [Space(10)]
    [Header("Sensor Topics")]
    [Tooltip("DVL velocity and altitude data topic")]
    public string DVLTopic = "/sensors/dvl/data";
    
    [Tooltip("IMU orientation, gyro, and accelerometer data topic")]
    public string IMUTopic = "/sensor/imu/data";
    
    [Tooltip("Depth sensor data topic")]
    public string DepthTopic = "/sensors/depth";
    
    [Tooltip("Hydrophone bearing data topic")]
    public string HydrophonesTopic = "/sensors/hydrophones/data";
    
    [Tooltip("Front camera image topic")]
    public string FrontCameraTopic = "/sensors/camera/front/image_raw";
    
    [Tooltip("Down camera image topic")]
    public string DownCameraTopic = "/down_cam/image_raw";
    
    [Tooltip("Front depth camera topic")]
    public string DepthCameraTopic = "/sensors/camera/depth/image_raw";
    
    [Tooltip("ROS clock topic for time synchronization")]
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
