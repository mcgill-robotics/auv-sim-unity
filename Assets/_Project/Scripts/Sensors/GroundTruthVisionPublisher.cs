using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Auv;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using System.Collections.Generic;
using Sensors;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Sensors
{
    /// <summary>
    /// Bypasses the ROS vision package to publish ground truth poses
    /// for all objects in the scene that have a Labeling component.
    /// Publishes a VisionObjectArrayMsg directly to the object map topic.
    /// </summary>
    public class GroundTruthVisionPublisher : ROSPublisher
    {
        [Header("Reference")]
        [Tooltip("Reference used for conditional labeling. Uses AUV Rigidbody if empty.")]
        [SerializeField] private Transform AUVCameraOverride;
        public Transform AUVCamera
        {
            get => AUVCameraOverride != null ? AUVCameraOverride : (SimulationSettings.Instance?.AUVRigidbody != null ? SimulationSettings.Instance.AUVRigidbody.transform : null);
            private set => AUVCameraOverride = value;
        }


        [Header("Ground Truth Vision Settings")]

        [Tooltip("Maximum distance to publish an object. Simulates camera clipping plane.")]
        public float maxDetectionDistance = 20f;

        [Tooltip("Simulated confidence value to assign to detections (0 to 1).")]
        [Range(0f, 1f)]
        public float defaultConfidence = 0.95f;

        public override string Topic => ROSSettings.Instance != null ? ROSSettings.Instance.VisionObjectMapTopic : "/vision/object_map";

        public struct CachedObjectData
        {
            public Labeling Labeling;
            public string LabelString;
            public RosMessageTypes.Geometry.Vector3Msg SizeRos;
            public RosMessageTypes.Geometry.PointMsg PosRos;
            public RosMessageTypes.Geometry.QuaternionMsg RotRos;
        }
        public CachedObjectData[] cachedObjects { get; private set; }

        protected override void Start()
        {
            base.Start(); // Handles RegisterPublisher and rate setup
            RefreshObjectCache();
        }

        protected override void RegisterPublisher()
        {
            ros.RegisterPublisher<VisionObjectArrayMsg>(Topic);
        }

        private void RefreshObjectCache()
        {
            var labelings = FindObjectsByType<Labeling>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var cacheList = new List<CachedObjectData>(labelings.Length);

            if (SimulationOrigin.Instance != null)
            {
                SimulationOrigin.Instance.InitializeOrigin();
            }
            bool useOrigin = SimulationOrigin.Instance != null && SimulationOrigin.Instance.IsInitialized;
            Quaternion inverseInitialRot = useOrigin ? Quaternion.Inverse(SimulationOrigin.Instance.InitialRotation) : Quaternion.identity;
            Vector3 initialPos = useOrigin ? SimulationOrigin.Instance.InitialPosition : Vector3.zero;

            foreach (var label in labelings)
            {
                if (label.labels.Count == 0 || label.name.StartsWith("VisionObj_")) continue;

                var data = new CachedObjectData();
                data.Labeling = label;
                data.LabelString = label.labels[0].ToLowerInvariant();

                // Cache approximate size
                var renderer = label.GetComponentInChildren<Renderer>();
                data.SizeRos = new RosMessageTypes.Geometry.Vector3Msg { x = 1, y = 1, z = 1 };
                if (renderer != null)
                {
                    var bounds = renderer.bounds.size;
                    data.SizeRos.x = bounds.z; // Fwd
                    data.SizeRos.y = bounds.x; // Right->Left
                    data.SizeRos.z = bounds.y; // Up
                }

                // Precalculate relative positions since objects are static
                Vector3 odomPos = label.transform.position;
                Quaternion odomRot = label.transform.rotation;

                if (useOrigin)
                {
                    Vector3 worldDisp = label.transform.position - initialPos;
                    odomPos = inverseInitialRot * worldDisp;
                    odomRot = inverseInitialRot * label.transform.rotation;
                }

                data.PosRos = new RosMessageTypes.Geometry.PointMsg
                {
                    x = odomPos.z,
                    y = -odomPos.x,
                    z = label.transform.position.y // Depth is absolute from surface
                };
                data.RotRos = odomRot.To<FLU>();

                cacheList.Add(data);
            }

            cachedObjects = cacheList.ToArray();
        }

        public override void PublishMessage()
        {
            if (SimulationSettings.Instance != null && !SimulationSettings.Instance.PublishGTObjectMap) return;
            if (cachedObjects == null || cachedObjects.Length == 0) return;

            Transform referenceTransform = SimulationSettings.Instance?.AUVTransform;
            if (referenceTransform == null) return;

            var msg = new VisionObjectArrayMsg();

            // Generate standard ROS HeaderMsg
            var stampMsg = ROSClock.GetROSTimestamp();
            msg.header = new RosMessageTypes.Std.HeaderMsg
            {
                stamp = stampMsg,
                frame_id = ROSSettings.Instance?.WorldFrameId ?? "pool_link"
            };

            var objectsList = new List<VisionObjectMsg>(cachedObjects.Length);

            foreach (var cachedObj in cachedObjects)
            {
                var label = cachedObj.Labeling;

                // Ensure object is active
                if (label == null || !label.enabled || !label.gameObject.activeInHierarchy)
                    continue;

                // Ensure it's within detection range of AUV
                float distance = Vector3.Distance(label.transform.position, referenceTransform.position);
                if (distance > maxDetectionDistance)
                    continue;

                var visionObj = new VisionObjectMsg
                {
                    header = msg.header,
                    label = cachedObj.LabelString,
                    id = label.gameObject.GetInstanceID(),
                    confidence = defaultConfidence,
                    size = cachedObj.SizeRos,
                    has_orientation = true,
                    pose = new RosMessageTypes.Geometry.PoseMsg
                    {
                        position = cachedObj.PosRos,
                        orientation = cachedObj.RotRos
                    },
                    frames_since_last_seen = 0
                };

                objectsList.Add(visionObj);
            }

            msg.array = objectsList.ToArray();
            ros.Publish(Topic, msg);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(GroundTruthVisionPublisher))]
public class GroundTruthVisionPublisherEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GroundTruthVisionPublisher script = (GroundTruthVisionPublisher)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Conditional Labeling Camera Reference", EditorStyles.boldLabel);
        DrawUpdateCameraButton(script);
    }

    private void DrawUpdateCameraButton(GroundTruthVisionPublisher script)
    {
        if (GUILayout.Button("Update Conditional Labeling Camera References"))
        {
            Debug.Log("Updating camera references for all ConditionalLabeling components...");
            if (script.AUVCamera == null)
            {
                Debug.LogError("AUVCamera is null on the target script!");
                return;
            }
            // Update camera reference for all conditional labeling in cache
            ConditionalLabeling[] conditionalLabelers = FindObjectsByType<ConditionalLabeling>(FindObjectsSortMode.None);
            Debug.Log($"Found {conditionalLabelers.Length} ConditionalLabeling components to update.");

            Undo.RecordObjects(conditionalLabelers, "Update Conditional Labeling Camera Reference");
            foreach (var labeler in conditionalLabelers)
            {
                Debug.Log($"Updating camera reference for {labeler.gameObject.name}");
                labeler.UpdateCameraReference(script.AUVCamera);
                EditorUtility.SetDirty(labeler);
            }
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
    }
}
#endif
