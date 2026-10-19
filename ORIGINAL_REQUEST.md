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
- [ ] `node .agents/skills/archify/bin/archify.mjs deliver docs/architecture/diagrams/hprebar-system.architecture.html` generates valid standalone HTML at `docs/architecture/diagrams/hprebar-system.architecture.html`.
- [ ] `node .agents/skills/archify/bin/archify.mjs deliver docs/architecture/diagrams/column-rebar.workflow.html` generates valid standalone HTML at `docs/architecture/diagrams/column-rebar.workflow.html`.
- [ ] In `column-rebar.workflow.json`, the post-check and report/export steps are explicitly marked as proposed (`[Đề xuất]`), not current features.

### Documentation & Code Integrity
- [ ] `docs/system-architecture.md` contains updated links to both rendered HTML and JSON diagram files.
- [ ] Zero C# source files (`*.cs`) or XAML files (`*.xaml`) are modified.
- [ ] `HPRebar.Core` references no Autodesk Revit assemblies.



## 2026-10-07T22:29:51Z

Xây dựng và tích hợp Trình mô phỏng 3D Cốt thép Dầm trực quan bằng Helix Toolkit (WPF) cùng Mô-đun tính toán & bố trí cốt thép dầm theo TCVN 5574:2018 vào Add-in Revit 2026 (HPRebar), cho phép tương tác 3D thời gian thực, đọc nội lực từ ETABS 22 / Excel và tự động sinh Rebar 3D trong Revit.

Working directory: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRebar
Integrity mode: development

Tài liệu quy chuẩn & Nền tảng tham chiếu:
- Quy chuẩn Clean Code bắt buộc: docs/clean-code/HP_CLEAN_CODE_CORE.md, docs/clean-code/host-appendix/revit.md
- Nền tảng hiện hữu: HPRebar/HPRebar/BeamRebar/ (View, ViewModel, Services, Canvases 2D hiện có)
- Hạ tầng Revit MCP & ETABS MCP trong repo: McpShared/ và HPEtabs/

## Requirements

### R1. Tích hợp Thư viện Đồ họa 3D Helix Toolkit vào HPRebar
- Bổ sung package HelixToolkit.Wpf vào HPRebar/HPRebar/HPRebar.csproj tương thích với cấu hình đa phiên bản (đặc biệt là .NET 8 / net8.0-windows7.0 cho Revit 2026).
- Cấu hình ILRepack để merge an toàn HelixToolkit.Wpf vào HPRebar.dll, đảm bảo không xung đột dynamic loading trong tiến trình Revit.exe.

### R2. Xây dựng Khung nhìn Viewport 3D Helix (Beam3DViewport)
- Xây dựng UserControl Beam3DViewport.xaml và code-behind đóng gói HelixViewport3D với:
  - Hệ thống ánh sáng mặc định DefaultLights, lưới mặt bằng trục tọa độ GridLinesVisual3D, ViewCube điều hướng góc nhìn.
  - Khối bê tông dầm bán trong suốt (Semi-transparent Material, Alpha ~ 0.35) thể hiện rõ bao hình học dầm (b x h x L).
  - Hệ cốt thép dọc 3D dựng bằng TubeVisual3D hoặc MeshBuilder (chuẩn đường kính thực tế): Thép chủ lớp trên (màu Đỏ), thép chủ lớp dưới (màu Xanh lam), thép cấu tạo bên hông (màu Xanh lá).
  - Hệ cốt đai 3D khép kín theo đúng bước đai gia cường vùng gối (a100) và bước đai khoảng giữa nhịp (a200).
  - Bộ điều khiển Camera 3D mượt mà: Xoay Orbit (Chuột trái/phải), Pan, Zoom to cursor.

