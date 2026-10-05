# Code review: joint stirrups + hanger bars ("vai bò") at loads on a span. Uncommitted on a9fdede

Scope: new `KataJointStirrups`, `KataHangerBarLayout`, `KataSpanLoad`, `KataJointLoadTests`; changes in `KataStirrupZoneLayout`, `KataInnerStirrupLayout`, `KataTieStations`, `KataRebarCalculator`, `KataBarNumbering`, `KataLayoutRemoval`, `KataRebarPlanner`, the models (`KataMeasuredBeam`, `KataSpanRebarSpec`, `KataDetailingRules`, `Enums`, `KataRebarLayoutResult`, `KataRunModels`), `KataSupportRules.LoadsBetweenSupports`, and on the Revit side `KataSupportCollector.CollectWithLoads`, `KataBeamMatcher.LoadsOn`, `KataRebarTypeResolver`. Spec rows R-120 / R-121. About 450 changed LOC.

Checks run:
- `dotnet test HPRebar.Core.Tests`: 1353/1353 pass. The filtered runs confirm that the 8 `KataJointLoadTests` and `LoadsFramingIn…` ran.
- `dotnet build HPRebar/HPRebar.csproj -c Debug.R26 -p:DeployAddin=false`: succeeds, with no new warning in the changed files.
- Probes: a scratch console on HPRebar.Core, using the B01 sheet plus injected `KataSpanLoad`s (scratchpad `jointprobe/`).
- Revit: CHƯA TEST (T6). Hanger creation, hosting and the collector are not run in Revit.

Score: **6/10**.
- The DWG cases are reproduced exactly: mid-D gives joint stirrups 4850…5050 ‖ 5550…5750 and span stirrups …4650 ‖ 5950…; F gives 14350…14550 ‖ 15050…15250 and …14150 ‖ 15450….
- Inner stirrups and C ties leave the joint zones alone, the zone names are unique, and the weight and bar count include the hangers.
- The edge cases break: near the ends of the beam, on consoles, with two top bars, with two loads close together, and for many load positions (stirrup pairs 10 mm apart, gaps of 2.6 × the spacing).

## Critical
None. No data loss and no crash path were found. H1 creates bars outside the concrete, which Revit will flag or refuse, but nothing is destroyed.

## High

