# Dispatch: Explorer M2.1 (HPDynamo Solution & Build Scaffolding)

## Mission
Analyze exact setup for `HPDynamo/HPDynamo.slnx`, `Directory.Build.props`, and `global.json`:
- Reference `McpShared/` projects (`HPRebar.Mcp.Contracts`, `HPRebar.Mcp.Server.Core`).
- Configure assembly resolution for Dynamo 3.7 / Revit 2026 assemblies (`C:\Program Files\Autodesk\Revit 2026\AddIns\DynamoForRevit\`).
- Ensure .NET 10.0 target for `HPDynamo.Mcp.Server` and .NET 8.0-windows7.0 target for `HPDynamo.McpBridge`.
- Ensure clean compilation without circular dependencies.

## References
- `ORIGINAL_REQUEST.md` (mandatory read)
- `.agents/teamwork/orchestrator_4/PROJECT.md`
- `TEST_READY.md`

## Output
Write your report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m2_1\handoff.md`
Then send a message back to orchestrator_4.


## 2026-10-08T08:58:40Z
You are teamwork_preview_explorer_m2_1.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m2_1\
Read your dispatch instructions in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m2_1\DISPATCH.md
Read the authoritative request in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Read the project specifications in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_4\PROJECT.md

Investigate and formulate the exact setup for HPDynamo/HPDynamo.slnx, Directory.Build.props, and global.json:
1. XML .slnx solution structure referencing HPDynamo.Mcp.Server, HPDynamo.McpBridge, HPDynamo.Mcp.Server.Tests, HPDynamo.McpBridge.Tests, and HPDynamo.E2E.Tests.
2. Directory.Build.props configuration pointing to McpShared projects and Revit 2026 Dynamo paths (C:\Program Files\Autodesk\Revit 2026\AddIns\DynamoForRevit\).
3. Verify that dotnet build can resolve all dependencies cleanly.
Provide line-by-line implementation guidance in g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m2_1\handoff.md and send a message back to orchestrator_4.