### R3. Nâng cấp Giao diện Điều khiển Tương tác (BeamRebarView & ViewModel)
- Tích hợp Beam3DViewport vào BeamRebarView.xaml song song với 2 Canvas mặt cắt 2D (Elevation & Section) hiện tại (hỗ trợ chuyển đổi Tab 3D / 2D hoặc bố cục chia màn hình).
- Ràng buộc dữ liệu MVVM phản ứng thời gian thực (Reactive): Khi kỹ sư thay đổi kích thước tiết diện dầm, số lượng thanh thép hoặc bước đai trên giao diện, mô hình 3D trong Helix Viewport tự động tái tạo và cập nhật ngay lập tức.

### R4. Mô-đun Tính toán Cốt thép Dầm theo TCVN 5574:2018 (HPRebar.Core)
- Triển khai thuật toán tính toán diện tích cốt thép yêu cầu As (uốn) và Asw (cắt) theo TCVN 5574:2018 cho tiết diện chữ rectangular thuần C# trong HPRebar.Core.
- Tự động gợi ý phương án đường kính và số thanh thép tối ưu thỏa mãn điều kiện bền và khoảng hở thông thủy cấu tạo (t >= 25 - 30 mm).
- Đọc bảng nội lực dầm từ 2 nguồn: Import bảng Excel (.xlsx qua ClosedXML) hoặc đọc trực tiếp từ ETABS 22.

### R5. Tự động Sinh Rebar 3D trong Revit 2026 & Xuất Báo cáo Excel
- Kích hoạt lệnh sinh Rebar bản địa (Structural Rebar) trong Revit 2026 vào dầm được chọn thông qua RebarCreationService, gán đúng RebarBarType, hook và spacing.
- Xuất file thuyết minh tính toán kiểm tra kết cấu chi tiết ra file Excel (.xlsx).

## Acceptance Criteria

### Tính toàn vẹn Mã nguồn & Clean Code
- [ ] 100% C# .NET Native (ĐIỀU 0: Tuyệt đối không dùng Python).
- [ ] Tuân thủ HP Clean Code Governance: Lớp <= 300 dòng, phương thức <= 50 dòng, 0 catch vô cớ, 0 warnings, 0 errors.
- [ ] Lệnh dotnet build HPRebar/HPRebar.slnx -c Debug.R26 biên dịch thành công 100%.

### Hiệu năng & Trải nghiệm 3D
- [ ] Khung nhìn 3D HelixViewport3D hiển thị mượt mà 60 FPS, xoay/zoom không giật lag.
- [ ] Cốt thép 3D hiển thị dạng thanh ống tròn đúng tỷ lệ đường kính và màu sắc quy ước.
- [ ] Cập nhật 3D tức thì (< 50ms) khi thay đổi dữ liệu cấu hình thép.

### Độ chính xác Tính toán TCVN & Revit
- [ ] 100% Unit tests trong HPRebar.Core.Tests kiểm tra công thức TCVN 5574:2018 đạt PASS.
- [ ] Sinh Rebar 3D vào Revit Document chính xác vị trí, lớp bảo vệ và không gây lỗi InvalidOperationException.


## 2026-10-08T08:01:51Z

Build HPDynamo MCP (Model Context Protocol) subsystem for Dynamo in Revit 2026, enabling AI coding agents to inspect, author, and execute Dynamo graphs (.dyn) and DesignScript/C# code both headlessly and live on the active Revit canvas.

Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar
Integrity mode: development

## Requirements

### R1. McpShared Dynamo Profile & Pipe Contracts
Extend `McpShared` to define Dynamo host contracts without breaking existing hosts:
- Add `PipeNaming.DynamoHost = "hpdynamo-mcp-2026"`.
- Add `HostProfile.Dynamo` and Dynamo-specific `GuardProfile` / `AnalyzerProfile` with standard namespaces and safety rules.
- Maintain strict host-neutrality and wire contract compatibility in `McpShared/`.

