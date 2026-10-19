# Dispatch Specification — M2 UI ViewModels & Views Integration Explorer (explorer_hpkata_m2_2)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_2\`
- Parent Conversation ID: `3c323530-716e-4b3b-a14c-2a7fcee0c68a`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (UTC header `## 2026-10-18T02:28:56Z`)
- Project Scope: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md` (Milestone M2, Feature 6)
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx`

## Mission: Feature 6 — ViewModel & View Excel Integration in HPKata.UI
Investigate `HPKata.UI` ViewModels and Views, designing the UI integration for bi-directional Excel interaction with `Kata.xlsm`.

## Detailed Requirements:
1. **Target ViewModels & Views**:
   - `BeamDetailingViewModel.cs` & `BeamDetailingWindow.xaml`
   - `ColumnDetailingViewModel.cs` & `ColumnDetailingWindow.xaml`
   - `WallDetailingViewModel.cs` & `WallDetailingWindow.xaml`
2. **Commands & Interactions**:
   - `OpenExcelCommand`: opens `KataInstallEnvironment.Current.KataWorkbookPath` in desktop Excel.
   - `ReadExcelCommand`: populates ViewModel properties from `Kata.xlsm` (via `IKataExcelAdapter`).
   - `WriteExcelCommand`: saves current ViewModel properties to `Kata.xlsm`.
   - Auto-sync on `ApplyCommand` (optional toggle or default behavior).
3. **UI / Styling Constraints**:
   - STRICT CONSTRAINT: Must retain `System.Windows.Window` + `MaterialDesignThemes` (v5.2.1).
   - DO NOT switch to `MahApps.Metro`.
   - Preserve all existing `Theme.xaml` `StaticResource` keys (`StandardDataGrid`, `Button.Primary`, `Button.Secondary`, `GroupBox.Header`, `Text.Label`, etc.).
   - All windows must instantiate without XAML or resource exceptions on STA thread under both Dark and Light themes.
4. **Code Quality**:
   - Keep each `.cs` file `<= 300` lines (Rule C2), methods `<= 50` lines (Rule M3), 0 warnings.
5. **Testing**:
   - Design unit tests in `HPKata.UI.Tests` and E2E STA thread window tests in `HPKata.E2E.Tests`.

Write your findings, exact XAML snippets, ViewModel command signatures, and class designs to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_2\handoff.md`
Send a message back to parent when done.


## 2026-10-18T03:24:39Z
You are explorer_hpkata_m2_2 (M2 UI Integration Explorer).
Your Working Directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_2\
Your Dispatch Specification is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_2\DISPATCH.md
The Authoritative User Request is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (lines 1450-1512).
Target Scope Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md (Milestone M2, Feature 6).
Target Solution: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx
Your Parent Conversation ID is: 3c323530-716e-4b3b-a14c-2a7fcee0c68a

Read ORIGINAL_REQUEST.md and DISPATCH.md first.
Your mission:
1. Investigate HPKata.UI ViewModels (BeamDetailingViewModel.cs, ColumnDetailingViewModel.cs, WallDetailingViewModel.cs) and Views (BeamDetailingWindow.xaml, ColumnDetailingWindow.xaml, WallDetailingWindow.xaml).
2. Design the RelayCommands: OpenExcelCommand, ReadExcelCommand, WriteExcelCommand, and AutoSyncOnApply.
3. Design toolbar/menu controls and visual indicators in XAML.
4. STRICT UI CONSTRAINT: Retain System.Windows.Window + MaterialDesignThemes v5.2.1. DO NOT switch to MahApps.Metro. Retain all Theme.xaml StaticResource keys. Verify that windows can instantiate without errors on STA thread under Dark and Light themes.
5. Check file length limits (<= 300 lines, methods <= 50 lines).
6. Design unit tests for HPKata.UI.Tests and STA E2E tests for HPKata.E2E.Tests.
7. Write your comprehensive report to:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_2\handoff.md
8. Send a message to parent (3c323530-716e-4b3b-a14c-2a7fcee0c68a) when done.
