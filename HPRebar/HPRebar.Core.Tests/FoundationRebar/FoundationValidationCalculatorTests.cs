using System;
using HPRebar.Core.FoundationRebar.Calculators;
using HPRebar.Core.FoundationRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.FoundationRebar;

public sealed class FoundationValidationCalculatorTests
{
    [Fact]
    public void Validate_StandardParameters_ReturnsSuccess()
    {
        // Arrange
        var snapshot = FoundationTestData.StandardSnapshot();
        var spec = FoundationTestData.StandardSpec();

        // Act
        var result = FoundationValidationCalculator.Validate(snapshot, spec);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.ErrorMessages);
        Assert.Null(result.ErrorMessage);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-50.0)]
    public void Validate_NonPositiveBottomSpacingX_FailsValidation(double invalidSpacing)
    {
        // Arrange
        var snapshot = FoundationTestData.StandardSnapshot();
        var spec = FoundationTestData.StandardSpec(spacingBottomX: invalidSpacing);

        // Act
        var result = FoundationValidationCalculator.Validate(snapshot, spec);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.ErrorMessages, e => e.Contains("Bottom X spacing", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-100.0)]
    public void Validate_NonPositiveBottomSpacingY_FailsValidation(double invalidSpacing)
    {
        // Arrange
        var snapshot = FoundationTestData.StandardSnapshot();
        var spec = FoundationTestData.StandardSpec(spacingBottomY: invalidSpacing);

        // Act
        var result = FoundationValidationCalculator.Validate(snapshot, spec);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.ErrorMessages, e => e.Contains("Bottom Y spacing", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-200.0)]
    public void Validate_NonPositiveTopSpacing_WhenTopMatEnabled_FailsValidation(double invalidSpacing)
    {
        // Arrange
        var snapshot = FoundationTestData.StandardSnapshot();
        var specX = FoundationTestData.StandardSpec(spacingTopX: invalidSpacing, isTopMatEnabled: true);
        var specY = FoundationTestData.StandardSpec(spacingTopY: invalidSpacing, isTopMatEnabled: true);

        // Act & Assert
        var resultX = FoundationValidationCalculator.Validate(snapshot, specX);
        Assert.False(resultX.IsValid);
        Assert.Contains(resultX.ErrorMessages, e => e.Contains("Top X spacing", StringComparison.OrdinalIgnoreCase));

        var resultY = FoundationValidationCalculator.Validate(snapshot, specY);
        Assert.False(resultY.IsValid);
        Assert.Contains(resultY.ErrorMessages, e => e.Contains("Top Y spacing", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_NonPositiveTopSpacing_WhenTopMatDisabled_Succeeds()
    {
        // Arrange: Top mat disabled -> top spacings are inactive and should not trigger errors
        var snapshot = FoundationTestData.StandardSnapshot();
        var spec = FoundationTestData.StandardSpec(spacingTopX: 0.0, spacingTopY: -50.0, isTopMatEnabled: false);

        // Act
        var result = FoundationValidationCalculator.Validate(snapshot, spec);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-16.0)]
    public void Validate_NonPositiveBottomDiameters_FailsValidation(double invalidDiameter)
    {
        // Arrange
        var snapshot = FoundationTestData.StandardSnapshot();
        var specX = FoundationTestData.StandardSpec(diameterBottomX: invalidDiameter);
        var specY = FoundationTestData.StandardSpec(diameterBottomY: invalidDiameter);

        // Act & Assert
        var resultX = FoundationValidationCalculator.Validate(snapshot, specX);
        Assert.False(resultX.IsValid);
        Assert.Contains(resultX.ErrorMessages, e => e.Contains("Bottom X diameter", StringComparison.OrdinalIgnoreCase));

        var resultY = FoundationValidationCalculator.Validate(snapshot, specY);
        Assert.False(resultY.IsValid);
        Assert.Contains(resultY.ErrorMessages, e => e.Contains("Bottom Y diameter", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_NonPositiveTopDiameters_WhenTopMatEnabled_FailsValidation()
    {
        // Arrange
        var snapshot = FoundationTestData.StandardSnapshot();
        var spec = FoundationTestData.StandardSpec(diameterTopX: 0.0, diameterTopY: -12.0, isTopMatEnabled: true);

        // Act
        var result = FoundationValidationCalculator.Validate(snapshot, spec);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.ErrorMessages, e => e.Contains("Top X diameter", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.ErrorMessages, e => e.Contains("Top Y diameter", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(-1.0, 50.0, 50.0)]
    [InlineData(50.0, -5.0, 50.0)]
    [InlineData(50.0, 50.0, -10.0)]
    public void Validate_NegativeConcreteCovers_FailsValidation(double topCover, double bottomCover, double sideCover)
    {
        // Arrange
        var snapshot = FoundationTestData.StandardSnapshot();
        var spec = FoundationTestData.StandardSpec(coverTop: topCover, coverBottom: bottomCover, coverSide: sideCover);

        // Act
        var result = FoundationValidationCalculator.Validate(snapshot, spec);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.ErrorMessages, e => e.Contains("cover cannot be negative", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(100.0)] // exactly 2 × side cover
    [InlineData(90.0)]  // below 2 × side cover
    public void Validate_LengthNotAboveTwiceSideCover_FailsValidation(double length)
    {
        // Arrange
        var spec = FoundationTestData.StandardSpec(coverSide: 50.0);
        var snapshot = FoundationTestData.StandardSnapshot(length: length, width: 2000.0);

        // Act
        var result = FoundationValidationCalculator.Validate(snapshot, spec);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.ErrorMessages, e => e.Contains("Length", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(100.0)] // exactly 2 × side cover
    [InlineData(90.0)]  // below 2 × side cover
    public void Validate_WidthNotAboveTwiceSideCover_FailsValidation(double width)
    {
        // Arrange
        var spec = FoundationTestData.StandardSpec(coverSide: 50.0);
        var snapshot = FoundationTestData.StandardSnapshot(length: 2000.0, width: width);

        // Act
        var result = FoundationValidationCalculator.Validate(snapshot, spec);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.ErrorMessages, e => e.Contains("Width", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_InsufficientSlabThickness_WithTopMatEnabled_FailsValidation()
    {
        // Arrange:
        // Covers: Top=50, Bottom=50.
        // Diameters: dBX=16, dBY=16, dTX=12, dTY=12.
        // H_min = 50 + 50 + 16 + 16 + 12 + 12 = 156.0 mm.
        var spec = FoundationTestData.StandardSpec(
            coverTop: 50.0,
            coverBottom: 50.0,
            diameterBottomX: 16.0,
            diameterBottomY: 16.0,
            diameterTopX: 12.0,
            diameterTopY: 12.0,
            isTopMatEnabled: true);

        var snapshotInsufficient = FoundationTestData.StandardSnapshot(thickness: 155.0);

        // Act
        var result = FoundationValidationCalculator.Validate(snapshotInsufficient, spec);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.ErrorMessages, e => e.Contains("thickness (155.0 mm) is insufficient", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ExactMinimumSlabThickness_WithTopMatEnabled_Succeeds()
    {
        // Arrange: H_min = 156.0 mm.
        var spec = FoundationTestData.StandardSpec(
            coverTop: 50.0,
            coverBottom: 50.0,
            diameterBottomX: 16.0,
            diameterBottomY: 16.0,
            diameterTopX: 12.0,
            diameterTopY: 12.0,
            isTopMatEnabled: true);

        var snapshotExact = FoundationTestData.StandardSnapshot(thickness: 156.0);

        // Act
        var result = FoundationValidationCalculator.Validate(snapshotExact, spec);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ThicknessRequirement_WhenTopMatDisabled_RequiresOnlyBottomLayers()
    {
        // Arrange:
        // Covers: Top=50, Bottom=50. Diameters: dBX=16, dBY=16.
        // H_min_bottom_only = 50 + 50 + 16 + 16 = 132.0 mm.
        var spec = FoundationTestData.StandardSpec(
            coverTop: 50.0,
            coverBottom: 50.0,
            diameterBottomX: 16.0,
            diameterBottomY: 16.0,
            isTopMatEnabled: false);

        var snapshot140 = FoundationTestData.StandardSnapshot(thickness: 140.0);
        var snapshot131 = FoundationTestData.StandardSnapshot(thickness: 131.0);

        // Act & Assert
        var resultValid = FoundationValidationCalculator.Validate(snapshot140, spec);
        Assert.True(resultValid.IsValid);

        var resultInvalid = FoundationValidationCalculator.Validate(snapshot131, spec);
        Assert.False(resultInvalid.IsValid);
        Assert.Contains(resultInvalid.ErrorMessages, e => e.Contains("thickness (131.0 mm) is insufficient", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ExcessiveBarCount_ExceedingLimit_FailsValidation()
    {
        // Arrange: Limit is 1002 bars per layer.
        // With Width = 2000, CoverSide = 50 -> effWidth = 1900 mm.
        // If spacingBottomX = 1.0 mm -> bar count = floor(1900 / 1) + 1 = 1901 > 1002.
        var snapshot = FoundationTestData.StandardSnapshot(length: 2000.0, width: 2000.0);
        var spec = FoundationTestData.StandardSpec(spacingBottomX: 1.0);

        // Act
        var result = FoundationValidationCalculator.Validate(snapshot, spec);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.ErrorMessages, e => e.Contains("Excessive bar count for Bottom X layer", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ExcessiveBarCount_TopLayer_FailsValidation()
    {
        // Arrange: With effLength = 1900 mm, spacingTopY = 1.0 mm -> countTy = 1901 > 1002.
        var snapshot = FoundationTestData.StandardSnapshot(length: 2000.0, width: 2000.0);
        var spec = FoundationTestData.StandardSpec(spacingTopY: 1.0, isTopMatEnabled: true);

        // Act
        var result = FoundationValidationCalculator.Validate(snapshot, spec);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.ErrorMessages, e => e.Contains("Excessive bar count for Top Y layer", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_BarCountAtExactLimit_Succeeds()
    {
        // Arrange:
        // effWidth = 100100 mm, spacing = 100 mm -> floor(100100 / 100) + 1 = 1001 + 1 = 1002 bars (<= 1002)
        // With CoverSide = 50 -> Width = 100200 mm.
        // effLength = 1000 mm, spacing = 100 mm -> 11 bars.
        var snapshot = FoundationTestData.StandardSnapshot(length: 1100.0, width: 100200.0, thickness: 500.0);
        var spec = FoundationTestData.StandardSpec(spacingBottomX: 100.0, spacingBottomY: 100.0, isTopMatEnabled: false);

        // Act
        var result = FoundationValidationCalculator.Validate(snapshot, spec);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_NullArguments_ThrowsArgumentNullException()
    {
        var snapshot = FoundationTestData.StandardSnapshot();
        var spec = FoundationTestData.StandardSpec();

        Assert.Throws<ArgumentNullException>(() => FoundationValidationCalculator.Validate(null!, spec));
        Assert.Throws<ArgumentNullException>(() => FoundationValidationCalculator.Validate(snapshot, null!));
    }
}
