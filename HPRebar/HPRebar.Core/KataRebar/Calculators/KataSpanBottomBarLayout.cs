using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Additional bottom bars of the spans (rows 17-18). Row 18 (layer 1) shares the bottom main bars' level and
/// fills the gaps between them, each bar resting on the stirrup by its own diameter when that sits lower than
/// the main bars' centre; row 17 (layer 2) sits above the highest bar of that level at a clear gap of
/// max(30, d) (the rules' layer gap), spread over the width with the larger bars at the edges, or on the stirrup when the level is
/// empty. Both are straight and stop <see cref="KataDetailingRules.BottomExtraCutFraction"/> x the clear span
/// from each support face.
/// </summary>
public static class KataSpanBottomBarLayout
{
    public const int Layer1Row = 18;
    public const int Layer2Row = 17;

    public static List<KataRebarCurve> Build(
        KataBeamRebarSpec spec,
        KataDetailingRules rules,
        KataBeamStations st,
        IReadOnlyList<KataRebarCurve> topBars,
        List<string> warnings,
        List<string> blocking,
        ref int barId)
    {
        var bars = new List<KataRebarCurve>();
        bool hasMain = !spec.BottomContinuous.IsEmpty;
        double mainD = hasMain ? spec.BottomContinuous.Diameter : 0.0;
        double mainZ = -spec.Height + rules.BottomBarCentreDepth;
        var mainY = hasMain
            ? KataRebarCalculator.ComputeTransverseYPositions(spec.Width, rules.StirrupCover, rules.StirrupDiameter, mainD, spec.BottomContinuous.Count)
            : Array.Empty<double>();

        // Centre of a bar of diameter d on the bottom level: the main bars' centre, or lower on the stirrup's
        // inner face when a bigger bar would otherwise cut into it.
        double Seat(double d) => -spec.Height + Math.Max(rules.BottomBarCentreDepth, rules.StirrupCover + rules.StirrupDiameter + d / 2.0);

        for (int s = 0; s < st.SpanCount && s < spec.Spans.Count; s++)
        {
            var span = spec.Spans[s];
            var layer1 = Readable(warnings, span, Layer1Row, span.BottomExtraLayer1, span.BottomExtraLayer1Text);
            var layer2 = Readable(warnings, span, Layer2Row, span.BottomExtraLayer2, span.BottomExtraLayer2Text);
            if (layer1.Count == 0 && layer2.Count == 0) continue;
            if (span.Length <= 0.0)
            {
                warnings.Add($"{Cell(span, Layer1Row)}/{Layer2Row}: nhịp dài {span.Length:0} mm — không vẽ thép gia cường nhịp.");
                continue;
            }

            double cut = rules.RoundDown(rules.BottomExtraCutFraction * span.Length);
            double xStart = st.SpanStart[s] + cut;
            double xEnd = st.SpanEnd[s] - cut;

            // Top face and largest bar of the bottom level (main bars + row 18); none while both are empty.
            double? levelTop = hasMain ? mainZ + mainD / 2.0 : null;
            double levelD = mainD;
            (double Top, double D, int Row)? highest = null;

            if (layer1.Count > 0)
            {
                string mark = Mark(s, 1);
                var ys = KataLayerPositions.BetweenMainBars(mainY, Count(layer1), spec.Width, rules, MaxDiameter(layer1));
                var placed = OuterBarsLargest(layer1, ys);
                KataLayerPositions.CheckSpacing(warnings, blocking, mark, mainY.Concat(ys), Math.Max(mainD, MaxDiameter(layer1)), rules);
                Add(bars, warnings, ref barId, placed, Seat, xStart, xEnd, s, 1, mark);

                foreach (var (_, d) in placed)
                {
                    levelTop = Math.Max(levelTop ?? double.MinValue, Seat(d) + d / 2.0);
                    levelD = Math.Max(levelD, d);
                }

                highest = (levelTop!.Value, levelD, Layer1Row);
            }

            if (layer2.Count > 0)
            {
                string mark = Mark(s, 2);
                double d = MaxDiameter(layer2);
                double z = levelTop is { } top ? top + rules.LayerGap(levelD, d) + d / 2.0 : Seat(d);
                var ys = KataRebarCalculator.ComputeTransverseYPositions(spec.Width, rules.StirrupCover, rules.StirrupDiameter, d, Count(layer2));
                KataLayerPositions.CheckSpacing(warnings, blocking, mark, ys, d, rules);
                Add(bars, warnings, ref barId, OuterBarsLargest(layer2, ys), _ => z, xStart, xEnd, s, 2, mark);
                highest = (z + d / 2.0, d, Layer2Row);
            }

            if (highest is { } h)
                CheckClearOfTopBars(spec, rules, topBars, blocking, span, h.Top, h.D, h.Row, xStart, xEnd);
        }

        return bars;
    }

