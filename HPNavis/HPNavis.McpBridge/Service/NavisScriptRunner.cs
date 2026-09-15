using System.Diagnostics;
using Autodesk.Navisworks.Api;
using HPNavis.McpBridge.Model;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Serilog;

namespace HPNavis.McpBridge.Service;

/// <summary>
///     Runs one compiled script on Navisworks' main thread inside the one transaction the bridge owns.
///     Navisworks cannot roll a transaction back while it is open — <c>Commit()</c> is mandatory — but
///     <c>Document.Rollback()</c> undoes the last <em>completed</em> transaction of the whole document.
///     So every run commits, and rolls back only when the top of the undo stack is the entry this run
///     just created: an empty transaction (read-only script, exception before the first edit) leaves the
///     user's own last edit alone. <c>none</c> runs are wrapped the same way and fail when the fingerprint
///     moved. Everything that can go wrong ends in a result with a message — never an exception out of here.
/// </summary>
public sealed class NavisScriptRunner
{
    private readonly BridgeSettings _settings;
    private readonly NavisResultSerializer _serializer;
    private readonly NavisApp _app;

    public NavisScriptRunner(BridgeSettings settings, NavisResultSerializer serializer, NavisApp app)
    {
        _settings = settings;
        _serializer = serializer;
        _app = app;
    }

    /// <param name="cancelSource">Cancelled by cancel_execution; the runner adds the timeout on top.</param>
    /// <param name="hasHeavyCalls">The pre-pass saw a heavy member: never claim a rollback and refuse dry runs.</param>
    /// <param name="maxTimeoutSeconds">Ceiling captured on the pipe thread with the heavy verdict, so a toggle of the heavy opt-in mid-request cannot move it.</param>
    public ExecuteResult Run(Document doc, ExecuteRequest request, Script<object> script, bool hasHeavyCalls, int maxTimeoutSeconds,
        IProgress<ScriptProgress>? progress, CancellationTokenSource cancelSource)
    {
        var mode = TransactionModes.Normalize(request.Transaction) ?? TransactionModes.Auto;

        if (doc.IsActiveTransaction)
            return ExecuteResult.Failure("Navisworks already has a transaction open (an operation is in progress). Finish it and retry.");
        if (hasHeavyCalls && request.DryRun)
            return ExecuteResult.Failure("dryRun cannot undo AppendFile/MergeFile/SaveFile/Export/TestsRunTest. Run with dryRun=false, or remove the heavy call.");

        var logs = new List<string>();
        var droppedLogs = 0;
        var units = UnitsOf(doc, logs);
        if (mode == TransactionModes.Manual) logs.Add("transaction=\"manual\" behaves like \"auto\" in Navisworks: the bridge owns the only transaction.");

        var timeoutSeconds = Math.Clamp(request.TimeoutSeconds, 5, maxTimeoutSeconds);
        using var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancelSource.Token, timeoutSource.Token);

        var stopwatch = Stopwatch.StartNew();
        var before = NavisChangeCounter.Snapshot(doc, NavisClashModule.TestCount);
        var label = NavisUndoDecision.LabelFor(request.Label, before.NextUndo);
        System.Text.Json.JsonElement? valueJson = null;
        var valueType = "null";
        var truncated = false;
        string? message = null;
        var rolledBack = false;
        var timedOut = false;
        Transaction? transaction = null;

