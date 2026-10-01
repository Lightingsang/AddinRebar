using System.Linq;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// Additional bottom bars of the span on the live-check beam (300×600, columns 400 | 6000 | 400, J9 43/25,
/// stirrup Ø8), here with 2Ø20 bottom main bars so the row-18 bars split one gap: D18 2Ø20, D17 2Ø18.
/// </summary>
public sealed class KataSpanBottomBarTests
{
    // 0.15 × 6000 = 900 kept free of the bars, rounded down to the 50 mm cut step.
    private const double Cut = 900.0;

    private static KataRebarPlan Plan(KataCellTable table, KataMeasuredBeam? measured = null) =>
        KataRebarPlanner.Plan(KataDamSheetParser.Parse(table), measured ?? KataRebarTestSheets.MeasuredSingleSpan());

    private static KataCellTable Sheet()
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("B12", "2f20");
        table.Set("D18", "2f20");
        table.Set("D17", "2f18");
        return table;
    }

    [Fact]
    public void Row_18_bars_split_the_gaps_of_the_bottom_main_level()
    {
        var plan = Plan(Sheet());
        var row18 = plan.Layout.ExtraBottomBars.Where(b => b.Layer == 1).OrderBy(b => b.TransverseY).ToList();

        Assert.True(plan.CanGenerate);
        Assert.Empty(plan.Warnings);
        Assert.Equal(2, row18.Count);
        Assert.Equal(-107.0 / 3.0, row18[0].TransverseY, 9);
        Assert.Equal(107.0 / 3.0, row18[1].TransverseY, 9);
        foreach (var bar in row18)
        {
            Assert.Equal(2, bar.Polyline.Points.Count);
            Assert.Equal(400.0 + Cut, bar.Polyline.Points[0].X, 6);
            Assert.Equal(6400.0 - Cut, bar.Polyline.Points[1].X, 6);
            Assert.Equal(-557.0, bar.Polyline.Points[0].Z, 6);
            Assert.Equal("4.1.1", bar.BarMark);
            Assert.Equal("00", bar.ShapeCode);
        }
    }

    [Fact]
    public void Row_17_bars_sit_one_layer_up_spread_over_the_width()
    {
        var row17 = Plan(Sheet()).Layout.ExtraBottomBars.Where(b => b.Layer == 2).OrderBy(b => b.TransverseY).ToList();

        // −557 + 20/2 + max(30, 20) + 18/2; edge bars touch the stirrup: 150 − 25 − 8 − 9.
        Assert.Equal(new[] { -108.0, 108.0 }, row17.Select(b => b.TransverseY).ToArray());
        Assert.All(row17, b => Assert.Equal(-508.0, b.Polyline.Points[0].Z, 6));
        Assert.All(row17, b => Assert.Equal("4.1.2", b.BarMark));
    }

    [Fact]
    public void Row_17_alone_takes_the_level_above_the_main_bars()
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("D17", "2f18");

        var bars = Plan(table).Layout.ExtraBottomBars;

        Assert.Equal(2, bars.Count);
        Assert.All(bars, b => Assert.Equal(-508.0, b.Polyline.Points[0].Z, 6));
    }

    [Fact]
    public void A_row_18_bar_larger_than_the_main_bars_rests_on_the_stirrup()
    {
        var table = KataRebarTestSheets.SingleSpan(cover: "");
        table.Set("B12", "2f18");
        table.Set("D18", "2f25");

        var plan = Plan(table);
        var row18 = plan.Layout.ExtraBottomBars.Where(b => b.Layer == 1).ToList();

        // Stirrup inner face 600 − 25 − 8 above the soffit; a Ø25 centre sits 12.5 higher.
        double seat = -600.0 + System.Math.Max(plan.Rules.BottomBarCentreDepth, 25.0 + 8.0 + 12.5);
        Assert.Equal(2, row18.Count);
        Assert.All(row18, b => Assert.Equal(seat, b.Polyline.Points[0].Z, 6));
        Assert.All(row18, b => Assert.True(b.Polyline.Points[0].Z - 12.5 >= -600.0 + 33.0 - 1e-6));
    }

    [Fact]
    public void Without_bottom_main_bars_row_18_rests_on_the_stirrup_and_row_17_stacks_on_it()
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("B12", "");
        table.Set("D18", "2f16");
        table.Set("D17", "2f18");

        var plan = Plan(table);
        var row18 = plan.Layout.ExtraBottomBars.Where(b => b.Layer == 1).ToList();
        var row17 = plan.Layout.ExtraBottomBars.Where(b => b.Layer == 2).ToList();

        double z18 = -600.0 + System.Math.Max(plan.Rules.BottomBarCentreDepth, 25.0 + 8.0 + 8.0);
        Assert.All(row18, b => Assert.Equal(z18, b.Polyline.Points[0].Z, 6));
        Assert.All(row17, b => Assert.Equal(z18 + 8.0 + 30.0 + 9.0, b.Polyline.Points[0].Z, 6));
    }

    [Fact]
    public void Row_17_alone_without_bottom_main_bars_rests_on_the_stirrup()
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("B12", "");
        table.Set("D17", "2f18");

        var plan = Plan(table);

        double seat = -600.0 + System.Math.Max(plan.Rules.BottomBarCentreDepth, 25.0 + 8.0 + 9.0);
        Assert.All(plan.Layout.ExtraBottomBars, b => Assert.Equal(seat, b.Polyline.Points[0].Z, 6));
    }

    [Fact]
    public void A_partly_unreadable_cell_draws_what_it_reads_and_names_the_rest()
    {
        var table = Sheet();
        table.Set("D18", "2f20+2x18");

        var plan = Plan(table);

        Assert.Equal(2, plan.Layout.ExtraBottomBars.Count(b => b.Layer == 1));
        Assert.Contains(plan.Warnings, w => w.StartsWith("D18 '2f20+2x18'") && w.Contains("'2x18'"));
    }

    [Fact]
    public void Bars_on_top_of_each_other_block()
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("B12", "1f20");
        table.Set("D18", "1f20");

        var plan = Plan(table);

        Assert.False(plan.CanGenerate);
        Assert.Contains(plan.Blocking, b => b.StartsWith("Thép gia cường 4.1.1"));
    }

    [Fact]
    public void A_mixed_row_17_cell_puts_the_larger_bars_at_the_edges()
    {
        var table = Sheet();
        table.Set("D18", "");
        table.Set("D17", "2f20+1f18");

        var row17 = Plan(table).Layout.ExtraBottomBars.OrderBy(b => b.TransverseY).ToList();

        Assert.Equal(new[] { -107.0, 0.0, 107.0 }, row17.Select(b => b.TransverseY).ToArray());
        Assert.Equal(new[] { 20.0, 18.0, 20.0 }, row17.Select(b => b.Diameter).ToArray());
    }

    [Fact]
    public void A_span_without_length_draws_no_additional_bottom_bars()
    {
        var table = Sheet();
        table.Set("D11", -100.0);

        var layout = KataRebarCalculator.Calculate(KataDamSheetParser.Parse(table));

        Assert.Empty(layout.ExtraBottomBars);
        Assert.Contains(layout.Warnings, w => w.Contains("nhịp dài -100"));
    }

    [Fact]
    public void An_unreadable_cell_is_reported()
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("D18", "2 thanh 20");

        var plan = Plan(table);

        Assert.Empty(plan.Layout.ExtraBottomBars);
        Assert.Contains(plan.Warnings, w => w.StartsWith("D18 '2 thanh 20'"));
    }

    [Fact]
    public void A_zero_draws_nothing_and_says_nothing()
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("D17", "0");

        var plan = Plan(table);

        Assert.Empty(plan.Layout.ExtraBottomBars);
        Assert.Empty(plan.Warnings);
    }

    [Fact]
    public void Row_17_blocks_where_it_meets_support_bars_over_the_same_stretch()
    {
        var plan = Plan(Shallow(0.2), ShallowBeam());

        Assert.False(plan.CanGenerate);
        Assert.Contains(plan.Blocking, b => b.StartsWith("D17"));
    }

    [Fact]
    public void Row_17_passes_under_support_bars_that_stop_before_it_starts()
    {
        // Row 14 reaches 0.05 × 6000 = 300 past the face (x 700), row 17 starts at 400 + 6000/7.
        var plan = Plan(Shallow(0.05), ShallowBeam());

        Assert.DoesNotContain(plan.Blocking, b => b.StartsWith("D17"));
        Assert.Equal(2, plan.Layout.ExtraBottomBars.Count);
    }

    [Fact]
    public void Sheet_text_of_rows_17_and_18_is_kept()
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("D17", " 2f18 ");
        table.Set("D18", "abc");

        var span = KataDamSheetParser.Parse(table).Spans[0];

        Assert.Equal("2f18", span.BottomExtraLayer2Text);
        Assert.Equal("abc", span.BottomExtraLayer1Text);
    }

    /// <summary>A 200 mm deep beam: row 14 over the supports at z −87, row 17 (Ø25) at z −109.5.</summary>
    private static KataCellTable Shallow(double h3)
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("B5", 200.0);
        table.Set("H3", h3);
        table.Set("C14", "2f18");
        table.Set("E14", "2f18");
        table.Set("D17", "2f25");
        return table;
    }

    private static KataMeasuredBeam ShallowBeam() => KataRebarTestSheets.MeasuredSingleSpan() with { HeightMm = 200.0 };
}