### R2. HPDynamo Solution & MCP Server Executable (`HPDynamo.Mcp.Server`)
Create `HPDynamo/HPDynamo.slnx` and `HPDynamo.Mcp.Server` (.NET 10.0 console application, stdio transport):
- Implement `Program.cs` bootstrapping `McpServerHost.CreateBuilder(args, HostProfile.Dynamo)`.
- Implement Core Tools: `execute_dynamo_code`, `execute_designscript`, `get_dynamo_context`, `cancel_execution`.
- Wire 8 Registry Meta Tools (`search_tools`, `get_tool`, `run_tool`, `get_run`, `propose_tool`, `test_tool`, `publish_tool`, `manage_tool`).
- Embed and auto-install the 12 embedded seed tools library.

### R3. Dual-Host In-Process Bridge (`HPDynamo.McpBridge`)
Create `HPDynamo.McpBridge` (.NET 8.0-windows7.0) running inside `Revit.exe`:
- **Dual-Host Lifecycle:**
  1. Revit Add-in (`IExternalApplication` + `.addin` manifest) that starts the Named Pipe listener `hpdynamo-mcp-2026` immediately upon Revit startup (allowing headless execution via `RevitDynamoModel` without Dynamo UI open).
  2. Dynamo ViewExtension (`IViewExtension` + XML manifest) that binds to the active `DynamoViewModel` / `WorkspaceModel` when Dynamo UI is opened, enabling live canvas node/wire authoring and inspection.
- **Dual-Level Safety & Rollback:**
  - Wrap graph executions modifying Revit documents in a Revit `TransactionGroup` ("MCP Dynamo: <label>"); rollback completely when `dryRun=true`.
  - Use Dynamo workspace snapshot / `UndoRecorder` for canvas-level changes.
- **ScriptGuard & Compiler:**
  - Roslyn compiler with `ScriptArgs`, denying forbidden namespaces (`System.IO`, `System.Net`, `Process`, unauthorized `Reflection`).
  - Direct DesignScript execution engine interacting with Dynamo geometry and Revit nodes.
- **Bridge UI:**
  - Modeless status window with MaterialDesignThemes 5.3.2, dark/light theme tracking, and mandatory "Allow AI Dynamo execution" opt-in toggle (default OFF).

### R4. Embedded Seed Library (12 Seeds)
Implement 12 verified seed tools under `HPDynamo.Mcp.Server/Registry/SeedLibrary/`:
1. `run_dyn_file`: Execute a `.dyn` file with input parameter dictionary overrides.
2. `get_active_graph_info`: Inspect active canvas workspace (nodes count, wires, run status, errors).
3. `search_nodes_library`: Search available node definitions in loaded Dynamo packages & core.
4. `inspect_node_type`: Get input/output ports, descriptions, and type signatures of a node.
5. `add_node_to_workspace`: Place a new node on canvas at coordinates (X, Y).
6. `connect_node_ports`: Wire an output port of Node A to an input port of Node B.
7. `disconnect_node_ports`: Disconnect wire between specified ports.
8. `set_node_properties`: Configure node attributes (title, slider range, boolean, text).
9. `get_node_output_values`: Retrieve evaluated output data from nodes after execution.
10. `export_graph_to_image`: Render canvas or graph to PNG image artifact.
11. `validate_dyn_file`: Inspect a `.dyn` JSON file for missing packages, cyclic wires, or errors.
12. `create_code_block_node`: Generate a DesignScript CodeBlock node with script content.

### R5. Test Suites & Quality Assurance
- `HPDynamo.Mcp.Server.Tests` (.NET 10.0, xUnit v3):
  - Registry lifecycle tests (draft -> tested -> pending_approval -> published).
  - Seed tool compilation tests against Dynamo and Revit reference assemblies.
  - ScriptGuard deny-list and AST analyzer tests.
- `HPDynamo.McpBridge.Tests` (.NET 8.0-windows7.0):
  - Dispatcher and named pipe protocol tests.
  - Headless graph parser and DesignScript runner tests.
- Compliance with `HP_CLEAN_CODE_CORE.md` (no forbidden namespaces, no commented-out code, structured exception logging).

