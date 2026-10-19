# Handoff Report: Investigation of Findings N8 and N9 in HPGrasshopper

- **Date**: 2026-10-13T06:31:00Z
- **Author**: Explorer Subagent (`explorer_m2_2`)
- **Recipient**: Orchestrator Sentinel (`b578e01d-ac14-4013-9369-951a857a2c11`)
- **Status**: Investigation Complete — Ready for Implementation

---

## 1. Observation

### 1.1 Finding N8: Diagnostic Loss on Revit Error & PascalCase Casing Freezing

#### 1.1.1 Diagnostic Payload Discarding in `GrasshopperExecutionEngine.cs`
In `HPGrasshopper/HPGrasshopper.McpBridge/Execution/GrasshopperExecutionEngine.cs` (lines 82–119):
```csharp
93:  if (uiapp is not null && HasActiveRevitDoc(uiapp))
94:  {
95:      var execRes = RevitTransactionScope.Execute(uiapp, request, settings, null, cancel, (doc, linkedToken, log) =>
96:      {
97:          var runResult = _playerRunner.Run(path, inputs, outputs, headless, linkedToken);
98:          var elem = JsonSerializer.SerializeToElement(runResult);
99:          if (elem.TryGetProperty("success", out var s) && !s.GetBoolean())
100:         {
101:             var err = elem.TryGetProperty("error", out var e) ? e.GetString() : "Player execution failed";
102:             throw new InvalidOperationException(err);
103:         }
104:         return runResult;
105:     }, startAutoTransaction: false);
106: 
107:     if (execRes.IsError && execRes.Value == null)
108:         execRes.Value = JsonSerializer.SerializeToElement(new { success = false, error = execRes.Message, rolledBack = execRes.RolledBack });
109: 
110:     return execRes;
111: }
```
- Direct observation: `runResult` is scoped strictly within the lambda passed to `RevitTransactionScope.Execute`.
- When Player solver encounters errors, `_playerRunner.Run(...)` returns rich diagnostics:
  - `durationMs` (elapsed solver time)
  - `inputsApplied` (count of applied parameters)
  - `rejectedInputs` (list of `PlayerInputFailure`)
  - `errors` (runtime messages from `IGH_ActiveObject.RuntimeMessages(Error)`)
  - `warnings` (runtime messages from `IGH_ActiveObject.RuntimeMessages(Warning)`)
- At lines 99–102, because `success == false`, an `InvalidOperationException` is thrown to trigger Revit transaction rollback.
- In `RevitTransactionScope.cs` (lines 162–167, 190–204), the catch block captures the exception and calls `CreateAbortedOutcome`, which returns `outcome.Value = null`.
- Back in `GrasshopperExecutionEngine.cs` at line 107, `execRes.Value` is `null`. Line 108 reconstructs a dummy object with only `{ success = false, error = execRes.Message, rolledBack = execRes.RolledBack }`.
- **Verbatim defect**: All original diagnostics (`errors`, `warnings`, `rejectedInputs`, `durationMs`, `inputsApplied`) are completely dropped. The AI receives no solver errors or rejected parameter details to guide remediation.
- In contrast, `RunBatchEditAsync` (lines 136–155) declares `object? lastBatch = null;` outside the lambda, preserving the batch result.

#### 1.1.2 PascalCase Casing Freezing in `MapToExecuteResult`
In `GrasshopperExecutionEngine.cs` (lines 186–193):
```csharp
186: internal static ExecuteResult MapToExecuteResult(object? data)
187: {
188:     if (data is null) return ExecuteResult.Failure("Execution returned null result");
189:     var element = data is JsonElement je ? je : JsonSerializer.SerializeToElement(data);
190:     var isRolledBack = element.ValueKind == JsonValueKind.Object &&
191:                        element.TryGetProperty("rolledBack", out var rbProp) &&
192:                        rbProp.ValueKind == JsonValueKind.True;
```
- Line 189 calls `JsonSerializer.SerializeToElement(data)` without any `JsonSerializerOptions`.
- In `CanvasReader.cs` (lines 16–19):
  ```csharp
  public sealed record CanvasReadResult(
      object Document,
      IReadOnlyList<object> Components,
      IReadOnlyList<object> Wires);
  ```
