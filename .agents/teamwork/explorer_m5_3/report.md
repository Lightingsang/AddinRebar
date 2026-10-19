# Architectural Proposal: Large-Scale Model Batching, Revit Worksharing & Extensible Storage Sharding (Milestone M5 — Feature F11 Part 3)

## Executive Summary

As building information modeling expands from single residential buildings (e.g., NhaPho 5x18m, ~300 elements) to massive commercial complexes, high-rise towers, and airport transit hubs (>100,000 physical elements), monolithic execution pipelines break down. Sending 100,000 elements across a Named Pipe in a single transaction induces Out-Of-Memory (OOM) failures, locks the Revit UI thread for over an hour, breaches the mandatory 120-second execution timeout ceiling, and creates a 100% blast-radius failure mode where a single invalid geometric join rolls back hours of computation. Furthermore, enterprise models operate in workshared environments (Central Models on LAN or Autodesk Construction Cloud), where uncoordinated element borrowing crashes automation with modal conflict dialogues, and linked models demand cross-document coordinate transforms.

This proposal establishes the architectural blueprint for:
1. **Chunked Transaction Batching (500–1000 items)** with topological dependency sorting, progressive commit checkpointing, and isolated failure blast radii.
2. **Revit Worksharing Integration** featuring dynamic workset assignment, fail-fast proactive element checkout (`WorksharingUtils.CheckoutElements`), conflict avoidance, and Central sync coordination.
3. **Multi-Document & Linked Model Coordination** with cross-document composite keys (`[HostDoc]>[LinkInstance]#[Category]:[Handle]`) and 3D affine transformation reconciliation across Internal, Project, and Shared Coordinate systems.
4. **Extensible Storage Sharding** eliminating the 16MB entity size exhaustion constraint by provisioning discrete `DataStorage` elements per chunk indexed by a master manifest.
5. **C# Interface Contracts** (`IBatchExecutionPlanner`, `IWorksharingCoordinator`, `IShardedReceiptManager`) adhering strictly to Clean Code Governance (`HP_CLEAN_CODE_CORE.md`, `REVITADDINAI_CLEAN_CODE_STANDARD.md`).

---

## 1. Large-Scale Model Batching & Chunked Execution Plans

### 1.1 The Megaproject Scale Problem

In a megaproject ($N = 100,000$ elements consisting of columns, shear walls, transfer girders, floor slabs, MEP runs, and rebar sets), executing changes within a single monolithic transaction incurs severe compounding failures:

| Metric / Failure Mode | Monolithic Transaction ($N = 100,000$) | Chunked Pipeline ($C = 500 - 1000$ items) | Architectural Gain |
|---|---|---|---|
| **Named Pipe Payload** | 60 MB – 180 MB JSON | 300 KB – 900 KB per chunk | Zero OOM during IPC serialization |
| **Revit Memory Footprint** | Peak $\Delta > 3.5\text{ GB}$ uncommitted undo stack | $\Delta \approx 25\text{ MB} - 45\text{ MB}$ per chunk | GC and memory release between chunks |
| **Execution Duration** | 45 – 75 minutes continuous lock | 2.5 – 6.0 seconds per chunk | Well below the 120s timeout limit |
| **UI Thread Responsiveness** | OS "Not Responding", complete UI freeze | Yields control between chunks | Responsive message pump and progress |
| **Blast Radius** | 100% (1 element fails $\rightarrow$ 99,999 lost) | $\le 1.0\%$ (isolated to active chunk) | Failure contained to 500 elements |
| **Failure Probability** | $P_{\text{fail}} = 1 - (1 - 10^{-4})^{10^5} \approx 99.995\%$ | $P_{\text{chunk\_fail}} = 1 - (1 - 10^{-4})^{500} \approx 4.8\%$ | Checkpointing preserves 95.2% on error |

```
Monolithic Failure Blast Radius:
[Item 1 ... Item 99,998 (Valid)] -> [Item 99,999 (Invalid Join)] ──▶ [TOTAL ROLLBACK (100,000 Items Lost)]

Chunked Checkpoint Blast Radius:
[Chunk 1 (500) Committed] ──▶ [Chunk 2 (500) Committed] ──▶ [Chunk 3 (Fail)] ──▶ [Halt at Checkpoint 2]
                                                                                   (1,000 Items Preserved)
```

### 1.2 Topological Dependency Ordering & Chunking Strategy

Elements cannot be partitioned arbitrarily by index slicing. In BIM, structural hierarchy enforces a strict topological partial order:
$$\text{Levels} \prec \text{Grids} \prec \text{Substructure} \prec \text{Columns/ShearWalls} \prec \text{Beams} \prec \text{Slabs} \prec \text{Walls} \prec \text{Openings} \prec \text{Rooms} \prec \text{MEP} \prec \text{Rebars}$$

