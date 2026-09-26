---
phase: 7
title: "Mặt đứng dải dầm trong cửa sổ KataExport"
status: completed
priority: P2
effort: "1.5d"
dependencies: [5]
---

# Phase 7: Mặt đứng dải dầm (elevation view)

## Overview
View vẽ mặt đứng dải dầm trên bảng preview, số liệu = đúng các ô sẽ ghi Kata. Chỉ hình học (không thép). Contract chốt qua `/grill-me` 2026-09-27, phương án A (OnRender + hình học ở Core).

## Requirements
- Vẽ: đường bao dầm theo từng phần tử (bậc h / z offset), gối theo loại (cột/vách = cổ cột dưới; móng = khối rộng dưới; dầm giao = mặt cắt b×h), cột trên lệch tâm, trục lưới + bubble, joint (gối 0), console đầu tự do.
- Ghi chú: chuỗi gối/nhịp = hàng 11 · chuỗi trục–trục · "Nhịp n – b×h" + z / đáy khi ≠0 · "rộng;lệch" cột trên (hàng 19) · lệch trục (23) + dầm giao (21) khi ≠0 · chữ cột Excel C.. dưới mỗi đoạn.
- Đồng bộ 2 chiều view ↔ bảng (cột chọn); ‹ Trước / Tiếp › theo nhịp + zoom vào nhịp; "Toàn dải"; lăn chuột zoom quanh con trỏ; kéo = pan.
- Tỉ lệ ngang đúng; đứng phóng để dầm ≥ ~60 px, bậc giữ tỉ lệ nhau. Reverse lật hình. Dark/Light đổi sống.
- Không lỗi khi không có lưới / 1 nhịp / 75 cột; chữ không đè nhau (bỏ chữ không vừa, zoom vào mới hiện).
- Ngoài phạm vi: thép, chọn phần tử Revit từ view, xuất ảnh, mặt cắt ngang.

## Architecture
- Core `HPRebar.Core/KataExport/`: `Models/KataElevationModels.cs` (cột, dầm, gối, lưới, chuỗi kích thước — mm theo toạ độ vẽ, trái→phải = thứ tự cột sheet), `Calculators/KataElevationBuilder.cs` (input + options + sheet → model; chữ lấy từ chính ô sheet), `Calculators/KataElevationViewport.cs` (fit / fit khoảng / zoom quanh neo / pan / tỉ lệ đứng / hit test), `Calculators/KataColumnLetters.cs`.
- Add-in `KataExport/View/Controls/`: `KataElevationCanvas` (FrameworkElement, DP Elevation/SelectedColumnIndex/FocusRequest/SurfaceBrush, chuột), `KataElevationPainter` + `KataElevationAnnotations` (vẽ), `KataCanvasPalette`, `KataDrawPrimitives`, `KataLabelLane` (chống đè chữ), `ListBoxScrollSelection` (attached behavior cuộn bảng tới cột chọn).
- ViewModel: `Elevation`, `PreviewColumns`, `HeaderLine`, `SelectedColumnIndex`, `FocusRequest`, lệnh `PreviousSpan/NextSpan/FitAll`.
- Theme: token có sẵn (`Brush.Canvas.*`, `Brush.Accent`, `Brush.Warning`); đổi theme → DynamicResource vào DP `SurfaceBrush` → xoá palette cache.

## Related Code Files
- Create: 4 file Core + test `HPRebar.Core.Tests/KataExport/KataElevationTests.cs`; 7 file `KataExport/View/Controls/`.
- Modify: `KataExportView.xaml`, `KataExportViewModel.cs`, `KataPreviewBuilder.cs`; `tools/theme-gallery/Program.cs` (cảnh mẫu Kata dark/light).

## Implementation Steps
1. Core model + builder + viewport + letters; xUnit (khớp hàng 11, Reverse lật, console, joint, dầm giao, cột trên, lưới ngoài gối, fit/zoom/hit test, 75 cột).
2. Canvas + painter + palette; bảng cột ngang có chọn.
3. VM + XAML; build R26 + R24.
4. Theme gallery: cảnh mẫu, ảnh dark/light → tự soát.
5. code-reviewer; sửa; deploy + chạy thật GMX3 / T1-DX12 (cần đóng Revit).

## Success Criteria
- [x] Tests Core pass (444); build R26 + R24 pass; ThemeTokenCoverage pass.
- [x] Ảnh gallery dark/light: hình rõ, chữ không đè (22/22; review 8/10 → M1–M4 + L1–L4/L6/L7/L9 fixed, [report](reports/code-review-phase-07-elevation-view.md)).
- [x] Live (Revit 2026, GMX3, 2026-09-27): cửa sổ 1040×920 mở từ ribbon; số trên view = bảng = ô Excel đọc lại (giống hệt lần 2); bấm hình → bảng chọn F; Tiếp › → nhịp 3 zoom đúng G–H–I; lăn chuột/kéo pan; bấm bảng L → view pan tới L; Toàn dải; Reverse lật hình + chọn L→J. Ảnh: scratchpad `p7/live/`.
- [x] Sửa sau live: bubble trục ghi sang Kata ưu tiên hiện (Reverse: 33A che 31A) — deploy 02:59, live T1-DX12 Reverse: đủ 6 bubble trục ghi (12.1a…3.1a). T1-DX12 normal: số trên view = ô Excel = lần 1.

## Risk Assessment
- Chữ dày khi toàn dải → lane chống đè + bỏ ghi chú phụ khi không vừa.
- Canvas nhận chuột trong Revit modeless window — kiểm live.
