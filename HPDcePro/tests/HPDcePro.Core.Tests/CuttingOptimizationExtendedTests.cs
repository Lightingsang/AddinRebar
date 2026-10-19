using System;
using System.Collections.Generic;
using System.Linq;
using HPDcePro.Core.Rebar;
using Xunit;

namespace HPDcePro.Core.Tests
{
    public class CuttingOptimizationExtendedTests
    {
        [Fact]
        public void PartitionContinuousBar_15mContinuousBar_InsertsSingleLapSplice40d()
        {
            double barLength = 15000.0;
            int diameter = 20;
            double lap40d = 40.0 * diameter; // 800 mm

            var pieces = RebarCuttingOptimizer.PartitionContinuousBar(barLength, diameter, lapMultiplier: 40.0);

            Assert.Equal(2, pieces.Count);
            Assert.Equal(11700.0, pieces[0]);
            Assert.Equal(4100.0, pieces[1]);
            Assert.Equal(barLength + lap40d, pieces.Sum());
        }

        [Fact]
        public void PartitionContinuousBar_15mContinuousBar_InsertsSingleLapSplice30d()
        {
            double barLength = 15000.0;
            int diameter = 20;
            double lap30d = 30.0 * diameter; // 600 mm

            var pieces = RebarCuttingOptimizer.PartitionContinuousBar(barLength, diameter, lapMultiplier: 30.0);

            Assert.Equal(2, pieces.Count);
            Assert.Equal(11700.0, pieces[0]);
            Assert.Equal(3900.0, pieces[1]);
            Assert.Equal(barLength + lap30d, pieces.Sum());
        }

        [Fact]
        public void PartitionContinuousBar_24mContinuousBar_InsertsTwoLapSplices()
        {
            double barLength = 24000.0;
            int diameter = 20;
            double lap40d = 40.0 * diameter; // 800 mm each, 2 laps = 1600 mm

            var pieces = RebarCuttingOptimizer.PartitionContinuousBar(barLength, diameter, lapMultiplier: 40.0);

            Assert.Equal(3, pieces.Count);
            Assert.Equal(11700.0, pieces[0]);
            Assert.Equal(11700.0, pieces[1]);
            Assert.Equal(2200.0, pieces[2]);
            Assert.Equal(barLength + (2 * lap40d), pieces.Sum());
        }

        [Fact]
        public void CuttingDemandItem_ZeroAndNegativeLength_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new CuttingDemandItem("bad1", 20, 0.0));
            Assert.Throws<ArgumentException>(() => new CuttingDemandItem("bad2", 20, -500.0));
        }

        [Fact]
        public void PartitionContinuousBar_NegativeLength_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => RebarCuttingOptimizer.PartitionContinuousBar(-1000.0, 20));
        }

        [Fact]
        public void RebarCuttingOptimizer_ScrapMinimization_ReusableOffcutThreshold()
        {
            // Case 1: Remnant 1,700 mm >= 1,500 mm threshold -> Reusable, scrap is 0
            var reusableDemand = new List<CuttingDemandItem>
            {
                new("Rebar_10m", 16, 10000.0, 1)
            };
            var resReusable = RebarCuttingOptimizer.Optimize(reusableDemand, 16);
            Assert.Single(resReusable.Patterns);
            Assert.Equal(1700.0, resReusable.Patterns[0].RemnantLengthMm);
            Assert.True(resReusable.Patterns[0].IsReusableRemnant);
            Assert.Equal(0.0, resReusable.ScrapLengthMm);

            // Case 2: Remnant 700 mm < 1,500 mm threshold -> Scrap
            var scrapDemand = new List<CuttingDemandItem>
            {
                new("Rebar_11m", 16, 11000.0, 1)
            };
            var resScrap = RebarCuttingOptimizer.Optimize(scrapDemand, 16);
            Assert.Single(resScrap.Patterns);
            Assert.Equal(700.0, resScrap.Patterns[0].RemnantLengthMm);
            Assert.False(resScrap.Patterns[0].IsReusableRemnant);
            Assert.Equal(700.0, resScrap.ScrapLengthMm);
        }

        [Fact]
        public void RebarCuttingOptimizer_MultipleDemands_ComparesBfdAndFfd()
        {
            var demands = new List<CuttingDemandItem>
            {
                new("D_A", 20, 4500.0, 3),
                new("D_B", 20, 3200.0, 4),
                new("D_C", 20, 2100.0, 5)
            };

            var bfd = RebarCuttingOptimizer.Optimize(demands, 20, CuttingStrategy.BestFitDecreasing);
            var ffd = RebarCuttingOptimizer.Optimize(demands, 20, CuttingStrategy.FirstFitDecreasing);
            var auto = RebarCuttingOptimizer.Optimize(demands, 20, CuttingStrategy.Auto);

            Assert.True(bfd.TotalStockBarsUsed > 0);
            Assert.True(ffd.TotalStockBarsUsed > 0);
            Assert.True(auto.TotalStockBarsUsed <= Math.Max(bfd.TotalStockBarsUsed, ffd.TotalStockBarsUsed));

            foreach (var pat in auto.Patterns)
            {
                Assert.True(pat.UsedLengthMm <= pat.StockLengthMm);
            }
        }
    }
}
