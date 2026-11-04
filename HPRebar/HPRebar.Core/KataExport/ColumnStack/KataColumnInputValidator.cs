using System;
using HPRebar.Core.KataExport.Calculators;

namespace HPRebar.Core.KataExport.ColumnStack;

/// <summary>
/// Rejects stacks Kata cannot draw: Kata adds the storey heights from the cap bottom, so a gap or an overlap
/// between storeys would shift every level above it, and a NaN would be written to Excel as text.
/// </summary>
public static class KataColumnInputValidator
{
    public static void Validate(KataColumnStackInput input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        if (input.Storeys is null || input.Storeys.Count == 0)
        {
            throw new ArgumentException("The column stack has no storey.", nameof(input));
        }

        if (input.Storeys.Count > KataColumnSheet.MaxStoreys)
        {
            throw new ArgumentException(
                $"Sheet Cot holds {KataColumnSheet.MaxStoreys} storeys; the stack has {input.Storeys.Count}.", nameof(input));
        }

        for (int i = 0; i < input.Storeys.Count; i++)
        {
            var storey = input.Storeys[i];
            ValidateStorey(storey);
            if (i > 0)
            {
                RequireContiguous(input.Storeys[i - 1], storey);
            }
        }

        if (input.PileCap is { } cap)
        {
            RequireFinite(cap.BottomElevationMm, "pile cap bottom");
            RequirePositive(cap.HeightMm, "pile cap height");
        }

        if (input.CrossingBeamWidthMm is { } beamWidth)
        {
            RequirePositive(beamWidth, "crossing beam width");
        }

        if (input.SlabThicknessMm is { } slab)
        {
            RequirePositive(slab, "slab thickness");
        }
    }

    private static void ValidateStorey(KataColumnStorey storey)
    {
        if (string.IsNullOrWhiteSpace(storey.LevelName))
        {
            throw new ArgumentException("A storey has no level name.");
        }

        string name = storey.LevelName;
        RequireFinite(storey.BaseElevationMm, $"base of {name}");
        RequirePositive(storey.TopElevationMm - storey.BaseElevationMm, $"height of {name}");
        RequirePositive(storey.Section.WidthMm, $"b of {name}");
        RequirePositive(storey.Section.DepthMm, $"h of {name}");
        RequireFinite(storey.Section.MinXMm + storey.Section.MinYMm, $"section position of {name}");
        if (storey.BeamDepthMm < 0 || double.IsNaN(storey.BeamDepthMm) || double.IsInfinity(storey.BeamDepthMm))
        {
            throw new ArgumentException($"The beam depth at the head of {name} must be 0 or more (got {storey.BeamDepthMm}).");
        }
    }

    private static void RequireContiguous(KataColumnStorey lower, KataColumnStorey upper)
    {
        double gapMm = upper.BaseElevationMm - lower.TopElevationMm;
        if (Math.Abs(gapMm) > KataTolerance.StationMm)
        {
            throw new ArgumentException(
                $"{upper.LevelName} starts {KataFormat.Whole(gapMm)} mm from the top of {lower.LevelName}; " +
                "Kata needs storeys that meet.");
        }
    }

    private static void RequireFinite(double value, string what)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentException($"The {what} is not a number.");
        }
    }

    private static void RequirePositive(double value, string what)
    {
        RequireFinite(value, what);
        if (value <= 0)
        {
            throw new ArgumentException($"The {what} must be positive (got {value}).");
        }
    }
}
