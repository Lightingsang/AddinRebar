using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using HPGeo.Core.Settings;

namespace HPGeo.AutoCad.Cad;

/// <summary>
/// The user's last settings: <c>%AppData%\HPGeo\settings.json</c>. A missing or unreadable file is "no
/// settings" — never an error the user sees. No secrets live here.
/// </summary>
internal static class UserSettingsStore
{
    public static readonly string Path =
        System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HPGeo", "settings.json");

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    public static GeoSettings? Load()
    {
        try
        {
            if (!File.Exists(Path)) return null;
            return JsonSerializer.Deserialize<GeoSettings>(File.ReadAllText(Path), Options);
        }
        catch (Exception exception)
        {
            HPGeoLog.Warning($"settings.json unreadable, ignored: {exception.Message}");
            return null;
        }
    }

    public static void Save(GeoSettings settings)
    {
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            File.WriteAllText(Path, JsonSerializer.Serialize(settings with { SavedBy = "HPGeo " + Entry.Version }, Options));
        }
        catch (Exception exception)
        {
            HPGeoLog.Warning($"settings.json could not be written: {exception.Message}");
        }
    }
}
