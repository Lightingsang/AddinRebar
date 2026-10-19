## 2026-10-08T15:05:50Z

You are orchestrator_6 (Project Orchestrator) for HPBim Wave 2 Remediation.
Working directory: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_6\
Target project: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim
Original Request: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under header ## 2026-10-08T15:03:54Z)

Mission:
Triển khai hoàn thiện đợt 2 (Wave 2 Remediation) cho HPBim tại G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim giải quyết triệt để 5 lỗi chặn P0/P1 từ báo cáo re-review ngày 08/10/2026:
(1) Strict Record Matching & 1:1 Bijection Validation (P0)
(2) Journal Torn-tail Auto-Recovery (P1)
(3) Recovery Receipt Completeness & Coverage Verification (P1)
(4) CLI Ledger Wiring & Durable Ledger-First Commit Order (P1)
(5) Measured Geometric Engine Strict Inconclusive Threshold (P0/P1)

Please review prior context at G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_5\handoff.md if helpful.

Requirements & Acceptance Criteria:
- R1: ReconcileRecordsStrict1To1 trong BimPipelineEngine.ElementRecords.cs kiểm tra song ánh chính xác 1:1. Reject ngay ngoại lai, trùng lặp, status invalid, Created/Updated thiếu TargetUniqueId, sai Category/Mark. Skipped không lý do là Integrity Defect -> rollback.
- R2: IntentJournal.cs xử lý torn-tail dưới _ioLock, kiểm tra newline cuối file, quarantine/trim hoặc đệm newline nếu thiếu trước khi ghi mới. 100% intent sau crash đọc lại chính xác (khắc phục TORN_APPEND_INTENT_LOST).
- R3: ExtensibleStorageReceiptReader.cs và ReconcileService.cs: ParseElementMap không chuyển JSON hỏng thành map rỗng (phải null/error). ApplyCommittedReceiptAsync đối soát ItemCount & expected keys vs intent gốc; không khớp -> OutcomeUnknown. Đối soát Doc Identity (Title, PathName, VersionGuid/CentralGuid).
- R4: Nối BimCoordinationLedgerService vào GenerateCommand.cs. Tra cứu TargetUniqueId tự động (Create vs Update/NoOp). Thứ tự Ledger-First: lưu BimCoordinationLedger xuống đĩa nguyên tử (.tmp -> atomic replace) TRƯỚC KHI cập nhật IntentJournal sang Committed. Test cold process 2 lần -> 100% NoOp.
- R5: MeasuredGeometricEngine.Structural.cs & ProfileGrid.cs: tập Required Measurements theo Category. Thiếu thuộc tính bắt buộc -> Inconclusive. VerifyCommand coi Inconclusive/Defect là fail (exit code 1).
- Zero Regressions: 100% test suite PASS (HPBim.Core.Tests, HPBim.Client.Tests, HPBim.E2E.Tests, HPRebar.Core.Tests, McpShared) (3,543+ tests).
- Clean Code rules: files <= 300 lines (Rule C2), methods <= 50 lines (Rule M3), 0 commented code, 0 empty catch without reason comment.
- HPBim.Core giữ thuần netstandard2.0 không reference Autodesk.Revit.*.

Maintain progress.md, BRIEFING.md, plan.md in your working directory.
Decompose into subagents per Teamwork protocol. When all milestones are verified and passing, deliver your completion report to Sentinel.
