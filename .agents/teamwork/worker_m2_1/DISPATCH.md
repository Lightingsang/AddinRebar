# DISPATCH — Worker M2 (13 MetroWindow Views Migration & DialogResult Safety)

## Mission
Execute Milestone M2: Convert all 13 dialogs in `HPKata/HPKata.UI/Views/` to inherit from `MahApps.Metro.Controls.MetroWindow` (`mah:MetroWindow`), configure Metro chrome, remove all `xmlns:md` attributes, standardize constructor lifecycle (`ThemeManager.EnsureAssembliesLoaded()` before `InitializeComponent()`, `ThemeManager.Apply(this, dark: true)` after), provide default parameterless constructors for all 13 views, and implement safe `DialogResult = true` handling on modal close.

## Key Inputs
- Path to authoritative user request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (read entry dated `2026-10-17T14:27:21Z`).
- Path to project blueprint: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`.
- Explorer 2 survey report (detailed view catalog): `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_survey_2\handoff.md`.
- Working directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_1\`

## Exclusive Write Ownership
You own and may modify ONLY these 26 files in `HPKata/HPKata.UI/Views/`:
1. `BeamDetailingWindow.xaml` & `BeamDetailingWindow.xaml.cs`
2. `ColumnDetailingWindow.xaml` & `ColumnDetailingWindow.xaml.cs`
3. `WallDetailingWindow.xaml` & `WallDetailingWindow.xaml.cs`
4. `FoundationDetailingWindow.xaml` & `FoundationDetailingWindow.xaml.cs`
5. `SlabDetailingWindow.xaml` & `SlabDetailingWindow.xaml.cs`
6. `StairLintelDetailingWindow.xaml` & `StairLintelDetailingWindow.xaml.cs`
7. `EtabsImportWindow.xaml` & `EtabsImportWindow.xaml.cs`
8. `QsTakeoffWindow.xaml` & `QsTakeoffWindow.xaml.cs`
9. `DrawingUtilityWindow.xaml` & `DrawingUtilityWindow.xaml.cs`
10. `CuttingStockView.xaml` & `CuttingStockView.xaml.cs`
11. `DetailingView.xaml` & `DetailingView.xaml.cs`
12. `ScheduleView.xaml` & `ScheduleView.xaml.cs`
13. `SetupView.xaml` & `SetupView.xaml.cs`

## Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Detailed Requirements for All 13 Views
1. **XAML Migration**:
   - Change root element from `<Window ...>` to `<mah:MetroWindow ...>` and closing tag `</mah:MetroWindow>`.
   - Add namespace: `xmlns:mah="http://metro.mahapps.com/winfx/xaml/controls"`.
   - Remove any `xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes"`.
   - Replace `<Window.Resources>` with `<mah:MetroWindow.Resources>`.
   - Configure Metro chrome attributes on the root element:
     `GlowBrush="{DynamicResource Brush.Accent}" BorderThickness="1" TitleCharacterCasing="Normal" Background="{DynamicResource Brush.Background}" Foreground="{DynamicResource Brush.Text}" FontFamily="Segoe UI"`
2. **C# Code-Behind Migration**:
   - Change base class from `: Window` to `: MetroWindow` (using `MahApps.Metro.Controls`).
   - Standardize constructor sequence:
     ```csharp
     ThemeManager.EnsureAssembliesLoaded();
     InitializeComponent();
     ThemeManager.Apply(this, dark: true);
     ```
   - Parameterless constructor guarantee:
     Ensure all 13 views can be instantiated via a parameterless constructor (chaining to their default ViewModel). Specifically, add parameterless constructors to `FoundationDetailingWindow`, `SlabDetailingWindow`, and `StairLintelDetailingWindow`.
   - Safe modal `DialogResult` handling:
     In `OnRequestClose()`, safely set `DialogResult = true` when shown modally before calling `Close()`:
     ```csharp
     private void OnRequestClose()
     {
         try
         {
             if (System.Windows.Interop.ComponentDispatcher.IsThreadModal)
             {
                 DialogResult = true;
             }
         }
         catch (InvalidOperationException)
         {
             // Fallback for modeless display
         }
         Close();
     }
     ```
     Ensure all detailing windows subscribe `RequestClose` to this safe handler.
     For `DetailingView` and `SetupView`, wrap their `DialogResult` assignment in the same check.
3. **Clean Code Governance**:
   - Every `.cs` file must remain $\le 300$ lines (Rule C2).
   - Every method $\le 50$ lines (Rule M3).
   - 0 dead or commented-out code (Rule Q-B1).
   - 0 unannotated catch blocks (Rule Q-B2).
4. **Build and Test Verification**:
   - Run: `dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1`
   - Run: `dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1`
   - Confirm: 0 warnings, 0 errors, 100% tests pass.
5. **Handoff Report**:
   - Write comprehensive report to `handoff.md` and send message to `orchestrator_19`.

## 2026-10-17T15:03:26Z
[Message] timestamp=2026-10-17T15:03:26Z sender=6ca2dcc3-e478-4dfe-bcd3-44e77f9ffa52 priority=MESSAGE_PRIORITY_HIGH
Task: You are worker_m2_1, the implementation worker for Milestone M2 (13 MetroWindow Views Migration & DialogResult Safety).
All 13 view pairs (26 files) in HPKata/HPKata.UI/Views/.
