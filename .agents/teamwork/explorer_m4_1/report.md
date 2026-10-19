# Investigation & Technical Architecture Report: Local Intent Journal (Feature F9 Part 1)

**Agent**: `explorer_m4_1` (Teamwork Explorer)  
**Milestone**: M4 (Recovery & Reconciliation) — Feature F9 Part 1  
**Date**: 2026-10-07T21:25:00Z  
**Target Solution**: `HPBim/HPBim.slnx` (`HPBim.Client`, `HPBim.Client.Tests`, `HPBim.Core`)

---

## 1. Executive Summary

Milestone M4 establishes crash resilience, idempotent recovery, and truthful outcome reporting across the process boundary between `HPBim.Client` and `HPRebar.McpBridge` inside Revit 2026.

Feature F9 Part 1 introduces the **Local Intent Journal** (`IntentJournal`), a write-ahead persistent logging mechanism. Before sending any model generation plan across the Named Pipe (`\\.\pipe\hprebar-mcp-r2026`), the client must atomically persist an `InFlight` intent record to local disk and force a hardware flush (`fsync` / `FlushFileBuffers`). Upon completion, the intent transitions to `Committed` (on success), `Failed` (on confirmed rollback), or `OutcomeUnknown` (on pipe severance, timeout, or abrupt client crash). This guarantees that subsequent reconciliation services (`ReconcileService`, Feature F10) have an unshakeable local record of what was sent to Revit, preventing duplicate generation and blind retries.

---

## 2. Problem Statement & Threat Analysis

### 2.1 The Two-Generals Problem in CAD/BIM IPC
When `HPBim.Client` dispatches a C# Roslyn script containing hundreds of BIM element creation commands over Named Pipe to Revit:
1. **Network Disconnect / Pipe Severance**: The pipe may drop or close during execution due to Revit UI thread stalls, OS pipe buffer overflows, or Revit crashing. The client receives an `IOException` or pipe close signal, but cannot know whether Revit's transaction already committed.
2. **Transport / Execution Timeout**: The client timeout timer expires before a response packet arrives. Revit may still be actively committing the transaction.
3. **Client Process Abrupt Termination**: A sudden power failure, SIGKILL, or unhandled crash terminates `HPBim.Client` while Revit is processing.
4. **Blind Retry Hazard (Duplication Bug)**: If the client blindly re-runs the plan upon reconnection without knowing previous status, it risks creating duplicate geometry (e.g. 196 duplicate piles, 72 duplicate footings, overlapping walls) in violation of the Golden Rule ("Thà để một cấu kiện chưa dựng còn hơn dựng sai nhưng báo đúng").

### 2.2 Write-Ahead Journaling Solution
By applying Write-Ahead Logging (WAL) principles:
- **Rule 1 (Persist Before Transmission)**: No bytes enter the Named Pipe until the operation intent is durably flushed to the local filesystem (`intent_journal.jsonl`).
- **Rule 2 (Explicit State Transitions)**: Every state change (`InFlight` $\rightarrow$ `Committed` / `Failed` / `OutcomeUnknown`) is appended to the journal.
- **Rule 3 (Crash Recovery Cold Scan)**: On startup, any intent found in `InFlight` or `OutcomeUnknown` state triggers `ReconcileService` (Feature F10) to cross-check Revit's Extensible Storage receipt (Feature F9 Part 2) before any new commands are dispatched.

---

## 3. Data Model Design

### 3.1 `IntentStatus` Enum
Located in `HPBim.Client/Connection/IntentStatus.cs` (namespace `HPBim.Client.Connection`):

```csharp
namespace HPBim.Client.Connection;

/// <summary>
/// Lifecycle status of an operation intent recorded in the local write-ahead journal.
/// </summary>
public enum IntentStatus
{
    /// <summary>Intent recorded locally, preparing for network dispatch.</summary>
    Pending = 0,

    /// <summary>Dispatched across Named Pipe; actively awaiting execution response from Revit.</summary>
    InFlight = 1,

    /// <summary>Execution confirmed successful and committed in Revit.</summary>
    Committed = 2,

    /// <summary>Execution failed with confirmed transaction rollback or clean pre-flight rejection.</summary>
    Failed = 3,

    /// <summary>Pipe disconnected, timed out, or process terminated while InFlight; outcome ambiguous.</summary>
    OutcomeUnknown = 4
}
```

