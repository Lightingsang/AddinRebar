using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using HPRebar.Core.KataExport.Models;

namespace HPRebar.Core.KataExport.Calculators;

/// <summary>
/// Lays a beam run out as an elevation whose columns are the columns of the sheet about to be written: the
/// same segmentation, the same order (a reversed run is mirrored so column C stays on the left) and, for every
/// number shown, the text of the cell itself — the drawing cannot disagree with what goes to Excel.
/// </summary>
public static class KataElevationBuilder
{
    /// <summary>Grids this far beyond the run are still drawn; farther ones cross the beam line elsewhere.</summary>
    private const double GridReachMm = 1000.0;

    private static readonly Regex SectionPattern = new(@"^\s*(\d+(?:\.\d+)?)\s*x\s*(\d+(?:\.\d+)?)\s*$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static KataElevation Build(KataRunInput input, KataBuildOptions options, KataSheet sheet)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));
        if (sheet is null) throw new ArgumentNullException(nameof(sheet));

        var segmentation = KataSegmenter.Segment(input, options);
        var natural = NaturalColumns(segmentation);
        if (natural.Count != sheet.ColumnCount)
            throw new InvalidOperationException($"The elevation has {natural.Count} columns but the sheet has {sheet.ColumnCount}.");

        // Everything drawn: beams, supports (wider than the beam at the ends) and the columns standing on them.
        var run = new Interval1D(input.Pieces.Min(p => p.Extent.Start), input.Pieces.Max(p => p.Extent.End));
        var drawn = natural.Aggregate(run, (range, c) => range.Union(c.Extent));
        drawn = natural
            .Select(c => c.Segment?.Support?.Upper)
            .Aggregate(drawn, (range, upper) => upper is { } u ? range.Union(u) : range);

        var reach = new Interval1D(drawn.Start - GridReachMm, drawn.End + GridReachMm);
        var supportExtents = natural
            .Where(c => c.Kind == KataColumnKind.Support)
            .Select(c => c.Extent)
            .ToList();
        var row22Grids = new HashSet<string>(
            sheet.Row22.OfType<string>().Where(name => !string.IsNullOrEmpty(name)));
        var grids = input.Grids
            .Where(g => reach.Contains(g.StationMm, 0.0)
                        && row22Grids.Contains(g.Name)
                        && supportExtents.Any(s => s.Contains(g.StationMm, 0.0)))
            .ToList();
        drawn = grids.Aggregate(drawn, (range, g) => range.Union(new Interval1D(g.StationMm, g.StationMm)));
        var map = new StationMap(drawn, options.Reverse);

        if (options.Reverse) natural.Reverse();

        var columns = new List<KataElevationColumn>(natural.Count);
        var supports = new List<KataElevationSupport>();
        int spanNumber = 0;
        for (int i = 0; i < natural.Count; i++)
        {
            var (kind, extent, segment) = natural[i];
            var piece = segment?.Piece;
            CheckRow11(i, kind, extent, segment, sheet);
            columns.Add(new KataElevationColumn(
                i,
                KataColumnLetters.Letter(i),
                kind,
                map.Map(extent),
                Cell(sheet.Row11, i),
                Cell(sheet.Row19, i),
                Cell(sheet.Row21, i),
                Cell(sheet.Row22, i),
                Cell(sheet.Row23, i),
                kind == KataColumnKind.Span ? ++spanNumber : null,
                piece is null ? null : KataFormat.Section(piece.WidthMm, piece.HeightMm)));

            if (segment?.Support is { } support)
                supports.Add(Support(i, support, segment.Extent, Cell(sheet.Row19, i), map));
        }

        var beams = input.Pieces
            .Select(p => new KataElevationBeam(map.Map(p.Extent), p.ZOffsetMm, p.ZOffsetMm - p.HeightMm, p.HeightMm))
            .OrderBy(b => b.Extent.Start)
            .ToList();

        var elevationGrids = grids
            .Select(g => new KataElevationGrid(g.Name, map.Map(g.StationMm), GridOffset(g, columns)))
            .OrderBy(g => g.X)
            .ToList();

        return new KataElevation(columns, beams, supports, elevationGrids, GridDimensions(elevationGrids), map.Bounds);
    }

