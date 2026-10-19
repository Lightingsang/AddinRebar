# Investigation Report: HPGrasshopper Findings N6 and N7

**Author**: `explorer_m2_1` (Teamwork Explorer subagent)  
**Date**: 2026-10-13  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_1\`  
**Target Codebase**: `HPGrasshopper/HPGrasshopper.McpBridge/`  
**Milestone**: M2 (Priority P2 Findings N6 and N7)  

---

## 1. Observation

### 1.1 Finding N6 Observations: Player Input Data Assignment Verification (`PlayerDataTreeHelper.cs` & `PlayerRunner.cs`)

Direct code inspection of `HPGrasshopper/HPGrasshopper.McpBridge/Tools/PlayerDataTreeHelper.cs`:

1. **Ignored `AddVolatileData` return values**:
   - `PlayerDataTreeHelper.cs:18–22`:
     ```csharp
     if (val.ValueKind == JsonValueKind.Number && val.TryGetDouble(out var numValue))
     {
         param.AddVolatileData(new GH_Path(0), 0, new GH_Number(numValue));
         return true;
     }
     ```
     The McNeel Grasshopper API defines `bool IGH_Param.AddVolatileData(GH_Path path, int index, IGH_Goo data)`. Here, the boolean return value of `AddVolatileData` is ignored and the method unconditionally returns `true`.
   - `PlayerDataTreeHelper.cs:23–27`:
     ```csharp
     if (val.ValueKind is JsonValueKind.True or JsonValueKind.False)
     {
         param.AddVolatileData(new GH_Path(0), 0, new GH_Boolean(val.GetBoolean()));
         return true;
     }
     ```
     Boolean return is ignored, unconditionally returns `true`.
   - `PlayerDataTreeHelper.cs:28–32`:
     ```csharp
     if (val.ValueKind == JsonValueKind.String)
     {
         param.AddVolatileData(new GH_Path(0), 0, new GH_String(val.GetString()));
         return true;
     }
     ```
     Boolean return is ignored, unconditionally returns `true`.
   - `PlayerDataTreeHelper.cs:39–43`:
     ```csharp
     if (TryParsePoint(val, out var pt))
     {
         param.AddVolatileData(new GH_Path(0), 0, new GH_Point(pt));
         return true;
     }
     ```
     Boolean return is ignored, unconditionally returns `true`.
   - `PlayerDataTreeHelper.cs:56–60`:
     ```csharp
     if (IsPointTarget(param) && TryParsePoint(arrayElement, out var pt))
     {
         param.AddVolatileData(new GH_Path(0), 0, new GH_Point(pt));
         return true;
     }
     ```
     Boolean return is ignored, unconditionally returns `true`.
   - `PlayerDataTreeHelper.cs:63–70`:
     ```csharp
     int itemIndex = 0;
     foreach (var element in arrayElement.EnumerateArray())
     {
         var gooInstance = ConvertJsonToGoo(element, param);
         if (gooInstance != null)
         {
             param.AddVolatileData(new GH_Path(0), itemIndex++, gooInstance);
         }
     }
     return itemIndex > 0;
     ```
     Line 68: `param.AddVolatileData(...)` return is ignored. If `AddVolatileData` fails/rejects the goo, `itemIndex` was already post-incremented, and line 71 returns `true` (`itemIndex > 0`).
   - `PlayerDataTreeHelper.cs:91–100`:
     ```csharp
     int indexInBranch = 0;
     foreach (var branchItem in branchProperty.Value.EnumerateArray())
     {
         var gooInstance = ConvertJsonToGoo(branchItem, param);
         if (gooInstance != null)
         {
             param.AddVolatileData(branchPath, indexInBranch++, gooInstance);
             itemsAppended = true;
         }
     }
     ```
     Line 97: `param.AddVolatileData(...)` return is ignored, `itemsAppended` is set to `true` unconditionally.
   - `PlayerDataTreeHelper.cs:104–110`:
     ```csharp
     var gooInstance = ConvertJsonToGoo(branchProperty.Value, param);
     if (gooInstance != null)
     {
         param.AddVolatileData(branchPath, 0, gooInstance);
         itemsAppended = true;
     }
     ```
     Line 107: `param.AddVolatileData(...)` return is ignored, `itemsAppended` is set to `true` unconditionally.

2. **Tree Mixed Valid / Malformed Branch Silent Dropping**:
   - `PlayerDataTreeHelper.cs:81–87, 112`:
     ```csharp
     foreach (var branchProperty in objectElement.EnumerateObject())
     {
         if (!TryParsePath(branchProperty.Name, out var branchPath))
         {
             Log.Warning("PlayerDataTreeHelper: Malformed branch path '{Path}' skipped", branchProperty.Name);
             continue;
         }
         ...
     }
     return itemsAppended;
     ```
     When a payload contains both a valid branch (e.g. `"{0}": [1.0]`) and a malformed branch (e.g. `"{invalid;branch}": [2.0]`), the malformed branch only emits a warning log and continues. Since the valid branch appended items, `itemsAppended` is `true`. `SetTreeParamValue` returns `true`, and caller `PlayerRunner` increments `inputsApplied++` with zero entries in `rejectedInputs`.

3. **Index Shifting (Index Dồn) on Null or Unsupported Items**:
   - `PlayerDataTreeHelper.cs:62–71` and `91–100`:
     When `ConvertJsonToGoo` returns `null` (because an array item is a `null` JSON literal or an unsupported object), the item is simply skipped by `if (gooInstance != null)`. Because `itemIndex++` (or `indexInBranch++`) is inside the `if` block, `itemIndex` is not incremented for the skipped null/unsupported item.
     For example, given `[10.0, null, 20.0]`, item 0 (10.0) is placed at index 0, item 1 (null) is skipped, and item 2 (20.0) is placed at index 1. The list becomes `[10.0, 20.0]` of length 2 instead of length 3, corrupting list matching and component index assumptions downstream.

4. **Premature Mutation in `PlayerRunner.ApplyInputs`**:
   - `PlayerRunner.cs:199–224`:
     ```csharp
     foreach (var prop in inputs.Value.EnumerateObject())
     {
         var targetParam = FindInputParam(doc.Objects, prop.Name, g => doc.FindObject(g, true));
         if (targetParam == null)
         {
             rejected.Add(new PlayerInputFailure(prop.Name, $"Parameter '{prop.Name}' was not found in the definition."));
             continue;
         }

         try
         {
             targetParam.ClearData();
             if (SetParamValue(targetParam, prop.Value))
             {
                 targetParam.ExpireSolution(false);
                 applied++;
             }
             else
                 rejected.Add(new PlayerInputFailure(prop.Name, "Failed setting parameter value: incompatible type or structure."));
         }
         catch (Exception ex)
         {
             rejected.Add(new PlayerInputFailure(prop.Name, $"Exception during assignment: {ex.Message}"));
         }
     }
     ```
     `targetParam.ClearData()` and parameter mutation happen eagerly in the loop before all inputs are validated. If input 1 succeeds and input 2 fails, input 1 has already mutated the document, and `appliedCount` is 1 when `BuildRejectedInputsResult` returns `success: false`.

---

### 1.2 Finding N7 Observations: Requested Output Duplicate Nickname Overwriting (`PlayerOutputExtractor.cs`)

Direct code inspection of `HPGrasshopper/HPGrasshopper.McpBridge/Tools/PlayerOutputExtractor.cs`:

- `PlayerOutputExtractor.cs:50–65`:
  ```csharp
  private static void AddOutputResult(
      Dictionary<string, object?> result,
      IGH_Param param,
      string? matchedKey)
  {
      var defaultKey = result.ContainsKey(param.NickName)
          ? $"{param.NickName}#{param.InstanceGuid.ToString("D")[..8]}"
          : param.NickName;

      var keyToUse = !string.IsNullOrEmpty(matchedKey) && !result.ContainsKey(matchedKey)
          ? matchedKey
          : defaultKey;

      var extractedVal = ExtractParamData(param);
      result[keyToUse] = extractedVal;

      if (!string.IsNullOrEmpty(matchedKey) && !string.Equals(matchedKey, keyToUse, StringComparison.OrdinalIgnoreCase))
      {
          result[matchedKey] = extractedVal;
      }
  }
  ```

Trace of execution with two output parameters having `NickName = "R"` (param1 with Guid `11111111-...`, value `A`; param2 with Guid `22222222-...`, value `B`) and filter `requestedOutputs = ["R"]`:
1. `param1`:
   - `MatchesOutput` sets `matchedKey = "R"`.
   - `result.ContainsKey("R")` is `false`.
   - `defaultKey = "R"`.
   - `keyToUse = "R"`.
   - `result["R"] = A`.
   - Line 61: `string.Equals("R", "R")` is `true`, so line 63 is not run.
   - Result state: `result["R"] = A`.
2. `param2`:
   - `MatchesOutput` sets `matchedKey = "R"`.
   - `result.ContainsKey("R")` is `true`.
   - `defaultKey = "R#22222222"`.
   - `keyToUse = defaultKey = "R#22222222"` (because `result.ContainsKey("R")` is `true`).
   - Line 59: `result["R#22222222"] = B`.
   - Line 61: `matchedKey` ("R") does NOT equal `keyToUse` ("R#22222222").
   - Line 63: `result[matchedKey] = extractedVal;` -> executes `result["R"] = B;`.
3. Final state in `result`:
   - `result["R"] = B`
   - `result["R#22222222"] = B`
   - Value `A` is completely overwritten and lost! Neither `result["R"]` has `A`, nor is `A` present under any other key (e.g. `R#11111111`).

