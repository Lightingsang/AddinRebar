# Original User Request

## Initial Request — 2026-09-07T07:23:58Z

Refactor and migrate the complete continuous beam rebar generation module R02_BeamsRebar into the production architecture of HPRebar (AddinRebar), ensuring pure domain logic testability in HPRebar.Core, strict feature-folder conventions, atomic transaction safety, and clean WPF MVVM theming on Revit 2025/2026 (.NET 8).

Working directory: F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar
Source directory (read-only): F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\RebarAddin-master\RebarAddin-master\R02_BeamsRebar
Integrity mode: development

## Requirements

### R1. Pure Domain Logic & Geometry Engine (HPRebar.Core/BeamRebar/)
Implement all beam reinforcement calculation algorithms as pure C# records and stateless/pure calculators in HPRebar.Core (netstandard2.0), with zero dependencies on Autodesk.Revit.*:
- Continuous beam span and support node representations in millimetres (double).
- Stirrup distribution calculator supporting uniform spacing and 3-zone (gối - nhịp - gối) layouts.
- Main longitudinal bars (top & bottom) polyline calculations, anchorage hooks (90° bends), and staggered lap splices.
- Additional reinforcement bars (top bars over supports with L/3, L/4 rules; bottom bars in midspan) with multi-layer vertical offsets.
- Side/web reinforcement bars for deep beams (h > 700 mm).
- Special hanging stirrups / diagonal ties at intersections with secondary beams.
- Preview canvas scaling calculator.

### R2. Pure Domain Unit Test Suite (HPRebar.Core.Tests/BeamRebar/)
Create comprehensive xUnit v3 tests verifying all core calculators:
- Single-span, multi-span, and cantilever beams (left, right, both).
- Beams with varying cross-sections (b, h) and z-offsets across spans.
- Stirrup count and spacing boundary rounding.
- Bar lengths, lap splices, and anchorage geometry.
- 100% pass rate under dotnet test HPRebar.Core.Tests.

### R3. Revit Add-In Feature Implementation (HPRebar/Beam Rebar/)
Implement the feature according to repository conventions:
- Root files in HPRebar/HPRebar/Beam Rebar/:
  - BeamRebarCommand.cs: External command entry point ([Transaction(TransactionMode.Manual)]).
  - BeamRebarOrchestrator.cs: Sole owner of the atomic TransactionGroup("Beam Rebar").
  - BeamStackReader.cs, BeamSolidFaceReader.cs, BeamSupportFinder.cs: Revit geometry extraction and support identification.
  - BeamStackValidator.cs: Geometric validation (collinear axis, rectangular cross-sections, same level, valid solids).
  - BeamRebarCreationService.cs & creators: BeamStirrupCreator.cs (CreateFromRebarShape), BeamMainBarCreator.cs (CreateFromCurves), BeamAdditionalBarCreator.cs, BeamSideBarCreator.cs, BeamSpecialBarCreator.cs.
  - BeamDetailViewCreator.cs, BeamSectionViewCreator.cs, BeamDimensionCreator.cs, BeamTagCreator.cs: Drawing and annotation generation.
  - StructuralFramingSelectionFilter.cs: Category-safe selection filter (BuiltInCategory.OST_StructuralFraming).
  - RevitUnits.cs: Millimetres <-> internal feet boundary.
  - LocalizationService.cs: English and Vietnamese string resources.

### R4. WPF MVVM User Interface (HPRebar/Beam Rebar/View/ & View Models/)
- Modal window (ShowDialog()) executing on Revit main thread.
- sealed partial class BeamRebarViewModel : ObservableObject with CommunityToolkit.Mvvm.
- Tabbed interface (Geometry, Stirrups, MainBars, AddTopBars, AddBottomBars, SideBars, SpecialBars, Settings).
- Dynamic styling using {DynamicResource Brush.X} and {DynamicResource Spacing.X} matching Revit light/dark themes.
- Interactive WPF preview canvas rendering the beam elevation and cross-sections directly from domain calculation results.

### R5. Ribbon Integration
Register the "Beam Rebar" push button on the "Rebar" ribbon panel in HPRebar/HPRebar/Application.cs.

## Acceptance Criteria

### Automated Compilation & Build
- [ ] dotnet build HPRebar/HPRebar.slnx -c Debug.R25 -p:DeployAddin=false succeeds with 0 errors.
- [ ] dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false succeeds with 0 errors.

### Automated Unit Testing
- [ ] dotnet test HPRebar/HPRebar.Core.Tests runs all tests and passes with 0 failures and 0 skipped.

### Architectural & Code Quality Guardrails
- [ ] HPRebar.Core contains zero references to Autodesk.Revit.*.
- [ ] Zero deprecated Revit APIs (no DisplayUnitType, no deprecated CreateFromCurves signatures).
- [ ] TransactionGroup in BeamRebarOrchestrator cleanly rolls back on cancel or error and assimilates on success into a single undo operation.
- [ ] All new files reside strictly in HPRebar/HPRebar/Beam Rebar/ and HPRebar.Core/BeamRebar/ with PascalCase namespaces.
- [ ] No modifications to other deliverables (revit-market-research, course-website, scripts/skill_sync).

## Follow-up — 2026-09-07T15:37:30Z

Chuyển đổi và refactor toàn bộ tool `R03_FoundationRebar` từ source cũ sang project `AddinRebar` thành feature mới `Foundation Rebar` theo Phương án A (bản móng/móng bè `Floor` với lưới thép 2 lớp 2 phương, tách lớp pure logic và kiểm thử xUnit).

Working directory: F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar
Integrity mode: development

## Requirements

### R1. Triển khai Pure Logic Layer trong `HPRebar.Core`
- Tạo các model và calculator tính toán thuần túy:
  - `FoundationGeometrySnapshot`: Chiều dài, chiều rộng, chiều dày, tọa độ mặt trên/mặt dưới, hướng trục local X/Y.
  - `FoundationRebarSpec`: Cấu hình đường kính thép, bước thép ($s$), lớp bê tông bảo vệ ($c$), bật/tắt lớp trên (Top Mat), loại móc uốn (Hook/Straight).
  - `FoundationMeshCalculator`: Thuật toán tính toán số lượng thanh thép, tọa độ đường tim thép (curve coordinates) cho 2 phương (Phương chính X và Phương phụ Y), lớp dưới (Bottom Mat) và lớp trên (Top Mat).
  - `FoundationBoundaryCalculator`: Tính toán phạm vi rải thép hữu dụng sau khi trừ lớp bảo vệ các cạnh.
- **Ràng buộc**: Tuyệt đối không tham chiếu bất kỳ namespace `Autodesk.Revit.*` nào trong `HPRebar.Core`.

### R2. Bổ sung Bộ Unit Tests trong `HPRebar.Core.Tests`
- Viết các test suite xUnit bao phủ các kịch bản edge cases:
  - Tính đúng số thanh và khoảng rải đều khi chiều dài chia hết hoặc không chia hết cho spacing.
  - Hai phương phân bố vuông góc chính xác, tọa độ z của lớp 1 và lớp 2 không bị trùng hoặc đảo lộn.
  - Xử lý móng xoay bất kỳ trong mặt bằng.
  - Chặn và phát hiện lỗi khi chiều dày móng nhỏ hơn 2 lần lớp bảo vệ cộng đường kính thép.
  - Chặn khi spacing âm hoặc bằng 0.