        try
        {
            transaction = doc.BeginTransaction(label);

            var globals = new NavisScriptGlobals(doc, _app, units, linked.Token,
                line =>
                {
                    if (logs.Count < _settings.MaxLogLines) logs.Add(line ?? string.Empty);
                    else droppedLogs++;
                },
                (current, total, text) => progress?.Report(new ScriptProgress(current, total, text)),
                new ScriptArgs(request.Args));

            // The guard rejects await, so the task is already complete when RunAsync returns.
            var value = script.RunAsync(globals, linked.Token).GetAwaiter().GetResult().ReturnValue;

            // A script that noticed the timeout and returned early still timed out: nothing partial is kept.
            if (timeoutSource.IsCancellationRequested) throw new OperationCanceledException(timeoutSource.Token);
            if (cancelSource.IsCancellationRequested) throw new OperationCanceledException(cancelSource.Token);

            (valueJson, valueType, truncated) = _serializer.Serialize(value, units);
        }
        catch (OperationCanceledException)
        {
            timedOut = timeoutSource.IsCancellationRequested && !cancelSource.IsCancellationRequested;
            message = timedOut
                ? $"Script timed out after {timeoutSeconds}s (cooperative timeout). Raise timeoutSeconds (max {maxTimeoutSeconds}) or do less per call."
                : hasHeavyCalls
                    ? "Script was cancelled; the cancel arrived after a heavy call had already completed, so its effect persisted."
                    : "Script was cancelled.";
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "MCP script '{Label}' failed", label);
            message = SafeText.StripPaths($"{exception.GetType().Name}: {exception.Message}");
        }

        // Commit is mandatory even after a failure; the undo decision comes right after (rules in NavisUndoDecision).
        var committed = Commit(transaction, label);
        var after = NavisChangeCounter.Snapshot(doc, NavisClashModule.TestCount);
        var changed = NavisChangeCounter.Delta(before, after);
        var documentChanged = !before.SameAs(after);
        var undoIsOurs = NavisUndoDecision.IsOurs(committed, label, before.NextUndo, after.NextUndo);
        var reason = NavisUndoDecision.Classify(mode, request.DryRun, message is not null, undoIsOurs, documentChanged);

        if (NavisUndoDecision.ShouldRollBack(reason, undoIsOurs)) rolledBack = RollbackOwn(doc, label);
        if (hasHeavyCalls && rolledBack)
        {
            // File and clash-run effects are outside the undo stack: never let the AI read "rolledBack" for them.
            rolledBack = false;
            logs.Add(NavisUndoDecision.HeavyNotUndoable);
        }

        switch (reason)
        {
            case UndoReason.NoneViolation:
                message = NavisUndoDecision.NoneViolationMessage(rolledBack);
                break;
            case UndoReason.DryRun when !undoIsOurs && documentChanged:
                message = NavisUndoDecision.DryRunChangePersisted;
                break;
            case UndoReason.DryRun when !undoIsOurs:
                logs.Add(NavisUndoDecision.DryRunNothingToUndo);
                break;
            case UndoReason.Failure when !undoIsOurs && documentChanged:
                message += NavisUndoDecision.FailureNotUndone;
                break;
        }

        stopwatch.Stop();
        if (droppedLogs > 0) logs.Add($"… {droppedLogs} more log line(s) dropped (limit {_settings.MaxLogLines})");

        var result = new ExecuteResult
        {
            IsError = message is not null,
            Message = message,
            Logs = logs,
            Changed = rolledBack ? new ChangedCounts(0, 0, 0) : changed,
            RolledBack = rolledBack,
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

    private static bool Commit(Transaction? transaction, string label)
    {
        if (transaction is null) return false;

        try
        {
            if (!transaction.IsCommitted) transaction.Commit();
            return true;
        }
        catch (Exception exception)
        {
            Log.Error(exception, "MCP script '{Label}': Commit() failed", label);
            return false;
        }
        finally
        {
            transaction.Dispose();
        }
    }

    /// <summary>Undoes the entry this run left on top of the stack; false (and a log line) if Navisworks refuses.</summary>
    private static bool RollbackOwn(Document doc, string label)
    {
        try
        {
            doc.Rollback();
            return true;
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "MCP script '{Label}': Rollback() refused", label);
            return false;
        }
    }

    /// <summary>Every API length is in <c>Document.Units</c>; models appended in other units are already scaled by Navisworks.</summary>
    public static ScriptUnits UnitsOf(Document doc, List<string>? logs = null)
    {
        Units documentUnits;
        try { documentUnits = doc.Units; }
        catch { return ScriptUnits.Millimeters; }

        var mmPerUnit = UnitConversion.ScaleFactor(documentUnits, Units.Millimeters);
        string? note = null;
        try
        {
            var foreign = doc.Models.Count(m => m.Units != documentUnits);
            if (foreign > 0) note = $"document units = {documentUnits}; {foreign} of {doc.Models.Count} models declare other units (already scaled by Navisworks).";
        }
        catch
        {
            // model enumeration is best effort
        }

        if (note is not null) logs?.Add(note);
        return new ScriptUnits(documentUnits.ToString(), mmPerUnit, note);
    }
}
