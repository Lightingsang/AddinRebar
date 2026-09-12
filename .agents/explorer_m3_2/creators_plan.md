# Implementation Plan: Beam Rebar Creators & Shape Generation (Milestone M3 Part 2)

**Document**: `creators_plan.md`  
**Author**: `explorer_m3_2`  
**Date**: 2026-09-07  
**Target Solution**: `HPRebar/HPRebar.slnx`  
**Target Projects**: `HPRebar` (Revit Add-In, .NET 8 / Revit 2025–2026, multi-version R23–R27), `HPRebar.Core` (netstandard2.0)  
**Status**: Ready for Implementation  

---

## 1. Architectural Overview & Design Principles

Milestone M3 Part 2 implements the Revit rebar generation subsystem of the **Continuous Beam Rebar** feature. It bridges the pure mathematical models and calculations produced by `HPRebar.Core/BeamRebar/` into native, fully parametric, shape-driven Autodesk Revit `Rebar` elements.

```
┌────────────────────────────────────────────────────────────────────────┐
│             Pure Domain Calculations (HPRebar.Core/BeamRebar/)         │
│  - BeamContinuousStack, BeamSpan, BeamSupportNode (all mm, double)     │
│  - BeamStirrupDistributionCalculator -> IReadOnlyList<StirrupRun>      │
│  - BeamMainBarCalculator             -> IReadOnlyList<BarPolyline>     │
│  - BeamAdditionalBarCalculator       -> IReadOnlyList<BarPolyline>     │
│  - BeamSideBarCalculator             -> IReadOnlyList<BarPolyline>     │
│  - BeamSpecialBarCalculator          -> IReadOnlyList<BarPolyline>     │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ consumes domain records & coordinates
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│             Rebar Creation Service (HPRebar.BeamRebar)                 │
│  RebarCreationService.cs (Preflight Check, Staged Transactions)        │
├───────────────────────────────────┬────────────────────────────────────┤
│  1. BeamStirrupCreator.cs         │ Rebar.CreateFromRebarShape         │
│                                   │ ScaleToBox + SetLayoutAsNumber...  │
│  2. BeamMainBarCreator.cs         │ Rebar.CreateFromCurves (Standard)  │
│                                   │ 90° downward/upward hooks, splice  │
│  3. BeamAdditionalBarCreator.cs   │ Rebar.CreateFromCurves             │
│                                   │ Top support L/3, L/4, Bottom L/7   │
│  4. BeamSideBarCreator.cs         │ Rebar.CreateFromCurves (skin bars) │
│                                   │ Rebar.CreateFromCurves (cross-ties)│
│  5. BeamSpecialBarCreator.cs      │ Rebar.CreateFromRebarShape/Curves  │
│                                   │ Hanging stirrups & 45° diag ties   │
├───────────────────────────────────┴────────────────────────────────────┤
│  Shared Resolvers & Utilities                                          │
│  - RebarShapeResolver.cs (M_T1, T1, M_T10 fallback dictionary)         │
│  - RebarTypeCatalog.cs   (RebarBarType, RebarCoverType, HookResolver)  │
│  - PointMapper.cs        (Local mm (x,y,z) -> Revit world XYZ in ft)   │
│  - RebarFailureHandling  (IFailuresPreprocessor SwallowWarnings)       │
│  - RevitUnits.cs         (MmToFt, FtToMm via UnitTypeId.Millimeters)   │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ instantiates
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                       Revit Database Elements                          │
│  - Native Autodesk.Revit.DB.Structure.Rebar Elements                   │
│  - Correct Host FamilyInstance (OST_StructuralFraming)                 │
│  - Assigned 'Partition' Parameter for scheduling                       │
└────────────────────────────────────────────────────────────────────────┘
```

### Core Architecture Rules:
1. **Feature Folder Discipline**: All files reside in `HPRebar/HPRebar/Beam Rebar/` (and its subfolder `Models/`). No subfolders like `Creators/` or `Services/`.
2. **Explicit Namespaces**: Namespace is `namespace HPRebar.BeamRebar;` for root files and `namespace HPRebar.BeamRebar.Models;` for models.
3. **Strict Separation of Concerns**:
   - `HPRebar.Core` holds zero Revit references and computes all geometry, spacing, rounding, and polylines in millimetres.
   - Revit creators only handle coordinate mapping, element creation, shape resolution, and parameter assignment.
4. **Transaction Safety**:
   - `BeamRebarOrchestrator` owns the outer `TransactionGroup("Beam Rebar")`.
   - `RebarCreationService` owns discrete inner `Transaction`s per category with `RebarFailureHandling.Apply(transaction)`.
5. **Multi-Version Compatibility**: Zero deprecated APIs (no `DisplayUnitType`, no `CreateFreeForm` which was removed in R27). All length conversions use `UnitTypeId.Millimeters`.

---

## 2. Component Inventory & Specification