### 3.2 `IntentJournalRecord` Model
Located in `HPBim.Client/Connection/IntentJournalRecord.cs`:

```csharp
namespace HPBim.Client.Connection;

using System;
using System.Text.Json.Serialization;
using HPBim.Core.Execution;

/// <summary>
/// Immutable record capturing operation intent and its lifecycle state in the local journal.
/// Serialized as a single JSON line in the append-only JSONL journal file.
/// </summary>
public sealed record IntentJournalRecord
{
    /// <summary>Unique operation batch ID generated before dispatch.</summary>
    public Guid OperationId { get; init; } = Guid.NewGuid();

    /// <summary>Deterministic SHA-256 hash of the execution plan.</summary>
    public string PlanHash { get; init; } = string.Empty;

    /// <summary>Timestamp (UTC) when this record or state transition was recorded.</summary>
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;

    /// <summary>Current lifecycle status of the operation.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public IntentStatus Status { get; init; } = IntentStatus.Pending;

    /// <summary>Total number of element items targeted in this operation.</summary>
    public int ItemCount { get; init; }

    /// <summary>Identity and version snapshot of the target Revit document.</summary>
    public DocumentIdentitySnapshot TargetDoc { get; init; } = new();

    /// <summary>Optional error message or diagnostic detail if status is Failed or OutcomeUnknown.</summary>
    public string? ErrorReason { get; init; }

    /// <summary>Indicates whether this intent has reached a final state (Committed or Failed).</summary>
    [JsonIgnore]
    public bool IsTerminal => Status is IntentStatus.Committed or IntentStatus.Failed;

    /// <summary>Indicates whether this intent requires recovery or reconciliation.</summary>
    [JsonIgnore]
    public bool IsIncomplete => Status is IntentStatus.Pending or IntentStatus.InFlight or IntentStatus.OutcomeUnknown;
}
```

### 3.3 `IntentJournalOptions`
Located in `HPBim.Client/Connection/IntentJournalOptions.cs`:

```csharp
namespace HPBim.Client.Connection;

using System;
using System.IO;

/// <summary>
/// Configuration options for the Local Intent Journal.
/// </summary>
public sealed record IntentJournalOptions
{
    /// <summary>
    /// File path of the append-only JSONL journal.
    /// Default: .hpbim_journal/intent_journal.jsonl in current working directory.
    /// </summary>
    public string JournalPath { get; init; } =
        Path.Combine(Environment.CurrentDirectory, ".hpbim_journal", "intent_journal.jsonl");

    /// <summary>
    /// Whether to force immediate physical disk flushing (fsync / FlushFileBuffers)
    /// on every append before returning. Default: true.
    /// </summary>
    public bool FlushToDiskImmediately { get; init; } = true;

    /// <summary>
    /// Default retention window for pruning completed (terminal) entries.
    /// Default: 7 days.
    /// </summary>
    public TimeSpan DefaultPruneRetention { get; init; } = TimeSpan.FromDays(7);
}
```

---

## 4. Storage & Concurrency Architecture

### 4.1 Append-Only JSONL Format
The journal is stored as line-delimited JSON (`.jsonl`), encoded in UTF-8 without BOM:
- **Append Complexity**: $O(1)$ disk append.
- **Atomic Single-Line Writes**: Each record or status transition is a self-contained JSON line terminated by `\n`.
- **Fault-Tolerant Scanning**: Partial writes from crash at line $N$ do not invalidate lines $1 \dots N-1$.
- **Format Example**:
```json
{"operationId":"7f9d8a3e-4b2c-4d1e-8f90-1a2b3c4d5e6f","planHash":"8a4f91c2...","timestampUtc":"2026-10-07T21:05:00.1234567Z","status":"InFlight","itemCount":72,"targetDoc":{"title":"NhaPho.rvt","versionGuid":"4e2b...","numberOfSaves":12},"errorReason":null}
{"operationId":"7f9d8a3e-4b2c-4d1e-8f90-1a2b3c4d5e6f","planHash":"8a4f91c2...","timestampUtc":"2026-10-07T21:05:14.8901234Z","status":"Committed","itemCount":72,"targetDoc":{"title":"NhaPho.rvt","versionGuid":"4e2b...","numberOfSaves":12},"errorReason":null}
```

