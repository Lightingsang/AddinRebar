# Milestone 4 Adversarial Challenge Report

**Date**: 2026-10-09  
**Challenger**: Challenger 2 (`teamwork_preview_challenger_w3_m4_2`)  
**Scope**: Milestone 4 (M4: Truthfulness of NoOp, Skip, and Rollback Semantics & ProvenanceHandles Preservation)  
**Target Projects**: `HPBim.Core`, `HPBim.Client`, `HPBim.Tests`

---

## 1. Challenge Summary

**Overall risk assessment**: LOW

Worker W3 M4 has successfully addressed all four aspects of requirement R4 (Features 10, 11, 12, 13) in accordance with the Wave 3 remediation plan:
1. **NoOp Identity & Consistency**: `ValidateReturnedRecord` requires `TargetUniqueId` on `NoOp` elements (`ERR_MISSING_TARGET_UNIQUE_ID`) and verifies consistency with ledger targets (`ERR_NOOP_TARGET_MISMATCH`).
2. **Exact Skip Policy Enforcement**: Arbitrary error reasons or substring occurrences of skip codes (e.g. `"Cannot skip: SKIP_FROZEN_ZONE disabled"`) are rejected (`ERR_INTEGRITY_DEFECT_SKIPPED`). Only exact canonical codes in `ValidSkipReasonCodes` (case-insensitive) or explicit plan permission (`item.Parameters["SkipAllowed"] == true`) are permitted.
3. **Truthful Post-Dispatch Outcome & Rollback Reporting**: When an integrity defect occurs post-dispatch and the host has not rolled back (`RolledBack == false`), non-defective elements are marked `ElementExecutionStatus.OutcomeUnknown` with `ERR_POST_DISPATCH_INTEGRITY_DEFECT`, and `result.IsOutcomeUnknown = true`. When host rolled back (`RolledBack == true`), items truthfully report `ERR_BATCH_ROLLED_BACK`.
4. **ProvenanceHandles Full Roundtrip Preservation**: `CompositeSourceKeyJsonConverter` outputs a structured JSON object whenever `ProvenanceHandles.Count > 0`, preserving 100% of provenance handles through JSON serialization/deserialization.

---

## 2. Challenges & Boundary Stress-Testing

### Challenge 1: Substring Matching Infiltration in Skip Reason Evaluation
- **Assumption Challenged**: Can unhandled exceptions or error text casually mentioning a skip token (e.g., `"Cannot skip: SKIP_FROZEN_ZONE disabled"`, `"SKIP_STAGE_DISABLED_NOW"`, `"Error evaluating SKIP_PRECONDITION_FAILED"`) bypass skip validation and masquerade as valid intentional skips?
- **Attack Scenario**: Send execution records with `status = "Skipped"` where the error reason contains a valid skip token as a substring, or where `errorCode` has prefixes, suffixes, or trailing spaces (e.g. `"SKIP_PREREQUISITE_FAILED "`).
- **Empirical Test**: Executed theory tests in `EmpiricalChallenger2M4SemanticsTests.ExecutePlanAsync_AdversarialSkipErrorReasonsAndCodes_RejectedAsIntegrityDefect` across 6 adversarial inputs.
- **Observed Behavior**: All 6 adversarial inputs were rejected with `ERR_INTEGRITY_DEFECT_SKIPPED`. When `RolledBack == false`, companion plan items were marked `ERR_POST_DISPATCH_INTEGRITY_DEFECT` and `result.IsOutcomeUnknown = true`.
- **Verdict**: PASS. The system strictly checks `rec.ErrorCode` against `ValidSkipReasonCodes` with exact equality (case-insensitive).

### Challenge 2: Post-Dispatch Integrity Defect Reporting Under `RolledBack == false`
- **Assumption Challenged**: Does the client truthfully report ambiguous transaction state without fabricating a client-side "Transaction rolled back" message when the host transaction was NOT rolled back?
- **Attack Scenario**: Submit plans where one item triggers an integrity defect (e.g. `ERR_NOOP_TARGET_MISMATCH` or unapproved skip) while another item succeeds (`status = "Created"`), with bridge response `RolledBack = false`.
- **Empirical Test**: Executed `ExecutePlanAsync_NoOpTargetMismatch_ReportsPostDispatchIntegrityDefectWhenRolledBackFalse` and `ExecutePlanAsync_SkipWithNullErrorCodeAndSubstringInReason_RejectedAsIntegrityDefect`.
- **Observed Behavior**:
  - Defective item retains exact defect code (`ERR_NOOP_TARGET_MISMATCH`).
  - Non-defective item is marked `Status = ElementExecutionStatus.OutcomeUnknown`, `ErrorCode = "ERR_POST_DISPATCH_INTEGRITY_DEFECT"`.
  - Batch result reports `Success = false` and `IsOutcomeUnknown = true`.
