# Phase 2 — Canvas painter + layout + gestures

Files: `src/kataColumn/drawing/{columnLayout,columnPainter,ColumnDrawing}.ts(x)` + tests. Status: pending.

- Layout (model mm → sheet mm): elevation at 1:30 left, sections at 1:20 right in a column top-down (top storey
  first), gap = 2 × section size; one sheet space so one viewport scales everything (as AutoCAD paper).
- Painter → `ShapeList` (shapes/palette/kataCad): concrete thin, bars `BAR_PX` with Ø-scaled weight, hoops, dims,
  CT blocks, leaders + labels, section markers, section titles "1-1 TL 1/20". Theme palette from page theme.
- Canvas component: DPR-aware, resize observer, wheel zoom about cursor (`wheelNotches`), drag pan
  (`startPress/endPress`), fit button + double-click fit, click a section title → frame it.
- Replace `ColumnSectionGrid` in `ColumnRebarView`; keep `ColumnSection` SVG only if still used elsewhere.
- vitest: painter counts on the fixture (bars, cuts, titles), framing fits extents, gestures.
