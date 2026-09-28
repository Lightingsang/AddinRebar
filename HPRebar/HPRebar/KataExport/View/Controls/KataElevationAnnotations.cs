using System;
using System.Collections.Generic;
using System.Globalization;
using HPRebar.Core.KataExport.Models;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// Draws the annotations of the elevation: column letters (row 11), span captions (section, z offset, soffit step),
/// CAD 2-tier dimension chains (Detail chain with column face-to-grid offsets + clear spans, and Grid-to-grid chain),
/// upper column text, and grid offsets.
/// </summary>
internal sealed class KataElevationAnnotations
{
    private readonly KataElevationScene _scene;
    private readonly KataCanvasPalette _palette;
    private readonly KataDrawPrimitives _draw;

    public KataElevationAnnotations(KataElevationScene scene, KataCanvasPalette palette, KataDrawPrimitives draw)
    {
        _scene = scene;
        _palette = palette;
        _draw = draw;
    }

    public void Paint()
    {
        PaintUpperColumns();
        PaintGridOffsets();
        PaintCrossingOffsets();
        PaintLetters();
        PaintTopChain();
        PaintDetailChain();
        PaintGridChain();
        PaintCaptions();
    }

    /// <summary>
    /// Top Dim Chain: overall column widths (e.g. 300 / 350) and clear span lengths (e.g. 3850 / 3900),
    /// with witness lines extending from upper column stubs, 45-degree architectural ticks, and CAD green text.
    /// When supports are Foundation with Upper columns, it dimensions the upper column widths, clear spans between
    /// columns, and outer cantilever.
    /// </summary>
    private void PaintTopChain()
    {
        var columns = _scene.Elevation.Columns;
        if (columns.Count == 0) return;

        double y = _scene.TopChainY;
        double startStation = columns[0].Extent.Start;
        double endStation = columns[columns.Count - 1].Extent.End;

        bool hasFoundationWithUpper = _scene.Elevation.Supports.Any(s => s.Kind == KataSupportKind.Foundation && s.Upper != null);
        if (!hasFoundationWithUpper)
        {
            _draw.Line(_palette.Dimension, _scene.X(startStation), y, _scene.X(endStation), y);

            foreach (var column in columns)
            {
                if (column.IsZeroWidth) continue;

                double left = _scene.X(column.Extent.Start), right = _scene.X(column.Extent.End);
                if (!_scene.IsVisible(left - 5, right + 5)) continue;

                _draw.Tick(_palette.Dimension, left, y);
                _draw.Tick(_palette.Dimension, right, y);

                if (column.Kind == KataColumnKind.Support)
                {
                    var (topMm, _) = _scene.BeamFaces(column.Extent);
                    double stubTop = _scene.Y(topMm) - KataElevationScene.UpperStubPx;

                    // Witness lines from upper stub top up to TopChainY (overshooting by 3px)
                    _draw.Line(_palette.Dimension, left, stubTop - 2.0, left, y - 3.0);
                    _draw.Line(_palette.Dimension, right, stubTop - 2.0, right, y - 3.0);
                }

                if (column.Row11.Length == 0) continue;

                var text = _draw.Text(column.Row11, _palette.DimText, KataDrawPrimitives.SmallTextSize);
                if (text.Width + 4 <= right - left)
                {
                    _draw.Centered(text, (left + right) / 2.0, y - text.Height - 1.5);
                }
            }
            return;
        }

        // Foundation with Upper columns:
        // Top dim chain measures: outer cantilever, upper column widths, and clear spans between columns.
        var supports = _scene.Elevation.Supports.OrderBy(s => s.Extent.Start).ToList();
        var intervals = new List<(Interval1D Extent, bool IsStub, double? StubTop)>();
        double currentX = startStation;

        foreach (var s in supports)
        {
            if (s.Kind == KataSupportKind.Foundation && s.Upper is { } upper)
            {
                if (upper.Extent.Start > currentX + 0.5)
                {
                    intervals.Add((new Interval1D(currentX, upper.Extent.Start), false, null));
                }
                var (topMm, _) = _scene.BeamFaces(s.Extent);
                double stubTop = _scene.Y(topMm) - KataElevationScene.UpperStubPx;
                intervals.Add((upper.Extent, true, stubTop));
                currentX = upper.Extent.End;
            }
            else if (s.Kind != KataSupportKind.Foundation)
            {
                if (s.Extent.Start > currentX + 0.5)
                {
                    intervals.Add((new Interval1D(currentX, s.Extent.Start), false, null));
                }
                var (topMm, _) = _scene.BeamFaces(s.Extent);
                double stubTop = _scene.Y(topMm) - KataElevationScene.UpperStubPx;
                intervals.Add((s.Extent, true, stubTop));
                currentX = s.Extent.End;
            }
            else
            {
                if (s.Extent.Start > currentX + 0.5)
                {
                    intervals.Add((new Interval1D(currentX, s.Extent.Start), false, null));
                }
                intervals.Add((s.Extent, false, null));
                currentX = s.Extent.End;
            }
        }

        if (endStation > currentX + 0.5)
        {
            intervals.Add((new Interval1D(currentX, endStation), false, null));
        }

        _draw.Line(_palette.Dimension, _scene.X(startStation), y, _scene.X(endStation), y);

        for (int i = 0; i < intervals.Count; i++)
        {
            var intv = intervals[i];
            double left = _scene.X(intv.Extent.Start), right = _scene.X(intv.Extent.End);
            if (!_scene.IsVisible(left - 5, right + 5)) continue;

            _draw.Tick(_palette.Dimension, left, y);
            _draw.Tick(_palette.Dimension, right, y);

            if (i == 0)
            {
                var (topMm, _) = _scene.BeamFaces(intv.Extent);
                double beamTop = _scene.Y(topMm);
                _draw.Line(_palette.Dimension, left, beamTop - 2.0, left, y - 3.0);
            }

            if (intv.IsStub && intv.StubTop is { } stubTop)
            {
                _draw.Line(_palette.Dimension, left, stubTop - 2.0, left, y - 3.0);
                _draw.Line(_palette.Dimension, right, stubTop - 2.0, right, y - 3.0);
            }

            if (i == intervals.Count - 1)
            {
                var (topMm, _) = _scene.BeamFaces(intv.Extent);
                double beamTop = _scene.Y(topMm);
                _draw.Line(_palette.Dimension, right, beamTop - 2.0, right, y - 3.0);
            }

            double lengthMm = Math.Round(intv.Extent.Length);
            if (lengthMm > 0)
            {
                var text = _draw.Text(lengthMm.ToString(CultureInfo.InvariantCulture), _palette.DimText, KataDrawPrimitives.SmallTextSize);
                if (text.Width + 4 <= right - left)
                {
                    _draw.Centered(text, (left + right) / 2.0, y - text.Height - 1.5);
                }
            }
        }
    }

