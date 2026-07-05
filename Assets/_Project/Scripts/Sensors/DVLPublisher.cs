using UnityEngine;
using RosMessageTypes.Dvl;
using RosMessageTypes.Std;
using RosMessageTypes.Nav;
using RosMessageTypes.Geometry;
using Utils;

public class DVLPublisher : ROSPublisher
{
    public override string Topic => ROSSettings.Instance.DVLTopic;

    [Header("Physical Setup")]
    [Tooltip("AUV Rigidbody - leave empty to use SimulationSettings.AUVRigidbody")]
    [SerializeField] private Rigidbody auvRbOverride;
    
    /// <summary>Returns the AUV Rigidbody from override or SimulationSettings.</summary>
    private Rigidbody AuvRb => auvRbOverride != null ? auvRbOverride : SimulationSettings.Instance?.AUVRigidbody;
    
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
    
    [Tooltip("Beam tilt angle from vertical (degrees). A50 = 22.5°")]
    [Range(0f, 45f)]
    public float beamTiltAngle = 22.5f;
    
    [Tooltip("Minimum beams required for valid bottom lock (typically 3 of 4)")]
    [Range(1, 4)]
    public int minBeamsForLock = 3;
    
    [Space(5)]
    [Tooltip("Horizontal velocity noise std dev (m/s). A50 has 22.5° beams - horizontal error is ~2.6x higher than vertical")]
    [Range(0.001f, 0.1f)]
    public float sigmaVelocityHorizontal = 0.01f;
    
    [Tooltip("Vertical velocity noise std dev (m/s)")]
    [Range(0.0001f, 0.05f)]
    public float sigmaVelocityVertical = 0.004f;
    
    [Space(10)]
    [Header("Bias Model (Gauss-Markov)")]
    [Tooltip("Correlation time (seconds) - how fast bias reverts to zero. Larger = slower drift.")]
    [Range(10f, 500f)]
    public float biasCorrelationTime = 100f;
    
    [Tooltip("Steady-state bias standard deviation (m/s)")]
    [Range(0.001f, 0.05f)]
    public float biasSigma = 0.005f;
    
    [Space(10)]
    [Header("Outlier Model")]
    [Tooltip("Probability of an outlier spike per measurement (0-5%)")]
    [Range(0f, 0.05f)]
    public float outlierProbability = 0.01f;
    
    [Tooltip("Magnitude of outlier spike (m/s)")]
    [Range(0.1f, 2f)]
    public float outlierMagnitude = 0.5f;
    
    [Space(10)]
    [Header("Simulation Options")]
    [Tooltip("If enabled, noise and bias are added to the velocity measurements.")]
    public bool enableNoise = true;

    [Tooltip("If enabled, update rate adapts based on altitude (4-26Hz). Disable for fixed rate publishing")]
    public bool simulateAdaptiveRate = true;
    
    [Tooltip("Maximum grazing angle (degrees). Beams hitting surfaces at shallower angles are rejected.")]
    [Range(30f, 80f)]
    public float maxGrazingAngle = 60f;
    
    [Space(10)]
    [Tooltip("Enable LineRenderer visualization of beams and velocity")]
    public bool enableVisualization = true;

    [Tooltip("Color for velocity arrow and location dot")]
    public Color visualizationColor = new Color(1f, 0.8f, 0.2f); // Golden
    
    [Tooltip("Scale factor for velocity arrow visualization")]
    [Range(0.1f, 5f)]
    public float velocityArrowScale = 1f;
    
    [Tooltip("Line width for beam visualization")]
    [Range(0.005f, 0.05f)]
    public float beamLineWidth = 0.01f;

    // Public properties for UI/other scripts (Unity frame)
    public Vector3 LastVelocity { get; private set; }
    public float LastAltitude { get; private set; }
    public bool IsValid { get; private set; }
    public int ValidBeamCount { get; private set; }
    public Vector3[] BeamHitPoints { get; private set; } = new Vector3[4];
    public bool[] BeamValid { get; private set; } = new bool[4];
    
    // ROS-frame accessor (FRD convention) - read directly from message
    public Vector3 RosVelocity => dvlMsg != null ? new Vector3((float)dvlMsg.velocity.x, (float)dvlMsg.velocity.y, (float)dvlMsg.velocity.z) : Vector3.zero;

