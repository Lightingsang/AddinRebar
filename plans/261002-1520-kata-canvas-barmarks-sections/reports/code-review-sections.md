# Code review — Kata section cuts (Đợt 2), uncommitted

Scope: [KataSectionCuts.cs](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataSectionCuts.cs) (new, 102 l), [KataElevationSectionPainter.cs](../../../HPRebar/HPRebar/KataExport/View/Controls/KataElevationSectionPainter.cs) (rewritten, 214 l), [KataSectionCutsTests.cs](../../../HPRebar/HPRebar.Core.Tests/KataRebar/KataSectionCutsTests.cs) (new, 49 l).
Not re-litigated: cut-position formula, Kata golden positions/numbers, KataBarNumbering.

## Checks run

| Check | Result |
|---|---|
| `dotnet test HPRebar.Core.Tests` | ✅ 874/874 |
| `dotnet build HPRebar/HPRebar.csproj -c Debug.R26 -p:DeployAddin=false --artifacts-path <scratch>` | ✅ 0 errors, no warning in the 2 files |
| Scratch probe (console over the built Core + test-sheet builders, scratchpad only) | DY7 / DY14 / DY14 E=500 per-cut contents dumped; synthetic variants: left cantilever, zero span, 999/1000/2999 mm spans, G6 = 0, cut at bar end ±0.5/±1.5 mm |
| Revit / canvas visual | CHƯA TEST (not asked; no Revit) |

Probe facts (verified by probe output):
- Golden contents match the brief: DY14 cut 9 (x 15825, span 3) and the span-4 cut (16425) have identical bars `1,4,9`, hoop `19a100`, no sets → shared 9.
- Zero span → no cut, no exception. 999 mm span → 1 cut; 1000 mm → 3 cuts (100/450/900 from face). G6 = 0 → no zone, `Hoops` null, no throw.
- `Crossing` includes a bar whose end is ≤ 1 mm from the cut, excludes it at 1.5 mm (as designed). Nearest bar vertex to any golden cut ≥ 50 mm, so the golden is not tolerance-sensitive.
- Number reuse is by content, not adjacency: with span 4 = 999 mm the sequence is `1..9, 8`; with G6 = 0 it is `1..7, 8, 8, 8`. Consistent with the stated rule.

## High

None found.

## Medium

