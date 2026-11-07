---
phase: 3
title: "Nội lực về Excel"
status: pending
priority: P1
effort: ""
dependencies: [2]
type: AFK
---

# Phase 3: Nội lực về Excel

## Overview
Áp quy tắc đã chốt lên `AnalysisResult`, ghi vào bản sao, để Excel tính thép, và xuất báo cáo đối chiếu.

## Requirements
- Gối: `max(|M3 trái|, |M3 phải|)` tại tim cột. Ghi cả hai hàng của gối.
- Nhịp: `max |M3|` giữa hai cột. Vị trí cực trị vào báo cáo.
- Q: A172 = |V2| lớn nhất, A173 = lớn nhì tại **cột khác**, làm tròn lên số nguyên.
- Mô men ghi 2 chữ số thập phân. Giả định, xác nhận tại phase 6.
- Chỉ ghi ô đã khai báo trong profile. Ghi lại ô cũ → ô mới → nguồn.
- Sau khi ghi, đọc lại, phải đúng số đã ghi. Không có `#`, `!!!`.
- Báo cáo: bảng số cũ / số tool / chênh / lý do; run.json có hash, tham số, kết quả.
- Băm lại file gốc cuối phiên; khác hash là cảnh báo "file đã đổi trong lúc chạy".

## Architecture
```text
Core:  ForcePicker (thuần), ReportBuilder
App:   SheetWriter, ReportWriter
```

## Related Code Files
- Create: `HPFoundation/HPFoundation.Core/Forces/**`, `HPFoundation/HPFoundation.Core/Reporting/**`
- Create: `HPFoundation/HPFoundation.App/Excel/SheetWriter.cs`
- Test: fixture lấy từ kết quả `MB1` đã đọc ở GĐ2

## Implementation Steps
1. Test đỏ `ForcePicker` với bảng 234 station của MB1. Kỳ vọng: gối A 50.53, B 15.34, C 5.95, D 2.13; nhịp 29.16, 46.49, 21.90; Q 75, 63.
2. `ForcePicker`.
3. `SheetWriter` + đọc lại.
4. `ReportBuilder`.
5. Chạy trọn: đọc → ETABS → ghi bản sao.

## Success Criteria
- [ ] Test `ForcePicker` đỏ rồi xanh với số MB1.
- [ ] Ghi đúng 11 + 2 ô, không ô nào khác đổi (so toàn sheet trước và sau).
- [ ] Mọi công thức trên sheet giữ nguyên, trừ F34, D39 đã khai báo.
- [ ] File gốc giữ nguyên hash.
- [ ] Ảnh `N145` được xem xét. Nếu có dữ liệu cần dùng, cập nhật phase này trước khi đóng.

## Risk Assessment
- Mô men gối D đổi dấu giữa hai bên: quy tắc dùng trị tuyệt đối, có test.
- Hai Q lớn nhất cùng một cột: quy tắc "cột khác", có test.
- Thớ thép không có cột trong sheet: chỉ thể hiện ở báo cáo.
