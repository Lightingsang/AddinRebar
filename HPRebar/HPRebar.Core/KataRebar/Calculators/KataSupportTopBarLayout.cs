using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Additional top bars over the supports (rows 13-16). Row 13 fills the gaps between the main bars on their
/// level, rows 14-16 stack below (<see cref="KataTopLayerStack"/>). Every row reaches into a span by H5 × L from the
/// support face or centre as I5 says, L being that span's clear length (each side its own span, as Kata draws it);
/// <see cref="KataTopBarStagger"/> then pushes each outer row G1 past the row inside it. A "-" over the
/// neighbouring support continues the row there (<see cref="KataTopBarContinuation"/>). In an end support a bar
/// anchors like the main bars, its bend inboard of the level above.
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
        double dBottom = spec.BottomContinuous.IsEmpty ? 0.0 : spec.BottomContinuous.Diameter;
        var mainY = spec.TopContinuous.IsEmpty
            ? Array.Empty<double>()
            : KataRebarCalculator.ComputeTransverseYPositions(spec.Width, rules.StirrupCover, rules.StirrupDiameter, spec.TopContinuous.Diameter, spec.TopContinuous.Count);

        for (int k = 0; k <= last; k++)
        {
            if (st.SupportWidth[k] <= 0.0) continue;

            int spanSide = k == 0 ? 1 : k == last ? -1 : 0;
            double zBottom = -spec.SupportDepth(k) + rules.BottomBarCentreDepth;
            var levels = KataTopLayerStack.At(spec, rules, k, spanSide);
            foreach (var low in levels.Skip(1))
            {
                double needed = low.Diameter / 2.0 + rules.LayerGap(low.Diameter, dBottom) + dBottom / 2.0;
                if (low.Z - zBottom + 1e-6 < needed)
                    blocking.Add($"{Cell(spec, k, low.Row)}: lớp gia cường cách thép chủ dưới {low.Z - zBottom:0} mm tâm-tâm, cần {needed:0} mm — dầm không đủ cao cho số lớp này.");
            }

            var rows = new List<TopRow>();
            for (int layer = 0; layer < 4; layer++)
            {
                var sides = KataTopLayerStack.Sides(spec, k, layer);
                var level = levels.FirstOrDefault(l => l.Row == KataTopLayerStack.FirstRow + layer);
                string cell = Cell(spec, k, KataTopLayerStack.FirstRow + layer);
                if (sides.IsEmpty)
                {
                    // "0" is nothing, "-" in row 13 continues the neighbouring support's row (handled from there).
                    if (KataTopBarContinuation.IsMark(spec, k, layer) && layer > 0)
                        warnings.Add($"{cell} '-': nối tiếp thanh gối bên cạnh mới hỗ trợ ở hàng 13 — hàng này không vẽ.");
                    else if (!string.IsNullOrWhiteSpace(sides.Text) && sides.Text.Trim() is not ("0" or KataTopBarContinuation.Mark))
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

                var (leftCut, rightCut) = Cuts(spec, rules, st, k);
                // A cut beyond an end support would leave the beam: the bar then stops at that support's far face.
                leftCut = Math.Max(leftCut, st.SupportStart[0] + rules.TopEndCover);
                rightCut = Math.Min(rightCut, st.SupportEnd[last] - rules.TopEndCover);
                // Into a console they run on to its tip, as the main bars do.
                if (k == last - 1 && st.IsRightCantilever) rightCut = st.SupportEnd[last] - rules.TopEndCover;
                if (k == 1 && st.IsLeftCantilever) leftCut = st.SupportStart[0] + rules.TopEndCover;
                var (leftThrough, rightThrough) = spanSide == 0 && !sides.IsSymmetric ? RunThrough(rules, st, k, sides) : (null, null);
                rows.Add(new TopRow(layer, sides, level, cell, leftCut, rightCut,
                    ReachesLeft: spanSide < 0 || (spanSide == 0 && sides.Left.Count > 0),
                    ReachesRight: spanSide > 0 || (spanSide == 0 && sides.Right.Count > 0),
                    leftThrough, rightThrough));
            }

            KataTopBarStagger.Apply(rows, st, rules, k, warnings);

            foreach (var row in rows)
            {
                var (layer, sides, level, _, leftCut, rightCut, _, _, leftThrough, rightThrough) = row;
                string mark = $"3.{k + 1}.{layer + 1}";
                var leftOut = KataTopBarContinuation.Left(spec, rules, st, k, layer, level, sides.Left, leftCut);
                var rightOut = KataTopBarContinuation.Right(spec, rules, st, k, layer, level, sides.Right, rightCut);
                if (layer == 0 && KataTopBarContinuation.SharedChainEnd(spec, st, k) is int other)
                    warnings.Add($"{Cell(spec, k, KataTopLayerStack.FirstRow)} và {Cell(spec, other, KataTopLayerStack.FirstRow)}: cả hai cùng kéo qua các gối '-' giữa chúng — hai thanh hàng 13 chồng nhau, kiểm tra ô '-'.");

                if (spanSide > 0)
                    Add(bars, spec, rules, st, warnings, blocking, ref barId, sides.Right, layer, level, mainY, k, mark, Anchor(spec, rules, st, k, level, sides.Right), rightOut);
                else if (spanSide < 0)
                    Add(bars, spec, rules, st, warnings, blocking, ref barId, sides.Left, layer, level, mainY, k, mark, leftOut, Anchor(spec, rules, st, k, level, sides.Left));
                else if (sides.IsSymmetric)
                    Add(bars, spec, rules, st, warnings, blocking, ref barId, sides.Left, layer, level, mainY, k, mark, leftOut, rightOut);
                else
                {
                    // Both sides are kept. Across the beam they share the level's slots, the side with more steel
                    // on the outer ones; over the support that side runs to the far face and anchors like an end
                    // support, the other runs straight through and on G2·d into the neighbouring span.
                    bool leftStrong = Area(sides.Left) >= Area(sides.Right);
                    int countLeft = sides.Left.Sum(i => i.Count);
                    int countRight = sides.Right.Sum(i => i.Count);
                    double maxD = sides.Left.Concat(sides.Right).Max(i => i.Diameter);
                    double spacingD = layer == 0 && !spec.TopContinuous.IsEmpty ? Math.Max(maxD, spec.TopContinuous.Diameter) : maxD;

                    var allYs = layer == 0
                        ? KataLayerPositions.BetweenMainBars(mainY, countLeft + countRight, spec.Width, rules, maxD)
                        : KataRebarCalculator.ComputeTransverseYPositions(spec.Width, rules.StirrupCover, rules.StirrupDiameter, maxD, countLeft + countRight);
                    KataLayerPositions.CheckSpacing(warnings, blocking, mark, layer == 0 ? mainY.Concat(allYs) : allYs, spacingD, rules);
                    var (ysLeft, ysRight) = layer > 0 && SpansDifferInWidth(spec, st, k) && !(countLeft % 2 == 1 && countRight % 2 == 1)
                        // Spans of different widths: each side spread across its own span, under its corner bars (B01 at
                        // K, "2f20;2f16": 2Ø16 at the corners of the 300 span L, section 11-11). Two odd counts would
                        // both put a bar on the axis, so those share the level's slots like equal widths do.
                        ? (Spread(spec, rules, maxD, countLeft), Spread(spec, rules, maxD, countRight))
                        : KataLayerPositions.PartitionInterleaved(allYs, countLeft, countRight, leftStrong);

                    var leftEnd = leftStrong
                        ? Anchor(spec, rules, st, k, level, sides.Left, outward: +1)
                        : new KataBarEnd(rightThrough ?? st.SupportEnd[k], 0.0, 0.0);
                    var rightStart = leftStrong
                        ? new KataBarEnd(leftThrough ?? st.SupportStart[k], 0.0, 0.0)
                        : Anchor(spec, rules, st, k, level, sides.Right, outward: -1);

                    Add(bars, spec, rules, st, warnings, blocking, ref barId, sides.Left, layer, level, mainY, k, mark + "T",
                        leftOut, leftEnd, ysLeft);
                    Add(bars, spec, rules, st, warnings, blocking, ref barId, sides.Right, layer, level, mainY, k, mark + "P",
                        rightStart, rightOut, ysRight);
                }
            }
        }

        return bars;
    }

    /// <summary>
    /// End of a bar of <paramref name="level"/> anchored in end support <paramref name="support"/>: G2·d from the
    /// inner face, bent down at the far face (inboard by the level's inset) when the support is too narrow.
    /// </summary>
    public static KataBarEnd Anchor(KataBeamRebarSpec spec, KataDetailingRules rules, KataBeamStations st, int support, KataTopLevel level, IReadOnlyList<KataBarItem> items) =>
        Anchor(spec, rules, st, support, level, items, outward: support == 0 ? -1 : +1);

    /// <param name="outward">+1 when the bar reaches the support from the span on its left, −1 from the right.</param>
    private static KataBarEnd Anchor(KataBeamRebarSpec spec, KataDetailingRules rules, KataBeamStations st, int support, KataTopLevel level, IReadOnlyList<KataBarItem> items, int outward)
    {
        double d = items.Count == 0 ? level.Diameter : items.Max(i => i.Diameter);
        double innerFace = outward < 0 ? st.SupportEnd[support] : st.SupportStart[support];
        double legRoom = level.Z - (-spec.SupportDepth(support) + rules.BottomBarCentreDepth);
        bool interior = support > 0 && support < st.SpanCount;
        if (interior)
        {
            // The bottom bars run on through an interior support: the leg stops a layer gap above them.
            double dBottom = spec.BottomContinuous.IsEmpty ? 0.0 : spec.BottomContinuous.Diameter;
            legRoom -= (d + dBottom) / 2.0 + rules.LayerGap(d, dBottom);
        }
        return KataAnchorage.Solve(innerFace, st.AnchorWidth(support), outward, rules.ColumnEndCover,
            rules.TopAnchorageFactor * d, rules.MinimumLegFactor * d, legRoom, level.Inset, rules.RoundLegMm);
    }

    private static double Area(IEnumerable<KataBarItem> items) => items.Sum(i => i.Count * i.Diameter * i.Diameter);

    /// <summary>
    /// Where the weaker side of a left/right cell over interior support <paramref name="k"/> stops: it runs straight
    /// through the support and on G2·d into the neighbouring span (never past the beam ends). The stronger side
    /// anchors over the support instead.
    /// </summary>
    private static (double? Left, double? Right) RunThrough(KataDetailingRules rules, KataBeamStations st, int k, KataSideBars sides)
    {
        bool leftStrong = Area(sides.Left) >= Area(sides.Right);
        if (leftStrong)
            return sides.Right.Count == 0
                ? (null, null)
                : (Math.Max(st.SupportStart[0] + rules.TopEndCover, st.SupportStart[k] - rules.TopAnchorageFactor * MaxDiameter(sides.Right)), null);

        return sides.Left.Count == 0
            ? (null, null)
            : (null, Math.Min(st.SupportEnd[st.SpanCount] - rules.TopEndCover, st.SupportEnd[k] + rules.TopAnchorageFactor * MaxDiameter(sides.Left)));
    }

    private static double MaxDiameter(IReadOnlyList<KataBarItem> items) => items.Count == 0 ? 0.0 : items.Max(i => i.Diameter);

    private static string Cell(KataBeamRebarSpec spec, int support, int row) =>
        spec.Supports[support].SheetColumn > 0
            ? KataDamCellAccessorExtensions.ToAddress(row, spec.Supports[support].SheetColumn)
            : $"Gối {support + 1} hàng {row}";

    /// <summary>The spans either side of support <paramref name="k"/> differ in width (row 20).</summary>
    private static bool SpansDifferInWidth(KataBeamRebarSpec spec, KataBeamStations st, int k) =>
        k > 0 && k < st.SpanCount && Math.Abs(spec.WidthOf(k - 1) - spec.WidthOf(k)) > 0.5;

    /// <summary><paramref name="count"/> bars of a layer across B6 (each is moved to its own span's width afterwards).</summary>
    private static IReadOnlyList<double> Spread(KataBeamRebarSpec spec, KataDetailingRules rules, double diameter, int count) =>
        KataRebarCalculator.ComputeTransverseYPositions(spec.Width, rules.StirrupCover, rules.StirrupDiameter, diameter, count);

    /// <summary>
    /// Stations where a row's bars stop in the spans left and right of support <paramref name="k"/>: H5 × that
    /// span's clear length (rounded up to the cut step) from the support face or centre as I5 says.
    /// </summary>
    private static (double Left, double Right) Cuts(KataBeamRebarSpec spec, KataDetailingRules rules, KataBeamStations st, int k)
    {
        double ratio = spec.TopCutoffRatioLayer1 > 0.0 ? spec.TopCutoffRatioLayer1 : 0.25;
        bool fromCentre = spec.CutoffOriginLayer1 == KataCutoffOrigin.FromColumnCenter;
        double leftReach = k > 0 ? rules.RoundUp(ratio * spec.Spans[k - 1].Length) : 0.0;
        double rightReach = k < st.SpanCount ? rules.RoundUp(ratio * spec.Spans[k].Length) : 0.0;
        double left = (fromCentre ? st.SupportCentre(k) : st.SupportStart[k]) - leftReach;
        double right = (fromCentre ? st.SupportCentre(k) : st.SupportEnd[k]) + rightReach;
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
        KataBarEnd end,
        IReadOnlyList<double>? customY = null)
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
        var ys = customY ?? (layer == 0
            ? KataLayerPositions.BetweenMainBars(mainY, count, spec.Width, rules, maxD)
            : KataRebarCalculator.ComputeTransverseYPositions(spec.Width, rules.StirrupCover, rules.StirrupDiameter, maxD, count));
        if (customY is null)
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
