# Task Assignment: Reviewer 1 (M1 MCP & M2 Steel CAD Drafting Review)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_reviewer_1\`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (UTC header `## 2026-10-18T00:11:55Z`)
- Project Specification: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_22\PROJECT.md`
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\HPDcePro.slnx`
- Clean Code Standards: `AGENTS.md`, `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/clean-code/CODE_REVIEW_CHECKLIST.md`

## Objectives
Independently review and verify:
1. **Milestone M1 (R1: MCP Tool Catalog & Skill)**:
   - Check `HPDcePro/src/HPDcePro.Mcp/`: All 24 modular files, Program.cs, DceProMcpServer, DceProMcpToolCatalog, Execution handlers.
   - Verify all 8 MCP tools exist and match schemas:
     `dce_design_beam`, `dce_design_column_wall`, `dce_design_foundation_pile`, `dce_design_slab_stair_lintel`, `dce_design_steel_peb`, `dce_optimize_rebar_cutting`, `dce_query_mayhoc_rebar`, `dce_run_autostr_pipeline`.
   - Check `.agents/skills/hp-dce-pro/SKILL.md`: Detailed tool documentation, parameters, triggers.
2. **Milestone M2 (R2: DbimSteelAutocad Steel CAD Drafting)**:
   - Check `HPDcePro.Core/Drafting/SteelFrameDraftingGenerator*.cs` (5 partial files) & `SteelFrameDraftingModels.cs`.
   - Verify PEB portal frame elevations (`MiddleFrameBT`, `MiddleFrame3`, `MiddleFrame3BT`, `MiddleFrame5`, `MiddleFrame5a`, `VDS1`), purlin plans, anchor bolt detail.
   - Check `HPDcePro.AutoCad/Commands/SteelCadCommands.cs` (10 commands) and `HPDcePro.lsp` (10 LISP aliases).
3. **Build & Test Verification**:
   - Run `dotnet build HPDcePro/HPDcePro.slnx -c Debug` -> Must be 0 errors, 0 warnings.
   - Run `dotnet test HPDcePro/HPDcePro.slnx -c Debug` -> Must pass 100% (>= 330 tests, 0 failed, 0 skipped).
4. **Clean Code Verification**:
   - Check every touched file: <= 300 lines, methods <= 50 lines, 0 commented code, 0 unannotated catch.

Deliver verdict: **APPROVE** or **REQUEST_CHANGES** in `handoff.md`.
Notify orchestrator via send_message.


## 2026-10-18T01:18:06Z
You are Reviewer 1 for HPDcePro Wave 3.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_reviewer_1\
Read your task assignment at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_reviewer_1\DISPATCH.md
MANDATORY: Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (entry dated 2026-10-18T00:11:55Z).
Read PROJECT.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_22\PROJECT.md
Read Clean Code rules at: docs/clean-code/HP_CLEAN_CODE_CORE.md and AGENTS.md.

Review M1 (HPDcePro.Mcp & .agents/skills/hp-dce-pro/SKILL.md) and M2 (SteelFrameDraftingGenerator, SteelCadCommands, HPDcePro.lsp).
Verify:
1. All 8 MCP tools match schemas and execute properly.
2. Steel CAD drafting produces correct 2D primitives and LISP aliases match.
3. `dotnet build HPDcePro/HPDcePro.slnx -c Debug` -> 0 errors, 0 warnings.
4. `dotnet test HPDcePro/HPDcePro.slnx -c Debug` -> >= 330 tests pass (0 failed, 0 skipped).
5. Clean code: all files <= 300 lines, methods <= 50 lines, 0 comments, 0 unannotated catch.

Deliver verdict: APPROVE or REQUEST_CHANGES in handoff.md and notify orchestrator via send_message.
