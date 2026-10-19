# Grasshopper Canvas Rollback & Player Safety Strategy

**Author:** `explorer_m3_it2_2` (Dual-Safety & Undo Scope Explorer)  
**Milestone:** M3 Iteration 2 (HPGrasshopper Canvas Rollback & Player Safety)  
**Target Solution:** `HPGrasshopper/HPGrasshopper.slnx`  
**Target Projects:** `HPGrasshopper.McpBridge` (`Execution/CanvasUndoScope.cs`, `Tools/CanvasEditor.cs`, `Tools/PlayerRunner.cs`)

---

## 1. Executive Summary

Forensic review and adversarial auditing revealed two critical Level 2 safety defects in `HPGrasshopper.McpBridge`:

1. **Permanent Object Deletion during `dryRun=true` in `CanvasEditor` / `CanvasUndoScope`**:
   `CanvasEditor.RemoveComponents` calls `doc.RemoveObject(obj, false)` without registering an undo record or passing the target object to `CanvasUndoScope`. Consequently, `CanvasUndoScope` has no record of deleted objects and only removes newly added objects on rollback. Calling `batch_edit_grasshopper_canvas` with `dryRun=true` or experiencing an exception permanently destroys existing components and severed wires on the user's active canvas.

2. **Volatile Input Invalidation in `PlayerRunner.Run`**:
   `PlayerRunner.Run` sets input values via `param.AddVolatileData(...)` and then calls `doc.NewSolution(true)` with `expireAllObjects = true`. In Grasshopper, `expireAllObjects = true` triggers `doc.ExpireSolution()`, which calls `ClearData()` on every parameter in the document, wiping out the freshly supplied input volatile data before calculation occurs.

This strategy document provides the architectural and mathematical specification to resolve both defects cleanly, ensuring:
- Full snapshotting and compensating rollback for removed components and severed wires.
- Native `GH_UndoRecord` integration for Grasshopper UI (Ctrl+Z).
- Reliable solution computation in `PlayerRunner` without clearing applied inputs.
- 100% compliance with Clean Code Rules M3 ($\le 50$ lines per method) and C2 ($\le 300$ lines per file).

---

## 2. Root Cause Analysis

### 2.1 Canvas Component Deletion & Rollback Defect

In `HPGrasshopper.McpBridge/Tools/CanvasEditor.cs:104-123`:
```csharp
private static int RemoveComponents(GH_Document doc, JsonElement args)
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
                doc.RemoveObject(obj, false);
                count++;
            }
        }
    }
    return count;
}
```

#### Observations:
1. `RemoveComponents` does not receive `CanvasUndoScope undoScope`.
2. When `doc.RemoveObject(obj, false)` is invoked:
   - Grasshopper removes `obj` from `doc.Objects`.
   - Grasshopper automatically disconnects all incoming wires to `obj`'s inputs (`comp.Params.Input[i].RemoveAllSources()`).
   - Grasshopper automatically disconnects all outgoing wires from `obj`'s outputs to downstream recipients (`recipient.RemoveSource(comp.Params.Output[j])`).
3. In `CanvasUndoScope.cs`:
   - `CanvasUndoScope` only tracks `_addedGuids`, `_connectedWires`, and `_disconnectedWires`.
   - On `Rollback()`:
     ```csharp
     RollbackAddedComponents(); // doc.RemoveObject on _addedGuids
     RollbackWires();           // Reverts _connectedWires and _disconnectedWires
     TriggerNativeUndo();       // doc.Undo()
     ```
   - Because `obj` was never tracked, `_removedObjects` was non-existent.
   - `TriggerNativeUndo()` calls `doc.Undo()`, but no undo action was registered in `_undoRecord` for `RemoveObject`.
   - **Result:** Any components removed during `dryRun=true` are permanently lost from the active canvas.

### 2.2 Volatile Input Expiration Defect

In `HPGrasshopper.McpBridge/Tools/PlayerRunner.cs:46-47`:
```csharp
var appliedCount = ApplyInputs(doc, inputs);
doc.NewSolution(true);
```

