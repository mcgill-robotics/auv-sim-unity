using UnityEngine;

/// <summary>
/// Centralized registry for all ROS topic names and frame IDs.
/// 
/// COORDINATE FRAME CONVENTIONS:
/// - DVL (FRD): +X Forward, +Y Right, +Z Down
/// - IMU (FLU): +X Forward, +Y Left, +Z Up
/// - Depth: + Down (Positive value increasing with depth)
/// - Ground Truth (FLU): +X Forward, +Y Left, +Z Up
/// 
/// HANDEDNESS:
/// All angular data (velocities/accelerations) across all sensors follows the 
/// RIGHT-HAND RULE (positive = CCW when viewed along the axis).
/// </summary>
[DefaultExecutionOrder(-100)]
public class ROSSettings : MonoBehaviour
{
    public static ROSSettings Instance { get; private set; }

    [Header("Actuator Topics")]
    [Tooltip("Topic for individual thruster force commands (8 thrusters)")]
    public string ThrusterForcesTopic = "/propulsion/forces";
    
    [Tooltip("Topic for dropper trigger command (Bool)")]
    public string DropperTopic = "/auv/dropper/trigger";

    [Tooltip("Topic for torpedo launch command (Bool)")]
    public string TorpedoLaunchTopic = "/auv/torpedo/launch";

    [Tooltip("Topic for torpedo reset command (Bool)")]
    public string TorpedoResetTopic = "/auv/torpedo/reset";

    [Tooltip("Topic for torpedo launcher rotation (Float32)")]
    public string TorpedoRotationTopic = "/auv/torpedo/rotate";

    [Space(10)]
    [Header("Sensor Topics")]
    [Tooltip("DVL velocity and altitude data topic")]
    public string DVLTopic = "/sensors/dvl/data";
    
    [Tooltip("IMU orientation, gyro, and accelerometer data topic")]
    public string IMUTopic = "/sensors/imu/data";
    
    [Tooltip("Depth sensor data topic")]
    public string DepthTopic = "/sensors/depth/data";
    
    [Tooltip("Hydrophone bearing data topic")]
    public string HydrophonesTopic = "/sensors/hydrophones/data";
    
    [Tooltip("Front camera image topic")]
    public string FrontCameraTopic = "/zed2i/zed_node/stereo/image_rect_color";
    
    [Tooltip("Down camera image topic")]
    public string DownCameraTopic = "/down_cam/image_raw";
    
    [Tooltip("Front depth camera topic")]
    public string DepthCameraTopic = "/zed2i/zed_node/depth/image_rect";
    
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

    [Space(10)]
    [Header("Ground Truth")]
    [Tooltip("Ground truth velocity (linear + angular) in body frame")]
    public string GroundTruthTwistTopic = "/auv/ground_truth/twist";
    
    [Tooltip("Ground truth acceleration (linear + angular) in body frame")]
    public string GroundTruthAccelTopic = "/auv/ground_truth/accel";
    
    [Tooltip("Ground truth orientation quaternion")]
    public string GroundTruthOrientationTopic = "/auv/ground_truth/orientation";
    
    [Tooltip("Ground truth depth (positive down)")]
    public string GroundTruthDepthTopic = "/auv/ground_truth/depth";

    [Tooltip("Ground truth pose (relative to start)")]
    public string GroundTruthPoseTopic = "/auv/ground_truth/pose";

    [Header("Competition")]
    public string PingerBearingTopic = "/sensors/hydrophones/pinger_bearing";
    public string VisionDetectionFrameTopic = "/vision/detection_frame";
    public string VisionObjectMapTopic = "/vision/object_map";
    
    [Tooltip("VIO camera pose from ZED positional tracking")]
    public string VIOPoseTopic = "/vision/vio_pose";

    [Header("Frames")]
    public string WorldFrameId = "world";
    public string BaseLinkFrameId = "base_link";
    public string ImuFrameId = "imu_link";
    public string DvlFrameId = "dvl_link";
    public string FrontCamFrameId = "zed_left_camera_optical_frame";
    public string DownCamFrameId = "down_camera_optical_frame";
    public string DepthCamFrameId = "zed_left_camera_optical_frame";
    public string PressureFrameId = "pressure_link";

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
