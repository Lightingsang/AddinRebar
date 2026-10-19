# Dispatch Instructions

## 2026-10-07T13:52:22Z
You are the Project Orchestrator (teamwork_preview_orchestrator) for the HPBim deliverable.

## Working Directory & Context
- Your working directory: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_1\
- Project root directory: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar
- Project workspace directory: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim
- Authoritative user request: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (inspect the latest entry `## 2026-10-07T13:49:28Z`).

## Objective
Triển khai hoàn thiện quy trình BIM Pipeline và Coordination Ledger cho HPBim (P0 và P1) tại G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim: bảo đảm kết quả trung thực, preview và apply đồng nhất 100%, có khả năng tự phục hồi và tiếp tục an toàn sau lỗi hoặc đứt kết nối mà không tạo trùng lặp hay báo thành công sai.

## Scope of Work

### R1. P0 — Kết quả trung thực & Preview/Apply tương đương
- **Phân định Artifact & Schema Validation**: Kiểm tra hợp chuẩn schema, version, required fields và dependencies; từ chối dứt khoát raw CAD JSON hoặc file sai định dạng, không deserialize thành BimModel rỗng rồi báo PASS.
- **Single Source of Truth Execution Plan**: Toàn bộ logic giải quyết tài nguyên (level, family symbol, tọa độ, transform) và ranh giới transaction được thống nhất thành một Execution Plan (IR) duy nhất dùng chung cho cả Preview (Dry-Run với rollback) và Apply (Commit). Loại bỏ các script phân tán trong CLI commands.
- **Báo cáo chi tiết từng cấu kiện (Per-Element Granularity)**: Mọi cấu kiện trong plan phải trả về trạng thái rõ ràng: Created, Updated, NoOp, Skipped hoặc Failed kèm ID và lý do chi tiết. Cấm fallback ngầm (chọn type đầu tiên, bỏ qua lỗi).
- **Rollback Verification & Status Honesty**: Dry-run chỉ được báo thành công khi mã thực thi không có lỗi và Revit bridge xác nhận transaction đã được rollback 100%. Phản ánh trung thực partial commit và outcome unknown; thu thập warning từ Revit.
- **Loại bỏ Metric/PASS Literal chưa đo**: Mọi đánh giá QA/QC phải dựa trên dữ liệu đo đạc thực nghiệm. Nếu thiếu nguồn dữ liệu đối chiếu hoặc thiếu thông số đo, phải kết luận Inconclusive, cấm tự động nghiệm thu.

### R2. P1 — Phục hồi & Idempotency qua các phiên
- **Document & Plan Identity**: Quản lý định danh tài liệu Revit (Document Title, Path, ActiveView), snapshot revision/hash và plan hash. Kiểm tra tính hợp lệ và độ mới của tài liệu trên Revit thread trước khi chạy mỗi batch.
- **Composite Source Key & Provenance Mapping**: Định danh cấu kiện bằng Composite Source Key `[DocIdentifier]#[Category]:[PrimaryHandle]` kèm danh sách ProvenanceHandles thành phần. Thiết lập bảng ánh xạ SourceKey → Revit UniqueId. Thực thi Idempotency: chạy lại cùng plan không sinh cấu kiện trùng lặp.
- **Intent Journal & Revit Extensible Storage Receipt**: Ghi Operation Intent vào Journal cục bộ trước khi gửi qua pipe. Trong Revit, lưu Operation ID và Element Mapping nguyên tử cùng transaction dựng hình vào Revit Extensible Storage (DataStorage element ẩn).
- **Phân tách Timeout & Tự động Reconcile**: Tách biệt transport timeout, queue deadline và execution budget. Khi gặp timeout sau khi gửi hoặc đứt kết nối, đánh dấu `OutcomeUnknown`. Khi kết nối lại, tự động đọc Extensible Storage trong Revit để xác nhận commit status và hoàn tất mapping mà không gửi lệnh mù.
- **Kiểm toán hình học độc lập (Measured Geometric Verification)**: Kiểm toán sau commit bằng cách đo trực tiếp từ Revit API (tâm, cao độ đỉnh/đáy, kích thước tiết diện từ Solid/Curve/Parameters) so sánh với BimExecutionPlan trong phạm vi CoordinationTolerances; không dùng số lượng cấu kiện (counts) để nghiệm thu.

