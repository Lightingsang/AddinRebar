namespace HPGrasshopper.Mcp.Server.Tools;

using System.ComponentModel;
using System.Text.Json;
using HPGrasshopper.Mcp.Server.Hosts;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

/// <summary>
///     Core execution tool enabling AI agents to run C# Roslyn scripts in Grasshopper and Revit.
///     Supports multi-engine globals, transaction tiers, and dual-layer rollback (Revit TransactionGroup + GH_UndoRecord).
/// </summary>
[McpServerToolType]
public sealed class ExecuteGrasshopperCodeTool(ExecuteCodeService service)
{
    public const string ToolDescription =
        "Runs a C# script inside Rhino.Inside.Revit 2026 and Grasshopper with user privileges. " +
        "Globals: doc (GH_Document active canvas), canvas (GH_Canvas), rhinoDoc (RhinoDoc), revitDoc (Document), " +
        "ct (CancellationToken check in long loops), log(string), progress(int current, int total, string message), " +
        "args (args.Str/Double/Int/Long/Bool(key, fallback), args.Obj/List(key), args.Has/Require(key)). " +
        "Modifications touching Revit elements are wrapped in a Revit TransactionGroup ('MCP Grasshopper: <label>'). " +
        "transaction=auto commits when the script returns; none is read-only; manual manages its own Transaction. " +
        "dryRun=true runs the script, then rolls back all Revit elements and Grasshopper canvas modifications. " +
        "Requires the user to tick 'Allow AI Grasshopper execution' in the HPGrasshopper MCP Bridge window inside Revit.";

    [McpServerTool(
        Name = GrasshopperHostProfile.ExecuteCodeToolName,
        Title = "Execute C# in Grasshopper",
        ReadOnly = false,
        Destructive = true,
        Idempotent = false,
        OpenWorld = false)]
    [Description(ToolDescription)]
    public Task<CallToolResult> ExecuteAsync(
        [Description("C# script body, max 32 KB. Forbidden: System.IO, System.Net, Process, unauthorized reflection.")]
        string code,
        [Description("auto (default): commits changes. none: read-only; any modification fails. manual: manages own Transaction.")]
        string transaction = TransactionModes.Auto,
        [Description("Run the script, then roll back all Revit elements and canvas modifications. Use first for destructive operations.")]
        bool dryRun = false,
        [Description("Cooperative timeout in seconds, 5–600.")]
        int timeoutSeconds = 60,
        [Description("Short name for the audit log and Revit undo history. Max 64 characters.")]
        string? label = null,
        [Description("Optional JSON object handed to the script as args. Keys are matched case-insensitively.")]
        JsonElement? args = null,
        IProgress<ProgressNotificationValue>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return service.ExecuteAsync(code, transaction, dryRun, timeoutSeconds, label, args, progress, cancellationToken);
    }
}
