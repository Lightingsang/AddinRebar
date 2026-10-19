using System;
using HPKata.Revit.Abstractions;
using HPKata.Revit.Bridge;
using HPKata.Revit.Units;
using Xunit;

namespace HPKata.E2E.Tests.Tier2_BoundaryCorner;

/// <summary>
/// Tier 2: Boundary tests for Revit parameter bridges, shape adapters, and sub-millimeter unit conversions.
/// </summary>
public sealed class Tier2_RevitParameterEdgeCasesTests
{
    [Fact]
    public void ResolveKataShape_WithUnmappedRevitShape_FallsBackToFreeformDang00()
    {
        // Act
        string resolved = RevitShapeAdapter.ResolveKataShape("UNKNOWN_CUSTOM_SHAPE_XYZ");

        // Assert
        Assert.Equal("dang00", resolved);
    }

    [Fact]
    public void ResolveRevitShape_WithUnmappedKataShape_FallsBackToStraightM00()
    {
        // Act
        string resolved = RevitShapeAdapter.ResolveRevitShape("dang999");

        // Assert
        Assert.Equal("M_00", resolved);
    }

    [Fact]
    public void ResolveKataShape_UBarDisambiguation_DistinguishesSymmetricAndAsymmetric()
    {
        // Arrange
        // Symmetric U-bar (A == C)
        double[] symmetricParams = [500.0, 3000.0, 500.0];
        // Asymmetric U-bar (|A - C| >= 1.0mm)
        double[] asymmetricParams = [500.0, 3000.0, 700.0];

        // Act
        string sym = RevitShapeAdapter.ResolveKataShape("M_21", symmetricParams);
        string asym = RevitShapeAdapter.ResolveKataShape("M_21", asymmetricParams);

        // Assert
        Assert.Equal("dang02", sym);
        Assert.Equal("dang04", asym);
    }

    [Fact]
    public void ToRebarItem_NegativeQuantity_ClampsToOne()
    {
        // Arrange
        var dto = new RevitRebarDto
        {
            Quantity = -5,
            DiameterMm = 20,
            SingleBarLengthMm = 4000.0
        };

        // Act
        var item = RevitParameterBridge.ToRebarItem(dto);

        // Assert: clamped to at least 1
        Assert.Equal(1, item.CountPerMember);
    }

    [Fact]
    public void RevitUnitConverter_SubMillimeterValues_MaintainsMicroTolerance()
    {
        // Arrange: 0.1 mm = 0.1 / 304.8 ft
        double subMm = 0.1;

        // Act
        double ft = RevitUnitConverter.MmToFeet(subMm);
        double restoredMm = RevitUnitConverter.FeetToMm(ft);

        // Assert: precision within 1e-6
        Assert.Equal(subMm, restoredMm, 6);
    }

    [Fact]
    public void ToRebarDto_WithEmptyParameters_InitializesEmptyArray()
    {
        // Arrange
        var item = new HPKata.Core.Models.RebarItem
        {
            ComponentMark = "C1",
            BarMark = "1",
            ShapeCode = "01",
            ParametersMm = []
        };

        // Act
        var dto = RevitParameterBridge.ToRebarDto(item, 10);

        // Assert
        Assert.NotNull(dto.ParametersMm);
        Assert.Empty(dto.ParametersMm);
    }
}
