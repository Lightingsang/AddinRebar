namespace HPRebar.Core.Shared;

/// <summary>
/// Checks for numbers a user can type into a window but no geometry can use: NaN and ±∞ pass a plain
/// <c>&lt;= 0</c> test, so a spacing or offset is checked with these instead.
/// </summary>
public static class FiniteNumber
{
    /// <summary>Neither NaN nor ±∞ (netstandard2.0 has no <c>double.IsFinite</c>).</summary>
    public static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    /// <summary>A finite number greater than zero.</summary>
    public static bool IsPositive(double value) => value > 0 && !double.IsInfinity(value);
}
