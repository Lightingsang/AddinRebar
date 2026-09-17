using System.Diagnostics;
using System.IO;
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
///     Runs one compiled script on the STA worker against the attached model, by tier. The budget clock starts
///     first, then the present units are forced to kN / mm / °C; a writing or destructive script then has its
///     path values screened and the model saved and copied (<see cref="EtabsSnapshotManager"/>) inside that
///     budget; every tier takes a fingerprint before and after so <c>Changed</c> reports additions and deletions.
///     There is nothing to roll back — ETABS has no transaction — so an exception after a write leaves the
///     changes in place and the result says so, naming the snapshot. The user's units come back in <c>finally</c>.
/// </summary>
public sealed class EtabsScriptRunner
{
    private readonly BridgeSettings _settings;
    private readonly EtabsResultSerializer _serializer;
    private readonly EtabsSnapshotManager _snapshots;

    public EtabsScriptRunner(BridgeSettings settings, EtabsResultSerializer serializer, EtabsSnapshotManager snapshots)
    {
        _settings = settings;
        _serializer = serializer;
        _snapshots = snapshots;
    }

    /// <param name="verdict">The analyzer's verdict from the pipe thread; the executor already refused D without the opt-in and previews.</param>
    /// <param name="maxTimeoutSeconds">Ceiling captured on the pipe thread with the tier verdict.</param>
    /// <param name="cancelSource">Cancelled by cancel_execution; the runner adds the timeout on top.</param>
    /// <param name="beforeSave">Called once, right before the forced save touches the user's file (the executor writes the audit "started" line there).</param>
    public ExecuteResult Run(cOAPI etabs, cSapModel sapModel, ExecuteRequest request, Script<object> script, TierVerdict verdict, int maxTimeoutSeconds,
        IProgress<ScriptProgress>? progress, CancellationTokenSource cancelSource, Action? beforeSave = null)
    {
        var logs = new List<string>();
        var droppedLogs = 0;
        var mode = TransactionModes.Normalize(request.Transaction) ?? TransactionModes.Auto;
        if (mode == TransactionModes.Manual) logs.Add("transaction=\"manual\" behaves like \"auto\" in ETABS: there is no transaction to manage.");

        var timeoutSeconds = Math.Clamp(request.TimeoutSeconds, 5, maxTimeoutSeconds);
        if (request.TimeoutSeconds > maxTimeoutSeconds) logs.Add($"timeoutSeconds {request.TimeoutSeconds} clamped to {maxTimeoutSeconds}.");
        // The clock runs from here: the forced save and the snapshot copy count against the caller's budget.
        using var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancelSource.Token, timeoutSource.Token);
        var stopwatch = Stopwatch.StartNew();

        var writes = verdict.Tier >= EtabsTier.Write;
        var label = request.Label ?? "script";
        ExecuteResult? early = null;
        string? snapshot = null;
        System.Text.Json.JsonElement? valueJson = null;
        var valueType = "null";
        var truncated = false;
        string? message = null;
        var timedOut = false;
        var changed = new ChangedCounts(0, 0, 0);
        EtabsFingerprint? before = null;

        var unitsFailure = EtabsUnitsPolicy.Run(sapModel.GetPresentUnits, sapModel.SetPresentUnits, logs, () =>
        {
            if (writes)
            {
                // Preconditions throw the no-document code (environment, never a tool failure); the rest fail the run before the script.
                var modelPath = EtabsContextReader.ModelFile(sapModel.GetModelFilename(true));
                EtabsSnapshotManager.EnsureSnapshotable(modelPath);

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
                    // The manager only knows "the budget ended"; whether that was cancel_execution or the clock is the runner's to say.
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

            before = EtabsFingerprint.Take(sapModel);

            try
            {
                var globals = new EtabsScriptGlobals(sapModel, etabs, EtabsUnitsPolicy.Units, linked.Token,
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
                Log.Warning(exception, "MCP script '{Label}' failed", label);
                message = SafeText.StripPaths($"{exception.GetType().Name}: {exception.Message}");
            }
            finally
            {
                if (before is not null) changed = DiffAfter(sapModel, before, writes, logs);
            }

            if (message is not null && writes)
                message += $" Changes made before that persisted — ETABS has no rollback; snapshot {snapshot} holds the model as saved before the run.";
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

    /// <summary>
    ///     The run-time half of the path policy: every literal, every `args` key the script reads for a path (which
    ///     must be present as a string — a missing key would fall through to ETABS unscreened), plus any other `args`
    ///     string that is shaped like a path.
    /// </summary>
    public static IReadOnlyList<ScriptDiagnostic> PathRefusals(TierVerdict verdict, System.Text.Json.JsonElement? args, string? modelDirectory)
    {
        if (!verdict.TakesPaths) return [];
        var refusals = new List<ScriptDiagnostic>();

        foreach (var literal in verdict.PathLiterals)
        {
            if (EtabsPathPolicy.RuntimeRefusal(literal, modelDirectory) is { } reason)
                refusals.Add(new ScriptDiagnostic(0, 0, EtabsTierAnalyzer.PathDiagnosticId, $"path \"{literal}\" refused — {reason}."));
        }

        var strings = EtabsPathPolicy.StringValues(args).ToArray();
        foreach (var key in verdict.PathArgKeys.Distinct(StringComparer.Ordinal))
        {
            if (!strings.Any(s => s.key == key))
                refusals.Add(new ScriptDiagnostic(0, 0, EtabsTierAnalyzer.PathDiagnosticId, $"args.{key} is read as a file path and must be given as a string."));
        }

        foreach (var (key, value) in strings)
        {
            var isPathKey = verdict.PathArgKeys.Contains(key, StringComparer.Ordinal);
            if (!isPathKey && !EtabsPathPolicy.LooksLikePath(value)) continue;
            if (EtabsPathPolicy.RuntimeRefusal(value, modelDirectory) is { } reason)
                refusals.Add(new ScriptDiagnostic(0, 0, EtabsTierAnalyzer.PathDiagnosticId, $"args.{key} = \"{value}\" refused — {reason}."));
        }

        return refusals;
    }

    private static ChangedCounts DiffAfter(cSapModel sapModel, EtabsFingerprint before, bool writes, List<string> logs)
    {
        try
        {
            var (changed, notes) = EtabsFingerprint.Diff(before, EtabsFingerprint.Take(sapModel));
            if (notes.Count > 0 && !writes) logs.Add("model changed during a read-only run (another writer?): " + string.Join("; ", notes));
            else if (notes.Count > 0) logs.Add("changed: " + string.Join("; ", notes));
            return changed;
        }
        catch (Exception exception) when (!EtabsAttachment.IsDisconnectError(exception))
        {
            logs.Add("warning: could not fingerprint the model after the run: " + exception.GetType().Name);
            return new ChangedCounts(0, 0, 0);
        }
    }
}