## Acceptance Criteria

### Build & Packaging
- [ ] `dotnet build HPDynamo/HPDynamo.slnx` succeeds with 0 errors and 0 warnings.
- [ ] `HPDynamo.Mcp.Server` publishes cleanly as a self-contained / win-x64 console exe.
- [ ] Deployment manifests (`HPDynamo.McpBridge.addin` and `HPDynamo_ViewExtensionDefinition.xml`) are correctly generated and positioned.

### Server & Tool Surface
- [ ] Starting `HPDynamo.Mcp.Server.exe` and sending `tools/list` returns exactly 24 tools (4 Core + 8 Registry + 12 Seeds) with valid JSON Schema.
- [ ] Script proposals via `propose_tool` undergo AST validation and reject scripts exceeding 300 lines or containing forbidden namespaces.

### Execution & Safety
- [ ] Named Pipe listener `hpdynamo-mcp-2026` handles requests with JSON-RPC 2.0 NDJSON.
- [ ] `dryRun=true` in `execute_dynamo_code` or `run_dyn_file` rolls back all created Revit elements via Revit `TransactionGroup`.
- [ ] Script execution is rejected when the bridge UI opt-in checkbox is OFF.

### Test Verification
- [ ] All unit and integration tests in `HPDynamo.Mcp.Server.Tests` and `HPDynamo.McpBridge.Tests` pass.
- [ ] `McpShared` regression tests continue to pass without breaks (`HPRebar.Mcp.Server.Core.Tests`).

## 2026-10-08T11:29:50Z

Triển khai hoàn thiện quy trình BIM Pipeline và Coordination Ledger cho HPBim (P0 và P1) giải quyết triệt để 10 lỗi chặn từ review ngày 08/10/2026, bảo đảm kết quả trung thực tuyệt đối, preview/apply tương đương, phục hồi an toàn sau sự cố và loại bỏ mọi hình thức shadow testing.

Working directory: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim
Integrity mode: development

## Requirements

### R1. Phase 1 — P0 Core Wire, Cardinality & Strict Batch Transaction
- **Canonical String SourceKey**: Chuẩn hóa `SourceKey` thành chuỗi canonical `[DocIdentifier]#[Category]:[PrimaryHandle]` trên toàn bộ `ElementPlanItem`, wire DTO, `ScriptArgs`, Roslyn runner và `HPBimCoordinationReceipt`. Bảo đảm 100% roundtrip equality qua JSON serialization/deserialization.
- **Cardinality 1:1 & Không Synthesize Created**: Bắt buộc đối soát chính xác 1:1 giữa danh sách items trong plan và execution records trả về từ Revit. Cấm tuyệt đối việc tự tạo kết quả `Created` khi response thiếu records hoặc khi response bị cắt cụt do vượt ngưỡng 64KB (`execRes.Truncated == true`). Bất kỳ sự thiếu hụt nào phải đánh dấu ngay `OutcomeUnknown`.
- **Strict All-or-Nothing Batch Transaction**: Khi bất kỳ cấu kiện nào trong batch gặp lỗi (`Failed`), rollback toàn bộ transaction của batch đó trên Revit, không ghi receipt vào Extensible Storage, và báo lỗi toàn diện, bảo đảm không để lại mô hình dở dang trong RVT.
- **Loại bỏ Heuristic Fallbacks trong Script Runner**: Áp dụng đầy đủ `rotationDeg` và `topLevel` constraint cho Cột. Loại bỏ hoàn toàn các lệnh `FirstOrDefault()` khi chọn Pipe/PipingSystem type; nếu thiếu symbol/type phải báo lỗi tường minh `ERR_SYMBOL_NOT_FOUND`.

