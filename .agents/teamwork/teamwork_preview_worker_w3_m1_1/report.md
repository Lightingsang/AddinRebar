# Implementation Report: Milestone 1 (R1) — Cross-Process Concurrency & File Lock for IntentJournal

**Worker**: W3 M1 (`teamwork_preview_worker_w3_m1_1`)  
**Date**: 2026-10-09  
**Parent ID**: `8b5286ec-5528-473c-b9fe-c3867bdf1bf8`  
**Milestone**: Wave 3 Remediation Milestone 1: Cross-Process Concurrency & File Lock cho IntentJournal (R1)

---

## 1. Summary of Changes

### 1.1 Cross-Process File Lock Helper (`CrossProcessFileLock.cs`)
- Path: `HPBim/src/HPBim.Client/Connection/CrossProcessFileLock.cs` (94 lines).
- Implements cross-process file locking via companion `${lockFilePath}` using `new FileStream(lockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.None)`.
- Operating system kernel enforces mutual exclusion across all processes and threads.
- Asynchronous retry polling loop:
  - Immediate handoff via `await Task.Yield()` on first 3 attempts for microsecond uncontended handoff.
  - Falls back to `await Task.Delay(10, cancellationToken)` for contention mitigation.
  - Configurable timeout (default 60,000ms) with `TimeoutException`.
- Implements `IDisposable` and `IAsyncDisposable`.
- Zero orphaned lock risk: when a process exits abnormally or is killed, the OS kernel unconditionally releases all open file handles.
- Fully compatible with `netstandard2.0` and modern .NET (`net8.0`, `net10.0`).

### 1.2 Upgraded `IntentJournal` & Companion Storage (`IntentJournal.cs` & `IntentJournal.Storage.cs`)
- Paths:
  - `HPBim/src/HPBim.Client/Connection/IntentJournal.cs` (162 lines)
  - `HPBim/src/HPBim.Client/Connection/IntentJournal.Storage.cs` (201 lines)
- Replaced instance-level `SemaphoreSlim` with `CrossProcessFileLock` guarding:
  - `RecordIntentAsync`: acquires lock, refreshes disk cache if modified, updates active record, appends line with flush, releases lock.
  - `UpdateStatusAsync`: acquires lock, refreshes disk cache, resolves existing record, updates status, appends new line with flush, releases lock.
  - `GetIncompleteIntentsAsync`: acquires lock, refreshes disk cache, returns incomplete records, releases lock.
  - `GetIntentAsync`: acquires lock, refreshes disk cache, returns record by operation ID, releases lock.
  - `PruneOldEntriesAsync`: acquires lock, refreshes disk cache, writes compacted entries to `.tmp` file, flushes hardware buffers, atomically replaces journal file via `File.Move(..., overwrite: true)` without sharing violation, updates cache, releases lock.
- Dynamic cache invalidation & refresh (`RefreshCacheFromDiskIfNeededAsync`):
  - Tracks `_lastKnownFileLength` and `_lastKnownLastWriteTimeUtc`.
  - When another process writes to the journal file, detects change and re-reads latest record mapping from disk inside the lock.
  - O(1) in-memory performance when uncontended; 100% synchronized across instances when multi-writer is active.
- Split across companion partial class to comply with Clean Code Rule C2 (files <= 300 lines).

### 1.3 Multi-Writer Concurrency Test Suite (`IntentJournalConcurrencyTests.cs`)
- Path: `HPBim/tests/HPBim.Client.Tests/Connection/IntentJournalConcurrencyTests.cs` (249 lines).
- **Core Multi-writer Test**:
  - `MultiWriter_TwoInstancesWriting1000UniqueIntents_ColdRestartRecoversAllWithoutLoss`:
    - 2 distinct instances of `IntentJournal` concurrently writing 1,000 unique intents (500 distinct GUIDs per instance).
    - Executed via `await Task.WhenAll(task1, task2)`.
    - Cold restart (a brand new 3rd instance) loading the journal and verifying 1,000/1,000 intact records without any record loss.
    - Verified 1,000 uncorrupted JSON lines on disk matching all 1,000 GUIDs.
- **Interleaved Updates Test**:
  - `MultiWriter_InterleavedAppendsAndStatusUpdates_PreservesConsistencyAcrossInstances`:
    - 2 instances concurrently appending and updating statuses (`Committed` and `OutcomeUnknown`).
    - Cold restart verifies all 200 statuses intact.
- **Immediate Visibility Test**:
  - `MultiWriter_CacheRefreshedOnExternalAppendAndStatusUpdate_VisibleImmediately`:
    - Instance 1 records intent; Instance 2 immediately reads it via cache refresh.
    - Instance 2 updates status; Instance 1 immediately observes updated status.
- **Concurrent Compaction Test**:
  - `MultiWriter_ConcurrentCompactionWhileWriting_ZeroRecordLossAndNoSharingViolation`:
    - 2 instances concurrently appending 200 records while a 3rd instance triggers `PruneOldEntriesAsync`.
    - Verified 0 `IOException` sharing violations, all 200 records preserved.

---

## 2. Test Verification Results

### 2.1 HPBim.Client.Tests
```
Command: dotnet test tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj
Result: Passed (462 tests passed, 0 failed, 0 skipped)
Duration: 3.93s
```

### 2.2 Full Solution Test Suite (HPBim.slnx)
```
Command: dotnet test HPBim.slnx
Result: Passed (969 tests passed, 0 failed, 0 skipped)
- HPBim.Client.Tests: 462 passed
- HPBim.Core.Tests: 465 passed
- HPBim.E2E.Tests: 42 passed
Duration: 4.82s
```

---

## 3. Clean Code Governance Compliance

| Rule | Requirement | Actual Status | Evidence |
|---|---|---|---|
| **C2** | File <= 300 lines | PASS | `CrossProcessFileLock.cs`: 94 lines<br>`IntentJournal.cs`: 162 lines<br>`IntentJournal.Storage.cs`: 201 lines<br>`IntentJournalConcurrencyTests.cs`: 249 lines |
| **M3** | Method <= 50 lines | PASS | All methods <= 46 lines |
| **Q-B1** | 0 commented-out code | PASS | Verified 0 commented-out code lines |
| **Q-B2** | 0 empty catch blocks | PASS | All catch blocks have explicit reason comments |
| **FM2** | Braced blocks | PASS | 100% blocks braced (`if`, `while`, `try`, etc.) |
| **Format** | `dotnet format` clean | PASS | `dotnet format HPBim.slnx --verify-no-changes` returned 0 on owned files |
| **Purity** | netstandard2.0 compatible | PASS | Pure BCL FileStream, FileMode, FileShare, async retry |
| **Scope** | File write ownership | PASS | Only owned files modified/created |
