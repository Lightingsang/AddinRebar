# 2026-09-17 — AEC engine phase F: rooms from walls, and what a review taught about doors

## What landed

- Phase F of `plans/260916-1140-aec-automation-mcp-autocad/`: 5 `arch_*` seeds over `HPAutoCad.Aec/Architecture/` (loop finder, labels, boundary checks,
  area schedule, dimension rules) + `Cad/RoomService` / `RoomWriteService`. 52 tools on the AutoCAD server. Report: `reports/phase-F-architecture-live.md`.
- Review round 5/10 → fixed the same day; `ArchitectureTests` 12. Tests 191 + 222, live 75/75 + 89/89.

## Decisions worth remembering

- **Doors are inferred from the walls.** The first cut had no notion of an opening, so a single-line plan with one door read as 0 rooms and a
  double-line plan as one merged room. Now two free ends facing each other along one line, or two jamb lines spanning the two long lines of a
  double wall, 600–2500 mm apart, are bridged by a virtual wall. No door objects needed; `detection.maxOpeningMm` bounds it.
- **A room holding another room is still a room.** The "nested" rule meant for the outer line of a double wall dropped a hall with a shaft and
  a room with a closet. The outer line of a double wall is the loop whose *every* vertex lies within the wall thickness of a loop it contains.
- **An overshoot is short and the junction must still exist.** A wall running 2 m past a T-junction and stopping is a real wall end; two chains
  peeled from both ends of a detached wall meet in the middle and are not "attached" to anything.
- **Facing tips are one gap.** One `boundary_gap` per pair at the midpoint; the reach is `detection.maxGapMm`, not a hidden multiple of roomGap.
- **A polyline's own far end counts as the nearest wall, its own next segment does not** (a sampled arc's neighbours are 50 mm away).
- **Room numbers need a separator after letters** (`A-101`, `P.101`, `101`, `1.01`); `B01`, `C1` and `KT-12` are marks. Every text line is a label.
- **Every tool that consumes room ids takes the same detection inputs**, or `R-003` means a different room in the next call.

## Gotchas

- `SpatialRelation.Overlaps` holds on a shared boundary run: tiled room outlines "overlapped" on every party wall until the check looked at
  interiors only.
- Git Bash heredocs halve backslashes and `newline="\n"` becomes `"
"`; a patch that must match `\P` in a file needs the Write tool.
- The harness scene grew to 55 entities; classification pages at 50, so a check that reads the door block from page one had to filter by handle.
