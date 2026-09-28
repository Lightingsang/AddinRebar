using HPRebar.Core.KataExport.Models;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// Draws the geometry of the elevation: selection band, grid lines and bubbles, supports by kind (column stubs,
/// footing blocks, crossing-beam sections), the beams with their steps, joints and free ends.
/// Texts that describe the geometry are drawn by <see cref="KataElevationAnnotations"/>.
/// </summary>
internal sealed class KataElevationPainter
{
    private readonly KataElevationScene _scene;
    private readonly KataCanvasPalette _palette;
    private readonly KataDrawPrimitives _draw;

    public KataElevationPainter(KataElevationScene scene, KataCanvasPalette palette, KataDrawPrimitives draw)
    {
        _scene = scene;
        _palette = palette;
        _draw = draw;
    }

    public void Paint()
    {
        _draw.Box(_palette.Fill, null, 0, 0, _scene.Width, _scene.Height);
        PaintSelection();
        PaintGridLines();
        PaintMonolithicFrame();
        foreach (var column in _scene.Elevation.Columns) PaintZeroWidth(column);
        foreach (var support in _scene.Elevation.Supports) PaintCrossingBeam(support);
        PaintBubbles();
    }

    private void PaintSelection()
    {
        var columns = _scene.Elevation.Columns;
        if (_scene.SelectedColumn < 0 || _scene.SelectedColumn >= columns.Count) return;

        var (left, right) = _scene.ScreenSpan(columns[_scene.SelectedColumn].Extent, 10.0);
        _draw.Box(_palette.SelectionFill, null, left, _scene.SelectionTopY, right, _scene.GridLineBottomY);
    }

    private void PaintGridLines()
    {
        double top = _scene.GridLineTopY;
        double bottom = _scene.GridLineBottomY;
        foreach (var grid in _scene.Elevation.Grids)
        {
            double x = _scene.X(grid.X);
            if (x < -20 || x > _scene.Width + 20) continue;
            _draw.Line(_palette.GridLine, x, top, x, bottom);
        }
    }

