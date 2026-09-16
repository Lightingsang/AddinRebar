---
phase: E
title: "Structural — grids, members, connectivity, alignment, openings, tagging, schedule"
status: pending
priority: P2
effort: "22h"
dependencies: [B]
---

# Phase E: Structural — grids, members, connectivity, alignment, openings, tagging, schedule

## Context Links
- [architecture.md](architecture.md) · [ADR-01](adr/adr-01-tools-as-seeds-over-execute-plus-aec-assembly.md) · [ADR-02](adr/adr-02-units-tolerance-handles-envelopes.md)

## Tools
`structural_detect_grids` (lines + bubbles + labels → grid topology), `structural_detect_members` (classifier + geometry → beams/columns/walls/slabs/openings with
section sizes), `structural_member_connectivity_check` (beam ends vs supports within `EndpointConnection`; `STR-CON-nnn`), `structural_column_alignment_check`
(column centroid vs nearest grid intersection, tolerance), `structural_opening_conflict_check` (geometry only: through column, too close to column / joint, outside host),
`structural_tag_members` (prefix, start, sort, duplicates; writes text or attribute; edit envelope), `structural_generate_member_schedule` (JSON; AutoCAD Table when
a table helper exists).

## Success Criteria
- [ ] Each check produces issues with location + suggestedAction on the beam B01 sample drawing; no structural capacity claims

## Risk Assessment
Same as phase A (output cap, ALC visibility of `HPAutoCad.Aec`, tolerance defaults); phase-specific risks are added when the phase starts.
