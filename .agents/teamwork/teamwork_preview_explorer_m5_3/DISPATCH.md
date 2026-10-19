# DISPATCH — teamwork_preview_explorer_m5_3

## Task Assignment
**Role**: Solution Integration & Test Harness Explorer (teamwork_preview_explorer)
**Mission**: Investigate solution integration and automation runner mechanics for Milestone 5 and acceptance pipelines.

## Required Reading
1. `ORIGINAL_REQUEST.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`
2. Project Specifications: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_4\PROJECT.md`
3. Solution files:
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.slnx`
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\Directory.Build.props`
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\run-e2e-dynamo-tests.ps1`

## Scope of Investigation
1. **Solution Integration**:
   - How `HPDynamo.Mcp.Server.Tests` and `HPDynamo.McpBridge.Tests` should be added to `HPDynamo/HPDynamo.slnx` (XML format).
   - Ensure `<Build Project="true"/>` for standard test execution, or configure appropriate build filters.
   - Verify that adding test projects does not cause build collisions or require live host DLLs in output bins.
2. **Runner & Script Automation**:
   - How `dotnet test` or `powershell` test scripts can run all unit and integration tests across the solution in under 30 seconds.
   - How to integrate `HPDynamo.Mcp.Server.Tests` and `HPDynamo.McpBridge.Tests` into `run-e2e-dynamo-tests.ps1` or a dedicated verification script.
   - Avoid passing `--nologo` to MTP / xUnit v3 runners (causes failure).
3. **Clean Code & Verification Standards**:
   - Ensure test projects adhere to the repository's Clean Code rules and isolation bounds.
   - Document the exact commands to build and run all test suites.

## Deliverable
Write your comprehensive architectural report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m5_3\handoff.md`
When complete, send a message back to orchestrator_4.

## 2026-10-08T11:44:48Z
[Message] timestamp=2026-10-08T11:44:48Z sender=b20bb4a5-dd75-4c48-9f4d-621e02412393 priority=MESSAGE_PRIORITY_HIGH content=You are teamwork_preview_explorer_m5_3.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m5_3\
Read your dispatch instructions in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m5_3\DISPATCH.md
Read the authoritative request in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Read the project specifications in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_4\PROJECT.md

Investigate solution integration and automation runner mechanics for Milestone 5:
1. Solution integration in HPDynamo/HPDynamo.slnx (XML format) for HPDynamo.Mcp.Server.Tests and HPDynamo.McpBridge.Tests.
2. Directory.Build.props settings: Ensure tests build cleanly without copying unwanted host DLLs or conflicting with Microsoft.Testing.Platform.
3. Automation runner integration: How dotnet test and powershell harnesses can execute both unit test suites and acceptance suites rapidly (<30s).
4. Do NOT pass --nologo to MTP / xUnit v3 runners.
Write your integration report to:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m5_3\handoff.md
When finished, send a message back to orchestrator_4.
