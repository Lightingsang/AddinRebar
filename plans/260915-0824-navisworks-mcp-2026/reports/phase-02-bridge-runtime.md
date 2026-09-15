# Phase 2 — bridge runtime report: HPNavis.McpBridge in Navisworks Manage 2026

**Date:** 2026-09-15 · **Machine:** dev box, Navisworks Manage 2026 23.0.1432.76, .NET Framework 4.8.9181 · **Models:** `Samples\gatehouse\gatehouse_pub.nwd` (+ `Samples\Getting Started\MEP.nwc` appended with the heavy opt-in) · **Runner:** `powershell.exe -File HPNavis/tools/harness/run-bridge-unattended.ps1 -Runs 2 -WithModal -WithNoDoc` · **Raw output:** `HPNavis/output/spike/run-{1,2,nodoc}.{log,json}` (gitignored)

**Gate result: 2/2 runs PASS — 43 checks per run** (opt-in OFF 1 · default matrix 30 · clash 2 · heavy append 5 · heavy OFF again 3 · modal 1 · ping after modal 1) **+ no-document run 1/1**, Roamer closed gracefully every time (exit 0, `HPNavis MCP bridge stopped` logged, pipe gone).

## What phase 2 added on top of the phase-1 spike

| Area | Change | Verified by |
|---|---|---|
| Undo rules | `NavisUndoDecision` (pure): `LabelFor` (one line, ≤ 64 chars, unique vs. undo top), `IsOurs`, `Classify` → `Keep/DryRun/Failure/NoneViolation`, `ShouldRollBack`; runner uses it; a dry run whose change persisted without an undo entry is reported as an error (`DryRunChangePersisted`) rather than a clean success | 20 unit tests + live `w1/dryrun/samelabel/empty/nonemod/exception/manual` |
| Timeout clamp | ceiling captured on the pipe thread with the heavy verdict and passed into `Run` (a heavy toggle mid-request cannot move it) | build + live `timeout` (max 120 in message) |
| Serializer | `BoundedOutputStream` + the `JsonSerializer.Serialize(Stream, …)` overload (flushes every ~15 KB): the JSON walk stops at `MaxOutputBytes` (64 KB) instead of serialising everything first — the review caught that the `Utf8JsonWriter` overload buffered the whole document; Navisworks collections enumerate ≤ 200 items and stop (total from `ICollection.Count` when known) instead of one `{type,text}` line | 8 unit tests incl. a pull-counting lazy sequence (≤ 20 000 of 500 000 pulled at a 4 KB bound); live `bigreturn`: whole gatehouse tree (`List<ModelItem>`) → `truncated=true` in 0.3 s; `search`/`selected` return `ModelItem` summaries |
| Self-check | probe builds a `Search` + `SearchCondition…DisplayStringContains`, calls `progress`, `log`, `units.ToMm` | log `self-check OK in 3346 ms … conditions 1 \| mm 1 \| args 42` |
| Window | "Open log folder" (`OpenLogs`) beside the audit link; heavy checkbox disabled while execution is off (executor enforces it too) | UIA `AllowHeavy.IsEnabled == false` before the execution tick, both runs |
| Executor | audit/refusal half split into `NavisMainThreadExecutor.Audit.cs` (partial) — every `.cs` < 300 lines | build |
| Tests | new `HPNavis/HPNavis.McpBridge.Tests` (net48, xunit.v3, MTP runner): heavy gate, undo decision, fingerprint/version, serializer, resolver → **62 test cases** | `dotnet test HPNavis/HPNavis.McpBridge.Tests` → 62/62 |
| Harness | `pipe-scenarios.py` 30-check default set + exclusive `heavyappend`, `nodoc`; runner checks the window (heavy disabled, listener down/up), runs clash + append with heavy ON, closes the modal with WM_CLOSE, adds a no-document Roamer | this report |

## Scenario matrix (run 1; run 2 identical)

