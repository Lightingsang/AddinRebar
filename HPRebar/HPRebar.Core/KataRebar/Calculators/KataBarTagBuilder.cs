using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>One elevation tag: the circled numbers, the bar text, and the point of the bar its leader starts from.</summary>
/// <param name="Numbers">Bar numbers, drawn as circles joined by "+".</param>
/// <param name="Text">"2Ø18+1Ø18", "3Ø18", "2x2Ø12".</param>
/// <param name="X">Local station of the leader's foot (mm).</param>
/// <param name="Z">Level of the bar the leader starts from (mm, beam top = 0).</param>
/// <param name="Above">Drawn above the beam (top bars) or below it (bottom and side bars).</param>
/// <param name="Inner">An inner row (14-16, 17) or the side bars: drawn in the second row of tags, clear of the outer one.</param>
public sealed record KataBarTag(IReadOnlyList<int> Numbers, string Text, double X, double Z, bool Above, bool Inner = false);

/// <summary>
/// The elevation tags Kata draws (T2-DY7 / T2-DY14): over each support the top main bars and the row-13 bars share one
/// tag ("1+4 2Ø18+1Ø18"), and under each span the bottom main bars and row 18 share one ("2+10 2Ø18+1Ø18"); an inner
/// row (14-16, 17) has its own; the main bars get their own tag at a span's middle where no outer row covers it; the
/// side bars one tag per bar ("13 2Ø12", two layers "15 2x2Ø12"). Stirrups are labelled on their zones and C ties only
/// in sections.
/// </summary>
public static class KataBarTagBuilder
{
    public static IReadOnlyList<KataBarTag> Build(KataRebarLayoutResult layout, KataBeamStations st)
    {
        if (layout is null) throw new ArgumentNullException(nameof(layout));
        if (st is null) throw new ArgumentNullException(nameof(st));

        var tags = new List<KataBarTag>();
        var top = layout.MainTopBars;

        foreach (var group in layout.ExtraTopBars.GroupBy(b => (b.HostSupportIndex, b.Layer)).OrderBy(g => g.Key.HostSupportIndex).ThenBy(g => g.Key.Layer))
        {
            var (x0, x1, z) = Level(group);
            double x = (x0 + x1) / 2.0;
            var main = group.Key.Layer == 1 ? Covering(top, x) : new List<KataRebarCurve>();
            tags.Add(Tag(main, group.ToList(), x, z, above: true) with { Inner = group.Key.Layer > 1 });
        }

        foreach (var group in layout.ExtraBottomBars.GroupBy(b => (b.HostSpanIndex, b.Layer)).OrderBy(g => g.Key.HostSpanIndex).ThenBy(g => g.Key.Layer))
        {
            var (x0, x1, z) = Level(group);
            double x = (x0 + x1) / 2.0;
            var main = group.Key.Layer == 1 ? Covering(layout.MainBottomBars, x) : new List<KataRebarCurve>();
            tags.Add(Tag(main, group.ToList(), x, z, above: false) with { Inner = group.Key.Layer > 1 });
        }

        // The main bars on their own at the middle of a span no outer row covers there.
        var row13 = layout.ExtraTopBars.Where(b => b.Layer == 1).ToList();
        var row18 = layout.ExtraBottomBars.Where(b => b.Layer == 1).ToList();
        for (int s = 0; s < st.SpanCount; s++)
        {
            double x = (st.SpanStart[s] + st.SpanEnd[s]) / 2.0;
            var mainTop = Covering(top, x);
            if (mainTop.Count > 0 && Covering(row13, x).Count == 0)
                tags.Add(Tag(mainTop, new List<KataRebarCurve>(), x, LevelAt(mainTop[0], x), above: true));
            var mainBottom = Covering(layout.MainBottomBars, x);
            if (mainBottom.Count > 0 && Covering(row18, x).Count == 0)
                tags.Add(Tag(mainBottom, new List<KataRebarCurve>(), x, LevelAt(mainBottom[0], x), above: false));
        }

        // One tag per run of side bars (a number may come back on another run of the same length).
        foreach (var group in layout.SideBars.GroupBy(b => (b.BarNumber, Start: Math.Round(b.Polyline.Points.Min(p => p.X)))))
        {
            var (x0, x1, _) = Level(group);
            int layers = group.Select(b => b.Layer).Distinct().Count();
            double d = group.First().Diameter;
            string text = layers > 1 ? $"{layers}x2Ø{Dia(d)}" : $"2Ø{Dia(d)}";
            tags.Add(new KataBarTag(new[] { group.Key.BarNumber }, text, (x0 + x1) / 2.0, group.Min(b => b.Polyline.Points[0].Z), Above: false, Inner: true));
        }

        return tags;
    }

    /// <summary>Main bars (counted once per position across the beam) first, then the row's bars.</summary>
    private static KataBarTag Tag(IReadOnlyList<KataRebarCurve> main, IReadOnlyList<KataRebarCurve> row, double x, double z, bool above)
    {
        var parts = main.Concat(row)
            .GroupBy(b => b.BarNumber)
            .Select(g => (Number: g.Key, Count: g.Select(b => Math.Round(b.TransverseY)).Distinct().Count(), g.First().Diameter))
            .ToList();
        return new KataBarTag(
            parts.Select(p => p.Number).ToList(),
            string.Join("+", parts.Select(p => $"{p.Count}Ø{Dia(p.Diameter)}")),
            x, z, above);
    }

    /// <summary>Bars that run past station <paramref name="x"/> (a cranked bar counts over its whole length).</summary>
    private static List<KataRebarCurve> Covering(IEnumerable<KataRebarCurve> bars, double x) =>
        bars.Where(b => x >= b.Polyline.Points.Min(p => p.X) - 1e-6 && x <= b.Polyline.Points.Max(p => p.X) + 1e-6).ToList();

    /// <summary>The longest level run of a group's first bar: x0, x1 and its height.</summary>
    private static (double X0, double X1, double Z) Level(IEnumerable<KataRebarCurve> bars)
    {
        var points = bars.First().Polyline.Points;
        (double, double, double) best = (points[0].X, points[0].X, points[0].Z);
        double longest = -1.0;
        for (int i = 1; i < points.Count; i++)
        {
            if (Math.Abs(points[i].Z - points[i - 1].Z) > 1.0) continue;
            double length = Math.Abs(points[i].X - points[i - 1].X);
            if (length <= longest) continue;
            longest = length;
            best = (Math.Min(points[i].X, points[i - 1].X), Math.Max(points[i].X, points[i - 1].X), points[i].Z);
        }

        return best;
    }

    /// <summary>Height of a bar at station <paramref name="x"/> (a cranked bottom bar changes level).</summary>
    private static double LevelAt(KataRebarCurve bar, double x)
    {
        var p = bar.Polyline.Points;
        for (int i = 1; i < p.Count; i++)
        {
            double a = Math.Min(p[i - 1].X, p[i].X), b = Math.Max(p[i - 1].X, p[i].X);
            if (x < a - 1e-6 || x > b + 1e-6 || b - a < 1e-6) continue;
            double t = (x - p[i - 1].X) / (p[i].X - p[i - 1].X);
            return p[i - 1].Z + t * (p[i].Z - p[i - 1].Z);
        }

        return p[0].Z;
    }

    private static string Dia(double d) => d.ToString("0.#", CultureInfo.InvariantCulture);
}
