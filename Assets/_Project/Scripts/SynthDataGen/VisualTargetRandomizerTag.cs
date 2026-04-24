using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.Perception.Randomization.Randomizers.Tags;
using UnityEngine.Perception.Randomization.Utilities;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace UnityEngine.Perception.Randomization.Randomizers.Tags
{
    /// <summary>
    /// Used to randomize materials and labels on 1 or 2 quads (e.g., Gate targets or Task Boards) every frame.
    /// Pairs each material with a specific label to ensure ground truth matches the visual.
    /// </summary>
    [AddComponentMenu("RoboSub/RandomizerTags/Visual Target Randomizer Tag")]
    public class VisualTargetRandomizerTag : RandomizerTag
    {
        private readonly Dictionary<int, CachedSpawnedObjectData> _spawnedObjectDataCache = new Dictionary<int, CachedSpawnedObjectData>();

        [Header("Randomization Setup")]
        [Tooltip("The target objects to randomize, their children will be made to match the reference target's children (configs) with different materials and labels. Can be 1 or 2 targets, if 2 ensures different configs for each.")]
        public Transform[] targets;
        [Tooltip("The configuration objects to use for randomization. A random config from this list will be applied to each target every iteration.")]
        public GameObject[] configs;

        // for each target, parent of all config variants used in cache and in editor, this allows editor to clean up anything inside this container when a new config needs to be spawned
        public GameObject[] ConfigContainers { private set; get; }

        void Awake()
        {
            InitializeConfigContainers();
        }
        public void InitializeConfigContainers()
        {
            if (targets == null) return;
            if (ConfigContainers != null && ConfigContainers.Length == targets.Length)
            {
                bool needsRebuild = false;
                for (int i = 0; i < targets.Length; i++)
                {
                    if (targets[i] != null && ConfigContainers[i] == null)
                    {
                        needsRebuild = true;
                        break;
                    }
                }

                if (!needsRebuild) return; // already initialized
            }

            // initialize config containers for each target
            ConfigContainers = new GameObject[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null)
                {
                    // check if already exists
                    Transform existingContainer = targets[i].Find(targets[i].name + "_Configs");
                    if (existingContainer != null)
                    {
                        ConfigContainers[i] = existingContainer.gameObject;
                    }
                    else
                    {
                        ConfigContainers[i] = new GameObject(targets[i].name + "_Configs");
                        ConfigContainers[i].transform.SetParent(targets[i], false); // parent to target so it moves with it and is organized in the hierarchy
                    }
                }
            }
        }
        public int GetTargetCount()
        {
            if (targets == null) return 0;
            int count = 0;
            foreach (var t in targets)
            {
                if (t != null) count++;
            }
            return count;
        }

        public IEnumerable<(int index, Transform target)> GetValidTargets()
        {
            if (targets == null) yield break;

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != null)
                {
                    yield return (i, targets[i]);
                }
            }
        }
        /// <summary>
        /// Returns the number of valid configs (child objects) under this tag. Each child should be a visual variant with a specific Material and Labelling component
        /// </summary>
        /// <returns></returns>
        public int GetConfigCount()
        {
            // assume all targets have the same number of configs (child objects), so we just take first one.
            return configs != null ? configs.Length : 0;
        }

        public void ConfigureSpawnedObject(GameObject SpawnedObject)
        {
            // reset local transform so that spawned config is positioned correctly relative to the target
            SpawnedObject.transform.localPosition = Vector3.zero;
            SpawnedObject.transform.localRotation = Quaternion.identity;

            var cachedData = GetOrCreateCachedSpawnedObjectData(SpawnedObject);

            // Force Perception labeller to update immediately
            for (int i = 0; i < cachedData.Labelings.Length; i++)
            {
                Labeling l = cachedData.Labelings[i];

                // check if the label's game object is conditionally labeled
                ConditionalLabeling cl = cachedData.ConditionalLabelings[i];
                if (cl != null)
                {
                    l.enabled = cl.ShouldLabel(); // update the ConditionalLabeling state based on current camera position/angle
                }
                if (l.isActiveAndEnabled) l.RefreshLabeling(); // refresh to ensure ground truth is updated with the new material/visual
            }
        }

        public void ClearConfigContainerAt(int targetIndex)
        {
            if (ConfigContainers == null || targetIndex < 0 || targetIndex >= ConfigContainers.Length)
                return;

            GameObject configContainer = ConfigContainers[targetIndex];
            if (configContainer == null)
                return;

            Transform containerTransform = configContainer.transform;
            for (int i = containerTransform.childCount - 1; i >= 0; i--)
            {
                Transform child = containerTransform.GetChild(i);
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                    continue;
                }
#endif
                child.localPosition = new Vector3(10000f, 0f, 0f);
                child.gameObject.SetActive(false);
            }
        }

        private CachedSpawnedObjectData GetOrCreateCachedSpawnedObjectData(GameObject spawnedObject)
        {
            int instanceId = spawnedObject.GetInstanceID();
            if (_spawnedObjectDataCache.TryGetValue(instanceId, out var cachedData))
                return cachedData;

            Labeling[] labelings = spawnedObject.GetComponentsInChildren<Labeling>(true);
            var conditionalLabelings = new ConditionalLabeling[labelings.Length];
            for (int i = 0; i < labelings.Length; i++)
            {
                labelings[i].gameObject.TryGetComponent(out conditionalLabelings[i]);
            }

            cachedData = new CachedSpawnedObjectData(labelings, conditionalLabelings);
            _spawnedObjectDataCache[instanceId] = cachedData;
            return cachedData;
        }

        private readonly struct CachedSpawnedObjectData
        {
            public readonly Labeling[] Labelings;
            public readonly ConditionalLabeling[] ConditionalLabelings;

            public CachedSpawnedObjectData(Labeling[] labelings, ConditionalLabeling[] conditionalLabelings)
            {
                Labelings = labelings;
                ConditionalLabelings = conditionalLabelings;
            }
        }
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(VisualTargetRandomizerTag))]
public class VisualTargetRandomizerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        VisualTargetRandomizerTag script = (VisualTargetRandomizerTag)target;

        EditorGUILayout.Space();
        DrawRandomize(script);
    }


    private void DrawRandomize(VisualTargetRandomizerTag script)
    {
        GUI.backgroundColor = Color.cyan;
        EditorGUILayout.HelpBox("Randomize Materials and Labels on demand in the editor. During dataset generation, the VisualTargetRandomizer component will randomize every frame to ensure consistent variety in the data.", MessageType.Info);
        using (new EditorGUI.DisabledScope(script.GetTargetCount() == 0 || script.GetConfigCount() == 0))
        {
            if (GUILayout.Button("Randomize Materials & Labels Now", GUILayout.Height(30)))
            {
                Undo.RecordObjects(GatherUndoObjects(script), "Randomize Visual Targets");
                GUIRandomizeMaterials(script);
            }
        }

        if (script.GetTargetCount() == 0 || script.GetConfigCount() == 0)
        {
            EditorGUILayout.HelpBox("Add at least one target and one config to randomize.", MessageType.Warning);
        }
        GUI.backgroundColor = Color.white;
    }

    private void GUIRandomizeMaterials(VisualTargetRandomizerTag script)
    {
        script.InitializeConfigContainers();
        int targetCount = script.GetTargetCount();
        int configCount = script.GetConfigCount();
        if (targetCount == 0 || configCount == 0) return;

        int seed = Random.Range(1, int.MaxValue);
        int[] shuffledIndices = DataSynthRandom.GetShuffledIndices(configCount, new Unity.Mathematics.Random((uint)seed)); // shuffle configs to ensure different randomization each time button is pressed
        int validTargetOrdinal = 0;
        foreach (var (targetIndex, target) in script.GetValidTargets())
        {
            int configIndex = shuffledIndices[validTargetOrdinal % configCount]; // wrap around if there are more targets than configs

            Transform ConfigContainerT = script.ConfigContainers[targetIndex].transform;
            while (ConfigContainerT.childCount > 0)
            {
                Undo.DestroyObjectImmediate(ConfigContainerT.GetChild(0).gameObject); // destroy old config, if it exists, to clean up before spawning new one. Use Undo to allow undoing in editor.
            }
            GameObject prefabToSpawn = script.configs[configIndex];

            GameObject spawnedObject = (GameObject)PrefabUtility.InstantiatePrefab(prefabToSpawn, ConfigContainerT); // spawn the new config as a child of the target so it automatically gets cleaned up and organized in the hierarchy
            Undo.RegisterCreatedObjectUndo(spawnedObject, "Spawn Config");

            // initialize newly selected config for this target
            script.ConfigureSpawnedObject(spawnedObject);
            validTargetOrdinal++;
        }

        foreach (var t in script.targets)
        {
            if (t != null)
            {
                EditorUtility.SetDirty(t);
                if (t.TryGetComponent<Labeling>(out var l)) EditorUtility.SetDirty(l);
            }
        }

        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(script.gameObject.scene);
        }
    }
    /// <summary>
    /// Gathers all objects that need to be undone when randomizing materials and labels.
    /// </summary>
    /// <returns>Array of objects to undo</returns>
    private static Object[] GatherUndoObjects(VisualTargetRandomizerTag script)
    {
        var objectsToUndo = new HashSet<Object>();
        if (script.targets == null) return new List<Object>(objectsToUndo).ToArray();

        foreach (var t in script.targets)
        {
            if (t == null) continue;
            objectsToUndo.Add(t);

            // get all descendants of the target to ensure we can undo material and label changes on all configs/variants
            foreach (var childTransform in t.GetComponentsInChildren<Transform>(true))
            {
                objectsToUndo.Add(childTransform.gameObject);
            }

            foreach (var labeling in t.GetComponentsInChildren<Labeling>(true))
            {
                objectsToUndo.Add(labeling);
            }
        }

        return new List<Object>(objectsToUndo).ToArray();
    }


}
#endif
