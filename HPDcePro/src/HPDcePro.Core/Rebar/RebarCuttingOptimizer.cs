using System;
using System.Collections.Generic;
using System.Linq;

namespace HPDcePro.Core.Rebar
{
    public enum CuttingStrategy
    {
        BestFitDecreasing,
        FirstFitDecreasing,
        Auto
    }

    public sealed class CuttingDemandItem
    {
        public string Id { get; set; } = string.Empty;
        public int DiameterMm { get; set; }
        public double LengthMm { get; set; }
        public int Quantity { get; set; } = 1;

        public CuttingDemandItem(string id, int diameterMm, double lengthMm, int quantity = 1)
        {
            if (lengthMm <= 0.0)
            {
                throw new ArgumentException("Bar length must be strictly positive.", nameof(lengthMm));
            }
            Id = id;
            DiameterMm = diameterMm;
            LengthMm = lengthMm;
            Quantity = quantity;
        }
    }

    public sealed class CutPiece
    {
        public string DemandId { get; set; } = string.Empty;
        public double LengthMm { get; set; }

        public CutPiece(string demandId, double lengthMm)
        {
            DemandId = demandId;
            LengthMm = lengthMm;
        }
    }

    public sealed class CuttingPattern
    {
        public int StockBarIndex { get; set; }
        public int DiameterMm { get; set; }
        public double StockLengthMm { get; set; } = 11700.0;
        public List<CutPiece> Cuts { get; set; } = new();
        public double UsedLengthMm => Cuts.Sum(c => c.LengthMm);
        public double RemnantLengthMm => Math.Max(0.0, StockLengthMm - UsedLengthMm);
        public bool IsReusableRemnant => RemnantLengthMm >= 1500.0;
    }

    public sealed class CuttingOptimizationResult
    {
        public int DiameterMm { get; set; }
        public int TotalStockBarsUsed => Patterns.Count;
        public double TotalStockLengthMm => Patterns.Sum(p => p.StockLengthMm);
        public double TotalUsedLengthMm => Patterns.Sum(p => p.UsedLengthMm);
        public double TotalRemnantLengthMm => Patterns.Sum(p => p.RemnantLengthMm);
        public double ScrapLengthMm => Patterns.Where(p => !p.IsReusableRemnant).Sum(p => p.RemnantLengthMm);
        public double ScrapPercentage => TotalStockLengthMm > 0.0 ? (ScrapLengthMm / TotalStockLengthMm) * 100.0 : 0.0;
        public double TotalWastePercentage => TotalStockLengthMm > 0.0 ? (TotalRemnantLengthMm / TotalStockLengthMm) * 100.0 : 0.0;
        public List<CuttingPattern> Patterns { get; set; } = new();
        public CuttingStrategy StrategyUsed { get; set; }
    }

    /// <summary>
    /// 1D Cutting Stock Optimizer (ToiUuCatThep) for commercial 11,700 mm bars.
    /// Handles automatic lap splicing for bars exceeding 11.7 m and BFD/FFD heuristics.
    /// </summary>
    public static class RebarCuttingOptimizer
    {
        public const double CommercialStockLengthMm = 11700.0;
        public const double DefaultMinReusableRemnantMm = 1500.0;

        /// <summary>
        /// Automatically inserts lap splices (30d..40d) for continuous bars exceeding stock length.
        /// </summary>
        public static List<double> PartitionContinuousBar(
            double totalLengthMm,
            int diameterMm,
            double lapMultiplier = 40.0,
            double stockLengthMm = CommercialStockLengthMm)
        {
            if (totalLengthMm < 0.0)
            {
                throw new ArgumentException("Total bar length cannot be negative.", nameof(totalLengthMm));
            }
            if (stockLengthMm <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(stockLengthMm), "Stock bar length must be strictly positive.");
            }
            if (totalLengthMm == 0.0) return new List<double>();
            if (totalLengthMm <= stockLengthMm) return new List<double> { totalLengthMm };

            double lapLength = Math.Max(0.0, lapMultiplier * diameterMm);
            double effectiveAdvance = Math.Max(stockLengthMm * 0.25, stockLengthMm - lapLength);
            var segments = new List<double>();
            double remainingToCover = totalLengthMm;

            while (remainingToCover > 0)
            {
                if (remainingToCover <= stockLengthMm)
                {
                    segments.Add(remainingToCover);
                    break;
                }

                double pieceLen = stockLengthMm;
                segments.Add(pieceLen);
                remainingToCover -= effectiveAdvance;
            }

            return segments;
        }

