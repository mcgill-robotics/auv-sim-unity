using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Lets a player build open any scene in the build list from the command line, e.g.
///   ./auv-sim.x86_64 -scene McGill_Pool
/// Without the argument the first scene in the build list (RoboSub2026) opens as usual.
/// Does nothing in the editor.
/// </summary>
public static class SceneSelector
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LoadSceneFromCommandLine()
    {
        if (Application.isEditor) return;

        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] != "-scene") continue;

            string requested = args[i + 1];
            if (requested == SceneManager.GetActiveScene().name) return;

            for (int b = 0; b < SceneManager.sceneCountInBuildSettings; b++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(b);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == requested)
                {
                    SceneManager.LoadScene(b);
                    return;
                }
            }

            Debug.LogError($"[SceneSelector] Scene '{requested}' is not in the build. Available: {ListBuildScenes()}");
            return;
        }
    }

    private static string ListBuildScenes()
    {
        var names = new string[SceneManager.sceneCountInBuildSettings];
        for (int b = 0; b < names.Length; b++)
        {
            names[b] = System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(b));
        }
        return string.Join(", ", names);
    }
}
