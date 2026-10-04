# Phase 03 — Beam: session on the Core stack, bar types by id

## Context
- `BeamRebarSession.Stack` (`BeamStack`: `XYZ` axes, `PointMapper`, faces) — 18 uses in `BeamRebar/View` + `ViewModel` (painters, tabs); `Faces` property; `RebarTypeInfo` (14 uses in the VM) with `RebarBarType BarType` read by 6 creators.

## Steps (two batches)
- **3a — bar types:** `BeamRebar/Model/RebarTypeInfo` → `long BarTypeId` (same helper as phase 1); creators resolve the type through `RebarTypeCatalog` by id; `BeamRebarSpec.MainBarType` etc. keep `RebarTypeInfo`.
- **3b — stack:** session and painters read `BeamContinuousStack` (Core) — every use today is `Stack.Spans`, `Stack.Supports`, `Stack.ContinuousStack.*`; the handler/orchestrator keeps `BeamStack` + faces; drop `Session.Faces`.
- Build R23/R26/R27, Core tests, review, log per batch.

## Success
- No Revit type in `BeamRebar/ViewModel/**`, painters included.
- Golden run (beam fixture with a cantilever + secondary beam) identical — or waived.

## Risks
- Painters draw from `Stack` — a wrong property swap draws a different preview with no test failing; compare preview screenshots before/after (theme gallery or Revit).
