using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Side bars ("cốt giá") span by span: G5 layers of G4, or the span's row 20 ("2f12" = two bars, one layer;
/// "0" = none). Each layer is a bar on each face against the stirrup, the layers spread evenly between the
/// top and bottom main bars; a bar runs into each support by the settings' anchorage (never past its far
/// face − a). Unless G5 is negative, C ties with 180° hooks hold each layer at the settings' spacing.
/// </summary>
public static class KataSideBarLayout
{
    public static (List<KataRebarCurve> Bars, List<KataBarSet> Ties) Build(
        KataBeamRebarSpec spec,
        KataDetailingRules rules,
        KataBeamStations st,
        List<string> warnings,
        ref int barId)
    {
        var bars = new List<KataRebarCurve>();
        var ties = new List<KataBarSet>();

        double zTop = spec.TopContinuous.IsEmpty ? -(rules.StirrupCover + rules.StirrupDiameter) : -rules.TopBarCentreDepth;
        double zBottom = spec.BottomContinuous.IsEmpty
            ? -spec.Height + rules.StirrupCover + rules.StirrupDiameter
            : -spec.Height + rules.BottomBarCentreDepth;

        for (int s = 0; s < st.SpanCount && s < spec.Spans.Count; s++)
        {
            var (layers, diameter) = Layers(spec, spec.Spans[s], warnings);
            if (layers == 0 || diameter <= 0.0) continue;

            double length = st.SpanEnd[s] - st.SpanStart[s];
            double xStart = st.SpanStart[s] - Anchorage(rules, st.SupportWidth[s], diameter, interior: s > 0);
            double xEnd = st.SpanEnd[s] + Anchorage(rules, st.SupportWidth[s + 1], diameter, interior: s + 1 < st.SpanCount);
            double y = rules.EdgeBarOffset(spec.Width, diameter);
            double step = (zTop - zBottom) / (layers + 1);

            for (int r = 1; r <= layers; r++)
            {
                double z = zBottom + r * step;
                string mark = $"5.{s + 1}.{r}";
                foreach (double side in new[] { -y, y })
                    bars.Add(Bar(barId++, diameter, side, z, xStart, xEnd, s, r, mark));

                if (spec.SideBarTies && rules.StirrupDiameter > 0.0)
                    AddTies(ties, rules, st, s, length, y, z, diameter, mark + "C");
            }
        }

        return (bars, ties);
    }

    /// <summary>Number of layers and bar diameter of a span: its row 20 first, else G4/G5.</summary>
    private static (int Layers, double Diameter) Layers(KataBeamRebarSpec spec, KataSpanRebarSpec span, List<string> warnings)
    {
        if (span.SideBars.Count > 0)
        {
            var items = span.SideBars.Where(i => i.Count > 0 && i.Diameter > 0.0).ToList();
            if (items.Count == 0) return (0, 0.0);

            int count = items.Sum(i => i.Count);
            int layers = (count + 1) / 2;
            if (count % 2 != 0)
                warnings.Add($"{Cell(span)} '{Notation(span.SideBars)}': số thanh cốt giá lẻ, mỗi lớp 2 thanh — vẽ {layers} lớp.");
            return (layers, items.Max(i => i.Diameter));
        }

        return spec.GlobalSideBars.Count == 0 ? (0, 0.0) : (spec.GlobalSideBars.Count, spec.GlobalSideBars.Max(i => i.Diameter));
    }

    /// <param name="interior">The next span's side bars come in from the other face: each stays in its half, a bar apart.</param>
    private static double Anchorage(KataDetailingRules rules, double supportWidth, double diameter, bool interior)
    {
        if (supportWidth <= 0.0) return 0.0;
        double room = interior ? supportWidth / 2.0 - diameter / 2.0 : supportWidth - rules.TopBarCentreDepth;
        return Math.Min(rules.SideBarAnchorageFactor * diameter, Math.Max(0.0, room));
    }

    /// <summary>
    /// C ties across the beam at one side-bar layer, from the first to the last stirrup station of the span:
    /// the straight part passes under the two side bars and each 180° hook wraps one of them.
    /// </summary>
    private static void AddTies(List<KataBarSet> ties, KataDetailingRules rules, KataBeamStations st, int span, double length,
        double y, double z, double sideDiameter, string mark)
    {
        // Two stirrup diameters along the beam from the hoops' first station: clear of the hoops and of the
        // inner stirrups, which sit one diameter along.
        double first = rules.FirstStirrupOffset + 2.0 * rules.StirrupDiameter;
        double spacing = rules.SideBarTieSpacing;
        if (length < 2.0 * first || spacing <= 0.0) return;

        int count = (int)Math.Floor((length - 2.0 * first) / spacing + 1e-9) + 1;
        var stations = Enumerable.Range(0, count).Select(i => st.SpanStart[span] + first + i * spacing).ToList();
        double x0 = stations[0];

        ties.Add(new KataBarSet
        {
            BarMark = mark,
            Description = $"Móc C giữ cốt giá nhịp {span + 1}",
            Role = KataBarRole.CrossTie,
            Diameter = rules.StirrupDiameter,
            SpanIndex = span,
            ZoneName = "Cốt giá",
            // The side bars' centres: the tie is laid out round them where the bar type's bend radius is known.
            Shape = new Polyline3(new List<Point3> { new(x0, -y, z), new(x0, y, z) }),
            WrapEnds = true,
            WrapOffset = new Point3(0.0, 0.0, -1.0),
            Stations = stations,
            Spacing = spacing,
            HookAngle = rules.CrossTieHookAngle,
            HookFactor = rules.CrossTieHookFactor,
            HookToward = new Point3(x0, 0.0, z)
        });
    }

    private static KataRebarCurve Bar(int id, double dia, double y, double z, double xStart, double xEnd, int span, int layer, string mark) => new()
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
        BarDescription = $"Cốt giá nhịp {span + 1} lớp {layer}",
        DimA = xEnd - xStart,
        SttCad = 5
    };

    private static string Cell(KataSpanRebarSpec span) =>
        span.SheetColumn > 0 ? KataDamCellAccessorExtensions.ToAddress(20, span.SheetColumn) : $"Nhịp {span.SpanIndex + 1} hàng 20";

    private static string Notation(IEnumerable<KataBarItem> items) => string.Join("+", items.Select(i => $"{i.Count}f{i.Diameter:0}"));
}
