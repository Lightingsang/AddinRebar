using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Where the beam top changes level (row 19): over an interior support between spans of different tops, or at a
/// station inside a span joined over a support of no width.
/// </summary>
/// <param name="Support">The support, or -1 for a station inside a span.</param>
public sealed record KataTopTransition(int Support, double Start, double End, double FromTop, double ToTop)
{
    public bool IsStation => End - Start < 1e-6;
    public double Step => ToTop - FromTop;
    public double Middle => (Start + End) / 2.0;
}

/// <summary>
/// Puts the top bars, laid out as if the beam top were level, onto the real top (row 19), as Kata draws B01. A bar
/// crossing a change of level is cranked at 1:<see cref="KataDetailingRules.CrankSlope"/> when Kata's node rule allows it
/// (<see cref="KataDetailingRules.Cranks"/>, the crank centred in the support; at a station of no width it starts there
/// and runs into the next span); otherwise it is cut there: the bar of the higher side anchors in the support like an
/// end support (bending down), the bar of the lower side runs on straight past it into the higher span, overlapping
/// it by G3·d from its end, at its own level all along. A leg hanging from a dropped top drops with it, its foot kept a
/// bottom cover above the soffit.
/// </summary>
public static class KataTopProfile
{
    /// <summary>The largest change of top a station of no width cranks over (B01 at I: 50).</summary>
    public const double MaxStationCrank = 100.0;

    /// <summary>Tops closer than this are one level (mm).</summary>
    public const double LevelTolerance = 0.5;

    public static IReadOnlyList<KataTopTransition> Transitions(KataBeamRebarSpec spec, KataBeamStations st)
    {
        var transitions = new List<KataTopTransition>();
        for (int s = 0; s < st.SpanCount; s++)
        {
            if (s > 0)
            {
                double from = spec.TopAt(s - 1, spec.Spans[s - 1].Length);
                double to = spec.TopAt(s, 0.0);
                if (Math.Abs(to - from) > LevelTolerance)
                    transitions.Add(new KataTopTransition(s, st.SupportStart[s], st.SupportEnd[s], from, to));
            }

            double top = spec.Spans[s].TopDrop;
            foreach (var step in spec.Spans[s].TopSteps)
            {
                double x = st.SpanStart[s] + step.AtMm;
                if (Math.Abs(step.TopDrop - top) > LevelTolerance)
                    transitions.Add(new KataTopTransition(-1, x, x, top, step.TopDrop));
                top = step.TopDrop;
            }
        }

        return transitions;
    }

    /// <summary>True when the beam top is level all along: nothing to do.</summary>
    public static bool IsLevel(KataBeamRebarSpec spec) =>
        spec.Spans.All(s => Math.Abs(s.TopDrop - spec.Spans[0].TopDrop) <= LevelTolerance && s.TopSteps.Count == 0)
        && Math.Abs(spec.Spans.Count > 0 ? spec.Spans[0].TopDrop : 0.0) <= LevelTolerance;

    /// <summary>The top bars on the real top: cranked, cut, or moved down with it.</summary>
    public static List<KataRebarCurve> Drape(
        KataBeamRebarSpec spec, KataDetailingRules rules, KataBeamStations st, IReadOnlyList<KataRebarCurve> bars, List<string> warnings, ref int barId)
    {
        var draped = new List<KataRebarCurve>(bars.Count);
        if (IsLevel(spec))
        {
            draped.AddRange(bars);
            return draped;
        }

        var transitions = Transitions(spec, st);
        foreach (var bar in bars)
        {
            var cranked = transitions.Where(t => Cranks(rules, t, bar.Diameter)).ToList();
            var parts = Cut(spec, rules, st, bar, transitions.Where(t => Crosses(bar, t) && !Cranks(rules, t, bar.Diameter)));
            for (int i = 0; i < parts.Count; i++)
            {
                var part = OnTop(spec, rules, st, parts[i].Bar, parts[i].Level, cranked, warnings);
                draped.Add(i == 0 ? part : part with { BarId = barId++ });
            }
        }

        return draped;
    }

    /// <summary>Level of the top at <paramref name="x"/>; over a support between two levels, the side named by <paramref name="right"/>.</summary>
    public static double LevelAt(KataBeamRebarSpec spec, KataBeamStations st, double x, bool right)
    {
        int n = st.SpanCount;
        if (x <= st.SpanStart[0]) return spec.TopAt(0, 0.0);
        if (x >= st.SpanEnd[n - 1]) return spec.TopAt(n - 1, spec.Spans[n - 1].Length);

        for (int s = 0; s < n; s++)
        {
            if (x <= st.SpanEnd[s] + 1e-6)
            {
                if (x >= st.SpanStart[s] - 1e-6) return spec.TopAt(s, x - st.SpanStart[s]);
                // Inside support s, between span s - 1 and span s.
                return right ? spec.TopAt(s, 0.0) : spec.TopAt(s - 1, spec.Spans[s - 1].Length);
            }
        }

        return spec.TopAt(n - 1, spec.Spans[n - 1].Length);
    }

