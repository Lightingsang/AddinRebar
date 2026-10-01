using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>Transverse positions of additional bars and the clear-spacing check of a layer.</summary>
public static class KataLayerPositions
{
    /// <summary>
    /// Positions for <paramref name="count"/> bars sharing the main bars' level: the main bars keep their places
    /// and the new bars split the gaps between them evenly, the extra ones going to the gaps nearest the beam's
    /// centre line. With fewer than two main bars there is no gap, so the bars spread over the whole width.
    /// </summary>
    public static IReadOnlyList<double> BetweenMainBars(IReadOnlyList<double> mainY, int count, double width, KataDetailingRules rules, double diameter)
    {
        if (count <= 0) return Array.Empty<double>();
        if (mainY.Count < 2)
            return KataRebarCalculator.ComputeTransverseYPositions(width, rules.StirrupCover, rules.StirrupDiameter, diameter, count);

        var sorted = mainY.OrderBy(y => y).ToList();
        int gaps = sorted.Count - 1;
        var perGap = Enumerable.Repeat(count / gaps, gaps).ToArray();
        foreach (int g in Enumerable.Range(0, gaps)
                     .OrderBy(g => Math.Abs((sorted[g] + sorted[g + 1]) / 2.0))
                     .ThenBy(g => g)
                     .Take(count % gaps))
            perGap[g]++;

        var result = new List<double>(count);
        for (int g = 0; g < gaps; g++)
        {
            for (int j = 1; j <= perGap[g]; j++)
                result.Add(sorted[g] + (sorted[g + 1] - sorted[g]) * j / (perGap[g] + 1));
        }

        return result;
    }

    /// <summary>
    /// Warns when two neighbouring bars of a layer leave less than max(25, d) clear between them (the rules' bar
    /// gap), and blocks
    /// when they would overlap: Revit would draw one bar through the other.
    /// </summary>
    public static void CheckSpacing(List<string> warnings, List<string> blocking, string mark, IEnumerable<double> positions, double diameter, KataDetailingRules rules)
    {
        var ys = positions.OrderBy(y => y).ToList();
        double needed = diameter + rules.BarGap(diameter);
        for (int i = 1; i < ys.Count; i++)
        {
            double clear = ys[i] - ys[i - 1] - diameter;
            if (clear < -1e-6)
            {
                blocking.Add($"Thép gia cường {mark}: hai thanh chồng lên nhau (tâm cách {ys[i] - ys[i - 1]:0} mm) — đổi số thanh của lớp.");
                return;
            }

            if (ys[i] - ys[i - 1] + 1e-6 < needed)
            {
                warnings.Add($"Thép gia cường {mark}: khe thông thủy {clear:0} mm < {rules.BarGap(diameter):0} mm, dầm hẹp cho số thanh này.");
                return;
            }
        }
    }

    /// <summary>
    /// Partitions <paramref name="allY"/> (length N = <paramref name="countLeft"/> + <paramref name="countRight"/>)
    /// into two interleaved sets of transverse positions, symmetrically distributed about the centerline.
    /// The higher priority side takes outer slots.
    /// </summary>
    public static (IReadOnlyList<double> Left, IReadOnlyList<double> Right) PartitionInterleaved(
        IReadOnlyList<double> allY, int countLeft, int countRight, bool leftPriority)
    {
        if (countLeft <= 0) return (Array.Empty<double>(), allY);
        if (countRight <= 0) return (allY, Array.Empty<double>());

        int n = allY.Count;
        if (n != countLeft + countRight)
            throw new ArgumentException($"allY count ({n}) must equal countLeft ({countLeft}) + countRight ({countRight})");

        var leftIndices = new HashSet<int>();
        var rightIndices = new HashSet<int>();

        int remainingLeft = countLeft;
        int remainingRight = countRight;

        // If N is odd, exactly one side is odd. That side takes the center slot (y = 0).
        if (n % 2 != 0)
        {
            int center = n / 2;
            if (remainingLeft % 2 != 0)
            {
                leftIndices.Add(center);
                remainingLeft--;
            }
            else
            {
                rightIndices.Add(center);
                remainingRight--;
            }
        }

        int numPairs = n / 2;
        var pairIndices = new List<(int Low, int High)>();
        for (int p = 0; p < numPairs; p++)
            pairIndices.Add((p, n - 1 - p));

        // If both remaining counts are odd (N even, countLeft and countRight both odd):
        // Assign the innermost pair (one to Left, one to Right) so remaining counts become even.
        if (remainingLeft % 2 != 0 && remainingRight % 2 != 0 && pairIndices.Count > 0)
        {
            var innerPair = pairIndices[pairIndices.Count - 1];
            pairIndices.RemoveAt(pairIndices.Count - 1);
            if (leftPriority)
            {
                leftIndices.Add(innerPair.Low);
                rightIndices.Add(innerPair.High);
            }
            else
            {
                rightIndices.Add(innerPair.Low);
                leftIndices.Add(innerPair.High);
            }
            remainingLeft--;
            remainingRight--;
        }

        int pairsLeft = remainingLeft / 2;
        int pairsRight = remainingRight / 2;

        for (int p = 0; p < pairIndices.Count; p++)
        {
            var pair = pairIndices[p];
            bool assignToLeft;
            if (pairsLeft > 0 && pairsRight > 0)
            {
                assignToLeft = p % 2 == 0 ? leftPriority : !leftPriority;
                if (assignToLeft && pairsLeft > 0)
                {
                    leftIndices.Add(pair.Low);
                    leftIndices.Add(pair.High);
                    pairsLeft--;
                }
                else
                {
                    rightIndices.Add(pair.Low);
                    rightIndices.Add(pair.High);
                    pairsRight--;
                }
            }
            else if (pairsLeft > 0)
            {
                leftIndices.Add(pair.Low);
                leftIndices.Add(pair.High);
                pairsLeft--;
            }
            else
            {
                rightIndices.Add(pair.Low);
                rightIndices.Add(pair.High);
                pairsRight--;
            }
        }

        var left = leftIndices.OrderBy(i => i).Select(i => allY[i]).ToArray();
        var right = rightIndices.OrderBy(i => i).Select(i => allY[i]).ToArray();
        return (left, right);
    }
}