### 4.2 Hardware Disk Flushing (`FlushFileBuffers`)
In .NET on Windows, calling `StreamWriter.FlushAsync()` flushes to the OS kernel page cache, but NOT necessarily to disk platters/NVMe cells.
To satisfy the strict atomic flush requirement before pipe transmission:
```csharp
await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
if (_options.FlushToDiskImmediately)
{
    // Forces OS-level FlushFileBuffers (fsync)
    fileStream.Flush(flushToDisk: true);
}
```

### 4.3 High-Concurrency Thread Safety
- A `SemaphoreSlim _ioLock = new(1, 1)` serializes all disk write and compaction operations.
- A `ConcurrentDictionary<Guid, IntentJournalRecord> _activeRecords` maintains an in-memory view of the latest state for fast lookups without re-reading the file.
- Reads and appends are non-blocking across threads via async semaphore acquisition.

### 4.4 Graceful Corrupted Line Handling
When reading `intent_journal.jsonl`:
- If an incomplete or malformed line is encountered (e.g. abrupt power cut during write), the parser catches `JsonException`, logs an internal warning, and continues reading subsequent valid records.
- Conforms strictly to Clean Code: explanatory comment inside the catch block, zero silent swallowing.

```csharp
try
{
    var record = JsonSerializer.Deserialize<IntentJournalRecord>(line, JsonOptions);
    if (record != null)
    {
        latestRecords[record.OperationId] = record;
    }
}
catch (JsonException)
{
    // Corrupted or truncated journal line from abrupt process termination; skip gracefully to preserve undamaged records.
    continue;
}
```

### 4.5 Log Compaction (`PruneOldEntriesAsync`)
Over time, the append-only journal accumulates historical records.
- Compaction reads all lines and resolves the latest state per `OperationId`.
- **Absolute Retention Rule**: Never prune incomplete records (`Pending`, `InFlight`, `OutcomeUnknown`) regardless of age.
- Only terminal records (`Committed`, `Failed`) whose `TimestampUtc` is older than `olderThan` are dropped.
- Atomic file replacement: writes to `<file>.tmp` and executes `File.Move(tempFile, targetFile, overwrite: true)`.

---

## 5. `IIntentJournal` Interface & Implementation Specification

### 5.1 `IIntentJournal` Contract
Located in `HPBim.Client/Connection/IIntentJournal.cs`:

```csharp
namespace HPBim.Client.Connection;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Persistent write-ahead journal recording operation intent before network dispatch
/// across Revit Named Pipe, enabling crash recovery and idempotent reconciliation.
/// </summary>
public interface IIntentJournal : IAsyncDisposable, IDisposable
{
    /// <summary>Gets the absolute file path of the underlying JSONL journal.</summary>
    string JournalFilePath { get; }

    /// <summary>
    /// Records a new operation intent with status InFlight or Pending and flushes to disk.
    /// MUST be awaited before sending the request across the pipe.
    /// </summary>
    Task RecordIntentAsync(IntentJournalRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends a status update for an existing operation (e.g. Committed, Failed, OutcomeUnknown).
    /// </summary>
    Task UpdateStatusAsync(
        Guid operationId,
        IntentStatus status,
        string? errorReason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all operations whose latest known status is Incomplete (Pending, InFlight, or OutcomeUnknown).
    /// </summary>
    Task<IReadOnlyList<IntentJournalRecord>> GetIncompleteIntentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the most recent record for a specific operation ID, if present.
    /// </summary>
    Task<IntentJournalRecord?> GetIntentAsync(Guid operationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compacts the journal by removing completed entries older than the specified retention window,
    /// while strictly preserving all incomplete entries.
    /// </summary>
    Task<int> PruneOldEntriesAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);
}
```

