# BRIEFING — 2026-09-20T12:45:00Z

## Mission
Investigate and catalog the entire HPGeo codebase for migration into HPAutoCad as the unified feature folder HPGeoLink.

## 🔒 My Identity
- Archetype: explorer
- Roles: survey, domain catalog, architecture mapping
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_geo\
- Original parent: 050984c1-afaa-4911-859c-331e9279dc4f
- Milestone: HPGeo Survey & Migration Mapping

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- Zero code modifications to source code
- Strict focus on HPGeo architecture, models, commands, UI, TileFetch, Tests, and mapping to HPAutoCad/HPGeoLink

## Current Parent
- Conversation ID: 050984c1-afaa-4911-859c-331e9279dc4f
- Updated: 2026-09-20T12:45:00Z

## Investigation State
- **Explored paths**: HPGeo.Core (all 41 files), HPGeo.AutoCad (Commands, Cad, Imagery, UI, Entry), HPGeo.AutoCad.Loader (HPGeoCommands, HPGeoRibbonTab, PackageContents), HPGeo.TileFetch (Program, TileFetchProtocol), HPGeo.Tests (all 12 fixture files, golden fixtures, test runner), HPAutoCad & HPCivil3d mirror tests.
- **Key findings**:
  - 6 registered commands (HPGEO, -HPGEOKMZ, HPGEOIMPORT, -HPGEOIMPORT, -HPGEOIMAGE, HPGEOINFO) in HPGeo.AutoCad.Loader.
  - Complete geodetic pipeline: WGS84 Ellipsoid, Snyder TM forward/inverse, Helmert 7 coordinate frame, Bowring geocentric, 34/63 province catalog.
  - WPF MVVM with WebView2 satellite map panel (Leaflet + Esri World Imagery) and MaterialDesignThemes theme bridge.
  - Standalone tile download utility HPGeo.TileFetch to bypass AutoCAD WSAEACCES outbound firewall rules.
  - Test suite verified: 161 total tests (158 passed, 3 skipped for live tiles). The 111 vs 161 count was due to 50 imagery tests added in a later milestone.
  - HPCivil3d.McpBridge.Tests (60 tests) pins HPAutoCad.McpBridge files via mirror-tokens.json; backwards compatibility must be strictly preserved.
- **Unexplored areas**: None. Exploration complete.

## Key Decisions Made
- Cataloged all 7 survey dimensions requested by user and orchestrator.
- Detailed file-by-file mapping table from HPGeo/ to HPAutoCad/HPGeoLink and HPAutoCad.Core/HPGeoLink.
- Documented manifest resource naming, ILRepack requirements, and mirror test constraints.

## Artifact Index
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\survey_hpgeo.md — Comprehensive technical survey report
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_geo\handoff.md — Handoff report
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_geo\progress.md — Progress log
