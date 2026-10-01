namespace HPRebar.Core.KataRebar.Models;

/// <summary>
/// The office's detailing settings Kata keeps in its own "Detail thép" dialog, which the add-in cannot read:
/// the user keeps them in step here. Defaults are Kata's. Only settings that change the drawn bars are kept;
/// laps, cranks and couplers are not drawn yet.
/// </summary>
public sealed record KataSettings
{
    public static readonly KataSettings Default = new();

    /// <summary>Longest stock bar (mm); a longer continuous bar is reported.</summary>
    public double MaxBarLength { get; init; } = 11700.0;

    /// <summary>Hook of the closed stirrup (°) and its straight end in stirrup diameters.</summary>
    public int ClosedStirrupHookAngle { get; init; } = 135;

    public double ClosedStirrupHookFactor { get; init; } = 7.5;

    /// <summary>Hook of the C tie (°) and its straight end in stirrup diameters.</summary>
    public int CrossTieHookAngle { get; init; } = 180;

    public double CrossTieHookFactor { get; init; } = 7.5;

    /// <summary>Additional bars are cut on multiples of this length (mm): reaches round up, bars only grow.</summary>
    public double RoundCutExtraMm { get; init; } = 50.0;

    /// <summary>Side bars ("cốt giá") run this many diameters into each support.</summary>
    public double SideBarAnchorageFactor { get; init; } = 10.0;

    /// <summary>Spacing of the C ties holding the side bars (mm).</summary>
    public double SideBarTieSpacing { get; init; } = 400.0;

    /// <summary>
    /// Stagger of the additional top bars (mm): over a support, each layer reaches at least this much further
    /// into the span than the next filled layer inside it. Cell G1 overrides it when it holds a positive number.
    /// </summary>
    public double CurtailedExtensionMm { get; init; } = 500.0;

    /// <summary>Dense stirrup zone at each support = max(this × h, <see cref="EndZoneFraction"/> × clear span).</summary>
    public double DenseZoneHeightFactor { get; init; } = 2.0;

    public double EndZoneFraction { get; init; } = 0.25;

    /// <summary>Additional bottom bars (rows 17-18) stop this fraction of the clear span from each support face.</summary>
    public double BottomExtraCutFraction { get; init; } = 0.15;

    /// <summary>Shortest bent leg of an anchorage, in bar diameters.</summary>
    public double MinimumLegFactor { get; init; } = 15.0;

    /// <summary>Smallest clear gap between two bar layers (mm); the larger bar diameter wins when bigger.</summary>
    public double LayerClearGap { get; init; } = 30.0;

    /// <summary>Bent legs are rounded up to a multiple of this length (mm) when the rounded leg still fits.</summary>
    public double RoundLegMm { get; init; } = 25.0;

    /// <summary>A beam at least this deep with no side bars is reported (TCVN 5574:2018 § 10.3.1.2).</summary>
    public double SideBarRequiredHeight { get; init; } = 700.0;
}
