using System;

namespace HPRebar.Core.BeamRebar;

/// <summary>
/// Numerical precision thresholds and floating-point comparisons across all beam calculations.
/// </summary>
public static class Tolerance
{
    public const double Default = 1.0e-9;
    public const double CollinearToleranceMm = 1.0e-6;
    public const double MinimumSegmentMm = 1.0;

    public static bool AreEqual(double first, double second, double tolerance = Default) =>
        second - tolerance < first && first < second + tolerance;

    public static bool IsZero(double value, double tolerance = Default) =>
        Math.Abs(value) < tolerance;

    public static bool IsPositive(double value, double tolerance = Default) =>
        value > tolerance;

    public static bool IsNegative(double value, double tolerance = Default) =>
        value < -tolerance;

    public static bool IsGreaterOrEqual(double first, double second, double tolerance = Default) =>
        first > second - tolerance;

    public static bool IsLessOrEqual(double first, double second, double tolerance = Default) =>
        first < second + tolerance;
}
