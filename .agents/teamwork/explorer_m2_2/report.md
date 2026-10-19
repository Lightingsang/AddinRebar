# Investigation Report: Canvas Rollback Ledger & Object Lifecycle (Finding R4)

**Role**: Explorer 2 (Milestone M2)  
**Agent ID**: `explorer_m2_2`  
**Date**: 2026-10-12  
**Target Solution**: `HPGrasshopper/` (`HPGrasshopper.McpBridge`, `HPGrasshopper.Mcp.Server`, `HPGrasshopper.McpBridge.Tests`)  
**Scope**: Canvas Undo Scope, Rollback Ledger, Object Lifecycle & Wire Mutation Timing  
**Status**: Completed Investigation (Read-Only)

---

## Executive Summary

This investigation resolves Finding R4 from `plans/reports/261012-2212-hpgrasshopper-reaudit.md`:
1. **Root Cause of Temporary Object Resurrection**: `CanvasUndoScope` tracks added objects (`_addedGuids`) and removed objects (`_removedSnapshots`) completely independently. When an object is created and subsequently deleted within the same scope, it is recorded in `_removedSnapshots`. Upon rollback, `RollbackRemovedComponents()` unconditionally deserializes and re-adds all items in `_removedSnapshots` (line 221), resurrecting objects on canvas that never existed in the baseline document.
2. **Wire Action Inversion**: In `CanvasEditor.cs` (lines 216–217, 246–247), wire mutations (`AddSource` and `RemoveSource`) are performed *before* tracking them in `CanvasUndoScope`. Because `GH_WireAction` snapshots `param.Sources` upon instantiation in its constructor, tracking *after* mutation captures post-mutation state rather than pre-mutation state, corrupting native Ctrl+Z undo behavior.
3. **Silent Partial Restore Failures**: `CanvasUndoScope.Rollback()` swallows wire restoration errors, archive deserialization errors, and severed wire restoration errors into debug logs, while `CanvasEditor.cs` line 89 marks `rolledBackCleanly = true` as long as `Rollback()` does not throw, reporting misleading clean rollback status to callers.
4. **Concrete Solution & Clean Code Adherence**:
   - Establish baseline object tracking (`_baselineGuids` snapshot at construction) combined with in-scope tracking (`_addedGuids` / `_addedGuidsSet`).
   - When an object is deleted, check `IsAddedInScope(guid)`. If true, suppress `_removedSnapshots` addition and native `GH_RemoveObjectAction`. Rollback will never resurrect it.
   - Invert wire mutation order in `CanvasEditor.cs` to track *before* mutating.
   - Surface rollback integrity via `bool Rollback()` and `CleanlyRolledBack` property.
   - Extract `RemovedComponentSnapshot` to its own file to ensure `CanvasUndoScope.cs` stays <= 275 lines (complying with Rule C2 <= 300 lines) and all methods stay <= 40 lines (complying with Rule M3 <= 50 lines).

---

## 1. Codebase Analysis & Root Cause

### 1.1 CanvasUndoScope.cs Structure & Current Flow

`CanvasUndoScope.cs` is located at `HPGrasshopper/HPGrasshopper.McpBridge/Execution/CanvasUndoScope.cs` (296 lines).
The relevant tracking structures:
```csharp
20: private readonly List<Guid> _addedGuids = new();
21: private readonly List<RemovedComponentSnapshot> _removedSnapshots = new();
22: private readonly List<(IGH_Param Target, IGH_Param Source)> _disconnectedWires = new();
23: private readonly List<(IGH_Param Target, IGH_Param Source)> _connectedWires = new();
```

When objects are added to the document:
```csharp
70: private void OnObjectsAdded(object sender, GH_DocObjectEventArgs e)
71: {
72:     if (e?.Objects == null) return;
73:     foreach (var obj in e.Objects)
74:     {
75:         if (obj != null && !_addedGuids.Contains(obj.InstanceGuid))
76:         {
77:             _addedGuids.Add(obj.InstanceGuid);
78:             AddAction(new GH_AddObjectAction(obj));
79:         }
80:     }
81: }
```

When objects are deleted from the document:
```csharp
83: private void OnObjectsDeleted(object sender, GH_DocObjectEventArgs e)
84: {
85:     if (e?.Objects == null) return;
86:     foreach (var obj in e.Objects)
87:     {
88:         if (obj != null) TrackRemovedObject(obj);
89:     }
90: }
```

