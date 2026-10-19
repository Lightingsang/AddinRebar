# Handoff Report: Survey Explorer 1 (Wave 3 Remediation: R1 & R2)

## 1. Observation

1. **`IntentJournal.cs` Concurrency Locking**:
   - File: `HPBim/src/HPBim.Client/Connection/IntentJournal.cs`
   - Line 25: `private readonly SemaphoreSlim _ioLock = new(1, 1);`
   - Lines 157-158: `await using var stream = new FileStream(_journalPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, useAsync: true);`
   - Lines 177-193 (`EnsureLineBoundaryAsync`): `stream.Seek(-1, SeekOrigin.End); int lastByte = stream.ReadByte(); if (lastByte != '\n' && lastByte != '\r') { stream.Seek(0, SeekOrigin.End); await stream.WriteAsync(NewlineBytes.AsMemory(), ct)... }`
   - Line 213: `using var stream = new FileStream(_journalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, useAsync: true);`
   - Lines 134-147: `File.Move(tempFile, _journalPath, overwrite: true);`
   - Existing tests (`AdversarialIntentJournalTests.cs` lines 42-60) only test multiple concurrent tasks on a **single** `IntentJournal` instance, never across multiple instances or separate processes.

2. **`ReconcileService.cs` State Sequencing & Keyset Validation**:
   - File: `HPBim/src/HPBim.Client/Recovery/ReconcileService.cs`
   - Lines 188-227 (`ApplyCommittedReceiptAsync`):
     - Line 200: `if (intent.ItemCount != receipt.ElementMap.Count)` (scalar count comparison only).
     - Lines 220-223: `_ledgerService.UpdateMapping(sourceKey, uniqueId, intent.PlanHash, ElementExecutionStatus.Created);` (in-memory update only).
     - Line 225: `await _journal.UpdateStatusAsync(intent.OperationId, IntentStatus.Committed, null, ct).ConfigureAwait(false);` (journal marked `Committed` before ledger file is persisted).
     - `ReconcileService` has no reference to `ledgerFilePath` and never calls `_ledgerService.SaveToFile(...)` or `SaveToFileAsync(...)`.
     - `CoordinationCommand.cs` line 246 calls `_ledgerService.SaveToFile(ledgerPath);` only after the entire reconciliation loop exits.

3. **Atomic File Write Mechanism in Codebase**:
   - File: `HPBim/src/HPBim.Core/Coordination/BimCoordinationLedgerSerializer.cs`
   - Lines 111-127: Acquires companion lock file `${filePath}.lock` using `FileShare.None`, writes to `${filePath}.tmp_{Guid:N}`, calls `stream.Flush(flushToDisk: true)`, and replaces destination via `File.Move(tempPath, filePath, overwrite: true)`.

4. **Baseline Test Suite Status**:
   - Tool Command: `dotnet test tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj` -> 458 passed, 0 failed.
   - Tool Command: `dotnet test tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj` -> 465 passed, 0 failed.
   - Tool Command: `dotnet test tests/HPBim.E2E.Tests/HPBim.E2E.Tests.csproj` -> 42 passed, 0 failed.
   - Total current passing tests: 965 tests.

---

## 2. Logic Chain

1. **R1 Concurrency Logic**:
   - *From Observation 1*: `_ioLock` is an instance-level `SemaphoreSlim(1, 1)`. When two separate instances of `IntentJournal` are created pointing to the same file, they possess distinct `SemaphoreSlim` objects.
   - *From Observation 1*: Both instances open `_journalPath` with `FileShare.ReadWrite`.
   - *Inference*: Both instances can concurrently execute `AppendLineWithFlushAsync`. Stream pointers concurrently seek to the end of file, causing overwritten characters, torn JSON lines, or invalid newline injections via `EnsureLineBoundaryAsync`.
   - *Inference*: In compaction (`PruneOldEntriesAsync`), `File.Move` fails with `IOException` sharing violation on Windows if another instance holds the stream open.
   - *Conclusion for R1*: A cross-process synchronization lock (such as companion `.lock` file opened with `FileShare.None`) is mandatory to serialize operations across all instances and processes.

