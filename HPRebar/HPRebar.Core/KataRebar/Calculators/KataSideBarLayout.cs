using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Side bars ("cốt giá"): G5 layers of G4, or the span's row 20 ("2f12" = two layers of Ø12 as G5/G4 say it, as the
/// Kata drawing "2x2Ø12" shows; "0" = none).
/// Each layer is a bar on each face against the stirrup, the layers spread evenly between the top and bottom
/// main bars; consecutive spans with the same side bars share one bar through the interior supports, at the
/// shallowest span's levels; a bar runs into each end of its group by the settings' anchorage (never past the
/// far face − a, at an interior support only to its middle). Unless G5 is negative, C ties with 180° hooks hold
/// each layer span by span, spaced by J7/I8 (<see cref="KataTieStations"/>).
/// </summary>
public static class KataSideBarLayout
{
    /// <summary>Zone name of the C ties holding the side bars.</summary>
    public const string TieZoneName = "Cốt giá";

    public static (List<KataRebarCurve> Bars, List<KataBarSet> Ties) Build(
        KataBeamRebarSpec spec,
        KataDetailingRules rules,
        KataBeamStations st,
        IReadOnlyList<KataStirrupZoneResult> hoopZones,
        List<string> warnings,
        ref int barId)
    {
        var bars = new List<KataRebarCurve>();
        var ties = new List<KataBarSet>();
        int n = Math.Min(st.SpanCount, spec.Spans.Count);

        double topBar = spec.TopContinuous.IsEmpty ? -(rules.StirrupCover + rules.StirrupDiameter) : -rules.TopBarCentreDepth;
        double ZBottom(double depth) => spec.BottomContinuous.IsEmpty
            ? -depth + rules.StirrupCover + rules.StirrupDiameter
            : -depth + rules.BottomBarCentreDepth;

        var perSpan = new (int Layers, double Diameter)[n];
        var missing = new List<string>();
        for (int s = 0; s < n; s++)
        {
            perSpan[s] = Layers(spec, spec.Spans[s], warnings);
            // Row 20 "0" is the user's own choice; an empty row 20 with no G4/G5 is an omission.
            if ((perSpan[s].Layers == 0 || perSpan[s].Diameter <= 0.0) && spec.Spans[s].SideBars.Count == 0
                && rules.SideBarRequiredHeight > 0.0 && spec.DepthOf(s) + LowestTopOf(spec, s) >= rules.SideBarRequiredHeight - 1e-6)
                missing.Add($"{Cell(spec.Spans[s])} (h {spec.DepthOf(s) + LowestTopOf(spec, s):0})");
        }

        // Consecutive spans with the same side bars share them: one bar runs on through the interior supports
        // (Kata drawing), at the levels of the shallowest span of the group so it stays inside each of them.
        for (int a = 0; a < n; a++)
        {
            var (layers, diameter) = perSpan[a];
            if (layers == 0 || diameter <= 0.0) continue;

            int b = a;
            // A change of width ends the run too: the bars would not stay against the stirrups.
            while (b + 1 < n && perSpan[b + 1] == perSpan[a] && Math.Abs(spec.WidthOf(b + 1) - spec.WidthOf(a)) <= 0.5) b++;

            double depth = Enumerable.Range(a, b - a + 1).Min(spec.DepthOf);
            double zBottom = ZBottom(depth);
            // Under the lowest top of the run (row 19), so they stay inside each span.
            double zTop = topBar + Enumerable.Range(a, b - a + 1).Min(span => LowestTopOf(spec, span));
            double xStart = st.SpanStart[a] - Anchorage(rules, st.SupportWidth[a], diameter, interior: a > 0);
            double xEnd = st.SpanEnd[b] + Anchorage(rules, st.SupportWidth[b + 1], diameter, interior: b + 1 < st.SpanCount);
            double y = rules.EdgeBarOffset(spec.WidthOf(a), diameter);
            double step = (zTop - zBottom) / (layers + 1);

            for (int r = 1; r <= layers; r++)
            {
                double z = zBottom + r * step;
                string mark = $"5.{a + 1}.{r}";
                foreach (double side in new[] { -y, y })
                    bars.Add(Bar(barId++, diameter, side, z, xStart, xEnd, a, b, r, mark));

                if (spec.SideBarTies && rules.StirrupDiameter > 0.0)
                    for (int s = a; s <= b; s++)
                        AddTies(ties, rules, st, hoopZones, s, y, z, diameter, $"5.{s + 1}.{r}C");
            }

            a = b;
        }

        if (missing.Count > 0)
            warnings.Add($"Nhịp cao ≥ {rules.SideBarRequiredHeight:0} mm nhưng G4/G5 và hàng 20 ({string.Join(", ", missing)}) trống: " +
                "thiếu cốt giá (TCVN 5574 mục 10.3.1.2) — không tự sinh, nhập G4/G5 hoặc hàng 20.");

        return (bars, ties);
    }

