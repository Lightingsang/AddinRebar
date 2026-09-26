---
phase: 1
title: "Spec + golden data"
status: completed
priority: P1
effort: "0.5d + thao tác người dùng"
dependencies: []
---

# Phase 1: Spec + golden data

## Overview
Chốt hợp đồng ô Excel của Kata và thu golden output từ tool Dynamo cũ để P2/P6 so khớp. Không đổi code.

## Requirements
- Functional: spec `reports/kata-cell-contract.md` (bảng ô → quy tắc gối/nhịp, đơn vị, dấu, console); fixture golden cho 2–3 dải dầm.
- Non-functional: fixture là dữ liệu thuần (giá trị ô), không nhúng đường dẫn máy người dùng.

## Architecture
Hợp đồng khởi điểm (từ report phân tích mục 4; ✦ = sửa so với Dynamo):

| Ô / hàng | Gối | Nhịp |
|---|---|---|
| B3/B4 | tham số Name/Count (mặc định `STR_ElementName`/`STR_ElementCount`) của dầm đầu dải ✦ lấy từ dầm được chọn | |
| B5–B10 | h, b, `0`, `""`, −b/2, cao độ Reference Level (m) ✦ UnitUtils | |
| 11 | cột/móng: bề rộng dọc trục; dầm: `"bxh"` ✦ đúng gối | chiều dài |
| 19 | `0` hoặc `"rộngCộtTrên;lệch"` (×±1) ✦ tìm theo cao độ | `z Offset Value` |
| 21 | lệch trục (từ hàng 23) hoặc `""` | −(h_i − h_1) + zOffset_i |
| 22 | tên trục cắt qua gối ✦ lưới trong view | `""` |
| 23 | s_trục − s_tâm gối (Reverse đổi dấu) | `""` |
| console | chèn cột đầu/cuối: `0` (11, 19), `""` (21–23) | |

## Related Code Files
- Create: `plans/260926-2317-kata-export-hprebar/reports/kata-cell-contract.md`
- Create: `plans/260926-2317-kata-export-hprebar/reports/golden/<case>.json` (sau đó chép vào `HPRebar/HPRebar.Core.Tests/KataExport/` ở P2)

## Implementation Steps
1. Soạn danh sách case cần chạy: (a) dải có console đầu và cuối, (b) có gối là dầm giao, (c) có cột tầng trên lệch tâm, (d) lưới lệch khỏi tâm gối, (e) một phần tử dầm vắt qua ≥ 2 gối.
2. 👤 Người dùng: gửi Kata `.xlsm` mẫu; chạy nút pyRevit cũ trên từng case (Normal + 1 case Reverse), lưu bản sao workbook sau mỗi lần.
3. Đọc bằng `hprebar-excel` `read_range` (B3:B10, C11:BZ23) → JSON; ghi kèm ElementId/UniqueId các dầm đã chọn và tên model.
4. Chốt 5 điểm mở: hàng 12–18/20 có nội dung Kata không (quyết định vùng xoá); "gối 0" ở điểm nối có cần không; công thức hàng 21 (có trừ zOffset_1?); header B3–B10 khi Reverse lấy dầm nào; ý nghĩa B7/B8.
5. Case (e) và các chỗ Dynamo sai: ghi giá trị Dynamo **và** giá trị đúng mong đợi.

## Success Criteria
- [ ] `kata-cell-contract.md` không còn `[chưa xác minh]` ở 5 điểm mở (hoặc ghi rõ lý do giữ)
- [ ] ≥ 2 fixture golden đọc được, kèm tham chiếu model/ElementId

## Kết quả (2026-09-26)
- ✅ Hợp đồng chốt bằng file Kata mẫu `C:\kata_pro\Kata.xlsm` (nhãn A, hàng 10 Cột/Nhịp xen kẽ, comment ô): [kata-cell-contract.md](reports/kata-cell-contract.md). 4/5 điểm mở đóng; điểm 5 (B7–B9) người dùng chọn tính từ model.
- 🟡 Golden từ tool cũ chuyển sang P6 (tuỳ chọn) — chỉ còn để kiểm số liệu hình học.

## Risk Assessment
Người dùng không có file Kata hoặc không chạy được tool cũ → tiếp tục với hợp đồng từ report phân tích, đánh dấu P6 "chưa Verified với Kata thật".
