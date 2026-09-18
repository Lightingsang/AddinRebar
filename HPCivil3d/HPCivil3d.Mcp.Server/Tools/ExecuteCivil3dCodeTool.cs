using System.ComponentModel;
using System.Text.Json;
using HPCivil3d.Mcp.Server.Hosts;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPCivil3d.Mcp.Server.Tools;

/// <summary>
///     The one tool that makes Civil 3D a runtime for the AI: any C# the model writes runs inside the user's
///     Civil 3D session. This class only names the tool and describes the Civil 3D script contract — the only
///     "API documentation" the model reads before writing code; validation, the pipe round trip and run
///     history are <see cref="ExecuteCodeService"/>, shared with every host.
/// </summary>
[McpServerToolType]
public sealed class ExecuteCivil3dCodeTool(ExecuteCodeService service)
{
    [McpServerTool(
        Name = Civil3dHostProfile.ExecuteToolName,
        Title = "Execute C# in Civil 3D",
        ReadOnly = false,
        Destructive = true,
        Idempotent = false,
        OpenWorld = false)]
    [Description(
        "Runs a C# script inside the open Civil 3D drawing with the user's full privileges. " +
        "Globals: doc, db, ed (WriteMessage/SelectImplied/SelectAll only), app, tr (the bridge's Transaction: tr.GetObject(id, OpenMode.ForRead), AppendEntity + tr.AddNewlyCreatedDBObject(obj, true); never Commit/Abort/Dispose it, never StartTransaction or LockDocument), " +
        "civil (CivilDocument: GetAlignmentIds/GetSurfaceIds/GetPipeNetworkIds/GetSiteIds, CorridorCollection, CogoPoints, Settings, Styles), " +
        "units (the Civil drawing unit, Meters or Feet: ToDrawing(mm), ToMm(du), Label — plan x/y cross the tool boundary in mm; stations, elevations and areas stay in the Civil unit; a drawing without Civil settings reports Feet whatever INSUNITS says, so read get_civil3d_context and warn on insunitsMismatch), " +
        "ct, log(string), progress(cur, total, msg), args (Str/Double/Int/Long/Bool(key, fallback), Obj/List(key), Has/Require(key)). " +
        "Civil and AutoCAD both define Entity/DBObject/Surface: write Autodesk.Civil.DatabaseServices.Surface in full. End with `return <value>;` (Entity → {handle,type,layer,dxfName,name}, CogoPoint adds number/x/y/elevation, Point3d → {x,y,z} — all in drawing units). " +
        "Usings: the AutoCAD and Autodesk.Civil namespaces. " +
        "transaction=auto commits on return; none is read-only and fails if anything changed; manual runs like auto. dryRun runs everything, rolls back and still reports `changed`. " +
        "Blocked: Rebuild*, DataShortcuts, SurveyProjects, file import/export, AeccUiMgd dialogs, COM interop, Editor prompts. U in Civil 3D reverts every AI run since the user's last command. " +
        "isError=true + diagnostics on compile error, exception, guard rejection or timeout; nothing is kept then. " +
        "Requires 'Allow AI code execution' ticked in the HPCivil3d MCP Bridge window (command HPC3DMCPBRIDGE).")]
    public Task<CallToolResult> ExecuteAsync(
        [Description("C# script body, max 32 KB. No `await`, no System.IO / System.Net / System.Diagnostics.Process / reflection, no Editor prompts or commands, no Civil rebuilds or data-shortcut calls (blocked by the guard).")]
        string code,
        [Description("auto (default): the bridge commits `tr` when the script returns. none: read-only; any change fails. manual: accepted for compatibility, behaves like auto.")]
        string transaction = TransactionModes.Auto,
        [Description("Run the script, then roll everything back. Use first for destructive changes; `changed` still reports what would have happened.")]
        bool dryRun = false,
        [Description("Cooperative timeout in seconds, 5–120. The script sees it through `ct`; a script that ignores `ct` blocks Civil 3D until it returns.")]
        int timeoutSeconds = 30,
        [Description("Short name for the audit log and the command line summary. Max 64 characters.")]
        string? label = null,
        [Description("Optional JSON object handed to the script as `args` (e.g. {\"alignment\": \"Road A\", \"stepMm\": 10000}). Keys are matched case-insensitively.")]
        JsonElement? args = null,
        IProgress<ProgressNotificationValue>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return service.ExecuteAsync(code, transaction, dryRun, timeoutSeconds, label, args, progress, cancellationToken);
    }
}
