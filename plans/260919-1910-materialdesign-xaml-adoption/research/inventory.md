# WPF surface inventory — 12 projects, 6 hosts (read-only, 2026-09-19)

Cites are `path:line` from the repo root; the load-bearing ones were re-read by hand after the scan.

## 0. Summary

| # | Project | TFM(s) | XAML files / lines | Windows | Load mechanism | Theme tokens | Dark/light source | Switch happens |
|---|---|---|---|---|---|---|---|---|
| 1 | `HPRebar/HPRebar` | net48 (R23/R24) · net8.0-windows7.0 (R25/R26) · net10.0-windows7.0 (R27) | 27 / 2 814 | 3 modeless | **ILRepack** merge | `Color.*`→`Brush.*`, `Spacing.*`, `Font.*` | Revit `UIThemeManager` | window ctor only |
| 2 | `HPRebar/HPRebar.McpBridge` | net8 (R25/R26) | 2 own + 7 linked / 180 | 1 modeless | loose DLLs, Revit per-add-in ALC | same (linked `Page`s) | Revit `UIThemeManager` | ctor only |
| 3 | `HPAutoCad/HPAutoCad.McpBridge` (+ Loader) | net8.0-windows | 3 / 299 | 1 modeless | Loader → isolated **ALC** `Contents\Bridge\` | `Brush.*`, `Font.*`, `Spacing.*`, `Radius.Card` | `COLORTHEME` sysvar | ctor only (light overlay added, never removed) |
| 4 | `HPCivil3d/HPCivil3d.McpBridge` (+ Loader) | net8.0-windows | 3 / 299 | 1 modeless | same ALC pattern; **token mirror of #3** | identical | `COLORTHEME` | ctor only |
| 5 | `HPNavis/HPNavis.McpBridge` | **net48** | 3 / 322 (1 not compiled) | 1 modeless | `AppDomain.AssemblyResolve` allow-list | bridge family, light only | **none** | never |
| 6 | `HPEtabs/HPEtabs.McpBridge` | net8.0-windows `WinExe` | 3 / 334 | 1 main window | plain WPF app, `App.xaml` | bridge family, light only | **none** | never |
| 7 | `HPGeo/HPGeo.AutoCad` (+ Loader) | net8.0-windows | 7 / 446 | 2 modal | Loader → isolated ALC `Contents\App\` + WebView2 | `Brush.Text/TextMuted/Input/AccentText`, `Spacing.XS/S/M`, `Padding.Control`, `Margin.*` | `COLORTHEME` | ctor only |
| 8 | `McpShared/HPRebar.McpBridge.Core` | net8.0 · net48 | 0 | 0 | — | — | — | WPF-free (no `UseWPF`, no `System.Windows`) — must stay so |
| 9–11 | `*.Loader` projects (HPAutoCad, HPCivil3d, HPGeo) | net8.0-windows | 0 | 0 | loaded by acad.exe into the default ALC | `UseWPF` for `Autodesk.Windows` ribbon only | — | out of scope |
| 12 | `HPRebar/HPRebar.Tests` / other test projects | — | 0 | — | — | — | — | out of scope |

No XAML file > 500 lines (largest `FoundationSettingView.xaml` 213). C# > 300 lines under UI folders: `HPRebar/HPRebar/BeamRebar/ViewModel/BeamRebarSession.cs` (536), `HPRebar/HPRebar/BeamRebar/View/Controls/BeamElevationPainter.cs` (442), `HPRebar/HPRebar/ColumnRebar/ViewModel/ColumnSpecEditor.cs` (320) — pre-existing, not touched by this plan.

Three token vocabularies exist → three alias dictionaries in phase 1, not one.

## 1. HPRebar/HPRebar (Revit add-in)

- csproj `HPRebar/HPRebar/HPRebar.csproj`: `UseWPF` :4, `DeployAddin` :5, `LaunchRevit` :6, **`IsRepackable=true`** :7, `EnableDynamicLoading` :8, configurations `Debug.R23..R27`/`Release.R23..R27` :9-10, `CommunityToolkit.Mvvm 8.4.0` :21, **`ILRepack 2.0.46`** :29, `Polyfill` :33, `ProjectReference HPRebar.Core` :37. No MaterialDesign package (repo-wide grep).
- Repack = SDK target `RepackAddinFiles` (`%NUGET%\nice3point.revit.sdk\6.2.3\Sdk\Nice3point.Revit.Repack.targets:11-14`): every `$(OutputPath)*.dll` minus `RepackBinariesExcludes` :28-35 is merged into `HPRebar.dll` with `ILRepack.exe /union /illink /parallel [/noRepackRes] /lib:<every ReferencePath dir>` :43-56.
- Theme aggregator `HPRebar/HPRebar/Resources/Themes/Theme.xaml:5-15`: `ThemeDark.xaml` first :7 ("swapped at runtime by ThemeSwitcher"), then Typography :9, Spacing :10, Buttons :11, TextBoxes :12, Controls :13, ThemeIcons :14 — absolute `pack://application:,,,/HPRebar;component/…`.
- Tokens: `ThemeDark.xaml` / `ThemeLight.xaml` each 26 `Color.*` + 26 `Brush.*` (`Brush.Background/Surface/SurfaceElevated/SurfaceHover/Foreground.Primary|Secondary|Tertiary|Disabled|OnAccent/Accent/Accent.Hover|Pressed/Border/Border.Focus/Success/Warning/Danger/Info/Canvas.*`; accent `#0696D7` at `ThemeDark.xaml:20`, `ThemeLight.xaml:20`; dark background `#1E1E1E` :6); `Spacing.xaml` 20 keys (5 doubles :7-11, 15 `Thickness` :14-36); `Typography.xaml` `Font.Family.Default/Mono` :7-8, `Font.Size.Caption/Body/Subheading/Heading/Title` :11-16, styles `Caption/Body/BodyStrong/Subheading/Heading/Title` :19-54; `Buttons.xaml` `PrimaryButton/SecondaryButton/DangerButton/IconButton/LinkButton` :6,44,74,79,104; `TextBoxes.xaml` `StandardTextBox/NumberTextBox/SearchTextBox` :6,43,49; `Controls.xaml` `Card/Separator/Badge/BadgeText/Tag` :6-36; `ThemeIcons.xaml` 8 `TabIcon.*` templates :11-174.
- Every view uses `{DynamicResource}` for tokens; `StaticResource` only for `BasedOn` chains and the `TabIcon` templates (counts per file in the scan: e.g. `BeamRebar/View/Tabs/AdditionalBarsTabView.xaml` Dyn 59, `FoundationRebar/View/FoundationSettingView.xaml` Dyn 65).
- **Runtime swap** = `ThemeSwitcher` ×3 identical copies: `HPRebar/HPRebar/ColumnRebar/Service/ThemeSwitcher.cs:14-15` (URIs), `:26-37` (replace the merged dictionary whose `Source` ends with ThemeDark/ThemeLight), `:68-77` (`RevitPrefersDark`: `#if REVIT2024_OR_GREATER` → `UIThemeManager.CurrentTheme == UITheme.Dark`, else `true`); `BeamRebar/Service/ThemeSwitcher.cs`, `FoundationRebar/Service/ThemeSwitcher.cs` same shape.
- Windows merge `Theme.xaml` in XAML (`BeamRebar/View/BeamRebarView.xaml:16-20`, `ColumnRebar/View/ColumnRebarView.xaml:17-21`, `FoundationRebar/View/FoundationRebarView.xaml:17-21`) and call `ThemeSwitcher.ApplyFromRevit(this)` in code-behind (`BeamRebarView.xaml.cs:17`, `ColumnRebarView.xaml.cs:14`, `FoundationRebarView.xaml.cs:14`); `BeamRebarCommand.cs:95` calls it again (redundant).
- Creation (Revit UI thread, `ExternalCommand.Execute`, static `_window`, `Owner = Application.MainWindowHandle` through `WindowInteropHelper`, `Show()`): `BeamRebar/BeamRebarCommand.cs:92-107`, `ColumnRebar/ColumnRebarCommand.cs:100-114`, `FoundationRebar/FoundationRebarCommand.cs:88-102`.
- `Application.ThemeChanged` is subscribed only for ribbon icons (`HPRebar/HPRebar/Application.cs:70-73` → `ApplyIcons()`); **open windows are never re-themed**.
- Code-behind beyond `InitializeComponent` + `DataContext`: the three windows (theme call + `CloseRequested += Close`); all 15 tab/sub views are `InitializeComponent()` only.
- Stale tracked file: `HPRebar/HPRebar/HPRebar_u4a1q5mn_wpftmp.csproj` (WPF markup-compile temp project with absolute paths) — flag for the user, not in scope.

