# Code review — Kata Export cut-over (phase 6 of 261025-0621)

Reviewer: code-reviewer agent (static, read-only). Score 7.5/10. Every finding was then fixed or deliberately left, as recorded below.

| # | Sev | Finding | Resolution |
|---|---|---|---|
| H1 | High | A scene-build exception was only logged: the page kept waiting forever, or kept the previous plan's bars | ✅ The bridge publishes `KataSceneFrame(rev, null, Error)`, and the page shows the error in place of the drawing (`KataWebBridge.Scene.cs`, `useKata.ts`, `KataApp.tsx`); vitest "a scene the host could not build shows why" |
| M1 | Medium | `scene: null` (sheet failed → `Elevation = null`) looked like loading | ✅ The page tracks `sceneArrived`: a null frame shows a "no elevation" message (wording below the table). The mock sends null frames and honours latency; vitest added |
| M2 | Medium | `kata.getState` re-sent the scene, so harness polling dropped the page's bar selection | ✅ `getState` is pure again; new command `kata.ready` (sent once by `useKata` when the page listens) triggers the scene |
| M3 | Medium | `KataOnly` on a non-2026 configuration built the full rebar panel | ✅ `HPRebar.csproj` target `RefuseKataOnlyOutsideRevit2026` (verified: R25 + KataOnly errors) |
| M4 | Medium | Light theme of the drawing was untested once the gallery was deleted | ✅ `palette.test.ts`: canvas tokens pinned to ThemeDark/ThemeLight.xaml, every pen/text ≥ 3:1 on its fill in both themes |
| L1 | Low | Dead view-model members (`HeaderLine`, `HasWarnings`, …) | Left (not touched by this diff; Boy-Scout later) |
| L2 | Low | `KataElevationViewport` (Core) without a caller; the TS copy is the live one | ✅ Class and its tests deleted, comments and `codebase-summary.md` updated |
| L3 | Low | A window that fails to construct left the ExternalEvent undisposed and showed a misleading message | ✅ `KataExportCommand.OpenWindow`: own try/catch, disposes the handler, message names WebView2 |
| L4 | Low | Port-provenance comments name deleted WPF types | Left: they say where the rule came from; git history resolves them |
| L5 | Low | Audit rows pointed at deleted files | ✅ AUD-006/029/054 marked closed by deletion (AUD-054 re-pointed to `palette.ts`); ADR-0010 header notes ADR-0011 |
| L6 | Info | The Kata Export view model and handler still compile on R23–R25/R27 with no entry point | Left (harmless; Kata Rebar / Kata Settings verified reachable) |

Message shown for M1: "Chưa có mặt đứng cho dải dầm này — xem dòng trạng thái và cảnh báo."

Re-check after the fixes:
- Build R26 ✅ 0 errors.
- Core 1625 (−12 viewport tests) ✅.
- vitest 157 ✅, typecheck ✅, lint ✅.
- Live (new deploy, B03 copy): web 16/16, rebar 9/9, native 14/14, 3D 15/15, tools 6/6.
