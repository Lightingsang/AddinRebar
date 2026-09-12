using System.ComponentModel;
using System.Text;
using System.Text.Json;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Services;
using HPRebar.Mcp.Contracts;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPRebar.Mcp.Server.Tools;

/// <summary>
///     The one tool that makes Revit a runtime for the AI: any C# the model writes runs inside the user's
///     Revit session. Validation here is about size and shape only; what the code may do is decided by the
///     bridge (guard, opt-in switch, transaction policy) and reported back through <see cref="ExecuteResult"/>.
/// </summary>
[McpServerToolType]
public sealed class ExecuteRevitCodeTool(IRevitBridgeClient bridge, ResultFormatter formatter, IOptions<BridgeOptions> options, ToolManager? registry = null)
{
    private const int MinTimeoutSeconds = 5;
    private const int MaxTimeoutSeconds = 120;
    private const int MaxLabelLength = 64;
    private const int ReusableMinLines = 12;

    /// <summary>Cheap heuristic: a successful script of some size, with a loop, or that changed the model.</summary>
    internal static bool LooksReusable(string code, ExecuteResult result)
    {
        if (result.IsError) return false;
        var lines = code.Split('\n').Count(l => !string.IsNullOrWhiteSpace(l));
        var loops = code.Contains("foreach ", StringComparison.Ordinal) || code.Contains("for (", StringComparison.Ordinal) || code.Contains("while (", StringComparison.Ordinal);
        return lines >= ReusableMinLines || loops || result.Changed.Added > 0 || result.Changed.Modified > 0;
    }

    [McpServerTool(
        Name = "execute_revit_code",
        Title = "Execute C# in Revit",
        ReadOnly = false,
        Destructive = true,
        Idempotent = false,
        OpenWorld = false)]
    [Description(
        "Runs a C# script inside the open Revit session with the user's full privileges. " +
        "Globals: doc (Document), uidoc (UIDocument), app (Application), uiapp (UIApplication), " +
        "ct (CancellationToken — check it inside long loops), log(string) to append output, " +
        "progress(int current, int total, string message) to report progress, " +
        "args (data passed in the `args` parameter: args.Str/Double/Int/Long/Bool(key, fallback), args.Obj(key), args.List(key), args.Has(key), args.Require(key)). " +
        "Prefer args over literals for anything that may change between calls — a script whose text stays identical compiles once and is cached. " +
        "End with `return <value>;` to send a result; Revit objects are summarised (ElementId as number, XYZ as {x,y,z} in feet, Element as {id,name,category}). " +
        "Default usings: System, System.Linq, System.Collections.Generic, Autodesk.Revit.DB, Autodesk.Revit.UI, Autodesk.Revit.DB.Structure. " +
        "All model changes land in one undoable group named 'MCP: <label>'. Use dryRun=true first for anything destructive. " +
        "Fails with isError=true and diagnostics when the code does not compile, throws, is blocked by the guard, or exceeds the timeout; nothing is committed in those cases. " +
        "Requires the user to have enabled code execution in the HPRebar MCP Bridge window inside Revit.")]
    public Task<CallToolResult> ExecuteAsync(
        [Description("C# script body, max 32 KB. No `await`, no System.IO / System.Net / System.Diagnostics.Process / reflection (blocked by the guard).")]
        string code,
        [Description("auto (default): the bridge wraps the script in one Transaction. manual: the script opens its own Transaction(s) inside one TransactionGroup. none: read-only; any modification fails.")]
        string transaction = TransactionModes.Auto,
        [Description("Run the script, then roll everything back. Use first for destructive changes.")]
        bool dryRun = false,
        [Description("Cooperative timeout in seconds, 5–120. The script sees it through `ct`.")]
        int timeoutSeconds = 30,
        [Description("Short name shown in Revit's Undo history as 'MCP: <label>'. Max 64 characters.")]
        string? label = null,
        [Description("Optional JSON object handed to the script as `args` (e.g. {\"spacing\": 150, \"names\": [\"A\",\"B\"]}). Keys are matched case-insensitively.")]
        JsonElement? args = null,
        IProgress<ProgressNotificationValue>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return formatter.RunAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(code)) return formatter.Error("code is empty. Send the C# script body to run.");

            var bytes = Encoding.UTF8.GetByteCount(code);
            if (bytes > options.Value.MaxSourceBytes)
                return formatter.Error($"code is {bytes:N0} bytes; the limit is {options.Value.MaxSourceBytes:N0}. Split the work into smaller scripts.");

            var mode = TransactionModes.Normalize(transaction);
            if (mode is null)
                return formatter.Error($"transaction must be one of: {string.Join(", ", TransactionModes.All)} (got '{transaction}').");

            var timeout = Math.Clamp(timeoutSeconds, MinTimeoutSeconds, MaxTimeoutSeconds);
            var cleanLabel = string.IsNullOrWhiteSpace(label) ? "script" : label.Trim();
            if (cleanLabel.Length > MaxLabelLength) cleanLabel = cleanLabel[..MaxLabelLength];

            if (args is { ValueKind: not (JsonValueKind.Object or JsonValueKind.Null or JsonValueKind.Undefined) })
                return formatter.Error("args must be a JSON object (or omitted).");

            var request = new ExecuteRequest(code, mode, dryRun, timeout, cleanLabel, args);
            // Forward synchronously: Progress<T> would hop to the thread pool and reorder the steps.
            var bridgeProgress = progress is null ? null : new SynchronousProgress<ProgressParams>(p => progress.Report(new ProgressNotificationValue
            {
                Progress = p.Progress,
                Total = p.Total,
                Message = p.Message,
            }));

            var result = await bridge.SendAsync<ExecuteResult>(
                JsonRpcMethods.Execute,
                request,
                TimeSpan.FromSeconds(timeout + options.Value.ExtraTimeoutSeconds),
                bridgeProgress,
                cancellationToken).ConfigureAwait(false);

            // Memory: a successful ad-hoc run is the raw material of a future tool.
            if (registry is not null)
            {
                try
                {
                    result.RunId = registry.RecordAdhoc(code, args, dryRun, result);
                    if (LooksReusable(code, result))
                        result.Hint = $"This run succeeded and looks reusable. To keep it as a tool: get_run {result.RunId} (shows literals to turn into args), then propose_tool. Skip if it was a one-off.";
                }
                catch (Exception exception) { Console.Error.WriteLine("run history unavailable: " + exception.Message); }
            }

            return formatter.FromExecute(result);
        });
    }
}