- Because C# record positional parameters generate PascalCase properties (`Document`, `Components`, `Wires`), default `JsonSerializer.SerializeToElement` materializes a `JsonElement` with PascalCase property names:
  `{"Document": {...}, "Components": [...], "Wires": [...]}`.
- When `GrasshopperRpcDispatcher.cs` (line 114) returns `JsonRpcEnvelope.Success(id, execResult.Value)`, and `BridgeJson.Serialize` transmits it over the pipe, `BridgeJson.Options` (with `PropertyNamingPolicy = JsonNamingPolicy.CamelCase`) **cannot alter keys of an already materialized `JsonElement`**.
- System.Text.Json preserves raw tokens of existing `JsonElement` instances verbatim. The JSON output emitted across the pipe contains `"Document"`, `"Components"`, `"Wires"` in PascalCase, directly violating public contract schemas and breaking clients (e.g. `tools/harness/live-verify.py` line 95 asserts `"document" in (r1.get("value") or r1)`).
- The same defect affects nested records:
  - `CreatedComponentInfo.cs`: `Index`, `TempId`, `InstanceGuid`, `Name`, `NickName`, `Type`
  - `CanvasBatchError.cs`: `Operation`, `Index`, `Reason`

---

### 1.2 Finding N9: Silent No-Op on Malformed Operation Containers

#### 1.2.1 Early Return Without Error in `CanvasBatchValidator.cs`
In `HPGrasshopper/HPGrasshopper.McpBridge/Tools/CanvasBatchValidator.cs`:
- Line 61:
  ```csharp
  if (!args.TryGetProperty("componentsToAdd", out var list) || list.ValueKind != JsonValueKind.Array)
      return;
  ```
- Line 107:
  ```csharp
  if (!args.TryGetProperty("componentsToRemove", out var list) || list.ValueKind != JsonValueKind.Array)
      return;
  ```
- Line 138:
  ```csharp
  if (!args.TryGetProperty("wiresToConnect", out var list) || list.ValueKind != JsonValueKind.Array)
      return;
  ```
- Line 167:
  ```csharp
  if (!args.TryGetProperty("wiresToDisconnect", out var list) || list.ValueKind != JsonValueKind.Array)
      return;
  ```
- Observation: When a caller supplies an operation container as a non-array (e.g., `componentsToAdd: { "type": "Addition" }` or `componentsToRemove: "all"`), `list.ValueKind != JsonValueKind.Array` evaluates to `true`.
- The method immediately executes `return;` without adding any `CanvasBatchError` to `errors`.
- If no other valid containers have errors, `CanvasBatchValidator.Preflight` reports `IsValid = true`.

#### 1.2.2 Silent Partial Execution in `CanvasEditor.cs`
In `HPGrasshopper/HPGrasshopper.McpBridge/Tools/CanvasEditor.cs`:
- Lines 117–118:
  ```csharp
  if (!args.TryGetProperty("componentsToAdd", out var list) || list.ValueKind != JsonValueKind.Array)
      return (0, createdList);
  ```
- Lines 180–181:
  ```csharp
  if (!args.TryGetProperty("componentsToRemove", out var list) || list.ValueKind != JsonValueKind.Array)
      return 0;
  ```
- Lines 207–208:
  ```csharp
  if (!args.TryGetProperty("wiresToConnect", out var list) || list.ValueKind != JsonValueKind.Array)
      return 0;
  ```
- Lines 236–237:
  ```csharp
  if (!args.TryGetProperty("wiresToDisconnect", out var list) || list.ValueKind != JsonValueKind.Array)
      return 0;
  ```
