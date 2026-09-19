# MaterialDesignInXAML toolkit — facts verified 2026-09-19

Every row = verified against the URL beside it unless tagged `[chưa xác minh]`. Verification method: GitHub REST API (`api.github.com`, unauthenticated), NuGet registration API + the 5.3.2 `.nupkg` downloaded into the session scratchpad and listed (no restore into any project), raw source at tag `v5.3.2`, `System.Reflection.Metadata` over the package DLLs.

## 1. Versions, licence, packages

| Fact | Value | Source |
|---|---|---|
| Latest stable `MaterialDesignThemes` | **5.3.2** (2026-05-01); prerelease `5.4.0-ci1487` (2026-09-16) | https://www.nuget.org/packages/MaterialDesignThemes · registration API `api.nuget.org/v3/registration5-gz-semver2/materialdesignthemes/index.json` |
| Latest stable `MaterialDesignColors` | **5.3.2** (2026-05-01) | https://www.nuget.org/packages/MaterialDesignColors |
| Latest GitHub release | `v5.3.2` (2026-05-01), repo pushed 2026-09-11, 16 257 ★ | https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/releases/tag/v5.3.2 |
| Licence | **MIT** (repo `license.spdx_id`, nuspec `<license type="expression">MIT`) | https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit · nuspec inside the 5.3.2 nupkg |
| `lib/` folders of MaterialDesignThemes 5.3.2 | `net462`, `net8.0-windows7.0`, `net10.0-windows7.0` — one `MaterialDesignThemes.Wpf.dll` each, **~10.0 MB**, strong-named, `AssemblyVersion 5.3.2.0` | nupkg listing; csproj `<TargetFrameworks>net462;net8.0-windows;net10.0-windows` https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/blob/v5.3.2/src/MaterialDesignThemes.Wpf/MaterialDesignThemes.Wpf.csproj |
| `lib/` of MaterialDesignColors 5.3.2 | same three TFMs, ~320 KB, strong-named | nupkg listing |
| **net4x asset** | yes — `net462` (works for `net48` projects) | nupkg listing |
| **net10.0-windows** asset | yes — native `net10.0-windows7.0` asset; actual restore on `net10.0-windows7.0` **[chưa xác minh]** (no restore run in this plan) | nupkg listing |
| Dependencies (all three TFMs) | `MaterialDesignColors [5.3.2, )`, `Microsoft.Xaml.Behaviors.Wpf [1.1.77, )` (Behaviors 1.1.77 ships `net462` + `net6.0-windows7.0`; latest 1.1.161); framework reference `Microsoft.WindowsDesktop.App.WPF` on net8/net10 | nuspec · https://www.nuget.org/packages/Microsoft.Xaml.Behaviors.Wpf |
| Assembly references of the net8 DLL | shared-framework only (`PresentationFramework`, `System.Xaml`, `Microsoft.Win32.Registry`, `System.Configuration.ConfigurationManager`, …) + `Microsoft.Xaml.Behaviors 1.1.0.0` + `MaterialDesignColors 5.3.2.0`; net462 DLL: `mscorlib/System/System.Xaml/PresentationFramework/WindowsBase/PresentationCore` + the same two | `System.Reflection.Metadata` over the nupkg DLLs |
| Manifest resources | `MaterialDesignThemes.Wpf.g.resources` = **89 entries**: every `themes/*.baml` incl. `themes/generic.baml`, `materialdesign2.defaults.baml`, `materialdesign3.defaults.baml`, `materialdesigntheme.window.baml`, plus **18 Roboto `.ttf`** | `ResourceReader` over the net8 DLL |
| `build/MaterialDesignThemes.targets` | copies the Roboto TTFs to output only when `IncludeMaterialDesignFont=True` (default `False`) — the DLL already embeds them | file inside the nupkg |

## 2. MD2 vs MD3, dictionaries, merge order

