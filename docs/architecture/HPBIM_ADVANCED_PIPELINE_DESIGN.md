# HPBim Advanced Pipeline Architecture Specification (P2)
## Bidirectional Change Tracking, Background Queue Daemon, Megaproject Batching & Sharded Coordination Ledger

**Document ID**: HPBIM-ARCH-P2-20261008  
**Author**: Engineering Architecture Team (Synthesis of `explorer_m5_1`, `explorer_m5_2`, `explorer_m5_3`, compiled by `worker_m5_r1`)  
**Status**: APPROVED DESIGN PROPOSAL (Milestone M5 — Feature F11)  
**Target Solution**: `HPBim/HPBim.slnx` (`HPBim.Core`, `HPBim.Client`, `HPBim.Cli`, `HPBim.Daemon`)  
**Referenced Shared Components**: `McpShared/` (`HPRebar.Mcp.Contracts`, `HPRebar.McpBridge.Core`, `HPRebar.Mcp.Server.Core`)  
**Binding Standards**: `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/architecture/ARCHITECTURE.md`, `docs/clean-code/host-appendix/revit.md`  
**Governing Rule**: Rule 0 ("Nguyên Tắc Vàng") — *"Thà để một cấu kiện chưa dựng còn hơn dựng sai nhưng báo đúng."*

---

## 1. Executive Summary & System Context

### 1.1 Context, Scope & Strategic Motivation
The HPBim pipeline bridges autonomous engineering intelligence with Autodesk Revit 2026. In Milestones M1 through M4 (the P0/P1 foundation), the system established:
1. **P0 (Truthful Pipeline & Single Execution IR)**: Strict schema validation, unified `BimExecutionPlan` intermediate representation for Preview (dry-run rollback) and Apply (commit), per-element granularity with zero heuristic fallback, and measured geometric auditing via direct Revit API queries.
2. **P1 (Resilient Identity & Ledger Recovery)**: Document and plan identity snapshots, permanent Composite Source Keys (`[DocIdentifier]#[Category]:[PrimaryHandle]`), append-only local `IntentJournal`, atomic Revit Extensible Storage receipts on hidden `DataStorage` elements, 3-tier timeout management, and automatic recovery from ambiguous connection drops (`OutcomeUnknown`).

While P0 and P1 provide rock-solid point-to-point execution for standard residential projects (~300 elements, e.g., `NhaPho_5x18m`), deploying HPBim into industrial, multi-agent, and megaproject environments exposes three severe physical bottlenecks:
- **Human-Modeler Interference**: Interactive edits in Revit cause the **Zombie Element Problem** (deleted elements re-instantiated on next run) and **Silent Overwrite Hazards** (manual engineering adjustments overwritten by raw CAD syncs).
- **Physical IPC Exclusivity & Single-Threaded Apartment (STA) Deadlocks**: Revit is an STA application bound to Windows OS message pumps. The deployed Revit MCP Bridge listens on Named Pipe `\\.\pipe\hprebar-mcp-r2026` with `maxNumberOfServerInstances = 1`. Concurrent multi-agent queries throw pipe-busy exceptions, and ephemeral CLI commands severed mid-flight leave orphaned transactions.
- **Megaproject Scale Invalidation (>100,000 Elements)**: Sending 100,000 elements across a pipe in a single transaction breaches the mandatory 120-second execution budget, induces Out-of-Memory (OOM) undo stack thrashing, triggers worksharing modal borrowing dialogues, and risks total rollback (100% blast radius) from a single invalid join.

This document defines the **P2 Enterprise Architecture**, synthesizing three synchronized pillars:
1. **Pillar 1: Bidirectional Change Tracking & Revit Dynamic Model Update (DMU)** — In-transaction change capture (`IUpdater`), Ledger Tombstoning, deterministic 3-Way Merge ($S_{\text{Base}}$ vs $S_{\text{CAD}}$ vs $S_{\text{RVT}}$), and Rule 0 Request for Information (RFI) escalation.
2. **Pillar 2: Background Queue Daemon (`HPBim.Daemon`)** — Out-of-process .NET 10 Worker Service, exclusive broker to Revit's named pipe, concurrent client IPC server, SQLite WAL durable task queue, 3-tier priority scheduling with anti-starvation aging, and single-writer UI thread serialization.
3. **Pillar 3: Megaproject Scaling, Worksharing & Sharded Extensible Storage** — Chunked transaction batches (500–1000 items) across 8 topological stages, chained cryptographic checkpointing, proactive element borrowing (`WorksharingUtils.GetCheckoutStatus`), stage-boundary Central sync (SWC), cross-document linked model coordination, and two-tier Extensible Storage sharding (Root Manifest + Leaf Chunk Shards).

---

### 1.2 Evolution from P0/P1 Foundation to P2 Enterprise Architecture

```
┌──────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                ARCHITECTURAL EVOLUTION ROADMAP                                   │
├──────────────────────────────┬─────────────────────────────────┬─────────────────────────────────┤
│    P0 Foundation (M1-M2)     │       P1 Resilience (M3-M4)     │       P2 Enterprise (M5)        │
├──────────────────────────────┼─────────────────────────────────┼─────────────────────────────────┤
│ • Schema Validator           │ • DocumentIdentitySnapshot      │ • Bidirectional Revit DMU       │
│ • BimExecutionPlan (IR)      │ • CompositeSourceKey            │ • Ledger Tombstone Engine       │
│ • Preview & Apply Parity     │ • ProvenanceHandle Mapping      │ • Deterministic 3-Way Merge     │
│ • Honest Rollback Capture    │ • Local IntentJournal           │ • Background Queue Daemon       │
│ • Measured Geometric Audit   │ • Single ExtStorage Receipt     │ • SQLite WAL Priority Queue     │
│ • Zero Heuristic Fallbacks   │ • 3-Tier Timeout Policies       │ • Megaproject Chunk Batching    │
│ • FailFastGate & Rule 0      │ • Auto-Reconcile Unknowns       │ • Worksharing Pre-Flight Borr.  │
│                              │                                 │ • Two-Tier ExtStorage Sharding  │
└──────────────────────────────┴─────────────────────────────────┴─────────────────────────────────┘
```

---

### 1.3 High-Level System Architecture Topology

