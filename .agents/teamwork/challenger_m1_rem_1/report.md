# Empirical Challenge Report — Milestone M1 Remediation (R1.1 & R1.2)

## Challenge Summary

**Overall risk assessment**: LOW  
**Verdict**: **APPROVE**  
**Evaluator**: `challenger_m1_rem_1` (Empirical Challenger)  
**Target Scope**: 
- R1.1: Canonical String SourceKey (`[DocIdentifier]#[Category]:[PrimaryHandle]`)
- R1.2: 1:1 Cardinality & Truncation Deficit Handling

All empirical stress tests and boundary challenge suites for R1.1 and R1.2 passed with 100% green tests. Truncation and deficit handling strictly enforce `OutcomeUnknown` with exact error codes, maintain exact 1:1 cardinality with zero synthetic `Created` status records, and guarantee canonical string roundtrip equality.

---

## Challenges

### [Low] Challenge 1: SourceKey Wire and Roundtrip Equality Under Complex/Special Characters
- **Assumption challenged**: The canonical format `[DocIdentifier]#[Category]:[PrimaryHandle]` remains lossless across JSON serialization, string formatting, `ScriptArgs`, dictionary lookup, and receipts, even when handling complex doc identifiers (e.g., path-like names, timestamps, dots, hyphens) or handles containing special characters (colons, hex numbers, dashes).
- **Attack scenario**: Formatted keys with names like `Drawing_01.dwg`, `TowerA_Floor03`, `MoHinh_Tang1`, `Model-2026.04` with handles like `0x4A1F`, `B-101:Span-A`, `Slab.Main.01` subjected to roundtrip serialization via `JsonSerializer`, `BridgeJson.ToElement`, and simulated Extensible Storage receipts. Also tested empty document identifiers (`[]#[Opening]:Door-101`).
- **Blast radius**: If `CompositeSourceKey` fails roundtrip formatting or regex parsing, element identity across execution plans, bridge scripts, and ledger mappings would desynchronize or fail with `FormatException`.
- **Stress test observation**:
  - `CompositeSourceKeyJsonConverter` serializes directly to the canonical string format `"[Doc]#[Category]:Handle"`.
  - Regex pattern `^\[([^\]]*)\]\s*#\s*\[([^\]]+)\]\s*:\s*(.+)$` cleanly separates doc identifier, category, and handle.
  - Roundtrip through `JsonSerializer.Serialize` -> `JsonSerializer.Deserialize` maintains strict object equality (`IEquatable<CompositeSourceKey>`), case-insensitive hash codes, and exact property values.
  - Wire serialization into `BimExecutionPlan.Items[0].SourceKey` produces the exact canonical string for `BimPlanRunnerScriptTemplate`.
- **Verdict**: **PASS (Robust)**

### [Critical] Challenge 2: 64KB Truncation Simulation (`execRes.Truncated == true`)
- **Assumption challenged**: When the named-pipe transport buffer limit is exceeded and `execRes.Truncated == true`, the engine must NEVER report success and must mark 100% of planned elements as `OutcomeUnknown` with code `ERR_RESPONSE_TRUNCATED`.
- **Attack scenario**: Executed a 10-element plan (`C1..C10`) with `execRes.Truncated = true` and `execRes.IsError = false` returning arbitrary truncated string payload.
- **Blast radius**: If any element were marked `Created` or if `result.Success` were `true`, the client would incorrectly assume elements exist in Revit when they may not have been created, causing silent data divergence.
- **Stress test observation**:
  - `BimPipelineEngine.ElementRecords.cs` (line 15) intercepts `if (execRes.Truncated)` and calls `CreateBatchUniformRecords(plan, ElementExecutionStatus.OutcomeUnknown, "ERR_RESPONSE_TRUNCATED", ...)`.
  - `BimPipelineEngine.PlanExecution.cs` (lines 108-114) forces `success = false`, `isOutcomeUnknown = true`, and sets `errorMsg`.
  - Empirical verification confirmed:
    - `result.Success == false`
    - `result.IsOutcomeUnknown == true`
    - `result.CreatedCount == 0`
    - `result.OutcomeUnknownCount == 10` (100% of items)
    - All 10 element records had `Status == ElementExecutionStatus.OutcomeUnknown` and `ErrorCode == "ERR_RESPONSE_TRUNCATED"`.
- **Verdict**: **PASS (Robust)**

### [High] Challenge 3: Deficit Resolution and 1:1 Cardinality Enforcement
- **Attack scenario**: Executed a plan with 5 items (`B1..B5`), but the bridge returned only 2 records (`B1` and `B2` Created). Simulated missing 3 records (`B3`, `B4`, `B5`).
- **Blast radius**: If missing items are omitted from `ElementRecords`, cardinality collapses (5 planned, 2 reported), breaking the ledger. If missing items are synthesized as `Created` or `Success`, false positive reporting occurs.
- **Stress test observation**:
  - `ReconcileRecordsStrict1To1` uses a `Dictionary<CompositeSourceKey, Queue<ElementExecutionRecord>>` mapped by plan item `SourceKey`.
  - Present records (`B1`, `B2`) are dequeued with their returned `Created` status.
  - Missing records (`B3`, `B4`, `B5`) are reconciled as `ElementExecutionRecord.OutcomeUnknown` with `ErrorCode = "ERR_RECORD_DEFICIT"`.
  - `result.ElementRecords.Count == 5` (exact 1:1 cardinality preserved).
  - `result.Success == false`, `result.IsOutcomeUnknown == true`.
  - `result.CreatedCount == 2`, `result.OutcomeUnknownCount == 3`.
