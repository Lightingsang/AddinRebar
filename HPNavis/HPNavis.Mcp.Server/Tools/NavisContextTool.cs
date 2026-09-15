using System.ComponentModel;
using HPNavis.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPNavis.Mcp.Server.Tools;

/// <summary>Read-only snapshot of the Navisworks session so the AI can write correct code before touching the model.</summary>
[McpServerToolType]
public sealed class NavisContextTool(ContextService service)
{
    [McpServerTool(
        Name = NavisHostProfile.ContextToolName,
        Title = "Get Navisworks context",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description(
        "Returns the current Navisworks session: hostVersion, model title and docPath (absent when no model is open or it was never saved), isReadOnly, " +
        "isModifiable (true when a model is open and Navisworks is idle — no load, clash run or dialog in progress), units.length (the document's units), " +
        "executionEnabled, and navis {documentUnits, modelCount, models [{fileName, units, sourceFileName}], selectionSetCount, savedViewpointCount, clashTestCount, " +
        "hasClashModule (Manage only), heavyOperationsEnabled (the second opt-in for append/save/export/clash runs), isClear (no model open), isBusy, isModified}. " +
        "With includeSelection the current selection comes back (max 50) as {id = instance-guid hash, category = class display name, name = display name}. " +
        "Fails fast with a busy error while a script is running. Call this before execute_navis_code so the script matches the real model and its units.")]
    public Task<CallToolResult> GetContextAsync(
        [Description("Include the model items currently selected in Navisworks (max 50: id = instance-guid hash, category = class display name, name = display name).")]
        bool includeSelection = false,
        CancellationToken cancellationToken = default)
    {
        return service.GetAsync(includeSelection, cancellationToken);
    }
}