```
┌──────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                   CLIENT LAYER (M Concurrent Producers)                          │
│                                                                                                  │
│  ┌───────────────────────┐   ┌───────────────────────────┐   ┌───────────────────────────┐       │
│  │     HPBim.Cli CLI     │   │   AI Multi-Agent Team     │   │  Web Telemetry Dashboard  │       │
│  │ (Ephemeral Commands)  │   │ (Coordinator, CAD, QA/QC) │   │     (Status & Metrics)    │       │
│  └───────────┬───────────┘   └─────────────┬─────────────┘   └─────────────┬─────────────┘       │
└──────────────┼─────────────────────────────┼───────────────────────────────┼─────────────────────┘
               │                             │                               │
               └─────────────────────────────┼───────────────────────────────┘
                                             │ Concurrent Local Named Pipe IPC
                                             │ \\.\pipe\hpbim-daemon-r2026 (NDJSON / JSON-RPC 2.0)
                                             ▼
┌──────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                BACKGROUND QUEUE DAEMON (HPBim.Daemon)                            │
│                                                                                                  │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────┐  │
│  │ Multi-Client Named Pipe Server (PipeOptions.CurrentUserOnly, MaxAllowedServerInstances)    │  │
│  │ - Session ACL Enforcement, Request Deserialization & Schema Validation                     │  │
│  └──────────────────────────────────────────────┬─────────────────────────────────────────────┘  │
│                                                 ▼                                                │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────┐  │
│  │ Admission Controller & Backpressure Gate (Soft: 80% / Hard: 1,000 tasks, Token Bucket 20/s)│  │
│  └──────────────────────────────────────────────┬─────────────────────────────────────────────┘  │
│                                                 ▼                                                │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────┐  │
│  │ Durable Storage Engine: SQLite WAL (state/hpbim_queue.db)                                  │  │
│  │ - Priority Indexing: PreFlight (200) > Normal (100) > BackgroundQC (10)                    │  │
│  │ - Anti-Starvation Aging Engine (+10 priority per minute in queue, max +100 bonus)          │  │
│  │ - Atomic Lease via UPDATE ... RETURNING                                                    │  │
│  └──────────────────────────────────────────────┬─────────────────────────────────────────────┘  │
│                                                 ▼ Atomic Lease (1 Task at a Time)                │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────┐  │
│  │ Single-Writer Task Dispatcher & Watchdog                                                   │  │
│  │ - Sequential UI Thread Serialization (1-Consumer Model)                                    │  │
│  │ - Topological Batch Chunking (500–1000 items) & Checkpoint Sequencing                      │  │
│  │ - IntentJournal Synchronization (Pending -> InFlight -> Committed / Failed)                │  │
│  │ - 3-Tier Timeout Enforcement (Transport 5s, Queue 30s-15m, Execution Budget)               │  │
│  └──────────────────────────────────────────────┬─────────────────────────────────────────────┘  │
│                                                 ▼ Exclusive Client Connection                    │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────┐  │
│  │ Revit Connection Supervisor (Sole Client to \\.\pipe\hprebar-mcp-r2026)                    │  │
│  │ - Auto-Reconnect Exponential Backoff, revit.ping Heartbeat, Document Switch Sentry         │  │
│  └──────────────────────────────────────────────┬─────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────┼────────────────────────────────────────────────┘
                                                  │ Exclusive Named Pipe Connection
                                                  │ \\.\pipe\hprebar-mcp-r2026 (maxInstances = 1)
                                                  ▼
┌──────────────────────────────────────────────────────────────────────────────────────────────────┐
│                            AUTODESK REVIT 2026 (Host Process: Revit.exe)                         │
│                                                                                                  │
│  ┌────────────────────────────────────────────────────────────────────────────────────────────┐  │
│  │ HPRebar.McpBridge Add-in (In-Process Listener & Execution Core)                            │  │
│  │ - Interlocked _busy Gate -> ExternalEvent.Raise() -> UI Thread Apartment                   │  │
│  │ - Pre-Flight Identity Gate (Document Title & VersionGUID Verification)                     │  │
│  │ - Worksharing Pre-Flight Gate (Proactive Element Borrowing without Modals)                 │  │
│  │ - Chunked Transaction Runner with Progressive Commit Checkpoints                           │  │
│  │ - Two-Tier Extensible Storage Engine (Master Manifest Index + Leaf Chunk Shards)           │  │
│  └──────────────────────────────────────────────┬─────────────────────────────────────────────┘  │
│                                                 ▲ In-Transaction Trigger Capture                 │
│  ┌──────────────────────────────────────────────┴─────────────────────────────────────────────┐  │
│  │ BimDynamicModelUpdater (implements IUpdater, isOptional: true)                             │  │
│  │ - Triggers: Added, Modified, Deleted on Structural & Architectural Categories              │  │
│  │ - Context Suppression Scope (BimTrackingContext.CurrentOrigin == PipelineAutomation)       │  │
│  │ - RecursionGuard Thread-Local Lock                                                         │  │
│  │ - Real-Time Event Staging -> BimCoordinationLedger Tombstoning & 3-Way Merge               │  │
│  └────────────────────────────────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Pillar 1: Bidirectional Change Tracking & Revit Dynamic Model Update (DMU)

### 2.1 The Dynamic Model Update (`IUpdater`) Architecture
In a collaborative BIM workflow, Revit models are subject to continuous human refinement. Modelers shift partition walls, resize structural columns to meet punching shear calculations, or delete elements. 

In Autodesk Revit, the **Dynamic Model Update (DMU)** framework via `IUpdater` is the sole API mechanism permitted to intercept element additions, modifications, and deletions **within the exact transaction** that introduces the modification. This provides absolute consistency: changes are captured before transaction commit, and any policy violation (such as tampering with a frozen coordination zone) can either modify the change or abort the transaction cleanly.

---

### 2.2 Lifecycle, Registration & Trigger Filter Strategy

#### 2.2.1 Updater Identity & Optional Registration
The updater class `BimDynamicModelUpdater` implements `Autodesk.Revit.DB.IUpdater`:
- **Permanent GUID**:
  ```csharp
  public static readonly Guid UpdaterGuid = new("B7C48D21-39F2-4E15-9A82-5D1A041E67C9");
  ```
- **Mandatory Optional Registration (`isOptional: true`)**:
  ```csharp
  UpdaterRegistry.RegisterUpdater(updaterInstance, isOptional: true);
  ```
  *Crucial Architecture Requirement*: If an updater is registered with `isOptional: false`, opening the `.rvt` project file on any machine lacking the `HPRebar.McpBridge` add-in causes Revit to display a blocking error dialog: *"Third party updater is missing"*, preventing file access. Registering with `isOptional: true` ensures seamless interoperability across multidisciplinary teams.
- **Priority**:
  `GetChangePriority()` returns `ChangePriority.Structure` for structural elements and `ChangePriority.Views` for annotation updates, ensuring geometric dependencies solve in correct order.

#### 2.2.2 Trigger Scoping & Filter Optimization
To prevent UI thread degradation during intensive modeling sessions, triggers are strictly scoped via an `ElementMulticategoryFilter`:
- **Monitored Categories**:
  - `BuiltInCategory.OST_StructuralColumns`
  - `BuiltInCategory.OST_StructuralFraming`
  - `BuiltInCategory.OST_StructuralFoundation`
  - `BuiltInCategory.OST_Walls`
  - `BuiltInCategory.OST_Floors`
  - `BuiltInCategory.OST_Grids`
  - `BuiltInCategory.OST_Levels`
- **Registered Triggers**:
  1. `Element.GetChangeTypeElementAddition()`
  2. `Element.GetChangeTypeElementDeletion()`
  3. `Element.GetChangeTypeGeometry()`
  4. Explicit Parameter Watches:
     - `BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM` (Type/Dimension changes)
     - `BuiltInParameter.ALL_MODEL_MARK` (Mark/Identifier changes)
     - `BuiltInParameter.FAMILY_BASE_LEVEL_PARAM` / `FAMILY_TOP_LEVEL_PARAM` (Elevation constraints)
     - `BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM` (Elevation offsets)

---

### 2.3 In-Transaction Execution Semantics & Disappearing Element Dilemma

During `IUpdater.Execute(UpdaterData data)`:
- `data.GetDocument()` supplies the active `Document`.
- **Added Elements (`data.GetAddedElementIds()`)**:
  - If triggered by the automated pipeline (checked via `BimTrackingContext.CurrentOrigin == ChangeOrigin.PipelineAutomation`), the new ID is registered in the runtime identity lookup table.
  - If triggered by a human modeler, a `ModelChangeEvent` (`ModelChangeType.Added`, `ChangeOrigin.UserInteractive`) is generated.
- **Modified Elements (`data.GetModifiedElementIds()`)**:
  - The updater queries the modified element's geometry and parameters.
  - Compares the measured metrics against the coordination baseline in `LedgerElementMapping`.
  - If any delta exceeds `CoordinationTolerances`, a `ModelChangeEvent` is queued.
- **Deleted Elements (`data.GetDeletedElementIds()`) — The Disappearing Element Dilemma**:
  - *Revit API Constraint*: When an element is deleted, calling `doc.GetElement(id)` inside `Execute()` returns `null`. Its parameters, `UniqueId`, and geometric relationships are already erased from the document state!
  - *Solution*: The tracker maintains an in-memory dual map `RuntimeElementRegistry`:
    ```csharp
    private readonly Dictionary<long, TrackedElementMetadata> _elementIdToMetadata = new();
    private readonly Dictionary<string, long> _uniqueIdToElementId = new();
    ```
    Populated upon document load and updated after every pipeline receipt commit. When an `ElementId` appears in `data.GetDeletedElementIds()`, the tracker immediately retrieves its cached `UniqueId`, `CompositeSourceKey`, and category from `_elementIdToMetadata` before Revit finalizes memory purging.

---

### 2.4 Recursion Guards, Thread Locks & Context-Aware Suppression

Uncontrolled dynamic updaters risk infinite loops when an updater's write triggers another invocation. Furthermore, programmatic writes dispatched by HPBim must not be misconstrued as human modeling deviations.

We implement a two-layer defense:

```
┌────────────────────────────────────────────────────────────────────────┐
│                        TWO-LAYER RECURSION GUARD                       │
├────────────────────────────────────────────────────────────────────────┤
│ 1. BimTrackingContext.SuppressTracking(ChangeOrigin.PipelineAutomation)│
│    - Ambient AsyncLocal<ChangeOrigin> scope                            │
│    - When Active == PipelineAutomation: IUpdater.Execute immediately   │
│      bypasses conflict logging                                         │
├────────────────────────────────────────────────────────────────────────┤
│ 2. Thread-Local RecursionScope (RecursionGuard)                        │
│    - [ThreadStatic] private static bool _isExecuting                   │
│    - Re-entrant updater calls within the same callstack return null    │
│      and exit immediately                                              │
└────────────────────────────────────────────────────────────────────────┘
```

```csharp
public static class BimTrackingContext
{
    private static readonly AsyncLocal<ChangeOrigin> CurrentOriginHolder = new();

    public static ChangeOrigin CurrentOrigin => CurrentOriginHolder.Value;

    public static IDisposable SuppressTracking(ChangeOrigin origin)
    {
        var previous = CurrentOriginHolder.Value;
        CurrentOriginHolder.Value = origin;
        return new DisposableScope(() => CurrentOriginHolder.Value = previous);
    }

