# Code review: KataExport canvas, 2D zoom/pan (CAD-style)

Date 2026-09-27 · base HEAD 0002421 · read-only · **8/10**

## Scope
- `HPRebar.Core/KataExport/Calculators/KataElevationViewport.cs` (+ tests)
- `HPRebar/KataExport/View/Controls/KataElevationScene.cs`, `KataElevationCanvas.cs`, NEW `KataElevationCanvas.Input.cs`, `KataElevationPainter.cs`, `KataElevationAnnotations.cs`
- `HPRebar/KataExport/View/KataExportView.xaml` (hint text only)
- 8 files, +152 / −173 plus 117 new lines. Export path (VM / Service / COM writer) untouched, confirmed by `git diff --stat`. No other callers of the changed symbols (`ZoomAt`, the static `VerticalScale` that was removed, `BubbleY`).

## Checks run
| Check | Result |
|---|---|
| `dotnet test HPRebar.Core.Tests` | ✅ 446/446 (re-run by the reviewer) |
| Build R26/R24, gallery 22/22 | per caller, not re-run |
| Phase-8 gallery screenshots (scratchpad `p8/gallery`) vs phase 7 (`p7/gallery3`) | inspected: whole run, tie beams, 37 spans fit; **zoom-on-span clips the caption** (see M1) |

## Acceptance criteria
| # | Criterion | Verdict |
|---|---|---|
| 1 | Wheel zooms both axes around the cursor, anchor still while the exaggeration fades, limits 0.5× fit .. 3 px/mm | ✅ `ZoomAt` math correct (x by station, y by depth ratio `after/before`). Tested at 2×, 20× (crosses MinV), 0.5×. Below MinV only x zooms, which is what criterion 3 says |
| 2 | Middle drag pans, Shift+left pans, left click selects, left drag without Shift does nothing, double click (M/L) frames all | ✅ `OnMouseDown`/`OnMouseUp` filter on `ChangedButton`. `ClickCount` is counted per button in WPF, so a middle double click works. Capture is taken on press and released in `EndPress`/`OnLostMouseCapture` |
| 3 | Vertical scale = max(h, 60 px / deepest beam), no band cap | ✅ `VerticalScale` property + `Scene.MinVerticalScale` |
| 4 | Labels move with the drawing at constant pixel size | ✅ every row is relative to `BandTopY`/`BandBottomY`. `Height` is used only for the background fill |
| 5 | Trước/Tiếp/Toàn dải frame horizontally as before and centre the band; a table pick pans only horizontally | 🟡 centring OK, `EnsureVisible` changes only `OffsetPx` (tested). **"As before" is not exact:** the focus cap went from 1.2 to 3.0 (M2), and short spans lose their bottom rows (M1) |
| 6 | No crash at the zoom limits, with zero-width columns, on resize; capture always released | ✅ every divisor > 0 on reachable paths (`Fit` scale > 0, `MinScale` > 0, `VerticalScale ≥ Scale`). `OnRender` guards sizes below 1 px. Before layout `FrameAll` uses width 1, and the first `SizeChanged` re-frames because `_userFramed` is false |

## Critical
None.

## High
None.

## Medium

