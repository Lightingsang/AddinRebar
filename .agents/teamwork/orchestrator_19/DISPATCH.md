# DISPATCH — 2026-10-17T14:27:21Z

## Task Assignment for orchestrator_19 (Project Orchestrator)

You are **orchestrator_19**, the Project Orchestrator responsible for migrating the entire `HPKata.UI` WPF dialog layer (shared by `HPKata.Revit` for Revit 2026 and `HPKata.AutoCad` for AutoCAD 2026) from `MaterialDesignThemes` to `MahApps.Metro` (`v2.4.11`) using `MetroWindow`, eliminating all `StaticResourceExtension` / `XamlParseException` runtime errors across all 13 dialogs, and upgrading the Revit rebar detailing commands to automatically read actual element dimensions ($b \times h \times L/H$) with safe manual-mode fallback.

Working directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_19\`
Parent Sentinel conversation ID: `7e24d92c-711c-48ce-bbb8-c38e9b46a4f5`
Authoritative user request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (see timestamp `## 2026-10-17T14:27:21Z`)

### Requirements Summary:
1. **R1. Replace MaterialDesignThemes with MahApps.Metro 2.4.11 Across HPKata Projects**:
   - Remove `MaterialDesignThemes` and `MaterialDesignColors` from `HPKata/HPKata.UI/HPKata.UI.csproj`, `HPKata/HPKata.Revit/HPKata.Revit.csproj`, and `HPKata/HPKata.AutoCad/HPKata.AutoCad.csproj`.
   - Add `<PackageReference Include="MahApps.Metro" Version="2.4.11" />` (already cached in `%USERPROFILE%\.nuget\packages\mahapps.metro\2.4.11`).
   - Zero remaining references to `MaterialDesignThemes` or `http://materialdesigninxaml.net/winfx/xaml/themes` in `HPKata/`.
2. **R2. Migrate Theme System & All 13 Windows to MahApps.Metro MetroWindow**:
   - Update `HPKata/HPKata.UI/Themes/MaterialBridge.xaml` (or MahApps theme bootstrap merged by `Theme.xaml`) and `HPKata/HPKata.UI/Themes/Theme.xaml` to merge `MahApps.Metro` standard resource dictionaries (`Controls.xaml`, `Fonts.xaml`, `Dark.Blue.xaml`).
   - Define all required style keys in `Theme.xaml` (`StandardDataGrid`, `StandardTextBox`, `StandardComboBox`, `PrimaryButton`, `Button.Primary`, `SecondaryButton`, `Button.Secondary`, `Card.Panel`, `GroupBox.Header`, `Text.Title`, `Text.Section`, `Text.Muted`, `Text.Bold`, `Text.Label`, implicit styles).
   - Update `HPKata/HPKata.UI/Common/ThemeManager.cs`: `EnsureAssembliesLoaded()` preloads `MetroWindow` & `ControlzEx` assemblies before XAML parsing; `Apply(Window, bool, ...)` and `ApplyTheme(Window, bool)` handle dynamic theme switching without MaterialDesignThemes.
   - Convert all 13 windows in `HPKata/HPKata.UI/Views/` (`BeamDetailingWindow`, `ColumnDetailingWindow`, `WallDetailingWindow`, `FoundationDetailingWindow`, `SlabDetailingWindow`, `StairLintelDetailingWindow`, `EtabsImportWindow`, `QsTakeoffWindow`, `DrawingUtilityWindow`, `CuttingStockView`, `DetailingView`, `ScheduleView`, `SetupView`) to inherit from `mah:MetroWindow`, configure Metro chrome, call `EnsureAssembliesLoaded()` before `InitializeComponent()`, `Apply(this, dark: true)` after, and handle `DialogResult = true` safely in `OnRequestClose()`.
3. **R3. Upgrade Revit Element Geometry Extraction & Safe Picker Fallback in HPKata.Revit**:
   - Implement `HPKata/HPKata.Revit/Services/RevitElementGeometryReader.cs` to read $b \times h \times L/H$ in mm from selected `Element` / `FamilyInstance`.
   - Update `FramingRebarCommands.cs`, `ColumnRebarCommands.cs`, and `WallRebarCommands.cs` with pre-selection reading, interactive picking with safe fallback on `OperationCanceledException` (ESC key opens dialog in manual mode), and executing Revit rebar transaction on modal OK (`ShowDialog() == true`).
4. **R4. Comprehensive STA Unit Tests for All 13 MetroWindows & StaticResource Parity**:
   - In `HPKata/HPKata.UI.Tests`, implement STA test suite (`Views/AllWindowsXamlInstantiationTests.cs`) testing STA instantiation with dark and light themes, ensuring zero XAML exceptions and 100% StaticResource resolution parity.

### Clean Code Governance & Build Directives:
- Always run `dotnet build` and `dotnet test` with `--disable-build-servers -m:1`.
- Every C# file <= 300 lines (Rule C2), methods <= 50 lines (Rule M3).
- Zero compiler warnings (`TreatWarningsAsErrors=true`).
- 0 dead code, 0 unannotated catch blocks.
- Report completion back to Sentinel `7e24d92c-711c-48ce-bbb8-c38e9b46a4f5` when complete.


## 2026-10-17T14:29:57Z

You are orchestrator_19, the Project Orchestrator.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_19\
Your parent Sentinel conversation ID is: 7e24d92c-711c-48ce-bbb8-c38e9b46a4f5

Your assignment is detailed in your dispatch file:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_19\DISPATCH.md
and the authoritative user request at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (entry dated 2026-10-17T14:27:21Z).

Summary of your mission:
1. Replace MaterialDesignThemes with MahApps.Metro 2.4.11 across HPKata projects (HPKata.UI, HPKata.Revit, HPKata.AutoCad) with zero remaining references to MaterialDesign.
2. Migrate theme system and all 13 windows in HPKata/HPKata.UI/Views/ to MahApps.Metro MetroWindow, defining all required style keys in Theme.xaml, adding ThemeManager.EnsureAssembliesLoaded() and dynamic theme switching, and safe DialogResult handling.
3. Upgrade Revit element geometry extraction (RevitElementGeometryReader.cs) and safe picker fallback on ESC in FramingRebarCommands, ColumnRebarCommands, WallRebarCommands.
4. Add comprehensive STA unit tests in HPKata.UI.Tests (Views/AllWindowsXamlInstantiationTests.cs) verifying STA instantiation of all 13 MetroWindows under dark and light themes and 100% StaticResource resolution parity.
5. Ensure 100% test pass rate, <= 300 lines per C# file, 0 warnings, 0 errors, and report completion back to parent Sentinel 7e24d92c-711c-48ce-bbb8-c38e9b46a4f5 when ready for Victory Audit.
