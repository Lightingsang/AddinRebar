# BRIEFING — 2026-09-22T00:27:00Z

## Mission
Investigate McpShared architecture, existing host patterns (HPNavis net48 in-process, HPAutoCad, HPRobot, HPEtabs), identify all host-specific contracts/files, verify test suites baseline, and provide precise additive design/recommendations for Tekla Structures 2025 integration without regressions.

## 🔒 My Identity
- Archetype: explorer
- Roles: Teamwork preview explorer survey 1
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_explorer_survey_1
- Original parent: 5d7560ee-5142-428f-a172-e73cf7738ac1
- Milestone: Survey & Architecture Analysis for Tekla MCP in McpShared

## 🔒 Key Constraints
- Read-only investigation — do NOT implement changes in source code (only write reports/handoffs in own folder).
- Preserve existing 9 hosts in McpShared without breaking any behavior or test.
- Maintain strict host isolation (no cross-referencing between host projects).

## Current Parent
- Conversation ID: 5d7560ee-5142-428f-a172-e73cf7738ac1
- Updated: 2026-09-22T00:25:00Z

## Investigation State
- **Explored paths**:
  - `McpShared/HPRebar.Mcp.Contracts/` (`PipeNaming.cs`, `JsonRpcMethods.cs`, `HostScriptContracts.cs`, `Messages/ContextMessages.cs`, `BridgeJson.cs`)
  - `McpShared/HPRebar.McpBridge.Core/` (`Scripting/GuardProfile.cs`, `AnalyzerProfile.cs`, `ScriptGuard.cs`, `Pipe/RequestDispatcher.cs`, `Host/McpBridgeHost.cs`, `MainThreadQueue.cs`, `Model/BridgeSettingsStore.cs`)
  - `McpShared/HPRebar.Mcp.Server.Core/` (`Bootstrap/McpServerHost.cs`, `Hosts/IHostProfile.cs`, `Hosts/HostProfile.cs`, `Models/BridgeOptions.cs`, `Services/ExecuteCodeService.cs`, `Services/ContextService.cs`, `Registry/ToolValidator.cs`, `DynamicToolRegistrar.cs`)
  - `McpShared/HPRebar.Mcp.Server.Core.Tests/` and `McpShared/HPRebar.McpBridge.Core.Net48Tests/`
  - `HPNavis/`, `HPRobot/`, `HPEtabs/`, `HPAutoCad/` host integration patterns
- **Key findings**:
  - McpShared is 100% decoupled from host APIs via `IHostProfile`, `GuardProfile`, `AnalyzerProfile`, `HostScriptContracts`.
  - Baseline tests pass 100%: 613/613 net10 and 72/72 net48.
  - Adding Tekla Structures 2025 (`tekla`, `hptekla-mcp-2025`, `tekla.`) requires edits to 6 files in McpShared, all 100% additive.
  - In-process plugin on .NET Framework 4.8 directly mirrors `HPNavis` (uses `net48` assets of McpShared, `PluginAssemblyResolver`, `PipeSecurity`).
- **Unexplored areas**: None within the survey scope.

## Key Decisions Made
- Confirmed `PipeNaming.TeklaHost = "tekla"` and pipe `hptekla-mcp-{version}`.
- Confirmed method prefix `tekla.` seamlessly handled by suffix-based `RequestDispatcher`.
- Recommended bridge-owned commit model (`model.CommitChanges()` placed on denied members list) to enforce `dryRun` safety.
- Documented complete test addition strategy in `Server.Core.Tests` and `Bridge.Core.Net48Tests`.

## Artifact Index
- `.agents/teamwork_preview_explorer_survey_1/DISPATCH.md` — Initial dispatch instructions
- `.agents/teamwork_preview_explorer_survey_1/BRIEFING.md` — Agent briefing and persistent working memory
- `.agents/teamwork_preview_explorer_survey_1/progress.md` — Liveness heartbeat
- `.agents/teamwork_preview_explorer_survey_1/report.md` — Full technical survey report
- `.agents/teamwork_preview_explorer_survey_1/handoff.md` — Self-contained handoff
