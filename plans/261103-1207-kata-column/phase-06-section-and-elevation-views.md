# Phase 6 — Section drawings (page) + Revit section and elevation views

Contract agreed 2026-11-03 (grill-me). Order 1 → 2 → 3, each live-checked on its own.

## 1. Page: one section per storey (AFK + live)
- Core `KataColumnSectionDrawings.Build(spec, storeys, design)` → per storey: cut at mid clear height, bars crossing the
  cut (position interpolated on the run, so insets/cranks show), hoop (rect/circle, centreline offset cover + Ø/2), ties
  from `KataColumnTieGeometry` (shared with the creator), numbers. Section-local mm (origin = storey min corner, X = b).
- `KataColumnRebarSummary.Sections` (additive) → `columnContracts.ts`; grid of small SVGs, click = enlarged.
- Errors in the sheet → no sections. Tests: Core (bars at cut, tie boxes = creator), vitest on regenerated fixture.

## 2. Revit section views (AFK + live)
- Per storey a ViewSection looking down at the cut, crop = column + margin, far clip ≥ one hoop spacing, 1:20, named
  "<C3> – <storey>"; dims b, h, bar rows; Rebar tags (model's tag family, warning when missing). Created in the same
  group as the bars; previous views of the column (same name prefix, stamped) replaced.

## 3. Revit elevation view (AFK + live)
- One ViewSection along b through the stack, 1:50, named "<C3> – mặt đứng"; dims storey levels, lap lengths (A/B on
  either side), hoop zones; tags. Replaced on rerun.

Out of scope: sheets, view templates, page elevation, walls, coupler.
Assumptions: cut at mid clear height; first Section view type; existing rebar tag family.

## Status 2026-11-03
| Part | Status | Evidence |
|---|---|---|
| 1 page sections | ✅ | Core 1792, vitest 25; live screenshot (6 storeys, large view with numbers) |
| 2 Revit section views | ✅ (visual check by user pending) | live: 6 structural plans "C24 – <storey>", 24 dims 0 refused, tags on the rebar's subelement (the element reference is refused: "The reference can not be tagged"); rerun reuses the views |
| 3 Revit elevation | ✅ (visual check by user pending) | live: 1 section "C24 – mặt đứng" 1:50, 4 dim chains, 18 tags; rerun reuses |

Notes: a horizontal cut is a structural plan (Revit sections look sideways); views need `Regenerate` after range/crop/unobscured before tagging.
Open: layer-2 bars on slots taken by layer 1 are dropped without a note (phase-3 rule, logged); visual check of views by user.