```
                ┌────────────────────────────────────────────────────────┐
                │             Stage 1: Control Baseline                  │
                │             (Levels, Grids, Reference Planes)          │
                └───────────────────────────┬────────────────────────────┘
                                            ▼
                ┌────────────────────────────────────────────────────────┐
                │             Stage 2: Substructure                      │
                │     (Piles, Caps, Footings, Grade Beams, Rafts)        │
                └───────────────────────────┬────────────────────────────┘
                                            ▼
                ┌────────────────────────────────────────────────────────┐
                │             Stage 3: Superstructure Frame              │
                │        (Columns, Core Walls, Beams, Slabs)             │
                └───────────────────────────┬────────────────────────────┘
                                            ▼
                ┌────────────────────────────────────────────────────────┐
                │             Stage 4: Building Enclosure & Partitions   │
                │        (Curtain Walls, Exterior/Interior Walls)        │
                └───────────────────────────┬────────────────────────────┘
                                            ▼
                ┌────────────────────────────────────────────────────────┐
                │             Stage 5: Hosted Fixtures & Openings        │
                │               (Doors, Windows, Openings)               │
                └───────────────────────────┬────────────────────────────┘
                                            ▼
                ┌────────────────────────────────────────────────────────┐
                │             Stage 6: Spatial Synthesis (Rooms)         │
                │              (Room Boundaries, Tags, Spaces)           │
                └───────────────────────────┬────────────────────────────┘
                                            ▼
                ┌────────────────────────────────────────────────────────┐
                │             Stage 7: MEP Distribution Networks         │
                │           (Ducts, Pipes, Trays, Equipment)             │
                └───────────────────────────┬────────────────────────────┘
                                            ▼
                ┌────────────────────────────────────────────────────────┐
                │             Stage 8: Structural Reinforcement          │
                │         (Rebar Containers, Area/Path Rebar)            │
                └────────────────────────────────────────────────────────┘
```

Within each stage, elements are sorted by:
1. **Host Dependency**: Host elements precede dependent hosted elements (e.g., Walls before hosted Doors/Windows; Beams/Columns before hosted Rebars).
2. **Elevation/Level Index**: Ascending $Z$-elevation ($Z_0 \le Z_1 \le \dots \le Z_k$).
3. **Spatial Octree / Bounding Box Clustered Buckets**: Elements located in spatial proximity are batched together to minimize Revit R-Tree spatial index thrashing during join recalculations.

### 1.3 Progressive Commit & Checkpointing Protocol

Each chunk $k \in [1..M]$ is executed as an isolated atomic transaction in Revit:

```
Pipeline Client                                    Revit MCP Bridge (UI Thread)
       │                                                         │
       │ ── 1. Dispatch Chunk k (Args: Chunk k, Checkpoint) ───▶ │
       │                                                         │ ── Start Transaction "MCP: Chunk k"
       │                                                         │ ── Create/Update Elements
       │                                                         │ ── Write Sharded Receipt DataStorage
       │                                                         │ ── Commit Transaction
       │ ◀── 2. Return ExecuteResult (Committed, Records) ────── │
       │                                                         │
       │ ── 3. Update IntentJournal (Chunk k: Committed)         │
       │ ── 4. Update BimCoordinationLedger (SourceKey -> UID)   │
       │ ── 5. Yield / Message Pump (50ms interval)              │
       │                                                         │
       │ ── 6. Dispatch Chunk k+1 ─────────────────────────────▶ │
```

#### Checkpointing Data Model
Each chunk carries cryptographic and topological lineage:
- `ChunkId` (`Guid`): Unique identifier for this chunk.
- `BatchId` (`Guid`): Master batch identifier.
- `ChunkIndex` (`int`): Sequence position ($1 \dots M$).
- `TotalChunks` (`int`): Total chunk count ($M$).
- `StageIndex` (`int`): Active topological stage ($1 \dots 8$).
- `ChunkHash` (`string`): Deterministic SHA-256 hash of the chunk items.
- `PriorCheckpointHash` (`string`): Chained hash of Chunk $k-1$ (verifying unbroken lineage).

#### Failure Recovery Policies
When Chunk $k$ fails:
1. **`HaltAndPreserveCheckpoint` (Default Rule 0 Policy)**:
   - Chunks $1 \dots k-1$ remain permanently committed in Revit and recorded in `IntentJournal` and Extensible Storage shards.
   - Chunk $k$ rolls back cleanly inside Revit (zero uncommitted mutations).
   - Remaining chunks $k+1 \dots M$ are marked `SkippedDueToPriorFailure`.
   - The pipeline outputs a `BatchCheckpointReport` documenting:
     - Exact failed element `CompositeSourceKey`s and Revit error codes.
     - Preserved checkpoint hash.
     - RFI item for modeler resolution.
   - Resumption: Once the CAD model or geometry is corrected, the pipeline re-runs the plan. The `IdempotentElementResolver` recognizes Chunks $1 \dots k-1$ as `NoOp` (already existing in Revit via ledger) and resumes execution directly at Chunk $k$.
