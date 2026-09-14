using System.ComponentModel;
using System.Text.Json;
using HPAutoCad.Mcp.Server.Hosts;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPAutoCad.Mcp.Server.Tools;

/// <summary>
///     The one tool that makes AutoCAD a runtime for the AI: any C# the model writes runs inside the user's
///     AutoCAD session. This class only names the tool and describes the AutoCAD script contract — the
///     only "API documentation" the model reads before writing code; validation, the pipe round trip and
///     run history are <see cref="ExecuteCodeService"/>, shared with every host.
/// </summary>
[McpServerToolType]
public sealed class ExecuteAutocadCodeTool(ExecuteCodeService service)
{
    [McpServerTool(
        Name = AutocadHostProfile.ExecuteToolName,
        Title = "Execute C# in AutoCAD",
        ReadOnly = false,
        Destructive = true,
        Idempotent = false,
        OpenWorld = false)]
    [Description(
        "Runs a C# script inside the open AutoCAD drawing with the user's full privileges. " +
        "Globals: doc (Document), db (Database), ed (Editor — WriteMessage/SelectImplied/SelectAll only; every Get* prompt is blocked), app (DocumentCollection), " +
        "tr (the Transaction the bridge opened: read with tr.GetObject(id, OpenMode.ForRead), add with AppendEntity + tr.AddNewlyCreatedDBObject(obj, true); never Commit/Abort/Dispose it and never call StartTransaction or LockDocument — the guard rejects them), " +
        "units (drawing unit from INSUNITS: units.ToDrawing(mm), units.ToMm(du), units.Label — all coordinates are drawing units), " +
        "ct (check it inside long loops), log(string), progress(int current, int total, string message), " +
        "args (args.Str/Double/Int/Long/Bool(key, fallback), args.Obj/List(key), args.Has/Require(key); prefer args over literals so identical text compiles once). " +
        "End with `return <value>;`. Returned AutoCAD objects are summarised: ObjectId → {handle,class}, Entity → {handle,type,layer,dxfName}, Point3d → {x,y,z}, ObjectIdCollection/SelectionSet → arrays of ids. " +
        "Default usings: System, System.Linq, System.Collections.Generic, Autodesk.AutoCAD.ApplicationServices, DatabaseServices, EditorInput, Geometry, Colors. " +
        "transaction=auto commits when the script returns; none is read-only and fails if anything changed; manual runs like auto. dryRun runs everything, rolls it back and still reports `changed`. " +
        "U in AutoCAD reverts every AI run made since the user's last command. " +
        "Fails with isError=true and diagnostics on compile error, exception, guard rejection or timeout; nothing is kept then. " +
        "Requires the user to tick 'Allow AI code execution' in the HPAutoCad MCP Bridge window (command HPMCPBRIDGE) inside AutoCAD.")]
    public Task<CallToolResult> ExecuteAsync(
        [Description("C# script body, max 32 KB. No `await`, no System.IO / System.Net / System.Diagnostics.Process / reflection, no Editor prompts or commands (blocked by the guard).")]
        string code,
        [Description("auto (default): the bridge commits `tr` when the script returns. none: read-only; any change fails. manual: accepted for compatibility, behaves like auto.")]
        string transaction = TransactionModes.Auto,
        [Description("Run the script, then roll everything back. Use first for destructive changes; `changed` still reports what would have happened.")]
        bool dryRun = false,
        [Description("Cooperative timeout in seconds, 5–120. The script sees it through `ct`; a script that ignores `ct` blocks AutoCAD until it returns.")]
        int timeoutSeconds = 30,
        [Description("Short name for the audit log and the AutoCAD command line summary. Max 64 characters.")]
        string? label = null,
        [Description("Optional JSON object handed to the script as `args` (e.g. {\"lengthMm\": 1500, \"layer\": \"WALLS\"}). Keys are matched case-insensitively.")]
        JsonElement? args = null,
        IProgress<ProgressNotificationValue>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return service.ExecuteAsync(code, transaction, dryRun, timeoutSeconds, label, args, progress, cancellationToken);
    }
}
