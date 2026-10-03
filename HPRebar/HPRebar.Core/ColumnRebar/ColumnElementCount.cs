using HPRebar.Core.ColumnRebar.Models;

namespace HPRebar.Core.ColumnRebar;

/// <summary>How many Revit rebar elements one column segment turns into — sizes the progress bar of a run.</summary>
public static class ColumnElementCount
{
    /// <summary>Tie groups + cross-tie elements + main bars.</summary>
    public static int Planned(ColumnSection section, StirrupSpec stirrups, AdditionalTieSpec ties, int barCount)
    {
        int runCount = StirrupDistributionCalculator.ComputeRuns(section, stirrups).Count;
        return runCount + CrossTies(section.Shape, ties, runCount) + barCount;
    }

    /// <summary>
    ///     Cross-tie elements for <paramref name="runCount"/> tie groups. On a rectangle a closed inner tie (type 0)
    ///     is one element per group when it has a leg, otherwise each of the NH / NV cross-ties is; a circular
    ///     column's horizontal tie is one element per group and its vertical one a pair, one on each axis.
    /// </summary>
    public static int CrossTies(SectionShape shape, AdditionalTieSpec ties, int runCount)
    {
        var count = 0;

        if (shape == SectionShape.Rectangle)
        {
            if (ties.AddH) count += ties.TypeH == 0 ? ties.AH == 0 ? 0 : runCount : ties.NH * runCount;
            if (ties.AddV) count += ties.TypeV == 0 ? ties.AV == 0 ? 0 : runCount : ties.NV * runCount;
        }
        else
        {
            if (ties.AddH) count += runCount;
            if (ties.AddV) count += 2 * runCount;
        }

        return count;
    }
}
