# Handoff Report — Explorer M3.3: Domain Seeds & Context/Compiler Lifecycle (Findings R8 & R9)

## 1. Observation

### Observation 1.1: `generate_rebar_curves/code.cs` — Diameter Resolution Lacks Tolerance Policy
- **File**: `HPGrasshopper/HPGrasshopper.Mcp.Server/Registry/SeedLibrary/Revit/generate_rebar_curves/code.cs`
- **Lines 34–50**:
```csharp
Autodesk.Revit.DB.Structure.RebarBarType? matchedBarType = null;
double minDiameterDifference = double.MaxValue;
for (int typeIndex = 0; typeIndex < availableBarTypes.Count; typeIndex++)
{
    var candidateType = availableBarTypes[typeIndex];
    double candidateDiameterMm = candidateType.BarNominalDiameter / feetPerMm;
    double diameterDifference = Math.Abs(candidateDiameterMm - barDiameter);
    if (diameterDifference < minDiameterDifference)
    {
        minDiameterDifference = diameterDifference;
        matchedBarType = candidateType;
    }
}
if (matchedBarType == null)
{
    matchedBarType = availableBarTypes[0];
}
```
- **Observed Behavior**: The algorithm performs a simple linear minimum search (`minDiameterDifference`) with zero tolerance clamping. If a model only contains `D10` (10mm) and `D12` (12mm), and a caller requests `barDiameterMm: 16.0`, the algorithm matches `D12` with a 4.0mm discrepancy. It generates 12mm rebar without warnings, rejections, or error payloads.
- **`tool.json`**:
  `HPGrasshopper/HPGrasshopper.Mcp.Server/Registry/SeedLibrary/Revit/generate_rebar_curves/tool.json` lines 54–58 only define `barDiameterMm` with default 16.0. No `diameterToleranceMm` parameter exists in the schema.

### Observation 1.2: `generate_rebar_curves/code.cs` — Normal Calculation Breaks on Collinear Segments & Vertical Planes
- **File**: `HPGrasshopper/HPGrasshopper.Mcp.Server/Registry/SeedLibrary/Revit/generate_rebar_curves/code.cs`
- **Lines 72–86**:
```csharp
    var normalVector = new Autodesk.Revit.DB.XYZ(0.0, 0.0, 1.0);
    if (curveSegments.Count >= 2)
    {
        var tangentOne = (curveSegments[0].GetEndPoint(1) - curveSegments[0].GetEndPoint(0)).Normalize();
        var tangentTwo = (curveSegments[1].GetEndPoint(1) - curveSegments[1].GetEndPoint(0)).Normalize();
        var crossProduct = tangentOne.CrossProduct(tangentTwo);
        normalVector = crossProduct.GetLength() > 0.001 ? crossProduct.Normalize() : normalVector;
    }
    if (curveSegments.Count == 1 || Math.Abs(normalVector.GetLength() - 1.0) > 0.01)
    {
        var tangent = (curveSegments[0].GetEndPoint(1) - curveSegments[0].GetEndPoint(0)).Normalize();
        var referenceAxis = Math.Abs(tangent.Z) < 0.8 ? new Autodesk.Revit.DB.XYZ(0.0, 0.0, 1.0) : new Autodesk.Revit.DB.XYZ(1.0, 0.0, 0.0);
        normalVector = tangent.CrossProduct(referenceAxis).Normalize();
    }
```
- **Observed Behavior**:
  1. If `curveSegments.Count >= 2` and segments 0 and 1 are collinear (e.g., polyline has two segments along the X-axis before turning into the Z-axis in the XZ plane), `tangentOne.CrossProduct(tangentTwo)` has zero length ($0.0 \le 0.001$).
  2. `normalVector` stays `(0.0, 0.0, 1.0)`.
  3. Because `(0.0, 0.0, 1.0)` has length 1.0, the guard at line 80 (`Math.Abs(normalVector.GetLength() - 1.0) > 0.01`) evaluates to `false`. The fallback block (lines 81–85) NEVER executes.
  4. The subsequent segment turns into the Z-axis, which is parallel to the normal vector `(0.0, 0.0, 1.0)`.
  5. Revit's `Rebar.CreateFromCurves` throws `Autodesk.Revit.Exceptions.ArgumentException: The curves must be in a plane perpendicular to the normal vector.`
  6. Furthermore, for a straight vertical bar with 2 segments (both vertical, tangent = $(0, 0, 1)$), cross product is 0, normal vector remains $(0, 0, 1)$, which is parallel to the bar and invalid.

