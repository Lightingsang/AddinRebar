# Dispatch for Worker Milestone 1: McpShared Additive Integration

## 2026-09-21T17:33:00Z

- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m1
- Authoritative Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (read header ## 2026-09-21T17:20:33Z)
- Explorer 1 Report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_explorer_survey_1\report.md
- Repo Root: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

File Ownership:
You have exclusive write access to:
- `McpShared/HPRebar.Mcp.Contracts/PipeNaming.cs`
- `McpShared/HPRebar.Mcp.Contracts/JsonRpc/JsonRpcMethods.cs`
- `McpShared/HPRebar.Mcp.Contracts/HostScriptContracts.cs`
- `McpShared/HPRebar.Mcp.Contracts/Messages/ContextMessages.cs`
- `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs`
- `McpShared/HPRebar.McpBridge.Core/Scripting/AnalyzerProfile.cs`
- `McpShared/HPRebar.Mcp.Server.Core.Tests/` (new and existing test files)
- `McpShared/HPRebar.McpBridge.Core.Net48Tests/` (new and existing test files)

Objective:
Implement the additive integration for Tekla Structures in McpShared:
1. `PipeNaming.cs`: Define `TeklaHost = "tekla"`, map `TeklaHost => $"hptekla-mcp-{version}"`.
2. `JsonRpcMethods.cs`: Define `TeklaPrefix = "tekla."`.
3. `HostScriptContracts.cs`: Add `TeklaImports`, `TeklaGlobals`, and `TeklaHeavyMaxTimeoutSeconds = 600`.
4. `ContextMessages.cs`: Add `TeklaInfo? Tekla` to `ContextResult` and define `TeklaInfo` record.
5. `GuardProfile.cs`: Add `GuardProfile.Tekla` (per Explorer 1 specifications).
6. `AnalyzerProfile.cs`: Add `AnalyzerProfile.Tekla`.
7. Tests:
   - Add `TeklaProfileTests.cs` in `McpShared/HPRebar.Mcp.Server.Core.Tests/`
   - Update `HostNeutralityTests.cs` to add `"Tekla.Structures"` to the banned assembly check.
   - Update `ContextServiceTests.cs` to verify Tekla shape.
   - Update `ScriptCompilerNet48Tests.cs` in `HPRebar.McpBridge.Core.Net48Tests/` to verify Tekla profile on net48.
8. Build & Test:
   - Run `dotnet test HPRebar.Mcp.Server.Core.Tests` (must pass 100%).
   - Run `dotnet test HPRebar.McpBridge.Core.Net48Tests` (must pass 100%).
9. Write your completion report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m1\report.md` and handoff to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m1\handoff.md`.
