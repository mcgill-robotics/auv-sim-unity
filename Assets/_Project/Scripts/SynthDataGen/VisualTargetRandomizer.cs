using UnityEngine;
using UnityEngine.Perception.Randomization.Scenarios;
using UnityEngine.Perception.GroundTruth.LabelManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Randomizes materials and labels on 1 or 2 quads (e.g., Gate targets or Task Boards) every frame.
/// Pairs each material with a specific label to ensure ground truth matches the visual.
/// </summary>
public class VisualTargetRandomizer : MonoBehaviour
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

    private int _lastIteration = -1;

    private void Start()
    {
        RandomizeMaterials();
    }

    private void Update()
    {
        var scenario = ScenarioBase.activeScenario;
        if (scenario == null) return;

        if (scenario.currentIteration != _lastIteration)
        {
            _lastIteration = scenario.currentIteration;
            RandomizeMaterials();
        }
    }

    /// <summary>
    /// Randomizes materials and labels. Ensures Left != Right if there are two targets.
    /// </summary>
    public void RandomizeMaterials()
    {
        if (configs == null || configs.Length == 0 || targets == null || targets.Length == 0)
            return;

        // Pick first config
        int firstIndex = Random.Range(0, configs.Length);
        ApplyConfig(targets[0], configs[firstIndex]);

        // If there's a second target, pick a different config if possible
        if (targets.Length > 1 && targets[1] != null)
        {
            if (configs.Length > 1)
            {
                int secondIndex;
                do
                {
                    secondIndex = Random.Range(0, configs.Length);
                } while (secondIndex == firstIndex);
                
                ApplyConfig(targets[1], configs[secondIndex]);
            }
            else
            {
                // Fallback if only 1 config provided
                ApplyConfig(targets[1], configs[firstIndex]);
            }
        }
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
}

#if UNITY_EDITOR
[CustomEditor(typeof(VisualTargetRandomizer))]
public class VisualTargetRandomizerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        VisualTargetRandomizer script = (VisualTargetRandomizer)target;

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
                
                script.RandomizeMaterials();
                
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