### M1: Trước/Tiếp on a short span pushes the rows the user checks (letters, row-11 chain, caption) off the bottom
- Where: `KataElevationCanvas.cs:97-103` (`Frame`) → `KataElevationViewport.cs:42-47` (`CentreBand` top-aligns when it does not fit).
- Evidence: gallery host 1040×380, which gives about the same canvas height as the real window's `*` row (MinHeight 300). `kata-floor-zoom` (4.8 m span, stepped neighbour) cuts the focused span's own caption "Nhịp 3 – 220x500" in half at the bottom edge. The phase-7 shot showed it whole, because of the old 150 px band cap.
- Scenario: a 1.5 m cantilever plus its supports gives a range of about 2.2 m, so the scale is about 0.4 px/mm and the vertical scale is 1:1. A 500 mm beam is then 200 px, so the band and its labels need 88 + 200 + 150 = 438 px against about 326 px of canvas. The row-11 chain (BandBottom + 86 = 374 px) is off screen, and it is the value being exported.
- Status: this follows confirmed criteria 3 and 5 ("no band cap"; "top labels kept visible when it does not fit"). It is not a code defect, but the effect is visible, so it needs a **user decision**.
- Recommended fix, framing commands only (the wheel and table picks are unchanged): zoom to extents in both axes, like CAD ZOOM E.
  ```csharp
  // in Frame, after the MaxScale cap
  double room = Math.Max(1.0, ActualHeight) - KataElevationScene.AbovePx - KataElevationScene.BelowPx;
  double bandMm = Math.Max(1.0, elevation.TopMm - elevation.BottomMm);
  double fitY = room / bandMm;                          // scale at which the band exactly fits
  if (viewport.Scale > fitY && fitY > viewport.MinVerticalScale)
      viewport = viewport.ZoomAt(fitY / viewport.Scale, width / 2.0, 0.0, 0.0, MaxScale);
  ```
  Long spans (where the horizontal fit is the binding limit) frame exactly as now. Short spans zoom out just enough to show the chain and the captions. When the exaggeration alone overflows (a deep crossing girder), it falls back to the current top-aligned layout.

### M2: `MaxScale` is shared by the wheel limit and the focus cap, so "frame horizontally as before" changed
- Where: `KataElevationCanvas.cs:23` (1.2 → 3.0) is used by `Frame` at `:101`.
- Scenario: the focus range is under `usable/1.2` (about 720 mm at 1040 px, about 1 480 mm on a maximised 1900 px window). Trước/Tiếp now zooms up to 2.5× further than before. With the vertical scale at 1:1 this makes M1 worse: the beam soffit itself can leave the canvas.
- Fix: `private const double FocusMaxScale = 1.2;` for `Frame`, and keep `MaxScale = 3.0` for the wheel. This is moot if the M1 fix is applied, because the vertical fit then bounds it.

## Low
- **L1: after a window grows, a wheel-out step zooms in.** `KataElevationViewport.cs:55` clamps to `minScale`, which `KataElevationCanvas.cs:106-107` recomputes from the current `ActualWidth`. Scenario: zoom out to the minimum in a small window, maximise (the view is kept because `_userFramed` is true), then wheel out: the new minimum is about 1.9× the current scale, so the view jumps in. The same clamp existed at HEAD (0.8× fit). Fix: clamp only in the direction of travel, `double lo = Math.Min(minScale, Scale), hi = Math.Max(maxScale, Scale);`.
- **L2: `Cursor` doubles as the "drag started" flag.** `KataElevationCanvas.Input.cs:71`. This couples state to a UI property: any future style or setter that sets `Cursor` would skip the threshold. Related: a left press that moves 4 px or more and comes back within 4 px of the start still selects on release (`:85`), because HEAD's latched `_dragging` was dropped. Fix: a `_moved` bool latched when the threshold is crossed, used for both the pan start and the click test.
- **L3: the hint text got about 15 characters longer and has no trimming.** `KataExportView.xaml:155`. The DockPanel measures it before the fill `SelectionText`. At MinWidth 760 the hint (about 470 px) exceeds the room left after the title and buttons: it is clipped mid-word and `SelectionText` collapses to 0. Fix: add `TextTrimming="CharacterEllipsis"` + `ToolTip`, or shorten to "Lăn: phóng · Giữa / Shift+kéo: di chuyển · Bấm: chọn · Nhấp đúp: toàn dải".
- **L4: the public Core API has no guard on degenerate input.** `ZoomAt` divides by `VerticalScale`: `default(KataElevationViewport)` or a scale of 0 or NaN gives NaN offsets, which WPF then draws as nothing. No current caller can reach this. A one-line guard would do: `if (!(verticalBefore > 0) || !(Scale > 0)) return this;` (netstandard2.0 has no `double.IsFinite`).
- **L5: for a run shorter than about `usable/6` mm (about 155 mm at 1040 px), `MinScale` exceeds `MaxScale`.** `ZoomAt` then snaps to above 3 px/mm. Not realistic for a beam run. `Math.Min(MinScale(e), MaxScale)` closes it.
- **L6: test gaps.** `KataElevationViewportTests.cs:55-67` does not assert the vertical anchor at the clamp limits, nor the horizontal anchor for `floored`. There is no test for a current scale outside [min, max] (L1).

