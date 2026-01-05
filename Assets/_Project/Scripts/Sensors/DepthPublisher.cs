using UnityEngine;
using RosMessageTypes.Std;
using Utils;

/// <summary>
/// Publishes depth sensor data to ROS based on sensor depth.
/// Uses standard std_msgs/Float64 message.
/// Measures depth at THIS GameObject's position (like a real sensor).
/// 
/// CONVENTION:
/// Depth is published as a positive value increasing with depth (+ Down).
/// </summary>
public class DepthPublisher : ROSPublisher
{
    public override string Topic => ROSSettings.Instance.DepthTopic;

    [Header("Depth Sensor Characteristics")]
    [Tooltip("Depth is -Y position in Unity coordinates.")]
    public bool useUnityYAsDepth = true;
    
    [Space(10)]
    [Header("Noise Model")]
    [Tooltip("White noise standard deviation (meters)")]
    [Range(0f, 0.5f)]
    public float noiseStdDev = 0.01f;
    
    [Tooltip("Bias (systematic offset) in meters")]
    [Range(-0.5f, 0.5f)]
    public float bias = 0f;
    
    [Space(10)]
    [Header("Visualization")]
    [Tooltip("Show depth line from sensor to water surface")]
    public bool enableVisualization = true;
    
    [Tooltip("Color of the depth visualization line and location dot")]
    public Color visualizationColor = new Color(0.2f, 0.6f, 1f, 0.8f); // Light blue

    // Internals
    private Float64Msg depthMsg;
    private LineRenderer depthLine;
    private GameObject visualizationRoot;
    private Material lineMaterial;
    private Texture2D lineTexture;
    private Material dotMat;
    private GameObject locationDot;
    
    // Public property for UI
    public float LastDepth { get; private set; }

    protected override void Start()
    {
        base.Start();
        InitializeMessage();
        SetupVisualization();
    }

    protected override void RegisterPublisher()
    {
        ros.RegisterPublisher<Float64Msg>(Topic);
    }

    private void InitializeMessage()
    {
        depthMsg = new Float64Msg();
    }
    
    private void SetupVisualization()
    {
        visualizationRoot = new GameObject("Depth_Visualization");
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
        
        depthLine.textureMode = LineTextureMode.Tile;
        depthLine.textureScale = new Vector2(5f, 1f); 
        
        // Create sensor location dot
        locationDot = VisualizationUtils.CreateSensorDot("Depth_Location", visualizationRoot.transform, visualizationColor, 0.05f);
        dotMat = locationDot.GetComponent<Renderer>().material;
        
        visualizationRoot.SetActive(enableVisualization);
    }
    
    private Material CreateDottedLineMaterial()
    {
        lineTexture = new Texture2D(16, 1, TextureFormat.RGBA32, false);
        lineTexture.filterMode = FilterMode.Point;
        lineTexture.wrapMode = TextureWrapMode.Repeat;
        
        for (int i = 0; i < 16; i++)
        {
            Color pixelColor = i < 8 ? Color.white : Color.clear;
            lineTexture.SetPixel(i, 0, pixelColor);
        }
        lineTexture.Apply();
        
        lineMaterial = new Material(Shader.Find("Sprites/Default"));
        lineMaterial.mainTexture = lineTexture;
        lineMaterial.mainTextureScale = new Vector2(30f, 1f); 
        
        return lineMaterial;
    }

    protected override void FixedUpdate()
    {
        CalculateDepth();
        
        if (SimulationSettings.Instance.PublishDepth && SimulationSettings.Instance.PublishROS)
        {
            base.FixedUpdate();
        }
        
        if (enableVisualization && visualizationRoot != null)
        {
            UpdateVisualization();
        }
    }
    
    private void CalculateDepth()
    {
        float depth = -transform.position.y;
        depth = Mathf.Max(0f, depth);
        
        // Add noise/bias only for the "noisy" measurement sent to ROS
        float noisyDepth = depth + bias;
        if (noiseStdDev > 0)
        {
            noisyDepth += (float)Stochastic.GenerateGaussian() * noiseStdDev;
        }
        
        LastDepth = depth; // Clean depth for simple display/viz
        depthMsg.data = noisyDepth;
    }

    public override void PublishMessage()
    {
        // data is already set in FixedUpdate -> CalculateDepth
        ros.Publish(Topic, depthMsg);
    }
    
    private void UpdateVisualization()
    {
        if (depthLine == null) return;
        
        Vector3 sensorPos = transform.position;
        Vector3 surfacePos = new Vector3(sensorPos.x, 0f, sensorPos.z);
        
        depthLine.SetPosition(0, sensorPos);
        depthLine.SetPosition(1, surfacePos);
        
        depthLine.enabled = sensorPos.y < 0;
    }
    
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
        
        if (visualizationRoot != null)
        {
            visualizationRoot.SetActive(enableVisualization);
        }
        
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
        if (lineMaterial != null) Destroy(lineMaterial);
        if (lineTexture != null) Destroy(lineTexture);
        if (dotMat != null) Destroy(dotMat);
    }
}
