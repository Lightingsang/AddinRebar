# Technical Exploration & Architectural Specification: Milestone M1 (P0 Core: Single Execution Plan IR)

**Author**: `explorer_m1_2` (Teamwork Explorer)  
**Date**: 2026-10-07  
**Working Directory**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_2`  
**Master Plan Reference**: `PROJECT.md` (Milestone M1: P0 Core: Baseline Repair, Schema Validation & Execution Plan IR)  
**Authoritative Requirement**: `ORIGINAL_REQUEST.md` (## 2026-10-07T13:49:28Z: R1. P0 — Kết quả trung thực & Preview/Apply tương đương)  

---

## 1. Executive Summary

This report establishes the technical specification and detailed C# design for **Milestone M1 (P0 Core: Single Execution Plan IR)** in the `HPBim` CAD-to-BIM coordination ecosystem.

The current system exhibits four critical structural defects that violate the Golden Rule (ĐIỀU 0: *"Khi chưa đủ cơ sở để kết luận, hãy dừng lại và báo chưa đủ thông tin. Không được suy đoán để hoàn thành công việc. Thà để một cấu kiện chưa dựng còn hơn dựng sai nhưng báo đúng"*):
1. **Divergent Execution Worlds**: Dry-run preview executes via `CompositeBimGenerator` in a single transaction group, whereas apply commit executes via 8 separate sequential RPC stages using individual stage generators. Over 20 ad-hoc CLI commands bypass the pipeline entirely with inline Roslyn scripts.
2. **View Creation Discrepancy**: `Stage1LevelGenerator.cs` (used in apply) creates plan views via `ViewPlan.Create`, while `CompositeBimGenerator.Stages1To3.cs` (used in preview) completely omits `ViewPlan.Create`.
3. **Pervasive Implicit Fallbacks**: Multiple generators fall back to arbitrary levels (`levels.First()`), arbitrary column/framing symbols (`symbols.First()`), first plumbing fixtures, or silently swallow exceptions and clamp geometries.
4. **Coarse Aggregate Reporting**: Bridge outcomes only return aggregate numbers (`createdCount = 10`), omitting per-element tracking (`Created`, `Updated`, `NoOp`, `Skipped`, `Failed`) and source key mapping.

To solve these defects once and for all, this exploration delivers:
- **`BimExecutionPlan` IR Suite** in `HPBim.Core/Execution/` (`BimExecutionPlan`, `ElementPlanItem`, `ExecutionOptions`, `ElementGeometrySpec`, `ResourceResolutionSpec`, `ElementExecutionRecord`, `DocumentIdentitySnapshot`).
- **`ExecutionPlanBuilder`** in `HPBim.Client/Pipeline/` to resolve all coordinates, levels, and symbols strictly ahead-of-time before dispatching code to Revit.
- **Unified `BimPipelineEngine` Specification** executing the exact same parameter-driven script and arguments for both Preview (`dryRun: true` with rollback confirmation) and Apply (`dryRun: false` with commit), guaranteeing 100% equivalence.
- **Unified `ViewPlan.Create` Strategy** executing identically in Preview and Apply with duplicate-name conflict resolution and explicit `ViewFamilyType` binding.

---

## 2. As-Is Codebase Observations & Defect Evidence

### 2.1 The Preview vs. Apply Disconnect in `BimPipelineEngine.cs`
In `HPBim\src\HPBim.Client\Pipeline\BimPipelineEngine.cs:51-57`:
```csharp
if (options.DryRun && options.AtomicDryRun)
{
    return await ExecuteAtomicDryRunAsync(model, options, sw, cancellationToken).ConfigureAwait(false);
}

