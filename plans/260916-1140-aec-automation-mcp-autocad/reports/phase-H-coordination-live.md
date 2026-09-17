# Phase H — Coordination: implementation + live verification report (2026-09-17)

## Implemented

| Piece | Files | Notes |
|---|---|---|
| Clash pairs (pure) | `HPAutoCad.Aec/Coordination/ClashClassifier.cs` — what two meeting shapes mean: `hard_clash` (critical) = an MEP element interpenetrates a member or another service's run (crosses, overlaps, lies inside, ends inside; a route × route proper crossing whose end is within `endpointConnection` is a tee, not a clash); `contact` (info) = boundaries meet without interpenetration — a beam on a column face or at its centre, a tee, equipment on the end of a run, a door in its wall, any two structural / architectural members meeting (`meets`, `runs along`, `connects to`, `tees into`, `touches`); `area_overlap` (info) = inside or across the edge of a `Room` / `StructuralSlab` outline (`AreaTypes`); `Between` = the closest vertex–foot pair's midpoint for a clearance location | |
| Detector (pure) | `Coordination/ClashDetector.cs` — broad phase `SpatialIndex` over set B grown by the clearance, subjects pair only within one space, same-set pairs once by ordered handle, narrow phase `Evaluate(Intersects)` → classifier, else `DistanceXY(…, upTo: clearance)` → `clearance_clash` (warning, `valueMm` = gap); `ct` per pair; caps `MaxPairs` 200 000 + `MaxSegmentPairs` 200 000 000 (segment work counted before the pair runs); ids `CL-nnnn` after `AuditIssue.Ordered`; `Outcome` counts hard / clearance / contacts / areaOverlaps | |
| Opening planner (pure) | `Coordination/OpeningPlanner.cs` — boundary intersections with `proper: false`, sorted by station along the route; a host outline: the stretch between two consecutive stations whose route midpoint is strictly inside (in the ring and clear of its boundary) is a pass, opened at that midpoint, turned along the host at the entry — a vertex on the face still passes, a route starting on the near face passes, one ending inside / touching / running along a face does not; passes longer than `maxChordMm` (default 1 000) are counted (`LongChordsSkipped`); a host line: every intersection where the route continues on the other side (side test `endpointConnection` before / after; not its own ends, not a touch-and-turn); `OpeningSizes` pipe 150 / duct 400 / tray 300 + 2 × margin 50; `MaxRequests` 100 | |
| Read adapter | `Cad/CoordinationService.cs` — a set = `filter` + `aecTypes` (any discipline, validated against `AecType.All`) classified once; subjects carry their space; points and open chains ≤ `tinySegment` set aside and counted | |
| Write adapter | `Cad/OpeningWriteService.cs` — two-phase: layer (`HP-MCP-OPENINGS`, ACI 30, created on demand; locked/frozen → `Refused`), space, sizes and the whole plan checked before anything is `ForWrite`; rectangle turned along the host + `MLeader` label `<id>: <routeType> <route> through <hostType> <host> (W×H)`; preview lists the first 100 of a longer plan with `truncated`, drawing a longer plan is refused | |
| Facade + seeds | `AecTools.Coordination.cs` — `AecClashCheck` (`MaxClashLimit` 80 per page — an issue is ~520 B —, `minSeverity` default `warning` so contacts / area overlaps are counted not listed, summary found / listed / belowMinSeverity / hard / clearance / contacts / areaOverlaps / bySeverity / byPair; empty sets warned), `AecCreateOpeningRequests` (`DefaultRouteTypes` Pipe / Duct / CableTray, `DefaultHostTypes` ArchitecturalWall / StructuralWall / StructuralBeam — the slab is selectable, never default —, `maxChordMm`, `RouteSpace`: explicit → filter space → the one space the routes live in; `longChordsSkipped` + warning); `SpatialPredicates.DistanceXY(upTo)` culls segment pairs by box separation and, above 4 096 segment pairs, queries a per-shape segment index (cached, `ConditionalWeakTable`); `PlanShape.SegmentBounds`; seeds `Coordination/aec_clash_check` (`none`) and `aec_create_opening_requests` (`auto`); category Coordination | server 57 tools (4 + 8 + 45 seeds) |
| Tests | `CoordinationTests` (5) + `CoordinationReviewTests` (8: a frame against itself = joints; a pipe network = one crossing + tees as contacts; area outlines hold their floor, a pipe into a column is a clash; clearance location in the gap; vertex crossings pass and entry/exit pairs never draw a phantom; a cavity run is skipped and counted; different spaces never pair; stacked curved runs and long polylines under 2 s) → `HPAutoCad.Aec.Tests` 210; `SeedLibraryTests` 45 seeds + the type-list pin → `HPAutoCad.Mcp.Server.Tests` 253 | |
| Harness | `aec-tools-live.py` step W (7 checks), `aec-edit-tools-live.py` step O (8 checks) | |

## Build / tests

| Check | Result |
|---|---|
| `dotnet build HPAutoCad.slnx -c Debug` | 0 warnings; bundle deployed |
| `HPAutoCad.Aec.Tests` | 210/210 |
| `HPAutoCad.Mcp.Server.Tests` | 253/253 (45 seeds, 27 read-only) |
| `run-server-smoke.ps1` | 22/22 |

## Live (AutoCAD 2026, 2026-09-17)

