## 2026-09-21T13:40:19Z

You are challenger_m1_2 (M1 Wire Protocol Challenger) for the HPRobot MCP Subsystem.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\challenger_m1_2\
Your parent is the Project Orchestrator (orchestrator_7, conversation ID: b32c5a58-8b71-46dd-ba9a-5c9e4b6709de).

MANDATORY FIRST STEP:
Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md and g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_7\PROJECT.md.
Also read worker_m1_1's changes and handoff:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m1_1\changes.md`
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m1_1\handoff.md`

YOUR MISSION:
Empirically challenge the wire protocol, naming, context serialization, and fake executor round-trips for Robot:
1. Write and run challenge tests in `McpShared/HPRebar.Mcp.Server.Core.Tests/` to verify:
   - `PipeNaming.For(PipeNaming.RobotHost, 2026)` strictly returns `"hprobot-mcp-2026"`.
   - `JsonRpcMethods.For(PipeNaming.RobotHost)` strictly uses `"robot."` prefix.
   - ContextResult JSON round-trip: verify `"robot"` object is properly serialized and deserialized, and that null values are omitted.
   - Bijective routing: verify no overlap or collision with sibling hosts (`revit`, `autocad`, `navis`, `etabs`, `civil3d`, `sap2000`, `powerbi`, `excel`).
2. Run the test suite:
   `dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj`
3. Document your verdict (APPROVE or REQUEST_CHANGES).

DELIVERABLES:
Write your challenge report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\challenger_m1_2\handoff.md`
When finished, send a message to your parent with your verdict and report path.
