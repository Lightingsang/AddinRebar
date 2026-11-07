---
phase: 1
title: "Đọc Excel trên bản sao"
status: completed
priority: P1
effort: ""
dependencies: []
type: AFK
---

# Phase 1: Đọc Excel trên bản sao

## Overview
Từ `.xls` + tên sheet, tạo bản sao, áp F34 và D39 theo quy ước ETABS, rồi xuất `FoundationInput` kiểm chứng được.

## Requirements
- Chép file gốc sang thư mục chạy, ghi SHA-256 và thời gian sửa. H: là ổ mạng dùng chung, file có thể đổi giữa chừng.
- Excel riêng, ẩn, không macro, `DisplayAlerts = false`. Luôn đóng ở `finally`.
- F34 mới: `ROUND(SUM(F17:F20)+ABS(C17*F29+C18*G29-C19*H29-C20*I29)+SUM(E17:E20)*D24,2)`.
- D39 mới: `ROUND(SUM(D17:D20)+SUM(G17:G20)*D24,2)`.
- Giữ số cũ để in đối chiếu. MB1: 42.85 → 50.55, 18.44 → 10.74.
- Tìm ô qua `SheetProfile`: nhãn neo + độ lệch, ví dụ `BẢNG TÍNH CỐT THÉP DẦM BĂNG` cho khối thép. Thiếu neo là dừng và báo lỗi, không đoán.
- Kiểm tra: chữ `!!!`, ô lỗi, b, bd, h, chiều dài âm.

## Architecture
```text
Core:  SheetProfile, FoundationInput, InputValidator
App:   WorkbookCopy, ExcelSession (late-bound), SheetReader
```
Dữ liệu: `FoundationInput { nhãn trục, x tim cột, tải 5 thành phần, b, bd, h, h1, ki giữa, ki đầu, ai, cấp bền, nhóm thép, nguồn }`, đơn vị m, T, T.m.

Hệ quả cần có test: F34, D39 đổi kéo theo D55, D57, D61, D64, bảng lún và K. Vì vậy phải đọc F128, F129 **sau** khi tính lại.

## Related Code Files
- Create: `HPFoundation/HPFoundation.slnx`, `global.json`, `Directory.Build.props`
- Create: `HPFoundation/HPFoundation.Core/**`, `HPFoundation/HPFoundation.Core.Tests/**`, `HPFoundation/HPFoundation.App/**`
- Create: `docs/architecture/adr/0015-hpfoundation-standalone-tool.md`

## Implementation Steps
1. Viết ADR-0015, dựng solution.
2. Test đỏ: `SheetProfile` tìm neo trên fixture JSON của `MB1-4COT`.
3. `FoundationInput`, `InputValidator`.
4. `WorkbookCopy` + băm.
5. `ExcelSession`, `SheetReader`, áp F34 và D39.
6. Chạy thử trên 8 sheet 4 cột, in bảng neo tìm thấy.

## Success Criteria
- [x] `FoundationInput` của MB1 khớp báo cáo GĐ1; F34 50.55, D39 10.74 đúng số dự tính.
- [x] Neo tìm đủ trên 8 sheet 4 cột, kể cả `MB10- 4COT` (F32, D37).
- [x] File gốc giữ nguyên hash sau khi chạy.
- [x] Không còn `EXCEL.EXE` mồ côi sau chạy thường và sau lỗi sai tên sheet.
- [x] Unit test pass. DLL mới build bị khoá khoảng 20 s rồi nhả; lúc đầu từng bị xoá, không rõ nguyên nhân.
- [ ] Lỗi cấp bền lạ, file có mật khẩu, kill app giữa chừng: code đã có, CHƯA TEST.

## Risk Assessment
- Bố cục sheet khác nhau: neo + test, MB7 đến cột V có thể có khối phụ.
- Excel cảnh báo khi mở `.xls` trên bản sao: `DisplayAlerts`, `UpdateLinks = 0`.
- Excel ẩn bị treo: timeout, chỉ kill đúng PID tool tạo.

## Kết quả chạy thật 2026-11-06 (bản `.xls` 17:00)

| Sheet | F34 hoặc F32 (T.m) | D39 hoặc D37 (T.m) | k giữa / đầu (T/m) | Trạng thái |
|---|---|---|---|---|
| MB1-4COT | 42.85 → 50.55 | 18.44 → 10.74 | 156.264 / 78.132 | Sẵn sàng |
| MB2-4COT | 45.19 → 41.44 | 13.85 → 17.60 | 176.938 / 88.469 | Sẵn sàng |
| MB5-4COT | 52.05 → 33.35 | 18.30 → 37.00 | 169.136 / 84.568 | Sẵn sàng |
| MB7-4COT | 12.60 → 24.25 | 2.60 → 9.05 | 167.312 / 83.656 | Sẵn sàng |
| MB9- 4COT | 4.26 → 5.24 | −9.44 → 0.06 | 172.276 / 86.138 | Sẵn sàng, phase 2 thêm nút trục 20 |
| MB10- 4COT | 27.12 → 26.57 | 16.97 → 17.52 | 160.203 / 80.102 | Sẵn sàng |
| MB11-4COT | 29.46 → 27.06 | 10.20 → 12.60 | 153.144 / 76.572 | Sẵn sàng |
| MB13- 4COT | 42.80 → 38.80 | 3.72 → 7.72 | 167.514 / 83.757 | Sẵn sàng |

Quy ước F34, D39 mới làm k của MB1 đổi từ 159.72 thành 156.26 T/m, vì mô men vào lệch tâm, ứng suất, lún rồi K.

## Việc để lại

- Phase 5: COM message filter (CO3).
- Không dùng `IgnoreRemoteRequests`: giá trị có thể lưu vào tùy chọn Excel của người dùng, làm hỏng mở file bằng double-click. `GIẢ ĐỊNH CHƯA XÁC MINH`.
- Test `SheetPreparation` cần Excel thật, sẽ là harness live.
