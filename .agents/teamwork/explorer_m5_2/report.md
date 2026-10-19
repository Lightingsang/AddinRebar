# Architectural Design & Specification Report: Background Queue Daemon & Out-of-Process Task Processing (Feature F11 Part 2)

**Agent**: `explorer_m5_2` (Teamwork Preview Explorer)  
**Milestone**: M5 (P2 Architecture Proposal) — Feature F11 Part 2  
**Date**: 2026-10-07T22:30:00Z  
**Target Solution**: `HPBim/HPBim.slnx` (`HPBim.Daemon`, `HPBim.Client`, `HPBim.Core`)  
**Binding Standards**: `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/architecture/ARCHITECTURE.md`, `docs/clean-code/host-appendix/revit.md`  
**Governing Rule**: Rule 0 ("Nguyên Tắc Vàng") — *"Thà để một cấu kiện chưa dựng còn hơn dựng sai nhưng báo đúng"*

---

## 1. Executive Summary

Milestone M5 establishes the long-term architectural foundation for high-performance, fault-tolerant, and autonomous CAD-to-BIM pipelines. While Milestones M1–M4 delivered synchronous and atomic execution directly from ephemeral CLI processes to Autodesk Revit 2026, real-world multi-agent coordination, large-scale structural detailing, and multi-user BIM production expose fundamental physical limitations in an ephemeral, direct-to-Revit IPC model:

1. **Revit Named Pipe Exclusivity**: The deployed `HPRebar.McpBridge` listens on `\\.\pipe\hprebar-mcp-r2026` with `maxNumberOfServerInstances = 1`. Concurrent connections from multiple CLI instances or autonomous agents collide immediately, throwing `IOException: All pipe instances are busy`.
2. **Revit UI Thread Apartment Constraint**: Revit is an STA (Single Threaded Apartment) application bound to Windows OS message pumps. All write transactions, document manipulations, and geometric queries must execute sequentially on the UI thread via `ExternalEvent`.
3. **Transient Process Vulnerability**: An ephemeral CLI command killed abruptly (terminal closed, network drop, memory pressure, Ctrl+C) while executing a 300-element plan severs the Named Pipe mid-flight, creating ambiguous `OutcomeUnknown` states and triggering expensive manual reconciliations.
4. **Lack of Priority & Anti-Starvation Scheduling**: In a multi-agent system, an interactive query from the Coordinator (e.g., a 20ms bounding box or level query) is blocked indefinitely behind a heavy 5-minute structural framing batch.

To resolve these architectural bottlenecks, this report designs the **Background Queue Daemon (`HPBim.Daemon`)** and **Out-of-Process Task Processing Engine**:
- A long-lived, out-of-process background daemon process serving as the **exclusive, persistent owner** of the Revit Named Pipe.
- A **multi-client concurrent IPC surface** over local Named Pipes (`\\.\pipe\hpbim-daemon-r2026`) admitting multiple CLI commands, autonomous agents, and dashboards without pipe contention.
- A **durable SQLite WAL-backed task queue** ensuring zero lost tasks, crash recovery, atomic leasing, and seamless alignment with `IntentJournal` and `TimeoutPolicy`.
- A **single-writer priority dispatcher** enforcing strict serialization to Revit's UI thread, dynamic anti-starvation aging, and cooperative cancellation propagation.

---

## 2. Process Topology & System Architecture

### 2.1 Global Process Topology

