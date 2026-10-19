# Test Suite Coverage & Regression Analysis Report — Milestone M1 Iteration 2

**Agent**: `explorer_m1_it2_2`  
**Date**: 2026-10-08  
**Scope**: HPBim Wave 2 Remediation — Milestone M1 Iteration 2 (Test Suite Coverage, Regressions & Governance)  
**Target Solution**: `HPBim/HPBim.slnx`

---

## Executive Summary

1. **New Test for Omitted `targetUniqueId` Property**:
   - `BimPipelineEngineRecordReconciliationTests.cs` currently tests `targetUniqueId = ""` (empty string) and `targetUniqueId = (string?)null` (which serializes to `"targetUniqueId": null` and parses to `""` due to `ParseSingleElementRecord:286`). Neither test evaluates the condition where the JSON property `"targetUniqueId"` is **completely omitted**.
   - When the property is omitted, `ParseSingleElementRecord` sets `TargetUniqueId = null`. In `BimPipelineEngine.ElementRecords.cs:167`, the check `rec.TargetUniqueId != null && string.IsNullOrWhiteSpace(rec.TargetUniqueId)` evaluates to `false`, allowing elements with missing UniqueIds to bypass validation.
   - A dedicated unit test `ExecutePlanAsync_CreatedStatusWithOmittedUniqueIdProperty_RejectsBatchWithMissingTargetUniqueId` (and companion `Updated` test) must be added where the anonymous payload has no `targetUniqueId` property at all.

2. **Critical Regression Found in Existing Test Suite (`Stress4`)**:
   - In `HPBim/tests/HPBim.Client.Tests/Pipeline/EmpiricalChallengerM1Tests_Stress.cs`, lines 96–97:
     The test `Stress4_MixedBatch_FailsFast_WhenSingleElementFails` mocks a mixed batch response with 2 `Created` elements (`C1`, `C2`) and 1 `Failed` element (`B1`, `ERR_SYMBOL_NOT_FOUND`).
     Crucially, lines 96–97 **omitted the `"targetUniqueId"` property** on `C1` and `C2`!
   - Because of the bug on line 167, `C1` and `C2` were previously permitted without unique IDs, so only `B1` failed (`failCount = 1`, first failure = `ERR_SYMBOL_NOT_FOUND`).
   - **Breaking Risk**: As soon as line 167 is corrected to `string.IsNullOrWhiteSpace(rec.TargetUniqueId)`, `C1` and `C2` in `Stress4` will be immediately rejected with `ERR_MISSING_TARGET_UNIQUE_ID`. The batch failure count will become 3 instead of 1, and the first failure will become `ERR_MISSING_TARGET_UNIQUE_ID` instead of `ERR_SYMBOL_NOT_FOUND`. Lines 131–132 of `Stress4` will fail!
   - **Resolution**: Lines 96–97 of `EmpiricalChallengerM1Tests_Stress.cs` must provide valid `targetUniqueId` values (`"targetUniqueId": "uid-c1"`, `"uid-c2"`) to preserve the intended test scenario (single element failure in an otherwise valid batch).

3. **Line Count & Method Length Governance (Rules C2 & M3)**:
   - `BimPipelineEngineRecordReconciliationTests.cs` is currently **297 lines**. Adding the new tests (+25–30 lines) will exceed the 300-line limit (Rule C2).
     - **Recommendation**: Split `BimPipelineEngineRecordReconciliationTests` into two files using a `partial class`:
       - `BimPipelineEngineRecordReconciliationTests.cs` (~175 lines): 1:1 bijection, foreign, duplicate, enum, and TargetUniqueId omission/null/whitespace validation.
       - `BimPipelineEngineRecordReconciliationTests.SkipAndRollback.cs` (~125 lines): Skip reason policies, `SkipAllowed` parameter, and batch rollback propagation.
   - `BimPipelineEngine.ElementRecords.cs` is currently **298 lines**.
     - `ReconcileSingleItem` (lines 140–211) is **72 lines** (violates Rule M3 <= 50 lines).
     - When refactoring `ReconcileSingleItem` to extract `ValidateReturnedRecord`, the implementation must format record return statements compactly so that `BimPipelineEngine.ElementRecords.cs` does not breach 300 lines (proposing a refactored layout of ~274 lines).

---

## 1. Examination of `BimPipelineEngineRecordReconciliationTests.cs`

### 1.1 As-Is Analysis

