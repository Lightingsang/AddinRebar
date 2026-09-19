---
title: "MaterialDesignInXAML 5.3.2 adoption across the 7 HP WPF surfaces (Revit, Revit MCP bridge, AutoCAD, Civil 3D, Navisworks, ETABS, HPGeo)"
description: "Replace the hand-written Brush.*/Spacing.* theme dictionaries with MaterialDesignThemes 5.3.2 behind a per-host bridge layer, host by host, each step gated by a build, a harness and a rollback. Plan only — no package added, no file outside plans/ touched."
status: in-progress
priority: P2
effort: 58h (phase 0 spikes 10h; 48h migration)
branch: RebarVersion1
tags: [wpf, xaml, materialdesign, theme, revit, autocad, civil3d, navisworks, etabs, hpgeo, ilrepack, alc]
created: 2026-09-19
revised: 2026-09-19
blockedBy: []
blocks: []
related: [260912-1521-dynamic-revit-mcp-server-2026, 260919-1608-express-tools-port-autocad-mcp]
---

# MaterialDesignInXAML adoption — 6 hosts, 7 WPF surfaces

> Đây là bước lập kế hoạch. Chưa thay đổi source code. No `dotnet add package`, no restore, no build was run.

Research: [toolkit-analysis.md](research/toolkit-analysis.md) (every toolkit claim + URL) · [inventory.md](research/inventory.md) (12 projects, `path:line`) · [compatibility-matrix.md](research/compatibility-matrix.md) (verdicts + spike gates).

## Locked facts (verified)

- Toolkit **5.3.2**, MIT, assets `net462` / `net8.0-windows7.0` / `net10.0-windows7.0`, deps `MaterialDesignColors 5.3.2` + `Microsoft.Xaml.Behaviors.Wpf ≥ 1.1.77`; 10 MB DLL embedding 89 resources incl. 18 Roboto fonts and `themes/generic.baml`.
- Add-ins have no `Application.Resources` → theme is merged **per window** (`BundledTheme`/`CustomColorTheme` + `MaterialDesign2.Defaults.xaml`, `Style=MaterialDesignWindow`) and switched with `ResourceDictionaryExtensions.SetTheme` on that window's `IMaterialDesignThemeDictionary`; `PaletteHelper` throws without `Application.Current` (source). HPEtabs (`App.xaml`) is the one textbook app.
- `SetTheme` replaces frozen `MaterialDesign.Brush.*` brushes → a `StaticResource` alias for our `Brush.*` keys goes stale; the bridge re-copies brushes on `IThemeManager.ThemeChanged` (~40 lines C#, per host).
- Three token vocabularies (HPRebar family · bridge family · HPGeo) → three alias dictionaries; `McpShared/…Core` stays WPF-free; HPGeo never references `McpShared` → the helper is copied per host (today: 4 `ThemeSwitcher` copies).
- **Phase-0 verdicts (2026-09-19):** (1) ILRepack merges the toolkit cleanly — every BAML pack URI rewritten, fonts + `generic.baml` merged, 15/15 off-Revit on R26 and R24 — **but the primary needs `[assembly: ThemeInfo(None, SourceAssembly)]`** (Nice3point never had one; without it `PackIcon`/`Card` have no default style) and **`MaterialDesignWindow`'s `FontFamily="{wpf:MaterialDesignFont}"` can never load under a merge** (folder URI ILRepack does not patch) → every HP window keeps its explicit Segoe UI `FontFamily` and the bridge dictionary pins `MaterialDesignFont` to Segoe UI. (2) Two loose copies in two ALCs of one acad.exe **mix**: WPF binds BAML to the last-loaded `MaterialDesignThemes.Wpf` regardless of `EnterContextualReflection` (HPGeo window got the bridge's `SmartHint`) → **the toolkit is repacked into every add-in assembly** (`RepackMaterialDesign` target, obj DLL as primary input, loose DLLs deleted): HPAutoCad, HPCivil3d, HPGeo, the Revit MCP bridge and HPNavis alike; HPRebar keeps Nice3point's whole-output repack. (3) net48 binds by strong name and needs no allow-list change; repacked anyway for symmetry.

## Phases

