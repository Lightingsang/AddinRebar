# Technical Exploration Report: Milestone M1 (P0 Core — Granularity & Fallback Removal)

**Agent**: `explorer_m1_3` (Teamwork Explorer)  
**Date**: 2026-10-07  
**Working Directory**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_3\`  
**Scope**: Technical exploration for Milestone M1 (P0 Core: Granularity & Fallback Removal) based on `ORIGINAL_REQUEST.md` (## 2026-10-07T13:49:28Z), `PROJECT.md`, and survey baseline findings.

---

## 1. Executive Summary

This exploration provides the formal engineering specification and architecture for eliminating all heuristic guessing and coarse aggregate reporting in the HPBim pipeline, enforcing the Golden Rule (**ĐIỀU 0**):
> *"Khi chưa đủ cơ sở để kết luận, hãy dừng lại và báo chưa đủ thông tin. Không được suy đoán để hoàn thành công việc. Thà để một cấu kiện chưa dựng còn hơn dựng sai nhưng báo đúng."*

### Key Deliverables in this Report:
1. **Design of `ElementExecutionStatus` & `ElementExecutionRecord`** in `HPBim.Core/Execution/` (`netstandard2.0`, zero Revit references): Complete immutable data model with status classification (`Created`, `Updated`, `NoOp`, `Skipped`, `Failed`), Revit element identity tracking (`TargetElementId`, `TargetUniqueId`), and structured diagnostics (`ErrorCode`, `ErrorReason`).
2. **Design of Companion `CompositeSourceKey`**: Structured identifier (`[DocIdentifier]#[Category]:[PrimaryHandle]`) ensuring seamless provenance linking between CAD source handles and Revit elements.
3. **Specification for Script Execution Granular Status Collection & Reporting**: A robust Roslyn execution protocol inside Revit utilizing `SubTransaction` boundaries, returning a typed array of element results through `ExecuteResult.Value`, and deserializing them into the `BimPipelineResult` without losing diagnostics.
4. **Comprehensive Codebase Audit of Fallbacks**: Exhaustive catalog of 26 severe fallback locations across `CompositeBimGenerator`, `DynamicTypeScriptHelper`, the 8 stage generators, and CLI commands—specifically highlighting `.FirstOrDefault() ?? .First()`, arbitrary symbol selection, coordinate clamping, and silent exception swallowing.
5. **Specification of Strict Fail-Fast Behavior (Rule 0)**: 4 pre-flight validation gates (Level, Symbol, Geometry, Host) and `SubTransaction` rollback logic that isolates single-element failures so invalid items fail explicitly without corrupting or preventing valid peer elements in the same batch.

---

## 2. Design: `ElementExecutionRecord` & `ElementExecutionStatus`

### 2.1 File Placement & Architecture Constraints
- **Target Projects**: `HPBim.Core` (`netstandard2.0`).
- **Forbidden References**: `Autodesk.Revit.*` (strictly prohibited).
- **Directory**: `HPBim/src/HPBim.Core/Execution/` (to be created by implementer).
- **Dependencies**: `HPBim.Core.Coordination` for `CoordinationCategory` and `CompositeSourceKey`.

### 2.2 `ElementExecutionStatus` Enum Specification
```csharp
// Path: HPBim/src/HPBim.Core/Execution/ElementExecutionStatus.cs
namespace HPBim.Core.Execution;

/// <summary>
/// Granular execution outcome classification for an individual BIM element.
/// </summary>
public enum ElementExecutionStatus
{
    /// <summary>
    /// Element was newly instantiated in the host Revit document.
    /// </summary>
    Created = 1,

    /// <summary>
    /// An existing element was updated (parameters, geometry, or position synchronized).
    /// </summary>
    Updated = 2,

    /// <summary>
    /// Element already exists and is strictly identical to design specification; no write performed.
    /// </summary>
    NoOp = 3,

    /// <summary>
    /// Intentionally bypassed (e.g., belongs to a frozen zone, disabled stage, or prerequisite failed upstream).
    /// </summary>
    Skipped = 4,

    /// <summary>
    /// Element creation failed due to missing resources, invalid geometry, or host API failure.
    /// </summary>
    Failed = 5
}
```

### 2.3 Companion Domain Record: `CompositeSourceKey`
To support composite provenance mapping specified in `PROJECT.md` line 61, `CompositeSourceKey` must reside in `HPBim.Core/Coordination/`:

