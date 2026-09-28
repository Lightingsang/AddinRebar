# Code review — phase 05 (additional bottom bars of the span, rows 17-18)

Date 2026-09-28. Scope: `git diff HEAD` over `HPRebar.Core/KataRebar`, `HPRebar.Core.Tests/KataRebar`, `HPRebar/KataRebar` + untracked `KataSpanBottomBarTests.cs`. Other-session files (`Application.cs`, `HPRebar.csproj`, `install/`, `.claude/`, `.gitignore`) ignored. Read-only; B1-B5 defaults not re-litigated.

## Verification done by reviewer
- `dotnet test HPRebar.Core.Tests`: 729/729 pass.
- Scratch console probe (scratchpad, ProjectReference to HPRebar.Core, `--artifacts-path` scratch) confirmed H1, M1, M2, L1 numerically (numbers below). Live-check sheet 300×600, 400|6000|400, G6 Ø8.
- No add-in build run (not asked; no Revit API surface changed beyond reuse of existing `CreateLongitudinalBars`).

## Overall
Straight bars, cut stations (face + L/7 from measured stations — `span.Length` and `SpanStart/End` come from the same measured value), marks `4.{s}.{1|2}`, row→Layer mapping, row-17 stacking on `max(main d, row-18 d)`, and the x-overlap test against support bars are correct. The golden numbers in the tests re-derive by hand (row 18 y ±35.67, z −557; row 17 z −513, y ±108). Main-bar anchorage is untouched (bars start ≥ L/7 inside the span, far from any bent leg). The only real defect is the vertical position of row 18 when its diameter differs from B12's.

