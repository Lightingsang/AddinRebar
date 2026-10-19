using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HPKata.Core.Models;
using HPKata.Core.Optimization;
using Xunit;

namespace HPKata.Core.Tests.Optimization
{
    public class CuttingStockEngineTests
    {
        [Fact]
        public void Solve_AutoStrategy_PicksOptimalPortfolioResult()
        {
            var engine = new CuttingStockEngine();
            var demands = new[]
            {
                new CuttingDemand(20, 5850.0, 4), // 2 bars of 11700 split exactly into 2x5850
                new CuttingDemand(20, 3900.0, 3)  // 1 bar of 11700 split exactly into 3x3900
            };

            var result = engine.Solve(20, demands, strategy: SolverStrategy.Auto);

            Assert.NotNull(result);
            Assert.Equal(20, result.DiameterMm);
            Assert.Equal(3, result.Patterns.Count);
            Assert.Equal(0.0, result.NetWastePercent);
            Assert.Equal(0.0, result.GrossWastePercent);
        }

        [Fact]
        public void Solve_DuBang0_ExactMultiples_ProducesZeroWaste()
        {
            var engine = new CuttingStockEngine();
            var demands = new[]
            {
                new CuttingDemand(25, 5850.0, 2),
                new CuttingDemand(25, 2925.0, 4)
            };

            var result = engine.Solve(25, demands, strategy: SolverStrategy.DuBang0);

            Assert.NotNull(result);
            Assert.Equal(2, result.Patterns.Count);
            Assert.Equal(0.0, result.NetWastePercent);
            Assert.Equal(0.0, result.GrossWastePercent);
        }

        [Fact]
        public void Solve_DuBang0_CombinatorialExplosion_CompletesWithin1000MsBudget()
        {
            // 150 items of 2000mm on 11,700mm stock: no exact subset exists (remainder is 1700 or 3700)
            // Remediation DEFECT-M2-01 ensures math pruning + timeout check prevents combinatorial hang
            var engine = new CuttingStockEngine();
            var demands = new[] { new CuttingDemand(16, 2000.0, 150) };

            var sw = Stopwatch.StartNew();
            var result = engine.Solve(16, demands, strategy: SolverStrategy.DuBang0);
            sw.Stop();

            Assert.NotNull(result);
            Assert.True(sw.ElapsedMilliseconds <= 1000,
                $"DuBang0 took {sw.ElapsedMilliseconds} ms, exceeding the 1000 ms budget.");
            Assert.True(result.Patterns.Count > 0);
        }

        [Fact]
        public void Solve_LapDayCanhDaiBfd_PacksDescendingCorrectly()
        {
            var engine = new CuttingStockEngine();
            var demands = new[]
            {
                new CuttingDemand(18, 6000.0, 2),
                new CuttingDemand(18, 5000.0, 2),
                new CuttingDemand(18, 4000.0, 2)
            };

            var result = engine.Solve(18, demands, strategy: SolverStrategy.LapDayCanhDai);

            Assert.NotNull(result);
            Assert.True(result.Patterns.Count > 0);
            Assert.All(result.Patterns, p => Assert.True(p.CutsMm.Sum() <= p.StockLengthMm + 1.0));
        }

        [Fact]
        public void Solve_BeamSearchTopK_GeneratesValidPatterns()
        {
            var engine = new CuttingStockEngine();
            var demands = new[]
            {
                new CuttingDemand(22, 4500.0, 3),
                new CuttingDemand(22, 3500.0, 3),
                new CuttingDemand(22, 2500.0, 3)
            };

            var result = engine.Solve(22, demands, strategy: SolverStrategy.BeamSearchTopK);

            Assert.NotNull(result);
            Assert.True(result.Patterns.Count > 0);
            Assert.All(result.Patterns, p => Assert.True(p.RemnantLengthMm >= 0.0));
        }

        [Fact]
        public void Solve_PatternCat_GeneratesValidPatterns()
        {
            var engine = new CuttingStockEngine();
            var demands = new[]
            {
                new CuttingDemand(14, 5850.0, 2),
                new CuttingDemand(14, 2925.0, 2)
            };

            var result = engine.Solve(14, demands, strategy: SolverStrategy.PatternCat);

            Assert.NotNull(result);
            Assert.True(result.Patterns.Count > 0);
            Assert.All(result.Patterns, p => Assert.True(p.CutsMm.Sum() <= p.StockLengthMm + 1.0));
        }

