# BRIEFING — 2026-09-27T16:32:00Z

## Mission
Implement Milestone 2 & 3: KataRebarCalculator and unit tests in HPRebar.Core for 3D rebar geometry and 3-zone stirrup distribution.

## 🔒 My Identity
- Archetype: worker
- Roles: implementer, qa, specialist
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2
- Original parent: aa8876fc-b61d-4725-aacd-616632eb9cc0
- Milestone: M2 - Core Rebar Geometry Calculator & M3 - Pure Domain xUnit Test Suite

## 🔒 Key Constraints
- HPRebar.Core must remain pure netstandard2.0 with 0 references to Autodesk.Revit.* and zero third-party packages (no ClosedXML, no COM).
- Genuine implementation with real state and behavior — no hardcoded test shortcuts.
- Polyline3.Simplify(1.0) must be applied to all generated polylines.
- 100% green tests under dotnet test HPRebar.Core.Tests with 0 failures and 0 skipped.

## Current Parent
- Conversation ID: aa8876fc-b61d-4725-aacd-616632eb9cc0
- Updated: 2026-09-27T16:32:00Z

## Task Summary
- **What to build**: `KataRebarCalculator.cs` in `HPRebar.Core/KataRebar/Calculators/` and unit test suite `KataRebarCalculatorTests.cs` in `HPRebar.Core.Tests/KataRebar/`.
- **Success criteria**:
  1. `KataRebarCalculator.Calculate(spec)` generates 3D rebar curves (`KataRebarLayoutResult`):
     - Top & bottom continuous main bars with exterior 90° hooks and cantilever handling.
     - Multi-layer top additional bars with span cutoff extensions and exterior hooks.
     - Multi-layer bottom additional bars with clear span cutoffs ($L_n/7$ or ratio).
     - Side bars for $h \ge 700\text{ mm}$ or explicit sheet rows with $\le 300\text{ mm}$ spacing.
     - 3-zone stirrups distribution ($L_n/4$ support dense $s_1$ with 50mm offset, $L_n/2$ midspan sparse $s_2$) for closed hoops, cap stirrups, and cross ties.
  2. Unit tests covering single-span, multi-span (Kata B01), cantilever, multi-layer top, deep beam side bars, 3-zone stirrup bounds/spacing.
  3. `dotnet build HPRebar/HPRebar.Core/HPRebar.Core.csproj` and `dotnet test HPRebar/HPRebar.Core.Tests` pass with 0 failures and 0 skipped.
- **Interface contracts**: `PROJECT.md` § Interface Contracts.
- **Code layout**: `PROJECT.md` § Code Layout.

## Key Decisions Made
- [2026-09-27] Reusing existing vector primitives Point3, Vector3, Polyline3, HookAngle from HPRebar.Core.BeamRebar.Models.
- [2026-09-27] Updated KataRebarCurve.TotalLength to handle polylines with explicit hook vertices without double-counting.
- [2026-09-27] Implemented cumulative vertical gap calculation for multi-layer top and bottom extra bars ensuring >= 50mm clear spacing.
- [2026-09-27] Symmetrically centered midspan stirrups within the physical gap between support dense zones to avoid bar clashes.
- [2026-09-27] Applied Polyline3.Simplify(1.0) to all generated polylines to protect against Revit short-curve crashes.

## Change Tracker
- **Files modified**:
  - `HPRebar/HPRebar.Core/KataRebar/Models/KataRebarCurve.cs`: updated `TotalLength` getter to support explicit polyline hooks without double-counting.
  - `HPRebar/HPRebar.Core/KataRebar/Calculators/KataRebarCalculator.cs`: created master geometry and 3-zone stirrup distribution calculator.
  - `HPRebar/HPRebar.Core.Tests/KataRebar/KataRebarCalculatorTests.cs`: created 15 comprehensive unit tests covering all required scenarios.
- **Build status**: Clean (HPRebar.Core: 0 warnings, 0 errors; HPRebar.slnx Debug.R26: 0 errors).
- **Pending issues**: None.

## Quality Status
- **Build/test result**: 521 unit tests passed, 0 failures, 0 skipped.
- **Lint status**: Clean.
- **Tests added/modified**: 15 new comprehensive tests in `KataRebarCalculatorTests.cs` (expanding suite from 506 to 521 tests).

## Loaded Skills
- None loaded.

## Artifact Index
- DISPATCH.md — Assignment instructions
- BRIEFING.md — Persistent working memory
- progress.md — Liveness heartbeat and progress tracking
- report.md — Detailed milestone report
- handoff.md — 5-component handoff report
