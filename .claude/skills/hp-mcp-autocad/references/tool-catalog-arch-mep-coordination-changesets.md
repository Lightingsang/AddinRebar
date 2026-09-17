# HPAutoCad MCP — tool catalog: architecture, MEP, coordination and change sets

Generated from `tools/list` of `HPAutoCad.Mcp.Server.exe` (62 tools in all) on an isolated registry — the surface a fresh install shows. Names are `mcp__hprebar-autocad__<name>` in Claude Code. `REQ` = required; every length is millimetres, points are `{x, y}` objects in mm; handles are hex strings. Seed descriptions end with `[Registry tool v1, <Category>, transaction=…]`; that suffix is stripped here.

## Architecture

### `arch_auto_dimension_plan` — Auto-dimension a plan by rules

*destructiveHint.* Draws dimensions from rules given as data. subject rooms (default) dimensions the rooms arch_detect_rooms finds with the same filter / tolerance / detection (roomIds narrows); subject entities dimensions the closed outlines the filter matches (handles must resolve, layers, types). rules [{rule, ...options}] — known rules: overall (the axis-aligned bounding box of each outline: aligned dimensions offsetMm (600) outside it on sides bottom|top|left|right, default bottom + right). Every dimension is an ALIGNED dimension on layer (default: current layer; locked refused) with dimStyle (default: current), in space (default: the space the subjects were read from). apply: false plans only (summary.dimensions lists rule, subject, side, p1Mm, p2Mm, dimLineMm, measurementMm) and writes nothing; after a write the items carry [rule:subject:side, N mm]. Max 120 dimensions per call. Returns the edit envelope {success, createdCount, items[{index, ok, handle, type DIMENSION, changed}], summary {planned, drawn, layer, dimStyle, byRule, bySubject, dimensions (preview only)}, warnings, errors}. Side effects: adds dimension entities inside one undo entry 'MCP: <label>'; dryRun on the request rolls it back; the outlines are never modified. An unknown rule or side is an ArgumentException. With changeSetId the call is recorded into that change set instead of applied (see begin_change_set).

| arg | type | default | description |
|---|---|---|---|
| `filter` | object |  | Which entities to classify (wildcards); empty = model space. Wall and room layers are recognised by the classification rule set (A-WALL, WALL, TUONG…, A-ROOM, PHONG…) |
| `ruleSet` | string |  | Classification rule set: default \| user \| <file name in the rules folder> |
| `tolerance` | object |  | Geometry tolerances in mm / degrees (pointEquality, endpointConnection, collinearity, parallelAngle, duplicate, tinySegment, roomGap — roomGap is the gap the loop finder closes) |
| `detection` | object |  | How rooms are found (same block on every arch_* tool so room ids agree): maxGapMm 300 (a free wall end farther from every wall is open_boundary, nearer is boundary_gap), minOpeningMm 600 / maxOpeningMm 2500 (facing wall ends or jamb lines that far apart are a doorway, bridged), minAreaMm2 500000 (smaller faces are not rooms), minWidthMm 450 (thinner faces are wall cavities), maxWallThicknessMm 500 (a loop hugging the loop around it that closely is the inner line of a double wall); 0 = default |
| `subject` | string rooms|entities | "rooms" |  |
| `roomIds` | array<string> |  | Room ids (R-001…) or wall/outline handles from arch_detect_rooms; empty = every room |
| `rules` REQ | array<object> |  | Dimension rules, in order; at least one |
| `layer` | string |  | Existing layer for the dimensions (default: current layer) |
| `dimStyle` | string |  | Existing dimension style (default: current) |
| `space` | string |  | Where the entities go: model or a layout name; default = the space the rooms were read from (filter.space, model unless given) |
| `apply` | boolean | true |  |
| `maxCandidates` | integer | 5000 | Entities scanned (walls and room outlines when the filter names no layers/types) |
| `changeSetId` | string |  | Record this call into the change set (begin_change_set) instead of applying it: the handles it names are checked now, nothing is written until commit_change_set |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

### `arch_create_room_tags` — Create room tags