| # | File Name | Primary Role | Key Revit APIs | Key Core Dependencies |
|---|---|---|---|---|
| 1 | `RebarCreationService.cs` | Master orchestrator of all rebar creation passes | `Transaction`, `IFailuresPreprocessor` | `BeamContinuousStack`, `BeamStirrupSpec`, `BeamMainBarSpec`, etc. |
| 2 | `BeamStirrupCreator.cs` | Perimeter stirrups across spans and support nodes | `Rebar.CreateFromRebarShape`, `ScaleToBox`, `SetLayoutAsNumberWithSpacing` | `BeamStirrupDistributionCalculator`, `StirrupRun` |
| 3 | `BeamMainBarCreator.cs` | Continuous top & bottom bars with 90° hooks & splices | `Rebar.CreateFromCurves`, `Line.CreateBound` | `BeamMainBarCalculator`, `BarPolyline`, `Polyline3` |
| 4 | `BeamAdditionalBarCreator.cs` | Top negative support additions & bottom midspan additions (2 layers) | `Rebar.CreateFromCurves`, `Line.CreateBound` | `BeamAdditionalBarCalculator`, `BarPolyline` |
| 5 | `BeamSideBarCreator.cs` | Skin bars ($h \ge 700$) & transverse anti-buckling cross-ties | `Rebar.CreateFromCurves`, `RebarHookType` | `BeamSideBarCalculator`, `BarPolyline` |
| 6 | `BeamSpecialBarCreator.cs` | Secondary beam hanging stirrups & 45° diagonal ties | `Rebar.CreateFromRebarShape`, `Rebar.CreateFromCurves` | `BeamSpecialBarCalculator`, `BarPolyline` |
| 7 | `RebarShapeResolver.cs` | Shape family resolver with multi-template fallbacks | `FilteredElementCollector`, `RebarShape` | `ValidationResult` |
| 8 | `RebarTypeCatalog.cs` | Bar type, cover type, and hook type catalog | `RebarBarType`, `RebarCoverType`, `RebarHookType` | `RebarTypeInfo` |
| 9 | `PointMapper.cs` | 3D coordinate transformation: local mm $\to$ Revit world ft | `XYZ`, `Transform` | `Point3`, `RevitUnits` |
| 10| `Models/BeamStack.cs` | Container for continuous beam geometry, faces, and axes | `FamilyInstance`, `PlanarFace` | `BeamContinuousStack` |
| 11| `Models/BeamFaces.cs` | Per-span Revit face handles & element reference | `FamilyInstance`, `PlanarFace` | `BeamSpan` |
| 12| `Models/CreatedBeamRebar.cs` | Result container holding references to created Rebars | `Rebar` | none |

---

## 3. Detailed Component Specifications

### 3.1 `RebarCreationService.cs`
**File Path**: `HPRebar/HPRebar/Beam Rebar/RebarCreationService.cs`  
**Namespace**: `HPRebar.BeamRebar`

#### Responsibilities:
- Pre-flight checks (`CanCreate`) ensuring all required shapes and bar types exist prior to transaction opening.
- Calculation of total elements (`PlannedCount`) for progress reporting.
- Executing creation across 5 distinct inner transactions:
  1. `"Create Stirrups"`
  2. `"Create Main Bars"`
  3. `"Create Additional Bars"`
  4. `"Create Side Bars"`
  5. `"Create Special Bars"`
- Applying `RebarFailureHandling.Apply(transaction)` to each transaction to discard non-fatal Revit warnings (e.g. rebar slightly outside host, bar overlap).
- Returning `CreatedBeamRebar` containing all created `Rebar` instances.

#### C# Design & Method Signatures:
```csharp
namespace HPRebar.BeamRebar;

public static class RebarCreationService
{
    public static ValidationResult CanCreate(
        RebarShapeResolver shapes,
        BeamStack stack,
        BeamRebarSpec spec);

    public static int PlannedCount(
        BeamStack stack,
        BeamRebarSpec spec);

    public static CreatedBeamRebar Create(
        Document document,
        BeamStack stack,
        BeamRebarSpec spec,
        RebarShapeResolver shapes,
        RebarTypeCatalog catalog,
        IProgress<int>? progress = null);
}
```

#### Transaction Phasing & Rollback Isolation:
```csharp
// Inside Create():
int done = 0;
var stirrups = new List<Rebar>();
var mainBars = new List<Rebar>();
var additionalBars = new List<Rebar>();
var sideBars = new List<Rebar>();
var specialBars = new List<Rebar>();

// Phase 1: Stirrups
using (var t = new Transaction(document, "Create Stirrups"))
{
    t.Start();
    RebarFailureHandling.Apply(t);
    stirrups.AddRange(BeamStirrupCreator.Create(
        document, stack, spec.Stirrups, shapes, catalog, spec.PartitionName, () => progress?.Report(++done)));
    t.Commit();
}

// Phase 2: Main Longitudinal Bars
using (var t = new Transaction(document, "Create Main Bars"))
{
    t.Start();
    RebarFailureHandling.Apply(t);
    mainBars.AddRange(BeamMainBarCreator.Create(
        document, stack, spec.MainBars, spec.Stirrups.Diameter, catalog, spec.PartitionName, () => progress?.Report(++done)));
    t.Commit();
}

// Phase 3: Additional Bars (Support Top & Midspan Bottom)
using (var t = new Transaction(document, "Create Additional Bars"))
{
    t.Start();
    RebarFailureHandling.Apply(t);
    additionalBars.AddRange(BeamAdditionalBarCreator.Create(
        document, stack, spec.AdditionalBars, spec.Stirrups.Diameter, catalog, spec.PartitionName, () => progress?.Report(++done)));
    t.Commit();
}

// Phase 4: Side Bars & Cross-Ties (h >= 700 mm)
using (var t = new Transaction(document, "Create Side Bars"))
{
    t.Start();
    RebarFailureHandling.Apply(t);
    sideBars.AddRange(BeamSideBarCreator.Create(
        document, stack, spec.SideBars, spec.Stirrups.Diameter, spec.MainBars.BottomDiameter, catalog, spec.PartitionName, () => progress?.Report(++done)));
    t.Commit();
}

// Phase 5: Special Bars (Secondary Beam Intersections)
using (var t = new Transaction(document, "Create Special Bars"))
{
    t.Start();
    RebarFailureHandling.Apply(t);
    specialBars.AddRange(BeamSpecialBarCreator.Create(
        document, stack, spec.SpecialBars, shapes, catalog, spec.PartitionName, () => progress?.Report(++done)));
    t.Commit();
}
```

---

### 3.2 `BeamStirrupCreator.cs`
**File Path**: `HPRebar/HPRebar/Beam Rebar/BeamStirrupCreator.cs`  
**Namespace**: `HPRebar.BeamRebar`

#### Mathematical & Revit API Mapping:
- **Shape Generation**: `Rebar.CreateFromRebarShape(doc, shape, barType, host, origin, xVec, yVec)`.
- **Coordinate Space**:
  - Longitudinal axis: $\vec{X}_{beam}$ (direction of continuous beam).
  - Transverse horizontal axis: $\vec{Y}_{beam} = \vec{Z} \times \vec{X}_{beam}$ (width $b$).
  - Vertical axis: $\vec{Z} = (0, 0, 1)$ (height $h$).
