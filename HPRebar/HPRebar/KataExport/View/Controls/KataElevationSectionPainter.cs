using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HPRebar.Core.KataExport.Models;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// Kata's sections (<see cref="KataSectionCuts"/>): every cut marked on the elevation with its number, and the card in
/// the canvas corner for the cut of the column picked in the table — a span's middle cut, a support's nearest one —
/// titled "MẶT CẮT n-n", drawing concrete b×h, outer hoop, inner stirrups and C ties, and every longitudinal bar at
/// that station with its Kata number on a leader.
/// </summary>
internal sealed class KataElevationSectionPainter
{
    private const double CardWidth = 220.0;
    private const double CardHeight = 270.0;
    private const double CardMargin = 12.0;
    private const double LabelRadius = 6.5;

    private readonly KataElevationScene _scene;
    private readonly KataRebarPlan _plan;
    private readonly KataStationMap _map;
    private readonly KataCanvasPalette _palette;
    private readonly KataDrawPrimitives _draw;

    private readonly IReadOnlyList<KataSectionCut> _cuts;
    private readonly bool _markers;

    /// <param name="markers">Mark the cuts on the elevation (Kata's elevation draws its own flags).</param>
    public KataElevationSectionPainter(KataElevationScene scene, KataRebarDrawing drawing, KataStationMap map, KataCanvasPalette palette, KataDrawPrimitives draw,
        bool markers = true)
    {
        _markers = markers;
        _scene = scene ?? throw new ArgumentNullException(nameof(scene));
        if (drawing is null) throw new ArgumentNullException(nameof(drawing));
        _plan = drawing.Plan;
        _cuts = drawing.Cuts;
        _map = map ?? throw new ArgumentNullException(nameof(map));
        _palette = palette ?? throw new ArgumentNullException(nameof(palette));
        _draw = draw ?? throw new ArgumentNullException(nameof(draw));
    }

    /// <summary>Width the card takes off the right of a canvas this size (0 when the canvas is too small to show it).</summary>
    public static double ReservedWidth(double width, double height) => HasRoom(width, height) ? CardWidth + 2.0 * CardMargin : 0.0;

    private static bool HasRoom(double width, double height) => width >= 420 && height >= 290;

    public void Paint()
    {
        var cuts = _cuts;
        if (cuts.Count == 0) return;

        var selected = Selected(cuts);
        if (_markers)
            foreach (var cut in cuts) PaintMarker(cut, ReferenceEquals(cut, selected));
        if (selected is null || !HasRoom(_scene.Width, _scene.Height)) return;

        PaintCard(selected);
    }

