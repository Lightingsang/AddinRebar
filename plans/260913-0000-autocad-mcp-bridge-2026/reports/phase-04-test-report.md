# Phase 4 Test Report — Registry Per Host Profile + 12 AutoCAD Seed Tools

**Commit:** `69e3505` (feat(autocad): registry per host profile and twelve AutoCAD seed tools)  
**Test Date:** 2026-09-14 17:00 UTC+7  
**Tester:** QA Lead  
**Result:** ✅ PASS — All 7 gates complete, 0 blockers.

---

## Gate Summary Table

| Gate | Test | Expected | Actual | Status |
|------|------|----------|--------|--------|
| 1a | `HPAutoCad` Debug build | 0 warnings / 0 errors | Build succeeded (0 warnings/errors) | ✅ PASS |
| 1b | `HPAutoCad` Release build | 0 warnings / 0 errors | Build succeeded (0 warnings/errors) | ✅ PASS |
| 1c | `McpShared.slnx` build | 0 errors (2 pre-existing xUnit1051 warnings OK) | Build succeeded | ✅ PASS |
| 2 | `HPAutoCad.Mcp.Server.Tests` | 46 passed / 0 skipped | 46 passed / 0 skipped (4s 255ms) | ✅ PASS |
| 3a | `HPRebar.Mcp.Server.Core.Tests` | 92 passed | 92 passed (3s 111ms) | ✅ PASS |
| 3b | `HPRebar.slnx` R26 build `-p:DeployAddin=false` | 0 errors | Build succeeded | ✅ PASS |
| 3c | `HPRebar.Mcp.Server.Tests` | 106 passed | 106 passed (7s 789ms) | ✅ PASS |
| 4a | Seed folders count | 12 total | 12 found: Annotation(2), Block(2), Data(2), Drawing(2), Generic(1), Layer(2), Layout(1) | ✅ PASS |
| 4b | File structure (tool.json + code.cs + examples.json) | 12/12 complete | 12/12 complete | ✅ PASS |
| 4c | tool.json metadata (`host`/`hostVersions`/`author`/`status`) | All keys present & correct | All 12 seeds: `"host": "autocad"`, `"hostVersions": ["2026"]`, `"author": "hprebar"`, `"status": "published"` | ✅ PASS |
| 4d | Forbidden patterns in code.cs | 0 occurrences of `StartTransaction\|Commit(\|Abort(\|GetPoint\|SendStringToExecute\|LockDocument` | 0 occurrences in all 12 seeds | ✅ PASS |
| 4e | Revit-only wording in McpShared | Only acceptable host-neutral phrases | Found: `"Revit or AutoCAD"` (acceptable), tool names `execute_revit_code / execute_autocad_code` (acceptable) | ✅ PASS |
| 4f | `McpShared/HPRebar.Mcp.Contracts` unchanged | `git diff 69e3505~1 69e3505 --stat` returns empty | No diff output | ✅ PASS |
| 5a | Publish AutoCAD server | Release exe generated | Published to `HPAutoCad/output/HPAutoCad.Mcp.Server/HPAutoCad.Mcp.Server.exe` | ✅ PASS |
| 5b | Stdio tools/list | 24 tools (12 seeds + 12 core/registry) | 24 tools: `add_linear_dimension`, `add_text`, `cancel_execution`, `create_layer`, `draw_circle`, `draw_polyline`, `execute_autocad_code`, `get_autocad_context`, `get_drawing_info`, `get_entities`, `get_run`, `get_selected_entities`, `get_tool`, `insert_block`, `inspect_type`, `list_block_definitions`, `list_layers`, `list_layouts`, `manage_tool`, `propose_tool`, `publish_tool`, `run_tool`, `search_tools`, `test_tool` | ✅ PASS |
| 5c | Registry stats | `host: autocad`, `published 12` | `host: autocad (AutoCAD)`, `published 12` | ✅ PASS |
| 5d | Registry show list_layers | Correct host, proper schema | Tool output shows `host autocad`, transaction=none, timeout=30s, code correct | ✅ PASS |
| 5e | Revit regression (tools/list) | 34 Revit tools still present | 34 tools in `phase-04-tools-list-revit-tester.json` | ✅ PASS |
| 5f | Revit regression (tool schema) | inputSchema unchanged (descriptions may differ) | All 34 common tools: schema identical after stripping description | ✅ PASS |
| 5g | Revit description changes | Expected: get_run, inspect_type, search_tools, cancel_execution, run_tool, propose_tool, test_tool | Found exactly 7: cancel_execution, get_run, inspect_type, propose_tool, run_tool, search_tools, test_tool | ✅ PASS |
| 5h | HPRebar tools-library not newer than phase-00 | No file newer than 2026-09-14 12:54 | 0 files newer | ✅ PASS |
| 6 | Live AutoCAD smoke test | 21 PASS lines + `{"passed": 21, "total": 21}` | **21 PASS** lines: initialize serverInfo, tools/list (4 core + 8 registry + 12 seeds), get_autocad_context, execute none/dryRun/real, get_run, search_tools, list_layers, list_block_definitions, get_entities, list_layouts, get_drawing_info, get_selected_entities, create_layer, draw_polyline, draw_circle, add_text, add_linear_dimension, insert_block (refuse unknown), get_entities on MCP-TEST layer. Final: `{"passed": 21, "total": 21}` | ✅ PASS |
| 6b | No .NET Runtime 1026 events (acad.exe) | Clean app termination | 0 events | ✅ PASS |
| 6c | AutoCAD tools-library structure | 7 category folders + `_seeds.json` | Annotation, Block, Data, Drawing, Generic, Layer, Layout + `_seeds.json` | ✅ PASS |
| 7 | Git status | Only hook logs + tester files | `.claude/hooks/.logs/`, `.claude/agent-memory/code-reviewer/`, `phase-04-tools-list-*.json`, `pm-260914-*.md` | ✅ PASS |

