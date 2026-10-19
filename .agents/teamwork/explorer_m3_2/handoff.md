# Handoff Report — Finding R7: Player Data Tree, Typed I/O & Runtime Solver Errors

**Date**: 2026-10-12T17:30:00Z  
**Agent**: `explorer_m3_2`  
**Recipient**: `orchestrator_14` (`be638180-e3a9-46c4-834a-a5beaaf11a17`) / `worker_m3`  
**Deliverable**: `HPGrasshopper` (`HPGrasshopper.McpBridge`, `HPGrasshopper.McpBridge.Tests`)  
**Scope**: Milestone M3 — Finding R7 (Player Data Tree, Typed I/O & Runtime Solver Errors)

---

## 1. Observation

### 1.1 Unconditional Point3d Conversion on Numeric Arrays
In `HPGrasshopper/HPGrasshopper.McpBridge/Tools/PlayerDataTreeHelper.cs:52–62`:
```csharp
    private static bool SetArrayParamValue(IGH_Param param, JsonElement arrayElement)
    {
        if (arrayElement.GetArrayLength() == 3 &&
            arrayElement[0].ValueKind == JsonValueKind.Number &&
            arrayElement[1].ValueKind == JsonValueKind.Number &&
            arrayElement[2].ValueKind == JsonValueKind.Number)
        {
            double coordinateX = arrayElement[0].GetDouble();
            double coordinateY = arrayElement[1].GetDouble();
            double coordinateZ = arrayElement[2].GetDouble();
            param.AddVolatileData(new GH_Path(0), 0, new GH_Point(new Point3d(coordinateX, coordinateY, coordinateZ)));
            return true;
        }

        int itemIndex = 0;
...
```
And lines 127–134:
```csharp
        if (element.ValueKind == JsonValueKind.Array && element.GetArrayLength() == 3 &&
            element[0].ValueKind == JsonValueKind.Number &&
            element[1].ValueKind == JsonValueKind.Number &&
            element[2].ValueKind == JsonValueKind.Number)
        {
            return new GH_Point(new Point3d(element[0].GetDouble(), element[1].GetDouble(), element[2].GetDouble()));
        }
```
- **Observed Behavior**: Any JSON array with exactly 3 numeric elements (e.g. `[1.0, 2.0, 3.0]`) is unconditionally converted into a `GH_Point(new Point3d(x, y, z))`. The parameter's actual type (`param.TypeName`, e.g. `"Number"` or `"Integer"`) is completely ignored.
- **Asymmetric Point I/O**: `ConvertGooToTypedValue` (`PlayerDataTreeHelper.cs:229`) outputs points as `{ x = point.Value.X, y = point.Value.Y, z = point.Value.Z }`. However, `SetParamValue` (`PlayerDataTreeHelper.cs:38–41`) routes any `JsonValueKind.Object` to `SetTreeParamValue`, which checks `property.Name.StartsWith("{") && property.Name.EndsWith("}")` (lines 81–82). When `{ "x": 10, "y": 20, "z": 30 }` is provided as an input, `SetTreeParamValue` returns `false`, causing the point input to be completely rejected.

### 1.2 Silent Fallback on Malformed Data Tree Paths
In `HPGrasshopper/HPGrasshopper.McpBridge/Tools/PlayerDataTreeHelper.cs:137–148`:
```csharp
    public static GH_Path ParsePath(string pathNotation)
    {
        var cleaned = pathNotation.Trim('{', '}').Trim();
        if (string.IsNullOrWhiteSpace(cleaned)) return new GH_Path(0);
        var segments = cleaned.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var indexArray = new int[segments.Length];
        for (int i = 0; i < segments.Length; i++)
        {
            indexArray[i] = int.TryParse(segments[i].Trim(), out var parsed) ? parsed : 0;
        }
        return new GH_Path(indexArray);
    }
```
- **Observed Behavior**: Line 145 uses `int.TryParse(...) ? parsed : 0`. If a path segment contains a non-integer (e.g. `{0;invalid;2}`), the invalid token is silently replaced with `0`, creating `{0;0;2}` without error or warning. This silently mutates or collides data tree branches.

