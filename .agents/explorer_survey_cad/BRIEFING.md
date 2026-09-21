# BRIEFING — 2026-09-20T12:59:00Z

## Mission
Investigate HPAutoCad architecture, bundle structure, ALC loader, Ribbon integration, MaterialDesignThemes repacking, WebView2 handling, and Civil 3D mirror constraints to inform migration of HPGeo into HPAutoCad.

## 🔒 My Identity
- Archetype: explorer
- Roles: survey, architectural investigation, read-only analysis
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_cad\
- Original parent: 050984c1-afaa-4911-859c-331e9279dc4f (orchestrator_3)
- Milestone: Survey HPAutoCad Architecture & Mirror Constraints

## 🔒 Key Constraints
- Read-only investigation — do NOT implement or modify project code
- Target report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\survey_hpautocad.md
- Maintain progress.md heartbeat
- Prepare self-contained handoff.md

## Current Parent
- Conversation ID: 050984c1-afaa-4911-859c-331e9279dc4f
- Updated: 2026-09-20T12:59:00Z

## Investigation State
- **Explored paths**:
  - `HPAutoCad/` (`HPAutoCad.slnx`, `HPAutoCad.McpBridge.Loader`, `HPAutoCad.McpBridge`, `HPAutoCad.Aec`, `HPAutoCad.Mcp.Server`, tests, harnesses)
  - `HPGeo/` (`HPGeo.slnx`, `HPGeo.AutoCad.Loader`, `HPGeo.AutoCad`, `HPGeo.Core`, `HPGeo.TileFetch`, `HPGeo.Tests`, tools)
  - `HPCivil3d/` (`mirror-tokens.json`, `HPCivil3d.McpBridge.Tests`, mirror test assertions)
- **Key findings**:
  - Baseline tests pass 100% (225 AEC, 280 McpServer, 60 Civil3d Mirror, 161 HPGeo = 726 total).
  - ALC isolation via `AssemblyDependencyResolver` prevents type collision in `acad.exe`.
  - MaterialDesignThemes repacked into DLLs via ILRepack eliminates BAML type collision across ALCs.
  - Native `WebView2Loader.dll` resolved via `deps.json` runtime targets in `runtimes\win-x64\native\`.
  - Ribbon tab `HPAUTOCAD_MCP_TAB` is already shared across HP add-ins.
  - Civil 3D mirror tests strictly scan `HPAutoCad.McpBridge*` and `HPAutoCad.Mcp.Server`; new projects (`HPAutoCad`, `HPAutoCad.Core`, etc.) do not cause mirror test failures if bridge files remain intact.
- **Unexplored areas**: None in survey scope.

## Key Decisions Made
- Recommended Strategy A: Deploy single `HPAutoCad.bundle` with dual ComponentEntry loaders (or preserved bridge loader + add-in loader), maintaining 100% green Civil 3D mirror assertions while fulfilling all single-bundle and shared-ribbon requirements.

## Artifact Index
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\survey_hpautocad.md` — Comprehensive survey report
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_cad\handoff.md` — 5-component handoff report
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_cad\progress.md` — Progress tracker