The existing file `HPBim/tests/HPBim.Client.Tests/Pipeline/BimPipelineEngineRecordReconciliationTests.cs` contains 11 tests across 297 lines:
- Lines 48–68: `ForeignRecordReturned` (`ERR_FOREIGN_RECORD`)
- Lines 71–91: `DuplicateRecordsReturned` (`ERR_DUPLICATE_RECORD`)
- Lines 94–114: `InvalidStatusEnumString` (`ERR_INVALID_STATUS_ENUM`)
- Lines 117–137: `CreatedStatusWithMissingUniqueId` — payload uses `targetUniqueId = ""`
- Lines 140–155: `UpdatedStatusWithMissingUniqueId` — payload uses `targetUniqueId = (string?)null`
- Lines 158–178: `CategoryMismatch` (`ERR_CATEGORY_MISMATCH`)
- Lines 181–196: `MarkMismatch` (`ERR_MARK_MISMATCH`)
- Lines 199–219: `RequiredItemSkippedWithoutDefinedReason` (`ERR_INTEGRITY_DEFECT_SKIPPED`)
- Lines 222–240: `PlanItemSkippedWithValidDefinedReason` (preserves `Skipped`)
- Lines 243–262: `PlanItemWithSkipAllowedParameter` (preserves `Skipped`)
- Lines 265–296: `IntegrityDefectInOneItem_ExecutesAllOrNothingBatchRollback`

### 1.2 The Property Omission Gap

In `BimPipelineEngine.ElementRecords.cs`:
```csharp
285: string? targetUid = elem.TryGetProperty("targetUniqueId", out var p)
286:     ? (p.ValueKind == JsonValueKind.String ? p.GetString() : string.Empty) : null;
```
When payload specifies `targetUniqueId = (string?)null`:
- Serialized JSON: `{"targetUniqueId": null}`.
- `elem.TryGetProperty("targetUniqueId", out var p)` returns `true`.
- `p.ValueKind` is `JsonValueKind.Null` (not `JsonValueKind.String`), so the ternary operator yields `string.Empty` (`""`), NOT `null`.
- Consequently, `rec.TargetUniqueId` becomes `""`.
- In line 167:
  ```csharp
  167: if (rec.Status is ElementExecutionStatus.Created or ElementExecutionStatus.Updated && rec.TargetUniqueId != null && string.IsNullOrWhiteSpace(rec.TargetUniqueId))
  ```
  `rec.TargetUniqueId != null` evaluates to `true` (since `"" != null`), so the error block was triggered.

However, when a Revit response or bridge **omits the property entirely**:
- Serialized JSON: `{"sourceKey": "...", "status": "Created"}` (no `targetUniqueId` key).
- `elem.TryGetProperty("targetUniqueId", out var p)` returns `false`.
- `targetUid` is assigned `null`.
- `rec.TargetUniqueId` is `null`.
- In line 167: `rec.TargetUniqueId != null` evaluates to `false`!
- The check is completely bypassed, returning `Created` with `TargetUniqueId = null`.

### 1.3 Proposed New Tests

The test suite must assert that omitting the `targetUniqueId` property produces `rec.TargetUniqueId == null`, which is strictly rejected with `ERR_MISSING_TARGET_UNIQUE_ID`:

```csharp
[Fact]
public async Task ExecutePlanAsync_CreatedStatusWithOmittedUniqueIdProperty_RejectsBatchWithMissingTargetUniqueId()
{
    var plan = CreatePlan((CoordinationCategory.Column, "C1"), (CoordinationCategory.Column, "C2"));
    var payload = new object[]
    {
        // targetUniqueId property completely omitted from anonymous object
        new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Created" },
        new { sourceKey = "[Doc]#[Column]:C2", category = "Column", mark = "C2", status = "Created", targetUniqueId = "uid-2" }
    };

    var engine = new BimPipelineEngine(CreatePipeClient(payload));
    var result = await engine.ExecutePlanAsync(plan);

    Assert.False(result.Success);
    var c1 = result.ElementRecords.First(r => r.Mark == "C1");
    Assert.Equal(ElementExecutionStatus.Failed, c1.Status);
    Assert.Equal("ERR_MISSING_TARGET_UNIQUE_ID", c1.ErrorCode);
    Assert.Null(c1.TargetUniqueId);

    var c2 = result.ElementRecords.First(r => r.Mark == "C2");
    Assert.Equal(ElementExecutionStatus.Failed, c2.Status);
    Assert.Equal("ERR_BATCH_ROLLED_BACK", c2.ErrorCode);
}

[Fact]
public async Task ExecutePlanAsync_UpdatedStatusWithOmittedUniqueIdProperty_RejectsBatchWithMissingTargetUniqueId()
{
    var plan = CreatePlan((CoordinationCategory.Column, "C1"));
    var payload = new[]
    {
        // targetUniqueId property completely omitted
        new { sourceKey = "[Doc]#[Column]:C1", category = "Column", mark = "C1", status = "Updated" }
    };

    var engine = new BimPipelineEngine(CreatePipeClient(payload));
    var result = await engine.ExecutePlanAsync(plan);

    Assert.False(result.Success);
    var c1 = result.ElementRecords[0];
    Assert.Equal(ElementExecutionStatus.Failed, c1.Status);
    Assert.Equal("ERR_MISSING_TARGET_UNIQUE_ID", c1.ErrorCode);
    Assert.Null(c1.TargetUniqueId);
}
```

