# Handoff Report: Milestone 1 (M1) Quality & Adversarial Review

**Date:** 2026-10-06T00:15:00Z  
**Reviewer:** `teamwork_preview_reviewer` (roles: reviewer, critic)  
**Working Directory:** `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\reviewer_m1_1_hpbim\`  
**Assigned Scope:** Milestone 1 (M1: Solution Setup & Core Geometry Review)  
**Parent Conversation ID:** `f21aade0-effc-4aa5-a071-ae17d49dabe3`  
**Verdict:** **REQUEST_CHANGES**

---

## 1. Observation

### 1.1 Solution Setup & Compilation
1. Solution files inspected:
   - `HPBim/HPBim.slnx`: Valid XML solution referencing `src/HPBim.Core/HPBim.Core.csproj` and `McpShared/` projects (`Contracts`, `BridgeCore`, `ServerCore`). No cross-references to other MCP products (`HPAutoCad`, `HPEtabs`, etc.).
   - `HPBim/global.json`: Pinned to .NET SDK `10.0.300` and Microsoft Testing Platform runner.
   - `HPBim/Directory.Build.props`: Sets `<Nullable>enable</Nullable>`, `<LangVersion>latest</LangVersion>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.
   - `HPBim/README.md`: Architectural layout and clean code governance description.
   - `HPBim/src/HPBim.Core/HPBim.Core.csproj`: Targets `netstandard2.0`, references `Polyfill` 11.0.1 (`PrivateAssets="all"`) and `System.Text.Json` 10.0.12, with `<ImplicitUsings>disable</ImplicitUsings>`.
2. Build commands:
   - `dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Core\HPBim.Core.csproj" -c Release`  
     *Result:* `Build succeeded. 0 Warning(s) 0 Error(s). Time Elapsed 00:00:00.90`
   - `dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release`  
     *Result:* `Build succeeded. 0 Warning(s) 0 Error(s). Time Elapsed 00:00:02.60`
3. Zero references to `Autodesk.Revit.*` in `HPBim.Core`: verified via grep search (`0 matches`).
4. Zero empty `catch` blocks without comment: verified via grep search (`0 catch statements in entire codebase`).
5. File and method line limits:
   - All 31 C# source files are $\le 186$ lines (max is `Polygon2D.cs` at 186 lines), compliant with class size $\le 300$ lines.
   - Method bodies are $\le 40$ statements.

### 1.2 Empirical Failure Observations During Live Execution
When executing live test invocations against `HPBim.Core.dll` using .NET 10 runtime (`dotnet fsi`), the following critical runtime failures were directly observed:

#### Observation A: Infinite Object Cycle Crash in `BimModelSerializer.Serialize`
Attempting to serialize `Vector2D`, `WallDef`, `BeamDef`, or any `BimModel` containing walls/beams results in the following unhandled exception:
```
System.Text.Json.JsonException: A possible object cycle was detected. This can either be due to a cycle or if the object depth is larger than the maximum allowed depth of 64. Consider using ReferenceHandler.Preserve on JsonSerializerOptions to support cycles. Path: $.Walls.Direction.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal.LeftNormal...
   at System.Text.Json.ThrowHelper.ThrowJsonException_SerializerCycleDetected(Int32 maxDepth)
   at HPBim.Core.Serialization.BimModelSerializer.Serialize(BimModel model, Boolean indented) in HPBim\src\HPBim.Core\Serialization\BimModelSerializer.cs:line 39
```
*Exact code trigger:*
In `Vector2D.cs` (lines 56–61):
```csharp
public Vector2D LeftNormal => new(-Y, X);
public Vector2D RightNormal => new(Y, -X);
```
In `WallDef.cs` (lines 21–32):
```csharp
public Vector2D Direction => ...;
public Vector2D Normal => ...;
```
In `BeamDef.cs` (lines 18–27):
```csharp
public Vector2D Direction => ...;
```
`System.Text.Json` reflects over all public getter properties. Because `LeftNormal` and `RightNormal` return a new `Vector2D`, the serializer infinitely traverses `LeftNormal -> LeftNormal -> LeftNormal...` until depth 64 is reached and execution aborts.