    // Public for UI
    public Vector3 DVLDeadReckoningPosition => drPositionUnity;
    
    // ROS-frame accessor for UI (Odom Frame - NED)
    // Uses the calculated relative position (North, East, Down) from UpdateDeadReckoningInternal
    public Vector3 RosDeadReckoningPosition => drPositionNED;
    public Quaternion ROSDeadReckoningOrientation => drOrientation;

    // Internals
    private DVLMsg dvlMsg;
    private DVLDRMsg drMsg;
    private OdometryMsg odomMsg;
    
    private Vector3 drPositionNED; // Cached NED position for UI property
    private Quaternion drOrientation;

    private ROSTime nextPublishTime = new ROSTime(0);
    private ROSTime lastPublishTime = new ROSTime(0); // used to compute center of ping
    private GaussMarkovVector velocityBias;
    
    // Dead Reckoning State (Unity Frame)
    private Vector3 drPositionUnity; // Relative to start (accumulated)
    
    // Janus X-configuration beam directions (calculated in Start)
    private Vector3[] beamDirectionsLocal;
    
    // Visualization Materials & Objects
    
    private GameObject visualizationRoot;
    private GameObject[] beamArrows;
    private GameObject velocityArrow;
    private GameObject drPoseRoot; // Dead Reckoning Pose
    
    // Cached materials/renderers for performance
    private Material beamValidMat;
    private Material beamInvalidMat;
    private Material velocityMat;
    private Material dotMat;
    private Renderer[][] beamArrowRenderers;

    private GameObject locationDot;
    private Vector3 initialSensorPosition; // DVL Sensor's starting position

    // Averaging Accumulators
    private Vector3 accumulatedVelocity;
    private int sampleCount;
    private float accumulatedTime;
    private SensorDelayBuffer delayBuffer;
    private Vector3 localSensorOffset;
    private Vector3 currentSensorPos;

    protected override void Start()
    {
        base.Start();
        
        // DVL uses its own adaptive timing based on altitude, not base class rate limiting
        useBaseRateLimiting = false;

        dvlMsg = new DVLMsg();
        drMsg = new DVLDRMsg();
        odomMsg = new OdometryMsg();

        if (AuvRb != null)
        {
            delayBuffer = SensorDelayBuffer.GetOrCreate(AuvRb);
            localSensorOffset = AuvRb.transform.InverseTransformPoint(transform.position);
        }
        currentSensorPos = transform.position;

        initialSensorPosition = transform.position;
        drPositionUnity = Vector3.zero;
        
        // Reset accumulators
        accumulatedVelocity = Vector3.zero;
        sampleCount = 0;
        accumulatedTime = 0f;
        
        // Initialize beam directions: Janus X-configuration
        // Azimuth angles: 45°, 135°, 225°, 315° (rotated around Y)
        // Then tilted down by beamTiltAngle from vertical
        beamDirectionsLocal = new Vector3[4];
        float[] azimuthAngles = { 45f, 135f, 225f, 315f };
        
        for (int i = 0; i < 4; i++)
        {
            // Start with down vector, rotate by tilt angle around local X, then azimuth around Y
            Quaternion tilt = Quaternion.Euler(beamTiltAngle, 0f, 0f);
            Quaternion azimuth = Quaternion.Euler(0f, azimuthAngles[i], 0f);
            beamDirectionsLocal[i] = azimuth * tilt * Vector3.down;

        }

        // Initialize Gauss-Markov bias model with pre-calculated coefficients
        velocityBias = new GaussMarkovVector(biasCorrelationTime, biasSigma, Time.fixedDeltaTime);
        
        // Always setup visualization objects (so they can be toggled at runtime)
        SetupVisualization();
        if (visualizationRoot != null)
        {
            visualizationRoot.SetActive(enableVisualization);
        }
    }

