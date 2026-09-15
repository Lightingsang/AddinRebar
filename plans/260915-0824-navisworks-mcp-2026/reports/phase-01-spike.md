# Phase 1 — spike report: HPNavis plugin in Roamer.exe (Navisworks Manage 2026)

**Date:** 2026-09-15 · **Machine:** dev box, Navisworks Manage 2026 23.0.1432.76, .NET Framework 4.8.9181 · **Model:** `Samples\gatehouse\gatehouse_pub.nwd` (1 model, 1 pre-existing clash test) · **Runner:** `powershell.exe -File HPNavis/tools/harness/run-bridge-spike.ps1 -Runs 2 -WithModal` · **Raw output:** `HPNavis/output/spike/run-{1,2}.{log,json}` (gitignored) · **Bridge log:** `%LocalAppData%\HPNavis\McpBridge\logs\mcpbridge-20260915.log`

**Gate result: 2/2 runs PASS** — main set 21/21 ×2, clash 2/2 ×2, modal 1/1 ×2, S-01/S-02/S-09 ×2. Foreign plugin `NavisworksMCPPlugin` **enabled** throughout (never touched).

## Scenario table

| # | Scenario | Result | Evidence (run 1 unless noted) |
|---|---|---|---|
| S-01 | Roamer starts → plugin loads | ✅ ×2 | log `HPNavis MCP bridge starting from …\Plugins\HPNavis.McpBridge; Navisworks runtime 23.0 → 2026; CLR 4.0.30319.42000 (.NET Framework 4.8.9181.0)`. **No security / "publisher" prompt** for the unsigned DLL (unattended run needs none). Add-ins tab shows "HPNavis MCP" (window opened via `AddInPlugin` in earlier manual check; harness opens it through `HPNAVIS_MCP_BRIDGE_SHOW_WINDOW=1`) |
| S-02 | Roslyn self-check | ✅ ×2 | `MCP scripting self-check OK in 2762 ms (5 assemblies resolved by the plugin): navisworks 23.0 \| year 2026 \| units Millimeters \| args 411`. Resolves: `Serilog 4.2.0.0→4.4.0.0`, `System.Memory 4.0.1.2/4.0.2.0→4.0.5.0`, `System.Collections.Immutable 10.0.0.0→10.0.0.1`, `System.Reflection.Metadata 10.0.0.0→10.0.0.1` — **all `requested by <none>`** (85/85 resolve lines all day). Roslyn/Immutable `Location` = plugin folder |
| S-03 | `ping` / `context` with Roamer idle, mouse untouched | ✅ ×2 | ping `0.01 s`, context `0.08 s` (host=navis, units Millimeters, models 1, clash module true). `PostMessage(WM_NULL)` wake works; no fallback needed |
| S-04 | `return doc.Title;` under `none` | ✅ ×2 | value `"gatehouse_pub.nwd"`, changed 0/0/0, rolledBack false, 10 ms |
| S-04a | execute with opt-in OFF | ✅ ×2 | `-32001 "Code execution is disabled. Ask the user to tick 'Allow AI code execution' …"` |
| S-05 | `auto` + W1 edits (`SelectionSets.AddCopy`, `SavedViewpoints.AddCopy`, `OverridePermanentColor`) | ✅ ×2 | sets 1→2, vps 6→7, `NextUndo == "MCP: spike w1"`, changed added=2 (color override not counted — fingerprint has no material counter), rolledBack false. dryRun: counts unchanged, undo top unchanged (`MCP: spike w1` stays), `rolledBack=true` — commit-then-`RollbackOwn` works |
| S-05b | `auto`+`dryRun` with **no edit** | ✅ ×2 | rolledBack **false**, undo top untouched, log `dry run: the script produced no undoable change; nothing to roll back.` |
| S-05c | `CurrentSelection.Clear(); CurrentSelection.Add(root)` under `auto` | ✅ recorded | **Creates an undo entry** (`NextUndo` → `MCP: spike current`), selected 0→1, changed modified=1 → `CurrentSelection.*` is **W1** (undoable). `CurrentViewpoint.CopyFrom` not exercised |
| S-05d | `new Transaction(doc,"x")` without Commit | ⬜ not run | Guard denies `Transaction` and `BeginTransaction` (S-11), so the experiment is unreachable from a script; the runner's pre-check `doc.IsActiveTransaction → reject` stays as defensive code (`NavisScriptRunner.cs`) |
| S-06 | `throw` after one edit in `auto` | ✅ ×2 | `isError`, `InvalidOperationException: boom`, rolledBack true, sets unchanged |
| S-07 | Modal owned by main window (Open dialog via Ctrl+O) → `context`, `execute` | ✅ ×2 (after fix) | both answered `-32002 "Navisworks is running a command or showing a dialog…"` after 8 s each (16.3 s total); log `work 'context' refused: Navisworks not quiescent within 8s`. **Finding:** `Application.Idle` does **not** fire while a native modal runs → first attempt hung until the dialog closed; fixed with timer-based expiry (below) |
| S-08 | Clash run (script-driven, heavy ON) | ✅ ×2 | new `ClashTest` (Hard, tol 0, A=29/B=30 root children) created + `TestsRunTest` → **852 results, status Complete, 61–62 ms**; `Progress*` events fire during load (depth 1→3→0 in 1 s, `progressDepthZeroAfterLoad=true`), 28 progress lines per run; after clash `context.isBusy=false`, `clashTestCount 1→2`; a second `execute` sent 0.5 s into the heavy run got `-32002` immediately (one run at a time). GUI-initiated clash **not automated** (Clash Detective UI) — same quiescence path as S-07 |
| S-09 | Close Roamer (WM_CLOSE, save prompt answered No) | ✅ ×2 | exit code 0, log `HPNavis MCP bridge stopped`, pipe gone, 0 `[ERR]`/`Exception` lines in the teardown window |
| S-10 | Foreign plugin `NavisworksMCPPlugin` enabled beside ours | ✅ observed | self-check OK ×all runs; resolver never answered a foreign requester (every line `requested by <none>`); no conflict seen. Whether that plugin itself loaded was not checked (its ribbon is not part of our gate) |
| S-11 | Guard + heavy pre-pass | ✅ ×2 | 7 guard cases (`BeginTransaction`, `new Transaction`, `.Undo`, `NavisworksCommand`+`ToNavisworksConnection`, `Expression.Call`, `MessageBox.Show`, `Automation.NavisworksApplication`) → `GUARD`, never run; 3 heavy-OFF cases (`AppendFile`, `SaveFile` UNC, `TestsRunAllTests`) → `HEAVY` naming "Allow heavy operations"; UNC path refused additionally |
| — | timeout 5 s / `cancel` | ✅ ×2 | timed out at 5.3 s, `timedOut=true`, rolledBack false; cancel: `{cancelled:true, wasRunning:true}` → `Script was cancelled.` (audit 1490 ms) |

