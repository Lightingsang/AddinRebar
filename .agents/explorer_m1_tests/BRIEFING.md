# BRIEFING — 2026-09-20T12:57:00Z

## Mission
Formulate exact implementation specification for creating HPAutoCad.Tests, migrating all test fixtures and golden data, updating HPAutoCad.slnx, and verifying test suite execution without impacting HPCivil3d mirror tests.

## 🔒 My Identity
- Archetype: explorer
- Roles: investigation, synthesis
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m1_tests
- Original parent: 050984c1-afaa-4911-859c-331e9279dc4f
- Milestone: M1 (HPAutoCad.Tests migration and solution integration)

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- Do NOT modify production source code or test files directly
- Output specification to g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m1_tests_plan.md

## Current Parent
- Conversation ID: 050984c1-afaa-4911-859c-331e9279dc4f
- Updated: 2026-09-20T12:57:00Z

## Investigation State
- **Explored paths**: `HPGeo/HPGeo.Tests/` (12 test fixtures, GoldenFixtures.cs, 3 JSON golden files), `HPGeo.Tests.csproj`, `HPAutoCad/HPAutoCad.slnx`, `HPAutoCad.Aec.Tests.csproj`, `HPAutoCad.Mcp.Server.Tests.csproj`, `HPCivil3d/HPCivil3d.McpBridge.Tests/` (`MirrorTests.cs`, `MirrorTokenTable.cs`, `mirror-tokens.json`), `orchestrator_3/survey_hpgeo.md`, `orchestrator_3/m1_core_plan.md`, `orchestrator_3/m1_tilefetch_plan.md`.
- **Key findings**:
  - Exactly 161 tests exist in `HPGeo.Tests` (158 pass, 3 skipped for live network `HPGEO_LIVE_TILES=1`).
  - 8 test files (101 tests) cover pure domain math in `HPAutoCad.Core.HPGeoLink.*`.
  - 4 test files (60 tests) cover ViewModels & Services (`GeoExportViewModel`, `GeoImportViewModel`, `HelperTileFetcher`, `TileStitcher`).
  - In M1, hosting the 9 support classes in `HPAutoCad.Tests/HPGeoLink/Support/` with `<UseWPF>true</UseWPF>` and `CommunityToolkit.Mvvm` allows 100% of the 161 tests to compile and pass before `HPAutoCad` (the add-in project) is created in M2.
  - `HPAutoCad.slnx` seamlessly accommodates the 3 new projects (`HPAutoCad.Core`, `HPAutoCad.TileFetch`, `HPAutoCad.Tests`).
  - `HPCivil3d.McpBridge.Tests` mirror tests only inspect `HPAutoCad.McpBridge`, `HPAutoCad.McpBridge.Loader`, and `HPAutoCad.Mcp.Server`, remaining 100% protected and passing 60/60.
- **Unexplored areas**: None for M1 test migration.

## Key Decisions Made
- Specified exact `HPAutoCad.Tests.csproj` with `net10.0-windows`, `xunit.v3` 3.1.0, MTP runner, `UseWPF=true`, `CommunityToolkit.Mvvm`, and ProjectReferences to `HPAutoCad.Core` and `HPAutoCad.TileFetch`.
- Resolved the M1/M2 boundary by specifying co-location of support ViewModels and Services in `HPAutoCad.Tests/HPGeoLink/Support/` during M1 for instant 161-test execution.
- Verified Civil 3D mirror invariant (60/60 tests pass live).
- Emitted full technical plan to `.agents/orchestrator_3/m1_tests_plan.md`.

## Artifact Index
- `progress.md` — Liveness heartbeat and milestone checklist
- `DISPATCH.md` — Task assignment and dispatch log
- `handoff.md` — 5-component handoff report upon completion
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m1_tests_plan.md` — Authoritative M1 Tests technical plan
