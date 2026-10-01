using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// Inner stirrups on the live-check beam (300×600, top 3Ø20 at y −107 / 0 / 107, stirrup Ø8, cover 25): the
/// legs sit (20 + 8) / 2 = 14 outside the named bars, the branches on the outer hoop's centrelines −29 / −571.
/// </summary>
public sealed class KataInnerStirrupTests
{
    private static KataRebarPlan Plan(params (string Address, object Value)[] cells)
    {
        var table = KataRebarTestSheets.SingleSpan();
        foreach (var (address, value) in cells) table.Set(address, value);
        return KataRebarPlanner.Plan(KataDamSheetParser.Parse(table), KataRebarTestSheets.MeasuredSingleSpan());
    }

    [Fact]
    public void A_U_stirrup_is_open_at_the_top_with_its_legs_outside_the_named_bars()
    {
        var plan = Plan(("C25", "Đai U"), ("D25", "1-2"));
        var sets = plan.Layout.BarSets;

        Assert.True(plan.CanGenerate);
        Assert.Equal(3, sets.Count);
        var points = sets[0].Shape.Points;
        Assert.Equal((-121.0, -29.0), (points[0].Y, points[0].Z));
        Assert.Equal((-121.0, -571.0), (points[1].Y, points[1].Z));
        Assert.Equal((14.0, -571.0), (points[2].Y, points[2].Z));
        Assert.Equal((14.0, -29.0), (points[3].Y, points[3].Z));
        Assert.Equal((135, 7.5), (sets[0].HookAngle, sets[0].HookFactor));
    }

    [Fact]
    public void Inner_stirrups_follow_the_outer_zones_one_stirrup_diameter_along()
    {
        var plan = Plan(("C25", "Đai U"), ("D25", "1-2"));

        Assert.Equal(plan.Layout.StirrupZones.Select(z => z.Stations[0] + 8.0), plan.Layout.BarSets.Select(s => s.Stations[0]));
        Assert.Equal(plan.Layout.StirrupZones.Select(z => (z.Count, z.Spacing)), plan.Layout.BarSets.Select(s => (s.Count, s.Spacing)));
    }

    [Fact]
    public void A_C_tie_stands_beside_its_bar_with_180_degree_hooks()
    {
        var set = Plan(("C25", "Đai C"), ("D25", "3")).Layout.BarSets[0];

        // Wraps top bar 3 (107, −43) and the bottom bar position below it (107, −557), the tie on the centre side.
        Assert.Equal(new[] { (107.0, -43.0), (107.0, -557.0) }, set.Shape.Points.Select(p => (p.Y, p.Z)).ToArray());
        Assert.True(set.WrapEnds);
        Assert.Equal((-1.0, 0.0), (set.WrapOffset.Y, set.WrapOffset.Z));
        Assert.Equal(180, set.HookAngle);
    }

    [Fact]
    public void A_closed_hoop_over_all_the_top_bars_is_the_outer_hoop()
    {
        Assert.Empty(Plan(("C25", "Đai □"), ("D25", "1-3")).Layout.BarSets);
        Assert.Equal(5, Plan(("C25", "Đai □"), ("D25", "2-3")).Layout.BarSets[0].Shape.Points.Count);
    }

    [Fact]
    public void Bars_the_section_does_not_have_are_reported()
    {
        var plan = Plan(("C25", "Đai U"), ("D25", "2-5"), ("C26", "Đai C"), ("D26", "x"));

        Assert.Empty(plan.Layout.BarSets);
        Assert.Contains(plan.Warnings, w => w.StartsWith("C25 'Đai U 2-5'"));
        Assert.Contains(plan.Warnings, w => w.StartsWith("C26 'Đai C x'"));
    }

    [Fact]
    public void Row_22_sets_the_span_spacings_with_its_own_last_zone()
    {
        var zones = Plan(("D22", "a100/200/50")).Layout.StirrupZones;

        Assert.Equal(new[] { 100.0, 200.0, 50.0 }, zones.Select(z => z.Spacing).ToArray());
        Assert.Empty(Plan(("D22", "a100/200/50")).Skipped.Where(s => s.StartsWith("D22")));
    }

    [Fact]
    public void J7_is_reported_as_not_drawn()
    {
        Assert.Contains(Plan(("J7", "a500")).Skipped, s => s.StartsWith("J7 'a500'"));
    }
}