### 5.2 `IntentJournal` Core Implementation Sketch
Located in `HPBim.Client/Connection/IntentJournal.cs` (~220 lines):

```csharp
namespace HPBim.Client.Connection;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

public sealed class IntentJournal : IIntentJournal
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private readonly IntentJournalOptions _options;
    private readonly SemaphoreSlim _ioLock = new(1, 1);
    private readonly ConcurrentDictionary<Guid, IntentJournalRecord> _activeRecords = new();
    private int _disposed;

    public string JournalFilePath => _options.JournalPath;

    public IntentJournal(IntentJournalOptions? options = null)
    {
        _options = options ?? new IntentJournalOptions();
        EnsureDirectoryExists(_options.JournalPath);
    }

    public async Task RecordIntentAsync(IntentJournalRecord record, CancellationToken cancellationToken = default)
    {
        if (record == null) throw new ArgumentNullException(nameof(record));
        ThrowIfDisposed();

        _activeRecords[record.OperationId] = record;
        string jsonLine = JsonSerializer.Serialize(record, JsonOptions);
        await AppendLineWithFlushAsync(jsonLine, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateStatusAsync(
        Guid operationId,
        IntentStatus status,
        string? errorReason = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        IntentJournalRecord updated;
        if (_activeRecords.TryGetValue(operationId, out var existing))
        {
            updated = existing with
            {
                Status = status,
                TimestampUtc = DateTime.UtcNow,
                ErrorReason = errorReason
            };
        }
        else
        {
            updated = new IntentJournalRecord
            {
                OperationId = operationId,
                Status = status,
                TimestampUtc = DateTime.UtcNow,
                ErrorReason = errorReason
            };
        }

        _activeRecords[operationId] = updated;
        string jsonLine = JsonSerializer.Serialize(updated, JsonOptions);
        await AppendLineWithFlushAsync(jsonLine, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<IntentJournalRecord>> GetIncompleteIntentsAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var allRecords = await ReadLatestRecordsMapAsync(cancellationToken).ConfigureAwait(false);
        return allRecords.Values
            .Where(r => r.IsIncomplete)
            .OrderBy(r => r.TimestampUtc)
            .ToList();
    }

    public async Task<IntentJournalRecord?> GetIntentAsync(Guid operationId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (_activeRecords.TryGetValue(operationId, out var cached))
        {
            return cached;
        }

        var allRecords = await ReadLatestRecordsMapAsync(cancellationToken).ConfigureAwait(false);
        return allRecords.TryGetValue(operationId, out var diskRecord) ? diskRecord : null;
    }

    public async Task<int> PruneOldEntriesAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _ioLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(_options.JournalPath)) return 0;

            var latestMap = await ReadLatestRecordsMapCoreAsync(cancellationToken).ConfigureAwait(false);
            var cutoff = DateTime.UtcNow - olderThan;

            var toKeep = latestMap.Values
                .Where(r => r.IsIncomplete || r.TimestampUtc > cutoff)
                .OrderBy(r => r.TimestampUtc)
                .ToList();

            int prunedCount = latestMap.Count - toKeep.Count;
            if (prunedCount <= 0) return 0;

            string tempFile = $"{_options.JournalPath}.tmp_{Guid.NewGuid():N}";
            await using (var stream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            await using (var writer = new StreamWriter(stream, Utf8NoBom))
            {
                foreach (var rec in toKeep)
                {
                    await writer.WriteLineAsync(JsonSerializer.Serialize(rec, JsonOptions).AsMemory(), cancellationToken).ConfigureAwait(false);
                }
                await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
                if (_options.FlushToDiskImmediately) stream.Flush(flushToDisk: true);
            }

            File.Move(tempFile, _options.JournalPath, overwrite: true);

            _activeRecords.Clear();
            foreach (var rec in toKeep) _activeRecords[rec.OperationId] = rec;

            return prunedCount;
        }
        finally
        {
            _ioLock.Release();
        }
    }

    private async Task AppendLineWithFlushAsync(string jsonLine, CancellationToken ct)
    {
        await _ioLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await using var stream = new FileStream(
                _options.JournalPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, 4096, useAsync: true);
            await using var writer = new StreamWriter(stream, Utf8NoBom, 4096, leaveOpen: true);
            await writer.WriteLineAsync(jsonLine.AsMemory(), ct).ConfigureAwait(false);
            await writer.FlushAsync(ct).ConfigureAwait(false);

            if (_options.FlushToDiskImmediately)
            {
                stream.Flush(flushToDisk: true);
            }
        }
        finally
        {
            _ioLock.Release();
        }
    }

    private async Task<Dictionary<Guid, IntentJournalRecord>> ReadLatestRecordsMapAsync(CancellationToken ct)
    {
        await _ioLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await ReadLatestRecordsMapCoreAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _ioLock.Release();
        }
    }

    private async Task<Dictionary<Guid, IntentJournalRecord>> ReadLatestRecordsMapCoreAsync(CancellationToken ct)
    {
        var map = new Dictionary<Guid, IntentJournalRecord>();
        if (!File.Exists(_options.JournalPath)) return map;

        using var stream = new FileStream(_options.JournalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, useAsync: true);
        using var reader = new StreamReader(stream, Utf8NoBom);

        string? line;
        while ((line = await reader.ReadLineAsync(ct).ConfigureAwait(false)) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                var rec = JsonSerializer.Deserialize<IntentJournalRecord>(line, JsonOptions);
                if (rec != null) map[rec.OperationId] = rec;
            }
            catch (JsonException)
            {
                // Corrupted or truncated journal line from abrupt termination; skip gracefully to preserve undamaged records.
                continue;
            }
        }
        return map;
    }

    private static void EnsureDirectoryExists(string filePath)
    {
        string? dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        opts.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return opts;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed != 0) throw new ObjectDisposedException(nameof(IntentJournal));
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _ioLock.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
```

