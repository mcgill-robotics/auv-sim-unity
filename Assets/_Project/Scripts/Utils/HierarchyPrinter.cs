using UnityEngine;
using UnityEngine.SceneManagement;
using System.Text;

public class HierarchyPrinter : MonoBehaviour
{
    void Start()
    {
        Debug.Log("--- Dumping Scene Hierarchy and Components ---");
        DumpSceneHierarchy();
        Debug.Log("--- Dump Complete ---");
    }

    void DumpSceneHierarchy()
    {
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject rootObject in rootObjects)
        {
            // Start the recursive traversal for each root object
            TraverseHierarchy(rootObject.transform, 0);
        }
    }

    void TraverseHierarchy(Transform parentTransform, int depth)
    {
        string indent = new string(' ', depth * 4);
        GameObject go = parentTransform.gameObject;

        // Log the GameObject name as its own entry
        Debug.Log($"{indent}- GameObject: {go.name}");

        Component[] components = go.GetComponents<Component>();
        foreach (Component component in components)
        {
            if (component != null)
            {
                // Log each component as its own entry
                Debug.Log($"{indent}    [Component: {component.GetType().Name}]");
            }
        }

        // Recursively call this function for each child
        foreach (Transform childTransform in parentTransform)
        {
            TraverseHierarchy(childTransform, depth + 1);
        }
    }
}
