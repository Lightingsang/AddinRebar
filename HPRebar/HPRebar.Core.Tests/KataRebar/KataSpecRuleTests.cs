using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// The numeric rules of docs/specs/kata-beam-rebar-rules.md: dense stirrup zones, leg rounding, the side-bar
/// warning, cell G1 and the new settings.
/// </summary>
public sealed class KataSpecRuleTests
{
    /// <summary>The seismic dense zone of spec § 6.1 / TCVN 9386: max(2h, 0.25 L).</summary>
    private static readonly KataSettings Seismic = KataSettings.Default with { DenseZoneHeightFactor = 2.0 };

    [Fact]
    public void With_a_height_factor_of_2_the_dense_zone_is_2h_when_that_is_longer_than_a_quarter_of_the_span()
    {
        // Kata draws 0.25 × L (the default); the seismic 2h rule stays available through the settings.
        var plan = KataRebarPlanner.Plan(KataDamSheetParser.Parse(KataRebarTestSheets.TwoSpans()), KataRebarTestSheets.MeasuredTwoSpans(), Seismic);
        var span2 = plan.Layout.StirrupZones.Where(z => z.SpanIndex == 1).OrderBy(z => z.ZoneIndex).ToList();

        // 4500 span, h 600: max(1200, 1125) = 1200 from each face (6800 and 11300), first stirrup 50 in.
        Assert.Equal((13, 6850.0, 8000.0), (span2[0].Count, span2[0].StartStationX, span2[0].EndStationX));
        Assert.Equal((13, 10100.0, 11250.0), (span2[2].Count, span2[2].StartStationX, span2[2].EndStationX));
        Assert.Equal((10, 200.0, 8200.0, 9900.0), (span2[1].Count, span2[1].LabelSpacing, span2[1].StartStationX, span2[1].EndStationX));

        // The 6000 span keeps a quarter: 1500 > 1200.
        var span1 = plan.Layout.StirrupZones.Where(z => z.SpanIndex == 0).OrderBy(z => z.ZoneIndex).ToList();
        Assert.Equal((16, 1900.0), (span1[0].Count, span1[0].EndStationX));
    }

    [Theory]
    [InlineData(2000.0, 20)]
    [InlineData(2100.0, 21)]
    public void Dense_zones_that_fill_the_span_leave_no_middle_zone_and_no_stirrup_twice(double span, int count)
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("D11", span);

        var plan = KataRebarPlanner.Plan(KataDamSheetParser.Parse(table), KataRebarTestSheets.MeasuredSingleSpan(span: span), Seismic);
        var stations = plan.Layout.StirrupZones.SelectMany(z => z.Stations).OrderBy(x => x).ToList();