## Defects found and fixed during the spike

| # | Symptom | Root cause | Fix (where) |
|---|---|---|---|
| D1 | Roamer: "The Plugin was not found"; no log at all | `Assembly.GetTypes()` on the plugin threw `ReflectionTypeLoadException: System.Text.Json 10.0.0.0` — `HPRebar.Mcp.Contracts` (netstandard2.0) is compiled against the package's `lib/netstandard2.0` asset (assembly version **10.0.0.0**) while the plugin ships the `lib/net462` asset (**10.0.0.12**); .NET Framework binds exactly, no redirects inside a plugin, and the resolver is installed only after discovery | `HPRebar.Mcp.Contracts.csproj` → `<TargetFrameworks>netstandard2.0;net48</TargetFrameworks>` (additive: net8/net10 consumers keep the netstandard asset). Probe `GetTypes()` → 400 types, 2 plugin types |
| D2 | Every request `-32002` while the status window was open | `ModalOpen` used `GetWindow(main, GW_ENABLEDPOPUP)`, which also reports our own **modeless** window (owned by main) | `NavisQuiescence.ModalOpen` = `!IsWindowEnabled(main)` only |
| D3 | S-07: request hung until the dialog closed | Navisworks raises **no `Idle`** during a native modal; `MainThreadQueue` expires items only on ticks | `MainThreadQueue(…, expireWithoutTicks: true)` — opt-in timer (`Task.Delay(grace+50 ms)` → `FailExpired`) touching queued items only; **default false** → Revit/AutoCAD unchanged. 2 new tests (`MainThreadQueueTests`), also linked into Net48Tests |
| D4 | `ObjectDisposedException (WeakRef) … GroupItem.get_Children` after `TestsRunTest` | The `ClashTest` wrapper held across `TestsRunTest` is disposed by the run | API gotcha for phase 4 seeds: re-resolve the test from `TestsData.Tests` after running |
| D5 | CS8604 in `McpBridgeStatusViewModel` on net48 | `string.IsNullOrEmpty` carries no nullability annotation on net48 | `source is null \|\| source.Length == 0` (behaviour-identical) |

## Harness findings