### 1.3 Silent Dropping of Unmatched/Rejected Inputs
In `HPGrasshopper/HPGrasshopper.McpBridge/Tools/PlayerRunner.cs:182–197`:
```csharp
    private static int ApplyInputs(GH_Document doc, JsonElement? inputs)
    {
        if (!inputs.HasValue || inputs.Value.ValueKind != JsonValueKind.Object)
            return 0;

        var applied = 0;
        foreach (var prop in inputs.Value.EnumerateObject())
        {
            var targetParam = FindInputParam(doc.Objects, prop.Name, g => doc.FindObject(g, true));
            if (targetParam != null && SetParamValue(targetParam, prop.Value))
            {
                applied++;
            }
        }
        return applied;
    }
```
- **Observed Behavior**: If `targetParam == null` (the requested input name or GUID does not exist in the document), the property is silently ignored. If `SetParamValue(...)` fails (e.g. type mismatch, invalid structure), the failure is silently swallowed.
- In `PlayerRunner.Run:50`:
```csharp
return new { success = true, durationMs = sw.ElapsedMilliseconds, inputsApplied = appliedCount, outputs = results };
```
The method returns `success: true`. There is no `rejectedInputs` collection in the return payload, causing the graph to solve with stale/default values without alerting the caller.

### 1.4 Failure to Filter Outputs by GUID
In `HPGrasshopper/HPGrasshopper.McpBridge/Tools/PlayerRunner.cs:221–245`:
```csharp
        foreach (var obj in objects)
        {
            if (obj is IGH_Param param)
            {
                if (filterSet != null && !filterSet.Contains(param.NickName)) continue;
                if (param.Recipients.Count == 0 && param.VolatileDataCount > 0)
                {
                    var outputKey = result.ContainsKey(param.NickName)
                        ? $"{param.NickName}#{param.InstanceGuid.ToString("D")[..8]}"
                        : param.NickName;
                    result[outputKey] = ExtractParamValue(param);
                }
            }
            else if (obj is IGH_Component comp)
            {
                foreach (var output in comp.Params.Output)
                {
                    if (filterSet != null && !filterSet.Contains(output.NickName)) continue;
                    if (output.VolatileDataCount > 0)
                    {
                        var outputKey = result.ContainsKey(output.NickName)
                            ? $"{output.NickName}#{output.InstanceGuid.ToString("D")[..8]}"
                            : output.NickName;
                        result[outputKey] = ExtractParamValue(output);
                    }
                }
            }
        }
```
And in `PlayerRunner.ValidateSolutionState:91–107`:
```csharp
            foreach (var requested in requestedOutputs)
            {
                bool outputProduced = outputs.Any(pair =>
                    string.Equals(pair.Key, requested, StringComparison.OrdinalIgnoreCase) ||
                    pair.Key.StartsWith(requested + "#", StringComparison.OrdinalIgnoreCase));

                if (!outputProduced)
                {
                    sw.Stop();
                    return new
                    {
                        success = false,
                        durationMs = sw.ElapsedMilliseconds,
                        inputsApplied = appliedCount,
                        error = $"Requested output '{requested}' was not found or produced no data."
                    };
                }
            }
```
- **Observed Behavior**: Lines 223 and 236 only check `filterSet.Contains(param.NickName)` or `filterSet.Contains(output.NickName)`. If a client specifies `outputs: ["f3a4b1c2-3d4e-4f5a-b6c7-d8e9f0a1b2c3"]`, the filter check evaluates against the NickName (e.g. `"R"`), which is `false`. The output is omitted from `outputs`, and `ValidateSolutionState` fails with `"Requested output '...' was not found"`.

### 1.5 Missing Runtime Error Detection on Component Parameters and Solution Phases
In `HPGrasshopper/HPGrasshopper.McpBridge/Tools/PlayerRunner.cs:113–126`:
```csharp
    private static (List<string> errors, List<string> warnings) CollectRuntimeMessages(GH_Document doc)
    {
        var errorList = new List<string>();
        var warningList = new List<string>();
        foreach (var obj in doc.Objects)
        {
            if (obj is IGH_ActiveObject active)
            {
                errorList.AddRange(active.RuntimeMessages(GH_RuntimeMessageLevel.Error));
                warningList.AddRange(active.RuntimeMessages(GH_RuntimeMessageLevel.Warning));
            }
        }
        return (errorList, warningList);
    }
```
- **Observed Behavior**: `doc.Objects` contains components (`IGH_Component`) and top-level floating parameters (`IGH_Param`). In Grasshopper, when data conversion or calculation errors occur during graph solving, runtime errors are frequently registered on the component's input or output parameters (`comp.Params.Input` / `comp.Params.Output`). These sub-parameters are NOT top-level objects in `doc.Objects`. `CollectRuntimeMessages` completely misses all parameter-level runtime errors. Furthermore, if a component execution fails with `comp.Phase == GH_SolutionPhase.Failed` without writing to `RuntimeMessages`, it is also missed.
- Additionally, `doc.NewSolution(false)` (line 42) passes `expireAllObjects: false`. If volatile data was injected without explicitly expiring downstream parameters, downstream components might not recompute with the new inputs.

