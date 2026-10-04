using System;
using System.Linq;
using HPRebar.Core.BeamRebar.Calculators;
using HPRebar.Core.BeamRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.BeamRebar;

public sealed class BeamSideBarCalculatorTests
{
    private const int Precision = 6;

    // Height Trigger & Counting (Tier 1 & 2)
    [Theory]
    [InlineData(500d, 0)]
    [InlineData(600d, 0)]
    [InlineData(699d, 0)]
    [InlineData(700d, 2)]
    [InlineData(800d, 2)]
    [InlineData(900d, 2)]
    [InlineData(1000d, 3)]
    [InlineData(1200d, 3)]
    public void BeamHeightThresholdDeterminesNumberOfSideBarPairs(double height, int expectedPairs)
    {
        int actual = BeamSideBarCalculator.ComputeRowCount(height);
        Assert.Equal(expectedPairs, actual);
    }

    // Geometry & Positions
    [Fact]
    public void SideBarsArePositionedInPairsAlongLeftAndRightLateralFaces()
    {
        var stack = TestBeamData.DeepBeam(height: 800);
        var bars = BeamSideBarCalculator.ComputeLongitudinalSideBars(stack, new BeamSideBarSpec(), 8.0, 20.0);

        Assert.Equal(4, bars.Count); // 2 rows * 2 sides = 4 bars
        Assert.True(bars[0].TransverseY < 0.0);
        Assert.True(bars[1].TransverseY > 0.0);
        Assert.Equal(bars[0].Points[0].Z, bars[1].Points[0].Z, Precision);
    }

    [Fact]
    public void SideBarTransverseOffsetsNestInsideStirrupLegs()
    {
        var stack = TestBeamData.DeepBeam(width: 400);
        var bars = BeamSideBarCalculator.ComputeLongitudinalSideBars(stack, new BeamSideBarSpec(), 8.0, 20.0);

        double stirrupOuterLeft = (-400.0 / 2.0) + 25.0;
        Assert.True(bars[0].TransverseY > stirrupOuterLeft);
    }

    [Theory]
    [InlineData(700)]
    [InlineData(750)]
    [InlineData(800)]
    [InlineData(900)]
    [InlineData(1000)]
    [InlineData(1200)]
    public void SideBarVerticalSpacingNeverExceedsThreeHundredMillimetres(double height)
    {
        var stack = TestBeamData.DeepBeam(height: height);
        var bars = BeamSideBarCalculator.ComputeLongitudinalSideBars(stack, new BeamSideBarSpec(), 8.0, 20.0);

        double zBotMain = stack.Spans[0].BottomElevation + 25.0 + 8.0 + 10.0;
        double zTopMain = stack.Spans[0].TopElevation - 25.0 - 8.0 - 10.0;

        var leftBars = bars.Where(b => b.TransverseY < 0.0).OrderBy(b => b.Points[0].Z).ToList();
        Assert.NotEmpty(leftBars);

        // Distance from bottom main bar to first skin bar
        Assert.True(leftBars[0].Points[0].Z - zBotMain <= 300.0);

        // Spacing between adjacent skin bars
        for (int i = 0; i < leftBars.Count - 1; i++)
        {
            double diff = leftBars[i + 1].Points[0].Z - leftBars[i].Points[0].Z;
            Assert.True(diff <= 300.0);
        }

        // Distance from last skin bar to top main bar
        Assert.True(zTopMain - leftBars.Last().Points[0].Z <= 300.0);
    }

    [Fact]
    public void SideBarsRunContinuouslyAcrossEntireSpanLength()
    {
        var stack = TestBeamData.DeepBeam(length: 6000);
        var bars = BeamSideBarCalculator.ComputeLongitudinalSideBars(stack, new BeamSideBarSpec(), 8.0, 20.0);

        double barLen = bars[0].Points[1].X - bars[0].Points[0].X;
        Assert.Equal(stack.Spans[0].LengthClear, barLen, Precision);
    }

    // Cross-Ties (C-Ties)
    [Fact]
    public void CrossTiesAreGeneratedWhenSideBarsArePresent()
    {
        var stack = TestBeamData.DeepBeam(height: 800);
        var ties = BeamSideBarCalculator.ComputeCrossTies(stack, new BeamSideBarSpec { IncludeCrossTies = true }, 8.0, 20.0);

        Assert.NotEmpty(ties);
    }

    [Fact]
    public void CrossTiesAreOmittedWhenSideBarsAreAbsent()
    {
        var stack = TestBeamData.SingleSpan(height: 600); // h < 700 mm
        var ties = BeamSideBarCalculator.ComputeCrossTies(stack, new BeamSideBarSpec { IncludeCrossTies = true }, 8.0, 20.0);

        Assert.Empty(ties);
    }

    [Fact]
    public void CrossTiesLongitudinalSpacingMatchesSpecification()
    {
        var stack = TestBeamData.DeepBeam(height: 800);
        var ties = BeamSideBarCalculator.ComputeCrossTies(stack, new BeamSideBarSpec { CrossTieSpacing = 400.0 }, 8.0, 20.0);

        var rowTies = ties.Where(t => t.Layer == 1).OrderBy(t => t.Points[0].X).ToList();
        Assert.True(rowTies.Count >= 2);
        double actualSpacing = rowTies[1].Points[0].X - rowTies[0].Points[0].X;
        Assert.Equal(400.0, actualSpacing, Precision);
    }

