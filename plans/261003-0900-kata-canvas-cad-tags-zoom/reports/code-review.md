# Code review — Kata canvas: CAD tags/bars in mm + AutoCAD zoom/pan

Date 2026-10-03. Scope = uncommitted files listed in task (Core: viewport, tag builder, tag style, drafting + tests; WPF: canvas, input, scene, tag/rebar painters, primitives, palette). Rules table of plan.md taken as contract, not challenged.

## Checks run
| Check | Result |
|---|---|
| `dotnet test HPRebar.Core.Tests` | ✅ 892/892 |
| `dotnet build HPRebar/HPRebar.csproj -c Debug.R26 -p:DeployAddin=false` | ✅ 0 errors, 0 warnings in Kata files |
| R25/R24 build, live Kata Export | CHƯA TEST |

## Overall
Solid. Core is pure + golden-tested against DY7/DY14/E500; viewport simplification is clean (one scale, tests updated); overlays now wrapped so an exception cannot kill the modeless window. No Critical. Main gaps: mirrored (reversed map) stirrup tags, tag rows beyond 2 levels collide, section-cut markers overprint the new tags, per-render rebuilds.

## Critical
None.

## High
None.

## Medium

### M1 — Stirrup tag misplaced on runs drawn backwards
`KataBarTagBuilder.cs:87-88` bakes `+StirrupShift` (125) into the layout-local X; `KataElevationBarTagPainter.cs:73-83` maps that X through the station map (mirrored when `Direction < 0`) but lays text + circle out screen-fixed (`right: true`, text left of insertion). Reversed run: insertion lands 125 mm *before* the zone centre and the text runs further left → tag ~250 mm off vs. Kata (leader tags are fine: `right = insertX >= x` mirrors the whole tag consistently).
Fix: keep the zone centre in the tag (`X = (first+last)/2`) and apply the shift in screen space: `insertX = X(tag.X) + KataTagStyle.StirrupShift * Scale` (or `X(tag.X + StirrupShift * map.Direction)` if the shift must stay in the builder). Add a painter-independent test via a helper returning the screen insertion for `sameOrder: false`.

### M2 — 3rd/4th top bar level collides with the stirrup row and leaves the tag band
`KataBarTagBuilder.cs:151-153` places row k at `100 + k·137.5`; `KataTopLayerStack.cs` allows up to 4 top levels (rows 13–16). Level 3 → 375 mm, on top of stirrup row 387.5 (`KataTagStyle.cs:35`); level 4 → 512.5 mm, past `BandAbove` 450 (`KataTagStyle.cs:60`) into the dim chain/letters (scene only reserves `BandAbove`). Outside DWG evidence (DY7/DY14 have 2 levels) so not a contract violation, but the canvas will overprint.
Fix: stirrup row = `max(StirrupRow, FirstRowAbove + levels·RowPitch)` per span (or per cut), and `TagBand()` computes the band from the built tags (`max RowZ + CircleRadius`) instead of the constants. Same for bottom (3 bottom levels → 412.5 > `BandBelow` 337.5).

### M3 — Section-cut markers overprint the bar tags at the same stations
Tags now sit exactly at `KataSectionCuts` stations; `KataElevationSectionPainter.PaintMarker` (`:203-215`, previous batch) draws the cut number at `sx+2, soffit+6 px`, painted after the tags. Worked case, 6 m span framed at 0.2 px/mm: right-pointing "3Ø18" text spans x+1.8…36 px, y 10.8…23 px below soffit; the cut digit occupies x+2…8, y 6…18 → overlap at every cut.
Fix: when `ShowRebar && ShowBarTags`, move the cut number below the tag band (`scene.BandBottomY + tagsBelowPx + 4`) or onto the left of the stroke for right-pointing tags; strokes can stay (they merge with the leader).

### M4 — Selected column can be scrolled under the now-opaque section card
`KataElevationCanvas.cs:262` `EnsureVisible(..., canvas.ActualWidth, MarginPx)` ignores `KataElevationSectionPainter.ReservedWidth` that `Frame` (`:197`) subtracts. Card became opaque (`KataCanvasPalette.cs:154` `Tint(fill, 255)`), so a column brought "into view" at the right edge is hidden behind it (top 282 px).
Fix: pass `ActualWidth - reserve` (same expression as `Frame`), extracted to a `UsableWidth()` helper.