    private void PaintMonolithicFrame()
    {
        // 1. Spans (horizontal beam top and bottom lines)
        foreach (var column in _scene.Elevation.Columns)
        {
            if (column.Kind != KataColumnKind.Span) continue;
            double left = _scene.X(column.Extent.Start), right = _scene.X(column.Extent.End);
            if (!_scene.IsVisible(left, right)) continue;

            var (topMm, bottomMm) = _scene.BeamFaces(column.Extent);
            double top = _scene.Y(topMm), bottom = _scene.Y(bottomMm);

            _draw.Box(_palette.BeamFill, null, left, top, right, bottom);
            _draw.Line(_palette.Outline, left, top, right, top);
            _draw.Line(_palette.Outline, left, bottom, right, bottom);
        }

        // 2. Supports (column stubs with breaklines or foundations/crossing beams)
        foreach (var support in _scene.Elevation.Supports)
        {
            double left = _scene.X(support.Extent.Start), right = _scene.X(support.Extent.End);
            if (!_scene.IsVisible(left - 20, right + 20)) continue;
            if (right - left < 2.0)
            {
                double mid = (left + right) / 2.0;
                left = mid - 1.0;
                right = mid + 1.0;
            }

            // Find adjacent spans on left and right to get precise beam face elevations at each side
            var columns = _scene.Elevation.Columns;
            var spanLeft = columns.Take(support.ColumnIndex).LastOrDefault(c => c.Kind == KataColumnKind.Span && System.Math.Abs(c.Extent.End - support.Extent.Start) < 10.0);
            var spanRight = columns.Skip(support.ColumnIndex + 1).FirstOrDefault(c => c.Kind == KataColumnKind.Span && System.Math.Abs(c.Extent.Start - support.Extent.End) < 10.0);

            var facesLeft = spanLeft != null ? _scene.BeamFaces(spanLeft.Extent) : _scene.BeamFaces(support.Extent);
            var facesRight = spanRight != null ? _scene.BeamFaces(spanRight.Extent) : _scene.BeamFaces(support.Extent);

            double topMm = System.Math.Max(
                spanLeft != null ? facesLeft.Top : double.NegativeInfinity,
                spanRight != null ? facesRight.Top : double.NegativeInfinity);
            if (double.IsNegativeInfinity(topMm)) topMm = _scene.BeamFaces(support.Extent).Top;

            double bottomMm = System.Math.Min(
                spanLeft != null ? facesLeft.Bottom : double.PositiveInfinity,
                spanRight != null ? facesRight.Bottom : double.PositiveInfinity);
            if (double.IsPositiveInfinity(bottomMm)) bottomMm = _scene.BeamFaces(support.Extent).Bottom;

            double top = _scene.Y(topMm), bottom = _scene.Y(bottomMm);

            switch (support.Kind)
            {
                case KataSupportKind.Foundation:
                {
                    double footingBottom = bottom + KataElevationScene.FootingPx;
                    double gridX = _scene.Elevation.Grids.FirstOrDefault(g => support.Extent.Contains(g.X, 10.0)) is { } gr
                        ? _scene.X(gr.X)
                        : (left + right) / 2.0;

                    // Footing body fill and beam body fill inside support extent
                    _draw.Box(_palette.SupportFill, null, left, bottom, right, footingBottom);
                    _draw.Box(_palette.BeamFill, null, left, top, right, bottom);

                    // Footing bottom breakline with zigzag cut centered at grid axis
                    _draw.BreakLineHorizontal(_palette.BreakLine, left, right, footingBottom, overhang: 0.0, centerOverride: gridX);

                    // Vertical edges at footing boundaries
                    if (spanLeft != null)
                    {
                        double bottomLeft = _scene.Y(facesLeft.Bottom);
                        _draw.Line(_palette.Outline, left, bottomLeft, left, footingBottom);
                    }
                    else
                    {
                        // Outer cantilever face: continuous edge from beam top down to footing bottom
                        _draw.Line(_palette.Outline, left, top, left, footingBottom);
                    }

                    if (spanRight != null)
                    {
                        double bottomRight = _scene.Y(facesRight.Bottom);
                        _draw.Line(_palette.Outline, right, footingBottom, right, bottomRight);
                    }
                    else
                    {
                        // Outer cantilever face: continuous edge from beam top down to footing bottom
                        _draw.Line(_palette.Outline, right, top, right, footingBottom);
                    }

                    // Beam top line and upper column stub
                    if (support.Upper is { } upper)
                    {
                        double upperLeft = _scene.X(upper.Extent.Start);
                        double upperRight = _scene.X(upper.Extent.End);
                        double upperTop = top - KataElevationScene.UpperStubPx;

                        // Upper column stub fill & outline
                        _draw.Box(_palette.SupportFill, null, upperLeft, upperTop, upperRight, top);
                        _draw.BreakLineHorizontal(_palette.BreakLine, upperLeft, upperRight, upperTop, overhang: 4.5, centerOverride: gridX);
                        _draw.Line(_palette.Outline, upperLeft, top, upperLeft, upperTop);
                        _draw.Line(_palette.Outline, upperRight, top, upperRight, upperTop);

                        // Horizontal beam top line outside column stub
                        if (upperLeft > left)
                            _draw.Line(_palette.Outline, left, top, upperLeft, top);
                        if (upperRight < right)
                            _draw.Line(_palette.Outline, upperRight, top, right, top);
                    }
                    else
                    {
                        _draw.Line(_palette.Outline, left, top, right, top);
                    }

                    break;
                }

                case KataSupportKind.Beam:
                {
                    double sectionBottom = support.SectionHeightMm is { } height ? _scene.Y(topMm - height) : bottom;
                    _draw.Box(_palette.SupportFill, _palette.Outline, left, top, right, sectionBottom);
                    _draw.Line(_palette.Outline, left, top, right, sectionBottom);
                    _draw.Line(_palette.Outline, left, sectionBottom, right, top);
                    break;
                }

                default:
                {
                    double upperTop = top - KataElevationScene.UpperStubPx;
                    double lowerBottom = bottom + KataElevationScene.LowerStubPx;

                    _draw.Box(_palette.SupportFill, null, left, upperTop, right, lowerBottom);

                    // Upper breakline & Lower breakline
                    _draw.BreakLineHorizontal(_palette.BreakLine, left, right, upperTop);
                    _draw.BreakLineHorizontal(_palette.BreakLine, left, right, lowerBottom);

                    // Left vertical edge: starts from the exact beam face of spanLeft down/up to breaklines
                    if (spanLeft is null)
                    {
                        _draw.Line(_palette.Outline, left, upperTop, left, lowerBottom);
                    }
                    else
                    {
                        double topLeft = _scene.Y(facesLeft.Top);
                        double bottomLeft = _scene.Y(facesLeft.Bottom);
                        _draw.Line(_palette.Outline, left, topLeft, left, upperTop);
                        _draw.Line(_palette.Outline, left, bottomLeft, left, lowerBottom);
                    }

                    // Right vertical edge: starts from the exact beam face of spanRight down/up to breaklines
                    if (spanRight is null)
                    {
                        _draw.Line(_palette.Outline, right, upperTop, right, lowerBottom);
                    }
                    else
                    {
                        double topRight = _scene.Y(facesRight.Top);
                        double bottomRight = _scene.Y(facesRight.Bottom);
                        _draw.Line(_palette.Outline, right, topRight, right, upperTop);
                        _draw.Line(_palette.Outline, right, bottomRight, right, lowerBottom);
                    }
                    break;
                }
            }
        }
    }

