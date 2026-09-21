## 2026-09-21T15:18:18Z
You are worker_m4_1 (Automated Test Suites Worker) for the HPRobot MCP Subsystem.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m4_1\
Your parent is the Project Orchestrator (orchestrator_7, conversation ID: b32c5a58-8b71-46dd-ba9a-5c9e4b6709de).

MANDATORY FIRST STEP:
Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (specifically the latest section ## 2026-09-21T13:16:14Z) and g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_7\PROJECT.md.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

READ INVESTIGATION FINDINGS:
Read the comprehensive findings from all 3 M4 Explorers:
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m4_1\handoff.md (Project Configuration, Dependencies, MTP runner)
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m4_2\handoff.md (Detailed Test Suites for Profile, Catalog, and Pipe Execution)
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m4_3\handoff.md (Seed Compilation Tests & HPRobot.slnx Integration)

WRITE OWNERSHIP:
You exclusively own:
- `HPRobot/HPRobot.Mcp.Server.Tests/**`
- `HPRobot/HPRobot.slnx` (adding project registration only)

YOUR TASKS:
1. Create `HPRobot/HPRobot.Mcp.Server.Tests/HPRobot.Mcp.Server.Tests.csproj`:
   - Target `net10.0`, `<OutputType>Exe</OutputType>`, `<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>`.
   - Reference `HPRobot.Mcp.Server`, `HPRebar.Mcp.Server.Core`, `HPRebar.McpBridge.Core`, `HPRebar.Mcp.Contracts`.
   - Link `..\..\McpShared\HPRebar.Mcp.Server.Core.Tests\Fakes\FakeRevitExecutor.cs`.
   - Packages: `xunit.v3` (3.1.0), `xunit.runner.visualstudio` (3.1.5), `Microsoft.Bcl.AsyncInterfaces` (10.0.12) to silence `MSB3277`.
2. Implement Test Classes:
   - `RobotHostProfileTests.cs`: Profile constants, default version 2026, prefix "robot.", imports, globals, 300s timeout ceiling, options binding, static tool surface.
   - `SeedCatalogTests.cs`: Discovery of 12 manifest seeds, schema validity, examples validation (>= 2 examples with distinct "args" matching schema), guard check, dynamic 24-tool registration.
   - `SeedExecutionTests.cs`: Named pipe round-trip tests using `FakeRevitExecutor` verifying `robot.ping`, `robot.context`, `robot.execute`, `robot.cancel`, parameter forwarding, snapshot name, static preview, timeout ceiling.
   - `SeedCompilationTests.cs`: Dynamic Roslyn compilation of all 12 seeds against `Interop.RobotOM.dll` (with `FindWrapper()` and `Assert.SkipWhen(...)` if not installed).
3. Register Project in `HPRobot/HPRobot.slnx`:
   - Add `<Project Path="HPRobot.Mcp.Server.Tests/HPRobot.Mcp.Server.Tests.csproj" />` directly after `HPRobot.Mcp.Server`.
4. Build and Test Verification:
   - Build solution: `dotnet build HPRobot/HPRobot.slnx -c Debug` and `-c Release`. Verify 0 warnings, 0 errors.
   - Run server test suite: `dotnet run --project HPRobot/HPRobot.Mcp.Server.Tests/HPRobot.Mcp.Server.Tests.csproj`. Verify 100% passing.
   - Run bridge test suite: `dotnet run --project HPRobot/HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj`. Verify 197/197 passing.
   - Run McpShared regression tests: verify 685/685 passing.
5. Documentation:
   - Record genuine, unadulterated verbatim terminal outputs in `handoff.md`.

DELIVERABLES:
Write your implementation report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m4_1\changes.md`
And write your handoff report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m4_1\handoff.md`
When finished, send a message to your parent with summary and file paths.
