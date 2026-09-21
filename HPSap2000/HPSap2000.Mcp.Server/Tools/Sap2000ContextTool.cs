using System.ComponentModel;
using HPSap2000.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPSap2000.Mcp.Server.Tools;

/// <summary>Read-only snapshot of the attached SAP2000 session so the AI can write correct code before touching the model.</summary>
[McpServerToolType]
public sealed class Sap2000ContextTool(ContextService service)
{
    [McpServerTool(
        Name = Sap2000HostProfile.ContextToolName,
        Title = "Get SAP2000 context",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description(
        "Returns the SAP2000 session the bridge app is attached to: hostVersion (27), docTitle and docPath of the model file (absent while no model is open or it was never saved), " +
        "isModifiable (attached, a model is open, SAP2000 is idle — no dialog open), units.length (m: every run forces kN_m_C), executionEnabled, " +
        "and sap2000 {isAttached, attachedPid, oapiVersion, isLocked (definitions cannot change until unlocked — unlocking discards results), presentUnits (the user's own API units), databaseUnits, " +
        "destructiveOperationsEnabled (the second opt-in: unlock, RunAnalysis, file operations), pointCount, frameCount, areaCount}. " +
        "With includeSelection the selected objects come back (max 50) as {id, category = object type, name}. " +
        "Fails fast with a busy error while a script is running, with 'not attached' until the user clicks Attach in the HPSap2000 MCP Bridge window, and with 'no model' on the SAP2000 start screen. " +
        "Call this before execute_sap2000_code so the script matches the real model and its lock state.")]
    public Task<CallToolResult> GetContextAsync(
        [Description("Include the objects currently selected in SAP2000 (max 50: id = running index, category = object type (Point/Frame/Area/…), name = object unique name).")]
        bool includeSelection = false,
        CancellationToken cancellationToken = default)
    {
        return service.GetAsync(includeSelection, cancellationToken);
    }
}