| Phase | File | Scope | Gate | Effort | Status |
|---|---|---|---|---|---|
| 0 — Spikes | [phase-00-spikes.md](phase-00-spikes.md) | S0-A ILRepack (R26 then R24), S0-B two ALCs (AutoCAD + HPGeo), S0-C net48 resolver (Navis); throw-away worktree, nothing merged | S0-A `go` (15/15 ×2), S0-B loose `no-go` → **repack `go`** (8/8 ×2), S0-C `go` (repack 6/6) — [reports/](reports/) | 10h | **completed 2026-09-19** |
| 1 — Bridge layer | [phase-01-bridge-layer.md](phase-01-bridge-layer.md) | `MaterialThemeBridge` (C#) + `MaterialBridge.xaml` per vocabulary: legacy `Brush.*`/`Spacing.*`/style keys resolve to MaterialDesign resources; host theme sources wired (Revit `ThemeChanged`, AutoCAD `SystemVariableChanged COLORTHEME`, Windows `AppsUseLightTheme` for Navis/ETABS, settings override) | old XAML renders unchanged on top of the toolkit; unit tests for the mapping table | 8h | **completed 2026-09-19** |
| 2 — HPRebar (Revit) | [phase-02-hprebar-revit.md](phase-02-hprebar-revit.md) | `Debug.R26` first, then R23/R24 (net48), then R27 (blocked by the known Revit 2027 API errors) | build per config with `-p:DeployAddin=false`; 334 Core tests; F5 walk of the 3 windows + 16 tabs on both Revit themes (user) | 14h | **completed 2026-09-19** (Revit-verified) |
| 3 — Revit MCP bridge + ETABS | [phase-03-mcpbridge-etabs.md](phase-03-mcpbridge-etabs.md) | `HPRebar.McpBridge` window (linked pages), `HPEtabs` `App.xaml` + window | bridge live in Revit 2026; `HPEtabs run-live-verify.ps1 -Phase bridge` | 6h | planned |
| 4 — AutoCAD family | [phase-04-autocad-family.md](phase-04-autocad-family.md) | HPAutoCad → port to HPCivil3d + re-pin `mirror-tokens.json` → HPGeo (dark `ComboBox` gap closed) | `run-ribbon-check.ps1` ×2, MirrorTests 55, `dialog-check.ps1`, `acceptance.ps1` | 12h | planned |
| 5 — Navisworks | [phase-05-navisworks.md](phase-05-navisworks.md) | repack (net48 ILRepack) + net462 assets + window | `HPNavis run-ribbon-check.ps1 -WithNoDoc`, `run-bridge-unattended.ps1`, 135 + 49 tests | 8h | planned |

Order rationale: phase 0 de-risks the two hard hosts before any migration; phase 1 lets old and new XAML coexist so every later phase is a per-window swap with a one-file rollback; Revit first because it is the product, bridges next because each is one window, HPGeo inside the AutoCAD phase because it shares the S0-B answer and the shared ribbon tab.

## Invariants every phase keeps

`{DynamicResource}` for every brush/spacing/font in views (`StaticResource` only for `BasedOn` and toolkit style keys inside dictionaries, as today) · code-behind = `InitializeComponent` + `DataContext` + the one theme call + `CloseRequested += Close` · feature-folder convention (`Service/` holds the bridge helper) · C# < 300 / XAML < 500 lines · one shared `HPAutoCad` ribbon tab · `Application.cs` / `StartupCommand.cs` untouched · loaders carry no NuGet dependency · `McpShared` never references the toolkit · every AutoCAD-side UI change ported to Civil 3D and re-pinned · build with `-p:DeployAddin=false` / `-p:DeployBundle=false` / `-p:DeployPlugin=false` while a host is open.

## User decisions (product, not engineering) — confirmed 2026-09-19 ("có" = all three recommendations)

1. **MD2** defaults (`MaterialDesign2.Defaults.xaml`). 2. **`CustomColorTheme`** primary `#0696D7` (the accent every host already uses), secondary `#E0641E` (steel); `Inherit` rejected because Revit/AutoCAD dialogs do not follow the Windows accent. 3. **Phases 0–2 first** (spikes, bridge layer, Revit), gate, then 3 → 4 → 5; all six hosts stay in the plan.

## Rollback (global)

Every phase is one commit per host; rollback = revert the host's commit(s) + delete the deployed folder (`%AppData%\Autodesk\Revit\Addins\<ver>\`, `…\ApplicationPlugins\*.bundle\`, `…\Navisworks Manage 2026\Plugins\HPNavis.McpBridge\`) and rebuild the previous commit with the host closed. Phase-0 spikes live on throw-away branches and are never merged.
