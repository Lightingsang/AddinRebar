# Harnesses — unattended checks against a live Navisworks Manage 2026

Five PowerShell wrappers share `harness-common.ps1` (guarded `Roamer.exe` start with `HPNAVIS_MCP_BRIDGE_SHOW_WINDOW=1`,
UI Automation opt-ins on the HPNavis window only, Win32 for Roamer's own dialogs, graceful close that answers the
save prompt with *No*). The Python side speaks to the server through the host-neutral scripts in
`../../../McpShared/tools/` (`mcp-session.py`, `mcp-call.py`) — the folder that every HP MCP harness may import.

- `run-bridge-unattended.ps1` — the plugin alone: `pipe-scenarios.py` speaks NDJSON JSON-RPC straight to
  `\\.\pipe\hpnavis-mcp-2026`, no MCP server involved (43 checks per run; `-Runs 2 -WithModal -WithNoDoc`).
- `run-server-smoke.ps1` — the published `HPNavis.Mcp.Server.exe` over stdio (`server-smoke.py`, 9 steps) on an
  ephemeral registry root: initialize, `tools/list`, context, execute read / dry run / heavy OFF, `inspect_type`, search hit + miss.
- `run-seeds-live.ps1` — the 12 embedded seeds through `run_tool`/`test_tool` (`seeds-live.py`, phases `normal` and,
  after ticking "Allow heavy operations", `heavy`).
- `run-live-verify.ps1` — the phase-5 proof through `live-verify.py`, one stdio session per phase on **one isolated
  registry root** (`HPNavis/output/live-verify[/<Tag>]/registry` via `HPNAVIS_MCP_Registry__LibraryPath/DbPath`; the seeds
  are installed into it on first start, so proposals, approvals and quarantines never touch `%AppData%\HPNavis\McpServer`):

  | Phase | What it proves |
  |---|---|
  | `disabled` | opt-in off → `execute_navis_code` refused naming the checkbox; `get_navis_context` still answers |
  | `main` E | read under `none`, W1 commit (set + viewpoint + colour → one undo entry `MCP: <label>`), `dryRun` rolled back, same-label dryRun after a real run, empty dryRun leaves the user's undo alone, exception → rolled back, `none` + modify → refused + rolled back, `manual` ≡ `auto`, guard ×5, heavy OFF → `HEAVY`, compile error, `cancel_execution` racing a spin, timeout 5 s, context while a script runs → busy < 1.5 s, audit last line |
  | `main` S | every seed on the open model (`run_tool` real for read-only + `create_viewpoint`, `test_tool` dry run for the other writers, heavy seed refused twice) |
  | `main` R | MISS → ad-hoc run + hint → `get_run` → `toolify_run` → `propose_tool count_items_by_source_file` → `test_tool` → `publish_tool` → CLI `HPNavis.Mcp.Server.exe registry approve` → in `tools/list` ≤ 5 s → call by name; fragile tool (`InvalidOperationException`) 5 × fail → quarantined → `manage_tool restore` → `propose_tool newVersion` (`ArgumentException`) → re-approve → 5 argument errors keep it published; a proposal calling `TestsRunAllTests` is refused (heavy tools are seed-only) |
  | `main` C | `get_navis_context`, resources `navis://document/info` + `registry://tools`, prompt `navis_query_template`, `inspect_type Search` |
  | `modal` / `aftermodal` | Open dialog (Ctrl+O) up → execute answers busy after the grace; closed → runs again |
  | `heavy` | heavy ON: `create_and_run_clash_test` for real (600 s ceiling, never rolled back), `get_clash_results` sees it, `AppendFile` of the MEP sample + context (`modelCount` +1, `heavyOperationsEnabled`) |
  | `nodoc` (`-WithNoDoc`) | a Roamer without a model: context `isClear`, execute refused with the model noun |
  | isolation (`-IncludeIsolation`) | a second Roamer beside the first logs "already in use … Navisworks 2026" and the first keeps serving; our plugin folder renamed away → a Roamer start writes no bridge line and no pipe (restored in `finally`). **Never touches other plugins.** |

  Flags: `-WithNoDoc`, `-IncludeIsolation`, `-SkipModal`, `-SkipHeavy`, `-Tag run1` (own output folder), `-Exe`, `-Model`,
  `-AppendFile`. Outputs: `live-verify.log`, `summary-<phase>.json`, `run-summary.json`.
- `run-ribbon-check.ps1` — the Ribbon tab **HPNavis** ▸ **MCP** ▸ **MCP Bridge**: Roamer started *without* the show-window
  variable, tab header found once through UI Automation under the Roamer main window (`Find-RibbonTabHeaders`, a `Button`
  whose `AutomationId` is the tab id), selected with whichever pattern works (Invoke, then SelectionItem — verified by the button coming on
  screen), screenshot of the Ribbon (the one MANUAL: icon crisp), button
  click → bridge window + `MCP bridge status window opened` log line, second click → still one window, no `HPNavis MCP`
  Add-ins entry, no ERR/FTL; `-WithNoDoc` adds a Roamer without a model where Navisworks' start page greys every tab
  (header present, disabled). Exit 1 on FAIL, 2 when UI Automation was blind. Outputs under `HPNavis/output/ribbon-check/`.

**Destructive by design — read before running:** every wrapper refuses to start while any `Roamer.exe` is running,
closes only the Roamer it started (`CloseMainWindow`, save prompt answered *No*, kill as last resort) and never saves
the sample model. The harness edits (selection sets, viewpoints, colour overrides, a Timeliner task, clash tests, an
appended model) live only in the unsaved document.

```powershell
pwsh HPNavis/tools/harness/run-bridge-unattended.ps1 -Runs 2 -WithModal -WithNoDoc   # plugin over the pipe, ~4 min
pwsh HPNavis/tools/harness/run-server-smoke.ps1                                     # published exe, ~1.5 min (publish first — see HPNavis/README.md)
pwsh HPNavis/tools/harness/run-seeds-live.ps1                                       # 12 seeds, ~2 min
pwsh HPNavis/tools/harness/run-live-verify.ps1 -WithNoDoc -IncludeIsolation -Tag run1   # phase-5 proof, ~2.5–3 min (4 Roamer starts)
pwsh HPNavis/tools/harness/run-ribbon-check.ps1 -WithNoDoc                              # Ribbon tab/button, ~2.5 min (2 Roamer starts)
python HPNavis/tools/harness/live-verify.py <exe> --registry <dir> --phase main       # against a Roamer you started and opted in yourself
python McpShared/tools/mcp-call.py <exe> tools/list                                  # any HP MCP exe, no host needed
```

Windows PowerShell 5.1 only (UIA + `SendKeys`; the wrappers relaunch themselves from pwsh). UI Automation is used only
inside the bridge's own window and, for the Ribbon check, under the main window of the Roamer we started — never from the
desktop root; Roamer is brought to the foreground before a tab is selected or the Ribbon is photographed. A Roamer started through
the Automation API exits within ~15 s on the dev machine, so the harness launches `Roamer.exe "<model>"` directly.
