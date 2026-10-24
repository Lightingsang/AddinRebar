# 0009 — HP BIM Toolkit uses HPRebar.Core for Kata; Kata's Revit adapters are ported

- **Status:** Superseded by [0010](0010-kata-export-web-ui-in-hprebar.md) (2026-10-24, by user: Kata stays in the rebar add-in; the toolkit reference, ported adapter and D8 were removed)
- **Tags:** [PROJECT] [REVIT]

## Context
Kata Export is rebuilt as a web module of HP BIM Toolkit (plan `plans/261024-2245-kata-export-web-module/`); the WPF tool keeps running beside it until the web module reaches parity. Kata's rules — segmenter, row builder, elevation model, sheet parser, bar numbering, anchorage, splitting, settings JSON — are pure code in `HPRebar.Core/KataExport` and `HPRebar.Core/KataRebar` (~12 k lines, netstandard2.0, Polyfill only, tested by `HPRebar.Core.Tests`). Kata's Revit and Excel adapters live in the rebar add-in (`HPRebar/HPRebar/KataExport/Service`, `KataRebar/Service`, ~4–5 k lines). D3 lets the rebar add-in reference `HPRebar.Core` only; nothing so far governs the toolkit, and one add-in never references another.

## Decision
1. `HPBimToolkit.Core` references the project `HPRebar.Core` (D8). Kata rules are never copied: a rule both tools need is added to, or fixed in, `HPRebar.Core`.
2. The toolkit never references the rebar add-in (`HPRebar/HPRebar`). Kata's Revit and Excel adapters are **ported** into `HPBimToolkit/Services/Kata/` (Revit 2026 only); each ported file and its source commit are listed in the plan's `reports/port-ledger.md`.
3. The duplication ends when the WPF Kata tool is retired (a separate user decision once the web module reaches parity) or when a shared Revit library is approved.
4. Runtime isolation: the toolkit runs in its own load context (`ContextName` in `HPBimToolkit.addin`) and ships `HPRebar.Core.dll`; the rebar add-in has `HPRebar.Core` ILRepack-merged into `HPRebar.dll`, so no assembly named `HPRebar.Core` exists outside the toolkit's context. Verified by the phase-0 spike. Revit's add-in loader resolves by simple name across add-in contexts (the loose-Serilog precedent of ADR-0008), so this isolation holds **only while** the rebar add-in keeps `HPRebar.Core` ILRepack-merged (`IsRepackable=true`) and no add-in deploys a loose `HPRebar.Core.dll`; if either changes, rename the toolkit's copy or share one file deliberately.

## Consequences
- One source of Kata rules for both tools; `HPRebar.Core` changes must keep both callers building (the toolkit builds only under `*.R26`, the rebar add-in under every `R*`).
- Temporary duplicate adapter code that can drift while the WPF tool is still edited; the port ledger is the drift check.
- `HPBimToolkit.Core` is no longer free of rebar knowledge, but it stays free of `Autodesk.*`.

## Alternatives
- **Copy the engine into the toolkit:** independent, but two rule sets drift — rejected by the user.
- **Shared Revit library for the adapters (e.g. `HPRebar.Kata.Revit`):** no duplication, but refactors the live-verified WPF tool now (overlaps Wave 6 / ADR-0006) and needs R23–R27 + ILRepack handling — rejected for now by the user.
- **Toolkit references the rebar add-in:** couples two add-ins and their load contexts — rejected.

## Rules
DEPENDENCY_RULES D1, D3, D8 (new); ADR-0006 (one Kata feature in the rebar add-in, unchanged); ADR-0008.