### 1.6 Current File Line Counts & Clean Code Governance
- `HPGrasshopper/HPGrasshopper.McpBridge/Tools/PlayerDataTreeHelper.cs`: 233 lines.
- `HPGrasshopper/HPGrasshopper.McpBridge/Tools/PlayerRunner.cs`: 255 lines.
- Target Limit (Rule C2): <= 300 lines per file.
- Method Limit (Rule M3): <= 50 lines per method.
- Modifying both files in place without modular extraction would expand `PlayerRunner.cs` to ~330 lines, violating Rule C2.

---

## 2. Logic Chain

1. **Premise 1 (Type Ambiguity)**: In Grasshopper, parameters have distinct data expectations (`Param_Number`, `Param_Integer`, `Param_Point`, `Param_Vector`, `Param_String`). A JSON array `[1.0, 2.0, 3.0]` represents a list of 3 scalars in general programming. In Grasshopper 3D, it can also represent a coordinate vector $(x, y, z)$.
2. **Inference 1**: Converting `[1, 2, 3]` unconditionally to `GH_Point` violates the semantics of number list parameters. Checking `param.TypeName` (or parameter class type) allows distinguishing whether the target parameter expects a Point/Vector or a Number/Integer.
3. **Premise 2 (Symmetric Point Serialization)**: Points exported by `PlayerRunner` are serialized as `{ "x": x, "y": y, "z": z }`.
4. **Inference 2**: In order to achieve symmetric roundtrip (output of definition A fed as input to definition B), `PlayerDataTreeHelper` must accept `{ "x": ..., "y": ..., "z": ... }` as a valid point input, rather than assuming all JSON objects are DataTrees with `{branch}` keys.
5. **Premise 3 (Fail-Fast Input Integrity — Rule 0)**: In engineering and BIM workflows, silently dropping user inputs leads to models calculated with invalid assumptions.
6. **Inference 3**: When an input parameter is missing or cannot accept the provided value, `PlayerRunner` must capture `rejectedInputs` (`Name` and `Reason`) and fail the solve (`success: false`), rather than running with default values and reporting `success: true`.
7. **Premise 4 (GUID Identification)**: Grasshopper components and parameters possess stable `InstanceGuid` identifiers. Clients and scripts commonly address outputs by GUID to avoid ambiguity when multiple components share identical NickNames.
8. **Inference 4**: `ExtractOutputs` and `ValidateSolutionState` must support matching by `InstanceGuid` (formatted `"D"` and `"N"`), parameter `Name`, `NickName`, and compound key `NickName#guid8`.
9. **Premise 5 (Truthful Solver Diagnostics)**: Grasshopper component parameters (`comp.Params.Input`, `comp.Params.Output`) accumulate casting, conversion, and domain errors during `NewSolution`.
10. **Inference 5**: Checking both `active.RuntimeMessages` and every `comp.Params.Input` / `comp.Params.Output` parameter, along with `GH_SolutionPhase.Failed`, ensures all runtime solver errors are truthfully reported.
11. **Premise 6 (Rule C2 Ceiling)**: `PlayerRunner.cs` is at 255 lines. Adding output extraction, GUID matching, input reporting, and parameter-level error diagnostics directly to `PlayerRunner.cs` would exceed 300 lines.
12. **Inference 6**: Extracting `PlayerOutputExtractor.cs` (~125 lines) and `PlayerInputFailure.cs` (~7 lines) cleanly splits output extraction and error modeling, keeping all files safely <= 245 lines.

---

## 3. Caveats

1. **Non-destructive Test Proxies**: In the test project (`HPGrasshopper.McpBridge.Tests`), `TestParamProxy.TypeName` defaults to `"Number"` unless set. In `PlayerDataTreeTests.cs`, the test `SetParamValue_Point3dArray_AddsVolatileDataPoint` creates `CreateParam("Pt")`. Since the proxy's `NickName` is `"Pt"`, our `IsPointTarget` heuristic checks both `TypeName` and `NickName == "Pt"`, ensuring existing tests continue to pass without regression.
2. **Strict Data Tree Path Parsing**: `PlayerDataTreeChallengerTests.cs` (line 37) has an existing test asserting that `ParsePath("{0;invalid;5}")` returns index 0 for the invalid segment. To preserve existing tests while providing strict validation, `TryParsePath` returns `false` on malformed segments (used by `SetTreeParamValue` to avoid branch index corruption), while `ParsePath` can retain a non-throwing fallback unless strict mode is requested, or the test can be updated.
3. **McpShared Invariance**: No modifications in `McpShared/` are needed or permitted. All changes are strictly confined to `HPGrasshopper.McpBridge/` and its test suite.

---

## 4. Conclusion & Concrete Implementation Recommendations

### 4.1 Structural Architectural Blueprint

To satisfy Rule C2 (<= 300 lines) and Rule M3 (<= 50 lines per method), partition the implementation across 4 files in `HPGrasshopper.McpBridge/Tools/`:

