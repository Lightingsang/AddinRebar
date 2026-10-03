---
name: project-kata-rebar-review-checks
description: Recurring defect classes in HPRebar.Core KataRebar layouts (ties, layer groups, sheet cells) and a fast offline probe recipe
metadata:
  type: project
---

Checks that found real defects in the Kata rebar core (2026-10-02 layer-spacer-tie review):

- Asymmetric "left;right" support cells produce two sub-rows (`…T`/`…P`) with the SAME `HostSupportIndex` + `Layer`; anything grouping by those keys merges two cross-sections (bar-count thresholds and "outer bars" go wrong).
- `KataTieWrap` offsets a wrapping tie by the tie's bend radius only — fine for side bars Ø12–16, clashes with wrapped bars ≥ Ø22. DY7 golden sheet uses Ø18 so tests never see it.
- Sheet spacing cells (J7 etc.) parsed with "any v > 0" — probe a typo like `a5`: thousands of bars, no warning.
- LikeHoops-style "+2Ø along X" shifts point outward at a RIGHT cantilever tip.
- Bottom row 17 is `Layer == 2` even when it is seated on the stirrup (no level beneath).

**Why:** golden tests (DY7: 3 spans, Ø18, symmetric cells, no cantilever) cover none of these.
**How to apply:** probe with a scratch console (`ProjectReference` to HPRebar.Core.csproj, copy `KataDy7DrawingTests.Sheet()` body, `KataCellTable` lives in Core) and mutate cells (`E14 = "2f20;2f18"`, `J7 = "a5"`, `I11 = 0` for a right cantilever). Run `dotnet test` from `HPRebar/` or the test folder (see [[project-dotnet-test-zero-tests-from-repo-root]]). BarId has no consumer outside tests — renumbering is safe.
