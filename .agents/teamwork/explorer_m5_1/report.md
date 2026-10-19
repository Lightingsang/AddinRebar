# Architectural Proposal: Bidirectional Change Tracking & Dynamic Model Update (DMU)

**Milestone**: M5 (Feature F11 Part 1)  
**Author**: `explorer_m5_1` (teamwork_preview_explorer)  
**Date**: 2026-10-07  
**Scope**: In-Revit Dynamic Model Update (`IUpdater`), Ledger Synchronization, Provenance Integrity, Tombstoning, 3-Way Merge Conflict Resolution, and Interface Contracts.  
**Governing Standard**: HP Clean Code Core (`HP_CLEAN_CODE_CORE.md`), Revit Add-in Standard (`REVITADDINAI_CLEAN_CODE_STANDARD.md`), Rule 0 ("Nguyên Tắc Vàng").

---

## 1. Executive Summary

In a production BIM environment, building models are living artifacts. While the autonomous HPBim pipeline generates native structural and architectural elements from verified CAD engineering data, human modelers simultaneously interact with the Revit model: tweaking column dimensions for structural calculations, relocating walls for architectural clearances, or deleting redundant elements.

Without bidirectional change tracking:
1. **The Zombie Element Problem**: When a human modeler deletes an element in Revit, subsequent runs of the automated generator fail to detect the deletion intent and blindly re-instantiate the element from CAD source data.
2. **The Silent Overwrite Catastrophe**: If an automated sync blindly forces CAD dimensions onto an existing Revit model, manual engineering refinements (e.g., resizing a column from $220 \times 220$ to $220 \times 300$ for punching shear) are silently obliterated.
3. **The Blind Modeler Hazard**: If Revit modelers make changes, CAD engineers have zero feedback regarding deviations until construction site clashes occur.

This proposal establishes a rock-solid, zero-guesswork architecture for **Bidirectional Change Tracking & Dynamic Model Update (DMU)** in Autodesk Revit 2026. It combines:
- A native in-process Revit `IUpdater` (`BimDynamicModelUpdater`) capturing modifications in real-time within the active transaction.
- An intelligent **Tombstoning Engine** inside `BimCoordinationLedger` preventing zombie element re-creation while preserving full audit history.
- A deterministic **3-Way Merge Engine** adhering strictly to **Rule 0 ("Nguyên Tắc Vàng")**: zero silent overwrites, explicit tolerance checking, and automated generation of formal Request for Information (RFI) records whenever unresolvable deviations occur.
- Strongly-typed C# contracts (`IRevitChangeTracker`, `IConflictResolutionPolicy`, `ModelChangeEvent`) designed for clean host-neutral testing in `HPBim.Core` and robust execution in `HPBim.Client` / `HPRebar.McpBridge`.

---

## 2. Process Topology & Architectural Topology

