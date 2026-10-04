using System;
using HPRebar.Core.ColumnRebar;
using HPRebar.Core.ColumnRebar.Models;
using HPRebar.Core.Shared;
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
    public void ASpacingThatWouldNeedMoreTiesThanARebarSetHoldsIsRejected()
    {
        // Revit refuses a set of more than 1002 bar positions, so the limit is caught here rather than
        // surfacing as a failure part-way through building the model.
        var tooTight = 3000d / RevitRebarLimits.MaxBarPositions;

        Assert.Throws<ArgumentOutOfRangeException>(
            () => StirrupDistributionCalculator.Compute(3000, new StirrupSpec { TypeDis = 0, S = tooTight }));
    }

    [Fact]
    public void ASpacingJustInsideTheLimitIsStillAccepted()
    {
        // Sized to land just under the cap. The exact count depends on floating point, so the invariant
        // worth asserting is that it is accepted and stays within what a rebar set can hold.
        var run = StirrupDistributionCalculator.Compute(
            3000, new StirrupSpec { TypeDis = 0, S = 3000d / (RevitRebarLimits.MaxBarPositions - 2) });

        var group = Assert.Single(run);

        Assert.InRange(group.Count, 2, RevitRebarLimits.MaxBarPositions);
    }

    [Fact]
    public void TheLimitAlsoAppliesToEachZoneOfAZonedLayout()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => StirrupDistributionCalculator.Compute(
                3000, new StirrupSpec { TypeDis = 1, S1 = 0.5, S2 = 200 }));
    }

    [Fact]
    public void ZeroOrNegativeSpacingIsRejectedRatherThanLoopingForever()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => StirrupDistributionCalculator.Compute(3000, new StirrupSpec { TypeDis = 0, S = 0 }));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => StirrupDistributionCalculator.Compute(3000, new StirrupSpec { TypeDis = 1, S1 = 100, S2 = -5 }));
    }

    [Fact]
    public void WhenRunLengthIsShorterThanSpacingSingleCentredTieIsProduced()
    {
        // 100 mm run with 150 mm spacing: count is (int)(100/150) + 1 = 1 tie, offset is (100 - 0)/2 = 50 mm
        var runs = StirrupDistributionCalculator.Compute(100, new StirrupSpec { TypeDis = 0, S = 150 });

        var run = Assert.Single(runs);
        Assert.Equal(1, run.Count);
        Assert.Equal(150d, run.Spacing, Precision);
        Assert.Equal(50d, run.StartOffset, Precision);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(99)]
    public void UnrecognisedZoneTypeFallsBackToZeroZones(int invalidType)
    {
        var (l1, l2) = StirrupDistributionCalculator.ComputeZones(3000, invalidType);

        Assert.Equal(0d, l1, Precision);
        Assert.Equal(0d, l2, Precision);
    }

    [Fact]
    public void ASpacingSoSmallTheCountOverflowsAnIntIsStillRejected()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(
            () => StirrupDistributionCalculator.Compute(3000, new StirrupSpec { TypeDis = 0, S = 1e-6 }));

        Assert.Equal("spacing", error.ParamName);
    }
}
