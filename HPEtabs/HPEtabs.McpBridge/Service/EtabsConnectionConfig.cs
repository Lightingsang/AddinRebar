using System.IO;
using System.Text.Json;
using Serilog;

namespace HPEtabs.McpBridge.Service;

/// <summary>
///     Settings governing how the bridge attaches to or auto-starts ETABS 22.
///     Persisted to `%AppData%\HPEtabs\McpBridge\connection.json`.
/// </summary>
public sealed class EtabsConnectionConfig
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>Explicit path to ETABS.exe if multiple versions are installed or ETABS is in a non-standard location.</summary>
    public string? EtabsExePath { get; set; }

    /// <summary>Automatically launch ETABS if no instance is running when an MCP request arrives.</summary>
    public bool AutoStart { get; set; } = true;

    /// <summary>Prefer attaching to an already running ETABS instance before attempting to start a new one.</summary>
    public bool PreferExistingInstance { get; set; } = true;

    /// <summary>Whether to show the ETABS main GUI window when auto-starting.</summary>
    public bool StartVisible { get; set; } = true;

    /// <summary>Timeout in seconds to wait for ETABS to start and become ready for OAPI calls (default 60s).</summary>
    public int StartupTimeoutSeconds { get; set; } = 60;

    /// <summary>Optional .EDB model file path to open upon auto-start. If null or empty, a blank model is initialized.</summary>
    public string? DefaultModelPath { get; set; }

    public static string GetConfigPath(string vendorFolder, string productFolder) =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), vendorFolder, productFolder, "connection.json");

    public static EtabsConnectionConfig Load(string vendorFolder, string productFolder)
    {
        var path = GetConfigPath(vendorFolder, productFolder);
        try
        {
            if (File.Exists(path))
            {
                var loaded = JsonSerializer.Deserialize<EtabsConnectionConfig>(File.ReadAllText(path), JsonOptions);
                if (loaded is not null)
                {
                    ApplyEnvOverrides(loaded);
                    return loaded;
                }
            }
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "Failed to load ETABS connection config from {Path}; using defaults", path);
        }

        var config = new EtabsConnectionConfig { AutoStart = true };
        ApplyEnvOverrides(config);
        return config;
    }

    public void Save(string vendorFolder, string productFolder)
    {
        var path = GetConfigPath(vendorFolder, productFolder);
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "Failed to save ETABS connection config to {Path}", path);
        }
    }

    private static void ApplyEnvOverrides(EtabsConnectionConfig config)
    {
        if (Environment.GetEnvironmentVariable("HPETABS_EXE_PATH") is { } exe && !string.IsNullOrWhiteSpace(exe))
            config.EtabsExePath = exe;

        if (Environment.GetEnvironmentVariable("HPETABS_AUTO_START") is { } auto && bool.TryParse(auto, out var autoVal))
            config.AutoStart = autoVal;

        if (Environment.GetEnvironmentVariable("HPETABS_PREFER_EXISTING") is { } pref && bool.TryParse(pref, out var prefVal))
            config.PreferExistingInstance = prefVal;
    }
}
