---
title: "Phase 6 — Typed tools ported from mcp-servers-for-revit"
status: built+tested (2026-09-12) — live verify in phase 9
priority: P2
effort: 14h
depends_on: [phase-04, phase-05]
created: 2026-09-12
---

# Phase 6 — Typed tools ported from mcp-servers-for-revit

## Context
- Source: https://github.com/mcp-servers-for-revit/mcp-servers-for-revit (MIT, 327★, pushed 2026-04-05). Tool contracts read from `server/src/tools/*.ts` (zod schemas), semantics from `commandset/Commands/**` + `commandset/Services/**`.
- Snapshot of every tool schema: session scratchpad `ref-tools-summary.md` (30 files → 26 real tools).
- Our bridge: [architecture.md](architecture.md), [adr-03](adr/adr-03-roslyn-in-process-execution.md), [adr-04](adr/adr-04-execute-code-security-model.md).

## Overview
User asked (2026-09-12) to add the reference repo's tools to the bridge, then (same day) to grow the bridge into a self-extending AI BIM system (phases 7-9). This phase therefore delivers two things: (1) the **`args` data slot** every tool needs, plus a Revit-free `revit.analyze` method; (2) the 21 reference tools as **seed library records** (`tool.json` + `code.cs` + `examples.json`, embedded in the server), not as 21 hand-written C# tool classes. Phase 7's registry exposes them as MCP tools. No new pipe methods that touch Revit, no command registry in Revit, no copy of the reference commandset.

**Revised 2026-09-12:** `Tools/Typed/*.cs`, `ScriptTemplateCatalog` and `TypedToolRunner` from the first draft are dropped; their role is taken by `Registry/SeedLibrary/**` + `ToolManager.RunAsync` (phase 7).

## Key insights
- Reference: TS server → TCP 8080 → C# plugin with a command registry + `command.json`; every tool is a separate `ExternalEventCommandBase` in a commandset DLL. Copying that means 21 more Revit classes, a redeploy per tool, and a second execution path outside guard/audit/dry-run.
- Ours: one execution path already has guard, cache, transaction policy, dry-run, timeout, audit, Undo group. A template script is just a script whose text never changes → compiled **once per Revit session** (hash cache) as long as parameters travel as data, not as code.
- Gap: `ScriptGlobals` has no data slot. Adding `args` (typed accessor over a `JsonElement`) is the only bridge change and also benefits `execute_revit_code` (AI can pass data separately from code).
- Reference units are mm; ours convert at the template boundary with `UnitUtils`.

## Requirements
Functional
- 21 tools with the reference names and parameter shapes (table below); output JSON in the same spirit (ids as numbers, mm units, `success/message` where the reference has them).
- Create/modify tools accept `dryRun` (bridge policy); read tools run with `transaction:"none"` so they work on read-only documents.
- `execute_revit_code` gains optional `args` (object) → visible in script as `args`.

Non-functional
- Bridge stays generic: the only bridge change is `ScriptArgs` + `ScriptGlobals.args` + `ExecuteRequest.Args`.
- Every template passes `ScriptGuard` and is < 32 KB (unit-tested).
- Opt-in still required for every tool (single security rule; relax later if wanted).

## Tool mapping
| Reference tool | Ours | Mode | Notes |
|---|---|---|---|
| get_current_view_info | same | none | view id/name/type/scale/level/detail level/discipline |
| get_current_view_elements | same | none | model/annotation category lists, includeHidden, limit |
| get_selected_elements | same | none | limit |
| get_available_family_types | same | none | categoryList, familyNameFilter, limit |
| ai_element_filter | same | none | category/type/symbol filters, includeTypes/instances, visibleInCurrentView, boundingBox, maxElements |
| analyze_model_statistics | same | none | counts by category / level; includeDetailedTypes |
| export_room_data | same | none | includeUnplaced / includeNotEnclosed |
| get_material_quantities | same | none | categoryFilters, selectedElementsOnly |
| create_level | same | auto | name/elevation + optional plan views |
| create_grid | same | auto | x/y counts, spacing, labels, naming style, extents |
| create_line_based_element | same | auto | OST_Walls / OST_StructuralFraming / MEP curves; typeId, p0/p1, thickness, height, baseLevel, baseOffset |
| create_point_based_element | same | auto | doors/windows/furniture; hostWallId auto-detect, rotation, facingFlipped |
| create_surface_based_element | same | auto | floors/ceilings/roofs from outer loop |
| create_room | same | auto | name/number/location/level/limits/department |
| create_structural_framing_system | same | auto | BeamSystem in rectangle; spacing, directionEdge, justify |
| create_dimensions | same | auto | between elements or auto references at two points |
| tag_all_rooms | same | auto | useLeader, tagTypeId, roomIds |
| tag_all_walls | same | auto | useLeader, tagTypeId |
| delete_element | same | auto | elementIds |
| operate_element | same | auto/none | Select, SelectionBox, SetColor, SetTransparency, Delete, Hide, TempHide, Isolate, Unhide, ResetIsolate, Highlight |
| color_elements | same | auto | categoryName, parameterName, useGradient, customColors |
| say_hello | — | — | modal TaskDialog blocks the API thread; `get_revit_context` covers the "is it alive" case |
| send_code_to_revit | — | — | = `execute_revit_code` |
| store_project_data / store_room_data / query_stored_data | — | — | SQLite inside the TS server, no Revit involvement |
| search_modules / use_module / register / modify_element | — | — | server infrastructure, not tools |

