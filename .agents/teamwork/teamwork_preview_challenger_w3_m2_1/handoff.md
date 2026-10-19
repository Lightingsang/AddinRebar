# Handoff Report: Milestone 2 (M2) — Adversarial Challenge & Verification

**From**: Challenger 1 (`teamwork_preview_challenger_w3_m2_1`)  
**To**: Orchestrator / Parent (`8b5286ec-5528-473c-b9fe-c3867bdf1bf8`)  
**Milestone**: M2 (R2: Reconcile Persist-Before-Terminal & Exact Keyset Coverage)  
**Date**: 2026-10-09  

---

## 1. Observation

1. **Targeted Verification Tests**:
   - `dotnet test tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj -- --filter-class *ReconcilePersistBeforeTerminalTests*`
     Executed 6 tests authored by Worker M2: **6/6 passed (0 failures, 0 skipped)** in 871ms.
   - `dotnet test HPBim.slnx`
     Executed full solution suite across Core, Client, and E2E projects: **1,004/1,004 passed (0 failures, 0 skipped)** in 4s 801ms.

2. **Empirical Challenger Stress & Boundary Tests (`EmpiricalChallenger1PersistTests.cs`)**:
   - Developed and executed 7 empirical challenge tests in `HPBim/tests/HPBim.Client.Tests/Recovery/EmpiricalChallenger1PersistTests.cs`:
     - `PersistBeforeTerminal_WhenSaveThrowsUnauthorized_JournalNeverCommitted`: Verifies that when disk write fails due to permissions or unwriteable directory, the journal retains `OutcomeUnknown` and is never marked `Committed`. (PASS)
     - `KeysetCoverage_When1OutOf100KeysMissing_OutcomeStrictlyUnknown`: Verifies that when receipt contains 99 out of 100 elements (missing 1), reconcile returns `StillUnknown`, diagnostic specifies `"missing=1, foreign=0"`, ledger is not written to disk, and journal status is `OutcomeUnknown`. (PASS)
     - `KeysetCoverage_When1OutOf100KeysForeign_OutcomeStrictlyUnknown`: Verifies that when 1 of 100 keys is foreign, reconcile rejects with `"missing=1, foreign=1"` and preserves `OutcomeUnknown`. (PASS)
     - `KeysetCoverage_TemporalOrderStrictlyVerifiedBySpy`: Verifies via spy interceptor that `bim_coordination_ledger.json` is physically flushed to disk with all deserializable mappings *before* `_journal.UpdateStatusAsync(..., IntentStatus.Committed)` is invoked. (PASS)
     - `InvalidPayload_WhenKeyIsUnparseable_PreservesOutcomeUnknown_NoLedgerMutation`: Verifies malformed keys prevent ledger mutations and journal commits. (PASS)
     - `InvalidPayload_WhenUniqueIdIsWhitespace_PreservesOutcomeUnknown_NoLedgerMutation`: Verifies empty/whitespace target uniqueIds are rejected with `StillUnknown`. (PASS)
     - `Reconcile_WhenLedgerFilePathIsNull_MarksJournalCommittedWithoutWritingToDisk`: Empirically demonstrates that when `LedgerFilePath` is null, `ReconcileService` commits the journal without saving anything to disk. (PASS)
   - Results: **7/7 passed**.

3. **Discovered Architectural Integration Gaps**:
   - **Gap 1 (`CoordinationCommand.cs:179`)**:
     In `HPBim/src/HPBim.Cli/Commands/CoordinationCommand.cs`:
     ```csharp
     var service = _reconcileService ?? new ReconcileService(journalToUse, pipeClient, _ledgerService);
     return await RunReconciliationLoopAsync(service, opStr, ledgerPath, ct).ConfigureAwait(false);
     ```
     `CoordinationCommand` has `ledgerPath`, but instantiates `ReconcileService` without passing `ledgerFilePath: ledgerPath`. Consequently, `service.LedgerFilePath` is `null`. In production CLI usage, `PersistLedgerBeforeCommitAsync` returns `null`, the journal is marked `Committed` on disk *before* the ledger is saved, and only later does `CoordinationCommand.ProcessReconcileResults` line 246 save the ledger. If a crash or disk failure occurs between journal commit and line 246, persist-before-terminal is bypassed.
   - **Gap 2 (`BimPipelineEngine.IntentJournal.cs:107-116`)**:
     In `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.IntentJournal.cs`:
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
     `record.SourceKeys` is never populated from `plan.Items`. During real pipeline execution, `record.SourceKeys` remains empty (`Array.Empty<string>()`). When `ReconcileService.ValidateKeysetCoverage` executes, `intent.SourceKeys.Count > 0` evaluates to `false`, causing reconcile to fall back to scalar `intent.ItemCount != elementMap.Count`. Exact keyset coverage is never exercised on real production intents.

