using System.IO;
using System.Text.Json;
using Serilog;

namespace HPRebar.McpBridge.Core.Model;

/// <summary>
///     Persists the few settings that should survive a host restart. The per-session execution switch
///     is stripped on save and forced off on load so the file can never pre-authorise AI code.
///     Paths are per product (`%AppData%\{vendor}\{product}\`) so two bridges on one machine — Revit
///     and AutoCAD — never read each other's file.
/// </summary>
public sealed class BridgeSettingsStore
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { WriteIndented = true };

    /// <summary>The Revit bridge's store: `%AppData%\HPRebar\McpBridge\`, unchanged since the first release.</summary>
    public static BridgeSettingsStore Revit { get; } = new BridgeSettingsStore("HPRebar", "McpBridge");

    public BridgeSettingsStore(string vendorFolder, string productFolder)
    {
        if (string.IsNullOrWhiteSpace(vendorFolder)) throw new ArgumentException("vendorFolder is required", nameof(vendorFolder));
        if (string.IsNullOrWhiteSpace(productFolder)) throw new ArgumentException("productFolder is required", nameof(productFolder));

        Directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), vendorFolder, productFolder);
    }

    public string Directory { get; }

    public string SettingsPath => Path.Combine(Directory, "settings.json");

    public string AuditDirectory => Path.Combine(Directory, "audit");

    public BridgeSettings Load()
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

        return new BridgeSettings
        {
            AutoStartListener = true,
        };
    }

    public void Save(BridgeSettings settings)
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
