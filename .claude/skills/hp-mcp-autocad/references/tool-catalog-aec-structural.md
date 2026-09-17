# HPAutoCad MCP — tool catalog: AEC classification / relationships and the structural tools

Generated from `tools/list` of `HPAutoCad.Mcp.Server.exe` (62 tools in all) on an isolated registry — the surface a fresh install shows. Names are `mcp__hprebar-autocad__<name>` in Claude Code. `REQ` = required; every length is millimetres, points are `{x, y}` objects in mm; handles are hex strings. Seed descriptions end with `[Registry tool v1, <Category>, transaction=…]`; that suffix is stripped here.

## Aec

### `classify_aec_entities` — Classify AEC entities

*readOnlyHint.* Reads entities as AEC objects: StructuralColumn / StructuralBeam / StructuralWall / StructuralSlab / StructuralOpening / StructuralGrid, ArchitecturalWall / Door / Window / Room / Stair / Furniture, Pipe / Duct / CableTray / Equipment / Fixture / Terminal / Fitting — each with a confidence, the evidence (layer, block name, closed footprint and size, nearby text mark) and dimensions in mm (width x depth, length, area, orientation, centroid, diameter). Rules come from a JSON rule set (embedded default, or a project file under the MCP server's rules folder) matching layer wildcards, entity types, block names, closed/open, size ranges, aspect ratio and text. Use this before any structural, architectural or MEP question, when the user asks 'what is this', 'find the columns/beams/walls/pipes/doors', or to feed handles into relationship, connectivity, clash or schedule tools. Read-only.

| arg | type | default | description |
|---|---|---|---|
| `filter` | object |  | Which entities to consider. Types, layers and block names accept AutoCAD wildcards (* and ?) and comma lists; handles short-circuit everything else. |
| `handles` | array<string> |  | Classify exactly these entities instead of a filter |
| `disciplines` | array<string> |  | Only rules of these disciplines; omit for all |
| `minConfidence` | number | 0.5 | Drop objects below this confidence (they count as unknown) |
| `includeUnknown` | boolean | false | Also return entities no rule matched, as aecType Unknown with the runner-up rules |
| `ruleSet` | string |  | default (embedded), user (rules\aec-classification.json beside the server registry) or the name of another JSON file there; omitted = user if present, else default |
| `limit` | integer | 50 | Objects per page (an object with evidence and dimensions is ~0.5–1 KB; 50 keeps the answer under the 64 KB cap) |
| `offset` | integer | 0 | Skip this many objects (paging) |
| `maxCandidates` | integer | 5000 | Cap on entities read |

### `get_entity_relationships` — Entity relationships

*readOnlyHint.* Derives relationships between entities from their plan geometry, naming both ends by AEC type: intersect (boundaries cross or one lies inside the other), connected (an end of a run reaches the other entity within the connection tolerance — beam to column, pipe to fitting; the gap in mm and a confidence that falls with the gap), near (within maxDistance), inside / contains, touching, and the axis relations aligned / parallel / perpendicular on the main axes of linear members (valueMm carries a gap/distance/offset, angleDeg the angle between axes). Source and target are filters or handles; omit the target to relate the source set to itself. Use this for 'which beams sit on this column', 'is this pipe connected to the equipment', 'what is inside this room', 'are these walls aligned'. Read-only.

| arg | type | default | description |
|---|---|---|---|
| `filter` | object |  | Which entities to consider. Types, layers and block names accept AutoCAD wildcards (* and ?) and comma lists; handles short-circuit everything else. |
| `handles` | array<string> |  | Source entities by handle instead of a filter |
| `target` | object |  | Which entities to consider. Types, layers and block names accept AutoCAD wildcards (* and ?) and comma lists; handles short-circuit everything else. |
| `relations` | array<string> |  | Relations to test; omit for intersect, connected, inside, contains, touching |
| `maxDistance` | number |  | mm — search radius of the near relation (default 100) |
| `tolerance` | object |  | Overrides of the geometry tolerances in millimetres (angles in degrees); omitted members keep the defaults shown. |
| `ruleSet` | string |  | Classification rule set used to name the ends (default / user / file name) |
| `limit` | integer | 100 | Maximum relationships returned (count tells the total that hold) |
| `maxCandidates` | integer | 5000 | Cap on entities read per set |

## Structural

### `structural_column_alignment_check` — Check column alignment to the grid

*readOnlyHint.* Read-only. Each column centre is matched to the nearest grid intersection (structural_detect_grids on the same rule set) within searchRadiusMm: farther than alignmentToleranceMm is column_off_grid (warning, valueMm = offset, dx/dy in the description, handles = column + the two grid lines); no intersection within reach is column_no_grid. Ids STR-ALN-nnn. Issues come back as the shared issue objects {issueId, category structural, type, severity, handles, locationMm, valueMm, description, suggestedAction} in a stable order — pass them to create_issue_markup. Read-only; geometry only — it says nothing about structural capacity.

| arg | type | default | description |
|---|---|---|---|
| `filter` | object |  | Which entities to classify (wildcards); empty = model space. Structural layers are recognised by the classification rule set (S-COL, DAM, S-GRID, TRUC…) |
| `ruleSet` | string |  | Classification rule set: default \| user \| <file name in the rules folder> |
| `tolerance` | object |  | Geometry tolerances in mm / degrees (pointEquality, endpointConnection, collinearity, parallelAngle, duplicate, tinySegment, roomGap) |
| `alignmentToleranceMm` | number | 25 | Offset a column centre may have from its intersection |
| `searchRadiusMm` | number | 2000 | How far to look for the column's intersection |
| `limit` | integer | 100 |  |
| `offset` | integer | 0 |  |
| `maxCandidates` | integer | 5000 |  |

### `structural_detect_grids` — Detect structural grids

*readOnlyHint.* Read-only. Finds the grid: open runs on grid layers (classified StructuralGrid — S-GRID, A-GRID, GRID, TRUC, AXIS… per the rule set) are grid lines (collinear pieces such as the stub carrying a bubble merge into one line; a jogged polyline follows its longest segment; XLINE/RAY grids have no extent and are only counted), circles / block references on those layers are bubbles, and the label is a short TEXT inside the bubble or a block bubble's attribute. A line takes the bubble sitting on its axis nearest either end (within reachMm); with no bubble, a short TEXT within 50 mm of its midpoint. Returns lines {handle, label, direction horizontal|vertical|skewed, startMm, endMm, lengthMm, directionDeg, bubbleHandle, mergedSegments} and, in the summary, labels, spacingMm per direction, and intersections [{a, b, pointMm, handleA, handleB}] (lines extended by reachMm so lines that stop at the bubble still cross; first 200 listed, intersectionCount exact). Millimetres. Returns the analysis envelope (max 100 lines per page, offset to page).

| arg | type | default | description |
|---|---|---|---|
| `filter` | object |  | Which entities to classify (wildcards); empty = model space. Structural layers are recognised by the classification rule set (S-COL, DAM, S-GRID, TRUC…) |
| `ruleSet` | string |  | Classification rule set: default \| user \| <file name in the rules folder> |
| `tolerance` | object |  | Geometry tolerances in mm / degrees (pointEquality, endpointConnection, collinearity, parallelAngle, duplicate, tinySegment, roomGap) |
| `reachMm` | number | 2500 | How far past a line's end a bubble or a crossing line may sit (0 = default 2500) |
| `limit` | integer | 100 |  |
| `maxCandidates` | integer | 5000 |  |
| `offset` | integer | 0 |  |

### `structural_detect_members` — Detect structural members

*readOnlyHint.* Read-only. Classifies the structural discipline (rule set) and returns members with their sizes: kind column|beam|wall|slab|opening, section (width×depth or Ø of a footprint; L <length> for a beam or wall however it is drawn), widthMm, depthMm, lengthMm, areaMm2, orientationDeg, centerMm, boundsMm (a block's symbol without its attributes), axisMm (runs), confidence, and the existing mark: mark + markHandle + markSource — a block reference's own MARK attribute (markSource attribute), else a mark-shaped TEXT (C1, B12, KC-3) on or within 300 mm of the member whose letters are a known prefix (defaults C, B, W, S, O or prefixes) (markSource text); every text belongs to one member only — the one containing it, else the nearest. summary counts by kind, the most common sections, marked members and duplicateExisting marks. Millimetres. Returns the analysis envelope (items ~470 B, max 100 per page).

| arg | type | default | description |
|---|---|---|---|
| `filter` | object |  | Which entities to classify (wildcards); empty = model space. Structural layers are recognised by the classification rule set (S-COL, DAM, S-GRID, TRUC…) |
| `kinds` | array<string> |  | Member kinds; empty = all |
| `ruleSet` | string |  | Classification rule set: default \| user \| <file name in the rules folder> |
| `prefixes` | object |  | {column, beam, wall, slab, opening} → mark prefix (defaults C, B, W, S, O); a TEXT beside a member counts as its mark only when its letters are one of these |
| `minConfidence` | number | 0 |  |
| `limit` | integer | 100 |  |
| `offset` | integer | 0 |  |
| `maxCandidates` | integer | 5000 |  |

### `structural_generate_member_schedule` — Generate a member schedule

*destructiveHint.* Groups the classified members by kind and section and returns schedule rows {kind, section, count, totalLengthMm, totalAreaMm2, marks, handles (first 20)} in the analysis envelope (kinds column → beam → wall → slab → opening, biggest groups first; marks as structural_detect_members finds them — MARK attributes and mark texts whose letters are a default or given prefix). With writeTable true it also draws the schedule as an AutoCAD Table (title row, header Kind | Section | Count | Total length (m) | Marks — first 20 marks per cell, one row per line) at insertPoint {x, y} mm on layer (default: current layer), rowHeightMm / columnWidthMm / textHeightMm in mm, in space (default: the space the members were read from), and returns the edit envelope with the table handle and the rows in summary.schedule; no members = ArgumentException, nothing drawn. Side effects: only with writeTable — adds one ACAD_TABLE inside one undo entry; dryRun rolls it back; nothing is modified otherwise. With changeSetId the call is recorded into that change set instead of applied (see begin_change_set).

| arg | type | default | description |
|---|---|---|---|
| `filter` | object |  | Which entities to classify (wildcards); empty = model space. Structural layers are recognised by the classification rule set (S-COL, DAM, S-GRID, TRUC…) |
| `kinds` | array<string> |  | Member kinds; empty = all |
| `ruleSet` | string |  | Classification rule set: default \| user \| <file name in the rules folder> |
| `prefixes` | object |  | {column, beam, wall, slab, opening} → mark prefix (defaults C, B, W, S, O); a TEXT beside a member counts as its mark only when its letters are one of these |
| `writeTable` | boolean | false |  |
| `insertPoint` | object |  | writeTable: top-left corner of the table, mm |
| `title` | string | "MEMBER SCHEDULE" |  |
| `layer` | string |  |  |
| `rowHeightMm` | number | 400 |  |
| `columnWidthMm` | number | 3000 |  |
| `textHeightMm` | number | 200 |  |
| `space` | string |  | Where the table goes: model or a layout name; default = the space the members were read from (filter.space, model unless given) |
| `changeSetId` | string |  | Record this call into the change set (begin_change_set) instead of applying it: the handles it names are checked now, nothing is written until commit_change_set |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

### `structural_member_connectivity_check` — Check beam-to-support connectivity

*readOnlyHint.* Read-only. Each beam end must land on a support (column, wall or another beam) within tolerance.endpointConnection (10 mm): a gap up to 3× the tolerance is gap_to_support (warning, valueMm = gap), farther is unsupported_end (critical), both ends free is beam_without_supports (critical). Duplicates of the same beam on the same axis do not count as supports. Ids STR-CON-nnn. Issues come back as the shared issue objects {issueId, category structural, type, severity, handles, locationMm, valueMm, description, suggestedAction} in a stable order — pass them to create_issue_markup. Read-only; geometry only — it says nothing about structural capacity.

| arg | type | default | description |
|---|---|---|---|
| `filter` | object |  | Which entities to classify (wildcards); empty = model space. Structural layers are recognised by the classification rule set (S-COL, DAM, S-GRID, TRUC…) |
| `ruleSet` | string |  | Classification rule set: default \| user \| <file name in the rules folder> |
| `tolerance` | object |  | Geometry tolerances in mm / degrees (pointEquality, endpointConnection, collinearity, parallelAngle, duplicate, tinySegment, roomGap) |
| `limit` | integer | 100 |  |
| `offset` | integer | 0 |  |
| `maxCandidates` | integer | 5000 |  |

### `structural_opening_conflict_check` — Check openings against columns and hosts

*readOnlyHint.* Read-only. Openings (StructuralOpening outlines) against the members around them: opening_through_column (critical) when the outline intersects a column footprint, opening_near_column (warning, valueMm = distance) when closer than clearanceMm to a column face, opening_outside_host (warning) when the opening lies inside no slab or wall outline (only when hosts exist). Ids STR-OPN-nnn. Issues come back as the shared issue objects {issueId, category structural, type, severity, handles, locationMm, valueMm, description, suggestedAction} in a stable order — pass them to create_issue_markup. Read-only; geometry only — it says nothing about structural capacity.

| arg | type | default | description |
|---|---|---|---|
| `filter` | object |  | Which entities to classify (wildcards); empty = model space. Structural layers are recognised by the classification rule set (S-COL, DAM, S-GRID, TRUC…) |
| `ruleSet` | string |  | Classification rule set: default \| user \| <file name in the rules folder> |
| `tolerance` | object |  | Geometry tolerances in mm / degrees (pointEquality, endpointConnection, collinearity, parallelAngle, duplicate, tinySegment, roomGap) |
| `clearanceMm` | number | 300 | Minimum clear distance from an opening to a column face |
| `limit` | integer | 100 |  |
| `offset` | integer | 0 |  |
| `maxCandidates` | integer | 5000 |  |

### `structural_tag_members` — Tag structural members

*destructiveHint.* Numbers the classified members (max 120 per call — tag one kind or a filter at a time, start continues the numbering) and writes each mark where the member keeps it: a block reference's MARK attribute when it has one, the member's existing mark TEXT edited in place when it is renumbered, else a new middle-centred TEXT at the member's centre on the tag layer (default S-ANNO-TEXT, created if missing; locked/frozen refused, off warned) in space (default: the space the members were read from). Marks are {prefix}{n}: prefixes per kind (defaults C, B, W, S, O — override with prefixes {column: "KC"}), from start, padded to digits, in sortBy order: row (top-left to bottom-right, rows 250 mm apart, default), column, handle. Existing marks (a MARK attribute, or a mark text on / within 300 mm whose letters are a known prefix) are respected unless overwrite is true: same prefix → kept_existing and its number reserved (C01 reserves 1 whatever the padding); another prefix → kept_foreign, untouched; duplicates are warned. overwrite true renumbers everything in place. apply: false only decides (summary.marks lists every member, mark, outcome — assigned | kept_existing | kept_foreign | overwritten — and markHandle) and writes nothing. Returns the edit envelope {success, createdCount (new texts), modifiedCount (attributes + texts edited in place), items[{index, ok, handle, type, changed [mark:C1, for:<member> | was:<old>]}], summary {assigned, keptExisting, keptForeign, overwritten, byKind, marks, written [{handle, mark, via attribute|text|newText, textHandle}]}, warnings, errors}. Two-phase: nothing is written unless every mark can be (a mark text on a locked layer refuses the whole call). Side effects: adds/edits text entities (and possibly a layer) inside one undo entry 'MCP: <label>'; dryRun on the request rolls it back; member geometry is never modified. With changeSetId the call is recorded into that change set instead of applied (see begin_change_set).

| arg | type | default | description |
|---|---|---|---|
| `filter` | object |  | Which entities to classify (wildcards); empty = model space. Structural layers are recognised by the classification rule set (S-COL, DAM, S-GRID, TRUC…) |
| `kinds` | array<string> |  | Member kinds; empty = all |
| `ruleSet` | string |  | Classification rule set: default \| user \| <file name in the rules folder> |
| `prefixes` | object |  | {column, beam, wall, slab, opening} → mark prefix (defaults C, B, W, S, O); a TEXT beside a member counts as its mark only when its letters are one of these |
| `start` | integer | 1 |  |
| `digits` | integer | 1 | Zero-padding width (2 → C01) |
| `sortBy` | string row|column|handle | "row" |  |
| `overwrite` | boolean | false | Renumber members that already carry a mark (attribute or text), editing the mark in place |
| `layer` | string | "S-ANNO-TEXT" |  |
| `textHeightMm` | number | 200 |  |
| `space` | string |  | Where new mark texts go: model or a layout name; default = the space the members were read from (filter.space, model unless given) |
| `apply` | boolean | true | false = decide only, write nothing |
| `changeSetId` | string |  | Record this call into the change set (begin_change_set) instead of applying it: the handles it names are checked now, nothing is written until commit_change_set |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