- **Vectors passed to `CreateFromRebarShape`**:
  - `xVec` = $\vec{Y}_{beam}$ (`stack.NormalDirection`)
  - `yVec` = $\vec{Z}$ (`XYZ.BasisZ`)
  - Note: $\vec{xVec} \times \vec{yVec} = \vec{Y}_{beam} \times \vec{Z} = \vec{X}_{beam}$. Thus, the normal to the stirrup plane points along the beam axis!
- **Local Origin Calculation**:
  - Lower-left corner of the stirrup in beam section space at the first stirrup position:
    - $X = \text{run.StartX}$ (longitudinal station)
    - $Y = -b/2 + c$ (transverse left edge minus cover)
    - $Z = Z_{bottom} + c$ (bottom soffit elevation plus cover)
  - Transformed to Revit world XYZ via `stack.PointMapper.ToXyz(originPoint3)`.
- **Sizing & Array Expansion**:
  - Width: $W_{ft} = \text{RevitUnits.MmToFt}(b - 2c)$
  - Height: $H_{ft} = \text{RevitUnits.MmToFt}(h - 2c)$
  - `accessor.ScaleToBox(originXyz, W_ft, H_ft)`
  - `accessor.SetLayoutAsNumberWithSpacing(run.Count, RevitUnits.MmToFt(run.Spacing), barsOnNormalSide: true, includeFirstBar: true, includeLastBar: true)`

#### C# Implementation Specification:
```csharp
namespace HPRebar.BeamRebar;

public static class BeamStirrupCreator
{
    public static IReadOnlyList<Rebar> Create(
        Document document,
        BeamStack stack,
        BeamStirrupSpec spec,
        RebarShapeResolver shapes,
        RebarTypeCatalog catalog,
        string partitionName,
        Action? onBarCreated = null)
    {
        var shape = shapes.MainStirrup();
        if (shape is null)
            throw new InvalidOperationException("Rectangular stirrup shape (M_T1/T1) not loaded in document.");

        var barType = catalog.FindBarType(spec.BarTypeName, spec.Diameter);
        if (barType is null)
            throw new InvalidOperationException($"RebarBarType for stirrups ({spec.Diameter} mm) not found.");

        var created = new List<Rebar>();

        // 1. Spans (Clear Spans)
        for (int i = 0; i < stack.Spans.Count; i++)
        {
            var span = stack.Spans[i];
            var hostElement = stack.SpanFaces[i].Element;
            var runs = BeamStirrupDistributionCalculator.ComputeSpanRuns(span.LengthClear, spec, span.IsCantilever);

            foreach (var run in runs)
            {
                if (run.Count <= 0) continue;

                var rebar = PlaceStirrupRun(
                    document, hostElement, shape, barType.BarType, stack, span, run, partitionName);

                created.Add(rebar);
                onBarCreated?.Invoke();
            }
        }

        // 2. Interior Support Nodes (if enabled)
        if (spec.IncludeStirrupsInNodes && stack.Supports.Count > 2)
        {
            for (int k = 1; k < stack.Supports.Count - 1; k++)
            {
                var support = stack.Supports[k];
                var nodeRun = BeamStirrupDistributionCalculator.ComputeNodeRun(support.Width, spec.Cover, spec.NodeSpacing);
                if (nodeRun.Count <= 0) continue;

                // Host on the adjacent span element
                var hostSpan = stack.Spans[k - 1];
                var hostElement = stack.SpanFaces[k - 1].Element;

                var rebar = PlaceNodeStirrupRun(
                    document, hostElement, shape, barType.BarType, stack, hostSpan, support, nodeRun, spec.Cover, partitionName);

                created.Add(rebar);
                onBarCreated?.Invoke();
            }
        }

        return created;
    }

    private static Rebar PlaceStirrupRun(
        Document doc,
        Element host,
        RebarShape shape,
        RebarBarType barType,
        BeamStack stack,
        BeamSpan span,
        StirrupRun run,
        string partitionName)
    {
        double widthMm = span.Width - (2.0 * span.Cover);
        double heightMm = span.Height - (2.0 * span.Cover);

        // Lower-left corner of first stirrup
        var localOrigin = new Point3(
            run.StartX,
            -(span.Width / 2.0) + span.Cover,
            span.BottomElevation + span.Cover);

        XYZ originXyz = stack.PointMapper.ToXyz(localOrigin);
        XYZ xVec = stack.NormalDirection; // Y_beam
        XYZ yVec = XYZ.BasisZ;            // Z

        var rebar = Rebar.CreateFromRebarShape(doc, shape, barType, host, originXyz, xVec, yVec);
        var accessor = rebar.GetShapeDrivenAccessor();

        accessor.ScaleToBox(originXyz, RevitUnits.MmToFt(widthMm), RevitUnits.MmToFt(heightMm));
        accessor.SetLayoutAsNumberWithSpacing(
            run.Count, RevitUnits.MmToFt(run.Spacing), true, true, true);

        SetPartition(rebar, partitionName);
        return rebar;
    }

    private static Rebar PlaceNodeStirrupRun(
        Document doc,
        Element host,
        RebarShape shape,
        RebarBarType barType,
        BeamStack stack,
        BeamSpan refSpan,
        BeamSupportNode support,
        StirrupRun run,
        double coverMm,
        string partitionName)
    {
        double widthMm = refSpan.Width - (2.0 * coverMm);
        double heightMm = refSpan.Height - (2.0 * coverMm);

        double stationX = support.LeftFaceX + run.StartOffset;
        var localOrigin = new Point3(
            stationX,
            -(refSpan.Width / 2.0) + coverMm,
            refSpan.BottomElevation + coverMm);

        XYZ originXyz = stack.PointMapper.ToXyz(localOrigin);
        XYZ xVec = stack.NormalDirection;
        XYZ yVec = XYZ.BasisZ;

        var rebar = Rebar.CreateFromRebarShape(doc, shape, barType, host, originXyz, xVec, yVec);
        var accessor = rebar.GetShapeDrivenAccessor();

        accessor.ScaleToBox(originXyz, RevitUnits.MmToFt(widthMm), RevitUnits.MmToFt(heightMm));
        accessor.SetLayoutAsNumberWithSpacing(
            run.Count, RevitUnits.MmToFt(run.Spacing), true, true, true);

        SetPartition(rebar, partitionName);
        return rebar;
    }

    internal static void SetPartition(Element rebar, string partitionName)
    {
        if (string.IsNullOrWhiteSpace(partitionName)) return;
        var param = rebar.LookupParameter("Partition");
        if (param is { IsReadOnly: false })
            param.Set(partitionName);
    }
}
```

