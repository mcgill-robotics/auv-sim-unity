using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildPlayerExample
{
    private static string[] GetEnabledScenes()
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            scenes = new string[] { "Assets/_Project/Scenes/Main_Scenes/RoboSub2026.unity" };
        }

        return scenes;
    }

    public static void Build(string[] scenes, string path, BuildTarget target)
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = scenes;
        buildPlayerOptions.locationPathName = path;
        buildPlayerOptions.target = target;
        buildPlayerOptions.options = BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
        }

        if (summary.result == BuildResult.Failed)
        {
            Debug.LogError("Build failed");
        }
    }

    [MenuItem("Build/Build Linux")]
    public static void BuildLinux()
    {
        Build(GetEnabledScenes(), "Builds/Linux/sim.x86_64", BuildTarget.StandaloneLinux64);
    }

    [MenuItem("Build/Build Windows")]
    public static void BuildWindows()
    {
        Build(GetEnabledScenes(), "Builds/Windows/sim.exe", BuildTarget.StandaloneWindows64);
    }

    [MenuItem("Build/Build Mac")]
    public static void BuildMac()
    {
        Build(GetEnabledScenes(), "Builds/Mac/sim.app", BuildTarget.StandaloneOSX);
    }

    [MenuItem("Build/Build All")]
    public static void BuildAll()
    {
        BuildLinux();
        BuildWindows();
        BuildMac();
    }
}