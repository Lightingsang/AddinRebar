using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataExport.Models;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.KataExport.View.Controls;

/// <summary>
/// The cross-section card in the canvas corner, for the column picked in the table: a support is cut just
/// inside the span next to it (its first stirrup station), a span at mid-span. It draws the planned section
/// — concrete b×h, outer hoop, inner stirrups and C ties, every longitudinal bar at that station — and marks
/// the cut on the elevation.
/// </summary>
internal sealed class KataElevationSectionPainter
{
    private const double CardWidth = 200.0;
    private const double CardHeight = 236.0;

    private readonly KataElevationScene _scene;
    private readonly KataRebarPlan _plan;
    private readonly KataStationMap _map;
    private readonly KataCanvasPalette _palette;
    private readonly KataDrawPrimitives _draw;

    public KataElevationSectionPainter(KataElevationScene scene, KataRebarPlan plan, KataStationMap map, KataCanvasPalette palette, KataDrawPrimitives draw)
    {
        _scene = scene ?? throw new ArgumentNullException(nameof(scene));
        _plan = plan ?? throw new ArgumentNullException(nameof(plan));
        _map = map ?? throw new ArgumentNullException(nameof(map));
        _palette = palette ?? throw new ArgumentNullException(nameof(palette));
        _draw = draw ?? throw new ArgumentNullException(nameof(draw));
    }

    public void Paint()
    {
        if (_scene.Width < 420 || _scene.Height < 260) return;

        var cut = Cut();
        if (cut is null) return;
        var (title, localX) = cut.Value;

        PaintMarker(localX);

        double right = _scene.Width - 12.0, left = right - CardWidth, top = 12.0, bottom = top + CardHeight;
        _draw.Box(_palette.SectionCardFill, _palette.GridLine, left, top, right, bottom);
        _draw.Centered(_draw.Text(title, _palette.Accent, KataDrawPrimitives.SmallTextSize, bold: true), (left + right) / 2.0, top + 6.0);

        double b = _plan.Spec.Width, h = DepthAt(localX);
        _draw.Centered(_draw.Text($"{b:0} × {h:0} mm · x = {localX:0}", _palette.MutedText, KataDrawPrimitives.SmallTextSize - 1.0), (left + right) / 2.0, top + 21.0);

        // Section scaled into the card, beam top at z = 0, centre line at y = 0.
        double areaTop = top + 40.0, areaBottom = bottom - 34.0;
        double scale = Math.Min((CardWidth - 40.0) / b, (areaBottom - areaTop) / h);
        double cx = (left + right) / 2.0;
        double y0 = areaTop + ((areaBottom - areaTop) - h * scale) / 2.0;
        // Seen from the drawing's side: when the drawing runs against the plan, +Y is on the left.
        double X(double y) => cx + _map.Direction * y * scale;
        double Y(double z) => y0 - z * scale;

        _draw.Box(_palette.BeamFill, _palette.Outline, X(-b / 2.0), Y(0.0), X(b / 2.0), Y(-h));

        var rules = _plan.Rules;
        double c = rules.StirrupCover + rules.StirrupDiameter / 2.0;
        if (_plan.Layout.StirrupZones.Count > 0)
            _draw.Box(null, _palette.RebarStirrup, X(-b / 2.0 + c), Y(-c), X(b / 2.0 - c), Y(-h + c));

        foreach (var set in _plan.Layout.BarSets.Where(s => Covers(s, localX)))
        {
            var p = set.Shape.Points;
            if (set.WrapEnds && p.Count == 2)
            {
                // A tie round two bars: its straight part a bend radius off them (≈ 1.75 d), a ring round each bar.
                double radius = 1.75 * set.Diameter;
                var (tie, barA, barB) = KataTieWrap.Lay(set, radius);
                var t = tie.Points;
                _draw.Line(_palette.RebarStirrup, X(t[0].Y), Y(t[0].Z), X(t[1].Y), Y(t[1].Z));
                foreach (var bar in new[] { barA, barB })
                    _draw.Circle(null, _palette.RebarStirrup, X(bar.Y), Y(bar.Z), Math.Max(2.0, radius * scale));
                continue;
            }

            for (int i = 0; i + 1 < p.Count; i++)
                _draw.Line(_palette.RebarStirrup, X(p[i].Y), Y(p[i].Z), X(p[i + 1].Y), Y(p[i + 1].Z));
        }

        int topCount = 0, bottomCount = 0, sideCount = 0;
        foreach (var bar in _plan.Layout.LongitudinalBars)
        {
            if (!Crosses(bar, localX, out double z)) continue;
            var (fill, pen) = Appearance(bar);
            _draw.Circle(fill, pen, X(bar.TransverseY), Y(z), Math.Max(2.2, Math.Min(4.5, bar.Diameter / 2.0 * scale)));
            if (bar.Role == KataBarRole.SideBar) sideCount++;
            else if (z > -h / 2.0) topCount++;
            else bottomCount++;
        }

        string summary = $"Trên {topCount} · Dưới {bottomCount}" + (sideCount > 0 ? $" · Giá {sideCount}" : "");
        _draw.Centered(_draw.Text(summary, _palette.RebarText, KataDrawPrimitives.SmallTextSize - 1.0), cx, bottom - 26.0);
    }

