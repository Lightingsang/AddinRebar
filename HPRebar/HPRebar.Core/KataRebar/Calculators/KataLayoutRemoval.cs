using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Bars the user struck off the drawing before generating: each longitudinal bar and each stirrup zone has a key that
/// stays the same when the same sheet is planned again on the same beams (bar ids, positions and lengths are
/// deterministic), so the canvas and the generation remove the same groups; <see cref="Fingerprint"/> proves the
/// plan is the one the keys were taken from. A stirrup zone goes with the inner U / C stirrups laid in it; the C ties
/// stay (they are not drawn on the elevation). What is left is numbered again from 1 in Kata's order, and its weight
/// recounted.
/// </summary>
public static class KataLayoutRemoval
{
    /// <summary>Key of one longitudinal bar (main, additional, side bar).</summary>
    public static string Key(KataRebarCurve bar)
    {
        if (bar is null) throw new ArgumentNullException(nameof(bar));
        var points = bar.Polyline.Points;
        double x0 = points.Count > 0 ? points.Min(p => p.X) : 0.0;
        return string.Format(CultureInfo.InvariantCulture, "bar|{0}|{1}|{2:0}|{3:0}|{4}|{5:0}|{6:0}",
            bar.Role, bar.BarId, bar.Diameter, bar.Polyline.TotalLength, bar.Layer, bar.TransverseY, x0);
    }

    /// <summary>Key of one stirrup zone.</summary>
    public static string Key(KataStirrupZoneResult zone)
    {
        if (zone is null) throw new ArgumentNullException(nameof(zone));
        double first = zone.Stations.Count > 0 ? zone.Stations[0] : zone.StartStationX;
        return string.Format(CultureInfo.InvariantCulture, "zone|{0}|{1}|{2}|{3:0}", zone.SpanIndex, zone.ZoneIndex, zone.Count, first);
    }

    /// <summary>
    /// One text naming every bar and stirrup zone of <paramref name="layout"/>: the generation re-plans and refuses to
    /// remove anything when the new plan's fingerprint differs from the one the keys were picked on.
    /// </summary>
    public static string Fingerprint(KataRebarLayoutResult layout)
    {
        if (layout is null) throw new ArgumentNullException(nameof(layout));
        return string.Join(";", layout.LongitudinalBars.Select(Key).Concat(layout.StirrupZones.Select(Key)).OrderBy(k => k, StringComparer.Ordinal));
    }

    /// <summary>
    /// <paramref name="plan"/> without the groups of <paramref name="keys"/>, numbered again; the same plan when
    /// nothing is removed. <paramref name="unknown"/> lists the keys the plan does not have (planned differently).
    /// </summary>
    public static KataRebarPlan Remove(KataRebarPlan plan, IReadOnlyCollection<string> keys, out IReadOnlyList<string> unknown)
    {
        if (plan is null) throw new ArgumentNullException(nameof(plan));
        if (keys is null) throw new ArgumentNullException(nameof(keys));

        var wanted = new HashSet<string>(keys, StringComparer.Ordinal);
        if (wanted.Count == 0)
        {
            unknown = Array.Empty<string>();
            return plan;
        }

        var layout = plan.Layout;
        var found = new HashSet<string>(StringComparer.Ordinal);
        List<KataRebarCurve> Keep(IReadOnlyList<KataRebarCurve> bars) => bars.Where(b =>
        {
            string key = Key(b);
            if (!wanted.Contains(key)) return true;
            found.Add(key);
            return false;
        }).ToList();

        var removedZones = layout.StirrupZones.Where(z => wanted.Contains(Key(z))).ToList();
        foreach (var zone in removedZones) found.Add(Key(zone));

        var trimmed = layout with
        {
            MainTopBars = Keep(layout.MainTopBars),
            MainBottomBars = Keep(layout.MainBottomBars),
            ExtraTopBars = Keep(layout.ExtraTopBars),
            ExtraBottomBars = Keep(layout.ExtraBottomBars),
            SideBars = Keep(layout.SideBars),
            StirrupZones = layout.StirrupZones.Where(z => !removedZones.Contains(z)).ToList(),
            IndividualStirrups = layout.IndividualStirrups.Where(s => !removedZones.Any(z => Belongs(s, z))).ToList(),
            // The inner stirrups of a removed zone go with it. Numbering keeps a set's number once it has one: every
            // number is given again.
            BarSets = layout.BarSets
                .Where(s => KataBarNumbering.IsTie(s) || !removedZones.Any(z => z.SpanIndex == s.SpanIndex && z.ZoneName == s.ZoneName))
                .Select(s => s with { BarNumber = 0 })
                .ToList()
        };

        unknown = wanted.Where(k => !found.Contains(k)).OrderBy(k => k, StringComparer.Ordinal).ToList();
        if (found.Count == 0) return plan;

        trimmed = trimmed with
        {
            TotalSteelWeightKg = Math.Round(
                KataRebarCalculator.WeightKg(trimmed.MainTopBars) + KataRebarCalculator.WeightKg(trimmed.MainBottomBars)
                + KataRebarCalculator.WeightKg(trimmed.ExtraTopBars) + KataRebarCalculator.WeightKg(trimmed.ExtraBottomBars)
                + KataRebarCalculator.WeightKg(trimmed.SideBars) + KataRebarCalculator.WeightKg(trimmed.IndividualStirrups)
                + KataRebarCalculator.WeightKg(trimmed.BarSets), 2)
        };
        return plan with { Layout = KataBarNumbering.Apply(trimmed, plan.Rules.StirrupDiameter) };
    }

    /// <summary>A single stirrup of <paramref name="zone"/>: same span, at one of its stations.</summary>
    private static bool Belongs(KataRebarCurve stirrup, KataStirrupZoneResult zone)
    {
        if (stirrup.HostSpanIndex >= 0 && stirrup.HostSpanIndex != zone.SpanIndex) return false;
        var points = stirrup.Polyline.Points;
        if (points.Count == 0) return false;
        double x = points[0].X;
        return zone.Stations.Any(s => Math.Abs(s - x) < 0.5);
    }
}
