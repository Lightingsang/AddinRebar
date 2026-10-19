# Dispatch Assignment — explorer_dynamo_survey_3

## Identity
- Name: explorer_dynamo_survey_3
- TypeName: teamwork_preview_explorer
- Role: Scope & Architecture Explorer (R3, R4: RV-04, RV-07, RV-08, Benchmarks & Docs)
- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_dynamo_survey_3\
- Parent: orchestrator_7 (Conversation ID: f448d195-c008-4b83-983a-30d0f3ab6338)

## Inputs
- Authoritative Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (section `## 2026-10-09T05:36:26Z` - MUST read first)
- Codex Review Report: `plans/261009-1118-hpdynamo-mcp-performance/reports/codex-review-20261009.md`
- Performance Plan: `plans/261009-1118-hpdynamo-mcp-performance/plan.md`
- Core Clean Code: `docs/clean-code/HP_CLEAN_CODE_CORE.md`
- HPDynamo Codebase: `HPDynamo/` (`HPDynamo.McpBridge`, `HPDynamo.Mcp.Server`), `McpShared/`
- Skill: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\skills\hp-mcp-dynamo\SKILL.md`

## Objective
Investigate and report on R3 & R4 (RV-04, RV-07, RV-08, Benchmark, Docs):
1. **RV-04**: Request-scoped cancellation via `ICorrelatedCancelExecutor`.
   - Where cancellation is handled in `McpShared/HPRebar.McpBridge.Core` (`RequestDispatcher`, `IBridgeExecutor`).
   - How to introduce `ICorrelatedCancelExecutor` preserving 100% binary/source backward compatibility for all other 10 hosts (including .NET Framework 4.8 hosts).
   - How `DynamoBridgeCoordinator` currently handles cancel vs how it should correlate `requestId`.
2. **RV-07 & RV-08**: ProtoCore MirrorData & Dictionary serialization in `get_node_output_values/code.cs`.
   - How `MirrorData.IsCollection` and `MirrorData.GetElements()` should be structured when `MirrorData.Data == null`.
   - How `DesignScript.Builtin.Dictionary` is currently serialized (or fails) and how to extract `.Keys` and `.ValueAtKey()`.
   - Breadth budget, depth budget, and cooperative cancellation token checking during data traversal.
3. **R4**: Benchmark harness, documentation & Skill updates.
   - Investigate test model copy isolation requirements, PowerShell benchmark harness, raw data format in `reports/baseline-vs-upgraded.json`.
   - Skill `hp-mcp-dynamo/SKILL.md` update requirements (tool count 25: 4 core + 8 registry + 13 seeds, documenting `batch_edit_graph`).
4. Output a comprehensive report to `report.md` in your working directory. Include file paths, line numbers, concrete findings, and actionable recommendations. Provide `handoff.md` and message parent when complete.
