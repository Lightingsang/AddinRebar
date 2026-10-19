# Dispatch: Explorer M2.2 (HPDynamo.Mcp.Server Core Tools & Bootstrap)

## Mission
Analyze exact implementation for `HPDynamo.Mcp.Server`:
- `Program.cs`: bootstrap `McpServerHost.CreateBuilder(args, HostProfile.Dynamo)`.
- `DynamoHostProfile.cs`: server profile with `.WithHostAssembly(typeof(Program).Assembly)`.
- Core tools:
  - `execute_dynamo_code` (transaction: auto/manual/none, dryRun, label, timeoutSeconds).
  - `execute_designscript` (executes DesignScript code, returns output values).
  - `get_dynamo_context` (reads workspace, nodes, wires, run type).
  - `cancel_execution` (wired from engine).
- Clean Code rules: classes <= 300 lines, methods <= 50 lines.

## References
- `ORIGINAL_REQUEST.md` (mandatory read)
- `.agents/teamwork/orchestrator_4/PROJECT.md`
- `.agents/teamwork/teamwork_preview_explorer_survey_3/handoff.md`

## Output
Write your report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m2_2\handoff.md`
Then send a message back to orchestrator_4.

## 2026-10-08T08:58:40Z
You are teamwork_preview_explorer_m2_2.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m2_2\
Read your dispatch instructions in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m2_2\DISPATCH.md
Read the authoritative request in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Read the project specifications in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_4\PROJECT.md

Investigate and formulate the exact implementation for HPDynamo.Mcp.Server:
1. Program.cs bootstrapping McpServerHost.CreateBuilder(args, HostProfile.Dynamo).
2. DynamoHostProfile.cs configuring CoreToolNames and .WithHostAssembly(typeof(Program).Assembly).
3. Core tools implementation:
   - ExecuteDynamoCodeTool (execute_dynamo_code)
   - ExecuteDesignScriptTool (execute_designscript)
   - DynamoContextTool (get_dynamo_context)
   - cancel_execution (from engine)
Ensure classes <= 300 lines, methods <= 50 lines.
Provide line-by-line implementation guidance in g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m2_2\handoff.md and send a message back to orchestrator_4.
