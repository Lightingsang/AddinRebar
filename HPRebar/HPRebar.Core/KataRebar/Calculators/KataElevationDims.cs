using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// The dimensions of Kata's elevation (T2-DY7.dwg, DY7 and DY14), every chain a run of touching dimensions between
/// consecutive stations:
/// <list type="bullet">
/// <item>over the beam: support widths and each span split where its first stirrup zone ends and its last begins;</item>
/// <item>under it: the run's ends, the grids, the support faces and where the innermost additional bottom bars stop;</item>
/// <item>under that: grid to grid;</item>
/// <item>the stagger of the additional bars' two layers where both end free, over the layer-1 bar;</item>
/// <item>left of the beam: its depth, and the slab and the rest;</item>
/// <item>each stirrup zone from its first stirrup to its last, without text (kata_rai_thep), at the slab soffit.</item>
/// </list>
/// </summary>
internal static class KataElevationDims
{
    private const double Same = 1.0;

    public static IEnumerable<KataDrawingDim> Build(KataDrawingFrame f, KataRebarLayoutResult layout, double stirrupDiameter)
    {
        var st = f.St;
        if (f.SpanCount == 0) yield break;
        var runs = KataStirrupRuns.Of(layout);

        // Over the beam.
        var top = new List<double> { 0.0, f.Length };
        for (int k = 0; k < f.SupportCount; k++)
        {
            top.Add(st.SupportStart[k]);
            top.Add(st.SupportEnd[k]);
        }

        for (int i = 0; i < f.SpanCount; i++) top.AddRange(ZoneSplits(runs, i));
        foreach (var d in Chain(top, KataDrawingStyle.TopChainZ + f.Lift, _ => 0.0)) yield return d;

        // Under the beam: bar cuts, then grids.
        var bottom = new List<double> { 0.0, f.Length };
        bottom.AddRange(f.Grids());
        for (int k = 0; k < f.SupportCount; k++)
        {
            bottom.Add(st.SupportStart[k]);
            bottom.Add(st.SupportEnd[k]);
        }

        for (int i = 0; i < f.SpanCount; i++)
        {
            var extra = layout.ExtraBottomBars.Where(b => b.HostSpanIndex == i && b.Polyline.Points.Count > 1).ToList();
            if (extra.Count == 0) continue;
            bottom.Add(extra.Max(b => b.Polyline.Points.Min(p => p.X)));
            bottom.Add(extra.Min(b => b.Polyline.Points.Max(p => p.X)));
        }

        foreach (var d in Chain(bottom, f.StubBottom - KataDrawingStyle.BottomChainBelow, f.SoffitNear)) yield return d;
        foreach (var d in Chain(f.Grids().ToList(), f.StubBottom - KataDrawingStyle.AxisChainBelow, f.SoffitNear)) yield return d;

        // Stagger of the additional bars.
        foreach (var group in layout.ExtraTopBars.GroupBy(b => b.HostSupportIndex))
            foreach (var d in Stagger(group.ToList(), stirrupDiameter)) yield return d;
        foreach (var group in layout.ExtraBottomBars.GroupBy(b => b.HostSpanIndex))
            foreach (var d in Stagger(group.ToList(), stirrupDiameter)) yield return d;

        // Depth, left of the beam.
        double soffit = f.Soffit(0);
        yield return new KataDrawingDim(0.0, 0.0, 0.0, soffit, true, KataDrawingStyle.DepthDimX);
        if (f.Slab > 0.0 && f.Slab < -soffit)
        {
            yield return new KataDrawingDim(0.0, 0.0, 0.0, -f.Slab, true, KataDrawingStyle.SlabDimX);
            yield return new KataDrawingDim(0.0, -f.Slab, 0.0, soffit, true, KataDrawingStyle.SlabDimX);
        }

        // Stirrup runs.
        double runZ = -(f.Slab > 0.0 ? f.Slab : KataDrawingStyle.DefaultSlab);
        foreach (var run in runs.Where(r => r.Last - r.First > Same))
            yield return new KataDrawingDim(run.First, runZ, run.Last, runZ, false, runZ, KataDimStyle.Run);
    }