```
┌──────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                 AUTODESK REVIT 2026 (UI THREAD)                                  │
│                                                                                                  │
│   ┌────────────────────────────────┐                 ┌───────────────────────────────────────┐   │
│   │   Interactive Modeler Action   │                 │     Automated Pipeline Sync Script    │   │
│   │   (Move, Resize, Delete, Type) │                 │     (Named Pipe: hprebar-mcp-r2026)   │   │
│   └───────────────┬────────────────┘                 └───────────────────┬───────────────────┘   │
│                   │ Active Transaction                                   │ Active Transaction    │
│                   │                                                      │ (Suppressed Context)  │
│                   ▼                                                      ▼                       │
│   ┌──────────────────────────────────────────────────────────────────────────────────────────┐   │
│   │                          BimDynamicModelUpdater (implements IUpdater)                    │   │
│   │  - Updaters: AddedElementIds, ModifiedElementIds, DeletedElementIds                      │   │
│   │  - RecursionGuard: Prevents re-entrant trigger cascades                                  │   │
│   │  - Context Filter: Ignores pipeline writes via BimTrackingContext.CurrentOrigin          │   │
│   │  - Fast Lookup: ElementId (long) <-> UniqueId (string) <-> CompositeSourceKey             │   │
│   └───────────────────────────────────────────┬──────────────────────────────────────────────┘   │
│                                               │ Emits ModelChangeEvent                           │
│                                               ▼                                                  │
│   ┌──────────────────────────────────────────────────────────────────────────────────────────┐   │
│   │                     RevitChangeJournal & Extensible Storage Buffer                       │   │
│   │  - Schema GUID: D4E5F6A7-8B9C-0D1E-2F3A-4B5C6D7E8F90 (Coordination Receipt)              │   │
│   │  - Real-time staging of deviations in hidden DataStorage element                         │   │
│   └───────────────────────────────────────────┬──────────────────────────────────────────────┘   │
└───────────────────────────────────────────────┼──────────────────────────────────────────────────┘
                                                │ IPC / Named Pipe Query
                                                ▼
┌──────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                    HPBim.Client & HPBim.Cli                                      │
│                                                                                                  │
│   ┌──────────────────────────────────────────────────────────────────────────────────────────┐   │
│   │                           ReconcileService / IRevitChangeTracker                         │   │
│   │  - Polls or flushes ModelChangeEvents from Revit Extensible Storage                      │   │
│   │  - Reconstructs 3-way merge context (Base vs CAD Ground Truth vs Revit Model)            │   │
│   └───────────────────────────────────────────┬──────────────────────────────────────────────┘   │
│                                               │ Evaluates                                        │
│                                               ▼                                                  │
│   ┌──────────────────────────────────────────────────────────────────────────────────────────┐   │
│   │                      3-Way Merge Engine & IConflictResolutionPolicy                      │   │
│   │  - Tolerance Gate: SectionDimension (0.0mm), Elevation (1.0mm), PlanPosition (1.0mm)     │   │
│   │  - Rule 0 Gate: If delta > tolerance, HALT automatic write and generate RFI             │   │
│   │  - Resolution Decision: NoOp | ApplyCad | PreserveRevit | RaiseRfiAndHalt                │   │
│   └───────────────────────────────────────────┬──────────────────────────────────────────────┘   │
│                                               │ Commits state                                    │
│                                               ▼                                                  │
│   ┌──────────────────────────────────────────────────────────────────────────────────────────┐   │
│   │                       BimCoordinationLedger (Pure C# - HPBim.Core)                       │   │
│   │  - Mappings: CompositeSourceKey -> LedgerElementMapping (Updated / Tombstoned)           │   │
│   │  - Rfis: Append formal RfiRecord for chief engineer review                               │   │
│   │  - Audits: Records measured delta in RevitTargetSpec & AuditRecord                       │   │
│   └──────────────────────────────────────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Revit DMU & IUpdater Architecture

### 3.1 Lifecycle & Registration Strategy
In Autodesk Revit, `IUpdater` is the only mechanism permitted to react to model modifications **within the same transaction** that introduced the change. This guarantees atomic consistency: if a modeler's change causes an invalid geometric state or violates a frozen zone policy, the updater can modify parameters or cancel the transaction before commit.

1. **Updater Identity & Modality**:
   - Class `BimDynamicModelUpdater` implements `Autodesk.Revit.DB.IUpdater`.
   - `UpdaterId`: Composed of `AddInId` (from `HPRebar.addin`) and a unique permanent GUID:
     ```csharp
     public static readonly Guid UpdaterGuid = new("B7C48D21-39F2-4E15-9A82-5D1A041E67C9");
     ```
   - **Optional Registration (`isOptional: true`)**:
     `UpdaterRegistry.RegisterUpdater(updater, isOptional: true)` is strictly mandatory. If registered with `isOptional: false`, opening the `.rvt` project file on a workstation without the HPRebar add-in installed will prompt an intrusive blocking dialog: *"Third party updater is missing"*. Setting `isOptional: true` allows collaborative team members without the add-in to continue working without Revit blocking file access.
   - `GetChangePriority()`: Returns `ChangePriority.Structure` for structural elements and `ChangePriority.Views` for annotation updates.

2. **Trigger Configuration & Filter Optimization**:
   To minimize CPU overhead on the Revit UI thread during intensive user operations, triggers must be scoped via an optimized `ElementMulticategoryFilter`:
   - Categories tracked:
     - `BuiltInCategory.OST_StructuralColumns`
     - `BuiltInCategory.OST_StructuralFraming`
     - `BuiltInCategory.OST_StructuralFoundation`
     - `BuiltInCategory.OST_Walls`
     - `BuiltInCategory.OST_Floors`
     - `BuiltInCategory.OST_Grids`
     - `BuiltInCategory.OST_Levels`
   - Three dedicated triggers registered via `UpdaterRegistry.AddTrigger`:
     1. **Addition Trigger**: `Element.GetChangeTypeElementAddition()`
     2. **Deletion Trigger**: `Element.GetChangeTypeElementDeletion()`
     3. **Modification Trigger**: `Element.GetChangeTypeGeometry()` plus explicit parameter watches:
        - `BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM` (Type changes)
        - `BuiltInParameter.ALL_MODEL_MARK` (Mark changes)
        - `BuiltInParameter.FAMILY_BASE_LEVEL_PARAM` / `FAMILY_TOP_LEVEL_PARAM` (Level changes)
        - `BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM` (Elevation offsets)

### 3.2 In-Transaction Execution Semantics
During `IUpdater.Execute(UpdaterData data)`:
- `data.GetDocument()` provides the active `Document`.
- **Added Elements (`data.GetAddedElementIds()`)**:
  - The updater queries the element. If created by the automated pipeline (checked via `BimTrackingContext.CurrentOrigin == ChangeOrigin.PipelineAutomation`), it registers the element in the runtime `IdLookupTable`.
  - If created manually by a user, it generates a `ModelChangeEvent` with `ModelChangeType.Added` and origin `ChangeOrigin.UserInteractive`.
- **Modified Elements (`data.GetModifiedElementIds()`)**:
  - For each element in the lookup table, the updater queries its current parameters and bounding box/location.
  - It compares current metrics with the baseline stored in the element's Extensible Storage or ledger mapping.
  - If deltas exceed `CoordinationTolerances`, a `ModelChangeEvent` is queued.
- **Deleted Elements (`data.GetDeletedElementIds()`) — The Disappearing Element Caveat**:
  - **Revit API Constraint**: When an element is deleted, `doc.GetElement(id)` returns `null` inside `Execute()`. You cannot inspect its parameters, its `UniqueId`, or its geometry!
  - **Solution**: The tracker maintains an in-memory dual map `RuntimeElementRegistry`:
    ```csharp
    // Bi-directional lookup maintained in memory and populated on DocumentOpen / Receipt sync:
    Dictionary<long, TrackedElementMetadata> _elementIdToMetadata;
    Dictionary<string, long> _uniqueIdToElementId;
    ```
    When `id` appears in `data.GetDeletedElementIds()`, the tracker immediately retrieves `TrackedElementMetadata` (which contains `UniqueId`, `CompositeSourceKey`, and category) before Revit permanently purges the element from memory!

### 3.3 Recursion & Reentrancy Guards
Revit dynamic updaters can easily cause infinite loops if the updater writes to an element, which re-triggers the updater, which writes again.
Furthermore, when the automated HPBim pipeline runs a batch script via Named Pipe, we must not record those automated modifications as user-induced conflicts.

We establish a two-level guard:
1. **Thread-Local Reentrancy Guard (`RecursionGuard`)**:
   ```csharp
   public sealed class RecursionGuard : IDisposable
   {
       [ThreadStatic]
       private static bool _isExecuting;

       public static bool IsExecuting => _isExecuting;

       public static IDisposable? Enter()
       {
           if (_isExecuting) return null;
           _isExecuting = true;
           return new RecursionScope();
       }

       private sealed class RecursionScope : IDisposable
       {
           public void Dispose() => _isExecuting = false;
       }
   }
   ```
2. **Context-Aware Suppression (`BimTrackingContext`)**:
   When `HPBim.Client` dispatches an automated execution plan through `BimPipelineEngine`, the execution script sets:
   ```csharp
   using (BimTrackingContext.SuppressTracking(ChangeOrigin.PipelineAutomation))
   {
       // Execute transaction batch...
   }
   ```
   Inside `IUpdater.Execute()`, the updater checks:
   ```csharp
   if (BimTrackingContext.CurrentOrigin == ChangeOrigin.PipelineAutomation)
   {
       // Bypass tracking: This is an intentional automated write from HPBim
       return;
   }
   ```
3. **Execution Order Sequencing**:
   If third-party structural updaters (or Revit's internal join/cut updaters) are active, we call:
   ```csharp
   UpdaterRegistry.SetExecutionOrder(hpbimUpdaterId, otherUpdaterId);
   ```
   ensuring the change tracking updater evaluates the final, solved geometry after structural joins have settled.

---

## 4. Ledger Synchronization & Provenance Integrity

### 4.1 Ledger Data Model Enhancements
To support bidirectional synchronization, `BimCoordinationLedger` and `LedgerElementMapping` in `HPBim.Core` are augmented with provenance tracking, deviation status, and tombstone metadata:

```csharp
/// <summary>
/// Status of an element relative to the CAD-BIM coordination baseline.
/// </summary>
public enum ElementDeviationState
{
    Synchronized = 1,           // Revit identical to CAD ground-truth
    PendingUserModification = 2, // Modeler edited parameters/geometry in Revit
    Tombstoned = 3,             // Deleted by user in Revit; preserved in ledger
    Conflicted = 4              // CAD and Revit have diverging changes (Rule 0)
}
```

In `LedgerElementMapping.cs`:
```csharp
public sealed record LedgerElementMapping
{
    public CompositeSourceKey SourceKey { get; init; } = CompositeSourceKey.Empty;
    public string TargetUniqueId { get; init; } = string.Empty;
    public long? TargetElementId { get; init; }
    public CoordinationCategory Category { get; init; }
    public string Mark { get; init; } = string.Empty;
    public string PlanHash { get; init; } = string.Empty;
    public IReadOnlyList<string> ProvenanceHandles { get; init; } = Array.Empty<string>();
    