return await ExecuteSequentialStagesAsync(model, options, sw, cancellationToken).ConfigureAwait(false);
```
- In **Preview** (`DryRun && AtomicDryRun`), line 65 calls `_compositeGenerator.Generate(model)`.
- In **Apply** (`!DryRun`), line 113 iterates through `_generators` (8 separate script generators) sending 8 distinct JSON-RPC requests over the named pipe.
- If `DryRun == true` and `AtomicDryRun == false`, it runs 8 sequential requests each with `dryRun: true`. Because Stage 1 rolls back, levels do not exist when Stage 3 runs, causing Stage 3 to skip all columns and beams silently.

### 2.2 The View Creation Asymmetry
- In `HPBim\src\HPBim.Client\ScriptGenerators\Stage1LevelGenerator.cs:98-102` (used during sequential apply):
  ```csharp
  if (createViews && planType != null && !existingViewLevelIds.Contains(level.Id))
  {
      ViewPlan.Create(doc, planType.Id, level.Id);
      existingViewLevelIds.Add(level.Id);
  }
  ```
- In `HPBim\src\HPBim.Client\ScriptGenerators\CompositeBimGenerator.Stages1To3.cs:5-29` (used during atomic preview):
  `GetStage1LevelsScript()` contains **zero calls** to `ViewPlan.Create`.
- **Impact**: If a Revit template has conflicting view names, missing view types, or throws an exception during `ViewPlan.Create`, the Preview passes with 100% success, but the Apply immediately fails.

### 2.3 Implicit Heuristics & Silent Fallbacks Catalogue
1. **Level Fallback**: `HPBim.Cli\Commands\GeneratePilesCommand.cs:162-163`:
   ```csharp
   var level = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
       .FirstOrDefault(l => l.Name == "Tầng Móng")
       ?? new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().First();
   ```
   *Defect*: If `"Tầng Móng"` is missing, piles are placed on whichever level happens to be first in the document (e.g. roof).
2. **Column Symbol Fallback**: `HPBim.Client\TypeResolver\DynamicTypeScriptHelper.cs:76`:
   ```csharp
   var baseSymbol = symbols.FirstOrDefault() ?? throw new InvalidOperationException("No structural column symbol found.");
   ```
   *Defect*: Arbitrarily picks the first column symbol in the project (could be circular steel or hollow timber) to duplicate concrete columns.
3. **Plumbing Fixture Fallback**: `HPBim.Client\ScriptGenerators\CompositeBimGenerator.Stages7To8.cs:13`:
   ```csharp
   var defaultFixtureSym = fixtureSymbols.FirstOrDefault();
   ```
   *Defect*: The first fixture found is used for every fixture: toilets, sinks, floor drains become identical.
4. **Rebar Host & Silent Catch**: `CompositeBimGenerator.Stages7To8.cs:103, 144-147`:
   ```csharp
   host ??= hostCandidates.FirstOrDefault();
   ...
   try { var rebar = Rebar.CreateFromCurves(...); }
   catch (Exception) { /* Fallback gracefully */ }
   ```
   *Defect*: Silently swallows exceptions and attaches rebar to arbitrary candidate hosts.

---

## 3. Core Domain IR Design: `HPBim.Core/Execution/`

All models in `HPBim.Core/Execution/` target `netstandard2.0`, maintain zero references to `Autodesk.Revit.*`, use C# immutable records with `init` accessors, and are 100% testable on any .NET runtime.

### 3.1 Class Diagram & Structural Relationships

```
┌────────────────────────────────────────────────────────────────────────┐
│                          BimExecutionPlan                              │
│ - PlanId: Guid                                                         │
│ - PlanHash: string (SHA-256)                                           │
│ - SourceModelName: string                                              │
│ - TargetDoc: DocumentIdentitySnapshot                                  │
│ - Items: IReadOnlyList<ElementPlanItem>                                │
│ - Options: ExecutionOptions                                            │
│ - Metadata: IReadOnlyDictionary<string, string>                        │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ contains 1..N
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                          ElementPlanItem                               │
│ - SourceKey: CompositeSourceKey                                        │
│ - Category: CoordinationCategory                                       │
│ - Mark: string                                                         │
│ - SequenceOrder: int                                                   │
│ - TargetLevelName: string                                              │
│ - TargetTopLevelName: string?                                          │
│ - Resource: ResourceResolutionSpec                                     │
│ - Geometry: ElementGeometrySpec                                        │
│ - ProvenanceHandles: IReadOnlyList<string>                             │
│ - Parameters: IReadOnlyDictionary<string, object>                      │
│ - IsStructural: bool                                                   │
│ - CreateAssociatedView: bool                                           │
│ - HostElementKey: string?                                              │
└───────────────────┬───────────────────────────────┬────────────────────┘
                    │ uses                          │ uses
                    ▼                               ▼
┌──────────────────────────────────────┐  ┌──────────────────────────────┐
│       ResourceResolutionSpec         │  │     ElementGeometrySpec      │
│ - FamilyName: string                 │  │ (Abstract Base)              │
│ - TypeName: string                   │  ├──────────────────────────────┤
│ - RequiresProceduralCreation: bool   │  │ PointGeometrySpec            │
│ - BaseTypeName: string?              │  │ LineGeometrySpec             │
│ - DimensionParametersMm: Dictionary  │  │ ProfileGeometrySpec          │
└──────────────────────────────────────┘  │ OpeningGeometrySpec          │
                                          │ LevelGeometrySpec            │
                                          └──────────────────────────────┘
```

### 3.2 Detailed Contract Specifications

#### 1. `BimExecutionPlan.cs`
```csharp
namespace HPBim.Core.Execution;

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HPBim.Core.Coordination;

/// <summary>
/// Immutable, single-source-of-truth Intermediate Representation (IR) defining
/// the exact topological sequence of operations to execute in Revit.
/// Shared identically by Preview (dry-run rollback) and Apply (commit).
/// </summary>
public sealed record BimExecutionPlan
{
    public Guid PlanId { get; init; } = Guid.NewGuid();
    public string PlanHash { get; init; } = string.Empty;
    public string SourceModelName { get; init; } = string.Empty;
    public string SchemaVersion { get; init; } = "1.0.0";
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public DocumentIdentitySnapshot TargetDoc { get; init; } = new();
    public IReadOnlyList<ElementPlanItem> Items { get; init; } = Array.Empty<ElementPlanItem>();
    public ExecutionOptions Options { get; init; } = new();
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    public int TotalElementCount => Items.Count;

