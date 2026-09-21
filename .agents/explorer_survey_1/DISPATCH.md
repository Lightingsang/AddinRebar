# Task Assignment: Survey Explorer 1 — McpShared Architecture & Extension

## Identity
- Role: Explorer
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_1
- Parent Orchestrator: orchestrator_6

## Objective
Investigate `McpShared/` and map out all integration points required to support the new `HPExcel` host without breaking existing hosts or host neutrality.

## Context & Inputs
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (§ 2026-09-21T09:44:29Z)
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\AGENTS.md
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared/

## Scope of Investigation
1. Examine `McpShared/HPRebar.Mcp.Contracts/`:
   - `PipeNaming.cs` (how `PipeNaming.ExcelHost` should be registered, e.g., `"hpexcel-mcp-2026"`).
   - `JsonRpcMethods.cs` (method prefixes, e.g., `excel.execute`, `excel.context`).
   - Wire DTOs and contracts.
2. Examine `McpShared/HPRebar.McpBridge.Core/`:
   - `GuardProfile.cs` (what namespaces should be blocked/allowed for Excel, Roslyn script compiler setup).
   - `AnalyzerProfile.cs`, `HostScriptContracts.cs` (global variables like `excel`, default imports).
   - `IHostProfile.cs` (hints, timeouts, etc.).
3. Examine `McpShared/HPRebar.Mcp.Server.Core/`:
   - Server bootstrap, tool registry, context service shaping, execute service.
   - Look at how other hosts (HPPowerBi, HPEtabs, HPSap2000) are integrated.
4. Report exact required changes in `McpShared` to enable HPExcel.

## Output
Write detailed report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_1\handoff.md`.
Report when finished via `send_message`.

## 2026-09-21T09:47:00Z
You are Survey Explorer 1 for the HPExcel MCP Ecosystem.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_1

MANDATORY: Read the full user request first:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (§ 2026-09-21T09:44:29Z).
Also read your task assignment at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_1\DISPATCH.md

Your task is to investigate McpShared/ and determine all exact code additions and modifications required to add HPExcel support:
1. PipeNaming.cs: PipeNaming.ExcelHost ('hpexcel-mcp-2026')
2. JsonRpcMethods.cs: Excel prefix ('excel.')
3. McpBridge.Core: GuardProfile (forbidden/allowed namespaces, ScriptGuard checks), AnalyzerProfile, HostScriptContracts (Excel imports/globals), IHostProfile implementations.
4. Mcp.Server.Core: ContextService, ExecuteCodeService, tool registry integration for Excel.
5. Inspect how HPPowerBi, HPEtabs, and HPSap2000 are wired in McpShared to follow the exact same architectural pattern with 0 regressions.

Write your complete findings and recommendations to:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_1\handoff.md
Send a completion message back when done.

