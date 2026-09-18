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
        "Globals: doc (Document), db (Database), ed (Editor — WriteMessage/SelectImplied/SelectAll only; every Get* prompt is blocked), app (DocumentCollection), " +
        "tr (the Transaction the bridge opened: read with tr.GetObject(id, OpenMode.ForRead), add with AppendEntity + tr.AddNewlyCreatedDBObject(obj, true); never Commit/Abort/Dispose it and never call StartTransaction or LockDocument), " +
        "civil (CivilDocument: GetAlignmentIds(), GetSurfaceIds(), GetPipeNetworkIds(), GetSiteIds(), CorridorCollection, CogoPoints, GetAllPointIds(), Settings, Styles; null when the drawing holds no Civil data — check it), " +
        "units (the Civil drawing unit, Meters or Feet: units.ToDrawing(mm), units.ToMm(du), units.Label — Civil coordinates, stations and elevations are drawing units; convert plan geometry to mm at the tool boundary, keep stations and elevations in drawing units and say so), " +
        "ct (check it inside long loops), log(string), progress(int current, int total, string message), " +
        "args (args.Str/Double/Int/Long/Bool(key, fallback), args.Obj/List(key), args.Has/Require(key); prefer args over literals so identical text compiles once). " +
        "End with `return <value>;`. Returned objects are summarised: ObjectId → {handle,class}, Entity → {handle,type,layer,dxfName,name for Civil entities}, Point3d → {x,y,z}. " +
        "Default usings: System, System.Linq, System.Collections.Generic, Autodesk.AutoCAD.ApplicationServices/DatabaseServices/EditorInput/Geometry/Colors, Autodesk.Civil, Autodesk.Civil.ApplicationServices/DatabaseServices/DatabaseServices.Styles/Settings. " +
        "transaction=auto commits when the script returns; none is read-only and fails if anything changed; manual runs like auto. dryRun runs everything, rolls it back and still reports `changed`. " +
        "Blocked: Rebuild/RebuildAll/RebuildSnapshot, DataShortcuts, SurveyProjects, file import/export members (CreateFromLandXML, ExportToDEM, ImportPoints…), AeccUiMgd dialogs, COM interop. " +
        "U in Civil 3D reverts every AI run made since the user's last command. " +
        "Fails with isError=true and diagnostics on compile error, exception, guard rejection or timeout; nothing is kept then. " +
        "Requires the user to tick 'Allow AI code execution' in the HPCivil3d MCP Bridge window (ribbon HPCivil3d > MCP > MCP Bridge, command HPC3DMCPBRIDGE) inside Civil 3D.")]
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
