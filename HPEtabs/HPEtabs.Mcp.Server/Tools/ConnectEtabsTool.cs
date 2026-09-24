using System.ComponentModel;
using System.Text.Json;
using HPEtabs.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPEtabs.Mcp.Server.Tools;

/// <summary>Outcome of an explicit connect_etabs MCP call.</summary>
public sealed record EtabsConnectionDto(
    string State,
    string? Version = null,
    double? VersionNumber = null,
    string? ModelTitle = null,
    int Pid = 0,
    string? ErrorMessage = null,
    string? Warning = null);

/// <summary>
///     Explicit connection and auto-start management tool for ETABS 22.
/// </summary>
[McpServerToolType]
public sealed class ConnectEtabsTool(IRevitBridgeClient bridgeClient)
{
    public const string ToolDescription =
        "Checks connection to ETABS 22, connects to an already-running ETABS instance, or automatically launches a new ETABS instance if none is running. " +
        "Returns connection state ('already_connected', 'attached_existing', 'started_new', or 'failed'), ETABS version, active model title, and process PID. " +
        "Note: Calling get_etabs_context, execute_etabs_code or any seed tool will automatically connect/start ETABS on-demand, so calling this tool explicitly is optional.";

    [McpServerTool(
        Name = EtabsHostProfile.ConnectToolName,
        Title = "Connect or Start ETABS",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = false)]
    [Description(ToolDescription)]
    public async Task<CallToolResult> ConnectAsync(
        [Description("Automatically launch ETABS if no instance is running (default: true).")]
        bool autoStart = true,
        [Description("Prefer attaching to an already-running ETABS instance before attempting to start a new one (default: true).")]
        bool preferExistingInstance = true,
        [Description("Optional explicit path to ETABS.exe. If omitted, uses auto-detected installation.")]
        string? etabsExePath = null,
        [Description("Timeout in seconds to wait for ETABS to start and be ready (5–120, default: 60).")]
        int timeoutSeconds = 60,
        CancellationToken cancellationToken = default)
    {
        var parameters = new
        {
            AutoStart = autoStart,
            PreferExistingInstance = preferExistingInstance,
            EtabsExePath = etabsExePath,
            StartupTimeoutSeconds = Math.Clamp(timeoutSeconds, 5, 120)
        };

        var method = bridgeClient.Profile.Method("connect");
        try
        {
            var result = await bridgeClient.SendAsync<EtabsConnectionDto>(
                method,
                parameters,
                TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 5, 120) + 5),
                null,
                cancellationToken).ConfigureAwait(false);

            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            return new CallToolResult
            {
                Content = [new TextContentBlock { Text = json }],
                IsError = string.Equals(result.State, "failed", StringComparison.OrdinalIgnoreCase)
            };
        }
        catch (Exception ex)
        {
            return new CallToolResult
            {
                Content = [new TextContentBlock { Text = $"Failed to connect or start ETABS: {ex.Message}" }],
                IsError = true
            };
        }
    }
}
