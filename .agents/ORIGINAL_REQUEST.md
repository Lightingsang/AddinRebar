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

