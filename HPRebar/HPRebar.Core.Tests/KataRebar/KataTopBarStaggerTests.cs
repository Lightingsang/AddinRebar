using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// Staggered cut-off of the additional top bars (G1 "Kéo thép gia cường") on the two-span live-check beam:
/// 400 | 6000 | 400 | 4500 | 400, interior support E at 6400-6800. Every row reaches H5 0.25 × its own side's
/// span from the face (Kata drawing of T2-DY7): 1500 into the 6000 span (4900), 1125 → 1150 into the 4500 span
/// (7950); outer rows then G1 further.
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
        Assert.Equal((4900.0, 7950.0), Reach(plan, 1, 3));
        Assert.Equal((4400.0, 8450.0), Reach(plan, 1, 2));
        Assert.Equal((3900.0, 8950.0), Reach(plan, 1, 1));
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
        Assert.Equal((4900.0, 7950.0), Reach(plan, 1, 3));
        Assert.Equal((4400.0, 8450.0), Reach(plan, 1, 1));
    }

    [Fact]
    public void Every_row_reaches_H5_of_its_own_span_before_the_stagger()
    {
        var table = Sheet();
        table.Set("H5", 0.4);
        table.Set("E13", "2f20");
        table.Set("E14", "2f18");

        var plan = Plan(table);

        // Row 14: 0.4 × 6000 = 2400 (4000) and 0.4 × 4500 = 1800 (8600); row 13 another 500 each way.
        Assert.Equal((4000.0, 8600.0), Reach(plan, 1, 2));
        Assert.Equal((3500.0, 9100.0), Reach(plan, 1, 1));
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
        Assert.Equal((4200.0, 8650.0), Reach(plan, 1, 1));
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
        Assert.Equal((4400.0, 8450.0), Reach(plan, 1, 1));
    }

    [Fact]
    public void A_zero_step_in_the_settings_turns_the_stagger_off()
    {
        var table = Sheet();
        table.Set("E13", "2f20");
        table.Set("E14", "2f18");

        var plan = Plan(table, KataSettings.Default with { CurtailedExtensionMm = 0.0 });

        Assert.Equal((4900.0, 7950.0), Reach(plan, 1, 1));
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

        // Row 14: 1500 + 3000 = 4500 (1900) and 1150 + 3000 = 4150 (10950). Row 13 would need −1100 / 13950:
        // held at the faces of supports C (400) and G (11300).
        Assert.Equal((1900.0, 10950.0), Reach(plan, 1, 2));
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

        // Support C: row 14 reaches 400 + 1500 = 1900, row 13 500 further (2400); both anchor in C.
        Assert.Equal(2400.0, Reach(plan, 0, 1).Right);
        Assert.Equal(1900.0, Reach(plan, 0, 2).Right);
    }

    [Fact]
    public void Both_sides_of_a_left_right_cell_are_staggered()
    {
        var table = Sheet();
        table.Set("E13", "2f20;2f16");
        table.Set("E14", "2f18");

        var plan = Plan(table);
        var row13 = plan.Layout.ExtraTopBars.Where(b => b.HostSupportIndex == 1 && b.Layer == 1).ToList();

        Assert.Equal(4400.0, row13.Where(b => b.BarMark == "3.2.1T").Min(b => b.Polyline.Points.Min(p => p.X)));
        Assert.Equal(8450.0, row13.Where(b => b.BarMark == "3.2.1P").Max(b => b.Polyline.Points.Max(p => p.X)));
    }
    [Fact]
    public void The_weaker_side_running_through_the_support_counts_as_an_end_of_its_row()
    {
        var table = Sheet();
        table.Set("H5", 0.1);
        table.Set("E13", "2f20");
        table.Set("E14", "2f25;2f18");

        var plan = Plan(table);

        // Row 14: the Ø25 side is cut 600 from the face (5800), the weaker Ø18 side runs through E and on
        // 40 × 18 = 720 into the left span (5680). Row 13 clears the furthest one: 720 + 500 → 1250 → 5150.
        // Right span: only the cut at 450 → row 13 950 (7750).
        Assert.True(plan.CanGenerate, string.Join(" | ", plan.Blocking));
        Assert.Equal((5150.0, 7750.0), Reach(plan, 1, 1));
    }

    [Fact]
    public void An_outer_row_held_at_the_face_across_a_short_span_is_reported_with_what_it_got()
    {
        var table = Sheet();
        table.Set("D11", 1500.0);
        table.Set("F11", 8000.0);
        table.Set("G1", 2000.0);
        table.Set("H5", 0.15);
        table.Set("E13", "2f20");
        table.Set("E14", "2f18");

        var result = KataRebarCalculator.Calculate(KataDamSheetParser.Parse(table));
        var row13 = result.ExtraTopBars.Where(b => b.HostSupportIndex == 1 && b.Layer == 1).ToList();

        // Support E at 1900-2300: row 14 reaches 0.15 × 1500 = 225 → 250 (1650); row 13 would need 2250 but the
        // 1500 span ends at C's face (400): it gets 1500, i.e. 1250 beyond row 14.
        Assert.All(row13, b => Assert.Equal(400.0, b.Polyline.Points.Min(p => p.X), 6));
        Assert.Contains(result.Warnings, w => w.Contains("chỉ vươn xa hơn hàng 14 1250 mm") && w.Contains("nhịp trái"));
    }
}
