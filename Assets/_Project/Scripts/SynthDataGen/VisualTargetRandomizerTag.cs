using UnityEngine;
using UnityEngine.Perception.Randomization.Scenarios;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.Perception.Randomization.Randomizers.Tags;
using System.Collections.Generic;
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
        [Header("Targets")]
        [Tooltip("Assign 1 quad (for Board) or 2 quads (for Gate)")]
        public GameObject[] targets;

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
            // TODO add a validation method to check all targets have same number of configs and all children have Labeling + Material
            return targets[0].transform.childCount;
        }

        /// <summary>
        /// Randomizes materials and labels. Enables the config at configIndex and disables the others.
        /// </summary>
        public void RandomizeMaterials(int targetIndex, int configIndex)
        {
            if (targets == null) return;
            if (targetIndex < 0 || targetIndex >= targets.Length) return;
            if (configIndex < 0 || configIndex >= GetConfigCount()) return;
            ApplyConfig(targets[targetIndex], configIndex);
        }

        private void ApplyConfig(GameObject target, int configIndex)
        {
            if (target == null) return;

            for (int i = 0; i < GetConfigCount(); i++)
            {
                if (i == configIndex)
                {
                    target.transform.GetChild(i).gameObject.SetActive(true);
                    foreach (Labeling l in target.transform.GetChild(i).gameObject.GetComponentsInChildren<Labeling>())
                    {
                        l.RefreshLabeling();
                    }
                }
                else
                {
                    target.transform.GetChild(i).gameObject.SetActive(false);
                }
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
            int ConfigCount = GetConfigCount();
            int firstIndex = Random.Range(0, ConfigCount);
            indices.Add((0, firstIndex));
            if (targets.Length > 1 && targets[1] != null)
            {
                if (ConfigCount > 1)
                {
                    int secondIndex;
                    do
                    {
                        secondIndex = Random.Range(0, ConfigCount);
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
                // TODO fix this to undo all variants
                var objectsToUndo = new List<Object>();
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