using System;
using HPRebar.Core.ColumnRebar;
using HPRebar.Core.ColumnRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.ColumnRebar;

public sealed class BarLayoutCalculatorTests
{
    private const int Precision = 6;

    [Fact]
    public void A3By4GridProducesTenBars()
    {
        var bars = BarLayoutCalculator.Compute(TestSections.Rectangle(), TestSections.Grid());

        Assert.Equal(10, bars.Count);

        for (var i = 0; i < bars.Count; i++)
        {
            Assert.Equal(i + 1, bars[i].BarNumber);
        }
    }

    [Fact]
    public void TheFirstBarSitsInsideCoverStirrupAndHalfItsOwnDiameter()
    {
        var section = TestSections.Rectangle(west: 100, south: 200);

        var first = BarLayoutCalculator.Compute(section, TestSections.Grid())[0];

        Assert.Equal(143d, first.X0, Precision);
        Assert.Equal(243d, first.Y0, Precision);
    }

    [Fact]
    public void SouthFaceBarsAreSpacedByTheClearWidthDividedByTheGaps()
    {
        var bars = BarLayoutCalculator.Compute(TestSections.Rectangle(), TestSections.Grid());

        Assert.Equal(157d, bars[1].X0 - bars[0].X0, Precision);
        Assert.Equal(157d, bars[2].X0 - bars[1].X0, Precision);
    }

    [Fact]
    public void EastFaceBarsAreSpacedByTheClearDepthDividedByTheGaps()
    {
        var bars = BarLayoutCalculator.Compute(TestSections.Rectangle(), TestSections.Grid());

        // Bars 4 and 5 climb the east face between the two corners.
        Assert.Equal(514d / 3, bars[3].Y0 - bars[0].Y0, Precision);
        Assert.Equal(514d / 3, bars[4].Y0 - bars[3].Y0, Precision);
    }

    [Fact]
    public void NumberingRunsClockwiseSouthEastNorthWest()
    {
        var bars = BarLayoutCalculator.Compute(TestSections.Rectangle(), TestSections.Grid());

        Assert.Equal(BarSide.South, bars[0].Side);
        Assert.Equal(BarSide.South, bars[2].Side);
        Assert.Equal(BarSide.East, bars[3].Side);
        Assert.Equal(BarSide.East, bars[4].Side);
        Assert.Equal(BarSide.North, bars[5].Side);
        Assert.Equal(BarSide.North, bars[7].Side);
        Assert.Equal(BarSide.West, bars[8].Side);
        Assert.Equal(BarSide.West, bars[9].Side);
    }

    [Fact]
    public void TheNorthFaceRunsBackFromEastToWest()
    {
        var bars = BarLayoutCalculator.Compute(TestSections.Rectangle(), TestSections.Grid());

        Assert.Equal(bars[2].X0, bars[5].X0, Precision);
        Assert.Equal(bars[0].X0, bars[7].X0, Precision);
        Assert.Equal(bars[7].Y0, bars[5].Y0, Precision);
    }

    [Fact]
    public void TheWestFaceDescendsBackTowardsTheStartingCorner()
    {
        var bars = BarLayoutCalculator.Compute(TestSections.Rectangle(), TestSections.Grid());

        Assert.Equal(bars[0].X0, bars[8].X0, Precision);
        Assert.True(bars[8].Y0 > bars[9].Y0);
    }

    [Fact]
    public void TheSmallestGridIsFourCornerBars()
    {
        var bars = BarLayoutCalculator.Compute(TestSections.Rectangle(), TestSections.Grid(2, 2));

        Assert.Equal(4, bars.Count);
        Assert.All(bars, bar => Assert.True(bar.Side == BarSide.South || bar.Side == BarSide.North));
    }

    [Fact]
    public void ACircularSectionPlacesEveryBarOnTheSameRadius()
    {
        var section = TestSections.Circular();
        var spec = TestSections.Ring();

        var bars = BarLayoutCalculator.Compute(section, spec);

        Assert.Equal(8, bars.Count);
        Assert.Equal(207d, BarLayoutCalculator.BarRadius(section, spec), Precision);
        Assert.All(bars, bar => Assert.Equal(207d, Math.Sqrt(bar.X0 * bar.X0 + bar.Y0 * bar.Y0), Precision));
    }

    [Fact]
    public void TheFirstCircularBarSitsOnThePositiveXAxis()
    {
        var bars = BarLayoutCalculator.Compute(TestSections.Circular(), TestSections.Ring());

        Assert.Equal(207d, bars[0].X0, Precision);
        Assert.Equal(0d, bars[0].Y0, Precision);
    }

    [Fact]
    public void AGridNeedsAtLeastTwoBarsPerFace()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => BarLayoutCalculator.Compute(TestSections.Rectangle(), TestSections.Grid(1, 4)));
    }

    [Fact]
    public void ARingNeedsABarCountDivisibleByFour()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => BarLayoutCalculator.Compute(TestSections.Circular(), TestSections.Ring(6)));
    }

    [Fact]
    public void AHighAspectRatioWallColumnPlacesBarsCorrectly()
    {
        // 200 x 1200 mm column, 2 bars along width, 6 bars along depth = 12 bars total
        var section = TestSections.Rectangle(b: 200, h: 1200);
        var spec = TestSections.Grid(2, 6);

        var bars = BarLayoutCalculator.Compute(section, spec);

        Assert.Equal(12, bars.Count);
        Assert.Equal(BarSide.South, bars[0].Side);
        Assert.Equal(BarSide.South, bars[1].Side);
        Assert.Equal(BarSide.East, bars[2].Side);
        Assert.Equal(BarSide.East, bars[5].Side);
        Assert.Equal(BarSide.North, bars[6].Side);
        Assert.Equal(BarSide.North, bars[7].Side);
        Assert.Equal(BarSide.West, bars[8].Side);
        Assert.Equal(BarSide.West, bars[11].Side);
    }

    [Theory]
    [InlineData(16)]
    [InlineData(20)]
    public void DenseCircularRingsWith16And20BarsAreSupported(int barCount)
    {
        var section = TestSections.Circular(600);
        var spec = TestSections.Ring(barCount);

        var bars = BarLayoutCalculator.Compute(section, spec);

        Assert.Equal(barCount, bars.Count);
        for (var i = 0; i < bars.Count; i++)
        {
            Assert.Equal(i + 1, bars[i].BarNumber);
            Assert.True(bars[i].Side is BarSide.South or BarSide.East or BarSide.North or BarSide.West);
        }
    }
}
