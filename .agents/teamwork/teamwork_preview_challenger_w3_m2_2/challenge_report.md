# Adversarial Challenge Report: Milestone 2 (M2)
**Scope**: Reconcile Persist-Before-Terminal & Exact Keyset Coverage  
**Reviewer**: Challenger 2 (Empirical Challenger)  
**Date**: 2026-10-09  

---

## Challenge Summary

**Overall risk assessment**: **LOW**

Worker M2 has implemented the required persist-before-terminal invariants and exact keyset coverage rules in `ReconcileService.cs` and `IntentJournalRecord.cs`.
Empirical stress testing using a dedicated adversarial test suite (`EmpiricalChallenger2ReconcileBoundaryTests.cs`, 6 tests) confirmed:
1. Extra foreign keys in a receipt are rejected with `ReconcileStatus.StillUnknown` without mutating in-memory ledger mappings or creating an on-disk ledger file.
2. An empty receipt map against a non-empty intent is rejected with `ReconcileStatus.StillUnknown` for both keyset-aware intents and legacy count-based intents.
3. Sequential multiple failure attempts (absent receipt → empty receipt → foreign key receipt → disk save permission error) followed by a valid recovery succeed cleanly, persisting the ledger file to disk before updating the journal to `Committed`. Cold restart across crashes also recovers cleanly without state corruption.
4. Repository test suites passed 100% (3,709 passed across `HPBim.slnx`, `HPRebar.Core.Tests`, and `McpShared`) with zero regressions.

One integration finding was noted regarding `BimPipelineEngine.RecordPreDispatchIntentAsync` (pre-dispatch intents do not yet populate `SourceKeys`, defaulting to count-based fallback for live pipeline dispatches; planned for pipeline wiring).

---

## Challenges

### [Low] Challenge 1: Keyset Validation Order & In-Memory Ledger Contamination Risk
- **Assumption challenged**: Does `ApplyCommittedReceiptAsync` mutate in-memory ledger state before keyset coverage and key format validation succeed?
- **Attack scenario**:
  An adversarial receipt arrives containing valid items mixed with extra foreign keys or unparseable source keys. If `_ledgerService.UpdateMapping` is called prior to full verification, in-memory state is contaminated even if the operation returns `StillUnknown`.
- **Blast radius**:
  Subsequent queries in the same process would see uncommitted/foreign mappings in `BimCoordinationLedgerService`.
- **Empirical verification**:
  Inspected lines 214–237 of `ReconcileService.cs`:
  `ValidateKeysetCoverage` runs first (lines 214–219). If it fails, it returns immediately without touching `_ledgerService`.
  Parsing into `parsedKeys` runs second (lines 221–232). If any key is invalid or unparseable, it returns immediately without touching `_ledgerService`.
  `_ledgerService.UpdateMapping` only executes after all keys have passed validation.
  Verified by `Reconcile_ReceiptWithExtraForeignKeys_PreservesOutcomeUnknown_NoLedgerMutation_NoCommit` and `Reconcile_ReceiptWithOnlyForeignKeys_PreservesOutcomeUnknown_NoLedgerMutation`: `Assert.Empty(ledger.Mappings)` passes.
- **Result**: **PASS** (Protected against in-memory state contamination).

---

### [Low] Challenge 2: Temporal Ordering of Disk Persistence vs Journal Terminal Status
- **Assumption challenged**: Can the journal ever be updated to `IntentStatus.Committed` if disk writing of the ledger fails?
- **Attack scenario**:
  Simulate a disk write failure (e.g., path unwritable, read-only directory, lock failure). Verify whether `_journal.UpdateStatusAsync(..., IntentStatus.Committed)` is ever invoked.
- **Blast radius**:
  If the journal records `Committed` but the ledger file is missing from disk, cold process restarts will treat the operation as terminal, while the ledger lacks the mapping, leading to duplicate BIM element generation on subsequent runs.
