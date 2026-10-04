using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.ColumnRebar;
using HPRebar.Core.ColumnRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.ColumnRebar;

public sealed class SpliceCalculatorTests
{
    private const int Precision = 6;
    private const double Inset = TestSections.Cover + TestSections.StirrupDiameter + TestSections.BarDiameter / 2;

    private static SpliceSpec Transition() =>
        new() { IsTopDowels = true, TopStyle = TopDowelStyle.BendIntoColumnAbove };

    private static SpliceSpec StoppedUnderBeam() =>
        new() { IsTopDowels = true, TopStyle = TopDowelStyle.StopUnderBeam };

    [Fact]
    public void WithNoSectionAboveEveryBarStaysWhereItIs()
    {
        var spec = TestSections.Grid();
        var bars = BarLayoutCalculator.Compute(TestSections.Rectangle(), spec);
        var splices = bars.Select(_ => Transition()).ToList();

        var upper = SpliceCalculator.ComputeUpperPositions(null, spec, 8, 8, bars, splices);

        for (var i = 0; i < bars.Count; i++)
        {
            Assert.Equal(bars[i].X0, upper[i].X, Precision);
            Assert.Equal(bars[i].Y0, upper[i].Y, Precision);
        }
    }

    [Fact]
    public void SouthFaceBarsRespreadAcrossTheNarrowerSectionAbove()
    {
        var spec = TestSections.Grid();
        var above = TestSections.Rectangle(b: 300, h: 500);
        var bars = BarLayoutCalculator.Compute(TestSections.Rectangle(), spec);

        // Only the three south-face bars carry on upward.
        var splices = bars.Select(bar => bar.Side == BarSide.South ? Transition() : StoppedUnderBeam()).ToList();

        var upper = SpliceCalculator.ComputeUpperPositions(above, spec, 8, 8, bars, splices);

        var delta = (300d - 2 * TestSections.Cover - 2 * TestSections.StirrupDiameter - TestSections.BarDiameter) / 2;

        // The outermost bars are nudged one diameter clear of the bars already standing above.
        Assert.Equal(Inset + 20, upper[0].X, Precision);
        Assert.Equal(Inset + 20 + delta, upper[1].X, Precision);
        Assert.Equal(Inset - 20 + 2 * delta, upper[2].X, Precision);

        Assert.All(upper.Take(3), point => Assert.Equal(Inset, point.Y, Precision));
    }

    [Fact]
    public void ABarStoppingUnderTheBeamIsNotRespread()
    {
        var spec = TestSections.Grid();
        var above = TestSections.Rectangle(b: 300, h: 500);
        var bars = BarLayoutCalculator.Compute(TestSections.Rectangle(), spec);

        var allStopped = bars.Select(_ => StoppedUnderBeam()).ToList();
        var onlyFirst = bars.Select(bar => bar.BarNumber == 1 ? Transition() : StoppedUnderBeam()).ToList();

        var baseline = SpliceCalculator.ComputeUpperPositions(above, spec, 8, 8, bars, allStopped);
        var withOne = SpliceCalculator.ComputeUpperPositions(above, spec, 8, 8, bars, onlyFirst);

        // Bar 2 is untouched by bar 1 changing its mind.
        Assert.Equal(baseline[1].X, withOne[1].X, Precision);
        Assert.Equal(baseline[1].Y, withOne[1].Y, Precision);
    }

    [Fact]
    public void ALoneTransitionBarOnAFaceKeepsItsGridSlot()
    {
        var spec = TestSections.Grid();
        var above = TestSections.Rectangle(b: 300, h: 500);
        var bars = BarLayoutCalculator.Compute(TestSections.Rectangle(), spec);

        var splices = bars.Select(bar => bar.BarNumber == 2 ? Transition() : StoppedUnderBeam()).ToList();
        var upper = SpliceCalculator.ComputeUpperPositions(above, spec, 8, 8, bars, splices);

        var grid = DefaultUpperPositions.X(above, spec, 8, TestSections.BarDiameter);

        Assert.Equal(grid[1], upper[1].X, Precision);
    }

    [Fact]
    public void EastFaceBarsSpreadOverOneMoreGapThanThereAreBarsToClearTheCorners()
    {
        var spec = TestSections.Grid();
        var above = TestSections.Rectangle(b: 300, h: 500);
        var bars = BarLayoutCalculator.Compute(TestSections.Rectangle(), spec);

        var splices = bars.Select(bar => bar.Side == BarSide.East ? Transition() : StoppedUnderBeam()).ToList();
        var upper = SpliceCalculator.ComputeUpperPositions(above, spec, 8, 8, bars, splices);

        var delta = (500d - 2 * TestSections.Cover - 2 * TestSections.StirrupDiameter - TestSections.BarDiameter) / 3;

        Assert.Equal(300d - Inset, upper[3].X, Precision);
        Assert.Equal(Inset + delta - 20, upper[3].Y, Precision);
        Assert.Equal(Inset + 2 * delta - 20, upper[4].Y, Precision);
    }

