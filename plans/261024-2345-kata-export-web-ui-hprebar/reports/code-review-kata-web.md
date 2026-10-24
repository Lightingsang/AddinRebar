# Code review — Kata Export web UI (HPRebar R26) + HPWebHost split

Date 2026-10-25. Scope: uncommitted diff + untracked (HPWebHost.Core, HPWebHost, HPWebHost.Tests, KataExport/View/Web/*, canvas extraction, Frontend kata page + bridge generics + embed-frontend.targets + build-frontend-once.mjs, toolkit namespace moves, csproj/slnx, ADR-0010 docs). Ignored per brief: hooks logs, hp-mcp-autocad skill, agent-memory, plans/261024-0811-*.
Rules: CODE_REVIEW_CHECKLIST.md, host-appendix/revit.md (+ Revit standard R*/D*/P*).

## Gates run (this review)

| Check | Result |
|---|---|
| `dotnet build HPRebar.slnx -c Debug.R26 -p:DeployAddin=false` | Pass, 0 C# warnings; ILRepack Polyfill noise only (pre-existing); EmbedFrontend 6 files into HPRebar + 6 into HPBimToolkit |
| `dotnet test HPWebHost.Tests` | 63/63 |
| `dotnet test HPBimToolkit.Core.Tests` | 31/31 |
| Frontend typecheck / lint / vitest | pass / pass / 41/41 |
| Live (not re-run, per brief) | toolkit 22/13/6, Kata web 19/19 + rebar 9/9 |

## Verified OK (no finding)

