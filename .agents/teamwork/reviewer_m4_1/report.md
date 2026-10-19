# Technical Review & Adversarial Analysis Report — Milestone M4: Recovery & Reconciliation

- **Reviewer**: `reviewer_m4_1` (teamwork_preview_reviewer)
- **Role**: Correctness, Interface & Adversarial Reviewer (M4)
- **Date**: 2026-10-08T04:42:00Z
- **Target Deliverable**: `HPBim` (`HPBim.slnx`)
- **Verdict**: **APPROVE**

---

## 1. Executive Summary & Verdict

Milestone M4 establishes crash resilience, write-ahead durability, and idempotent recovery for the CAD-to-BIM coordination pipeline across the process boundary between `HPBim.Client` and Autodesk Revit 2026 (`HPRebar.McpBridge`).

All five packages requested under Milestone M4 have been verified:
1. **Feature F9 Part 1 (Local Intent Journal)**: Write-ahead logging in append-only JSONL format with immediate hardware flush (`stream.Flush(flushToDisk: true)`), concurrency protection (`SemaphoreSlim(1,1)`), corrupted line tolerance, and log compaction preserving incomplete intents.
2. **Feature F9 Part 2 (Revit Extensible Storage Receipt)**: Atomic persistence of execution receipts on hidden `DataStorage` elements in Revit under Schema GUID `D4E5F6A7-8B9C-0D1E-2F3A-4B5C6D7E8F90` within the main creation transaction. Clean dry-run bypass with 100% rollback confirmation. Read-only receipt querying under `TransactionModes.None`.
3. **Feature F10 (3-Tier Timeout Policy & ReconcileService)**: Clear separation of Transport (5s), Queue (30s), and Dynamic Execution Budget ($15\text{s} + N \times 50\text{ms}$, clamped to $15\text{s}\dots 300\text{s}$). Graceful classification of severed pipe transactions as `OutcomeUnknown`, followed by automatic reconciliation restoring `CompositeSourceKey -> UniqueId` ledger mappings.
4. **Clean Code & Architectural Governance**: All 24 created/modified files strictly obey Rule C2 ($\le 300$ lines), Rule M3 ($\le 50$ lines/method), zero unhandled/empty catches, and zero forbidden cross-dependencies.
5. **Independent Test Execution**: Solution builds cleanly with 0 errors and 0 warnings. 100% of all test suites pass (210 in `HPBim.Client.Tests`, 381 in `HPBim.Core.Tests`, 42 in `HPBim.E2E.Tests`, 793 in `HPRebar.Mcp.Server.Core.Tests` — total 1,426 tests).

**Final Verdict**: **APPROVE** — Work product is verified authentic, robust, and compliant.

---

## 2. Integrity & Authenticity Audit

As required by reviewer protocol, the codebase was inspected for integrity violations:
- **Hardcoded test results / expected outputs**: None found. All test fixtures construct real objects, invoke actual logic, and assert genuine properties.
- **Dummy or facade implementations**: None found. `IntentJournal` writes real JSONL lines with physical disk flushing; `ReconcileService` queries real receipt objects and updates the ledger; `TimeoutPolicy` computes dynamic budgets using mathematical formulas; `ExtensibleStorageReceiptReader` parses JSON payloads and handles corruption.
- **Shortcuts or task bypass**: None found. Full 5-package architecture implemented as planned.
- **Fabricated verification outputs or logs**: None detected. All test runner outputs and compilation results were independently reproduced and confirmed.
- **Self-certifying work**: Independently inspected each source file and verified behavioral invariants.

---

## 3. Detailed Component Review

### 3.1 Feature F9 Part 1: Local Intent Journal (`Connection/`)

