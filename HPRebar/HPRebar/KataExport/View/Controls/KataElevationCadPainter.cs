using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;

// WPF types, not the Revit ones the SDK imports globally.
using Brush = System.Windows.Media.Brush;
using Point = System.Windows.Point;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// Kata's elevation (<see cref="KataElevationDrawing"/>) on the canvas while the bars are shown, in model millimetres
/// scaled with the view like CAD. Lines keep Kata's lineweights in whole pixels at any zoom (0.35 → 3 px, 0.20 →
/// 2 px, thinner → 1 px, as AutoCAD shows them with LWDISPLAY on) and its HIDDEN / CENTER linetypes scaled as drawn.
/// The dimensions are <see cref="KataCadDimPainter"/>'s, the bar tags <see cref="KataElevationBarTagPainter"/>'s.
/// </summary>
internal sealed class KataElevationCadPainter
{
    private const double BarPx = 3.0;
    private const double OutlinePx = 2.0;
    private const double ThinPx = 1.0;

    // kata_block_SECBAL at scale 25: stem −25..108.9, flag (0, 69.45)–(147.3, 108.9), number right-aligned 12.5 left
    // of the stem, its middle 43.75 up.
    private const double FlagStemBottom = -25.0;
    private const double FlagTop = 108.9;
    private const double FlagSlopeStart = 69.45;
    private const double FlagWidth = 147.3;
    private const double FlagTextGap = 12.5;
    private const double FlagTextMiddle = 43.75;

    private readonly KataElevationScene _scene;
    private readonly KataCanvasPalette _palette;
    private readonly KataDrawPrimitives _draw;
    private readonly KataElevationDrawing _drawing;
    private readonly KataStationMap _map;
    private readonly KataCadText _text;

    public KataElevationCadPainter(KataElevationScene scene, KataCanvasPalette palette, KataDrawPrimitives draw, KataElevationDrawing drawing, KataStationMap map)
    {
        _scene = scene ?? throw new ArgumentNullException(nameof(scene));
        _palette = palette ?? throw new ArgumentNullException(nameof(palette));
        _draw = draw ?? throw new ArgumentNullException(nameof(draw));
        _drawing = drawing ?? throw new ArgumentNullException(nameof(drawing));
        _map = map ?? throw new ArgumentNullException(nameof(map));
        _text = new KataCadText(draw, Scale);
    }

    private double Scale => _scene.Viewport.Scale;

    public void Paint()
    {
        _draw.Box(_palette.Fill, null, 0, 0, _scene.Width, _scene.Height);
        PaintSelection();

        var pens = new Dictionary<KataDrawingPen, Pen>
        {
            [KataDrawingPen.Outline] = Lineweight(_palette.KataOutline, OutlinePx),
            [KataDrawingPen.Hidden] = Lineweight(_palette.KataHidden, ThinPx, KataDrawingStyle.HiddenDashes),
            [KataDrawingPen.Grid] = Lineweight(_palette.KataGrey, ThinPx, KataDrawingStyle.CenterDashes),
            [KataDrawingPen.Thin] = Lineweight(_palette.KataGrey, ThinPx),
            [KataDrawingPen.Stirrup] = Lineweight(_palette.KataStirrup, BarPx),
            [KataDrawingPen.Bar] = Lineweight(_palette.KataBar, BarPx)
        };

        // Bars last, over the stirrups and the concrete, as Kata's layer order shows them.
        foreach (var line in _drawing.Lines.OrderBy(l => l.Pen == KataDrawingPen.Bar ? 1 : 0))
            _draw.Polyline(null, pens[line.Pen], line.Points.Select(p => P(p.X, p.Z)).ToList());

        new KataCadDimPainter(_palette, _draw, _text, P, Scale, Lineweight(_palette.KataGrey, ThinPx), Lineweight(_palette.KataRun, ThinPx)).Paint(_drawing.Dims);

        var thin = pens[KataDrawingPen.Thin];
        foreach (var flag in _drawing.Flags) PaintFlag(flag);
        foreach (var bubble in _drawing.Bubbles) PaintBubble(bubble, thin);
        if (_drawing.Level is { } level) PaintLevel(level, thin);
        if (_drawing.Title is { } title) PaintTitle(title);
    }

    /// <summary>Screen point of a drawing point (mm along the run, mm up from the beam top).</summary>
    private Point P(double x, double z) => new(_scene.X(_map.ToStation(x)), _scene.Y(_scene.Elevation.TopMm + z));

    /// <summary>A pen a whole number of device pixels wide, dashed as the linetype is drawn (solid once its dashes shrink under 2 px).</summary>
    private Pen Lineweight(Brush brush, double px, double[]? dashesMm = null)
    {
        double thickness = px / _draw.PixelsPerDip;
        var pen = new Pen(brush, thickness) { LineJoin = PenLineJoin.Round, StartLineCap = PenLineCap.Flat, EndLineCap = PenLineCap.Flat };
        if (dashesMm is not null && dashesMm.Min() * Scale >= 2.0)
            pen.DashStyle = new DashStyle(dashesMm.Select(d => d * Scale / thickness), 0.0);
        pen.Freeze();
        return pen;
    }

