# Handoff Report: Milestone 2 (M2) Remediation Iteration 2

**From**: Worker W3 M2 Iteration 2 (`teamwork_preview_worker_w3_m2_it2`)  
**To**: Orchestrator / Parent (`8b5286ec-5528-473c-b9fe-c3867bdf1bf8`)  
**Milestone**: HPBim Wave 3 Milestone 2 Remediation  
**Date**: 2026-10-09  

---

## 1. Observation

1. **Upstream Defect 1 — Pre-Dispatch Intent SourceKeys Omission**:
   - In `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.IntentJournal.cs` lines 107-116:
     ```csharp
     var record = new IntentJournalRecord
     {
         OperationId = plan.Options.OperationId,
         PlanHash = plan.PlanHash,
         TimestampUtc = DateTime.UtcNow,
         Status = IntentStatus.InFlight,
         ItemCount = plan.Items.Count,
         TargetDoc = plan.TargetDoc,
         ErrorReason = null
     };
     ```
     `record.SourceKeys` was not initialized, remaining `Array.Empty<string>()`. During production pipeline recovery, `ReconcileService.ValidateKeysetCoverage` bypassed exact keyset checking (`intent.SourceKeys.Count > 0` was `false`) and fell back to scalar count comparisons.

2. **Upstream Defect 2 — CLI ReconcileService LedgerFilePath Bypass**:
   - In `HPBim/src/HPBim.Cli/Commands/CoordinationCommand.cs` line 179:
     ```csharp
     var service = _reconcileService ?? new ReconcileService(journalToUse, pipeClient, _ledgerService);
     ```
     `CoordinationCommand` had `ledgerPath`, but instantiated `ReconcileService` without passing `ledgerFilePath: ledgerPath`. Consequently, `service.LedgerFilePath` remained `null`, causing `PersistLedgerBeforeCommitAsync` to return `null` and skip disk persistence before journal commit.

3. **Remediation Implemented**:
   - In `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.IntentJournal.cs`:
     Added `using System.Linq;` and populated:
     `SourceKeys = plan.Items.Select(item => item.SourceKey?.ToString() ?? string.Empty).ToList(),`
   - In `HPBim/src/HPBim.Cli/Commands/CoordinationCommand.cs` line 179:
     Updated to:
     `var service = _reconcileService ?? new ReconcileService(journalToUse, pipeClient, _ledgerService, ledgerFilePath: ledgerPath);`
   - In `HPBim/tests/HPBim.Client.Tests/Recovery/PreDispatchSourceKeysAndCliWiringTests.cs`:
     Authored 4 new tests covering pre-dispatch `SourceKeys` persistence, exact keyset coverage rejection, CLI ledger persistence order, and failure resilience when the ledger file is unwritable.

4. **Test Verification Results**:
   - `dotnet test tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj --no-build`:
     `total: 501, failed: 0, succeeded: 501, skipped: 0` (PASS in 4s 347ms).
   - `dotnet test HPBim.slnx --no-build`:
     `total: 1,008, failed: 0, succeeded: 1,008, skipped: 0` (PASS across HPBim.Core.Tests, HPBim.Client.Tests, HPBim.E2E.Tests).
   - Zero compilation warnings, zero errors.

---

## 2. Logic Chain

1. *From Observation 1*: Populating `SourceKeys = plan.Items.Select(item => item.SourceKey?.ToString() ?? string.Empty).ToList()` in `BimPipelineEngine.RecordPreDispatchIntentAsync` guarantees that every intent dispatched to the pipe carries the complete list of targeted canonical composite source keys (`[Doc]#[Category]:[Handle]`).
2. *From Observation 1 & 3*: When `ReconcileService.ValidateKeysetCoverage` executes, `intent.SourceKeys` is populated. The method computes `expectedKeys.SetEquals(actualKeys)`. As proven by `RealPipelineIntent_WhenReceiptHasForeignKeys_ReconcileRejectsWithOutcomeUnknown`, any receipt that is missing an expected key or that contains an unexpected foreign key is immediately rejected with `StillUnknown` (`"missing=1, foreign=1"`), preserving `OutcomeUnknown` in the journal.
3. *From Observation 2 & 3*: Passing `ledgerFilePath: ledgerPath` to `new ReconcileService(...)` in `CoordinationCommand.cs` configures `ReconcileService.LedgerFilePath`. During `hpbim coord reconcile`, when a committed receipt is processed, `PersistLedgerBeforeCommitAsync` calls `await _ledgerService.SaveToFileAsync(LedgerFilePath, ct)`. This saves the ledger to disk *before* `await _journal.UpdateStatusAsync(..., IntentStatus.Committed)`.
4. *From Observation 3*: As proven by `CoordinationCommand_Reconcile_WhenLedgerUnwritable_PreservesOutcomeUnknown`, if the target ledger path cannot be written to disk, `ReconcileService` halts the commit and returns `StillUnknown`. The journal is never transitioned to `Committed`, preserving the strict persist-before-terminal invariant in production CLI usage.
5. *From Observation 4*: All 1,008 tests pass with zero regressions, confirming complete architectural and behavioral integrity.

---

## 3. Caveats

1. In existing unit tests where `ReconcileService` is instantiated without a `ledgerFilePath` parameter (or where it is explicitly passed as `null`), `PersistLedgerBeforeCommitAsync` returns `null` without error to preserve backward compatibility with purely in-memory test mocks that do not write files.
2. CLI commands pass `ledgerPath` explicitly from `--ledger` or fallback to `state/bim_coordination_ledger.json`, ensuring the physical file path is always present for user-facing commands.

---

## 4. Conclusion

Both vulnerabilities flagged by Challenger 1 have been remediated with minimal, surgical changes that strictly conform to repo governance:
1. Real pipeline pre-dispatch intents now carry complete `SourceKeys` for exact keyset verification.
2. CLI `coord reconcile` now passes `ledgerFilePath: ledgerPath` into `ReconcileService`, ensuring persist-before-terminal durable execution.
3. All Clean Code Governance rules (C2 <= 300 lines, M3 <= 50 lines, Q-B1 zero commented code, Q-B2 zero empty catches) and integrity requirements are fully satisfied.
4. Total test suite across the solution stands at **1,008/1,008 passing tests** with zero failures and zero skipped tests.

---

## 5. Verification Method

To independently reproduce and verify this remediation:

1. **Verify SourceKeys population and CLI wiring tests**:
   ```powershell
   dotnet test tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj --no-build
   ```
   *Expected*: `total: 501, failed: 0, succeeded: 501, skipped: 0`.

2. **Verify full solution test pass**:
   ```powershell
   dotnet test HPBim.slnx --no-build
   ```
   *Expected*: `total: 1008, failed: 0, succeeded: 1008, skipped: 0`.

3. **Inspect modified code**:
   - `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.IntentJournal.cs` (lines 104-121): Verify `SourceKeys` populated in `IntentJournalRecord`.
   - `HPBim/src/HPBim.Cli/Commands/CoordinationCommand.cs` (line 179): Verify `ledgerFilePath: ledgerPath` passed to `ReconcileService`.
   - `HPBim/tests/HPBim.Client.Tests/Recovery/PreDispatchSourceKeysAndCliWiringTests.cs`: Inspect the 4 new verification tests.