        [Fact]
        public void Solve_WithWarehouseOffcuts_PrioritizesWarehouseBars()
        {
            var engine = new CuttingStockEngine();
            var warehouse = new[]
            {
                new WarehouseOffcut(20, 6000.0, 2),
                new WarehouseOffcut(20, 4000.0, 1)
            };
            var demands = new[]
            {
                new CuttingDemand(20, 5500.0, 2),
                new CuttingDemand(20, 3800.0, 1)
            };

            var result = engine.Solve(20, demands, warehouse, SolverStrategy.LapDayCanhDai);

            Assert.NotNull(result);
            int warehouseBarsUsed = result.Patterns.Count(p => p.IsWarehouseBar);
            int newBars = result.Patterns.Count(p => !p.IsWarehouseBar);

            Assert.Equal(3, warehouseBarsUsed);
            Assert.Equal(0, newBars);
        }

        [Fact]
        public void Solve_AllWarehouseOffcutsNoNewBars_AvoidsDivideByZeroInNetWaste()
        {
            var engine = new CuttingStockEngine();
            var warehouse = new[] { new WarehouseOffcut(16, 5000.0, 2) };
            var demands = new[] { new CuttingDemand(16, 4800.0, 2) }; // remnant = 200mm each (< 500mm min remnant => scrap)

            var result = engine.Solve(16, demands, warehouse, SolverStrategy.LapDayCanhDai);

            Assert.NotNull(result);
            int newBars = result.Patterns.Count(p => !p.IsWarehouseBar);
            Assert.Equal(0, newBars);

            // Piecewise defense should calculate waste against consumed warehouse stock without NaN or DivideByZero
            Assert.False(double.IsNaN(result.NetWastePercent));
            Assert.False(double.IsInfinity(result.NetWastePercent));
            Assert.True(result.NetWastePercent >= 0.0);
        }

        [Fact]
        public void Solve_KerfAndMinimumRemnant_DistinguishesReusableOffcutFromScrap()
        {
            var engine = new CuttingStockEngine
            {
                SawKerfMm = 5.0,
                MinRemnantLengthMm = 500.0
            };

            // Stock = 11700. Cut 10500. Kerf = 0 (1 piece). Remnant = 11700 - 10500 = 1200mm >= 500mm -> Reusable
            // Cut 11300. Remnant = 11700 - 11300 = 400mm < 500mm -> Scrap
            var demands = new[]
            {
                new CuttingDemand(25, 10500.0, 1),
                new CuttingDemand(25, 11300.0, 1)
            };

            var result = engine.Solve(25, demands, strategy: SolverStrategy.LapDayCanhDai);

            Assert.NotNull(result);
            Assert.Equal(2, result.Patterns.Count);

            var reusablePattern = result.Patterns.FirstOrDefault(p => p.IsReusableRemnant);
            Assert.NotNull(reusablePattern);
            Assert.True(reusablePattern.RemnantLengthMm >= 500.0);

            var scrapPattern = result.Patterns.FirstOrDefault(p => !p.IsReusableRemnant);
            Assert.NotNull(scrapPattern);
            Assert.True(scrapPattern.RemnantLengthMm < 500.0);

            Assert.NotEmpty(result.NewWarehouseOffcuts);
        }

        [Fact]
        public void Solve_EmptyDemands_ReturnsEmptyResult()
        {
            var engine = new CuttingStockEngine();
            var result = engine.Solve(20, Array.Empty<CuttingDemand>());

            Assert.NotNull(result);
            Assert.Equal(20, result.DiameterMm);
            Assert.Empty(result.Patterns);
            Assert.Equal(0.0, result.NetWastePercent);
            Assert.Equal(0.0, result.GrossWastePercent);
        }

        [Fact]
        public void Solve_PatternCat_WithKerf_NeverExceedsStockLength()
        {
            var engine = new CuttingStockEngine
            {
                StandardStockLengthMm = 11700.0,
                SawKerfMm = 5.0
            };
            var demands = new[]
            {
                new CuttingDemand(20, 5850.0, 4),
                new CuttingDemand(20, 6000.0, 2),
                new CuttingDemand(20, 5700.0, 2)
            };

            var result = engine.Solve(20, demands, strategy: SolverStrategy.PatternCat);

            Assert.NotNull(result);
            Assert.True(result.Patterns.Count > 0);
            Assert.All(result.Patterns, p =>
            {
                Assert.True(p.UsedLengthMm <= p.StockLengthMm + 1e-4,
                    $"Pattern exceeds stock: Used={p.UsedLengthMm}mm > Stock={p.StockLengthMm}mm");
            });
        }
    }
}