    // Bidirectional Tracking Fields
    public ElementDeviationState DeviationState { get; init; } = ElementDeviationState.Synchronized;
    public bool IsTombstoned { get; init; } = false;
    public DateTime? TombstonedAtUtc { get; init; }
    public string? TombstoneReason { get; init; }
    public IReadOnlyList<ModelChangeEventRecord> ChangeHistory { get; init; } = Array.Empty<ModelChangeEventRecord>();
    
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime LastModifiedAtUtc { get; init; } = DateTime.UtcNow;
    public ElementExecutionStatus LastModifiedStatus { get; init; } = ElementExecutionStatus.Created;
}
```

### 4.2 Handling Manual Moves & Parameter Edits
When a modeler modifies an element in Revit:
1. `IUpdater.Execute()` captures the change and measures the new values:
   - Width $b_{\text{revit}}$, Height $h_{\text{revit}}$
   - Center Point $(X_{\text{revit}}, Y_{\text{revit}}, Z_{\text{revit}})$
   - Top Elevation $Z_{\text{top}}$
2. The values are checked against `CoordinationTolerances`:
   - Section Dimension: Tolerance = $0.0\text{ mm}$ (Strict: no implicit tolerance on structural cross-section).
   - Plan Position: Tolerance = $1.0\text{ mm}$.
   - Elevation: Tolerance = $1.0\text{ mm}$.
3. If $\Delta > \text{Tolerance}$:
   - The ledger mapping transitions `DeviationState = ElementDeviationState.PendingUserModification`.
   - The corresponding `CoordinationElementRecord` in `BimCoordinationLedger.Elements` has its `RevitTargetSpec` updated with the new measured metrics.
   - The element's `TechnicalGroup` switches to `ElementTechnicalGroup.Group2_ConflictingInfo` until reconciled.

### 4.3 Handling User Deletions & Tombstoning ("The Zombie Element Problem")
If a user deletes column `C1` (mapped to `[NhaPho_5x18m.dwg]#[Column]:5A3`), what happens when `hpbim generate` runs again?

