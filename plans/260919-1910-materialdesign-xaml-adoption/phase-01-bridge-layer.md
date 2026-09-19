---
phase: 1
title: "Bridge layer — legacy tokens over MaterialDesign, host theme sources"
status: completed
priority: P1
effort: "8h"
dependencies: []
---

# Phase 1: Bridge layer

> **Done 2026-09-19** (HPRebar vocabulary; the two other vocabularies are copied in phases 3–5 when they can compile) — [reports/phase-01-token-map.md](reports/phase-01-token-map.md). Built the reverse of the plan's brush copy: **MaterialDesign follows the HP palette** (`Theme.Background/Foreground/…` from `Color.*`), and the apply is a **top-level overlay swap** because a nested dictionary mutation does not repaint a shown window.

## Overview
Make the toolkit and the hand-written dictionaries coexist: after this phase a window that merges `MaterialThemeBridge` sees the toolkit's implicit styles **and** still resolves every legacy `Brush.*` / `Spacing.*` / `Font.*` / style key, so each view migrates on its own schedule and the last step of every host is deleting the old dictionary. Also wires dark/light to each host's real source and keeps open windows in sync (today none re-themes after its constructor — [inventory §1](research/inventory.md#1-hprebarhprebar-revit-add-in)).

## Requirements
- Functional: (1) `MaterialThemeBridge.Attach(Window, IHostTheme)` merges `CustomColorTheme` (colours from the user decision) + `MaterialDesign2.Defaults.xaml` + the host's `MaterialBridge.xaml`, applies `MaterialDesignWindow`, sets the base theme from the host and re-applies on the host's change event; (2) `MaterialBridge.xaml` defines every legacy key of that vocabulary in terms of MaterialDesign resources; (3) brushes are **copied**, not aliased, after every `SetTheme` (frozen-brush replacement, [toolkit-analysis §3](research/toolkit-analysis.md#3-theme-objects-and-the-run-time-darklight-switch)).
- Non-functional: C# file < 120 lines, no `Application.Current`, no host API inside the bridge (the host adapter is a 20-line class per host implementing `IHostTheme { bool IsDark { get; } event Action? Changed; }`), `{DynamicResource}` in views unchanged, `McpShared` untouched.

## Architecture
```
Window.Resources.MergedDictionaries
  [0] CustomColorTheme (IMaterialDesignThemeDictionary)   ← SetTheme(BaseTheme) on host change
  [1] MaterialDesign2.Defaults.xaml                        ← implicit styles (Button, ComboBox, TextBox, DataGrid, …)
  [2] MaterialBridge.<vocab>.xaml                          ← legacy keys: Spacing.*, Font.*, Radius.*, style keys BasedOn toolkit styles
  [3] (until the host finishes) the old Theme.xaml         ← wins for keys it still defines; deleted at the end of the host phase
MaterialThemeBridge.CopyBrushes(rd): Brush.Background ← MaterialDesign.Brush.Background, Brush.Surface ← …Card.Background,
  Brush.Foreground.Primary ← …Foreground, Brush.Foreground.Secondary ← …ForegroundLight, Brush.Border ← …TextBox.Border,
  Brush.Accent ← …Primary, Brush.Accent.Foreground/OnAccent ← …Primary.Foreground, Brush.Danger ← …ValidationError,
  Brush.Success/Warning/Info ← fixed swatches (Green/Amber/Blue from MaterialDesignColors, adjusted per base theme)
  — runs once after Attach and again in IThemeManager.ThemeChanged.
```
Mapping table lives in the phase report (`reports/phase-01-token-map.md`) and is pinned by a unit test per vocabulary: every key of the old dictionary must be produced by `MaterialBridge.xaml` ∪ `CopyBrushes`.

Host adapters (each ≤ 25 lines, in the host's `Service/`): `RevitHostTheme` (`UIThemeManager.CurrentTheme` under `#if REVIT2024_OR_GREATER`, `Application.ThemeChanged` forwarded from `Application.cs`'s existing subscription through a static event — `Application.cs` itself is not reformatted; one added line raising the event is allowed), `AutocadHostTheme` (`COLORTHEME` + `Application.SystemVariableChanged`, filtered on `Name == "COLORTHEME"`, marshalled through `Application.Idle` as the ribbon already does), `WindowsHostTheme` (`Theme.GetSystemTheme()` + `SystemEvents.UserPreferenceChanged`; used by Navisworks and ETABS, ETABS with a `settings.json` `Theme: light|dark|system` override), `HPGeoHostTheme` (= AutoCAD's, modal window so `Changed` unused).

## Related Code Files
- Create (HPRebar vocabulary): `HPRebar/HPRebar/Resources/Themes/MaterialBridge.xaml`, `HPRebar/HPRebar/Resources/Themes/MaterialThemeBridge.cs`, `HPRebar/HPRebar/Resources/Themes/IHostTheme.cs`, `HPRebar/HPRebar/Resources/Themes/RevitHostTheme.cs` (shared cross-feature → `Resources/`, like `RibbonIcons.cs`); linked into `HPRebar.McpBridge` the way `RibbonIcons.cs` is (`HPRebar.McpBridge.csproj:43`).
- Create (bridge vocabulary, one copy per host — folders never reference each other): `HPAutoCad/HPAutoCad.McpBridge/Resources/Themes/MaterialBridge.xaml` + `Service/MaterialThemeBridge.cs` + `Service/AutocadHostTheme.cs`; mirrored to `HPCivil3d/…` (phase 4); `HPNavis/HPNavis.McpBridge/…` + `Service/WindowsHostTheme.cs`; `HPEtabs/HPEtabs.McpBridge/…` + `Service/WindowsHostTheme.cs`.
- Create (HPGeo vocabulary): `HPGeo/HPGeo.AutoCad/UI/MaterialBridge.xaml`, `UI/MaterialThemeBridge.cs`, `UI/HPGeoHostTheme.cs`.
- Create tests: `HPRebar/HPRebar.Core.Tests/…`? **No** — Core must not reference WPF; the mapping test is a pure-string test reading both XAML files as text (keys of `ThemeDark.xaml` ⊆ keys of `MaterialBridge.xaml` ∪ `CopyBrushes` list) and lives in the existing WPF-free test project of each host: `HPAutoCad/HPAutoCad.Mcp.Server.Tests` already reads seed text; for HPRebar use `HPRebar.Core.Tests` with a text fixture (no WPF reference needed).
- Modify: nothing in views yet; csproj `PackageReference MaterialDesignThemes 5.3.2` added per host **in that host's phase**, not here (phase 1 code compiles against the package only where it is added — HPRebar in phase 2 is the first).
- Create once, copied per host in its phase: the **`RepackMaterialDesign` MSBuild target** (S0-B, text in [spike-s0b-two-alc.md](reports/spike-s0b-two-alc.md)) and the one-line `[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]` (S0-A) — both go beside `MaterialThemeBridge.cs` in each host; HPRebar itself needs only the attribute (Nice3point repacks the whole output).

## Implementation Steps
1. Write `MaterialBridge.HPRebar.xaml`: `Spacing.*` (20 keys, values unchanged), `Font.Family.*`, `Font.Size.*` (map to MD type scale only where equal; keep numbers), **`MaterialDesignFont` redefined as Segoe UI** (S0-A: under a merge the toolkit's own Roboto folder URI never loads; every HP window also keeps its explicit `FontFamily="{DynamicResource Font.Family.Default}"` and no HP XAML uses `{md:MaterialDesignFont}`), style keys `PrimaryButton` `BasedOn MaterialDesignRaisedButton`, `SecondaryButton` → `MaterialDesignOutlinedButton`, `DangerButton` → raised + `Background=Brush.Danger`, `IconButton` → `MaterialDesignIconButton`, `LinkButton` → `MaterialDesignFlatButton`, `StandardTextBox`/`NumberTextBox`/`SearchTextBox` → `MaterialDesignOutlinedTextBox` (+ `HintAssist`), `Card` → `materialDesign:Card`-like `Border` (keeps the `Style TargetType=Border` contract), `Separator/Badge/BadgeText/Tag`, `Caption/Body/…/Title` `BasedOn MaterialDesignBody2/Body1/Subtitle1/Headline6…`; `ThemeIcons.xaml` untouched (vector templates).
2. Write `MaterialThemeBridge.cs` (Attach / Apply / CopyBrushes / Detach on `Closed`), `IHostTheme.cs`, host adapters.
3. Same for the bridge vocabulary (`Brush.Accent.Foreground`, `Radius.Card`, `Card`, `ButtonBase/PrimaryButton/SecondaryButton/LinkButton`, `StandardTextBox`, implicit `CheckBox` → `MaterialDesignCheckBox`) and HPGeo (`Brush.Text/TextMuted/Input/AccentText`, `Spacing.XS/S/M`, `Padding.Control`, `Margin.*`, implicit `ComboBox` → **drop the local style; `MaterialDesignComboBox` from Defaults takes over = the gap fix**, `DataGrid*` → `MaterialDesignDataGrid*`).
4. Token-map report + the three text tests.
5. Decide `Brush.Success/Warning/Info` values per base theme (dark needs lighter swatches) and record them in the report.

## Success Criteria
- [ ] Mapping tests pass for the 3 vocabularies (every legacy key produced).
- [ ] `MaterialThemeBridge.cs` < 120 lines, host adapters < 25 lines, no `Application.Current`, no host API in the bridge.
- [ ] `reports/phase-01-token-map.md` lists old key → new source for all 3 vocabularies.
- [ ] Nothing compiled against the toolkit yet outside the spike branches (the files are added to the host csproj in phases 2–5); `git status` shows only new files + tests.

## Risk Assessment
- Legacy `Font.Size.*` numbers vs MD type scale: keep our numbers (Revit dialogs are 12–13 px); MD3 typography would change every label → one more reason for MD2.
- `Card` today is a `Border` style; `materialDesign:Card` is a control — keep the `Border` style (BasedOn nothing, brushes from MD) to avoid touching 15 tab views.
- `SetTheme` animates non-frozen brushes for 300 ms; `CopyBrushes` must run **after** the animation target is set (the `ThemeChanged` event fires synchronously inside `SetTheme` — copy the target colour, not the animating brush).

## Next Steps
Phase 2 adds the package to HPRebar, merges the bridge into the three windows, and deletes the four `ThemeSwitcher` copies.
