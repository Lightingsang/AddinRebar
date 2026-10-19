using System;
using HPRebar.Core.Tcvn5574.Calculators;
using HPRebar.Core.Tcvn5574.Materials;
using Xunit;

namespace HPRebar.Core.Tests.Tcvn5574;

public sealed class TcvnBeamFlexureCalculatorTests
{
    private readonly ConcreteMaterial _concreteB25 = ConcreteMaterial.Create(ConcreteGrade.B25);
    private readonly SteelMaterial _steelCb400 = SteelMaterial.Create(SteelGrade.CB400V);

    [Fact]
    public void FlexuralDesign_SinglyReinforced_CalculatesAccurateAs()
    {
        // Beam 300x600, a = 40 mm => h0 = 560 mm, B25 (Rb = 14.5 MPa), CB400-V (Rs = 350 MPa)
        // M = 150 kN.m => alpha_m = 150e6 / (14.5 * 300 * 560^2) = 0.1099 < alpha_R (0.3911)
        double b = 300.0;
        double h = 600.0;
        double momentKnm = 150.0;

        var result = TcvnBeamFlexureCalculator.Calculate(
            b, h, momentKnm, _concreteB25, _steelCb400, a: 40.0, aPrime: 40.0);

        Assert.Equal(560.0, result.EffectiveDepthH0);
        Assert.False(result.IsDoublyReinforced);
        Assert.True(result.IsCompliant);
        Assert.False(result.IsOverReinforced);
        Assert.Equal(0.0, result.AsCompressionRequired);

        // Theoretical As = 812.7 mm2 (within +/- 2 mm2)
        Assert.InRange(result.AsRequired, 810.0, 815.0);

        // Reinforcement ratio: mu = As / (b * h0) * 100%
        double expectedMu = (result.AsRequired / (b * 560.0)) * 100.0;
        Assert.Equal(expectedMu, result.MuPercent, precision: 2);
    }

    [Fact]
    public void FlexuralDesign_DoublyReinforced_WhenAlphaMExceedsAlphaR_CalculatesCompressionSteel()
    {
        // Beam 300x600, h0 = 560 mm, B25, CB400-V
        // For M = 600 kN.m => alpha_m = 600e6 / (14.5 * 300 * 560^2) = 0.4396 > alpha_R (0.3911)
        double b = 300.0;
        double h = 600.0;
        double momentKnm = 600.0;

        var result = TcvnBeamFlexureCalculator.Calculate(
            b, h, momentKnm, _concreteB25, _steelCb400, a: 40.0, aPrime: 40.0);

        Assert.True(result.IsDoublyReinforced);
        Assert.True(result.AlphaM > _steelCb400.AlphaR);
        Assert.True(result.AsCompressionRequired > 0.0);
        Assert.True(result.AsRequired > result.AsCompressionRequired);
    }

    [Fact]
    public void FlexuralDesign_ZeroMoment_AppliesMinimumReinforcement()
    {
        double b = 300.0;
        double h = 600.0;

        var result = TcvnBeamFlexureCalculator.Calculate(
            b, h, 0.0, _concreteB25, _steelCb400, a: 40.0);

        double expectedAsMin = 0.001 * b * 560.0; // mu_min = 0.1% => 168 mm2
        Assert.Equal(expectedAsMin, result.AsRequired, precision: 2);
        Assert.Equal(0.1, result.MuPercent, precision: 2);
        Assert.False(result.IsDoublyReinforced);
        Assert.True(result.IsCompliant);
    }

    [Fact]
    public void FlexuralDesign_NegativeMoment_UsesAbsoluteValue()
    {
        double b = 300.0;
        double h = 600.0;

        var positiveResult = TcvnBeamFlexureCalculator.Calculate(b, h, 150.0, _concreteB25, _steelCb400);
        var negativeResult = TcvnBeamFlexureCalculator.Calculate(b, h, -150.0, _concreteB25, _steelCb400);

        Assert.Equal(positiveResult.AsRequired, negativeResult.AsRequired, precision: 2);
        Assert.Equal(positiveResult.AlphaM, negativeResult.AlphaM, precision: 4);
    }

    [Fact]
    public void FlexuralDesign_ExtremeMoment_FlagsOverReinforced()
    {
        // Massive moment on small beam
        double b = 200.0;
        double h = 300.0;
        double momentKnm = 400.0;

        var result = TcvnBeamFlexureCalculator.Calculate(b, h, momentKnm, _concreteB25, _steelCb400);

        Assert.True(result.IsOverReinforced);
        Assert.False(result.IsCompliant);
    }

    [Theory]
    [InlineData(-100.0, 600.0, 40.0)]
    [InlineData(300.0, -600.0, 40.0)]
    [InlineData(300.0, 600.0, 700.0)] // a >= h
    [InlineData(300.0, 600.0, -10.0)] // a <= 0
    public void FlexuralDesign_InvalidDimensions_ThrowsArgumentOutOfRangeException(double b, double h, double a)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TcvnBeamFlexureCalculator.Calculate(b, h, 100.0, _concreteB25, _steelCb400, a));
    }

    [Fact]
    public void FlexuralDesign_NullMaterials_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            TcvnBeamFlexureCalculator.Calculate(300.0, 600.0, 100.0, null!, _steelCb400));
        Assert.Throws<ArgumentNullException>(() =>
            TcvnBeamFlexureCalculator.Calculate(300.0, 600.0, 100.0, _concreteB25, null!));
    }

    [Theory]
    [InlineData(70.0, 40.0, 40.0)]  // h < a + a' (70 < 80)
    [InlineData(80.0, 40.0, 40.0)]  // h == a + a' (80 == 80)
    [InlineData(50.0, 25.0, 25.0)]  // h == a + a' (50 == 50)
    public void FlexuralDesign_HeightLessOrEqualToSumOfCentroidDistances_ThrowsArgumentOutOfRangeException(
        double h, double a, double aPrime)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            TcvnBeamFlexureCalculator.Calculate(
                b: 200.0, h: h, momentKnm: 20.0, concrete: _concreteB25, steel: _steelCb400, a: a, aPrime: aPrime));

        Assert.Equal("h", ex.ParamName);
        Assert.Contains("a + aPrime", ex.Message);
    }

    [Fact]
    public void FlexuralDesign_HeightGreaterThanSumOfCentroidDistances_Succeeds()
    {
        // Boundary case: h = 81 mm > 40 + 40 = 80 mm
        var result = TcvnBeamFlexureCalculator.Calculate(
            b: 200.0, h: 81.0, momentKnm: 2.0, concrete: _concreteB25, steel: _steelCb400, a: 40.0, aPrime: 40.0);

        Assert.NotNull(result);
        Assert.Equal(41.0, result.EffectiveDepthH0);
    }
}
