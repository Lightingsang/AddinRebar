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

    /// <summary>
    /// How far the top main bars stop short of a concrete end they run to (a console tip, a column's outer face): J9's
    /// a, which never moves the bars across the section.
    /// </summary>
    public double TopEndCover { get; init; }

    /// <summary>The same for the bottom main bars.</summary>
    public double BottomEndCover { get; init; }

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

    public const double DefaultTieSpacing = 500.0;

    /// <summary>A J7 outside [<see cref="MinTieSpacing"/>, <see cref="MaxTieSpacing"/>] is taken for a typo.</summary>
    public const double MinTieSpacing = 100.0;

    public const double MaxTieSpacing = 1000.0;

    /// <summary>Spacing of the C ties (side-bar ties, layer spacer ties) when they are spaced evenly (J7, mm).</summary>
    public double TieSpacing { get; init; } = DefaultTieSpacing;

    /// <summary>Evenly at <see cref="TieSpacing"/>, or at every outer hoop (cell I8).</summary>
    public KataTieSpacingMode TieSpacingMode { get; init; } = KataTieSpacingMode.Uniform;

    /// <summary>Why <see cref="TieSpacing"/> is the default and not J7; reported once some tie is drawn.</summary>
    public string? TieSpacingNote { get; init; }

    /// <summary>
    /// An additional bar layer under the outer one (top rows 14-16, bottom row 17) with at least this many bars
    /// gets C ties under it, their hooks round its two outer bars.
    /// </summary>
    public int LayerTieMinBarCount { get; init; } = 3;

    public int ClosedStirrupHookAngle { get; init; } = 135;
    public double ClosedStirrupHookFactor { get; init; } = 7.5;
    public int CrossTieHookAngle { get; init; } = 180;
    public double CrossTieHookFactor { get; init; } = 7.5;

    /// <summary>A reach rounded up to the cut increment, so a rounded bar is never shorter.</summary>
    public double RoundUp(double length) =>
        RoundCutExtraMm > 0.0 ? Math.Ceiling(length / RoundCutExtraMm - 1e-9) * RoundCutExtraMm : length;

    /// <summary>A length rounded to the nearest cut increment (Kata's L/6 cap of the span bottom bars).</summary>
    public double RoundNearest(double length) =>
        RoundCutExtraMm > 0.0 ? Math.Round(length / RoundCutExtraMm, MidpointRounding.AwayFromZero) * RoundCutExtraMm : length;

    /// <summary>A distance kept free of a bar, rounded down to the cut increment.</summary>
    public double RoundDown(double length) =>
        RoundCutExtraMm > 0.0 ? Math.Floor(length / RoundCutExtraMm + 1e-9) * RoundCutExtraMm : length;

    /// <summary>Shortest bent leg of an anchorage, in diameters.</summary>
    public double MinimumLegFactor { get; init; } = 0.0;

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
    public double BottomExtraCutFraction { get; init; } = 1.0 / 6.0;

    /// <summary>
    /// Kata's beam-node detail: the bottom main bars crank across a soffit step only when they are at least this
    /// thick ("Bẻ cổ chai cho thép có phi từ") and e / H ≤ 1 / <see cref="CrankSlope"/>, e being the clear offset of
    /// the two bars (step − Ø) and H the support width; otherwise they are cut there and anchored (case 2).
    /// </summary>
    public double CrankMinDiameter { get; init; } = 16.0;

    /// <summary>Run of the crank per unit of rise (1:6, "Tỷ lệ đoạn nhấn cổ chai").</summary>
    public double CrankSlope { get; init; } = 6.0;

    /// <summary>Whether a step <paramref name="step"/> over a support <paramref name="supportWidth"/> wide is cranked (not cut).</summary>
    public bool Cranks(double step, double supportWidth, double diameter) =>
        diameter + 1e-6 >= CrankMinDiameter
        && supportWidth > 0.0
        && (Math.Abs(step) - diameter) / supportWidth <= 1.0 / CrankSlope + 1e-9;

    /// <summary>First and last stirrup of a span, measured from the support faces.</summary>
    public double FirstStirrupOffset { get; init; } = 50.0;

    /// <summary>
    /// Joint stirrups on each face of a load resting on a span (tab "Thép mặc định": 5f10a50 each side; B01 at mid-D
    /// 4850…5050 and 5550…5750 round a beam 5100…5500): the first <see cref="FirstStirrupOffset"/> from the face.
    /// </summary>
    public int JointStirrupCount { get; init; } = 5;

    /// <summary>Spacing of the joint stirrups (mm).</summary>
    public double JointStirrupSpacing { get; init; } = 50.0;

    /// <summary>A load whose face is this close to a support face gets all its joint stirrups on the span side.</summary>
    public double JointNearSupportMm { get; init; } = 200.0;

    /// <summary>Hanger bars ("vai bò") under a load: count and diameter (B01: 2Ø16 at both loads).</summary>
    public int HangerBarCount { get; init; } = 2;

    /// <summary>Diameter of the hanger bars (mm).</summary>
    public double HangerBarDiameter { get; init; } = 16.0;

    /// <summary>Level run of a hanger bar at the top past each slope (B01: 150).</summary>
    public double HangerTopLength { get; init; } = 150.0;

    /// <summary>Slope of a hanger bar from the horizontal, degrees (B01: 45, rise 900 over 900).</summary>
    public double HangerAngleDegrees { get; init; } = 45.0;

    /// <summary>
    /// Dense stirrup zone at each support: max(<see cref="DenseZoneHeightFactor"/> × h, <see cref="EndZoneFraction"/>
    /// × clear span), never more than half the span.
    /// </summary>
    public double EndZoneFraction { get; init; } = 0.25;

    public double DenseZoneHeightFactor { get; init; } = 0.0;

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