### R3. Triển khai Revit Feature Layer trong `HPRebar/Foundation Rebar/`
- Tuân thủ cấu trúc thư mục bắt buộc:
  ```
  HPRebar/HPRebar/Foundation Rebar/
  ├── FoundationRebarCommand.cs
  ├── FoundationSelectionFilter.cs
  ├── FoundationSolidFaceReader.cs
  ├── FoundationRebarValidator.cs
  ├── FoundationRebarCreationService.cs
  ├── FoundationRebarOrchestrator.cs
  ├── Models/
  │   └── FoundationSession.cs
  ├── View/
  │   ├── FoundationRebarView.xaml
  │   ├── FoundationGeometryView.xaml
  │   └── FoundationSettingView.xaml
  └── View Models/
      ├── FoundationRebarViewModel.cs
      ├── FoundationGeometryViewModel.cs
      └── FoundationSettingViewModel.cs
  ```
- `FoundationSelectionFilter`: Cho phép chọn đối tượng sàn móng (`Floor`).
- `FoundationSolidFaceReader`: Trích xuất Solid, mặt phẳng trên (Top PlanarFace có normal $(0,0,1)$), mặt dưới, độ dày và bounding box.
- `FoundationRebarValidator`: Kiểm tra móng hợp lệ (nằm ngang, độ dày hợp lệ).
- `FoundationRebarCreationService`: Sử dụng Revit API hiện hành (`Rebar.CreateFromCurves`) tạo các thanh thép trong transaction.
- `FoundationRebarOrchestrator`: Điều phối snapshot hình học, mở modal dialog, và thực thi commit qua `TransactionGroup`.
- Multi-version: Sử dụng `// Multi-version: ElementId` để tương thích giữa Revit 2024+ (`ElementId.Value`) và Revit cũ (`IntegerValue`).

### R4. Giao diện WPF MVVM và Shared Theme
- View kế thừa `Resources/Themes/Theme.xaml` qua `{DynamicResource ...}`, hỗ trợ runtime dark/light.
- Không hardcode màu sắc, font chữ hay spacing.
- ViewModel sử dụng `CommunityToolkit.Mvvm` (`ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`).
- Code-behind chỉ chứa `InitializeComponent()` và gán `DataContext`.

### R5. Tích hợp Ribbon Panel trong `Application.cs`
- Thêm nút nhấn "Foundation Rebar" vào ribbon panel `Rebar` hiện có trong `Application.cs`, sử dụng icon sẵn có `/HPRebar;component/Resources/Icons/RibbonIcon16.png` và `RibbonIcon32.png`.

## Acceptance Criteria

### Verification & Tests
- [ ] `dotnet test HPRebar/HPRebar.Core.Tests` chạy thành công 100% (241 baseline tests + toàn bộ tests mới đều pass, 0 failed, 0 skipped).
- [ ] `dotnet build HPRebar/HPRebar.slnx -c Debug.R25 -p:DeployAddin=false` biên dịch thành công (0 errors).
- [ ] `dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false` biên dịch thành công (0 errors).
- [ ] Không sử dụng bất kỳ API deprecated nào trong Revit 2025/2026.
- [ ] Không có tham chiếu `Autodesk.Revit.*` trong `HPRebar.Core`.
- [ ] Nút Ribbon "Foundation Rebar" được đăng ký thành công trong `Application.cs`.
- [ ] Toàn bộ mã nguồn cũ và các deliverable khác (`revit-market-research`, `skill_sync`, `course-website`) được giữ nguyên vẹn.

## Follow-up — 2026-09-20T12:39:24Z

Migrate the HPGeo geodetic toolkit into HPAutoCad as a unified feature folder `HPGeoLink` mirroring the HPRebar architecture, and establish a mandatory closed-loop live verification cycle via MCP AutoCAD that every tool implementation and bug fix must satisfy before being declared complete.

Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar
Integrity mode: development
Requested team: Full team

## Requirements

### R1. Restructure HPAutoCad & Migrate HPGeo to HPGeoLink
- Establish the HPRebar architectural pattern in HPAutoCad: create HPAutoCad (Add-In project targeting AutoCAD 2026, .NET 8.0-windows, WPF, MVVM) and HPAutoCad.Core (host-free geodetic/algorithm library, .NET 8.0, zero AutoCAD API dependencies).
- Migrate all geodetic, VN-2000, WGS84, and KMZ features from HPGeo into feature folder HPGeoLink/ inside both HPAutoCad (Commands/, Model/, Service/, View/, ViewModel/) and HPAutoCad.Core (HPGeoLink/).
- Preserve HPAutoCad.TileFetch as a companion console utility project in HPAutoCad.slnx, emitting its executable into the bundle runtime directory for background tile downloading.
- Transfer all existing geodetic unit test suites into HPAutoCad.Tests (net10.0-windows / xUnit v3 / MTP).
- Remove the legacy standalone HPGeo/ folder at the repository root once migration and verification succeed.

### R2. Single Bundle Packaging, ALC Loader & Shared Ribbon Tab
- Package a single bundle HPAutoCad.bundle deployed to %AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\.
- Provide a unified HPAutoCad.Loader executing in an isolated AssemblyLoadContext that constructs a single Ribbon tab HPAutoCad containing both the MCP panel (Bridge AI) and the HPGeoLink panel (UI commands).
- Repack MaterialDesignThemes and ensure WebView2Loader.dll is bundled correctly to prevent runtime assembly collisions with AutoCAD or other add-ins.
- Ensure full backward compatibility with HPAutoCad.McpBridge and mirror test assertions (HPCivil3d.McpBridge.Tests).

### R3. Mandatory Closed-Loop Verification via MCP AutoCAD
- Enforce an autonomous build-and-test loop for all AutoCAD tools: Build project → Deploy / Load into AutoCAD → Execute commands and scenarios via MCP AutoCAD → Inspect logs and error codes → Self-correct code if failing → Re-test until error-free.
- Never mark any tool implementation or bug fix as complete based solely on compilation or local unit tests; completion must be verified live in AutoCAD 2026 via MCP.
- Autonomous troubleshooting: read logs, exceptions, and error codes (-32001, -32002, -32003), diagnose root causes, and continue iteration without interrupting the user unless business requirements or missing credentials dictate it.
- Verification must run unattended via harness scripts (HPAutoCad/tools/harness/run-bridge-unattended.ps1 or equivalent script launching AutoCAD with bridge.scr and handling opt-in).

### R4. Standardized Rules & Repository Documentation
- Document the rule in AGENTS.md and docs/code-standards.md: Every new AutoCAD tool must be created inside HPAutoCad/ as a dedicated feature folder (HPAutoCad/<ToolName>/ for UI/Commands/MVVM and HPAutoCad.Core/<ToolName>/ for host-free logic).
- Update docs/system-architecture.md and docs/codebase-summary.md to reflect the 6-deliverable repository structure, retiring HPGeo as a separate pillar.

## Acceptance Criteria

### Compilation & Static Quality
- [ ] HPAutoCad.slnx builds cleanly in both Debug and Release configurations without warnings treated as errors.
- [ ] All 161 geodetic unit tests in HPAutoCad.Tests pass 100%.
- [ ] All MCP server and bridge tests in HPAutoCad.Mcp.Server.Tests pass 100%.
- [ ] Civil 3D mirror tests (HPCivil3d.McpBridge.Tests) pass 100%.

