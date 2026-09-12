# Revit 2025/2026 API Specification: Continuous Beam Rebar Generation

**Target Solution**: `HPRebar.slnx`  
**Target Runtimes**: Revit 2025 / 2026 (.NET 8, `net8.0-windows7.0`), compatible with Revit 2023/2024 (.NET Framework 4.8) and Revit 2027 (.NET 10)  
**Author**: `spec_miner_revit_1`  
**Date**: 2026-09-07  
**Status**: Authoritative Technical Specification  

---

## Executive Summary & Architectural Overview

This document specifies the exact Revit API contracts, geometric algorithms, transaction boundaries, annotation workflows, unit boundaries, and deprecation guardrails for porting the continuous beam reinforcement module (`R02_BeamsRebar`) into the production architecture of **HPRebar**.

The implementation strictly segregates concerns into two layers:
1. **`HPRebar.Core/BeamRebar/`**: Pure .NET standard 2.0 domain logic (records, stateless calculators, geometry builders) operating entirely in **millimetres** with **zero** `Autodesk.Revit.*` dependencies.
2. **`HPRebar/Beam Rebar/`**: Revit integration layer handling element selection, geometry extraction, unit conversions, atomic `TransactionGroup` orchestration, view generation, dimensioning, tagging, and shape-driven rebar instantiation.

---

## Features Discovered

| # | Category | Feature | Description | Inputs | Outputs | Error Behavior | Discovered Via |
|---|----------|---------|-------------|--------|---------|----------------|----------------|
| 1 | Rebar Creation | `Rebar.CreateFromCurves` | Creates shape-driven parametric rebar from 2D/3D curve chains (main bars, support additions, midspan additions, side bars). | `Document`, `RebarStyle`, `RebarBarType`, `RebarHookType` (start/end), `Element` host, `XYZ` normal vector, `IList<Curve>`, `RebarHookOrientation` (start/end), `bool` useExistingShape, `bool` createNewShape | `Rebar` instance | Throws `ArgumentException` if curves are non-planar, disconnected, self-intersecting, or shorter than `Application.ShortCurveTolerance` (~0.78 mm). | Revit API Docs / `HPRebar/Column Rebar` |
| 2 | Rebar Creation | `Rebar.CreateFromRebarShape` | Creates standard closed stirrups and cross-ties from preloaded RebarShape definitions (e.g. M_T1, M_T10). | `Document`, `RebarShape`, `RebarBarType`, `Element` host, `XYZ` origin, `XYZ` xVec, `XYZ` yVec | `Rebar` instance | Throws `ArgumentException` if `xVec` and `yVec` are not orthogonal or not unit length, or if shape is null. | `HPRebar/Column Rebar/StirrupCreator.cs` |
| 3 | Rebar Distribution | `RebarShapeDrivenAccessor.SetLayoutAsNumberWithSpacing` | Expands a single stirrup or bar into an evenly spaced array with specified count and spacing. | `int numberOfBarPositions`, `double spacing` (ft), `bool barsOnNormalSide`, `bool includeFirstBar`, `bool includeLastBar` | `void` | Throws `ArgumentOutOfRangeException` if count > 1002 (`MaxBarPositions`) or spacing <= 0. | `HPRebar/Column Rebar/StirrupCreator.cs` |
| 4 | Rebar Distribution | `RebarShapeDrivenAccessor.ScaleToBox` | Resizes shape-driven rebar to match host cross-section bounding box minus concrete cover. | `XYZ origin`, `double width` (ft), `double height` (ft) | `void` | Throws exception if width or height <= 0 or if shape does not support box scaling. | `HPRebar/Column Rebar/StirrupCreator.cs` |
| 5 | Geometry Extraction | `BeamSolidFaceReader` | Extracts clean `Solid`, top face, bottom soffit face, lateral vertical faces, and cross-section dimensions ($b$, $h$). | `Element` (Structural Framing) | `Solid`, `PlanarFace` (Top, Bottom, Left, Right) | Throws `InvalidOperationException` if element has 0 or >1 solid (e.g. complex void cuts, invalid geometry). | `HPRebar/Column Rebar/ColumnSolidFaceReader.cs` |
| 6 | Continuous Alignment | `BeamStackReader` / `BeamStackValidator` | Validates that a selection of beams forms a continuous, collinear, sequential chain sharing the same level. | `Document`, `IReadOnlyList<Element>` | `BeamStack` object containing ordered spans and support nodes | Returns `ValidationResult.Fail` with descriptive error if collinearity angle > 1°, gap > max support, or level mismatch. | Architecture specification |
| 7 | Support Detection | `BeamSupportFinder` | Queries columns (`OST_StructuralColumns`), walls (`OST_Walls`), and girder beams (`OST_StructuralFraming`) directly underneath or at beam nodes. | `Document`, `Element` beam, `BoundingBoxXYZ` | `IReadOnlyList<BeamSupportNode>` with support widths and types (Column, Wall, Girder, Cantilever) | Returns empty support list (treated as Cantilever or floating) if no intersection found. | `HPRebar/Column Rebar/ColumnNeighbourFinder.cs` |
| 8 | Secondary Beam Framing | Secondary Framing Intersect Finder | Identifies incoming secondary beams framing into the continuous primary beam to locate hanging tie zones. | `Document`, `Element` beam | `IReadOnlyList<SecondaryIntersection>` (location, width $b_s$, depth $h_s$) | Ignores non-intersecting or collinear beams. | Feature mining requirement |
| 9 | Transaction Safety | Atomic `TransactionGroup` | Encloses all view, dimension, rebar, and annotation creation in a single undo step named `"Beam Rebar"`. | `Document`, group name | Commits or rolls back all inner transactions | Rolls back entire model state if any inner transaction fails or user cancels; assimilates on success. | `HPRebar/Column Rebar/ColumnRebarOrchestrator.cs` |
| 10 | Warning Handling | `IFailuresPreprocessor` (`SwallowWarnings`) | Suppresses non-fatal Revit warnings (e.g., rebar outside host, overlapping geometry) and writes them to Serilog. | `FailuresAccessor` | `FailureProcessingResult.Continue` | Discards warning elements; allows errors to bubble up to trigger rollback. | `HPRebar/Column Rebar/RebarFailureHandling.cs` |
| 11 | View Creation | `ViewSection.CreateSection` / `CreateDetail` | Creates longitudinal detail elevation views and transverse cross-section views for each span. | `Document`, `ElementId` viewFamilyTypeId, `BoundingBoxXYZ` | `ViewSection` instance | Throws exception if view type missing or box degenerates; catches and falls back to suffixed name on name clash. | `HPRebar/Column Rebar/DetailViewCreator.cs` |
| 12 | Dimensioning | `Document.Create.NewDimension` | Generates dimension chains on elevations (levels, spans) and sections ($b$, $h$). | `View`, `Line`, `ReferenceArray`, `DimensionType` | `Dimension` instance | Catches exceptions and logs warnings; converts `SURFACE` stable references to `LINEAR` for section views. | `HPRebar/Column Rebar/DimensionCreator.cs` |
| 13 | Rebar Tagging | `IndependentTag.Create` / Detail Tables | Creates native rebar tags on elevations and detailed tabular schedule blocks beside cross-sections. | `Document`, `ElementId` viewId, `Reference` rebarRef, `XYZ` headPosition | `IndependentTag` or `DetailCurve` + `TextNote` | Gracefully skips if tag type or text note type missing from project. | `HPRebar/Column Rebar/RebarTableTagCreator.cs` |
| 14 | Unit Boundary | `RevitUnits` | Strictly converts lengths between domain millimetres and Revit internal decimal feet. | `double` value, `UnitTypeId.Millimeters` | `double` converted value | Zero deprecated `DisplayUnitType`; compile-time verified with `UnitTypeId`. | `HPRebar/Column Rebar/RevitUnits.cs` |

