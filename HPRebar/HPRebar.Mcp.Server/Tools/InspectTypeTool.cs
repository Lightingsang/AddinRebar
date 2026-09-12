using System.ComponentModel;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPRebar.Mcp.Server.Tools;

/// <summary>
///     Lets the AI look up a Revit API type by reflection in the running Revit instead of guessing member
///     names from training data — the cheapest way to avoid compile errors on rarely used APIs.
/// </summary>
[McpServerToolType]
public sealed class InspectTypeTool(IRevitBridgeClient bridge, ResultFormatter formatter)
{
    private const int MaxMembersCeiling = 500;

    [McpServerTool(
        Name = "inspect_type",
        Title = "Inspect a Revit API type",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description(
        "Reflects over a Revit API type loaded in the running Revit and returns its public members as C#-like signatures " +
        "(kind: property | method | field | event). Use it to confirm exact method names and parameters before writing a script.")]
    public Task<CallToolResult> InspectAsync(
        [Description("Type name, e.g. 'Wall', 'FilteredElementCollector', or fully qualified 'Autodesk.Revit.DB.Structure.Rebar'.")]
        string typeName,
        [Description("Optional case-insensitive substring to keep only matching member names, e.g. 'Create'.")]
        string? memberFilter = null,
        [Description("Maximum number of members to return (default 100, max 500).")]
        int maxMembers = 100,
        CancellationToken cancellationToken = default)
    {
        return formatter.RunAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(typeName)) return formatter.Error("typeName is empty.");

            var request = new InspectRequest(typeName.Trim(), memberFilter?.Trim(), Math.Clamp(maxMembers, 1, MaxMembersCeiling));

            var result = await bridge.SendAsync<InspectResult>(
                JsonRpcMethods.Inspect,
                request,
                RevitContextTool.Timeout,
                null,
                cancellationToken).ConfigureAwait(false);

            return result.Message is not null && result.Members.Count == 0
                ? formatter.Error(result.Message)
                : formatter.Text(result);
        });
    }
}
