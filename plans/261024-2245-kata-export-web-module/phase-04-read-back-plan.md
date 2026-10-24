# Phase 4 — Đọc lại Excel + kế hoạch thép

**Status:** Planned · **Type:** AFK

- Port `KataDamComReader`, `KataBeamMatcher`, `KataBeamPlacement`, `KataRebarTypeResolver` (đọc), `KataSettingsStore` (đọc) → `Services/Kata/`.
- Engine: `KataDamSheetParser`, `KataBarNotationParser`, numbering/anchorage/split… (HPRebar.Core) tạo kế hoạch; port phần lập kế hoạch đang nằm trong `KataRebarWorkflow` (add-in) mà không chạm Revit vào **Domain toolkit** hoặc gọi thẳng Core.
- Lệnh `kata.readSheet` → `KataRebarPlan` (nhịp, nhóm thép, đai, mặt cắt, cảnh báo khớp dầm, ánh xạ bar type có thể đổi); `kata.planRemoval {groups}` bỏ nhóm thép như `KataExportViewModel.Removal`.
- Trang: mặt đứng có thép (phase 2 + lớp thép), bảng ánh xạ đường kính → bar type, danh sách cảnh báo.
- Cổng: test so kế hoạch với fixture `HPRebar.Core.Tests/KataRebar/Fixtures` (b02-dwg.json…), live: cùng workbook nháp + bản sao model → kế hoạch trùng tool WPF (số nhóm, số thanh).
