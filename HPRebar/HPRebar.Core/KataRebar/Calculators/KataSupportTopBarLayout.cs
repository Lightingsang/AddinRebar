using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Additional top bars over the supports (rows 13-16). Row 13 fills the gaps between the main bars on their
/// level, rows 14-16 stack below (<see cref="KataTopLayerStack"/>). A bar reaches into a span by H5 × L (row 13)
/// or H3 × L (rows 14-16) from the support face or centre as I5 / I3 say, L being the larger clear span next to
/// the support. In an end support it anchors like the main bars, its bend inboard of the level above.
/// </summary>
public static class KataSupportTopBarLayout
{
    public static List<KataRebarCurve> Build(
        KataBeamRebarSpec spec,
        KataDetailingRules rules,
        KataBeamStations st,
        List<string> warnings,
        List<string> blocking,
        ref int barId)
    {
        var bars = new List<KataRebarCurve>();
        int last = st.SpanCount;
        double zBottom = -spec.Height + rules.BottomBarCentreDepth;
        double dBottom = spec.BottomContinuous.IsEmpty ? 0.0 : spec.BottomContinuous.Diameter;
        var mainY = spec.TopContinuous.IsEmpty
            ? Array.Empty<double>()
            : KataRebarCalculator.ComputeTransverseYPositions(spec.Width, rules.StirrupCover, rules.StirrupDiameter, spec.TopContinuous.Diameter, spec.TopContinuous.Count);

        for (int k = 0; k <= last; k++)
        {
            if (st.SupportWidth[k] <= 0.0) continue;

            int spanSide = k == 0 ? 1 : k == last ? -1 : 0;
            var levels = KataTopLayerStack.At(spec, rules, k, spanSide);
            foreach (var low in levels.Skip(1))
            {
                double needed = low.Diameter / 2.0 + rules.LayerGap(low.Diameter, dBottom) + dBottom / 2.0;
                if (low.Z - zBottom + 1e-6 < needed)
                    blocking.Add($"{Cell(spec, k, low.Row)}: lớp gia cường cách thép chủ dưới {low.Z - zBottom:0} mm tâm-tâm, cần {needed:0} mm — dầm không đủ cao cho số lớp này.");
            }

            double span = Math.Max(k > 0 ? spec.Spans[k - 1].Length : 0.0, k < last ? spec.Spans[k].Length : 0.0);

            for (int layer = 0; layer < 4; layer++)
            {
                var sides = KataTopLayerStack.Sides(spec, k, layer);
                var level = levels.FirstOrDefault(l => l.Row == KataTopLayerStack.FirstRow + layer);
                string cell = Cell(spec, k, KataTopLayerStack.FirstRow + layer);
                if (sides.IsEmpty)
                {
                    if (!string.IsNullOrWhiteSpace(sides.Text) && sides.Text.Trim() != "0")
                        warnings.Add($"{cell} '{sides.Text}': không đọc được ký hiệu thép — không vẽ.");
                    continue;
                }

                foreach (var token in KataBarNotationParser.UnreadableTokens(sides.Text))
                    warnings.Add($"{cell} '{sides.Text}': không đọc được '{token}' — phần đó không vẽ.");

                if ((spanSide > 0 && sides.Right.Count == 0) || (spanSide < 0 && sides.Left.Count == 0))
                {
                    warnings.Add($"{cell} '{sides.Text}': gối biên chỉ dùng vế phía nhịp, vế đó trống — không vẽ.");
                    continue;
                }

                if (level is null) continue;

                var (leftCut, rightCut) = Cuts(spec, st, k, layer, span);
                // A cut beyond an end support would leave the beam: the bar then stops at that support's far face.
                leftCut = Math.Max(leftCut, st.SupportStart[0] + rules.TopBarCentreDepth);
                rightCut = Math.Min(rightCut, st.SupportEnd[last] - rules.TopBarCentreDepth);
                string mark = $"3.{k + 1}.{layer + 1}";

                if (spanSide > 0)
                    Add(bars, spec, rules, st, warnings, blocking, ref barId, sides.Right, layer, level, mainY, k, mark, Anchor(spec, rules, st, k, level, sides.Right), new KataBarEnd(rightCut, 0.0, 0.0));
                else if (spanSide < 0)
                    Add(bars, spec, rules, st, warnings, blocking, ref barId, sides.Left, layer, level, mainY, k, mark, new KataBarEnd(leftCut, 0.0, 0.0), Anchor(spec, rules, st, k, level, sides.Left));
                else if (sides.IsSymmetric)
                    Add(bars, spec, rules, st, warnings, blocking, ref barId, sides.Left, layer, level, mainY, k, mark, new KataBarEnd(leftCut, 0.0, 0.0), new KataBarEnd(rightCut, 0.0, 0.0));
                else
                {
                    double a = rules.TopBarCentreDepth;
                    Add(bars, spec, rules, st, warnings, blocking, ref barId, sides.Left, layer, level, mainY, k, mark + "T", new KataBarEnd(leftCut, 0.0, 0.0), new KataBarEnd(st.SupportEnd[k] - a, 0.0, 0.0));
                    Add(bars, spec, rules, st, warnings, blocking, ref barId, sides.Right, layer, level, mainY, k, mark + "P", new KataBarEnd(st.SupportStart[k] + a, 0.0, 0.0), new KataBarEnd(rightCut, 0.0, 0.0));
                }
            }
        }

        return bars;
    }

