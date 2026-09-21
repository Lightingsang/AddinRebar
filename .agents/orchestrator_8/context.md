# Orchestrator 8 Context - HPTekla Subsystem

Project: Complete Trimble Tekla Structures 2025.0 MCP Solution (HPTekla)
Original Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md
Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPTekla
Repo Root: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar
Orchestrator Workspace: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_8

## Requirements Overview
- R1. Additive Integration in McpShared:
  - `PipeNaming.TeklaHost = "tekla"`, pipe `hptekla-mcp-{version}`, method prefix `tekla`.
  - `HostScriptContracts.TeklaImports` (`Tekla.Structures`, `Tekla.Structures.Model`, `Tekla.Structures.Geometry3d`, `Tekla.Structures.Catalogs`), `TeklaGlobals` (`model`, `selector`, `ct`, `log`, `args`).
  - `GuardProfile.Tekla` and `AnalyzerProfile.Tekla`.
  - `ContextResult.Tekla` and `TeklaInfo` DTOs.
  - `HostProfile.Tekla`: `DefaultVersion = 2025`, `ValidVersions = [2025]`.
  - 100% additive, McpShared tests continue to pass 100%.
- R2. HPTekla.McpBridge (.NET Framework 4.8, In-Process Plugin for Tekla Structures 2025.0):
  - In-Process plugin / extension with Ribbon button and WPF status window.
  - Named Pipe `hptekla-mcp-2025`.
  - Safe thread synchronization on Tekla Model/UI thread.
  - 3-tier safety (Read / Write / Destructive), `dryRun = true` rollback enforcement without `model.CommitChanges()`.
  - Model snapshot manager.
- R3. HPTekla.Mcp.Server (.NET 10 Console, Stdio):
  - 24 tools: 4 Core, 8 Registry Meta, 12 Embedded Seeds (`get_model_info`, `select_objects`, `get_part_properties`, `create_beam`, `create_column`, `create_contour_plate`, `create_rebar_group`, `create_single_rebar`, `modify_user_properties`, `get_reinforcement_info`, `list_drawings`, `export_ifc`).
- R4. Test Suites & Unattended Live Harness:
  - `HPTekla.Mcp.Server.Tests` (.NET 10).
  - `HPTekla.McpBridge.Tests` (.NET Framework 4.8).
  - `HPRebar.Mcp.Server.Core.Tests` host-neutrality verification.
  - `HPTekla/tools/harness/` Python live verification harness.
- Solution & Integrity:
  - `HPTekla/HPTekla.slnx` containing the 4 projects, cleanly building, zero cross-dependencies with other host MCPs.
  - Skill `.agents/skills/hp-mcp-tekla/SKILL.md` and repository registration in `AGENTS.md`.