```csharp
// Path: HPBim/src/HPBim.Core/Coordination/CompositeSourceKey.cs
namespace HPBim.Core.Coordination;

using System;
using System.Collections.Generic;

/// <summary>
/// Composite source identifier format: [DocIdentifier]#[Category]:[PrimaryHandle]
/// Uniquely traces a BIM element back to its CAD origin and constituent drawing handles.
/// </summary>
public sealed record CompositeSourceKey
{
    public string DocIdentifier { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string PrimaryHandle { get; init; } = string.Empty;
    public IReadOnlyList<string> ProvenanceHandles { get; init; } = Array.Empty<string>();

    public static readonly CompositeSourceKey Empty = new();

    public CompositeSourceKey() { }

    public CompositeSourceKey(string docId, string category, string primaryHandle, IReadOnlyList<string>? provenance = null)
    {
        DocIdentifier = docId ?? string.Empty;
        Category = category ?? string.Empty;
        PrimaryHandle = primaryHandle ?? string.Empty;
        ProvenanceHandles = provenance ?? Array.Empty<string>();
    }

    public static CompositeSourceKey Create(string docId, CoordinationCategory category, string primaryHandle, params string[] provenance)
    {
        return new CompositeSourceKey(docId, category.ToString(), primaryHandle, provenance);
    }

    public static CompositeSourceKey Parse(string formattedKey)
    {
        if (string.IsNullOrWhiteSpace(formattedKey)) return Empty;

        // Syntax: DocId#Category:PrimaryHandle
        int hashIdx = formattedKey.IndexOf('#');
        int colonIdx = formattedKey.IndexOf(':');

        if (hashIdx < 0 || colonIdx < 0 || colonIdx <= hashIdx)
        {
            return new CompositeSourceKey(string.Empty, string.Empty, formattedKey);
        }

        string docId = formattedKey.Substring(0, hashIdx);
        string cat = formattedKey.Substring(hashIdx + 1, colonIdx - hashIdx - 1);
        string handle = formattedKey.Substring(colonIdx + 1);

        return new CompositeSourceKey(docId, cat, handle);
    }

    public override string ToString() => $"{DocIdentifier}#{Category}:{PrimaryHandle}";
}
```

### 2.4 `ElementExecutionRecord` Model Specification
```csharp
// Path: HPBim/src/HPBim.Core/Execution/ElementExecutionRecord.cs
namespace HPBim.Core.Execution;

using System;
using System.Collections.Generic;
using HPBim.Core.Coordination;

/// <summary>
/// Granular execution outcome record for a single BIM element.
/// Guarantees zero-guesswork reporting with explicit status, Revit IDs, and error diagnostics.
/// </summary>
public sealed record ElementExecutionRecord
{
    /// <summary>
    /// Unique composite source key tracing back to the CAD drawing and handles.
    /// </summary>
    public CompositeSourceKey SourceKey { get; init; } = CompositeSourceKey.Empty;

    /// <summary>
    /// Coordination category (e.g. Column, Wall, Beam, Pile, Footing, Slab).
    /// </summary>
    public CoordinationCategory Category { get; init; }

    /// <summary>
    /// Human-readable design mark or tag (e.g. "C1", "D220x400", "P_300").
    /// </summary>
    public string Mark { get; init; } = string.Empty;

    /// <summary>
    /// Granular execution outcome status.
    /// </summary>
    public ElementExecutionStatus Status { get; init; }

    /// <summary>
    /// Numeric Revit ElementId (int in net48 / long in Revit 2024+). Null if execution failed or skipped.
    /// </summary>
    public long? TargetElementId { get; init; }

    /// <summary>
    /// Stable Revit UniqueId GUID string (e.g., "3f124b89-..."). Null if not instantiated.
    /// </summary>
    public string? TargetUniqueId { get; init; }

    /// <summary>
    /// Standardized error classification code (e.g. "ERR_LEVEL_NOT_FOUND", "ERR_SYMBOL_NOT_FOUND").
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Human-readable diagnostic message explaining reason for failure or skip.
    /// </summary>
    public string? ErrorReason { get; init; }

    /// <summary>
    /// Non-fatal diagnostic warnings collected from Revit for this element.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Execution duration in milliseconds for this specific element.
    /// </summary>
    public long ExecutionDurationMs { get; init; }

    /// <summary>
    /// UTC timestamp when this execution record was generated.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Helper checking whether this element is in a successful state.
    /// </summary>
    public bool IsSuccess => Status is ElementExecutionStatus.Created 
                                    or ElementExecutionStatus.Updated 
                                    or ElementExecutionStatus.NoOp;

    // Factory Helpers
    public static ElementExecutionRecord Created(
        CompositeSourceKey sourceKey,
        CoordinationCategory category,
        string mark,
        long targetElementId,
        string targetUniqueId,
        IReadOnlyList<string>? warnings = null,
        long durationMs = 0) => new()
    {
        SourceKey = sourceKey,
        Category = category,
        Mark = mark,
        Status = ElementExecutionStatus.Created,
        TargetElementId = targetElementId,
        TargetUniqueId = targetUniqueId,
        Warnings = warnings ?? Array.Empty<string>(),
        ExecutionDurationMs = durationMs
    };

    public static ElementExecutionRecord Updated(
        CompositeSourceKey sourceKey,
        CoordinationCategory category,
        string mark,
        long targetElementId,
        string targetUniqueId,
        IReadOnlyList<string>? warnings = null,
        long durationMs = 0) => new()
    {
        SourceKey = sourceKey,
        Category = category,
        Mark = mark,
        Status = ElementExecutionStatus.Updated,
        TargetElementId = targetElementId,
        TargetUniqueId = targetUniqueId,
        Warnings = warnings ?? Array.Empty<string>(),
        ExecutionDurationMs = durationMs
    };

    public static ElementExecutionRecord NoOp(
        CompositeSourceKey sourceKey,
        CoordinationCategory category,
        string mark,
        long targetElementId,
        string targetUniqueId) => new()
    {
        SourceKey = sourceKey,
        Category = category,
        Mark = mark,
        Status = ElementExecutionStatus.NoOp,
        TargetElementId = targetElementId,
        TargetUniqueId = targetUniqueId
    };

    public static ElementExecutionRecord Skipped(
        CompositeSourceKey sourceKey,
        CoordinationCategory category,
        string mark,
        string reason) => new()
    {
        SourceKey = sourceKey,
        Category = category,
        Mark = mark,
        Status = ElementExecutionStatus.Skipped,
        ErrorReason = reason
    };

    public static ElementExecutionRecord Failed(
        CompositeSourceKey sourceKey,
        CoordinationCategory category,
        string mark,
        string errorCode,
        string errorReason,
        long durationMs = 0) => new()
    {
        SourceKey = sourceKey,
        Category = category,
        Mark = mark,
        Status = ElementExecutionStatus.Failed,
        ErrorCode = errorCode,
        ErrorReason = errorReason,
        ExecutionDurationMs = durationMs
    };
}
```

