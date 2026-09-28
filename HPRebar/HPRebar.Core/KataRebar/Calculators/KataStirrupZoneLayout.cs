using System;
using System.Collections.Generic;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Three stirrup zones per span: a dense zone at each support (length = clear span × end-zone fraction,
/// first stirrup at the first-stirrup offset from the face) and a middle zone at exactly the mid-span
/// spacing, centred in the gap so each transition gap lies between half and one mid-span spacing.
/// A cantilever span gets one uniform zone.
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
        var box = new Box(
            Width: Math.Max(0.0, spec.Width - 2.0 * b),
            Height: Math.Max(0.0, spec.Height - 2.0 * b),
            MinY: -spec.Width / 2.0 + b,
            MinZ: -spec.Height + b);

        double yMin = box.MinY + ds / 2.0;
        double yMax = spec.Width / 2.0 - b - ds / 2.0;
        double zTop = -b - ds / 2.0;
        double zBot = box.MinZ + ds / 2.0;

        for (int s = 0; s < st.SpanCount; s++)
        {
            double ln = spec.Spans[s].Length;
            if (ln <= 0.0) continue;

            var stSpec = spec.Spans[s].StirrupOverride ?? spec.GlobalStirrup;
            double sDense = stSpec.SupportSpacing > 0.0 ? stSpec.SupportSpacing : 150.0;
            double sSparse = stSpec.MidspanSpacing > 0.0 ? stSpec.MidspanSpacing : 200.0;
            double sEnd = stSpec.EndSupportSpacing ?? sDense;
            double sCant = stSpec.CantileverSpacing > 0.0 ? stSpec.CantileverSpacing : 150.0;
            var branches = stSpec.Branches.Count > 0 ? stSpec.Branches : new[] { KataStirrupBranchSpec.Outer };

            bool isCantilever = (s == 0 && st.IsLeftCantilever) || (s == st.SpanCount - 1 && st.IsRightCantilever);
            var runs = isCantilever
                ? new List<(string Name, double Spacing, List<double> Stations)> { ("Console", sCant, CantileverStations(st, s, ln, sCant, rules)) }
                : ThreeZones(st, s, ln, sDense, sSparse, sEnd, rules);

            foreach (var branch in branches)
            {
                for (int z = 0; z < runs.Count; z++)
                {
                    var (name, spacing, stations) = runs[z];
                    if (stations.Count == 0) continue;

                    zones.Add(new KataStirrupZoneResult
                    {
                        SpanIndex = s,
                        ZoneIndex = isCantilever ? 0 : ZoneIndexOf(name),
                        ZoneName = name,
                        StartStationX = stations[0],
                        EndStationX = stations[stations.Count - 1],
                        Spacing = spacing,
                        Count = stations.Count,
                        Stations = stations,
                        OutToOutWidth = box.Width,
                        OutToOutHeight = box.Height,
                        BoxMinY = box.MinY,
                        BoxMinZ = box.MinZ,
                        StirrupType = branch.ShapeType,
                        BarMark = KataStirrupCurveFactory.MarkOf(branch.ShapeType)
                    });

                    foreach (double x in stations)
                        stirrups.Add(KataStirrupCurveFactory.Create(branch.ShapeType, x, yMin, yMax, zTop, zBot, ds, barId++, s));
                }
            }
        }

        return (zones, stirrups);
    }

    private static int ZoneIndexOf(string name) => name switch
    {
        LeftZone => 0,
        MiddleZone => 1,
        _ => 2
    };

    private const string LeftZone = "Gối trái";
    private const string MiddleZone = "Giữa nhịp";
    private const string RightZone = "Gối phải";

    private static List<(string, double, List<double>)> ThreeZones(
        KataBeamStations st, int s, double ln, double sDense, double sSparse, double sEnd, KataDetailingRules rules)
    {
        double offset = rules.FirstStirrupOffset;
        double endZone = ln * rules.EndZoneFraction;

        int intervals1 = (int)Math.Floor(Math.Max(0.0, endZone - offset) / sDense + 1e-9);
        var left = new List<double>(intervals1 + 1);
        for (int i = 0; i <= intervals1; i++) left.Add(st.SpanStart[s] + offset + i * sDense);

        int intervals3 = (int)Math.Floor(Math.Max(0.0, endZone - offset) / sEnd + 1e-9);
        double firstRight = st.SpanEnd[s] - offset - intervals3 * sEnd;
        var right = new List<double>(intervals3 + 1);
        for (int i = 0; i <= intervals3; i++) right.Add(firstRight + i * sEnd);

        double lastLeft = left[left.Count - 1];
        double gap = firstRight - lastLeft;
        var middle = new List<double>();
        if (gap > 2.0 * Math.Min(sDense, sSparse))
        {
            int intervals2 = Math.Max(0, (int)Math.Ceiling(gap / sSparse - 2.0 - 1e-9));
            double delta = (gap - intervals2 * sSparse) / 2.0;
            for (int i = 0; i <= intervals2; i++) middle.Add(lastLeft + delta + i * sSparse);
        }

        return new List<(string, double, List<double>)>
        {
            (LeftZone, sDense, left),
            (MiddleZone, sSparse, middle),
            (RightZone, sEnd, right)
        };
    }

    private static List<double> CantileverStations(KataBeamStations st, int s, double ln, double spacing, KataDetailingRules rules)
    {
        var stations = new List<double>();
        double length = ln - rules.FirstStirrupOffset - rules.StirrupCover;
        if (length <= 0.0) return stations;

        int count = (int)Math.Floor(length / spacing + 1e-9) + 1;
        double startX = s == 0 && st.IsLeftCantilever
            ? st.SpanStart[s] + rules.StirrupCover
            : st.SpanStart[s] + rules.FirstStirrupOffset;

        for (int i = 0; i < count; i++) stations.Add(startX + i * spacing);
        return stations;
    }

    private readonly record struct Box(double Width, double Height, double MinY, double MinZ);
}
