# Dispatch Instructions — worker_m4_r1

## Identity & Context
- Agent: `worker_m4_r1`
- Archetype: `teamwork_preview_worker`
- Role: `Milestone M4 Implementation Worker`
- Working Directory: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_r1\`
- Authoritative User Request: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (MANDATORY: read this first).
- Project Scope Document: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`.

## Mandatory Explorer Reports to Read Before Writing Code
Read these 3 explorer reports thoroughly in your working directory context:
1. `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_1\report.md` (Intent Journal Architecture)
2. `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_2\report.md` (Extensible Storage Receipt Schema & Atomic Storage)
3. `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_3\report.md` (3-Tier Timeout & Auto-Reconciliation)

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Clean Code Governance Rules (Non-Negotiable)
- **Rule C2**: EVERY file in `HPBim/src/` and `HPBim/tests/` created or modified MUST be strictly <= 300 lines.
- **Rule M3**: EVERY method created or modified MUST be strictly <= 50 lines (aim for <= 35 lines).
- **Rule FM**: No empty catch blocks without an explicit reason comment.
- **Rule D**: No commented-out dead code.
- **Rule 0**: "Thà để một cấu kiện chưa dựng còn hơn dựng sai nhưng báo đúng."

## Exclusive Write Ownership
You have exclusive write ownership over the following files:
- `HPBim/src/HPBim.Client/Connection/IntentStatus.cs` (new)
- `HPBim/src/HPBim.Client/Connection/IntentJournalRecord.cs` (new)
- `HPBim/src/HPBim.Client/Connection/IIntentJournal.cs` (new)
- `HPBim/src/HPBim.Client/Connection/IntentJournal.cs` (new)
- `HPBim/src/HPBim.Client/Connection/TimeoutPolicy.cs` (new)
- `HPBim/src/HPBim.Client/Recovery/CoordinationReceiptRecord.cs` (new)
- `HPBim/src/HPBim.Client/Recovery/IExtensibleStorageReceiptReader.cs` (new)
- `HPBim/src/HPBim.Client/Recovery/ExtensibleStorageQueryScriptTemplate.cs` (new)
- `HPBim/src/HPBim.Client/Recovery/ExtensibleStorageReceiptReader.cs` (new)
- `HPBim/src/HPBim.Client/Recovery/ReconcileStatus.cs` (new)
- `HPBim/src/HPBim.Client/Recovery/ReconcileResult.cs` (new)
- `HPBim/src/HPBim.Client/Recovery/IReconcileService.cs` (new)
- `HPBim/src/HPBim.Client/Recovery/ReconcileService.cs` (new)
- `HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.cs` (modify to add receipt script call)
- `HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.Receipt.cs` (new partial class)
- `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.cs` (modify constructor to accept optional IIntentJournal)
- `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.IntentJournal.cs` (new partial class for journal execution wrapper)
- `HPBim/src/HPBim.Client/Pipeline/BimPipelineResult.cs` (add IsOutcomeUnknown and OperationId)
- `HPBim/tests/HPBim.Client.Tests/Fakes/FakeRevitPipeClient.cs` (support receipt simulation and disconnect/timeout triggers)
- `HPBim/tests/HPBim.Client.Tests/Connection/IntentJournalTests.cs` (new)
- `HPBim/tests/HPBim.Client.Tests/Connection/TimeoutPolicyTests.cs` (new)
- `HPBim/tests/HPBim.Client.Tests/Recovery/ExtensibleStorageReceiptReaderTests.cs` (new)
- `HPBim/tests/HPBim.Client.Tests/Recovery/ReconcileServiceTests.cs` (new)
- `HPBim/tests/HPBim.Client.Tests/Recovery/AutoReconcileIdempotencyTests.cs` (new)

## Key Deliverables & Implementation Details

### 1. Package 1: Local Intent Journal (`Connection/`)
- Implement `IntentStatus.cs` (`Pending`, `InFlight`, `Committed`, `Failed`, `OutcomeUnknown`).
- Implement `IntentJournalRecord.cs` (record with `OperationId`, `PlanHash`, `TimestampUtc`, `Status`, `ItemCount`, `TargetDoc`, `ErrorReason`).
- Implement `IIntentJournal.cs` & `IntentJournal.cs`:
  - Append-only JSONL format in `.hpbim_journal/intent_journal.jsonl` (or custom directory).
  - Use `FileStream` with `FileShare.ReadWrite` and call `stream.Flush(flushToDisk: true)` after every append to ensure durability.
  - Concurrency safety with `SemaphoreSlim(1, 1)`.
  - Methods: `RecordIntentAsync`, `UpdateStatusAsync`, `GetIncompleteIntentsAsync`, `PruneOldEntriesAsync`.

