using UnityEngine;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

public class IMUPublisher : ROSPublisher
{
    protected override string Topic => ROSSettings.Instance.IMUTopic;

    [Header("Physical Setup")]
    [Tooltip("AUV Rigidbody - used to calculate point velocity and acceleration at sensor location")]
    public Rigidbody auvRb;

    [Space(10)]
    [Header("Accelerometer Noise Parameters")]
    [Tooltip("White noise std dev (m/s²) added to acceleration measurements")]
    [Range(0.001f, 1.0f)]
    public float accelerometerNoise = 0.1f;
    
    [Tooltip("Constant bias (m/s²) in accelerometer readings")]
    [Range(0.001f, 0.1f)]
    public float accelerometerBias = 0.01f;
    
    [Space(10)]
    [Header("Gyroscope Noise Parameters")]
    [Tooltip("White noise std dev (rad/s) added to angular velocity measurements")]
    [Range(0.001f, 1.0f)]
    public float gyroscopeNoise = 0.05f;
    
    [Tooltip("Initial bias (rad/s) in gyroscope readings - will drift over time")]
    [Range(0.001f, 0.1f)]
    public float gyroscopeBias = 0.01f;
    
    [Tooltip("Random walk step size for gyroscope bias drift")]
    [Range(0.0001f, 0.01f)]
    public float randomWalkStepSize = 0.001f;

    private ImuMsg imuMsg;
    private Vector3 lastPointVelocity; // Velocity of the SENSOR, not the Center of Mass
    private Vector3 gyroscopeBiasWalk;
    private System.Random random = new System.Random();

    protected override void Start()
    {
        base.Start();
        InitializeMessage();
        
        if (auvRb != null)
        {
            // Initialize velocity at the sensor's position
            lastPointVelocity = auvRb.GetPointVelocity(transform.position);
        }
    }

    protected override void RegisterPublisher()
    {
        ros.RegisterPublisher<ImuMsg>(Topic);
    }

    private void InitializeMessage()
    {
        if (imuMsg == null)
        {
            imuMsg = new ImuMsg();
            imuMsg.header = new HeaderMsg { frame_id = ROSSettings.Instance.ImuFrameId };
            imuMsg.orientation_covariance = new double[9];
            imuMsg.angular_velocity_covariance = new double[9];
            imuMsg.linear_acceleration_covariance = new double[9];
            SetCovarianceMatrices();
        }
    }

    protected override void FixedUpdate()
    {
        if (!SimulationSettings.Instance.PublishIMU || auvRb == null) return;
        PublishMessage();
    }

    protected override void PublishMessage()
    {
        float dt = Time.fixedDeltaTime;

        // 1. Orientation
        imuMsg.orientation = transform.rotation.To<FLU>();

        // 2. Angular Velocity (Gyro)
        // Note: Rotating objects have the same angular velocity everywhere on the body
        Vector3 currentAngularVel = auvRb.angularVelocity;
        
        // Transform to Local Sensor Frame (IMUs read in their own frame)
        Vector3 sensorAngularVel = transform.InverseTransformDirection(currentAngularVel);

        // 3. Linear Acceleration (Accelerometer)
        // "GetPointVelocity" automatically accounts for the rotation/lever-arm effect!
        Vector3 currentPointVelocity = auvRb.GetPointVelocity(transform.position);
        
        // Calculate World Acceleration (dv/dt)
        Vector3 worldAccel = (currentPointVelocity - lastPointVelocity) / dt;
        
        // IMPORTANT: Subtract Gravity to get "Proper Acceleration" (What a sensor actually feels)
        // Stationary sensor = (0 - (-9.81)) = +9.81 UP. This is correct.
        Vector3 properAccelWorld = worldAccel - Physics.gravity;

        // Transform to Local Sensor Frame
        Vector3 sensorAccel = transform.InverseTransformDirection(properAccelWorld);

        // 4. Apply Your Original Noise Model
        ApplyMonteCarloNoise(ref sensorAccel, ref sensorAngularVel);

        // 5. Publish
        imuMsg.linear_acceleration = sensorAccel.To<FLU>();
        imuMsg.angular_velocity = sensorAngularVel.To<FLU>();
        imuMsg.header.stamp = ROSClock.GetROSTimestamp();

        ros.Publish(Topic, imuMsg);

        // Update History
        lastPointVelocity = currentPointVelocity;
    }

    private void ApplyMonteCarloNoise(ref Vector3 acceleration, ref Vector3 angularVelocity)
    {
        UpdateRandomWalkBias();
        
        acceleration.x += GenerateGaussianNoise() * accelerometerNoise;
        acceleration.y += GenerateGaussianNoise() * accelerometerNoise;
        acceleration.z += GenerateGaussianNoise() * accelerometerNoise;

        angularVelocity.x += gyroscopeBiasWalk.x + GenerateGaussianNoise() * gyroscopeNoise;
        angularVelocity.y += gyroscopeBiasWalk.y + GenerateGaussianNoise() * gyroscopeNoise;
        angularVelocity.z += gyroscopeBiasWalk.z + GenerateGaussianNoise() * gyroscopeNoise;
    }

    private void UpdateRandomWalkBias()
    {
        // This is your original simple Random Walk
        gyroscopeBiasWalk.x += GenerateGaussianNoise() * randomWalkStepSize * gyroscopeBias;
        gyroscopeBiasWalk.y += GenerateGaussianNoise() * randomWalkStepSize * gyroscopeBias;
        gyroscopeBiasWalk.z += GenerateGaussianNoise() * randomWalkStepSize * gyroscopeBias;
        
        // Safety Clamp to prevent it from going to infinity over long runs
        gyroscopeBiasWalk = Vector3.ClampMagnitude(gyroscopeBiasWalk, gyroscopeBias * 10f);
    }

    private float GenerateGaussianNoise()
    {
        float u1 = (float)random.NextDouble();
        float u2 = (float)random.NextDouble();
        return Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);
    }

    private void SetCovarianceMatrices()
    {
        for (int i = 0; i < 9; i++) imuMsg.orientation_covariance[i] = (i % 4 == 0) ? -1.0 : 0.0;
        
        double gyroVariance = gyroscopeNoise * gyroscopeNoise;
        for (int i = 0; i < 9; i++) imuMsg.angular_velocity_covariance[i] = (i % 4 == 0) ? gyroVariance : 0.0;

        double accelVariance = accelerometerNoise * accelerometerNoise;
        for (int i = 0; i < 9; i++) imuMsg.linear_acceleration_covariance[i] = (i % 4 == 0) ? accelVariance : 0.0;
    }
}