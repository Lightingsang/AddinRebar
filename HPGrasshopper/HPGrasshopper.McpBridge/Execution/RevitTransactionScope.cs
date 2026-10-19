namespace HPGrasshopper.McpBridge.Execution;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HPGrasshopper.McpBridge.Support;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using Serilog;

/// <summary>
///     Manages Revit transaction boundaries with atomic TransactionGroup rollback for dryRun operations.
/// </summary>
public static class RevitTransactionScope
{
    public delegate object? ExecutionAction(object document, CancellationToken cancellationToken, Action<string?> logger);

    public static ExecuteResult Execute(
        object? uiapp, ExecuteRequest request, BridgeSettings settings,
        IProgress<ScriptProgress>? progress, CancellationTokenSource cancelSource,
        ExecutionAction action, bool startAutoTransaction = true,
        ICanvasUndoScope? canvasScope = null) =>
        uiapp is null
            ? ExecuteResult.Failure("No document is open in Revit. Open a project first.")
            : ExecuteWithUiApp(uiapp, request, settings, progress, cancelSource, action, startAutoTransaction, canvasScope);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ExecuteResult ExecuteWithUiApp(
        object uiapp, ExecuteRequest request, BridgeSettings settings,
        IProgress<ScriptProgress>? progress, CancellationTokenSource cancelSource,
        ExecutionAction action, bool startAutoTransaction = true,
        ICanvasUndoScope? canvasScope = null)
    {
        var app = (UIApplication)uiapp;
        var uidoc = app.ActiveUIDocument;
        var doc = uidoc?.Document;
        if (doc is null || uidoc is null)
            return ExecuteResult.Failure("No document is open in Revit. Open a project first.");

        var mode = TransactionModes.Normalize(request.Transaction) ?? TransactionModes.Auto;
        var validationFailure = ValidateState(mode, doc.IsReadOnly, doc.IsFamilyDocument, doc.IsModifiable, settings.AllowFamilyDocuments);
        if (validationFailure != null) return validationFailure;

        using var txContext = new RevitTransactionContext(doc);
        var counter = app.Application != null ? DocumentChangeCounter.Begin(app.Application) : null;
        return ExecuteCore(
            txContext, request, settings, progress, cancelSource,
            (ct, log) => action(doc, ct, log), doc.IsModifiable, counter, startAutoTransaction, canvasScope);
    }

    internal static ExecuteResult ExecuteWithContext(
        ITransactionContext txContext,
        ExecuteRequest request,
        BridgeSettings settings,
        IProgress<ScriptProgress>? progress,
        CancellationTokenSource cancelSource,
        Func<CancellationToken, Action<string?>, object?> action,
        bool isDocReadOnly = false,
        bool isDocFamily = false,
        bool isDocModifiable = false,
        bool startAutoTransaction = true,
        ICanvasUndoScope? canvasScope = null)
    {
        var mode = TransactionModes.Normalize(request.Transaction) ?? TransactionModes.Auto;
        var validationFailure = ValidateState(mode, isDocReadOnly, isDocFamily, isDocModifiable, settings.AllowFamilyDocuments);
        if (validationFailure != null) return validationFailure;

        return ExecuteCore(
            txContext, request, settings, progress, cancelSource,
            action, isDocModifiable, null, startAutoTransaction, canvasScope);
    }