    private void PaintSelection()
    {
        var columns = _scene.Elevation.Columns;
        if (_scene.SelectedColumn < 0 || _scene.SelectedColumn >= columns.Count) return;

        var (left, right) = _scene.ScreenSpan(columns[_scene.SelectedColumn].Extent, 10.0);
        _draw.Box(_palette.SelectionFill, null, left, P(0.0, _drawing.Top).Y, right, P(0.0, _drawing.Bottom).Y);
    }

    /// <summary>kata_block_SECBAL: a stem with a filled flag at its top (under the beam the block is mirrored upside down), the cut number before it.</summary>
    private void PaintFlag(KataDrawingFlag flag)
    {
        var o = P(flag.X, flag.Z);
        if (!_scene.IsVisible(o.X - FlagWidth * Scale, o.X + FlagWidth * Scale)) return;
        double s = Scale, up = flag.Below ? 1.0 : -1.0;
        var stem = Lineweight(_palette.KataFlag, ThinPx);
        _draw.Line(stem, o.X, o.Y + up * FlagStemBottom * s, o.X, o.Y + up * FlagTop * s);
        _draw.Polyline(_palette.KataFlag, null, new[]
        {
            new Point(o.X, o.Y + up * FlagSlopeStart * s), new Point(o.X + FlagWidth * s, o.Y + up * FlagTop * s), new Point(o.X, o.Y + up * FlagTop * s)
        });

        double middle = o.Y + up * FlagTextMiddle * s;
        _text.Draw(flag.Number.ToString(System.Globalization.CultureInfo.InvariantCulture), _palette.KataFlag, KataTagStyle.TextHeight,
            o.X - FlagTextGap * s, middle + KataTagStyle.TextHeight * s / 2.0, KataCadText.Align.Right);
    }

    /// <summary>kata_block_GRID: a circle, four ticks out to 175, the grid's name in the middle.</summary>
    private void PaintBubble(KataDrawingBubble bubble, Pen pen)
    {
        var c = P(bubble.X, bubble.Z);
        double r = KataDrawingStyle.BubbleRadius * Scale, t = KataDrawingStyle.BubbleTickEnd * Scale;
        if (!_scene.IsVisible(c.X - t, c.X + t)) return;
        _draw.Circle(null, pen, c.X, c.Y, r);
        _draw.Line(pen, c.X + r, c.Y, c.X + t, c.Y);
        _draw.Line(pen, c.X - r, c.Y, c.X - t, c.Y);
        _draw.Line(pen, c.X, c.Y + r, c.X, c.Y + t);
        _draw.Line(pen, c.X, c.Y - r, c.X, c.Y - t);
        _text.Draw(bubble.Name, _palette.KataNumber, KataDrawingStyle.BubbleTextHeight, c.X, c.Y + KataDrawingStyle.BubbleTextHeight * Scale / 2.0, KataCadText.Align.Centre);
    }

    /// <summary>
    /// kata_block_CT at scale 25: a triangle on the beam top (left half solid, right half hatched), a stem up from its
    /// tip, two lines across, the level written over the upper one.
    /// </summary>
    private void PaintLevel(KataDrawingLevel level, Pen pen)
    {
        var o = P(level.X, level.Z);
        double s = Scale;
        Point At(double x, double y) => new(o.X + x * s, o.Y - y * s);

        _draw.Line(pen, o.X, o.Y, o.X, o.Y - 167.4 * s);
        _draw.Polyline(_palette.KataGrey, pen, new[] { At(0, 0), At(-40.3, 39.9), At(0, 39.9) });
        _draw.Polyline(null, pen, new[] { At(0, 0), At(40.3, 39.9), At(0, 39.9) });
        // ANSI31: 45° hatch lines, parallel to the right half's slope.
        for (double c = 13.3; c < 39.9; c += 13.3) _draw.Line(pen, At(0, c).X, At(0, c).Y, At(39.9 - c, 39.9).X, At(39.9 - c, 39.9).Y);
        _draw.Line(pen, At(-39.8, 78.5).X, At(-39.8, 78.5).Y, At(286.7, 78.5).X, At(286.7, 78.5).Y);
        _draw.Line(pen, At(-41.0, 0).X, o.Y, At(289.0, 0).X, o.Y);
        _text.Draw(level.Text, _palette.KataTagText, KataTagStyle.TextHeight, At(35.25, 101.7).X, At(35.25, 101.7).Y, KataCadText.Align.Left);
    }

    /// <summary>kata_block_TD: the beam's name, count and length underlined, the scale under it.</summary>
    private void PaintTitle(KataDrawingTitle title)
    {
        var o = P(title.X, title.Z);
        double s = Scale;
        double baseline = o.Y - KataDrawingStyle.TitleNameLift * s;
        double width = _text.Draw(title.Name, _palette.KataNumber, KataDrawingStyle.TitleTextHeight, o.X, baseline, KataCadText.Align.Centre);
        if (width > 0.0)
        {
            double underline = baseline + KataDrawingStyle.TitleTextHeight * 0.2 * s;
            _draw.Line(Lineweight(_palette.KataNumber, ThinPx), o.X - width / 2.0, underline, o.X + width / 2.0, underline);
        }

        _text.Draw(title.Scale, _palette.KataTagText, KataTagStyle.TextHeight, o.X, o.Y + KataDrawingStyle.TitleScaleDrop * s, KataCadText.Align.Centre);
    }
}
