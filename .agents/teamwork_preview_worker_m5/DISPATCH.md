# DISPATCH: Worker M5 (Solution Packaging, AGENTS.md Registration & Skill Documentation)

## Assignment
- **Agent**: `teamwork_preview_worker_m5`
- **Role**: Worker M5 (Solution Packaging & Ecosystem Docs)
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m5`
- **Parent / Orchestrator**: `5d7560ee-5142-428f-a172-e73cf7738ac1`
- **Original Request**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (header `## 2026-09-21T17:20:33Z`)
- **Repo Root**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar`

## Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Write Ownership
You have exclusive write access to:
- `HPTekla/HPTekla.slnx`
- `AGENTS.md`
- `.agents/skills/hp-mcp-tekla/SKILL.md`
- `.agents/teamwork_preview_worker_m5/**`

## Deliverables & Acceptance Criteria

### 1. Solution Configuration (`HPTekla/HPTekla.slnx`)
- Create `HPTekla/HPTekla.slnx` in standard XML `.slnx` format matching `HPRobot/HPRobot.slnx` / `HPEtabs/HPEtabs.slnx`.
- Solution structure:
  - Configurations: `Debug`, `Release`.
  - Folder `/Solution Items/`: `global.json`, `Directory.Build.props`.
  - Projects:
    - `HPTekla.McpBridge/HPTekla.McpBridge.csproj` (.NET Framework 4.8)
    - `HPTekla.McpBridge.Tests/HPTekla.McpBridge.Tests.csproj` (.NET Framework 4.8)
    - `HPTekla.Mcp.Server/HPTekla.Mcp.Server.csproj` (.NET 10.0)
    - `HPTekla.Mcp.Server.Tests/HPTekla.Mcp.Server.Tests.csproj` (.NET 10.0)
  - Folder `/Shared/`:
    - `../McpShared/HPRebar.Mcp.Contracts/HPRebar.Mcp.Contracts.csproj`
    - `../McpShared/HPRebar.McpBridge.Core/HPRebar.McpBridge.Core.csproj`
    - `../McpShared/HPRebar.Mcp.Server.Core/HPRebar.Mcp.Server.Core.csproj`
- Verification commands:
  - `dotnet build HPTekla/HPTekla.slnx -c Release` -> Must succeed with 0 errors.
  - `dotnet build HPTekla/HPTekla.slnx -c Debug` -> Must succeed with 0 errors.
  - `dotnet test HPTekla/HPTekla.Mcp.Server.Tests/HPTekla.Mcp.Server.Tests.csproj` -> 96/96 tests pass.
  - `dotnet test HPTekla/HPTekla.McpBridge.Tests/HPTekla.McpBridge.Tests.csproj` -> 24/24 tests pass.

### 2. AGENTS.md Registration
- Add `HPTekla/` into the Repository Layout table in `AGENTS.md` (as Deliverable #11):
  ```markdown
  | `HPTekla/` | The **Tekla Structures 2025 MCP** (`HPTekla.McpBridge` in-process plugin inside `TeklaStructures.exe` net48 + `HPTekla.Mcp.Server` net10 stdio exe, **24 tools** = 4 core + 8 registry + 12 embedded seeds). In-process plugin connecting via Tekla Open API; Named Pipe `hptekla-mcp-2025`; 3-tier safety (Read, Write, Destructive); native transaction savepoint rollback for `dryRun` (`SetTestSavePoint`/`RollbackToTestSavePoint`); automatic `.db1`/`.db2` model database snapshots before writes; WPF modeless status dialog with theme sync. Own `HPTekla.slnx` + `global.json` + `Directory.Build.props` (references installed Tekla Open API 2025.0); references `../McpShared/` only. Tests `HPTekla.Mcp.Server.Tests` (96) + `HPTekla.McpBridge.Tests` (24). | C# / net48 · net10 / Tekla Open API 2025.0 (installed) |
  ```
- Add a dedicated section `## HPTekla — Build, Run, Test` in `AGENTS.md` documenting:
  - Solution file `HPTekla/HPTekla.slnx`
  - How to build bridge and server in Release and Debug
  - How to run unit tests and seed compilation tests
  - How to run live verification harness (`run-live-verify.ps1`)
  - Load-bearing architectural facts (in-process plugin, CLR v4.0.30319 / .NET Framework 4.8, pipe `hptekla-mcp-2025`, `MainThreadQueue` thread synchronization, savepoint rollback, snapshot engine, 12 embedded seeds).

### 3. Skill Documentation (`.agents/skills/hp-mcp-tekla/SKILL.md`)
- Create `.agents/skills/hp-mcp-tekla/SKILL.md` following the pattern of `hp-mcp-robot` / `hp-mcp-etabs`:
  - YAML frontmatter with `name: hp-mcp-tekla`, comprehensive description, triggers, keywords.
  - Portable host contract comments.
  - Architecture overview: Claude -> `HPTekla.Mcp.Server` (stdio) -> Pipe `hptekla-mcp-2025` -> `HPTekla.McpBridge` (in-process inside `TeklaStructures.exe`) -> Tekla Open API 2025.0.
  - 3-tier safety system, `dryRun = true` savepoint rollback, automatic `.db1`/`.db2` snapshots.
  - Connection checklist (Tekla Structures 2025 open, model loaded, bridge status dialog, `get_tekla_context`).
  - Decision tree & workflow guidance.
  - Catalog of all 24 tools:
    - 4 Core tools: `execute_tekla_code`, `get_tekla_context`, `inspect_type`, `cancel_execution`.
    - 8 Registry Meta tools: `search_tools`, `get_tool`, `run_tool`, `get_run`, `propose_tool`, `test_tool`, `publish_tool`, `manage_tool`.
    - 12 Embedded Seeds across 6 categories: `get_model_info`, `select_objects`, `get_part_properties`, `create_beam`, `create_column`, `create_contour_plate`, `create_rebar_group`, `create_single_rebar`, `modify_user_properties`, `get_reinforcement_info`, `list_drawings`, `export_ifc`.
  - C# Roslyn Scripting Guide for Tekla Open API 2025:
    - Available globals: `model` (`Tekla.Structures.Model.Model`), `selector` (`ModelObjectSelector`), `log` (`Action<string>`), `args` (`IReadOnlyDictionary<string, object>`), `ct` (`CancellationToken`).
    - Transaction rules: `auto` commits model, `manual` leaves commit to script, `none` forbids `model.CommitChanges()`.
    - .NET Framework 4.8 BCL notes: Do NOT use .NET Core 2.1+ APIs like `Math.Clamp` (use `Math.Max`/`Math.Min`).
    - Rebar classes: `SingleRebar`, `BaseRebarGroup` / `RebarGroup`.
  - Troubleshooting & error codes (-32001 Guard violation, -32002 Busy, -32003 Host error).

### 4. Verification & Handoff
- Build the solution, execute tests, verify skill and AGENTS.md changes.
- Write report to `.agents/teamwork_preview_worker_m5/report.md`.
- Write handoff to `.agents/teamwork_preview_worker_m5/handoff.md`.
- Notify parent orchestrator with completion message.

## 2026-09-21T19:23:18Z
Received dispatch assignment for Milestone 5 (Solution Packaging, AGENTS.md Registration & Skill Documentation).