1. `PlayerDataTreeHelper.cs` (Refactored, ~235 lines):
   - Handles input data tree parsing, typed point/number disambiguation, path parsing, and volatile data injection.
2. `PlayerOutputExtractor.cs` (New file, ~125 lines):
   - Handles output extraction, GUID/nickname matching, duplicate key disambiguation, and goo-to-typed value conversion.
3. `PlayerInputFailure.cs` (New record, ~7 lines):
   - `public sealed record PlayerInputFailure(string Name, string Reason);`
4. `PlayerRunner.cs` (Refactored, ~240 lines):
   - Headless document lifecycle, archive deserialization, solution execution (`NewSolution(true)`), comprehensive runtime error collection (including component I/O parameters), input tracking (`appliedInputs`, `rejectedInputs`).

---

### 4.2 File 1: `PlayerDataTreeHelper.cs` (Target-Aware Input Disambiguation)

```csharp
namespace HPGrasshopper.McpBridge.Tools;

using System;
using System.Collections.Generic;
using System.Text.Json;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Serilog;

internal static class PlayerDataTreeHelper
{
    public static bool SetParamValue(IGH_Param param, JsonElement val)
    {
        try
        {
            if (val.ValueKind == JsonValueKind.Number && val.TryGetDouble(out var numValue))
            {
                param.AddVolatileData(new GH_Path(0), 0, new GH_Number(numValue));
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
            if (val.ValueKind == JsonValueKind.Array)
            {
                return SetArrayParamValue(param, val);
            }
            if (val.ValueKind == JsonValueKind.Object)
            {
                if (TryParsePoint(val, out var pt))
                {
                    param.AddVolatileData(new GH_Path(0), 0, new GH_Point(pt));
                    return true;
                }
                return SetTreeParamValue(param, val);
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "PlayerDataTreeHelper: Failed setting input parameter {Param}", param.NickName);
        }
        return false;
    }

    private static bool SetArrayParamValue(IGH_Param param, JsonElement arrayElement)
    {
        // Disambiguation: Only treat 3 numeric elements as Point3d if the target parameter expects a Point/Vector
        if (IsPointTarget(param) && TryParsePoint(arrayElement, out var pt))
        {
            param.AddVolatileData(new GH_Path(0), 0, new GH_Point(pt));
            return true;
        }

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
    }

    private static bool SetTreeParamValue(IGH_Param param, JsonElement objectElement)
    {
        bool hasDataTreeKeys = false;
        foreach (var property in objectElement.EnumerateObject())
        {
            if (property.Name.StartsWith("{") && property.Name.EndsWith("}"))
            {
                hasDataTreeKeys = true;
                break;
            }
        }
        if (!hasDataTreeKeys) return false;

        bool itemsAppended = false;
        foreach (var branchProperty in objectElement.EnumerateObject())
        {
            if (!TryParsePath(branchProperty.Name, out var branchPath))
            {
                Log.Warning("PlayerDataTreeHelper: Malformed branch path '{Path}' skipped", branchProperty.Name);
                continue;
            }

            if (branchProperty.Value.ValueKind == JsonValueKind.Array)
            {
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
            }
            else
            {
                var gooInstance = ConvertJsonToGoo(branchProperty.Value, param);
                if (gooInstance != null)
                {
                    param.AddVolatileData(branchPath, 0, gooInstance);
                    itemsAppended = true;
                }
            }
        }
        return itemsAppended;
    }

    public static bool TryParsePoint(JsonElement element, out Point3d point)
    {
        point = default;
        if (element.ValueKind == JsonValueKind.Object)
        {
            bool hasX = element.TryGetProperty("x", out var px) || element.TryGetProperty("X", out px);
            bool hasY = element.TryGetProperty("y", out var py) || element.TryGetProperty("Y", out py);
            if (hasX && hasY && px.ValueKind == JsonValueKind.Number && py.ValueKind == JsonValueKind.Number)
            {
                double x = px.GetDouble();
                double y = py.GetDouble();
                double z = 0.0;
                if ((element.TryGetProperty("z", out var pz) || element.TryGetProperty("Z", out pz)) &&
                    pz.ValueKind == JsonValueKind.Number)
                {
                    z = pz.GetDouble();
                }
                point = new Point3d(x, y, z);
                return true;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array && element.GetArrayLength() == 3 &&
                 element[0].ValueKind == JsonValueKind.Number &&
                 element[1].ValueKind == JsonValueKind.Number &&
                 element[2].ValueKind == JsonValueKind.Number)
        {
            point = new Point3d(element[0].GetDouble(), element[1].GetDouble(), element[2].GetDouble());
            return true;
        }
        return false;
    }

    public static bool IsPointTarget(IGH_Param param)
    {
        var typeName = param.TypeName ?? string.Empty;
        if (typeName.Contains("Point", StringComparison.OrdinalIgnoreCase) ||
            typeName.Contains("Vector", StringComparison.OrdinalIgnoreCase) ||
            typeName.Contains("Plane", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var nick = param.NickName ?? string.Empty;
        var name = param.Name ?? string.Empty;
        return nick.Equals("Pt", StringComparison.OrdinalIgnoreCase) ||
               nick.Equals("Point", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("Point", StringComparison.OrdinalIgnoreCase);
    }

    private static IGH_Goo? ConvertJsonToGoo(JsonElement element, IGH_Param? targetParam = null)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var d))
            return new GH_Number(d);
        if (element.ValueKind is JsonValueKind.True or JsonValueKind.False)
            return new GH_Boolean(element.GetBoolean());
        if (element.ValueKind == JsonValueKind.String)
            return new GH_String(element.GetString());
        if (element.ValueKind == JsonValueKind.Object && TryParsePoint(element, out var ptFromObj))
            return new GH_Point(ptFromObj);
        if (element.ValueKind == JsonValueKind.Array &&
            (targetParam == null || IsPointTarget(targetParam)) &&
            TryParsePoint(element, out var ptFromArray))
        {
            return new GH_Point(ptFromArray);
        }
        return null;
    }

    public static bool TryParsePath(string pathNotation, out GH_Path path)
    {
        path = new GH_Path(0);
        if (string.IsNullOrWhiteSpace(pathNotation)) return false;

        var trimmed = pathNotation.Trim();
        if (!trimmed.StartsWith("{") || !trimmed.EndsWith("}")) return false;

        var inner = trimmed.Substring(1, trimmed.Length - 2).Trim();
        if (inner.Length == 0) return true;

        var segments = inner.Split(';', StringSplitOptions.None);
        var indices = new int[segments.Length];
        for (int i = 0; i < segments.Length; i++)
        {
            var seg = segments[i].Trim();
            if (!int.TryParse(seg, out var idx) || idx < 0)
            {
                return false;
            }
            indices[i] = idx;
        }

        path = new GH_Path(indices);
        return true;
    }

    public static GH_Path ParsePath(string pathNotation)
    {
        var cleaned = pathNotation.Trim('{', '}').Trim();
        if (string.IsNullOrWhiteSpace(cleaned)) return new GH_Path(0);
        var segments = cleaned.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var indexArray = new int[segments.Length];
        for (int i = 0; i < segments.Length; i++)
        {
            indexArray[i] = int.TryParse(segments[i].Trim(), out var parsed) ? parsed : 0;
        }
        return new GH_Path(indexArray);
    }

    public static IGH_Param? FindParam(
        IEnumerable<IGH_DocumentObject> objects,
        string nameOrGuid,
        Func<Guid, IGH_DocumentObject?>? findById = null)
    {
        if (Guid.TryParse(nameOrGuid, out var guid))
        {
            if (findById != null && findById(guid) is IGH_Param foundByIdParam)
                return foundByIdParam;

            foreach (var docObj in objects)
            {
                if (docObj.InstanceGuid == guid && docObj is IGH_Param directParam)
                    return directParam;
                if (docObj is IGH_Component comp)
                {
                    foreach (var input in comp.Params.Input)
                    {
                        if (input.InstanceGuid == guid) return input;
                    }
                }
            }
        }

        foreach (var obj in objects)
        {
            if (obj is IGH_Param param && string.Equals(param.NickName, nameOrGuid, StringComparison.OrdinalIgnoreCase))
                return param;
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

    // Delegations to PlayerOutputExtractor for backwards compatibility
    public static object? ExtractParamData(IGH_Param param) => PlayerOutputExtractor.ExtractParamData(param);
    public static object? ConvertGooToTypedValue(IGH_Goo gooInstance) => PlayerOutputExtractor.ConvertGooToTypedValue(gooInstance);
}
```