### R3. P2 — Lập đề xuất kiến trúc (Design Proposal Only)
- Soạn thảo tài liệu đề xuất kiến trúc (Design Proposal) cho các tính năng nâng cao trong tương lai: change tracking hai chiều, hàng đợi xử lý nền (background queue), và batching quy mô lớn.

## Acceptance Criteria
- [ ] Toàn bộ 370 unit/integration tests hiện hữu (268 Core + 102 Client) tiếp tục PASS 100% (zero regressions).
- [ ] Bộ unit test mới kiểm thử Schema Validation: Chặn đứng raw CAD JSON hoặc schema không hợp lệ, không để lọt model rỗng báo PASS.
- [ ] Bộ test mới kiểm thử Single Execution Plan: Chứng minh Preview và Apply thực thi cùng một plan logic, chỉ khác cờ rollback/commit.
- [ ] Bộ test mới kiểm thử Dry-Run: Chứng minh Dry-Run báo FAIL nếu có lỗi execution hoặc không có xác nhận rollback từ bridge.
- [ ] Bộ test mới kiểm thử Idempotency: Chạy plan lần đầu sinh cấu kiện; chạy lại lần 2 nhận diện trạng thái NoOp/Update, không tạo trùng element trong RVT.
- [ ] Bộ test mới kiểm thử Reconcile & Recovery: Mô phỏng đứt kết nối/timeout sau commit; cơ chế Reconcile tự động phát hiện receipt trong Extensible Storage và cập nhật ledger chính xác.
- [ ] Bộ test mới kiểm thử Geometric Verification: Cấu kiện sai lệch kích thước hoặc cao độ vượt quá `CoordinationTolerances` bị đánh dấu Defect/Failed ngay cả khi số lượng cấu kiện (counts) khớp 100%.

## Governance & Protocols
- Không tạo dependency hai chiều giữa `HPAutoCad` và `HPRebar`. `HPBim` chỉ tham chiếu `McpShared/`.
- Tuân thủ ĐIỀU 0: "Khi chưa đủ cơ sở để kết luận, hãy dừng lại và báo chưa đủ thông tin. Không được suy đoán để hoàn thành công việc. Thà để một cấu kiện chưa dựng còn hơn dựng sai nhưng báo đúng."
- Tuân thủ Clean Code standards (`HP_CLEAN_CODE_CORE.md`, `REVITADDINAI_CLEAN_CODE_STANDARD.md`).


## 2026-10-07T14:40:57Z
[Server Restart - Resume Instructions]
Hệ thống vừa khởi động lại. Người dùng đã gửi lệnh tiếp tục ("tiep tuc") và yêu cầu đẩy mạnh tiến độ triển khai các Milestone M1-M5 cho HPBim theo kế hoạch.

Trạng thái ghi nhận trước khi restart:
- Phase 0 Survey đã hoàn tất.
- M1 Exploration đã hoàn tất (3 Explorers).
- Track A (E2E Testing Track): subagent `test_writer_e2e_1` (`ec29dcfe-8f06-4b9f-955d-57314a814618`).
- Track B (Milestone M1 Implementation): subagent `worker_m1_pipeline` (`6802773d-33f4-48d6-9437-fef790c60a76`).

Vui lòng kiểm tra trạng thái trên đĩa của các subagent, gửi tin nhắn hồi sinh (revive) cho các subagent đang dở dang hoặc khởi tạo lại nếu cần, và tiếp tục dẫn dắt đội ngũ hoàn thiện các Milestone M1-M5 theo `PROJECT.md` và `ORIGINAL_REQUEST.md`.
Sentinel đã tái kích hoạt 2 cron giám sát (Cron 1 & Cron 2) và đang túc trực.


## 2026-10-07T16:25:44Z
[Server Restart - Resume Instructions]
Hệ thống vừa khởi động lại lần thứ hai lúc 16:24:11Z. Người dùng đã gửi lệnh tiếp tục ("tiep tuc") và yêu cầu tiếp tục đẩy mạnh tiến độ triển khai các Milestone cho HPBim theo đúng kế hoạch.

Trạng thái hệ thống:
- Sentinel đã ghi nhận yêu cầu vào `ORIGINAL_REQUEST.md` (mục `## Follow-up — 2026-10-07T16:24:11Z`).
- Sentinel đã thiết lập lại 2 cron giám sát: Cron 1 (`task-179`) và Cron 2 (`task-181`).
- Các subagent của bạn (`test_writer_e2e_1` tại `ec29dcfe-8f06-4b9f-955d-57314a814618` và `worker_m1_pipeline` tại `6802773d-33f4-48d6-9437-fef790c60a76`) có thể đã dừng do restart.

