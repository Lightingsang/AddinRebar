using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Row 17 over a support with a width (B03, T2-DY7.dwg 2026-10-06), "left;right" or one text for both sides:
/// <list type="bullet">
/// <item>left "-": the row-17 bars of the span on the left run into the support and end there as the bottom main
/// bar of that span does (B03 E: D17's 6Ø25 bent up at 11100, beside the main bar's 11150);</item>
/// <item>right "-": those bars run on into the span on the right as bars of their own, starting as that span's
/// bottom main bar starts (B03: 6Ø25 from 10450 through span 2 to G, under its own F17 2Ø20, which take the gaps
/// nearest the middle);</item>
/// <item>bars ("2f20"): laid over the support, the left part from H5 × L of the left span off the support face to
/// where that span's bottom main bar ends, the right part from where the right span's starts to H5 × L into it (a
/// console: to a cover short of its tip). B03 I: 2Ø20 29650 → 31800 and 31275 → 33570.</item>
/// </list>
/// An end anchored in the support lies <see cref="AnchoredEndInset"/> inside its far face (B03: 11100 in E, 24900 in
/// G, 31275 in I) and turns up by G3·d less its run inside the support.
/// </summary>
public static class KataSupportBottomBarLayout
{
    /// <summary>Mark suffix of the bars a support's "-" runs on into the next span.</summary>
    public const string ThroughSuffix = "R";

    private const string Dash = "-";

    /// <summary>Distance of an anchored row-17 end from the support's far face.</summary>
    public const double AnchoredEndInset = 100.0;

    public static List<KataRebarCurve> Apply(
        KataBeamRebarSpec spec, KataDetailingRules rules, KataBeamStations st, IReadOnlyList<KataRebarCurve> mainBottom,
        IReadOnlyList<KataRebarCurve> spanBars, List<string> warnings, ref int barId)
    {
        var bars = spanBars.ToList();
        foreach (int end in new[] { 0, st.SpanCount })
            if (end < spec.Supports.Count && spec.Supports[end].BottomLayer2Text is { Length: > 0 } endText && !KataDamSheetParser.IsBlank(endText))
                warnings.Add($"{Cell(spec, end)} '{endText}': hàng 17 ở gối đầu/cuối dầm chưa vẽ.");

        for (int k = 1; k < st.SpanCount && k < spec.Supports.Count; k++)
        {
            string text = spec.Supports[k].BottomLayer2Text;
            if (st.SupportWidth[k] <= 0.0 || text.Length == 0) continue;

            int split = text.IndexOf(';');
            string left = (split < 0 ? text : text.Substring(0, split)).Trim();
            string right = (split < 0 ? text : text.Substring(split + 1)).Trim();
            var anchors = new Anchors(spec, rules, st, mainBottom, k);

            var source = bars.Where(b => b.Layer == 2 && b.HostSpanIndex == k - 1 && b.HostSupportIndex < 0).ToList();
            if (left == Dash)
                foreach (var bar in source)
                    bars[bars.IndexOf(bar)] = WithEnd(bar, anchors.Left(bar.Diameter, bar.Polyline.Points.Last().Z));
            if (right == Dash && source.Count > 0)
                RunOn(spec, rules, st, bars, source, anchors, k, ref barId);
            if ((left == Dash || right == Dash) && source.Count == 0)
                warnings.Add($"{Cell(spec, k)} '{text}': nhịp trái không có thép hàng 17 để chạy qua gối — bỏ qua.");

            if (left != Dash) Over(spec, rules, st, bars, Bars(left), anchors, k, toRight: false, ref barId);
            if (right != Dash) Over(spec, rules, st, bars, Bars(right), anchors, k, toRight: true, ref barId);
            foreach (int s in new[] { k - 1, k })
                WarnIfTight(warnings, spec, rules, k, bars.Where(b => b.Layer == 2 && b.HostSpanIndex == s).ToList());
        }

        return bars;
    }

