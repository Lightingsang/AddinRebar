using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// What a span takes from the span before it (rows 20 and 21 left empty) and how a support of no width joins its
/// two spans, as Kata draws B01.
/// </summary>
public sealed class KataSpanCarryOverTests
{
    /// <summary>Four spans C | D 6000 | E | F 4500 | G | H 3000 | I | J 2500 | K, columns of 400, h 800.</summary>
    private static KataCellTable FourSpans(params (string Address, object Value)[] cells)
    {
        var table = KataRebarTestSheets.TwoSpans();
        table.Set("B5", 800.0);
        foreach (var (col, caption, length) in new[] { ("H", "Nhịp", 3000.0), ("I", "Cột", 400.0), ("J", "Nhịp", 2500.0), ("K", "Cột", 400.0) })
        {
            table.Set(col + "10", caption);
            table.Set(col + "11", length);
        }

        foreach (var (address, value) in cells) table.Set(address, value);
        return table;
    }

    private static KataBeamRebarSpec Parse(params (string Address, object Value)[] cells) => KataDamSheetParser.Parse(FourSpans(cells));

    [Fact]
    public void An_empty_row_21_keeps_the_soffit_step_of_the_span_before_and_0_resets_it()
    {
        var spec = Parse(("F21", 200.0), ("J21", 0.0));

        Assert.Equal(new[] { 800.0, 600.0, 600.0, 800.0 }, spec.Spans.Select(s => s.Depth));
    }

    [Fact]
    public void A_row_21_naming_bars_but_no_step_keeps_the_step_of_the_span_before()
    {
        var spec = Parse(("F21", 200.0), ("H21", "3f20"), ("J21", "0;3f20"));

        Assert.Equal(600.0, spec.Spans[2].Depth);
        Assert.Equal(3, spec.Spans[2].SoffitDropBars[0].Count);
        Assert.Equal(800.0, spec.Spans[3].Depth);
    }

    [Fact]
    public void An_empty_row_20_keeps_the_side_bars_of_the_span_before_and_a_dash_resets_them()
    {
        var spec = Parse(("F20", "1f12"), ("J20", "-"));

        Assert.Equal(1, spec.Spans[2].SideBars.Single().Count);
        Assert.True(spec.Spans[3].SideBars.Single().IsEmpty);
        Assert.Empty(spec.Spans[0].SideBars);
    }

    [Fact]
    public void A_number_first_in_row_20_is_the_span_width_carried_on_and_the_side_bars_carry_on_too()
    {
        var spec = Parse(("F20", "1f12"), ("H20", 300.0), ("J20", "abc"));

        Assert.Equal(new[] { spec.Width, spec.Width, 300.0, 300.0 }, spec.Spans.Select((s, i) => spec.WidthOf(i)));
        Assert.Equal(new[] { 0.0, 0.0, 300.0, 300.0 }, spec.Spans.Select(s => s.Width));
        Assert.Equal(1, spec.Spans[2].SideBars.Single().Count);
        Assert.Equal(1, spec.Spans[3].SideBars.Single().Count);
        var skipped = KataScopeFilter.Apply(spec).Skipped;
        Assert.DoesNotContain(skipped, s => s.StartsWith("H20"));
        Assert.Contains(skipped, s => s.StartsWith("J20 'abc'") && s.Contains("lấy cốt giá của nhịp trước"));
    }

    [Fact]
    public void Row_20_width_and_side_bars_together_and_row_19_carries_on_until_reset()
    {
        var spec = Parse(("F20", "300;2f12"), ("F19", -50.0), ("J19", 0.0));

        Assert.Equal(300.0, spec.Spans[1].Width);
        Assert.Equal(2, spec.Spans[1].SideBars.Single().Count);
        Assert.Equal(new[] { 0.0, -50.0, -50.0, 0.0 }, spec.Spans.Select(s => s.TopDrop));
        Assert.Equal(new[] { 800.0, 750.0, 750.0, 800.0 }, spec.Spans.Select((s, i) => spec.HeightOf(i)));
    }

    [Fact]
    public void A_support_of_no_width_between_spans_of_different_tops_gives_one_span_with_a_top_step()
    {
        var spec = Parse(("G11", 0.0), ("F19", -50.0), ("H19", 0.0));

        Assert.Equal(7500.0, spec.Spans[1].Length);
        Assert.Equal(new[] { new KataTopStep(4500.0, 0.0) }, spec.Spans[1].TopSteps);
        Assert.Equal(-50.0, spec.TopAt(1, 100.0));
        Assert.Equal(0.0, spec.TopAt(1, 5000.0));
    }