- **Trigger Scenario**: A client sends a batch edit request containing:
  ```json
  {
    "componentsToAdd": { "type": "Addition", "x": 100, "y": 200 },
    "componentsToRemove": ["d8f8a1e2-3b4c-5d6e-7f8a-9b0c1d2e3f4a"]
  }
  ```
  1. `Preflight` runs: `componentsToAdd` is skipped (0 errors). `componentsToRemove` is valid (0 errors). `Preflight` passes!
  2. `CanvasEditor.ExecuteBatch` runs `ApplyMutations`.
  3. `AddComponents` encounters `list.ValueKind == JsonValueKind.Object` and returns `(0, empty)` with 0 components added.
  4. `RemoveComponents` runs and removes the component.
  5. The canvas solution updates and commits. The tool returns `success: true`!
- **Verbatim defect**: The malformed container was silently skipped without error, while the other operations committed. The outcome contradicts user intent and breaks atomicity.

#### 1.2.3 Array Item Kind Validation Flaw
- Inside the loops of `ValidateComponentsToAdd`, `ValidateWiresToConnect`, and `ValidateWiresToDisconnect`:
  If an array item is a primitive or array rather than a JSON object, calling `item.TryGetProperty(...)` throws an unhandled `System.InvalidOperationException: Cannot get property on JsonValueKind.Number/String`.
- In `ValidateComponentsToRemove`:
  If an item is an object rather than a string, calling `item.GetString()` throws `System.InvalidOperationException: Cannot get string from JsonValueKind.Object`.
- These unhandled exceptions cause ungraceful crashes instead of structured preflight errors.

---

## 2. Logic Chain

### 2.1 Logic Chain for Finding N8
1. **Fact (from Obs 1.1.1)**: `RunPlayerAsync` executes `_playerRunner.Run` inside a lambda passed to `RevitTransactionScope.Execute`.
2. **Fact (from Obs 1.1.1)**: When `_playerRunner.Run` returns `success == false`, the lambda throws `InvalidOperationException` so that `RevitTransactionScope` can catch it and abort any uncommitted Revit transactions.
3. **Fact (from Obs 1.1.1)**: `RevitTransactionScope` catches the exception and returns `ExecuteResult` with `Value = null`.
4. **Fact (from Obs 1.1.1)**: Because `runResult` was declared as a local variable inside the lambda, the outer method has no reference to it. Line 108 replaces `Value` with a generic object containing only `success`, `error`, and `rolledBack`.
5. **Inference**: Declaring `object? lastRun = null;` outside the lambda allows the outer scope to retain the original return value from `_playerRunner.Run` across the exception boundary.
6. **Fact (from Obs 1.1.2)**: System.Text.Json never alters property names of an existing `JsonElement` upon re-serialization.
7. **Fact (from Obs 1.1.2)**: `MapToExecuteResult` calls `JsonSerializer.SerializeToElement(data)` without options. C# record DTOs (`CanvasReadResult`, `CreatedComponentInfo`, `CanvasBatchError`) have PascalCase properties.
8. **Inference**: `MapToExecuteResult` and all serialization points in `GrasshopperExecutionEngine.cs` must use `BridgeJson.Options` (`PropertyNamingPolicy = JsonNamingPolicy.CamelCase`) or `BridgeJson.ToElement(data)` when converting objects to `JsonElement`.
9. **Conclusion**: Capturing `lastRun` outside the lambda, merging it with `rolledBack: execRes.RolledBack`, and serializing via `BridgeJson.ToElement` fully preserves the diagnostic payload (`errors`, `warnings`, `rejectedInputs`, `durationMs`) and enforces camelCase JSON across all public tool outputs.

### 2.2 Logic Chain for Finding N9
1. **Fact (from Obs 1.2.1)**: An operation container property in `batch_edit_grasshopper_canvas` can have three distinct states:
   - *Absent* (`!args.TryGetProperty`): User did not request this operation. Valid.
   - *Null / Undefined* (`prop.ValueKind == Null || Undefined`): User passed explicit null. Valid.
   - *Array* (`prop.ValueKind == Array`): User provided operation array. Valid.
   - *Other Kind* (`prop.ValueKind == Object, String, Number, Boolean`): Malformed user input. Invalid.