#### Observations:
1. In `ApplyInputs`, values are pushed into target parameters using `param.AddVolatileData(...)`.
2. `doc.NewSolution(true)` is called with `expireAllObjects = true`.
3. In Grasshopper's internal execution pipeline:
   - When `expireAllObjects == true`, `GH_Document.ExpireSolution()` is called.
   - `ExpireSolution()` traverses all objects in `doc.Objects` and invokes `param.ExpireSolution(false)`.
   - `GH_Param<T>.ExpireSolution(bool)` calls `param.ClearData()`, which empties `param.m_data` (the volatile data tree).
   - Consequently, the volatile data set by `ApplyInputs` is deleted immediately before the solver executes.
   - The graph calculates using default persistent values (or null), discarding user-provided arguments.

---

## 3. Architecture & Design Specification

### 3.1 Dual-Level Component Preservation Strategy

To guarantee that any component deleted during a batch operation can be completely restored, we implement a **Dual-Level Preservation Model**:

```
                       [Target Component]
                              │
             ┌────────────────┴────────────────┐
             ▼                                 ▼
   Level 1: Live Managed             Level 2: Binary Archive
   Instance Preservation             Chunk Serialization
   - Retains exact runtime state     - Retains serialized byte[]
   - Preserves attributes, pivot     - Created via GH_LooseChunk
   - Fast, zero allocation           - Fallback verification
             │                                 │
             └────────────────┬────────────────┘
                              ▼
                     [Severed Wire Ledger]
                     - Incoming: (param, upstream source)
                     - Outgoing: (downstream recipient, output)
                              │
                              ▼
                   [CanvasUndoScope Ledger]
```

#### Data Model: `RemovedComponentSnapshot`

A dedicated snapshot class encapsulates:
1. The live `IGH_DocumentObject` instance.
2. The `Guid InstanceGuid`.
3. The list of severed incoming and outgoing wires `(IGH_Param Target, IGH_Param Source)`.
4. The serialized binary archive bytes `byte[]? ArchiveBytes` generated via `GH_LooseChunk`.

```csharp
internal sealed class RemovedComponentSnapshot
{
    public IGH_DocumentObject Object { get; }
    public Guid InstanceGuid => Object.InstanceGuid;
    public IReadOnlyList<(IGH_Param Target, IGH_Param Source)> SeveredWires { get; }
    public byte[]? ArchiveBytes { get; }

    public RemovedComponentSnapshot(
        IGH_DocumentObject obj,
        IReadOnlyList<(IGH_Param Target, IGH_Param Source)> severedWires,
        byte[]? archiveBytes)
    {
        Object = obj ?? throw new ArgumentNullException(nameof(obj));
        SeveredWires = severedWires ?? Array.Empty<(IGH_Param, IGH_Param)>();
        ArchiveBytes = archiveBytes;
    }
}
```

### 3.2 Wire Harvesting Algorithm

Before `doc.RemoveObject(obj, false)` is executed, all active wires connected to `obj` must be harvested:

```csharp
private static List<(IGH_Param Target, IGH_Param Source)> CaptureObjectWires(IGH_DocumentObject obj)
{
    var wires = new List<(IGH_Param Target, IGH_Param Source)>();

    if (obj is IGH_Component comp)
    {
        foreach (var input in comp.Params.Input)
        {
            foreach (var source in input.Sources)
            {
                wires.Add((input, source));
            }
        }

        foreach (var output in comp.Params.Output)
        {
            foreach (var recipient in output.Recipients)
            {
                wires.Add((recipient, output));
            }
        }
    }
    else if (obj is IGH_Param param)
    {
        foreach (var source in param.Sources)
        {
            wires.Add((param, source));
        }

        foreach (var recipient in param.Recipients)
        {
            wires.Add((recipient, param));
        }
    }

    return wires;
}
```

### 3.3 Archive Binary Serialization

Using `GH_IO.Serialization.GH_LooseChunk`:

```csharp
private static byte[]? CaptureArchiveSnapshot(IGH_DocumentObject obj)
{
    try
    {
        var chunk = new GH_LooseChunk("ObjectSnapshot");
        if (obj.Write(chunk))
        {
            return chunk.Serialize_Binary();
        }
    }
    catch (Exception ex)
    {
        Log.Debug(ex, "Failed to capture binary archive chunk for object {Guid}", obj.InstanceGuid);
    }
    return null;
}
```

### 3.4 Multi-Phase Compensating Rollback Pipeline

