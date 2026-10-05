using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Stirrups round a load resting on a span (a beam framing into it, a stub column standing on it), as Kata draws B01:
/// <see cref="KataDetailingRules.JointStirrupCount"/> joint stirrups at <see cref="KataDetailingRules.JointStirrupSpacing"/>
/// on each face, the first <see cref="KataDetailingRules.FirstStirrupOffset"/> from it (mid-D: 4850…5050 | 5550…5750
/// round 5100…5500), and the span's own stirrups kept off the load and stopped one of their spacings short of the joint
/// stirrups, spaced evenly again up to there (3200…4650 | 5950…8000 at a200). A load whose face is within
/// <see cref="KataDetailingRules.JointNearSupportMm"/> of one support face has all its joint stirrups on the span side.
/// Loads whose joint stirrups would meet are one group: their stirrups are laid once.
/// </summary>
public static class KataJointStirrups
{
    /// <summary>Name of a zone of joint stirrups ("trái" / "phải" and a number when a span has several groups).</summary>
    public const string ZoneName = "Đai gia cường nút";

    public static bool IsJointZone(string zoneName) => zoneName.StartsWith(ZoneName, StringComparison.Ordinal);

    private sealed record Group(double Lo, double Hi, List<double> Stations);

    /// <summary>The span's zones cut round its loads, followed by the joint stirrups of each group of loads.</summary>
    public static IEnumerable<(string Name, double Spacing, List<double> Stations)> Apply(
        KataBeamRebarSpec spec, KataDetailingRules rules, KataBeamStations st, int s,
        IReadOnlyList<(string Name, double Spacing, List<double> Stations)> runs)
    {
        var groups = Groups(rules, st, s, spec.Spans[s].Loads);
        if (groups.Count == 0) return runs;

        var parts = new List<(string Name, double Spacing, List<double> Stations)>();
        foreach (var run in runs)
        {
            var pieces = new List<List<double>> { run.Stations };
            foreach (var g in groups)
                pieces = pieces.SelectMany(p => Cut(p, g.Lo - run.Spacing, g.Hi + run.Spacing, run.Spacing, rules.JointStirrupSpacing)).ToList();
            for (int k = 0; k < pieces.Count; k++)
                if (pieces[k].Count > 0) parts.Add((k == 0 ? run.Name : $"{run.Name} [{k + 1}]", run.Spacing, pieces[k]));
        }

        CloseGaps(parts, groups, rules.JointStirrupSpacing);

        for (int i = 0; i < groups.Count; i++)
        {
            string number = groups.Count > 1 ? $" {i + 1}" : "";
            var stretches = Stretches(groups[i].Stations, rules.JointStirrupSpacing);
            for (int k = 0; k < stretches.Count; k++)
            {
                string side = stretches.Count == 2 ? (k == 0 ? " trái" : " phải") : stretches.Count > 2 ? $" ({k + 1})" : "";
                parts.Add(($"{ZoneName}{number}{side}", rules.JointStirrupSpacing, stretches[k]));
            }
        }

        return parts;
    }

    /// <summary>
    /// The joint stirrups of each load, kept inside the span's first-stirrup offsets; loads whose stirrups come within
    /// one joint spacing of each other are one group. Each group covers its loads and their joint stirrups.
    /// </summary>
    private static List<Group> Groups(KataDetailingRules rules, KataBeamStations st, int s, IReadOnlyList<KataSpanLoad> loads)
    {
        var groups = new List<Group>();
        foreach (var load in loads.OrderBy(l => l.AtMm))
        {
            double centre = st.SpanStart[s] + load.AtMm;
            double leftFace = centre - load.WidthMm / 2.0, rightFace = centre + load.WidthMm / 2.0;
            var stations = Stations(rules, st, s, leftFace, rightFace);
            double lo = Math.Min(leftFace, stations.DefaultIfEmpty(leftFace).Min());
            double hi = Math.Max(rightFace, stations.DefaultIfEmpty(rightFace).Max());

            var last = groups.Count > 0 ? groups[groups.Count - 1] : null;
            if (last is not null && lo <= last.Hi + rules.JointStirrupSpacing + 1e-6)
                groups[groups.Count - 1] = new Group(last.Lo, Math.Max(last.Hi, hi), Dedupe(last.Stations.Concat(stations), rules.JointStirrupSpacing));
            else
                groups.Add(new Group(lo, hi, Dedupe(stations, rules.JointStirrupSpacing)));
        }

        return groups;
    }