2. **Fact (from Obs 1.2.1)**: `CanvasBatchValidator` currently groups "not present" and "present but not array" together with `!args.TryGetProperty(...) || list.ValueKind != JsonValueKind.Array`, returning early for both.
3. **Fact (from Obs 1.2.2)**: `CanvasEditor.ApplyMutations` does the same, leading to silent omission of malformed containers while committing valid ones.
4. **Inference**: Schema validation must distinguish between *absent/null* (no-op) and *present but not an array* (preflight validation error).
5. **Inference**: A centralized validator helper `TryGetContainerArray(args, propName, errors, out array)` can inspect `prop.ValueKind`:
   - If absent or null: returns `false` with 0 errors.
   - If not an array: adds `CanvasBatchError(propName, -1, $"Property '{propName}' must be a JSON array if provided, but found '{prop.ValueKind}'.")` and returns `false`.
   - If array: outputs `array` and returns `true`.
6. **Inference**: Array items must also be validated: `componentsToAdd`, `wiresToConnect`, and `wiresToDisconnect` require `JsonValueKind.Object`; `componentsToRemove` requires `JsonValueKind.String`.
7. **Inference**: Defense-in-depth in `CanvasEditor.cs`: if a malformed container is supplied to `AddComponents`, `RemoveComponents`, `ConnectWires`, or `DisconnectWires`, it must throw an `InvalidOperationException` rather than silently returning 0, ensuring that any unvalidated execution fails fast and triggers rollback.
8. **Conclusion**: Preflight validation will reject mixed valid + wrong-container requests before mutation, preserving batch atomicity and reporting clear error messages.

---

## 3. Caveats

1. **Clean Code Line Ceiling (Rule C2: Class <= 300 lines)**:
   - `GrasshopperExecutionEngine.cs` currently has 293 lines.
   - `CanvasEditor.cs` currently has 295 lines.
   - `CanvasBatchValidator.cs` currently has 284 lines.
   Any proposed fix must NOT simply append lines. It must streamline existing verbose blocks to guarantee all three files remain well under the 300-line ceiling (target: <= 285 lines each).
2. **Rule M3 (Method <= 50 lines)**:
   - `ExecuteBatch`, `Preflight`, `RunPlayerAsync`, and `RunBatchEditAsync` must each remain under 50 lines.
3. **McpShared Immutability**:
   - `McpShared/` must have 0 edits. All fixes reside entirely within `HPGrasshopper.McpBridge`.
4. **No Direct Execution on Real Rhino/Revit**:
   - Unit and mock tests run headlessly. Native Rhino/Revit host execution requires live bridge attachment.

---

## 4. Conclusion & Proposed Implementation Plan

### 4.1 Concrete Signatures and Design for N8

#### File: `HPGrasshopper/HPGrasshopper.McpBridge/Execution/GrasshopperExecutionEngine.cs`

1. **Add Helper Method `MergeOutcomeValue`**:
```csharp
internal static JsonElement MergeOutcomeValue(object? payload, bool rolledBack, string? fallbackError)
{
    var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    if (payload != null)
    {
        var elem = payload is JsonElement je ? je : (BridgeJson.ToElement(payload) ?? default);
        if (elem.ValueKind == JsonValueKind.Object)
            foreach (var prop in elem.EnumerateObject()) dict[prop.Name] = prop.Value;
    }
    if (!dict.ContainsKey("success")) dict["success"] = false;
    if (!dict.ContainsKey("error") && !string.IsNullOrEmpty(fallbackError)) dict["error"] = fallbackError;
    dict["rolledBack"] = rolledBack;
    return BridgeJson.ToElement(dict) ?? default;
}
```

2. **Update `RunPlayerAsync` (lines 93–111)**:
```csharp
if (uiapp is not null && HasActiveRevitDoc(uiapp))
{
    object? lastRun = null;
    var execRes = RevitTransactionScope.Execute(uiapp, request, settings, null, cancel, (doc, linkedToken, log) =>
    {
        lastRun = _playerRunner.Run(path, inputs, outputs, headless, linkedToken);
        var elem = BridgeJson.ToElement(lastRun) ?? default;
        if (elem.TryGetProperty("success", out var s) && !s.GetBoolean())
        {
            var err = elem.TryGetProperty("error", out var e) ? e.GetString() : "Player execution failed";
            throw new InvalidOperationException(err);
        }
        return lastRun;
    }, startAutoTransaction: false);

    if (execRes.IsError)
        execRes.Value = MergeOutcomeValue(lastRun, execRes.RolledBack, execRes.Message);

    return execRes;
}

var standaloneResult = _playerRunner.Run(path, inputs, outputs, headless, cancel.Token);
return MapToExecuteResult(standaloneResult);
```

