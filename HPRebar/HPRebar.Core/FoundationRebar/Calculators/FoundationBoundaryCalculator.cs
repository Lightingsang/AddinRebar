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

    /// <summary>
    /// Tuple helper returning (xMin, xMax, yMin, yMax) bounds.
    /// </summary>
    public static (double XMin, double XMax, double YMin, double YMax) ComputeEffectiveBoundary(
        FoundationGeometrySnapshot snapshot, double coverSide)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
        return (coverSide, snapshot.Length - coverSide, coverSide, snapshot.Width - coverSide);
    }

    /// <summary>
    /// Validates whether the lateral cover allows positive layout spans.
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) ValidateBoundary(double length, double width, double coverSide)
    {
        if (coverSide < 0)
            return (false, "Side cover cannot be negative.");
        if (length <= 2 * coverSide)
            return (false, $"Foundation Length ({length:F1} mm) must be greater than 2 * Side Cover ({2 * coverSide:F1} mm).");
        if (width <= 2 * coverSide)
            return (false, $"Foundation Width ({width:F1} mm) must be greater than 2 * Side Cover ({2 * coverSide:F1} mm).");

        return (true, null);
    }

    /// <summary>
    /// Validates whether the lateral cover allows positive layout spans for the given snapshot.
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) ValidateBoundary(FoundationGeometrySnapshot snapshot, double coverSide)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
        return ValidateBoundary(snapshot.Length, snapshot.Width, coverSide);
    }
}
