---
name: seed-review-pitfalls
description: What the compile check + structure tests of an HP MCP seed library do NOT prove (found reviewing the Civil 3D phase-3 seeds, 2026-09-18); check these by hand before scoring any seed phase
metadata:
  type: project
---

Reviewing `HPCivil3d/HPCivil3d.Mcp.Server/Registry/SeedLibrary/**` (phase 3, `reports/code-review-phase-03.md`, 7/10) showed three things the green test suite cannot see:

- **64 KB cap arithmetic.** `SeedLibraryTests` only pins `maximum ≤ 500`; nobody multiplies bytes-per-item by the maximum. Measure from the smoke output (`output/smoke/seed-*.json`): Civil items are 180–540 B, so `limit`/`partLimit` 500 (and even the 200 default with parts) overflow, and the bridge serializer then returns the whole value as one truncated string. Compute `perItem × maximum` for every list seed and for any uncapped nested list (`entities` in `get_alignment_geometry`).
- **Nested `args` keys are unproven.** `ScriptAnalyzer` records `args.X("literal")` only when the receiver is the identifier `args`; `points[i].Double("x")` is invisible. Verified by mutation (`x`→`xx`, 84/84 still pass). Ask for a regex pass over non-`args` receivers ⇔ `items.properties`.
- **`units.Label == drawingUnit` by construction** in the Civil bridge (`Civil3dUnitTable.Resolve` labels with the Civil `DrawingUnits` string), so any seed computing `insunitsMismatch = units.Label != drawingUnit` is dead-false; the real signal is `units.Note != null`. A smoke scene whose INSUNITS equals the Civil unit cannot expose it.

Also: seed files are untracked while the phase is uncommitted, so `git status` cannot prove generator idempotence — hash the tree before/after (`find … | xargs sha256sum | sha256sum`).

**Why:** every HP MCP host ships seeds through the same contract; the AEC phases each lost a review round to envelope-cap overflows, and the Civil seeds repeated it despite green tests.
**How to apply:** for any `SeedLibrary` review, (1) measure per-item bytes from a real envelope and multiply by every `maximum`, (2) mutate a nested key and rerun the tests, (3) grep seeds for flags derived from `units.Label`.
