---
phase: I
title: "Change management — logical change sets"
status: pending
priority: P2
effort: "12h"
dependencies: [C]
---

# Phase I: Change management — logical change sets

## Context Links
- [architecture.md](architecture.md) · [ADR-01](adr/adr-01-tools-as-seeds-over-execute-plus-aec-assembly.md) · [ADR-02](adr/adr-02-units-tolerance-handles-envelopes.md)

## Overview
No AutoCAD `Transaction` survives a request (the bridge owns it). A change set is a logical plan kept in `HPAutoCad.Aec.Cad.ChangeSets` (static store in the
bridge process): operations (create/modify/delete + target handles + original state snapshot + proposed state + validation). `commit_change_set` replays the plan
inside one real run (`transaction: auto`, atomic); `rollback_change_set` discards the plan (or, when already committed, reverts through the stored original
state in a new run).

## Tools
`begin_change_set`, `preview_change_set`, `commit_change_set`, `rollback_change_set`, `get_change_summary` (+ edit tools accept `changeSetId` to record instead of apply).

## Success Criteria
- [ ] 30-operation change set previewed, committed as one undo entry, rolled back by handle list; store survives across requests, cleared on document close

## Risk Assessment
Same as phase A (output cap, ALC visibility of `HPAutoCad.Aec`, tolerance defaults); phase-specific risks are added when the phase starts.