## 2. HPRebar/HPRebar.McpBridge (Revit bridge add-in)

- csproj `HPRebar/HPRebar.McpBridge/HPRebar.McpBridge.csproj`: **`IsRepackable=false`** :9 (Roslyn copy-local), `EnableDynamicLoading` :10, configurations R25/R26 only :14 (comment :12-13), `Page ..\HPRebar\Resources\Themes\*.xaml` linked except `Theme.xaml` :49-51, `RibbonIcons.cs` linked :43, references `McpShared` Contracts :37 + Core :38.
- Own `Resources/Themes/Theme.xaml` (17 lines, URIs on `HPRebar.McpBridge`), `View/McpBridgeStatusView.xaml` (163, Dyn 47; local `StatusDot`/`CompileCountText` styles :18-54), `Service/ThemeSwitcher.cs` (4th copy; URIs :14-15, `RevitPrefersDark` :68-77).
- Creation `McpBridgeCommand.cs:43-57` (`Dispatcher.CurrentDispatcher`, `McpBridgeStatusViewModel(host, dispatcher.InvokeAsync, Clipboard.SetText)`, `Owner`, `Closed → Detach()`, `Show()`); runs "in its own assembly load context" beside HPRebar (`Application.cs:21-22`).
- Script compiler references are an explicit list `Application.cs:112-120`; a toolkit DLL beside the bridge is **not** auto-exposed to scripts.

