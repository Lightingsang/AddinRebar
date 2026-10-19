# Survey Report: HPBim Wave 3 Remediation — R1 & R2 Investigation

**Date**: 2026-10-09  
**Agent**: Survey Explorer 1 (`teamwork_preview_explorer_w3_survey_1`)  
**Parent ID**: `8b5286ec-5528-473c-b9fe-c3867bdf1bf8`  
**Scope**: 
1. **R1**: IntentJournal Multi-writer Concurrency & Cross-process File Lock
2. **R2**: ReconcileService Persist-Before-Terminal & Exact Keyset Coverage

---

## 1. Executive Summary

An exhaustive read-only static analysis and runtime test suite verification was conducted on `HPBim` (`HPBim.Core`, `HPBim.Client`, `HPBim.Cli`, and their respective test suites). All 965 existing tests currently pass (458 Client tests, 465 Core tests, 42 E2E tests).

However, our investigation identified **two critical architectural vulnerabilities** in the current implementation of `IntentJournal` (R1) and `ReconcileService` (R2) that directly threaten data integrity and crash recovery:

1. **R1 Vulnerability (Multi-Writer Concurrency)**: 
   `IntentJournal` relies entirely on an instance-level `SemaphoreSlim(1, 1)` for synchronization. It opens files with `FileShare.ReadWrite`. When two instances or processes write to the same journal file simultaneously, their append operations interleave or overwrite each other at the stream tail, `EnsureLineBoundaryAsync` corrupts line boundaries, compaction (`PruneOldEntriesAsync`) triggers `IOException` sharing violations, and the in-memory cache `_activeRecords` becomes stale and desynchronized.
2. **R2 Vulnerability (Reconcile Persist-Before-Terminal & Weak Keyset Check)**:
   In `ReconcileService.ApplyCommittedReceiptAsync`:
   - It updates in-memory ledger mappings, but **never persists the ledger to disk** before marking the `IntentJournal` as `Committed`. If the process crashes immediately after marking the journal `Committed`, the ledger on disk remains un-updated while the journal marks the intent terminal. On cold rerun, the ledger lacks the mapped element IDs and creates duplicate elements in Revit.
   - It only compares scalar `ItemCount` (`intent.ItemCount != receipt.ElementMap.Count`). It never validates whether the recovered `SourceKey`s match the original intent's keyset, allowing foreign or mismatched keys to be committed.
   - When disk persistence fails, the system does not catch the failure or preserve `OutcomeUnknown`.

Below is the complete evidence chain, root-cause trace, and architectural remediation proposal.

---

## 2. Requirement R1: IntentJournal Multi-writer Concurrency & Cross-process File Lock

### 2.1 File Locations & Project Boundaries
- **`HPBim/src/HPBim.Client/Connection/IntentJournal.cs`** (272 lines): Main implementation of `IIntentJournal`.
- **`HPBim/src/HPBim.Client/Connection/IIntentJournal.cs`** (48 lines): Public interface contract.
- **`HPBim/src/HPBim.Client/Connection/IntentJournalRecord.cs`** (56 lines): Immutable JSONL record DTO.
- **`HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.IntentJournal.cs`** (162 lines): Integration with pipeline dispatch.
- **Target Framework**: `HPBim.Client` targets `net8.0;net10.0`. `HPBim.Core` targets `netstandard2.0`.

### 2.2 Current Concurrency Mechanisms & Flaws
In `IntentJournal.cs`:
- **Line 25**: `private readonly SemaphoreSlim _ioLock = new(1, 1);`  
  *Observation*: `_ioLock` is an instance field. It synchronizes multiple asynchronous tasks executing on the *same* instance of `IntentJournal`. It provides **zero isolation** between two distinct instances of `IntentJournal` (even in the same process) and **zero isolation** across operating system processes.
- **Line 26**: `private readonly ConcurrentDictionary<Guid, IntentJournalRecord> _activeRecords = new();`  
  *Observation*: In-memory cache is local to the instance. When instance 2 appends a record or updates a status, instance 1's `_activeRecords` has no awareness of it. In `GetIntentAsync` (line 95), if `_activeRecords.TryGetValue` hits, it returns the stale cached record without checking disk!
- **Lines 157-158**: 
  ```csharp
  await using var stream = new FileStream(
      _journalPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, useAsync: true);
  ```
  *Observation*: Opened with `FileShare.ReadWrite`. This explicitly permits other processes and file handles to open, read, and write to the same file simultaneously without blocking.
