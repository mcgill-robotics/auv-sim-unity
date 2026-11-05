using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Build menu for local player builds (Build > ...). CI does not use this; it builds through GameCI
/// with the same scene list (File > Build Profiles) and the same output name.
/// </summary>
public static class BuildMenu
{
    private const string OutputName = "auv-sim";
    private const string FallbackScene = "Assets/_Project/Scenes/Main_Scenes/RoboSub2026.unity";

    private static string[] GetEnabledScenes()
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        return scenes.Length > 0 ? scenes : new[] { FallbackScene };
    }

    public static void Build(string path, BuildTarget target)
    {
        var options = new BuildPlayerOptions
        {
            scenes = GetEnabledScenes(),
            locationPathName = path,
            target = target,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildMenu] Build succeeded: {path} ({summary.totalSize / (1024 * 1024)} MB)");
        }
        else
        {
            Debug.LogError($"[BuildMenu] Build {summary.result}: {path}");
        }
    }

    [MenuItem("Build/Build Linux")]
    public static void BuildLinux() => Build($"Builds/StandaloneLinux64/{OutputName}.x86_64", BuildTarget.StandaloneLinux64);

    [MenuItem("Build/Build Windows")]
    public static void BuildWindows() => Build($"Builds/StandaloneWindows64/{OutputName}.exe", BuildTarget.StandaloneWindows64);

    [MenuItem("Build/Build Mac")]
    public static void BuildMac() => Build($"Builds/StandaloneOSX/{OutputName}.app", BuildTarget.StandaloneOSX);

    [MenuItem("Build/Build All")]
    public static void BuildAll()
    {
        BuildLinux();
        BuildWindows();
        BuildMac();
    }
}