    /// <summary>
    /// Warns where two bars of a span's row 17 that run side by side somewhere come closer than max(25, d) clear: the
    /// layer of bars run on, the span's own and those laid over a support (Kata draws such bars on one another).
    /// </summary>
    private static void WarnIfTight(List<string> warnings, KataBeamRebarSpec spec, KataDetailingRules rules, int k, IReadOnlyList<KataRebarCurve> layer)
    {
        for (int i = 0; i < layer.Count; i++)
        for (int j = i + 1; j < layer.Count; j++)
        {
            var (a, b) = (layer[i], layer[j]);
            bool alongside = Math.Min(MaxX(a), MaxX(b)) - Math.Max(MinX(a), MinX(b)) > 1.0 && Math.Abs(a.Polyline.Points[0].Z - b.Polyline.Points[0].Z) < (a.Diameter + b.Diameter) / 2.0;
            double clear = Math.Abs(a.TransverseY - b.TransverseY) - (a.Diameter + b.Diameter) / 2.0;
            double needed = Math.Max(25.0, Math.Max(a.Diameter, b.Diameter));
            if (!alongside || clear >= needed - 1e-6) continue;
            string warning = $"{Cell(spec, k)}: thép hàng 17 nhịp {a.HostSpanIndex + 1} chỉ cách nhau {Math.Max(0.0, clear):0} mm thông thủy (cần {needed:0}) — kiểm tra trong Revit.";
            if (!warnings.Contains(warning)) warnings.Add(warning);
            return;
        }
    }

    private static double MinX(KataRebarCurve b) => b.Polyline.Points.Min(p => p.X);

    private static double MaxX(KataRebarCurve b) => b.Polyline.Points.Max(p => p.X);

    private static string Cell(KataBeamRebarSpec spec, int k) =>
        spec.Supports[k].SheetColumn > 0 ? KataDamCellAccessorExtensions.ToAddress(17, spec.Supports[k].SheetColumn) : $"Gối {k + 1} hàng 17";

    /// <summary>The bars of span k-1's row 17 again in span k, under its own row 17, which moves to the gaps nearest the middle.</summary>
    private static void RunOn(KataBeamRebarSpec spec, KataDetailingRules rules, KataBeamStations st, List<KataRebarCurve> bars,
        IReadOnlyList<KataRebarCurve> source, Anchors anchors, int k, ref int barId)
    {
        var own = bars.Where(b => b.Layer == 2 && b.HostSpanIndex == k && b.HostSupportIndex < 0).ToList();
        double d = source.Max(b => b.Diameter);
        double z = LevelZ(spec, rules, k, d);
        var start = anchors.Right(d, z);
        double end = st.SpanEnd[k] - Cut(spec, rules, st, k);
        string mark = $"4.{k + 1}.2{ThroughSuffix}";
        var throughY = source.Select(b => b.TransverseY).OrderBy(y => y).ToList();
        var ownY = MiddleGaps(throughY, own.Count);
        if (ownY.Count < own.Count)
        {
            // Too few gaps: the whole layer is spread across the span again, the span's own bars nearest the middle.
            var all = KataRebarCalculator.ComputeTransverseYPositions(spec.Width, rules.StirrupCover, rules.StirrupDiameter,
                Math.Max(d, own.Max(b => b.Diameter)), source.Count + own.Count);
            ownY = all.OrderBy(y => Math.Round(Math.Abs(y), 3)).ThenBy(y => y).Take(own.Count).OrderBy(y => y).ToList();
            throughY = all.Except(ownY).OrderBy(y => y).ToList();
        }

        var sources = source.OrderBy(b => b.TransverseY).ToList();
        for (int i = 0; i < sources.Count; i++)
            bars.Add(Bar(barId++, sources[i].Diameter, throughY[i], z, start, new KataBarEnd(end, 0.0, 0.0), k, -1, mark,
                $"Gia cường nhịp {k} hàng 17 chạy qua gối {k + 1} vào nhịp {k + 1}"));

        var ordered = own.OrderBy(b => b.TransverseY).ToList();
        for (int i = 0; i < ordered.Count; i++)
            bars[bars.IndexOf(ordered[i])] = Moved(ordered[i], ownY[i]);
    }

    /// <summary>
    /// The gaps between <paramref name="ys"/> nearest the middle, each once, pairs either side of it before the next
    /// pair out, the middle gap only for an odd count (B03 span 2: ±83, never 0 and −83): Kata draws a span's own row 17 over the middle bars run on into it (B03 5-5: 2Ø20 on the ±41.5 of
    /// 6Ø25), where they cannot lie, so they take the gaps beside those bars. Fewer gaps than bars leaves the rest
    /// where they were laid (the spacing warning then names the layer).
    /// </summary>
    private static List<double> MiddleGaps(IReadOnlyList<double> ys, int count) =>
        ys.Zip(ys.Skip(1), (a, b) => (a + b) / 2.0)
            .Where(g => count % 2 == 1 || Math.Abs(g) > 1e-6)
            .OrderBy(g => Math.Round(Math.Abs(g), 3)).ThenBy(g => g).Take(count).OrderBy(g => g).ToList();

