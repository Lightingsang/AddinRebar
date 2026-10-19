using HPKata.Revit.Units;
using Xunit;

namespace HPKata.Revit.Tests.Units;

public sealed class RevitUnitConverterTests
{
    [Fact]
    public void FeetToMm_Scalar_ConvertsAccurately()
    {
        double feet = 1.0;
        double mm = RevitUnitConverter.FeetToMm(feet);
        Assert.Equal(304.8, mm, 4);

        double tenFeet = 10.0;
        Assert.Equal(3048.0, RevitUnitConverter.FeetToMm(tenFeet), 4);
    }

    [Fact]
    public void MmToFeet_Scalar_ConvertsAccurately()
    {
        double mm = 304.8;
        double feet = RevitUnitConverter.MmToFeet(mm);
        Assert.Equal(1.0, feet, 6);
    }

    [Fact]
    public void AreCloseMm_WithinTolerance_ReturnsTrue()
    {
        double a = 500.0;
        double b = 500.3;
        Assert.True(RevitUnitConverter.AreCloseMm(a, b, 0.5));
    }

    [Fact]
    public void AreCloseMm_ExceedingTolerance_ReturnsFalse()
    {
        double a = 500.0;
        double b = 500.9;
        Assert.False(RevitUnitConverter.AreCloseMm(a, b, 0.5));
    }

    [Fact]
    public void AreCloseFeet_Tolerance_MatchesFeetEquivalence()
    {
        double a = 1.0;
        double b = 1.0 + (0.2 / 304.8);
        Assert.True(RevitUnitConverter.AreCloseFeet(a, b));
    }

    [Fact]
    public void FeetToMm_Array_ConvertsAllElements()
    {
        double[] feet = [1.0, 2.0, 5.0];
        double[] mm = RevitUnitConverter.FeetToMm(feet);

        Assert.Equal(3, mm.Length);
        Assert.Equal(304.8, mm[0], 4);
        Assert.Equal(609.6, mm[1], 4);
        Assert.Equal(1524.0, mm[2], 4);
    }

    [Fact]
    public void Point_FeetAndMm_RoundtripsPreservingCoordinates()
    {
        double origX = 1500.0;
        double origY = 3000.0;
        double origZ = -500.0;

        var (xFt, yFt, zFt) = RevitUnitConverter.CoordinatesMmToFeet(origX, origY, origZ);
        var (xMm, yMm, zMm) = RevitUnitConverter.CoordinatesFeetToMm(xFt, yFt, zFt);

        Assert.True(RevitUnitConverter.AreCloseMm(origX, xMm));
        Assert.True(RevitUnitConverter.AreCloseMm(origY, yMm));
        Assert.True(RevitUnitConverter.AreCloseMm(origZ, zMm));
    }
}
