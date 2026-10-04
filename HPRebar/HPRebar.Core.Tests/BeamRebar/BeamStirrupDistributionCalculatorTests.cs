using System;
using System.Linq;
using HPRebar.Core.BeamRebar.Calculators;
using HPRebar.Core.BeamRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.BeamRebar;

public sealed class BeamStirrupDistributionCalculatorTests
{
    private const int Precision = 6;

    // Feature Coverage (Tier 1)
    [Fact]
    public void UniformLayoutReturnsSingleRunEvenlySpaced()
    {
        var spec = TestBeamData.UniformStirrupSpec(spacing: 150);
        var runs = BeamStirrupDistributionCalculator.ComputeSpanRuns(5200, spec);

        Assert.Single(runs);
        Assert.Equal(150.0, runs[0].Spacing, Precision);
    }

    [Fact]
    public void UniformLayoutCentersLeftoverSlackBetweenFirstAndLastBar()
    {
        var spec = TestBeamData.UniformStirrupSpec(spacing: 150);
        // Clear span = 5240 mm, startOffset = 50 mm. Available = 5140 mm.
        // intervals = floor(5140/150) = 34 (5100 mm). Slack = 40 mm. Delta = 20 mm.
        // Expected start offset = 50 + 20 = 70 mm.
        var runs = BeamStirrupDistributionCalculator.ComputeSpanRuns(5240, spec);

        Assert.Single(runs);
        Assert.Equal(70.0, runs[0].StartOffset, Precision);
    }

    [Theory]
    [InlineData(StirrupLayout.ThreeZoneL4, 1400.0, 2800.0)]
    [InlineData(StirrupLayout.ThreeZoneL3, 1866.666667, 1866.666667)]
    public void ZonedLayoutCalculatesExactZoneLengths(StirrupLayout layout, double expectedL1, double expectedL2)
    {
        var (l1, l2, l3) = BeamStirrupDistributionCalculator.ComputeZoneLengths(5600.0, layout);

        Assert.Equal(expectedL1, l1, Precision);
        Assert.Equal(expectedL2, l2, Precision);
        Assert.Equal(l1, l3, Precision);
    }

    [Fact]
    public void ThreeZoneL4LayoutProducesSymmetricSupportRuns()
    {
        var spec = TestBeamData.ThreeZoneL4StirrupSpec(s1: 100, s2: 200);
        var runs = BeamStirrupDistributionCalculator.ComputeSpanRuns(5600, spec);

        Assert.Equal(3, runs.Count);
        Assert.Equal(runs[0].Count, runs[2].Count);
        Assert.Equal(100.0, runs[0].Spacing, Precision);
        Assert.Equal(200.0, runs[1].Spacing, Precision);
        Assert.Equal(100.0, runs[2].Spacing, Precision);
    }

    [Fact]
    public void ThreeZoneL3LayoutDistributesDenseSparseDense()
    {
        var spec = TestBeamData.ThreeZoneL3StirrupSpec(s1: 100, s2: 200);
        var runs = BeamStirrupDistributionCalculator.ComputeSpanRuns(5600, spec);

        Assert.Equal(3, runs.Count);
        Assert.Equal(runs[0].Count, runs[2].Count);
        Assert.Equal(100.0, runs[0].Spacing, Precision);
        Assert.Equal(200.0, runs[1].Spacing, Precision);
        Assert.Equal(100.0, runs[2].Spacing, Precision);
    }

    [Fact]
    public void SupportNodeStirrupToggleGeneratesRunThroughColumnWidth()
    {
        var nodeRun = BeamStirrupDistributionCalculator.ComputeNodeRun(supportWidthMm: 400, coverMm: 25, spacingMm: 150);

        Assert.True(nodeRun.Count > 0);
        Assert.Equal(150.0, nodeRun.Spacing, Precision);
        Assert.InRange(nodeRun.Positions[0], 25.0, 400.0 - 25.0);
    }

    [Fact]
    public void SupportNodeStirrupToggleDisabledProducesZeroNodeStirrups()
    {
        var stack = TestBeamData.TwoSpan();
        var spec = TestBeamData.UniformStirrupSpec() with { IncludeStirrupsInNodes = false };
        var runs = BeamStirrupDistributionCalculator.ComputeStackRuns(stack, spec);

        Assert.Equal(2, runs.Count);
    }

