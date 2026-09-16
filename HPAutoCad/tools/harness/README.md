# Harnesses — unattended checks against a live AutoCAD 2026

Four scripts share `harness-common.ps1` (SECURELOAD auto-answer, UI Automation opt-in + Ribbon helpers, guarded AutoCAD start):

- `run-bridge-unattended.ps1` — the bridge alone: `pipe-scenarios.py` speaks NDJSON JSON-RPC straight to the
  named pipe `\.\pipe\hpautocad-mcp-2026`, no MCP server involved.
- `run-server-smoke.ps1` — the published `HPAutoCad.Mcp.Server.exe` over stdio through `McpShared/tools/mcp-call.py`, exactly as a
  host AI would (initialize, tools/list, get_autocad_context, execute_autocad_code read / dry run / real run, get_run,
  every seed by name — 22 steps).
- `run-live-verify.ps1` — the full phase-5 proof through `live-verify.py` on **one** stdio session (`McpShared/tools/mcp-session.py`, the helper every HP MCP harness shares):
  the execute matrix (incl. busy → ESC posted → retry, no drawing, opt-in off), every seed with a real block and a
  pickfirst set, MISS → `propose_tool` → `test_tool` → `publish_tool` → CLI `registry approve` → `tools/list_changed`
  → call by name, an unguarded tool quarantined then restored/fixed/re-approved, the Revit exe beside it; with
  `-IncludeIsolation` also a second AutoCAD (pipe in use) and Civil 3D (bundle not loaded). `-OnlyIsolation`,
  `-SkipRevit`, `-RevitExe <path>`. The server and the CLI run on an **isolated registry root**
  (`HPAutoCad/output/live-verify/registry`, via `HPAUTOCAD_MCP_Registry__LibraryPath/DbPath`; seeds are installed into it
  on first start), so runs, quarantines and the two tools it proposes never touch `%AppData%\HPAutoCad\McpServer`
  (`-UseLiveRegistry` opts back in). The entities it draws stay in the harness's unsaved drawing. It kills only the
  AutoCAD/Civil 3D processes it started. Outputs under `HPAutoCad/output/live-verify/`.
- `run-aec-tools-live.ps1` + `aec-tools-live.py` — the AEC tools (phases A–B) in one stdio session on an isolated registry root: draws a
  scene with deliberate defects through `execute_autocad_code` (columns, beams incl. a 7 mm gap and a duplicate, a room, an almost-closed
  outline, overlapping and gapped walls, a bow-tie, a zero-length line, a pipe crossing a beam, a circle, text, an arc), then
  `get_drawing_context`, `query_entities` (filters, paging, detail, property selector), `query_entities_spatial` (crosses, within,
  nearest, distance_to, touches ± tolerance), `measure_geometry` (every measure), `detect_geometry_issues` (every issue kind, restricted
  types, tolerance), the error paths (INVALID_HANDLE, ERASED, NOT_CLOSED, ArgumentException), geometry that only reads right through
  AutoCAD's own maths (a mirrored arc with normal -Z, a bulge polyline, a hatch loop, a block with an attribute) and a 3 000-line
  performance grid (layer query, spatial nearest, issue scan, timed), `classify_aec_entities` (columns, beams, walls, pipe, door block,
  unknowns, discipline filter + paging) and `get_entity_relationships` (connected with a 7 mm gap, intersect, self-set parallel de-dup,
  empty source refused; honest `count` past the relationship cap, classify page cap warning, every relationship located off the origin, a closed polyline with a repeated closing vertex read as 4 vertices). Defaults to the Debug server exe so new seeds run without republishing; 55 checks. 2026-09-16: 51/51, after the phase-B review round 55/55.