    /// <summary>Bars of a support cell laid over it into the span on one side.</summary>
    private static void Over(KataBeamRebarSpec spec, KataDetailingRules rules, KataBeamStations st, List<KataRebarCurve> bars,
        IReadOnlyList<KataBarItem> items, Anchors anchors, int k, bool toRight, ref int barId)
    {
        int count = items.Sum(i => i.Count);
        if (count == 0) return;

        int span = toRight ? k : k - 1;
        double d = items.Max(i => i.Diameter);
        double z = LevelZ(spec, rules, span, d);
        var ys = KataRebarCalculator.ComputeTransverseYPositions(spec.Width, rules.StirrupCover, rules.StirrupDiameter, d, count);
        var diameters = items.SelectMany(i => Enumerable.Repeat(i.Diameter, i.Count)).ToList();
        bool tip = toRight && st.SupportWidth[k + 1] <= 0.0;
        // H5 × L, as B03 I17 2Ø20 stops 1550 into span 3 (6200).
        double reach = rules.RoundUp((spec.TopCutoffRatioLayer1 > 0.0 ? spec.TopCutoffRatioLayer1 : 0.25) * (st.SpanEnd[span] - st.SpanStart[span]));
        var start = toRight ? anchors.Right(d, z) : new KataBarEnd(st.SupportStart[k] - reach, 0.0, 0.0);
        var end = toRight
            ? new KataBarEnd(tip ? st.SupportStart[k + 1] - rules.BottomEndCover : st.SupportEnd[k] + reach, 0.0, 0.0)
            : anchors.Left(d, z);
        string mark = $"4.{k + 1}.2{(toRight ? "P" : "L")}";
        for (int i = 0; i < count; i++)
            bars.Add(Bar(barId++, diameters[i], ys[i], z, start, end, span, k, mark, $"Gia cường dưới gối {k + 1} hàng 17"));
    }

    /// <summary>Distance kept free of a span's own row 17 from a support face: H3 × L rounded up to the cut step.</summary>
    private static double Cut(KataBeamRebarSpec spec, KataDetailingRules rules, KataBeamStations st, int span) =>
        rules.RoundUp((spec.TopCutoffRatioLayer2 > 0.0 ? spec.TopCutoffRatioLayer2 : 0.20) * (st.SpanEnd[span] - st.SpanStart[span]));

    /// <summary>Centre of a row-17 bar of diameter d in span s: a layer gap above the span's bottom main bars.</summary>
    private static double LevelZ(KataBeamRebarSpec spec, KataDetailingRules rules, int s, double d)
    {
        double mainD = spec.BottomMainOf(s).IsEmpty ? 0.0 : spec.BottomMainOf(s).Diameter;
        double mainZ = -spec.DepthOf(s) + rules.BottomBarCentreDepth;
        return mainD > 0.0 ? mainZ + mainD / 2.0 + rules.LayerGap(mainD, d) + d / 2.0 : -spec.DepthOf(s) + rules.StirrupCover + rules.StirrupDiameter + d / 2.0;
    }

    private static IReadOnlyList<KataBarItem> Bars(string text) =>
        KataBarNotationParser.ParseBarList(text, defaultLayer: 2).Where(i => !i.IsEmpty).ToList();

    private static KataRebarCurve WithEnd(KataRebarCurve bar, KataBarEnd end)
    {
        var p = bar.Polyline.Points;
        var points = p.Take(p.Count - 1).ToList();
        var last = p[p.Count - 1];
        points.Add(new Point3(end.X, last.Y, last.Z));
        if (end.IsBent) points.Add(new Point3(end.X, last.Y, last.Z + end.Leg));
        return bar with
        {
            Polyline = new Polyline3(points),
            EndHookAngle = end.IsBent ? HookAngle.Hook90 : HookAngle.None,
            EndHookLength = end.Leg,
            ShapeCode = end.IsBent ? (bar.StartHookAngle != HookAngle.None ? "15a" : "05a") : bar.ShapeCode,
            DimA = end.X - p[0].X,
            DimC = end.Leg
        };
    }

    private static KataRebarCurve Moved(KataRebarCurve bar, double y) => bar with
    {
        TransverseY = y,
        Polyline = new Polyline3(bar.Polyline.Points.Select(p => new Point3(p.X, y, p.Z)).ToList())
    };

