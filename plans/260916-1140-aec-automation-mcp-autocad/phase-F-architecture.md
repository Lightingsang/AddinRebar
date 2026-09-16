---
phase: F
title: "Architecture — rooms, boundaries, room tags, area schedule, auto-dimension framework"
status: completed
priority: P2
effort: "18h"
dependencies: [B]
---

# Phase F: Architecture — rooms, boundaries, room tags, area schedule, auto-dimension framework

## Context Links
- [architecture.md](architecture.md) · [ADR-01](adr/adr-01-tools-as-seeds-over-execute-plus-aec-assembly.md) · [ADR-02](adr/adr-02-units-tolerance-handles-envelopes.md)

## Tools
`arch_detect_rooms` (closed loops from walls/polylines; area, centroid, nearby room text), `arch_room_boundary_check` (open room, tiny gaps, overlaps, duplicates),
`arch_create_room_tags` (MText or block attribute per config; edit envelope), `arch_generate_area_schedule` (room/department/area/percentage/total; no room standard hard-coded),
`arch_auto_dimension_plan` (rule framework + one rule: overall dimensions of a closed outline).

## Success Criteria
- [x] Loop finding on the harness wall scene (closed / 12 mm-open outlines, a two-room single-line plan with two doorways and a 250 mm gap) and on synthetic double-line, L-shaped, island, hall + shaft and closet plans; gaps reported with location — `reports/phase-F-architecture-live.md`

## Risk Assessment
Same as phase A (output cap, ALC visibility of `HPAutoCad.Aec`, tolerance defaults); phase-specific risks are added when the phase starts.
