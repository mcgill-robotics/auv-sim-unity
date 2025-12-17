using UnityEngine;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using Utils;

public class IMUPublisher : ROSPublisher
{
    public override string Topic => ROSSettings.Instance.IMUTopic;

    [Header("Physical Setup")]
    [Tooltip("AUV Rigidbody - used to calculate point velocity and acceleration at sensor location")]
    public Rigidbody auvRb;

    [Space(10)]
    [Header("Accelerometer Noise")]
    [Tooltip("White noise std dev (m/s²) added to acceleration measurements")]
    [Range(0.001f, 1.0f)]
    public float accelNoise = 0.1f;
    
    [Space(5)]
    [Header("Accelerometer Bias (Gauss-Markov)")]
    [Tooltip("Correlation time for accelerometer bias (seconds)")]
    [Range(10f, 1000f)]
    public float accelBiasCorrelationTime = 300f;
    
    [Tooltip("Steady-state accelerometer bias std dev (m/s²)")]
    [Range(0.001f, 0.1f)]
    public float accelBiasSigma = 0.02f;
    
    [Space(10)]
    [Header("Gyroscope Noise")]
    [Tooltip("White noise std dev (rad/s) added to angular velocity measurements")]
    [Range(0.001f, 1.0f)]
    public float gyroNoise = 0.05f;
    
    [Space(5)]
    [Header("Gyroscope Bias (Gauss-Markov)")]
    [Tooltip("Correlation time for gyroscope bias (seconds). Longer = slower drift.")]
    [Range(10f, 500f)]
    public float gyroBiasCorrelationTime = 100f;
    
    [Tooltip("Steady-state gyroscope bias std dev (rad/s)")]
    [Range(0.0001f, 0.05f)]
    public float gyroBiasSigma = 0.01f;

    [Space(10)]
    [Header("Orientation Output Mode")]
    [Tooltip("If false, orientation covariance is set to -1 (Raw IMU mode - EKF should ignore). If true, orientation is corrupted with AHRS-style noise.")]
    public bool publishOrientation = true;
    
    [Tooltip("Simulate AHRS drift: add orientation noise. Only applies if publishOrientation = true.")]
    public bool simulateAHRS = true;
    
    [Space(5)]
    [Tooltip("Roll orientation noise (degrees) - typically small")]
    [Range(0.01f, 2f)]
    public float orientationNoiseRoll = 0.5f;
    
    [Tooltip("Pitch orientation noise (degrees) - typically small")]
    [Range(0.01f, 2f)]
    public float orientationNoisePitch = 0.5f;
    
    [Tooltip("Yaw/Heading orientation noise (degrees) - typically larger due to magnetometer drift")]
    [Range(0.1f, 10f)]
    public float orientationNoiseYaw = 2.0f;

    [Space(10)]
    [Header("Visualization")]
    [Tooltip("Enable LineRenderer visualization of acceleration and angular velocity")]
    public bool enableVisualization = true;
    
    [Tooltip("Scale factor for acceleration arrow")]
    [Range(0.01f, 0.5f)]
    public float accelArrowScale = 0.1f;
    
    [Tooltip("Scale factor for angular velocity arrows")]
    [Range(0.1f, 2f)]
    public float angVelArrowScale = 0.5f;

    // Public properties for UI/other scripts
    public Vector3 LastAcceleration { get; private set; }
    public Vector3 LastAngularVelocity { get; private set; }
    public Quaternion LastOrientation { get; private set; }
    public Vector3 CurrentGyroBias => gyroBias?.CurrentBias ?? Vector3.zero;
    public Vector3 CurrentAccelBias => accelBias?.CurrentBias ?? Vector3.zero;

    // Internals
    private ImuMsg imuMsg;
    private Vector3 lastPointVelocity;
    private GaussMarkovVector gyroBias;
    private GaussMarkovVector accelBias;
    
    // Visualization (3D mesh arrows)
    private GameObject accelArrow;
    private GameObject[] angVelArrows; // RGB for XYZ
    private GameObject visualizationRoot;
    private Material accelMat;
    private Material[] angVelMats;

    protected override void Start()
    {
        base.Start();
        InitializeMessage();
        
        if (auvRb != null)
        {
            lastPointVelocity = auvRb.GetPointVelocity(transform.position);
        }
        
        // Initialize Gauss-Markov bias models with pre-calculated coefficients
        float dt = Time.fixedDeltaTime;
        gyroBias = new GaussMarkovVector(gyroBiasCorrelationTime, gyroBiasSigma, dt);
        accelBias = new GaussMarkovVector(accelBiasCorrelationTime, accelBiasSigma, dt);
        
        // Always setup visualization objects (so they can be toggled at runtime)
        SetupVisualization();
        if (visualizationRoot != null)
        {
            visualizationRoot.SetActive(enableVisualization);
        }
    }

    protected override void RegisterPublisher()
    {
        ros.RegisterPublisher<ImuMsg>(Topic);
    }