And in `TrackRemovedObject`:
```csharp
114: public void TrackRemovedObject(IGH_DocumentObject obj)
115: {
116:     if (obj == null) return;
117:     if (!_removedSnapshots.Any(s => s.InstanceGuid == obj.InstanceGuid))
118:     {
119:         var severedWires = CaptureObjectWires(obj);
120:         var archiveBytes = CaptureArchiveSnapshot(obj);
121:         _removedSnapshots.Add(new RemovedComponentSnapshot(obj, severedWires, archiveBytes));
122:         try { AddAction(new GH_RemoveObjectAction(obj)); }
123:         catch (Exception ex) { Log.Debug(ex, "Failed creating GH_RemoveObjectAction for {Guid}", obj.InstanceGuid); }
124:     }
125: }
```

### 1.2 The Temporary Object Bug: Step-by-Step Trace

Consider a scenario where the baseline canvas contains zero components:
1. **Creation**: The user/agent invokes `batch_edit_grasshopper_canvas` or a script with dryRun=true.
   - Component A is created and added to document: `doc.AddObject(A)`.
   - `OnObjectsAdded` or `undoScope.TrackAddedObject(A.InstanceGuid)` runs.
   - State: `_addedGuids` = `[ Guid_A ]`.
   - Native undo: `GH_AddObjectAction(A)` added to record.
2. **Deletion**: Within the same scope, the user/agent decides to remove component A:
   - `doc.RemoveObject(A)` or `undoScope.TrackRemovedObject(A)` runs.
   - `OnObjectsDeleted` fires, calling `TrackRemovedObject(A)`.
   - `_removedSnapshots.Any(s => s.InstanceGuid == A.InstanceGuid)` is `false`.
   - A snapshot of A (with archive bytes) is created and added to `_removedSnapshots`.
   - State: `_removedSnapshots` = `[ Snapshot_A ]`.
   - Native undo: `GH_RemoveObjectAction(A)` added to record.
   - At this point on canvas: Component A does not exist.
3. **Rollback**: A subsequent operation fails, or `dryRun: true` requests rollback:
   - `undoScope.Rollback()` is called.
   - Step A: `RollbackAddedComponents()` iterates `_addedGuids` (`[ Guid_A ]`).
     It calls `_document.FindObject(Guid_A, true)`.
     Since A was already removed from the document in Step 2, `FindObject` returns `null`. Nothing is removed.
   - Step B: `RollbackRemovedComponents()` iterates `_removedSnapshots` (`[ Snapshot_A ]`).
     Lines 208–217 deserialize A from `ArchiveBytes`.
     **Line 221**: `_document.AddObject(snapshot.Object, false);`
     **Component A is re-added to the canvas!**
   - Step C: `PopNativeUndoRecord()` clears native undo record.
4. **Final Post-Rollback State**:
   - The canvas now contains Component A!
   - Baseline state was 0 components.
   - Rollback resulted in 1 component.
   - **Conclusion**: Rollback failed to restore baseline state and created an object that never existed in the baseline document.

---

## 2. Wire Undo Mutation Timing in CanvasEditor.cs

In `CanvasEditor.cs`:
Lines 214–220 (`ConnectWires`):
```csharp
214: if (srcParam != null && tgtParam != null)
215: {
216:     tgtParam.AddSource(srcParam);
217:     undoScope.TrackConnectedWire(tgtParam, srcParam);
218:     count++;
219: }
```
Lines 244–250 (`DisconnectWires`):
```csharp
244: if (srcParam != null && tgtParam != null && tgtParam.Sources.Contains(srcParam))
245: {
246:     tgtParam.RemoveSource(srcParam);
247:     undoScope.TrackDisconnectedWire(tgtParam, srcParam);
248:     count++;
249: }
```

In `CanvasUndoScope.cs`:
```csharp
127: public void TrackConnectedWire(IGH_Param target, IGH_Param source)
128: {
129:     _connectedWires.Add((target, source));
130:     try { AddAction(new GH_WireAction(target)); }
131:     catch (Exception ex) { Log.Debug(ex, "Failed creating GH_WireAction"); }
132: }
```

### Analysis of GH_WireAction
- `GH_WireAction` is Grasshopper's native undo action for parameter wire connections.
- In Grasshopper Kernel, `new GH_WireAction(target)` snapshots the collection of sources currently attached to `target` at the moment of construction.
- When `tgtParam.AddSource(srcParam)` is called *first*, `srcParam` is already in `tgtParam.Sources` when `GH_WireAction` is constructed!
- As a consequence, `GH_WireAction` snapshots the *post-connection* state. When Grasshopper's native Undo is invoked, it restores the post-connection state rather than the pre-connection state.
- In `Milestone2ChallengerEmpiricalTests.cs` (lines 198–204), the test author correctly called `TrackDisconnectedWire` *before* `RemoveSource`, demonstrating the intended contract:
  ```csharp
  scope.TrackDisconnectedWire(target, existingSource);
  target.RemoveSource(existingSource);
  ```
