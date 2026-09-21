using System.ComponentModel;
using System.Text.Json;
using HPTekla.Mcp.Server.Hosts.Tekla;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPTekla.Mcp.Server.Hosts.Tekla.Tools;

/// <summary>
///     The primary tool that executes C# Roslyn scripts against the active Tekla Structures model
///     via the HPTekla MCP Bridge.
/// </summary>
[McpServerToolType]
public sealed class ExecuteTeklaCodeTool(ExecuteCodeService service)
{
    public const string ToolDescription =
        "Runs a C# script against the Tekla Structures 2025.0 model via the HPTekla MCP Bridge extension (Tekla Open API). " +
        "Globals: model (Tekla.Structures.Model.Model), selector (ModelObjectSelector), ct (CancellationToken), log(string), progress(cur,total,msg), args (args.Str/Int/Double/Bool). " +
        "Coordinates and lengths are in millimetres (mm). " +
        "Tiers: " +
        "R read-only: Get*/Is*/Has*/Count/Find*/Exist/Query; transaction=\"none\". " +
        "W write: Insert/Modify/Delete; transaction=\"auto\": the bridge executes changes in model memory and commits with model.CommitChanges() upon success. " +
        "dryRun=true runs script logic in memory without calling model.CommitChanges(), ensuring zero persistence. " +
        "Heavy operations (e.g. IFC export, drawing numbering) require enabling the 'Allow heavy operations' checkbox in HPTekla MCP Bridge. " +
        "No MessageBox, Picker dialogs, Process, reflection, threading, #r, or #load. Needs 'Allow AI code execution' enabled in the bridge window.";

    [McpServerTool(
        Name = TeklaHostProfile.ExecuteToolName,
        Title = "Execute C# in Tekla",
        ReadOnly = false,
        Destructive = true,
        Idempotent = false,
        OpenWorld = false)]
    [Description(ToolDescription)]
    public Task<CallToolResult> ExecuteAsync(
        [Description("C# script body, max 32 KB. Globals: model, selector, args, ct, log, progress. No await, no System.IO/Net, no MessageBox/Picker/Quit/#r/#load.")]
        string code,
        [Description("auto (default): writing script — bridge commits changes upon success. none: read-only query. manual: script manages transactions.")]
        string transaction = TransactionModes.Auto,
        [Description("Test execution in memory without calling CommitChanges(). Confirms logic and catches runtime exceptions without modifying the model.")]
        bool dryRun = false,
        [Description("Cooperative timeout in seconds, 5–120 (up to 600 while heavy operations are allowed).")]
        int timeoutSeconds = 30,
        [Description("Short name for the audit log and snapshot file name. Max 64 characters; letters, digits, _ and - survive, the rest becomes _.")]
        string? label = null,
        [Description("Optional JSON object handed to the script as `args` (e.g. {\"startX\": 0, \"profile\": \"HEA300\"}). Keys are matched case-insensitively.")]
        JsonElement? args = null,
        IProgress<ProgressNotificationValue>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return service.ExecuteAsync(code, transaction, dryRun, timeoutSeconds, label, args, progress, cancellationToken);
    }
}