    private static bool Crosses(KataRebarCurve bar, KataTopTransition t)
    {
        double min = bar.Polyline.Points.Min(p => p.X), max = bar.Polyline.Points.Max(p => p.X);
        return min < t.Start - 1.0 && max > t.End + 1.0;
    }

    private static bool Cranks(KataDetailingRules rules, KataTopTransition t, double diameter) => t.IsStation
        ? Math.Abs(t.Step) <= MaxStationCrank
        : rules.Cranks(t.Step, t.End - t.Start, diameter);

    /// <summary>
    /// The crank of a bar over <paramref name="t"/>: centred in the support; at a station of no width on the higher side
    /// of it, so the bar never stands above the concrete (B01 at I, up from −50: from the station into J).
    /// </summary>
    private static (double X0, double X1) Crank(KataDetailingRules rules, KataTopTransition t)
    {
        double run = rules.CrankSlope * Math.Abs(t.Step);
        if (t.IsStation) return t.Step > 0.0 ? (t.Start, t.Start + run) : (t.Start - run, t.Start);
        double x0 = t.Start + Math.Max(0.0, (t.End - t.Start - run) / 2.0);
        return (x0, x0 + run);
    }

    /// <summary>
    /// The bar cut at every change of level it cannot crank over, each piece ending as Kata ends it there; a piece
    /// after a cut carries the level of its own side (null: the level under its start).
    /// </summary>
    private static List<(KataRebarCurve Bar, double? Level)> Cut(KataBeamRebarSpec spec, KataDetailingRules rules, KataBeamStations st,
        KataRebarCurve bar, IEnumerable<KataTopTransition> cuts)
    {
        var parts = new List<(KataRebarCurve Bar, double? Level)> { (bar, null) };
        foreach (var t in cuts.OrderBy(t => t.Start))
        {
            var (last, level) = parts[parts.Count - 1];
            parts.RemoveAt(parts.Count - 1);
            bool leftHigher = t.FromTop > t.ToTop;
            // The lower bar overlaps the anchored one by G3·d from its end. Main bars: d the beam's main bar (B11) when
            // larger, B01 at M laps its 3Ø20 by 750 = 30 × 25 (31550 − 750 = 30800). Additional bars: d the anchored
            // span's main bar when larger, B01 M14 2Ø16 by 600 = 30 × 20 (30960 drawn).
            int anchoredSpan = t.Support >= 0 ? (leftHigher ? t.Support - 1 : t.Support) : SpanAt(st, t.Start);
            var reference = last.Role == KataBarRole.MainTop ? spec.TopContinuous : spec.TopMainOf(anchoredSpan);
            double lap = Lap(rules, Math.Max(last.Diameter, reference.IsEmpty ? 0.0 : reference.Diameter));
            var anchored = Anchor(spec, rules, st, t.Support, leftHigher ? +1 : -1, last.Diameter);
            var leftEnd = leftHigher ? anchored : new KataBarEnd(anchored.X + lap, 0.0, 0.0);
            var rightEnd = leftHigher ? new KataBarEnd(anchored.X - lap, 0.0, 0.0) : anchored;
            parts.Add((KataBarSplit.Until(last, leftEnd, -1), level));
            parts.Add((KataBarSplit.From(last, rightEnd, -1), t.ToTop));
        }

        return parts;
    }

    private static double Lap(KataDetailingRules rules, double diameter) => rules.RoundUp(rules.BottomAnchorageFactor * diameter);

    private static KataBarEnd Anchor(KataBeamRebarSpec spec, KataDetailingRules rules, KataBeamStations st, int support, int outward, double d) =>
        KataMainBarLayout.Solve(st, rules, support, outward, rules.TopBarCentreDepth, rules.TopAnchorageFactor * d,
            rules.MinimumLegFactor * d, KataMainBarLayout.LegRoom(spec, rules, support), 0.0);

