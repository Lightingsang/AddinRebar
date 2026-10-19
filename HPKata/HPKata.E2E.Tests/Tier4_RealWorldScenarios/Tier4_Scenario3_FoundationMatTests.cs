using System.Collections.Generic;
using System.Linq;
using HPKata.Core.Models;
using HPKata.Core.Optimization;
using HPKata.Core.Scheduling;
using Xunit;

namespace HPKata.E2E.Tests.Tier4_RealWorldScenarios;

/// <summary>
/// Scenario 3: Foundation Mat & Footing Detailing.
/// Exercises orthogonal top and bottom rebar meshes with 90° anchorage hooks,
/// stand chair bars (dang26), tonnage takeoff, and 1D cutting stock on 11.7m bars.
/// </summary>
public sealed class Tier4_Scenario3_FoundationMatTests
{
    [Fact]
    public void Scenario3_FoundationMatAndChairs_ExecutesTonnageAndCuttingOptimization()
    {
        // 1. Arrange: Mat foundation 12.0m x 15.0m, thickness 1200mm
        var matItems = CreateFoundationMatItems();

        // 2. Aggregate schedule and compute mass
        var report = RebarScheduleAggregator.GenerateReport(matItems);
        Assert.Equal(4, report.Items.Count);
        Assert.Equal(2, report.DiameterSummaries.Count); // D16 and D25

        // Massive foundation: D25 mesh > 10,000 kg
        var summaryD25 = report.DiameterSummaries.First(s => s.DiameterMm == 25);
        Assert.True(summaryD25.TotalWeightKg > 10000.0);

        // Stand chairs: 180 chairs * 3.1m = 558m of D16 (approx 881 kg)
        var summaryD16 = report.DiameterSummaries.First(s => s.DiameterMm == 16);
        Assert.Equal(558.0, summaryD16.TotalLengthM, 1);
        Assert.True(summaryD16.TotalWeightKg > 800.0);

        // 3. Cutting stock optimization for stand chairs D16 (180 pieces of 3100mm)
        // 11700 / 3100 = 3 pieces per 11.7m bar (3 * 3100 = 9300; Rem = 2400mm)
        // 180 / 3 = exactly 60 bars!
        var engine = new CuttingStockEngine { StandardStockLengthMm = 11700.0, MinRemnantLengthMm = 1000.0 };
        var chairDemands = new[] { new CuttingDemand(16, 3100.0, 180) };
        var chairResult = engine.Solve(16, chairDemands, null, SolverStrategy.DuBang0);

        Assert.Equal(60, chairResult.NewStandardBarsCount);
        // Remnants of 2400mm (>1000mm) saved to warehouse
        Assert.True(chairResult.NewWarehouseOffcuts.Count > 0);
    }

    private static List<RebarItem> CreateFoundationMatItems() =>
    [
        new()
        {
            ComponentMark = "MAT_M1",
            BarMark = "1",
            ShapeCode = "02",
            DiameterMm = 25,
            CountPerMember = 100,
            MemberCount = 1,
            ParametersMm = [250.0, 12000.0],
            CuttingLengthMm = 12500.0
        },
        new()
        {
            ComponentMark = "MAT_M1",
            BarMark = "2",
            ShapeCode = "02",
            DiameterMm = 25,
            CountPerMember = 80,
            MemberCount = 1,
            ParametersMm = [250.0, 15000.0],
            CuttingLengthMm = 15500.0
        },
        new()
        {
            ComponentMark = "MAT_M1",
            BarMark = "3",
            ShapeCode = "02",
            DiameterMm = 25,
            CountPerMember = 100,
            MemberCount = 1,
            ParametersMm = [250.0, 12000.0],
            CuttingLengthMm = 12500.0
        },
        new()
        {
            ComponentMark = "MAT_M1",
            BarMark = "4",
            ShapeCode = "26",
            DiameterMm = 16,
            CountPerMember = 180,
            MemberCount = 1,
            ParametersMm = [400.0, 1000.0, 300.0, 1000.0, 400.0],
            CuttingLengthMm = 3100.0
        }
    ];
}
