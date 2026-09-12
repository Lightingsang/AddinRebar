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

    [Fact]
    public void ComputeEffectiveBoundary_ReturnsCorrectTupleCoordinates()
    {
        // Arrange
        var snapshot = FoundationTestData.StandardSnapshot(length: 2500.0, width: 1800.0);
        double coverSide = 40.0;

        // Act
        var (xMin, xMax, yMin, yMax) = FoundationBoundaryCalculator.ComputeEffectiveBoundary(snapshot, coverSide);

        // Assert
        Assert.Equal(40.0, xMin, Tolerance);
        Assert.Equal(2460.0, xMax, Tolerance);
        Assert.Equal(40.0, yMin, Tolerance);
        Assert.Equal(1760.0, yMax, Tolerance);
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
    public void ValidateBoundary_WithValidParameters_ReturnsTrueAndNullError()
    {
        // Act
        var (isValid, errorMessage) = FoundationBoundaryCalculator.ValidateBoundary(3000.0, 2000.0, 50.0);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void ValidateBoundary_WithSnapshot_ReturnsTrueForValidSnapshot()
    {
        // Arrange
        var snapshot = FoundationTestData.StandardSnapshot();

        // Act
        var (isValid, errorMessage) = FoundationBoundaryCalculator.ValidateBoundary(snapshot, 50.0);

        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(-50.0)]
    public void ValidateBoundary_WhenSideCoverIsNegative_ReturnsFalseWithAppropriateError(double negativeCover)
    {
        // Act
        var (isValid, errorMessage) = FoundationBoundaryCalculator.ValidateBoundary(3000.0, 2000.0, negativeCover);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("negative", errorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(100.0, 50.0)]  // L == 2 * cover
    [InlineData(90.0, 50.0)]   // L < 2 * cover
    public void ValidateBoundary_WhenLengthLessThanOrEqualToTwiceSideCover_ReturnsFalse(double length, double coverSide)
    {
        // Act
        var (isValid, errorMessage) = FoundationBoundaryCalculator.ValidateBoundary(length, 2000.0, coverSide);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("Length", errorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(100.0, 50.0)]  // W == 2 * cover
    [InlineData(70.0, 50.0)]   // W < 2 * cover
    public void ValidateBoundary_WhenWidthLessThanOrEqualToTwiceSideCover_ReturnsFalse(double width, double coverSide)
    {
        // Act
        var (isValid, errorMessage) = FoundationBoundaryCalculator.ValidateBoundary(3000.0, width, coverSide);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("Width", errorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Calculate_NullSnapshot_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => FoundationBoundaryCalculator.Calculate(null!, 50.0));
    }

    [Fact]
    public void ComputeEffectiveBoundary_NullSnapshot_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => FoundationBoundaryCalculator.ComputeEffectiveBoundary(null!, 50.0));
    }

    [Fact]
    public void ValidateBoundary_NullSnapshot_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => FoundationBoundaryCalculator.ValidateBoundary(null!, 50.0));
    }
}
