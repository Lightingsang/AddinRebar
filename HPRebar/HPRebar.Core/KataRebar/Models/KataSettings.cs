namespace HPRebar.Core.KataRebar.Models;

/// <summary>
/// The office's detailing settings Kata keeps in its own "Detail thép" dialog, which the add-in cannot read:
/// the user keeps them in step here. Defaults are Kata's. Only settings that change the drawn bars are kept.
/// Main bars are drawn whole (a design model): laps, cranks and couplers belong to the shop drawings and the
/// template's rebar schedule accounts for the laps.
/// </summary>
public sealed record KataSettings
{
    public static readonly KataSettings Default = new();

    /// <summary>
    /// Format of the saved file: 2 = defaults of the Kata drawing (leg 0, dense 0.25 L, bottom cap L/6); 3 = joint
    /// defaults, pending beam options and shop settings beside these.
    /// </summary>
    public int SettingsVersion { get; init; } = 3;

    /// <summary>Hook of the closed stirrup (°) and its straight end in stirrup diameters.</summary>
    public int ClosedStirrupHookAngle { get; init; } = 135;

    public double ClosedStirrupHookFactor { get; init; } = 7.5;

    /// <summary>Hook of the C tie (°) and its straight end in stirrup diameters.</summary>
    public int CrossTieHookAngle { get; init; } = 180;

    public double CrossTieHookFactor { get; init; } = 7.5;

    /// <summary>Additional bars are cut on multiples of this length (mm): reaches round up; the L/6 cap of the span bottom bars rounds to the nearest.</summary>
    public double RoundCutExtraMm { get; init; } = 50.0;

    /// <summary>Bottom main bars crank across a soffit step only from this diameter (Kata "Bẻ cổ chai cho thép có phi từ").</summary>
    public double CrankMinDiameter { get; init; } = 16.0;

    /// <summary>Run of a crank per unit of rise (Kata "Tỷ lệ đoạn nhấn cổ chai" 1/6 → 6).</summary>
    public double CrankSlope { get; init; } = 6.0;

    /// <summary>Joint stirrups and hanger bars round a beam framing into a span (Kata tab "Thép mặc định", "Dầm giao").</summary>
    public KataJointRebarSettings JointBeam { get; init; } = KataJointRebarSettings.Default;

    /// <summary>The same round a stub column standing on a span ("Cột cấy").</summary>
    public KataJointRebarSettings JointColumn { get; init; } = KataJointRebarSettings.Default;

    /// <summary>Side bars ("cốt giá") run this many diameters into each support.</summary>
    public double SideBarAnchorageFactor { get; init; } = 10.0;

    /// <summary>
    /// An additional bar layer under the outer one with at least this many bars gets C ties under it (2 or more).
    /// The spacing of every C tie comes from cells J7/I8 of the sheet.
    /// </summary>
    public int LayerTieMinBarCount { get; init; } = 3;

    /// <summary>
    /// Stagger of the additional top bars (mm): over a support, each layer reaches at least this much further
    /// into the span than the next filled layer inside it. Cell G1 overrides it when it holds a positive number.
    /// </summary>
    public double CurtailedExtensionMm { get; init; } = 500.0;

    /// <summary>Dense stirrup zone at each support = max(this × h, <see cref="EndZoneFraction"/> × clear span).</summary>
    public double DenseZoneHeightFactor { get; init; } = 0.0;

    public double EndZoneFraction { get; init; } = 0.25;

    /// <summary>
    /// Largest distance the additional bottom bars (rows 17-18) keep from a support face, as a fraction of the clear
    /// span (L/6, as Kata draws it); H3 × L is used when shorter.
    /// </summary>
    public double BottomExtraCutFraction { get; init; } = 1.0 / 6.0;

    /// <summary>Shortest bent leg of an anchorage, in bar diameters; 0 (Kata) bends exactly the missing length.</summary>
    public double MinimumLegFactor { get; init; } = 0.0;

    /// <summary>Smallest clear gap between two bar layers (mm); the larger bar diameter wins when bigger.</summary>
    public double LayerClearGap { get; init; } = 30.0;

    /// <summary>Bent legs are rounded up to a multiple of this length (mm) when the rounded leg still fits.</summary>
    public double RoundLegMm { get; init; } = 25.0;

    /// <summary>A beam at least this deep with no side bars is reported (TCVN 5574:2018 § 10.3.1.2).</summary>
    public double SideBarRequiredHeight { get; init; } = 700.0;

    /// <summary>
    /// Kata "Thể hiện móc cắt kết thúc thép": each bar end gets the short slanted cut mark on the beam's long section.
    /// </summary>
    public bool ShowBarEndMarks { get; init; } = true;

    /// <summary>
    /// Generation also drafts the run as Kata's drawing does: the long section's dimensions, one cross section per
    /// section flag with its dimensions, and a sheet holding them.
    /// </summary>
    public bool CreateKataDrawings { get; init; } = true;
}
