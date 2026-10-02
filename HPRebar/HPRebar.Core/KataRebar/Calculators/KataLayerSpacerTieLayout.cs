using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// C ties under an inner layer of additional bars, as the Kata drawing puts them: wherever a section cuts at
/// least <see cref="KataDetailingRules.LayerTieMinBarCount"/> bars of a support's inner top layer (rows 14-16)
/// or of a span's upper bottom layer (row 17), a stirrup-sized tie runs under the layer with its 180° hooks round
/// the two outer bars of that section. The bars are counted section by section, so the two halves of a
/// "left;right" cell, or the layers of two neighbouring supports meeting in a short span, are judged by what a
/// section really holds. Ties keep one clearance inside the bars' ends, span by span (never inside a support),
/// spaced by J7/I8.
/// </summary>
public static class KataLayerSpacerTieLayout
{
    public const string ZoneName = "Thanh C kê";

    public static List<KataBarSet> Build(
        KataBeamRebarSpec spec,
        KataDetailingRules rules,
        KataBeamStations st,
        IReadOnlyList<KataStirrupZoneResult> hoopZones,
        IReadOnlyList<KataRebarCurve> extraTop,
        IReadOnlyList<KataRebarCurve> extraBottom,
        IReadOnlyList<KataRebarCurve> otherBars,
        List<string> warnings)
    {
        var sets = new List<KataBarSet>();
        if (rules.StirrupDiameter <= 0.0) return sets;

        double clear = KataTieStations.Clearance(rules);
        var everyBar = extraTop.Concat(extraBottom).Concat(otherBars).ToList();
        var layers = extraTop.Where(b => b.Layer >= 2).GroupBy(b => (Top: true, b.Layer))
            .Concat(extraBottom.Where(b => b.Layer == 2).GroupBy(b => (Top: false, b.Layer)));

        foreach (var layer in layers)
        {
            // Where each bar can hold a tie: its level run, one clearance inside both ends.
            var reach = layer.Select(b => (Bar: b, Level: Level(b)))
                .Select(r => new Reach(r.Bar, r.Level.X0 + clear, r.Level.X1 - clear, r.Level.Z))
                .Where(r => r.To > r.From)
                .ToList();

            for (int s = 0; s < st.SpanCount; s++)
            {
                foreach (var (start, end, a, b) in Stretches(reach, st.SpanStart[s], st.SpanEnd[s], rules.LayerTieMinBarCount))
                {
                    var (top, number) = layer.Key;
                    int index = top ? a.Bar.HostSupportIndex : a.Bar.HostSpanIndex;
                    string mark = top ? $"3.{index + 1}.{number}C" : $"4.{index + 1}.{number}C";
                    string description = top
                        ? $"Thanh C kê gia cường gối {index + 1} lớp {number}"
                        : $"Thanh C kê gia cường nhịp {index + 1} lớp {number}";
                    double z = Math.Min(a.Z, b.Z);
                    double wrapped = Math.Max(a.Bar.Diameter, b.Bar.Diameter);

                    var runs = KataTieStations.InSpan(rules, st, hoopZones, s, start, end);
                    foreach (var run in runs)
                        sets.Add(Tie(rules, s, a.Bar.TransverseY, b.Bar.TransverseY, z, wrapped, mark, description, run));
                    if (runs.Count > 0)
                        CheckClearance(warnings, rules, everyBar, layer, mark, runs.First().Stations[0], runs.Last().Stations.Last(), z);
                }
            }
        }

        return sets;
    }

    private sealed record Reach(KataRebarCurve Bar, double From, double To, double Z);

