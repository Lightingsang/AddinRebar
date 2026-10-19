# Investigation Report: Finding N10 & Milestone M2 Testing Strategy (HPGrasshopper)

**Author:** Explorer Subagent (`explorer_m2_3`)  
**Date:** 2026-10-13  
**Working Directory:** `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_3\`  
**Target Solution:** `HPGrasshopper/HPGrasshopper.slnx`  
**Governance:** McpShared STRICTLY IMMUTABLE (0 edits), Rule C2 ($\le 300$ lines), Rule M3 ($\le 50$ lines), ADR-0007.

---

## 1. Observation

### 1.1 Finding N10: Stale / Unavailable Context Markers & Resource Mixing
- **File:** `HPGrasshopper/HPGrasshopper.McpBridge/Coordinator/GrasshopperContextExtractor.cs` (lines 36–77)
  ```csharp
  private static ContextResult ExtractNullUiApp(
      GrasshopperBridgeCoordinator coordinator,
      bool executionEnabled)
  {
      if (coordinator.CachedUiContext != null)
      {
          var cached = coordinator.CachedUiContext;
          return new ContextResult
          {
              Host = cached.Host,
              HostVersion = cached.HostVersion,
              RevitVersion = cached.RevitVersion,
              DocTitle = coordinator.ActiveDocumentTitle ?? cached.DocTitle,
              DocPath = coordinator.ActiveDocumentPath ?? cached.DocPath,
              IsFamily = cached.IsFamily,
              IsReadOnly = cached.IsReadOnly,
              IsModifiable = false,
              Units = cached.Units,
              OpenDocs = cached.OpenDocs,
              ExecutionEnabled = executionEnabled,
              Selection = cached.Selection,
              Grasshopper = cached.Grasshopper
          };
      }

      return new ContextResult
      {
          Host = PipeNaming.GrasshopperHost,
          HostVersion = "2026",
          RevitVersion = "2026",
          DocTitle = coordinator.ActiveDocumentTitle,
          DocPath = coordinator.ActiveDocumentPath,
          IsFamily = false,
          IsReadOnly = false,
          IsModifiable = false,
          Units = new UnitsInfo("m"),
          OpenDocs = coordinator.ActiveDocumentTitle != null ? [coordinator.ActiveDocumentTitle] : [],
          ExecutionEnabled = executionEnabled,
          Selection = [],
          Grasshopper = BuildGrasshopperFallbackInfo(coordinator)
      };
  }
  ```
- **File:** `HPGrasshopper/HPGrasshopper.McpBridge/Coordinator/GrasshopperBridgeCoordinator.cs` (lines 57, 82, 205–225)
  ```csharp
  public ContextResult? CachedUiContext { get; private set; }
  public void UpdateCachedUiContext(ContextResult context) => CachedUiContext = context;
  ```
  ```csharp
  public async Task<ContextResult> GetContextAsync(bool includeSelection, CancellationToken cancellationToken)
  {
      if (_externalEvent == null)
          return GrasshopperContextExtractor.Extract(null, this, includeSelection, _settings?.ExecutionEnabled ?? true);

      using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
      cts.CancelAfter(TimeSpan.FromMilliseconds(500));
      var work = new McpBridgeWorkRequest("get_context",
          (uiapp, _) => GrasshopperContextExtractor.Extract(uiapp, this, includeSelection, _settings?.ExecutionEnabled ?? true), cts.Token);

      try
      {
          var res = await RunOnRevitThreadAsync(work).ConfigureAwait(false);
          return res as ContextResult ?? GrasshopperContextExtractor.Extract(null, this, includeSelection, _settings?.ExecutionEnabled ?? true);
      }
      catch (Exception ex)
      {
          Log.Debug(ex, "GetContextAsync fell back to background context extraction");
          return GrasshopperContextExtractor.Extract(null, this, includeSelection, _settings?.ExecutionEnabled ?? true);
      }
  }
  ```
- **Observed Vulnerability:**
  1. Lines 48–57 overwrite `DocTitle` and `DocPath` with `coordinator.ActiveDocumentTitle` (e.g. "DocB.rvt"), but blindly reuse `cached.Units`, `cached.Selection`, `cached.OpenDocs`, and `cached.Grasshopper` from `cached` (which was recorded for "DocA.rvt").
  2. If the active document in Revit or the active Grasshopper definition switches while the UI thread is busy or times out (500 ms deadline in line 211), the AI receives a **Frankenstein context**: Title = DocB, but Units, Element Selection, and Grasshopper definition facts are from DocA.
  3. Neither `ContextResult` nor `coordinator` retains a timestamp (`DateTimeOffset`) indicating when the snapshot was captured.
  4. When `coordinator.CachedUiContext == null` (un-cached), line 71 assumes `Units = new UnitsInfo("m")`, `IsFamily = false`, `IsReadOnly = false`, and `ComponentCount = 0` without proof, masquerading unverified guesses as verified facts.

---

### 1.2 Finding N6: Player Input Data Assignment & Index Shifting
- **File:** `HPGrasshopper/HPGrasshopper.McpBridge/Tools/PlayerDataTreeHelper.cs` (lines 18–32, 54–72, 74–113)
  ```csharp
  if (val.ValueKind == JsonValueKind.Number && val.TryGetDouble(out var numValue))
  {
      param.AddVolatileData(new GH_Path(0), 0, new GH_Number(numValue)); // Return boolean ignored
      return true;
  }
  ```
  ```csharp
  private static bool SetArrayParamValue(IGH_Param param, JsonElement arrayElement)
  {
      ...
      int itemIndex = 0;
      foreach (var element in arrayElement.EnumerateArray())
      {
          var gooInstance = ConvertJsonToGoo(element, param);
          if (gooInstance != null)
          {
              param.AddVolatileData(new GH_Path(0), itemIndex++, gooInstance); // Return boolean ignored
          }
      }
      return itemIndex > 0; // If any item succeeded, returns true
  }
  ```
  ```csharp
  private static bool SetTreeParamValue(IGH_Param param, JsonElement objectElement)
  {
      ...
      foreach (var branchProperty in objectElement.EnumerateObject())
      {
          if (!TryParsePath(branchProperty.Name, out var branchPath))
          {
              Log.Warning("PlayerDataTreeHelper: Malformed branch path '{Path}' skipped", branchProperty.Name);
              continue; // Silently skipped!
          }
          ...
      }
      return itemsAppended; // Returns true if at least 1 item was appended in another valid branch!
  }
  ```
- **File:** `HPGrasshopper/HPGrasshopper.McpBridge/Tools/PlayerRunner.cs` (lines 208–218)
  ```csharp
  targetParam.ClearData();
  if (SetParamValue(targetParam, prop.Value))
  {
      targetParam.ExpireSolution(false);
      applied++;
  }
  else
      rejected.Add(new PlayerInputFailure(prop.Name, "Failed setting parameter value: incompatible type or structure."));
  ```
- **Observed Vulnerability:**
  1. `IGH_Param.AddVolatileData(...)` returns `bool` according to McNeel Rhino/Grasshopper API. If a parameter refuses conversion or cannot store the data, it returns `false`. `PlayerDataTreeHelper` ignores the return value across all call sites and blindly returns `true`.
  2. In `SetArrayParamValue`, if an item cannot be converted (e.g. `null` or type mismatch in `[10, null, 20]`), `gooInstance == null` and `itemIndex` is not incremented. Item `20` is assigned to index `1` instead of `2`, causing a silent **index shift**.
  3. In `SetTreeParamValue`, if a branch has a malformed path like `{"{0}":[1], "{a}":[2]}`, `{a}` is skipped via `continue;`. If `{0}` succeeded, `itemsAppended == true`, so `SetParamValue` returns `true`. `PlayerRunner` logs success, increments `applied++`, and leaves `rejectedInputs` empty. The client is never told `{a}` was lost.

---

### 1.3 Finding N7: Duplicate Output Nicknames Overwritten
- **File:** `HPGrasshopper/HPGrasshopper.McpBridge/Tools/PlayerOutputExtractor.cs` (lines 45–65)
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
          result[matchedKey] = extractedVal; // OVERWRITES PREVIOUS MATCHED KEY!
      }
  }
  ```
