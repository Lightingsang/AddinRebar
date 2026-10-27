using HPRebar.Core.KataExport.Settings;

namespace HPRebar.KataExport.View.Web.Settings;

/// <summary>Commands the Kata Settings page sends (protocol v1 of HPWebHost), mirrored in the page's settingsContracts.ts.</summary>
public static class KataSettingsWebCommands
{
    /// <summary>The settings file as the page edits it (<see cref="KataSettingsPageState" />).</summary>
    public const string Load = "settings.load";

    /// <summary>What is wrong with a form (<see cref="KataSettingsValidation" />); nothing is written.</summary>
    public const string Validate = "settings.validate";

    /// <summary>Writes a valid form to the settings file (<see cref="KataSettingsSaveResult" />); the window stays open.</summary>
    public const string Save = "settings.save";

    /// <summary>Kata's defaults as a form (<see cref="KataSettingsSnapshot" />); nothing is written until saved.</summary>
    public const string Defaults = "settings.defaults";

    /// <summary>Closes the window, saved or not.</summary>
    public const string Close = "settings.close";
}

public static class KataSettingsWebEvents
{
    /// <summary>Revit's theme changed while the window is open (<see cref="KataSettingsThemeEvent" />).</summary>
    public const string Theme = "settings.theme";
}

/// <param name="Theme">"dark" or "light", as Revit shows.</param>
public sealed record KataSettingsPageState(string Theme, KataSettingsSnapshot Snapshot);

public sealed record KataSettingsValidation(IReadOnlyList<KataSettingsError> Errors);

/// <param name="Saved">The form was valid and taken; false with <paramref name="Errors" /> when it was refused.</param>
/// <param name="FileFailed">Taken but the file could not be written: the settings apply to this Revit session only.</param>
public sealed record KataSettingsSaveResult(bool Saved, bool FileFailed, IReadOnlyList<KataSettingsError> Errors);

public sealed record KataSettingsThemeEvent(string Theme);
