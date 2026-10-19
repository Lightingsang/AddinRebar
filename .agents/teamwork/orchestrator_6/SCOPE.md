# Scope: HPBim Wave 2 Remediation (5 P0/P1 Blocking Defects)

## Architecture
- Target Project: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\`
- Core Domain: `HPBim/HPBim.Core/` (`netstandard2.0` - pure logic, zero Revit references)
  - `Journal/` (`IntentJournal.cs`, `BimCoordinationLedgerService.cs`, `BimCoordinationLedgerSerializer.cs`)
  - `Coordination/` (`ElementPlanItem.cs`, `BimExecutionPlan.cs`, `CompositeSourceKey.cs`)
  - `Verification/` (`MeasuredGeometricEngine.cs`, `MeasuredGeometricEngine.Structural.cs`, `MeasuredGeometricEngine.ProfileGrid.cs`)
- Client: `HPBim/HPBim.Client/` (.NET 8.0/.NET 10.0)
  - `Execution/` (`BimPipelineEngine.ElementRecords.cs`, `ReconcileService.cs`, `ExtensibleStorageReceiptReader.cs`)
  - `Commands/` (`GenerateCommand.cs`, `VerifyCommand.cs`)
- Tests:
  - `HPBim/HPBim.Core.Tests/`
  - `HPBim/HPBim.Client.Tests/`
  - `HPBim/HPBim.E2E.Tests/`

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | R1: Strict 1:1 Bijection & Record Validation | ReconcileRecordsStrict1To1: reject foreign, duplicate, invalid status enum, Created/Updated missing TargetUniqueId, Category/Mark mismatch; Skipped without reason -> Integrity Defect & rollback. | M1 | ORIGINAL_REQUEST R1 |
| 2 | R2: Journal Torn-tail Auto-Recovery | IntentJournal.cs: check newline before append under _ioLock, quarantine/trim corrupt tail, ensure 100% recovery after crash. | M1 | ORIGINAL_REQUEST R2 |
| 3 | R3: Recovery Receipt Completeness & Coverage | ParseElementMap returns null/error on corrupt JSON; ApplyCommittedReceiptAsync validates ItemCount & expected keys vs original intent; validates Title, PathName, VersionGuid/CentralGuid. | M2 | ORIGINAL_REQUEST R3 |
| 4 | R4: CLI Ledger Wiring & Ledger-First Commit | Wire BimCoordinationLedgerService into GenerateCommand; automatic TargetUniqueId lookup; atomic ledger save BEFORE updating journal to Committed; 2-process rerun -> 100% NoOp. | M2 | ORIGINAL_REQUEST R4 |
| 5 | R5: Measured Geometric Strict Inconclusive | Required Measurements per Category; missing/null -> Inconclusive; VerifyCommand fails with exit code 1 on Inconclusive/Defect. | M3 | ORIGINAL_REQUEST R5 |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| Survey | Codebase Assessment & Mapping | Map R1-R5 current implementation & existing test coverage | none | DONE |
| M1 | Pipeline 1:1 Bijection & Journal Torn-tail | R1 + R2: ReconcileRecordsStrict1To1 & IntentJournal auto-recovery | Survey | DONE |
| M2 | Receipt Completeness & CLI Ledger-First | R3 + R4: Receipt Reader/Reconcile & GenerateCommand Ledger-First wiring | M1 | DONE |
| M3 | Measured Geometric Inconclusive & Verify QA | R5: Category Required Measurements & VerifyCommand exit code 1 | M2 | DONE |
| Final | Full Regression & Final Audit | 3,779 tests green across repo, Final Forensic Auditor CLEAN | M1, M2, M3 | DONE |