---

## 2. Logic Chain

### 2.1 Logic Chain for Finding N6

```
[Observation 1.1.1]
8 calls to param.AddVolatileData ignore boolean return value; SetParamValue returns true unconditionally.
      │
      ▼
[Inference 1]
If Grasshopper rejects data (type mismatch, invalid Goo conversion, locked parameter), PlayerDataTreeHelper falsely reports success.
      │
      ▼
[Observation 1.1.4]
PlayerRunner.ApplyInputs increments applied++ based solely on SetParamValue return; rejectedInputs is empty.
      │
      ▼
[Inference 2]
PlayerRunner runs doc.NewSolution(true) with unpopulated or stale parameter data, producing silent calculation errors.

[Observation 1.1.2]
Tree with mixed valid and malformed branch skips invalid branch and returns true if any item was added.
      │
      ▼
[Inference 3]
Partial tree payloads drop invalid branches silently. No structured failure is added to rejectedInputs.

[Observation 1.1.3]
ConvertJsonToGoo returns null for null/unsupported items; itemIndex++ is skipped inside if (gooInstance != null).
      │
      ▼
[Inference 4]
Indices are shifted (index dồn) when arrays contain nulls, altering list lengths and breaking data pairing in downstream nodes.

[Observation 1.1.4]
doc parameters are cleared and modified in a single loop before all inputs are validated.
      │
      ▼
[Inference 5]
Partial mutations corrupt the document state even when subsequent inputs fail and execution is aborted.
```