### Observation 1.3: `directshape_from_geometry/tool.json` & `query_revit_geometry/tool.json` — Tool Descriptions Over-Promise Capabilities
- **`directshape_from_geometry/tool.json` lines 21–23**:
  - `title`: `"Bake Complex Geometry into Revit DirectShape"`
  - `description`: `"Bake complex Rhino Breps or Meshes into native Revit DirectShape elements with assigned BuiltInCategory and name."`
  - **Actual Implementation** (`code.cs` lines 17–25, 57–101): Accepts only `boundingDimensions: { width, length, height }` (in meters) and creates an extruded cuboid solid (`shapeType: Solid`) or an 8-corner tessellated box mesh (`shapeType: Mesh`). It does NOT bake arbitrary Rhino Breps or Meshes.
- **`query_revit_geometry/tool.json` lines 22–24**:
  - `title`: `"Stream Revit Element Geometry into Grasshopper"`
  - `description`: `"Stream Revit element solids, faces, volumes, and bounding boxes into Grasshopper for computational analysis."`
  - **Actual Implementation** (`code.cs` lines 68–77): Iterates Revit solids and returns scalar summaries: `solidCount`, `volumeCubicMeters`, `surfaceAreaSquareMeters`, and 3D bounding box coordinates `bounds: { minX, minY, minZ, maxX, maxY, maxZ }`. It does NOT stream 3D geometry objects into Grasshopper.

### Observation 1.4: `GrasshopperContextExtractor.cs` — Unsafe Background Object Traversal & Unverified Defaults
- **File**: `HPGrasshopper/HPGrasshopper.McpBridge/Coordinator/GrasshopperContextExtractor.cs`
- **Lines 20–44**:
```csharp
    public static ContextResult Extract(
        object? uiapp,
        GrasshopperBridgeCoordinator coordinator,
        bool includeSelection,
        bool executionEnabled)
    {
        if (uiapp is null)
        {
            return new ContextResult
            {
                Host = PipeNaming.GrasshopperHost,
                HostVersion = "2026",
                RevitVersion = "2026",
                DocTitle = coordinator.ActiveDocumentTitle,
                DocPath = null,
                IsFamily = false,
                IsReadOnly = false,
                IsModifiable = false,
                Units = new UnitsInfo("m"),
                OpenDocs = coordinator.ActiveDocumentTitle != null ? [coordinator.ActiveDocumentTitle] : [],
                ExecutionEnabled = executionEnabled,
                Selection = [],
                Grasshopper = BuildGrasshopperInfo()
            };
        }
```
- **Lines 101–161**:
```csharp
    private static GrasshopperInfo BuildGrasshopperInfo()
    {
        ...
        var doc = Grasshopper.Instances.ActiveDocument;
        var (wireCount, selectedCount) = CountGraphMetrics(doc);
        ...
    }

    private static (int wireCount, int selectedCount) CountGraphMetrics(GH_Document? doc)
    {
        if (doc is null) return (0, 0);
        lock (doc)
        {
            objects = doc.Objects.ToArray();
        }
        foreach (var obj in objects)
        {
            ...
            if (obj is IGH_Component comp) { ... }
        }
    }
```
- **Observed Behavior**:
  1. When called from `GrasshopperBridgeCoordinator.GetContextAsync` (lines 263–280) when `_externalEvent` is null or the 500ms UI deadline expires, `uiapp` is null (invoked on a background pipe thread).
  2. The background thread calls `BuildGrasshopperInfo()`, which locks `doc` and iterates `doc.Objects.ToArray()` and `comp.Params.Input` sources. Performing Grasshopper graph traversal from a background thread during active Revit/Grasshopper UI execution violates host thread-affinity.
  3. It fabricates default facts (`Units = "m"`, `IsFamily = false`, `OpenDocs = []`) as truth, discarding any previously captured UI snapshot.

### Observation 1.5: `InvalidateCompiler()` — Dead Code in Production Lifecycle
- **File**: `HPGrasshopper/HPGrasshopper.McpBridge/Coordinator/GrasshopperBridgeCoordinator.cs`
- **Line 74**:
  `public void InvalidateCompiler() { _compiler = null; _inspector = null; Log.Information("GrasshopperBridgeCoordinator: Compiler cache invalidated."); }`
- **Observed Call Sites**:
  Search across the entire codebase revealed `InvalidateCompiler()` is called ONLY in test files:
  - `F1_F7_CoreSafetyRegressionTests.cs:235`
  - `QueueLifecycleAndIdentityAdversarialTests.cs:158`
  - `Milestone2QueueAndContextTests.cs:79`
  Zero production call sites exist!
