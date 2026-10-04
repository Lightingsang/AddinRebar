using System;
using HPRebar.Core.BeamRebar.Models;

namespace HPRebar.Core.BeamRebar.Calculators;

/// <summary>How many Revit elements a beam run turns into: sizes the progress bar, not an exact count.</summary>
public static class BeamElementCount
{
    /// <summary>Stirrup groups assumed per span, whatever the layout.</summary>
    private const int StirrupRunsPerSpan = 3;

    /// <summary>Elements assumed for the skin bars of a deep run, whatever its spans.</summary>
    private const int SkinBarElements = 4;

    /// <summary>
    /// Rebar elements: three stirrup groups per span, every top and bottom main bar, two per additional support or
    /// span bar, the skin bars when the run is deep enough for them, and the hanging stirrups at each secondary beam.
    /// </summary>
    public static int Bars(
        BeamContinuousStack stack,
        BeamMainBarSpec mainBars,
        BeamAdditionalBarSpec additionalBars,
        BeamSideBarSpec sideBars,
        BeamSpecialBarSpec specialBars)
    {
        if (stack is null)
        {
            throw new ArgumentNullException(nameof(stack));
        }

        if (mainBars is null)
        {
            throw new ArgumentNullException(nameof(mainBars));
        }

        if (additionalBars is null)
        {
            throw new ArgumentNullException(nameof(additionalBars));
        }

        if (sideBars is null)
        {
            throw new ArgumentNullException(nameof(sideBars));
        }

        if (specialBars is null)
        {
            throw new ArgumentNullException(nameof(specialBars));
        }

        int count = stack.Spans.Count * StirrupRunsPerSpan;
        count += mainBars.TopCount + mainBars.BottomCount;
        count += additionalBars.SupportTopBars.Count * 2;
        count += additionalBars.SpanBottomBars.Count * 2;
        if (sideBars.AutoSkinBars && stack.MaxHeight >= sideBars.DepthThreshold)
        {
            count += SkinBarElements;
        }

        if (specialBars.EnableHangingStirrups)
        {
            count += stack.SecondaryIntersections.Count * specialBars.HangingStirrupsPerSide * 2;
        }

        return count;
    }

    /// <summary>
    /// Upper bound of the dimensions a run draws: span chain and height on the elevation, width and height per
    /// section.
    /// </summary>
    public static int Dimensions(bool onElevation, int sectionCount) => (onElevation ? 2 : 0) + sectionCount * 2;
}