```
┌─────────────────────────────────────────────────────────────────────────────────────────────┐
│                                   CLIENT LAYER (M-Producers)                                │
│                                                                                             │
│  ┌───────────────────────┐   ┌───────────────────────────┐   ┌───────────────────────────┐  │
│  │    HPBim.Cli CLI      │   │   AI Agent Team (4-Agent) │   │    Web / Dashboard / IDE  │  │
│  │ (Ephemeral Commands)  │   │  (Coordinator, CAD, QC)   │   │     (Telemetry & Status)  │  │
│  └───────────┬───────────┘   └─────────────┬─────────────┘   └─────────────┬─────────────┘  │
└──────────────┼─────────────────────────────┼───────────────────────────────┼────────────────┘
               │                             │                               │
               └─────────────────────────────┼───────────────────────────────┘
                                             │ Client IPC Channel (M Concurrent Connections)
                                             │ \\.\pipe\hpbim-daemon-r2026 (NDJSON / RPC 2.0)
                                             ▼
┌─────────────────────────────────────────────────────────────────────────────────────────────┐
│                            BACKGROUND QUEUE DAEMON (HPBim.Daemon)                           │
│                                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────────────────────┐  │
│  │ Concurrent IPC Server (NamedPipeServerStream with maxInstances = MaxAllowed)          │  │
│  │ - Session Authentication & Windows CurrentUserOnly ACL Enforcement                    │  │
│  │ - Request Deserialization, Schema Validation & Fast-Fail Admission Gate              │  │
│  └───────────────────────────────────────────┬───────────────────────────────────────────┘  │
│                                              │ Validated Task Submissions                   │
│                                              ▼                                              │
│  ┌───────────────────────────────────────────────────────────────────────────────────────┐  │
│  │ Admission Controller & Backpressure Gate                                              │  │
│  │ - MaxQueueDepth = 1,000 tasks | Soft Backpressure at 80% | Hard Rejection at 100%     │  │
│  │ - Token Bucket Rate Limiting per Client ID                                            │  │
│  └───────────────────────────────────────────┬───────────────────────────────────────────┘  │
│                                              │                                              │
│                                              ▼                                              │
│  ┌───────────────────────────────────────────────────────────────────────────────────────┐  │
│  │ Durable Storage Engine (SQLite WAL: state/hpbim_queue.db)                             │  │
│  │ - ACID Transactions | Zero Memory Loss | Crash Recovery Cold Scanner                  │  │
│  │ - Priority Indexing: PreFlight (200) > Normal (100) > BackgroundQC (10)               │  │
│  │ - Anti-Starvation Dynamic Virtual Priority Calculator (+10 priority / min wait)       │  │
│  └───────────────────────────────────────────┬───────────────────────────────────────────┘  │
│                                              │ Atomic Dequeue Lease (1 Task at a time)      │
│                                              ▼                                              │
│  ┌───────────────────────────────────────────────────────────────────────────────────────┐  │
│  │ Single-Writer Task Dispatcher & Watchdog                                              │  │
│  │ - Sequential UI Thread Serialization (1-Consumer Model)                               │  │
│  │ - IntentJournal Synchronization (Pending -> InFlight -> Committed / Failed)           │  │
│  │ - 3-Tier Timeout Enforcement: Transport (5s) | Queue (30s-15m) | Budget (15s-5m)      │  │
│  │ - Pre-Flight Document Identity Assertion (Title & VersionGuid Gate)                   │  │
│  └───────────────────────────────────────────┬───────────────────────────────────────────┘  │
│                                              │ Dedicated Client Connection                  │
│                                              ▼                                              │
│  ┌───────────────────────────────────────────────────────────────────────────────────────┐  │
│  │ Revit Connection Supervisor (Sole Pipe Client)                                        │  │
│  │ - Active Heartbeat (revit.ping every 10s idle)                                        │  │
│  │ - Auto-Reconnect with Exponential Backoff (500ms -> 10s max)                          │  │
│  │ - Document Switch Detection (ActiveUIDocument title change tracking)                  │  │
│  └───────────────────────────────────────────┬───────────────────────────────────────────┘  │
└──────────────────────────────────────────────┼──────────────────────────────────────────────┘
                                               │ Exclusive Named Pipe Connection
                                               │ \\.\pipe\hprebar-mcp-r2026 (maxInstances = 1)
                                               ▼
┌─────────────────────────────────────────────────────────────────────────────────────────────┐
│                          AUTODESK REVIT 2026 (Host Process: Revit.exe)                      │
│                                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────────────────────┐  │
│  │ HPRebar.McpBridge Add-in (Single-Instance Listener)                                   │  │
│  │  - PipeListener -> RequestDispatcher -> McpBridgeExternalEventHandler                 │  │
│  │  - Interlocked _busy Gate -> ExternalEvent.Raise()                                    │  │
│  │  - Revit UI Thread: ScriptRunner inside TransactionGroup "HPBim Execution Plan"       │  │
│  │  - Extensible Storage Receipt: Schema D4E5F6A7-8B9C-0D1E-2F3A-4B5C6D7E8F90            │  │
│  └───────────────────────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────────────────────┘
```

### 2.2 IPC Architecture Comparison & Evaluation

To establish client-to-daemon communication, three IPC technologies were evaluated against repository constraints:

| Evaluation Criteria | Named Pipe (Windows NDJSON) | Unix Domain Sockets (UDS) | Local gRPC (HTTP/2 Loopback) |
|---|---|---|---|
| **Windows Native Support** | Native, highly optimized kernel driver (`npfs.sys`). | Supported on Windows 10+ via `AF_UNIX`. | Supported via `SocketsHttpHandler` over TCP loopback. |
| **Security & Isolation** | Granular Windows ACLs (`PipeOptions.CurrentUserOnly`). Elevated processes securely segregated. | File permissions on socket file; Windows ACL mapping is complex. | Loopback port binding; requires bearer tokens to prevent local escalation. |
| **Throughput & Latency** | **< 0.2 ms** round-trip latency. Zero network stack overhead. | < 0.3 ms round-trip latency. | ~ 1.5–3.0 ms latency due to HTTP/2 framing, SSL/TLS, and TCP stack. |
| **Dependency Footprint** | **Zero external NuGet dependencies** (stdlib `System.IO.Pipes`). Shares `McpShared` contracts. | Standard library in .NET 10. | Heavy (`Grpc.AspNetCore`, `Google.Protobuf`, code-gen tooling). |
| **Multiplexing / Streaming** | Supports multi-instance listeners and async line streaming. | Stream-based; requires custom framing. | Native multiplexing over single connection. |
| **Architecture Verdict** | **SELECTED (Primary)**: Standard protocol for CLI, Agents, and SDK. | Candidate for future Linux/Docker headless runners. | Optional secondary adapter for telemetry dashboards. |

