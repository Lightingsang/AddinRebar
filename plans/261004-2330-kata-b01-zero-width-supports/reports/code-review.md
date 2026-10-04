# Code review — Kata B01 rules N1–N3 (uncommitted, 2026-10-04)

Scope: `HPRebar.Core/KataRebar` — `Parsers/KataDamSheetParser.cs` (InheritSteps, HasNumber, Merge call), `Parsers/KataZeroWidthSupports.cs` (new), `Models/KataCellNote.cs`, `Calculators/KataScopeFilter.cs`, `Calculators/KataSheetGeometryCheck.cs`, `Calculators/KataRebarPlanner.cs`; tests `KataSpanCarryOverTests` (new), `KataB01DrawingTests` (new), `KataScopeFilterTests`, `KataDamSheetColumnListTests`.
Checklist: `docs/clean-code/CODE_REVIEW_CHECKLIST.md`. Score 7/10.

## Evidence
- `dotnet test HPRebar.Core.Tests` → 1284/1284 ✅ (run 2026-10-04).
- Scratch probe (console app referencing HPRebar.Core, deleted after the run) over a 3-span sheet C|D 6000|E|F 4500|G|H 3000|I, B5 800, G4/G5 = 12/2:

| # | Input | Observed |
|---|---|---|
| 1 | G11 = `C400` (not a number) | merged → spans 6000, 7500; **0 blocking, 0 note** |
| 2 | E11 = 0, F21 = `3f20` | skipped = [] (F21 bars **silently lost**) |
| 3 | D20 = `-` (first span), G4/G5 set | side bars 0 (4 without the dash), no warning |
| 4 | C11 = 0 (left console), Revit [span, col, span, col, span, col] fwd + reversed | no blocking, console warning, reversed detected ✅ |
| 5 | D21 = 200, F21 = `-` | depths 600, 600, 600 (dash inherits, does not reset) |
| 6 | E11 = G11 = 0 | one span 13500 @D, supports 400@0, 400@1 ✅ |
| 7 | D20 = `2f14`, F20 = 250 | "F20 '250': không đọc được là cốt giá — lấy cốt giá của nhịp trước" |
| 8 | E11 = 0, F21 = 200 | depths 800 (merged D+F), **600 (H)**; note says F21 "dùng của nhịp D" |

## High
**H1 — `Parsers/KataZeroWidthSupports.cs:27` — PCC-078 (precondition) — `ColumnWidth > 0.0` is the merge test, but `ParseSupportDimension` returns 0 for any unparseable row 11 (`C400`, `D300`, `—`), and negative for `-300`.** Before this diff such a sheet blocked ("Các gối phải có bề rộng > 0"); now an interior typo silently merges two spans (probe 1), and the same at an end silently turns into a console (scope only warns). Rule N2/N3 is "row 11 = **0**".
Fix: merge/console only when the cell reads exactly as zero; keep blocking otherwise.
```csharp
// parser: record the fact, not the derived width
bool zero = double.TryParse(row11?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var w) && w == 0.0;
// → KataSupportRebarSpec.IsZeroWidth (or pass a bool[] to Merge); Merge/SheetSequence/ScopeFilter test that.
```
and in `KataScopeFilter.Apply` block any support with `ColumnWidth <= 0 && !IsZeroWidth` ("G11 'C400' không đọc được bề rộng gối"). Add a test with `G11 = "C400"` and `C11 = -300`.

## Medium
**M1 — `KataZeroWidthSupports.cs:17,57-61` — K/“never drawn silently” contract — bars of the right span's row 21 are dropped without a note (probe 2).** `RightSpanRows` omits 21, `ReportDifferences` compares only `SoffitDrop`, and `KataScopeFilter.Apply:58-62` only sees surviving spans. Fix: in `ReportDifferences`, report `21` when `right.SoffitDrop != left.SoffitDrop || right.SoffitDropBars.Count > 0`, text = raw cell (pass `accessor`) rather than `{SoffitDrop:0}` (also fixes the quoted text not matching the cell). Same gap for inner stirrups of equal count but different content (line 64): compare `string.Join(";", InnerStirrups)` not `Count`.

**M2 — `KataDamSheetParser.cs:147` + `KataZeroWidthSupports.cs:59` — inheritance chain disagrees with the merge outcome (probe 8).** Carry-over runs in the parse loop *before* `Merge`, so the span after a merged pair inherits the right span's step/side bars, while the note tells the user the merged span "dùng của nhịp trái". H gets a 200 mm soffit step from an empty H21; the geometry check would then quote H21 (an empty cell) on a depth mismatch. Either (a) run `Merge` first and inherit from the merged span, or (b) keep Kata's column-by-column carry and change the note to "nhịp sau vẫn kế thừa giá trị này". Needs one Kata check (B01 has J21 empty, so B01 does not decide it); add a test pinning the chosen behaviour.

**M3 — `KataDamSheetParser.cs:304-305` — behaviour change without test: `-` in row 20 of the first span now means "no side bars" and switches off the R-74 missing-side-bar warning (probe 3).** Before: `-` → empty → G4/G5. The spec (R-71) only defines `0` = none; `-` elsewhere in Kata means "continue from neighbour". The docstring/plan say "`0` resets" for N1. Fix: apply the sentinel only when `previous is not null` (reset to previous-less state = G4/G5), or confirm in Kata that `-` = none and add `First_span_dash_in_row_20_*` test + R-row in `docs/specs/kata-beam-rebar-rules.md`.

