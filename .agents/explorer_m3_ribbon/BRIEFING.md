# BRIEFING — 2026-09-20T13:48:00Z

## Mission
Formulate the exact specification for the shared Ribbon tab (`HPAUTOCAD_MCP_TAB`) and `HPGEOLINK_PANEL` for Milestone M3.

## 🔒 My Identity
- Archetype: Teamwork explorer
- Roles: explorer, analyst
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m3_ribbon
- Original parent: 050984c1-afaa-4911-859c-331e9279dc4f
- Milestone: M3 (Shared Ribbon Tab Integration)

## 🔒 Key Constraints
- Read-only investigation — do NOT implement production code
- Design shared Ribbon tab and HPGEOLINK_PANEL specifications for M3
- Preserve AutoCAD/Civil 3D mirror invariant (`HPAutoCad.McpBridge.Loader` must remain intact or compatible)
- Write comprehensive plan to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m3_ribbon_plan.md`

## Current Parent
- Conversation ID: 050984c1-afaa-4911-859c-331e9279dc4f
- Updated: not yet

## Investigation State
- **Explored paths**:
  * `HPAutoCad/HPAutoCad.McpBridge.Loader/Ribbon/{McpRibbonTab, RibbonCommandHandler, RibbonIcons}.cs`
  * `HPGeo/HPGeo.AutoCad.Loader/Ribbon/{HPGeoRibbonTab, RibbonIcons}.cs`
  * `HPGeo/HPGeo.AutoCad.Loader/HPGeoCommands.cs`
  * `HPAutoCad/HPGeoLink/Commands/*.cs` (`HPGeoDialogCommand`, `HPGeoImportCommand`, `HPGeoKmzScriptCommand`, `HPGeoImageScriptCommand`, `HPGeoInfoCommand`, `HPGeoImportScriptCommand`)
  * `HPCivil3d/tools/mirror-tokens.json` (Mirror invariant verified)
  * `plans/260914-2204-autocad-ribbon-tab/reports/ribbon-code-review.md` (AdWindows API constraints, fill rule, dispatcher)
  * `HPAutoCad/tools/harness/run-ribbon-check.ps1` (Automation harness verification pattern)
- **Key findings**:
  * Shared Tab ID: `HPAUTOCAD_MCP_TAB`, Title: `HPAutoCad`.
  * Dedicated Panel ID: `HPGEOLINK_PANEL`, Title: `HPGeoLink`.
  * Dual-add-in coexistence protocol operates flawlessly: either creates tab, both add only their own panel, theme flip rebuilds only own panel (`removeEmptyTab: false`), unload removes tab only if empty (`tab.Panels.Count == 0`).
  * Civil 3D mirror parity (`HPCivil3d.McpBridge.Tests`) is 100% safeguarded because `HPAutoCad.McpBridge.Loader/` remains untouched.
  * Vector icons: 32×32 box with even coordinates provides razor-sharp 16×16 downsampling for AdWindows standard items.
  * Theme ink switching: `#E6E6E6` on `COLORTHEME=0`, `#3C3C3C` on `COLORTHEME=1`, `#0696D7` accent.
  * SplitButton: `HPGEO_SECONDARY_SPLIT` with `IsSplit=true` gives 1-click access to `HPGEOIMPORT` while providing a menu of 4 secondary tools (`Map`, `Info`, `-KMZ`, `-Import`).
- **Unexplored areas**: None. Specification is complete and ready for implementation.

## Key Decisions Made
- Placed all new Ribbon components into `HPAutoCad/HPAutoCad.Loader/Ribbon/` (Default ALC).
- Kept `HPAutoCad.McpBridge.Loader` unchanged to preserve Civil 3D mirror invariant.
- Implemented `RibbonSplitButton` (`IsSplit = true`) with primary action `HPGEOIMPORT` and dropdown menu containing `HPGEO_IMAGE`, `HPGEO_INFO`, `HPGEO_KMZ_SCRIPT`, `HPGEO_IMPORT_SCRIPT`.
- Implemented pure vector `DrawingImage` icons in `RibbonIcons.cs` with even-odd cutouts.

## Artifact Index
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\m3_ribbon_plan.md` — Comprehensive specification for M3 Ribbon integration
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m3_ribbon\handoff.md` — Agent handoff report
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m3_ribbon\progress.md` — Progress tracker