3. **Update `RunBatchEditAsync` (lines 149–155)**:
```csharp
    if (execRes.IsError)
        execRes.Value = MergeOutcomeValue(lastBatch, execRes.RolledBack, execRes.Message);

    return execRes;
```

4. **Update `MapToExecuteResult` (lines 186–212)**:
```csharp
internal static ExecuteResult MapToExecuteResult(object? data)
{
    if (data is null) return ExecuteResult.Failure("Execution returned null result");
    var element = data is JsonElement je ? je : (BridgeJson.ToElement(data) ?? default);
    var isRolledBack = element.ValueKind == JsonValueKind.Object &&
                       element.TryGetProperty("rolledBack", out var rbProp) &&
                       rbProp.ValueKind == JsonValueKind.True;

    if (element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty("success", out var successProp) &&
        successProp.ValueKind == JsonValueKind.False)
    {
        var errorMessage = element.TryGetProperty("error", out var errProp) ? errProp.GetString() : null;
        if (string.IsNullOrEmpty(errorMessage) && element.TryGetProperty("rollbackErrors", out var rbErr) && rbErr.ValueKind == JsonValueKind.Array)
        {
            var errs = rbErr.EnumerateArray().Select(x => x.GetString()).Where(s => !string.IsNullOrEmpty(s));
            var joined = string.Join("; ", errs);
            if (!string.IsNullOrEmpty(joined)) errorMessage = $"Canvas rollback failed: {joined}";
        }
        return new ExecuteResult { IsError = true, Message = errorMessage ?? "Operation failed", Value = element, RolledBack = isRolledBack };
    }
    return new ExecuteResult { Value = element, RolledBack = isRolledBack };
}
```

5. **Update Remaining `SerializeToElement` Calls in `GrasshopperExecutionEngine.cs`**:
- Line 131: `var batchArgs = request.Args ?? BridgeJson.ToElement(new object()) ?? default;`
- Line 281: In `MergeDryRun`, return `BridgeJson.ToElement(dict) ?? default;`

---

### 4.2 Concrete Signatures and Design for N9

#### File: `HPGrasshopper/HPGrasshopper.McpBridge/Tools/CanvasBatchValidator.cs`

1. **Add `TryGetContainerArray` Helper**:
```csharp
private static bool TryGetContainerArray(
    JsonElement args, string propName, List<CanvasBatchError> errors, out JsonElement array)
{
    array = default;
    if (!args.TryGetProperty(propName, out var prop) || prop.ValueKind == JsonValueKind.Null || prop.ValueKind == JsonValueKind.Undefined)
        return false;

    if (prop.ValueKind != JsonValueKind.Array)
    {
        errors.Add(new CanvasBatchError(propName, -1, $"Property '{propName}' must be a JSON array if provided, but found '{prop.ValueKind}'."));
        return false;
    }

    array = prop;
    return true;
}
```

2. **Update `Preflight` (lines 26–45)**:
```csharp
public static CanvasBatchPreflightResult Preflight(
    JsonElement args,
    Func<Guid, IGH_DocumentObject?>? findObject = null,
    Func<string, bool>? canCreateObject = null,
    Func<string, (int Inputs, int Outputs)?>? getPortBounds = null)
{
    var errors = new List<CanvasBatchError>();
    if (args.ValueKind != JsonValueKind.Object && args.ValueKind != JsonValueKind.Undefined)
    {
        errors.Add(new CanvasBatchError("batch", -1, $"Batch arguments must be a JSON object, but found '{args.ValueKind}'."));
        return CanvasBatchPreflightResult.Failure(errors);
    }

    var definedTempIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var tempIdToType = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    var removedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    ValidateComponentsToAdd(args, errors, definedTempIds, tempIdToType, canCreateObject);
    ValidateComponentsToRemove(findObject, args, errors, removedIds);
    ValidateWiresToConnect(findObject, args, errors, definedTempIds, tempIdToType, removedIds, getPortBounds);
    ValidateWiresToDisconnect(findObject, args, errors, removedIds, getPortBounds);

    return errors.Count == 0
        ? CanvasBatchPreflightResult.Success()
        : CanvasBatchPreflightResult.Failure(errors);
}
```

