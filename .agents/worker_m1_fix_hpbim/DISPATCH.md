## 2026-10-06T00:13:48Z
You are teamwork_preview_worker assigned to fix the issues identified in Milestone 1 Iteration 1.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m1_fix_hpbim

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Read the authoritative request at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under ## 2026-10-05T23:21:03Z)

Read the project specification at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_10\PROJECT.md

Reviewer & Challenger Feedback to resolve:
1. `HPBim/src/HPBim.Core/Geometry/Vector2D.cs`:
   - Properties `LeftNormal`, `RightNormal`, `Length`, `SquaredLength` cause infinite recursion cycle in `System.Text.Json.JsonSerializer`.
   - Add `[System.Text.Json.Serialization.JsonIgnore]` attribute to `LeftNormal`, `RightNormal`, `Length`, and `SquaredLength`.
2. `HPBim/src/HPBim.Core/Geometry/Point2D.cs` & `Point3D.cs`:
   - Add `[System.Text.Json.Serialization.JsonConstructor]` attribute to `Point2D(double x, double y)` and `Point3D(double x, double y, double z)`.
   - Ensure property names `X`, `Y`, `Z` match constructor parameter names `x`, `y`, `z` so JSON deserialization restores coordinates accurately instead of defaulting to 0.
3. `HPBim/src/HPBim.Core/Geometry/Polygon2D.cs`:
   - Add `[System.Text.Json.Serialization.JsonConstructor]` to the constructor taking `IReadOnlyList<Point2D> vertices` or change constructor parameter to `IReadOnlyList<Point2D> vertices` matching property `Vertices`.
   - Add `[System.Text.Json.Serialization.JsonIgnore]` to computed properties `Perimeter`, `SignedArea`, `Area`, `Centroid`, `BoundingBox`, `IsClockwise`.
4. `HPBim/src/HPBim.Core/Qc/QcClashDetector.cs`:
   - Multi-storey partition: When checking Column vs Opening, ensure `column.BaseLevel == wall.BaseLevel` (or compare level names/elevations) so columns on level 1 do not clash with openings on level 2.
   - Slicing beam check: When checking `slab.Boundary.Contains(beamMid, includeBoundary: false)`, ensure `includeBoundary: false` is used and ensure beam and slab are on the same level (`beam.Level == slab.Level`), so perimeter edge beams do not trigger false slicing clashes.
   - Guard against zero-length walls before calling `Normalize()` (check `wall.Length > 1e-4`).
5. `HPBim/src/HPBim.Core/Qc/ClearanceChecker.cs`:
   - Multi-storey partition: When checking Opening vs Column clearance, ensure they are on the same level.
   - Guard against zero-length walls before calling `Normalize()`.

Clean Code Governance:
- Methods <= 50 lines, classes <= 300 lines, 0 empty catches, 0 Revit references in `HPBim.Core`.
- Target `netstandard2.0` with `Polyfill 11.0.1` and `TreatWarningsAsErrors=true`.

Verification:
- Compile: `dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release`
- Write a quick self-test in your handoff to verify that `BimModelSerializer.Serialize` and `Deserialize` round-trip cleanly without JsonException and preserve coordinates.
- Write your handoff report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m1_fix_hpbim\handoff.md`.
- Update `progress.md` and send message to parent.
