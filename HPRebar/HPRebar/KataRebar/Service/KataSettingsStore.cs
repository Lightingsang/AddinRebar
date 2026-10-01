using System;
using System.IO;
using System.Text;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Serilog;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// The office's Kata detailing settings, kept per user in %AppData%\HPRebar\KataSettings.json. A missing or
/// unreadable file means the Kata defaults, never a failed run.
/// </summary>
public static class KataSettingsStore
{
    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HPRebar", "KataSettings.json");

    private static KataSettings? _cached;

    public static KataSettings Load()
    {
        if (_cached is not null) return _cached;

        try
        {
            if (File.Exists(SettingsFilePath))
                return _cached = KataSettingsJson.Read(File.ReadAllText(SettingsFilePath, Encoding.UTF8));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException)
        {
            Log.Warning(ex, "Kata Rebar: settings file {Path} could not be read; using the Kata defaults", SettingsFilePath);
        }

        return _cached = KataSettings.Default;
    }

    /// <returns>False when the file could not be written (the settings still apply to this session).</returns>
    public static bool Save(KataSettings settings)
    {
        // What is cached and written is what the rules will use: out-of-range values already replaced.
        _cached = KataSettingsJson.Sanitize(settings ?? throw new ArgumentNullException(nameof(settings)));
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath)!);
            File.WriteAllText(SettingsFilePath, KataSettingsJson.Write(_cached), new UTF8Encoding(false));
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error(ex, "Kata Rebar: settings file {Path} could not be written", SettingsFilePath);
            return false;
        }
    }
}
