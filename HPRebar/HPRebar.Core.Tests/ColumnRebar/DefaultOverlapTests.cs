using HPRebar.Core.ColumnRebar;
using Xunit;

namespace HPRebar.Core.Tests.ColumnRebar;

public sealed class DefaultOverlapTests
{
    private const double Precision = 6;

    [Fact]
    public void AtFiftyPercentSplittingEvenBarsGetTheSingleLap() =>
        Assert.Equal(700d, DefaultOverlap.LapLength(2, 20, 50, 35), (int)Precision);

    [Fact]
    public void AtFiftyPercentSplittingOddBarsGetTheDoubleLapSoNeighboursDoNotStopTogether() =>
        Assert.Equal(1400d, DefaultOverlap.LapLength(1, 20, 50, 35), (int)Precision);

    [Fact]
    public void AtAnyOtherSplitPercentageEveryBarGetsTheSameLap()
    {
        Assert.Equal(700d, DefaultOverlap.LapLength(1, 20, 100, 35), (int)Precision);
        Assert.Equal(700d, DefaultOverlap.LapLength(2, 20, 100, 35), (int)Precision);
    }

    [Fact]
    public void TheLapScalesWithBothTheFactorAndTheBarDiameter() =>
        Assert.Equal(500d, DefaultOverlap.LapLength(2, 25, 50, 20), (int)Precision);
}