- **Observed Vulnerability:**
  - Suppose component 1 has output `NickName = "R"` (GUID 1) with value `"Val1"`.
  - Component 2 has output `NickName = "R"` (GUID 2) with value `"Val2"`.
  - The client specifies `requestedOutputs = ["R"]`.
  - Component 1 runs: `result["R"] = "Val1"`.
  - Component 2 runs: `result.ContainsKey("R")` is true, so `keyToUse` becomes `"R#guid2"`. Line 59 writes `result["R#guid2"] = "Val2"`.
  - Line 61 checks `!string.Equals(matchedKey, keyToUse)` ("R" != "R#guid2") which is `true`.
  - Line 63 executes: `result["R"] = "Val2"`.
  - **Result:** `"Val1"` is completely overwritten by `"Val2"`. The output of component 1 is destroyed and inaccessible.

---

### 1.4 Finding N8: Diagnostics Lost on Error & PascalCase Casing
- **File:** `HPGrasshopper/HPGrasshopper.McpBridge/Execution/GrasshopperExecutionEngine.cs` (lines 95–109, 186–212)
  ```csharp
  var execRes = RevitTransactionScope.Execute(uiapp, request, settings, null, cancel, (doc, linkedToken, log) =>
  {
      var runResult = _playerRunner.Run(path, inputs, outputs, headless, linkedToken);
      var elem = JsonSerializer.SerializeToElement(runResult);
      if (elem.TryGetProperty("success", out var s) && !s.GetBoolean())
      {
          var err = elem.TryGetProperty("error", out var e) ? e.GetString() : "Player execution failed";
          throw new InvalidOperationException(err); // Throws only string message!
      }
      return runResult;
  }, startAutoTransaction: false);

  if (execRes.IsError && execRes.Value == null)
      execRes.Value = JsonSerializer.SerializeToElement(new { success = false, error = execRes.Message, rolledBack = execRes.RolledBack });
  ```
  ```csharp
  internal static ExecuteResult MapToExecuteResult(object? data)
  {
      if (data is null) return ExecuteResult.Failure("Execution returned null result");
      var element = data is JsonElement je ? je : JsonSerializer.SerializeToElement(data); // DEFAULT OPTIONS: PascalCase!
      ...
  }
  ```
