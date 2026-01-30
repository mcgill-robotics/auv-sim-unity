using UnityEngine;
using Utils;

/// <summary>
/// Singleton that captures and provides the simulation's origin reference frame.
/// This typically corresponds to the AUV's starting position and rotation (World Frame -> Odom Frame).
/// Used by sensors (DVL) and visualizers (VisionObjects) to ensure consistent calculations.
/// </summary>
[DefaultExecutionOrder(-100)] // Initialize before other scripts
public class SimulationOrigin : MonoBehaviour
{
    private static SimulationOrigin _instance;
    public static SimulationOrigin Instance 
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SimulationOrigin>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("SimulationOrigin_AutoCreated");
                    _instance = go.AddComponent<SimulationOrigin>();
                    Debug.Log("[SimulationOrigin] Auto-created singleton instance.");
                    // Force initialization immediately upon creation
                    _instance.InitializeOrigin();
                }
            }
            return _instance;
        }
        private set { _instance = value; }
    }

    [Tooltip("If true, origin is captured from AUVTransform at Start. If false, uses (0,0,0) identity.")]
    public bool captureAuvStartPose = true;

    [Tooltip("Show the FLU (Forward-Left-Up) coordinate axes at the origin")]
    public bool showOriginAxes = true;

    private GameObject originAxis;
    private GameObject originLabelObj;

    // The reference frame for the "Odom" frame (or World frame relative to start)
    public Vector3 InitialPosition { get; private set; }
    public Quaternion InitialRotation { get; private set; }
    
    public bool IsInitialized { get; private set; }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        InitializeOrigin();
    }

    private void Start()
    {
        InitializeOrigin();
        if (showOriginAxes)
        {
            CreateOriginVisualization();
        }
    }

    private void CreateOriginVisualization()
    {
        if (originAxis != null) Destroy(originAxis);
        if (originLabelObj != null) Destroy(originLabelObj);

        // Create standard RGB axis (Red=Right(X), Green=Up(Y), Blue=Fwd(Z))
        originAxis = VisualizationUtils.CreateAxis("Origin_FLU", transform, 1.0f);
        
        // Position at Origin
        originAxis.transform.position = InitialPosition;
        
        // Rotate/Flip for FLU
        Quaternion rot = Quaternion.LookRotation(Vector3.up, Vector3.left);
        originAxis.transform.rotation = InitialRotation * rot;
        originAxis.transform.localScale = new Vector3(-1, 1, 1);
        
        // Add Label (Parented to transform to avoid negative scale inheritance)
        originLabelObj = VisualizationUtils.CreateLabel("Origin", transform, Color.white, 24);
        originLabelObj.transform.position = InitialPosition; // World position
    }

    private void Update()
    {
        if (showOriginAxes && originLabelObj != null)
        {
            // Try to find a camera to face
            Camera cam = Camera.main;
            if (cam == null) cam = FindFirstObjectByType<Camera>(); // Fallback
            
            if (cam != null)
            {
                // Billboard: Face the camera
                // standard billboard: transform.LookAt(transform.position + cam.transform.rotation * Vector3.back, cam.transform.rotation * Vector3.up);
                // Simple LookRotation:
                originLabelObj.transform.rotation = Quaternion.LookRotation(originLabelObj.transform.position - cam.transform.position);
            }
        }
    }

    public void InitializeOrigin()
    {
        if (IsInitialized) return;

        if (captureAuvStartPose && SimulationSettings.Instance != null && SimulationSettings.Instance.AUVTransform != null)
        {
            InitialPosition = SimulationSettings.Instance.AUVTransform.position;
            
            // Force Origin to Surface (Y=0) so Odom Depth matches Absolute Pressure Depth
            InitialPosition = new Vector3(InitialPosition.x, 0.0f, InitialPosition.z);
            
            InitialRotation = SimulationSettings.Instance.AUVTransform.rotation;
            Debug.Log($"[SimulationOrigin] Captured Origin from AUV (Projected to Surface): Pos={InitialPosition}, Rot={InitialRotation.eulerAngles}");
        }
        else
        {
            InitialPosition = Vector3.zero;
            InitialRotation = Quaternion.identity;
            Debug.Log("[SimulationOrigin] Defaulted Origin to (0,0,0) Identity");
        }

        IsInitialized = true;
    }

    /// <summary>
    /// Transforms a Unity World position to a Start-Relative (Odom) position.
    /// Rotates the displacement into the Initial Frame.
    /// </summary>
    public Vector3 TransformPointToLocal(Vector3 worldPos)
    {
        // P_local = R_init_inverse * (P_world - P_init)
        return Quaternion.Inverse(InitialRotation) * (worldPos - InitialPosition);
    }

    /// <summary>
    /// Transforms a Start-Relative (Odom) position to Unity World position.
    /// </summary>
    public Vector3 TransformPointToWorld(Vector3 localPos)
    {
        // P_world = P_init + (R_init * P_local)
        return InitialPosition + (InitialRotation * localPos);
    }
}