## 3. HPAutoCad/HPAutoCad.McpBridge + Loader

- Bridge csproj: `net8.0-windows` :4, `UseWPF` :8, `EnableDynamicLoading` :15, `AutoCAD.NET [25.1.0] ExcludeAssets=runtime` :25, Mvvm :26, references Contracts :32, Core :33, `HPAutoCad.Aec` :35. Loader csproj: `DeployBundle` :16-18, `ProjectReference … ReferenceOutputAssembly=false Private=false` :30, `DeployBundle` target :38-53 (wipes `Contents\Bridge`, copies bridge output).
- ALC `HPAutoCad/HPAutoCad.McpBridge.Loader/BridgeLoadContext.cs:15-41`: `HostAssemblyPrefixes = ["Ac","Ad","Autodesk."]` :17 → `null` (default ALC), everything else through `AssemblyDependencyResolver` (deps.json) :36-40 → **a toolkit `PackageReference` lands in deps.json and loads inside the bridge ALC automatically**.
- Theme `Resources/Themes/AutocadTheme.xaml` (123; brushes :12-23 — accent `#3C8DDE`, background `#2B2B2B`; `Font.*` :26-30; `Spacing.*` :33-38; `Radius.Card` :39; styles `Caption/BodyStrong/Subheading` :42-56, `Card` :59, `ButtonBase/PrimaryButton/SecondaryButton/LinkButton` :68-108, `StandardTextBox` :111, implicit `CheckBox` :119); `AutocadThemeLight.xaml` (16) overrides the 12 brushes.
- Switch `Service/AutocadThemeSwitcher.cs:17-29` **adds** the light dictionary when `IsLight()` (`COLORTHEME` `short/int == 1`, :31-42) — never removed, no `SystemVariableChanged` subscription anywhere in `HPAutoCad/`.
- Window `View/AutocadBridgeStatusView.xaml` (160; merges `/HPAutoCad.McpBridge;component/Resources/Themes/AutocadTheme.xaml` :18-22); code-behind `AutocadBridgeStatusView.xaml.cs:14-18` wraps `InitializeComponent()` + `ApplyFromAutocad(this)` in `AssemblyLoadContext.GetLoadContext(...).EnterContextualReflection()` ("WPF resolves `/…;component/` through `Assembly.Load`, which searches the default load context").
- Creation `BridgeEntry.cs:121-146 ShowWindow` (static `_window` :49, `Dispatcher.CurrentDispatcher` :132, `AcadApp.ShowModelessWindow(view)` :143); delegate `"show"` :89 ← `HPMCPBRIDGE` (`Loader/BridgeLoaderCommands.cs:16-17`) and the ribbon (`Loader/Ribbon/McpRibbonTab.cs`).
- Script compiler `BridgeEntry.cs:113-119` = explicit `typeof(...)` list — a toolkit DLL in `Contents\Bridge` is loadable by the ALC but **invisible to scripts** unless added there (do not add).

## 4. HPCivil3d/HPCivil3d.McpBridge + Loader (mirror of #3)