Hành động bắt buộc:
1. Kiểm tra trạng thái hiện tại trên đĩa của `worker_m1_pipeline` và `test_writer_e2e_1`.
2. Gửi tin nhắn hồi sinh (revive) cho các subagent này hoặc khởi động lại (respawn) nếu chúng đã kết thúc/mất trạng thái.
3. Thúc đẩy `worker_m1_pipeline` hoàn tất các nhiệm vụ M1:
   - Sửa lỗi CS0535 trong `FakeRevitPipeClient.cs`.
   - `BimModelSchemaValidator` chặn raw CAD JSON và `{}` bypass.
   - `BimExecutionPlan` IR thống nhất Preview và Apply.
   - Loại bỏ fallback ngầm `.First()` và ghi nhận `ElementExecutionRecord`.
   - Chạy kiểm thử xUnit và chuẩn bị nghiệm thu qua Reviewers/Challengers/Auditor.
4. Thúc đẩy `test_writer_e2e_1` hoàn tất `TEST_INFRA.md` và các bộ test 4 tầng.

Vui lòng tiếp tục điều phối và cập nhật `progress.md`.

## 2026-10-07T18:57:07Z
[Parent Sentinel Acknowledgment]
Sentinel has received and logged the official completion of Milestone M1 (PASS, 473/473 tests green, Clean Code C2/M3 fully satisfied, Auditor CLEAN) and the initiation of Milestone M2 (P0 Verification & Measured Geometric Audit).
Proceed with Milestone M2 exploration and subsequent worker implementation as planned. Sentinel monitors liveness and progress in the background.

## 2026-10-07T19:06:46Z
[Parent Sentinel Acknowledgment]
Sentinel has received and logged the completion of M2 Exploration and the dispatch of `worker_m2_r1` (`3505bccf-f511-4e4a-b352-2ae4012a3259`).
Proceed with implementation under strict Clean Code constraints (C2 <= 300, M3 <= 50) and zero regressions. Sentinel will continue monitoring progress and liveness in the background.




