---
phase: 4
title: "AutoCAD family — HPAutoCad → HPCivil3d (mirror + re-pin) → HPGeo (dark ComboBox fix)"
status: pending
priority: P2
effort: "12h"
dependencies: [0, 1]
---

# Phase 4: AutoCAD family

## Overview
Three isolated-ALC surfaces on the same `acad.exe` family. HPAutoCad first (one status window), ported verbatim to HPCivil3d with the mirror test re-pinned, then HPGeo's two modal dialogs where the toolkit's `MaterialDesignComboBox` closes the documented dark-theme gap (`HPGeo/HPGeo.AutoCad/UI/Theme.xaml:44-48` has no template/brushes). Load path = **S0-B verdict: repacked** into each add-in assembly (`RepackMaterialDesign` target, [spike-s0b-two-alc.md](reports/spike-s0b-two-alc.md)) — loose DLLs in two ALCs mixed (HPGeo's window bound to the bridge's copy in order B); repacked 8/8 in both orders with the HPGeo dark popup already MaterialDesign-dark.

## Requirements
- Functional: bridge window follows `COLORTHEME` **live** (`SystemVariableChanged` → `Application.Idle`, replacing the add-only overlay of `AutocadThemeSwitcher.cs:17-29`); HPGeo dialogs pick the theme at open (modal); `CrsSelectionView` editable `ComboBox` + `GeoImportWindow` pair-order `ComboBox` render dark popups on `COLORTHEME 0`.
- Non-functional: loaders unchanged (no NuGet; `Ac/Ad/Autodesk.` prefixes still refused by the ALC); ribbon stays one shared `HPAutoCad` tab + vector icons; builds with `-p:DeployBundle=false` while AutoCAD is open; `HPCivil3d/tools/mirror-tokens.json` updated so `MirrorTests` (55) pass; `BridgeEntry.CompilerReferences` **not** extended (scripts never see the toolkit).

