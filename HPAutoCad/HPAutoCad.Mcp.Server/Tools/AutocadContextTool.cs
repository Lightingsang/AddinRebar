using System.ComponentModel;
using HPAutoCad.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPAutoCad.Mcp.Server.Tools;

/// <summary>Read-only snapshot of the AutoCAD session so the AI can write correct code before touching the drawing.</summary>
[McpServerToolType]
public sealed class AutocadContextTool(ContextService service)
{
    [McpServerTool(
        Name = AutocadHostProfile.ContextToolName,
        Title = "Get AutoCAD context",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description(
        "Returns the current AutoCAD session: hostVersion, active drawing title and docPath (absent for an unsaved drawing), isReadOnly, " +
        "isModifiable (true when the drawing is writable and AutoCAD is idle — no command or dialog in progress), units.length (drawing unit from INSUNITS), " +
        "activeView (current layout: Model or a paper-space layout), openDocs, executionEnabled, and autocad {insunits, measurement (English/Metric), " +
        "currentLayout, currentLayer, isModelSpace, isQuiescent, isNamedDrawing}. With includeSelection the current selection comes back as " +
        "{id = handle value, category = layer, name = DXF name}. Call this before execute_autocad_code so the script matches the real drawing and its units.")]
    public Task<CallToolResult> GetContextAsync(
        [Description("Include the entities currently selected in AutoCAD (id = handle, category = layer, name = DXF name).")]
        bool includeSelection = false,
        CancellationToken cancellationToken = default)
    {
        return service.GetAsync(includeSelection, cancellationToken);
    }
}