### Live AutoCAD Verification (Unattended via MCP)
- [ ] AutoCAD 2026 loads HPAutoCad.bundle cleanly; Ribbon tab HPAutoCad appears with both MCP and HPGeoLink panels.
- [ ] HPAutoCad.McpBridge starts named pipe hpautocad-mcp-2026 and accepts AI execution.
- [ ] Commands (HPGEODIALOG, HPGEOKMZ, HPGEOIMPORT, HPGEOINFO) execute successfully without unhandled exceptions.
- [ ] Dialog windows (WPF + WebView2) open, render correctly across dark/light themes, and close cleanly.
- [ ] All existing MCP seed tools and AEC tools remain functional with zero regressions.

### Repository Cleanliness
- [ ] Standalone HPGeo/ folder is deleted; git status shows no leftover unmanaged files.
- [ ] AGENTS.md, docs/code-standards.md, and docs/system-architecture.md reflect the updated architecture.

## 2026-09-20T22:21:59Z

Build Smart Plot Pro, an advanced automated batch plotting and PDF publishing tool integrated into the HPAutoCad ecosystem (AutoCAD 2026, .NET 8), supporting frame extraction by Block/Layer/Layout, automatic paper size/orientation detection, spatial sorting, and single/merged PDF generation via PdfSharp.

Requested team: Full team (chuyên biệt hóa theo từng phân lớp: Core logic, CAD plot engine, WPF MVVM UI, Ribbon/Loader, và Unit tests)
Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad
Integrity mode: demo

## Requirements

### R1. Pure Logic Engine (HPAutoCad.Core/SmartPlot/)
- Host-free logic layer with zero AutoCAD references.
- Models:
  - PlotItem: Bounds (MinX, MinY, MaxX, MaxY), LayoutName, DisplayName, AttributeValue, Order, Rotation.
  - PlotConfiguration, PlotPreset, PlotResult.
  - Enums: FrameSourceType (Block, Layer, Layout), OutputMode (SingleFiles, MergedPdf), OrientationMode (Auto, Portrait, Landscape).
- Services:
  - IPlotOrderService / PlotOrderService: Sắp xếp khung theo thứ tự đọc (Trên → Dưới, Trái → Phải) có nhóm dải cao độ Y (tolerance overlap band) để xử lý các khung vẽ lệch tầng/zic-zac.
  - LayoutRangeParser: Phân tích chuỗi range (All, 1-5, 1,3,5, 1-3,5,8-10) thành danh sách 1-based index an toàn, không ném exception khi nhập sai cú pháp.
  - IFileNameService / FileNameService: Sanitize tên file bằng Path.GetInvalidFileNameChars(), hỗ trợ template tokens ({Prefix}_{Layout}_{Title}_{SheetNo}).
  - IPresetService / PresetService: Đọc/ghi JSON tại %AppData%\HPAutoCad\SmartPlot\presets.json bằng System.Text.Json.

### R2. AutoCAD Plot Engine & Providers (HPAutoCad/SmartPlot/Cad/)
- Frame Providers (Cad/Providers/):
  - IFrameProvider: Interface chung cho các cơ chế quét khung in.
  - BlockFrameProvider: Quét BlockReference theo tên hiệu dụng (EffectiveName), hỗ trợ Dynamic Block và đọc block attribute để lấy số hiệu/tên bản vẽ.
  - LayerFrameProvider: Quét các đường bao Polyline/Polyline2d/Polyline3d khép kín trên layer chỉ định.
  - LayoutFrameProvider: Quét các Layout theo TabOrder và lọc theo range được chỉ định.
- AutoCadPlotEngine (Cad/Plot/):
  - Điều khiển quá trình in theo pipeline chuẩn của AutoCAD:
    PlotSettings → PlotSettingsValidator → PlotInfo → PlotInfoValidator → PlotEngine (PlotFactory.ProcessPlotState.CreatePlotEngine()).
  - Hỗ trợ in Window, tự động nhận diện Portrait/Landscape theo tỷ lệ khung, chọn máy in (PC3/System), khổ giấy (CanonicalMediaName), Plot style (CTB/STB), Fit/Scale, Center.
  - Quản lý an toàn: Sử dụng DocumentLock, tạm thời đặt BACKGROUNDPLOT=0 và CMDECHO=0, luôn khôi phục biến hệ thống ban đầu trong khối finally.
  - Điều khiển tiến trình: Cập nhật PlotProgressDialog và hỗ trợ CancellationToken cho phép hủy tác vụ cooperative.

### R3. In-Process PDF Merging Service (HPAutoCad/SmartPlot/Pdf/)
- Cài đặt package PdfSharp (v6.x cho .NET 8) vào HPAutoCad.csproj.
- IPdfMergeService / PdfMergeService: Nhận danh sách PDF tạm thời, gộp thành một file PDF duy nhất qua PdfSharp thuần túy trong process và tự động dọn dẹp các file PDF tạm. Không phụ thuộc công cụ ngoài (như PDF24).

### R4. WPF MVVM Modeless UI & Theme Synchronization (HPAutoCad/SmartPlot/UI/)
- Sử dụng CommunityToolkit.Mvvm (8.4.0) và MaterialDesignThemes (5.3.2).
- Cửa sổ Modeless (Application.ShowModelessWindow) cho phép người dùng zoom/pan và click "Pick" trên bản vẽ trong khi dialog đang mở.
- Tự động đồng bộ Dark/Light theme theo AutoCAD qua AutocadHostTheme.Instance (lắng nghe biến hệ thống COLORTHEME 55 vs 245) và nạp MaterialBridge.xaml.
- Assembly isolation: Merge MaterialDesignThemes trực tiếp vào HPAutoCad.dll bằng target RepackMaterialDesign (dùng ILRepack), không sinh file loose DLL MaterialDesignThemes.Wpf.dll.
- Giao diện SmartPlotWindow.xaml: Header, ProgressBar (X / Y sheets), Tabs (Plot, Presets, Settings, About) và các Card chức năng (Frame Source, Printer & Paper, Plot Style, Orientation, Output & Naming).
- ViewModel SmartPlotViewModel: Kế thừa ObservableObject, quản lý trạng thái, danh sách PlotItem, tiến trình in, CancellationTokenSource, và các command [RelayCommand].

### R5. Command Registration & Ribbon Integration
- Commands/SmartPlotCommands.cs: Đăng ký lệnh [CommandMethod("HPSMARTPLOT")] và alias [CommandMethod("HPLOT")].
- Đấu nối qua kiến trúc HPAutoCad.Loader (chuyển tiếp qua delegate trong Entry.Start / AppLoadContext).
- Đăng ký nút "Smart Plot Pro" trên panel "Plot" của Ribbon tab HPAutoCad (HPAUTOCAD_MCP_TAB) qua HPAutoCad.Loader với icon vector/theme-aware.

## Verification Plan

