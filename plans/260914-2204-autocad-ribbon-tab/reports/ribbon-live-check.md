# Ribbon tab "MCP AutoCAD" — live check in AutoCAD 2026 (2026-09-14)

Machine: dev (AutoCAD 2026 R25.1.74 .NET 8, started by the harness from `acad.dwt`; bundle 0.2.0 deployed by `dotnet build HPAutoCad.slnx -c Debug`). Harness: `HPAutoCad/tools/harness/run-ribbon-check.ps1` (UI Automation + COM, pid-guarded). Logs: `ribbon-check-run{1,2,3}.log`, `bridge-harness-regression.log`, `server-smoke-regression.log`.

## Run 3 — 8/8 PASS, 0 MANUAL (exit 0)
| Check | Result |
|---|---|
| `loader.log` "ribbon tab HPAUTOCAD_MCP_TAB created (bridge available)" | ✅ 7 s after the pipe wait |
| tab button `AutomationId=HPAUTOCAD_MCP_TAB` under the AutoCAD frame | ✅ exactly 1 |
| `WSCURRENT` "Drafting & Annotation" → "3D Modeling" → back (COM `SetVariable`) | ✅ 1 tab after each switch — loader.log shows the tab **re-created** at 22:41:25 and 22:41:35: a workspace switch does drop code-added tabs and the `SystemVariableChanged → Idle → EnsureCreated` path restores exactly one |
| listener off before the button test | ✅ no pipe |
| Invoke "Bật listener" | ✅ `\\.\pipe\hpautocad-mcp-2026` up within 10 s |
| Invoke "Tắt listener" | ✅ pipe gone |
| Invoke "Bảng điều khiển" | ✅ bridge window with `AllowExecution` found under the AutoCAD frame |
| Invoke "Trạng thái" | ✅ no "failed" line in loader.log |

## Regression with the new loader + bridge
- `run-bridge-unattended.ps1` **21/21** (opt-in off, 18 pipe scenarios, no document, busy).
- `run-server-smoke.ps1` **22/22** (Claude Code's stdio flow: initialize, tools/list, context, execute, search, all 12 seeds). First pass failed only its own `tools/list == 24` assertion — the dev registry holds 26 (two tools approved during the phase-5 loop); the assertion now accepts approved tools beyond the seeds.

## What the earlier runs taught
1. AdWindows exposes a Ribbon **tab header as a `Button` whose `AutomationId` is the `RibbonTab.Id`** (Name = title), not as a `TabItem`; a `RibbonButton` is a `Button` named after its text once its tab is selected. Run 1 looked for a TabItem and found nothing (MANUAL everywhere); run 2 onward looks up the id.
2. `SendCommand('_.WSCURRENT "…" ')` through COM **never returned** (run 2 hung > 10 min, no dialog on screen) — the ribbon rebuild swallows the command echo. `ActiveDocument.SetVariable('WSCURRENT', …)` switches and returns at once.
3. Another bundle on this machine (`AutoCadMcp.bundle`, June 2026) already adds tabs "AutoCAD MCP" (`AUTOCADMCP_TAB`) and "Civil 3D MCP" with an "MCP: OFF" button. Ours is "MCP AutoCAD" / `HPAUTOCAD_MCP_TAB` — distinct id, but the two titles read alike; rename ours if that confuses (one string in `McpRibbonTab.TabTitle`).

## Not verified by the harness (user checklist)
- Tab visible after a normal AutoCAD start from the Start menu (harness starts `acad.exe /b script`; same autoloader).
- Behaviour while a command waits for input (a click calls the bridge delegate directly, never the command line, so it should work; not scripted).
- "Sao chép script cuối", "Thư viện tool", "Mở nhật ký", "Mở audit", "Hướng dẫn", "Tự khởi động listener" — effects are outside AutoCAD (clipboard, Explorer, settings.json): compile-verified and wired to real entry points, not pressed by the harness.
- Icons/label layout by eye; tooltip text.