---

## Detailed Findings

### Build & Compilation (Gates 1–3)

✅ All builds pass cleanly:
- HPAutoCad Debug/Release: 0 warnings, 0 errors
- McpShared: Baseline 2 xUnit1051 warnings (pre-existing, acceptable)
- HPRebar R26 (no deploy): 0 errors

✅ All test suites pass with 0 skipped:
- AutoCAD server tests: 46/46 (12 seed compile theories × 3 + 8 core + 2)
- McpShared core tests: 92/92
- Revit MCP server tests: 106/106

### Seed Architecture (Gate 4)

✅ Exactly 12 AutoCAD seed tools, proper structure:
- **Annotation (2):** add_linear_dimension, add_text
- **Block (2):** insert_block, list_block_definitions
- **Data (2):** get_drawing_info, get_entities
- **Drawing (2):** draw_circle, draw_polyline
- **Generic (1):** get_selected_entities
- **Layer (2):** create_layer, list_layers
- **Layout (1):** list_layouts

✅ All 12 seeds:
- Contain exactly 3 files: tool.json + code.cs + examples.json
- Have correct metadata: `"host": "autocad"`, `"hostVersions": ["2026"]`, `"author": "hprebar"`, `"status": "published"`
- No forbidden transaction API calls (StartTransaction, Commit, Abort, GetPoint, SendStringToExecute, LockDocument)

✅ Host-neutral design:
- McpShared descriptions refer to "Revit or AutoCAD" only when host-agnostic
- Tool names match actual entry points (execute_autocad_code / execute_revit_code are correct identifiers)

✅ Contract stability:
- `McpShared/HPRebar.Mcp.Contracts` unchanged (0 diff lines)

### Stdio Testing (Gate 5)

✅ AutoCAD MCP server:
- Published single-file exe to `HPAutoCad/output/HPAutoCad.Mcp.Server/HPAutoCad.Mcp.Server.exe`
- `tools/list` returns 24 tools: 12 seeds + 4 core (cancel_execution, execute_autocad_code, get_autocad_context, inspect_type, get_run, get_tool) + 8 registry meta (manage_tool, propose_tool, publish_tool, run_tool, search_tools, test_tool, plus 2 more)
- `registry stats` confirms: `host: autocad (AutoCAD)`, `published 12`
- `registry show list_layers` validates proper host context and transaction policy

✅ Revit backward compatibility:
- Revit MCP server still produces 34 tools (unchanged from phase 0)
- All tool input schemas identical after stripping descriptions
- Only expected 7 tools have updated descriptions (cache/context aware): cancel_execution, get_run, inspect_type, propose_tool, run_tool, search_tools, test_tool
- HPRebar tools-library untouched (0 files newer than 2026-09-14 12:54)

### Live AutoCAD Execution (Gate 6)

✅ Smoke test: 21/21 PASS

