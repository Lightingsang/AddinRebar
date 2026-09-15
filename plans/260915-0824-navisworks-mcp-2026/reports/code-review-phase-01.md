# Code review — phase 1: HPNavis bridge plugin + 3 additive McpShared changes

**Date:** 2026-09-15 · **Branch:** RebarVersion1 · **Reviewer:** code-reviewer (read-only) · **Score: 7 / 10**

## Scope

- `HPNavis/HPNavis.McpBridge/**` (17 files, 2 082 LOC cs+xaml), `HPNavis/{Directory.Build.props,HPNavis.slnx,.gitignore,global.json}`, `HPNavis/tools/harness/*` (622 LOC)
- Tracked diff `McpShared/` (4 files, +57/−3): Contracts `netstandard2.0;net48`, `MainThreadQueue(expireWithoutTicks)`, 1-line VM nullability fix, +2 queue tests
- Compared against `HPAutoCad/HPAutoCad.McpBridge/{BridgeEntry,MainThreadExecutor,Service/AutocadScriptRunner}.cs` and the loader
- Gates re-run: `dotnet build HPNavis.slnx -c Debug -p:DeployPlugin=false` → 0 W / 0 E · `McpShared` Server.Core.Tests **128/128** · Net48Tests **60/60**

## Acceptance-criteria verdict

| # | Check | Verdict |
|---|---|---|
| a | Only `..\McpShared\*` referenced; `HPNavis.*` prefix; no secrets/dev paths (registry → env → `ProgramW6432`) | ✅ |
| b | Execution opt-in unreachable from env/flag/file (`BridgeSettingsStore.Load` forces false); heavy flag = `volatile` field on `NavisHeavyGate`, never persisted; `HPNAVIS_MCP_BRIDGE_SHOW_WINDOW` → `ShowWindow()` only | ✅ |
| c | `BeginTransaction → run → Commit` always; timeout always fails; heavy never `RolledBack=true`; `none`+change → error; dryRun+heavy rejected pre-run | ✅ logic — **but `RollbackOwn` condition breaks on same-label consecutive runs (H1)**; several handlers let exceptions reach Roamer (H2, M6, M7) |
| d | Resolver allow-list + requester check; `Install` idempotent via `Interlocked` | 🟡 requester check is decorative in Roamer (M1) |
| e | Pipe/main-thread races; `ScheduleExpiry`; default `expireWithoutTicks=false` unchanged | ✅ default byte-identical (one `if` on a `false` field; test covers it) · 🟡 peek/dequeue race (M4) |
| f | Serializer `MaxDepth=8`, `IgnoreCycles`, Navis types summarised | ✅ · 🟡 whole-value serialisation before truncation (M5) |
| g | Harness kills/closes only its own Roamer; never saves; standard install-dir fallback only | ✅ (`Assert-NoNavisworksRunning`, pid-scoped `Answer-SavePromptNo`) |
| h | File-scoped namespaces = folders, `sealed`, nullable, no plan refs in code | ✅ code clean; `.slnx` comment says "phase 3" (L3); `NavisMainThreadExecutor.cs` 309 lines — no natural split (AutoCAD twin is 238 with less to do) |

## Findings

### High

**H1 — `RollbackOwn` misses its own entry when the previous undo top has the same label → dryRun edits persist**
`HPNavis/HPNavis.McpBridge/Service/NavisScriptRunner.cs:110`
`undoIsOurs = committed && after.NextUndo == label && after.NextUndo != before.NextUndo`. `Document.NextUndo` is a *display name* only (API xml: "Display name of transaction that will be undone next"). `run_tool X` (commit) followed by `test_tool X` (dryRun) — the registry labels both with the tool name (`ToolManager.cs:174`) — gives `before.NextUndo == after.NextUndo == "MCP: X"` → `undoIsOurs=false` → no `Rollback()`, the dryRun's edit **persists**, `rolledBack=false`, `changed.added=1`, and line 120 logs the wrong "produced no undoable change". Same for exception/timeout after a same-label run ("Changes could not be rolled back"). Not covered by the spike (every scenario used a distinct label).
**Fix:** make the label unique when it collides: `if (before.NextUndo == label) label += " ·";` (or append a per-session counter), computed before `BeginTransaction`; keep the plain label otherwise so the Undo menu stays readable. Add a harness step "w1 → dryrun with the same label".

