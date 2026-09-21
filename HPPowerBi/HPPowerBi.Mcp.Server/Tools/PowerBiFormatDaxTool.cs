using System.ComponentModel;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPPowerBi.Mcp.Server.Tools;

/// <summary>Tool to format DAX expressions deterministically for readability.</summary>
[McpServerToolType]
public sealed class PowerBiFormatDaxTool(IRevitBridgeClient bridge, ResultFormatter formatter)
{
    [McpServerTool(
        Name = "powerbi_format_dax",
        Title = "Format DAX Expression",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Formats a DAX expression with proper indentation, keyword capitalization, line breaks, and whitespace.")]
    public Task<CallToolResult> FormatDaxAsync(
        [Description("The raw DAX expression to format.")]
        string dax,
        CancellationToken cancellationToken = default)
    {
        return formatter.RunAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(dax))
            {
                return formatter.Error("DAX expression cannot be empty.");
            }

            var parameters = new { Dax = dax };

            var result = await bridge.SendAsync<object>(
                bridge.Profile.MethodPrefix + "format_dax",
                parameters,
                TimeSpan.FromSeconds(15),
                null,
                cancellationToken).ConfigureAwait(false);

            return formatter.Text(result);
        });
    }
}
