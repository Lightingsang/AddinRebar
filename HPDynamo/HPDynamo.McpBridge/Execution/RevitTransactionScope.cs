namespace HPDynamo.McpBridge.Execution;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Runtime.CompilerServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HPDynamo.McpBridge.Support;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using Serilog;

/// <summary>
///     Manages Revit transaction boundaries with atomic TransactionGroup rollback for dryRun operations.
/// </summary>
public static class RevitTransactionScope
{
    public delegate object? ExecutionAction(
        object document,
        CancellationToken cancellationToken,
        Action<string?> logger);

    public static ExecuteResult Execute(
        object? uiapp,
        ExecuteRequest request,
        BridgeSettings settings,
        IProgress<ScriptProgress>? progress,
        CancellationTokenSource cancelSource,
        ExecutionAction action,
        bool startAutoTransaction = true)
    {
        if (uiapp is null)
            return ExecuteResult.Failure("No document is open in Revit. Open a project first.");

        return ExecuteWithUiApp(uiapp, request, settings, progress, cancelSource, action, startAutoTransaction);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ExecuteResult ExecuteWithUiApp(
        object uiapp,
        ExecuteRequest request,
        BridgeSettings settings,
        IProgress<ScriptProgress>? progress,
        CancellationTokenSource cancelSource,
        ExecutionAction action,
        bool startAutoTransaction = true)
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
            txContext,
            request,
            settings,
            progress,
            cancelSource,
            (ct, log) => action(doc, ct, log),
            doc.IsModifiable,
            counter,
            startAutoTransaction);
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
        bool startAutoTransaction = true)
    {
        var mode = TransactionModes.Normalize(request.Transaction) ?? TransactionModes.Auto;
        var validationFailure = ValidateState(mode, isDocReadOnly, isDocFamily, isDocModifiable, settings.AllowFamilyDocuments);
        if (validationFailure != null) return validationFailure;

        return ExecuteCore(
            txContext,
            request,
            settings,
            progress,
            cancelSource,
            action,
            isDocModifiable,
            null,
            startAutoTransaction);
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
        bool startAutoTransaction = true)
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
            BeginTransactions(txContext, mode, label, startAutoTransaction);
            object? returnValue = null;
            string? errorMessage = null;
            var rolledBack = false;
            var timedOut = false;

            try
            {
                returnValue = action(linked.Token, Logger);

                if (linked.Token.IsCancellationRequested)
                    throw new OperationCanceledException(linked.Token);

                rolledBack = FinalizeTransactions(txContext, request, mode, isDocModifiable, Logger);
            }
            catch (OperationCanceledException)
            {
                timedOut = timeoutSource.IsCancellationRequested && !cancelSource.IsCancellationRequested;
                errorMessage = timedOut
                    ? $"Execution timed out after {request.TimeoutSeconds}s. Nothing was committed."
                    : "Execution was cancelled. Nothing was committed.";
                rolledBack = SafeRollback(txContext);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Execution failed in RevitTransactionScope for label '{Label}'", label);
                errorMessage = SafeText.StripPaths($"{ex.GetType().Name}: {ex.Message}");
                rolledBack = SafeRollback(txContext);
            }

            stopwatch.Stop();
            if (droppedLogs > 0)
                logs.Add($"... {droppedLogs} more log lines dropped (limit {settings.MaxLogLines})");

            var jsonValue = SerializeValue(returnValue, out var valType);
            return new ExecuteResult
            {
                IsError = errorMessage is not null,
                Message = errorMessage,
                Value = jsonValue,
                ValueType = valType,
                Warnings = txContext.Warnings,
                Logs = logs,
                Changed = counter?.Counts ?? new ChangedCounts(0, 0, 0),
                RolledBack = rolledBack,
                TimedOut = timedOut,
                DurationMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    internal static JsonElement? SerializeValue(object? value, out string? valueType)
    {
        if (value is null)
        {
            valueType = null;
            return null;
        }

        valueType = value.GetType().Name;
        if (value is JsonElement je) return je;

        try
        {
            return JsonSerializer.SerializeToElement(value);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to serialize return value as JSON, falling back to string representation");
            return JsonSerializer.SerializeToElement(value.ToString());
        }
    }

    internal static ExecuteResult? ValidateState(string mode, bool isReadOnly, bool isFamily, bool isModifiable, bool allowFamily)
    {
        if (mode == TransactionModes.None) return null;

        if (isReadOnly)
            return ExecuteResult.Failure("The active document is read-only; only transaction='none' operations are permitted.");

        if (isFamily && !allowFamily)
            return ExecuteResult.Failure("The active document is a family; modifying families is disabled in bridge settings.");

        if (isModifiable)
            return ExecuteResult.Failure("Revit already has a transaction open. Complete or cancel it before running.");

        return null;
    }

    private static void BeginTransactions(ITransactionContext txContext, string mode, string label, bool startAutoTransaction = true)
    {
        if (mode != TransactionModes.None)
        {
            txContext.StartGroup("MCP Dynamo: " + label);
        }

        if (mode == TransactionModes.Auto && startAutoTransaction)
        {
            txContext.StartAutoTransaction("MCP Dynamo auto-step");
        }
    }

    private static bool FinalizeTransactions(
        ITransactionContext txContext,
        ExecuteRequest request,
        string mode,
        bool isDocModifiable,
        Action<string?> logger)
    {
        if (mode == TransactionModes.Manual && isDocModifiable)
            throw new InvalidOperationException("The script left a Transaction open. Commit or roll it back before returning.");

        if (mode == TransactionModes.Auto && txContext.IsAutoTransactionStarted && !txContext.CommitAutoTransaction())
            throw new InvalidOperationException("Revit rejected the change (a transaction failure was posted).");

        if (request.DryRun)
        {
            txContext.RollbackGroup();
            logger("dryRun=true: All Revit document modifications rolled back cleanly.");
            return true;
        }

        txContext.AssimilateGroup();
        return false;
    }

    private static bool SafeRollback(ITransactionContext txContext)
    {
        var autoSuccess = false;
        var groupSuccess = false;
        try
        {
            txContext.RollbackAutoTransaction();
            autoSuccess = true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception encountered rolling back auto transaction");
        }

        try
        {
            txContext.RollbackGroup();
            groupSuccess = true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception encountered rolling back transaction group");
        }

        return autoSuccess && groupSuccess;
    }
}
