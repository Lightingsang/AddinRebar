---
title: "Phase 7 — Tool Registry core (DB, library, manager, dynamic tools)"
status: built+tested (2026-09-12) — verified over stdio without Revit; in-Revit run of seeds → phase 9
priority: P1
effort: 20h
depends_on: [phase-06]
created: 2026-09-12
---

# Phase 7 — Tool Registry core

## Context
- Design of record: [research/ai-bim-self-extending-tool-registry-design.md](research/ai-bim-self-extending-tool-registry-design.md) (§2 principles, §4 data model, §7 memory) — written for the reference repo, applied here to `HPRebar.Mcp.Server`.
- SDK facts verified 2026-09-12 in `ModelContextProtocol.Core` 2.2.0 XML docs: `McpServerOptions.ToolCollection` is a `McpServerPrimitiveCollection<McpServerTool>` with `Add/TryAdd/Remove/Clear`, a `Changed` event and `DeferChangedEvents()`; `McpServerTool.Create(AIFunction, McpServerToolCreateOptions)` builds a tool from any `AIFunction`, whose `JsonSchema` is overridable → arbitrary JSON Schema per generated tool; `ToolsCapability.ListChanged` advertises `notifications/tools/list_changed`.
- Seed content: the 21 typed tools of phase 6 become the first library entries (`author: hprebar`, `status: published`).

## Overview
Adds the memory layer to the server: a **Tools Library** on disk (source of truth, git-friendly), a **Tool Database** (SQLite index + run history), a **Tool Manager** (load, search, run, stability) and a **Dynamic Tool Registrar** that exposes every `published` tool as a real MCP tool at runtime. No bridge change beyond phase 6.

## Key insights
- A generated tool is **data**: `{tool.json, code.cs, examples.json}`. Running it = `revit.execute(code, args)` — the phase-6 path. Nothing is loaded into Revit per tool.
- One `McpServer` per stdio session; tools are added to `ServerOptions.ToolCollection` after startup and on library changes — the SDK sends `list_changed` itself.
- Files are edited by humans (approve, fix code) and by the server (publish, stats never — stats live in DB). A `FileSystemWatcher` on the library folder reconciles DB ← files and re-registers tools without restart.
- Several server processes (several Claude sessions) may share one library + DB → SQLite WAL, no in-memory-only state.