- **Files**:
  - `HPBim/src/HPBim.Client/Connection/IntentStatus.cs` (23 lines)
  - `HPBim/src/HPBim.Client/Connection/IntentJournalRecord.cs` (43 lines)
  - `HPBim/src/HPBim.Client/Connection/IIntentJournal.cs` (48 lines)
  - `HPBim/src/HPBim.Client/Connection/IntentJournal.cs` (241 lines)
- **Correctness & Resilience**:
  - `IntentStatus` defines 5 explicit lifecycle states: `Pending`, `InFlight`, `Committed`, `Failed`, `OutcomeUnknown`.
  - `IntentJournalRecord` captures `OperationId`, `PlanHash`, `TimestampUtc`, `Status`, `ItemCount`, `TargetDoc`, and `ErrorReason`. Properties `IsTerminal` and `IsIncomplete` provide clear predicates.
  - In `IntentJournal.cs`:
    - Hardware flush: Lines 153–156 explicitly call `stream.Flush(flushToDisk: true)` guaranteeing that data is physically committed to disk media before IPC dispatch.
    - Thread-safety: Concurrency is protected via `SemaphoreSlim _ioLock = new(1, 1)` around all file I/O operations (lines 97, 144, 166). In-memory cache `ConcurrentDictionary<Guid, IntentJournalRecord>` provides lock-free fast path for single record lookups.
    - Corrupted line tolerance: Line 194 catches `JsonException` with explicit rationale comment `// Corrupted or truncated journal line from abrupt process termination; skip gracefully to preserve undamaged records.`, preventing crash recovery loops.
    - Log compaction: `PruneOldEntriesAsync` (lines 94–120) strictly filters `toKeep = latestMap.Values.Where(r => r.IsIncomplete || r.TimestampUtc > cutoff)`, guaranteeing that uncompleted operations are never purged, regardless of age.

### 3.2 Feature F9 Part 2: Revit Extensible Storage Receipt (`Recovery/` & `Pipeline/`)

- **Files**:
  - `HPBim/src/HPBim.Client/Recovery/CoordinationReceiptRecord.cs` (16 lines)
  - `HPBim/src/HPBim.Client/Recovery/IExtensibleStorageReceiptReader.cs` (29 lines)
  - `HPBim/src/HPBim.Client/Recovery/ExtensibleStorageQueryScriptTemplate.cs` (63 lines)
  - `HPBim/src/HPBim.Client/Recovery/ExtensibleStorageReceiptReader.cs` (160 lines)
  - `HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.Receipt.cs` (84 lines)
- **Correctness & Atomic Attachment**:
  - Pinned Schema GUID: `D4E5F6A7-8B9C-0D1E-2F3A-4B5C6D7E8F90`. Name: `HPBimCoordinationReceipt`. Fields: `OperationId` (`Guid`), `PlanHash` (`string`), `CommittedAtUtc` (`string`), `ElementMapJson` (`string`).
  - Access Level: Public read and write (`builder.SetReadAccessLevel(AccessLevel.Public)`), allowing safe inspection across addins and scripts.
  - In `BimPlanRunnerScriptTemplate.Receipt.cs`:
    - Receipt creation runs within the outer model transaction (`TransactionModes.Auto`), ensuring atomic commit with model elements.
    - If `dryRun == true`, line 20 (`if (!dryRun && !string.IsNullOrEmpty(planHash))`) skips receipt creation entirely, ensuring zero residual artifacts when dryRun transaction rolls back.
    - Attaches to non-graphical `DataStorage` element, avoiding interference with 3D model geometry or views.
  - In `ExtensibleStorageReceiptReader.cs`:
    - Queries run under `TransactionModes.None` (line 90), executing purely read-only queries that do not lock or alter the document.
    - Corrupt or malformed `ElementMapJson` is parsed defensively via `ParseElementMap` (lines 141–158), returning empty dictionaries rather than throwing.

### 3.3 Feature F10: 3-Tier Timeout & Auto-Reconciliation (`Connection/` & `Recovery/`)

