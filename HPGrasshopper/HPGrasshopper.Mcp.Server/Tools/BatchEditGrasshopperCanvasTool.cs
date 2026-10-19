namespace HPGrasshopper.Mcp.Server.Tools;

using System.ComponentModel;
using System.Text.Json;
using HPGrasshopper.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

/// <summary>
///     Core authoring tool for performing batch atomic modifications to the active Grasshopper canvas,
///     including component creation, deletion, port wiring, and parameter mutations backed by GH_UndoRecord.
/// </summary>
[McpServerToolType]
public sealed class BatchEditGrasshopperCanvasTool(IRevitBridgeClient client, ResultFormatter formatter)
{
    public const string ToolDescription =
        "Performs batch atomic edits on the active Grasshopper canvas (adding/removing components, " +
        "wiring/unwiring ports, updating parameters and positions) backed by GH_UndoRecord for full undo support. " +
        "Includes preflight validation and returns createdComponents ID mappings. " +
        "Under dryRun=true, changes are simulated and automatically rolled back.";

    [McpServerTool(
        Name = GrasshopperHostProfile.BatchEditToolName,
        Title = "Batch Edit Grasshopper Canvas",
        ReadOnly = false,
        Destructive = true,
        Idempotent = false,
        OpenWorld = false)]
    [Description(ToolDescription)]
    public Task<CallToolResult> BatchEditAsync(
        [Description("Array of component descriptions {type, x, y, nickName, tempId}.")]
        JsonElement? componentsToAdd = null,
        [Description("Array of component instance Guids to remove.")]
        string[]? componentsToRemove = null,
        [Description("Array of wire linkages {source, sourceOutput, target, targetInput}.")]
        JsonElement? wiresToConnect = null,
        [Description("Array of wire linkages {source, sourceOutput, target, targetInput}.")]
        JsonElement? wiresToDisconnect = null,
        [Description("If true, simulates changes and immediately rolls back via GH_UndoRecord. Default false.")]
        bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        return formatter.RunAsync(async () =>
        {
            var requestParams = new
            {
                componentsToAdd,
                componentsToRemove,
                wiresToConnect,
                wiresToDisconnect,
                dryRun
            };

            var result = await client.SendAsync<JsonElement>(
                client.Profile.Method(GrasshopperHostProfile.BatchEditToolName),
                requestParams,
                TimeSpan.FromSeconds(60),
                null,
                cancellationToken).ConfigureAwait(false);

            return GrasshopperToolResult.From(result, formatter);
        });
    }
}