### 2. Package 2: 3-Tier Timeout Policy (`Connection/`)
- Implement `TimeoutPolicy.cs`:
  - `TransportTimeout` = 5 seconds.
  - `QueueDeadline` = 30 seconds.
  - `BaseExecutionBudget` = 15 seconds.
  - `PerItemBudget` = 50 milliseconds.
  - `MaxExecutionBudget` = 300 seconds.
  - `CalculateExecutionBudget(int itemCount)` $\rightarrow$ clamped between Base and Max.
  - `CalculateTotalTimeout(int itemCount)` $\rightarrow$ `QueueDeadline + CalculateExecutionBudget(itemCount)`.

### 3. Package 3: Revit Extensible Storage Receipt (`Recovery/` & `Pipeline/`)
- Schema details:
  - GUID: `D4E5F6A7-8B9C-0D1E-2F3A-4B5C6D7E8F90`
  - Name: `HPBimCoordinationReceipt`
  - AccessLevel: `Public`
  - Host: Hidden `DataStorage` element (`DataStorage.Create(doc)`)
  - Fields: `OperationId` (Guid), `PlanHash` (string), `CommittedAtUtc` (string), `ElementMapJson` (string).
- In `BimPlanRunnerScriptTemplate.Receipt.cs`:
  - Provide `GetReceiptScript()` injected in `GetScriptCode()` right before `GetEpilogueScript()`.
  - Only executes if `!dryRun`. Creates or updates the schema and entity on a new `DataStorage` element within the SAME transaction.
- Implement `CoordinationReceiptRecord.cs`, `IExtensibleStorageReceiptReader.cs`, `ExtensibleStorageQueryScriptTemplate.cs`, and `ExtensibleStorageReceiptReader.cs`:
  - Queries `DataStorage` elements for matching `OperationId` or `PlanHash` using `TransactionModes.None`.

### 4. Package 4: Auto-Reconciliation & Pipeline Integration (`Recovery/` & `Pipeline/`)
- Implement `ReconcileStatus.cs` & `ReconcileResult.cs`.
- Implement `IReconcileService.cs` & `ReconcileService.cs`:
  - Query incomplete intents from `IntentJournal`.
  - For each intent, query Extensible Storage via `IExtensibleStorageReceiptReader`.
  - If receipt found: populate `BimCoordinationLedger` (`SourceKey -> UniqueId`), mark intent `Committed`, return `ReconcileResult.Committed`.
  - If receipt not found: mark intent `RolledBack` / `Failed`, return `ReconcileResult.RolledBack`.
  - If pipe query fails / cannot connect: return `ReconcileResult.StillUnknown`.
- In `BimPipelineEngine.IntentJournal.cs`:
  - Add optional `IIntentJournal` and `TimeoutPolicy` integration to `ExecutePlanAsync`.
  - Write `InFlight` intent before sending request to pipe.
  - On success: write `Committed` intent.
  - On clean rollback/failure: write `Failed` intent.
  - On timeout or `IOException`: mark `OutcomeUnknown` and set `result.IsOutcomeUnknown = true`.
- Update `BimPipelineResult.cs`: add `IsOutcomeUnknown` and `OperationId`.

### 5. Package 5: Comprehensive Tests & Verification
- Update `FakeRevitPipeClient.cs`: support receipt query handling, timeout simulation, disconnect simulation.
- `IntentJournalTests.cs`: test append, update, reload, concurrent appends, corrupt line handling.
- `TimeoutPolicyTests.cs`: test scaling formulas, boundary clamps, defaults.
- `ExtensibleStorageReceiptReaderTests.cs`: test query script generation, parsing receipt JSON, empty results.
- `ReconcileServiceTests.cs`: test all 3 branches (Committed, RolledBack, StillUnknown).
- `AutoReconcileIdempotencyTests.cs`: simulate plan execution $\rightarrow$ timeout $\rightarrow$ auto-reconcile $\rightarrow$ rerun plan $\rightarrow$ 0 duplicate elements created (`Created: 0, NoOp: N`).

## Verification & Clean Code Attestation
1. Run `dotnet build HPBim/HPBim.slnx` -> 0 errors, 0 warnings.
2. Run `dotnet test HPBim/HPBim.slnx` -> 100% tests pass (all 568 existing + all new tests).
3. Run `dotnet test McpShared/McpShared.slnx` -> 100% tests pass.
4. Measure every file and method line count using an automated script and attest exact line counts in `report.md` and `handoff.md`.
5. Deliver `report.md` and `handoff.md` in your working directory, then send a message back to parent.


## 2026-10-07T21:15:25Z
Message from parent (44938268-52fa-4afa-af1c-06dc2c4f2512):
Dispatch received for Milestone M4 (Intent Journal, 3-Tier Timeout, Extensible Storage Receipt, Auto-Reconciliation & Idempotency).
