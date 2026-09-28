using System;
using System.Collections.Generic;

namespace HPRebar.Core.KataRebar.Models;

/// <summary>
/// Resolved placement rules for one beam, in millimetres. Every number the layout uses comes from here so
/// the sheet's cells (J9, G2, G3, G6) and the office defaults are applied in one place.
/// </summary>
public sealed record KataDetailingRules
{
    /// <summary>Distance from the beam top to the centre of the top main bars.</summary>
    public double TopBarCentreDepth { get; init; }

    /// <summary>Distance from the beam soffit to the centre of the bottom main bars.</summary>
    public double BottomBarCentreDepth { get; init; }

    /// <summary>Clear cover to the outer face of the stirrups, on all four faces.</summary>
    public double StirrupCover { get; init; }

    public double StirrupDiameter { get; init; }

    /// <summary>Anchorage of the top bars in diameters (G2, tension zone over the supports).</summary>
    public double TopAnchorageFactor { get; init; }

    /// <summary>Anchorage of the bottom bars in diameters (G3, compression zone at the supports).</summary>
    public double BottomAnchorageFactor { get; init; }

    /// <summary>Anchorage factor for beam side bars into supports in diameters (default 10d in Kata settings).</summary>
    public double SideBarAnchorageFactor { get; init; } = 10.0;

    /// <summary>Longest stock bar; a continuous bar beyond it is reported (laps are not drawn yet).</summary>
    public double MaxBarLength { get; init; } = 11700.0;

    /// <summary>Additional bars are cut on multiples of this length (mm).</summary>
    public double RoundCutExtraMm { get; init; } = 50.0;

    /// <summary>Spacing of the C ties holding the side bars (mm).</summary>
    public double SideBarTieSpacing { get; init; } = 400.0;

    public int ClosedStirrupHookAngle { get; init; } = 135;
    public double ClosedStirrupHookFactor { get; init; } = 7.5;
    public int CrossTieHookAngle { get; init; } = 180;
    public double CrossTieHookFactor { get; init; } = 7.5;

    /// <summary>A reach rounded up to the cut increment, so a rounded bar is never shorter.</summary>
    public double RoundUp(double length) =>
        RoundCutExtraMm > 0.0 ? Math.Ceiling(length / RoundCutExtraMm - 1e-9) * RoundCutExtraMm : length;

    /// <summary>A distance kept free of a bar, rounded down to the cut increment.</summary>
    public double RoundDown(double length) =>
        RoundCutExtraMm > 0.0 ? Math.Floor(length / RoundCutExtraMm + 1e-9) * RoundCutExtraMm : length;

    /// <summary>Shortest bent leg of an anchorage, in diameters.</summary>
    public double MinimumLegFactor { get; init; } = 10.0;

    /// <summary>Clear gap kept between a bottom-bar leg moved inboard and the top-bar leg beside it.</summary>
    public double MinimumLegGap { get; init; } = 25.0;

    /// <summary>Smallest clear gap between two bar layers; the larger bar diameter wins when bigger.</summary>
    public double LayerClearGap { get; init; } = 25.0;

    /// <summary>Clear gap between two bar layers of the given diameters.</summary>
    public double LayerGap(double upperDiameter, double lowerDiameter) =>
        Math.Max(LayerClearGap, Math.Max(upperDiameter, lowerDiameter));

    /// <summary>
    /// Where the additional bottom bars of a span (rows 17-18) stop, as a fraction of the clear span measured
    /// from each support face. The sheet has no cell for it.
    /// </summary>
    public double BottomExtraCutFraction { get; init; } = 1.0 / 7.0;

    /// <summary>First and last stirrup of a span, measured from the support faces.</summary>
    public double FirstStirrupOffset { get; init; } = 50.0;

    /// <summary>Length of each dense end zone as a fraction of the clear span.</summary>
    public double EndZoneFraction { get; init; } = 0.25;

    /// <summary>Warnings about the inputs (a cover that had to be raised, a thin stirrup cover...).</summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>Inputs that make any layout meaningless; nothing is drawn while there is one.</summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>Plan offset of the outermost bars of a layer: they touch the stirrup's inner face.</summary>
    public double EdgeBarOffset(double beamWidth, double barDiameter) =>
        beamWidth / 2.0 - StirrupCover - StirrupDiameter - barDiameter / 2.0;
}