---

## Edge Cases

| # | Feature | Input | Observed Behavior & Handling |
|---|---------|-------|------------------------------|
| 1 | `Rebar.CreateFromCurves` | Curve segment length < 1.0 mm (e.g. tiny bend offset or numerical rounding). | Revit throws `ArgumentException: Curve is too short`. Must run `Simplify(points)` dropping points where distance < 1.0 mm. |
| 2 | `Rebar.CreateFromCurves` | Non-planar curves (normal vector `norm` not strictly perpendicular to all curve segments). | Revit rejects the curves with `ArgumentException`. Normal must be normalized and curves projected onto the target plane. |
| 3 | `Rebar.CreateFromCurves` | Disconnected curve chain (gap > 0.001 ft between end of curve $i$ and start of curve $i+1$). | Revit throws exception. Curves must be constructed by chaining `p[i]` to `p[i+1]` with exact shared endpoints. |
| 4 | `Rebar.CreateFromCurves` | `useExistingShapeIfPossible = false, createNewShape = false` when no matching shape loaded. | Revit throws `ArgumentException`. Always pass `useExistingShapeIfPossible = true, createNewShape = true` or explicitly resolve shape. |
| 5 | Stirrup Distribution | Span clear length $L_n$ very long with dense spacing (e.g. $L_n = 15$ m @ 100 mm -> 151 bars). | Works fine. But if $count > 1002$, `SetLayoutAsNumberWithSpacing` crashes. Calculator must enforce `RequireUsableCount(count, MaxBarPositions = 1002)`. |
| 6 | Stirrup Distribution | Beam clear span $L_n$ very short ($L_n < 400$ mm, e.g. short link beam). | 3-zone distribution zones collapse ($L_1, L_2, L_3$ smaller than bar spacing). System must fall back to uniform distribution (`TypeDis = 0`). |
| 7 | Beam Selection | User picks beams drawn in opposite directions (e.g. Beam 1 West->East, Beam 2 East->West). | Centerline direction vectors oppose. Reader must normalize direction: identify dominant axis and invert curve parameterization of reversed beams. |
| 8 | Beam Selection | User picks non-collinear beams (e.g. L-shaped or framing at 90° or offset in plan by 100 mm). | Validator must calculate cross-product of axes and point-to-line distance; reject with `ValidationResult.Fail("Beams are not collinear")`. |
| 9 | Beam Selection | Beams across spans have different cross-sections (e.g. Span 1 $300 \times 600$, Span 2 $300 \times 500$). | Top face is flush at slab level, bottom soffit steps up. Main bottom bars must either anchor at the step or bend with a 1:6 transition slope. |
| 10 | Support Identification | Beam cantilever end (no column or wall at exterior end). | Support finder returns `null` for external node. System recognizes `CantileverLeft` or `CantileverRight`, extends top bars into cantilever with hook downward at tip. |
| 11 | Support Identification | Beam supported by primary concrete girder (beam-to-beam framing). | Bounding box check detects `OST_StructuralFraming` perpendicular to beam. Support width equals primary beam width; soffit datum matches girder. |
| 12 | Support Identification | Support column is rotated (e.g. 45° or 90° in plan). | Support finder must compute intersection polygon of column top face and beam bottom face to determine actual bearing length along beam axis. |
| 13 | Multi-layer Bars | High bar count exceeding single layer clearance (e.g. 6 bars $\Phi 25$ in $b = 250$ mm beam). | Clear spacing between bars < 25 mm violates standard codes. Calculator must place excess bars into Layer 2 with vertical offset $\Delta z = d_{bar} + 30$ mm. |
| 14 | View Section Creation | Existing view has the identical name (e.g. `"Beam Detail - Span 1"`). | Revit throws `ArgumentException: Name must be unique`. DetailViewCreator catches exception, renames with suffix `"A"`, `"B"` or keeps default. |
| 15 | Dimension Creation | Passing `PlanarFace.Reference` directly to `NewDimension` in `ViewSection`. | Revit fails silently or throws error because section view expects `LINEAR` reference. Code must rewrite stable representation token `SURFACE` -> `LINEAR`. |
| 16 | Rebar Hook Generation | User selects standard hook but document does not have matching `RebarHookType`. | If hook type is missing, `Rebar.CreateFromCurves` throws. Creator must check available hook types before opening transaction or build geometric polyline bend. |
| 17 | Secondary Beam Crossing | Multiple secondary beams framing at same or adjacent locations. | Spacings may overlap. Hanging stirrup creator must merge overlapping hanging zones to prevent duplicate bar clashes. |