*destructiveHint.* Tags rooms (detected as arch_detect_rooms does with the same filter / tolerance / labels / detection, so R-nnn ids agree; roomIds narrows — an id or a wall/outline handle, a party wall selects both rooms; onlyUnlabelled skips rooms that already carry a name or number) with one middle-centred MTEXT per room at its labelPointMm, rendered from format with placeholders {name} {number} {department} {id} {areaM2} {areaMm2} {perimeterMm} (\P = new line; blank lines are dropped; default "{name}\P{areaM2} m²"), on layer (default A-ANNO-ROOM, created if missing; locked/frozen refused, off warned), textHeightMm (default 250), in space (default: the space the rooms were read from). With blockName, one block reference per room instead, its attributes filled from attributes {TAG: format} rendered the same way — every TAG must exist on the block (checked before anything is written; the preview lists the attribute values). apply: false only decides (summary.tags lists room, name, number, text, atMm) and writes nothing; after a write summary.written lists room, handle, text. Max 120 rooms per call. Returns the edit envelope {success, createdCount, items[{index, ok, handle, type MTEXT|INSERT, changed [room:R-001]}], summary {requested, tagged, layer, layerCreated, blockName, tags | written}, warnings, errors}. Two-phase: every tag renders and every attribute tag resolves before anything is written (an unknown placeholder or attribute refuses the call). The tool's own tags read back on the next run as the room name plus an ignored area line. Side effects: adds MTEXT / INSERT entities (and possibly a layer) inside one undo entry 'MCP: <label>'; dryRun on the request rolls it back; walls and existing texts are never modified. With changeSetId the call is recorded into that change set instead of applied (see begin_change_set).

| arg | type | default | description |
|---|---|---|---|
| `filter` | object |  | Which entities to classify (wildcards); empty = model space. Wall and room layers are recognised by the classification rule set (A-WALL, WALL, TUONG…, A-ROOM, PHONG…) |
| `ruleSet` | string |  | Classification rule set: default \| user \| <file name in the rules folder> |
| `tolerance` | object |  | Geometry tolerances in mm / degrees (pointEquality, endpointConnection, collinearity, parallelAngle, duplicate, tinySegment, roomGap — roomGap is the gap the loop finder closes; keep it below detection.maxGapMm) |
| `labels` | object |  | How texts inside a room are read: numberPattern (regex the room number matches, default ^[A-Z]{0,2}-?\d{1,4}[A-Z]?$), departmentPattern (regex; its first group is the department), ignorePattern (area annotations, default numbers with m2/m²); the longest other text is the name |
| `detection` | object |  | How rooms are found (same block on every arch_* tool so room ids agree): maxGapMm 300 (a free wall end farther from every wall is open_boundary, nearer is boundary_gap), minOpeningMm 600 / maxOpeningMm 2500 (facing wall ends or jamb lines that far apart are a doorway, bridged), minAreaMm2 500000 (smaller faces are not rooms), minWidthMm 450 (thinner faces are wall cavities), maxWallThicknessMm 500 (a loop hugging the loop around it that closely is the inner line of a double wall); 0 = default |
| `roomIds` | array<string> |  | Room ids (R-001…) or wall/outline handles from arch_detect_rooms; empty = every room |
| `onlyUnlabelled` | boolean | false | Tag only rooms with no name or number text |
| `format` | string | "{name}\\P{areaM2} m²" |  |
| `layer` | string | "A-ANNO-ROOM" |  |
| `textHeightMm` | number | 250 |  |
| `blockName` | string |  | Tag block to insert instead of MTEXT (must be defined) |
| `attributes` | object |  | {TAG: format} attribute values for the tag block, same placeholders as format |
| `space` | string |  | Where the entities go: model or a layout name; default = the space the rooms were read from (filter.space, model unless given) |
| `apply` | boolean | true |  |
| `maxCandidates` | integer | 5000 | Entities scanned (walls and room outlines when the filter names no layers/types) |
| `changeSetId` | string |  | Record this call into the change set (begin_change_set) instead of applying it: the handles it names are checked now, nothing is written until commit_change_set |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

### `arch_detect_rooms` — Detect rooms from walls and room outlines