#### The Flawed Approach (Hard Deletion from Ledger)
If the ledger removes the mapping upon deletion:
`_mappings.Remove(key)` $\implies$ Next run: `TryGetMapping(key)` is `false` $\implies$ `IdempotentElementResolver` concludes *"Not in ledger"* $\implies$ Issues `PlannedAction.Create` $\implies$ **Column C1 is re-created against user intent!**

#### The Robust Tombstone Engine
1. When deletion is detected by the updater:
   - The mapping is **retained**, but flagged:
     `IsTombstoned = true`, `TombstonedAtUtc = DateTime.UtcNow`, `TombstoneReason = "User interactive deletion in Revit"`, `TargetUniqueId = string.Empty`, `TargetElementId = null`.
2. When `IdempotentElementResolver.Resolve` evaluates a plan item whose key is tombstoned:
   - It checks `mapping.IsTombstoned`.
   - Instead of blindly creating or recreating, it delegates to `IConflictResolutionPolicy`.
   - Under **Rule 0 Policy** (Default):
     ```csharp
     return ElementResolutionDecision.Skipped(
         planItem.SourceKey,
         "Element is tombstoned in Revit by user. Re-generation blocked pending RFI approval.");
     ```
   - An explicit RFI is logged:
     `RFI: Element [NhaPho_5x18m.dwg]#[Column]:5A3 deleted in Revit model but exists in CAD ground truth. Clarify if structural column was intentionally removed or relocated.`
