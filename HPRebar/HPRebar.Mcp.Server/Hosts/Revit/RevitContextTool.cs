using System.ComponentModel;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPRebar.Mcp.Server.Hosts.Revit;

/// <summary>Read-only snapshot of the Revit session so the AI can write correct code before touching the model.</summary>
[McpServerToolType]
public sealed class RevitContextTool(ContextService service)
{
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
        return service.GetAsync(includeSelection, cancellationToken);
    }
}