- **Observed Vulnerability:**
  1. When `PlayerRunner.Run` fails inside a Revit transaction, throwing `InvalidOperationException(err)` discards the `runResult` object containing `errors`, `warnings`, `rejectedInputs`, and `durationMs`.
  2. Lines 107–108 replace `execRes.Value` with a bare 3-field anonymous object `{ success = false, error = execRes.Message, rolledBack = execRes.RolledBack }`. The AI coding agent receives no diagnostics to fix the graph.
  3. `MapToExecuteResult` uses `JsonSerializer.SerializeToElement(data)` without `BridgeJson.Options`, causing C# DTO properties (`Document`, `Components`, `Wires`, `Outputs`) to serialize in PascalCase instead of camelCase, violating the wire contract.

---

### 1.5 Finding N9: Non-Array Container Operations Silently Skipped
- **File:** `HPGrasshopper/HPGrasshopper.McpBridge/Tools/CanvasBatchValidator.cs` (lines 61–62, 107–108, 138–139, 167–168)
  ```csharp
  private static void ValidateComponentsToAdd(...)
  {
      if (!args.TryGetProperty("componentsToAdd", out var list) || list.ValueKind != JsonValueKind.Array)
          return; // Silently ignores wrong container type!
      ...
  }
  ```
- **File:** `HPGrasshopper/HPGrasshopper.McpBridge/Tools/CanvasEditor.cs` (lines 117–118)
  ```csharp
  private static (int count, List<CreatedComponentInfo> created) AddComponents(...)
  {
      var createdList = new List<CreatedComponentInfo>();
      if (!args.TryGetProperty("componentsToAdd", out var list) || list.ValueKind != JsonValueKind.Array)
          return (0, createdList);
      ...
  }
  ```
