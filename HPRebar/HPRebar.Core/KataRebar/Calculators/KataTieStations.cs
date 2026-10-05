using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Stations of the C ties along one span (side-bar ties and layer spacer ties alike), from the option group
/// "Khoảng cách đai gia cường" (I8): evenly at J7 from one face clearance inside the span, or one tie beside
/// every outer hoop. A tie sits two stirrup diameters along the beam from a hoop's station, clear of the hoop
/// and of the inner stirrups, which sit one diameter along.
/// </summary>
public static class KataTieStations
{
    /// <summary>One set of evenly spaced stations.</summary>
    public readonly record struct Run(IReadOnlyList<double> Stations, double Spacing, string ZoneName);

    /// <summary>Name of a run spread evenly at J7 (I8 = 2), which follows no hoop zone: "Đều a500".</summary>
    public const string EvenRunPrefix = "Đều a";

    /// <summary>Distance kept from a support face, or from the end of a bar a tie holds.</summary>
    public static double Clearance(KataDetailingRules rules) => rules.FirstStirrupOffset + 2.0 * rules.StirrupDiameter;

    /// <summary>The runs of span <paramref name="span"/> whose stations lie within [lo, hi] (local X, mm).</summary>
    public static List<Run> InSpan(
        KataDetailingRules rules,
        KataBeamStations st,
        IReadOnlyList<KataStirrupZoneResult> hoopZones,
        int span,
        double lo,
        double hi)
    {
        var runs = new List<Run>();
        if (rules.TieSpacingMode == KataTieSpacingMode.LikeHoops)
        {
            double shift = 2.0 * rules.StirrupDiameter;
            // The shift runs towards +X: at a right cantilever tip it must not push a tie into the end cover.
            double last = span == st.SpanCount - 1 && st.IsRightCantilever
                ? st.SpanEnd[span] - rules.StirrupCover - rules.StirrupDiameter / 2.0
                : st.SpanEnd[span];
            foreach (var zone in hoopZones.Where(z => z.SpanIndex == span && z.Count > 0 && !KataJointStirrups.IsJointZone(z.ZoneName)))
            {
                // A zone's stations are evenly spaced, so the ones inside [lo, hi] still are.
                var stations = zone.Stations.Select(x => x + shift)
                    .Where(x => x >= lo - 1e-6 && x <= hi + 1e-6 && x < last)
                    .ToList();
                if (stations.Count > 0) runs.Add(new Run(stations, zone.Spacing, zone.ZoneName));
            }

            return runs;
        }

        double spacing = rules.TieSpacing;
        double clear = Clearance(rules);
        double from = Math.Max(lo, st.SpanStart[span] + clear);
        double to = Math.Min(hi, st.SpanEnd[span] - clear);
        if (spacing <= 0.0 || to < from - 1e-6) return runs;

        int count = (int)Math.Floor(Math.Max(0.0, to - from) / spacing + 1e-9) + 1;
        runs.Add(new Run(Enumerable.Range(0, count).Select(i => from + i * spacing).ToList(), spacing, $"{EvenRunPrefix}{spacing:0}"));
        return runs;
    }
}
