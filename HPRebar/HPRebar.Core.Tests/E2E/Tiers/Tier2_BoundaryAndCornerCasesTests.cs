using System;
using System.Linq;
using HPRebar.Core.BeamRebar.Calculators;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.Tcvn5574.Calculators;
using HPRebar.Core.Tcvn5574.Materials;
using Xunit;

namespace HPRebar.Core.Tests.E2E.Tiers;

/// <summary>
/// Tier 2: Boundary and Corner Cases Acceptance Tests.
/// Stresses physical crushing thresholds, zero/extreme moments, narrow sections,
/// clearance violations, and pathological argument validation.
/// </summary>
public sealed class Tier2_BoundaryAndCornerCasesTests
{
    [Fact]
    public void TC_T2_SHR_01_CrushThreshold_ExactBoundaryTransitionsAccurately()
    {
        // 300 x 600 mm beam, B25 (Rb = 14.5 MPa), a = 40 mm => h0 = 560 mm
        // Qb,max = 0.3 * 14.5 * 300 * 560 = 730,800 N = 730.8 kN
        var concrete = ConcreteMaterial.Create(ConcreteGrade.B25);
        var steel = SteelMaterial.Create(SteelGrade.CB240T);

        double qBelowKn = 729.0; // below 730.8 kN
        var resultBelow = TcvnBeamShearCalculator.Calculate(
            b: 300.0, h: 600.0, shearKn: qBelowKn, concrete: concrete, stirrupSteel: steel, stirrupDiameter: 12.0, legs: 4);

        Assert.False(resultBelow.IsWebCrushExceeded);
        Assert.True(resultBelow.IsCompliant);

        double qAboveKn = 732.0; // above 730.8 kN
        var resultAbove = TcvnBeamShearCalculator.Calculate(
            b: 300.0, h: 600.0, shearKn: qAboveKn, concrete: concrete, stirrupSteel: steel, stirrupDiameter: 12.0, legs: 4);

        Assert.True(resultAbove.IsWebCrushExceeded);
        Assert.False(resultAbove.IsCompliant);
        Assert.Contains("crushing exceeded", resultAbove.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TC_T2_FLX_01_ZeroMoment_AppliesMinimumConstructiveReinforcement()
    {
        // 200 x 400 mm beam, B20, CB300-V, M = 0.0 kN.m
        var concrete = ConcreteMaterial.Create(ConcreteGrade.B20);
        var steel = SteelMaterial.Create(SteelGrade.CB300V);

        var result = TcvnBeamFlexureCalculator.Calculate(
            b: 200.0, h: 400.0, momentKnm: 0.0, concrete: concrete, steel: steel, a: 40.0);

        // h0 = 360 mm, As,min = 0.001 * 200 * 360 = 72.0 mm2
        Assert.True(result.IsCompliant);
        Assert.Equal(72.0, result.AsRequired, 2);
        Assert.Equal(0.0, result.AsCompressionRequired, 2);
        Assert.Equal(0.0, result.AlphaM, 4);
    }

    [Fact]
    public void TC_T2_FLX_02_ExtremeMoment_FlagsOverReinforcedCongestion()
    {
        // 200 x 300 mm small beam, B15 (Rb=8.5), CB400-V, huge moment M = 350 kN.m
        var concrete = ConcreteMaterial.Create(ConcreteGrade.B15);
        var steel = SteelMaterial.Create(SteelGrade.CB400V);

        var result = TcvnBeamFlexureCalculator.Calculate(
            b: 200.0, h: 300.0, momentKnm: 350.0, concrete: concrete, steel: steel, a: 40.0, aPrime: 40.0);

        Assert.True(result.IsDoublyReinforced);
        Assert.True(result.IsOverReinforced);
        Assert.False(result.IsCompliant);
        Assert.Contains("overloaded", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(-200.0, 400.0, 40.0)]
    [InlineData(200.0, -400.0, 40.0)]
    [InlineData(200.0, 400.0, 450.0)] // a >= h
    public void TC_T2_ARG_01_FlexureCalculator_RejectsPathologicalDimensions(
        double b, double h, double a)
    {
        var concrete = ConcreteMaterial.Create(ConcreteGrade.B25);
        var steel = SteelMaterial.Create(SteelGrade.CB400V);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TcvnBeamFlexureCalculator.Calculate(b, h, 50.0, concrete, steel, a: a));
    }

    [Theory]
    [InlineData(200.0, 400.0, 0.0, 2)] // d = 0
    [InlineData(200.0, 400.0, 8.0, 0)] // legs = 0
    public void TC_T2_ARG_02_ShearCalculator_RejectsInvalidStirrupParameters(
        double b, double h, double stirrupD, int legs)
    {
        var concrete = ConcreteMaterial.Create(ConcreteGrade.B25);
        var steel = SteelMaterial.Create(SteelGrade.CB240T);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TcvnBeamShearCalculator.Calculate(b, h, 50.0, concrete, steel, stirrupDiameter: stirrupD, legs: legs));
    }

    [Fact]
    public void TC_T2_SPC_01_NarrowBeamSevereCrowding_DetectsNonCompliance()
    {
        // b = 150 mm narrow beam with 3 bars d25 mm
        // clearWidth = 150 - 2*(25 + 8) = 84 mm
        // 3 bars => (84 - 3*25) / 2 = (84 - 75) / 2 = 4.5 mm < 25 mm!
        var (spacing, isCompliant, required) = TcvnBeamRebarOptimizer.ValidateClearSpacing(
            barCount: 3, barDiameter: 25.0, b: 150.0, cover: 25.0, stirrupDiameter: 8.0, isTop: false);

        Assert.Equal(4.5, spacing, 1);
        Assert.False(isCompliant);
        Assert.Equal(25.0, required);
    }

    [Fact]
    public void TC_T2_GEO_01_ShortDeepBeamSpan_CalculatesSafeStirrupRuns()
    {
        var spec = new BeamStirrupSpec(
            layout: StirrupLayout.Uniform,
            diameter: 8.0,
            cover: 25.0,
            spacingDense: 100.0,
            spacingSparse: 100.0);

        var runs = BeamStirrupDistributionCalculator.ComputeSpanRuns(clearSpanMm: 1200.0, spec: spec);

        Assert.NotEmpty(runs);
        Assert.True(runs.Sum(r => r.Count) > 5);
        Assert.All(runs, r => Assert.True(r.Spacing > 0));
    }

    [Fact]
    public void TC_T2_GEO_02_LongTransferBeamSpan_MaintainsGeometricInvariants()
    {
        var spec = new BeamStirrupSpec(
            layout: StirrupLayout.ThreeZoneL4,
            diameter: 10.0,
            cover: 30.0,
            spacingDense: 100.0,
            spacingSparse: 200.0);

        var runs = BeamStirrupDistributionCalculator.ComputeSpanRuns(clearSpanMm: 12000.0, spec: spec);

        Assert.NotEmpty(runs);
        int totalStirrups = runs.Sum(r => r.Count);
        Assert.True(totalStirrups > 60);
    }
}
