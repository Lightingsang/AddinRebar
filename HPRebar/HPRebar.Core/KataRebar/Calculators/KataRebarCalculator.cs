using System;
using System.Collections.Generic;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Lays out every bar of a <see cref="KataBeamRebarSpec"/> in the beam's local frame (mm): X along the beam
/// from the outer face of the first support, Y across it with 0 on the concrete centre line, Z up with 0 on
/// the beam top. The placement rules come from <see cref="KataDetailingRuleBuilder"/>; each bar family has
/// its own layout class.
/// </summary>
public static class KataRebarCalculator
{
    private const double SteelDensityKgPerM3 = 7850.0;

    /// <summary>
    /// Transverse positions of <paramref name="count"/> bars spread evenly between the two bars that touch
    /// the stirrup's inner faces; a single bar sits on the centre line.
    /// </summary>
    public static IReadOnlyList<double> ComputeTransverseYPositions(
        double widthMm,
        double coverMm,
        double stirrupDiameterMm,
        double barDiameterMm,
        int count)
    {
        if (count <= 1)
            return new[] { 0.0 };

        double y0 = -(widthMm / 2.0) + coverMm + stirrupDiameterMm + (barDiameterMm / 2.0);
        double yn = +(widthMm / 2.0) - coverMm - stirrupDiameterMm - (barDiameterMm / 2.0);

        var positions = new List<double>(count);
        if (y0 >= yn)
        {
            for (int i = 0; i < count; i++) positions.Add(0.0);
            return positions;
        }

        double deltaY = (yn - y0) / (count - 1);
        for (int i = 0; i < count; i++) positions.Add(y0 + i * deltaY);
        return positions;
    }

    /// <summary>Lays out the spec with the rules derived from its own cells.</summary>
    public static KataRebarLayoutResult Calculate(KataBeamRebarSpec spec)
    {
        if (spec is null) throw new ArgumentNullException(nameof(spec));
        return Calculate(spec, KataDetailingRuleBuilder.Build(spec));
    }

    public static KataRebarLayoutResult Calculate(KataBeamRebarSpec spec, KataDetailingRules rules)
    {
        if (spec is null) throw new ArgumentNullException(nameof(spec));
        if (rules is null) throw new ArgumentNullException(nameof(rules));

        if (spec.Spans.Count == 0)
            return Empty(spec, "Beam specification contains no spans.");

        if (spec.Width <= 0.0 || spec.Height <= 0.0)
            return Empty(spec, "Beam dimensions width and height must be strictly positive.");

        var warnings = new List<string>(rules.Warnings);
        var stations = KataBeamStations.From(spec);
        int barId = 1;

        var (mainTop, mainBottom) = KataMainBarLayout.Build(spec, rules, stations, warnings, ref barId);
        var blocking = new List<string>();
        var extraTop = KataSupportTopBarLayout.Build(spec, rules, stations, warnings, blocking, ref barId);
        var extraBottom = KataSpanBottomBarLayout.Build(spec, rules, stations, ref barId);
        var sideBars = KataSideBarLayout.Build(spec, rules, stations, ref barId);
        var (zones, stirrups) = KataStirrupZoneLayout.Build(spec, rules, stations, ref barId);

        return new KataRebarLayoutResult
        {
            BeamName = spec.BeamName,
            MainTopBars = mainTop,
            MainBottomBars = mainBottom,
            ExtraTopBars = extraTop,
            ExtraBottomBars = extraBottom,
            SideBars = sideBars,
            StirrupZones = zones,
            IndividualStirrups = stirrups,
            Warnings = warnings,
            Blocking = blocking,
            TotalSteelWeightKg = Math.Round(
                WeightKg(mainTop) + WeightKg(mainBottom) + WeightKg(extraTop)
                + WeightKg(extraBottom) + WeightKg(sideBars) + WeightKg(stirrups), 2)
        };
    }

    /// <summary>Steel weight of a list of bars (kg) from their centreline lengths.</summary>
    public static double WeightKg(IReadOnlyList<KataRebarCurve> bars)
    {
        double total = 0.0;
        foreach (var bar in bars)
        {
            double lengthM = bar.Polyline.TotalLength / 1000.0;
            double radiusM = bar.Diameter / 2000.0;
            total += Math.PI * radiusM * radiusM * lengthM * SteelDensityKgPerM3;
        }

        return total;
    }

    private static KataRebarLayoutResult Empty(KataBeamRebarSpec spec, string warning) => new()
    {
        BeamName = spec.BeamName,
        Warnings = new[] { warning }
    };
}
