# BRIEFING — 2026-10-06T00:15:00Z

## Mission
Milestone 1 (M1: Solution Setup & Core Geometry Review) independent quality and adversarial review.

## 🔒 My Identity
- Archetype: reviewer / critic
- Roles: reviewer, critic
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\reviewer_m1_1_hpbim
- Original parent: f21aade0-effc-4aa5-a071-ae17d49dabe3
- Milestone: M1: Solution Setup & Core Geometry Review
- Instance: 1 of 1

## 🔒 Key Constraints
- Review-only — do NOT modify implementation code
- Actively check for integrity violations (hardcoded test results, dummy/facade implementations, shortcuts, fabricated verification, self-certifying work)
- Verify target netstandard2.0 with Polyfill 11.0.1, zero Autodesk.Revit references
- Clean Code limits: methods <= 50 lines, classes <= 300 lines, 0 empty catches, Q-B1..Q-B3 compliance

## Current Parent
- Conversation ID: f21aade0-effc-4aa5-a071-ae17d49dabe3
- Updated: 2026-10-06T00:00:05Z

## Review Scope
- **Files to review**: `HPBim/HPBim.slnx`, `HPBim/global.json`, `HPBim/Directory.Build.props`, `HPBim/README.md`, `HPBim/src/HPBim.Core/HPBim.Core.csproj`, all files in `HPBim/src/HPBim.Core/Geometry/` and `HPBim/src/HPBim.Core/Models/`, algorithms, TCVN rules, QC, serialization
- **Interface contracts**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_10\PROJECT.md`
- **Review criteria**: Correctness, completeness, clean code compliance, integrity, adversarial stress testing

## Review Checklist
- **Items reviewed**:
  - `HPBim.slnx`, `global.json`, `Directory.Build.props`, `README.md`, `HPBim.Core.csproj`
  - 6 Geometry primitives (`Point2D`, `Point3D`, `Vector2D`, `BoundingBox2D`, `Polygon2D`, `LiangBarskyClipping`)
  - 13 Domain models (`AxisGrid`, `LevelDef`, `ColumnDef`, `WallDef`, `OpeningDef`, `BeamDef`, `SlabDef`, `RoomDef`, `MepFixtureDef`, `PipeRunDef`, `RebarDef`, `BimModel`, `Enums`)
  - 3 Spatial algorithms (`WallSegmenter`, `RoomAreaCalculator`, `OpeningPlacementEngine`)
  - 3 TCVN rules (`TcvnStandards`, `ConcreteCoverRules`, `TcvnRebarCalculator`)
  - 5 QC modules (`SpatialGridIndex`, `QcReport`, `QcClashDetector`, `ClearanceChecker`, `QcEngine`)
  - 1 Serialization module (`BimModelSerializer`)
- **Verdict**: REQUEST_CHANGES
- **Unverified claims**: Worker claimed serialization was ready and functional, but independent live testing proved serialization crashes on infinite cycles and deserialization silently zeroes out coordinates.

## Attack Surface
- **Hypotheses tested**:
  - `Vector2D` recursive serialization cycles (`LeftNormal` / `RightNormal`)
  - `Point2D` / `Point3D` struct deserialization behavior under `System.Text.Json`
  - `Polygon2D` constructor binding under `System.Text.Json`
  - Zero-length degenerate walls in QC clash/clearance checking
  - Multi-storey level separation in QC clash/clearance checking
  - Door swing polygon vs radial distance approximations
- **Vulnerabilities found**:
  - [CRITICAL] `BimModelSerializer.Serialize` aborts with `JsonException` object cycle on `Vector2D.LeftNormal`/`RightNormal` (in `WallDef.Direction`, `WallDef.Normal`, `BeamDef.Direction`).
  - [CRITICAL] `Point2D` and `Point3D` deserialization silently produces `(0, 0)` and `(0, 0, 0)` due to missing `[JsonConstructor]` / init setters on readonly structs.
  - [CRITICAL] `Polygon2D` deserialization throws `JsonException` due to constructor parameter `IEnumerable<Point2D>` mismatch with property `IReadOnlyList<Point2D>`.
  - [MAJOR] `QcClashDetector` and `ClearanceChecker` are vertically blind across multi-storey storeys (checks all levels together without filtering by `BaseLevel`).
  - [MAJOR] Zero-length walls crash `QcClashDetector` and `ClearanceChecker` with unhandled `InvalidOperationException` via `Vector2D.Normalize()`.
- **Untested angles**: Full BIM model roundtrip over named pipe (reserved for M3).

## Key Decisions Made
- Discovered 3 critical serialization defects and 2 major QC defects via independent live execution
- Formulated clear fix recommendations with minimal diffs
- Issued REQUEST_CHANGES verdict to prevent downstream failures in M2/M3

## Artifact Index
- handoff.md — Comprehensive 5-component review and adversarial challenge report
- progress.md — Liveness heartbeat
- DISPATCH.md — Dispatched instructions
