# Dispatch: Worker M2 (HPDynamo Solution & MCP Server Executable)

## Mission
Implement Milestone 2: Create the `HPDynamo` solution structure and build `HPDynamo.Mcp.Server` (.NET 10.0 console application, stdio transport) with 4 Core tools, 8 Registry meta tools, and strict 24-tool surface enforcement.

## Mandatory Inputs & Handoff Reports
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (mandatory read)
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_4\PROJECT.md`
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_READY.md`
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m2_1\handoff.md` (Solution scaffolding & Directory.Build.props)
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m2_2\handoff.md` (Program.cs, DynamoHostProfile & Core tools)
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m2_3\handoff.md` (Registry meta tools & 24 tools surface filtering)

## Write Ownership & File Boundaries
You exclusively own and may create or edit:
1. `HPDynamo/HPDynamo.slnx`
2. `HPDynamo/Directory.Build.props`
3. `HPDynamo/global.json`
4. `HPDynamo/HPDynamo.Mcp.Server/HPDynamo.Mcp.Server.csproj`
5. `HPDynamo/HPDynamo.Mcp.Server/Program.cs`
6. `HPDynamo/HPDynamo.Mcp.Server/Hosts/DynamoHostProfile.cs`
7. `HPDynamo/HPDynamo.Mcp.Server/Tools/ExecuteDynamoCodeTool.cs`
8. `HPDynamo/HPDynamo.Mcp.Server/Tools/ExecuteDesignScriptTool.cs`
9. `HPDynamo/HPDynamo.Mcp.Server/Tools/DynamoContextTool.cs`

## Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Clean Code Governance (Mandatory)
- Classes <= 300 lines (Rule C2).
- Methods <= 50 lines (Rule M3).
- 0 empty catch blocks without explanatory comments.
- 0 commented-out code.
- Structured logging to stderr.

## Verification Requirements
You MUST run:
```powershell
dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server\HPDynamo.Mcp.Server.csproj"
dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.E2E.Tests\HPDynamo.E2E.Tests.csproj"
powershell -ExecutionPolicy Bypass -File "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\run-e2e-dynamo-tests.ps1"
```
Ensure build compiles with 0 errors and all 84 E2E tests pass 100% green.

## Output
Write your handoff report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_m2_1\handoff.md`
When finished, send a message back to orchestrator_4.
