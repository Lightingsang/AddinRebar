# BRIEFING — 2026-09-20T22:23:25Z

## Mission
Investigate UI, Theming, Ribbon, Loader & ILRepack Architecture for Smart Plot Pro in HPAutoCad.

## 🔒 My Identity
- Archetype: explorer
- Roles: explorer
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_3
- Original parent: 5a9f631f-8834-48f7-a8a5-72b226a4b480
- Milestone: Smart Plot Pro UI, Theming, Ribbon, Loader & ILRepack Architecture

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- Write only to .agents/explorer_3/ folder
- Use send_message to report back to parent (5a9f631f-8834-48f7-a8a5-72b226a4b480)

## Current Parent
- Conversation ID: 5a9f631f-8834-48f7-a8a5-72b226a4b480
- Updated: 2026-09-20T22:27:00Z

## Investigation State
- **Explored paths**:
  - `HPAutoCad/HPAutoCad.csproj`, `HPAutoCad.Loader/HPAutoCad.Loader.csproj`, `HPAutoCad.Loader/Bundle/PackageContents.xml`
  - `HPAutoCad/Resources/Themes/` (`Theme.xaml`, `MaterialBridge.xaml`, `ThemeDark.xaml`, `ThemeLight.xaml`, `ThemeInfo.cs`, `AutocadHostTheme.cs`, `MaterialThemeBridge.cs`, `ThemeResources.cs`)
  - `HPAutoCad.Loader/` (`AppLoadContext.cs`, `HPAutoCadLoaderApplication.cs`, `HPGeoCommands.cs`, `Ribbon/HPGeoLinkRibbonTab.cs`, `Ribbon/RibbonIcons.cs`)
  - `HPAutoCad/Entry.cs`, `HPAutoCad/HPGeoLink/Commands/HPGeoDialogCommand.cs`
  - `HPAutoCad/HPGeoLink/View/GeoExportWindow.xaml`, `GeoExportWindow.xaml.cs`, `HPAutoCad/HPGeoLink/ViewModel/GeoExportViewModel.cs`
  - `HPAutoCad.McpBridge/BridgeEntry.cs` (modeless window pattern `AcadApp.ShowModelessWindow`)
- **Key findings**:
  - `MaterialDesignThemes` 5.3.2 is ILRepack-merged into `HPAutoCad.dll` via `RepackMaterialDesign` target in `HPAutoCad.csproj`, ensuring zero cross-ALC BAML conflicts.
  - `PdfSharp` (v6.x) does NOT contain BAML and must NOT be repacked into `HPAutoCad.dll`; it is loaded cleanly inside `AppLoadContext` via `HPAutoCad.deps.json` and deployed to `Contents\App\`.
  - Commands must be registered in `HPAutoCad.Loader` via `[CommandMethod("HPSMARTPLOT")]` / `[CommandMethod("HPLOT")]` because `PackageContents.xml` only declares loaders in the Default ALC. `HPAutoCad.Entry.Start` maps delegates to commands.
  - Modeless window via `AcadApp.ShowModelessWindow(window)` requires single-instance tracking and `using (doc.LockDocument())` for all AutoCAD database interactions (including Pick Frame and Scan).
  - Ribbon integration: Add a "Plot" panel (`HPPLOT_PANEL`) to shared tab `HPAUTOCAD_MCP_TAB` with vector icon `Plot` drawn via `RibbonIcons.cs`.
- **Unexplored areas**: All required investigation scope has been explored.

## Key Decisions Made
- Confirmed that `PdfSharp` needs no special repack target, only standard NuGet reference + deployment copy.
- Confirmed single-instance modeless window pattern with `LockDocument` and temporary window minimizing/hiding for pick operations.
- Confirmed loader delegate bridge pattern for `HPSMARTPLOT` / `HPLOT`.
- Confirmed pure vector icon in `RibbonIcons.cs` matching `COLORTHEME` dark/light.

## Artifact Index
- DISPATCH.md — Task assignment and instructions
- BRIEFING.md — Persistent working memory and state
- progress.md — Liveness heartbeat
- handoff.md — Final 5-component handoff report
