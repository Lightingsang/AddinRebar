---
phase: 4
title: "Double-line walls — create from centrelines, repair junctions and gaps, change thickness"
status: planned
priority: P3
effort: "24h"
dependencies: []
---

# Phase 4: Double-line walls (tường 2 nét)

## Context Links
- [plan.md](plan.md) · [research](research/vinacad-express-tools-analysis.md) (rows `WW` / `EW` / `WWT` / `WWO` / `TW`)
- Reuse: `Architecture/RoomLoopFinder` + `WallGraph` (already pairs wall faces, cuts pieces at crossings / T-junctions / collinear overlaps,
  `maxWallThicknessMm` 500, `roomGap`), `Geometry/GeometryMath` (`IntersectXY`, parallel test), `Spatial/SpatialIndex`, `Cad/EntityFactory`,
  `Cad/EntityUpdater.Geometry` (line endpoints), `Cad/EditContext.Layers`, `ChangeSetRecorder`, `WriteToolTable`

## Overview
Architectural plans in AutoCAD are still drawn as two LINEs per wall. Drawing, cleaning the L / T / X junctions, healing the hole left
after deleting a door or a wall, and changing a wall's thickness are the daily grind. Three tools give the AI those operations as batch
edits with preview and undo. **Read side already exists** (`arch_detect_rooms` understands double-line walls); this phase is the write side.

Tools (category Drawing, all `auto`): `create_double_line_walls` · `repair_double_line_walls` · `change_wall_thickness`.

## Key Insights
- Wall identity: two parallel LINEs (angle ≤ `parallelAngle` 0.5°) at distance `thickness` ≤ `maxWallThicknessMm` with overlapping
  projections. The source tags each face in XData so later commands find pairs even after edits; do the same — regapp `HPAEC_WALL`,
  payload `{wallId, side L|R, thicknessMm, centreStartMm, centreEndMm}` — **but never require it**: untagged walls are paired
  geometrically (the `RoomLoopFinder` rule), tagged walls are paired by id first.
- Junction cleanup is a per-vertex operation on the *centreline graph*: L (two ends meet) → miter the outer faces, trim the inner;
  T (an end on a body) → cut the crossed face between the two faces of the arriving wall; X (two bodies cross) → cut both pairs into four
  pieces around the crossing; free end → optional cap LINE of the thickness. Working on the centreline graph (not on face lines
  pairwise) is what keeps it from becoming the source's 2 000-line special-casing.
- Healing: after erasing a wall segment (or a door block that had cut the faces), two facing free ends of the same wall within
  `healDistanceMm` 600 get joined (extend both faces to meet, remove caps).
- Thickness change keeps a reference: `centreline` (both faces move), `left` / `right` (one face moves); touched junctions are re-cleaned.
- Arcs: **not in v1** (straight walls only; an ARC in the selection is reported `UNSUPPORTED_ENTITY`). The read side already handles arcs
  for rooms; writes stay linear until the linear version is verified live.

## Requirements
### Functional
- `create_double_line_walls`: `centrelines[]` = LINE / LWPOLYLINE handles **or** `segments[{fromMm{x,y}, toMm{x,y}}]` (≤ 200), `thicknessMm`
  (required, ≤ `maxWallThicknessMm`), `justify center|left|right` (left/right relative to the drawing direction), `layer` (created on demand),
  `capEnds` (default true), `cleanJunctions` (default true — among the new walls **and** existing walls on the same layer within a padding of
  2 × thickness), `tag` (default true), `eraseCentrelines` (default false), `dryRun`, `changeSetId`, `atomic`. Output `EditResult` +
  `walls[{wallId, faceHandles[2], capHandles[], lengthMm}]`, `junctions{L, T, X, free}`.
- `repair_double_line_walls`: `filter` (layer required or handles; space; bbox via `query_entities_spatial`-style `insideBboxMm`), `ops[]`
  from `healGaps` (`healDistanceMm` 600), `cleanJunctions`, `capFreeEnds`, `removeCaps`, `eraseWalls{handles or wallIds}` (+ auto-heal the
  neighbours — the `EW` idea), `retag` (write / refresh XData on pairs found geometrically); `maxWallThicknessMm` 500, `parallelAngleDeg`
  0.5, `dryRun`, `changeSetId`. Output `EditResult` + `summary{walls, pairedByTag, pairedByGeometry, unpaired, junctions{L,T,X}, healed,
  changedLines}` + `items` per line changed; unpaired lines listed (cap 100) as warnings, never touched.
