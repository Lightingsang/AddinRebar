using System;
using HPRebar.Core.Tcvn5574.Materials;
using Xunit;

namespace HPRebar.Core.Tests.Tcvn5574;

public sealed class ConcreteMaterialTests
{
    [Theory]
    [InlineData(ConcreteGrade.B15, 8.5, 0.75, 24000.0)]
    [InlineData(ConcreteGrade.B20, 11.5, 0.90, 27500.0)]
    [InlineData(ConcreteGrade.B25, 14.5, 1.05, 30000.0)]
    [InlineData(ConcreteGrade.B30, 17.0, 1.15, 32500.0)]
    [InlineData(ConcreteGrade.B35, 19.5, 1.30, 34500.0)]
    [InlineData(ConcreteGrade.B40, 22.0, 1.40, 36000.0)]
    public void ConcreteMaterial_MatchesTcvn5574Tables6_7_10(
        ConcreteGrade grade, double expectedRb, double expectedRbt, double expectedEb)
    {
        var mat = ConcreteMaterial.Create(grade);

        Assert.Equal(expectedRb, mat.Rb, precision: 4);
        Assert.Equal(expectedRbt, mat.Rbt, precision: 4);
        Assert.Equal(expectedEb, mat.Eb, precision: 4);
    }

    [Fact]
    public void DefaultB25_HasExpectedStandardProperties()
    {
        var mat = ConcreteMaterial.DefaultB25;

        Assert.Equal(ConcreteGrade.B25, mat.Grade);
        Assert.Equal(14.5, mat.Rb);
        Assert.Equal(1.05, mat.Rbt);
        Assert.Equal(30000.0, mat.Eb);
        Assert.Equal("B25", mat.Name);
    }

    [Theory]
    [InlineData("B15", ConcreteGrade.B15)]
    [InlineData("b20", ConcreteGrade.B20)]
    [InlineData("B25", ConcreteGrade.B25)]
    [InlineData("M350", ConcreteGrade.B25)]
    [InlineData("B30", ConcreteGrade.B30)]
    [InlineData("M400", ConcreteGrade.B30)]
    [InlineData("B35", ConcreteGrade.B35)]
    [InlineData("B40", ConcreteGrade.B40)]
    public void MaterialGradeParser_ParsesValidConcreteStrings(string input, ConcreteGrade expectedGrade)
    {
        bool success = MaterialGradeParser.TryParseConcrete(input, out var grade);

        Assert.True(success);
        Assert.Equal(expectedGrade, grade);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("InvalidGrade")]
    [InlineData("B999")]
    public void MaterialGradeParser_ReturnsFalseForInvalidConcrete(string? input)
    {
        bool success = MaterialGradeParser.TryParseConcrete(input, out _);

        Assert.False(success);
    }

    [Fact]
    public void Create_ThrowsForInvalidGrade()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ConcreteMaterial.Create((ConcreteGrade)999));
    }
}
