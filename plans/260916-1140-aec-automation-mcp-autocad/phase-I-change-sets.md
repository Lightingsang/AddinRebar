---
phase: I
title: "Change management — logical change sets"
status: completed
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
- [x] 30-operation change set previewed, committed as one undo entry, rolled back by handle list; store survives across requests, cleared on document close — `reports/phase-I-change-sets-live.md` (live Z, 2026-09-17: 30 ops in one run, one `U` reverts all; a second drawing has its own store, the first keeps its sets)
- [x] A commit or rollback undone by its request (dryRun) or by `U` is noticed on the next call (`UndoRule`); a partial rollback keeps the set committed with its snapshots; `keep` closes a set (review H1–H3)

Delivered: the snapshot of the original state is taken at **commit** time (`ObjectOpenedForModify` → `Entity.Clone()`), not at record time as the overview says — an entity can change between the record and the commit, and only the commit knows which entities an op opens for write. Ops are recorded as the write-tool calls themselves (tool + arguments), not as a proposed state.

## Risk Assessment
Same as phase A (output cap, ALC visibility of `HPAutoCad.Aec`, tolerance defaults); phase-specific risks are added when the phase starts.
