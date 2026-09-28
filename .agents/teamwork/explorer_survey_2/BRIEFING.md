# BRIEFING — 2026-09-27T16:07:00Z

## Mission
Investigate existing beam rebar math in HPRebar.Core and design KataRebar calculation architecture.

## 🔒 My Identity
- Archetype: explorer
- Roles: survey, math & geometry analysis, architecture design
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_2
- Original parent: aa8876fc-b61d-4725-aacd-616632eb9cc0
- Milestone: Kata Rebar Survey & Architecture

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- 100% netstandard2.0 purity with ZERO references to Autodesk.Revit.* in HPRebar.Core
- Pure domain models and stateless calculators
- Produce report.md and handoff.md in working directory
- Communicate via send_message to parent

## Current Parent
- Conversation ID: aa8876fc-b61d-4725-aacd-616632eb9cc0
- Updated: not yet

## Investigation State
- **Explored paths**:
  - `HPRebar.Core/BeamRebar/`: Models (`Point3`, `Vector3`, `Polyline3`, `BarPolyline`, `Enums`, `BeamSpan`, `BeamSupportNode`, `BeamContinuousStack`), Calculators (`BeamStirrupDistributionCalculator`, `BeamMainBarCalculator`, `BeamAdditionalBarCalculator`, `BeamSideBarCalculator`), `Tolerance`.
  - `HPRebar.Core.Tests/BeamRebar/`: 452 passing unit tests.
  - `HPRebar.Core/KataExport/`: `Interval1D`, `KataSegmentModels`, `KataRunModels`.
  - `HPRebar/BeamRebar/Service/`: `PointMapper.cs`, `BeamMainBarCreator.cs`, `BeamStirrupCreator.cs`.
  - `HPRebar/KataExport/Service/`: `KataAxisFrame.cs`.
  - `C:\kata_pro\Kata.xlsm`: live inspection of sheet `Dam` via `inspect_dam_details.py`.
- **Key findings**:
  - ~70% core math is 100% reusable: `Point3`, `Vector3`, `Polyline3`, `Tolerance`, `BeamSpan`, `BeamSupportNode`, `BeamContinuousStack`, and 3-zone stirrup distribution algorithm (`BeamStirrupDistributionCalculator`).
  - Sheet `Dam` in `Kata.xlsm` has alternating Cột | Nhịp columns, compound bar notations (`2f20;2f16`), cutoff options (`H3:H5`, `I3:I5`), and 3 stirrup types (□, U, C).
  - Recommended creating dedicated namespace `HPRebar.Core.KataRebar` with models (`KataBeamRebarSpec`, `KataSpanRebarSpec`, `KataSupportRebarSpec`, `KataBarItem`, `KataRebarCurve`, `KataStirrupZoneResult`, `KataRebarLayoutResult`) and calculators (`KataBarNotationParser`, `KataRebarCalculator`).
  - Proposed 7-step master calculation pipeline with full `Polyline3.Simplify(1.0)` short curve protection and zero Revit API coupling.
- **Unexplored areas**: None for survey scope.

## Key Decisions Made
- Architecture blueprint and calculation pipeline finalized in `report.md` and `handoff.md`.
- Confirmed 100% `netstandard2.0` purity.

## Artifact Index
- report.md — Comprehensive survey report
- handoff.md — 5-component handoff report
- progress.md — Liveness heartbeat
