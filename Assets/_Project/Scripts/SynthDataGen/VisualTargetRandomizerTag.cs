using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.Perception.Randomization.Randomizers.Tags;
using UnityEngine.Perception.Randomization.Scenarios;
using UnityEngine.Perception.Randomization.Utilities;
using UnityEngine.UIElements;





#if UNITY_EDITOR
using UnityEditor;
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
            if (ConfigContainers != null && ConfigContainers.Length == targets.Length) return; // already initialized
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
            int count = 0;
            foreach (var t in targets)
            {
                if (t != null) count++;
            }
            return count;
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

            // Force Perception labeller to update immediately
            foreach (Labeling l in SpawnedObject.GetComponentsInChildren<Labeling>())
            {
                // check if the label's game object is conditionally labeled
                if (l.gameObject.TryGetComponent<ConditionalLabeling>(out var cl))
                {
                    l.enabled = cl.ShouldLabel(); // update the ConditionalLabeling state based on current camera position/angle
                }
                if (l.isActiveAndEnabled) l.RefreshLabeling(); // refresh to ensure ground truth is updated with the new material/visual
            }
            SpawnedObject.SetActive(true); // ensure the new config is active after configuring it
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
        if (GUILayout.Button("Randomize Materials & Labels Now", GUILayout.Height(30)))
        {
            Undo.RecordObjects(GatherUndoObjects(script), "Randomize Visual Targets");
            GUIRandomizeMaterials(script);
        }
        GUI.backgroundColor = Color.white;
    }

    private void GUIRandomizeMaterials(VisualTargetRandomizerTag script)
    {
        script.InitializeConfigContainers();
        int[] shuffledIndices = DataSynthRandom.GetShuffledIndices(script.GetConfigCount(), new Unity.Mathematics.Random((uint)Random.Range(1, int.MaxValue))); // shuffle configs to ensure different randomization each time button is pressed
        for (int targetIndex = 0; targetIndex < script.GetTargetCount(); targetIndex++)
        {
            int configIndex = shuffledIndices[targetIndex % script.GetConfigCount()]; // wrap around if there are more targets than configs
            Transform target = script.targets[targetIndex];
            if (target == null) continue;

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
        }

        foreach (var t in script.targets)
        {
            if (t != null)
            {
                EditorUtility.SetDirty(t);
                if (t.TryGetComponent<Labeling>(out var l)) EditorUtility.SetDirty(l);
            }
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(script.gameObject.scene);
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