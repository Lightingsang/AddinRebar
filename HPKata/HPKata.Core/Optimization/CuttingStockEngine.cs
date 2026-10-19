using System;
using System.Collections.Generic;
using System.Linq;
using HPKata.Core.Models;

namespace HPKata.Core.Optimization
{
    /// <summary>
    /// 1D Cutting-Stock Problem (1D-CSP) optimization engine for commercial 11,700 mm rebar bars and warehouse offcuts.
    /// </summary>
    public class CuttingStockEngine
    {
        public double StandardStockLengthMm { get; set; } = 11700.0;
        public double MinRemnantLengthMm { get; set; } = 500.0;
        public double SawKerfMm { get; set; } = 0.0;

        public CuttingStockResult Solve(
            int diameterMm,
            IEnumerable<CuttingDemand> demands,
            IEnumerable<WarehouseOffcut>? availableWarehouse = null,
            SolverStrategy strategy = SolverStrategy.Auto)
        {
            var pieces = ExpandDemandsToPieces(demands);
            if (pieces.Count == 0)
            {
                return new CuttingStockResult { DiameterMm = diameterMm, StrategyUsed = strategy };
            }

            var warehouseBars = (availableWarehouse ?? Enumerable.Empty<WarehouseOffcut>())
                .Where(w => w.DiameterMm == diameterMm)
                .SelectMany(w => Enumerable.Repeat(w.LengthMm, w.Quantity))
                .ToList();

            var winningPatterns = ExecuteStrategy(strategy, pieces, warehouseBars, out var chosenStrategy);
            return AnalyzeResult(diameterMm, winningPatterns, chosenStrategy);
        }

        private static List<double> ExpandDemandsToPieces(IEnumerable<CuttingDemand> demands)
        {
            var pieces = new List<double>();
            foreach (var d in demands)
            {
                if (d.LengthMm > 0.0 && d.Quantity > 0)
                {
                    pieces.AddRange(Enumerable.Repeat(d.LengthMm, d.Quantity));
                }
            }
            return pieces;
        }

        private List<CuttingPattern> ExecuteStrategy(
            SolverStrategy requested,
            List<double> pieces,
            List<double> warehouseBars,
            out SolverStrategy resolved)
        {
            if (requested != SolverStrategy.Auto)
            {
                resolved = requested;
                return RunSingleStrategy(requested, pieces, warehouseBars);
            }

            resolved = SolverStrategy.Auto;
            return RunPortfolioAuto(pieces, warehouseBars);
        }

        private List<CuttingPattern> RunSingleStrategy(
            SolverStrategy s,
            List<double> pieces,
            List<double> warehouseBars)
        {
            return s switch
            {
                SolverStrategy.DuBang0 => CuttingStockStrategies.SolveDuBang0(pieces, StandardStockLengthMm, SawKerfMm, warehouseBars),
                SolverStrategy.LapDayCanhDai => CuttingStockStrategies.SolveBfd(pieces, StandardStockLengthMm, SawKerfMm, warehouseBars),
                SolverStrategy.BeamSearchTopK => CuttingStockStrategies.SolveBeamSearchTopK(pieces, StandardStockLengthMm, SawKerfMm, warehouseBars),
                SolverStrategy.PatternCat => CuttingStockStrategies.SolvePatternCat(pieces, StandardStockLengthMm, SawKerfMm, warehouseBars),
                _ => CuttingStockStrategies.SolveBfd(pieces, StandardStockLengthMm, SawKerfMm, warehouseBars)
            };
        }

        private List<CuttingPattern> RunPortfolioAuto(List<double> pieces, List<double> warehouseBars)
        {
            var candidates = new List<(SolverStrategy Strategy, List<CuttingPattern> Patterns)>();
            var strategies = new[]
            {
                SolverStrategy.DuBang0,
                SolverStrategy.LapDayCanhDai,
                SolverStrategy.BeamSearchTopK,
                SolverStrategy.PatternCat
            };

            foreach (var st in strategies)
            {
                var copyPieces = new List<double>(pieces);
                var candidate = RunSingleStrategy(st, copyPieces, warehouseBars);
                candidates.Add((st, candidate));
            }

            return PickWinningPatterns(candidates);
        }

        private List<CuttingPattern> PickWinningPatterns(List<(SolverStrategy Strategy, List<CuttingPattern> Patterns)> candidates)
        {
            double bestPenalty = double.MaxValue;
            List<CuttingPattern> bestPatterns = candidates[0].Patterns;

            foreach (var cand in candidates)
            {
                int newBars = cand.Patterns.Count(p => !p.IsWarehouseBar);
                double scrapMm = cand.Patterns
                    .Where(p => p.RemnantLengthMm < MinRemnantLengthMm)
                    .Sum(p => p.RemnantLengthMm);

                double netWastePercent = CalculateNetWaste(newBars, scrapMm, cand.Patterns);
                double penalty = netWastePercent + 0.1 * newBars;

                if (penalty < bestPenalty)
                {
                    bestPenalty = penalty;
                    bestPatterns = cand.Patterns;
                }
            }

            return bestPatterns;
        }

        private CuttingStockResult AnalyzeResult(int diameterMm, List<CuttingPattern> patterns, SolverStrategy strategy)
        {
            var newOffcuts = new List<WarehouseOffcut>();
            double totalStockMm = 0.0;
            double totalRemnantMm = 0.0;
            double unrecoverableScrapMm = 0.0;

            foreach (var pat in patterns)
            {
                totalStockMm += pat.StockLengthMm;
                totalRemnantMm += pat.RemnantLengthMm;

                if (pat.RemnantLengthMm >= MinRemnantLengthMm)
                {
                    pat.IsReusableRemnant = true;
                    newOffcuts.Add(new WarehouseOffcut(diameterMm, pat.RemnantLengthMm, 1));
                }
                else
                {
                    pat.IsReusableRemnant = false;
                    unrecoverableScrapMm += pat.RemnantLengthMm;
                }
            }

            int newBars = patterns.Count(p => !p.IsWarehouseBar);
            double grossWaste = totalStockMm > 0.0 ? (totalRemnantMm / totalStockMm) * 100.0 : 0.0;
            double netWaste = CalculateNetWaste(newBars, unrecoverableScrapMm, patterns);

            return new CuttingStockResult
            {
                DiameterMm = diameterMm,
                Patterns = patterns,
                NewWarehouseOffcuts = GroupOffcuts(newOffcuts),
                GrossWastePercent = Math.Round(grossWaste, 2),
                NetWastePercent = Math.Round(netWaste, 2),
                StrategyUsed = strategy
            };
        }

        private double CalculateNetWaste(int newBars, double scrapMm, List<CuttingPattern> patterns)
        {
            if (newBars > 0)
            {
                return (scrapMm / (newBars * StandardStockLengthMm)) * 100.0;
            }

            if (Math.Abs(scrapMm) < 1e-4)
            {
                return 0.0;
            }

            double totalConsumedStock = patterns.Sum(p => p.StockLengthMm);
            return totalConsumedStock > 0.0 ? (scrapMm / totalConsumedStock) * 100.0 : 0.0;
        }

        private static List<WarehouseOffcut> GroupOffcuts(List<WarehouseOffcut> list)
        {
            return list
                .GroupBy(w => new { w.DiameterMm, Length = Math.Round(w.LengthMm) })
                .Select(g => new WarehouseOffcut(g.Key.DiameterMm, g.Key.Length, g.Count()))
                .OrderByDescending(w => w.LengthMm)
                .ToList();
        }
    }
}