- **Observed Vulnerability:**
  - If a client sends `"componentsToAdd": { "type": "Addition" }` (an object instead of an array), `Preflight` treats it as absent and returns `IsValid = true`.
  - `CanvasEditor` silently skips the addition, mutates other operations (e.g. `componentsToRemove`), and returns `success: true`.
  - The requested operation is silently dropped (silent no-op), leading to partial, unexpected mutations.

---

## 2. Logic Chain

```
[Observation 1.1]
ExtractNullUiApp takes coordinator.ActiveDocumentTitle (DocB)
and blends it with cached.Units, Selection, and Grasshopper definition (from DocA).
       │
       ▼
[Logical Consequence N10]
When document or definition switches and UI thread is unresponsive (modal/timeout),
the AI agent receives a Frankenstein Context: title of DocB but units and selection of DocA.
In un-cached states, units are assumed to be "m" (meters) and graph empty.
       │
       ▼
[Resolution Requirement N10]
Create an immutable GrasshopperContextSnapshot with explicit timestamp and state flags:
- If cached document != coordinator active document, treat as unavailable / unmixed.
- Mark IsStale = true when UI thread is unreachable.
- If un-cached, report units as "unavailable" rather than assuming "m".

────────────────────────────────────────────────────────────────────────────

[Observation 1.2]
PlayerDataTreeHelper ignores AddVolatileData boolean return,
skips index on null/unsupported items, and silently ignores invalid branch paths.
       │
       ▼
[Logical Consequence N6]
1. Target parameter conversion rejections are counted as successful assignments.
2. Null array items cause index shifts, distorting element pairings.
3. Malformed branch {"{a}":[2]} is discarded while reporting inputsApplied > 0.
       │
       ▼
[Resolution Requirement N6]
1. Propagate AddVolatileData return boolean; return false if assignment fails.
2. Reject malformed branch paths and record in rejectedInputs.
3. Preserve array indices without shifting when handling null/unsupported elements.

────────────────────────────────────────────────────────────────────────────

[Observation 1.3]
PlayerOutputExtractor line 63 overwrites result[matchedKey]
whenever keyToUse differs from matchedKey.
       │
       ▼
[Logical Consequence N7]
When requestedOutputs=["R"] matches multiple components with nickname "R",
the second component clobbers the first component's value.
       │
       ▼
[Resolution Requirement N7]
Preserve multiple outputs sharing an alias (e.g. as list or distinct GUID-suffixed entries)
without destructive overwrites.

────────────────────────────────────────────────────────────────────────────

[Observation 1.4]
GrasshopperExecutionEngine throws string on player error,
and overwrites execRes.Value with a bare anonymous object.
MapToExecuteResult serializes using default PascalCase options.
       │
       ▼
[Logical Consequence N8]
Diagnostics payload (errors, warnings, rejectedInputs) is obliterated on Revit rollback.
Public JSON shapes use PascalCase instead of camelCase wire standards.
       │
       ▼
[Resolution Requirement N8]
Preserve full diagnostics payload in execRes.Value upon error rollback.
Serialize via BridgeJson.Options in MapToExecuteResult.

────────────────────────────────────────────────────────────────────────────

[Observation 1.5]
CanvasBatchValidator checks list.ValueKind != JsonValueKind.Array
and immediately returns without logging an error.
       │
       ▼
[Logical Consequence N9]
Passing an object or primitive for componentsToAdd or wiresToConnect
is silently skipped. Mutations are partially executed without warning.
       │
       ▼
[Resolution Requirement N9]
Explicitly validate container types: if property is present and NOT an Array,
fail preflight immediately with a structured CanvasBatchError.
```

