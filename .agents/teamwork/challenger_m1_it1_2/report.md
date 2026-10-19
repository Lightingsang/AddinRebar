# Empirical Challenge Report: Milestone M1 (Iteration 1)

**Agent**: `challenger_m1_it1_2` (Teamwork Empirical Challenger: Critic, Specialist)  
**Date**: 2026-10-07  
**Working Directory**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m1_it1_2\`  
**Scope**: Verification of `BimExecutionPlan` IR, `ExecutionPlanBuilder`, `BimPipelineEngine`, `BimPlanRunnerScriptTemplate`, and Rule 0 Zero-Fallback Compliance.  
**Empirical Harness**: `HPBim/tests/HPBim.Client.Tests/Pipeline/EmpiricalChallengerM1Tests.cs` (6 tests executed via `dotnet test`, 100% confirmed).

---

## 1. Executive Summary

Milestone M1 aimed to establish a single source of truth intermediate representation (`BimExecutionPlan`), strict schema validation, 100% parity between Preview and Apply, and strict enforcement of the Golden Rule (**ĐIỀU 0: Zero Heuristics / Zero Guessing: "Thà để một cấu kiện chưa dựng còn hơn dựng sai nhưng báo đúng"**).

Through empirical and adversarial stress testing, **CRITICAL architectural and behavioural defects** were discovered and proven in code. Specifically:
1. **Wire Format Serialization Flaw**: The intermediate representation serializes `CoordinationCategory` as integer numbers via `BridgeJson`, while the Revit script template compares them against string literals. In an active Revit bridge, **100% of all elements fall through into the `else` block and are recorded as `NoOp`**. Zero elements are modeled, yet the pipeline reports `Success: true`.
2. **Blatant Violation of Rule 0**: `BimPlanRunnerScriptTemplate.cs` explicitly contains `.Values.FirstOrDefault()` fallbacks for Columns, Beams, and Walls. When a requested symbol/type is missing in the document, it grabs an arbitrary existing symbol, duplicates it without assigning required geometry parameters, instantiates a malformed element, and marks it as `Created` rather than failing fast with `Failed`.
3. **False Pass Masking**: `BimPipelineResult.Success` is evaluated solely from `!execRes.IsError`. When elements fail during execution (due to missing levels, missing symbols, or API errors), the script catches them and returns the records cleanly, resulting in `execRes.IsError == false`. The engine reports `Success: true` and `ErrorMessage: null`, masking complete execution failures from users and agents.
4. **Omission of Domain Stages in IR Runner**: Slabs, Openings (Doors/Windows), Rooms, MEP Fixtures, Pipes, and Rebar are completely unhandled in `BimPlanRunnerScriptTemplate.cs`. They silently fall into `else` and report `NoOp`.
5. **Non-Deterministic Plan Hash**: `ComputePlanHash` hashes `PlanId`, which is assigned a new `Guid.NewGuid()` on every call to `ExecutionPlanBuilder.Build`. Identical models produce differing plan hashes, defeating idempotency.
6. **CLI Command Bypass**: `GenerateCommand` instantiates `BimPipelineOptions` without setting `UseExecutionPlan = true` (defaults to `false`), meaning `hpbim generate` bypasses the execution plan IR entirely and continues using legacy generation where Preview and Apply use differing scripts.

**Verdict**: **CHALLENGE_FAILED** (Blocking defects must be resolved before M1 approval).

---

## 2. Challenge Summary

**Overall risk assessment**: **CRITICAL**

| # | Severity | Defect / Vulnerability | Affected Components | Impact |
|---|----------|------------------------|---------------------|--------|
| 1 | **CRITICAL** | Wire Format Enum Type Mismatch Causes 100% Silent NoOp Fall-Through | `ElementPlanItem.cs`, `BimPlanRunnerScriptTemplate.cs`, `BridgeJson.cs` | 100% of BIM elements in plan are bypassed as `NoOp`; 0 elements created in Revit; silent failure masked as `Success: true`. |
| 2 | **CRITICAL** | Heuristic `.FirstOrDefault()` Fallback Violates Rule 0 | `BimPlanRunnerScriptTemplate.cs` (lines 212, 269, 329) | Missing symbols trigger arbitrary first-symbol duplication without setting dimensions; models wrong geometry and falsely reports `Created`. |
| 3 | **HIGH** | False Pass: `BimPipelineResult.Success` Reports `true` on Element Failures | `BimPipelineEngine.cs` (lines 192-213) | Pipeline reports `Success: true` even when 100% of elements fail execution. |
| 4 | **HIGH** | Incomplete Script Runner: Slabs, Openings, Rooms, MEP, Pipes, Rebar Unhandled | `BimPlanRunnerScriptTemplate.cs` (lines 368-378) | Major architectural and structural categories fall into `else` and report `NoOp`. |
| 5 | **MEDIUM** | Non-Deterministic `PlanHash` Across Rebuilds of Identical Models | `BimExecutionPlan.cs` (line 47), `ExecutionPlanBuilder.cs` (line 56) | `PlanHash` incorporates random `PlanId`; identical models produce differing hashes, breaking idempotency. |
| 6 | **MEDIUM** | `CompositeSourceKey.TryParse` Fails on Empty `DocIdentifier` | `CompositeSourceKey.cs` (lines 13, 47) | Format string `"[]#[Column]:C1"` cannot be parsed back by `TryParse`. |
| 7 | **MEDIUM** | CLI `GenerateCommand` Bypasses Execution Plan IR | `GenerateCommand.cs` (line 78), `BimPipelineOptions.cs` (line 39) | `UseExecutionPlan` defaults to `false`; CLI still runs legacy staged generator where Preview and Apply scripts diverge. |

---

## 3. Detailed Challenges & Empirical Proofs

### [CRITICAL] Challenge 1: Wire Format Enum Type Mismatch Causes 100% Silent NoOp Fall-Through

- **Assumption Challenged**: The worker claimed that `BimPlanRunnerScriptTemplate` executes the topological plan in Revit and models Levels, Grids, Columns, Beams, and Walls.
- **Attack Scenario**:
  1. `ExecutionPlanBuilder.Build(model)` constructs `BimExecutionPlan` where `item.Category` is the C# enum `CoordinationCategory` (e.g., `CoordinationCategory.Level = 8`, `Column = 4`, `Wall = 5`).
  2. `BimPipelineEngine.ExecutePlanAsync` serializes the plan using `BridgeJson.ToElement(plan)`.
  3. `BridgeJson.Options` (`HPRebar.Mcp.Contracts.JsonRpc`) specifies standard camelCase naming and relaxes HTML escaping, but **does NOT register `JsonStringEnumConverter`**. Furthermore, `ElementPlanItem.Category` lacks a `[JsonConverter(typeof(JsonStringEnumConverter))]` attribute.
  4. Consequently, `BridgeJson` serializes `category` as a JSON number (e.g., `8`, `9`, `4`, `7`, `5`).
  5. In Revit, the script reads:
     ```csharp
     string category = item.Require("category");
     ```
     `ScriptArgs.ToStr(e)` evaluates a `JsonValueKind.Number` element to its raw text string: `"8"`, `"9"`, `"4"`, `"7"`, `"5"`.
  6. The script evaluates:
     ```csharp
     if (category == "Level") ...
     else if (category == "Grid") ...
     else if (category == "Column") ...
     else if (category == "Beam" || category == "GradeBeam") ...
     else if (category == "Wall") ...
     else
     {
         subTx.Commit();
         records.Add(new Dictionary<string, object>
         {
             ["sourceKey"] = sourceKey,
             ["category"] = category,
             ["mark"] = mark,
             ["status"] = "NoOp"
         });
     }
     ```
     Because `"8" == "Level"` is `false`, `"4" == "Column"` is `false`, etc., **every single element executes the `else` branch**.
- **Blast Radius**: In an active Revit bridge session, 0 elements are created, 0 elements are updated. 100% of elements are logged as `NoOp`. The pipeline reports `Success: true`, completely misleading the user that the model was created.
- **Empirical Proof**:
  Executed test `Challenge1_Category_WireFormat_CausesAllElementsToFallIntoElseNoOp` in `EmpiricalChallengerM1Tests.cs`:
  - Input: Benchmark `NhaPho5x18mFixture.CreateModel()` (46 elements).
  - Observed output:
    - `Total items`: 46
    - `Levels matched`: 0
    - `Grids matched`: 0
    - `Columns matched`: 0
    - `Beams matched`: 0
    - `Walls matched`: 0
    - `Else (NoOp) matched`: 46
  - `Assert.Equal(items.Count, elseNoOpMatches)` PASSED.
- **Mitigation**:
  Decorate `public CoordinationCategory Category` in `ElementPlanItem.cs` with `[JsonConverter(typeof(JsonStringEnumConverter))]`, or configure `BridgeJson` to serialize enums as strings, or parse `category` in `BimPlanRunnerScriptTemplate` to accept both string names and integer values.

---

### [CRITICAL] Challenge 2: Heuristic `.FirstOrDefault()` Fallbacks Violate Rule 0

- **Assumption Challenged**: The worker claimed that `ExecutionPlanBuilder` and `BimPlanRunnerScriptTemplate` eradicated all `.First()` and `.FirstOrDefault()` heuristic guessing, strictly abiding by the Golden Rule: "Thà để một cấu kiện chưa dựng còn hơn dựng sai nhưng báo đúng".
- **Attack Scenario**:
  Inspect lines 208-230, 266-286, and 326-346 in `BimPlanRunnerScriptTemplate.cs`:
  ```csharp
  // Columns
  string symName = res.Require("typeName");
  if (!columnSymbols.TryGetValue(symName, out var sym))
  {
      var baseSym = columnSymbols.Values.FirstOrDefault(); // <-- HEURISTIC FALLBACK!
      if (baseSym == null)
      {
          subTx.RollBack();
          records.Add(new Dictionary<string, object> { ... ["status"] = "Failed" });
          continue;
      }
      sym = (FamilySymbol)baseSym.Duplicate(symName);
      columnSymbols[symName] = sym;
  }
  ```
  1. If the project contains ANY structural column symbol (e.g., an arbitrary timber column or steel H-beam), `baseSym` is NOT null.
  2. The script duplicates that arbitrary symbol to `symName`.
  3. **Crucially, the script DOES NOT set any dimension parameters** (`b`, `h`, `Width`, `Depth`). It leaves the duplicated symbol with whatever random dimensions `baseSym` had!
  4. It instantiates the column and reports:
     `records.Add(new Dictionary<string, object> { ... ["status"] = "Created" });`
  5. The element is created with incorrect dimensions and arbitrary geometry, and reported as SUCCESS (`Created`)!
  6. The exact same violation exists for Beams (`beamSymbols.Values.FirstOrDefault()`) and Walls (`wallTypes.Values.FirstOrDefault()`).
- **Blast Radius**: Directly violates Requirement R1 of `ORIGINAL_REQUEST.md`: *"Cấm fallback ngầm (chọn type đầu tiên, bỏ qua lỗi)"*. Produces wrong BIM geometry while deceptively reporting `Created`.
- **Empirical Proof**:
  Executed test `Challenge2_ScriptTemplate_ContainsFirstValueFallback_ViolatingRule0` in `EmpiricalChallengerM1Tests.cs`:
  - Verified string occurrences of `columnSymbols.Values.FirstOrDefault()`, `beamSymbols.Values.FirstOrDefault()`, and `wallTypes.Values.FirstOrDefault()` in `BimPlanRunnerScriptTemplate.GetScriptCode()`.
  - Assertions PASSED.
- **Mitigation**:
  Under Rule 0, if `symName` is not found and cannot be procedurally constructed with verified dimensions, the element MUST immediately fail fast:
  ```csharp
  subTx.RollBack();
  records.Add(new Dictionary<string, object>
  {
      ["sourceKey"] = sourceKey,
      ["category"] = category,
      ["mark"] = mark,
      ["status"] = "Failed",
      ["errorCode"] = "ERR_SYMBOL_NOT_FOUND",
      ["errorReason"] = $"Required symbol '{symName}' not found in document and procedural template is absent."
  });
  continue;
  ```
  If procedural generation is desired, it must strictly target the designated `res.FamilyName`, duplicate from that exact family, set all dimension parameters (`b`, `h`, `Width`, `Depth`) via `LookupParameter().Set(valFt)`, and fail if the parameter cannot be set. Under no circumstances should it grab `columnSymbols.Values.FirstOrDefault()`.

---

### [HIGH] Challenge 3: False Pass — `BimPipelineResult.Success` Reports `true` on Element Failures

- **Assumption Challenged**: The worker claimed that `BimPipelineEngine` accurately reports execution outcomes.
- **Attack Scenario**:
  In `BimPipelineEngine.cs` (lines 192-216):
  ```csharp
  var elementRecords = ParseElementRecords(execRes, plan);

  bool success = !execRes.IsError;
  if (plan.Options.DryRun && !execRes.RolledBack)
  {
      success = false;
  }
  ```
  When elements encounter errors in Revit (such as missing levels, missing symbols, degenerate curves, or Revit API exceptions), the script catches the exception or records the failure:
  `["status"] = "Failed", ["errorCode"] = "ERR_LEVEL_NOT_FOUND"`
  The script finishes and returns the dictionary `{ total, created: 0, failed: N, records }`.
  Because the script returned valid JSON without throwing an unhandled exception at script root, the pipe server returns `ExecuteResult { IsError = false }`.
  `BimPipelineEngine` evaluates:
  `success = !execRes.IsError;` which evaluates to **`true`**!
  The engine returns `BimPipelineResult { Success = true, ErrorMessage = null }`.
- **Blast Radius**: Any command or caller inspecting `result.Success` is falsely informed that the pipeline succeeded, even if 100% of the elements failed.
- **Empirical Proof**:
  Executed test `Challenge3_PipelineResult_ReportsSuccessTrue_WhenElementsFail` in `EmpiricalChallengerM1Tests.cs`:
  - Created a mock execute result returning 2 failed element records (`status: "Failed"`).
  - Executed `engine.ExecutePlanAsync(plan)`.
  - Observed: `result.Success == true` and `result.ErrorMessage == null`.
  - `Assert.True(result.Success)` and `Assert.All(..., r => r.Status == Failed)` PASSED.
- **Mitigation**:
  `BimPipelineResult.Success` must evaluate whether elements failed when `plan.Options.StrictMode` is active:
  ```csharp
  bool hasElementFailures = elementRecords.Any(r => r.Status == ElementExecutionStatus.Failed);
  if (hasElementFailures && plan.Options.StrictMode)
  {
      success = false;
  }
  ```

---

### [HIGH] Challenge 4: Incomplete Script Runner — Slabs, Openings, Rooms, MEP, Pipes, Rebar Unhandled

- **Assumption Challenged**: The worker claimed `BimPlanRunnerScriptTemplate` unifies the 8-stage pipeline into a single IR script.
- **Attack Scenario**:
  Inspect lines 97-378 of `BimPlanRunnerScriptTemplate.cs`.
  The category dispatcher only has branches for:
  - `category == "Level"`
  - `category == "Grid"`
  - `category == "Column"`
  - `category == "Beam" || category == "GradeBeam"`
  - `category == "Wall"`
  There are NO handlers for:
  - `Slab`
  - `Opening` (Door, Window)
  - `Room`
  - `MepFixture`
  - `PipeRun`
  - `Rebar`
  All these categories fall into `else` and report `status: "NoOp"`.
- **Blast Radius**: Despite `ExecutionPlanBuilder` building items for Slabs, Openings, Rooms, MEP, and Rebar, executing the plan via `ExecutePlanAsync` generates none of them.
- **Empirical Proof**:
  Executed test `Challenge4_ScriptTemplate_OmitsSlabsOpeningsRoomsMepRebar` in `EmpiricalChallengerM1Tests.cs`:
  - Verified absence of `category == "Slab"`, `"Opening"`, `"Room"`, `"MepFixture"`, `"PipeRun"`, `"Rebar"`.
  - Assertions PASSED.
- **Mitigation**:
  Either implement the script routines for all 8 stages within `BimPlanRunnerScriptTemplate` (leveraging the logic from `CompositeBimGenerator.Stages3To8.cs`), or mark unhandled stages explicitly as `ElementExecutionStatus.Failed` with `ERR_UNIMPLEMENTED_CATEGORY` rather than returning `NoOp`.

---

### [MEDIUM] Challenge 5: Non-Deterministic `PlanHash` Across Rebuilds of Identical Models

- **Assumption Challenged**: `BimExecutionPlan.PlanHash` is a deterministic SHA-256 fingerprint representing the model content.
- **Attack Scenario**:
  In `BimExecutionPlan.cs` (lines 45-47):
  ```csharp
  public static string ComputePlanHash(Guid planId, DocumentIdentitySnapshot targetDoc, IReadOnlyList<ElementPlanItem> items)
  {
      using var sha256 = SHA256.Create();
      var sb = new StringBuilder();
      sb.Append(planId.ToString("N")); // <-- Incorporates PlanId
  ```
  In `ExecutionPlanBuilder.Build(model)`:
  ```csharp
  var planId = Guid.NewGuid(); // <-- Generates new random GUID every call
  string planHash = BimExecutionPlan.ComputePlanHash(planId, targetDoc, sequencedItems);
  ```
  Calling `ExecutionPlanBuilder.Build(model)` twice on the identical model produces two completely different plan hashes.
- **Blast Radius**: Breaks idempotency checks, Extensible Storage receipt matching (`receipt.PlanHash == plan.PlanHash`), and change detection.
- **Empirical Proof**:
  Executed test `Challenge5_PlanHash_IsNotDeterministicAcrossBuildsOfSameModel` in `EmpiricalChallengerM1Tests.cs`:
  - `var plan1 = ExecutionPlanBuilder.Build(model);`
  - `var plan2 = ExecutionPlanBuilder.Build(model);`
  - `Assert.NotEqual(plan1.PlanHash, plan2.PlanHash)` PASSED.
- **Mitigation**:
  `ComputePlanHash` should compute the deterministic content hash from `targetDoc` and `items` (and optionally a caller-supplied `planId` or deterministic namespace GUID), or `ExecutionPlanBuilder` should generate a deterministic GUID based on the content hash.

---

### [MEDIUM] Challenge 6: `CompositeSourceKey.TryParse` Fails on Empty `DocIdentifier`

- **Attack Scenario**:
  `CompositeSourceKey.Create("", CoordinationCategory.Column, "C1").ToString()` produces `"[]#[Column]:C1"`.
  `CompositeSourceKey.TryParse` uses regex `^\[([^\]]+)\]#\[([^\]]+)\]:(.+)$`.
  The group `[^\]]+` requires at least 1 non-bracket character, so it fails on empty brackets `[]`.
