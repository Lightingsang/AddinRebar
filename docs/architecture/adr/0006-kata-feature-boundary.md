# 0006 — Kata Export / Kata Rebar boundary

- **Status:** Accepted (2026-10-03, by user) — option A chosen
- **Tags:** [PROJECT]

## Context
KataExport (5.3 k lines) and KataRebar (2.3 k lines) reference each other's `Service`/`Model` namespaces (6 + 5 files — a cycle), and KataRebar also uses BeamRebar's `PointMapper` and `RevitDialogs`. Kata Export's window already carries the whole Revit → Excel → Revit round trip (read the sheet back, preview, settings, generate); Kata Rebar is a second window over the same `KataRebarWorkflow.Generate`, with small behavioural differences (no `preferReversed`, settings loaded in the handler, `Raise()` result ignored). The `KATA_ONLY` build ships only Kata Export. Kata is under active development in another session (canvas drawing, 2026-10-03).

## Options
- **A. One Kata feature.** Retire the Kata Rebar button/window; its services become part of KataExport (`KataExport/Service/…`). Removes the cycle, one window, one VM, one runner. User-visible: one ribbon button less.
- **B. Two features + shared Kata kernel.** Keep both windows; move run reading, Excel COM, settings store and `KataRebarWorkflow` into `Shared/` (+ Core where pure). Removes the cycle; no user-visible change; two UIs to maintain.

**Decision (2026-10-03): A.** The Kata Rebar button/window is retired; its services move under KataExport. Executed in Wave 6 after the concurrent Kata work is committed and stable.

## Consequences
A: less code, ribbon change. B: no ribbon change, more code in `Shared/`.

## Rules
PCC-230, PCC-236, PCC-237; DEPENDENCY_RULES F1.