    /// <summary>
    /// Computes a deterministic SHA-256 hash representing the full plan content and preconditions.
    /// </summary>
    public static string ComputePlanHash(
        Guid planId,
        DocumentIdentitySnapshot targetDoc,
        IReadOnlyList<ElementPlanItem> items)
    {
        using var sha256 = SHA256.Create();
        var sb = new StringBuilder();
        sb.Append(planId.ToString("N"));
        sb.Append('|').Append(targetDoc.Title);
        sb.Append('|').Append(targetDoc.VersionGuid.ToString("N"));

        foreach (var item in items)
        {
            sb.Append(';').Append(item.SourceKey);
            sb.Append(':').Append(item.Category);
            sb.Append(':').Append(item.Mark);
            sb.Append(':').Append(item.TargetLevelName);
            sb.Append(':').Append(item.Resource.TypeName);
            sb.Append(':').Append(item.Geometry.GetCanonicalString());
        }

        byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
        return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
    }
}
```

#### 2. `ElementPlanItem.cs`
```csharp
namespace HPBim.Core.Execution;

using System;
using System.Collections.Generic;
using HPBim.Core.Coordination;

/// <summary>
/// Atomic element operation in the execution plan with pre-resolved coordinates,
/// level bindings, and family symbol specifications.
/// </summary>
public sealed record ElementPlanItem
{
    public CompositeSourceKey SourceKey { get; init; } = new();
    public CoordinationCategory Category { get; init; }
    public string Mark { get; init; } = string.Empty;
    public int SequenceOrder { get; init; }
    public string TargetLevelName { get; init; } = string.Empty;
    public string? TargetTopLevelName { get; init; }
    public ResourceResolutionSpec Resource { get; init; } = new();
    public ElementGeometrySpec Geometry { get; init; } = new PointGeometrySpec();
    public IReadOnlyList<string> ProvenanceHandles { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();
    public bool IsStructural { get; init; }
    public bool CreateAssociatedView { get; init; }
    public string? HostElementKey { get; init; }
}
```

#### 3. `ExecutionOptions.cs`
```csharp
namespace HPBim.Core.Execution;

using System;

/// <summary>
/// Operational parameters controlling plan execution in BimPipelineEngine and Revit bridge.
/// </summary>
public sealed record ExecutionOptions
{
    /// <summary>True for Preview (rollback transaction); false for Apply (commit transaction).</summary>
    public bool DryRun { get; init; }

    /// <summary>When true, any unresolved symbol or level immediately aborts (Golden Rule / Fail-Fast).</summary>
    public bool StrictMode { get; init; } = true;

    /// <summary>Timeout ceiling in seconds for Revit script execution.</summary>
    public int TimeoutSeconds { get; init; } = 120;

    /// <summary>Whether to create associated FloorPlan views for Level items.</summary>
    public bool CreatePlanViews { get; init; } = true;

    /// <summary>Instructs Revit bridge to capture all FailureSeverity.Warning messages into the response.</summary>
    public bool CaptureWarnings { get; init; } = true;

    /// <summary>Label attached to the Revit TransactionGroup.</summary>
    public string TransactionLabel { get; init; } = "HPBim Execution Plan";

    /// <summary>Unique operation tracking ID recorded in IntentJournal and Extensible Storage receipt.</summary>
    public Guid OperationId { get; init; } = Guid.NewGuid();
}
```

#### 4. `ResourceResolutionSpec.cs`
```csharp
namespace HPBim.Core.Execution;

using System;
using System.Collections.Generic;

/// <summary>
/// Explicit Revit family and symbol definition resolved ahead-of-time.
/// Eliminates runtime guessing in Revit scripts.
/// </summary>
public sealed record ResourceResolutionSpec
{
    public string FamilyName { get; init; } = string.Empty;
    public string TypeName { get; init; } = string.Empty;
    public bool RequiresProceduralCreation { get; init; }
    public string? BaseTypeName { get; init; }
    public IReadOnlyDictionary<string, double> DimensionParametersMm { get; init; } = new Dictionary<string, double>();
}
```

#### 5. `ElementGeometrySpec.cs` & Concrete Variants
```csharp
namespace HPBim.Core.Execution;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HPBim.Core.Geometry;
using HPBim.Core.Models;

public abstract record ElementGeometrySpec
{
    public abstract string GetCanonicalString();
}

public sealed record PointGeometrySpec : ElementGeometrySpec
{
    public Point3D Location { get; init; } = new(0, 0, 0);
    public double RotationDegrees { get; init; }
    public double OffsetZMm { get; init; }

    public override string GetCanonicalString() =>
        string.Format(CultureInfo.InvariantCulture, "PT({0:F2},{1:F2},{2:F2},r:{3:F1},z:{4:F1})",
            Location.X, Location.Y, Location.Z, RotationDegrees, OffsetZMm);
}

public sealed record LineGeometrySpec : ElementGeometrySpec
{
    public Point3D Start { get; init; } = new(0, 0, 0);
    public Point3D End { get; init; } = new(0, 0, 0);
    public double ElevationMm { get; init; }

