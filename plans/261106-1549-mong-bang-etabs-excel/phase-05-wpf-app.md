---
phase: 5
title: "App WPF"
status: pending
priority: P2
effort: ""
dependencies: [3, 4]
type: HITL
---

# Phase 5: App WPF

## Overview
Một cửa sổ cho toàn bộ quy trình. Kỹ sư thấy dữ liệu đầu vào, kết quả, chênh lệch và bấm duyệt.

## Requirements
- MaterialDesignThemes 5.3.2, `CommunityToolkit.Mvvm`, Segoe UI, `{DynamicResource}`.
- 5 bước:
  1. Chọn file, sheet, xem nguồn và hash.
  2. Xem `FoundationInput`, cảnh báo, F34 và D39 cũ / mới.
  3. Chạy ETABS, tiến độ, huỷ giữa các bước.
  4. Bảng số cũ / số tool, đề xuất thép, cảnh báo.
  5. Duyệt, ghi bản sao, xuất file ra thư mục kỹ sư chọn.
- Xuất không ghi đè file nào có sẵn. Tên `<gốc>-tool-<ts>.xls`.
- Serilog vào `%LocalAppData%\HPFoundation\logs\`.
- Lỗi hiện rõ bước nào, log ở đâu, thư mục chạy còn giữ.
- Toàn bộ COM chạy trên một worker STA, UI không bị khoá.

## Architecture
```text
MainViewModel → PipelineRunner (App) → ExcelSession / EtabsSession trên STA worker
```
Code-behind chỉ `InitializeComponent` + `DataContext`.

## Related Code Files
- Create: `HPFoundation/HPFoundation.App/View/**`, `ViewModel/**`, `Service/PipelineRunner.cs`
- Tham khảo: `HPEtabs/HPEtabs.McpBridge/Resources/Themes/**`

## Implementation Steps
1. Trích `PipelineRunner` từ các phase trước.
2. Theme, ViewModel, 5 bước.
3. Huỷ, lỗi giữa chừng, dọn tiến trình.
4. Chạy tay trên MB1.

## Success Criteria
- [ ] Trọn quy trình MB1 qua giao diện.
- [ ] Huỷ và lỗi ở mỗi bước không để lại `EXCEL.EXE`, `ETABS.exe` của tool.
- [ ] Xuất từ chối ghi đè.
- [ ] Giao diện đúng theme sáng và tối.
- [ ] 👤 Kỹ sư dùng thử và nhận xét.

## Risk Assessment
- COM treo làm đứng cửa sổ: worker riêng, timeout theo bước.
- Người dùng đóng app giữa lúc chạy: xác nhận, rồi dọn tiến trình.