### 2.5 JSON Representation Schema
When serialized across the named pipe or written to disk, `ElementExecutionRecord` outputs clean JSON:

```json
{
  "sourceKey": {
    "docIdentifier": "NhaPho_5x18m",
    "category": "Column",
    "primaryHandle": "C_Story1_01",
    "provenanceHandles": ["3A4B", "3A4C"]
  },
  "category": 4,
  "mark": "C1",
  "status": 1,
  "targetElementId": 451293,
  "targetUniqueId": "3f124b89-a291-4e89-9821-bc74e98f0123-0006e2cd",
  "errorCode": null,
  "errorReason": null,
  "warnings": [],
  "executionDurationMs": 14,
  "timestamp": "2026-10-07T14:30:00Z"
}
```

---

## 3. Specification: Script Execution Granular Status Collection & Reporting

### 3.1 The Execution Flow & Boundary Lifecycle
The lifecycle for executing an execution plan or stage in Revit:
1. **Client Dispatch**: `BimPipelineEngine` dispatches `ExecuteRequest` to `revit.execute` with script code and JSON args.
2. **Revit ScriptRunner**: Enters `TransactionGroup` (for outer rollback/assimilate) and `Transaction` (for modification).
3. **Script In-Process Loop**:
   - Compiles pre-flight dictionaries (`levelsByName`, `symbolsDict`, `wallsById`).
   - Iterates through input items.
   - For each item, verifies pre-flight gates.
   - Opens an isolated `SubTransaction` in Revit to execute element creation.
   - If creation succeeds: commits `SubTransaction` and records `Created` status.
   - If creation throws or fails: rolls back `SubTransaction` and records `Failed` status with exact exception details.
   - Accumulates result in a local record list.
4. **Script Return**: Returns structured JSON object containing summary counts and the array of element records.
5. **Client Deserialization**: In `HPBim.Client`, parses `ExecuteResult.Value` directly into `IReadOnlyList<ElementExecutionRecord>`.
6. **Ledger Synchronization**: Updates `BimCoordinationLedger` and registers any failures as RFIs.

### 3.2 Standardized Roslyn Script Template for Granular Reporting
The following C# script pattern MUST be implemented in generators:

