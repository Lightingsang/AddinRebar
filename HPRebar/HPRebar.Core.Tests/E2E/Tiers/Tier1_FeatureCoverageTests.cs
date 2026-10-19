using System;
using System.Linq;
using HPRebar.Core.BeamRebar.Calculators;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.Tcvn5574.Calculators;
using HPRebar.Core.Tcvn5574.Materials;
using HPRebar.Core.Tcvn5574.Models;
using Xunit;

namespace HPRebar.Core.Tests.E2E.Tiers;

/// <summary>
/// Tier 1: Feature Coverage Acceptance Tests.
/// Validates nominal behavior for Concrete B15-B40, Steel CB240-T to CB500-V,
/// singly/doubly reinforced flexure, shear capacity, clear spacing, and 3D geometry contracts.
/// </summary>
public sealed class Tier1_FeatureCoverageTests
{
    [Theory]
    [InlineData(ConcreteGrade.B15, 8.5, 0.75, 24000.0)]
    [InlineData(ConcreteGrade.B20, 11.5, 0.90, 27500.0)]
    [InlineData(ConcreteGrade.B25, 14.5, 1.05, 30000.0)]
    [InlineData(ConcreteGrade.B30, 17.0, 1.15, 32500.0)]
    [InlineData(ConcreteGrade.B35, 19.5, 1.30, 34500.0)]
    [InlineData(ConcreteGrade.B40, 22.0, 1.40, 36000.0)]
    public void TC_T1_MAT_01_ConcreteGrades_MatchTcvn5574Tables(
        ConcreteGrade grade, double expectedRb, double expectedRbt, double expectedEb)
    {
        var mat = ConcreteMaterial.Create(grade);

        Assert.Equal(expectedRb, mat.Rb, 3);
        Assert.Equal(expectedRbt, mat.Rbt, 3);
        Assert.Equal(expectedEb, mat.Eb, 1);
        Assert.False(string.IsNullOrWhiteSpace(mat.Name));
    }

    [Theory]
    [InlineData(SteelGrade.CB240T, 210.0, 210.0, 170.0, 0.6154, 0.426)]
    [InlineData(SteelGrade.CB300V, 260.0, 260.0, 210.0, 0.5833, 0.413)]
    [InlineData(SteelGrade.CB400V, 350.0, 350.0, 280.0, 0.5333, 0.391)]
    [InlineData(SteelGrade.CB500V, 435.0, 400.0, 300.0, 0.4934, 0.372)]
    public void TC_T1_MAT_02_SteelGrades_MatchTcvn5574Tables(
        SteelGrade grade, double expectedRs, double expectedRsc, double expectedRsw,
        double expectedXiR, double expectedAlphaR)
    {
        var mat = SteelMaterial.Create(grade);

        Assert.Equal(expectedRs, mat.Rs, 2);
        Assert.Equal(expectedRsc, mat.Rsc, 2);
        Assert.Equal(expectedRsw, mat.Rsw, 2);
        Assert.Equal(expectedXiR, mat.XiR, 3);
        Assert.Equal(expectedAlphaR, mat.AlphaR, 3);
    }

    [Fact]
    public void TC_T1_FLX_01_SinglyReinforcedBending_ComputesAccurateAs()
    {
        // 250 x 500 mm beam, B25, CB400-V, M = 110 kN.m
        var concrete = ConcreteMaterial.Create(ConcreteGrade.B25);
        var steel = SteelMaterial.Create(SteelGrade.CB400V);

        var result = TcvnBeamFlexureCalculator.Calculate(
            b: 250.0, h: 500.0, momentKnm: 110.0, concrete: concrete, steel: steel, a: 40.0);

        Assert.True(result.IsCompliant);
        Assert.False(result.IsDoublyReinforced);
        Assert.False(result.IsOverReinforced);
        Assert.True(result.AlphaM < steel.AlphaR);
        Assert.InRange(result.AsRequired, 720.0, 760.0);
        Assert.Equal(0.0, result.AsCompressionRequired, 2);
    }

    [Fact]
    public void TC_T1_FLX_02_DoublyReinforcedBending_ComputesAsAndAsPrime()
    {
        // 250 x 500 mm beam, B20 (Rb=11.5), CB400-V, M = 280 kN.m (exceeds alpha_R)
        var concrete = ConcreteMaterial.Create(ConcreteGrade.B20);
        var steel = SteelMaterial.Create(SteelGrade.CB400V);

        var result = TcvnBeamFlexureCalculator.Calculate(
            b: 250.0, h: 500.0, momentKnm: 280.0, concrete: concrete, steel: steel, a: 40.0, aPrime: 40.0);

        Assert.True(result.IsDoublyReinforced);
        Assert.True(result.AlphaM > steel.AlphaR);
        Assert.True(result.AsCompressionRequired > 0.0);
        Assert.True(result.AsRequired > result.AsCompressionRequired);
    }

