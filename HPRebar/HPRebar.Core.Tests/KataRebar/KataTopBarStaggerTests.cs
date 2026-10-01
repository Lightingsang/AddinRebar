using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// Staggered cut-off of the additional top bars (G1 "Kéo thép gia cường") on the two-span live-check beam:
/// 400 | 6000 | 400 | 4500 | 400, interior support E at 6400-6800. H5 0.25 and H3 0.2 both from the face,
/// so over E row 13 reaches 1500 (4900 / 8300) and rows 14-16 reach 1200 (5200 / 8000).
/// </summary>
public sealed class KataTopBarStaggerTests
{
    private static KataCellTable Sheet()
    {
        var table = KataRebarTestSheets.TwoSpans();
        table.Set("H5", 0.25);
        table.Set("I5", "L từ mép cột");
        table.Set("H3", 0.2);
        table.Set("I3", "L từ mép cột");
        return table;
    }

    private static KataRebarPlan Plan(KataCellTable table, KataSettings? settings = null) =>
        KataRebarPlanner.Plan(KataDamSheetParser.Parse(table), KataRebarTestSheets.MeasuredTwoSpans(), settings);

    private static (double Left, double Right) Reach(KataRebarPlan plan, int support, int layer)
    {
        var bars = plan.Layout.ExtraTopBars.Where(b => b.HostSupportIndex == support && b.Layer == layer).ToList();
        Assert.NotEmpty(bars);
        return (bars.Min(b => b.Polyline.Points.Min(p => p.X)), bars.Max(b => b.Polyline.Points.Max(p => p.X)));
    }

    [Fact]
    public void Each_outer_row_reaches_500_further_than_the_row_inside_it()
    {
        var table = Sheet();
        table.Set("E13", "2f20");
        table.Set("E14", "2f18");
        table.Set("E15", "2f18");

        var plan = Plan(table);

        Assert.True(plan.CanGenerate, string.Join(" | ", plan.Blocking));
        Assert.Equal((5200.0, 8000.0), Reach(plan, 1, 3));
        Assert.Equal((4700.0, 8500.0), Reach(plan, 1, 2));
        Assert.Equal((4200.0, 9000.0), Reach(plan, 1, 1));
        Assert.DoesNotContain(plan.Warnings, w => w.Contains("G1"));
    }

    [Fact]
    public void An_empty_row_takes_no_step()
    {
        var table = Sheet();
        table.Set("E13", "2f20");
        table.Set("E15", "2f18");

        var plan = Plan(table);

        // Row 15 is the next filled row inside row 13: one step of 500, not two.
        Assert.Equal((5200.0, 8000.0), Reach(plan, 1, 3));
        Assert.Equal((4700.0, 8500.0), Reach(plan, 1, 1));
    }

    [Fact]
    public void A_row_already_far_enough_is_left_alone()
    {
        var table = Sheet();
        table.Set("H5", 0.4);
        table.Set("E13", "2f20");
        table.Set("E14", "2f18");

        var plan = Plan(table);

        // 0.4 × 6000 = 2400 from the faces: 4000 / 9200, beyond 5200 − 500 and 8000 + 500 already.
        Assert.Equal((4000.0, 9200.0), Reach(plan, 1, 1));
    }

    [Fact]
    public void G1_gives_the_step_when_it_holds_a_positive_number()
    {
        var table = Sheet();
        table.Set("G1", 700.0);
        table.Set("E13", "2f20");
        table.Set("E14", "2f18");

        var plan = Plan(table);

        Assert.Equal(700.0, plan.Rules.CurtailedExtension);
        Assert.Equal((4500.0, 8700.0), Reach(plan, 1, 1));
    }

    [Fact]
    public void A_G1_that_is_not_one_number_falls_back_to_the_settings_and_is_reported()
    {
        var table = Sheet();
        table.Set("G1", "-300;11700");
        table.Set("E13", "2f20");
        table.Set("E14", "2f18");

        var plan = Plan(table);

        Assert.Equal(500.0, plan.Rules.CurtailedExtension);
        Assert.Contains(plan.Warnings, w => w.StartsWith("G1 '-300;11700'") && w.Contains("500"));
        Assert.Equal((4700.0, 8500.0), Reach(plan, 1, 1));
    }