    private void PaintCard(KataSectionCut cut)
    {
        double localX = cut.X;
        double right = _scene.Width - CardMargin, left = right - CardWidth, top = CardMargin, bottom = top + CardHeight;
        _draw.Box(_palette.SectionCardFill, _palette.GridLine, left, top, right, bottom);
        _draw.Centered(_draw.Text($"MẶT CẮT {cut.Number}-{cut.Number}", _palette.Accent, KataDrawPrimitives.SmallTextSize, bold: true), (left + right) / 2.0, top + 6.0);

        double b = _plan.Spec.Width, h = _plan.Spec.DepthOf(cut.SpanIndex);
        _draw.Centered(_draw.Text($"{b:0} × {h:0} mm · nhịp {cut.SpanIndex + 1} · x = {localX:0}", _palette.MutedText, KataDrawPrimitives.SmallTextSize - 1.0), (left + right) / 2.0, top + 21.0);

        // Section scaled into the card between two rows of bar numbers, beam top at z = 0, centre line at y = 0.
        double areaTop = top + 56.0, areaBottom = bottom - 62.0;
        double scale = Math.Min((CardWidth - 70.0) / b, (areaBottom - areaTop) / h);
        double cx = (left + right) / 2.0 - 10.0;
        double y0 = areaTop + ((areaBottom - areaTop) - h * scale) / 2.0;
        // Seen from the drawing's side: when the drawing runs against the plan, +Y is on the left.
        double X(double y) => cx + _map.Direction * y * scale;
        double Y(double z) => y0 - z * scale;

        _draw.Box(_palette.BeamFill, _palette.Outline, X(-b / 2.0), Y(0.0), X(b / 2.0), Y(-h));

        var rules = _plan.Rules;
        double c = rules.StirrupCover + rules.StirrupDiameter / 2.0;
        var hoops = KataSectionCuts.Hoops(_plan.Layout, cut.SpanIndex, localX);
        if (hoops is not null)
            _draw.Box(null, _palette.RebarStirrup, X(-b / 2.0 + c), Y(-c), X(b / 2.0 - c), Y(-h + c));

        var sets = KataSectionCuts.Sets(_plan.Layout, localX).ToList();
        foreach (var set in sets) PaintSet(set, X, Y);

        var crossing = KataSectionCuts.Crossing(_plan.Layout, localX).ToList();
        foreach (var (bar, z) in crossing)
        {
            var (fill, pen) = Appearance(bar);
            _draw.Circle(fill, pen, X(bar.TransverseY), Y(z), Math.Max(2.2, Math.Min(4.5, bar.Diameter / 2.0 * scale)));
        }

        PaintBarNumbers(crossing, h, X, Y, X(b / 2.0 * _map.Direction), Y(0.0) - 12.0, Y(-h) + 12.0);

        var footer = new List<string>();
        if (hoops is not null) footer.Add($"({hoops.BarNumber}) Ø{rules.StirrupDiameter:0}a{hoops.Spacing:0}");
        foreach (var set in sets.Where(KataBarNumbering.IsTie).GroupBy(s => s.BarNumber).Select(g => g.First()))
            footer.Add($"({set.BarNumber}) C Ø{set.Diameter:0}");
        foreach (var set in sets.Where(s => !KataBarNumbering.IsTie(s)).GroupBy(s => s.BarNumber).Select(g => g.First()))
            footer.Add($"({set.BarNumber}) đai trong Ø{set.Diameter:0}");
        if (footer.Count > 0)
            _draw.Centered(_draw.Text(string.Join(" · ", footer), _palette.RebarText, KataDrawPrimitives.SmallTextSize - 1.0), (left + right) / 2.0, bottom - 34.0);

        int topCount = crossing.Count(x => x.Bar.Role != KataBarRole.SideBar && x.Z > -h / 2.0);
        int bottomCount = crossing.Count(x => x.Bar.Role != KataBarRole.SideBar && x.Z <= -h / 2.0);
        int sideCount = crossing.Count(x => x.Bar.Role == KataBarRole.SideBar);
        string summary = $"Trên {topCount} · Dưới {bottomCount}" + (sideCount > 0 ? $" · Giá {sideCount}" : "");
        _draw.Centered(_draw.Text(summary, _palette.MutedText, KataDrawPrimitives.SmallTextSize - 1.0), (left + right) / 2.0, bottom - 20.0);
    }

    /// <summary>
    /// One circled number per bar number: top bars in a row above the section, bottom bars below it, side bars to the
    /// right, each on a thin leader to the nearest of its bars.
    /// </summary>
    private void PaintBarNumbers(IReadOnlyList<(KataRebarCurve Bar, double Z)> crossing, double h, Func<double, double> X, Func<double, double> Y,
        double sideX, double topRowY, double bottomRowY)
    {
        var ring = new System.Windows.Media.Pen(_palette.Accent, 0.9);
        ring.Freeze();
        var leader = new System.Windows.Media.Pen(_palette.MutedText, 0.5);
        leader.Freeze();
        var topLane = new KataLabelLane();
        var bottomLane = new KataLabelLane();
        double sideY = double.NegativeInfinity;

        // A number can sit both at the top and at the bottom (Kata gives identical bars one number): one label each.
        // Side bars run down the whole web and keep one label, as Kata writes "2x2Ø12" once.
        foreach (var group in crossing
                     .GroupBy(c => (c.Bar.BarNumber, Side: c.Bar.Role == KataBarRole.SideBar,
                         Upper: c.Bar.Role != KataBarRole.SideBar && c.Z > -h / 2.0))
                     .OrderBy(g => g.Key.BarNumber))
        {
            var dots = group.Select(g => (X: X(g.Bar.TransverseY), Y: Y(g.Z))).ToList();
            bool side = group.Key.Side;
            bool upper = group.Key.Upper;
            double lx, ly;
            if (side)
            {
                lx = sideX + 18.0;
                ly = Math.Max(dots.Min(d => d.Y), sideY + 2.0 * LabelRadius + 3.0);
                sideY = ly;
            }
            else
            {
                double want = dots.Average(d => d.X) - LabelRadius;
                var lane = upper ? topLane : bottomLane;
                if (lane.PlaceNear(want, 2.0 * LabelRadius, 60.0, 4.0) is not { } at) continue;
                lx = at + LabelRadius;
                ly = upper ? topRowY : bottomRowY;
            }

            var target = dots.OrderBy(d => Math.Abs(d.X - lx) + Math.Abs(d.Y - ly)).First();
            _draw.Line(leader, target.X, target.Y, lx, ly);
            _draw.Circle(_palette.SectionCardFill, ring, lx, ly, LabelRadius);
            var text = _draw.Text(group.Key.BarNumber.ToString(CultureInfo.InvariantCulture), _palette.Accent, KataDrawPrimitives.SmallTextSize - 1.5, bold: true);
            _draw.Centered(text, lx, ly - text.Height / 2.0);
        }
    }

