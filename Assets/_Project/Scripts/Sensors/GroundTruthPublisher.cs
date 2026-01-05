using UnityEngine;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;

/// <summary>
/// Publishes ground truth state of the AUV from the Rigidbody center.
/// Used for comparing against noisy sensor outputs and validating sensor fusion.
/// 
/// COORDINATE FRAME (UNITY):
/// - X = Right
/// - Y = Up
/// - Z = Forward
/// 
/// ROTATION HANDEDNESS:
/// All angular data follows the RIGHT-HAND RULE (positive = CCW when viewed along axis).
/// - Angular velocities are negated from Unity's default left-hand convention.
/// - Quaternion X and Z components are negated for right-handed coordinate system.
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
    private Float64Msg depthMsg;

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
        
        depthMsg = new Float64Msg();
        
        // Initialize previous velocity for acceleration calculation
        if (AuvRb != null)
        {
            prevVelocity = AuvRb.linearVelocity;
            prevAngularVelocity = AuvRb.angularVelocity;
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
        
        // Transform to body frame (local Unity coordinates: X=right, Y=up, Z=forward)
        Vector3 linearVelLocal = AuvRb.transform.InverseTransformDirection(linearVelWorld);
        Vector3 angularVelLocal = AuvRb.transform.InverseTransformDirection(angularVelWorld);
        
        // Publish in Unity frame with RIGHT-HAND RULE for rotations
        // Unity uses left-hand rule, so negate angular velocities to convert
        twistMsg.header.stamp = stamp;
        twistMsg.twist.linear.x = linearVelLocal.x;  // Right
        twistMsg.twist.linear.y = linearVelLocal.y;  // Up
        twistMsg.twist.linear.z = linearVelLocal.z;  // Forward
        twistMsg.twist.angular.x = -angularVelLocal.x;  // Roll rate (around X/Right axis) - negated for RH rule
        twistMsg.twist.angular.y = -angularVelLocal.y;  // Yaw rate (around Y/Up axis) - negated for RH rule
        twistMsg.twist.angular.z = -angularVelLocal.z;  // Pitch rate (around Z/Forward axis) - negated for RH rule
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
        
        // Publish in Unity frame with RIGHT-HAND RULE
        accelMsg.header.stamp = stamp;
        accelMsg.accel.linear.x = linearAccelLocal.x;
        accelMsg.accel.linear.y = linearAccelLocal.y;
        accelMsg.accel.linear.z = linearAccelLocal.z;
        accelMsg.accel.angular.x = -angularAccelLocal.x;  // Negated for RH rule
        accelMsg.accel.angular.y = -angularAccelLocal.y;  // Negated for RH rule
        accelMsg.accel.angular.z = -angularAccelLocal.z;  // Negated for RH rule
        ros.Publish(ROSSettings.Instance.GroundTruthAccelTopic, accelMsg);
        
        // === ORIENTATION ===
        // Convert Unity quaternion from left-handed to right-handed
        // For same axes but opposite handedness: negate ALL imaginary components (X, Y, Z)
        // This reverses the rotation direction to match right-hand rule
        Quaternion q = AuvRb.rotation;
        orientationMsg.header.stamp = stamp;
        orientationMsg.quaternion.x = -q.x;  // Negated for RH coordinate system
        orientationMsg.quaternion.y = -q.y;  // Negated for RH coordinate system
        orientationMsg.quaternion.z = -q.z;  // Negated for RH coordinate system
        orientationMsg.quaternion.w = q.w;   // W unchanged
        ros.Publish(ROSSettings.Instance.GroundTruthOrientationTopic, orientationMsg);
        
        // === DEPTH ===
        // Depth as positive-down (negate Unity Y which is up)
        depthMsg.data = -AuvRb.position.y;
        ros.Publish(ROSSettings.Instance.GroundTruthDepthTopic, depthMsg);
    }
}
