using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Spans of their own width (row 20): the bars are laid out across B6, then moved across to the width of the span
/// they lie in (the same bars spread between the stirrup legs of that span, the beam centred). Where the width
/// changes over a support the longitudinal bars cannot run on straight, so Kata cuts them there (B01 at K, 500 → 300):
/// the bar of the wider span anchors in the support like an end support (top bars bend down, bottom bars up), the
/// bar of the narrower span runs on straight into the wider one, overlapping the anchored bar by G3·d from its end. An
/// additional bar of that support reaching mostly into one span is only shortened to that side.
/// </summary>
public static class KataWidthProfile
{
    public static bool IsUniform(KataBeamRebarSpec spec) =>
        Enumerable.Range(0, spec.Spans.Count).All(s => Math.Abs(spec.WidthOf(s) - spec.Width) <= 0.5);

    /// <summary>The supports where the width changes.</summary>
    public static IEnumerable<int> Changes(KataBeamRebarSpec spec, KataBeamStations st) =>
        Enumerable.Range(1, Math.Max(0, st.SpanCount - 1))
            .Where(k => Math.Abs(spec.WidthOf(k - 1) - spec.WidthOf(k)) > 0.5);

    /// <param name="legDirection">−1 for top bars (bending down), +1 for bottom bars (up).</param>
    /// <param name="mainBars">The bars of each span when these are its main bars (row 19 / 21): they are cut where they change too.</param>
    public static List<KataRebarCurve> Apply(
        KataBeamRebarSpec spec, KataDetailingRules rules, KataBeamStations st, IReadOnlyList<KataRebarCurve> bars, int legDirection, ref int barId,
        Func<int, KataBarItem>? mainBars = null)
    {
        var result = new List<KataRebarCurve>(bars.Count);
        var changes = Changes(spec, st)
            .Union(mainBars is null ? Enumerable.Empty<int>() : Enumerable.Range(1, Math.Max(0, st.SpanCount - 1)).Where(k => !mainBars(k - 1).SameBars(mainBars(k))))
            .OrderBy(k => k)
            .ToList();
        if (changes.Count == 0)
        {
            result.AddRange(bars);
            return result;
        }
        foreach (var bar in bars)
        {
            var parts = new List<KataRebarCurve> { bar };
            foreach (int k in changes)
            {
                var last = parts[parts.Count - 1];
                double min = Min(last), max = Max(last);
                if (min >= st.SupportStart[k] - 1.0 || max <= st.SupportEnd[k] + 1.0) continue;
                // A main bar not covering both spans was already cut there (a console, a soffit step): its pieces
                // only take their own span's bars and width.
                if (mainBars is not null && (min > st.SpanStart[k - 1] + 1.0 || max < st.SpanEnd[k] - 1.0)) continue;

                parts.RemoveAt(parts.Count - 1);
                parts.AddRange(Cut(spec, rules, st, last, k, legDirection, mainBars));
            }

            for (int i = 0; i < parts.Count; i++)
            {
                var part = Across(spec, rules, st, parts[i]);
                result.Add(i == 0 ? part : part with { BarId = barId++ });
            }
        }

        return result;
    }

    /// <summary>A bar over support <paramref name="k"/> cut there; one that belongs to one side is only shortened.</summary>
    private static IEnumerable<KataRebarCurve> Cut(KataBeamRebarSpec spec, KataDetailingRules rules, KataBeamStations st,
        KataRebarCurve bar, int k, int legDirection, Func<int, KataBarItem>? mainBars)
    {
        // The wider span anchors its bars; where only the bars change, the side with more steel does.
        bool leftWider = Math.Abs(spec.WidthOf(k - 1) - spec.WidthOf(k)) > 0.5 || mainBars is null
            ? spec.WidthOf(k - 1) > spec.WidthOf(k)
            : mainBars(k - 1).TotalAreaMm2 >= mainBars(k).TotalAreaMm2;
        // Each piece is sized for the bars it will carry (its span's own, rows 19 / 21).
        double DiameterOf(int span) => mainBars is null || mainBars(span).IsEmpty ? bar.Diameter : mainBars(span).Diameter;
        double dAnchored = DiameterOf(leftWider ? k - 1 : k), dLapped = DiameterOf(leftWider ? k : k - 1);
        var anchored = Anchor(spec, rules, st, k, leftWider ? +1 : -1, dAnchored, legDirection);
        // Top: the narrower bar overlaps the anchored one by G3·d from its end, d the larger (B01 at K: 24945 − 750 =
        // 24200). Bottom main bars: G3·d of their own past their own face of the support, as the bottom bars beside a
        // cut step run (B01: 25000 − 600 = 24400).
        bool fromFace = legDirection > 0 && mainBars is not null;
        double lapLength = rules.BottomAnchorageFactor * Math.Max(dLapped, legDirection < 0 ? dAnchored : 0.0);
        double lap = fromFace ? lapLength : rules.RoundUp(lapLength);
        double leftLap = fromFace ? st.SupportStart[k] + lap : anchored.X + lap;
        double rightLap = fromFace ? st.SupportEnd[k] - lap : anchored.X - lap;
        var leftEnd = leftWider ? anchored : new KataBarEnd(leftLap, 0.0, 0.0);
        var rightEnd = leftWider ? new KataBarEnd(rightLap, 0.0, 0.0) : anchored;

        double leftLength = st.SupportStart[k] - Min(bar), rightLength = Max(bar) - st.SupportEnd[k];
        bool ownBar = bar.HostSupportIndex == k;
        if (ownBar && leftLength < 2.0 * lap && leftLength < rightLength)
            return new[] { KataBarSplit.From(bar, rightEnd, legDirection) };
        if (ownBar && rightLength < 2.0 * lap)
            return new[] { KataBarSplit.Until(bar, leftEnd, legDirection) };
        return new[] { KataBarSplit.Until(bar, leftEnd, legDirection), KataBarSplit.From(bar, rightEnd, legDirection) };
    }