3. **Update Container Validation Methods**:
- `ValidateComponentsToAdd`:
  ```csharp
  if (!TryGetContainerArray(args, "componentsToAdd", errors, out var list)) return;
  var index = 0;
  foreach (var item in list.EnumerateArray())
  {
      if (item.ValueKind != JsonValueKind.Object)
      {
          errors.Add(new CanvasBatchError("componentsToAdd", index++, $"Item at index {index} must be a JSON object, but found '{item.ValueKind}'."));
          continue;
      }
      ...
  ```
- `ValidateComponentsToRemove`:
  ```csharp
  if (!TryGetContainerArray(args, "componentsToRemove", errors, out var list)) return;
  var index = 0;
  foreach (var item in list.EnumerateArray())
  {
      if (item.ValueKind != JsonValueKind.String)
      {
          errors.Add(new CanvasBatchError("componentsToRemove", index++, $"Item at index {index} must be a GUID string, but found '{item.ValueKind}'."));
          continue;
      }
      ...
  ```
- `ValidateWiresToConnect`:
  ```csharp
  if (!TryGetContainerArray(args, "wiresToConnect", errors, out var list)) return;
  var index = 0;
  foreach (var item in list.EnumerateArray())
  {
      if (item.ValueKind != JsonValueKind.Object)
      {
          errors.Add(new CanvasBatchError("wiresToConnect", index++, $"Item at index {index} must be a JSON object, but found '{item.ValueKind}'."));
          continue;
      }
      ...
  ```
- `ValidateWiresToDisconnect`:
  ```csharp
  if (!TryGetContainerArray(args, "wiresToDisconnect", errors, out var list)) return;
  var index = 0;
  foreach (var item in list.EnumerateArray())
  {
      if (item.ValueKind != JsonValueKind.Object)
      {
          errors.Add(new CanvasBatchError("wiresToDisconnect", index++, $"Item at index {index} must be a JSON object, but found '{item.ValueKind}'."));
          continue;
      }
      ...
  ```

#### File: `HPGrasshopper/HPGrasshopper.McpBridge/Tools/CanvasEditor.cs`

1. **Add Defense-in-Depth Checks in Mutation Methods**:
- `AddComponents`:
  ```csharp
  if (!args.TryGetProperty("componentsToAdd", out var list) || list.ValueKind == JsonValueKind.Null || list.ValueKind == JsonValueKind.Undefined)
      return (0, createdList);
  if (list.ValueKind != JsonValueKind.Array)
      throw new InvalidOperationException($"Property 'componentsToAdd' must be a JSON array, but found '{list.ValueKind}'.");
  ```
- `RemoveComponents`:
  ```csharp
  if (!args.TryGetProperty("componentsToRemove", out var list) || list.ValueKind == JsonValueKind.Null || list.ValueKind == JsonValueKind.Undefined)
      return 0;
  if (list.ValueKind != JsonValueKind.Array)
      throw new InvalidOperationException($"Property 'componentsToRemove' must be a JSON array, but found '{list.ValueKind}'.");
  ```
- `ConnectWires`:
  ```csharp
  if (!args.TryGetProperty("wiresToConnect", out var list) || list.ValueKind == JsonValueKind.Null || list.ValueKind == JsonValueKind.Undefined)
      return 0;
  if (list.ValueKind != JsonValueKind.Array)
      throw new InvalidOperationException($"Property 'wiresToConnect' must be a JSON array, but found '{list.ValueKind}'.");
  ```
