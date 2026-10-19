# Remediation Strategy & Refactoring Specification — Milestone M3 (Iteration 2)

**Author:** `explorer_m3_it2_1` (Forensic Audit Remediation Explorer)  
**Date:** 2026-10-10  
**Target Solution:** `HPGrasshopper/HPGrasshopper.slnx`  
**Governing Standard:** `docs/clean-code/HP_CLEAN_CODE_CORE.md` (Rules M3 <= 50 lines, C2 <= 300 lines, Q-B1, Q-B2)

---

## 1. Executive Summary & Root Cause Analysis

In Milestone M3 Gate 1, the work product failed audit and review due to:
1. **Rule M3 Method Length Violations**:
   - `HPGrasshopper.McpBridge/Execution/RevitTransactionScope.cs`: `ExecuteCore` had 78 lines total span (68 lines of body).
   - `HPGrasshopper.McpBridge/Tools/ImageExporter.cs`: `Export` had 58 lines total span (52 lines of body).
   - `HPGrasshopper.McpBridge/Tools/PlayerRunner.cs`: `Run` had 53 lines total span (48 lines of body).
2. **Reviewer Functional/Safety Defect**:
   - `HPGrasshopper.McpBridge/Tools/CanvasEditor.cs`: `RemoveComponents` deleted objects via `doc.RemoveObject(obj, false)` without tracking them in `CanvasUndoScope`. In `dryRun=true` or error rollback, deleted components were permanently lost.
   - `HPGrasshopper.McpBridge/Tools/PlayerRunner.cs`: Calling `doc.NewSolution(true)` passed `expireAllObjects = true`, erasing the volatile parameter inputs set immediately prior by `ApplyInputs`.

### Critical Line-Count Constraint (Rule C2 Ceiling Alert)
`RevitTransactionScope.cs` currently has 288 lines. Simply adding helper methods into `RevitTransactionScope.cs` would expand the file to **315+ lines**, causing an immediate **Rule C2 violation (file > 300 lines)**.
Therefore, the refactoring strategy below utilizes:
1. Extraction of an internal record struct `ExecutionOutcome` into a standalone file `ExecutionOutcome.cs` (~12 lines).
2. Parameter consolidation and deduplication in `RevitTransactionScope.cs`, reducing total lines from 288 to **~255 lines** (a 33-line buffer below the 300-line ceiling).

---

## 2. Refactoring Target 1: `RevitTransactionScope.cs`

### 2.1 New Supporting File: `Execution/ExecutionOutcome.cs`
**Path:** `HPGrasshopper/HPGrasshopper.McpBridge/Execution/ExecutionOutcome.cs`  
**Lines:** 12 lines  
**Responsibilities:** Encapsulates the execution result, error message, and rollback/timeout booleans returned by the transaction execution pipeline.

```csharp
namespace HPGrasshopper.McpBridge.Execution;

/// <summary>
///     Encapsulates the transactional outcome of a Revit action execution.
/// </summary>
internal readonly record struct ExecutionOutcome(
    object? Value,
    string? Error,
    bool RolledBack,
    bool TimedOut);
```

### 2.2 Refactoring `RevitTransactionScope.cs`
**File Line Target:** ~255 lines (down from 288 lines; 45 lines buffer under the 300-line limit).

#### Method Decomposition Plan
| Method | Visibility | Span (Lines) | Body (Lines) | Responsibilities |
|---|---|---|---|---|
| `Execute` | `public static` | 8 | 5 | Validates `uiapp`, returns early failure if null. |
| `ExecuteWithUiApp` | `private static` | 20 | 17 | Unpacks `UIApplication`, `UIDocument`, validates document state, begins counter. |
| `ExecuteWithContext` | `internal static` | 15 | 11 | Direct test/context entry point; validates document state. |
| `ExecuteCore` | `private static` | 32 | 26 | Sets up stopwatch, logging delegate, timeout/cancel tokens; coordinates execution and result construction. |
| `RunActionWithTransactions` | `private static` | 32 | 25 | Manages try/catch boundary, transaction start/finalize, and exception mapping to `ExecutionOutcome`. |
| `BuildExecuteResult` | `private static` | 19 | 15 | Assembles `ExecuteResult` wire DTO from `ExecutionOutcome`, warnings, and counters. |
| `SerializeValue` | `internal static` | 20 | 18 | Unchanged serialization logic. |
| `ValidateState` | `internal static` | 13 | 11 | Unchanged document readiness validation. |
| `BeginTransactions` | `private static` | 12 | 10 | Unchanged TransactionGroup / auto-transaction start logic. |
| `FinalizeTransactions` | `private static` | 22 | 19 | Unchanged TransactionGroup assimilate / auto-transaction commit logic. |
| `SafeRollback` | `private static` | 7 | 5 | Coordinates auto-transaction and group rollback. |
| `TryRollbackAction` | `private static` | 13 | 10 | Extracts duplicate try/catch logging for rollback actions. |

