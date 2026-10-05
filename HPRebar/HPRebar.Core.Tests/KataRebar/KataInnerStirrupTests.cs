using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// Inner stirrups on the live-check beam (300×600, top 3Ø20, stirrup Ø8, cover 25, I8 = 2, J7 = a500): bars are
/// counted from the left of Kata's section, which looks along the beam, so bar 1 is at y +107, bar 2 at 0, bar 3 at
/// −107; the legs sit (20 + 8) / 2 = 14 outside the named bars, the branches on the outer hoop's centrelines −29 / −571.
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
    public void A_U_stirrup_is_open_at_the_top_its_legs_outside_the_named_bars_turned_in_and_down()
    {
        var plan = Plan(("C25", "Đai U"), ("D25", "1-2"));
        var sets = plan.Layout.BarSets;

        Assert.True(plan.CanGenerate);
        Assert.Single(sets);
        // B01 section 2-2: each leg turns in 4 Ø and down 5.5 Ø (32 and 44 for Ø8).
        Assert.Equal(new[] { (89.0, -73.0), (89.0, -29.0), (121.0, -29.0), (121.0, -571.0), (-14.0, -571.0), (-14.0, -29.0), (18.0, -29.0), (18.0, -73.0) },
            sets[0].Shape.Points.Select(p => (p.Y, p.Z)).ToArray());
        Assert.Equal(0, sets[0].HookAngle);
    }

    [Fact]
    public void With_I8_2_inner_stirrups_go_every_J7_one_stirrup_diameter_short_of_the_C_ties()
    {
        var plan = Plan(("C25", "Đai U"), ("D25", "1-2"), ("G4", 12.0), ("G5", 2));
        var ties = plan.Layout.BarSets.Where(KataBarNumbering.IsTie).ToList();
        var inner = plan.Layout.BarSets.Single(s => !KataBarNumbering.IsTie(s));

        Assert.Equal(500.0, inner.Spacing);
        Assert.Equal(ties[0].Stations.Select(x => x - 8.0), inner.Stations);
    }

    [Fact]
    public void With_I8_1_inner_stirrups_follow_the_outer_zones_one_stirrup_diameter_along()
    {
        var plan = Plan(("C25", "Đai U"), ("D25", "1-2"), ("I8", 1));

        Assert.Equal(plan.Layout.StirrupZones.Select(z => z.Stations[0] + 8.0), plan.Layout.BarSets.Select(s => s.Stations[0]));
        Assert.Equal(plan.Layout.StirrupZones.Select(z => (z.Count, z.Spacing)), plan.Layout.BarSets.Select(s => (s.Count, s.Spacing)));
    }

    [Fact]
    public void A_C_tie_stands_beside_its_bar_with_180_degree_hooks()
    {
        var set = Plan(("C25", "Đai C"), ("D25", "3")).Layout.BarSets[0];

        // Wraps top bar 3 (−107, −43) and the bottom bar position below it; as every C of B01 section 2-2, its long leg
        // on the left of the bar (+Y), the hooks round to the right.
        Assert.Equal(new[] { (-107.0, -43.0), (-107.0, -557.0) }, set.Shape.Points.Select(p => (p.Y, p.Z)).ToArray());
        Assert.True(set.WrapEnds);
        Assert.Equal((1.0, 0.0), (set.WrapOffset.Y, set.WrapOffset.Z));
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

        Assert.Equal(new[] { 100.0, 200.0, 50.0 }, zones.Select(z => z.LabelSpacing).ToArray());
        Assert.Empty(Plan(("D22", "a100/200/50")).Skipped.Where(s => s.StartsWith("D22")));
    }

    [Fact]
    public void J7_is_read_not_reported_as_skipped()
    {
        Assert.DoesNotContain(Plan(("J7", "a500")).Skipped, s => s.StartsWith("J7"));
    }
}
