using System;
using System.Collections.Generic;
using System.Linq;
using HPKata.Core.Models;
using HPKata.Core.Scheduling;
using Xunit;

namespace HPKata.Core.Tests.Scheduling
{
    public class RebarScheduleAggregatorTests
    {
        [Fact]
        public void GenerateReport_ComputesLengthsAndWeightsForItems()
        {
            var items = CreateTestItems();
            var report = RebarScheduleAggregator.GenerateReport(items);

            Assert.NotNull(report);
            Assert.Equal(2, report.Items.Count);

            // Item 1: straight bar 6000mm, dia 20
            var item1 = report.Items[0];
            Assert.Equal(6000.0, item1.CuttingLengthMm);
            Assert.Equal(8, item1.TotalCount);
            Assert.Equal(48.0, item1.TotalLengthM); // 6m * 8 = 48m
            Assert.Equal(2.470, item1.UnitMassKgPerM);
            Assert.Equal(48.0 * 2.470, item1.TotalWeightKg, 2);

            // Item 2: closed stirrup dia 8
            var item2 = report.Items[1];
            Assert.True(item2.CuttingLengthMm > 1200.0); // 2*(200+400) + 2*75 = 1350
            Assert.Equal(0.395, item2.UnitMassKgPerM);

            // Diameter summaries
            Assert.Equal(2, report.DiameterSummaries.Count);
            Assert.Contains(report.DiameterSummaries, s => s.DiameterMm == 8);
            Assert.Contains(report.DiameterSummaries, s => s.DiameterMm == 20);

            // TCVN bins
            Assert.True(report.Bins.WireLe10Kg > 0.0);
            Assert.True(report.Bins.DeformedGt18Kg > 0.0);
            Assert.Equal(report.Bins.TotalProjectWeightKg,
                report.Bins.WireLe10Kg + report.Bins.Deformed10To18Kg + report.Bins.DeformedGt18Kg, 2);
        }

        [Fact]
        public void SummarizeByDiameter_GroupsCorrectly()
        {
            var items = new List<RebarItem>
            {
                new() { DiameterMm = 16, CuttingLengthMm = 3000.0, CountPerMember = 2, MemberCount = 1 }, // 6m
                new() { DiameterMm = 16, CuttingLengthMm = 4000.0, CountPerMember = 1, MemberCount = 1 }, // 4m => total 10m
                new() { DiameterMm = 25, CuttingLengthMm = 5000.0, CountPerMember = 2, MemberCount = 1 }  // 10m
            };

            var summaries = RebarScheduleAggregator.SummarizeByDiameter(items);

            Assert.Equal(2, summaries.Count);

            var s16 = summaries.First(s => s.DiameterMm == 16);
            Assert.Equal(10.0, s16.TotalLengthM);
            Assert.Equal(1.580, s16.UnitMassKgPerM);
            Assert.Equal(15.80, s16.TotalWeightKg, 2);

            var s25 = summaries.First(s => s.DiameterMm == 25);
            Assert.Equal(10.0, s25.TotalLengthM);
            Assert.Equal(3.850, s25.UnitMassKgPerM);
            Assert.Equal(38.50, s25.TotalWeightKg, 2);
        }

        [Fact]
        public void CalculateTcvnBins_PartitionsIntoCorrectBins()
        {
            var summaries = new List<DiameterSummaryItem>
            {
                new() { DiameterMm = 6, TotalWeightKg = 100.0 },   // <= 10
                new() { DiameterMm = 10, TotalWeightKg = 150.0 },  // <= 10
                new() { DiameterMm = 14, TotalWeightKg = 300.0 },  // 10 < phi <= 18
                new() { DiameterMm = 18, TotalWeightKg = 400.0 },  // 10 < phi <= 18
                new() { DiameterMm = 20, TotalWeightKg = 500.0 },  // > 18
                new() { DiameterMm = 32, TotalWeightKg = 600.0 }   // > 18
            };

            var bins = RebarScheduleAggregator.CalculateTcvnBins(summaries);

            Assert.Equal(250.0, bins.WireLe10Kg);
            Assert.Equal(700.0, bins.Deformed10To18Kg);
            Assert.Equal(1100.0, bins.DeformedGt18Kg);
            Assert.Equal(2050.0, bins.TotalProjectWeightKg);
        }

        [Fact]
        public void ExtractCouplers_FindsCouplerItemsAndNotes()
        {
            var items = new List<RebarItem>
            {
                new()
                {
                    ComponentMark = "COT_C1",
                    ShapeCode = "coupler",
                    DiameterMm = 25,
                    CountPerMember = 4,
                    MemberCount = 2,
                    Notes = "Coupler sleeve standard"
                },
                new()
                {
                    ComponentMark = "DAM_D1",
                    ShapeCode = "01",
                    DiameterMm = 20,
                    CountPerMember = 2,
                    MemberCount = 1,
                    Notes = "Noi ren TCVN"
                },
                new()
                {
                    ComponentMark = "DAM_D1",
                    ShapeCode = "01",
                    DiameterMm = 16,
                    CountPerMember = 4,
                    MemberCount = 1,
                    Notes = "Thép thường chịu lực"
                }
            };

            var couplers = RebarScheduleAggregator.ExtractCouplers(items);

            Assert.Equal(2, couplers.Count);
            Assert.Contains(couplers, c => c.ComponentMark == "COT_C1" && c.DiameterMm == 25 && c.TotalCouplers == 8);
            Assert.Contains(couplers, c => c.ComponentMark == "DAM_D1" && c.DiameterMm == 20 && c.TotalCouplers == 2);
        }

        private static List<RebarItem> CreateTestItems()
        {
            return new List<RebarItem>
            {
                new()
                {
                    ComponentMark = "DAM_D1",
                    BarMark = "1",
                    ShapeCode = "01",
                    DiameterMm = 20,
                    CountPerMember = 4,
                    MemberCount = 2,
                    ParametersMm = new[] { 6000.0 }
                },
                new()
                {
                    ComponentMark = "DAM_D1",
                    BarMark = "2",
                    ShapeCode = "22",
                    DiameterMm = 8,
                    CountPerMember = 20,
                    MemberCount = 2,
                    ParametersMm = new[] { 200.0, 400.0 }
                }
            };
        }
    }
}
