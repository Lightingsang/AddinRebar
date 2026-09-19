# Spike S0-A — ILRepack merge of MaterialDesignThemes 5.3.2 into `HPRebar.dll` (2026-09-19)

**Verdict: `go` (merge), with three accompaniments** — `[assembly: ThemeInfo(None, SourceAssembly)]`, every window keeps an explicit `FontFamily`, never `{md:MaterialDesignFont}` in our XAML.

Worktree `../AddinRebar-md-spike` (branch `spike/md-spike` off `4e63e83`), never merged. Changes: `HPRebar.csproj` + `MaterialDesignThemes 5.3.2`; `ColumnRebarView.xaml` merges `BundledTheme` + `MaterialDesign2.Defaults.xaml` before `Theme.xaml`; throw-away `Resources/Spike/MaterialSpikeWindow.xaml(.cs)` (ComboBox, TextBox, CheckBox, raised/icon/outlined Button, `PackIcon`, `Card`, legacy-token panel, `ToggleTheme()` via `ResourceDictionaryExtensions.SetTheme`). Builds: `dotnet build HPRebar/HPRebar.csproj -c Debug.R26 -p:DeployAddin=false` and `-c Debug.R24`. Nothing deployed to Revit.

## Measurements (`list-baml-pack-uris.ps1`, `s0a-r26/`, `s0a-r24/`)

| Gate | R26 (net8.0-windows7.0) | R24 (net48 → toolkit `net462`) |
|---|---|---|
| Loose toolkit DLLs beside `HPRebar.dll` | none — `ILRepack.List` names `MaterialDesignThemes.Wpf 5.3.2.0`, `MaterialDesignColors 5.3.2.0`, `Microsoft.Xaml.Behaviors 1.1.0.0` as merged | none |
| `HPRebar.g.resources` | **261** entries: 18 Roboto `.ttf`, `themes/generic.baml` (ILRepack-generated, 520 B, merges `/HPRebar;component/materialdesignthemes.wpf/themes/generic.xaml`), toolkit BAML renamed `materialdesignthemes.wpf/themes/*.baml` (89), own views; `MaterialDesignThemes.Wpf.g.resources` / `MaterialDesignColors.g.resources` kept as **empty** stubs | same 261 |
| `;component` URIs inside BAML | **224 / 224 name `HPRebar`** (e.g. `pack://application:,,,/HPRebar;component/MaterialDesignThemes.Wpf/Themes/MaterialDesignTheme.Button.xaml`; font key `…/HPRebar;component/MaterialDesignThemes.Wpf/Resources/Roboto/#Roboto`) | 224 / 224 |
| CLR (`ldstr`) pack URIs | 7: 5 patched (`…HPRebar;component/MaterialDesignThemes.Wpf/Themes/MaterialDesignTheme.{Dark,Light,Shadows}.xaml`, our two theme URIs) + **2 unpatched**: `pack://application:,,,/MaterialDesignThemes.Wpf;component/Resources/Roboto/` and `…/Noto/` (`MaterialDesignFont.cs:10` — folder URIs without `.xaml`, ILRepack's `XamlResourcePathPatcherStep` leaves them) | same |
| `ThemeInfoAttribute` | **missing** before the fix (the Nice3point template never declares one; ILRepack keeps only the primary's attributes) → added `[assembly: ThemeInfo(None, SourceAssembly)]` | present after fix |
| Size | 2 189 824 → **12 660 736 B** (+10.5 MB) | 13 473 280 B |
| ILRepack `/union` collisions / warnings | none in the build log | none |

## Off-Revit runtime gate (`SpikeHost`, scratchpad; loads the merged DLL with `Assembly.LoadFrom`, **no `Application` object**, shows the spike window, in-process checks + screen capture)

| Check | R26 | R24 |
|---|---|---|
| window constructed (`InitializeComponent` merges BundledTheme + Defaults + `Theme.xaml`) | PASS | PASS |
| `MaterialDesignWindow` style applied (background `#FF323232`) | PASS | PASS |
| `PackIcon` (Kind=Play) 24×24 with a template child — `ThemeStyle` set, `Template` set, `generic.xaml` loads with the PackIcon style | PASS | PASS |
| `Card` rendered | PASS | PASS |
| `ComboBox` visual tree has toolkit parts `SmartHint`, `ComboBoxPopup` (implicit style from Defaults, `BasedOn` set) | PASS | PASS |
| legacy `Brush.Accent` (`#FF0696D7`) resolves beside `MaterialDesign.Brush.Primary` (`#FF2196F3`) | PASS | PASS |
| `MaterialDesignFont` **resource key** resolves to the patched pack URI | PASS | PASS |
| ComboBox popup opens (screenshot `s0a-dark-combo-open.png`) | PASS | PASS |
| `SetTheme(Light)` → `MaterialDesign.Brush.Background` `#FFF1F1F1`, screenshot `s0a-light.png`; back to Dark | PASS | PASS |
| **Total** | **15 / 15** | **15 / 15** |

Screenshots: toolkit side re-themes; the legacy panel stays dark because `ThemeDark.xaml` brushes are not derived from the toolkit — exactly what phase 1's `CopyBrushes` is for.

## Findings that change the plan

1. **`ThemeInfo` is mandatory.** Without it `PackIcon`/`Card`/every toolkit custom control has no default style (the #1340 symptom). One attribute in `HPRebar` (and in every other host that repacks). Phase 2 step 1.
2. **`MaterialDesignWindow` sets `FontFamily="{wpf:MaterialDesignFont}"`** (`MaterialDesignTheme.Window.xaml:6`) — a C#-built `FontFamily` whose folder URI ILRepack does not patch; WPF probes `MaterialDesignThemes.Wpf` 15× per layout pass, finds nothing and silently falls back to the system font. Harmless for HP windows because each sets `FontFamily="{DynamicResource Font.Family.Default}"` (Segoe UI) locally (`ColumnRebarView.xaml:13`) — local value beats the style setter. **Rule for phase 1:** the bridge dictionary also defines `MaterialDesignFont` = Segoe UI, and no HP XAML uses `{md:MaterialDesignFont}`. Roboto is not wanted (Revit/AutoCAD dialogs use Segoe UI).
3. `Debug.R24` behaves identically (net462 asset + Behaviors net462), so the net48 verdict is `go` at build + off-Revit level; Revit 2023/2024 runtime stays `[chưa xác minh]` (not installed).
4. Deploying to Revit 2026 for the in-host check was **not done** (would overwrite the user's installed add-in) — phase 2 does it with the real windows.

## Not covered
`Release.R26` (repack identical, `/illink` the same), R27 (blocked by the known Revit 2027 errors), in-Revit modal owner behaviour, `DialogHost`/`Snackbar` (not used).