```csharp
// --- SCRIPT PREAMBLE & PRE-FLIGHT ---
var items = args.List("items");
var records = new List<Dictionary<string, object>>();

var levelsByName = new FilteredElementCollector(doc)
    .OfClass(typeof(Level))
    .Cast<Level>()
    .ToDictionary(l => l.Name, l => l, StringComparer.OrdinalIgnoreCase);

var symbolsDict = new FilteredElementCollector(doc)
    .OfCategory(BuiltInCategory.OST_StructuralColumns)
    .WhereElementIsElementType()
    .Cast<FamilySymbol>()
    .ToDictionary(s => s.Name, s => s, StringComparer.OrdinalIgnoreCase);

foreach (var item in items)
{
    string sourceKey = item.Str("sourceKey", item.Require("id"));
    string category = item.Str("category", "Column");
    string mark = item.Require("id");
    string lvlName = item.Require("baseLevel");
    string symName = item.Require("symbolName");

    // 1. GATE: LEVEL RESOLUTION (NO .First() FALLBACK)
    if (!levelsByName.TryGetValue(lvlName, out var lvl))
    {
        records.Add(new Dictionary<string, object>
        {
            ["sourceKey"] = sourceKey,
            ["category"] = category,
            ["mark"] = mark,
            ["status"] = "Failed",
            ["errorCode"] = "ERR_LEVEL_NOT_FOUND",
            ["errorReason"] = $"Cao trình '{lvlName}' không tồn tại trong tài liệu Revit."
        });
        continue;
    }

    // 2. GATE: SYMBOL RESOLUTION (NO .First() FALLBACK)
    if (!symbolsDict.TryGetValue(symName, out var sym))
    {
        records.Add(new Dictionary<string, object>
        {
            ["sourceKey"] = sourceKey,
            ["category"] = category,
            ["mark"] = mark,
            ["status"] = "Failed",
            ["errorCode"] = "ERR_SYMBOL_NOT_FOUND",
            ["errorReason"] = $"FamilySymbol '{symName}' không tồn tại trong dự án và cấm fallback ngầm."
        });
        continue;
    }

    // 3. GATE: ISOLATED SUBTRANSACTION EXECUTION
    using (var subTx = new SubTransaction(doc))
    {
        subTx.Start();
        try
        {
            if (!sym.IsActive) sym.Activate();

            var pt = ToXyz(item.Double("x"), item.Double("y"), 0);
            var inst = doc.Create.NewFamilyInstance(pt, sym, lvl, Autodesk.Revit.DB.Structure.StructuralType.Column);

            inst.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.Set(mark);
            inst.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set(sourceKey);

            subTx.Commit();

            records.Add(new Dictionary<string, object>
            {
                ["sourceKey"] = sourceKey,
                ["category"] = category,
                ["mark"] = mark,
                ["status"] = "Created",
                ["targetElementId"] = inst.Id.Value,
                ["targetUniqueId"] = inst.UniqueId,
                ["errorCode"] = null,
                ["errorReason"] = null
            });
        }
        catch (Exception ex)
        {
            subTx.RollBack();
            records.Add(new Dictionary<string, object>
            {
                ["sourceKey"] = sourceKey,
                ["category"] = category,
                ["mark"] = mark,
                ["status"] = "Failed",
                ["errorCode"] = "ERR_REVIT_API_EXCEPTION",
                ["errorReason"] = $"{ex.GetType().Name}: {ex.Message}"
            });
        }
    }
}

// --- SCRIPT RETURN PAYLOAD ---
return new
{
    planId = args.Str("planId", string.Empty),
    total = items.Count,
    created = records.Count(r => (string)r["status"] == "Created"),
    updated = records.Count(r => (string)r["status"] == "Updated"),
    noOp = records.Count(r => (string)r["status"] == "NoOp"),
    skipped = records.Count(r => (string)r["status"] == "Skipped"),
    failed = records.Count(r => (string)r["status"] == "Failed"),
    records = records
};
```

### 3.3 Client Integration in `BimPipelineEngine`
When `ExecuteResult` is received by `BimPipelineEngine`:

```csharp
// Path: HPBim.Client/Pipeline/BimPipelineEngine.cs
private static IReadOnlyList<ElementExecutionRecord> ParseElementRecords(ExecuteResult execRes, string defaultCategory)
{
    if (execRes.Value == null || execRes.Value.Value.ValueKind != JsonValueKind.Object)
    {
        return Array.Empty<ElementExecutionRecord>();
    }

    var root = execRes.Value.Value;
    if (!root.TryGetProperty("records", out var recordsProp) || recordsProp.ValueKind != JsonValueKind.Array)
    {
        return Array.Empty<ElementExecutionRecord>();
    }

    var list = new List<ElementExecutionRecord>();
    foreach (var elem in recordsProp.EnumerateArray())
    {
        string rawSourceKey = elem.GetProperty("sourceKey").GetString() ?? string.Empty;
        string mark = elem.GetProperty("mark").GetString() ?? string.Empty;
        string statusStr = elem.GetProperty("status").GetString() ?? "Failed";
        
        Enum.TryParse<ElementExecutionStatus>(statusStr, true, out var status);
        
        long? elemId = elem.TryGetProperty("targetElementId", out var idProp) && idProp.ValueKind == JsonValueKind.Number
            ? idProp.GetInt64()
            : null;
            
        string? uniqueId = elem.TryGetProperty("targetUniqueId", out var uidProp) && uidProp.ValueKind == JsonValueKind.String
            ? uidProp.GetString()
            : null;
            
        string? errCode = elem.TryGetProperty("errorCode", out var ecProp) && ecProp.ValueKind == JsonValueKind.String
            ? ecProp.GetString()
            : null;
            
        string? errReason = elem.TryGetProperty("errorReason", out var erProp) && erProp.ValueKind == JsonValueKind.String
            ? erProp.GetString()
            : null;

        list.Add(new ElementExecutionRecord
        {
            SourceKey = CompositeSourceKey.Parse(rawSourceKey),
            Mark = mark,
            Status = status,
            TargetElementId = elemId,
            TargetUniqueId = uniqueId,
            ErrorCode = errCode,
            ErrorReason = errReason
        });
    }

    return list;
}
```

