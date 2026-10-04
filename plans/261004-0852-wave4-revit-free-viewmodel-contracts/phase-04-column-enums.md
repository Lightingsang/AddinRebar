# Phase 04 — Column: meaningful enums for the int codes (AUD-037)

## Context
- `TypeDis` (stirrup layout 0/1/2), `TypeH` / `TypeV` (cross-tie kind 0 closed / 1+ open), `*DowelsType`, `CrossTie(spec.TypeV + 1)` — 57 uses in 20 files, Core models included (`StirrupSpec`, `AdditionalTieSpec`).

## Steps (two batches)
- **4a — Core:** enums `TieLayout { Uniform, ... }`, `CrossTieKind { Closed, ... }`, dowel enum, with the current numeric values; model properties change type; Core callers and tests follow. Check `CharacterizationText.Hash` of every Column case: if an enum value hashes differently from its int, re-pin with a log line proving representation only (same geometry).
- **4b — add-in:** VM/editor bindings (combo boxes bind `SelectedIndex` today — map to the enum explicitly), `RebarShapeResolver.CrossTie(...)`, `ValidationMessages`.
- Build, Core tests, review, log per batch.

## Success
- No raw-int layout / cross-tie / dowel codes outside the enum definitions.
- Column window selections still map to the same layouts (manual check in Revit or theme gallery).