- **Blast Radius**: Prevents deserialization of source keys when CAD document title is unknown or omitted.
- **Empirical Proof**:
  Executed test `Challenge6_CompositeSourceKey_RoundTrip_FailsOnEmptyDocIdentifier` in `EmpiricalChallengerM1Tests.cs`:
  - `CompositeSourceKey.TryParse("[]#[Column]:C1", out _)` returned `false`.
  - Assertion PASSED.
- **Mitigation**:
  Update regex to allow empty document identifier: `^\[([^\]]*)\]#\[([^\]]+)\]:(.+)$`.

---

### [MEDIUM] Challenge 7: CLI `GenerateCommand` Bypasses Execution Plan IR

- **Attack Scenario**:
  In `HPBim/src/HPBim.Cli/Commands/GenerateCommand.cs` line 78:
  ```csharp
  var options = new BimPipelineOptions { DryRun = dryRun, AtomicDryRun = dryRun };
  var result = await pipeline.ExecuteAsync(model, options, ct).ConfigureAwait(false);
  ```
  In `BimPipelineOptions.cs` line 39:
  ```csharp
  public bool UseExecutionPlan { get; init; } // Defaults to false
  ```
  Because `options.UseExecutionPlan` is not set to `true`, `pipeline.ExecuteAsync` branches to legacy execution:
  - If `dryRun == true`: calls `ExecuteAtomicDryRunAsync` (`CompositeBimGenerator`).
  - If `dryRun == false`: calls `ExecuteSequentialStagesAsync` (8 separate stage generator requests).
