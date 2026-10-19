## 2026-10-07T22:28:42Z

# Task Assignment: worker_m5_r1

## Identity & Role
- **Agent Name**: worker_m5_r1
- **Archetype**: teamwork_preview_worker
- **Role**: Milestone M5 Implementation Worker (P2 Architecture Proposal)
- **Working Directory**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m5_r1\`
- **Project Root**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\`

## Authoritative Inputs (MANDATORY: Read First)
1. `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (read §R3 P2 Architectural Proposal).
2. `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md` (Feature Inventory F11, Architecture, Milestones).
3. `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\architecture\ARCHITECTURE.md`.
4. `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\clean-code\HP_CLEAN_CODE_CORE.md`.
5. `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m5_1\report.md` (Bidirectional DMU, Tombstoning & 3-Way Merge).
6. `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m5_2\report.md` (Background Queue Daemon, SQLite WAL & Concurrency).
7. `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m5_3\report.md` (Megaproject Batching, Worksharing & Sharded Storage).

## Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Write Ownership
- `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\architecture\HPBIM_ADVANCED_PIPELINE_DESIGN.md` (Exclusive target deliverable).

## Key Deliverable Requirements
Synthesize a comprehensive, production-grade architectural specification in `docs/architecture/HPBIM_ADVANCED_PIPELINE_DESIGN.md` containing:
1. **Executive Summary & System Context**:
   - Scope, purpose, and evolution from P0/P1 foundation to P2 enterprise architecture.
   - High-level system architecture diagram and component topology.
2. **Pillar 1 — Bidirectional Change Tracking & Dynamic Model Update (DMU)**:
   - Revit `IUpdater` lifecycle, registration (`isOptional: true`), event filtering, and in-transaction execution.
   - Solving the Zombie Element problem via Ledger Tombstoning (`IsTombstoned = true`, `TargetUniqueId = string.Empty`).
   - Deterministic 3-Way Merge Engine ($S_{\text{Base}}$ vs $S_{\text{CAD}}$ vs $S_{\text{RVT}}$) adhering to Rule 0 (0.0mm section tolerance, 1.0mm elevation/position tolerance) and formal RFI generation.
   - C# interface contracts and data models (`IRevitChangeTracker`, `IConflictResolutionPolicy`, `ModelChangeEvent`, `ConflictContext`, `CoordinationConflictReport`).
3. **Pillar 2 — Background Queue Daemon & Out-of-Process Task Processing**:
   - Long-lived background daemon process (`HPBim.Daemon`, .NET 10 Worker Service) as exclusive client to Revit's named pipe (`\\.\pipe\hprebar-mcp-r2026`).
   - Multi-client Windows Named Pipe IPC server (`\\.\pipe\hpbim-daemon-r2026`).
   - SQLite WAL durable task queue (`state/hpbim_queue.db`) with atomic `UPDATE ... RETURNING` leasing and 8-state task lifecycle.
   - Single-writer UI thread serialization, 3-tier priority scheduling with anti-starvation aging (+10/min), admission backpressure, and cancellation token cascade.
   - C# interface contracts (`IBimTaskQueue`, `IBimQueueDaemon`, `IBimTaskDispatcher`, `QueuedBimTask`).
4. **Pillar 3 — Megaproject Batching, Multi-Document Worksharing & Extensible Storage Sharding**:
   - Megaproject scaling (>100,000 elements) solving the 120s bridge timeout ceiling.
   - Topological chunked execution batches (500–1000 items/chunk), chained cryptographic checkpointing, and isolated blast radii.
   - Revit Worksharing (Central Models & Local Files): Disciplinary workset provisioning, proactive borrowing checks (`WorksharingUtils.GetCheckoutStatus`), avoidance of interactive modal deadlocks, and stage-boundary Central sync (SWC).
   - Cross-document linked models (`CrossDocumentCompositeKey` `[HostDoc]>[LinkInstance]#[Category]:[Handle]`) and 3D affine transform reconciliation.
   - Two-tier Extensible Storage sharding (Root Manifest Index + Leaf Chunk Shards on discrete `DataStorage` elements), capping entity payload to $\sim 36\text{ KB}$.
   - C# interface contracts (`IBatchExecutionPlanner`, `IWorksharingCoordinator`, `IShardedReceiptManager`).
5. **Implementation Roadmap & Migration Strategy**:
   - Phased rollout plan, risk mitigation matrix, and backward-compatibility preservation for P0/P1 baseline.
6. **Delivery**:
   - Save the document at `docs/architecture/HPBIM_ADVANCED_PIPELINE_DESIGN.md`.
   - Deliver detailed `report.md` and completion `handoff.md` in your working directory, then send a message back to parent.
