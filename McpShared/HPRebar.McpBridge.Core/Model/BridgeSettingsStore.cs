using System.IO;
using System.Text.Json;
using Serilog;

namespace HPRebar.McpBridge.Core.Model;

/// <summary>
///     Persists the few settings that should survive a Revit restart. The per-session execution switch
///     is stripped on save and forced off on load so the file can never pre-authorise AI code.
/// </summary>
public static class BridgeSettingsStore
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { WriteIndented = true };

    public static string Directory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HPRebar", "McpBridge");

    public static string SettingsPath => Path.Combine(Directory, "settings.json");

    public static string AuditDirectory => Path.Combine(Directory, "audit");

    public static BridgeSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var settings = JsonSerializer.Deserialize<BridgeSettings>(File.ReadAllText(SettingsPath), Options);
                if (settings is not null)
                {
                    settings.ExecutionEnabled = false;
                    return settings;
                }
            }
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "MCP bridge settings unreadable; using defaults");
        }

        return new BridgeSettings();
    }

    public static void Save(BridgeSettings settings)
    {
        try
        {
            System.IO.Directory.CreateDirectory(Directory);

            var persisted = new BridgeSettings
            {
                ExecutionEnabled = false,
                AutoStartListener = settings.AutoStartListener,
                RequireLocalApproval = settings.RequireLocalApproval,
                DefaultTimeoutSeconds = settings.DefaultTimeoutSeconds,
                MaxSourceBytes = settings.MaxSourceBytes,
                MaxOutputBytes = settings.MaxOutputBytes,
                MaxLogLines = settings.MaxLogLines,
                ScriptCacheSize = settings.ScriptCacheSize,
                AllowFamilyDocuments = settings.AllowFamilyDocuments,
            };

            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(persisted, Options));
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "MCP bridge settings could not be saved");
        }
    }
}