    private void SetupVisualization()
    {
        visualizationRoot = new GameObject("DVL_Visualization");
        visualizationRoot.transform.SetParent(transform);
        visualizationRoot.transform.localPosition = Vector3.zero;
        visualizationRoot.transform.localRotation = Quaternion.identity;
        
        // Create materials using shared utility
        beamValidMat = VisualizationUtils.CreateMaterial(Color.green);
        beamInvalidMat = VisualizationUtils.CreateMaterial(Color.red);
        velocityMat = VisualizationUtils.CreateMaterial(visualizationColor); // Yellow/Gold
        
        // Create beam arrows and cache their renderers
        beamArrows = new GameObject[4];
        beamArrowRenderers = new Renderer[4][];
        for (int i = 0; i < 4; i++)
        {
            beamArrows[i] = VisualizationUtils.CreateArrow($"Beam_{i}", beamValidMat, beamLineWidth);
            beamArrows[i].transform.SetParent(visualizationRoot.transform);
            VisualizationUtils.SetXRayLayer(beamArrows[i]);
            beamArrowRenderers[i] = beamArrows[i].GetComponentsInChildren<Renderer>();
        }
        
        // Create velocity arrow (Yellow) -> Shows ground truth local velocity
        velocityArrow = VisualizationUtils.CreateArrow("VelocityArrow", velocityMat, 0.05f);
        velocityArrow.transform.SetParent(visualizationRoot.transform);
        VisualizationUtils.SetXRayLayer(velocityArrow);
        velocityArrow.SetActive(false);
        
        // Create DR Pose Visualizer (RGB Axis)
        // Parented to transform initially, but we will move it to World space logic
        // Actually, if we want it to stay at DR position, we should make it top-level or handle world pos manually.
        // We'll keep it under visualizationRoot but set its WORLD position in Update.
        drPoseRoot = VisualizationUtils.CreateAxis("DR_Pose_Est", visualizationRoot.transform, 0.5f);
        drPoseRoot.SetActive(false); // Hidden until valid velocity
        
        // Note: X-Ray layer is set inside CreateAxis
        
        // Create sensor location dot (always visible X-Ray marker)
        locationDot = VisualizationUtils.CreateSensorDot("DVL_Location", visualizationRoot.transform, visualizationColor, 0.05f);
        dotMat = locationDot.GetComponent<Renderer>().material;
    }


    /// <summary>
    /// Resets dead-reckoning position, accumulators, and sensor bias models.
    /// Called upon SITL simulation reset. Because DVL odometry and position are computed via time-integration
    /// of velocity (drPositionUnity += worldVel * dt), instantaneous teleportation of the Rigidbody
    /// does not clear accumulated displacement unless explicitly reset.
    /// </summary>
    public void ResetState()
    {
        drPositionUnity = Vector3.zero;
        drPositionNED = Vector3.zero;
        drOrientation = Quaternion.identity;
        accumulatedVelocity = Vector3.zero;
        sampleCount = 0;
        accumulatedTime = 0f;
        if (velocityBias != null)
        {
            velocityBias.Reset();
        }
        if (delayBuffer != null) delayBuffer.ResetState();
        LastVelocity = Vector3.zero;
        initialSensorPosition = transform.position;
        currentSensorPos = transform.position;
        UpdateDvlMessageData();
        UpdateDeadReckoningInternal();
        if (drPoseRoot != null)
        {
            drPoseRoot.transform.position = initialSensorPosition;
            drPoseRoot.transform.rotation = transform.rotation;
        }
    }

