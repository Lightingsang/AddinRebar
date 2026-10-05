using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// A "-" in row 13 over a support means, as Kata draws it: the row-13 bars of the neighbouring support run on to
/// this one. Over an end support they anchor there like the main bars (at a cantilever tip they stop a cover short
/// of it); over an interior support they run through it and stop H5 × L into the span beyond, G1 further when that
/// support has lower rows of its own. Consecutive "-" chain: the bar runs on to the last of them (T2-DY14: G13 through
/// I and on to the end beam K). Rows 14-16 do not take "-" yet (their levels would collide).
/// </summary>
internal static class KataTopBarContinuation
{
    public const string Mark = "-";

    public static bool Continues(KataBeamRebarSpec spec, int support, int layer) =>
        layer == 0 && IsMark(spec, support, layer);

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

        double reach = Reach(spec, rules, next, spec.Spans[next - 1].Length);
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

        double reach = Reach(spec, rules, next, spec.Spans[next].Length);
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

    /// <summary>H5 × L, one G1 further when the "-" support has lower rows of its own to stagger over.</summary>
    private static double Reach(KataBeamRebarSpec spec, KataDetailingRules rules, int support, double span)
    {
        double ratio = spec.TopCutoffRatioLayer1 > 0.0 ? spec.TopCutoffRatioLayer1 : 0.25;
        bool lowerRows = Enumerable.Range(1, 3).Any(layer => !KataTopLayerStack.Sides(spec, support, layer).IsEmpty);
        return rules.RoundUp(rules.RoundUp(ratio * span) + (lowerRows ? rules.CurtailedExtension : 0.0));
    }

    private static bool FromCentre(KataBeamRebarSpec spec) => spec.CutoffOriginLayer1 == KataCutoffOrigin.FromColumnCenter;
}
