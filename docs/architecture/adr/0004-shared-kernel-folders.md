# 0004 — Shared kernel folders `Shared/` and `HPRebar.Core/Shared/`

- **Status:** Proposed (2026-10-03)
- **Tags:** [PCC] [PROJECT]

## Context
The feature folder rule says nothing about code used by several features, so each feature copied what it needed: `RevitUnits` ×4, `RevitDialogs` ×4, `RebarFailureHandling` ×3 (+ Kata variant), `PointMapper` ×2, `LocalizationService` ×2, draw primitives ×3, Core `Point3` ×3, `Polyline3`/`Vector3` ×2, the ExternalEvent handler skeleton ×5. Where nothing was copied, features reached into each other (KataRebar → BeamRebar.Service, Core.KataRebar → Core.BeamRebar.Models), creating hidden coupling. Copies have already diverged (two `FindBarType` tolerances 0.5 / 1.0 mm, two ways to write `Partition`).

## Decision
1. Add `HPRebar/HPRebar/Shared/` (namespaces `HPRebar.Shared.Revit`, `.Wpf`, `.Excel`) and `HPRebar/HPRebar.Core/Shared/` (`HPRebar.Core.Shared.Geometry`).
2. Admission: a type enters `Shared/` only when ≥ 2 features use it **and** the copies represent the same knowledge (PCC-230), not coincidental similarity (PCC-233).
3. `Shared/` never references a feature; features never reference each other (DEPENDENCY_RULES F1–F3).
4. `Resources/` keeps its role (themes/icons).
5. CLAUDE.md "Feature Folder Convention" gains one paragraph naming `Shared/` as the second cross-feature folder.

## Consequences
- \+ One fix lands everywhere (R27 `CreateFromCurves` gate, `Raise()` check, failure policy). Cycles disappear.
- − `Shared/` can become a junk drawer; the admission rule and review keep it small. Moving types changes namespaces (commit moves separately — PCC-213).

## Alternatives
Keep copies (rejected: divergence already observed). A separate `HPRebar.Revit.Common` project (rejected: needs its own multi-version Revit targeting; a folder suffices).

## Rules
PCC-213, PCC-218, PCC-230, PCC-232, PCC-233, PCC-234.