3. **Revoking a Tombstone**:
   If the Chief Engineer determines the deletion was accidental or invalid, the CLI command:
   ```bash
   hpbim reconcile --untombstone "[NhaPho_5x18m.dwg]#[Column]:5A3"
   ```
   clears `IsTombstoned = false`. On the next pipeline run, `IdempotentElementResolver` schedules `PlannedAction.Create`.

---

## 5. Bidirectional CAD/Revit Synchronization & 3-Way Merge Conflict Resolution

### 5.1 The 3-Way Merge Engine
When synchronizing changes between CAD and Revit, we evaluate three distinct states for each element:
1. **$S_{\text{Base}}$ (Baseline Ancestor)**: The snapshot state recorded at the last clean, verified synchronization (stored in `LedgerElementMapping` and Revit Extensible Storage).
2. **$S_{\text{CAD}}$ (Incoming CAD Spec)**: The latest state extracted from the CAD drawing.
3. **$S_{\text{RVT}}$ (Current Revit State)**: The live state in the active Revit document.

Let:
- $\Delta_{\text{CAD}} = (S_{\text{CAD}} \neq S_{\text{Base}})$: Has CAD changed since the last sync?
- $\Delta_{\text{RVT}} = (S_{\text{RVT}} \neq S_{\text{Base}})$: Has Revit changed since the last sync?

#### 3-Way Merge Decision Matrix

| Case | $\Delta_{\text{CAD}}$ | $\Delta_{\text{RVT}}$ | Relationship ($S_{\text{CAD}}$ vs $S_{\text{RVT}}$) | Action | Classification | Rule 0 Adherence |
| :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| **1** | False | False | $S_{\text{CAD}} = S_{\text{RVT}}$ | `NoOp` | Fully Synchronized | PASS (Zero action needed) |
| **2** | True | False | $S_{\text{CAD}} \neq S_{\text{RVT}}$ | `ApplyCad` | Upstream CAD Update | Safe: Modeler has not touched element; apply CAD changes. |
| **3** | False | True | $S_{\text{CAD}} \neq S_{\text{RVT}}$ (within tolerance) | `ReconcileTolerance` | Modeling Tolerance | Auto-align measurements within 1.0mm tolerance. |
| **4** | False | True | $S_{\text{CAD}} \neq S_{\text{RVT}}$ (exceeds tolerance) | `RaiseRfiAndHalt` | **Single Divergence Conflict** | **Rule 0 TRIGGER**: Modeler modified element; CAD unchanged. Stop and report RFI. |
| **5** | True | True | $S_{\text{CAD}} = S_{\text{RVT}}$ | `NoOp` | Convergent Edit | Both parties independently agreed on identical change. Update baseline. |
| **6** | True | True | $S_{\text{CAD}} \neq S_{\text{RVT}}$ | `RaiseRfiAndHalt` | **Dual Divergence Conflict** | **Rule 0 TRIGGER**: Both CAD and Revit edited to different values. Stop and report RFI. |
| **7** | Any | Deleted | Element Tombstoned | `RaiseRfiAndHalt` | **Tombstone Conflict** | CAD expects element, Revit deleted it. Stop and report RFI. |

