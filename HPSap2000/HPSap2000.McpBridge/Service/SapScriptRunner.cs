using System.Diagnostics;
using System.IO;
using SAP2000v1;
using HPSap2000.McpBridge.Model;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Serilog;

namespace HPSap2000.McpBridge.Service;

/// <summary>
///     Runs one compiled script on the STA worker against the attached SAP2000 model, by tier.
/// </summary>
public sealed class SapScriptRunner
{
    private readonly BridgeSettings _settings;
    private readonly SapResultSerializer _serializer;
    private readonly SapSnapshotManager _snapshots;

    public SapScriptRunner(BridgeSettings settings, SapResultSerializer serializer, SapSnapshotManager snapshots)
    {
        _settings = settings;
        _serializer = serializer;
        _snapshots = snapshots;
    }

    public ExecuteResult Run(cOAPI sap, cSapModel sapModel, ExecuteRequest request, Script<object> script, TierVerdict verdict, int maxTimeoutSeconds,
        IProgress<ScriptProgress>? progress, CancellationTokenSource cancelSource, Action? beforeSave = null)
    {
        var logs = new List<string>();
        var droppedLogs = 0;
        var mode = TransactionModes.Normalize(request.Transaction) ?? TransactionModes.Auto;
        if (mode == TransactionModes.Manual) logs.Add("transaction=\"manual\" behaves like \"auto\" in SAP2000: there is no transaction to manage.");

        var timeoutSeconds = Math.Clamp(request.TimeoutSeconds, 5, maxTimeoutSeconds);
        if (request.TimeoutSeconds > maxTimeoutSeconds) logs.Add($"timeoutSeconds {request.TimeoutSeconds} clamped to {maxTimeoutSeconds}.");

        using var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancelSource.Token, timeoutSource.Token);
        var stopwatch = Stopwatch.StartNew();

        var writes = verdict.Tier >= SapTier.Write;
        var label = request.Label ?? "script";
        ExecuteResult? early = null;
        string? snapshot = null;
        System.Text.Json.JsonElement? valueJson = null;
        var valueType = "null";
        var truncated = false;
        string? message = null;
        var timedOut = false;
        var changed = new ChangedCounts(0, 0, 0);
        SapFingerprint? before = null;

        var unitsFailure = SapUnitsPolicy.Run(sapModel.GetPresentUnits, sapModel.SetPresentUnits, logs, () =>
        {
            if (writes)
            {
                var modelPath = SapContextReader.ModelFile(sapModel.GetModelFilename(true));
                SapSnapshotManager.EnsureSnapshotable(modelPath);

                var refusals = PathRefusals(verdict, request.Args, Path.GetDirectoryName(modelPath));
                if (refusals.Count > 0)
                {
                    early = new ExecuteResult { IsError = true, RolledBack = true, Message = "A file path the script would use is refused; nothing ran. See the diagnostics.", Diagnostics = refusals, Logs = logs };
                    return;
                }

                beforeSave?.Invoke();
                var outcome = _snapshots.Prepare(modelPath!, label, () => sapModel.File.Save(), linked.Token);
                if (outcome.PresaveFileName is not null) logs.Add($"presave snapshot {outcome.PresaveFileName} (the file on disk was not last written by this bridge).");
                if (!outcome.Succeeded)
                {
                    var cancelled = outcome.TimedOut && cancelSource.IsCancellationRequested;
                    early = new ExecuteResult
                    {
                        IsError = true,
                        RolledBack = true,
                        Message = cancelled ? "Cancelled while saving the model for the snapshot; the script did not run." : outcome.Failure,
                        TimedOut = outcome.TimedOut && !cancelled,
                        Logs = logs,
                        Snapshot = outcome.PresaveFileName,
                    };
                    return;
                }

                snapshot = outcome.FileName;
                logs.Add($"model saved and snapshot {snapshot} taken before the run ({stopwatch.ElapsedMilliseconds} ms).");
            }

            before = SapFingerprint.Take(sapModel);

            try
            {
                var globals = new SapScriptGlobals(sapModel, sap, SapUnitsPolicy.Units, linked.Token,
                    line =>
                    {
                        if (logs.Count < _settings.MaxLogLines) logs.Add(line ?? string.Empty);
                        else droppedLogs++;
                    },
                    (current, total, text) => progress?.Report(new ScriptProgress(current, total, text)),
                    new ScriptArgs(request.Args));

                var value = script.RunAsync(globals, linked.Token).GetAwaiter().GetResult().ReturnValue;

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
            catch (Exception exception) when (SapAttachment.IsDisconnectError(exception))
            {
                throw;
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "MCP script '{Label}' failed", label);
                message = SafeText.StripPaths($"{exception.GetType().Name}: {exception.Message}");
            }
            finally
            {
                if (before is not null) changed = DiffAfter(sapModel, before, writes, logs);
            }

            if (message is not null && writes)
                message += changed.Added + changed.Deleted > 0
                    ? $" Changes made before that persisted — SAP2000 has no rollback; snapshot {snapshot} holds the model as saved before the run."
                    : $" No additions or deletions were recorded (a Set* change would not be counted); snapshot {snapshot} holds the model as saved before the run.";
        });

