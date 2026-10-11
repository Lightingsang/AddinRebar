# Code review 2026-10-11 — clash batch runs + report with pictures (HPNavis BIMCoordinator slice 3)

Read-only review of uncommitted diff. Rules: HP_CLEAN_CODE_CORE (N/M/FM/C/K/CM/T/Q), host appendix net48-inprocess (NI1–NI10), CODE_REVIEW_CHECKLIST. Build/test/live numbers taken from the lead (BIMCoordinator 174, Mcp.Server 82, McpBridge 188, Python 9; live THCSLT copy 121 tests ~80 s, 300 pictures 8.4 s) — not re-run here. Reflection on installed `Autodesk.Navisworks.Clash.dll` done for API facts (below).

## Scope
| File | Kind |
|---|---|
| HPNavis/HPNavis.BIMCoordinator/Reports/ClashReportRules.cs | new, pure (73) |
| HPNavis/HPNavis.BIMCoordinator/Reports/NavisClashReportWriter.cs | new, Interop (200) |
| HPNavis/HPNavis.BIMCoordinator/Reports/ClashSnapshot.cs | `HtmlReport` added (additive, optional) |
| HPNavis/HPNavis.BIMCoordinator/CoordinatorTools.Reports.cs | `ExportClashSnapshot(…, writeHtml, maxPictures, …)`, `PhaseTestNames` |
| SeedLibrary/Coordination/bim_run_phase_tests/* | new seed (HEAVY, auto) |
| SeedLibrary/Coordination/bim_export_clash_snapshot/* | `images`, `maxPictures` |
| NavisHeavyGate.cs + 4 test files | `ExportClashSnapshot` on the heavy list, counts |
| tools/bim-coordinator/{build-clash-report,clash-description-sheet,clash-report-html}.py + tests | xlsx agenda + HTML picture index |

API facts verified by reflection: `LcClClashGUIProxy`, `LcClashReportResultSelector`, `LcClClashReport`, `LcClReportField` all derive `Autodesk.Navisworks.Api.NativeHandle` (IDisposable). The proxy has exactly 3 virtual prompts (`PromptForFilename`, `PromptForFolder`, `ReportError`) — all overridden, so no dialog can appear. `LcClReportField` exposes only `GetUserNameW` (localized), no internal name.

## Acceptance (focus areas)
| Focus | Verdict | Evidence |
|---|---|---|
| Settings restore on every path | ✅ with caveat | Capture before any Set, restore in `finally` — NavisClashReportWriter.cs:28-41; Restore can mask the original exception (L1) |
| Selector / picture budget | 🟡 | Budget in test-name order, Resolved counted — :56-62, :84-88; empty selector path unverified (M4); native handles not disposed (M3) |
| Resume offsets with onlyNotRun | ⚠️ | index cursor `offset + failed.Count` assumes failed ⇔ `LastRun == null` — bim_run_phase_tests/code.cs:41 (M1) |
| No throw after a run | ⚠️ | `ct.ThrowIfCancellationRequested` :19, `Summarize` outside try :35, timeout margin 180 s (M2) |
| HTML parsing robustness | 🟡 | works on live format; silent-zero and mis-attribution paths (M5, L6) |
| Row/picture mapping | 🟡 | key (test, clash name) — clash-report-html.py:71; fallback found date wrong for unpictured rows (M6) |
| Excel formulas | ✅ | COUNT/SUM/COUNTIFS ranges correct incl. 0 rows (clash-description-sheet.py:95-105, 169-186); `All` row counts once, disciplines double-count by design (note A8) |
| File size / caps | ✅ | maxPictures 1..2000 checked (CoordinatorTools.Reports.cs:26), envelope small, byLevel ≤ 40; JSON rows ≤ 50 000 |
| Heavy fence | 🟡 | `ExportClashSnapshot` gated (NavisHeavyGate.cs); writer is internal; boundary test does not fence `WriteReport` (L7) |
| Contracts | ✅ | `htmlReport` optional, schemaVersion stays 1; Python reads with `.get` |

## Findings

### Logged defect outside the diff (High, pre-existing — log as H-xx, do not fix in this change)
**NavisScriptRunner.cs:86-98, :114-119** — the bridge forces a failure on timeout/cancel even when the script returned, then rolls its undo entry back; with heavy calls it rewrites `rolledBack` to `false` and logs "heavy operations … not undoable" and, on cancel, "its effect persisted". The live fact of this slice says the rollback **does** undo clash runs. So for `bim_run_phase_tests` a timeout or a cancel undoes every run of the call and tells the AI the opposite. Fix (separate change): treat clash runs as undoable in the message, or skip `RollbackOwn` when `hasHeavyCalls` and only report. Until then the seed's only protection is never running into the timeout (M2).

### Medium
**M1 — bim_run_phase_tests/code.cs:41, ClashReportRules.cs:44-54 — K5/T4 — index-based resume.** `resumeAt = offset + failed.Count` is right only if a test whose `TestsRunTest` threw keeps `LastRun == null` and every test that ran gets one (both unverified). If a failed run still stamps `LastRun`, the next call skips one never-run test per failure; if a run leaves `LastRun` null (e.g. empty selection) the call re-runs tests. Without `onlyNotRun`, any test added/removed between calls shifts the index too. Fix: name cursor — return `nextAfter = <last attempted name>`; seed/`PhaseTests(…, after)` starts at the first name with `string.CompareOrdinal(name, after) > 0` (list is already Ordinal-sorted). Pure, testable in `ClashReportRulesTests` (no test covers resume today).

**M2 — bim_run_phase_tests/code.cs:19, :35, :5/:8, CLAUDE.md claim — NI8 — a run can still be undone.** (a) `Summarize(doc, name, …)` sits outside the try: `FindTest` throws `InvalidOperationException` if the test vanished → exception after earlier runs → bridge rollback undoes them. Move it inside the try (or its own try that records into `failed`). (b) Budget check is "elapsed < budget" only: a test that starts at 419 s and takes > 180 s overruns the 600 s call → forced failure → all runs undone (see logged defect). Add a predictive stop: track the longest test so far and do not start another when `elapsed + 1.5 × longest > budgetSeconds`; consider default 300. (c) `ct.ThrowIfCancellationRequested()` after runs — the bridge fails the call anyway on cancel, so `break` would not save the runs; document instead. CLAUDE.md says "the seed never throws once a test ran" — not yet true; correct after the fix. Tool description "a run cannot be interrupted or undone" (tool.json:21) should say a timeout/cancel makes the bridge undo the call's runs.

**M3 — NavisClashReportWriter.cs:22, :27, :149-150, :166, :173, :182-183 — NI3/resource — NativeHandle not disposed.** `new LcClashReportResultSelector()` and the `FixedPathProxy` (both NativeHandle) are left to the finalizer, which frees native Navisworks objects on the GC thread, not the main thread. Fix: `using var selector = …; using var proxy = …;` (dispose after `WriteReport` returns, on the main thread). The per-index `LcClReportField`/`LcClStatusFlag` wrappers are borrowed handles; disposing them in a `using` is harmless and keeps the pattern uniform.

**M4 — NavisClashReportWriter.cs:24-35 — fail-closed — empty selector.** When no result is reported (all Approved, or tests with 0 results) `WriteReport` runs with an empty selector. Whether Navisworks then writes nothing or **every** result (≈20 k renders → 600 s timeout) is unverified. Fix: `if (budget.Taken == 0)` skip the write, return `HtmlPath = null` and warn "no reported clash — no pictures"; `ExportClashSnapshot` leaves `htmlReport` null.

**M5 — build-clash-report.py:226-235, clash-report-html.py:49-56, :78-87 — silent picture loss / mis-attribution.** (a) `htmlReport` recorded or `--html` given but file missing or parsed to 0 entries → `{}` and the console still prints "ảnh từ <path>"; every row says "chưa có ảnh". Warn (stderr) when the path does not exist or 0 rows matched. (b) `self.test` survives the end of a main table: a main table with no summary table of its own would attach its rows (and pictures) to the previous test. Reset `self.test = None` when a `mainTable` closes so a format change degrades to "no picture", never "wrong picture". (c) Snapshot cannot tell "beyond maxPictures" from "parse failed": add per-row `pictured: bool` (or the selected GUID list) to the snapshot and count mismatches.

**M6 — clash-description-sheet.py:118 — wrong data.** Column K "Ngày phát hiện / Found Date" falls back to the export date for every row without a picture (beyond `maxPictures`, or `images:false`), which states a false found date. Carry `ClashResult.CreatedTime` (UTC ISO) in `ClashSnapshotRow` (additive) and use it; otherwise leave K blank. Same column mixes `2026/10/11 09:55` (HTML) and `2026-10-11` (fallback) — one format.

### Low
- **L1 NavisClashReportWriter.cs:38-41, :177-184** — `Restore()` throwing inside `finally` masks the WriteReport exception and leaves the tab half-restored. Restore each setting group in its own try, collect failures into `Errors`, never throw from `finally`.
- **L2 ClashReportRules.cs:36, clash-report-html.py:61-75** — field/header logic keys on English display strings (`"Propert"`, `"Clash Name"`, `"Image"`, `"Grid Location"`, `"Date Found"`); the API offers no internal field name. A non-English Navisworks fails open (826 property columns, 12 MB per 20 clashes). Fail closed: require a field named `Image` among the names and refuse when the enabled-field count exceeds a cap (e.g. 60); state the English-UI assumption in the comment.
- **L3 NavisClashReportWriter.cs:84-88** — the budget spends pictures on Resolved results of P1 tests before New/Active ones of P2/P3. Two passes (open statuses first, then Resolved) keep pictures where the meeting acts.
- **L4 clash-description-sheet.py:136** — `os.path.relpath` raises `ValueError` when `--out` is on another drive than the pictures (Drive G: vs C:) after the whole book is built. Catch and fall back to an absolute `file:///` URI.
- **L5 build-clash-report.py:247, clash-description-sheet.py:143-145** — a truncated snapshot (`truncated: true`, > 50 000 rows) makes Summary/CLASH DESCRIPTION undercount while "Tổng hợp" (test byStatus) is complete; nothing in the workbook says so. Add a red note row when truncated.
- **L6 clash-report-html.py:31-33, :39, :55-56, :66** — parser robustness: a nested `<table>` inside a main-table cell turns `in_main` off for the rest of the table (use a depth stack); `endswith(".jpg")` is tested before `strip()` (the fixture's `href` already carries a trailing space) and ignores `.png/.jpeg`; `zip(headers, cells)` relies on header and content colspans mirroring (true for the columns used, which precede "Clash Point" — expand by `colspan` to be safe); a result-group row named like a clash collides on the key.
- **L7 CoordinationEngineHeavyBoundaryTests.cs:17-26** — the fence lists heavy names only; engine code that calls `NavisClashReportWriter.Write`/`LcClClashReport.WriteReport` from any other public facade would write files with the heavy opt-in off and still pass. Add: those two names may only appear in `NavisClashReportWriter.cs` and inside `ExportClashSnapshot`.
- **L8 K1 — ClashReportRules.cs:33 vs clash-description-sheet.py:33** — "reported statuses" defined in C# and Python. Let the snapshot carry `reportedStatuses` (or the M5c per-row flag) and have Python read it.
- **L9 M4/M5/N2 — CoordinatorTools.Reports.cs:23, NavisClashReportWriter.cs:17, :43** — `ExportClashSnapshot` has 6 parameters incl. a behaviour `bool writeHtml` (bare `true, 500` in NavisHeavyGateTests); `Outcome(Selected, Reported)` where "Reported" means "asked for a picture" clashes with `ReportedStatuses`. Rename to `Pictured`/`Eligible`. Facade shape is acceptable for the analyzer (`args.Bool("images")`); note only.
- **L10 FM2** — unbraced single-line `if`s and lines > 120 chars in new C#: NavisClashReportWriter.cs:21, :36, :59, :93, :160-161; CoordinatorTools.Reports.cs:26, :39, :46-47; ClashReportRules.cs:51, :69-70. Boy Scout on touched lines.
- **L11 bim_run_phase_tests/code.cs:48** — `ran = runs.Count` counts failed runs too; report `ran = runs.Count - failed.Count`, `attempted = runs.Count`.
- **L12 ClashReportRules.cs:44-54** — duplicate test names (user copies) are listed twice; `FindTest` returns the first both times, the second never runs and stays "not run" forever under `onlyNotRun`. `Distinct()` + a `duplicates` warning (the sync already removes HP duplicates with approval).

### Info (CHƯA XÁC MINH)
- Selecting a result group's GUID (NavisClashReportWriter.cs:93): if the report then emits all children of the group, Approved/over-budget children get pictures and the budget is exceeded. Check once on a model with grouped results.
- Whether `WriteReport` leaves the user's current viewpoint/hidden state changed (only selection/undo are fingerprinted; live `none` run passed).
- Each export adds a picture folder (2000 × ~50 KB ≈ 100 MB) on the synced Drive; no retention. Mention in the skill.
- New test names use sentence style, not `Method_Scenario_ExpectedResult` (N10) — consistent with the rest of BIMCoordinator.Tests; no churn asked.

## Positives
- Interop kept internal behind the single heavy facade; scripts cannot reach `Interop` (guard) and `ExportClashSnapshot` is on `HeavyMembers` with a gate test.
- Report-tab state captured before the first mutation and restored in `finally`; formatter/driver resolved by name and refused with the list on mismatch (fail closed) — `ClashReportRules.IndexOf` tested.
- Driver chosen by internal name (`Clash_ReportDriver_AllCombined`), not the localized label.
- Snapshot/HTML pairing by folder name derived from the snapshot (`HtmlFolderFor`), additive schema field, relative path stored.
- Batch seed: per-test failure isolation, wrapper re-resolved by name before each run, pure test list in Core with tests; seed defaults == schema defaults.
- Workbook: thumbnails instead of 1024 px originals, per-cell hyperlink strings (shared Hyperlink pitfall documented), `fullCalcOnLoad`, save retries for a locked file.

## Score
7/10 — Approve after M1–M4 (they touch the "runs must never be undone" invariant and native-handle hygiene); M5–M6 before the first meeting workbook from the original model; Lows at leisure. Log the bridge rollback/message contradiction as an H-xx defect.

**Status:** DONE_WITH_CONCERNS
**Summary:** Feature is sound and fenced; resume cursor, post-run throw/timeout paths, undisposed NativeHandles and the empty-selector case need fixes before running on the original model.
**Concerns/Blockers:** Bridge rollback after heavy runs contradicts its own message (pre-existing, outside diff).
