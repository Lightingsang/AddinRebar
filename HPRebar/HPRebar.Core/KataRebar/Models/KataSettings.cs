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
}
