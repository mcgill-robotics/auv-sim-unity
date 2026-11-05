using UnityEngine;

/// <summary>
/// Central list of the ROS topic names and frame IDs the sim uses. The values serialized in each
/// scene are the ones that count; the defaults below match the AUV software.
///
/// FRAME CONVENTIONS (what each publisher actually sends):
/// - Body and world data use ROS FLU: +X forward, +Y left, +Z up. This covers the IMU
///   (imu_link), the ground-truth twist/accel/orientation (auv_link) and the ground-truth pose
///   and object map (pool_link, relative to the AUV's start pose projected to the surface).
/// - DVL velocity is in the DVL's own FRD frame (dvl_link): +X forward, +Y right, +Z down.
///   DVL dead reckoning and odometry positions are NED relative to the start pose.
/// - Depth is a positive number that grows as the AUV goes deeper.
/// - Camera images use optical frames: +Z out of the lens, +X right, +Y down in the image.
/// - Angular rates follow the right-hand rule about the axes of the frame they are expressed in.
/// </summary>
[DefaultExecutionOrder(-100)]
public class ROSSettings : MonoBehaviour
{
    public static ROSSettings Instance { get; private set; }

    [Header("Actuator Topics")]
    [Tooltip("Topic for individual thruster force commands (8 thrusters)")]
    public string ThrusterForcesTopic = "/propulsion/forces";

    [Tooltip("Grabber/dropper opening command (UInt8, 0 = closed, 255 = fully open). Matches the AUV planner's /actuators/grabber")]
    public string GrabberTopic = "/actuators/grabber";

    [Tooltip("Old dropper trigger topic (Bool). The AUV uses GrabberTopic instead")]
    [System.Obsolete("use GrabberTopic")]
    public string DropperTopic = "/auv/dropper/trigger";

    [Tooltip("Old gripper trigger topic (Bool). The AUV uses GrabberTopic instead")]
    [System.Obsolete("use GrabberTopic")]
    public string GripperTopic = "/auv/gripper/trigger";

    [Tooltip("Topic for torpedo launch command (Bool)")]
    public string TorpedoLaunchTopic = "/actuators/torpedo";

    [Tooltip("Topic for torpedo reset command (Bool)")]
    public string TorpedoResetTopic = "/auv/torpedo/reset";

    [Tooltip("Topic for triggering simulation reset from ROS 2 (Bool)")]
    public string SimulationResetTopic = "/simulation/reset";


    [Space(10)]
    [Header("Sensor Topics")]
    [Tooltip("DVL velocity and altitude data topic")]
    public string DVLTopic = "dvl/velocity";
    public string DVLDeadReckoningTopic = "dvl/dead_reckoning";
    public string DVLOdometryTopic = "dvl/odometry";

    [Tooltip("IMU orientation, gyro, and accelerometer data topic")]
    public string IMUTopic = "imu/data";

    [Tooltip("Depth sensor data topic")]
    public string DepthTopic = "/sensors/depth/z";

    [Tooltip("Front camera image topic")]
    public string FrontCameraTopic = "/zed/zed_node/rgb/color/rect/image";

    [Tooltip("Down camera image topic")]
    public string DownCameraTopic = "/down_cam/image_raw";

    [Tooltip("Front depth camera topic")]
    public string DepthCameraTopic = "/zed/zed_node/depth/depth_registered";

    [Tooltip("ROS clock topic for time synchronization")]
    public string ClockTopic = "/clock";

    [Tooltip("The AUV's state estimate (PoseStamped). Shown in the HUD telemetry and as the believed-pose marker")]
    public string StatePoseTopic = "/state/pose";

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

    [Header("Vision and Pingers")]
    [Tooltip("Pinger bearing topic used by the pinger bearing visualizer")]
    public string PingerBearingTopic = "/sensors/hydrophones/pinger_bearing";

    [Tooltip("Object map topic. The sim publishes ground truth here when SimulationSettings.PublishGTObjectMap is on")]
    public string VisionObjectMapTopic = "/vision/object_map";

    [Tooltip("VIO camera pose from ZED positional tracking")]
    public string VIOPoseTopic = "/vision/vio_pose";

    [Space(10)]

    [Header("Frames")]
    public string OdomFrameId = "odom";
    public string WorldFrameId = "pool_link";
    public string BaseLinkFrameId = "auv_link";
    public string ImuFrameId = "imu_link";
    public string DvlFrameId = "dvl_link";
    public string FrontCamFrameId = "zed_left_camera_frame_optical";
    public string DownCamFrameId = "down_camera_optical_frame";
    public string DepthCamFrameId = "zed_left_camera_frame_optical";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Only remove this component: the GameObject also carries SimulationSettings and others.
            Debug.LogWarning("[ROSSettings] A second ROSSettings was found; removing the duplicate component.");
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
