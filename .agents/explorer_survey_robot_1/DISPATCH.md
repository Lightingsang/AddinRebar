## 2026-09-21T13:18:22Z

You are Explorer 1 (McpShared Architecture Researcher) for the HPRobot MCP Subsystem.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_robot_1\
Your parent is the Project Orchestrator (orchestrator_7, conversation ID: b32c5a58-8b71-46dd-ba9a-5c9e4b6709de).

MANDATORY FIRST STEP:
Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (specifically the latest section ## 2026-09-21T13:16:14Z) and g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_7\context.md.

YOUR MISSION:
Investigate and map the host integration in McpShared and examine sibling COM host subsystems (HPEtabs, HPSap2000, HPExcel, HPPowerBi) as patterns for HPRobot.

INVESTIGATION SCOPE:
1. Examine McpShared projects:
   - `McpShared/HPRebar.Mcp.Contracts/` (PipeNaming, JsonRpcMethods, ContextResult, HostScriptContracts, DTOs).
   - `McpShared/HPRebar.McpBridge.Core/` (GuardProfile, AnalyzerProfile, ScriptGuard, ScriptUnits, SettingsStore).
   - `McpShared/HPRebar.Mcp.Server.Core/` (IHostProfile, ContextService, ToolRegistryEngine, etc.).
   - Review how tests are structured in `McpShared/HPRebar.Mcp.Server.Core.Tests/` and `McpShared/HPRebar.McpBridge.Core.Net48Tests/`.
2. Examine sibling COM host implementations:
   - Check `HPSap2000/` and `HPEtabs/` (and `HPExcel/`):
     - Solution structure, Directory.Build.props, COM wrappers / references.
     - Standalone WPF bridge app (`.McpBridge`), pipe listener, Roslyn script execution, units policy, tier analyzer.
     - Server stdio console app (`.Mcp.Server`), HostProfile implementation, embedded seeds, tool registration.
     - Test projects (`.Mcp.Server.Tests`, `.McpBridge.Tests`).
3. Detail the exact changes required in `McpShared/` for Robot Structural Analysis 2026:
   - `PipeNaming.RobotHost` = "hprobot-mcp-2026"
   - `JsonRpcMethods.RobotPrefix` = "robot."
   - `GuardProfile.Robot` and `AnalyzerProfile.Robot` (allowed namespace `RobotOM`, deny list for file I/O & reflection)
   - `HostScriptContracts.RobotImports` (`RobotOM`, `System`, `System.Collections.Generic`, `System.Linq`), `RobotGlobals` (`robot`, `structure`, `args`, `units`), `RobotHeavyMaxTimeoutSeconds` (300s)
   - `ContextResult.Robot` & `RobotInfo` DTOs
   - Any profile adaptations in `McpServerHost.ConfigureOptions` or `ContextService`.
4. Document the exact test suite and baseline for McpShared (e.g. 164 net10 tests + 62 net48 tests) and how to ensure zero regressions across all hosts.

DELIVERABLES:
Write your comprehensive investigation report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_robot_1\analysis.md`
And write your self-contained handoff report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_robot_1\handoff.md`
When finished, send a message to your parent with summary and file paths.
