using System.Collections.Generic;
using System.Linq;
using HPKata.Core.Models;
using HPKata.Core.Optimization;
using Xunit;

namespace HPKata.E2E.Tests.Tier3_CrossFeature;

/// <summary>
/// Tier 3 Pipeline: Multi-phase cutting stock optimization, offcut warehouse accumulation, and offcut re-use.
/// </summary>
public sealed class Tier3_WarehouseOffcutReclaimPipelineTests
{
    [Fact]
    public void Pipeline_TwoPhaseCutting_Phase1OffcutsReclaimedInPhase2()
    {
        // Phase 1: Beam 1 requires 1 cut of 7,000mm from 11,700mm
        var engine = new CuttingStockEngine
        {
            StandardStockLengthMm = 11700.0,
            MinRemnantLengthMm = 1000.0,
            SawKerfMm = 0.0
        };

        var demandsPhase1 = new[] { new CuttingDemand(25, 7000.0, 1) };
        var resultPhase1 = engine.Solve(25, demandsPhase1);

        // Assert Phase 1: 1 standard bar, 1 reusable offcut of 4,700mm
        Assert.Equal(1, resultPhase1.NewStandardBarsCount);
        Assert.Single(resultPhase1.NewWarehouseOffcuts);
        Assert.Equal(4700.0, resultPhase1.NewWarehouseOffcuts[0].LengthMm);

        // Phase 2: Beam 2 requires 1 cut of 4,500mm
        // With warehouse offcuts available, it should consume the 4,700mm offcut!
        var warehouse = new List<WarehouseOffcut>(resultPhase1.NewWarehouseOffcuts);
        var demandsPhase2 = new[] { new CuttingDemand(25, 4500.0, 1) };
        var resultPhase2 = engine.Solve(25, demandsPhase2, warehouse, SolverStrategy.LapDayCanhDai);

        // Assert Phase 2: 0 new standard 11.7m bars purchased, 1 warehouse offcut consumed
        Assert.Equal(0, resultPhase2.NewStandardBarsCount);
        Assert.Equal(1, resultPhase2.ConsumedWarehouseBarsCount);
    }

    [Fact]
    public void Pipeline_MultiDiameterWarehouse_MaintainsStrictDiameterIsolation()
    {
        // Arrange: Warehouse has D20 offcut of 5,000mm and D25 offcut of 6,000mm
        var warehouse = new List<WarehouseOffcut>
        {
            new() { DiameterMm = 20, LengthMm = 5000.0, Quantity = 1 },
            new() { DiameterMm = 25, LengthMm = 6000.0, Quantity = 1 }
        };

        var engine = new CuttingStockEngine { StandardStockLengthMm = 11700.0 };

        // Act: Solve for D20 demand of 4,500mm
        var demandsD20 = new[] { new CuttingDemand(20, 4500.0, 1) };
        var resultD20 = engine.Solve(20, demandsD20, warehouse, SolverStrategy.LapDayCanhDai);

        // Assert: D20 demands only consume D20 warehouse bars, never D25
        Assert.Equal(0, resultD20.NewStandardBarsCount);
        Assert.Equal(1, resultD20.ConsumedWarehouseBarsCount);
        Assert.Equal(20, resultD20.DiameterMm);
    }

    [Fact]
    public void Pipeline_WarehouseReuse_SignificantlyReducesGrossWaste()
    {
        // Demand: 1 piece of 5,000mm
        var engine = new CuttingStockEngine { StandardStockLengthMm = 11700.0 };
        var demands = new[] { new CuttingDemand(22, 5000.0, 1) };

        // Solve without warehouse: buys 11.7m bar => 6,700mm waste/remnant (57.2% gross waste)
        var resultWithoutWarehouse = engine.Solve(22, demands);

        // Solve with warehouse (5,500mm bar): only 500mm remnant
        var warehouse = new[] { new WarehouseOffcut { DiameterMm = 22, LengthMm = 5500.0, Quantity = 1 } };
        var resultWithWarehouse = engine.Solve(22, demands, warehouse, SolverStrategy.LapDayCanhDai);

        // Assert: 0 new standard bars needed
        Assert.Equal(1, resultWithoutWarehouse.NewStandardBarsCount);
        Assert.Equal(0, resultWithWarehouse.NewStandardBarsCount);
        Assert.Equal(1, resultWithWarehouse.ConsumedWarehouseBarsCount);
    }

    [Fact]
    public void Pipeline_OffcutsShorterThanThreshold_ClassifiedAsScrap()
    {
        // Arrange: MinRemnantLengthMm = 800mm
        // 11,700mm bar cut with 11,000mm => remnant = 700mm (< 800mm min)
        var engine = new CuttingStockEngine
        {
            StandardStockLengthMm = 11700.0,
            MinRemnantLengthMm = 800.0
        };
        var demands = new[] { new CuttingDemand(20, 11000.0, 1) };

        // Act
        var result = engine.Solve(20, demands);

        // Assert: Not added to warehouse offcuts
        Assert.Single(result.Patterns);
        Assert.Equal(700.0, result.Patterns[0].RemnantLengthMm);
        Assert.False(result.Patterns[0].IsReusableRemnant);
        Assert.Empty(result.NewWarehouseOffcuts);
    }
}