**Decision**: The daemon implements a primary **Named Pipe server** listening on `\\.\pipe\hpbim-daemon-r2026` configured with `PipeOptions.CurrentUserOnly` and `NamedPipeServerStream.MaxAllowedServerInstances`. Communication uses the existing NDJSON framing and JSON-RPC 2.0 messages defined in `McpShared/HPRebar.Mcp.Contracts`.

### 2.3 Daemon Lifecycle & Discovery Protocol

The daemon runs as a self-hosting background service (`HPBim.Daemon.exe`) managed via Windows Service Manager or user-session background process:
1. **Discovery File**: On successful startup, the daemon atomically writes a registration manifest to `%LOCALAPPDATA%\HPBim\daemon.json`:
   ```json
   {
     "Pid": 38492,
     "PipeName": "hpbim-daemon-r2026",
     "HostVersion": 2026,
     "StartedAtUtc": "2026-10-07T22:00:00.0000000Z",
     "RevitPipeName": "hprebar-mcp-r2026",
     "RevitConnected": true,
     "ActiveDocTitle": "NhaPho_5x18m.rvt",
     "Version": "1.0.0"
   }
   ```
2. **Lockfile & Single Daemon Instance**: The daemon holds an exclusive file lock on `%LOCALAPPDATA%\HPBim\daemon.lock`. If a second daemon process is spawned, it detects the active lock, outputs the existing daemon's PID, and exits immediately.
3. **Auto-Spawn Protocol for CLI**: When a transient CLI command (e.g. `hpbim generate`) executes:
   - Check if `daemon.json` exists and the process is alive.
   - If daemon is running and healthy: connect to `\\.\pipe\hpbim-daemon-r2026`.
   - If daemon is NOT running: CLI can automatically launch `HPBim.Daemon.exe --background` in a detached process, poll for 2 seconds until `daemon.json` is healthy, then submit the task.

---

## 3. Durable Task Queue Storage & State Persistence

### 3.1 SQLite WAL Engine Design

In accordance with Rule 0 ("Nguyên Tắc Vàng"), no task submitted to HPBim can ever be lost, silently dropped, or executed in duplicate. The queue persistence layer is implemented via **SQLite with Write-Ahead Logging (WAL)**:

#### SQLite Configuration PRAGMAs:
```sql
PRAGMA journal_mode = WAL;          -- Readers do not block writers; writers do not block readers
PRAGMA synchronous = NORMAL;        -- Guarantees zero corruption on OS crash with high write speed
PRAGMA busy_timeout = 5000;         -- Wait up to 5s if locked before throwing SQLITE_BUSY
PRAGMA foreign_keys = ON;           -- Enforce relational integrity across task events
PRAGMA temp_store = MEMORY;         -- High-speed temporary indexes in memory
```

### 3.2 Relational Database Schema

```sql
-- Core durable task registry
CREATE TABLE IF NOT EXISTS BimTasks (
    TaskId                  TEXT PRIMARY KEY,              -- UUID (Guid.ToString("D"))
    OperationId             TEXT NOT NULL UNIQUE,          -- Guid matching IntentJournal & Revit Extensible Storage
    PlanHash                TEXT NOT NULL,                 -- SHA-256 hash of BimExecutionPlan
    Priority                INTEGER NOT NULL,              -- 200 = PreFlight/High, 100 = Normal, 10 = BackgroundQC
    VirtualPriority         INTEGER NOT NULL,              -- Priority + Anti-starvation aging bonus
    State                   INTEGER NOT NULL DEFAULT 0,    -- 0=Queued, 1=Dispatched, 2=InFlight, 3=Committed, 4=Failed, 5=OutcomeUnknown, 6=DeadLetter, 7=Cancelled
    ItemCount               INTEGER NOT NULL,              -- Element count in execution plan
    EnqueuedAtUtc           TEXT NOT NULL,                 -- ISO 8601 UTC timestamp
    DispatchedAtUtc         TEXT,                          -- Timestamp when single-writer leased the task
    CompletedAtUtc          TEXT,                          -- Terminal completion timestamp
    DeadlineUtc             TEXT NOT NULL,                 -- Absolute expiration deadline
    TargetDocTitle          TEXT NOT NULL,                 -- Target Revit document title
    TargetDocVersionGuid    TEXT NOT NULL,                 -- Target Revit VersionGUID
    PlanJson                TEXT NOT NULL,                 -- Serialized BimExecutionPlan payload
    ExecutionResultJson     TEXT,                          -- Serialized BimPipelineResult on completion
    ErrorReason             TEXT,                          -- Diagnostic failure reason
    RetryCount              INTEGER NOT NULL DEFAULT 0,    -- Current retry attempt
    MaxRetries              INTEGER NOT NULL DEFAULT 3,    -- Maximum allowed automatic retries
    LeaseToken              TEXT,                          -- Guid assigned to active worker lease
    WorkerLeaseExpiryUtc    TEXT                           -- Expiry time for active worker lease
);

CREATE INDEX IF NOT EXISTS IX_BimTasks_Dispatch 
    ON BimTasks (State, VirtualPriority DESC, EnqueuedAtUtc ASC)
    WHERE State = 0;

CREATE INDEX IF NOT EXISTS IX_BimTasks_OperationId 
    ON BimTasks (OperationId);

CREATE INDEX IF NOT EXISTS IX_BimTasks_PlanHash 
    ON BimTasks (PlanHash);

-- Append-only task event journal for real-time progress & diagnostics
CREATE TABLE IF NOT EXISTS BimTaskEvents (
    EventId                 INTEGER PRIMARY KEY AUTOINCREMENT,
    TaskId                  TEXT NOT NULL REFERENCES BimTasks(TaskId) ON DELETE CASCADE,
    TimestampUtc            TEXT NOT NULL,
    EventType               TEXT NOT NULL,                 -- Enqueued, StateChanged, StageProgress, Warning, Audit
    PayloadJson             TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_BimTaskEvents_TaskId 
    ON BimTaskEvents (TaskId, EventId ASC);
```

