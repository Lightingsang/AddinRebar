# Code review — phase 3 (Kata column stack: Core rebar plan from sheet `Cot`)

Mode: read-only on source. `dotnet test HPRebar.Core.Tests` → 1766/1766 pass. Findings marked **probed** were reproduced with a scratch console (net8, project reference to `HPRebar.Core` + `KataColumnTestSheet.cs`) outside the repo; the rest are static. VC parity of the new probes not checked against Kata (CHƯA TEST).

## Scope

`HPRebar.Core/KataRebar/ColumnStack/` — `KataColumnSpecModels.cs`, `KataColumnSpecReader.cs`, `KataColumnSectionPlanner.cs`, `KataColumnHoopPlanner.cs`, `KataColumnBarModels.cs`, `KataColumnBarPlanner.cs` (792 lines) + `HPRebar.Core.Tests/KataRebar/ColumnStack/*` (342 lines).

## Probe results

| Scenario | Result |
|---|---|
| J = 1550 (Kata sample value), 50 mm face step at FLOOR 1 | 7 bars with a segment going **down** (z −1550 → −1600) |
| Upper storeys 1400 wide over 1000 | 0 notes; lower bars end at x 52…948, upper bars at x −130…1130 start at base + J, nothing laps or anchors them |
| E 7 → 5 between storeys | 4 upper layer-1 bars with no lower bar within 60 mm; 2 lapped beside a bar of the other group (laps never overlap) |
| 800×800 square → Ø800 round, same extents | 0 notes, no cut; 20 of 24 lower bars run up outside the round column's cover circle |
| Storeys shifted +10 m (Revit datum ≠ sheet C5) | starter runs −5400 → 7250 (12.6 m) under a base at 6000 |
| F = 1 on 6 slots / F = 5 with G = 3 | 0 layer-2 bars, no error, no note |
| Storey 700 high, beam 600 | hoop zone (50, 50); clear < 100 gives `ToMm < FromMm` |

## Findings

