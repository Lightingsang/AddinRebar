## 2026-09-07T15:48:08Z
You are worker_m1_2, an implementation worker.
Working directory: F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m1_2\
Authoritative User Request: F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\ORIGINAL_REQUEST.md (Refer to '## Follow-up — 2026-09-07T15:37:30Z')
Scope Document: F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\orchestrator_2\SCOPE.md

Reference Analysis to Study Before Coding:
- Geometry Engine Analysis: F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_geometry_2\handoff.md
- Specification Mining: F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_source_2\handoff.md
- Target Architecture: F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_target_2\handoff.md

Your Mission:
Implement Milestone M1: Pure Domain Logic & Geometry Engine in `HPRebar.Core/FoundationRebar/`.

Write Ownership:
You exclusively own all files created in `HPRebar/HPRebar.Core/FoundationRebar/`.
Do NOT touch existing files in `BeamRebar/` or `ColumnRebar/` or any other directory.

Detailed Requirements:
1. Namespace convention: `namespace HPRebar.Core.FoundationRebar.Models;` and `namespace HPRebar.Core.FoundationRebar.Calculators;`
2. Strict constraint: Zero references to `Autodesk.Revit.*`. Pure C# standard records and classes targeting `netstandard2.0`.
3. Models to implement in `HPRebar.Core/FoundationRebar/Models/`:
   - `Point3.cs`: Cartesian 3D point in millimetres with basic vector arithmetic (`+`, `-`, `*`, `/`, `DistanceTo`, `IsAlmostEqualTo`).
   - `Vector3.cs`: 3D direction vector in millimetres with dot product, cross product, normalization, and magnitude.
   - `Polyline3.cs`: Ordered sequence of `Point3` representing rebar centerline curves, with `Simplify(double minSegmentLength = 1.0)` to eliminate micro-segments below Revit tolerance.
   - `FoundationHookType.cs`: Enum (`None`, `Hook90Degrees`).
   - `FoundationGeometrySnapshot.cs`: Sealed record holding `Length`, `Width`, `Thickness`, `TopZ`, `BottomZ`, `Origin` (`Point3`), `LocalX` (`Vector3`), `LocalY` (`Vector3`), `LocalZ` (`Vector3`).
   - `FoundationRebarSpec.cs`: Sealed record holding:
     `DiameterBottomX`, `DiameterBottomY`, `DiameterTopX`, `DiameterTopY`,
     `SpacingBottomX`, `SpacingBottomY`, `SpacingTopX`, `SpacingTopY`,
     `CoverTop`, `CoverBottom`, `CoverSide`,
     `IsTopMatEnabled`, `HookType`, `HookLength` (or bottom/top hook lengths).
   - `FoundationMeshResult.cs`: Holds `IReadOnlyList<Polyline3>` for `BottomBarsX`, `BottomBarsY`, `TopBarsX`, `TopBarsY`, and summary statistics.
   - `FoundationValidationResult.cs`: Holds `IsValid`, `IReadOnlyList<string> ErrorMessages`.
4. Calculators to implement in `HPRebar.Core/FoundationRebar/Calculators/`:
   - `FoundationBoundaryCalculator.cs`: Calculates effective 2D boundaries $[c_{side}, L - c_{side}] \times [c_{side}, W - c_{side}]$, with effective lengths $L_{eff}, W_{eff}$.
   - `FoundationValidationCalculator.cs`: Validates:
     * Non-positive spacing ($s \le 0$) -> error.
     * Insufficient slab thickness ($H < c_{bot} + c_{top} + \sum d_{active}$) -> error.
     * Boundary smaller than 2*side cover ($L \le 2 c_{side}$ or $W \le 2 c_{side}$) -> error.
     * Excessive bar count (> 1002 per layer) -> error.
   - `FoundationMeshCalculator.cs`:
     * Generates exact 3D centerline polylines in world coordinates using orthonormal basis transformation:
       $P_{world}(u, v, z) = Origin + u \cdot LocalX + v \cdot LocalY + z \cdot LocalZ$.
     * Exact 4-layer vertical stacking:
       - Layer 1 (Bottom X): $z_1 = c_{bot} + \Phi_{bx}/2$
       - Layer 2 (Bottom Y): $z_2 = c_{bot} + \Phi_{bx} + \Phi_{by}/2$
       - Layer 3 (Top Y): $z_3 = H - c_{top} - \Phi_{tx} - \Phi_{ty}/2$ (if TopMat enabled)
       - Layer 4 (Top X): $z_4 = H - c_{top} - \Phi_{tx}/2$ (if TopMat enabled)
     * Spacing distribution: Symmetrical centering margin $\delta = (Span_{eff} - N \cdot s) / 2$, exact bar count $N = \lfloor Span_{eff} / s \rfloor + 1$ (or ceiling spacing mode).
     * 90° hooks: Vertical legs bending UP (+Z) for bottom bars and DOWN (-Z) for top bars, clamped to prevent breaching opposite concrete cover.
     * All curves strictly coplanar in their respective planes.

Verification:
Run:
```bash
dotnet test HPRebar/HPRebar.Core.Tests
dotnet build HPRebar/HPRebar.Core/HPRebar.Core.csproj
```
Verify that `HPRebar.Core` compiles cleanly with 0 errors, and all 241 existing baseline tests in `HPRebar.Core.Tests` continue to pass.