#### Complete Proposed Implementation for `RevitTransactionScope.cs`
```csharp
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
    public delegate object? ExecutionAction(
        object document,
        CancellationToken cancellationToken,
        Action<string?> logger);

    public static ExecuteResult Execute(
        object? uiapp, ExecuteRequest request, BridgeSettings settings, IProgress<ScriptProgress>? progress,
        CancellationTokenSource cancelSource, ExecutionAction action, bool startAutoTransaction = true)
    {
        return uiapp is null
            ? ExecuteResult.Failure("No document is open in Revit. Open a project first.")
            : ExecuteWithUiApp(uiapp, request, settings, progress, cancelSource, action, startAutoTransaction);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ExecuteResult ExecuteWithUiApp(
        object uiapp, ExecuteRequest request, BridgeSettings settings, IProgress<ScriptProgress>? progress,
        CancellationTokenSource cancelSource, ExecutionAction action, bool startAutoTransaction = true)
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
            (ct, log) => action(doc, ct, log), doc.IsModifiable, counter, startAutoTransaction);
    }

    internal static ExecuteResult ExecuteWithContext(
        ITransactionContext txContext, ExecuteRequest request, BridgeSettings settings,
        IProgress<ScriptProgress>? progress, CancellationTokenSource cancelSource,
        Func<CancellationToken, Action<string?>, object?> action,
        bool isDocReadOnly = false, bool isDocFamily = false, bool isDocModifiable = false,
        bool startAutoTransaction = true)
    {
        var mode = TransactionModes.Normalize(request.Transaction) ?? TransactionModes.Auto;
        var validationFailure = ValidateState(mode, isDocReadOnly, isDocFamily, isDocModifiable, settings.AllowFamilyDocuments);
        if (validationFailure != null) return validationFailure;

        return ExecuteCore(
            txContext, request, settings, progress, cancelSource,
            action, isDocModifiable, null, startAutoTransaction);
    }

    private static ExecuteResult ExecuteCore(
        ITransactionContext txContext, ExecuteRequest request, BridgeSettings settings,
        IProgress<ScriptProgress>? progress, CancellationTokenSource cancelSource,
        Func<CancellationToken, Action<string?>, object?> action,
        bool isDocModifiable, DocumentChangeCounter? counter, bool startAutoTransaction = true)
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
                action, linked.Token, cancelSource, timeoutSource, Logger);
            stopwatch.Stop();

            if (droppedLogs > 0)
                logs.Add($"... {droppedLogs} more log lines dropped (limit {settings.MaxLogLines})");

            return BuildExecuteResult(outcome, txContext, logs, counter, stopwatch.ElapsedMilliseconds);
        }
    }

    private static ExecutionOutcome RunActionWithTransactions(
        ITransactionContext txContext, ExecuteRequest request, string mode, string label,
        bool isDocModifiable, bool startAutoTransaction,
        Func<CancellationToken, Action<string?>, object?> action,
        CancellationToken linkedToken, CancellationTokenSource cancelSource,
        CancellationTokenSource timeoutSource, Action<string?> logger)
    {
        BeginTransactions(txContext, mode, label, startAutoTransaction);
        try
        {
            var value = action(linkedToken, logger);
            if (linkedToken.IsCancellationRequested)
                throw new OperationCanceledException(linkedToken);

            var rolledBack = FinalizeTransactions(txContext, request, mode, isDocModifiable, logger);
            return new ExecutionOutcome(value, null, rolledBack, false);
        }
        catch (OperationCanceledException)
        {
            var timedOut = timeoutSource.IsCancellationRequested && !cancelSource.IsCancellationRequested;
            var error = timedOut
                ? $"Execution timed out after {request.TimeoutSeconds}s. Nothing was committed."
                : "Execution was cancelled. Nothing was committed.";
            return new ExecutionOutcome(null, error, SafeRollback(txContext), timedOut);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Execution failed in RevitTransactionScope for label '{Label}'", label);
            var error = SafeText.StripPaths($"{ex.GetType().Name}: {ex.Message}");
            return new ExecutionOutcome(null, error, SafeRollback(txContext), false);
        }
    }

    private static ExecuteResult BuildExecuteResult(
        ExecutionOutcome outcome, ITransactionContext txContext,
        List<string> logs, DocumentChangeCounter? counter, long durationMs)
    {
        var jsonValue = SerializeValue(outcome.Value, out var valType);
        return new ExecuteResult
        {
            IsError = outcome.Error is not null,
            Message = outcome.Error,
            Value = jsonValue,
            ValueType = valType,
            Warnings = txContext.Warnings,
            Logs = logs,
            Changed = counter?.Counts ?? new ChangedCounts(0, 0, 0),
            RolledBack = outcome.RolledBack,
            TimedOut = outcome.TimedOut,
            DurationMs = durationMs
        };
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
            txContext.StartGroup("MCP Grasshopper: " + label);
        }

        if (mode == TransactionModes.Auto && startAutoTransaction)
        {
            txContext.StartAutoTransaction("MCP Grasshopper auto-step");
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
        var autoSuccess = TryRollbackAction(txContext.RollbackAutoTransaction, "auto transaction");
        var groupSuccess = TryRollbackAction(txContext.RollbackGroup, "transaction group");
        return autoSuccess && groupSuccess;
    }

    private static bool TryRollbackAction(Action action, string name)
    {
        try
        {
            action();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception encountered rolling back {Name}", name);
            return false;
        }
    }
}
```

