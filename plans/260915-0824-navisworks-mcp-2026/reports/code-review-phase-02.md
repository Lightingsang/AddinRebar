# Code review — phase 2: HPNavis bridge runtime (undo rules, bounded serializer, tests, harness)

**Date:** 2026-09-15 · **Branch:** RebarVersion1 (phase 1 = 63d9454, phase 2 = uncommitted diff) · **Reviewer:** code-reviewer (read-only on source) · **Score: 7 / 10**

## Scope

- Tracked diff `git diff -- HPNavis` (13 files, +482/−128): `BridgeEntry.cs`, `NavisMainThreadExecutor.cs`, `PluginAssemblyResolver.cs`, `Service/{NavisResultSerializer,NavisScriptRunner,ScriptingSelfCheck}.cs`, `View/NavisBridgeStatusView.xaml`, `ViewModel/NavisBridgeStatusViewModel.cs`, `HPNavis.slnx`, `README.md`, harness (`harness-common.ps1`, `pipe-scenarios.py`, `run-bridge-spike.ps1 → run-bridge-unattended.ps1`)
- Untracked: `Service/NavisUndoDecision.cs` (67), `Service/BoundedOutputStream.cs` (47), `NavisMainThreadExecutor.Audit.cs` (65), `HPNavis.McpBridge.Tests/` (6 files, 457 LOC, net48 xunit.v3)
- Parity: `git status --short McpShared HPRebar HPAutoCad` → no changes; the phase touches `HPNavis/` only. Contracts untouched.
- Mode: gates re-run by me (the task allowed it): `dotnet build HPNavis.slnx -c Debug -p:DeployPlugin=false` → **0 W / 0 E**; `dotnet test HPNavis.McpBridge.Tests` → **60/60**. Live numbers (2/2 runs, 43 checks) taken from `reports/phase-02-bridge-runtime.md`.
- Fact checks beyond reading: (1) STJ 10.0.12 flush behaviour probed on net48 **and** net10 with a counting enumerable + a copy of `BoundedOutputStream` (scratchpad, numbers below); (2) `Autodesk.Navisworks.Api.dll` / `Clash.dll` / `Timeliner.dll` reflected (ReflectionOnly, no Roamer) for every public type implementing `IEnumerable` and the `ClashTest`/`ClashResult` base chains.

## Explicit checks (a–g)

