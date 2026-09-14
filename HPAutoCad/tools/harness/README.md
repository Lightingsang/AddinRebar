# Bridge harness — unattended pipe scenarios in AutoCAD 2026

No MCP server involved: `pipe-scenarios.py` speaks NDJSON JSON-RPC straight to the bridge's named pipe
`\.\pipe\hpautocad-mcp-2026`. `run-bridge-unattended.ps1` drives the whole thing without a human:

1. starts `acad.exe /b bridge.scr` (`HPMCPBRIDGE` opens the status window, `HPMCPSTART` the listener) and
   answers the SECURELOAD prompt with *Always Load* if it appears;
2. waits for the pipe, then ticks "Allow AI code execution" through UI Automation (the opt-in is never
   persisted, by design) — with the box off it first checks that execute is refused with `-32001`;
3. runs the scenarios: ping, context, read, dryRun, commit, exception, `none` + modify, guard, compile
   error, cancel, timeout, progress/logs, serializer/args, modify + erase, undo (`U` after a `REGEN`
   boundary), then closes the drawing through COM (`-32003`), opens a new one and starts `LINE` through
   COM so the bridge answers `-32002` after the busy grace;
4. kills AutoCAD (the bridge itself never quits the host).

```powershell
pwsh HPAutoCad/tools/harness/run-bridge-unattended.ps1      # AutoCAD must be closed; ~2 minutes warm
python HPAutoCad/tools/harness/pipe-scenarios.py --only ping,context   # against an AutoCAD you started yourself
```

COM automation goes through Windows PowerShell 5.1 (`GetActiveObject` is not in PowerShell 7); a `SendCommand`
that leaves a command waiting for input never returns, so it runs detached and is killed at the end.
