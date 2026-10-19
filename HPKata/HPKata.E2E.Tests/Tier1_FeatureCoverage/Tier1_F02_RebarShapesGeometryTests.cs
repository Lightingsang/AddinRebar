using System;
using HPKata.Core.Shapes;
using Xunit;

namespace HPKata.E2E.Tests.Tier1_FeatureCoverage;

/// <summary>
/// Feature 02: Rebar shape definitions, parameter mappings, and cutting length equations.
/// </summary>
public sealed class Tier1_F02_RebarShapesGeometryTests
{
    [Fact]
    public void StraightBar_Dang01_ReturnsExactL1()
    {
        // Arrange
        var shape = RebarShapeType.Dang01;
        double[] parameters = [5850.0];

        // Act
        double length = RebarShapeCatalog.CalculateCuttingLength(shape, parameters, 0, 16, false);

        // Assert
        Assert.Equal(5850.0, length);
    }

    [Fact]
    public void SymmetricUBar_Dang02_CalculatesTwoLegsPlusBase()
    {
        // Arrange (L1 = leg 400mm, L2 = base 2500mm)
        var shape = RebarShapeType.Dang02;
        double[] parameters = [400.0, 2500.0];

        // Act
        double lengthNoDed = RebarShapeCatalog.CalculateCuttingLength(shape, parameters, 0, 20, false);
        double lengthWithDed = RebarShapeCatalog.CalculateCuttingLength(shape, parameters, 0, 20, true);

        // Assert (2*400 + 2500 = 3300)
        Assert.Equal(3300.0, lengthNoDed);
        Assert.True(lengthWithDed < lengthNoDed, "Bend deduction should reduce total cutting length.");
    }

    [Fact]
    public void ClosedStirrup_Dang22_IncludesPerimeterPlusTwo135Hooks()
    {
        // Arrange (H = 500, B = 200, hook = 80)
        var shape = RebarShapeType.Dang22;
        double[] parameters = [500.0, 200.0, 80.0];

        // Act
        double length = RebarShapeCatalog.CalculateCuttingLength(shape, parameters, 0, 8, false);

        // Assert (2*(500+200) + 2*80 = 1400 + 160 = 1560)
        Assert.Equal(1560.0, length);
    }

    [Fact]
    public void AsymmetricUBar_Dang04_SumsThreeSegments()
    {
        // Arrange (L1=300, L2=4000, L3=600)
        var shape = RebarShapeType.Dang04;
        double[] parameters = [300.0, 4000.0, 600.0];

        // Act
        double length = RebarShapeCatalog.CalculateCuttingLength(shape, parameters, 0, 16, false);

        // Assert (300 + 4000 + 600 = 4900)
        Assert.Equal(4900.0, length);
    }

    [Fact]
    public void NormalizeShapeCode_SupportsVariousPrefixesAndAliases()
    {
        // Act & Assert
        Assert.Equal(RebarShapeType.Dang01, RebarShapeCatalog.NormalizeShapeCode("01"));
        Assert.Equal(RebarShapeType.Dang01, RebarShapeCatalog.NormalizeShapeCode("dang01"));
        Assert.Equal(RebarShapeType.Dang22, RebarShapeCatalog.NormalizeShapeCode("22"));
        Assert.Equal(RebarShapeType.Dang22, RebarShapeCatalog.NormalizeShapeCode("dang22"));
        Assert.Equal(RebarShapeType.Dang01, RebarShapeCatalog.NormalizeShapeCode(null));
    }
}