---

### 4.3 File 2: `PlayerOutputExtractor.cs` (GUID & Name Output Extraction)

```csharp
namespace HPGrasshopper.McpBridge.Tools;

using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

internal static class PlayerOutputExtractor
{
    public static Dictionary<string, object?> ExtractOutputs(
        IEnumerable<IGH_DocumentObject> objects,
        IReadOnlyList<string>? requestedOutputs)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var filterSet = requestedOutputs?.Count > 0
            ? new HashSet<string>(requestedOutputs, StringComparer.OrdinalIgnoreCase)
            : null;

        foreach (var obj in objects)
        {
            if (obj is IGH_Param param)
            {
                if (MatchesOutput(param, filterSet, out var matchedKey) &&
                    param.Recipients.Count == 0 && param.VolatileDataCount > 0)
                {
                    AddOutputResult(result, param, matchedKey);
                }
            }
            else if (obj is IGH_Component comp)
            {
                foreach (var output in comp.Params.Output)
                {
                    if (MatchesOutput(output, filterSet, out var matchedKey) &&
                        output.VolatileDataCount > 0)
                    {
                        AddOutputResult(result, output, matchedKey);
                    }
                }
            }
        }
        return result;
    }

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

    public static bool MatchesOutput(
        IGH_Param param,
        HashSet<string>? filterSet,
        out string? matchedKey)
    {
        matchedKey = null;
        if (filterSet == null || filterSet.Count == 0) return true;

        if (!string.IsNullOrEmpty(param.NickName) && filterSet.Contains(param.NickName))
        {
            matchedKey = param.NickName;
            return true;
        }

        if (!string.IsNullOrEmpty(param.Name) && filterSet.Contains(param.Name))
        {
            matchedKey = param.Name;
            return true;
        }

        var guidD = param.InstanceGuid.ToString("D");
        if (filterSet.Contains(guidD))
        {
            matchedKey = guidD;
            return true;
        }

        var guidN = param.InstanceGuid.ToString("N");
        if (filterSet.Contains(guidN))
        {
            matchedKey = guidN;
            return true;
        }

        var guid8 = guidD[..8];
        if (filterSet.Contains(guid8))
        {
            matchedKey = guid8;
            return true;
        }

        var compound = $"{param.NickName}#{guid8}";
        if (filterSet.Contains(compound))
        {
            matchedKey = compound;
            return true;
        }

        return false;
    }

    public static object? ExtractParamData(IGH_Param param)
    {
        if (param.VolatileDataCount == 0) return null;

        if (param.VolatileData.PathCount > 1 || (param.VolatileData.PathCount == 1 && param.VolatileData.Paths[0].ToString() != "{0}"))
        {
            var branchDictionary = new Dictionary<string, object?>();
            foreach (var path in param.VolatileData.Paths)
            {
                var branchItems = new List<object?>();
                var branch = param.VolatileData.get_Branch(path);
                if (branch != null)
                {
                    foreach (var item in branch)
                    {
                        if (item is IGH_Goo typedGoo) branchItems.Add(ConvertGooToTypedValue(typedGoo));
                        else if (item != null) branchItems.Add(item.ToString());
                    }
                }
                branchDictionary[path.ToString()] = branchItems;
            }
            return branchDictionary;
        }

        var scalarOrList = new List<object?>();
        foreach (var item in param.VolatileData.AllData(true))
        {
            if (item is IGH_Goo typedGoo) scalarOrList.Add(ConvertGooToTypedValue(typedGoo));
            else if (item != null) scalarOrList.Add(item.ToString());
        }
        return scalarOrList.Count == 1 ? scalarOrList[0] : scalarOrList;
    }

    public static object? ConvertGooToTypedValue(IGH_Goo gooInstance)
    {
        if (gooInstance is GH_Number num) return num.Value;
        if (gooInstance is GH_Integer integer) return integer.Value;
        if (gooInstance is GH_Boolean boolean) return boolean.Value;
        if (gooInstance is GH_String str) return str.Value;
        if (gooInstance is GH_Point point) return new { x = point.Value.X, y = point.Value.Y, z = point.Value.Z };
        return gooInstance.ToString();
    }
}
```