    protected override void FixedUpdate()
    {
        if (AuvRb == null) return;

        // Update bias every physics step (always needed for accurate simulation)
        velocityBias.Step();

        currentSensorPos = delayBuffer != null ? delayBuffer.GetDelayedSensorPosition(localSensorOffset, -1f) : transform.position;

        // 1. Accumulate Velocity (Ensemble Processing)
        // A real DVL integrates Doppler shifts over the sample window.
        // We simulate this by accumulating the true physics velocity every frame.
        if (IsValid)
        {
            Vector3 currentVel = delayBuffer != null ? delayBuffer.GetThrottledDelayedVelocityAtLocalOffset(localSensorOffset, -1f) : AuvRb.GetPointVelocity(transform.position);
            // Convert to sensor frame purely for accumulation if needed, but World is fine 
            // as long as we rotate it later? No, body rotation changes!
            // Crucial: A DVL measures velocity relative to the sensor *at that moment*.
            // So we must convert to LOCAL frame before accumulating.
            Vector3 localVel = transform.InverseTransformDirection(currentVel);
            accumulatedVelocity += localVel;
            sampleCount++;
            accumulatedTime += Time.fixedDeltaTime;
        }

        // 2. Check Publish Trigger (Adaptive Rate)
        ROSTime currentTime = ROSClock.GetROSTime();
        if (currentTime.GetNanoSec() >= nextPublishTime.GetNanoSec())
        {
            ProcessVelocitySample();

            // Only publish to ROS if enabled
            if (SimulationSettings.Instance.PublishDVL && SimulationSettings.Instance.PublishROS)
            {
                PublishMessage();
            }

            // Calculate next update time
            lastPublishTime = nextPublishTime;
            float rate = 2.0f; // Default "Search" rate when lock is lost
            
            if (simulateAdaptiveRate && IsValid)
            {
                // Linearly interpolate between 15Hz (shallow) and 4Hz (deep) 
                rate = Mathf.Lerp(15.0f, 4.0f, LastAltitude / maxAltitude);
            }
            // update next publish time based on current time + interval from rate
            nextPublishTime.sec = currentTime.sec + (1.0f / rate);
        }
        
        // Update visualization (always if enabled)
        if (enableVisualization && visualizationRoot != null)
        {
            UpdateVisualization();
        }
    }

    protected override void RegisterPublisher()
    {
        ros.RegisterPublisher<DVLMsg>(Topic);
        ros.RegisterPublisher<DVLDRMsg>(ROSSettings.Instance.DVLDeadReckoningTopic);
        ros.RegisterPublisher<OdometryMsg>(ROSSettings.Instance.DVLOdometryTopic);
    }

    public override void PublishMessage()
    {
        // Update timestamp for publication
        var stamp = ROSClock.GetROSTimestamp();
        dvlMsg.header.stamp = stamp;
        
        drMsg.header.stamp = stamp;
        odomMsg.header.stamp = stamp;
        
        ros.Publish(Topic, dvlMsg);
        ros.Publish(ROSSettings.Instance.DVLDeadReckoningTopic, drMsg);
        ros.Publish(ROSSettings.Instance.DVLOdometryTopic, odomMsg);
    }

