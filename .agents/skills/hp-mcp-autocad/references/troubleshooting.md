# Troubleshooting — symptom → cause → what to do

| Symptom | Cause | Do |
|---|---|---|
| Tool list is empty / server missing in Claude Code | `.mcp.json` has no `hprebar-autocad` entry, or the exe path is wrong | user adds the entry (exe `HPAutoCad/output/HPAutoCad.Mcp.Server/HPAutoCad.Mcp.Server.exe`, env `HPAUTOCAD_MCP_Bridge__HostVersion=2026`); never commit `.mcp.json` |
| Only 24 tools (no `query_entities`, no `aec_*`) | an old published exe (seeds are embedded in the exe) | republish: `dotnet publish HPAutoCad/HPAutoCad.Mcp.Server -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -p:IncludeNativeLibrariesForSelfExtract=true -o HPAutoCad/output/HPAutoCad.Mcp.Server`; the exe is locked while any Claude Code session runs it — rename the running exe (NTFS allows it), publish into the path, restart the sessions |
| Every call: "bridge not connected" / pipe `hpautocad-mcp-2026` missing | AutoCAD closed, bundle not loaded, or SECURELOAD blocked it | start AutoCAD 2026; check `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.McpBridge.bundle\` exists; answer *Always Load* once; ribbon **HPAutoCad ▸ MCP ▸ MCP Bridge** shows the window (`HPMCPBRIDGE` command too) |
| `-32001` "Allow AI code execution" | opt-in off (resets on every AutoCAD start, never persisted) | user ticks the checkbox in the bridge window; no MCP way to turn it on |
| `-32002` busy after ~8 s | AutoCAD in a command, a modal dialog, or a previous script still running | user presses ESC / closes the dialog; `cancel_execution` stops a cooperative script only |
| `-32003` no drawing | no document open (start tab) | open a drawing |
| Second AutoCAD instance says pipe in use | one bridge per pipe per version | the first instance keeps serving; close the second or use it without MCP |
| `INVALID_HANDLE` | typo, or an entity of another drawing | handles come from `query_entities` of *this* drawing |
| `ERASED` | the entity was erased (also after a `U`) | query again |
| `NOT_AN_ENTITY` | the handle is a layer / block definition / dictionary | those are not edited by entity tools |
| `UNSUPPORTED_ENTITY` "inside block definition" | the entity lives in a block definition | edit the definition through `manage_blocks_attributes` or a script on the `BlockTableRecord` |
| `LAYER_LOCKED` / `LAYER_FROZEN` | target layer state | user unlocks/thaws; structured refusal, nothing written (atomic) |
| `NOT_CLOSED` | hatch boundary not closed | `detect_geometry_issues {issueTypes: ["open_polyline", "endpoint_gap"]}` |
| `ArgumentException: … filter.space all …` | rooms / MEP / write tools need one space | pass `filter.space: "model"` (or a layout name), or `space` on the write tool |
| `truncated: true` with few items | page cap (`limit.maximum` in the schema) or `maxCandidates` reached | page with `offset`; narrow `filter` (layers/types) before raising `maxCandidates` |
| `warnings: ["… scan stopped at maxCandidates …"]` | big drawing | narrow the filter; the AEC tools classify only what the filter selects |
| Classification returns `Unknown` for obvious walls/pipes | layer names outside the default rule set (AIA/NCS + English + Vietnamese stems) | `classify_aec_entities {includeUnknown: true}` shows evidence; a project rule set at `%AppData%\HPAutoCad\McpServer\rules\aec-classification.json` (`ruleSet: "user"`) — copy the embedded default from `HPAutoCad.Aec/Rules/aec-classification.default.json` |
| `arch_detect_rooms` finds 0 rooms | walls do not close (gaps > `tolerance.roomGap` 25), doorways wider than `detection.maxOpeningMm` 2500, or rooms drawn on unexpected layers | `arch_room_boundary_check` lists the open ends; raise `roomGap` / `maxGapMm`; add the layers to the filter |
| `aec_clash_check` lists nothing but summary says `contacts: 40` | contacts (joints, tees, doors in walls) are info by design | `minSeverity: "info"` to list them; hard clashes are MEP interpenetration only |
| `commit_change_set` "is committed; only a pending set commits" | already committed (maybe by a dryRun the engine has not seen yet) | `get_change_summary` first (it verifies); `rollback_change_set` or `keep: true` to finish the set |
| `rollback_change_set` `success:false` with `LAYER_LOCKED` | a layer was locked after the commit | unlock, roll back again — the set stays committed with its snapshots |
| Change set disappeared | the drawing was closed (store is per drawing, in-process) or 20 live sets were exceeded (the oldest committed one is closed) | begin again; `get_change_summary` shows `closed` sets |
| `run_tool` refuses a published tool: "quarantined" | ≥ 5 runs with > 40 % failures (not argument errors) | fix and `propose_tool {newVersion: true}` → approve, or `manage_tool {op: "restore"}` |
| Script: `COMPILE` diagnostic | C# error; the wrapper compiles with the bridge's usings only | read line/column; add nothing that needs `System.IO` etc. |
| Script: `GUARD` diagnostic | denied member/namespace (`ed.GetPoint`, `Commit`, `System.IO`, `#r`) | see `script-contract.md`; use a seed or restructure |
| Script returns `rolledBack: true` without `dryRun` | exception after writes, or timeout | the run failed as a whole; read `message`; do not re-run blindly |
| Harness `run-aec-*-live.ps1` fails to launch | AutoCAD already running (the harness starts its own) | close AutoCAD; the harness kills only what it started |
| Bridge log: no `MCP scripting self-check OK … aec tolerance 0.5` | `HPAutoCad.Aec.dll` missing beside the bridge (stale bundle) | `dotnet build HPAutoCad/HPAutoCad.slnx -c Debug` with AutoCAD closed redeploys the bundle |

Logs: bridge `%LocalAppData%\HPAutoCad\McpBridge\logs\`, audit `%AppData%\HPAutoCad\McpBridge\audit\`, registry `%AppData%\HPAutoCad\McpServer\` (`registry.db`, `tools-library\<Category>\<name>\`).