| # | Check | Verdict |
|---|---|---|
| a | `NavisUndoDecision` vs ADR-02 §1 | ✅ All four rows + "exception/timeout/cancel in every mode" + heavy-never-rolled-back reproduce; `ShouldRollBack` requires `undoIsOurs`, and `IsOurs` requires `topAfter == label && topAfter != topBefore` with `LabelFor` guaranteeing `label != topBefore` → the user's entry is never undone (the main thread is ours between `Commit` and `Rollback`, so no entry can appear in between). `LabelFor("t","MCP: t (2)")` → `"MCP: t"` is fine (differs from top). Only deviation from the ADR text: the dry-run "no undoable change" sentence goes to `logs`, not `Message` (correct — `Message` would flip `IsError`). **Gap M2:** a dry run whose edit persisted because its undo entry does not carry our label is reported as a clean success. |
| b | `NavisScriptRunner.Run` after the refactor | ✅ `NoneViolation` message set only when `failed == false`; `DryRun when !undoIsOurs` → log; `Failure when !undoIsOurs && documentChanged` → suffix appended to a non-null message; `hasHeavyCalls && rolledBack` → forced `false` + log before `Changed` is computed; `maxTimeoutSeconds` used for both `Math.Clamp` (:53) and the timeout message (:94), captured on the pipe thread right after `_heavy.Check` (`NavisMainThreadExecutor.cs:121-122`). **L1:** the `none` message is composed before the heavy override. Phase-1 **L4 closed**. |
| c | `BoundedOutputStream` + `Utf8JsonWriter` on net48 | Stream surface ✅: net48 `Stream` has no `Write(ReadOnlySpan<byte>)`; STJ's netstandard asset ends in `Write(byte[],int,int)` (probe: exactly 1 call). `writer.Dispose()` after the throw → `Flush()` with `BytesPending == 0` → only `_stream.Flush()` (MemoryStream no-op) ✅. `GetBuffer()` valid (parameterless ctor). Partial UTF-8 tail → U+FFFD → `TrimEnd('�')` → re-serialised as a JSON string ✅. **But H1: the bound does not stop the walk** — the writer buffers everything and the stream sees one `Write` at the final flush. |
| d | `NavisCollectionConverter<T>` scope | ✅ Reflection over the installed API: the only public Navisworks types implementing `IEnumerable` are collections (`ModelItemCollection`, `ModelItemEnumerableCollection`, `SavedItemCollection`, `DocumentModels`, `DataPropertyCollection`, `PropertyCategoryCollection`, `SearchConditionCollection`, `CommentCollection`, grid/schema/hyperlink collections, `TimelinerDataSourceFieldCollection`). `ModelItem`, `SavedItem`, `GroupItem`, `SelectionSet`, `Search`, `ClashTest` (GroupItem > SavedItem), `ClashResult` (SavedItem), `ClashResultGroup`, `DocumentClashTests` are **not** enumerable → their summary shape is unchanged. **M1:** `ModelItemEnumerableCollection` and `PropertyCategoryCollection` have no `Count`, and the converter keeps enumerating past the cap to count `skipped`. |
| e | Test project | ✅ `[ModuleInitializer]` from Polyfill compiles on net48 (Roslyn matches the well-known type by name); dropping `InternalsVisibleTo` removed the CS0436 risk and is why `PluginAssemblyResolver` went `public` — acceptable. No test instantiates `NavisObjectConverter<T>`, calls `Snapshot`, or reaches a Navis type (60/60 pass without the API on the probe path). Names/assertions are meaningful except **`Output_beyond_the_limit_is_cut_while_it_is_written`** (H1: asserts output size, cannot observe the walk, and the walk is in fact not cut). **L5:** the probe itself is unproven (Api dll is Amd64 mixed-mode; `LoadFrom` also needs its native imports on the search path). |
| f | Harness | Never saves ✅ (`SaveFile` appears only in guard/heavy refusal cases; `heavy` set runs with heavy OFF, and with heavy ON the UNC path policy still refuses; save prompt answered No; kill fallback). No dev-machine paths (install-dir fallback + Samples only; `output/spike` gitignored). `Stop-Navisworks` order (close dialogs → `CloseMainWindow` → answer No → kill after 25 s) ✅. **M3:** the PASS gate ignores four scenario sets' exit codes (incl. opt-in OFF). **L3:** `Close-RoamerDialogs` can hit the main window / floating panes. |
| g | Standards | ✅ Every `.cs` < 300 lines (max 273); file-scoped namespaces = folders (27 files checked); no phase/S-xx/D1/M5/ADR references in `.cs`/`.xaml` (grep clean; `S-0x` labels only in harness output/comments — allowed); new XAML uses `Spacing.SmallHorizontal` + `LinkButton` from `NavisTheme.xaml`; `[RelayCommand]`, `sealed partial`, try/catch in `OpenLogFolder`. |

## Findings

### High