---

### 3.3 `BeamMainBarCreator.cs`
**File Path**: `HPRebar/HPRebar/Beam Rebar/BeamMainBarCreator.cs`  
**Namespace**: `HPRebar.BeamRebar`

#### Mathematical & Revit API Mapping:
- **Shape Generation**: `Rebar.CreateFromCurves` with `RebarStyle.Standard`.
- **Normal Vector**:
  - The 90° downward hooks at exterior ends bend in the vertical plane.
  - The vertical plane is spanned by $\vec{X}_{beam}$ (longitudinal) and $\vec{Z}$ (vertical).
  - The vector perpendicular to this plane is $\vec{Y}_{beam} = \vec{Z} \times \vec{X}_{beam}$ (`stack.NormalDirection`).
  - Passing `norm = stack.NormalDirection` strictly satisfies Revit API planarity requirements!
- **Hook Handling**:
  - Pass `startHook: null, endHook: null`!
  - **Rationale**: `BeamMainBarCalculator` already embeds the exact physical 90° anchorage hook legs into the polyline vertices ($Z - \text{hookLength}$). Creating explicit curve segments ensures exact anchorage length compliance with structural drawings without relying on template-dependent hook radius parameters. Revit's shape matching engine automatically recognizes the 3- or 4-segment curve as shape `M_02` (one-hook) or `M_04` (two-hooks) or synthesizes a parametric shape!
- **Splicing & Division**:
  - Spans $> 11.7$ m (or custom stock length) are automatically divided into staggered lap splices (Group A and Group B) by `BeamMainBarCalculator`. Each segment is instantiated as an independent `Rebar` hosted on the span it occupies.
- **Depth Step Support**:
  - Beams with varying cross-section depths across spans have bottom bars anchored or stepped at support faces according to `BeamMainBarCalculator.ComputeBottomMainBars`.

#### C# Implementation Specification:
```csharp
namespace HPRebar.BeamRebar;

public static class BeamMainBarCreator
{
    public static IReadOnlyList<Rebar> Create(
        Document document,
        BeamStack stack,
        BeamMainBarSpec spec,
        double stirrupDiameterMm,
        RebarTypeCatalog catalog,
        string partitionName,
        Action? onBarCreated = null)
    {
        var created = new List<Rebar>();

        // 1. Top Main Bars
        var topBars = BeamMainBarCalculator.ComputeTopMainBars(stack.ContinuousStack, spec, stirrupDiameterMm);
        var topBarType = catalog.FindBarType(spec.TopBarTypeName, spec.TopDiameter);
        if (topBarType is null)
            throw new InvalidOperationException($"RebarBarType for top main bars ({spec.TopDiameter} mm) not found.");

        foreach (var bar in topBars)
        {
            var rebar = CreateBarFromPolyline(
                document, stack, bar, topBarType.BarType, partitionName);
            created.Add(rebar);
            onBarCreated?.Invoke();
        }

        // 2. Bottom Main Bars
        var bottomBars = BeamMainBarCalculator.ComputeBottomMainBars(stack.ContinuousStack, spec, stirrupDiameterMm);
        var bottomBarType = catalog.FindBarType(spec.BottomBarTypeName, spec.BottomDiameter);
        if (bottomBarType is null)
            throw new InvalidOperationException($"RebarBarType for bottom main bars ({spec.BottomDiameter} mm) not found.");

        foreach (var bar in bottomBars)
        {
            var rebar = CreateBarFromPolyline(
                document, stack, bar, bottomBarType.BarType, partitionName);
            created.Add(rebar);
            onBarCreated?.Invoke();
        }

        return created;
    }

    internal static Rebar CreateBarFromPolyline(
        Document document,
        BeamStack stack,
        BarPolyline bar,
        RebarBarType defaultBarType,
        string partitionName)
    {
        int hostIdx = bar.HostSpanIndex >= 0 && bar.HostSpanIndex < stack.SpanFaces.Count
            ? bar.HostSpanIndex
            : 0;

        var hostElement = stack.SpanFaces[hostIdx].Element;
        var curves = BuildCurves(bar.Polyline, stack.PointMapper);

        var rebar = Rebar.CreateFromCurves(
            document,
            RebarStyle.Standard,
            defaultBarType,
            startHook: null,
            endHook: null,
            host: hostElement,
            norm: stack.NormalDirection,
            curves: curves,
            startHookOrient: RebarHookOrientation.Right,
            endHookOrient: RebarHookOrientation.Right,
            useExistingShapeIfPossible: true,
            createNewShape: true);

        BeamStirrupCreator.SetPartition(rebar, partitionName);
        return rebar;
    }

    public static IList<Curve> BuildCurves(Polyline3 polyline, PointMapper mapper)
    {
        var simplified = polyline.Simplify(1.0); // 1.0 mm minimum segment limit
        if (simplified.Points.Count < 2)
            throw new InvalidOperationException("Polyline collapsed to fewer than 2 points.");

        var curves = new List<Curve>(simplified.Points.Count - 1);
        for (int i = 1; i < simplified.Points.Count; i++)
        {
            var p0 = mapper.ToXyz(simplified.Points[i - 1]);
            var p1 = mapper.ToXyz(simplified.Points[i]);
            curves.Add(Line.CreateBound(p0, p1));
        }

        return curves;
    }
}
```

---

