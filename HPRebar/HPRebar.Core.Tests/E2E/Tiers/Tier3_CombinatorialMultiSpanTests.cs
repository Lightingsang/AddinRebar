using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.BeamRebar.Calculators;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.Tcvn5574.Calculators;
using HPRebar.Core.Tcvn5574.Materials;
using HPRebar.Core.Tcvn5574.Models;
using Xunit;

namespace HPRebar.Core.Tests.E2E.Tiers;

/// <summary>
/// Tier 3: Combinatorial & Multi-Span Feature Acceptance Tests.
/// Validates cross-combinations of materials, section sizes, multi-span continuous beams,
/// dual-diameter rebar arrangements, and multi-zone stirrup layouts.
/// </summary>
public sealed class Tier3_CombinatorialMultiSpanTests
{
    [Theory]
    [InlineData(ConcreteGrade.B20, SteelGrade.CB300V, 80.0, 200.0, 400.0)]
    [InlineData(ConcreteGrade.B25, SteelGrade.CB400V, 150.0, 250.0, 500.0)]
    [InlineData(ConcreteGrade.B30, SteelGrade.CB400V, 250.0, 300.0, 600.0)]
    [InlineData(ConcreteGrade.B35, SteelGrade.CB500V, 400.0, 400.0, 700.0)]
    public void TC_T3_CMB_01_MaterialAndSectionCombinations_ProduceCompliantResults(
        ConcreteGrade cGrade, SteelGrade sGrade, double momentKnm, double b, double h)
    {
        var concrete = ConcreteMaterial.Create(cGrade);
        var steel = SteelMaterial.Create(sGrade);

        var flexure = TcvnBeamFlexureCalculator.Calculate(b, h, momentKnm, concrete, steel);

        Assert.True(flexure.IsCompliant);
        Assert.True(flexure.AsRequired > 0.0);

        var opt = TcvnBeamRebarOptimizer.Optimize(flexure.AsRequired, b, h);
        Assert.True(opt.IsValid);
        Assert.True(opt.ProvidedArea >= flexure.AsRequired);
    }

    [Fact]
    public void TC_T3_MSP_01_TwoSpanContinuousBeam_CalculatesStationsAndSupportsCorrectly()
    {
        var span0 = new BeamSpan(0, "Span 1", 5000.0, 250.0, 500.0, 0.0, 25.0, 4600.0);
        var span1 = new BeamSpan(1, "Span 2", 6000.0, 250.0, 500.0, 0.0, 25.0, 5600.0);

        var stirrupSpec = new BeamStirrupSpec(
            layout: StirrupLayout.ThreeZoneL4,
            diameter: 8.0,
            cover: 25.0,
            spacingDense: 100.0,
            spacingSparse: 200.0);

        var runsSpan0 = BeamStirrupDistributionCalculator.ComputeSpanRuns(span0.LengthClear, stirrupSpec);
        var runsSpan1 = BeamStirrupDistributionCalculator.ComputeSpanRuns(span1.LengthClear, stirrupSpec);

        Assert.Equal(3, runsSpan0.Count);
        Assert.Equal(3, runsSpan1.Count);

        Assert.True(runsSpan0.Sum(r => r.Count) > 20);
        Assert.True(runsSpan1.Sum(r => r.Count) > 25);
    }

    [Fact]
    public void TC_T3_MSP_02_ThreeSpanContinuousFrame_MaintainsGeometryContinuity()
    {
        var spans = new List<BeamSpan>
        {
            new(0, "Span 1", 4500.0, 250.0, 750.0, 0.0, 25.0, 4100.0),
            new(1, "Span 2", 5500.0, 250.0, 750.0, 0.0, 25.0, 5100.0),
            new(2, "Span 3", 4500.0, 250.0, 750.0, 0.0, 25.0, 4100.0)
        };

        var stack = new BeamContinuousStack(spans, Array.Empty<BeamSupportNode>());
        var sideSpec = new BeamSideBarSpec(autoSkinBars: true, depthThreshold: 700.0, diameter: 12.0);

        var sideBars = BeamSideBarCalculator.ComputeLongitudinalSideBars(stack, sideSpec, 8.0, 20.0);

        // 3 deep spans, each requires side bars (2 rows * 2 faces = 4 bars per span, total 12)
        Assert.NotEmpty(sideBars);
        Assert.Equal(12, sideBars.Count);
        Assert.All(sideBars, bar => Assert.Equal(BarType.SideSkin, bar.Type));
    }

    [Fact]
    public void TC_T3_DUL_01_DualDiameterArrangement_SelectsValidBalancedArrangement()
    {
        var candidates = TcvnBeamRebarOptimizer.GenerateCandidates(
            asRequired: 1050.0, b: 250.0, cover: 25.0, stirrupDiameter: 8.0, isTop: false);

        var dualOption = candidates.FirstOrDefault(c => c.DiameterLayer2 == 0.0 && c.Description.Contains("+") && c.IsValid);

        Assert.NotNull(dualOption);
        Assert.True(dualOption.ProvidedArea >= 1050.0);
        Assert.True(dualOption.IsSpacingCompliant);
        Assert.True(dualOption.ClearSpacingLayer1 >= 25.0);
    }

    [Fact]
    public void TC_T3_ZON_01_VaryingStirrupZones_TransitionsSmoothlyWithoutGap()
    {
        var spec = new BeamStirrupSpec(
            layout: StirrupLayout.ThreeZoneL4,
            diameter: 8.0,
            cover: 25.0,
            spacingDense: 100.0,
            spacingSparse: 200.0);

        var runs = BeamStirrupDistributionCalculator.ComputeSpanRuns(clearSpanMm: 6000.0, spec: spec);

        Assert.Equal(3, runs.Count);
        Assert.Equal(100.0, runs[0].Spacing);
        Assert.Equal(200.0, runs[1].Spacing);
        Assert.Equal(100.0, runs[2].Spacing);

        double totalCovered = runs.Sum(r => r.Length);
        Assert.True(totalCovered > 5500.0);
    }
}
