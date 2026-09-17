# Workflows — end-to-end call sequences that were verified live (AutoCAD 2026, harness 2026-09-17)

Every sequence starts with `get_autocad_context` (opt-in, quiescence, INSUNITS). Tool names are `mcp__hprebar-autocad__<name>`; every length is mm.

## 1. Understand a drawing, then audit it and cloud the findings

1. `get_drawing_context {includeLayouts: true, includeLayers: true}` → layers with counts, layouts, units.
2. `query_entities {filter: {layers: ["S-*"]}, mode: "summary", limit: 100}` → handles, types, bounds; `mode: "detail"` for vertices, text, `textHeightMm`, `color` (page with `offset`; detail is capped at 20 entities × 64 vertices).
3. `classify_aec_entities {filter: {layers: ["S-*", "A-*", "M-*"]}, minConfidence: 0.6}` → `aecType`, `confidence`, `evidence`, `properties {widthMm, depthMm, areaMm2, centroidMm}`; `includeUnknown: true` to see what the rule set skips; `ruleSet: "user"` for `%AppData%\HPAutoCad\McpServer\rules\aec-classification.json`.
4. `audit_aec_drawing {sections: ["geometry", "standards"], minSeverity: "warning"}` → `GEO-`/`STD-` issues severity-first with `bySeverity`; `cad_standards_check {checks: ["layer_naming", "entity_layer", "unused_layer"]}` alone when only standards matter (`unused_layer` needs an empty filter).
5. Show the user the issues; on approval `create_issue_markup {issues: <the issue objects>, style: "revcloud", dryRun: true}` → then without `dryRun`. Each cloud goes to the space of its own entities; table findings (no location) are skipped with a warning.

## 2. Structure: grids, members, checks, tags, schedule

