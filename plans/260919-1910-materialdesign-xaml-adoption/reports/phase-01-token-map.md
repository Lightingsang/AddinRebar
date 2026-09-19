# Phase 1 — bridge layer as built (HPRebar vocabulary, 2026-09-19)

## Design change vs the plan (engineering call, recorded here)
The plan derived the legacy `Brush.*` tokens **from** MaterialDesign (`CopyBrushes` after `SetTheme`). Built the other way round: the hand-tuned HP palette (`ThemeDark.xaml` / `ThemeLight.xaml`, 26 `Color.*` + 26 `Brush.*` + 6 canvas each) stays the source of truth and **MaterialDesign follows it** — `Theme.Create(base, primary, secondary)` then `Background / Foreground / ForegroundLight / ValidationError / Cards.Background / Cards.Border / ToolTips.Background` are assigned from the `Color.*` tokens before `SetTheme`. No colour math, no second palette to keep in step, and the accent is identical by decision (`#0696D7`). Primary/secondary come from the `CustomColorTheme` declared in `MaterialBridge.xaml` (`#0696D7` / `#E0641E`).

## Finding that shaped the code: nested dictionary mutations do not repaint
`MaterialThemeBridge.Apply` first swapped `ThemeDark.xaml` inside `Theme.xaml` and mutated the nested `CustomColorTheme` (like the old `ThemeSwitcher`, which only ever ran before `Show()`). The gallery proved the lookup changed (`Color.Background=#F8F8F8`) while the tree kept the old brushes (`window.Background=#1E1E1E`): WPF's resource-change invalidation does not reach a shown tree from a nested dictionary swap. Fix = build one **top-level overlay** (palette dictionary + the MaterialDesign brushes written by `overlay.SetTheme(theme)`), marked `HPRebar.ThemeOverlay`, and replace it in `window.Resources.MergedDictionaries` — one top-level replacement re-evaluates every `{DynamicResource}`. The `ThemeDark.xaml` merged by `Theme.xaml` remains the initial (design-time) state.

## Files (`HPRebar/HPRebar/Resources/Themes/`, shared cross-feature like `RibbonIcons.cs`)
| File | Role |
|---|---|
| `MaterialBridge.xaml` (77 lines) | merges `CustomColorTheme` + `MaterialDesign2.Defaults.xaml`; `MaterialDesignFont` = Segoe UI; legacy style keys re-based: `PrimaryButton` → `MaterialDesignRaisedButton`, `SecondaryButton` → `MaterialDesignOutlinedButton`, `DangerButton` → raised + `Brush.Danger`, `IconButton` → `MaterialDesignIconButton` 32×32, `LinkButton` → `MaterialDesignFlatButton`, `StandardTextBox` → `MaterialDesignOutlinedTextBox` (Padding 8,6 · MinHeight 32 · corner 4), `NumberTextBox` (right, mono), `SearchTextBox` (+ `HintAssist.Hint`) |
| `MaterialThemeBridge.cs` (81 lines) | `Attach(Window, IHostTheme)` (apply + follow `Changed` on the window dispatcher until `Closed`), `Apply(Window, bool dark)` (overlay build + top-level swap) |
| `IHostTheme.cs` | `bool IsDark`, `event Action? Changed` |
| `RevitHostTheme.cs` | singleton; `UIThemeManager.CurrentTheme` under `#if REVIT2024_OR_GREATER` (else dark); `NotifyChanged()` raised by `Application.cs`'s existing `ThemeChanged` handler (+1 line, the template file is otherwise untouched) |
| `ThemeInfo.cs` | `[assembly: ThemeInfo(None, SourceAssembly)]` (S0-A) |

`Theme.xaml` now merges `MaterialBridge.xaml` **first**, then `ThemeDark.xaml`, `Typography.xaml`, `Spacing.xaml`, `Controls.xaml`, `ThemeIcons.xaml`; `Buttons.xaml` / `TextBoxes.xaml` are no longer merged by HPRebar (the files stay on disk until phase 3 because `HPRebar.McpBridge` links them by glob and its own `Theme.xaml` still lists them). Windows keep their XAML unchanged (they merge `Theme.xaml` as before); code-behind swaps `ThemeSwitcher.ApplyFromRevit(this)` for `MaterialThemeBridge.Attach(this, RevitHostTheme.Instance)`. The three `ThemeSwitcher` copies under the feature folders are deleted; the bridge's fourth copy waits for phase 3.

## Token map (legacy key → where it now comes from)
| Legacy key(s) | Source now |
|---|---|
| `Brush.*` (26) + `Brush.Canvas.*` (6), `Color.*` | unchanged: `ThemeDark.xaml` / `ThemeLight.xaml`, swapped inside the overlay |
| `Spacing.*` (20), `Font.Family.*`, `Font.Size.*`, `Caption/Body/BodyStrong/Subheading/Heading/Title`, `Card/Separator/Badge/BadgeText/Tag`, `TabIcon.*` | unchanged dictionaries (`Spacing.xaml`, `Typography.xaml`, `Controls.xaml`, `ThemeIcons.xaml`) |
| `PrimaryButton`, `SecondaryButton`, `DangerButton`, `IconButton`, `LinkButton` | `MaterialBridge.xaml`, `BasedOn` toolkit button styles |
| `StandardTextBox`, `NumberTextBox`, `SearchTextBox` | `MaterialBridge.xaml`, `BasedOn MaterialDesignOutlinedTextBox` |
| implicit `ComboBox`, `CheckBox`, `RadioButton`, `TabControl`, `Expander`, `DataGrid`, `ScrollBar`, `ToolTip`, … | `MaterialDesign2.Defaults.xaml` (no legacy implicit styles existed) |
| `MaterialDesign.Brush.Background / Foreground / ForegroundLight / ValidationError / Card.Background / Card.Border / ToolTip.Background` | derived from `Color.Background / Foreground.Primary / Foreground.Secondary / Danger / SurfaceElevated / Border / SurfaceElevated` |
| `MaterialDesign.Brush.Primary[.Light/.Dark]`, `Secondary…` | `CustomColorTheme` `#0696D7` / `#E0641E` |

## Tests
`HPRebar.Core.Tests/Themes/ThemeTokenCoverageTests.cs` (2, text-level, WPF-free): every `{DynamicResource}`/`{StaticResource}` key used by any XAML under `HPRebar/HPRebar/` is defined by a dictionary `Theme.xaml` merges (or the toolkit, or the same file); `ThemeDark` and `ThemeLight` define the same key set; `MaterialBridge.xaml` is merged before `ThemeDark.xaml`, `Buttons.xaml`/`TextBoxes.xaml` are not merged, the 8 legacy style keys and the `MaterialDesignFont` override exist, no `{md:MaterialDesignFont}` markup. Core tests **336/336**.

## Deferred to the host phases
The bridge-family (`Brush.Accent.Foreground`, `Radius.Card`, …) and HPGeo vocabularies get their own `MaterialBridge.xaml` + `MaterialThemeBridge.cs` copies in phases 3–5, when their csproj gains the package and the copy can be compiled and rendered — writing them now would leave dead, untestable files (YAGNI). The `RepackMaterialDesign` target text lives in [spike-s0b-two-alc.md](spike-s0b-two-alc.md).