    [Fact]
    public void NorthFaceBarsAreMeasuredBackFromTheEastEdge()
    {
        var spec = TestSections.Grid();
        var above = TestSections.Rectangle(b: 300, h: 500);
        var bars = BarLayoutCalculator.Compute(TestSections.Rectangle(), spec);

        var splices = bars.Select(bar => bar.Side == BarSide.North ? Transition() : StoppedUnderBeam()).ToList();
        var upper = SpliceCalculator.ComputeUpperPositions(above, spec, 8, 8, bars, splices);

        var delta = (300d - 2 * TestSections.Cover - 2 * TestSections.StirrupDiameter - TestSections.BarDiameter) / 2;

        Assert.Equal(300d - Inset - 20, upper[5].X, Precision);
        Assert.Equal(300d - Inset + 20 - 2 * delta, upper[7].X, Precision);
        Assert.All(new[] { upper[5], upper[6], upper[7] }, point => Assert.Equal(500d - Inset, point.Y, Precision));
    }

    [Fact]
    public void WestFaceBarsDescendFromTheNorthEdge()
    {
        var spec = TestSections.Grid();
        var above = TestSections.Rectangle(b: 300, h: 500);
        var bars = BarLayoutCalculator.Compute(TestSections.Rectangle(), spec);

        var splices = bars.Select(bar => bar.Side == BarSide.West ? Transition() : StoppedUnderBeam()).ToList();
        var upper = SpliceCalculator.ComputeUpperPositions(above, spec, 8, 8, bars, splices);

        var delta = (500d - 2 * TestSections.Cover - 2 * TestSections.StirrupDiameter - TestSections.BarDiameter) / 3;

        Assert.Equal(Inset, upper[8].X, Precision);
        Assert.Equal(500d - Inset - delta - 20, upper[8].Y, Precision);
        Assert.Equal(500d - Inset - 2 * delta - 20, upper[9].Y, Precision);
    }

    [Fact]
    public void ACircularQuadrantCornerBarKeepsItsSlot()
    {
        var spec = TestSections.Ring();
        var above = TestSections.Circular(400);
        var bars = BarLayoutCalculator.Compute(TestSections.Circular(), spec);
        var splices = bars.Select(_ => Transition()).ToList();

        var upper = SpliceCalculator.ComputeUpperPositions(above, spec, 8, 8, bars, splices);
        var grid = DefaultUpperPositions.X(above, spec, 8, TestSections.BarDiameter);

        // Bars 1, 3, 5 and 7 sit exactly on the quadrant boundaries of an eight-bar ring.
        foreach (var index in new[] { 0, 2, 4, 6 })
        {
            Assert.Equal(grid[index], upper[index].X, Precision);
        }
    }

    [Fact]
    public void ACircularBarInsideAQuadrantIsRespreadOntoTheSmallerRadius()
    {
        var spec = TestSections.Ring();
        var above = TestSections.Circular(400);
        var bars = BarLayoutCalculator.Compute(TestSections.Circular(), spec);
        var splices = bars.Select(_ => Transition()).ToList();

        var upper = SpliceCalculator.ComputeUpperPositions(above, spec, 8, 8, bars, splices);

        var radius = 400 * 0.5 - TestSections.Cover - TestSections.StirrupDiameter - TestSections.BarDiameter * 0.5;
        var distance = Math.Sqrt(upper[1].X * upper[1].X + upper[1].Y * upper[1].Y);

        Assert.Equal(radius, distance, Precision);
    }

    [Fact]
    public void FourCircularBarsHaveNothingToRespread()
    {
        var spec = TestSections.Ring(4);
        var above = TestSections.Circular(400);
        var bars = BarLayoutCalculator.Compute(TestSections.Circular(), spec);
        var splices = bars.Select(_ => Transition()).ToList();

        var upper = SpliceCalculator.ComputeUpperPositions(above, spec, 8, 8, bars, splices);
        var gridX = DefaultUpperPositions.X(above, spec, 8, TestSections.BarDiameter);
        var gridY = DefaultUpperPositions.Y(above, spec, 8, TestSections.BarDiameter);

        for (var i = 0; i < 4; i++)
        {
            Assert.Equal(gridX[i], upper[i].X, Precision);
            Assert.Equal(gridY[i], upper[i].Y, Precision);
        }
    }

    [Fact]
    public void OneSpliceSpecPerBarIsRequired()
    {
        var spec = TestSections.Grid();
        var bars = BarLayoutCalculator.Compute(TestSections.Rectangle(), spec);

        Assert.Throws<ArgumentException>(
            () => SpliceCalculator.ComputeUpperPositions(TestSections.Rectangle(), spec, 8, 8, bars, new List<SpliceSpec>()));
    }
}