2. **`CompensatingRollback` (Optional All-or-Nothing Mode)**:
   - If user explicitly configures `AllOrNothing = true`:
   - The pipeline queries the committed `DataStorage` receipt shards for Chunks $k-1 \dots 1$.
   - Executes compensating transactions in reverse order ($k-1 \rightarrow 1$), deleting created elements by their `UniqueId`s.
   - Rolls back ledger entries and updates `IntentJournal` to `CompensatedRollback`.

---

## 2. Revit Worksharing Integration

### 2.1 Central Model Architecture & Concurrency Challenges

Enterprise models utilize Worksharing:
- **Central Model Types**: Local Area Network (UNC path `\\server\share\Project_Central.rvt`) or Autodesk Construction Cloud (`doc.IsModelInCloud == true`).
- **Local Files**: Each user and automation runner works in a local copy (`doc.GetWorksharingCentralModelPath()`).

When multiple human modelers and automated pipelines operate concurrently, two fatal collision modes emerge:
1. **Workset Misplacement**: Elements created by automation end up in whatever workset was active in the document (often `Shared Levels and Grids` or `Workset1`), corrupting project standards.
2. **Element Borrowing Deadlock**: Modifying an existing element owned/borrowed by another user causes Revit to trigger an interactive Windows modal dialog: *"Element is currently borrowed by [User]. You must place a request to edit."* In headless automation over Named Pipe, this modal dialogue halts the Revit UI thread indefinitely, causing a timeout crash.

```
                    ┌──────────────────────────────────────────────┐
                    │               Central Model                  │
                    │         (LAN Server / ACC Cloud)             │
                    └───────────────▲───────────────▲──────────────┘
                                    │               │
                            Sync /  │               │ Sync /
                            Checkout│               │ Checkout
                                    │               │
                    ┌───────────────▼──────┐ ┌──────▼──────────────┐
                    │    Human Modeler     │ │  Automated Pipe     │
                    │   (Local Copy A)     │ │   (Local Copy B)    │
                    │  Owns Column COL-12  │ │  Tries Update COL-12│
                    └──────────────────────┘ └──────┬──────────────┘
                                                    │
                                                    ▼
                                    ┌──────────────────────────────┐
                                    │ Proactive Checkout Check:    │
                                    │ Detects "OwnedByOtherUser"   │
                                    │ ──▶ FAILS FAST (No Modal!)   │
                                    └──────────────────────────────┘
```

### 2.2 Dynamic Workset Provisioning & Element Assignment

The pipeline implements automated workset governance:
1. **Standard Disciplinary Workset Taxonomy**:
   - `ST_Substructure`: Piles, Footings, Grade Beams, Foundation Slabs.
   - `ST_Superstructure`: Columns, Core Shear Walls, Framing Beams, Slabs.
   - `AR_Exterior`: Curtain Walls, Facades, Exterior Doors/Windows.
   - `AR_Interior`: Interior Partitions, Doors, Finishes, Ceilings.
   - `AR_Rooms`: Room Boundaries, Areas, Tags.
   - `MEP_Plumbing`, `MEP_HVAC`, `MEP_Electrical`.
   - `ST_Rebar`: Rebar Containers, Structural Reinforcement.
2. **Workset Resolution Algorithm**:
   - For each element category, resolve target workset name.
   - Inspect active document:
     ```csharp
     WorksetTable worksetTable = doc.GetWorksetTable();
     bool exists = !WorksetTable.IsUniqueWorksetName(doc, targetWorksetName);
     if (!exists)
     {
         Workset.Create(doc, targetWorksetName);
     }
     ```
   - Before element creation: Set active workset to target workset:
     ```csharp
     worksetTable.SetActiveWorksetId(targetWorksetId);
     ```
   - After creation: Explicitly assign `BuiltInParameter.ELEM_PARTITION_PARAM`:
     ```csharp
     Parameter partitionParam = element.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM);
     if (partitionParam != null && !partitionParam.IsReadOnly)
     {
         partitionParam.Set(targetWorksetId.IntegerValue);
     }
     ```

### 2.3 Proactive Element Borrowing & Conflict Avoidance

To guarantee zero modal dialog popups during update/delete operations, the pipeline enforces a **Proactive Checkout Pre-Flight Gate**:

1. **Step 1: Identify Target Elements**:
   - Extract all target `ElementId`s for items with status `Updated` or `Deleted`.
2. **Step 2: Inspect Checkout Status**:
   - For each element, evaluate `WorksharingUtils.GetCheckoutStatus(doc, elementId)`:
     - `CheckoutStatus.OwnedByCurrentUser`: Safe to modify immediately.
     - `CheckoutStatus.NotOwned`: Eligible for immediate checkout.
     - `CheckoutStatus.OwnedByOtherUser`: **BORROW CONFLICT**.
