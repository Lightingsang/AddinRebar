---
phase: 6
title: "Mốc chuẩn, 8 sheet 4 cột, phát hành"
status: pending
priority: P2
effort: ""
dependencies: [5]
type: HITL
---

# Phase 6: Mốc chuẩn, 8 sheet 4 cột, phát hành

## Overview
Lập mốc đối chiếu được kỹ sư duyệt, chạy cho cả 8 sheet 4 cột, rồi phát hành nội bộ.

## Requirements
- Số Excel hiện tại không làm mốc trực tiếp: tải, lò xo đầu phải, Eb, F34, D39 và quy tắc M đều đã đổi.
- Mốc = chạy MB1 bằng tool trên bản `.xls` được xác nhận là chuẩn, kỹ sư duyệt từng số.
- Lưu mốc trong repo dưới dạng JSON, không chứa file dự án. Test hồi quy so với mốc.
- Dung sai đề xuất, chờ kỹ sư duyệt:
  - Tái tạo model tay: M3, V2 ≤ 0.01.
  - Hồi quy mốc: M, Q ≤ 0.01; Fa ≤ 0.01 cm².
  - Tổng phản lực ≤ 0.01 T.
- Chênh lệch so với sheet cũ phải có lý do trong báo cáo, không được "lệch không giải thích".

## Related Code Files
- Create: `HPFoundation/HPFoundation.Core.Tests/Baselines/**`
- Create: `HPFoundation/README.md`, `docs/hp-foundation-user-guide.md`
- Modify: `CLAUDE.md` mục Repository Layout, `AGENTS.md` chèn tay cùng nội dung, `docs/codebase-summary.md`, `docs/project-changelog.md`

## Implementation Steps
1. Chốt bản `.xls` chuẩn và `D136`.
2. Chạy MB1, kỹ sư duyệt, lưu mốc.
3. Chạy 7 sheet 4 cột còn lại trên bản sao, mỗi sheet một báo cáo.
4. Sửa profile nếu sheet nào khác bố cục.
5. `dotnet publish`, hướng dẫn cài.
6. Review theo `docs/clean-code/CODE_REVIEW_CHECKLIST.md`, TOOL_DEVELOPMENT_WORKFLOW bước 12-16.

## Success Criteria
- [ ] Mốc MB1 có xác nhận của kỹ sư.
- [ ] 8 sheet 4 cột chạy hết, hoặc sheet không chạy được có lý do.
- [ ] Mọi chênh lệch có giải thích.
- [ ] Test hồi quy xanh, build sạch.
- [ ] 👤 Kỹ sư chấp nhận dung sai và kết quả.

## Risk Assessment
- MB2, MB3 đang được làm dở: dùng bản đã khoá, ghi hash trong báo cáo.
- Dùng tool cho hồ sơ phát hành trước khi đủ mốc: ghi rõ "kỹ sư chịu trách nhiệm kiểm tra" trong báo cáo xuất.
