using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

public sealed class KataScopeFilterTests
{
    private static KataScopeResult Apply(params (string Address, object Value)[] cells)
    {
        var table = KataRebarTestSheets.SingleSpan();
        foreach (var (address, value) in cells) table.Set(address, value);
        return KataScopeFilter.Apply(KataDamSheetParser.Parse(table));
    }

    [Fact]
    public void The_plain_single_span_sheet_keeps_everything()
    {
        var result = Apply();

        Assert.Empty(result.Skipped);
        Assert.Empty(result.Blocking);
        Assert.Equal(3, result.Filtered.TopContinuous.Count);
    }

    [Fact]
    public void Support_top_bars_and_span_bottom_bars_are_kept()
    {
        var result = Apply(("C14", "2f18"), ("D17", "2f18"), ("D18", "2f16"), ("E13", "3f16"));

        Assert.Empty(result.Skipped);
        Assert.Equal(2, result.Filtered.Supports[0].TopExtraSides[1].Right[0].Count);
        Assert.Equal(3, result.Filtered.Supports[1].TopExtraSides[0].Left[0].Count);
        Assert.Equal(18.0, result.Filtered.Spans[0].BottomExtraLayer2[0].Diameter);
        Assert.Equal(16.0, result.Filtered.Spans[0].BottomExtraLayer1[0].Diameter);
    }

    [Fact]
    public void Only_the_first_main_bar_group_is_drawn()
    {
        var result = Apply(("B11", "2f20;2f16"));

        Assert.Equal(2, result.Filtered.TopContinuous.Count);
        Assert.Equal(20.0, result.Filtered.TopContinuous.Diameter);
        Assert.Contains(result.Skipped, s => s.StartsWith("B11 '2f16'"));
    }

    [Fact]
    public void Row_20_bars_are_kept_in_scope()
    {
        var result = Apply(("D20", "2f12"));
        Assert.Empty(result.Skipped);
        Assert.Single(result.Filtered.Spans[0].SideBars);
        Assert.Equal(12.0, result.Filtered.Spans[0].SideBars[0].Diameter);
    }

    [Fact]
    public void Steps_are_skipped_and_span_stirrup_overrides_are_kept()
    {
        var result = Apply(("D19", "100;5f25"), ("D21", "-100"), ("D22", "a100/200"));

        Assert.Contains(result.Skipped, s => s.StartsWith("D19 '100;5f25'"));
        Assert.Contains(result.Skipped, s => s.StartsWith("D21 '-100'"));
        Assert.DoesNotContain(result.Skipped, s => s.StartsWith("D22"));
        Assert.NotNull(result.Filtered.Spans[0].StirrupOverride);
        Assert.Equal(100.0, result.Filtered.Spans[0].StirrupOverride!.SupportSpacing);
        Assert.Equal(200.0, result.Filtered.Spans[0].StirrupOverride!.MidspanSpacing);
    }

    [Fact]
    public void Inner_stirrups_are_kept_in_scope()
    {
        var result = Apply(("C25", "Đai U"), ("D25", "3-4"));

        Assert.Empty(result.Skipped);
        Assert.Contains(result.Filtered.Spans[0].InnerStirrups, b => b.ShapeType == KataStirrupShapeType.CapStirrup);
    }

    [Fact]
    public void Two_spans_are_supported()
    {
        var result = Apply(("F10", "Nhịp"), ("F11", 3000.0), ("G10", "Cột "), ("G11", 400.0));

        Assert.Empty(result.Blocking);
        Assert.Equal(2, result.Filtered.Spans.Count);
        Assert.Equal(3, result.Filtered.Supports.Count);
    }

    [Fact]
    public void A_cantilever_end_blocks()
    {
        var result = Apply(("E11", 0.0));

        Assert.NotEmpty(result.Blocking);
    }
}