---

## 3. Caveats

1. **McpShared Immutability Constraint:** `McpShared` is strictly read-only (0 edits permitted). Wire contracts (`ContextResult`, `GrasshopperInfo`, `UnitsInfo`) cannot have new C# properties added to them. Stale context markers and timestamps must be tracked on `GrasshopperBridgeCoordinator` / `GrasshopperContextSnapshot` and conveyed on the wire via unmixed resources and explicit sentinel values (`units.length = "unavailable"`).
2. **Headless vs Live Rhino Runtime:** In CI and local test execution where `rhcommon_c.dll` is not loaded into process memory, Grasshopper kernel interactions rely on `DispatchProxy` test doubles (`TestProxyFactory`). Test designs must utilize these proxies so they execute 100% reliably without requiring a live Rhino 8 license.
3. **Governance Limits:** All proposed implementation files must strictly adhere to Rule C2 ($\le 300$ lines) and Rule M3 ($\le 50$ lines per method).

---

## 4. Conclusion

- **Finding N10** is caused by unvalidated fallback caching in `GrasshopperContextExtractor.ExtractNullUiApp` (lines 48–57), which merges the identity of a new document with the units, selection, and definition metrics of an old document, without timestamp or stale indicators.
- **Finding N6** stems from ignoring `AddVolatileData` return values, index skipping on null array items, and swallowing invalid branch path errors in `PlayerDataTreeHelper.cs`.
- **Finding N7** stems from unconditional alias re-assignment in `PlayerOutputExtractor.cs` (lines 61–64).
- **Finding N8** stems from throwing away the `runResult` diagnostic payload in `GrasshopperExecutionEngine.cs` (lines 101–108) and missing `BridgeJson.Options` in `MapToExecuteResult` (line 189).
- **Finding N9** stems from early returns on non-array containers in `CanvasBatchValidator.cs` (lines 61, 107, 138, 167).

---

## 5. Verification Method & Proposed Test Suite Design

The following test suites must be implemented in `HPGrasshopper.McpBridge.Tests` and `HPGrasshopper.Mcp.Server.Tests`:

### 5.1 Test Suite for Finding N10 (`FindingN10ContextStaleAndIsolationTests.cs`)
**Target File:** `HPGrasshopper.McpBridge.Tests/Coordinator/FindingN10ContextStaleAndIsolationTests.cs`