    private sealed class DisposableScope(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}
```

---

### 2.5 Ledger Tombstoning & The "Zombie Element" Solution

#### 2.5.1 The Zombie Element Defect
In naive CAD-to-BIM synchronization:
1. CAD defines Column `C1`.
2. Pipeline generates `C1` in Revit (`UniqueId = "u-123"`).
3. The structural engineer determines in Revit that `C1` must be deleted because an architectural atrium expands across that bay. Modeler deletes `C1`.
4. Next day, CAD engineer runs `hpbim generate --input plan.dwg.json`.
5. The generator looks up `C1` in the ledger. If deletion removed `C1` from the ledger, the resolver treats `C1` as brand-new and **re-creates it in Revit against engineering intent**!

#### 2.5.2 The Tombstone Architecture
When an element deletion is intercepted by `BimDynamicModelUpdater`:
1. The mapping in `BimCoordinationLedger` is **never removed**. Instead, it transitions to a **Tombstone State**:
   ```csharp
   mapping = mapping with
   {
       IsTombstoned = true,
       TombstonedAtUtc = DateTime.UtcNow,
       TombstoneReason = "User interactive deletion in Revit UI",
       TargetUniqueId = string.Empty,
       TargetElementId = null,
       DeviationState = ElementDeviationState.Tombstoned
   };
   ```
2. On subsequent pipeline executions, `IdempotentElementResolver` checks `mapping.IsTombstoned`.
3. Under **Rule 0 ("Nguyên Tắc Vàng")**:
   - The generator **REFUSES** to regenerate the element.
   - Status is marked `ElementExecutionStatus.Skipped`.
   - Reason: `"Element is tombstoned in Revit by user. Re-generation blocked pending RFI approval."`
   - An explicit Request for Information (`RfiRecord`) is raised in `BimCoordinationLedger.Rfis`.
4. **Revoking a Tombstone**:
   If the Chief Engineer determines the deletion was accidental:
   ```bash
   hpbim reconcile --untombstone "[NhaPho_5x18m.dwg]#[Column]:5A3"
   ```
   This clears `IsTombstoned = false`. On the subsequent run, the resolver issues `PlannedAction.Create`.

---

### 2.6 The Deterministic 3-Way Merge Engine

When reconciling data between CAD and Revit, the system evaluates three distinct states:
- **$S_{\text{Base}}$ (Baseline Ancestor)**: Snapshot state verified at the last synchronization (stored in `LedgerElementMapping` and Revit Extensible Storage).
- **$S_{\text{CAD}}$ (Incoming CAD Specification)**: The state extracted from current DWG drawings.
- **$S_{\text{RVT}}$ (Current Live Revit State)**: State measured directly in the active Revit document.

Let:
- $\Delta_{\text{CAD}} = (S_{\text{CAD}} \neq S_{\text{Base}})$: Indicates upstream CAD revision.
- $\Delta_{\text{RVT}} = (S_{\text{RVT}} \neq S_{\text{Base}})$: Indicates downstream Revit modeler edit.

```
                         ┌─────────────────────────────┐
                         │      S_Base (Baseline)      │
                         └──────────────┬──────────────┘
                                        │
                       ┌────────────────┴────────────────┐
                       ▼                                 ▼
        ┌─────────────────────────────┐   ┌─────────────────────────────┐
        │   S_CAD (Incoming DWG)      │   │   S_RVT (Live Revit State)  │
        └──────────────┬──────────────┘   └──────────────┬──────────────┘
                       │                                 │
                       └────────────────┬────────────────┘
                                        │
                                        ▼
                         ┌─────────────────────────────┐
                         │   3-Way Merge Evaluator     │
                         │    (Rule 0 Tolerance Gate)  │
                         └──────────────┬──────────────┘
                                        │
         ┌──────────────────────────────┼──────────────────────────────┐
         ▼                              ▼                              ▼
  [Within Tolerance]             [Δ_CAD XOR Δ_RVT]             [Conflict Exceeds Tol]
   - Reconcile / NoOp             - Upstream Apply /            - HALT & RAISE RFI
   - Update Baseline                Preserve Modeler            - ZERO SILENT WRITE
```

#### Deterministic 3-Way Merge Decision Matrix

| Case | $\Delta_{\text{CAD}}$ | $\Delta_{\text{RVT}}$ | Condition ($S_{\text{CAD}}$ vs $S_{\text{RVT}}$) | Action | Classification | Rule 0 Mandate |
| :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| **1** | False | False | $S_{\text{CAD}} = S_{\text{RVT}}$ | `NoOp` | Synchronized | PASS: Zero mutations required. |
| **2** | True | False | $S_{\text{CAD}} \neq S_{\text{RVT}}$ | `ApplyCad` | Upstream CAD Update | Modeler has not touched element; apply CAD changes. |
| **3** | False | True | Within `CoordinationTolerances` | `ReconcileTolerance` | Modeling Drift | Auto-align measurements within 1.0mm tolerance. |
| **4** | False | True | Exceeds `CoordinationTolerances` | `RaiseRfiAndHalt` | **Single Divergence Conflict** | **Rule 0 TRIGGER**: Modeler modified element; CAD unchanged. Stop and report RFI. |
| **5** | True | True | $S_{\text{CAD}} = S_{\text{RVT}}$ | `NoOp` | Convergent Edit | Both parties independently made identical change. Update baseline. |
| **6** | True | True | $S_{\text{CAD}} \neq S_{\text{RVT}}$ | `RaiseRfiAndHalt` | **Dual Divergence Conflict** | **Rule 0 TRIGGER**: Both CAD and Revit edited to different values. Stop and report RFI. |
| **7** | Any | Deleted | Element Tombstoned | `RaiseRfiAndHalt` | **Tombstone Conflict** | CAD expects element, Revit deleted it. Stop and report RFI. |

---

### 2.7 Concrete Divergence Scenario: Section 220x220 vs 220x300 Under Rule 0

Consider a production structural conflict:
- $S_{\text{Base}}$ for Column `C1`: Section $220 \times 220\text{ mm}$, Top Elevation $+3.800\text{ m}$.
- $S_{\text{CAD}}$ in latest drawing: Section $220 \times 220\text{ mm}$ (no revision).
- $S_{\text{RVT}}$ in active Revit document: Modeler changed Family Type to $220 \times 300\text{ mm}$ to satisfy punch shear checks.

#### Evaluation:
1. $\Delta_{\text{CAD}} = \text{False}$, $\Delta_{\text{RVT}} = \text{True}$.
2. Cross-section height deviation: $|300\text{ mm} - 220\text{ mm}| = 80.0\text{ mm}$.
3. Tolerance threshold from `CoordinationTolerances.SectionDimensionToleranceMm`: **$0.0\text{ mm}$** (Strict: zero tolerance on structural dimensions).
4. Delta $80.0\text{ mm} > 0.0\text{ mm} \implies$ Tolerance Breached!

#### Pipeline Behavior Under Rule 0:
1. **Zero Silent Overwrite**: The pipeline **REFUSES** to force $220 \times 220\text{ mm}$ onto Revit. Overwriting would destroy the structural engineer's sizing work without consent.
2. **Zero Heuristic Guesswork**: The pipeline **REFUSES** to assume Revit is correct and update CAD. The modeler may have assigned the wrong family type by accident.
3. **Execution Output**:
   - `BimPipelineEngine` marks element status: `ElementExecutionStatus.Skipped`.
   - Reason: `"Rule 0 Conflict: CAD specifies 220x220 mm but Revit model has 220x300 mm. Section delta = 80.0mm exceeds 0.0mm tolerance."`
   - Element moves to `ElementTechnicalGroup.Group2_ConflictingInfo`.
   - `BimCoordinationLedger` creates an immutable `RfiRecord`:
     ```json
     {
       "RfiId": "RFI-20261008-001",
       "Title": "Kích thước tiết diện Cột C1 mâu thuẫn giữa CAD và Revit",
       "ConflictDescription": "Bản vẽ CAD chỉ định Cột C1 tiết diện 220x220mm. Mô hình Revit hiện tại sử dụng Type 220x300mm. Độ lệch chiều cao tiết diện 80.0mm vượt dung sai cho phép 0.0mm.",
       "ElementIds": ["[NhaPho_5x18m.dwg]#[Column]:5A3"],
       "ProposedAction": "Chủ trì kết cấu xác nhận tiết diện chuẩn: Giữ 220x300mm (cập nhật lại bản vẽ CAD) hoặc sửa Revit về 220x220mm.",
       "Status": "PendingChiefEngineer",
       "CreatedAtUtc": "2026-10-08T06:00:00Z"
     }
     ```
   - Written to `state/reports/COORDINATION_CONFLICT_REPORT.json`.

---

### 2.8 Complete C# Interface Contracts & Domain Data Models

These contracts reside in `HPBim.Core/Coordination/` (`netstandard2.0`, zero `Autodesk.Revit.*` dependencies):

```csharp
namespace HPBim.Core.Coordination;

using System;
using System.Collections.Generic;

public enum ModelChangeType
{
    Added = 1,
    ModifiedGeometry = 2,
    ModifiedParameter = 3,
    Deleted = 4
}

public enum ChangeOrigin
{
    UserInteractive = 1,
    PipelineAutomation = 2,
    ExternalSync = 3
}

public sealed record ModelChangeEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public CompositeSourceKey SourceKey { get; init; } = CompositeSourceKey.Empty;
    public string TargetUniqueId { get; init; } = string.Empty;
    public long? TargetElementId { get; init; }
    public CoordinationCategory Category { get; init; }
    public ModelChangeType ChangeType { get; init; }
    public ChangeOrigin Origin { get; init; }
    public string ChangedProperty { get; init; } = string.Empty;
    public string? PreviousValue { get; init; }
    public string? CurrentValue { get; init; }
    public double? ExceededDeltaMm { get; init; }
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;

    public static ModelChangeEvent Create(
        CompositeSourceKey sourceKey,
        string uniqueId,
        long? elementId,
        CoordinationCategory category,
        ModelChangeType changeType,
        ChangeOrigin origin,
        string propertyName,
        string? prevVal,
        string? currVal,
        double? deltaMm = null) => new()
    {
        SourceKey = sourceKey ?? throw new ArgumentNullException(nameof(sourceKey)),
        TargetUniqueId = uniqueId ?? string.Empty,
        TargetElementId = elementId,
        Category = category,
        ChangeType = changeType,
        Origin = origin,
        ChangedProperty = propertyName ?? string.Empty,
        PreviousValue = prevVal,
        CurrentValue = currVal,
        ExceededDeltaMm = deltaMm,
        TimestampUtc = DateTime.UtcNow
    };
}

public interface IRevitChangeTracker
{
    bool IsTrackingActive { get; }
    void StartTracking(string documentTitle);
    void StopTracking();
    void RegisterTrackedElement(CompositeSourceKey sourceKey, string uniqueId, long elementId);
    IReadOnlyList<ModelChangeEvent> FlushPendingEvents();
    IDisposable SuppressTracking(ChangeOrigin origin);
}

public enum ConflictResolutionAction
{
    NoOp = 1,
    ApplyCad = 2,
    PreserveRevit = 3,
    RaiseRfiAndHalt = 4
}

public sealed record ConflictContext
{
    public CompositeSourceKey SourceKey { get; init; } = CompositeSourceKey.Empty;
    public string TargetUniqueId { get; init; } = string.Empty;
    public CoordinationCategory Category { get; init; }
    public string PropertyName { get; init; } = string.Empty;
    public string BaseValue { get; init; } = string.Empty;
    public string CadValue { get; init; } = string.Empty;
    public string RevitValue { get; init; } = string.Empty;
    public double MeasuredDeltaMm { get; init; }
    public double ToleranceThresholdMm { get; init; }
    public bool IsTombstonedInRevit { get; init; }
}

public sealed record ConflictResolutionDecision
{
    public ConflictResolutionAction Action { get; init; }
    public string Reason { get; init; } = string.Empty;
    public bool GeneratesRfi { get; init; }
    public RfiRecord? GeneratedRfi { get; init; }