**H2 — Plugin lifecycle calls are unguarded; any exception goes straight into Roamer.exe**
`HPNavis/HPNavis.McpBridge/HPNavisBridgePlugin.cs:102,104`, `HPNavisWindowPlugin.cs:122`, `BridgeEntry.cs:55-93,197-205`
The AutoCAD sibling's loader wraps `Start`/`dispose` in try/catch (`BridgeLoaderApplication.cs:36-66,79-86`); the Navis design dropped the loader and the wrapping went with it. `BridgeEntry.Start` (AuditLogger dir creation, `McpBridgeHost.Install`, self-check), `BridgeEntry.Dispose` (`_currentCancel?.Cancel()` can throw `ObjectDisposedException` racing the pipe-thread `finally` at `NavisMainThreadExecutor.cs:142-143`; `_host.Dispose()` unwrapped) and `ShowWindow()` (throws `InvalidOperationException` when `_host` is null) all propagate to Navisworks' plugin loader / Add-ins click with no log line saying why.
**Fix:** `OnLoaded`/`OnUnloading`/`Execute` → `try { … } catch (Exception e) { Log.Error(e, …); }` (Execute: also a WPF `MessageBox` pointing at `LogDirectory`); in `Dispose` wrap `_currentCancel?.Cancel()` in `try/catch (ObjectDisposedException)`.

### Medium

**M1 — Resolver answers every null-requester request; in Roamer that is every request**
`HPNavis/HPNavis.McpBridge/PluginAssemblyResolver.cs:57-58`
Spike log: 85/85 resolutions "requested by <none>", including our own static binds (Core → Serilog 4.2). So `IsInFolder(requester)` never filters anything, and a foreign plugin's failed bind for `System.Text.Json 8.0` / `Serilog 3.x` / `System.Memory 4.0.1.1` is answered with our 10.0 / 4.4 / 4.0.5 (the runtime accepts a simple-name match). Also `Path.GetFullPath`/`Assembly.LoadFrom` exceptions inside the handler surface as `FileLoadException` in the *foreign* requester.
**Fix:** answer only when `ourVersion.Major == requested.Major && ourVersion >= requested` (all 4 legitimate resolutions in the spike pass this); wrap `OnResolve` body in try/catch returning null.

**M2 — `doc.Clear()` is not in the heavy gate although ADR-04 §3 lists it**
`HPNavis/HPNavis.McpBridge/Service/NavisHeavyGate.cs:139-145`
`Document.Clear` = "Delete entire contents of document, returning to default (untitled, empty) state" — destructive, not undoable, runs with heavy OFF. Name-only matching cannot include `Clear` (every `List.Clear()` would trip).
**Fix:** in `VisitMemberAccessExpression`, treat `Clear` as heavy when the receiver is `doc`, `app.MainDocument`, `app.Documents[...]` or an identifier ending in `Document`.

**M3 — Path policy fires on read-only scripts (no heavy call in sight)**
`HPNavis/HPNavis.McpBridge/Service/NavisHeavyGate.cs:194-212`
`CheckPath` runs on every string literal regardless of `HasHeavyCalls`. In Navisworks source models normally live on shares, so `doc.Models.Where(m => m.SourceFileName.StartsWith(@"\\bim-srv\"))` under `none` is refused with "would authenticate to a remote share"; any literal starting with `//` is refused as UNC; `AppendFile` of the install-dir *Samples* is refused as a write. ADR scopes the policy to heavy runs.
**Fix:** collect path hits during the walk, emit them only when `HasHeavyCalls`; apply the install-dir fragment only to writers (`SaveFile`, `Export*`, `PublishFile`, `GenerateImage`).

**M4 — `ScheduleExpiry` peek-then-dequeue is not atomic against a concurrent tick**
`McpShared/HPRebar.McpBridge.Core/Host/MainThreadQueue.cs:185-187` (called from the timer at `:119-123` without the `_ticking` gate)
`TryPeek(head A, expired) → [tick dequeues A] → TryDequeue → B` refuses a *young* B as busy even though the host is quiescent. Window is nanoseconds and needs two queued items (execute + context both parked behind a modal), so impact is one spurious `-32002`; still a correctness gap in shared code. `Task.Delay` per enqueue is not a leak (completes after grace + 50 ms). Default path unaffected.
**Fix:** `lock (_pending)` around the peek/dequeue decision in `FailExpired` and around the `TryDequeue` in `OnTick` (not around `Work`), or re-check `IsExpired(item)` after dequeue and re-enqueue is not FIFO-safe — prefer the lock.