- **Correction**: In `CanvasEditor.cs`, `TrackConnectedWire` and `TrackDisconnectedWire` must be called *before* mutating `tgtParam.AddSource` and `tgtParam.RemoveSource`.

---

## 3. Rollback Failure Surfacing & Cleanliness Reporting

In `CanvasUndoScope.cs`:
- Line 178: `catch (Exception ex) { Log.Debug(ex, "Failed to disconnect wire during rollback"); }`
- Line 187: `catch (Exception ex) { Log.Debug(ex, "Failed to restore wire during rollback"); }`
- Line 215: `catch (Exception ex) { Log.Debug(ex, "Failed deserializing archive bytes for {Guid}", snapshot.InstanceGuid); }`
- Line 235: `catch (Exception ex) { Log.Debug(ex, "Failed restoring wire for object {Guid}", snapshot.InstanceGuid); }`
- Line 226: `_document.AddObject(snapshot.Object, false)` throws directly, abandoning any subsequent removed components in the loop!

In `CanvasEditor.cs`:
```csharp
88: undoScope.Rollback();
89: rolledBackCleanly = true;
```
If wire restoration fails or archive deserialization fails, `CanvasEditor` still returns `rolledBack: true`.
- **Correction**:
  - `CanvasUndoScope.Rollback()` must return `bool` indicating whether 100% of the rollback actions succeeded without warnings or errors.
  - A public property `bool CleanlyRolledBack { get; }` should expose this status.
  - `RollbackRemovedComponents` should catch exceptions per component so that failure of one component does not abort the restoration of subsequent components, recording `clean = false`.
  - `CanvasEditor.cs` should assign `rolledBackCleanly = undoScope.Rollback();`.

---

## 4. Preservation of Reflection & Backward Compatibility

Existing tests rely on private fields and methods via reflection:
1. `DualSafetyRollbackTests.cs`:
   - `_addedGuids` (`List<Guid>`)
   - `_connectedWires` (`List<(IGH_Param, IGH_Param)>`)
   - `_disconnectedWires` (`List<(IGH_Param, IGH_Param)>`)
   - `_isCompleted` (`bool`)
   - `_undoRecord` (`GH_UndoRecord`)
   - Method `RollbackRemovedComponents`
   - Method `RollbackConnectedWires`
   - Method `RollbackDisconnectedWires`
2. `DualSafetyAdversarialStressTests.cs`:
   - Field `_removedSnapshots`
   - Method `RollbackWires`
3. `TestScopeFactory.CreateHeadlessScope`:
   - Initializes `_addedGuids`, `_removedSnapshots`, `_disconnectedWires`, `_connectedWires`, `_undoRecord`, `_isCompleted` using `RuntimeHelpers.GetUninitializedObject(typeof(CanvasUndoScope))`.

**Mandatory Constraint**:
All these private field names, types, and method signatures MUST remain intact to ensure 100% backward compatibility with all 534 existing tests.

---

## 5. Concrete Implementation Recommendations for Worker

### Recommendation 1: CanvasUndoScope.cs Refactoring

1. **Add Baseline Object Snapshot**:
   ```csharp
   private readonly HashSet<Guid> _baselineGuids = new();
   private readonly HashSet<Guid> _addedGuidsSet = new();
   ```
   In constructor:
   ```csharp
   CaptureBaselineObjects();
   ```
   ```csharp
   private void CaptureBaselineObjects()
   {
       try
       {
           if (_document?.Objects != null)
           {
               foreach (var obj in _document.Objects)
               {
                   if (obj != null) _baselineGuids.Add(obj.InstanceGuid);
               }
           }
       }
       catch (Exception ex)
       {
           Log.Debug(ex, "Failed capturing baseline objects from GH_Document");
       }
   }
   ```

2. **Synchronize In-Scope Added Tracking**:
   In `OnObjectsAdded` and `TrackAddedObject`:
   ```csharp
   if (!_addedGuidsSet.Contains(instanceGuid))
   {
       _addedGuids.Add(instanceGuid);
       _addedGuidsSet.Add(instanceGuid);
       ...
   }
   ```

