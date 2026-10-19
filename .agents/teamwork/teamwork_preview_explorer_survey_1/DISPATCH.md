# Task Assignment: Survey Explorer 1 (Codebase Architecture & MCP Infrastructure)

- Subagent Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_survey_1\`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (UTC header `## 2026-10-18T00:11:55Z`)
- Project Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\HPDcePro.slnx`
- Clean Code Standards: `AGENTS.md`, `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/clean-code/CODE_REVIEW_CHECKLIST.md`

## Objectives
1. Read `ORIGINAL_REQUEST.md` at header `## 2026-10-18T00:11:55Z` and investigate the existing `HPDcePro` solution (`HPDcePro.slnx`), its project structure, dependencies, target frameworks, and existing test suites (Core.Tests and Adapters.Tests).
2. Investigate the architecture and implementation strategy for **R1: `HPDcePro.Mcp` (`net8.0`)** and `.agents/skills/hp-dce-pro/SKILL.md`:
   - Inspect existing MCP projects in the repo (`HPAutoCad/`, `HPEtabs/`, `McpShared/`) for conventions.
   - Detail the design for `DceProMcpToolCatalog` and `DceProMcpToolExecutor` exposing the 8 tools:
     `dce_design_beam`, `dce_design_column_wall`, `dce_design_foundation_pile`, `dce_design_slab_stair_lintel`, `dce_design_steel_peb`, `dce_optimize_rebar_cutting`, `dce_query_mayhoc_rebar`, `dce_run_autostr_pipeline`.
   - Identify existing domain services in `HPDcePro.Core` and `HPDcePro.Excel` that each tool should delegate to.
3. Identify all architectural constraints: `C:\Dce Pro` untouched, line limits (<=300 lines/file, <=50 lines/method), zero warnings with `TreatWarningsAsErrors=true`.
4. Output your findings into `handoff.md` in your working directory.

## 2026-10-18T00:14:25Z
You are Survey Explorer 1 for HPDcePro Wave 3.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_survey_1\
Read your task assignment at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_survey_1\DISPATCH.md
MANDATORY: Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (entry dated 2026-10-18T00:11:55Z).
Read Clean Code rules at: docs/clean-code/HP_CLEAN_CODE_CORE.md and AGENTS.md.

Explore the existing HPDcePro codebase (HPDcePro/HPDcePro.slnx, HPDcePro.Core, HPDcePro.Excel, HPDcePro.AutoCad, HPDcePro.UI, HPDcePro.Core.Tests, HPDcePro.Adapters.Tests).
Map out:
1. Current project structure, existing tests (280 passing baseline), references, and how HPDcePro.Mcp (net8.0) should be integrated into HPDcePro.slnx.
2. The complete architecture for R1: HPDcePro.Mcp with DceProMcpToolCatalog and DceProMcpToolExecutor exposing all 8 tools:
   - dce_design_beam
   - dce_design_column_wall
   - dce_design_foundation_pile
   - dce_design_slab_stair_lintel
   - dce_design_steel_peb
   - dce_optimize_rebar_cutting
   - dce_query_mayhoc_rebar
   - dce_run_autostr_pipeline
   Map each tool to its input/output schema, parameters, and corresponding service in HPDcePro.Core or HPDcePro.Excel.
3. How .agents/skills/hp-dce-pro/SKILL.md should be structured.
4. Clean code constraints and file decomposition strategy (keep all files <= 300 lines, methods <= 50 lines).

Write your detailed findings into:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_survey_1\handoff.md
When finished, send a message to the orchestrator (caller) informing that you are done with the path to handoff.md.
