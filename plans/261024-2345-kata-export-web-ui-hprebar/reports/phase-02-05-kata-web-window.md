# Phases 2–5 — Kata Export web window in HPRebar R26 (2026-10-25)

## Design change vs plan (phase 3)
Plan: painters backend-neutral (interface + WPF backend + SVG backend). Done instead: web window keeps the WPF `KataElevationCanvas` (same bindings as `KataExportView.xaml`) under WebView2, `Opacity 0`, `IsHitTestVisible false`; canvas raises `Rendered` after each `OnRender`; bridge reads `VisualTreeHelper.GetDrawing(canvas)` → `HPWebHost.Rendering.DrawingSvgWriter` → event `kata.drawing`. Page gestures → `ZoomAt / PanFrom / ClickAt / ZoomExtentsAt / KeyPressed`, extracted from the canvas's mouse/key handlers (WPF handlers now call them, behaviour unchanged). Why: same "one painter" goal, zero change to ~3k lines of live-verified painters, hit-testing reuses WPF code. ADR-0010 decision 3 updated.

## Code
| Area | Files |
|---|---|
| Contracts (hand-written mirror in TS) | `KataExport/View/Web/KataWebContracts.cs` ↔ `HPBimToolkit.Frontend/src/kata/kataContracts.ts` — commands `kata.getState/update/run/viewport/canvas`, events `kata.state/drawing` |
| Bridge (code-behind of the web view) | `KataWebBridge.cs` — VM → `KataViewState`; `Run` starts VM commands fire-and-forget (pick/generate can take minutes, outcome via state); state + drawing coalesced once per dispatcher pass at Background priority; SVG sent only when changed |
| Window | `KataWebWindow.cs` — `kata.hprebar.local/kata.html`, profile `%LocalAppData%\HPRebar\WebView2`, theme via `MaterialThemeBridge`, hides during pick like WPF |
| Canvas | `KataElevationCanvas.cs` (`Rendered`), `.Input.cs` (extract), `.Edit.cs` (`KeyPressed`, `HasBarSelection`) |
| Command | `KataExportCommand.CreateWindow`: R26 → web unless `HPREBAR_KATA_UI=wpf` |
| SVG writer | `HPWebHost/Rendering/DrawingSvgWriter.cs` + 7 tests (line, transform/clip nesting, dashes × thickness, fill opacity, ellipse, glyph run text with WPF advance, empty) |
| Frontend | `kata.html`, `src/kata/{main,KataApp,KataParameters,KataColumnsTable,ElevationView,useKata,elevationGestures,kataMockBridge}`; bridge client generic over command/event maps; multi-page vite; `embed-frontend.targets` + `tools/build-frontend-once.mjs` (dir lock, freshness re-check) imported by toolkit and HPRebar R26 |

## Results
| Check | Result |
|---|---|
| `dotnet build HPRebar.slnx -c Debug.R26` | ✅ (one npm build, second project "up to date"; 6 files embedded in each) |
| `HPWebHost.Tests` | ✅ 63/63 |
| `HPBimToolkit.Core.Tests` | ✅ 31/31 |
| `HPRebar.Core.Tests` | ✅ 1627/1627 |
| frontend typecheck / lint / vitest / build | ✅ / ✅ / 41/41 / ✅ |
| Toolkit dashboard live after frontend change | ✅ 22/22 |
| `live-verify-kata-web.mjs 9334 6` (copy of Dam kata, web = default) | ✅ 19/19 — page on virtual host, state from VM (6 beams, 11 columns), SVG 27.5k chars / 153 paths / 47 texts at area size, select from table + from canvas click (WPF hit test), zoom 1 ms, pan, Toàn dải, Đảo chiều reversed kinds, layer flags, BAD_REQUEST paths, Esc unhandled |
| `live-verify-kata-web-rebar.mjs 9334 --generate` | ✅ 9/9 — Đọc thép Excel (user's open KataDam.xlsm, read only) 458 bars 2318.8 kg, Kata elevation with bars 141k chars, tags toggle, bar group picked by click, Delete → 452 bars, Ctrl+Z → 458, Tạo thép Revit into the model copy OK |
| Screenshots | `scratchpad/kata-web-1..3.png` — dark theme follows Revit; selection line clipped by flex shrink in shot 1–2 → fixed (`shrink-0`, taller drawing row), shot 3 OK |

## Not verified
- **Xuất Excel from the web window: CHƯA TEST** — writes the active workbook = the user's KataDam.xlsm; same `ExportCommand` as the WPF button.
- Chọn lại dầm (interactive pick), section flag click / section panel pan, light theme, HiDPI, R23–R25/R27 compile (user rule: R26 gate only; canvas edits are version-neutral; R25 built in phase 0).

## Phase 6 — review (`code-review-kata-web.md`: 0 Critical, 2 High, 6 Medium, 13 Low) → fixes
| Finding | Fix |
|---|---|
| H1 lock left after failed build (`process.exit` skips `finally`) | npm failure thrown, `process.exitCode`; lock stores owner pid, dead owner = taken over at once (checked: 0.6 s); stale 15 min < wait 20 min |
| H2 lock inside `node_modules` (deleted by `npm ci`) | lock `HPBimToolkit.Frontend/.build-lock/` (gitignored) |
| M1 arc fallback flattened from (0,0) | non-similarity transform with arcs → whole path flattened (untransformed clone), test |
| M2 matrix rounded to 2 decimals | coefficients 6 decimals, test |
| M3 warm-up JIT outside try | call site wrapped in `Application.OnStartup` |
| M4 WebView2 not disposed when closed during start | `_isDisposed` checked after `CreateAsync`, control disposed after `EnsureCoreWebView2Async` |
| M5 no drift guard for hand-written TS | `kataContracts.test.ts` reads the C# records/constants (`?raw`) — 9 tests |
| M6 `--generate` without path guard | requires `--run-folder`; open document path (toolkit `getDocumentInfo`) must be inside it |
| Low: click 1 px off, pan preview not reset on identical SVG, Esc not closing | `clientLeft/Top`; offset reset 500 ms after the pan answer; unhandled Esc → `close` (WPF `KeyBinding Escape → CloseCommand`) |
| Low kept | `can` flags refresh on VM property change only; failed async command only logged; composition in window ctor; bindings duplicated code/XAML; both DLLs embed whole build; global clip ids; `StartLineCap` only; full SVG per paint (perf not measured on long runs) |

After fixes: HPWebHost.Tests 68/68, Toolkit Core 31/31, vitest 50/50, typecheck + lint ✅, solution build R26 ✅, live Kata web 19/19 + rebar 9/9 (generate guarded) on a fresh copy.