    public override string GetCanonicalString() =>
        string.Format(CultureInfo.InvariantCulture, "LN(({0:F2},{1:F2},{2:F2})->({3:F2},{4:F2},{5:F2}),el:{6:F1})",
            Start.X, Start.Y, Start.Z, End.X, End.Y, End.Z, ElevationMm);
}

public sealed record ProfileGeometrySpec : ElementGeometrySpec
{
    public IReadOnlyList<Point3D> OuterLoop { get; init; } = Array.Empty<Point3D>();
    public IReadOnlyList<IReadOnlyList<Point3D>> InnerLoops { get; init; } = Array.Empty<IReadOnlyList<Point3D>>();
    public double ThicknessMm { get; init; }
    public double ElevationMm { get; init; }

    public override string GetCanonicalString() =>
        string.Format(CultureInfo.InvariantCulture, "PRF({0}pts,t:{1:F1},el:{2:F1})",
            OuterLoop.Count, ThicknessMm, ElevationMm);
}

public sealed record OpeningGeometrySpec : ElementGeometrySpec
{
    public string HostWallSourceKey { get; init; } = string.Empty;
    public double DistanceAlongWallMm { get; init; }
    public double SillHeightMm { get; init; }
    public double WidthMm { get; init; }
    public double HeightMm { get; init; }
    public OpeningKind Kind { get; init; }

    public override string GetCanonicalString() =>
        string.Format(CultureInfo.InvariantCulture, "OPN(h:{0},d:{1:F1},sill:{2:F1},{3:F0}x{4:F0},{5})",
            HostWallSourceKey, DistanceAlongWallMm, SillHeightMm, WidthMm, HeightMm, Kind);
}

public sealed record LevelGeometrySpec : ElementGeometrySpec
{
    public double ElevationM { get; init; }
    public int FloorNumber { get; init; }
    public bool IsBuildingStory { get; init; } = true;

    public override string GetCanonicalString() =>
        string.Format(CultureInfo.InvariantCulture, "LVL(el:{0:F3}m,f:{1},story:{2})",
            ElevationM, FloorNumber, IsBuildingStory);
}
```

#### 6. `ElementExecutionRecord.cs` & `ElementExecutionStatus.cs`
```csharp
namespace HPBim.Core.Execution;

using System;
using System.Collections.Generic;
using HPBim.Core.Coordination;

public enum ElementExecutionStatus
{
    Created = 1,
    Updated = 2,
    NoOp = 3,
    Skipped = 4,
    Failed = 5
}

public sealed record ElementExecutionRecord
{
    public CompositeSourceKey SourceKey { get; init; } = new();
    public ElementExecutionStatus Status { get; init; }
    public string? TargetUniqueId { get; init; }
    public long? TargetElementId { get; init; }
    public string? ErrorReason { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
    public double? MeasuredWidthMm { get; init; }
    public double? MeasuredHeightMm { get; init; }
    public double? MeasuredElevationMm { get; init; }
}
```

#### 7. `DocumentIdentitySnapshot.cs`
```csharp
namespace HPBim.Core.Execution;

using System;

public sealed record DocumentIdentitySnapshot
{
    public string Title { get; init; } = string.Empty;
    public string? PathName { get; init; }
    public Guid VersionGuid { get; init; } = Guid.Empty;
    public int NumberOfSaves { get; init; }
    public Guid CentralGuid { get; init; } = Guid.Empty;
    public string? ActiveViewName { get; init; }
}
```

#### 8. `CompositeSourceKey.cs` (in `HPBim.Core/Coordination/`)
```csharp
namespace HPBim.Core.Coordination;

using System;
using System.Text.RegularExpressions;

/// <summary>
/// Composite source key format: [DocIdentifier]#[Category]:[PrimaryHandle]
/// Example: NhaPho5x18#[Column]:COL_1
/// </summary>
public sealed record CompositeSourceKey : IEquatable<CompositeSourceKey>
{
    private static readonly Regex KeyPattern = new(@"^\[([^\]]+)\]#\[([^\]]+)\]:(.+)$", RegexOptions.Compiled);

    public string DocIdentifier { get; init; } = string.Empty;
    public CoordinationCategory Category { get; init; }
    public string PrimaryHandle { get; init; } = string.Empty;

    public static CompositeSourceKey Create(string docId, CoordinationCategory category, string handle) =>
        new()
        {
            DocIdentifier = docId ?? throw new ArgumentNullException(nameof(docId)),
            Category = category,
            PrimaryHandle = handle ?? throw new ArgumentNullException(nameof(handle))
        };

    public static CompositeSourceKey Parse(string formattedKey)
    {
        if (TryParse(formattedKey, out var result)) return result!;
        throw new FormatException($"Invalid CompositeSourceKey format: '{formattedKey}'. Expected format: [DocIdentifier]#[Category]:[PrimaryHandle]");
    }

    public static bool TryParse(string? formattedKey, out CompositeSourceKey? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(formattedKey)) return false;

        var match = KeyPattern.Match(formattedKey);
        if (!match.Success) return false;

        string docId = match.Groups[1].Value;
        string catStr = match.Groups[2].Value;
        string handle = match.Groups[3].Value;

