namespace HPRebar.Core.ColumnRebar;

/// <summary>
///     Floating-point comparison used throughout the port. The 1e-9 window is the one the source tool used;
///     changing it would change which bars get merged into the same schedule row.
/// </summary>
public static class Tolerance
{
    public const double Default = 1.0e-9;

    public static bool AreEqual(double first, double second, double tolerance = Default) =>
        second - tolerance < first && first < second + tolerance;
}
