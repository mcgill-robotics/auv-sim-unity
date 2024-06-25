using UnityEditor;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

// Output the build size or a failure depending on BuildPlayer.

public class BuildPlayerExample
{
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
            Debug.Log("Build failed");
        }
    }
    [MenuItem("Build/Build Linux")]
    public static void BuildLinux()
    {
        string[] scenes = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/SquarePool.unity" };
        Build(scenes, "Builds/Linux/sim.x86_64", BuildTarget.StandaloneLinux64);
    }

    [MenuItem("Build/Build Windows")]
    public static void BuildWindows()
    {
        string[] scenes = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/SquarePool.unity" };
        Build(scenes, "Builds/Windows/sim.exe", BuildTarget.StandaloneWindows64);
    }

    [MenuItem("Build/Build Mac")]
    public static void BuildMac()
    {
        string[] scenes = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/SquarePool.unity" };
        Build(scenes, "Builds/Mac/sim.app", BuildTarget.StandaloneOSX);
    }

    [MenuItem("Build/Build All")]
    public static void BuildAll()
    {
        BuildLinux();
        BuildWindows();
        BuildMac();
    }
}