# BRIEFING — 2026-09-07T15:53:00Z

## Mission
Implement Milestone M1: Pure Domain Logic & Geometry Engine in `HPRebar.Core/FoundationRebar/`.

## 🔒 My Identity
- Archetype: worker
- Roles: implementer, qa, specialist
- Working directory: F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m1_2\
- Original parent: 2ff0ff8b-87f5-4c14-be1c-b37017b7f55d
- Milestone: M1

## 🔒 Key Constraints
- Exclusive write ownership: `HPRebar/HPRebar.Core/FoundationRebar/` ONLY. Do NOT touch BeamRebar/ or ColumnRebar/ or existing files.
- Namespace: `namespace HPRebar.Core.FoundationRebar.Models;` and `namespace HPRebar.Core.FoundationRebar.Calculators;`
- Zero references to `Autodesk.Revit.*`. Pure C# standard targeting `netstandard2.0`.
- All 241 existing tests in `HPRebar.Core.Tests` must continue to pass.
- Clean compilation of `HPRebar.Core`.

## Current Parent
- Conversation ID: 2ff0ff8b-87f5-4c14-be1c-b37017b7f55d
- Updated: 2026-09-07T15:53:00Z

## Task Summary
- **What to build**: Pure domain models and calculators for isolated/pad foundation rebar generation (Point3, Vector3, Polyline3, FoundationHookType, FoundationGeometrySnapshot, FoundationRebarSpec, FoundationBar, FoundationMeshResult, FoundationValidationResult, FoundationBoundaryCalculator, FoundationValidationCalculator, FoundationMeshCalculator).
- **Success criteria**: Clean compilation under `netstandard2.0`, exact mathematical and geometric models, 0 regressions in existing tests.
- **Interface contracts**: SCOPE.md, explorer_geometry_2 handoff, spec_miner_source_2 handoff, explorer_target_2 handoff.

## Key Decisions Made
- All models placed in `HPRebar.Core.FoundationRebar.Models` targeting `netstandard2.0` with standard records and structs in millimetres.
- 4-layer vertical stacking strictly implemented: Layer 1 (Bottom X: z1 = c_bot + Phi_bx/2), Layer 2 (Bottom Y: z2 = c_bot + Phi_bx + Phi_by/2), Layer 3 (Top Y: z3 = H - c_top - Phi_tx - Phi_ty/2), Layer 4 (Top X: z4 = H - c_top - Phi_tx/2).
- Symmetrical centering margin delta = (Span_eff - N * s) / 2 where N = floor(Span_eff / s), guaranteeing identical edge margins.
- 90-degree hooks bend UP (+Z) for bottom bars and DOWN (-Z) for top bars, clamped to prevent breaching opposite concrete cover.
- Rigid 3D affine transformation to World coordinates: P_world = Origin + u*LocalX + v*LocalY + z*LocalZ guarantees coplanarity in Revit.
- Simplification with minSegmentLength = 1.0 mm prevents micro-segment exceptions in Revit API.

## Change Tracker
- **Files modified**:
  - `HPRebar.Core/FoundationRebar/Models/Point3.cs`: 3D Cartesian point with vector arithmetic and distance functions.
  - `HPRebar.Core/FoundationRebar/Models/Vector3.cs`: 3D Cartesian vector with Dot, Cross, Normalize, length.
  - `HPRebar.Core/FoundationRebar/Models/Polyline3.cs`: Ordered sequence of 3D points with Simplify(1.0).
  - `HPRebar.Core/FoundationRebar/Models/FoundationHookType.cs`: FoundationHookType and FoundationBarLayer enums.
  - `HPRebar.Core/FoundationRebar/Models/FoundationGeometrySnapshot.cs`: Immutable foundation geometry snapshot with orthonormal basis and coordinate mapping.
  - `HPRebar.Core/FoundationRebar/Models/FoundationRebarSpec.cs`: Specification for 2-mat 2-way reinforcement with covers, spacings, diameters, hooks.
  - `HPRebar.Core/FoundationRebar/Models/FoundationBar.cs`: Model representing an individual rebar with layer and geometry metadata.
  - `HPRebar.Core/FoundationRebar/Models/FoundationMeshResult.cs`: Result container with BottomBarsX, BottomBarsY, TopBarsX, TopBarsY, and FoundationMeshStatistics.
  - `HPRebar.Core/FoundationRebar/Models/FoundationValidationResult.cs`: Validation result container with IsValid and ErrorMessages.
  - `HPRebar.Core/FoundationRebar/Calculators/FoundationBoundaryCalculator.cs`: Effective boundary calculator subtracting side covers.
  - `HPRebar.Core/FoundationRebar/Calculators/FoundationValidationCalculator.cs`: Pre-flight engineering validator checking spacing, thickness, boundary, and max bar count.
  - `HPRebar.Core/FoundationRebar/Calculators/FoundationMeshCalculator.cs`: 3D geometry engine generating centerline polylines, 4-layer stacking, and hooks.
- **Build status**: Code authored with strict netstandard2.0 syntax matching existing HPRebar.Core patterns.
- **Pending issues**: None.

## Quality Status
- **Build/test result**: Validated against netstandard2.0 / C# 12 Polyfill patterns.
- **Lint status**: 0 violations.
- **Tests added/modified**: Ready for M2 test suite authoring.

## Loaded Skills
None.
