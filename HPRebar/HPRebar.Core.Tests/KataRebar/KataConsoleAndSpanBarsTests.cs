using System;
using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// Consoles and spans with bars of their own (rows 19 / 21) next to B01: the B01 sheet changed one cell at a time, and
/// the simple test sheets (300×600, 3f20 / 4f20, J9 43/25) shaped into consoles.
/// </summary>
public sealed class KataConsoleAndSpanBarsTests
{
    private static KataCellTable B01(params (string Address, object Value)[] cells) => With(KataB01DrawingTests.Sheet(), cells);

    private static KataCellTable Two(params (string Address, object Value)[] cells) => With(KataRebarTestSheets.TwoSpans(), cells);

    private static KataCellTable One(params (string Address, object Value)[] cells) => With(KataRebarTestSheets.SingleSpan(), cells);

    private static KataCellTable With(KataCellTable table, (string Address, object Value)[] cells)
    {
        foreach (var (address, value) in cells) table.Set(address, value);
        return table;
    }

    private static KataRebarLayoutResult Layout(KataCellTable table) => KataRebarCalculator.Calculate(KataDamSheetParser.Parse(table));

    private static KataRebarPlan Plan(KataCellTable table) => KataRebarPlanner.Plan(KataDamSheetParser.Parse(table), null);

    private static (double Min, double Max) Extent(KataRebarCurve bar) =>
        (bar.Polyline.Points.Min(p => p.X), bar.Polyline.Points.Max(p => p.X));

    private static void Close(double expected, double actual, double tolerance = 1.0) =>
        Assert.True(Math.Abs(expected - actual) <= tolerance, $"expected {expected:0.#}, got {actual:0.#}");

    [Fact]
    public void B01s_narrow_bottom_bars_lap_30_d_of_their_own_past_their_faces_of_K_and_M()
    {
        var narrow = Layout(B01()).MainBottomBars.Where(b => b.Diameter == 20.0 && Extent(b).Min < 26000).ToList();

        Assert.Equal(3, narrow.Count);
        Assert.All(narrow, b => { Close(24400, Extent(b).Min); Close(31800, Extent(b).Max); });
    }

    [Fact]
    public void B01s_console_tip_leg_stops_one_bar_clear_above_the_console_bottom_bar()
    {
        var layout = Layout(B01());

        double foot = layout.MainTopBars.Where(b => Extent(b).Min > 30000).Max(b => b.Polyline.Points.Min(p => p.Z));
        double bottom = layout.MainBottomBars.Where(b => Extent(b).Min > 31000).Max(b => b.Polyline.Points[b.Polyline.Points.Count - 1].Z);
        Close(40, foot - bottom);
    }

    [Fact]
    public void B01s_console_extras_lap_30_d_of_the_span_bars_into_L()
    {
        var extras = Layout(B01()).ExtraTopBars.Where(b => Extent(b).Max > 33000).ToList();

        Assert.NotEmpty(extras);
        Assert.All(extras, b => Close(30960, Extent(b).Min, 25.0));
    }

    [Fact]
    public void A_narrower_console_is_cut_once_at_its_support()
    {
        var bottom = Layout(B01(("N20", 200.0))).MainBottomBars.Where(b => Extent(b).Max > 31000).ToList();

        Assert.Equal(6, bottom.Count);
    }

    [Fact]
    public void A_console_of_its_own_bottom_bars_is_cut_once_and_gets_them()
    {
        var bottom = Layout(B01(("N21", "0;2f16"))).MainBottomBars.Where(b => Extent(b).Max > 31000).ToList();

        Assert.Equal(5, bottom.Count);
        Assert.Equal(2, bottom.Count(b => b.Diameter == 16.0 && Extent(b).Min > 31000));
    }

    [Fact]
    public void A_support_changing_both_the_top_and_the_top_bars_is_refused()
    {
        Assert.Contains(Plan(B01(("N19", "-200;3f16"))).Blocking, b => b.Contains("thép chủ trên (hàng 19)"));
    }

