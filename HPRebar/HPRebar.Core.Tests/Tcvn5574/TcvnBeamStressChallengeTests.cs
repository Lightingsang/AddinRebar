using System;
using HPRebar.Core.Tcvn5574.Calculators;
using HPRebar.Core.Tcvn5574.Materials;
using HPRebar.Core.Tcvn5574.Models;
using Xunit;

namespace HPRebar.Core.Tests.Tcvn5574;

/// <summary>
/// Empirical adversarial stress tests challenging Milestone M4 calculation algorithms:
/// 1. Extreme bending moments (zero moment minimum steel, balanced moment boundary, enormous moments alpha_m >> alpha_R).
/// 2. Shear near web crush limit (Q = 0.3 * Rb * b * h0 exact boundary, crack limit, and high shear stirrup deficiency).
/// 3. Crowded rebar spacing (t >= max(25, d) bottom, max(30, d) top, negative spacing, optimizer fallback).
/// </summary>
public sealed class TcvnBeamStressChallengeTests
{
    private readonly ConcreteMaterial _concreteB25 = ConcreteMaterial.Create(ConcreteGrade.B25);
    private readonly ConcreteMaterial _concreteB30 = ConcreteMaterial.Create(ConcreteGrade.B30);
    private readonly SteelMaterial _steelCb400 = SteelMaterial.Create(SteelGrade.CB400V);
    private readonly SteelMaterial _stirrupCb240 = SteelMaterial.Create(SteelGrade.CB240T);

    // =========================================================================
    // 1. EXTREME BENDING MOMENTS CHALLENGE
    // =========================================================================