**M5 — Result serialiser walks the whole value before truncating; per-item cost is high on the main thread**
`HPNavis/HPNavis.McpBridge/Service/NavisResultSerializer.cs:43,51-58,152-170`
Shared pattern with AutoCAD, but each `ModelItem` line calls `BoundingBox()` and `Children.Count()` (enumerates children). `return root.DescendantsAndSelf.ToList();` on a real model serialises 10⁵–10⁶ items after the script returned, `ct` no longer observed, Navisworks frozen for minutes, then 99.9 % is thrown away. Also `CanConvert` intercepts Navis *collections* (`ModelItemCollection`, `ClashResultGroup`) and Clash types → one `{type,text}` line instead of the elements; `new JsonSerializerOptions` per run discards STJ's metadata cache.
**Fix:** serialise into a `Stream` that throws past `_maxOutputBytes` and catch it into the truncated result (bounds both bytes and per-item work); exclude `IEnumerable` from `CanConvert`; add `ClashResult`/`ClashTest` cases; cache options and pass `units` through a per-run field.

**M6 — Idle handler is not exception-safe**
`HPNavis/HPNavis.McpBridge/NavisMainThreadExecutor.cs:60,62`; `MainThreadQueue.cs:137`
`OnTick` has try/finally but no catch around `_isQuiescent()`; `NavisQuiescence.IsQuiescent(Application.ActiveDocument)` reads `document.IsActiveTransaction` and P/Invokes `IsWindowEnabled` on every idle tick — a throwing document during teardown crashes Roamer (AutoCAD has the same exposure but a smaller `isQuiescent`).
**Fix:** `_onIdle = (_, _) => { try { _queue.OnTick(); } catch (Exception e) { Log.Debug(e, …); } };` — host-side, no engine change.

**M7 — `after` fingerprint can throw past `Commit` and skip the rollback decision**
`HPNavis/HPNavis.McpBridge/Service/NavisChangeCounter.cs:28`
`doc.CurrentSelection.SelectedItems` sits outside `Safe(...)`; a throw at `NavisScriptRunner.cs:108` (after `Commit`) leaves a dryRun's edit committed with an exception result instead of a rollback.
**Fix:** move the `SelectedItems` read inside `Safe` (pass a null-tolerant collection to `HashSelection`).

### Low

- **L1** `NavisMainThreadExecutor.cs:74-84` — `HeavyOperationsEnabled` setter does not require `_settings.ExecutionEnabled`; only the VM enforces the dependency (`NavisBridgeStatusViewModel.cs:66-68`). Add the guard in the setter so the invariant does not live in the UI.
- **L2** `harness/run-bridge-spike.ps1:117-119` — `AppActivate` result discarded; if activation fails, `SendKeys ^o` opens a file dialog in whatever window has focus. Check the boolean and skip S-07 with a note.
- **L3** `HPNavis/HPNavis.slnx:11,15` — "from phase 3 on" / "(server, phase 3)" plan references in a checked-in artefact; say "the stdio server exe (not yet added)".
- **L4** `NavisScriptRunner.cs:55` — `MaxTimeoutSeconds` is read on the main thread at run time; a heavy toggle between `Check` (pipe thread) and `Run` changes the clamp mid-request. Capture the clamp with `hasHeavyCalls` on the pipe thread and pass it in.
- **L5** `Resources/Themes/` — single (light) `NavisTheme.xaml`; AutoCAD ships Dark+Light. Fine for MVP (ADR-05), flagging for the Ribbon phase.

## Positive observations

- Transaction policy implemented exactly as ADR-02 (commit-then-`RollbackOwn`, `none` via fingerprint, heavy never claims rollback, dryRun+heavy refused before running); heavy audit "started" line before enqueue as ADR-04.
- Every Roslyn touch that can fail on a Simulate install sits behind `NoInlining` (`NavisClashModule`, `TimelinerAssembly`); self-check verifies the resolver won the race and logs every resolution.
- `GetContextAsync` answers busy immediately (no 15 s server timeout → stray cancel); `ModalOpen = !IsWindowEnabled(main)` after the D2 lesson; depth counter with reset + staleness.
- Engine changes are genuinely additive (Contracts multi-target, optional ctor param with default, 1 nullability line); tool-surface snapshots byte-identical per spike; 188 engine tests green.
- Code standards: file-scoped namespaces = folders, `sealed`, `nullable enable`, `[ObservableProperty]`, code-behind = `InitializeComponent` + `DataContext` + `CloseRequested += Close`, all XAML colours/spacing via `DynamicResource` tokens, no plan artefacts in code comments, summaries explain the *why*.
- Harness: refuses to run beside a user's Navisworks, closes only its own pid, answers the save prompt "No", UTF-8-BOM gotchas documented.

