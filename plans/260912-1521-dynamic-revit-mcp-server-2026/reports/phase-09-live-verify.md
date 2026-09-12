# Phase 9 — Live verification in Revit 2026 (2026-09-12)

Machine: dev (Revit 2026, `NhaDanDung-3Tang-KetCau.rvt`, active 3D view "MODEL REBAR"). Client: `output/HPRebar.Mcp.Server/HPRebar.Mcp.Server.exe` over stdio (same path as `.mcp.json`). Bridge deployed 23:27 with `args` + `revit.analyze`. Scripts: scratchpad `scenario1_hit.py`, `scenario2_miss_to_memory.py`, `scenario3_quarantine.py`, `seed_smoke.py`.

## Scenario 1 — HIT
| Step | Result |
|---|---|
| `get_revit_context` | 2026, doc title, executionEnabled=true |
| `execute_revit_code` with `args {spacing:150, names:[A,B]}` | script read both through `args` → runId 1 |
| `search_tools "đếm phần tử theo tầng thống kê model"` | `analyze_model_statistics` 1.155 › `ai_element_filter` › `get_current_view_elements` |
| `analyze_model_statistics` (dynamic tool, by name) | 45 988 elements, per-category + per-level counts, runId 2 |
| `get_tool` after run | runs 1, stability 0.1; next search ranks it 3.72 |

## Scenario 2 — MISS → memory → HIT
| Step | Result |
|---|---|
| search "gán Mark từ Comments" | only unrelated tools (≈1.1) |
| ad-hoc script dryRun | 20 columns updated, rolled back, runId 7, **hint** present |
| `get_run 7` | literal `100 → maxLength` line 2, hasLoops |
| `propose_tool set_mark_from_comments` (4 args) | accepted, draft, 0 warnings |
| `test_tool` | 2/2 pass (20 columns / 93 beams, dryRun) → tested |
| `publish_tool` | pending_approval, `_review/set_mark_from_comments.md` |
| gate | not in `tools/list`; `run_tool` refused |
| `registry approve … --by "Sang (CLI)"` | published; `notifications/tools/list_changed`; tool listed |
| call by name dryRun / real | 20 marks set (C-A1 …) — verified with a read script |
| `get_tool` | approvedBy, createdFromRunId 7, runs 2 (tests excluded), stability 0.2 |

## Scenario 3 — degradation
| Step | Result |
|---|---|
| propose/test/publish/approve `count_rooms_strict` | published |
| 5 × strict run on a model without rooms | 5 failures → **quarantined** automatically, notes explain, removed from `tools/list` (Unknown tool) |
| `manage_tool restore` | draft; CLI `list --status draft` shows it |

## Seed smoke (dryRun for writers)
| Tool | Outcome |
|---|---|
| get_current_view_elements, get_selected_elements, export_room_data, get_current_view_info, ai_element_filter, get_material_quantities, get_available_family_types, analyze_model_statistics | ✅ real run |
| create_level, create_grid, create_room, create_structural_framing_system, create_line_based_element (wall + beam), create_point_based_element (column, rotated), create_surface_based_element (floor) | ✅ dryRun, rolled back |
| create_dimensions | ✅ in structural plan "Tầng 1" between grids 1–2 (value 4000); auto-detect in 3D view correctly reports "pass elementIds" |
| tag_all_walls | ✅ 2 tags in plan view; in 3D view: Revit error "3D view … is not locked" (expected) |
| tag_all_rooms | ✅ after fix (RoomTagType via FamilySymbol collector); 0 rooms in this model |
| operate_element Isolate / ResetIsolate | ✅ real |
| color_elements | ✅ dryRun |
| delete_element | ✅ dryRun (1 requested → 6 removed incl. dependents, rolled back) |

## Defects found and fixed during verification
1. 4 seeds named `ScriptArgs` in helper signatures; the bridge's default imports did not include `HPRebar.McpBridge.Core.Scripting` → CS0246 in Revit while the metadata compile test passed (its wrapper had the using). Fix: seeds use the fully-qualified name, the test wrapper mirrors the bridge imports, and the bridge gains the import for AI-written tools (effective at the next add-in deploy).
2. `tag_all_rooms` used `OfClass(typeof(RoomTagType))`, which Revit rejects → collect `FamilySymbol` of `OST_RoomTags` and `OfType<RoomTagType>()`.
3. Seeds on disk were never upgraded → `SeedInstaller` now records install checksums in `_seeds.json`, upgrades untouched seeds, adopts pre-manifest folders that still equal the shipped seed, never touches edited ones (verified live: `tag_all_rooms` fix reached the library without user action).

## Not verified
- Revit 2025 (bridge builds, never run).
- Tools that need families absent from the RC template (doors/windows host detection, room tags on a model with rooms, ceilings/roofs) — code paths compile and are guarded by clear error messages only.
- `test_tool` with `realRun=true`; policy `auto` only in xUnit.