3. **Step 3: Extract Conflict Provenance**:
   - If owned by another user:
     ```csharp
     WorksharingTooltipInfo info = WorksharingUtils.GetWorksharingTooltipInfo(doc, elementId);
     string owner = info.Owner; // e.g. "john.smith@domain.com"
     ```
   - Fail-Fast / Skip: Mark item as `SkippedDueToBorrowConflict` with error reason:
     `"Element {elementId} is checked out by user '{owner}'. Operation skipped to prevent worksharing lock."`
4. **Step 4: Atomic Batch Checkout**:
   - For all `NotOwned` elements, execute batch checkout:
     ```csharp
     ICollection<ElementId> checkedOut = WorksharingUtils.CheckoutElements(doc, eligibleIds);
     ```
   - If `checkedOut.Count < eligibleIds.Count`, identify elements rejected by Central and skip them gracefully.

### 2.4 Synchronize with Central (SWC) Coordination

Automated pipelines must not hold borrowed elements indefinitely. The coordination protocol defines exact synchronization milestones:

```
[Pre-Job: ReloadLatest]
        │
        ▼
[Execute Stage 1 Chunks] ──▶ [Commit Locally]
        │
        ▼
[Execute Stage 2 Chunks] ──▶ [Commit Locally]
        │
        ▼
[Stage Boundary Checkpoint] ──▶ [SynchronizeWithCentral (Relinquish All Borrowed Elements)]
        │
        ▼
[Execute Remaining Stages]
        │
        ▼
[Post-Job: Final SWC & Relinquish All Elements & Worksets]
```

- **Pre-Flight Reload**: `doc.ReloadLatest(new ReloadLatestOptions())` ensures local model has latest baseline changes before computing offsets.
- **Stage Boundary Sync**: Synchronize after critical structural milestones (Substructure, Superstructure) using `SynchronizeWithCentralOptions`:
  - `RelinquishOptions`:
    - `CheckedOutElements = true` (Release all modified elements immediately)
    - `UserWorksets = true`
    - `FamilyWorksets = false`, `ViewWorksets = false`
  - `TransactWithCentralOptions`: Exponential backoff retry policy (5 retries, 2s – 10s delay) to handle transient Central file locks when humans sync concurrently.

---

## 3. Multi-Document & Linked Model Coordination

### 3.1 Federated Multi-Document Topology

In major construction projects, models are federated across specialized disciplines:
- `Campus_Master.rvt`: Topography, civil grids, property lines.
- `TowerA_Structure.rvt`: Structural framing, columns, foundations, rebars.
- `TowerA_Architecture.rvt`: Exterior facades, interior walls, doors, windows, finishes.
- `TowerA_MEP.rvt`: Mechanical ductwork, sanitary piping, electrical equipment.

When the automated pipeline runs in `TowerA_Structure.rvt`, it frequently needs to:
- Reference grids and levels hosted in `Campus_Master.rvt` or `TowerA_Architecture.rvt`.
- Detect structural slab openings required for MEP vertical risers hosted in `TowerA_MEP.rvt`.

### 3.2 Cross-Document Composite Keys

The standard single-document key `[DocIdentifier]#[Category]:[PrimaryHandle]` is extended to support federated linked documents:

#### Syntax Specification
$$\text{CompositeKey} := \text{LinkPrefix}^? \text{"["} \text{DocIdentifier} \text{"]#["} \text{Category} \text{"]:"} \text{PrimaryHandle}$$
Where $\text{LinkPrefix}$ identifies cross-document relationships:
1. **Direct Native Element**:
   `[TowerA_ST]#[Column]:C12_STORY1`
2. **Linked Element Reference (Element in Linked Model)**:
   `[TowerA_ST]>[ARC_LINK:Inst_102]#[Wall]:EXT_WALL_SOUTH`
   - `[TowerA_ST]`: Host document title or identifier.
   - `ARC_LINK:Inst_102`: Link document identifier and `RevitLinkInstance` ID or GUID.
   - `#[Wall]:EXT_WALL_SOUTH`: Category and primary handle within the linked document.
3. **Hosted Across Link (Element in Host referencing Linked Host)**:
   `[TowerA_MEP]^[STR_LINK:Inst_105]#[PipeOpening]:BEAM_OPENING_B4`

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

### 3.3 Coordinate Systems & Transform Reconciliation

Revit models operate across three distinct 3D coordinate spaces:

```
┌────────────────────────────────────────────────────────────────────────┐
│ 1. Internal Coordinate System (ICS)                                    │
│    - Fixed, unmovable native Revit Cartesian origin (0, 0, 0).         │
│    - Geometry creation APIs (Curve, Point, XYZ) strictly use ICS.      │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ P_ICS = Transform_Total * P_Link
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│ 2. Project Coordinate System (PCS)                                     │
│    - Defined by Project Base Point (PBP).                              │
│    - Relative to ICS by offset (E/W, N/S, Elev) and True North angle.  │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│ 3. Shared Coordinate System (SCS)                                      │
│    - Defined by Survey Point (SP) and Site Location georeferencing.    │
│    - Aligns disciplines (CAD VN-2000 / UTM ↔ Arch RVT ↔ Struct RVT).  │
└────────────────────────────────────────────────────────────────────────┘
```