    /// <summary>
    /// Population of the DvlMsg fields. 
    /// Moved here so RosVelocity accessor works for HUD even when not publishing.
    /// </summary>
    private void UpdateDvlMessageData()
    {
        ROSTime time = ROSClock.GetROSTime();
        // time of validity is the center of the ping, which we approximate as the current time minus half the interval since the last publish (since the velocity is effectively averaged over that interval)
        long timeOfValidity = time.GetMicroSec() - (lastPublishTime.GetMicroSec() / 1000 / 2);

        dvlMsg.header.frame_id = ROSSettings.Instance.DvlFrameId;
        dvlMsg.time = lastPublishTime.GetMilliSec(); // Convert back to milliseconds for the message

        dvlMsg.velocity = new Vector3Msg
        {
            x = RosVelocity.x,
            y = RosVelocity.y,
            z = RosVelocity.z
        };
        dvlMsg.fom = IsValid ? 100 : 0; // Figure of Merit based on validity
        if (IsValid)
        {
            // 3. Velocity (Sensor Frame: FRD)
            // Unity Z (Fwd) -> DVL X (Fwd), Unity X (Right) -> DVL Y (Right), Unity -Y (Down) -> DVL Z (Down)
            dvlMsg.velocity.x = LastVelocity.z; 
            dvlMsg.velocity.y = LastVelocity.x; 
            dvlMsg.velocity.z = -LastVelocity.y;

            // 4. Covariance (3x3 Diagonal)
            for (int i = 0; i < 9; i++) dvlMsg.covariance[i] = 0;
            dvlMsg.covariance[0] = Mathf.Pow(sigmaVelocityHorizontal, 2); // Var X
            dvlMsg.covariance[4] = Mathf.Pow(sigmaVelocityHorizontal, 2); // Var Y
            dvlMsg.covariance[8] = Mathf.Pow(sigmaVelocityVertical, 2);   // Var Z
        }
        else
        {
            // Loss of Lock: Zero velocity, Infinite covariance
            dvlMsg.velocity.x = 0;
            dvlMsg.velocity.y = 0;
            dvlMsg.velocity.z = 0;
            for (int i = 0; i < 9; i++) dvlMsg.covariance[i] = 0;

            dvlMsg.covariance[0] = 10000;
            dvlMsg.covariance[4] = 10000;
            dvlMsg.covariance[8] = 10000;
        }


        dvlMsg.altitude = LastAltitude;


        dvlMsg.beams = new DVLBeamMsg[]
        {
            GetBeamData(0),
            GetBeamData(1),
            GetBeamData(2),
            GetBeamData(3)
        };
        dvlMsg.velocity_valid = IsValid;
        dvlMsg.status = 0; // always valid for simplicity, DVL will not overheat in simulation
        dvlMsg.time_of_validity = timeOfValidity;
        dvlMsg.time_of_transmission = time.GetMicroSec();
        dvlMsg.form = "simulation";

        // Odometry Twist using DVL velocity (FRD) or Body FLU?
        // nav_msgs/Odometry twist is usually in child_frame_id (Body/Sensor).
        // Since child_frame_id is "dvl_link", and user defined DVL as FRD for velocity...
        // We populate it with the FRD velocity we have in dvlMsg.

        odomMsg.twist.twist.linear.x = dvlMsg.velocity.x;
        odomMsg.twist.twist.linear.y = dvlMsg.velocity.y;
        odomMsg.twist.twist.linear.z = dvlMsg.velocity.z;
        // Angular velocity - unobserved by DVL, zero
        odomMsg.twist.twist.angular.x = 0;
        odomMsg.twist.twist.angular.y = 0;
        odomMsg.twist.twist.angular.z = 0;

        // Covariance
        // Copy DVL covariance to Odom twist
        // Pose covariance grows over time - implementing a simple growth model or constant
        // For now, using identity/constant for pose.

        // Twist covariance (mapping 3x3 diagonal from DvlMsg to 6x6)
        // DvlMsg has 9 elements (row major 3x3)
        // Twist has 36 elements (row major 6x6)
        for (int k = 0; k < 36; k++) odomMsg.twist.covariance[k] = 0;

        if (IsValid)
        {
            odomMsg.twist.covariance[0] = dvlMsg.covariance[0]; // xx
            odomMsg.twist.covariance[7] = dvlMsg.covariance[4]; // yy
            odomMsg.twist.covariance[14] = dvlMsg.covariance[8]; // zz
        }
        else
        {
            odomMsg.twist.covariance[0] = 10000;
            odomMsg.twist.covariance[7] = 10000;
            odomMsg.twist.covariance[14] = 10000;
        }
    }

    private DVLBeamMsg GetBeamData(int index)
    {
        if (index < 0 || index >= 4) return null;
        DVLBeamMsg beam = new DVLBeamMsg
        {
            id = index,
            velocity = Vector3.Dot(LastVelocity, beamDirectionsLocal[index]),
            distance = Vector3.Distance(currentSensorPos, BeamHitPoints[index]),
            rssi = BeamValid[index] ? 100 : 0, // Simplified RSSI based on validity
            nsd = BeamValid[index] ? 1.0 : 10.0, // Normalized Signal Strength (lower is better), simplified
            valid = BeamValid[index]
        };
        return beam;
    }

