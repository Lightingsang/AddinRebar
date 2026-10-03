using System;
using HPRebar.Core.FoundationRebar.Calculators;
using HPRebar.Core.FoundationRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.FoundationRebar;

public sealed class FoundationBoundaryCalculatorTests
{
    private const double Tolerance = 1.0e-6;

    [Fact]
    public void Calculate_WithStandardDimensions_ReturnsExpectedBoundsAndEffectiveSpans()
    {
        // Arrange: 3000 x 2000 mm slab with 50 mm side cover
        double length = 3000.0;
        double width = 2000.0;
        double coverSide = 50.0;

        // Act
        var boundary = FoundationBoundaryCalculator.Calculate(length, width, coverSide);

        // Assert: [c_side, L - c_side] x [c_side, W - c_side]
        Assert.Equal(50.0, boundary.XMin, Tolerance);
        Assert.Equal(2950.0, boundary.XMax, Tolerance);
        Assert.Equal(50.0, boundary.YMin, Tolerance);
        Assert.Equal(1950.0, boundary.YMax, Tolerance);

        // Effective spans: L_eff = L - 2*c_side, W_eff = W - 2*c_side
        Assert.Equal(2900.0, boundary.EffectiveLength, Tolerance);
        Assert.Equal(1900.0, boundary.EffectiveWidth, Tolerance);
    }

    [Fact]
    public void Calculate_WithSnapshot_MatchesDirectDimensions()
    {
        // Arrange
        var snapshot = FoundationTestData.StandardSnapshot(length: 4500.0, width: 3200.0, thickness: 600.0);
        double coverSide = 60.0;

        // Act
        var boundary = FoundationBoundaryCalculator.Calculate(snapshot, coverSide);

        // Assert
        Assert.Equal(60.0, boundary.XMin, Tolerance);
        Assert.Equal(4440.0, boundary.XMax, Tolerance);
        Assert.Equal(60.0, boundary.YMin, Tolerance);
        Assert.Equal(3140.0, boundary.YMax, Tolerance);
        Assert.Equal(4380.0, boundary.EffectiveLength, Tolerance);
        Assert.Equal(3080.0, boundary.EffectiveWidth, Tolerance);
    }

    [Theory]
    [InlineData(100.0, 50.0, 0.0)]   // L == 2 * c_side -> eff = 0
    [InlineData(80.0, 50.0, 0.0)]    // L < 2 * c_side -> clamped to 0
    [InlineData(150.0, 50.0, 50.0)]  // L > 2 * c_side -> eff = 50
    public void Calculate_WhenDimensionLessThanOrEqualToTwiceCover_ClampsEffectiveLengthSafely(
        double length, double coverSide, double expectedEffLength)
    {
        // Arrange & Act
        var boundary = FoundationBoundaryCalculator.Calculate(length, 1000.0, coverSide);

        // Assert
        Assert.Equal(expectedEffLength, boundary.EffectiveLength, Tolerance);
    }

    [Theory]
    [InlineData(100.0, 50.0, 0.0)]   // W == 2 * c_side -> eff = 0
    [InlineData(60.0, 50.0, 0.0)]    // W < 2 * c_side -> clamped to 0
    [InlineData(200.0, 50.0, 100.0)] // W > 2 * c_side -> eff = 100
    public void Calculate_WhenWidthLessThanOrEqualToTwiceCover_ClampsEffectiveWidthSafely(
        double width, double coverSide, double expectedEffWidth)
    {
        // Arrange & Act
        var boundary = FoundationBoundaryCalculator.Calculate(1000.0, width, coverSide);

        // Assert
        Assert.Equal(expectedEffWidth, boundary.EffectiveWidth, Tolerance);
    }

    [Fact]
    public void Calculate_NullSnapshot_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => FoundationBoundaryCalculator.Calculate(null!, 50.0));
    }
}
