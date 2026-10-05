using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Main bars. The top bars (B11) run from the first to the last support — the top of the beam is level — and
/// anchor in both end supports by <see cref="KataAnchorage"/>, bending down. The bottom bars (B12) follow each
/// span's soffit: neighbouring spans share one bar cranked across the support when Kata's beam-node rule allows it,
/// otherwise the step cuts them there (<see cref="KataBottomMainBarRuns"/>).
/// Each bar is one piece however long: splitting into stock lengths is shop-drawing work. Where a bottom leg would
/// overlap a top leg in an end support it moves inboard.
/// </summary>
public static class KataMainBarLayout
{
    public static (List<KataRebarCurve> Top, List<KataRebarCurve> Bottom) Build(
        KataBeamRebarSpec spec,
        KataDetailingRules rules,
        KataBeamStations stations,
        List<string> warnings,
        ref int barId)
    {
        var top = new List<KataRebarCurve>();
        var bottom = new List<KataRebarCurve>();

        double dTop = spec.TopContinuous.IsEmpty ? 0.0 : spec.TopContinuous.Diameter;
        double dBot = spec.BottomContinuous.IsEmpty ? 0.0 : spec.BottomContinuous.Diameter;
        int last = stations.SpanCount;

        var topStart = TopEnd(spec, rules, stations, 0, dTop);
        var topEnd = TopEnd(spec, rules, stations, last, dTop);

        if (!spec.TopContinuous.IsEmpty)
        {
            Report(warnings, "trên", "trái", topStart, LegRoom(spec, rules, 0));
            Report(warnings, "trên", "phải", topEnd, LegRoom(spec, rules, last));
            double z = -rules.TopBarCentreDepth;
            foreach (double y in Positions(spec, rules, spec.TopContinuous))
                top.Add(TopBar(barId++, spec.TopContinuous.Diameter, y, z, topStart, topEnd));
        }

        if (!spec.BottomContinuous.IsEmpty)
        {
            var ends = new KataBottomMainBarRuns.EndSolver(
                support => BottomEnd(spec, rules, stations, support, dTop, dBot, support == 0 ? topStart : topEnd),
                (support, outward, room, d) => Solve(stations, rules, support, outward, rules.BottomEndCover,
                    rules.BottomAnchorageFactor * d, rules.MinimumLegFactor * d, room, 0.0),
                support => LowestTopCentre(spec, rules, support) - BottomLegClearance(spec, rules, support, dBot));

            double BarsOf(int span) => spec.BottomMainOf(span).IsEmpty ? dBot : spec.BottomMainOf(span).Diameter;
            foreach (var run in KataBottomMainBarRuns.Plan(spec, rules, stations, dBot, ends, warnings, BarsOf))
            {
                foreach (var (end, support, side) in new[] { (run.Start, run.FirstSupport, "trái"), (run.End, run.LastSupport, "phải") })
                    Report(warnings, "dưới", $"{side} (gối {support + 1})", end, LegRoom(spec, rules, support));

                foreach (double y in Positions(spec, rules, spec.BottomContinuous))
                    bottom.Add(KataBottomMainBarRuns.Bar(barId++, spec.BottomContinuous.Diameter, y, run));
            }
        }

        return (top, bottom);
    }

    /// <summary>Centre of the lowest top level over a support: the main bars' or an additional row's (z, mm).</summary>
    private static double LowestTopCentre(KataBeamRebarSpec spec, KataDetailingRules rules, int support) =>
        KataTopLayerStack.At(spec, rules, support, 0).Min(l => l.Z);

    /// <summary>Centre-to-centre gap a bottom leg keeps under that lowest top level.</summary>
    private static double BottomLegClearance(KataBeamRebarSpec spec, KataDetailingRules rules, int support, double dBot)
    {
        var lowest = KataTopLayerStack.At(spec, rules, support, 0).OrderBy(l => l.Z).First();
        double dTop = Math.Max(lowest.Diameter, spec.TopContinuous.IsEmpty ? 0.0 : spec.TopContinuous.Diameter);
        return (dTop + dBot) / 2.0 + rules.LayerGap(dTop, dBot);
    }

    /// <summary>Longest leg between the main bar layers over support <paramref name="support"/> (mm).</summary>
    internal static double LegRoom(KataBeamRebarSpec spec, KataDetailingRules rules, int support) =>
        spec.SupportDepth(support) - rules.TopBarCentreDepth - rules.BottomBarCentreDepth;

    private static KataBarEnd TopEnd(KataBeamRebarSpec spec, KataDetailingRules rules, KataBeamStations st, int support, double d)
    {
        if (st.SupportWidth[support] > 0.0)
            return Solve(st, rules, support, support == 0 ? -1 : 1, rules.TopEndCover, rules.TopAnchorageFactor * d,
                rules.MinimumLegFactor * d, LegRoom(spec, rules, support), 0.0);

        // Console tip, as Kata draws B01: the bar stops a (J9) short of the tip and bends down all the way to the
        // bottom bars' level of the console's own depth (under its own top, row 19).
        double tip = st.SupportStart[support];
        double x = support == 0 ? tip + rules.TopEndCover : tip - rules.TopEndCover;
        int span = support == 0 ? 0 : support - 1;
        double atTip = support == 0 ? 0.0 : spec.Spans[span].Length;
        double dT = spec.TopMainOf(span).IsEmpty ? d : spec.TopMainOf(span).Diameter;
        double dB = spec.BottomMainOf(span).IsEmpty ? 0.0 : spec.BottomMainOf(span).Diameter;
        // Centre to centre once the span's own bars took their place, each outer face kept on the stirrup.
        double shifts = ((d - dT) + (spec.BottomContinuous.Diameter - dB)) / 2.0;
        double clear = dB > 0.0 ? (dT + dB) / 2.0 + Math.Max(dT, dB) - shifts : 0.0;
        double room = Math.Max(0.0, spec.HeightOf(span, atTip) - rules.TopBarCentreDepth - rules.BottomBarCentreDepth - clear);
        return new KataBarEnd(x, room, 0.0);
    }