`run-aec-tools-live.ps1` **90/90** · `run-aec-edit-tools-live.ps1` **96/96**.

| Step | Verified |
|---|---|
| W clash | the pipe polyline × {b1, b2, b3, room outline, slab}: 3 hard clashes listed (crosses b1 and b3 at (3000, 0), crosses the room wall at (3000, 1000)), the slab edge crossing counted as an `area_overlap` and listed only with `minSeverity: info` (last, "crosses the edge of" at (3000, −500)); b2 clear; `CL-0001..3`, rule `Pipe×Type`, `byPair`; paging `offset 1 limit 1`; clearance 50 mm: b2 7 mm short of c4 → `clearance_clash` warning located in the gap, b2 on c3 a contact counted (listed with info as "meets"), clearance 5 → nothing; a frame {b1, b3, c1, c2} against itself → 0 clashes, 5 contacts (b1×b3 "runs along"), each pair once, `sameSet`, no `setB`; the pipe set {main, branch, copy, short} with clearance 100 → the copy a hard "overlaps", the 50 mm short branch a clearance clash at (67000, 25), the tee a contact; errors: unknown type / negative clearance / unknown set key / unknown minSeverity → ArgumentException, a set that classifies nothing → empty result + warning |
| O openings | a pipe line across the plan: preview → 2 requests (through the beam at (5000, 1200), the wall line at (5000, 4800)) and the 6.75 m chord across the room outline skipped + counted + warned; `maxChordMm 8000` → 3 requests incl. (5000, 3375); `aec_clash_check` on the same sets → 3 hard clashes, the same pairs; dryRun → 4 entities created then rolled back; locked request layer → `LAYER_LOCKED`; bad size key → ArgumentException, empty hosts → warned, 0 requested; apply → 2 closed 250 × 250 rectangles + 2 multileaders on the created `HP-MCP-OPENINGS`, summary lists both handles per request |

## Review round (2026-09-17, `plans/reports/code-review-2026-09-17-aec-phase-h.md`, 5.5/10 → fixed)

| # | Finding | Fix | Pinned by |
|---|---|---|---|
| H1 | contact = critical hard clash (a frame against itself = 5 criticals, every tee a critical) | `ClashClassifier`: `contact` (info) for boundaries meeting, tees (incl. a few mm of overshoot), equipment on runs, structural / architectural members meeting; hard only when an MEP element interpenetrates | `A_frame_checked_against_itself…`, `A_pipe_network…`; live W |
| H2 | only proper crossings; entry/exit pairing slipped → passes lost, phantom openings | `proper: false` intersections, stations along the route, intervals classified by a strictly-inside route point; host lines by side change | `Crossings_through_a_vertex…` (P5b/d/e, P6, P7c, L7) |
| M1 | "lies inside" a slab / room = critical for every run on the floor | `area_overlap` (info) for `Room` / `StructuralSlab`, counted in the summary | `Area_outlines_hold_their_floor…`; live W |
| M2 | clearance `locationMm` metres from the gap | closest vertex–foot pair | `A_clearance_clash_is_located_in_the_gap…`; live W (67000, 25) |
| M3 | any chord = a pass (cavity runs, slab chords); slab a default host | `maxChordMm` 1 000 + `longChordsSkipped` + warning; slab dropped from the default hosts | `A_run_drawn_inside_a_wall_cavity…`; live O |
| M4 | the pair cap does not bound the work (10 s for 2 025 polyline pairs, 15 s for 100 stacked arcs) | `DistanceXY(upTo)` with box culling, a per-shape segment index above 4 096 segment pairs (also in `IntersectionPoints`), `ct` per pair, `MaxSegmentPairs` | `The_clearance_search_stays_fast…` (< 2 s) |
| M5 | `MepEquipment` documented, per-set "empty = every type" wrong for the opening tool | descriptions list `AecType.All` names; "empty = the tool's default types" | `Coordination_seeds_document_only_real_aec_types…` |
| M6 | subjects from different spaces paired by coordinates | `ClashSubject.Space` / `OpeningSubject.Space`; pairs only within one space | `Subjects_in_different_spaces_never_pair` |
| L1–L7 | `Inner().First` could throw; zero-length lines as subjects; preview of > 100 requests refused; empty set an "argument" error; description drift (label `(W×H)`, space inference, `marginMm 0`); `pairsChecked` 200 001; a route starting on the near face | centroid fallback; ≤ `tinySegment` counted as without shape; preview lists 100 + `truncated`; empty sets warned; descriptions; counted after the cap; passes (documented) | tests / seeds / live |
| L8, L9 | two classification scans per call; the 45° leader arrow | not changed (Low; the scans are bounded by `maxCandidates`) | — |
| L10 | the plan text promised "rectangle + cloud + tag + metadata" | delivered rectangle + MLeader (the ids and both handles in the leader text); no revcloud, no XRecord — noted in the phase file | — |

## Known limitations (phase H)

- A plan has no heights: a hard clash is "same place in plan"; the description says to check the section.
- A route passing through an equipment block it does not serve reads as a contact (the network builder attaches nodes the same way).
- Two structural / architectural members crossing in plan (a beam over a wall) are contacts, never clashes.
- Chords inside an outline longer than `maxChordMm` are skipped, not requested; a run inside a wall cavity is not an opening.
- Clearance is measured between plan boundaries only.
