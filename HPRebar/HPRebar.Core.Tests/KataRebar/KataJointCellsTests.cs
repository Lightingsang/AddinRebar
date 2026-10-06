using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// Support cells B01 fills beyond the top bars: row 24 "*" (no joint stirrups at K) and row 17 at the support of no
/// width I (2f20 through the joint).
/// </summary>
public sealed class KataJointCellsTests
{
    private static KataCellTable B01(params (string Address, object Value)[] cells)
    {
        var table = KataB01DrawingTests.Sheet();
        foreach (var (address, value) in cells) table.Set(address, value);
        return table;
    }

    private static KataRebarPlan Plan(KataCellTable table) => KataRebarPlanner.Plan(KataDamSheetParser.Parse(table), null);

    private static (double Min, double Max) Extent(KataRebarCurve bar) =>
        (bar.Polyline.Points.Min(p => p.X), bar.Polyline.Points.Max(p => p.X));

    [Fact]
    public void K24_star_is_reported_as_kept_not_as_unsupported()
    {
        var note = Assert.Single(Plan(B01()).Skipped, s => s.StartsWith("K24"));

        Assert.Contains("đúng như bản vẽ", note);
        Assert.DoesNotContain("chưa hỗ trợ", note);
    }

    [Fact]
    public void I17_becomes_the_bottom_bars_of_the_joined_span_H_J()
    {
        var plan = Plan(B01());

        // Joined span H + J: 18100 … 24600 (6500); cut min(0.2 L, L/6) = 1100 from each face (Kata: 18950 … 23300).
        var joint = plan.Layout.ExtraBottomBars.Where(b => Extent(b).Min > 18100 && Extent(b).Max < 24600).ToList();
        Assert.Equal(2, joint.Count);
        Assert.All(joint, b => { Assert.Equal(20.0, b.Diameter); Assert.Equal(19200, Extent(b).Min, 0); Assert.Equal(23500, Extent(b).Max, 0); });
        Assert.Contains(plan.Skipped, s => s.StartsWith("I17") && s.Contains("R-51"));
    }

    [Theory]
    [InlineData("H17")]
    [InlineData("H18")]
    public void Bars_through_a_joint_whose_span_has_bars_of_its_own_are_only_reported(string own)
    {
        var alone = Plan(B01((own, "2f16"), ("I17", "")));
        var plan = Plan(B01((own, "2f16")));

        Assert.Contains(plan.Skipped, s => s.StartsWith("I17") && s.Contains("đã có thép"));
        Assert.DoesNotContain(plan.Layout.ExtraBottomBars, b => b.Diameter == 20.0 && Extent(b).Min > 18100 && Extent(b).Max < 24600);
        Assert.Equal(alone.Layout.ExtraBottomBars.Select(Extent), plan.Layout.ExtraBottomBars.Select(Extent));
    }

    [Fact]
    public void Bars_through_a_joint_near_a_face_are_only_reported()
    {
        var plan = Plan(B01(("H11", 500.0), ("J11", 6000.0)));

        Assert.Contains(plan.Skipped, s => s.StartsWith("I17") && s.Contains("nút gần mặt gối"));
    }

    [Fact]
    public void Bottom_bars_at_a_support_of_unreadable_width_are_reported()
    {
        var table = KataRebarTestSheets.TwoSpans();
        table.Set("E11", "abc");
        table.Set("E17", "2f16");

        Assert.Contains(Plan(table).Skipped, s => s.StartsWith("E17"));
    }

    [Fact]
    public void Row_17_over_a_support_with_a_width_is_drawn_and_row_18_there_is_reported()
    {
        var table = KataRebarTestSheets.TwoSpans();
        table.Set("E11", 400.0);
        table.Set("E17", "2f16");
        table.Set("E18", "2f16");

        var plan = Plan(table);

        Assert.DoesNotContain(plan.Skipped, s => s.StartsWith("E17"));
        Assert.Contains(plan.Skipped, s => s.StartsWith("E18"));
        Assert.Equal(4, plan.Layout.ExtraBottomBars.Count(b => b.HostSupportIndex == 1 && b.Diameter == 16.0));
    }

    [Fact]
    public void Blank_bottom_cells_at_a_support_are_not_reported()
    {
        var table = KataRebarTestSheets.TwoSpans();
        table.Set("E17", "0");
        table.Set("E18", "-");

        Assert.DoesNotContain(Plan(table).Skipped, s => s.StartsWith("E17") || s.StartsWith("E18"));
    }
}