## Architecture
- HPAutoCad bridge: `PackageReference MaterialDesignThemes 5.3.2` + `RepackMaterialDesign` target (merged into `HPAutoCad.McpBridge.dll`, loose toolkit DLLs deleted, `deps.json` entry harmless) + `ThemeInfo`; `AutocadBridgeStatusView.xaml` merges `CustomColorTheme` → Defaults → `MaterialBridge.xaml` (bridge vocabulary), `MaterialDesignWindow`; code-behind keeps the `EnterContextualReflection` scope around `InitializeComponent()` + `MaterialThemeBridge.Attach(this, AutocadHostTheme.Instance)`. `AutocadHostTheme` subscribes `Application.SystemVariableChanged` in `BridgeEntry.Start` and unsubscribes in `Dispose` (window-independent, so a window opened later sees the current value). Delete `AutocadThemeSwitcher.cs`, `AutocadThemeLight.xaml`; `AutocadTheme.xaml` shrinks to what `MaterialBridge.xaml` does not cover, then goes.
- HPCivil3d: every mirrored file re-copied with the 50 tokens (`AutocadBridgeStatusView` → `Civil3dBridgeStatusView`, `AutocadTheme` → `Civil3dTheme`, …); new files (`MaterialBridge.xaml`, `MaterialThemeBridge.cs`, `AutocadHostTheme.cs` → `Civil3dHostTheme.cs`) added to `mirroredFiles`; `HPAutoCad.McpBridge.csproj` and `BridgeEntry.cs` sha256 re-pinned in `ownedCounterparts` (`mirror-tokens.json:326-340`) after the Civil counterparts are ported by hand.
- HPGeo: package in `HPGeo.AutoCad.csproj`; `ThemeResources.cs` returns `CustomColorTheme` + Defaults + `MaterialBridge.xaml` instead of `ThemeDark/Light + Theme.xaml`; `GeoExportWindow.xaml.cs` / `GeoImportWindow.xaml.cs` keep their shape (merge → `InitializeComponent` inside the ALC scope → `MapView.DarkTheme = dark`); the implicit `ComboBox` style is removed (Defaults' `MaterialDesignComboBox` applies, editable variant included), `DataGrid` styles `BasedOn MaterialDesignDataGrid*`; `IsDarkTheme()` duplicated in both windows → `HPGeoHostTheme`. WebView2 map untouched.

## Related Code Files
- Modify: `HPAutoCad/HPAutoCad.McpBridge/HPAutoCad.McpBridge.csproj`, `View/AutocadBridgeStatusView.xaml(.cs)`, `BridgeEntry.cs` (subscribe/unsubscribe host theme; `ShowWindow` unchanged); `HPCivil3d/HPCivil3d.McpBridge/…` counterparts + `HPCivil3d/tools/mirror-tokens.json`; `HPGeo/HPGeo.AutoCad/HPGeo.AutoCad.csproj`, `UI/ThemeResources.cs`, `UI/Theme.xaml`, `UI/GeoExportWindow.xaml(.cs)`, `UI/GeoImportWindow.xaml(.cs)`, `UI/CrsSelectionView.xaml`.
- Create: `HPAutoCad/HPAutoCad.McpBridge/Resources/Themes/MaterialBridge.xaml`, `Service/MaterialThemeBridge.cs`, `Service/AutocadHostTheme.cs` (+ Civil mirrors); `HPGeo/HPGeo.AutoCad/UI/MaterialBridge.xaml`, `UI/MaterialThemeBridge.cs`, `UI/HPGeoHostTheme.cs`.
- Delete: `Service/AutocadThemeSwitcher.cs`, `Resources/Themes/AutocadThemeLight.xaml`, `AutocadTheme.xaml` (+ Civil mirrors); `HPGeo/HPGeo.AutoCad/UI/ThemeDark.xaml`, `UI/ThemeLight.xaml`.
- Repack (decided): `RepackMaterialDesign` target in the two bridge csproj + HPGeo csproj (ILRepack 2.0.46 `PrivateAssets=all ExcludeAssets=all GeneratePathProperty`, inputs = obj DLL + the three toolkit DLLs, no `/internalize`) — Roslyn, `HPAutoCad.Aec`, WebView2 stay loose; `DeployBundle` copies fewer DLLs; the target text lives in both sha-pinned csproj, so `mirror-tokens.json` is re-pinned once.

## Implementation Steps
1. HPAutoCad: package, view, host theme, deletions; `dotnet build HPAutoCad/HPAutoCad.slnx -c Debug -p:DeployBundle=false`; `dotnet test HPAutoCad/HPAutoCad.Mcp.Server.Tests` (280) + `HPAutoCad.Aec.Tests` (225) unchanged; with AutoCAD closed deploy; `pwsh HPAutoCad/tools/harness/run-ribbon-check.ps1` (12 + 1 MANUAL: tab once, window opens, second click activates, COLORTHEME flip — now also proves the **live** re-theme: add one check reading the window background brush after the flip); `run-bridge-unattended.ps1` (21) and `run-server-smoke.ps1` (22).
2. HPCivil3d: port + tokens + new mirrored entries + re-pin; `dotnet test HPCivil3d/HPCivil3d.McpBridge.Tests` (55, incl. the one-character mutation checks) + `HPCivil3d.Mcp.Server.Tests` (106); `dotnet build HPCivil3d/HPCivil3d.slnx -c Debug -p:DeployBundle=false`; deploy with Civil 3D closed (SECURELOAD ×4 + the toolkit DLLs are signed by the vendor — count the prompts, record); `pwsh HPCivil3d/tools/harness/run-ribbon-check.ps1`.
3. HPGeo: package, `ThemeResources`, dialogs, ComboBox/DataGrid styles; `dotnet build HPGeo/HPGeo.slnx -c Debug -p:DeployBundle=false`; `dotnet test HPGeo/HPGeo.Tests` (154); deploy with AutoCAD closed; `pwsh HPGeo/tools/dialog-check.ps1` (HPGEO on both COLORTHEMEs + HPGEOIMPORT, `PrintWindow` screenshots — inspect the ComboBox popup on dark) and `pwsh HPGeo/tools/acceptance.ps1` (51 checks, KMZ/import maths untouched).
4. Co-load check (S0-B in production form): AutoCAD 2026 with both bundles, `HPMCPBRIDGE` + `HPGEO` in both orders, loader logs clean.
5. Isolation regression: `pwsh HPAutoCad/tools/harness/run-live-verify.ps1 -OnlyIsolation` (4/4: second AutoCAD, Civil 3D never loads the AutoCAD bundle) and `HPCivil3d … -OnlyIsolation` (Advance Steel loads neither).

## Success Criteria
- [ ] HPAutoCad ribbon check 12/12 + live re-theme check; bridge 21/21; smoke 22/22; window dark and light screenshots.
- [ ] MirrorTests 55/55 with the new files classified and pins updated; Civil ribbon check passes; both Civil server tests 106/106.
- [ ] HPGeo `dialog-check.ps1` screenshots show a dark ComboBox popup on COLORTHEME 0 (the known gap closed); `acceptance.ps1` 51/51; tests 154/154.
- [ ] Co-load both orders clean; isolation 4/4 on both harnesses.
- [ ] `BridgeEntry.CompilerReferences` and `HostScriptContracts.AutocadImports` unchanged (toolkit invisible to scripts).

## Risk Assessment
- `SystemVariableChanged` may fire on a non-UI thread or during a command — marshal through `Application.Idle` exactly as `McpRibbonTab.cs` does for its COLORTHEME rebuild; S0-B step 5 measured it.
- Civil 3D is never co-loaded with HPGeo (`Platform="AutoCAD"`), so the S0-B fallback (repack) applies to Civil only for symmetry with the mirror — decide once, apply to both.
- `dialog-check.ps1` asserts WebView2 gate lines in `hpgeo-*.log`; the theme change must not alter those lines.

## Next Steps
Phase 5 (Navisworks) — independent; needs S0-C.
