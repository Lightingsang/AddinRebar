using System.Collections.Generic;
using HPKata.Core.Models;
using HPKata.Core.Scheduling;
using Xunit;

namespace HPKata.E2E.Tests.Tier1_FeatureCoverage;

/// <summary>
/// Feature 06: Rebar schedule aggregation, diameter grouping, and TCVN weight binning.
/// </summary>
public sealed class Tier1_F06_ScheduleSummariesTests
{
    [Fact]
    public void GenerateReport_AggregatesTotalLengthAndWeightAccurately()
    {
        // Arrange: 2 straight bars of D20, L = 5000mm (5m each)
        // Total length = 2 * 5 = 10m. D20 unit weight = 2.47 kg/m => 24.7 kg.
        var items = new List<RebarItem>
        {
            new()
            {
                ComponentMark = "D1",
                BarMark = "1",
                ShapeCode = "01",
                DiameterMm = 20,
                CountPerMember = 2,
                MemberCount = 1,
                ParametersMm = [5000.0],
                CuttingLengthMm = 5000.0
            }
        };

        // Act
        var report = RebarScheduleAggregator.GenerateReport(items);

        // Assert
        Assert.Single(report.Items);
        Assert.Single(report.DiameterSummaries);
        var summaryD20 = report.DiameterSummaries[0];
        Assert.Equal(20, summaryD20.DiameterMm);
        Assert.Equal(10.0, summaryD20.TotalLengthM, 2);
        Assert.Equal(24.7, summaryD20.TotalWeightKg, 1);
        Assert.Equal(24.7, report.Bins.TotalProjectWeightKg, 1);
    }

    [Fact]
    public void GenerateReport_BinsByTcvnDiameterThresholds()
    {
        // Arrange: D8 (<=10), D16 (10-18), D25 (>18)
        var items = new List<RebarItem>
        {
            new() { DiameterMm = 8, CountPerMember = 10, MemberCount = 1, CuttingLengthMm = 1000.0 }, // 10m * 0.395 = 3.95 kg
            new() { DiameterMm = 16, CountPerMember = 4, MemberCount = 1, CuttingLengthMm = 5000.0 }, // 20m * 1.580 = 31.6 kg
            new() { DiameterMm = 25, CountPerMember = 2, MemberCount = 1, CuttingLengthMm = 10000.0 } // 20m * 3.850 = 77.0 kg
        };

        // Act
        var report = RebarScheduleAggregator.GenerateReport(items);

        // Assert
        Assert.Equal(3.95, report.Bins.WireLe10Kg, 2);
        Assert.Equal(31.60, report.Bins.Deformed10To18Kg, 2);
        Assert.Equal(77.00, report.Bins.DeformedGt18Kg, 2);
        Assert.Equal(112.55, report.Bins.TotalProjectWeightKg, 2);
    }

    [Fact]
    public void GenerateReport_WithMultiMemberCount_MultipliesTotalBars()
    {
        // Arrange: 3 identical beams, each with 4 bars of D18, L=6000mm (6m)
        // Total bars = 3 * 4 = 12. Total length = 72m. D18 = 2.0 kg/m => 144 kg.
        var items = new List<RebarItem>
        {
            new()
            {
                ComponentMark = "D1..D3",
                BarMark = "1",
                ShapeCode = "01",
                DiameterMm = 18,
                CountPerMember = 4,
                MemberCount = 3,
                ParametersMm = [6000.0],
                CuttingLengthMm = 6000.0
            }
        };

        // Act
        var report = RebarScheduleAggregator.GenerateReport(items);

        // Assert
        Assert.Equal(12, report.Items[0].TotalCount);
        Assert.Equal(72.0, report.DiameterSummaries[0].TotalLengthM, 2);
        Assert.Equal(144.0, report.DiameterSummaries[0].TotalWeightKg, 1);
    }

    [Fact]
    public void GenerateReport_ComputesMissingCuttingLengthsAutomatically()
    {
        // Arrange: CuttingLengthMm is 0; should be computed from ShapeCode and ParametersMm
        var items = new List<RebarItem>
        {
            new()
            {
                ComponentMark = "Col1",
                BarMark = "1",
                ShapeCode = "02",
                DiameterMm = 20,
                CountPerMember = 2,
                MemberCount = 1,
                ParametersMm = [400.0, 3000.0], // 2*400 + 3000 = 3800
                CuttingLengthMm = 0.0
            }
        };

        // Act
        var report = RebarScheduleAggregator.GenerateReport(items);

        // Assert
        Assert.Equal(3800.0, report.Items[0].CuttingLengthMm);
    }

    [Fact]
    public void SummarizeByDiameter_SortsInAscendingDiameterOrder()
    {
        // Arrange
        var items = new List<RebarItem>
        {
            new() { DiameterMm = 25, CountPerMember = 1, MemberCount = 1, CuttingLengthMm = 1000.0 },
            new() { DiameterMm = 10, CountPerMember = 1, MemberCount = 1, CuttingLengthMm = 1000.0 },
            new() { DiameterMm = 16, CountPerMember = 1, MemberCount = 1, CuttingLengthMm = 1000.0 }
        };

        // Act
        var summaries = RebarScheduleAggregator.SummarizeByDiameter(items);

        // Assert
        Assert.Equal(10, summaries[0].DiameterMm);
        Assert.Equal(16, summaries[1].DiameterMm);
        Assert.Equal(25, summaries[2].DiameterMm);
    }
}
