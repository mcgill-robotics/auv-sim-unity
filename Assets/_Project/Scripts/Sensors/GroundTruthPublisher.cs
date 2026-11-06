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

    // Message Pools
    private TwistStampedMsg[] twistMsgPool;
    private AccelStampedMsg[] accelMsgPool;
    private QuaternionStampedMsg[] orientationMsgPool;
    private PoseStampedMsg[] poseMsgPool;
    private Float64Msg[] depthMsgPool;
    private int poolIdx = 0;

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
        
        // Pre-allocate 3-slot ring pools for zero-allocation publishing
        twistMsgPool = new TwistStampedMsg[3];
        accelMsgPool = new AccelStampedMsg[3];
        orientationMsgPool = new QuaternionStampedMsg[3];
        poseMsgPool = new PoseStampedMsg[3];
        depthMsgPool = new Float64Msg[3];

        for (int i = 0; i < 3; i++)
        {
            twistMsgPool[i] = new TwistStampedMsg
            {
                header = new HeaderMsg { frame_id = ROSSettings.Instance.BaseLinkFrameId, stamp = new RosMessageTypes.BuiltinInterfaces.TimeMsg() },
                twist = new TwistMsg { linear = new Vector3Msg(), angular = new Vector3Msg() }
            };

            accelMsgPool[i] = new AccelStampedMsg
            {
                header = new HeaderMsg { frame_id = ROSSettings.Instance.BaseLinkFrameId, stamp = new RosMessageTypes.BuiltinInterfaces.TimeMsg() },
                accel = new AccelMsg { linear = new Vector3Msg(), angular = new Vector3Msg() }
            };

            orientationMsgPool[i] = new QuaternionStampedMsg
            {
                header = new HeaderMsg { frame_id = ROSSettings.Instance.BaseLinkFrameId, stamp = new RosMessageTypes.BuiltinInterfaces.TimeMsg() },
                quaternion = new QuaternionMsg()
            };

            poseMsgPool[i] = new PoseStampedMsg
            {
                header = new HeaderMsg { frame_id = ROSSettings.Instance.WorldFrameId, stamp = new RosMessageTypes.BuiltinInterfaces.TimeMsg() },
                pose = new PoseMsg { position = new PointMsg(), orientation = new QuaternionMsg() }
            };

            depthMsgPool[i] = new Float64Msg();
        }
        
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
        if (AuvRb == null || twistMsgPool == null) return;

        int idx = poolIdx;
        poolIdx = (poolIdx + 1) % 3;

        var twistMsg = twistMsgPool[idx];
        var accelMsg = accelMsgPool[idx];
        var orientationMsg = orientationMsgPool[idx];
        var poseMsg = poseMsgPool[idx];
        var depthMsg = depthMsgPool[idx];

        ROSClock.PopulateROSTimestamp(twistMsg.header.stamp);
        int sec = twistMsg.header.stamp.sec;
        uint nanosec = twistMsg.header.stamp.nanosec;

        accelMsg.header.stamp.sec = sec;
        accelMsg.header.stamp.nanosec = nanosec;
        orientationMsg.header.stamp.sec = sec;
        orientationMsg.header.stamp.nanosec = nanosec;
        poseMsg.header.stamp.sec = sec;
        poseMsg.header.stamp.nanosec = nanosec;

        float dt = Time.fixedDeltaTime;
        
        // === VELOCITIES ===
        // Get velocity at center of mass in world frame
        Vector3 linearVelWorld = AuvRb.linearVelocity;
        Vector3 angularVelWorld = AuvRb.angularVelocity;
        
        // Transform to body frame (local Unity coordinates)
        Vector3 linearVelLocal = AuvRb.transform.InverseTransformDirection(linearVelWorld);
        Vector3 angularVelLocal = AuvRb.transform.InverseTransformDirection(angularVelWorld);
        
        // Linear: Direct mapping Unity RUF -> ROS FLU (z, -x, y)
        twistMsg.twist.linear.x = linearVelLocal.z;
        twistMsg.twist.linear.y = -linearVelLocal.x;
        twistMsg.twist.linear.z = linearVelLocal.y;

        // Angular: Pseudovector mapping (Unity -> FLU): (-z, x, -y)
        twistMsg.twist.angular.x = -angularVelLocal.z;
        twistMsg.twist.angular.y = angularVelLocal.x;
        twistMsg.twist.angular.z = -angularVelLocal.y;

        Publish(ROSSettings.Instance.GroundTruthTwistTopic, twistMsg);
        
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
        
        // Linear: Unity RUF -> ROS FLU (z, -x, y)
        accelMsg.accel.linear.x = linearAccelLocal.z;
        accelMsg.accel.linear.y = -linearAccelLocal.x;
        accelMsg.accel.linear.z = linearAccelLocal.y;
        
        // Angular: Pseudovector mapping (-z, x, -y)
        accelMsg.accel.angular.x = -angularAccelLocal.z;
        accelMsg.accel.angular.y = angularAccelLocal.x;
        accelMsg.accel.angular.z = -angularAccelLocal.y;

        Publish(ROSSettings.Instance.GroundTruthAccelTopic, accelMsg);
        
        // === ORIENTATION ===
        // Unity RUF -> ROS FLU: (z, -x, y, -w)
        Quaternion rot = AuvRb.rotation;
        orientationMsg.quaternion.x = rot.z;
        orientationMsg.quaternion.y = -rot.x;
        orientationMsg.quaternion.z = rot.y;
        orientationMsg.quaternion.w = -rot.w;
        Publish(ROSSettings.Instance.GroundTruthOrientationTopic, orientationMsg);
        
        // === RELATIVE POSE ===
        // Relative Position (vector from start to current, ROTATED into start frame)
        Vector3 worldDisp = AuvRb.position - initialPosition;
        Vector3 relPos = Quaternion.Inverse(initialRotation) * worldDisp;
        
        poseMsg.pose.position.x = relPos.z;
        poseMsg.pose.position.y = -relPos.x;
        poseMsg.pose.position.z = AuvRb.position.y; // Absolute vertical position (FLU Z = Up)
        
        // Relative Rotation (rotation from start to current)
        Quaternion relRot = Quaternion.Inverse(initialRotation) * AuvRb.rotation;
        poseMsg.pose.orientation.x = relRot.z;
        poseMsg.pose.orientation.y = -relRot.x;
        poseMsg.pose.orientation.z = relRot.y;
        poseMsg.pose.orientation.w = -relRot.w;
        
        Publish(ROSSettings.Instance.GroundTruthPoseTopic, poseMsg);
        
        // === DEPTH ===
        // Depth as positive-down (negate Unity Y which is up)
        depthMsg.data = -AuvRb.position.y;
        Publish(ROSSettings.Instance.GroundTruthDepthTopic, depthMsg);
    }
}
