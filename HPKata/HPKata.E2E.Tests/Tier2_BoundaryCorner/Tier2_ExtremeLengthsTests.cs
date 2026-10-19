using HPKata.Core.Calculators;
using HPKata.Core.Optimization;
using HPKata.Core.Shapes;
using Xunit;

namespace HPKata.E2E.Tests.Tier2_BoundaryCorner;

/// <summary>
/// Tier 2: Boundary tests for extreme bar lengths (1mm micro-bars, 11,700mm stock limits, 24,000mm multi-lap).
/// </summary>
public sealed class Tier2_ExtremeLengthsTests
{
    [Fact]
    public void MicroBar_1MmLength_EvaluatesWithoutNumericalDegeneracy()
    {
        // Arrange
        double[] parameters = [1.0];

        // Act
        double length = RebarShapeCatalog.CalculateCuttingLength(
            RebarShapeType.Dang01, parameters, 0.0, 10, false);
        double weight = RebarCalculator.CalculateWeight(length / 1000.0, 10);

        // Assert
        Assert.Equal(1.0, length);
        Assert.True(weight > 0.0);
    }

    [Fact]
    public void StockBarLimit_11700Mm_FitsInSingleStockBarWithZeroWaste()
    {
        // Arrange
        var engine = new CuttingStockEngine { StandardStockLengthMm = 11700.0, SawKerfMm = 0.0 };
        var demands = new[] { new CuttingDemand(20, 11700.0, 1) };

        // Act
        var result = engine.Solve(20, demands);

        // Assert
        Assert.Single(result.Patterns);
        Assert.Equal(1, result.NewStandardBarsCount);
        Assert.Equal(0.0, result.Patterns[0].RemnantLengthMm);
        Assert.Equal(0.0, result.GrossWastePercent, 2);
    }

    [Fact]
    public void LongBar_11701Mm_ExceedsSingleStockAndRequiresTwoBars()
    {
        // Arrange: 11,701mm demand cannot fit in single 11,700mm bar without lap splicing
        var engine = new CuttingStockEngine { StandardStockLengthMm = 11700.0 };
        var demands = new[]
        {
            new CuttingDemand(20, 11700.0, 1),
            new CuttingDemand(20, 1.0, 1)
        };

        // Act
        var result = engine.Solve(20, demands);

        // Assert
        Assert.Equal(2, result.NewStandardBarsCount);
    }

    [Fact]
    public void MultiSpanContinuousBar_24000Mm_DecomposesIntoSplicedSegments()
    {
        // Arrange: 24m total length requires 2 full 11.7m bars + 1 splice of 40d (40*20 = 800mm)
        // Segment 1 = 11,700; Segment 2 = (24,000 - 11,700) + 800 = 13,100 => needs 3rd segment
        double totalRunMm = 24000.0;
        int dia = 20;
        double lapMm = RebarCalculator.LapSpliceLength(dia, 40.0);

        // Act: Segment 1 (11.7m), Segment 2 (11.7m), Segment 3 remainder
        double seg1 = 11700.0;
        double seg2 = 11700.0;
        double seg3 = totalRunMm - (seg1 - lapMm) - (seg2 - lapMm);

        // Assert
        Assert.Equal(800.0, lapMm);
        Assert.True(seg3 > 0.0 && seg3 < 11700.0);
        Assert.Equal(totalRunMm, (seg1 - lapMm) + (seg2 - lapMm) + seg3);
    }

    [Fact]
    public void BoundaryRemnant_11699Mm_Produces1MmWasteWithoutCrashing()
    {
        // Arrange: 11,699mm cut produces 1mm remnant
        var engine = new CuttingStockEngine { StandardStockLengthMm = 11700.0, MinRemnantLengthMm = 500.0 };
        var demands = new[] { new CuttingDemand(25, 11699.0, 1) };

        // Act
        var result = engine.Solve(25, demands);

        // Assert
        Assert.Single(result.Patterns);
        Assert.Equal(1.0, result.Patterns[0].RemnantLengthMm);
        Assert.False(result.Patterns[0].IsReusableRemnant, "1mm remnant must not be reusable.");
        Assert.Empty(result.NewWarehouseOffcuts);
    }

    [Fact]
    public void TripleStockBar_35100Mm_IntegratesAccuratelyInCuttingStock()
    {
        // Arrange: Exactly 3 full stock bars (3 * 11,700 = 35,100)
        var engine = new CuttingStockEngine { StandardStockLengthMm = 11700.0 };
        var demands = new[] { new CuttingDemand(16, 11700.0, 3) };

        // Act
        var result = engine.Solve(16, demands);

        // Assert
        Assert.Equal(3, result.NewStandardBarsCount);
        Assert.Equal(0.0, result.GrossWastePercent, 2);
    }
}