```csharp
namespace HPGrasshopper.McpBridge.Tests.Coordinator;

using System;
using HPGrasshopper.McpBridge.Coordinator;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.Messages;
using Xunit;

public sealed class FindingN10ContextStaleAndIsolationTests
{
    [Fact]
    public void ExtractNullUiApp_WhenDocumentSwitched_DoesNotMixUnitsOrSelectionOrDefinition()
    {
        var coordinator = GrasshopperBridgeCoordinator.Instance;
        var docAContext = new ContextResult
        {
            Host = PipeNaming.GrasshopperHost,
            DocTitle = "ProjectA.rvt",
            DocPath = "C:\\ProjectA.rvt",
            Units = new UnitsInfo("feet"),
            Selection = new[] { new ElementInfo(101, "Walls", "Wall-A") },
            Grasshopper = new GrasshopperInfo(true, "1.0", "8.0", "DefA.gh", "C:\\DefA.gh", 42, 10, 0, false, true)
        };
        coordinator.UpdateCachedUiContext(docAContext);

        // Switch active document identity in coordinator to ProjectB
        coordinator.UpdateActiveSnapshot("ProjectB.rvt", "C:\\ProjectB.rvt", Guid.NewGuid(), "DefB.gh", "C:\\DefB.gh");

        // Act: Fallback context extraction under null UI app (UI thread unavailable)
        var result = GrasshopperContextExtractor.Extract(null, coordinator, false, true);

        // Assert: MUST NOT mix ProjectA's units, selection, or GH definition into ProjectB
        Assert.Equal("ProjectB.rvt", result.DocTitle);
        Assert.NotEqual("feet", result.Units.Length); // Must not use DocA units
        Assert.Equal("unavailable", result.Units.Length);
        Assert.Empty(result.Selection); // Must not use DocA selection
        Assert.NotEqual("DefA.gh", result.Grasshopper?.ActiveDefinitionName); // Must not use DocA definition
        Assert.True(coordinator.IsContextUnavailable || coordinator.IsContextStale);
    }

    [Fact]
    public void ExtractNullUiApp_WhenSameDocument_PreservesTimestampAndMarksStale()
    {
        var coordinator = GrasshopperBridgeCoordinator.Instance;
        var initialContext = new ContextResult
        {
            Host = PipeNaming.GrasshopperHost,
            DocTitle = "ProjectSame.rvt",
            DocPath = "C:\\ProjectSame.rvt",
            Units = new UnitsInfo("mm"),
            Grasshopper = new GrasshopperInfo(true, "1.0", "8.0", "Same.gh", "C:\\Same.gh", 5, 2, 0, false, true)
        };
        coordinator.UpdateCachedUiContext(initialContext);
        coordinator.SetActiveDocumentTitle("ProjectSame.rvt");

        // Act
        var result = GrasshopperContextExtractor.Extract(null, coordinator, false, true);

        // Assert
        Assert.Equal("ProjectSame.rvt", result.DocTitle);
        Assert.False(result.IsModifiable); // Background snapshot is never modifiable
        Assert.True(coordinator.IsContextStale);
        Assert.NotNull(coordinator.LastSnapshotTimestamp);
    }

    [Fact]
    public void ExtractNullUiApp_WhenUncached_DoesNotAssumeMetersOrFakeCounts()
    {
        var coordinator = GrasshopperBridgeCoordinator.Instance;
        coordinator.UpdateCachedUiContext(null!);
        coordinator.SetActiveDocumentTitle("NewModel.rvt");

        // Act
        var result = GrasshopperContextExtractor.Extract(null, coordinator, false, true);

        // Assert
        Assert.Equal("unavailable", result.Units.Length); // MUST NOT assume "m"
        Assert.False(result.Grasshopper?.IsEditorLoaded);
        Assert.True(coordinator.IsContextUnavailable);
    }
}
```

---

### 5.2 Test Suite for Finding N6 (`FindingN6PlayerDataTreeAssignmentTests.cs`)
**Target File:** `HPGrasshopper.McpBridge.Tests/Tools/FindingN6PlayerDataTreeAssignmentTests.cs`

