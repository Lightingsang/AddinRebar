using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;

// WPF types, not the Revit ones the SDK imports globally.
using Brush = System.Windows.Media.Brush;
using Point = System.Windows.Point;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// Kata's section n-n (<see cref="KataSectionDrawing"/>) in a floating panel over the elevation, fitted to it then
/// zoomed and panned on its own (<see cref="KataSectionView"/>), in model
/// millimetres scaled like CAD with the elevation's lineweights (<see cref="KataCadPens"/>): concrete and slab breaks,
/// hoop and ties, bars as kata_block_THEP (a circle of the bar's diameter, its polyline width d / 12, with a cross),
/// leaders with their arrowheads, marking circles, dimensions (<see cref="KataCadDimPainter"/>), tags
/// (<see cref="KataCadTag"/>) and the title.
/// </summary>
internal sealed class KataSectionCadPainter
{
    private const double PaddingPx = 14.0;

    /// <summary>kata_block_THEP's cross: arms half the bar's diameter, diagonals 0.35 of it.</summary>
    private const double CrossArm = 0.5;

    private const double CrossDiagonal = 0.35;

    /// <summary>_DotSmall: a ring of radius 0.0625 and width 0.5 of the arrow size, i.e. a dot of radius 0.3125.</summary>
    private const double DotSmallRadius = 0.3125;

    private readonly KataCanvasPalette _palette;
    private readonly KataDrawPrimitives _draw;
    private readonly KataSectionDrawing _drawing;
    private readonly Rect _panel;
    private readonly KataSectionView _view;

    public KataSectionCadPainter(KataCanvasPalette palette, KataDrawPrimitives draw, KataSectionDrawing drawing, Rect panel, KataSectionView view)
    {
        _palette = palette ?? throw new ArgumentNullException(nameof(palette));
        _draw = draw ?? throw new ArgumentNullException(nameof(draw));
        _drawing = drawing ?? throw new ArgumentNullException(nameof(drawing));
        _panel = panel;
        _view = view;
    }

    public void Paint()
    {
        var border = KataCadPens.Lineweight(_palette.KataGrey, KataCadPens.ThinPx, _draw.PixelsPerDip, 1.0);
        _draw.Box(_palette.Fill, border, _panel.Left, _panel.Top, _panel.Right, _panel.Bottom);

        double width = _drawing.MaxX - _drawing.MinX, height = _drawing.Top - _drawing.Bottom;
        double fit = Math.Min((_panel.Width - 2.0 * PaddingPx) / width, (_panel.Height - 2.0 * PaddingPx) / height);
        if (width <= 0.0 || height <= 0.0 || fit <= 0.0) return;

        // Fitted to the panel, then the panel's own zoom about its centre and its own pan.
        double scale = fit * _view.Zoom;
        double ox = _panel.Left + _panel.Width / 2.0 + _view.Pan.X - (_drawing.MinX + _drawing.MaxX) / 2.0 * scale;
        double oy = _panel.Top + _panel.Height / 2.0 + _view.Pan.Y + (_drawing.Top + _drawing.Bottom) / 2.0 * scale;
        Point P(double x, double z) => new(ox + x * scale, oy - z * scale);

        _draw.PushClip(_panel);
        try
        {
            Paint(P, scale);
        }
        finally
        {
            _draw.Pop();
        }
    }

