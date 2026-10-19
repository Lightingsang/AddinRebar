# DISPATCH: explorer_m2_2 (M2 ViewModel & UI Explorer)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_2\`
- Milestone: M2 — Bi-Directional Excel Integration & Detailing Parity (Feature 6)
- Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md`
- Test Ready Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\TEST_READY.md`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (lines 1450-1512)
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx`
- Parent Conversation ID: `3c323530-716e-4b3b-a14c-2a7fcee0c68a`

## Mission
Design the implementation specification for Feature 6 (ViewModel Excel Integration in `HPKata.UI`):
1. Extension of ViewModels:
   - `BeamDetailingViewModel`: add `OpenKataWorkbookCommand`, `ReadFromExcelCommand`, `WriteToExcelCommand`, and auto-sync on `ApplyCommand`.
   - `ColumnDetailingViewModel`: add `OpenKataWorkbookCommand`, `ReadFromExcelCommand`, `WriteToExcelCommand`, and auto-sync on `ApplyCommand`.
   - `WallDetailingViewModel`: add `OpenKataWorkbookCommand`, `ReadFromExcelCommand`, `WriteToExcelCommand`, and auto-sync on `ApplyCommand`.
2. Extension of Views (XAML):
   - `BeamDetailingWindow.xaml`: add toolbar buttons for Open, Read, Write (using existing `Button.Primary` / `Button.Secondary` / MaterialDesign icons).
   - `ColumnDetailingWindow.xaml`: add toolbar buttons.
   - `WallDetailingWindow.xaml`: add toolbar buttons.
3. Strict constraints:
   - Retain `System.Windows.Window` + `MaterialDesignThemes` (v5.2.1). Do NOT switch to `MahApps.Metro`.
   - Keep all `Theme.xaml` StaticResource keys intact so all 13 windows instantiate without `StaticResourceExtension` errors.
   - All files <= 300 lines (Rule C2), methods <= 50 lines (Rule M3).

Write your report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_2\handoff.md`.