    [Fact]
    public void A_support_of_no_width_between_spans_of_different_soffits_stays_and_blocks()
    {
        var spec = Parse(("G11", 0.0), ("H21", 100.0));

        Assert.Equal(4, spec.Spans.Count);
        Assert.Contains(KataScopeFilter.Apply(spec).Blocking, b => b.Contains("gối giữa"));
    }

    [Fact]
    public void A_dash_in_the_first_span_keeps_G4_G5_and_a_dash_in_row_21_resets_the_step()
    {
        var spec = Parse(("G4", 12.0), ("G5", 2.0), ("D20", "-"), ("F21", 200.0), ("H21", "-"));

        Assert.Empty(spec.Spans[0].SideBars);
        Assert.Equal(new[] { 800.0, 600.0, 800.0, 800.0 }, spec.Spans.Select(s => s.Depth));
    }

    [Theory]
    [InlineData("C400")]
    [InlineData(-300.0)]
    public void A_row_11_that_is_not_a_width_blocks_instead_of_joining_spans_or_making_a_console(object cell)
    {
        foreach (string address in new[] { "G11", "K11" })
        {
            var spec = Parse((address, cell));

            Assert.Equal(4, spec.Spans.Count);
            Assert.Contains(KataScopeFilter.Apply(spec).Blocking, b => b.StartsWith(address + ": không đọc được bề rộng gối"));
        }
    }

    [Fact]
    public void Two_supports_of_no_width_in_a_row_join_three_spans_and_the_right_span_cells_are_reported()
    {
        var spec = Parse(("G11", 0.0), ("I11", 0.0), ("H22", "a100/200"), ("J17", "2f16"));

        Assert.Equal(new[] { 6000.0, 10000.0 }, spec.Spans.Select(s => s.Length));
        var skipped = KataScopeFilter.Apply(spec).Skipped;
        Assert.Contains(skipped, s => s.StartsWith("H22 'a100/200'") && s.Contains("nhịp F và H gộp làm một"));
        Assert.Contains(skipped, s => s.StartsWith("J17 '2f16'") && s.Contains("nhịp F và J gộp làm một"));
    }

    [Fact]
    public void A_console_at_the_start_is_planned_against_a_run_that_begins_with_a_span()
    {
        var spec = Parse(("C11", 0.0));
        var measured = new KataMeasuredBeam(300.0, 800.0, new[] { 6000.0, 400.0, 4500.0, 400.0, 3000.0, 400.0, 2500.0, 400.0 }
            .Select((l, i) => new KataMeasuredSegment(i % 2 == 0 ? KataMeasuredSupportKind.None : KataMeasuredSupportKind.Column, l)).ToList(), 1);

        var plan = KataRebarPlanner.Plan(spec, measured);

        Assert.Empty(plan.Blocking);
        Assert.Contains(plan.Warnings, w => w.StartsWith("C11 = 0: đầu console"));
    }

    [Fact]
    public void A_support_of_no_width_joins_its_spans_and_its_own_cells_are_reported()
    {
        var spec = Parse(("G11", 0.0), ("G17", "2f20"), ("H18", "2f16"));

        Assert.Equal(new[] { 6000.0, 7500.0, 2500.0 }, spec.Spans.Select(s => s.Length));
        Assert.Equal(new[] { 400.0, 400.0, 400.0, 400.0 }, spec.Supports.Select(s => s.ColumnWidth));
        Assert.Equal(new[] { 0, 1, 2 }, spec.Spans.Select(s => s.SpanIndex));
        var skipped = KataScopeFilter.Apply(spec).Skipped;
        Assert.Contains(skipped, s => s.StartsWith("G17 '2f20'") && s.Contains("nhịp F và H gộp làm một"));
        Assert.Contains(skipped, s => s.StartsWith("H18 '2f16'"));
    }

    [Fact]
    public void Merged_spans_are_compared_with_the_one_span_Revit_measures_and_a_console_end_with_nothing()
    {
        var spec = Parse(("G11", 0.0), ("K11", 0.0));
        var measured = new KataMeasuredBeam(300.0, 800.0, new[]
        {
            new KataMeasuredSegment(KataMeasuredSupportKind.Column, 400.0),
            new KataMeasuredSegment(KataMeasuredSupportKind.None, 6000.0),
            new KataMeasuredSegment(KataMeasuredSupportKind.Column, 400.0),
            new KataMeasuredSegment(KataMeasuredSupportKind.None, 7500.0),
            new KataMeasuredSegment(KataMeasuredSupportKind.Column, 400.0),
            new KataMeasuredSegment(KataMeasuredSupportKind.None, 2500.0)
        }, 1);

        var plan = KataRebarPlanner.Plan(spec, measured);

        Assert.Empty(plan.Blocking);
        Assert.Contains(plan.Warnings, w => w.StartsWith("K11 = 0: đầu console"));
        Assert.Equal(0.0, plan.Spec.Supports.Last().ColumnWidth);
    }
}