- **Automation API start is unusable here:** `new NavisworksApplication()` (Windows PowerShell, `Autodesk.Navisworks.Automation.dll`) starts a Roamer that loads the plugin, logs `ready`, then exits within ~15 s **with or without our plugin** (folder renamed for the control run); `OpenFile` fails `0x800706BE` / `0x800706BA`; the process is invisible to `Get-Process Roamer`. Root cause not pursued. `ExecuteAddInPlugin("HPNavis.McpBridge.Window.HPNV")` therefore **unverified**. Harness launches `Roamer.exe "<model>"` directly (stable, 16–26 s to a titled main window).
- Window for unattended opt-in: process-scoped env `HPNAVIS_MCP_BRIDGE_SHOW_WINDOW=1` → `BridgeEntry.ShowWindowWhenGuiIsReady()` (after `GuiCreated`, one `ApplicationIdle` dispatcher pass). Only the window — the execution opt-in still starts OFF and is ticked by UIA (`AutomationId AllowExecution`, `AllowHeavy`, `ToggleListener`).
- Pipe open right after the listener starts fails once with `EINVAL` (python `open(r+b)`) → retry 0.5 s ×20 (bridge logs a harmless `Pipe is broken` for the aborted client).
- The cancelled run's response usually precedes the `cancel` response → read it from `_notifications`, never block on `readline()` after.
- Graceful close: `CloseMainWindow()` → Navisworks asks "save changes?" → UIA clicks `No` → `OnUnloading → Dispose` → exit 0.
- Windows PowerShell 5.1 reads BOM-less `.ps1` as ANSI → em dashes break parsing → scripts saved UTF-8 **with BOM**; `Tee-Object` writes UTF-16 → `Add-Content -Encoding UTF8`.

## Regression gates (shared engine touched)

| Gate | Result |
|---|---|
| `McpShared/HPRebar.Mcp.Server.Core.Tests` | ✅ 128 (126 + 2 queue tests) |
| `McpShared/HPRebar.McpBridge.Core.Net48Tests` | ✅ 60 (58 + 2) |
| `HPRebar/HPRebar.Mcp.Server.Tests` | ✅ 109 |
| `HPAutoCad/HPAutoCad.Mcp.Server.Tests` | ✅ 58 |
| `dotnet build HPAutoCad.slnx -c Debug -p:DeployBundle=false` | ✅ 0 warnings |
| `dotnet build HPRebar.slnx -c Debug.R26 -p:DeployAddin=false` | ✅ 0 errors (24 pre-existing rebar warnings, none in McpShared) |
| `dotnet build HPNavis.slnx -c Debug` (deploys plugin) | ✅ 0 warnings |
| `tools/list` Revit 33 / AutoCAD 24 vs phase-0 `before` snapshots | ✅ byte-identical (`snapshot-tools-list.ps1 -Tag after`, Server.Core.dll `63557D8A…`) |

## ADR updates driven by this spike

- **ADR-02:** S-05/S-05b/S-06 confirm commit-then-`RollbackOwn`; `CurrentSelection.*` moves from "W1?" to **W1**; S-05d dropped (unreachable behind the guard).
- **ADR-04:** `Idle` does not fire under a native modal → queue `expireWithoutTicks` (Navis only); `ModalOpen` = `!IsWindowEnabled(main)`; Progress depth counter verified (returns to 0); heavy run blocks a concurrent request with immediate `-32002`.
- **ADR-05:** no load-time security prompt for the unsigned plugin (2026, per-user `Plugins\`); `ExecuteAddInPlugin` unverified (Automation start broken on this machine); harness hook `HPNAVIS_MCP_BRIDGE_SHOW_WINDOW`.
- **ADR-01 (addendum):** Contracts must ship a net48 asset — see D1.

## Open items carried to later phases

- `CurrentViewpoint.CopyFrom` undo behaviour (W1?) — check when a seed needs it (phase 4).
- GUI-initiated clash / long file load quiescence (`Idle` ticking while `ProgressDepth > 0`) — same code path as S-07 but not exercised live.
- `ExecuteAddInPlugin` id format — verify if the Automation API works on another machine (phase 5 harness note).
- Whether `NavisworksMCPPlugin` should be disabled for the phase-5 live verify — 👤 user's profile; spike ran fine with it enabled.

## Code review (same day) and fixes

`reports/code-review-phase-01.md`: 7/10, 2 High + 7 Medium + 5 Low. Fixed before commit: H1 (dry run after a real run **with the same label** — the registry always labels a tool run with the tool name — kept its edits because `NextUndo` matched `before`; the label now gets a ` (n)` suffix until it differs, harness case `samelabel` 3/3), H2 (try/catch on every plugin entry point + idle tick + dispose), M1 (resolver answers only the same major / not newer), M2 (`doc.Clear()` heavy), M3 (path policy only with heavy calls), M4 (dequeue lock in `MainThreadQueue`), M6, M7, L1–L3. Deferred: M5 + L4 → phase 2 (serializer / clamp capture), L5 → Ribbon phase. Final live run after all fixes: 2/2 PASS (22 + 2 + 1 + 1 per run) and one extra PASS after the Low fixes.