- **Verdict**: **PASS (Robust)**

### [High] Challenge 4: Zero Records Returned (Empty Response / Value == null)
- **Attack scenario**: Executed a 4-item plan with (1) `Value = null`, and (2) `Value = {"records": []}` with `AutoEnrichRecords = false`.
- **Blast radius**: Ensuring that an empty or missing records response never synthesizes a `Created` state.
- **Stress test observation**:
  - For `Value = null`: All 4 items are reconciled as `OutcomeUnknown` with `ErrorCode = "ERR_NO_RECORDS"`.
  - For `Value = {"records": []}`: All 4 items are reconciled as `OutcomeUnknown` with `ErrorCode = "ERR_RECORD_DEFICIT"`.
  - In both scenarios, `result.CreatedCount == 0`, `result.Success == false`, `result.IsOutcomeUnknown == true`.
- **Verdict**: **PASS (Robust)**

### [High] Challenge 5: Deficit Handling on Failed Rollback
- **Attack scenario**: Executed a 3-item plan where transaction failed and rolled back (`RolledBack == true`, `IsError == true`), returning only 1 record (`C1` Created).
- **Blast radius**: If `C1` remained `Created` despite batch rollback, the caller would register an uncommitted element.
- **Stress test observation**:
  - `ReconcileRecordsStrict1To1` detects `isFailedRollback == true`.
  - Preceding returned record `C1` is downgraded from `Created` to `Failed` with `ErrorCode = "ERR_BATCH_ROLLED_BACK"`.
  - Deficit missing items `C2` and `C3` are recorded as `Failed` with `ErrorCode = "ERR_BATCH_ROLLED_BACK"`.
  - `result.ElementRecords.Count == 3` (1:1 preserved).
  - `result.CreatedCount == 0`, `result.FailedCount == 3`.
- **Verdict**: **PASS (Robust)**

### [Medium] Challenge 6: Stress Scale (500 Items, 250 Deficit)
- **Attack scenario**: Submitted a large plan of 500 slabs, returning only 250 records.
- **Stress test observation**:
  - `result.ElementRecords.Count == 500` (exact 1:1 cardinality).
  - `result.CreatedCount == 250`.
  - `result.OutcomeUnknownCount == 250`.
  - `result.FailedCount == 0`.
  - `result.Success == false`, `result.IsOutcomeUnknown == true`.
  - Execution completed in sub-second time with zero allocation bottleneck.
- **Verdict**: **PASS (Robust)**

---

## Stress Test Results

| # | Scenario | Expected Behavior | Actual Behavior | Result |
|---|---|---|---|---|
| 1 | Canonical SourceKey complex string roundtrip | Strict equality across serialization, parsing, hash codes | Lossless roundtrip, exact match | **PASS** |
| 2 | Canonical SourceKey wire into `ScriptArgs` & receipts | String `[Doc]#[Category]:Handle` in wire JSON & receipt dictionary | Matches canonical string verbatim | **PASS** |
| 3 | Case-insensitive SourceKey dictionary lookups | `doca#gradebeam:gb-01` finds `DocA#GradeBeam:GB-01` in ledger & dict | Match found, UID retrieved | **PASS** |
| 4 | Empty doc identifier loose parsing | `[]#[Opening]:Door-101` parsed with `allowEmptyDoc=true` | Parsed successfully, roundtrips | **PASS** |
| 5 | Malformed SourceKey string | `CompositeSourceKey.TryParse` returns `false` safely | Returned `false`, `null` out | **PASS** |
| 6 | 64KB Truncation simulation (`Truncated == true`) | All 10 items marked `OutcomeUnknown` with `ERR_RESPONSE_TRUNCATED`, `Success == false` | 10 `OutcomeUnknown`, 0 `Created`, `Success == false` | **PASS** |
| 7 | Deficit resolution (5 items, 2 records returned) | Exactly 5 records returned, 3 missing marked `OutcomeUnknown` with `ERR_RECORD_DEFICIT` | 5 records, 2 `Created`, 3 `OutcomeUnknown`, `ERR_RECORD_DEFICIT` | **PASS** |
| 8 | Null response payload (`Value == null`) | All 4 items marked `OutcomeUnknown` with `ERR_NO_RECORDS`, `CreatedCount == 0` | 4 `OutcomeUnknown`, 0 `Created`, `Success == false` | **PASS** |
| 9 | Empty records array (`records: []`) | All 4 items marked `OutcomeUnknown` with `ERR_RECORD_DEFICIT`, `CreatedCount == 0` | 4 `OutcomeUnknown`, 0 `Created`, `Success == false` | **PASS** |
| 10 | Deficit during failed transaction rollback | Preceding items downgraded to `Failed` (`ERR_BATCH_ROLLED_BACK`), deficit marked `Failed` | All 3 `Failed`, 0 `Created`, `Success == false` | **PASS** |
| 11 | Stress scale (500 items, 250 deficit) | Exact 1:1 cardinality (500 records), 250 `Created`, 250 `OutcomeUnknown` | 500 records, 250 `Created`, 250 `OutcomeUnknown` | **PASS** |

---

## Unchallenged Areas

- **Extensible Storage live Revit interaction**: Verified via headless Roslyn script string template inspection and offline fake schema tests. Live Revit process testing is deferred to end-to-end integration environments with Revit 2026 attached.
- **R1.3 and R1.4 (Strict Batch Rollback & Heuristic Elimination)**: Handled by parallel challenger `challenger_m1_rem_2`.

---

## Verdict

**APPROVE** — The implementation of R1.1 and R1.2 in Milestone M1 Remediation is empirically verified, robust, and mathematically sound.