---

## 1. Revit Rebar API Specifications (2025/2026, .NET 8)

### 1.1 `Rebar.CreateFromCurves` Method Contract

`Rebar.CreateFromCurves` is the primary method for shape-driven longitudinal reinforcement in continuous beams (Main Top, Main Bottom, Additional Top, Additional Bottom, and Side Bars).

```csharp
public static Rebar CreateFromCurves(
    Document doc,
    RebarStyle style,
    RebarBarType barType,
    RebarHookType startHook,
    RebarHookType endHook,
    Element host,
    XYZ norm,
    IList<Curve> curves,
    RebarHookOrientation startHookOrient,
    RebarHookOrientation endHookOrient,
    bool useExistingShapeIfPossible,
    bool createNewShape)
```

#### Detailed Parameter Rules:
1. **`doc`**: The active Revit `Document`.
2. **`style`**: `RebarStyle.Standard` for all longitudinal, additional, and side bars (`RebarStyle.StirrupTie` is used exclusively for transverse ties).
3. **`barType`**: A valid, non-null `RebarBarType` retrieved via `FilteredElementCollector(doc).OfClass(typeof(RebarBarType))`.
4. **`startHook` / `endHook`**: 
   - When the bar is modeled as an **explicit polyline** (including 90° bends at ends), pass `null` for both `startHook` and `endHook`.
   - When the bar is modeled as a **straight line** with native hooks, pass the appropriate `RebarHookType` (e.g. 90° standard hook).
5. **`host`**: The `FamilyInstance` of category `BuiltInCategory.OST_StructuralFraming` representing the beam element. For continuous multi-span beams, each bar is hosted by the beam instance containing the majority of its length or the first span.
6. **`norm`**: **Normal Vector to the Bar Plane**:
   - For vertical-plane bends (e.g. top bars bent downward into columns, bottom bars bent upward):
     $$\vec{N} = \vec{Y}_{beam} = \vec{Z} \times \vec{X}_{beam}$$
     where $\vec{X}_{beam}$ is the unit centerline vector of the beam and $\vec{Z} = (0,0,1)$.
   - For horizontal-plane bends (e.g. bars bent sideways into slabs): $\vec{N} = \vec{Z}$.
   - For straight bars: Any unit vector orthogonal to $\vec{X}_{beam}$ (standardize on $\vec{Y}_{beam}$).
   - **Critical**: `norm` must have unit length (`norm.IsUnitLength() == true`).