    /// <summary>
    /// Pairs positions with bars so a mixed cell stays symmetric: the largest bars take the outermost places.
    /// </summary>
    private static List<(double Y, double D)> OuterBarsLargest(IReadOnlyList<KataBarItem> items, IReadOnlyList<double> ys)
    {
        var diameters = items.SelectMany(i => Enumerable.Repeat(i.Diameter, i.Count)).OrderByDescending(d => d).ToList();
        var places = ys.OrderByDescending(y => Math.Abs(y)).ThenBy(y => y).ToList();
        return places.Zip(diameters, (y, d) => (y, d)).OrderBy(p => p.y).ToList();
    }

    /// <summary>
    /// Blocks when the highest additional bottom level comes closer than the layer gap to a top level over the
    /// stretch both share:
    /// the top main bars run the whole beam, a support's additional bars only as far as they reach.
    /// </summary>
    private static void CheckClearOfTopBars(
        KataBeamRebarSpec spec,
        KataDetailingRules rules,
        IReadOnlyList<KataRebarCurve> topBars,
        List<string> blocking,
        KataSpanRebarSpec span,
        double topFace,
        double d,
        int row,
        double xStart,
        double xEnd)
    {
        var levels = new List<(double Z, double D)>();
        if (!spec.TopContinuous.IsEmpty) levels.Add((-rules.TopBarCentreDepth, spec.TopContinuous.Diameter));
        foreach (var bar in topBars)
        {
            var points = bar.Polyline.Points;
            if (points.Max(p => p.X) > xStart && points.Min(p => p.X) < xEnd)
                levels.Add((points.Max(p => p.Z), bar.Diameter));
        }

        foreach (var (topZ, topD) in levels.OrderBy(l => l.Z))
        {
            double clear = topZ - topD / 2.0 - topFace;
            double needed = rules.LayerGap(topD, d);
            if (clear + 1e-6 < needed)
            {
                blocking.Add($"{Cell(span, row)}: lớp gia cường dưới cách lớp thép trên {clear:0} mm thông thủy, cần {needed:0} mm — dầm không đủ cao cho số lớp này.");
                return;
            }
        }
    }

    /// <summary>The bars of a row, warning when the cell holds text none of which reads as bars.</summary>
    private static IReadOnlyList<KataBarItem> Readable(
        List<string> warnings, KataSpanRebarSpec span, int row, IReadOnlyList<KataBarItem> items, string text)
    {
        var bars = items.Where(i => !i.IsEmpty).ToList();
        string trimmed = text.Trim();
        var unreadable = KataBarNotationParser.UnreadableTokens(trimmed);
        if (bars.Count == 0 && unreadable.Count > 0)
            warnings.Add($"{Cell(span, row)} '{trimmed}': không đọc được ký hiệu thép — không vẽ.");
        else
            foreach (var token in unreadable)
                warnings.Add($"{Cell(span, row)} '{trimmed}': không đọc được '{token}' — phần đó không vẽ.");
        return bars;
    }

    private static void Add(
        List<KataRebarCurve> bars,
        List<string> warnings,
        ref int barId,
        IReadOnlyList<(double Y, double D)> placed,
        Func<double, double> zOf,
        double xStart,
        double xEnd,
        int span,
        int layer,
        string mark)
    {
        if (xEnd - xStart < 1.0)
        {
            warnings.Add($"Thép gia cường {mark}: nhịp quá ngắn, hai điểm cắt gặp nhau — không vẽ.");
            return;
        }

        foreach (var (y, d) in placed)
            bars.Add(Bar(barId++, d, y, zOf(d), xStart, xEnd, span, layer, mark));
    }

    private static KataRebarCurve Bar(int id, double dia, double y, double z, double xStart, double xEnd, int span, int layer, string mark) => new()
    {
        BarId = id,
        Role = KataBarRole.ExtraBottom,
        Diameter = dia,
        Layer = layer,
        Polyline = new Polyline3(new List<Point3> { new(xStart, y, z), new(xEnd, y, z) }),
        StartHookAngle = HookAngle.None,
        EndHookAngle = HookAngle.None,
        TransverseY = y,
        HostSpanIndex = span,
        HostSupportIndex = -1,
        ShapeCode = "00",
        BarMark = mark,
        BarDescription = $"Gia cường nhịp {span + 1} hàng {(layer == 1 ? Layer1Row : Layer2Row)}",
        DimA = xEnd - xStart,
        SttCad = 4
    };

    private static string Mark(int span, int layer) => $"4.{span + 1}.{layer}";

    private static int Count(IReadOnlyList<KataBarItem> items) => items.Sum(i => i.Count);

    private static double MaxDiameter(IReadOnlyList<KataBarItem> items) => items.Count == 0 ? 0.0 : items.Max(i => i.Diameter);

    private static string Cell(KataSpanRebarSpec span, int row) =>
        span.SheetColumn > 0 ? KataDamCellAccessorExtensions.ToAddress(row, span.SheetColumn) : $"Nhịp {span.SpanIndex + 1} hàng {row}";
}
