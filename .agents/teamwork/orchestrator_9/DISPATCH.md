# Dispatch Log — orchestrator_9

## 2026-10-09T14:58:36Z
You are the Project Orchestrator (orchestrator_9).

## Working Environment
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_9\
- Target codebase: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim
- Authoritative User Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under header `## 2026-10-09T14:54:56Z`)

## Mission: HPBim Wave 3 Remediation
Implement Wave 3 Remediation for HPBim resolving P0/P1 blocking defects:

### R1. Cross-Process Concurrency & File Lock cho IntentJournal (P1)
- Xử lý xung đột ghi đồng thời (multi-writer concurrency) giữa nhiều instance/process của IntentJournal. Thay thế semaphore đơn lẻ theo instance bằng cơ chế khóa liên tiến trình (cross-process file lock / mutex) khi thực hiện append, update và compaction.
- Bảo đảm an toàn tuyệt đối cho chu trình read/modify/write: kiểm thử bằng kịch bản chạy 2 instance ghi đồng thời 1,000 unique intents (mỗi instance 500 GUID) và cold-read bằng instance mới phải nạp đủ 1,000/1,000 intents nguyên vẹn, không thất thoát bản ghi nào.

### R2. Reconcile Persist-Before-Terminal & Exact Keyset Coverage (P1)
- Nâng cấp ReconcileService.ApplyCommittedReceiptAsync: lưu BimCoordinationLedger xuống đĩa nguyên tử (.tmp -> replace) TRƯỚC KHI cập nhật trạng thái IntentJournal sang Committed.
- Đối soát tính đầy đủ của keyset so với intent gốc (không chỉ đối soát số lượng ItemCount mà còn kiểm tra toàn bộ danh sách SourceKey). Nếu mapping bị thiếu, hỏng hoặc lỗi lưu đĩa, bảo toàn trạng thái OutcomeUnknown để cho phép reconcile chạy lại an toàn.

### R3. Tách biệt Persistent Document Identity khỏi Dynamic DocumentVersion GUID (P1)
- Tách bạch rõ ràng định danh tĩnh của tài liệu Revit (Title, PathName, CentralGuid/Project GUID) khỏi chỉ số revision động (VersionGuid/NumberOfSaves vốn thay đổi sau khi tạo element).
- MatchTargetDocSnapshot: bắt buộc đối soát các trường định danh tĩnh cần thiết (không fail-open khi thiếu path/GUID nếu intent đã có).
- Ngăn chặn việc từ chối nhầm receipt: việc VersionGuid thay đổi do chính operation tạo element gây ra không được làm thất bại quá trình recovery của chính operation đó.

### R4. Tính Trung thực của NoOp, Skip và Rollback Semantics (P0/P1)
- ValidateReturnedRecord: Bắt buộc cấu kiện có trạng thái NoOp phải có TargetUniqueId hợp lệ và khớp với mapping trong ledger (từ chối NoOp thiếu UniqueId).
- Chuẩn hóa lý do Skipped bằng enum/mã lỗi cụ thể và policy định nghĩa trước trong plan; loại bỏ hoàn toàn việc so khớp chuỗi con lỏng lẻo (Contains).
- Khi phát hiện integrity defect sau khi dispatch: nếu host chưa xác nhận rollback, pipeline phải báo trạng thái OutcomeUnknown chứ không tự ghi "Transaction rolled back" khi chưa có lệnh undo thực sự trên host.
- Khắc phục CompositeSourceKeyJsonConverter: bảo toàn toàn bộ mảng ProvenanceHandles qua chu trình serialize và deserialize JSON.

### R5. Chuẩn hóa Đo đạc Hình học & Nghiệm thu (P0/P1)
- Mở rộng kiểm tra hình học: Bổ sung kiểm tra cao độ cắt đỉnh cọc (Pile cutoff elevation); kiểm tra outline và thickness cho Profile/Slab; loại bỏ việc tự động Verified trên Generic category khi thiếu phép đo thực tế.
- Truy vấn và đối soát hình học chính xác theo mapped TargetUniqueId của từng cấu kiện.

## Acceptance Criteria
- [ ] Multi-writer Journal Concurrency Test: Test 2 instance ghi đồng thời 1,000 unique intents; cold restart nạp lại đủ 1,000/1,000 intents không mất mát.
- [ ] Reconcile Persist-Before-Terminal Test: Test mô phỏng crash hoặc lỗi ghi ledger trong reconcile; verify journal không bị đánh dấu Committed khi ledger chưa lưu thành công.
- [ ] Dynamic DocumentVersion Resilience Test: Test kiểm chứng việc VersionGuid thay đổi sau khi apply không khiến recovery từ chối receipt hợp lệ của chính operation đó.
- [ ] Strict NoOp UniqueId & Exact Skip Test: Test kiểm chứng NoOp thiếu UniqueId bị từ chối; Skipped sai lý do bị chặn; defect sau dispatch báo OutcomeUnknown.
- [ ] ProvenanceHandles Preservation Test: Test chứng minh ProvenanceHandles được bảo toàn 100% roundtrip qua JSON serialization.
- [ ] Expanded Measured Geometric Test: Test kiểm chứng Pile thiếu cutoff elevation trả về Inconclusive.
- [ ] Zero Regressions: 100% tests toàn bộ repository PASS (3,779+ tests).
- [ ] Repository & Governance Rules: Tuân thủ triệt để Rule 0 (Zero guesswork) và Clean Code: file <= 300 dòng (Rule C2), method <= 50 dòng (Rule M3), không code comment (Q-B1), không empty catch (Q-B2). HPBim.Core giữ thuần netstandard2.0, không reference Autodesk.Revit.*. Không tự ý commit/push git hoặc deploy add-in ra môi trường thực.


## 2026-10-09T18:16:16Z
The rate limit quota has reset. Please resume execution. Worker M4 has delivered handoff.md in .agents/teamwork/teamwork_preview_worker_w3_m4_1/ with 1,053/1,053 tests passing. Please proceed with Gate M4 and Milestone M5.
