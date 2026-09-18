using System.ComponentModel;
using HPCivil3d.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPCivil3d.Mcp.Server.Tools;

/// <summary>Read-only snapshot of the Civil 3D session so the AI can write correct code before touching the drawing.</summary>
[McpServerToolType]
public sealed class Civil3dContextTool(ContextService service)
{
    [McpServerTool(
        Name = Civil3dHostProfile.ContextToolName,
        Title = "Get Civil 3D context",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description(
        "Returns the current Civil 3D session: hostVersion, active drawing title and docPath (absent for an unsaved drawing), isReadOnly, " +
        "isModifiable (true when the drawing is writable and the editor is idle), units.length (the Civil drawing unit: Meters or Feet), activeView, openDocs, executionEnabled, " +
        "autocad {insunits, measurement, currentLayout, currentLayer, isModelSpace, isQuiescent, isNamedDrawing} and " +
        "civil3d {product (Civil3D when the bridge runs where it should), isCivilDocument, drawingUnit, coordinateSystemCode (absent without a zone), insunitsMismatch, " +
        "alignmentCount, surfaceCount, corridorCount, pipeNetworkCount, pressureNetworkCount, cogoPointCount}. With includeSelection the current selection comes back as " +
        "{id = handle value, category = layer, name = DXF name}. Call this before execute_civil3d_code so the script matches the real drawing and its unit.")]
    public Task<CallToolResult> GetContextAsync(
        [Description("Include the entities currently selected in Civil 3D (id = handle, category = layer, name = DXF name).")]
        bool includeSelection = false,
        CancellationToken cancellationToken = default)
    {
        return service.GetAsync(includeSelection, cancellationToken);
    }
}
