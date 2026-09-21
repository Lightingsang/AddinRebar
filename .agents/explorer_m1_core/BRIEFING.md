# BRIEFING — 2026-09-20T12:55:00Z

## Mission
Formulate the exact, exhaustive technical specification for creating `HPAutoCad.Core` (.NET 8.0 library) in `HPAutoCad/HPAutoCad.Core/` by migrating geodetic domain algorithms, projections, catalogs, KML/KMZ serialization, and imagery math from `HPGeo/HPGeo.Core/` to `HPAutoCad.Core.HPGeoLink.*`.

## 🔒 My Identity
- Archetype: explorer
- Roles: investigation, domain analysis, specification design
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m1_core\
- Original parent: 050984c1-afaa-4911-859c-331e9279dc4f (orchestrator_3)
- Milestone: M1 (HPAutoCad.Core Domain Migration)

## 🔒 Key Constraints
- Read-only investigation — do NOT implement or modify codebase source files
- Pure domain library: TargetFramework net8.0, Nullable enable, ImplicitUsings enable
- ZERO references to `Autodesk.*` or any CAD binaries
- Namespace migration: `HPGeo.Core.*` -> `HPAutoCad.Core.HPGeoLink.*`
- Embedded resource: `HPGeoLink/Data/vn2000-provinces.json`
- Output target: `.agents/orchestrator_3/m1_core_plan.md`

## Current Parent
- Conversation ID: 050984c1-afaa-4911-859c-331e9279dc4f
- Updated: 2026-09-20T12:55:00Z

## Investigation State
- **Explored paths**: `HPGeo/HPGeo.Core/` (all 40 .cs files + 1 .json file), `HPGeo/HPGeo.Tests/`, `HPAutoCad/HPAutoCad.slnx`, `HPAutoCad/HPAutoCad.Aec/`, `.agents/orchestrator_3/PROJECT.md`, `.agents/orchestrator_3/m1_tilefetch_plan.md`
- **Key findings**: Complete 41-file inventory cataloged; 1:1 namespace transformation established (`HPGeo.Core.<Folder>` -> `HPAutoCad.Core.HPGeoLink.<Folder>`); zero CAD references verified; embedded resource name updated in `ProvinceCatalog.cs`; `.csproj` and `.slnx` specifications authored.
- **Unexplored areas**: None within M1 Core scope.

## Key Decisions Made
- All files placed in `HPAutoCad/HPAutoCad.Core/HPGeoLink/<Folder>/`.
- Embedded resource explicit LogicalName: `HPAutoCad.Core.HPGeoLink.Data.vn2000-provinces.json`.
- `InternalsVisibleTo Include="HPAutoCad.Tests"` included for test visibility.
- Tile cache path kept as `%LocalAppData%\HPGeo\tiles` to avoid re-downloading existing tiles and retain synergy with `HPGEO` NOD settings.

## Artifact Index
- `.agents/explorer_m1_core/progress.md` — Liveness & task execution tracker
- `.agents/explorer_m1_core/handoff.md` — Final 5-component handoff report
- `.agents/orchestrator_3/m1_core_plan.md` — Authoritative M1 technical implementation specification
