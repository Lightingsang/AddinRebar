# Dispatch for Reviewer 1 - Milestone 1

## 2026-09-21T17:38:00Z

- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_reviewer_m1_1
- Authoritative Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (header ## 2026-09-21T17:20:33Z)
- Worker Report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m1\report.md
- Repo Root: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar

Objective:
Independently review Milestone 1 implementation in `McpShared/`:
1. Check code changes in `McpShared/HPRebar.Mcp.Contracts/PipeNaming.cs`, `JsonRpcMethods.cs`, `HostScriptContracts.cs`, `Messages/ContextMessages.cs`, `HPRebar.McpBridge.Core/Scripting/GuardProfile.cs`, `AnalyzerProfile.cs`.
2. Verify that changes are 100% additive, clean, conformant to repository code standards, and introduce zero regressions to the 9 existing hosts.
3. Run `dotnet test HPRebar.Mcp.Server.Core.Tests` and `dotnet test HPRebar.McpBridge.Core.Net48Tests`. Confirm test results.
4. Check Host Neutrality: confirm that shared assemblies have zero references to `Tekla.Structures`.
5. Write your review report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_reviewer_m1_1\report.md` and handoff with explicit verdict (APPROVE or REQUEST_CHANGES) to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_reviewer_m1_1\handoff.md`.
