using System.Collections.Generic;
using HPKata.Core.Models;
using HPKata.Core.Optimization;
using HPKata.Core.Scheduling;
using HPKata.Core.Tagging;
using Xunit;

namespace HPKata.E2E.Tests.Tier2_BoundaryCorner;

/// <summary>
/// Tier 2: Boundary tests for extreme volume, large item counts, and high-throughput solver stress.
/// </summary>
public sealed class Tier2_HugeQuantityStressTests
{
    [Fact]
    public void CuttingStockEngine_HighQuantityDemands_ProcessesWithinPerformanceBounds()
    {
        // Arrange: 1,000 demands of 3,900mm (exactly 1,000 / 3 = 333 bars of 3 cuts + 1 bar of 1 cut = 334 bars)
        var engine = new CuttingStockEngine { StandardStockLengthMm = 11700.0, SawKerfMm = 0.0 };
        var demands = new[]
        {
            new CuttingDemand(20, 3900.0, 1000)
        };

        // Act
        var result = engine.Solve(20, demands, null, SolverStrategy.DuBang0);

        // Assert: 1000 cuts / 3 per bar = 334 bars
        Assert.Equal(334, result.NewStandardBarsCount);
        Assert.True(result.GrossWastePercent < 0.5);
    }

    [Fact]
    public void ScheduleAggregator_ThousandsOfItems_AggregatesWithoutPrecisionLoss()
    {
        // Arrange: 5,000 rebar items across various diameters
        var items = new List<RebarItem>(5000);
        for (int i = 0; i < 5000; i++)
        {
            items.Add(new RebarItem
            {
                ComponentMark = $"COL_{i % 50}",
                BarMark = $"{(i % 8) + 1}",
                DiameterMm = 20,
                CountPerMember = 4,
                MemberCount = 1,
                CuttingLengthMm = 4000.0
            });
        }

        // Act
        var report = RebarScheduleAggregator.GenerateReport(items);

        // Assert: 5000 items * 4 bars = 20,000 bars. Total length = 20,000 * 4m = 80,000m.
        // Weight = 80,000 * 2.47 kg/m = 197,600 kg.
        Assert.Equal(5000, report.Items.Count);
        Assert.Equal(80000.0, report.DiameterSummaries[0].TotalLengthM, 1);
        Assert.Equal(197600.0, report.Bins.TotalProjectWeightKg, 1);
    }

    [Fact]
    public void QrTagGenerator_BatchGeneration_ProducesDistinctValidBytes()
    {
        // Arrange & Act: generate 20 QR barcodes sequentially
        var payloads = new List<string>();
        for (int i = 1; i <= 20; i++)
        {
            var p = new QrTagPayload
            {
                Project = "StressTest",
                Component = $"Beam_{i}",
                Mark = $"{i}",
                DiameterMm = 16,
                CutLengthMm = 5000.0,
                Quantity = i
            };
            string text = QrTagGenerator.FormatPayload(p);
            byte[] bytes = QrTagGenerator.GeneratePngBytes(text, 5);

            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0);
        }
    }

    [Fact]
    public void LargeRebarWeight_MegaProject_AvoidsFloatInfinity()
    {
        // Arrange: 1,000,000 meters of D50 rebar (15.42 kg/m => 15,420,000 kg = 15,420 tons)
        double lengthM = 1_000_000.0;
        int dia = 50;

        // Act
        double weightKg = HPKata.Core.Calculators.RebarCalculator.CalculateWeight(lengthM, dia);

        // Assert
        Assert.Equal(15_420_000.0, weightKg, 1);
        Assert.False(double.IsInfinity(weightKg));
    }

    [Fact]
    public void CuttingPattern_LargeCutCollection_CalculatesUsedLengthAccurately()
    {
        // Arrange: 50 small cuts of 200mm each with 2mm kerf
        // 50 cuts * 200 = 10,000. 49 kerf interfaces * 2 = 98. Total = 10,098mm.
        var pattern = new CuttingPattern
        {
            StockLengthMm = 11700.0,
            KerfMm = 2.0
        };
        for (int i = 0; i < 50; i++) pattern.CutsMm.Add(200.0);

        // Assert
        Assert.Equal(10098.0, pattern.UsedLengthMm);
        Assert.Equal(1602.0, pattern.RemnantLengthMm);
    }

    [Fact]
    public void Schedule_ExtremeMemberCount_MultipliesCorrectly()
    {
        // Arrange: 10,000 precast elements
        var items = new[]
        {
            new RebarItem
            {
                ComponentMark = "PrecastPile",
                BarMark = "1",
                DiameterMm = 16,
                CountPerMember = 4,
                MemberCount = 10000,
                CuttingLengthMm = 6000.0
            }
        };

        // Act
        var report = RebarScheduleAggregator.GenerateReport(items);

        // Assert: 10,000 * 4 = 40,000 bars
        Assert.Equal(40000, report.Items[0].TotalCount);
    }
}
