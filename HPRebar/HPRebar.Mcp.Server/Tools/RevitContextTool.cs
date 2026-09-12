using System.ComponentModel;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPRebar.Mcp.Server.Tools;

/// <summary>Read-only snapshot of the Revit session so the AI can write correct code before touching the model.</summary>
[McpServerToolType]
public sealed class RevitContextTool(IRevitBridgeClient bridge, ResultFormatter formatter)
{
    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    [McpServerTool(
        Name = "get_revit_context",
        Title = "Get Revit context",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description(
        "Returns the current Revit session: version, active document title/path, whether it is a family or read-only, " +
        "display length unit, active view, open documents, whether code execution is enabled, and optionally the current selection " +
        "as {id, category, name}. Call this before execute_revit_code so the script matches the real document.")]
    public Task<CallToolResult> GetContextAsync(
        [Description("Include the elements currently selected in Revit (id, category, name).")]
        bool includeSelection = false,
        CancellationToken cancellationToken = default)
    {
        return formatter.RunAsync(async () =>
        {
            var context = await bridge.SendAsync<ContextResult>(
                JsonRpcMethods.Context,
                new ContextRequest(includeSelection),
                Timeout,
                null,
                cancellationToken).ConfigureAwait(false);

            return formatter.Text(context);
        });
    }
}