3. **Filter Temporary Objects in TrackRemovedObject**:
   ```csharp
   public void TrackRemovedObject(IGH_DocumentObject obj)
   {
       if (obj == null) return;
       if (IsAddedInScope(obj.InstanceGuid))
       {
           Log.Debug("CanvasUndoScope: Suppressing removed snapshot for in-scope temporary object {Guid}", obj.InstanceGuid);
           return;
       }

       if (!_removedSnapshots.Any(s => s.InstanceGuid == obj.InstanceGuid))
       {
           var severedWires = CaptureObjectWires(obj);
           var archiveBytes = CaptureArchiveSnapshot(obj);
           _removedSnapshots.Add(new RemovedComponentSnapshot(obj, severedWires, archiveBytes));
           try { AddAction(new GH_RemoveObjectAction(obj)); }
           catch (Exception ex) { Log.Debug(ex, "Failed creating GH_RemoveObjectAction for {Guid}", obj.InstanceGuid); }
       }
   }

   private bool IsAddedInScope(Guid instanceGuid)
   {
       if (_addedGuidsSet.Contains(instanceGuid) || _addedGuids.Contains(instanceGuid))
           return true;

       if (_baselineGuids.Count > 0 && !_baselineGuids.Contains(instanceGuid))
           return true;

       return false;
   }
   ```

4. **Robust Rollback Reporting**:
   Change signature: `public bool Rollback()`
   Add property: `public bool CleanlyRolledBack { get; private set; } = true;`
   Update helper methods `RollbackConnectedWires`, `RollbackDisconnectedWires`, `RollbackAddedComponents`, `RollbackRemovedComponents` to return `bool clean` and catch per-element errors without aborting the loop.

5. **Separate `RemovedComponentSnapshot.cs`**:
   Extract `RemovedComponentSnapshot` into `HPGrasshopper/HPGrasshopper.McpBridge/Execution/RemovedComponentSnapshot.cs`:
   ```csharp
   namespace HPGrasshopper.McpBridge.Execution;

   using System;
   using System.Collections.Generic;
   using Grasshopper.Kernel;

   internal sealed record RemovedComponentSnapshot(
       IGH_DocumentObject Object,
       IReadOnlyList<(IGH_Param Target, IGH_Param Source)> SeveredWires,
       byte[]? ArchiveBytes)
   {
       public Guid InstanceGuid => Object.InstanceGuid;
   }
   ```
   This frees up 7 lines in `CanvasUndoScope.cs`, bringing total file size to ~270 lines (<= 300 lines limit).

### Recommendation 2: CanvasEditor.cs Wire Snapshot Inversion

In `HPGrasshopper/HPGrasshopper.McpBridge/Tools/CanvasEditor.cs`:
- In `ConnectWires` (lines 214–220):
  ```csharp
  if (srcParam != null && tgtParam != null && !tgtParam.Sources.Contains(srcParam))
  {
      undoScope.TrackConnectedWire(tgtParam, srcParam);
      tgtParam.AddSource(srcParam);
      count++;
  }
  ```
- In `DisconnectWires` (lines 244–250):
  ```csharp
  if (srcParam != null && tgtParam != null && tgtParam.Sources.Contains(srcParam))
  {
      undoScope.TrackDisconnectedWire(tgtParam, srcParam);
      tgtParam.RemoveSource(srcParam);
      count++;
  }
  ```
- In `HandleBatchException` (lines 88–89):
  ```csharp
  rolledBackCleanly = undoScope.Rollback();
  ```

---

## 6. Verification Test Plan

The Worker should add the following unit tests in `HPGrasshopper.McpBridge.Tests/Execution/CanvasUndoScopeTests.cs` (or a dedicated test file `CanvasUndoScopeLifecycleTests.cs`):

1. **`CanvasUndoScope_TemporaryObjectAddedAndDeleted_DoesNotResurrectOnRollback`**:
   - Create headless scope or test scope.
   - Track added object `idTemp`.
   - Call `TrackRemovedObject(tempObj)`.
   - Assert `_removedSnapshots` does NOT contain `idTemp`.
   - Call `Rollback()`.
   - Verify `_document.AddObject` was never called for `tempObj`.
2. **`CanvasUndoScope_BaselineObjectDeleted_IsRestoredOnRollback`**:
   - Baseline object `idBase` is present (not in `_addedGuids`).
   - Call `TrackRemovedObject(baseObj)`.
   - Assert `_removedSnapshots` contains `idBase`.
   - Call `Rollback()`.
   - Verify `baseObj` is restored.
3. **`CanvasEditor_ConnectWires_TracksWireBeforeMutation`**:
   - Verify `GH_WireAction` snapshots `tgtParam` before `AddSource`.
4. **`CanvasUndoScope_PartialRollbackFailure_SurfacesFalseCleanStatus`**:
   - Simulate a wire disconnection error during rollback.
   - Verify `Rollback()` returns `false` and `CleanlyRolledBack == false`.