The rollback sequence must execute in strict reverse dependency order:

```
[Rollback Triggered] (dryRun=true or Exception)
         │
         ▼
[Phase 1: RollbackConnectedWires]
   - Disconnect explicitly connected wires (wiresToConnect)
         │
         ▼
[Phase 2: RollbackDisconnectedWires]
   - Reconnect explicitly disconnected wires (wiresToDisconnect)
         │
         ▼
[Phase 3: RollbackAddedComponents]
   - Remove newly created components from doc.Objects
         │
         ▼
[Phase 4: RollbackRemovedComponents]
   - Sub-phase 4A: Re-add all removed objects to doc.Objects (doc.AddObject)
   - Sub-phase 4B: Reconnect all harvested severed wires (target.AddSource)
         │
         ▼
[Phase 5: PopNativeUndoRecord]
   - Pop or clean undo record from doc.UndoServer if pushed
         │
         ▼
[Phase 6: NewSolution]
   - Recompute canvas state cleanly with doc.NewSolution(false)
```

**Key Architectural Insight**: Phase 4 is split into Sub-phase 4A (re-adding all objects) and Sub-phase 4B (reconnecting wires). This ensures that if two interconnected components A and B were both deleted, both A and B are restored to the document before wires between them are re-established.

---

## 4. PlayerRunner Solution Calculation Strategy

### 4.1 Solution Flow Without Volatile Expiration

To prevent `ApplyInputs` data from being erased by `ExpireSolution`:

1. **Pre-solve Expiration (`doc.ExpireSolution()`)**:
   Clear any stale cached calculation trees from the `.gh` file before applying new values.
2. **Input Application with Downstream Invalidation**:
   - For `GH_NumberSlider`: Call `slider.SetSliderValue((decimal)d)`, which updates the slider's UI value and invalidates downstream components.
   - For general parameters:
     ```csharp
     param.ClearData();
     param.AddVolatileData(new GH_Path(0), 0, ghValue);
     param.ExpireDownStreamObjects();
     ```
   `ExpireDownStreamObjects()` invalidates all dependent components without touching `param`'s own data.
3. **Execution with `expireAllObjects = false`**:
   ```csharp
   doc.NewSolution(false);
   ```
   Grasshopper solves only the expired downstream objects using the newly supplied volatile inputs!

### 4.2 Modular Refactoring for Rule M3

In `PlayerRunner.cs`, `Run` originally spanned 53 lines. Extracting `LoadDefinition` reduces `Run` to 36 lines:

```csharp
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
```

---

## 5. Concrete Proposed Code Implementations

### 5.1 Proposed `CanvasUndoScope.cs`

Target File: `HPGrasshopper/HPGrasshopper.McpBridge/Execution/CanvasUndoScope.cs`  
Lines: ~210 lines (well under Rule C2 limit of 300 lines)  
Method lengths: All methods $\le 25$ lines (well under Rule M3 limit of 50 lines)

