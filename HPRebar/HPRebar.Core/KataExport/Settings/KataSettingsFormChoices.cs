using System.Collections.Generic;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataExport.Settings;

/// <summary>
/// The lists the settings page offers, as Kata's dialog offers them. A value outside its list is refused, except
/// "Spec số lượng đai", whose list is only a suggestion (Kata takes any expression there).
/// </summary>
public sealed record KataSettingsFormChoices(
    IReadOnlyList<int> HookAngles,
    IReadOnlyList<double> CrankMinDiameters,
    IReadOnlyList<double> CrankSlopes,
    IReadOnlyList<double> CouplerMinDiameters,
    IReadOnlyList<string> StirrupCountSpecs,
    IReadOnlyList<int> HangerAngles)
{
    public static readonly KataSettingsFormChoices Kata = new(
        new[] { 90, 135, 180 },
        KataSettingsChoices.CrankMinDiameters,
        KataSettingsChoices.CrankSlopes,
        KataSettingsChoices.CouplerMinDiameters,
        new[] { "None", "H1", "H", "W", "W+H-1" },
        new[] { 45, 60 });
}
