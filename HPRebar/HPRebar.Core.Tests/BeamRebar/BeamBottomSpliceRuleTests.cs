using System.Linq;
using HPRebar.Core.BeamRebar.Calculators;
using HPRebar.Core.BeamRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.BeamRebar;

/// <summary>
///     Where bottom bars longer than the stock length are lapped: over the interior support nearest the middle of
///     the run, never over an end support or a cantilever root; with no interior support, a quarter of the first
///     supported span from its start.
/// </summary>
public sealed class BeamBottomSpliceRuleTests
{
    private const double LapLength = 40.0 * TestBeamData.DefaultMainBottomDiameter;
    private const double StaggerOffset = 1.3 * LapLength;

    [Fact]
    public void ComputeBottomMainBars_SingleSpanLongerThanStock_LapsAQuarterSpanFromTheLeftSupport()
    {
        // Arrange
        var stack = TestBeamData.SingleSpan(length: 12500);
        var span = stack.Spans[0];
        double expectedCenter = span.StartX + (span.LengthClear / 4.0);

        // Act
        var bars = BeamMainBarCalculator.ComputeBottomMainBars(stack, TestBeamData.MainBarSpec(), stirrupDiameterMm: 10.0);

        // Assert
        Assert.Equal(expectedCenter, LapCenterOfFirstBar(bars), 6);
    }

    [Fact]
    public void ComputeBottomMainBars_SingleSpanLongerThanStock_EveryPieceFitsTheStockLength()
    {
        // Arrange
        var stack = TestBeamData.SingleSpan(length: 12500);
        var spec = TestBeamData.MainBarSpec();

        // Act
        var bars = BeamMainBarCalculator.ComputeBottomMainBars(stack, spec, stirrupDiameterMm: 10.0);

        // Assert
        Assert.All(bars, bar => Assert.True(bar.Polyline.TotalLength <= spec.MaxStockLength, $"piece {bar.BarIndex} is {bar.Polyline.TotalLength:0} mm"));
    }

    [Fact]
    public void ComputeBottomMainBars_LeftCantileverSpliced_LapsInTheSupportedSpanNotAtTheRoot()
    {
        // Arrange: tip, root column, end column — no interior support the bottom bars run over
        var stack = TestBeamData.CantileverLeft();
        var span = stack.Spans[1];
        var spec = TestBeamData.MainBarSpec(hookLength: 0.0) with { MaxStockLength = 5000.0 };

        // Act
        var bars = BeamMainBarCalculator.ComputeBottomMainBars(stack, spec, stirrupDiameterMm: 10.0);

        // Assert
        Assert.Equal(span.StartX + (span.LengthClear / 4.0), LapCenterOfFirstBar(bars), 6);
    }

    [Fact]
    public void ComputeBottomMainBars_ThreeEqualSpans_LapsOverTheLaterOfTheTwoMiddleSupports()
    {
        // Arrange: supports at 0, 6000, 12000, 18000; both interior ones are 3000 mm from the middle
        var stack = TestBeamData.ThreeSpan();

        // Act
        var bars = BeamMainBarCalculator.ComputeBottomMainBars(stack, TestBeamData.MainBarSpec(), stirrupDiameterMm: 10.0);

        // Assert
        Assert.Equal(stack.Supports[2].CenterX, LapCenterOfFirstBar(bars), 6);
    }

    [Fact]
    public void ComputeBottomMainBars_LongRunWithoutSupports_LapsInTheSpanInsteadOfThrowing()
    {
        // Arrange
        var stack = TestBeamData.SingleSpan(length: 12500) with { Supports = System.Array.Empty<BeamSupportNode>() };

        // Act
        var bars = BeamMainBarCalculator.ComputeBottomMainBars(stack, TestBeamData.MainBarSpec(), stirrupDiameterMm: 10.0);

        // Assert
        var span = stack.Spans[0];
        Assert.Equal(span.StartX + (span.LengthClear / 4.0), LapCenterOfFirstBar(bars), 6);
    }

    /// <summary>The first bar's left piece ends half a lap past the lap center, which sits half a stagger before the splice point.</summary>
    private static double LapCenterOfFirstBar(System.Collections.Generic.IReadOnlyList<BarPolyline> bars)
    {
        var leftPiece = bars.First(bar => bar.BarIndex == 0);
        return leftPiece.EndX - (LapLength / 2.0) + (StaggerOffset / 2.0);
    }
}
