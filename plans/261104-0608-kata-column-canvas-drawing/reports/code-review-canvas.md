# Code review — Kata Column Rebar canvas drawing (2026-11-04)

Score **7/10**. Read-only. Transforms (`toScreen`/`toSheet`/`fit`/`zoomAt`) checked algebraically: y-up correct, fit centres, zoom keeps the cursor's sheet point. Section y-up = plan seen from above, same x as elevation (b to the right) → consistent. Contract fields in sync. Mapper null paths fine (errors, refused sheet, `storeys.Count == 0`).

## High

**H1 — Bar label counts are double when both layers have the same Ø** — [KataColumnDrawing.cs:112-113](../../../HPRebar/HPRebar.Core/KataRebar/ColumnStack/KataColumnDrawing.cs#L112)
Count key = (storey, group, Ø) but one bar is drawn per (storey, group, **layer**). Fixture `kata-column-rebar-sample.json`: BASEMENT = 40Ø25 total (row `mainBars`), the drawing shows four labels "20Ø25" (A L1, A L2, B L1, B L2) → reads as 80 bars. FLOOR 2 (layer Ø differs) is right (12+8+12+8). Construction drawing quantity is wrong. The test [KataColumnDrawingMapperTests.cs:51-52](../../../HPRebar/HPRebar.Core.Tests/KataRebar/ColumnStack/KataColumnDrawingMapperTests.cs#L51) hides it with `g.First().Count` per (group, Ø).
Fix: count per layer too (`&& b.Run.Layer == run.Layer`), or keep the per-group count and emit the label only on the first layer of each (group, Ø). Test: Σ over **all drawn bars** of `Count` == storey bar count.

**H2 — Wheel zoom also scrolls the page** — [ColumnDrawing.tsx:70-74,137](../../../HPRebar/HPBimToolkit.Frontend/src/kataColumn/drawing/ColumnDrawing.tsx#L70)
React's `onWheel` is passive; the drawing sits in `<main overflow-auto>` ([ColumnRebarView.tsx:48](../../../HPRebar/HPBimToolkit.Frontend/src/kataColumn/ColumnRebarView.tsx#L48)) with the table above it, so the wheel zooms *and* scrolls `main`; the box moves under the cursor and the zoom anchor drifts. The beam page already solved this ([NativeElevation.tsx:178-189](../../../HPRebar/HPBimToolkit.Frontend/src/kata/elevation/NativeElevation.tsx#L178)).
Fix: `useEffect` adding `element.addEventListener('wheel', h, { passive: false })` with `e.preventDefault()`, reading the view through a ref / functional `setView(v => zoomAt(v, …))`.

## Medium

- **M1 — Zoom unbounded** — [columnSheet.ts:108-111](../../../HPRebar/HPBimToolkit.Frontend/src/kataColumn/drawing/columnSheet.ts#L108). Repeated notches → scale → 0 or huge (lost drawing, float blow-up in SVG coords). Clamp like `framing.ts` (`MAX_SCALE`, `fit.scale * MIN_SCALE_OF_EXTENTS`).
- **M2 — Stuck press** — [ColumnDrawing.tsx:76-117](../../../HPRebar/HPBimToolkit.Frontend/src/kataColumn/drawing/ColumnDrawing.tsx#L76). No `onPointerCancel`/`onLostPointerCapture`; when capture fails (the `catch` path) and the button is released outside the box, `press` stays set and the next plain mouse move pans. Fix: clear `press` on cancel / lost capture, and in move drop it when `e.buttons === 0`. Also `preventDefault()` on pointerdown for button 1 (Chromium middle-click autoscroll fights the pan in a scrollable `main`).
- **M3 — Re-fit discards the user's zoom** — [ColumnDrawing.tsx:52-55](../../../HPRebar/HPBimToolkit.Frontend/src/kataColumn/drawing/ColumnDrawing.tsx#L52). `sheet` is a new object on every `column.reload` (incl. the one after every Generate) and on any box width change (window resize, `main` scrollbar appearing when warnings show). Acceptable on a *different* column; annoying after Generate (user was inspecting a bar). Fix: re-fit only when a content key changes (`summary.name` + storey count, or a JSON hash) or the user has not moved yet (`userMoved` ref); on resize keep scale and shift offsets by half the size delta. Use `useLayoutEffect` to avoid one frame of the new sheet under the old view.
- **M4 — Full repaint per pointermove** — [ColumnDrawing.tsx:63,93](../../../HPRebar/HPBimToolkit.Frontend/src/kataColumn/drawing/ColumnDrawing.tsx#L63). Every pan event rebuilds all shapes in screen px and React diffs them (`ShapesSvg` keyed by index). Hoop lines are bounded by the real hoop count (sample 151; ~1 000 for 30 storeys), not by zoom — not thousands for typical stacks — but sections (bars + numbers) and hoops together reach ~3–5k elements on tall stacks → jank. Fix (cheap): rAF-coalesce `setView` during drag; (better) paint in sheet mm once per zoom level and pan with a `<g transform>`; optionally cull shapes outside the viewport.
- **M5 — No pile cap (B9 = 0)** — [KataColumnDrawing.cs:76-84](../../../HPRebar/HPRebar.Core/KataRebar/ColumnStack/KataColumnDrawing.cs#L76) + [columnSheet.ts:75](../../../HPRebar/HPBimToolkit.Frontend/src/kataColumn/drawing/columnSheet.ts#L75). Starters then run one lap (~1 000 mm) below the base, but `BottomMm` = base, so fit cuts ~400 mm of them off; a zero-height cap rect and a "PILECAPS" level on top of the first storey's level (overlapping text). Fix: bottom = min(cap bottom, min bar z); skip cap rect + PILECAPS level when `PileCapHeightMm == 0`. Add a mapper test.

## Low

- **L1 (K2/K1)** Representative "outermost bar of group" logic exists twice: mapper [KataColumnDrawing.cs:105-110](../../../HPRebar/HPRebar.Core/KataRebar/ColumnStack/KataColumnDrawing.cs#L105) and `KataColumnElevationDims.Laps` [KataColumnElevationDims.cs:59-64](../../../HPRebar/HPRebar.Core/KataRebar/ColumnStack/KataColumnElevationDims.cs#L59). Same today; drift moves lap dims off the drawn bar. Extract one `Representative(bars, storeyKey, group, layer)`.
- **L2 (K1)** Hoop count re-derived on the page [columnPainter.ts:220](../../../HPRebar/HPBimToolkit.Frontend/src/kataColumn/drawing/columnPainter.ts#L220) = `KataColumnRebarSummaryMapper.Count`. Send `count` in `KataColumnDrawingHoopZone`.
- **L3 (N9)** `e.rightMm + 4 * 300` [columnPainter.ts:195](../../../HPRebar/HPBimToolkit.Frontend/src/kataColumn/drawing/columnPainter.ts#L195) → `DIM_GAP_MM`. `'PILECAPS'` literal duplicated in [ColumnDrawing.tsx:165](../../../HPRebar/HPBimToolkit.Frontend/src/kataColumn/drawing/ColumnDrawing.tsx#L165) — use `elevation.levels[0].name`.
- **L4** Contract drift guard gaps [columnContracts.test.ts:42-63](../../../HPRebar/HPBimToolkit.Frontend/src/kataColumn/columnContracts.test.ts#L42): `KataColumnElevationPoint` (a `record struct`, regex `record <Name>(` would not match), enum literals `'starter'|'main'|'top'`, `'a'|'b'`, and the inline `stations` type are unchecked.
- **L5** Mixed lapped + dowelled bars in one (storey, group, layer): outermost wins, so a wider upper storey shows the dowel foot as "the" bar while the label counts all. Document or prefer `Partner`-lapped bars.
- **L6** Double click on a bar = pick + unpick + fit; double click in a section = frame + frame + fit. Harmless, maybe ignore clicks with `e.detail > 1`.
- **L7 (C2)** columnPainter.ts 313 lines; section painting (`paintSection`, `paintSectionDims`) is a natural split. **(T3)** loop in [KataColumnDrawingMapperTests.cs:30](../../../HPRebar/HPRebar.Core.Tests/KataRebar/ColumnStack/KataColumnDrawingMapperTests.cs#L30) → `[Theory]` over storeys.
- **L8** Section-only bars in the chip show group "–" and no length; group/length could come from the design via number lookup (most bars are not representative).

## Checked, OK
- `fit`: top/bottom/left/right map to margins (algebra); `zoomAt` invariant; `toSheet` inverse of `toScreen`.
- `xAt` interpolation and end fallback; label heights per kind/group/layer don't collide with hoop tags for sample storeys.
- Hit testing: segment distance clamp, circle `r` min 1.5 px + 6 px reach; hits in the same px frame as `local()`.
- Hoop band/line toggle protects `spacingMm <= 0` (band branch) — no infinite loop.
- Picked-bar drop effect deps correct; `paletteFor` returns static objects (memo stable).
- Session refused path passes `null` Elevation ([KataColumnRebarSession.cs:87](../../../HPRebar/HPRebar/KataExport/Service/KataColumnRebarSession.cs#L87)); view hides drawing on any error.

**Status:** DONE_WITH_CONCERNS — H1 (wrong bar counts on labels) and H2 (wheel scrolls page) should be fixed before calling the slice done.

## Fix round 2026-11-04
| # | Fix |
|---|---|
| H1 | `KataColumnDrawingBar.Labelled`: one label per storey key, group and Ø (VC labels group + Ø, v00 KHT "8Ø20"/"12Ø25"); test: labels of every storey add up to its bars |
| H2 | native `wheel` listener `{ passive: false }` + `preventDefault`, functional `setView` |
| M1 | `clampZoom`: max 20 px/mm, min fit / 4; test |
| M2 | `pointercancel` / `lostpointercapture` / `buttons === 0` end the press; `preventDefault` on pointerdown |
| M3 | refit only on content change (JSON of elevation + sections) or first size; reload after Generate and resize keep the zoom; `useLayoutEffect` |
| M4 | pan coalesced to one `setView` per animation frame, flushed on pointerup |
| M5 | no cap (B9 = 0): no PILECAPS level, cap = storey width, bottom = lowest bar point; painter draws the cap only below storey 0; test |
| L1 | `KataColumnElevationDims.StoreyKey` / `Outermost` shared by laps and drawing |
| L2 | hoop `Count` sent in the zone (`KataColumnRebarSummaryMapper.Count`) |
| L3 | `DIM_GAP_MM`; starter level from the cap level, else storey 0 |
| L7 | sections → `columnSectionPainter.ts`, shared consts → `columnStyle.ts` (painter 225 lines) |
Not fixed (noted): L4 contract test misses `record struct` + enum values; L5 dowel may be the outermost bar; L6 double click on a bar picks + unpicks then fits; L8 chip without length/group for section-only bars.
Checks: Core 1824/1824, vitest kataColumn 41/41, tsc + eslint clean.