    private void PaintSet(KataBarSet set, Func<double, double> X, Func<double, double> Y)
    {
        var p = set.Shape.Points;
        if (set.WrapEnds && p.Count == 2)
        {
            // A tie round two bars: its straight part a bend radius off them (≈ 1.75 d), a ring round each bar.
            double radius = 1.75 * set.Diameter;
            var (tie, barA, barB) = KataTieWrap.Lay(set, radius);
            var t = tie.Points;
            _draw.Line(_palette.RebarStirrup, X(t[0].Y), Y(t[0].Z), X(t[1].Y), Y(t[1].Z));
            double r = Math.Abs(X(radius) - X(0.0));
            foreach (var bar in new[] { barA, barB })
                _draw.Circle(null, _palette.RebarStirrup, X(bar.Y), Y(bar.Z), Math.Max(2.0, r));
            return;
        }

        for (int i = 0; i + 1 < p.Count; i++)
            _draw.Line(_palette.RebarStirrup, X(p[i].Y), Y(p[i].Z), X(p[i + 1].Y), Y(p[i + 1].Z));
    }

    /// <summary>The cut of the selected column: a span's middle one, else the one nearest the column.</summary>
    private KataSectionCut? Selected(IReadOnlyList<KataSectionCut> cuts)
    {
        var columns = _scene.Elevation.Columns;
        int index = _scene.SelectedColumn;
        if (index < 0 || index >= columns.Count) index = columns.ToList().FindIndex(c => c.Kind == KataColumnKind.Span);
        if (index < 0) return null;

        var column = columns[index];
        double centre = (Local(column.Extent.Start) + Local(column.Extent.End)) / 2.0;
        if (column.Kind == KataColumnKind.Span)
        {
            var inSpan = cuts.Where(c => c.X >= Math.Min(Local(column.Extent.Start), Local(column.Extent.End)) - 1.0
                                      && c.X <= Math.Max(Local(column.Extent.Start), Local(column.Extent.End)) + 1.0).ToList();
            if (inSpan.Count > 0) return inSpan[inSpan.Count / 2];
        }

        return cuts.OrderBy(c => Math.Abs(c.X - centre)).First();
    }

    private double Local(double station) => (station - _map.Origin) * _map.Direction;

    /// <summary>A cut on the elevation: a short stroke above and below the beam and its number under the beam, past the bar tags.</summary>
    private void PaintMarker(KataSectionCut cut, bool selected)
    {
        double station = _map.ToStation(cut.X);
        double sx = _scene.X(station);
        if (!_scene.IsVisible(sx - 6, sx + 6)) return;

        var (topMm, bottomMm) = _scene.BeamFaces(new Interval1D(station, station));
        var pen = selected ? _palette.Marker : _palette.Dimension;
        if (selected) _draw.Line(_palette.Marker, sx, _scene.Y(topMm), sx, _scene.Y(bottomMm));
        _draw.Line(pen, sx, _scene.Y(topMm) - 14.0, sx, _scene.Y(topMm) - 4.0);
        _draw.Line(pen, sx, _scene.Y(bottomMm) + 4.0, sx, _scene.Y(bottomMm) + 14.0);
        var text = _draw.Text(cut.Number.ToString(CultureInfo.InvariantCulture), selected ? _palette.Accent : _palette.DimText, KataDrawPrimitives.SmallTextSize - 1.0, bold: selected);
        _draw.At(text, sx + 2.0, Math.Max(_scene.Y(bottomMm) + 6.0, _scene.BandBottomY + _scene.TagsBelowPx + 2.0));
    }

    private (System.Windows.Media.Brush Fill, System.Windows.Media.Pen Pen) Appearance(KataRebarCurve bar) => bar.Role switch
    {
        KataBarRole.MainTop => (_palette.RebarMainTopBrush, _palette.RebarMainTop),
        KataBarRole.MainBottom => (_palette.RebarMainBottomBrush, _palette.RebarMainBottom),
        KataBarRole.ExtraTop => bar.Layer <= 1 ? (_palette.RebarExtraTop1Brush, _palette.RebarExtraTop1) : (_palette.RebarExtraTop2Brush, _palette.RebarExtraTop2),
        KataBarRole.ExtraBottom => bar.Layer <= 1 ? (_palette.RebarExtraBottom1Brush, _palette.RebarExtraBottom1) : (_palette.RebarExtraBottom2Brush, _palette.RebarExtraBottom2),
        KataBarRole.SideBar => (_palette.RebarSideBrush, _palette.RebarSide),
        _ => (_palette.RebarMainTopBrush, _palette.RebarMainTop)
    };
}
