---
name: project-rebar-input-order-dependencies
description: Which HPRebar command pipelines depend on the order of the elements handed in (pick order vs selection order) — check when a diff changes how elements are gathered
metadata:
  type: project
---

- Beam: `BeamStackReader.Read` and `BeamStackValidator` take the axis origin and direction from `beams[0]`'s location line (its drawn direction), then sort along it. For a run whose beams are not all drawn the same way, span 1 / start-end flip depending on which beam is first. Pick path = first clicked; preselection (`Shared/Revit/PreselectionPicker`, 2026-10-04) = `Selection.GetElementIds()` order (unspecified). Noted as Medium in the preselection review, not fixed there.
- Column: order-independent — the command sorts by `ColumnStackReader.BottomFace(...).Origin.Z`.
- Foundation: single element.

**Why:** gathering changes (preselection, MCP-driven golden runs) look behaviour-neutral but silently change beam span direction.
**How to apply:** any diff that touches how Beam/Kata elements are collected or ordered; ask whether the reader should canonicalise the axis instead.