    /// <summary>The bar moved onto the top: every point by the level under it, cranks inserted, end legs kept above the soffit.</summary>
    private static KataRebarCurve OnTop(KataBeamRebarSpec spec, KataDetailingRules rules, KataBeamStations st, KataRebarCurve bar,
        double? level, IReadOnlyList<KataTopTransition> transitions, List<string> warnings)
    {
        var pts = bar.Polyline.Points;
        double min = pts.Min(p => p.X), max = pts.Max(p => p.X);
        double startLevel = level ?? LevelAt(spec, st, min, right: true);
        var cranks = transitions
            .Where(t => min < t.Start - 1.0 && max > t.End + 1.0)
            .OrderBy(t => t.Start)
            .Select(t => (Crank: Crank(rules, t), t.FromTop, t.ToTop))
            .ToList();

        double Offset(double x)
        {
            double level = startLevel;
            foreach (var (crank, from, to) in cranks)
            {
                if (x >= crank.X1) { level = to; continue; }
                if (x > crank.X0) return from + (to - from) * (x - crank.X0) / (crank.X1 - crank.X0);
                return from;
            }

            return level;
        }

        var knots = cranks.SelectMany(c => new[] { c.Crank.X0, c.Crank.X1 }).ToList();
        var moved = new List<Point3>();
        for (int i = 0; i < pts.Count; i++)
        {
            if (i > 0)
            {
                var q = pts[i - 1];
                var p = pts[i];
                foreach (double x in knots.Where(k => (q.X - k) * (pts[i].X - k) < 0.0).OrderBy(k => q.X < p.X ? k : -k))
                {
                    double f = (x - q.X) / (p.X - q.X);
                    moved.Add(new Point3(x, q.Y + f * (p.Y - q.Y), q.Z + f * (p.Z - q.Z) + Offset(x)));
                }
            }

            moved.Add(new Point3(pts[i].X, pts[i].Y, pts[i].Z + Offset(pts[i].X)));
        }

        double FootLimit(double x) => -spec.DepthOf(SpanAt(st, x)) + rules.BottomBarCentreDepth;
        KeepFeetAboveSoffit(moved, FootLimit(min), FootLimit(max));
        double startLeg = moved.Count > 1 && IsLeg(moved[0], moved[1]) ? moved[1].Z - moved[0].Z : 0.0;
        double endLeg = moved.Count > 1 && IsLeg(moved[moved.Count - 1], moved[moved.Count - 2]) ? moved[moved.Count - 2].Z - moved[moved.Count - 1].Z : 0.0;
        foreach (var (before, after) in new[] { (bar.StartHookLength, startLeg), (bar.EndHookLength, endLeg) })
        {
            if (before > 0.0 && after < before - 1.0)
                warnings.Add($"{bar.BarDescription}: chân neo dưới đỉnh hạ thấp chỉ còn {after:0} mm (cần {before:0}) — kiểm tra neo.");
        }

        var simplified = new Polyline3(moved).Simplify(1.0);
        bool startBent = bar.StartHookLength > 0.0 && startLeg > 1.0, endBent = bar.EndHookLength > 0.0 && endLeg > 1.0;
        return bar with
        {
            Polyline = simplified,
            StartHookAngle = startBent ? bar.StartHookAngle : HookAngle.None,
            EndHookAngle = endBent ? bar.EndHookAngle : HookAngle.None,
            StartHookLength = startBent ? startLeg : 0.0,
            EndHookLength = endBent ? endLeg : 0.0,
            DimA = simplified.Points.Max(p => p.X) - simplified.Points.Min(p => p.X),
            DimB = startBent ? startLeg : 0.0,
            DimC = endBent ? endLeg : 0.0
        };
    }

    private static bool IsLeg(Point3 foot, Point3 bend) => Math.Abs(foot.X - bend.X) < 1e-6 && foot.Z < bend.Z;

    /// <summary>A leg hanging from a dropped top drops with it, but its foot stays a bottom cover above the soffit.</summary>
    private static void KeepFeetAboveSoffit(List<Point3> points, double startLimit, double endLimit)
    {
        if (points.Count < 3) return;
        if (IsLeg(points[0], points[1]) && points[0].Z < startLimit)
            points[0] = new Point3(points[0].X, points[0].Y, Math.Min(points[1].Z, startLimit));
        int n = points.Count - 1;
        if (IsLeg(points[n], points[n - 1]) && points[n].Z < endLimit)
            points[n] = new Point3(points[n].X, points[n].Y, Math.Min(points[n - 1].Z, endLimit));
    }

    /// <summary>The span under station <paramref name="x"/> (an end support counts with its span).</summary>
    private static int SpanAt(KataBeamStations st, double x)
    {
        for (int s = 0; s < st.SpanCount; s++)
            if (x <= st.SpanEnd[s]) return s;
        return st.SpanCount - 1;
    }
}
