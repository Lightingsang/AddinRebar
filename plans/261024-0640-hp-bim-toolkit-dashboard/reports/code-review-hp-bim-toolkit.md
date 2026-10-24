# Code review — HP BIM Toolkit (Dashboard, Revit 2026, WebView2 + React)

Date 2026-10-24 · reviewer: code-reviewer · scope: all new untracked files · read-only (no code changed)

## Scope
- C# add-in `HPRebar/HPBimToolkit/**` (~1.2k LOC), Core `HPRebar/HPBimToolkit.Core/**` (~520), tests `HPBimToolkit.Core.Tests/**` (~500), frontend `HPBimToolkit.Frontend/src/**` + `vite.config.ts` + `package.json` (~1.6k), csproj frontend targets, `HPRebar.slnx` BimToolkit folder, ADR-0008. Harness scripts: light pass.
- Rules: HP_CLEAN_CODE_CORE, REVITADDINAI R1–R13 (P1 replaced + P5 exempt per ADR-0008), CODE_REVIEW_CHECKLIST.
- Verified by reading the built DLL: embedded names are `wwwroot/index.html`, `wwwroot/assets/index-*.js|css` (forward slashes OK).
- Not re-run: build/tests (caller verified 65 xUnit + 25 vitest + live spike). Findings below are by code reading; none reproduced live = GIẢ ĐỊNH CHƯA XÁC MINH where marked.

## Overall
Solid base: one API road (queue `Execute` + Revit events), R9 honoured (Raise result checked, pending failed on Dispose, `RunContinuationsAsynchronously`), origin check on every message, CSP on HTML, traversal-safe asset lookup (lookup is in-assembly anyway), wire shape pinned by tests and matching `protocol.ts` field-for-field. Defects cluster around **timeout/BUSY lifecycle** (host and page disagree on when a statistics run is over) and **stale state after doc/view/pane changes**. No Critical.

## High