    /// <summary>
    /// Updates the DR and Odometry messages based on the latest simulation step.
    /// </summary>
    private void UpdateDeadReckoningInternal()
    {
        // 1. Headers
        drMsg.header.frame_id = ROSSettings.Instance.OdomFrameId;
        
        odomMsg.header.frame_id = ROSSettings.Instance.OdomFrameId;
        odomMsg.child_frame_id = ROSSettings.Instance.DvlFrameId;
        
        // 2. Convert Unity Phase Position -> Odom Frame Position (Start-Relative)
        // drPositionUnity is displacement in World Frame.
        // We want the position relative to the DVL's OWN starting position.
        // So we just rotate the displacement vector into the Initial Frame.
        
        Vector3 relativePos = Quaternion.Inverse(SimulationOrigin.Instance.InitialRotation) * drPositionUnity;
        
        // Unity (Right, Up, Fwd) -> NED (North, East, Down) relative to Start
        // Unity Z (Fwd) -> NED X (North)
        // Unity X (Right) -> NED Y (East)
        // Unity -Y (Down) -> NED Z (Down)
        
        // Cache for Public Property
        drPositionNED = new Vector3(relativePos.z, relativePos.x, -relativePos.y);

        drMsg.position = new Vector3Msg
        {
            x = drPositionNED.x,
            y = drPositionNED.y,
            z = drPositionNED.z
        };
        // 3. Orientation (Relative to Start)
        Quaternion relativeRot = Quaternion.Inverse(SimulationOrigin.Instance.InitialRotation) * transform.rotation;
        
        // Convert Unity Rotation to NED Quaternion
        // Standard mapping for Identity-aligned frames:
        // q.x = -q_unity.z
        // q.y = -q_unity.x
        // q.z = q_unity.y
        // q.w = q_unity.w
        
        Quaternion q = relativeRot;
        drOrientation = new Quaternion(-q.z, -q.x, q.y, q.w);
        Vector3 drEuler = drOrientation.eulerAngles; // For debugging/UI

        drMsg.roll = drEuler.x;
        drMsg.pitch = drEuler.y;
        drMsg.yaw = drEuler.z;

        // 4. Copy to Odometry
        odomMsg.pose.pose.position.x = drPositionNED.x;
        odomMsg.pose.pose.position.y = drPositionNED.y;
        odomMsg.pose.pose.position.z = drPositionNED.z;
        odomMsg.pose.pose.orientation.x = drOrientation.x;
        odomMsg.pose.pose.orientation.y = drOrientation.y;
        odomMsg.pose.pose.orientation.z = drOrientation.z;
        odomMsg.pose.pose.orientation.w = drOrientation.w;

        // DR Pose Covariance - just set some defaults
        drMsg.pos_std = 0.1f; // Small uncertainty

        drMsg.format = "simulation";
    }

    /// <summary>
    /// Updates beam validity and altitude by raycasting.
    /// </summary>
    private void UpdateBeams()
    {
        // 1. Perform 4-Beam Raycasting
        int validBeams = 0;
        float avgAltitude = 0f;
        
        for (int i = 0; i < 4; i++)
        {
            // Transform beam direction from local sensor frame to world
            Vector3 worldBeamDir = transform.TransformDirection(beamDirectionsLocal[i]);
            
            RaycastHit hit;
            if (Physics.Raycast(currentSensorPos, worldBeamDir, out hit, maxAltitude, acousticLayerMask))
            {
                // Calculate incidence angle (angle between beam and surface normal)
                // Perfect hit = 0°, grazing hit = 90°
                float incidenceAngle = Vector3.Angle(-worldBeamDir, hit.normal);
                
                // Calculate vertical altitude from slant range
                float slantRange = hit.distance;
                float verticalAltitude = slantRange * Mathf.Cos(beamTiltAngle * Mathf.Deg2Rad);
                
                // Check validity: incidence angle, min/max altitude
                if (incidenceAngle > maxGrazingAngle)
                {
                    // Grazing angle - acoustic signal reflects away
                    BeamValid[i] = false;
                    BeamHitPoints[i] = hit.point; // Still show where it hit
                }
                else if (verticalAltitude < minAltitude)
                {
                    // Dead zone - too close
                    BeamValid[i] = false;
                    BeamHitPoints[i] = hit.point;
                }
                else if (verticalAltitude > maxAltitude)
                {
                    // Out of range
                    BeamValid[i] = false;
                    BeamHitPoints[i] = currentSensorPos + worldBeamDir * maxAltitude;
                }
                else
                {
                    // Valid hit
                    BeamValid[i] = true;
                    BeamHitPoints[i] = hit.point;
                    avgAltitude += verticalAltitude;
                    validBeams++;
                }
            }
            else
            {
                BeamValid[i] = false;
                BeamHitPoints[i] = currentSensorPos + worldBeamDir * maxAltitude;
            }
        }
        
        ValidBeamCount = validBeams;
        IsValid = validBeams >= minBeamsForLock;
        LastAltitude = IsValid ? avgAltitude / validBeams : -1f;
    }