    /// <summary>
    /// Where Kata's top chain splits span <paramref name="span"/>: at the last stirrup of its first zone, then at the first
    /// stirrup of each later zone (three zones: 1400 / 2700 / 1400 in DY7's first span; one zone: none).
    /// </summary>
    private static IEnumerable<double> ZoneSplits(IReadOnlyList<KataStirrupRun> runs, int span)
    {
        var zones = runs.Where(r => r.Span == span).OrderBy(r => r.First).ToList();
        for (int j = 0; j + 1 < zones.Count; j++)
        {
            // Two zones of different stirrups (B01 at the zero-width joint I: H's 27 a200 ends 20550, J's 26 a200
            // starts 20650) meet half way between them: Kata splits the chain at 20600.
            if (j > 0 && zones[j].Number != zones[j + 1].Number) yield return (zones[j].Last + zones[j + 1].First) / 2.0;
            else yield return j == 0 ? zones[0].Last : zones[j + 1].First;
        }
    }

    /// <summary>Touching dimensions between the distinct <paramref name="stations"/>, their line at <paramref name="lineZ"/>.</summary>
    private static IEnumerable<KataDrawingDim> Chain(IReadOnlyCollection<double> stations, double lineZ, Func<double, double> originZ)
    {
        var xs = new List<double>();
        foreach (double x in stations.OrderBy(x => x))
            if (xs.Count == 0 || x - xs[xs.Count - 1] > Same) xs.Add(x);
        for (int i = 0; i + 1 < xs.Count; i++)
        {
            double mid = (xs[i] + xs[i + 1]) / 2.0;
            yield return new KataDrawingDim(xs[i], originZ(mid), xs[i + 1], originZ(mid), false, lineZ);
        }
    }

    /// <summary>
    /// The additional bars of one support (or span): on each side, where the layer-1 and layer-2 bars reaching furthest
    /// that way both end free (no hook), a dimension between those ends on the layer-1 bar as drawn, its line
    /// <see cref="KataDrawingStyle.StaggerDimAbove"/> over it.
    /// </summary>
    private static IEnumerable<KataDrawingDim> Stagger(IReadOnlyList<KataRebarCurve> bars, double stirrupDiameter)
    {
        foreach (bool left in new[] { true, false })
        {
            var one = Outermost(bars, 1, left);
            var two = Outermost(bars, 2, left);
            if (one is null || two is null) continue;

            var (x1, z1, free1) = End(one, left);
            var (x2, _, free2) = End(two, left);
            if (!free1 || !free2 || Math.Abs(x1 - x2) <= Same) continue;
            double z = KataDrawingLevels.Drawn(one, z1, stirrupDiameter);
            yield return new KataDrawingDim(x1, z, x2, z, false, z + KataDrawingStyle.StaggerDimAbove);
        }
    }

    /// <summary>The bar of <paramref name="layer"/> whose end reaches furthest left (or right).</summary>
    private static KataRebarCurve? Outermost(IReadOnlyList<KataRebarCurve> bars, int layer, bool left)
    {
        var candidates = bars.Where(b => b.Layer == layer && b.Polyline.Points.Count > 1).ToList();
        if (candidates.Count == 0) return null;
        return left ? candidates.OrderBy(b => End(b, true).X).First() : candidates.OrderByDescending(b => End(b, false).X).First();
    }

    /// <summary>The bar's left (or right) end: station, height, and whether it ends without a hook.</summary>
    private static (double X, double Z, bool Free) End(KataRebarCurve bar, bool left)
    {
        var p = bar.Polyline.Points;
        bool forward = p[0].X <= p[p.Count - 1].X;
        bool atStart = left == forward;
        var end = atStart ? p[0] : p[p.Count - 1];
        var hook = atStart ? bar.StartHookAngle : bar.EndHookAngle;
        return (end.X, end.Z, hook == HPRebar.Core.BeamRebar.Models.HookAngle.None);
    }
}