7. **`curves`**: Continuous chain of `Line` or `Arc` segments.
   - Curves must be strictly planar and coplanar with $\vec{N}$.
   - Curve $i$ endpoint must equal Curve $i+1$ startpoint within $1.0\times 10^{-4}$ ft.
   - Segment lengths must exceed $1.0$ mm (~0.00328 ft).
8. **`startHookOrient` / `endHookOrient`**: `RebarHookOrientation.Right` or `RebarHookOrientation.Left`. (Ignored by Revit when hook is `null`).
9. **`useExistingShapeIfPossible`**: Set to `true`. Allows Revit to match existing shape families in the template (e.g. M_00 straight, M_02 one-hook, M_04 two-hooks).
10. **`createNewShape`**: Set to `true`. Ensures Revit synthesizes a new parametric rebar shape if custom bend lengths don't match standard library shapes.

### 1.2 `Rebar.CreateFromRebarShape` & `RebarShapeDrivenAccessor` Contract

Stirrups and cross-ties are created using preloaded standard shapes:

```csharp
public static Rebar CreateFromRebarShape(
    Document doc,
    RebarShape shape,
    RebarBarType barType,
    Element host,
    XYZ origin,
    XYZ xVec,
    XYZ yVec)
```

#### Coordinate System & Box Scaling:
1. **`origin`**: Lower-left corner of the stirrup in beam cross-section space:
   $$\text{Origin} = \text{SectionBottomLeft} + (\text{Cover} + d_{tie}/2) \cdot \vec{Y}_{beam} + (\text{Cover} + d_{tie}/2) \cdot \vec{Z}$$
2. **`xVec`**: Transverse width vector $\vec{Y}_{beam}$.
3. **`yVec`**: Vertical height vector $\vec{Z} = (0, 0, 1)$.
4. **Resizing**:
   ```csharp
   var accessor = rebar.GetShapeDrivenAccessor();
   double widthFt = RevitUnits.MmToFt(sectionB - 2 * coverMm);
   double heightFt = RevitUnits.MmToFt(sectionH - 2 * coverMm);
   accessor.ScaleToBox(origin, widthFt, heightFt);
   ```
5. **Array Layout Options**:
   ```csharp
   // Fixed count with exact spacing:
   accessor.SetLayoutAsNumberWithSpacing(
       run.Count, 
       RevitUnits.MmToFt(run.Spacing), 
       barsOnNormalSide: true, 
       includeFirstBar: true, 
       includeLastBar: true);
   ```

### 1.3 Hook Types & Hook Orientation Discovery

In continuous beams, hooks at exterior ends must adhere to:
- **Top Bars at Exterior Columns**: 90° bend downward into the column core. Anchorage length $L_{anch} \ge 35 d_{bar}$ (or $h_{beam} - 2\cdot cover$).
- **Bottom Bars at Exterior Columns**: 90° bend upward into the column core. Anchorage length $L_{anch} \ge 30 d_{bar}$.
- **RebarHookType Discovery**:
  ```csharp
  public static RebarHookType? FindHook(Document doc, int angleDegrees) =>
      new FilteredElementCollector(doc)
          .OfClass(typeof(RebarHookType))
          .Cast<RebarHookType>()
          .FirstOrDefault(h => h.HookAngle == angleDegrees);
  ```

### 1.4 Parameter and Partition Management

To ensure proper scheduling, all generated rebar elements must have their `Partition` parameter assigned:
```csharp
public static void SetPartition(Rebar rebar, string partitionName)
{
    var param = rebar.LookupParameter("Partition");
    if (param is { IsReadOnly: false })
    {
        param.Set(partitionName);
    }
}
```

### 1.5 Multi-Version & Deprecated API Avoidance Guardrails

| Deprecated / Obsolete API | Forbidden Syntax | Required Revit 2025/2026 Syntax | Multi-Version Directive |
|---------------------------|------------------|----------------------------------|-------------------------|
| `DisplayUnitType` / `UnitType` | `DisplayUnitType.DUT_MILLIMETERS` | `UnitTypeId.Millimeters` via `UnitUtils.ConvertToInternalUnits(val, UnitTypeId.Millimeters)` | None (Universal since 2022) |
| Length Specifier | `UnitType.UT_Length` | `SpecTypeId.Length` | None (Universal since 2022) |
| `ElementId.IntegerValue` | `int id = elem.Id.IntegerValue;` | `long id = elem.Id.Value;` | `#if REVIT2024_OR_GREATER` |
| `Category.Id.IntegerValue` | `elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFraming` | `elem.Category?.BuiltInCategory == BuiltInCategory.OST_StructuralFraming` | Always compare enum directly |
| `Line.CreateBound` | `doc.Application.Create.NewLineBound(...)` | `Line.CreateBound(start, end)` | Static method |
| `Rebar.CreateFreeForm` | `Rebar.CreateFreeForm(..., out var res)` (deprecated in R26, removed in R27) | Use `Rebar.CreateFromCurves` for shape-driven bars! | Avoid FreeForm for continuous beams |

---

## 2. Structural Framing Revit Geometry & Element Querying