    private void InitializeMessage()
    {
        imuMsg = new ImuMsg();
        imuMsg.header = new HeaderMsg { frame_id = ROSSettings.Instance.ImuFrameId };
        imuMsg.orientation_covariance = new double[9];
        imuMsg.angular_velocity_covariance = new double[9];
        imuMsg.linear_acceleration_covariance = new double[9];
        SetCovarianceMatrices();
    }

    private void SetupVisualization()
    {
        visualizationRoot = new GameObject("IMU_Visualization");
        visualizationRoot.transform.SetParent(transform);
        visualizationRoot.transform.localPosition = Vector3.zero;
        visualizationRoot.transform.localRotation = Quaternion.identity;
        
        // Create materials using shared utility
        accelMat = VisualizationUtils.CreateMaterial(new Color(1f, 0.2f, 0.8f)); // Magenta
        
        angVelMats = new Material[3];
        angVelMats[0] = VisualizationUtils.CreateMaterial(Color.red);   // X
        angVelMats[1] = VisualizationUtils.CreateMaterial(Color.green); // Y
        angVelMats[2] = VisualizationUtils.CreateMaterial(Color.blue);  // Z
        
        // Create acceleration arrow
        accelArrow = VisualizationUtils.CreateArrow("AccelArrow", accelMat, 0.03f);
        accelArrow.transform.SetParent(visualizationRoot.transform);
        accelArrow.SetActive(false);
        
        // Create angular velocity arrows (RGB for XYZ)
        angVelArrows = new GameObject[3];
        string[] names = { "AngVel_X", "AngVel_Y", "AngVel_Z" };
        
        for (int i = 0; i < 3; i++)
        {
            angVelArrows[i] = VisualizationUtils.CreateArrow(names[i], angVelMats[i], 0.02f);
            angVelArrows[i].transform.SetParent(visualizationRoot.transform);
            angVelArrows[i].SetActive(false);
        }
    }


    protected override void FixedUpdate()
    {
        if (auvRb == null) return;
        
        // Bias models must update every physics step for accurate simulation
        gyroBias.Step();
        accelBias.Step();
        
        // Always simulate sensor for visualization (updates LastAcceleration, LastAngularVelocity, etc.)
        SimulateSensor();
        
        // Only publish to ROS if enabled
        if (SimulationSettings.Instance.PublishIMU && SimulationSettings.Instance.PublishROS)
        {
            // Let base class handle publish rate timing
            base.FixedUpdate();
        }
        
        // Update visualization every frame regardless of publish rate
        if (enableVisualization && visualizationRoot != null)
        {
            UpdateVisualization();
        }
    }

    public override void PublishMessage()
    {
        // Timestamp and publish the pre-calculated message
        imuMsg.header.stamp = ROSClock.GetROSTimestamp();
        ros.Publish(Topic, imuMsg);
    }
    
    /// <summary>
    /// Simulate the IMU sensor: calculate acceleration, angular velocity, orientation.
    /// This runs independently of ROS publishing for visualization support.
    /// </summary>
    private void SimulateSensor()
    {
        float dt = Time.fixedDeltaTime;

        // 1. Angular Velocity (Gyroscope)
        Vector3 currentAngularVelWorld = auvRb.angularVelocity;
        Vector3 sensorAngularVel = transform.InverseTransformDirection(currentAngularVelWorld);
        
        // Apply noise: White noise + Gauss-Markov bias
        Vector3 noisyAngVel = sensorAngularVel;
        noisyAngVel += Stochastic.GenerateWhiteNoiseVector(gyroNoise);
        noisyAngVel += gyroBias.CurrentBias;
        
        LastAngularVelocity = noisyAngVel;

        // 2. Linear Acceleration (Accelerometer)
        Vector3 currentPointVelocity = auvRb.GetPointVelocity(transform.position);
        Vector3 worldAccel = (currentPointVelocity - lastPointVelocity) / dt;
        
        // Proper Acceleration = Kinematic - Gravity (what sensor actually feels)
        Vector3 properAccelWorld = worldAccel - Physics.gravity;
        Vector3 sensorAccel = transform.InverseTransformDirection(properAccelWorld);
        
        // Apply noise: White noise + Gauss-Markov bias
        Vector3 noisyAccel = sensorAccel;
        noisyAccel += Stochastic.GenerateWhiteNoiseVector(accelNoise);
        noisyAccel += accelBias.CurrentBias;
        
        LastAcceleration = noisyAccel;
        lastPointVelocity = currentPointVelocity;

        // 3. Orientation
        Quaternion trueOrientation = transform.rotation;
        
        if (publishOrientation)
        {
            if (simulateAHRS)
            {
                // Apply AHRS-style orientation noise
                float noiseRoll = Stochastic.GenerateGaussian() * orientationNoiseRoll;
                float noisePitch = Stochastic.GenerateGaussian() * orientationNoisePitch;
                float noiseYaw = Stochastic.GenerateGaussian() * orientationNoiseYaw;
                
                Quaternion noiseRotation = Quaternion.Euler(noiseRoll, noiseYaw, noisePitch);
                LastOrientation = trueOrientation * noiseRotation;
            }
            else
            {
                LastOrientation = trueOrientation;
            }
            imuMsg.orientation = LastOrientation.To<FLU>();
        }
        else
        {
            // Raw IMU mode - orientation is unknown
            LastOrientation = Quaternion.identity;
            imuMsg.orientation = new QuaternionMsg { x = 0, y = 0, z = 0, w = 0 };
        }

        // 4. Populate message (ready for publishing)
        imuMsg.linear_acceleration = noisyAccel.To<FLU>();
        imuMsg.angular_velocity = noisyAngVel.To<FLU>();
    }

