using UnityEngine;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

/// <summary>
/// Publishes ground truth state of the AUV from the Rigidbody center.
/// Used for verifying sensor outputs and validating sensor fusion.
/// 
/// COORDINATE FRAME (ROS FLU):
/// - X = Forward (Unity Z)
/// - Y = Left    (Unity -X)
/// - Z = Up      (Unity Y)
/// 
/// This matches standard ROS conventions and the output of the IMUPublisher.
/// 
/// DEPTH CONVENTION:
/// - Depth is published as a positive value increasing with depth (+ Down).
/// </summary>
public class GroundTruthPublisher : ROSPublisher
{
    public override string Topic => ROSSettings.Instance != null ? ROSSettings.Instance.GroundTruthTwistTopic : null;

    [Header("Reference")]
    [Tooltip("AUV Rigidbody - leave empty to use SimulationSettings.AUVRigidbody")]
    [SerializeField] private Rigidbody auvRbOverride;
    
    private Rigidbody AuvRb => auvRbOverride != null ? auvRbOverride : SimulationSettings.Instance?.AUVRigidbody;

    // Messages
    private TwistStampedMsg twistMsg;           // Linear + Angular velocity
    private AccelStampedMsg accelMsg;           // Linear + Angular acceleration
    private QuaternionStampedMsg orientationMsg;
    private PoseStampedMsg poseMsg;             // Relative pose (Position + Orientation)
    private Float64Msg depthMsg;

    // Initial state for relative validation
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    // For acceleration calculation
    private Vector3 prevVelocity;
    private Vector3 prevAngularVelocity;

    protected override void Start()
    {
        base.Start();
        
        // Publish every FixedUpdate frame, no rate limiting
        useBaseRateLimiting = false;
        
        // Initialize messages with proper frame IDs
        twistMsg = new TwistStampedMsg();
        twistMsg.header = new HeaderMsg { frame_id = ROSSettings.Instance.BaseLinkFrameId };
        
        accelMsg = new AccelStampedMsg();
        accelMsg.header = new HeaderMsg { frame_id = ROSSettings.Instance.BaseLinkFrameId };
        
        orientationMsg = new QuaternionStampedMsg();
        orientationMsg.header = new HeaderMsg { frame_id = ROSSettings.Instance.BaseLinkFrameId };
        
        poseMsg = new PoseStampedMsg();
        // Pose is relative to the "world" or "odom" frame fixed at the start location
        poseMsg.header = new HeaderMsg { frame_id = ROSSettings.Instance.WorldFrameId };

        depthMsg = new Float64Msg();
        
        // Initialize previous velocity for acceleration calculation
        if (AuvRb != null)
        {
            prevVelocity = AuvRb.linearVelocity;
            prevAngularVelocity = AuvRb.angularVelocity;
            
            // Capture initial state
            initialPosition = AuvRb.position;
            initialRotation = AuvRb.rotation;
        }
    }

    protected override void FixedUpdate()
    {
        // Publish every physics frame if ROS is enabled
        if (SimulationSettings.Instance.PublishROS)
        {
            PublishMessage();
        }
    }

    protected override void RegisterPublisher()
    {
        ros.RegisterPublisher<TwistStampedMsg>(ROSSettings.Instance.GroundTruthTwistTopic);
        ros.RegisterPublisher<AccelStampedMsg>(ROSSettings.Instance.GroundTruthAccelTopic);
        ros.RegisterPublisher<QuaternionStampedMsg>(ROSSettings.Instance.GroundTruthOrientationTopic);
        ros.RegisterPublisher<PoseStampedMsg>(ROSSettings.Instance.GroundTruthPoseTopic);
        ros.RegisterPublisher<Float64Msg>(ROSSettings.Instance.GroundTruthDepthTopic);
    }

