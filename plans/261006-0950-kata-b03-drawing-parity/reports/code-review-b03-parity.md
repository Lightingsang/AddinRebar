# Code review — Kata Rebar B03 drawing parity (working tree vs 97c4b4e)

Reviewer: code-reviewer agent, 2026-10-06. Read-only; no source edited.

## Scope
- New: [KataSupportBottomBarLayout.cs](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataSupportBottomBarLayout.cs) (248 lines), [KataB03DrawingTests.cs](../../../HPRebar/HPRebar.Core.Tests/KataRebar/KataB03DrawingTests.cs), `Fixtures/b03-dwg.json` (157 KB, loaded by `KataDwgFixture` by source path — OK).
- Modified: 16 Core files, 1 add-in file ([KataTransactionRunner.cs](../../../HPRebar/HPRebar/KataRebar/Service/KataTransactionRunner.cs)), 6 test files, spec §12b R-149…R-158. ~+360 / −78 lines.
- Rules: CODE_REVIEW_CHECKLIST, HP_CLEAN_CODE_CORE (PCC ids), dev rules (C# < 300 lines).

## Checks run
| Check | Result |
|---|---|
| `dotnet test HPRebar/HPRebar.Core.Tests` | ✅ 1590/1590 |
| `dotnet build HPRebar/HPRebar.csproj -c Debug.R26 -p:DeployAddin=false` | ✅ 0 errors; 32 warnings, all pre-existing ILRepack `EXEC : warning` (none in Kata files) |
| Live Revit | CHƯA TEST |

## Overall
Rule work is careful and test-anchored on B01–B03 DWG. Main risk: several new rules are fitted to B03 only and change behaviour for any other sheet (side-bar levels, row-17 run-on gaps, end overhang from row 20 width). Two findings produce bars outside concrete / coincident bars on plausible sheets with no warning.

## Findings (severity-ranked)

### High
1. **Side bars can leave the concrete when a run continues into a shallower span** — [KataSideBarLayout.cs:65](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataSideBarLayout.cs#L65). Levels now come from span `a` (was: shallowest span of the run); the run only stops on width/top change, not depth. Ex: span 1 h900, span 2 h500, 2 layers → z ≈ −585 / −315; −585 is below span 2's soffit (−500). B03 works only because 900 > 780. PCC-230/R-154 overreach. Fix: also end the run (or fall back to min depth + warning) when any level of span `a` is below `ZBottom(depth(b+1)) + layer gap`. Add a test with a deep→shallow pair.
2. **Run-on row 17: span's own bars not moved when gaps are short → coincident bars; odd/odd gives duplicate Y** — [KataSupportBottomBarLayout.cs:78-99](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataSupportBottomBarLayout.cs#L78-L99). `MiddleGaps` excludes the gap at 0 for even counts and returns fewer slots than `own.Count`; unmatched own bars stay at their own `ComputeTransverseYPositions` = same Y as the run-on bars (e.g. 2Ø20 run on + own 2Ø20 → both pairs at the edge positions, same z). Odd source + odd own (3 + 3): picks −a/2 twice. No `KataLayerPositions.CheckSpacing` is called for any bar this class creates (RunOn and Over), unlike `KataSpanBottomBarLayout`. Fix: run `CheckSpacing` on the combined layer and warn/block when `ys.Count < own.Count`; de-duplicate the odd pick.
3. **`EndsAtColumns` leaves an overhang when the crossing beam is wider than the column** — [KataRebarPlanner.cs:163](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataRebarPlanner.cs#L163) with [KataBeamStations.cs:83-90](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataBeamStations.cs#L83-L90). Only `CrossingBeamOffset` is zeroed; `Overhang` is still `(W − C)/2 > 0` (e.g. 300 beam on a 220 column → 40 mm each end), so Revit bars anchor past the column face while the warning says "thép neo trong cột". Also: for every sheet (not only B03) a row-20 width > column width at an end now lengthens main/top anchorage (`AnchorWidth`) and moves side-bar ends — a new behaviour, never seen in Kata for a centred beam. Fix: zero `CrossingBeamWidth` too (or add an explicit "ends at column" flag) and assert `StartOverhang == EndOverhang == 0` after the call; decide whether a centred wider crossing beam should overhang at all (spec R-149 only covers offset).

### Medium
4. **Mark suffix collision "T"** — [KataSupportBottomBarLayout.cs:120](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataSupportBottomBarLayout.cs#L120) vs `ThroughSuffix = "T"` (line 28). "T"/"P" already mean left/right (`KataSupportTopBarLayout.cs:136-138`). The left part of a bar cell ("2f20") is marked `4.{k+1}.2T` → `KataLayerSpacerTieLayout.Mixed` counts it as run-on and forces a tie wherever it overlaps the span's own row 17 (0.20L–0.25L zone); with `"2f20;-"` the left bars and the run-on bars share the identical mark `4.{k+1}.2T` for different bars. Fix: a distinct run-on token (e.g. `ThroughSuffix = "N"`), or carry the kind as a property instead of parsing `BarMark` (PCC-055/N1).
5. **Row 17 at end supports silently dropped** — [KataDamSheetParser.cs:225](../../../HPRebar/HPRebar.Core/KataRebar/Parsers/KataDamSheetParser.cs#L225) stores row 17 for every support with a width and no longer reports it; `Apply` loops only interior supports ([KataSupportBottomBarLayout.cs:40](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataSupportBottomBarLayout.cs#L40)). Contradicts `KataScopeFilter` "every other filled detailing cell is reported". Fix: keep the note for k = 0 / last.
6. **Over-support bars vs span's own row 17** — `Over` lays bars from H5·L off the face while the span's own row 17 stops at H3·L ([:115](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataSupportBottomBarLayout.cs#L115), [:126-127](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataSupportBottomBarLayout.cs#L126-L127)): both at the same z and the same `ComputeTransverseYPositions` Ys over 0.05L → clash, no spacing check (see 2). Also top-bar cut ratios (H5/H3) are reused for bottom bars with two different cells both commented "H5 × L" — name which cell governs which bar (CM1).
7. **Inner-stirrup carry-over is now unconditional until row 24 "*"** — [KataInnerStirrupLayout.cs:53](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataInnerStirrupLayout.cs#L53). Previous rule (same top bars) is replaced for every sheet; carried entries that don't fit are dropped with no warning, those that fit are applied to a span with different top bars. Backed by B01 + B03 only. Index alignment checked: `KataZeroWidthSupports.Merge` keeps supports/spans paired (`Supports[s]` = left support of span s) — OK; a merged-away zero-width support loses its "*" (acceptable, merge already drops the right span's stirrups with a note). Recommend a warning when carried entries are dropped.
8. **Overhang not in the drawing's length/title/dims; right-end overhang half drawn** — title `L={f.Length}` and dims `0…f.Length` ([KataElevationDrawingBuilder.cs:44-49](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataElevationDrawingBuilder.cs#L44-L49), `KataElevationDims.cs:31,42`) ignore `StartOverhang` (spec R-149 says Kata L = 33750; HP prints 33600). `Slab()` draws the hidden crossing beam only at k = 0 ([KataElevationOutline.cs:124-131](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataElevationOutline.cs#L124-L131)); `maxX` ignores `EndOverhang`. End-overhang sign (+offset = outward at the right end) is GIẢ ĐỊNH CHƯA XÁC MINH — no test with `EndOverhang > 0`.
9. **`WithEnd` shape code when the bar already has a start hook** — [KataSupportBottomBarLayout.cs:152](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataSupportBottomBarLayout.cs#L152). Chained "-" supports (run-on bars start bent at k, end bent at k+1) get `05a` instead of `15a`; a bar that already ended in a hook would get its new end at the leg's top z (`last` = leg point). Fix: rebuild from the horizontal run, derive shape from both ends.
10. **File size** — [KataSectionTags.cs](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataSectionTags.cs) now 317 lines (> 300, PCC-183/184, dev rule); `KataSupportTopBarLayout.cs` 298. `Combined` + the side-level branch are a natural split (e.g. `KataInnerLayerTags`).

### Low
11. Misplaced XML docs: Stretches' `<summary>` now sits on `Mixed` (two summaries) — [KataLayerSpacerTieLayout.cs:82-89](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataLayerSpacerTieLayout.cs#L82-L89); same for `WithMeasuredGeometry`'s summary now on `EndsAtColumns` — [KataRebarPlanner.cs:148](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataRebarPlanner.cs#L148). Move each back to its method.
12. Plan references in touched comments (CM6 / PCC-253): `KataDwgBeam.cs:77` "the plan's reports/kataB03-dam-cells.txt", `KataDwgElevationTests.cs:112` "(phase-01 report)".
13. Row 17 "-" at a support whose left span has no row-17 bars does nothing silently (`source` empty, [:50-55](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataSupportBottomBarLayout.cs#L50-L55)) — warn. Width-0 interior supports: `tip` ([:113](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataSupportBottomBarLayout.cs#L113)) would treat them as a console tip, but `KataScopeFilter` blocks unmerged interior width-0 supports, so only reachable by calling the calculator directly.
14. `Bent` room uses `-TopBarCentreDepth` from the beam top, ignoring a row-19 drop → leg may reach the top bars in a stepped span ([:239-241](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataSupportBottomBarLayout.cs#L239-L241)).
15. Tag rules fitted on 2–3 drawings and extrapolated: `tooLow` now lifts inner-stirrup leaders over the beam on any shallow section (row −250 vs bottom bars of a ≤ 400 beam) ([KataSectionTags.cs:202-205](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataSectionTags.cs#L202-L205)); width-linear side-tag terms (`b − 300`) and per-Ø stirrup offsets from two points; `Combined` fires whenever a face's inner layers hold ≥ 2 numbers (e.g. left/right support bars cut in one section). DY7/DY14 tests still green — fine, but untested on other widths/depths.
16. Method shape: `Over` 9 params incl. behaviour-switching `bool toRight` (PCC-060/063); `Bar` 10 params; `ref int barId` + list mutation (PCC-075/076) — consistent with existing Kata calculators, not new debt per se. `EndsAtColumns` returns warnings and the spec through `out` — return a tuple. Many lines > 120 chars (FM2), e.g. KataSideBarLayout.cs:15.
17. `KataTransactionRunner` diagnostics: `KataRebarStorage.Read` (`GetEntity`) inside `PreprocessFailures` is unguarded — an exception there aborts failure processing for a log line; wrap the id/number formatting in try/catch, hoist `GetDocument()` out of the loop, and log ids for errors too (R3 improved otherwise).
18. Row 20 now parsed via `GetText` + `ParseSupportDimension` (was `GetDouble`): fine for "400"/"400x500"; a numeric cell rendered with a decimal comma by the accessor would parse as 0 — check `GetText` on numeric cells returns invariant text.

## Positive
- Additive model changes (`init` props with defaults) — no break for other callers.
- Planner keeps "Revit decides 3D" explicit with a cell-addressed warning and a test for it.
- `KataSupportBottomBarLayout` is well documented with measured DWG evidence; numbering/tie rules cite drawings, not plan codes (except items in 12).
- Known deviations are recorded honestly in spec §12b "Còn lệch".

## Recommended actions
1. Fix 1 (side-bar depth guard) and 2 (gap allocation + `CheckSpacing`) with tests for deep→shallow and 2+2 / 3+3 run-on.
2. Fix 3 (zero width or flag; assert no overhang in Revit spec).
3. Rename the run-on suffix (4); restore end-support row-17 note (5).
4. Split `KataSectionTags` (10); fix doc placement and plan refs (11, 12).

## Plan follow-up
B03 parity tasks appear implemented and tested (Core); live Revit run with the B03 model CHƯA TEST. Do not mark Verified.

**Status:** DONE_WITH_CONCERNS
**Summary:** Build + 1590 tests green; 3 High (side bars outside shallower span, coincident run-on bars, overhang kept in Revit for wide crossing beams) + 7 Medium to address before commit.

## Fix round 2026-10-06

| # | Status | Change |
|---|---|---|
| H1 | fixed | side-bar run also ends at a span whose bottom bars the lowest layer cannot clear by a layer gap; test deep→shallow |
| H2 | fixed | MiddleGaps picks each gap once (nearest middle, pairs); too few gaps → whole layer re-spread, own bars nearest middle; spacing warning for row 17 per span (warning only — Kata draws B03's tighter); test 3+3 |
| H3 | fixed | EndsAtColumns zeroes crossing-beam width too; test crossing beam 500 on column 400 |
| M4 | fixed | run-on suffix "R"; bars over a support "L"/"P" |
| M5 | fixed | row 17 at end supports warned ("chưa vẽ") |
| M6 | partly | overlap now warned by spacing check; Cut comment says H3 |
| M7 | kept | carry-over until row-24 "*" is the DWG rule (B01 + B03); documented R-155 |
| M8 | partly | title L and centre include the overhang (33750); dims chain / right-end crossing beam not drawn (no evidence) |
| M9 | fixed | WithEnd keeps 15a when the bar has a start hook |
| M10 | fixed | KataSectionTags split → KataSectionTags.InnerStirrups.cs (260 + 69 lines) |
| Low | fixed | doc comments re-placed; plan refs removed from test comments |
| Low | open | Bent leg room vs row-19 drop; tag rules fitted on 2–3 drawings; Over parameter count; PreprocessFailures storage read without try/catch |

Checks: Core 1594/1594; Debug.R26 build OK.