## Requirements
Functional
- Library root default `%AppData%\HPRebar\McpServer\tools-library\` (override `Registry:LibraryPath`); DB `%AppData%\HPRebar\McpServer\registry.db` (override `Registry:DbPath`).
- `search_tools(query, category?, limit=5, includeUnpublished=false)` → ranked list with `inputSchema`, `stability`, `runs`, `status`.
- `get_tool(name, includeCode=true)` → full record + examples + last 10 runs.
- `run_tool(name, args, dryRun=false, allowUnpublished=false)` → executes stored code with `args`; result shaped like `execute_revit_code`.
- Every `published` tool also callable directly by name (dynamic MCP tool) with `dryRun` added to its schema when `transaction != none`.
- Run history: every run (dynamic tool, `run_tool`, `execute_revit_code`) recorded in `runs`.
- Seed: 21 phase-6 tools installed into an empty library on first start; never overwrite a user-modified entry (checksum compare).
Non-functional
- Search offline (FTS5; fallback `LIKE` + in-memory BM25 if FTS5 missing in the bundled e_sqlite3).
- Startup cost < 300 ms for 200 tools.
- Single-file publish keeps working (`IncludeNativeLibrariesForSelfExtract=true` for e_sqlite3).

## Architecture
```
HPRebar.Mcp.Server/
├── Registry/
│   ├── Model/ToolRecord.cs           name,title,description,category,tags,status,version,inputSchema(JsonElement),
│   │                                 transaction,timeoutSeconds,destructive,revitVersions,author,createdFromRunId,approvedBy,timestamps
│   ├── Model/ToolStatus.cs           Draft | Tested | PendingApproval | Published | Quarantined | Deprecated
│   ├── Model/ToolExample.cs          title,args,expected?,verifiedRunId?
│   ├── Model/RunRecord.cs            id,toolName?,version?,kind(adhoc|tool|test),argsSha,codeSha,dryRun,success,durationMs,error,revitVersion,docTitle,ts
│   ├── Model/RegistryOptions.cs      LibraryPath, DbPath, PublishPolicy(manual|auto), SearchTopK, QuarantineMinRuns, QuarantineMaxFailureRate, RunWindow
│   ├── ToolLibraryStore.cs           read/write <Category>/<name>/{tool.json,code.cs,examples.json}; checksum; FileSystemWatcher → Changed event
│   ├── ToolRegistryDb.cs             Microsoft.Data.Sqlite; tables tools, tool_versions, runs, tools_fts; WAL; migrations
│   ├── StabilityScorer.cs            successRate over RunWindow, stability = rate × min(1, runs/10); quarantine rule
│   ├── ToolManager.cs                LoadAll (files→db), Search, Get, Run (via IRevitBridgeClient), RecordRun, Reconcile(on watcher)
│   ├── DynamicToolRegistrar.cs       RegistryToolFunction : AIFunction (JsonSchema from record + dryRun) → McpServerTool.Create → ToolCollection add/remove
│   └── SeedLibrary/<Category>/<name>/{tool.json,code.cs,examples.json}   EmbeddedResource (phase-6 tools)
├── Tools/Registry/ToolRegistryQueryTools.cs     search_tools, get_tool
├── Tools/Registry/RunToolTool.cs                run_tool
├── Services/RegistryStartup.cs                  IHostedService: ensure dirs, seed, LoadAll, register published, start watcher
└── Program.cs / appsettings.json                Registry section, DI, ToolsCapability.ListChanged = true
```
Ranking: `bm25 × (0.5 + 0.5·stability) × statusWeight` (published 1.0 · tested 0.6 · draft 0.2 · pending 0.4 · quarantined/deprecated 0). Category filter exact; Revit-version filter from `get_revit_context` when connected.

## Related code files
Modify: `Program.cs`, `appsettings.json`, `HPRebar.Mcp.Server.csproj` (Microsoft.Data.Sqlite, embedded seed, `IncludeNativeLibrariesForSelfExtract`), `Models/BridgeOptions.cs` (unchanged), `Tools/ExecuteRevitCodeTool.cs` (record adhoc run), `build/Modules/PublishServerModule.cs` (native libs property).
Create: everything under `Registry/`, `Tools/Registry/`, `Services/RegistryStartup.cs`, tests `HPRebar.Mcp.Server.Tests/Registry/*`.

## Implementation steps
1. `RegistryOptions` + config binding + validation (paths absolute after expansion).
2. `ToolLibraryStore`: parse/write records, checksum (`sha256(tool.json+code.cs+examples.json)`), watcher with 300 ms debounce.
3. `ToolRegistryDb`: schema + migrations; `UpsertTool`, `Search`, `InsertRun`, `RunStats(name)`; detect FTS5 (`pragma compile_options`) → fallback.
4. `StabilityScorer` (pure) + tests.
5. `ToolManager`: `LoadAllAsync` (files → db, drop db rows whose folder vanished), `SearchAsync`, `GetAsync`, `RunAsync(name,args,dryRun)` → `ExecuteRequest(code, args, transaction, dryRun, timeout, label:name)` → `RecordRun`.
6. `DynamicToolRegistrar`: `RegistryToolFunction` (Name, Description, JsonSchema = record.inputSchema + optional `dryRun`; `InvokeCoreAsync` → `ToolManager.RunAsync`; returns `CallToolResult` via `ResultFormatter`); `SyncAsync()` diff published set vs registered set inside `DeferChangedEvents()`.
7. `RegistryStartup` hosted service; seed install; `ToolsCapability.ListChanged = true`.
8. Meta tools `search_tools`, `get_tool`, `run_tool` (+ `[McpServerTool(ReadOnly)]` flags).
9. Tests: db round-trip (temp file), search ranking, seed install idempotent, registrar sync adds/removes, `run_tool` over pipe with `FakeRevitExecutor` (args echoed back).
10. Publish exe; `mcp_call.py tools/list` shows 4 + 3 + 21 = 28 tools.

## Todo
- [x] 1 options · [x] 2 library store · [x] 3 db (FTS5 confirmed in e_sqlite3) · [x] 4 scorer · [x] 5 manager (+ quarantine hook) · [x] 6 registrar · [x] 7 startup/seed · [x] 8 meta tools · [x] 9 tests (142 xUnit) · [x] 10 publish + tools/list = 28, list_changed on disk edit verified

## Success criteria
- `tools/list` = 28 with the 21 seed tools callable by name; `search_tools "tạo lưới trục"` returns `create_grid` first.
- Editing a `tool.json` on disk (status → deprecated) removes the tool from `tools/list` within 1 s without restart.
- `runs` table grows by one row per call; `stability` visible in `search_tools`.

## Risks
| Risk | Mitigation |
|---|---|
| e_sqlite3 without FTS5 | `pragma compile_options` check; LIKE + in-memory BM25 fallback (≤ 500 tools fine) |
| Single-file publish loses native dll | `IncludeNativeLibrariesForSelfExtract=true`; verified by running published exe |
| ToolCollection mutated from watcher thread while a call is in flight | collection is thread-safe per SDK; wrap sync in `DeferChangedEvents` |
| Two servers write same DB | WAL + busy_timeout 5 s; files written atomically (temp + move) |

## Security
Stored code runs through the same guard/opt-in/timeout/audit as ad-hoc code. Dynamic tools are `Destructive` unless `transaction == none`. Library folder is per-user; a shared folder is a deliberate user choice.

## Next steps
Phase 8 adds proposing/testing/publishing (write side) on top of this read/run side.