### 3.4 `BeamAdditionalBarCreator.cs`
**File Path**: `HPRebar/HPRebar/Beam Rebar/BeamAdditionalBarCreator.cs`  
**Namespace**: `HPRebar.BeamRebar`

#### Detailing & Multi-Layer Logic:
1. **Top Negative Support Bars**:
   - Centered over column/wall support nodes.
   - Extend $L/3$ (Layer 1) and $L/4$ (Layer 2) into adjacent left and right clear spans.
   - At exterior columns: 90° hook bends downward.
   - **Layer 1**: $Z_1 = Z_{top} - cover - d_{stirrup} - d_{bar}/2$.
   - **Layer 2**: $Z_2 = Z_1 - \text{LayerGap}$ (default $\Delta Z = 50$ mm).
2. **Bottom Positive Midspan Bars**:
   - Placed in the midspan tensile zone between support faces.
   - Cutoff distance: starts at $L/7$ from left support face, terminates at $L/7$ from right support face.
   - **Layer 1**: $Z_1 = Z_{bottom} + cover + d_{stirrup} + d_{bar}/2$.
   - **Layer 2**: $Z_2 = Z_1 + \text{LayerGap}$.
3. **Revit API Creation**:
   - Created via `Rebar.CreateFromCurves` with `norm = stack.NormalDirection`.
   - Host assigned to the nearest span element.

#### C# Implementation Specification:
```csharp
namespace HPRebar.BeamRebar;

public static class BeamAdditionalBarCreator
{
    public static IReadOnlyList<Rebar> Create(
        Document document,
        BeamStack stack,
        BeamAdditionalBarSpec spec,
        double stirrupDiameterMm,
        RebarTypeCatalog catalog,
        string partitionName,
        Action? onBarCreated = null)
    {
        var created = new List<Rebar>();

        // 1. Support Top Bars
        var topBars = BeamAdditionalBarCalculator.ComputeSupportTopBars(stack.ContinuousStack, spec, stirrupDiameterMm);
        foreach (var bar in topBars)
        {
            var barType = catalog.FindBarType(bar.BarTypeName, bar.Diameter);
            if (barType is null) continue;

            var rebar = BeamMainBarCreator.CreateBarFromPolyline(
                document, stack, bar, barType.BarType, partitionName);
            created.Add(rebar);
            onBarCreated?.Invoke();
        }

        // 2. Span Bottom Bars
        var bottomBars = BeamAdditionalBarCalculator.ComputeSpanBottomBars(
            stack.ContinuousStack,
            new BeamAdditionalBottomBarSpec { SpanBottomBars = spec.SpanBottomBars },
            stirrupDiameterMm);

        foreach (var bar in bottomBars)
        {
            var barType = catalog.FindBarType(bar.BarTypeName, bar.Diameter);
            if (barType is null) continue;

            var rebar = BeamMainBarCreator.CreateBarFromPolyline(
                document, stack, bar, barType.BarType, partitionName);
            created.Add(rebar);
            onBarCreated?.Invoke();
        }

        return created;
    }
}
```

---

### 3.5 `BeamSideBarCreator.cs`
**File Path**: `HPRebar/HPRebar/Beam Rebar/BeamSideBarCreator.cs`  
**Namespace**: `HPRebar.BeamRebar`

#### Deep Beam Standards (TCVN 5574:2018 §10.3.2 / ACI 318 §9.7.2.3):
- Triggered when beam height $h \ge 700$ mm.
- **Longitudinal Skin Bars**:
  - Rows placed along left and right lateral faces with vertical spacing $\le 300$ mm.
  - Normal vector: `stack.NormalDirection` ($\vec{Y}_{beam}$).
- **Transverse Anti-Buckling Cross-Ties**:
  - Connect opposite side bars across the beam web to prevent lateral buckling.
  - Spacing along beam axis: default 400 mm.
  - End hooks: 90° at one end, 135° at opposite end, alternating orientation on successive ties.
  - Normal vector: `stack.BeamDirection` ($\vec{X}_{beam}$, perpendicular to the transverse cross-section plane).

#### C# Implementation Specification:
```csharp
namespace HPRebar.BeamRebar;

public static class BeamSideBarCreator
{
    public static IReadOnlyList<Rebar> Create(
        Document document,
        BeamStack stack,
        BeamSideBarSpec spec,
        double stirrupDiameterMm,
        double mainBarDiameterMm,
        RebarTypeCatalog catalog,
        string partitionName,
        Action? onBarCreated = null)
    {
        var created = new List<Rebar>();

        // 1. Longitudinal Side (Skin) Bars
        var sideBars = BeamSideBarCalculator.ComputeLongitudinalSideBars(
            stack.ContinuousStack, spec, stirrupDiameterMm, mainBarDiameterMm);

        var sideBarType = catalog.FindBarType(spec.SideBarTypeName, spec.Diameter);
        if (sideBarType is not null)
        {
            foreach (var bar in sideBars)
            {
                var rebar = BeamMainBarCreator.CreateBarFromPolyline(
                    document, stack, bar, sideBarType.BarType, partitionName);
                created.Add(rebar);
                onBarCreated?.Invoke();
            }
        }

        // 2. Transverse Cross-Ties
        if (spec.IncludeCrossTies)
        {
            var crossTies = BeamSideBarCalculator.ComputeCrossTies(
                stack.ContinuousStack, spec, stirrupDiameterMm, mainBarDiameterMm);

            var tieBarType = catalog.FindBarType(spec.CrossTieBarTypeName, spec.CrossTieDiameter);
            if (tieBarType is not null)
            {
                foreach (var tie in crossTies)
                {
                    int hostIdx = tie.HostSpanIndex >= 0 && tie.HostSpanIndex < stack.SpanFaces.Count
                        ? tie.HostSpanIndex
                        : 0;
                    var hostElement = stack.SpanFaces[hostIdx].Element;

                    var curves = BeamMainBarCreator.BuildCurves(tie.Polyline, stack.PointMapper);

                    // Cross ties lie in transverse Y-Z plane -> normal is BeamDirection (X_beam)
                    var rebar = Rebar.CreateFromCurves(
                        document,
                        RebarStyle.StirrupTie,
                        tieBarType.BarType,
                        startHook: null,
                        endHook: null,
                        host: hostElement,
                        norm: stack.BeamDirection,
                        curves: curves,
                        startHookOrient: RebarHookOrientation.Right,
                        endHookOrient: RebarHookOrientation.Right,
                        useExistingShapeIfPossible: true,
                        createNewShape: true);

                    BeamStirrupCreator.SetPartition(rebar, partitionName);
                    created.Add(rebar);
                    onBarCreated?.Invoke();
                }
            }
        }

        return created;
    }
}
```