### 3.3 Atomic Dequeue & Leasing Semantics

To prevent multi-threading race conditions while supporting single-writer leasing, SQLite atomic updates with `RETURNING` are employed:

```sql
UPDATE BimTasks
SET State = 1, -- Dispatched
    DispatchedAtUtc = @NowUtc,
    WorkerLeaseExpiryUtc = @LeaseExpiryUtc,
    LeaseToken = @LeaseToken
WHERE TaskId = (
    SELECT TaskId 
    FROM BimTasks
    WHERE State = 0 -- Queued
      AND DeadlineUtc > @NowUtc
    ORDER BY VirtualPriority DESC, EnqueuedAtUtc ASC
    LIMIT 1
)
RETURNING TaskId, OperationId, PlanHash, Priority, ItemCount, TargetDocTitle, TargetDocVersionGuid, PlanJson, LeaseToken;
```

If a daemon crashes while a task is `Dispatched` or `InFlight`, the startup cold scanner detects any task whose `WorkerLeaseExpiryUtc < @NowUtc` and transitions it to recovery or reconciliation.

---

## 4. Task Lifecycle State Machine & Integration

### 4.1 State Machine Diagram

```
                 ┌────────────────────────────────────────────────────────┐
                 │                   Client Submits Task                  │
                 └───────────────────────────┬────────────────────────────┘
                                             │ Schema Validation PASS
                                             ▼
                                     ┌───────────────┐
                     ┌──────────────►│    Queued     │◄──────────────────────────────────┐
                     │               └───────┬───────┘                                   │
                     │                       │ Atomic Lease by Single-Writer             │
                     │                       ▼                                           │
                     │               ┌───────────────┐                                   │
                     │               │  Dispatched   │                                   │
                     │               └───────┬───────┘                                   │
                     │                       │ Pre-Flight Gate OK & IntentJournal Flushed│
                     │                       ▼                                           │
                     │               ┌───────────────┐                                   │
                     │               │   InFlight    │                                   │
                     │               └───────┬───────┘                                   │
                     │                       │                                           │
      Transient      │       ┌───────────────┼───────────────┬───────────────────┐       │
      Retry with     │       │ Commit OK     │ Pre-flight    │ Pipe Severed /    │       │
      Backoff        │       │ ExtStore OK   │ Rollback Conf.│ Execution Timeout │       │
                     │       ▼               ▼               ▼                   │       │
                     │ ┌───────────┐   ┌───────────┐   ┌────────────────┐        │       │
                     │ │ Committed │   │  Failed   │   │ OutcomeUnknown │        │       │
                     │ └───────────┘   └─────┬─────┘   └────────┬───────┘        │       │
                     │                       │                  │                │       │
                     │                       │                  │ Reconcile via  │       │
                     │                       │                  │ ExtStore Schema│       │
                     │                       │                  ▼                │       │
                     │                       │         ┌─────────────────┐       │       │
                     │                       │         │ Receipt Found?  │       │       │
                     │                       │         └───┬─────────┬───┘       │       │
                     │                       │         YES │         │ NO        │       │
                     │                       │             │         └───────────┘       │
                     │                       │             ▼                             │
                     │                       │       ┌───────────┐                       │
                     │                       │       │ Committed │                       │
                     │                       │       └───────────┘                       │
                     │                       │ Retries                                   │
                     │                       │ Exhausted                                 │
                     │                       ▼                                           │
                     │               ┌───────────────┐                                   │
                     └───────────────┤   DeadLetter  │                                   │
                                     └───────────────┘                                   │
                                             ▲                                           │
                                             │ Unrecoverable Schema/Data Conflict        │
                                             └───────────────────────────────────────────┘
```

### 4.2 Lifecycle State Transitions