        public static CuttingOptimizationResult Optimize(
            IEnumerable<CuttingDemandItem> demands,
            int diameterMm,
            CuttingStrategy strategy = CuttingStrategy.Auto,
            double stockLengthMm = CommercialStockLengthMm,
            double lapMultiplier = 40.0)
        {
            if (stockLengthMm <= 0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(stockLengthMm), "Stock bar length must be strictly positive.");
            }
            var pieces = ExpandAndPartitionDemands(demands, diameterMm, stockLengthMm, lapMultiplier);
            if (pieces.Count == 0)
            {
                return new CuttingOptimizationResult { DiameterMm = diameterMm, StrategyUsed = strategy };
            }

            pieces.Sort((a, b) => b.LengthMm.CompareTo(a.LengthMm));

            if (strategy == CuttingStrategy.Auto)
            {
                var bfdResult = PackBfd(pieces, diameterMm, stockLengthMm);
                var ffdResult = PackFfd(pieces, diameterMm, stockLengthMm);
                return (bfdResult.TotalStockBarsUsed <= ffdResult.TotalStockBarsUsed &&
                        bfdResult.ScrapPercentage <= ffdResult.ScrapPercentage)
                    ? bfdResult
                    : ffdResult;
            }

            return strategy == CuttingStrategy.FirstFitDecreasing
                ? PackFfd(pieces, diameterMm, stockLengthMm)
                : PackBfd(pieces, diameterMm, stockLengthMm);
        }

        private static List<CutPiece> ExpandAndPartitionDemands(
            IEnumerable<CuttingDemandItem> demands,
            int diameterMm,
            double stockLengthMm,
            double lapMultiplier)
        {
            var list = new List<CutPiece>();
            foreach (var d in demands.Where(x => x.DiameterMm == diameterMm && x.Quantity > 0 && x.LengthMm > 0))
            {
                var subLengths = PartitionContinuousBar(d.LengthMm, d.DiameterMm, lapMultiplier, stockLengthMm);
                for (int q = 0; q < d.Quantity; q++)
                {
                    for (int s = 0; s < subLengths.Count; s++)
                    {
                        string id = subLengths.Count > 1 ? $"{d.Id}_p{s + 1}" : d.Id;
                        list.Add(new CutPiece(id, subLengths[s]));
                    }
                }
            }
            return list;
        }

        private static CuttingOptimizationResult PackBfd(List<CutPiece> pieces, int diameterMm, double stockLengthMm)
        {
            var patterns = new List<CuttingPattern>();
            foreach (var piece in pieces)
            {
                CuttingPattern? bestPattern = null;
                double minRemainingAfterCut = double.MaxValue;

                foreach (var p in patterns)
                {
                    if (p.RemnantLengthMm >= piece.LengthMm)
                    {
                        double spaceAfter = p.RemnantLengthMm - piece.LengthMm;
                        if (spaceAfter < minRemainingAfterCut)
                        {
                            minRemainingAfterCut = spaceAfter;
                            bestPattern = p;
                        }
                    }
                }

                if (bestPattern != null)
                {
                    bestPattern.Cuts.Add(piece);
                }
                else
                {
                    var newPat = new CuttingPattern
                    {
                        StockBarIndex = patterns.Count + 1,
                        DiameterMm = diameterMm,
                        StockLengthMm = stockLengthMm
                    };
                    newPat.Cuts.Add(piece);
                    patterns.Add(newPat);
                }
            }

            return new CuttingOptimizationResult
            {
                DiameterMm = diameterMm,
                Patterns = patterns,
                StrategyUsed = CuttingStrategy.BestFitDecreasing
            };
        }

        private static CuttingOptimizationResult PackFfd(List<CutPiece> pieces, int diameterMm, double stockLengthMm)
        {
            var patterns = new List<CuttingPattern>();
            foreach (var piece in pieces)
            {
                var target = patterns.FirstOrDefault(p => p.RemnantLengthMm >= piece.LengthMm);
                if (target != null)
                {
                    target.Cuts.Add(piece);
                }
                else
                {
                    var newPat = new CuttingPattern
                    {
                        StockBarIndex = patterns.Count + 1,
                        DiameterMm = diameterMm,
                        StockLengthMm = stockLengthMm
                    };
                    newPat.Cuts.Add(piece);
                    patterns.Add(newPat);
                }
            }

            return new CuttingOptimizationResult
            {
                DiameterMm = diameterMm,
                Patterns = patterns,
                StrategyUsed = CuttingStrategy.FirstFitDecreasing
            };
        }
    }
}