```csharp
namespace HPGrasshopper.McpBridge.Execution;

using System;
using System.Collections.Generic;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Undo;
using Serilog;

/// <summary>
///     Level 2 Safety: Manages Grasshopper canvas mutation ledger and undo boundaries.
///     Enforces GH_UndoRecord push and compensating rollback for dryRun=true or failures.
/// </summary>
public sealed class CanvasUndoScope : IDisposable
{
    private readonly GH_Document _document;
    private readonly GH_UndoRecord _undoRecord;
    private readonly List<Guid> _addedGuids = new();
    private readonly List<RemovedComponentSnapshot> _removedSnapshots = new();
    private readonly List<(IGH_Param Target, IGH_Param Source)> _disconnectedWires = new();
    private readonly List<(IGH_Param Target, IGH_Param Source)> _connectedWires = new();
    private bool _isCompleted;

    public CanvasUndoScope(GH_Document document, string actionName = "MCP Grasshopper Canvas Action")
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _undoRecord = new GH_UndoRecord(actionName);
        try
        {
            _document.UndoServer?.PushUndoRecord(_undoRecord);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to push undo record onto GH_Document.UndoServer");
        }
    }

    public void TrackAddedObject(Guid instanceGuid)
    {
        _addedGuids.Add(instanceGuid);
    }

    public void TrackRemovedObject(IGH_DocumentObject obj)
    {
        if (obj == null) return;
        var severedWires = CaptureObjectWires(obj);
        var archiveBytes = CaptureArchiveSnapshot(obj);
        _removedSnapshots.Add(new RemovedComponentSnapshot(obj, severedWires, archiveBytes));
    }

    public void TrackConnectedWire(IGH_Param target, IGH_Param source)
    {
        _connectedWires.Add((target, source));
    }

    public void TrackDisconnectedWire(IGH_Param target, IGH_Param source)
    {
        _disconnectedWires.Add((target, source));
    }

    public void Rollback()
    {
        if (_isCompleted) return;

        try
        {
            RollbackConnectedWires();
            RollbackDisconnectedWires();
            RollbackAddedComponents();
            RollbackRemovedComponents();
            PopNativeUndoRecord();

            _document.NewSolution(false);
            Log.Information(
                "CanvasUndoScope: Rolled back {AddedCount} added, {RemovedCount} removed objects cleanly",
                _addedGuids.Count,
                _removedSnapshots.Count);
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

    private void RollbackConnectedWires()
    {
        foreach (var (target, source) in _connectedWires)
        {
            try { target.RemoveSource(source); }
            catch (Exception ex) { Log.Debug(ex, "Failed to disconnect wire during rollback"); }
        }
    }

    private void RollbackDisconnectedWires()
    {
        foreach (var (target, source) in _disconnectedWires)
        {
            try
            {
                if (!target.Sources.Contains(source)) target.AddSource(source);
            }
            catch (Exception ex) { Log.Debug(ex, "Failed to restore wire during rollback"); }
        }
    }

    private void RollbackAddedComponents()
    {
        foreach (var guid in _addedGuids)
        {
            var obj = _document.FindObject(guid, true);
            if (obj != null) _document.RemoveObject(obj, false);
        }
    }

    private void RollbackRemovedComponents()
    {
        for (var i = _removedSnapshots.Count - 1; i >= 0; i--)
        {
            var snapshot = _removedSnapshots[i];
            try { _document.AddObject(snapshot.Object, false); }
            catch (Exception ex) { Log.Warning(ex, "Failed to re-add removed object {Guid}", snapshot.InstanceGuid); }
        }

        foreach (var snapshot in _removedSnapshots)
        {
            foreach (var (target, source) in snapshot.SeveredWires)
            {
                try
                {
                    if (!target.Sources.Contains(source)) target.AddSource(source);
                }
                catch (Exception ex) { Log.Debug(ex, "Failed to restore severed wire for object {Guid}", snapshot.InstanceGuid); }
            }
        }
    }

    private void PopNativeUndoRecord()
    {
        try
        {
            if (_document.UndoServer != null && _document.UndoServer.UndoCount > 0)
            {
                _document.UndoServer.PopUndoRecord();
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed popping undo record from UndoServer during rollback");
        }
    }

    private static List<(IGH_Param Target, IGH_Param Source)> CaptureObjectWires(IGH_DocumentObject obj)
    {
        var wires = new List<(IGH_Param Target, IGH_Param Source)>();
        if (obj is IGH_Component comp)
        {
            foreach (var input in comp.Params.Input)
                foreach (var source in input.Sources)
                    wires.Add((input, source));

            foreach (var output in comp.Params.Output)
                foreach (var recipient in output.Recipients)
                    wires.Add((recipient, output));
        }
        else if (obj is IGH_Param param)
        {
            foreach (var source in param.Sources)
                wires.Add((param, source));

            foreach (var recipient in param.Recipients)
                wires.Add((recipient, param));
        }
        return wires;
    }

    private static byte[]? CaptureArchiveSnapshot(IGH_DocumentObject obj)
    {
        try
        {
            var chunk = new GH_LooseChunk("ObjectSnapshot");
            if (obj.Write(chunk)) return chunk.Serialize_Binary();
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed capturing binary archive chunk for object {Guid}", obj.InstanceGuid);
        }
        return null;
    }

    public void Commit() => _isCompleted = true;

    public void Dispose()
    {
        if (!_isCompleted) Rollback();
    }
}

internal sealed class RemovedComponentSnapshot
{
    public IGH_DocumentObject Object { get; }
    public Guid InstanceGuid => Object.InstanceGuid;
    public IReadOnlyList<(IGH_Param Target, IGH_Param Source)> SeveredWires { get; }
    public byte[]? ArchiveBytes { get; }

    public RemovedComponentSnapshot(
        IGH_DocumentObject obj,
        IReadOnlyList<(IGH_Param Target, IGH_Param Source)> severedWires,
        byte[]? archiveBytes)
    {
        Object = obj ?? throw new ArgumentNullException(nameof(obj));
        SeveredWires = severedWires ?? Array.Empty<(IGH_Param, IGH_Param)>();
        ArchiveBytes = archiveBytes;
    }
}
```