---

### 3.6 `BeamSpecialBarCreator.cs`
**File Path**: `HPRebar/HPRebar/Beam Rebar/BeamSpecialBarCreator.cs`  
**Namespace**: `HPRebar.BeamRebar`

#### Secondary Beam Intersection Details:
1. **Concentrated Hanging Stirrups ("Cốt treo")**:
   - Secondary beams framing into the primary beam web induce concentrated downward shear loads.
   - Hanging stirrups flank the intersection joint (e.g. 3 pairs at 50 mm spacing on left and right).
   - Created via `Rebar.CreateFromRebarShape` (shape `M_T1`, scaled to $w, h$, placed at each station $X$, single bar layout) or via `Rebar.CreateFromCurves` for the 5-point closed rectangular curve loop.
2. **45° Diagonal Bent Ties ("Thép vai bò")**:
   - Placed under the secondary beam soffit at 45° angle.
   - Polyline: horizontal anchor leg $\to$ 45° downward incline $\to$ bottom horizontal leg under joint $\to$ 45° upward incline $\to$ horizontal anchor leg.
   - Clamped within span bounds so anchor legs never protrude outside the clear span.
   - Normal vector: `stack.NormalDirection` ($\vec{Y}_{beam}$).

#### C# Implementation Specification:
```csharp
namespace HPRebar.BeamRebar;

public static class BeamSpecialBarCreator
{
    public static IReadOnlyList<Rebar> Create(
        Document document,
        BeamStack stack,
        BeamSpecialBarSpec spec,
        RebarShapeResolver shapes,
        RebarTypeCatalog catalog,
        string partitionName,
        Action? onBarCreated = null)
    {
        var created = new List<Rebar>();

        // 1. Hanging Stirrups
        if (spec.EnableHangingStirrups && stack.ContinuousStack.SecondaryIntersections.Count > 0)
        {
            var hangingBars = BeamSpecialBarCalculator.ComputeHangingStirrups(stack.ContinuousStack, spec);
            var shape = shapes.MainStirrup();
            var barType = catalog.FindBarType(spec.HangingStirrupTypeName, spec.HangingStirrupDiameter);

            if (shape is not null && barType is not null)
            {
                foreach (var bar in hangingBars)
                {
                    int hostIdx = bar.HostSpanIndex >= 0 && bar.HostSpanIndex < stack.SpanFaces.Count
                        ? bar.HostSpanIndex
                        : 0;
                    var hostElement = stack.SpanFaces[hostIdx].Element;
                    var span = stack.Spans[hostIdx];

                    double widthMm = span.Width - (2.0 * span.Cover);
                    double heightMm = span.Height - (2.0 * span.Cover);

                    var localOrigin = new Point3(
                        bar.StartX,
                        -(span.Width / 2.0) + span.Cover,
                        span.BottomElevation + span.Cover);

                    XYZ originXyz = stack.PointMapper.ToXyz(localOrigin);
                    XYZ xVec = stack.NormalDirection;
                    XYZ yVec = XYZ.BasisZ;

                    var rebar = Rebar.CreateFromRebarShape(
                        document, shape, barType.BarType, hostElement, originXyz, xVec, yVec);

                    var accessor = rebar.GetShapeDrivenAccessor();
                    accessor.ScaleToBox(originXyz, RevitUnits.MmToFt(widthMm), RevitUnits.MmToFt(heightMm));
                    accessor.SetLayoutAsSingle();

                    BeamStirrupCreator.SetPartition(rebar, partitionName);
                    created.Add(rebar);
                    onBarCreated?.Invoke();
                }
            }
        }

        // 2. 45° Diagonal Bent Ties
        if (spec.EnableDiagonalTies && stack.ContinuousStack.SecondaryIntersections.Count > 0)
        {
            var diagBars = BeamSpecialBarCalculator.ComputeDiagonalTies(stack.ContinuousStack, spec);
            var barType = catalog.FindBarType(spec.DiagonalTieTypeName, spec.DiagonalTieDiameter);

            if (barType is not null)
            {
                foreach (var bar in diagBars)
                {
                    var rebar = BeamMainBarCreator.CreateBarFromPolyline(
                        document, stack, bar, barType.BarType, partitionName);
                    created.Add(rebar);
                    onBarCreated?.Invoke();
                }
            }
        }

        return created;
    }
}
```

---

### 3.7 `RebarShapeResolver.cs` & `RebarTypeCatalog.cs`
**File Path**: `HPRebar/HPRebar/Beam Rebar/RebarShapeResolver.cs` & `RebarTypeCatalog.cs`  
**Namespace**: `HPRebar.BeamRebar`

#### RebarShapeResolver Design:
- Builds a fast, case-insensitive dictionary of all loaded `RebarShape` families in the document.
- Resolves standard closed rectangular stirrups with comprehensive aliases:
  - Candidates: `"M_T1"`, `"T1"`, `"01"`, `"M_01"`, `"Rebar Shape 1"`
- Resolves cross-ties:
  - Candidates: `"M_T10"`, `"T10"`, `"M_T10B"`, `"M_T10C"`
- `Require(...)` pre-flight validation prevents transactions from opening if vital shapes are absent.

