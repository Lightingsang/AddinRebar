using System.ComponentModel;
using System.Text.Json;
using HPPowerBi.Mcp.Server.Hosts;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPPowerBi.Mcp.Server.Tools;

/// <summary>
///     Executes C# Roslyn script against the Power BI Tabular Model (Microsoft.AnalysisServices.Tabular).
///     Injected globals: model (Model), server (Server), adomd (AdomdConnection), ct, log, progress, args.
/// </summary>
[McpServerToolType]
public sealed class ExecutePowerBiCodeTool(ExecuteCodeService service)
{
    public const string ToolDescription =
        "Runs a C# script against the Power BI Desktop tabular model via AMO-TOM (Microsoft.AnalysisServices.Tabular). " +
        "Globals: model (active TOM Model), server (TOM Server connected to local Analysis Services), " +
        "adomd (active AdomdConnection for DAX execution), ct, log(string), progress(cur,total,msg), args (args.Str/Int/Double/Bool(key, fallback)). " +
        "Return value: End with `return <value>;`. " +
        "Mutation safety: Writing operations that call model.SaveChanges() require 'Allow Model Mutation' ticked on the Bridge window, else error -32001. " +
        "A snapshot backup of model metadata is created before applying mutations. " +
        "dryRun=true validates script compilation and static structure without committing changes. " +
        "Timeout: 5–120 seconds (up to 600 seconds when enabled). " +
        "Forbidden: System.IO, reflection, processes, threads, network, modal dialogs, disposing/disconnecting server or adomd.";

    [McpServerTool(
        Name = PowerBiHostProfile.ExecuteToolName,
        Title = "Execute C# in Power BI",
        ReadOnly = false,
        Destructive = true,
        Idempotent = false,
        OpenWorld = false)]
    [Description(ToolDescription)]
    public Task<CallToolResult> ExecuteAsync(
        [Description("C# script body, max 32 KB. Has access to model, server, adomd.")]
        string code,
        [Description("auto (default): writing script. none: read-only script. manual: accepted for compatibility.")]
        string transaction = TransactionModes.Auto,
        [Description("Static preview/check without committing changes.")]
        bool dryRun = false,
        [Description("Cooperative timeout in seconds, 5–120 (up to 600s).")]
        int timeoutSeconds = 30,
        [Description("Short label for audit log and snapshot name.")]
        string? label = null,
        [Description("Optional JSON object passed to the script as args.")]
        JsonElement? args = null,
        IProgress<ProgressNotificationValue>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return service.ExecuteAsync(code, transaction, dryRun, timeoutSeconds, label, args, progress, cancellationToken);
    }
}
