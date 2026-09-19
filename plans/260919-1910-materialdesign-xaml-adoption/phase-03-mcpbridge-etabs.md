---
phase: 3
title: "HPRebar.McpBridge (Revit) + HPEtabs (standalone app)"
status: completed
priority: P2
effort: "6h"
dependencies: [1, 2]
---

# Phase 3: Revit MCP bridge window + ETABS bridge app

> **Completed 2026-09-19 — ETABS verified by screenshot (Debug + publish folder), Revit bridge verified live in Revit 2026 (UIA-opened window + MCP round trip through the MD opt-in checkbox)** — [reports/phase-03-mcpbridge-etabs.md](reports/phase-03-mcpbridge-etabs.md).

## Overview
Two one-window surfaces. The Revit bridge reuses HPRebar's dictionaries by link (`HPRebar.McpBridge.csproj:49-51`) and runs inside Revit 2025+'s per-add-in ALC with Roslyn as loose DLLs (`IsRepackable=false`, `Application.cs:21-22`); only the toolkit gets repacked into `HPRebar.McpBridge.dll`. HPEtabs is the only real WPF application (`App.xaml`) — textbook toolkit setup, `PaletteHelper` allowed.

## Requirements
- Functional: both status windows render with MaterialDesign, follow their theme source (Revit `UIThemeManager`; Windows `AppsUseLightTheme` with a `settings.json` override for ETABS), keep the `StatusDot` / `CompileCountText` / `SelfCheckText` local styles working.
- Non-functional: `McpShared` untouched (`McpBridgeStatusViewModel` stays WPF-free); ETABS publish stays a **folder** (`PublishSingleFile=false`, `HPEtabs.McpBridge.csproj:19`) — the toolkit DLLs land beside the exe; Revit bridge builds with `-p:DeployAddin=false` while Revit is open.

## Architecture
- Revit bridge: `PackageReference MaterialDesignThemes 5.3.2` **+ the `RepackMaterialDesign` target** (S0-B: two loose copies in two load contexts of one process mix — another Revit add-in shipping the toolkit would be that second copy; `IsRepackable=false` stays for Roslyn, the toolkit-only target is independent) + `ThemeInfo`; `View/McpBridgeStatusView.xaml` merges `CustomColorTheme` → Defaults → linked `MaterialBridge.xaml`; code-behind `MaterialThemeBridge.Attach(this, RevitHostTheme.Instance)` — the bridge's `Application.cs:90-93` already subscribes `ThemeChanged` for its icon, one added line raises `RevitHostTheme.Changed`. Delete `Service/ThemeSwitcher.cs` (4th copy) and the bridge's own `Resources/Themes/Theme.xaml` (17 lines) once the linked set covers it.
- ETABS: loose DLLs beside the exe (its own process, one copy, nothing else loads a toolkit) — no repack, no `ThemeInfo` needed; `App.xaml` merges `CustomColorTheme` → `MaterialDesign2.Defaults.xaml` → `Resources/Themes/MaterialBridge.xaml` (bridge vocabulary) in `Application.Resources` (the window has no merged dictionaries today, `EtabsBridgeStatusView.xaml:19-51`); `WindowsHostTheme` reads `Theme.GetSystemTheme()` and `SystemEvents.UserPreferenceChanged`, `settings.json` gains `"Theme": "system" | "light" | "dark"` (`BridgeSettings` in the bridge, not in Core); `PaletteHelper.SetTheme` is fine here (`Application.Current` exists). `EtabsTheme.xaml` deleted at the end.

## Related Code Files
- Modify: `HPRebar/HPRebar.McpBridge/HPRebar.McpBridge.csproj` (package), `View/McpBridgeStatusView.xaml(.cs)`, `Application.cs:90-93` (+1 line); `HPEtabs/HPEtabs.McpBridge/HPEtabs.McpBridge.csproj` (package), `App.xaml`, `App.xaml.cs` (apply theme at startup, subscribe), `View/EtabsBridgeStatusView.xaml` (style keys), `Service/…Settings` (theme key).
- Create: `HPEtabs/HPEtabs.McpBridge/Resources/Themes/MaterialBridge.xaml`, `Service/MaterialThemeBridge.cs`, `Service/WindowsHostTheme.cs` (phase-1 copies).
- Delete: `HPRebar/HPRebar.McpBridge/Service/ThemeSwitcher.cs`, `HPRebar/HPRebar.McpBridge/Resources/Themes/Theme.xaml`; `HPEtabs/HPEtabs.McpBridge/Resources/Themes/EtabsTheme.xaml`.
- Tests: `HPEtabs/HPEtabs.McpBridge.Tests` (184, needs ETABS installed) — add one test for the settings `Theme` key parsing; `HPRebar.Mcp.Server.Tests` (109) unaffected.

## Implementation Steps
1. Revit bridge: package, view merge, `Attach`, delete `ThemeSwitcher`; `dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false`; then with Revit closed deploy; open **HPRebar ▸ MCP ▸ MCP Bridge**; check `MCP scripting self-check OK` still logged (Roslyn untouched) and the window on both Revit themes; "publisher could not be verified" → *Always Load* (known).
2. Run the MCP smoke used before: `.mcp.json` server `hprebar-revit` → `get_revit_context` answers; opt-in checkbox toggles; `cancel_execution` path unaffected (window-only change).
3. ETABS: package, `App.xaml`, `WindowsHostTheme`, settings key; `dotnet build HPEtabs/HPEtabs.slnx -c Debug`; `dotnet test HPEtabs/HPEtabs.McpBridge.Tests` (184 + 1) and `HPEtabs.Mcp.Server.Tests` (81).
4. `pwsh HPEtabs/tools/harness/run-live-verify.ps1 -Phase bridge -Runs 1` (UIA scoped to our window by pid — the harness finds controls by AutomationId/name; MaterialDesign templates keep `AutomationProperties.Name` = `HintAssist.Hint` since 5.3.0, but **check every UIA locator the harness uses** (`spike-step.ps1`, `live-verify.py`) against the restyled window before trusting a PASS).
5. `dotnet publish HPEtabs/HPEtabs.McpBridge -c Release -r win-x64 -p:SelfContained=false -o HPEtabs/output/HPEtabs.McpBridge` → toolkit DLLs present in the folder; `-Phase bridge -Publish` once.

## Success Criteria
- [x] Revit bridge window opens (dark theme verified; light/live switch not driven), `get_revit_context` + `execute_revit_code` via MCP work; log has `MCP scripting self-check OK`.
- [x] HPEtabs 184 + 81 tests pass; window screenshots dark/light from Debug and the publish folder; `run-live-verify.ps1 -Phase bridge` **48/48** on the user's test model (UIA locators unchanged).
- [x] Both windows: no `StaticResource` for a brush; code-behind = one `Attach` call.

## Risk Assessment
- ETABS harness UIA locators may target Aero2 control names (e.g. the opt-in `CheckBox` found by `Name`) — MaterialDesign's `CheckBox` template keeps the content as `Name`; verify before the run, fix the locator (harness change, allowed) if not.
- The Revit bridge's linked `Page` glob picks up HPRebar's `MaterialBridge.xaml` automatically — its pack URIs must be relative-to-assembly (`/…;component/`) or written with `HPRebar.McpBridge` in mind; phase 1 uses **relative** URIs in the shared file for that reason.

## Next Steps
Phase 4 (AutoCAD family) — independent of this phase, may run before it.
