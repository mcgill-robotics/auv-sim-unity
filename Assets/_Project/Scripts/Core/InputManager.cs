using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Names and default keys of every rebindable keyboard action. The names are also the
/// PlayerPrefs keys a rebinding is stored under.
/// </summary>
public static class InputBindings
{
    public const string Surge = "surgeKeybind";
    public const string NegSurge = "negSurgeKeybind";
    public const string Sway = "swayKeybind";
    public const string NegSway = "negSwayKeybind";
    public const string Heave = "heaveKeybind";
    public const string NegHeave = "negHeaveKeybind";
    public const string Yaw = "yawKeybind";
    public const string NegYaw = "negYawKeybind";
    public const string Pitch = "pitchKeybind";
    public const string NegPitch = "negPitchKeybind";
    public const string Roll = "rollKeybind";
    public const string NegRoll = "negRollKeybind";
    public const string Freeze = "freezeKeybind";
    public const string Dropper = "dropperKeybind";
    public const string Gripper = "gripperKeybind";
    public const string TorpedoLaunch = "torpedoLaunchKeybind";
    public const string TorpedoReset = "torpedoResetKeybind";
    public const string CameraSnapshot = "cameraSnapshotKeybind";
    public const string CameraCycle = "cameraCycleKeybind";
    public const string CameraToggle = "cameraToggleKeybind";

    /// <summary>Default key for each binding.</summary>
    public static readonly IReadOnlyDictionary<string, KeyCode> Defaults = new Dictionary<string, KeyCode>
    {
        { Surge, KeyCode.W }, { NegSurge, KeyCode.S },
        { Sway, KeyCode.A }, { NegSway, KeyCode.D },
        { Heave, KeyCode.E }, { NegHeave, KeyCode.Q },
        { Yaw, KeyCode.J }, { NegYaw, KeyCode.L },
        { Pitch, KeyCode.I }, { NegPitch, KeyCode.K },
        { Roll, KeyCode.O }, { NegRoll, KeyCode.U },
        { Freeze, KeyCode.Space },
        { Dropper, KeyCode.G }, { Gripper, KeyCode.H },
        { TorpedoLaunch, KeyCode.T }, { TorpedoReset, KeyCode.Y },
        { CameraSnapshot, KeyCode.P }, { CameraCycle, KeyCode.V }, { CameraToggle, KeyCode.C },
    };

    /// <summary>Bindings that drive the vehicle; the HUD releases UI focus when one is held.</summary>
    public static readonly string[] Movement =
    {
        Surge, NegSurge, Sway, NegSway, Heave, NegHeave, Yaw, NegYaw, Pitch, NegPitch, Roll, NegRoll, Freeze,
    };
}

/// <summary>
/// Singleton that manages rebindable keyboard inputs. Rebindings are stored in PlayerPrefs and
/// cached in memory, so asking for a key every frame costs a dictionary lookup.
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private readonly Dictionary<string, KeyCode> cache = new Dictionary<string, KeyCode>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Only remove this component: the GameObject also carries the settings components.
            Debug.LogWarning("[InputManager] A second InputManager was found; removing the duplicate component.");
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Returns the key bound to <paramref name="keyName"/>, or <paramref name="defaultKey"/> if it was
    /// never rebound. PlayerPrefs is read only the first time a name is asked for.
    /// </summary>
    public KeyCode GetKey(string keyName, KeyCode defaultKey)
    {
        if (cache.TryGetValue(keyName, out KeyCode key)) return key;

        key = defaultKey;
        if (PlayerPrefs.HasKey(keyName) && Enum.TryParse(PlayerPrefs.GetString(keyName), out KeyCode saved))
        {
            key = saved;
        }
        cache[keyName] = key;
        return key;
    }

    /// <summary>Returns the key bound to one of the <see cref="InputBindings"/> names.</summary>
    public KeyCode GetKey(string keyName)
    {
        return GetKey(keyName, InputBindings.Defaults.TryGetValue(keyName, out KeyCode def) ? def : KeyCode.None);
    }

    /// <summary>Rebinds an action and stores it in PlayerPrefs.</summary>
    public void SetKey(string keyName, KeyCode key)
    {
        PlayerPrefs.SetString(keyName, key.ToString());
        PlayerPrefs.Save();
        cache[keyName] = key;
    }

    /// <summary>Removes every stored rebinding. Other PlayerPrefs (for example the clock) are kept.</summary>
    public void ResetToDefaults()
    {
        foreach (string name in InputBindings.Defaults.Keys) PlayerPrefs.DeleteKey(name);
        foreach (string name in cache.Keys) PlayerPrefs.DeleteKey(name);
        PlayerPrefs.Save();
        cache.Clear();
        Debug.Log("[InputManager] Input bindings reset to defaults.");
    }
}
