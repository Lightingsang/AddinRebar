# Phase 5 — Tạo thép (module ghi đầu tiên)

**Status:** Planned · **Type:** HITL

- Port `KataRebarOrchestrator`, `KataTransactionRunner`, `KataRebarCreationService`, `KataBarSetCreator`, `KataStirrupSetCreator`, `KataRebarCurveFactory`, `KataRebarHookResolver`, `KataRebarShapeResolver`, `KataRebarSectionFit`, `KataStirrupCoverFit`, `KataHostCoverService`, `KataRebarCleanupService`, `KataRebarNumberAssigner`, `KataRebarStamp`/`Storage` (cùng schema GUID với WPF để hai tool nhận ra thép của nhau), drafting: `KataLongSectionView/Drafter`, `KataSectionViews`, `KataCrossSections`, `KataDrawingDims`, `KataBarEndMarkCreator`, `KataLineStyles`, `KataSheetComposer`, `KataDraftingStorage`. Phụ thuộc chéo `BeamRebar` (`PointMapper`, `RebarCurveFactory`) port tối thiểu.
- Luật ghi (skill): preview đã có ở phase 4 → `kata.generate {planId}` chạy **một** `RunAsync`: `TransactionGroup "HP: Kata Rebar – <B3>"` + các Transaction con → `Assimilate` (1 Undo); timeout dài (đo thực tế), trang xác nhận trước. Warnings Revit gom vào kết quả (`IFailuresPreprocessor` như `KataTransactionRunner`).
- Trả `KataGenerationResult` (số set, thanh, fallback, view/sheet tạo, cảnh báo).
- Cổng: live trên bản sao model + workbook nháp: thép/số hiệu/view/sheet so với tool WPF chạy trên bản sao thứ hai cùng input (đếm theo storage, Rebar Number, tên view/sheet); Undo một lần xoá hết; chạy lại thay lượt cũ. Đo thời gian → cập nhật mục ghi trong skill (hết "giả định chưa xác minh").