**M4 — `KataDamSheetParser.cs:309-311` vs `KataBarNotationParser.ParseOffsetAndBars:195` — row 21 `-` is read as "0" by the bar parser but as "no number → inherit" by `HasNumber` (probe 5).** Two rules for one cell (PCC-230). Fix: treat `-` like `0` in `HasNumber` (`cell.Trim() == "-"` → true) or document `-` = inherit and test it.

**M5 — `KataZeroWidthSupports.cs:19-43` — PCC-075/PCC-076 (Core purity) — `Merge` mutates three caller lists in place, returns void.** The parser already threads a `notes` accumulator (pre-existing), but `supports`/`spans` mutation is new. Fix: `public static (IReadOnlyList<KataSupportRebarSpec>, IReadOnlyList<KataSpanRebarSpec>) Merge(accessor, supports, spans, notes)` returning new lists; easier to test directly (no test calls `Merge` today).

**M6 — Tests (PCC-279–282) — gaps on the edges this diff opens:**
- `KataSpanCarryOverTests.A_row_20_that_reads_as_no_bar_*` uses `H20 = 300` — per spec §1 line 61 a pure number in row 20 is a **width change**, so the test pins a misleading message (probe 7). Use an unreadable token (`"abc"`), and give pure numbers their own note "đổi bề rộng nhịp — chưa hỗ trợ" (also B01 L20 = 300).
- Missing: left console through `KataRebarPlanner.Plan` with a measured run starting with a span (probe 4 passes — pin it), reversed + console, two consecutive zero supports (probe 6), zero support next to a console end, single-span with one console end, `ReportDifferences` row 20/21/inner-stirrup notes, unparseable support (H1), right-span row 21 bars (M1).

## Low
- **L1** `KataScopeFilter.cs:45` — `new[] { First, Last }.Distinct()` relies on record value equality to dedup; index loop over `{0, Count-1}` is clearer. When both ends are consoles the beam both blocks and gets two console warnings — skip warnings when blocked.
- **L2** `KataScopeFilter.cs:38-40` — message "Các gối giữa phải có bề rộng > 0; điểm nối chưa hỗ trợ." is also emitted for a support/span **count** mismatch (pre-existing condition shared); split the two messages.
- **L3** `KataZeroWidthSupports.cs:25` — descending loop emits merge notes in reverse sheet order; collect then reverse, or sort skipped by address.
- **L4** `KataZeroWidthSupports.cs:73-74` — `ToAddress(1, col).TrimEnd('1')` to get a column letter; add `KataDamCellAccessorExtensions.ColumnLetter(col)` (PCC-232).
- **L5** FM2 — `KataSheetGeometryCheck.cs:93` (~150 chars), `KataDamSheetParser.cs:323-324`, `KataScopeFilter.cs:48` past 120 chars; unbraced single-line `if`s (`:306`, `KataZeroWidthSupports.cs:47`) — match file style or brace.
- **L6** PCC-055 — `"0"`, `"-"` literals repeated across `InheritSteps`, `ReportCells`, `ParseBarList`, `ParseOffsetAndBars`; one `KataCellTokens.IsReset(text)` helper.
- **L7** R7 — `KataScopeFilter.cs:66` filters notes by the display string `"đai trong"` (pre-existing); the new `Outcome` field is the right place for a kind enum later.

## Verified OK
- Index math: `spans[k-1]`/`spans[k]` around `supports[k]` holds whenever `Supports.Count == Spans.Count + 1` (sheet starts with a support); malformed sheets still block in `KataScopeFilter:38`. Reindexing (`SupportIndex`/`SpanIndex`) keeps `KataSupportTopBarLayout`, `KataStirrupRuns`, `KataBarNumbering`, `KataDetailingRuleBuilder` messages consistent; merged span keeps the left `SheetColumn`, so `SheetSequence` column ordering stays monotonic.
- `SheetSequence` console guard indexes `Supports[i]` only when `i == 0 || i == Count-1` and `Count > 1` — no out-of-range; `WithMeasuredGeometry` leaves the console support at 0 so `KataBeamStations.IsLeft/RightCantilever` (pre-existing) still fires.
- Revit side: `KataBeamMatcher` segments with `InsertJoints = false` (`KataRunModels.cs:87`), so a beam drawn in two pieces across the zero-width support measures as one span — matches the merge. A free end produces no segment (`KataSegmenter.Segment`), matching the skipped console cell. Kata Export writes free ends as 0 (`KataElevationBuilder.cs:131`) → round trip now reads back as consoles.
- No double report: support rows 13–18 and right-span rows 17/18/19/22 are not noted by `ParseSupport`/`ParseSpan` (they note 23/24 only).
- `KataScopeResult` ctor change: one construction site; `KataCellNote.Outcome` optional → source compatible.

## Recommended order
1. H1 (exact-zero test + block unparseable), 2. M1, 3. M3/M4 decisions + tests, 4. M2 after one Kata check, 5. M6 tests, 6. M5 when touching Merge again.

## Unresolved (need Kata / user)
- M2: does Kata carry the right half's row 21/20 to the next span after a zero-width support? (decides inherit-before vs after merge)
- M3: does `-` in span row 20 mean "none" or "same as G4/G5" in Kata?
