using System;
using HPRebar.Core.ColumnRebar;
using HPRebar.Core.ColumnRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.ColumnRebar;

public sealed class StirrupDistributionCalculatorTests
{
    private const int Precision = 6;

    [Fact]
    public void TiesNormallyStopUnderTheBeam()
    {
        var section = TestSections.Rectangle() with { Hc = 3000, Hb = 500, Zb = 100 };

        Assert.Equal(2400d, StirrupDistributionCalculator.ComputeRunLength(section, tiesUp: false), Precision);
    }

    [Fact]
    public void TiesUpCarriesTheRunBackThroughTheBeamDepth()
    {
        var section = TestSections.Rectangle() with { Hc = 3000, Hb = 500, Zb = 100 };

        Assert.Equal(3000d, StirrupDistributionCalculator.ComputeRunLength(section, tiesUp: true), Precision);
    }

    [Fact]
    public void TheEvenLayoutIsOneCentredGroup()
    {
        var runs = StirrupDistributionCalculator.Compute(3000, new StirrupSpec { TypeDis = 0, S = 150 });

        var run = Assert.Single(runs);
        Assert.Equal(21, run.Count);
        Assert.Equal(150d, run.Spacing, Precision);
        Assert.Equal(0d, run.StartOffset, Precision);
    }

    [Fact]
    public void TheEvenLayoutCentresTheLeftoverSlack()
    {
        var runs = StirrupDistributionCalculator.Compute(3050, new StirrupSpec { TypeDis = 0, S = 150 });

        var run = Assert.Single(runs);
        Assert.Equal(21, run.Count);
        Assert.Equal(25d, run.StartOffset, Precision);
    }

    [Theory]
    [InlineData(1, 750d, 1500d)]
    [InlineData(2, 500d, 2000d)]
    [InlineData(3, 375d, 2250d)]
    public void EachZonedTypeSplitsTheRunIntoItsOwnDenseAndSparseLengths(int typeDis, double expectedL1, double expectedL2)
    {
        var (l1, l2) = StirrupDistributionCalculator.ComputeZones(3000, typeDis);

        Assert.Equal(expectedL1, l1, Precision);
        Assert.Equal(expectedL2, l2, Precision);
    }

    [Fact]
    public void TheEvenTypeHasNoZones()
    {
        var (l1, l2) = StirrupDistributionCalculator.ComputeZones(3000, 0);

        Assert.Equal(0d, l1, Precision);
        Assert.Equal(0d, l2, Precision);
    }

    [Fact]
    public void AZonedLayoutIsDenseThenSparseThenDense()
    {
        var runs = StirrupDistributionCalculator.Compute(3000, new StirrupSpec { TypeDis = 1, S1 = 100, S2 = 200 });

        Assert.Equal(3, runs.Count);

        Assert.Equal(8, runs[0].Count);
        Assert.Equal(100d, runs[0].Spacing, Precision);
        Assert.Equal(25d, runs[0].StartOffset, Precision);

        Assert.Equal(8, runs[1].Count);
        Assert.Equal(200d, runs[1].Spacing, Precision);
        Assert.Equal(800d, runs[1].StartOffset, Precision);

        Assert.Equal(8, runs[2].Count);
        Assert.Equal(100d, runs[2].Spacing, Precision);
        Assert.Equal(2275d, runs[2].StartOffset, Precision);
    }

    [Fact]
    public void TheTopDenseZoneRepeatsTheBottomOne()
    {
        var runs = StirrupDistributionCalculator.Compute(3000, new StirrupSpec { TypeDis = 3, S1 = 100, S2 = 250 });

        Assert.Equal(runs[0].Count, runs[2].Count);
        Assert.Equal(runs[0].Spacing, runs[2].Spacing, Precision);
    }

    [Fact]
    public void ZeroOrNegativeSpacingIsRejectedRatherThanLoopingForever()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => StirrupDistributionCalculator.Compute(3000, new StirrupSpec { TypeDis = 0, S = 0 }));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => StirrupDistributionCalculator.Compute(3000, new StirrupSpec { TypeDis = 1, S1 = 100, S2 = -5 }));
    }
}