2. **R2 Persist-Before-Terminal Logic**:
   - *From Observation 2*: `ApplyCommittedReceiptAsync` executes `_ledgerService.UpdateMapping` in memory and directly proceeds to `_journal.UpdateStatusAsync(..., IntentStatus.Committed)`.
   - *Inference*: If process termination occurs after updating the journal but before the caller invokes `_ledgerService.SaveToFile`, the on-disk ledger remains empty of the recovered mappings.
   - *Inference*: Upon subsequent cold process rerun, the journal considers the intent terminal (`Committed`), while the ledger has no mapping. The pipeline therefore attempts to recreate the elements in Revit, producing duplicate elements.
   - *Conclusion for R2*: `ReconcileService` must directly persist the ledger to disk atomically (`.tmp` -> atomic replace) and verify disk write success *before* updating `IntentJournal` to `Committed`. If persistence fails, it must preserve `OutcomeUnknown`.

3. **R2 Keyset Coverage Logic**:
   - *From Observation 2*: Line 200 only validates `intent.ItemCount != receipt.ElementMap.Count`.
   - *Inference*: If a receipt returns the same element count but with mismatched or foreign `SourceKey`s (e.g. from an unrelated transaction or schema mismatch), the validation incorrectly succeeds.
   - *Conclusion for R2*: `IntentJournalRecord` must capture the expected `SourceKeys`, and `ApplyCommittedReceiptAsync` must validate exact set equality between expected keys and receipt keys.

---

## 3. Caveats

1. R3 (separating persistent Document Identity from dynamic DocumentVersion GUID) and R4 (NoOp / Skip / Rollback semantics) are assigned to other tracks or explorers; their touchpoints with `ReconcileService` (such as `MatchSnapshotGuids`) were noted but left for their dedicated investigations.
2. The exact timeout for cross-process file lock acquisition should be set to at least 30,000ms to allow 1,000 intense concurrent disk flushes without premature timeouts under slower HDD or CI environments.
3. Clean Code Rule C2 (file $\le 300$ lines) requires that the cross-process file lock logic be housed in a separate class (`CrossProcessFileLock.cs` or partial class) to keep `IntentJournal.cs` (currently 272 lines) under the 300-line limit.

---

## 4. Conclusion

1. **For R1**: Replace instance-only synchronization in `IntentJournal` with a cross-process lock file pattern (`${_journalPath}.lock` with `FileShare.None` and async retry polling). Encapsulate this in a helper (`CrossProcessFileLock`) compatible with `netstandard2.0`. Verify with a 2-instance 1,000-intent concurrent write test followed by cold-read.
2. **For R2**:
   - Add `LedgerFilePath` to `ReconcileService`.
   - In `ApplyCommittedReceiptAsync`, execute: in-memory update -> durable atomic disk write (`_ledgerService.SaveToFileAsync`) -> `_journal.UpdateStatusAsync(..., Committed)`.
   - Wrap ledger persistence in `try/catch`: on failure, keep `OutcomeUnknown` and return `ReconcileResult.StillUnknown`.
   - Extend `IntentJournalRecord` with `SourceKeys` and enforce exact keyset bijection matching in `ApplyCommittedReceiptAsync`.

---

## 5. Verification Method

1. **Run Full Current Test Suite**:
   ```powershell
   dotnet test tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj
   dotnet test tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj
   dotnet test tests/HPBim.E2E.Tests/HPBim.E2E.Tests.csproj
   ```
2. **Files to Inspect**:
   - `HPBim/src/HPBim.Client/Connection/IntentJournal.cs` (lines 25-27, 107-175)
   - `HPBim/src/HPBim.Client/Recovery/ReconcileService.cs` (lines 188-227)
   - `HPBim/src/HPBim.Client/Connection/IntentJournalRecord.cs`
   - `HPBim/src/HPBim.Core/Coordination/BimCoordinationLedgerSerializer.cs` (lines 111-136)
3. **Invalidation Conditions**:
   - If two `IntentJournal` instances concurrently writing 500 records each result in `< 1000` records or `JsonException` on cold read, R1 is invalidated.
   - If a failure to save the ledger file during reconciliation still results in the journal status changing to `Committed`, R2 is invalidated.