| Group | Check | Evidence |
|---|---|---|
| Window | heavy checkbox disabled while execution off; listener toggle → pipe down → pipe up | `heavyDisabledWhileExecutionOff=true`, `pipeDownAfterToggle=true`, `pipeUpAgain=true` |
| Opt-in | execute with opt-in OFF → `-32001` | message names the checkbox |
| Read | `ping` 0.01 s · `context` 0.08 s (units Millimeters, 1 model, clash module) · `doc.Title` · `doc.Models` projection (units, 59 root children) · `Search(Item.Name contains "a")` → `ModelItem[]` summaries · `CurrentSelection.SelectedItems` after a selecting run | all `changed 0/0/0`, `rolledBack=false` |
| W1 | `auto` W1 edits → sets+1, vps+1, undo `MCP: spike w1` · same script `dryRun` → counts unchanged, undo top unchanged, `rolledBack=true` · **same label twice** → dry run still undone · empty `auto`+`dryRun` → `rolledBack=false`, user's top untouched · `CurrentSelection.Add` → undo entry (W1) · `manual` → commits + log "behaves like auto" | |
| `none` | `none` + `SelectionSets.AddCopy` → `isError`, message *declared transaction="none" … (rolled back: yes)*, sets unchanged, undo top restored | |
| Errors | compile error `CS1061` · exception after one edit → `rolledBack=true` · 7 guard cases (`GUARD`) · 3 heavy-OFF cases (`HEAVY`, checkbox named) · timeout 5 s (`timedOut`, max 120 in message) · `cancel_execution` | |
| Busy | `context` **while a script runs** → `-32002` in 0.00 s (executor answers busy at once) · modal (Open dialog) → `-32002` after the 8 s grace, script never ran; dialog closed by WM_CLOSE, main window enabled again, ping OK | `context.isBusy` not read (call itself refused) |
| Big result | `RootItems.First().DescendantsAndSelf.ToList()` → `truncated=true`, 0.3 s | bound works while writing |
| Heavy ON | clash: `ClashTest` Hard/tol 0 between 29/30 root children → **852 results, 66 ms**; second request during the run → `-32002` at once; `clashTestCount 1→2`, not busy · `AppendFile(MEP.nwc)` + `dryRun` → refused before running · real append → `Models 1→2` in **0.1 s**, `changed.added=1`, `rolledBack=false` · audit `started:[heavy] queued` then `ok:[heavy] ok` · UNC path → `HEAVY` path policy · `context` → `modelCount 2`, per-model units, `heavyOperationsEnabled=true` · heavy OFF again → 3 refusals | Roamer working set 1577 → 1645 MB (+68 MB) |
| No document | fresh Roamer, no model: `context.navis.isClear=true`, execute → `-32003` | run-nodoc.json |
| Shutdown | WM_CLOSE → save prompt answered **No** → `OnUnloading → Dispose`, exit 0, pipe gone | both runs + nodoc |

## Harness lessons (all in `harness-common.ps1`)

- **UI Automation from the desktop root is unreliable on a busy desktop** (`FindFirst/FindAll` time out with `0x80131505` whenever any other app's UI thread is slow). The bridge window is now looked up under Roamer's own top-level windows only and cached per Roamer; dialogs, the main window's enabled state and dialog closing use **Win32** (`EnumWindows`, `IsWindowEnabled`, `WM_CLOSE`) instead.
- **Keystrokes need the main window in the foreground**: the bridge's owned WPF window keeps focus after start, so `AppActivate(pid)` + `SendKeys ^o` often did nothing (or opened the dialog late). `SetForegroundWindow(mainHwnd)` first, then wait until `IsWindowEnabled(main)` turns false before running the modal scenario; skip with a note otherwise.
- **A Roamer killed with a dialog up greets the next start with a recovery prompt** that disables the main window → every request `-32002`, model never loads. `Stop-Navisworks` therefore closes dialogs (WM_CLOSE) before `CloseMainWindow`, and `Start-NavisworksWithModel` reports/dismisses startup dialogs after 30 s.
- Python stdout is captured through the console codepage → `sys.stdout.reconfigure(encoding="utf-8")`.
- Audit lines are picked by `source`, not by position: later read-only calls add lines of their own.

## Regression gates

| Gate | Result |
|---|---|
| `dotnet test HPNavis/HPNavis.McpBridge.Tests` (net48) | ✅ 62 |
| `McpShared/HPRebar.Mcp.Server.Core.Tests` / `Net48Tests` | ✅ 128 / 60 (unchanged this phase except the dequeue lock already in phase 1) |
| `dotnet build HPNavis/HPNavis.slnx -c Debug` | ✅ 0 warnings, plugin deployed |
| Success-criteria greps | `HeavyOperationsEnabled` in `McpShared/` = 1 hit — the `NavisInfo` DTO field (wire report, phase 0), no flag/setting there · hex colours in `View/*.xaml` = 0 · every `.cs` < 300 lines |

## Deferred / not covered

- `CurrentViewpoint.CopyFrom` and `OverrideTemporaryColor/Transparency` undo behaviour — still "W1?" in ADR-02 §3 (check when a seed needs them, phase 4).
- GUI-initiated clash / long load quiescence (Idle ticking with `ProgressDepth > 0`) — same code path as the modal case, not driven live.
- Undo menu text `MCP: <label>` checked through `doc.NextUndo`, not by opening the menu.
- `ExecuteAddInPlugin` / Add-ins menu single-instance — Automation start still unusable on this machine (phase 1 finding).
- Server side (phase 3): `inspect_type` over the pipe not exercised by the harness yet.

## Code review (same day)

`reports/code-review-phase-02.md`: 7/10, 1 High + 3 Medium + 6 Low, no Critical. Fixed before commit: H1 (bound trips only at the end — `Stream` overload + pull-count test), M1 (capped collection stops enumerating), M2 (dry run with a persisted, non-undoable change → error), M3 (harness gate covers every scenario set), L1–L3. Deferred: L5 (probe test needs the native search path), L6 (options per run, negligible). Final live run after the fixes: see the gate line at the top.
