---
phase: H
title: "Coordination — clash check, opening requests"
status: completed
priority: P2
effort: "14h"
dependencies: [B, C]
---

# Phase H: Coordination — clash check, opening requests

## Context Links
- [architecture.md](architecture.md) · [ADR-01](adr/adr-01-tools-as-seeds-over-execute-plus-aec-assembly.md) · [ADR-02](adr/adr-02-units-tolerance-handles-envelopes.md)

## Tools
`aec_clash_check` (set A × set B by aecTypes/filters; broad phase `SpatialIndex` on expanded boxes, narrow phase segment/curve distance; clearance;
`CL-nnnn` with location, required vs actual clearance), `aec_create_opening_requests` (MEP route × wall/slab → OpeningRequest proposals on a dedicated layer:
rectangle + cloud + tag + metadata; never cuts members).

## Success Criteria
- [x] Clash between a duct polyline and a beam outline found with the right clearance sign; opening request drawn under dryRun and commit — `reports/phase-H-coordination-live.md` (live W / O, 2026-09-17)
- [x] Contacts (joints, tees, equipment on runs) and area overlaps (rooms, slabs) are `info`, hidden by `minSeverity` warning; only MEP interpenetration is a `hard_clash` (review H1 / M1)
- [x] Passes found through route / outline vertices, never a phantom between two real passes; chords longer than `maxChordMm` skipped and counted (review H2 / M3)

Delivered as rectangle + MLeader (ids and both handles in the leader text) on `HP-MCP-OPENINGS`; the plan's "cloud + metadata" (revcloud, XRecord) was not built — the leader text is what a later tool resolves. `StructuralSlab` is selectable, not a default host.

## Risk Assessment
Same as phase A (output cap, ALC visibility of `HPAutoCad.Aec`, tolerance defaults); phase-specific risks are added when the phase starts.