    [Theory]
    [InlineData(200.0, 300.0, 35.0)]
    [InlineData(300.0, 600.0, 40.0)]
    [InlineData(400.0, 800.0, 50.0)]
    [InlineData(500.0, 1000.0, 60.0)]
    public void Flexure_ZeroMoment_EnforcesMinimumSteelRatioAcrossAllSections(double b, double h, double a)
    {
        // TCVN 5574:2018 Clause 10.3.1.1: mu_min = 0.1% = 0.001
        double h0 = h - a;
        double expectedAsMin = 0.001 * b * h0;

        var result = TcvnBeamFlexureCalculator.Calculate(b, h, 0.0, _concreteB25, _steelCb400, a);

        Assert.Equal(expectedAsMin, result.AsRequired, precision: 4);
        Assert.Equal(0.1, result.MuPercent, precision: 4);
        Assert.Equal(0.0, result.AsCompressionRequired);
        Assert.False(result.IsDoublyReinforced);
        Assert.True(result.IsCompliant);
        Assert.False(result.IsOverReinforced);
        Assert.Contains("Zero moment", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(1e-12)]
    [InlineData(1e-9)]
    [InlineData(1e-6)]
    [InlineData(1e-3)]
    public void Flexure_InfinitesimalMoment_EnforcesMinimumSteelWithoutCrash(double smallMomentKnm)
    {
        double b = 300.0;
        double h = 600.0;
        double a = 40.0;
        double h0 = h - a;
        double expectedAsMin = 0.001 * b * h0;

        var result = TcvnBeamFlexureCalculator.Calculate(b, h, smallMomentKnm, _concreteB25, _steelCb400, a);

        Assert.True(result.AsRequired >= expectedAsMin);
        Assert.False(double.IsNaN(result.AsRequired));
        Assert.False(double.IsInfinity(result.AsRequired));
        Assert.True(result.IsCompliant);
    }

    [Theory]
    [InlineData(50.0)]
    [InlineData(150.0)]
    [InlineData(400.0)]
    [InlineData(1200.0)]
    public void Flexure_NegativeMoment_ProducesIdenticalResultsToPositive(double momentMagnitudeKnm)
    {
        double b = 300.0;
        double h = 600.0;

        var posResult = TcvnBeamFlexureCalculator.Calculate(b, h, momentMagnitudeKnm, _concreteB25, _steelCb400);
        var negResult = TcvnBeamFlexureCalculator.Calculate(b, h, -momentMagnitudeKnm, _concreteB25, _steelCb400);

        Assert.Equal(posResult.AsRequired, negResult.AsRequired, precision: 4);
        Assert.Equal(posResult.AsCompressionRequired, negResult.AsCompressionRequired, precision: 4);
        Assert.Equal(posResult.AlphaM, negResult.AlphaM, precision: 6);
        Assert.Equal(posResult.Xi, negResult.Xi, precision: 6);
        Assert.Equal(posResult.IsDoublyReinforced, negResult.IsDoublyReinforced);
        Assert.Equal(posResult.IsCompliant, negResult.IsCompliant);
    }

    [Fact]
    public void Flexure_BalancedMomentBoundary_SmoothTransitionWithoutDiscontinuity()
    {
        // Boundary: M_R = alpha_R * Rb * b * h0^2
        double b = 300.0;
        double h = 600.0;
        double a = 40.0;
        double h0 = h - a;
        double mbNmm = _steelCb400.AlphaR * _concreteB25.Rb * b * h0 * h0;
        double mbKnm = mbNmm / 1e6;

        // Just below balanced moment: alpha_m <= alpha_R (singly reinforced)
        var resultBelow = TcvnBeamFlexureCalculator.Calculate(b, h, mbKnm - 0.01, _concreteB25, _steelCb400, a);
        Assert.False(resultBelow.IsDoublyReinforced);
        Assert.Equal(0.0, resultBelow.AsCompressionRequired);

        // Just above balanced moment: alpha_m > alpha_R (doubly reinforced)
        var resultAbove = TcvnBeamFlexureCalculator.Calculate(b, h, mbKnm + 0.01, _concreteB25, _steelCb400, a);
        Assert.True(resultAbove.IsDoublyReinforced);
        Assert.True(resultAbove.AsCompressionRequired > 0.0);

        // Tension steel area must be continuous across boundary (within 1 mm2)
        Assert.InRange(Math.Abs(resultAbove.AsRequired - resultBelow.AsRequired), 0.0, 1.0);
    }

    [Theory]
    [InlineData(2_000.0)]
    [InlineData(10_000.0)]
    [InlineData(100_000.0)]
    [InlineData(1_000_000.0)]
    public void Flexure_EnormousMoment_CalculatesDoublyReinforcedWithoutSqrtCrashAndFlagsOverloaded(double enormousMomentKnm)
    {
        double b = 300.0;
        double h = 600.0;

        var result = TcvnBeamFlexureCalculator.Calculate(b, h, enormousMomentKnm, _concreteB25, _steelCb400);

        // Must not crash, no NaNs, no infinities
        Assert.False(double.IsNaN(result.AsRequired));
        Assert.False(double.IsNaN(result.AsCompressionRequired));
        Assert.False(double.IsInfinity(result.AsRequired));
        Assert.False(double.IsInfinity(result.AsCompressionRequired));

        // Must be doubly reinforced and flag overloaded section
        Assert.True(result.IsDoublyReinforced);
        Assert.True(result.IsOverReinforced);
        Assert.False(result.IsCompliant);
        Assert.True(result.MuPercent > 3.0);
        Assert.Contains("Section overloaded", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(70.0, 40.0, 40.0)]
    [InlineData(80.0, 40.0, 40.0)]
    public void Flexure_DegenerateSectionHeight_ThrowsArgumentOutOfRangeException(double h, double a, double aPrime)
    {
        // Challenge finding remediation: h <= a + aPrime produces degenerate depth and negative (h0 - a')
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            TcvnBeamFlexureCalculator.Calculate(
                b: 200.0, h: h, momentKnm: 25.0, concrete: _concreteB25, steel: _steelCb400, a: a, aPrime: aPrime));

        Assert.Equal("h", ex.ParamName);
        Assert.Contains("a + aPrime", ex.Message);
    }

    // =========================================================================
    // 2. SHEAR WEB CRUSH LIMIT & BOUNDARY CONDITIONS CHALLENGE
    // =========================================================================

    [Fact]
    public void Shear_WebCrushBoundary_ExactThresholdPassesJustBelowAndFailsJustAbove()
    {
        // Section 300x600, h0 = 560 mm, B25 (Rb = 14.5 MPa)
        // Qb,max = 0.3 * 14.5 * 300 * 560 = 730,800 N = 730.8 kN
        double b = 300.0;
        double h = 600.0;
        double qbMaxKn = 0.3 * _concreteB25.Rb * b * (h - 40.0) / 1e3; // 730.8 kN

        // Case 1: Just below (730.799 kN) -> Passes web crush
        var resultBelow = TcvnBeamShearCalculator.Calculate(
            b, h, qbMaxKn - 0.001, _concreteB25, _stirrupCb240);
        Assert.False(resultBelow.IsWebCrushExceeded);

        // Case 2: Exactly on boundary (730.8 kN) -> Passes web crush (Q <= Qb,max)
        var resultExact = TcvnBeamShearCalculator.Calculate(
            b, h, qbMaxKn, _concreteB25, _stirrupCb240);
        Assert.False(resultExact.IsWebCrushExceeded);

        // Case 3: Just above (730.801 kN) -> Fails web crush
        var resultAbove = TcvnBeamShearCalculator.Calculate(
            b, h, qbMaxKn + 0.001, _concreteB25, _stirrupCb240);
        Assert.True(resultAbove.IsWebCrushExceeded);
        Assert.False(resultAbove.IsCompliant);
        Assert.Contains("web crushing exceeded", resultAbove.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Shear_ZeroAndNegativeShear_HandledCorrectly()
    {
        double b = 300.0;
        double h = 600.0;

        var zeroResult = TcvnBeamShearCalculator.Calculate(b, h, 0.0, _concreteB25, _stirrupCb240);
        Assert.False(zeroResult.IsWebCrushExceeded);
        Assert.False(zeroResult.RequiresStirrupByCalculation);
        Assert.True(zeroResult.IsCompliant);

        var posResult = TcvnBeamShearCalculator.Calculate(b, h, 200.0, _concreteB25, _stirrupCb240);
        var negResult = TcvnBeamShearCalculator.Calculate(b, h, -200.0, _concreteB25, _stirrupCb240);
        Assert.Equal(posResult.CalculatedSpacing, negResult.CalculatedSpacing, precision: 4);
        Assert.Equal(posResult.RecommendedSpacing, negResult.RecommendedSpacing, precision: 4);
    }

    [Fact]
    public void Shear_TensileCrackBoundary_QbMin_SwitchesToCalculatedStirrup()
    {
        // Qb,min = 0.5 * Rbt * b * h0 = 0.5 * 1.05 * 300 * 560 = 88.2 kN
        double b = 300.0;
        double h = 600.0;
        double qbMinKn = 0.5 * _concreteB25.Rbt * b * (h - 40.0) / 1e3; // 88.2 kN

        var resultBelow = TcvnBeamShearCalculator.Calculate(b, h, qbMinKn - 0.01, _concreteB25, _stirrupCb240);
        Assert.False(resultBelow.RequiresStirrupByCalculation);

        var resultAbove = TcvnBeamShearCalculator.Calculate(b, h, qbMinKn + 0.01, _concreteB25, _stirrupCb240);
        Assert.True(resultAbove.RequiresStirrupByCalculation);
    }

    [Fact]
    public void Shear_HighShearNearWebCrush_RevealsUnderdesignedSpacingVulnerability()
    {
        // CHALLENGE FINDING:
        // When Q is high near web crush limit (Q = 700 kN < 730.8 kN):
        // 2-leg d8 stirrups have Asw = 100.53 mm2.
        // Required spacing s_calc = 20.7 mm.
        // But RoundToPracticalSpacing clamps to 50 mm minimum!
        // At 50 mm spacing, provided shear resistance is only ~41% of required!
        // This test empirically documents that CalculatedSpacing < RecommendedSpacing.
        double b = 300.0;
        double h = 600.0;
        double highShearKn = 700.0;

        var result = TcvnBeamShearCalculator.Calculate(
            b, h, highShearKn, _concreteB25, _stirrupCb240, stirrupDiameter: 8.0, legs: 2);

        // Empirically verify the symptom:
        // Calculated required spacing is smaller than practical 50 mm:
        Assert.True(result.CalculatedSpacing < 50.0,
            $"Expected s_calc < 50 mm under Q=700kN, but was {result.CalculatedSpacing:F1} mm.");

        // But RecommendedSpacing was clamped UP to 50 mm:
        Assert.Equal(50.0, result.RecommendedSpacing);

        // Consequently, RecommendedSpacing EXCEEDS CalculatedSpacing (under-designed stirrups!):
        Assert.True(result.RecommendedSpacing > result.CalculatedSpacing,
            "VULNERABILITY: RecommendedSpacing > CalculatedSpacing implies provided shear capacity < required!");

        // Remediated compliance check: must flag non-compliant and provide clear warning:
        Assert.False(result.IsCompliant, "Remediated engine must flag inadequate stirrup spacing as non-compliant!");
        Assert.Contains("Stirrups inadequate", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================================
    // 3. CROWDED REBAR SPACING CHALLENGE
    // =========================================================================

    [Theory]
    [InlineData(20.0, false, 25.0)] // Bottom bar d20 <= 25 -> threshold = 25 mm
    [InlineData(25.0, false, 25.0)] // Bottom bar d25 == 25 -> threshold = 25 mm
    [InlineData(28.0, false, 28.0)] // Bottom bar d28 > 25  -> threshold = 28 mm (d governs!)
    [InlineData(32.0, false, 32.0)] // Bottom bar d32 > 25  -> threshold = 32 mm (d governs!)
    [InlineData(20.0, true, 30.0)]  // Top bar d20 <= 30    -> threshold = 30 mm
    [InlineData(25.0, true, 30.0)]  // Top bar d25 <= 30    -> threshold = 30 mm
    [InlineData(28.0, true, 30.0)]  // Top bar d28 <= 30    -> threshold = 30 mm
    [InlineData(32.0, true, 32.0)]  // Top bar d32 > 30     -> threshold = 32 mm (d governs!)
    public void Spacing_ThresholdRules_EnforcesMaxBetweenConstructiveAndDiameter(
        double diameter, bool isTop, double expectedThreshold)
    {
        var (_, _, req) = TcvnBeamRebarOptimizer.ValidateClearSpacing(
            barCount: 2, barDiameter: diameter, b: 300.0, cover: 25.0, stirrupDiameter: 8.0, isTop: isTop);

        Assert.Equal(expectedThreshold, req);
    }

    [Fact]
    public void Spacing_TopVsBottomBoundary_SameLayoutCanPassBottomAndFailTop()
    {
        // Width b = 200 mm, cover = 25 mm, stirrup = 8 mm => W_clear = 134 mm.
        // 3 bars of d25: W_bars = 75 mm => t = (134 - 75) / 2 = 29.5 mm.
        // Bottom threshold: max(25, 25) = 25 mm => 29.5 >= 25 => PASS!
        // Top threshold: max(30, 25) = 30 mm => 29.5 < 30 => FAIL!
        double b = 200.0;
        int count = 3;
        double diameter = 25.0;

        var (sBottom, isBottomOk, reqBottom) = TcvnBeamRebarOptimizer.ValidateClearSpacing(
            count, diameter, b, cover: 25.0, stirrupDiameter: 8.0, isTop: false);

        var (sTop, isTopOk, reqTop) = TcvnBeamRebarOptimizer.ValidateClearSpacing(
            count, diameter, b, cover: 25.0, stirrupDiameter: 8.0, isTop: true);

        Assert.Equal(29.5, sBottom, precision: 1);
        Assert.Equal(29.5, sTop, precision: 1);
        Assert.Equal(25.0, reqBottom);
        Assert.Equal(30.0, reqTop);
        Assert.True(isBottomOk, "3d25 in b=200 must pass bottom spacing (29.5 mm >= 25 mm).");
        Assert.False(isTopOk, "3d25 in b=200 must fail top spacing (29.5 mm < 30 mm).");
    }

    [Fact]
    public void Spacing_LargeBarGovernedByDiameter_FailsWhenBetween25AndDiameter()
    {
        // Width b = 200 mm, cover = 25, stirrup = 8 => W_clear = 134 mm.
        // 3 bars of d28: W_bars = 84 mm => t = (134 - 84) / 2 = 25.0 mm.
        // For bottom bar: required = max(25, 28) = 28 mm.
        // Since t = 25.0 mm < 28 mm, it must FAIL even though t >= 25 mm!
        double b = 200.0;
        int count = 3;
        double diameter = 28.0;

        var (spacing, isCompliant, req) = TcvnBeamRebarOptimizer.ValidateClearSpacing(
            count, diameter, b, cover: 25.0, stirrupDiameter: 8.0, isTop: false);

        Assert.Equal(25.0, spacing, precision: 1);
        Assert.Equal(28.0, req);
        Assert.False(isCompliant, "Spacing t=25 mm must fail for d28 bottom bars because required is 28 mm.");
    }

    [Fact]
    public void Spacing_SeverelyOvercrowdedSection_NegativeClearSpacingHandledGracefully()
    {
        // 8 bars of d32 in b = 200 mm => clearWidth = 134 mm, totalBarWidth = 256 mm
        // spacing = (134 - 256) / 7 = -17.4 mm
        double b = 200.0;
        int count = 8;
        double diameter = 32.0;

        var (spacing, isCompliant, _) = TcvnBeamRebarOptimizer.ValidateClearSpacing(
            count, diameter, b, cover: 25.0, stirrupDiameter: 8.0, isTop: false);

        Assert.True(spacing < 0.0);
        Assert.False(isCompliant);
    }

    [Fact]
    public void Optimizer_ExtremeRequiredAreaInNarrowBeam_ReturnsInvalidFallbackOption()
    {
        // Require As = 6000 mm2 in narrow beam b = 200 mm
        double asReq = 6000.0;
        double b = 200.0;
        double h = 600.0;

        var option = TcvnBeamRebarOptimizer.Optimize(asReq, b, h, cover: 25.0, stirrupDiameter: 8.0, isTop: false);

        Assert.False(option.IsValid);
        Assert.False(option.IsSpacingCompliant);
        Assert.Contains("quá dày", option.Description, StringComparison.OrdinalIgnoreCase);
    }
}
