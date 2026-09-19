---
phase: 2
title: "HPRebar (Revit) — R26, then R23/R24 net48, then R27"
status: completed
priority: P1
effort: "14h"
dependencies: [0, 1]
---

# Phase 2: HPRebar (Revit add-in)

> **Completed 2026-09-19 — verified in Revit 2026 by the user (3 windows open, theme follows live); a dangling icon PNG from `a4a2f64` was found by the first run and fixed** — [reports/phase-02-revit-r26.md](reports/phase-02-revit-r26.md): R26/R24/R23/Release.R26 clean, R27 = the same 10 pre-existing errors, Core 336/336, MCP server 109/109, gallery 15/15 on net8 and net48.

## Overview
The product add-in gets the toolkit first: `Debug.R26` (verified host), then the net48 configurations, then R27 once its pre-existing Revit 2027 API errors are fixed (out of this plan's scope). Three windows + 16 tab views migrate view by view on top of the phase-1 bridge; the four `ThemeSwitcher` copies go away.

## Requirements
- Functional: every window merges `MaterialThemeBridge` and follows Revit's theme live (`Application.ThemeChanged`, which today only repaints ribbon icons — `HPRebar/HPRebar/Application.cs:70-73`); `ComboBox`, `TextBox`, `Button`, `TabControl`, `Expander`, `DataGrid` use MaterialDesign styles; the rebar canvases (`BeamElevationPainter.cs`, `Brush.Canvas.*`) keep their colours.
- Non-functional: build per configuration with `-p:DeployAddin=false` while Revit is open; `HPRebar.Core` untouched; ILRepack **merge** (S0-A verdict `go` on R26 and R24 — 15/15 off-Revit each; no `RepackBinariesExcludes`), recorded in the csproj comment; every window keeps `FontFamily="{DynamicResource Font.Family.Default}"` (the `MaterialDesignWindow` Roboto setter cannot load under the merge).

## Architecture
`HPRebar.csproj` + `MaterialDesignThemes 5.3.2`. Views: `Window.Resources` merges `MaterialThemeBridge`'s dictionaries **in XAML** (so the designer works) — `CustomColorTheme` → `MaterialDesign2.Defaults.xaml` → `MaterialBridge.xaml` → (until step 6) `Theme.xaml`; code-behind keeps one call `MaterialThemeBridge.Attach(this, RevitHostTheme.Instance)` in place of `ThemeSwitcher.ApplyFromRevit(this)`. `RevitHostTheme.Changed` is raised from `Application.OnStartup`'s existing `ThemeChanged` handler (one line next to `ApplyIcons()`; the two template files are otherwise untouched).

## Related Code Files
- Modify: `HPRebar/HPRebar/HPRebar.csproj` (package; no `RepackBinariesExcludes` — S0-A verdict is the merge), `Resources/Themes/Theme.xaml` (drops `ThemeDark.xaml` once brushes come from the bridge), `ColumnRebar/View/ColumnRebarView.xaml(.cs)`, `BeamRebar/View/BeamRebarView.xaml(.cs)`, `FoundationRebar/View/FoundationRebarView.xaml(.cs)`, the 16 tab/sub views (style keys only where a legacy key is retired), `BeamRebar/BeamRebarCommand.cs:95` (remove the redundant second theme call), `Application.cs:70-73` (raise `RevitHostTheme.Changed`).
- Delete: `ColumnRebar/Service/ThemeSwitcher.cs`, `BeamRebar/Service/ThemeSwitcher.cs`, `FoundationRebar/Service/ThemeSwitcher.cs`; at the end `Resources/Themes/ThemeDark.xaml`, `ThemeLight.xaml`, `Buttons.xaml`, `TextBoxes.xaml`, `Controls.xaml` (their keys now live in `MaterialBridge.xaml`); `Typography.xaml` / `Spacing.xaml` / `ThemeIcons.xaml` stay.
- Keep: `HPRebar.McpBridge.csproj:49-51` links `..\HPRebar\Resources\Themes\*.xaml` except `Theme.xaml` — every deletion here must leave the bridge compiling (phase 3 handles its window; until then the link glob simply picks up fewer files).

## Implementation Steps
1. `PackageReference MaterialDesignThemes 5.3.2` + `[assembly: ThemeInfo(None, SourceAssembly)]` (new file `Resources/Themes/ThemeInfo.cs` — S0-A: without it `PackIcon`/`Card` have no default style after the merge); build `Debug.R26 -p:DeployAddin=false`; `reports/list-baml-pack-uris.ps1` on the real add-in must still say `RESULT: PASS` and `ThemeInfo … SourceAssembly`.
2. `ColumnRebarView`: merge order as above, `MaterialDesignWindow`, `Attach`; walk its 8 tabs — replace `Style="{DynamicResource StandardTextBox}"` etc. only where the bridge's `BasedOn` mapping looks wrong; keep `TabIcon.*` templates.
3. `BeamRebarView` (5 tabs) and `FoundationRebarView` (2 views) the same way; `BeamElevationPainter.cs` reads `Brush.Canvas.*` through `FindResource` — keep those keys in `MaterialBridge.xaml` with the old values.
4. Live switch: change Revit theme with a window open → brushes follow; close/reopen → correct theme.
5. `dotnet test HPRebar/HPRebar.Core.Tests` (334) + the phase-1 mapping test; `dotnet build HPRebar/HPRebar.slnx -c Debug.R26` then `-c Release.R26` (repack path exercised in Release too).
6. Delete the retired dictionaries and the three `ThemeSwitcher` copies; rebuild; grep proves no `ThemeDark.xaml` / `ThemeSwitcher` reference remains.
7. `-c Debug.R24 -p:DeployAddin=false` and `-c Debug.R23`: build + resource listing (net462 asset); runtime on Revit 2023/2024 is **not** available on the dev machine → recorded as build-only, same as the rebar features today.
8. `-c Debug.R27`: expect the 10 pre-existing errors (`RebarHookOrientation`, `Curve.Intersect`) — verify **no additional** error mentions `MaterialDesign`; record. R27 runtime stays out of scope.
9. Ribbon icons unchanged (`RibbonIcons.cs`, `preview-ribbon-icons.ps1` still renders both themes).

## Success Criteria
- [x] `Debug.R26` + `Release.R26` build clean; deployed build opens the 3 windows in Revit 2026, live switch works (user walk 2026-09-19; log `hprebar-20260919.log` 21:55 "read a Rectangle stack").
- [x] `HPRebar.Core.Tests` 336 pass (334 + 2 theme token tests).
- [x] `Debug.R23`/`Debug.R24` build clean; `HPRebar.dll` (net48) carries the toolkit resources; net48 gallery 15/15; Revit 2023/2024 runtime `[chưa xác minh]`.
- [x] `Debug.R27`: error count unchanged (10), none from the toolkit.
- [x] Three feature `ThemeSwitcher` copies gone (the bridge's copy goes in phase 3); `ThemeDark/Light.xaml` **stay** (they are the palette — design change, see phase 1 report); no `StaticResource` for a brush in any view.

## Risk Assessment
- `/illink` trimming a toolkit type used only from BAML → `XamlParseException` on a rarely opened tab. Mitigation: step 2–3 open **every** tab; the phase report lists them.
- The merged `HPRebar.dll` grows ≈ 10 MB per Revit version → `dotnet run -- pack` installer size; acceptable, note it in `docs/deployment-guide.md`.
- Other Revit add-ins with their own MaterialDesign copy: the merged build exports no `MaterialDesignThemes.Wpf` name → no clash on any Revit version (S0-B showed that loose copies in separate load contexts do mix on .NET 8, so the merge is the isolation, not the ALC).

## Next Steps
Phase 3 (Revit MCP bridge window shares the linked dictionaries) right after; user F5 walk is the only manual gate.
