using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Three stirrup zones per span, as Kata draws them (T2-DY7, T2-DY14): a dense zone at each support from the
/// first-stirrup offset to the zone length (0.25 × clear span by default, rounded up to 50 mm, never more than half the
/// span) from the face, and a middle zone from one mid-span spacing past the last dense stirrup to one before the
/// first of the other dense zone. Each zone is spaced evenly at its spacing or a little less, so it ends exactly where
/// Kata ends it (DY7 span 1: 500…1850 | 2050…4350 | 4550…5900 from the outer face of C). When the dense zones fill
/// the span there is no sparse middle zone: a right-zone stirrup that would sit on the last left-zone one is dropped,
/// and a leftover gap wider than the dense spacing is closed with evenly spaced dense stirrups. A cantilever span
/// gets one uniform zone. Each hoop is as wide as its span (row 20) and reaches from the soffit to the top over it
/// (row 19); a zone running over a change of top inside a span (a joined support of no width) is cut there, each
/// part ending the first-stirrup offset from it and spaced evenly again (B01 at I: 19950…20550 | 20650…22750).
/// </summary>
public static class KataStirrupZoneLayout
{
    public static (List<KataStirrupZoneResult> Zones, List<KataRebarCurve> Stirrups) Build(
        KataBeamRebarSpec spec,
        KataDetailingRules rules,
        KataBeamStations st,
        ref int barId)
    {
        var zones = new List<KataStirrupZoneResult>();
        var stirrups = new List<KataRebarCurve>();

        // G6 = 0 means no stirrups; the rules still reserve a default stirrup when placing the main bars.
        if (spec.GlobalStirrup.Diameter <= 0.0)
            return (zones, stirrups);

        double b = rules.StirrupCover;
        double ds = rules.StirrupDiameter;

        for (int s = 0; s < st.SpanCount; s++)
        {
            double ln = spec.Spans[s].Length;
            if (ln <= 0.0) continue;

            // Each span's hoop is as deep as that span (row 21) and as wide (row 20).
            double depth = spec.DepthOf(s);
            double width = spec.WidthOf(s);
            double yMin = -width / 2.0 + b + ds / 2.0;
            double yMax = width / 2.0 - b - ds / 2.0;
            double zBot = -depth + b + ds / 2.0;

            var stSpec = spec.Spans[s].StirrupOverride ?? spec.GlobalStirrup;
            double sDense = stSpec.SupportSpacing > 0.0 ? stSpec.SupportSpacing : 150.0;
            double sSparse = stSpec.MidspanSpacing > 0.0 ? stSpec.MidspanSpacing : 200.0;
            double sEnd = stSpec.EndSupportSpacing ?? sDense;
            double sCant = stSpec.CantileverSpacing > 0.0 ? stSpec.CantileverSpacing : 150.0;

            bool isCantilever = (s == 0 && st.IsLeftCantilever) || (s == st.SpanCount - 1 && st.IsRightCantilever);
            var runs = isCantilever
                ? new List<(string Name, double Spacing, List<double> Stations)> { ("Console", sCant, CantileverStations(st, s, ln, sCant, rules)) }
                : ThreeZones(st, s, ln, depth, sDense, sSparse, sEnd, rules);

            // The outer closed hoop; the inner stirrups follow its zones (KataInnerStirrupLayout).
            {
                foreach (var (name, spacing, stations) in KataJointStirrups.Apply(spec, rules, st, s, AtTopSteps(spec, st, s, runs, rules.FirstStirrupOffset).ToList()))
                {
                    if (stations.Count == 0) continue;
                    double top = spec.TopAt(s, (stations[0] + stations[stations.Count - 1]) / 2.0 - st.SpanStart[s]);
                    var box = new Box(
                        Width: Math.Max(0.0, width - 2.0 * b),
                        Height: Math.Max(0.0, depth + top - 2.0 * b),
                        MinY: -width / 2.0 + b,
                        MinZ: -depth + b);
                    double zTop = top - b - ds / 2.0;

                    zones.Add(new KataStirrupZoneResult
                    {
                        SpanIndex = s,
                        ZoneIndex = isCantilever ? 0 : ZoneIndexOf(name),
                        ZoneName = name,
                        StartStationX = stations[0],
                        EndStationX = stations[stations.Count - 1],
                        Spacing = stations.Count > 1 ? (stations[stations.Count - 1] - stations[0]) / (stations.Count - 1) : spacing,
                        NominalSpacing = spacing,
                        Count = stations.Count,
                        Stations = stations,
                        OutToOutWidth = box.Width,
                        OutToOutHeight = box.Height,
                        BoxMinY = box.MinY,
                        BoxMinZ = box.MinZ,
                        StirrupType = KataStirrupShapeType.ClosedHoop,
                        BarMark = KataStirrupCurveFactory.MarkOf(KataStirrupShapeType.ClosedHoop)
                    });

                    foreach (double x in stations)
                        stirrups.Add(KataStirrupCurveFactory.Create(KataStirrupShapeType.ClosedHoop, x, yMin, yMax, zTop, zBot, ds, barId++, s));
                }
            }
        }

        return (zones, stirrups);
    }

