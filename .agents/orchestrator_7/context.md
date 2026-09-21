# Orchestrator 7 Context - HPRobot Subsystem

Project: Complete Robot Structural Analysis Professional 2026 MCP Subsystem (HPRobot)
Original Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md
Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot
Repo Root: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar
Orchestrator Workspace: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_7

## Deliverables
- McpShared Host Integration:
  - `PipeNaming.RobotHost` ("hprobot-mcp-2026"), `JsonRpcMethods.RobotPrefix` ("robot.").
  - `GuardProfile.Robot` and `AnalyzerProfile.Robot` (allow namespace `RobotOM`, deny-list file I/O & reflection).
  - `HostScriptContracts.RobotImports` (`RobotOM`, `System`, `System.Collections.Generic`, `System.Linq`), `RobotGlobals` (`robot`, `structure`, `args`, `units`), `RobotHeavyMaxTimeoutSeconds` (300s).
  - `ContextResult.Robot` & `RobotInfo` DTOs.
  - McpShared tests (164 tests net10 + 62 tests net48) continue to pass 100%.
- HPRobot.McpBridge (.NET 8.0-windows, WPF):
  - Standalone desktop application connecting out-of-process via COM Interop `RobotOM.dll` (`C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll`).
  - Named Pipe `hprobot-mcp-2026`, dispatching `robot.execute` and `robot.context`.
  - MaterialDesignThemes 5.3.2 UI (Dark/Light mode) showing COM connection status, open RTD model info, live log, and safety checkboxes: `AllowExecution`, `AllowHeavyOperations`.
  - `RobotUnitsPolicy`: Metric standardization (Meter, kN, kN·m, MPa) during script execution and restore on finish.
- 3-Tier Safety System & RTD Snapshot:
  - `RobotTierAnalyzer`: Read (no snapshot), Write (backup snapshot .rtd to `.hprobot_snapshots/` or `%TEMP%`), Delete/Heavy (requires `AllowHeavyOperations` checkbox).
- HPRobot.Mcp.Server (.NET 10 Console, Stdio):
  - Stdio server compliant with MCP 2.2.0.
  - 24 tools: 4 Core (`execute_robot_code`, `get_robot_context`, `robot://` resources, Prompts), 8 Registry Meta Tools, 12 Embedded Seed Tools (`get_model_info`, `get_structural_objects`, `get_materials_and_sections`, `get_coordinate_systems_and_grids`, `get_load_definitions`, `draw_bar_by_coords`, `assign_node_support`, `assign_bar_section`, `assign_bar_load`, `run_calculations`, `get_node_reactions`, `get_bar_forces`).
- Test Suites & Unattended Live Harness:
  - `HPRobot.Mcp.Server.Tests` (.NET 10 xUnit): 24 tools check, seed compile check, mock bridge pipe flow.
  - `HPRobot.McpBridge.Tests` (.NET 8 Windows xUnit): Tier analyzer, snapshot manager, units policy.
  - `HPRobot/tools/harness/`: Python `run-live-verify.ps1` / `live-verify.py` inheriting `McpShared/tools/harness_common.py`.
- Solution: `HPRobot/HPRobot.slnx` containing all 4 projects building cleanly with 0 errors.
- Documentation: Skill file `.agents/skills/hp-mcp-robot/SKILL.md` and repository registration in `AGENTS.md`.
