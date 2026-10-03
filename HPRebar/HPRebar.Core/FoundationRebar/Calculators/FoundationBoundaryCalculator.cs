using System;
using HPRebar.Core.FoundationRebar.Models;

namespace HPRebar.Core.FoundationRebar.Calculators;

/// <summary>
/// Effective 2D boundary rectangle after subtracting side concrete covers.
/// </summary>
public sealed record FoundationEffectiveBoundary(
    double XMin,
    double XMax,
    double YMin,
    double YMax,
    double EffectiveLength,
    double EffectiveWidth);

/// <summary>
/// Calculates effective 2D placement boundaries and spans after deducting concrete side covers.
/// </summary>
public static class FoundationBoundaryCalculator
{
    /// <summary>
    /// Computes effective 2D layout boundaries from dimensions and lateral cover.
    /// </summary>
    public static FoundationEffectiveBoundary Calculate(double length, double width, double coverSide)
    {
        double xMin = coverSide;
        double xMax = length - coverSide;
        double yMin = coverSide;
        double yMax = width - coverSide;

        double effLength = Math.Max(0.0, xMax - xMin);
        double effWidth = Math.Max(0.0, yMax - yMin);

        return new FoundationEffectiveBoundary(xMin, xMax, yMin, yMax, effLength, effWidth);
    }

    /// <summary>
    /// Computes effective 2D layout boundaries from snapshot and lateral cover.
    /// </summary>
    public static FoundationEffectiveBoundary Calculate(FoundationGeometrySnapshot snapshot, double coverSide)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
        return Calculate(snapshot.Length, snapshot.Width, coverSide);
    }
}