    // Boundary & Corner Cases (Tier 2)
    [Fact]
    public void ClearSpanBelowStartOffsetReturnsZeroStirrups()
    {
        var spec = TestBeamData.UniformStirrupSpec();
        var runs = BeamStirrupDistributionCalculator.ComputeSpanRuns(80, spec);

        Assert.Empty(runs);
    }

    [Fact]
    public void ShortSpanLinkBeamCollapsesThreeZoneToUniform()
    {
        var spec = TestBeamData.ThreeZoneL4StirrupSpec(s1: 100, s2: 200);
        var runs = BeamStirrupDistributionCalculator.ComputeSpanRuns(500, spec);

        Assert.Single(runs);
        Assert.Equal(100.0, runs[0].Spacing, Precision);
    }

    [Fact]
    public void ZeroOrNegativeClearSpanThrowsArgumentOutOfRangeException()
    {
        var spec = TestBeamData.UniformStirrupSpec();
        Assert.Throws<ArgumentOutOfRangeException>(() => BeamStirrupDistributionCalculator.ComputeSpanRuns(0, spec));
        Assert.Throws<ArgumentOutOfRangeException>(() => BeamStirrupDistributionCalculator.ComputeSpanRuns(-500, spec));
    }

    [Fact]
    public void ZeroOrNegativeSpacingThrowsArgumentOutOfRangeException()
    {
        var specZero = TestBeamData.UniformStirrupSpec(spacing: 0);
        var specNeg = TestBeamData.UniformStirrupSpec(spacing: -100);

        Assert.Throws<ArgumentOutOfRangeException>(() => BeamStirrupDistributionCalculator.ComputeSpanRuns(5000, specZero));
        Assert.Throws<ArgumentOutOfRangeException>(() => BeamStirrupDistributionCalculator.ComputeSpanRuns(5000, specNeg));
    }

    [Fact]
    public void SpacingExceedingRevitMaxBarPositionsThrowsArgumentOutOfRangeException()
    {
        // 10000 mm span with 5 mm spacing -> 2001 bars (> 1002)
        var spec = TestBeamData.UniformStirrupSpec(spacing: 5);
        Assert.Throws<ArgumentOutOfRangeException>(() => BeamStirrupDistributionCalculator.ComputeSpanRuns(10000, spec));
    }

    [Fact]
    public void SpacingJustInsideRevitLimitSucceeds()
    {
        // 100100 mm span, spacing 100 mm, startOffset 50 mm.
        // Available = 100100 - 100 = 100000 mm. intervals = 1000. count = 1001 <= 1002.
        var spec = TestBeamData.UniformStirrupSpec(spacing: 100);
        var runs = BeamStirrupDistributionCalculator.ComputeSpanRuns(100100, spec);

        Assert.Single(runs);
        Assert.Equal(1001, runs[0].Count);
    }

    // Multi-Span & Realistic (Tier 3 & 4)
    [Fact]
    public void MultiSpanStackGeneratesIndependentStirrupRunsPerSpan()
    {
        var stack = TestBeamData.TwoSpan();
        var spec = TestBeamData.UniformStirrupSpec();
        var runs = BeamStirrupDistributionCalculator.ComputeStackRuns(stack, spec);

        Assert.Equal(2, runs.Count);
    }

    [Fact]
    public void CantileverSpanAppliesDenseUniformLayoutAlongCantileverLength()
    {
        var stack = TestBeamData.CantileverLeft();
        var spec = TestBeamData.ThreeZoneL4StirrupSpec(s1: 100, s2: 200);
        var runs = BeamStirrupDistributionCalculator.ComputeSpanRuns(stack.Spans[0].LengthClear, spec, isCantilever: true);

        Assert.Single(runs);
        Assert.Equal(100.0, runs[0].Spacing, Precision);
    }