### Automated Tests (HPAutoCad.Tests/SmartPlot/)
- Bộ unit test xUnit v3 (chạy độc lập không cần mở AutoCAD):
  - PlotOrderServiceTests: Kiểm tra sắp xếp lưới khung thẳng hàng và zic-zac lệch tầng có tolerance band.
  - LayoutRangeParserTests: Kiểm tra các chuỗi range hợp lệ (All, 1-5, 1,3,5, 1-3,5,8-10) và phòng chống crash khi nhập chuỗi sai cú pháp.
  - FileNameServiceTests: Kiểm tra loại bỏ ký tự cấm và thay thế token ({Prefix}_{Layout}_{Title}_{SheetNo}).
  - PresetServiceTests: Kiểm tra serialize/deserialize JSON cài đặt và tải cấu hình mặc định.
- Lệnh build: dotnet build HPAutoCad/HPAutoCad.slnx -c Debug
- Lệnh test: dotnet test HPAutoCad.Tests

### Build & Packaging Sanity Checks
- Kiểm tra RepackMaterialDesign thực thi thành công, không để lại file MaterialDesignThemes.Wpf.dll độc lập trong thư mục build Contents\App\.
- Đảm bảo HPAutoCad.Core thuần túy .NET 8, không có bất kỳ reference nào đến AutoCAD API.

## Acceptance Criteria

### Core Logic & Services
- [ ] HPAutoCad.Core/SmartPlot biên dịch sạch, không phụ thuộc AutoCAD API.
- [ ] PlotOrderService sắp xếp khung theo thứ tự chuẩn Top-to-Bottom, Left-to-Right với tolerance overlap band.
- [ ] LayoutRangeParser xử lý chính xác tất cả cú pháp range hợp lệ và phòng chống lỗi ngoại lệ trên input rác.
- [ ] FileNameService khử sạch ký tự cấm và thay thế đúng các token mẫu tên file.
- [ ] PresetService đọc/ghi JSON mượt mà tại %AppData%\HPAutoCad\SmartPlot\presets.json.

### AutoCAD Engine & PDF Merging
- [ ] Các FrameProvider nhận diện chính xác khung từ Block (kể cả Dynamic Block & Attributes), Polyline khép kín, và Layout tabs.
- [ ] AutoCadPlotEngine điều khiển pipeline PlotEngine của AutoCAD chuẩn xác.
- [ ] Các biến hệ thống BACKGROUNDPLOT và CMDECHO luôn được khôi phục nguyên vẹn ngay cả khi có lỗi ngoại lệ hoặc người dùng hủy lệnh.
- [ ] Chế độ Single Files xuất file PDF riêng biệt; chế độ Merged gộp thành một file PDF duy nhất qua PdfSharp v6.x và dọn sạch file tạm.

### UI & UX Integration
- [ ] Cửa sổ Modeless hiển thị mượt mà, cho phép tương tác viewport và hỗ trợ Pick frame từ bản vẽ.
- [ ] Đồng bộ giao diện Dark/Light mode theo biến COLORTHEME (55 vs 245) của AutoCAD.
- [ ] Target RepackMaterialDesign chạy chuẩn xác, toolkit MaterialDesign được đóng gói trực tiếp vào HPAutoCad.dll.
- [ ] Lệnh HPSMARTPLOT và HPLOT được đăng ký và hoạt động thông qua loader.
- [ ] Nút Ribbon "Smart Plot Pro" hiển thị trên tab HPAUTOCAD_MCP_TAB thuộc panel "Plot".

### Test Coverage
- [ ] 100% unit tests trong HPAutoCad.Tests/SmartPlot pass khi chạy dotnet test HPAutoCad.Tests.

## 2026-09-21T06:10:48Z

Implement the complete Power BI MCP subsystem (HPPowerBi) in the AddinRebar repository based on implementation_plan.md: a standalone WPF bridge connecting to Power BI Desktop local Analysis Services (AMO-TOM/ADOMD.NET) and Power BI Service Cloud REST API, a .NET 10 stdio MCP server exposing tabular schema, DAX evaluation, measure authoring, and C# Roslyn scripting, accompanied by automated tests and agent documentation.

Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar
Integrity mode: development

## Requirements

### R1. Complete HPPowerBi.McpBridge (.NET 8.0-windows, WPF)
Build the standalone desktop bridge application:
- Detect running PBIDesktop.exe processes and local SSAS TCP port from msmdsrv.port.txt in the AnalysisServicesWorkspaces folder.
- Connect via AMO-TOM (Microsoft.AnalysisServices.NetCore.retail) and ADOMD.NET (Microsoft.AnalysisServices.AdomdClient.NetCore.retail).
- Support auto-registration of .pbitool.json in Windows External Tools directory (%CommonProgramFiles%\Microsoft Shared\Power BI Desktop\External Tools).
- Implement 3-layer safety: UI opt-in checkbox, DAX syntax validation, and metadata/TMDL snapshot backup before write operations.
- Implement Cloud REST API client supporting MSAL OAuth 2.0 and Service Principal.
- Provide MaterialDesign 5.3.2 MVVM UI with instance switcher, connection state, safety checkboxes, and pipe listener on hppowerbi-mcp-2026.

### R2. Complete HPPowerBi.Mcp.Server (.NET 10 Console, Stdio)
Build the stdio MCP server connecting to the bridge over named pipe:
- Implement PowerBiHostProfile for IHostProfile.
- Expose all core tools: get_powerbi_context, execute_powerbi_code, powerbi_get_schema, powerbi_evaluate_dax, powerbi_create_or_update_measure, powerbi_delete_measure, powerbi_manage_relationship, powerbi_format_dax.
- Expose cloud tools: powerbi_cloud_list_workspaces, powerbi_cloud_list_datasets, powerbi_cloud_trigger_refresh, powerbi_cloud_execute_dax.
- Integrate dynamic tool registry and meta tools from McpShared.

### R3. Automated Test Suites
- Create HPPowerBi.McpBridge.Tests verifying port detection, DAX result serialization, and snapshot management.
- Create HPPowerBi.Mcp.Server.Tests verifying profile registration, core tool metadata, and registry lifecycle.

### R4. Skill and Ecosystem Documentation
- Create .agents/skills/hp-mcp-powerbi/SKILL.md documenting Power BI MCP tools, DAX guidelines, and safe usage.
- Update AGENTS.md to register HPPowerBi/ in the repository architecture table.

## Acceptance Criteria

### Build & Test Verification
- [ ] dotnet build HPPowerBi/HPPowerBi.slnx succeeds with 0 errors and 0 warnings.
- [ ] dotnet test HPPowerBi/HPPowerBi.Mcp.Server.Tests passes 100%.
- [ ] dotnet test HPPowerBi/HPPowerBi.McpBridge.Tests passes 100%.
- [ ] dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests continues to pass 100% (no regressions).

### Functional Contracts
- [ ] HPPowerBi.Mcp.Server runs in stdio mode and responds to MCP tools/list with all core and cloud tools.
- [ ] HPPowerBi.McpBridge can detect local Power BI Desktop instances and connect via AMO-TOM.
- [ ] DAX query results are formatted with row count and duration; mutations are gated behind the bridge safety checkbox.



## 2026-09-21T09:44:29Z

Requested team: Full team

Build the HPExcel MCP ecosystem (Standalone WPF Bridge application, Stdio MCP Server, Tool Catalog, Safety & Snapshot Engine, and automated Test Suites) for Microsoft Excel, integrating cleanly with the repository's host-neutral McpShared architecture.

Working directory: g:/09-PROJECT AI/01_Revit/02_CshapRevit/01_AddinRebar/HPExcel
Integrity mode: development

## Requirements

