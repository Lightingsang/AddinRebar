using System.Diagnostics;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using HPCivil3d.McpBridge.Model;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Serilog;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using AcadException = Autodesk.AutoCAD.Runtime.Exception;
using DocumentTransactionManager = Autodesk.AutoCAD.ApplicationServices.TransactionManager;

namespace HPCivil3d.McpBridge.Service;

/// <summary>
///     Runs one compiled script on AutoCAD's main thread inside the transaction policy the request asked
///     for. AutoCAD has no transaction group, but aborting the outermost transaction discards everything
///     inside it, nested commits included — so the bridge opens two: an outer one that plays the group
///     (commit = keep, abort = dry run / failure) and an inner one the script sees as `tr`. The inner one
///     is committed by the bridge as soon as the script returns, because the database's object events
///     only fire when a transaction commits — that is what makes <see cref="ExecuteResult.Changed"/>
///     countable before the outer decision. The script never commits or aborts `tr` (the guard rejects
///     that). Everything that can go wrong ends in an aborted outer transaction and a result with a
///     message — never an exception out of here.
/// </summary>
public sealed class Civil3dScriptRunner
{
    /// <summary>Name the document lock registers for the run; it is what UNDO lists and what U reverts.</summary>
    private const string UndoCommandName = "HPMCP";

    private readonly BridgeSettings _settings;
    private readonly Civil3dResultSerializer _serializer;

    public Civil3dScriptRunner(BridgeSettings settings, Civil3dResultSerializer serializer)
    {
        _settings = settings;
        _serializer = serializer;
    }

