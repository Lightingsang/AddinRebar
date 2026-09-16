---
phase: H
title: "Coordination — clash check, opening requests"
status: pending
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
- [ ] Clash between a duct polyline and a beam outline found with the right clearance sign; opening request drawn under dryRun and commit

## Risk Assessment
Same as phase A (output cap, ALC visibility of `HPAutoCad.Aec`, tolerance defaults); phase-specific risks are added when the phase starts.
