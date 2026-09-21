# Dispatch for Survey Explorer 1: Architecture & McpShared Patterns

## 2026-09-21T17:22:00Z

- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_explorer_survey_1
- Authoritative Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (read header ## 2026-09-21T17:20:33Z)
- Repo Root: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar

Objective:
Investigate McpShared architecture and how existing hosts (especially HPNavis which is net48 in-process, HPAutoCad, HPRobot, HPEtabs) are integrated:
1. Identify all files in McpShared (HPRebar.Mcp.Contracts, HPRebar.McpBridge.Core, HPRebar.Mcp.Server.Core) that define host profiles, pipe naming, guard profiles, script contracts, context results, and analyzers.
2. Determine the exact additive changes required for Tekla Structures 2025.0:
   - PipeNaming.TeklaHost = "tekla", pipe hptekla-mcp-{version}, RPC method prefix "tekla"
   - HostScriptContracts.TeklaImports and TeklaGlobals
   - GuardProfile.Tekla and AnalyzerProfile.Tekla
   - ContextResult.Tekla and TeklaInfo
   - HostProfile.Tekla (DefaultVersion = 2025, ValidVersions = [2025])
3. Inspect McpShared test suites (HPRebar.Mcp.Server.Core.Tests net10, HPRebar.McpBridge.Core.Net48Tests net48) and verify current pass state and test coverage patterns.
4. Output report to g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_explorer_survey_1\report.md and handoff.md.