- UI-thread affinity: WebViewHost marshals WebMessageReceived via Dispatcher.BeginInvoke ([WebViewHost.cs:138](../../../HPRebar/HPWebHost/Hosting/WebViewHost.cs#L138)); dispatcher invokes handlers synchronously before first await, continuation resumes on WPF sync context, so PostJson runs on UI thread. Kata handlers all synchronous (Task.FromResult); sync throws inside handler lambdas land in DispatchAsync try → BAD_REQUEST/BUSY envelopes.
- Canvas extraction (Input.cs / Edit.cs): wheel (no-elevation → not handled), middle double-click, pan (panel vs run, `_userFramed`), left-click (panel close button / flag / bar / column via SetCurrentValue), Esc/Delete/Ctrl+Z all map 1:1 to old code; `Keyboard.Modifiers` passed through. `Rendered` has no subscriber in the WPF window → no R23–R25/R27 behaviour change. Hidden canvas bindings identical to KataExportView.xaml:467-478.
- SVG injection: text + font family through SecurityElement.Escape, numbers invariant, colours hex; page CSP `script-src 'self'` blocks inline handlers anyway; System.Text.Json Web defaults reject NaN/Infinity so gesture numbers are finite.
- Disposal: Closed → bridge unsubscribes VM/canvas/theme, transport unsubscribes, host disposes; late Background callbacks hit `CanPublish == false` (host `_webView` null) → no throw. Close from a page `kata.run close` is outside the COM callback (BeginInvoke) → safe.
- Resize: finite check + clamp 50..8000; Run: CanExecute checked, ExecuteAsync task observed (OnlyOnFaulted continuation) → no unobserved exception, no async-void rethrow.
- kataContracts.ts vs KataWebContracts.cs + KataPreviewColumn: every field matches camelCase (state, header, columns, can, update, run, viewport, canvas, frame, results).
- R13 modeless contract kept (static `_window` now `Window`, Activate on second click, CloseRequested, handler disposed on Closed). R6: `#if REVIT2026` + `// Multi-version:` at both call sites.

## High

**H1 — build-frontend-once.mjs: failed npm step leaves the lock; next build hangs 10 min then fails** — [build-frontend-once.mjs:37](../../../HPRebar/HPBimToolkit.Frontend/tools/build-frontend-once.mjs#L37), [:12-13](../../../HPRebar/HPBimToolkit.Frontend/tools/build-frontend-once.mjs#L12-L13), [:61](../../../HPRebar/HPBimToolkit.Frontend/tools/build-frontend-once.mjs#L61)
- `npm()` calls `process.exit()` inside the `try`; Node exits immediately, `finally { rmSync(lock) }` never runs (verified in scratch: `try{process.exit(3)}finally{…}` prints nothing).
- Lock then looks fresh: STALE 15 min > TIMEOUT 10 min → next `dotnet build` (even after the TS error is fixed) waits 10 min and throws. Same after Ctrl+C / MSBuild cancel.
- Scenario: one TypeScript error in src/kata → build fails (fine) → fix → rebuild blocked 10 min → fails "lock still held".
- Fix: set `process.exitCode` + throw instead of `process.exit`, release in finally; STALE < TIMEOUT (or write `process.pid` into the lock and treat a dead pid as stale).

**H2 — lock lives inside node_modules, which `npm ci` deletes** — [build-frontend-once.mjs:11](../../../HPRebar/HPBimToolkit.Frontend/tools/build-frontend-once.mjs#L11), [:43-51](../../../HPRebar/HPBimToolkit.Frontend/tools/build-frontend-once.mjs#L43-L51)
- `npm ci` removes an existing node_modules first → lock dir gone. Waiter: mkdir ENOENT → `mtime(lock)=0` → "stale" → loops; once npm recreates node_modules, waiter's mkdir succeeds while the first `npm ci` is still running → `isInstalled()` false (`.package-lock.json` written last) → second `npm ci` deletes node_modules under the first. First caller's finally then deletes the second's lock.
- Scenario: fresh clone or package-lock change + `dotnet build HPRebar.slnx -c Debug.R26` (MSBuild builds HPBimToolkit and HPRebar in parallel, both import embed-frontend.targets) → EPERM/ENOENT/half-installed builds, flaky.
- Fix: lock outside node_modules (`<FrontendDir>/.build-lock`, gitignored, or `os.tmpdir()` keyed by root hash).

## Medium

**M1 — SVG arc fallback flattens from (0,0)** — [DrawingSvgWriter.cs:100](../../../HPRebar/HPWebHost/Rendering/DrawingSvgWriter.cs#L100)
- Non-similarity geometry transform → `new PathFigure { Segments = { arc } }` has StartPoint (0,0), not the arc's real start (previous point). Flattened polyline is a different curve.
- Latent today (Kata painters use group transforms → `<g transform>`, arcs untouched), but HPWebHost is a shared library and no test covers it.
- Fix: pass the current point into `Segment`/`Arc` (track last point per figure) and set `StartPoint` = untransformed current point; add a test with a rotated EllipseGeometry/ArcSegment.

**M2 — matrix coefficients rounded to 2 dp shift squeezed text** — [DrawingSvgWriter.cs:127](../../../HPRebar/HPWebHost/Rendering/DrawingSvgWriter.cs#L127), [:168](../../../HPRebar/HPWebHost/Rendering/DrawingSvgWriter.cs#L168)
- `Number()` rounds M11/M12/M21/M22 like coordinates. `KataDrawPrimitives.AtWidth` pushes `ScaleTransform(sx,1,left,top)` with arbitrary sx (e.g. 0.857 → 0.86): text start moves by ≈ (Δsx)·left, up to 0.005·x → ~4–7 px at x 800–1500, label overlaps column lines / flags in the web view only.
- Fix: write matrix scale/rotation terms with ≥ 6 significant digits (`"0.######"`), keep 2 dp for offsets/coords; add a test with sx = 6/7 at x = 1000.

**M3 — KataWebWarmUp "best effort" is not: JIT-time bind failure escapes** — [Application.cs:33](../../../HPRebar/HPRebar/Application.cs#L33), [KataWebWarmUp.cs:19-33](../../../HPRebar/HPRebar/KataExport/View/Web/KataWebWarmUp.cs#L19-L33)
- `typeof(CoreWebView2Environment)` is resolved when `Run` is JIT-compiled, i.e. at the call in `OnStartup`, before Run's try and outside OnStartup's own try. A missing/unbindable WebView2 (damaged install, renamed Revit folder) → FileNotFoundException from OnStartup → whole HPRebar add-in fails (no ribbon), not just Kata. Same lesson as the ETABS `NoInlining Wire()`.
- Fix: move the call inside OnStartup's existing try, or `try { KataWebWarmUp.Run(); } catch (Exception ex) { Log.Warning(...) }` at the call site.

**M4 — WebViewHost leaks the browser control when closed during start-up** — [WebViewHost.cs:120-129](../../../HPRebar/HPWebHost/Hosting/WebViewHost.cs#L120-L129)
- Dispose while `CreateAsync` is pending: no `_isDisposed` check after it → `new WebView2()` added + assigned after Dispose nulled `_webView`; check after `EnsureCoreWebView2Async` returns without disposing it (or Ensure never completes on a window that is gone).
- Mattered little for the life-long dockable pane; the Kata window is opened/closed per session. Scenario: open Kata, close within ~1 s → orphan WebView2/controller (profile folder held) until GC.
- Fix: check `_isDisposed` after CreateAsync; on the post-Ensure check `webView.Dispose()` before return.

**M5 — hand-written kataContracts.ts has no drift guard** — [kataContracts.ts:1-2](../../../HPRebar/HPBimToolkit.Frontend/src/kata/kataContracts.ts#L1-L2), [KataWebContracts.cs](../../../HPRebar/HPRebar/KataExport/View/Web/KataWebContracts.cs)
- Matches today (verified field by field). Toolkit contracts are generated + pinned by TypeScriptContractsTests; Kata's are not, and the C# side lives in the Revit add-in (no xUnit project sees it). A renamed record member silently becomes `undefined` in the page (only caught live).
- Fix: text-level test (like ThemeTokenCoverageTests) that parses record parameters of KataWebContracts.cs + KataPreviewColumn.cs and the TS interfaces, or move the DTOs to a Revit-free assembly and reuse TypeScriptContractWriter.

**M6 — RV7: rebar harness writes into whatever model the window is bound to** — [live-verify-kata-web-rebar.mjs:80-84](../../../HPRebar/HPBimToolkit/tools/harness/live-verify-kata-web-rebar.mjs#L80-L84)
- `--generate` runs `generateRebar`; nothing checks the document path. Rule RV7: every live script refuses unless `doc.PathName` is under the run folder. Wrong port / user's own Revit with debug port → bars written into a real project.
- Fix: expose the document path in `kata.getState` (or read it from the harness's own Revit launcher) and refuse unless it is under the copy folder.

## Low

| # | Where | Finding | Fix |
|---|---|---|---|
| L1 | [ElevationView.tsx:46](../../../HPRebar/HPBimToolkit.Frontend/src/kata/ElevationView.tsx#L46), [:119](../../../HPRebar/HPBimToolkit.Frontend/src/kata/ElevationView.tsx#L119) | `getBoundingClientRect` = border box, SVG sits at padding box (absolute left-0 top-0, 1 px border) → every click/zoom/pan point off by 1 px | subtract `clientLeft/clientTop` |
| L2 | [ElevationView.tsx:43](../../../HPRebar/HPBimToolkit.Frontend/src/kata/ElevationView.tsx#L43), [KataWebBridge.cs:239](../../../HPRebar/HPRebar/KataExport/View/Web/KataWebBridge.cs#L239) | pan preview offset cleared only by a new revision; a pan that paints the same SVG (section panel showing a section failure, `PanFrom` early return) leaves the picture shifted vs host coordinates | reset offset when the `kata.canvas` pan answer arrives, or `Resend()` after each gesture |
| L3 | [KataWebBridge.cs:107](../../../HPRebar/HPRebar/KataExport/View/Web/KataWebBridge.cs#L107) | `can.*` refreshed only on VM PropertyChanged; `NotifyCanExecuteChanged` alone (e.g. ResetRemovals) does not push state — covered today by accompanying property changes, fragile | subscribe the five commands' `CanExecuteChanged` → ScheduleState |
| L4 | [KataWebBridge.cs:162](../../../HPRebar/HPRebar/KataExport/View/Web/KataWebBridge.cs#L162) | faulted async command only logged; page status unchanged (WPF path surfaces it via dispatcher) | in continuation (UI scheduler) set a visible status or publish an error event |
| L5 | KataWebBridge.cs 113-118, 219, 231, 239; KataWebWindow.cs 70; Canvas Input.cs/Edit.cs new lines | FM2: unbraced single-line `if`/`else` on new lines | brace (Boy Scout on touched lines) |
| L6 | [KataWebWindow.cs:49-58](../../../HPRebar/HPRebar/KataExport/View/Web/KataWebWindow.cs#L49-L58) | D1: window constructor composes asset server, host, dispatcher, transport, bridge; composition root should be KataExportCommand (pass a factory/bundle in) | move composition to `KataExportCommand.CreateWindow` |
| L7 | [KataWebWindow.cs:88-113](../../../HPRebar/HPRebar/KataExport/View/Web/KataWebWindow.cs#L88-L113) | canvas bindings duplicated from KataExportView.xaml (K1) — identical now, drift risk | comment cross-link both places, or a shared helper used by both |
| L8 | embed-frontend.targets (both imports) | both DLLs embed the whole dist: HPRebar.dll serves the toolkit dashboard at `https://kata.hprebar.local/` (`/` → index.html, trusted origin), toolkit carries kata.html | per-project page filter property (`FrontendPages`) |
| L9 | build-frontend-once.mjs 48; targets Inputs | two waiters can both judge a stale lock and both acquire; MSBuild Inputs never sees a deleted source file (script's dir-mtime does) | atomic rename-based stale takeover; accept |
| L10 | DrawingSvgWriter | clip ids `c1…` are document-global; only `StartLineCap` mapped (End/DashCap ignored); `Win32FamilyNames` dictionary order arbitrary; `CreateFromGeometry` twice per geometry; tests lack escaping, arcs, group opacity, transformed text | prefix ids, add tests |
| L11 | [KataElevationCanvas.Edit.cs:101](../../../HPRebar/HPRebar/KataExport/View/Controls/KataElevationCanvas.Edit.cs#L101) | Esc with nothing to close closes the WPF window ("else Esc still closes the window") but does nothing in the web window | page: on Esc with `handled:false` → `run('close')` if parity wanted |
| L12 | HPWebHost.Core.csproj comment; DEPENDENCY_RULES D8 | comment names `HPWebHost.Core.Tests` (project is HPWebHost.Tests); D8 silent on HPWebHost and on HPRebar importing `HPBimToolkit.Frontend/embed-frontend.targets` | fix comment; add one D8 clause |
| L13 | KataWebBridge.ScheduleDrawing | every paint = full SVG + O(n) compare + JSON escape + innerHTML re-parse; fine live, MB-scale on long runs with tags under wheel bursts | measure on the longest run; consider gzip-free diff later only if slow |

## Positive

- Clean separation: HPWebHost.Core host-free + tested, HPWebHost WPF/WebView2 only, generalized by options record; toolkit diff is namespace-only.
- Gesture extraction keeps WPF behaviour exact; page only classifies gestures, the WPF canvas stays the single source of hit-testing/painting (no rule duplicated, K1).
- Coalescing (state/drawing once per dispatcher pass, wheel notches summed while a zoom is in flight), revision dedupe, CSP + origin checks kept.

## Recommended order

1. H1 + H2 (build script) — small, deterministic breakage on clean / failed builds.
2. M3 (warm-up call site) — one-line guard.
3. M2, M1 + tests in DrawingSvgWriterTests.
4. M4, M6, M5.
5. Lows opportunistically.

## Unresolved questions

None needing the user.