### R1. Standalone WPF Bridge & Stdio Server Architecture
- `HPExcel.McpBridge`: Standalone desktop application (.NET 8 Windows, MaterialDesignThemes 5.3.2) listening on Named Pipe (`hpexcel-mcp-2026`). Connects out-of-process to live running Microsoft Excel instances via COM Interop (`Excel.Application`), with hybrid support for direct headless workbook operations via `ClosedXML` when targeting closed `.xlsx` files.
- `HPExcel.Mcp.Server`: .NET 10 console application speaking standard MCP over stdio, connecting to the bridge via Named Pipe client, powered by `HPRebar.Mcp.Server.Core`.
- Architectural isolation: `HPExcel/` references only `../McpShared/` and never cross-references other host projects (`HPRebar`, `HPAutoCad`, `HPNavis`, `HPEtabs`, `HPCivil3d`, `HPSap2000`, `HPPowerBi`). Register `PipeNaming.ExcelHost` in `McpShared`.

### R2. Comprehensive Excel Toolset (Core + Embedded Seeds)
- Core tools: `get_excel_context` (active workbook, active sheet, selection, open files, Excel version), `execute_excel_code` (Roslyn C# script with `excel` global), and registry meta tools (8 tools: propose, test, publish, search, etc.).
- 12 Embedded seed tools:
  1. `read_range`: read cell values, formulas, and display text from range or named range.
  2. `read_worksheet_info`: enumerate worksheets, used range dimensions, tables, charts, comments.
  3. `find_cells`: search for matching values, text, or formulas across sheet or workbook.
  4. `read_table`: read structured Excel table (`ListObject`) rows into JSON records.
  5. `write_range`: batch write values or formulas into target range with auto-sizing.
  6. `format_range`: apply number formats, font styling, colors, borders, and alignment.
  7. `manage_worksheet`: add, rename, duplicate, delete, hide/unhide worksheets.
  8. `create_table`: convert range to structured table with headers, table style, and totals.
  9. `create_chart`: generate standard charts (Column, Line, Pie, Bar, Area) linked to data range.
  10. `evaluate_formula`: calculate Excel formulas dynamically.
  11. `export_worksheet`: export worksheet or workbook to PDF or CSV.
  12. `run_macro`: invoke existing VBA macros/functions with parameters safely.

### R3. 3-Tier Safety & Automatic Snapshot Engine
- Three Operation Tiers:
  - Tier R (Read): `read_range`, `read_worksheet_info`, `find_cells`, `read_table`, `get_excel_context`. Allowed without snapshot overhead.
  - Tier W (Write): `write_range`, `format_range`, `create_table`, `create_chart`, sheet addition/renaming. Requires Write permission toggle enabled in Bridge UI. Takes an automatic timestamped `.xlsx` backup snapshot before writing.
  - Tier D (Destructive): Worksheet deletion, clear contents of entire sheets, mutating VBA macros. Requires explicit Destructive toggle enabled in Bridge UI. Auto-takes snapshot.
- Snapshot Engine: Saves backup copies into a `.hpexcel_snapshots/` directory beside the workbook (or `%TEMP%` for unsaved workbooks), returning the snapshot path in the response.
- Roslyn Guard Profile: Integrates `McpShared`'s `ScriptGuard` to block forbidden namespaces (`System.Diagnostics.Process`, `#r`/`#load` external binaries, unauthorized file deletions).

### R4. Automated Verification Suite & Test Harness
- `HPExcel.Mcp.Server.Tests` (.NET 10 xUnit): Tool catalog completeness tests, schema validation, fake executor round-trip execution for all 12 seed tools, Roslyn seed script compilation tests.
- `HPExcel.McpBridge.Tests` (.NET 8 Windows xUnit): 3-tier safety classification tests, snapshot engine backup/restore tests, ClosedXML headless reading/writing tests, and Named Pipe request dispatcher tests runnable without Excel installed.
- Smoke harness test verifying stdio JSON-RPC initialization and `tools/list` response.

## Acceptance Criteria

### Solution Build & Isolation
- [ ] `dotnet build HPExcel.slnx` builds successfully with 0 errors.
- [ ] No project in `HPExcel/` references any forbidden assembly or sibling host project outside `McpShared/`.

### Tool Catalog & Execution
- [ ] All 12 embedded seed tools and core tools (`get_excel_context`, `execute_excel_code`) report valid JSON schemas and descriptions.
- [ ] Fake executor round-trip tests pass for every seed tool in `HPExcel.Mcp.Server.Tests`.

### Safety Tiers & Snapshot Recovery
- [ ] Operations classified as Tier W fail with descriptive error when Write toggle is disabled.
- [ ] Operations classified as Tier D fail with descriptive error when Destructive toggle is disabled.
- [ ] When Write or Destructive operation succeeds, an auto-snapshot file is created on disk and its path is returned in the response.

### Headless & COM Integration
- [ ] Headless tests via `ClosedXML` verify range reading, range writing, formula evaluation, and table creation on `.xlsx` files without requiring an active Excel process.

### Unit Test Execution
- [ ] `dotnet test HPExcel.Mcp.Server.Tests` passes 100% of tests.
- [ ] `dotnet test HPExcel.McpBridge.Tests` passes 100% of tests.

## 2026-09-21T13:16:14Z

# Teamwork Project Prompt — HPRobot MCP

Xây dựng hệ thống Robot Structural Analysis Professional 2026 MCP (Model Context Protocol) gồm Standalone WPF Desktop Bridge kết nối COM out-of-process qua `RobotOM.dll` và Server stdio (net10.0) theo chuẩn kiến trúc sinh thái McpShared / HPRebar.

Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot
Integrity mode: development

## Requirements

### R1. McpShared Profile & Host Integration
- Bổ sung cấu hình host Robot vào `McpShared/`:
  - `PipeNaming.RobotHost` (`"hprobot-mcp-2026"`), `JsonRpcMethods.RobotPrefix` (`"robot."`).
  - `GuardProfile.Robot` và `AnalyzerProfile.Robot` (cho phép namespace `RobotOM`, deny-list file I/O & reflection).
  - `HostScriptContracts.RobotImports` (`RobotOM`, `System`, `System.Collections.Generic`, `System.Linq`), `RobotGlobals` (`robot`, `structure`, `args`, `units`), `RobotHeavyMaxTimeoutSeconds` (300s).
  - `ContextResult.Robot` & `RobotInfo` DTOs.
  - Bảo đảm toàn bộ tests hiện có trong `McpShared` (164 tests net10 + 62 tests net48) tiếp tục PASS 100%, không hồi quy bất kỳ host nào khác.

### R2. HPRobot.McpBridge (net8.0-windows)
- Standalone WPF desktop application kết nối out-of-process tới Robot Structural Analysis Professional 2026 qua COM Interop `RobotOM.dll` (`C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll`).
- Lắng nghe Named Pipe `hprobot-mcp-2026`, dispatch các lệnh `robot.execute` và `robot.context`.
- Tích hợp giao diện người dùng MaterialDesignThemes 5.3.2 (Dark/Light mode) hiển thị trạng thái kết nối COM tới Robot, thông tin model RTD đang mở, log thực thi thời gian thực, và 2 checkbox kiểm soát an toàn:
  - `AllowExecution`: Bật/tắt cho phép thực thi script từ MCP client.
  - `AllowHeavyOperations`: Cho phép các tác vụ nặng/xoá dữ liệu (Delete structural elements, run calculations).
- Triển khai `RobotUnitsPolicy`: chuẩn hóa đơn vị làm việc Metric (Meter cho hình học, kN cho lực, kN·m cho mô men, MPa cho ứng suất) trong suốt thời gian script chạy và khôi phục nguyên trạng đơn vị người dùng khi kết thúc.

### R3. 3-Tier Safety System & RTD Snapshot
- Bộ phân tích cú pháp Roslyn AST (`RobotTierAnalyzer`) tự động phân loại script trước khi chạy thành 3 cấp độ an toàn:
  - `Read`: Đọc thông tin mô hình, hình học, kết quả nội lực - không cần snapshot.
  - `Write`: Tạo hoặc sửa đổi Nodes, Bars, Panels, Supports, Loads, Sections - tự động lưu model hiện tại và tạo bản sao lưu snapshot file `.rtd` vào thư mục `.hprobot_snapshots/` hoặc `%TEMP%`.
  - `Delete / Heavy`: Xóa cấu kiện hoặc chạy `Calculate()` phân tích kết cấu - yêu cầu người dùng phải tick bật checkbox `AllowHeavyOperations` trên Bridge UI mới cho phép thực thi.

### R4. HPRobot.Mcp.Server (net10.0)
- MCP Server chạy console stdio tuân thủ đặc tả MCP 2.2.0 (`ModelContextProtocol`), kết nối đến Bridge qua Named Pipe `hprobot-mcp-2026`.
- Cung cấp đủ bộ 24 tools theo tiêu chuẩn của hệ thống:
  - **4 Core Tools**:
    1. `execute_robot_code`: Biên dịch và thực thi script C# RobotOM với cơ chế 3-tier safety.
    2. `get_robot_context`: Trích xuất ngữ cảnh mô hình RTD đang mở (phiên bản, chế độ kết cấu, số lượng nút, thanh, tấm, tải trọng, trạng thái tính toán).
    3. `robot://` resources: Đọc cấu trúc mô hình dạng JSON.
    4. Prompts: Hướng dẫn viết mã Roslyn C# điều khiển RobotOM.
  - **8 Registry Meta Tools**: `search_tools`, `propose_tool`, `test_tool`, `publish_tool`, `disable_tool`, `enable_tool`, `deprecate_tool`, `delete_tool`.
  - **12 Embedded Seed Tools**:
    1. `Model/get_model_info`: Đọc thông tin mô hình tổng thể (version, project type, file path, units).
    2. `Geometry/get_structural_objects`: Lấy danh sách và thông số Nodes, Bars, Panels.
    3. `Property/get_materials_and_sections`: Đọc thông số vật liệu và tiết diện dầm/cột/tấm.
    4. `Geometry/get_coordinate_systems_and_grids`: Đọc hệ lưới trục và toạ độ làm việc.
    5. `Load/get_load_definitions`: Đọc danh sách trường hợp tải trọng và tổ hợp tải.
    6. `Geometry/draw_bar_by_coords`: Tạo thanh kết cấu mới theo tọa độ 2 đầu kèm gán tiết diện.
    7. `Geometry/assign_node_support`: Gán liên kết gối/điều kiện biên cho nút.
    8. `Property/assign_bar_section`: Gán đặc trưng tiết diện cho thanh.
    9. `Load/assign_bar_load`: Gán tải trọng phân bố hoặc tập trung lên thanh.
    10. `Analysis/run_calculations`: Chạy phân tích kết cấu FEA.
    11. `Results/get_node_reactions`: Trích xuất phản lực gối theo load case/combo.
    12. `Results/get_bar_forces`: Trích xuất biểu đồ nội lực (My, Mz, Fz, Fy, Fx) của thanh.

### R5. Test Suites & Unattended Live Harness
- `HPRobot.Mcp.Server.Tests` (net10.0):
  - Kiểm tra Tool Registry nạp đủ 24 tools (4 core + 8 meta + 12 seeds).
  - Kiểm tra biên dịch thành công 12 seeds đối chiếu với `Interop.RobotOM.dll`.
  - Kiểm tra luồng gọi pipe với Mock Bridge.
- `HPRobot.McpBridge.Tests` (net8.0-windows):
  - Kiểm tra bộ phân tích tier (Read / Write / Heavy-Delete).
  - Kiểm tra bộ quản lý Snapshot `.rtd`.
  - Kiểm tra `RobotUnitsPolicy`.
- `HPRobot/tools/harness/`:
  - Kịch bản Python `run-live-verify.ps1` / `live-verify.py` kế thừa từ `McpShared/tools/harness_common.py` để kiểm thử tương tác thực tế với Robot Structural Analysis Professional 2026.

## Acceptance Criteria

### Build & Solution Integrity
- [ ] Giải pháp `HPRobot/HPRobot.slnx` bao gồm 4 project (`HPRobot.McpBridge`, `HPRobot.Mcp.Server`, `HPRobot.McpBridge.Tests`, `HPRobot.Mcp.Server.Tests`) build thành công không lỗi và không cảnh báo nghiêm trọng.
- [ ] `McpShared` build thành công và toàn bộ unit test hiện tại tiếp tục PASS 100%.

### Tool Surface & Registry
- [ ] `HPRobot.Mcp.Server` khởi chạy stdio và báo cáo chính xác 24 tools qua MCP protocol.
- [ ] 12/12 Seed tools có mã nguồn C# hợp lệ và biên dịch hoàn hảo trên nền tảng `RobotOM`.

### Safety & Snapshot Verification
- [ ] Các script đọc dữ liệu (Read) chạy trực tiếp không tạo file snapshot.
- [ ] Các script thay đổi dữ liệu (Write) tạo thành công bản sao lưu file `.rtd` trước khi thay đổi có hiệu lực.
- [ ] Các script có từ khóa xoá hoặc tính toán (Delete/Heavy) bị chặn với thông báo lỗi cụ thể khi checkbox `AllowHeavyOperations` chưa được bật.

### Automated Test Coverage
- [ ] `HPRobot.Mcp.Server.Tests` chạy bằng `dotnet test` đạt 100% PASS.
- [ ] `HPRobot.McpBridge.Tests` chạy bằng `dotnet test` đạt 100% PASS.

## 2026-09-21T17:20:33Z

# Teamwork Project Prompt — Final

> Status: Launched
> Goal: Execute project via teamwork_preview multi-agent system
> Requested team: Full agent team

Xây dựng bộ giải pháp HPTekla MCP hoàn chỉnh kết nối trí tuệ nhân tạo (AI Agent) với Trimble Tekla Structures 2025.0 thông qua Tekla Open API. Hệ thống bao gồm HPTekla.McpBridge (In-Process Plugin chạy trên .NET Framework 4.8 tích hợp Ribbon UI bên trong Tekla) và HPTekla.Mcp.Server (.NET 10 stdio MCP Console Server) tuân thủ kiến trúc McpShared chuẩn của hệ sinh thái HP MCP.

Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPTekla
Integrity mode: development

## Requirements

### R1. Hợp đồng tích hợp McpShared (Additive Integration)
- Mở rộng thư viện dùng chung `McpShared` (`HPRebar.Mcp.Contracts`, `HPRebar.McpBridge.Core`, `HPRebar.Mcp.Server.Core`) hỗ trợ host Tekla Structures:
  - Khai báo hằng số định danh host `PipeNaming.TeklaHost = "tekla"`, prefix pipe `hptekla-mcp-{version}` và RPC method prefix `tekla`.
  - Cấu hình hợp đồng script `HostScriptContracts.TeklaImports` nạp các namespace chính của Tekla Open API (`Tekla.Structures`, `Tekla.Structures.Model`, `Tekla.Structures.Geometry3d`, `Tekla.Structures.Catalogs`).
  - Định nghĩa `GuardProfile.Tekla` và `AnalyzerProfile.Tekla` với các quy tắc kiểm soát an toàn Roslyn.
  - Định nghĩa DTO ngữ cảnh mô hình `ContextResult.Tekla` / `TeklaInfo` (Tên model, đường dẫn dự án, đơn vị, trạng thái kết nối).
  - Khai báo `HostProfile.Tekla` với `DefaultVersion = 2025` và `ValidVersions = [2025]`.
  - Đảm bảo tính tương thích ngược tuyệt đối (100% additive, không phá vỡ hoặc làm thay đổi hành vi của 9 host hiện có).

### R2. HPTekla.McpBridge — In-Process Plugin bên trong Tekla Structures 2025
- Triển khai `HPTekla.McpBridge` dưới dạng In-Process Plugin / Extension biên dịch cho target framework `net48` (tương thích CLR v4.0.30319 của Tekla Structures 2025.0).
- Khởi tạo `McpBridgeHost` lắng nghe trên Named Pipe `hptekla-mcp-2025`.
- Đồng bộ hóa luồng (Thread Synchronization) qua cơ chế hàng đợi xử lý an toàn trên luồng Tekla Model/UI, đảm bảo các lời gọi Tekla Open API không bị xung đột tiến trình.
- Thiết lập hệ thống an toàn 3 tầng:
  - Phân loại rủi ro (Read / Write / Destructive) dựa trên AST phân tích script.
  - Kiểm soát giao dịch: Chế độ `dryRun = true` cho phép thực thi thử nghiệm mà không gọi `model.CommitChanges()`.
  - Trình quản lý snapshot mô hình tự động sao lưu dữ liệu quan trọng trước khi ghi.
- Tích hợp giao diện quản lý (Ribbon button / WPF Status Dialog) hiển thị trạng thái kết nối pipe, thông tin model hiện hành, nhật ký thực thi và các tùy chọn an toàn.

### R3. HPTekla.Mcp.Server — Stdio Console Server (.NET 10)
- Triển khai `HPTekla.Mcp.Server` là console executable độc lập trên nền .NET 10 (`net10.0`), giao tiếp chuẩn MCP qua `stdio` với AI host (Claude Code, Antigravity, Gemini CLI).
- Đăng ký đầy đủ 24 tools theo tiêu chuẩn HP MCP:
  - **4 Core Tools**: `execute_tekla_code`, `get_tekla_context`, `inspect_type`, `cancel_execution`.
  - **8 Registry Meta Tools**: `search_tools`, `get_tool_schema`, `propose_tool`, `test_tool`, `publish_tool`, `deprecate_tool`, `rollback_tool`, `list_dynamic_tools`.
  - **12 Embedded Seed Tools** nghiệp vụ kết cấu thép và bê tông cốt thép:
    1. `get_model_info`: Lấy thông tin chung về mô hình Tekla đang mở.
    2. `select_objects`: Truy vấn hoặc chọn đối tượng mô hình theo loại hoặc ID.
    3. `get_part_properties`: Đọc thuộc tính chi tiết cấu kiện (profile, vật liệu, class, tọa độ).
    4. `create_beam`: Tạo mới cấu kiện dầm thép hoặc bê tông.
    5. `create_column`: Tạo mới cấu kiện cột thép hoặc bê tông.
    6. `create_contour_plate`: Tạo bản mã hoặc tấm sàn bê tông phẳng.
    7. `create_rebar_group`: Tạo nhóm cốt thép (rebar group/stirrup) với khoảng rải và hình dạng chuẩn.
    8. `create_single_rebar`: Tạo thanh cốt thép đơn lẻ theo các điểm tim.
    9. `modify_user_properties`: Cập nhật User-Defined Attributes (UDA) của cấu kiện.
    10. `get_reinforcement_info`: Trích xuất bảng kê cốt thép và thông số hình học của rebar.
    11. `list_drawings`: Liệt kê và lọc danh sách bản vẽ sản xuất/lắp dựng trong mô hình.
    12. `export_ifc`: Kích hoạt xuất mô hình Tekla ra định dạng IFC phục vụ phối hợp BIM.

### R4. Bộ kiểm thử tự động toàn diện (Test Suites & Verification Harness)
- **HPTekla.Mcp.Server.Tests** (.NET 10):
  - Kiểm thử `TeklaHostProfile` (metadata, pipe name, default version 2025).
  - Kiểm thử danh mục 12 Seed Tools (schema hợp lệ, danh mục, tham số, mô tả).
  - Kiểm thử biên dịch seed script đối với các assembly Tekla Open API tại `C:\Program Files\Tekla Structures\2025.0\bin`.
- **HPTekla.McpBridge.Tests** (.NET Framework 4.8):
  - Kiểm thử biên dịch và thực thi script Roslyn trên runtime net48.
  - Kiểm thử cơ chế phân tích Tier (Read vs Write vs Destructive).
  - Kiểm thử từ chối các thao tác ghi khi `dryRun = true`.
  - Kiểm thử vòng lặp giao tiếp qua Named Pipe với mock Tekla executor.
- **HPRebar.Mcp.Server.Core.Tests**:
  - Bổ sung test kiểm chứng tính trung lập (host-neutrality) cho profile Tekla.
- **Live Verification Harness** (`tools/harness/`):
  - Cung cấp kịch bản Python harness tự động (`mcp-call.py` / `harness_common.py`) kiểm tra kết nối live với Tekla Structures 2025 đang mở.

## Acceptance Criteria

### McpShared Integration
- [ ] Mọi thay đổi trong `McpShared` là hoàn toàn additive; tất cả các test hiện có của `HPRebar.Mcp.Server.Core.Tests` và `HPRebar.McpBridge.Core.Net48Tests` đều pass 100%.
- [ ] `PipeNaming.For("tekla", 2025)` trả về chính xác `hptekla-mcp-2025`.
- [ ] `HostProfile.Tekla` khởi tạo hợp lệ với `DefaultVersion = 2025`, `ValidVersions = [2025]`, và `MethodPrefix = "tekla"`.

### HPTekla Solution & Build Integrity
- [ ] File giải pháp `HPTekla.slnx` bao gồm `HPTekla.McpBridge`, `HPTekla.Mcp.Server`, `HPTekla.McpBridge.Tests`, `HPTekla.Mcp.Server.Tests`.
- [ ] `HPTekla.McpBridge.csproj` biên dịch thành công trên `net48` với tham chiếu tới các assembly Tekla Open API 2025 (`Tekla.Structures.dll`, `Tekla.Structures.Model.dll`, `Tekla.Structures.Catalogs.dll`).
- [ ] `HPTekla.Mcp.Server.csproj` biên dịch thành công trên `net10.0`.
- [ ] Không có cảnh báo hoặc lỗi tham chiếu chéo vi phạm quy tắc AGENTS.md (`HPTekla` chỉ tham chiếu `McpShared`, không tham chiếu host khác).

### Tool Registry & Capability
- [ ] Lệnh `tools/list` trên `HPTekla.Mcp.Server` trả về đúng 24 công cụ (4 core + 8 registry meta + 12 embedded seeds).
- [ ] Toàn bộ 12 seed tools biên dịch thành công và tuân thủ schema JSON của MCP.
- [ ] `execute_tekla_code` cung cấp các biến toàn cục `model`, `selector`, `ct`, `log`, `args` với ngữ cảnh thực thi chính xác.

### Safety & Rollback Enforcement
- [ ] Khi tham số `dryRun` là `true`, không có lệnh `model.CommitChanges()` nào được phép gọi thực tế, đảm bảo mô hình giữ nguyên trạng thái.
- [ ] Phân loại Tier nhận diện chính xác các phương thức sửa đổi mô hình và áp dụng cơ chế bảo vệ phù hợp.

### Automated Test Coverage
- [ ] Toàn bộ test suite trong `HPTekla.Mcp.Server.Tests` pass 100%.
- [ ] Toàn bộ test suite trong `HPTekla.McpBridge.Tests` pass 100%.
- [ ] Thư mục `HPTekla/tools/harness/` chứa đầy đủ kịch bản kiểm thử trực tiếp với Tekla 2025.

## 2026-09-23T23:47:49Z

This is a single self-contained task; keep it small and focused. Integrate a project-local Archify v2.16.0 skill into the HPRebar repository for development architecture documentation, create two verifiable interactive architecture diagrams (HPRebar System Architecture and Column Rebar Workflow), and link them in the existing architecture documentation.

Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar
Integrity mode: development

## Requirements

### R1. Project-Local Archify v2.16.0 Skill Installation
- Extract and install the minimal runtime of Archify from stable tag v2.16.0 into `.agents/skills/archify/` (portable skill) and mirror to `.claude/skills/archify/` (Claude skill).
- Include only required files: `SKILL.md`, `package.json`, `assets/template.html`, `bin/` (`archify.mjs`, `preview.mjs`, `visual-check.mjs`, `open-artifact.mjs`), `renderers/`, `schemas/`, `recipes/`, `references/`, `delta/`, and minimal test fixtures/examples required to satisfy `archify doctor`.
- Exclude website, demo, benchmarks, experiments, tests, DeepSeek integration (`archify-dsh`), and `archify-review`.
- Do NOT run a global `sync-agent-skills.py apply` across the entire repo to avoid conflicting with existing drifted skills (`grill-me`, `hp-mcp-etabs`, `hp-mcp-excel`).
- Do NOT add Node or Archify to any `.csproj`, installer, Revit runtime, or MCP build process.

### R2. Archify System Architecture Diagram
- Create `docs/architecture/diagrams/hprebar-system.architecture.json` adhering to `architecture.schema.json`.
- Accurately model HPRebar's multi-tier boundary:
  - `HPRebar.dll` (in-process Revit add-in, UI/MVVM, commands, external event handlers, Nice3point SDK, R23–R27 multi-version).
  - `HPRebar.Core` (referenced library, netstandard2.0, pure math/geometry, strictly zero references to Autodesk.Revit.*).
  - `Revit Process / Document` (Autodesk Revit host, API thread, element database, transactions).
  - `HPRebar.McpBridge` (separate in-process plugin add-in on R25/R26, Roslyn compiler, pipe listener) and `HPRebar.Mcp.Server` (separate net10 stdio process connecting via named pipe `hprebar-mcp-2026`).

### R3. Archify Column Rebar Workflow Diagram
- Create `docs/architecture/diagrams/column-rebar.workflow.json` adhering to `workflow.schema.json`.
- Accurately map the real execution sequence from code:
  - Step 1: User interaction & UI parameters (`ColumnRebarView` / `ColumnRebarViewModel`).
  - Step 2: Bar layout calculation & core rules (`HPRebar.Core`: `BarLayoutCalculator`, `SpliceCalculator`, `Tolerance`).
  - Step 3: Dispatching to Revit main thread (`ColumnRebarExternalEventHandler.RunAsync()` → `ExternalEvent.Raise()`).
  - Step 4: Transaction orchestration (`ColumnRebarOrchestrator.Run()` opening `TransactionGroup("Column Rebar")`).
  - Step 5: Element generation (`CreateViews`, `CreateDimensions`, `RebarCreationService.Create`, `CreateTables`).
  - Step 6: Commit transaction group (`group.Assimilate()`) or rollback on error (`group.RollBack()`).
  - Step 7: [Proposed] Model QA/QC & clash check (clearly badged with status `proposed` and title `[Đề xuất] Hậu kiểm mô hình`).
  - Step 8: [Proposed] Report & BOM/schedule export (clearly badged with status `proposed` and title `[Đề xuất] Xuất báo cáo & thống kê`).

### R4. Verification, Rendering & Documentation Links
- Run `node .agents/skills/archify/bin/archify.mjs doctor` to verify skill readiness.
- Run `node .agents/skills/archify/bin/archify.mjs validate` on both JSON diagrams to ensure zero schema errors.
- Run `node .agents/skills/archify/bin/archify.mjs deliver` to render standalone interactive HTML artifacts:
  - `docs/architecture/diagrams/hprebar-system.architecture.html`
  - `docs/architecture/diagrams/column-rebar.workflow.html`
- Update `docs/system-architecture.md` to reference the new diagrams with clickable relative links and explanations.
- Ensure `HPRebar.Core` remains 100% free of Autodesk API references and zero C# logic is modified.

## Acceptance Criteria

### Skill Installation & Health
- [ ] `node .agents/skills/archify/bin/archify.mjs doctor` exits with code 0 and reports `[ok]` for all check items.
- [ ] `.agents/skills/archify/SKILL.md` and `.claude/skills/archify/SKILL.md` are present with consistent metadata.
- [ ] No website, benchmarks, experiments, or deepseek packages are copied into the repository.
- [ ] No changes are made to `.csproj`, `.slnx`, or build/installer files.

### Diagram Verification & Delivery
- [ ] `node .agents/skills/archify/bin/archify.mjs validate docs/architecture/diagrams/hprebar-system.architecture.json` passes validation with zero errors.
- [ ] `node .agents/skills/archify/bin/archify.mjs validate docs/architecture/diagrams/column-rebar.workflow.json` passes validation with zero errors.
- [ ] `node .agents/skills/archify/bin/archify.mjs deliver docs/architecture/diagrams/hprebar-system.architecture.json` generates valid standalone HTML at `docs/architecture/diagrams/hprebar-system.architecture.html`.
- [ ] `node .agents/skills/archify/bin/archify.mjs deliver docs/architecture/diagrams/column-rebar.workflow.json` generates valid standalone HTML at `docs/architecture/diagrams/column-rebar.workflow.html`.
- [ ] In `column-rebar.workflow.json`, the post-check and report/export steps are explicitly marked as proposed (`[Đề xuất]`), not current features.

### Documentation & Code Integrity
- [ ] `docs/system-architecture.md` contains updated links to both rendered HTML and JSON diagram files.
- [ ] Zero C# source files (`*.cs`) or XAML files (`*.xaml`) are modified.
- [ ] `HPRebar.Core` references no Autodesk Revit assemblies.
