using System.Diagnostics;
using ETABSv1;
using HPEtabs.McpBridge.Model;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Serilog;

namespace HPEtabs.McpBridge.Service;

/// <summary>
///     Runs one compiled, read-only script on the STA worker against the attached model. Before the script the
///     present units are forced to kN / mm / °C so every length the API hands out is already in millimetres;
///     the user's own API units are restored in <c>finally</c> whatever happened. There is nothing to commit or
///     roll back — ETABS has no transaction — so a run is: units in, script, units back. Writing tiers (save +
///     snapshot before the run, fingerprint after) are the next step; this runner refuses them.
/// </summary>
public sealed class EtabsScriptRunner
{
    public const eUnits ForcedUnits = eUnits.kN_mm_C;

    private readonly BridgeSettings _settings;
    private readonly EtabsResultSerializer _serializer;

    public EtabsScriptRunner(BridgeSettings settings, EtabsResultSerializer serializer)
    {
        _settings = settings;
        _serializer = serializer;
    }

    /// <summary>The unit system every run works in: identity conversions, because the API itself is switched to millimetres.</summary>
    public static ScriptUnits Units { get; } = new ScriptUnits(ForcedUnits.ToString(), 1.0,
        "present units forced to kN_mm_C for this run: lengths mm, forces kN, moments kN·mm, stresses kN/mm²; the user's units are restored afterwards");

    /// <param name="cancelSource">Cancelled by cancel_execution; the runner adds the timeout on top.</param>
    /// <param name="maxTimeoutSeconds">Ceiling captured on the pipe thread with the tier verdict.</param>
    public ExecuteResult Run(cOAPI etabs, cSapModel sapModel, ExecuteRequest request, Script<object> script, int maxTimeoutSeconds,
        IProgress<ScriptProgress>? progress, CancellationTokenSource cancelSource)
    {
        var logs = new List<string>();
        var droppedLogs = 0;
        var mode = TransactionModes.Normalize(request.Transaction) ?? TransactionModes.Auto;
        if (mode == TransactionModes.Manual) logs.Add("transaction=\"manual\" behaves like \"auto\" in ETABS: there is no transaction to manage.");

        var timeoutSeconds = Math.Clamp(request.TimeoutSeconds, 5, maxTimeoutSeconds);
        if (request.TimeoutSeconds > maxTimeoutSeconds) logs.Add($"timeoutSeconds {request.TimeoutSeconds} clamped to {maxTimeoutSeconds}.");
        using var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancelSource.Token, timeoutSource.Token);

        var stopwatch = Stopwatch.StartNew();
        System.Text.Json.JsonElement? valueJson = null;
        var valueType = "null";
        var truncated = false;
        string? message = null;
        var timedOut = false;

        var savedUnits = sapModel.GetPresentUnits();
        var ret = sapModel.SetPresentUnits(ForcedUnits);
        if (ret != 0) return ExecuteResult.Failure($"ETABS returned {ret} from SetPresentUnits({ForcedUnits}); the run was not started.");

        try
        {
            var globals = new EtabsScriptGlobals(sapModel, etabs, Units, linked.Token,
                line =>
                {
                    if (logs.Count < _settings.MaxLogLines) logs.Add(line ?? string.Empty);
                    else droppedLogs++;
                },
                (current, total, text) => progress?.Report(new ScriptProgress(current, total, text)),
                new ScriptArgs(request.Args));

            // The guard rejects await, so the task is already complete when RunAsync returns.
            var value = script.RunAsync(globals, linked.Token).GetAwaiter().GetResult().ReturnValue;

            // A script that noticed the timeout and returned early still timed out.
            if (timeoutSource.IsCancellationRequested) throw new OperationCanceledException(timeoutSource.Token);
            if (cancelSource.IsCancellationRequested) throw new OperationCanceledException(cancelSource.Token);

            (valueJson, valueType, truncated) = _serializer.Serialize(value);
        }
        catch (OperationCanceledException)
        {
            timedOut = timeoutSource.IsCancellationRequested && !cancelSource.IsCancellationRequested;
            message = timedOut
                ? $"Script timed out after {timeoutSeconds}s (cooperative: only checked between OAPI calls). Raise timeoutSeconds (max {maxTimeoutSeconds}) or do less per call."
                : "Script was cancelled between OAPI calls.";
        }
        catch (Exception exception) when (EtabsAttachment.IsDisconnectError(exception))
        {
            // ETABS went away under the script: the executor drops the attachment and answers "not attached", not a script error.
            throw;
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "MCP script '{Label}' failed", request.Label ?? "script");
            message = SafeText.StripPaths($"{exception.GetType().Name}: {exception.Message}");
        }
        finally
        {
            RestoreUnits(sapModel, savedUnits, logs);
        }

        stopwatch.Stop();
        if (droppedLogs > 0) logs.Add($"… {droppedLogs} more log line(s) dropped (limit {_settings.MaxLogLines})");

        var result = new ExecuteResult
        {
            IsError = message is not null,
            Message = message,
            Logs = logs,
            Changed = new ChangedCounts(0, 0, 0),
            RolledBack = false,
            TimedOut = timedOut,
            DurationMs = stopwatch.ElapsedMilliseconds,
        };

        if (message is null)
        {
            result.Value = valueJson;
            result.ValueType = valueType;
            result.Truncated = truncated;
        }

        return result;
    }

    private static void RestoreUnits(cSapModel sapModel, eUnits saved, List<string> logs)
    {
        try
        {
            var ret = sapModel.SetPresentUnits(saved);
            if (ret != 0) logs.Add($"warning: ETABS returned {ret} restoring present units to {saved}; they may still be {ForcedUnits}.");
        }
        catch (Exception exception)
        {
            logs.Add($"warning: restoring present units to {saved} threw {exception.GetType().Name}; they may still be {ForcedUnits}.");
        }
    }
}
