using System;
using System.Collections.Generic;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Continuous top (B11) and bottom (B12) bars from the first to the last support, anchored in both end
/// supports by <see cref="KataAnchorage"/>. Top bars bend down, bottom bars bend up; where both legs of an
/// end would overlap in the same plane the bottom leg moves inboard.
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
        double legRoom = spec.Height - rules.TopBarCentreDepth - rules.BottomBarCentreDepth;
        int last = stations.SpanCount;

        var topStart = TopEnd(rules, stations, 0, dTop, legRoom, spec.Height);
        var topEnd = TopEnd(rules, stations, last, dTop, legRoom, spec.Height);
        var botStart = BottomEnd(spec, rules, stations, 0, dTop, dBot, legRoom, topStart);
        var botEnd = BottomEnd(spec, rules, stations, last, dTop, dBot, legRoom, topEnd);

        if (!spec.TopContinuous.IsEmpty)
        {
            Report(warnings, "trên", "trái", topStart, legRoom);
            Report(warnings, "trên", "phải", topEnd, legRoom);
            double z = -rules.TopBarCentreDepth;
            foreach (double y in Positions(spec, rules, spec.TopContinuous))
                top.Add(Bar(barId++, KataBarRole.MainTop, spec.TopContinuous.Diameter, y, z, -1.0, topStart, topEnd, "1", "Thép chủ trên", 1));
        }

        if (!spec.BottomContinuous.IsEmpty)
        {
            Report(warnings, "dưới", "trái", botStart, legRoom);
            Report(warnings, "dưới", "phải", botEnd, legRoom);
            double z = -spec.Height + rules.BottomBarCentreDepth;
            foreach (double y in Positions(spec, rules, spec.BottomContinuous))
                bottom.Add(Bar(barId++, KataBarRole.MainBottom, spec.BottomContinuous.Diameter, y, z, +1.0, botStart, botEnd, "2", "Thép chủ dưới", 2));
        }

        return (top, bottom);
    }

    private static KataBarEnd TopEnd(KataDetailingRules rules, KataBeamStations st, int support, double d, double legRoom, double height)
    {
        if (st.SupportWidth[support] > 0.0)
            return Solve(st, support, rules.TopBarCentreDepth, rules.TopAnchorageFactor * d, rules.MinimumLegFactor * d, legRoom, 0.0);

        // Cantilever tip: the layout of the earlier version is kept until the console rules are settled —
        // the bar stops at the stirrup cover and hooks down by the compression anchorage, as deep as fits.
        double tip = st.SupportStart[support];
        double x = support == 0 ? tip + rules.StirrupCover : tip - rules.StirrupCover;
        double room = Math.Max(0.0, height - 2.0 * rules.StirrupCover - 2.0 * rules.StirrupDiameter);
        return new KataBarEnd(x, Math.Min(room, Math.Max(rules.BottomAnchorageFactor * d, CantileverHookMinimum)), 0.0);
    }

    private const double CantileverHookMinimum = 200.0;

    private static KataBarEnd BottomEnd(
        KataBeamRebarSpec spec,
        KataDetailingRules rules,
        KataBeamStations st,
        int support,
        double dTop,
        double dBot,
        double legRoom,
        KataBarEnd topEnd)
    {
        // Cantilever: the bottom bars stop at the column face on the cantilever side, as before.
        if (st.SupportWidth[support] <= 0.0)
            return new KataBarEnd(support == 0 ? st.SupportStart[1] : st.SupportEnd[support - 1], 0.0, 0.0);

        double required = rules.BottomAnchorageFactor * dBot;
        double minimumLeg = rules.MinimumLegFactor * dBot;
        var end = Solve(st, support, rules.BottomBarCentreDepth, required, minimumLeg, legRoom, 0.0);

        if (spec.TopContinuous.IsEmpty || !KataAnchorage.LegsOverlap(topEnd, end, legRoom))
            return end;

        double inset = KataAnchorage.BottomLegInset(dTop, dBot, rules.MinimumLegGap);
        return Solve(st, support, rules.BottomBarCentreDepth, required, minimumLeg, legRoom, inset);
    }

    private static KataBarEnd Solve(KataBeamStations st, int support, double cover, double required, double minimumLeg, double legRoom, double inset)
    {
        bool left = support == 0;
        double innerFace = left ? st.SupportEnd[support] : st.SupportStart[support];
        return KataAnchorage.Solve(innerFace, st.SupportWidth[support], left ? -1 : 1, cover, required, minimumLeg, legRoom, inset);
    }

    private static IReadOnlyList<double> Positions(KataBeamRebarSpec spec, KataDetailingRules rules, KataBarItem item) =>
        KataRebarCalculator.ComputeTransverseYPositions(spec.Width, rules.StirrupCover, rules.StirrupDiameter, item.Diameter, item.Count);

    private static void Report(List<string> warnings, string layer, string side, KataBarEnd end, double legRoom)
    {
        if (end.Shortfall > 0.5)
            warnings.Add($"Neo thép chủ {layer} ở gối {side} thiếu {end.Shortfall:0} mm: chân bẻ bị giới hạn {legRoom:0} mm bởi chiều cao dầm.");
    }

    /// <param name="legDirection">−1 for legs bent down (top bars), +1 for legs bent up (bottom bars).</param>
    private static KataRebarCurve Bar(
        int id,
        KataBarRole role,
        double dia,
        double y,
        double z,
        double legDirection,
        KataBarEnd start,
        KataBarEnd end,
        string mark,
        string description,
        int sttCad)
    {
        var points = new List<Point3>();
        if (start.IsBent) points.Add(new Point3(start.X, y, z + legDirection * start.Leg));
        points.Add(new Point3(start.X, y, z));
        points.Add(new Point3(end.X, y, z));
        if (end.IsBent) points.Add(new Point3(end.X, y, z + legDirection * end.Leg));

        string shapeCode = start.IsBent && end.IsBent ? "15a" : start.IsBent || end.IsBent ? "05a" : "00";
        bool bent = start.IsBent || end.IsBent;

        return new KataRebarCurve
        {
            BarId = id,
            Role = role,
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
            ShapeCode = shapeCode,
            BarMark = mark,
            BarDescription = description,
            DimA = end.X - start.X,
            DimB = start.Leg,
            DimC = end.Leg,
            DimR = bent ? 2.0 * dia : 0.0,
            SttCad = sttCad
        };
    }
}
