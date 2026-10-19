## 2026-10-18T00:27:10Z

# Task Assignment: Worker M1 (HPDcePro.Mcp & Agent Skill)

- Subagent Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_m1\`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (UTC header `## 2026-10-18T00:11:55Z`)
- Project Specification: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_22\PROJECT.md`
- Survey Blueprint: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_survey_1\handoff.md`
- Clean Code Standards: `AGENTS.md`, `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/clean-code/CODE_REVIEW_CHECKLIST.md`

## Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Write Ownership Boundaries
You have exclusive write ownership of:
- `HPDcePro/HPDcePro.slnx` (adding `src/HPDcePro.Mcp/HPDcePro.Mcp.csproj`)
- `HPDcePro/src/HPDcePro.Mcp/**` (all files in HPDcePro.Mcp project)
- `.agents/skills/hp-dce-pro/SKILL.md`

## Detailed Implementation Tasks
1. Create `HPDcePro/src/HPDcePro.Mcp/HPDcePro.Mcp.csproj`:
   - Target framework: `net8.0`, OutputType: `Exe`.
   - References: `ProjectReference` to `HPDcePro.Core.csproj` and `HPDcePro.Excel.csproj`.
   - `Nullable=enable`, `TreatWarningsAsErrors=true`.
   - Add the project to `HPDcePro/HPDcePro.slnx` under `/src/`.
2. Follow the 23-file modular layout defined in `teamwork_preview_explorer_survey_1/handoff.md § 2.4`:
   - `Program.cs` & `DceProMcpServer.cs` implementing standard stdio JSON-RPC 2.0 loop (`tools/list`, `tools/call`, `initialize`).
   - `Catalog/DceProMcpToolCatalog.cs` registering tool definitions.
   - `Catalog/Schemas/` with schema classes for each tool.
   - `Execution/DceProMcpToolExecutor.cs` dispatching calls.
   - `Execution/Handlers/` with dedicated handlers for all 8 tools:
     1. `dce_design_beam` (TCVN 5574:2018 single/continuous/foundation/curved beam design + shop drawing primitive summary)
     2. `dce_design_column_wall` (TCVN 5574:2018 biaxial P-Mx-My column & shear wall boundary element check)
     3. `dce_design_foundation_pile` (TCVN 10304:2025 SPT/friction pile capacity, pile cap & pad footing design)
     4. `dce_design_slab_stair_lintel` (Two-way Marcus slab, EngineBanThang/EngineZigZac staircase & opening lintel design)
     5. `dce_design_steel_peb` (TCVN 5575:2024 PEB portal frame, bolted moment connection, anchor bolt base plate & TCVN 2737:2023 wind load)
     6. `dce_optimize_rebar_cutting` (1D 11.7m commercial bar cutting stock optimizer ToiUuCatThep & TCVN 1651 schedule aggregator)
     7. `dce_query_mayhoc_rebar` (Query MayHoc/mauhoc.db ML patterns & tokenize Vietnamese rebar callout strings)
     8. `dce_run_autostr_pipeline` (Execute the 24-step AutoStrOrchestrator on a .autostr project with auto-resizing & multi-sheet ClosedXML BOQ export)
   - Ensure all files are $\le 300$ lines (aim for $< 160$ lines) and methods $\le 50$ lines.
   - Zero commented-out code, zero empty catches without explanation comments.
3. Author `.agents/skills/hp-dce-pro/SKILL.md`:
   - Documenting the 8 MCP tools, JSON schema parameters, input/output samples, and engineering workflows for AI coding agents.
4. Verify build and execution:
   - Build `dotnet build HPDcePro/src/HPDcePro.Mcp/HPDcePro.Mcp.csproj -c Debug` -> 0 errors, 0 warnings.
   - Build `dotnet build HPDcePro/HPDcePro.slnx -c Debug` -> 0 errors, 0 warnings.
5. Write your completion report in `handoff.md` in your working directory and notify the orchestrator.