    private void SetCovarianceMatrices()
    {
        // Orientation covariance
        if (publishOrientation)
        {
            float rollVar = (orientationNoiseRoll * Mathf.Deg2Rad) * (orientationNoiseRoll * Mathf.Deg2Rad);
            float pitchVar = (orientationNoisePitch * Mathf.Deg2Rad) * (orientationNoisePitch * Mathf.Deg2Rad);
            float yawVar = (orientationNoiseYaw * Mathf.Deg2Rad) * (orientationNoiseYaw * Mathf.Deg2Rad);
            imuMsg.orientation_covariance[0] = rollVar;
            imuMsg.orientation_covariance[4] = pitchVar;
            imuMsg.orientation_covariance[8] = yawVar;
        }
        else
        {
            // Set first element to -1 to indicate orientation is unknown (ROS convention)
            imuMsg.orientation_covariance[0] = -1.0;
        }
        
        // Gyroscope covariance
        double gyroVariance = gyroNoise * gyroNoise;
        for (int i = 0; i < 9; i++) 
            imuMsg.angular_velocity_covariance[i] = (i % 4 == 0) ? gyroVariance : 0.0;

        // Accelerometer covariance
        double accelVariance = accelNoise * accelNoise;
        for (int i = 0; i < 9; i++) 
            imuMsg.linear_acceleration_covariance[i] = (i % 4 == 0) ? accelVariance : 0.0;
    }

    private void UpdateVisualization()
    {
        // Acceleration arrow
        if (accelArrow != null)
        {
            Vector3 accelWorld = transform.TransformDirection(LastAcceleration);
            float magnitude = accelWorld.magnitude;
            
            if (magnitude > 0.1f)
            {
                accelArrow.SetActive(true);
                accelArrow.transform.position = transform.position;
                
                // Point in acceleration direction
                accelArrow.transform.rotation = Quaternion.LookRotation(accelWorld) * Quaternion.Euler(90, 0, 0);
                
                // Scale based on magnitude
                float length = magnitude * accelArrowScale;
                accelArrow.transform.localScale = new Vector3(1, length, 1);
            }
            else
            {
                accelArrow.SetActive(false);
            }
        }
        
        // Angular velocity arrows
        if (angVelArrows != null)
        {
            Vector3[] localAxes = { Vector3.right, Vector3.up, Vector3.forward };
            float[] angVelComponents = { LastAngularVelocity.x, LastAngularVelocity.y, LastAngularVelocity.z };
            
            for (int i = 0; i < 3; i++)
            {
                if (angVelArrows[i] != null)
                {
                    float mag = angVelComponents[i];
                    
                    if (Mathf.Abs(mag) > 0.01f)
                    {
                        Vector3 axisWorld = transform.TransformDirection(localAxes[i]);
                        float length = Mathf.Abs(mag) * angVelArrowScale;
                        
                        angVelArrows[i].SetActive(true);
                        angVelArrows[i].transform.position = transform.position;
                        
                        // Point in axis direction (positive or negative based on sign)
                        Vector3 dir = axisWorld * Mathf.Sign(mag);
                        angVelArrows[i].transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(90, 0, 0);
                        angVelArrows[i].transform.localScale = new Vector3(1, length, 1);
                    }
                    else
                    {
                        angVelArrows[i].SetActive(false);
                    }
                }
            }
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying) return;
        
        // Recalculate bias coefficients if parameters change
        float dt = Time.fixedDeltaTime;
        gyroBias?.RecalculateConstants(gyroBiasCorrelationTime, gyroBiasSigma, dt);
        accelBias?.RecalculateConstants(accelBiasCorrelationTime, accelBiasSigma, dt);
        
        // Update covariance matrices
        if (imuMsg != null) SetCovarianceMatrices();
        
        // Toggle visualization
        if (visualizationRoot != null) visualizationRoot.SetActive(enableVisualization);
    }
    
    /// <summary>
    /// Sets the visualization GameObject active state. Called by UI toggles.
    /// </summary>
    public void SetVisualizationActive(bool active)
    {
        enableVisualization = active;

        if (visualizationRoot != null)
        {
            visualizationRoot.SetActive(active);
        }
    }

    private void OnDestroy()
    {
        if (visualizationRoot != null) Destroy(visualizationRoot);
        // Cleanup runtime-created materials to prevent memory leaks
        if (accelMat != null) Destroy(accelMat);
        if (angVelMats != null)
        {
            foreach (var mat in angVelMats)
            {
                if (mat != null) Destroy(mat);
            }
        }
    }
}