using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataExport.Models;

namespace HPRebar.Core.KataExport.Calculators;

/// <summary>
/// Turns a beam run into the cell values of sheet "Dam": B3..B10 and rows 11, 19, 21, 22, 23 from column C,
/// one column per segment. Every span takes its values from the framing element under its midpoint and
/// every support from itself, so the columns never drift when elements and spans do not pair one to one.
/// </summary>
public static class KataRowBuilder
{
    private const string Empty = "";

    public static KataSheet Build(KataRunInput input, KataBuildOptions? options = null)
    {
        options ??= new KataBuildOptions();
        var segmentation = KataSegmenter.Segment(input, options);
        var warnings = new List<string>(segmentation.Warnings);

        double sign = options.Reverse ? -1.0 : 1.0;
        // Soffit steps are measured from the first element of the run, the same element the header describes.
        double firstHeight = input.Header.HeightMm;
        var rows = new Rows(segmentation.Segments.Count + 2);

        if (!segmentation.StartsWithSupport) rows.AddCantileverEnd();

        foreach (var segment in segmentation.Segments)
        {
            switch (segment.Kind)
            {
                case KataSegmentKind.Support:
                    AddSupport(rows, segment, input.Grids, sign, warnings);
                    break;
                case KataSegmentKind.Joint:
                    rows.Add(0.0, 0.0, Empty, Empty, Empty);
                    break;
                default:
                    var piece = segment.Piece!;
                    rows.Add(
                        KataFormat.Round(segment.Extent.Length),
                        KataFormat.Round(piece.ZOffsetMm),
                        KataFormat.Round(-(piece.HeightMm - firstHeight) + piece.ZOffsetMm),
                        Empty,
                        Empty);
                    break;
            }
        }

        if (!segmentation.EndsWithSupport) rows.AddCantileverEnd();

        if (rows.R11.Count > KataSheet.MaxColumns)
            throw new InvalidOperationException(
                $"The beam run needs {rows.R11.Count} columns; the Kata sheet holds {KataSheet.MaxColumns} (C..BZ).");

        if (options.Reverse) rows.Reverse();

        var header = Header(input.Header, sign, warnings);
        return new KataSheet(header, rows.R11, rows.R19, rows.R21, rows.R22, rows.R23, warnings);
    }

    private static void AddSupport(Rows rows, KataSegment segment, IReadOnlyList<KataGridCrossing> grids, double sign, List<string> warnings)
    {
        var support = segment.Support!;
        var extent = segment.Extent;

        object width = support.Kind == KataSupportKind.Beam && support.SectionText is { } section
            ? section
            : KataFormat.Round(extent.Length);

        object upper = support.Upper is { } column
            ? KataFormat.UpperColumn(column.Length, (column.Mid - extent.Mid) * sign)
            : 0.0;

        var center = support.Upper is { } upperCol ? upperCol.Mid : extent.Mid;
        var onSupport = grids
            .Where(g => extent.Contains(g.StationMm))
            .OrderBy(g => Math.Abs(g.StationMm - center))
            .ToList();
        var grid = onSupport.FirstOrDefault();

        var onColumn = support.Upper is { } col
            ? onSupport.Where(g => col.Contains(g.StationMm)).ToList()
            : onSupport;
        if (onColumn.Count > 1)
            warnings.Add($"Grids {string.Join(", ", onColumn.Select(g => g.Name))} cross one support; {grid!.Name} (nearest the centre) is written.");

        object gridOffset = grid is null ? Empty : KataFormat.Round((grid.StationMm - extent.Mid) * sign);

        // Kata row 21 at a column is the offset of the crossing beam from the column centre; the grid offset
        // stands in when no crossing beam was found, which is right whenever the crossing beam sits on the grid.
        object crossingOffset = support.CrossingBeamStationMm is { } crossing
            ? KataFormat.Round((crossing - extent.Mid) * sign)
            : gridOffset;

        rows.Add(width, upper, crossingOffset, grid?.Name ?? Empty, gridOffset);
    }

    private static IReadOnlyList<object?> Header(KataHeader header, double sign, List<string> warnings)
    {
        if (header.SlabThicknessMm is null)
            warnings.Add("No floor found on top of the beam; B7 (slab thickness) is 0.");
        if (header.AxisGridName is null)
            warnings.Add("No grid runs along the beam; B8 (axis name) is empty and B9 (axis offset) is -b/2.");

        return new object?[]
        {
            header.Name,
            header.Count,
            KataFormat.Clean(header.HeightMm),
            KataFormat.Clean(header.WidthMm),
            KataFormat.Clean(header.SlabThicknessMm ?? 0.0),
            Empty, // B8: Kata Pro leaves beam axis grid name empty
            // Left of the run becomes right when the run is written from its far end.
            header.AxisOffsetMm is { } axisOffset ? KataFormat.Clean(axisOffset * sign) : KataFormat.Clean(-header.WidthMm / 2.0),
            new KataText(KataFormat.Elevation(header.LevelElevationMm))
        };
    }

    /// <summary>The five data rows, filled column by column.</summary>
    private sealed class Rows
    {
        public Rows(int capacity)
        {
            R11 = new List<object?>(capacity);
            R19 = new List<object?>(capacity);
            R21 = new List<object?>(capacity);
            R22 = new List<object?>(capacity);
            R23 = new List<object?>(capacity);
        }

        public List<object?> R11 { get; }
        public List<object?> R19 { get; }
        public List<object?> R21 { get; }
        public List<object?> R22 { get; }
        public List<object?> R23 { get; }

        public void Add(object r11, object r19, object r21, object r22, object r23)
        {
            R11.Add(r11);
            R19.Add(r19);
            R21.Add(r21);
            R22.Add(r22);
            R23.Add(r23);
        }

        /// <summary>A beam end that does not sit on a support gets a zero-width support column.</summary>
        public void AddCantileverEnd() => Add(0.0, 0.0, Empty, Empty, Empty);

        public void Reverse()
        {
            R11.Reverse();
            R19.Reverse();
            R21.Reverse();
            R22.Reverse();
            R23.Reverse();
        }
    }
}