---

## 4. Comprehensive Codebase Audit: Heuristic Fallbacks

The investigation audited all script generators in `HPBim.Client` and CLI command scripts in `HPBim.Cli`. We cataloged 26 specific code locations exhibiting heuristic fallbacks, silent skips, or unhandled exception swallows that violate Rule 0.

### 4.1 Summary of Violations by Category
| Violation Pattern | Frequency | Severity | Core Risk |
| :--- | :---: | :---: | :--- |
| **A. `.FirstOrDefault() ?? .First()` Level/Symbol Pick** | 11 locations | **CRITICAL** | Places piles/foundations on roof levels; substitutes arbitrary column/framing cross sections. |
| **B. Arbitrary Plumbing / MEP Symbol Fallback** | 3 locations | **HIGH** | Substitutes first plumbing fixture in project for all toilets, sinks, floor drains. |
| **C. Arbitrary Host Rebar / Opening Fallback** | 2 locations | **CRITICAL** | Hosts rebar into arbitrary columns/beams when target host not found; clamps opening positions. |
| **D. Silent Exception Swallows (`catch (Exception)`)** | 3 locations | **CRITICAL** | Hides geometric errors in rooms and rebars; caller reports clean pass while elements are missing. |
| **E. Silent `continue` on Missing Levels or Curves** | 7 locations | **HIGH** | Silently drops walls, columns, beams, and slabs from model without error reporting or ledger tracking. |

---

### 4.2 Exhaustive Code Locations Audit & Remediation Guide

#### 1. `HPBim.Client/TypeResolver/DynamicTypeScriptHelper.cs`
- **Line 41**:
  ```csharp
  var matching = wallTypes.FirstOrDefault(w => Math.Abs(w.Width - targetWidthFt) < 0.005);
  if (matching != null) return matching;
  ```
  *Violation*: Heuristic thickness matching. If `"HP_Tuong_220mm"` does not exist, it picks any wall with 220mm thickness (e.g. interior gypsum wall instead of exterior brick wall).
  *Remediation*: Remove thickness matching. Exact name resolution only.
- **Line 44**:
  ```csharp
  var baseType = wallTypes.FirstOrDefault() ?? throw new InvalidOperationException("No basic wall type found in project.");
  ```
  *Violation*: Picks whatever wall type happens to be first in document as base for duplication (could be curtain wall or retaining wall).
  *Remediation*: Require explicit base template family name (e.g. `"Generic - 200mm"` or fail).
- **Line 76**:
  ```csharp
  var baseSymbol = symbols.FirstOrDefault() ?? throw new InvalidOperationException("No structural column symbol found.");
  ```
  *Violation*: Arbitrary column duplication. First symbol might be a round column, steel H-column, or timber column.
  *Remediation*: Require base family `"M_Concrete-Rectangular-Column"` or fail fast.
- **Line 101**:
  ```csharp
  var baseSymbol = symbols.FirstOrDefault() ?? throw new InvalidOperationException("No structural framing symbol found.");
  ```
  *Violation*: Arbitrary beam duplication. First symbol might be a steel truss or precast hollow slab.
  *Remediation*: Require base family `"M_Concrete-Rectangular Beam"` or fail fast.
- **Line 122**:
  ```csharp
  var baseType = floorTypes.FirstOrDefault() ?? throw new InvalidOperationException("No floor type found in project.");
  ```
  *Violation*: Arbitrary floor type duplication.
  *Remediation*: Require explicit template floor type.

---

#### 2. `HPBim.Client/ScriptGenerators/CompositeBimGenerator.Stages1To3.cs`
- **Line 43**:
  ```csharp
  if (existingGrids.ContainsKey(name)) continue;
  ```
  *Violation*: Silent skip of existing grids. No record emitted indicating `NoOp`.
  *Remediation*: Emit `ElementExecutionRecord.NoOp` with existing grid element ID.
- **Line 46**:
  ```csharp
  if (p0.DistanceTo(p1) < 0.001) continue;
  ```
  *Violation*: Silent skip of zero-length degenerate grid lines.
  *Remediation*: Emit `ElementExecutionRecord.Failed(..., "ERR_DEGENERATE_GEOMETRY", "Grid curve length < 1.0mm")`.
- **Line 62**:
  ```csharp
  if (!existingLevels.TryGetValue(baseLvlName, out var baseLvl)) continue;
  ```
  *Violation*: Silent skip of columns when level is not found.
  *Remediation*: Emit `ElementExecutionRecord.Failed(..., "ERR_LEVEL_NOT_FOUND", ...)`.
- **Line 83**:
  ```csharp
  if (!existingLevels.TryGetValue(b.Require("level"), out var lvl)) continue;
  ```
  *Violation*: Silent skip of beams when level is not found.
  *Remediation*: Emit `ElementExecutionRecord.Failed(..., "ERR_LEVEL_NOT_FOUND", ...)`.