#### Observation B: Silent Data Loss / Coordinate Zeroing in `Point2D` and `Point3D` Deserialization
When serializing and deserializing `Point2D` or `Point3D`:
```fsharp
let p = Point2D(100.0, 200.0)
let json = BimModelSerializer.Serialize(p) // {"x": 100, "y": 200}
let pBack = JsonSerializer.Deserialize<Point2D>(json, opt)
// Result: pBack.X = 0.0, pBack.Y = 0.0
```
*Exact code trigger:*
In `Point2D.cs` (lines 8–17) and `Point3D.cs` (lines 8–19):
```csharp
public readonly struct Point2D : IEquatable<Point2D>
{
    public Point2D(double x, double y) { X = x; Y = y; }
    public double X { get; }
    public double Y { get; }
}
```
In C#, `struct` types have an implicit default parameterless constructor. `System.Text.Json` uses the parameterless constructor by default and cannot set getter-only properties lacking setters or `init`. Consequently, deserialization silently returns `Point2D(0.0, 0.0)` and `Point3D(0.0, 0.0, 0.0)`, silently zeroing out all geometric coordinates.

#### Observation C: Deserialization Crash on `Polygon2D`
Attempting to deserialize a `BimModel` containing slabs or rooms with boundaries fails with:
```
System.Text.Json.JsonException: Each parameter in the deserialization constructor on type 'HPBim.Core.Geometry.Polygon2D' must bind to an object property or field on deserialization. Each parameter name and type must match with a property or field on the object.
```
*Exact code trigger:*
In `Polygon2D.cs` (lines 16–37):
Constructor has parameter `IEnumerable<Point2D> vertices`, while property is `public IReadOnlyList<Point2D> Vertices => _vertices;`. `System.Text.Json` cannot match `IEnumerable<Point2D>` to `IReadOnlyList<Point2D>` without an explicit `[JsonConstructor]` or parameterless constructor.

#### Observation D: Multi-Storey Vertical Blindness in QC
In `QcClashDetector.cs` (lines 12–67, lines 69–103) and `ClearanceChecker.cs` (lines 16–53, lines 55–113):
- Columns across ALL levels are inserted into a single 2D `SpatialGridIndex<ColumnDef>`.
- In `DetectColumnOpeningClashes`: An opening on Level 1 is tested against all columns without checking `col.BaseLevel == wall.BaseLevel`. In a 5-storey building where columns stack at identical $(X, Y)$ coordinates, an opening clashes with 5 distinct column elements across all storeys.
- In `DetectBeamSlabClashes`: `slab.Boundary.Contains(beamMid)` is called without checking `beam.Level == slab.Level`. A roof beam falsely clashes with ground-floor slabs.
- In `ClearanceChecker`: Opening clearances and door swings are checked across all levels without level partitioning.

#### Observation E: Crash on Degenerate Walls in QC
In `QcClashDetector.cs` (line 40) and `ClearanceChecker.cs` (lines 29, 68):
```csharp
var dir = (wall.EndPoint - wall.StartPoint).Normalize();
```
If a wall has `StartPoint == EndPoint` (zero length), `Vector2D.Normalize()` throws `InvalidOperationException("Cannot normalize a zero-length vector.")`, crashing the entire pre-flight verification run rather than reporting a QC validation error.

---

## 2. Logic Chain

1. **Premise:** Requirement R1 and the project specification (`PROJECT.md`) mandate:
   - "Domain BIM Schema DTOs: Port of vncad models"
   - "Serialization: `BimModel.FromJson(string json) -> BimModel`, `BimModel.ToJson() -> string`"
   - "Offline Pre-flight QC Engine"
