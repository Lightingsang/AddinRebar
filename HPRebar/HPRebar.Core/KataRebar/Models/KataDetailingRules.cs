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
    public double MinimumLegFactor { get; init; } = 15.0;

    /// <summary>Bent legs are rounded up to a multiple of this length when the rounded leg still fits (0 = no rounding).</summary>
    public double RoundLegMm { get; init; } = 25.0;

    /// <summary>Clear gap kept between a bottom-bar leg moved inboard and the top-bar leg beside it.</summary>
    public double MinimumLegGap { get; init; } = 25.0;

    /// <summary>Smallest clear gap between two bar layers; the larger bar diameter wins when bigger.</summary>
    public double LayerClearGap { get; init; } = 30.0;

    /// <summary>Clear gap between two bar layers of the given diameters.</summary>
    public double LayerGap(double upperDiameter, double lowerDiameter) =>
        Math.Max(LayerClearGap, Math.Max(upperDiameter, lowerDiameter));

    /// <summary>Smallest clear gap between two neighbouring bars of one layer; the bar diameter wins when bigger.</summary>
    public double BarClearSpacing { get; init; } = 25.0;

    /// <summary>Clear gap wanted between two neighbouring bars of one layer.</summary>
    public double BarGap(double diameter) => Math.Max(BarClearSpacing, diameter);

    /// <summary>
    /// Stagger of the additional top bars over a support (G1, else the settings): each filled layer reaches at
    /// least this much further into the span than the next filled layer inside it.
    /// </summary>
    public double CurtailedExtension { get; init; } = 500.0;

    /// <summary>
    /// Where the additional bottom bars of a span (rows 17-18) stop, as a fraction of the clear span measured
    /// from each support face. The sheet has no cell for it.
    /// </summary>
    public double BottomExtraCutFraction { get; init; } = 0.15;

    /// <summary>First and last stirrup of a span, measured from the support faces.</summary>
    public double FirstStirrupOffset { get; init; } = 50.0;

    /// <summary>
    /// Dense stirrup zone at each support: max(<see cref="DenseZoneHeightFactor"/> × h, <see cref="EndZoneFraction"/>
    /// × clear span), never more than half the span.
    /// </summary>
    public double EndZoneFraction { get; init; } = 0.25;

    public double DenseZoneHeightFactor { get; init; } = 2.0;

    /// <summary>Length of the dense stirrup zone at each support of a span.</summary>
    public double DenseZoneLength(double clearSpan, double beamHeight) =>
        Math.Min(clearSpan / 2.0, Math.Max(DenseZoneHeightFactor * beamHeight, EndZoneFraction * clearSpan));

    /// <summary>A beam at least this deep with no side bars is reported (TCVN 5574:2018 § 10.3.1.2); 0 = never.</summary>
    public double SideBarRequiredHeight { get; init; } = 700.0;

    /// <summary>Warnings about the inputs (a cover that had to be raised, a thin stirrup cover...).</summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>Inputs that make any layout meaningless; nothing is drawn while there is one.</summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>Plan offset of the outermost bars of a layer: they touch the stirrup's inner face.</summary>
    public double EdgeBarOffset(double beamWidth, double barDiameter) =>
        beamWidth / 2.0 - StirrupCover - StirrupDiameter - barDiameter / 2.0;
}