- 5.0.0 **removed the unified `MaterialDesignTheme.Defaults.xaml`** (PR #3467 "Removing defaults in favor of specifying MD2 or MD3"); at `v5.3.2` the `Themes/` folder has `MaterialDesign2.Defaults.xaml` and `MaterialDesign3.Defaults.xaml` only (the legacy file is absent). https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/releases/tag/v5.0.0
- `MaterialDesign2.Defaults.xaml` = 46 `MaterialDesignTheme.*.xaml` control dictionaries + implicit styles (`Button → MaterialDesignRaisedButton`, `ComboBox → MaterialDesignComboBox`, …). https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/blob/v5.3.2/src/MaterialDesignThemes.Wpf/Themes/MaterialDesign2.Defaults.xaml
- `MaterialDesign3.Defaults.xaml` = MD3 `Font/TextBlock/Window/NavigationRail/NavigationBar/NavigationDrawer/Slider/ToggleButton` **then the same MD2 control dictionaries** (comment in file: "add MD3 toggle button last because CheckBox and ToolBar resource dictionaries import the MD2 version"). https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/blob/v5.3.2/src/MaterialDesignThemes.Wpf/Themes/MaterialDesign3.Defaults.xaml
- README: "`MaterialDesign3Demo` - Reference WPF app with Material Design 3 styling, **under development**"; quick start uses MD2 and says switch the one line to `MaterialDesign3.Defaults.xaml` for MD3. https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit#getting-started
- Required order (README + wiki): **`BundledTheme` first, `*.Defaults.xaml` second**, then `Style="{StaticResource MaterialDesignWindow}"` on the window ("to properly setup all of the colors"). https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/wiki/Getting-Started
- Wiki says **nothing** about class libraries / plugins without `App.xaml` (checked). Same wiki page.

## 3. Theme objects and the run-time dark/light switch

| API | Behaviour (source at `v5.3.2`) | Source |
|---|---|---|
| `BundledTheme : ResourceDictionary, IMaterialDesignThemeDictionary` | `BaseTheme` (`Inherit/Light/Dark`) + `PrimaryColor`/`SecondaryColor` swatch enums (+ `Inherit` = Windows DWM accent, 5.3.0 PR #3812) → `Theme.Create(...)` → `this.SetTheme(theme)` | https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/blob/v5.3.2/src/MaterialDesignThemes.Wpf/BundledTheme.cs |
| `CustomColorTheme` | same, exact `Color` values instead of swatches | https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/blob/v5.3.2/src/MaterialDesignThemes.Wpf/CustomColorTheme.cs |
| `PaletteHelper.GetTheme/SetTheme/GetThemeManager` | **throws `InvalidOperationException` when `Application.Current is null`** ("Use ResourceDictionaryExtensions.SetTheme on the appropriate resource dictionary instead"); otherwise finds the `IMaterialDesignThemeDictionary` in `Application.Current.Resources.MergedDictionaries` and re-creates dictionaries flagged `MaterialDesign.Resources.RecreateOnThemeChange` | https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/blob/v5.3.2/src/MaterialDesignThemes.Wpf/PaletteHelper.cs |
| `ResourceDictionaryExtensions.SetTheme(rd, theme)` | writes `MaterialDesign.Brush.Primary[.Light/.Dark][.Foreground]`, `…Secondary…`, then every generated `MaterialDesign.Brush.*` key (Background, Foreground, Card.Background, ComboBox.*, TextBox.*, DataGrid.*, ScrollBar.*, ToolTip.Background, ValidationError, … — 89 keys in `ResourceDictionaryExtensions.g.cs`); a **frozen** brush is replaced by a new frozen brush, a non-frozen one is animated 300 ms; stores the theme under `MaterialDesignThemes.CurrentThemeKey`; raises `IThemeManager.ThemeChanged(oldTheme, newTheme)` | https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/blob/v5.3.2/src/MaterialDesignThemes.Wpf/ResourceDictionaryExtensions.cs |
| `GetTheme(rd)` | returns the stored theme or rebuilds one from the brushes (throws "Could not locate required resource with key(s) 'MaterialDesign.Brush.Primary.Light'" when called on a dictionary that never had a theme — issue #3749 from a Revit plugin) | same file · https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/issues/3749 |
| `Theme.GetSystemTheme()` / `Theme.GetSystemAccentColor()` | registry `AppsUseLightTheme` / DWM `ColorizationColor` | https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/blob/v5.3.2/src/MaterialDesignThemes.Wpf/Theme.cs |
| Obsolete-name bridges | `MaterialDesignTheme.ObsoleteBrushes.xaml`, `MaterialDesignTheme.ObsoleteStyles.xaml`, `ObsoleteConverters.xaml` still shipped at 5.3.2; `SetObsoleteBrushes` re-creates their `StaticResourceExtension` entries after a theme change | Themes folder listing at `v5.3.2` |

**Per-window merge without `Application.Resources` (add-ins)** — maintainer's own sample (linked from #3749 and #3809), verified file by file:
`Window.Resources` merges `<materialDesign:BundledTheme …/>` then `MaterialDesign3.Defaults.xaml`; `Window.Style = {StaticResource MaterialDesignWindow}` **after** the merge; the switch does `Resources.MergedDictionaries.Single(x => x is IMaterialDesignThemeDictionary)` → `GetTheme()` → `SetBaseTheme(...)` → `SetTheme(theme)` — "We can't use PaletteHelper here because it will try to use Application.Current.Resource".
https://github.com/Keboo/MaterialDesignInXaml.Examples/tree/68b4feebf1cdca0f0229d3b7b184e8ea0a1cf3f0/Theming/ThemingInClassLib (files `LibWithMaterialDesign/LibraryWindow.xaml`, `LibraryWindow.xaml.cs`, `IThemeSwitcher.cs`; the lib targets `net8.0-windows`, `MaterialDesignThemes 5.1.0`, `CommunityToolkit.Mvvm 8.4.0`).

## 4. Breaking changes 4.x → 5.x

Migration guide https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/discussions/3466 (full list in issue #2255):
- Brushes renamed to `MaterialDesign.Brush.*` (`MaterialDesign.Brush.<Primary|Secondary>[.Light|.Dark][.Foreground]`); "Accent" → "Secondary" everywhere (PR #3409); `MaterialDesignTheme.ObsoleteBrushes.xaml` / `ObsoleteStyles.xaml` as temporary shims; migration scripts in the repo `build` folder.
- Unified `Defaults.xaml` split into MD2/MD3 (above). `ShadowAssist` → `ElevationAssist` (`ShadowDepth` → `Elevation.Dp*`). `*Mixin` → `*Extensions`. Everything obsolete in 4.9 deleted.
- Target frameworks: 5.0.0 `net462;net6.0;net7.0` → 5.1.0 drops net7, adds net8 (PR #3597) → 5.3.0 **removes .NET 6** (PR #3859); 5.3.x ships net10 (nupkg). https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/releases/tag/v5.1.0 · https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/releases/tag/v5.3.0
- 5.3.0 "Breaking Changes" section: disabled `Calendar` white rectangle fix (#3935), horizontal scrollbar placement for multi-line `TextBox` (#3934). 5.2.0 removed global converter resources (#3732). 5.3.2 (latest): `Clock.MinuteSelectionStep` only. https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/releases/tag/v5.3.2

## 5. Known issues relevant to add-ins (issue search `ILRepack`, `ILMerge`, `AssemblyLoadContext`, `Revit`, `AutoCAD`)

| Topic | Finding | Source |
|---|---|---|
| ILRepack | **No toolkit issue reports a merge failure.** #3809 (COM-hosted net8 lib, "Could not load file or assembly 'MaterialDesignThemes.Wpf'") was *solved by the reporter* by ILRepacking every .NET DLL into one; maintainer pointed at the ThemingInClassLib sample. ILMerge: 0 issues | https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/issues/3809 |
| ILRepack's own WPF support | `ILRepack/Steps/ResourceProcessing/BamlResourcePatcher.cs` patches BAML `AssemblyInfo`, `TypeInfo`, `Xmlns` (`assembly=` → merged name) and `PropertyWithConverter`/`Text` records through `XamlResourcePathPatcherStep.PatchPath` (regex `/([^/]*?);component` and `pack://…`); `BamlGenerator` builds a merged `Themes/Generic.xaml`. README lists no WPF option — behaviour is implicit. Latest ILRepack 2.0.48 (2026-09-01); repo pins **2.0.46** | https://github.com/gluck/il-repack/blob/master/ILRepack/Steps/ResourceProcessing/BamlResourcePatcher.cs · https://github.com/gluck/il-repack/blob/master/ILRepack/Steps/XamlResourcePathPatcherStep.cs · https://github.com/gluck/il-repack/releases |
| Revit plugin, no App.xaml | #1249 (2019, net48 TextBox `StaticResourceHolder` NotImplementedException — maintainer: assembly loading, use `Assembly.LoadFrom` like Dynamo); #3749 (2025, 5.1, `PaletteHelper` throws in a Revit class library → solved by the sample above); #3879 (2025-06, **open**, 5.x "Can not find resource path" in a Revit window merging `BundledTheme` + `MaterialDesign3.Defaults.xaml`; reporter says 4.9.0 works) | https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/issues/1249 · /issues/3749 · /issues/3879 |
| AutoCAD plugin | #1340 (2019, NETLOAD class library: `PackIcon`-bearing templates threw `StaticResourceHolder`; needed "kick-start" type references so the toolkit assemblies load) | https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/issues/1340 |
| Multiple copies in one process | #3316 (2023, net48 Revit plugin opened its window in a **new AppDomain** "to avoid version conflict between different versions of MaterialDesign — other uploaded plugins"; ComboBox popups stopped opening; closed stale). No issue covers two copies in two `AssemblyLoadContext`s; WPF resolves `;component` by **simple assembly name** (`BaseUriHelper`), so which copy answers when two are loaded is **[chưa xác minh]** → phase-0 spike | https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/issues/3316 |
| Pack URIs | every toolkit dictionary references siblings as `pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/…` (verified in both Defaults files) — inside an isolated ALC this needs the assembly resolvable by name during `InitializeComponent` (the repo already wraps it in `EnterContextualReflection`) | Defaults sources above |
| `Application.Current` dependence | `PaletteHelper` only (verified); `DialogHost`/`Snackbar` **[chưa xác minh]** — not needed by any current window | PaletteHelper.cs |

## 6. What this means for the plan (decisions carried into `plan.md`)

1. One toolkit version everywhere: **5.3.2** (`net462` for the net48 hosts, `net8.0-windows7.0`, `net10.0-windows7.0`).
2. Windows merge the theme **per window** (`BundledTheme`/`CustomColorTheme` + `MaterialDesign2.Defaults.xaml`) and switch through `ResourceDictionaryExtensions.SetTheme` — never `PaletteHelper` — except HPEtabs, which has `App.xaml` and may use either.
3. Legacy `Brush.*` tokens are re-derived from `MaterialDesign.Brush.*` after every `SetTheme` (C# copy, ~40 lines), because a `StaticResource` alias goes stale when a frozen brush is replaced (§3).
4. ILRepack merge of the toolkit into `HPRebar.dll` is plausible (BAML/pack-URI patching exists) but **unproven for this toolkit** → phase-0 spike with a measurable gate; loose DLLs are the fallback (`RepackBinariesExcludes`).
5. Two isolated ALCs in one `acad.exe` (HPAutoCad bridge + HPGeo) each carrying `MaterialDesignThemes.Wpf` → phase-0 spike; fallback = repack the toolkit into each bridge assembly so no process ever sees the simple name twice.
