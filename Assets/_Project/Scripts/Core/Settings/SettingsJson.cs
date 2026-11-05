using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Small helpers for the settings files: writing only the values that differ from the scene
/// defaults, and parsing numbers the same way on every machine locale.
/// </summary>
public static class SettingsJson
{
    /// <summary>
    /// Serializes <paramref name="current"/> with JsonUtility but keeps only the keys whose value
    /// differs from <paramref name="defaults"/>. Both objects must be the same flat type
    /// (one primitive field per line in JsonUtility's pretty output).
    /// Writing only the changes means a later change to the scene defaults still reaches
    /// everyone who did not override that particular value.
    /// </summary>
    public static string ToOverridesJson(object current, object defaults)
    {
        string[] cur = JsonUtility.ToJson(current, true).Split('\n');
        string[] def = JsonUtility.ToJson(defaults, true).Split('\n');

        var kept = new List<string>();
        // The first and last lines are the braces of the object.
        for (int i = 1; i < cur.Length - 1; i++)
        {
            string line = cur[i].TrimEnd('\r').TrimEnd(',');
            string defLine = i < def.Length - 1 ? def[i].TrimEnd('\r').TrimEnd(',') : null;
            if (line != defLine)
            {
                kept.Add(line);
            }
        }

        return kept.Count == 0 ? "{}\n" : "{\n" + string.Join(",\n", kept) + "\n}\n";
    }

    /// <summary>
    /// Parses a float written by either an invariant-culture writer ("40.9") or a
    /// current-culture one (for example "40,9" on a French locale).
    /// </summary>
    public static bool TryParseFloat(string text, out float value)
    {
        return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
            || float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
    }

    /// <summary>Parses an int with the invariant culture first, then the current culture.</summary>
    public static bool TryParseInt(string text, out int value)
    {
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
            || int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out value);
    }
}
