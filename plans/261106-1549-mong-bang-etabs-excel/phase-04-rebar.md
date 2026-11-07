---
phase: 4
title: "Đề xuất và kiểm tra thép"
status: pending
priority: P2
effort: ""
dependencies: [3]
type: HITL
---

# Phase 4: Đề xuất và kiểm tra thép

## Overview
Từ Fa của Excel, đề xuất bố trí và chạy cảnh báo. Kỹ sư duyệt rồi mới ghi. Tool không tự ghi.

## Requirements
- Đề xuất cho hàng 150-160: thép chạy suốt + gia cường. Gối là thép dưới, nhịp là thép trên.
- Cảnh báo, không đổi kết quả Excel:
  - αm ≤ αR
  - hàm lượng μ trong [μmin, μmax]
  - khoảng hở thanh trong bề rộng bd với a đang dùng
- Đề xuất cốt đai theo H172, H173, kỹ sư chốt I175, I176.
- Ghi vào C150:F160 và I175:I176 chỉ sau khi duyệt.

## Câu hỏi phải chốt trước khi code
Không tự chọn. Hỏi kỹ sư, ghi vào ADR hoặc `rules.json`:
1. Danh mục Ø và số thanh tối đa mỗi lớp.
2. Quy tắc chọn thép chạy suốt: lấy Fa nào làm chuẩn.
3. Giá trị αR, μmin, μmax theo tiêu chuẩn đang áp dụng. Chưa đối chiếu TCVN 5574:2018.
4. Khoảng hở tối thiểu, số lớp tối đa. 10Ø20 trong bd = 50 cm có thể cần 2 lớp, khi đó `a` phải khớp.
5. Bước đai làm tròn thế nào.

## Architecture
```text
Core:  RebarRules (đọc rules.json), RebarSuggester, SectionChecks
App:   SheetWriter mở rộng cho ô thép
```

## Related Code Files
- Create: `HPFoundation/HPFoundation.Core/Rebar/**`
- Create: `HPFoundation/HPFoundation.Core/Rules/rules.json`, `materials.json`

## Implementation Steps
1. Phỏng vấn 5 câu trên.
2. Chép bảng `Data` vào `materials.json`, test ghim giá trị theo B15..B60, CB240-T..CB500-V.
3. Test đỏ `SectionChecks` với số MB1.
4. Test đỏ `RebarSuggester`.
5. Cài đặt, nối vào pipeline.

## Success Criteria
- [ ] Cảnh báo chạy đúng với ca đạt và ca cố ý vi phạm.
- [ ] Đề xuất cho MB1 được kỹ sư so với bố trí tay hiện tại.
- [ ] Bản sao không đổi ô thép trước khi duyệt.
- [ ] 👤 Kỹ sư duyệt `rules.json`.

## Risk Assessment
- Lấy sai ngưỡng tiêu chuẩn: giá trị nằm trong `rules.json`, kỹ sư duyệt, không ghi cứng.
- Đề xuất bị xem như kết luận: giao diện ghi rõ "đề xuất, cần duyệt".