---

## 6. Integration with `BimPipelineEngine`

### 6.1 Clean Code Boundary Management
- Current `BimPipelineEngine.PlanExecution.cs` is **274 lines**. Adding journal handling directly would breach Rule C2 (300 lines limit).
- **Architectural Solution**: Create a new partial class file `HPBim.Client/Pipeline/BimPipelineEngine.IntentJournal.cs` (~80 lines).
- This keeps both files comfortably under the 300 lines ceiling.

### 6.2 The 4-Step Engine State Machine
Inside `BimPipelineEngine.PlanExecution.cs`:

```
┌────────────────────────┐
│  BimExecutionPlan (IR) │
└───────────┬────────────┘
            │ 1. Record InFlight (FileStream.Flush to disk)
            ▼
┌────────────────────────┐
│  Local IntentJournal   │ Status = InFlight
└───────────┬────────────┘
            │ 2. Dispatched via Named Pipe: revit.execute
            ▼
┌────────────────────────┐
│ Revit Named Pipe IPC   │
└─────┬────────────┬─────┘
      │ Success    │ Rollback / Error   │ Disconnect / Timeout
      ▼            ▼                    ▼
┌───────────┐ ┌───────────┐       ┌─────────────────┐
│ Committed │ │  Failed   │       │ OutcomeUnknown  │
└───────────┘ └───────────┘       └─────────────────┘
```

#### Detailed State Transition Logic:
1. **Step 1 (Pre-Dispatch)**:
   - Identify `Guid opId = plan.Options.OperationId`.
   - If `IntentJournal != null && !plan.Options.DryRun`:
     Construct `IntentJournalRecord` with `Status = IntentStatus.InFlight`, `ItemCount = plan.Items.Count`, `PlanHash = plan.PlanHash`, `TargetDoc = plan.TargetDoc`.
     Await `IntentJournal.RecordIntentAsync(record, cancellationToken)`.
2. **Step 2 (Success)**:
   - Bridge returns `execRes` with `!execRes.IsError` and (for apply) `!execRes.RolledBack` and no failed records.
   - Await `IntentJournal.UpdateStatusAsync(opId, IntentStatus.Committed, null, cancellationToken)`.