## Recommended order

1. H1 (label collision) — 3 lines + one harness scenario; blocks phase 4 seeds (`test_tool` after `run_tool`).
2. H2 (lifecycle try/catch) — 15 lines, no behaviour change on the happy path.
3. M2 + M3 (heavy gate: `doc.Clear`, path policy scoped to heavy) — before the tool descriptions are written.
4. M1, M6, M7 — small host-side hardening.
5. M4, M5 — shared-engine / serialiser; M4 needs a test (`FailExpired` racing `OnTick`), M5 can ride with phase 4 when seeds return collections.

**Status:** DONE_WITH_CONCERNS
**Summary:** Phase-1 plugin is well-structured, matches the ADRs and passes every gate (build 0/0, 128 + 60 tests, 2/2 live runs); two High items need fixing before phase 2: `RollbackOwn` silently keeps dryRun edits when consecutive runs share a label (the registry always does), and plugin lifecycle/Add-ins entry points let exceptions reach Roamer.exe with no log trail.

## Outcome (applied 2026-09-15, same day)

| Finding | Action | Where |
|---|---|---|
| H1 label collision hides own undo entry | **Fixed** — label gets ` (n)` suffix until it differs from `before.NextUndo`; harness case `samelabel` (W1 run → dryRun, same label) passes 3/3 runs | `NavisScriptRunner.cs`, `pipe-scenarios.py` |
| H2 plugin entry points unguarded | **Fixed** — try/catch + `Log.Error` in `OnLoaded`/`OnUnloading`/`Execute`, `BridgeEntry.ReportStartupFailure`, `Dispose` swallows `ObjectDisposedException` from `Cancel()` | `HPNavisBridgePlugin.cs`, `HPNavisWindowPlugin.cs`, `BridgeEntry.cs`, `NavisMainThreadExecutor.cs` |
| M1 resolver requester check ineffective | **Fixed** — answer only when requested major == file major and requested ≤ file version; `OnResolve` wrapped in try/catch | `PluginAssemblyResolver.cs` |
| M2 `doc.Clear()` not heavy | **Fixed** — `Clear` counts as heavy when the receiver is the `doc` global | `NavisHeavyGate.cs` |
| M3 path policy on read-only scripts | **Fixed** — path diagnostics reported only when the script has heavy calls | `NavisHeavyGate.cs` |
| M4 timer/tick dequeue race | **Fixed** — `_dequeueGate` lock around peek+dequeue in both `OnTick` and `FailExpired`; 128 + 60 tests green | `MainThreadQueue.cs` |
| M5 serializer walks the whole value before truncating | **Deferred to phase 2** (serializer is that phase's scope: streaming/limit per collection) | — |
| M6 `_onIdle` not exception-safe | **Fixed** — try/catch + log | `NavisMainThreadExecutor.cs` |
| M7 `SelectedItems` outside `Safe` | **Fixed** | `NavisChangeCounter.cs` |
| L1 heavy setter ignores execution opt-in | **Fixed** — setter forces false while `ExecutionEnabled` is off | `NavisMainThreadExecutor.cs` |
| L2 `AppActivate` result ignored | **Fixed** — S-07 skipped with a note when Roamer is not foreground | `run-bridge-spike.ps1` |
| L3 plan refs in `.slnx` comment | **Fixed** | `HPNavis.slnx` |
| L4 timeout clamp read at run time | **Deferred to phase 2** (pass the clamp with `hasHeavyCalls`) | — |
| L5 single light theme | **Deferred** (Ribbon phase, per ADR-05) | — |

Re-verified after the fixes: `run-bridge-spike.ps1 -Runs 2 -WithModal` → 2/2 PASS (main 22/22, clash 2/2, modal 1/1), plus one more run after the Low fixes → PASS. Engine tests 128 + 60.