### R2. Phase 2 — P1 Document Identity, Complete Hash & Journal Hydration
- **Ràng buộc Định danh Tài liệu Đích**: CLI `generate` tự động truy vấn thông tin tài liệu Revit đang hoạt động (Title, Path, VersionGUID) trên Revit API thread trước khi biên dịch plan; gắn chặt vào `ExecutionPlan` và `IntentJournal`. Pre-flight trên Revit runner sẽ từ chối thực thi nếu phát hiện sai tài liệu hoặc snapshot đã lỗi thời.
- **Canonical Plan Hash Toàn diện**: `BimExecutionPlan.ComputePlanHash` phải bao phủ toàn bộ các thuộc tính ảnh hưởng đến hình học và mô hình: tọa độ tất cả đỉnh polygon của profile, độ dày, cao độ, thông số tiết diện, family, host, top level và document identity.
- **Hydration Index Sổ cái & Bảo toàn Metadata Journal**:
  - `BimCoordinationLedgerService.LoadFromFile` phải tự động khôi phục và hydrate toàn bộ chỉ mục `_mappings` (SourceKey → Revit UniqueId) từ dữ liệu lưu trên đĩa.
  - `IntentJournal.UpdateStatusAsync` khi cache miss phải nạp lại record gốc từ đĩa trước khi cập nhật trạng thái, bảo toàn 100% metadata (`PlanHash`, `ItemCount`, `SourceDoc`, `TargetDoc`) qua các phiên process restart.
  - `BimCoordinationLedgerSerializer` thực hiện Atomic File Replace (.tmp -> atomic replace) kèm file-lock để chống hư hỏng dữ liệu khi tiến trình bị tắt đột ngột.
- **Đối soát Reconcile 3 Yếu tố Chặt chẽ**: `ReconcileService` chỉ công nhận `Committed` khi receipt trong Extensible Storage khớp đồng thời cả 3 yếu tố: `OperationId`, `PlanHash`, và `TargetDocId`. Nếu không tìm thấy receipt hoặc thiếu bằng chứng, giữ nguyên trạng thái `OutcomeUnknown` để ngăn chặn retry ghi đè mù quáng.

### R3. Phase 3 — Đo đạc Hình học QA/QC Thực tế & Chuyển đổi E2E Tests
- **Kiểm toán Hình học Thực tế (Measured Geometric Verification)**: Nối `MeasuredGeometricVerifier` vào lệnh `VerifyCommand` và quy trình nghiệm thu CLI. Thực hiện truy vấn trực tiếp hình học (Solid, Curve, Elevation) từ Revit API qua named pipe để so sánh với dung sai `CoordinationTolerances`. Loại bỏ toàn bộ các số liệu gán sẵn (`AvgCadEdgeDevMm=0.5`...) và cấm nghiệm thu nếu thiếu dữ liệu đối chiếu (phải trả về `Inconclusive`).
- **Xóa bỏ Shadow Mocks trong E2E Tests**: Refactor toàn bộ `HPBim.E2E.Tests`, loại bỏ hoàn toàn các lớp shadow contracts tự tạo (`E2ESchemaValidator`, `E2EExecutionPlan`, `E2EIntentJournal`...), chuyển 100% sang kiểm thử trực tiếp các production classes của `HPBim.Core` và `HPBim.Client`. Chỉ sử dụng fake test ở biên truyền tải I/O Named Pipe.

## Acceptance Criteria