- **Verdict**: PASS. OutcomeUnknown is truthfully preserved for reconciliation.

### Challenge 3: Truthful Rollback Reporting Under `RolledBack == true`
- **Assumption Challenged**: When the host bridge confirms an actual transaction rollback (`RolledBack == true`), does the client report `ERR_BATCH_ROLLED_BACK` instead of `ERR_POST_DISPATCH_INTEGRITY_DEFECT`?
- **Attack Scenario**: Execute plan where one item triggers an integrity defect, but the host bridge returns `RolledBack = true`.
- **Empirical Test**: Executed `ExecutePlanAsync_IntegrityDefectsWithRolledBackTrue_ReportsBatchRolledBack`.
- **Observed Behavior**:
  - Defective item retains specific defect code (`ERR_NOOP_TARGET_MISMATCH`).
  - Companion item reports `Status = Failed`, `ErrorCode = "ERR_BATCH_ROLLED_BACK"`.
  - Batch result reports `IsOutcomeUnknown = false`, `Success = false`.
- **Verdict**: PASS. Truthful distinction between rolled back and unrolled back state is verified.

### Challenge 4: High-Arity and Special-Character ProvenanceHandles Roundtrip
- **Assumption Challenged**: Does `CompositeSourceKeyJsonConverter` withstand high handle counts, whitespace, quotes, colons, and unicode characters without truncation or corrupting JSON syntax?
- **Attack Scenario**: Roundtrip a key with 10 complex provenance handles (`"handle:0x1A4F"`, `"axis:Trục_1"`, `"cad_layer:KẾT_CẤU_CỘT"`, `"line:\"quoted_entity\""`), and serialize within an `ElementPlanItem`.
- **Empirical Test**: Executed `Roundtrip_TenProvenanceHandlesWithSymbolsAndUnicode_PreservesExactArray` and `Roundtrip_ElementPlanItemWithProvenanceHandles_PreservesSourceKeyHandles`.
- **Observed Behavior**: 100% of handles and exact array sequence were restored identically. Canonical string format is preserved when `ProvenanceHandles` is empty.
- **Verdict**: PASS.

---

## 3. Stress Test Results Matrix

| Scenario | Expected Behavior | Actual Behavior | Result |
|---|---|---|---|
| Skip reason `"Cannot skip: SKIP_FROZEN_ZONE disabled"` | Rejected as `ERR_INTEGRITY_DEFECT_SKIPPED` | Rejected as `ERR_INTEGRITY_DEFECT_SKIPPED` | PASS |
| Skip code `"SKIP_STAGE_DISABLED_NOW"` | Rejected as `ERR_INTEGRITY_DEFECT_SKIPPED` | Rejected as `ERR_INTEGRITY_DEFECT_SKIPPED` | PASS |
| Skip code `"SKIP_PREREQUISITE_FAILED "` (trailing space) | Rejected as `ERR_INTEGRITY_DEFECT_SKIPPED` | Rejected as `ERR_INTEGRITY_DEFECT_SKIPPED` | PASS |
| Skip code `"skip_frozen_zone"` (lowercase) | Accepted as valid skip | Accepted as valid skip | PASS |
| Post-dispatch defect with `RolledBack == false` | Non-defective items `ERR_POST_DISPATCH_INTEGRITY_DEFECT`, `IsOutcomeUnknown == true` | Non-defective items `ERR_POST_DISPATCH_INTEGRITY_DEFECT`, `IsOutcomeUnknown == true` | PASS |
| Post-dispatch defect with `RolledBack == true` | Non-defective items `ERR_BATCH_ROLLED_BACK`, `IsOutcomeUnknown == false` | Non-defective items `ERR_BATCH_ROLLED_BACK`, `IsOutcomeUnknown == false` | PASS |
| Multiple defective items in batch | Each defective item retains specific defect code | Specific defect codes retained | PASS |
| 10 ProvenanceHandles with unicode & symbols | 100% roundtrip equality | 100% roundtrip equality | PASS |
| `ElementPlanItem` with `ProvenanceHandles` | Preserved across serialization | Preserved across serialization | PASS |
| Full HPBim solution test suite (`dotnet test HPBim.slnx`) | 100% tests pass (1,078/1,078) | 1,078 passed, 0 failed, 0 skipped | PASS |
| Clean code audit on touched files | Files <= 300 lines, methods <= 50 lines, 0 comments | 8/8 files compliant | PASS |

---

## 4. Unchallenged Areas

- **Extensible Storage live write/read inside Revit.exe process**: In-process Revit API execution was verified via unit/contract tests and Roslyn script generation, but live Revit GUI execution is out of scope for headless test runner.
- **Large-scale background queue orchestration**: Deferred to future architectural proposals per P2 requirements.
