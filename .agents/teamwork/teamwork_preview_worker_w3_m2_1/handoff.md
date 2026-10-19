# Handoff Report: Milestone 2 (R2) — Reconcile Persist-Before-Terminal & Exact Keyset Coverage

## 1. Observation

1. **Initial Vulnerabilities in `ReconcileService.cs`**:
   - In `ApplyCommittedReceiptAsync`:
     - Keyset validation was strictly scalar: `if (intent.ItemCount != receipt.ElementMap.Count)` (line 200). A receipt returning foreign keys or missing specific targeted keys with equal count was accepted.
     - State ordering: `_ledgerService.UpdateMapping` updated in-memory mappings, and then `_journal.UpdateStatusAsync(intent.OperationId, IntentStatus.Committed, null, ct)` (line 225) immediately marked the journal `Committed` on disk without ever persisting the ledger to disk.
     - `ReconcileService` did not hold `LedgerFilePath` and never invoked `_ledgerService.SaveToFileAsync`.
     - In `IntentJournalRecord.cs`, there was no `SourceKeys` property to record targeted element keys.

2. **Implemented Enhancements**:
   - `HPBim/src/HPBim.Client/Connection/IntentJournalRecord.cs` (60 lines):
     - Added `public IReadOnlyList<string> SourceKeys { get; init; } = Array.Empty<string>();`.
   - `HPBim/src/HPBim.Client/Recovery/ReconcileService.cs` (290 lines):
     - Added `public string? LedgerFilePath { get; set; }` and overloaded constructor accepting `string? ledgerFilePath`.
     - Added `ValidateKeysetCoverage`: validates `expectedKeys.SetEquals(actualKeys)` against `intent.SourceKeys` when populated; falls back to `ItemCount` for legacy records.
     - Added `PersistLedgerBeforeCommitAsync`: saves ledger to disk via `_ledgerService.SaveToFileAsync(LedgerFilePath, ct)` before journal update.
     - On persistence failure or keyset mismatch, records `OutcomeUnknown` in journal and returns `ReconcileResult.StillUnknown`.
   - `HPBim/tests/HPBim.Client.Tests/Recovery/ReconcilePersistBeforeTerminalTests.cs` (230 lines):
     - 6 integration tests proving disk write failure handling, temporal commit ordering, missing keys rejection, foreign keys rejection, successful reconcile persistence, and failure recovery retry.

3. **Verification Results**:
   - `dotnet test tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj`: `total: 484, failed: 0, succeeded: 484` (PASS).
   - `dotnet test HPBim.slnx`: `total: 991, failed: 0, succeeded: 991` (PASS across Client, Core, and E2E test projects).
   - `dotnet test HPRebar.Core.Tests`: `total: 1682, failed: 0, succeeded: 1682` (PASS).
   - `dotnet test McpShared`: `total: 915, failed: 0, succeeded: 915` (PASS).
   - `dotnet format HPBim.slnx --verify-no-changes`: 0 violations (PASS).

---

## 2. Logic Chain

1. *From Observation 1*: `ApplyCommittedReceiptAsync` updated in-memory mappings and wrote `IntentStatus.Committed` to the journal before the ledger was written to disk. If the process crashed before subsequent file persistence, on-disk ledger lacked mappings while journal marked the intent terminal. On cold rerun, the pipeline saw no ledger mappings and generated duplicate elements.
2. *From Observation 2*: Implementing `PersistLedgerBeforeCommitAsync` guarantees that `await _ledgerService.SaveToFileAsync(LedgerFilePath, ct)` runs and flushes the atomic `.tmp` replace to physical disk *before* `await _journal.UpdateStatusAsync(..., IntentStatus.Committed)`.
3. *From Observation 2*: If `SaveToFileAsync` throws (e.g. disk full, permission denied, lock contention), the exception is captured, `OutcomeUnknown` is preserved in the journal, and `ReconcileResult.StillUnknown` is returned. The journal is never marked `Committed`, allowing subsequent recovery attempts.
4. *From Observation 2*: Adding `intent.SourceKeys` and checking `expectedKeys.SetEquals(actualKeys)` ensures that a receipt missing even one expected key or containing foreign keys is rejected with `StillUnknown`.
5. *From Observation 3*: Verified empirically by 6 targeted tests in `ReconcilePersistBeforeTerminalTests.cs` and full test runs with 0 regressions.

---

## 3. Caveats

1. In-memory unit tests that instantiate `ReconcileService` without passing `ledgerFilePath` (or where `ledgerFilePath` is null) maintain purely in-memory ledger state for testing convenience without creating disk files, preserving compatibility with all 478 legacy tests.
2. The dispatch message mentioned `HPBim/src/HPBim.Core/Storage/IntentJournalRecord.cs (if adding SourceKeys list)`. In the existing codebase, `IntentJournalRecord.cs` is located at `HPBim/src/HPBim.Client/Connection/IntentJournalRecord.cs` (and `HPBim.Core` has no `Storage` directory). `SourceKeys` was added to the canonical `HPBim.Client.Connection.IntentJournalRecord` class.

---

## 4. Conclusion

Milestone 2 (R2) is fully complete. `ReconcileService` now strictly enforces persist-before-terminal durable semantics and exact keyset bijection coverage. On any ledger persistence failure, corrupt mapping, or keyset mismatch, `OutcomeUnknown` is preserved. All Clean Code Governance rules (file <= 300 lines, method <= 50 lines, 0 commented-out code, 0 unannotated catches, netstandard2.0 purity) and integrity mandates are strictly satisfied with 100% test pass (991/991 HPBim tests) and zero regressions.

---

## 5. Verification Method

1. **Run HPBim.Client Test Suite**:
   ```powershell
   dotnet test tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj
   ```
   *Expected*: 484 passed, 0 failed.

2. **Run Full HPBim Test Suite**:
   ```powershell
   dotnet test HPBim.slnx
   ```
   *Expected*: 991 passed, 0 failed.

3. **Files to Inspect**:
   - `HPBim/src/HPBim.Client/Connection/IntentJournalRecord.cs` (lines 30-31: `SourceKeys` property)
   - `HPBim/src/HPBim.Client/Recovery/ReconcileService.cs` (lines 202-289: `ApplyCommittedReceiptAsync`, `ValidateKeysetCoverage`, `PersistLedgerBeforeCommitAsync`)
   - `HPBim/tests/HPBim.Client.Tests/Recovery/ReconcilePersistBeforeTerminalTests.cs` (all 6 tests)

4. **Invalidation Conditions**:
   - If a failure during ledger save still marks the journal as `Committed`, Milestone 2 is invalidated.
   - If a receipt missing a single `SourceKey` from `intent.SourceKeys` transitions to `Committed`, Milestone 2 is invalidated.
   - If any test in `HPBim.slnx` fails, Milestone 2 is invalidated.
