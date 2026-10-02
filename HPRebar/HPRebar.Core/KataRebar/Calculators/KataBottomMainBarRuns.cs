using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// The bottom main bars (B12) of a beam whose spans may differ in depth (row 21), the top staying level, as Kata
/// draws them. Consecutive spans share one bar, cranked at 1:<see cref="KataDetailingRules.CrankSlope"/> from the face of
/// the shallower span into the deeper one, when Kata's beam-node detail allows it (<see cref="KataDetailingRules.Cranks"/>:
/// Ø ≥ 16 and (step − Ø) / support width ≤ 1/6). A steeper step — or a crank with no room, beside another crank or a
/// cantilever — cuts the bars at
/// that support: the deeper span's bar runs to the far face and bends up like an end anchorage (its leg kept clear of
/// the shallower bar and of the top bars), the shallower span's bar runs straight G3·d past the support face on its
/// own side, into the deeper span. A cantilever cut off this way keeps no bottom bar of its own.
/// </summary>
public static class KataBottomMainBarRuns
{
    /// <summary>Anchorages a run asks for: in an end support (with the top legs it must clear), or at a step.</summary>
    /// <param name="EndSupport">The bar end in end support 0 or the last one.</param>
    /// <param name="Step">The bar end bent up in an interior support: (support, outward, leg room).</param>
    /// <param name="TopRoom">Lowest centre a bottom leg may reach under the top bars over a support (z, mm).</param>
    public sealed record EndSolver(Func<int, KataBarEnd> EndSupport, Func<int, int, double, KataBarEnd> Step, Func<int, double> TopRoom);

    /// <summary>One bottom bar: spans <see cref="FirstSpan"/> to <see cref="LastSpan"/> (<see cref="Whole"/> = all of them), its path (X, Z) between the ends.</summary>
    public sealed record Run(
        int FirstSpan,
        int LastSpan,
        bool Whole,
        KataBarEnd Start,
        KataBarEnd End,
        IReadOnlyList<(double X, double Z)> Path)
    {
        public int FirstSupport => FirstSpan;
        public int LastSupport => LastSpan + 1;
    }

    public static IReadOnlyList<Run> Plan(KataBeamRebarSpec spec, KataDetailingRules rules, KataBeamStations st, double diameter, EndSolver ends, List<string> warnings)
    {
        int n = st.SpanCount;
        var runs = new List<Run>();
        if (n == 0) return runs;

        double Depth(int s) => spec.DepthOf(s);
        double Z(int s) => -Depth(s) + rules.BottomBarCentreDepth;
        double straight = rules.BottomAnchorageFactor * diameter;
        bool Cantilever(int s) => (s == 0 && st.SupportWidth[0] <= 0.0) || (s == n - 1 && st.SupportWidth[n] <= 0.0);

        // A crank must lie between the previous crank (or the span's start) and the end of the deeper span.
        var cut = new bool[n + 1];
        double reached = st.SpanStart[0];
        for (int k = 1; k < n; k++)
        {
            double step = Depth(k) - Depth(k - 1);
            if (Math.Abs(step) < 1e-6) continue;
            if (!rules.Cranks(step, st.SupportWidth[k], diameter))
            {
                cut[k] = true;
                reached = st.SpanStart[k];
                // Cut, the shallow bar runs on past the support beside the deep one, only the step apart.
                double gap = Math.Abs(step) - diameter;
                if (gap < rules.LayerGap(diameter, diameter))
                    warnings.Add($"Thép chủ dưới qua gối {k + 1}: bậc đáy {Math.Abs(step):0} mm bị cắt (Ø{diameter:0} < {rules.CrankMinDiameter:0} hoặc gối hẹp) — hai thanh chồng nhau chỉ cách {gap:0} mm, kiểm tra trong Revit.");
                continue;
            }

            var (x0, x1) = Crank(st, rules, k, step);
            if (Cantilever(k - 1) || Cantilever(k) || x0 < reached + 1.0 || x1 > st.SpanEnd[k] - 1.0)
            {
                cut[k] = true;
                reached = st.SpanStart[k];
                warnings.Add($"Thép chủ dưới qua gối {k + 1}: không đủ chỗ uốn chuyển cao độ 1:{rules.CrankSlope:0} ({Math.Abs(step):0} mm) — cắt và neo tại gối.");
                continue;
            }

            reached = x1;
        }

        cut[n] = true;
        int first = 0;
        for (int s = 1; s <= n; s++)
        {
            if (!cut[s]) continue;

            int last = s - 1;
            // A cantilever cut off from its neighbours keeps no bottom bar of its own.
            if (first == last && Cantilever(first) && n > 1) { first = s; continue; }

            KataBarEnd start = first == 0
                ? ends.EndSupport(0)
                : Depth(first) > Depth(first - 1)
                    ? ends.Step(first, -1, DeepLegRoom(ends, rules, first, Depth(first) - Depth(first - 1), Z(first), diameter))
                    : new KataBarEnd(Math.Max(st.SupportStart[0] + rules.BottomBarCentreDepth, st.SupportEnd[first] - straight), 0.0, 0.0);
            KataBarEnd end = last == n - 1
                ? ends.EndSupport(n)
                : Depth(last) > Depth(last + 1)
                    ? ends.Step(last + 1, +1, DeepLegRoom(ends, rules, last + 1, Depth(last) - Depth(last + 1), Z(last), diameter))
                    : new KataBarEnd(Math.Min(st.SupportEnd[n] - rules.BottomBarCentreDepth, st.SupportStart[last + 1] + straight), 0.0, 0.0);

            var path = new List<(double X, double Z)> { (start.X, Z(first)) };
            for (int k = first + 1; k <= last; k++)
            {
                double step = Depth(k) - Depth(k - 1);
                if (Math.Abs(step) < 1e-6) continue;

                var (x0, x1) = Crank(st, rules, k, step);
                path.Add((x0, Z(k - 1)));
                path.Add((x1, Z(k)));
            }

            path.Add((end.X, Z(last)));
            runs.Add(new Run(first, last, first == 0 && last == n - 1, start, end, path));
            first = s;
        }

        return runs;
    }