- `DisconnectWires`:
  ```csharp
  if (!args.TryGetProperty("wiresToDisconnect", out var list) || list.ValueKind == JsonValueKind.Null || list.ValueKind == JsonValueKind.Undefined)
      return 0;
  if (list.ValueKind != JsonValueKind.Array)
      throw new InvalidOperationException($"Property 'wiresToDisconnect' must be a JSON array, but found '{list.ValueKind}'.");
  ```

2. **Streamline `CanvasEditor.cs` to Maintain Line Limits**:
- Condense lines 138–141, 186–196, and 270–277.
- Current lines: 295. Projected lines after stream-lining: ~275 lines (well within Rule C2 <= 300).

---

## 5. Verification Method

### 5.1 Automated Unit / Regression Tests to Create

#### For N8:
1. `ExecutionEngine_PlayerErrorInRevit_PreservesDiagnostics`:
   - Simulate a Player execution error in Revit with `errors = ["Solve failed"]`, `warnings = ["Warning 1"]`, `rejectedInputs = [...]`, `durationMs = 50`.
   - Assert `execRes.IsError == true`.
   - Assert `execRes.Value` contains `errors`, `warnings`, `rejectedInputs`, `durationMs`, and `rolledBack == true`.
2. `MapToExecuteResult_CanvasReadResult_MaterializesInCamelCase`:
   - Pass `CanvasReader.CanvasReadResult` to `MapToExecuteResult`.
   - Assert `execRes.Value.TryGetProperty("document", out _)` is true.
   - Assert `execRes.Value.TryGetProperty("components", out _)` is true.
   - Assert `execRes.Value.TryGetProperty("wires", out _)` is true.
   - Assert PascalCase keys (`Document`, `Components`, `Wires`) do not exist.
3. `CreatedComponentInfo_MaterializesInCamelCase`:
   - Assert properties are `index`, `tempId`, `instanceGuid`, `name`, `nickName`, `type`.

#### For N9:
1. `Preflight_MalformedContainer_ObjectInsteadOfArray_ReturnsValidationError`:
   - Pass `{"componentsToAdd": { "type": "Addition" }}` to `CanvasBatchValidator.Preflight`.
   - Assert `preflight.IsValid == false`.
   - Assert `preflight.Errors.Any(e => e.Operation == "componentsToAdd" && e.Reason.Contains("must be a JSON array"))`.
2. `Preflight_MalformedContainer_MixedValidAndWrongContainer_RejectsBatch`:
   - Pass `{"componentsToAdd": { "type": "Addition" }, "componentsToRemove": ["00000000-0000-0000-0000-000000000001"]}`.
   - Assert `preflight.IsValid == false`.
   - Assert batch is rejected without committing any component removal.
3. `Preflight_AbsentOrNullContainers_AreValid`:
   - Pass `{}` and `{"componentsToAdd": null}`.
   - Assert `preflight.IsValid == true` with 0 errors.
4. `Preflight_PrimitiveItemsInContainer_ReturnsValidationError`:
   - Pass `{"componentsToAdd": [123, "string"]}`.
   - Assert `preflight.IsValid == false` with descriptive item type errors (no unhandled exception thrown).

### 5.2 Commands to Run
```powershell
dotnet test HPGrasshopper/HPGrasshopper.McpBridge.Tests/HPGrasshopper.McpBridge.Tests.csproj -c Debug -p:DeployAddin=false
dotnet test HPGrasshopper/HPGrasshopper.Mcp.Server.Tests/HPGrasshopper.Mcp.Server.Tests.csproj -c Debug -p:DeployAddin=false
dotnet test HPGrasshopper/HPGrasshopper.E2E.Tests/HPGrasshopper.E2E.Tests.csproj -c Debug -p:DeployAddin=false
```

### 5.3 Invalidation Conditions
- Any test failure in the 965 existing tests across `HPGrasshopper`.
- Any modification to `McpShared/` (must remain 0 edits).
- Line count of `GrasshopperExecutionEngine.cs`, `CanvasEditor.cs`, or `CanvasBatchValidator.cs` exceeding 300 lines (Rule C2).
- Any method exceeding 50 lines (Rule M3).
