# Dispatch: orchestrator_5 (Project Orchestrator)

## Mission
Triển khai hoàn thiện quy trình BIM Pipeline và Coordination Ledger cho HPBim (P0 và P1) giải quyết triệt để 10 lỗi chặn từ review ngày 08/10/2026, bảo đảm kết quả trung thực tuyệt đối, preview/apply tương đương, phục hồi an toàn sau sự cố và loại bỏ mọi hình thức shadow testing.

## Identity & Hierarchy
- Your identity: `orchestrator_5` (teamwork_preview_orchestrator).
- Your working directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_5\`.
- Your parent is: `a09df7a8-6df3-43d7-ac7b-40b8c01e2243` (Sentinel. Use this conversation ID for all status reporting and communication via `send_message`).
- Target Product Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\`

## Reference Documents & Foundations
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (see timestamp `## 2026-10-08T11:29:50Z`)
- Clean Code Governance: `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/clean-code/host-appendix/revit.md`, `AGENTS.md`
- Previous HPBim Artifacts:
  - `HPBim/` codebase (`HPBim.Core`, `HPBim.Client`, `HPBim.E2E.Tests`, `HPBim.Tests`, etc.)
  - `docs/architecture/HPBIM_ADVANCED_PIPELINE_DESIGN.md`
  - `TEST_INFRA.md`, `TEST_READY.md`

## Requirements to Fulfill

### R1. Phase 1 — P0 Core Wire, Cardinality & Strict Batch Transaction
- **Canonical String SourceKey**: Chuẩn hóa `SourceKey` thành chuỗi canonical `[DocIdentifier]#[Category]:[PrimaryHandle]` trên toàn bộ `ElementPlanItem`, wire DTO, `ScriptArgs`, Roslyn runner và `HPBimCoordinationReceipt`. Bảo đảm 100% roundtrip equality qua JSON serialization/deserialization.
- **Cardinality 1:1 & Không Synthesize Created**: Bắt buộc đối soát chính xác 1:1 giữa danh sách items trong plan và execution records trả về từ Revit. Cấm tuyệt đối việc tự tạo kết quả `Created` khi response thiếu records hoặc khi response bị cắt cụt do vượt ngưỡng 64KB (`execRes.Truncated == true`). Bất kỳ sự thiếu hụt nào phải đánh dấu ngay `OutcomeUnknown`.
- **Strict All-or-Nothing Batch Transaction**: Khi bất kỳ cấu kiện nào trong batch gặp lỗi (`Failed`), rollback toàn bộ transaction của batch đó trên Revit, không ghi receipt vào Extensible Storage, và báo lỗi toàn diện, bảo đảm không để lại mô hình dở dang trong RVT.
- **Loại bỏ Heuristic Fallbacks trong Script Runner**: Áp dụng đầy đủ `rotationDeg` và `topLevel` constraint cho Cột. Loại bỏ hoàn toàn các lệnh `FirstOrDefault()` khi chọn Pipe/PipingSystem type; nếu thiếu symbol/type phải báo lỗi tường minh `ERR_SYMBOL_NOT_FOUND`.

### R2. Phase 2 — P1 Document Identity, Complete Hash & Journal Hydration
- **Ràng buộc Định danh Tài liệu Đích**: CLI `generate` tự động truy vấn thông tin tài liệu Revit đang hoạt động (Title, Path, VersionGUID) trên Revit API thread trước khi biên dịch plan; gắn chặt vào `ExecutionPlan` và `IntentJournal`. Pre-flight trên Revit runner sẽ từ chối thực thi nếu phát hiện sai tài liệu hoặc snapshot đã lỗi thời.
- **Canonical Plan Hash Toàn diện**: `BimExecutionPlan.ComputePlanHash` phải bao phủ toàn bộ các thuộc tính ảnh hưởng đến hình học và mô hình: tọa độ tất cả đỉnh polygon của profile, độ dày, cao độ, thông số tiết diện, family, host, top level và document identity.
- **Hydration Index Sổ cái & Bảo toàn Metadata Journal**:
  - `BimCoordinationLedgerService.LoadFromFile` phải tự động khôi phục và hydrate toàn bộ chỉ mục `_mappings` (SourceKey → Revit UniqueId) từ dữ liệu lưu trên đĩa.
  - `IntentJournal.UpdateStatusAsync` khi cache miss phải nạp lại record gốc từ đĩa trước khi cập nhật trạng thái, bảo toàn 100% metadata (`PlanHash`, `ItemCount`, `SourceDoc`, `TargetDoc`) qua các phiên process restart.
  - `BimCoordinationLedgerSerializer` thực hiện Atomic File Replace (.tmp -> atomic replace) kèm file-lock để chống hư hỏng dữ liệu khi tiến trình bị tắt đột ngột.
