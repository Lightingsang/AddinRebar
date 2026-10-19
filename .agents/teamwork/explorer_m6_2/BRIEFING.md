# BRIEFING — 2026-10-17T04:46:00Z

## Mission
Investigate and design Revit Rebar parameter bridge, shape adapters, and unit conversion quarantine for HPKata M6.

## 🔒 My Identity
- Archetype: explorer
- Roles: Teamwork explorer
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m6_2\
- Original parent: 4300a9ce-e3eb-419c-8493-f2001d2d04d6
- Milestone: M6

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- C:\kata_pro is strictly read-only
- Never inspect, decompile, or touch Security.dll or licensing code
- Zero feet/mm conversions outside HPKata.Revit
- Classes <= 300 lines (Rule C2), methods <= 50 lines (Rule M3)

## Current Parent
- Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6
- Updated: 2026-10-17T04:35:09Z

## Investigation State
- **Explored paths**:
  - `HPKata/HPKata.Core/Models/RebarItem.cs`, `KataRebarSpec.cs`, `RebarShapeDefinition.cs`, `RebarShapeCatalog.cs`
  - `docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md` (Sections 1, 4, 6, 9, 10)
  - `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `REVITADDINAI_CLEAN_CODE_STANDARD.md`
  - `HPKata/HPKata.AutoCad/Abstractions/CadDtos.cs`, `Interop/RebarGeometryService.cs`
  - `HPRebar/HPRebar/BeamRebar/Service/RebarShapeResolver.cs`, `KataRebar/Service/KataBarSetCreator.cs`
- **Key findings**:
  - Revit internal units are decimal feet; `HPKata.Core` is metric mm. Dedicated `RevitUnitConverter` isolates all $304.8$ conversions.
  - Parameter Bridge maps `REBAR_PARTITION` <-> `ten_ck`/`ComponentMark`, `REBAR_NUMBER` / `ALL_MODEL_MARK` <-> `stt`/`BarMark`, `Quantity` <-> `sl`, `BarNominalDiameter` <-> `phi`, and `REBAR_ELEM_LENGTH` <-> `L`/`CuttingLengthMm`.
  - Rebar Shape Adapter maps Revit standard shapes (M_00, M_02, M_11, M_21, M_T1, M_T10, M_77, M_74, etc.) to Kata 85 shapes (`dang01`..`dang67`).
  - Geometric fallback via `RevitCurveAnalyzer` examines centerline curves to detect topology when shape names are unknown or Freeform.
  - Decoupled `RevitRebarDto` allows 100% headless testing in `HPKata.Revit.Tests`.
- **Unexplored areas**: None. Comprehensive mapping catalog and class designs ready.

## Key Decisions Made
- Architecture decomposed into 5 focused classes: `RevitUnitConverter`, `RevitParameterBridge`, `RevitShapeAdapter`, `RevitShapeMappingTable`, `RevitCurveAnalyzer`.
- Every class $\le 300$ lines, methods $\le 50$ lines, zero literal 304.8 outside `RevitUnitConverter`.

## Artifact Index
- DISPATCH.md — Dispatch instructions
- BRIEFING.md — Working memory
- progress.md — Heartbeat
- analysis.md — Comprehensive investigation and architecture report
- handoff.md — Handoff protocol report with 5 mandatory components
