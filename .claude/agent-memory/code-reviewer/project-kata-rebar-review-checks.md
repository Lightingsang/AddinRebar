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

- Section n-n drawing (`KataSectionDrawingBuilder`, 2026-10-03): tag rows are fixed offsets fitted to 2-layer DY7/DY14 samples. 3+ top layers (C15/E15/G15 = "2f18") stack inner-layer tags 43 mm apart at one insertion; ≥ 2 bottom middle numbers (D18 = "2f20+2f22") put a row 31 mm above the width dim (dimZ allows one extra row only). Base DY7 already has side-bar vs inner tag 119 mm apart — that is Kata's own, not a bug.
- Canvas section cache keyed by cut Number is sound: `KataSectionCuts.Content` covers everything the builder reads.

- Canvas bar-group removal (`KataLayoutRemoval`, 2026-10-04): `KataStirrupRuns.Of` merges same-span/number/spacing zones without a contiguity check, so striking a mid-span zone fuses both dense zones into one a100 run (lines, dims, tags, selection). Removal never touches `BarSets` (side ties, layer-spacer ties, inner stirrups) — striking everything still leaves 58 bars / 10 kg on DY7. Keys use the ordinal `BarId`: one extra bar earlier in the order makes 11/28 old keys name a different bar, and the `unknown` check cannot see it.

**Why:** golden tests (DY7: 3 spans, Ø18, symmetric cells, no cantilever) cover none of these.
**How to apply:** probe with a scratch console (`ProjectReference` to HPRebar.Core.csproj, copy `KataDy7DrawingTests.Sheet()` body, `KataCellTable` lives in Core) and mutate cells (`E14 = "2f20;2f18"`, `J7 = "a5"`, `I11 = 0` for a right cantilever). Run `dotnet test` from `HPRebar/` or the test folder (see [[project-dotnet-test-zero-tests-from-repo-root]]). BarId has no consumer outside tests — renumbering is safe.