    /// <summary>A joint is a tick through the beam; a free end is a vertical end face.</summary>
    private void PaintZeroWidth(KataElevationColumn column)
    {
        if (!column.IsZeroWidth) return;

        double x = _scene.X(column.Extent.Mid);
        if (x < -10 || x > _scene.Width + 10) return;

        var (topMm, bottomMm) = _scene.BeamFaces(new Interval1D(column.Extent.Mid - 1.0, column.Extent.Mid + 1.0));
        double top = _scene.Y(topMm), bottom = _scene.Y(bottomMm);
        if (column.Kind == KataColumnKind.FreeEnd)
        {
            _draw.Line(_palette.Outline, x, top, x, bottom);
        }
        else
        {
            _draw.Line(_palette.Outline, x, top - 6, x, bottom + 6);
        }
    }

    private void PaintCrossingBeam(KataElevationSupport support)
    {
        if (support.CrossingBeamX is not { } crossing) return;

        double x = _scene.X(crossing);
        if (x < -10 || x > _scene.Width + 10) return;

        var (topMm, bottomMm) = _scene.BeamFaces(support.Extent);
        _draw.Line(_palette.Marker, x, _scene.Y(topMm) - 4, x, _scene.Y(bottomMm) + 4);
    }

    /// <summary>Grids written to Kata (row 22) claim their bubble first; the others fill the gaps.</summary>
    private void PaintBubbles()
    {
        var lane = new KataLabelLane();
        foreach (var grid in _scene.Elevation.Grids.OrderBy(g => g.OffsetText is null))
        {
            double x = _scene.X(grid.X);
            var name = _draw.Text(grid.Name, _palette.BubbleText, KataDrawPrimitives.TextSize, bold: true);
            double radius = System.Math.Max(KataElevationScene.BubbleRadius, name.Width / 2.0 + 4.0);
            if (x + radius < 0 || x - radius > _scene.Width) continue;
            if (!lane.TryPlace(x - radius, x + radius)) continue;

            _draw.Circle(_palette.Fill, _palette.Bubble, x, _scene.BubbleY, radius);
            _draw.Centered(name, x, _scene.BubbleY - name.Height / 2.0);
        }
    }
}
