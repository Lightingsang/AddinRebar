using System;
using HPRebar.Core.Tcvn5574.Materials;
using Xunit;

namespace HPRebar.Core.Tests.Tcvn5574;

public sealed class SteelMaterialTests
{
    [Theory]
    [InlineData(SteelGrade.CB240T, 210.0, 210.0, 170.0)]
    [InlineData(SteelGrade.CB300V, 260.0, 260.0, 210.0)]
    [InlineData(SteelGrade.CB400V, 350.0, 350.0, 280.0)]
    [InlineData(SteelGrade.CB500V, 435.0, 400.0, 300.0)] // Rsc capped at 400 MPa per Clause 8.1.2.3.1
    public void SteelMaterial_MatchesTcvn5574Tables13_14_15(
        SteelGrade grade, double expectedRs, double expectedRsc, double expectedRsw)
    {
        var mat = SteelMaterial.Create(grade);

        Assert.Equal(expectedRs, mat.Rs, precision: 4);
        Assert.Equal(expectedRsc, mat.Rsc, precision: 4);
        Assert.Equal(expectedRsw, mat.Rsw, precision: 4);
        Assert.Equal(200000.0, mat.Es, precision: 4);
    }

    [Fact]
    public void SteelMaterial_CB400V_MatchesBoundaryValues()
    {
        var mat = SteelMaterial.Create(SteelGrade.CB400V);

        // xi_R = 0.8 / (1 + 350/700) = 8/15 = 0.5333...
        Assert.Equal(8.0 / 15.0, mat.XiR, precision: 4);

        // alpha_R = (8/15) * (1 - 4/15) = 88/225 = 0.3911...
        Assert.Equal(88.0 / 225.0, mat.AlphaR, precision: 4);
    }

    [Fact]
    public void SteelMaterial_CB240T_MatchesBoundaryValues()
    {
        var mat = SteelMaterial.Create(SteelGrade.CB240T);

        // xi_R = 0.8 / (1 + 210/700) = 8/13 = 0.61538...
        Assert.Equal(8.0 / 13.0, mat.XiR, precision: 4);
        Assert.Equal(72.0 / 169.0, mat.AlphaR, precision: 4);
    }

    [Fact]
    public void SteelMaterial_CB300V_MatchesBoundaryValues()
    {
        var mat = SteelMaterial.Create(SteelGrade.CB300V);

        // xi_R = 0.8 / (1 + 260/700) = 7/12 = 0.5833...
        Assert.Equal(7.0 / 12.0, mat.XiR, precision: 4);
        Assert.Equal(119.0 / 288.0, mat.AlphaR, precision: 4);
    }

    [Fact]
    public void SteelMaterial_CB500V_MatchesBoundaryValues()
    {
        var mat = SteelMaterial.Create(SteelGrade.CB500V);

        // xi_R = 0.8 / (1 + 435/700) = 112/227 = 0.49339...
        Assert.Equal(112.0 / 227.0, mat.XiR, precision: 4);
        Assert.Equal(0.3717, mat.AlphaR, precision: 3);
    }

    [Theory]
    [InlineData("CB240-T", SteelGrade.CB240T)]
    [InlineData("cb240t", SteelGrade.CB240T)]
    [InlineData("CB300-V", SteelGrade.CB300V)]
    [InlineData("CB300V", SteelGrade.CB300V)]
    [InlineData("CB400-V", SteelGrade.CB400V)]
    [InlineData("cb400", SteelGrade.CB400V)]
    [InlineData("CB500-V", SteelGrade.CB500V)]
    [InlineData("CB500V", SteelGrade.CB500V)]
    public void MaterialGradeParser_ParsesValidSteelStrings(string input, SteelGrade expectedGrade)
    {
        bool success = MaterialGradeParser.TryParseSteel(input, out var grade);

        Assert.True(success);
        Assert.Equal(expectedGrade, grade);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("UnknownSteel")]
    public void MaterialGradeParser_ReturnsFalseForInvalidSteel(string? input)
    {
        bool success = MaterialGradeParser.TryParseSteel(input, out _);

        Assert.False(success);
    }

    [Fact]
    public void Create_ThrowsForInvalidGrade()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SteelMaterial.Create((SteelGrade)999));
    }
}
