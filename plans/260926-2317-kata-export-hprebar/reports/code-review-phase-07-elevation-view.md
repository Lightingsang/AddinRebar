# Code review — KataExport elevation view

Date 2026-09-27 · reviewer: code-reviewer · read-only (no source changed) · **Score 8/10**

## Scope
- Core: `KataElevationModels.cs`, `KataElevationBuilder.cs`, `KataElevationViewport.cs`, `KataColumnLetters.cs` + tests `KataElevationTests.cs`, `KataElevationViewportTests.cs`
- Add-in: `View/Controls/*` (8 files), `KataExportViewModel.cs`, `KataPreviewBuilder.cs`, `KataViewFocus.cs`, `KataExportView.xaml`
- Tooling: `tools/theme-gallery/KataElevationGallery.cs`
- ~1 700 LOC. Context read: `KataSegmenter`, `KataRowBuilder`, `KataFormat`, `KataInputValidator`, `KataGridReader`, `MaterialThemeBridge`, palettes.

## Verification run by reviewer
| Check | Result |
|---|---|
| `dotnet build HPRebar.csproj -c Debug.R24 -p:DeployAddin=false --artifacts-path <scratch>` (net48) | ✅ 0 errors, no Kata warnings |
| `HPRebar.Core.Tests` (scratch artifacts) | ✅ 437/440; the 3 `ThemeTokenCoverageTests` failures are the scratch layout (they walk up to `HPRebar.slnx`) — re-run from a junction layout: 3/3 pass → 440/440 |
| `KataRowBuilder.cs` / `KataExcelWriter.cs` untouched | ✅ mtimes 00:12 / 01:10, elevation files 02:15+ |
| Gallery PNGs inspected (floor light, zoom dark, reverse dark, tie-beams light, long dark) | ✅ geometry/texts right; 1 overlap seen (M2), 1 edge label (L2) |
| Live in Revit (mouse in modeless window, wheel routing) | 🟡 CHƯA TEST |