```csharp
namespace HPGrasshopper.McpBridge.Tests.Tools;

using System.Text.Json;
using Grasshopper.Kernel;
using HPGrasshopper.McpBridge.Tests.Fakes;
using HPGrasshopper.McpBridge.Tools;
using Xunit;

public sealed class FindingN6PlayerDataTreeAssignmentTests
{
    [Fact]
    public void SetParamValue_WhenAddVolatileDataReturnsFalse_ReturnsFalseAndRejects()
    {
        var param = TestProxyFactory.CreateParam("TargetNumber");
        var proxy = TestProxyFactory.GetProxy<TestParamProxy>(param);
        proxy.AddVolatileDataReturnValue = false; // Simulates parameter refusing conversion

        using var jsonDoc = JsonDocument.Parse("123.45");

        // Act
        var success = PlayerDataTreeHelper.SetParamValue(param, jsonDoc.RootElement, out var errorMessage);

        // Assert
        Assert.False(success);
        Assert.NotNull(errorMessage);
        Assert.Contains("refused", errorMessage.ToLowerInvariant());
    }

    [Fact]
    public void SetParamValue_DataTree_MalformedBranchPath_ReturnsFalseAndReportsError()
    {
        var param = TestProxyFactory.CreateParam("TreeParam");
        using var jsonDoc = JsonDocument.Parse("""
        {
            "{0}": [10],
            "{a}": [20]
        }
        """);

        // Act
        var success = PlayerDataTreeHelper.SetParamValue(param, jsonDoc.RootElement, out var errorMessage);

        // Assert
        Assert.False(success);
        Assert.NotNull(errorMessage);
        Assert.Contains("{a}", errorMessage);
    }

    [Fact]
    public void SetParamValue_ArrayWithNullOrIncompatibleItem_PreservesIndexWithoutShift()
    {
        var param = TestProxyFactory.CreateParam("NumberList");
        var proxy = TestProxyFactory.GetProxy<TestParamProxy>(param);

        using var jsonDoc = JsonDocument.Parse("[10, null, 30]");

        // Act: Must reject or preserve slot without shifting item 30 into index 1
        var success = PlayerDataTreeHelper.SetParamValue(param, jsonDoc.RootElement, out var errorMessage);

        // Assert: Index 1 must not receive value 30
        if (success)
        {
            Assert.Equal(3, proxy.VolatileDataAdded.Count); // Preserved slot
        }
        else
        {
            Assert.NotNull(errorMessage); // Or rejected explicitly
        }
    }
}
```

---

### 5.3 Test Suite for Finding N7 (`FindingN7DuplicateOutputNicknamesTests.cs`)
**Target File:** `HPGrasshopper.McpBridge.Tests/Tools/FindingN7DuplicateOutputNicknamesTests.cs`

```csharp
namespace HPGrasshopper.McpBridge.Tests.Tools;

using System;
using System.Collections.Generic;
using Grasshopper.Kernel.Types;
using HPGrasshopper.McpBridge.Tests.Fakes;
using HPGrasshopper.McpBridge.Tools;
using Xunit;

public sealed class FindingN7DuplicateOutputNicknamesTests
{
    [Fact]
    public void ExtractOutputs_DuplicateNicknameWithRequestedOutputs_PreservesAllValuesWithoutOverwrite()
    {
        var param1 = TestProxyFactory.CreateParam("R", "Param1", instanceGuid: Guid.NewGuid());
        var proxy1 = TestProxyFactory.GetProxy<TestParamProxy>(param1);
        proxy1.VolatileDataAdded.Add(new GH_String("OutputA"));

        var param2 = TestProxyFactory.CreateParam("R", "Param2", instanceGuid: Guid.NewGuid());
        var proxy2 = TestProxyFactory.GetProxy<TestParamProxy>(param2);
        proxy2.VolatileDataAdded.Add(new GH_String("OutputB"));

        var objects = new[] { param1, param2 };
        var requestedOutputs = new[] { "R" };

        // Act
        var result = PlayerOutputExtractor.ExtractOutputs(objects, requestedOutputs);

        // Assert: OutputA must NOT be overwritten by OutputB!
        Assert.NotEmpty(result);
        var allValues = new List<string>();
        foreach (var val in result.Values)
        {
            if (val is string s) allValues.Add(s);
            else if (val is IEnumerable<object> list)
                foreach (var item in list) allValues.Add(item.ToString()!);
        }

        Assert.Contains("OutputA", allValues);
        Assert.Contains("OutputB", allValues);
    }
}
```

---

### 5.4 Test Suite for Finding N8 (`FindingN8DiagnosticsAndCamelCaseTests.cs`)
**Target File:** `HPGrasshopper.McpBridge.Tests/Execution/FindingN8DiagnosticsAndCamelCaseTests.cs`

