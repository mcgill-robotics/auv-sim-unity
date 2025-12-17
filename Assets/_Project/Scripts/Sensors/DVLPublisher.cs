using UnityEngine;
using RosMessageTypes.Auv;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using Utils;

public class DVLPublisher : ROSPublisher
{
    public override string Topic => ROSSettings.Instance.DVLTopic;

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
    [Tooltip("If enabled, update rate adapts based on altitude (4-26Hz). Disable for fixed rate publishing")]
    public bool simulateAdaptiveRate = true;
    
    [Tooltip("Maximum grazing angle (degrees). Beams hitting surfaces at shallower angles are rejected.")]
    [Range(30f, 80f)]
    public float maxGrazingAngle = 60f;
    
    [Space(10)]
    [Header("Visualization")]
    [Tooltip("Enable LineRenderer visualization of beams and velocity")]
    public bool enableVisualization = true;
    
    [Tooltip("Scale factor for velocity arrow visualization")]
    [Range(0.1f, 5f)]
    public float velocityArrowScale = 1f;
    
    [Tooltip("Line width for beam visualization")]
    [Range(0.005f, 0.05f)]
    public float beamLineWidth = 0.01f;

    // Public properties for UI/other scripts
    public Vector3 LastVelocity { get; private set; }
    public float LastAltitude { get; private set; }
    public bool IsValid { get; private set; }
    public int ValidBeamCount { get; private set; }
    public Vector3[] BeamHitPoints { get; private set; } = new Vector3[4];
    public bool[] BeamValid { get; private set; } = new bool[4];

    // Internals
    private VelocityReportMsg msg;
    private float nextPublishTime = 0;
    private GaussMarkovVector velocityBias;
    
    // Janus X-configuration beam directions (calculated in Start)
    private Vector3[] beamDirectionsLocal;
    
    // Visualization (3D mesh arrows)
    private GameObject[] beamArrows;
    private Renderer[][] beamArrowRenderers;  // Cached renderers to avoid per-frame allocation
    private GameObject velocityArrow;
    private GameObject visualizationRoot;
    private Material beamValidMat;
    private Material beamInvalidMat;
    private Material velocityMat;

    protected override void Start()
    {
        base.Start();
        
        // DVL uses its own adaptive timing based on altitude, not base class rate limiting
        useBaseRateLimiting = false;
        
        msg = new VelocityReportMsg();
        msg.covariance = new double[9];
        
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
        velocityMat = VisualizationUtils.CreateMaterial(new Color(1f, 0.8f, 0.2f)); // Yellow/Gold
        
        // Create beam arrows and cache their renderers
        beamArrows = new GameObject[4];
        beamArrowRenderers = new Renderer[4][];
        for (int i = 0; i < 4; i++)
        {
            beamArrows[i] = VisualizationUtils.CreateArrow($"Beam_{i}", beamValidMat, 0.02f);
            beamArrows[i].transform.SetParent(visualizationRoot.transform);
            beamArrowRenderers[i] = beamArrows[i].GetComponentsInChildren<Renderer>();
        }
        
        // Create velocity arrow
        velocityArrow = VisualizationUtils.CreateArrow("VelocityArrow", velocityMat, 0.04f);
        velocityArrow.transform.SetParent(visualizationRoot.transform);
        velocityArrow.SetActive(false);
    }


    protected override void FixedUpdate()
    {
        if (auvRb == null) return;

        // Update bias every physics step (always needed for accurate simulation)
        velocityBias.Step();

        // Run simulation and update sensor data (always, for visualization)
        if (Time.time >= nextPublishTime)
        {
            SimulateSensor();
            
            // Only publish to ROS if enabled
            if (SimulationSettings.Instance.PublishDVL && SimulationSettings.Instance.PublishROS)
            {
                PublishMessage();
            }
            
            // Calculate next update time
            float rate = 26f;
            if (simulateAdaptiveRate && IsValid)
            {
                rate = Mathf.Lerp(26f, 4f, LastAltitude / 50.0f);
            }
            nextPublishTime = Time.time + (1.0f / rate);
        }
        
        // Update visualization (always if enabled)
        if (enableVisualization && visualizationRoot != null)
        {
            UpdateVisualization();
        }
    }

    protected override void RegisterPublisher()
    {
        ros.RegisterPublisher<VelocityReportMsg>(Topic);
    }

    public override void PublishMessage()
    {
        // Timestamp and publish the pre-calculated message
        var rosTime = ROSClock.GetROSTimestamp();
        msg.time = rosTime.sec + rosTime.nanosec * 1e-9;
        ros.Publish(Topic, msg);
    }
    
    /// <summary>
    /// Simulate the DVL sensor: perform raycasting, calculate velocity, update message.
    /// This runs independently of ROS publishing for visualization support.
    /// </summary>
    private void SimulateSensor()
    {
        // 1. Perform 4-Beam Raycasting
        int validBeams = 0;
        float avgAltitude = 0f;
        
        for (int i = 0; i < 4; i++)
        {
            // Transform beam direction from local sensor frame to world
            Vector3 worldBeamDir = transform.TransformDirection(beamDirectionsLocal[i]);
            
            RaycastHit hit;
            if (Physics.Raycast(transform.position, worldBeamDir, out hit, maxAltitude, acousticLayerMask))
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
                    BeamHitPoints[i] = transform.position + worldBeamDir * maxAltitude;
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
                BeamHitPoints[i] = transform.position + worldBeamDir * maxAltitude;
            }
        }
        
        ValidBeamCount = validBeams;
        IsValid = validBeams >= minBeamsForLock;
        
        // 2. Get Ground Truth Velocity at sensor point
        // CRITICAL: Use transform (sensor frame), NOT auvRb.transform (robot frame)
        Vector3 pointVelWorld = auvRb.GetPointVelocity(transform.position);
        Vector3 localVel = transform.InverseTransformDirection(pointVelWorld);
        
        // 3. Populate Message
        if (IsValid)
        {
            LastAltitude = avgAltitude / validBeams;
            
            // Apply noise model: White noise + Gauss-Markov bias + Outliers
            Vector3 noisyVel = localVel;
            
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
            
            LastVelocity = noisyVel;
            
            // Convert to ROS frame (Unity FLU to ROS convention)
            // Unity: X=Right, Y=Up, Z=Forward
            // DVL sensor frame: X=Forward, Y=Left, Z=Up (FLU)
            msg.vx = noisyVel.z;   // Unity Z (Forward) -> ROS X
            msg.vy = -noisyVel.x;  // Unity -X (Left) -> ROS Y  
            msg.vz = noisyVel.y;   // Unity Y (Up) -> ROS Z
            
            msg.altitude = LastAltitude;
            msg.valid = true;
            msg.status = true;
            
            // Update covariance for valid lock
            msg.covariance[0] = sigmaVelocityHorizontal * sigmaVelocityHorizontal;
            msg.covariance[4] = sigmaVelocityHorizontal * sigmaVelocityHorizontal;
            msg.covariance[8] = sigmaVelocityVertical * sigmaVelocityVertical;
        }
        else
        {
            // Loss of Lock
            LastVelocity = Vector3.zero;
            LastAltitude = -1f;
            
            msg.vx = 0; msg.vy = 0; msg.vz = 0;
            msg.altitude = -1.0;
            msg.valid = false;
            msg.status = false;
            
            // Blow up covariance to signal untrusted data
            msg.covariance[0] = 10000;
            msg.covariance[4] = 10000;
            msg.covariance[8] = 10000;
        }
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
                    foreach (var r in beamArrowRenderers[i]) r.material = mat;
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
    }
}