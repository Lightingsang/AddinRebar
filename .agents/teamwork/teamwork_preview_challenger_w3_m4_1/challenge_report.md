# Adversarial Challenge Report: Milestone 4 (W3-M4)
**Focus**: Truthfulness of NoOp, Skip, and Rollback Semantics & ProvenanceHandles Preservation (R4)  
**Agent**: Challenger 1 (critic, specialist)  
**Date**: 2026-10-09  

---

## Challenge Summary

**Overall risk assessment**: **LOW** (Empirically Verified & Robust)

Milestone 4 implementation by Worker W3-M4 has been subjected to exhaustive empirical stress testing using a custom adversarial test harness (`scratch/m4_adversarial_harness/Program.cs`) comprising 71 specialized attack vectors and edge cases, supplemented by targeted and full test suite executions, clean code forensic scanning, and regression testing across the broader codebase.

All claims made by Worker M4 regarding ProvenanceHandles preservation, NoOp strict validation, standardized exact Skip policy, truthful post-dispatch defect bifurcation, and clean code governance have been **empirically proven and verified**.

---

## Adversarial Challenge Dimensions & Empirical Results

### 1. Dimension 1: ProvenanceHandles Serialization & Deserialization Roundtrip

#### Challenge Hypotheses & Attack Scenarios
- **Scenario 1.1 (High Volume Arrays)**: 100 entity handles (`Handle_Entity_0001` ... `Handle_Entity_0100`). Verified that 100/100 handles are preserved without truncation, reordering, or data loss.
- **Scenario 1.2 (Duplicate Handles)**: Repeated handles `["H1", "H1", "H2", "H3", "H2"]`. Verified that duplicate handles and exact array sequence are preserved 100% without deduplication side-effects.
- **Scenario 1.3 (Empty & Whitespace Handle Strings)**: Mixed array containing `""` and `"   "`. Verified that empty/whitespace handle items are preserved faithfully.
- **Scenario 1.4 (Special Characters & Escapes)**: Handles containing quotes (`"`), backslashes (`\`), slashes (`/`), composite key syntax (`[Doc]#[Col]:123`), Vietnamese accents (`Cột_Tầng_1_Đài_Móng_ĐC01_tiết_diện_220x220`), newlines (`\n`), tabs (`\t`), and nested JSON strings (`{"json":"nested"}`). Verified exact roundtrip byte fidelity.
- **Scenario 1.5 (Null vs Empty Collection)**: Tested `ProvenanceHandles = null` and `ProvenanceHandles = Array.Empty<string>()`. Both serialize to the canonical string `"[Doc]#[Category]:PrimaryHandle"` and deserialize to an empty collection, maintaining backward wire-format compatibility.
- **Scenario 1.6 (Heterogeneous JSON Array Resilience)**: Injected numbers, booleans, nulls, and nested objects into the `provenanceHandles` array (`["valid_h1", 12345, null, true, {"foo":"bar"}, "valid_h2"]`). Verified that `CompositeSourceKeyJsonConverter` gracefully filters non-string tokens and safely retains only valid strings (`valid_h1`, `valid_h2`).
- **Scenario 1.7 (Property Name Case-Insensitivity)**: Tested root properties in UPPERCASE (`DOCIDENTIFIER`, `CATEGORY`, `PRIMARYHANDLE`, `PROVENANCEHANDLES`). Deserialized accurately with ordinal case-insensitivity.
- **Scenario 1.8 (Malformed String Formatting)**: Deserializing unparseable string `"completely-invalid-key-no-category"` strictly throws `JsonException`. Deserializing whitespace string returns `CompositeSourceKey.Empty`.
- **Scenario 1.9 (Dictionary Property Name & Core Identity Equality)**: Serialized `Dictionary<CompositeSourceKey, string>`. Verified that dictionary lookup matches by core identity `(DocIdentifier, Category, PrimaryHandle)` regardless of runtime provenance instance differences, preserving hash table semantics while retaining provenance handles on direct object roundtrip.

**Empirical Result**: **18/18 PASS** in Stress Harness.

---

### 2. Dimension 2: NoOp Semantics and TargetUniqueId Enforcement

#### Challenge Hypotheses & Attack Scenarios
- **Scenario 2.1 (Missing TargetUniqueId on NoOp)**: Returned record with `Status = NoOp` and `targetUniqueId = null` or `""` or `"   "`. Correctly rejected with `rec.Status = Failed` and `rec.ErrorCode = "ERR_MISSING_TARGET_UNIQUE_ID"`.
- **Scenario 2.2 (Valid TargetUniqueId on NoOp Without Pre-assigned Target)**: When plan item did not have a pre-assigned `TargetUniqueId` and host returns `Status = NoOp` with a valid `targetUniqueId = "revit-unique-id-999"`, record is accepted as `NoOp` and `TargetUniqueId` is preserved.
- **Scenario 2.3 (Mismatched TargetUniqueId on NoOp)**: Plan item specifies `TargetUniqueId = "expected-uid-100"`. Returned record has `Status = NoOp` but `targetUniqueId = "different-uid-200"`. Correctly rejected with `rec.Status = Failed` and `rec.ErrorCode = "ERR_NOOP_TARGET_MISMATCH"`.
- **Scenario 2.4 (Case-Insensitive TargetUniqueId Matching)**: Plan item specifies `"expected-uid-100"`, returned record has `"EXPECTED-UID-100"`. Successfully matched and accepted as `NoOp` (case-insensitivity prevents false rejections due to GUID casing).
- **Scenario 2.5 (Ledger Pre-population Integration)**: Tested pipeline execution with `BimCoordinationLedgerService`. When ledger maps `[Doc]#[Column]:C1` to `ledger-known-uid-42`:
  - Returned NoOp with `some-other-uid-99` is rejected with `ERR_NOOP_TARGET_MISMATCH`.
  - Returned NoOp with `ledger-known-uid-42` is accepted with `Status = NoOp`.
- **Scenario 2.6 (Category and Mark Mismatch Guards)**: Returned records with mismatched `Category` (`ERR_CATEGORY_MISMATCH`) or mismatched `Mark` (`ERR_MARK_MISMATCH`) are strictly rejected.

**Empirical Result**: **19/19 PASS** in Stress Harness.

---

### 3. Dimension 3: Skipped Semantics and Whitelist Policy

#### Challenge Hypotheses & Attack Scenarios
- **Scenario 3.1 (Whitelisted Skip Codes)**:
  - `SKIP_FROZEN_ZONE` -> PASS (Skipped)
  - `SKIP_STAGE_DISABLED` -> PASS (Skipped)
  - `SKIP_PRECONDITION_FAILED` -> PASS (Skipped)
  - `SKIP_PREREQUISITE_FAILED` -> PASS (Skipped)
  - `SKIP_USER_EXCLUDED` -> PASS (Skipped)
  - Case variations (`skip_frozen_zone`, `Skip_Stage_Disabled`) -> PASS (Skipped)
- **Scenario 3.2 (Non-whitelisted Skip Codes)**:
  - `SKIP_UNAPPROVED_REASON`, `ERR_TIMEOUT`, `SKIP_CUSTOM`, `ERR_SKIP_FROZEN_ZONE_FAILED`, `""`, `"   "` -> All rejected as `ERR_INTEGRITY_DEFECT_SKIPPED`.
- **Scenario 3.3 (Sub-string Bypass Elimination)**: Injected unhandled error with `errorCode = "ERR_INTERNAL_ERROR"` and `errorReason = "Error occurred inside SKIP_FROZEN_ZONE handler"`. The loose `.Contains()` loophole was completely closed: correctly rejected as `ERR_INTEGRITY_DEFECT_SKIPPED`.
- **Scenario 3.4 (Plan Item SkipAllowed Override)**: Plan item explicitly configured with `Parameters["SkipAllowed"] = true` permits custom engineering skip reasons without failing batch integrity.

**Empirical Result**: **15/15 PASS** in Stress Harness.

---

### 4. Dimension 4: Truthful Rollback & Post-Dispatch Defect Bifurcation

#### Challenge Hypotheses & Attack Scenarios
- **Scenario 4.1 (Post-Dispatch Defect with HostRolledBack == false)**:
  - Host executed transaction without host rollback (`RolledBack == false`). Client detects an integrity defect (e.g. NoOp missing TargetUniqueId).
  - Defective item reports its specific defect (`ERR_MISSING_TARGET_UNIQUE_ID`).
  - Other valid items report `Status = ElementExecutionStatus.OutcomeUnknown` and `ErrorCode = "ERR_POST_DISPATCH_INTEGRITY_DEFECT"`.
  - Plan outcome: `Success = false`, `RolledBack = false`, `IsOutcomeUnknown = true`.
  - **Truthfulness verified**: System NEVER falsely reports "Transaction rolled back" when host did not roll back!
- **Scenario 4.2 (Post-Dispatch Defect with HostRolledBack == true)**:
  - Host bridge confirms rollback (`RolledBack == true`).
  - Defective item reports specific defect; other items report `ERR_BATCH_ROLLED_BACK`.
  - Plan outcome: `Success = false`, `RolledBack = true`, `IsOutcomeUnknown = false`.
- **Scenario 4.3 (Foreign & Duplicate Records)**:
  - Foreign record: Batch rejected with `ERR_FOREIGN_RECORD`, `Success = false`.
  - Duplicate records: Batch rejected with `ERR_DUPLICATE_RECORD`, `Success = false`.
- **Scenario 4.4 (Multiple Distinct Defects in One Batch)**:
  - Batch contains Item 1 (Valid), Item 2 (NoOp missing UID), Item 3 (NoOp mismatched UID).
  - Both Item 2 (`ERR_MISSING_TARGET_UNIQUE_ID`) and Item 3 (`ERR_NOOP_TARGET_MISMATCH`) retain their distinct defect codes.
  - Item 1 truthfully reports `ERR_POST_DISPATCH_INTEGRITY_DEFECT` / `OutcomeUnknown`.
- **Scenario 4.5 (Response Truncation)**: Truncated response (>64KB) cleanly sets all items to `OutcomeUnknown` with `ERR_RESPONSE_TRUNCATED`.

**Empirical Result**: **19/19 PASS** in Stress Harness.

---

### 5. Dimension 5: Clean Code Governance & Purity Audit

Executed forensic scan script `scratch/check_m4_clean_code.py` across touched production and test files:
- `HPBim/src/HPBim.Core/Coordination/CompositeSourceKeyJsonConverter.cs`: 155 lines (Rule C2 <= 300 PASS), 0 comments (Rule Q-B1 PASS), 0 empty catch (Rule Q-B2 PASS)
- `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.cs`: 179 lines (Rule C2 <= 300 PASS), 0 comments (Rule Q-B1 PASS), 0 empty catch (Rule Q-B2 PASS)
- `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.Validation.cs`: 218 lines (Rule C2 <= 300 PASS), 0 comments (Rule Q-B1 PASS), 0 empty catch (Rule Q-B2 PASS)
- `HPBim/tests/HPBim.Core.Tests/Coordination/CompositeSourceKeyJsonConverterTests.cs`: 216 lines (Rule C2 <= 300 PASS), 0 comments (Rule Q-B1 PASS), 0 empty catch (Rule Q-B2 PASS)
- `HPBim/tests/HPBim.Client.Tests/Pipeline/BimPipelineEngineRecordReconciliationTests.cs`: 244 lines (Rule C2 <= 300 PASS), 0 comments (Rule Q-B1 PASS), 0 empty catch (Rule Q-B2 PASS)
- `HPBim/tests/HPBim.Client.Tests/Pipeline/BimPipelineEngineRecordReconciliationTests.SkipAndRollback.cs`: 234 lines (Rule C2 <= 300 PASS), 0 comments (Rule Q-B1 PASS), 0 empty catch (Rule Q-B2 PASS)
- **Method length check (Rule M3 <= 50 lines)**: All methods verified <= 50 lines.
- **Reference Purity**: `HPBim.Core` has zero `Autodesk.Revit.*` references (pure `netstandard2.0`). `HPBim.Client` has zero direct `Autodesk.Revit.*` references.

---

## Stress Test Results Matrix

| Dimension | Scenarios Tested | Passed | Failed | Status |
|---|---|---|---|---|
| ProvenanceHandles Roundtrip & Serialization | 18 | 18 | 0 | **PASS** |
| NoOp Validation & TargetUniqueId Enforcement | 19 | 19 | 0 | **PASS** |
| Skipped Semantics & Whitelist Reason Policy | 15 | 15 | 0 | **PASS** |
| Post-Dispatch Defect Bifurcation & Truthful Rollback | 19 | 19 | 0 | **PASS** |
| Clean Code Governance (C2, M3, Q-B1, Q-B2, netstandard2.0) | 8 checks | 8 | 0 | **PASS** |
| **Total Adversarial Suite** | **71** | **71** | **0** | **100% PASS** |

---

## Targeted & Full Solution Test Execution

1. `HPBim.Core.Tests`:
   - Command: `dotnet run --project HPBim/tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj`
   - Total: 469, Failed: 0, Succeeded: 469, Skipped: 0 (431 ms)
2. `HPBim.Client.Tests`:
   - Command: `dotnet run --project HPBim/tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj`
   - Total: 542, Failed: 0, Succeeded: 542, Skipped: 0 (4,136 ms)
3. `HPBim` Full Suite (`dotnet test` in `HPBim/`):
   - Total: 1,053, Failed: 0, Succeeded: 1,053, Skipped: 0 (5,425 ms)
4. Broader Codebase Regression Checks:
   - `HPRebar.Core.Tests`: 1,682 tests passed (3,048 ms)
   - `HPRebar.Mcp.Server.Tests`: 118 tests passed (7,874 ms)

---

## Verdict

**APPROVE** — All R4 requirements are implemented with complete semantic truthfulness, rigorous boundary checks, robust JSON serialization fidelity, and clean code excellence.