---

## 2. Logic Chain

1. *From Observation 1 & 2*: The core isolated algorithms in `ReconcileService` (`ValidateKeysetCoverage` and `PersistLedgerBeforeCommitAsync`) correctly implement the persist-before-terminal invariant and bijective keyset coverage *when* `LedgerFilePath` is populated and `intent.SourceKeys` is non-empty. All unit and integration tests pass with 100% green.
2. *From Observation 3 (Gap 1)*: In the CLI entry point `CoordinationCommand.cs`, `ReconcileService` is instantiated with the 3-parameter constructor that leaves `LedgerFilePath = null`. As proven by test `Reconcile_WhenLedgerFilePathIsNull_MarksJournalCommittedWithoutWritingToDisk`, when `LedgerFilePath` is null, `ReconcileService` transitions the journal to `Committed` in-memory and on disk without writing the ledger to disk. This completely inverts the persist-before-terminal contract during real user CLI runs.
3. *From Observation 3 (Gap 2)*: In `BimPipelineEngine.RecordPreDispatchIntentAsync`, `record.SourceKeys` is left default/empty. Because `intent.SourceKeys` is empty for all real pipeline dispatches, `ValidateKeysetCoverage` always skips the `SetEquals` branch and falls back to scalar count comparison (`intent.ItemCount != elementMap.Count`). Therefore, exact keyset coverage is dead code in actual production pipeline recovery until wired up.
4. *Conclusion from Logic Chain*: The feature logic works in isolation, but the two critical touchpoints connecting it to the CLI and the Pipeline engine are missing.

---

## 3. Caveats

1. The core unit logic written by Worker M2 is clean, well-tested, and adheres strictly to Clean Code governance (file lengths <= 300, methods <= 50, zero unannotated empty catches, zero commented code).
2. The two gaps identified do not cause existing unit tests to fail because unit tests manually construct `IntentJournalRecord` with `SourceKeys` and manually instantiate `ReconcileService` with `ledgerFilePath`.
3. As a Reviewer/Challenger under Rule: "Review-only — do NOT modify implementation code. Report any failures as findings — do NOT fix them yourself", Challenger 1 has not modified `CoordinationCommand.cs` or `BimPipelineEngine.IntentJournal.cs`.

---

## 4. Conclusion & Verdict

**Final Verdict**: **REQUEST_CHANGES**

Milestone 2 implementation is 90% complete and verified at the unit/service layer, but requires two minor, critical wiring fixes to be truly production-grade and tamper-proof:

### Required Remediation for Worker M2:
1. **Fix `HPBim/src/HPBim.Cli/Commands/CoordinationCommand.cs` line 179**:
   Pass `ledgerFilePath: ledgerPath` into `new ReconcileService(...)`:
   ```csharp
   var service = _reconcileService ?? new ReconcileService(journalToUse, pipeClient, _ledgerService, ledgerFilePath: ledgerPath);
   ```
2. **Fix `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.IntentJournal.cs` lines 107-116**:
   Populate `SourceKeys` from `plan.Items` when creating `IntentJournalRecord`:
   ```csharp
   SourceKeys = plan.Items.Select(item => item.SourceKey.ToString()).ToArray(),
   ```

Once Worker M2 applies these two one-line changes and confirms all 1,004+ tests pass, Milestone 2 will achieve 100% end-to-end integrity.

---

## 5. Verification Method

To verify the remediation and ensure zero regressions:

1. **Verify CLI ledger path wiring**:
   Inspect `CoordinationCommand.cs` to ensure `ledgerFilePath: ledgerPath` is passed to `ReconcileService`.
2. **Verify pre-dispatch intent SourceKeys**:
   Inspect `BimPipelineEngine.IntentJournal.cs` to ensure `SourceKeys` is populated with `plan.Items.Select(i => i.SourceKey.ToString()).ToArray()`.
3. **Run targeted tests**:
   ```powershell
   dotnet test tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj -- --filter-class *ReconcilePersistBeforeTerminalTests*
   dotnet test tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj -- --filter-class *EmpiricalChallenger1PersistTests*
   ```
4. **Run full HPBim test suite**:
   ```powershell
   dotnet test HPBim.slnx
   ```
   *Expected*: All 1,004+ tests pass with 0 failures and 0 skipped.
