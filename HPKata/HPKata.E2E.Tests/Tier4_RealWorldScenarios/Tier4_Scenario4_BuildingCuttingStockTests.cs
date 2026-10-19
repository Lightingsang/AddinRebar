using System.Collections.Generic;
using System.Linq;
using HPKata.Core.Models;
using HPKata.Core.Optimization;
using Xunit;

namespace HPKata.E2E.Tests.Tier4_RealWorldScenarios;

/// <summary>
/// Scenario 4: Building-Wide Multi-Diameter Cutting Stock & Warehouse Offcut Management.
/// Exercises 200+ piece structural rebar demand across multiple diameters (D10, D16, D20, D25, D28, D32),
/// validates 1D-CSP solver efficiency, bar conservation, and multi-phase warehouse offcut reclamation.
/// </summary>
public sealed class Tier4_Scenario4_BuildingCuttingStockTests
{
    [Fact]
    public void Scenario4_FullBuildingMultiDiameterTakeoff_SolvesCuttingStockWithHighConservation()
    {
        // 1. Arrange: 220 total rebar pieces across 6 diameters representing a 5-story building
        var demands = new List<CuttingDemand>
        {
            new(10, 1850.0, 50, "S1", "STIRRUPS"),  // 50 x 1850mm D10 beam stirrups
            new(16, 2600.0, 40, "T1", "COLUMN_TIES"), // 40 x 2600mm D16 master ties
            new(20, 5800.0, 40, "B1", "BEAM_TOP"),    // 40 x 5800mm D20 top bars (2 per 11.7m)
            new(25, 4200.0, 40, "B2", "BEAM_BOT"),    // 40 x 4200mm D25 bottom bars
            new(28, 3900.0, 30, "C1", "COL_VERT"),    // 30 x 3900mm D28 column bars (3 per 11.7m)
            new(32, 5850.0, 20, "C2", "COL_HEAVY")    // 20 x 5850mm D32 heavy verticals (2 per 11.7m)
        };

        var engine = new CuttingStockEngine
        {
            StandardStockLengthMm = 11700.0,
            MinRemnantLengthMm = 800.0,
            SawKerfMm = 5.0
        };

        // 2. Act: Solve cutting stock for each diameter group
        int totalDemandedPieces = demands.Sum(d => d.Quantity);
        int totalSolvedPieces = 0;
        int totalStandardBars = 0;

        foreach (var group in demands.GroupBy(d => d.DiameterMm))
        {
            var result = engine.Solve(group.Key, group);
            totalSolvedPieces += result.TotalPiecesCut;
            totalStandardBars += result.NewStandardBarsCount;

            // Assert exact piece conservation for each diameter
            int expectedGroupPieces = group.Sum(d => d.Quantity);
            Assert.Equal(expectedGroupPieces, result.TotalPiecesCut);
            Assert.True(result.NewStandardBarsCount > 0);
        }

        // 3. Assert total building-wide bar conservation
        Assert.Equal(220, totalDemandedPieces);
        Assert.Equal(220, totalSolvedPieces);
        Assert.True(totalStandardBars >= 70 && totalStandardBars <= 110);
    }

    [Fact]
    public void Scenario4_MultiPhaseOffcutReclaim_ReducesNewCommercialBars()
    {
        var engine = new CuttingStockEngine
        {
            StandardStockLengthMm = 11700.0,
            MinRemnantLengthMm = 1000.0,
            SawKerfMm = 0.0
        };

        // Phase 1 (Basement): 20 pieces of 4000mm D25 cut from 11.7m bars
        // 11700 - 2 * 4000 = 3700mm remnant per bar (10 bars used, 10 remnants of 3700mm)
        var phase1Demands = new[] { new CuttingDemand(25, 4000.0, 20, "1", "BASEMENT") };
        var phase1Result = engine.Solve(25, phase1Demands);

        Assert.Equal(10, phase1Result.NewStandardBarsCount);
        Assert.Equal(10, phase1Result.NewWarehouseOffcuts.Sum(w => w.Quantity));

        // Phase 2 (Tower): 8 pieces of 3500mm D25
        // Can be cut entirely from the 10 available 3700mm warehouse offcuts!
        var phase2Demands = new[] { new CuttingDemand(25, 3500.0, 8, "2", "TOWER") };
        var phase2Result = engine.Solve(25, phase2Demands, phase1Result.NewWarehouseOffcuts);

        // Assert: Zero new standard 11.7m bars purchased for Phase 2; 8 warehouse offcuts consumed!
        Assert.Equal(0, phase2Result.NewStandardBarsCount);
        Assert.Equal(8, phase2Result.ConsumedWarehouseBarsCount);
        Assert.Equal(8, phase2Result.TotalPiecesCut);
    }

    [Fact]
    public void Scenario4_SolverStrategiesComparison_DuBang0YieldsZeroScrapOnExactMultiples()
    {
        // Arrange: 30 pieces of 3900mm D20 (exactly 11700 / 3 = 3900mm)
        var demands = new[] { new CuttingDemand(20, 3900.0, 30) };
        var engine = new CuttingStockEngine { StandardStockLengthMm = 11700.0, SawKerfMm = 0.0 };

        // Act: Solve with DuBang0 strategy
        var result = engine.Solve(20, demands, null, SolverStrategy.DuBang0);

        // Assert: 10 standard bars, 0mm scrap, 0.0% net waste
        Assert.Equal(10, result.NewStandardBarsCount);
        Assert.Equal(30, result.TotalPiecesCut);
        Assert.Equal(0.0, result.NetWastePercent);
    }
}