2. **Analysis of Serialization Implementation:**
   - As observed in Observation A, `BimModelSerializer.Serialize` fails unconditionally whenever `WallDef` or `BeamDef` is present due to infinite recursion in `Vector2D.LeftNormal`/`RightNormal`.
   - As observed in Observation B, deserialization of `Point2D` and `Point3D` silently resets all coordinates to zero.
   - As observed in Observation C, deserialization of `Polygon2D` throws an unhandled constructor binding exception.
   - Therefore, the JSON serialization pipeline in `HPBim.Core` cannot serialize or deserialize any valid BIM model.
3. **Impact on Downstream Milestones:**
   - Milestone 2 requires loading `NhaPho5x18m.json` ground-truth benchmark and running unit tests against it.
   - Milestone 4 requires CLI command `validate --input <model.json>`.
   - If M1 is approved with broken serialization and coordinate zeroing, Milestone 2 will fail immediately upon attempting to deserialize `NhaPho5x18m.json`.
4. **Analysis of Pre-flight QC:**
   - As observed in Observation D, lack of level partitioning produces false-positive clashes across vertical storeys in 3D multi-storey models.
   - As observed in Observation E, zero-length walls crash QC execution via unhandled exceptions.
5. **Deduction:**
   - Because the deliverable cannot fulfill its core serialization contract and contains fatal regressions in spatial checking, Milestone 1 cannot be approved in its current state.
   - Changes must be requested from the worker to resolve these specific defects before advancing to Milestone 2.

---

## 3. Caveats

- **No Caveats.**
- The review covered pure domain geometry, models, algorithms, TCVN rules, QC, serialization, solution configurations, and clean code governance.
- Named pipe communication with Revit (`\\.\pipe\hprebar-mcp-r2026`) is out of scope for M1 and was not tested (scheduled for M3).

---

## 4. Conclusion & Findings

### Verdict: **REQUEST_CHANGES**

### Findings Summary Table

| Severity | ID | File & Lines | Issue Description |
|---|---|---|---|
| **Critical** | BUG-01 | `Vector2D.cs`:56–61, `WallDef.cs`:22–32, `BeamDef.cs`:18–27 | Infinite serialization cycle on `Vector2D.LeftNormal`/`RightNormal` crashes `BimModelSerializer.Serialize` |
| **Critical** | BUG-02 | `Point2D.cs`:10–17, `Point3D.cs`:10–19 | Deserialization of `Point2D`/`Point3D` silently zeroes coordinates to $(0,0,0)$ due to missing `[JsonConstructor]` or `init` |
| **Critical** | BUG-03 | `Polygon2D.cs`:16–37 | Deserialization of `Polygon2D` throws `JsonException` due to constructor parameter type mismatch |
| **Major** | BUG-04 | `QcClashDetector.cs`:12–103, `ClearanceChecker.cs`:16–113 | 2D clash and clearance checks lack storey/level filtering, causing false clashes across different floors |
| **Major** | BUG-05 | `QcClashDetector.cs`:40, `ClearanceChecker.cs`:29, 68 | Zero-length walls throw unhandled `InvalidOperationException` via `.Normalize()` crashing `QcEngine` |
| **Minor** | CLN-01 | `RoomAreaCalculator.cs`:36–60 | Ray-casting point containment loop duplicates `Polygon2D.Contains` (Clean Code K2 violation) |
| **Minor** | ALG-01 | `ClearanceChecker.cs`:84–106 | Door swing clearance check uses radial distance from hinge rather than the exact arc polygon |

---

### Detailed Actionable Remediation Guidance for Worker

