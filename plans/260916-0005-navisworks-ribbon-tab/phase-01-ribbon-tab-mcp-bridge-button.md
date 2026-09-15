---
phase: 1
title: "Ribbon tab HPNavis ▸ MCP ▸ MCP Bridge: plugin, layout, icon, tests, harness, docs"
status: completed
priority: P2
effort: "3h"
dependencies: []
---

# Phase 1: Ribbon tab "HPNavis" ▸ panel "MCP" ▸ button "MCP Bridge"

## Overview
One `CommandHandlerPlugin` puts the bridge window behind a Ribbon button, mirroring the Revit MCP ribbon (product tab / `MCP` panel / `MCP Bridge`), with a purpose-drawn, pixel-exact icon. The Add-ins entry is hidden. Verified live in Navisworks Manage 2026.

## Requirements
- Functional: tab `HPNavis` exactly once; button `MCP Bridge` (English label + tooltip) opens or activates `NavisBridgeStatusView`; no duplicate Add-ins entry; nothing else changes (pipe, opt-ins, env-var window, harnesses).
- Non-functional: no reference outside `../McpShared/*` + the installed Navisworks API; no `McpShared/`/`HPRebar/`/`HPAutoCad/` change; icon crisp at 100 % DPI; tests pin attributes ⇔ XAML ⇔ strings ⇔ PNG.

## Architecture
```
Roamer.exe ── reflects plugin types ──▶ HPNavisRibbonPlugin [Plugin("HPNavis.McpBridge.Ribbon","HPNV")]
   ├─ [RibbonLayout("HPNavisRibbon.xaml")] → en-US\HPNavisRibbon.xaml  (RibbonTab ID_HPNAVIS ▸ RibbonPanelSource "MCP" ▸ NWRibbonButton ID_HPNAVIS_MCP_BRIDGE, Image ..\Images/McpBridge_16.png, LargeImage ..\Images/McpBridge_32.png)
   ├─ [Strings("HPNavisRibbon.name")]     → en-US\HPNavisRibbon.name  (DisplayName / ToolTip / ExtendedToolTip per id)
   ├─ [RibbonTab("ID_HPNAVIS", DisplayName="HPNavis")]
   └─ [Command("ID_HPNAVIS_MCP_BRIDGE", DisplayName="MCP Bridge", Icon="McpBridge_16.png", LargeIcon="McpBridge_32.png", CallCanExecute=Always)]
        ExecuteCommand → BridgeEntry.ShowWindow() (activates when already open) → 0 / 1 + Serilog on failure
HPNavisWindowPlugin [AddInPlugin(AddInLocation.None)]  — programmatic ExecuteAddInPlugin only
csproj: <Page Remove="Ribbon\**\*.xaml"/> + <None Include="Ribbon\en-US\*" Link="en-US\..."/> + <None Include="Ribbon\Images\*.png" Link="Images\..."/>  → DeployPlugin copies en-US\ + Images\
```

## Related Code Files
- Create: `HPNavis/HPNavis.McpBridge/HPNavisRibbonPlugin.cs`, `Ribbon/en-US/HPNavisRibbon.xaml`, `Ribbon/en-US/HPNavisRibbon.name`, `Ribbon/Images/McpBridge_16.png`, `Ribbon/Images/McpBridge_32.png`, `HPNavis/tools/icons/render-ribbon-icons.ps1`, `HPNavis/HPNavis.McpBridge.Tests/RibbonPluginTests.cs`, `HPNavis/tools/harness/run-ribbon-check.ps1`.
- Modify: `HPNavis.McpBridge.csproj`, `HPNavisWindowPlugin.cs`, `HPNavis.McpBridge.Tests.csproj` (reference `Autodesk.Navisworks.Api`, Private=false), `tools/harness/harness-common.ps1` (`Start-NavisworksWithModel -showWindow`, Ribbon UIA helpers), `CLAUDE.md` (+ `AGENTS.md` regen), `HPNavis/README.md`, `tools/harness/README.md`, `docs/codebase-summary.md`, `docs/project-changelog.md`.

## Implementation Steps
1. Glyph script → PNG 16/32 (aliased, even coordinates); inspect 8× preview.
2. Layout XAML (header from the SDK example) + `.name`; plugin class; `AddInLocation.None`; csproj items.
3. `dotnet build -p:DeployPlugin=false` → verify `bin\…\en-US\*.xaml` verbatim + `Images\*.png`.
4. Tests (11) → 135 green.
5. Deploy (Navisworks closed) → `run-ribbon-check.ps1 -WithNoDoc` until 0 FAIL / only the icon MANUAL.
6. Regression `run-live-verify.ps1 -Tag ribbon` + `run-bridge-unattended.ps1 -Runs 1`.
7. Docs, plan, report, review, commit.

## Todo List
- [x] Icon script + PNGs (`McpBridge_16.png` 203 B, `McpBridge_32.png` 240 B, RGBA)
- [x] Plugin class + layout + strings + csproj
- [x] Tests 135/135 (`RibbonPluginTests` 11)
- [x] Deploy + `run-ribbon-check.ps1 -WithNoDoc` → 14 PASS, 0 FAIL, 1 MANUAL (icon screenshot inspected: crisp)
- [x] Regression: `run-live-verify.ps1 -WithNoDoc -IncludeIsolation -Tag ribbon` 62 pass / 170 s; `run-bridge-unattended.ps1 -Runs 1` PASS (43 checks)
- [x] Docs + `AGENTS.md` regen + changelog
- [x] Report `reports/ribbon-live-check.md`

## Success Criteria
- [x] Tab `HPNavis` exactly once; `MCP Bridge` opens the window from a Roamer started **without** `HPNAVIS_MCP_BRIDGE_SHOW_WINDOW`; second click → one window; log line `MCP bridge status window opened`.
- [x] No `HPNavis MCP` Add-ins entry; no ERR/FTL in the bridge log.
- [x] `git diff --stat McpShared/ HPRebar/ HPAutoCad/` empty.

## Risk Assessment
See `plan.md` → "Rủi ro → kết quả". Open: high-DPI sharpness (PNG scaled by AdWindows) — upgrade path is a vector `DrawingImage` swap via `ComponentManager.Ribbon` [chưa xác minh].