### Verification & Testing
- [ ] **Test Production Coverage**: 100% tests trong `HPBim.Core.Tests`, `HPBim.Client.Tests`, và `HPBim.E2E.Tests` sử dụng trực tiếp production classes và PASS 100% (zero regressions trên toàn bộ 3,489 tests của repo).
- [ ] **SourceKey Wire Roundtrip**: Test chứng minh `CompositeSourceKey` string canonical giữ nguyên equality sau chu trình `ExecutionPlan` -> `BridgeJson` -> `ScriptArgs` -> `BimPlanRunner` -> `ExecuteResult` -> `Ledger/Receipt`.
- [ ] **Strict Batch Rollback**: Test chứng minh khi 1 cấu kiện trong batch 10 phần tử bị lỗi, toàn bộ batch bị rollback, không có receipt nào được tạo và CLI trả về exit code 1.
- [ ] **No Synthesized Created**: Test chứng minh khi response bị rỗng hoặc truncated >64KB, pipeline trả về `OutcomeUnknown` và kích hoạt Reconcile, không tự tạo kết quả `Created`.
- [ ] **Ledger & Journal Persistence Across Restarts**: Test khởi tạo instance `IntentJournal` và `BimCoordinationLedgerService` mới từ file trên đĩa chứng minh `_mappings` được hydrate đầy đủ và metadata của intent không bị mất khi cập nhật trạng thái.
- [ ] **Measured Geometric Verification**: Cấu kiện sai lệch hình học hoặc thiếu bằng chứng đo đạc từ Revit API bị đánh dấu `Inconclusive` hoặc `Defect`, cấm tự động chuyển sang `ConfirmedPass`.
- [ ] **Revit API Binding Probe**: Biên dịch thành công mã runner C# Roslyn đối chiếu với các reference assemblies của Revit 2026.

### Repository & Governance Rules
- [ ] Không tạo dependency hai chiều giữa `HPAutoCad` và `HPRebar`.
- [ ] Tuân thủ triệt để Rule 0 (Zero guesswork) và Clean Code: file <= 300 dòng (Rule C2), method <= 50 dòng (Rule M3), không code comment (Q-B1), không empty catch (Q-B2).
- [ ] Không tự ý commit/push git hoặc deploy add-in ra môi trường thực.


## 2026-10-09T05:36:26Z

Thực hiện kế hoạch hiệu chỉnh toàn diện phân hệ HPDynamo MCP nhằm khắc phục triệt để 8 phát hiện kỹ thuật (RV-01 đến RV-08) từ báo cáo đánh giá độc lập Codex Review 2026-10-09, bảo đảm tính đúng đắn giao dịch Revit/Dynamo, cơ chế rollback canvas toàn diện cho dryRun, an toàn hàng đợi và cách ly hủy bỏ theo request ID, đồng bộ hóa evaluation xuyên suốt core tools và seeds, trích xuất chuẩn xác dữ liệu ProtoCore MirrorData và DesignScript Dictionary, cùng harness benchmark thực nghiệm lưu raw data.

Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar
Integrity mode: development

## Requirements

### R1. Tính Đúng Đắn Giao Dịch & Canvas Rollback Toàn Vẹn (RV-01, RV-06)
- **Chuẩn hóa nguồn `dryRun` duy nhất tại execution boundary**: Đảm bảo các script và seed (bao gồm `batch_edit_graph`) nhận biết chính xác cờ `dryRun` từ `ExecuteRequest` (đặc biệt khi `DynamicToolRegistrar` đã bóc tách cờ này khỏi `args`).
- **Rollback canvas toàn diện cho `batch_edit_graph`**: Kết hợp Dynamo `UndoRecorder` khi ở Canvas UI với danh sách programmatic rollback snapshot (ghi nhận trạng thái dây cũ bị thay thế, giá trị/thuộc tính cũ của nodes có sẵn). Khi `dryRun = true` hoặc phát sinh ngoại lệ/cancel ở bất kỳ thao tác nào, phải hoàn tác 100% thay đổi trên canvas Dynamo và rollback Revit `TransactionGroup`.
- **Phân định quyền quản lý Transaction thông minh trong `DynamoExecutionEngine`**:
  - Nếu script C# Roslyn thao tác trực tiếp Revit DB (gọi `doc.Create`, set parameter...) với `transaction = "auto"`, kích hoạt `startAutoTransaction: true`.
  - Nếu phát hiện script gọi wrapper `Revit.Elements.` hoặc tự quản lý qua Dynamo `TransactionManager`, đặt `startAutoTransaction: false` để nhường quyền mở child transaction cho Dynamo và chỉ bọc `TransactionGroup` ngoài cùng.

