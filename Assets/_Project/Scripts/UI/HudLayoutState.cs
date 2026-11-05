using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Which HUD drawers are open. Kept in its own small file (hud_layout.json) so that opening a
/// drawer never touches the simulation settings, and deleting one file does not reset the other.
/// </summary>
[Serializable]
public class HudLayoutState
{
    public bool ConfigOpen = true;
    public bool ControlsOpen = true;
    public bool TelemetryOpen = true;
    public bool SensorsOpen = true;
    public bool CameraOpen = true;

    private static HudLayoutState current;

    /// <summary>Full path of the layout file.</summary>
    public static string FilePath => Path.Combine(Application.persistentDataPath, "hud_layout.json");

    /// <summary>True if a layout file has already been written on this machine.</summary>
    public static bool FileExists => File.Exists(FilePath);

    /// <summary>The layout state, loaded from disk the first time it is used.</summary>
    public static HudLayoutState Current
    {
        get
        {
            if (current == null)
            {
                current = new HudLayoutState();
                if (Application.isPlaying && FileExists)
                {
                    try
                    {
                        JsonUtility.FromJsonOverwrite(File.ReadAllText(FilePath), current);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[HudLayoutState] Could not read '{FilePath}': {e.Message}. Using defaults.");
                    }
                }
            }
            return current;
        }
    }

    /// <summary>Writes the current layout to disk. Does nothing outside play mode.</summary>
    public static void Save()
    {
        if (!Application.isPlaying) return;
        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(Current, true));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[HudLayoutState] Could not write '{FilePath}': {e.Message}");
        }
    }
}