- `run-aec-edit-tools-live.ps1` + `aec-edit-tools-live.py` — the AEC write tools (phase C) through the same launcher (`run-aec-tools-live.ps1 -Script`)
  on their own output folder: a scene with a locked and a frozen layer, a room, a column, an open outline, a text and an attributed block;
  `create_entities_batch` (5 types in one atomic batch, atomic refusal on a locked layer with nothing created, non-atomic partial, frozen
  warning, block + dimension, dryRun rolled back, empty refused), `update_entities_batch` (shared set, per-item text/geometry/rotate/move,
  LAYER_LOCKED, atomic refusal leaving the drawing untouched, ERASED / INVALID_HANDLE / UNSUPPORTED_ENTITY), `manage_blocks_attributes`
  (every op; setDynamic refused on a plain block), `manage_annotations` (text, mtext, four dimension kinds with measurements, mleader,
  update, delete refusing geometry), `manage_hatches` (ANSI31 from a polyline with area, SOLID from a seed point, detectBoundary innermost
  first, NOT_CLOSED, update, delete), `manage_xrefs` (a DWG written by `Wblock` + `SaveAs`, attach + overlay, unload → bind refused,
  reload, detach, bind), then `U` over COM after a REGEN boundary reverting the last batch; after the phase-C review: structural refusals for
  locked layers on single creates, an attribute on a locked layer, an atomic update with a bad key (change counter 0), block-definition entities,
  duplicate handles, a hatch keeping its layer/colour, a polygon hatch with a style, an associative hatch following its moved boundary,
  resolveStatus, invalid xref names/paths, detach refused whole, dryRun on every write tool. 62 checks. 2026-09-16: 62/62 (first run 40/40, before the review: 33/40:
  `Hatch.Area` is not readable in the creating transaction → boundary fallback; a top-level `using var` compiles in the seed test's method
  wrapper but not as a Roslyn script).
- `run-ribbon-check.ps1` — the Ribbon tab through UI Automation (AdWindows exposes a tab header as a Button whose
  AutomationId is the tab id, and a RibbonButton as a Button named after its text): exactly one `HPAUTOCAD_MCP_TAB`,
  still exactly one after `WSCURRENT` to another workspace and back and after a `COLORTHEME` round trip (COM;
  the loader rebuilds the tab so the icon ink follows the theme — one screenshot per theme), then Invoke
  "MCP Bridge" (window with `AllowExecution` appears), Invoke it again (still one window), no failure in loader.log.
  The icon itself is a MANUAL item (UIA cannot read pixels): the run exits 2 with the two screenshots — never a PASS.

`run-bridge-unattended.ps1` drives the bridge run without a human:

1. starts `acad.exe /b bridge.scr` (`HPMCPBRIDGE` opens the status window, `HPMCPSTART` the listener) and
   answers the SECURELOAD prompt with *Always Load* if it appears;
2. waits for the pipe, then ticks "Allow AI code execution" through UI Automation (the opt-in is never
   persisted, by design) — with the box off it first checks that execute is refused with `-32001`;
3. runs the scenarios: ping, context, read, dryRun, commit, exception, `none` + modify, guard, compile
   error, cancel, timeout, progress/logs, serializer/args, modify + erase, undo (`U` after a `REGEN`
   boundary), then closes the drawing through COM (`-32003`), opens a new one and starts `LINE` through
   COM so the bridge answers `-32002` after the busy grace;
4. kills AutoCAD (the bridge itself never quits the host).

**Destructive by design — read before running:** it refuses to start while any AutoCAD is running, and every
COM call checks it is talking to the acad.exe it started; still, the drawings it opens are closed without
saving, the process is killed at the end, and answering the SECURELOAD prompt with *Always Load* trusts the
bundle folder permanently for this Windows user.

```powershell
pwsh HPAutoCad/tools/harness/run-bridge-unattended.ps1      # AutoCAD must be closed; ~2 minutes warm
pwsh HPAutoCad/tools/harness/run-server-smoke.ps1           # same, for the published exe (publish first — see its header)
pwsh HPAutoCad/tools/harness/run-live-verify.ps1 -IncludeIsolation   # phase-5 proof (~6 min, +8 with isolation)
pwsh HPAutoCad/tools/harness/run-ribbon-check.ps1                 # Ribbon tab: one tab, still one after workspace + theme round trips, MCP Bridge opens the window once (~2 min)
python HPAutoCad/tools/harness/live-verify.py --exe <autocad exe> --revit-exe <revit exe> --only e   # Revit beside, no AutoCAD start
python HPAutoCad/tools/harness/pipe-scenarios.py --only ping,context   # against an AutoCAD you started yourself
python McpShared/tools/mcp-call.py <exe> tools/list                    # any HP MCP exe, no AutoCAD/Revit needed
```

COM automation goes through Windows PowerShell 5.1 (`GetActiveObject` is not in PowerShell 7); a `SendCommand`
that leaves a command waiting for input never returns, so it runs detached and is killed at the end.
