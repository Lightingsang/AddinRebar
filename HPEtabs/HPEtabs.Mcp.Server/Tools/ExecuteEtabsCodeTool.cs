using System.ComponentModel;
using System.Text.Json;
using HPEtabs.Mcp.Server.Hosts;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPEtabs.Mcp.Server.Tools;

/// <summary>
///     The one tool that makes ETABS a runtime for the AI: any C# the model writes runs against the ETABS model
///     the bridge app is attached to. This class only names the tool and describes the ETABS script contract —
///     the only "API documentation" the model reads before writing code; validation, the pipe round trip and
///     run history are <see cref="ExecuteCodeService"/>, shared with every host.
/// </summary>
[McpServerToolType]
public sealed class ExecuteEtabsCodeTool(ExecuteCodeService service)
{
    /// <summary>Kept under 1 500 characters: the tiers, the forced save and the two refusal codes must all fit in what a model reads first.</summary>
    public const string ToolDescription =
        "Runs a C# script against the ETABS 22 model the HPEtabs MCP Bridge app is attached to (ETABSv1 API over COM). " +
        "Globals: sapModel (cSapModel), etabs (cOAPI), units (present units forced to kN_mm_C for the run: mm, kN, kN·mm, kN/mm²; restored after), ct, log(string), progress(cur,total,msg), args (args.Str/Int/Double/Bool(key, fallback)). " +
        "OAPI calls return int: check ret, throw InvalidOperationException($\"ETABS returned {ret} from X\"); bad inputs → ArgumentException. End with `return <value>;`. " +
        "No transaction or undo. Tiers, decided statically from `sapModel.X.Member(...)` chains (an alias, cast, ?., lambda or argument of a global = D): R read-only (Get*/Is*/Has*/Count/RefreshView/all of sapModel.Results/GetTableForDisplayArray; transaction=none). " +
        "W write (other members; transaction=auto): the bridge saves your model and copies a .EDB snapshot first (`snapshot` names it); unsaved or UNC models are refused; rolledBack:false after an exception means the changes persisted. " +
        "D destructive (SetModelIsLocked, RunAnalysis, DeleteResults, OpenFile/New*/Save(path), ApplyEditedTables, Start*/Modify*/Merge*/Reset*/Clear*/Rename*/Show*/Export*/Import*, any path-taking member): needs 'Allow destructive operations' in the bridge window, else error -32001; up to 600 s. " +
        "dryRun or transaction=none on a writing script = static preview (nothing runs; a PREVIEW diagnostic lists the members); manual runs like auto; changed = additions/deletions only. " +
        "cancel/timeout cannot interrupt a running ETABS call; the snapshot save counts against the timeout. Paths: a literal or args.Str(\"key\"), never UNC. " +
        "No Helper/ApplicationExit/dialogs; the base guard also blocks the File and GetProperty identifiers. Needs 'Allow AI code execution' in the HPEtabs MCP Bridge window (a separate app, not inside ETABS).";

    [McpServerTool(
        Name = EtabsHostProfile.ExecuteToolName,
        Title = "Execute C# in ETABS",
        ReadOnly = false,
        Destructive = true,
        Idempotent = false,
        OpenWorld = false)]
    [Description(ToolDescription)]
    public Task<CallToolResult> ExecuteAsync(
        [Description("C# script body, max 32 KB. No `await`, no System.IO / System.Net / reflection / expression trees / interop, no Helper or application lifecycle calls, no dialogs (blocked by the guard).")]
        string code,
        [Description("auto (default): writing script — the bridge saves the model and takes a .EDB snapshot before running. none: read-only; a script that writes is refused with a PREVIEW diagnostic. manual: accepted for compatibility, behaves like auto.")]
        string transaction = TransactionModes.Auto,
        [Description("Static preview for a writing script (nothing runs; the PREVIEW diagnostic lists what it would call); a read-only script runs normally. Refused for destructive members.")]
        bool dryRun = false,
        [Description("Cooperative timeout in seconds, 5–120 (up to 600 while destructive operations are allowed). Checked between OAPI calls only — a running RunAnalysis/Save/OpenFile cannot be interrupted.")]
        int timeoutSeconds = 30,
        [Description("Short name for the audit log and the snapshot file name. Max 64 characters; letters, digits, _ and - survive, the rest becomes _.")]
        string? label = null,
        [Description("Optional JSON object handed to the script as `args` (e.g. {\"frame\": \"12\", \"section\": \"C40x40\"}). Keys are matched case-insensitively.")]
        JsonElement? args = null,
        IProgress<ProgressNotificationValue>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return service.ExecuteAsync(code, transaction, dryRun, timeoutSeconds, label, args, progress, cancellationToken);
    }
}
