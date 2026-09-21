## 2026-09-21T13:18:23Z
You are Explorer 3 (Tool Catalog and Safety Engineer) for the HPRobot MCP Subsystem.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_robot_3\
Your parent is the Project Orchestrator (orchestrator_7, conversation ID: b32c5a58-8b71-46dd-ba9a-5c9e4b6709de).

MANDATORY FIRST STEP:
Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (specifically the latest section ## 2026-09-21T13:16:14Z) and g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_7\context.md.

YOUR MISSION:
Design the complete 24-tool catalog for HPRobot.Mcp.Server, the 3-Tier Safety System (`RobotTierAnalyzer`) with RTD snapshot management, and the test suite / live harness architecture.

INVESTIGATION SCOPE:
1. Complete 24-Tool Catalog Design:
   - 4 Core Tools:
     - `execute_robot_code`: Roslyn C# execution against RobotOM with 3-tier safety checks.
     - `get_robot_context`: Extracts active model metadata, structural mode, node/bar/panel counts, load cases, calculation status.
     - `robot://` resources: Resource schemas for model structure and results.
     - Prompts: Prompt templates for Robot script generation.
   - 8 Registry Meta Tools: Inherited from McpShared (`search_tools`, `propose_tool`, `test_tool`, `publish_tool`, `disable_tool`, `enable_tool`, `deprecate_tool`, `delete_tool`).
   - 12 Embedded Seed Tools: Provide exact JSON schema and C# Roslyn script implementation for:
     1. `get_model_info`
     2. `get_structural_objects`
     3. `get_materials_and_sections`
     4. `get_coordinate_systems_and_grids`
     5. `get_load_definitions`
     6. `draw_bar_by_coords`
     7. `assign_node_support`
     8. `assign_bar_section`
     9. `assign_bar_load`
     10. `run_calculations`
     11. `get_node_reactions`
     12. `get_bar_forces`
2. 3-Tier Safety System (`RobotTierAnalyzer`):
   - Tier R (Read): No snapshot. Allowed by default.
   - Tier W (Write): Requires `AllowExecution`. Automatically takes .rtd snapshot to `.hprobot_snapshots/` or `%TEMP%`.
   - Tier D / Heavy (Delete structural objects, run calculations `Calculate()`): Requires `AllowHeavyOperations` checkbox.
   - Map Roslyn AST syntax patterns for RobotOM to classify methods/properties into R/W/Heavy.
   - Snapshot Manager: Save backup `.rtd` with timestamp before write operations; return snapshot path in response.
3. Test Architecture:
   - `HPRobot.Mcp.Server.Tests` (.NET 10 xUnit): 24 tools validation, fake executor pipe round-trip, seed Roslyn compilation.
   - `HPRobot.McpBridge.Tests` (.NET 8 Windows xUnit): RobotTierAnalyzer tests, snapshot manager tests, RobotUnitsPolicy tests.
   - `HPRobot/tools/harness/`: `run-live-verify.ps1` and `live-verify.py` using `McpShared/tools/harness_common.py`.
4. Solution structure:
   - Solution `HPRobot/HPRobot.slnx` with 4 projects.
   - Directory.Build.props referencing `McpShared/` and `RobotOM.dll`.

DELIVERABLES:
Write your comprehensive investigation report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_robot_3\analysis.md`
And write your self-contained handoff report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_robot_3\handoff.md`
When finished, send a message to your parent with summary and file paths.
