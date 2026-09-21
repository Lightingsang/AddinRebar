using System.ComponentModel;
using System.Text.Json;
using HPRobot.Mcp.Server.Hosts.Robot;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPRobot.Mcp.Server.Hosts.Robot.Tools;

/// <summary>
///     The primary tool that executes C# Roslyn scripts against the active Robot Structural Analysis model
///     via the HPRobot MCP Bridge.
/// </summary>
[McpServerToolType]
public sealed class ExecuteRobotCodeTool(ExecuteCodeService service)
{
    public const string ToolDescription =
        "Runs a C# script against the Robot Structural Analysis Professional 2026 model the HPRobot MCP Bridge app is attached to (RobotOM API). " +
        "Globals: robot (IRobotApplication), structure (IRobotStructure), units (IRobotUnitMngr; metric m, kN, kN·m, MPa enforced during run), ct, log(string), progress(cur,total,msg), args (args.Str/Int/Double/Bool). " +
        "Robot has no transaction or undo API. Tiers decided statically: " +
        "R read-only (Get*/Is*/Has*/Count/Find*/Exist/Query; transaction=none; no snapshot). " +
        "W write (Create/Add*/SetLabel*/SetValue*/Store*/Update; transaction=auto; bridge saves model and copies a .rtd snapshot first; unsaved models are refused; rolledBack:false means changes persisted). " +
        "D destructive/heavy (Calculate, Delete*, project.New/Open/Close/SaveAs): needs 'Allow heavy/destructive operations' in HPRobot MCP Bridge window, else error -32001; up to 300 s. " +
        "dryRun or transaction=none on a writing script = static preview (nothing runs; PREVIEW diagnostic lists called members). " +
        "cancel/timeout cannot interrupt a running Robot calculation solver call. " +
        "No Quit/ApplicationExit/Interactive/MessageBox/Process/#r/#load. Needs 'Allow AI code execution' in the HPRobot MCP Bridge window.";

    [McpServerTool(
        Name = RobotHostProfile.ExecuteToolName,
        Title = "Execute C# in Robot",
        ReadOnly = false,
        Destructive = true,
        Idempotent = false,
        OpenWorld = false)]
    [Description(ToolDescription)]
    public Task<CallToolResult> ExecuteAsync(
        [Description("C# script body, max 32 KB. Globals: robot (IRobotApplication), structure (IRobotStructure), units, args, ct, log, progress. No await, no System.IO/Net, no Quit/dialogs/#r/#load.")]
        string code,
        [Description("auto (default): writing script — bridge saves model and takes a .rtd snapshot before running. none: read-only; writing script is refused with a PREVIEW diagnostic. manual: behaves like auto.")]
        string transaction = TransactionModes.Auto,
        [Description("Static preview for a writing script (nothing runs; PREVIEW diagnostic lists what would be called); read-only scripts run normally. Refused for destructive members.")]
        bool dryRun = false,
        [Description("Cooperative timeout in seconds, 5–120 (up to 300 while heavy operations are allowed). Checked between calls; a running Calculate solver cannot be interrupted.")]
        int timeoutSeconds = 30,
        [Description("Short name for the audit log and snapshot file name. Max 64 characters; letters, digits, _ and - survive, the rest becomes _.")]
        string? label = null,
        [Description("Optional JSON object handed to the script as `args` (e.g. {\"nodeNumber\": 1, \"section\": \"HEA 200\"}). Keys are matched case-insensitively.")]
        JsonElement? args = null,
        IProgress<ProgressNotificationValue>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return service.ExecuteAsync(code, transaction, dryRun, timeoutSeconds, label, args, progress, cancellationToken);
    }
}