    /// <summary>Bottom bar end in an end support, its leg moved inboard of the top legs it would overlap.</summary>
    private static KataBarEnd BottomEnd(
        KataBeamRebarSpec spec,
        KataDetailingRules rules,
        KataBeamStations st,
        int support,
        double dTop,
        double dBot,
        KataBarEnd topEnd)
    {
        // Cantilever: the bottom bars stop at the column face on the cantilever side, as before.
        if (st.SupportWidth[support] <= 0.0)
            return new KataBarEnd(support == 0 ? st.SupportStart[1] : st.SupportEnd[support - 1], 0.0, 0.0);

        int outward = support == 0 ? -1 : 1;
        double required = rules.BottomAnchorageFactor * dBot;
        double minimumLeg = rules.MinimumLegFactor * dBot;
        double legRoom = LegRoom(spec, rules, support);
        var end = Solve(st, rules, support, outward, rules.BottomEndCover, required, minimumLeg, legRoom, 0.0);

        // The bottom leg moves inboard of the innermost top leg it would overlap: the main bars' or an
        // additional level's, whose bends already sit inboard by the level's inset.
        double zBottom = -spec.SupportDepth(support) + rules.BottomBarCentreDepth;
        var tops = TopEndsAt(spec, rules, st, support, dTop, topEnd).ToList();
        double inset = 0.0;
        // A leg moved inboard gets longer and may then reach a deeper top level: check once more with it.
        for (int pass = 0; pass < 2; pass++)
        {
            foreach (var (level, top) in tops)
            {
                if (KataAnchorage.LegsOverlap(top, end, level.Z - zBottom))
                    inset = Math.Max(inset, level.Inset + KataAnchorage.BottomLegInset(level.Diameter, dBot, rules.MinimumLegGap));
            }

            if (inset > 0.0) end = Solve(st, rules, support, outward, rules.BottomEndCover, required, minimumLeg, legRoom, inset);
        }

        return end;
    }

    /// <summary>The top bends in an end support: the main bars' and those of each additional level on the span side.</summary>
    private static IEnumerable<(KataTopLevel Level, KataBarEnd End)> TopEndsAt(
        KataBeamRebarSpec spec, KataDetailingRules rules, KataBeamStations st, int support, double dTop, KataBarEnd mainEnd)
    {
        bool first = support == 0;
        var levels = KataTopLayerStack.At(spec, rules, support, first ? 1 : -1);
        if (!spec.TopContinuous.IsEmpty)
            yield return (levels[0] with { Diameter = dTop }, mainEnd);

        foreach (var level in levels)
        {
            var bars = KataTopLayerStack.Sides(spec, support, level.Row - KataTopLayerStack.FirstRow).Side(first);
            if (bars.Count > 0)
                yield return (level, KataSupportTopBarLayout.Anchor(spec, rules, st, support, level, bars));
        }
    }

    /// <param name="outward">−1 when the support lies before the bar (smaller stations), +1 after it.</param>
    internal static KataBarEnd Solve(KataBeamStations st, KataDetailingRules rules, int support, int outward, double cover, double required, double minimumLeg, double legRoom, double inset)
    {
        double innerFace = outward < 0 ? st.SupportEnd[support] : st.SupportStart[support];
        return KataAnchorage.Solve(innerFace, st.SupportWidth[support], outward, cover, required, minimumLeg, legRoom, inset, rules.RoundLegMm);
    }

    private static IReadOnlyList<double> Positions(KataBeamRebarSpec spec, KataDetailingRules rules, KataBarItem item) =>
        KataRebarCalculator.ComputeTransverseYPositions(spec.Width, rules.StirrupCover, rules.StirrupDiameter, item.Diameter, item.Count);

    private static void Report(List<string> warnings, string layer, string side, KataBarEnd end, double legRoom)
    {
        if (end.Shortfall > 0.5)
            warnings.Add($"Neo thép chủ {layer} ở gối {side} thiếu {end.Shortfall:0} mm: chân bẻ bị giới hạn {legRoom:0} mm bởi chiều cao dầm.");
    }

    private static KataRebarCurve TopBar(int id, double dia, double y, double z, KataBarEnd start, KataBarEnd end)
    {
        var points = new List<Point3>();
        if (start.IsBent) points.Add(new Point3(start.X, y, z - start.Leg));
        points.Add(new Point3(start.X, y, z));
        points.Add(new Point3(end.X, y, z));
        if (end.IsBent) points.Add(new Point3(end.X, y, z - end.Leg));

        bool bent = start.IsBent || end.IsBent;
        return new KataRebarCurve
        {
            BarId = id,
            Role = KataBarRole.MainTop,
            Diameter = dia,
            Layer = 1,
            Polyline = new Polyline3(points).Simplify(1.0),
            StartHookAngle = start.IsBent ? HookAngle.Hook90 : HookAngle.None,
            EndHookAngle = end.IsBent ? HookAngle.Hook90 : HookAngle.None,
            StartHookLength = start.Leg,
            EndHookLength = end.Leg,
            TransverseY = y,
            HostSpanIndex = -1,
            HostSupportIndex = -1,
            ShapeCode = start.IsBent && end.IsBent ? "15a" : bent ? "05a" : "00",
            BarMark = "1",
            BarDescription = "Thép chủ trên",
            DimA = end.X - start.X,
            DimB = start.Leg,
            DimC = end.Leg,
            DimR = bent ? 2.0 * dia : 0.0,
            SttCad = 1
        };
    }
}
