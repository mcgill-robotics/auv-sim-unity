using UnityEngine;
using UnityEngine.Perception.Randomization.Scenarios;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.Perception.Randomization.Randomizers.Tags;
using System.Collections.Generic;


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
        [System.Serializable]
        public struct MaterialLabelConfig
        {
            public Material material;
            public string label;
        }
        [Header("Targets")]
        [Tooltip("Assign 1 quad (for Board) or 2 quads (for Gate)")]
        public MeshRenderer[] targets;

        [Header("Configurations")]
        [Tooltip("List of material/label pairs to pick from.")]
        public MaterialLabelConfig[] configs;

        public int GetTargetCount()
        {
            int count = 0;
            foreach (var t in targets)
            {
                if (t != null) count++;
            }
            return count;
        }

        public int GetConfigCount()
        {
            return configs != null ? configs.Length : 0;
        }

        /// <summary>
        /// Randomizes materials and labels. Ensures Left != Right if there are two targets.
        /// </summary>
        public void RandomizeMaterials(int targetIndex, int configIndex)
        {
            if (targets == null || configs == null) return;
            if (targetIndex < 0 || targetIndex >= targets.Length) return;
            if (configIndex < 0 || configIndex >= configs.Length) return;
            ApplyConfig(targets[targetIndex], configs[configIndex]);
        }

        private void ApplyConfig(MeshRenderer target, MaterialLabelConfig config)
        {
            if (target == null) return;

            // Apply Material
            target.sharedMaterial = config.material;

            // Apply Labeling
            if (target.TryGetComponent<Labeling>(out var labeling))
            {
                labeling.labels.Clear();
                if (!string.IsNullOrEmpty(config.label))
                {
                    labeling.labels.Add(config.label);
                }
                labeling.RefreshLabeling();
            }
        }
        /// <summary>
        /// Generates a list of (targetIndex, configIndex) pairs to randomize materials/labels. Ensures different configs for each target if possible.
        /// !!! SHOULD ONLY BE USED in editor for testing randomization on demand, the final dataset generation is handled by VisualTargetRandomizer to ensure consistency across runs.
        /// </summary>
        /// <returns>Array of (targetIndex, configIndex) pairs</returns>
        public (int, int)[] GetRandomConfigIndices()
        {
            List<(int, int)> indices = new List<(int, int)>();
            int firstIndex = Random.Range(0, configs.Length);
            indices.Add((0, firstIndex));
            if (targets.Length > 1 && targets[1] != null)
            {
                if (configs.Length > 1)
                {
                    int secondIndex;
                    do
                    {
                        secondIndex = Random.Range(0, configs.Length);
                    } while (secondIndex == firstIndex);
                    indices.Add((1, secondIndex));
                }
                else
                {
                    // Fallback if only 1 config provided
                    indices.Add((1, firstIndex));
                }
            }
            return indices.ToArray();
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
        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("Randomize Materials & Labels Now", GUILayout.Height(30)))
        {
            if (script.targets != null)
            {
                // Gather targets and their labeling components for Undo
                var objectsToUndo = new System.Collections.Generic.List<Object>();
                foreach (var t in script.targets)
                {
                    if (t == null) continue;
                    objectsToUndo.Add(t);
                    if (t.TryGetComponent<Labeling>(out var l)) objectsToUndo.Add(l);
                }

                Undo.RecordObjects(objectsToUndo.ToArray(), "Randomize Visual Targets");

                foreach (var (targetIndex, configIndex) in script.GetRandomConfigIndices())
                {
                    script.RandomizeMaterials(targetIndex, configIndex);
                }

                foreach (var t in script.targets)
                {
                    if (t != null)
                    {
                        EditorUtility.SetDirty(t);
                        if (t.TryGetComponent<Labeling>(out var l)) EditorUtility.SetDirty(l);
                    }
                }
            }
        }
        GUI.backgroundColor = Color.white;
    }
}
#endif