    [Fact]
    public void CrossTiesHookShapesEncloseOppositeSideBars()
    {
        var stack = TestBeamData.DeepBeam(height: 800);
        var ties = BeamSideBarCalculator.ComputeCrossTies(stack, new BeamSideBarSpec(), 8.0, 20.0);

        Assert.All(ties, t =>
        {
            Assert.True(t.StartHookAngle != HookAngle.None);
            Assert.True(t.EndHookAngle != HookAngle.None);
        });
    }

    [Fact]
    public void DeepBeamCombinationMaintainsClearDistanceToTopAndBottomLayers()
    {
        var stack = TestBeamData.DeepBeam(height: 800);
        var bars = BeamSideBarCalculator.ComputeLongitudinalSideBars(stack, new BeamSideBarSpec(), 8.0, 20.0);

        double zBotMain = stack.Spans[0].BottomElevation + 25.0 + 8.0 + 10.0;
        double zTopMain = stack.Spans[0].TopElevation - 25.0 - 8.0 - 10.0;

        Assert.All(bars, b => Assert.InRange(b.Points[0].Z, zBotMain + 20.0, zTopMain - 20.0));
    }

    [Fact]
    public void StepChangeInDepthOmitsSideBarsOnlyOnShallowerSpan()
    {
        var stack = TestBeamData.VariableDepth(h1: 800, h2: 600);
        var bars = BeamSideBarCalculator.ComputeLongitudinalSideBars(stack, new BeamSideBarSpec(), 8.0, 20.0);

        Assert.True(bars.All(b => b.HostSpanIndex == 0));
    }

    [Fact]
    public void SideBarsToggleDisabledProducesZeroSideBars()
    {
        var stack = TestBeamData.DeepBeam(height: 800);
        var bars = BeamSideBarCalculator.ComputeLongitudinalSideBars(stack, new BeamSideBarSpec { AutoSkinBars = false }, 8.0, 20.0);

        Assert.Empty(bars);
    }

    [Fact]
    public void CrossTiesAlternatesHookAnglesBetweenStations()
    {
        var stack = TestBeamData.DeepBeam(height: 800);
        var ties = BeamSideBarCalculator.ComputeCrossTies(stack, new BeamSideBarSpec(), 8.0, 20.0);

        Assert.True(ties.Count >= 2);
        Assert.NotEqual(ties[0].StartHookAngle, ties[1].StartHookAngle);
        Assert.NotEqual(ties[0].EndHookAngle, ties[1].EndHookAngle);
    }

    [Fact]
    public void SingleSpanDeepBeamGeneratesExpectedSideBarCount()
    {
        var stack = TestBeamData.DeepBeam(height: 1000);
        var bars = BeamSideBarCalculator.ComputeLongitudinalSideBars(stack, new BeamSideBarSpec(), 8.0, 20.0);

        // 3 rows * 2 sides = 6 bars
        Assert.Equal(6, bars.Count);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(1e-9)]   // the row count passes int.MaxValue
    public void ComputeRowCount_SpacingNeedingMoreRowsThanRevitTakes_Throws(double spacing)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(
            () => BeamSideBarCalculator.ComputeRowCount(900, maxVerticalSpacingMm: spacing));

        Assert.Equal("maxVerticalSpacingMm", error.ParamName);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(0.0)]
    public void ComputeRowCount_SpacingNotAPositiveNumber_UsesThe300MillimetreDefault(double spacing)
    {
        Assert.Equal(
            BeamSideBarCalculator.ComputeRowCount(900),
            BeamSideBarCalculator.ComputeRowCount(900, maxVerticalSpacingMm: spacing));
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(1e-9)]   // the tie count passes int.MaxValue
    public void ComputeCrossTies_SpacingNeedingMoreTiesThanRevitTakes_Throws(double spacing)
    {
        var stack = TestBeamData.DeepBeam(height: 900);
        var spec = new BeamSideBarSpec { IncludeCrossTies = true, CrossTieSpacing = spacing };

        Assert.Throws<ArgumentOutOfRangeException>(
            () => BeamSideBarCalculator.ComputeCrossTies(stack, spec, 8.0, 20.0));
    }

    [Fact]
    public void ComputeCrossTies_InfiniteSpacing_UsesThe400MillimetreDefault()
    {
        var stack = TestBeamData.DeepBeam(height: 900);
        var infinite = new BeamSideBarSpec { IncludeCrossTies = true, CrossTieSpacing = double.PositiveInfinity };
        var standard = new BeamSideBarSpec { IncludeCrossTies = true, CrossTieSpacing = 400.0 };

        var ties = BeamSideBarCalculator.ComputeCrossTies(stack, infinite, 8.0, 20.0);

        Assert.Equal(BeamSideBarCalculator.ComputeCrossTies(stack, standard, 8.0, 20.0).Count, ties.Count);
        Assert.All(ties, tie => Assert.False(double.IsNaN(tie.Polyline.Points[0].X)));
    }
}