        Assert.True(plan.CanGenerate, string.Join(" | ", plan.Blocking));
        Assert.DoesNotContain(plan.Layout.StirrupZones, z => z.ZoneIndex == 1);
        Assert.Equal(count, stations.Count);
        Assert.Equal(450.0, stations[0]);
        Assert.Equal(400.0 + span - 50.0, stations[stations.Count - 1]);
        for (int i = 1; i < stations.Count; i++)
            Assert.Equal(100.0, stations[i] - stations[i - 1], 6);
    }

    [Fact]
    public void A_leg_is_rounded_up_to_25_when_the_rounded_leg_fits()
    {
        var end = KataAnchorage.Solve(400.0, 400.0, -1, 43.0, 800.0, 300.0, 514.0, legStep: 25.0);

        Assert.Equal(450.0, end.Leg);
        Assert.Equal(0.0, end.Shortfall);
    }

    [Fact]
    public void A_rounded_leg_that_would_not_fit_keeps_its_exact_length()
    {
        var end = KataAnchorage.Solve(400.0, 400.0, -1, 43.0, 800.0, 300.0, 445.0, legStep: 25.0);

        Assert.Equal(443.0, end.Leg);
        Assert.Equal(0.0, end.Shortfall);
    }

    [Fact]
    public void A_leg_bends_exactly_the_missing_length_by_default()
    {
        var rules = KataDetailingRuleBuilder.Build(KataDamSheetParser.Parse(KataRebarTestSheets.SingleSpan()));

        // Kata's drawing: no minimum (spec § 5.1 asked 15d; the user follows the drawing).
        Assert.Equal(0.0, rules.MinimumLegFactor);
        Assert.Equal(30.0, rules.LayerGap(16.0, 16.0));
        Assert.Equal(32.0, rules.LayerGap(32.0, 16.0));
        Assert.Equal(25.0, rules.BarGap(20.0));
    }

    [Theory]
    [InlineData(700.0, "", "", "", true)]
    [InlineData(690.0, "", "", "", false)]
    [InlineData(700.0, "0", "", "", false)]
    [InlineData(700.0, "", "12", "1", false)]
    [InlineData(700.0, "", "12", "0", true)]
    public void A_deep_beam_without_side_bars_is_reported_and_none_are_drawn(double height, string row20, string g4, string g5, bool reported)
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("B5", height);
        if (row20.Length > 0) table.Set("D20", row20);
        if (g4.Length > 0) table.Set("G4", double.Parse(g4, System.Globalization.CultureInfo.InvariantCulture));
        if (g5.Length > 0) table.Set("G5", double.Parse(g5, System.Globalization.CultureInfo.InvariantCulture));

        var result = KataRebarCalculator.Calculate(KataDamSheetParser.Parse(table));
        bool warned = result.Warnings.Any(w => w.Contains("thiếu cốt giá (TCVN 5574 mục 10.3.1.2)"));

        Assert.Equal(reported, warned);
        if (reported)
        {
            Assert.Empty(result.SideBars);
            Assert.Contains(result.Warnings, w => w.Contains("D20"));
        }
    }

    [Fact]
    public void A_zero_height_limit_in_the_settings_turns_the_side_bar_warning_off()
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("B5", 900.0);

        var result = KataRebarCalculator.Calculate(KataDamSheetParser.Parse(table), KataSettings.Default with { SideBarRequiredHeight = 0.0 });

        Assert.DoesNotContain(result.Warnings, w => w.Contains("thiếu cốt giá"));
    }

    [Theory]
    [InlineData("500", 500.0)]
    [InlineData("750.5", 750.5)]
    [InlineData("-300;11700", 0.0)]
    [InlineData("-300", 0.0)]
    [InlineData("0", 0.0)]
    [InlineData("abc", 0.0)]
    public void G1_is_read_only_as_one_positive_number(string text, double expected)
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("G1", text);

        var spec = KataDamSheetParser.Parse(table);

        Assert.Equal(expected, spec.CurtailedExtension);
        Assert.Equal(text, spec.CurtailedExtensionText);
    }

    [Fact]
    public void An_empty_G1_uses_the_settings_without_a_warning()
    {
        var spec = KataDamSheetParser.Parse(KataRebarTestSheets.SingleSpan());

        var rules = KataDetailingRuleBuilder.Build(spec, KataSettings.Default with { CurtailedExtensionMm = 400.0 });

        Assert.Equal(400.0, rules.CurtailedExtension);
        Assert.DoesNotContain(rules.Warnings, w => w.StartsWith("G1"));
    }

    [Fact]
    public void The_new_settings_reach_the_rules()
    {
        var settings = KataSettings.Default with
        {
            DenseZoneHeightFactor = 1.5,
            EndZoneFraction = 0.2,
            BottomExtraCutFraction = 0.2,
            MinimumLegFactor = 12.0,
            LayerClearGap = 35.0,
            RoundLegMm = 10.0,
            SideBarRequiredHeight = 800.0
        };

        var rules = KataDetailingRuleBuilder.Build(KataDamSheetParser.Parse(KataRebarTestSheets.SingleSpan()), settings);

        Assert.Equal((1.5, 0.2, 0.2, 12.0, 35.0, 10.0, 800.0),
            (rules.DenseZoneHeightFactor, rules.EndZoneFraction, rules.BottomExtraCutFraction, rules.MinimumLegFactor,
             rules.LayerClearGap, rules.RoundLegMm, rules.SideBarRequiredHeight));
        // max(1.5 × 600, 0.2 × 4000) = 900; never more than half the span.
        Assert.Equal(900.0, rules.DenseZoneLength(4000.0, 600.0));
        Assert.Equal(500.0, rules.DenseZoneLength(1000.0, 600.0));
    }

    [Fact]
    public void New_settings_out_of_range_fall_back_to_the_spec_defaults()
    {
        var back = KataSettingsJson.Read(
            "{ \"EndZoneFraction\": 0.7, \"BottomExtraCutFraction\": 0.5, \"MinimumLegFactor\": 0, " +
            "\"LayerClearGap\": -1, \"CurtailedExtensionMm\": 0, \"RoundLegMm\": -25, \"DenseZoneHeightFactor\": -2 }");

        Assert.Equal(0.25, back.EndZoneFraction);
        Assert.Equal(1.0 / 6.0, back.BottomExtraCutFraction);
        Assert.Equal(0.0, back.MinimumLegFactor);
        Assert.Equal(30.0, back.LayerClearGap);
        Assert.Equal(0.0, back.CurtailedExtensionMm);
        Assert.Equal(25.0, back.RoundLegMm);
        Assert.Equal(0.0, back.DenseZoneHeightFactor);
    }
    /// <summary>a150 / a200: where the dense zones meet (Ln ≤ 4h) no gap is wider than 150; elsewhere none wider than 200.</summary>
    [Theory]
    [InlineData(1898.0, 500.0, 150.0)]
    [InlineData(2198.0, 600.0, 150.0)]
    [InlineData(2000.0, 600.0, 150.0)]
    [InlineData(1500.0, 600.0, 150.0)]
    [InlineData(2100.0, 500.0, 200.0)]
    [InlineData(2150.0, 500.0, 200.0)]
    [InlineData(2350.0, 500.0, 200.0)]
    public void Stirrup_gaps_never_exceed_the_spacing_of_their_zone(double span, double height, double widest)
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("B5", height);
        table.Set("D11", span);
        table.Set("G7", "a150");
        table.Set("G8", "a200");

        var result = KataRebarCalculator.Calculate(KataDamSheetParser.Parse(table), Seismic);
        var stations = result.StirrupZones.SelectMany(z => z.Stations).OrderBy(x => x).ToList();

        Assert.Equal(450.0, stations[0], 6);
        Assert.Equal(400.0 + span - 50.0, stations[stations.Count - 1], 6);
        Assert.DoesNotContain(result.StirrupZones, z => z.Spacing > widest + 1e-6);
        for (int i = 1; i < stations.Count; i++)
        {
            double gap = stations[i] - stations[i - 1];
            Assert.True(gap >= 75.0 - 1e-6 && gap <= widest + 1e-6, $"{stations[i - 1]:0.#} → {stations[i]:0.#}");
        }
    }

    [Fact]
    public void Settings_that_are_not_finite_fall_back_to_the_defaults_in_the_rules()
    {
        var settings = KataSettings.Default with
        {
            DenseZoneHeightFactor = double.NaN,
            LayerClearGap = double.PositiveInfinity,
            BottomExtraCutFraction = 0.48,
            EndZoneFraction = double.NaN
        };

        var rules = KataDetailingRuleBuilder.Build(KataDamSheetParser.Parse(KataRebarTestSheets.SingleSpan()), settings);
        var result = KataRebarCalculator.Calculate(KataDamSheetParser.Parse(KataRebarTestSheets.SingleSpan()), settings);

        Assert.Equal((0.0, 30.0, 0.48, 0.25), (rules.DenseZoneHeightFactor, rules.LayerClearGap, rules.BottomExtraCutFraction, rules.EndZoneFraction));
        Assert.Equal(3, result.StirrupZones.Count);
        var sanitised = KataSettingsJson.Sanitize(settings);
        Assert.Equal(sanitised, KataSettingsJson.Read(KataSettingsJson.Write(sanitised)));
    }

    [Theory]
    [InlineData(910.0)]
    [InlineData(1020.0)]
    [InlineData(1450.0)]
    public void Stirrups_of_a_short_span_never_stand_closer_than_half_their_spacing(double span)
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("D11", span);

        var plan = KataRebarPlanner.Plan(KataDamSheetParser.Parse(table), KataRebarTestSheets.MeasuredSingleSpan(span: span), KataSettings.Default);
        var stations = plan.Layout.StirrupZones.SelectMany(z => z.Stations).OrderBy(x => x).ToList();

        for (int i = 1; i < stations.Count; i++)
            Assert.True(stations[i] - stations[i - 1] >= 50.0 - 1e-6 && stations[i] - stations[i - 1] <= 200.0 + 1e-6,
                $"{span}: {string.Join(" ", stations.Select(x => x.ToString("0.#")))}");
    }
}