### 2.1 Solid & Face Extraction (`BeamSolidFaceReader`)

A structural beam element in Revit (`OST_StructuralFraming`) may contain joined cutouts, cope cuts, or instance geometry.

```csharp
public static class BeamSolidFaceReader
{
    public static Solid GetBeamSolid(Element beam)
    {
        var options = new Options { ComputeReferences = true, DetailLevel = ViewDetailLevel.Fine };
        var geom = beam.get_Geometry(options);
        if (geom is null) throw new InvalidOperationException($"No geometry found for beam {beam.Id}");

        foreach (var obj in geom)
        {
            if (obj is Solid solid && solid.Volume > 1e-6) return solid;
            if (obj is GeometryInstance instance)
            {
                foreach (var instObj in instance.GetInstanceGeometry())
                {
                    if (instObj is Solid instSolid && instSolid.Volume > 1e-6) return instSolid;
                }
            }
        }
        throw new InvalidOperationException($"Beam {beam.Id} contains no solid with positive volume.");
    }

    public static (PlanarFace Top, PlanarFace Bottom, PlanarFace SideLeft, PlanarFace SideRight) 
        ExtractFaces(Solid solid, XYZ beamAxis)
    {
        var horizontal = solid.Faces.OfType<PlanarFace>()
            .Where(f => Math.Abs(Math.Abs(f.FaceNormal.Z) - 1.0) < 1e-6)
            .OrderBy(f => f.Origin.Z)
            .ToList();

        if (horizontal.Count < 2) 
            throw new InvalidOperationException("Beam does not have distinct top and bottom horizontal faces.");

        var bottom = horizontal[0];
        var top = horizontal[horizontal.Count - 1];

        XYZ sideVec = XYZ.BasisZ.CrossProduct(beamAxis).Normalize();

        var vertical = solid.Faces.OfType<PlanarFace>()
            .Where(f => Math.Abs(f.FaceNormal.Z) < 1e-6)
            .ToList();

        var left = vertical.FirstOrDefault(f => f.FaceNormal.DotProduct(sideVec) > 0.8)
                   ?? throw new InvalidOperationException("Left side face not identified.");
        var right = vertical.FirstOrDefault(f => f.FaceNormal.DotProduct(-sideVec) > 0.8)
                    ?? throw new InvalidOperationException("Right side face not identified.");

        return (top, bottom, left, right);
    }
}
```

### 2.2 Continuous Beam Alignment & Ordering (`BeamStackReader` & `BeamStackValidator`)

A continuous beam consists of $M$ collinear structural framing elements arranged in sequence.

```csharp
public static class BeamStackValidator
{
    private const double MaxCollinearAngleDeg = 1.0;
    private const double MaxOffsetMm = 10.0;

    public static ValidationResult ValidateCollinear(IReadOnlyList<Element> beams)
    {
        if (beams.Count == 0) return ValidationResult.Fail("No beams selected.");
        if (beams.Count == 1) return ValidationResult.Ok; // Single span is valid

        var primaryCurve = (beams[0].Location as LocationCurve)?.Curve as Line;
        if (primaryCurve is null) return ValidationResult.Fail("Beam 0 has no valid linear location curve.");

        XYZ origin = primaryCurve.GetEndPoint(0);
        XYZ primaryDir = (primaryCurve.GetEndPoint(1) - origin).Normalize();

        for (int i = 1; i < beams.Count; i++)
        {
            var line = (beams[i].Location as LocationCurve)?.Curve as Line;
            if (line is null) return ValidationResult.Fail($"Beam {i} is not a linear beam.");

            XYZ dir = (line.GetEndPoint(1) - line.GetEndPoint(0)).Normalize();
            double dot = Math.Abs(dir.DotProduct(primaryDir));
            double angleDeg = Math.Acos(Math.Min(1.0, dot)) * 180.0 / Math.PI;

            if (angleDeg > MaxCollinearAngleDeg)
                return ValidationResult.Fail($"Beam {i} is not collinear with Beam 0 (Angle: {angleDeg:0.1}°).");

            // Check distance from beam line to primary infinite axis
            XYZ pStart = line.GetEndPoint(0);
            XYZ perp = pStart - origin;
            XYZ proj = perp - perp.DotProduct(primaryDir) * primaryDir;
            double distMm = RevitUnits.FtToMm(proj.GetLength());

            if (distMm > MaxOffsetMm)
                return ValidationResult.Fail($"Beam {i} is offset from the continuous axis by {distMm:0.1} mm.");
        }

        return ValidationResult.Ok;
    }
}
```

#### Ordering & Parameterization:
1. Project all start and end points onto the primary axis line: $s = (\vec{P} - \vec{P}_0) \cdot \vec{X}_{beam}$.
2. Sort beams such that $s_{start, 0} < s_{start, 1} < \dots < s_{start, M-1}$.
3. Flip beam local orientation if $s_{end, i} < s_{start, i}$ so all spans progress from $s=0$ forward.

### 2.3 Support Identification & Clear Span Extraction (`BeamSupportFinder`)

