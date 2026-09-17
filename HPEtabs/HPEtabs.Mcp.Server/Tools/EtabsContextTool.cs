using System.ComponentModel;
using HPEtabs.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPEtabs.Mcp.Server.Tools;

/// <summary>Read-only snapshot of the attached ETABS session so the AI can write correct code before touching the model.</summary>
[McpServerToolType]
public sealed class EtabsContextTool(ContextService service)
{
    [McpServerTool(
        Name = EtabsHostProfile.ContextToolName,
        Title = "Get ETABS context",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description(
        "Returns the ETABS session the bridge app is attached to: hostVersion (22), docTitle and docPath of the model file (absent while no model is open or it was never saved), " +
        "isModifiable (attached, a model is open, ETABS is idle — no dialog open), units.length (mm: every run forces kN_mm_C), executionEnabled, " +
        "and etabs {isAttached, attachedPid, oapiVersion, isLocked (definitions cannot change until unlocked — unlocking discards results), presentUnits (the user's own API units), databaseUnits, " +
        "destructiveOperationsEnabled (the second opt-in: unlock, RunAnalysis, file operations), pointCount, frameCount, areaCount}. " +
        "With includeSelection the selected objects come back (max 50) as {id, category = object type, name}. " +
        "Fails fast with a busy error while a script is running, with 'not attached' until the user clicks Attach in the HPEtabs MCP Bridge window, and with 'no model' on the ETABS start screen. " +
        "Call this before execute_etabs_code so the script matches the real model and its lock state.")]
    public Task<CallToolResult> GetContextAsync(
        [Description("Include the objects currently selected in ETABS (max 50: id = running index, category = object type (Point/Frame/Area/…), name = object unique name).")]
        bool includeSelection = false,
        CancellationToken cancellationToken = default)
    {
        return service.GetAsync(includeSelection, cancellationToken);
    }
}
