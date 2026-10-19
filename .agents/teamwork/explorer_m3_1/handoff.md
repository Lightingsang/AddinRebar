# Handoff Report — Finding R6: Canvas Batch Preflight & Atomic Operation Plan

## 1. Observation

Direct code examination of `HPGrasshopper.McpBridge` reveals four critical vulnerability sites:

1. **`HPGrasshopper/HPGrasshopper.McpBridge/Tools/CanvasBatchValidator.cs:195–202`**:
   ```csharp
   if (prop.ValueKind != JsonValueKind.Number || !prop.TryGetInt32(out var port))
   {
       errors.Add(new CanvasBatchError(operation, index, $"{propName} port index must be a valid integer."));
   }
   else if (port < 0)
   {
       errors.Add(new CanvasBatchError(operation, index, $"{propName} port index must be non-negative."));
   }
   ```
   - Only checks whether `port < 0`.
   - Upper bounds are completely unchecked: `sourceOutput = 999` or `sourceOutput = 2147483647` passes preflight validation without error.
   - The validation function operates in total isolation from the referenced source/target components and ignores actual port counts.

2. **`HPGrasshopper/HPGrasshopper.McpBridge/Tools/CanvasEditor.cs:273–278` and `:212–221`**:
   ```csharp
   // Lines 273-278 (ResolveParam)
   if (targetObject is IGH_Component comp)
   {
       var ports = isInput ? comp.Params.Input : comp.Params.Output;
       if (portIndex >= 0 && portIndex < ports.Count) return ports[portIndex];
   }
   return null;

   // Lines 212-221 (ConnectWires)
   var srcParam = ResolveParam(doc, sourceId, tempMap, false, sourcePort);
   var tgtParam = ResolveParam(doc, targetId, tempMap, true, targetPort);

   if (srcParam != null && tgtParam != null)
   {
       undoScope.TrackConnectedWire(tgtParam, srcParam);
       tgtParam.AddSource(srcParam);
       count++;
   }
   ```
   - When `portIndex >= ports.Count` (e.g., `sourcePort = 999`), `ResolveParam` returns `null`.
   - `ConnectWires` checks `if (srcParam != null && tgtParam != null)`: when `srcParam` is `null`, it **silently skips** the wire connection.
   - No exception is thrown, no error is recorded, and no rollback is triggered.
   - The same silent skip occurs in `DisconnectWires` (line 245).

3. **`HPGrasshopper/HPGrasshopper.McpBridge/Tools/CanvasEditor.cs:72–85`**:
   ```csharp
   private static object CreateBatchSuccess(
       bool dryRun, bool rolledBack,
       (int added, int removed, int connected, int disconnected, List<CreatedComponentInfo> created) m,
       IReadOnlyList<string>? rollbackErrors = null) =>
       new
       {
           success = !dryRun || rolledBack,
           dryRun,
           rolledBack,
           added = m.added, removed = m.removed, connected = m.connected, disconnected = m.disconnected,
           createdComponents = m.created,
           errors = Array.Empty<CanvasBatchError>(),
           rollbackErrors = rollbackErrors ?? Array.Empty<string>()
       };
   ```
   - `CreateBatchSuccess` unconditionally computes `success = !dryRun || rolledBack`.
   - When wire connection is silently skipped due to out-of-range ports or deleted components, `CreateBatchSuccess` reports `success = true`, `connected = 0`, and `errors = []` to the caller.

4. **`HPGrasshopper/HPGrasshopper.McpBridge/Tools/CanvasEditor.cs:117–122`**:
   ```csharp
   var compObject = CreateObject(typeStr);
   if (compObject == null)
   {
       index++;
       continue;
   }
   ```
   - When `CreateObject(typeStr)` fails (e.g. unknown plugin or catalog proxy emission failure), it executes `index++; continue;`, silently dropping the component creation without error or rollback.

5. **`HPGrasshopper/HPGrasshopper.McpBridge/Tools/CanvasBatchValidator.cs:97–151`**:
   - `ValidateComponentsToRemove` records GUIDs to remove, but does not track them into a shared set for wire validation.
   - `ValidateWiresToConnect` and `ValidateWiresToDisconnect` never check whether `source` or `target` is present in `componentsToRemove`.
   - If a batch requests removing `GuidA` and simultaneously wiring `GuidA`, preflight passes. Then during `ApplyMutations`, `RemoveComponents` deletes `GuidA`, causing `ConnectWires` to fail silently, reporting `success = true`.

6. **File Size Metrics**:
   - `CanvasBatchValidator.cs`: 206 lines (Rule C2 limit <= 300).
   - `CanvasEditor.cs`: 286 lines (Rule C2 limit <= 300; headroom = 14 lines).
   - `HPGrasshopper.McpBridge.Tests`: currently 563 tests passing (verified via `dotnet test`).

