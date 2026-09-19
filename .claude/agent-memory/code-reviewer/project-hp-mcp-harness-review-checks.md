---
name: hp-mcp-harness-review-checks
description: What to check first when reviewing an HP MCP live-verify harness (HPAutoCad/HPCivil3d/HPNavis/HPEtabs tools/harness/*.ps1 + *.py) — pid guards, silent skips, pipe-name presence, live-registry writes by "beside" servers, vacuous audit/status checks, schema-default misconception; found in the Civil 3D phase-4 review 2026-09-18 (7.5/10)
metadata:
  type: project
---

Every new host harness is a copy of `HPAutoCad/tools/harness/`, so the same gaps recur. Check these before reading the diff:

- **Safety baseline that is now standard (do not re-flag):** every COM call refuses unless `Get-Process acad` count = 1 **and** pid = `HP_HARNESS_ACAD_PID`; `Answer-SecureLoad` is pid-scoped and clicks *Load Once* (no permanent trust); ESC by `PostMessage` to the pid's windows; drawings are copies closed with `Close($false)`; registry root under `output/`; env vars removed in `finally`. Verify by grep, then move on.
- **"Beside" servers run on the LIVE registry.** A scenario that starts the *other* host's exe (X group, coexistence context) inherits the environment → opens `%AppData%\HP<Other>\McpServer\registry.db` (mtime proves it). README claims of "never touches another product's registry" are then false. Ask for `--env <PREFIX>_MCP_Registry__LibraryPath/DbPath` on those calls and hash the live roots at the start *and* end of `main()`.
- **Silent skips:** `Write-Host '… skipped'` instead of a SKIP row makes the wrapper summary lie by omission; a group dropping out of `--only` when an exe is missing is the Python twin. Count SKIP lines in the log vs. the report's "n/n".
- **Pipe-name presence ≠ live listener.** `Directory.GetFiles('\\.\pipe\')` lists any instance with an open handle (a connected client, a process tearing down). Positive "pipe up" checks need a `-not (Pipe-Up)` baseline before the product starts. `Test-Path \\.\pipe\x` *connects* and the bridge drops the client — that was the run-2 false FAIL.
- **Vacuous evidence checks:** "audit lines exist" (> 0 forever after the first run), grepping the last N lines of a daily log for a fault line without a line-count baseline, `"quarantined" in json.dumps(detail)` (event history contains every past status). Ask for before/after counts and `detail["status"]`.
- **Schema `default` is documentation only.** Nothing in `HPRebar.Mcp.Server.Core` fills a missing arg from the schema default; the code's `args.X("k", fallback)` is what runs. A theory comparing them must fail when the fallback is not a literal (`Assert.NotEmpty(reads)`), else an expression fallback passes silently (mutation `Math.Max(50, 1)` → green).
- **Cross-check the report against the logs yourself:** `grep -c '^PASS '`/`'^FAIL '`/`'^SKIP '` per run, the `{"passed": …}` summary lines, `wrapper: n steps, m failed`, graceful-quit lines, and the mtimes of both live registry roots during the run window.

**Why:** the Civil phase-4 harness was the safest so far and still had a factually wrong README claim (live AutoCAD `registry.db` written at 13:36:18 inside run 3) plus two silent skips that made run 2's "5/6" an inference. Reading alone would not have found the registry write; the mtime did.
**How to apply:** for any `tools/harness` review — grep the pid guard, list every `Write-Host …skipped`, list every "beside" server start and its env, check pipe checks for a baseline, then diff the report numbers against the logs. Related: [[verify-static-gates-with-scratch-probe]], [[review-report-shape]], [[hp-mcp-seed-review-pitfalls]].
