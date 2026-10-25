# Phase 2 — viewport + plain elevation drawn by the page (2026-10-25)

## Done
| Area | Files (`HPBimToolkit.Frontend/src/kata/elevation/`) |
|---|---|
| Viewport (port of `KataElevationViewport`): fit, centreBand, zoomAt with limits, shrinkToHeight, panBy, ensureVisible | `viewport.ts` + `viewport.test.ts` (same 11 cases as the C# tests) |
| Scene rows (port of `KataElevationScene`), beamFaces, screenSpan, columnAt, focusRange | `scene.ts` |
| Framing (port of `KataElevationCanvas.Framing` + constants): frame / frameAll / frameColumn / minScale; band + rows are options so phase 3 frames Kata's drawing with its own | `framing.ts` |
| Painters as pure functions → shapes (px): geometry (`KataElevationPainter`), annotations (`KataElevationAnnotations`), label lanes, break lines, ticks | `elevationPainter.ts`, `elevationAnnotations.ts`, `labelLane.ts`, `shapes.ts` |
| Palette from the HP theme tokens (dark/light) + Kata layer colours for phase 3; WPF dash lengths converted to px | `palette.ts` |
| Text: Segoe UI 11 / 9.5 px, SemiBold; widths from canvas `measureText` (jsdom: average advance) | `text.ts` |
| Renderer + view state: SVG of shapes; framed whole until the user zooms/pans, refit on resize, new run → framed whole, same run rebuilt keeps framing, `kata.focus` frames a span/the run, table pick → ensureVisible; wheel (non-passive, never scrolls the page), middle/Shift drag pan, middle double click = Toàn dải, click → `kata.update {selectedColumn}`, Esc → close | `ShapesSvg.tsx`, `useElevationView.ts`, `NativeElevation.tsx` |
| Page wiring: `useKata` keeps `scene` (revision-filtered) + `focus`; `KataApp` draws natively when there is no rebar plan or bars and section card are off, else the WPF SVG; `?renderer=native|svg` forces one | `useKata.ts`, `KataApp.tsx` |
| Dev: mock bridge serves the B03 fixture (dynamic import — not in the add-in bundle), table rows from the scene, focus events for Trước/Tiếp/Toàn dải | `kataMockBridge.ts`, `main.tsx` |
| Live harness: native drawing, zoom/pan with 0 messages to Revit, click → view model, Toàn dải / Tiếp from focus requests; CDP mouse helpers | `HPBimToolkit/tools/harness/live-verify-kata-native.mjs`, `cdp-session.mjs` (`mouse.wheel/click/drag`) |

## Checks
| Check | Result |
|---|---|
| vitest / typecheck / lint | ✅ 111/111 (viewport 11, painters/framing/picking 9, app native 3 new) / ✅ / ✅ |
| `dotnet build HPRebar.slnx -c Debug.R26` | ✅ |
| Dev page (`npm run dev`, B03 fixture) | ✅ drawn, wheel zooms around the cursor, click selects F (table + band) |
| Live, harness Revit on a Dam kata copy (`live-verify-kata-native.mjs 9334`) | ✅ 7/7: 35 texts, all 11 letters, wheel 0.024 → 0.047, zoom+pan 0 bridge messages, click selects H via `kata.update`, Toàn dải 0.047 → 0.024, Tiếp 0.108 |
| Renderer switch with a plan read | ✅ bars off+section off → native; bars on or section on → WPF SVG |
| Visual vs WPF (same model, dark) | ✅ screenshot side by side: same letters, chains, captions, stubs, colours |
| `live-verify-kata-web-rebar.mjs` | ⚠️ 1/5 — the user's open workbook is now KataB02.xlsm with beam B03 → plan has 2 [Chặn] items; not caused by this phase (WPF path, data) |

## Not done / next
- Kata's rebar elevation, tags, cuts (phase 3); section card + cross sections (phase 4) — until then those modes still use the WPF SVG.
- Light theme only seen in the dev page; HiDPI not checked.
