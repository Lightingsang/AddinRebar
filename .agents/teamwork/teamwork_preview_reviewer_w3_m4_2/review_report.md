# Review Report: Milestone 4 (M4) — Truthfulness of NoOp, Skip, and Rollback Semantics & ProvenanceHandles Preservation

**Reviewer**: Reviewer 2 (Reviewer & Adversarial Critic)  
**Date**: 2026-10-09  
**Milestone**: Wave 3 Remediation — Milestone 4 (W3-M4)  
**Target Solution**: `HPBim/HPBim.slnx`  
**Verdict**: **APPROVE**  

---

## 1. Executive Summary

Milestone 4 (M4) resolves four critical integrity and truthfulness requirements (R4-A through R4-D) in the HPBim execution pipeline and coordination ledger infrastructure:
1. **ProvenanceHandles Wire Preservation (R4-D)**: Fully preserves the `ProvenanceHandles` array across JSON serialization without breaking backward compatibility for standard string keys.
2. **Strict NoOp TargetUniqueId & Ledger Validation (R4-A)**: Rejects missing, empty, whitespace-only, or mismatched `TargetUniqueId` for `NoOp` elements with case-insensitive consistency against planned targets.
3. **Exact Skip Policy Enforcement (R4-B)**: Replaces loose substring matching with strict exact enum/code validation (`ValidSkipReasonCodes`) or explicit plan-level authorization (`SkipAllowed: true`).
4. **Truthful Post-Dispatch Rollback Semantics (R4-C)**: Accurately reflects host state by bifurcating outcomes: reports `OutcomeUnknown` with `ERR_POST_DISPATCH_INTEGRITY_DEFECT` when the host transaction was not undone (`RolledBack == false`), rather than falsely claiming transaction rollback.

All 1,053 tests across `HPBim` pass with zero failures. An independent adversarial stress test suite of 34 edge cases was constructed and executed with 100% pass rate. Strict Clean Code governance rules (C2, M3, Q-B1, Q-B2) are fully met.

---

## 2. Independent Evaluation of Core Objectives

### Objective 1: Wire Format Backward Compatibility for ProvenanceHandles
- **Mechanism**: In `CompositeSourceKeyJsonConverter.cs`, `Write` inspects `value.ProvenanceHandles != null && value.ProvenanceHandles.Count > 0`.
  - When provenance handles are present: Serializes a JSON object containing `docIdentifier`, `category`, `primaryHandle`, and a JSON array `provenanceHandles`.
  - When provenance handles are empty or null: Writes the canonical string `"[DocIdentifier]#[Category]:PrimaryHandle"` via `writer.WriteStringValue(value.ToString())`.
- **Deserialization**: `Read` supports both token types:
  - String tokens: Parses canonical `[Doc]#[Cat]:Handle` and plain `Doc#Cat:Handle` with empty provenance. Empty string returns `CompositeSourceKey.Empty`.
  - Object tokens: `ReadLegacyJsonObject` parses properties dynamically, handling `docIdentifier`, `primaryHandle`, `category` (string or integer enum), `provenanceHandles` array, and legacy `key`/`sourceKey` properties.
- **Verification**: Verified that null, empty array, non-empty array, and legacy representations roundtrip without data corruption.

### Objective 2: Dictionary Key Serialization via WriteAsPropertyName & ReadAsPropertyName
- **Constraint**: RFC 8259 mandates that JSON object property names must be string literals; JSON keys cannot be object literals.
- **Implementation**:
  - `WriteAsPropertyName`: Writes `value?.ToString() ?? string.Empty`, emitting canonical string `"[Doc]#[Category]:PrimaryHandle"`.
  - `ReadAsPropertyName`: Reads the string via `CompositeSourceKey.TryParse(raw, out var key)` (allowing empty doc identifier) and returns the parsed key or `CompositeSourceKey.Empty`.
- **Verification**: Verified that `Dictionary<CompositeSourceKey, string>` serializes keys as valid property strings, and deserialization reconstructs equal dictionary keys with full lookup capability.

