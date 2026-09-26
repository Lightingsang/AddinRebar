using System;
using HPRebar.Core.KataExport.Models;

namespace HPRebar.Core.KataExport.Calculators;

/// <summary>
/// Rejects inputs that would silently produce a wrong sheet: a NaN station makes every overlap test fail,
/// so real supports would vanish and the NaN itself would be written to Excel.
/// </summary>
public static class KataInputValidator
{
    public static void Validate(KataRunInput input)
    {
        if (input is null) throw new ArgumentNullException(nameof(input));
        if (input.Pieces is null) throw new ArgumentNullException(nameof(input), "Pieces is null.");
        if (input.Supports is null) throw new ArgumentNullException(nameof(input), "Supports is null.");
        if (input.Grids is null) throw new ArgumentNullException(nameof(input), "Grids is null.");
        if (input.Header is null) throw new ArgumentNullException(nameof(input), "Header is null.");
        if (input.Pieces.Count == 0) throw new ArgumentException("The beam run has no framing element.", nameof(input));

        foreach (var piece in input.Pieces)
        {
            RequireInterval(piece.Extent, $"beam {piece.ElementKey}");
            if (piece.Extent.Length < KataTolerance.StationMm)
                throw new ArgumentException($"Beam {piece.ElementKey} has zero length along the run.", nameof(input));
            RequirePositive(piece.WidthMm, $"width of beam {piece.ElementKey}");
            RequirePositive(piece.HeightMm, $"height of beam {piece.ElementKey}");
            RequireFinite(piece.ZOffsetMm, $"z offset of beam {piece.ElementKey}");
        }

        foreach (var support in input.Supports)
        {
            RequireInterval(support.Extent, $"support {support.ElementKey}");
            if (support.Upper is { } upper) RequireInterval(upper, $"column above support {support.ElementKey}");
        }

        foreach (var grid in input.Grids)
        {
            if (grid.Name is null) throw new ArgumentException("A grid has no name.", nameof(input));
            RequireFinite(grid.StationMm, $"station of grid {grid.Name}");
        }

        RequirePositive(input.Header.HeightMm, "header height");
        RequirePositive(input.Header.WidthMm, "header width");
        RequireFinite(input.Header.LevelElevationMm, "level elevation");
        if (input.Header.SlabThicknessMm is { } slab)
        {
            RequireFinite(slab, "slab thickness");
            if (slab < 0) throw new ArgumentException($"The slab thickness must not be negative (got {slab}).");
        }

        if (input.Header.AxisOffsetMm is { } axisOffset) RequireFinite(axisOffset, "axis offset");
        foreach (var support in input.Supports)
        {
            if (support.CrossingBeamStationMm is { } crossing)
                RequireFinite(crossing, $"crossing beam of support {support.ElementKey}");
        }
    }

    private static void RequireInterval(Interval1D interval, string what)
    {
        RequireFinite(interval.Start, what);
        RequireFinite(interval.End, what);
    }

    private static void RequirePositive(double value, string what)
    {
        RequireFinite(value, what);
        if (value <= 0) throw new ArgumentException($"The {what} must be greater than 0 (got {value}).");
    }

    private static void RequireFinite(double value, string what)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new ArgumentException($"The {what} is not a finite number.");
    }
}