## Architecture
```
Claude Code ── tools/call create_level {data:[…]} ──▶ Mcp.Server
   dynamic tool (phase 7) ──▶ ToolManager.RunAsync("create_level", args)  ← code from Registry/SeedLibrary
                          ──▶ ExecuteRequest(code, args=JSON, transaction=auto, dryRun, label="create_level")
                          ──▶ RevitBridgeClient.SendAsync("revit.execute")            ← unchanged path
Bridge: guard → compile (cache hit after first call) → ExternalEvent → ScriptRunner
        ScriptGlobals.args = new ScriptArgs(request.Args)                     ← only bridge change
```
Server layout (new):
```
HPRebar.Mcp.Server/Registry/SeedLibrary/<Category>/<name>/
├── tool.json        name, title, description, category, tags, status=published, version=1, inputSchema, transaction, timeoutSeconds, author=hprebar
├── code.cs          script body; parameters read through `args`
└── examples.json    [{title, args, expected}]
```
Bridge Core (new, Revit-free): `Scripting/ScriptAnalyzer.cs` — Roslyn walker returning literals (value, kind, line, col, context) and `args.<Kind>("key")` keys; `RequestDispatcher` case `revit.analyze` = guard + `GetOrCompile` + analyzer on the pipe thread.
Bridge/Contracts (changed):
```
HPRebar.Mcp.Contracts/Messages/ExecuteRequest.cs   + JsonElement? Args
HPRebar.Mcp.Contracts/Messages/AnalyzeMessages.cs  AnalyzeRequest / AnalyzeResult (new)
HPRebar.Mcp.Contracts/JsonRpc/JsonRpcMethods.cs     + Analyze = "revit.analyze"
HPRebar.McpBridge.Core/Scripting/ScriptArgs.cs      typed accessor (Int/Double/Str/Bool/List/Obj/Has/Raw)
HPRebar.McpBridge/Model/ScriptGlobals.cs            + public readonly ScriptArgs args
HPRebar.McpBridge/Service/ScriptRunner.cs           passes request.Args
HPRebar.McpBridge/Application.cs                    + typeof(ScriptArgs).Assembly in references
```

## Implementation steps
1. Contracts: `ExecuteRequest.Args` (optional, last positional). Tests still compile.
2. Core: `ScriptArgs` (+ xUnit tests: missing key defaults, nested list/object, number coercion).
3. Bridge: `ScriptGlobals.args`, `ScriptRunner` wiring, `Application` references; description string of `execute_revit_code` mentions `args`.
4. Core: `ScriptAnalyzer` + `revit.analyze` in dispatcher/`IRevitExecutor` (+ xUnit tests: literals with positions, arg keys, guard violations surface).
5. Server: `execute_revit_code` optional `args` param (JsonElement) forwarded as `ExecuteRequest.Args`.
6. Seed records, group by group: Query (8) → Create (7) → Annotate (3: dimensions, tag rooms, tag walls) → Operate (3). Each `code.cs` written against the reference handler's semantics; each `examples.json` has ≥ 2 cases.
7. Tests: every seed `code.cs` passes `ScriptGuard`, < 32 KB, every `inputSchema` property is referenced via `args`; pipe round-trip with `args`.
7. Build `Debug.R26` (bridge, Revit closed) + publish server exe; verify each tool live on `NhaDanDung-3Tang-KetCau.rvt` (read tools) and a scratch document (create/operate tools, dryRun first).
8. Docs: CLAUDE.md § MCP bridge (tool surface), AGENTS.md regen, codebase-summary, architecture § tool layer, ADR-05 "typed tools are script templates", MIT attribution.

## Todo
- [x] 1 Contracts Args
- [x] 2 ScriptArgs + tests
- [x] 3 Bridge wiring
- [x] 4 ScriptAnalyzer + `revit.analyze`
- [x] 5 `execute_revit_code.args`
- [x] 6a Seed Query (8)
- [x] 6b Seed Create (7)
- [x] 6c Seed Annotate (3)
- [x] 6d Seed Operate (3)
- [x] 7 Tests — 119 xUnit incl. metadata compile of every seed against RevitAPI 2026 ref assemblies
- [ ] 8 Live verify moves to phase 9; docs/ADR-05 move to phase 9

## Success criteria
- 21 seed records load and validate; `revit.analyze` returns literals/arg keys over the pipe; `execute_revit_code` with `args` echoes them in the script. Live `tools/list` count and dryRun checks are phase 7/9 criteria.
- 0 new bridge pipe methods; `ScriptGuard` unchanged.

## Risks
| Risk | Mitigation |
|---|---|
| Template semantics drift from reference (e.g. `create_point_based_element` host detection) | Read reference handler before writing each template; note deviations in the tool description |
| 25 tools inflate the AI's tool list | Group descriptions short; `execute_revit_code` remains the escape hatch |
| Redeploy needs Revit closed | One restart; model already saved |
| Reference tools assume architectural templates (rooms, tags, ceilings) | Verify on a scratch architectural document, not only the RC model |

## Security
Unchanged model (ADR-04): templates go through guard, opt-in, timeout, audit. `args` is data, never concatenated into code. `delete_element` / `operate_element(Delete)` are `Destructive=true` with `dryRun` support.

## Next steps
Phase 7 candidates: relax opt-in for read-only typed tools; TUnit in-Revit tests for templates.
