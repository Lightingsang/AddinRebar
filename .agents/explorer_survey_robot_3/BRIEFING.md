# BRIEFING — 2026-09-21T13:25:30Z

## Mission
Design the complete 24-tool catalog for HPRobot.Mcp.Server, the 3-Tier Safety System (`RobotTierAnalyzer`) with RTD snapshot management, and the test suite / live harness architecture for Autodesk Robot Structural Analysis Professional 2026.

## 🔒 My Identity
- Archetype: Explorer / Tool Catalog and Safety Engineer
- Roles: Read-only investigation, tool catalog design, safety analyzer design, test architecture specification
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_robot_3\
- Original parent: orchestrator_7 (conversation ID: b32c5a58-8b71-46dd-ba9a-5c9e4b6709de)
- Milestone: HPRobot Discovery & Architectural Planning

## 🔒 Key Constraints
- Read-only investigation — do NOT implement production code in source trees
- Follow MCP architecture standards established across HPRebar, HPAutoCad, HPNavis, HPEtabs, HPCivil3d, HPSap2000, HPPowerBi, HPExcel
- Keep dependencies strictly host-neutral -> McpShared; no cross-referencing between host folders
- Deliver full JSON schemas and C# Roslyn script code for all 12 seed tools
- Detail 3-tier safety analyzer AST classification for RobotOM API

## Current Parent
- Conversation ID: b32c5a58-8b71-46dd-ba9a-5c9e4b6709de
- Updated: 2026-09-21T13:25:30Z

## Investigation State
- **Explored paths**:
  - `C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll` (reflection confirmed 3,085 types)
  - `HPSap2000/`, `HPEtabs/`, `HPExcel/`, `HPNavis/`, `McpShared/`
- **Key findings**:
  - Full 24-tool catalog designed (4 core, 8 registry meta, 12 embedded seeds).
  - 12 seed tools specified with production C# Roslyn code and JSON schemas mapped to RobotOM types.
  - 3-Tier safety architecture (`RobotTierAnalyzer`) designed with AST visitor, allow-list fixture, fail-closed security.
  - Pre-run snapshot manager designed with dual-bucket system (presave + prerun) for `.rtd` files.
  - Metric standardizer `RobotUnitsPolicy` designed (`m, kN, kN·m, MPa`).
  - Test suites (`HPRobot.Mcp.Server.Tests`, `HPRobot.McpBridge.Tests`, `tools/harness/`) specified.
- **Unexplored areas**: None within scope.

## Key Decisions Made
- Standard 24-tool surface adopted (4 core + 8 meta + 12 seeds).
- Out-of-process standalone WPF bridge (`HPRobot.McpBridge`, .NET 8.0-windows) with Named Pipe `hprobot-mcp-2026`.
- Solver execution (`Calculate()`) and element deletion classified as Tier D / Heavy, requiring `AllowHeavyOperations`.
- Timeout clamp set to 300s (`RobotHeavyMaxTimeoutSeconds`).
- Host isolation strictly preserved (`HPRobot/` -> `../McpShared/`).

## Artifact Index
- DISPATCH.md — Dispatch log
- BRIEFING.md — Persistent working memory
- progress.md — Liveness heartbeat
- analysis.md — Full investigation report with complete schemas and C# scripts
- handoff.md — Self-contained 5-component handoff report
