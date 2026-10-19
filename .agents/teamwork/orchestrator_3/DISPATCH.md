# Dispatch: orchestrator_3 (Project Orchestrator)

## Mission
Xây dựng và tích hợp Trình mô phỏng 3D Cốt thép Dầm trực quan bằng Helix Toolkit (WPF) cùng Mô-đun tính toán & bố trí cốt thép dầm theo TCVN 5574:2018 vào Add-in Revit 2026 (HPRebar), cho phép tương tác 3D thời gian thực, đọc nội lực từ ETABS 22 / Excel và tự động sinh Rebar 3D trong Revit.

## Identity & Hierarchy
- Your identity: `orchestrator_3` (teamwork_preview_orchestrator).
- Your working directory: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_3\`.
- Your parent is: `a708f6f4-aef7-4c05-adf5-a1300c76293e` (Use this conversation ID for all status reporting and communication via `send_message`).

## Reference Documents & Foundations
- Authoritative User Request: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (see timestamp `## 2026-10-07T22:29:51Z`)
- Clean Code Governance: `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/clean-code/host-appendix/revit.md`
- Existing Platform:
  - `HPRebar/HPRebar/BeamRebar/` (View, ViewModel, Services, Canvases 2D hiện có)
  - `HPRebar/HPRebar.Core/` (Domain logic, calculators, models)
  - `HPRebar/HPRebar.Core.Tests/` (xUnit tests)
  - `McpShared/` and `HPEtabs/` (Revit MCP & ETABS MCP infrastructure)

## Requirements to Fulfill

### R1. Tích hợp Thư viện Đồ họa 3D Helix Toolkit vào HPRebar
- Bổ sung package `HelixToolkit.Wpf` vào `HPRebar/HPRebar/HPRebar.csproj` tương thích với cấu hình đa phiên bản (đặc biệt là .NET 8 / `net8.0-windows7.0` cho Revit 2026).
- Cấu hình ILRepack để merge an toàn `HelixToolkit.Wpf` vào `HPRebar.dll`, đảm bảo không xung đột dynamic loading trong tiến trình `Revit.exe`.

### R2. Xây dựng Khung nhìn Viewport 3D Helix (Beam3DViewport)
- Xây dựng UserControl `Beam3DViewport.xaml` và code-behind đóng gói `HelixViewport3D` với:
  - Hệ thống ánh sáng mặc định `DefaultLights`, lưới mặt bằng trục tọa độ `GridLinesVisual3D`, `ViewCube` điều hướng góc nhìn.
  - Khối bê tông dầm bán trong suốt (Semi-transparent Material, Alpha ~ 0.35) thể hiện rõ bao hình học dầm ($b \times h \times L$).
  - Hệ cốt thép dọc 3D dựng bằng `TubeVisual3D` hoặc `MeshBuilder` (chuẩn đường kính thực tế): Thép chủ lớp trên (màu Đỏ), thép chủ lớp dưới (màu Xanh lam), thép cấu tạo bên hông (màu Xanh lá).
  - Hệ cốt đai 3D khép kín theo đúng bước đai gia cường vùng gối ($a100$) và bước đai khoảng giữa nhịp ($a200$).
  - Bộ điều khiển Camera 3D mượt mà: Xoay Orbit (Chuột trái/phải), Pan, Zoom to cursor.

### R3. Nâng cấp Giao diện Điều khiển Tương tác (BeamRebarView & ViewModel)
- Tích hợp `Beam3DViewport` vào `BeamRebarView.xaml` song song với 2 Canvas mặt cắt 2D (Elevation & Section) hiện tại (hỗ trợ chuyển đổi Tab 3D / 2D hoặc bố cục chia màn hình).
- Ràng buộc dữ liệu MVVM phản ứng thời gian thực (Reactive): Khi kỹ sư thay đổi kích thước tiết diện dầm, số lượng thanh thép hoặc bước đai trên giao diện, mô hình 3D trong Helix Viewport tự động tái tạo và cập nhật ngay lập tức.

### R4. Mô-đun Tính toán Cốt thép Dầm theo TCVN 5574:2018 (HPRebar.Core)
- Triển khai thuật toán tính toán diện tích cốt thép yêu cầu $A_s$ (uốn) và $A_{sw}$ (cắt) theo TCVN 5574:2018 cho tiết diện chữ nhật thuần C# trong `HPRebar.Core`.
- Tự động gợi ý phương án đường kính và số thanh thép tối ưu thỏa mãn điều kiện bền và khoảng hở thông thủy cấu tạo ($t \ge 25 - 30\text{ mm}$).
- Đọc bảng nội lực dầm từ 2 nguồn: Import bảng Excel (`.xlsx` qua `ClosedXML`) hoặc đọc trực tiếp từ ETABS 22.

### R5. Tự động Sinh Rebar 3D trong Revit 2026 & Xuất Báo cáo Excel
- Kích hoạt lệnh sinh Rebar bản địa (`Structural Rebar`) trong Revit 2026 vào dầm được chọn thông qua `RebarCreationService`, gán đúng `RebarBarType`, hook và spacing.
- Xuất file thuyết minh tính toán kiểm tra kết cấu chi tiết ra file Excel (`.xlsx`).

## Acceptance Criteria
- 100% C# .NET Native (ĐIỀU 0: Tuyệt đối không dùng Python).
- Tuân thủ HP Clean Code Governance: Lớp $\le 300$ dòng, phương thức $\le 50$ dòng, 0 catch vô cớ, 0 warnings, 0 errors.
- `dotnet build HPRebar/HPRebar.slnx -c Debug.R26` biên dịch thành công 100%.
- Khung nhìn 3D `HelixViewport3D` mượt mà, phản ứng tức thì ($< 50\text{ms}$).
- 100% Unit tests trong `HPRebar.Core.Tests` kiểm tra công thức TCVN 5574:2018 đạt PASS.
- Sinh Rebar 3D vào Revit Document chính xác vị trí, lớp bảo vệ và không gây lỗi `InvalidOperationException`.

## Operational Protocol
1. Tạo `BRIEFING.md` và `progress.md` trong working directory ngay khi khởi động.
2. Thiết lập kế hoạch chi tiết (`plan.md`), phân chia milestone rõ ràng.
3. Điều phối các worker chuyên biệt (Explorer, Worker, Reviewer, Challenger) theo quy trình Clean Code.
4. Cập nhật `progress.md` liên tục sau mỗi tiến độ quan trọng để Sentinel theo dõi.
5. Khi hoàn thành toàn bộ công việc, gửi báo cáo tổng kết và handoff cho Sentinel qua `send_message`.