**H1 — The serializer still walks the whole value; the bound trips only on the final flush**
`HPNavis/HPNavis.McpBridge/Service/NavisResultSerializer.cs:50-52` (+ test `HPNavis.McpBridge.Tests/NavisResultSerializerTests.cs:47-61`)
`JsonSerializer.Serialize(Utf8JsonWriter, …)` never flushes mid-walk: `WriteStack.FlushThreshold` is only set by the `Stream` overloads, and `Utf8JsonWriter.Grow` over a stream just grows its `ArrayBufferWriter`. Probe (STJ 10.0.12, counting `IEnumerable<T>`, limit 64 KB, ~300-byte items): **net48 — 100 000 / 100 000 items pulled, 1 `Write` call**; the `Stream` overload pulled **212**. Same on net10 (200 000 / 200 000 vs 3 173 for ints). So `return root.DescendantsAndSelf.ToList()` (a `List<ModelItem>`, not a Navis type, so the 200-cap does not apply) still costs one `BoundingBox()` + `Children.Count()` per item on the main thread before 99 % is thrown away — phase-1 **M5 is not closed**; the live `bigreturn` (0.3 s on the small gatehouse model) cannot tell the two behaviours apart, and the unit test cannot either.
**Fix:** replace lines 50-52 with `JsonSerializer.Serialize(stream, value, value.GetType(), options);` (STJ flushes every ~14 KB; `DefaultBufferSize = 4096` on the options tightens it to ~limit + 4 KB) and keep the two `catch` blocks unchanged. Strengthen the test with a `yield`-based enumerable that counts pulls and assert `pulled < 1_000` of 200 000. Re-run `bigreturn` on a model where the full walk is measurable, or add a scripted `Enumerable.Range(0, 1_000_000).Select(i => new { i, pad = new string('x', 200) })` case with a 60 s timeout — it must return in well under a second.

### Medium

**M1 — Collection cap counts the remainder by enumerating it all**
`NavisResultSerializer.cs:113-119`
`if (written >= MaxCollectionItems) { skipped++; continue; }` keeps pulling the enumerator to the end so the marker can say "of N". For `ModelItemEnumerableCollection` (`Descendants`, `DescendantsAndSelf` — lazy tree walks) and `PropertyCategoryCollection` (no `Count`) that is a full walk of the model on the main thread, `ct` unobserved; only the per-item cost is saved. The summary's "one screen of JSON, not a full walk" is not yet true for these two.
**Fix:** `var total = value is ICollection c ? c.Count : (int?)null;` then `break` at the cap and write `"first 200 of {total}"` when known, otherwise `"200 shown; more exist (filter in the script)"`.

**M2 — A dry run whose edit persisted is reported as a clean success**
`NavisScriptRunner.cs:119-121`, `NavisUndoDecision.cs:48-53`
When `DryRun && !undoIsOurs && documentChanged` (the undo top is not our label although the fingerprint moved — exactly the "W1?" calls the ADR still lists as unknown, `CurrentViewpoint.CopyFrom` / `OverrideTemporaryColor*`, which are allowed under `dryRun` today), the result is `isError=false`, `rolledBack=false`, log "produced no undoable change; nothing to roll back", while `changed.modified=1`. `test_tool` would read it as safe. The `Failure` branch handles the same situation (`FailureNotUndone`); the `DryRun` branch does not.
**Fix:** add `case UndoReason.DryRun when !undoIsOurs && documentChanged: message = "dry run: the document changed but no undo entry of this run is on top of the stack; the change persisted. Do not use dryRun for this call.";` (a constant beside `DryRunNothingToUndo`), keep the existing case for `!documentChanged`, and add the row to `Classification_follows_the_transaction_table` / a runner-level table test.

**M3 — Harness PASS ignores the exit code of four scenario sets, including "opt-in OFF"**
`HPNavis/tools/harness/run-bridge-unattended.ps1:127,151,168,177,205`
`$r.disabled`, `$r.heavyOffRefusesAgain`, `$r.modal`, `$r.afterModal` store `.summary` only; `$ok` (line 205) gates on `mainExit`, `clashExit`, `appendExit`. A bridge that executed code with "Allow AI code execution" unticked, or with heavy switched back OFF, would still print `PASS` (the failure would be visible only inside `run-N.log`). The "43 checks per run" figure therefore is not what the verdict enforces.
**Fix:** make `Invoke-Scenarios` push `$code` into `$r.scenarioExits[$only]` and add `-and (($r.scenarioExits.Values | Where-Object { $_ -ne 0 }).Count -eq 0)` to `$ok`; treat a skipped modal as 0.

### Low

**L1 — `none` message says "rolled back: yes" while `RolledBack=false` on a heavy run**
`NavisScriptRunner.cs:116-118` vs `:127-132` — `NoneViolationMessage(rolledBack)` is composed before the heavy override clears `rolledBack`. Move the `hasHeavyCalls && rolledBack` block above the `switch`, or compose the message after it.