Support detection identifies what holds the beam up at each junction and end:

```csharp
public static class BeamSupportFinder
{
    public static IReadOnlyList<SupportInfo> FindSupports(Document doc, Element beam)
    {
        var box = beam.get_BoundingBox(null);
        if (box is null) return Array.Empty<SupportInfo>();

        // Expand bounding box downward slightly to catch columns/walls below
        var outline = new Outline(
            new XYZ(box.Min.X, box.Min.Y, box.Min.Z - 2.0),
            new XYZ(box.Max.X, box.Max.Y, box.Min.Z + 0.5));

        var filter = new LogicalOrFilter(new ElementFilter[]
        {
            new ElementCategoryFilter(BuiltInCategory.OST_StructuralColumns),
            new ElementCategoryFilter(BuiltInCategory.OST_Walls),
            new ElementCategoryFilter(BuiltInCategory.OST_StructuralFraming)
        });

        var candidates = new FilteredElementCollector(doc)
            .WherePasses(new BoundingBoxIntersectsFilter(outline))
            .WherePasses(filter)
            .WhereElementIsNotElementType()
            .Where(e => e.Id != beam.Id)
            .ToList();

        var supports = new List<SupportInfo>();
        foreach (var candidate in candidates)
        {
            if (candidate.Category.BuiltInCategory == BuiltInCategory.OST_StructuralColumns)
            {
                supports.Add(new SupportInfo(candidate, SupportType.Column, GetColumnWidth(candidate, beam)));
            }
            else if (candidate.Category.BuiltInCategory == BuiltInCategory.OST_Walls)
            {
                supports.Add(new SupportInfo(candidate, SupportType.Wall, GetWallThickness(candidate)));
            }
            else if (candidate.Category.BuiltInCategory == BuiltInCategory.OST_StructuralFraming)
            {
                supports.Add(new SupportInfo(candidate, SupportType.Girder, GetGirderWidth(candidate)));
            }
        }

        return supports;
    }
}
```

#### Clear Span Calculation:
$$L_{clear} = L_{center} - \frac{c_{width, left}}{2} - \frac{c_{width, right}}{2}$$

If no support is found at an exterior end, that end is classified as **Cantilever**:
- Support width $c_{width} = 0$.
- Clear span extends to the free end face.

### 2.4 Secondary Beam Detection for Hanging Reinforcement

Secondary beams framing into the primary beam web are identified by:
1. `OST_StructuralFraming` elements intersecting the lateral bounding box of the continuous beam.
2. The angle between axes is near 90° ($75^\circ \le \theta \le 105^\circ$).
3. Elevation of the secondary beam soffit is at or above the primary beam soffit.
4. Extract intersection width $b_{secondary}$ to locate the hanging stirrup cluster:
   - Placement zone: $x_{center} \pm (b_{secondary}/2 + 50\text{ mm})$.

---

## 3. Atomic TransactionGroup Management

### 3.1 Outer `TransactionGroup` Lifecycle

All database mutations (views, dimensions, tags, stirrups, main bars, additional bars, side bars) MUST be encapsulated in a single outer `TransactionGroup` named `"Beam Rebar"` owned by `BeamRebarOrchestrator`:

```csharp
public OrchestratorResult Run(Document doc, BeamStack stack, BeamRebarSpec spec)
{
    using var group = new TransactionGroup(doc, "Beam Rebar");
    group.Start();

    try
    {
        // 1. Create Detail Elevation Views
        var views = CreateViews(doc, stack);

        // 2. Create Dimensions
        CreateDimensions(doc, views, stack);

        // 3. Create Reinforcement (Stirrups, Main, Add, Side, Special)
        var rebar = CreateRebar(doc, stack, spec);

        // 4. Create Bar Tables & Tags
        CreateAnnotations(doc, views, stack, spec, rebar);

        // Assimilate merges all inner transactions into ONE user undo step
        group.Assimilate();

        return OrchestratorResult.Success(views, rebar);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Beam Rebar creation failed; rolling back all changes");
        group.RollBack();
        throw;
    }
}
```

### 3.2 Inner Named Transactions Breakdown

| Step | Inner Transaction Name | Purpose | Rollback Impact |
|---|---|---|---|
| 1 | `"Create Beam Detail View"` | Generates overall continuous elevation section view. | Clean rollback via group |
| 2 | `"Create Beam Section Views"` | Generates 2 to 3 transverse cross-section views per span. | Clean rollback via group |
| 3 | `"Create Beam Dimensions"` | Adds span, level, $b$, and $h$ dimensions. | Clean rollback via group |
| 4 | `"Create Stirrups"` | Generates all 3-zone or uniform stirrup sets. | Clean rollback via group |
| 5 | `"Create Main Bars"` | Generates continuous top and bottom longitudinal bars. | Clean rollback via group |
| 6 | `"Create Additional Bars"` | Generates top support additions and bottom midspan additions. | Clean rollback via group |
| 7 | `"Create Side Bars"` | Generates web skin reinforcement ($h > 700$ mm). | Clean rollback via group |
| 8 | `"Create Special Bars"` | Generates hanging stirrups at secondary beam joints. | Clean rollback via group |
| 9 | `"Create Beam Tags"` | Generates rebar schedule tables and callout tags. | Clean rollback via group |