- **Line 89**:
  ```csharp
  if (p0.DistanceTo(p1) < 0.001) continue;
  ```
  *Violation*: Silent skip of zero-length beams.
  *Remediation*: Emit `ElementExecutionRecord.Failed(..., "ERR_DEGENERATE_GEOMETRY", ...)`.
- **Line 102, 106, 114**:
  ```csharp
  if (!existingLevels.TryGetValue(s.Require("level"), out var lvl)) continue;
  if (pts.Count < 3) continue;
  if (loop.IsOpen()) continue;
  ```
  *Violation*: Silent skip of slabs due to missing level, insufficient boundary points, or open loops.
  *Remediation*: Emit explicit `Failed` record with diagnostic reason for each case.

---

#### 3. `HPBim.Client/ScriptGenerators/CompositeBimGenerator.Stages4To6.cs`
- **Line 14, 19**:
  ```csharp
  if (!existingLevels.TryGetValue(baseLvlName, out var baseLvl)) continue;
  if (p0.DistanceTo(p1) < 0.001) continue;
  ```
  *Violation*: Silent skip of walls.
  *Remediation*: Emit explicit `Failed` records.
- **Line 21**:
  ```csharp
  double defaultHeightFt = Ft(3000.0);
  ```
  *Violation*: Hardcoded 3000mm fallback height when `topLevel` is missing.
  *Remediation*: Require explicit wall height or top level; fail if unspecified.
- **Line 59-62**:
  ```csharp
  var defaultDoorSym = doorSymbols.FirstOrDefault();
  var defaultWinSym = windowSymbols.FirstOrDefault();
  ```
  *Violation*: Heuristic default door/window symbol assignment.
  *Remediation*: Require explicit target family symbol.
- **Line 67**:
  ```csharp
  if (!wallsById.TryGetValue(hostWallId, out var hostWall)) continue;
  ```
  *Violation*: Silent skip of door/window opening if host wall not found.
  *Remediation*: Emit `ElementExecutionRecord.Failed(..., "ERR_HOST_NOT_FOUND", ...)`.
- **Line 74-75**:
  ```csharp
  var sym = symbolList.FirstOrDefault(s => string.Equals(s.Name, targetName, StringComparison.OrdinalIgnoreCase))
      ?? symbolList.FirstOrDefault();
  ```
  *Violation*: Extreme heuristic fallback. If opening symbol is missing, grabs the first door/window symbol in project.
  *Remediation*: Remove `?? symbolList.FirstOrDefault()`. Fail element if symbol not found.
- **Line 82**:
  ```csharp
  double param = Math.Clamp(distFt / locCurve.Curve.Length, 0.05, 0.95);
  ```
  *Violation*: Heuristic geometry distortion. If opening falls outside wall curve, clamps it to 5%-95% of wall length!
  *Remediation*: If opening distance falls outside wall bounds, fail element with `ERR_GEOMETRY_OUT_OF_BOUNDS`.
- **Line 110**:
  ```csharp
  catch (Exception) { /* Skip room if point outside boundary */ }
  ```
  *Violation*: Silent exception swallowing on room creation.
  *Remediation*: Catch exception and emit `ElementExecutionRecord.Failed(..., "ERR_ROOM_NOT_ENCLOSED", ...)`.

---

#### 4. `HPBim.Client/ScriptGenerators/CompositeBimGenerator.Stages7To8.cs`
- **Line 13**:
  ```csharp
  var defaultFixtureSym = fixtureSymbols.FirstOrDefault();
  ```
  *Violation*: Assigns first plumbing fixture to every fixture in project.
  *Remediation*: Match fixture family name explicitly; fail if missing.
- **Line 33, 38**:
  ```csharp
  var pipingSystemType = ... .FirstOrDefault();
  var pipeType = ... .FirstOrDefault();
  ```
  *Violation*: Arbitrary piping system and pipe type selection.
  *Remediation*: Match system type (e.g. Sanitary, Domestic Cold Water) explicitly.
- **Line 98**:
  ```csharp
  var barType = rebarBarTypes.OrderBy(b => Math.Abs(b.BarModelDiameter - diamFt)).FirstOrDefault() ?? rebarBarTypes[0];
  ```
  *Violation*: Arbitrary bar type fallback to index 0.
  *Remediation*: Fail rebar element if diameter cannot be matched within tolerance ($\pm 0.5\text{mm}$).
- **Line 103**:
  ```csharp
  host ??= hostCandidates.FirstOrDefault();
  ```
  *Violation*: Assigns rebar to an arbitrary column or beam if `hostId` not found!
  *Remediation*: Fail rebar element if host is not found.
- **Line 144-147**:
  ```csharp
  catch (Exception)
  {
      // Fallback gracefully if host lacks valid geometry cover or boundaries.
  }
  ```
  *Violation*: Silent exception swallowing for failed rebar creation.
  *Remediation*: Record `ElementExecutionRecord.Failed(..., "ERR_REBAR_CREATION_FAILED", ex.Message)`.