    /// <summary>The crank over support <paramref name="k"/>: from the shallower span's face into the deeper span.</summary>
    private static (double X0, double X1) Crank(KataBeamStations st, KataDetailingRules rules, int k, double step)
    {
        double run = rules.CrankSlope * Math.Abs(step);
        double x0 = step > 0.0 ? st.SupportStart[k] : st.SupportEnd[k] - run;
        return (x0, x0 + run);
    }

    /// <summary>
    /// Longest up-turned leg of the deeper span's bar at a cut step: below the shallower span's bar (one bar and a
    /// layer gap) and below the top bars over the support.
    /// </summary>
    private static double DeepLegRoom(EndSolver ends, KataDetailingRules rules, int support, double step, double zDeep, double diameter)
    {
        double underShallow = step - diameter - rules.LayerGap(diameter, diameter);
        double underTop = ends.TopRoom(support) - zDeep;
        return Math.Max(0.0, Math.Min(underShallow, underTop));
    }

    public static KataRebarCurve Bar(int id, double dia, double y, Run run)
    {
        var points = new List<Point3>();
        var path = run.Path;
        if (run.Start.IsBent) points.Add(new Point3(path[0].X, y, path[0].Z + run.Start.Leg));
        points.AddRange(path.Select(p => new Point3(p.X, y, p.Z)));
        var tail = path[path.Count - 1];
        if (run.End.IsBent) points.Add(new Point3(tail.X, y, tail.Z + run.End.Leg));

        bool bent = run.Start.IsBent || run.End.IsBent;
        return new KataRebarCurve
        {
            BarId = id,
            Role = KataBarRole.MainBottom,
            Diameter = dia,
            Layer = 1,
            Polyline = new Polyline3(points).Simplify(1.0),
            StartHookAngle = run.Start.IsBent ? HookAngle.Hook90 : HookAngle.None,
            EndHookAngle = run.End.IsBent ? HookAngle.Hook90 : HookAngle.None,
            StartHookLength = run.Start.Leg,
            EndHookLength = run.End.Leg,
            TransverseY = y,
            HostSpanIndex = -1,
            HostSupportIndex = -1,
            ShapeCode = run.Start.IsBent && run.End.IsBent ? "15a" : bent ? "05a" : "00",
            BarMark = "2",
            BarDescription = run.Whole
                ? "Thép chủ dưới"
                : run.FirstSpan == run.LastSpan
                ? $"Thép chủ dưới nhịp {run.FirstSpan + 1}"
                : $"Thép chủ dưới nhịp {run.FirstSpan + 1}–{run.LastSpan + 1}",
            DimA = run.End.X - run.Start.X,
            DimB = run.Start.Leg,
            DimC = run.End.Leg,
            DimR = bent ? 2.0 * dia : 0.0,
            SttCad = 2
        };
    }
}