- **Đối soát Reconcile 3 Yếu tố Chặt chẽ**: `ReconcileService` chỉ công nhận `Committed` khi receipt trong Extensible Storage khớp đồng thời cả 3 yếu tố: `OperationId`, `PlanHash`, và `TargetDocId`. Nếu không tìm thấy receipt hoặc thiếu bằng chứng, giữ nguyên trạng thái `OutcomeUnknown` để ngăn chặn retry ghi đè mù quáng.

### R3. Phase 3 — Đo đạc Hình học QA/QC Thực tế & Chuyển đổi E2E Tests
- **Kiểm toán Hình học Thực tế (Measured Geometric Verification)**: Nối `MeasuredGeometricVerifier` vào lệnh `VerifyCommand` và quy trình nghiệm thu CLI. Thực hiện truy vấn trực tiếp hình học (Solid, Curve, Elevation) từ Revit API qua named pipe để so sánh với dung sai `CoordinationTolerances`. Loại bỏ toàn bộ các số liệu gán sẵn (`AvgCadEdgeDevMm=0.5`...) và cấm nghiệm thu nếu thiếu dữ liệu đối chiếu (phải trả về `Inconclusive`).
- **Xóa bỏ Shadow Mocks trong E2E Tests**: Refactor toàn bộ `HPBim.E2E.Tests`, loại bỏ hoàn toàn các lớp shadow contracts tự tạo (`E2ESchemaValidator`, `E2EExecutionPlan`, `E2EIntentJournal`...), chuyển 100% sang kiểm thử trực tiếp các production classes của `HPBim.Core` và `HPBim.Client`. Chỉ sử dụng fake test ở biên truyền tải I/O Named Pipe.

## Acceptance Criteria
- [ ] **Test Production Coverage**: 100% tests trong `HPBim.Core.Tests`, `HPBim.Client.Tests`, và `HPBim.E2E.Tests` sử dụng trực tiếp production classes và PASS 100% (zero regressions trên toàn bộ tests của repo).
- [ ] **SourceKey Wire Roundtrip**: Test chứng minh `CompositeSourceKey` string canonical giữ nguyên equality sau chu trình `ExecutionPlan` -> `BridgeJson` -> `ScriptArgs` -> `BimPlanRunner` -> `ExecuteResult` -> `Ledger/Receipt`.
- [ ] **Strict Batch Rollback**: Test chứng minh khi 1 cấu kiện trong batch 10 phần tử bị lỗi, toàn bộ batch bị rollback, không có receipt nào được tạo và CLI trả về exit code 1.
- [ ] **No Synthesized Created**: Test chứng minh khi response bị rỗng hoặc truncated >64KB, pipeline trả về `OutcomeUnknown` và kích hoạt Reconcile, không tự tạo kết quả `Created`.
- [ ] **Ledger & Journal Persistence Across Restarts**: Test khởi tạo instance `IntentJournal` và `BimCoordinationLedgerService` mới từ file trên đĩa chứng minh `_mappings` được hydrate đầy đủ và metadata của intent không bị mất khi cập nhật trạng thái.
- [ ] **Measured Geometric Verification**: Cấu kiện sai lệch hình học hoặc thiếu bằng chứng đo đạc từ Revit API bị đánh dấu `Inconclusive` hoặc `Defect`, cấm tự động chuyển sang `ConfirmedPass`.
- [ ] **Revit API Binding Probe**: Biên dịch thành công mã runner C# Roslyn đối chiếu với các reference assemblies của Revit 2026.
- [ ] **Repository & Governance Rules**:
  - Không tạo dependency hai chiều giữa `HPAutoCad` và `HPRebar`.
  - Tuân thủ triệt để Rule 0 (Zero guesswork) và Clean Code: file <= 300 dòng (Rule C2), method <= 50 dòng (Rule M3), không code comment (Q-B1), không empty catch (Q-B2).
  - Không tự ý commit/push git hoặc deploy add-in ra môi trường thực.

## Operational Protocol
1. Create `BRIEFING.md` and `progress.md` in your working directory immediately upon starting.
2. Establish a structured milestone plan (`plan.md`).
3. Coordinate specialists (Explorers, Workers, Reviewers, Challengers) adhering strictly to Clean Code standards.
4. Keep `progress.md` updated continuously so Sentinel crons track health and report to the user.
5. When complete, submit final handoff and completion notice to Sentinel via `send_message`.


## 2026-10-08T11:32:01Z

You are orchestrator_5, the Project Orchestrator for completing the BIM Pipeline and Coordination Ledger for HPBim (P0 and P1 remediation).
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_5\
Your parent Sentinel ID is: a09df7a8-6df3-43d7-ac7b-40b8c01e2243

Please read your complete dispatch specifications at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_5\DISPATCH.md
and the authoritative user request at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (see ## 2026-10-08T11:29:50Z)

Immediately create your BRIEFING.md, progress.md, and plan.md in your working directory, and coordinate the team to implement and verify R1, R2, and R3 according to all acceptance criteria and Clean Code standards.