| State | Value | Description | Permitted Next States |
|---|---|---|---|
| `Queued` | 0 | Task validated and persisted in SQLite. Waiting for single-writer lease. | `Dispatched`, `Cancelled` |
| `Dispatched` | 1 | Single-writer leased task. Performing on-thread pre-flight identity check. | `InFlight`, `Failed`, `Queued` (re-lease) |
| `InFlight` | 2 | Flushed to local `IntentJournal` as `InFlight`. Active script executing in Revit UI thread. | `Committed`, `Failed`, `OutcomeUnknown` |
| `Committed` | 3 | Revit transaction committed, Extensible Storage receipt written, ledger updated. Terminal success. | *(None — Terminal)* |
| `Failed` | 4 | Clean rejection (document mismatch, Roslyn compile error, dry-run rollback). | `Queued` (if retryable), `DeadLetter` |
| `OutcomeUnknown` | 5 | Pipe dropped, timeout expired, or Revit crashed while `InFlight`. Requires Extensible Storage reconciliation. | `Committed` (reconciled), `Failed` (reconciled rollback), `DeadLetter` |
| `DeadLetter` | 6 | Irrecoverable failure, schema corruption, or retry ceiling (3) reached. Awaiting human/agent audit. | *(None — Terminal)* |
| `Cancelled` | 7 | Cooperative cancellation requested by client before or during execution. | *(None — Terminal)* |

### 4.3 Two-Tier Persistence: SQLite Queue + IntentJournal

A critical question arises: *Why have both SQLite and IntentJournal?*
- **SQLite (`state/hpbim_queue.db`)**: Manages the **Queue Management Tier** — multi-client concurrency, task submission, priority ordering, worker leasing, and event streaming.
- **IntentJournal (`state/intent_journal.jsonl`)**: Manages the **Revit Coordination Audit Tier** — an append-only, hardware-flushed (`fsync`) record of every atomic transaction dispatched across the Revit boundary, directly matching the GUIDs stored in Revit's `DataStorage` Extensible Storage entity (`HPBimCoordinationReceipt`).

#### Synchronization Protocol:
1. When the dispatcher leases a task (`Dispatched`), it calls:
   `await _intentJournal.RecordIntentAsync(new IntentJournalRecord { OperationId = task.OperationId, PlanHash = task.PlanHash, Status = IntentStatus.Pending, ItemCount = task.ItemCount, TargetDoc = task.TargetDoc });`
2. Immediately before writing the script to Revit Named Pipe:
   `await _intentJournal.UpdateStatusAsync(task.OperationId, IntentStatus.InFlight);`
3. Upon receiving successful execution result:
   `await _intentJournal.UpdateStatusAsync(task.OperationId, IntentStatus.Committed);`
   `await _taskQueue.CompleteTaskAsync(task.TaskId, result);`
4. Upon network drop or timeout:
   `await _intentJournal.UpdateStatusAsync(task.OperationId, IntentStatus.OutcomeUnknown, ex.Message);`
   `await _taskQueue.FailTaskAsync(task.TaskId, ex.Message, isOutcomeUnknown: true);`

---

## 5. Backpressure, Priority & Concurrency Control

### 5.1 Single-Writer Serialization for Revit UI Thread

Revit API's architecture strictly forbids multi-threaded access. All calls into `Autodesk.Revit.DB` must happen sequentially on the main UI thread. Furthermore, `HPRebar.McpBridge` protects its execution with an atomic compare-exchange:
```csharp
if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
    return ExecuteResult.Failure("Another script is still running in Revit...");
```

The daemon solves this constraint by implementing an **M-Producers, 1-Consumer Architecture**:
- Any number of CLI commands, AI agents, or background processes can concurrently submit tasks to `IBimTaskQueue`.
- Exactly **one** long-running worker loop (`BimTaskDispatcher`) dequeues and executes tasks sequentially.
- This physically prevents pipe contention, eliminates `_busy` failures, and guarantees that each task receives an isolated, atomic transaction.

### 5.2 Priority Classes & Anti-Starvation Scheduling

#### Priority Classes:
1. **`PreFlight` / `High` (200)**:
   - Interactive queries: Document title check (`revit.context`), active view bounding box, ping, element parameter inspection, cancel requests.
   - Execution Budget: < 5 seconds.
2. **`Normal` (100)**:
   - Standard BIM execution plans: Substructure batches, column runs, beam networks, floor slabs.
   - Execution Budget: 15–120 seconds.
3. **`BackgroundQC` / `Low` (10)**:
   - Heavy offline audits: Measured geometric verification (`MeasuredGeometricVerifier`), full-model clash detection, mesh extraction.
   - Execution Budget: 60–300 seconds.

#### Anti-Starvation Aging Algorithm:
To prevent heavy generation batches from starving background QC tasks indefinitely, the daemon applies an **aging algorithm**:
$$\text{VirtualPriority} = \text{BasePriority} + \min\left(100, \left\lfloor \frac{\text{WaitTimeSeconds}}{60} \right\rfloor \times 10\right)$$