| # | Sev | File:line | Problem | Fix |
|---|---|---|---|---|
| 1 | Critical | [KataColumnBarPlanner.cs:159-163](../../../HPRebar/HPRebar.Core/KataRebar/ColumnStack/KataColumnBarPlanner.cs#L159-L163), [:138](../../../HPRebar/HPRebar.Core/KataRebar/ColumnStack/KataColumnBarPlanner.cs#L138) | The crank (soffit point) and the cut end (`TopMm − cover − hoop − Ø/2`) are appended after the bottom lap's crank-back point (`lapTop + 6d`) without checking z order. With J + 40d + 30d + 6d > clear height, which happens with the sample's own J 1550 and a 600 beam, the polyline goes down or folds back (**probed**: 7 bars). Revit then builds a self-overlapping or invalid curve loop. | Check each run is z-monotonic and has no segment shorter than the Revit short-curve tolerance. If the crank cannot sit above the lap, either move the crank above the lap (if it stays below the slab top) or report a per-storey error ("J{row} too high for a crank under the beam"). Add a test with J 1550 and a step. |
| 2 | High | [KataColumnBarPlanner.cs:168-177](../../../HPRebar/HPRebar.Core/KataRebar/ColumnStack/KataColumnBarPlanner.cs#L168-L177) | **The upper column is larger.** `ShiftInto` only clamps the lower bars into the upper zone. When the upper storey grows, nothing moves and nothing is cut, so the bars on the new outer faces start at base + J with no bar to lap and no dowel into the storey below (**probed**: 0 notes). | When an upper bar has no lower bar of the same group within the lap tolerance, give it starter dowels (30d or F5·d into the storey below), or add a plan error. This is the user's decision: record it like the crank/cut rule. |
| 3 | High | [KataColumnBarPlanner.cs:145](../../../HPRebar/HPRebar.Core/KataRebar/ColumnStack/KataColumnBarPlanner.cs#L145), [:199-212](../../../HPRebar/HPRebar.Core/KataRebar/ColumnStack/KataColumnBarPlanner.cs#L199-L212) | **Laps are never paired.** The planner assigns groups separately per storey (atan2 index parity), and a lower bar ends at its own group's lap end. If E, G, F or H change, or the width changes so the intermediate pitch shifts (Clamp leaves bars that are inside the zone where they are), an upper bar can sit beside a bar of the other group. Their laps then do not overlap at all (A ends at start + 30d, B starts at start + 40d). It can also sit tens of mm from any lower bar (**probed** E 7→5: 2 group mismatches, 4 unpaired). | Pair each upper bar with the nearest lower bar (same layer) and take the group from that lower bar. Or validate: every upper bar needs a same-group lower bar within, say, max(4d, 50 mm), else a note or error. Assert pairing in a test whose E changes between storeys. |
| 4 | High | [KataColumnBarPlanner.cs:172-176](../../../HPRebar/HPRebar.Core/KataRebar/ColumnStack/KataColumnBarPlanner.cs#L172-L176) | **Round columns in the crank/cut logic.** The bar zone is always the bounding box, so a bar of a square storey inside the box of a round upper storey "fits". Square 800 → Ø800 round: 20 of 24 bars run up outside the round concrete, with no note (**probed**). The same applies to round → round with an eccentric centre. | When the upper storey is round, clamp to the circle of radius R − inset around its centre (radial move). Then the 75 mm crank limit compares the radial move. |
| 5 | High | [KataColumnBarPlanner.cs:81](../../../HPRebar/HPRebar.Core/KataRebar/ColumnStack/KataColumnBarPlanner.cs#L81) | The starter foot elevation is the sheet's absolute C5 (`PileCapBottomMm`) mixed with Revit storey elevations. Any datum offset between Kata's levels and the Revit model (project base, survey point, or a sheet that was not refreshed) makes the starter too long or reversed (**probed**: 12.6 m starter). The tests pass only because the fixture puts C5 = −5.5 on the same datum as the storeys. | Take the cap bottom as `storeys[0].BaseMm − PileCapHeightMm` (B9). Or validate `bottom < storeys[0].BaseMm − lap` and that it matches B9 within tolerance, else an error. K1: one source of truth for the cap. |
| 6 | High | [KataColumnBarPlanner.cs:187-192](../../../HPRebar/HPRebar.Core/KataRebar/ColumnStack/KataColumnBarPlanner.cs#L187-L192), [:184](../../../HPRebar/HPRebar.Core/KataRebar/ColumnStack/KataColumnBarPlanner.cs#L184) | The lap length and the B offset come from the **group** (I a → A, I b → B), while the bar's Ø comes from its **layer** ([KataColumnSectionPlanner.cs:194-221](../../../HPRebar/HPRebar.Core/KataRebar/ColumnStack/KataColumnSectionPlanner.cs#L194-L221), cell contract row I = layer 1 / layer 2). With "25/20", a layer-1 Ø25 bar in group B laps 600 mm = 24d, and a Ø20 bar in group A laps 37.5d. It matches the VC chain on layer 1 (the test pins it), but the next phase will place real under-lapped Ø25 bars. | Present this to the user as a parity-vs-code decision: lap = max(rule by group, 30·Ø of the bar itself), or keep Kata's lap and add a note listing the bars under 30d. Do not change it silently. |
| 7 | Medium | [KataColumnSectionPlanner.cs:160-175](../../../HPRebar/HPRebar.Core/KataRebar/ColumnStack/KataColumnSectionPlanner.cs#L160-L175), [:204-205](../../../HPRebar/HPRebar.Core/KataRebar/ColumnStack/KataColumnSectionPlanner.cs#L204-L205) | Bars the user typed are dropped silently: an odd count on an even slot count (F = 1, E = 6 gives 0 bars), F/H when G/E < 4 (**probed**), and F = 2 is absorbed by the layer-1 side bars. G3 "ghép sát" (`TightInnerLayer`) and H3 dense hoops at laps (`DenseAtLaps`) are read but ignored, so a ticked box gives a section or hoop layout Kata does not draw. | Return a note or error per dropped count from `Plan` (the section already has a record to carry it). Refuse G3/H3 = true with an error naming the cell until they are implemented (K4: no unused flags pretending to work). |
| 8 | Medium | [KataColumnBarPlanner.cs:88](../../../HPRebar/HPRebar.Core/KataRebar/ColumnStack/KataColumnBarPlanner.cs#L88), [:141](../../../HPRebar/HPRebar.Core/KataRebar/ColumnStack/KataColumnBarPlanner.cs#L141) | Feet and legs use `Outward`/`In` of the nearest face. Layer-2 bars sit on the same grid column as layer-1 bars, so an outer bar's inward top leg passes **exactly** through the inner bar's end point (same x, z within 2.5 mm), and an inner starter foot passes through the outer starter's corner. The cut-step legs (inward, 15d) also cross the upper storey's dowels at the same z. These are guaranteed clashes in Revit. | Offset the layer-2 legs/feet by one bar Ø in z (or turn them along the face), and keep the cut legs clear of the upper bar zone. Add a test that no two runs intersect within (Ø1 + Ø2)/2. |
| 9 | Medium | [KataColumnBarPlanner.cs:196](../../../HPRebar/HPRebar.Core/KataRebar/ColumnStack/KataColumnBarPlanner.cs#L196), [:169](../../../HPRebar/HPRebar.Core/KataRebar/ColumnStack/KataColumnBarPlanner.cs#L169), [:229-230](../../../HPRebar/HPRebar.Core/KataRebar/ColumnStack/KataColumnBarPlanner.cs#L229-L230) | Magic tolerances 0.5 / 0.5 / 1 mm (N9). A 0.5–5 mm Revit face noise (e.g. 399.6) produces a real 3–30 mm crank jog per bar. `KataTolerance.StationMm` already exists. | Use one named tolerance (`KataTolerance.StationMm` or a column constant ≥ a few mm) for "no move", and reuse it in `NeedsCut`. |
| 10 | Medium | [KataColumnBarPlanner.cs:10](../../../HPRebar/HPRebar.Core/KataRebar/ColumnStack/KataColumnBarPlanner.cs#L10), [KataColumnSpecReader.cs:51](../../../HPRebar/HPRebar.Core/KataRebar/ColumnStack/KataColumnSpecReader.cs#L51), [:168](../../../HPRebar/HPRebar.Core/KataRebar/ColumnStack/KataColumnSpecReader.cs#L168) | K1 duplication: `CrankSlope = 6` duplicates `KataDetailingRules.CrankSlope` (beam rules, user-settable through Kata Settings), and the lap 30d / F5 default 15 / lift 100 are new literals beside the beam rule set. Changing the crank slope in Settings will not reach columns. | Build `KataColumnBarRules` from the same settings/rule builder (`KataDetailingRuleBuilder`), or name the defaults once with the TCVN/Kata source in a comment. |
| 11 | Low | [KataColumnHoopPlanner.cs:303-318](../../../HPRebar/HPRebar.Core/KataRebar/ColumnStack/KataColumnHoopPlanner.cs#L303-L318) | No guard for clear ≤ 2·EndGap or beam ≥ height (zone To < From, **probed**). The planner also uses the sheet b/h, while bars use the real faces (`OnRealFaces`), so Ld can differ from the Revit section. | Refuse (or return no zone) when `clearTop < EndGapMm`, and pass the real-face row as the bar planner does. |
| 12 | Low | [KataColumnBarPlanner.cs:206-212](../../../HPRebar/HPRebar.Core/KataRebar/ColumnStack/KataColumnBarPlanner.cs#L206-L212), [KataColumnSectionPlanner.cs:263-271](../../../HPRebar/HPRebar.Core/KataRebar/ColumnStack/KataColumnSectionPlanner.cs#L263-L271) | **Round sections.** An odd E + F (7 bars) gives A/B = 4/3, with bars 0 and 6 adjacent and both A (two laps at the same level). The I "a/b" inner Ø is ignored (every bar uses the outer Ø). AutoTies with ≥ 3 pairs (E ≥ 13) produces nested closed ties that no VC variant covered. | Add a note for an odd round count. Document or refuse "a/b" on round sections. Add a note "tie layout extrapolated" when pairs > 2. |

The rectangle layer-1 count is 2E + 2G − 4, always even, so the atan2 alternation gives equal halves there. Layer 2 is symmetric, so it is even as well. Only round sections can split unevenly (#12).

## Positive

- Pure, host-free, deterministic. Every sheet error is reported per cell, and the sheet is never guessed (J required: user decision honoured, test `Plan_EmptyJ_RefusedWithTheCells`).
- Chains A/B are pinned to the VC j-all500 drawing to the millimetre. Hoop Ld is pinned to TCVN 9386 with the variant evidence in the plan. Reuses `KataBarNotationParser` (no second hoop parser).
- Files stay under 300 lines, methods are short, and the XML docs give units (CM5). No plan or finding codes in the code (CM6).

## Tests gap

The tests cover only one geometry: a constant 1000×800 section, the same counts on every storey, and J = 500. None covers a growing section, count changes, round columns, large J, datum offset, or z-monotonicity. Add a property-style test over all runs: z non-decreasing, no zero-length segment, inside the storey section at every z.

## Score

**5.5/10.** The rules taken from the VC drawings are implemented faithfully and the tests are clean. But the geometry the next phase hands to Revit has three reproducible defect classes: folded polylines, unanchored or unpaired laps, and bars outside a round or larger section. It also has a silent datum dependence. Fix #1–#5 before the Revit phase. #6 needs the user's decision.

**Status:** DONE_WITH_CONCERNS
**Summary:** 12 findings (1 Critical, 5 High). Tests 1766/1766 pass. Six of the findings were reproduced by a scratch probe.
**Concerns/Blockers:** #6 (lap per group vs per bar Ø) is a parity-vs-code-safety decision for the user.

## Fix round (2026-11-03)

User decisions: larger storey above → dowel the unpaired bars; every lap ≥ 30 × the bar's own Ø.

| # | Outcome |
|---|---|
| 1 | ✅ crank starts at max(soffit, last point); every run cleaned (< 2 mm segments merged) and checked to rise — else error naming the J cell; theory test J 500/1550 × step 0/50/200 |
| 2 | ✅ unpaired bars dowel one lap into the storey below (inside its bar zone) or get an F5·d foot in the floor beam; note per storey; test |
| 3 | ✅ groups paired: each bar takes the group of the nearest lower bar of its layer within 60 mm (after its crank); unpaired alternate; test E 7→5 |
| 4 | ✅ round upper storey clamps radially to its bar circle (square → Ø: cut note); test |
| 5 | ✅ starter foot = storey 0 base − B9 + lift, never C5; test with a +10 m datum |
| 6 | ✅ lap = max(Kata lap, 30 × own Ø) — two chain values differ from VC by design (11650, 8550/12450), pinned in tests |
| 7 | ✅ dropped layer-2 bars and round "a/b" reported as notes; G3 and H3 refused with an error until implemented |
| 8 | 🟡 layer-2 starter feet one outer Ø higher, layer-2 top/cut ends one outer Ø lower; cut legs vs dowels above still cross (accepted, Revit allows touching bars) |
| 9 | ✅ one `FaceToleranceMm` (2) for steps/corners, `PartnerReachMm` 60, `ShortestSegmentMm` 2 |
| 10 | 🟡 `CrankSlope` is a rule parameter: the Revit phase passes the Kata settings' crank slope; 30d/10d stay named rule defaults |
| 11 | ✅ no clear height → only the joint zone; hoop zones use the real faces through the bar planner (direct calls take the sheet row) |
| 12 | ⏭ odd round counts split ceil/floor; ties for ≥ 3 pairs (E ≥ 13) remain an extrapolation |

Checks: Core 1778/1778 (12 new).