### 5.2 Deep Dive Scenario: Section 220x220 vs 220x300
Consider the concrete problem:
- CAD ground-truth specifies Column C1: $220 \times 220\text{ mm}$.
- Modeler in Revit changed Column C1 Type: $220 \times 300\text{ mm}$.
- Base was $220 \times 220\text{ mm}$.

Here:
- $\Delta_{\text{CAD}} = \text{False}$ (CAD still says $220 \times 220$).
- $\Delta_{\text{RVT}} = \text{True}$ (Revit says $220 \times 300$).
- Cross-section deviation = $|300 - 220| = 80\text{ mm} > 0.0\text{ mm}$ tolerance.

#### What Happens Under Rule 0 ("Nguyên Tắc Vàng"):
1. **Zero Silent Overwrite**:
   The pipeline **REFUSES** to force $220 \times 220$ back onto Revit. Doing so would destroy the modeler's work without engineering consensus.
2. **Zero Heuristic Assumption**:
   The pipeline **REFUSES** to assume the modeler is correct and update CAD. The modeler might have picked the wrong family type by accident.
3. **Execution Behavior**:
   - `BimPipelineEngine` marks element record status:
     `Status = ElementExecutionStatus.Skipped`
     `Reason = "Conflict: CAD specifies 220x220 mm but Revit model has 220x300 mm. Section delta = 80mm > 0mm tolerance."`
   - Element is placed in `ElementTechnicalGroup.Group2_ConflictingInfo`.
   - `BimCoordinationLedger` creates an `RfiRecord`:
     ```json
     {
       "rfiId": "RFI-20261007-001",
       "title": "Kích thước tiết diện Cột C1 mâu thuẫn giữa CAD và Revit",
       "conflictDescription": "Bản vẽ CAD chỉ định Cột C1 tiết diện 220x220mm. Mô hình Revit hiện tại sử dụng Type 220x300mm. Độ lệch chiều cao tiết diện 80.0mm vượt dung sai cho phép 0.0mm.",
       "elementIds": ["[NhaPho_5x18m.dwg]#[Column]:5A3"],
       "proposedAction": "Chủ trì kết cấu xác nhận tiết diện chuẩn: Giữ 220x300mm (cập nhật lại bản vẽ CAD) hoặc sửa Revit về 220x220mm.",
       "status": "PendingChiefEngineer",
       "createdAt": "2026-10-07T22:30:00Z"
     }
     ```
   - An explicit `CoordinationConflictReport` is written to disk for coordinator review.

### 5.3 Deterministic Conflict Resolution Policies
While Rule 0 is the default and uncompromised standard, the system provides configurable policies via `IConflictResolutionPolicy`:

1. **`Rule0StrictPolicy` (DEFAULT / MANDATORY FOR QC)**:
   Any deviation beyond tolerance halts write for that element, generates RFI, leaves both intact.
2. **`CadAuthorityPolicy` (EXPLICIT CLI FLAG `--force-cad-authority`)**:
   Used when CAD drawing revision has been approved by Chief Engineer as the absolute source of truth. Overwrites Revit, but archives the previous Revit parameters in `AuditRecord`.
3. **`RevitPreservationPolicy` (EXPLICIT CLI FLAG `--accept-revit-override`)**:
   Used when Revit modeling refinements are accepted. Preserves Revit parameters, updates ledger baseline, and flags CAD for back-annotation (DWG revision).

---

## 6. C# Interface Contracts & Data Models

All contracts adhere strictly to repository rules:
- Defined in `HPBim.Core/Coordination/` (host-neutral, `netstandard2.0`, zero `Autodesk.Revit.*` references).
- Adapter and tracker implementations in `HPBim.Revit` / `HPRebar.McpBridge`.
- Method lengths $\le 50$ lines, no boolean flag arguments, explicit value objects.

### 6.1 `ModelChangeEvent` & Supporting Enums

