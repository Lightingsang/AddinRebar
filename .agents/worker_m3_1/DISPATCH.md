## 2026-09-21T14:24:36Z

You are worker_m3_1 (HPRobot Server & Tools Worker) for the HPRobot MCP Subsystem.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m3_1\
Your parent is the Project Orchestrator (orchestrator_7, conversation ID: b32c5a58-8b71-46dd-ba9a-5c9e4b6709de).

MANDATORY FIRST STEP:
Read:
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (specifically ## 2026-09-21T13:16:14Z)
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_7\PROJECT.md
- Survey findings in:
  - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_robot_3\analysis.md` (Contains exact JSON schemas, parameters, and verified C# Roslyn scripts for all 12 seeds!)
- Sibling references:
  - `HPSap2000/HPSap2000.Mcp.Server/` and `HPEtabs/HPEtabs.Mcp.Server/`

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

YOUR MISSION (Milestone M3 — HPRobot Stdio Server & 24 Tools Catalog):
Build the complete stdio MCP server `HPRobot.Mcp.Server` in `HPRobot/`:

1. Project Setup:
   - Create `HPRobot/HPRobot.Mcp.Server/HPRobot.Mcp.Server.csproj` targeting `net10.0`.
   - ProjectReference: `../../McpShared/HPRebar.Mcp.Server.Core/HPRebar.Mcp.Server.Core.csproj`.
   - Reference `Interop.RobotOM.dll` from `$(RobotInstallDir)` with `<Private>false</Private>` and `<EmbedInteropTypes>false</EmbedInteropTypes>`.
   - Embed seed resources: `<EmbeddedResource Include="Registry\SeedLibrary\**\*" />`.
   - Update `HPRobot/HPRobot.slnx` to include `HPRobot.Mcp.Server.csproj`.

2. Server Implementation:
   - `Program.cs`: Clean standard entry point running `McpServerHost.CreateBuilder(args, new RobotHostProfile()).Build().RunAsync();`.
   - `appsettings.json`: Standard config with `Bridge:PipeName = "hprobot-mcp-2026"` and `Bridge:HostVersion = 2026`.
   - `Hosts/Robot/`:
     - `RobotHostProfile.cs`: Implements `IHostProfile`. Sets HostName = "robot", PipeName = "hprobot-mcp-2026", JsonRpcPrefix = "robot.", DefaultVersion = 2026, MaxTimeoutSeconds = 300, hints, and registers core tools, resources, and prompts.
     - `RobotContextService.cs`: Custom context shaping if needed, or default via HostProfile.
     - `Tools/GetRobotContextTool.cs`: Core tool `get_robot_context`.
     - `Tools/ExecuteRobotCodeTool.cs`: Core tool `execute_robot_code`.
     - `Resources/RobotResourceProvider.cs`: Exposes `robot://` resources.
     - `Prompts/RobotPromptProvider.cs`: Exposes Roslyn RobotOM prompt templates.

3. Complete 12 Embedded Seed Tools in `Registry/SeedLibrary/`:
   Every seed tool directory must contain `tool.json`, `code.cs`, and `examples.json`:
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
   (Extract and use the production-ready C# code and schemas from `explorer_survey_robot_3/analysis.md`).

4. Build Verification:
   - Run `dotnet build HPRobot/HPRobot.slnx -c Debug` and `-c Release`.
   - Ensure 0 warnings and 0 errors.

WRITE OWNERSHIP:
You own exclusively `HPRobot/HPRobot.Mcp.Server/` and adding its project entry to `HPRobot/HPRobot.slnx`.

DELIVERABLES:
Write your changes report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m3_1\changes.md`
And write your handoff report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m3_1\handoff.md`
Include build commands and execution results. When finished, send a message to your parent.