    /// <summary>
    /// The stretches of [spanStart, spanEnd] where every section holds at least <paramref name="minBars"/> bars of
    /// the layer, with the same two outer bars throughout.
    /// </summary>
    private static IEnumerable<(double Start, double End, Reach Outer0, Reach Outer1)> Stretches(
        IReadOnlyList<Reach> reach, double spanStart, double spanEnd, int minBars)
    {
        var cuts = reach.SelectMany(r => new[] { r.From, r.To })
            .Append(spanStart).Append(spanEnd)
            .Where(x => x >= spanStart - 1e-6 && x <= spanEnd + 1e-6)
            .OrderBy(x => x)
            .ToList();

        (double Start, double End, Reach A, Reach B)? open = null;
        for (int i = 1; i < cuts.Count; i++)
        {
            double from = cuts[i - 1], to = cuts[i];
            if (to - from < 1e-6) continue;

            double mid = (from + to) / 2.0;
            var present = reach.Where(r => r.From <= mid && r.To >= mid).OrderBy(r => r.Bar.TransverseY).ToList();
            bool holds = present.Count >= minBars && present[present.Count - 1].Bar.TransverseY - present[0].Bar.TransverseY >= 1.0;
            if (holds && open is { } o && ReferenceEquals(o.A, present[0]) && ReferenceEquals(o.B, present[present.Count - 1]))
            {
                open = (o.Start, to, o.A, o.B);
                continue;
            }

            if (open is { } done) yield return done;
            open = holds ? (from, to, present[0], present[present.Count - 1]) : null;
        }

        if (open is { } last) yield return last;
    }

    private static KataBarSet Tie(KataDetailingRules rules, int span, double ya, double yb, double z, double wrapped, string mark, string description, KataTieStations.Run run)
    {
        double x0 = run.Stations[0];
        return new KataBarSet
        {
            BarMark = mark,
            Description = description,
            Role = KataBarRole.CrossTie,
            Diameter = rules.StirrupDiameter,
            SpanIndex = span,
            ZoneName = ZoneName,
            // The outer bars' centres: the tie is laid out round them where the bar type's bend radius is known.
            Shape = new Polyline3(new List<Point3> { new(x0, ya, z), new(x0, yb, z) }),
            WrapEnds = true,
            WrapOffset = new Point3(0.0, 0.0, -1.0),
            WrappedBarDiameter = wrapped,
            Stations = run.Stations,
            Spacing = run.Spacing,
            HookAngle = rules.CrossTieHookAngle,
            HookFactor = rules.CrossTieHookFactor,
            HookToward = new Point3(x0, 0.0, z)
        };
    }

    /// <summary>
    /// Reports a bar under the layer that the tie would touch between its first and last station: the tie hangs
    /// about one bend radius (taken as two tie diameters, the drawing's 16 mm for Ø8) plus half its own diameter
    /// below the layer's centres.
    /// </summary>
    private static void CheckClearance(List<string> warnings, KataDetailingRules rules, IReadOnlyList<KataRebarCurve> bars,
        IEnumerable<KataRebarCurve> layer, string mark, double lo, double hi, double z)
    {
        var own = new HashSet<KataRebarCurve>(layer);
        double tieBottom = z - 2.5 * rules.StirrupDiameter;
        foreach (var bar in bars.Where(b => !own.Contains(b)))
        {
            var points = bar.Polyline.Points;
            for (int i = 1; i < points.Count; i++)
            {
                var a = points[i - 1];
                var b = points[i];
                if (Math.Abs(a.Z - b.Z) > 1.0) continue;
                if (Math.Max(a.X, b.X) < lo || Math.Min(a.X, b.X) > hi) continue;
                double face = a.Z + bar.Diameter / 2.0;
                if (a.Z >= z - 1.0 || face <= tieBottom + 1e-6) continue;

                string warning = $"Thanh C kê {mark}: thanh C Ø{rules.StirrupDiameter:0} ôm dưới lớp thép (≈ {2.5 * rules.StirrupDiameter:0} mm dưới tâm) chạm thanh {bar.BarMark} bên dưới — kiểm tra trong Revit.";
                if (!warnings.Contains(warning)) warnings.Add(warning);
                return;
            }
        }
    }

    /// <summary>The bar's longest level run: where it lies, from x0 to x1 at height z.</summary>
    private static (double X0, double X1, double Z) Level(KataRebarCurve bar)
    {
        var points = bar.Polyline.Points;
        (double X0, double X1, double Z) best = (0.0, 0.0, points.Count > 0 ? points[0].Z : 0.0);
        double longest = -1.0;
        for (int i = 1; i < points.Count; i++)
        {
            var a = points[i - 1];
            var b = points[i];
            if (Math.Abs(a.Z - b.Z) > 1.0) continue;
            double length = Math.Abs(b.X - a.X);
            if (length <= longest) continue;
            longest = length;
            best = (Math.Min(a.X, b.X), Math.Max(a.X, b.X), a.Z);
        }

        return best;
    }
}