    private static KataBarEnd Anchor(KataBeamRebarSpec spec, KataDetailingRules rules, KataBeamStations st, int support, int outward, double d, int legDirection)
    {
        bool top = legDirection < 0;
        return KataMainBarLayout.Solve(st, rules, support, outward,
            top ? rules.TopEndCover : rules.BottomEndCover,
            (top ? rules.TopAnchorageFactor : rules.BottomAnchorageFactor) * d,
            rules.MinimumLegFactor * d, KataMainBarLayout.LegRoom(spec, rules, support), 0.0);
    }

    /// <summary>The bar moved across to the width of the span it lies mostly in.</summary>
    private static KataRebarCurve Across(KataBeamRebarSpec spec, KataDetailingRules rules, KataBeamStations st, KataRebarCurve bar)
    {
        double width = spec.WidthOf(HomeSpan(st, bar));
        if (Math.Abs(width - spec.Width) <= 0.5) return bar;

        double from = rules.EdgeBarOffset(spec.Width, bar.Diameter);
        double to = rules.EdgeBarOffset(width, bar.Diameter);
        double scale = from > 0.0 ? Math.Max(0.0, to) / from : 1.0;
        var points = bar.Polyline.Points.Select(p => new Point3(p.X, p.Y * scale, p.Z)).ToList();
        return bar with { Polyline = new Polyline3(points), TransverseY = bar.TransverseY * scale };
    }

    /// <summary>
    /// Main bars of a span narrower than B6 closer than the clear gap Kata keeps between bars (the larger of the layer
    /// gap and the bar): they are drawn as laid out, the sheet's row 20 named so the user can check.
    /// </summary>
    public static IEnumerable<string> SpacingWarnings(KataBeamRebarSpec spec, KataDetailingRules rules)
    {
        for (int s = 0; s < spec.Spans.Count; s++)
        {
            double width = spec.WidthOf(s);
            foreach (var (item, beam, name) in new[] { (spec.TopMainOf(s), spec.TopContinuous, "trên"), (spec.BottomMainOf(s), spec.BottomContinuous, "dưới") })
            {
                // B11 / B12 across B6 are the user's own layout; only a span of its own width or bars is checked.
                if (Math.Abs(width - spec.Width) <= 0.5 && item.SameBars(beam)) continue;
                if (item.IsEmpty || item.Count < 2) continue;
                double clear = 2.0 * rules.EdgeBarOffset(width, item.Diameter) / (item.Count - 1) - item.Diameter;
                double needed = Math.Max(rules.LayerGap(item.Diameter, item.Diameter), item.Diameter);
                if (clear + 1e-6 < needed)
                {
                    int row = Math.Abs(width - spec.Width) > 0.5 ? 20 : name == "trên" ? 19 : 21;
                    string cell = spec.Spans[s].SheetColumn > 0 ? Parsers.KataDamCellAccessorExtensions.ToAddress(row, spec.Spans[s].SheetColumn) : $"nhịp {s + 1}";
                    yield return $"{cell}: nhịp rộng {width:0} — {item.Count}Ø{item.Diameter:0} thép chủ {name} chỉ cách nhau {Math.Max(0.0, clear):0} mm (cần {needed:0}).";
                }
            }
        }
    }

    /// <summary>The span holding the longest stretch of the bar.</summary>
    public static int HomeSpan(KataBeamStations st, KataRebarCurve bar)
    {
        double min = Min(bar), max = Max(bar);
        int best = 0;
        double longest = double.NegativeInfinity;
        for (int s = 0; s < st.SpanCount; s++)
        {
            double overlap = Math.Min(max, st.SpanEnd[s]) - Math.Max(min, st.SpanStart[s]);
            if (overlap > longest) { longest = overlap; best = s; }
        }

        return best;
    }

    private static double Min(KataRebarCurve bar) => bar.Polyline.Points.Min(p => p.X);
    private static double Max(KataRebarCurve bar) => bar.Polyline.Points.Max(p => p.X);
}
