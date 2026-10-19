# Dispatch: Worker M1 (McpShared Dynamo Profile & Pipe Contracts)

## Mission
Implement Milestone 1 (Dynamo Host Profile & Pipe Contracts) in `McpShared/` and verify that all unit and regression tests pass 100% green.

## Mandatory Inputs & Handoff Reports
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (mandatory read)
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_4\PROJECT.md`
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m1_1\handoff.md` (Contracts & DTOs)
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m1_2\handoff.md` (Profiles & Guard rules)
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m1_3\handoff.md` (Test suites & verbatim code)

## Write Ownership & File Boundaries
You exclusively own and may modify or create the following files:
1. `McpShared/HPRebar.Mcp.Contracts/PipeNaming.cs`
2. `McpShared/HPRebar.Mcp.Contracts/JsonRpc/JsonRpcMethods.cs`
3. `McpShared/HPRebar.Mcp.Contracts/HostScriptContracts.cs`
4. `McpShared/HPRebar.Mcp.Contracts/Messages/ContextMessages.cs`
5. `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs`
6. `McpShared/HPRebar.McpBridge.Core/Scripting/AnalyzerProfile.cs`
7. `McpShared/HPRebar.Mcp.Server.Core/Hosts/HostProfile.cs`
8. `McpShared/tools/live-verify-quality.py`
9. `McpShared/HPRebar.Mcp.Server.Core.Tests/DynamoTestProfile.cs` (new)
10. `McpShared/HPRebar.Mcp.Server.Core.Tests/DynamoProfileTests.cs` (new)
11. `McpShared/HPRebar.Mcp.Server.Core.Tests/HostNeutralityTests.cs`
12. `McpShared/HPRebar.Mcp.Server.Core.Tests/ContextServiceTests.cs`
13. `McpShared/HPRebar.McpBridge.Core.Net48Tests/TeklaMilestone1Challenger2Net48Tests.cs` (if needed for pipe assertion update)

## Verification Requirements
You MUST run the following build and test commands and document exact test counts:
```powershell
dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.Mcp.Server.Core.Tests\HPRebar.Mcp.Server.Core.Tests.csproj"
dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.McpBridge.Core.Net48Tests\HPRebar.McpBridge.Core.Net48Tests.csproj"
```
Do NOT pass `--nologo` to `dotnet test` as it causes MTP runner errors.
Ensure all existing 793 tests + 11 new tests = 804 tests pass in `HPRebar.Mcp.Server.Core.Tests`.

## Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Output
Write your handoff report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_m1_1\handoff.md`
When finished, send a message back to orchestrator_4.

## 2026-10-08T08:34:27Z
You are teamwork_preview_worker_m1_1.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_m1_1\
Read your dispatch instructions in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_m1_1\DISPATCH.md
Read the authoritative request in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Read the project specifications in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_4\PROJECT.md
Read the complete Explorer findings:
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m1_1\handoff.md
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m1_2\handoff.md
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m1_3\handoff.md

Implement Milestone 1 (Dynamo Host Profile & Pipe Contracts) in McpShared/:
1. McpShared/HPRebar.Mcp.Contracts:
   - PipeNaming.cs (PipeNaming.DynamoHost = "dynamo", For switch)
   - JsonRpcMethods.cs (DynamoPrefix = "dynamo.")
   - HostScriptContracts.cs (DynamoImports, DynamoGlobals, DynamoHeavyMaxTimeoutSeconds = 600)
   - ContextMessages.cs (ContextResult.Dynamo, DynamoInfo record)
2. McpShared/HPRebar.McpBridge.Core:
   - GuardProfile.cs (GuardProfile.Dynamo)
   - AnalyzerProfile.cs (AnalyzerProfile.Dynamo)
3. McpShared/HPRebar.Mcp.Server.Core:
   - HostProfile.cs (HostProfile.Dynamo)
4. McpShared/tools/live-verify-quality.py ("dynamo" entry)
5. McpShared/HPRebar.Mcp.Server.Core.Tests:
   - DynamoTestProfile.cs (new)
   - DynamoProfileTests.cs (new, 11 tests)
   - HostNeutralityTests.cs (dynamo pipe & denial of Dynamo assemblies)
   - ContextServiceTests.cs (isolation assertion)
   - TeklaMilestone1Challenger2Net48Tests.cs in Net48Tests (pipe assertions)
