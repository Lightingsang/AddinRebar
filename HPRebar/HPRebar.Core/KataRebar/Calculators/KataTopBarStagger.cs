using System;
using System.Collections.Generic;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>One filled row of additional top bars over a support, with the stations where it stops in the spans.</summary>
/// <param name="Layer">0 for row 13 (on the main bars' level, the outermost) to 3 for row 16 (the innermost).</param>
/// <param name="ReachesLeft">Some of its bars stop at <paramref name="LeftCut"/> in the span on the left.</param>
/// <param name="ReachesRight">Some of its bars stop at <paramref name="RightCut"/> in the span on the right.</param>
/// <param name="LeftThrough">End in the left span of the weaker side of a left/right cell, which runs straight
/// through the support and on G2·d; null when the row has none there.</param>
/// <param name="RightThrough">The same in the right span.</param>
internal sealed record TopRow(
    int Layer,
    KataSideBars Sides,
    KataTopLevel Level,
    string Cell,
    double LeftCut,
    double RightCut,
    bool ReachesLeft,
    bool ReachesRight,
    double? LeftThrough = null,
    double? RightThrough = null)
{
    public double LeftCut { get; set; } = LeftCut;
    public double RightCut { get; set; } = RightCut;
}

/// <summary>
/// Staggered cut-off of the additional top bars (TCVN 5574:2018 § 10.3.2, Kata cell G1 "Kéo thép gia cường"):
/// on each side of a support every filled row reaches at least <see cref="KataDetailingRules.CurtailedExtension"/>
/// further into the span than the furthest bar end of the next filled row inside it, so no two rows stop at the
/// same section. Only the outer row's cut moves — outward, rounded up to the cut step, never past the face of the
/// support across the span; a row that cannot get the full step is reported. The weaker side of a left/right cell,
/// which runs through the support as anchorage, counts as an end of its row but is never moved.
/// </summary>
internal static class KataTopBarStagger
{
    public static void Apply(IReadOnlyList<TopRow> rows, KataBeamStations st, KataDetailingRules rules, int support, List<string> warnings)
    {
        double step = rules.CurtailedExtension;
        if (step <= 0.0 || rows.Count < 2) return;

        int last = st.SpanCount;
        if (support > 0)
        {
            // The span on the left ends at the face of support k − 1 (or at the beam end, a cover inside).
            double limit = Math.Max(st.SpanStart[support - 1], st.SupportStart[0] + rules.TopEndCover);
            Side(rows, rules, step, -1, st.SupportStart[support], limit, warnings, "trái",
                r => r.ReachesLeft, r => r.LeftCut, (r, x) => r.LeftCut = x, r => r.LeftThrough);
        }

        if (support < last)
        {
            double limit = Math.Min(st.SpanEnd[support], st.SupportEnd[last] - rules.TopEndCover);
            Side(rows, rules, step, +1, st.SupportEnd[support], limit, warnings, "phải",
                r => r.ReachesRight, r => r.RightCut, (r, x) => r.RightCut = x, r => r.RightThrough);
        }
    }

    /// <param name="direction">−1 for the span on the left (reaching further = smaller station), +1 for the right.</param>
    /// <param name="face">The support's face towards that span, from which a reach is rounded.</param>
    private static void Side(
        IReadOnlyList<TopRow> rows,
        KataDetailingRules rules,
        double step,
        int direction,
        double face,
        double limit,
        List<string> warnings,
        string side,
        Func<TopRow, bool> reaches,
        Func<TopRow, double> cut,
        Action<TopRow, double> setCut,
        Func<TopRow, double?> through)
    {
        // Distance into the span, measured from the face: larger = further.
        double Reach(double x) => direction * (x - face);

        TopRow? inner = null;
        double innerReach = double.NegativeInfinity;
        for (int i = rows.Count - 1; i >= 0; i--)
        {
            var row = rows[i];
            if (reaches(row) && inner is not null)
            {
                double wanted = rules.RoundUp(innerReach + step);
                double room = Reach(limit);
                double own = Reach(cut(row));
                if (own + 1e-6 < wanted)
                {
                    double reach = Math.Max(own, Math.Min(wanted, room));
                    setCut(row, face + direction * reach);
                    if (reach + 1e-6 < wanted)
                        warnings.Add(Held(row, inner, reach - innerReach, step, side));
                }
            }

            // The row's furthest bar end in this span: its cut, or the run-through end of its weaker side.
            double end = double.NegativeInfinity;
            if (reaches(row)) end = Reach(cut(row));
            if (through(row) is { } x) end = Math.Max(end, Reach(x));
            if (!double.IsNegativeInfinity(end))
            {
                inner = row;
                innerReach = end;
            }
        }
    }

    private static string Held(TopRow outer, TopRow inner, double beyond, double step, string side)
    {
        int outerRow = KataTopLayerStack.FirstRow + outer.Layer;
        int innerRow = KataTopLayerStack.FirstRow + inner.Layer;
        string by = beyond >= 0.0 ? $"chỉ vươn xa hơn hàng {innerRow} {beyond:0} mm" : $"ngắn hơn hàng {innerRow} {-beyond:0} mm";
        return $"{outer.Cell}: thép gia cường hàng {outerRow} {by} ở nhịp {side} (cần {step:0} mm theo G1) — nhịp không đủ chỗ cắt lệch tới mặt gối đối diện.";
    }
}
