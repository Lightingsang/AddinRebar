namespace HPKata.Revit.Bridge;

/// <summary>
/// Adapter resolving hook angles, types, and orientations according to TCVN 5574:2018.
/// </summary>
public static class RevitHookAdapter
{
    public const int HookAngle90 = 90;
    public const int HookAngle135 = 135;
    public const int HookAngle180 = 180;

    /// <summary>
    /// Computes minimum standard hook length (in mm) based on bar diameter and bend angle.
    /// </summary>
    public static double GetStandardHookLengthMm(int diameterMm, int hookAngleDeg)
    {
        return hookAngleDeg switch
        {
            HookAngle135 => Math.Max(75.0, 10.0 * diameterMm),
            HookAngle180 => Math.Max(60.0, 5.0 * diameterMm),
            _ => Math.Max(70.0, 8.0 * diameterMm) // Default 90 degrees
        };
    }

    /// <summary>
    /// Evaluates if a specified angle corresponds to a valid standard structural hook.
    /// </summary>
    public static bool IsStandardHook(int angleDeg) =>
        angleDeg is HookAngle90 or HookAngle135 or HookAngle180;
}
