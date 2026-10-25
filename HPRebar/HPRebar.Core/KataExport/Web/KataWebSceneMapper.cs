using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataExport.Models;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataExport.Web;

/// <summary>
/// Turns the elevation and the rebar plan into the records the Kata Export page draws. Builds Kata's elevation, bar
/// tags and section cuts with the same builders the drawings use, so the page shows exactly what is generated; a
/// section is built only when the page asks for it. Lengths are rounded to 0.1 mm (the page cannot show finer).
/// </summary>
public static class KataWebSceneMapper
{
    public static KataWebScene Map(KataElevation elevation, KataRebarPlan? plan, KataStationMap stationMap)
    {
        if (elevation is null) throw new ArgumentNullException(nameof(elevation));
        if (stationMap is null) throw new ArgumentNullException(nameof(stationMap));

        return new KataWebScene(
            MapElevation(elevation),
            plan is null ? null : MapRebar(plan),
            new KataWebStationMap(R(stationMap.Origin), stationMap.Direction));
    }

    /// <summary>Section <paramref name="number" /> of <paramref name="plan" />; null when the plan has no cut with that number.</summary>
    /// <param name="mirror">Seen from the other side: the elevation lists the run backwards.</param>
    public static KataWebSection? MapSection(KataRebarPlan plan, int number, bool mirror)
    {
        if (plan is null) throw new ArgumentNullException(nameof(plan));

        var cut = KataSectionCuts.Build(plan.Spec, plan.Layout).FirstOrDefault(c => c.Number == number);
        if (cut is null) return null;

        var s = KataSectionDrawingBuilder.Build(plan.Spec, plan.Layout, plan.Rules, cut, mirror);
        return new KataWebSection(
            s.Number,
            mirror,
            s.Lines.Select(l => new KataWebPolyline(l.Pen, l.Vertices.Select(v => new KataWebVertex(R(v.X), R(v.Z), v.Bulge)).ToList())).ToList(),
            s.Bars.Select(b => new KataWebSectionBar(R(b.X), R(b.Z), b.Diameter, b.Number)).ToList(),
            s.Leaders.Select(l => new KataWebLeader(Points(l.Points), l.Arrow, R(l.ArrowSize))).ToList(),
            s.Marks.Select(m => new KataWebMark(R(m.X), R(m.Z), R(m.Radius))).ToList(),
            s.Tags.Select(t => new KataWebSectionTag(R(t.X), R(t.Z), t.PointsRight, t.Text, t.Numbers, t.Spacing, t.Layout, t.BlockState)).ToList(),
            s.Dims.Select(Dim).ToList(),
            Title(s.Title),
            R(s.MinX), R(s.MaxX), R(s.Top), R(s.Bottom));
    }

    private static KataWebElevation MapElevation(KataElevation e) => new(
        e.Columns.Select(c => new KataWebColumn(c.Index, c.Letter, c.Kind, Range(c.Extent), c.Row11, c.Row19, c.Row20, c.Row21, c.Row22, c.Row23,
            c.SpanNumber, c.SpanSection)).ToList(),
        e.Beams.Select(b => new KataWebBeam(Range(b.Extent), R(b.TopMm), R(b.BottomMm), R(b.HeightMm))).ToList(),
        e.Supports.Select(s => new KataWebSupport(s.ColumnIndex, s.Kind, Range(s.Extent), R(s.SectionHeightMm),
            s.Upper is { } upper ? new KataWebUpper(Range(upper.Extent), upper.Text) : null, R(s.CrossingBeamX))).ToList(),
        e.Grids.Select(g => new KataWebGrid(g.Name, R(g.X), g.OffsetText)).ToList(),
        e.GridDimensions.Select(d => new KataWebDimension(Range(d.Extent), d.Text)).ToList(),
        Range(e.Bounds),
        R(e.TopMm),
        R(e.BottomMm),
        R(e.MaxBeamHeightMm));

    private static KataWebRebar MapRebar(KataRebarPlan plan)
    {
        double stirrup = plan.Rules.StirrupDiameter;
        var tags = KataBarTagBuilder.Build(plan.Spec, plan.Layout, stirrup);
        var cuts = KataSectionCuts.Build(plan.Spec, plan.Layout);
        var d = KataElevationDrawingBuilder.Build(plan.Spec, plan.Layout, cuts, stirrup, KataBarTagBuilder.StirrupRowOf(tags));

        return new KataWebRebar(
            d.Lines.Select(l => new KataWebLine(l.Pen, Points(l.Points), l.Keys ?? Array.Empty<string>())).ToList(),
            d.Dims.Select(Dim).ToList(),
            d.Flags.Select(f => new KataWebFlag(R(f.X), R(f.Z), f.Number, f.Below)).ToList(),
            d.Bubbles.Select(b => new KataWebBubble(R(b.X), R(b.Z), b.Name)).ToList(),
            d.Level is { } level ? new KataWebLevel(R(level.X), R(level.Z), level.Text) : null,
            d.Title is { } title ? Title(title) : null,
            R(d.MinX), R(d.MaxX), R(d.Top), R(d.Bottom),
            tags.Select(t => new KataWebTag(t.Numbers, t.Text, R(t.X), R(t.RowZ), t.Above, t.PointsLeft, t.FootZ.Select(R).ToList(),
                R(t.LeaderLength), R(t.InsertX), t.Kind)).ToList(),
            cuts.Select(c => new KataWebCut(c.Number, R(c.X), c.SpanIndex)).ToList(),
            plan.Layout.TotalBarCount,
            Math.Round(plan.Layout.TotalSteelWeightKg, 1));
    }

    private static KataWebDim Dim(KataDrawingDim d) => new(R(d.X1), R(d.Z1), R(d.X2), R(d.Z2), d.Vertical, R(d.LineAt), d.Style, d.Text);

    private static KataWebTitle Title(KataDrawingTitle t) => new(R(t.X), R(t.Z), t.Name, t.Scale);

    private static IReadOnlyList<KataWebPoint> Points(IReadOnlyList<(double X, double Z)> points) =>
        points.Select(p => new KataWebPoint(R(p.X), R(p.Z))).ToList();

    private static KataWebRange Range(Interval1D extent) => new(R(extent.Start), R(extent.End));

    private static double R(double value) => Math.Round(value, 1);

    private static double? R(double? value) => value is { } v ? R(v) : null;
}
