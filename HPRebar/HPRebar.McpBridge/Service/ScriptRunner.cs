using System.Diagnostics;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Scripting;
using HPRebar.McpBridge.Model;
using Microsoft.CodeAnalysis.Scripting;
using Serilog;

namespace HPRebar.McpBridge.Service;

/// <summary>
///     Runs one compiled script on Revit's API thread inside the transaction policy the request asked for.
///     Everything that can go wrong ends in a rolled-back group and an <see cref="ExecuteResult"/> with a
///     message — never an exception out of here, never a Revit dialog.
/// </summary>
public sealed class ScriptRunner
{
    private readonly BridgeSettings _settings;
    private readonly ResultSerializer _serializer;

    public ScriptRunner(BridgeSettings settings, ResultSerializer serializer)
    {
        _settings = settings;
        _serializer = serializer;
    }

    /// <param name="cancelSource">Cancelled by cancel_execution; the runner adds the timeout on top.</param>
    public ExecuteResult Run(UIApplication uiapp, ExecuteRequest request, Script<object> script,
        IProgress<ScriptProgress>? progress, CancellationTokenSource cancelSource)
    {
        var uidoc = uiapp.ActiveUIDocument;
        var doc = uidoc?.Document;
        if (doc is null || uidoc is null) return ExecuteResult.Failure("No document is open in Revit. Open a project first.");

        var mode = TransactionModes.Normalize(request.Transaction) ?? TransactionModes.Auto;
        if (mode != TransactionModes.None)
        {
            if (doc.IsReadOnly) return ExecuteResult.Failure("The active document is read-only; only transaction=\"none\" scripts can run.");
            if (doc.IsFamilyDocument && !_settings.AllowFamilyDocuments) return ExecuteResult.Failure("The active document is a family; modifying families is disabled in the bridge settings.");
            if (doc.IsModifiable) return ExecuteResult.Failure("Revit already has a transaction open (a command is in progress). Finish it and retry.");
        }

        var logs = new List<string>();
        var droppedLogs = 0;
        var label = string.IsNullOrWhiteSpace(request.Label) ? "script" : request.Label!;

        using var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Clamp(request.TimeoutSeconds, 5, 120)));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancelSource.Token, timeoutSource.Token);

        var globals = new ScriptGlobals(doc, uidoc, uiapp.Application, uiapp, linked.Token,
            line =>
            {
                if (logs.Count < _settings.MaxLogLines) logs.Add(line ?? string.Empty);
                else droppedLogs++;
            },
            (current, total, message) => progress?.Report(new ScriptProgress(current, total, message)),
            new ScriptArgs(request.Args));

        using var counter = DocumentChangeCounter.Begin(uiapp.Application);
        var stopwatch = Stopwatch.StartNew();

        TransactionGroup? group = null;
        Transaction? transaction = null;
        object? value = null;
        string? message = null;
        var rolledBack = false;
        var timedOut = false;

        try
        {
            if (mode != TransactionModes.None)
            {
                group = new TransactionGroup(doc, "MCP: " + label);
                group.Start();
            }

            if (mode == TransactionModes.Auto)
            {
                transaction = new Transaction(doc, "MCP script");
                AutoDismissFailurePreprocessor.ApplyTo(transaction);
                transaction.Start();
            }

            // The guard rejects await, so the task is already complete when RunAsync returns.
            value = script.RunAsync(globals, linked.Token).GetAwaiter().GetResult().ReturnValue;

            // A script that noticed the timeout and returned early still timed out: partial work is never
            // committed, and the AI is told to raise timeoutSeconds instead of trusting a truncated result.
            if (timeoutSource.IsCancellationRequested) throw new OperationCanceledException(timeoutSource.Token);

            if (mode == TransactionModes.Manual && doc.IsModifiable)
                throw new InvalidOperationException("The script left a Transaction open. Commit or roll it back before returning.");

            if (transaction is not null && transaction.Commit() != TransactionStatus.Committed)
                throw new InvalidOperationException("Revit rejected the change (a failure was posted during commit).");

            if (group is not null)
            {
                if (request.DryRun)
                {
                    group.RollBack();
                    rolledBack = true;
                }
                else
                {
                    group.Assimilate();
                }
            }
        }
        catch (OperationCanceledException)
        {
            timedOut = timeoutSource.IsCancellationRequested && !cancelSource.IsCancellationRequested;
            message = timedOut
                ? $"Script timed out after {request.TimeoutSeconds}s (cooperative timeout). Nothing was committed; raise timeoutSeconds (max 120) or do less per call."
                : "Script was cancelled. Nothing was committed.";
            rolledBack = RollBack(transaction, group);
        }
        catch (Autodesk.Revit.Exceptions.ModificationOutsideTransactionException)
        {
            message = "The script modified the model without a transaction. Use transaction=\"auto\" (or open one yourself with transaction=\"manual\").";
            rolledBack = RollBack(transaction, group);
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "MCP script '{Label}' failed", label);
            message = SafeText.StripPaths($"{exception.GetType().Name}: {exception.Message}");
            rolledBack = RollBack(transaction, group);
        }
        finally
        {
            transaction?.Dispose();
            group?.Dispose();
        }

        stopwatch.Stop();
        if (droppedLogs > 0) logs.Add($"… {droppedLogs} more log line(s) dropped (limit {_settings.MaxLogLines})");

        var result = new ExecuteResult
        {
            IsError = message is not null,
            Message = message,
            Logs = logs,
            Changed = counter.Counts,
            RolledBack = rolledBack,
            TimedOut = timedOut,
            DurationMs = stopwatch.ElapsedMilliseconds,
        };

        if (message is null)
        {
            var (json, typeName, truncated) = _serializer.Serialize(value);
            result.Value = json;
            result.ValueType = typeName;
            result.Truncated = truncated;
        }

        return result;
    }

    /// <summary>
    ///     Best-effort unwind. Returns true only when the group's changes are actually gone: either its
    ///     RollBack succeeded, or it had already ended before we got here. A rollback Revit refuses is
    ///     logged and reported as false so the AI never reads "rolledBack" for a model that still changed.
    /// </summary>
    private static bool RollBack(Transaction? transaction, TransactionGroup? group)
    {
        try
        {
            if (transaction is not null && transaction.HasStarted() && !transaction.HasEnded()) transaction.RollBack();
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "MCP script transaction rollback failed");
        }

        if (group is null) return false;

        try
        {
            if (group.HasStarted() && !group.HasEnded()) group.RollBack();
            return true;
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "MCP script transaction group rollback failed");
            return false;
        }
    }
}