---

## 2. Comprehensive Test Suite & Regression Verification

### 2.1 Full Solution Test Baseline

| Test Suite | Total Tests | Passed | Failed | Duration |
|---|---|---|---|---|
| `HPBim.Client.Tests` | 406 | 406 | 0 | 3.8s |
| `HPBim.Core.Tests` | 429 | 429 | 0 | 0.5s |
| `HPBim.E2E.Tests` | 42 | 42 | 0 | 0.5s |
| **Total** | **877** | **877** | **0** | **~4.8s** |

Challenger harness (`scratch/AdversarialM1Wave2Challenger`):
- Total checks: 39
- Passed: 38
- Failed: 1 (Check 3.4: Created with targetUniqueId property completely omitted)

### 2.2 Deep Dive into Existing Tests

#### A. `Stress4` in `EmpiricalChallengerM1Tests_Stress.cs` (CRITICAL REGRESSION RISK)
- **Location**: `tests/HPBim.Client.Tests/Pipeline/EmpiricalChallengerM1Tests_Stress.cs:84–133`
- **Current code**:
  ```csharp
  var fakeResponseJson = """
  {
      "planId": "stress-plan",
      "total": 3,
      "created": 2,
      "updated": 0,
      "noOp": 0,
      "skipped": 0,
      "failed": 1,
      "records": [
          { "sourceKey": "[D]#[Column]:C1", "mark": "C1", "category": "Column", "status": "Created" },
          { "sourceKey": "[D]#[Column]:C2", "mark": "C2", "category": "Column", "status": "Created" },
          { "sourceKey": "[D]#[Beam]:B1", "mark": "B1", "category": "Beam", "status": "Failed",
            "errorCode": "ERR_SYMBOL_NOT_FOUND", "errorReason": "Beam symbol not found" }
      ]
  }
  """;
  ```
- **Assertions**:
  ```csharp
  Assert.False(result.Success, "Pipeline must report Success=false when any element fails.");
  Assert.NotNull(result.ErrorMessage);
  Assert.Contains("Execution failed for 1 element(s)", result.ErrorMessage);
  Assert.Contains("ERR_SYMBOL_NOT_FOUND", result.ErrorMessage);
  ```
- **Regression Mechanism**:
  When line 167 is fixed, `C1` and `C2` have `TargetUniqueId == null`. They are rejected with `ERR_MISSING_TARGET_UNIQUE_ID` as batch integrity defects.
  All 3 items become `Failed`.
  `result.ErrorMessage` becomes: `"Execution failed for 3 element(s). First failure: [ERR_MISSING_TARGET_UNIQUE_ID]..."`.
  Both `Assert.Contains("Execution failed for 1 element(s)", ...)` and `Assert.Contains("ERR_SYMBOL_NOT_FOUND", ...)` will FAIL!
- **Action**: Update `EmpiricalChallengerM1Tests_Stress.cs` lines 96–97 to:
  ```csharp
  { "sourceKey": "[D]#[Column]:C1", "mark": "C1", "category": "Column", "status": "Created", "targetUniqueId": "uid-c1" },
  { "sourceKey": "[D]#[Column]:C2", "mark": "C2", "category": "Column", "status": "Created", "targetUniqueId": "uid-c2" },
  ```

#### B. `SingleExecutionPlanTests.cs` (SAFE)
- `BimPipelineEngine_ExecutePlanAsync_PreviewAndApplyUseIdenticalScriptAndArgs`: Uses default `FakeRevitPipeClient` handler which auto-enriches `targetUniqueId` with `fake-uid-{counter}`.
- `BimPipelineEngine_ExecutePlanAsync_ParsesGranularElementRecords`: Explicitly specifies `"targetUniqueId": "guid-lvl-1"` for the created Level. Col is `Failed`.
- Status: **100% Safe**. Zero regression risk.

