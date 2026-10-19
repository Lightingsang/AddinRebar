# BRIEFING — 2026-10-05T23:35:00Z

## Mission
Comprehensive technical survey of Revit MCP Named Pipe infrastructure, McpShared architecture, and Revit API 2026 BIM creation patterns for HPBim deliverable.

## 🔒 My Identity
- Archetype: explorer
- Roles: Teamwork explorer
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_mcp
- Original parent: f21aade0-effc-4aa5-a071-ae17d49dabe3
- Milestone: M0-Survey

## 🔒 Key Constraints
- Read-only investigation — do NOT implement production code
- Clean Code governance adherence: docs/clean-code/HP_CLEAN_CODE_CORE.md
- Strict reference direction: HPBim -> McpShared (no cross-MCP folder referencing)
- 100% C# .NET solution target

## Current Parent
- Conversation ID: f21aade0-effc-4aa5-a071-ae17d49dabe3
- Updated: 2026-10-05T23:35:00Z

## Investigation State
- **Explored paths**:
  - `McpShared/HPRebar.Mcp.Contracts/` (PipeNaming, JsonRpcEnvelope, JsonRpcMethods, BridgeErrorCode, ExecuteRequest, ExecuteResult)
  - `McpShared/HPRebar.McpBridge.Core/` (PipeListener, RequestDispatcher, ScriptGuard, GuardProfile, ScriptCompiler)
  - `McpShared/HPRebar.Mcp.Server.Core/` (RevitBridgeClient, NdjsonPipeTransport, HostProfile, ExecuteCodeService)
  - `HPRebar/HPRebar.McpBridge/` (Application, McpBridgeExternalEventHandler, ScriptGlobals, ScriptRunner, ResultSerializer, AutoDismissFailurePreprocessor)
  - `HPRebar/HPRebar.Mcp.Server/Registry/SeedLibrary/` (create_level, create_grid, create_line_based_element, create_point_based_element, create_surface_based_element, create_room, ai_element_filter, analyze_model_statistics)
  - `vn-autocad-skill-main/examples/nha_pho_5x18m/` (layout.py, model.py, draw_structure.py)
  - `vn-autocad-skill-main/vncad/` (standards.py, qc.py, qc_geom.py)
- **Key findings**:
  - Pipe `hprebar-mcp-r2026` uses NDJSON and JSON-RPC 2.0 with CurrentUserOnly Windows ACL.
  - Scripts execute on Revit main thread via `ExternalEvent` after pre-compilation on pipe thread.
  - Globals `doc`, `uidoc`, `app`, `uiapp`, `ct`, `log`, `progress`, `args` exposed.
  - `AutoDismissFailurePreprocessor` deletes warnings, preventing modal lockups.
  - `dryRun: true` commits inner transaction and rolls back outer `TransactionGroup`, leaving Document 100% clean.
  - Dynamic type duplication patterns for System Families (CompoundStructure) and Component Families (Type Parameter b, h, Width, Height) verified.
  - BIM stage order determined: Levels -> Grids -> Columns/Beams/Floors -> Walls -> Doors/Windows -> Rooms -> MEP -> Verification.
- **Unexplored areas**: None. Survey is complete.

## Key Decisions Made
- Recommended HPBim structure: HPBim.slnx (HPBim.Core, HPBim.Client, HPBim.Cli, HPBim.Core.Tests, HPBim.Client.Tests).
- Client can directly connect to Named Pipe `hprebar-mcp-r2026` using McpShared NdjsonPipeTransport without requiring intermediate CLI server.

## Artifact Index
- handoff.md — Comprehensive technical survey report
- progress.md — Liveness heartbeat
- DISPATCH.md — Received dispatch log
