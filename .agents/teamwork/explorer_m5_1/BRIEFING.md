# BRIEFING — 2026-10-17T03:30:00Z

## Mission
Investigate and design the AutoCAD 2026 (.NET 8) add-in architecture for HPKata.AutoCad across project configuration, lifecycle, RegApp registration, WPF host integration, and clean modular command partitioning.

## 🔒 My Identity
- Archetype: teamwork_preview_explorer
- Roles: AutoCAD 2026 .NET 8 Architecture Explorer (M5)
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m5_1\
- Original parent: 4300a9ce-e3eb-419c-8493-f2001d2d04d6
- Milestone: M5

## 🔒 Key Constraints
- Read-only investigation — do NOT implement or modify source code
- Strictly read-only access to C:\kata_pro
- Never inspect, decompile, or touch Security.dll or licensing code
- Clean code governance: Rule C2 (classes <= 300 lines), Rule M3 (methods <= 50 lines), Rule Q-B1 (no dead code), Rule Q-B2 (no unannotated empty catch)
- Write outputs only to working directory .agents/teamwork/explorer_m5_1/

## Current Parent
- Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6
- Updated: 2026-10-17T03:30:00Z

## Investigation State
- **Explored paths**:
  - `HPKata/HPKata.slnx`, `global.json`, `Directory.Build.props`
  - `HPKata.Core/`: Models, Shapes, Configuration, Serialization, Optimization, Scheduling, Tagging
  - `HPKata.UI/`: Views (`SetupView`, `DetailingView`, `ScheduleView`, `CuttingStockView`), ViewModels, `ThemeManager.cs`, `IHostTheme.cs`
  - `HPAutoCad/`: `HPAutoCad.Loader` (ALC, `ExtensionApplication`, Ribbon creation, `AutocadHostTheme`), `HPAutoCad.csproj`
  - `C:\kata_pro\`: `Goi lenh.lsp` (lines 1-150 aliases, 152-237 config), `menukata.cui` (MenuGroup MENUKATA), `Ribbon_Command_Clicks.txt`
  - Blueprint: `docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md` (§1, §2, §3, §10)
  - Standards: `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/clean-code/host-appendix/autocad-civil.md`
- **Key findings**:
  - `AutoCAD.NET 25.1.0` (.NET 8) requires `ExcludeAssets="runtime" PrivateAssets="all"` to avoid copying host binaries (`AcCoreMgd.dll`, `AcDbMgd.dll`, `AcMgd.dll`).
  - Dynamic loading requires `<EnableDynamicLoading>true</EnableDynamicLoading>` and careful ALC isolation to prevent dependency collisions.
  - RegApp `"KATA"` must be registered in the active document and all new/opened documents via `DocumentCreated` and `DocumentActivated` events.
  - WPF views (`SetupView`, `DetailingView`, `ScheduleView`, `CuttingStockView`) can be safely hosted via `Application.ShowModalWindow()` or modeless `Application.ShowModelessWindow()` with `ThemeManager.Attach(window, AutocadHostTheme.Instance)` and mandatory `doc.LockDocument()` during CAD interactions.
  - Command partitioning: thin command classes mapped to modular domain controllers ensuring zero files exceed 300 lines (Rule C2) and methods <= 50 lines (Rule M3).
- **Unexplored areas**: None for M5 architecture scope.

## Key Decisions Made
- Project layout: `HPKata.AutoCad` project configured with `net8.0-windows`, referencing `AutoCAD.NET 25.1.0`, `HPKata.Core`, and `HPKata.UI`.
- Lifecycle architecture: `HPKataExtensionApplication : IExtensionApplication` managing `RegAppManager`, `RibbonManager`, and `AutocadHostTheme`.
- Controller decomposition: 7 focused controllers (`SetupController`, `RebarDrawController`, `RebarEditController`, `RebarLinkController`, `ScheduleController`, `CuttingStockController`, `TagController`) isolating business/CAD operations from command registrations.

## Artifact Index
- `DISPATCH.md` — Task assignment and incoming messages
- `analysis.md` — Comprehensive architectural investigation report for HPKata.AutoCad
- `handoff.md` — 5-component handoff report for M5 implementer
- `progress.md` — Heartbeat and milestone checklist