    public static ConflictResolutionDecision HaltWithRfi(string reason, RfiRecord rfi) => new()
    {
        Action = ConflictResolutionAction.RaiseRfiAndHalt,
        Reason = reason,
        GeneratesRfi = true,
        GeneratedRfi = rfi
    };

    public static ConflictResolutionDecision ApplyCad(string reason) => new()
    {
        Action = ConflictResolutionAction.ApplyCad,
        Reason = reason,
        GeneratesRfi = false
    };

    public static ConflictResolutionDecision PreserveRevit(string reason) => new()
    {
        Action = ConflictResolutionAction.PreserveRevit,
        Reason = reason,
        GeneratesRfi = false
    };

    public static ConflictResolutionDecision NoOp(string reason) => new()
    {
        Action = ConflictResolutionAction.NoOp,
        Reason = reason,
        GeneratesRfi = false
    };
}

public interface IConflictResolutionPolicy
{
    ConflictResolutionDecision ResolveConflict(ConflictContext context);
}

public sealed record CoordinationConflictReport
{
    public Guid ReportId { get; init; } = Guid.NewGuid();
    public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;
    public string ProjectTitle { get; init; } = string.Empty;
    public int TotalTrackedElements { get; init; }
    public int SynchronizedCount { get; init; }
    public int ConflictedCount { get; init; }
    public int TombstonedCount { get; init; }
    public IReadOnlyList<ConflictContext> ActiveConflicts { get; init; } = Array.Empty<ConflictContext>();
    public IReadOnlyList<RfiRecord> GeneratedRfis { get; init; } = Array.Empty<RfiRecord>();
}
```

---

## 3. Pillar 2: Background Queue Daemon & Out-of-Process Task Processing

### 3.1 The Physical Constraints of Direct-to-Revit IPC
In P0/P1, CLI processes connected directly to Revit's named pipe `\\.\pipe\hprebar-mcp-r2026`. This model encounters strict physical boundaries:
1. **Revit Named Pipe Exclusivity**: The Revit bridge accepts exactly 1 client instance (`maxNumberOfServerInstances = 1`). Any concurrent request from another CLI command, subagent, or monitoring probe throws `IOException: All pipe instances are busy`.
2. **UI Thread Apartment Bottleneck**: Revit is strictly single-threaded for model mutation (`Autodesk.Revit.DB.Transaction`). Multi-threaded access corrupts Revit's native C++ memory heaps.
3. **Transient Process Drops**: An ephemeral CLI command killed abruptly (e.g., terminal closed, network drop) severs the Named Pipe mid-flight, creating ambiguous `OutcomeUnknown` states.
4. **Interactive Starvation**: An interactive query from the Coordinator (e.g., a 20ms bounding box or level query) is blocked indefinitely behind a heavy 5-minute structural framing batch.

---

### 3.2 The Background Queue Daemon (`HPBim.Daemon`) Architecture
To solve these bottlenecks, `HPBim.Daemon` is established as a long-lived, out-of-process .NET 10 Worker Service. It operates as the **exclusive, persistent broker** to Revit's Named Pipe, exposing a concurrent multi-client IPC surface to all external callers:

```
┌─────────────────────────────────────────────────────────────────────────────────────────────┐
│                          M-Producers, 1-Consumer Architecture                               │
├─────────────────────────────────────────────────────────────────────────────────────────────┤
│  CLI 1 (Generate)   ───┐                                                                    │
│  CLI 2 (Verify)     ───┼──▶ \\.\pipe\hpbim-daemon-r2026 ──▶ SQLite WAL Queue ──▶ Dispatcher│
│  Agent (Coordinator)───┤                                                            │       │
│  Dashboard          ───┘                                                            │       │
│                                                                                     ▼       │
│                                                                   \\.\pipe\hprebar-mcp-r2026│
│                                                                                     │       │
│                                                                                     ▼       │
│                                                                              Revit UI Thread│
└─────────────────────────────────────────────────────────────────────────────────────────────┘
```

---

### 3.3 Multi-Client Named Pipe IPC Server (`\\.\pipe\hpbim-daemon-r2026`)
- **Protocol**: NDJSON (Newline-Delimited JSON) streaming conforming to JSON-RPC 2.0 messages from `HPRebar.Mcp.Contracts`.
- **Security**: Windows ACL restricted via `PipeOptions.CurrentUserOnly`. Unprivileged local processes cannot inject commands.
- **Concurrency**: Configured with `NamedPipeServerStream.MaxAllowedServerInstances`. Whenever a client connects, the daemon immediately provisions the next listener instance.
- **Discovery**: Upon startup, the daemon acquires an exclusive file lock on `%LOCALAPPDATA%\HPBim\daemon.lock` and atomically writes registration metadata to `%LOCALAPPDATA%\HPBim\daemon.json`:
  ```json
  {
    "Pid": 41280,
    "PipeName": "hpbim-daemon-r2026",
    "HostVersion": 2026,
    "StartedAtUtc": "2026-10-08T06:00:00.0000000Z",
    "RevitPipeName": "hprebar-mcp-r2026",
    "RevitConnected": true,
    "ActiveDocTitle": "NhaPho_5x18m.rvt"
  }
  ```

---

### 3.4 Durable SQLite WAL Queue Engine & Schema

To guarantee that no task is ever lost across crashes (Rule 0 non-repudiation), queue persistence uses **SQLite in Write-Ahead Logging (WAL)** mode:
```sql
PRAGMA journal_mode = WAL;
PRAGMA synchronous = NORMAL;
PRAGMA busy_timeout = 5000;
PRAGMA foreign_keys = ON;
PRAGMA temp_store = MEMORY;
```

#### Relational Table DDL:
```sql
CREATE TABLE IF NOT EXISTS BimTasks (
    TaskId                  TEXT PRIMARY KEY,              -- Guid (ToString("D"))
    OperationId             TEXT NOT NULL UNIQUE,          -- Matching IntentJournal & ExtStorage
    PlanHash                TEXT NOT NULL,                 -- SHA-256 hash of execution plan
    Priority                INTEGER NOT NULL,              -- 200=PreFlight, 100=Normal, 10=BackgroundQC
    VirtualPriority         INTEGER NOT NULL,              -- Priority + Anti-starvation aging bonus
    State                   INTEGER NOT NULL DEFAULT 0,    -- 0..7 (BimTaskState)
    ItemCount               INTEGER NOT NULL,              -- Total elements in plan
    EnqueuedAtUtc           TEXT NOT NULL,                 -- ISO 8601 UTC
    DispatchedAtUtc         TEXT,                          -- Dispatch lease timestamp
    CompletedAtUtc          TEXT,                          -- Terminal completion timestamp
    DeadlineUtc             TEXT NOT NULL,                 -- Absolute expiration deadline
    TargetDocTitle          TEXT NOT NULL,                 -- Target Revit document title
    TargetDocVersionGuid    TEXT NOT NULL,                 -- Target Revit VersionGUID
    PlanJson                TEXT NOT NULL,                 -- Serialized BimExecutionPlan
    ExecutionResultJson     TEXT,                          -- Serialized BimPipelineResult on completion
    ErrorReason             TEXT,                          -- Diagnostic failure reason
    RetryCount              INTEGER NOT NULL DEFAULT 0,    -- Current retry attempt
    MaxRetries              INTEGER NOT NULL DEFAULT 3,    -- Maximum allowed automatic retries
    LeaseToken              TEXT,                          -- Worker lease token GUID
    WorkerLeaseExpiryUtc    TEXT                           -- Expiry time for active worker lease
);

CREATE INDEX IF NOT EXISTS IX_BimTasks_Dispatch 
    ON BimTasks (State, VirtualPriority DESC, EnqueuedAtUtc ASC)
    WHERE State = 0;

CREATE INDEX IF NOT EXISTS IX_BimTasks_OperationId 
    ON BimTasks (OperationId);