- **Observed Behavior**: If `ScriptCompiler` is initialized before Rhino.Inside.Revit finishes loading Rhino 8 and Grasshopper assemblies into the process, `_compiler` remains permanently cached with only `RevitAPI` references. It never invalidates when RhinoCommon or Grasshopper assemblies become available.
- **File Line Count Constraint**: `GrasshopperBridgeCoordinator.cs` currently has **299 lines**. Adding more than 1 line directly to this file will violate Rule C2 (<= 300 lines).

---

## 2. Logic Chain

1. **Safety and Integrity in Domain Seeds (PCC-001, Rule 0)**:
   - *Premise*: In structural BIM automation, guessing or substituting rebar diameter without explicit user consent violates Rule 0: "Khi chưa đủ cơ sở để kết luận, hãy dừng lại và báo chưa đủ thông tin. Không được suy đoán để hoàn thành công việc."
   - *Observation 1.1* demonstrates that `generate_rebar_curves` matches nearest diameter even if the difference is massive (e.g. 16mm requested -> 12mm placed).
   - *Inference*: An explicit `diameterToleranceMm` parameter (default: 2.0mm) must be added. If `minDiameterDifference > diameterToleranceMm`, the tool must fail-fast with an informative error listing available bar types.
2. **Geometric Normal Determination for Polyline Curves**:
   - *Premise*: Revit's `Rebar.CreateFromCurves` requires all curves to be coplanar and perpendicular to a non-zero unit normal vector.
   - *Observation 1.2* shows that checking only the cross product of segments 0 and 1 fails whenever those segments are collinear (e.g., straight horizontal run before a vertical bend in the XZ plane), leaving the normal vector as `(0, 0, 1)`, which is parallel to the bend.
   - *Inference*: The algorithm must scan all segment pairs $(i, j)$ to find any non-collinear pair $(T_i \times T_j)$. If found, it must verify coplanarity for all segments ($|T_k \cdot N| \le 0.01$). If all segments are collinear (straight rebar), it must choose a perpendicular axis based on tangent orientation (using $(1, 0, 0)$ if $|T.Z| \ge 0.85$, else $(0, 0, 1)$).
3. **Contract Honesty for Tools (PCC-012, Rule T4)**:
   - *Observation 1.3* shows that tool descriptions in `directshape_from_geometry` and `query_revit_geometry` promise capabilities not present in the code.
   - *Inference*: Tool descriptions must be updated to accurately describe their real functionality (bounding cuboids and scalar geometry summaries).
4. **Thread Safety & Snapshot Cache in Context Extraction**:
   - *Observation 1.4* shows background thread traversal of `GH_Document.Objects` and discard of historical UI facts.
   - *Inference*: Coordinator must store `_cachedUiContext`. When `uiapp` is null, it should return the cached UI context if available (with `IsModifiable = false`), or a safe fallback without enumerating Grasshopper objects.
5. **Dynamic Compiler Lifecycle & Assembly Loading**:
   - *Observation 1.5* shows `InvalidateCompiler` is never called in production, and `GrasshopperBridgeCoordinator.cs` is at 299 lines (critical limit).
   - *Inference*: A dedicated `GrasshopperCompilerLifecycle.cs` should be extracted. It must subscribe to `AppDomain.CurrentDomain.AssemblyLoad` for Rhino/Grasshopper assemblies, check reference fingerprint drift on `GetCompiler()`, and delegate `InvalidateCompiler()` calls cleanly while shrinking `GrasshopperBridgeCoordinator.cs` to ~260 lines.

---

## 3. Caveats

1. **Native Revit Bar Types**: If a Revit project contains NO `RebarBarType` at all, `generate_rebar_curves` will continue to return `success = false, message = "No RebarBarType found in Revit project."` as already implemented.
2. **Complex Non-Planar Rebar**: Revit API `Rebar.CreateFromCurves` natively only supports planar rebars. 3D non-planar spiral rebars require multiple rebar objects or separate APIs; the coplanarity validation will reject non-planar curve sets with an explicit error rather than letting Revit crash with an unhandled exception.
3. **McpShared Invariance**: Zero modifications are made to `McpShared/`. All changes reside strictly within `HPGrasshopper/`.

---

## 4. Conclusion & Actionable Fix Strategy for Worker