    private void PaintUpperColumns()
    {
        var lane = new KataLabelLane();
        foreach (var support in _scene.Elevation.Supports)
        {
            if (support.Kind == KataSupportKind.Foundation) continue;
            if (support.Upper is not { } upper || upper.Text.Length == 0) continue;

            var (topMm, _) = _scene.BeamFaces(support.Extent);
            double x = _scene.X(upper.Extent.Mid);
            var text = _draw.Text(upper.Text, _palette.Text, KataDrawPrimitives.SmallTextSize);
            PlaceCentered(lane, text, x, _scene.Y(topMm) - KataElevationScene.UpperStubPx - text.Height - 2);
        }
    }

    private void PaintGridOffsets()
    {
        var lane = new KataLabelLane();
        foreach (var grid in _scene.Elevation.Grids)
        {
            if (!IsNonZero(grid.OffsetText)) continue;

            var text = _draw.Text($"lệch {grid.OffsetText}", _palette.Accent, KataDrawPrimitives.SmallTextSize);
            double left = _scene.X(grid.X) + KataElevationScene.BubbleRadius + 4;
            double top = _scene.BubbleY - text.Height / 2.0;
            if (!_scene.IsVisible(left, left + text.Width) || !lane.TryPlace(left, left + text.Width)) continue;
            _draw.At(text, left, top);
        }
    }

    private void PaintCrossingOffsets()
    {
        var lane = new KataLabelLane();
        foreach (var support in _scene.Elevation.Supports)
        {
            if (support.CrossingBeamX is not { } crossing) continue;

            string row21 = _scene.Elevation.Columns[support.ColumnIndex].Row21;
            if (!IsNonZero(row21)) continue;

            var text = _draw.Text($"giao {row21}", _palette.MutedText, KataDrawPrimitives.SmallTextSize);
            PlaceCentered(lane, text, _scene.X(crossing), _scene.MarkerTextY);
        }
    }

