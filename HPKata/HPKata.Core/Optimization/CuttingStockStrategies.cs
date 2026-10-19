using System;
using System.Collections.Generic;
using System.Linq;

namespace HPKata.Core.Optimization
{
    /// <summary>
    /// Implementation of the 5 cutting stock optimization heuristics.
    /// </summary>
    public static partial class CuttingStockStrategies
    {
        public static List<CuttingPattern> SolveBfd(
            List<double> pieces,
            double stockLength,
            double kerf,
            List<double> warehouseBars)
        {
            var sortedPieces = pieces.OrderByDescending(p => p).ToList();
            var patterns = new List<CuttingPattern>();

            // Pack into available warehouse offcuts first
            var availableWarehouse = warehouseBars.OrderBy(w => w).ToList();
            foreach (double wBar in availableWarehouse)
            {
                var pat = PackSingleBar(sortedPieces, wBar, kerf, true);
                if (pat.CutsMm.Count > 0)
                {
                    patterns.Add(pat);
                }
            }

            // Pack remaining into standard stock bars
            while (sortedPieces.Count > 0)
            {
                var pat = PackSingleBar(sortedPieces, stockLength, kerf, false);
                if (pat.CutsMm.Count == 0)
                {
                    // Single piece larger than stockLength; place alone
                    double oversized = sortedPieces[0];
                    sortedPieces.RemoveAt(0);
                    patterns.Add(new CuttingPattern
                    {
                        StockLengthMm = Math.Max(stockLength, oversized),
                        IsWarehouseBar = false,
                        CutsMm = new List<double> { oversized },
                        KerfMm = kerf
                    });
                }
                else
                {
                    patterns.Add(pat);
                }
            }

            return patterns;
        }

        private static CuttingPattern PackSingleBar(List<double> pieces, double capacity, double kerf, bool isWarehouse)
        {
            var pat = new CuttingPattern
            {
                StockLengthMm = capacity,
                IsWarehouseBar = isWarehouse,
                KerfMm = kerf
            };

            int i = 0;
            while (i < pieces.Count)
            {
                double piece = pieces[i];
                double needed = pat.CutsMm.Count == 0 ? piece : piece + kerf;
                if (pat.UsedLengthMm + needed <= capacity + 1e-4)
                {
                    pat.CutsMm.Add(piece);
                    pieces.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }

            return pat;
        }

        public static List<CuttingPattern> SolvePatternCat(
            List<double> pieces,
            double stockLength,
            double kerf,
            List<double> warehouseBars)
        {
            var remaining = pieces.OrderByDescending(p => p).ToList();
            var patterns = new List<CuttingPattern>();

            // Common standard cuts of 11,700 mm
            ExtractModularDividers(remaining, patterns, stockLength, kerf, 5850.0, 2);
            ExtractModularDividers(remaining, patterns, stockLength, kerf, 3900.0, 3);
            ExtractModularDividers(remaining, patterns, stockLength, kerf, 2925.0, 4);
            ExtractPairPattern(remaining, patterns, stockLength, kerf, 6000.0, 5700.0);

            // Cascade remainder to BFD
            patterns.AddRange(SolveBfd(remaining, stockLength, kerf, warehouseBars));
            return patterns;
        }

        private static void ExtractModularDividers(
            List<double> list,
            List<CuttingPattern> patterns,
            double stockLength,
            double kerf,
            double pieceLen,
            int count)
        {
            double totalNeeded = count * pieceLen + (count > 1 ? (count - 1) * kerf : 0.0);
            if (totalNeeded > stockLength + 1e-4)
            {
                return;
            }

            while (true)
            {
                var indices = new List<int>();
                for (int i = 0; i < list.Count; i++)
                {
                    if (Math.Abs(list[i] - pieceLen) < 1.0)
                    {
                        indices.Add(i);
                        if (indices.Count == count) break;
                    }
                }

                if (indices.Count == count)
                {
                    var pat = new CuttingPattern { StockLengthMm = stockLength, KerfMm = kerf };
                    for (int j = indices.Count - 1; j >= 0; j--)
                    {
                        pat.CutsMm.Add(list[indices[j]]);
                        list.RemoveAt(indices[j]);
                    }
                    patterns.Add(pat);
                }
                else
                {
                    break;
                }
            }
        }

        private static void ExtractPairPattern(
            List<double> list,
            List<CuttingPattern> patterns,
            double stockLength,
            double kerf,
            double len1,
            double len2)
        {
            double totalNeeded = len1 + len2 + kerf;
            if (totalNeeded > stockLength + 1e-4)
            {
                return;
            }

            while (true)
            {
                int idx1 = list.FindIndex(x => Math.Abs(x - len1) < 1.0);
                int idx2 = list.FindIndex(x => Math.Abs(x - len2) < 1.0);

                if (idx1 >= 0 && idx2 >= 0 && idx1 != idx2)
                {
                    var pat = new CuttingPattern { StockLengthMm = stockLength, KerfMm = kerf };
                    pat.CutsMm.Add(list[idx1]);
                    pat.CutsMm.Add(list[idx2]);
                    patterns.Add(pat);

                    int maxIdx = Math.Max(idx1, idx2);
                    int minIdx = Math.Min(idx1, idx2);
                    list.RemoveAt(maxIdx);
                    list.RemoveAt(minIdx);
                }
                else
                {
                    break;
                }
            }
        }

        public static List<CuttingPattern> SolveBeamSearchTopK(
            List<double> pieces,
            double stockLength,
            double kerf,
            List<double> warehouseBars,
            int topK = 20)
        {
            // Beam Search uses diversified BFD sort orders and picks highest packing density
            var bestPatterns = SolveBfd(new List<double>(pieces), stockLength, kerf, warehouseBars);
            double bestDensity = CalculateDensity(bestPatterns);

            var random = new Random(42);
            for (int k = 0; k < Math.Min(topK, 25); k++)
            {
                var shuffled = pieces.OrderBy(_ => random.Next()).ToList();
                var candidate = SolveBfd(shuffled, stockLength, kerf, warehouseBars);
                double density = CalculateDensity(candidate);
                if (density > bestDensity)
                {
                    bestDensity = density;
                    bestPatterns = candidate;
                }
            }

            return bestPatterns;
        }

        private static double CalculateDensity(List<CuttingPattern> patterns)
        {
            if (patterns.Count == 0) return 0.0;
            double totalUsed = patterns.Sum(p => p.UsedLengthMm);
            double totalStock = patterns.Sum(p => p.StockLengthMm);
            return totalStock > 0.0 ? totalUsed / totalStock : 0.0;
        }
    }
}
