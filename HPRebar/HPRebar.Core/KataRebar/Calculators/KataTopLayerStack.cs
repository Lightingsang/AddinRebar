using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>One bar level over a support: row 13 shares the main-bar level, rows 14-16 stack below it.</summary>
/// <param name="Row">Sheet row (13-16); the main bars share row 13's level.</param>
/// <param name="Z">Centre of the bars (mm, beam top = 0).</param>
/// <param name="Diameter">Largest bar of the level.</param>
/// <param name="Inset">How far the level's end bends sit inboard of the main bars' bends (mm).</param>
public sealed record KataTopLevel(int Row, double Z, double Diameter, double Inset);

/// <summary>
/// Stacks the additional-bar levels over a support: the main bars' level first, then each filled row below the
/// previous one at the rules' layer gap, max(30, d); empty rows take no room. A lower level's bends sit inboard of the
/// level above by the distance between their centres, so the legs never cross.
/// </summary>
public static class KataTopLayerStack
{
    public const int FirstRow = 13;

    /// <param name="spanSide">+1: only the bars reaching into the span on the right count (first support);
    /// −1: only those on the left (last support); 0: both.</param>
    public static IReadOnlyList<KataTopLevel> At(KataBeamRebarSpec spec, KataDetailingRules rules, int support, int spanSide)
    {
        double mainZ = -rules.TopBarCentreDepth;
        double mainD = spec.TopContinuous.IsEmpty ? 0.0 : spec.TopContinuous.Diameter;
        var levels = new List<KataTopLevel>();

        double firstD = Math.Max(mainD, MaxDiameter(Sides(spec, support, 0), spanSide));
        levels.Add(new KataTopLevel(FirstRow, mainZ, firstD, 0.0));

        for (int layer = 1; layer < 4; layer++)
        {
            double d = MaxDiameter(Sides(spec, support, layer), spanSide);
            if (d <= 0.0) continue;

            var above = levels[levels.Count - 1];
            double z = above.Z - (above.Diameter / 2.0 + rules.LayerGap(above.Diameter, d) + d / 2.0);
            levels.Add(new KataTopLevel(FirstRow + layer, z, d, mainZ - z));
        }

        return levels;
    }

    /// <summary>Bars of row 13 + <paramref name="layer"/> over a support; a spec built without sides uses the layer lists on both sides.</summary>
    public static KataSideBars Sides(KataBeamRebarSpec spec, int support, int layer)
    {
        if (support < 0 || support >= spec.Supports.Count) return KataSideBars.None;

        var s = spec.Supports[support];
        if (layer < s.TopExtraSides.Count) return s.TopExtraSides[layer];

        var items = s.AllTopExtraLayers[layer];
        return items.Count == 0 ? KataSideBars.None : new KataSideBars(items, items);
    }

    private static double MaxDiameter(KataSideBars sides, int spanSide)
    {
        var items = spanSide > 0 ? sides.Right : spanSide < 0 ? sides.Left : sides.Left.Concat(sides.Right).ToList();
        return items.Count == 0 ? 0.0 : items.Max(i => i.Diameter);
    }
}
