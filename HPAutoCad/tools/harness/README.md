# Harnesses — unattended checks against a live AutoCAD 2026

Three scripts share `harness-common.ps1` (SECURELOAD auto-answer, UI Automation opt-in, guarded AutoCAD start):

- `run-bridge-unattended.ps1` — the bridge alone: `pipe-scenarios.py` speaks NDJSON JSON-RPC straight to the
  named pipe `\.\pipe\hpautocad-mcp-2026`, no MCP server involved.
- `run-server-smoke.ps1` — the published `HPAutoCad.Mcp.Server.exe` over stdio through `mcp-call.py`, exactly as a
  host AI would (initialize, tools/list, get_autocad_context, execute_autocad_code read / dry run / real run, get_run,
  every seed by name — 22 steps).
- `run-live-verify.ps1` — the full phase-5 proof through `live-verify.py` on **one** stdio session (`mcp-session.py`):
  the execute matrix (incl. busy → ESC posted → retry, no drawing, opt-in off), every seed with a real block and a
  pickfirst set, MISS → `propose_tool` → `test_tool` → `publish_tool` → CLI `registry approve` → `tools/list_changed`
  → call by name, an unguarded tool quarantined then restored/fixed/re-approved, the Revit exe beside it; with
  `-IncludeIsolation` also a second AutoCAD (pipe in use) and Civil 3D (bundle not loaded). `-OnlyIsolation`,
  `-SkipRevit`, `-RevitExe <path>`. Re-runnable: it deletes its own `move_text_between_layers` and
  `mcp_verify_count_block_refs` folders from the library first (`--reset-verify-tools`); the entities it draws stay in
  the harness's unsaved drawing. Outputs under `HPAutoCad/output/live-verify/`.

`run-bridge-unattended.ps1` drives the bridge run without a human:

1. starts `acad.exe /b bridge.scr` (`HPMCPBRIDGE` opens the status window, `HPMCPSTART` the listener) and
   answers the SECURELOAD prompt with *Always Load* if it appears;
2. waits for the pipe, then ticks "Allow AI code execution" through UI Automation (the opt-in is never
   persisted, by design) — with the box off it first checks that execute is refused with `-32001`;
3. runs the scenarios: ping, context, read, dryRun, commit, exception, `none` + modify, guard, compile
   error, cancel, timeout, progress/logs, serializer/args, modify + erase, undo (`U` after a `REGEN`
   boundary), then closes the drawing through COM (`-32003`), opens a new one and starts `LINE` through
   COM so the bridge answers `-32002` after the busy grace;
4. kills AutoCAD (the bridge itself never quits the host).

**Destructive by design — read before running:** it refuses to start while any AutoCAD is running, and every
COM call checks it is talking to the acad.exe it started; still, the drawings it opens are closed without
saving, the process is killed at the end, and answering the SECURELOAD prompt with *Always Load* trusts the
bundle folder permanently for this Windows user.

```powershell
pwsh HPAutoCad/tools/harness/run-bridge-unattended.ps1      # AutoCAD must be closed; ~2 minutes warm
pwsh HPAutoCad/tools/harness/run-server-smoke.ps1           # same, for the published exe (publish first — see its header)
pwsh HPAutoCad/tools/harness/run-live-verify.ps1 -IncludeIsolation   # phase-5 proof (~6 min, +8 with isolation)
python HPAutoCad/tools/harness/live-verify.py --exe <autocad exe> --revit-exe <revit exe> --only e   # Revit beside, no AutoCAD start
python HPAutoCad/tools/harness/pipe-scenarios.py --only ping,context   # against an AutoCAD you started yourself
python HPAutoCad/tools/harness/mcp-call.py <exe> tools/list            # any HP MCP exe, no AutoCAD/Revit needed
```

COM automation goes through Windows PowerShell 5.1 (`GetActiveObject` is not in PowerShell 7); a `SendCommand`
that leaves a command waiting for input never returns, so it runs detached and is killed at the end.
