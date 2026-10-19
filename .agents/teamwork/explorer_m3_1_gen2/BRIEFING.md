# BRIEFING — 2026-10-06T02:00:00Z

## Mission
Investigate Milestone 3 (Revit Client & Pipe Engine): inspect McpShared for Named Pipe contracts, design 8-stage BIM generators, dynamic type resolution, dryRun rollback, and class layout for HPBim.Client and HPBim.Client.Tests complying with clean code limits.

## 🔒 My Identity
- Archetype: teamwork_preview_explorer
- Roles: explorer, analyst
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_1_gen2
- Original parent: 18c3ad8c-1fc9-42cc-83e7-40ab9000587e
- Milestone: Milestone 3 (Revit Client & Pipe Engine)

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- Strict Clean Code rules: classes <= 300 lines, methods <= 50 lines
- Produce comprehensive design report in handoff.md and communicate via send_message
- Write only to own folder: .agents/teamwork/explorer_m3_1_gen2/

## Current Parent
- Conversation ID: 18c3ad8c-1fc9-42cc-83e7-40ab9000587e
- Updated: 2026-10-06T02:00:00Z

## Investigation State
- **Explored paths**:
  - McpShared/HPRebar.Mcp.Contracts (PipeNaming, BridgeJson, JsonRpcEnvelope, ExecuteRequest, ExecuteResult)
  - McpShared/HPRebar.McpBridge.Core (ScriptRunner, ScriptGuard, GuardProfile, ScriptArgs, ScriptCompiler)
  - McpShared/HPRebar.Mcp.Server.Core (NdjsonPipeTransport, RevitBridgeClient)
  - HPRebar SeedLibrary (create_level, create_grid, create_point_based_element, create_surface_based_element, create_room, create_line_based_element, create_structural_framing_system)
  - HPRebar RebarCurveFactory (Rebar.CreateFromCurves multi-version)
  - HPBim.Core Models & NhaPho5x18mFixture
- **Key findings**:
  - Pipe name: \\.\pipe\hprebar-mcp-r2026, NDJSON framing, JSON-RPC 2.0 with ExecuteRequest/ExecuteResult.
  - Script template + args design ensures Roslyn ScriptCache hits (~0ms compilation).
  - DryRun rollback handled natively by bridge TransactionGroup.RollBack() when dryRun=true.
  - For atomic dryRun across stages, a CompositeBimGenerator script runs all 8 stages inside one TransactionGroup.
  - Dynamic Type duplication (WallType 110/220, Column C220x220, symbol.Activate()) fully specified.
  - 100% clean class layout for HPBim.Client & HPBim.Client.Tests with classes <= 220 lines (limit 300).
- **Unexplored areas**: None for M3 survey. Ready for handoff report.

## Key Decisions Made
- Multi-target HPBim.Client to net8.0;net10.0 referencing only HPBim.Core and HPRebar.Mcp.Contracts (netstandard2.0;net48), keeping it lightweight without hauling SQLite or ModelContextProtocol.
- Provide both individual stage generators (for live staged execution with progress) and a composite generator (for atomic dryRun rollback).

## Artifact Index
- BRIEFING.md — working memory
- DISPATCH.md — task instructions
- progress.md — liveness heartbeat
- handoff.md — final handoff report