```csharp
namespace HPRebar.BeamRebar;

public sealed class RebarShapeResolver
{
    private static readonly string[] RectangularStirrupNames = { "M_T1", "T1", "01", "M_01", "Rebar Shape 1" };
    private static readonly string[] CrossTieNames = { "M_T10", "T10", "M_T10B", "M_T10C", "10" };

    private readonly IReadOnlyDictionary<string, RebarShape> _shapes;

    private RebarShapeResolver(IReadOnlyDictionary<string, RebarShape> shapes) => _shapes = shapes;

    public static RebarShapeResolver Load(Document document)
    {
        var shapes = new FilteredElementCollector(document)
            .OfClass(typeof(RebarShape))
            .Cast<RebarShape>()
            .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        return new RebarShapeResolver(shapes);
    }

    public RebarShape? MainStirrup() => FindFirst(RectangularStirrupNames);

    public RebarShape? CrossTie(int tieType = 0)
    {
        string specific = tieType switch
        {
            1 => "M_T10B",
            2 => "M_T10",
            3 => "M_T10C",
            _ => "M_T10"
        };
        return Find(specific) ?? FindFirst(CrossTieNames);
    }

    public ValidationResult Require(bool needsCrossTies, bool needsSpecialStirrups)
    {
        if (MainStirrup() is null)
            return ValidationResult.Fail("Rectangular stirrup shape (M_T1 or T1) is not loaded in the project.");

        if (needsCrossTies && CrossTie() is null)
            return ValidationResult.Fail("Cross-tie shape (M_T10 or T10) is not loaded in the project.");

        return ValidationResult.Ok();
    }

    private RebarShape? Find(string name) => _shapes.TryGetValue(name, out var shape) ? shape : null;

    private RebarShape? FindFirst(IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            if (_shapes.TryGetValue(name, out var shape))
                return shape;
        }
        return null;
    }
}
```

#### RebarTypeCatalog Design:
```csharp
namespace HPRebar.BeamRebar;

public sealed class RebarTypeCatalog
{
    private readonly IReadOnlyList<RebarTypeInfo> _barTypes;
    private readonly IReadOnlyList<RebarCoverType> _coverTypes;
    private readonly IReadOnlyList<RebarHookType> _hookTypes;

    public RebarTypeCatalog(Document doc)
    {
        _barTypes = new FilteredElementCollector(doc)
            .OfClass(typeof(RebarBarType))
            .Cast<RebarBarType>()
            .Select(b => new RebarTypeInfo
            {
                Name = b.Name,
                DiameterMm = RevitUnits.FtToMm(b.get_Parameter(BuiltInParameter.REBAR_BAR_DIAMETER).AsDouble()),
                BarType = b
            })
            .OrderBy(b => b.DiameterMm)
            .ToList();

        _coverTypes = new FilteredElementCollector(doc)
            .WhereElementIsElementType()
            .OfClass(typeof(RebarCoverType))
            .Cast<RebarCoverType>()
            .OrderBy(c => c.CoverDistance)
            .ToList();

        _hookTypes = new FilteredElementCollector(doc)
            .OfClass(typeof(RebarHookType))
            .Cast<RebarHookType>()
            .ToList();
    }

    public IReadOnlyList<RebarTypeInfo> BarTypes => _barTypes;
    public IReadOnlyList<RebarCoverType> CoverTypes => _coverTypes;

    public RebarTypeInfo? FindBarType(string name, double targetDiameterMm)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            var match = _barTypes.FirstOrDefault(b => b.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (match is not null) return match;
        }

        // Match closest diameter within 0.5 mm tolerance
        var closest = _barTypes.FirstOrDefault(b => Math.Abs(b.DiameterMm - targetDiameterMm) < 0.5);
        if (closest is not null) return closest;

        // Fallback to closest overall
        return _barTypes.OrderBy(b => Math.Abs(b.DiameterMm - targetDiameterMm)).FirstOrDefault();
    }

    public RebarHookType? FindHook(int angleDegrees)
    {
        double angleRad = angleDegrees * Math.PI / 180.0;
        return _hookTypes.FirstOrDefault(h =>
            Math.Abs(h.HookAngle - angleRad) < 1e-2 ||
            h.Name.Contains(angleDegrees.ToString()));
    }

    public double DefaultCoverMm() => _coverTypes.Count > 0 ? RevitUnits.FtToMm(_coverTypes[0].CoverDistance) : 25.0;
}
```

---

### 3.8 Supporting Coordinate & Model Files

#### `PointMapper.cs`:
Converts local continuous beam $(X, Y, Z)$ coordinates (in mm) to Revit world `XYZ` (in ft):
```csharp
namespace HPRebar.BeamRebar;

public sealed class PointMapper
{
    private readonly XYZ _origin;
    private readonly XYZ _axisX;
    private readonly XYZ _axisY;
    private readonly XYZ _axisZ;

    public PointMapper(XYZ origin, XYZ axisX, XYZ axisY, XYZ axisZ)
    {
        _origin = origin;
        _axisX = axisX.Normalize();
        _axisY = axisY.Normalize();
        _axisZ = axisZ.Normalize();
    }

    public XYZ ToXyz(Point3 point) =>
        _origin
        + RevitUnits.MmToFt(point.X) * _axisX
        + RevitUnits.MmToFt(point.Y) * _axisY
        + RevitUnits.MmToFt(point.Z) * _axisZ;

    public XYZ ToXyz(double xMm, double yMm, double zMm) =>
        _origin
        + RevitUnits.MmToFt(xMm) * _axisX
        + RevitUnits.MmToFt(yMm) * _axisY
        + RevitUnits.MmToFt(zMm) * _axisZ;
}
```

#### `Models/BeamStack.cs`:
```csharp
namespace HPRebar.BeamRebar.Models;

public sealed record BeamStack
{
    public BeamContinuousStack ContinuousStack { get; init; } = null!;
    public IReadOnlyList<BeamSpan> Spans => ContinuousStack.Spans;
    public IReadOnlyList<BeamSupportNode> Supports => ContinuousStack.Supports;
    public IReadOnlyList<BeamFaces> SpanFaces { get; init; } = Array.Empty<BeamFaces>();
    public PointMapper PointMapper { get; init; } = null!;
    public XYZ BeamDirection { get; init; } = XYZ.BasisX;
    public XYZ NormalDirection { get; init; } = XYZ.BasisY;
}
```