### Target 1: `generate_rebar_curves/code.cs` & `tool.json`
- **File**: `HPGrasshopper/HPGrasshopper.Mcp.Server/Registry/SeedLibrary/Revit/generate_rebar_curves/tool.json`
  - Add `diameterToleranceMm` parameter to `properties`:
    ```json
    "diameterToleranceMm": {
      "type": "number",
      "default": 2.0,
      "description": "Maximum allowable difference in millimeters between target diameter and available Revit RebarBarType."
    }
    ```
- **File**: `HPGrasshopper/HPGrasshopper.Mcp.Server/Registry/SeedLibrary/Revit/generate_rebar_curves/code.cs`
  - Replace lines 3–8 with:
    ```csharp
    double barDiameter = args.Double("barDiameterMm", 16.0);
    double diameterTolerance = args.Double("diameterToleranceMm", 2.0);

    if (barDiameter <= 0.0)
    {
        return new { success = false, message = "barDiameterMm must be greater than zero." };
    }
    if (diameterTolerance < 0.0)
    {
        return new { success = false, message = "diameterToleranceMm cannot be negative." };
    }
    ```
  - Replace lines 47–51 with:
    ```csharp
    if (matchedBarType == null || minDiameterDifference > diameterTolerance)
    {
        string closestName = matchedBarType?.Name ?? "None";
        double closestMm = matchedBarType != null ? Math.Round(matchedBarType.BarNominalDiameter / feetPerMm, 2) : 0.0;
        return new
        {
            success = false,
            message = $"No RebarBarType matching diameter {barDiameter}mm found within tolerance ±{diameterTolerance}mm. Closest available is '{closestName}' ({closestMm}mm, diff: {Math.Round(minDiameterDifference, 2)}mm)."
        };
    }
    ```
  - Replace lines 72–86 (Normal calculation) with:
    ```csharp
    var tangents = new List<Autodesk.Revit.DB.XYZ>();
    for (int segIdx = 0; segIdx < curveSegments.Count; segIdx++)
    {
        var seg = curveSegments[segIdx];
        var vec = seg.GetEndPoint(1) - seg.GetEndPoint(0);
        if (vec.GetLength() > 0.0001) tangents.Add(vec.Normalize());
    }
    if (tangents.Count == 0) continue;

    Autodesk.Revit.DB.XYZ? computedNormal = null;
    for (int i = 0; i < tangents.Count - 1 && computedNormal == null; i++)
    {
        for (int j = i + 1; j < tangents.Count; j++)
        {
            var cross = tangents[i].CrossProduct(tangents[j]);
            if (cross.GetLength() > 0.001)
            {
                computedNormal = cross.Normalize();
                break;
            }
        }
    }

    Autodesk.Revit.DB.XYZ normalVector;
    if (computedNormal != null)
    {
        normalVector = computedNormal;
        for (int k = 0; k < tangents.Count; k++)
        {
            if (Math.Abs(tangents[k].DotProduct(normalVector)) > 0.01)
            {
                return new { success = false, message = $"Rebar curve set {setIndex} is non-planar; all segments must lie in a single plane." };
            }
        }
    }
    else
    {
        var primaryTangent = tangents[0];
        var refAxis = Math.Abs(primaryTangent.Z) < 0.85
            ? new Autodesk.Revit.DB.XYZ(0.0, 0.0, 1.0)
            : new Autodesk.Revit.DB.XYZ(1.0, 0.0, 0.0);
        normalVector = primaryTangent.CrossProduct(refAxis).Normalize();
    }
    ```

### Target 2: Tool Descriptions in `tool.json`
- **File**: `HPGrasshopper/HPGrasshopper.Mcp.Server/Registry/SeedLibrary/Geometry/directshape_from_geometry/tool.json`
  - Update `title`: `"Create DirectShape Bounding Volume in Revit"`
  - Update `description`: `"Creates native Revit DirectShape elements (Solid extrusion or Tessellated Mesh box) from bounding box dimensions with assigned BuiltInCategory and name."`
  - Update `notes`: `"Generates DirectShape cuboids (Solid or Mesh) with category assignment and bounding dimensions in meters."`
- **File**: `HPGrasshopper/HPGrasshopper.Mcp.Server/Registry/SeedLibrary/Geometry/query_revit_geometry/tool.json`
  - Update `title`: `"Query Revit Element Geometry Summaries"`
  - Update `description`: `"Queries Revit elements and returns geometric summaries (solid count, volume in cubic meters, surface area in square meters, and bounding boxes) without modifying the document."`
  - Update `notes`: `"Calculates solid volume, surface area, and bounding dimensions in meters from Revit element geometry."`

