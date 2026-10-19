# DISPATCH — worker_m4_1

## Task Assignment
Implement Milestone M4: Comprehensive STA Unit Tests & StaticResource Parity in `HPKata.UI.Tests`.

## Paths to Context Files
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (entry dated 2026-10-17T14:27:21Z)
- Project Plan: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`
- Survey 2 Report: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_survey_2\handoff.md`

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Exclusive Write Ownership
You own and may create/edit exclusively:
- `HPKata/HPKata.UI.Tests/Views/AllWindowsXamlInstantiationTests.cs`
- `HPKata/HPKata.UI.Tests/Views/StaticResourceParityScannerTests.cs` (optional if needed to keep file <= 300 lines)

## Detailed Requirements

### 1. Implement STA Instantiation Unit Tests (`Views/AllWindowsXamlInstantiationTests.cs`)
- Target: Test instantiation of all 13 `MetroWindow` views under both Dark and Light themes in `HPKata.UI.Tests`:
  1. `BeamDetailingWindow`
  2. `ColumnDetailingWindow`
  3. `WallDetailingWindow`
  4. `FoundationDetailingWindow`
  5. `SlabDetailingWindow`
  6. `StairLintelDetailingWindow`
  7. `EtabsImportWindow`
  8. `QsTakeoffWindow`
  9. `DrawingUtilityWindow`
  10. `CuttingStockView`
  11. `DetailingView`
  12. `ScheduleView`
  13. `SetupView`
- Helper `RunOnSta(Action action)`:
  - Must run on STA thread (`thread.SetApartmentState(ApartmentState.STA)`).
  - If `Application.Current == null`, initialize `new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown }`.
  - Call `ThemeManager.EnsureAssembliesLoaded()` before instantiating views.
  - Instantiate window via parameterless constructor.
  - Test Dark theme: call `ThemeManager.Apply(window, dark: true)` or `ThemeManager.ApplyTheme(window, dark: true)`.
  - Test Light theme: call `ThemeManager.Apply(window, dark: false)` or `ThemeManager.ApplyTheme(window, dark: false)`.
  - Assert `window is MahApps.Metro.Controls.MetroWindow`.
  - Safely close the window.

### 2. Implement Automated StaticResource Parity Scanner
- Automated scanner test:
  - Scan all 13 `.xaml` files in `HPKata/HPKata.UI/Views/`.
  - Extract all `{StaticResource <Key>}` references using regex (`\{StaticResource\s+([^\},]+)\}`).
  - Parse `HPKata/HPKata.UI/Themes/Theme.xaml` and collect all defined `x:Key`s and `<Style TargetType="...">` types.
  - Also include standard MahApps styles (`MahApps.Styles.*`, `MahApps.Brushes.*`, `MetroWindow.*`), local view resources, and standard WPF system keys.
  - Assert that 100% of `{StaticResource}` keys referenced across all 13 views resolve cleanly against available keys.
  - Assert 0 missing/unresolved keys.

### 3. Clean Code Rules
- File size <= 300 lines (Rule C2). If needed, separate into `AllWindowsXamlInstantiationTests.cs` and `StaticResourceParityScannerTests.cs`.
- Method size <= 50 lines (Rule M3).
- 0 warnings (`TreatWarningsAsErrors=true`).
- 0 dead code (Rule Q-B1), 0 unannotated catches (Rule Q-B2).

### 4. Build and Test Verification
Run:
`dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1`
`dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1`
Ensure 0 warnings and 100% of tests pass across all test projects.

### 5. Handoff
Write `handoff.md` in `.agents/teamwork/worker_m4_1/handoff.md` and notify `orchestrator_19`.

## 2026-10-17T15:59:25Z
From: 6ca2dcc3-e478-4dfe-bcd3-44e77f9ffa52 (orchestrator_19)
Task: worker_m4_1, implementation worker for Milestone M4 (Comprehensive STA Unit Tests & StaticResource Parity).
Exclusive write ownership:
- HPKata/HPKata.UI.Tests/Views/AllWindowsXamlInstantiationTests.cs
- HPKata/HPKata.UI.Tests/Views/StaticResourceParityScannerTests.cs (optional if needed for <= 300 line limit)
