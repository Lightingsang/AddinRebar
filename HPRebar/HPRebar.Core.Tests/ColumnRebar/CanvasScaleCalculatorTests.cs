using System;
using System.Collections.Generic;
using HPRebar.Core.ColumnRebar;
using HPRebar.Core.ColumnRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.ColumnRebar;

public sealed class CanvasScaleCalculatorTests
{
    private const int Precision = 6;

    private static IReadOnlyList<ColumnSection> Stack(params ColumnSection[] sections) => sections;

    [Fact]
    public void ARealSizedColumnIsShrunkToFitTheCanvasWidth()
    {
        var stack = Stack(TestSections.Rectangle(top: 3000));

        var layout = CanvasScaleCalculator.Elevation(stack);

        // Elevation shows width plus depth side by side: 400 + 600 against a 200 budget.
        Assert.Equal(5d, layout.Scale, Precision);
    }

    [Fact]
    public void ATallColumnIsDrivenByItsHeightInstead()
    {
        var stack = Stack(TestSections.Rectangle(b: 200, h: 200, top: 30000));

        var layout = CanvasScaleCalculator.Elevation(stack);

        Assert.Equal((30000d + 160) / 4640, layout.Scale, Precision);
    }

    [Fact]
    public void SomethingSmallEnoughToFitIsDrawnAtFullSize()
    {
        var stack = Stack(TestSections.Rectangle(b: 80, h: 80, top: 1000));

        var layout = CanvasScaleCalculator.Elevation(stack);

        Assert.Equal(1d, layout.Scale, Precision);
    }

    [Fact]
    public void TheCanvasNeverDropsBelowItsMinimumHeight()
    {
        var stack = Stack(TestSections.Rectangle(b: 80, h: 80, top: 500));

        var layout = CanvasScaleCalculator.Elevation(stack);

        Assert.Equal(850d, layout.Height, Precision);
        Assert.Equal(600d, layout.Width, Precision);
        Assert.Equal(770d, layout.Baseline, Precision);
    }

    [Fact]
    public void ATallStackGrowsTheCanvasAndPushesTheBaselineDown()
    {
        var stack = Stack(TestSections.Rectangle(b: 200, h: 200, top: 30000));

        var layout = CanvasScaleCalculator.Elevation(stack);
        var expected = (30000d + 160) / layout.Scale + 160;

        Assert.Equal(expected, layout.Height, Precision);
        Assert.Equal(expected - 80, layout.Baseline, Precision);
    }

    [Fact]
    public void ACircularColumnIsMeasuredAcrossTwoDiameters()
    {
        var stack = Stack(TestSections.Circular(500, top: 3000));

        Assert.Equal(1000d / 200, CanvasScaleCalculator.Elevation(stack).Scale, Precision);
    }

    [Fact]
    public void TheStackIsScaledByItsTallestAndWidestSegment()
    {
        var stack = Stack(
            TestSections.Rectangle(b: 400, h: 600, top: 3000),
            TestSections.Rectangle(b: 900, h: 300, bottom: 3000, top: 6000));

        Assert.Equal((900d + 600) / 200, CanvasScaleCalculator.Elevation(stack).Scale, Precision);
    }

    [Fact]
    public void TheSectionViewUsesItsOwnLargerBudget()
    {
        var stack = Stack(TestSections.Rectangle());

        Assert.Equal(600d / 270, CanvasScaleCalculator.Section(stack), Precision);
    }

    [Fact]
    public void TheDowelsViewIsDrawnSmallerThanTheSectionView()
    {
        var stack = Stack(TestSections.Rectangle());

        Assert.True(CanvasScaleCalculator.Dowels(stack) > CanvasScaleCalculator.Section(stack));
        Assert.Equal(600d / 190, CanvasScaleCalculator.Dowels(stack), Precision);
    }

    [Fact]
    public void TheLargestPlanDimensionIsTheBiggerOfWidthAndDepth()
    {
        var stack = Stack(TestSections.Rectangle(b: 900, h: 300));

        Assert.Equal(900d, CanvasScaleCalculator.LargestPlanDimension(stack), Precision);
    }

    [Fact]
    public void AnEmptyStackIsRejected() =>
        Assert.Throws<ArgumentException>(() => CanvasScaleCalculator.Elevation(new List<ColumnSection>()));
}