        if (unitsFailure is not null) return new ExecuteResult { IsError = true, Message = unitsFailure, Logs = logs };
        if (early is not null) { early.DurationMs = stopwatch.ElapsedMilliseconds; return early; }

        stopwatch.Stop();
        if (droppedLogs > 0) logs.Add($"… {droppedLogs} more log line(s) dropped (limit {_settings.MaxLogLines})");

        var result = new ExecuteResult
        {
            IsError = message is not null,
            Message = message,
            Logs = logs,
            Changed = changed,
            RolledBack = false,
            TimedOut = timedOut,
            DurationMs = stopwatch.ElapsedMilliseconds,
            Snapshot = snapshot,
        };

        if (message is null)
        {
            result.Value = valueJson;
            result.ValueType = valueType;
            result.Truncated = truncated;
        }

        return result;
    }

    public static IReadOnlyList<ScriptDiagnostic> PathRefusals(TierVerdict verdict, System.Text.Json.JsonElement? args, string? modelDirectory)
    {
        if (!verdict.TakesPaths) return [];
        var refusals = new List<ScriptDiagnostic>();

        foreach (var literal in verdict.PathLiterals)
        {
            if (SapPathPolicy.RuntimeRefusal(literal, modelDirectory) is { } reason)
                refusals.Add(new ScriptDiagnostic(0, 0, SapTierAnalyzer.PathDiagnosticId, $"path \"{literal}\" refused — {reason}."));
        }

        var strings = SapPathPolicy.StringValues(args).ToArray();
        foreach (var key in verdict.PathArgKeys.Distinct(StringComparer.Ordinal))
        {
            if (!strings.Any(s => s.key == key))
                refusals.Add(new ScriptDiagnostic(0, 0, SapTierAnalyzer.PathDiagnosticId, $"args.{key} is read as a file path and must be given as a string."));
        }

        foreach (var (key, value) in strings)
        {
            var isPathKey = verdict.PathArgKeys.Contains(key, StringComparer.Ordinal);
            if (!isPathKey && !SapPathPolicy.LooksLikePath(value)) continue;
            if (SapPathPolicy.RuntimeRefusal(value, modelDirectory) is { } reason)
                refusals.Add(new ScriptDiagnostic(0, 0, SapTierAnalyzer.PathDiagnosticId, $"args.{key} = \"{value}\" refused — {reason}."));
        }

        return refusals;
    }

    private static ChangedCounts DiffAfter(cSapModel sapModel, SapFingerprint before, bool writes, List<string> logs)
    {
        try
        {
            var (changed, notes) = SapFingerprint.Diff(before, SapFingerprint.Take(sapModel));
            if (notes.Count > 0 && !writes) logs.Add("model changed during a read-only run (another writer?): " + string.Join("; ", notes));
            else if (notes.Count > 0) logs.Add("changed: " + string.Join("; ", notes));
            return changed;
        }
        catch (Exception exception) when (!SapAttachment.IsDisconnectError(exception))
        {
            logs.Add("warning: could not fingerprint the model after the run: " + exception.GetType().Name);
            return new ChangedCounts(0, 0, 0);
        }
    }
}