    /// <summary>
    /// End of a bar of <paramref name="level"/> anchored in end support <paramref name="support"/>: G2·d from the
    /// inner face, bent down at the far face (inboard by the level's inset) when the support is too narrow.
    /// </summary>
    public static KataBarEnd Anchor(KataBeamRebarSpec spec, KataDetailingRules rules, KataBeamStations st, int support, KataTopLevel level, IReadOnlyList<KataBarItem> items)
    {
        double d = items.Count == 0 ? level.Diameter : items.Max(i => i.Diameter);
        bool first = support == 0;
        double innerFace = first ? st.SupportEnd[support] : st.SupportStart[support];
        double legRoom = level.Z - (-spec.Height + rules.BottomBarCentreDepth);
        return KataAnchorage.Solve(innerFace, st.SupportWidth[support], first ? -1 : 1, rules.TopBarCentreDepth,
            rules.TopAnchorageFactor * d, rules.MinimumLegFactor * d, legRoom, level.Inset);
    }

    private static string Cell(KataBeamRebarSpec spec, int support, int row) =>
        spec.Supports[support].SheetColumn > 0
            ? KataDamCellAccessorExtensions.ToAddress(row, spec.Supports[support].SheetColumn)
            : $"Gối {support + 1} hàng {row}";

    /// <summary>Stations where a row's bars stop in the spans left and right of support <paramref name="k"/>.</summary>
    private static (double Left, double Right) Cuts(KataBeamRebarSpec spec, KataBeamStations st, int k, int layer, double span)
    {
        bool firstRow = layer == 0;
        double ratio = firstRow ? spec.TopCutoffRatioLayer1 : spec.TopCutoffRatioLayer2;
        if (ratio <= 0.0) ratio = firstRow ? 0.25 : 0.20;
        bool fromCentre = (firstRow ? spec.CutoffOriginLayer1 : spec.CutoffOriginLayer2) == KataCutoffOrigin.FromColumnCenter;

        double reach = ratio * span;
        double left = (fromCentre ? st.SupportCentre(k) : st.SupportStart[k]) - reach;
        double right = (fromCentre ? st.SupportCentre(k) : st.SupportEnd[k]) + reach;
        return (left, right);
    }

    private static void Add(
        List<KataRebarCurve> bars,
        KataBeamRebarSpec spec,
        KataDetailingRules rules,
        KataBeamStations st,
        List<string> warnings,
        List<string> blocking,
        ref int barId,
        IReadOnlyList<KataBarItem> items,
        int layer,
        KataTopLevel level,
        IReadOnlyList<double> mainY,
        int support,
        string mark,
        KataBarEnd start,
        KataBarEnd end)
    {
        int count = items.Sum(i => i.Count);
        if (count <= 0) return;
        if (end.X - start.X < 1.0)
        {
            warnings.Add($"Thép gia cường {mark}: điểm cắt không vượt ra khỏi gối — không vẽ.");
            return;
        }

        double maxD = items.Max(i => i.Diameter);
        double spacingD = layer == 0 && !spec.TopContinuous.IsEmpty ? Math.Max(maxD, spec.TopContinuous.Diameter) : maxD;
        var ys = layer == 0
            ? KataLayerPositions.BetweenMainBars(mainY, count, spec.Width, rules, maxD)
            : KataRebarCalculator.ComputeTransverseYPositions(spec.Width, rules.StirrupCover, rules.StirrupDiameter, maxD, count);
        KataLayerPositions.CheckSpacing(warnings, blocking, mark, layer == 0 ? mainY.Concat(ys) : ys, spacingD, rules);

        foreach (var shortfall in new[] { start.Shortfall, end.Shortfall }.Where(s => s > 0.5))
            warnings.Add($"Neo thép gia cường {mark} thiếu {shortfall:0} mm: chân bẻ bị giới hạn bởi chiều cao dầm.");

        int index = 0;
        foreach (var item in items)
        {
            for (int n = 0; n < item.Count; n++, index++)
                bars.Add(Bar(barId++, item.Diameter, layer, ys[index], level.Z, start, end, support, mark));
        }
    }

    private static KataRebarCurve Bar(int id, double dia, int layer, double y, double z, KataBarEnd start, KataBarEnd end, int support, string mark)
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
            Role = KataBarRole.ExtraTop,
            Diameter = dia,
            Layer = layer + 1,
            Polyline = new Polyline3(points).Simplify(1.0),
            StartHookAngle = start.IsBent ? HookAngle.Hook90 : HookAngle.None,
            EndHookAngle = end.IsBent ? HookAngle.Hook90 : HookAngle.None,
            StartHookLength = start.Leg,
            EndHookLength = end.Leg,
            TransverseY = y,
            HostSupportIndex = support,
            ShapeCode = start.IsBent && end.IsBent ? "15a" : bent ? "05a" : "00",
            BarMark = mark,
            BarDescription = $"Gia cường gối {support + 1} hàng {KataTopLayerStack.FirstRow + layer}",
            DimA = end.X - start.X,
            DimB = start.Leg,
            DimC = end.Leg,
            DimR = bent ? 2.0 * dia : 0.0,
            SttCad = 3
        };
    }
}
