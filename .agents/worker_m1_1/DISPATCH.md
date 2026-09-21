## 2026-09-21T13:32:07Z

You are worker_m1_1 (McpShared Host Integration Worker) for the HPRobot MCP Subsystem.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m1_1\
Your parent is the Project Orchestrator (orchestrator_7, conversation ID: b32c5a58-8b71-46dd-ba9a-5c9e4b6709de).

MANDATORY FIRST STEP:
Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (specifically the latest section ## 2026-09-21T13:16:14Z) and g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_7\PROJECT.md.

Also read the survey findings in:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_robot_1\handoff.md` and `analysis.md`.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

YOUR MISSION (Milestone M1 — McpShared Robot Integration):
Implement the Robot Structural Analysis Professional 2026 host integration in `McpShared/`:

1. `McpShared/HPRebar.Mcp.Contracts/PipeNaming.cs`:
   - Add `public const string RobotHost = "robot";`
   - In `For(string host, int version)`, map `RobotHost => $"hprobot-mcp-{version}",`

2. `McpShared/HPRebar.Mcp.Contracts/JsonRpc/JsonRpcMethods.cs`:
   - Add `public const string RobotPrefix = "robot.";`

3. `McpShared/HPRebar.Mcp.Contracts/HostScriptContracts.cs`:
   - Add `RobotImports`: `new[] { "RobotOM", "System", "System.Collections.Generic", "System.Linq", "HPRebar.McpBridge.Core.Scripting" }`
   - Add `RobotGlobals`: `new[] { "robot", "structure", "units", "ct", "log", "progress", "args" }`
   - Add `RobotHeavyMaxTimeoutSeconds = 300;`

4. `McpShared/HPRebar.Mcp.Contracts/Messages/ContextMessages.cs`:
   - Add property in `ContextResult`: `RobotInfo? Robot = null`
   - Define DTO record:
     ```csharp
     public sealed record RobotInfo(
         bool IsAttached,
         int? AttachedPid,
         string? RobotVersion,
         string? StructureType,
         bool IsCalculated,
         bool HeavyOperationsEnabled,
         int NodeCount,
         int BarCount,
         int PanelCount,
         int LoadCaseCount);
     ```

5. `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs`:
   - Add `GuardProfile.Robot` with allowed namespaces (`RobotOM`, etc.) and denied methods/types (`Quit`, `ApplicationExit`, `Interactive`, `MessageBox`, `System.Diagnostics.Process`, `#r`/`#load` directives, reflection, threading, bridge host internals).

6. `McpShared/HPRebar.McpBridge.Core/Scripting/AnalyzerProfile.cs`:
   - Add `AnalyzerProfile.Robot` (empty transaction sets, matching CSI COM model).

7. `McpShared/HPRebar.Mcp.Server.Core.Tests/`:
   - Add unit tests verifying `RobotHost` pipe naming, prefix, contracts, context serialization, guard profile, and analyzer profile.
   - Run the McpShared test suites:
     `dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj`
     `dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests/HPRebar.McpBridge.Core.Net48Tests.csproj`
   - Ensure 100% of tests pass (baseline was 456 tests: 385 net10 + 71 net48, now must be 456+ tests with 0 failures, 0 skipped).

WRITE OWNERSHIP:
You own exclusively the files mentioned above in `McpShared/`.

DELIVERABLES:
Write your implementation report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m1_1\changes.md`
And write your handoff report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m1_1\handoff.md`
Include build and test commands and exact execution results in your handoff. When finished, send a message to your parent.
