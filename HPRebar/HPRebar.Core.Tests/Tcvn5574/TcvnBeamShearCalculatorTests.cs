using System;
using HPRebar.Core.Tcvn5574.Calculators;
using HPRebar.Core.Tcvn5574.Materials;
using Xunit;

namespace HPRebar.Core.Tests.Tcvn5574;

public sealed class TcvnBeamShearCalculatorTests
{
    private readonly ConcreteMaterial _concreteB25 = ConcreteMaterial.Create(ConcreteGrade.B25);
    private readonly SteelMaterial _stirrupCb240 = SteelMaterial.Create(SteelGrade.CB240T);

    [Fact]
    public void ShearDesign_WebCrushLimit_FailsWhenShearExceedsCrushLimit()
    {
        // Beam 300x600, a = 40 mm => h0 = 560 mm, B25 (Rb = 14.5 MPa)
        // Qb,max = 0.3 * 14.5 * 300 * 560 = 730,800 N = 730.8 kN
        // For Q = 800 kN > 730.8 kN => Concrete web crush failure!
        double b = 300.0;
        double h = 600.0;
        double shearKn = 800.0;

        var result = TcvnBeamShearCalculator.Calculate(
            b, h, shearKn, _concreteB25, _stirrupCb240, stirrupDiameter: 8.0, legs: 2);

        Assert.True(result.IsWebCrushExceeded);
        Assert.False(result.IsCompliant);
        Assert.Equal(730.8, result.QbMaxKn, precision: 1);
        Assert.Contains("diagonal web crushing", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShearDesign_ConcreteAloneResistsShear_WhenQBelowQbMin()
    {
        // Beam 300x600, h0 = 560 mm, B25 (Rbt = 1.05 MPa)
        // Qb,min = 0.5 * 1.05 * 300 * 560 = 88,200 N = 88.2 kN
        // For Q = 50 kN < 88.2 kN => Stirrups not required by calculation
        double b = 300.0;
        double h = 600.0;
        double shearKn = 50.0;

        var result = TcvnBeamShearCalculator.Calculate(
            b, h, shearKn, _concreteB25, _stirrupCb240, stirrupDiameter: 8.0, legs: 2, isSupportZone: true);

        Assert.False(result.IsWebCrushExceeded);
        Assert.False(result.RequiresStirrupByCalculation);
        Assert.True(result.IsCompliant);
        Assert.Equal(88.2, result.QbMinKn, precision: 1);

        // For h > 450 mm in support zone, constructive spacing = 150 mm
        Assert.Equal(150.0, result.ConstructiveSpacing);
        Assert.Equal(150.0, result.RecommendedSpacing);
    }

    [Fact]
    public void ShearDesign_ModerateShear_CalculatesValidStirrupSpacing()
    {
        // For Q = 250 kN (between 88.2 kN and 730.8 kN)
        double b = 300.0;
        double h = 600.0;
        double shearKn = 250.0;

        var result = TcvnBeamShearCalculator.Calculate(
            b, h, shearKn, _concreteB25, _stirrupCb240, stirrupDiameter: 8.0, legs: 2, isSupportZone: true);

        Assert.False(result.IsWebCrushExceeded);
        Assert.True(result.RequiresStirrupByCalculation);
        Assert.True(result.IsCompliant);
        Assert.True(result.CalculatedSpacing > 0.0);
        Assert.True(result.RecommendedSpacing <= result.CalculatedSpacing);
        Assert.True(result.RecommendedSpacing <= result.ConstructiveSpacing);
        Assert.True(result.RecommendedSpacing <= result.MaxSpacing);
    }

    [Fact]
    public void ShearDesign_SupportZoneVsMidspan_AppliesAppropriateConstructiveLimit()
    {
        double b = 300.0;
        double h = 600.0;
        double lowShear = 30.0;

        var supportResult = TcvnBeamShearCalculator.Calculate(
            b, h, lowShear, _concreteB25, _stirrupCb240, isSupportZone: true);
        var midspanResult = TcvnBeamShearCalculator.Calculate(
            b, h, lowShear, _concreteB25, _stirrupCb240, isSupportZone: false);

        // Support zone: 150 mm
        Assert.Equal(150.0, supportResult.ConstructiveSpacing);

        // Midspan zone: min(3h/4, 500) = min(450, 500) = 450 mm
        Assert.Equal(450.0, midspanResult.ConstructiveSpacing);
    }

    [Theory]
    [InlineData(-300.0, 600.0)]
    [InlineData(300.0, -600.0)]
    public void ShearDesign_InvalidDimensions_ThrowsArgumentOutOfRangeException(double b, double h)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TcvnBeamShearCalculator.Calculate(b, h, 100.0, _concreteB25, _stirrupCb240));
    }

    [Fact]
    public void ShearDesign_NullMaterials_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            TcvnBeamShearCalculator.Calculate(300.0, 600.0, 100.0, null!, _stirrupCb240));
        Assert.Throws<ArgumentNullException>(() =>
            TcvnBeamShearCalculator.Calculate(300.0, 600.0, 100.0, _concreteB25, null!));
    }

    [Fact]
    public void ShearDesign_HighShearWithDeficientStirrup_FlagsNonCompliantWithWarningMessage()
    {
        // Beam 300x600, B25, Q = 700 kN with 2-leg d8 stirrups (s_calc ~ 20.7 mm < 50 mm)
        double b = 300.0;
        double h = 600.0;
        double highShearKn = 700.0;

        var result = TcvnBeamShearCalculator.Calculate(
            b, h, highShearKn, _concreteB25, _stirrupCb240, stirrupDiameter: 8.0, legs: 2);

        Assert.False(result.IsWebCrushExceeded);
        Assert.False(result.IsCompliant);
        Assert.True(result.CalculatedSpacing < 50.0);
        Assert.Equal(50.0, result.RecommendedSpacing);
        Assert.Contains("Stirrups inadequate", result.Message);
        Assert.Contains("< 50mm minimum", result.Message);
    }

    [Fact]
    public void ShearDesign_HighShearWithAdequateStirrup_FlagsCompliant()
    {
        // Upgrading to 4-leg d12 stirrups provides adequate capacity (s_calc >= 50 mm)
        double b = 300.0;
        double h = 600.0;
        double highShearKn = 700.0;

        var result = TcvnBeamShearCalculator.Calculate(
            b, h, highShearKn, _concreteB25, _stirrupCb240, stirrupDiameter: 12.0, legs: 4);

        Assert.False(result.IsWebCrushExceeded);
        Assert.True(result.IsCompliant);
        Assert.True(result.CalculatedSpacing >= 50.0);
        Assert.True(result.CalculatedSpacing >= result.RecommendedSpacing);
        Assert.Contains("satisfies TCVN 5574:2018", result.Message);
    }
}
