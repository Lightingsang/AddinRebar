using HPKata.Core.Calculators;
using HPKata.Core.Optimization;
using HPKata.Core.Shapes;
using Xunit;

namespace HPKata.E2E.Tests.Tier2_BoundaryCorner;

/// <summary>
/// Tier 2: Boundary tests for zero, negative, and invalid numerical inputs across calculators and engines.
/// </summary>
public sealed class Tier2_ZeroAndNegativeDimensionsTests
{
    [Fact]
    public void StraightBar_ZeroLength_ReturnsZeroWithoutException()
    {
        // Act
        double length = RebarShapeCatalog.CalculateCuttingLength(
            RebarShapeType.Dang01, [0.0], 0.0, 16, false);

        // Assert
        Assert.Equal(0.0, length);
    }

    [Fact]
    public void Calculator_ZeroAndNegativeDiameter_YieldsZeroUnitMass()
    {
        // Act & Assert
        Assert.Equal(0.0, RebarCalculator.GetUnitMass(0));
        Assert.Equal(0.0, RebarCalculator.GetUnitMass(-16));
        Assert.Equal(0.0, RebarCalculator.CalculateWeight(10.0, 0));
        Assert.Equal(0.0, RebarCalculator.CalculateWeight(10.0, -20));
    }

    [Fact]
    public void HooksAndLaps_NegativeDiameter_ReturnsZeroSafely()
    {
        // Act & Assert
        Assert.Equal(0.0, RebarCalculator.Hook90Length(-20));
        Assert.Equal(0.0, RebarCalculator.Hook135Length(-20));
        Assert.Equal(0.0, RebarCalculator.Hook180Length(-20));
        Assert.Equal(0.0, RebarCalculator.LapSpliceLength(-25, 40.0));
        Assert.Equal(0.0, RebarCalculator.TensionAnchorageLength(-25, 35.0));
    }

    [Fact]
    public void BendDeduction_NegativeOrZeroDiameter_ReturnsZero()
    {
        // Act
        double deductionZero = RebarCalculator.BendDeduction(0, 90.0, true);
        double deductionNeg = RebarCalculator.BendDeduction(-16, 90.0, true);

        // Assert
        Assert.Equal(0.0, deductionZero);
        Assert.Equal(0.0, deductionNeg);
    }

    [Fact]
    public void CuttingStockEngine_DemandsWithZeroOrNegativeLength_FiltersGracefully()
    {
        // Arrange: Demands with <= 0 length or quantity
        var engine = new CuttingStockEngine();
        var demands = new[]
        {
            new CuttingDemand(20, 0.0, 5),
            new CuttingDemand(20, -100.0, 3),
            new CuttingDemand(20, 5000.0, 0),
            new CuttingDemand(20, 4000.0, 2) // only this is valid
        };

        // Act
        var result = engine.Solve(20, demands);

        // Assert: Only 2 valid cuts of 4000mm in 1 bar
        Assert.Single(result.Patterns);
        Assert.Equal(1, result.NewStandardBarsCount);
        Assert.Equal(2, result.Patterns[0].CutsMm.Count);
    }

    [Fact]
    public void RoundLength_ZeroOrNegativeStep_ReturnsOriginalLength()
    {
        // Act & Assert
        Assert.Equal(1234.5, RebarCalculator.RoundLength(1234.5, 0.0));
        Assert.Equal(1234.5, RebarCalculator.RoundLength(1234.5, -5.0));
    }
}
