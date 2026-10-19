# DISPATCH — auditor_m4_r3

## Task Objective
Conduct a Forensic Integrity Audit of Milestone M4 deliverables and recent test integrity remediation.

## Context & Inputs
- Original Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`
- Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_14\plan.md`
- Worker Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_fix2\handoff.md`

## Instructions
1. Perform forensic checks:
   - Check `PublicRouteRegressionTests.cs`: confirm dead facade lines 130–135 are gone, line count <= 300 lines (Rule C2), method lengths <= 50 lines (Rule M3).
   - Check `PlayerRunner.cs`: line count <= 300 lines (Rule C2), method lengths <= 50 lines (Rule M3).
   - Check `PlayerIntegrationTests.cs`: genuine non-mocked tests exercising real `GH_SolutionPhase.Failed` logic.
   - Check `git status McpShared`: strictly 0 modifications.
   - Check 0 dead code (Q-B1), 0 unannotated catch blocks (Q-B2), 0 cheating/dummy facades.
2. Run all solution and engine test suites:
   `dotnet test HPGrasshopper/HPGrasshopper.McpBridge.Tests/HPGrasshopper.McpBridge.Tests.csproj -c Debug -p:DeployAddin=false`
   `dotnet test HPGrasshopper/HPGrasshopper.Mcp.Server.Tests/HPGrasshopper.Mcp.Server.Tests.csproj -c Debug -p:DeployAddin=false`
   `dotnet test HPGrasshopper/HPGrasshopper.E2E.Tests/HPGrasshopper.E2E.Tests.csproj -c Debug -p:DeployAddin=false`
   `dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj -c Debug`
3. Provide your binary audit verdict (`CLEAN` or `INTEGRITY VIOLATION`).
4. Write your report to `handoff.md` and notify orchestrator (`be638180-e3a9-46c4-834a-a5beaaf11a17`) via `send_message`.