    [Fact]
    public void VaryingSpansGenerateMatchingRunCountsForStandardThreeSpanGirder()
    {
        var spec = TestBeamData.ThreeZoneL4StirrupSpec(s1: 100, s2: 200);
        var runs1 = BeamStirrupDistributionCalculator.ComputeSpanRuns(5600, spec);
        var runs2 = BeamStirrupDistributionCalculator.ComputeSpanRuns(4600, spec);

        Assert.Equal(42, runs1.Sum(r => r.Count));
        Assert.Equal(35, runs2.Sum(r => r.Count));
    }

    [Fact]
    public void DenseSpacingInDeepBeamMaintainsClearDistanceRules()
    {
        var deep = TestBeamData.DeepBeam();
        var runs = BeamStirrupDistributionCalculator.ComputeSpanRuns(deep.Spans[0].LengthClear, TestBeamData.ThreeZoneL4StirrupSpec(s1: 100, s2: 200));

        Assert.All(runs, r => Assert.True(r.Spacing >= 50.0));
    }

    [Fact]
    public void TotalStirrupCountMatchesCalculatedDesignEquation()
    {
        var stack = TestBeamData.ThreeSpan();
        var spec = TestBeamData.ThreeZoneL4StirrupSpec(s1: 100, s2: 200);
        var runs = BeamStirrupDistributionCalculator.ComputeStackRuns(stack, spec);

        int totalCount = runs.Sum(r => r.Count);
        // Span 1 (42) + Span 2 (35) + Span 3 (42) = 119
        Assert.Equal(119, totalCount);
    }

    [Theory]
    [InlineData(4600)]
    [InlineData(5200)]
    [InlineData(5600)]
    [InlineData(6200)]
    [InlineData(7500)]
    public void ThreeZoneBoundaryTransitionsNeverProduceCoincidentOrSubAggregateSpacing(double clearSpan)
    {
        var spec = TestBeamData.ThreeZoneL4StirrupSpec(s1: 100, s2: 200);
        var runs = BeamStirrupDistributionCalculator.ComputeSpanRuns(clearSpan, spec);

        Assert.Equal(3, runs.Count);
        double delta1to2 = runs[1].StartX - runs[0].EndX;
        double delta2to3 = runs[2].StartX - runs[1].EndX;

        // Strictly exceeds aggregate clearance (>= 50 mm)
        Assert.True(delta1to2 >= 50.0);
        Assert.True(delta2to3 >= 50.0);

        // Never exceeds sparse spacing limit (<= 200 mm)
        Assert.True(delta1to2 <= runs[1].Spacing);
        Assert.True(delta2to3 <= runs[1].Spacing);
    }

    [Fact]
    public void ChallengerAttackScenarioSixtyTwoHundredMillimetresHasZeroClash()
    {
        var spec100 = TestBeamData.ThreeZoneL4StirrupSpec(s1: 100, s2: 100);
        var runs100 = BeamStirrupDistributionCalculator.ComputeSpanRuns(6200, spec100);

        Assert.Equal(3, runs100.Count);
        Assert.Equal(100.0, runs100[1].StartX - runs100[0].EndX, Precision);
        Assert.Equal(100.0, runs100[2].StartX - runs100[1].EndX, Precision);

        var spec310 = TestBeamData.ThreeZoneL4StirrupSpec(s1: 100, s2: 310);
        var runs310 = BeamStirrupDistributionCalculator.ComputeSpanRuns(6200, spec310);

        Assert.Equal(3, runs310.Count);
        double gap = runs310[1].StartX - runs310[0].EndX;
        Assert.True(gap >= 50.0);
        Assert.True(gap <= 310.0);
    }

    [Fact]
    public void NodeSpacingSoSmallTheCountOverflowsAnIntIsRefusedAtTheLimit()
    {
        // 350 mm / 1e-7 mm = 3.5e9 intervals, past int.MaxValue
        var error = Assert.Throws<ArgumentOutOfRangeException>(
            () => BeamStirrupDistributionCalculator.ComputeNodeRun(supportWidthMm: 400, coverMm: 25, spacingMm: 1e-7));

        Assert.Contains("exceeds maximum 1002", error.Message);
        Assert.Equal("spacingMm", error.ParamName);
    }
}