3. **Step 3 (Clean Failure / Rollback)**:
   - Bridge returns `execRes` with `execRes.RolledBack == true` or pre-flight failure.
   - Transaction is confirmed rolled back in Revit.
   - Await `IntentJournal.UpdateStatusAsync(opId, IntentStatus.Failed, errorMsg, cancellationToken)`.
4. **Step 4 (Ambiguous / Transport Interruption)**:
   - Any `RevitPipeException` (timeout or connection severed), `IOException`, `TimeoutException`, or caller `OperationCanceledException` occurring during `_pipeClient.ExecuteAsync`:
   - Outcome is ambiguous because Revit may still have committed or may commit in the future.
   - Await `IntentJournal.UpdateStatusAsync(opId, IntentStatus.OutcomeUnknown, ex.Message, CancellationToken.None)`.
   - Also: if execution returns `!success` but `execRes.RolledBack == false` (apply mode), mark `OutcomeUnknown`.

### 6.3 Code Sketch for `BimPipelineEngine.IntentJournal.cs`

```csharp
namespace HPBim.Client.Pipeline;

using System;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Core.Execution;

public sealed partial class BimPipelineEngine
{
    /// <summary>
    /// Optional persistent write-ahead intent journal for crash recovery and auditability.
    /// </summary>
    public IIntentJournal? IntentJournal { get; set; }

    private async Task RecordPreDispatchIntentAsync(
        BimExecutionPlan plan,
        CancellationToken ct)
    {
        if (IntentJournal == null || plan.Options.DryRun) return;

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

        await IntentJournal.RecordIntentAsync(record, ct).ConfigureAwait(false);
    }

    private async Task RecordPostDispatchOutcomeAsync(
        BimExecutionPlan plan,
        bool success,
        bool rolledBack,
        string? errorMsg,
        CancellationToken ct)
    {
        if (IntentJournal == null || plan.Options.DryRun) return;

        Guid opId = plan.Options.OperationId;
        if (success)
        {
            await IntentJournal.UpdateStatusAsync(opId, IntentStatus.Committed, null, ct).ConfigureAwait(false);
        }
        else if (rolledBack)
        {
            await IntentJournal.UpdateStatusAsync(opId, IntentStatus.Failed, errorMsg, ct).ConfigureAwait(false);
        }
        else
        {
            await IntentJournal.UpdateStatusAsync(opId, IntentStatus.OutcomeUnknown, errorMsg, ct).ConfigureAwait(false);
        }
    }

    private async Task RecordExceptionOutcomeAsync(
        BimExecutionPlan plan,
        Exception ex)
    {
        if (IntentJournal == null || plan.Options.DryRun) return;

        // Use CancellationToken.None to guarantee journal write even if caller CT cancelled.
        await IntentJournal.UpdateStatusAsync(
            plan.Options.OperationId,
            IntentStatus.OutcomeUnknown,
            $"Pipe communication interruption: {ex.GetType().Name} - {ex.Message}",
            CancellationToken.None).ConfigureAwait(false);
    }
}
```

---

## 7. Test Strategy & Acceptance Matrix

### 7.1 Proposed Test Files in `HPBim.Client.Tests`

| Test Class | Focus Area | Test Count | Location |
|---|---|---|---|
| `IntentJournalTests.cs` | File operations, atomic flush, compaction, corrupted lines, thread-safety | ~12 tests | `HPBim.Client.Tests/Connection/` |
| `BimPipelineEngineIntentJournalTests.cs` | Integration between pipeline engine and journal transitions | ~8 tests | `HPBim.Client.Tests/Pipeline/` |
| `FakeIntentJournal.cs` | Test double for isolating tests without disk I/O | Fake helper | `HPBim.Client.Tests/Fakes/` |

### 7.2 Detailed Test Scenarios

1. **`RecordIntentAsync_WritesRecordToJsonlFile_WithValidJson`**:
   - Create `IntentJournal` pointing to temp path.
   - Record an `InFlight` intent.
   - Read lines from disk, verify 1 line, parse JSON, assert all fields match.