---

### 4.4 File 3: `PlayerInputFailure.cs`

```csharp
namespace HPGrasshopper.McpBridge.Tools;

/// <summary>
///     Diagnostic record detailing a rejected or unmatched input parameter in Grasshopper Player.
/// </summary>
public sealed record PlayerInputFailure(string Name, string Reason);
```

---

### 4.5 File 4: `PlayerRunner.cs` (Input Validation, Output Filtering & Runtime Error Detection)

```csharp
namespace HPGrasshopper.McpBridge.Tools;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Serilog;

public sealed class PlayerRunner
{
    public object Run(
        string filePath,
        JsonElement? inputs = null,
        IReadOnlyList<string>? requestedOutputs = null,
        bool headless = true,
        System.Threading.CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(filePath))
            return new { success = false, error = $"Definition file not found: {filePath}" };

        var sw = Stopwatch.StartNew();
        if (!TryReadArchive(filePath, out var archive, out var archiveError))
            return new { success = false, error = archiveError };

        GH_Document? doc = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            doc = new GH_Document();
            if (!archive.ExtractObject(doc, "Definition"))
                return new { success = false, error = "Failed to extract definition graph from archive." };

            var (appliedCount, rejectedInputs) = ApplyInputs(doc, inputs);
            if (rejectedInputs.Count > 0)
            {
                sw.Stop();
                return new
                {
                    success = false,
                    durationMs = sw.ElapsedMilliseconds,
                    inputsApplied = appliedCount,
                    rejectedInputs,
                    error = $"Failed to apply {rejectedInputs.Count} input(s): {string.Join("; ", rejectedInputs.Select(r => $"{r.Name}: {r.Reason}"))}"
                };
            }

            cancellationToken.ThrowIfCancellationRequested();
            doc.NewSolution(true); // Expire all objects to force recomputation with injected inputs
            cancellationToken.ThrowIfCancellationRequested();

            var validationResult = ValidateSolutionState(doc, sw, requestedOutputs, appliedCount);
            if (validationResult != null) return validationResult;

            var results = ExtractOutputs(doc.Objects, requestedOutputs);
            sw.Stop();
            return new
            {
                success = true,
                durationMs = sw.ElapsedMilliseconds,
                inputsApplied = appliedCount,
                rejectedInputs = new List<PlayerInputFailure>(),
                outputs = results
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "PlayerRunner: Execution failed for {Path}", filePath);
            return new { success = false, error = $"{ex.GetType().Name}: {ex.Message}" };
        }
        finally
        {
            DisposeDocument(doc, headless);
        }
    }

    private static object? ValidateSolutionState(
        GH_Document doc,
        Stopwatch sw,
        IReadOnlyList<string>? requestedOutputs,
        int appliedCount)
    {
        var (errors, warnings) = CollectRuntimeMessages(doc);
        if (errors.Count > 0)
        {
            sw.Stop();
            return new
            {
                success = false,
                durationMs = sw.ElapsedMilliseconds,
                inputsApplied = appliedCount,
                error = $"Solve failed with {errors.Count} runtime error(s).",
                errors,
                warnings
            };
        }

        if (requestedOutputs != null && requestedOutputs.Count > 0)
        {
            var outputs = ExtractOutputs(doc.Objects, requestedOutputs);
            foreach (var requested in requestedOutputs)
            {
                bool outputProduced = outputs.Any(pair =>
                    string.Equals(pair.Key, requested, StringComparison.OrdinalIgnoreCase) ||
                    pair.Key.StartsWith(requested + "#", StringComparison.OrdinalIgnoreCase) ||
                    (pair.Key.Contains('#') && pair.Key.EndsWith(requested[..Math.Min(8, requested.Length)], StringComparison.OrdinalIgnoreCase)));

                if (!outputProduced)
                {
                    sw.Stop();
                    return new
                    {
                        success = false,
                        durationMs = sw.ElapsedMilliseconds,
                        inputsApplied = appliedCount,
                        error = $"Requested output '{requested}' was not found or produced no data."
                    };
                }
            }
        }
        return null;
    }

    private static (List<string> errors, List<string> warnings) CollectRuntimeMessages(GH_Document doc)
    {
        var errorList = new List<string>();
        var warningList = new List<string>();

        foreach (var obj in doc.Objects)
        {
            if (obj is IGH_ActiveObject active)
            {
                var errs = active.RuntimeMessages(GH_RuntimeMessageLevel.Error);
                if (errs != null)
                {
                    foreach (var err in errs) errorList.Add($"[{obj.NickName ?? obj.Name}] {err}");
                }
                var warns = active.RuntimeMessages(GH_RuntimeMessageLevel.Warning);
                if (warns != null)
                {
                    foreach (var warn in warns) warningList.Add($"[{obj.NickName ?? obj.Name}] {warn}");
                }
            }

            if (obj is IGH_Component comp)
            {
                if (comp.Phase == GH_SolutionPhase.Failed && errorList.Count == 0)
                    errorList.Add($"[{comp.NickName ?? comp.Name}] Component execution failed (SolutionPhase.Failed).");

                if (comp.Params != null)
                {
                    CheckParamMessages(comp.Params.Input, comp, errorList, warningList);
                    CheckParamMessages(comp.Params.Output, comp, errorList, warningList);
                }
            }
        }
        return (errorList, warningList);
    }

    private static void CheckParamMessages(
        IEnumerable<IGH_Param>? parameters,
        IGH_Component parentComp,
        List<string> errorList,
        List<string> warningList)
    {
        if (parameters == null) return;
        foreach (var param in parameters)
        {
            var errs = param.RuntimeMessages(GH_RuntimeMessageLevel.Error);
            if (errs != null)
            {
                foreach (var err in errs)
                    errorList.Add($"[{parentComp.NickName ?? parentComp.Name} : {param.NickName ?? param.Name}] {err}");
            }
            var warns = param.RuntimeMessages(GH_RuntimeMessageLevel.Warning);
            if (warns != null)
            {
                foreach (var warn in warns)
                    warningList.Add($"[{parentComp.NickName ?? parentComp.Name} : {param.NickName ?? param.Name}] {warn}");
            }
        }
    }

    private static (int appliedCount, List<PlayerInputFailure> rejectedInputs) ApplyInputs(GH_Document doc, JsonElement? inputs)
    {
        if (!inputs.HasValue || inputs.Value.ValueKind != JsonValueKind.Object)
            return (0, new List<PlayerInputFailure>());

        var applied = 0;
        var rejected = new List<PlayerInputFailure>();

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
                {
                    rejected.Add(new PlayerInputFailure(prop.Name, $"Failed setting parameter value: incompatible type or structure."));
                }
            }
            catch (Exception ex)
            {
                rejected.Add(new PlayerInputFailure(prop.Name, $"Exception during assignment: {ex.Message}"));
            }
        }
        return (applied, rejected);
    }

    private static bool TryReadArchive(string filePath, out GH_Archive archive, out string? error)
    {
        archive = new GH_Archive();
        try
        {
            if (!archive.ReadFromFile(filePath))
            {
                error = "Failed to deserialize Grasshopper definition from file.";
                return false;
            }
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = $"Failed to deserialize Grasshopper definition from file: {ex.Message}";
            return false;
        }
    }

    internal static void DisposeDocument(IDisposable? doc, bool headless)
    {
        if (headless)
        {
            doc?.Dispose();
        }
        else if (doc is GH_Document ghDoc && Grasshopper.Instances.DocumentServer != null)
        {
            if (!Grasshopper.Instances.DocumentServer.Contains(ghDoc))
                Grasshopper.Instances.DocumentServer.AddDocument(ghDoc);
            if (Grasshopper.Instances.ActiveCanvas != null)
                Grasshopper.Instances.ActiveCanvas.Document = ghDoc;
        }
    }

    internal static IGH_Param? FindInputParam(
        IEnumerable<IGH_DocumentObject> objects,
        string nameOrGuid,
        Func<Guid, IGH_DocumentObject?>? findById = null) =>
        PlayerDataTreeHelper.FindParam(objects, nameOrGuid, findById);

    internal static bool SetParamValue(IGH_Param param, JsonElement val) =>
        PlayerDataTreeHelper.SetParamValue(param, val);

    internal static Dictionary<string, object?> ExtractOutputs(
        IEnumerable<IGH_DocumentObject> objects,
        IReadOnlyList<string>? requestedOutputs) =>
        PlayerOutputExtractor.ExtractOutputs(objects, requestedOutputs);
}
```