**L2 — Ownership proof relies on `NextUndo` echoing the label verbatim, but the bridge does not normalise it**
`NavisUndoDecision.cs:36-42` — the server clamps labels to 64 chars (`ExecuteCodeService.cs:27,63`) but the pipe accepts any same-user client (the harness is one), and a label with a newline or 300 chars may come back shortened/normalised from `Document.NextUndo`, which would make our own entry "not ours" → dryRun edits persist. Collapse whitespace to one line and cap at 64 in `LabelFor`; add a theory row.

**L3 — `Close-RoamerDialogs` can WM_CLOSE the wrong window**
`harness-common.ps1:169-179,196-204` — when `Get-RoamerMainWindowHandle` returns `IntPtr.Zero` (title/class mismatch during startup, the 30 s path at `:75-79`), the main window is not excluded and receives `WM_CLOSE`; a floating dock pane (visible top-level, WinForms class) is also closed and Roamer persists that layout on exit. Return `@()` when `$main -eq [IntPtr]::Zero`; close only while `IsWindowEnabled($main)` is `$false` (a real modal disables its owner) or when `Class -eq '#32770'`.

**L4 — `finally { writer.Dispose(); }` can swap a converter exception for the limit exception**
`NavisResultSerializer.cs:52` — probe: a property throwing mid-walk with a nearly full buffer surfaces as `OutputLimitReachedException` (the dispose-flush trips the bound), so the value is reported as *truncated* instead of *not serializable*. Disappears with the H1 fix (the `Stream` overload flushes through its own pooled buffer).

**L5 — `NavisworksApiProbe` is dead code until it fails**
`HPNavis.McpBridge.Tests/NavisworksApiProbe.cs:18-33` — no test exercises the resolver, and `Autodesk.Navisworks.Api.dll` is Amd64 mixed-mode, so `Assembly.LoadFrom` alone will also need the install folder on the native search path (`SetDllDirectory`/`PATH`). Either add one smoke test that touches a trivial API type (and `SetDllDirectory(InstallDir)` in `Install()`), or shorten the csproj comment to "resolver present, unverified".

**L6 — `JsonSerializerOptions` rebuilt per run**
`NavisResultSerializer.cs:40-42` — every run discards STJ's metadata cache (reflection cost per run on the main thread); carried over from phase-1 M5. Cache one options instance per distinct `ScriptUnits.MmPerUnit` (a handful of values) or read `units` from a per-run field inside the factory.

## Deferred items from phase 1

| Item | Status |
|---|---|
| M5 serializer walks everything before truncating | 🟡 Navis collections capped at 200 (good) — **but the byte bound still walks the whole value (H1)** and the cap counts by enumerating (M1); `ClashTest`/`ClashResult` fall into the `SavedItem` branch (type name, displayName, guid, isGroup, childCount) — adequate until a seed needs status/distance (phase 4) |
| L4 timeout clamp read at run time | ✅ Captured on the pipe thread beside the heavy verdict, passed as `maxTimeoutSeconds` |
| L5 single light theme | ⏸ unchanged (Ribbon phase) |

## Plan follow-ups (report only — no plan files edited)

- Phase-2 checkbox "Serializer … (bounded writer, collection cap …)" and the `reports/phase-02-bridge-runtime.md` row "bound works while writing" should be reopened until H1 lands; the `bigreturn` evidence does not distinguish the two behaviours.
- ADR-02 §3 "W1?" (`CurrentViewpoint.CopyFrom`, `OverrideTemporaryColor/Transparency`): M2 gives the runner an honest report for those calls until phase 4 verifies their undo behaviour.
- Phase-2 plan line 46 lists `ClashTest/ClashResult/TimelinerTask` summaries; only the generic `SavedItem` shape exists — decide in phase 4 with the seed that needs them.

## What is good

