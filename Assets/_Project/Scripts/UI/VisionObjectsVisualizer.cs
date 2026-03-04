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
    public float minConfidence = 0.0f;

    [Tooltip("Transparency of the visualized objects")]
    [Range(0f, 1f)]
    public float objectTransparency = 0.5f;

    [Header("Outline Configuration")]
    [Tooltip("Whether to apply the torpedo outline shader to make them distinct")]
    public bool applyOutline = true;

    [Tooltip("Width of the outline")]
    public float outlineWidth = 0.05f;

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
    public class ClassVisualizer
    {
        public string className;
        public Color color = Color.white;
        [Tooltip("Optional prefab to instantiate instead of a sphere")]
        public GameObject prefab;
    }

    [Tooltip("Visualizer assignments for each object class")]
    public List<ClassVisualizer> classVisualizers = new List<ClassVisualizer>()
    {
        new ClassVisualizer { className = "gate", color = Color.green },
        new ClassVisualizer { className = "lane_marker", color = Color.yellow },
        new ClassVisualizer { className = "red_pipe", color = Color.red },
        new ClassVisualizer { className = "white_pipe", color = Color.white },
        new ClassVisualizer { className = "octagon", color = Color.magenta },
        new ClassVisualizer { className = "table", color = new Color(0.6f, 0.4f, 0.2f) },
        new ClassVisualizer { className = "bin", color = Color.blue },
        new ClassVisualizer { className = "board", color = Color.cyan },
        new ClassVisualizer { className = "shark", color = new Color(0.5f, 0.5f, 0.5f) },
        new ClassVisualizer { className = "sawfish", color = new Color(0.8f, 0.6f, 0.2f) }
    };

    private class PooledVisObject
    {
        public GameObject Root;
        public TextMesh TextMesh;
        public string PoolKey;
        public int LastConfidenceInt = -1;
    }

    // Object Pooling
    private Dictionary<string, Queue<PooledVisObject>> objectPools = new Dictionary<string, Queue<PooledVisObject>>();
    private List<PooledVisObject> activeObjects = new List<PooledVisObject>();

    private Dictionary<string, ClassVisualizer> classLookup = new Dictionary<string, ClassVisualizer>();
    private ROSConnection ros;
    private Transform visualizerRoot;

    private Vector3 worldOrigin; // AUV's position when VIO started
    private Quaternion worldRotation; // AUV's rotation when simulation started

    // VIO pose marker
    private GameObject vioPoseMarker;

    private void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<VisionObjectArrayMsg>(ROSSettings.Instance.VisionObjectMapTopic, OnVisionObjectsReceived);
        ros.Subscribe<PoseStampedMsg>(ROSSettings.Instance.VIOPoseTopic, OnVIOPoseReceived);

        visualizerRoot = new GameObject("VisionObjects_Visualizer").transform;
        visualizerRoot.SetParent(transform);

        foreach (var cv in classVisualizers)
        {
            if (!string.IsNullOrEmpty(cv.className))
                classLookup[cv.className.ToLower()] = cv;
        }

        // Initialize World Origin from centralized singleton
        // This ensures VIO/Vision objects share the same reference frame as DVL DR
        if (SimulationOrigin.Instance != null)
        {
            // Ensure origin is initialized (it runs before this script usually, but just within frame)
            SimulationOrigin.Instance.InitializeOrigin();
            
            worldRotation = SimulationOrigin.Instance.InitialRotation;
            worldOrigin = SimulationOrigin.Instance.InitialPosition;
            
            Debug.Log($"[VisionObjectsVisualizer] Initialized Origin from SimulationOrigin: {worldOrigin}");
        }
        else
        {
            worldOrigin = Vector3.zero;
            worldRotation = Quaternion.identity;
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
        // Deactivate all objects from the previous frame
        DeactivateAllObjects();

        // Create or reuse visualizer for each object in current message
        foreach (var obj in msg.array)
        {
            if (obj.confidence < minConfidence)
                continue;

            // Convert position from ROS (X-Fwd, Y-Left, Z-Up) to Unity (X-Right, Y-Up, Z-Fwd)
            Vector3 rosToUnity = new Vector3(
                -(float)obj.pose.position.y,
                (float)obj.pose.position.z,
                (float)obj.pose.position.x
            );

            // Apply starting rotation and position offset
            Vector3 unityPos = worldOrigin + (worldRotation * rosToUnity);

            // Get rotation if available
            Quaternion unityRot = Quaternion.identity;
            if (obj.has_orientation)
            {
                // Convert orientation from ROS FLU to Unity RUDF
                Quaternion rosRot = new Quaternion(
                    -(float)obj.pose.orientation.y,
                    (float)obj.pose.orientation.z,
                    (float)obj.pose.orientation.x,
                    -(float)obj.pose.orientation.w
                );
                unityRot = worldRotation * rosRot;
            }

            CreateVisualizer(obj.label, unityPos, unityRot, (float)obj.confidence, obj.has_orientation);
        }
    }

    private void CreateVisualizer(string label, Vector3 position, Quaternion rotation, float confidence, bool hasOrientation)
    {
        string poolKey = string.IsNullOrEmpty(label) ? "unknown" : label.ToLower();
        
        if (!objectPools.ContainsKey(poolKey))
        {
            objectPools[poolKey] = new Queue<PooledVisObject>();
        }

        Queue<PooledVisObject> pool = objectPools[poolKey];
        PooledVisObject visObj = null;

        if (pool.Count > 0)
        {
            visObj = pool.Dequeue();
        }
        else
        {
            visObj = CreateNewVisualizerObject(label, poolKey);
        }

        // Update the object's transform and label
        visObj.Root.transform.position = position;
        if (hasOrientation)
        {
            visObj.Root.transform.rotation = rotation;
        }
        else
        {
            visObj.Root.transform.rotation = Quaternion.identity;
        }

        // Update TextMesh visibility and text
        if (visObj.TextMesh != null)
        {
            int confInt = Mathf.RoundToInt(confidence * 100f);
            if (confInt != visObj.LastConfidenceInt)
            {
                visObj.TextMesh.text = $"{label}\n{confidence:F2}";
                visObj.LastConfidenceInt = confInt;
            }
            
            if (visObj.TextMesh.gameObject.activeSelf != showLabels)
            {
                visObj.TextMesh.gameObject.SetActive(showLabels);
            }
        }

        if (!visObj.Root.activeSelf)
        {
            visObj.Root.SetActive(true);
        }

        activeObjects.Add(visObj);
    }

    private PooledVisObject CreateNewVisualizerObject(string label, string poolKey)
    {
        GameObject visObj = null;
        Color objColor = defaultColor;
        bool usingPrefab = false;

        if (classLookup.TryGetValue(poolKey, out ClassVisualizer cv))
        {
            objColor = cv.color;
            if (cv.prefab != null)
            {
                visObj = Instantiate(cv.prefab, visualizerRoot);
                usingPrefab = true;

                // Disable Labeling and ConditionalLabeling components to prevent recursive publishing loop
                var autoLabels = visObj.GetComponentsInChildren<UnityEngine.Perception.GroundTruth.LabelManagement.Labeling>(true);
                foreach(var al in autoLabels) al.enabled = false;
                
                // If there's a conditional labeling script, we need to disable it too or it will re-enable the label
                var conditionalLabels = visObj.GetComponentsInChildren<MonoBehaviour>(true);
                foreach(var cl in conditionalLabels)
                {
                    if (cl.GetType().Name.Contains("ConditionalLabeling"))
                    {
                        cl.enabled = false;
                    }
                }
            }
        }

        if (!usingPrefab)
        {
            visObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            // Remove collider
            var collider = visObj.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            
            visObj.transform.SetParent(visualizerRoot);
            visObj.transform.localScale = Vector3.one * sphereRadius * 2f;
            
            var renderer = visObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("HDRP/Lit"));
                renderer.material.color = objColor;
            }
        }

        visObj.name = $"VisionObj_{label}";

        AddOutline(visObj, objColor);

        // Create label object
        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(visObj.transform);
        labelObj.transform.localPosition = Vector3.up * (usingPrefab ? 0.5f : sphereRadius + 0.1f);

        var textMesh = labelObj.AddComponent<TextMesh>();
        textMesh.fontSize = 24;
        textMesh.characterSize = 0.05f;
        textMesh.anchor = TextAnchor.LowerCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = objColor;

        return new PooledVisObject
        {
            Root = visObj,
            TextMesh = textMesh,
            PoolKey = poolKey,
            LastConfidenceInt = -1
        };
    }

    private void AddOutline(GameObject obj, Color classColor)
    {
        if (!applyOutline) return;

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        
        Shader outlineShader = Shader.Find("Custom/TorpedoOutline");
        if (outlineShader == null) return;
        
        Material outlineMat = new Material(outlineShader);
        outlineMat.SetColor("_OutlineColor", classColor);
        outlineMat.SetFloat("_OutlineWidth", outlineWidth);

        foreach (var ren in renderers)
        {
            if (ren.gameObject.name == "Label") continue;

            Material[] currentMats = ren.materials; // instances materials to allow append
            Material[] newMats = new Material[currentMats.Length + 1];
            
            for (int i = 0; i < currentMats.Length; i++)
            {
                newMats[i] = currentMats[i];
            }
            newMats[currentMats.Length] = outlineMat;
            ren.materials = newMats;
        }
    }

    private void DeactivateAllObjects()
    {
        foreach (var obj in activeObjects)
        {
            if (obj != null && obj.Root != null)
            {
                obj.Root.SetActive(false);
                
                if (!objectPools.ContainsKey(obj.PoolKey))
                {
                    objectPools[obj.PoolKey] = new Queue<PooledVisObject>();
                }
                objectPools[obj.PoolKey].Enqueue(obj);
            }
        }
        activeObjects.Clear();
    }

    // Use LateUpdate so camera has finished moving for the frame before we align the labels
    private void LateUpdate()
    {
        // Make labels face camera
        if (!showLabels) return;
        
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            return;
        }

        var camRot = mainCam.transform.rotation;
        foreach (var obj in activeObjects)
        {
            if (obj != null && obj.TextMesh != null && obj.Root != null && obj.Root.activeInHierarchy)
            {
                // Align the label exactly with the camera's rotation so it always faces the screen
                // TextMesh renders text facing +Z. We need its +Z to point AWAY from the camera (into the scene) to read it.
                // We use LookRotation from the text position along the camera's forward vector.
                obj.TextMesh.transform.rotation = Quaternion.LookRotation(mainCam.transform.forward, mainCam.transform.up);
            }
        }
    }

    private void CleanupMaterials(GameObject obj)
    {
        if (obj == null) return;
        var renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            var mats = r.sharedMaterials;
            foreach (var m in mats)
            {
                if (m != null && m.name.EndsWith("(Instance)"))
                {
                    Destroy(m);
                }
            }
        }
    }

    private void OnDestroy()
    {
        foreach (var pool in objectPools.Values)
        {
            while (pool.Count > 0)
            {
                var obj = pool.Dequeue();
                if (obj != null && obj.Root != null)
                {
                    CleanupMaterials(obj.Root);
                    Destroy(obj.Root);
                }
            }
        }
        
        foreach (var obj in activeObjects)
        {
            if (obj != null && obj.Root != null)
            {
                CleanupMaterials(obj.Root);
                Destroy(obj.Root);
            }
        }

        objectPools.Clear();
        activeObjects.Clear();

        if (visualizerRoot != null) Destroy(visualizerRoot.gameObject);
    }
}
