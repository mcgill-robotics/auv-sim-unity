using UnityEngine;
using RosMessageTypes.Auv;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

public class DVLPublisher : ROSPublisher
{
    protected override string Topic => ROSSettings.Instance.DVLTopic;
    
    [Tooltip("Assign Diana's Rigidbody here")]
    public Rigidbody auvRb;

    private VelocityReportMsg msg;

    protected override void Start()
    {
        base.Start();
        msg = new VelocityReportMsg();
    }

    protected override void RegisterPublisher()
    {
        ros.RegisterPublisher<VelocityReportMsg>(Topic);
    }

    protected override void FixedUpdate()
    {
        if (!SimulationSettings.Instance.PublishDVL) return;
        base.FixedUpdate();
    }

    protected override void PublishMessage()
    {
        if (auvRb == null)
        {
            Debug.LogWarning("[DVLPublisher] AUV Rigidbody not assigned!");
            return;
        }

        // Reuse cached message object
        // VelocityReportMsg msg = new VelocityReportMsg(); // REMOVED ALLOCATION
        
        // 1. Calculate Local Velocity (Body Frame)
        // TODO: Apply lever arm effect if DVL is not at Center of Mass
        // V_sensor = V_com + omega x r_offset
        Vector3 localVel = auvRb.transform.InverseTransformDirection(auvRb.linearVelocity);
        
        // 2. Convert to ROS Coordinate System
        // Unity (Left Handed) -> ROS (Right Handed/FLU)
        // Using RUF conversion helper which handles the axis swapping
        // Note: To<FLU>() returns a Vector3<FLU> struct, we need to cast or access values directly
        var rosVelFlu = localVel.To<FLU>(); 

        msg.vx = rosVelFlu.x;
        msg.vy = rosVelFlu.y;
        msg.vz = rosVelFlu.z;

        // 3. Altitude (Raycast Down)
        // Raycast from the DVL sensor position (transform.position) downwards
        if (Physics.Raycast(transform.position, -transform.up, out RaycastHit hit))
        {
            msg.altitude = hit.distance;
            msg.valid = true;
        }
        else
        {
            msg.altitude = -1.0; // Standard convention for "bottom not found"
            msg.valid = false;
        }

        // VelocityReportMsg does not have a standard Header
        // It has a 'time' field (double)
        // We'll use the ROSClock time if available, or Time.time
        var rosTime = ROSClock.GetROSTimestamp();
        msg.time = rosTime.sec + rosTime.nanosec * 1e-9;

        ros.Publish(Topic, msg);
    }
}