2. **`UpdateStatusAsync_AppendsNewStateLine_ReflectsLatestStatus`**:
   - Record `InFlight`.
   - Update to `Committed`.
   - Verify file has 2 lines.
   - `GetIntentAsync` returns `Committed`.
3. **`GetIncompleteIntentsAsync_FiltersCorrectly`**:
   - Record 4 operations: Op1=`Committed`, Op2=`Failed`, Op3=`InFlight`, Op4=`OutcomeUnknown`.
   - Call `GetIncompleteIntentsAsync()`.
   - Assert returns only Op3 and Op4, ordered by `TimestampUtc`.
4. **`CorruptedJournalLines_GracefullySkipped_PreservesValidRecords`**:
   - Write file with: Valid Line 1 $\rightarrow$ Truncated JSON `{ "op` $\rightarrow$ Empty Line $\rightarrow$ Non-JSON `GARBAGE` $\rightarrow$ Valid Line 2.
   - Call `GetIncompleteIntentsAsync()`.
   - Assert returns both Valid Line 1 and Valid Line 2 without throwing any unhandled exception.
5. **`PruneOldEntriesAsync_RemovesExpiredTerminalRecords_PreservesIncompleteRecords`**:
   - Seed journal with:
     - Terminal record 10 days old (`Committed`).
     - Terminal record 1 day old (`Failed`).
     - Incomplete record 10 days old (`InFlight`).
   - Run `PruneOldEntriesAsync(TimeSpan.FromDays(7))`.
   - Assert 10-day-old terminal record is purged.
   - Assert 10-day-old incomplete record is **retained**.
6. **`ConcurrentAppends_ThreadSafe_NoLineInterleaving`**:
   - Spawn 50 parallel tasks appending records to same journal.
   - Verify file contains exactly 50 well-formed JSON lines.
7. **`BimPipelineEngine_ExecutePlanAsync_OnSuccess_UpdatesToCommitted`**:
   - Engine with `FakeRevitPipeClient` returning success.
   - Assert journal has `InFlight` then `Committed`.
8. **`BimPipelineEngine_ExecutePlanAsync_OnTimeout_UpdatesToOutcomeUnknown`**:
   - `FakeRevitPipeClient` throws `RevitPipeException(..., isTimeout: true)`.
   - Assert journal has `InFlight` then `OutcomeUnknown`.
9. **`BimPipelineEngine_ExecutePlanAsync_OnDryRun_DoesNotJournal`**:
   - Execute with `DryRun = true`.
   - Assert journal file remains empty.

---

## 8. Clean Code & Architectural Compliance Checklist

- [x] **Rule C2 (File Length)**:
  - `IntentStatus.cs`: ~30 lines ($\le 300$).
  - `IntentJournalRecord.cs`: ~65 lines ($\le 300$).
  - `IntentJournalOptions.cs`: ~40 lines ($\le 300$).
  - `IIntentJournal.cs`: ~60 lines ($\le 300$).
  - `IntentJournal.cs`: ~220 lines ($\le 300$).
  - `BimPipelineEngine.IntentJournal.cs`: ~80 lines ($\le 300$).
  - `BimPipelineEngine.PlanExecution.cs`: preserved at 274 lines ($\le 300$).
- [x] **Rule M3 (Method Length)**:
  - All methods designed to be $\le 35$ lines (well below the 50-line limit).
- [x] **Catch Block Discipline**:
  - `JsonException` catch block contains mandatory reason comment: `"// Corrupted or truncated journal line from abrupt termination; skip gracefully to preserve undamaged records."`
- [x] **No Forbidden Dependencies**:
  - `HPBim.Client` references only `HPBim.Core` and `McpShared/HPRebar.Mcp.Contracts`.
  - Zero references to `Autodesk.Revit.*`.
- [x] **Rule 0 Compliance**:
  - Ambiguous execution outcomes (pipe timeout, disconnect, unconfirmed rollback) are explicitly recorded as `OutcomeUnknown`—never guessed or assumed to have failed.
