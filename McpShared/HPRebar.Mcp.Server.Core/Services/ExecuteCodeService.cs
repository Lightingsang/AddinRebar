using System.Text;
using System.Text.Json;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPRebar.Mcp.Server.Services;

/// <summary>
///     The body of every host's `execute_*_code` tool: validate size and shape, send the script down the
///     pipe, record the run in the registry and nudge the model when the run looks reusable. The host
///     tool class only contributes its name and description; what the code may do is decided by the
///     bridge (guard, opt-in switch, transaction policy) and reported back through <see cref="ExecuteResult"/>.
/// </summary>
public sealed class ExecuteCodeService(IRevitBridgeClient bridge, ResultFormatter formatter, IOptions<BridgeOptions> options, ToolManager? registry = null)
{
    public const int MinTimeoutSeconds = 5;

    /// <summary>Historical ceiling; the effective one is <see cref="IHostProfile.MaxTimeoutSeconds"/> of the connected host (120 for Revit/AutoCAD).</summary>
    public const int MaxTimeoutSeconds = 120;
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

    public Task<CallToolResult> ExecuteAsync(
        string code,
        string transaction,
        bool dryRun,
        int timeoutSeconds,
        string? label,
        JsonElement? args,
        IProgress<ProgressNotificationValue>? progress,
        CancellationToken cancellationToken)
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

            var timeout = Math.Clamp(timeoutSeconds, MinTimeoutSeconds, bridge.Profile.MaxTimeoutSeconds);
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
                bridge.Profile.Method(JsonRpcMethods.ExecuteSuffix),
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