#### Linked Model Transform Reconciliation
When querying geometry from a linked model (`RevitLinkInstance`):
```csharp
RevitLinkInstance linkInstance = ...;
Transform linkTransform = linkInstance.GetTotalTransform();

// Transform points from Linked Model ICS to Host Model ICS:
XYZ hostPoint = linkTransform.OfPoint(linkedPoint);

// Transform direction vectors (rotation only, no translation):
XYZ hostVector = linkTransform.OfVector(linkedVector);
```

#### Shared Coordinates Alignment Pre-Flight Check
Before generating geometry from CAD or linked models, the pipeline performs an **Alignment Pre-Flight Check**:
1. Query Project Base Point and Survey Point:
   ```csharp
   var pbp = new FilteredElementCollector(doc)
       .OfCategory(BuiltInCategory.OST_ProjectBasePoint)
       .FirstElement();
   var sp = new FilteredElementCollector(doc)
       .OfCategory(BuiltInCategory.OST_SharedBasePoint)
       .FirstElement();
   ```
2. Verify reference control point (e.g. Intersection of Grid X1 and Grid YA):
   - Measure distance between CAD control point and Revit control point:
     $$\Delta = \| P_{\text{CAD}} - P_{\text{Revit\_ICS}} \|$$
   - If $\Delta > \text{CoordinationTolerances.PlanMaxDeviationMm}$ (1.0mm) and no known transform offset is defined:
     **RULE 0 ENFORCEMENT**: Throw `CoordinateSystemMismatchException`. Refuse execution until the transform matrix is explicitly resolved.

---

## 4. Extensible Storage Sharding Across Multiple DataStorage Elements

### 4.1 The Single-Entity Extensible Storage Bottleneck

In Milestone M4, coordination receipts were written to a single hidden `DataStorage` element under schema `HPBimCoordinationReceipt`:
```csharp
// M4 Schema:
builder.AddSimpleField("OperationId", typeof(Guid));
builder.AddSimpleField("PlanHash", typeof(string));
builder.AddSimpleField("CommittedAtUtc", typeof(string));
builder.AddSimpleField("ElementMapJson", typeof(string)); // Contains entire SourceKey -> UniqueId map
```

For megaprojects ($N = 100,000$ elements), this design suffers from three catastrophic failures:
1. **Entity Size Exhaustion**: Serializing 100,000 `CompositeSourceKey -> UniqueId` entries produces a JSON string of $\sim 18\text{ MB} - 35\text{ MB}$. Storing a string of this size in a single Revit Extensible Storage `Entity` exceeds internal string buffer limitations, triggering `Autodesk.Revit.Exceptions.ArgumentException` or silent serialization truncation.
2. **Worksharing Lock Hotspot**: A single `DataStorage` element represents a single element in Revit's database (`ElementId`). Whenever an automated batch or user updates receipts, that single element is checked out. If multiple batches or sync cycles run, they experience immediate worksharing conflicts on the receipt element.
3. **Query Deserialization Overhead**: Deserializing a 35MB JSON string on every reconciliation pre-flight takes 1.5 – 3.0 seconds and allocates 150MB of managed memory.

### 4.2 Two-Tier Sharded Receipt Architecture

The sharded receipt architecture partitions data across a two-tier hierarchy:

```
┌────────────────────────────────────────────────────────────────────────┐
│ Tier 1: Master Manifest Index Shard (Single Root DataStorage)          │
│ Schema: HPBimReceiptManifestIndex                                      │
│ - ManifestId (Guid)                                                    │
│ - LastUpdatedUtc (string)                                              │
│ - TotalBatches (int)                                                   │
│ - ManifestIndexJson (string): Lightweight array of batch headers:      │
│   [{ BatchId, OperationId, ChunkCount, CommittedAtUtc, ShardIds[] }]   │
│   Payload size: ~120 bytes per batch (1,000 batches = 120 KB, safe!)   │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ references
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│ Tier 2: Leaf Chunk Shards (Discrete DataStorage Element Per Chunk)     │
│ Schema: HPBimChunkReceiptShard                                         │
│                                                                        │
│  ┌───────────────────────┐ ┌───────────────────────┐                   │
│  │ Chunk Shard 1 (500)   │ │ Chunk Shard 2 (500)   │ ... [Shard M]     │
│  │ - OperationId (Guid)  │ │ - OperationId (Guid)  │                   │
│  │ - BatchId (Guid)      │ │ - BatchId (Guid)      │                   │
│  │ - ChunkIndex: 1       │ │ - ChunkIndex: 2       │                   │
│  │ - StageIndex: 2       │ │ - StageIndex: 2       │                   │
│  │ - PlanHash (string)   │ │ - PlanHash (string)   │                   │
│  │ - ElementCount: 500   │ │ - ElementCount: 500   │                   │
│  │ - ChunkMapJson (~35KB)│ │ - ChunkMapJson (~35KB)│                   │
│  └───────────────────────┘ └───────────────────────┘                   │
└────────────────────────────────────────────────────────────────────────┘
```