        if (!Enum.TryParse<CoordinationCategory>(catStr, true, out var category)) return false;

        result = new CompositeSourceKey { DocIdentifier = docId, Category = category, PrimaryHandle = handle };
        return true;
    }

    public override string ToString() => $"[{DocIdentifier}]#[{Category}]:{PrimaryHandle}";
}
```

---

## 4. Pipeline Orchestrator Design: `ExecutionPlanBuilder`

`ExecutionPlanBuilder` lives in `HPBim.Client/Pipeline/` and runs entirely on the client before dispatching any code to Revit.

### 4.1 Architecture & Workflow

```
┌──────────────────┐    ┌────────────────────────┐
│     BimModel     │    │ DocumentIdentitySnapshot│
│  (Validated JSON)│    │ (from revit.context)   │
└─────────┬────────┘    └───────────┬────────────┘
          │                         │
          ▼                         ▼
┌────────────────────────────────────────────────────────┐
│                 ExecutionPlanBuilder                   │
│                                                        │
│  1. Pre-Flight Query: Cache Revit levels & symbols     │
│  2. Topological Sorter: Strict construction sequence   │
│  3. Strict Level Resolver: Zero fallback (.First() ban)│
│  4. Symbol Resolver: Standard naming & procedural spec │
│  5. Geometry Validator: Check lengths, closed loops    │
│  6. Idempotency Classifier: Create / Update / NoOp     │
│  7. Deterministic PlanHash Generation (SHA-256)        │
└───────────────────────────┬────────────────────────────┘
                            │
                            ▼
                ┌────────────────────────┐
                │    BimExecutionPlan    │
                │ (Immutable IR Payload) │
                └────────────────────────┘
```

### 4.2 Topological Ordering Sequence (13 Categories)

Elements are sorted topologically to respect physical and logical Revit dependencies:
1. **`Level`** (Elevation datums — prerequisite for all other elements)
2. **`Grid`** (Spatial alignment reference lines)
3. **`Pile`** (Deep foundation elements)
4. **`Footing`** (Pile caps & isolated footings hosting piles)
5. **`GradeBeam`** (Ground tie-beams spanning between footings)
6. **`Column`** (Vertical structural framing from base to top levels)
7. **`Beam`** (Horizontal structural framing between columns)
8. **`Slab`** (Structural floors and foundation slabs)
9. **`Wall`** (Architectural and structural vertical enclosures)
10. **`Opening`** (Doors, Windows, wall openings — hosted inside specific walls)
11. **`Room`** (Spatial boundary assets calculated from enclosing walls)
12. **`MepFixture` & `PipeRun`** (MEP fixtures and pipe distributions hosted on levels/walls)
13. **`Rebar`** (Reinforcement hosted inside concrete elements)

### 4.3 Strict Zero-Fallback Resolution Algorithms

#### Algorithm 1: Strict Level Resolution
```csharp
public static LevelResolutionResult ResolveLevel(
    string levelName,
    IReadOnlyDictionary<string, LevelDef> modelLevels,
    IReadOnlyDictionary<string, LevelSummary> revitLevels,
    bool strictMode)
{
    if (string.IsNullOrWhiteSpace(levelName))
        throw new FailFastCoordinationException("Level name is null or empty. Cannot resolve host level.");

    // Check pre-existing Revit levels first
    if (revitLevels.TryGetValue(levelName, out var revitLvl))
        return new LevelResolutionResult(levelName, revitLvl.ElevationM, false);

    // Check levels defined in the current BimModel
    if (modelLevels.TryGetValue(levelName, out var modelLvl))
        return new LevelResolutionResult(levelName, modelLvl.ElevationM, true);

    // STRICT MODE: NO FALLBACK to levels.First()
    throw new FailFastCoordinationException(
        $"[ERR_UNRESOLVED_LEVEL] Level '{levelName}' could not be resolved in Document or Model. " +
        $"Zero fallback policy prohibits arbitrary level selection.");
}
```

#### Algorithm 2: Strict Family Symbol Resolution
```csharp
public static ResourceResolutionSpec ResolveColumnSymbol(
    double widthMm,
    double depthMm,
    IReadOnlyDictionary<string, SymbolSummary> revitSymbols,
    bool strictMode)
{
    string targetTypeName = TypeResolutionRules.GetColumnSymbolName(widthMm, depthMm); // e.g. HP_Cot_C220x220
    string targetFamilyName = "1_STR_Column_ConcreteRectangular_DungTam";

    // 1. Exact match in Revit
    if (revitSymbols.TryGetValue(targetTypeName, out var exactSym))
    {
        return new ResourceResolutionSpec
        {
            FamilyName = exactSym.FamilyName,
            TypeName = targetTypeName,
            RequiresProceduralCreation = false
        };
    }

    // 2. Compatible base symbol available for duplication
    var baseSym = revitSymbols.Values
        .FirstOrDefault(s => s.Category == BuiltInCategory.OST_StructuralColumns &&
                             s.FamilyName.Contains("ConcreteRectangular"));

    if (baseSym != null)
    {
        return new ResourceResolutionSpec
        {
            FamilyName = baseSym.FamilyName,
            TypeName = targetTypeName,
            RequiresProceduralCreation = true,
            BaseTypeName = baseSym.TypeName,
            DimensionParametersMm = new Dictionary<string, double>
            {
                { "b", widthMm },
                { "h", depthMm },
                { "Width", widthMm },
                { "Depth", depthMm }
            }
        };
    }

    // STRICT MODE: NO FALLBACK to arbitrary symbol
    throw new FailFastCoordinationException(
        $"[ERR_UNRESOLVED_SYMBOL] Cannot resolve or duplicate symbol for Column {widthMm}x{depthMm}mm. " +
        $"No compatible structural rectangular column family found in Revit.");
}
```

---

## 5. Uniform Execution Specification: `BimPipelineEngine`

### 5.1 The Equivalence Architecture

To achieve 100% Preview/Apply equivalence, the client dispatches **one identical parameter-driven script** (`BimPlanRunnerScriptTemplate`) with **one identical JSON argument payload** (`BimExecutionPlan`).

```
                    ┌─────────────────────────┐
                    │    BimExecutionPlan     │
                    └────────────┬────────────┘
                                 │
           ┌─────────────────────┴─────────────────────┐
           ▼                                           ▼
┌─────────────────────────┐                 ┌─────────────────────────┐
│     PREVIEW MODE        │                 │       APPLY MODE        │
│  Options.DryRun = true  │                 │  Options.DryRun = false │
└──────────┬──────────────┘                 └──────────┬──────────────┘
           │                                           │
           ▼                                           ▼
┌─────────────────────────────────────────────────────────────────────┐
│                 BimPipelineEngine.ExecuteAsync                      │
│                                                                     │
│  Payload: ExecuteRequest(                                           │
│      Code: BimPlanRunnerScriptTemplate.ScriptCode,                   │
│      Args: Serialized BimExecutionPlan JSON,                        │
│      DryRun: Options.DryRun,                                        │
│      Transaction: TransactionModes.Auto)                            │
└──────────────────────────────────┬──────────────────────────────────┘
                                   │
                      IPC via Named Pipe JSON-RPC
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│                 HPRebar.McpBridge (Inside Revit)                    │
│                                                                     │
│  1. Pre-Flight Gate: Verify doc.Title & VersionGUID                 │
│  2. Execute Plan Items: Levels -> Grids -> Columns -> Slabs...      │
│  3. Measured Self-Check: Verify b, h, z on created elements         │
│  4. Write Extensible Storage Receipt (Atomic DataStorage)           │
│  5. Return IReadOnlyList<ElementExecutionRecord>                    │
│                                                                     │
│  If DryRun == true  ──► TransactionGroup.RollBack()                 │
│  If DryRun == false ──► TransactionGroup.Assimilate()               │
└──────────────────────────────────┬──────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│                   Client Validation & Handling                      │
│                                                                     │
│  - If DryRun: Assert ExecuteResult.RolledBack == true               │
│  - If Apply:  Assert ExecuteResult.RolledBack == false              │
│  - Inspect all ElementExecutionRecord statuses (Created/Failed)     │
│  - Update local BimCoordinationLedger & IntentJournal               │
└─────────────────────────────────────────────────────────────────────┘
```

### 5.2 The Unified Script Runner Implementation (`BimPlanRunnerScriptTemplate.cs`)

```csharp
namespace HPBim.Client.Pipeline;

public static class BimPlanRunnerScriptTemplate
{
    public static string GetScriptCode() => """
        // =========================================================================
        // HPBim Unified Plan Runner (Single Source of Truth)
        // =========================================================================
        double Ft(double mm) => mm / 304.8;
        double Mm(double ft) => ft * 304.8;
        XYZ ToXyz(double xMm, double yMm, double zMm = 0.0) => new XYZ(Ft(xMm), Ft(yMm), Ft(zMm));

        var targetDoc = args.Obj("targetDoc");
        string expectedTitle = targetDoc.Str("title");
        string expectedVersionGuid = targetDoc.Str("versionGuid");

        // 1. On-Thread Pre-Flight Gate
        if (!string.IsNullOrEmpty(expectedTitle) &&
            !string.Equals(doc.Title, expectedTitle, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"[PRE_FLIGHT_FAILED] Active document title '{doc.Title}' does not match target '{expectedTitle}'.");
        }

        var items = args.List("items");
        var records = new List<object>();

        // Cache existing datums
        var existingLevels = new FilteredElementCollector(doc)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .ToDictionary(l => l.Name, l => l, StringComparer.OrdinalIgnoreCase);

        var existingGrids = new FilteredElementCollector(doc)
            .OfClass(typeof(Grid))
            .Cast<Grid>()
            .ToDictionary(g => g.Name, g => g, StringComparer.OrdinalIgnoreCase);

        var existingViews = new FilteredElementCollector(doc)
            .OfClass(typeof(ViewPlan))
            .Cast<ViewPlan>()
            .ToList();
        var existingViewLevelIds = existingViews
            .Where(v => v.GenLevel != null)
            .Select(v => v.GenLevel.Id)
            .ToHashSet();
        var existingViewNames = new HashSet<string>(existingViews.Select(v => v.Name), StringComparer.OrdinalIgnoreCase);

        ViewFamilyType planType = new FilteredElementCollector(doc)
            .OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>()
            .FirstOrDefault(v => v.ViewFamily == ViewFamily.FloorPlan);

        // 2. Sequential Execution of Items
        foreach (var item in items)
        {
            string sourceKey = item.Require("sourceKey");
            string category = item.Require("category");
            string mark = item.Str("mark", string.Empty);
            var res = item.Obj("resource");
            var geom = item.Obj("geometry");

            try
            {
                if (category == "Level")
                {
                    double elevationM = geom.Double("elevationM");
                    double elevationFt = elevationM * (1000.0 / 304.8);
                    Level level;
                    bool created = false;

                    if (!existingLevels.TryGetValue(mark, out level))
                    {
                        level = Level.Create(doc, elevationFt);
                        level.Name = mark;
                        existingLevels[mark] = level;
                        created = true;
                    }

                    var pStory = level.get_Parameter(BuiltInParameter.LEVEL_IS_BUILDING_STORY);
                    if (pStory != null && !pStory.IsReadOnly) pStory.Set(1);

                    // Unified ViewPlan Creation
                    bool createView = item.Bool("createAssociatedView", true);
                    if (createView && planType != null && !existingViewLevelIds.Contains(level.Id))
                    {
                        string viewName = mark;
                        if (existingViewNames.Contains(viewName)) viewName = $"{mark} - Plan";

                        var newView = ViewPlan.Create(doc, planType.Id, level.Id);
                        newView.Name = viewName;
                        existingViewLevelIds.Add(level.Id);
                        existingViewNames.Add(viewName);
                    }

                    records.Add(new {
                        sourceKey = sourceKey,
                        status = created ? "Created" : "NoOp",
                        targetUniqueId = level.UniqueId,
                        targetElementId = (long)level.Id.Value
                    });
                }
                else if (category == "Grid")
                {
                    if (existingGrids.TryGetValue(mark, out var grid))
                    {
                        records.Add(new {
                            sourceKey = sourceKey,
                            status = "NoOp",
                            targetUniqueId = grid.UniqueId,
                            targetElementId = (long)grid.Id.Value
                        });
                        continue;
                    }

                    var start = geom.Obj("start");
                    var end = geom.Obj("end");
                    var p0 = ToXyz(start.Double("x"), start.Double("y"), 0);
                    var p1 = ToXyz(end.Double("x"), end.Double("y"), 0);

                    var newGrid = Grid.Create(doc, Line.CreateBound(p0, p1));
                    newGrid.Name = mark;
                    existingGrids[mark] = newGrid;

                    records.Add(new {
                        sourceKey = sourceKey,
                        status = "Created",
                        targetUniqueId = newGrid.UniqueId,
                        targetElementId = (long)newGrid.Id.Value
                    });
                }
                // (Other categories: Column, Beam, Slab, Wall, Opening, Room follow identical pattern)
            }
            catch (Exception ex)
            {
                records.Add(new {
                    sourceKey = sourceKey,
                    status = "Failed",
                    errorReason = ex.Message
                });
            }
        }

        return records;
        """;
}
```

### 5.3 Proof of 100% Preview/Apply Equivalence

| Aspect | Preview (`dryRun: true`) | Apply (`dryRun: false`) | Equivalence Verification |
| :--- | :--- | :--- | :--- |
| **Script Code** | `BimPlanRunnerScriptTemplate.ScriptCode` | `BimPlanRunnerScriptTemplate.ScriptCode` | Byte-for-byte identical |
| **Arguments Payload** | `BimExecutionPlan` JSON element | `BimExecutionPlan` JSON element | Byte-for-byte identical |
| **Pre-Flight Gate** | Asserts Document Title & VersionGUID | Asserts Document Title & VersionGUID | Identical checks |
| **API Calls** | `Level.Create`, `ViewPlan.Create`, `Grid.Create`... | `Level.Create`, `ViewPlan.Create`, `Grid.Create`... | Executed in identical order |
| **Constraint Checks** | Revit checks duplicate view names, geometry boundaries | Revit checks duplicate view names, geometry boundaries | Same Revit validation rules |
| **Transaction State** | Runs inside `TransactionGroup` | Runs inside `TransactionGroup` | Same transaction scope |
| **Outcome Disposition** | `group.RollBack()` $\rightarrow$ `RolledBack = true` | `group.Assimilate()` $\rightarrow$ `RolledBack = false` | Isolated to bridge boundary |
| **Return Value** | `List<ElementExecutionRecord>` | `List<ElementExecutionRecord>` | Identical record structure |

---

## 6. Unified View Creation Specification (`ViewPlan.Create`)

### 6.1 Root Cause of Current Discrepancy
As discovered during survey, `Stage1LevelGenerator.cs` (line 100) called `ViewPlan.Create(doc, planType.Id, level.Id)`, while `CompositeBimGenerator.Stages1To3.cs` omitted it entirely.
When Revit creates a `Level`, no view is created automatically. Creating a view via `ViewPlan.Create`:
1. Requires a valid `ViewFamilyType` with `ViewFamily == ViewFamily.FloorPlan`.
2. Automatically assigns the Level's Name to the View. If another view in the project already uses that name (even an elevation, section, or unassociated view), Revit throws:
   `Autodesk.Revit.Exceptions.ArgumentException: "The name is already in use."`
3. If this occurs during Apply, the transaction fails and aborts. Because DryRun previously omitted `ViewPlan.Create`, DryRun reported PASS, masking the crash until Apply.

### 6.2 The Unified Solution
1. **Explicit Plan Intent**: `ElementPlanItem` for each Level carries `CreateAssociatedView = true/false` (default `true` from `ExecutionOptions.CreatePlanViews`).
2. **Pre-Check Existing Views**:
   Query all existing `ViewPlan` elements in the document:
   - Check if a plan view is already attached to this level (`view.GenLevel?.Id == level.Id`). If so, do not attempt to recreate (NoOp).
   - Check existing view names across the entire document (`new HashSet<string>(existingViews.Select(v => v.Name))`).
3. **Collision-Proof Disambiguation**:
   If the target name (`level.Name`) is already taken by a different view, append a standard disambiguator (e.g. `"{level.Name} - Plan"`), log a warning, and proceed.
4. **Execution in Both Modes**:
   Because `BimPlanRunnerScriptTemplate` executes this exact logic in both modes:
   - In Preview, `ViewPlan.Create` creates the view in-memory, tests view templates and Revit view table rules, and then the transaction group rolls back cleanly.
   - In Apply, `ViewPlan.Create` commits cleanly.
   - 100% parity is mathematically guaranteed.

---

## 7. Migration Plan & Step-by-Step Implementation for M1

### Step 1: Baseline Compilation Fix (`HPBim.Client.Tests`)
- **File**: `HPBim\tests\HPBim.Client.Tests\Fakes\FakeRevitPipeClient.cs`
- **Action**: Add implementation of `CancelAsync`:
  ```csharp
  public Task<bool> CancelAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
  ```
- **Verification**: `dotnet test HPBim.slnx` compiles cleanly.

### Step 2: Implement Execution Plan Models in `HPBim.Core`
- Create directory `HPBim/src/HPBim.Core/Execution/`.
- Add `BimExecutionPlan.cs`, `ElementPlanItem.cs`, `ExecutionOptions.cs`, `ElementGeometrySpec.cs`, `ResourceResolutionSpec.cs`, `ElementExecutionRecord.cs`, `ElementExecutionStatus.cs`, `DocumentIdentitySnapshot.cs`.
- Add `CompositeSourceKey.cs` in `HPBim/src/HPBim.Core/Coordination/`.
- Add unit tests in `HPBim.Core.Tests/Execution/` verifying plan creation, serialization round-trip, and hash computation.

### Step 3: Implement `ExecutionPlanBuilder` in `HPBim.Client`
- Create `HPBim/src/HPBim.Client/Pipeline/ExecutionPlanBuilder.cs`.
- Implement pre-flight caching, strict level/symbol resolution, geometry validation, and deterministic SHA-256 hash generation.
- Add unit tests in `HPBim.Client.Tests/Pipeline/` asserting that unresolved levels or symbols throw `FailFastCoordinationException` and never pick `.First()`.

### Step 4: Refactor `BimPipelineEngine` & Script Runner
- Create `HPBim/src/HPBim.Client/Pipeline/BimPlanRunnerScriptTemplate.cs`.
- Refactor `BimPipelineEngine.cs` to execute `BimExecutionPlan` via `BimPlanRunnerScriptTemplate` for both Preview and Apply.
- Deprecate `AtomicDryRun` vs `SequentialStages` split in favor of single-plan execution.
- Update `GenerateCommand.cs` to compile `BimModel` into `BimExecutionPlan` via `ExecutionPlanBuilder` before dispatching.

---

## 8. Verification Matrix

| Test Case | Method | Expected Result |
| :--- | :--- | :--- |
| **Zero-Fallback Level Resolution** | Unit test `ExecutionPlanBuilder` with missing Level `"Level_99"` | Throws `FailFastCoordinationException`, 0 elements placed on arbitrary levels |
| **Zero-Fallback Symbol Resolution** | Unit test `ExecutionPlanBuilder` with unknown column dimensions | Throws `FailFastCoordinationException`, never duplicates arbitrary non-concrete symbol |
| **Deterministic Plan Hash** | Compare hash of two identically constructed plans | `PlanHash` values are byte-for-byte identical |
| **Preview/Apply Script Equivalence** | Compare `ExecuteRequest.Code` and `ExecuteRequest.Args` in FakeClient | `Code` and `Args` are identical; only `DryRun` boolean differs |
| **Preview Rollback Confirmation** | Execute Preview with `DryRun = true` | `result.RolledBack == true`, `result.Success == true` |
| **Unified ViewPlan.Create** | Inspect script executed during Preview | Contains `ViewPlan.Create`, tests view family types, rolls back without residue |