- **Files**:
  - `HPBim/src/HPBim.Client/Connection/TimeoutPolicy.cs` (48 lines)
  - `HPBim/src/HPBim.Client/Recovery/ReconcileStatus.cs` (20 lines)
  - `HPBim/src/HPBim.Client/Recovery/ReconcileResult.cs` (57 lines)
  - `HPBim/src/HPBim.Client/Recovery/IReconcileService.cs` (18 lines)
  - `HPBim/src/HPBim.Client/Recovery/ReconcileService.cs` (107 lines)
  - `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.IntentJournal.cs` (156 lines)
- **Correctness & Reconciliation**:
  - `TimeoutPolicy` separates:
    - Tier 1: Transport connection timeout (5s)
    - Tier 2: Queue deadline (30s)
    - Tier 3: Dynamic execution budget ($15\text{s} + N \times 50\text{ms}$, clamped between 15s and 300s; default 60s for non-positive count)
    - Total timeout = QueueDeadline + ExecutionBudget.
  - In `BimPipelineEngine.IntentJournal.cs`:
    - Pre-dispatch write-ahead logging (line 44): records `IntentStatus.InFlight` before pipe call.
    - Catches `RevitPipeException`, `IOException`, `TimeoutException`, `OperationCanceledException` (line 83), sets `IsOutcomeUnknown = true` and records `IntentStatus.OutcomeUnknown` using `CancellationToken.None` (line 153) to guarantee log persistence even when caller was cancelled.
  - In `ReconcileService.cs`:
    - Discovers all incomplete intents via `GetIncompleteIntentsAsync()`.
    - If receipt found in Revit: parses `ElementMap`, updates `BimCoordinationLedgerService` via `UpdateMapping(sourceKey, uniqueId, planHash, Created)`, transitions intent to `Committed`, and returns `ReconcileResult.Committed`.
    - If receipt absent in Revit: transitions intent to `Failed` ("Receipt absent in Revit") and returns `ReconcileResult.RolledBack`.
    - If pipe disconnects during reconciliation: preserves intent as `OutcomeUnknown`, avoiding false negative failures, adhering to Rule 0.

---

## 4. Adversarial Challenges & Stress Testing

### Challenge 1: Process Interruption / Crash During Intent Write
- **Attack Scenario**: Host OS process is killed while writing to `intent_journal.jsonl`, leaving a half-written or corrupted JSON line at EOF.
- **Blast Radius**: Subsequent startups could crash when deserializing the corrupted journal file, halting all recovery.
- **Mitigation & Verification**:
  - In `IntentJournal.cs:194`, `JsonException` is caught per line and skipped.
  - Verified by `IntentJournalTests.CorruptedJournalLines_GracefullySkipped_PreservesValidRecords`: synthetic file with truncated JSON and invalid non-JSON strings parses remaining valid records with 100% fidelity.

### Challenge 2: Client Timeout While Revit Transaction Commits
- **Attack Scenario**: Revit is busy; client hits timeout and drops named pipe connection while Revit finishes committing transaction in the background.
- **Blast Radius**: Client assumes failure, re-executes plan blindly, causing duplicate elements in Revit (e.g., duplicated columns/walls).
- **Mitigation & Verification**:
  - Client classifies timeout as `OutcomeUnknown` instead of `Failed`.
  - On restart/reconnect, `ReconcileService` queries `ExtensibleStorageReceiptReader`, discovers the committed receipt, and populates `BimCoordinationLedgerService` with `SourceKey -> UniqueId`.
  - Re-running the identical plan resolves all items as `NoOp` (or `Updated`), creating **0 duplicates**.
  - Verified by `AutoReconcileIdempotencyTests.EndToEnd_Timeout_AutoReconcile_RerunPlan_CreatesZeroDuplicates`: `CreatedCount == 0`, `NoOpCount == 4`.