### 2.2 Logic Chain for Finding N7

```
[Observation 1.2]
AddOutputResult unconditionally assigns result[matchedKey] = extractedVal when matchedKey != keyToUse.
      │
      ▼
[Inference 1]
When multiple parameters match the same requested alias (e.g. NickName "R"), the first parameter's value A was stored at result["R"].
      │
      ▼
[Inference 2]
When the second parameter arrives, its disambiguated key is "R#22222222", but line 63 overwrites result["R"] = B.
      │
      ▼
[Inference 3]
Value A is irreversibly erased from result["R"]. The caller receiving outputs gets B twice (under "R" and "R#22222222"), and A is lost.
```

---

## 3. Caveats

1. **Unfiltered Outputs Compatibility**:
   In unfiltered mode (`requestedOutputs == null`), `matchedKey` is `null`, and line 61 does not run. The existing test `PlayerOutputExtractor_DisambiguatesDuplicateNicknames` asserts that `outputs.Count == 2`, with `"Result"` and `"Result#22222222"`. Any fix must preserve this count and behavior when `requestedOutputs == null` so existing unit tests do not break.
2. **Grasshopper Parameter Constraints**:
   Grasshopper parameters running in-process via Revit/Rhino.Inside can enforce custom casting or reject nulls depending on parameter implementation. Staging inputs before mutation ensures that malformed inputs are caught in .NET before Grasshopper memory structures are touched.
3. **McpShared Boundary**:
   Neither `PlayerDataTreeHelper` nor `PlayerOutputExtractor` is in `McpShared/`. `McpShared/` remains strictly immutable (0 edits).

---

## 4. Conclusion