    private static ExecuteResult ExecuteCore(
        ITransactionContext txContext,
        ExecuteRequest request,
        BridgeSettings settings,
        IProgress<ScriptProgress>? progress,
        CancellationTokenSource cancelSource,
        Func<CancellationToken, Action<string?>, object?> action,
        bool isDocModifiable,
        DocumentChangeCounter? counter,
        bool startAutoTransaction = true,
        ICanvasUndoScope? canvasScope = null)
    {
        using (counter)
        {
            var logs = new List<string>();
            var droppedLogs = 0;
            var label = string.IsNullOrWhiteSpace(request.Label) ? "script" : request.Label.Trim();
            var mode = TransactionModes.Normalize(request.Transaction) ?? TransactionModes.Auto;

            using var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Clamp(request.TimeoutSeconds, 5, 600)));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancelSource.Token, timeoutSource.Token);

            void Logger(string? line)
            {
                if (logs.Count < settings.MaxLogLines) logs.Add(line ?? string.Empty);
                else droppedLogs++;
            }

            var stopwatch = Stopwatch.StartNew();
            var outcome = RunActionWithTransactions(
                txContext, request, mode, label, isDocModifiable, startAutoTransaction,
                action, linked.Token, cancelSource, timeoutSource, Logger, canvasScope);
            stopwatch.Stop();

            if (droppedLogs > 0)
                logs.Add($"... {droppedLogs} more log lines dropped (limit {settings.MaxLogLines})");

            var changesCount = counter != null ? counter.Counts.Added + counter.Counts.Modified + counter.Counts.Deleted : 0;
            if (outcome.Error is null && mode == TransactionModes.None && changesCount > 0)
            {
                outcome = new ExecutionOutcome(
                    null,
                    $"Document modifications detected under transaction='none' ({changesCount} changes). Modifications are forbidden under mode 'none'.",
                    false,
                    false);
            }

            return BuildExecuteResult(outcome, txContext, logs, counter, stopwatch.ElapsedMilliseconds);
        }
    }

    private static ExecutionOutcome RunActionWithTransactions(
        ITransactionContext txContext, ExecuteRequest request, string mode, string label,
        bool isDocModifiable, bool startAutoTransaction, Func<CancellationToken, Action<string?>, object?> action,
        CancellationToken linkedToken, CancellationTokenSource cancelSource,
        CancellationTokenSource timeoutSource, Action<string?> logger, ICanvasUndoScope? canvasScope)
    {
        BeginTransactions(txContext, mode, label, startAutoTransaction);
        try
        {
            var value = action(linkedToken, logger);
            if (linkedToken.IsCancellationRequested)
                throw new OperationCanceledException(linkedToken);

            var rolledBack = FinalizeTransactions(txContext, request, mode, isDocModifiable, logger);
            var canvasRollback = FinalizeCanvas(request.DryRun, canvasScope, logger);

            if (request.DryRun && canvasRollback is { Success: false })
            {
                var error = $"Canvas rollback failed with {canvasRollback.Errors.Count} error(s): {string.Join("; ", canvasRollback.Errors)}";
                return new ExecutionOutcome(value, error, false, false, canvasRollback);
            }

            return new ExecutionOutcome(value, null, rolledBack, false, canvasRollback);
        }
        catch (OperationCanceledException)
        {
            var timedOut = timeoutSource.IsCancellationRequested && !cancelSource.IsCancellationRequested;
            var baseError = timedOut
                ? $"Execution timed out after {request.TimeoutSeconds}s. Nothing was committed."
                : "Execution was cancelled. Nothing was committed.";
            return CreateAbortedOutcome(txContext, canvasScope, baseError, timedOut, logger);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Execution failed in RevitTransactionScope for label '{Label}'", label);
            var baseError = SafeText.StripPaths($"{ex.GetType().Name}: {ex.Message}");
            return CreateAbortedOutcome(txContext, canvasScope, baseError, false, logger);
        }
    }

    private static CanvasRollbackResult? FinalizeCanvas(bool dryRun, ICanvasUndoScope? canvasScope, Action<string?> logger)
    {
        if (!dryRun) { canvasScope?.Commit(); return null; }
        var result = SafeRollbackCanvas(canvasScope);
        if (result.Success) logger("dryRun=true: Canvas modifications rolled back cleanly.");
        else logger($"dryRun=true: Canvas rollback failed with {result.Errors.Count} error(s): {string.Join("; ", result.Errors)}");
        return result;
    }

    private static CanvasRollbackResult SafeRollbackCanvas(ICanvasUndoScope? canvasScope)
    {
        if (canvasScope == null) return CanvasRollbackResult.Clean();
        try { return canvasScope.Rollback(); }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception encountered rolling back canvas undo scope");
            return CanvasRollbackResult.Failed(new[] { $"Canvas rollback threw {ex.GetType().Name}: {ex.Message}" });
        }
    }

    private static ExecutionOutcome CreateAbortedOutcome(
        ITransactionContext txContext, ICanvasUndoScope? canvasScope, string baseError, bool timedOut, Action<string?> logger)
    {
        var canvasRollback = SafeRollbackCanvas(canvasScope);
        var revitRolledBack = SafeRollback(txContext);
        var combinedError = baseError;
        if (canvasRollback is { Success: false })
        {
            combinedError += $" Additionally, canvas rollback failed: {string.Join("; ", canvasRollback.Errors)}";
            logger($"Canvas rollback failed: {string.Join("; ", canvasRollback.Errors)}");
        }
        if (!revitRolledBack) combinedError += " Additionally, Revit transaction rollback failed.";
        bool cleanlyRolledBack = revitRolledBack && (canvasRollback == null || canvasRollback.Success);
        return new ExecutionOutcome(null, combinedError, cleanlyRolledBack, timedOut, canvasRollback);
    }

    private static ExecuteResult BuildExecuteResult(
        ExecutionOutcome outcome, ITransactionContext txContext, List<string> logs, DocumentChangeCounter? counter, long durationMs) =>
        new()
        {
            IsError = outcome.Error is not null,
            Message = outcome.Error,
            Value = SerializeValue(outcome.Value, out var valType),
            ValueType = valType,
            Warnings = txContext.Warnings,
            Logs = logs,
            Changed = counter?.Counts ?? new ChangedCounts(0, 0, 0),
            RolledBack = outcome.RolledBack,
            TimedOut = outcome.TimedOut,
            DurationMs = durationMs
        };

    internal static JsonElement? SerializeValue(object? value, out string? valueType)
    {
        if (value is null) { valueType = null; return null; }
        valueType = value.GetType().Name;
        if (value is JsonElement je) return je;

        try { return JsonSerializer.SerializeToElement(value); }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to serialize return value as JSON, falling back to string representation");
            return JsonSerializer.SerializeToElement(value.ToString());
        }
    }

    internal static ExecuteResult? ValidateState(string mode, bool isReadOnly, bool isFamily, bool isModifiable, bool allowFamily)
    {
        if (mode == TransactionModes.None) return null;
        if (isReadOnly) return ExecuteResult.Failure("The active document is read-only; only transaction='none' operations are permitted.");
        if (isFamily && !allowFamily) return ExecuteResult.Failure("The active document is a family; modifying families is disabled in bridge settings.");
        if (isModifiable) return ExecuteResult.Failure("Revit already has a transaction open. Complete or cancel it before running.");
        return null;
    }

    private static void BeginTransactions(ITransactionContext txContext, string mode, string label, bool startAutoTransaction = true)
    {
        if (mode != TransactionModes.None) txContext.StartGroup("MCP Grasshopper: " + label);
        if (mode == TransactionModes.Auto && startAutoTransaction) txContext.StartAutoTransaction("MCP Grasshopper auto-step");
    }

    private static bool FinalizeTransactions(
        ITransactionContext txContext,
        ExecuteRequest request,
        string mode,
        bool isDocModifiable,
        Action<string?> logger)
    {
        if (mode == TransactionModes.None)
        {
            if (isDocModifiable) throw new InvalidOperationException("The script modified the document or left a transaction open under transaction='none'.");
            if (!request.DryRun) txContext.AssimilateGroup();
            return false;
        }

        if (mode == TransactionModes.Manual && isDocModifiable)
            throw new InvalidOperationException("The script left a Transaction open. Commit or roll it back before returning.");

        if (mode == TransactionModes.Auto && txContext.IsAutoTransactionStarted && !txContext.CommitAutoTransaction())
            throw new InvalidOperationException("Revit rejected the change (a transaction failure was posted).");

        if (request.DryRun)
        {
            if (txContext.IsGroupStarted) { txContext.RollbackGroup(); logger("dryRun=true: All Revit document modifications rolled back cleanly."); return true; }
            return false;
        }

        txContext.AssimilateGroup();
        return false;
    }

    private static bool SafeRollback(ITransactionContext txContext)
    {
        if (!txContext.IsAutoTransactionStarted && !txContext.IsGroupStarted) return false;
        var autoSuccess = !txContext.IsAutoTransactionStarted || TryRollbackAction(txContext.RollbackAutoTransaction, "auto transaction");
        var groupSuccess = !txContext.IsGroupStarted || TryRollbackAction(txContext.RollbackGroup, "transaction group");
        return autoSuccess && groupSuccess;
    }

    private static bool TryRollbackAction(Action action, string name)
    {
        try { action(); return true; }
        catch (Exception ex) { Log.Error(ex, "Exception encountered rolling back {Name}", name); return false; }
    }
}