## Acceptance criteria
| # | Verdict | Evidence |
|---|---|---|
| 1 one drawn column per sheet column, mirrored on Reverse, row-11 chain = cells | ✅ | texts are `Cell(sheet.RowXX, i)` + count guard [KataElevationBuilder.cs:29](../../../HPRebar/HPRebar.Core/KataExport/Calculators/KataElevationBuilder.cs#L29); reverse mirrors map + list together |
| 2 grid chain, captions, z/soffit, upper "w;o", row 23, row 21, letters | ✅ | `KataElevationAnnotations` all present; zero-texts suppressed by `IsNonZero` |
| 3 two-way sync, ‹ Trước/Tiếp ›, Toàn dải, dbl-click, wheel, pan | 🟡 | works; click on a wide span while zoomed re-centres the view (M3); lost capture leaves sticky pan (M1) |
| 4 true H scale, V exaggerated ≥ ~60 px, steps proportional | ✅ | `VerticalScale`; band cap may drop below 60 px for deep steps — by design, tested |
| 5 live Dark/Light | ✅ | `SurfaceBrush` DynamicResource → `_palette = null`; `MaterialThemeBridge` loads a new palette dictionary per apply, so the DP always changes (WPF compares reference types by reference) |
| 6 no crash (no grids / 1 span / free ends / joints / 75 cols), no overlaps | 🟡 | no crash path found (inputs validated finite, lists non-empty, all denominators clamped); selected letter overlaps neighbours (M2) |

## High
None.

## Medium

### M1 — Mouse capture loss not handled: sticky pan + stuck cursor
- [KataElevationCanvas.cs:110-160](../../../HPRebar/HPRebar/KataExport/View/Controls/KataElevationCanvas.cs#L110): state `_pressAt/_dragging/Cursor` is reset only in `OnMouseLeftButtonUp`; no `OnLostMouseCapture`; `OnMouseMove` never checks `e.LeftButton`.
- Failure: press on canvas → capture stolen (Alt+Tab, Revit autosave/worksharing modal, Win+D) → button-up never reaches canvas → every later hover pans the view, cursor stays `SizeWE`, until next click.
- Fix: `protected override void OnLostMouseCapture(MouseEventArgs e) { base.OnLostMouseCapture(e); _pressAt = null; _dragging = false; Cursor = null; }` and in `OnMouseMove` bail when `e.LeftButton != MouseButtonState.Pressed`.

### M2 — Selected letter drawn outside the lane overlaps neighbours (criterion 6)
- [KataElevationAnnotations.cs:84-94](../../../HPRebar/HPRebar/KataExport/View/Controls/KataElevationAnnotations.cs#L84): the lane skips the selected column, neighbours are placed where the bold selected letter is then drawn unconditionally.
- Evidence: gallery `kata-long-dark.png` — "AP AQ AR" touch/overlap at the selection.
- Fix: compute the selected label's `[left,right]` first (bold size), then place the other letters with the lane AND skip any whose interval intersects the reserved one (+Gap).

### M3 — Canvas click on a wide span re-centres the zoomed view
- [KataElevationCanvas.cs:208-216](../../../HPRebar/HPRebar/KataExport/View/Controls/KataElevationCanvas.cs#L208) runs `EnsureVisible` for every selection change, including the canvas's own click; [KataElevationViewport.cs:40](../../../HPRebar/HPRebar.Core/KataExport/Calculators/KataElevationViewport.cs#L40) pans to the range mid when the range is wider than the view.
- Failure: zoom to 1.2 px/mm on part of a 6 m span, click it → view jumps to the span centre (the part the user was looking at slides away). Same for a table pick of a partly visible wide span.
- Fix: in `EnsureVisible`, when `right - left > usable` and the range already overlaps `[margin, width-margin]`, return `this`; optionally skip `EnsureVisible` when the change originated from the canvas (flag set around the `SelectedColumnIndex` write).

### M4 — Export now coupled to drawing code; fields committed non-atomically (latent)
- [KataExportViewModel.cs:116-125](../../../HPRebar/HPRebar/KataExport/ViewModel/KataExportViewModel.cs#L116): `_sheet` is assigned, then `Elevation`/`PreviewColumns` are built from the drawing builder; catch filter only takes `ArgumentException`/`InvalidOperationException`.
  1. Any failure of the (drawing-only) elevation builder nulls `_sheet` → Export disabled for a sheet that was fine.
  2. An unexpected exception type (NullReference, IndexOutOfRange, Overflow…) escapes `RebuildSheet`; from `OnIsReverseChanged` the binding engine swallows it with `IsReverse` field already true and `_sheet` = reversed sheet, while table/elevation still show the old direction and `CanExport` is true → Excel receives a sheet that was never previewed. Violates the class invariant "what is shown is exactly what goes to Excel".
- No current trigger found (builder cannot throw on validated input) → latent, but it guards the one invariant this feature exists for.
- Fix: build sheet + elevation + preview into locals inside one `try (Exception)`, assign fields only after all succeed (else clear all + message); if the drawing should never block export, fall back to `Elevation = null` + warning while keeping `_sheet` and a sheet-derived preview.

## Low
- **L1 deep crossing girder leaves the band** — [KataElevationPainter.cs:77](../../../HPRebar/HPRebar/KataExport/View/Controls/KataElevationPainter.cs#L77) draws the section to `Y(top − h)` but band depth/`BottomMm` ([KataElevationModels.cs:81](../../../HPRebar/HPRebar.Core/KataExport/Models/KataElevationModels.cs#L81)) are beams only. Zoomed in (0.23 px/mm), a 300x800 girder under 220x500 beams protrudes ~69 px, over the letter row (+52). Fix: include `top − SectionHeightMm` of beam supports in the band bottom, or clamp to `BandBottomY + LowerStubPx`.
- **L2 bounds/grid reach ignore supports and upper columns** — [KataElevationBuilder.cs:33-38](../../../HPRebar/HPRebar.Core/KataExport/Calculators/KataElevationBuilder.cs#L33): upper-column extents are not in `natureBounds` (gallery tie-beams: "250;790" stub sits past the last footing at the canvas edge); grid reach is run ± 1 m, so a grid at the centre of a support that overhangs the run end by > 1 m is named in row 22 but not drawn. Fix: union upper extents into bounds; reach = union(run, column extents) ± 1 m.
- **L3 builder trusts the caller's sheet** — `Build(input, options, sheet)` only checks the column count; a sheet built with other `Reverse` passes and mislabels every column. Fix: one entry point returning `(sheet, elevation)` from the same options, or check each column's Row11 against the segment it maps.
- **L4 test gaps** — `EveryColumnCarriesItsExcelLetterAndTheTextsOfItsCells` asserts Row11/Row22 only; add all five rows × {normal, reverse} on a mixed run (free end + joint + girder + crossing + off-support grid); `ColumnAt` returning null; `FocusRange(0)` / last.
- **L5 XAML spacing** — 25 numeric `Margin`/`Padding` + fixed `Height`s in [KataExportView.xaml](../../../HPRebar/HPRebar/KataExport/View/KataExportView.xaml); `ColumnRebar` views use `Spacing.*` tokens. No hex colours (✅).
- **L6 conventions** — VM 255 lines (> 250 rule); `KataPreviewColumn` is a second public type in `KataPreviewBuilder.cs`; View control depends on ViewModel type `KataViewFocus` (move to `Model/` or give the control its own request type); unused `KataElevationBeam.WidthMm`, `KataCanvasPalette.Warning`.
- **L7 control writes DP with `SetValue`** — [KataElevationCanvas.cs:152](../../../HPRebar/HPRebar/KataExport/View/Controls/KataElevationCanvas.cs#L152): works with the TwoWay binding, but a future `Mode=OneWay` would be silently replaced; use `SetCurrentValue`.
- **L8 render cost during pan** — letters/bubbles/upper texts build `FormattedText` before the visibility check (~200/frame at 76 columns), and `Elevation.TopMm` is a LINQ `Max` per `Y()` call. Fine today; cache TopMm in the scene and test visibility on the anchor x before formatting if pan stutters.
- **L9 window size** — `Height="920"` default > work area of 1366×768 (728 DIP): Export button starts off-screen (GIẢ ĐỊNH CHƯA XÁC MINH). Clamp to `SystemParameters.WorkArea` in the command.
- **L10 light-theme contrast** — `Brush.Accent` #0696D7 (~3.4:1) and `Brush.Canvas.MainBar.Selected` #E07B00 (~2.9:1) on white for 9.5–11 px text are below WCAG AA 4.5:1.

## Positive
- Every drawn number is the sheet cell's own text; count guard; preview table fed by the same columns → drawing, table and Excel cannot drift apart.
- Geometry/hit-test/viewport in Core, immutable `record struct` viewport, xUnit coverage of fit/zoom/ensure/vertical scale, 75-column run, letters to BZ.
- `KataViewFocus.Serial` defeats the toolkit's value-equality suppression of repeated "Toàn dải".
- Reverse selection mirror captured before rebuild (`count − 1 − selected`) — correct whatever the ListBox does on `ItemsSource` reset.
- Palette: frozen pens/brushes, token fallbacks logged once per rebuild; no hex colours in XAML; namespaces match folders; no plan references in comments; code-behind minimal.
- Zero-width hit reach expressed in screen px; all divisions clamped (no NaN path found).

## Recommended actions (order)
1. M1 `OnLostMouseCapture` + button-state check (5 lines).
2. M4 atomic commit in `RebuildSheet`.
3. M2 reserve the selected letter's slot.
4. M3 no re-centre when the range already overlaps the view.
5. L1/L2 band + bounds; L4 tests.
6. Live check in Revit 2026: click/drag/wheel in the modeless window, capture loss (Alt+Tab mid-drag), theme flip.

## Plan follow-up
Phase 7 steps 1–4 appear complete (Core + tests, canvas, VM/XAML, gallery); step 5 (review fixes + live GMX3 / T1-DX12) open; success criterion "Live: …" unchecked.

## Unresolved questions
None needing the user.

## Fix round (2026-09-27)
- M1 fixed: `OnLostMouseCapture` + left-button check in `OnMouseMove` (`EndPress`).
- M2 fixed: selected letter reserves its space first; neighbours that would overlap skipped (gallery long-run: AQ clear).
- M3 fixed: `EnsureVisible` keeps a wider-than-view range still while on screen (test `RangeWiderThanTheViewStaysPutWhileOnScreenAndIsCentredOtherwise`).
- M4 fixed: sheet + elevation + table built into locals, published together; any exception → nothing exportable (+ Log.Error for unexpected types); table selection restored after ItemsSource swap.
- L1 fixed: `KataElevation.BottomMm` includes crossing-girder soffits; L2 fixed: bounds include columns above, grid reach from drawn extent; L3 fixed: builder recomputes row 11 per column, refuses a sheet of the other direction (test); L4: +4 tests (rows 19/21/23 all columns, reverse with free end + joint, `ColumnAt` null, girder/upper bounds); L6: VM 237 lines + `KataExportViewModel.Navigation.cs`, `KataPreviewColumn.cs`, unused `WidthMm`/`Warning` removed; L7 `SetCurrentValue`; L9 height clamped to work area.
- Not done (accepted): L5 numeric margins (same as other windows' local layout), L8 perf (no stutter seen), L10 light-theme contrast of 9.5 px accent text, `KataViewFocus` stays in ViewModel.
- Result: build R26 + R24 pass, Core tests 444/444, gallery 22/22, deployed 02:48. Live Revit: CHƯA TEST.