### Objective 3: Case-Insensitivity & Whitespace Handling for NoOp TargetUniqueId
- **Validation**:
  - Line 68 of `BimPipelineEngine.ElementRecords.Validation.cs`: Checks `string.IsNullOrWhiteSpace(rec.TargetUniqueId)` for `Created`, `Updated`, and `NoOp`. Null, empty string, and whitespace-only (`"   "`) are rejected with `ERR_MISSING_TARGET_UNIQUE_ID`.
  - Line 79 of `BimPipelineEngine.ElementRecords.Validation.cs`: Checks `!string.Equals(rec.TargetUniqueId, item.TargetUniqueId, StringComparison.OrdinalIgnoreCase)`.
- **Behavior**:
  - Revit GUIDs with different casing (e.g. `3a334139...` vs `3A334139...`) are matched successfully without false positives.
  - Different GUIDs are rejected with `ERR_NOOP_TARGET_MISMATCH`.
  - Untrimmed whitespace in `TargetUniqueId` is treated as a mismatch under strict literal identity matching.

### Objective 4: Permitted Skip Codes & Policy
- **Whitelist**: `ValidSkipReasonCodes` defines 5 approved codes with `StringComparer.OrdinalIgnoreCase`:
  - `SKIP_FROZEN_ZONE`
  - `SKIP_STAGE_DISABLED`
  - `SKIP_PRECONDITION_FAILED`
  - `SKIP_PREREQUISITE_FAILED`
  - `SKIP_USER_EXCLUDED`
- **Enforcement**:
  - Exact match required via `ValidSkipReasonCodes.Contains(rec.ErrorCode!)`. Casual mentions in `errorReason` (e.g. `"Failed during SKIP_FROZEN_ZONE"`) are rejected with `ERR_INTEGRITY_DEFECT_SKIPPED`.
  - Custom skip reasons are permitted if and only if the plan item explicitly defines `item.Parameters["SkipAllowed"] = true`. Otherwise, unapproved skip reasons are rejected as integrity defects.

### Objective 5: Ambiguous Model State Recovery (OutcomeUnknown)
- **Problem**: When Revit commits a transaction but the client detects an integrity defect post-dispatch (such as a missing `TargetUniqueId` or unexpected skip), Revit did NOT rollback the transaction (`RolledBack == false`). Previously, reporting "Batch rolled back" was untruthful.
- **Solution**:
  - In `ResolveSingleItemOutcome`: When `hostRolledBack == false`, non-defective items are marked `Status = ElementExecutionStatus.OutcomeUnknown` with `ErrorCode = "ERR_POST_DISPATCH_INTEGRITY_DEFECT"`.
  - `ResolvePlanExecutionOutcome`: Sets `result.Success = false` and `result.IsOutcomeUnknown = true`.
  - Ledger isolation: `ledgerService.SyncFromExecutionRecords` is NOT executed, preventing corrupted mappings from polluting the ledger.
  - Reconcile integration: `ReconcileService` queries Revit Extensible Storage receipts, checks 3-factor verification, validates complete keyset coverage, and updates the ledger before marking the operation committed.

---

## 3. Adversarial Red-Team & Stress-Testing

An independent test harness (`scratch/test_m4_adversarial/Program.cs`) was compiled and executed against production `HPBim.Core` and `HPBim.Client` binaries:

| Test Group | Scenarios Tested | Result |
|---|---|---|
| **Wire Format** | Null provenance, empty array, multi-handle array, special chars, unicode (`tiếng_việt_cột`), JSON `provenanceHandles: null`, legacy object format | 7/7 PASS |
| **Dictionary Keys** | Dictionary serialization, property name string format, deserialization and key lookup with `CompositeSourceKey` | 5/5 PASS |
| **NoOp Semantics** | Empty UniqueId, whitespace-only UniqueId, case-insensitive GUID matching, mismatched GUID, untrimmed whitespace | 6/6 PASS |
| **Skip Policy** | All 5 standard codes (mixed case), casual substring in error text, custom skip with `SkipAllowed=true`, custom skip without `SkipAllowed` | 9/9 PASS |
| **Rollback & Recovery** | Unconfirmed host rollback (`RolledBack=false`), confirmed host rollback (`RolledBack=true`), defect isolation, `OutcomeUnknown` propagation | 7/7 PASS |
| **Total Adversarial Tests** | **34 scenarios** | **34/34 PASS (100%)** |

