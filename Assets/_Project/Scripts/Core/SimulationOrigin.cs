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
    private static bool reportedMissing;

    /// <summary>
    /// The scene's origin. If the scene has no SimulationOrigin, this logs an error once and adds
    /// one to the SimulationSettings GameObject so the sensors keep working; add it to the scene
    /// to silence the error. Returns null only if there is no SimulationSettings either.
    /// </summary>
    public static SimulationOrigin Instance
    {
        get
        {
            if (_instance != null) return _instance;

            _instance = FindFirstObjectByType<SimulationOrigin>();
            if (_instance == null && Application.isPlaying && SimulationSettings.Instance != null)
            {
                if (!reportedMissing)
                {
                    Debug.LogError("[SimulationOrigin] No SimulationOrigin in the scene. Add one next to SimulationSettings. " +
                                   "Using a temporary one on the SimulationSettings GameObject for now.");
                    reportedMissing = true;
                }
                // Awake runs inside AddComponent and registers the instance.
                SimulationSettings.Instance.gameObject.AddComponent<SimulationOrigin>();
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
            Debug.LogWarning("[SimulationOrigin] A second SimulationOrigin was found; removing the duplicate component.");
            Destroy(this);
            return;
        }
        _instance = this;
        InitializeOrigin();
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
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
                // Billboard: keep the label facing the camera.
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
