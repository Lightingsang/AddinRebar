# Phase 1 — scene contract (2026-10-25)

## Done
| Area | Files |
|---|---|
| Wire records (mm, `{x,z}` points, Core enums → camelCase strings) | `HPRebar.Core/KataExport/Web/KataWebSceneModels.cs` |
| Pure mapper: elevation + rebar (Kata elevation lines/dims/flags/bubbles/level/title, tags, cuts, bar count, kg) + section on demand; 0.1 mm rounding | `HPRebar.Core/KataExport/Web/KataWebSceneMapper.cs` |
| Tests (7): no-plan scene, rebar = drawing builders (pens, point counts, keys, dims, tag insert X, cuts), rounding, section + unknown number, wire shape, size < 400 KB, fixtures in step | `HPRebar.Core.Tests/KataExport/KataWebSceneMapperTests.cs` |
| Fixtures (B03 as "Dam kata test.rvt" models it: 4 spans, 33.6 m, 74 lines, 50 dims, 50 tags, 10 cuts, 571 bars) — rewrite with `HPREBAR_UPDATE_KATA_FIXTURES=1` | `HPBimToolkit.Frontend/src/kata/__fixtures__/kata-scene-b03.json` (71 KB indented), `kata-section-b03-1.json` |
| Bridge: event `kata.scene {revision, scene}` on Elevation/RebarPlan/RebarStationMap change (coalesced, also on Resend), event `kata.focus` from `FocusRequest`, commands `kata.section {number, mirror?}` (cached per plan/number/mirror, mirror defaults to station map direction) and `kata.removeBars {keys}` (→ `RemoveBarsCommand`, 1…5000 keys, BUSY when not allowed) | `View/Web/KataWebBridge.Scene.cs` (new partial), `KataWebBridge.cs`, `KataWebContracts.cs` |
| TS mirror + drift guard over every scene record and 7 Core enums | `src/kata/kataScene.ts`, `kataContracts.ts`, `kataContracts.test.ts` |

`kata.drawing` (hidden WPF canvas → SVG) still runs: it is the parity reference until phase 6.

## Deviation
3D DTO (bars/stirrups/concrete) moved to phase 7: designed with the renderer that uses it, not ahead of it.

## Checks
| Check | Result |
|---|---|
| `dotnet build HPRebar.slnx -c Debug.R26 -p:DeployAddin=false` | ✅ |
| HPRebar.Core.Tests | ✅ 1634/1634 |
| HPWebHost.Tests | ✅ 68/68 |
| vitest / typecheck / lint | ✅ 88/88 / ✅ / ✅ — drift test caught `KataTagLayout.Centred` missing in TS, fixed |
| Live `kata.scene` / `kata.section` / `kata.removeBars` in Revit | CHƯA TEST — first live use is phase 2 (page draws from the scene) |
