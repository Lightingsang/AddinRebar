# Ribbon "HPNavis" ▸ "MCP" ▸ "MCP Bridge" — live check report

**Date:** 2026-09-16 (final run 00:27 after the code review) · **Host:** Navisworks Manage 2026 (23.0.1432.76), `Samples\gatehouse\gatehouse_pub.nwd`, dev machine 96 DPI · **Plugin build:** `dotnet build HPNavis/HPNavis.slnx -c Debug` (0 warnings) deployed to `%AppData%\Autodesk\Navisworks Manage 2026\Plugins\HPNavis.McpBridge\` with `en-US\HPNavisRibbon.{xaml,name}` + `Images\McpBridge_{16,32}.png` · **Runner:** `powershell.exe -File HPNavis/tools/harness/run-ribbon-check.ps1 -WithNoDoc` · **Raw:** `HPNavis/output/ribbon-check/` (copied here: `ribbon-model.png`, `ribbon-nodoc.png`, `ribbon-check-summary.json`).

## Result: 14 PASS, 0 FAIL, 1 MANUAL (icon — inspected, crisp)

| # | Check | Result |
|---|---|---|
| 1–4 | deployed `en-US\HPNavisRibbon.xaml`, `.name`, `Images\McpBridge_16.png`, `_32.png` | PASS |
| 5 | model: tab `HPNavis` visible exactly once (UIA `Button`, AutomationId `ID_HPNAVIS`) | PASS |
| 6 | model: icon crisp, reads as a window with a plug | **MANUAL** — `ribbon-model.png`: tab selected, panel `MCP`, button `MCP Bridge`, 32-px glyph pixel-exact (ink frame + blue plug) |
| 7 | model: no bridge window before the click (Roamer started without `HPNAVIS_MCP_BRIDGE_SHOW_WINDOW`) | PASS |
| 8 | model: button `MCP Bridge` enabled | PASS |
| 9 | model: click opens the bridge window (`Find-BridgeWindow`) | PASS |
| 10 | model: log line `MCP bridge status window opened` since start | PASS ×1 |
| 11 | model: second click activates, one window | PASS (windows: 1) |
| 12 | model: no `HPNavis MCP` Add-ins entry — Navisworks shows a "Tool add-ins" tab only while some AddInPlugin is visible; with ours `AddInLocation.None` that tab is gone (if another plugin brings it back, the harness selects it and asserts our button is not on it) | PASS (no Add-ins tab at all) |
| 13 | model: no `[ERR]`/`[FTL]` in the bridge log | PASS ×0 |
| 14 | nodoc: tab header visible exactly once | PASS |
| 15 | nodoc: header **disabled** — Navisworks shows its start page ("Recent") and greys every Ribbon tab, ours included (`ribbon-nodoc.png`) | PASS (documented host behaviour; the window still opens through the env variable / `ExecuteAddInPlugin` — live-verify `nodoc` phase) |

Earlier runs of the same script (same plugin build, harness fixes only): run 1 model 9/9 PASS but nodoc selection hit a stale UIA element; run 2 `SelectionItemPattern.Select()` returned without error yet did not display the tab and the screenshot caught another application on top → harness now brings Roamer to the foreground, tries Invoke → SelectionItem and accepts a selection only when `MCP Bridge` comes on screen (`Wait-RibbonButtonVisible`); the no-document case is asserted as "header present, disabled" instead of a click.

## Regression (unchanged behaviour)

- `run-live-verify.ps1 -WithNoDoc -IncludeIsolation -Tag ribbon` → **62 pass, 0 skip, 0 fail, 170 s** (env-var window, pipe, opt-ins, registry loop, modal, heavy, no-doc, isolation).
- `run-bridge-unattended.ps1 -Runs 1` → PASS (43 checks; S-09 pipe gone, 0 error lines).
- Tests: `HPNavis.McpBridge.Tests` **135** (124 + 11 `RibbonPluginTests`), `HPNavis.Mcp.Server.Tests` 49 untouched (no server change).
- `git diff --stat McpShared/ HPRebar/ HPAutoCad/` empty.

## Facts pinned for future Ribbon work

- Header = UIA `Button` with `AutomationId` = tab id (same as AdWindows in AutoCAD); the tab's buttons enter the UIA tree only while the tab is displayed; `InvokePattern` on the header switches tabs, `SelectionItemPattern.Select()` reports success without switching; right after start-up Invoke may answer "Unrecognized error" for a few seconds.
- With no document Navisworks greys all tabs (start page) — not a plugin state; `CallCanExecute.Always` only matters once a document is open.
- Icon files: attribute `Icon`/`LargeIcon` = bare file names in `Images\`; XAML `Image`/`LargeImage` = paths relative to the XAML (`..\Images/…`). PNG RGBA accepted (foreign plugin ships 38-px large icons; 32 is the documented size).
- `UseWPF` globs every `*.xaml` as `Page`: the Navisworks layout must be `<Page Remove>`d and shipped as `None` with `Link`, or the culture folder ends up empty and the tab never appears.
