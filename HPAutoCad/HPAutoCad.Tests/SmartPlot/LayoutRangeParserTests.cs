using HPAutoCad.Core.SmartPlot.Services;
using Xunit;

namespace HPAutoCad.Tests.SmartPlot;

public sealed class LayoutRangeParserTests
{
    [Fact]
    public void Parse_EmptyOrNull_ReturnsExpected()
    {
        // When maxCount is specified, empty/null means all pages
        var nullResult = LayoutRangeParser.Parse(null, 5);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, nullResult);

        var emptyResult = LayoutRangeParser.Parse("", 3);
        Assert.Equal(new[] { 1, 2, 3 }, emptyResult);

        var whitespaceResult = LayoutRangeParser.Parse("   ", 4);
        Assert.Equal(new[] { 1, 2, 3, 4 }, whitespaceResult);

        // When maxCount is unbounded (int.MaxValue), empty returns empty
        var unbounded = LayoutRangeParser.Parse(null);
        Assert.Empty(unbounded);
    }

    [Theory]
    [InlineData("All")]
    [InlineData("all")]
    [InlineData("ALL")]
    [InlineData("*")]
    public void Parse_AllKeyword_ReturnsSequential(string keyword)
    {
        var result = LayoutRangeParser.Parse(keyword, 4);
        Assert.Equal(new[] { 1, 2, 3, 4 }, result);
    }

    [Fact]
    public void Parse_CommaSeparated_ReturnsDistinctSorted()
    {
        var result = LayoutRangeParser.Parse("5, 1, 3, 1, 5", 10);
        Assert.Equal(new[] { 1, 3, 5 }, result);

        var semicolonResult = LayoutRangeParser.Parse("2;4;6", 10);
        Assert.Equal(new[] { 2, 4, 6 }, semicolonResult);
    }

    [Fact]
    public void Parse_DashRange_ReturnsConsecutive()
    {
        var result = LayoutRangeParser.Parse("1-5", 10);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, result);
    }

    [Fact]
    public void Parse_InvertedRange_ReturnsAscending()
    {
        var result = LayoutRangeParser.Parse("5-1", 10);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, result);
    }

    [Fact]
    public void Parse_ComplexRange()
    {
        var result = LayoutRangeParser.Parse("1-3, 5, 8-10", 12);
        Assert.Equal(new[] { 1, 2, 3, 5, 8, 9, 10 }, result);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData(",,,,")]
    [InlineData("1--5")]
    [InlineData("!@#")]
    [InlineData("-5")]
    [InlineData("5-")]
    [InlineData("--")]
    [InlineData("xyz-123")]
    public void Parse_MalformedInput_ImmuneToExceptions(string malformed)
    {
        // Must never throw exceptions
        var result = LayoutRangeParser.Parse(malformed, 10);
        Assert.NotNull(result);
    }

    [Fact]
    public void Parse_MixedValidAndMalformed_ExtractsValidPortion()
    {
        var result = LayoutRangeParser.Parse("abc, 2, xyz, 4-6, !!!", 10);
        Assert.Equal(new[] { 2, 4, 5, 6 }, result);
    }

    [Fact]
    public void Parse_ClampedToMaxCount()
    {
        var result = LayoutRangeParser.Parse("1-100", 5);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, result);

        var singleOutOfBounds = LayoutRangeParser.Parse("10", 5);
        Assert.Empty(singleOutOfBounds);
    }

    [Fact]
    public void Parse_HugeRange_ProtectedByHardCap()
    {
        var result = LayoutRangeParser.Parse("1-2000000000");
        Assert.Equal(10000, result.Count);
        Assert.Equal(1, result[0]);
        Assert.Equal(10000, result[result.Count - 1]);
    }

    [Fact]
    public void Parse_NegativeOrZeroMaxCount_ReturnsEmpty()
    {
        Assert.Empty(LayoutRangeParser.Parse("1-5", 0));
        Assert.Empty(LayoutRangeParser.Parse("1-5", -10));
    }
}