#### C. `IdempotentExecutionTests.cs` and `IdempotentExecutionStressTests.cs` (SAFE)
- Both use `SimulatedRevitStore` with:
  ```csharp
  string newUid = $"uid-{mark}-{elemId}";
  return MakeRecord(sourceKey, category, mark, "Created", newUid, elemId);
  ```
  `MakeRecord` explicitly sets `["targetUniqueId"] = newUid`.
- Status: **100% Safe**. Zero regression risk.

#### D. `EmpiricalChallengerM2RollbackTests.cs` (SAFE)
- `Challenge_Apply_WhenBridgeClaimsCreatedRecords_ButRolledBackTrue_MustFail`: Lines 118–125 mock a record without `targetUniqueId`.
- However, `execRes.RolledBack = true` and `plan.Options.DryRun = false`.
- In `ResolvePlanExecutionOutcome:132–135`, the branch `!plan.Options.DryRun && execRes.RolledBack` takes precedence over element failure messages and sets `errorMsg = "Apply execution failed: Transaction was unexpectedly rolled back by Revit bridge."`.
- Line 142 asserts `Assert.Contains("unexpectedly rolled back", result.ErrorMessage)`, which continues to pass.
- Recommended: Add `targetUniqueId = "uid-1"` to keep mock data clean.

---

## 3. Line Count & Governance Compliance (Rules C2 & M3)

### 3.1 Line Count Inventory

| File | Current Lines | Rule C2 Limit | Headroom | Impact of Changes |
|---|---|---|---|---|
| `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.cs` | **298** | <= 300 | 2 lines | High risk of breach during M3 refactor |
| `HPBim/tests/HPBim.Client.Tests/Pipeline/BimPipelineEngineRecordReconciliationTests.cs` | **297** | <= 300 | 3 lines | Guaranteed breach (+30 lines) if not split/compacted |
| `HPBim/src/HPBim.Client/Connection/IntentJournal.cs` | 271 | <= 300 | 29 lines | Safe |
| `HPBim/tests/HPBim.Client.Tests/Connection/IntentJournalAdversarialTests.cs` | 288 | <= 300 | 12 lines | Safe |

### 3.2 File Splitting Strategy for Test Class

To prevent violating Rule C2 (<= 300 lines), `BimPipelineEngineRecordReconciliationTests` should be split into two partial class files in `HPBim/tests/HPBim.Client.Tests/Pipeline/`:

#### File 1: `BimPipelineEngineRecordReconciliationTests.cs` (~175 lines)
Scope: Fixture setup, helper methods, 1:1 bijection, foreign, duplicate, enum, and TargetUniqueId omission/null/whitespace validation.
- `CreatePlan`, `CreatePipeClient`
- `ForeignRecordReturned`
- `DuplicateRecordsReturned`
- `InvalidStatusEnumString`
- `CreatedStatusWithMissingUniqueId` (empty string)
- `CreatedStatusWithOmittedUniqueIdProperty` (omitted property) **[NEW]**
- `UpdatedStatusWithMissingUniqueId` (explicit null)
- `UpdatedStatusWithOmittedUniqueIdProperty` (omitted property) **[NEW]**
- `CategoryMismatch`
- `MarkMismatch`

#### File 2: `BimPipelineEngineRecordReconciliationTests.SkipAndRollback.cs` (~125 lines)
Scope: Skip reason policy, SkipAllowed overrides, and all-or-nothing batch rollback.
- `RequiredItemSkippedWithoutDefinedReason`
- `PlanItemSkippedWithValidDefinedReason`
- `PlanItemWithSkipAllowedParameter`
- `IntegrityDefectInOneItem_ExecutesAllOrNothingBatchRollback`

Both files will be between 120 and 180 lines, providing large headroom (> 40%) under the 300-line ceiling.

### 3.3 Method Refactoring Strategy for `ReconcileSingleItem` (Rule M3)

In `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.cs`:
- Method `ReconcileSingleItem` (lines 140–211) is 72 lines.
- Rule M3 requires methods <= 50 lines.
- In addition, the file itself is 298 lines (2 lines under the 300-line ceiling).
- By extracting validation into `ValidateReturnedRecord` and utilizing concise return expressions, the method lengths and total file size are optimized simultaneously:

```csharp
private static ElementExecutionRecord ReconcileSingleItem(
    ElementPlanItem item,
    Queue<ElementExecutionRecord>? queue,
    bool isFailedRollback)
{
    if (queue != null && queue.Count > 0)
    {
        var rec = queue.Dequeue();
        return ValidateReturnedRecord(item, rec, isFailedRollback);
    }

    if (isFailedRollback)
    {
        return new ElementExecutionRecord
        {
            SourceKey = item.SourceKey, Category = item.Category, Mark = item.Mark,
            Status = ElementExecutionStatus.Failed, ErrorCode = "ERR_BATCH_ROLLED_BACK",
            ErrorReason = $"Transaction rolled back before plan item '{item.Mark}' ({item.SourceKey}) could be committed."
        };
    }

    return ElementExecutionRecord.OutcomeUnknown(
        item.SourceKey, item.Category, item.Mark,
        $"No execution record returned from Revit for plan item '{item.Mark}' ({item.SourceKey}).",
        "ERR_RECORD_DEFICIT");
}

private static ElementExecutionRecord ValidateReturnedRecord(
    ElementPlanItem item, ElementExecutionRecord rec, bool isFailedRollback)
{
    if (rec.Category != item.Category)
        return rec with { Status = ElementExecutionStatus.Failed, ErrorCode = "ERR_CATEGORY_MISMATCH", ErrorReason = $"Category mismatch for '{item.SourceKey}': expected '{item.Category}', returned '{rec.Category}'." };

    if (!string.Equals(rec.Mark, item.Mark, StringComparison.OrdinalIgnoreCase))
        return rec with { Status = ElementExecutionStatus.Failed, ErrorCode = "ERR_MARK_MISMATCH", ErrorReason = $"Mark mismatch for '{item.SourceKey}': expected '{item.Mark}', returned '{rec.Mark}'." };

    if (rec.Status is ElementExecutionStatus.Created or ElementExecutionStatus.Updated && string.IsNullOrWhiteSpace(rec.TargetUniqueId))
        return rec with { Status = ElementExecutionStatus.Failed, ErrorCode = "ERR_MISSING_TARGET_UNIQUE_ID", ErrorReason = $"Element '{item.Mark}' with status '{rec.Status}' MUST have a non-empty TargetUniqueId." };

    if (rec.Status == ElementExecutionStatus.Skipped && !IsSkipAllowed(item, rec))
        return rec with { Status = ElementExecutionStatus.Failed, ErrorCode = "ERR_INTEGRITY_DEFECT_SKIPPED", ErrorReason = $"Required plan item '{item.Mark}' was skipped without a valid defined reason: '{rec.ErrorReason ?? rec.ErrorCode}'." };

    if (isFailedRollback && rec.Status is ElementExecutionStatus.Created or ElementExecutionStatus.Updated)
        return rec with { Status = ElementExecutionStatus.Failed, ErrorCode = "ERR_BATCH_ROLLED_BACK", ErrorReason = "Transaction rolled back due to failure in batch." };

    return rec;
}
```

**Metrics After Refactor**:
- `ReconcileSingleItem`: 25 lines (PASS <= 50)
- `ValidateReturnedRecord`: 25 lines (PASS <= 50)
- `BimPipelineEngine.ElementRecords.cs` total lines: **~274 lines** (PASS <= 300, 26 lines of headroom gained!)
- Defect on line 167 fixed: `string.IsNullOrWhiteSpace(rec.TargetUniqueId)` correctly catches `null`, `""`, and whitespace.

---

## 4. Summary Table of Planned Fixes & Touchpoints

| Item | Target File | Issue | Action Required |
|---|---|---|---|
| 1 | `BimPipelineEngine.ElementRecords.cs:167` | `rec.TargetUniqueId != null &&` bypasses null | Remove `rec.TargetUniqueId != null &&`, use `string.IsNullOrWhiteSpace(rec.TargetUniqueId)` |
| 2 | `BimPipelineEngine.ElementRecords.cs:140–211` | Method is 72 lines (violates Rule M3) | Extract `ValidateReturnedRecord` helper, keeping file <= 300 lines |
| 3 | `EmpiricalChallengerM1Tests_Stress.cs:96–97` | `Stress4` payload lacks `targetUniqueId` | Add `"targetUniqueId": "uid-c1"` and `"uid-c2"` to prevent regression |
| 4 | `BimPipelineEngineRecordReconciliationTests.cs` | 297 lines (cannot fit new tests) | Split into two partial class files (`*.cs` + `*.SkipAndRollback.cs`) |
| 5 | `BimPipelineEngineRecordReconciliationTests.cs` | Missing test for omitted property | Add `Created` and `Updated` omitted property tests |
