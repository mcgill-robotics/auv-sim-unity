using UnityEngine;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using Utils;

/// <summary>
/// Publishes pressure sensor data to ROS based on sensor depth.
/// Uses standard sensor_msgs/FluidPressure message.
/// Pressure = Atmospheric + (Water Density × Gravity × Depth)
/// Measures depth at THIS GameObject's position (like a real sensor).
/// </summary>
public class PressurePublisher : ROSPublisher
{
    public override string Topic => ROSSettings.Instance.PressureTopic;

    [Header("Pressure Sensor Characteristics")]
    [Tooltip("Atmospheric pressure at surface (Pa). Standard = 101325 Pa")]
    public double atmosphericPressure = 101325.0;
    
    [Tooltip("Water density (kg/m³). Fresh = 1000, Seawater ≈ 1025")]
    [Range(990f, 1050f)]
    public float waterDensity = 1000f;
    
    [Tooltip("Gravity (m/s²)")]
    public float gravity = 9.81f;
    
    [Space(10)]
    [Header("Noise Model")]
    [Tooltip("White noise standard deviation (Pa)")]
    [Range(0f, 500f)]
    public float noiseStdDev = 100f;
    
    [Tooltip("Bias (systematic offset) in Pascals")]
    [Range(-500f, 500f)]
    public float bias = 0f;
    
    [Space(10)]
    [Header("Visualization")]
    [Tooltip("Show depth line from sensor to water surface")]
    public bool enableVisualization = true;
    
    [Tooltip("Color of the depth visualization line and location dot")]
    public Color visualizationColor = new Color(0.2f, 0.6f, 1f, 0.8f); // Light blue

    // Internals
    private FluidPressureMsg pressureMsg;
    private LineRenderer depthLine;
    private GameObject visualizationRoot;
    private Material lineMaterial;  // Store reference for cleanup
    private Texture2D lineTexture;  // Store reference for cleanup
    private Material dotMat;
    private GameObject locationDot;
    
    // Public property for UI/other scripts
    public double LastPressure { get; private set; }
    public float LastDepth { get; private set; }

    protected override void Start()
    {
        base.Start();
        InitializeMessage();
        SetupVisualization();
    }

    protected override void RegisterPublisher()
    {
        ros.RegisterPublisher<FluidPressureMsg>(Topic);
    }

    private void InitializeMessage()
    {
        pressureMsg = new FluidPressureMsg();
        pressureMsg.header = new HeaderMsg { frame_id = ROSSettings.Instance.PressureFrameId };
        
        // Variance = StdDev^2
        pressureMsg.variance = noiseStdDev * noiseStdDev;
    }
    
    private void SetupVisualization()
    {
        visualizationRoot = new GameObject("Pressure_Visualization");
        visualizationRoot.transform.SetParent(transform);
        visualizationRoot.transform.localPosition = Vector3.zero;
        
        // Create depth line
        GameObject lineObj = new GameObject("DepthLine");
        lineObj.transform.SetParent(visualizationRoot.transform);
        VisualizationUtils.SetXRayLayer(lineObj);
        depthLine = lineObj.AddComponent<LineRenderer>();
        
        // Configure line appearance
        depthLine.positionCount = 2;
        depthLine.startWidth = 0.03f;
        depthLine.endWidth = 0.03f;
        
        // Create dotted line material
        depthLine.material = CreateDottedLineMaterial();
        depthLine.startColor = visualizationColor;
        depthLine.endColor = visualizationColor;
        depthLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        depthLine.receiveShadows = false;
        
        // Enable texture tiling along the line length
        // Tile mode + textureScale controls dots per world unit
        depthLine.textureMode = LineTextureMode.Tile;
        depthLine.textureScale = new Vector2(5f, 1f); // 5 dots per world unit
        
        // Create sensor location dot (always visible X-Ray marker)
        locationDot = VisualizationUtils.CreateSensorDot("Pressure_Location", visualizationRoot.transform, visualizationColor, 0.05f);
        dotMat = locationDot.GetComponent<Renderer>().material;
        
        visualizationRoot.SetActive(enableVisualization);
    }
    
