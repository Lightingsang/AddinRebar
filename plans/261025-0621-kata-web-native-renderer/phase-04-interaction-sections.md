# Phase 4 — Interaction + sections

**Context:** `View/Controls/{KataElevationCanvas.Edit,KataElevationCanvas.Section,KataSectionView,KataSectionCadPainter}.cs`, `ViewModel/KataExportViewModel.Removal.cs`.

## Requirements
- Hit test in the browser (SVG pointer events on fat invisible strokes, tolerance in px): hover highlights a bar/stirrup group (all lines sharing a key), tooltip with number, Ø, length, layer.
- Click selects the group (hint "Delete: xóa" kept), Shift adds; Delete → `kata.removeBars {keys}`, Ctrl+Z → `undoRemoveBars`; Esc clears, unused Esc closes (as today).
- Section flag click → `kata.section` (cached per number/mirror) → section panel: native SVG with bulge arcs, own zoom/pan, bars + leaders + tags.
- Selection state survives a new scene only when its keys still exist.

## Steps
1. Read Edit/Section C# rules (what counts as a group, mirror logic).
2. Implement hit layer + selection store + tooltip; section panel component.
3. Live-verify harness additions (pick, delete, undo, open section) adapted from `live-verify-kata-web-rebar.mjs`.

## Success
vitest on selection store; live: delete 6 bars → 452, undo → 458 (same as phase 5 of previous plan); section opens.
