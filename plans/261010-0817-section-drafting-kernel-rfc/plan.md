# RFC: section drafting kernel (`Shared/Revit` + `HPRebar.Core/Shared/Drafting`)

Status: **proposed — needs user approval before any wave batch** · Source: `/bs:code-review codebase deepen HPRebar/HPRebar` candidate #1 ([report](../reports/architecture-deepening-261010.md)), user pick 2026-10-10 · Design: 3 parallel sub-agents (A facade, B pure planner, C run session).

## Problem
Section/detail view creation (view type, template, unique name, scale), face → linear reference, and text-table rows are copied between `ColumnRebar/Service/` and `BeamRebar/Service/` (`DetailViewCreator`, `SectionViewCreator`, `DimensionCreator`, `RebarTableTagCreator`, `*AnnotationSettings`), plus a third naming policy in `KataRebar/Service/KataSectionViews.cs`. The copies already diverged: Beam got the B-16/B-18 fixes, Column did not → B-47, B-48; both share B-17. Records: B-16/17/18/47/48, B-07, AUD-016, AUD-048, ADR-0004.

## Options
| | A — minimal facade | B — pure Core planner + applier | C — run-scoped session |
|---|---|---|---|
| Shape | static `SectionDrafting` with 3 methods (`CreateView`, `LinearReference`, `WriteTable`) + request records | `DraftingCatalogReader` → `SectionDraftPlanner.Plan(catalog, request)` (policies as records, paper-mm, ids) → `SectionDraftApplier`, `TextTableApplier`, `DimensionApplier` | `DraftingChoices` (ids only, window side) → `DraftingSession.Open(doc, choices, nameStyle)` in `Execute`: `CreateSection`, `TryLinearReference`, `DimensionFaces`, `WriteTable`, `Warnings` |
| B-16/B-48 | contained at the facade (ids in); settings still need Beam's `ForRun` | impossible by construction (catalog read at Run) | impossible by construction (window holds ids only, resolved at Run) |
| B-18/B-47 | fixed at apply (`IsValidViewTemplate`) | fixed in planner rule + apply check | fixed in `TemplateChoice` + per-view check |
| B-17 | row height from the created view's scale | paper mm × actual view scale | `view.Scale` after template |
| xUnit reach | naming, table layout, row height, scale policy | widest: + view-type choice, template filter, dimension chain, box characterization | naming, template choice, table layout, row height, scale policy |
| Cost / risk | smallest diff; settings classes untouched (B-48 half-fixed) | largest: catalog + 3 policy families + two-step plan→apply→plan; box-convention records | medium; one class + ids record; lazy view-type duplication inside a transaction; god-object pull (cap at 4 ops) |

Common to all three: pure helpers in `HPRebar.Core/Shared/Drafting/` — `UniqueViewName` (styles `SuffixLetter`, `KataParenthesised`), `TemplateChoice`, `TableLayout`, `AnnotationScale.RowHeight`, `ScalePolicy` — with xUnit tests.

## Recommendation: C + the common Core helpers
C turns the window/Run split that caused B-16/B-48 into a type boundary (ids before Run, elements only inside `Execute`), which A leaves to each feature and B achieves with much more surface. Keep B's two ideas that cost little: a characterization test pinning today's Column/Beam/Kata boxes, and `FaceReferences.ToLinear` as the single home of the SURFACE→LINEAR rewrite (inside the session). Cap `DraftingSession` at resolution + 4 operations; geometry stays in features.
Trade-off accepted: a per-run object (IDisposable as a use-after-run fence, no real resource) and lazy view-type duplication inside the caller's transaction.

## Slices (refactor never mixed with fixes)
| # | Slice | Type | Blocked by |
|---|---|---|---|
| 0 | Fix track: B-47/B-48 (port Beam fixes to Column), B-17 in both (row factor re-tuned), B-45 (unblocks Column golden run) — own commits | HITL (behaviour change approval + visual check 1:25/1:50/1:100) | — |
| 1 | Core helpers + xUnit (tracer-bullet TDD) + box characterization tests | AFK | — |
| 2 | `DraftingSession` with `CreateSection`; Beam migrates | AFK build + golden run (HITL) | 1 |
| 3 | Column migrates (`DraftingChoices` at window open) | AFK + golden run (HITL) | 0, 2 |
| 4 | `TryLinearReference` + `DimensionFaces`; delete both `ToLinearReference` | AFK + golden run | 2 |
| 5 | `WriteTable`; delete `WriteRow`/`RowHeight` copies | AFK + golden run + visual (HITL) | 0, 4 |
| 6 | Kata creation path (keeps reuse/recrop/storage) | HITL live on a Kata scratch copy | Kata freeze lifted |
| 7 | ADR-0004 note (Core `Shared/Drafting`), REFACTORING_LOG entry | AFK | 5 |

Gates per slice: `dotnet build HPRebar.slnx -c Debug.R26 -p:DeployAddin=false`, `dotnet test HPRebar.Core.Tests`, golden run identical (`HPRebar/tools/golden-run`) except where slice 0 changed behaviour on purpose.

## Open decisions for the user
1. Approve the RFC into a wave (proposed W6, after slice 0) — refactoring never starts without it.
2. Slice 0 behaviour changes: B-17 row height will differ from today's output; approve the fix track first.
3. Naming: keep both "+A" (Column/Beam) and "(Kata n)" styles, or unify (product decision; default keep both).

## Risks
- Column golden run refused by B-45 until slice 0 → Column slices blocked.
- Kata files under concurrent edits → slice 6 waits.
- Box conventions differ (Column/Beam BasisX = across, Kata −right) → characterization tests in slice 1.
- `RevitUnits` ×4 and KataRebar→BeamRebar references (AUD-001/004) stay out of scope.