H1. The hanger bars are never clipped to the span or the beam, so they run outside the concrete near the beam ends and past a console tip.
- [KataHangerBarLayout.cs:48-52](HPRebar/HPRebar.Core/KataRebar/Calculators/KataHangerBarLayout.cs#L48-L52): the path is `centre ± (W/2 + 50 + rise/tan45 + 150)`, about ±1 290 mm under a 900-deep crossing beam, and nothing checks it against `st.SpanStart/SpanEnd` or the beam ends.
- Probe, load 350 from the face of C (the same load as `A_load_near_a_support_face…`): hanger points (-533,-50) (-383,-50) (500,-933)… The bar starts **533 mm before the outer face of C** (x = 0) and crosses the whole column.
- Probe, stub column 300 wide at 1700 on the right console N (31600…33600): the hanger reaches 34450, **850 mm past the tip**.
- Probe, console filled by a 1 800-wide load: the hanger runs 30700…34500, through the support and into span 4, and past the tip.
- A load within about 1.3 m of an interior support pushes the 45° leg and the top leg across the column into the next span's top zone.
- The test `A_load_near_a_support_face…` passes because it only asserts the joint stirrups.
- Rules: M8 (precondition), K1 (the beam extent is the authoritative limit).
- Fix:
  - Clamp the two top legs to `[st.SpanStart[s] - SupportWidth(left), st.SpanEnd[s] + SupportWidth(right)]`, and never beyond `0 + cover` / `TotalLength - cover`.
  - At a console tip, stop at `tip - StirrupCover`.
  - When the slope itself does not fit (the face is closer than `run` to the limit), shorten the top leg to 0. If that is still not enough, drop that side's slope and warn "vai bò … sát gối / đầu console".
  - Pin it with tests at 350 from C, at the console tip and with the console filled.

H2. With two top main bars, both hanger bars land at the same place (y = 0).
- [KataHangerBarLayout.cs:85-95](HPRebar/HPRebar.Core/KataRebar/Calculators/KataHangerBarLayout.cs#L85-L95): the code falls back to the corner position only for `Count < 2`. With `Count == 2`, `gap = (ys[1]-ys[0])/2` is half the beam's inner width, so `ys[0] + gap == ys[1] - gap == 0`.
- Probe, B11 = C14 = "2f25": `hanger y0.0` twice, with an identical polyline. `KataLongitudinalSetGrouping.IsEven` rejects a step of 0, so Revit gets two single bars that coincide.
- Fix: inset by `(D + d)/2 + clear` (clear ≥ max(d, 25)) from the outer bars whenever `Count <= 2`, or whenever `gap` would put the two positions within `d` of each other. Assert `|y0 - y1| > d` in a test with 2 bars.

H3. Joint stirrups of neighbouring loads coincide, and their removal keys collide.
- `KataJointStirrups.Apply` lays each load on its own ([KataJointStirrups.cs:35](HPRebar/HPRebar.Core/KataRebar/Calculators/KataJointStirrups.cs#L35), [:50-55](HPRebar/HPRebar.Core/KataRebar/Calculators/KataJointStirrups.cs#L50-L55)). `LoadsBetweenSupports` merges only extents that overlap.
- Probe, two 300-wide loads 600 apart in span 1: zones "Đai gia cường 1 phải" and "Đai gia cường 2 trái" both have stations 4600,4650,4700,4750,4800. The minimum gap is **0**, which gives 5 duplicate Revit hoops. Both zones get the removal key `zone|0|0|5|4600`, so striking one strikes both, and `Fingerprint` carries a duplicate.
- The two hangers overlap over 4800…5483 at the same Y.
- Any two loads whose faces are less than 2·(50 + 4·50) + spacing ≈ 700 apart interleave.
- Rules: K1, T4 (no test with more than one load per span, although the code numbers them).
- Fix:
  - Group the loads of a span whose joint windows overlap. Lay the joint stirrups from the outer face of the first load to the outer face of the last, at ≤ a50, and merge the inner sides into one evenly spaced run.
  - Alternatively, take the union of the stations, dedupe within `JointStirrupSpacing/2`, and re-space.
  - Hangers: one pair spanning the group, or warn.
  - Make the removal key include the zone name, or give joint zones their own `ZoneIndex`.

H4. `Cut` leaves stirrup pairs 10 mm apart and gaps of up to 2.6 × the spacing.
- [KataJointStirrups.cs:80-92](HPRebar/HPRebar.Core/KataRebar/Calculators/KataJointStirrups.cs#L80-L92). A scan of span 1 with the load centre at 700…9700 in 10 mm steps found two defects.
  - **Minimum gap 10 mm** (load at 1990): "Gối trái [2]" = 2990, 3000. `Even(Math.Max(hi, first), last)` with `hi` 10 mm before `last` returns two stations, because `Even` only collapses spans under 1 mm.
  - **Gap of 390 mm in an a150 zone** (load at 3440): the dense zone keeps 3000, and the next stirrup is the first joint stirrup at 3390. The window `[3390 - 200, …]` starts before the middle zone's first station 3200, so the middle part before the joint is dropped, and the dense zone ends at its old last station because `lo(dense) = 3240 > 3000`. Nothing fills 3000…3390.
- R-120 says "đai của nhịp dừng/nối lại cách đai gia cường một bước của vùng, chia đều lại". Both defects break that rule.
- Fix:
  1. In `Cut`, drop a remainder shorter than `spacing/2`, keeping only `first` or `last`.
  2. After cutting all runs of the span, check the gap between the last span station before the joint group and the first joint stirrup. If it exceeds that run's spacing, extend the run before the window with `Even(prevLast, lo, spacing)`. Do the same on the right side.
  3. Property test: for every load position in a span, every gap is ≤ max(spacing of the neighbouring zones) and ≥ 0.5 × the joint spacing, except across the load footprint itself.

## Medium

M1. In sections the hanger bars count as bottom bars, but the elevation canvas never draws them.
- [KataSectionCuts.cs:71](HPRebar/HPRebar.Core/KataRebar/Calculators/KataSectionCuts.cs#L71) iterates `LongitudinalBars`, which now includes the hangers. [KataSectionBars.cs:72](HPRebar/HPRebar.Core/KataRebar/Calculators/KataSectionBars.cs#L72) treats every non-side bar whose role is not MainTop/ExtraTop as a bottom-face bar.
- Probe: the mid-span cut of span 1 at x = 5450 (mid − 150) falls exactly under the crossing beam. It crosses both hangers at z −933, which the section draws in the bottom layer-1 row beside the 6Ø25, untagged. The cut's `Content` changes, which can split a section number that Kata shares.
- [KataElevationDrawingBuilder.cs:77](HPRebar/HPRebar.Core/KataRebar/Calculators/KataElevationDrawingBuilder.cs#L77) lists side, main and extra bars only. The hangers are invisible on the canvas and get no tag, and the `Keep(layout.HangerBars)` in [KataLayoutRemoval.cs:83](HPRebar/HPRebar.Core/KataRebar/Calculators/KataLayoutRemoval.cs#L83) cannot be reached from the UI. The user cannot strike a hanger that Revit will create.
- The preview list ([KataRebarPreviewBuilder](HPRebar/HPRebar/KataRebar/ViewModel/KataRebarPreviewBuilder.cs)) does not list them either.
- Rule: C5 (what is computed should also be presented).
- Fix:
  - Add `HangerBars` to the elevation `Bars(...)` with its own pen/tag ("VB nØ16").
  - In the sections, either skip `HangerBar` in `Crossing`, or draw it as its own face, matching what the DWG shows in the mid-D section.
  - Add a canvas strike test.

M2. Near a support or on a console, span stirrups are left under the load.
- The cut window is built from the joint stirrups only ([KataJointStirrups.cs:43](HPRebar/HPRebar.Core/KataRebar/Calculators/KataJointStirrups.cs#L43)). When all joint stirrups are on one side, the load's own footprint is not cleared.
- Probe, load 350 from C (footprint 550…950): "Gối trái" = 450, 583, 717, **850**. Two hoops sit under the crossing beam, while a mid-span load keeps its footprint free (5050 ‖ 5550).
- Console probe: "Console [2]" = 33250, 33375 under the stub column at 33150…33450.
- Fix: window = `[min(jointMin, leftFace) - s, max(jointMax, rightFace) + s]`.

M3. A load near both supports gets no joint stirrups and no warning, and the "near" threshold is a cliff.
- [KataJointStirrups.cs:65-68](HPRebar/HPRebar.Core/KataRebar/Calculators/KataJointStirrups.cs#L65-L68): when `nearLeft && nearRight`, both counts are 0. Probe, console filled by an 1 800 load: no joint zone at all, and only the hanger (H1) is drawn.
- A face 200 mm from the support gives 10 stirrups on the span side. At 201 mm the left set is laid, but the `x >= lo` filter keeps only 3 of them (151, 101, 51), so the total is 8.
- Fix:
  - When both sides are near, lay n on each side, clipped to `[lo, hi]`, and warn.
  - When `leftCount` is clipped, move the missing stirrups to the other side, so the count stays 2n.

M4. Unreadable or equal-depth crossing beams are lost silently or misreported.
- [KataSupportCollector.cs:181](HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L181) and [:187](HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L187): a beam without a vertical range gets soffit `double.MaxValue`. It becomes a load whose `SoffitBelowTopMm` is 0 ([:255](HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L255)). The layout then warns "dầm giao … không còn chỗ cho thép vai bò" (probe: soffit 0 → that warning), which blames the room for a geometry read failure. Use the type `height` that the collector already reads, or warn "không đọc được đáy dầm giao".
- [KataSupportCollector.cs:225-231](HPRebar/HPRebar/KataExport/Service/KataSupportCollector.cs#L225-L231): a one-sided beam of equal or greater depth framing in away from a column is neither a support nor a load, and gets no warning.
- A one-sided beam whose extent overlaps a support face by 1 mm or more is dropped by `LoadsBetweenSupports`. Probe: `[400.5, 700]` beside the support `[0, 400]` → 0 loads.
- Both are common framings: a secondary beam of the same depth, or one flush with the column face. At least count them in a warning.

## Low

L1. Joint zones and cut parts get a misleading `ZoneIndex`, and joint zones are recognised by a string prefix.
- `ZoneIndexOf` returns `_ => 0` ([KataStirrupZoneLayout.cs:132](HPRebar/HPRebar.Core/KataRebar/Calculators/KataStirrupZoneLayout.cs#L132)), so "Giữa nhịp [2]", "Gối trái [2]" and every "Đai gia cường …" are reported as the left dense zone. Numbering order and removal keys use `ZoneIndex`.
- `IsJointZone` is `StartsWith("Đai gia cường")`. R-122 "đai gia cường nhịp" (row 23) would match it the day it is drawn.
- Rules: N9, K1.
- Fix: strip the ` [k]` / ` (k)` suffix in `ZoneIndexOf`, and give joint zones an explicit kind, either a `KataStirrupZoneResult.IsJoint` flag or `ZoneIndex = 3`.

L2. The stub-column hanger is 50 mm off the DWG, although R-121 says ✅.
- Code gives 13800·13950·14550 ‖ 15050·15700·15850, DWG 13750·13900·…. The rise is 600 at real bar levels, while Kata uses drawn levels (−25 / −675).
- `Under_a_stub_column…` asserts only p[2]/p[3] (±30 on z), and the crossing-beam test allows ±60 on p[0]/p[5] (actual 4017 vs 4000).
- Decide which levels the slope is sized on, then tighten the tests (T4).

L3. The spec table is broken.
- [kata-beam-rebar-rules.md:195-196](docs/specs/kata-beam-rebar-rules.md#L195-L196): R-120/R-121 are now ✅ with a 6th "Code · Test" cell, but they still sit in §13 "Chưa làm (backlog)", whose header has 5 columns.
- Move them into the implemented section. Also add "vai bò after side bars" to R-110 (numbering order) and to the `KataBarNumbering` summary.

L4. Loads are dropped when the segment has no pieces.
- [KataRebarPlanner.cs:92-93](HPRebar/HPRebar.Core/KataRebar/Calculators/KataRebarPlanner.cs#L92-L93) returns early before `Loads = …` ([:106](HPRebar/HPRebar.Core/KataRebar/Calculators/KataRebarPlanner.cs#L106)). Set `Loads` before the early return.

L5. `KataBeamMatcher.LoadsOn` uses `Contains(mid, 0.0)` with inclusive ends ([KataBeamMatcher.cs:82](HPRebar/HPRebar/KataRebar/Service/KataBeamMatcher.cs#L82)). A load centred exactly on a span boundary would land on two spans. This is unlikely after the support filter. Use `[Start, End)`.

L6. `LoadsBetweenSupports` re-derives `Interval1D.Union` by hand ([KataSupportRules.cs:55](HPRebar/HPRebar.Core/KataExport/Calculators/KataSupportRules.cs#L55)), which is K2. The test name `LoadsFramingInFromBothSidesAreOneAndAColumnOverABeamIsAColumn` does not follow N10, although the file has no prior convention.

L7. The hanger ignores everything along its length except the load centre.
- It ignores top steps inside its length (`TopAt(s, AtMm)` at the centre only).
- It ignores layer-1 additional top bars near supports. B01 has only layer-2 extras, so no clash was found, but a layer-1 support bar in the outer gap would clash.
- `Build` threads `warnings` and `ref barId`, which is the M6 smell, but it follows the calculator's existing pattern.

L8. `KataRebarTypeResolver` now also adds per-span main-bar diameters (rows 19/21).
- This is a real fix: without it, `barTypes[d]` throws KeyNotFound for a span bar of a diameter that appears nowhere else.
- It belongs to the rows 19/21 change, not the joint loads, so keep the commits separate (P6).

## Edge cases checked (probe results)
| Case | Result |
|---|---|
| B01 mid-D beam 400×900, F stub column | ✅ stations and hanger bottoms as drawn; hanger ends 17 / 50 mm off (L2) |
| Load 350 from C face | ⚠️ hanger from x = −533 (H1); hoops under the load (M2) |
| Stub column near the console tip / console filled | ⚠️ hanger past the tip (H1); 0 joint stirrups, no warning (M3) |
| Two loads 600 apart | ⚠️ 5 coincident hoops, duplicate zone keys (H3) |
| Scan of the load position | ⚠️ 10 mm pair at 1990, 390 mm gap at 3440 (H4) |
| Top bars 2Ø25 | ⚠️ both hangers at y = 0 (H2) |
| Crossing soffit 100 above the beam soffit | ✅ bottom clamped at −1033 / −1050, clear of the bottom bars |
| Soffit unknown (0) | ⚠️ joint stirrups, hanger skipped with a misleading warning (M4) |
| Load touching a support face / two loads touching | dropped / merged (M4; merging by the 1 mm tolerance is intended) |
| Mirrored run | ✅ by inspection: `Mirrored()` reverses the loads with `L − start − width`; no test |
| Inner U/C stirrups and C ties in the joint zones | ✅ none (test plus probe) |
| Hanger keys / zone keys | ✅ 4/4 and 20/20 distinct in B01 (collide only in H3) |
| Revit grouping | ✅ one 2-bar set per load, spacing 324, host = piece under the load centre (`HostOf` midpoint) |

## Positive
- The DWG evidence is pinned to ±1 mm for the stirrups.
- The joint zones are excluded consistently from the inner stirrups, the C ties and numbering-by-shape.
- `Collect` keeps its signature, which keeps Kata Export unchanged, and `CollectWithLoads` is additive.
- Loads are mirrored with the pieces, and the 1 mm `Snap` is applied.
- The weight, `TotalBarCount`, `Fingerprint` and type mapping all include the hangers.
- R26 builds clean, and the full Core suite is green.

## Recommended order
H1 → H2 → H4 → H3 (all in Core, testable with the B01 fixture) → M1 (draw them before the user is asked to approve them) → M2/M3 → M4 (collector warnings) → L1–L3.

## Unresolved (user)
- Kata's own rule for a hanger whose slope would cross a support (shorten it, drop that side, or anchor it into the column) is not in the DWG evidence. H1 needs this decision, or an explicit HPRebar rule.

Status: DONE_WITH_CONCERNS. 4 High findings, all in host-free code, all reproducible with the B01 fixture.