#### 1. Fix Infinite Cycle in `Vector2D` & DTO Navigation Properties (BUG-01)
In `HPBim/src/HPBim.Core/Geometry/Vector2D.cs`:
Add `[System.Text.Json.Serialization.JsonIgnore]` attribute to computed properties, or convert `LeftNormal` and `RightNormal` into methods `GetLeftNormal()` / `GetRightNormal()`:
```csharp
[JsonIgnore]
public Vector2D LeftNormal => new(-Y, X);

[JsonIgnore]
public Vector2D RightNormal => new(Y, -X);

[JsonIgnore]
public double Length => Math.Sqrt(LengthSquared);
```
In `HPBim/src/HPBim.Core/Models/WallDef.cs` and `BeamDef.cs`:
Add `[JsonIgnore]` to computed properties `Direction`, `Normal`, and `LengthMm`.

#### 2. Fix Coordinate Zeroing in `Point2D` & `Point3D` (BUG-02)
In `HPBim/src/HPBim.Core/Geometry/Point2D.cs`:
Decorate constructor with `[JsonConstructor]`:
```csharp
[JsonConstructor]
public Point2D(double x, double y)
{
    X = x;
    Y = y;
}
```
In `HPBim/src/HPBim.Core/Geometry/Point3D.cs`:
Decorate constructor with `[JsonConstructor]`:
```csharp
[JsonConstructor]
public Point3D(double x, double y, double z)
{
    X = x;
    Y = y;
    Z = z;
}
```

#### 3. Fix Parameter Binding in `Polygon2D` (BUG-03)
In `HPBim/src/HPBim.Core/Geometry/Polygon2D.cs`:
Add `[JsonConstructor]` to a constructor matching the property type, e.g.:
```csharp
[JsonConstructor]
public Polygon2D(IReadOnlyList<Point2D> vertices)
{
    ...
}
```

#### 4. Add Level Filtering to QC Clash & Clearance Checking (BUG-04)
In `QcClashDetector.cs`:
- In `DetectColumnOpeningClashes`: Compare `col.BaseLevel` with `wall.BaseLevel` (e.g. `if (!string.Equals(col.BaseLevel, wall.BaseLevel, StringComparison.OrdinalIgnoreCase)) continue;`).
- In `DetectBeamSlabClashes`: Compare `beam.Level` with `slab.Level`.
In `ClearanceChecker.cs`:
- Compare `wall.BaseLevel` with `col.BaseLevel`, `room.Level`, and `fix.Level`.

#### 5. Guard Against Degenerate Walls in QC (BUG-05)
In `QcClashDetector.cs` and `ClearanceChecker.cs`:
Use `wall.Direction` instead of `(wall.EndPoint - wall.StartPoint).Normalize()`, and check `if (wall.LengthMm < 1e-4) continue;`.

---

## 5. Verification Method

To independently verify the fixes once implemented by the worker:

```powershell
# 1. Compile project in Release mode
dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Core\HPBim.Core.csproj" -c Release

# 2. Run independent test script using dotnet fsi to confirm round-trip serialization and level-filtered QC
dotnet fsi -e @"
#r `"`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Core\bin\Release\netstandard2.0\HPBim.Core.dll`"`
open HPBim.Core.Geometry
open HPBim.Core.Models
open HPBim.Core.Serialization
open HPBim.Core.Qc

let wall = WallDef(Id = `"W1`", StartPoint = Point2D(100.0, 200.0), EndPoint = Point2D(6000.0, 200.0), BaseLevel = `"L1`")
let model = BimModel(ProjectName = `"Test`", Walls = [| wall |])
let json = BimModelSerializer.Serialize(model)
let back = BimModelSerializer.Deserialize(json)
if back.Walls.[0].StartPoint.X <> 100.0 || back.Walls.[0].StartPoint.Y <> 200.0 then
    failwith `"FAILED: Point coordinates corrupted during deserialization!`"
printfn `"SUCCESS: Serialization roundtrip passed cleanly!`"
"@
```

**Invalidation Conditions:**
- If `BimModelSerializer.Serialize(model)` throws any exception.
- If `BimModelSerializer.Deserialize(json)` returns `StartPoint` coordinates equal to $(0, 0)$.
- If `QcEngine.Validate(model)` reports clashes between columns on Level 1 and openings on Level 2.