    /// <summary>
    /// Processes the accumulated velocity samples, calculates average, and updates sensor state.
    /// </summary>
    private void ProcessVelocitySample()
    {
        if (sampleCount > 0)
        {
            // Calculate Average Velocity over the interval
            Vector3 avgLocalVel = accumulatedVelocity / sampleCount;
            
            // Process this average sample (Apply Noise, Update DR, Publish)
            SimulateSensor(avgLocalVel, accumulatedTime);
            
            // Reset accumulators
            accumulatedVelocity = Vector3.zero;
            sampleCount = 0;
            accumulatedTime = 0f;
        }
        else
        {
            // No samples (maybe newly enabled?), just process "instant" current
             Vector3 currentVel = transform.InverseTransformDirection(delayBuffer != null ? delayBuffer.GetThrottledDelayedVelocityAtLocalOffset(localSensorOffset, -1f) : AuvRb.GetPointVelocity(transform.position));
             SimulateSensor(currentVel, Time.fixedDeltaTime); 
        }
    }

    /// <summary>
    /// Simulate the DVL sensor: raycasting, noise, and message population.
    /// Now accepts averaged velocity and dt from the ensemble.
    /// </summary>
    private void SimulateSensor(Vector3 inputLocalVel, float dt)
    {
        // 1. Raycast for Altitude and Beam Validity
        UpdateBeams(); 
        
        if (!IsValid)
        {
             // Handle Lock Loss
             dvlMsg.velocity.x = 0;
             dvlMsg.velocity.y = 0;
             dvlMsg.velocity.z = 0;
             // Covariance stays large/default from initialization if not valid
        }

        // 2. Velocity Calculation (Using Input Averaged Velocity)
        if (IsValid)
        {
            // Apply noise model: White noise + Gauss-Markov bias + Outliers
            Vector3 noisyVel = inputLocalVel;
            
            if (enableNoise)
            {
                // White noise (anisotropic)
                noisyVel.x += Stochastic.GenerateGaussian() * sigmaVelocityHorizontal;
                noisyVel.y += Stochastic.GenerateGaussian() * sigmaVelocityVertical;
                noisyVel.z += Stochastic.GenerateGaussian() * sigmaVelocityHorizontal;
                
                // Gauss-Markov bias
                noisyVel += velocityBias.CurrentBias;
                
                // Salt-and-pepper outliers
                if (Stochastic.GenerateUniform() < outlierProbability)
                {
                    noisyVel += Stochastic.GenerateWhiteNoiseVector(outlierMagnitude);
                }
            }
            
            LastVelocity = noisyVel; // Update public property

            // Populate DVL Message (Body-Fixed FRD)
            dvlMsg.velocity.x = noisyVel.z;
            dvlMsg.velocity.y = noisyVel.x;
            dvlMsg.velocity.z = -noisyVel.y;
            
            // --- Dead Reckoning Integration ---
            // Transform Noisy Local Velocity (Unity Frame) to Unity World Velocity
            Vector3 worldVel = transform.rotation * noisyVel; 
            
            // Integrate using the specific dt (accumulation time) passed in
            drPositionUnity += worldVel * dt;
        }
        else
        {
            // Loss of lock
            LastVelocity = Vector3.zero;
        }
        UpdateDvlMessageData(); // Update DVLMsg with the latest velocity and validity for publishing and UI access

        // 3. Update ROS Messages (DR and Odom) from new state
        UpdateDeadReckoningInternal();
    }

