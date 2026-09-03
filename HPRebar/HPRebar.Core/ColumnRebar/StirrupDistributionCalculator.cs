using System;
using System.Collections.Generic;
using HPRebar.Core.ColumnRebar.Models;

namespace HPRebar.Core.ColumnRebar;

/// <summary>
///     Splits the tie run of one column segment into evenly spaced groups. All millimetres.
/// </summary>
public static class StirrupDistributionCalculator
{
    /// <summary>
    ///     Length of the tie run. Ties normally stop under the beam; <see cref="StirrupSpec.IsTiesUp"/>
    ///     carries them back up through the beam depth.
    /// </summary>
    public static double ComputeRunLength(ColumnSection section, bool tiesUp)
    {
        if (section is null) throw new ArgumentNullException(nameof(section));

        var clear = section.Hc - section.Hb - section.Zb;

        return tiesUp ? clear + section.Hb + section.Zb : clear;
    }

    /// <summary>
    ///     Zone lengths for a dense/sparse/dense layout. Both are zero for the even layout (TypeDis 0),
    ///     and the second dense zone always repeats the first.
    /// </summary>
    public static (double L1, double L2) ComputeZones(double runLength, int typeDis)
    {
        switch (typeDis)
        {
            case 1: return (runLength / 4, runLength / 2);
            case 2: return (runLength / 6, runLength * 4.0 / 6);
            case 3: return (runLength / 8, runLength * 6.0 / 8);
            default: return (0d, 0d);
        }
    }

    /// <summary>
    ///     Tie groups for the run, ordered from the base up. The even layout returns one group centred in
    ///     the run; the zoned layouts return dense, sparse, dense.
    /// </summary>
    public static IReadOnlyList<StirrupRun> Compute(double runLength, StirrupSpec spec)
    {
        if (spec is null) throw new ArgumentNullException(nameof(spec));

        if (spec.TypeDis == 0)
        {
            RequirePositiveSpacing(spec.S, nameof(spec.S));

            var count = (int)(runLength / spec.S) + 1;

            return new[]
            {
                new StirrupRun
                {
                    Count = count,
                    Spacing = spec.S,
                    StartOffset = (runLength - (count - 1) * spec.S) / 2
                }
            };
        }

        RequirePositiveSpacing(spec.S1, nameof(spec.S1));
        RequirePositiveSpacing(spec.S2, nameof(spec.S2));

        var (l1, l2) = ComputeZones(runLength, spec.TypeDis);

        var denseCount = (int)(l1 / spec.S1) + 1;
        var sparseCount = (int)(l2 / spec.S2) + 1;
        var denseSlack = (l1 - (denseCount - 1) * spec.S1) / 2;
        var sparseSlack = (l2 - (sparseCount - 1) * spec.S2) / 2;

        return new[]
        {
            new StirrupRun { Count = denseCount, Spacing = spec.S1, StartOffset = denseSlack },
            new StirrupRun { Count = sparseCount, Spacing = spec.S2, StartOffset = sparseSlack + l1 },
            new StirrupRun { Count = denseCount, Spacing = spec.S1, StartOffset = denseSlack + l1 + l2 }
        };
    }

    private static void RequirePositiveSpacing(double spacing, string name)
    {
        if (spacing <= 0)
        {
            throw new ArgumentOutOfRangeException(name, spacing, "Tie spacing must be greater than zero.");
        }
    }
}