- Bridge csproj mirrors #3 plus `AeccDbMgd/AeccPressurePipesMgd/AecBaseMgd` `Private=false` :38-49; ALC prefixes add `"Aec"` (`Loader/BridgeLoadContext.cs:17`).
- `HPCivil3d/tools/mirror-tokens.json` (enforced by `HPCivil3d/HPCivil3d.McpBridge.Tests/MirrorTests.cs` :29, :73, :92, :108-115):
  - **mirroredFiles** touching UI: `View/AutocadBridgeStatusView.xaml` :278-281, `.xaml.cs` :282-285, `Resources/Themes/AutocadTheme.xaml` :286-289, `AutocadThemeLight.xaml` :290-293, `Service/AutocadThemeSwitcher.cs` :266-269, plus every Loader/Ribbon file :210-245 → any edit on the AutoCAD side must be copied verbatim + tokens.
  - **ownedCounterparts (sha256-pinned)**: `HPAutoCad.McpBridge/BridgeEntry.cs` :326-330 (`ShowWindow` lives there), **`HPAutoCad.McpBridge/HPAutoCad.McpBridge.csproj` :336-340** (adding a `PackageReference` trips the pin until re-pinned), `Service/ScriptingSelfCheck.cs` :331-335.
  - Rule 5: every new file under the AutoCAD bridge/loader must be classified (`mirroredFiles` or `ownedCounterparts`) or `MirrorTests` fails. No `<!-- civil-only -->` block exists in the Civil XAML today.

## 5. HPNavis/HPNavis.McpBridge (net48)

