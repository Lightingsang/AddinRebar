---
phase: C
title: "Editing — batch create/update, blocks + attributes, annotations, hatches, xrefs"
status: completed
priority: P2
effort: "20h"
dependencies: [A]
---

# Phase C: Editing — batch create/update, blocks + attributes, annotations, hatches, xrefs

## Context Links
- [architecture.md](architecture.md) · [ADR-01](adr/adr-01-tools-as-seeds-over-execute-plus-aec-assembly.md) · [ADR-02](adr/adr-02-units-tolerance-handles-envelopes.md)

## Overview
Write tools with the edit envelope; `atomic` (default) = any failure → the run throws → the bridge aborts the outer transaction (nothing kept);
`atomic: false` = per-item errors collected, the rest committed. All support the request-level `dryRun`.

## Tools
| Tool | Category | Notes |
|---|---|---|
| `create_entities_batch` | Drawing | items[] of {type: line/polyline/circle/arc/text/mtext/blockReference/dimension, layer, color, … geometry in mm}; validates layer exists/unlocked; returns handles in input order |
| `update_entities_batch` | Drawing | items[] {handle, set: {layer, color, linetype, text, attributes{}, visible, geometry}}; `LAYER_LOCKED`/`LAYER_FROZEN` per item |
| `manage_blocks_attributes` | Block | op: listDefinitions / findReferences / insert / readAttributes / writeAttributes / batchUpdateAttributes / inspectDynamic / setDynamic |
| `manage_annotations` | Annotation | op: create/update/delete/batchUpdate for text, mtext, dimension (aligned/linear/angular/radial/diameter), mleader |
| `manage_hatches` | Drawing | op: create/update/delete/detectBoundary; validates closed boundary (`NOT_CLOSED`) |
| `manage_xrefs` | Data | op: list/attach/detach/reload/unload/bind(safe only)/resolveStatus; response name/path/loaded/found/nested/status |

## Success Criteria
- [x] Every op ran live with dryRun and real commit; U reverts one run — `run-aec-edit-tools-live.ps1` 40/40 (2026-09-16), [report](reports/phase-C-editing-live.md)
- [x] Locked/frozen layer and erased handle paths return structured errors, never exceptions to the client — LAYER_LOCKED / LAYER_FROZEN / ERASED / INVALID_HANDLE / UNSUPPORTED_ENTITY / NOT_CLOSED verified live per item

## Implementation notes (as built)
- `EditResult` envelope + `ItemOutcome` per input item; `EditContext` (layer guard, space, points, common properties); `EntityFactory` / `EntityUpdater` / `BatchEditService` (atomic: validate all → refuse on one invalid item, nothing written; write failure throws → bridge aborts); `BlockService`, `AnnotationService`, `HatchService` (`detectBoundary` geometric, not `Editor.TraceBoundary`), `XrefService` (`bind` only Resolved + loaded); `AecTools.Editing` facade with flat parameters because the analyzer needs every schema key read as `args.X("literal")` in the shim.
- Read ops under a write tool (list/find/read/inspect/detectBoundary/resolveStatus) return the analysis envelope; write ops the edit envelope.
- Not verified live: `setDynamic` on a real dynamic block (refusal path only), associative hatches.

## Risk Assessment
Same as phase A (output cap, ALC visibility of `HPAutoCad.Aec`, tolerance defaults); phase-specific risks are added when the phase starts.