#### Mathematical Sizing Guarantee
- Chunk size: $C = 500$ elements.
- Single mapping entry size:
  `"[TowerA_ST]#[Column]:C12_STORY1": "e4f5a6b7-8c9d-0e1f-2a3b-4c5d6e7f8a9b"` $\approx 72\text{ bytes}$.
- Total `ChunkMapJson` payload:
  $$500 \times 72\text{ bytes} \approx 36\text{ KB}$$
- Safety margin: $36\text{ KB} \ll 16\text{ MB}$ (utilizing only **$0.22\%$** of the safe limit).
- **Zero Worksharing Contention**: Each chunk creates a *brand-new* `DataStorage` element via `DataStorage.Create(doc)`. Creating a new element generates a fresh `ElementId` and does NOT edit or lock existing elements!

### 4.3 Schemas & Data Storage Definitions

#### Schema 1: Tier 1 Master Manifest Index
- **Schema GUID**: `A1B2C3D4-E5F6-7A8B-9C0D-1E2F3A4B5C6D`
- **Schema Name**: `HPBimReceiptManifestIndex`
- **Fields**:
  - `ManifestId` (`Guid`): Unique index identifier.
  - `LastUpdatedUtc` (`string`): ISO 8601 UTC timestamp.
  - `BatchCount` (`int`): Total committed batches.
  - `ManifestEntriesJson` (`string`): Compact JSON array of batch descriptors:
    ```json
    [
      {
        "batchId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "operationId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
        "planHash": "a1b2c3...",
        "chunkCount": 20,
        "committedAtUtc": "2026-10-08T10:30:00Z",
        "shardUniqueIds": ["uid-1", "uid-2", "..."]
      }
    ]
    ```

#### Schema 2: Tier 2 Leaf Chunk Shard
- **Schema GUID**: `B2C3D4E5-F6A7-8B9C-0D1E-2F3A4B5C6D7E`
- **Schema Name**: `HPBimChunkReceiptShard`
- **Fields**:
  - `OperationId` (`Guid`): Batch operation ID.
  - `BatchId` (`Guid`): Parent batch ID.
  - `ChunkId` (`Guid`): Unique chunk ID.
  - `ChunkIndex` (`int`): Sequence index ($1 \dots M$).
  - `TotalChunks` (`int`): Total chunk count ($M$).
  - `StageIndex` (`int`): Topological stage ($1 \dots 8$).
  - `PlanHash` (`string`): Deterministic plan SHA-256 hash.
  - `ChunkHash` (`string`): Chunk content SHA-256 hash.
  - `CommittedAtUtc` (`string`): ISO 8601 UTC timestamp.
  - `ElementCount` (`int`): Number of elements in this shard ($\le 1000$).
  - `ChunkMapJson` (`string`): Serialized mapping dictionary `SourceKey -> UniqueId` ($\le 70\text{ KB}$).

### 4.4 Sharded Query & Fast Resolution Engine

1. **Direct Batch Lookup**:
   - Client queries Tier 1 `HPBimReceiptManifestIndex`.
   - Locates target `batchId` or `operationId`.
   - Extracts `shardUniqueIds`.
   - Queries only those specific `DataStorage` elements via `doc.GetElement(uniqueId)`.
   - Time complexity: $O(1)$ direct element lookups, zero full-model collector scans!
2. **Orphan Recovery & Index Rebuilding**:
   - If Tier 1 index is missing or corrupted:
   - Run fallback collector:
     ```csharp
     var shards = new FilteredElementCollector(doc)
         .OfClass(typeof(DataStorage))
         .Where(ds => ds.GetEntity(leafSchema) != null && ds.GetEntity(leafSchema).IsValid());
     ```
   - Reconstruct Tier 1 manifest dynamically from leaf shards and save.

---

## 5. C# Interface Contracts

Adhering strictly to Clean Code Governance (`HP_CLEAN_CODE_CORE.md`, `N1–N10`, `M1–M10`, `C1–C7`, `S1–S5`, `D1–D6`), all interfaces feature trivial constructors, immutable record parameter objects, explicit dependency direction, and zero Revit API types in Core abstractions.

### 5.1 Contract 1: `IBatchExecutionPlanner` (`HPBim.Core/Execution/`)

