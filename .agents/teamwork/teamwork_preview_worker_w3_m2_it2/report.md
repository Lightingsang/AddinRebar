# Remediation Report: Milestone 2 (M2) Remediation Iteration 2

**Agent**: Worker W3 M2 Iteration 2 (`teamwork_preview_worker_w3_m2_it2`)  
**Parent**: Orchestrator (`8b5286ec-5528-473c-b9fe-c3867bdf1bf8`)  
**Milestone**: HPBim Wave 3 Milestone 2 Remediation  
**Date**: 2026-10-09  

---

## 1. Executive Summary

All vulnerabilities identified by Challenger 1 have been completely remediated, verified, and backed by automated empirical tests:

1. **Pre-Dispatch Intent Keyset Population (`BimPipelineEngine.IntentJournal.cs`)**:
   - In `RecordPreDispatchIntentAsync`, `record.SourceKeys` is now actively populated from `plan.Items.Select(item => item.SourceKey?.ToString() ?? string.Empty).ToList()`.
   - Real pipeline executions now write all canonical composite source keys to `intent_journal.jsonl` prior to pipe dispatch.
   - When recovering via `ReconcileService.ValidateKeysetCoverage`, real intents strictly enforce exact bijective keyset matching against receipts, rejecting missing or foreign keys with `OutcomeUnknown`.

2. **CLI Coordination Reconcile Wiring (`CoordinationCommand.cs`)**:
   - In `CoordinationCommand.ExecuteReconcileAsync` (line 179), `ReconcileService` is instantiated passing `ledgerFilePath: ledgerPath`.
   - Production CLI invocations (`hpbim coord reconcile --ledger <path>`) now configure `ReconcileService.LedgerFilePath`.
   - In `ReconcileService.ApplyCommittedReceiptAsync`, `PersistLedgerBeforeCommitAsync` strictly persists the ledger to disk *before* updating the journal status to `Committed`.
   - If disk persistence fails (e.g. read-only file, permission denied), the journal is never marked `Committed` and preserves `OutcomeUnknown`.

3. **Empirical Verification & Zero Regressions**:
   - 4 new comprehensive tests added in `PreDispatchSourceKeysAndCliWiringTests.cs`.
   - `HPBim.Client.Tests`: **501/501 PASS** (100% green).
   - Full solution `HPBim.slnx`: **1,008/1,008 PASS** (Core: 496, Client: 501, E2E: 11).
   - Zero compilation errors, zero warnings.

---

## 2. Remediation Details

### A. Pre-Dispatch SourceKeys Recording
- **File**: `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.IntentJournal.cs` (lines 104-121)
- **Problem**: Pre-dispatch intents were recorded with `ItemCount = plan.Items.Count`, leaving `SourceKeys` empty. As a result, `ValidateKeysetCoverage` could only perform scalar count comparisons during recovery of production intents.
- **Fix**: Added `using System.Linq;` and populated `SourceKeys` during `RecordPreDispatchIntentAsync`:
  ```csharp
  var record = new IntentJournalRecord
  {
      OperationId = plan.Options.OperationId,
      PlanHash = plan.PlanHash,
      TimestampUtc = DateTime.UtcNow,
      Status = IntentStatus.InFlight,
      ItemCount = plan.Items.Count,
      SourceKeys = plan.Items.Select(item => item.SourceKey?.ToString() ?? string.Empty).ToList(),
      TargetDoc = plan.TargetDoc,
      ErrorReason = null
  };
  ```

### B. CLI Coordination Command ReconcileService Wiring
- **File**: `HPBim/src/HPBim.Cli/Commands/CoordinationCommand.cs` (line 179)
- **Problem**: `CoordinationCommand.ExecuteReconcileAsync` instantiated `ReconcileService` without passing `ledgerPath`, leaving `service.LedgerFilePath = null`. In CLI runs, `PersistLedgerBeforeCommitAsync` was skipped, inverting the persist-before-terminal guarantee.
- **Fix**: Wired `ledgerFilePath: ledgerPath` into constructor:
  ```csharp
  var service = _reconcileService ?? new ReconcileService(journalToUse, pipeClient, _ledgerService, ledgerFilePath: ledgerPath);
  ```

---

## 3. Test Verification Matrix

| Test Method | Category | Verified Behavior | Result |
|---|---|---|---|
| `PreDispatchIntent_PopulatesSourceKeysFromPlanItems` | IntentJournal | `ExecutePlanJournaledAsync` populates all `SourceKeys` in pre-dispatch journal records | **PASS** |
| `RealPipelineIntent_WhenReceiptHasForeignKeys_ReconcileRejectsWithOutcomeUnknown` | Exact Keyset Coverage | Reconciling a real pipeline intent against a receipt with foreign keys returns `StillUnknown` (`missing=1, foreign=1`) and leaves journal uncommitted | **PASS** |
| `CoordinationCommand_Reconcile_PersistsLedgerViaConfiguredPath` | CLI Integration | `hpbim coord reconcile` uses configured `--ledger` path, flushes mappings to disk, and transitions journal to `Committed` | **PASS** |
| `CoordinationCommand_Reconcile_WhenLedgerUnwritable_PreservesOutcomeUnknown` | Failure Resilience | If the ledger file is unwritable, reconcile returns exit code 1 and journal status remains `OutcomeUnknown` | **PASS** |
| Full `HPBim.Client.Tests` | Solution | All 501 client unit, integration, and recovery tests | **PASS** (501/501) |
| Full `HPBim.slnx` | Solution | Full solution across Core, Client, and E2E suites | **PASS** (1,008/1,008) |

---

## 4. Clean Code Governance Compliance

- **File Length (Rule C2 <= 300 lines)**:
  - `BimPipelineEngine.IntentJournal.cs`: 164 lines (PASS)
  - `CoordinationCommand.cs`: 285 lines (PASS)
  - `PreDispatchSourceKeysAndCliWiringTests.cs`: 199 lines (PASS)
- **Method Length (Rule M3 <= 50 lines)**:
  - Longest method in touched files is <= 41 lines (PASS)
- **Dead/Commented-Out Code (Rule Q-B1)**:
  - 0 commented-out code lines (PASS)
- **Empty Catches (Rule Q-B2)**:
  - 0 unannotated empty catches (PASS)
- **Integrity Mandate**:
  - Genuine implementations; no dummy/facade implementations or hardcoded values.
