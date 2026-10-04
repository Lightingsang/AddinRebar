using System;
using System.Collections.Generic;
using System.Linq;

namespace HPRebar.Core.KataExport.Models;

/// <summary>What a Kata column stands for in the elevation.</summary>
public enum KataColumnKind
{
    Support,
    Span,

    /// <summary>Zero-width support where two framing elements meet without anything under them.</summary>
    Joint,

    /// <summary>Zero-width column Kata expects at a beam end that does not sit on a support.</summary>
    FreeEnd
}

/// <summary>
/// One column of the Kata sheet as drawn: where it lies along the elevation and the cells written for it,
/// formatted exactly as the preview table shows them.
/// </summary>
/// <param name="Index">Zero-based column index, 0 = Excel column C.</param>
/// <param name="Extent">Drawing stations (mm from the left end of the elevation); zero length for joints and free ends.</param>
/// <param name="SpanNumber">1-based span number from the left, for spans.</param>
/// <param name="SpanSection">"b x h" of the framing element under a span.</param>
/// <param name="Row20">Row 20 as built: the crossing beam at a support, the span width when it differs from B6.</param>
public sealed record KataElevationColumn(
    int Index,
    string Letter,
    KataColumnKind Kind,
    Interval1D Extent,
    string Row11,
    string Row19,
    string Row21,
    string Row22,
    string Row23,
    int? SpanNumber = null,
    string? SpanSection = null,
    string Row20 = "")
{
    public bool IsZeroWidth => Kind is KataColumnKind.Joint or KataColumnKind.FreeEnd;
}

/// <summary>One framing element of the run, heights relative to its reference level (top = its z offset).</summary>
public sealed record KataElevationBeam(Interval1D Extent, double TopMm, double BottomMm, double HeightMm);

/// <summary>Column standing on a support: its extent along the elevation and the row-19 text "width;offset".</summary>
public sealed record KataElevationUpper(Interval1D Extent, string Text);

/// <summary>A support column with what the painter needs beyond the cells.</summary>
/// <param name="SectionHeightMm">Height of a supporting beam, parsed from its "b x h".</param>
/// <param name="CrossingBeamX">Centre line of a beam framing into a column or footing across the run.</param>
public sealed record KataElevationSupport(
    int ColumnIndex,
    KataSupportKind Kind,
    Interval1D Extent,
    double? SectionHeightMm = null,
    KataElevationUpper? Upper = null,
    double? CrossingBeamX = null);

/// <summary>A grid line crossing the elevation, with its row-23 offset when it is the grid written for a support.</summary>
public sealed record KataElevationGrid(string Name, double X, string? OffsetText = null);

/// <summary>One piece of a dimension chain.</summary>
public sealed record KataDimension(Interval1D Extent, string Text);

/// <summary>
/// The beam run drawn as an elevation, left to right in the order of the sheet columns (a reversed run is mirrored),
/// in millimetres from the left end of <see cref="Bounds"/>. Heights are relative to the reference level.
/// </summary>
public sealed record KataElevation(
    IReadOnlyList<KataElevationColumn> Columns,
    IReadOnlyList<KataElevationBeam> Beams,
    IReadOnlyList<KataElevationSupport> Supports,
    IReadOnlyList<KataElevationGrid> Grids,
    IReadOnlyList<KataDimension> GridDimensions,
    Interval1D Bounds)
{
    /// <summary>Highest beam top, relative to the reference level.</summary>
    public double TopMm { get; } = Beams.Max(b => b.TopMm);

    /// <summary>Lowest face drawn in the beam band: a beam soffit or the soffit of a deeper crossing girder.</summary>
    public double BottomMm { get; } = LowestFace(Beams, Supports);

    public double MaxBeamHeightMm { get; } = Beams.Max(b => b.HeightMm);

    /// <summary>Top of the beams over <paramref name="extent"/>; the highest top when no beam reaches it.</summary>
    public double TopOver(Interval1D extent) => TopOver(Beams, extent);

    /// <summary>
    /// The column under a station: a zero-width column within <paramref name="zeroWidthReachMm"/> wins (it has no
    /// width to click), then the column whose extent holds the station; null outside every column.
    /// </summary>
    public int? ColumnAt(double x, double zeroWidthReachMm)
    {
        var nearZero = Columns
            .Where(c => c.Extent.Length <= KataTolerance.StationMm && Math.Abs(c.Extent.Mid - x) <= zeroWidthReachMm)
            .OrderBy(c => Math.Abs(c.Extent.Mid - x))
            .FirstOrDefault();
        if (nearZero is not null) return nearZero.Index;

        return Columns.FirstOrDefault(c => c.Extent.Length > KataTolerance.StationMm && c.Extent.Contains(x, 0.0))?.Index;
    }

    /// <summary>
    /// The span after (<paramref name="direction"/> &gt; 0) or before the column <paramref name="current"/>; with no
    /// current column, the first or last span. Null when there is no span that way.
    /// </summary>
    public int? SpanStep(int? current, int direction)
    {
        var spans = Columns.Where(c => c.Kind == KataColumnKind.Span).Select(c => c.Index);
        if (current is not { } from)
            return direction > 0 ? spans.Cast<int?>().FirstOrDefault() : spans.Cast<int?>().LastOrDefault();

        return direction > 0
            ? spans.Where(i => i > from).Cast<int?>().FirstOrDefault()
            : spans.Where(i => i < from).Cast<int?>().LastOrDefault();
    }

    private static double TopOver(IReadOnlyList<KataElevationBeam> beams, Interval1D extent) =>
        beams.Where(b => b.Extent.Overlaps(extent)).Select(b => b.TopMm).DefaultIfEmpty(beams.Max(b => b.TopMm)).Max();

    /// <summary>A crossing girder is drawn top-flush with the run, so a deeper one reaches below the beams.</summary>
    private static double LowestFace(IReadOnlyList<KataElevationBeam> beams, IReadOnlyList<KataElevationSupport> supports) =>
        supports
            .Where(s => s.SectionHeightMm.HasValue)
            .Select(s => TopOver(beams, s.Extent) - s.SectionHeightMm!.Value)
            .Concat(beams.Select(b => b.BottomMm))
            .Min();

    /// <summary>A span with the supports on either side, for zooming onto it.</summary>
    public Interval1D FocusRange(int columnIndex)
    {
        int first = Math.Max(0, columnIndex - 1);
        int last = Math.Min(Columns.Count - 1, columnIndex + 1);
        var range = Columns[first].Extent;
        for (int i = first + 1; i <= last; i++) range = range.Union(Columns[i].Extent);
        return range;
    }
}