---

## 2. Logic Chain

1. **Premise 1 (Preflight Blindness)**: Because `CanvasBatchValidator.ValidatePortIndex` only asserts `port >= 0` without consulting component parameter counts (Obs 1), out-of-range indices (such as 999) pass preflight validation unchecked.
2. **Premise 2 (Conflicting Operations Blindness)**: Because `CanvasBatchValidator` does not cross-reference `componentsToRemove` against `wiresToConnect`/`wiresToDisconnect` (Obs 5), conflicting batches pass preflight validation unchecked.
3. **Premise 3 (Silent Execution Skipping)**: During execution in `CanvasEditor`, any out-of-range port or deleted component results in `ResolveParam` returning `null`, which `ConnectWires` silently ignores without throwing an exception or aborting the batch (Obs 2). Similarly, failed component creation is silently skipped (Obs 4).
4. **Premise 4 (False Success Reporting)**: Because no exception is thrown, execution reaches `undoScope.Commit()` and returns `CreateBatchSuccess` with `success: true` (Obs 3).
5. **Deduction (Atomicity Violation)**: A batch that is partially executed or contradictory is marked successful. This violates the atomic contract of `batch_edit_grasshopper_canvas` and misleads LLM callers into assuming all requested wires were connected.
6. **Remediation Requirement (Two-Tier Defense)**:
   - **Tier 1 (Preflight)**: Reject out-of-range ports and conflicting component deletions before any mutations are performed.
   - **Tier 2 (Execution)**: If any component fails to create or any wire fails to resolve during execution, throw an explicit exception to trigger `undoScope.Rollback()`, guaranteeing atomic failure with clean rollback.

---

## 3. Caveats

1. **Dynamic Parameter Components**: In Grasshopper, some components (such as ZUI components like Merge, Entwine, or script components) allow users to dynamically add/remove parameters at runtime. However, in batch execution, initial component proxies and standard component definitions instantiate with their default parameter sets (e.g. `Addition` has 2 inputs, 1 output). Validating against default bounds at preflight is strictly correct.
2. **Empty Test Stubs in Unit Tests**: Some unit test doubles (e.g. `dummyComp = TestProxyFactory.CreateComponent("Dummy", "D")` in `CanvasPortAdversarialTests.cs:164`) instantiate `TestComponentProxy` with empty input/output lists (`Count == 0`). Bounds checking must distinguish between an unpopulated test stub and a populated component to avoid breaking existing synthetic tests.
3. **Line Count Budget (Rule C2)**: `CanvasEditor.cs` currently has 286 lines. Adding validation logic directly would breach the 300-line ceiling. Moving `CreatedComponentInfo` record (14 lines) to `CreatedComponentInfo.cs` restores 14 lines of headroom, allowing `CanvasEditor.cs` to comfortably stay under 285 lines.

---

## 4. Conclusion & Concrete Strategy for Worker

### Target Files to Modify / Create

1. **Extract `CreatedComponentInfo.cs`** (NEW file, ~15 lines):
   - Path: `HPGrasshopper/HPGrasshopper.McpBridge/Tools/CreatedComponentInfo.cs`
   - Namespace: `HPGrasshopper.McpBridge.Tools`
   - Content: Extract the `CreatedComponentInfo` record from `CanvasEditor.cs:12–13`.

2. **Upgrade `CanvasBatchValidator.cs`** (~245 lines, <= 300):
   - **Track `componentsToRemove`**:
     In `ValidateComponentsToRemove`, populate a `HashSet<string> removedIds = new(StringComparer.OrdinalIgnoreCase);` containing both the string format and `"D"` format of every GUID to be removed.
   - **Detect Conflicting Deletions in Wires**:
     In `ValidateWiresToConnect` and `ValidateWiresToDisconnect`:
     If `source` or `target` is contained in `removedIds`, emit error:
     `new CanvasBatchError("wiresToConnect", index, $"Source component '{sourceId}' is marked for removal in componentsToRemove.");`
   - **Track Created Component Types**:
     In `ValidateComponentsToAdd`, collect `Dictionary<string, string> tempIdToType = new(StringComparer.OrdinalIgnoreCase);`.
   - **Add Port Bounds Validation Overload**:
     Update `Preflight` signature with optional `Func<string, (int Inputs, int Outputs)?>? getPortBounds = null`:
     ```csharp
     public static CanvasBatchPreflightResult Preflight(
         JsonElement args,
         Func<Guid, IGH_DocumentObject?>? findObject = null,
         Func<string, bool>? canCreateObject = null,
         Func<string, (int Inputs, int Outputs)?>? getPortBounds = null)
     ```
     Provide matching overload taking `GH_Document? doc`.
   - **Validate Actual Port Bounds**:
     In `ValidateWirePortBounds`:
     * For created components (`tempIdToType.TryGetValue(id, out var typeStr)`):
       If `getPortBounds != null`: query `var bounds = getPortBounds(typeStr);`.
       If `bounds.HasValue`: verify `port < (isInput ? bounds.Value.Inputs : bounds.Value.Outputs)`. If exceeded, emit out-of-range error.
     * For existing canvas components (`Guid.TryParse(id, out var guid) && findObject != null`):
       Look up `findObject(guid)`.
       If `obj is IGH_Component comp`:
       Query `var ports = isInput ? comp.Params.Input : comp.Params.Output;`.
       If `ports != null && ports.Count > 0 && port >= ports.Count`: emit out-of-range error.
       If `obj is IGH_Param param`:
       If `port > 0`: emit out-of-range error (`parameters only have port index 0`).

