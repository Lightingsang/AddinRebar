# Phase 3 — Rebar layers

**Context:** `View/Controls/{KataElevationCadPainter,KataCadDimPainter,KataCadText,KataCadTag,KataCadPens,KataElevationBarTagPainter,KataElevationSectionPainter}.cs`.

## Requirements
- Kata elevation (when "Thép" on): lines by pen (colour, lineweight, dash: HIDDEN/CENTER), dims Kata (ticks + text) and Run (arrows, no text), section flags (SECBAL block), grid bubbles, level mark, title — all from scene mm.
- Bar tags (number circles + leaders) with the same stacking as `KataBarTagBuilder` / painter; "Hiện số hiệu" toggle client-side.
- Section cuts drawn on the beam; layer toggles (bars / stirrups / tags / dims) instant, no bridge call except to persist VM flags.
- Text in model units scales with zoom like AutoCAD; minimum legible size rule ported.

## Steps
1. Pen table + `<marker>` defs (ticks, arrows) + CAD text helper.
2. Port each painter as a React component over pure geometry helpers; vitest on helper outputs (tag positions, dim text).
3. Compare with WPF on the fixture (screenshots) and on the old `kata.drawing` SVG in the same page (toggle).

## Success
vitest green; fixture with bars renders all layers; visual diff vs WPF accepted by eye on 2 beams (1 span, 2 spans).