## Revit-side contract (asked explicitly)
- No `barTypes[bar.Diameter]` KeyNotFound path: `Diameters(plan)` ([KataRebarExternalEventHandler.cs:141-145](HPRebar/HPRebar/KataRebar/KataRebarExternalEventHandler.cs#L141-L145)) and `CreateLongitudinalBars` ([KataRebarCreationService.cs:23-30](HPRebar/HPRebar/KataRebar/Service/KataRebarCreationService.cs#L23-L30)) both enumerate `Layout.LongitudinalBars` of the same re-planned `plan`; a diameter missing from the UI mapping returns `Failed("Thiếu RebarBarType …")` before any transaction. Mapping table gains `Gia cường nhịp` rows ([KataRebarTypeResolver.cs:83-84](HPRebar/HPRebar/KataRebar/Service/KataRebarTypeResolver.cs#L83-L84)). verified by file:line.
- Cleanup deletes by host tag in comments (`KataRebarStamp`), so re-runs remove 4.x bars too. Counts: `mainBars = longitudinal − extraTop − extraBottom` is right because creation is all-or-throw.

## High

**H1 — Row 18 sits at the main bars' centre whatever its diameter → bar cuts into the stirrup** — [KataSpanBottomBarLayout.cs:31](HPRebar/HPRebar.Core/KataRebar/Calculators/KataSpanBottomBarLayout.cs#L31), [:54](HPRebar/HPRebar.Core/KataRebar/Calculators/KataSpanBottomBarLayout.cs#L54); root in [KataDetailingRuleBuilder.cs:52](HPRebar/HPRebar.Core/KataRebar/Calculators/KataDetailingRuleBuilder.cs#L52) (`BottomBarCentreDepth` from B12's d only).
- Scenario A (common: extras larger than main): J9 empty, B12 `2f18`, D18 `2f25`. Probe: centre 42 from soffit, bar bottom 29.5, stirrup inner face 33 → 3.5 mm into the stirrup's bottom leg. J9 `43/25` same sheet: bottom 30.5 → 2.5 mm. No warning, `CanGenerate = true`.
- Scenario B: J9 empty, B12 empty, D18 `2f16`. Probe: `botDepth` = 25 + 8 + 0 = 33 → row-18 centre on the stirrup inner face, bar bottom at 25 = stirrup outer face: the bar occupies the whole stirrup leg (and edge bars at y ±109 sit in the corner bend). Same sheet with only D17 `2f16`: row 17 floats at bottom 58 over an empty level (level1D = 0 still adds the 25 mm gap).
- Row 17 inherits the error (its z is stacked on `mainZ`), and the phase-04 support-level check ([KataSupportTopBarLayout.cs:232](HPRebar/HPRebar.Core/KataRebar/Calculators/KataSupportTopBarLayout.cs#L232)) still measures against B12's d only.
- Tests miss it: `KataSpanBottomBarTests.Sheet()` uses equal diameters (B12 2Ø20, D18 2Ø20).
- Fix: level-1 z per bar = `max(mainZ, −H + StirrupCover + StirrupDiameter + d/2)` (bars rest on the stirrup, as real bars do), or keep one level z = `max` over main and row-18 diameters and warn when it moves B12; stack row 17 on the top face of level 1 = `max(mainZ + mainD/2, z18 + d18/2)`; when B12 and D18 are empty put row 17 on the resting level. Add a test with D18 d > B12 d and one with B12 empty. Same latent flaw exists for row 13 vs B11 (phase 04) — fix both in one helper.

## Medium

**M1 — Partly unreadable cell drops the unreadable part silently** — [KataSpanBottomBarLayout.cs:108-116](HPRebar/HPRebar.Core/KataRebar/Calculators/KataSpanBottomBarLayout.cs#L108-L116), parser [KataBarNotationParser.cs:35-46](HPRebar/HPRebar.Core/KataRebar/Parsers/KataBarNotationParser.cs#L35-L46)
- Scenario: D18 `2f20+2x18` (typo). Probe: 2 bars Ø20 drawn, 0 warnings about `2x18` → half the span reinforcement missing, user believes the cell was drawn. B5 only fires when *nothing* parses.
- Fix: in `Readable`, compare token count (split on `;+,`, ignoring `0`/`-`) with parsed item count; warn `"{addr} '{text}': phần '…' không đọc được — chỉ vẽ …"`. Same gap in `KataSupportTopBarLayout` (`sides.IsEmpty` test).

**M2 — Coincident bars are drawn with only a spacing warning** — [KataLayerPositions.cs:19-20](HPRebar/HPRebar.Core/KataRebar/Calculators/KataLayerPositions.cs#L19-L20), [KataSpanBottomBarLayout.cs:52-54](HPRebar/HPRebar.Core/KataRebar/Calculators/KataSpanBottomBarLayout.cs#L52-L54)
- Scenario: B12 `1f20` (mainY = {0}), D18 `1f20` (or any odd count). Probe: row-18 bar at y 0, z −557 = the main bar's axis; warning "khe thông thủy −20 mm", `CanGenerate = true` → two Rebar elements on the same line in Revit.
- Fix: with fewer than 2 main bars, place row 18 over the width *excluding* the main positions (or block when any clear < 0). Negative clear from `CheckSpacing` should be blocking, not a warning.

## Low

**L1 — Multi-item row 17 is asymmetric** — [KataSpanBottomBarLayout.cs:62](HPRebar/HPRebar.Core/KataRebar/Calculators/KataSpanBottomBarLayout.cs#L62), [:136-141](HPRebar/HPRebar.Core/KataRebar/Calculators/KataSpanBottomBarLayout.cs#L136-L141). D17 `2f20+1f18`: probe Ø20 at y −107 and 0, Ø18 at +107 (1 mm off the stirrup since positions use max d). Assign larger bars to the edges symmetrically (sort items by d, fill outside-in), and compute edge offsets per bar d. Row 18 in 4-bar B12 with 2 extras likewise goes centre gap + right gap (y 0, +71.3) — acceptable but lopsided.

**L2 — Row 18 vs deep support levels uses B12's d** — [KataSupportTopBarLayout.cs:232](HPRebar/HPRebar.Core/KataRebar/Calculators/KataSupportTopBarLayout.cs#L232). Rows 14-16 reach 0.2 L, row 18 starts at L/7 → they overlap in x; a row-18 bar larger than B12 is not in that check (under-reports by (d18 − dB12)/2). Folds into the H1 fix.

**L3 — Negative sheet length in sheet-only preview** — [KataSpanBottomBarLayout.cs:44-46](HPRebar/HPRebar.Core/KataRebar/Calculators/KataSpanBottomBarLayout.cs#L44-L46). Stations clamp `Length` to ≥ 0 but `cut` uses the raw value → negative cut → bars run into both supports. Measured geometry replaces it before generation, so preview-only. Use `Math.Max(0, span.Length)` (the old code skipped `ln <= 0`).

**L4 — Class summary contradicts code** — [KataSpanBottomBarLayout.cs:11-15](HPRebar/HPRebar.Core/KataRebar/Calculators/KataSpanBottomBarLayout.cs#L11-L15): "row 17 … takes the main level's place when row 18 is empty" but code/test put row 17 one layer *above* the main level (B3). Reword.

**L5 — Test value** — `Main_bottom_bars_keep_their_anchorage` is tautological today (`KataMainBarLayout` never reads span extras); keep as a regression pin but it guards nothing now. Missing: d18 > dB12 (H1), B12 empty, partial parse (M1), multi-item order (L1), coincident bars (M2). The two blocking tests (x-overlap yes/no) are meaningful and correct.

## Checked, no finding
- Vertical clash with top bars: `CheckClearOfTopBars` covers B11 level + every support bar whose x range overlaps [xStart, xEnd] (legs bend down, so `Max(Z)` is the level). Probe/tests: blocks at H3 0.2, passes at 0.05.
- Stirrup side legs: row-17 edge bars touch the stirrup inner face at the straight vertical leg (above the corner bend) — fine.
- Main-bar anchorage: unchanged by extras; no interplay (bars end ≥ L/7 from faces).
- Scope: rows 17-18 kept; multi-span still blocked by `KataScopeFilter`; weight includes `extraBottom`.

## Recommended actions
1. H1 (+L2): per-diameter resting level for row 18, stack row 17 on level 1's top face; tests with d18 > dB12 and B12 empty.
2. M1: partial-parse warning (rows 13-18 share the helper).
3. M2: block negative clear gaps.
4. L1/L3/L4 when convenient.

Status: DONE_WITH_CONCERNS — 1 H (row-18 level ignores its own diameter), 2 M, 5 L.

## Xử lý (2026-09-28)
| # | Trạng thái | Cách sửa | Test |
|---|---|---|---|
| H1 | sửa | thanh hàng 18 ngồi trên đai theo Ø riêng: tâm = max(a dưới, b + d_đai + d/2); hàng 17 xếp trên mặt trên cao nhất của lớp 1; lớp 1 trống → hàng 17 ngồi trên đai | `A_row_18_bar_larger…`, `Without_bottom_main_bars…`, `Row_17_alone_without…` |
| M1 | sửa | `KataBarNotationParser.UnreadableTokens` → cảnh báo phần không đọc được (dưới + gia cường gối hàng 13–16) | `A_partly_unreadable_cell…` (2 file) |
| M2 | sửa | `CheckSpacing` chặn khi hai thanh chồng nhau (khe < 0), cả thép gối | `Bars_on_top_of_each_other_block` |
| L1 | sửa | thanh Ø lớn nhận vị trí ngoài cùng (`OuterBarsLargest`) | `A_mixed_row_17_cell…` |
| L2 | sửa | kiểm khe với lớp trên dùng mặt trên thật của lớp dưới cao nhất (hàng 17 hoặc hàng 18) | 2 test chặn/không chặn |
| L3 | sửa | nhịp dài ≤ 0 → cảnh báo, không vẽ | `A_span_without_length…` |
| L4 | sửa | doc lớp | — |
| L5 | sửa | bỏ test không thể fail | — |
Core 736/736; build R26/R25/R24 0 lỗi.
