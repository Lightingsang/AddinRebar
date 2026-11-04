using System;
using HPRebar.Core.KataExport.Calculators;

namespace HPRebar.Core.KataExport.ColumnStack;

/// <summary>
/// Kata places a storey relative to the storey below it with the sign of b and h (sheet "Cot", note of C11,
/// confirmed by drawing variants with Kata's VC command): a negative size keeps the left (bottom) face of the
/// storey below, a positive size is centred on it. Any other placement cannot be written; the nearer of the two
/// is chosen and reported.
/// </summary>
public static class KataColumnStepRule
{
    /// <summary>Signed b and h for <paramref name="upper"/>; <paramref name="lower"/> null for the first storey.</summary>
    public static KataColumnStep Place(KataColumnSection upper, KataColumnSection? lower, string levelName)
    {
        if (upper.Shape == KataColumnShape.Circle)
        {
            return PlaceCircle(upper, lower, levelName);
        }

        if (lower is null)
        {
            return new KataColumnStep(KataFormat.Round(upper.WidthMm), KataFormat.Round(upper.DepthMm), null);
        }

        var b = PlaceAxis(upper.MinXMm, upper.MaxXMm, lower.MinXMm, lower.MaxXMm);
        var h = PlaceAxis(upper.MinYMm, upper.MaxYMm, lower.MinYMm, lower.MaxYMm);
        string? warning = Join(Describe(levelName, ("b", b), ("h", h)), Growth(upper, lower, b, h, levelName));
        return new KataColumnStep(b.SignedSizeMm, h.SignedSizeMm, warning);
    }

    private static KataColumnStep PlaceCircle(KataColumnSection upper, KataColumnSection? lower, string levelName)
    {
        double diameter = KataFormat.Round(upper.WidthMm);
        if (lower is null)
        {
            return new KataColumnStep(diameter, 0, null);
        }

        double offset = Math.Max(Math.Abs(upper.CentreXMm - lower.CentreXMm), Math.Abs(upper.CentreYMm - lower.CentreYMm));
        string? warning = offset > KataTolerance.StationMm
            ? $"{levelName}: the round column is {KataFormat.Whole(offset)} mm off the centre of the storey below; Kata draws it centred."
            : null;
        return new KataColumnStep(diameter, 0, warning);
    }

    private static AxisPlacement PlaceAxis(double upperMin, double upperMax, double lowerMin, double lowerMax)
    {
        double size = KataFormat.Round(upperMax - upperMin);
        double centreOffset = Math.Abs((upperMin + upperMax) / 2 - (lowerMin + lowerMax) / 2);
        double faceOffset = Math.Abs(upperMin - lowerMin);

        if (centreOffset <= KataTolerance.StationMm)
        {
            return new AxisPlacement(size, null);
        }

        bool sizeChanges = Math.Abs((upperMax - upperMin) - (lowerMax - lowerMin)) > KataTolerance.StationMm;
        if (faceOffset <= KataTolerance.StationMm && sizeChanges)
        {
            return new AxisPlacement(-size, null);
        }

        return faceOffset < centreOffset && sizeChanges
            ? new AxisPlacement(-size, faceOffset)
            : new AxisPlacement(size, centreOffset);
    }

    /// <summary>
    /// Kata drew a wider storey centred on a narrower one (sign +); a wider storey keeping the left face (sign -) was
    /// never drawn, so it is written by the same rule and reported.
    /// </summary>
    private static string? Growth(KataColumnSection upper, KataColumnSection lower, AxisPlacement b, AxisPlacement h, string levelName)
    {
        bool growsOneSided = (b.SignedSizeMm < 0 && upper.WidthMm > lower.WidthMm + KataTolerance.StationMm)
                             || (h.SignedSizeMm < 0 && upper.DepthMm > lower.DepthMm + KataTolerance.StationMm);
        return growsOneSided ? $"{levelName}: larger than the storey below and kept on its left face; check Kata's drawing of the step." : null;
    }

    private static string? Join(string? first, string? second) =>
        first is null ? second : second is null ? first : $"{first} {second}";

    private static string? Describe(string levelName, params (string Axis, AxisPlacement Placement)[] axes)
    {
        string? text = null;
        foreach (var (axis, placement) in axes)
        {
            if (placement.MisfitMm is not { } misfit)
            {
                continue;
            }

            string kind = placement.SignedSizeMm < 0 ? "keeping the left face" : "centred";
            string part = $"{axis} is {KataFormat.Whole(misfit)} mm away from both Kata placements; written {kind}";
            text = text is null ? $"{levelName}: {part}" : $"{text}; {part}";
        }

        return text is null ? null : text + ".";
    }

    private readonly record struct AxisPlacement(double SignedSizeMm, double? MisfitMm);
}

/// <summary>Signed sizes of a storey for columns C and D, with a warning when the placement had to be rounded.</summary>
public sealed record KataColumnStep(double SignedBMm, double SignedHMm, string? Warning);
