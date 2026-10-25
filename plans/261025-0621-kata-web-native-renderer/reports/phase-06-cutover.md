# Phase 6 — Cut-over: WPF Kata window deleted, page only, R26 only

Date 2026-10-25. Decision record: [ADR-0011](../../../docs/architecture/adr/0011-kata-export-web-only-native-drawing.md).

## Changes
- **C# (HPRebar):**
  - `KataWebWindow`: hidden `KataElevationCanvas` removed; content = WebView2 host only.
  - `KataWebBridge`: takes the window `Dispatcher`.
    - Removed: `kata.drawing`, `kata.viewport`, `kata.canvas`, the gesture forwarding and the SVG write.
    - The first `kata.getState` schedules the scene. Before, the scene first went out on the page's viewport report.
  - `KataWebContracts`: removed the records `KataViewportRequest`, `KataCanvasInput`, `KataCanvasResult`, `KataDrawingFrame` and the class `KataCanvasGestures`.
  - `KataExportCommand`: always opens the web window. `HPREBAR_KATA_UI` is gone.
  - Gating:
    - `HPRebar.csproj` compiles the command and `View/Web` only for 2026.
    - `Application.cs` adds the ribbon buttons under `#if REVIT2026` (also with `KATA_ONLY`).
- **Deleted:**
  - `KataExportView.xaml(.cs)` (813 lines of XAML)
  - `KataExport/View/Controls/**` (3 131 lines: canvas and painters)
  - `HPWebHost/Rendering/DrawingSvgWriter.cs` and its tests
  - `tools/theme-gallery/KataElevationGallery.cs`
  - Total ≈ 4 460 lines.
- **Frontend:**
  - `ElevationView.tsx` deleted. `useKata`, `kataContracts`, `KataApp` and the mock bridge (which now sends the scene after `getState`, as the host does) updated.
  - Until the scene arrives the page shows "Đang dựng mặt đứng dải dầm…".
  - `panOffset` removed.
- **Harness:**
  - `live-verify-kata-web.mjs` and `live-verify-kata-web-rebar.mjs` moved from `kata.canvas` calls to real mouse and keyboard input.
  - `live-verify-kata-native.mjs`: the bar is found by hovering until a tooltip appears. Before, the script clicked the first `line-*` shape, which can be concrete; Ctrl+Z now only counts after a real removal.
- **Docs:** ADR-0011 (+ README, ADR-0010 header), CLAUDE.md + AGENTS.md (same text inserted by hand), `docs/codebase-summary.md`, `.claude/rules/development-rules.md`, skill `revit-webview-addin` (SKILL.md + `references/architecture-layers.md`, .claude and .agents).

## Checks
| Check | Result |
|---|---|
| `dotnet build HPRebar.slnx -c Debug.R26` | ✅ 0 errors |
| `HPRebar.csproj` Debug.R23 / Debug.R27 | ✅ compile |
| HPRebar.Core.Tests / HPWebHost.Tests / HPBimToolkit.Core.Tests | ✅ 1637 / 56 / 31 |
| Frontend typecheck, lint, vitest | ✅ / ✅ / 151 |
| Live `live-verify-kata-web.mjs` (B03 copy of Dam kata test) | ✅ 16/16, incl. `kata.canvas` = UNKNOWN_COMMAND |
| Live `live-verify-kata-web-rebar.mjs` (reads KataB03.xlsm, no generate) | ✅ 9/9: 2D hover → pick → Delete 571 → 565 → Ctrl+Z 571 |
| Live `live-verify-kata-native.mjs` | ✅ 14/14 (12/14 before the harness pick fix) |
| Live `live-verify-kata-tools.mjs --export` | ✅ 7/7; the host Save dialog was closed by WM_CLOSE, nothing saved |
| Live `live-verify-kata-3d.mjs` | ✅ 15/15, orbit median 1.1 ms CPU |

Screenshots: [phase-06-2d.png](phase-06-2d.png) (dark, the harness Revit's theme), [phase-06-3d.png](phase-06-3d.png).

## Review round
[code-review-2026-10-25-kata-phase-06.md](../../reports/code-review-2026-10-25-kata-phase-06.md), score 7.5/10.
- **Fixed:** H1 and M1–M4, L2, L3, L5.
  - A scene that could not be built now shows its error; a null scene says so instead of loading forever.
  - `kata.ready` replaces the side effect `getState` had.
  - KataOnly is refused outside R26.
  - `palette.test.ts` covers both themes.
  - The dead `KataElevationViewport` is deleted.
  - Window creation has its own failure path.
- **Re-checked after the fixes:**
  - Build R26 ✅; Core 1625 ✅; vitest 157 ✅.
  - Live: web 16/16, rebar 9/9, native 14/14, 3D 15/15, tools 6/6.

## Not done
- Light-theme screenshot: it needs a Revit theme flip (the user's setting), not done without approval. The light palette is now covered by `palette.test.ts`.
- Side-by-side with the WPF window: no longer possible (it is deleted). Parity was checked in phase 3, side by side, and found identical.
- Performance figures beyond the 3D orbit: scene build and first paint were not timed separately.
- Xuất Excel from the web window: CHƯA TEST (it writes the user's active workbook).
- Revit 2023–2025 and 2027: build only, and no Kata Export button there (by decision).
