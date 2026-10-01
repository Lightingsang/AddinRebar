using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

public sealed class KataSettingsTests
{
    [Fact]
    public void Defaults_are_those_of_the_Kata_detail_dialog()
    {
        var s = KataSettings.Default;

        Assert.Equal(11700.0, s.MaxBarLength);
        Assert.Equal((135, 7.5), (s.ClosedStirrupHookAngle, s.ClosedStirrupHookFactor));
        Assert.Equal((180, 7.5), (s.CrossTieHookAngle, s.CrossTieHookFactor));
        Assert.Equal(50.0, s.RoundCutExtraMm);
        Assert.Equal(10.0, s.SideBarAnchorageFactor);
        Assert.Equal(400.0, s.SideBarTieSpacing);
    }

    [Fact]
    public void The_rules_take_the_settings()
    {
        var spec = KataDamSheetParser.Parse(KataRebarTestSheets.SingleSpan());
        var custom = KataSettings.Default with { SideBarAnchorageFactor = 15.0, MaxBarLength = 9000.0, RoundCutExtraMm = 100.0, SideBarTieSpacing = 300.0, CrossTieHookAngle = 135 };

        var rules = KataDetailingRuleBuilder.Build(spec, custom);

        Assert.Equal(15.0, rules.SideBarAnchorageFactor);
        Assert.Equal(9000.0, rules.MaxBarLength);
        Assert.Equal(300.0, rules.SideBarTieSpacing);
        Assert.Equal(135, rules.CrossTieHookAngle);
        Assert.Equal(900.0, rules.RoundUp(857.1));
        Assert.Equal(800.0, rules.RoundDown(857.1));
    }

    [Fact]
    public void Additional_bars_are_cut_on_the_rounding_step()
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("H3", 0.21);
        table.Set("C14", "2f18");
        table.Set("D18", "2f20");
        var plan = KataRebarPlanner.Plan(KataDamSheetParser.Parse(table), KataRebarTestSheets.MeasuredSingleSpan());

        // Row 14 reach 0.21 × 6000 = 1260 → 1300 from the face; row 18 kept 0.15 × 6000 = 900 from each face.
        Assert.All(plan.Layout.ExtraTopBars, b => Assert.Equal(400.0 + 1300.0, b.Polyline.Points[b.Polyline.Points.Count - 1].X, 6));
        Assert.All(plan.Layout.ExtraBottomBars, b => Assert.Equal((1300.0, 5500.0), (b.Polyline.Points[0].X, b.Polyline.Points[1].X)));
    }
}