    /// <summary>The zones cut where the top changes inside the span, each part spaced evenly up to the cut.</summary>
    private static IEnumerable<(string Name, double Spacing, List<double> Stations)> AtTopSteps(
        KataBeamRebarSpec spec, KataBeamStations st, int s, List<(string Name, double Spacing, List<double> Stations)> runs, double offset)
    {
        var cuts = spec.Spans[s].TopSteps.Select(step => st.SpanStart[s] + step.AtMm).ToList();
        foreach (var run in runs)
        {
            var stations = run.Stations;
            int part = 0;
            foreach (double x in cuts.Where(x => stations.Count > 0 && x > stations[0] - 1e-6 && x < stations[stations.Count - 1] + 1e-6))
            {
                // Each part keeps the first-stirrup offset from the step; a part with no room left has no stirrup.
                double first = stations[0], last = stations[stations.Count - 1];
                if (x - offset >= first) yield return (PartName(run.Name, part++), run.Spacing, Even(first, x - offset, run.Spacing));
                stations = x + offset <= last ? Even(x + offset, last, run.Spacing) : new List<double>();
            }

            yield return (PartName(run.Name, part), run.Spacing, stations);
        }
    }

    /// <summary>The parts of a zone cut at a step are told apart by name (removal and numbering key on it).</summary>
    private static string PartName(string name, int part) => part == 0 ? name : $"{name} ({part + 1})";

    private static int ZoneIndexOf(string name) => name switch
    {
        LeftZone => 0,
        MiddleZone => 1,
        FillerZone => 1,
        RightZone => 2,
        _ => 0
    };

    private const string LeftZone = "Gối trái";
    private const string MiddleZone = "Giữa nhịp";
    private const string FillerZone = "Giữa nhịp (đai dày)";
    private const string RightZone = "Gối phải";

