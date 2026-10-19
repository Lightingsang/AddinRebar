using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace HPKata.Core.Optimization
{
    public static partial class CuttingStockStrategies
    {
        public static List<CuttingPattern> SolveDuBang0(
            List<double> pieces,
            double stockLength,
            double kerf,
            List<double> warehouseBars)
        {
            var remaining = pieces.OrderByDescending(p => p).ToList();
            var patterns = new List<CuttingPattern>();
            var sw = Stopwatch.StartNew();

            while (remaining.Count >= 2 && sw.ElapsedMilliseconds < 1000)
            {
                var exactIndices = FindExactSubset(remaining, stockLength, kerf, 5, sw);
                if (exactIndices == null || exactIndices.Count == 0)
                {
                    break;
                }

                var pat = new CuttingPattern
                {
                    StockLengthMm = stockLength,
                    IsWarehouseBar = false,
                    KerfMm = kerf
                };

                foreach (int idx in exactIndices.OrderByDescending(x => x))
                {
                    pat.CutsMm.Add(remaining[idx]);
                    remaining.RemoveAt(idx);
                }
                patterns.Add(pat);
            }

            // Cascade remaining pieces to BFD
            var bfdPatterns = SolveBfd(remaining, stockLength, kerf, warehouseBars);
            patterns.AddRange(bfdPatterns);
            return patterns;
        }

        private static List<int>? FindExactSubset(
            List<double> list,
            double target,
            double kerf,
            int maxK,
            Stopwatch sw)
        {
            int n = list.Count;
            for (int k = 2; k <= Math.Min(maxK, n); k++)
            {
                if (sw.ElapsedMilliseconds >= 1000) return null;

                var indices = new int[k];
                double pieceTarget = target - (k - 1) * kerf;
                if (pieceTarget <= 0.0) continue;

                if (SearchCombinations(list, pieceTarget, 0, 0, 0.0, indices, sw))
                {
                    return new List<int>(indices);
                }
            }
            return null;
        }

        private static bool SearchCombinations(
            List<double> list,
            double target,
            int startIdx,
            int depth,
            double currentSum,
            int[] chosen,
            Stopwatch sw)
        {
            if (sw.ElapsedMilliseconds >= 1000) return false;
            int k = chosen.Length;
            if (depth == k) return Math.Abs(currentSum - target) < 1.0;

            int rem = k - depth;
            if (startIdx > list.Count - rem) return false;

            // Mathematical pruning: upper-bound & lower-bound
            if (currentSum + (rem * list[startIdx]) < target - 1.0) return false;
            if (currentSum + (rem * list[list.Count - 1]) > target + 1.0) return false;

            for (int i = startIdx; i <= list.Count - rem; i++)
            {
                if (sw.ElapsedMilliseconds >= 1000) return false;
                if (currentSum + (rem * list[i]) < target - 1.0) break;

                double nextSum = currentSum + list[i];
                if (nextSum > target + 1.0) continue;

                chosen[depth] = i;
                if (SearchCombinations(list, target, i + 1, depth + 1, nextSum, chosen, sw))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