- **Empirical verification**:
  Lines 239–246 in `ReconcileService.cs`:
  `PersistLedgerBeforeCommitAsync` executes `await _ledgerService.SaveToFileAsync(LedgerFilePath!, ct)`. If an exception is thrown, it is caught in `PersistLedgerBeforeCommitAsync` and returned as `persistError`.
  `ApplyCommittedReceiptAsync` immediately updates journal status to `OutcomeUnknown` with `persistError` and returns `StillUnknown`.
  `_journal.UpdateStatusAsync(..., IntentStatus.Committed)` is never reached.
  Verified by `Reconcile_WhenLedgerDiskSaveFails_PreservesOutcomeUnknown_JournalNotMarkedCommitted` and `Reconcile_SuccessfulReconcile_SavesLedgerToDiskBeforeMarkingJournalCommitted`.
- **Result**: **PASS** (Persist-before-terminal strictly enforced).

---

### [Low] Challenge 3: Pre-Dispatch Intent SourceKeys Population in BimPipelineEngine
- **Assumption challenged**: Does `BimPipelineEngine` populate `SourceKeys` when dispatching an intent in production?
- **Attack scenario**:
  In `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.IntentJournal.cs` (lines 107–116):
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
  `SourceKeys` is not assigned here.
- **Blast radius**:
  Intents dispatched by `BimPipelineEngine` have `SourceKeys.Count == 0`. When reconciled, `ValidateKeysetCoverage` falls back to `intent.ItemCount != elementMap.Count`. Keyset-level verification is bypassed for pipeline-dispatched intents until populated.
- **Mitigation / Next Step**:
  In the relevant pipeline/wiring milestone (e.g. M4 / E2E), update `RecordPreDispatchIntentAsync` to include:
  `SourceKeys = plan.Items.Select(i => i.SourceKey.ToString()).ToList()`.
  This does not block M2 because M2's scope was strictly `ReconcileService` and `IntentJournalRecord` contracts.

---

## Stress Test Results

| Test Scenario | Expected Behavior | Actual Behavior | Result |
|---|---|---|---|
| Receipt containing extra foreign keys | Return `StillUnknown`, `foreign=1`, journal `OutcomeUnknown`, 0 ledger mutations, 0 disk files | Returned `StillUnknown`, `foreign=1`, journal `OutcomeUnknown`, 0 ledger mutations | **PASS** |
| Receipt containing 100% foreign keys (equal count) | Return `StillUnknown`, `missing=2, foreign=2`, journal `OutcomeUnknown`, 0 ledger mutations | Returned `StillUnknown`, `missing=2, foreign=2`, journal `OutcomeUnknown`, 0 ledger mutations | **PASS** |
| Empty receipt map vs non-empty intent (SourceKeys populated) | Return `StillUnknown`, `missing=3, foreign=0`, journal `OutcomeUnknown`, 0 ledger mutations | Returned `StillUnknown`, `missing=3, foreign=0`, journal `OutcomeUnknown`, 0 ledger mutations | **PASS** |
| Empty receipt map vs non-empty intent (Legacy count-based intent) | Return `StillUnknown`, `ItemCount=2, entries=0`, journal `OutcomeUnknown`, 0 ledger mutations | Returned `StillUnknown`, completeness mismatch reported, journal `OutcomeUnknown` | **PASS** |
| 5-phase sequential failure sequence (No receipt → Empty receipt → Foreign keys → Disk unwritable → Success) | Retain `OutcomeUnknown` across all 4 failures, cleanly commit and persist on attempt 5 | Preserved `OutcomeUnknown` through attempts 1–4; committed and verified 2 mappings on attempt 5 | **PASS** |
| Multi-phase failure with cold process restart simulation | In-memory clean state across process boundaries, recovers cleanly | Cold process 2 successfully recovered, persisted ledger and committed journal | **PASS** |
| Full HPBim test suite (`dotnet test HPBim.slnx`) | 997 tests pass with 0 failures | 997 passed, 0 failed | **PASS** |
| Full HPRebar core test suite (`dotnet test HPRebar.Core.Tests`) | 1,682 tests pass with 0 failures | 1,682 passed, 0 failed | **PASS** |
| Full McpShared test suite (`dotnet test McpShared`) | 1,030 tests pass with 0 failures | 1,030 passed, 0 failed | **PASS** |

---

## Unchallenged Areas

- **Extensible Storage Schema Registration in Live Revit Process**:
  Live interaction with Revit.exe process was not challenged (controlled environment constraint: zero touch on active Revit session). Verified through `FakeRevitPipeClient` and unit/integration simulation tests.