- **Lines 177-193 (`EnsureLineBoundaryAsync`)**:
  ```csharp
  if (stream.Length == 0) return;
  stream.Seek(-1, SeekOrigin.End);
  int lastByte = stream.ReadByte();
  if (lastByte != '\n' && lastByte != '\r')
  {
      stream.Seek(0, SeekOrigin.End);
      await stream.WriteAsync(NewlineBytes.AsMemory(), ct).ConfigureAwait(false);
      await stream.FlushAsync(ct).ConfigureAwait(false);
  }
  ```
  *Observation*: If Process 1 is writing half-way through a line, Process 2 opens the stream, reads `lastByte` (which is in the middle of Process 1's JSON line), treats it as missing newline, and writes a newline right inside Process 1's JSON record, destroying both lines!
- **Lines 134-147 (`RewriteCompactedJournalAsync`)**:
  ```csharp
  string tempFile = $"{_journalPath}.tmp_{Guid.NewGuid():N}";
  ...
  File.Move(tempFile, _journalPath, overwrite: true);
  ```
  *Observation*: If another instance has `_journalPath` open in `AppendLineWithFlushAsync` or `ReadLatestRecordsMapCoreAsync`, `File.Move` on Windows throws `IOException: The process cannot access the file because it is being used by another process`. Moreover, any record appended by another process between the compaction read and `File.Move` is permanently lost.

### 2.3 Operation Trace
| Operation | Location in Code | Current Locking | Failure Mode in Multi-Writer |
|---|---|---|---|
| **Append Intent** | `RecordIntentAsync` (L40) -> `AppendLineWithFlushAsync` (L152) | `_ioLock.WaitAsync` (instance only) | Interleaved stream writes, destroyed line boundaries, overwritten lines |
| **Update Status** | `UpdateStatusAsync` (L50) -> `ReadLatestRecordsMapAsync` (L60) -> `AppendLineWithFlushAsync` (L79) | `_ioLock.WaitAsync` (instance only) | Race during read-modify-write; writes stale base record; torn writes |
| **Compaction** | `PruneOldEntriesAsync` (L104) -> `RewriteCompactedJournalAsync` (L132) | `_ioLock.WaitAsync` (instance only) | `File.Move` fails with `IOException` on Windows; obliterates concurrent appends |
| **Cold / Disk Read** | `ReadLatestRecordsMapCoreAsync` (L208) | `_ioLock.WaitAsync` (instance only) | Reads partial/unflushed line; throws `JsonException`; drops valid records |

### 2.4 Cross-Process Synchronization Mechanisms Analysis
We evaluated candidate synchronization mechanisms compatible with `netstandard2.0` and `net8.0/net10.0`:

1. **Option 1: Companion File Lock (`.lock` file with `FileShare.None`)** — **RECOMMENDED**:
   - *Pattern*: Open a companion file `$"{_journalPath}.lock"` with:
     ```csharp
     new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.None);
     ```
   - *Evaluation*:
     - **OS Enforced**: The OS kernel guarantees that only one process/thread can hold an open handle with `FileShare.None`. Any concurrent open attempt immediately throws `IOException` (sharing violation).
     - **Async Polling**: Wrapped in a retry loop (`await Task.Delay(15, ct)`) with a configurable timeout (e.g. 30,000ms).
     - **Zero Orphaned Locks**: If a process terminates abnormally, crashes, or is killed via `kill -9`, the OS kernel unconditionally closes all open file descriptors/handles, automatically releasing the lock.
     - **Compaction Safety**: Compaction acquires the `.lock` file before creating `.tmp` and before `File.Move`. No other process can open `_journalPath` during `File.Move`.
     - **Netstandard2.0 Compatible**: 100% native `System.IO.FileStream` supported in all .NET platforms.
     - **Proven in Codebase**: This exact pattern is already implemented in `BimCoordinationLedgerSerializer.cs` (lines 150-189).
2. **Option 2: Named `System.Threading.Mutex`**:
   - *Evaluation*: Named mutexes have serious drawbacks with `async/await`. A `Mutex` has strict thread affinity: `ReleaseMutex()` must be called on the exact same OS thread as `WaitOne()`. In asynchronous C#, continuations after `await` switch threads on the `ThreadPool`, triggering `ApplicationException: Object synchronization method was called from an unsynchronized block of code`. Furthermore, crashed processes leave `AbandonedMutexException`. Not recommended for async code.
3. **Option 3: `FileStream.Lock(position, length)`**:
   - *Evaluation*: Synchronous only, byte-range based, platform-dependent behaviors on Unix, and does not prevent `File.Move` sharing violations because the primary file must remain open to hold byte locks.

### 2.5 Multi-Writer Concurrency Test Requirements
- **Test Objective**: Verify that 2 distinct instances of `IntentJournal` writing concurrently to the same journal file on disk achieve 100% consistency with zero lost records and zero corrupt lines.
- **Workload**:
  - `journal1 = new IntentJournal(journalPath);`
  - `journal2 = new IntentJournal(journalPath);`
  - Instance 1 records 500 unique intents (`Guid.NewGuid()`).
  - Instance 2 records 500 unique intents (`Guid.NewGuid()`).
  - Run concurrently via `await Task.WhenAll(task1, task2)`.
- **Cold-Read Verification**:
  - `using var coldJournal = new IntentJournal(journalPath);`
  - `var allRecords = await coldJournal.GetIncompleteIntentsAsync();` (or read map).
  - Assert that all 1,000 unique `OperationId`s are retrieved.
  - Assert that `allRecords.Count == 1000`.
  - Assert that all lines in the file are valid JSON, uncorrupted, and correctly formatted.

---

## 3. Requirement R2: ReconcileService Persist-Before-Terminal & Exact Keyset Coverage

### 3.1 File Locations & Project Boundaries
- **`HPBim/src/HPBim.Client/Recovery/ReconcileService.cs`** (229 lines): Core reconciliation coordinator.
- **`HPBim/src/HPBim.Client/Recovery/IReconcileService.cs`** (18 lines): Interface contract.
- **`HPBim/src/HPBim.Client/Recovery/CoordinationReceiptRecord.cs`** (21 lines): Extensible Storage DTO.
- **`HPBim/src/HPBim.Core/Coordination/BimCoordinationLedgerSerializer.cs`** (230 lines): Atomic persistence serializer (`.tmp` -> replace with lock).
- **`HPBim/src/HPBim.Core/Coordination/BimCoordinationLedgerService.cs`** (291 lines): Ledger domain service.
- **`HPBim/src/HPBim.Core/Coordination/BimCoordinationLedgerService.Mappings.cs`** (160 lines): In-memory mappings manager.

### 3.2 Trace of `ApplyCommittedReceiptAsync`
In `ReconcileService.cs`, lines 188-227:
```csharp
private async Task<ReconcileResult> ApplyCommittedReceiptAsync(
    IntentJournalRecord intent,
    CoordinationReceiptRecord receipt,
    CancellationToken ct)
{
    // Check 1: Null ElementMap
    if (receipt.ElementMap == null)
    {
        string reason = "Receipt ElementMap is null or corrupt in Revit Extensible Storage. Preserving OutcomeUnknown.";
        await _journal.UpdateStatusAsync(intent.OperationId, IntentStatus.OutcomeUnknown, reason, ct).ConfigureAwait(false);
        return ReconcileResult.StillUnknown(intent.OperationId, intent.PlanHash, reason);
    }

    // Check 2: Cardinality check (ItemCount only!)
    if (intent.ItemCount != receipt.ElementMap.Count)
    {
        string reason = $"Receipt completeness mismatch: expected ItemCount={intent.ItemCount}, but receipt contained {receipt.ElementMap.Count} entries. Preserving OutcomeUnknown.";
        await _journal.UpdateStatusAsync(intent.OperationId, IntentStatus.OutcomeUnknown, reason, ct).ConfigureAwait(false);
        return ReconcileResult.StillUnknown(intent.OperationId, intent.PlanHash, reason);
    }

    // Check 3: Parse composite keys
    var parsedKeys = new List<(CompositeSourceKey Key, string UniqueId)>(receipt.ElementMap.Count);
    foreach (var (keyStr, uniqueId) in receipt.ElementMap)
    {
        if (!CompositeSourceKey.TryParse(keyStr, out var sourceKey) || sourceKey == null || string.IsNullOrWhiteSpace(uniqueId))
        {
            string reason = $"Receipt contains invalid or unparseable source key '{keyStr}' or empty uniqueId. Preserving OutcomeUnknown.";
            await _journal.UpdateStatusAsync(intent.OperationId, IntentStatus.OutcomeUnknown, reason, ct).ConfigureAwait(false);
            return ReconcileResult.StillUnknown(intent.OperationId, intent.PlanHash, reason);
        }
        parsedKeys.Add((sourceKey, uniqueId));
    }

    // Step 4: Update in-memory mapping ONLY
    foreach (var (sourceKey, uniqueId) in parsedKeys)
    {
        _ledgerService.UpdateMapping(sourceKey, uniqueId, intent.PlanHash, ElementExecutionStatus.Created);
    }

    // Step 5: Mark Journal as Committed (BEFORE PERSISTING LEDGER TO DISK!)
    await _journal.UpdateStatusAsync(intent.OperationId, IntentStatus.Committed, null, ct).ConfigureAwait(false);
    return ReconcileResult.Committed(intent.OperationId, intent.PlanHash, receipt.ElementMap);
}
```

### 3.3 Root Cause Analysis of Deficiencies

#### Deficiency 1: Violation of Persist-Before-Terminal
- **Current Sequence**:
  1. In-memory `_ledgerService.UpdateMapping(...)`
  2. `_journal.UpdateStatusAsync(..., IntentStatus.Committed)` (Persisted to journal on disk)
  3. `SaveToFile` is NEVER called by `ReconcileService`! In `CoordinationCommand.cs`, it is only called outside the service at line 246 *after* all intents have finished processing!
- **Failure Scenario**:
  If a crash, power outage, or exception occurs right after Step 2:
  - On restart, `IntentJournal` sees the intent as `Committed`. It will never be picked up by `GetIncompleteIntentsAsync()`.
  - The ledger on disk (`state/bim_coordination_ledger.json`) was never written and does NOT have the recovered unique IDs!
  - When the user runs `hpbim generate` again, the ledger has no mappings for these elements. The pipeline treats them as new elements, dispatching them to Revit and generating duplicate columns/beams/slabs!
- **Required Sequence**:
  1. Update in-memory ledger mappings.
  2. **Persist ledger to disk atomically** (`await _ledgerService.SaveToFileAsync(ledgerPath, ct)`).
  3. If ledger disk write fails (I/O error, disk full):
     - Catch exception.
     - **Do NOT** mark journal as `Committed`!
     - Keep / transition journal to `OutcomeUnknown` with diagnostic message.
     - Return `ReconcileResult.StillUnknown(...)`.
  4. Only after successful ledger write:
     - Mark `IntentJournal` as `Committed`.

#### Deficiency 2: Weak Keyset Coverage (Scalar Count vs Full Keyset)
- **Current Check**:
  `if (intent.ItemCount != receipt.ElementMap.Count)`
- **Vulnerability**:
  If an operation targeted `["Doc#Column:C1", "Doc#Column:C2"]` (count = 2), but the Extensible Storage receipt contained `{"Doc#Beam:B1": "uid-b1", "Doc#Wall:W1": "uid-w1"}` (count = 2) or foreign keys:
  - The count check evaluates to `2 == 2` -> **PASSES**!
  - The service updates mappings for completely irrelevant elements and marks the column intent `Committed`!
- **Remediation**:
  1. Extend `IntentJournalRecord` with:
     ```csharp
     public IReadOnlyList<string> SourceKeys { get; init; } = Array.Empty<string>();
     ```
  2. In `BimPipelineEngine.IntentJournal.cs`, populate `SourceKeys = plan.Items.Select(it => it.SourceKey.ToString()).ToList()`.
  3. In `ApplyCommittedReceiptAsync`:
     - If `intent.SourceKeys.Count > 0`:
       - Check bijection / full coverage:
         ```csharp
         var expectedKeys = new HashSet<string>(intent.SourceKeys, StringComparer.OrdinalIgnoreCase);
         var receiptKeys = new HashSet<string>(receipt.ElementMap.Keys, StringComparer.OrdinalIgnoreCase);
         if (!expectedKeys.SetEquals(receiptKeys))
         {
             string reason = $"Receipt keyset mismatch: missing {expectedKeys.Except(receiptKeys).Count()} keys, {receiptKeys.Except(expectedKeys).Count()} foreign keys. Preserving OutcomeUnknown.";
             await _journal.UpdateStatusAsync(intent.OperationId, IntentStatus.OutcomeUnknown, reason, ct).ConfigureAwait(false);
             return ReconcileResult.StillUnknown(intent.OperationId, intent.PlanHash, reason);
         }
         ```
     - If `intent.SourceKeys` is empty (for legacy records): fallback to `intent.ItemCount == receipt.ElementMap.Count`.

#### Deficiency 3: Atomic File Write Mechanism
- The atomic write mechanism is already implemented in `BimCoordinationLedgerSerializer.SerializeToFileAsync`:
  - Acquires companion `.lock` file (`${filePath}.lock`).
  - Writes JSON to temporary file `${filePath}.tmp_{Guid:N}`.
  - Flushes hardware disk buffers via `stream.Flush(flushToDisk: true)`.
  - Atomically replaces target file via `File.Move(tempPath, filePath, overwrite: true)`.
  - Cleans up temp file in `finally`.
- `ReconcileService` must accept `LedgerFilePath` (as optional constructor parameter or property), and invoke `_ledgerService.SaveToFileAsync(LedgerFilePath, ct)` within `ApplyCommittedReceiptAsync`.

---

## 4. Architectural Remediation Design

### 4.1 CrossProcessFileLock Helper
To avoid bloat in `IntentJournal.cs` and ensure compliance with Clean Code Rule C2 (file $\le 300$ lines), extract the cross-process lock into a clean helper:

```csharp
namespace HPBim.Client.Connection;

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public sealed class CrossProcessFileLock : IAsyncDisposable, IDisposable
{
    private readonly FileStream _lockStream;

    private CrossProcessFileLock(FileStream lockStream) => _lockStream = lockStream;

    public static async Task<CrossProcessFileLock> AcquireAsync(
        string lockFilePath, int timeoutMs = 30000, CancellationToken ct = default)
    {
        string? dir = Path.GetDirectoryName(lockFilePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var sw = Stopwatch.StartNew();
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var stream = new FileStream(
                    lockFilePath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    bufferSize: 1,
                    FileOptions.None);

                return new CrossProcessFileLock(stream);
            }
            catch (IOException)
            {
                if (sw.ElapsedMilliseconds >= timeoutMs)
                {
                    throw new TimeoutException($"Failed to acquire file lock '{lockFilePath}' within {timeoutMs}ms.");
                }
                await Task.Delay(15, ct).ConfigureAwait(false);
            }
        }
    }

    public void Dispose() => _lockStream.Dispose();
    public ValueTask DisposeAsync() => _lockStream.DisposeAsync();
}
```

### 4.2 ReconcileService Enhanced Flow
In `ReconcileService.cs`:
- Add `public string? LedgerFilePath { get; set; }`
- In `ApplyCommittedReceiptAsync`:
  1. Validate `receipt.ElementMap != null`.
  2. Validate keyset coverage against `intent.SourceKeys` (or `ItemCount`).
  3. Validate parsing of `CompositeSourceKey` and non-empty `uniqueId`.
  4. Update `_ledgerService.UpdateMapping(...)`.
  5. If `!string.IsNullOrEmpty(LedgerFilePath)`:
     ```csharp
     try
     {
         await _ledgerService.SaveToFileAsync(LedgerFilePath!, ct).ConfigureAwait(false);
     }
     catch (Exception ex)
     {
         string reason = $"Failed to persist coordination ledger to disk at '{LedgerFilePath}': {ex.Message}. Preserving OutcomeUnknown.";
         await _journal.UpdateStatusAsync(intent.OperationId, IntentStatus.OutcomeUnknown, reason, ct).ConfigureAwait(false);
         return ReconcileResult.StillUnknown(intent.OperationId, intent.PlanHash, reason);
     }
     ```
  6. Transition journal to `Committed`:
     ```csharp
     await _journal.UpdateStatusAsync(intent.OperationId, IntentStatus.Committed, null, ct).ConfigureAwait(false);
     return ReconcileResult.Committed(intent.OperationId, intent.PlanHash, receipt.ElementMap);
     ```

---

## 5. Verification Strategy

### 5.1 Verification Commands
- `dotnet test HPBim/tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj`
- `dotnet test HPBim/tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj`
- `dotnet test HPBim/tests/HPBim.E2E.Tests/HPBim.E2E.Tests.csproj`
- `dotnet test HPBim/HPBim.slnx`

### 5.2 New Unit Tests to be Implemented
1. **Multi-Writer Concurrency Test**:
   - `IntentJournalMultiWriterConcurrencyTests.cs`:
     - Test 2 separate `IntentJournal` instances concurrently appending 500 records each (1,000 total).
     - Cold-read via 3rd instance verifying exactly 1,000 distinct, uncorrupted records.
2. **Persist-Before-Terminal Failure Test**:
   - `ReconcilePersistBeforeTerminalTests.cs`:
     - Simulate a disk write failure when saving the ledger (e.g. read-only ledger path or mocked file failure).
     - Verify `journal.GetIntentAsync` remains `OutcomeUnknown` and is NOT `Committed`.
3. **Exact Keyset Coverage Test**:
   - `ReconcileExactKeysetCoverageTests.cs`:
     - Test receipt with matching count but foreign keys -> verify preserved `OutcomeUnknown`.
     - Test receipt with missing expected key -> verify preserved `OutcomeUnknown`.
     - Test receipt with exact matching keyset -> verify successful transition to `Committed`.
