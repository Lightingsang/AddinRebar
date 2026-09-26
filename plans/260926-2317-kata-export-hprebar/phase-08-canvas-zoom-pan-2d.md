---
phase: 8
title: "Canvas mặt đứng: zoom/pan 2 chiều kiểu CAD"
status: completed
priority: P2
effort: "0.5d"
dependencies: [7]
---

# Phase 8: Zoom/pan 2 chiều kiểu CAD

## Overview
Contract chốt `/grill-me` 2026-09-27, phương án A: viewport Core 2 chiều, chữ/nét cỡ cố định, phóng đứng tắt dần về 1:1.

## Requirements
- Lăn chuột: phóng đều ngang + đứng quanh con trỏ (điểm dưới con trỏ đứng yên); giới hạn 0.5× toàn dải .. 3 px/mm.
- Chuột giữa kéo = pan mọi hướng; Shift + kéo trái = pan (touchpad); bấm trái không kéo = chọn cột; nhấp đúp giữa/trái = toàn dải; kéo trái không Shift = không làm gì.
- Tỉ lệ đứng = max(tỉ lệ ngang, 60 px / h dầm lớn nhất); bỏ trần 150 px.
- Ghi chú đi theo hình. Trước/Tiếp/Toàn dải: khung ngang như cũ + căn giữa dải dầm theo chiều đứng. Chọn ở bảng: chỉ pan ngang.
- Ngoài phạm vi: zoom khung, thanh cuộn, phím tắt, nhớ zoom.

## Related Code Files
- Modify: `HPRebar.Core/KataExport/Calculators/KataElevationViewport.cs` (+ tests), `KataExport/View/Controls/KataElevationScene.cs`, `KataElevationCanvas.cs`, `KataElevationPainter.cs`, `KataElevationAnnotations.cs`, `KataExportView.xaml` (dòng hướng dẫn).
- Create: `KataExport/View/Controls/KataElevationCanvas.Input.cs` (chuột, partial).

## Success Criteria
- [x] Test Core viewport 2D (zoom giữ điểm neo cả 2 trục, căn giữa dải, pan 2 chiều, tỉ lệ đứng về 1:1, co vừa chiều cao khi đóng khung) — 448/448.
- [x] Build R26 + R24; gallery 22/22; review 8/10 → M1/M2/L1–L4/L6 fixed ([report](reports/code-review-phase-08-zoom-pan-2d.md)).
- [x] Live (Revit 2026, GMX3, deploy 03:26, 2026-09-27): lăn 8 nấc tại gối K → phóng 2 chiều, K giữ nguyên x; lăn thêm 10 nấc → dầm 500 mm ≈ 330 px (1:1), điểm neo đứng yên; chuột giữa kéo (−150, −120) và Shift+kéo trái (+250, +80) pan đúng hướng; kéo trái không Shift = không đổi gì; nhấp đúp chuột giữa = toàn dải (căn giữa đứng); Tiếp › = nhịp 1 kèm 2 gối, đủ chuỗi hàng 11 + nhãn nhịp; bấm trái vào gối E → chọn E, bảng đồng bộ, hình không nhảy. Ảnh: scratchpad `p8/live/`.
- [x] Bonus P6: Excel tắt → cửa sổ báo đỏ "Microsoft Excel hiện không chạy…", nút Xuất Excel bị khoá.

## Risk Assessment
- Người quen kéo trái để pan → dòng hướng dẫn ghi rõ.
