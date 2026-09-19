---
phase: 5
title: "HPNavis (net48, repacked toolkit)"
status: pending
priority: P3
effort: "8h"
dependencies: [0, 1]
---

# Phase 5: Navisworks bridge

## Overview
The one .NET Framework host: `net48` plugin consuming the toolkit's `net462` asset. Navisworks exposes no theme API (`NavisTheme.xaml:4`), so the window follows Windows `AppsUseLightTheme` through `WindowsHostTheme`. Load path = **S0-C verdict: repacked** ([spike-s0c-navis-resolver.md](reports/spike-s0c-navis-resolver.md): loose also bound correctly beside a foreign 4.9.0 because Roamer probes the plugin folder itself and BAML references the strong name — the allow-list was never asked — but the repack removes the simple name from the process for symmetry with the other hosts). **`PluginAssemblyResolver` is not modified.**

## Requirements
- Functional: `NavisBridgeStatusView` renders with MaterialDesign, base theme from Windows (live via `SystemEvents.UserPreferenceChanged`), heavy-ops checkbox + status dot unchanged in behaviour.
- Non-functional: `net462` assets copied to the plugin folder by the existing `DeployPlugin` target (`.csproj:64-71`, whole output); foreign `NavisworksMCPPlugin` untouched; `-p:DeployPlugin=false` while Roamer is open; `HPRebar.McpBridge.Core` net48 asset untouched.

## Architecture
`PackageReference MaterialDesignThemes 5.3.2` (net462 asset + `Microsoft.Xaml.Behaviors 1.1.77` net462 + `MaterialDesignColors`) + `RepackMaterialDesign` target (net48 ILRepack, `HPNavis.McpBridge.dll` ≈ 10.9 MB, no loose toolkit DLL in the plugin folder) + `ThemeInfo`; `NavisBridgeStatusView.xaml` merges `CustomColorTheme` → Defaults → `MaterialBridge.xaml`; code-behind `MaterialThemeBridge.Attach(this, WindowsHostTheme.Instance)` (net48 build of the phase-1 helper — `#if NET48` only if a BCL gap appears; none expected: `ResourceDictionary`, `SystemEvents` exist on 4.8). `xmlns:sys` stays `mscorlib` (`NavisTheme.xaml:9`).

## Related Code Files
- Modify: `HPNavis/HPNavis.McpBridge/HPNavis.McpBridge.csproj` (package + repack target), `View/NavisBridgeStatusView.xaml(.cs)`.
- Create: `Resources/Themes/MaterialBridge.xaml`, `Service/MaterialThemeBridge.cs`, `Service/WindowsHostTheme.cs`.
- Delete: `Resources/Themes/NavisTheme.xaml` at the end.
- Tests: `HPNavis/HPNavis.McpBridge.Tests` (135, net48; `RibbonPluginTests` pin attributes ⇔ XAML ids ⇔ `.name` keys ⇔ PNG sizes — ribbon untouched) + one test that the deployed output holds no `MaterialDesignThemes.Wpf.dll` and `HPNavis.McpBridge.dll` carries `ThemeInfo(SourceAssembly)`; `HPNavis.Mcp.Server.Tests` (49) unchanged.

## Implementation Steps
1. Package + repack target + ThemeInfo + view + helper; `dotnet build HPNavis/HPNavis.slnx -c Debug -p:DeployPlugin=false`; `dotnet test HPNavis/HPNavis.McpBridge.Tests` (136) and `HPNavis.Mcp.Server.Tests` (49) — both need the installed Navisworks API, not a running Roamer.
2. Roamer closed → deploy; confirm the plugin folder holds the three toolkit DLLs + `en-US\` + `Images\`.
3. `pwsh HPNavis/tools/harness/run-ribbon-check.ps1 -WithNoDoc` (14 + 1 MANUAL: tab once, no window before click, click opens, second click activates, no-model start page greys the tab); `pwsh HPNavis/tools/harness/run-bridge-unattended.ps1` (43 ×2 + no-doc); `run-server-smoke.ps1` (9).
4. `%LocalAppData%\HPNavis\McpBridge\logs\`: `MCP scripting self-check OK` present; no `MaterialDesignThemes.Wpf` in the AppDomain from our side (S0-C `MD-SPIKE` style check: every toolkit type reports `HPNavis.McpBridge`) — record.
5. Flip Windows app mode (Settings ▸ Personalization ▸ Colors) with the window open → base theme follows; record a screenshot pair.
6. Delete `NavisTheme.xaml`; rebuild; rerun step 3.

## Success Criteria
- [ ] Tests 136/136 + 49/49; ribbon check 14 PASS + 1 MANUAL; bridge unattended 43×2 + no-doc; smoke 9/9.
- [ ] No loose toolkit DLL in the plugin folder, `ThemeInfo(SourceAssembly)` on the deployed DLL; foreign plugin still loads silently (Roamer start log has no error).
- [ ] Window follows Windows light/dark live; screenshots in `reports/phase-05-navisworks.md`.
- [ ] `NavisTheme.xaml` deleted; no `StaticResource` brush in the view.

## Risk Assessment
- A foreign plugin with **the same** 5.3.2 loaded first from another folder was not measured (S0-C used 4.9.0); under the repack our BAML references `HPNavis.McpBridge` only, so it cannot matter.
- net48 ILRepack needs `/lib:` for the Navisworks API directory — the target derives it from `ReferencePath`, which already includes `$(NavisworksInstallDir)` (verified in the spike build).
- `SystemEvents.UserPreferenceChanged` fires on a non-UI thread → marshal with the dispatcher captured in `BridgeEntry.Start` (`_mainDispatcher`, `BridgeEntry.cs:63`).

## Next Steps
Plan complete → `docs/codebase-summary.md` + `CLAUDE.md` (per-host "theme" facts, the `AGENTS.md` regen one-liner) + `docs/project-changelog.md`; then `/bs:plan archive`.
