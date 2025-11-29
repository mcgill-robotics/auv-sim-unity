using UnityEngine;
using RosMessageTypes.Auv;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using System.Collections;

public class DVLPublisher : ROSPublisher
{
    protected override string Topic => ROSSettings.Instance.DVLTopic;

    [Header("Physical Setup")]
    [Tooltip("AUV Rigidbody - used to calculate point velocity at sensor location")]
    public Rigidbody auvRb;
    
    [Tooltip("Layer for Pool Floor/Walls/Objects. MUST Exclude Water surface.")]
    public LayerMask acousticLayerMask;

    [Space(10)]
    [Header("A50 DVL Characteristics")]
    [Tooltip("Minimum altitude (m) - dead zone below which DVL cannot measure")]
    [Range(0.01f, 1.0f)]
    public float minAltitude = 0.05f;
    
    [Tooltip("Maximum altitude (m) - max range of acoustic beams")]
    [Range(1.0f, 100.0f)]
    public float maxAltitude = 50.0f;
    
    [Space(5)]
    [Tooltip("Horizontal velocity noise std dev (m/s). A50 has 22.5° beams - horizontal error is ~2.6x higher than vertical")]
    [Range(0.001f, 0.1f)]
    public float sigmaVelocityHorizontal = 0.01f;
    
    [Tooltip("Vertical velocity noise std dev (m/s)")]
    [Range(0.0001f, 0.05f)]
    public float sigmaVelocityVertical = 0.004f;
    
    [Space(10)]
    [Header("Simulation Options")]
    [Tooltip("If enabled, update rate adapts based on altitude (4-26Hz). Disable for fixed rate publishing")]
    public bool simulateAdaptiveRate = true;
    
    private VelocityReportMsg msg;
    private float nextPublishTime = 0;
    private System.Random random = new System.Random();

    protected override void Start()
    {
        base.Start();
        msg = new VelocityReportMsg();
        
        // Initialize covariance (Diagonal) based on A50 specs
        msg.covariance = new double[9]; 
    }

    protected override void FixedUpdate()
    {
        // Override standard ROSPublisher update because we handle our own adaptive timing
        if (!SimulationSettings.Instance.PublishDVL || auvRb == null) return;

        if (Time.time >= nextPublishTime)
        {
            PublishMessage();
        }
    }

    protected override void RegisterPublisher()
    {
        ros.RegisterPublisher<VelocityReportMsg>(Topic);
    }

    protected override void PublishMessage()
    {
        // 1. Get Ground Truth Velocity (Includes Lever Arm Effect)
        // The DVL measures velocity of the MOUNTING POINT, not the COM.
        Vector3 pointVel = auvRb.GetPointVelocity(transform.position);

        // 2. Transform to DVL Frame (Forward-Right-Down usually, or just Body Frame)
        // Let's assume the DVL is mounted facing Down.
        // However, for ROS standard, we usually publish in the Base_Link frame (FLU).
        // Let's convert World Velocity to Local Robot Velocity (FLU).
        Vector3 localVel = auvRb.transform.InverseTransformDirection(pointVel);
        
        // 3. Raycast for Altitude & Validity
        RaycastHit hit;
        bool isValid = Physics.Raycast(transform.position, -transform.up, out hit, maxAltitude, acousticLayerMask);

        // Enforce Dead Zone (Min Altitude)
        if (isValid && hit.distance < minAltitude) isValid = false;

        // 4. Populate Message
        if (isValid)
        {
            // Apply Anisotropic Noise (Horizontal is noisier than Vertical)
            msg.vx = localVel.z + GaussianNoise(sigmaVelocityHorizontal); // Unity Z is Forward (ROS X)
            msg.vy = -localVel.x + GaussianNoise(sigmaVelocityHorizontal); // Unity -X is Left (ROS Y)
            msg.vz = localVel.y + GaussianNoise(sigmaVelocityVertical); // Unity Y is Up (ROS Z)
            
            msg.altitude = hit.distance;
            msg.valid = true;
            msg.status = true; // A50 specific status flag
            
            // Update Covariance to reflect valid lock
            msg.covariance[0] = sigmaVelocityHorizontal * sigmaVelocityHorizontal; // Var X
            msg.covariance[4] = sigmaVelocityHorizontal * sigmaVelocityHorizontal; // Var Y
            msg.covariance[8] = sigmaVelocityVertical * sigmaVelocityVertical;     // Var Z
        }
        else
        {
            // Loss of Lock
            msg.vx = 0; msg.vy = 0; msg.vz = 0;
            msg.altitude = -1.0;
            msg.valid = false;
            msg.status = false;
            
            // Blow up covariance to tell Kalman Filter "Don't trust me"
            msg.covariance[0] = 10000; 
            msg.covariance[4] = 10000; 
            msg.covariance[8] = 10000;
        }

        // 5. Timing & Stamps
        var rosTime = ROSClock.GetROSTimestamp();
        msg.time = rosTime.sec + rosTime.nanosec * 1e-9;

        ros.Publish(Topic, msg);

        // 6. Calculate Next Update Time (Adaptive Rate)
        // A50: Speed of sound ~1500m/s. Round trip + processing.
        // Approx: 4Hz at max range, 26Hz at close range.
        float rate = 26f;
        if (simulateAdaptiveRate && isValid)
        {
            // Simple linear approx based on altitude
            // Deep (50m) -> 4Hz. Shallow (0m) -> 26Hz.
            rate = Mathf.Lerp(26f, 4f, (float)msg.altitude / 50.0f);
        }
        
        nextPublishTime = Time.time + (1.0f / rate);
    }

    private double GaussianNoise(float stdDev)
    {
        float u1 = 1.0f - (float)random.NextDouble();
        float u2 = 1.0f - (float)random.NextDouble();
        return stdDev * Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);
    }
}