### 3.3 Failure Handling & Warning Suppression (`IFailuresPreprocessor`)

Revit frequently raises non-fatal warnings during rebar creation (e.g. "Rebar is slightly outside host", "Curves overlap"). To prevent modal dialogs from blocking the automated execution, attach an `IFailuresPreprocessor`:

```csharp
internal static class RebarFailureHandling
{
    public static void Apply(Transaction transaction)
    {
        var options = transaction.GetFailureHandlingOptions();
        options = options.SetFailuresPreprocessor(new SwallowWarnings());
        options = options.SetClearAfterRollback(true);
        transaction.SetFailureHandlingOptions(options);
    }

    private sealed class SwallowWarnings : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor accessor)
        {
            foreach (var failure in accessor.GetFailureMessages())
            {
                if (failure.GetSeverity() == FailureSeverity.Warning)
                {
                    Log.Warning("Revit warning during beam rebar creation: {Warning}", 
                        failure.GetDescriptionText());
                    accessor.DeleteWarning(failure);
                }
            }
            return FailureProcessingResult.Continue;
        }
    }
}
```

---

## 4. View & Annotation Creation Specifications

### 4.1 Detail Elevation View Creation (`ViewSection.CreateDetail` / `CreateSection`)

To display the entire continuous beam with all spans:
1. View Family Type: Resolved via `ViewFamilyType` where `ViewFamily == ViewFamily.Section` (or `ViewFamily.Detail`).
2. Section Box Orientation:
   - Origin: Midpoint of overall beam axis at mid-depth.
   - Basis X: $\vec{X}_{beam}$ (along beam length).
   - Basis Y: $\vec{Z} = (0, 0, 1)$ (vertical).
   - Basis Z: $\vec{Y}_{beam} = \vec{Z} \times \vec{X}_{beam}$ (view direction looking at side elevation).
3. Section Box Dimensions:
   - $\text{Min} = (-\text{TotalLengthFt}/2 - \text{Margin}, -\text{MaxDepthFt}/2 - \text{Margin}, -1.0)$
   - $\text{Max} = (\text{TotalLengthFt}/2 + \text{Margin}, \text{MaxDepthFt}/2 + \text{Margin}, 1.0)$
4. Naming: `"Beam Detail - {BeamMark}"`. Catch duplicate name exceptions and suffix with `"A"`, `"B"`.

### 4.2 Cross-Section Views Creation (`ViewSection.CreateSection`)

For each span, cross-sections are created at:
- **Section 1-1 (Support)**: Cut at $L_{clear}/6$ from left column face (dense stirrups + top additional bars).
- **Section 2-2 (Midspan)**: Cut at $L_{clear}/2$ (sparse stirrups + bottom additional bars).
- Section Box Orientation:
  - Origin: Center of beam section at cut location.
  - Basis X: $\vec{Y}_{beam}$ (across beam width).
  - Basis Y: $\vec{Z}$ (vertical).
  - Basis Z: $\vec{X}_{beam}$ (looking along beam axis).
  - Cropped closely to $b$ and $h$ with extra right-side margin for the rebar schedule table.

### 4.3 Automated Dimensioning & Linear Reference Conversion

Dimensioning requires references to the faces of the beam and levels.

```csharp
public static class BeamDimensionCreator
{
    public static void CreateElevationDimensions(
        Document doc, 
        ViewSection view, 
        BeamStack stack, 
        DimensionType dimType)
    {
        var refs = new ReferenceArray();
        foreach (var face in stack.SupportFaces)
        {
            if (face.Reference is not null)
                refs.Append(ToLinearReference(doc, face));
        }

        if (refs.Size >= 2)
        {
            var line = Line.CreateBound(stack.DimLineStart, stack.DimLineEnd);
            doc.Create.NewDimension(view, line, refs, dimType);
        }
    }

    /// <summary>
    /// Converts a planar face reference (SURFACE) to a LINEAR reference 
    /// required by Revit NewDimension in ViewSection.
    /// </summary>
    private static Reference ToLinearReference(Document doc, PlanarFace face)
    {
        var surface = face.Reference.ConvertToStableRepresentation(doc);
        var linear = surface.Replace("SURFACE", "LINEAR");
        return Reference.ParseFromStableRepresentation(doc, linear);
    }
}
```

### 4.4 Rebar Tagging & Tabular Schedules

Two annotation mechanisms:
1. **Native Rebar Tags** on Elevation:
   ```csharp
   var tag = IndependentTag.Create(
       doc, 
       view.Id, 
       new Reference(rebar), 
       addLeader: true, 
       TagMode.TM_ADDBY_CATEGORY, 
       TagOrientation.Horizontal, 
       headPosition);
   ```