### M1 — A bar number shared by a top and a bottom bar gets one label; the other bars carry no number
[KataElevationSectionPainter.cs:118-139](../../../HPRebar/HPRebar/KataExport/View/Controls/KataElevationSectionPainter.cs#L118-L139)
`GroupBy(BarNumber)` then one row chosen by `group.Average(Z) > -h/2` and one leader to the nearest dot. KataBarNumbering gives the same number to mirror-identical bars in different rows — verified on DY7: `#6` is `ExtraTop L1 (y0, z-42)` at cuts 3/4 and `ExtraBottom L2 (3 bars, z-510)` at cut 5 (= plan's "6 E13 = F17").
Failure: a beam whose bottom extra of span F runs into the support zone of E (any bottom extra longer than the gap to the L/10 cut) puts both rows of `#6` in one section → average z ≈ (−42 + 3·−510)/4 = −393 < −h/2 → one label in the bottom row with a leader to a bottom bar; the top extra in that section has no number. Not hit by DY7/DY14 (the two never meet in one cut there).
Fix: group by `(BarNumber, upper)` — compute `upper` per crossing (`z > -h/2`) and side per bar — so each row gets its own circled number.

### M2 — Inner U/C stirrups are drawn in the card but numbered nowhere
[KataElevationSectionPainter.cs:89-92](../../../HPRebar/HPRebar/KataExport/View/Controls/KataElevationSectionPainter.cs#L89-L92)
Footer lists the hoop and `sets.Where(KataBarNumbering.IsTie)`; non-tie sets (KataInnerStirrupLayout) are painted by `PaintSet` but their `BarNumber` is never printed. The elevation tags do not label them either (KataBarTagBuilder: "Stirrups are labelled on their zones and C ties only in sections"; inner-stirrup sets are not zones).
Failure: a wide beam with inner stirrups (rows 25–44) → its Schedule Mark exists in Revit but no section or tag on the canvas shows it. No golden beam has inner stirrups, so tests cannot catch it.
Fix: add `(n) Ø{set.Diameter} a{set.Spacing}` for non-tie sets (or a circled number on the set's top leg).

### M3 — Cantilever cuts are unverified and the tip cut is near-empty
[KataSectionCuts.cs:50-61](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataSectionCuts.cs#L50-L61)
For a cantilever span (support width 0) `SpanStart` is the free tip, so the first cut sits L/10 from the tip. Probe (DY14 sheet with C11 = 0, D11 = 1500): cut 1 at x = 150 crosses only `#1 ×2 + #8` — no bottom main bars, no top extras — card reads "Trên 2 · Dưới 1"; and it gets its own section number, shifting every later number by one vs a non-cantilever beam. Neither the positions nor the content were checked against a Kata cantilever drawing, and no test covers it (the golden beams have no cantilever).
Fix: until a Kata cantilever DWG is read, either skip the tip-side cut on a cantilever span (`IsLeftCantilever && s == 0` / `IsRightCantilever && s == last`) or mark it GIẢ ĐỊNH CHƯA XÁC MINH in the plan and pin current behaviour with a test so a later change is deliberate.

### M4 — No tests for the helpers the painter relies on
[KataSectionCutsTests.cs](../../../HPRebar/HPRebar.Core.Tests/KataRebar/KataSectionCutsTests.cs) covers only `Build` on 3 golden sheets. Untested: `Stations` boundaries (999 / 1000 / 2999 / 3000 mm, zero span), `Crossing` tolerance at a bar end and on a cranked (sloped) segment, `Hoops` between/outside zones, `Sets` with `Count = 1`, and number reuse when a later section repeats an earlier one (`1..9, 8`). All behave sensibly in the probe, but nothing pins them. Cheap to add with the existing sheet builders (`KataDy14DrawingTests.Sheet()` + `t.Set("J11", 999.0)` etc.).

## Low

| # | Where | Scenario | Suggestion |
|---|---|---|---|
| L1 | [Painter:134](../../../HPRebar/HPRebar/KataExport/View/Controls/KataElevationSectionPainter.cs#L134) | `PlaceNear` returns null after ±60 px → `continue` silently drops a number. ~7 labels wanting the centre fill the ±60 band (13 px + 4 px gap); a support cut with main + 2 extra layers + spacer rows is 4–5 today, so only dense layouts hit it — but then the section shows a bar with no number and no hint | widen `maxShift` to the card half-width, or fall back to a second row |
| L2 | [Painter:124-129](../../../HPRebar/HPRebar/KataExport/View/Controls/KataElevationSectionPainter.cs#L124-L129) | side-label column only grows downward (`sideY + 16`); with ≥ 4 side-bar numbers in a shallow section it runs into the bottom row (`Y(-h)+12`) / footer (`bottom-34`). Horizontally it fits (max lx = left + 193 < left + 220) | clamp `ly` to `bottomRowY - LabelRadius` and stack upward when full |
| L3 | [KataSectionCuts.cs:99-100](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataSectionCuts.cs#L99-L100) | sets enter the key as `Distinct()` numbers; every C tie of one Ø shares one number, so "side-bar ties + layer-spacer ties" and "side-bar ties only" give the same key. Two cuts with the same bars but a different tie arrangement would share a number while the cards differ. Needs mirror-equal bars, so mostly theoretical | key on `number@Z` of each set's first point instead of the bare number |
| L4 | [KataSectionCuts.cs:71](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataSectionCuts.cs#L71) | segments with ΔZ > 1 mm are skipped, so a cut that lands on a crank misses the bar. E=500 crank runs 6100→6700, face at 6600: a span of 1000–1124 mm (offset rounds to 100) puts the cut on 6700 — kept by the 1 mm tolerance; a crank reaching > 101 mm past the face would drop the bottom bar from the section | interpolate Z on sloped segments instead of skipping them |
| L5 | [KataSectionCuts.cs:80-84](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataSectionCuts.cs#L80-L84) | `Hoops` returns the nearest zone even when the cut is far outside every zone (e.g. a span whose zones are all empty except one) → footer prints that zone's spacing. Harmless for normal spans | return null when the distance exceeds one spacing |
| L6 | [Painter:179-184](../../../HPRebar/HPRebar/KataExport/View/Controls/KataElevationSectionPainter.cs#L179-L184) | selecting a zero-length (or < 1 m measured vs sheet mismatch) span column finds no cut inside and falls back to the nearest cut of a neighbouring span; the card title shows that span's number, which reads as "wrong span" | acceptable; or show no card / a "không có mặt cắt" note |
| L7 | [Painter:58](../../../HPRebar/HPRebar/KataExport/View/Controls/KataElevationSectionPainter.cs#L58) | `x = {localX}` is the plan's local station; in a reversed drawing (`Direction = -1`) it counts from the far end, so it does not match the drawing's dimension chain | print `_map.ToStation(cut.X)` or the distance from the span's left face in drawing order |
| L8 | [Painter:197-202](../../../HPRebar/HPRebar/KataExport/View/Controls/KataElevationSectionPainter.cs#L197-L202) | marker numbers are drawn at `Y(bottom)+6` without a `KataLabelLane`; close cuts (DY14 span 4 is 600 mm from cut 9, 15 px at a 420 px canvas fit) and the under-beam annotations can overlap. Visual, CHƯA TEST | reuse a lane like the bar tags |
| L9 | [Painter:62-64](../../../HPRebar/HPRebar/KataExport/View/Controls/KataElevationSectionPainter.cs#L62-L64) | `b = 0` and `h = 0` together → `scale = ∞`, `0·∞ = NaN` coordinates passed to WPF. Same formula as before; only reachable with an invalid sheet the planner did not block | guard `b <= 0 || h <= 0 → return` |
| L10 | [Painter:90-92](../../../HPRebar/HPRebar/KataExport/View/Controls/KataElevationSectionPainter.cs#L90-L92) | footer prints `rules.StirrupDiameter` for ties instead of `set.Diameter`; equal today (both layouts use `rules.StirrupDiameter`) but breaks silently if tie Ø becomes its own setting | use `set.Diameter` |

## Not issues (checked)

- `Local()` inverse of `ToStation` is correct for `Direction = ±1`; `Selected()` uses min/max of the mapped extent, so reversed drawings pick the right span; `sideX = X(b/2·Direction)` is always the right edge.
- `Box` normalises reversed corners (`new Rect(Point, Point)`), so `Direction = -1` draws correctly.
- Card fits: guard `Height ≥ 290` vs card 12 + 270; top label row ≥ top + 37.5 clears the subtitle; bottom row ≤ bottom − 43.5 clears the footer at bottom − 34.
- Performance: `Build` per `OnRender` = ~10 cuts × ~30 bars × few segments + small string keys — sub-millisecond; no caching needed.
- Sets with `Count = 1` carry the run spacing (500 in DY7/DY14, `n1` at 13916 shows in cut 7); `Spacing = 0` is not produced by any layout; the `Max(spacing, 1)` guard is enough.
- Numbering cannot collide: `numbers.Count + 1` per new key, dictionary keyed by full content.

## Unresolved questions

None needing the user — M3 (cantilever) only needs a Kata cantilever drawing when one is available.
