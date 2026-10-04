using System;
using HPRebar.Core.ColumnRebar.Models;

namespace HPRebar.Core.ColumnRebar;

/// <summary>How many Revit rebar elements one column segment turns into — sizes the progress bar of a run.</summary>
public static class ColumnElementCount
{
    /// <summary>Tie groups + cross-tie elements + main bars.</summary>
    public static int Planned(ColumnSection section, StirrupSpec stirrups, AdditionalTieSpec ties, int barCount)
    {
        if (section is null) throw new ArgumentNullException(nameof(section));
        if (ties is null) throw new ArgumentNullException(nameof(ties));

        int runCount = StirrupDistributionCalculator.ComputeRuns(section, stirrups).Count;
        return runCount + CrossTies(section.Shape, ties, runCount) + barCount;
    }

    /// <summary>
    ///     Cross-tie elements for <paramref name="runCount"/> tie groups. On a rectangle a closed inner tie (type 0)
    ///     is one element per group unless its leg is 0, otherwise each of the NH / NV cross-ties is; a circular
    ///     column's horizontal tie is one element per group and its vertical one a pair, one on each axis.
    /// </summary>
    public static int CrossTies(SectionShape shape, AdditionalTieSpec ties, int runCount)
    {
        if (ties is null) throw new ArgumentNullException(nameof(ties));

        if (shape != SectionShape.Rectangle)
        {
            return (ties.AddH ? runCount : 0) + (ties.AddV ? 2 * runCount : 0);
        }

        var count = 0;
        if (ties.AddH)
        {
            count += RectangleTies(ties.KindH, ties.AH, ties.NH, runCount);
        }

        if (ties.AddV)
        {
            count += RectangleTies(ties.KindV, ties.AV, ties.NV, runCount);
        }

        return count;
    }

    private static int RectangleTies(CrossTieKind kind, double leg, int crossTieCount, int runCount)
    {
        if (kind != CrossTieKind.ClosedTie)
        {
            return crossTieCount * runCount;
        }

        return leg == 0 ? 0 : runCount;
    }
}
