using System.Globalization;
using UnityEngine;

/// <summary>
/// Number formatting and angle helpers shared by the HUD controllers. Everything is formatted
/// with the invariant culture so the HUD shows "1.25" on every machine locale.
/// </summary>
public static class HudFormat
{
    /// <summary>Formats a number with a .NET format string using the invariant culture.</summary>
    public static string Num(float value, string format) => value.ToString(format, CultureInfo.InvariantCulture);

    /// <summary>Formats a number with a .NET format string using the invariant culture.</summary>
    public static string Num(double value, string format) => value.ToString(format, CultureInfo.InvariantCulture);

    /// <summary>
    /// Roll, pitch and yaw in degrees from a ROS FLU quaternion, using the ZYX (yaw-pitch-roll)
    /// order that ROS tools such as tf2 and the AUV use. Unity's eulerAngles uses a different axis
    /// order and frame, so reading it directly gives wrong numbers once the vehicle tilts.
    /// </summary>
    public static Vector3 RollPitchYawDegrees(Quaternion q)
    {
        double x = q.x, y = q.y, z = q.z, w = q.w;

        double sinrCosp = 2.0 * (w * x + y * z);
        double cosrCosp = 1.0 - 2.0 * (x * x + y * y);
        double roll = System.Math.Atan2(sinrCosp, cosrCosp);

        double sinp = 2.0 * (w * y - z * x);
        double pitch = System.Math.Abs(sinp) >= 1.0
            ? System.Math.Sign(sinp) * System.Math.PI / 2.0
            : System.Math.Asin(sinp);

        double sinyCosp = 2.0 * (w * z + x * y);
        double cosyCosp = 1.0 - 2.0 * (y * y + z * z);
        double yaw = System.Math.Atan2(sinyCosp, cosyCosp);

        const double toDeg = 180.0 / System.Math.PI;
        return new Vector3((float)(roll * toDeg), (float)(pitch * toDeg), (float)(yaw * toDeg));
    }
}
