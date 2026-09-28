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

    /// <summary>Warns when two neighbouring bars of a layer leave less than max(25, d) clear between them.</summary>
    public static void CheckSpacing(List<string> warnings, string mark, IEnumerable<double> positions, double diameter, KataDetailingRules rules)
    {
        var ys = positions.OrderBy(y => y).ToList();
        double needed = diameter + rules.LayerGap(diameter, diameter);
        for (int i = 1; i < ys.Count; i++)
        {
            double clear = ys[i] - ys[i - 1] - diameter;
            if (ys[i] - ys[i - 1] + 1e-6 < needed)
            {
                warnings.Add($"Thép gia cường {mark}: khe thông thủy {clear:0} mm < {rules.LayerGap(diameter, diameter):0} mm, dầm hẹp cho số thanh này.");
                return;
            }
        }
    }
}