    private static List<double> Stations(KataDetailingRules rules, KataBeamStations st, int s, double leftFace, double rightFace)
    {
        double lo = st.SpanStart[s] + rules.FirstStirrupOffset, hi = st.SpanEnd[s] - rules.FirstStirrupOffset;
        int n = Math.Max(0, rules.JointStirrupCount);
        bool nearLeft = leftFace - st.SpanStart[s] <= rules.JointNearSupportMm;
        bool nearRight = st.SpanEnd[s] - rightFace <= rules.JointNearSupportMm;
        // Near one support all go to the span side; near both there is no span side, so each keeps its own.
        int leftCount = nearLeft && !nearRight ? 0 : nearRight && !nearLeft ? 2 * n : n;
        int rightCount = nearRight && !nearLeft ? 0 : nearLeft && !nearRight ? 2 * n : n;

        return Enumerable.Range(0, leftCount).Select(i => leftFace - rules.FirstStirrupOffset - i * rules.JointStirrupSpacing)
            .Concat(Enumerable.Range(0, rightCount).Select(i => rightFace + rules.FirstStirrupOffset + i * rules.JointStirrupSpacing))
            .Where(x => x >= lo - 1e-6 && x <= hi + 1e-6)
            .ToList();
    }

    /// <summary>Stations in order, any closer than half a spacing to the one before dropped.</summary>
    private static List<double> Dedupe(IEnumerable<double> stations, double spacing)
    {
        var kept = new List<double>();
        foreach (double x in stations.OrderBy(x => x))
            if (kept.Count == 0 || x - kept[kept.Count - 1] >= spacing / 2.0) kept.Add(x);
        return kept;
    }

    /// <summary>The stations split where the step between them changes: each stretch is one evenly spaced set.</summary>
    private static List<List<double>> Stretches(List<double> stations, double spacing)
    {
        var stretches = new List<List<double>>();
        foreach (double x in stations)
        {
            var current = stretches.Count > 0 ? stretches[stretches.Count - 1] : null;
            bool continues = current is not null
                && (current.Count == 1 ? x - current[0] <= spacing + 1.0 : Math.Abs(x - current[current.Count - 1] - (current[1] - current[0])) <= 1.0);
            if (continues) current!.Add(x);
            else stretches.Add(new List<double> { x });
        }

        return stretches;
    }

    /// <summary>
    /// The stations of one evenly spaced run outside [lo, hi], each remaining part spaced evenly up to it; a part
    /// shorter than the joint spacing keeps its outer station only (two stirrups closer than that are one).
    /// </summary>
    private static IEnumerable<List<double>> Cut(List<double> stations, double lo, double hi, double spacing, double closest)
    {
        if (stations.Count == 0) yield break;
        double first = stations[0], last = stations[stations.Count - 1];
        if (hi < first - 1e-6 || lo > last + 1e-6)
        {
            yield return stations;
            yield break;
        }

        // A short part keeps its outer station, the one that was already there (beside a support or the next zone).
        if (lo >= first - 1e-6) yield return Part(first, Math.Min(lo, last), spacing, closest, first);
        if (hi <= last + 1e-6) yield return Part(Math.Max(hi, first), last, spacing, closest, last);
    }

    private static List<double> Part(double from, double to, double spacing, double closest, double keep) =>
        to - from < closest - 1e-6 ? new List<double> { keep } : KataStirrupZoneLayout.Even(from, to, spacing);

    /// <summary>
    /// A part ending more than its spacing before a group's joint stirrups (the window opened between two zones) is
    /// stretched up to one spacing from them, and one starting more than its spacing after them is stretched back.
    /// </summary>
    private static void CloseGaps(List<(string Name, double Spacing, List<double> Stations)> parts, List<Group> groups, double closest)
    {
        foreach (var g in groups)
        {
            int before = -1, after = -1;
            for (int i = 0; i < parts.Count; i++)
            {
                var p = parts[i].Stations;
                if (p[p.Count - 1] < g.Lo && (before < 0 || p[p.Count - 1] > Last(parts[before].Stations))) before = i;
                if (p[0] > g.Hi && (after < 0 || p[0] < parts[after].Stations[0])) after = i;
            }

            if (before >= 0)
            {
                var (name, spacing, p) = parts[before];
                double target = g.Lo - spacing;
                if (g.Lo - Last(p) > spacing + 1.0 && target > Last(p)) parts[before] = (name, spacing, Part(p[0], target, spacing, closest, p[0]));
            }

            if (after >= 0)
            {
                var (name, spacing, p) = parts[after];
                double target = g.Hi + spacing;
                if (p[0] - g.Hi > spacing + 1.0 && target < p[0]) parts[after] = (name, spacing, Part(target, Last(p), spacing, closest, Last(p)));
            }
        }
    }

    private static double Last(List<double> stations) => stations[stations.Count - 1];
}