- **Blast Radius**: Running `hpbim generate --dry-run` vs `hpbim generate` from CLI executes completely divergent script generators, defeating the core requirement of single-source-of-truth Preview/Apply parity.
- **Mitigation**:
  Set `UseExecutionPlan = true` by default in `BimPipelineOptions`, or explicitly pass `UseExecutionPlan = true` in `GenerateCommand`.

---

## 4. Stress Test Results Summary

| Scenario / Test Name | Expected Behavior | Actual Behavior | Result |
|----------------------|-------------------|-----------------|--------|
| `Challenge1_Category_WireFormat_CausesAllElementsToFallIntoElseNoOp` | Plan items map to respective categories in Revit script | Wire format integer causes 100% of items to fall into `else` (NoOp) | **CONFIRMED BUG** |
| `Challenge2_ScriptTemplate_ContainsFirstValueFallback_ViolatingRule0` | Missing symbol fails element with `Failed` (Rule 0) | Script uses `.Values.FirstOrDefault()` fallback, duplicates arbitrary symbol, reports `Created` | **CONFIRMED BUG** |
| `Challenge3_PipelineResult_ReportsSuccessTrue_WhenElementsFail` | `BimPipelineResult.Success` is `false` when elements fail | `result.Success` is `true` and `ErrorMessage` is `null` | **CONFIRMED BUG** |
| `Challenge4_ScriptTemplate_OmitsSlabsOpeningsRoomsMepRebar` | Script template handles all 8 BIM stages | Slabs, Openings, Rooms, MEP, Pipes, Rebars completely missing; fall into `else` | **CONFIRMED BUG** |
| `Challenge5_PlanHash_IsNotDeterministicAcrossBuildsOfSameModel` | `PlanHash` is identical for identical model content | `PlanHash` changes on every build because `PlanId` is random | **CONFIRMED BUG** |
| `Challenge6_CompositeSourceKey_RoundTrip_FailsOnEmptyDocIdentifier` | `TryParse` parses key with empty `DocIdentifier` | `TryParse` returns `false` due to restrictive regex | **CONFIRMED BUG** |

---

## 5. Unchallenged Areas

- **Live Revit 2026 Process IPC**: Testing was performed headlessly using `FakeRevitPipeClient` and pure AST / Roslyn string verification. The actual live Named Pipe `\\.\pipe\hprebar-mcp-r2026` inside Revit 2026 was not attached, as the host Revit instance was not running during this automated challenge. However, wire DTO compatibility was verified directly against `HPRebar.Mcp.Contracts` and `HPRebar.McpBridge.Core.Scripting.ScriptArgs`.