1. **Finding N6**:
   - `PlayerDataTreeHelper.cs` must be refactored to introduce a two-phase pipeline:
     - **Phase 1: Staging & Validation** (`TryStageParamValue`): Parses and validates all items (numbers, booleans, strings, points, arrays, trees). Rejects malformed branch paths and null/unsupported array items upfront, preventing index shifting.
     - **Phase 2: Atomic Application** (`ApplyStagedItems`): Clears parameter data and calls `AddVolatileData`, strictly checking the boolean return value of every call. If any call returns `false`, application fails with a detailed diagnostic.
   - `PlayerRunner.ApplyInputs` must stage and validate all inputs across all requested properties before mutating any document parameter. If any input fails staging, return `appliedCount = 0` with complete `rejectedInputs` diagnostics without touching `GH_Document`.

2. **Finding N7**:
   - In `PlayerOutputExtractor.AddOutputResult`, line 63 must NOT overwrite existing values in `result[matchedKey]`.
   - When multiple outputs match the same `matchedKey`:
     - Disambiguate each output with its unique qualified key (`$"{NickName}#{Guid[..8]}"`).
     - Backfill the first output's qualified key so both outputs are accessible individually.
     - For the shared alias `matchedKey`, convert it into a multi-value list (`List<object?> { val1, val2, ... }`), ensuring zero data loss whether querying by alias or by unique qualified key.

---

## 5. Proposed Fix & Implementation Specifications

### 5.1 Proposed Changes to `PlayerDataTreeHelper.cs`

**Target File**: `HPGrasshopper/HPGrasshopper.McpBridge/Tools/PlayerDataTreeHelper.cs`

#### Concrete Signatures:
```csharp
internal readonly record struct StagedItem(GH_Path Path, int Index, IGH_Goo Goo);

public static bool TryStageParamValue(
    IGH_Param param,
    JsonElement val,
    out List<StagedItem>? stagedItems,
    out string? failureReason);

public static bool ApplyStagedItems(
    IGH_Param param,
    IReadOnlyList<StagedItem> stagedItems,
    out string? failureReason);

public static bool SetParamValue(IGH_Param param, JsonElement val);
public static bool SetParamValue(IGH_Param param, JsonElement val, out string? failureReason);
```

#### Detailed Logic:
1. **`TryStageParamValue`**:
   - Handles scalars (`Number`, `True`/`False`, `String`): creates a single `StagedItem(new GH_Path(0), 0, goo)`.
   - Handles `Point`:
     - `TryParsePoint(val, out var pt)` -> `new StagedItem(new GH_Path(0), 0, new GH_Point(pt))`.
   - Handles `Array`:
     - If `IsPointTarget(param) && TryParsePoint(val, out var pt)` -> stage single point.
     - Else: iterates elements with index `idx`. If `elem.ValueKind == JsonValueKind.Null`, fails immediately with `$"Array item at index {idx} is null; null items are not supported."` (prevents index shifting!). If `ConvertJsonToGoo` returns `null`, fails immediately with `$"Array item at index {idx} could not be converted ({elem.ValueKind})."`.
     - Ensures `list.Count > 0`.
   - Handles `Object` (DataTree):
     - Validates `hasDataTreeKeys`. If false, fails with `"Object is neither a 3D Point nor a DataTree."`.
     - Iterates branches:
       - If `!TryParsePath(branchProp.Name, out var branchPath)`: fails immediately with `$"Malformed branch path '{branchProp.Name}'; paths must match '{{index;...}}' format."` (no silent skipping!).
       - If branch value is Array: iterates items with index `bIdx`. Rejects null or unconverted items immediately with detailed index diagnostic.
       - If branch value is scalar: rejects null or unconverted item immediately.
     - Ensures `list.Count > 0`.
2. **`ApplyStagedItems`**:
   - `param.ClearData();`
   - For each `item` in `stagedItems`:
     - `bool accepted = param.AddVolatileData(item.Path, item.Index, item.Goo);`
     - If `!accepted`:
       `failureReason = $"Parameter '{param.NickName}' rejected data of type '{item.Goo.TypeName}' at path {item.Path}[{item.Index}]."; return false;`
   - Returns `true`.