```csharp
namespace HPBim.Core.Coordination;

using System;
using System.Collections.Generic;

/// <summary>
/// Type of modification detected on a BIM model element.
/// </summary>
public enum ModelChangeType
{
    Added = 1,
    ModifiedGeometry = 2,
    ModifiedParameter = 3,
    Deleted = 4
}

/// <summary>
/// Source origin that triggered the model change.
/// </summary>
public enum ChangeOrigin
{
    UserInteractive = 1,      // Manual user action on Revit UI
    PipelineAutomation = 2,   // Programmatic execution via HPBim Named Pipe
    ExternalSync = 3          // External file reload or linked model update
}

/// <summary>
/// Immutable record capturing an atomic change event detected on a BIM element.
/// </summary>
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
```

### 6.2 `IRevitChangeTracker` Contract

```csharp
namespace HPBim.Core.Coordination;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Host-neutral abstraction for bidirectional Revit change tracking.
/// Monitors element lifecycle, detects geometry and parameter shifts,
/// and supplies batched change events to the coordination ledger.
/// </summary>
public interface IRevitChangeTracker
{
    /// <summary>
    /// Gets whether the native Revit DMU updater is currently active and listening.
    /// </summary>
    bool IsTrackingActive { get; }

    /// <summary>
    /// Starts tracking changes in the designated document.
    /// </summary>
    void StartTracking(string documentTitle);

    /// <summary>
    /// Stops tracking and unregisters triggers.
    /// </summary>
    void StopTracking();

    /// <summary>
    /// Registers an element mapping with the runtime tracking cache.
    /// </summary>
    void RegisterTrackedElement(CompositeSourceKey sourceKey, string uniqueId, long elementId);

    /// <summary>
    /// Flushes and retrieves all pending model change events accumulated in the buffer.
    /// </summary>
    IReadOnlyList<ModelChangeEvent> FlushPendingEvents();

    /// <summary>
    /// Temporarily suppresses change tracking for the duration of the returned scope,
    /// attributing any internal operations to the specified origin.
    /// </summary>
    IDisposable SuppressTracking(ChangeOrigin origin);
}
```

### 6.3 `IConflictResolutionPolicy` & Conflict Data Models

```csharp
namespace HPBim.Core.Coordination;

using System;
using System.Collections.Generic;

/// <summary>
/// Action resolved for a conflicted element.
/// </summary>
public enum ConflictResolutionAction
{
    NoOp = 1,                 // No action required (identical or convergent)
    ApplyCad = 2,             // Overwrite Revit with CAD ground truth
    PreserveRevit = 3,        // Keep Revit modeler modifications; back-annotate CAD
    RaiseRfiAndHalt = 4       // Rule 0: Halt execution, generate RFI, leave both intact
}

/// <summary>
/// Context payload provided to the conflict resolution policy.
/// </summary>
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

/// <summary>
/// Deterministic decision emitted by a conflict resolution policy.
/// </summary>
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

/// <summary>
/// Policy governing resolution of diverging CAD and Revit model states.
/// </summary>
public interface IConflictResolutionPolicy
{
    /// <summary>
    /// Evaluates a conflict context and deterministically resolves the required action.
    /// </summary>
    ConflictResolutionDecision ResolveConflict(ConflictContext context);
}
```

### 6.4 `CoordinationConflictReport` Data Contract

```csharp
namespace HPBim.Core.Coordination;

using System;
using System.Collections.Generic;

/// <summary>
/// Comprehensive coordination report summarizing all detected CAD-Revit conflicts.
/// </summary>
public sealed record CoordinationConflictReport
{
    public Guid ReportId { get; init; } = Guid.NewGuid();
    public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;
    public string Project { get; init; } = string.Empty;
    public int TotalTrackedElements { get; init; }
    public int SynchronizedCount { get; init; }
    public int ConflictedCount { get; init; }
    public int TombstonedCount { get; init; }
    public IReadOnlyList<ConflictContext> ActiveConflicts { get; init; } = Array.Empty<ConflictContext>();
    public IReadOnlyList<RfiRecord> GeneratedRfis { get; init; } = Array.Empty<RfiRecord>();
}
```

---

## 7. Edge Cases & Failure Mode Mitigations

