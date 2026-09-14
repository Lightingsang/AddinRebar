# Phase 3: AutoCAD Server Exe — 2 Hours, Parity Port, Live 7/7 & 7/7, Zero Runtime Chaos

**Date**: 2026-09-14 16:35  
**Severity**: Low (two hygiene findings in review, no runtime issues)  
**Component**: HPAutoCad.Mcp.Server stdio exe / AutocadHostProfile  
**Status**: Resolved (fixes committed d9818f4 + a70feee)

## Bối cảnh

Phase 0 lifted every host-neutral piece into `McpShared/`. Phase 3 = the exe. No loader, no executor — just a profile + 4 attribute-decorated classes wired via `WithToolsFromAssembly`. AutocadHostProfile read `appsettings.json`, registry root = `%AppData%\HPAutoCad\McpServer\`, pipe name = `hpautocad-mcp-2026`, every field matching the Revit profile line for line. Engine picked it up unchanged. Result: thin 8-line Program, thin tool/resource/prompt classes, no engine modifications.

## Tổng Quan

**Commit d9818f4 (main + exe):** Profile, tools (`execute_autocad_code`, `get_autocad_context`), resources, prompts, harness helpers. Exe published 7.4 MB self-contained. Harness run 1 = 7/7 kịch bản (initialize, tools/list 12 no Revit-named, context without `revitVersion`, execute/dryRun/real, get_run, Revit exe running = 34 tools unchanged). 203 tests: 8 AutoCAD server, 89 McpShared, 106 HPRebar. Code review 8/10 → 2 mediums, 8 lows. Fix: modify few-shot checks LayerTable.Has (creates `WALLS` if missing); description 1844 → 1717 chars (duplicate sentences trimmed, budget 1800), `isFamily` + `revitVersion` dropped for non-Revit, harness `-Depth`, `#Requires -Version 7.3`, stderr drain. **Commit a70feee (review fixes):** Harness re-run 7/7 ✅.

## Số liệu

| Kiểm tra | Kết quả |
|---|---|
| `dotnet build HPAutoCad.slnx -c Debug/Release` | ✅ 0 warnings, 0 errors |
| `dotnet build HPRebar.slnx -c Debug.R26` | ✅ 0 errors (Core guarded by `HostId == revit`) |
| AutoCAD server tests | ✅ 8/8 (profile, options, 12 tools, resources/prompts, pipe) |
| McpShared tests | ✅ 89/89 (+1 Revit `revitVersion` regression) |
| HPRebar MCP tests | ✅ 106/106 |
| Harness run 1 (pre-fix) | ✅ 7/7 |
| Harness run 2 (post-fix) | ✅ 7/7 + audit |
| Two exes side by side | ✅ Revit 34 tools, AutoCAD 12 tools, disjoint registries |
| Code review | 8/10 → 2 mediums + 8 lows all resolved |

## Quyết định & Bài Học

**1. Description = the AI's only API doc.** Revit engine description says `tr`/deny list/units/transaction semantics. AutoCAD exe is longer (1717 vs Revit 1270) because AutoCAD's contract genuinely has more to say: `StartTransaction` banned by guard, `manual≡auto` undo merge, `none` fails on change, layers must exist before entity assignment, no modal dialogs. Every rule the bridge enforces must live in the description. The AI never reads `AutocadScriptRunner.cs:90` — it reads the tool description or fails.

**2. Few-shot examples must survive a fresh drawing.** The modify example set `pl.Layer = "WALLS"` with no LayerTable.Has check. Default `acad.dwt` has only layer `0` → `eKeyNotFound` → example teaches nothing. Now the example checks and creates the layer (as a side effect `changed.added = 2` on first run, which the AI should learn). Lesson: trace every literal (layer name, block, entity type) in few-shot against what a blank drawing contains.

**3. Harness in the repo beats `.mcp.json` edits.** Phase 2's smoke was manual post-harness; phase 3 `run-server-smoke.ps1` is checked in, refuses to run if AutoCAD is open (no races), reads the published exe direct. A new phase (or a human verifying both exes) never edits `.mcp.json` — they run the harness. Detects stdio issues early (stderr deadlock, JSON depth truncation).

## Tiếp theo

**Phase 4:** HostProfile meta-tool descriptions still say "Revit" / `execute_revit_code` — rephrase to profile-neutral or wire from `IHostProfile`. `get_run.revitVersion` field → phase-04 decision (alias `hostVersion`, keep dual-name for compat, or drop for non-Revit). AutoCAD seed library (empty for MVP; compile-check harness mirrors `AutocadImports` + `AutocadScriptGlobals` — reuse it).

**Commit:** d9818f4 + a70feee. Bundle 25 files (exe + tests + Core + harness), 203 tests, harness 7/7 ×2, 0 runtime crashes, 0 build warnings.

**Status**: DONE  
**File**: F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\docs\journals\2026-09-14-phase-3-autocad-server-exe.md
