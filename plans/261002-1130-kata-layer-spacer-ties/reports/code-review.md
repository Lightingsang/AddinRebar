# Code review — Kata layer spacer C ties + J7/I8 tie spacing

Date 2026-10-02 · reviewer: code-reviewer · scope: uncommitted L1–L5 change only (older Kata work ignored) · read-only.

## Scope
- New: `KataTieStations.cs`, `KataLayerSpacerTieLayout.cs`, `KataLayerSpacerTieTests.cs`
- Edited: `KataSideBarLayout.cs`, `KataRebarCalculator.cs`, `KataDetailingRuleBuilder.cs`, `KataDetailingRules.cs`, `KataSettings.cs`, `KataStirrupSpec.cs`, `Enums.cs`, `KataDamSheetParser.cs`, `KataSettingsJson.cs`, `KataSettingsViewModel.cs`, `KataSettingsView.xaml` + edited tests
- Consumers read: `KataBarSetCreator.cs`, `KataTieWrap.cs`, `KataRebarTypeResolver.cs`, `KataStirrupZoneLayout.cs`, `KataSupportTopBarLayout.cs`, `KataSpanBottomBarLayout.cs`, `KataTopLayerStack.cs`, `KataLayerPositions.cs`

## Checks run
| Check | Result |
|---|---|
| `dotnet test HPRebar/HPRebar.Core.Tests` | 849/849 pass |
| Scratch probe (console → HPRebar.Core, DY7 sheet variants) | asymmetric E14 cells, J7 `a5`, right cantilever — results quoted below |
| Revit / R24-R26 build | CHƯA TEST (not asked) |

