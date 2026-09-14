# Phase 5 — Live verification in AutoCAD 2026 (2026-09-14)

Machine: dev (AutoCAD 2026 R25.1.74 started by the harness from `acad.dwt`, Inches; Revit 2026 open with `NhaDanDung-3Tang-KetCau.rvt` and the deployed Revit bridge, opt-in off). Client: `HPAutoCad/output/HPAutoCad.Mcp.Server/HPAutoCad.Mcp.Server.exe` over stdio, **one session** for A–D (`tools/list_changed` must reach a running server), `HPRebar/HPRebar.Mcp.Server/bin/Debug/net10.0/HPRebar.Mcp.Server.exe` for E (the published Revit exe is locked by this session's `hprebar-revit` MCP). Harness: `HPAutoCad/tools/harness/run-live-verify.ps1` → `live-verify.py` (+ `mcp-session.py`); logs `phase-05-live-verify-run{1,2}.log`, `phase-05-live-verify-isolation.log`, `phase-05-bridge-harness.log`.

**Result:** run 2 (after the two engine fixes below) **64 pass + 1 skip** (Revit opt-in) + isolation **4/4** + bridge harness regression **21/21**; run 3 after the review fixes, on an isolated registry root (`output/live-verify/registry`), 64 pass + 1 skip again (`phase-05-live-verify-run3.log`). No `.NET Runtime 1026` event, no AutoCAD left running.

## A — Execute matrix over stdio (18)
| Scenario | Result |
|---|---|
| opt-in off → refused | ✅ "Code execution is disabled. Ask the user to tick 'Allow AI code execution' … inside AutoCAD." |
| none read | ✅ `db.Filename`, runId |
| dryRun | ✅ `changed.added=1`, `rolledBack`, count unchanged |
| commit + **U** after a `REGEN` boundary | ✅ count 0→1→0, runId + hint |
| exception | ✅ rolled back, message kept |
| none + modify | ✅ refused, `transaction="none"` named |
| manual: `StartTransaction` guard · manual ≈ auto | ✅ guard diagnostic · log line "behaves like auto" |
| guard `ed.GetPoint` / `tr.Commit` / `SendStringToExecute` | ✅ 3 diagnostics |
| compile error | ✅ CS0103 |
| `cancel_execution` racing a spinning script | ✅ `{cancelled:true, wasRunning:true}`, rolled back, 1.6 s |
| timeout 5 s | ✅ `timedOut`, rolled back, 7.0 s |
| logs + progress + args + serializer | ✅ |
| no drawing (COM closed it) | ✅ context `openDocs=[]`, "No drawing is open in AutoCAD. Open one first." |
| busy (`LINE` via COM) → **ESC posted (PostMessage to the focused window) → retry** | ✅ refused after 8.1 s, retry returns 2 — the phase-2 "manual" item is now automated |
| audit | ✅ 471 lines in `audit-20260914.log` |

## B — Every seed (18)
`create_layer MCP-VERIFY` (lineweight 30 → `list_layers` reports 30) · block definition `MCP-BLOCK` with an attribute made through `execute_autocad_code` · `list_block_definitions` sees it (`hasAttributes`) · **`insert_block` real** (scale 2, 45°, `attributes {TAG: D01}` → `attributesSet=1`) and dryRun (`added=3`, rolled back) · `draw_polyline` (3 vertices, closed) and its `[x, y]` refusal · `draw_circle` (area 196 350 mm²) · `add_text` DBText and MText with `\P` · `add_linear_dimension` aligned 1 000 mm · `get_entities` layer filter (INSERT + LWPOLYLINE + TEXT), type filter + limit · `list_layouts` · `get_drawing_info` (counts, extents) · **`get_selected_entities` with a pickfirst set** (`(sssetfirst nil (ssget "_X"))` through COM → 7 selected). All ✅.

## C — MISS → memory → HIT (13)
| Step | Result |
|---|---|
| `search_tools "move text entities to another layer"` | miss (seeds only) |
| ad-hoc dryRun / real (`TXT-A` → `TXT-B` literals) | 2 moved, `modified=2`; runId 182 + hint |
| `get_run 182 analyze` | literals `TXT-A`, `TXT-B` listed |
| `prompts/get toolify_run` | 3 272 chars, names the run and AutoCAD |
| `propose_tool move_text_between_layers` (category `annotation`) | draft v1; tool.json `host: autocad`, `hostVersions: ["2026"]`, category `Annotation` |
| `test_tool` | 2/2 dryRun → tested |
| `publish_tool` | pending_approval; `_review/move_text_between_layers.md` has `**Host:** autocad 2026` and `HPAutoCad.Mcp.Server.exe registry approve …`, never the Revit exe |
| `run_tool` while pending | refused |
| `HPAutoCad.Mcp.Server.exe registry approve … --by "harness (CLI)"` | published |
| running server | tool in `tools/list` after **0.5 s**, one `notifications/tools/list_changed` |
| `search_tools` | HIT, ranked first |
| call by name dryRun / real | 2 moved; runId 186 |
| `get_tool` | approvedBy, createdFromRunId 182, runs counted |

## D — Fragile → quarantine → restore (7)
| Step | Result |
|---|---|
| propose/test/publish/approve `mcp_verify_count_block_refs` v1 (**unguarded** `bt[name]`) | published |
| run on `MCP-BLOCK` | references 1 |
| 5 × run on `NONEXISTENT` (`eKeyNotFound`) | quarantined at 4/5, removed from `tools/list` in < 1 ms, the 5th call answered "Unknown tool" |
| `run_tool` on the quarantined tool | refused |
| `manage_tool restore` → `propose_tool newVersion` (guarded, `ArgumentException`) → `test_tool` → `publish_tool` → CLI approve | v2 published, back in `tools/list` after 0.5 s |
| run on `MCP-BLOCK` | references 1 |
| 5 × run on `NONEXISTENT` (now `ArgumentException`) | **stays published** |

## E — Revit exe beside (5, 1 skip)
`HPRebar Revit MCP` 34 tools, Revit names only · `get_revit_context` 2026 / `NhaDanDung-3Tang-KetCau` while the AutoCAD pipe is up (`hprebar-mcp-r2026` + `hpautocad-mcp-2026` both listed) · `execute_revit_code` refused with the Revit opt-in message · `%AppData%\HPRebar\McpServer\tools-library` hash unchanged (`c3c249ba4bd18594`). **Skipped:** `analyze_model_statistics` + `execute_revit_code` read — the Revit opt-in is off and the harness must not touch the user's Revit session; tick the box in Revit and run `python HPAutoCad/tools/harness/live-verify.py --exe <autocad exe> --revit-exe <revit exe> --only e`.

## F — Isolation (4)
| Check | Result |
|---|---|
| second AutoCAD 2026 with the bundle while the first serves | status window after 19 s: "Pipe hpautocad-mcp-2026 is already in use — another **AutoCAD** 2026 instance is serving MCP."; logged in the (now shared) `mcpbridge-*.log`; first instance still answers `get_autocad_context` |
| Civil 3D 2026 (`acad.exe /product C3D`, same R25.1) | main window up, **no new line in `loader.log`** after 180 s → bundle `Platform="AutoCAD"` not loaded; only the Revit and AutoCAD pipes exist |

## Defects found and fixed during verification
1. **Seed `insert_block` was quarantined by the harness itself** (Event Log: "Tool insert_block quarantined: 3/5 recent runs failed") — the phase-4 smoke runs called it with `NO-SUCH-BLOCK` on purpose; those `ArgumentException` refusals counted as tool failures, so two correct runs in B tipped it over. Fix (engine, both hosts): runs whose error starts with `Argument…Exception:` are the caller's and are excluded from the stability window (`ToolRegistryDb.StabilityRunFilter`); they stay in the history. `insert_block` restored + re-approved through the CLI; test `Argument_errors_are_the_callers_and_never_quarantine_a_tool`.
2. **A restored tool was re-quarantined on its first successful run** (run 1: `mcp_verify_count_block_refs` ended quarantined although D passed — the old failures were still in the window). Fix: the window starts at the tool's last `approved` / `published` / `restore` / `proposed_version` / `imported` / `status_changed` event (the last one is written by the reload when a status was edited by hand in tool.json — the reviewer's catch); tests `A_restored_tool_starts_with_a_clean_stability_window`, `A_status_edited_by_hand_in_tool_json_also_starts_a_clean_window`. Same behaviour would have hit Revit's `count_rooms_strict` from phase 9.
3. Pipe-in-use message said "another **Revit** … instance" on AutoCAD → `RequestDispatcher.HostName`, test `A_second_listener_on_the_same_pipe_faults_and_names_the_host`. Bridge log sink `shared: true` so a second instance can log the fault; `StatusDetail` got an `AutomationId` for the harness.

## Not verified
- Revit seed + execute with the opt-in on (E skip above); Revit 2025; AutoCAD 2026 Update 1.2 (.NET 10).
- Modal dialog open while a request waits (the busy path is proven with a command waiting for input; a modal dialog is the same `IsQuiescent=false` branch — not exercised).
- A second AutoCAD instance *serving* after the first closes (fail-fast only); `Platform="AutoCAD*"` for Civil 3D stays out of scope.
- `HPRebar/output/HPRebar.Mcp.Server.exe` not republished (locked by the running `hprebar-revit` MCP servers, pids 9008/33320) — restart that MCP and run `dotnet publish HPRebar/HPRebar.Mcp.Server …` to ship the phase-0…5 engine to Claude Code's Revit entry.
