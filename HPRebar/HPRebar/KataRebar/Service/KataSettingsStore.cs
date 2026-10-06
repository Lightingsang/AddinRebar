using System;
using System.IO;
using System.Text;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Serilog;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// The office's Kata settings, kept per user in %AppData%\HPRebar\KataSettings.json: the settings bars are drawn
/// with, the beam options and shop settings kept for later. A missing or unreadable file means the defaults, never a
/// failed run.
/// </summary>
public static class KataSettingsStore
{
    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HPRebar", "KataSettings.json");

    private static KataSettingsFile? _cached;

    /// <summary>The settings the bars are drawn with.</summary>
    public static KataSettings Load() => LoadFile().Drawing;

    public static KataSettingsFile LoadFile()
    {
        if (_cached is not null) return _cached;

        try
        {
            if (File.Exists(SettingsFilePath))
                return _cached = KataSettingsJson.ReadFile(File.ReadAllText(SettingsFilePath, Encoding.UTF8));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException)
        {
            Log.Warning(ex, "Kata Rebar: settings file {Path} could not be read; using the Kata defaults", SettingsFilePath);
        }

        return _cached = KataSettingsFile.Default;
    }

    /// <returns>False when the file could not be written (the settings still apply to this session).</returns>
    public static bool SaveFile(KataSettingsFile file)
    {
        // What is cached and written is what the rules will use: out-of-range values already replaced.
        _cached = KataSettingsSanitizer.File(file ?? throw new ArgumentNullException(nameof(file)));
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath)!);
            File.WriteAllText(SettingsFilePath, KataSettingsJson.WriteFile(_cached), new UTF8Encoding(false));
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error(ex, "Kata Rebar: settings file {Path} could not be written", SettingsFilePath);
            return false;
        }
    }
}