3. **`SetParamValue(param, val, out failureReason)`**:
   - Calls `TryStageParamValue`. If false, returns false.
   - Calls `ApplyStagedItems`. If false, returns false.
   - Preserves `SetParamValue(IGH_Param, JsonElement)` overload returning `bool` for 100% test compatibility.

### 5.2 Proposed Changes to `PlayerRunner.cs`

**Target File**: `HPGrasshopper/HPGrasshopper.McpBridge/Tools/PlayerRunner.cs`

#### Method: `ApplyInputs(GH_Document doc, JsonElement? inputs)`
Replace lines 191–225 with a clean 2-phase workflow:
```csharp
private static (int appliedCount, List<PlayerInputFailure> rejectedInputs) ApplyInputs(GH_Document doc, JsonElement? inputs)
{
    if (!inputs.HasValue || inputs.Value.ValueKind != JsonValueKind.Object)
        return (0, new List<PlayerInputFailure>());

    var rejected = new List<PlayerInputFailure>();
    var staged = new List<(IGH_Param Param, string Name, IReadOnlyList<PlayerDataTreeHelper.StagedItem> Items)>();

    // Phase 1: Stage and validate all inputs before any mutation
    foreach (var prop in inputs.Value.EnumerateObject())
    {
        var targetParam = FindInputParam(doc.Objects, prop.Name, g => doc.FindObject(g, true));
        if (targetParam == null)
        {
            rejected.Add(new PlayerInputFailure(prop.Name, $"Parameter '{prop.Name}' was not found in the definition."));
            continue;
        }

        if (!PlayerDataTreeHelper.TryStageParamValue(targetParam, prop.Value, out var stagedItems, out var failureReason))
        {
            rejected.Add(new PlayerInputFailure(prop.Name, failureReason ?? "Failed staging parameter value: incompatible type or structure."));
            continue;
        }

        staged.Add((targetParam, prop.Name, stagedItems!));
    }

    // Stop if any input failed validation: zero mutations applied
    if (rejected.Count > 0)
        return (0, rejected);

    // Phase 2: Apply staged items and check AddVolatileData return
    var applied = 0;
    foreach (var (param, name, items) in staged)
    {
        try
        {
            if (PlayerDataTreeHelper.ApplyStagedItems(param, items, out var applyError))
            {
                param.ExpireSolution(false);
                applied++;
            }
            else
            {
                rejected.Add(new PlayerInputFailure(name, applyError ?? "Parameter rejected input data."));
            }
        }
        catch (Exception ex)
        {
            rejected.Add(new PlayerInputFailure(name, $"Exception during assignment: {ex.Message}"));
        }
    }

    return (applied, rejected);
}
```

### 5.3 Proposed Changes to `PlayerOutputExtractor.cs`

**Target File**: `HPGrasshopper/HPGrasshopper.McpBridge/Tools/PlayerOutputExtractor.cs`

#### Method: `AddOutputResult`
Replace lines 45–65:
```csharp
private static void AddOutputResult(
    Dictionary<string, object?> result,
    Dictionary<string, (IGH_Param Param, object? Value)> initialMatchedParams,
    IGH_Param param,
    string? matchedKey)
{
    var guidSuffix = param.InstanceGuid.ToString("D")[..8];
    var uniqueKey = $"{param.NickName}#{guidSuffix}";
    var defaultKey = result.ContainsKey(param.NickName) ? uniqueKey : param.NickName;

    var extractedVal = ExtractParamData(param);

    if (string.IsNullOrEmpty(matchedKey))
    {
        result[defaultKey] = extractedVal;
        return;
    }

    if (!result.ContainsKey(matchedKey))
    {
        // First parameter matching this requested key
        result[matchedKey] = extractedVal;
        initialMatchedParams[matchedKey] = (param, extractedVal);
    }
    else
    {
        // Second or subsequent parameter matching this requested key (collision)
        // 1. Backfill the first parameter's qualified key if not yet present
        if (initialMatchedParams.TryGetValue(matchedKey, out var firstMatch))
        {
            var firstUniqueKey = $"{firstMatch.Param.NickName}#{firstMatch.Param.InstanceGuid.ToString("D")[..8]}";
            result[firstUniqueKey] = firstMatch.Value;
            initialMatchedParams.Remove(matchedKey);
        }

        // 2. Add current parameter under its unique key
        result[uniqueKey] = extractedVal;

        // 3. Aggregate all matching outputs under the requested alias as a multi-value list
        if (result[matchedKey] is List<object?> list)
        {
            list.Add(extractedVal);
        }
        else
        {
            result[matchedKey] = new List<object?> { result[matchedKey], extractedVal };
        }
    }
}
```
And in `ExtractOutputs`: pass `initialMatchedParams` dictionary (`new Dictionary<string, (IGH_Param Param, object? Value)>(StringComparer.OrdinalIgnoreCase)`).