## 2026-10-07T21:59:25Z
Received from explorer_m4_it2_3 (88fb1254-9006-4076-af14-22ea88a9a12f):
**Context**: Milestone M4 Clean Code AST Measurement & Verification Investigation
**Content**: Investigation complete. We have diagnosed and verified the attestation discrepancy, redesigned the verification tooling, audited all 24 files, and formulated the remediation blueprint:
1. Root Cause: scratch/verify_m4_clean_code.py had a logic hole in expression-bodied method parsing (=>). It skipped brace counting in raw multiline strings ("""), but its fallback searched for the first semicolon on any subsequent line. It matched line 9 (var schemaGuid = new Guid(...);) inside the string literal, falsely truncating GetQueryScript() (54 physical lines) to 2 lines. It also masked GetPreambleScript() (133L -> 5L), GetReceiptStorageScript() (43L -> 5L), and merged ComputeTotalTimeout() with Validate() (2L -> 18L).
2. Authentic Tooling: We rebuilt scratch/verify_m4_clean_code.py with an authentic C# lexical scanner and enhanced scratch/RoslynSyntaxAuditor (Roslyn AST parser). Both tools now accurately report physical spans (EndLine - StartLine + 1) and return exit code 1 upon any violation.
3. Census of All 24 Files: Exhaustive audit of all 130 executable members across all 24 files confirms:
   - Rule C2 (File Length <= 300): 24/24 PASS (Max file: 240 lines).
   - Rule M3 (Method Length <= 50): Exactly 2 methods > 50L (GetQueryScript 54L in ExtensibleStorageQueryScriptTemplate.cs, and baseline GetPreambleScript 133L in BimPlanRunnerScriptTemplate.cs).
   - The other 22 files have ZERO hidden M3 violations (next longest method is 43L).
4. Remediation Blueprint & Tables: Full findings, pre/post-remediation tables, and code split patterns are documented in report.md and handoff.md.
**Action**: Please review the report and handoff, and direct worker_m4_r1 (or remediation worker) to execute the 2-method split on ExtensibleStorageQueryScriptTemplate.cs and update the attestation table in report.md.


## 2026-10-07T22:09:06Z
Received from worker_m4_it2_r1 (d9b211bc-f55c-4e09-8328-3732e8176ae1):
**Context**: Milestone M4 Remediation (Recovery & Reconciliation Clean Code Clearance)
**Content**: Remediation tasks for Milestone M4 are 100% complete.
1. `ExtensibleStorageQueryScriptTemplate.cs`: Decomposed `GetQueryScript()` into `GetQueryHeaderScript()` (17L) + `GetQueryFilterAndReturnScript()` (28L) + `GetQueryScript()` (3L). Max method is 28L (Rule M3 PASS), file is 58L (Rule C2 PASS).
2. `ExtensibleStorageReceiptReader.cs`: Added defensive `if (item.ValueKind != JsonValueKind.Object) continue;` and extracted `ParseSingleReceipt(JsonElement item)` (15L). Max method is 30L (`QueryReceiptsAsync`), file is 166L (Rule M3 & C2 PASS).
3. `AdversarialReceiptReaderTests.cs`: Updated non-object test to assert `Assert.Empty(receipts)` and added mixed array extraction test. Added query script verification test in `ExtensibleStorageReceiptReaderTests.cs`.
4. `BimPlanRunnerScriptTemplate.cs`: Decomposed pre-existing 133-line `GetPreambleScript()` into 3 helpers (35L, 31L, 34L), eliminating legacy M3 debt.
5. `scratch/verify_m4_clean_code.py`: Enhanced lexer to support interpolated raw strings `$$"""`.
6. Verification:
   - `python scratch/verify_m4_clean_code.py` -> ALL 24 FILES PASS (Exit code 0).
   - `dotnet run --project scratch/RoslynSyntaxAuditor/RoslynSyntaxAuditor.csproj` -> OVERALL AUDIT VERDICT: CLEAN / 100% PASS (Exit code 0).
   - `dotnet build HPBim/HPBim.slnx` -> 0 Warning(s), 0 Error(s).
   - Total Tests Passing: 1,578 / 1,578 tests (HPBim Client: 247, Core: 381, E2E: 42; McpShared Server: 793, Net48: 115).
Comprehensive reports and authentic attestation table are written to:
- Report: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_it2_r1\report.md`
- Handoff: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_it2_r1\handoff.md`
**Action**: Please trigger the forensic auditor to re-evaluate Milestone M4 for final gate clearance.


## 2026-10-07T22:16:07Z
Received from reviewer_m4_it2_2 (b54eb8cb-87c0-42ee-85b0-850674418eeb):
**Context**: Milestone M4 Review & Adversarial Stress-Testing (Iteration 2)
**Content**: Milestone M4 remediation review and adversarial stress-testing are complete.
Official Verdict: **APPROVE**.
Key verifications:
1. Rule C2 Compliance: All 24 files are strictly <= 300 physical lines (maximum file is 240 lines).
2. Rule M3 Compliance: All 137 executable members across 24 files are strictly <= 50 physical lines (0 violations).
   - ExtensibleStorageQueryScriptTemplate.cs max method is 28 lines (<= 28).
   - BimPlanRunnerScriptTemplate.cs max method is 35 lines (<= 35).
3. Robustness & Defensive Guard: ExtensibleStorageReceiptReader.cs:119-125 non-object JSON array item guard verified, skips malformed items gracefully.
4. Rule 0 Zero Guessing: ReconcileService.cs transitions OutcomeUnknown strictly on verified Extensible Storage evidence (Committed or RolledBack); network/timeout errors preserve StillUnknown.
5. Tooling & Build/Tests:
   - python scratch/verify_m4_clean_code.py -> Exit code 0 (24/24 PASS).
   - dotnet run --project scratch/RoslynSyntaxAuditor/RoslynSyntaxAuditor.csproj -> Exit code 0 (100% CLEAN).
   - dotnet build HPBim/HPBim.slnx -> 0 warnings, 0 errors.
   - All 1,578 tests across HPBim and McpShared pass 100% (0 failures, 0 regressions).
6. Integrity Audit: Zero hardcoded shortcuts, facade implementations, or integrity violations.
**Action**: Accept review verdict APPROVE and advance Milestone M4 to DONE in PROJECT.md.


## 2026-10-07T22:16:13Z
Received from auditor_m4_it2_1 (0ced3f80-6600-41a5-9e79-6ad68055e338):
**Context**: Milestone M4 (Recovery & Reconciliation) Forensic Re-Evaluation after worker_m4_it2_r1 remediation.
**Content**: Forensic re-audit complete. **VERDICT: CLEAN (100% Approved)**.
Key findings:
1. Rule C2 Compliance: All 24 files are strictly <= 300 physical lines (maximum file is 241 lines in IntentJournal.cs and AutoReconcileIdempotencyTests.cs).
2. Rule M3 Compliance: All 137 members across all 24 files are strictly <= 50 physical lines (maximum member is 43 lines).
   - ExtensibleStorageQueryScriptTemplate.cs:GetQueryScript() violation resolved (all <= 28 lines).
   - BimPlanRunnerScriptTemplate.cs:GetPreambleScript() decomposed (all <= 35 lines).
3. Attestation Veracity: The master 24-file attestation table in worker_m4_it2_r1/report.md matches physical disk reality and Roslyn AST member lengths to 100% precision. Both RoslynSyntaxAuditor and verify_m4_clean_code.py exit with code 0.
4. Forensic Integrity & Non-Bypass: ExtensibleStorageReceiptReader.cs authentically filters non-object array elements. Zero hardcoded test results, zero dummy facades, zero commented-out code, zero undocumented empty catches. Hardware disk flush confirmed.
5. Build & Test Verification:
   - dotnet build HPBim/HPBim.slnx: 0 errors, 0 warnings.
   - HPBim.slnx tests: 670 / 670 passed.
   - McpShared.slnx tests: 908 / 908 passed.
   - Total test pass: 1,578 / 1,578 (100% pass, 0 failures, 0 regressions).
**Action**: Proceed to mark Milestone M4 as complete and proceed with remaining milestones (M5 / Final Acceptance).

## 2026-10-07T22:16:15Z
Received from challenger_m4_it2_1 (2f6ada81-cc35-4dbf-9004-37af6980bd32):
**Context**: Milestone M4 Iteration 2 Challenger Verification (challenger_m4_it2_1)
**Content**: Empirical and adversarial challenge complete.
- Query Script Roslyn Validation: GetQueryScript() parses into a Roslyn C# syntax tree with 0 errors and 0 warnings.
- Adversarial Array & JSON Parsing: Dedicated 48-test adversarial harness passed 100% (48/48). Non-object elements skipped safely without exceptions; mixed arrays extract valid receipt objects; malformed/truncated element map JSONs return empty dictionaries safely.
- Test Suites: All 19 tests in HPBim.Client.Tests.exe --filter-class "*ReceiptReader*" passed, and all 1,578 tests across HPBim and McpShared passed (0 failures).
- Clean Code Verification: Verified clean via RoslynSyntaxAuditor and verify_m4_clean_code.py across all 24 files.
- Explicit Verdict: **APPROVE**
**Action**: Proceed with Milestone M4 gate clearance and next steps.


## 2026-10-07T22:16:27Z
Received from reviewer_m4_it2_1 (4a552e68-9d95-4a9a-9d4c-d0e01b3fac7e):
**Context**: Review of Milestone M4 Iteration 2 Remediation (worker_m4_it2_r1)
**Content**: Review and adversarial stress-testing complete. Verdict: **APPROVE**.
- All 4 targeted remediation items verified:
  1. ExtensibleStorageQueryScriptTemplate.cs: Decomposed into GetQueryHeaderScript (17L) and GetQueryFilterAndReturnScript (28L) + GetQueryScript (3L), strictly satisfying Rule M3 (<= 50L) and C2 (<= 300L).
  2. ExtensibleStorageReceiptReader.cs: Defensive check if (item.ValueKind != JsonValueKind.Object) continue; added and ParseSingleReceipt (15L) extracted. Max method in file is 30L.
  3. BimPlanRunnerScriptTemplate.cs: Legacy 133L preamble decomposed into 3 helpers (35L, 31L, 34L). All methods in file <= 35L.
  4. Test suites: Tested non-object elements, corrupted JSON, and template generation.
- Independent AST audit via scratch/RoslynSyntaxAuditor: 24 / 24 files pass Rule C2 (<= 300L), 131 / 131 members pass Rule M3 (<= 50L), zero catch violations.
- Build & Tests: dotnet build HPBim/HPBim.slnx passed with 0 warnings, 0 errors. All 1,578 tests across HPBim and McpShared pass 100%.
**Action**: Proceed with Milestone M4 gate clearance and progression to Milestone M5.
