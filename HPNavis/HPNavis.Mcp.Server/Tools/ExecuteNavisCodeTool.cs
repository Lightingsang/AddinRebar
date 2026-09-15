using System.ComponentModel;
using System.Text.Json;
using HPNavis.Mcp.Server.Hosts;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPNavis.Mcp.Server.Tools;

/// <summary>
///     The one tool that makes Navisworks a runtime for the AI: any C# the model writes runs inside the
///     user's Navisworks session. This class only names the tool and describes the Navisworks script
///     contract — the only "API documentation" the model reads before writing code; validation, the pipe
///     round trip and run history are <see cref="ExecuteCodeService"/>, shared with every host.
/// </summary>
[McpServerToolType]
public sealed class ExecuteNavisCodeTool(ExecuteCodeService service)
{
    [McpServerTool(
        Name = NavisHostProfile.ExecuteToolName,
        Title = "Execute C# in Navisworks",
        ReadOnly = false,
        Destructive = true,
        Idempotent = false,
        OpenWorld = false)]
    [Description(
        "Runs a C# script inside the open Navisworks Manage model with the user's full privileges. Runtime is .NET Framework 4.8: no Span, Random.Shared, async or positional records. " +
        "Globals: doc (Document), app (Year, HasClashModule, IsModified), units (document units: units.ToMm(du), units.ToDrawing(mm), units.Label), " +
        "ct (check it in long loops), log(string), progress(cur, total, msg), args (args.Str/Int/Double/Bool(key, fallback), args.List/Obj(key)). " +
        "End with `return <value>;`. Navisworks objects are summarised (boxes in mm); collections stop at 200 items. " +
        "Find items with a Search (var s = new Search(); s.Selection.SelectAll(); s.SearchConditions.Add(SearchCondition.HasPropertyByDisplayName(\"Item\", \"Name\").DisplayStringContains(text)); s.FindAll(doc, false)) rather than walking Descendants. " +
        "Navisworks is a review tool: geometry is read-only. Undoable edits (one Undo entry `MCP: <label>`): selection sets, saved viewpoints, comments, permanent appearance overrides, hidden/required, current selection, clash tests and result status, TimeLiner tasks; the current viewpoint and temporary overrides may not be undoable. " +
        "Heavy (doc.AppendFile/MergeFile/RemoveFile/OpenFile/Clear/UpdateFiles, SaveFile/Export*/PublishFile/GenerateImage, TestsRun*/TestsCompact*) needs the user to tick 'Allow heavy operations' (a HEAVY diagnostic says when it is off); never undone, not interruptible, up to 600 s, no UNC paths — ask the user to save first. " +
        "transaction=auto commits on return; none is read-only and fails when the model changed (fingerprint check); manual runs like auto. dryRun undoes only the bridge's own Undo entry — `rolledBack:false` means the change may have persisted. " +
        "isError=true with diagnostics on compile error, exception, guard/HEAVY refusal or timeout. Never open a Transaction, call Undo/Redo, show a dialog or use Document.Database — the guard rejects them. " +
        "Requires the user to tick 'Allow AI code execution' in the HPNavis MCP Bridge window (Add-ins ▸ HPNavis MCP).")]
    public Task<CallToolResult> ExecuteAsync(
        [Description("C# script body, max 32 KB. No `await`, no System.IO / System.Net / System.Data / reflection / expression trees, no Transaction or Undo calls, no dialogs (blocked by the guard).")]
        string code,
        [Description("auto (default): the bridge commits its transaction when the script returns. none: read-only; any change fails (and is undone when it left an undo entry). manual: accepted for compatibility, behaves like auto.")]
        string transaction = TransactionModes.Auto,
        [Description("Run the script, then undo the bridge's own Undo entry. Refused for heavy calls (nothing to undo). `changed` still reports what happened.")]
        bool dryRun = false,
        [Description("Cooperative timeout in seconds, 5–120 (up to 600 while heavy operations are allowed). The script sees it through `ct`; a script that ignores `ct` blocks Navisworks until it returns.")]
        int timeoutSeconds = 30,
        [Description("Short name for the audit log and the Undo entry `MCP: <label>`. Max 64 characters.")]
        string? label = null,
        [Description("Optional JSON object handed to the script as `args` (e.g. {\"text\": \"Wall\", \"name\": \"MCP walls\"}). Keys are matched case-insensitively.")]
        JsonElement? args = null,
        IProgress<ProgressNotificationValue>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return service.ExecuteAsync(code, transaction, dryRun, timeoutSeconds, label, args, progress, cancellationToken);
    }
}
