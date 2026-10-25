using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataExport.Web;

/// <summary>
/// Turns a rebar plan into the page's 3D model: every bar the plan counts, grouped by shape so the page draws a
/// stirrup zone or a run of ties as instances of one mesh, and the concrete as boxes. Stirrups are the plan's own
/// 3D curves, keyed by the zone a click on the elevation selects; a set's shape is laid at each of its stations the
/// way the Revit creator lays it.
/// </summary>
public static class KataWeb3dMapper
{
    /// <summary>How far a column is drawn above and below the beam, mm.</summary>
    public const double ColumnStub = 600.0;

    public static KataWeb3dModel Map(KataRebarPlan plan)
    {
        if (plan is null) throw new ArgumentNullException(nameof(plan));

        var layout = plan.Layout;
        var groups = new List<KataWeb3dGroup>();
        groups.AddRange(layout.LongitudinalBars.Select(b => Group(KataLayoutRemoval.Key(b), b.Role, b.Diameter, b.BarNumber, b.Polyline, new[] { 0.0 })));
        groups.AddRange(Stirrups(layout));
        groups.AddRange(layout.BarSets.Where(s => s.Count > 0 && s.Shape.Points.Count > 0).Select(Set));

        var stations = KataBeamStations.From(plan.Spec);
        return new KataWeb3dModel(groups, Concrete(plan.Spec, stations), R(stations.TotalLength), layout.TotalBarCount);
    }

    /// <summary>The plan's single stirrups, one group per zone and shape (a zone cut at a top step has two).</summary>
    private static IEnumerable<KataWeb3dGroup> Stirrups(KataRebarLayoutResult layout)
    {
        var byShape = new Dictionary<string, (KataRebarCurve First, string Key, List<double> Offsets)>(StringComparer.Ordinal);
        var order = new List<string>();
        foreach (var stirrup in layout.IndividualStirrups.Where(s => s.Polyline.Points.Count > 0))
        {
            double x = stirrup.Polyline.Points[0].X;
            var zone = layout.StirrupZones.FirstOrDefault(z =>
                (stirrup.HostSpanIndex < 0 || stirrup.HostSpanIndex == z.SpanIndex) && z.Stations.Any(s => Math.Abs(s - x) < 0.5));
            string key = zone is null ? "" : KataLayoutRemoval.Key(zone);
            string id = key + "#" + Signature(stirrup.Polyline, x);
            if (byShape.TryGetValue(id, out var group))
            {
                group.Offsets.Add(x - group.First.Polyline.Points[0].X);
                continue;
            }

            byShape[id] = (stirrup, key, new List<double> { 0.0 });
            order.Add(id);
        }

        return order.Select(id =>
        {
            var (first, key, offsets) = byShape[id];
            return Group(key, first.Role, first.Diameter, first.BarNumber, first.Polyline, offsets);
        });
    }

    private static KataWeb3dGroup Set(KataBarSet set)
    {
        // Ties around a pair of side bars wrap them: the creator draws the wrapped shape (KataBarSetCreator).
        var shape = set.WrapEnds && set.Shape.Points.Count == 2 ? KataTieWrap.Lay(set, 0.0).Shape : set.Shape;
        double x0 = shape.Points[0].X;
        return Group("", set.Role, set.Diameter, set.BarNumber, shape, set.Stations.Select(s => s - x0).ToList());
    }

    private static KataWeb3dGroup Group(string key, KataBarRole role, double diameter, int number, Polyline3 shape, IReadOnlyList<double> offsets) =>
        new(key, role, diameter, number, shape.Points.Select(p => new KataWebPoint3(R(p.X), R(p.Y), R(p.Z))).ToList(), shape.IsClosed,
            offsets.Select(R).ToList());

    /// <summary>The shape relative to its first point's X, to 0.1 mm: equal for every stirrup of a zone.</summary>
    private static string Signature(Polyline3 shape, double x0) =>
        string.Join(";", shape.Points.Select(p => string.Format(CultureInfo.InvariantCulture, "{0:0.0},{1:0.0},{2:0.0}", p.X - x0, p.Y, p.Z)))
        + (shape.IsClosed ? "|c" : "");

    private static List<KataWeb3dBox> Concrete(KataBeamRebarSpec spec, KataBeamStations st)
    {
        var boxes = new List<KataWeb3dBox>();
        for (int i = 0; i < st.SpanCount; i++)
        {
            double half = spec.WidthOf(i) / 2.0, soffit = -spec.DepthOf(i);
            double start = st.SpanStart[i], end = st.SpanEnd[i];
            var cuts = new List<double> { 0.0 };
            if (i < spec.Spans.Count) cuts.AddRange(spec.Spans[i].TopSteps.Select(s => s.AtMm).Where(a => a > 0.0 && a < end - start).OrderBy(a => a));
            cuts.Add(end - start);
            for (int k = 0; k + 1 < cuts.Count; k++)
            {
                double top = spec.TopAt(i, cuts[k]);
                if (cuts[k + 1] - cuts[k] > 0.5)
                    boxes.Add(Box(start + cuts[k], start + cuts[k + 1], half, soffit, top, KataWeb3dBoxKind.Beam));
            }
        }

        for (int k = 0; k <= st.SpanCount; k++)
        {
            if (st.SupportWidth[k] <= 0.0) continue;
            double half = Math.Max(spec.WidthOf(Math.Max(0, k - 1)), spec.WidthOf(Math.Min(st.SpanCount - 1, k))) / 2.0;
            double depth = Math.Max(spec.DepthOf(Math.Max(0, k - 1)), spec.DepthOf(Math.Min(st.SpanCount - 1, k)));
            boxes.Add(Box(st.SupportStart[k], st.SupportEnd[k], half, -depth - ColumnStub, ColumnStub, KataWeb3dBoxKind.Column));
        }

        return boxes;
    }

    private static KataWeb3dBox Box(double x1, double x2, double halfWidth, double z1, double z2, KataWeb3dBoxKind kind) =>
        new(R(x1), R(x2), R(-halfWidth), R(halfWidth), R(z1), R(z2), kind);

    private static double R(double value) => Math.Round(value, 1);
}
