using HPRebar.Mcp.Contracts;

namespace HPRebar.Mcp.Server.Models;

/// <summary>
///     `Bridge` section of appsettings.json (override with HPREBAR_MCP_Bridge__* environment variables).
///     Everything the server needs to find and talk to the add-in running inside Revit.
/// </summary>
public sealed class BridgeOptions
{
    public const string SectionName = "Bridge";

    /// <summary>Revit major version whose bridge this server talks to. One pipe per version.</summary>
    public int RevitVersion { get; set; } = 2026;

    private string? _pipeName;

    /// <summary>Derived from <see cref="RevitVersion"/> unless set explicitly (tests, unusual setups).</summary>
    public string PipeName
    {
        get => _pipeName ?? PipeNaming.For(RevitVersion);
        set => _pipeName = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>How long a single pipe connect attempt may take before it counts as "Revit not running".</summary>
    public int ConnectTimeoutMs { get; set; } = 2000;

    /// <summary>Added on top of the script's own timeout before the server gives up waiting for a reply.</summary>
    public int ExtraTimeoutSeconds { get; set; } = 5;

    /// <summary>Upper bound on the UTF-8 size of a script; larger requests are rejected before reaching Revit.</summary>
    public int MaxSourceBytes { get; set; } = 32 * 1024;

    /// <summary>Longest single NDJSON line accepted from the bridge; anything bigger drops the connection.</summary>
    public int MaxMessageBytes { get; set; } = 4 * 1024 * 1024;

    public int PingIntervalSeconds { get; set; } = 10;

    public int MaxReconnectAttempts { get; set; } = 5;

    public bool IsValid() =>
        RevitVersion is 2025 or 2026
        && ConnectTimeoutMs is >= 100 and <= 30_000
        && ExtraTimeoutSeconds is >= 0 and <= 60
        && MaxSourceBytes is >= 1024 and <= 1024 * 1024
        && MaxMessageBytes >= 64 * 1024
        && PingIntervalSeconds is >= 1 and <= 300
        && MaxReconnectAttempts is >= 0 and <= 20;
}