    private void UpdateVisualization()
    {
        if (beamArrows == null) return;
        
        // Update beam arrows
        for (int i = 0; i < 4; i++)
        {
            if (beamArrows[i] != null)
            {
                // Calculate beam direction and length
                Vector3 beamDir = BeamHitPoints[i] - transform.position;
                float beamLength = beamDir.magnitude;
                
                // Position at sensor
                beamArrows[i].transform.position = transform.position;
                
                // Rotate to point towards hit point
                if (beamLength > 0.01f)
                {
                    beamArrows[i].transform.rotation = Quaternion.LookRotation(beamDir) * Quaternion.Euler(90, 0, 0);
                }
                
                // Scale length based on beam distance
                beamArrows[i].transform.localScale = new Vector3(1, beamLength, 1);
                
                // Update material color based on validity (using cached renderers)
                if (beamArrowRenderers != null && beamArrowRenderers[i] != null)
                {
                    Material mat = BeamValid[i] ? beamValidMat : beamInvalidMat;
                    Color col = BeamValid[i] ? Color.green : Color.red;
                    
                    foreach (var r in beamArrowRenderers[i])
                    {
                        r.material = mat;
                        VisualizationUtils.SetColorProperty(r, col);
                    }
                }
            }
        }
        
        // Update velocity arrow
        if (velocityArrow != null && IsValid)
        {
            Vector3 velWorld = transform.TransformDirection(LastVelocity);
            float magnitude = velWorld.magnitude;
            
            if (magnitude > 0.01f)
            {
                velocityArrow.SetActive(true);
                velocityArrow.transform.position = transform.position;
                
                // Point in velocity direction
                velocityArrow.transform.rotation = Quaternion.LookRotation(velWorld) * Quaternion.Euler(90, 0, 0);
                
                // Scale based on velocity magnitude
                float length = magnitude * velocityArrowScale;
                velocityArrow.transform.localScale = new Vector3(1, length, 1);
            }
            else
            {
                velocityArrow.SetActive(false);
            }
        }
        else if (velocityArrow != null)
        {
            velocityArrow.SetActive(false);
        }
        
        // Update DR Pose Visualizer
        if (drPoseRoot != null)
        {
            if (IsValid || drPositionUnity.sqrMagnitude > 0.001f)
            {
                drPoseRoot.SetActive(true);
                
                // Visualization starts at the SENSOR's initial position, not the vehicle's/origin
                drPoseRoot.transform.position = initialSensorPosition + drPositionUnity;
                
                // Align visual axes with DVL Frame (FRD)
                // We want: Red=Forward, Green=Right, Blue=Down
                // Unity Standard (LH): Red=Right, Green=Up, Blue=Forward
                
                // Step 1: Align Basis via Rotation
                // We map: Local Z -> Unity Up, Local Y -> Unity Right => Local X -> Unity Forward
                Quaternion localBasisRot = Quaternion.LookRotation(Vector3.up, Vector3.right);
                
                // Step 2: Apply Rotation relative to AUV
                drPoseRoot.transform.rotation = transform.rotation * localBasisRot;

                // Step 3: Flip Z axis to point Down (Handedness change from LH to RH)
                // Local Z was Up, now becomes Down (Blue).
                drPoseRoot.transform.localScale = new Vector3(1, 1, -1);
            }
        }
    }

    private void OnValidate()
    {
        // Recalculate bias coefficients if parameters change in Inspector
        if (velocityBias != null && Application.isPlaying)
        {
            velocityBias.RecalculateConstants(biasCorrelationTime, biasSigma, Time.fixedDeltaTime);
        }
        
        // Recalculate beam directions if tilt angle changes
        if (beamDirectionsLocal != null && Application.isPlaying)
        {
            float[] azimuthAngles = { 45f, 135f, 225f, 315f };
            for (int i = 0; i < 4; i++)
            {
                Quaternion tilt = Quaternion.Euler(beamTiltAngle, 0f, 0f);
                Quaternion azimuth = Quaternion.Euler(0f, azimuthAngles[i], 0f);
                beamDirectionsLocal[i] = azimuth * tilt * Vector3.down;
            }
        }
        
        // Update visualization toggle
        if (visualizationRoot != null)
        {
            visualizationRoot.SetActive(enableVisualization);
        }
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
        if (visualizationRoot != null)
        {
            Destroy(visualizationRoot);
        }
        // Cleanup runtime-created materials to prevent memory leaks
        if (beamValidMat != null) Destroy(beamValidMat);
        if (beamInvalidMat != null) Destroy(beamInvalidMat);
        if (velocityMat != null) Destroy(velocityMat);
        if (dotMat != null) Destroy(dotMat);
    }
}