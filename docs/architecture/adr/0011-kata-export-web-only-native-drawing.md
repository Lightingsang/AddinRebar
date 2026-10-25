# 0011 — Kata Export: web window only, drawn by the page, Revit 2026 only

- **Status:** Accepted (2026-10-25, by user — plan `plans/261025-0621-kata-web-native-renderer/`)
- **Tags:** [REVIT] [PROJECT]
- **Supersedes:** [0010](0010-kata-export-web-ui-in-hprebar.md) decisions 1 and 3 (the rest of 0010 stands)
- **Note (2026-10-25):** the drawing export (SVG / PNG / print) was removed at the user's request; see [0012](0012-kata-page-ui-kit-and-viewer-choices.md).

## Context
ADR-0010 kept the WPF Kata Export window as the drawing engine: the web window hid a `KataElevationCanvas` behind the browser, read its recorded `Drawing` back as SVG after each paint and forwarded the page's gestures to it. Every zoom or pan went through the host. Every drawing record is already pure and in millimetres in `HPRebar.Core` (`KataElevation`, `KataElevationDrawing`, `KataSectionDrawing`, `KataBarTag`, the rebar plan). So the page can draw everything itself and do things the WPF window cannot: smooth client-side zoom, hover and tooltips, minimap, find, measure, layers, export, and a 3D view. The user decided (2026-10-25) to move the drawing to the web completely, to delete the WPF window in every version and to work on Revit 2026 only.

## Decision
1. **Kata Export is the web window and exists on Revit 2026 only.** `KataExportCommand` and `KataExport/View/Web/` compile only when `RevitVersion == 2026` (`HPRebar.csproj`), and the ribbon button is added only there (`Application.cs`, `// Multi-version: Kata Export web only`). The WPF window `KataExportView`, its painters and its canvas (`KataExport/View/Controls/`) are deleted, and so is `HPREBAR_KATA_UI`. Kata Settings and Kata Rebar keep their WPF windows on every version.
2. **The page draws from model-space records.** The bridge sends `kata.scene`, built by `KataWebSceneMapper` (Core, pure), whenever the elevation, the plan or the station map change, and once when the page reports `kata.ready`. A scene that cannot be built arrives as a frame with an `Error` and no scene, so the page never waits forever. It answers `kata.section` and `kata.model3d` (`KataWeb3dMapper`) on demand, cached per plan. Zoom, pan, picking, hover and tools all run in the page. Only edits come back: `kata.update`, `kata.removeBars`, `kata.run`. `kata.drawing`, `kata.viewport`, `kata.canvas` and `HPWebHost.Rendering.DrawingSvgWriter` are removed.
3. The WPF layout and paint rules (`KataElevationScene`, `KataLabelLane`, `KataCadTag`, the painters) are ported 1:1 to TypeScript under `HPBimToolkit.Frontend/src/kata/elevation/` and tested by vitest against JSON fixtures that a Core test writes from a real plan (`HPREBAR_UPDATE_KATA_FIXTURES=1`). The 3D view uses three.js (MIT, pinned 0.186.1), in its own lazily loaded chunk.

## Consequences
- \+ One drawing implementation, in the page. Gestures cost no round trip, and the web-only features (3D, minimap, measure, export) need no host code.
- \+ About 4 400 lines are gone: the WPF window (800 lines of XAML), its canvas and painters (3 100) and the Drawing → SVG writer with its tests. Nothing paints twice any more.
- − Revit 2023–2025 and 2027 have no Kata Export. They have to wait until the WebView2 each of them ships has been checked.
- − There is no WPF fallback: a page regression blocks Kata Export. This is mitigated by the vitest goldens and the live harnesses in `HPBimToolkit/tools/harness/live-verify-kata-*.mjs`.

## Alternatives
- **Keep the hidden canvas (ADR-0010 decision 3):** two drawings, every gesture a round trip, and no way to add the web-only features.
- **Keep the WPF window on the other versions:** two UIs for one tool, drifting apart. The user chose to delete it.

## Rules
DEPENDENCY_RULES D6 (three.js approved with the plan); ADR-0002 (feature folder `KataExport/View/Web/`), ADR-0008 P5 exception as extended by ADR-0010.
