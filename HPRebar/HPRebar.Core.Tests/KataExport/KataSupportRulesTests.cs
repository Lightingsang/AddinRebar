using HPRebar.Core.KataExport.Calculators;
using HPRebar.Core.KataExport.Models;
using Xunit;

namespace HPRebar.Core.Tests.KataExport;

public sealed class KataSupportRulesTests
{
    [Theory]
    [InlineData(60.0, true)]
    [InlineData(100.0, true)]
    [InlineData(100.0000001, true)]
    [InlineData(101.0, false)]
    [InlineData(800.0, false)]
    public void FoundationSlabsUpTo100MmAreLeanConcrete(double thicknessMm, bool lean)
    {
        Assert.Equal(lean, KataSupportRules.IsLeanConcrete(thicknessMm));
    }

    [Theory]
    [InlineData(1700.0, false)]
    [InlineData(6000.0, false)]
    [InlineData(6001.0, true)]
    [InlineData(20000.0, true)]
    public void FoundationsLongerThan6000MmAlongTheRunAreStripsOrRafts(double lengthMm, bool strip)
    {
        Assert.Equal(strip, KataSupportRules.IsStripOrRaft(new Interval1D(0, lengthMm)));
    }

    [Fact]
    public void ColumnAboveAColumnOrFootingSupportIsNotWarnedButOneOnASpanIs()
    {
        var supports = new[] { new Interval1D(-200, 200), new Interval1D(5250, 6750) };
        var standing = new[]
        {
            new Interval1D(-150, 150),   // column of the storey above, on a column support
            new Interval1D(5850, 6150),  // column on a footing support
            new Interval1D(2900, 3100)   // transfer column in the middle of the span
        };

        Assert.Equal(1, KataSupportRules.CountStandingOffSupports(standing, supports));
    }

    [Fact]
    public void TwoColumnFootingIsCountedOnceAndSingleColumnFootingsAreNot()
    {
        var supports = new[] { new Interval1D(0, 5000), new Interval1D(8000, 9500) };
        var columnsAbove = new[] { new Interval1D(500, 900), new Interval1D(4100, 4500), new Interval1D(8550, 8950) };

        Assert.Equal(1, KataSupportRules.CountSupportsWithSeveralColumnsAbove(supports, columnsAbove));
    }
}