    /// <summary>The selected column's letter is always shown; its neighbours give way to it.</summary>
    private void PaintLetters()
    {
        var columns = _scene.Elevation.Columns;
        double reservedLeft = double.NaN, reservedRight = double.NaN;
        if (_scene.SelectedColumn >= 0 && _scene.SelectedColumn < columns.Count)
        {
            var selected = columns[_scene.SelectedColumn];
            var text = _draw.Text(selected.Letter, _palette.Selected, KataDrawPrimitives.TextSize, bold: true);
            double x = _scene.X(selected.Extent.Mid);
            reservedLeft = x - text.Width / 2.0 - 4.0;
            reservedRight = x + text.Width / 2.0 + 4.0;
            _draw.Centered(text, x, _scene.LetterY - 1);
        }

        var lane = new KataLabelLane();
        foreach (var column in columns)
        {
            if (column.Index == _scene.SelectedColumn) continue;

            var text = _draw.Text(column.Letter, _palette.MutedText, KataDrawPrimitives.SmallTextSize);
            double x = _scene.X(column.Extent.Mid);
            if (x + text.Width / 2.0 > reservedLeft && x - text.Width / 2.0 < reservedRight) continue;
            PlaceCentered(lane, text, x, _scene.LetterY);
        }
    }

    /// <summary>
    /// Upper Dim Chain (Detail): column face-to-grid distances on each side of the grid line,
    /// and clear span lengths between columns, drawn with CAD green text and architectural ticks.
    /// </summary>
    private void PaintDetailChain()
    {
        var columns = _scene.Elevation.Columns;
        if (columns.Count == 0) return;

        double y = _scene.ChainY;
        _draw.Line(_palette.Dimension, _scene.X(columns[0].Extent.Start), y, _scene.X(columns[columns.Count - 1].Extent.End), y);

        foreach (var column in columns)
        {
            if (column.Kind == KataColumnKind.Span)
            {
                double left = _scene.X(column.Extent.Start), right = _scene.X(column.Extent.End);
                if (!_scene.IsVisible(left - 5, right + 5)) continue;

                _draw.Tick(_palette.Dimension, left, y);
                _draw.Tick(_palette.Dimension, right, y);
                if (column.Row11.Length == 0) continue;

                var text = _draw.Text(column.Row11, _palette.DimText, KataDrawPrimitives.SmallTextSize);
                if (text.Width + 4 <= right - left) _draw.Centered(text, (left + right) / 2.0, y - text.Height - 1.5);
            }
            else if (column.Kind == KataColumnKind.Support)
            {
                double left = _scene.X(column.Extent.Start), right = _scene.X(column.Extent.End);
                if (!_scene.IsVisible(left - 5, right + 5)) continue;

                var (_, bottomMm) = _scene.BeamFaces(column.Extent);
                var supp = _scene.Elevation.Supports.FirstOrDefault(s => s.ColumnIndex == column.Index);
                double stubBottom = supp?.Kind == KataSupportKind.Foundation
                    ? _scene.Y(bottomMm) + KataElevationScene.FootingPx
                    : _scene.Y(bottomMm) + KataElevationScene.LowerStubPx;

                // Witness lines from lower stub/footing bottom down to dimension line
                _draw.Line(_palette.Dimension, left, stubBottom + 2.0, left, y + 3.0);
                _draw.Line(_palette.Dimension, right, stubBottom + 2.0, right, y + 3.0);
                _draw.Tick(_palette.Dimension, left, y);
                _draw.Tick(_palette.Dimension, right, y);

                // Find if a grid crosses this support column
                var grid = _scene.Elevation.Grids.FirstOrDefault(g => column.Extent.Contains(g.X, 0.5));
                if (grid is not null)
                {
                    double gx = _scene.X(grid.X);
                    _draw.Tick(_palette.Dimension, gx, y);

                    // Sub-segment 1: left edge to grid
                    double dLeft = Math.Round(grid.X - column.Extent.Start);
                    if (dLeft > 0.5)
                    {
                        var textLeft = _draw.Text(dLeft.ToString(CultureInfo.InvariantCulture), _palette.DimText, KataDrawPrimitives.SmallTextSize);
                        if (textLeft.Width + 2.0 <= gx - left)
                            _draw.Centered(textLeft, (left + gx) / 2.0, y - textLeft.Height - 1.5);
                    }

                    // Sub-segment 2: grid to right edge
                    double dRight = Math.Round(column.Extent.End - grid.X);
                    if (dRight > 0.5)
                    {
                        var textRight = _draw.Text(dRight.ToString(CultureInfo.InvariantCulture), _palette.DimText, KataDrawPrimitives.SmallTextSize);
                        if (textRight.Width + 2.0 <= right - gx)
                            _draw.Centered(textRight, (gx + right) / 2.0, y - textRight.Height - 1.5);
                    }
                }
                else if (!column.IsZeroWidth && column.Row11.Length > 0)
                {
                    var text = _draw.Text(column.Row11, _palette.DimText, KataDrawPrimitives.SmallTextSize);
                    if (text.Width + 2.0 <= right - left)
                        _draw.Centered(text, (left + right) / 2.0, y - text.Height - 1.5);
                }
            }
        }
    }

