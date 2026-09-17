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
  Phase D adds step T: `cad_standards_check` (layer naming, entity layer, layer 0, colour override, unused layer on a scene with those defects;
  ids identical on a second run; checks subset; unused_layer skipped on a subset; unknown rule set) and `audit_aec_drawing` (geometry + standards
  in one severity order, minSeverity, paging, sections). Phase E adds step N: `structural_detect_grids` (a stub merged into grid A, bubble 2 as a block
  labelled by its attribute, 4 intersections, spacing), `structural_detect_members` (4 drawn columns + a `COL-400` block measured without its MARK attribute and
  marked by it, beams with axes, slab, openings), connectivity (7 mm gap at 5 mm), column alignment (10 mm off grid B at 5 mm; the far block column has no grid),
  opening conflicts (through / outside). Phase F adds step R: `arch_detect_rooms` (the closed and the 12 mm-open outlines, the 2 × 1 m rectangle, and a
  two-room single-line plan whose 900 mm doorways are bridged — `PHONG KHACH` / `101` read from the texts, `B01` not a number, the right room open by a
  250 mm gap; roomGap 5, maxOpeningMm 800, maxGapMm above minOpeningMm refused), `arch_room_boundary_check` (12 open ends, one boundary_gap between
  the ends, closed gaps, openings, unlabelled rooms), `arch_generate_area_schedule` (by name, percentages, unknown groupBy refused). Phase G adds step V on an
  MEP set (a pipe main with a tee branch, a branch 50 mm short, a lost run, a copy over the main, an inline valve block, a duct served by a diffuser block, an
  orphan diffuser): `mep_detect_network` (5 networks, the valve attached, 2 m drawn twice, systems by layer map, endpointConnection 60 joins the short branch,
  nearMissMm not above endpointConnection refused), `mep_connectivity_check` (near miss, open ends, disconnected runs, duplicate, one orphan; no runs → warned,
  no orphans), `mep_endpoint_check` (open ends near misses first, includeConnected). 83 checks. 2026-09-17: 83/83 (phase F: 75/75; phase E: 68/68; 2026-09-16 phase D: 63/63).
- `run-aec-edit-tools-live.ps1` + `aec-edit-tools-live.py` — the AEC write tools (phases C–D) through the same launcher (`run-aec-tools-live.ps1 -Script`)
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
  resolveStatus, invalid xref names/paths, detach refused whole, dryRun on every write tool; phase D step K: `create_issue_markup` revclouds + leaders
  from audit issues on a created markup layer coloured by severity, a rectangle around two handles, dryRun, atomic refusal, locked markup layer;
  after the phase-D review: table findings skipped, unresolvable handles refused, structured LAYER_LOCKED, a paper-space finding drawn on its layout, radius kept at a
  location, `filter.space` alone a filter; phase E step M: `structural_tag_members` preview / apply / overwrite in place with `prefixes` (3 texts modified, same
  handles) / idempotence / foreign marks under the defaults, `structural_generate_member_schedule` refused on a locked layer (nothing created), rows, ACAD_TABLE;
  phase F step A: `arch_create_room_tags` preview / dryRun / locked layer / unknown placeholder / block attribute the block lacks (refused before any write) /
  block preview / apply (one MTEXT at the label point, `written` not the plan) / detect after tagging reads the tag back as the name, `arch_auto_dimension_plan`
  plan / unknown rule / apply (2 aligned dimensions measuring the room's bounds).
  89 checks. 2026-09-17: 89/89 (phase E: 78/78; 2026-09-16 phase D: 67/67 (phase C round: 62/62 (first run 40/40, before the review: 33/40:
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