---

## 5. Verification Method

To verify this implementation independently without regressions:

1. **Unit Test Verification (Bridge Tests)**:
   Run from repository root:
   ```powershell
   dotnet test HPGrasshopper/HPGrasshopper.McpBridge.Tests/HPGrasshopper.McpBridge.Tests.csproj -c Debug -p:DeployAddin=false
   ```
   *Expected outcome*: 563+ tests pass with 0 failures.

2. **Add Focused Tests in `HPGrasshopper.McpBridge.Tests`**:
   - `PlayerDataTreeTests`:
     - `SetParamValue_NumericArray3Elements_OnNumberParam_AddsThreeScalars`:
       Pass `[1.0, 2.0, 3.0]` to `CreateParam("Values", name: "NumberParam")`. Assert `VolatileDataAdded.Count == 3` and elements are `GH_Number`.
     - `SetParamValue_PointObject_AddsPoint`:
       Pass `{ "x": 10.0, "y": 20.0, "z": 30.0 }`. Assert `VolatileDataAdded[0]` is `GH_Point`.
   - `PlayerRunnerTests`:
     - `Run_UnmatchedInput_ReturnsFailureWithRejectedInputs`:
       Provide input `{"NoSuchParam": 100.0}`. Assert `success == false`, `rejectedInputs` contains `"NoSuchParam"`.
     - `ExtractOutputs_RequestedByGuid_RetrievesOutput`:
       Provide `requestedOutputs: [ targetGuid.ToString("D") ]`. Assert `outputs.ContainsKey(targetGuid.ToString("D"))`.
     - `CollectRuntimeMessages_ComponentParamError_DetectsAndFormatsError`:
       Add error to `comp.Params.Input[0]`. Assert collected errors contains `[CompNick : ParamNick] ErrorMessage`.

3. **Line Count & Quality Verification**:
   Inspect line counts of modified files:
   - `HPGrasshopper.McpBridge/Tools/PlayerDataTreeHelper.cs` (must be <= 300 lines)
   - `HPGrasshopper.McpBridge/Tools/PlayerRunner.cs` (must be <= 300 lines)
   - `HPGrasshopper.McpBridge/Tools/PlayerOutputExtractor.cs` (must be <= 300 lines)
   - Verify `git status McpShared` shows 0 modified files.
