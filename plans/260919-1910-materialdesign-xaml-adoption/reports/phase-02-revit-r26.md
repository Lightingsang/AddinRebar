# Phase 2 — HPRebar on MaterialDesign: build matrix + off-Revit gallery (2026-09-19)

Status: **Completed — user verified in Revit 2026 (2026-09-19 21:55+): Column/Beam/Foundation windows open, theme follows live.** Separate, out of scope: the stirrup run on the test column hit `RebarShapeDrivenAccessor.SetLayoutAsNumberWithSpacing` → `InternalException` in `StirrupCreator.Place` (rebar domain, pre-existing; `CLAUDE.md` still lists the rebar add-in as unverified at runtime). Deployed to `%AppData%\Autodesk\Revit\Addins\2026\HPRebar\` (`HPRebar.dll` 12 660 736 B, no loose toolkit DLL).

## Build matrix (`dotnet build HPRebar.slnx -c <config> -p:DeployAddin=false`)
| Configuration | Result | Note |
|---|---|---|
| Debug.R26 | 0 errors | whole solution incl. `HPRebar.McpBridge`, `HPRebar.Mcp.Server`, tests |
| Release.R26 | 0 errors | `HPRebar.dll` 12 603 904 B (repack path in Release) |
| Debug.R24 (net48) | 0 errors | `HPRebar.dll` 13 474 304 B, `ThemeInfo` present, 221/221 BAML pack URIs → `HPRebar` |
| Debug.R23 (net48) | 0 errors | |
| Debug.R27 (net10) | **10 errors — unchanged** (`RebarHookOrientation`, `Curve.Intersect` overload, `CurveIntersectResult` ==), **0 mention the toolkit** | pre-existing Revit 2027 API removals, out of scope |

## Tests
| Suite | Result |
|---|---|
| `HPRebar.Core.Tests` | **336 / 336** (334 + 2 theme token tests) |
| `HPRebar.Mcp.Server.Tests` | **109 / 109** |
| `list-baml-pack-uris.ps1` on the R26 / R24 DLLs | PASS (261 resources, 18 fonts, merged `generic.baml`, `ThemeInfo(SourceAssembly)`) |

## Off-Revit gallery (`HPRebar/tools/theme-gallery/`, new; net8.0-windows + net48)
Loads the merged DLL with `Assembly.LoadFrom` (no `Application` object, Revit API types skipped — 40 on net8, 165 on net48), hosts each of the **15 tab/sub views** in a window that merges `Theme.xaml` exactly as the real windows do, calls `MaterialThemeBridge.Apply(window, dark)` for dark then light, screenshots each pass and records `Color.Background`, `MaterialDesign.Brush.Background` and the window's *resolved* `Background` per pass (`index.txt`).
- R26 (net8): **15 / 15**, both passes; resolved background `#FF1E1E1E` → `#FFF8F8F8` on the light pass (the live switch reaches the tree).
- R24 (net48 build of the gallery over the net48 DLL): **15 / 15**, same values.
- Output: `HPRebar/output/theme-gallery/` and `…/theme-gallery-r24/` (30 PNG each). Inspected: `SettingTabView-dark`, `FoundationSettingView-light` (white cards, dark text, outlined inputs, MD combos), `StirrupsTabView-dark` (canvas preview keeps `Brush.Canvas.*` greens), `GeometryTabView` (DataGrid header MD).
- Bindings have no view model, so labels/values are empty; the gate is "every view parses, every key resolves, both palettes render, the switch repaints".

## Bug found by the first Revit run (fixed 2026-09-19 21:51)
`Could not read the selected columns. Provide value on 'System.Windows.Baml2006.TypeConverterMarkupExtension' threw an exception` → log: `IOException: Cannot locate resource 'resources/icons/columnrebar32.png'` in `ColumnRebarView.InitializeComponent()`. **Pre-existing since commit `a4a2f64` (2026-09-17)**: the vector-ribbon-icon commit deleted every PNG but `ColumnRebarView.xaml:8` kept `Icon="/HPRebar;component/Resources/Icons/ColumnRebar32.png"`; Beam/Foundation had no `Icon`, and the gallery hosts UserControls, not the Window. Fix (user choice: vector glyph on all three windows): `MaterialThemeBridge.Attach(window, host, icons => icons.ColumnRebar)` sets `window.Icon` from the theme-aware `RibbonIcons` on every apply; the XAML `Icon=` line is gone. Guards: `ThemeTokenCoverageTests.EveryComponentPathReferencedByXamlExistsOnDisk` (fails on any dangling `/HPRebar;component/<path>`; it failed on exactly this line before the fix), and the gallery's window-icon smoke (`RibbonIcons` → `Window.Icon` → HICON) — gallery now **17/17** on net8 and net48. Core tests **337/337**. Redeployed.

## What the user's Revit walk still has to prove (cannot be driven off-Revit)
1. Column / Beam / Foundation Rebar windows open on real data (view models, DataGrid rows, canvas with bars).
2. Both Revit UI themes at open time, and **Options ▸ Colors ▸ UI theme flipped while a window is open** → the window follows (new behaviour: `Application.ThemeChanged` → `RevitHostTheme.NotifyChanged` → overlay swap).
3. Every tab of every window once (`/illink` risk: a toolkit type used only from BAML on a rarely opened tab).
4. "Publisher could not be verified" → *Always Load* (unsigned DLL, known).

## Deltas vs the plan
- Windows' XAML unchanged (no `MaterialDesignWindow` style: each window already sets background/foreground/font from tokens; the toolkit style's Roboto setter would never load anyway).
- `Controls.xaml`, `Typography.xaml`, `Spacing.xaml`, `ThemeIcons.xaml` stay (pure token consumers). `Buttons.xaml`/`TextBoxes.xaml` un-merged but kept on disk for the bridge's link glob until phase 3.
- `HPRebar.McpBridge.csproj` link glob excludes `MaterialBridge.xaml` (toolkit types) until phase 3 adds the package there.