---

## 3. Refactoring Target 2: `ImageExporter.cs`

### 3.1 Method Decomposition Plan
**Current Span:** Lines 15–72 (58 lines total, 52 lines body).  
**Target Span:** Every method <= 32 lines. File length: ~80 lines.

| Method | Visibility | Span (Lines) | Body (Lines) | Responsibilities |
|---|---|---|---|---|
| `Export` | `public` | 32 | 26 | Validates canvas, coordinates bitmap creation, base64 conversion, saving to disk, error handling. |
| `RenderCanvasToBitmap` | `private static` | 12 | 10 | Clamps dimensions, renders `canvas.DrawToBitmap` into new `Bitmap`. |
| `ConvertToBase64` | `private static` | 7 | 5 | Saves bitmap to PNG memory stream, formats data URI. |
| `SaveBitmapToFile` | `private static` | 10 | 8 | Creates directory if needed, saves PNG bitmap to disk. |

#### Complete Proposed Implementation for `ImageExporter.cs`
```csharp
namespace HPGrasshopper.McpBridge.Tools;

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Grasshopper.GUI.Canvas;
using Serilog;

/// <summary>
///     Renders and exports snapshots of the active Grasshopper canvas to PNG / base64 data URLs.
/// </summary>
public sealed class ImageExporter
{
    public object Export(
        GH_Canvas? canvas,
        string mode = "FullCanvas",
        double zoom = 1.0,
        string? outputPath = null,
        bool returnBase64 = true)
    {
        if (canvas is null)
            return new { success = false, error = "Grasshopper canvas is not open or accessible." };

        try
        {
            using var bmp = RenderCanvasToBitmap(canvas, zoom, out var width, out var height);
            var base64 = returnBase64 ? ConvertToBase64(bmp) : null;

            if (!string.IsNullOrWhiteSpace(outputPath))
                SaveBitmapToFile(bmp, outputPath);

            return new
            {
                success = true,
                width,
                height,
                mode,
                outputPath = outputPath ?? string.Empty,
                base64 = base64 ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ImageExporter: Failed to export canvas image");
            return new { success = false, error = $"{ex.GetType().Name}: {ex.Message}" };
        }
    }

    private static Bitmap RenderCanvasToBitmap(GH_Canvas canvas, double zoom, out int width, out int height)
    {
        width = Math.Max(200, (int)(canvas.Width * Math.Clamp(zoom, 0.25, 4.0)));
        height = Math.Max(200, (int)(canvas.Height * Math.Clamp(zoom, 0.25, 4.0)));

        var bmp = new Bitmap(width, height);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.FromArgb(240, 240, 240));
        canvas.DrawToBitmap(bmp, new Rectangle(0, 0, width, height));
        return bmp;
    }

    private static string ConvertToBase64(Bitmap bmp)
    {
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
    }

    private static void SaveBitmapToFile(Bitmap bmp, string outputPath)
    {
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        bmp.Save(outputPath, ImageFormat.Png);
    }
}
```

---

## 4. Refactoring Target 3: `PlayerRunner.cs`