CREATE TABLE IF NOT EXISTS BimTaskEvents (
    EventId                 INTEGER PRIMARY KEY AUTOINCREMENT,
    TaskId                  TEXT NOT NULL REFERENCES BimTasks(TaskId) ON DELETE CASCADE,
    TimestampUtc            TEXT NOT NULL,
    EventType               TEXT NOT NULL,                 -- Enqueued, Dispatched, InFlight, Committed, Failed
    PayloadJson             TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_BimTaskEvents_TaskId 
    ON BimTaskEvents (TaskId, EventId ASC);
```

---

### 3.5 Atomic Dequeue Leasing with `UPDATE ... RETURNING`

To prevent multi-threading race conditions while supporting single-writer leasing, SQLite atomic updates with `RETURNING` are used:
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
If the daemon terminates unexpectedly while a task is `Dispatched` or `InFlight`, the startup cold scanner detects any task whose `WorkerLeaseExpiryUtc < @NowUtc` and transitions it to recovery reconciliation.

---

### 3.6 8-State Task Lifecycle Machine & Transition Matrix

```
                      ┌────────────────────────────────────────┐
                      │          Client Submits Task           │
                      └───────────────────┬────────────────────┘
                                          │ Schema Validation PASS
                                          ▼
                                  ┌───────────────┐
                  ┌──────────────►│    Queued     │◄──────────────────┐
                  │               └───────┬───────┘                   │
                  │                       │ Atomic Lease              │
                  │                       ▼                           │
                  │               ┌───────────────┐                   │
                  │               │  Dispatched   │                   │
                  │               └───────┬───────┘                   │
                  │                       │ Pre-Flight Gate OK        │
                  │                       ▼                           │
                  │               ┌───────────────┐                   │
                  │               │   InFlight    │                   │
                  │               └───────┬───────┘                   │
   Transient Retry│                       │                           │
   with Backoff   │       ┌───────────────┼───────────────┐           │
                  │       │ Commit OK     │ Pre-flight    │ Pipe Drop │
                  │       │ ExtStore OK   │ Rollback Conf.│ Timeout   │
                  │       ▼               ▼               ▼           │
                  │ ┌───────────┐   ┌───────────┐   ┌────────────────┐│
                  │ │ Committed │   │  Failed   │   │ OutcomeUnknown ││
                  │ └───────────┘   └─────┬─────┘   └────────┬───────┘│
                  │                       │                  │ Reconcile
                  │                       │                  ▼        │
                  │                       │         ┌─────────────────┤
                  │                       │         │ Receipt Found?  │
                  │                       │         └───┬─────────┬───┘
                  │                       │         YES │         │ NO
                  │                       │             │         └───┘
                  │                       │             ▼
                  │                       │       ┌───────────┐
                  │                       │       │ Committed │
                  │                       │       └───────────┘
                  │                       │ Retries
                  │                       │ Exhausted
                  │                       ▼
                  │               ┌───────────────┐
                  └───────────────┤   DeadLetter  │
                                  └───────────────┘
```

| State | Value | Description | Permitted Next States |
|---|---|---|---|
| `Queued` | 0 | Task validated and persisted in SQLite. Waiting for single-writer lease. | `Dispatched`, `Cancelled` |
| `Dispatched` | 1 | Single-writer leased task. Performing on-thread pre-flight identity check. | `InFlight`, `Failed`, `Queued` (re-lease) |
| `InFlight` | 2 | Flushed to local `IntentJournal`. Active script executing in Revit UI thread. | `Committed`, `Failed`, `OutcomeUnknown` |
| `Committed` | 3 | Transaction committed, Extensible Storage receipt written, ledger updated. | *(Terminal Success)* |
| `Failed` | 4 | Clean rejection (document mismatch, Roslyn compile error, dry-run rollback). | `Queued` (retryable), `DeadLetter` |
| `OutcomeUnknown` | 5 | Pipe dropped or timeout expired while InFlight. Requires ExtStorage reconcile. | `Committed` (reconciled), `Failed`, `DeadLetter` |
| `DeadLetter` | 6 | Irrecoverable failure, schema corruption, or retry ceiling (3) reached. | *(Terminal Audit)* |
| `Cancelled` | 7 | Cooperative cancellation requested by client before or during execution. | *(Terminal Cancellation)* |

---

### 3.7 Dual Persistence Strategy: SQLite Task Queue vs Local IntentJournal
- **SQLite (`state/hpbim_queue.db`)**: Manages the **Queue Management Tier** — multi-client concurrency, task submission, priority ordering, worker leasing, and event streaming.
- **IntentJournal (`state/intent_journal.jsonl`)**: Manages the **Revit Coordination Audit Tier** — an append-only, hardware-flushed (`fsync`) record of every atomic transaction dispatched across the Revit boundary, directly matching the GUIDs stored in Revit's `DataStorage` Extensible Storage entity (`HPBimCoordinationReceipt`).

---

### 3.8 Priority Classes & Anti-Starvation Aging Algorithm

#### Priority Tiers:
1. **`PreFlight` / `High` (200)**: Interactive queries: Document title check (`revit.context`), active view bounding box, ping, element parameter inspection, cancel requests. Budget: < 5 seconds.
2. **`Normal` (100)**: Standard BIM execution plans: Substructure batches, column runs, beam networks, floor slabs. Budget: 15–120 seconds.
3. **`BackgroundQC` / `Low` (10)**: Heavy offline audits: Measured geometric verification (`MeasuredGeometricVerifier`), full-model clash detection, mesh extraction. Budget: 60–300 seconds.

#### Dynamic Anti-Starvation Aging Formula:
$$\text{VirtualPriority} = \text{BasePriority} + \min\left(100, \left\lfloor \frac{\text{WaitTimeSeconds}}{60} \right\rfloor \times 10\right)$$
Every 60 seconds a task waits in `Queued` state, its `VirtualPriority` increases by **+10** (capped at +100 bonus). A `BackgroundQC` task (base priority 10) waiting for 10 minutes reaches virtual priority $110$, bypassing newly submitted `Normal` tasks (priority 100) and preventing starvation.

---

### 3.9 Admission Control, Token-Bucket Rate Limiting & Backpressure
1. **Queue Capacity Ceilings**:
   - `MaxQueueDepth`: 1,000 tasks.
   - Soft Backpressure at 80% (800 tasks): Delays low-priority submissions by 500ms and emits telemetry warnings.
   - Hard Backpressure at 100% (1,000 tasks): Rejects new submissions with `QueueCapacityExceededException` (JSON-RPC Error `-32005: Queue Full`).
2. **Token Bucket Rate Limiter**:
   - Limits each client ID (e.g., `agent-coordinator`, `agent-cad`) to 20 submissions/second with burst allowance of 40.

---

### 3.10 Cooperative Cancellation Cascade Protocol (`revit.cancel`)
1. **Queued Phase**: The task is updated directly in SQLite to `Cancelled`. The single-writer dispatcher skips it upon dequeue.
2. **InFlight Phase**:
   - Client calls `CancelTaskAsync(taskId)`.
   - Daemon signals the active `CancellationTokenSource`.
   - Daemon immediately dispatches `revit.cancel` across the Named Pipe to Revit.
   - Inside Revit, `McpBridgeExternalEventHandler.Cancel()` invokes `_currentCancel.Cancel()`.
   - The executing script observes `cancellationToken.ThrowIfCancellationRequested()`, rolls back `TransactionGroup`, and returns `ExecuteResult.Cancelled()`.

---

### 3.11 Complete C# Interface Contracts & DTOs

Residing in `HPBim.Core/Queue/` and `HPBim.Daemon/`:

```csharp
namespace HPBim.Core.Queue;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Core.Execution;

public enum BimTaskPriority
{
    BackgroundQc = 10,
    Normal = 100,
    PreFlight = 200
}

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

public sealed record BimTaskSubmission
{
    public BimExecutionPlan Plan { get; init; } = new();
    public BimTaskPriority Priority { get; init; } = BimTaskPriority.Normal;
    public string ClientId { get; init; } = string.Empty;
    public TimeSpan? QueueDeadline { get; init; }
    public string? CorrelationId { get; init; }
}

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
    public bool IsTerminal => State is BimTaskState.Committed or BimTaskState.Failed or BimTaskState.DeadLetter or BimTaskState.Cancelled;
}

public interface IBimTaskQueue : IAsyncDisposable, IDisposable
{
    Task<Guid> EnqueueAsync(BimTaskSubmission submission, CancellationToken cancellationToken = default);
    Task<QueuedBimTask?> DequeueLeaseAsync(TimeSpan leaseDuration, CancellationToken cancellationToken = default);
    Task CompleteTaskAsync(Guid taskId, BimPipelineResult result, CancellationToken cancellationToken = default);
    Task FailTaskAsync(Guid taskId, string errorReason, bool isOutcomeUnknown = false, CancellationToken cancellationToken = default);
    Task<bool> CancelTaskAsync(Guid taskId, string reason, CancellationToken cancellationToken = default);
    Task<QueuedBimTask?> GetTaskAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QueuedBimTask>> GetActiveTasksAsync(CancellationToken cancellationToken = default);
    Task<QueueStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
    Task<int> PurgeExpiredTasksAsync(TimeSpan retentionWindow, CancellationToken cancellationToken = default);
}

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