## Verified OK
- LikeHoops runs stay evenly spaced: zone stations are arithmetic (filler = `gap/(fill+1)`, [KataStirrupZoneLayout.cs:145](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataStirrupZoneLayout.cs#L145)); a constant shift + interval filter keeps a contiguous arithmetic subrange; `Spacing` = zone spacing; a 1-station run skips `SetLayoutAsNumberWithSpacing` ([KataBarSetCreator.cs:72](../../../HPRebar/HPRebar/KataRebar/Service/KataBarSetCreator.cs#L72)). Shape built at `Stations[0]`, host at the midpoint inside one span — creator contract holds.
- BarId renumbering: `BarId` has no consumer outside tests (grep: only the property, [KataRebarCurve.cs:11](../../../HPRebar/HPRebar.Core/KataRebar/Models/KataRebarCurve.cs#L11)); zones hold no ids. Ids match the old order. No side effect.
- Old settings files with `SideBarTieSpacing` load (unknown keys ignored, [KataSettingsJson.cs:13-14](../../../HPRebar/HPRebar.Core/KataRebar/Parsers/KataSettingsJson.cs#L13)); `LayerTieMinBarCount` sanitised ≥ 2 in JSON and VM.
- Layer numbering matches L1: top `Layer = layer + 1` with row 13 = layer 1 ([KataSupportTopBarLayout.cs:262](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataSupportTopBarLayout.cs#L262)), bottom row 17 = layer 2. Layer ≥ 2 rows never continue with "-" ([KataSupportTopBarLayout.cs:58](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataSupportTopBarLayout.cs#L58)), so no continued-row duplicates.
- Tie Ø added to bar-type mapping via `BarSets` ([KataRebarTypeResolver.cs:89](../../../HPRebar/HPRebar/KataRebar/Service/KataRebarTypeResolver.cs#L89)) — no `KeyNotFoundException` in creator.

## High
None.

## Medium

### M1 — Asymmetric support cell: N counted over both sides, ties under a 2-bar section
[KataLayerSpacerTieLayout.cs:90-99](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataLayerSpacerTieLayout.cs#L90) groups by `(HostSupportIndex, Layer)`, so the `…T` and `…P` sub-rows of a left;right cell ([KataSupportTopBarLayout.cs:126-129](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataSupportTopBarLayout.cs#L126)) become one group; the N test uses the sum, and lo/hi come from whichever side owns the outer slots.
- Probe, DY7 with `E14 = "2f20;2f18"` (2 bars each side, N = 3): `3.2.2C` drawn in span 1 at 4616/5116/5616. Left bars 4550–6359, right bars only from 5230 → at 4616 and 5116 the section holds 2 bars. Violates L1.
- Converse: `3f20;3f18` (left stronger by area) → outer slots = left bars ending at the far face of E → span 2 gets no tie although its section has 3 bars (inferred from `2f20;3f18`, where the outer slots moved to the right side and only span 2 got ties).
- Fix: decide per span on the bars whose level run covers the station range (count bars overlapping [lo, hi] ∩ span), and take the two outer bars among those; or group by `BarMark` (T/P) and test N per side.

### M2 — Tie offset ignores the wrapped bar's diameter: straight part cuts Ø22+ layer bars
Layer ties reuse `WrapEnds` + unit `WrapOffset` ([KataLayerSpacerTieLayout.cs:142-144](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataLayerSpacerTieLayout.cs#L142)); [KataTieWrap.cs:31-34](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataTieWrap.cs#L31) offsets by the tie's bend radius only. Ø8 tie, Revit bend radius 14 (KataTieWrapTests comment): tie top face sits 10 mm under the layer centres; a Ø25 bar reaches 12.5 → 2.5 mm overlap, Ø32 → 6 mm, and the hook's inner radius (10) is smaller than the bar it "wraps". Side bars (Ø12–16) never hit this; layer bars at supports are usually Ø20–32, DY7 (Ø18) does not. Revit raises nothing, so the clash reaches drawings/fabrication silently. `CheckClearance` uses a fixed `2.5 × Ø_tie` ([line 161](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataLayerSpacerTieLayout.cs#L161)), also blind to bar size.
- Fix: carry the wrapped bars' max diameter on `KataBarSet` and lay with `max(bendRadius, d_bar/2 + d_tie/2)` (or warn when d_bar/2 + d_tie/2 > bendRadius). Not a re-litigation of the verified wrap layout — that was verified on Ø18.

### M3 — J7 has no plausibility bound
[KataDamSheetParser.cs ParseTieSpacing](../../../HPRebar/HPRebar.Core/KataRebar/Parsers/KataDamSheetParser.cs#L292) accepts any v > 0; [KataTieStations.cs:53](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataTieStations.cs#L53) then lays `floor(L/spacing)+1`. Probe: DY7 with `J7 = "a5"` → 5 296 tie bars, no warning; `a50` (typo of a500) silently gives 10×. Each set becomes a Revit `SetLayoutAsNumberWithSpacing` of hundreds/thousands — slow creation, wrong drawing. Fix: warn (or fall back to 500 + note) outside a sane range, e.g. 100 … 1000 mm, or below the outer stirrup spacing.

## Low

- L1 — Bottom row 17 with no level beneath (no B12 main bars, row 18 empty) is seated on the stirrup (`z = Seat(d)`, [KataSpanBottomBarLayout.cs:92](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataSpanBottomBarLayout.cs#L92)) but still `Layer == 2` → tie hung under it lands on the hoop's bottom leg / in the cover (Ø18, cover 25, Ø8: tie centre ≈ 28 mm above soffit vs hoop leg at 29). `CheckClearance` only checks bars. Skip or warn when the layer is the lowest level.
- L2 — LikeHoops at a right cantilever tip: shift is always +X, last hoop is at ≤ SpanEnd − cover ([KataStirrupZoneLayout.cs:169-177](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataStirrupZoneLayout.cs#L169)), so a side-bar tie can sit at SpanEnd − 9 (inside the cover) when the console length leaves a remainder < 16 mm; side ties pass lo/hi = span ([KataSideBarLayout.cs:120](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataSideBarLayout.cs#L120)), unlike layer ties. Probe (2200 mm console) put the last hoop at SpanEnd − 50 → tie at −34, fine; exact-multiple consoles are not. Clamp `x ≤ SpanEnd − Clearance` or shift toward mid-span.
- L3 — Uniform mode anchors every run at its low-X end ([KataTieStations.cs:49-54](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataTieStations.cs#L49)): in the span left of a support the tie nearest the face can be up to spacing + 66 away (DY7 `3.2.2C`: last tie 5616, face E 5950 → 334 mm), while to the right it is 66. GIẢ ĐỊNH CHƯA XÁC MINH whether Kata anchors at the support; same pattern as before for side ties. Consider anchoring support-layer runs at the support face.
- L4 — G6 = 0: Uniform still draws Ø10 ties (default reserved stirrup), LikeHoops draws none (no zones) — silently ([KataTieStations.cs:35](../../../HPRebar/HPRebar.Core/KataRebar/Calculators/KataTieStations.cs#L35)). Warn or skip both.
- L5 — `CheckClearance` scans [lo, hi], which includes the support stretch where no tie exists, and ignores side-bar ties at the same station → possible false warnings / missed tie–tie contact. One warning per group, so noise is bounded.
- L6 — Adjacent supports' layer-2 rows that overlap in a short span (H5 ≥ 0.5, or wide spans with long reach) yield two coincident tie sets at identical stations; no de-dup.
- L7 — Uniform grid is independent of hoop stations: a tie can fall within Ø of a mid-zone hoop or inner stirrup (centred middle zone has an arbitrary phase). Pre-existing for side ties; now also for layer ties.

## Test gaps
No test for: asymmetric left;right cell (M1), layer bars ≥ Ø22 (M2), cantilever + LikeHoops, layer 3 groups, stepped soffit span with row 17, G6 = 0, J7 out of range.

## Plan status (report only, no plan edits)
Phases 1–2 implemented; phase 3 tests partially (DY7 golden, 2 bars, I8 = 1, J7 empty present; R24–R26 build not seen here); phase 4 UI setting present, live DY7 not run.

## Recommended actions
1. M1: per-span bar count/outer bars.
2. M2: diameter-aware wrap offset or warning.
3. M3: J7 range check.
4. Add the missing tests above.

**Status:** DONE_WITH_CONCERNS