### Target 3: `GrasshopperContextExtractor.cs`
- In `Extract(object? uiapp, coordinator, includeSelection, executionEnabled)`:
  - If `uiapp is null`:
    - If `coordinator.CachedUiContext != null`, return a clone with `ExecutionEnabled = executionEnabled` and `IsModifiable = false`.
    - If `coordinator.CachedUiContext == null`, return a safe fallback with `BuildGrasshopperFallbackInfo(coordinator)` (reading only static assembly versions and coordinator cached definition names, without enumerating `doc.Objects`).
- In `ExtractFromRevit(uiapp, coordinator, ...)`:
  - After constructing `ContextResult`, call `coordinator.UpdateCachedUiContext(result);`
- Split `ExtractFromRevit` into a sub-helper `BuildOpenDocsList(UIApplication app)` to ensure method line count is <= 40 lines (Rule M3 <= 50 lines).

### Target 4: `GrasshopperCompilerLifecycle.cs` & `InvalidateCompiler` Wiring
- Add `ComputeFingerprint()` to `GrasshopperCompilerReferences.cs`:
  ```csharp
  public static string ComputeFingerprint()
  {
      var refs = All;
      return string.Join(";", refs.Select(a => $"{a.GetName().Name}:{a.GetName().Version}").OrderBy(x => x));
  }
  ```
- Extract `GrasshopperCompilerLifecycle.cs` in `HPGrasshopper.McpBridge/Coordinator/`:
  - Encapsulates `ScriptCompiler? Compiler`, `_compilerFingerprint`, `InvalidateCompiler()`.
  - Subscribes to `AppDomain.CurrentDomain.AssemblyLoad` in constructor/Initialize.
  - Automatically invalidates compiler when a Rhino/Grasshopper assembly is loaded, or when `ComputeFingerprint()` detects newly resolved references.
  - Keeps `GrasshopperBridgeCoordinator.cs` well under 300 lines (down from 299 to ~260 lines), safely satisfying Rule C2.

---

## 5. Verification Method

1. **Compilation Check**:
   ```powershell
   dotnet test HPGrasshopper/HPGrasshopper.Mcp.Server.Tests/HPGrasshopper.Mcp.Server.Tests.csproj -c Debug -p:DeployAddin=false
   ```
   *Expected*: All 116 tests pass, confirming seeds compile cleanly under Roslyn `SeedCompilationTests`.
2. **Bridge Unit Tests**:
   ```powershell
   dotnet test HPGrasshopper/HPGrasshopper.McpBridge.Tests/HPGrasshopper.McpBridge.Tests.csproj -c Debug -p:DeployAddin=false
   ```
   *Expected*: All 563+ tests pass, including regression tests for tolerance, collinear normal, context fallback, and compiler auto-invalidation.
3. **Specific New Regression Tests**:
   Create or expand `R8_R9_DomainSeedAndLifecycleTests.cs`:
   - `GenerateRebarCurves_ToleranceExceeded_ReturnsFailureMessage`: Verify that requesting 16mm against available 12mm bar type with tolerance 2.0mm fails with descriptive message.
   - `GenerateRebarCurves_CollinearPrefixInVerticalPlane_CalculatesPerpendicularNormal`: Verify polyline with collinear prefix along X then turning to Z calculates normal along Y $(0, \pm 1, 0)$.
   - `GenerateRebarCurves_StraightVerticalBar_CalculatesValidHorizontalNormal`: Verify vertical bar tangent $(0, 0, 1)$ calculates normal perpendicular to Z.
   - `ContextExtractor_NullUiApp_DoesNotEnumerateGrasshopperObjects`: Verify `Extract(null, ...)` returns cached context or fallback without accessing `doc.Objects`.
   - `CompilerLifecycle_AssemblyLoadOrFingerprintDrift_InvalidatesCompilerAutomatically`: Verify that loading an assembly or changing fingerprint automatically resets the cached compiler.
4. **Clean Code Verification**:
   - Check line count of all modified files:
     - `GrasshopperBridgeCoordinator.cs`: Must be <= 300 lines (currently 299 lines).
     - `GrasshopperContextExtractor.cs`: Must be <= 300 lines (currently 195 lines).
     - `GrasshopperCompilerLifecycle.cs`: Must be <= 300 lines (~60 lines).
     - `generate_rebar_curves/code.cs`: Must be <= 300 lines (~140 lines).
     - All methods <= 50 lines (Rule M3).
     - 0 dead/commented code (Q-B1).
     - 0 empty unannotated catches (Q-B2).
