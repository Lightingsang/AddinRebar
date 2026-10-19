## 2026-10-17T03:25:03Z

# Dispatch: Explorer M5-1 (AutoCAD 2026 .NET 8 Architecture & Command Routing)

**Agent**: `explorer_m5_1` (`teamwork_preview_explorer`)
**Parent**: `orchestrator_16` (`4300a9ce-e3eb-419c-8493-f2001d2d04d6`)
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m5_1\`
**Date**: 2026-10-17


## Mandatory Reading
1. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`
2. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\hpkata\HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md` (Sections 1, 2, 3, 10)
3. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`
4. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\clean-code\HP_CLEAN_CODE_CORE.md`

## Mission
Investigate and design the AutoCAD 2026 (.NET 8) add-in architecture for `HPKata.AutoCad`:
1. Project configuration: `HPKata.AutoCad.csproj` targeting `net8.0-windows`, referencing `AutoCAD.NET` 25.1.0 (`ExcludeAssets="runtime" PrivateAssets="all"`), `HPKata.Core`, and `HPKata.UI`.
2. Lifecycle management: `IExtensionApplication` (`Initialize` and `Terminate`), registering RegApp `"KATA"`, handling document creation/activation, Ribbon/Menu integration.
3. WPF Host Integration: How to safely launch WPF dialogs (`SetupView`, `DetailingView`, `ScheduleView`, `CuttingStockView` from `HPKata.UI`) from AutoCAD commands using `Autodesk.AutoCAD.ApplicationServices.Application.ShowModalWindow()` or modeless hosting with theme synchronization.
4. Clean Code structure: Partition commands across modular controller classes so no class exceeds 300 lines (Rule C2) and no method exceeds 50 lines (Rule M3).
5. Deliver structured analysis in `analysis.md` and complete handoff report in `handoff.md`.

## Mandatory Constraints
- `C:\kata_pro` is strictly read-only.
- Never inspect, decompile, or touch `Security.dll` or licensing code.
- Report all file paths and recommended class structures.
