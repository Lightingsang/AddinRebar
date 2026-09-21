# BRIEFING — 2026-09-22T00:23:00+07:00

## Mission
Investigate and design HPTekla.Mcp.Server architecture, 24 tools catalog (4 core + 8 registry meta + 12 embedded seeds for steel/rebar), test suites (HPTekla.Mcp.Server.Tests net10, HPTekla.McpBridge.Tests net48), and Python live verification harness.

## 🔒 My Identity
- Archetype: explorer
- Roles: survey, analysis
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_explorer_survey_3
- Original parent: 5d7560ee-5142-428f-a172-e73cf7738ac1
- Milestone: HPTekla MCP Server, 24 Tools Catalog & Test Suites Design

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- Follow HP MCP architectural standards (McpShared/ contracts, IHostProfile, 24 tools)
- Respect repository layout: HPTekla references McpShared only, never sibling hosts

## Current Parent
- Conversation ID: 5d7560ee-5142-428f-a172-e73cf7738ac1
- Updated: 2026-09-22T00:23:00+07:00

## Investigation State
- **Explored paths**:
  - `McpShared/` (`PipeNaming.cs`, `JsonRpcMethods.cs`, `HostScriptContracts.cs`, `ContextMessages.cs`, `GuardProfile.cs`, `AnalyzerProfile.cs`, `IHostProfile.cs`, `HostProfile.cs`, `McpServerHost.cs`)
  - `HPRobot/` (`RobotHostProfile.cs`, `ExecuteRobotCodeTool.cs`, `GetRobotContextTool.cs`, `HPRobot.Mcp.Server.csproj`, `SeedCatalogTests.cs`, `SeedCompilationTests.cs`, `RobotHostProfileTests.cs`)
  - `HPNavis/` (`NavisHostProfile.cs`, `HPNavis.Mcp.Server.csproj`, `HPNavis.McpBridge.Tests.csproj`, `tools/harness/`)
  - `HPAutoCad/` (`AutocadHostProfile.cs`, `tools/harness/`)
  - `HPExcel/` (`ExcelHostProfile.cs`, `tools/harness/`)
- **Key findings**:
  - `HPTekla.Mcp.Server` must target .NET 10, expose 24 tools (4 Core, 8 Registry Meta, 12 Embedded Seeds).
  - `HPTekla.McpBridge` must target .NET Framework 4.8 to match Tekla Structures 2025 CLR runtime.
  - McpShared additions are 100% additive (`PipeNaming.TeklaHost = "tekla"`, `JsonRpcMethods.TeklaPrefix = "tekla."`, `GuardProfile.Tekla`, `AnalyzerProfile.Tekla`, `TeklaInfo`).
  - Seed tools cover structural steel (`create_beam`, `create_column`, `create_contour_plate`) and concrete rebar (`create_rebar_group`, `create_single_rebar`, `get_reinforcement_info`), plus UDAs, model info, drawing query, and IFC export.
- **Unexplored areas**: None within the survey scope.

## Key Decisions Made
- Matched HPTekla architecture to `HPNavis` for runtime framework (`net48` in-process bridge) and `HPRobot`/`HPAutoCad` for 24-tool catalog and 3-tier safety design.
- Seed compilation tests will gracefully skip with `Assert.SkipWhen` if Tekla 2025 is not installed on the dev machine, ensuring CI compatibility.

## Artifact Index
- report.md — Comprehensive analysis and design specification for HPTekla MCP Server, 24 tools, seeds, test suites, and harness
- handoff.md — 5-component self-contained handoff report
- progress.md — Liveness heartbeat and milestone progress