### 4.1 Method Decomposition & Flaw A2 Resolution Plan
**Current Span:** Lines 19–71 (53 lines total, 48 lines body).  
**Target Span:** `Run` method <= 37 lines. `LoadDefinition` helper: 18 lines. File length: ~195 lines.  
**Flaw A2 Fix:** Replace `doc.NewSolution(true)` with `doc.NewSolution(false)` so that volatile inputs pushed to `IGH_Param` are not expired/cleared before graph solution.

| Method | Visibility | Span (Lines) | Body (Lines) | Responsibilities |
|---|---|---|---|---|
| `Run` | `public` | 37 | 31 | Validates file existence, loads definition, applies inputs, solves document, extracts outputs. |
| `LoadDefinition` | `private static` | 18 | 16 | Reads `.gh`/`.ghx` file via `GH_Archive` and extracts "Definition" graph. |
| `ApplyInputs` | `private static` | 22 | 19 | Unchanged input binding. |
| `FindInputParam` | `private static` | 23 | 20 | Unchanged parameter lookup. |
| `SetParamValue` | `private static` | 25 | 22 | Unchanged volatile data injection. |
| `ExtractOutputs` | `private static` | 29 | 26 | Unchanged parameter harvesting. |
| `ExtractParamValue` | `private static` | 10 | 8 | Unchanged parameter value format. |

#### Complete Proposed Implementation for `PlayerRunner.cs`
```csharp
namespace HPGrasshopper.McpBridge.Tools;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Serilog;

/// <summary>
///     Headless execution runner for Grasshopper Player definitions (.gh / .ghx).
/// </summary>
public sealed class PlayerRunner
{
    public object Run(
        string filePath,
        JsonElement? inputs = null,
        IReadOnlyList<string>? requestedOutputs = null,
        bool headless = true)
    {
        if (!File.Exists(filePath))
            return new { success = false, error = $"Definition file not found: {filePath}" };

        var sw = Stopwatch.StartNew();
        var doc = new GH_Document();
        try
        {
            if (!LoadDefinition(doc, filePath, out var loadError))
                return new { success = false, error = loadError };

            var appliedCount = ApplyInputs(doc, inputs);
            doc.NewSolution(false);
            var results = ExtractOutputs(doc, requestedOutputs);
            sw.Stop();

            return new
            {
                success = true,
                durationMs = sw.ElapsedMilliseconds,
                inputsApplied = appliedCount,
                outputs = results
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "PlayerRunner: Execution failed for {Path}", filePath);
            return new { success = false, error = $"{ex.GetType().Name}: {ex.Message}" };
        }
        finally
        {
            if (headless) doc.Dispose();
        }
    }

    private static bool LoadDefinition(GH_Document doc, string filePath, out string? error)
    {
        var archive = new GH_Archive();
        if (!archive.ReadFromFile(filePath))
        {
            error = "Failed to deserialize Grasshopper definition from file.";
            return false;
        }

        if (!archive.ExtractObject(doc, "Definition"))
        {
            error = "Failed to extract definition graph from archive.";
            return false;
        }

        error = null;
        return true;
    }

    private static int ApplyInputs(GH_Document doc, JsonElement? inputs)
    {
        if (!inputs.HasValue || inputs.Value.ValueKind != JsonValueKind.Object)
        {
            return 0;
        }

        var applied = 0;
        foreach (var prop in inputs.Value.EnumerateObject())
        {
            var key = prop.Name;
            var targetParam = FindInputParam(doc, key);
            if (targetParam == null) continue;

            if (SetParamValue(targetParam, prop.Value))
            {
                applied++;
            }
        }

        return applied;
    }

    private static IGH_Param? FindInputParam(GH_Document doc, string nameOrGuid)
    {
        if (Guid.TryParse(nameOrGuid, out var guid))
        {
            return doc.FindObject(guid, true) as IGH_Param;
        }

        foreach (var obj in doc.Objects)
        {
            if (obj is IGH_Param param && string.Equals(param.NickName, nameOrGuid, StringComparison.OrdinalIgnoreCase))
            {
                return param;
            }
            if (obj is IGH_Component comp)
            {
                foreach (var input in comp.Params.Input)
                {
                    if (string.Equals(input.NickName, nameOrGuid, StringComparison.OrdinalIgnoreCase))
                        return input;
                }
            }
        }
        return null;
    }

    private static bool SetParamValue(IGH_Param param, JsonElement val)
    {
        try
        {
            if (val.ValueKind == JsonValueKind.Number && val.TryGetDouble(out var d))
            {
                param.AddVolatileData(new GH_Path(0), 0, new GH_Number(d));
                return true;
            }
            if (val.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                param.AddVolatileData(new GH_Path(0), 0, new GH_Boolean(val.GetBoolean()));
                return true;
            }
            if (val.ValueKind == JsonValueKind.String)
            {
                param.AddVolatileData(new GH_Path(0), 0, new GH_String(val.GetString()));
                return true;
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed setting input parameter {Param}", param.NickName);
        }
        return false;
    }

    private static Dictionary<string, object?> ExtractOutputs(GH_Document doc, IReadOnlyList<string>? requestedOutputs)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var filterSet = requestedOutputs?.Count > 0 ? new HashSet<string>(requestedOutputs, StringComparer.OrdinalIgnoreCase) : null;

        foreach (var obj in doc.Objects)
        {
            if (obj is IGH_Param param)
            {
                if (filterSet != null && !filterSet.Contains(param.NickName)) continue;
                if (param.Recipients.Count == 0 && param.VolatileDataCount > 0)
                {
                    result[param.NickName] = ExtractParamValue(param);
                }
            }
            else if (obj is IGH_Component comp)
            {
                foreach (var output in comp.Params.Output)
                {
                    if (filterSet != null && !filterSet.Contains(output.NickName)) continue;
                    if (output.VolatileDataCount > 0)
                    {
                        result[output.NickName] = ExtractParamValue(output);
                    }
                }
            }
        }
        return result;
    }

    private static object? ExtractParamValue(IGH_Param param)
    {
        if (param.VolatileDataCount == 0) return null;
        var list = new List<object?>();
        foreach (var item in param.VolatileData.AllData(true))
        {
            if (item != null) list.Add(item.ToString());
        }
        return list.Count == 1 ? list[0] : list;
    }
}
```