## Scout edge cases walked (no defect)
- WPF routing: `OnMouseDownThunk` calls `OnMouseDown` before it re-raises `MouseLeftButtonDown`. Setting `Handled` suppresses the sub-events, and no ancestor relies on them.
- Double click: the first click's up event selects and releases capture. The second down (`ClickCount == 2`) calls `EndPress` and `FrameAll`. Its up event is ignored because `_pressButton` is null.
- Mixed buttons (left held, then middle, or the reverse): the later press takes over, and the stray up event is ignored. Capture stays until the matching up event or until capture is lost.
- Shift: `Keyboard.Modifiers` reads the thread key state, and the modeless window runs on Revit's UI thread, so the value is correct.
- Elevation rebuilt mid-drag: `_viewport` is set to null, moves are ignored until the next render, and the up event still ends the press.
- Reverse: the rebuild keeps the viewport (same `Bounds` and count), the mirrored `SelectedColumnIndex` triggers `EnsureVisible`, and the VM's `FrameAll` (new `Serial`) re-frames the whole run.
- `MinVerticalScale` is stored in the viewport and kept across same-bounds rebuilds. Name and count parameter changes do not change beam heights, so this is fine today. If a future rebuild can change heights with the same bounds, refresh it in `OnRender`.
- Performance: `InvalidateVisual` per mouse move is coalesced to one render per frame. This is the same cost as HEAD's horizontal pan.

## Conventions
✅ `sealed partial`, file-scoped namespaces, the `Point` alias in the new partial file, comments explain why and contain no plan or phase references, every file under 300 lines (max 180), the feature-folder layout is respected (a partial file under `View/Controls/`).

## Positive
- The Core viewport stays pure and immutable, and the 2D maths are unit tested (anchor in both axes, including across the exaggeration boundary).
- Rows are laid out relative to the band. This removes the absolute `BubbleY`/`BeamTopY` constants cleanly, and the painter and annotations changed by only 5 lines.
- Capture handling covers lost capture, release outside the window and the double-click path.

## Plan follow-ups (report only, plan not edited)
- Success criterion "Core viewport tests": appears met.
- Criterion "Build R26 + R24, gallery 22/22": met per the caller.
- Criterion "Live in Revit": pending. Include in the live pass: middle drag, Shift+drag, a double middle click, Trước/Tiếp on the shortest span (M1), and a maximise followed by a wheel-out at minimum zoom (L1).

## Unresolved question (user decision)
1. M1: for Trước/Tiếp/Toàn dải, accept that short spans lose their bottom rows (the current contract), or fit the band vertically too (ZOOM E)? **Recommendation:** fit both axes, for framing commands only.

**Status:** DONE

## Fix round (2026-09-27)
- M1 fixed (self-decided: contract says framing "centres the band", nothing about hiding the chain): `KataElevationViewport.ShrinkToHeight` — framing commands (Trước/Tiếp/Toàn dải/double click) zoom out until band + labels fit the height, like ZOOM Extents; wheel/pan untouched. Test `ShrinkToHeightZoomsOutAShortSpanUntilItsLabelsFit`. Gallery zoom scene: caption + chains whole again.
- M2 fixed: `FocusMaxScale` 1.2 for framing (as before), `MaxScale` 3.0 wheel only.
- L1 fixed: limits stop movement only in their own direction (test `ALimitAlreadyPassedStopsOnlyFurtherMovementThatWay`); L4: `ZoomAt` returns unchanged on 0/NaN/∞ scale; L2: latched `_moved`; L3: shorter hint + trimming + full tooltip; L6: vertical anchor asserted at the cap.
- Not done: L5 (runs < ~155 mm, unrealistic).
- Result: Core 448/448, build R26 + R24 pass, gallery 22/22. Live: CHƯA TEST.