    /// <summary>Title and local station of the cut for the selected column; null when nothing is selected.</summary>
    private (string Title, double LocalX)? Cut()
    {
        var columns = _scene.Elevation.Columns;
        int index = _scene.SelectedColumn;
        if (index < 0 || index >= columns.Count) index = columns.ToList().FindIndex(c => c.Kind == KataColumnKind.Span);
        if (index < 0) return null;

        var column = columns[index];
        double a = Local(column.Extent.Start), b = Local(column.Extent.End);
        double lo = Math.Min(a, b), hi = Math.Max(a, b);

        if (column.Kind == KataColumnKind.Span)
            return ($"MẶT CẮT NHỊP {column.SpanNumber?.ToString() ?? column.Letter}", (lo + hi) / 2.0);

        // A support is cut beside it, inside the span that follows it (the last one inside the span before it).
        double end = _plan.Layout.MainTopBars.Concat(_plan.Layout.MainBottomBars).Select(bar => bar.Polyline.Points.Max(p => p.X)).DefaultIfEmpty(hi).Max();
        double offset = _plan.Rules.FirstStirrupOffset;
        double x = hi + offset < end ? hi + offset : lo - offset;
        string grid = string.IsNullOrWhiteSpace(column.Row22) ? column.Letter : $"trục {column.Row22}";
        return ($"MẶT CẮT GỐI {grid}", x);
    }

    private double Local(double station) => (station - _map.Origin) * _map.Direction;

    /// <summary>Depth of the beam at a local station: the span's own (row 21), a support's governing one.</summary>
    private double DepthAt(double localX)
    {
        var st = KataBeamStations.From(_plan.Spec);
        for (int s = 0; s < st.SpanCount; s++)
            if (localX >= st.SpanStart[s] && localX <= st.SpanEnd[s]) return _plan.Spec.DepthOf(s);
        for (int k = 0; k <= st.SpanCount; k++)
            if (localX >= st.SupportStart[k] && localX <= st.SupportEnd[k]) return _plan.Spec.SupportDepth(k);
        return _plan.Spec.Height;
    }

    private void PaintMarker(double localX)
    {
        double sx = _scene.X(_map.ToStation(localX));
        if (!_scene.IsVisible(sx - 2, sx + 2)) return;
        var (topMm, bottomMm) = _scene.BeamFaces(new Interval1D(_map.ToStation(localX), _map.ToStation(localX)));
        _draw.Line(_palette.Marker, sx, _scene.Y(topMm) - 14.0, sx, _scene.Y(bottomMm) + 14.0);
    }

    private static bool Covers(KataBarSet set, double x) =>
        set.Count > 0 && x >= set.Stations[0] - set.Spacing / 2.0 && x <= set.Stations[set.Count - 1] + set.Spacing / 2.0;

    /// <summary>Whether a bar's horizontal run passes the station, and at which height.</summary>
    private static bool Crosses(KataRebarCurve bar, double x, out double z)
    {
        var p = bar.Polyline.Points;
        for (int i = 0; i + 1 < p.Count; i++)
        {
            if (Math.Abs(p[i].Z - p[i + 1].Z) > 1.0) continue;
            if (x >= Math.Min(p[i].X, p[i + 1].X) - 1.0 && x <= Math.Max(p[i].X, p[i + 1].X) + 1.0)
            {
                z = p[i].Z;
                return true;
            }
        }

        z = 0.0;
        return false;
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