`NavisUndoDecision` is a faithful, testable transcription of ADR-02 §1 (all rows plus failure precedence and heavy), and the runner became a straight `Classify → ShouldRollBack → switch` with no duplicated conditions; the timeout ceiling capture closes a real race; the `IEnumerable` intercept is exactly the right scope (reflection confirms no summarised type is enumerable); the audit/refusal split keeps every file under 300 lines with the *why* in each summary; the test project runs the plugin's pure layers on the real net48 runtime without Roamer; the harness now refuses to depend on desktop-wide UIA, skips the modal case rather than sending keystrokes blind, answers the save prompt No and force-kills only its own pid; build 0/0, 60/60, no plan artefacts in code.

**Score: 7 / 10** — structure, undo rules and tests are solid and the live gate is green, but the phase's headline serializer claim ("bounded while writing") is not what the code does, is untestable by the test that carries that name, and leaves the phase-1 M5 risk open for the very case (`.ToList()` of a tree) that phase-4 seeds will produce; two more Medium items (cap counting, dry-run-persisted reporting) sit on the same two files.

**Status:** DONE_WITH_CONCERNS
**Summary:** Phase 2 is well-built and green (0 W/0 E, 60/60, 2/2 live), and (a) undo rules, (b) runner composition, (d) converter scope, (g) standards all check out; but the `BoundedOutputStream` bound only trips at the final flush because `Serialize(Utf8JsonWriter)` never flushes mid-walk (verified by probe on net48 and net10), so the whole value is still serialised before truncation — a one-line switch to the `Stream` overload plus a counting-enumerable test fixes it.
**Majors:** H1 (serializer walks everything; fix = `Serialize(stream, …)`), M1 (cap counts by full enumeration), M2 (dry run that persisted reported as success), M3 (harness PASS ignores opt-in-OFF exit code).

## Outcome (applied 2026-09-15, same day)

| Finding | Action | Where |
|---|---|---|
| H1 `Serialize(Utf8JsonWriter…)` buffers the whole document, bound trips only at the end | **Fixed** — `JsonSerializer.Serialize(Stream, …)` (flushes every ~15 KB); new test `The_walk_over_the_value_stops_near_the_limit…` counts pulls from a lazy 500 000-item sequence: ≤ 20 000 pulled at a 4 KB bound | `NavisResultSerializer.cs`, `NavisResultSerializerTests.cs` |
| M1 capped collection keeps enumerating to count the rest | **Fixed** — `break` at the cap; total from `ICollection.Count` when available, else "unknown" | `NavisResultSerializer.cs` |
| M2 dry run whose change persisted without an undo entry reported as clean success | **Fixed** — `DryRun && !undoIsOurs && documentChanged` → `IsError` with `DryRunChangePersisted` (the "W1?" surface stays honest until phase 4 verifies it) | `NavisScriptRunner.cs`, `NavisUndoDecision.cs` |
| M3 harness PASS gate ignored 4 scenario sets | **Fixed** — every `Invoke-Scenarios` exit code collected; `$ok` requires all zero (≥ 5 sets ran) | `run-bridge-unattended.ps1` |
| L1 `none` message "rolled back: yes" while heavy forced `RolledBack=false` | **Fixed** — heavy adjustment moved before the message switch | `NavisScriptRunner.cs` |
| L2 label not normalised | **Fixed** — whitespace collapsed to one line, core capped at 64 chars + "…"; test added | `NavisUndoDecision.cs` |
| L3 WM_CLOSE could hit a dock pane / run without a main window | **Fixed** — only `#32770` dialogs, and nothing when the main handle is unknown | `harness-common.ps1` |
| L4 writer `Dispose` in `finally` could mask the converter exception | **Gone** with H1 (no writer of our own) | — |
| L5 `NavisworksApiProbe` untested | **Deferred** — loading the mixed-mode Api DLL into the test process needs the native search path; the probe stays best-effort until a test genuinely needs the API (phase 4 seed compile-check) | — |
| L6 `JsonSerializerOptions` per run | **Kept** — the converter factory captures the run's units; allocation is negligible next to the script | — |

Re-verified: `dotnet test HPNavis/HPNavis.McpBridge.Tests` → 62; `run-bridge-unattended.ps1 -Runs 2 -WithModal -WithNoDoc` re-run after the fixes (see `phase-02-bridge-runtime.md`).
