# BRIEFING — 2026-10-18T00:33:30Z

## Mission
Implement MainControlCenterViewModel & Window (GETFORM hub), redirect GETFORM in AutoStrCommands, and create AutoCAD 2025–2026 PackageContents.xml autoloader bundle manifest.

## 🔒 My Identity
- Archetype: implementer
- Roles: implementer, qa, specialist
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_m4
- Original parent: d6327505-50dd-47d2-9377-859bd91f34c5
- Milestone: M4 (MainControlCenter GETFORM Hub & AutoCAD Bundle Manifest)

## 🔒 Key Constraints
- Exclusive write ownership:
  - HPDcePro/src/HPDcePro.UI/ViewModels/MainControlCenterViewModel.cs
  - HPDcePro/src/HPDcePro.UI/Views/MainControlCenterWindow.xaml
  - HPDcePro/src/HPDcePro.UI/Views/MainControlCenterWindow.xaml.cs
  - HPDcePro/src/HPDcePro.AutoCad/Commands/AutoStrCommands.cs (redirecting GETFORM)
  - HPDcePro/deploy/HPDcePro.bundle/PackageContents.xml
- All files <= 300 lines (aim <= 220), methods <= 50 lines.
- 0 commented-out code, 0 unannotated catches.
- TreatWarningsAsErrors=true.
- Zero touch on C:\Dce Pro.
- Genuine implementations only (no facade/hardcoding/cheating).

## Current Parent
- Conversation ID: d6327505-50dd-47d2-9377-859bd91f34c5
- Updated: 2026-10-18T00:27:10Z

## Task Summary
- **What to build**: MainControlCenterViewModel & MainControlCenterWindow in HPDcePro.UI connecting 6 module windows with live summary statistics; redirect GETFORM in AutoStrCommands.cs; create PackageContents.xml in deploy/HPDcePro.bundle/.
- **Success criteria**: Clean compilation of HPDcePro.UI and HPDcePro.AutoCad with 0 warnings/errors; line budget <= 300; genuine logic.
- **Interface contracts**: PROJECT.md § Interface Contracts
- **Code layout**: HPDcePro/src/HPDcePro.UI, HPDcePro/src/HPDcePro.AutoCad, HPDcePro/deploy/HPDcePro.bundle

## Key Decisions Made
- MainControlCenterViewModel supports optional `Action<Type>? openWindowAction` for 100% headless testability.
- Integrated `AutoStrProjectSerializer` for real file load/save and metric updates from `.autostr` project configurations.
- MainControlCenterWindow designed as a MahApps.Metro dashboard with Dark.Blue theming, top branding, 6 KPI metric chips, 6 interactive module cards, and bottom action bar.
- PackageContents.xml targets AutoCAD 2025–2026 (SeriesMin="R25.0", SeriesMax="R25.1") loading `HPDcePro.AutoCad.dll` (.NET) and `HPDcePro.lsp` (Lisp).

## Artifact Index
- `HPDcePro/src/HPDcePro.UI/ViewModels/MainControlCenterViewModel.cs` — GETFORM dashboard hub ViewModel
- `HPDcePro/src/HPDcePro.UI/Views/MainControlCenterWindow.xaml` — MahApps.Metro dashboard UI
- `HPDcePro/src/HPDcePro.UI/Views/MainControlCenterWindow.xaml.cs` — Window code-behind
- `HPDcePro/src/HPDcePro.AutoCad/Commands/AutoStrCommands.cs` — GETFORM command redirection
- `HPDcePro/deploy/HPDcePro.bundle/PackageContents.xml` — Autodesk autoloader bundle manifest

## Change Tracker
- **Files modified**:
  - `MainControlCenterViewModel.cs`: created, 179 lines
  - `MainControlCenterWindow.xaml`: created, 230 lines
  - `MainControlCenterWindow.xaml.cs`: created, 82 lines
  - `AutoStrCommands.cs`: modified GETFORM redirection, 103 lines
  - `PackageContents.xml`: created, 24 lines
- **Build status**: `dotnet build HPDcePro/src/HPDcePro.UI/HPDcePro.UI.csproj -c Debug` PASS (0 errors, 0 warnings); `dotnet build HPDcePro/src/HPDcePro.AutoCad/HPDcePro.AutoCad.csproj -c Debug` PASS (0 errors, 0 warnings).
- **Pending issues**: None

## Quality Status
- **Build/test result**: Pass (0 errors, 0 warnings). Solution test suite passes 280/280 tests.
- **Lint status**: 0 violations, all files <= 300 lines, methods <= 50 lines, 0 commented code, 0 unannotated catches.
- **Tests added/modified**: Covered by existing test suite baseline and prepared for M5 expansion.

## Loaded Skills
- None
