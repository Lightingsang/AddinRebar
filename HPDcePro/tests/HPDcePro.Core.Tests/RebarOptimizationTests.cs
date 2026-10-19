using System.Collections.Generic;
using System.Linq;
using HPDcePro.Core.Rebar;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public class RebarOptimizationTests
    {
        [Fact]
        public void PartitionContinuousBar_WhenUnderStockLength_ReturnsSinglePiece()
        {
            var pieces = RebarCuttingOptimizer.PartitionContinuousBar(6000.0, 20);
            Assert.Single(pieces);
            Assert.Equal(6000.0, pieces[0]);
        }

        [Fact]
        public void PartitionContinuousBar_WhenExceeds11700_InsertsLapSplice()
        {
            // 18,000 mm bar, d = 20 mm, lap = 40d = 800 mm
            var pieces = RebarCuttingOptimizer.PartitionContinuousBar(18000.0, 20, lapMultiplier: 40.0);

            Assert.Equal(2, pieces.Count);
            Assert.Equal(11700.0, pieces[0]);
            // First piece covers 11700 - 800 = 10900 mm. Remaining to cover = 18000 - 10900 = 7100 mm.
            Assert.Equal(7100.0, pieces[1]);

            // Total steel length = 11700 + 7100 = 18800 = 18000 + 800 (one lap splice of 800 mm!)
            Assert.Equal(18800.0, pieces.Sum());
        }

        [Fact]
        public void PartitionContinuousBar_WhenVeryLong_CreatesMultipleSplices()
        {
            // 30,000 mm bar, d = 25 mm, lap = 40d = 1000 mm
            var pieces = RebarCuttingOptimizer.PartitionContinuousBar(30000.0, 25, lapMultiplier: 40.0);

            Assert.Equal(3, pieces.Count);
            Assert.Equal(11700.0, pieces[0]);
            Assert.Equal(11700.0, pieces[1]);
            Assert.True(pieces[2] > 0);
            // Each internal joint adds 1000 mm lap
            Assert.Equal(32000.0, pieces.Sum());
        }

        [Fact]
        public void RebarCuttingOptimizer_PacksDemands_MinimizingScrap()
        {
            var demands = new List<CuttingDemandItem>
            {
                new("D1_Top1", 20, 5800.0, 2),
                new("D1_Top2", 20, 5800.0, 2),
                new("D1_Bot1", 20, 4000.0, 2),
                new("D1_Bot2", 20, 3000.0, 2)
            };

            var result = RebarCuttingOptimizer.Optimize(demands, 20, CuttingStrategy.Auto);

            Assert.True(result.TotalStockBarsUsed > 0);
            Assert.True(result.ScrapPercentage >= 0.0);
            Assert.Equal(20, result.DiameterMm);

            // Verify each pattern cuts within 11,700 mm
            foreach (var pat in result.Patterns)
            {
                Assert.True(pat.UsedLengthMm <= pat.StockLengthMm);
                Assert.Equal(pat.StockLengthMm - pat.UsedLengthMm, pat.RemnantLengthMm, 2);
            }
        }

        [Fact]
        public void RebarCuttingOptimizer_TagsReusableRemnants()
        {
            // Bar with large remainder >= 1500 mm
            var demands = new List<CuttingDemandItem>
            {
                new("D2_Cut1", 16, 9000.0, 1)
            };

            var result = RebarCuttingOptimizer.Optimize(demands, 16);

            Assert.Single(result.Patterns);
            var pat = result.Patterns[0];
            Assert.Equal(2700.0, pat.RemnantLengthMm); // 11700 - 9000 = 2700 mm
            Assert.True(pat.IsReusableRemnant);
            Assert.Equal(0.0, result.ScrapLengthMm); // Since remnant is reusable, scrap is 0
        }

        [Fact]
        public void RebarScheduleAggregator_ComputesUnitMassesPerTCVN1651()
        {
            Assert.Equal(0.395, RebarScheduleAggregator.GetUnitMass(8));
            Assert.Equal(0.617, RebarScheduleAggregator.GetUnitMass(10));
            Assert.Equal(1.580, RebarScheduleAggregator.GetUnitMass(16));
            Assert.Equal(2.470, RebarScheduleAggregator.GetUnitMass(20));
            Assert.Equal(3.850, RebarScheduleAggregator.GetUnitMass(25));
        }

        [Fact]
        public void RebarScheduleAggregator_AggregatesByElementAndDiameter()
        {
            var items = new List<ScheduleItem>
            {
                new() { ElementMark = "D1", BarMark = "1", DiameterMm = 20, LengthMm = 6000.0, QuantityPerElement = 2, ElementCount = 3 },
                new() { ElementMark = "D1", BarMark = "2", DiameterMm = 8, LengthMm = 1200.0, QuantityPerElement = 20, ElementCount = 3 },
                new() { ElementMark = "C1", BarMark = "1", DiameterMm = 20, LengthMm = 4000.0, QuantityPerElement = 4, ElementCount = 2 },
                new() { ElementMark = "S1", BarMark = "1", DiameterMm = 10, LengthMm = 5000.0, QuantityPerElement = 10, ElementCount = 1 }
            };

            var report = RebarScheduleAggregator.Aggregate(items);

            Assert.Equal(3, report.ElementSummaries.Count); // D1, C1, S1
            Assert.Equal(3, report.DiameterSummaries.Count); // d8, d10, d20
            Assert.True(report.TotalWeightKg > 0.0);
            Assert.True(report.TotalWeightTons > 0.0);

            // Bins verification
            Assert.True(report.WeightD10AndUnderKg > 0.0); // contains d8 and d10
            Assert.True(report.WeightAboveD18Kg > 0.0);    // contains d20
        }

        [Fact]
        public void RebarCuttingOptimizer_EmptyDemands_ReturnsEmptyResult()
        {
            var res = RebarCuttingOptimizer.Optimize(new List<CuttingDemandItem>(), 20);
            Assert.Equal(0, res.TotalStockBarsUsed);
            Assert.Empty(res.Patterns);
            Assert.Equal(0.0, res.ScrapPercentage);
        }

        [Fact]
        public void PartitionContinuousBar_ExactCommercialLength_NoSpliceNeeded()
        {
            var pieces = RebarCuttingOptimizer.PartitionContinuousBar(11700.0, 20);
            Assert.Single(pieces);
            Assert.Equal(11700.0, pieces[0]);

            var zeroPieces = RebarCuttingOptimizer.PartitionContinuousBar(0.0, 20);
            Assert.Empty(zeroPieces);
        }

        [Fact]
        public void RebarScheduleAggregator_NonStandardDiameter_UsesTheoreticalFormula()
        {
            // d30 is not in standard table, uses 30^2 / 162.2 = 900 / 162.2 = 5.5487
            double mass30 = RebarScheduleAggregator.GetUnitMass(30);
            Assert.Equal(900.0 / 162.2, mass30, 4);

            double massZero = RebarScheduleAggregator.GetUnitMass(0);
            Assert.Equal(0.0, massZero);
        }
    }
}