#### Trace with duplicate nickname "R" (values A and B, requestedOutputs=["R"]):
1. Param 1 (A):
   `result["R"] = A`.
   `initialMatchedParams["R"] = (param1, A)`.
2. Param 2 (B):
   `initialMatchedParams` found!
   `result["R#11111111"] = A`.
   `result["R#22222222"] = B`.
   `result["R"] = new List<object?> { A, B }`.
Result:
- `result["R"]` contains `[A, B]` (both values preserved!).
- `result["R#11111111"]` contains `A`.
- `result["R#22222222"]` contains `B`.
- Zero values overwritten or lost!

### 5.4 Clean Code Governance Compliance
- **Rule C2 (Class <= 300 lines)**:
  - `PlayerDataTreeHelper.cs`: Currently 255 lines -> Estimated ~270 lines (<= 300).
  - `PlayerRunner.cs`: Currently 277 lines -> Estimated ~285 lines (<= 300).
  - `PlayerOutputExtractor.cs`: Currently 161 lines -> Estimated ~185 lines (<= 300).
- **Rule M3 (Method <= 50 lines)**:
  - `TryStageParamValue`: ~45 lines (<= 50).
  - `ApplyStagedItems`: ~18 lines (<= 50).
  - `ApplyInputs`: ~44 lines (<= 50).
  - `AddOutputResult`: ~38 lines (<= 50).
- **Rule Q-B1**: Zero dead/commented-out code.
- **Rule Q-B2**: Zero unannotated empty catch blocks.
- **McpShared**: Strictly immutable (0 edits).

---

## 6. Verification Method

1. **Unit Tests to Add / Verify in `HPGrasshopper.McpBridge.Tests`**:
   - `PlayerDataTreeHelper_AddVolatileDataFails_ReturnsFalse`:
     Configure proxy with `AddVolatileDataValidator` returning false; verify `SetParamValue` returns false.
   - `PlayerDataTreeHelper_TreeWithMalformedBranch_FailsAndReportsBranchError`:
     Pass `{"{0}": [1.0], "{invalid;branch}": [2.0]}`; verify `TryStageParamValue` returns false with malformed path reason.
   - `PlayerDataTreeHelper_ArrayWithNull_FailsAndPreventsIndexShifting`:
     Pass `[1.0, null, 2.0]`; verify `TryStageParamValue` returns false without shifting indices.
   - `PlayerRunner_WhenOneInputMalformed_StagesZeroMutations`:
     Pass one valid input and one input with malformed tree branch; verify `appliedCount == 0`, `rejectedInputs` contains exact diagnostic, and document parameters remain untouched.
   - `PlayerOutputExtractor_DuplicateNicknamesInRequestedOutputs_PreservesAllOutputs`:
     Create 2 params with NickName "R" (values 100.0 and 200.0), request `requestedOutputs = ["R"]`. Verify `outputs["R"]` is `List<object?>` with both 100.0 and 200.0, `outputs["R#11111111"]` is 100.0, and `outputs["R#22222222"]` is 200.0.
   - `PlayerOutputExtractor_UnfilteredDuplicateNicknames_PreservesCount2`:
     Run with `requestedOutputs = null`; verify `outputs.Count == 2`, containing "R" and "R#22222222" (zero regressions on existing test).

2. **Automated Verification Commands**:
   ```powershell
   dotnet test HPGrasshopper.McpBridge.Tests/HPGrasshopper.McpBridge.Tests.csproj -c Debug -p:DeployAddin=false
   dotnet test HPGrasshopper.Mcp.Server.Tests/HPGrasshopper.Mcp.Server.Tests.csproj -c Debug -p:DeployAddin=false
   dotnet test HPGrasshopper.E2E.Tests/HPGrasshopper.E2E.Tests.csproj -c Debug -p:DeployAddin=false
   ```