- csproj: `net48` :5, `UseWPF` :9, `DeployPlugin` :16-17, Navisworks API `Private=false` :23-25, Mvvm :32, Polyfill :36, references Contracts :40, Core :41 (net48 assets); `Page Remove="Ribbon\**\*.xaml"` :51; `DeployPlugin` target :64-71.
- Resolver `HPNavis/HPNavis.McpBridge/PluginAssemblyResolver.cs`: `AllowList` :17-25 (Roslyn, Immutable, System.Text.Json, Mvvm, Serilog, Contracts, Core, …); `Resolve` :59-83 — allow-list :65, requester-in-folder-or-null :68-69, **version-family gate :77-78** (`requested.Major != available.Major || requested > available → null`), `Assembly.LoadFrom` :80. **A toolkit request is refused today** (not in the allow-list).
- Theme `Resources/Themes/NavisTheme.xaml` (123, light values :12-23, `xmlns:sys=mscorlib` :9, comment :4 "no theme API is exposed"); window `View/NavisBridgeStatusView.xaml` (172; merges NavisTheme :18-22); code-behind = `CloseRequested += Close`.
- Creation `BridgeEntry.cs:171-198 ShowWindow` (`Owner = executor.Quiescence.MainWindow` via `WindowInteropHelper` :186-187, `Show()` :196) ← ribbon `HPNavisRibbonPlugin.cs:37`, hidden AddIn `HPNavisWindowPlugin.cs:19`, env `HPNAVIS_MCP_BRIDGE_SHOW_WINDOW=1` :108-125.
- Foreign plugin beside ours: `%AppData%\Autodesk\Navisworks Manage 2026\Plugins\NavisworksMCPPlugin\` — on the dev machine it ships **no** `MaterialDesign*`/`Microsoft.Xaml.Behaviors*` DLL (listed 2026-09-19); other machines `[chưa xác minh]`.

## 6. HPEtabs/HPEtabs.McpBridge (standalone app)

- csproj: `OutputType=WinExe` :6, `net8.0-windows` :7, `UseWPF` :11, `ETABSv1 Private=false` :32-35, Mvvm :39, references Contracts :45, Core :46; `PublishSingleFile=false` :19 (publish as folder).
- `App.xaml:5-11` merges `/HPEtabs.McpBridge;component/Resources/Themes/EtabsTheme.xaml` in `Application.Resources`; `App.xaml.cs:18-38 OnStartup` builds the view model and `EtabsBridgeStatusView` (`MainWindow = window` :34, `Show()` :35).
- Theme `Resources/Themes/EtabsTheme.xaml` (123, light only); window `View/EtabsBridgeStatusView.xaml` (199, Dyn 61; **no MergedDictionaries**, local `StatusDot`/`SelfCheckText` :19-51; `Icon` :10). No dark/light source.

## 7. HPGeo/HPGeo.AutoCad + Loader

- csproj: `net8.0-windows` :7, `UseWPF` :11, `EnableDynamicLoading` :18, AutoCAD.NET :24, Mvvm :25, **WebView2 1.0.4191.47** :26, `HPGeo.Core` :30; no `McpShared` (decision, `CLAUDE.md` HPGeo section). Loader: `DeployBundle` :19-21, target :39-55 (copies to `Contents\App\`).
- ALC `HPGeo/HPGeo.AutoCad.Loader/GeoLoadContext.cs:14-35` (same prefixes/resolver as #3); bundle `Platform="AutoCAD"` (`Loader/Bundle/PackageContents.xml:21`) → **co-loads with the HPAutoCad bridge in every AutoCAD 2026 session**, never in Civil 3D.
- Theme: `UI/ThemeDark.xaml:4-14` / `UI/ThemeLight.xaml:4-14` = 11 brushes (`Brush.Background/Surface/Input/Border/Text/TextMuted/Accent(#0696D7)/AccentText/Danger/Warning/Success`); `UI/Theme.xaml` `Spacing.XS/S/M` :7-9 (doubles), `Padding.Control` :10, `Margin.Row/Section` :11-12, implicit styles `TextBlock` :17, `TextBox` :34, **`ComboBox` :44-48 (Padding/VerticalContentAlignment/MinHeight only — no brushes, no template → Aero2 light chrome + white popup in dark theme = the known gap)**, `RadioButton` :50, `Button` :56, `Button.Primary` :84, `Expander` :91, `DataGrid` :96, `DataGridColumnHeader` :110, `DataGridCell` :118.
- ComboBox usages: `UI/CrsSelectionView.xaml:30-41` (editable, `ItemTemplate`), `:43-44`, `:57-58`; `UI/GeoImportWindow.xaml:52-56`.
- Merge in code-behind, before `InitializeComponent`, inside `EnterContextualReflection`: `UI/GeoExportWindow.xaml.cs:14-19` (`ThemeResources.Brushes(dark)` then `ThemeResources.Styles()`, `MapView.DarkTheme = dark`), `UI/GeoImportWindow.xaml.cs:12-16`; `UI/ThemeResources.cs:11-16` builds the relative URIs. `IsDarkTheme()` duplicated (`GeoExportWindow.xaml.cs:34-38`, `GeoImportWindow.xaml.cs:22-26`, `COLORTHEME == 0`).
- Creation (modal, AutoCAD main thread): `Commands/HPGeoDialogCommand.cs:56-58` (`AcadApp.ShowModalWindow(AcadApp.MainWindow.Handle, window, false)`), `Commands/HPGeoImportCommand.cs:26-27`.
- Code-behind with logic: `UI/MapPanel.xaml.cs` (142, WebView2 lifecycle — untouched by this plan), the two windows (theme + `MapEnabled`).

## 8. McpShared/HPRebar.McpBridge.Core

- `HPRebar.McpBridge.Core.csproj`: `net8.0;net48` :6, no `UseWPF`, Mvvm :20 with comment "no WPF". `ViewModel/McpBridgeStatusViewModel.cs:48` takes `Action<Action> onUiThread` + `Action<string>? copyToClipboard` — dispatcher/clipboard injected, so the view model stays host- and toolkit-free. **The toolkit must never be referenced here.**

## 9. Cross-cutting

- Four `ThemeSwitcher` copies (three rebar features + McpBridge), three `IsLight/IsDark` readers (AutoCAD, Civil, HPGeo ×2) — phase 1 replaces each with one `MaterialThemeBridge` per host (same copy count as today; folders never reference each other).
- Pack-URI styles: absolute `pack://application:,,,/…` (HPRebar family) vs relative `/Asm;component/…` (bridges, HPGeo); the ALC hosts need `EnterContextualReflection` around `InitializeComponent` (AutoCAD :14, Civil :14, HPGeo :14/:12) — the toolkit's own `pack://application:,,,/MaterialDesignThemes.Wpf;component/…` loads inside that scope.
- Harnesses that exercise a window: `HPAutoCad/tools/harness/run-ribbon-check.ps1` (12 + 1 MANUAL, opens the bridge window twice, both COLORTHEMEs), `HPNavis/tools/harness/run-ribbon-check.ps1` (14 + 1 MANUAL), `HPCivil3d/tools/harness/run-ribbon-check.ps1`, `HPGeo/tools/dialog-check.ps1` (opens HPGEO in both COLORTHEMEs + HPGEOIMPORT, `PrintWindow` screenshots), `HPEtabs/tools/harness/run-live-verify.ps1` (UIA on the bridge window), `HPAutoCad/tools/harness/run-bridge-unattended.ps1`, `HPNavis/tools/harness/run-bridge-unattended.ps1`. Revit rebar windows: **no harness** — F5 by the user.
