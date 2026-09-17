---
phase: G
title: "MEP — network graph, connectivity, open endpoints"
status: completed
priority: P2
effort: "16h"
dependencies: [B]
---

# Phase G: MEP — network graph, connectivity, open endpoints

## Context Links
- [architecture.md](architecture.md) · [ADR-01](adr/adr-01-tools-as-seeds-over-execute-plus-aec-assembly.md) · [ADR-02](adr/adr-02-units-tolerance-handles-envelopes.md)

## Tools
`mep_detect_network` (pipes/ducts/trays as edges, fittings/equipment/terminals/endpoints as nodes → networks by system), `mep_connectivity_check` (open ends,
disconnected runs, orphan fittings, duplicate connections), `mep_endpoint_check` (every unconnected endpoint with location).

## Success Criteria
- [x] Graph built from lines / polylines / arcs / splines on the rule set's MEP layers with `tolerance.endpointConnection`; systems from `detection.systems` (layer wildcards) else the layer — `reports/phase-G-mep-live.md`

## Risk Assessment
Same as phase A (output cap, ALC visibility of `HPAutoCad.Aec`, tolerance defaults); phase-specific risks are added when the phase starts.
