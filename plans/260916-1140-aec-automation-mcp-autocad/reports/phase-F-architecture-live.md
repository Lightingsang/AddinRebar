# Phase F — Architecture: implementation + live verification report (2026-09-17)

## Implemented

| Piece | Files | Notes |
|---|---|---|
| Rooms from walls (pure) | `HPAutoCad.Aec/Architecture/RoomLoopFinder.cs` + `WallGraph.cs` + `OpeningBridger.cs` — wall pieces cut at every crossing / T-junction / collinear overlap, endpoints within `tolerance.roomGap` snapped onto the wall they miss (corners close as corners), doorways bridged by virtual walls (two free ends facing each other along one line, or two jamb lines spanning the two long lines of a double-line wall, `minOpeningMm` 600 … `maxOpeningMm` 2500 apart), dangling chains peeled (a tail ≤ `maxWallThicknessMm` past the junction its own wall crosses is an overshoot, counted), faces walked by leftmost turn (bounded faces CCW); dropped and counted: faces < `minAreaMm2` 0.5 m², thinner than `minWidthMm` 450 (wall cavities), the outer line of a double-line wall (every vertex within the wall thickness of a loop it contains). `LoopSettings` carries every size. | islands (a column, a shaft) are their own faces, the room stays gross; `MaxSegments` 20 000 |
| Room model + labels | `Architecture/Room.cs` — `Room` (id `R-nnn`, source walls|outline, outline, area, perimeter, centroid, `InsidePoint` grid search for L-shapes, name / number / department, text handles), `Describe` (outline ≤ 32 vertices, handles ≤ 32 + `handleCount`, texts ≤ 16 + `textCount`); `RoomLabelRules` from caller regexes (200 ms timeout; default number = 2–4 digits or letters + separator + 3–4 digits — `101`, `A-101`, `P.101`, `1.01`, never `B01` / `C1` / `KT-12`); `RoomLabels.Apply` splits every text into lines (`\P`, newlines) — number = first line matching, department = first match of its pattern (group 1), name = longest remaining line, area annotations ignored | the tool's own MTEXT tags read back as name + ignored area |
| Boundary check (pure) | `Architecture/RoomBoundaryChecks.cs` — `open_boundary` critical (no wall within `maxGapMm` 300), `boundary_gap` warning (one per gap, both handles, location between the ends), `boundary_gap_closed` + `opening_assumed` info, `room_overlap` (interiors only — tiled outlines sharing edges/corners are fine), `room_inside_room` info, `duplicate_room`, `unlabelled_room`; ids `ARC-nnn` after `AuditIssue.Ordered`; invariant-culture numbers | |
| Schedule + dimensions (pure) | `Architecture/AreaSchedule.cs` (rows by room | name | department, upper-cased groups, percentages, ≤ 20 rooms per row in id order), `Architecture/AutoDimensionRules.cs` (rule registry — `IDimensionRule`, rules as data `{rule, …}` — one rule `overall`: aligned dimensions of each outline's bounding box on `sides` at `offsetMm`) | unknown rule / side = ArgumentException |
| Read adapter | `Cad/RoomService.cs` — one classification pass of the Architecture discipline (the query narrowed to the rule set's wall + room layers/types when the filter names none; `filter.space: all` refused), walls → segments, closed `Room` outlines → rooms as drawn (a wall loop inside an outline with ≥ 60 % of its area is that outline drawn as walls; an outline holding other outlines is a zone), labels = texts strictly inside; `Select` by id or handle (a party wall selects both rooms) | `TextsTruncated` / narrowed / truncated scan warned |
| Write adapter | `Cad/RoomWriteService.cs` (+ `.Dimensions.cs`) — `WriteTags` two-phase: layer via `EditContext.CheckAnnotationLayer` / `EnsureLayer` (shared with phases D–E now), block tags validated against the definition's attribute tags before anything is written, every tag rendered (`Render` with `{name} {number} {department} {id} {areaM2} {areaMm2} {perimeterMm}`, blank lines dropped), MTEXT middle-centred at the label point or block + attributes; `WriteDimensions` via `AlignedDimension`, `MaxDimensions` 120; summaries list the plan before the write and only what was written after | |
| Facade + seeds | `AecTools.Architecture.cs` — `ArchDetectRooms` (`MaxRoomLimit` 30, `MaxOutlineVertices` 32, `includeOutline`), `ArchRoomBoundaryCheck` (`MaxIssueLimit`), `ArchCreateRoomTags` (`MaxRoomTags` 120), `ArchGenerateAreaSchedule` (`MaxAreaRowLimit` 50), `ArchAutoDimensionPlan` (subject rooms | entities); one `detection` block (`DetectionKeys`: maxGapMm, minOpeningMm, maxOpeningMm, minAreaMm2, minWidthMm, maxWallThicknessMm) + `tolerance` + `labels` + `maxCandidates` shared by all five so room ids agree across tools; seeds `Architecture/arch_detect_rooms`, `arch_room_boundary_check`, `arch_generate_area_schedule` (`none`), `arch_create_room_tags`, `arch_auto_dimension_plan` (`auto`); category Architecture | server 52 tools (4 + 8 + 40 seeds) |
| Rule set | `Rules/aec-classification.default.json` v3: `a-wall-run` types + ARC, ELLIPSE (a curved wall is a wall) | |
| Tests | `ArchitectureTests` (12: rooms / T-junctions / overshoots, gap closed vs open, L-room / island / double lines / hall + shaft / closet, doorways single- and double-line + a 400 mm gap, overshoot vs tail vs stub, overlaps / duplicates / gaps as one issue, labels + patterns + MTEXT lines, schedule, dimension rules, envelopes at every cap) → `HPAutoCad.Aec.Tests` 191; `SeedLibraryTests` 40 seeds + shared detection block → `HPAutoCad.Mcp.Server.Tests` 222 | |
| Harness | `aec-tools-live.py` step R (7 checks; scene + a two-room plan at (50 000, 0): single-line walls, a 900 door in the party wall and one in the outer wall, `PHONG KHACH` / `101` / `BEDROOM` / `B01` texts, a 250 mm gap in the right room's top wall → 55 entities) and `aec-edit-tools-live.py` step A (11 checks) | |

## Build / tests

| Check | Result |
|---|---|
| `dotnet build HPAutoCad.slnx -c Debug` | 0 warnings; bundle deployed |
| `HPAutoCad.Aec.Tests` | 191/191 |
| `HPAutoCad.Mcp.Server.Tests` | 222/222 (40 seeds) |

## Live (AutoCAD 2026, 2026-09-17)

`run-aec-tools-live.ps1` **75/75** · `run-aec-edit-tools-live.ps1` **89/89**.

| Step | Verified |
|---|---|
| R rooms | 4 rooms from walls: the closed outline (12 m², `OFFICE 01` from the text inside, 4 outline vertices), the 12 mm-open outline closed by roomGap (12 m², unlabelled), the 2 × 1 m rectangle, and `PHONG KHACH` / `101` (20 m²) kept whole by the two 900 mm doorways bridged (`openings` 2, the party wall and the outer wall in its handles); the right room is open (250 mm gap); `B01` is not a number; bow-tie halves too small; total 46 m². `tolerance.roomGap 5` → 3 rooms; `detection.maxOpeningMm 800` → doorways not bridged, 3 rooms; `detection.maxGapMm 700` (above minOpeningMm) → ArgumentException |
| R boundary | 12 `open_boundary` criticals (w1/w2, g1/g2, both arcs — walls now —, the semicircle polyline, the coloured line), **one** `boundary_gap` (250 mm, both handles, located between the ends), 2 `boundary_gap_closed` (12 mm, 7 mm), 2 `opening_assumed`, 2 `unlabelled_room`; `ARC-001` critical first |
| R schedule | by name: `PHONG KHACH` 20 m² (43.5 %, listed as "101 PHONG KHACH"), `OFFICE 01` 12 m², `(none)` 14 m² (2 rooms); total 46 m²; `groupBy: colour` → ArgumentException |
| A tags | preview renders `OFFICE 02 / <area> m²` (the room text renamed by the U step; the `B01` structural mark inside is not a number), nothing written; dryRun rolled back; locked layer → `LAYER_LOCKED`; unknown placeholder → ArgumentException; block tag with an attribute the block lacks → ArgumentException naming its tags, nothing inserted; block preview lists `MARK=OFFICE 02`; apply → one MTEXT on the created `A-ANNO-ROOM` at the room's label point, summary lists `written` not the plan; detect after tagging still reads the room as `OFFICE 02` |
| A dimensions | `overall` plan → 2 dimensions measuring the room's bounds, nothing drawn; unknown rule → ArgumentException listing `overall`; apply → 2 aligned DIMENSION entities with those measurements |

## Review round (2026-09-17, `plans/reports/code-review-2026-09-17-aec-phase-f.md`, 5/10 → fixed)

| # | Finding | Fix | Pinned by |
|---|---|---|---|
| H1 | no notion of an opening: a door in a single-line wall left 0 rooms + criticals; double-line walls with jambs merged both rooms | `OpeningBridger`: facing free ends along one line and facing jamb pairs (spanning two long parallel wall lines ≥ 2 × thickness, nothing between) bridged by virtual walls at `minOpeningMm`..`maxOpeningMm`; reported as `openings` / `opening_assumed` | `Doorways_are_bridged…`; live R (two 900 doors) |
| H2 | tiled outlines sharing an edge fired `room_overlap` on every pair | `Interpenetrate` = proper crossing or a vertex strictly inside; one inside the other = `room_inside_room` info | `Tiled_outlines_are_not_overlaps…` |
| H3 | the nested rule dropped a hall holding a shaft / a room with a closet | outer line of a double wall = every outer vertex within `maxWallThicknessMm` of a contained loop's boundary | `…hall + shaft, closet` cases |
| H4 | `MaxDimensions` 200 = 65 550 B with the dimensions committed | 120; the plan is listed in the preview only, the items carry rule/subject/side/measurement after the write | envelope test with 7-digit coordinates |
| H5 | overshoot heuristic swallowed real gaps and missed angled overshoots | overshoot = a tail ≤ wall thickness past the junction its own piece runs through (any angle); the junction must still be in the graph; a wall running on past a T is a real end | `An_overshoot_is_the_tail…`; live R |
| M1 | reach 12 × roomGap ignored `maxGapMm` | reach = `detection.maxGapMm`; `maxGapMm` must stay below `minOpeningMm` | `…a 400 mm gap` |
| M2 | one gap = two issues | facing open ends paired into one `boundary_gap` at the midpoint | `Tiled_outlines…`; live R |
| M3 | an unclosed polyline's own far end excluded | same-entity pieces count only through their other free end (never a sampled arc's neighbours) | live R (openroom) |
| M4 | positional ids across tools with different inputs | one `detection` block + `tolerance` + `labels` + `maxCandidates` on all five seeds (`SeedLibraryTests.Architecture_seeds_share_the_detection_block…`) | |
| M5 | default number pattern took marks (`B01`, `C1`, `KT-12`) | `^(?:[A-Z]{1,3}[-.\s]\s?\d{3,4}\|\d{2,4})[A-Z]?$\|^\d{1,2}\.\d{2,3}$` | `Labels_inside_a_room…`; live R/A |
| M6 | a two-line MTEXT was one label; the tool's own tags re-read as names | lines are labels; area lines ignored | `Labels…`; live A (detect after tagging) |
| M7 / M8 / M12 | uncapped handles/texts per room, 100 area rows × 20 labels, tags echoed thrice | handles ≤ 32 + count, texts ≤ 16 + count; `MaxAreaRowLimit` 50; `written` only after the write | envelope tests (rooms with 60 handles + 200 texts; 50 rows × 20 × 30 chars; 120 tags) |
| M9 | block attribute keys unvalidated; no block preview | tags checked against `BlockService.AttributeTags` in phase 1; preview lists `TAG=value` | live A |
| M10 | O(loops²) nesting, O(tips × pieces) nearest, no `ct` | `SpatialIndex` in both, `ct` in every loop | — |
| M11 | texts truncation silent; all entities counted against `maxCandidates` | warnings for truncated texts / scan; query narrowed to the rule set's wall + room layers when the filter names none | — |
| L1–L9, L11, L12, L14 | `SameRing` units; 10 % outline match; zone outlines; `outlineMm` when not asked; group spelling; culture; errors after Settle; handle selects one room; 600 mm width floor; bounding box wording; arcs not walls; `space: all` | perimeter-based; 60 %; zones excluded + counted; `null`; upper-invariant + id order; invariant culture; unresolvable handles refuse; every room with the handle; 450; description; ARC/ELLIPSE in the wall rule (v3); refused | tests / seeds / live R |

Left as documented: L10 (a tag block defined in other units is inserted at scale 1, like `insert_block`), L13 (gap closing is one hop: a cascade of snaps beyond `roomGap` is not chased), L15 (plan status — updated with this report).

Decisions (recommended by the review, taken): openings are inferred from the walls alone (facing ends / jamb pairs), no door objects needed; a room holding an island keeps its gross area, a room holding another room (booth in a hall) is kept as well; the default number pattern requires a separator after letters and 3–4 digits.

## Known limitations (phase F)

- Rooms are faces of single- or double-line wall drawings; a doorway wider than `maxOpeningMm` (2.5 m) or a wall missing altogether leaves the room open — the boundary check says where.
- Islands (columns, shafts) inside a room are not subtracted from its area; a shaft drawn on the wall layer is its own room when ≥ `minAreaMm2`.
- Curved walls are sampled polylines: their ends are free ends unless they meet a wall; a wall drawn as an ARC is a wall since rule set v3.
- Gap closing is one hop (an endpoint moves onto the nearest wall once); rooms drawn a few mm apart keep a sliver between them below `minWidthMm` (dropped as a cavity).
- Labels are texts strictly inside the outline; a text straddling a wall belongs to neither room.
- A tag block defined in other drawing units is inserted at scale 1.
