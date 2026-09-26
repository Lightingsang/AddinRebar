---
phase: 5
title: "UI + ribbon"
status: completed
priority: P2
effort: "1d"
dependencies: [3, 4]
---

# Phase 5: UI modeless + wiring + ribbon

## Overview
Feature folder `KataExport/` đúng convention (4 file gốc, Model/Service/View/ViewModel), window modeless có preview, nút **HPRebar ▸ Rebar ▸ Kata Export**.

## Requirements
- Functional: lấy dầm từ selection hiện có, không có thì `PickObjects` (Esc thoát êm); combo Name/Count (mặc định `STR_ElementName`/`STR_ElementCount` nếu có); radio Normal/Reverse; preview B3:B10 + 5 hàng + warnings; dòng "Workbook đích: …"; nút Chọn lại dầm, Ghi Excel, Đóng; sau khi ghi highlight dầm trong Revit + báo số cột.
- Non-functional: CommunityToolkit.Mvvm (`sealed partial : ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`); code-behind chỉ InitializeComponent + DataContext + `MaterialThemeBridge.Attach` + `CloseRequested += Close`; chỉ `{DynamicResource}` key có sẵn; không set `DialogResult`.

## Architecture
- `KataExportCommand` (mẫu `BeamRebarCommand.cs:28-112`): `static _window` → `Activate()`; đọc selection/pick → Service (P3) → session → VM → View; owner qua `WindowInteropHelper`; `Closed` → dispose handler.
- `KataExportExternalEventHandler` (mẫu `BeamRebarExternalEventHandler.cs:24-78`) implements `IKataExportRunner`: request `Repick` (PickObjects + đọc lại → session mới) và `Highlight` (`SetElementIds`).
- `KataExportRequest`: kind + payload + `TaskCompletionSource` (`RunContinuationsAsynchronously`).
- `KataExportSelectionFilter`: `OST_StructuralFraming` + `FamilyInstance` (theo `BeamRebarSelectionFilter.cs:11-23`).
- `KataExportViewModel`: đổi Name/Count/Normal/Reverse → gọi lại `KataRowBuilder` (pure, không API); `ExportCommand` → `KataExcelWriter` (UI thread) → `runner.RunAsync(Highlight)`.
- Ribbon: `Track(panel.AddPushButton<KataExportCommand>("Kata Export"), icons => icons.KataExport)` trong panel "Rebar" (`Application.cs:56-74`); glyph vector 32×32 toạ độ chẵn trong `RibbonIcons.cs:24-70`.

## Related Code Files
- Create: `HPRebar/HPRebar/KataExport/{KataExportCommand,KataExportExternalEventHandler,KataExportRequest,KataExportSelectionFilter}.cs`
- Create: `HPRebar/HPRebar/KataExport/View/KataExportView.xaml(.cs)`, `ViewModel/{KataExportViewModel,IKataExportRunner}.cs`
- Modify: `HPRebar/HPRebar/Application.cs`, `HPRebar/HPRebar/Resources/Icons/RibbonIcons.cs`

## Implementation Steps
1. Activate `/bs:revit-wpf-mvvm` + `/bs:revit-xaml-styles`.
2. 4 file gốc + runner interface.
3. ViewModel (< 250 dòng; tách preview-row mapping sang Model nếu dài).
4. View XAML theo `FoundationRebarView.xaml:12-24,94-105`.
5. Icon + nút ribbon.
6. Build Debug.R26 + Debug.R24 compile; `dotnet test HPRebar/HPRebar.Core.Tests` (ThemeTokenCoverageTests); chạy theme gallery `HPRebar/tools/theme-gallery/` nếu cần ảnh dark/light của view mới.

## Success Criteria
- [ ] Build R26 + compile R24 pass; ThemeTokenCoverageTests pass
- [ ] `HPRebar/output/icons` preview icon mới rõ ở 16/32 px (`HPRebar/tools/icons/preview-ribbon-icons.ps1`)

## Tiến độ (2026-09-27)
- Bản đầu do một agent khác viết (không phải Claude): 4 file gốc, View, ViewModel, icon, `Application.cs`. Claude đã review và sửa:
  - **H1:** chọn lại dầm lỗi không còn ném exception lên dispatcher (có thể làm crash Revit).
  - Một `KataSheet` duy nhất cho cả preview và export; lỗi của sheet và lỗi của Excel tách riêng.
  - Cờ `IsBusy` dùng chung cho Export và chọn lại dầm.
  - Preview thêm B3–B10 (tách ra `KataPreviewBuilder`).
  - Thiếu tham số `STR_*` → để trống, không lấy đại một tham số khác.
  - Khung cảnh báo hiện được (`HasWarnings`); bỏ màu viết cứng, dùng `Brush.Warning`/`Danger`, `Font.Family.Mono`.
  - Phím Esc đóng cửa sổ; tooltip cho dòng trạng thái.
  - Handler huỷ các request đang chờ khi không còn document.
  - Toạ độ icon về số chẵn.
- ✅ Build R26 + R24 pass; ThemeTokenCoverageTests pass (385/385); preview icon 16/32/64 cả 2 theme đã xem.
- 🟡 Còn lại (mức Low): L1 (id thuộc document khác), L6 (sheet bị khoá), L7 (margin/padding viết số thay token), L8 (cảnh báo từ reader còn tiếng Anh).
- ❌ Chạy trong Revit: CHƯA TEST (P6).

## Risk Assessment
PickObjects trong ExternalEvent handler khi window modeless đang mở → nếu Revit từ chối, fallback: đóng window và chạy lại command. Theme key thiếu → test theme bắt ngay.