2. **Tabular Schedules** beside Cross-Sections:
   Built with `DetailCurve` lines and `TextNote.Create` adjacent to the cross-section view (matching `RebarTableTagCreator.cs` convention).

---

## 5. Unit Conversions & Geometric Transformations

### 5.1 Strict Millimeters <-> Feet Boundary (`RevitUnits.cs`)

All calculations in `HPRebar.Core` use **millimetres** (`double`). The Revit API uses **decimal feet**. All conversions MUST funnel through `RevitUnits`:

```csharp
internal static class RevitUnits
{
    public static double MmToFt(double mm) => 
        UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);

    public static double FtToMm(double ft) => 
        UnitUtils.ConvertFromInternalUnits(ft, UnitTypeId.Millimeters);

    public static string Display(Document doc, double ft) => 
        UnitFormatUtils.Format(doc.GetUnits(), SpecTypeId.Length, ft, false);
}
```

### 5.2 Local-to-World Coordinate Transformations (`PointMapper.cs`)

A continuous beam has a 1D longitudinal coordinate system along its centerline:
- Coordinate $s \ge 0$: Distance in mm from the start of the first beam.
- Coordinate $y \in [-b/2, b/2]$: Transverse offset from beam centerline in mm.
- Coordinate $z \in [0, h]$: Vertical elevation from beam soffit in mm.

`PointMapper` transforms a local point $(s, y, z)$ to Revit world XYZ coordinates:
$$\vec{P}_{world} = \vec{P}_{datum} + \text{MmToFt}(s) \cdot \vec{X}_{beam} + \text{MmToFt}(y) \cdot \vec{Y}_{beam} + \text{MmToFt}(z) \cdot \vec{Z}$$

---

## 6. Target Feature File Architecture Mapping

In compliance with `AGENTS.md` Feature Folder Conventions, all feature files reside strictly in:

```
HPRebar/
├── HPRebar.Core/
│   └── BeamRebar/
│       ├── Models/
│       │   ├── BeamSpan.cs
│       │   ├── BeamSupportNode.cs
│       │   ├── BeamStirrupSpec.cs
│       │   ├── BeamStirrupDistribution.cs
│       │   ├── BeamMainBarSpec.cs
│       │   ├── BeamAdditionalBarSpec.cs
│       │   ├── BeamSideBarSpec.cs
│       │   ├── BeamSpecialBarSpec.cs
│       │   ├── BeamPolyline.cs
│       │   └── BeamScheduleItem.cs
│       ├── BeamStirrupCalculator.cs
│       ├── BeamMainBarCalculator.cs
│       ├── BeamAdditionalBarCalculator.cs
│       ├── BeamSideBarCalculator.cs
│       ├── BeamSpecialBarCalculator.cs
│       ├── BeamBarPolylineBuilder.cs
│       └── BeamCanvasScaleCalculator.cs
└── HPRebar/
    └── Beam Rebar/
        ├── BeamRebarCommand.cs
        ├── BeamRebarOrchestrator.cs
        ├── BeamStackReader.cs
        ├── BeamSolidFaceReader.cs
        ├── BeamSupportFinder.cs
        ├── BeamStackValidator.cs
        ├── BeamRebarCreationService.cs
        ├── BeamStirrupCreator.cs
        ├── BeamMainBarCreator.cs
        ├── BeamAdditionalBarCreator.cs
        ├── BeamSideBarCreator.cs
        ├── BeamSpecialBarCreator.cs
        ├── BeamDetailViewCreator.cs
        ├── BeamSectionViewCreator.cs
        ├── BeamDimensionCreator.cs
        ├── BeamTagCreator.cs
        ├── StructuralFramingSelectionFilter.cs
        ├── RevitUnits.cs
        ├── LocalizationService.cs
        ├── Models/
        │   ├── BeamStack.cs
        │   ├── BeamFaces.cs
        │   ├── BeamRebarSpec.cs
        │   ├── CreatedBeamRebar.cs
        │   └── CreatedBeamViews.cs
        ├── View/
        │   ├── BeamRebarView.xaml
        │   ├── BeamRebarView.xaml.cs
        │   └── Controls/
        │       ├── BeamElevationCanvas.cs
        │       └── BeamSectionCanvas.cs
        └── View Models/
            └── BeamRebarViewModel.cs
```

---

## 7. Verification Method

To independently verify these specifications against the project compiler and test harness:

1. **Verify No Deprecated API References**:
   Grep `HPRebar/` for `DisplayUnitType`, `UnitType.`, or un-guarded `IntegerValue`.
2. **Verify Multi-Version Compilation**:
   ```bash
   dotnet build HPRebar/HPRebar.slnx -c Debug.R25 -p:DeployAddin=false
   dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false
   ```
3. **Verify Core Domain Separation**:
   Ensure `HPRebar.Core/HPRebar.Core.csproj` has zero `Autodesk.Revit.*` package or project references.
4. **Verify Domain Test Execution**:
   ```bash
   dotnet test HPRebar/HPRebar.Core.Tests
   ```