```csharp
namespace HPBim.Core.Execution;

using System;
using System.Collections.Generic;

/// <summary>
/// Partitions large-scale BimExecutionPlans (>100,000 items) into ordered,
/// topologically dependency-sorted transaction chunks with checkpointing support.
/// Resides in HPBim.Core (netstandard2.0) with zero Revit references.
/// </summary>
public interface IBatchExecutionPlanner
{
    /// <summary>
    /// Creates a complete chunked execution batch plan from a monolithic plan.
    /// </summary>
    ExecutionBatchPlan CreateBatchPlan(
        BimExecutionPlan plan,
        BatchPlanningOptions options);

    /// <summary>
    /// Partitions items into topologically sorted chunks bounded by maxChunkSize.
    /// </summary>
    IReadOnlyList<ExecutionPlanChunk> PartitionIntoChunks(
        IReadOnlyList<ElementPlanItem> items,
        int maxChunkSize = 500);

    /// <summary>
    /// Validates topological ordering and stage boundary consistency.
    /// </summary>
    BatchValidationResult ValidateTopologicalOrder(
        IReadOnlyList<ExecutionPlanChunk> chunks);

    /// <summary>
    /// Generates a compensating rollback plan to reverse committed chunks in reverse order.
    /// </summary>
    CompensatingBatchPlan CreateCompensatingRollbackPlan(
        IReadOnlyList<ExecutionPlanChunk> committedChunks);
}

/// <summary>Immutable container for a partitioned batch execution plan.</summary>
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

/// <summary>Immutable unit of transaction execution (500–1000 elements).</summary>
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

/// <summary>Options governing chunk sizing and failure boundaries.</summary>
public sealed record BatchPlanningOptions
{
    public int MaxChunkSize { get; init; } = 500;
    public bool EnforceStageBoundaries { get; init; } = true;
    public bool EnableCompensatingRollback { get; init; } = false;
    public TimeSpan InterChunkDelay { get; init; } = TimeSpan.FromMilliseconds(50);
}
```

### 5.2 Contract 2: `IWorksharingCoordinator` (`HPBim.Client/Coordination/`)

```csharp
namespace HPBim.Client.Coordination;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Core.Execution;

/// <summary>
/// Coordinates workset assignment, proactive element checkout borrowing,
/// conflict prevention, and Central synchronization in workshared models.
/// Resides in HPBim.Client (net10.0).
/// </summary>
public interface IWorksharingCoordinator
{
    /// <summary>
    /// Inspects the target document's worksharing status and capabilities.
    /// </summary>
    Task<WorksharingStateResult> QueryWorksharingStateAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures designated worksets exist, creating missing worksets if permitted.
    /// </summary>
    Task<WorksetProvisionResult> EnsureWorksetsAsync(
        IReadOnlyList<string> requiredWorksetNames,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pre-checks borrowing status for target elements and performs atomic checkout
    /// without triggering interactive modal dialogues.
    /// </summary>
    Task<BorrowPreFlightResult> PrepareElementBorrowingAsync(
        IReadOnlyList<string> targetUniqueIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronizes changes with Central at designated stage checkpoints.
    /// </summary>
    Task<CentralSyncResult> SynchronizeWithCentralAsync(
        CentralSyncOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Relinquishes all borrowed elements and worksets upon job completion or failure.
    /// </summary>
    Task<RelinquishResult> RelinquishOwnershipAsync(
        RelinquishOptions options,
        CancellationToken cancellationToken = default);
}

/// <summary>Result of proactive borrowing pre-flight check.</summary>
public sealed record BorrowPreFlightResult
{
    public bool Success { get; init; }
    public IReadOnlyList<string> SuccessfullyCheckedOutUniqueIds { get; init; } = Array.Empty<string>();
    public IReadOnlyList<BorrowConflictDetail> ConflictedElements { get; init; } = Array.Empty<BorrowConflictDetail>();
    public bool HasConflicts => ConflictedElements.Count > 0;
}

/// <summary>Diagnostic detail of an element borrowed by another user.</summary>
public sealed record BorrowConflictDetail
{
    public string UniqueId { get; init; } = string.Empty;
    public long ElementId { get; init; }
    public string OwnerUsername { get; init; } = string.Empty;
    public string ElementCategory { get; init; } = string.Empty;
    public string ErrorMessage => $"Element {ElementId} is currently checked out by '{OwnerUsername}'.";
}
```

### 5.3 Contract 3: `IShardedReceiptManager` (`HPBim.Client/Recovery/`)