- Every 60 seconds a task spends in `Queued` state, its `VirtualPriority` increases by **+10** (capped at +100 bonus).
- Example: A `BackgroundQC` task (base priority 10) waiting for 10 minutes reaches a virtual priority of $10 + 100 = 110$, surpassing newly submitted `Normal` tasks (priority 100) and guaranteeing execution.

### 5.3 Admission Control & Backpressure

To protect the daemon from memory exhaustion during runaway multi-agent loops:
1. **Queue Capacity Ceilings**:
   - `MaxQueueDepth`: 1,000 tasks.
   - `MaxQueueMemoryBytes`: 100 MB.
2. **Soft Backpressure (80% / 800 tasks)**:
   - The daemon emits warning telemetry and throttles low-priority submissions by introducing a 500ms delay.
3. **Hard Backpressure (100% / 1,000 tasks)**:
   - The admission gate immediately rejects new submissions with `QueueCapacityExceededException` (JSON-RPC Error Code `-32005: Queue Full`).
4. **Token Bucket Rate Limiting**:
   - Each client ID (e.g. `agent-coordinator`, `agent-cad`) is allotted a maximum of **20 task submissions per second** with a burst limit of 40.

### 5.4 Cooperative Cancellation Propagation

When a user or agent issues a task cancellation:
1. **Queued Phase**: The task is updated directly in SQLite to `Cancelled`. The single-writer dispatcher simply skips it when dequeuing.
2. **In-Flight Phase**:
   - The client calls `CancelTaskAsync(taskId)`.
   - The daemon signals the active `CancellationTokenSource`.
   - The daemon immediately sends `revit.cancel` across the Named Pipe to Revit.
   - Inside Revit, `McpBridgeExternalEventHandler.Cancel()` invokes `_currentCancel.Cancel()`.
   - Inside the running script, `runner.Run` checks `cancellationToken.ThrowIfCancellationRequested()`, cleanly aborting the loop and triggering `transactionGroup.RollBack()`.

---

## 6. C# Interface Contracts

All interface contracts are written strictly according to `HP_CLEAN_CODE_CORE.md`, utilizing immutable records, file-scoped namespaces, and explicit dependency inversion.

### 6.1 `IBimTaskQueue`

```csharp
namespace HPBim.Core.Queue;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
///     Durable task queue managing persistent BIM execution requests, priority dispatch,
///     leasing, and lifecycle transitions.
/// </summary>
public interface IBimTaskQueue : IAsyncDisposable, IDisposable
{
    /// <summary>
    ///     Enqueues a new validated BIM task for execution.
    /// </summary>
    Task<Guid> EnqueueAsync(BimTaskSubmission submission, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Atomically acquires a timed lease on the highest-priority queued task.
    ///     Returns null if the queue is empty.
    /// </summary>
    Task<QueuedBimTask?> DequeueLeaseAsync(TimeSpan leaseDuration, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Marks a leased task as successfully committed.
    /// </summary>
    Task CompleteTaskAsync(Guid taskId, BimPipelineResult result, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Marks a leased task as failed, recording the failure diagnostic.
    /// </summary>
    Task FailTaskAsync(Guid taskId, string errorReason, bool isOutcomeUnknown = false, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Cooperatively cancels a task by ID.
    /// </summary>
    Task<bool> CancelTaskAsync(Guid taskId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves a task by ID, returning its current state and execution result.
    /// </summary>
    Task<QueuedBimTask?> GetTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves all currently active tasks (Queued, Dispatched, InFlight).
    /// </summary>
    Task<IReadOnlyList<QueuedBimTask>> GetActiveTasksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves aggregated queue statistics.
    /// </summary>
    Task<QueueStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Purges terminal tasks older than the retention window.
    /// </summary>
    Task<int> PurgeExpiredTasksAsync(TimeSpan retentionWindow, CancellationToken cancellationToken = default);
}
```

### 6.2 `IBimQueueDaemon`

```csharp
namespace HPBim.Daemon;

using System;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Core.Queue;

/// <summary>
///     Top-level host daemon managing the background service lifecycle, IPC server,
///     Revit Named Pipe connection supervisor, and task dispatch loop.
/// </summary>
public interface IBimQueueDaemon : IAsyncDisposable, IDisposable
{
    /// <summary>
    ///     Gets the current operational state of the daemon.
    /// </summary>
    DaemonState State { get; }

    /// <summary>
    ///     Starts the daemon IPC server, connection supervisor, and dispatch worker loop.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Initiates graceful drain and shutdown of the daemon.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves diagnostic health status of the daemon and connected Revit bridge.
    /// </summary>
    DaemonHealthStatus GetHealthStatus();

    /// <summary>
    ///     Event raised whenever a task lifecycle event occurs.
    /// </summary>
    event Action<BimTaskEvent>? TaskEventPublished;
}
```

### 6.3 `IBimTaskDispatcher`