### Challenge 3: Pipe Severance When Transaction Was Rolled Back
- **Attack Scenario**: Revit crashes or encounters unhandled API exception rolling back transaction; pipe is severed.
- **Blast Radius**: Client might wait indefinitely or falsely mark elements as committed.
- **Mitigation & Verification**:
  - `ReconcileService` checks Revit for receipt; finding none, it confirms transaction rolled back, marks intent as `Failed`, leaves ledger empty.
  - Verified by `AutoReconcileIdempotencyTests.EndToEnd_Disconnect_ReceiptAbsent_ReconcileRollsBack_AllowsCleanRerun`: allows clean subsequent execution from scratch without phantom mappings.

### Challenge 4: Dry-Run Dirtying Revit Extensible Storage
- **Attack Scenario**: A preview/dryRun execution writes an Extensible Storage receipt into the model before rollback, causing subsequent real runs or reconciles to see a phantom receipt.
- **Blast Radius**: Phantom receipts could cause false positive reconciliation and prevent actual elements from being created.
- **Mitigation & Verification**:
  - In `BimPlanRunnerScriptTemplate.Receipt.cs:20`, condition `if (!dryRun && !string.IsNullOrEmpty(planHash))` guarantees that receipt logic is completely omitted in dryRun mode.
  - In `BimPipelineEngine.IntentJournal.cs:104`, `plan.Options.DryRun` skips journal recording.
  - Verified by `AutoReconcileIdempotencyTests.DryRun_DoesNotRecordInJournal_AndRollsBackCleanly`.

---

## 5. Clean Code & Architectural Audit Findings

| Metric | Threshold | Observed Result | Status |
| :--- | :--- | :--- | :--- |
| Rule C2 (File Length) | $\le 300$ lines | Max file: `IntentJournal.cs` (241 lines), `AutoReconcileIdempotencyTests.cs` (241 lines) | **PASS** |
| Rule M3 (Method Length) | $\le 50$ lines | Max method: `ReconcileServiceTests` (42 lines), `FakeRevitPipeClient.ExecuteAsync` (41 lines) | **PASS** |
| Empty Catches | 0 unhandled / unexplained | All 4 catch blocks in M4 code have explicit comments explaining why the exception is safely swallowed/routed | **PASS** |
| Cross-MCP References | 0 allowed | `HPBim.Client` references only `HPBim.Core` and `McpShared/HPRebar.Mcp.Contracts` | **PASS** |
| Revit API in Core/Client | 0 in Core, 0 in Client compilation | No references to `Autodesk.Revit.*` in .csproj references; Roslyn scripts pass API calls as text strings | **PASS** |

---

## 6. Verified Claims

1. `dotnet build HPBim/HPBim.slnx` compiles cleanly with 0 errors and 0 warnings $\rightarrow$ **PASS**.
2. `HPBim.Client.Tests.exe` executes 210 tests with 0 failures $\rightarrow$ **PASS**.
3. `HPBim.Core.Tests.exe` executes 381 tests with 0 failures $\rightarrow$ **PASS**.
4. `HPBim.E2E.Tests.exe` executes 42 tests with 0 failures $\rightarrow$ **PASS**.
5. `HPRebar.Mcp.Server.Core.Tests.exe` executes 793 tests with 0 failures $\rightarrow$ **PASS**.
6. Total repository test count: 1,426 tests pass 100% $\rightarrow$ **PASS**.
7. All 24 M4 deliverables satisfy Rule C2 ($\le 300$) and Rule M3 ($\le 50$) $\rightarrow$ **PASS**.

---

## 7. Conclusion

Milestone M4 implementation satisfies all requirements set forth in `PROJECT.md`, `ORIGINAL_REQUEST.md`, and the dispatch instructions. The recovery and reconciliation architecture provides complete crash tolerance, write-ahead durability, and idempotency guarantees, rigorously adhering to the Golden Rule ("Thà để một cấu kiện chưa dựng còn hơn dựng sai nhưng báo đúng").