---

## 5. Remediation Target 4: `CanvasUndoScope.cs` & `CanvasEditor.cs` (Flaw A1 Fix)

### 5.1 Problem Statement
When `batch_edit_grasshopper_canvas` removes components (`componentsToRemove`), `CanvasEditor.RemoveComponents` calls `doc.RemoveObject(obj, false)`.
`CanvasUndoScope` did not record deleted objects. When `dryRun=true` or an exception triggered `undoScope.Rollback()`, the removed objects were never restored, violating the Level 2 Safety guarantee.

### 5.2 Remediation in `Execution/CanvasUndoScope.cs`
1. Add field:
   ```csharp
   private readonly List<IGH_DocumentObject> _removedObjects = new();
   ```
2. Add tracking method:
   ```csharp
   public void TrackRemovedObject(IGH_DocumentObject obj)
   {
       if (obj != null)
       {
           _removedObjects.Add(obj);
       }
   }
   ```
3. Add restoration method and call in `Rollback()`:
   ```csharp
   private void RollbackRemovedComponents()
   {
       foreach (var obj in _removedObjects)
       {
           try
           {
               _document.AddObject(obj, false);
           }
           catch (Exception ex)
           {
               Log.Debug(ex, "Failed to restore removed object during canvas rollback");
           }
       }
   }
   ```
4. Update `Rollback()`:
   ```csharp
   public void Rollback()
   {
       if (_isCompleted) return;

       try
       {
           RollbackAddedComponents();
           RollbackRemovedComponents();
           RollbackWires();
           TriggerNativeUndo();

           _document.NewSolution(false);
           Log.Information("CanvasUndoScope: Rolled back {AddedCount} added, {RemovedCount} removed objects cleanly",
               _addedGuids.Count, _removedObjects.Count);
       }
       catch (Exception ex)
       {
           Log.Warning(ex, "CanvasUndoScope: Error during canvas rollback");
       }
       finally
       {
           _isCompleted = true;
       }
   }
   ```
Total file length: ~155 lines (well below 300).

### 5.3 Remediation in `Tools/CanvasEditor.cs`
Update `RemoveComponents` to accept `CanvasUndoScope undoScope` and track objects:
```csharp
    private static int RemoveComponents(GH_Document doc, JsonElement args, CanvasUndoScope undoScope)
    {
        if (!args.TryGetProperty("componentsToRemove", out var list) || list.ValueKind != JsonValueKind.Array)
            return 0;

        var count = 0;
        foreach (var item in list.EnumerateArray())
        {
            if (Guid.TryParse(item.GetString(), out var guid))
            {
                var obj = doc.FindObject(guid, true);
                if (obj != null)
                {
                    undoScope.TrackRemovedObject(obj);
                    doc.RemoveObject(obj, false);
                    count++;
                }
            }
        }
        return count;
    }
```
And in `ExecuteBatch`:
```csharp
    var removed = RemoveComponents(doc, args, undoScope);
```
Total file length: ~205 lines (well below 300).