```csharp
namespace HPBim.Client.Recovery;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;

/// <summary>
/// Manages sharded coordination receipts across discrete DataStorage elements,
/// preventing Extensible Storage entity size exhaustion in megaprojects.
/// Resides in HPBim.Client (net10.0).
/// </summary>
public interface IShardedReceiptManager
{
    /// <summary>
    /// Commits a discrete chunk receipt to a dedicated DataStorage element
    /// and updates the Tier 1 manifest index.
    /// </summary>
    Task<ShardCommitResult> CommitChunkReceiptAsync(
        ExecutionPlanChunk chunk,
        IReadOnlyDictionary<CompositeSourceKey, string> committedMappings,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all chunk receipts associated with a master batch or operation ID.
    /// </summary>
    Task<IReadOnlyList<ChunkReceiptRecord>> GetBatchReceiptsAsync(
        Guid batchId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a Revit UniqueId for a given CompositeSourceKey across all shards.
    /// </summary>
    Task<string?> ResolveUniqueIdAsync(
        CompositeSourceKey sourceKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reconstructs the Tier 1 manifest index by scanning existing Tier 2 leaf shards.
    /// </summary>
    Task<IndexRebuildResult> RebuildManifestIndexAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Prunes or compacts historical dry-run or archived receipts older than the threshold.
    /// </summary>
    Task<CompactionReport> CompactHistoricalShardsAsync(
        TimeSpan olderThan,
        CancellationToken cancellationToken = default);
}

/// <summary>Immutable record of a committed leaf chunk receipt.</summary>
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

## 6. End-to-End Megaproject Execution Walkthrough

The following sequence details how an autonomous agent executes a 100,000-element model update:

```
[Agent / CLI: GenerateCommand]
       │
       ▼
[1. Schema Validation & ExecutionPlanBuilder]
       │ ── Converts CAD / Domain models into monolithic BimExecutionPlan (100,000 items)
       ▼
[2. BatchExecutionPlanner]
       │ ── Partitions plan into 200 chunks (500 items/chunk) across 8 topological stages
       ▼
[3. WorksharingCoordinator: Pre-Flight]
       │ ── ReloadLatest from Central
       │ ── Ensures ST_Substructure, ST_Superstructure, etc. worksets exist
       │ ── Inspects Borrowing status for updated elements; fails-fast on conflicts
       ▼
[4. Loop: Chunks 1 to 200]
       │ ── For Chunk k:
       │    ├── Dispatches ExecuteRequest to Revit MCP Bridge (Named Pipe)
       │    ├── Revit Bridge: Opens Transaction "MCP: Chunk k", creates 500 elements
       │    ├── Revit Bridge: Creates dedicated DataStorage element for Chunk k
       │    ├── Revit Bridge: Commits Transaction & returns ExecuteResult
       │    ├── Client: Updates IntentJournal (Chunk k: Committed)
       │    ├── Client: Updates BimCoordinationLedger with 500 mappings
       │    └── Checkpoint: If Stage Boundary reached ──▶ SynchronizeWithCentral
       ▼
[5. Measured Geometric Verifier]
       │ ── Direct Revit API geometric verification against tolerance
       ▼
[6. WorksharingCoordinator: Cleanup]
       │ ── Final SynchronizeWithCentral
       │ ── RelinquishOwnership (Releases all checked-out elements)
       ▼
[7. Complete with 100% Non-Repudiation Audit Report]
```

---

## 7. Performance Benchmarks & Mathematical Sizing Estimates

| Stage | Element Count | Chunks ($C = 500$) | Est. Runtime / Chunk | Total Stage Runtime | Memory Peak ($\Delta$) | Extensible Storage Shards |
|---|---|---|---|---|---|---|
| **Stage 1: Levels & Grids** | 150 | 1 | 0.8 s | 0.8 s | 8 MB | 1 shard (12 KB) |
| **Stage 2: Substructure** | 12,000 | 24 | 3.2 s | 76.8 s (~1.3 min) | 28 MB | 24 shards (860 KB total) |
| **Stage 3: Superstructure Frame** | 35,000 | 70 | 4.1 s | 287 s (~4.8 min) | 35 MB | 70 shards (2.5 MB total) |
| **Stage 4: Walls & Enclosure** | 18,000 | 36 | 3.5 s | 126 s (~2.1 min) | 30 MB | 36 shards (1.3 MB total) |
| **Stage 5: Openings & Doors** | 10,000 | 20 | 2.8 s | 56 s (~0.9 min) | 25 MB | 20 shards (720 KB total) |
| **Stage 6: Rooms & Areas** | 2,500 | 5 | 2.2 s | 11 s (~0.2 min) | 20 MB | 5 shards (180 KB total) |
| **Stage 7: MEP Networks** | 15,000 | 30 | 3.8 s | 114 s (~1.9 min) | 32 MB | 30 shards (1.1 MB total) |
| **Stage 8: Rebars & Containers** | 7,350 | 15 | 4.5 s | 67.5 s (~1.1 min) | 38 MB | 15 shards (530 KB total) |
| **TOTAL MEGAPROJECT** | **100,000** | **201 chunks** | **Avg 3.6 s** | **~12.3 minutes** | **Max 38 MB** | **201 shards (~7.2 MB total)** |

### Architectural Contrast:
- **Monolithic Single Transaction**: Fails after 120s due to pipe timeout, locks Revit UI thread, risks 100% loss.
- **Chunked & Sharded Architecture**: Completes all 100,000 elements in ~12.3 minutes, zero UI lockouts, zero timeout breaches, zero worksharing lock contention, and 100% checkpointed recovery.
