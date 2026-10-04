# Phase 4 — Baseline + per-batch protocol

## Overview
P1 · planned · depends on phases 1-3. Produces the committed baseline Wave 4 compares against.

## Baseline
1. HEAD = phase-1 seam commit + phase-2 fixture (+ any fix-track commits the user wants in first; B-01/B-02/B-03 change Beam/Column output — decide order with user before baseline, else re-baseline after them).
2. `run-golden.ps1` twice, fresh copy each → `compare-golden.py run1 run2` must be identical (determinism gate). Not identical → find the unstable field, fix normalization (not the add-in), repeat.
3. Commit `HPRebar/HPRebar.Tests/Fixtures/golden/baseline/{column,foundation,beam}.json`, `*.readback.json`, `*.dialog.txt`, `run-meta.json` (git sha, `HPRebar.dll` sha256, Revit build, fixture sha256). Small text files; run outputs stay in `HPRebar/output/golden/` (gitignored, `.gitignore:13`).

## Per Wave 4 batch (phases 1-3, also cheap for 4)
```
EDIT → BUILD R23/R26/R27 → TEST → REVIEW → (Revit closed) run-golden.ps1 → compare-golden.py baseline/ <run>/
identical  → batch may commit; log "Golden run: PASS <run dir> vs baseline <sha>"
different  → STOP (REFACTORING_PLAN §1.5); diff in log; fix the refactor, never the baseline
```
- Re-baseline only after an intentional behaviour commit (fix track), logged with old/new sha and the diff.
- Revit build differs from `run-meta.json` → compare still runs; any diff → re-run baseline sha first to separate Revit-update drift from code drift.
- Kata: excluded until its freeze; add a Kata area + spec later in the Kata wave.

## Docs
- `docs/clean-code/REFACTORING_PLAN.md` §3: "fresh copy instead of undo", harness path, R26-only note; Wave 0 row 0.3 ✅.
- `docs/clean-code/REFACTORING_LOG.md`: entry with commands/results.
- `CLAUDE.md` "Current State": TUnit fixture now exists (21 tests run or still skip — state measured); regenerate `AGENTS.md` only after `cmp` check (AGENTS.md may be ahead — memory note).

## Success criteria
Two baseline runs identical; files committed (no push); Wave 4 plan's "golden run per feature" gate satisfiable by one command.

## Residual risks (documented, not blocking)
- R23/R24 (`int` ElementId) and R25/R27 never run in Revit — build-only.
- Column cross-tie path (`M_T10`) and non-default specs beyond the overrides uncovered.
- Golden = same output on one fixture, not correctness.