- `change_wall_thickness`: `walls` = wallIds or face handles (one handle selects its pair), `thicknessMm`, `keep center|left|right`,
  `recleanJunctions` (default true), `dryRun`, `changeSetId`. Output per wall `{wallId, oldThicknessMm, newThicknessMm, movedFaces}`.
### Non-functional
- All geometry pure and unit-tested on synthetic walls (L, T, X, collinear, near-parallel 0.4° vs 0.6°, gap 599 vs 601).
- Bounded work: `MaxWallsPerCall` 200, junction search through `SpatialIndex`, `ct` per wall.

## Architecture
```
Architecture/WallGeometry.cs      offset a centreline both sides, miter at L, caps; justify
Architecture/WallPairing.cs       tagged pairs → geometric pairs (parallel + distance + overlap) → WallRecord[] {id, faces, centreline, thickness}
Architecture/WallJunctions.cs     centreline graph (vertices within tolerance) → L/T/X/free classification → face cut/extend plan (pure)
Architecture/WallHealing.cs       facing free ends of one wall within healDistance → join plan
Architecture/WallThickness.cs     face move plan
Cad/WallTagStore.cs               XData read/write (regapp registered on demand)
Cad/WallWriteService.cs           two-phase: validate every handle/layer/space, then create/trim/extend/erase faces, caps, tags
AecTools.Walls.{Create, Repair, ChangeThickness}
```
- Every plan is a list of primitive edits (`CreateLine`, `SetEndpoints`, `Erase`) applied by `WallWriteService`; the same list is the
  preview under `dryRun` and the record in a change set.

## Related Code Files
- Create: `HPAutoCad.Aec/Architecture/Wall{Geometry,Pairing,Junctions,Healing,Thickness}.cs`, `Cad/WallTagStore.cs`, `Cad/WallWriteService.cs`,
  `AecTools.Walls.cs`, 3 seed folders under `Drawing/`, `HPAutoCad.Aec.Tests/WallWriteTests.cs`.
- Modify: `WriteToolTable`, `RoomLoopFinder` (accept `WallRecord` pairs from `WallPairing` so both sides share one pairing rule — behaviour-preserving, `ArchitectureTests` stay green), harness scene (an L-shaped 200 mm wall run with a T, an X, a door gap, a 250 mm gap).

## Implementation Steps
1. `WallGeometry` + `WallPairing` (pure) + tests; make `RoomLoopFinder` consume `WallPairing` (no output change on the existing scenes).
2. `WallJunctions` (L/T/X/free on the centreline graph) + `WallHealing` + tests.
3. `WallTagStore` + `WallWriteService` two-phase primitives (`CreateLine`, `SetEndpoints`, `Erase`) + change-set snapshots (modified LINEs go through `ObjectOpenedForModify` → phase-I rollback works unchanged).
4. `create_double_line_walls` seed (segments + handles, justify, caps, clean, tag).
5. `repair_double_line_walls` seed (ops, unpaired listed, erase + heal).
6. `change_wall_thickness` seed.
7. Live: create the L/T/X run (6 walls), `arch_detect_rooms` sees the same rooms as the hand-drawn scene, erase one wall → neighbours healed, thickness 200 → 300 keeping the left face, `U` after each, arc in selection refused per item.
8. Review + docs.

## Todo List
- [ ] 1 · [ ] 2 · [ ] 3 · [ ] 4 · [ ] 5 · [ ] 6 · [ ] 7 live · [ ] 8 docs

## Success Criteria
- On the harness scene every junction is clean (no overshoot, no gap > `pointEquality`), `arch_room_boundary_check` reports 0 `open_boundary` after `healGaps`, thickness change preserves the kept face's handle and coordinates, `U` returns the drawing to its previous fingerprint (phase-I `Fingerprint`).

## Risk Assessment
- **Effort.** The source spent 4 000 lines on this; the centreline-graph approach is the bet that keeps it under 1 500. If step 2 exceeds 600 lines, stop and re-scope to L + free ends only.
- Existing walls drawn as LWPOLYLINE faces (not LINEs) → pairing works on segments but edits on polylines need vertex edits; v1 refuses polyline faces per item (`UNSUPPORTED_ENTITY`) and says so.
- Two walls of different thickness meeting at a T → the cut is asymmetric; covered by a unit test.

## Security Considerations
- Erases only what the caller names (`eraseWalls`) or what the cleanup provably replaces (pieces inside another wall's footprint, listed in the preview); everything else is extend/trim; change-set snapshots make it reversible.

## Next Steps
- Arcs; door/window openings as first-class objects (cut + jamb lines) — a later plan once `create` and `repair` have run on a real plan.