### R2. Đồng Bộ Hóa Evaluation & Queue Ownership Khi Cancel (RV-02, RV-03, RV-05)
- **Xác thực kết quả `RunAndWait`**: Trong `CanvasGraphRunner`, kiểm tra chặt chẽ giá trị trả về của `DynamoEvaluationSync.RunAndWait`. Nếu evaluation thất bại, timeout hoặc cancel thì không được trả `Success = true`, phải ghi log lỗi và kích hoạt rollback transaction.
- **Thống nhất đường Evaluation Sync**: Đảm bảo cả `run_dyn_file` và `batch_edit_graph` đều đi qua cơ chế chờ `EvaluationCompleted` trước khi trích xuất kết quả và trạng thái lỗi.
- **Bảo toàn Linked Timeout Token**: Truyền đúng `linkedToken` từ `RevitTransactionScope` vào runner execution callback thay vì dùng raw work item token.
- **Quản lý trạng thái Work Item và Thread Ownership**: Quản lý rõ ràng trạng thái (`Queued`, `Running`, `Completed`). Khi cancellation xảy ra trong lúc work đang chạy trên Revit thread, không được set `_busy = 0` sớm; giữ thread ownership cho tới khi toàn bộ thao tác thực thi và rollback trên Revit hoàn tất.

### R3. Request-Scoped Cancel & Trích Xuất Dữ Liệu ProtoCore MirrorData (RV-04, RV-07, RV-08)
- **Cách ly lệnh Cancel theo Request ID**: Bổ sung interface `ICorrelatedCancelExecutor` trong `McpShared/HPRebar.McpBridge.Core` kế thừa hoặc bổ trợ `IBridgeExecutor` (đảm bảo 100% tương thích nhị phân và mã nguồn cho 10 host khác, kể cả net48). `RequestDispatcher` kiểm tra và truyền `requestId` nếu executor hỗ trợ. `DynamoBridgeCoordinator` chỉ hủy request khớp với ID được yêu cầu.
- **Sửa Serializer trích xuất MirrorData & Dictionary**: Trong `get_node_output_values/code.cs`:
  - Kiểm tra `MirrorData.IsCollection` trước; nếu là collection, duyệt qua `MirrorData.GetElements()` thay vì bỏ qua khi `MirrorData.Data == null`.
  - Nhận diện và trích xuất cấu trúc `DesignScript.Builtin.Dictionary` thông qua các thuộc tính `.Keys` và phương thức `.ValueAtKey()`.
- **Kiểm soát ngân sách Payload**: Bổ sung giới hạn số lượng phần tử tối đa (breadth budget) và độ sâu đệ quy (depth budget), đồng thời kiểm tra cooperative `cancellationToken` trong quá trình duyệt dữ liệu lớn.

### R4. Benchmark Thực Nghiệm, Raw Data & Đồng Bộ Tài Liệu
- **Harness Benchmark Thực Nghiệm**: Tạo kịch bản PowerShell `run-benchmark.ps1` thực hiện đo đạc các workload chuẩn trên file model test copy cô lập; lưu file kết quả thô `reports/baseline-vs-upgraded.json` ghi nhận p50/p95, round-trips và UTF-8 payload bytes.
- **Hiệu Chỉnh Tài Liệu & Phân Định Bằng Chứng**: Cập nhật `audit.md`, `plan.md`, `benchmark-plan.md` làm rõ ranh giới giữa giả thuyết ước tính và số liệu đo đạc thực tế; ghi nhận thực nghiệm TFM .NET 10 của Revit 2026.
- **Cập Nhật Skill `hp-mcp-dynamo`**: Cập nhật số lượng công cụ lên 25 tools (4 core + 8 registry + 13 seeds) và bổ sung tài liệu hướng dẫn sử dụng `batch_edit_graph`.

