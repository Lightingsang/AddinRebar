# 2026-09-17 — AEC engine phase H: clashes in plan, and what "touching" is not

## What landed

- Phase H of `plans/260916-1140-aec-automation-mcp-autocad/`: `aec_clash_check` (read-only) and `aec_create_opening_requests` (auto) over
  `HPAutoCad.Aec/Coordination/` (classifier, detector, planner) + `Cad/CoordinationService`, `Cad/OpeningWriteService`. 57 tools on the AutoCAD
  server. Report: `reports/phase-H-coordination-live.md`.
- Review round 5.5/10 → fixed the same day; `CoordinationReviewTests` 8. Tests 210 + 253, live 90/90 + 96/96.

## Decisions worth remembering

- **A plan is drawn to meet.** The first cut called every boundary contact a critical clash, so a well-drawn frame checked against itself was
  nothing but criticals (beam ends on column faces) and every tee was a clash. A clash is now an MEP element interpenetrating something it
  cannot share the plan with; contacts, tees, equipment on runs, doors in walls and members meeting are `contact` (info); rooms and slabs holding
  their floor are `area_overlap` (info). `minSeverity` defaults to warning so the AI sees hard and clearance clashes first and the counts anyway.
- **Passes are intervals along the route, not pairs of proper crossings.** A vertex snapped to a wall face is not a "proper" crossing and the
  first cut lost the pass — or, with two passes, paired the survivors across the outside and drew a phantom opening where the duct never ran.
  Now: every boundary intersection, sorted by station, and the stretch between consecutive stations whose route midpoint is strictly inside.
- **A chord is not an opening.** A pipe drawn inside a wall cavity for 4 m, or a tray crossing a slab outline, produced a request at the
  middle of the chord. `maxChordMm` (1 000) skips and counts them; the slab left the default hosts.
- **The pair cap did not bound the work**: 64-vertex polylines under a clearance cost 5 ms a pair. `DistanceXY(upTo)` culls segment pairs by
  box separation, big pairs query a per-shape segment index, and segment pairs count toward the cap.
- **A clearance clash is located at the closest vertex–foot pair**, never at "the vertex nearest the other shape" (metres away on a long run).

## Gotchas

- Debug builds are what the harness and the tests run: struct-heavy inner loops through `IReadOnlyList` indexers cost 3× — the hot loops use arrays.
- `AreaTypes` (Room, StructuralSlab) are a type class, not a rule: an entity classified as a slab holds everything on it whatever its layer.
