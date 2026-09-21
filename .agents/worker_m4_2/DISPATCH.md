## 2026-09-21T15:40:19Z

You are worker_m4_2 (HPRobot M4 Remediation Worker) for the HPRobot MCP Subsystem.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m4_2\
Your parent is the Project Orchestrator (orchestrator_7, conversation ID: b32c5a58-8b71-46dd-ba9a-5c9e4b6709de).

MANDATORY FIRST STEP:
Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (specifically the latest section ## 2026-09-21T13:16:14Z) and g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_7\PROJECT.md.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

READ INVESTIGATION FINDINGS:
Read the findings from all 3 M4 R2 Explorers:
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m4_r2_1\handoff.md (Async Race Condition & Cancellation Analysis)
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m4_r2_2\handoff.md (Solution Test Runner & Multi-Run Stress Protocol)
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m4_r2_3\handoff.md (Audit Verification & Test Honesty Checklist)

WRITE OWNERSHIP:
You exclusively own:
`HPRobot/HPRobot.Mcp.Server.Tests/**`

YOUR TASKS:
1. Fix the async race condition in `HPRobot/HPRobot.Mcp.Server.Tests/SeedExecutionTests.cs`:
   In method `Timeout_InformsModelThatChangesMayHavePersisted`, replace line 356:
   ```csharp
   Assert.True(_executor.CancelCalls > 0);
   ```
   with the canonical polling loop matching HPExcel and HPPowerBi:
   ```csharp
   var deadline = DateTime.UtcNow.AddSeconds(3);
   while (DateTime.UtcNow < deadline && _executor.CancelCalls == 0)
   {
       await Task.Delay(50);
   }
   Assert.True(_executor.CancelCalls > 0, "Cancel() was not called on the executor when client timed out.");
   ```
2. Build Verification:
   - `dotnet build HPRobot/HPRobot.slnx -c Debug`
   - `dotnet build HPRobot/HPRobot.slnx -c Release`
   Verify 0 warnings, 0 errors.
3. Test Verification:
   - Run server tests: `dotnet run --project HPRobot/HPRobot.Mcp.Server.Tests/HPRobot.Mcp.Server.Tests.csproj` (97 passed).
   - Run bridge tests: `dotnet run --project HPRobot/HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj` (197 passed).
   - Run mandatory 5-consecutive-run solution test stress loop:
     `powershell -Command "1..5 | ForEach-Object { Write-Host \"`n=== RUN $_ / 5 ===\"; dotnet test \"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.slnx\"; if (`$LASTEXITCODE -ne 0) { exit `$LASTEXITCODE } }"`
     Verify all 5 runs pass 294/294 with exit code 0.
   - Run McpShared regression tests: verify 685/685 pass.
4. Documentation:
   - Record genuine, unabridged verbatim terminal output in `changes.md` and `handoff.md`.
   - Include the Flake Verification Table documenting all 5 consecutive solution test runs.

DELIVERABLES:
Write your implementation report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m4_2\changes.md`
And write your handoff report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m4_2\handoff.md`
When finished, send a message to your parent with summary and file paths.
