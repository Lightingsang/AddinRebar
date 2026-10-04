using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>One section of the beam ("MẶT CẮT n-n").</summary>
/// <param name="Number">Section number; sections that cut the same bars share it, as Kata draws them.</param>
/// <param name="X">Local station of the cut (mm).</param>
/// <param name="SpanIndex">Span it cuts.</param>
public sealed record KataSectionCut(int Number, double X, int SpanIndex);

/// <summary>
/// The sections Kata draws (T2-DY7, T2-DY14 and its E = 500 variant): three per span — near each support, one tenth of
/// the clear span from its face rounded to 25 mm, and at mid-span, 150 mm short of the middle (50 mm in a span under
/// 3 m) — and one at the middle of a span under 1 m. The positions are a fit of Kata's (within 75 mm); what Kata puts
/// in each section is reproduced: two sections that cut the same bars at the same places, in the same hoops at the same
/// spacing, share a number (DY14: 9-9 at the end of span 3 and in span 4). A cantilever has no cut near its free tip,
/// where only the top bars run (no Kata drawing of a cantilever yet: an assumption).
/// </summary>
public static class KataSectionCuts
{
    public const double ShortSpan = 1000.0;
    public const double MidShiftSpan = 3000.0;
    private const double Step = 25.0;

    public static IReadOnlyList<KataSectionCut> Build(KataBeamRebarSpec spec, KataRebarLayoutResult layout)
    {
        if (spec is null) throw new ArgumentNullException(nameof(spec));
        if (layout is null) throw new ArgumentNullException(nameof(layout));

        var st = KataBeamStations.From(spec);
        var numbers = new Dictionary<string, int>();
        var cuts = new List<KataSectionCut>();
        for (int s = 0; s < st.SpanCount; s++)
        {
            foreach (double x in Stations(st, s))
            {
                string content = Content(spec, layout, s, x);
                if (!numbers.TryGetValue(content, out int n)) numbers[content] = n = numbers.Count + 1;
                cuts.Add(new KataSectionCut(n, x, s));
            }
        }

        return cuts;
    }

    /// <summary>Stations of the cuts of span <paramref name="s"/>, left to right.</summary>
    public static IReadOnlyList<double> Stations(KataBeamStations st, int s)
    {
        double start = st.SpanStart[s], end = st.SpanEnd[s], length = end - start;
        if (length <= 0.0) return Array.Empty<double>();

        double mid = start + length / 2.0;
        if (length < ShortSpan) return new[] { mid };

        double offset = Math.Round(length / 10.0 / Step, MidpointRounding.AwayFromZero) * Step;
        double shift = length < MidShiftSpan ? 50.0 : 150.0;
        var cuts = new List<double>(3);
        if (!(s == 0 && st.IsLeftCantilever)) cuts.Add(start + offset);
        cuts.Add(mid - shift);
        if (!(s == st.SpanCount - 1 && st.IsRightCantilever)) cuts.Add(end - offset);
        return cuts;
    }

    /// <summary>Bars a cut at <paramref name="x"/> meets, each with the height it is cut at (mm, beam top = 0).</summary>
    public static IEnumerable<(KataRebarCurve Bar, double Z)> Crossing(KataRebarLayoutResult layout, double x)
    {
        foreach (var bar in layout.LongitudinalBars)
        {
            var p = bar.Polyline.Points;
            for (int i = 0; i + 1 < p.Count; i++)
            {
                if (Math.Abs(p[i].Z - p[i + 1].Z) > 1.0) continue;
                if (x < Math.Min(p[i].X, p[i + 1].X) - 1.0 || x > Math.Max(p[i].X, p[i + 1].X) + 1.0) continue;
                yield return (bar, p[i].Z);
                break;
            }
        }
    }

    /// <summary>The hoop zone a cut at <paramref name="x"/> falls in (the nearest of its span between two zones).</summary>
    public static KataStirrupZoneResult? Hoops(KataRebarLayoutResult layout, int span, double x) =>
        layout.StirrupZones.Where(z => z.SpanIndex == span && z.Count > 0)
            .OrderBy(z => x >= z.StartStationX - z.Spacing / 2.0 && x <= z.EndStationX + z.Spacing / 2.0 ? 0 : 1)
            .ThenBy(z => Math.Min(Math.Abs(x - z.StartStationX), Math.Abs(x - z.EndStationX)))
            .FirstOrDefault();

    /// <summary>The bar sets (C ties, inner stirrups) a cut at <paramref name="x"/> meets.</summary>
    public static IEnumerable<KataBarSet> Sets(KataRebarLayoutResult layout, double x) =>
        layout.BarSets.Where(s => s.Count > 0
            && x >= s.Stations[0] - Math.Max(s.Spacing, 1.0) / 2.0
            && x <= s.Stations[s.Count - 1] + Math.Max(s.Spacing, 1.0) / 2.0);

    private static string Content(KataBeamRebarSpec spec, KataRebarLayoutResult layout, int span, double x)
    {
        static string R(double v) => ((long)Math.Round(v)).ToString(CultureInfo.InvariantCulture);
        var bars = Crossing(layout, x)
            .Select(c => $"{c.Bar.BarNumber}@{R(c.Bar.TransverseY)},{R(c.Z)}")
            .OrderBy(t => t, StringComparer.Ordinal);
        var hoop = Hoops(layout, span, x);
        var sets = Sets(layout, x).Select(s => s.BarNumber.ToString(CultureInfo.InvariantCulture)).Distinct().OrderBy(t => t, StringComparer.Ordinal);
        return $"{R(spec.DepthOf(span))}|{R(spec.WidthOf(span))}|{string.Join(";", bars)}|{hoop?.BarNumber}a{R(hoop?.LabelSpacing ?? 0)}|{string.Join(",", sets)}";
    }
}
