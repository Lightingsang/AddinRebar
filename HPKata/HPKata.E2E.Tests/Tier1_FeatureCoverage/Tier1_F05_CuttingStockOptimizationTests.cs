using System.Collections.Generic;
using System.Linq;
using HPKata.Core.Optimization;
using Xunit;

namespace HPKata.E2E.Tests.Tier1_FeatureCoverage;

/// <summary>
/// Feature 05: 1D cutting-stock optimization, heuristics, remnant tracking, and scrap computation.
/// </summary>
public sealed class Tier1_F05_CuttingStockOptimizationTests
{
    [Fact]
    public void Solve_SingleDemandExactMultiple_YieldsZeroWaste()
    {
        // Arrange: 11,700mm bar divided into 3 pieces of 3,900mm (3 * 3900 = 11700)
        var engine = new CuttingStockEngine { StandardStockLengthMm = 11700.0, SawKerfMm = 0.0 };
        var demands = new List<CuttingDemand>
        {
            new(20, 3900.0, 3, "1", "B1")
        };

        // Act
        var result = engine.Solve(20, demands, null, SolverStrategy.DuBang0);

        // Assert
        Assert.Single(result.Patterns);
        Assert.Equal(1, result.NewStandardBarsCount);
        Assert.Equal(0.0, result.Patterns[0].RemnantLengthMm);
        Assert.Equal(0.0, result.GrossWastePercent, 2);
    }

    [Fact]
    public void Solve_AutoStrategy_SelectsOptimalPatternGrouping()
    {
        // Arrange: Mixed cuts
        var engine = new CuttingStockEngine { StandardStockLengthMm = 11700.0 };
        var demands = new List<CuttingDemand>
        {
            new(16, 5850.0, 2, "1", "D1"), // 2 * 5850 = 11700 (1 full bar)
            new(16, 2925.0, 4, "2", "D1")  // 4 * 2925 = 11700 (1 full bar)
        };

        // Act
        var result = engine.Solve(16, demands, null, SolverStrategy.Auto);

        // Assert: Total length = 23,400mm = exactly 2 bars of 11,700mm
        Assert.Equal(2, result.NewStandardBarsCount);
        Assert.Equal(0.0, result.GrossWastePercent, 1);
    }

    [Fact]
    public void Solve_WithWarehouseOffcuts_PrioritizesExistingStock()
    {
        // Arrange
        var engine = new CuttingStockEngine { StandardStockLengthMm = 11700.0 };
        var demands = new List<CuttingDemand>
        {
            new(25, 4500.0, 1, "1", "Col1")
        };
        var warehouse = new List<HPKata.Core.Models.WarehouseOffcut>
        {
            new() { DiameterMm = 25, LengthMm = 5000.0, Quantity = 1 }
        };

        // Act
        var result = engine.Solve(25, demands, warehouse, SolverStrategy.LapDayCanhDai);

        // Assert: Should consume the warehouse bar, 0 new 11.7m bars purchased
        Assert.Equal(0, result.NewStandardBarsCount);
        Assert.Equal(1, result.ConsumedWarehouseBarsCount);
    }

    [Fact]
    public void Solve_RemnantsAboveThreshold_CategorizedAsReusableOffcuts()
    {
        // Arrange: 11,700mm stock bar cut with 1 piece of 8,000mm => Remnant = 3,700mm (> 500mm min)
        var engine = new CuttingStockEngine
        {
            StandardStockLengthMm = 11700.0,
            MinRemnantLengthMm = 500.0
        };
        var demands = new List<CuttingDemand>
        {
            new(22, 8000.0, 1, "1", "BeamA")
        };

        // Act
        var result = engine.Solve(22, demands);

        // Assert
        Assert.Single(result.Patterns);
        Assert.True(result.Patterns[0].IsReusableRemnant);
        Assert.NotEmpty(result.NewWarehouseOffcuts);
        Assert.Equal(3700.0, result.NewWarehouseOffcuts[0].LengthMm);
    }

    [Fact]
    public void Solve_WithKerfAllowance_ReducesNetAvailableCapacity()
    {
        // Arrange: 2 cuts of 5850 with 10mm kerf => total required = 5850 + 5850 + 10 = 11,710 > 11700!
        var engine = new CuttingStockEngine
        {
            StandardStockLengthMm = 11700.0,
            SawKerfMm = 10.0
        };
        var demands = new List<CuttingDemand>
        {
            new(16, 5850.0, 2, "1", "D1")
        };

        // Act
        var result = engine.Solve(16, demands);

        // Assert: Cannot fit 2 cuts in 1 bar due to 10mm kerf; needs 2 bars
        Assert.Equal(2, result.NewStandardBarsCount);
    }
}
