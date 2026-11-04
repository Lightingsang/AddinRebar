using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Calculators;

namespace HPRebar.Core.KataRebar.ColumnStack;

/// <summary>
/// The dimension chains of a column section view, in section mm (X along b, "up" = Y along h): under the section the
/// bars of the b face between the two faces, then b overall; right of it the bars of the h face, then h overall. A round
/// section gets its diameter only.
/// </summary>
public static class KataColumnSectionDims
{
    private const double MinGapMm = 50;
    private const double GapFactor = 0.15;
    private const double RowToleranceMm = 1;

    public static IReadOnlyList<KataDimChain> Chains(KataColumnSectionDrawing d)
    {
        double w = d.WidthMm, h = d.DepthMm;
        double gap = Math.Max(MinGapMm, GapFactor * Math.Max(w, h));
        if (d.IsRound)
        {
            return new[] { Chain(false, -gap, (0, h / 2), (w, h / 2)) };
        }

        var chains = new List<KataDimChain>();
        if (d.Bars.Count > 0)
        {
            double bottom = d.Bars.Min(b => b.Y);
            var row = d.Bars.Where(b => b.Y <= bottom + RowToleranceMm).OrderBy(b => b.X).Select(b => (b.X, b.Y));
            chains.Add(Chain(false, -gap, new[] { (0.0, 0.0) }.Concat(row).Append((w, 0.0)).ToArray()));

            double right = d.Bars.Max(b => b.X);
            var column = d.Bars.Where(b => b.X >= right - RowToleranceMm).OrderBy(b => b.Y).Select(b => (b.Y, b.X));
            chains.Add(Chain(true, w + gap, new[] { (0.0, w) }.Concat(column).Append((h, w)).ToArray()));
        }

        chains.Add(Chain(false, -2 * gap, (0, 0), (w, 0)));
        chains.Add(Chain(true, w + 2 * gap, (0, w), (h, w)));
        return chains;
    }

    /// <param name="stations">(along, across) pairs in order; stations closer than a millimetre are merged.</param>
    private static KataDimChain Chain(bool vertical, double lineAt, params (double Along, double Across)[] stations)
    {
        var kept = new List<KataDimStation>();
        foreach (var (along, across) in stations)
        {
            if (kept.Count == 0 || along - kept[^1].Along > RowToleranceMm)
            {
                kept.Add(new KataDimStation(along, across));
            }
        }

        return new KataDimChain(vertical, lineAt, kept);
    }
}