---

### 5.2 Proposed `CanvasEditor.cs` Updates

Target File: `HPGrasshopper/HPGrasshopper.McpBridge/Tools/CanvasEditor.cs`  
Lines 33-36: Pass `undoScope` into `RemoveComponents`:
```csharp
            added = AddComponents(doc, args, tempMap, undoScope);
            removed = RemoveComponents(doc, args, undoScope);
            connected = ConnectWires(doc, args, tempMap, undoScope);
            disconnected = DisconnectWires(doc, args, undoScope);
```

Lines 104-123: Update `RemoveComponents` to track each object before removal:
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

---

### 5.3 Proposed `PlayerRunner.cs` Updates

Target File: `HPGrasshopper/HPGrasshopper.McpBridge/Tools/PlayerRunner.cs`

1. Extract `LoadDefinition`:
```csharp
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
```

2. Update `Run`:
```csharp
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

            doc.ExpireSolution();
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
```

3. Update `SetParamValue`:
```csharp
    private static bool SetParamValue(IGH_Param param, JsonElement val)
    {
        try
        {
            if (val.ValueKind == JsonValueKind.Number && val.TryGetDouble(out var d))
            {
                if (param is Grasshopper.Kernel.Special.GH_NumberSlider slider)
                {
                    slider.SetSliderValue((decimal)d);
                    return true;
                }
                param.ClearData();
                param.AddVolatileData(new GH_Path(0), 0, new GH_Number(d));
                param.ExpireDownStreamObjects();
                return true;
            }
            if (val.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                param.ClearData();
                param.AddVolatileData(new GH_Path(0), 0, new GH_Boolean(val.GetBoolean()));
                param.ExpireDownStreamObjects();
                return true;
            }
            if (val.ValueKind == JsonValueKind.String)
            {
                param.ClearData();
                param.AddVolatileData(new GH_Path(0), 0, new GH_String(val.GetString()));
                param.ExpireDownStreamObjects();
                return true;
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed setting input parameter {Param}", param.NickName);
        }
        return false;
    }
```

---

## 6. Clean Code Metrics Audit Table

| File | Proposed Line Count (Limit $\le 300$) | Longest Method | Longest Method Span (Limit $\le 50$) | Rule M3 & C2 Status |
|---|---|---|---|---|
| `CanvasUndoScope.cs` | ~210 lines | `Rollback` | 24 lines | **PASS** |
| `CanvasEditor.cs` | ~208 lines | `ExecuteBatch` | 38 lines | **PASS** |
| `PlayerRunner.cs` | ~195 lines | `Run` | 36 lines | **PASS** |

---

## 7. Next Steps for Implementer Worker Agent

1. Apply proposed changes to:
   - `HPGrasshopper/HPGrasshopper.McpBridge/Execution/CanvasUndoScope.cs`
   - `HPGrasshopper/HPGrasshopper.McpBridge/Tools/CanvasEditor.cs`
   - `HPGrasshopper/HPGrasshopper.McpBridge/Tools/PlayerRunner.cs`
2. Address the other two Rule M3 findings:
   - `HPGrasshopper/HPGrasshopper.McpBridge/Execution/RevitTransactionScope.cs` (`ExecuteCore` -> extract helpers)
   - `HPGrasshopper/HPGrasshopper.McpBridge/Tools/ImageExporter.cs` (`Export` -> extract helpers)
3. Build and test:
   - `dotnet build HPGrasshopper/HPGrasshopper.slnx -c Release`
   - `dotnet test HPGrasshopper/HPGrasshopper.Mcp.Server.Tests -c Release`
   - Run `accurate_ast_methods.py` to verify zero methods $> 50$ lines.
