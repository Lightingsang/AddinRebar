# PM status — AutoCAD Ribbon tab "MCP AutoCAD" (2026-09-14)

## Plan sync-back
| Phase | Todos | Status |
|---|---|---|
| 1 — Ribbon tab in the loader + bridge entry points | 4/4 | done |
| 2 — harness, deploy, regression, docs | 4/4 | done |

`plan.md`: 2/2 done; `status: completed`.

## Evidence
- Build: `HPAutoCad.slnx` Debug + Release 0 warnings; `HPAutoCad.Mcp.Server.Tests` 58/58 (loader/bridge have no unit tests — the tab is UI, verified live).
- Live (`reports/ribbon-live-check.md`, `run-ribbon-check.ps1` run 4): **9/9 PASS, 0 MANUAL** — tab created, exactly one, still one after a workspace round trip (re-created via `SystemVariableChanged → Idle`), Bật listener → pipe up, Tắt → down, Bảng điều khiển → window, Trạng thái clean, no failure/exception in loader.log for the whole run; `ribbon-tab.png` captured.
- Regression: `run-bridge-unattended.ps1` 21/21, `run-server-smoke.ps1` 22/22 (Claude Code's stdio flow) with the new loader/bridge.
- Review 8/10, no critical/high → 14 findings fixed (`reports/ribbon-code-review.md` §Resolution), re-verified run 4.
- Parity: nothing under `McpShared/`, `HPRebar/`, `HPAutoCad.Mcp.Server/`; bundle carries no AutoCAD DLL; the four `HPMCP*` commands unchanged (now via `BridgeActions`).

## Commits
`e5fae0a` feat (tab + 4 bridge entry points + harness), `0e28631` fix (14 review findings), docs commit next.

## Decisions
- Ribbon lives in the loader (default ALC, where AdWindows loads); buttons call the same bridge delegates as the commands; entry points cross the ALC as BCL types only.
- Not on the Ribbon: the AI-code-execution opt-in (window only, off each session) and any run-a-tool button (tools run through the MCP server Claude Code owns).
- Icons are vector `DrawingImage`s picked per COLORTHEME; no CUIx, no change to acad.cuix or the user's workspaces.

## User checklist (not scripted)
Start-menu launch shows the tab; a click while a command waits for input; the clipboard/Explorer/settings/guide buttons by hand; icons and Vietnamese labels/tooltips by eye; on this dev machine an unrelated `AutoCadMcp.bundle` adds look-alike tabs "AutoCAD MCP"/"Civil 3D MCP" (different ids).

## Next
Plan complete. `.mcp.json` `hprebar-autocad` and republishing the Revit server exe remain the user's (unchanged from the bridge plan).
