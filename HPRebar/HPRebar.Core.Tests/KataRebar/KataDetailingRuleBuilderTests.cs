using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

public sealed class KataDetailingRuleBuilderTests
{
    private static KataBeamRebarSpec Spec(double coverMain, double coverStirrup) => new()
    {
        Width = 300.0,
        Height = 600.0,
        CoverMain = coverMain,
        CoverStirrup = coverStirrup,
        TopContinuous = new KataBarItem(3, 20.0),
        BottomContinuous = new KataBarItem(4, 20.0),
        GlobalStirrup = new KataStirrupSpec { Diameter = 8.0 }
    };

    [Fact]
    public void The_bars_rest_on_the_stirrup_and_a_is_only_where_they_stop()
    {
        // As Kata draws B01 with "50/25": centre 25 + 8 + 20/2 across the section, ends 50 short of the concrete.
        var rules = KataDetailingRuleBuilder.Build(Spec(50.0, 25.0));

        Assert.Equal(43.0, rules.TopBarCentreDepth);
        Assert.Equal(43.0, rules.BottomBarCentreDepth);
        Assert.Equal(50.0, rules.TopEndCover);
        Assert.Equal(50.0, rules.BottomEndCover);
        Assert.Equal(25.0, rules.StirrupCover);
        Assert.Empty(rules.Warnings);
        Assert.Empty(rules.Errors);
    }

    [Fact]
    public void A_single_number_puts_the_stirrup_against_the_main_bars()
    {
        // b = a − d/2 − d_stirrup = 40 − 10 − 8 = 22: the bars' centre stays at 40, and they stop 40 short of the ends.
        var rules = KataDetailingRuleBuilder.Build(Spec(40.0, 0.0));

        Assert.Equal(22.0, rules.StirrupCover);
        Assert.Equal(40.0, rules.TopBarCentreDepth);
        Assert.Equal(40.0, rules.TopEndCover);
        Assert.Equal(110.0, rules.EdgeBarOffset(300.0, 20.0));
        Assert.Empty(rules.Warnings);
    }

    [Fact]
    public void A_single_number_leaving_a_thin_stirrup_cover_warns()
    {
        var rules = KataDetailingRuleBuilder.Build(Spec(30.0, 0.0));

        Assert.Equal(12.0, rules.StirrupCover);
        Assert.Contains(rules.Warnings, w => w.Contains("12 mm"));
    }

    [Fact]
    public void A_single_number_leaving_no_room_for_the_stirrup_blocks()
    {
        var rules = KataDetailingRuleBuilder.Build(Spec(15.0, 0.0));

        Assert.NotEmpty(rules.Errors);
    }

    [Fact]
    public void An_empty_cell_uses_a_25_mm_stirrup_cover_and_bars_resting_on_it()
    {
        var rules = KataDetailingRuleBuilder.Build(Spec(0.0, 0.0));

        Assert.Equal(25.0, rules.StirrupCover);
        Assert.Equal(43.0, rules.TopBarCentreDepth); // 25 + 8 + 20/2
        Assert.Equal(43.0, rules.BottomBarCentreDepth);
    }

    [Fact]
    public void A_bar_cover_smaller_than_the_stirrup_moves_only_the_ends()
    {
        // B01 "30/25", stirrup Ø10, bars Ø25: centre 47.5 on the stirrup, ends 30 short of the concrete, nothing to warn.
        var spec = Spec(30.0, 25.0) with
        {
            TopContinuous = new KataBarItem(6, 25.0),
            BottomContinuous = new KataBarItem(6, 25.0),
            GlobalStirrup = new KataStirrupSpec { Diameter = 10.0 }
        };

        var rules = KataDetailingRuleBuilder.Build(spec);

        Assert.Equal(47.5, rules.TopBarCentreDepth);
        Assert.Equal(47.5, rules.BottomBarCentreDepth);
        Assert.Equal(30.0, rules.TopEndCover);
        Assert.Empty(rules.Warnings);
    }

    [Fact]
    public void Anchorage_factors_come_from_G2_and_G3()
    {
        var rules = KataDetailingRuleBuilder.Build(Spec(0.0, 0.0) with { TensionLapMultiplier = 45.0, CompressionLapMultiplier = 35.0 });

        Assert.Equal(45.0, rules.TopAnchorageFactor);
        Assert.Equal(35.0, rules.BottomAnchorageFactor);
    }

    [Fact]
    public void Two_layers_deeper_than_the_beam_block()
    {
        var rules = KataDetailingRuleBuilder.Build(Spec(0.0, 0.0) with { Height = 80.0 });

        Assert.NotEmpty(rules.Errors);
    }
}
