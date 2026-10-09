# Plan: revit-xaml-styles → MaterialDesignInXamlToolkit style keys

Status: done (not committed) · Source: /grill-me contract 2026-10-10 (user confirmed "implement")

## Contract
- Output: skill teaches NEW XAML to use toolkit 5.3.2 (MD2) style keys directly; colours stay HP `{DynamicResource Brush.*}`; boilerplate = base only; rule file agrees.
- In scope: `.claude/skills/revit-xaml-styles/SKILL.md`, `references/boilerplate/MaterialBridge.xaml`, `references/styles/controls-sample.md`, `.claude/rules/development-rules.md` (WPF section).
- Out of scope: existing XAML of any host (legacy keys stay; ~130 uses HPRebar, ~85 other hosts), MaterialThemeBridge, palettes, `.agents/` mirror (next `apply`), skill rename.
- Constraints: MD2 5.3.2, Segoe UI override, no `{md:MaterialDesignFont}`, colour only via DynamicResource, MD follows HP palette.
- Assumptions: number input = `MaterialDesignOutlinedTextBox` + `HorizontalContentAlignment=Right`; danger = `MaterialDesignRaisedButton` + `Background={DynamicResource Brush.Danger}`; card = `md:Card`.

## Steps
| # | Step | Status |
|---|---|---|
| 1 | SKILL.md: rules 4–7 + checklist → toolkit keys; section "Project đã có key HP" (legacy table per host, don't mix in one file) | done |
| 2 | Boilerplate MaterialBridge.xaml → CustomColorTheme + MD2 Defaults + Segoe UI only | done |
| 3 | controls-sample.md → toolkit usage sample (no custom ControlTemplates) | done |
| 4 | development-rules.md line "Control Layout" → toolkit keys + Brush.* | done |
| 5 | Verify: every `MaterialDesign*` key named exists in 5.3.2 DLL (negative control); every `Brush.*` in sample palette; XML parse of boilerplate; no legacy key offered for new code | done |
| 6 | Eval: subagent writes a sample window from the skill → only toolkit keys + Brush.* | done |
| 7 | skill-sync check → `equivalent-dual-drift` (mirror rendered with engine adapter) | done |

## Evidence
- Toolkit keys extracted from `~/.nuget/packages/materialdesignthemes/5.3.2/lib/net8.0-windows7.0/MaterialDesignThemes.Wpf.dll` (866 `MaterialDesign*` strings; fake key `MaterialDesignOutlinedNumberBox` absent).
- HP tokens: `references/styles/theme-dark-sample.md`, `HPRebar/HPRebar/Resources/Themes/ThemeDark.xaml`.

## Results
- Static check (`$TEMP/verify_xaml_skill.py`): every `MaterialDesign*` key, `md:` type, assist property, PackIcon kind and `Brush.*` token resolves; no legacy key used outside SKILL §2b; boilerplate + sample window parse as XML. Negative control (fake key + legacy usage injected) caught both.
- PackIcon kinds confirmed via `[Enum]::IsDefined(PackIconKind)`; `WallOutline` does not exist (sample uses `Wall`).
- Eval: subagent wrote a SlabOpening window from the skill → only toolkit keys + `Brush.*`, all resolve.
- Sync: `revit-xaml-styles` equivalent-dual-drift; `grill-me` stays claude-drift — engine flags a correctly rendered grill-me mirror as conflict (normalisation asymmetry around `CLAUDE.md`→`AGENTS.md`, suspected, not fixed).
