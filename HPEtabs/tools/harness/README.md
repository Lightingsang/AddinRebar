# HPEtabs live-verify harness

Drives one stdio MCP session (`HPEtabs.Mcp.Server.exe` → pipe `hpetabs-mcp-22` → `HPEtabs.McpBridge.exe` → COM → ETABS 22)
through the scenarios that prove the bridge on the dev machine. Windows PowerShell 5.1 (UIA on the bridge's own window,
relaunches itself from pwsh) + Python 3 (`../../../McpShared/tools/mcp-session.py`, `harness_common.py`).

**It never starts, stops or sends keys to ETABS.** It starts and closes only the bridge exe it launched, ticks the two opt-in
checkboxes and clicks Attach/Detach through UIA scoped to that process, and isolates the server on a registry root under
`HPEtabs/output/live-verify[/<Tag>]/registry-runN` (env `HPETABS_MCP_Registry__LibraryPath/DbPath`). Your `%AppData%\HPEtabs\McpServer`
registry is never touched. The bridge's real settings/audit/log/snapshot folders are.

**The `bridge` and `seeds` phases write to the open model** (forced saves, frames added under W, restrained, analysed, deleted under
D, model unlocked again) — run them on a **throw-away copy** only. Start ETABS from its shortcut first, then File › Open the model:
an ETABS that was started another way may not register its API object (the bridge then says "not registered for the API in this session").

```powershell
pwsh HPEtabs/tools/harness/run-live-verify.ps1 -Phase detached                 # bridge only, no ETABS needed
pwsh HPEtabs/tools/harness/run-live-verify.ps1 -Phase spike                    # + Attach: units, GetNameList, timeout
pwsh HPEtabs/tools/harness/run-live-verify.ps1 -Phase bridge                   # + writes with snapshots, D on/off
pwsh HPEtabs/tools/harness/run-live-verify.ps1 -Phase seeds                    # + the 12 seeds, the registry loop, run_analysis for real
pwsh HPEtabs/tools/harness/run-live-verify.ps1 -Phase full -Publish -Runs 3    # everything, exes published first and run from output/
pwsh HPEtabs/tools/harness/run-live-verify.ps1 -Phase all -Interactive         # + nomodel / modal / closed steps you perform in ETABS
pwsh HPEtabs/tools/harness/spike-step.ps1 -Action start|attach|phase <p>|destructive on|off|state|stop   # one step at a time
```

Phases of `live-verify.py --phase …` (each = one server session): `disabled`, `detached`, `spike`, `nomodel`, `modal`, `closed`,
`bridge`, `bridgedestructive`, `seeds`, `registry`, `seedsdestructive`. `run-live-verify.ps1 -Phase full` runs
disabled → detached → Attach → spike → bridge → [D on] bridgedestructive [D off] → seeds → registry → [D on] seedsdestructive [D off]
on one registry root per run; results in `summary.json` + `live-verify.log` + the per-phase JSON saves.

Reference results: `plans/260916-2152-etabs-mcp-2026/reports/phase-04-live-verify.md` (3 × 102 PASS, 2026-09-17).
