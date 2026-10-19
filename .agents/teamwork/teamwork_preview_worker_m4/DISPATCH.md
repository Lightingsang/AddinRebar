# Task Assignment: Worker M4 (MainControlCenter GETFORM Hub & AutoCAD Bundle Manifest)

- Subagent Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_m4\`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (UTC header `## 2026-10-18T00:11:55Z`)
- Project Specification: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_22\PROJECT.md`
- Survey Blueprint: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_survey_2\handoff.md`
- Clean Code Standards: `AGENTS.md`, `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/clean-code/CODE_REVIEW_CHECKLIST.md`

## Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Write Ownership Boundaries
You have exclusive write ownership of:
- `HPDcePro/src/HPDcePro.UI/ViewModels/MainControlCenterViewModel.cs`
- `HPDcePro/src/HPDcePro.UI/Views/MainControlCenterWindow.xaml`
- `HPDcePro/src/HPDcePro.UI/Views/MainControlCenterWindow.xaml.cs`
- `HPDcePro/src/HPDcePro.AutoCad/Commands/AutoStrCommands.cs` (redirecting `GETFORM`)
- `HPDcePro/deploy/HPDcePro.bundle/PackageContents.xml`

## Detailed Implementation Tasks
1. Implement `MainControlCenterViewModel.cs` in `HPDcePro.UI/ViewModels/` (~190 lines, <= 300 lines):
   - Inherits `ObservableObject`.
   - Properties: `ProjectTitle`, `ProjectCode`, `TotalStories`, `TotalColumns`, `TotalBeams`, `TotalFootings`, `TotalSteelFrames`, `RebarEfficiency`, and status messages for each module.
   - Commands:
     `OpenBeamDetailingCommand`, `OpenColumnWallCommand`, `OpenFoundationSlabCommand`, `OpenCuttingOptimizationCommand`, `OpenSteelConnectionCommand`, `OpenAutoStrConfigCommand`, `LoadProjectCommand`, `SaveProjectCommand`, `RefreshCommand`.
   - Constructor allows optional `Action<Type>? openWindowAction = null` for headless testing without opening WPF windows.
2. Implement `MainControlCenterWindow.xaml` and `.xaml.cs` in `HPDcePro.UI/Views/`:
   - `MahApps.Metro.Controls.MetroWindow` with `Dark.Blue` dynamic theming and `Theme.xaml` resources (`Card.Panel`, `PrimaryButton`, `Text.Title`, etc.).
   - Visual dashboard layout:
     - Top Branding Header: "HPDCEPRO - TRUNG TÂM ĐIỀU KHIỂN & BẢN VẼ KẾT CẤU (GETFORM)".
     - KPI Summary Ribbon: 6 metric chips displaying live counts.
     - 6 Interactive Module Cards for the 6 windows (`BeamDetailing`, `ColumnWall`, `FoundationSlab`, `CuttingOptimization`, `SteelConnection`, `AutoStrConfig`).
     - Bottom Action Bar: Open project, save project, reload, close.
   - Code-behind (`MainControlCenterWindow.xaml.cs`): ~75 lines, calls `ThemeManager.EnsureAssembliesLoaded()`, `ThemeManager.Apply(this, dark: true)`.
3. Redirect `GETFORM` in `HPDcePro.AutoCad/Commands/AutoStrCommands.cs`:
   - Line 23 currently opens `AutoStrConfigWindow`. Change to instantiate `MainControlCenterWindow` and show via `AutoCadUiHelper.ShowModal(win)`.
4. Create `HPDcePro/deploy/HPDcePro.bundle/PackageContents.xml`:
   - Valid Autodesk Autoloader schema targeting AutoCAD 2025–2026 (`SeriesMin="R25.0"` and `SeriesMax="R25.1"`).
   - Declaring `HPDcePro.AutoCad.dll` (`AppType=".NET"`) and `HPDcePro.lsp` (`AppType="Lisp"`).
5. Clean Code compliance:
   - All files $\le 300$ lines, methods $\le 50$ lines.
   - 0 commented-out code, 0 unannotated catches.
   - `TreatWarningsAsErrors=true`.
6. Verify build:
   - Build `dotnet build HPDcePro/src/HPDcePro.UI/HPDcePro.UI.csproj -c Debug` -> 0 errors, 0 warnings.
   - Build `dotnet build HPDcePro/src/HPDcePro.AutoCad/HPDcePro.AutoCad.csproj -c Debug` -> 0 errors, 0 warnings.
7. Write your completion report in `handoff.md` in your working directory and notify the orchestrator.


## 2026-10-18T00:27:10Z
You are Worker M4 for HPDcePro Wave 3.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_m4\
Read your task assignment at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_m4\DISPATCH.md
MANDATORY: Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (entry dated 2026-10-18T00:11:55Z).
Read PROJECT.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_22\PROJECT.md
Read the Survey Blueprint at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_survey_2\handoff.md
Read Clean Code rules at: docs/clean-code/HP_CLEAN_CODE_CORE.md and AGENTS.md.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Your write boundaries:
- HPDcePro/src/HPDcePro.UI/ViewModels/MainControlCenterViewModel.cs
- HPDcePro/src/HPDcePro.UI/Views/MainControlCenterWindow.xaml
- HPDcePro/src/HPDcePro.UI/Views/MainControlCenterWindow.xaml.cs
- HPDcePro/src/HPDcePro.AutoCad/Commands/AutoStrCommands.cs (redirecting GETFORM)
- HPDcePro/deploy/HPDcePro.bundle/PackageContents.xml

Implement MainControlCenterViewModel and MainControlCenterWindow in HPDcePro.UI as the MahApps.Metro dashboard hub connecting the 6 existing module windows with live summary statistics.
Redirect GETFORM in AutoStrCommands.cs to show MainControlCenterWindow.
Create HPDcePro/deploy/HPDcePro.bundle/PackageContents.xml targeting AutoCAD 2025–2026.
Ensure every file is <= 300 lines (aim for <= 220) and methods <= 50 lines, 0 commented code, 0 unannotated catch blocks.
Verify compilation with `dotnet build HPDcePro/src/HPDcePro.UI/HPDcePro.UI.csproj -c Debug`.
Write your completion report in `handoff.md` and send a message back to the orchestrator.
