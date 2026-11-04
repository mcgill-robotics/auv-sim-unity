using UnityEngine;

/// <summary>
/// Single place that decides whether keyboard shortcuts should reach the simulation.
/// Every script that reads a gameplay key (thrusters, dropper, torpedo, camera toggle, ...)
/// should go through this, so typing in a HUD text field never moves the AUV.
/// </summary>
public static class InputGate
{
    /// <summary>
    /// True when gameplay keys are allowed: no HUD text field has keyboard focus.
    /// </summary>
    public static bool GameKeysEnabled => SimulatorHUD.Instance == null || !SimulatorHUD.Instance.IsInputFocused;

    /// <summary>Input.GetKey, but false while a HUD text field has focus.</summary>
    public static bool GetKey(KeyCode key) => GameKeysEnabled && Input.GetKey(key);

    /// <summary>Input.GetKeyDown, but false while a HUD text field has focus.</summary>
    public static bool GetKeyDown(KeyCode key) => GameKeysEnabled && Input.GetKeyDown(key);
}
