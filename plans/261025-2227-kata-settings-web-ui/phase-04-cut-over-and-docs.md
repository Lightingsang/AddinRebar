# Phase 04 — Cut-over + docs

Priority: medium · Status: DONE 2026-10-25 · Type: AFK (scope set by decision A)

## Delete (decision A = recommended)
- `KataExport/View/KataSettingsView.xaml(.cs)`, `View/Tabs/Kata*SettingsTab.xaml(.cs)`, `KataJointDefaultsTab.xaml(.cs)`,
  `ViewModel/KataSettingsViewModel.cs`, `ViewModel/Tabs/Kata*Settings*ViewModel.cs`, `KataShopTableRows.cs`, `KataJointColumnViewModel`.
- `Application.cs`: Kata Settings button inside `#if REVIT2026`; `KataSettingsCommand` compiled R26 only (csproj `Compile Remove`).
- Theme token coverage test still green (removed XAML keys).

## Docs
- ADR-0013 "Kata Settings web page" (or amend ADR-0011); CLAUDE.md HPRebar paragraph (+ same text by hand into AGENTS.md); install guide line.

## Success
- `dotnet build HPRebar.slnx -c Debug.R26`; Core.Tests + Mcp.Server.Tests green.

## Result
- `git rm` 13 files: `View/KataSettingsView.xaml(.cs)`, `View/Tabs/Kata{Detail,Special}SettingsTab.xaml(.cs)`, `View/Tabs/KataJointDefaultsTab.xaml(.cs)`,
  `ViewModel/KataSettingsViewModel.cs`, `ViewModel/Tabs/*` (4). `View/Tabs/` and `ViewModel/Tabs/` gone.
- `KataSettingsCommand` opens only the web window; csproj `Compile Remove` when RevitVersion ≠ 2026; `Application.cs` button inside `#if REVIT2026`
  (`// Multi-version:` tag). The `KATA_ONLY` ribbon variant still has no settings button (unchanged).
- Docs: ADR-0013 + ADR index; CLAUDE.md and AGENTS.md (same text inserted by hand: Kata paragraph + `settings.html` in the page list);
  `.claude/rules/development-rules.md` UI exception names Kata Settings (ADR-0013).
- Checks: `dotnet build HPRebar/HPRebar.csproj -c Debug.R26|Debug.R25 -p:DeployAddin=false` ✅ (41 warnings, unchanged), Core.Tests 1695/1695 (theme token coverage incl.).
