# HPCivil3d harness

Unattended checks of the Civil 3D MCP against a real Civil 3D 2026. Every script starts its own `acad.exe`, drives it
through UI Automation (the opt-in checkbox of our own bridge window) and COM (pid-guarded: it refuses when any acad.exe
it did not start is alive), works on **copies** of the Civil tutorial drawings under `HPCivil3d/output/`, never saves a
drawing, uses isolated registry roots (`Registry:LibraryPath/DbPath` overrides — `output/live-verify/registry` for the Civil
server, `output/live-verify/registry-autocad` for the AutoCAD exe checked beside it) so neither `%AppData%\HPCivil3d\McpServer`
nor `%AppData%\HPAutoCad\McpServer` is written (the live-verify session hashes both before and after and fails if they moved),
and quits only the processes it started (COM close-all + Quit, force-kill after 45 s). Close your own AutoCAD-family
products before running; nothing here touches another bundle. `-UseLiveRegistry` reverses the registry guarantee on purpose
(the harness then installs seeds and its own `mcp_verify_*` tools into your real Civil root — every tool it creates carries
that prefix, and `--reset-verify-tools` removes only those). Prerequisites for `-IncludeIsolation`: AutoCAD 2026 and Advance
Steel 2026 installed beside Civil 3D, both server exes published (`HPCivil3d/output/…`, `HPAutoCad/output/…`), and a bridge that
may prompt SECURELOAD (answered *Load Once* for the harness's own pid only). The user's older `Civil3dMcp.bundle` /
`AutoCadMcp.bundle` (`Platform="AutoCAD*"`) load into every product; the harness leaves them alone.

| Script | Shell | What | Result on 2026-09-18 |
|---|---|---|---|
| `run-live-verify.ps1 [-Runs n] [-IncludeIsolation] [-OnlyIsolation] [-SkipAutocad] [-UseLiveRegistry]` | Windows PowerShell 5.1 | The whole loop through the **published** exe over one stdio session (`live-verify.py`): a start-up/context, e execute matrix, s the 12 seeds on `Profile-5F.dwg` + `Corridor-1a.dwg`, r registry loop (MISS → propose → test → publish → CLI approve → HIT; fragile → quarantine → restore; refused proposals), x the AutoCAD exe beside; then disabled / nodoc / busy; with `-IncludeIsolation` a second Civil 3D (pipe in use), plain AutoCAD (own bundle loads, Civil's does not, both pipes served), Advance Steel (neither), and the AutoCAD harness's own `-OnlyIsolation` | `-Runs 3 -IncludeIsolation`: 3 × 76 + isolation 8/8; after the review round `-Runs 1 -IncludeIsolation`: 80/80 + 9/9, 0 skip, live registries untouched (`plans/260917-1633-civil3d-mcp-2026/reports/phase-04-live-verify.md`) |
| `run-server-smoke.ps1` | pwsh 7 | Published exe + every seed by name against live Civil 3D, 28 checks | 28/28 |
| `run-bridge-unattended.ps1` | Windows PowerShell 5.1 | Pipe-level scenarios straight against the bridge (`pipe-scenarios.py`, no server): AutoCAD set + C1–C9 Civil + ESC/retry, 31 checks | 31/31 ×4 |
| `run-ribbon-check.ps1` | pwsh 7 | The `HPCivil3d ▸ MCP ▸ MCP Bridge` tab: exactly one, workspace and COLORTHEME round trips, button opens one window; icon MANUAL with screenshots | 12/12 + 1 MANUAL ×2 |
| `run-spike.ps1 [-SkipBuild] [-SkipIsolation] [-SkipCoexist] [-Only …]` | Windows PowerShell 5.1 | The design spike (`spike.py`): bundle loads only in Civil 3D, `civil` global, units, rollback of Civil objects, guard | kept as re-runnable evidence |

Shared pieces: `harness-common.ps1` (`Start-AcadWithBridge -Product C3D|ACAD|ADVS`, `Answer-SecureLoad` — *Load Once* for our own
pid only, `Set-OptIn` with retry, `Stop-Acad`, ribbon helpers, `Open-BridgeWindow` — HPCivil3d ▸ MCP ▸ MCP Bridge once the
loader logs its panel), `bridge.scr` (`HPC3DMCPBRIDGE` + `HPC3DMCPSTART`; kept for callers but no longer passed with `/b`:
with a `/b` script and CadAddinManager installed the MCP panel never appears and COM answers `MK_E_UNAVAILABLE`), and
`../../../McpShared/tools/{mcp-session.py, mcp-call.py, harness_common.py}` (canonical, imported by relative path).

Gotchas learned here: Civil 3D 2026 prompts SECURELOAD once **per unsigned DLL hash** (4 prompts after every rebuild) and
blocks loading until answered; a `Documents.Open` of a drawing that is already open yields a read-only copy — activate
instead; `.dwl`/`.dwl2` locks are hidden files (`Get-ChildItem -Force`); Civil 3D rejects COM (`RPC_E_CALL_REJECTED`) for
tens of seconds after a workspace switch or a drawing open — retry; a toggle right after another toggle can be swallowed
— poll; scripts run on the main thread, so `get_civil3d_context` queues behind a running script (the busy grace, not a
"context while running" guarantee, is the contract); tutorial `*-1` drawings are empty starting points.