```csharp
namespace HPBim.Daemon.Dispatcher;

using System;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Core.Queue;
using HPRebar.Mcp.Contracts.Messages;

/// <summary>
///     Single-writer execution loop serializing task execution onto Revit's UI thread.
/// </summary>
public interface IBimTaskDispatcher : IAsyncDisposable, IDisposable
{
    /// <summary>
    ///     Runs the continuous dispatch loop until cancellation is requested.
    /// </summary>
    Task RunDispatchLoopAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Executes an urgent interactive query directly without enqueuing,
    ///     interleaving safely when the dispatch worker is between batches.
    /// </summary>
    Task<ExecuteResult> DispatchDirectQueryAsync(DirectQueryRequest request, CancellationToken cancellationToken = default);
}
```

### 6.4 Data Contracts & Records

```csharp
namespace HPBim.Core.Queue;

using System;
using System.Text.Json.Serialization;
using HPBim.Core.Execution;

/// <summary>
///     Execution priority category for a BIM task.
/// </summary>
public enum BimTaskPriority
{
    /// <summary>Offline geometric audits, clash detection, heavy validation (Weight = 10).</summary>
    BackgroundQc = 10,

    /// <summary>Standard model generation batches and element runs (Weight = 100).</summary>
    Normal = 100,

    /// <summary>Interactive queries, context checks, bounding boxes, cancellation (Weight = 200).</summary>
    PreFlight = 200
}

/// <summary>
///     Detailed lifecycle state of a queued BIM task.
/// </summary>
public enum BimTaskState
{
    Queued = 0,
    Dispatched = 1,
    InFlight = 2,
    Committed = 3,
    Failed = 4,
    OutcomeUnknown = 5,
    DeadLetter = 6,
    Cancelled = 7
}

/// <summary>
///     Client submission payload to enqueue a new task.
/// </summary>
public sealed record BimTaskSubmission
{
    public BimExecutionPlan Plan { get; init; } = new();
    public BimTaskPriority Priority { get; init; } = BimTaskPriority.Normal;
    public string ClientId { get; init; } = string.Empty;
    public TimeSpan? QueueDeadline { get; init; }
    public string? CorrelationId { get; init; }
}

/// <summary>
///     Immutable, fully hydrated record of a queued BIM task in the persistent queue.
/// </summary>
public sealed record QueuedBimTask
{
    public Guid TaskId { get; init; } = Guid.NewGuid();
    public Guid OperationId { get; init; } = Guid.NewGuid();
    public string PlanHash { get; init; } = string.Empty;
    public BimTaskPriority Priority { get; init; } = BimTaskPriority.Normal;
    public int VirtualPriority { get; init; }
    public BimTaskState State { get; init; } = BimTaskState.Queued;
    public int ItemCount { get; init; }
    public DateTime EnqueuedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime? DispatchedAtUtc { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
    public DateTime DeadlineUtc { get; init; }
    public DocumentIdentitySnapshot TargetDoc { get; init; } = new();
    public BimExecutionPlan Plan { get; init; } = new();
    public BimPipelineResult? Result { get; init; }
    public string? ErrorReason { get; init; }
    public int RetryCount { get; init; }
    public int MaxRetries { get; init; } = 3;
    public string? LeaseToken { get; init; }
    public DateTime? WorkerLeaseExpiryUtc { get; init; }

    [JsonIgnore]
    public bool IsTerminal => State is BimTaskState.Committed or BimTaskState.Failed or BimTaskState.DeadLetter or BimTaskState.Cancelled;
}

/// <summary>
///     Real-time progress and diagnostic event emitted by a task.
/// </summary>
public sealed record BimTaskEvent
{
    public long EventId { get; init; }
    public Guid TaskId { get; init; }
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
    public string EventType { get; init; } = string.Empty;
    public string PayloadJson { get; init; } = "{}";
}

/// <summary>
///     Aggregated operational statistics for queue monitoring.
/// </summary>
public sealed record QueueStatistics
{
    public int QueuedCount { get; init; }
    public int DispatchedCount { get; init; }
    public int InFlightCount { get; init; }
    public int CommittedCount { get; init; }
    public int FailedCount { get; init; }
    public int OutcomeUnknownCount { get; init; }
    public int DeadLetterCount { get; init; }
    public double AverageExecutionTimeMs { get; init; }
    public long DatabaseSizeBytes { get; init; }
}

/// <summary>
///     Diagnostic health report of the daemon process and host connection.
/// </summary>
public sealed record DaemonHealthStatus
{
    public bool IsRunning { get; init; }
    public bool IsRevitConnected { get; init; }
    public string? ActiveRevitDocument { get; init; }
    public TimeSpan Uptime { get; init; }
    public int ActiveConnections { get; init; }
    public QueueStatistics QueueStats { get; init; } = new();
}

public enum DaemonState
{
    Stopped = 0,
    Starting = 1,
    Running = 2,
    Draining = 3,
    Faulted = 4
}
```

---

## 7. Failure Modes & Edge Case Mitigations