    private static KataRebarCurve Bar(int id, double dia, double y, double z, KataBarEnd start, KataBarEnd end, int span, int support,
        string mark, string description)
    {
        var points = new List<Point3>();
        if (start.IsBent) points.Add(new Point3(start.X, y, z + start.Leg));
        points.Add(new Point3(start.X, y, z));
        points.Add(new Point3(end.X, y, z));
        if (end.IsBent) points.Add(new Point3(end.X, y, z + end.Leg));
        return new KataRebarCurve
        {
            BarId = id,
            Role = KataBarRole.ExtraBottom,
            Diameter = dia,
            Layer = 2,
            Polyline = new Polyline3(points),
            StartHookAngle = start.IsBent ? HookAngle.Hook90 : HookAngle.None,
            EndHookAngle = end.IsBent ? HookAngle.Hook90 : HookAngle.None,
            StartHookLength = start.Leg,
            EndHookLength = end.Leg,
            TransverseY = y,
            HostSpanIndex = span,
            HostSupportIndex = support,
            ShapeCode = start.IsBent && end.IsBent ? "15a" : start.IsBent || end.IsBent ? "05a" : "00",
            BarMark = mark,
            BarDescription = description,
            DimA = end.X - start.X,
            DimB = start.Leg,
            DimC = end.Leg,
            SttCad = 4
        };
    }

    /// <summary>Where a row-17 bar ends at support k, after the bottom main bars that end or start there.</summary>
    private sealed class Anchors
    {
        private readonly KataDetailingRules _rules;
        private readonly KataBeamStations _st;
        private readonly int _k;
        private readonly KataRebarCurve? _fromLeft;
        private readonly KataRebarCurve? _intoRight;

        public Anchors(KataBeamRebarSpec spec, KataDetailingRules rules, KataBeamStations st, IReadOnlyList<KataRebarCurve> main, int k)
        {
            _rules = rules;
            _st = st;
            _k = k;
            _fromLeft = main.Where(b => MinX(b) < st.SupportStart[k] - 1.0 && MaxX(b) >= st.SupportStart[k] - 1.0 && MaxX(b) <= st.SupportEnd[k] + spec.Height)
                .OrderBy(MaxX).FirstOrDefault();
            _intoRight = main.Where(b => MaxX(b) > st.SupportEnd[k] + 1.0 && MinX(b) <= st.SupportEnd[k] + 1.0 && MinX(b) >= st.SupportStart[k] - spec.Height)
                .OrderByDescending(MinX).FirstOrDefault();
        }

        /// <summary>End of a bar coming from the left span: as that span's main bar ends, half a bar each inside it.</summary>
        public KataBarEnd Left(double d, double z)
        {
            if (_fromLeft is null) return Bent(_st.SupportEnd[_k] - AnchoredEndInset, _st.SupportStart[_k], d, z);
            var p = _fromLeft.Polyline.Points;
            bool bent = p[p.Count - 1].Z > p[p.Count - 2].Z + 1.0;
            double x = MaxX(_fromLeft);
            return bent ? Bent(_st.SupportEnd[_k] - AnchoredEndInset, _st.SupportStart[_k], d, z) : new KataBarEnd(x, 0.0, 0.0);
        }

        /// <summary>Start of a bar going into the right span: as that span's main bar starts.</summary>
        public KataBarEnd Right(double d, double z)
        {
            if (_intoRight is null) return Bent(_st.SupportStart[_k] + AnchoredEndInset, _st.SupportEnd[_k], d, z);
            var p = _intoRight.Polyline.Points;
            bool bent = p[0].Z > p[1].Z + 1.0;
            double x = MinX(_intoRight);
            return bent ? Bent(_st.SupportStart[_k] + AnchoredEndInset, _st.SupportEnd[_k], d, z) : new KataBarEnd(x, 0.0, 0.0);
        }

        /// <summary>Turned up by G3·d less the run past the inner face, kept under the top bars.</summary>
        private KataBarEnd Bent(double x, double innerFace, double d, double z)
        {
            double leg = _rules.BottomAnchorageFactor * d - Math.Abs(x - innerFace);
            double room = -_rules.TopBarCentreDepth - d - z;
            return new KataBarEnd(x, Math.Max(0.0, Math.Min(leg, room)), 0.0);
        }

        private static double MinX(KataRebarCurve b) => b.Polyline.Points.Min(p => p.X);

        private static double MaxX(KataRebarCurve b) => b.Polyline.Points.Max(p => p.X);
    }
}
