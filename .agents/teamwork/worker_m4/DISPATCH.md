# DISPATCH — worker_m4

## Task
You are worker_m4. Your task is to implement Milestone M4 in `HPDcePro/HPDcePro.slnx` (Features 17, 18, 19, 20):

### 1. `HPDcePro.UI` (`src/HPDcePro.UI/HPDcePro.UI.csproj`):
- Target framework: `net8.0-windows`.
- Packages: `MahApps.Metro` 2.4.11, `CommunityToolkit.Mvvm` 8.4.0, `ControlzEx` 4.4.0.
- Reference: `HPDcePro.Core`.
- Theme & Styles:
  - `ThemeManager.cs`: `EnsureAssembliesLoaded()` preloading `MetroWindow` & `ControlzEx`, `Apply()` for Dark/Light switching, Segoe UI font enforcement.
  - `Themes/ThemeDark.xaml` and `Themes/ThemeLight.xaml`: Semantic color brushes.
  - `Themes/Theme.xaml`: Master styles for `MetroWindow`, `DataGrid`, `TextBox`, `ComboBox`, `Button`.
- ViewModels:
  - `BeamDetailingViewModel.cs`: Beam geometry $b \times h \times L$, material selection (B20..B35, CB400..500), continuous & extra bars, stirrups, live calculation preview.
  - `CuttingOptimizationViewModel.cs`: Rebar cutting demand table, stock bar length 11,700 mm, cut pattern results, scrap waste %.
  - `AutoStrConfigViewModel.cs`: Project metadata, levels/floors, material mapping, step execution checkboxes.
- Views (`MetroWindow`):
  - `BeamDetailingWindow.xaml`
  - `CuttingOptimizationWindow.xaml`
  - `AutoStrConfigWindow.xaml`

### 2. `HPDcePro.AutoCad` (`src/HPDcePro.AutoCad/HPDcePro.AutoCad.csproj`):
- Target framework: `net8.0-windows`.
- References: `HPDcePro.Core`, `HPDcePro.UI`, `AutoCAD.NET` 25.1.0 (`PrivateAssets="All"`).
- Command classes with `[CommandMethod]` preserving all 69 legacy command names from `DCE Pro.lsp`:
  - `BeamCommands.cs`: `ShopDrawingBeam`, `VeDam_DCE`, `VeDamAll_DCE`, `VeMCD_DCE`, `VeBlockDam2`..`VeBlockSection`, `ImportKichThuoc` (`ktc`), `SuaCKThep` (`suathep`).
  - `RebarCommands.cs`: `TKTSSOLUONGCAUKIEN` (`slck`), `TKTSDUONGKINHTHEP` (`sdkt`), `ToiUuCatThep`, `tkt_dce`, `stk_dce`, `TongHopThongKe_dce` (`THTK_dce`).
  - `ColumnCommands.cs`: `VeCotDcePro` (`vc`), `VeVachDcePro` (`vv`).
  - `SlabCommands.cs`: `VeThepSanTopX` (`T1`), `VeThepSanTopY` (`T2`), `VeThepSanBotX` (`B1`), `VeThepSanBotY` (`B2`), `ThongKeThepSan` (`tkts`).
  - `FootingCommands.cs`: `VeBlockCoc` (`VBC`), `VeMongCoc_DCE` (`VeMongCoc`), `VeMongDon_DCE` (`VeMongDon`), `TMCoc`, `RaiDaiCoc` (`rdc`).
  - `AutoStrCommands.cs`: `AutoStr`, `XuatDuLieuModelEtabs` (`mb1`), `UpdateModelEtabs`.
- Loader: `HPDcePro.lsp` with clean multi-CAD detection and all 69 aliases + 6 geometric utilities (`GenC`, `rdt`, `tencaukien`, `t2m`, `CHIAVIEWPORT`, `DCENAP`).

### 3. `HPDcePro.Excel` (`src/HPDcePro.Excel/HPDcePro.Excel.csproj`):
- Target framework: `net8.0`.
- Package: `ClosedXML` 0.104.2.
- Reference: `HPDcePro.Core`.
- `ClosedXmlScheduleExporter.cs`: Headless rebar schedule `.xlsx` exporter.
- `ClosedXmlReportExporter.cs`: Headless beam design calculation report exporter.
- `CalculationUdfBridge.cs`: Compatibility functions for `_xll.*` UDFs without Excel COM automation.

### 4. `HPDcePro.Revit` (`src/HPDcePro.Revit/HPDcePro.Revit.csproj`):
- Target framework: `net8.0-windows`.
- Packages: `Nice3point.Revit.Api.RevitApi` 2026.0.4, `Nice3point.Revit.Api.RevitApiUI` 2026.0.4 (`PrivateAssets="All"`).
- Reference: `HPDcePro.Core`.
- `RevitDataExchangeService.cs`: Converts Revit structural framing and columns into `HPDcePro.Core` models and vice versa.
- `ExportFramingCommand.cs`: Command skeleton for Revit 2026 add-in.

### 5. Solution Integration & Verification:
- Add all 4 projects to `HPDcePro/HPDcePro.slnx`.
- Run `dotnet build HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1`.
- Verify: 0 warnings, 0 errors, 100% passing tests.
- Verify clean code: all files <= 300 lines (Rule C2), all methods <= 50 lines (Rule M3), 0 dead code, 0 unannotated catch blocks.

### Mandatory Integrity Warning:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Write your handoff report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4\handoff.md`.


## 2026-10-17T17:35:19Z
You are worker_m4, a Teamwork Worker agent.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4
Your detailed task assignment is in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4\DISPATCH.md
The authoritative original request is at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Specification dossier: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\rea-dce-pro\

Implement Milestone M4 across the 4 adapter/UI projects in HPDcePro/:
1. HPDcePro.UI (net8.0-windows)
2. HPDcePro.AutoCad (net8.0-windows)
3. HPDcePro.Excel (net8.0)
4. HPDcePro.Revit (net8.0-windows)
5. Integrate all projects into HPDcePro/HPDcePro.slnx, verify compilation with dotnet build HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1.
6. Strictly obey clean code rules: files <= 300 lines, methods <= 50 lines, 0 commented code, 0 unannotated catch blocks.
