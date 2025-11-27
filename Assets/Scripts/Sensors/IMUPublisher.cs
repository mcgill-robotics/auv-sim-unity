using UnityEngine;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

public class IMUPublisher : ROSPublisher
{
    protected override string Topic => ROSSettings.Instance.IMUTopic;

    [Header("IMU Configuration")]
    public GameObject auv;
    public Rigidbody auvRb;

    [Header("Noise Parameters")]
    [Range(0.001f, 1.0f)] public float accelerometerNoise = 0.1f;
    [Range(0.001f, 1.0f)] public float gyroscopeNoise = 0.05f;
    [Range(0.001f, 0.1f)] public float accelerometerBias = 0.01f;
    [Range(0.001f, 0.1f)] public float gyroscopeBias = 0.01f;
    [Range(0.0001f, 0.01f)] public float randomWalkStepSize = 0.001f;

    private ImuMsg imuMsg;
    private Vector3 lastVelocity;
    private Vector3 gyroscopeBiasWalk;
    private System.Random random = new System.Random();

    protected override void Start()
    {
        base.Start();
        InitializeMessage();
    }

    protected override void RegisterPublisher()
    {
        ros.RegisterPublisher<ImuMsg>(Topic);
    }

    private void InitializeMessage()
    {
        // Only initialize if null
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
        // Check global toggle
        if (!SimulationSettings.Instance.PublishIMU) return;
        base.FixedUpdate();
    }

    protected override void PublishMessage()
    {
        Quaternion trueOrientation = auv.transform.rotation;
        imuMsg.orientation = trueOrientation.To<FLU>();

        Vector3 trueAngularVelocity = auvRb.angularVelocity;
        // TODO: Apply lever arm effect if IMU is not at Center of Mass
        // V_sensor = V_com + omega x r_offset
        Vector3 trueAcceleration = GetLinearAcceleration();

        ApplyMonteCarloNoise(ref trueAcceleration, ref trueAngularVelocity);

        imuMsg.angular_velocity = trueAngularVelocity.To<FLU>();
        imuMsg.linear_acceleration = trueAcceleration.To<FLU>();

        imuMsg.header.stamp = ROSClock.GetROSTimestamp();
        ros.Publish(Topic, imuMsg);
    }

    private Vector3 GetLinearAcceleration()
    {
        Vector3 currentVelocity = auvRb.linearVelocity;
        Vector3 acceleration = (currentVelocity - lastVelocity) / Time.fixedDeltaTime;
        lastVelocity = currentVelocity;
        return acceleration;
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
        gyroscopeBiasWalk.x += GenerateGaussianNoise() * randomWalkStepSize * gyroscopeBias;
        gyroscopeBiasWalk.y += GenerateGaussianNoise() * randomWalkStepSize * gyroscopeBias;
        gyroscopeBiasWalk.z += GenerateGaussianNoise() * randomWalkStepSize * gyroscopeBias;
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