---

#### 5. CLI Commands in `HPBim.Cli/Commands/`
- **`GeneratePilesCommand.cs:162-163`**:
  ```csharp
  var level = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
      .FirstOrDefault(l => l.Name == "Tầng Móng")
      ?? new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().First();
  ```
  *Violation*: Fallback to first level in project for piles.
  *Remediation*: Throw `InvalidOperationException("Cấp cao trình 'Tầng Móng' không tồn tại")`.
- **`GeneratePilesCommand.cs:174`**:
  ```csharp
  ?? new FilteredElementCollector(doc)...FirstOrDefault(s => s.Name.Contains("300x300") || s.Name.Contains("300"));
  ```
  *Violation*: Arbitrary fuzzy string search for pile symbol.
  *Remediation*: Exact symbol match required.
- **`GenerateColumnsCommand.cs:179-181`**:
  ```csharp
  FamilySymbol baseSymbol = symbols.FirstOrDefault(...) ?? symbols.FirstOrDefault(...) ?? symbols.First();
  ```
  *Violation*: Fallback to first column symbol in project.
  *Remediation*: Fail if rectangular concrete column family is missing.
- **`GenerateGradeBeamsCommand.cs:194-195`**:
  ```csharp
  var levelMong = ... .FirstOrDefault(l => l.Name == "Tầng Móng") ?? ... .First();
  ```
  *Violation*: Fallback to first level in project for grade beams.
  *Remediation*: Fail if `"Tầng Móng"` level is missing.
- **`FoundationRemediationBuilder.cs:41-42, 54, 115, 119, 186`**:
  Contains multiple `.First()` fallbacks on `FloorType`, `Level`, and `FamilySymbol`.
  *Remediation*: Replace all with strict dictionary lookup and fail fast.
- **`CadExtractionScript.cs:102-103, 114`**:
  ```csharp
  double originX = xGrids.FirstOrDefault(g => g.name == "1")?.x ?? (xGrids.Count > 0 ? xGrids[0].x : 0.0);
  string pName = nearest?.Name ?? "P?";
  ```
  *Violation*: Guesses grid origins and pile marks.
  *Remediation*: Fail extraction if datum grid is unresolvable.

---

## 5. Specification: Strict Fail-Fast Behavior per Rule 0

### 5.1 The 4 Pre-Flight Validation Gates
To prevent corrupting the Revit document or creating geometry based on speculation, every element generator MUST execute 4 mandatory validation gates before calling any creation API:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       INPUT: ElementPlanItem / Args Item                    │
└──────────────────────────────────────┬──────────────────────────────────────┘
                                       │
                                       ▼
             ┌───────────────────────────────────────────────────┐
             │ Gate 1: Level Resolution                          │
             │   - Lookup exact Level name                       │
             │   - Verify Level is valid & finite                │
             └────────┬──────────────────────────────────────────┘
                      │ Pass                        Fail
                      ├──────────────────────────────────────────────┐
                      ▼                                              │
             ┌───────────────────────────────────────────────────┐   │
             │ Gate 2: Family / Symbol Resolution                │   │
             │   - Lookup exact Family & Symbol name             │   │
             │   - Verify Symbol is active or activatable        │   │
             └────────┬──────────────────────────────────────────┘   │
                      │ Pass                        Fail             │
                      ├──────────────────────────────────────────────┤
                      ▼                                              │
             ┌───────────────────────────────────────────────────┐   │
             │ Gate 3: Geometric Integrity                       │   │
             │   - Check finite coordinates (no NaN/Infinity)    │   │
             │   - Curve length >= 1.0mm (no zero-length lines)  │   │
             │   - Polygon closed, >= 3 points, non-self-cut     │   │
             └────────┬──────────────────────────────────────────┘   │
                      │ Pass                        Fail             │
                      ├──────────────────────────────────────────────┤
                      ▼                                              │
             ┌───────────────────────────────────────────────────┐   │
             │ Gate 4: Host Dependency (if hosted)               │   │
             │   - Host element exists in Document               │   │
             │   - Opening falls within host curve boundary      │   │
             └────────┬──────────────────────────────────────────┘   │
                      │ Pass                        Fail             │
                      │                                              │
                      ▼                                              ▼
       ┌─────────────────────────────┐               ┌─────────────────────────────┐
       │ Execute SubTransaction      │               │ Record Status = Failed      │
       │   - Success: Status=Created │               │   - ErrorCode & ErrorReason │
       │   - Catch: Rollback SubTx   │               │   - Register RFI in Ledger  │
       │     Status=Failed           │               │   - Continue to next item   │
       └─────────────────────────────┘               └─────────────────────────────┘
