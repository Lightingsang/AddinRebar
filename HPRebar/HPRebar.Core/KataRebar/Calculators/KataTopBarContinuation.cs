using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// A "-" in one of rows 13-16 over a support means, as Kata draws it: the bars of that row of the neighbouring support
/// run on to this one. Over an end support they anchor there like the main bars (at a cantilever tip they stop a cover short
/// of it); over an interior support they run through it and stop H5 × L into the span beyond, G1 further when that
/// support has rows of its own below that row. Consecutive "-" chain: the bar runs on to the last of them (T2-DY14: G13 through
/// I and on to the end beam K; T1-DX10: E14 through short span F to the end support G14 "-").
/// </summary>
internal static class KataTopBarContinuation
{
    public const string Mark = "-";

    /// <remarks>
    /// Rows 14-16 run on only over a "-" support with no rows of its own below them: the levels of such a support close
    /// up under its own rows, so a passing bar would lie on one of them.
    /// </remarks>
    public static bool Continues(KataBeamRebarSpec spec, int support, int layer) =>
        IsMark(spec, support, layer) && (layer == 0 || !HasRowsBelow(spec, support, layer));

    private static bool HasRowsBelow(KataBeamRebarSpec spec, int support, int layer) =>
        Enumerable.Range(layer + 1, 3 - layer).Any(lower => !KataTopLayerStack.Sides(spec, support, lower).IsEmpty);

    public static bool IsMark(KataBeamRebarSpec spec, int support, int layer) =>
        support >= 0 && support < spec.Supports.Count && KataTopLayerStack.Sides(spec, support, layer).Text.Trim() == Mark;

    /// <summary>The end of a bar of support <paramref name="support"/> reaching into the span on the left.</summary>
    public static KataBarEnd Left(KataBeamRebarSpec spec, KataDetailingRules rules, KataBeamStations st, int support, int layer,
        KataTopLevel level, IReadOnlyList<KataBarItem> items, double cut)
    {
        int next = support - 1;
        if (!Continues(spec, next, layer)) return new KataBarEnd(cut, 0.0, 0.0);
        while (next > 0 && Continues(spec, next - 1, layer)) next--;
        if (next == 0)
            return st.SupportWidth[0] > 0.0
                ? KataSupportTopBarLayout.Anchor(spec, rules, st, 0, level, items)
                : new KataBarEnd(st.SupportStart[0] + rules.StirrupCover, 0.0, 0.0);

        double reach = Reach(spec, rules, next, layer, spec.Spans[next - 1].Length);
        double origin = FromCentre(spec) ? st.SupportCentre(next) : st.SupportStart[next];
        return new KataBarEnd(System.Math.Max(st.SupportStart[0] + rules.TopEndCover, origin - reach), 0.0, 0.0);
    }

    /// <summary>The end of a bar of support <paramref name="support"/> reaching into the span on the right.</summary>
    public static KataBarEnd Right(KataBeamRebarSpec spec, KataDetailingRules rules, KataBeamStations st, int support, int layer,
        KataTopLevel level, IReadOnlyList<KataBarItem> items, double cut)
    {
        int next = support + 1;
        int last = st.SpanCount;
        if (!Continues(spec, next, layer)) return new KataBarEnd(cut, 0.0, 0.0);
        while (next < last && Continues(spec, next + 1, layer)) next++;
        if (next == last)
            return st.SupportWidth[last] > 0.0
                ? KataSupportTopBarLayout.Anchor(spec, rules, st, last, level, items)
                : new KataBarEnd(st.SupportEnd[last] - rules.StirrupCover, 0.0, 0.0);

        double reach = Reach(spec, rules, next, layer, spec.Spans[next].Length);
        double origin = FromCentre(spec) ? st.SupportCentre(next) : st.SupportEnd[next];
        return new KataBarEnd(System.Math.Min(st.SupportEnd[last] - rules.TopEndCover, origin + reach), 0.0, 0.0);
    }

    /// <summary>
    /// The support beyond a "-" chain on the right of <paramref name="support"/> when it has row-13 bars of its own:
    /// both bars then run over the chain, one beside the other. Null when there is no such chain.
    /// </summary>
    public static int? SharedChainEnd(KataBeamRebarSpec spec, KataBeamStations st, int support)
    {
        int next = support + 1;
        int last = st.SpanCount;
        if (!Continues(spec, next, 0)) return null;
        while (next < last && Continues(spec, next + 1, 0)) next++;
        return next < last && !KataTopLayerStack.Sides(spec, next + 1, 0).IsEmpty ? next + 1 : null;
    }

    /// <summary>
    /// The rows that run on over a "-" chain and anchor in end support <paramref name="end"/>: the level and bars of
    /// the support they come from, so the bends they make there are known to the bars anchoring beside them.
    /// </summary>
    public static IEnumerable<(KataTopLevel Level, IReadOnlyList<KataBarItem> Items)> ArrivingAt(
        KataBeamRebarSpec spec, KataDetailingRules rules, KataBeamStations st, int end)
    {
        int last = st.SpanCount;
        if (end != 0 && end != last) yield break;
        int inward = end == 0 ? 1 : -1;
        for (int layer = 0; layer < 4; layer++)
        {
            if (!Continues(spec, end, layer)) continue;
            int source = end + inward;
            while (source > 0 && source < last && Continues(spec, source, layer)) source += inward;
            var items = KataTopLayerStack.Sides(spec, source, layer).Side(right: end != 0);
            int spanSide = source == 0 ? 1 : source == last ? -1 : 0;
            var level = KataTopLayerStack.At(spec, rules, source, spanSide).FirstOrDefault(l => l.Row == KataTopLayerStack.FirstRow + layer);
            if (items.Count > 0 && level is not null) yield return (level, items);
        }
    }

    /// <summary>H5 × L, one G1 further when the "-" support has lower rows of its own to stagger over.</summary>
    private static double Reach(KataBeamRebarSpec spec, KataDetailingRules rules, int support, int layer, double span)
    {
        double ratio = spec.TopCutoffRatioLayer1 > 0.0 ? spec.TopCutoffRatioLayer1 : 0.25;
        return rules.RoundUp(rules.RoundUp(ratio * span) + (HasRowsBelow(spec, support, layer) ? rules.CurtailedExtension : 0.0));
    }

    private static bool FromCentre(KataBeamRebarSpec spec) => spec.CutoffOriginLayer1 == KataCutoffOrigin.FromColumnCenter;
}