#### `Models/BeamFaces.cs`:
```csharp
namespace HPRebar.BeamRebar.Models;

public sealed record BeamFaces
{
    public Element Element { get; init; } = null!;
    public PlanarFace TopFace { get; init; } = null!;
    public PlanarFace BottomFace { get; init; } = null!;
    public PlanarFace LeftFace { get; init; } = null!;
    public PlanarFace RightFace { get; init; } = null!;
    public int SpanIndex { get; init; }
}
```

#### `Models/CreatedBeamRebar.cs`:
```csharp
namespace HPRebar.BeamRebar.Models;

public sealed class CreatedBeamRebar
{
    public IReadOnlyList<Rebar> Stirrups { get; init; } = Array.Empty<Rebar>();
    public IReadOnlyList<Rebar> MainBars { get; init; } = Array.Empty<Rebar>();
    public IReadOnlyList<Rebar> AdditionalBars { get; init; } = Array.Empty<Rebar>();
    public IReadOnlyList<Rebar> SideBars { get; init; } = Array.Empty<Rebar>();
    public IReadOnlyList<Rebar> SpecialBars { get; init; } = Array.Empty<Rebar>();
    public int TotalCount => Stirrups.Count + MainBars.Count + AdditionalBars.Count + SideBars.Count + SpecialBars.Count;
}
```

---

## 4. Edge Cases & Robustness Guardrails

| Edge Case | Cause / Trigger | Handling in Creator Subsystem |
|---|---|---|
| **Short Segment < 0.78 mm** | Numerical rounding in hook anchor or depth step calculation | `Polyline3.Simplify(1.0)` merges vertices closer than 1.0 mm before `Line.CreateBound` is called, preventing Revit's fatal `ArgumentException: Curve is too short`. |
| **Non-Planar Curves in `CreateFromCurves`** | Inadvertent transverse deflection or skewed points | In `BeamMainBarCalculator`, all points for a bar share an identical transverse $Y$ coordinate. In `BeamMainBarCreator`, the plane normal is strictly $\vec{Y}_{beam}$. |
| **Max Bar Positions > 1002** | Dense stirrup spacing across very long spans | `BeamStirrupDistributionCalculator` pre-enforces `MaxBarPositions = 1002`. In `BeamStirrupCreator`, runs exceeding 1002 positions are split into adjacent sub-runs. |
| **Missing RebarShape Family** | Project template missing standard `M_T1` | `RebarShapeResolver` searches alias array (`M_T1`, `T1`, `01`, `M_01`). If missing, `CanCreate()` reports `ValidationResult.Fail` with clear diagnostic advice before any transaction is opened. |
| **Warning Dialogs Blocking Execution** | Rebar slightly outside host or overlapping geometry | `RebarFailureHandling.Apply(transaction)` attaches `SwallowWarnings : IFailuresPreprocessor` to delete warnings and log them to Serilog. |
| **Cantilever Free Ends** | Beam overhangs without an exterior column | `BeamMainBarCalculator` extends top bars to cantilever tip with 90° downward hook, while stopping bottom bars at the interior column face. |
| **Depth Step Across Spans** | Adjacent spans have different heights (e.g. $600$ mm vs $500$ mm) | Bottom bars are split per span with upward 90° anchorage hooks at the transition support node. |
| **Secondary Beam Joint Clashes** | Multiple secondary beams framing close together | `BeamSpecialBarCalculator.MergeHangingStations` merges stirrup stations closer than 20 mm. Diagonal ties check clear span development length. |

---

## 5. Verification & Testing Matrix

To independently verify the creator subsystem once implemented:

1. **Unit Test Pass**:
   - `dotnet test HPRebar/HPRebar.Core.Tests` runs all 102+ unit tests covering stirrups, main bars, additional bars, side bars, and special bars.
2. **Revit Compilation Verification**:
   - `dotnet build HPRebar/HPRebar.slnx -c Debug.R25 -p:DeployAddin=false`
   - `dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false`
   - Zero errors, zero warnings.
3. **Multi-Version Directives Check**:
   - Grep `HPRebar/Beam Rebar/` for obsolete APIs (`DisplayUnitType`, `CreateFreeForm`, unguarded `IntegerValue`).
4. **Layout Verification**:
   - All files reside strictly in `HPRebar/HPRebar/Beam Rebar/`.
   - Namespaces strictly follow `namespace HPRebar.BeamRebar;` and `namespace HPRebar.BeamRebar.Models;`.

---

## 6. Implementation Task Breakdown for M3 Part 2

1. **Step 1 — Create Model Contracts**:
   - Implement `Models/BeamFaces.cs`, `Models/BeamStack.cs`, `Models/CreatedBeamRebar.cs`, `Models/RebarTypeInfo.cs`.
2. **Step 2 — Implement Utilities & Resolvers**:
   - Implement `RevitUnits.cs` (if not present in Beam Rebar).
   - Implement `PointMapper.cs`.
   - Implement `RebarFailureHandling.cs`.
   - Implement `RebarShapeResolver.cs` and `RebarTypeCatalog.cs`.
3. **Step 3 — Implement Creators**:
   - Implement `BeamStirrupCreator.cs` (`Rebar.CreateFromRebarShape`, `ScaleToBox`, `SetLayoutAsNumberWithSpacing`).
   - Implement `BeamMainBarCreator.cs` (`Rebar.CreateFromCurves`, 90° hooks, staggered splices).
   - Implement `BeamAdditionalBarCreator.cs` (top support additions, bottom midspan additions, 2 layers).
   - Implement `BeamSideBarCreator.cs` (skin bars, anti-buckling cross-ties).
   - Implement `BeamSpecialBarCreator.cs` (hanging stirrups, 45° diagonal bent ties).
4. **Step 4 — Implement RebarCreationService**:
   - Implement `RebarCreationService.cs` coordinating pre-flight checks, planned count, and 5-phase transaction execution.
5. **Step 5 — Multi-Version Build Verification**:
   - Verify zero compile errors on `Debug.R25` and `Debug.R26`.
