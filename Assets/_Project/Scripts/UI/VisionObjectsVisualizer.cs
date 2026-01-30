using System.Collections.Generic;
using RosMessageTypes.Auv;
using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

/// <summary>
/// Subscribes to /vision/objects_3d and renders spheres at detected object positions.
/// Also subscribes to /vision/vio_pose to show where the robot thinks it is.
/// Positions are received in ROS world frame (X-Forward, Y-Left, Z-Up) and converted to Unity.
/// </summary>
public class VisionObjectsVisualizer : MonoBehaviour
{
    [Header("Visualization")]
    [Tooltip("Radius of the visualization spheres")]
    [Range(0.05f, 1f)]
    public float sphereRadius = 0.15f;

    [Tooltip("Show object labels as TextMesh")]
    public bool showLabels = true;

    [Tooltip("Minimum confidence to display object")]
    [Range(0f, 1f)]
    public float minConfidence = 0.3f;

    [Header("VIO Pose Visualization")]
    [Tooltip("Color for the VIO pose marker")]
    public Color vioPoseColor = new Color(1f, 0.5f, 0f); // Orange

    [Tooltip("Size of the VIO pose marker")]
    [Range(0.1f, 1f)]
    public float vioPoseSize = 0.3f;

    [Tooltip("Continuously align VIO pose with AUV ground truth")]
    public bool autoFixDrift = false;

    [Header("Colors by Class")]
    [Tooltip("Default color for unknown classes")]
    public Color defaultColor = Color.white;

    [System.Serializable]
    public class ClassColor
    {
        public string className;
        public Color color = Color.white;
    }

    [Tooltip("Color assignments for each object class")]
    public List<ClassColor> classColors = new List<ClassColor>()
    {
        new ClassColor { className = "gate", color = Color.green },
        new ClassColor { className = "lane_marker", color = Color.yellow },
        new ClassColor { className = "red_pipe", color = Color.red },
        new ClassColor { className = "white_pipe", color = Color.white },
        new ClassColor { className = "octagon", color = Color.magenta },
        new ClassColor { className = "table", color = new Color(0.6f, 0.4f, 0.2f) },
        new ClassColor { className = "bin", color = Color.blue },
        new ClassColor { className = "board", color = Color.cyan },
        new ClassColor { className = "shark", color = new Color(0.5f, 0.5f, 0.5f) },
        new ClassColor { className = "sawfish", color = new Color(0.8f, 0.6f, 0.2f) }
    };

    private List<GameObject> activeSpheres = new List<GameObject>();
    private Dictionary<string, Color> colorLookup = new Dictionary<string, Color>();
    private ROSConnection ros;
    private Transform visualizerRoot;

    private Vector3 worldOrigin; // AUV's position when VIO started
    private Quaternion worldRotation; // AUV's rotation when simulation started
    private bool worldOriginInitialized = false;

    // VIO pose marker
    private GameObject vioPoseMarker;

    private void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<VisionObjectArrayMsg>(ROSSettings.Instance.VisionObjectMapTopic, OnVisionObjectsReceived);
        ros.Subscribe<PoseStampedMsg>(ROSSettings.Instance.VIOPoseTopic, OnVIOPoseReceived);

        visualizerRoot = new GameObject("VisionObjects_Visualizer").transform;
        visualizerRoot.SetParent(transform);

        foreach (var cc in classColors)
        {
            if (!string.IsNullOrEmpty(cc.className))
                colorLookup[cc.className.ToLower()] = cc.color;
        }

        // Initialize World Origin from centralized singleton
        // This ensures VIO/Vision objects share the same reference frame as DVL DR
        if (SimulationOrigin.Instance != null)
        {
            // Ensure origin is initialized (it runs before this script usually, but just within frame)
            SimulationOrigin.Instance.InitializeOrigin();
            
            worldRotation = SimulationOrigin.Instance.InitialRotation;
            worldOrigin = SimulationOrigin.Instance.InitialPosition;
            worldOriginInitialized = true;
            
            Debug.Log($"[VisionObjectsVisualizer] Initialized Origin from SimulationOrigin: {worldOrigin}");
        }
        else
        {
            worldOrigin = Vector3.zero;
            worldRotation = Quaternion.identity;
            worldOriginInitialized = true;
            Debug.LogWarning("[VisionObjectsVisualizer] SimulationOrigin not found - using Unity origin (0,0,0)");
        }