public sealed record DaemonHealthStatus
{
    public bool IsRunning { get; init; }
    public bool IsRevitConnected { get; init; }
    public string? ActiveRevitDocument { get; init; }
    public TimeSpan Uptime { get; init; }
    public int ActiveConnections { get; init; }
    public QueueStatistics QueueStats { get; init; } = new();
}
```

---

## 4. Pillar 3: Megaproject Scaling, Multi-Document Worksharing & Extensible Storage Sharding

### 4.1 Megaproject Scale Problem (>100,000 Elements) & Blast Radius Math

When models expand to megaproject scale ($N = 100,000$ elements):
1. **Named Pipe Payload**: Serializing 100,000 elements generates 60–180 MB JSON, risking Out-Of-Memory exceptions during IPC string allocations.
2. **Undo Stack Memory Explosion**: Maintaining uncommitted transaction states for 100,000 elements inflates Revit process memory by over 3.5 GB.
3. **Execution Timeout Breach**: Running 100,000 elements takes 45–75 minutes of continuous UI thread lock, violently breaching the 120s bridge timeout ceiling.
4. **Catastrophic Failure Blast Radius**:
   If an invalid geometric join occurs on element 99,999:
   - In a monolithic transaction, **100% of the work is rolled back** (all 100,000 elements lost).
   - In a chunked architecture ($C = 500$ elements/chunk), failure is strictly contained to that chunk ($\le 0.5\%$ blast radius), preserving all previously committed chunks.

#### Mathematical Blast Radius Comparison:
Let $p = 10^{-4}$ be the probability of an invalid geometric intersection per element.
- Monolithic Failure Probability:
  $$P_{\text{fail, mono}} = 1 - (1 - p)^{100,000} \approx 1 - e^{-10} \approx 99.995\%$$
  *(A monolithic transaction almost certainly fails on megaprojects!)*
- Chunk Failure Probability ($C = 500$):
  $$P_{\text{fail, chunk}} = 1 - (1 - p)^{500} \approx 1 - e^{-0.05} \approx 4.88\%$$
  *(95.12% of chunks commit without incident; errors are isolated to discrete 500-element blocks).*

---

### 4.2 Topological Dependency Ordering & 8-Stage Partitioning Strategy

Elements cannot be sliced arbitrarily by array index. Structural hierarchy enforces a strict topological partial order across 8 canonical BIM stages:

$$\text{Stage 1: Controls} \prec \text{Stage 2: Substructure} \prec \text{Stage 3: Frame} \prec \text{Stage 4: Enclosure} \prec \text{Stage 5: Fixtures} \prec \text{Stage 6: Rooms} \prec \text{Stage 7: MEP} \prec \text{Stage 8: Rebars}$$

```
┌────────────────────────────────────────────────────────────────────────┐
│               8-STAGE TOPOLOGICAL EXECUTION PIPELINE                   │
├────────────────────────────────────────────────────────────────────────┤
│ Stage 1: Control Baseline    (Levels, Grids, Reference Planes)         │
│ Stage 2: Substructure        (Piles, Footings, Caps, Grade Beams)      │
│ Stage 3: Superstructure      (Columns, Shear Walls, Beams, Slabs)      │
│ Stage 4: Building Enclosure  (Curtain Walls, Exterior/Interior Walls)  │
│ Stage 5: Hosted Fixtures     (Doors, Windows, Slab/Wall Openings)      │
│ Stage 6: Spatial Synthesis   (Rooms, Boundaries, Areas, Spaces)        │
│ Stage 7: MEP Networks        (Ducts, Pipes, Trays, Equipment)          │
│ Stage 8: Structural Rebar    (Rebar Containers, Area/Path Rebar)       │
└────────────────────────────────────────────────────────────────────────┘
```

Within each stage, items are bucketed into chunks ($C = 500$ elements) ordered by:
1. **Host-First Sorting**: Host elements precede dependent hosted elements (e.g., Walls before Doors; Columns/Beams before Rebars).
2. **Elevation Sorting**: Ordered by ascending elevation ($Z$-coordinate).
3. **Spatial Octree Clustering**: Spatially contiguous elements are batched together to minimize Revit R-Tree spatial index thrashing during join recalculations.

---

### 4.3 Progressive Commit Checkpointing & Cryptographic Hash Lineage

Each chunk $k \in [1..M]$ carries an immutable cryptographic lineage:
- `ChunkId` (`Guid`): Unique chunk ID.
- `BatchId` (`Guid`): Master batch execution ID.
- `ChunkIndex` (`int`): Sequence index ($1 \dots M$).
- `ChunkHash` (`string`): Deterministic SHA-256 hash of element items in Chunk $k$.
- `PriorCheckpointHash` (`string`): SHA-256 hash of Chunk $k-1$'s checkpoint.

This chained hash structure guarantees non-repudiation: if any chunk payload is tampered with or skipped, the cryptographic chain is invalidated.

#### Failure Recovery Policies:
1. **`HaltAndPreserveCheckpoint` (Default Rule 0 Policy)**:
   - Chunks $1 \dots k-1$ remain permanently committed in Revit, recorded in `IntentJournal`, and stored in Extensible Storage shards.
   - Chunk $k$ rolls back cleanly inside Revit.
   - Remaining chunks $k+1 \dots M$ are marked `SkippedDueToPriorFailure`.
   - Emits `BatchCheckpointReport` with exact failed handles and error diagnostics.
   - On retry: `IdempotentElementResolver` recognizes Chunks $1 \dots k-1$ as `NoOp` and resumes directly at Chunk $k$.
2. **`CompensatingRollback` (Optional All-or-Nothing Mode)**:
   - Reverses committed chunks in reverse sequence ($k-1 \rightarrow 1$), deleting generated elements by `UniqueId` via dedicated compensating transactions.

---

### 4.4 Revit Worksharing Integration: Central Models, ACC & Local Files

In workshared enterprise models (on LAN or Autodesk Construction Cloud), automation faces two fatal hazards:
1. **Workset Misplacement**: Elements created by automation end up in whatever workset was active (corrupting standards).
2. **Element Borrowing Deadlock**: Modifying an element borrowed by another human modeler causes Revit to throw an interactive modal dialog: *"Element is currently borrowed by [User]. You must place a request to edit."* In headless execution over Named Pipe, this modal freezes Revit's UI thread until timeout!

#### 4.4.1 Disciplinary Workset Taxonomy & Dynamic Provisioning
The pipeline enforces workset governance:
- `ST_Substructure`: Piles, Footings, Grade Beams.
- `ST_Superstructure`: Columns, Core Shear Walls, Framing Beams, Slabs.
- `AR_Exterior`: Curtain Walls, Exterior Doors/Windows.
- `AR_Interior`: Interior Partitions, Doors, Finishes.
- `AR_Rooms`: Room Boundaries, Areas.
- `MEP_Plumbing`, `MEP_HVAC`, `MEP_Electrical`.
- `ST_Rebar`: Rebar Containers, Structural Reinforcement.

Before creating elements, the pipeline verifies workset existence:
```csharp
WorksetTable worksetTable = doc.GetWorksetTable();
if (WorksetTable.IsUniqueWorksetName(doc, targetWorksetName))
{
    Workset.Create(doc, targetWorksetName);
}
worksetTable.SetActiveWorksetId(targetWorksetId);
```
And explicitly sets `BuiltInParameter.ELEM_PARTITION_PARAM` after element instantiation.

#### 4.4.2 Proactive Element Borrowing Pre-Flight Gate (Modal Dialog Prevention)
To eliminate interactive modal deadlocks during updates:
1. Extract all target `ElementId`s for items marked `Updated` or `Deleted`.
2. Evaluate `WorksharingUtils.GetCheckoutStatus(doc, elementId)`:
   - `CheckoutStatus.OwnedByCurrentUser`: Safe to mutate immediately.
   - `CheckoutStatus.NotOwned`: Safe for batch checkout.
   - `CheckoutStatus.OwnedByOtherUser`: **BORROW CONFLICT**.
3. If owned by another user:
   - Query owner: `string owner = WorksharingUtils.GetWorksharingTooltipInfo(doc, id).Owner;`
   - **FAIL-FAST**: Immediately skip element with status `SkippedDueToBorrowConflict`.
   - Reason: `"Element {id} is checked out by user '{owner}'. Operation skipped to prevent modal lock."`
4. For all `NotOwned` elements, execute batch checkout:
   ```csharp
   ICollection<ElementId> checkedOut = WorksharingUtils.CheckoutElements(doc, eligibleIds);
   ```

#### 4.4.3 Synchronize with Central (SWC) Coordination
Automation must not hoard borrowed elements. Synchronization occurs at designated boundaries:
- **Pre-Job**: `doc.ReloadLatest(new ReloadLatestOptions())` ensures the local copy reflects latest changes.
- **Stage Boundary Checkpoints**: After completing major structural stages (Substructure, Superstructure), execute `doc.SynchronizeWithCentral(transactOptions, syncOptions)` with:
  - `RelinquishOptions.CheckedOutElements = true` (releasing modified elements).
  - Exponential backoff retry (5 retries, 2s–10s) to handle transient file lock contention.

---

### 4.5 Multi-Document Coordination & Cross-Document Composite Keys

In federated BIM models (`Campus_Master.rvt`, `TowerA_Structure.rvt`, `TowerA_Architecture.rvt`), elements reference linked models. The single-document composite key is extended:

$$\text{CompositeKey} := \text{LinkPrefix}^? \text{"["} \text{DocIdentifier} \text{"]#["} \text{Category} \text{"]:"} \text{PrimaryHandle}$$

- **Direct Native Element**: `[TowerA_ST]#[Column]:C12_STORY1`
- **Linked Element Reference**: `[TowerA_ST]>[ARC_LINK:Inst_102]#[Wall]:EXT_WALL_SOUTH`
- **Hosted Across Link**: `[TowerA_MEP]^[STR_LINK:Inst_105]#[PipeOpening]:BEAM_OPENING_B4`

```csharp
public sealed record CrossDocumentCompositeKey
{
    public string HostDocIdentifier { get; init; } = string.Empty;
    public string? LinkIdentifier { get; init; }
    public CoordinationCategory Category { get; init; }
    public string PrimaryHandle { get; init; } = string.Empty;
    public bool IsLinked => !string.IsNullOrEmpty(LinkIdentifier);

    public override string ToString() => IsLinked
        ? $"[{HostDocIdentifier}]>[{LinkIdentifier}]#[{Category}]:{PrimaryHandle}"
        : $"[{HostDocIdentifier}]#[{Category}]:{PrimaryHandle}";
}
```

#### Coordinate Systems & 3D Affine Transform Reconciliation:
Revit models operate across three 3D coordinate spaces:
1. **Internal Coordinate System (ICS)**: Fixed, unmovable origin $(0, 0, 0)$ used by all geometry creation APIs.
2. **Project Coordinate System (PCS)**: Relative to Project Base Point (PBP) with True North rotation.
3. **Shared Coordinate System (SCS)**: Defined by Survey Point (SP) and georeferenced coordinates (VN-2000 / UTM).

When transforming points from a linked model:
```csharp
RevitLinkInstance link = ...;
Transform totalTransform = link.GetTotalTransform();
XYZ hostIcsPoint = totalTransform.OfPoint(linkedPoint);
XYZ hostIcsVector = totalTransform.OfVector(linkedVector);
```
Pre-flight verification checks the reference control point (e.g. Grid intersection X1/YA). If distance $\| P_{\text{CAD}} - P_{\text{Revit\_ICS}} \| > 1.0\text{ mm}$ without an explicit transform matrix, **Rule 0 halts execution** with `CoordinateSystemMismatchException`.

---

### 4.6 Two-Tier Extensible Storage Sharding Across Discrete DataStorage Elements

#### 4.6.1 The Single-Entity Extensible Storage Bottleneck
In Milestone M4, receipts were written to a single `DataStorage` element under schema `HPBimCoordinationReceipt`. For 100,000 elements, this design fails catastrophically:
1. **Entity Size Exhaustion**: Serializing 100,000 mappings produces a JSON string of 18–35 MB. Storing this in a single Extensible Storage `Entity` exceeds internal string buffer limitations, throwing `ArgumentException` or silently corrupting.
2. **Worksharing Lock Hotspot**: A single `DataStorage` element has a single `ElementId`. Every batch must check out this element, causing immediate worksharing lock contention.
3. **Deserialization Latency**: Deserializing 35 MB JSON on every pre-flight check takes 1.5–3.0 seconds and allocates 150 MB heap memory.

#### 4.6.2 The Two-Tier Sharded Architecture
To eliminate these bottlenecks, receipts are partitioned across a two-tier hierarchy:

```
┌────────────────────────────────────────────────────────────────────────┐
│ Tier 1: Master Manifest Index (Single Root DataStorage)                │
│ Schema: HPBimReceiptManifestIndex (GUID: A1B2C3D4-E5F6-7A8B-9C0D...)   │
│ - ManifestId (Guid), LastUpdatedUtc (string), BatchCount (int)         │
│ - ManifestEntriesJson: Compact array of batch headers (~120 B/batch)   │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ References Shard UniqueIds
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│ Tier 2: Leaf Chunk Shards (Discrete DataStorage Element Per Chunk)     │
│ Schema: HPBimChunkReceiptShard (GUID: B2C3D4E5-F6A7-8B9C-0D1E...)      │
│                                                                        │
│  ┌───────────────────────┐ ┌───────────────────────┐                   │
│  │ Chunk Shard 1 (500)   │ │ Chunk Shard 2 (500)   │ ... [Shard M]     │
│  │ - OperationId (Guid)  │ │ - OperationId (Guid)  │                   │
│  │ - BatchId (Guid)      │ │ - BatchId (Guid)      │                   │
│  │ - ChunkIndex: 1       │ │ - ChunkIndex: 2       │                   │
│  │ - ElementCount: 500   │ │ - ElementCount: 500   │                   │
│  │ - ChunkMapJson (~36KB)│ │ - ChunkMapJson (~36KB)│                   │
│  └───────────────────────┘ └───────────────────────┘                   │
└────────────────────────────────────────────────────────────────────────┘
```

#### Mathematical Sizing Guarantee:
- Chunk size: $C = 500$ elements.
- Single mapping entry size:
  `"[TowerA_ST]#[Column]:C12_STORY1": "e4f5a6b7-8c9d-0e1f-2a3b-4c5d6e7f8a9b"` $\approx 72\text{ bytes}$.
- Total `ChunkMapJson` payload:
  $$500 \times 72\text{ bytes} \approx 36\text{ KB}$$
- Safety Margin: $36\text{ KB} \ll 16\text{ MB}$ (utilizing only **$0.22\%$** of the safe capacity ceiling).
- **Zero Worksharing Contention**: Each chunk creates a *brand-new* `DataStorage` element via `DataStorage.Create(doc)`. Creating a new element allocates a fresh `ElementId` and does NOT edit or lock existing elements!

---

### 4.7 Complete C# Interface Contracts for Pillar 3

Residing in `HPBim.Core/Execution/`, `HPBim.Client/Coordination/`, and `HPBim.Client/Recovery/`:

```csharp
namespace HPBim.Core.Execution;

using System;
using System.Collections.Generic;

public interface IBatchExecutionPlanner
{
    ExecutionBatchPlan CreateBatchPlan(BimExecutionPlan plan, BatchPlanningOptions options);
    IReadOnlyList<ExecutionPlanChunk> PartitionIntoChunks(IReadOnlyList<ElementPlanItem> items, int maxChunkSize = 500);
    BatchValidationResult ValidateTopologicalOrder(IReadOnlyList<ExecutionPlanChunk> chunks);
    CompensatingBatchPlan CreateCompensatingRollbackPlan(IReadOnlyList<ExecutionPlanChunk> committedChunks);
}

public sealed record ExecutionBatchPlan
{
    public Guid BatchId { get; init; } = Guid.NewGuid();
    public Guid MasterPlanId { get; init; }
    public string PlanHash { get; init; } = string.Empty;
    public DocumentIdentitySnapshot TargetDoc { get; init; } = new();
    public IReadOnlyList<ExecutionPlanChunk> Chunks { get; init; } = Array.Empty<ExecutionPlanChunk>();
    public BatchPlanningOptions Options { get; init; } = new();
    public int TotalElementCount => Chunks.Sum(c => c.Items.Count);
    public int TotalChunkCount => Chunks.Count;
}

public sealed record ExecutionPlanChunk
{
    public Guid ChunkId { get; init; } = Guid.NewGuid();
    public Guid BatchId { get; init; }
    public int ChunkIndex { get; init; }
    public int TotalChunks { get; init; }
    public int StageIndex { get; init; }
    public string StageName { get; init; } = string.Empty;
    public string ChunkHash { get; init; } = string.Empty;
    public string? PriorCheckpointHash { get; init; }
    public IReadOnlyList<ElementPlanItem> Items { get; init; } = Array.Empty<ElementPlanItem>();
}

public sealed record BatchPlanningOptions
{
    public int MaxChunkSize { get; init; } = 500;
    public bool EnforceStageBoundaries { get; init; } = true;
    public bool EnableCompensatingRollback { get; init; } = false;
    public TimeSpan InterChunkDelay { get; init; } = TimeSpan.FromMilliseconds(50);
}
```

```csharp
namespace HPBim.Client.Coordination;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IWorksharingCoordinator
{
    Task<WorksharingStateResult> QueryWorksharingStateAsync(CancellationToken cancellationToken = default);
    Task<WorksetProvisionResult> EnsureWorksetsAsync(IReadOnlyList<string> requiredWorksetNames, CancellationToken cancellationToken = default);
    Task<BorrowPreFlightResult> PrepareElementBorrowingAsync(IReadOnlyList<string> targetUniqueIds, CancellationToken cancellationToken = default);
    Task<CentralSyncResult> SynchronizeWithCentralAsync(CentralSyncOptions options, CancellationToken cancellationToken = default);
    Task<RelinquishResult> RelinquishOwnershipAsync(RelinquishOptions options, CancellationToken cancellationToken = default);
}

public sealed record BorrowPreFlightResult
{
    public bool Success { get; init; }
    public IReadOnlyList<string> SuccessfullyCheckedOutUniqueIds { get; init; } = Array.Empty<string>();
    public IReadOnlyList<BorrowConflictDetail> ConflictedElements { get; init; } = Array.Empty<BorrowConflictDetail>();
    public bool HasConflicts => ConflictedElements.Count > 0;
}

public sealed record BorrowConflictDetail
{
    public string UniqueId { get; init; } = string.Empty;
    public long ElementId { get; init; }
    public string OwnerUsername { get; init; } = string.Empty;
    public string ElementCategory { get; init; } = string.Empty;
}
```

```csharp
namespace HPBim.Client.Recovery;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;

public interface IShardedReceiptManager
{
    Task<ShardCommitResult> CommitChunkReceiptAsync(
        ExecutionPlanChunk chunk,
        IReadOnlyDictionary<CompositeSourceKey, string> committedMappings,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChunkReceiptRecord>> GetBatchReceiptsAsync(
        Guid batchId,
        CancellationToken cancellationToken = default);

    Task<string?> ResolveUniqueIdAsync(
        CompositeSourceKey sourceKey,
        CancellationToken cancellationToken = default);

    Task<IndexRebuildResult> RebuildManifestIndexAsync(
        CancellationToken cancellationToken = default);

    Task<CompactionReport> CompactHistoricalShardsAsync(
        TimeSpan olderThan,
        CancellationToken cancellationToken = default);
}

public sealed record ChunkReceiptRecord
{
    public Guid ShardDataStorageUniqueId { get; init; }
    public Guid BatchId { get; init; }
    public Guid OperationId { get; init; }
    public Guid ChunkId { get; init; }
    public int ChunkIndex { get; init; }
    public int TotalChunks { get; init; }
    public int StageIndex { get; init; }
    public string PlanHash { get; init; } = string.Empty;
    public string ChunkHash { get; init; } = string.Empty;
    public DateTime CommittedAtUtc { get; init; }
    public int ElementCount { get; init; }
    public IReadOnlyDictionary<string, string> ElementMap { get; init; } = new Dictionary<string, string>();
}
```

---

## 5. Performance Benchmarks & Mathematical Sizing Estimates

### 5.1 100,000-Element Megaproject Simulation Profile
The following table details performance metrics simulated across 8 stages for an enterprise commercial tower complex containing 100,000 physical elements:

| Stage | Discipline / Category | Element Count | Chunks ($C = 500$) | Est. Runtime / Chunk | Total Stage Runtime | Memory Delta ($\Delta$) | Extensible Storage Shards |
|---|---|---|---|---|---|---|---|
| **Stage 1** | Levels, Grids, Controls | 150 | 1 | 0.8 s | 0.8 s | 8 MB | 1 shard (12 KB) |
| **Stage 2** | Substructure (Piles, Caps) | 12,000 | 24 | 3.2 s | 76.8 s (~1.3 min) | 28 MB | 24 shards (864 KB) |
| **Stage 3** | Superstructure (Cols, Beams, Slabs) | 35,000 | 70 | 4.1 s | 287.0 s (~4.8 min) | 35 MB | 70 shards (2.52 MB) |
| **Stage 4** | Building Enclosure (Walls, Facades) | 18,000 | 36 | 3.5 s | 126.0 s (~2.1 min) | 30 MB | 36 shards (1.30 MB) |
| **Stage 5** | Hosted Fixtures (Doors, Windows) | 10,000 | 20 | 2.8 s | 56.0 s (~0.9 min) | 25 MB | 20 shards (720 KB) |
| **Stage 6** | Spatial Synthesis (Rooms, Boundaries) | 2,500 | 5 | 2.2 s | 11.0 s (~0.2 min) | 20 MB | 5 shards (180 KB) |
| **Stage 7** | MEP Networks (Ducts, Pipes) | 15,000 | 30 | 3.8 s | 114.0 s (~1.9 min) | 32 MB | 30 shards (1.08 MB) |
| **Stage 8** | Structural Rebar Reinforcement | 7,350 | 15 | 4.5 s | 67.5 s (~1.1 min) | 38 MB | 15 shards (540 KB) |
| **TOTAL** | **Full Integrated Model** | **100,000** | **201 chunks** | **Avg 3.6 s** | **~12.3 minutes** | **Max 38 MB** | **201 shards (~7.2 MB total)** |