*readOnlyHint.* Read-only. Finds rooms: walls on wall layers (classified ArchitecturalWall — lines and polylines; with no layers/types in the filter the scan is narrowed to the rule set's wall and room layers) are cut at every crossing and T-junction, ends within tolerance.roomGap (25 mm) are closed onto the wall they miss, doorways are bridged (two wall ends facing each other along one line, or two jamb lines facing each other across a double-line wall, detection.minOpeningMm..maxOpeningMm apart — no door objects needed), dangling pieces are set aside (a wall overshooting the corner it crosses is not an open end), and every bounded face of the remaining plan is a room; closed outlines on room layers (classified Room) are rooms as drawn and replace the wall loop they duplicate; an outline holding other outlines is a zone, not a room. Faces below detection.minAreaMm2, thinner than detection.minWidthMm (the cavity between the two lines of a wall) and the outer line of a double-line wall are dropped and counted; an island inside a room (a column, a shaft) is its own face and the room's area stays gross. Each room: id R-nnn, source walls|outline, name / number / department read from the lines of the TEXT/MTEXT inside it by labels (the default number needs a separator after letters — 101, A-101, P.101, 1.01 — so marks like B01 are not numbers), areaMm2, areaM2, perimeterMm, centroidMm, labelPointMm (surely inside), boundsMm, outlineMm (first 32 vertices), handles (first 32 walls, handleCount), textHandles (first 16, textCount). summary: walls, wallSegments, outlines, zones, rooms by source, totalAreaM2, labelled, openEnds, closedGaps, openings, overshoots, cavities, tinyFaces, nested, detection. Millimetres. Returns the analysis envelope (max 30 rooms per page with outlines; includeOutline false for a lighter page).

| arg | type | default | description |
|---|---|---|---|
| `filter` | object |  | Which entities to classify (wildcards); empty = model space. Wall and room layers are recognised by the classification rule set (A-WALL, WALL, TUONG…, A-ROOM, PHONG…) |
| `ruleSet` | string |  | Classification rule set: default \| user \| <file name in the rules folder> |
| `tolerance` | object |  | Geometry tolerances in mm / degrees (pointEquality, endpointConnection, collinearity, parallelAngle, duplicate, tinySegment, roomGap — roomGap is the gap the loop finder closes; keep it below detection.maxGapMm) |
| `labels` | object |  | How texts inside a room are read: numberPattern (regex the room number matches, default ^[A-Z]{0,2}-?\d{1,4}[A-Z]?$), departmentPattern (regex; its first group is the department), ignorePattern (area annotations, default numbers with m2/m²); the longest other text is the name |
| `detection` | object |  | How rooms are found (same block on every arch_* tool so room ids agree): maxGapMm 300 (a free wall end farther from every wall is open_boundary, nearer is boundary_gap), minOpeningMm 600 / maxOpeningMm 2500 (facing wall ends or jamb lines that far apart are a doorway, bridged), minAreaMm2 500000 (smaller faces are not rooms), minWidthMm 450 (thinner faces are wall cavities), maxWallThicknessMm 500 (a loop hugging the loop around it that closely is the inner line of a double wall); 0 = default |
| `includeOutline` | boolean | true |  |
| `limit` | integer | 30 |  |
| `offset` | integer | 0 |  |
| `maxCandidates` | integer | 5000 | Entities scanned (walls and room outlines when the filter names no layers/types) |

### `arch_generate_area_schedule` — Generate an area schedule

*readOnlyHint.* Read-only. Rooms (detected as arch_detect_rooms does with the same filter / tolerance / labels / detection; roomIds narrows) grouped by groupBy — room (one row each, default), name, or department (read by labels.departmentPattern; rooms without one fall under "(none)"; names are compared case-insensitively and shown upper-case) — into rows {group, count, areaMm2, areaM2, percent of the total, rooms (first 20 as "number name", id order)}, largest first. No room standard is built in: departments and names are whatever the texts inside the rooms say. A zone outline holding room outlines is not scheduled (summary.zones). summary: rooms, groups, totalAreaM2, unlabelled, byDepartment, zones. Millimetres. Returns the analysis envelope (max 50 rows per page).

| arg | type | default | description |
|---|---|---|---|
| `filter` | object |  | Which entities to classify (wildcards); empty = model space. Wall and room layers are recognised by the classification rule set (A-WALL, WALL, TUONG…, A-ROOM, PHONG…) |
| `ruleSet` | string |  | Classification rule set: default \| user \| <file name in the rules folder> |
| `tolerance` | object |  | Geometry tolerances in mm / degrees (pointEquality, endpointConnection, collinearity, parallelAngle, duplicate, tinySegment, roomGap — roomGap is the gap the loop finder closes; keep it below detection.maxGapMm) |
| `labels` | object |  | How texts inside a room are read: numberPattern (regex the room number matches, default ^[A-Z]{0,2}-?\d{1,4}[A-Z]?$), departmentPattern (regex; its first group is the department), ignorePattern (area annotations, default numbers with m2/m²); the longest other text is the name |
| `detection` | object |  | How rooms are found (same block on every arch_* tool so room ids agree): maxGapMm 300 (a free wall end farther from every wall is open_boundary, nearer is boundary_gap), minOpeningMm 600 / maxOpeningMm 2500 (facing wall ends or jamb lines that far apart are a doorway, bridged), minAreaMm2 500000 (smaller faces are not rooms), minWidthMm 450 (thinner faces are wall cavities), maxWallThicknessMm 500 (a loop hugging the loop around it that closely is the inner line of a double wall); 0 = default |
| `roomIds` | array<string> |  | Room ids (R-001…) or wall/outline handles from arch_detect_rooms; empty = every room |
| `groupBy` | string room|name|department | "room" |  |
| `limit` | integer | 50 |  |
| `offset` | integer | 0 |  |
| `maxCandidates` | integer | 5000 | Entities scanned (walls and room outlines when the filter names no layers/types) |

### `arch_room_boundary_check` — Check room boundaries

*readOnlyHint.* Read-only. Reports what keeps the walls from reading as rooms: open_boundary (critical — a wall end that reaches nothing, no wall within detection.maxGapMm), boundary_gap (warning — one per gap: a wall end within maxGapMm of another wall but farther than tolerance.roomGap; both handles, the location between the ends, valueMm = the gap), boundary_gap_closed and opening_assumed (info — the gaps the room detection closed and the doorways it bridged, so the drawing itself is still open there), room_overlap (warning — explicit room outlines whose interiors overlap; a shared wall edge or corner is how outlines tile and is not reported), room_inside_room (info — one outline inside another; both are scheduled), duplicate_room (warning), unlabelled_room (info). Issues carry the shared audit fields (issueId ARC-nnn, category architecture, severity critical|warning|info, handles, locationMm, valueMm, description, suggestedAction) in severity order and can be passed to create_issue_markup. Same detection block as arch_detect_rooms. Millimetres. Returns the analysis envelope (max 100 issues per page).

| arg | type | default | description |
|---|---|---|---|
| `filter` | object |  | Which entities to classify (wildcards); empty = model space. Wall and room layers are recognised by the classification rule set (A-WALL, WALL, TUONG…, A-ROOM, PHONG…) |
| `ruleSet` | string |  | Classification rule set: default \| user \| <file name in the rules folder> |
| `tolerance` | object |  | Geometry tolerances in mm / degrees (pointEquality, endpointConnection, collinearity, parallelAngle, duplicate, tinySegment, roomGap — roomGap is the gap the loop finder closes; keep it below detection.maxGapMm) |
| `labels` | object |  | How texts inside a room are read: numberPattern (regex the room number matches, default ^[A-Z]{0,2}-?\d{1,4}[A-Z]?$), departmentPattern (regex; its first group is the department), ignorePattern (area annotations, default numbers with m2/m²); the longest other text is the name |
| `detection` | object |  | How rooms are found (same block on every arch_* tool so room ids agree): maxGapMm 300 (a free wall end farther from every wall is open_boundary, nearer is boundary_gap), minOpeningMm 600 / maxOpeningMm 2500 (facing wall ends or jamb lines that far apart are a doorway, bridged), minAreaMm2 500000 (smaller faces are not rooms), minWidthMm 450 (thinner faces are wall cavities), maxWallThicknessMm 500 (a loop hugging the loop around it that closely is the inner line of a double wall); 0 = default |
| `limit` | integer | 100 |  |
| `offset` | integer | 0 |  |
| `maxCandidates` | integer | 5000 | Entities scanned (walls and room outlines when the filter names no layers/types) |

## MEP

### `mep_connectivity_check` — Check MEP connectivity

*readOnlyHint.* Read-only. Reports what keeps the MEP drawing from reading as connected systems: near_miss (warning — a run end within detection.nearMissMm of a run or node it does not reach; two facing ends are one issue at their midpoint; valueMm = the gap), open_end (warning — a run end connected to nothing), disconnected_run (warning — one issue for a run loose at both ends that nothing touches and nothing is near), orphan_node (warning for equipment / fixtures / terminals no run touches or passes through, info for a lone fitting block; not reported when the drawing has no runs at all), duplicate_run (warning — two runs of one system drawn over each other or a copy a few mm off-axis, the shared length in valueMm), mixed_system (info — runs of several systems joined directly; a node never merges systems). Issues carry the shared audit fields (issueId MEP-nnn, category mep, severity, handles, locationMm, valueMm, description, suggestedAction) in severity order and can be passed to create_issue_markup. Same detection block as mep_detect_network; single-line drafting assumed (double-line ducts are warned). Millimetres. Returns the analysis envelope (max 100 issues per page).

| arg | type | default | description |
|---|---|---|---|
| `filter` | object |  | Which entities to classify (wildcards); empty = model space, narrowed to the rule set's MEP layers (M-PIPE, M-DUCT, E-TRAY, M-EQPM, M-DIFF, ONG, ONGGIO, MANGCAP, THIETBI…) and fitting block names |
| `ruleSet` | string |  | Classification rule set: default \| user \| <file name in the rules folder> |
| `tolerance` | object |  | Geometry tolerances in mm / degrees (pointEquality, endpointConnection — a run end within this of another run's end or body, or of a node entity, is connected —, collinearity, parallelAngle, duplicate, tinySegment, roomGap) |
| `detection` | object |  | Same block on every mep_* tool: nearMissMm 100 (an open end this close to a run or node is a near_miss with the gap; 0 = default; must be above tolerance.endpointConnection), systems {name: [layer wildcards]} — a run whose layer matches is of that system (first declared match wins), else its layer is its system |
| `limit` | integer | 100 |  |
| `offset` | integer | 0 |  |
| `maxCandidates` | integer | 5000 | Entities scanned (MEP layers and fitting blocks when the filter names no layers/types) |

### `mep_detect_network` — Detect MEP networks

*readOnlyHint.* Read-only. Builds the MEP graph from single-line (centreline) drafting: pipes, ducts and cable trays (classified Pipe / Duct / CableTray — open lines, polylines, arcs, splines) are runs; equipment, fixtures, terminals and fittings (Equipment / Fixture / Terminal / Fitting — blocks and closed outlines) are nodes. A run end is connected when it meets another run's end (joined), lands on a run's body — another run's or its own loop — (tee) or touches a node, all within tolerance.endpointConnection (10 mm); a node is also attached to every run passing through it (inline valves, pumps, VAVs). Runs that only cross each other in plan are not connected and are counted as crossings. Runs joined directly form a network N-nnn, longest first; through a node only runs of the same system join (supply and return meet at an AHU without merging) and a node belongs to every network it touches: {id, system (one, or mixed when runs of different systems join directly), systems, runs, runHandles (first 16), byKind, lengthMm, duplicateOverlapMm, nodes, nodeHandles (first 16), byNodeKind, openEnds, openEndsMm (first 8 with the nearest run/node within detection.nearMissMm and the gap), boundsMm}. Systems come from detection.systems, else the layer name. summary: examined, runs, nodes, networks, totalLengthMm, bySystem (first 20, systemsTotal), byKind, mixedNetworks, openEnds, nearMisses, orphanNodes (+ handles), duplicates (collinear or a copy a few mm off-axis, same system), duplicateOverlapMm, crossings, doubleLineDucts (a warning when duct drafting looks double-line), closedRunsIgnored, tinyRunsIgnored. Millimetres. Returns the analysis envelope (max 30 networks per page).

| arg | type | default | description |
|---|---|---|---|
| `filter` | object |  | Which entities to classify (wildcards); empty = model space, narrowed to the rule set's MEP layers (M-PIPE, M-DUCT, E-TRAY, M-EQPM, M-DIFF, ONG, ONGGIO, MANGCAP, THIETBI…) and fitting block names |
| `ruleSet` | string |  | Classification rule set: default \| user \| <file name in the rules folder> |
| `tolerance` | object |  | Geometry tolerances in mm / degrees (pointEquality, endpointConnection — a run end within this of another run's end or body, or of a node entity, is connected —, collinearity, parallelAngle, duplicate, tinySegment, roomGap) |
| `detection` | object |  | Same block on every mep_* tool: nearMissMm 100 (an open end this close to a run or node is a near_miss with the gap; 0 = default; must be above tolerance.endpointConnection), systems {name: [layer wildcards]} — a run whose layer matches is of that system (first declared match wins), else its layer is its system |
| `limit` | integer | 30 |  |
| `offset` | integer | 0 |  |
| `maxCandidates` | integer | 5000 | Entities scanned (MEP layers and fitting blocks when the filter names no layers/types) |

### `mep_endpoint_check` — List MEP run endpoints

*readOnlyHint.* Read-only. Lists run endpoints with their location and connection: {run, kind, layer, system, end start|end, pointMm, state joined|tee|node|open, connectedTo (first 4 handles, connectedCount), nearestHandle + nearestGapMm when open and something is within detection.nearMissMm}. By default only the open ends, near misses first (includeConnected true lists every end). summary: endpoints, byState, open, nearMisses, bySystem (open ends per system). Same detection block as mep_detect_network. Millimetres. Returns the analysis envelope (max 150 endpoints per page).

| arg | type | default | description |
|---|---|---|---|
| `filter` | object |  | Which entities to classify (wildcards); empty = model space, narrowed to the rule set's MEP layers (M-PIPE, M-DUCT, E-TRAY, M-EQPM, M-DIFF, ONG, ONGGIO, MANGCAP, THIETBI…) and fitting block names |
| `ruleSet` | string |  | Classification rule set: default \| user \| <file name in the rules folder> |
| `tolerance` | object |  | Geometry tolerances in mm / degrees (pointEquality, endpointConnection — a run end within this of another run's end or body, or of a node entity, is connected —, collinearity, parallelAngle, duplicate, tinySegment, roomGap) |
| `detection` | object |  | Same block on every mep_* tool: nearMissMm 100 (an open end this close to a run or node is a near_miss with the gap; 0 = default; must be above tolerance.endpointConnection), systems {name: [layer wildcards]} — a run whose layer matches is of that system (first declared match wins), else its layer is its system |
| `includeConnected` | boolean | false |  |
| `limit` | integer | 150 |  |
| `offset` | integer | 0 |  |
| `maxCandidates` | integer | 5000 | Entities scanned (MEP layers and fitting blocks when the filter names no layers/types) |

## Coordination

### `aec_clash_check` — Clash check between two sets in plan

*readOnlyHint.* Read-only. Finds where two sets of classified AEC entities meet in plan and says what the meeting means. hard_clash (critical): an MEP element (pipe, duct, cable tray, equipment…) interpenetrates something it cannot share the plan with — it crosses, overlaps, lies inside or ends inside a column, beam, wall or door/window, or two runs of different services cross; locationMm = the crossing (or a point inside the overlap). clearance_clash (warning): the shapes stay apart but their boundaries are closer than clearanceMm; valueMm = the gap, locationMm = its middle. contact (info): the boundaries meet without interpenetration — a beam end on a column face or at its centre, a pipe teeing into a main (a few mm of overshoot included), equipment on the end of a duct, a door in its wall, any two structural/architectural members meeting: joints, not clashes. area_overlap (info): one lies inside, or crosses the edge of, a Room or StructuralSlab outline — the normal state of a floor. minSeverity (default warning) lists hard and clearance clashes only; contacts and area overlaps are still counted in the summary (contacts, areaOverlaps, belowMinSeverity) and listed with minSeverity info. A set is {filter, aecTypes}: the filter picks entities (wildcards, handles, space), they are classified with the rule set, and aecTypes keeps the types wanted (empty = every classified type). setB {} = check setA against itself (each pair once, never an entity against itself). Subjects pair only within one space (model, or one layout). A set that classifies nothing is warned, not refused. A plan has no heights: a duct crossing a beam in plan is a clash to confirm on the sections, and the description says so; MEP-through-structure pairs point at aec_create_opening_requests. Issues carry the shared audit fields (issueId CL-nnnn, category coordination, type, severity, handles [a, b], locationMm, valueMm, description, suggestedAction, rule 'TypeA×TypeB') severity-first and can be passed to create_issue_markup. Broad phase over bounding boxes grown by the clearance, narrow phase over the exact plan shapes; stops at 200 000 candidate pairs or 200 000 000 segment pairs with a warning and truncated. Millimetres. Returns the analysis envelope (max 80 issues per page; summary: found, listed, belowMinSeverity, hard, clearance, contacts, areaOverlaps, bySeverity, byPair (hard + clearance, top 20), pairsChecked).

| arg | type | default | description |
|---|---|---|---|
| `setA` | object |  | The first set; empty = every classified entity of model space |
| `setB` | object |  | The second set; {} = setA against itself |
| `ruleSet` | string |  | Classification rule set: default \| user \| <file name in the rules folder> |
| `tolerance` | object |  | Geometry tolerances in mm / degrees (pointEquality, endpointConnection, collinearity, parallelAngle, duplicate, tinySegment, roomGap) |
| `clearanceMm` | number | 0 | Required clearance between the two sets' boundaries in plan; 0 = hard clashes only |
| `minSeverity` | string critical|warning|info | "warning" | Lowest severity listed: warning (default) = hard + clearance clashes; info adds contacts and area overlaps |
| `limit` | integer | 80 |  |
| `offset` | integer | 0 |  |
| `maxCandidates` | integer | 5000 | Entities scanned per set |

### `aec_create_opening_requests` — Create opening requests where MEP routes pass through structure

*destructiveHint.* Plans one opening request per place an MEP route (routes: default Pipe, Duct, CableTray) passes through a host (hosts: default ArchitecturalWall, StructuralWall, StructuralBeam — members with a thickness; StructuralSlab or a column may be named in hosts.aecTypes). A host drawn as a line (single-line wall, beam centreline): one request at each point where the route crosses to the other side — a route ending on the line, or touching it and turning back, does not pass. A host drawn as a closed outline: the boundary crossings are sorted along the route and each stretch between two consecutive crossings that runs inside the outline is a pass, opened at the route's middle point inside — a route with a vertex on the face still passes, one starting on the near face and leaving through the far face passes, one ending inside, touching a face or running along it does not; a pass longer than maxChordMm (default 1000: a run drawn inside a wall cavity, a chord across a slab) is not an opening through a member and is counted in summary.longChordsSkipped with a warning. Each request OPN-nnn carries route / host handles and types, centerMm, widthMm × heightMm (sizes: pipeMm 150, ductMm 400, trayMm 300, each + 2 × marginMm 50; absent = default, marginMm 0 is a real value) and angleDeg along the host, and is drawn as a closed rectangle turned along the host plus a multileader '<id>: <routeType> <route> through <hostType> <host> (W×H)' on layer (default HP-MCP-OPENINGS, orange 30, created on demand; locked/frozen refused with LAYER_LOCKED / LAYER_FROZEN, off warned) in space (explicit, else routes.filter.space, else the one space the routes were read from when given by handles; routes spread over several spaces need space). Routes and hosts pair only within one space; the routes and hosts are never modified — a request is a proposal for the structural engineer. Both sets are {filter, aecTypes} classified with the rule set as aec_clash_check does; a set that classifies nothing is warned. apply false previews the plan without drawing (summary.plan.requests lists up to 100 with truncated when there are more); apply true draws up to 100 requests per call and refuses a longer plan. Millimetres. Returns the edit envelope {success, createdCount, affectedHandles, items[{index, ok, handle (rectangle), type, changed [id, leader:<handle>, route:<handle>, host:<handle>]}], summary {routes, hosts, sizes, maxChordMm, longChordsSkipped, plan {requested, drawn, layer, layerCreated, written[…] | listed, truncated, requests[…]}}, warnings, errors}. Side effects: adds entities (and possibly a layer) inside one undo entry 'MCP: <label>'; dryRun on the request rolls it back. With changeSetId the call is recorded into that change set instead of applied (see begin_change_set).

| arg | type | default | description |
|---|---|---|---|
| `routes` | object |  | The MEP routes; empty = every Pipe / Duct / CableTray of model space |
| `hosts` | object |  | The hosts; empty = every wall and beam of model space |
| `ruleSet` | string |  | Classification rule set: default \| user \| <file name in the rules folder> |
| `tolerance` | object |  | Geometry tolerances in mm / degrees (pointEquality, endpointConnection, collinearity, parallelAngle, duplicate, tinySegment, roomGap) |
| `sizes` | object |  | Opening size per route type in mm before the margin on each side; absent or 0 = default (150 / 400 / 300); marginMm absent = 50, 0 = no margin |
| `maxChordMm` | number | 1000 | Longest stretch inside an outline that still counts as passing through a member; longer passes are skipped and counted; 0 = default |
| `layer` | string | "HP-MCP-OPENINGS" |  |
| `textHeightMm` | number | 150 |  |
| `space` | string |  | Where the requests go: model or a layout name; default = the space the routes were read from (routes.filter.space, model unless given) |
| `apply` | boolean | true | false = plan only, nothing drawn |
| `maxCandidates` | integer | 5000 | Entities scanned per set |
| `changeSetId` | string |  | Record this call into the change set (begin_change_set) instead of applying it: the handles it names are checked now, nothing is written until commit_change_set |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

## ChangeSet

### `begin_change_set` — Begin a change set

*readOnlyHint.* Read-only for the drawing. Starts a logical change set and returns its id (CS-nnn). Any write tool called with changeSetId then records its call into the set instead of applying it — the handles it names (handles, items[].handle, issues[].handles, hatch.boundaryHandles, filter.handles) must be entities and its op must be one the tool takes and a rollback can undo (manage_xrefs: attach only), checked at once; everything else when the set is committed. preview_change_set lists what was recorded, commit_change_set replays every op inside one run (one undo entry, atomic by default), rollback_change_set discards a pending set, undoes a committed one (erases what it created, un-erases what it erased, restores what it modified from snapshots taken as the commit ran — up to 2 000 entities) or, with keep, closes it (work kept, undo released). States: pending → committed → rolled_back | closed; pending → discarded; a commit or rollback undone by its request (dryRun, U) steps back on the next call. The store lives in the AutoCAD process per drawing (a set never replays into another document), survives between requests and is dropped when the drawing closes; at most 20 live (pending or committed) sets per drawing — at the cap the oldest committed set is closed to make room —, 200 ops per set. Returns the analysis envelope: items[0] = the set {changeSetId, label, state pending, ops 0, createdAt}, summary {changeSetId, document, byState, maxOps, maxLiveSets, closed (the set closed to make room, if any), writeTools (the tools a set may hold)}.

| arg | type | default | description |
|---|---|---|---|
| `label` | string |  | What the set is for (≤ 120 characters), shown in summaries |

### `commit_change_set` — Commit a change set

*destructiveHint.* Replays every op recorded into a pending change set, in order, inside this one run — one undo entry 'MCP: <label>' for the whole set. Each op runs exactly as the write tool would have (same arguments, same validation, same envelope rules); while it runs, the original state of every entity it opens for write is snapshotted so rollback_change_set can restore it. atomic (default true): an op that does not succeed (an op recorded with atomic false that partly fails counts as not succeeding) is refused as an ArgumentException naming the op — the run is aborted, nothing of the set is written, the set stays pending; atomic false keeps what succeeded and reports the failed ops per item. A set is committed once: commit again is refused; roll it back or close it (rollback_change_set keep) first. Returns the edit envelope {success, createdCount, modifiedCount, deletedCount, affectedHandles (up to 100), items[{index = op − 1, ok, type = tool, changed [op N, created n, modified n, deleted n], error}], summary {changeSetId, ops, failedOps, created, modified, deleted, layersCreated, blocksCreated, snapshots, snapshotsComplete, byTool}, warnings (grouped by message with op indices), errors (first 20 + a count)}. Side effects: everything the recorded ops do, inside one undo entry 'MCP: <label>'; dryRun on the request rolls it all back — the response's rolledBack says so at once and the engine notices on the next call, the set is pending again.

| arg | type | default | description |
|---|---|---|---|
| `changeSetId` | string |  | The change set (CS-nnn) begin_change_set returned |
| `atomic` | boolean | true | true = any failed op aborts the whole run; false = keep the ops that succeeded |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

### `get_change_summary` — List the change sets of the drawing

*readOnlyHint.* Read-only. Lists the change sets of the current drawing (or one by changeSetId): id, label, state (pending | committed | rolled_back | closed | discarded), ops and ops by tool, createdAt / endedAt, a note when something happened outside the normal path (a commit or rollback undone by dryRun / U, a rollback that could not restore every handle), and for a committed set the commit counts (created / modified / deleted handles, layers and block definitions created, whether every modification was snapshotted, whether its undo is still available, failed ops). Summary: document, sets, byState, live sets against the cap of 20. The store lives in the AutoCAD process per drawing (a set never replays into another document), survives between requests and is dropped when the drawing closes; at most 20 live (pending or committed) sets per drawing — at the cap the oldest committed set is closed to make room —, 200 ops per set. Returns the analysis envelope.

| arg | type | default | description |
|---|---|---|---|
| `changeSetId` | string |  | One set only; empty = every set of the drawing |

### `preview_change_set` — Preview a change set

*readOnlyHint.* Read-only. Lists the ops recorded into a change set in the order they will replay: index, tool, a one-line summary (op, item / handle counts), the handles the call names, and its raw arguments cut at 500 characters. The summary carries the set (state, ops by tool, commit counts when committed) and the number of distinct handles named. A committed set whose run turned out to be rolled back by its request (dryRun, U) is reported pending again with a note; a rolled-back set whose undo was rolled back is committed again. For the real effect of a commit without keeping it, call commit_change_set with dryRun true on the request. Paged (max 30 ops per page). Returns the analysis envelope.

| arg | type | default | description |
|---|---|---|---|
| `changeSetId` | string |  | The change set (CS-nnn) begin_change_set returned |
| `limit` | integer | 30 |  |
| `offset` | integer | 0 |  |

### `rollback_change_set` — Roll back or discard a change set

*destructiveHint.* A pending set is discarded (nothing was ever written; the response says so). A committed set is undone in this run: the entities the commit created are erased, the ones it erased are un-erased (same handles — AutoCAD keeps erased objects in the open drawing), and the ones it modified get their original state back from the snapshots taken as the commit ran (same handles, so references stay; dimensions and hatches are recomputed). Layers and block definitions the commit added stay and are listed. A handle that cannot be undone (a layer locked since the commit) is an error on that handle, the rest is restored — and the set then stays committed with its snapshots: fix the cause and roll back again. keep true undoes nothing: the committed work stays and only the undo is released (state closed) — the way to finish a set you want to keep. Returns the edit envelope {success, deletedCount (erased), modifiedCount (restored), createdCount (un-erased), affectedHandles, summary {changeSetId, rolledBack | closed | discarded, erased[], restored[], unerased[], missing[], layersKept, blocksKept, failed, failedByCode}, warnings, errors (first 20 + a count)}. Side effects: the undo, inside one undo entry 'MCP: <label>'; dryRun on the request rolls the undo back — the engine notices on the next call and the set is committed again, its undo still available.

| arg | type | default | description |
|---|---|---|---|
| `changeSetId` | string |  | The change set (CS-nnn) begin_change_set returned |
| `keep` | boolean | false | true = keep the committed work, release its undo (close the set); false = undo it |
| `dryRun` | boolean | false | Run the tool, then roll everything back. Use first on a model you care about. |

