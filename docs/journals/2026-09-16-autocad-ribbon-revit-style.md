# AutoCAD Ribbon Reduced to the Revit-Style Surface — One Button, One Vector Icon, Ten Buttons and Five Entry Points Gone

**Date**: 2026-09-16 11:40  
**Severity**: Low (deliberate simplification; no defect)  
**Component**: HPAutoCad.McpBridge.Loader / HPAutoCad.McpBridge / tools/harness  
**Status**: Verified live (build 0 warnings; `run-ribbon-check.ps1` 12/12 + icon MANUAL inspected on both themes; bridge 21/21; smoke 22/22)

## Tình huống

Two days after the ten-button "MCP AutoCAD" tab shipped (bundle 0.2.0), the Navisworks bridge got its Ribbon
in the Revit style — tab named after the product, one panel "MCP", one button "MCP Bridge" — and the user asked
for the AutoCAD tab to match, "with a sharp, clear, function-true, vector icon". The wording was ambiguous
(sharpen the ten icons, or collapse to one button?), and the two readings differ by a factor of ten in work and
reverse a decision the user made on 2026-09-14, so one question was asked; the answer was the Revit-style minimum.

## Tổng quan

- `Ribbon/McpRibbonTab.cs`: tab `HPAutoCad` (id `HPAUTOCAD_MCP_TAB` kept — the harness finds it by id) ▸ panel
  `MCP` ▸ button `MCP Bridge` → `BridgeActions.Run("show")`. The re-creation logic (workspace switch,
  COLORTHEME, `ItemInitialized`, `FindTab` guard, `_building` re-entrancy) is unchanged; tooltip `Command` is
  `HPMCPBRIDGE`; when the bridge did not start the button is disabled and the tooltip names the loader log
  (the "open the log" button it used to point at no longer exists).
- `Ribbon/RibbonIcons.cs`: one frozen `DrawingImage` — the window-with-plug glyph `HPNavis/tools/icons/
  render-ribbon-icons.ps1` rasterises for Navisworks, kept vector here because AdWindows takes an `ImageSource`.
  Every coordinate is even so the 16-px small image is an exact half of the 32-px large one; ink `#E6E6E6` on
  COLORTHEME 0, `#3C3C3C` on 1, plug `#0696D7` (the Navis accent, replacing the loader's own `#2F86E0`).
- Removed: `RibbonStatusPresenter.cs`, `BridgeEntry.Ribbon.cs` (entry points `status.subscribe`,
  `copyLastScript`, `autoStart.get/set`, `path` — grep proved the old ribbon was their only caller),
  `BridgeActions.Query<T>/OpenPath`, and the README copy into the bundle (`_Guide`, only the "Hướng dẫn" button
  read it). The bridge dictionary is back to `show/start/stop/status/dispose`.
- Bundle 0.2.0 → 0.3.0 in both csproj files and `PackageContents.xml` (AppVersion + ComponentEntry).
- `run-ribbon-check.ps1` rewritten: tab once → workspace round trip → **COLORTHEME round trip** (new; proves
  the theme rebuild path and yields a screenshot per theme) → button found → 0 windows → click → 1 window →
  second click → still 1 → loader.log clean. The icon stays a MANUAL item (exit 2) because UIA cannot read pixels.

## Verification

| Check | Result |
|---|---|
| `dotnet build HPAutoCad.slnx -c Debug` | 0 warnings, bundle deployed |
| `run-ribbon-check.ps1` | 12/12 PASS + 1 MANUAL; both screenshots zoomed ×6: frame + plug pixel-exact, ink follows the theme (run 3, after the review fixes; runs 1–2: 9/9 and 10/10 before the theme checks were tightened) |
| Code review | 8.5/10 APPROVE, 0 critical/high; M1–M3 + L1–L7 fixed the same hour: the COLORTHEME check now reads the value back and counts one loader.log `created` line per flip (3 → 4 → 5), the harness restores WSCURRENT/COLORTHEME in `finally`, window counts are evaluated once per check, the `[int16]` COM cast is explained, stale plural comments and a vestigial `partial` removed, `Tabs.Remove` in `OnIdle` guarded |
| `run-bridge-unattended.ps1` | 21/21 (1 + 18 + 1 + 1) |
| `run-server-smoke.ps1` | 22/22 |

## Lessons

- **Ambiguity that flips a prior user decision is worth one question.** "Giống Revit" had a precedent in this
  repo (Navis, the same morning) but also a plausible cheaper reading; a wrong guess would have cost a full
  live cycle or silently reverted the 09-14 decision. One `AskUserQuestion` with the recommendation first
  settled it in seconds.
- **COLORTHEME switches through `ActiveDocument.SetVariable` with an `[int16]`** like WSCURRENT does — the same
  COM route that never returns for `SendCommand`. Adding the theme round trip to the harness cost 15 s per run
  and turned an unverified claim ("icons follow the theme", stated since 09-14) into a PASS with evidence.
- **A stale file survives a deploy target that only copies.** `Contents\README.md` from 0.2.0 stayed in the
  bundle after the copy step was removed; `DeployBundle` only wipes `Contents\Bridge`. Deleted by hand this
  time; a fresh install never sees it.
- **Deleting a feature is mostly deleting its evidence trail**: docs (CLAUDE.md, README ×2, codebase summary,
  architecture, changelog) carried the ten-button table in five places. The 09-14 journal stays as history.
