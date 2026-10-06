using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>A two-span sheet (400 | 6000 | 400 | 4500 | 400) planned on the beams Revit measured.</summary>
public sealed class KataMultiSpanPlanTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void A_two_span_run_is_planned_whether_Revit_draws_it_as_one_or_two_beams(int pieces)
    {
        var plan = KataRebarPlanner.Plan(KataDamSheetParser.Parse(KataRebarTestSheets.TwoSpans()), KataRebarTestSheets.MeasuredTwoSpans(pieces));

        Assert.True(plan.CanGenerate, string.Join(" | ", plan.Blocking));
        Assert.Equal(2, plan.Spec.Spans.Count);
        // Main bars run on from the first support to the last: 11700 − 50 at the far face.
        Assert.All(plan.Layout.MainTopBars, b => Assert.Equal((50.0, 11650.0), (b.Polyline.Points[1].X, b.Polyline.Points[b.Polyline.Points.Count - 2].X)));
        Assert.Equal(6, plan.Layout.StirrupZones.Count);
    }

    [Fact]
    public void A_main_bar_longer_than_a_stock_bar_is_drawn_whole_and_not_reported()
    {
        var plan = KataRebarPlanner.Plan(KataDamSheetParser.Parse(KataRebarTestSheets.TwoSpans()), KataRebarTestSheets.MeasuredTwoSpans());

        // 11600 straight + 2 × 450 legs = 12500, one bar: splitting into stock lengths is shop-drawing work.
        Assert.All(plan.Layout.MainTopBars, b => Assert.Equal(12500.0, b.TotalLength, 6));
        Assert.DoesNotContain(plan.Warnings, w => w.Contains("cây thép"));
    }

    [Fact]
    public void A_different_span_in_the_sheet_blocks()
    {
        var table = KataRebarTestSheets.TwoSpans();
        table.Set("F11", 4600.0);

        var plan = KataRebarPlanner.Plan(KataDamSheetParser.Parse(table), KataRebarTestSheets.MeasuredTwoSpans());

        Assert.False(plan.CanGenerate);
        Assert.Contains(plan.Blocking, b => b.StartsWith("F11"));
    }
}