    [Fact]
    public void Span_bars_larger_than_B12_size_the_laps_and_the_console_leg()
    {
        var bottom = Layout(B01(("L19", "3f28"), ("L21", "3f28"))).MainBottomBars;

        var narrow = bottom.Where(b => b.Diameter == 28.0 && Extent(b).Min < 26000).ToList();
        Assert.Equal(3, narrow.Count);
        Assert.All(narrow, b => { Close(25000 - 840, Extent(b).Min); Close(31200 + 840, Extent(b).Max); });
        Assert.All(bottom.Where(b => Extent(b).Min > 31000), b => Close(280, b.StartHookLength));
    }

    [Fact]
    public void A_console_too_shallow_for_a_10d_leg_keeps_it_under_the_top_bars()
    {
        var layout = Layout(Two(("F11", 1500.0), ("G11", 0.0), ("F21", 320.0)));

        var console = layout.MainBottomBars.Where(b => Extent(b).Min > 6000).ToList();
        Assert.NotEmpty(console);
        Assert.All(console, b => Assert.True(b.Polyline.Points.Max(p => p.Z) < -43.0 - 20.0, "leg above the top bars"));
        Assert.Contains(layout.Warnings, w => w.Contains("chân neo chỉ còn"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_one_span_console_gets_its_own_bottom_bars_from_its_support_to_the_tip(bool right)
    {
        var table = right
            ? One(("D11", 1500.0), ("E11", 0.0))
            : One(("C11", 0.0), ("D11", 1500.0));

        var bottom = Layout(table).MainBottomBars;

        Assert.Equal(4, bottom.Count);
        Assert.All(bottom, b => Assert.True(Extent(b).Max - Extent(b).Min > 1500.0, "reaches the tip"));
        Assert.All(bottom, b => Assert.True(right ? b.StartHookLength > 0.0 : b.EndHookLength > 0.0, "anchored in the support"));
    }

    [Fact]
    public void Row_19_restating_B11_neither_cuts_the_bars_nor_stops_a_join_over_no_width()
    {
        Assert.Equal(3, Layout(Two(("F19", "0;3F20"))).MainTopBars.Count);

        var joined = Plan(Two(("G11", 0.0), ("H10", "Nhịp"), ("H11", 3000.0), ("I10", "Cột"), ("I11", 400.0), ("H19", "0;3F20")));
        Assert.Empty(joined.Blocking);
    }

    [Fact]
    public void A_refused_sheet_is_not_laid_out()
    {
        var plan = Plan(B01(("J19", "0;6f28")));

        Assert.NotEmpty(plan.Blocking);
        Assert.Empty(plan.Layout.MainTopBars);
    }

    [Fact]
    public void Span_bars_too_close_are_reported_against_their_own_row_and_B01_has_none()
    {
        Assert.Contains(Layout(Two(("F19", "0;8f20"))).Warnings, w => w.StartsWith("F19:"));
        Assert.DoesNotContain(Layout(B01()).Warnings, w => w.Contains("thép chủ") && w.Contains("chỉ cách nhau"));
    }

    [Fact]
    public void Span_bars_with_B11_empty_are_reported()
    {
        Assert.Contains(Plan(B01(("B11", ""))).Skipped, s => s.StartsWith("L19"));
    }

    [Fact]
    public void Consoles_at_both_ends_over_two_columns_are_drawn()
    {
        var plan = Plan(Two(("C11", 0.0), ("D11", 1500.0), ("F11", 6000.0), ("H10", "Nhịp"), ("H11", 1500.0), ("I10", "Cột"), ("I11", 0.0)));

        Assert.DoesNotContain(plan.Blocking, b => b.Contains("không có gối"));
        Assert.Equal(3 * 4, plan.Layout.MainBottomBars.Count);
    }
}
