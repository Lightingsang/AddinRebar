# Plan: HPBim Wave 2 Remediation

## Objective
Remediate the 5 blocking P0/P1 defects identified in the 2026-10-08 re-review for HPBim:
1. Strict Record Matching & 1:1 Bijection Validation (P0) - R1
2. Journal Torn-tail Auto-Recovery (P1) - R2
3. Recovery Receipt Completeness & Coverage Verification (P1) - R3
4. CLI Ledger Wiring & Durable Ledger-First Commit Order (P1) - R4
5. Measured Geometric Engine Strict Inconclusive Threshold (P0/P1) - R5

## Execution Strategy

### Phase 0: Survey & Codebase Assessment
- Dispatch 3 parallel Explorers:
  - `explorer_w2_1`: Investigate R1 (`BimPipelineEngine.ElementRecords.cs`) and R2 (`IntentJournal.cs`).
  - `explorer_w2_2`: Investigate R3 (`ExtensibleStorageReceiptReader.cs`, `ReconcileService.cs`) and R4 (`GenerateCommand.cs`, `BimCoordinationLedgerService.cs`).
  - `explorer_w2_3`: Investigate R5 (`MeasuredGeometricEngine.Structural.cs`, `MeasuredGeometricEngine.ProfileGrid.cs`, `VerifyCommand.cs`) and baseline test suite health.
- Merge reports into consolidated remediation architecture.

### Milestone Structure
- **Milestone M1 (Pipeline Integrity & Journal Resilience)**:
  - Scope: R1 (1:1 bijection, duplicate/foreign/invalid status rejection, Created/Updated missing TargetUniqueId check, Category/Mark check, Skipped-without-reason rollback) and R2 (Torn-tail auto-recovery under _ioLock, newline trimming/padding, cold restart recovery).
  - Loop: Worker -> 2 Reviewers -> 2 Challengers -> 1 Auditor -> Gate.
- **Milestone M2 (Receipt Verification & Ledger Wiring)**:
  - Scope: R3 (ParseElementMap corrupt JSON handling, ItemCount & expected keys reconcile, Doc Identity Title/Path/VersionGuid check) and R4 (BimCoordinationLedgerService wiring into GenerateCommand, automatic TargetUniqueId lookup, Ledger-First commit order, cold rerun NoOp).
  - Loop: Worker -> 2 Reviewers -> 2 Challengers -> 1 Auditor -> Gate.
- **Milestone M3 (Measured Geometric Verification & QA/QC)**:
  - Scope: R5 (Required Measurements per Category, missing/null measurements -> Inconclusive, VerifyCommand exit code 1 on Inconclusive/Defect).
  - Loop: Worker -> 2 Reviewers -> 2 Challengers -> 1 Auditor -> Gate.
- **Final Regression & Victory Audit**:
  - Full repo test execution (3,543+ tests across HPBim, HPRebar, McpShared).
  - Final Forensic Auditor check.
  - Final completion report delivery to Sentinel.