```csharp
namespace HPGrasshopper.McpBridge.Tests.Execution;

using System.Text.Json;
using HPGrasshopper.McpBridge.Execution;
using HPGrasshopper.McpBridge.Tools;
using HPRebar.Mcp.Contracts.JsonRpc;
using Xunit;

public sealed class FindingN8DiagnosticsAndCamelCaseTests
{
    [Fact]
    public void MapToExecuteResult_SerializesDtoPropertiesWithStrictCamelCase()
    {
        var sampleResult = new PlayerRunResult(
            Success: false,
            Outputs: new Dictionary<string, object?> { ["Area"] = 100 },
            Errors: new[] { "Calculation error" },
            Warnings: new[] { "Low precision" },
            RejectedInputs: new[] { new PlayerInputFailure("ParamX", "Bad type") },
            DurationMs: 125,
            Error: "Solve failed");

        // Act
        var execResult = GrasshopperExecutionEngine.MapToExecuteResult(sampleResult);

        // Assert
        Assert.True(execResult.IsError);
        var json = execResult.Value!.Value;

        Assert.True(json.TryGetProperty("success", out _));
        Assert.False(json.TryGetProperty("Success", out _));

        Assert.True(json.TryGetProperty("errors", out var errs));
        Assert.False(json.TryGetProperty("Errors", out _));
        Assert.Single(errs.EnumerateArray());

        Assert.True(json.TryGetProperty("warnings", out _));
        Assert.False(json.TryGetProperty("Warnings", out _));

        Assert.True(json.TryGetProperty("rejectedInputs", out _));
        Assert.False(json.TryGetProperty("RejectedInputs", out _));

        Assert.True(json.TryGetProperty("durationMs", out _));
        Assert.False(json.TryGetProperty("DurationMs", out _));
    }
}
```

---

### 5.5 Test Suite for Finding N9 (`FindingN9ContainerSchemaValidationTests.cs`)
**Target File:** `HPGrasshopper.McpBridge.Tests/Tools/FindingN9ContainerSchemaValidationTests.cs`

```csharp
namespace HPGrasshopper.McpBridge.Tests.Tools;

using System.Text.Json;
using HPGrasshopper.McpBridge.Tools;
using Xunit;

public sealed class FindingN9ContainerSchemaValidationTests
{
    [Theory]
    [InlineData("{\"componentsToAdd\": {\"type\": \"Addition\"}}", "componentsToAdd")]
    [InlineData("{\"componentsToRemove\": \"6ba7b810-9dad-11d1-80b4-00c04fd430c8\"}", "componentsToRemove")]
    [InlineData("{\"wiresToConnect\": {\"source\": \"A\", \"target\": \"B\"}}", "wiresToConnect")]
    [InlineData("{\"wiresToDisconnect\": 12345}", "wiresToDisconnect")]
    public void Preflight_WhenContainerIsNotJsonArray_FailsWithStructuredError(string rawJson, string expectedContainer)
    {
        using var jsonDoc = JsonDocument.Parse(rawJson);

        // Act
        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement);

        // Assert
        Assert.False(preflight.IsValid);
        Assert.Contains(preflight.Errors, e =>
            e.Operation == expectedContainer &&
            e.Reason.Contains("must be a JSON array", StringComparison.OrdinalIgnoreCase));
    }
}
```

---

## 6. Execution Command for Verification

To verify the test suites once implemented:
```powershell
dotnet test HPGrasshopper.McpBridge.Tests/HPGrasshopper.McpBridge.Tests.csproj -c Debug -p:DeployAddin=false
dotnet test HPGrasshopper.Mcp.Server.Tests/HPGrasshopper.Mcp.Server.Tests.csproj -c Debug -p:DeployAddin=false
dotnet test HPGrasshopper.E2E.Tests/HPGrasshopper.E2E.Tests.csproj -c Debug -p:DeployAddin=false
```
All tests must pass filter-free without modifying any file in `McpShared/`.
