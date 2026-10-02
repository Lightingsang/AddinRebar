using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>Side bars on the live-check beam (300×600, 400 | 6000 | 400, J9 43/25, stirrup Ø8) with G4 12.</summary>
public sealed class KataSideBarTests
{
    private static KataRebarPlan Plan(params (string Address, object Value)[] cells)
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("G4", 12.0);
        foreach (var (address, value) in cells) table.Set(address, value);
        return KataRebarPlanner.Plan(KataDamSheetParser.Parse(table), KataRebarTestSheets.MeasuredSingleSpan());
    }

    [Fact]
    public void G5_layers_spread_between_the_main_bars_and_anchor_10d_into_the_supports()
    {
        var plan = Plan(("G5", 2));
        var bars = plan.Layout.SideBars.OrderBy(b => b.Polyline.Points[0].Z).ThenBy(b => b.TransverseY).ToList();

        Assert.True(plan.CanGenerate);
        Assert.Equal(4, bars.Count);
        // Top main −43, bottom main −557: two layers at thirds.
        Assert.Equal(-557.0 + 514.0 / 3.0, bars[0].Polyline.Points[0].Z, 6);
        Assert.Equal(-557.0 + 2.0 * 514.0 / 3.0, bars[3].Polyline.Points[0].Z, 6);
        Assert.Equal(new[] { -111.0, 111.0 }, bars.Take(2).Select(b => b.TransverseY).ToArray());
        Assert.All(bars, b => Assert.Equal((280.0, 6520.0), (b.Polyline.Points[0].X, b.Polyline.Points[1].X)));
    }

    [Fact]
    public void Each_layer_gets_C_ties_at_J7_with_180_degree_hooks()
    {
        var ties = Plan(("G5", 2)).Layout.BarSets;

        Assert.Equal(2, ties.Count);
        foreach (var set in ties)
        {
            // Two stirrup diameters beside the hoops' first station (450 + 16), every J7 = 500, up to 6400 − 66.
            Assert.Equal(12, set.Count);
            Assert.Equal(466.0, set.Stations[0], 6);
            Assert.Equal(5966.0, set.Stations[11], 6);
            Assert.Equal((180, 7.5, 8.0), (set.HookAngle, set.HookFactor, set.Diameter));
            // The two side bars the hooks wrap; the tie runs under them.
            Assert.Equal((-111.0, 111.0), (set.Shape.Points[0].Y, set.Shape.Points[1].Y));
            Assert.True(set.WrapEnds);
            Assert.Equal((0.0, -1.0), (set.WrapOffset.Y, set.WrapOffset.Z));
        }
    }

    [Fact]
    public void A_zero_part_of_a_row_20_cell_is_ignored()
    {
        Assert.Equal(4, Plan(("D20", "2f12;0")).Layout.SideBars.Count);
    }

    [Fact]
    public void Different_side_bars_of_two_spans_keep_to_their_half_of_a_narrow_interior_support()
    {
        // Equal side bars would run on through the support; here span 2 has its own (row 20 2Ø14).
        var table = KataRebarTestSheets.TwoSpans();
        table.Set("G4", 16.0);
        table.Set("G5", 1);
        table.Set("F20", "2f14");
        var plan = KataRebarPlanner.Plan(KataDamSheetParser.Parse(table), KataRebarTestSheets.MeasuredTwoSpans(),
            KataSettings.Default with { SideBarAnchorageFactor = 20.0 });

        // Interior support 6400..6800: 20 × 16 = 320 would cross; each stops at the middle less its radius.
        double leftEnd = plan.Layout.SideBars.Where(b => b.HostSpanIndex == 0).Max(b => b.Polyline.Points[1].X);
        double rightStart = plan.Layout.SideBars.Where(b => b.HostSpanIndex == 1).Min(b => b.Polyline.Points[0].X);
        Assert.Equal(6592.0, leftEnd, 6);
        Assert.Equal(6607.0, rightStart, 6);
    }

    [Fact]
    public void A_negative_G5_keeps_the_layers_and_drops_the_ties()
    {
        var plan = Plan(("G5", -2));

        Assert.Equal(4, plan.Layout.SideBars.Count);
        Assert.Empty(plan.Layout.BarSets);
    }

    [Fact]
    public void Row_20_overrides_G5_for_its_span()
    {
        Assert.Empty(Plan(("G5", 2), ("D20", "0")).Layout.SideBars);
        var two = Plan(("G5", 3), ("D20", "2f14")).Layout.SideBars;
        Assert.Equal(4, two.Count);
        Assert.All(two, b => Assert.Equal(14.0, b.Diameter));
    }

    [Fact]
    public void Row_20_counts_layers_like_G5()
    {
        // "3f12": three layers of Ø12, a bar on each face (the Kata drawing labels two layers "2x2Ø12").
        var plan = Plan(("D20", "3f12"));

        Assert.Equal(6, plan.Layout.SideBars.Count);
        Assert.Equal(3, plan.Layout.SideBars.Select(b => b.Layer).Distinct().Count());
        Assert.DoesNotContain(plan.Warnings, w => w.StartsWith("D20"));
    }

    [Fact]
    public void The_settings_set_the_anchorage_and_J7_the_tie_spacing()
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("G4", 12.0);
        table.Set("G5", 1);
        table.Set("J7", "a500");
        var plan = KataRebarPlanner.Plan(KataDamSheetParser.Parse(table), KataRebarTestSheets.MeasuredSingleSpan(),
            KataSettings.Default with { SideBarAnchorageFactor = 40.0 });

        // 40 × 12 = 480 exceeds the support: stops at its far face − a (400 − 43).
        Assert.All(plan.Layout.SideBars, b => Assert.Equal(400.0 - 357.0, b.Polyline.Points[0].X, 6));
        Assert.Equal(12, plan.Layout.BarSets[0].Count);
    }
}