### M5 — Everything rebuilt on every OnRender (each pan mouse-move / wheel notch)
- `KataElevationBarTagPainter.cs:43` `KataBarTagBuilder.Build` (cuts × levels × all bars × LINQ GroupBy) + `KataElevationSectionPainter.cs:46` `KataSectionCuts.Build` (string `Content` per cut) — both pure functions of `RebarPlan`.
- `KataElevationRebarPainter.cs:103-114` per bar: string key, `Outline` list, new `StreamGeometry`; no visibility culling.
- FormattedText per visible tag + per number; `Arrow` allocates a StreamGeometry per foot.
Fine for one beam today, scales linearly with spans; pan of a 10-span run = thousands of allocations per frame.
Fix: cache `(tags, cuts, drafted outlines + dedup)` keyed on `ReferenceEquals(RebarPlan)` (invalidate in the `RebarPlan` property callback); per render only transform points. Optional: cull bars by `IsVisible(minX,maxX)` before building geometry.

## Low

- **L1** `KataBarTagBuilder.cs:162-164` — `SideTag` needs a Start cut: a **left** cantilever (cuts Middle+End) never gets a side-bar tag, a right one does. Asymmetric; use the support-side cut (`Start ?? End`) and measure the third toward the middle cut. Cantilever behaviour is an assumption anyway (no Kata DWG) — note in plan.
- **L2** `KataElevationBarTagPainter.cs:85` comment "first number nearest the text" is false for left-pointing tags (`:92` puts k=0 farthest left, i.e. reading order kept). Verify P-tag circle order against T2-DY7 (no test pins it), then fix comment or code.
- **L3** `KataDrawPrimitives.cs:81-83` `PushTransform`/`Pop` without `try/finally`; an exception from `DrawText` inside an `Overlay` leaves the transform pushed for the next overlay (now that exceptions are caught and drawing continues).
- **L4** `KataElevationRebarPainter.cs:103` dedup key formats with current culture (vi-VN → `6762,0,-17280,0`); ambiguous separators. Use a value-tuple key `(Math.Round(X,1), Math.Round(Z,1))` sequence or `CultureInfo.InvariantCulture`.
- **L5** `KataBarDrafting.Tick` — end segment shorter than `TickAlong` (75) puts the slash past the next vertex; a 2-point vertical bar has `meanX == end.X` → both ticks +X. Clamp: `along = min(75, segmentLength/2)`.
- **L6** `KataElevationCanvas.Input.cs:58` — Shift+left pan removed. Matches contract ("middle drag pan"), but touchpad users now cannot pan at all. Keeping Shift+left as an extra fallback does not contradict the contract — 👤 user call.
- **L7** Dead/stale: `KataCanvasPalette.RebarStirrupZoneFill` (`:47`, `:144`) lost its only user with the old rebar painter; `RebarStirrupBrush` (`:55`, `:152`) already unused. Scene comments `KataElevationScene.cs:18,21` still say the px margins hold "bar tags". `MarkerTextY` (`:46`) not pushed past `tagsBelowPx` (crossing labels sit near supports, cuts ≥ 0.1 L away — rarely collides).
- **L8** `Overlay` dedups on `ex.Message`; messages with indices/values would still log every frame. Key on `name|type|TargetSite` instead.
- **L9** Untracked build leftover `HPRebar/HPRebar/HPRebar_*_wpftmp.csproj` — do not commit.

## Edge cases checked, OK
- Row ordering: top `OrderBy(Z)` → deepest level row A; bottom `OrderBy(-Z)` → highest row A (`KataBarTagBuilder.cs:138`) — matches contract.
- Short span (L<3 m, 3 cuts): Classify ±200 mm stays correct for L > 500; spans < 1 m (single Middle cut) tagged full, as DY14 16425.
- Level grouping by `Math.Round(Z)`: layer Z from one `TopBarCentreDepth` regardless of diameter, so mixed Ø on one layer stay one tag.
- Stirrup runs merge only within one span (global station order keeps same-span zones adjacent).
- Fillet: tangent points both exactly r from centre for any corner within ±78°; segments < 2r keep sharp corner; zero-length duplicates removed before drafting; `pts.Count ≥ 2` guaranteed before `outline[0]`.
- Arrow/zero leader guarded (`l < 1e-9`, `length < 0.5`); font size bounded (min 1.5 px, max ≈1 786 px at `MaxScale` 20 < WPF 35 791 limit).
- Theme: palette dropped on `SurfaceBrush` change; all new pens/brushes frozen; light-theme ink defined.
- Viewport: one scale, anchor kept in both axes, limits only block their own direction; tests updated.

## Recommended order
1. M1 (visible on every reversed run) → 2. M3 → 3. M4 → 4. M2 → 5. M5 → Lows.

## Plan status
Phase 1 + 2 appear implemented (code + golden tests). Phase 3: R26 build + tests done here; R25/R24 builds, live Kata Export not run.

**Status:** DONE_WITH_CONCERNS
**Summary:** No Critical/High; 5 Medium (reversed-run stirrup tag, >2 levels overlap, section markers over tags, card hides selected column, per-render rebuilds), 9 Low.
