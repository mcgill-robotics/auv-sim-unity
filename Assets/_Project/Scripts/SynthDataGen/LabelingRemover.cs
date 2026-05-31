using UnityEngine;
using UnityEngine.Perception.GroundTruth.LabelManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class LabelingRemover : MonoBehaviour
{
    [ContextMenu("Remove Labeling Components")]
    public void RemoveLabeling()
    {
        int clCount = 0;
        int lCount = 0;

        // ConditionalLabeling requires Labeling, so it must be removed first
        ConditionalLabeling[] conditionalLabelings = GetComponentsInChildren<ConditionalLabeling>(true);
        foreach (var cl in conditionalLabelings)
        {
#if UNITY_EDITOR
            Undo.DestroyObjectImmediate(cl);
#else
            Destroy(cl);
#endif
            clCount++;
        }

        Labeling[] labelings = GetComponentsInChildren<Labeling>(true);
        foreach (var l in labelings)
        {
#if UNITY_EDITOR
            Undo.DestroyObjectImmediate(l);
#else
            Destroy(l);
#endif
            lCount++;
        }

        Debug.Log($"Removed {clCount} ConditionalLabeling and {lCount} Labeling components from {gameObject.name} and its children.");
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(LabelingRemover))]
public class LabelingRemoverEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LabelingRemover remover = (LabelingRemover)target;

        GUILayout.Space(10);
        if (GUILayout.Button("Remove Labeling Components", GUILayout.Height(30)))
        {
            remover.RemoveLabeling();
        }
    }
}
#endif