    [Fact]
    public void TC_T1_SHR_01_LowShear_ResistedByConcreteWithConstructiveSpacing()
    {
        // 250 x 500 mm beam, B25 (Rbt=1.05), Q = 40 kN <= Qb,min
        var concrete = ConcreteMaterial.Create(ConcreteGrade.B25);
        var steel = SteelMaterial.Create(SteelGrade.CB240T);

        var result = TcvnBeamShearCalculator.Calculate(
            b: 250.0, h: 500.0, shearKn: 40.0, concrete: concrete, stirrupSteel: steel,
            stirrupDiameter: 8.0, legs: 2, a: 40.0, isSupportZone: true);

        Assert.True(result.IsCompliant);
        Assert.False(result.IsWebCrushExceeded);
        Assert.False(result.RequiresStirrupByCalculation);
        Assert.Equal(150.0, result.ConstructiveSpacing, 1);
        Assert.InRange(result.RecommendedSpacing, 100.0, 150.0);
    }

    [Fact]
    public void TC_T1_SHR_02_ModerateShear_RequiresCalculatedStirrupSpacing()
    {
        // 250 x 500 mm beam, B25, CB240-T, Q = 140 kN > Qb,min
        var concrete = ConcreteMaterial.Create(ConcreteGrade.B25);
        var steel = SteelMaterial.Create(SteelGrade.CB240T);

        var result = TcvnBeamShearCalculator.Calculate(
            b: 250.0, h: 500.0, shearKn: 140.0, concrete: concrete, stirrupSteel: steel,
            stirrupDiameter: 8.0, legs: 2, a: 40.0, isSupportZone: true);

        Assert.True(result.IsCompliant);
        Assert.True(result.RequiresStirrupByCalculation);
        Assert.True(result.CalculatedSpacing > 0.0);
        Assert.InRange(result.RecommendedSpacing, 50.0, 150.0);
    }

    [Theory]
    [InlineData(3, 18.0, 250.0, 25.0, 8.0, false, 65.0, true)]
    [InlineData(4, 25.0, 250.0, 25.0, 8.0, false, 28.0, true)]
    [InlineData(5, 25.0, 250.0, 25.0, 8.0, false, 14.75, false)]
    public void TC_T1_SPC_01_ClearSpacingCheck_EnforcesClause103(
        int barCount, double barDiameter, double b, double cover, double stirrupD,
        bool isTop, double expectedSpacing, bool expectedCompliant)
    {
        var (spacing, isCompliant, _) = TcvnBeamRebarOptimizer.ValidateClearSpacing(
            barCount, barDiameter, b, cover, stirrupD, isTop);

        Assert.Equal(expectedSpacing, spacing, 1);
        Assert.Equal(expectedCompliant, isCompliant);
    }

    [Fact]
    public void TC_T1_OPT_01_OptimizerSelectsValidCandidateMeetingAreaAndSpacing()
    {
        // AsRequired = 780 mm2, b = 250, h = 500
        var option = TcvnBeamRebarOptimizer.Optimize(
            asRequired: 780.0, b: 250.0, h: 500.0, cover: 25.0, stirrupDiameter: 8.0, isTop: false);

        Assert.True(option.IsValid);
        Assert.True(option.IsSpacingCompliant);
        Assert.True(option.ProvidedArea >= 780.0);
        Assert.InRange(option.ClearSpacingLayer1, 25.0, 150.0);
    }

    [Fact]
    public void TC_T1_GEO_01_BeamStirrupDistribution_SegmentsSupportAndMidspanZones()
    {
        var spec = new BeamStirrupSpec(
            layout: StirrupLayout.ThreeZoneL4,
            diameter: 8.0,
            cover: 25.0,
            spacingDense: 100.0,
            spacingSparse: 200.0);

        var runs = BeamStirrupDistributionCalculator.ComputeSpanRuns(clearSpanMm: 6000.0, spec: spec);

        Assert.NotEmpty(runs);
        Assert.Equal(3, runs.Count); // Support Left, Midspan, Support Right
        Assert.Equal(100.0, runs[0].Spacing);
        Assert.Equal(200.0, runs[1].Spacing);
        Assert.Equal(100.0, runs[2].Spacing);
    }

    [Fact]
    public void TC_T1_GEO_02_BeamSideBarCalculator_GeneratesSkinBarsForDeepBeams()
    {
        Assert.True(BeamSideBarCalculator.RequiresSideBars(800.0));
        Assert.False(BeamSideBarCalculator.RequiresSideBars(600.0));

        int rows = BeamSideBarCalculator.ComputeRowCount(heightMm: 800.0);
        Assert.True(rows >= 1);

        var span = new BeamSpan(0, "Span 1", 6000.0, 300.0, 800.0, 0.0, 25.0, 5600.0);
        var stack = new BeamContinuousStack(new[] { span }, Array.Empty<BeamSupportNode>());
        var sideSpec = new BeamSideBarSpec(autoSkinBars: true, depthThreshold: 700.0, diameter: 12.0);

        var skinBars = BeamSideBarCalculator.ComputeLongitudinalSideBars(stack, sideSpec, 8.0, 20.0);

        Assert.NotNull(skinBars);
        Assert.NotEmpty(skinBars);
        Assert.All(skinBars, bar => Assert.Equal(BarType.SideSkin, bar.Type));
    }
}