    /// <param name="cancelSource">Cancelled by cancel_execution; the runner adds the timeout on top.</param>
    public ExecuteResult Run(Document doc, ExecuteRequest request, Script<object> script,
        IProgress<ScriptProgress>? progress, CancellationTokenSource cancelSource)
    {
        var db = doc.Database;
        // The document's transaction manager, not the database's: only its transactions join the drawing's
        // undo stack and refresh the graphics when they commit.
        var manager = doc.TransactionManager;
        var mode = TransactionModes.Normalize(request.Transaction) ?? TransactionModes.Auto;
        if (mode != TransactionModes.None && doc.IsReadOnly)
            return ExecuteResult.Failure("The active drawing is read-only; only transaction=\"none\" scripts can run.");
        if (manager.NumberOfActiveTransactions > 0)
            return ExecuteResult.Failure("AutoCAD already has a transaction open (a command is in progress). Finish it and retry.");

        var logs = new List<string>();
        var droppedLogs = 0;
        var label = string.IsNullOrWhiteSpace(request.Label) ? "script" : request.Label!;
        var units = AutocadInsunits.For((int)db.Insunits);
        // civil-only: begin
        var civil = Civil3dDocumentAccess.TryGetActive();
        units = Civil3dUnits.For(civil, db, out _, out _);
        if (civil is null) logs.Add("No Civil 3D document in the active drawing: `civil` is null; units follow INSUNITS.");
        // civil-only: end
        if (units.Note is not null) logs.Add(units.Note);
        // The guard refuses StartTransaction in AutoCAD scripts (a leaked Transaction wrapper is finalised later and
        // takes acad.exe down), so "manual" cannot mean "the script opens its own": it runs exactly like auto.
        if (mode == TransactionModes.Manual) logs.Add("transaction=\"manual\" behaves like \"auto\" in Civil 3D: `tr` is the only transaction a script gets.");

        using var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Clamp(request.TimeoutSeconds, 5, 120)));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancelSource.Token, timeoutSource.Token);

        // Application context: the document must be locked before anything in it is opened for write. The
        // command name on the lock is what the undo stack shows, so one U reverts the whole run.
        using var documentLock = doc.LockDocument(DocumentLockMode.ProtectedAutoWrite, UndoCommandName, UndoCommandName, false);
        using var counter = DatabaseChangeCounter.Begin(db);
        var stopwatch = Stopwatch.StartNew();
        var changed = new ChangedCounts(0, 0, 0);

        Transaction? outer = null;
        Transaction? inner = null;
        var open = false; // the outer transaction still decides
        var innerOpen = false;
        System.Text.Json.JsonElement? valueJson = null;
        var valueType = "null";
        var truncated = false;
        string? message = null;
        var rolledBack = false;
        var timedOut = false;

        try
        {
            // Both wrappers are created inside the try so the finally always disposes them: a Transaction
            // wrapper left to the finaliser takes AutoCAD down.
            outer = manager.StartTransaction();
            open = true;
            inner = manager.StartTransaction();
            innerOpen = true;

            var globals = new Civil3dScriptGlobals(doc, db, doc.Editor, AcadApp.DocumentManager, inner, civil, units, linked.Token,
                line =>
                {
                    if (logs.Count < _settings.MaxLogLines) logs.Add(line ?? string.Empty);
                    else droppedLogs++;
                },
                (current, total, text) => progress?.Report(new ScriptProgress(current, total, text)),
                new ScriptArgs(request.Args));

            // The guard rejects await, so the task is already complete when RunAsync returns.
            var value = script.RunAsync(globals, linked.Token).GetAwaiter().GetResult().ReturnValue;

            // A script that noticed the timeout and returned early still timed out: partial work is never
            // committed, and the AI is told to raise timeoutSeconds instead of trusting a truncated result.
            if (timeoutSource.IsCancellationRequested) throw new OperationCanceledException(timeoutSource.Token);

            // Objects the script fetched through `tr` are disposed when the transaction ends — summarise them now.
            (valueJson, valueType, truncated) = _serializer.Serialize(value);

            if (manager.NumberOfActiveTransactions > 2)
            {
                // Cannot happen past the guard; if it ever does, the leaked wrapper will crash AutoCAD at the next GC, so say so loudly.
                Log.Error("MCP script '{Label}' left {Count} transactions open", label, manager.NumberOfActiveTransactions);
                AbortNested(manager);
                throw new InvalidOperationException("The script left a transaction open. Use the bridge's `tr` instead of starting transactions.");
            }

            if (manager.NumberOfActiveTransactions < 2)
            {
                // `var t = tr; t.Commit();` slips past the receiver-scoped guard: `tr` is already ended, never touch it again.
                innerOpen = false;
                throw new InvalidOperationException("The script committed or aborted `tr` itself. The bridge owns `tr`; leave it open and return.");
            }

            changed = counter.Counts;
            inner.Commit();
            innerOpen = false;

            if (mode == TransactionModes.None)
            {
                if (changed.Added + changed.Modified + changed.Deleted > 0)
                    throw new InvalidOperationException("The script modified the drawing with transaction=\"none\". Use transaction=\"auto\" for scripts that change anything.");

                outer.Abort(); // read-only by contract: nothing to keep, nothing to report as rolled back
                open = false;
            }
            else if (request.DryRun)
            {
                outer.Abort();
                open = false;
                rolledBack = true;
            }
            else
            {
                outer.Commit();
                open = false;
                FlushGraphics(doc);
            }
        }
        catch (OperationCanceledException)
        {
            timedOut = timeoutSource.IsCancellationRequested && !cancelSource.IsCancellationRequested;
            message = timedOut
                ? $"Script timed out after {request.TimeoutSeconds}s (cooperative timeout). Nothing was committed; raise timeoutSeconds (max 120) or do less per call."
                : "Script was cancelled. Nothing was committed.";
            rolledBack = RollBack(manager, outer, inner, ref open, ref innerOpen);
        }
        catch (AcadException exception)
        {
            // ErrorStatus is what the AI can act on: eLockViolation, eNotOpenForWrite, eWasErased, ...
            Log.Warning(exception, "MCP script '{Label}' failed in Civil 3D", label);
            message = SafeText.StripPaths($"{exception.ErrorStatus}: {exception.Message}");
            rolledBack = RollBack(manager, outer, inner, ref open, ref innerOpen);
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "MCP script '{Label}' failed", label);
            message = SafeText.StripPaths($"{exception.GetType().Name}: {exception.Message}");
            rolledBack = RollBack(manager, outer, inner, ref open, ref innerOpen);
        }
        finally
        {
            if (open) RollBack(manager, outer, inner, ref open, ref innerOpen);
            inner?.Dispose();
            outer?.Dispose();
        }

        stopwatch.Stop();
        if (droppedLogs > 0) logs.Add($"… {droppedLogs} more log line(s) dropped (limit {_settings.MaxLogLines})");

        var result = new ExecuteResult
        {
            IsError = message is not null,
            Message = message,
            Logs = logs,
            Changed = message is null ? changed : new ChangedCounts(0, 0, 0), // nothing survives a rolled-back run
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

        WriteSummary(doc, label, mode, result);
        return result;
    }

    /// <summary>
    ///     Best-effort unwind of the bridge's transactions, innermost first. Returns true only when the
    ///     changes are actually gone: the outer abort succeeded, or it had already ended. An abort AutoCAD
    ///     refuses is logged and reported as false so the AI never reads "rolledBack" for a drawing that
    ///     still changed.
    /// </summary>
    private static bool RollBack(DocumentTransactionManager manager, Transaction? outer, Transaction? inner, ref bool open, ref bool innerOpen)
    {
        if (!open || outer is null) return true;

        try
        {
            AbortNested(manager);
            if (innerOpen && inner is not null)
            {
                inner.Abort();
                innerOpen = false;
            }

            outer.Abort();
            open = false;
            return true;
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "MCP script transaction abort failed");
            return false;
        }
    }

    /// <summary>
    ///     Defensive only — the guard keeps scripts from starting transactions. Anything above the bridge's
    ///     two must end before those do; the wrapper TopTransaction hands out is disposed here, the
    ///     script's own one cannot be reached.
    /// </summary>
    private static void AbortNested(DocumentTransactionManager manager)
    {
        for (var guard = 0; manager.NumberOfActiveTransactions > 2 && guard < 16; guard++)
        {
            using var top = manager.TopTransaction;
            top.Abort();
        }
    }

    /// <summary>Outside a command AutoCAD does not redraw on its own; without this the new entity appears on the next regen.</summary>
    private static void FlushGraphics(Document doc)
    {
        try
        {
            doc.TransactionManager.FlushGraphics();
            doc.Editor.UpdateScreen();
        }
        catch (Exception exception)
        {
            Log.Debug(exception, "MCP graphics flush failed");
        }
    }

    /// <summary>One command-line line per run so the user sees what the AI did without opening the window.</summary>
    private static void WriteSummary(Document doc, string label, string mode, ExecuteResult result)
    {
        try
        {
            var outcome = !result.IsError ? "ok" : result.TimedOut ? "timed out" : "error";
            var changes = $"{result.Changed.Added} added, {result.Changed.Modified} modified, {result.Changed.Deleted} erased";
            var undo = result.RolledBack ? "rolled back" : result.IsError ? "nothing kept" : mode == TransactionModes.None ? "read-only" : "undo with U";
            doc.Editor.WriteMessage($"\n[MCP] {label}: {outcome} in {result.DurationMs} ms; {changes}; {undo}.\n");
        }
        catch (Exception exception)
        {
            Log.Debug(exception, "MCP command-line summary could not be written");
        }
    }
}
