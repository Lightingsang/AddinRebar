# Phase E — Structural: implementation + live verification report (2026-09-17)

## Implemented

| Piece | Files | Notes |
|---|---|---|
| Members (pure) | `HPAutoCad.Aec/Structural/StructuralMember.cs` — `MemberKind` (column, beam, wall, slab, opening; `Rank`, `DefaultPrefix` C/B/W/S/O, `Supports`, `IsRun`), `StructuralMember` from a classified `AecObject` + `ShapeMetrics`: centre, bounds (a block's symbol without its attributes), width × depth for footprints, `L <length>` for beams/walls however drawn, `Ø` for round columns, axis only for runs or footprints with aspect ≥ 1.5, mark from the block's own `MARK` attribute (`MarkHandle` = itself, `MarkSource` attribute) | no capacity / material claims |
| Grids (pure) | `Structural/GridDetector.cs` — open runs on grid layers are lines (collinear pieces merge: the stub carrying a bubble is not a second line; a jogged polyline follows its longest segment; XLINE/RAY counted as `unbounded`), circles / INSERTs are bubbles, label = short TEXT in the bubble or a block bubble's attribute; a line takes the bubble **on its axis** nearest either end within `BubbleReachMm` 2500, else a TEXT within `LabelOnLineReachMm` 50 of its midpoint; intersections of differing directions with segments extended by the reach, de-duplicated; spacing per direction | `GridLine.MergedSegments`, `GridSystem.UnboundedCount` |
| Checks (pure) | `Structural/StructuralChecks.cs` — Connectivity (`gap_to_support` ≤ 3 × endpointConnection warning, `unsupported_end` critical, `beam_without_supports`; a parallel beam within the landing tolerance of the end is the same beam drawn twice, never a support), ColumnAlignment (nearest intersection within `GridSearchRadiusMm` 2000, `column_off_grid` beyond `DefaultAlignmentToleranceMm` 25, `column_no_grid`; no intersections at all → no issues + one warning), OpeningConflicts (`opening_through_column` only when interiors overlap — sharing a face/corner is `opening_near_column` at 0 mm; `opening_near_column` < clearance 300; `opening_outside_host` when hosts exist) | ids `STR-CON-nnn`, `STR-ALN-nnn`, `STR-OPN-nnn` after `AuditIssue.Ordered` |
| Tagging (pure) | `Structural/MemberTagging.cs` — reading order by gap-clustered bands (`DefaultRowBandMm` 250: a row on y = 5 125 never splits), `Assign` per kind from `start` padded to `digits`; existing marks parsed as letters + optional hyphen + number: same prefix → `kept_existing` and the **number** reserved (C01 reserves 1), another prefix → `kept_foreign` untouched, `overwrite` renumbers everything; `DuplicateExisting`; `Schedule` rows by kind + section | `MarkPattern` `^[A-Za-z]{1,3}-?\d{1,4}$` |
| Read adapter | `Cad/StructuralService.cs` — one classification pass (Structural discipline + text index), members + grid; `ClaimMarks`: a mark-shaped TEXT within `MarkReachMm` 300 whose letters are a default or given prefix belongs to exactly one member (containing it, else its kind's prefix, else nearest); `ParsePrefixes` (unknown kind / duplicate key = ArgumentException); `Describe` (+ `markHandle`, `markSource`) | |
| Write adapter | `Cad/StructuralWriteService.cs` + `.Table.cs` — `WriteTags` two-phase: phase 1 opens the write targets for read (a block's MARK attribute via `BlockService.ValidateAttributes`, the mark TEXT to edit in place, a member on a locked layer still gets a text beside it), atomic refusal on any error, no phase 2 when nothing is pending; phase 2 attributes + in-place texts first (modified), new middle-centred `DBText` at the centre on `S-ANNO-TEXT` created on demand (locked/frozen refused, off warned); `WriteTable`: args, `SpaceId`, layer validated before the space is opened for write, `Table` API, marks cell capped at 20, "-" for footprints' length, zero rows refused | |
| Facade + seeds | `AecTools.Structural.cs` (`MaxGridLimit` 100 + `offset`, `MaxIntersectionsListed` 200, `MaxMemberLimit` 100, `MaxTagMembers` 120, `PositiveOrDefault` — negative args are the caller's error, `WriteSpace` — marks/tables go to the members' space, `all` needs an explicit space); seeds `Structural/structural_detect_grids`, `structural_detect_members`, `structural_member_connectivity_check`, `structural_column_alignment_check`, `structural_opening_conflict_check` (`none`), `structural_tag_members`, `structural_generate_member_schedule` (`auto`); category Structural | server 47 tools (4 + 8 + 35 seeds) |
| Reader | `Cad/EntityShapeReader` — an INSERT with attributes gets its stand-in footprint from the block definition's extents (attribute definitions excluded, cached per definition, transformed by the reference), so a column block with its label beside it measures 400×400, not 400×900; `BoundsMm` (the reference's extents) unchanged for `query_entities` | |
| Tests | `StructuralTests` (10) + `StructuralReviewTests` (9: stub merge / attribute label / corner bubble, jogged polyline + stable ids, near-duplicate beam + no grid, face/corner openings, attribute mark / Ø / closed beam, numeric reservation / foreign / duplicates, band boundary, envelope size at `MaxGridLimit` and at `MaxMemberLimit` / `MaxTagMembers`) → `HPAutoCad.Aec.Tests` 179; `SeedLibraryTests` 35 seeds, caps pinned (`MaxGridLimit`, `MaxMemberLimit`, `MaxIssueLimit`) → `HPAutoCad.Mcp.Server.Tests` 193 | |
| Harness | `aec-tools-live.py` step N (5 checks; scene + grid A/B × 1/2 with a stub on A, bubble 2 as a block with an attribute, a slab, two openings, a `COL-400` block with its `MARK` attribute 600 mm to the right → 43 entities) and `aec-edit-tools-live.py` step M (8 checks) | |

## Build / tests

| Check | Result |
|---|---|
| `dotnet build HPAutoCad.slnx -c Debug` | 0 warnings; bundle deployed |
| `HPAutoCad.Aec.Tests` | 179/179 |
| `HPAutoCad.Mcp.Server.Tests` | 193/193 (35 seeds) |

## Live (AutoCAD 2026, 2026-09-17)

`run-aec-tools-live.ps1` **68/68** · `run-aec-edit-tools-live.ps1` **78/78**.

| Step | Verified |
|---|---|
| N grids | 4 lines labelled A, B, 1, 2 — A's stub merged into it (`mergedSegments` 2, `summary.merged` 1), 2 read from the block bubble's attribute (`bubbleHandle` = the INSERT), 4 intersections, spacing 5010 / 6000 |
| N members | 5 columns (4 outlines 400×400 + the `COL-400` block 400×400 despite its attribute, `mark` C9 / `markSource` attribute / `markHandle` = the block), 3 beams L 5600 / 5593 with axes, 1 slab, 2 openings |
| N checks | connectivity clean at 10 mm, `gap_to_support` 7 mm at 5 mm (`STR-CON-001`); alignment: the 4 drawn columns on grid, the block column 24 m away `column_no_grid`, at 5 mm c3 + c4 10 mm off grid B with the column + 2 grid-line handles; `open1` through c1 critical, `open2` outside the slab |
| M tagging | preview: the beam's stray "C3" text is its foreign mark (`kept_foreign`, `markHandle` = that text), both columns assigned, nothing written; apply digits 2 → C01 + C02 new texts on the created `S-ANNO-TEXT`, C3 untouched; `overwrite: true` + `prefixes {column: KC}` → 3 texts **modified**, 0 created: C01/C02 → KC01/KC02 and the beam's C3 text → B01, same handles; again → `kept_existing` 3; under the defaults KC is not a mark prefix → columns unmarked (assigned), beam `kept_existing` — a door tag never becomes a member mark |
| M schedule | `writeTable` on a locked layer → `LAYER_LOCKED` refusal, nothing created, entity count unchanged; rows column 400×400 (KC mark) + beam L 6000 (B01); `writeTable` → one ACAD_TABLE on `S-ANNO-TEXT`, rows in the summary |

## Review round (2026-09-17, `plans/reports/code-review-2026-09-17-aec-phase-e.md`, 5/10 → fixed)

| # | Finding | Fix | Pinned by |
|---|---|---|---|
| H1 | `structural_tag_members` had no member cap: 300 members = 74 KB while the texts stay committed | `MaxTagMembers` 120 → ArgumentException naming the kind/filter/`start` way out; size test with 120 overwritten tags + 50 duplicate warnings | `A_full_page_of_members_and_a_full_tagging_summary_stay_under_the_result_cap` |
| H2 | `structural_detect_grids` `limit` 500 = 129 KB, no `offset`, maximum unpinned | `MaxGridLimit` 100 + `offset`, `MaxIntersectionsListed` 200, schema maximum pinned in `SeedLibraryTests` | `A_full_page_of_grid_lines_with_the_listed_intersections_stays_under_the_result_cap` |
| H3 | a block member's `MARK` attribute value was never its existing mark → rewritten under `overwrite: false` | the attribute is the mark (`MarkSource` attribute, `MarkHandle` = the block) | live N `colblk` mark C9; `A_block_members_MARK_attribute_is_its_mark…` |
| H4 | "overwritten" wrote a second TEXT and left the old one | the existing mark TEXT is edited in place (`modified`, `was:<old>`); marks with another prefix are `kept_foreign` under `overwrite: false` | live M overwrite (3 modified, 0 created, same handles) |
| H5 | block bubbles with attribute labels got no label; a corner bubble of the crossing grid within reach of an end labelled the line | attribute label; the bubble must sit on the line's axis | `A_bubble_stub_merges…`; live N bubble 2 |
| M1 | the stub joining a bubble to its line was a second line (duplicate intersections, 0 spacing) | collinear runs merge (`MergedSegments`), intersections de-duplicated | same test; live N `merged` 1 |
| M2 | no grid → `column_no_grid` per column | no intersections → no issues + one warning | `…no_grid_means_no_alignment_issues` |
| M3 | an opening sharing a face/corner was "through" (critical) | `CutsInto` = interiors overlap; touching = `opening_near_column` at 0 mm | `An_opening_sharing_a_column_face_or_corner…` |
| M4 | a beam copied 2 mm beside the first supported it | a parallel beam passing within the landing tolerance of the end is never a support | `A_beam_copied_2_mm_beside_the_first_supports_nothing…` |
| M5 | numbers reserved as strings (`C01` + digits 1 → `C1` assigned); duplicate existing marks silent | `(prefix, number)` reservation via `MarkPattern`; `DuplicateExisting` warned in the tag summary and listed by detect/schedule | `Numbers_are_reserved_as_numbers…` |
| M6 | rounding bands split a row at odd multiples of 125 mm; `RoomGap × 10` | gap clustering, `DefaultRowBandMm` 250 | `A_row_whose_centres_straddle_a_band_boundary…` |
| M7 | marks/table went to the *current* space while members are read from model | `WriteSpace`: the members' space by default, `all` needs an explicit space | seed descriptions |
| M8 | `WriteTable` opened the space for write before validation; `WriteTags` opened space + layer table with nothing to write | `SpaceId` in phase 1, `ForWrite` in phase 2; phase 2 skipped when nothing is pending | live M locked-layer table refusal, entity count unchanged |
| M9 | any mark-shaped text within 300 mm of a member's bounds was that member's mark (a column's C1 became its beams' mark; `D01` door tags qualified) | letters must be a default or given prefix; one owner per text (containing member, else its kind's prefix, else nearest) | live M (C3 owned by one member); harness N `colblk` |
| M10 | INSERT stand-in = extents incl. attributes (400×900); round column `400×400` | symbol-only extents from the definition; `Ø400` | live N `colblk` 400×400; `…a_round_column_reads_diameter…` |
| L2, L3, L4, L8, L9, L10, L11, L12, L13 | jogged polyline as one skewed line; unnamed `× 2`; XLINE silently dropped; duplicate `prefixes` keys raw exception, negatives silently defaulted; empty table drawn, `0.00` for footprints, unbounded marks cell; no grid `offset`, truncated scans silent; four constants from `RoomGap`; closed beam outline `300×6000`; `Upgrade` failure ignored, OFF layer silent | longest segment; `LabelOnLineReachMm`; `unbounded` count + warning; `ParsePrefixes` + `PositiveOrDefault`; zero rows refused, "-", 20 marks per cell; `offset` + truncation warnings; `DefaultAlignmentToleranceMm`, `DefaultRowBandMm`, `MinBubbleLabelRadiusMm`; `L 6000`; `InvalidOperationException` (bridge aborts) + off warning | `StructuralReviewTests`, seeds |

Kept as documented: L1 (`MaxMemberLimit` 100 now — measured 47 KB at 100), L5 (a wall drawn as a centreline supports only ends on the line), L6 (a free beam is three issues: both ends + the beam), L7 (run tags sit on the line at mid-span).

Decisions (recommended by the review, taken): H4 — the existing mark is edited in place (one mark per member, the drafter's placement kept); M9 — a text is a mark only when its letters are a default or given prefix, and it belongs to one member.

## Known limitations (phase E)

- Everything is plan geometry: no capacity, no material, no level — a "support" is a footprint or run the beam end lands on.
- Grid lines drawn as XLINE/RAY are counted but not reported (no extent to intersect).
- Walls drawn as single centrelines support only beam ends that reach the line; a beam stopping at the wall face reads as a gap or an unsupported end.
- `structural_tag_members` places run tags at mid-span on the line; a slab tag sits at the centroid, which may fall outside an L-shaped slab.
- Members read from every space (`filter.space: all` / handles) need an explicit `space` for tags and tables.