    /// <summary>Columns in axis order, with the zero-width free-end columns the row builder adds.</summary>
    private static List<(KataColumnKind Kind, Interval1D Extent, KataSegment? Segment)> NaturalColumns(KataSegmentation segmentation)
    {
        var segments = segmentation.Segments;
        var columns = new List<(KataColumnKind, Interval1D, KataSegment?)>(segments.Count + 2);

        if (!segmentation.StartsWithSupport)
            columns.Add((KataColumnKind.FreeEnd, new Interval1D(segments[0].Extent.Start, segments[0].Extent.Start), null));

        foreach (var segment in segments)
        {
            var kind = segment.Kind switch
            {
                KataSegmentKind.Support => KataColumnKind.Support,
                KataSegmentKind.Joint => KataColumnKind.Joint,
                _ => KataColumnKind.Span
            };
            columns.Add((kind, segment.Extent, segment));
        }

        if (!segmentation.EndsWithSupport)
        {
            double end = segments[segments.Count - 1].Extent.End;
            columns.Add((KataColumnKind.FreeEnd, new Interval1D(end, end), null));
        }

        return columns;
    }

    /// <summary>
    /// Row 11 is recomputed from the geometry of each column and must equal the sheet's cell: a sheet built for the
    /// other direction or another run has the same column count but different lengths, and would label the drawing wrong.
    /// </summary>
    private static void CheckRow11(int index, KataColumnKind kind, Interval1D extent, KataSegment? segment, KataSheet sheet)
    {
        object expected = kind switch
        {
            KataColumnKind.Joint or KataColumnKind.FreeEnd => 0.0,
            KataColumnKind.Support when segment?.Support is { Kind: KataSupportKind.Beam, SectionText: { } section } => section,
            _ => KataFormat.Round(extent.Length)
        };

        string cell = Cell(sheet.Row11, index);
        if (cell != KataColumnLetters.CellText(expected))
            throw new InvalidOperationException(
                $"Column {KataColumnLetters.Letter(index)} of the sheet holds {cell} in row 11 but the run measures {KataColumnLetters.CellText(expected)}; the sheet was built for another run or direction.");
    }

    private static KataElevationSupport Support(int index, KataSupport support, Interval1D extent, string row19, StationMap map)
    {
        double? sectionHeight = support.Kind == KataSupportKind.Beam && support.SectionText is { } text && SectionPattern.Match(text) is { Success: true } match
            ? double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture)
            : null;

        var upper = support.Upper is { } column ? new KataElevationUpper(map.Map(column), row19) : null;
        double? crossing = support.CrossingBeamStationMm is { } station ? map.Map(station) : null;
        return new KataElevationSupport(index, support.Kind, map.Map(extent), sectionHeight, upper, crossing);
    }

    /// <summary>Row 23 belongs to the grid written in row 22 of a support that the grid actually crosses.</summary>
    private static string? GridOffset(KataGridCrossing grid, IReadOnlyList<KataElevationColumn> columns) =>
        columns.FirstOrDefault(c => c.Kind == KataColumnKind.Support && c.Row22 == grid.Name)?.Row23;

    private static IReadOnlyList<KataDimension> GridDimensions(IReadOnlyList<KataElevationGrid> grids)
    {
        var dimensions = new List<KataDimension>();
        for (int i = 1; i < grids.Count; i++)
        {
            double from = grids[i - 1].X, to = grids[i].X;
            if (to - from < KataTolerance.StationMm) continue;
            dimensions.Add(new KataDimension(new Interval1D(from, to), KataColumnLetters.CellText(KataFormat.Round(to - from))));
        }

        return dimensions;
    }

    private static string Cell(IReadOnlyList<object?> row, int index) => KataColumnLetters.CellText(row[index]);

    /// <summary>Axis stations → drawing stations: 0 at the left end, mirrored for a reversed run.</summary>
    private sealed class StationMap
    {
        private readonly Interval1D _natural;
        private readonly bool _reverse;

        public StationMap(Interval1D natural, bool reverse)
        {
            _natural = natural;
            _reverse = reverse;
        }

        public Interval1D Bounds => new(0.0, _natural.Length);

        public double Map(double station) => _reverse ? _natural.End - station : station - _natural.Start;

        public Interval1D Map(Interval1D range) => new(Map(range.Start), Map(range.End));
    }
}