## Controlled Infrastructure & Safety Constraints
- **Tuyệt đối không can thiệp phiên làm việc Revit hiện hành**: Không inject code, không mở hay đóng model người dùng đang mở trong PID 26264; mọi kiểm chứng live phải thực hiện trên file bản sao riêng biệt trong thư mục test.
- **Bảo toàn McpShared**: Mọi thay đổi trong `McpShared/` phải giữ nguyên tính host-neutral, tương thích tuyệt đối cả .NET 10 và .NET Framework 4.8, không gây breaking changes cho các bridge host khác.
- **Tuân thủ Clean Code Governance**: Mỗi file C# không vượt quá 300 dòng, không chứa khối catch rỗng thiếu lý do, không để lại code bị comment, và đạt chuẩn Q-W1 đến Q-W5.

## Acceptance Criteria

### Tính Đúng Đắn Giao Dịch & Rollback (RV-01, RV-06)
- [ ] Gọi `batch_edit_graph` với `dryRun = true` qua MCP: Toàn bộ node mới được dọn dẹp, các dây và thuộc tính của node cũ được phục hồi nguyên vẹn trên canvas Dynamo, và Revit `TransactionGroup` được rollback sạch sẽ.
- [ ] Gọi `batch_edit_graph` bị lỗi ở bước giữa: Toàn bộ các thao tác đã áp dụng trước đó được rollback tự động về trạng thái ban đầu.
- [ ] Script C# Roslyn thuần túy tạo element Revit (`doc.Create...`) dưới `execute_dynamo_code` với `transaction = "auto"` thực thi thành công, không gặp lỗi thiếu transaction.
- [ ] Script Dynamo tạo element thông qua wrapper Dynamo được bọc an toàn trong `TransactionGroup`, không phát sinh lỗi lồng transaction.

### Đồng Bộ Evaluation & Queue Safety (RV-02, RV-03, RV-05)
- [ ] Khi evaluation bị lỗi cú pháp, runtime error hoặc timeout, `CanvasGraphRunner` báo `Success = false` và rollback transaction tương ứng.
- [ ] `run_dyn_file` và `batch_edit_graph` chờ đợi sự kiện `EvaluationCompleted` hoàn tất trước khi đọc dữ liệu đầu ra.
- [ ] Khi một request đang chạy trên Revit thread bị cancel, cờ `_busy` vẫn giữ nguyên giá trị `true` cho tới khi Revit thread kết thúc và hoàn tất rollback, ngăn ngừa hoàn toàn tình trạng request mới chạy chồng lấn.

### Serialization & Cancellation (RV-04, RV-07, RV-08)
- [ ] Lệnh cancel gửi kèm `requestId` của một request cũ đã kết thúc không làm hủy nhầm request mới đang thực thi.
- [ ] Node CodeBlock xuất mảng (ví dụ `[1, 2, 3]` hoặc nested array) trích xuất thành công JSON Array tương ứng, không bị trả về `null`.
- [ ] Node xuất `DesignScript.Builtin.Dictionary` trích xuất thành công JSON Object chứa các cặp key-value tương ứng.
- [ ] Quá trình trích xuất mảng dữ liệu cực lớn tôn trọng giới hạn budget và hủy bỏ tức thì khi token cancellation được kích hoạt.

### Kiểm Chứng Test Suites & Tài Liệu
- [ ] 100% các bộ test suites tiếp tục pass:
  - `HPDynamo.Mcp.Server.Tests` (net10.0)
  - `HPDynamo.McpBridge.Tests` (net10.0-windows7.0)
  - `HPDynamo.E2E.Tests` (net10.0)
  - `HPRebar.Mcp.Server.Core.Tests` (net10.0)
  - `HPRebar.McpBridge.Core.Net48Tests` (net48)
- [ ] File benchmark raw data `reports/baseline-vs-upgraded.json` được tạo thành công với số liệu đo thực tế.
- [ ] `audit.md`, `plan.md` và skill `hp-mcp-dynamo` được cập nhật đồng bộ, không còn thông tin mâu thuẫn.
