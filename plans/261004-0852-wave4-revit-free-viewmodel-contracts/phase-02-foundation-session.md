# Phase 02 — Foundation: split the Revit session from the view data

## Context
- `FoundationRebar/Model/FoundationSession.cs` holds `Document`, `Floor`, `IReadOnlyList<RebarBarType>`, snapshot, spec; it is passed through `IFoundationRebarRunner` into the VM (9 files use it).

## Steps
1. New `FoundationViewData` (Model): snapshot (Core), bar types as a Revit-free list (name, diameter, id), spec.
2. `FoundationSession` stays Revit-side (command, handler, services); the command builds both and gives the VM only `FoundationViewData`.
3. `IFoundationRebarRunner.RunAsync(FoundationRebarSpec spec)` (or the view data) — the handler owns the `FoundationSession` it was created with.
4. `FindBarTypeByDiameter` moves to the service side (it returns `RebarBarType`).
5. Build R23/R26/R27, Core tests, review, log.

## Success
- No Revit type in `FoundationRebar/ViewModel/**` or `IFoundationRebarRunner`.
- Golden run (foundation fixture) identical — or waived.
