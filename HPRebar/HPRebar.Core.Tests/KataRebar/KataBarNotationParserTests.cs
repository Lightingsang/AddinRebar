using System.Linq;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

public class KataBarNotationParserTests
{
    [Theory]
    [InlineData("2f18", 2, 18.0)]
    [InlineData("3f20", 3, 20.0)]
    [InlineData("6f25", 6, 25.0)]
    [InlineData("2d8", 2, 8.0)]
    [InlineData("4phi22", 4, 22.0)]
    [InlineData("2Ø16", 2, 16.0)]
    [InlineData("2%%c14", 2, 14.0)]
    [InlineData("2 f 18", 2, 18.0)]
    [InlineData("3 d 20", 3, 20.0)]
    [InlineData("2 phi 25", 2, 25.0)]
    [InlineData("f10", 1, 10.0)]
    [InlineData("d12", 1, 12.0)]
    [InlineData("0f12", 0, 12.0)]
    public void ParseSingleBar_ValidNotations_ReturnsExpectedItem(string text, int expectedCount, double expectedDia)
    {
        var item = KataBarNotationParser.ParseSingleBar(text);

        Assert.NotNull(item);
        Assert.Equal(expectedCount, item!.Count);
        Assert.Equal(expectedDia, item.Diameter);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("0")]
    [InlineData("-")]
    [InlineData("abc")]
    public void ParseSingleBar_InvalidOrEmpty_ReturnsNull(string? text)
    {
        var item = KataBarNotationParser.ParseSingleBar(text);
        Assert.Null(item);
    }

    [Fact]
    public void ParseBarList_CompoundNotationWithSemicolon_ReturnsAllItems()
    {
        var items = KataBarNotationParser.ParseBarList("2f20;2f16");

        Assert.Equal(2, items.Count);
        Assert.Equal(2, items[0].Count);
        Assert.Equal(20.0, items[0].Diameter);
        Assert.Equal(2, items[1].Count);
        Assert.Equal(16.0, items[1].Diameter);
    }

    [Fact]
    public void ParseBarList_CompoundNotationWithPlusAndZero_FiltersOutZeros()
    {
        var items = KataBarNotationParser.ParseBarList("6f20;0");

        Assert.Single(items);
        Assert.Equal(6, items[0].Count);
        Assert.Equal(20.0, items[0].Diameter);
    }

    [Fact]
    public void ParseBarList_CompoundWithPlusSign_ReturnsBoth()
    {
        var items = KataBarNotationParser.ParseBarList("2f20+1f18");

        Assert.Equal(2, items.Count);
        Assert.Equal(2, items[0].Count);
        Assert.Equal(20.0, items[0].Diameter);
        Assert.Equal(1, items[1].Count);
        Assert.Equal(18.0, items[1].Diameter);
    }

    [Theory]
    [InlineData("-50;5f20", -50.0, 5, 20.0)]
    [InlineData("100;5f25", 100.0, 5, 25.0)]
    [InlineData("-100;5f20", -100.0, 5, 20.0)]
    public void ParseOffsetAndBars_BothOffsetAndBars_ReturnsBoth(
        string text, double expectedOffset, int expectedCount, double expectedDia)
    {
        var (offset, bars) = KataBarNotationParser.ParseOffsetAndBars(text);

        Assert.Equal(expectedOffset, offset);
        Assert.Single(bars);
        Assert.Equal(expectedCount, bars[0].Count);
        Assert.Equal(expectedDia, bars[0].Diameter);
    }

    [Fact]
    public void ParseOffsetAndBars_OnlyOffset_ReturnsEmptyBars()
    {
        var (offset, bars) = KataBarNotationParser.ParseOffsetAndBars("-50");

        Assert.Equal(-50.0, offset);
        Assert.Empty(bars);
    }

    [Fact]
    public void ParseOffsetAndBars_OnlyBars_ReturnsZeroOffset()
    {
        var (offset, bars) = KataBarNotationParser.ParseOffsetAndBars("5f20");

        Assert.Equal(0.0, offset);
        Assert.Single(bars);
        Assert.Equal(5, bars[0].Count);
        Assert.Equal(20.0, bars[0].Diameter);
    }

    [Theory]
    [InlineData("a150", 150.0, 150.0, null)]
    [InlineData("@150", 150.0, 150.0, null)]
    [InlineData("150", 150.0, 150.0, null)]
    [InlineData("a100/200", 100.0, 200.0, null)]
    [InlineData("100/200", 100.0, 200.0, null)]
    [InlineData("a100/200/50", 100.0, 200.0, 50.0)]
    [InlineData("100/200/50", 100.0, 200.0, 50.0)]
    public void ParseStirrupSpacing_VariousNotations_ReturnsExpectedValues(
        string text, double expStart, double expMid, double? expEnd)
    {
        var (s1, s2, s3) = KataBarNotationParser.ParseStirrupSpacing(text);

        Assert.Equal(expStart, s1);
        Assert.Equal(expMid, s2);
        Assert.Equal(expEnd, s3);
    }

    [Theory]
    [InlineData("50/25", 50.0, 25.0)]
    [InlineData("30/20", 30.0, 20.0)]
    [InlineData("30", 30.0, 0.0)]
    [InlineData("/25", 0.0, 25.0)]
    [InlineData(null, 0.0, 0.0)]
    public void ParseCover_VariousStrings_ReturnsGivenNumbersAndZeroForMissing(string? text, double expMain, double expStirrup)
    {
        var (cMain, cStirrup) = KataBarNotationParser.ParseCover(text);

        Assert.Equal(expMain, cMain);
        Assert.Equal(expStirrup, cStirrup);
    }

    [Theory]
    [InlineData("400", 400.0, 0.0)]
    [InlineData("300x500", 300.0, 500.0)]
    [InlineData("300*500", 300.0, 500.0)]
    [InlineData("0", 0.0, 0.0)]
    public void ParseSupportDimension_VariousFormats_ReturnsExpectedDimensions(string text, double expW, double expH)
    {
        var (w, h) = KataBarNotationParser.ParseSupportDimension(text);

        Assert.Equal(expW, w);
        Assert.Equal(expH, h);
    }

    [Theory]
    [InlineData("350;0", 350.0, 0.0)]
    [InlineData("250;790", 250.0, 790.0)]
    [InlineData("300,50", 300.0, 50.0)]
    public void ParsePair_VariousSeparators_ReturnsExpectedValues(string text, double expFirst, double expSecond)
    {
        var (first, second) = KataBarNotationParser.ParsePair(text);

        Assert.Equal(expFirst, first);
        Assert.Equal(expSecond, second);
    }
}
