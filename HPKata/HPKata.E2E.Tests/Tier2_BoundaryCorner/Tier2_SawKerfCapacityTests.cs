using System.Collections.Generic;
using HPKata.Core.Optimization;
using Xunit;

namespace HPKata.E2E.Tests.Tier2_BoundaryCorner;

/// <summary>
/// Tier 2: Boundary tests for saw kerf variations (0mm, 2mm, 5mm, 10mm, 20mm) and kerf capacity bounds.
/// </summary>
public sealed class Tier2_SawKerfCapacityTests
{
    [Fact]
    public void ZeroKerf_CalculatesExactSumOfCuts()
    {
        // Arrange
        var pattern = new CuttingPattern
        {
            StockLengthMm = 11700.0,
            KerfMm = 0.0,
            CutsMm = [3000.0, 4000.0, 4000.0]
        };

        // Assert: 3000 + 4000 + 4000 = 11,000; Rem = 700
        Assert.Equal(11000.0, pattern.UsedLengthMm);
        Assert.Equal(700.0, pattern.RemnantLengthMm);
    }

    [Fact]
    public void LaserKerf_2Mm_SubtractsKerfPerCutInterface()
    {
        // Arrange: 3 cuts have 2 blade interfaces => 2 * 2mm = 4mm kerf
        var pattern = new CuttingPattern
        {
            StockLengthMm = 11700.0,
            KerfMm = 2.0,
            CutsMm = [3000.0, 4000.0, 4000.0]
        };

        // Assert: 11,000 + 4 = 11,004; Rem = 696
        Assert.Equal(11004.0, pattern.UsedLengthMm);
        Assert.Equal(696.0, pattern.RemnantLengthMm);
    }

    [Fact]
    public void StandardSawKerf_5Mm_EnforcesExactInterfaceDeduction()
    {
        // Arrange: 4 cuts of 2900mm = 11,600mm. 3 blade interfaces * 5mm = 15mm. Total = 11,615mm.
        var pattern = new CuttingPattern
        {
            StockLengthMm = 11700.0,
            KerfMm = 5.0,
            CutsMm = [2900.0, 2900.0, 2900.0, 2900.0]
        };

        // Assert
        Assert.Equal(11615.0, pattern.UsedLengthMm);
        Assert.Equal(85.0, pattern.RemnantLengthMm);
    }

    [Fact]
    public void AbrasiveKerf_10Mm_PushesMarginalCutOverCapacity()
    {
        // Arrange: 2 cuts of 5845mm = 11,690mm.
        // With 10mm kerf: 11,690 + 10 = 11,700mm (Exact fit in 1 bar)
        // With 11mm kerf or cuts of 5850mm: exceeds 11,700mm
        var engine = new CuttingStockEngine
        {
            StandardStockLengthMm = 11700.0,
            SawKerfMm = 10.0
        };
        var demands = new List<CuttingDemand>
        {
            new(20, 5845.0, 2)
        };

        // Act
        var result = engine.Solve(20, demands);

        // Assert: Fits exactly in 1 bar with 0 remnant!
        Assert.Equal(1, result.NewStandardBarsCount);
        Assert.Equal(0.0, result.Patterns[0].RemnantLengthMm);
    }

    [Fact]
    public void HeavyTorchKerf_20Mm_ForcesSecondBarWhenExceedingLimit()
    {
        // Arrange: 2 cuts of 5845mm with 20mm kerf => 11,690 + 20 = 11,710 > 11,700
        var engine = new CuttingStockEngine
        {
            StandardStockLengthMm = 11700.0,
            SawKerfMm = 20.0
        };
        var demands = new List<CuttingDemand>
        {
            new(25, 5845.0, 2)
        };

        // Act
        var result = engine.Solve(25, demands);

        // Assert: Must allocate 2 bars
        Assert.Equal(2, result.NewStandardBarsCount);
    }

    [Fact]
    public void SingleCut_HasZeroKerfRegardlessOfKerfSetting()
    {
        // Arrange: 1 cut on stock bar does not require additional kerf interface
        var pattern = new CuttingPattern
        {
            StockLengthMm = 11700.0,
            KerfMm = 20.0,
            CutsMm = [8000.0]
        };

        // Assert
        Assert.Equal(8000.0, pattern.UsedLengthMm);
        Assert.Equal(3700.0, pattern.RemnantLengthMm);
    }
}
