# Phase 7 — 3D view of beam + rebar (three.js)

**Context:** `HPRebar.Core/KataRebar/Models/{KataRebarCurve,KataRebarLayoutResult,KataStirrupZoneResult}.cs`, `Calculators/{KataStirrupCurveFactory,KataWidthProfile,KataTopProfile,KataElevationOutline}.cs`.

## Requirements
- Scene DTO `rebar3d`: bars = centerline polylines (mm, beam local) + Ø + role + layer + number + keys; stirrups = zone (start, end, spacing, shape polyline in section plane) or explicit curves; concrete = beam outline × width profile, column stubs.
- Tab "3D" beside "Mặt đứng"; three.js renderer: tube geometry per bar (radius Ø/2), instanced stirrups, translucent concrete, orbit/pan/zoom, view cube presets (front = elevation, side = section), fit-all.
- Hover/click picks a bar → same selection store as 2D (highlight both), tooltip; Delete works from 3D too.
- Colours per role match 2D pens; dark/light.
- Lazy-load three.js chunk only when the 3D tab opens (keeps first paint fast).

## Steps
1. Mapper + Core tests (curve counts, stirrup expansion count = Σ zone length / spacing + 1 as Revit creates).
2. `Rebar3DView.tsx` + pure geometry helpers (vitest: tube path, instance matrices).
3. Live: open 3D on the 2-span fixture beam, compare bar count with Revit schedule; frame time with all bars.

## Success
vitest green; 3D shows every bar/stirrup the plan creates (count equal); orbit ≥ 50 fps on dev machine; selection syncs 2D ↔ 3D.