| # | Edge Case / Hazard | Mechanism of Failure | Mitigation & Architectural Guarantee |
|---|--------------------|----------------------|---------------------------------------|
| 1 | **Offline Modeler Edits** | Modeler opens the RVT file without the HPRebar add-in installed, modifies elements, saves, and reopens with add-in. | **Dual-Pass Verification**: Real-time DMU handles active sessions. Pre-flight scan compares live element geometry/parameters against the Extensible Storage receipt and ledger baseline upon file open or CLI command dispatch. |
| 2 | **Revit ElementId Recycling** | In Revit, deleted `ElementId` integers can theoretically be recycled by the database for newly created elements. | **Composite Source Key + UniqueId Primacy**: The system relies on Revit's permanent 128-bit `UniqueId` (string GUID) and `CompositeSourceKey`, never naked integer `ElementId`s for identity matching. |
| 3 | **Undo / Redo Stack Churn** | User edits an element, then presses Ctrl+Z (Undo) or Ctrl+Y (Redo). | The updater triggers on Undo/Redo just like manual edits. The 3-way merge compares against the baseline $S_{\text{Base}}$. If the user undos back to $S_{\text{Base}}$, delta becomes 0.0mm, and the conflict state clears automatically back to `Synchronized`. |
| 4 | **Revit Crash During Transaction** | Revit crashes mid-transaction before Extensible Storage is committed. | Atomic Transaction Boundary: All ledger staging in Revit occurs in the exact same `Transaction` as element creation. If transaction rolls back or crashes, Revit DB reverts 100% cleanly. Local `IntentJournal` reconciles on next boot. |
| 5 | **Worksharing Element Borrowing** | Modeler modifies an element borrowed from another user in a Central Model. | The updater inspects `WorksharingUtils.GetCheckoutStatus(doc, id)`. If an element is owned by another user, the updater flags it as `InaccessibleBorrow` rather than attempting a conflicting write. |
| 6 | **Cascading Updaters (Infinite Loop)** | A third-party add-in or structural auto-join modifies geometry inside our updater's execution. | `RecursionGuard` thread-local reentrancy lock prevents re-entry. Changes made by other updaters are queued for the next frame rather than recursing. |

---

## 8. Verification & Test Strategy

### 8.1 Unit Tests (Pure `HPBim.Core.Tests`, No Revit Needed)
- `CompositeSourceKeyTests`: Parsing, equality, formatting, and case-insensitivity.
- `BimCoordinationLedgerTombstoneTests`: Proving that deleting an element sets `IsTombstoned = true` and preserves the mapping without removal.
- `ThreeWayMergeEngineTests`:
  - Test Case 1: Base == CAD == RVT $\implies$ `NoOp`.
  - Test Case 2: Base == RVT, CAD != Base $\implies$ `ApplyCad`.
  - Test Case 3: Base == CAD, RVT modified by 80mm $\implies$ `RaiseRfiAndHalt` + RFI generated.
  - Test Case 4: Base == CAD, RVT modified by 0.5mm $\implies$ `NoOp` (within tolerance).
  - Test Case 5: Element tombstoned in RVT $\implies$ Generation skipped, RFI emitted.
- `Rule0ConflictPolicyTests`: Proving zero silent overwrites occur without explicit policy override.

### 8.2 Integration Tests (`HPBim.Client.Tests`)
- `ReconcileServiceTests`: Simulating Extensible Storage receipt round-trip and conflict detection.
- `FakeRevitChangeTrackerTests`: Emulating high-frequency `ModelChangeEvent` delivery and verifying buffer flush atomicity.

---

## 9. Conclusion & Recommendations for Worker M5

1. **Adopt Tombstoning immediately**: Extend `LedgerElementMapping` with `IsTombstoned` and `DeviationState` in `HPBim.Core`.
2. **Implement `ThreeWayMergeEngine` in `HPBim.Core.Coordination`**: Pure function taking `(Base, Cad, Revit, Tolerances, Policy)` and returning `ConflictResolutionDecision`.
3. **Implement `BimDynamicModelUpdater` in `HPRebar.McpBridge`**: Register optional updater with `AddTrigger` for structural categories and wire up `RecursionGuard`.
4. **Enforce Rule 0 Fail-Fast**: Whenever a conflict is detected, reject automated overwriting and write a formal `RfiRecord`.