    private Material CreateDottedLineMaterial()
    {
        // Create a simple dotted texture (16 pixels: 8 on, 8 off)
        lineTexture = new Texture2D(16, 1, TextureFormat.RGBA32, false);
        lineTexture.filterMode = FilterMode.Point;
        lineTexture.wrapMode = TextureWrapMode.Repeat;
        
        // Create dot pattern: filled, then empty
        for (int i = 0; i < 16; i++)
        {
            // First 8 pixels = visible (white), last 8 = transparent
            Color pixelColor = i < 8 ? Color.white : Color.clear;
            lineTexture.SetPixel(i, 0, pixelColor);
        }
        lineTexture.Apply();
        
        // Create material
        lineMaterial = new Material(Shader.Find("Sprites/Default"));
        lineMaterial.mainTexture = lineTexture;
        lineMaterial.mainTextureScale = new Vector2(30f, 1f); // 5 dots per unit length
        
        return lineMaterial;
    }

    protected override void FixedUpdate()
    {
        // Always calculate for visualization
        CalculateDepthAndPressure();
        
        // Only publish if enabled
        if (SimulationSettings.Instance.PublishPressure && SimulationSettings.Instance.PublishROS)
        {
            // Let base class handle publish rate timing
            base.FixedUpdate();
        }
        
        // Update visualization
        if (enableVisualization && visualizationRoot != null)
        {
            UpdateVisualization();
        }
    }
    
    private void CalculateDepthAndPressure()
    {
        // Calculate depth at THIS sensor's position (negative Y in Unity = positive depth)
        float depth = -transform.position.y;
        
        // Clamp to non-negative (can't have negative depth underwater)
        depth = Mathf.Max(0f, depth);
        LastDepth = depth;
        
        // Calculate pressure: P = P_atm + ρgh
        double pressure = atmosphericPressure + (waterDensity * gravity * depth);
        
        // Add noise (only for publishing, not visualization)
        double noisyPressure = pressure;
        if (noiseStdDev > 0)
        {
            noisyPressure += Stochastic.GenerateGaussian() * noiseStdDev;
        }
        
        // Add bias
        noisyPressure += bias;
        
        LastPressure = noisyPressure;
    }

    public override void PublishMessage()
    {
        // Populate message with already-calculated values
        pressureMsg.fluid_pressure = LastPressure;
        pressureMsg.variance = noiseStdDev * noiseStdDev;
        pressureMsg.header.stamp = ROSClock.GetROSTimestamp();
        
        ros.Publish(Topic, pressureMsg);
    }
    
    private void UpdateVisualization()
    {
        if (depthLine == null) return;
        
        // Draw line from sensor to water surface (Y=0)
        Vector3 sensorPos = transform.position;
        Vector3 surfacePos = new Vector3(sensorPos.x, 0f, sensorPos.z);
        
        depthLine.SetPosition(0, sensorPos);
        depthLine.SetPosition(1, surfacePos);
        
        // Only show if underwater
        depthLine.enabled = sensorPos.y < 0;
    }
    
    /// <summary>
    /// Toggle visualization on/off (called from UI)
    /// </summary>
    public void SetVisualizationActive(bool active)
    {
        enableVisualization = active;
        if (visualizationRoot != null)
        {
            visualizationRoot.SetActive(active);
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying) return;
        
        // Update variance if noise changes
        if (pressureMsg != null)
        {
            pressureMsg.variance = noiseStdDev * noiseStdDev;
        }
        
        // Update visualization state
        if (visualizationRoot != null)
        {
            visualizationRoot.SetActive(enableVisualization);
        }
        
        // Update line color
        if (depthLine != null)
        {
            depthLine.startColor = visualizationColor;
            depthLine.endColor = new Color(visualizationColor.r, visualizationColor.g, visualizationColor.b, 0.2f);
        }
    }
    
    private void OnDestroy()
    {
        if (visualizationRoot != null)
        {
            Destroy(visualizationRoot);
        }
        // Cleanup runtime-created material and texture to prevent memory leaks
        if (lineMaterial != null) Destroy(lineMaterial);
        if (lineTexture != null) Destroy(lineTexture);
        if (dotMat != null) Destroy(dotMat);
    }
}
