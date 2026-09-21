using System.ComponentModel;
using HPPowerBi.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPPowerBi.Mcp.Server.Tools;

/// <summary>Read-only snapshot of the connected Power BI Desktop session and tabular model.</summary>
[McpServerToolType]
public sealed class GetPowerBiContextTool(ContextService service)
{
    [McpServerTool(
        Name = PowerBiHostProfile.ContextToolName,
        Title = "Get Power BI context",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description(
        "Returns the Power BI Desktop session the bridge is connected to: hostVersion (2026), docTitle and docPath of the .pbix/.pbip file, " +
        "isModifiable (connected and model is writable), executionEnabled, and powerBi info " +
        "{isConnected, attachedPid, localPort, databaseName, compatibilityLevel, mutationEnabled, tableCount, measureCount, relationshipCount}. " +
        "Fails with 'not connected' if Power BI Desktop is not running or bridge is not connected. " +
        "Call this before scripting or modifying the model to verify connection and model metadata.")]
    public Task<CallToolResult> GetContextAsync(
        [Description("Include current selection (reserved for future view selection).")]
        bool includeSelection = false,
        CancellationToken cancellationToken = default)
    {
        return service.GetAsync(includeSelection, cancellationToken);
    }
}