**H1** [useDashboard.ts:26-29](../../../HPRebar/HPBimToolkit.Frontend/src/modules/dashboard/useDashboard.ts#L26-L29) + [DashboardModule.cs:34-37](../../../HPRebar/HPBimToolkit/Modules/Dashboard/DashboardModule.cs#L34-L37) + [Button.tsx:24](../../../HPRebar/HPBimToolkit.Frontend/src/ui/Button.tsx#L24) — K1/T4 — page locks in "loading": `statisticsLoading` is dispatched, BUSY returns without any dispatch, so status stays `loading`; `isCounting` disables both scope radios and Refresh (`busy` ⇒ `disabled`). Deterministic path: modal dialog open in Revit → getStatistics TIMEOUT at 30 s (run still in `_pending`, `_statisticsRun.IsCompleted == false` until `Execute`) → user clicks "Thử lại" while dialog open → BUSY → after the dialog closes the queued run cancels/finishes but nothing on the page settles the state. Only a document switch recovers. — Fix (both sides): page: on BUSY dispatch a `statisticsBusy` that restores the previous status and retry with backoff (or re-request when the run is known done); host: treat a run whose request already timed out as replaceable, or let a second caller **join** the in-flight task (same scope) instead of BUSY.

## Medium

**M1** [CommandDispatcher.cs:54-58](../../../HPRebar/HPBimToolkit.Core/Protocol/CommandDispatcher.cs#L54-L58) — R9/K5 — two timers with the same due time: `CancelAfter(_timeout)` and `WaitAsync(_timeout, …)`. If `WaitAsync` fires first, `DispatchAsync` returns and `using` disposes the CTS, which also kills the pending `CancelAfter` timer → token never cancelled → the queued request **still runs later** (`dashboard.openTool` posts e.g. Purge Unused ~minutes after the page saw TIMEOUT; `showElements` replaces the selection; statistics reset the tally for a result nobody reads). Order is not guaranteed; `DispatchAsync_HandlerSlowerThanTimeout_ReturnsTimeoutAndCancelsToken` is therefore flaky by design. — Fix: single timeout source: `await handler(...).WaitAsync(timeoutSource.Token)` and map OCE with `!callerToken.IsCancellationRequested` → TIMEOUT; or call `timeoutSource.Cancel()` in the catch before dispose.

**M2** [ModelStatisticsReader.cs:22-28](../../../HPRebar/HPBimToolkit/Services/ModelStatisticsReader.cs#L22-L28) + [ToolkitApplication.cs:26-27](../../../HPRebar/HPBimToolkit/Host/ToolkitApplication.cs#L26-L27) — R1/K1 — a count longer than 30 s can never succeed: the run blocks Revit's UI thread, the dispatcher's timeout continuation is queued behind it and wins → TIMEOUT though the result exists; page also gives up at 35 s; every refresh repeats the full freeze; `changes.Reset()` ([DashboardModule.cs:42](../../../HPRebar/HPBimToolkit/Modules/Dashboard/DashboardModule.cs#L42)) runs for the discarded result. The doc comment claims 30 s "covers a statistics run on a large model". — Fix: pass the token into the reader and `ThrowIfCancellationRequested()` every N elements (stops the freeze early), give statistics its own longer budget (dispatcher per-command timeout) and keep page timeout > host timeout for that command; reset the tally only when the response is delivered.

**M3** [useDashboard.ts:47-58](../../../HPRebar/HPBimToolkit.Frontend/src/modules/dashboard/useDashboard.ts#L47-L58) + [dashboardState.ts:59-62](../../../HPRebar/HPBimToolkit.Frontend/src/modules/dashboard/dashboardState.ts#L59-L62) + [ElementNavigator.cs:42-48](../../../HPRebar/HPBimToolkit/Services/ElementNavigator.cs#L42-L48) — R4 — numbers can belong to another document/view: (a) doc switch while a count runs → BUSY → the old run's result lands as `ready` under the new document header; `documentLoaded` keeps the previous document's statistics; (b) scope `activeView`: `documentKey` ignores the view, so a view switch neither refreshes nor marks stale, and "select category" then collects in the *new* view (count shown ≠ elements selected); (c) `ShowElementsRequest` carries no document/view identity. — Fix: return document key + view id in `ModelStatistics`; page drops responses whose key ≠ current; include `activeView.id` in the key when scope is `activeView` (or set `stale`); clear statistics on key change.

**M4** [WebViewHost.cs:46-55](../../../HPRebar/HPBimToolkit/Host/WebViewHost.cs#L46-L55), [WebViewHost.cs:120](../../../HPRebar/HPBimToolkit/Host/WebViewHost.cs#L120), [WebMessageTransport.cs:33-41](../../../HPRebar/HPBimToolkit/Bridge/WebMessageTransport.cs#L33-L41), [RevitEventPublisher.cs:44-47](../../../HPRebar/HPBimToolkit/Bridge/RevitEventPublisher.cs#L44-L47), [RevitEventPublisher.cs:87-88](../../../HPRebar/HPBimToolkit/Bridge/RevitEventPublisher.cs#L87-L88) — error boundaries — `PostWebMessageAsJson` throws once the browser process has exited (controller closed, documented WebView2 behaviour); `Publish` is called unguarded from a Revit event handler (`OnThemeChanged`) and from `DispatcherTimer.Tick` → exception on Revit's UI thread (Dispatcher unhandled exception). `ProcessFailed` only logs, `IsReady` stays true, pane stays blank until Revit restarts. GIẢ ĐỊNH CHƯA XÁC MINH (not driven live). — Fix: try/catch in `PostJson` (log + mark not ready); on `RenderProcessExited` `core.Reload()`, on `BrowserProcessExited` dispose and allow re-create on next `Loaded`.

**M5** [useDashboard.ts:108-116](../../../HPRebar/HPBimToolkit.Frontend/src/modules/dashboard/useDashboard.ts#L108-L116) + [DashboardContracts.cs:57-62](../../../HPRebar/HPBimToolkit.Core/Contracts/DashboardContracts.cs#L57-L62) + [ElementNavigator.cs:20](../../../HPRebar/HPBimToolkit/Services/ElementNavigator.cs#L20) — data loss (user state) — "Phóng tới" sends `selection.ids` (capped at 200) back with `mode: zoom`; the host `SetElementIds` first, so a 5 000-element selection is silently replaced by its first 200. — Fix: a host-side "zoom to current selection" (no ids → use `Selection.GetElementIds()`, never call `SetElementIds`), or skip `SetElementIds` when the ids equal the current selection.

**M6** [RevitEventPublisher.cs:90-102](../../../HPRebar/HPBimToolkit/Bridge/RevitEventPublisher.cs#L90-L102) + [WebMessageTransport.cs:30-38](../../../HPRebar/HPBimToolkit/Bridge/WebMessageTransport.cs#L30-L38) + [ModelChangeTally.cs:15](../../../HPRebar/HPBimToolkit.Core/Statistics/ModelChangeTally.cs#L15) — K4 — while the pane is hidden `model.stale` and `selection.changed` are dropped; on show only `document.changed` is published and the key is unchanged, so edits/selection made while hidden never reach the page (stale badge and selection count wrong). `HasChanges` exists but is unused. — Fix: on `DockableFrameShown` also publish `model.stale` when `_changes.HasChanges` and the current selection count (`_selectionCount`).

**M7** [HPBimToolkit.csproj:51-54](../../../HPRebar/HPBimToolkit/HPBimToolkit.csproj#L51-L54) — build correctness — `InstallFrontend` runs only when `node_modules` is missing: after a `package-lock.json` change the build keeps old packages and `npm run build` uses them. — Fix: `Inputs="$(FrontendDir)package-lock.json" Outputs="$(FrontendDir)node_modules\.package-lock.json"` (npm writes that file) instead of the `!Exists` condition.

## Low

| # | path:line | rule | fact | fix |
|---|---|---|---|---|
| L1 | [ToolkitApplication.cs:46-52](../../../HPRebar/HPBimToolkit/Host/ToolkitApplication.cs#L46-L52) | error boundaries | start-up failure after `_publisher.Subscribe()` (e.g. `RegisterDockablePane`/`CreateRibbon`) rethrows with Revit events still subscribed and the queue alive; logger disposed | run the same teardown as `OnShutdown` before rethrow |
| L2 | [CommandDispatcher.cs:74](../../../HPRebar/HPBimToolkit.Core/Protocol/CommandDispatcher.cs#L74) | API contract | any `System.ArgumentException` from host code (a bug) becomes BAD_REQUEST, i.e. "the page's fault" | handlers already throw `BridgeException(BadRequest)`; drop the `ArgumentException` arm or restrict it to Core validation |
| L3 | [AppModule.cs:22](../../../HPRebar/HPBimToolkit/Modules/AppModule.cs#L22) | R1 | `UIThemeManager.CurrentTheme` read in a WebView message handler, outside an API context | cache the theme at start-up and in `ThemeChanged` |
| L4 | [RevitEventPublisher.cs:104-125](../../../HPRebar/HPBimToolkit/Bridge/RevitEventPublisher.cs#L104-L125) | async void / perf | `PublishDocument` is an `async void` helper (safe: full try/catch) and queues an ExternalEvent on every `ViewActivated` even while paused or before the browser exists | `async Task` + `_ = …`; skip when `IsPaused \|\| !IsReady` (re-publish on show already exists) |
| L5 | [WebViewHost.cs:114-123](../../../HPRebar/HPBimToolkit/Host/WebViewHost.cs#L114-L123) | defence in depth | no `NavigationStarting` / `NewWindowRequested` guard; origin check already blocks bridge use, but a future link would navigate the pane away or open a popup | cancel navigation outside the trusted origin; open external links in the default browser |
| L6 | [WebViewHost.cs:94-103](../../../HPRebar/HPBimToolkit/Host/WebViewHost.cs#L94-L103), [ToolkitPaths.cs:14](../../../HPRebar/HPBimToolkit/Infrastructure/ToolkitPaths.cs#L14), [start-revit-with-toolkit.ps1:57](../../../HPRebar/HPBimToolkit/tools/harness/start-revit-with-toolkit.ps1#L57) | harness | one user-data folder for every Revit: a second Revit started with `--remote-debugging-port` while the user's Revit already runs the toolkit browser fails (`ERROR_INVALID_STATE`, different options) or joins the existing browser without the port (WebView2 docs; GIẢ ĐỊNH CHƯA XÁC MINH here) | when the debug port is set, use `WebView2-debug-<port>` as profile folder |
| L7 | [CommandDispatcher.cs:72-73](../../../HPRebar/HPBimToolkit.Core/Protocol/CommandDispatcher.cs#L72-L73), [ScopeCollector.cs:28-29](../../../HPRebar/HPBimToolkit/Services/ScopeCollector.cs#L28-L29) | N7 | English host messages shown verbatim in Vietnamese toasts | page maps `code` → Vietnamese text, host message only as detail |
| L8 | [HPBimToolkit.csproj:67-68](../../../HPRebar/HPBimToolkit/HPBimToolkit.csproj#L67-L68) | MSBuild | `AssignCulture` runs on every `EmbeddedResource`; an asset named `x.de.js`/`x.vi.css` would become a satellite resource (current Vite names are safe) | add `WithCulture="false"` |
| L9 | [App.tsx:18-24](../../../HPRebar/HPBimToolkit.Frontend/src/App.tsx#L18-L24) | resilience | a failed `app.hello` (e.g. Revit modal at first show) is never retried → tool buttons hidden and footer "…" for the session | retry hello on next `document.changed` / refresh |
| L10 | [ElementNavigator.cs:42-47](../../../HPRebar/HPBimToolkit/Services/ElementNavigator.cs#L42-L47) | input bounds | category path is unbounded (`MaxRequestedIds` applies only to explicit ids); zoom on 100k+ elements is heavy | cap or confirm above a threshold |
| L11 | [plan.md](../plan.md) | docs | phase 6 links `reports/phase-06-live-verify.md`, which does not exist yet | create it with the live run |
| L12 | `build/Modules/CreateBundleModule.cs:30` | packaging (info) | `dotnet run -- pack` bundles only HPRebar + McpBridge; the toolkit is not packaged; Core.Tests now run once per `Release.R*` (10×) in `TestProjectModule`; whole-slnx `Debug.R26` now needs Node ≥ 24 (accepted in ADR-0008, no friendly check — `npm` missing = Exec error) | decide packaging in phase 6; optional `Condition` on a `node --version` probe with a clear error |

## Edge cases found by scouting
- Modal Revit dialog + page request ⇒ TIMEOUT, then BUSY ⇒ H1 lock.
- Timer race in the dispatcher ⇒ late execution of timed-out commands (M1).
- Pane hidden/shown, doc switch during count, active-view scope + view switch ⇒ stale numbers (M3, M6).
- Browser process crash ⇒ unguarded `PostJson` from event/timer (M4).
- Selection > 200 ⇒ zoom truncates the user's selection (M5).
- Traversal (`..`, `%2e%2e`, `%252e`, backslash, query) in asset URLs: all end in 404 or a name inside `wwwroot/`; no file system access — safe.

## Positive observations
- `RevitRequestQueue`: single handler, Raise result checked, refused/cancelled/disposed requests complete their TCS, continuations asynchronous (R9).
- Strict origin check (`scheme+host+port`) before parsing; dev URL limited to localhost; rejections keep the request id so the page settles fast.
- CSP without `unsafe-inline` scripts, `nosniff`, `base-uri 'none'`, Vite build configured for it (no preload polyfill, relative base).
- Wire contracts pinned by `WireFormatTests`; `protocol.ts` matches every C# field name, enum spelling (`activeView`, `select|zoom`, `light|dark`) and nullability.
- Event handlers O(1) + coalesced; statistics one collector pass + `GetElementCount` metrics.
- Core is host-free and tested; file-scoped namespaces, sealed classes, records for DTOs, no plan codes in code.
- Logical names verified in the built DLL; design-time builds skip npm.

## Recommended actions (order)
1. H1 + M1 together (single timeout source, BUSY/join semantics, page BUSY handling) + a test for "timeout ⇒ queued request never runs".
2. M2 (token in the reader loop, statistics budget).
3. M3/M6 (document/view key in statistics, replay on pane show).
4. M4 (guard `PostJson`, process-failure recovery), M5 (host-side zoom to selection).
5. M7, then Lows.

## Metrics
- Type coverage: C# `Nullable enable` both projects, TS `strict` (tsconfig) — no `any` seen in reviewed files.
- Tests: 65 xUnit (Core) + 25 vitest, per caller; add-in layer (queue, publisher, transport) untested off-Revit (T6: live verify pending in phase 6).
- Lint: not re-run.

## Plan follow-ups
Phases 1–5 match the code read. Phase 6 should add: live check of H1/M1 scenario (modal dialog open during a count), pane hide/show stale replay, a zoom with > 200 selected.

## Unresolved questions
None needing the user — all fixes are technical.
