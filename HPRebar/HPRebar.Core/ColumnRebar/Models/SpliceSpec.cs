namespace HPRebar.Core.ColumnRebar.Models;

/// <summary>
///     Per-bar splice settings. Millimetres.
///     Top: <see cref="TopStyle"/> — the bar bends across into the column above, or stops under the beam,
///     optionally with a horizontal hook of <see cref="LaTop"/>.
///     Bottom: <see cref="BottomStyle"/> — the bar simply starts <see cref="LcBottom"/> above the segment
///     base, or runs down past the base by <see cref="LbBottom"/>.
/// </summary>
public sealed record SpliceSpec
{
    public bool IsTopDowels { get; init; } = true;

    public TopDowelStyle TopStyle { get; init; }

    /// <summary>Horizontal hook at the top. Sign picks the bend direction; zero means no hook.</summary>
    public double LaTop { get; init; }

    /// <summary>Anchorage above the segment top when the bar carries on into the column above.</summary>
    public double LbTop { get; init; }

    public bool IsBottomDowels { get; init; }

    public BottomDowelStyle BottomStyle { get; init; }

    /// <summary>Horizontal hook at the bottom. Sign picks the bend direction; zero means no hook.</summary>
    public double LaBottom { get; init; }

    /// <summary>Extension below the segment base.</summary>
    public double LbBottom { get; init; }

    /// <summary>Start height above the segment base when the bar does not run down.</summary>
    public double LcBottom { get; init; }

    /// <summary>Defaults matching the source tool: top dowels on, staggered lap, no bottom dowels.</summary>
    public static SpliceSpec Default(int barNumber, double barDiameter, double splitOverlap, double overlapFactor)
    {
        var lap = overlapFactor * barDiameter;

        return new SpliceSpec
        {
            IsTopDowels = true,
            TopStyle = TopDowelStyle.BendIntoColumnAbove,
            LaTop = lap,
            LbTop = DefaultOverlap.LapLength(barNumber, barDiameter, splitOverlap, overlapFactor),
            IsBottomDowels = false,
            BottomStyle = BottomDowelStyle.StartAboveBase,
            LaBottom = lap,
            LbBottom = lap,
            LcBottom = lap
        };
    }
}
