using System;
using System.Collections.Generic;
using System.Linq;
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

    /// <summary>Lays out the spec with the rules derived from user settings and sheet cells.</summary>
    public static KataRebarLayoutResult Calculate(KataBeamRebarSpec spec, KataSettings settings)
    {
        if (spec is null) throw new ArgumentNullException(nameof(spec));
        return Calculate(spec, KataDetailingRuleBuilder.Build(spec, settings));
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
        // Laid out across B6 on a level top, then cut and moved to each span's width (row 20) and top (row 19).
        // Main bars are cut where a span names its own (rows 19 / 21) and each piece takes that span's bars.
        var narrowTop = KataMainBarSwap.Apply(spec, rules, stations,
            KataWidthProfile.Apply(spec, rules, stations, mainTop, -1, ref barId, spec.TopMainOf), spec.TopMainOf, +1, ref barId);
        mainTop = KataTopProfile.Drape(spec, rules, stations, narrowTop, warnings, ref barId);
        mainBottom = KataMainBarSwap.Apply(spec, rules, stations,
            KataWidthProfile.Apply(spec, rules, stations, mainBottom, +1, ref barId, spec.BottomMainOf), spec.BottomMainOf, -1, ref barId);
        var narrowExtraTop = KataWidthProfile.Apply(spec, rules, stations, extraTop, -1, ref barId);
        extraTop = KataTopProfile.Drape(spec, rules, stations, narrowExtraTop, warnings, ref barId);
        warnings.AddRange(KataWidthProfile.SpacingWarnings(spec, rules));
        var spanBottom = KataSpanBottomBarLayout.Build(spec, rules, stations, extraTop, warnings, blocking, ref barId);
        var extraBottom = KataWidthProfile.Apply(spec, rules, stations,
            KataSupportBottomBarLayout.Apply(spec, rules, stations, mainBottom, spanBottom, warnings, ref barId), +1, ref barId);
        // Bar ids follow the order bars were numbered before the ties needed the hoop zones.
        int stirrupId = barId;
        var (zones, stirrups) = KataStirrupZoneLayout.Build(spec, rules, stations, ref stirrupId);
        var (sideBars, sideTies) = KataSideBarLayout.Build(spec, rules, stations, zones, warnings, ref barId);
        var hangers = KataHangerBarLayout.Build(spec, rules, stations, warnings, ref barId);
        RenumberFrom(stirrups, barId);
        barId += stirrups.Count;

        var layerTies = KataLayerSpacerTieLayout.Build(spec, rules, stations, zones, extraTop, extraBottom,
            mainTop.Concat(mainBottom).Concat(sideBars).ToList(), warnings);
        var barSets = sideTies.Concat(layerTies).Concat(KataInnerStirrupLayout.Build(spec, rules, stations, zones, warnings)).ToList();
        if (rules.TieSpacingNote is not null && sideTies.Count + layerTies.Count > 0)
            warnings.Add(rules.TieSpacingNote);

        var layout = new KataRebarLayoutResult
        {
            BeamName = spec.BeamName,
            MainTopBars = mainTop,
            MainBottomBars = mainBottom,
            ExtraTopBars = extraTop,
            ExtraBottomBars = extraBottom,
            SideBars = sideBars,
            HangerBars = hangers,
            BarSets = barSets,
            StirrupZones = zones,
            IndividualStirrups = stirrups,
            Warnings = warnings,
            Blocking = blocking,
            TotalSteelWeightKg = Math.Round(
                WeightKg(mainTop) + WeightKg(mainBottom) + WeightKg(extraTop)
                + WeightKg(extraBottom) + WeightKg(sideBars) + WeightKg(hangers) + WeightKg(stirrups) + WeightKg(barSets), 2)
        };
        return KataBarNumbering.Apply(layout, rules.StirrupDiameter, spec);
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

    /// <summary>Steel weight of repeated flat bars (kg).</summary>
    public static double WeightKg(IReadOnlyList<KataBarSet> sets)
    {
        double total = 0.0;
        foreach (var set in sets)
        {
            double radiusM = set.Diameter / 2000.0;
            total += Math.PI * radiusM * radiusM * set.BarLength / 1000.0 * SteelDensityKgPerM3 * set.Count;
        }

        return total;
    }

    private static void RenumberFrom(List<KataRebarCurve> bars, int first)
    {
        for (int i = 0; i < bars.Count; i++) bars[i] = bars[i] with { BarId = first + i };
    }

    private static KataRebarLayoutResult Empty(KataBeamRebarSpec spec, string warning) => new()
    {
        BeamName = spec.BeamName,
        Warnings = new[] { warning }
    };
}