```

### 5.2 Error Taxonomy
Standardized error codes to be reported in `ElementExecutionRecord.ErrorCode`:

| Error Code | Classification | Description |
| :--- | :--- | :--- |
| `ERR_LEVEL_NOT_FOUND` | Resource Missing | Specified target Level does not exist in the active document. |
| `ERR_SYMBOL_NOT_FOUND` | Resource Missing | Specified FamilySymbol does not exist in project and cannot be duplicated. |
| `ERR_FAMILY_NOT_FOUND` | Resource Missing | Base template family needed for dynamic type duplication is missing. |
| `ERR_HOST_NOT_FOUND` | Dependency Missing | Host wall, column, or beam could not be found for hosted element (Opening/Rebar). |
| `ERR_GEOMETRY_DEGENERATE` | Geometric Defect | Point distance < 1.0mm, collinear loop, or zero-area polygon. |
| `ERR_GEOMETRY_NON_FINITE` | Geometric Defect | Coordinates contain `NaN` or `Infinity`. |
| `ERR_GEOMETRY_OUT_OF_BOUNDS` | Geometric Defect | Opening distance along wall exceeds the host wall curve length. |
| `ERR_ROOM_NOT_ENCLOSED` | Spatial Defect | Room insertion point is located in an open, unbonded region. |
| `ERR_REVIT_API_EXCEPTION` | Runtime Fault | Revit DB API threw an exception during element instantiation. |
| `ERR_UNACCOUNTED_ITEM` | Pipeline Fault | Item was included in execution plan but omitted from script return payload. |

### 5.3 Ledger Synchronization & RFI Record Generation
When an element receives `ElementExecutionStatus.Failed`:
1. It is recorded in `BimCoordinationLedger.Elements[id]`:
   - `TechnicalGroup` $\rightarrow$ `ElementTechnicalGroup.Group3_MissingInfo` or `Group2_ConflictingInfo`.
   - `Step` remains at `Step2_CheckAndClassify` or routes to `Step5_RouteFix`.
2. An automated RFI is registered in `BimCoordinationLedger.Rfis`:
   ```csharp
   ledger.AddRfi(new RfiRecord
   {
       RfiId = $"RFI-{element.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}",
       ElementId = element.Id,
       Question = $"[Fail-Fast] Không thể dựng {element.Category} '{element.Mark}': {record.ErrorReason} (Mã lỗi: {record.ErrorCode})",
       Status = RfiStatus.PendingChiefEngineer,
       ProposedResolution = "Cung cấp bổ sung thông tin Family/Level hoặc hiệu chỉnh tọa độ CAD."
   });
   ```
3. The element is output to the CLI report under `"Chưa đủ cơ sở để kết luận"` with exact coordinates and reasons. It is NEVER falsely reported as built.

---

## 6. Implementation Blueprint for Implementer Agent

### 6.1 Concrete Implementation Steps
1. **Create Execution Models in `HPBim.Core`**:
   - Create directory `HPBim/src/HPBim.Core/Execution/`.
   - Implement `ElementExecutionStatus.cs` (enum: Created, Updated, NoOp, Skipped, Failed).
   - Implement `CompositeSourceKey.cs` in `HPBim/src/HPBim.Core/Coordination/`.
   - Implement `ElementExecutionRecord.cs` in `HPBim/src/HPBim.Core/Execution/`.
2. **Refactor `DynamicTypeScriptHelper.cs`**:
   - Eliminate all `.FirstOrDefault() ?? throw` and thickness-based fuzzy matching.
   - Implement strict lookup or explicit family template duplication.
3. **Refactor Script Generators**:
   - Update `CompositeBimGenerator` (all stages) and Stage 1–8 generators to:
     - Pre-validate Level, Symbol, Geometry, and Host.
     - Wrap each element creation in `using var subTx = new SubTransaction(doc)`.
     - Return `List<ElementExecutionRecord>` in script return dictionary.
4. **Update `HPBim.Client` Pipeline**:
   - Update `StageResult` and `BimPipelineResult` to include `IReadOnlyList<ElementExecutionRecord> ElementRecords`.
   - Implement `ParseElementRecords` in `BimPipelineEngine`.
   - Aggregate counts based on `ElementRecords` rather than raw `ChangedCounts`.
5. **Unit & Integration Testing**:
   - Add unit tests in `HPBim.Core.Tests` verifying `ElementExecutionRecord` and `CompositeSourceKey` serialization.
   - Add integration tests in `HPBim.Client.Tests` verifying granular reporting on partial failures and zero-fallback assertions.

---

## 7. Quality & Clean Code Compliance Check

- **Layer Boundaries**: `HPBim.Core` remains pure `netstandard2.0` with 0 Revit references.
- **Rule Limits**: Classes $\le 300$ lines, methods $\le 50$ lines.
- **Exception Governance**: Zero empty catch blocks; all exceptions recorded with diagnostic context.
- **Fail-Fast Compliance**: Zero heuristic fallbacks (`.FirstOrDefault() ?? .First()`); 100% adherence to Rule 0.