---

## 6. Metric Verification & Safety Proof

### 6.1 Method Length Prediction Table (Rule M3)
| File | Method | Original Span | Target Span | Target Body | Rule M3 Limit | Status |
|---|---|---|---|---|---|---|
| `RevitTransactionScope.cs` | `ExecuteCore` | 78 | 32 | 26 | <= 50 | **PASS** |
| `RevitTransactionScope.cs` | `RunActionWithTransactions` | New | 32 | 25 | <= 50 | **PASS** |
| `RevitTransactionScope.cs` | `BuildExecuteResult` | New | 19 | 15 | <= 50 | **PASS** |
| `RevitTransactionScope.cs` | `ExecuteWithUiApp` | 35 | 20 | 17 | <= 50 | **PASS** |
| `ImageExporter.cs` | `Export` | 58 | 32 | 26 | <= 50 | **PASS** |
| `ImageExporter.cs` | `RenderCanvasToBitmap` | New | 12 | 10 | <= 50 | **PASS** |
| `ImageExporter.cs` | `ConvertToBase64` | New | 7 | 5 | <= 50 | **PASS** |
| `ImageExporter.cs` | `SaveBitmapToFile` | New | 10 | 8 | <= 50 | **PASS** |
| `PlayerRunner.cs` | `Run` | 53 | 37 | 31 | <= 50 | **PASS** |
| `PlayerRunner.cs` | `LoadDefinition` | New | 18 | 16 | <= 50 | **PASS** |
| `CanvasUndoScope.cs` | `RollbackRemovedComponents` | New | 14 | 11 | <= 50 | **PASS** |
| `CanvasEditor.cs` | `RemoveComponents` | 19 | 20 | 17 | <= 50 | **PASS** |

### 6.2 File Length Prediction Table (Rule C2)
| File | Original Lines | Target Lines | Rule C2 Limit | Headroom | Status |
|---|---|---|---|---|---|
| `Execution/RevitTransactionScope.cs` | 288 | 255 | <= 300 | +45 lines | **PASS** |
| `Execution/ExecutionOutcome.cs` | New | 12 | <= 300 | +288 lines | **PASS** |
| `Tools/ImageExporter.cs` | 73 | 80 | <= 300 | +220 lines | **PASS** |
| `Tools/PlayerRunner.cs` | 188 | 195 | <= 300 | +105 lines | **PASS** |
| `Execution/CanvasUndoScope.cs` | 140 | 155 | <= 300 | +145 lines | **PASS** |
| `Tools/CanvasEditor.cs` | 204 | 205 | <= 300 | +95 lines | **PASS** |

---

## 7. Concrete Step-by-Step Implementation Instructions for Implementer

1. **Step 1**: Create `HPGrasshopper/HPGrasshopper.McpBridge/Execution/ExecutionOutcome.cs` with the record struct definition.
2. **Step 2**: Replace `HPGrasshopper/HPGrasshopper.McpBridge/Execution/RevitTransactionScope.cs` with the refactored code (§2.2).
3. **Step 3**: Replace `HPGrasshopper/HPGrasshopper.McpBridge/Tools/ImageExporter.cs` with the decomposed code (§3.1).
4. **Step 4**: Replace `HPGrasshopper/HPGrasshopper.McpBridge/Tools/PlayerRunner.cs` with the decomposed code and `doc.NewSolution(false)` fix (§4.1).
5. **Step 5**: Update `CanvasUndoScope.cs` to add `TrackRemovedObject` and `RollbackRemovedComponents` (§5.2).
6. **Step 6**: Update `CanvasEditor.cs` to pass `undoScope` into `RemoveComponents` (§5.3).
7. **Step 7**: Update `CanvasUndoScopeTests.cs` to assert reflection on `TrackRemovedObject` and `_removedObjects`.
8. **Step 8**: Run build and tests:
   ```powershell
   dotnet build HPGrasshopper/HPGrasshopper.slnx -c Release
   dotnet test HPGrasshopper/HPGrasshopper.Mcp.Server.Tests -c Release
   ```
9. **Step 9**: Run accurate AST method analyzer:
   ```powershell
   python "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m3_1\accurate_ast_methods.py"
   ```
   *Expected: Zero method length violations.*