    private void Paint(Func<double, double, Point> p, double scale)
    {
        Pen Weight(Brush brush, double px) => KataCadPens.Lineweight(brush, px, _draw.PixelsPerDip, scale);
        var pens = new Dictionary<KataDrawingPen, Pen>
        {
            [KataDrawingPen.Outline] = Weight(_palette.KataOutline, KataCadPens.OutlinePx),
            [KataDrawingPen.Hidden] = Weight(_palette.KataHidden, KataCadPens.ThinPx),
            [KataDrawingPen.Grid] = Weight(_palette.KataGrey, KataCadPens.ThinPx),
            [KataDrawingPen.Thin] = Weight(_palette.KataGrey, KataCadPens.ThinPx),
            [KataDrawingPen.Stirrup] = Weight(_palette.KataStirrup, KataCadPens.BarPx),
            [KataDrawingPen.Bar] = Weight(_palette.KataBar, KataCadPens.BarPx)
        };

        foreach (var line in _drawing.Lines)
            _draw.Polyline(null, pens[line.Pen], KataBulge.Points(line.Vertices).Select(v => p(v.X, v.Z)).ToList());

        var leader = Weight(_palette.KataLeaderBrush, KataCadPens.ThinPx);
        foreach (var mark in _drawing.Marks)
        {
            var c = p(mark.X, mark.Z);
            _draw.Circle(null, leader, c.X, c.Y, mark.Radius * scale);
        }

        foreach (var l in _drawing.Leaders) PaintLeader(l, p, scale, leader);

        var bar = pens[KataDrawingPen.Bar];
        foreach (var b in _drawing.Bars) PaintBar(b, p, scale, bar);

        var text = new KataCadText(_draw, scale);
        new KataCadDimPainter(_palette, _draw, text, (x, z) => p(x, z), scale, pens[KataDrawingPen.Thin], Weight(_palette.KataRun, KataCadPens.ThinPx))
            .Paint(_drawing.Dims);

        var tag = new KataCadTag(_palette, _draw, scale);
        foreach (var t in _drawing.Tags)
        {
            var at = p(t.X, t.Z);
            tag.OnLeader(t.Text, t.Numbers, at.X, at.Y, t.PointsRight, t.Spacing);
        }

        KataElevationCadPainter.PaintTitle(_draw, text, _palette, scale, _drawing.Title, p(_drawing.Title.X, _drawing.Title.Z));
    }

    /// <summary>kata_block_THEP: the bar's outline as a polyline of width d / 12 and a cross through its centre.</summary>
    private void PaintBar(KataSectionBar bar, Func<double, double, Point> p, double scale, Pen cross)
    {
        var c = p(bar.X, bar.Z);
        double r = bar.Diameter / 2.0 * scale, width = Math.Max(1.0 / _draw.PixelsPerDip, bar.Diameter / 12.0 * scale);
        var ring = new Pen(_palette.KataBar, width);
        ring.Freeze();
        _draw.Circle(null, ring, c.X, c.Y, r);

        double arm = CrossArm * bar.Diameter * scale, diagonal = CrossDiagonal * bar.Diameter * scale;
        _draw.Line(cross, c.X - arm, c.Y, c.X + arm, c.Y);
        _draw.Line(cross, c.X, c.Y - arm, c.X, c.Y + arm);
        _draw.Line(cross, c.X - diagonal, c.Y - diagonal, c.X + diagonal, c.Y + diagonal);
        _draw.Line(cross, c.X - diagonal, c.Y + diagonal, c.X + diagonal, c.Y - diagonal);
    }

    /// <summary>A leader and its arrowhead block at the first point.</summary>
    private void PaintLeader(KataSectionLeader leader, Func<double, double, Point> p, double scale, Pen pen)
    {
        var points = leader.Points.Select(v => p(v.X, v.Z)).ToList();
        if (points.Count < 2) return;

        double size = leader.ArrowSize * scale;
        var first = points[0];
        var next = points[1];
        switch (leader.Arrow)
        {
            case KataLeaderArrow.DotBlank:
                // The circle and the leader from its edge, as the block's own stroke continues it.
                _draw.Circle(null, pen, first.X, first.Y, size / 2.0);
                points[0] = Toward(first, next, size / 2.0);
                break;
            case KataLeaderArrow.DotSmall:
                _draw.Circle(_palette.KataLeaderBrush, pen, first.X, first.Y, DotSmallRadius * size);
                break;
            case KataLeaderArrow.Closed:
                _draw.Arrow(_palette.KataLeaderBrush, first.X, first.Y, next.X, next.Y, size);
                break;
        }

        _draw.Polyline(null, pen, points);
    }

    private static Point Toward(Point from, Point to, double distance)
    {
        var d = to - from;
        double length = d.Length;
        return length <= distance ? to : from + d * (distance / length);
    }
}
