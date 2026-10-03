using System;
using System.Collections.Generic;
using System.Windows.Media;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;

// WPF types, not the Revit ones the SDK imports globally.
using Point = System.Windows.Point;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// Kata's dimensions in model millimetres (<see cref="KataDrawingStyle"/>). kata_dim_25: extension lines of fixed
/// length from the dimension line and a little past it, the line running past them, a heavy 45° arch tick at each
/// end, the value above the line (left of it, reading upwards, when vertical). kata_rai_thep: a magenta line with a
/// run arrow and a cross stroke at each end, no text.
/// </summary>
internal sealed class KataCadDimPainter
{
    private const double RunArrowHalfWidth = 12.5;

    private readonly KataCanvasPalette _palette;
    private readonly KataDrawPrimitives _draw;
    private readonly KataCadText _text;
    private readonly Func<double, double, Point> _p;
    private readonly double _scale;
    private readonly Pen _line;
    private readonly Pen _run;
    private readonly Pen _tick;

    public KataCadDimPainter(KataCanvasPalette palette, KataDrawPrimitives draw, KataCadText text, Func<double, double, Point> point, double scale, Pen line, Pen run)
    {
        _palette = palette;
        _draw = draw;
        _text = text;
        _p = point;
        _scale = scale;
        _line = line;
        _run = run;
        _tick = new Pen(_palette.KataGrey, Math.Max(line.Thickness, KataDrawingStyle.DimTickWidth * scale)) { StartLineCap = PenLineCap.Flat, EndLineCap = PenLineCap.Flat };
        _tick.Freeze();
    }

    public void Paint(IReadOnlyList<KataDrawingDim> dims)
    {
        foreach (var dim in dims)
        {
            if (dim.Style == KataDimStyle.Run) PaintRun(dim);
            else if (dim.Vertical) PaintVertical(dim);
            else PaintHorizontal(dim);
        }
    }

    private void PaintHorizontal(KataDrawingDim dim)
    {
        var a = _p(dim.X1, dim.LineAt);
        var b = _p(dim.X2, dim.LineAt);
        Extension(a, _p(dim.X1, dim.Z1), vertical: true);
        Extension(b, _p(dim.X2, dim.Z2), vertical: true);

        double left = Math.Min(a.X, b.X), right = Math.Max(a.X, b.X), beyond = KataDrawingStyle.DimLineBeyond * _scale;
        _draw.Line(_line, left - beyond, a.Y, right + beyond, a.Y);
        Tick(a, vertical: false);
        Tick(b, vertical: false);
        _text.Draw(dim.Text, _palette.KataTagText, KataDrawingStyle.DimTextHeight, (a.X + b.X) / 2.0,
            a.Y - KataDrawingStyle.DimTextGap * _scale, KataCadText.Align.Centre);
    }

    private void PaintVertical(KataDrawingDim dim)
    {
        var a = _p(dim.LineAt, dim.Z1);
        var b = _p(dim.LineAt, dim.Z2);
        Extension(a, _p(dim.X1, dim.Z1), vertical: false);
        Extension(b, _p(dim.X2, dim.Z2), vertical: false);

        double top = Math.Min(a.Y, b.Y), bottom = Math.Max(a.Y, b.Y), beyond = KataDrawingStyle.DimLineBeyond * _scale;
        _draw.Line(_line, a.X, top - beyond, a.X, bottom + beyond);
        Tick(a, vertical: true);
        Tick(b, vertical: true);
        _text.Draw(dim.Text, _palette.KataTagText, KataDrawingStyle.DimTextHeight, a.X - KataDrawingStyle.DimTextGap * _scale,
            (a.Y + b.Y) / 2.0, KataCadText.Align.Centre, vertical: true);
    }

    /// <summary>
    /// The extension line at <paramref name="onLine"/> toward its origin: a fixed length on the origin's side (never
    /// past the origin) and a little beyond the dimension line.
    /// </summary>
    private void Extension(Point onLine, Point origin, bool vertical)
    {
        double toward = vertical ? origin.Y - onLine.Y : origin.X - onLine.X;
        if (Math.Abs(toward) < 0.5) return;
        double sign = Math.Sign(toward);
        double near = Math.Min(Math.Abs(toward), KataDrawingStyle.ExtensionLength * _scale) * sign;
        double far = -KataDrawingStyle.ExtensionBeyond * _scale * sign;
        if (vertical) _draw.Line(_line, onLine.X, onLine.Y + far, onLine.X, onLine.Y + near);
        else _draw.Line(_line, onLine.X + far, onLine.Y, onLine.X + near, onLine.Y);
    }

    /// <summary>
    /// kata_block_ArchTick: a heavy slash half a tick size each way, up to the right on a horizontal line; the block
    /// turns with a vertical line, so there it leans the other way.
    /// </summary>
    private void Tick(Point at, bool vertical)
    {
        double h = KataDrawingStyle.DimTickSize * _scale / 2.0, lean = vertical ? -h : h;
        _draw.Line(_tick, at.X - h, at.Y + lean, at.X + h, at.Y - lean);
    }

    /// <summary>A stirrup run: the line, and at each end a cross stroke and an arrow pointing out along it.</summary>
    private void PaintRun(KataDrawingDim dim)
    {
        var a = _p(dim.X1, dim.LineAt);
        var b = _p(dim.X2, dim.LineAt);
        _draw.Line(_run, a.X, a.Y, b.X, b.Y);
        RunEnd(a, b);
        RunEnd(b, a);
    }

    private void RunEnd(Point end, Point other)
    {
        double dir = Math.Sign(other.X - end.X), length = KataDrawingStyle.RunArrowLength * _scale;
        double cross = KataDrawingStyle.RunTickHalf * _scale, half = RunArrowHalfWidth * _scale;
        if (dir == 0) return;
        _draw.Line(_run, end.X, end.Y - cross, end.X, end.Y + cross);
        _draw.Polyline(_palette.KataRun, null, new[]
        {
            end, new Point(end.X + dir * length, end.Y - half), new Point(end.X + dir * length, end.Y + half)
        });
    }
}