    private static List<(string, double, List<double>)> ThreeZones(
        KataBeamStations st, int s, double ln, double height, double sDense, double sSparse, double sEnd, KataDetailingRules rules)
    {
        double offset = rules.FirstStirrupOffset;
        double endZone = Math.Min(ln / 2.0, Math.Ceiling(rules.DenseZoneLength(ln, height) / ZoneRound - 1e-9) * ZoneRound);

        // Zones that would leave less than a dense spacing between them meet: a 250 mm span is one dense run.
        bool zonesMeet = ln - 2.0 * endZone < Math.Min(sDense, sEnd) - 1e-6;
        List<double> left, right;
        if (zonesMeet)
        {
            // Dense from face to face: each zone at its own spacing from its face, the leftover closed below.
            left = Grid(st.SpanStart[s] + offset, endZone - offset, sDense, 1.0);
            right = Grid(st.SpanEnd[s] - offset, endZone - offset, sEnd, -1.0);
            right.Reverse();
        }
        else
        {
            left = Even(st.SpanStart[s] + offset, st.SpanStart[s] + Math.Max(endZone, offset), sDense);
            right = Even(st.SpanEnd[s] - Math.Max(endZone, offset), st.SpanEnd[s] - offset, sEnd);
        }

        double firstRight = right[0];

        double lastLeft = left[left.Count - 1];
        // Zones that meet in the middle: a right stirrup closer than half a spacing to the last left one is the
        // same stirrup twice.
        double tooClose = 0.5 * Math.Min(sDense, sEnd);
        while (right.Count > 0 && right[0] - lastLeft < tooClose) right.RemoveAt(0);
        if (right.Count > 0) firstRight = right[0];

        double gap = firstRight - lastLeft;
        var middle = new List<double>();
        double middleSpacing = sSparse;
        string middleName = MiddleZone;
        if (zonesMeet && right.Count > 0)
        {
            // Dense from face to face: the two zones each start at their own face, so the leftovers can meet
            // in a gap up to two spacings wide. Evenly spaced dense stirrups close it, never wider than the
            // smaller dense spacing.
            double dense = Math.Min(sDense, sEnd);
            int fill = (int)Math.Ceiling(gap / dense - 1e-9) - 1;
            if (fill > 0)
            {
                // Dense stirrups, labelled with the dense spacing they keep to.
                middleSpacing = dense;
                middleName = FillerZone;
                for (int i = 1; i <= fill; i++) middle.Add(lastLeft + i * gap / (fill + 1));
            }
        }
        else if (right.Count > 0 && gap >= 2.5 * sSparse - 1e-6)
        {
            // One mid-span spacing on from each dense zone, evenly spaced between.
            middle = Even(lastLeft + sSparse, firstRight - sSparse, sSparse);
        }
        else if (right.Count > 0 && gap >= 2.0 * sSparse - 1e-6)
        {
            // Room for one stirrup a spacing in from each zone but not for two apart: as few as keep the whole gap at
            // the mid-span spacing, evenly spread (never two hoops a few millimetres apart).
            int count = (int)Math.Ceiling(gap / sSparse - 1e-9) - 1;
            for (int i = 1; i <= count; i++) middle.Add(lastLeft + i * gap / (count + 1));
        }
        else if (right.Count > 0 && gap > sSparse + 1e-6)
        {
            // Too narrow for two transitions: a single stirrup in its middle.
            middle.Add((lastLeft + firstRight) / 2.0);
        }

        return new List<(string, double, List<double>)>
        {
            (LeftZone, sDense, left),
            (middleName, middleSpacing, middle),
            (RightZone, sEnd, right)
        };
    }

    /// <summary>Kata rounds a dense zone's length up to this step (DY7: L0/4 = 1375 → 1400).</summary>
    private const double ZoneRound = 50.0;

    /// <summary>Stirrups at exactly <paramref name="spacing"/> from <paramref name="from"/> (stepping by <paramref name="direction"/>), as many as fit in <paramref name="length"/>.</summary>
    private static List<double> Grid(double from, double length, double spacing, double direction)
    {
        int intervals = (int)Math.Floor(Math.Max(0.0, length) / spacing + 1e-9);
        var stations = new List<double>(intervals + 1);
        for (int i = 0; i <= intervals; i++) stations.Add(from + direction * i * spacing);
        return stations;
    }

    /// <summary>Stirrups from <paramref name="from"/> to <paramref name="to"/>, as few as keep them at most <paramref name="spacing"/> apart.</summary>
    internal static List<double> Even(double from, double to, double spacing)
    {
        double length = to - from;
        if (length < 1.0) return new List<double> { from };
        int intervals = Math.Max(1, (int)Math.Ceiling(length / spacing - 1e-9));
        var stations = new List<double>(intervals + 1);
        for (int i = 0; i <= intervals; i++) stations.Add(from + length * i / intervals);
        return stations;
    }

    /// <summary>
    /// A console's stirrups: from the first-stirrup offset past the support face to the same offset short of where the
    /// top bars stop (a from the tip), spaced evenly at G9 or a little less (B01 N: 31650…33500 a150).
    /// </summary>
    private static List<double> CantileverStations(KataBeamStations st, int s, double ln, double spacing, KataDetailingRules rules)
    {
        double tipOffset = rules.TopEndCover + rules.FirstStirrupOffset;
        bool left = s == 0 && st.IsLeftCantilever;
        double from = st.SpanStart[s] + (left ? tipOffset : rules.FirstStirrupOffset);
        double to = st.SpanStart[s] + ln - (left ? rules.FirstStirrupOffset : tipOffset);
        return to < from ? new List<double>() : Even(from, to, spacing);
    }

    private readonly record struct Box(double Width, double Height, double MinY, double MinZ);
}
