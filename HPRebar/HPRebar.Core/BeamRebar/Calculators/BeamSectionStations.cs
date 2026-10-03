using System;
using System.Collections.Generic;
using HPRebar.Core.BeamRebar.Models;

namespace HPRebar.Core.BeamRebar.Calculators;

/// <summary>
/// Where the section views of a beam cut it, along the run axis (mm): a cantilever, or a span asked for one
/// section or fewer, is cut once at mid-span; two sections cut the left support zone (a sixth of the clear span)
/// and mid-span; three or more cut those and the right support zone (five sixths).
/// </summary>
public static class BeamSectionStations
{
    /// <summary>The cut stations of one span along the run axis (mm), measured from the run origin.</summary>
    public static IReadOnlyList<double> ForSpan(BeamSpan span, int sectionsPerSpan)
    {
        if (span is null)
        {
            throw new ArgumentNullException(nameof(span));
        }

        double midspan = span.StartX + span.LengthClear * 0.5;
        if (span.IsCantilever || sectionsPerSpan <= 1)
        {
            return new[] { midspan };
        }

        double leftSupportZone = span.StartX + span.LengthClear / 6.0;
        if (sectionsPerSpan == 2)
        {
            return new[] { leftSupportZone, midspan };
        }

        double rightSupportZone = span.StartX + span.LengthClear * 5.0 / 6.0;
        return new[] { leftSupportZone, midspan, rightSupportZone };
    }

    /// <summary>How many sections a run cuts, over all its spans.</summary>
    public static int Count(IEnumerable<BeamSpan> spans, int sectionsPerSpan)
    {
        if (spans is null)
        {
            throw new ArgumentNullException(nameof(spans));
        }

        int count = 0;
        foreach (var span in spans)
        {
            count += ForSpan(span, sectionsPerSpan).Count;
        }

        return count;
    }
}