    [Fact]
    public void A_zero_step_in_the_settings_turns_the_stagger_off()
    {
        var table = Sheet();
        table.Set("E13", "2f20");
        table.Set("E14", "2f18");

        var plan = Plan(table, KataSettings.Default with { CurtailedExtensionMm = 0.0 });

        Assert.Equal((4900.0, 8300.0), Reach(plan, 1, 1));
    }

    [Fact]
    public void A_step_that_would_cross_the_span_stops_at_the_support_across_it_and_is_reported()
    {
        var table = Sheet();
        table.Set("G1", 3000.0);
        table.Set("E13", "2f20");
        table.Set("E14", "2f18");
        table.Set("E15", "2f18");

        var plan = Plan(table);

        // Row 14: 2200 / 11000. Row 13 would need −800 / 14000: held at the faces of supports C (400) and G (11300).
        Assert.Equal((2200.0, 11000.0), Reach(plan, 1, 2));
        Assert.Equal((400.0, 11300.0), Reach(plan, 1, 1));
        Assert.Equal(2, plan.Warnings.Count(w => w.Contains("nhịp không đủ chỗ cắt lệch")));
    }

    [Fact]
    public void Over_an_end_support_only_the_span_side_is_staggered()
    {
        var table = Sheet();
        table.Set("C13", "2f20");
        table.Set("C14", "2f18");

        var plan = Plan(table);

        // Support C: row 14 reaches 400 + 1200 = 1600, so row 13 goes from 1900 to 2100; both anchor in C.
        Assert.Equal(2100.0, Reach(plan, 0, 1).Right);
        Assert.Equal(1600.0, Reach(plan, 0, 2).Right);
    }

    [Fact]
    public void Both_sides_of_a_left_right_cell_are_staggered()
    {
        var table = Sheet();
        table.Set("E13", "2f20;2f16");
        table.Set("E14", "2f18");

        var plan = Plan(table);
        var row13 = plan.Layout.ExtraTopBars.Where(b => b.HostSupportIndex == 1 && b.Layer == 1).ToList();

        Assert.Equal(4700.0, row13.Where(b => b.BarMark == "3.2.1T").Min(b => b.Polyline.Points.Min(p => p.X)));
        Assert.Equal(8500.0, row13.Where(b => b.BarMark == "3.2.1P").Max(b => b.Polyline.Points.Max(p => p.X)));
    }
    [Fact]
    public void The_weaker_side_running_through_the_support_counts_as_an_end_of_its_row()
    {
        var table = Sheet();
        table.Set("H5", 0.15);
        table.Set("H3", 0.1);
        table.Set("E13", "2f20");
        table.Set("E14", "2f25;2f18");

        var plan = Plan(table);

        // Row 14: the Ø25 side is cut 600 from the face (5800), the weaker Ø18 side runs through E and on
        // 40 × 18 = 720 into the left span (5680). Row 13 clears the furthest one: 720 + 500 → 1250 → 5150.
        // Right span: only the cut at 600 → row 13 from 900 to 1100 (7900).
        Assert.True(plan.CanGenerate, string.Join(" | ", plan.Blocking));
        Assert.Equal((5150.0, 7900.0), Reach(plan, 1, 1));
    }

    [Fact]
    public void An_outer_row_that_ends_short_of_the_inner_one_is_reported_with_the_difference()
    {
        var table = Sheet();
        table.Set("D11", 1500.0);
        table.Set("F11", 8000.0);
        table.Set("H5", 0.15);
        table.Set("H3", 0.2);
        table.Set("E13", "2f20");
        table.Set("E14", "2f18");

        var result = KataRebarCalculator.Calculate(KataDamSheetParser.Parse(table));
        var row13 = result.ExtraTopBars.Where(b => b.HostSupportIndex == 1 && b.Layer == 1).ToList();

        // Support E at 1900-2300, L = 8000: row 14 reaches 1600 (to 300, inside support C), row 13 only 1200.
        // The 1500 span holds row 13 at C's face (400): 100 short of row 14.
        Assert.All(row13, b => Assert.Equal(400.0, b.Polyline.Points.Min(p => p.X), 6));
        Assert.Contains(result.Warnings, w => w.Contains("ngắn hơn hàng 14 100 mm") && w.Contains("nhịp trái"));
    }
}
