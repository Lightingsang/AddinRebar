---
name: hp-mcp-seed-review-pitfalls
description: What the compile check + structure tests of an HP MCP seed library (Registry/SeedLibrary/**) and the copied-host mirror fence do NOT prove — 64 KB cap arithmetic, nested args keys, units.Label == drawingUnit by construction, mirror fence per-file skip / one-directional coverage; consolidated from the Civil 3D phase-2/3 reviews (2026-09-18)
metadata:
  type: project
---

Consolidated from the Civil 3D phase-2 (8/10) and phase-3 (7/10) reviews, which earlier reviewer sessions had filed under `plans/…/.claude/` and `HPCivil3d/.claude/` (wrong cwd — those folders are strays, not the canonical memory).

**Seed libraries (`HP<Host>.Mcp.Server/Registry/SeedLibrary/**`)**
- **64 KB cap arithmetic.** The structure tests pin `maximum ≤ 500`; nobody multiplies bytes-per-item by the maximum. Measure per-item bytes from a real envelope (`output/smoke/seed-*.json`), multiply by every `limit`/`partLimit` maximum and by any uncapped nested list (`entities`). Over the cap the bridge serializer returns the whole value as one truncated string, so every item is lost. Every AEC phase and the Civil seeds repeated this despite green tests.
- **Nested `args` keys are unproven by the analyzer** — `ScriptAnalyzer` records `args.X("literal")` only when the receiver is the identifier `args`; `points[i].Double("x")` is invisible (mutation `x`→`xx` stayed green until the Civil `NestedArgReads` regex test landed). Ask for/verify a regex pass over non-`args` receivers ⇔ `items.properties`.
- **`units.Label == drawingUnit` by construction** in the Civil bridge (`Civil3dUnitTable.Resolve` labels with the Civil `DrawingUnits` string) — `insunitsMismatch = units.Label != drawingUnit` is dead-false; the signal is `units.Note != null`.
- Seed files may be untracked while a phase is uncommitted, so `git status` cannot prove generator idempotence — hash the tree before/after (`find … | xargs sha256sum | sha256sum`).
- Re-run every mutation yourself and restore by regenerating (`python HP<Host>/tools/generate-seed-library.py`); the first attempt in one review hit the wrong line and passed trivially.

**Mirror fence of a copied host (`HPCivil3d/tools/mirror-tokens.json` + `MirrorTests`)**
- Per-file skip when the *AutoCAD* counterpart is missing turns the fence off silently for that file; coverage is one-directional (Civil tree only) unless a reverse enumeration + pinned `autocadSha256` exists; add-only blocks keep stale AutoCAD doc lines. Check the fence's own blind spots before trusting "mirror tests green".

**Bundles (acad.exe family)**
- Debug IS the deployed configuration (`DeployBundle` only for `-c Debug`), so `#if DEBUG`-gated code ships to the user; the run-time check is the only gate.

**Why:** every HP MCP host after AutoCAD starts as a copy and ships seeds through the same contract; the green suites cannot see these classes, and each phase review lost a round to them.
**How to apply:** for any `SeedLibrary` review: (1) per-item bytes × every maximum, (2) mutate a nested key, (3) grep for flags derived from `units.Label`, (4) hash the seed tree around the generator. Related: [[hp-mcp-harness-review-checks]], [[verify-static-gates-with-scratch-probe]].