---

## 4. Clean Code Governance Compliance

Forensic audit of all files touched in Milestone 4:

| File Path | Lines (<=300) | Max Method (<=50) | Comments (Q-B1) | Catches (Q-B2) | Status |
|---|---|---|---|---|---|
| `HPBim/src/HPBim.Core/Coordination/CompositeSourceKeyJsonConverter.cs` | 155 | 43 (`ReadLegacyJsonObject`) | 0 | 0 | **COMPLIANT** |
| `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.cs` | 179 | 31 (`ReconcileRecordsStrict1To1`) | 0 | 0 unannotated | **COMPLIANT** |
| `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.Validation.cs` | 218 | 49 (`ValidateIdentityAndTargets`) | 0 | 0 | **COMPLIANT** |
| `HPBim/tests/HPBim.Core.Tests/Coordination/CompositeSourceKeyJsonConverterTests.cs` | 216 | 23 (`BimCoordinationLedgerSerializer...`) | 0 | 0 | **COMPLIANT** |
| `HPBim/tests/HPBim.Client.Tests/Pipeline/BimPipelineEngineRecordReconciliationTests.cs` | 244 | 19 (`Reconciliation_CreatedRecord...`) | 0 | 0 | **COMPLIANT** |
| `HPBim/tests/HPBim.Client.Tests/Pipeline/BimPipelineEngineRecordReconciliationTests.SkipAndRollback.cs` | 234 | 33 (`ExecutePlanAsync_IntegrityDefect...`) | 0 | 0 | **COMPLIANT** |

- **Architectural Boundary**: `HPBim.Core` retains pure `netstandard2.0` target with 0 references to `Autodesk.Revit.*`.
- **Modularity**: `BimPipelineEngine.ElementRecords.Validation.cs` cleanly partitions record validation from payload extraction.

---

## 5. Adversarial Integrity Audit

| Integrity Check | Observation | Verdict |
|---|---|---|
| **Hardcoded Test Results** | Inspected all source methods. No hardcoded GUIDs, fixed marks, or conditional test branch shortcuts. | PASS |
| **Dummy / Facade Logic** | All validations execute real condition evaluation, dictionary lookups, and regex/enum checks. | PASS |
| **Task Bypassing / Shortcuts** | Parsing handles both object and string JSON tokens; validation covers all statuses. | PASS |
| **Fabricated Verification** | Independent test runs executed live via CLI; raw test runner outputs verified. | PASS |
| **Self-Certifying Evidence** | Verified through independently authored test runner outside the worker's files. | PASS |

---

## 6. Verification Commands & Results

1. **HPBim Core Unit Tests**:
   - Command: `.\HPBim\tests\HPBim.Core.Tests\bin\Debug\net10.0\HPBim.Core.Tests.exe`
   - Output: `total: 469, failed: 0, succeeded: 469, skipped: 0, duration: 481ms`
2. **HPBim Client Unit Tests**:
   - Command: `.\HPBim\tests\HPBim.Client.Tests\bin\Debug\net10.0\HPBim.Client.Tests.exe`
   - Output: `total: 542, failed: 0, succeeded: 542, skipped: 0, duration: 3s 892ms`
3. **HPBim E2E Tests**:
   - Command: `.\HPBim\tests\HPBim.E2E.Tests\bin\Debug\net10.0\HPBim.E2E.Tests.exe`
   - Output: `total: 42, failed: 0, succeeded: 42, skipped: 0, duration: 588ms`
4. **McpShared & HPRebar Regression Tests**:
   - `HPRebar.Mcp.Server.Core.Tests.exe`: 915/915 passed
   - `HPRebar.Core.Tests.exe`: 1,682/1,682 passed

---

## 7. Review Verdict

**APPROVE**

Milestone 4 implementation is robust, authentic, and fully compliant with project standards and clean code governance.