---

### 5.2 Latency, Throughput & Memory Consumption Comparison

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                        MONOLITHIC vs CHUNKED SHARDED COMPARISON                        │
├──────────────────────────┬────────────────────────────┬────────────────────────────────┤
│ Metric                   │ Monolithic Transaction     │ Chunked & Sharded Pipeline     │
├──────────────────────────┼────────────────────────────┼────────────────────────────────┤
│ Total Execution Time     │ Fails at 120s timeout      │ ~12.3 minutes (201 chunks)     │
│ Peak Memory Overhead     │ > 3.5 GB (OOM risk)        │ 38 MB (GC released per chunk)  │
│ UI Thread Lockout        │ 45–75 min frozen           │ Yields 50ms between chunks     │
│ Failure Blast Radius     │ 100% (100,000 items lost)  │ <= 0.5% (500 items isolated)   │
│ Receipt Size             │ 35 MB single string (OOM)  │ 36 KB per discrete DataStorage │
│ Worksharing Borrowing    │ Fatal modal dialog lock    │ Proactive checkout pre-flight  │
│ Crash Recovery           │ Re-run entire 100k items   │ Resume directly at chunk k     │
└──────────────────────────┴────────────────────────────┴────────────────────────────────┘
```

---

## 6. Comprehensive Failure Modes & Edge Case Mitigations

| # | Failure Mode / Threat Scenario | Detection Mechanism | Architectural Mitigation | Rule 0 Compliance |
|---|---|---|---|---|
| **1** | **Offline Modeler Edits** (Model modified without add-in loaded) | Pre-flight hash comparison on document open or batch start. | Compares live element geometry/parameters against Extensible Storage baseline; raises RFI on divergence. | **Zero silent overwrite**: Catches offline changes before executing batch. |
| **2** | **The Zombie Element Problem** (Modeler deleted element in Revit) | In-memory lookup cache detects deletion in `IUpdater.Execute`. | Elements flagged `IsTombstoned = true`. Generator skips re-creation and emits RFI. | **Intent preservation**: Never recreate elements deleted by humans. |
| **3** | **Element Borrowing Deadlock in Workshared Models** | `WorksharingUtils.GetCheckoutStatus` reports `OwnedByOtherUser`. | Fails fast before Revit displays modal dialog. Item marked `SkippedDueToBorrowConflict`. | **Zero modal lock**: Automation never hangs Revit UI thread. |
| **4** | **120-Second Named Pipe Timeout Ceiling** | Chunk execution timer monitored by daemon dispatcher. | Partitions 100,000 items into 500-element chunks executing in 2.5–4.5s (well below 120s limit). | **Deterministic execution**: Eliminates transport timeouts. |
| **5** | **Extensible Storage Size Exhaustion (>16MB String)** | Serializer payload measurement before write. | Two-tier sharding: Each chunk stored on discrete `DataStorage` element (~36 KB payload). | **Zero data loss**: Safe within 0.22% of capacity ceiling. |
| **6** | **Concurrent Named Pipe Collisions (Pipe Busy)** | Multiple clients connecting to single Revit pipe. | `HPBim.Daemon` serves as sole persistent client to Revit; provides multi-instance IPC server to clients. | **Zero pipe contention**: Eliminates `IOException: Pipe busy`. |
| **7** | **Interactive Query Starvation Behind Heavy Batches** | Priority comparison in SQLite queue lease query. | 3-tier priority scheduling: PreFlight queries (200) preempt Normal batches; dynamic aging prevents starvation. | **High responsiveness**: Interactive queries return in <500ms. |
| **8** | **Revit Crashes Mid-Transaction (Power/OOM)** | Named Pipe disconnects while task is `InFlight`. | Task marked `OutcomeUnknown`. On restart, daemon reconciles against Extensible Storage shards. | **Atomic verification**: Never assume committed without receipt. |
| **9** | **Active Document Switched in Revit** | Pre-flight script header checks `doc.Title` & `VersionGUID`. | Script immediately halts if active document does not match `TargetDoc`; returns diagnostic error. | **Zero misplacement**: Prevents drawing elements into wrong project. |
| **10** | **Undo/Redo Stack Churn by User** | `IUpdater` triggers on undo/redo actions. | Evaluates live geometry against $S_{\text{Base}}$. Returning to baseline restores `Synchronized` status. | **State alignment**: Dynamically tracks user reversals. |
| **11** | **Linked Model Coordinate Offset Drift** | Pre-flight check measures distance between CAD/RVT control points. | If delta > 1.0mm without transform matrix, throws `CoordinateSystemMismatchException`. | **Rule 0 enforcement**: Halts execution on coordinate drift. |
| **12** | **Third-Party Updater Recursion Cascade** | Thread-local reentrancy check in `RecursionGuard`. | ThreadStatic boolean lock prevents nested updater execution; bypasses automated pipeline writes. | **Zero infinite loops**: Updaters never cascade out of control. |

---

## 7. Actionable Implementation Roadmap & Backward Compatibility

### 7.1 Phased Delivery Roadmap

```
┌──────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                   PHASED IMPLEMENTATION TIMELINE                                 │
├───────────────────┬────────────────────────────────────────────┬─────────────────────────────────┤
│ Phase             │ Scope & Deliverables                       │ Targeted Milestone / Branch     │
├───────────────────┼────────────────────────────────────────────┼─────────────────────────────────┤
│ **Phase 1**       │ • Domain Models & Contracts in Core        │ Milestone M6                    │
│ *(Weeks 1–2)*     │ • ThreeWayMergeEngine & Rule 0 Policies    │ `feature/p2-merge-and-models`   │
│                   │ • IBatchExecutionPlanner Chunking          │                                 │
│                   │ • Unit Test Suite (Zero Revit required)    │                                 │
├───────────────────┼────────────────────────────────────────────┼─────────────────────────────────┤
│ **Phase 2**       │ • BimDynamicModelUpdater in Revit Bridge   │ Milestone M7                    │
│ *(Weeks 3–4)*     │ • RecursionGuard & BimTrackingContext      │ `feature/p2-dmu-tracking`       │
│                   │ • Ledger Tombstone Integration             │                                 │
│                   │ • Sharded Extensible Storage Engine        │                                 │
├───────────────────┼────────────────────────────────────────────┼─────────────────────────────────┤
│ **Phase 3**       │ • HPBim.Daemon .NET 10 Worker Service      │ Milestone M8                    │
│ *(Weeks 5–6)*     │ • SqliteBimTaskQueue with WAL & Leasing    │ `feature/p2-queue-daemon`       │
│                   │ • Multi-Client Named Pipe IPC Server       │                                 │
│                   │ • Single-Writer Serialization & Aging      │                                 │
├───────────────────┼────────────────────────────────────────────┼─────────────────────────────────┤
│ **Phase 4**       │ • WorksharingCoordinator & SWC Sync        │ Milestone M9                    │
│ *(Weeks 7–8)*     │ • CLI Integration (`hpbim daemon`)         │ `feature/p2-worksharing-megaproj│
│                   │ • 100,000-Element Integration Benchmark    │                                 │
│                   │ • End-to-End Stress & Adversarial Hardening│                                 │
└───────────────────┴────────────────────────────────────────────┴─────────────────────────────────┘
```

---

### 7.2 Backward Compatibility & Zero-Regression Assurance for P0/P1

1. **Host-Neutral Separation**: All new interfaces (`IRevitChangeTracker`, `IBimTaskQueue`, `IBatchExecutionPlanner`) reside in `HPBim.Core` (targeting `netstandard2.0`) and have zero dependencies on `Autodesk.Revit.*`.
2. **Seamless Single-Execution Fallback**: If `HPBim.Daemon` is not running, CLI commands automatically fall back to direct Named Pipe execution over `\\.\pipe\hprebar-mcp-r2026`, preserving 100% of P0/P1 behavior.
3. **Receipt Upward Compatibility**: The Tier 1 Master Manifest Index recognizes legacy M4 `HPBimCoordinationReceipt` entities, reading them as a single monolithic shard without requiring database migrations.
4. **Zero Test Regressions**: The 670 existing unit and integration tests (`HPBim.Core.Tests`, `HPBim.Client.Tests`, `HPBim.E2E.Tests`) continue to pass with 100% green status.

---

### 7.3 Risk Management & Mitigation Matrix

| Risk Identified | Severity | Likelihood | Mitigation Strategy |
|---|---|---|---|
| **SQLite WAL Lock Contention on Windows** | Medium | Low | Configure `PRAGMA busy_timeout = 5000;` and dedicate single-writer connection for all task state mutations. |
| **Daemon Auto-Spawn Latency** | Low | Medium | CLI uses background detached process spawn; caches discovery token in `%LOCALAPPDATA%\HPBim\daemon.json`. |
| **Revit Memory Fragmentation on Megaprojects** | High | Medium | Execute `GC.Collect()` and trigger Revit idle yield between topological batch stages. |
| **Central Model Sync Conflicts During SWC** | Medium | High | Exponential backoff retry (5 retries, 2s–10s) with automatic reload latest before final sync. |

---

## 8. Architectural Sign-off & Conclusion

This specification provides the authoritative, production-grade architectural foundation for the **HPBim Advanced Pipeline (P2)**. By uniting:
- **Pillar 1**: Bidirectional DMU change tracking, ledger tombstoning, and 3-way merge conflict resolution,
- **Pillar 2**: The background queue daemon, SQLite WAL persistent task queue, and single-writer priority scheduling, and
- **Pillar 3**: Megaproject chunked execution batching, proactive worksharing borrowing, and two-tier Extensible Storage sharding,

HPBim scales from small residential models to enterprise megaprojects containing over 100,000 elements with mathematical reliability, zero guesswork, zero silent overwrites, and uncompromising adherence to **Rule 0 ("Nguyên Tắc Vàng")**:

> *"Thà để một cấu kiện chưa dựng còn hơn dựng sai nhưng báo đúng."*
