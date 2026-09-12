# BRIEFING — 2026-09-07T07:40:00Z

## Mission
Investigate and design the exact calculation algorithms for HPRebar.Core/BeamRebar/Calculators/ (BeamStirrupDistributionCalculator, BeamMainBarCalculator, BeamAdditionalBarCalculator, BeamSideBarCalculator, BeamSpecialBarCalculator, BeamCanvasTransformCalculator, Tolerance).

## 🔒 My Identity
- Archetype: explorer
- Roles: M1 Domain Calculators Architect
- Working directory: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m1_2
- Original parent: e303874c-1ef4-4fd0-9596-71bbccff874a
- Milestone: M1 (Domain Logic & Geometry Engine)

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- Strict zero references to Autodesk.Revit.* in HPRebar.Core
- Target netstandard2.0 with Polyfill 11.0.1
- Measurements strictly in millimetres (double)
- Pure, deterministic, stateless static calculators and immutable records

## Current Parent
- Conversation ID: e303874c-1ef4-4fd0-9596-71bbccff874a
- Updated: not yet

## Investigation State
- **Explored paths**:
  - `ORIGINAL_REQUEST.md`
  - `PROJECT.md`
  - `.agents/spec_miner_source_1/survey_source_analysis.md`
  - `.agents/explorer_target_1/target_arch_analysis.md`
  - `HPRebar/HPRebar.Core/ColumnRebar/` (StirrupDistributionCalculator, BarPolylineBuilder, CanvasScaleCalculator, Tolerance)
- **Key findings**:
  - Exact mathematical formulas, boundary rounding, and guardrails designed for all 7 calculators.
  - Splicing rules: 50% staggered lap splices in compression zones (top in midspan, bottom at supports).
  - Safety thresholds: `MaxBarPositions = 1002`, `MinimumSegmentMm = 1.0 mm`, `Tolerance = 1e-9`.
  - Elevation & section coordinate transformations with aspect ratio preservation and WPF Y-inversion.
- **Unexplored areas**:
  - None within M1 calculator scope. Full design completed in `calculators_plan.md`.

## Key Decisions Made
- Match ColumnRebar patterns: pure functions, immutable record inputs, guardrail limits.
- Enforce MaxBarPositions = 1002 guardrail in StirrupDistributionCalculator.
- Cull sub-millimeter segments (< 1.0mm) to prevent Revit polyline crashes.
- Vertical step handling: separate bottom bars at soffit drops to prevent cracking.
- Bidirectional screen/model mapping in BeamCanvasTransformCalculator for interactive hit-testing.

## Artifact Index
- `calculators_plan.md` — Detailed calculator algorithm design.
- `handoff.md` — 5-component handoff report.
- `progress.md` — Liveness heartbeat.
