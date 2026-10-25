# Phase 7 — 3D view (three.js) (2026-10-25)

## Done
| Item | Where |
|---|---|
| 3D DTO: bar groups = one shape + X offsets (longitudinal bar once, stirrup zone / tie set at its stations), concrete boxes per span (split at top steps) + column stubs ±600 mm; keys = `KataLayoutRemoval.Key` (bars, zones); hooks not drawn | `HPRebar.Core/KataExport/Web/KataWeb3dModels.cs`, `KataWeb3dMapper.cs` |
| Stirrups = the plan's own `IndividualStirrups` grouped by zone + exact shape; sets laid like `KataBarSetCreator` (wrapped side-bar ties via `KataTieWrap.Lay`) | `KataWeb3dMapper` |
| Command `kata.model3d` (built when the tab opens, cached per plan) | `KataWebContracts.cs`, `KataWebBridge.Scene.cs` |
| three.js 0.186.1 (+ @types/three 0.186.0, exact pins), own lazy chunk (563 kB, 142 kB gz) | `package.json`, `vite.config.ts` |
| View: tubes merged per bar (cylinder per segment + ball per bend, corners stay sharp), `InstancedMesh` per group, translucent concrete + edges, orbit/pan/zoom, presets Phối cảnh / Mặt đứng / Mặt bằng / Đầu dầm (fit by projecting the 8 bound corners), toggles Bê tông / Đai, legend by role, hover tooltip, render on demand only | `src/kata/model3d/Rebar3DView.tsx`, `model3dGeometry.ts` |
| 2D/3D tab in the elevation card; one bar selection shared by both views (keys of the plan shown); Delete / Ctrl+Z / Esc in 3D; the last model stays while a new plan's model loads (no WebGL teardown, focus kept) | `KataApp.tsx`, `Model3dPanel.tsx`, `useKataPicking.ts` (`SharedBarSelection`) |
| Mock + dev page carry the B03 3D fixture | `kataMockBridge.ts`, `main.tsx`, `__fixtures__/kata-model3d-b03.json` |
| Live harness | `HPBimToolkit/tools/harness/live-verify-kata-3d.mjs` (+ `mouse.move`, `screenshot` in `cdp-session.mjs`) |

## Checks
| Check | Result |
|---|---|
| HPRebar.Core.Tests | ✅ 1637/1637 (3 new: bar count = `TotalBarCount`, keys ⊇ elevation keys, concrete follows spans/columns; fixture + size cap) |
| vitest / typecheck / lint | ✅ 155/155 (geometry 10 new incl. every preset frames all corners with edge > 0.8; app 3D test incl. same view after Delete + Ctrl+Z) / ✅ / ✅ |
| `dotnet build HPRebar.slnx -c Debug.R26` | ✅ 0 errors |
| Live, harness Revit on a copy of `Dam kata test.rvt`, B03 (9795052/54/59/61), KataB03.xlsm read only | ✅ 15/15: 108 groups, 571/571 bars, WebGL in WebView2, orbit render median 0.7 ms (CPU side), 4 presets, hover "Đai kín Ø10 × 18 · số hiệu 24", click selects, Delete 571 → 553, Ctrl+Z → 571, back to 2D |
| Screenshots | [phase-07-3d.png](phase-07-3d.png), [phase-07-3d-detail.png](phase-07-3d-detail.png) |

## Notes
- Run 1 of the live check failed Ctrl+Z: a new plan unmounted the view while its model loaded (focus lost, WebGL rebuilt). Fixed (`useModel3d` keeps the last model); pinned by the app test; run 2 15/15.
- `Dam kata.rvt` + the open KataB03.xlsm block the plan (sheet ≠ run); `Dam kata test.rvt` matches B03 — use it for live checks with that workbook.
- GPU frame time not measured (render call only); `npm audit` reports 4 pre-existing dev-tool advisories, none in three.
- Not done: hooks in 3D, section box / clipping, selecting inner U / C ties alone (they go with their zone, as in the elevation).