    public override void PublishMessage()
    {
        if (AuvRb == null) return;
        
        var stamp = ROSClock.GetROSTimestamp();
        float dt = Time.fixedDeltaTime;
        
        // === VELOCITIES ===
        // Get velocity at center of mass in world frame
        Vector3 linearVelWorld = AuvRb.linearVelocity;
        Vector3 angularVelWorld = AuvRb.angularVelocity;
        
        // Transform to body frame (local Unity coordinates)
        Vector3 linearVelLocal = AuvRb.transform.InverseTransformDirection(linearVelWorld);
        Vector3 angularVelLocal = AuvRb.transform.InverseTransformDirection(angularVelWorld);
        
        // PUBLISH IN ROS FLU FRAME
        twistMsg.header.stamp = stamp;
        
        // Linear: Direct mapping via ROSGeometry
        twistMsg.twist.linear = linearVelLocal.To<FLU>();

        // Angular: Pseudovector mapping (Unity -> FLU)
        // Unity (Right, Up, Fwd) -> FLU (Fwd, Left, Up)
        // Rule: x_ros = -z_unity, y_ros = x_unity, z_ros = -y_unity
        twistMsg.twist.angular = new Vector3Msg
        {
            x = -angularVelLocal.z,
            y = angularVelLocal.x,
            z = -angularVelLocal.y
        };
        ros.Publish(ROSSettings.Instance.GroundTruthTwistTopic, twistMsg);
        
        // === ACCELERATION ===
        // Calculate acceleration via finite difference
        Vector3 linearAccelWorld = (AuvRb.linearVelocity - prevVelocity) / dt;
        Vector3 angularAccelWorld = (AuvRb.angularVelocity - prevAngularVelocity) / dt;
        
        // Store for next frame
        prevVelocity = AuvRb.linearVelocity;
        prevAngularVelocity = AuvRb.angularVelocity;
        
        // Transform to body frame
        Vector3 linearAccelLocal = AuvRb.transform.InverseTransformDirection(linearAccelWorld);
        Vector3 angularAccelLocal = AuvRb.transform.InverseTransformDirection(angularAccelWorld);
        
        // PUBLISH IN ROS FLU FRAME
        accelMsg.header.stamp = stamp;
        
        // Linear: Direct mapping via ROSGeometry
        accelMsg.accel.linear = linearAccelLocal.To<FLU>();
        
        // Angular: Pseudovector mapping
        accelMsg.accel.angular = new Vector3Msg
        {
            x = -angularAccelLocal.z,
            y = angularAccelLocal.x,
            z = -angularAccelLocal.y
        };
        ros.Publish(ROSSettings.Instance.GroundTruthAccelTopic, accelMsg);
        
        // === ORIENTATION ===
        // Convert Unity quaternion to ROS FLU
        orientationMsg.header.stamp = stamp;
        orientationMsg.quaternion = AuvRb.rotation.To<FLU>();
        ros.Publish(ROSSettings.Instance.GroundTruthOrientationTopic, orientationMsg);
        
        // === RELATIVE POSE ===
        // Calculate relative position and rotation from start
        if (poseMsg != null)
        {
            poseMsg.header.stamp = stamp;
            
            // Relative Position (vector from start to current, ROTATED into start frame)
            // This makes the position relative to the "orientation in which we started"
            Vector3 worldDisp = AuvRb.position - initialPosition;
            Vector3 relPos = Quaternion.Inverse(initialRotation) * worldDisp;
            
            // Convert to FLU:
            // Unity Z (Local Fwd) -> ROS X (Relative)
            // Unity X (Local Right) -> ROS -Y (Relative)
            // Unity Y (Absolute Up) -> ROS Z (Absolute)
            // We use absolute altitude/depth for Z, but relative X/Y
            poseMsg.pose.position.x = relPos.z;
            poseMsg.pose.position.y = -relPos.x;
            poseMsg.pose.position.z = AuvRb.position.y; // Absolute vertical position (FLU Z = Up)
            
            // Relative Rotation (rotation from start to current)
            // q_rel = Inverse(q_start) * q_current
            Quaternion relRot = Quaternion.Inverse(initialRotation) * AuvRb.rotation;
            poseMsg.pose.orientation = relRot.To<FLU>();
            
            ros.Publish(ROSSettings.Instance.GroundTruthPoseTopic, poseMsg);
        }
        
        // === DEPTH ===
        // Depth as positive-down (negate Unity Y which is up)
        depthMsg.data = -AuvRb.position.y;
        ros.Publish(ROSSettings.Instance.GroundTruthDepthTopic, depthMsg);
    }
}