    /// <summary>Number of layers and bar diameter of a span: its row 20 first, else G4/G5.</summary>
    private static (int Layers, double Diameter) Layers(KataBeamRebarSpec spec, KataSpanRebarSpec span, List<string> warnings)
    {
        if (span.SideBars.Count > 0)
        {
            var items = span.SideBars.Where(i => i.Count > 0 && i.Diameter > 0.0).ToList();
            if (items.Count == 0) return (0, 0.0);

            // n counts layers, each a bar on either face.
            return (items.Sum(i => i.Count), items.Max(i => i.Diameter));
        }

        return spec.GlobalSideBars.Count == 0 ? (0, 0.0) : (spec.GlobalSideBars.Count, spec.GlobalSideBars.Max(i => i.Diameter));
    }

    /// <summary>The lowest top over span <paramref name="span"/> (row 19, with any step inside it).</summary>
    private static double LowestTopOf(KataBeamRebarSpec spec, int span) =>
        spec.Spans[span].TopSteps.Select(s => s.TopDrop).Append(spec.Spans[span].TopDrop).Min();

    /// <param name="interior">The next span's side bars come in from the other face: each stays in its half, a bar apart.</param>
    private static double Anchorage(KataDetailingRules rules, double supportWidth, double diameter, bool interior)
    {
        // A console tip: they stop a (J9) short of it.
        if (supportWidth <= 0.0) return -rules.TopBarCentreDepth;
        double room = interior ? supportWidth / 2.0 - diameter / 2.0 : supportWidth - rules.TopBarCentreDepth;
        return Math.Min(rules.SideBarAnchorageFactor * diameter, Math.Max(0.0, room));
    }

    /// <summary>
    /// C ties across the beam at one side-bar layer along the span: the straight part passes under the two side
    /// bars and each 180° hook wraps one of them.
    /// </summary>
    private static void AddTies(List<KataBarSet> ties, KataDetailingRules rules, KataBeamStations st,
        IReadOnlyList<KataStirrupZoneResult> hoopZones, int span, double y, double z, double sideDiameter, string mark)
    {
        foreach (var run in KataTieStations.InSpan(rules, st, hoopZones, span, st.SpanStart[span], st.SpanEnd[span]))
            ties.Add(Tie(rules, span, y, z, sideDiameter, mark, run.Stations, run.Spacing));
    }

    private static KataBarSet Tie(KataDetailingRules rules, int span, double y, double z, double sideDiameter, string mark, IReadOnlyList<double> stations, double spacing)
    {
        double x0 = stations[0];
        return new KataBarSet
        {
            BarMark = mark,
            Description = $"Móc C giữ cốt giá nhịp {span + 1}",
            Role = KataBarRole.CrossTie,
            Diameter = rules.StirrupDiameter,
            SpanIndex = span,
            ZoneName = TieZoneName,
            // The side bars' centres: the tie is laid out round them where the bar type's bend radius is known.
            Shape = new Polyline3(new List<Point3> { new(x0, -y, z), new(x0, y, z) }),
            WrapEnds = true,
            WrapOffset = new Point3(0.0, 0.0, -1.0),
            WrappedBarDiameter = sideDiameter,
            Stations = stations,
            Spacing = spacing,
            HookAngle = rules.CrossTieHookAngle,
            HookFactor = rules.CrossTieHookFactor,
            HookToward = new Point3(x0, 0.0, z)
        };
    }

    private static KataRebarCurve Bar(int id, double dia, double y, double z, double xStart, double xEnd, int span, int lastSpan, int layer, string mark) => new()
    {
        BarId = id,
        Role = KataBarRole.SideBar,
        Diameter = dia,
        Layer = layer,
        Polyline = new Polyline3(new List<Point3> { new(xStart, y, z), new(xEnd, y, z) }),
        TransverseY = y,
        HostSpanIndex = span,
        ShapeCode = "00",
        BarMark = mark,
        BarDescription = lastSpan > span ? $"Cốt giá nhịp {span + 1}–{lastSpan + 1} lớp {layer}" : $"Cốt giá nhịp {span + 1} lớp {layer}",
        DimA = xEnd - xStart,
        SttCad = 5
    };

    private static string Cell(KataSpanRebarSpec span) =>
        span.SheetColumn > 0 ? KataDamCellAccessorExtensions.ToAddress(20, span.SheetColumn) : $"Nhịp {span.SpanIndex + 1} hàng 20";

}
