# Phase 2 — TS viewport + beam elevation layer

**Context:** `View/Controls/{KataElevationViewport?,KataElevationScene,KataElevationPainter,KataElevationAnnotations,KataLabelLane,KataDrawPrimitives,KataCanvasPalette,KataElevationCanvas.Framing,KataElevationCanvas.Input}.cs`.

## Requirements
- `viewport.ts`: one scale both axes, `toScreen/toScreenY`, zoom about cursor (same notch factor as WPF), pan, frame-all / frame-span (Framing rules), clamps.
- `scene.ts`: port of `KataElevationScene` (constants, rows above/below the band) — pure, unit-tested against numbers computed from the C# formulas.
- SVG layers: model `<g>` in mm under `matrix(s,0,0,-s,…)`; annotation layer in px (letters, dim chains, grid bubbles, column captions, crossing-beam markers, selection band).
- Gestures local: wheel/trackpad zoom, drag pan (left/middle), pinch, double-click = frame all, keyboard ←/→ span; 60 fps via `requestAnimationFrame`, no bridge call.
- Column click → `kata.update {selectedColumn}` (VM stays source of truth).
- Palette → CSS variables, dark/light.

## Steps
1. Port pure layout functions first (scene, label lanes, dim chains) + vitest.
2. `ElevationSvg.tsx` renders from the scene store; replace `ElevationView` SVG-string injection behind a flag (`?renderer=native`) so the old path stays for comparison.
3. Mock bridge serves the fixture → `npm run dev` shows the beam without Revit.

## Success
vitest green; dev page shows the beam elevation matching a WPF screenshot of the same fixture (manual side-by-side); zoom/pan round trip 0 bridge messages.