1. `structural_detect_grids {}` → `G-…` lines with labels from bubbles, intersections, spacing; `reachMm` when bubbles sit far from the line ends.
2. `structural_detect_members {kinds: ["column", "beam"], prefixes: {column: "C", beam: "B"}}` → members with size (`400×400`, `Ø600`, `L 5600`), axis, `mark` read from a nearby text or a block `MARK` attribute.
3. `structural_member_connectivity_check {}` (`gap_to_support`, `unsupported_end`, `beam_without_supports`), `structural_column_alignment_check {alignmentToleranceMm: 25}` (`column_off_grid`, `column_no_grid`), `structural_opening_conflict_check {}` → `STR-*` issues.
4. `structural_tag_members {kinds: ["column"], start: 1, digits: 2, apply: false}` → the plan (existing marks kept, numbers reserved); `apply: true` writes texts on `S-ANNO-TEXT` (or edits the existing mark text in place, or the block's `MARK` attribute); `overwrite: true` renumbers.
5. `structural_generate_member_schedule {kinds: ["column", "beam"], writeTable: true, insertPoint: {x, y}, title: "COLUMN SCHEDULE"}` → rows by kind + section, one `ACAD_TABLE`.

## 3. Architecture: rooms, boundary check, tags, areas, dimensions

1. `arch_detect_rooms {}` → `R-nnn` rooms as the bounded faces of the wall drawing (single- or double-line; doorways 600–2500 mm bridged automatically; gaps ≤ `tolerance.roomGap` 25 snapped) with `name`/`number`/`department` read from the texts inside, `areaM2`, `labelPointMm`, `outlineMm` (≤ 32 vertices). Explicit closed outlines on room layers replace the wall loop they duplicate.
2. `arch_room_boundary_check {detection: {maxGapMm: 300}}` → `open_boundary`, `boundary_gap` (one per facing pair), `opening_assumed`, `unlabelled_room` … (`ARC-*`).
3. `arch_create_room_tags {format: "{number} {name}\\P{areaM2} m²", apply: false}` → the tags planned; `apply: true` writes one MTEXT per room on `A-ANNO-ROOM`; `blockName` + `attributes {NAME: "{name}"}` for a tag block; `onlyUnlabelled: true` to skip rooms already labelled.
4. `arch_generate_area_schedule {groupBy: "department"}` → rows with `areaM2`, `percent`, `rooms`.
5. `arch_auto_dimension_plan {rules: [{rule: "overall", sides: ["bottom", "right"], offsetMm: 800}], apply: false}` → aligned dimensions planned per room; `apply: true` draws them.

Pass the same `filter` / `tolerance` / `labels` / `detection` to every `arch_*` call so `R-nnn` ids agree.

## 4. MEP: networks, connectivity, clashes, opening requests

1. `mep_detect_network {detection: {systems: {CHW: ["M-PIPE*"], SA: ["M-DUCT*"]}, nearMissMm: 100}}` → networks `N-nnn` (runs, nodes, open ends, systems, `duplicateOverlapMm`); a node never merges systems.
2. `mep_connectivity_check {}` → `near_miss`, `open_end`, `disconnected_run`, `orphan_node`, `duplicate_run`, `mixed_system` (`MEP-*`); `mep_endpoint_check {includeConnected: false}` → every open end with the nearest run/node and the gap.
3. `aec_clash_check {setA: {aecTypes: ["Pipe", "Duct", "CableTray"]}, setB: {aecTypes: ["StructuralColumn", "StructuralBeam", "StructuralWall"]}, clearanceMm: 50}` → `hard_clash` (critical: an MEP element crosses / lies inside / ends inside a member, or two services cross), `clearance_clash` (warning, `valueMm` = gap, located in the gap); contacts and room/slab overlaps are counted (`summary.contacts`, `areaOverlaps`) and listed only with `minSeverity: "info"`. `setB: {}` = setA against itself.
4. `aec_create_opening_requests {routes: {aecTypes: ["Duct"]}, hosts: {aecTypes: ["StructuralBeam", "StructuralWall"]}, sizes: {ductMm: 600, marginMm: 75}, apply: false}` → one request `OPN-nnn` per pass through a wall/beam (line: side change; outline: the stretch inside, ≤ `maxChordMm` 1000 — longer chords are runs inside the outline and are counted in `longChordsSkipped`); `apply: true` draws a rectangle turned along the host + an MLeader on `HP-MCP-OPENINGS`.

## 5. Change set: several edits, one undo entry, rollback by handle

1. `begin_change_set {label: "L3 reroute"}` → `CS-001`.
2. `create_entities_batch {changeSetId: "CS-001", items: [...]}`, `update_entities_batch {changeSetId: "CS-001", items: [{handle, set: {...}}]}`, `manage_hatches {changeSetId: "CS-001", op: "update", handles: [...], set: {pattern: "SOLID"}}` … — each answers `summary.recorded: true, opIndex` and writes nothing; a bad handle / unknown `op` / xref bind is refused at once.
3. `preview_change_set {changeSetId: "CS-001"}` → the ops in order (tool, one-line summary, handles, raw args ≤ 500 chars; paged 30).
4. Real effect without keeping it: `commit_change_set {changeSetId: "CS-001", dryRun: true}` → the run reports created/modified/deleted and `rolledBack: true`; the next call sees the rollback and the set is pending again (a note says so).
5. `commit_change_set {changeSetId: "CS-001"}` → replay in one run; `summary {created, modified, deleted, snapshots, layersCreated, blocksCreated}`; `items[i]` = op i+1 with `changed ["op N", "created n", …]`. Atomic: one failed op → `ArgumentException` naming it, nothing written, set still pending (`atomic: false` keeps what succeeded).
6. `rollback_change_set {changeSetId: "CS-001"}` → created erased, erased un-erased, modified restored by handle (`summary.erased/restored/unerased`); a locked layer → that handle fails, the set stays committed with its snapshots — unlock and roll back again; `keep: true` closes the set and keeps the work.
7. `get_change_summary {}` → every set of the drawing with state, counts and notes; a second drawing has its own store.

## 6. Toolify a script the user keeps asking for

1. Run it once with `execute_autocad_code` (literals in the code) → `get_run {runId}` shows the code and the literals to parameterise.
2. `propose_tool {name: "count_block_refs_by_layer", category: "Block", description, inputSchema: {properties: {blockName, layer}}, code: "...args.Str(\"blockName\")...", examples: [...], transaction: "none"}` → the registry's analyzer proves every schema key is read as `args.X("literal")` and the guard passes.
3. `test_tool {name}` (runs the examples with dryRun) → `publish_tool {name}` → pending; the user approves with `HPAutoCad.Mcp.Server.exe registry approve <name> --by <who>` → `tools/list_changed` within 0.5 s → call it by name.
4. A published tool failing ≥ 5 runs with > 40 % failures is quarantined automatically (`manage_tool {op: "restore"}` after fixing with `propose_tool {newVersion: true}`); argument errors never count.