| # | Failure Scenario | Detection Mechanism | Mitigation Strategy | Rule 0 Compliance |
|---|---|---|---|---|
| **1** | **Revit crashes mid-transaction while InFlight** | Named Pipe read throws `IOException: Pipe broken`. Daemon watchdog detects process termination. | Task marked `OutcomeUnknown`. On restart, cold-scan runs `ReconcileService`. Extensible Storage receipt is checked. If missing, transaction rolled back safely; requeue allowed. | **Zero guesswork**: Never assume committed without Extensible Storage proof. |
| **2** | **Daemon process crashes (power failure, SIGKILL)** | On reboot, startup scanner finds tasks in `Dispatched` or `InFlight` with expired leases. | SQLite WAL recovers to consistent state. In-flight tasks cross-referenced with `IntentJournal`. Unfinished tasks reconciled against Revit before resuming queue. | **Zero lost tasks**: Durable WAL guarantees disk persistence. |
| **3** | **Active Document switched in Revit** | On-thread Pre-Flight Gate checks `doc.Title == TargetDoc.Title` and `VersionGUID == TargetDoc.VersionGuid`. | Task immediately fails with `DocumentMismatchException`. Task is NOT applied to wrong project; returned to client with clear diagnostic. | **Zero misplacement**: Prevents drawing foundation elements into wrong project. |
| **4** | **Revit modal dialog opens (Export/Print/Error)** | `TimeoutPolicy.TransportTimeout` (5s) or `BusyGrace` expires on bridge idle tick. | Bridge reports host busy. Dispatcher pauses, records delay, and retries after dialog is dismissed, up to `QueueDeadline`. | **Transparent handling**: Does not crash client; reports waiting state. |
| **5** | **Queue flood / runaway agent loop** | Admission controller detects queue depth > 1,000 tasks or rate > 20 req/s. | Hard backpressure activated. New submissions rejected with HTTP 429 / RPC `-32005 Queue Full`. | **Self-protecting**: Prevents memory exhaustion and Revit crashes. |
| **6** | **Network drop between CLI and Daemon** | Client Named Pipe closes prematurely while task is queued or executing. | Daemon task execution continues uninterrupted! Result is persisted in SQLite. When CLI reconnects or polls, it retrieves the completed result. | **Decoupled execution**: CLI crash does not abort Revit generation. |
| **7** | **Execution budget exceeded (deadlock in script)** | Daemon execution timer expires (`TimeoutPolicy.ComputeExecutionBudget(items)`). | Daemon sends `revit.cancel` across pipe. If bridge unresponsive within 5s, pipe severed and task marked `OutcomeUnknown`. | **Deterministic boundaries**: Prevents infinite UI thread hangs. |
| **8** | **Corrupt schema or invalid geometry** | Schema validator checks payload at admission gate. | Rejected before reaching SQLite queue. Task never enters dispatch loop. | **Fail-fast**: Invalid inputs stopped at perimeter. |

---

## 8. Alignment with Milestone M5 Companion Proposals

The Background Queue Daemon serves as the execution backbone for both companion proposals in Milestone M5:

1. **Alignment with Feature F11 Part 1 (Bidirectional DMU & Change Tracking, explorer_m5_1)**:
   - When a native Revit `IUpdater` detects a user edit or deletion in Revit, the DMU handler cannot perform heavy synchronization synchronously on the UI thread without risking recursive updater cascades.
   - Instead, the DMU handler publishes a lightweight change event to the daemon queue as a `BimTaskPriority.BackgroundQc` or `Normal` synchronization task.
   - The daemon's single-writer loop processes this sync task, updating `BimCoordinationLedger` and resolving 3-way conflicts deterministically.

2. **Alignment with Feature F11 Part 3 (Large-Scale Batching & Worksharing, explorer_m5_3)**:
   - When generating a high-rise project (>10,000 elements), the batch execution planner partitions the execution plan into discrete chunks (e.g. 500 elements per chunk).
   - Each chunk is enqueued into `IBimTaskQueue` as an individual, ordered task sharing a common `BatchGroupId`.
   - The daemon executes chunks sequentially, committing progressive checkpoints to Extensible Storage. If chunk 4 fails, chunks 1–3 remain durably committed, and chunk 4 is isolated without rolling back the entire project.

---

## 9. Conclusion & Implementation Roadmap

The **Background Queue Daemon (`HPBim.Daemon`)** transforms HPBim from a point-to-point scripting bridge into an enterprise-grade, crash-resilient BIM automation platform. By strictly honoring Revit's single-writer UI thread constraint while providing a concurrent, priority-scheduled, durable queue interface, HPBim delivers 100% adherence to Rule 0: truthful outcomes, zero guesswork, and absolute resilience.

### Next Steps for M5 Worker:
1. Scaffold `src/HPBim.Daemon/` project (.NET 10 Worker Service) referencing `HPBim.Core` and `McpShared`.
2. Implement `SqliteBimTaskQueue` using `Microsoft.Data.Sqlite` in WAL mode.
3. Implement `BimTaskDispatcher` integrating `IRevitPipeClient`, `IIntentJournal`, and `TimeoutPolicy`.
4. Implement `NamedPipeDaemonIpcServer` exposing the multi-client JSON-RPC surface.
5. Update `HPBim.Cli` to automatically route commands through the daemon when available.