        // Create VIO pose marker (cube to distinguish from object spheres)
        CreateVIOPoseMarker();
    }

    private void CreateVIOPoseMarker()
    {
        vioPoseMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vioPoseMarker.name = "VIO_Pose_Marker";
        vioPoseMarker.transform.SetParent(visualizerRoot);
        vioPoseMarker.transform.localScale = Vector3.one * vioPoseSize;

        var collider = vioPoseMarker.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        var renderer = vioPoseMarker.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("HDRP/Lit"));
            renderer.material.color = vioPoseColor;
        }

        // Add label
        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(vioPoseMarker.transform);
        labelObj.transform.localPosition = Vector3.up * (vioPoseSize + 0.1f);

        var textMesh = labelObj.AddComponent<TextMesh>();
        textMesh.text = "VIO Pose";
        textMesh.fontSize = 24;
        textMesh.characterSize = 0.05f;
        textMesh.anchor = TextAnchor.LowerCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = vioPoseColor;

        vioPoseMarker.SetActive(false); // Hide until we receive data
    }

    private void OnVIOPoseReceived(PoseStampedMsg msg)
    {
        // origin initialized in Start() via SimulationOrigin


        // Convert from ROS (X-Fwd, Y-Left, Z-Up) to Unity (X-Right, Y-Up, Z-Fwd)
        Vector3 rosToUnity = new Vector3(
            -(float)msg.pose.position.y,
            (float)msg.pose.position.z,
            (float)msg.pose.position.x
        );

        // Auto-fix drift by aligning VIO to AUV ground truth
        if (autoFixDrift)
        {
            Transform auvTransform = SimulationSettings.Instance?.AUVTransform;
            if (auvTransform != null)
            {
                // Calculate required origin so that unityPos matches AUV position
                // unityPos = worldOrigin + (worldRotation * rosToUnity)
                // auvPos = worldOrigin + (worldRotation * rosToUnity)
                // worldOrigin = auvPos - (worldRotation * rosToUnity)
                worldOrigin = auvTransform.position - (worldRotation * rosToUnity);
            }
        }

        // Apply starting rotation and position offset
        Vector3 unityPos = worldOrigin + (worldRotation * rosToUnity);

        vioPoseMarker.SetActive(true);
        vioPoseMarker.transform.position = unityPos;
    }

    [ContextMenu("Fix Drift")]
    public void FixDrift()
    {
        if (vioPoseMarker == null || !vioPoseMarker.activeSelf)
        {
            Debug.LogWarning("[VisionObjectsVisualizer] Cannot fix drift: VIO pose not yet received.");
            return;
        }

        Transform auvTransform = SimulationSettings.Instance?.AUVTransform;
        if (auvTransform == null)
        {
            Debug.LogError("[VisionObjectsVisualizer] Cannot fix drift: AUV Transform not found.");
            return;
        }

        // Calculate the difference between where VIO thinks we are and where we actually are
        // We want: unityPos_new = unityPos_old + correction
        // unityPos_new should equal auvTransform.position
        // So: auvTransform.position = vioPoseMarker.transform.position + correction
        // correction = auvTransform.position - vioPoseMarker.transform.position

        Vector3 correction = auvTransform.position - vioPoseMarker.transform.position;
        
        // We apply this correction by shifting the worldOrigin
        // worldOrigin_new = worldOrigin_old + correction
        worldOrigin += correction;

        Debug.Log($"[VisionObjectsVisualizer] Drift fixed. Applied correction: {correction}. New World Origin: {worldOrigin}");
    }

    private void OnVisionObjectsReceived(VisionObjectArrayMsg msg)
    {
        // Clear all existing spheres
        ClearAllSpheres();

        // Create sphere for each object in current message
        foreach (var obj in msg.array)
        {
            if (obj.confidence < minConfidence)
                continue;

            // Convert from ROS (X-Fwd, Y-Left, Z-Up) to Unity (X-Right, Y-Up, Z-Fwd)
            Vector3 rosToUnity = new Vector3(
                -(float)obj.y,  // ROS Y (left) -> Unity -X (right)
                (float)obj.z,   // ROS Z (up) -> Unity Y
                (float)obj.x    // ROS X (forward) -> Unity Z
            );

            // Apply starting rotation and position offset
            Vector3 unityPos = worldOrigin + (worldRotation * rosToUnity);

            CreateSphere(obj.label, unityPos, (float)obj.confidence);
        }
    }

    private void CreateSphere(string label, Vector3 position, float confidence)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = $"VisionObj_{label}";
        sphere.transform.SetParent(visualizerRoot);
        sphere.transform.position = position;
        sphere.transform.localScale = Vector3.one * sphereRadius * 2f;

        // Remove collider
        var collider = sphere.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        // Set color
        Color objColor = defaultColor;
        if (!string.IsNullOrEmpty(label) && colorLookup.TryGetValue(label.ToLower(), out Color classColor))
        {
            objColor = classColor;
        }

        var renderer = sphere.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("HDRP/Lit"));
            renderer.material.color = objColor;
        }

        // Create label
        if (showLabels)
        {
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(sphere.transform);
            labelObj.transform.localPosition = Vector3.up * (sphereRadius + 0.1f);

            var textMesh = labelObj.AddComponent<TextMesh>();
            textMesh.text = $"{label}\n{confidence:F2}";
            textMesh.fontSize = 24;
            textMesh.characterSize = 0.05f;
            textMesh.anchor = TextAnchor.LowerCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = objColor;
        }

        activeSpheres.Add(sphere);
    }

    private void ClearAllSpheres()
    {
        foreach (var sphere in activeSpheres)
        {
            if (sphere != null) Destroy(sphere);
        }
        activeSpheres.Clear();
    }

    private void Update()
    {
        // Make labels face camera
        if (!showLabels || Camera.main == null) return;

        foreach (var sphere in activeSpheres)
        {
            if (sphere == null) continue;
            var label = sphere.GetComponentInChildren<TextMesh>();
            if (label != null)
            {
                label.transform.rotation = Quaternion.LookRotation(
                    label.transform.position - Camera.main.transform.position
                );
            }
        }
    }

    private void OnDestroy()
    {
        ClearAllSpheres();
        if (visualizerRoot != null) Destroy(visualizerRoot.gameObject);
    }
}
