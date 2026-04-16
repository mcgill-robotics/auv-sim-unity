using UnityEngine;
using UnityEngine.Perception.Randomization.Scenarios;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.Perception.Randomization.Randomizers.Tags;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.EditorTools;




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
        [Tooltip("Reference target all target will be set to.")]
        public Transform referenceTarget;
        [Tooltip("The target objects to randomize, their children will be made to match the reference target's children (configs) with different materials and labels. Can be 1 or 2 targets, if 2 ensures different configs for each.")]
        public Transform[] targets;

        void Awake()
        {
            SetTargetsToReference(); // ensure all targets start with the same visuals and labels as the reference before randomization begins
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
            // TODO add a validation method to check all targets have same number of configs and all children have Labeling + Material
            return referenceTarget.transform.childCount;
        }
        /// <summary>
        /// Clones the reference target's children into each target, so they all start with the same set of configs/variants. Then randomization will just enable/disable the existing children to ensure consistent pairing of visuals and labels.
        /// </summary>
        public void SetTargetsToReference()
        {
            if (referenceTarget == null || targets == null) return;
            // Clone the reference's children into every target anchor
            foreach (Transform target in targets)
            {
                if (target == null) continue;
                if (target == referenceTarget) continue; // skip the reference target
                for (int i = target.childCount - 1; i >= 0; i--)
                {
                    // Destroy existing children to ensure we start with a clean slate and avoid duplicates if this is called multiple times
                    DestroyImmediate(target.GetChild(i).gameObject);
                }
                // iterate over reference child and copy into target
                for (int i = 0; i < referenceTarget.transform.childCount; i++)
                {
                    Transform referenceChild = referenceTarget.transform.GetChild(i);

                    // Instantiate clones the scene object exactly as it is
                    GameObject clone = Instantiate(referenceChild.gameObject, target);

                    // Reset local transforms so it perfectly snaps to the target anchor
                    clone.transform.localPosition = Vector3.zero;
                    clone.transform.localRotation = Quaternion.identity;

                    // Inherit the scale from the reference child, while the parent anchor scales the whole thing
                    clone.transform.localScale = referenceChild.localScale;

                    // Ensure it starts disabled
                    clone.SetActive(false);
                }
            }
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

        private void ApplyConfig(Transform target, int configIndex)
        {
            if (target == null) return;

            for (int i = 0; i < GetConfigCount(); i++)
            {
                // Enable the selected config and disable the others
                if (i == configIndex)
                {
                    target.transform.GetChild(i).gameObject.SetActive(true);
                    // also update the labels to ensure ground truth matches the new visual
                    foreach (Labeling l in target.transform.GetChild(i).gameObject.GetComponentsInChildren<Labeling>())
                    {
                        // check if the label's game object is conditionally labeled
                        if (l.gameObject.TryGetComponent<ConditionalLabeling>(out var cl))
                        {
                            l.enabled = cl.ShouldLabel(); // update the ConditionalLabeling state based on current camera position/angle
                        }
                        if (l.isActiveAndEnabled) l.RefreshLabeling(); // refresh to ensure ground truth is updated with the new material/visual
                    }
                }
                else
                {
                    target.transform.GetChild(i).gameObject.SetActive(false);
                }
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
        DrawSyncSetup(script);

        EditorGUILayout.Space();
        DrawRandomize(script);
    }

    private void DrawSyncSetup(VisualTargetRandomizerTag script)
    {
        GUI.backgroundColor = new Color(1f, 0.8f, 0.4f); // Orange-ish warning color
        EditorGUILayout.HelpBox("Set up all variant children inside Reference Target. Then click button below to sync all other targets to match it.Clicking this button will also randomize the materials and labels (see button further down)", MessageType.Info);

        if (GUILayout.Button("Sync Targets to Reference", GUILayout.Height(30)))
        {
            Undo.RecordObjects(GatherUndoObjects(script), "Reset Visual Targets");

            script.SetTargetsToReference();

            foreach (var t in script.targets)
            {
                if (t != null)
                {
                    EditorUtility.SetDirty(t);
                    if (t.TryGetComponent<Labeling>(out var l)) EditorUtility.SetDirty(l);
                }
            }
            GUIRandomizeMaterials(script); // also randomize to apply the new visuals and labels to the scene immediately
        }
        GUI.backgroundColor = Color.white;
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
        foreach (var (targetIndex, configIndex) in GetRandomConfigIndices(script.targets, script.GetConfigCount()))
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
    /// <summary>
    /// Generates a list of (targetIndex, configIndex) pairs to randomize materials/labels. Ensures different configs for each target if possible.
    /// Only used in editor for testing randomization on demand, the final dataset generation is handled by VisualTargetRandomizer to ensure consistency across runs.
    /// </summary>
    /// <returns>Array of (targetIndex, configIndex) pairs</returns>
    public (int, int)[] GetRandomConfigIndices(Transform[] targets, int ConfigCount)
    {
        List<(int, int)> indices = new List<(int, int)>();
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