3. **Upgrade `CanvasEditor.cs`** (~275 lines, <= 300):
   - **Wire `GetPortBounds` to Preflight**:
     Implement static helper `GetPortBounds(string typeStr)`:
     ```csharp
     private static (int Inputs, int Outputs)? GetPortBounds(string typeStr)
     {
         var obj = CreateObject(typeStr);
         if (obj is IGH_Component comp && comp.Params != null)
             return (comp.Params.Input?.Count ?? 0, comp.Params.Output?.Count ?? 0);
         if (obj is IGH_Param)
             return (1, 1);
         return null;
     }
     ```
     Pass `GetPortBounds` into `CanvasBatchValidator.Preflight(doc, args, CanCreateObject, GetPortBounds);`.
   - **Fail-Fast Component Creation (`AddComponents`)**:
     Replace line 118–122:
     ```csharp
     var compObject = CreateObject(typeStr);
     if (compObject == null)
     {
         throw new InvalidOperationException($"Failed to create component of type '{typeStr}' at index {index}.");
     }
     ```
   - **Fail-Fast Wire Connection (`ConnectWires`)**:
     Replace silent skipping in lines 215–221:
     ```csharp
     var srcParam = ResolveParam(doc, sourceId, tempMap, false, sourcePort)
         ?? throw new InvalidOperationException($"Cannot connect wire: source component '{sourceId}' output port {sourcePort} is invalid or out of range.");
     var tgtParam = ResolveParam(doc, targetId, tempMap, true, targetPort)
         ?? throw new InvalidOperationException($"Cannot connect wire: target component '{targetId}' input port {targetPort} is invalid or out of range.");

     undoScope.TrackConnectedWire(tgtParam, srcParam);
     tgtParam.AddSource(srcParam);
     count++;
     ```
   - **Fail-Fast Wire Disconnection (`DisconnectWires`)**:
     Ensure `srcParam == null` or `tgtParam == null` throws `InvalidOperationException` instead of silently ignoring invalid port indices.
   - **Strict `ResolveParam` for Parameters**:
     Ensure `IGH_Param` only resolves when `portIndex == 0`; if `portIndex > 0`, return `null`.

4. **Add Comprehensive Unit Tests in `HPGrasshopper.McpBridge.Tests`**:
   - `Preflight_WireToRemovedComponent_FailsValidation`
   - `Preflight_OutOfRangePort_ExistingComponent_FailsValidation`
   - `Preflight_OutOfRangePort_CreatedComponent_FailsValidation`
   - `Preflight_OutOfRangePort_StandaloneParam_FailsValidation`
   - `ExecuteBatch_InvalidWirePort_TriggersRollbackAndFailsAtomically`
   - `ExecuteBatch_FailedComponentCreation_TriggersRollbackAndFailsAtomically`

---

## 5. Verification Method

1. **Compilation Check**:
   ```powershell
   dotnet build HPGrasshopper/HPGrasshopper.slnx -c Debug -p:DeployAddin=false
   ```
   Must succeed with 0 errors and 0 warnings.

2. **Clean Code Rule Compliance**:
   - `CanvasBatchValidator.cs` <= 300 lines (Rule C2).
   - `CanvasEditor.cs` <= 300 lines (Rule C2).
   - Every method <= 50 lines (Rule M3).
   - Zero commented-out code (Rule Q-B1).
   - Zero unannotated empty catch blocks (Rule Q-B2).
   - Zero modifications in `McpShared/`.

3. **Test Execution**:
   ```powershell
   dotnet test HPGrasshopper/HPGrasshopper.McpBridge.Tests/HPGrasshopper.McpBridge.Tests.csproj -c Debug -p:DeployAddin=false
   ```
   All existing 563 tests plus new Finding R6 preflight and atomic rollback tests must pass 100%.

4. **Invalidation Conditions**:
   - Any silent skipping of wires when `sourceOutput = 999`.
   - Any passing preflight when a wire connects to a component listed in `componentsToRemove`.
   - Any partial commit of canvas mutations when a wire connection fails.
