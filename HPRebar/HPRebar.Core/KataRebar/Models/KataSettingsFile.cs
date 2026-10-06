namespace HPRebar.Core.KataRebar.Models;

/// <summary>
/// Everything the settings dialog keeps in one file: the settings the bars are drawn with, the beam options Kata
/// has and the add-in does not apply yet, and the shop-drawing settings. Only <see cref="Drawing"/> reaches the rules.
/// </summary>
public sealed record KataSettingsFile(KataSettings Drawing, KataBeamOptions Pending, KataShopSettings Shop)
{
    public static readonly KataSettingsFile Default = new(KataSettings.Default, KataBeamOptions.Default, KataShopSettings.Default);
}