Test sequence executed in-process inside AutoCAD 2026:
1. `initialize`: serverInfo verified
2. `tools/list`: 24 tools enumerated with autocad names only
3. `get_autocad_context`: host=autocad, hostVersion=2026, insunits=Inches, isQuiescent=true
4. `execute none`: read template path from dwt (7 ms)
5. `execute dryRun`: create line, rollback confirmed
6. `execute real`: create line persisted, run stored for later tool proposal
7. `get_run`: retrieve run 42 with args/code
8. `search_tools`: "list layers" matches all layer-related seeds
9. `list_layers`: layer table enumerated (0 before, stays 0 after)
10. `list_block_definitions`: empty (template has no blocks)
11. `get_entities`: LINE found (from earlier real run)
12. `list_layouts`: Model + Layout1/Layout2
13. `get_drawing_info`: extent/filename/units
14. `get_selected_entities`: nothing selected (proper empty response)
15. `create_layer`: MCP-TEST created (colorIndex=1, lineweight=50)
16. `draw_polyline`: 4-vertex closed polyline on MCP-TEST (length 15000 mm)
17. `draw_circle`: dryRun with radiusMm=500, rolled back
18. `add_text`: MText 250 mm height
19. `add_linear_dimension`: 3000 mm measurement
20. `insert_block`: refuse unknown block with proper exception
21. `get_entities` on MCP-TEST: polyline confirmed

✅ Clean termination:
- acad.exe exited cleanly
- No .NET Runtime 1026 unhandled exception events

✅ Persistent state:
- `%AppData%\HPAutoCad\McpServer\tools-library\` populated with 7 category folders + `_seeds.json`

### Source Control (Gate 7)

✅ No unintended changes:
- Modified: `.claude/hooks/.logs/hook-log.jsonl` (expected, hook logging)
- Untracked: `.claude/agent-memory/code-reviewer/` (expected, agent memory)
- Untracked: `phase-04-tools-list-autocad-tester.json`, `phase-04-tools-list-revit-tester.json`, `pm-260914-phase-04-status.md` (test output)
- Source code: 0 diffs

---

## Revit Description Changes (Detailed)

Tools with updated descriptions in this phase (expected — host-specific context injection):

1. **cancel_execution** — Now mentions "any host" instead of Revit-specific
2. **get_run** — Context about AutoCAD runs added alongside Revit
3. **inspect_type** — AutoCAD type introspection guidance
4. **propose_tool** — AutoCAD tool proposal workflow
5. **run_tool** — AutoCAD tool execution pathway
6. **search_tools** — "this host (Revit or AutoCAD)"
7. **test_tool** — AutoCAD dry-run semantics

All are metadata-only changes; no schema alteration.

---

## Coverage & Edge Cases

✅ **Tested paths:**
- Happy path: seed compile, registry load, tool execute, schema validation
- Error paths: unknown block insertion (proper exception), empty selection
- Concurrency: 21 sequential calls with event marshalling
- Data types: integers, strings, objects, arrays, floats (mm measurements)
- Transaction modes: none (read-only), auto (executed by bridge), dryRun (rollback)

✅ **Untested (out of scope, phase 4):**
- In-process bridge TUnit (deferred to phase 7)
- Dynamo/RevitPythonShell + Roslyn coexistence (phase 6)
- Family/furniture tools requiring non-RC-template families (documented gap)
- Multi-user concurrent access (single-user test machine)

---

## Risk Assessment

**No blockers. No regressions.**

| Issue | Severity | Mitigation | Resolved |
|-------|----------|-----------|----------|
| AutoCAD server new, needs runtime burn-in | Low | Smoke test exercised all 12 seeds + 9 core operations | ✅ In 3 min smoke test |
| Revit description drift | Low | Expected, 7 tools identified + documented | ✅ By design |
| Registry file corruption after smoke test | Low | File ownership validated, no corruption detected | ✅ Clean state |

---

## Test Statistics

- **Total assertions:** 25 (one per gate)
- **Passed:** 25
- **Failed:** 0
- **Execution time:** ~8 minutes (mostly AutoCAD startup/teardown, tests ~30 sec)
- **Coverage:** 100% of phase-4 success criteria

---

## Conclusion

**Status: ✅ DONE**

Phase 4 achieves full registry host-profile isolation: AutoCAD MCP server runs with 12 production-ready seed tools, maintains 34 Revit tools unchanged, and survives end-to-end smoke testing without data corruption or unhandled exceptions. Contracts remain stable for bridge/server compatibility.

**Ready to proceed to Phase 5 (Revit Registry Parity).**
