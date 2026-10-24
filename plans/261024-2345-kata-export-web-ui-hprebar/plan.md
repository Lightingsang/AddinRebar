# Kata Export — giao diện web (WebView2 + React) trong HPRebar, Revit 2026

2026-10-24. Thay plan `261024-2245-kata-export-web-module` (module Toolkit — bỏ). Quy trình: skill `/revit-webview-addin`.

## Quyết định người dùng (2026-10-24)
1. Xây lại **giao diện** Kata Export trong add-in HPRebar, không tạo tool mới; nút HPRebar ▸ Kata Export giữ nguyên.
2. Revit 2026 dùng UI web; R23–R25/R27 giữ cửa sổ WPF hiện tại (`#if REVIT2026`).
3. Cửa sổ modeless như hiện tại (không dockable pane).
4. Gỡ Kata khỏi Toolkit (phase 1 cất ở `git stash@{0}`; phần Kata của commit 1a96ece gỡ ở phase 0), giữ sửa deadlock preloader.

## Hiện trạng (scout)
- Giữ nguyên: `KataExportCommand` (chọn dầm → session), `KataExportExternalEventHandler`, `IKataExportRunner`, toàn bộ `Service/`, engine `HPRebar.Core/KataExport|KataRebar`.
- `KataExportViewModel` (+ `.Rebar`, `.Removal`, `.Navigation`, ~620 dòng) là máy trạng thái: sheet, Excel probe/ghi, kế hoạch thép, bỏ nhóm thép, tạo thép.
- View: `KataExportView.xaml` 813 dòng + bộ vẽ `View/Controls/` ~3k dòng; chỉ `KataDrawPrimitives` gọi `DrawingContext`, các painter dùng type Pen/Brush/Geometry của WPF.

## Thiết kế đề xuất
| Hạng mục | Đề xuất | Phương án khác |
|---|---|---|
| Trạng thái + nghiệp vụ | **Giữ ViewModel**; thêm adapter bridge: VM → `KataViewState` (DTO) mỗi khi đổi, lệnh JSON → command của VM | viết lại logic phía trang (trùng, lệch WPF) |
| Vẽ mặt đứng / mặt cắt | **Một bộ painter cho cả hai UI**: `KataDrawPrimitives` thành giao diện vẽ trung tính, backend WPF + backend SVG (C#); trang hiện SVG, click theo `data-*` (cột, nhóm thép) | port painter sang TS/SVG (~2,5k dòng, hai bộ vẽ lệch nhau vì WPF còn sống) |
| Hạ tầng WebView | **Thư viện dùng chung `HPRebar/HPWebHost`** (net8-windows): host WebView2 (Revit's copy, compile-only), asset nhúng + CSP, transport, dispatcher/protocol, preloader; Toolkit và HPRebar(R26) cùng dùng | chép sang `HPRebar/Shared/Web` (trùng với Toolkit) |
| Frontend | **Dùng chung `HPBimToolkit.Frontend`, thêm trang `kata.html`** (bridge client, UI kit, generator kiểu TS dùng lại); HPRebar chỉ nhúng asset của trang Kata | project frontend riêng cho HPRebar |
| Cài đặt Kata | Giữ cửa sổ WPF "Kata Settings" (nút riêng) | làm web sau nếu cần |

## Phases
| # | Phase | Gate |
|---|---|---|
| 0 ✅ | Dọn Toolkit Kata (gỡ tham chiếu HPRebar.Core + Excel COM, ADR-0009 Superseded, D8), ADR-0010 (mở ngoại lệ ADR-0008 cho Kata Export R26), spike: cửa sổ WebView2 trong HPRebar R26 + warm-up assembly cho context HPREBAR | build R26, spike live, Dashboard regression |
| 1 ✅ | `HPWebHost` + Toolkit chuyển sang dùng (không đổi hành vi) | Toolkit 22/13/6 live PASS |
| 2 ✅ | Bridge adapter trên ViewModel: hợp đồng `kata.*`, state DTO, lệnh | test Core + build |
| 3 ✅ | Bộ vẽ trung tính + backend SVG; WPF dùng backend WPF như cũ | ảnh WPF trước/sau giống; SVG test |
| 4 ✅ | Trang Kata đủ tính năng XAML (header, tham số, đảo chiều, bảng, Excel, thép, cảnh báo) | 4 lệnh npm; so WPF trên bản copy |
| 5 🟡 | Live parity: đọc → ghi Excel nháp → đọc lại → tạo thép → Undo trên bản copy `Dam kata.rvt` | so từng bước với WPF |
| 6 ✅ | Review + docs + compile R23–R25/R27 (WPF vẫn chạy) | không còn High |

## Rủi ro
- Refactor bộ vẽ chạm tool WPF đã verify live → so ảnh trước/sau trên cùng dầm.
- HPRebar merge mọi DLL (ILRepack); WebView2 compile-only không bị merge; cần warm-up chống deadlock như Toolkit.
- Hai add-in nạp `HPWebHost` → phải ILRepack vào HPRebar.dll (không có DLL rời trùng tên với bản của Toolkit).

## Tiến độ
- Phase 0 ✅ 2026-10-24: [reports/phase-00-cleanup-and-spike.md](reports/phase-00-cleanup-and-spike.md)
- Phase 1 ✅ 2026-10-25: [reports/phase-01-hpwebhost.md](reports/phase-01-hpwebhost.md)
- Phase 2–5 2026-10-25: [reports/phase-02-05-kata-web-window.md](reports/phase-02-05-kata-web-window.md) — phase 3 đổi: không viết backend SVG cho painter, canvas WPF ẩn vẽ rồi xuất SVG (ADR-0010 cập nhật); phase 5 thiếu Xuất Excel (ghi vào workbook của người dùng)