    /// <summary>
    /// Lower Dim Chain (Grid-to-Grid): center-to-center distances between consecutive grid lines,
    /// drawn with CAD green text and architectural ticks.
    /// </summary>
    private void PaintGridChain()
    {
        var dimensions = _scene.Elevation.GridDimensions;
        if (dimensions.Count == 0) return;

        double y = _scene.GridChainY;
        _draw.Line(_palette.Dimension, _scene.X(dimensions[0].Extent.Start), y, _scene.X(dimensions[dimensions.Count - 1].Extent.End), y);
        foreach (var dimension in dimensions)
        {
            double left = _scene.X(dimension.Extent.Start), right = _scene.X(dimension.Extent.End);
            if (!_scene.IsVisible(left - 5, right + 5)) continue;

            _draw.Tick(_palette.Dimension, left, y);
            _draw.Tick(_palette.Dimension, right, y);
            var text = _draw.Text(dimension.Text, _palette.DimText, KataDrawPrimitives.TextSize);
            if (text.Width + 4 <= right - left) _draw.Centered(text, (left + right) / 2.0, y - text.Height - 1.5);
        }
    }

    /// <summary>"Nhịp n – b x h" above each span, then its z offset and soffit step when not zero.</summary>
    private void PaintCaptions()
    {
        var titles = new KataLabelLane();
        var details = new KataLabelLane();
        foreach (var column in _scene.Elevation.Columns)
        {
            if (column.Kind != KataColumnKind.Span) continue;

            double left = _scene.X(column.Extent.Start), right = _scene.X(column.Extent.End), mid = (left + right) / 2.0;
            if (!_scene.IsVisible(left, right)) continue;

            var (topMm, _) = _scene.BeamFaces(column.Extent);
            double top = _scene.Y(topMm);
            double captionY = top - 15.0;

            var title = _draw.Text($"Nhịp {column.SpanNumber} – {column.SpanSection}", _palette.Text, KataDrawPrimitives.SmallTextSize, bold: true);
            if (title.Width + 4 > right - left) title = _draw.Text($"{column.SpanNumber}", _palette.Text, KataDrawPrimitives.SmallTextSize, bold: true);
            PlaceCentered(titles, title, mid, captionY);

            var parts = new List<string>(2);
            if (IsNonZero(column.Row19)) parts.Add($"z {column.Row19}");
            if (IsNonZero(column.Row21)) parts.Add($"đáy {column.Row21}");
            if (parts.Count == 0) continue;

            var detail = _draw.Text(string.Join(" · ", parts), _palette.MutedText, KataDrawPrimitives.SmallTextSize);
            if (detail.Width + 4 <= right - left) PlaceCentered(details, detail, mid, captionY - title.Height - 1);
        }
    }

    private void PlaceCentered(KataLabelLane lane, System.Windows.Media.FormattedText text, double x, double top)
    {
        double left = x - text.Width / 2.0, right = x + text.Width / 2.0;
        if (!_scene.IsVisible(left, right) || !lane.TryPlace(left, right)) return;
        _draw.Centered(text, x, top);
    }

    private static bool IsNonZero(string? cell) => !string.IsNullOrEmpty(cell) && cell != "0";
}
