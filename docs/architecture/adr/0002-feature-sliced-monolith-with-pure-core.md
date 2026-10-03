# 0002 — Feature-sliced add-in + pure domain library (keep)

- **Status:** Proposed (2026-10-03) — records the existing design as a decision
- **Tags:** [PROJECT] [REVIT]

## Context
Each feature is one folder `HPRebar/HPRebar/<Feature>/` (Command, ExternalEventHandler, Request, SelectionFilter + Model/Service/View/ViewModel) with its maths in `HPRebar.Core/<Feature>/`. `Document` is sealed and unmockable, so testable logic must live outside the Revit-bound assembly. The split works: ~630 Core test methods, zero Autodesk references in Core.

## Decision
1. Keep the feature folder convention of CLAUDE.md unchanged.
2. Keep **one** add-in assembly and **one** Core assembly; do not split into Application/Infrastructure/Domain projects.
3. Inside a feature, the layers of [ARCHITECTURE.md §4.1](../ARCHITECTURE.md) apply: Entry → Presentation / Application → Revit adapters → Domain.
4. Branching logic that needs no Revit/WPF/COM type moves to `HPRebar.Core/<Feature>/` with tests.

## Consequences
- \+ Small, navigable units; a feature can be removed by excluding a folder. Core stays fast to test.
- − Layering inside a feature is by convention, not by compiler; checked by DEPENDENCY_RULES L1–L10 in review.

## Alternatives
Clean/onion architecture with 3–4 projects (rejected: multiplies projects for five small features, no second UI or host — PCC-123, PCC-236). Layer-first folders across features (rejected: the repo chose feature-first; PCC-210 says stay consistent).

## Rules
PCC-077, PCC-105, PCC-110, PCC-210, PCC-274; DEPENDENCY_RULES D1, L1–L10.
