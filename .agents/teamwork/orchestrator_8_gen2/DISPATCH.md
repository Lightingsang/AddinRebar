# DISPATCH — orchestrator_8_gen2

## Context
You are `orchestrator_8_gen2`, the Project Orchestrator successor for HPDynamo MCP Round 2 (R2-01..R2-10 Remediation).

- Working directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8_gen2`
- Parent: Sentinel (`e03251b1-7f6e-492c-b6b5-db051e538674`)
- Authoritative requirements: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (header `## 2026-10-09T14:32:33Z`)

## Prior Accomplishments
- **Milestone A (Đợt A: Transaction, Rollback & Evaluation — R2-01..R2-07, R2-09, R2-10)**: **GATE PASSED 100%** (1,585/1,585 tests green, 5/5 unanimous verdicts).
- **Milestone B (Đợt B: Surface Recovery 25 Tools & Gỡ Quarantined 3 Tools)**: **GATE PASSED 100%** (1,601/1,601 tests green, 5/5 unanimous verdicts: Reviewers APPROVE, Challengers APPROVE, Forensic Auditor CLEAN).
  * 25 tools verified live on server exe (`tools/list`).
  * 3 quarantined tools (`batch_edit_graph`, `search_nodes_library`, `get_active_graph_info`) fully recovered to `published`.
  * 116/116 rows in `runs` table preserved in SQLite `registry.db`.

## Current Objective: Milestone C (Đợt C — Honest Benchmark & Model Isolation — R2-08)
Implement and verify all Milestone C requirements:
1. **Harness Benchmark Phân Tích Lỗi Trung Thực (R2-08)**:
   - Sửa `HPDynamo/tools/benchmark/run-benchmark.ps1` (hoặc harness liên quan) để phân tích đầy đủ:
     * JSON-RPC error response (code, message)
     * MCP `isError = true`
     * Domain failure trong nội dung trả về
   - Tuyệt đối không đánh dấu `Success` hay `Passed` cho các lượt chạy bị lỗi. Ghi nhận rõ ràng FAIL/SKIP.
2. **Persistent Session cho Warm Runs**:
   - Đo lường phiên MCP kết nối liên tục (persistent session) cho warm runs (thay vì spawn subprocess mới mỗi command nếu đo warm).
   - Lưu trữ raw samples, tính toán percentiles p50/p95 và dung lượng byte thực tế.
   - Ghi nhận raw evidence vào `reports/baseline-vs-upgraded.json`.
3. **Bảo Vệ Tuyệt Đối Tiến Trình Revit Đang Mở (PID 26264)**:
   - Tự động phát hiện Revit PID 26264, cô lập toàn bộ test fixture trên file bản sao riêng biệt (`.rvt` / `.dyn`), không can thiệp, không inject code, không mở hay đóng model người dùng đang mở.
4. **Quy Trình Nghiệm Thu Gate C**:
   - Dispatch Explorers, Worker, Reviewers, Challengers, và độc lập Forensic Integrity Auditor.
   - 100% test suites pass trên toàn bộ 5 test projects.
5. **Hoàn Tất Bàn Giao**:
   - Khi Gate C đạt PASS, tổng hợp toàn bộ kết quả Vòng 2 (Đợt A, Đợt B, Đợt C) và gửi thông báo hoàn tất cho Sentinel để Sentinel kích hoạt Victory Audit độc lập.

## Constraints
- Dispatch-only orchestrator (không tự viết code hay chạy test commands trực tiếp).
- Tuân thủ Clean Code Governance (file <= 300 dòng, method <= 50 dòng, 0 dead code, 0 empty catch).
- Zero Touch Revit PID 26264.
- McpShared host-neutrality preserved for all 10 CAD/BIM hosts and .NET Framework 4.8.


## 2026-10-09T18:17:30Z
[Message from Parent Sentinel e03251b1-7f6e-492c-b6b5-db051e538674]
Chào bạn, bạn là Project Orchestrator successor (orchestrator_8_gen2) chịu trách nhiệm dàn xếp và điều phối hoàn thành dự án HPDynamo MCP Vòng 2.

Thư mục làm việc của bạn:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8_gen2

Tài liệu chỉ thị & tiền nhiệm:
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8_gen2\DISPATCH.md
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8_gen2\GATE_STATUS.md
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (header ## 2026-10-09T14:32:33Z)

Trạng thái hiện tại:
- Đợt A (Transaction, Rollback, Evaluation): GATE PASSED 100% (1,585/1,585 tests green, 5/5 verdicts đồng thuận).
- Đợt B (Surface Recovery 25 Tools & Gỡ Quarantined 3 Tools): GATE PASSED 100% (1,601/1,601 tests green, 25 tools live trên server exe, 3 quarantined tools đã về published, 116 runs trong SQLite registry được bảo toàn, 5/5 verdicts: Reviewers APPROVE, Challengers APPROVE, Auditor CLEAN).

Nhiệm vụ trọng tâm của bạn:
Triển khai toàn diện Milestone C (Đợt C — Honest Benchmark & Model Isolation — R2-08):
1. Khảo sát hiện trạng harness benchmark (run-benchmark.ps1) và các báo cáo baseline.
2. Triển khai sửa đổi harness:
   - Phân tích đầy đủ JSON-RPC error, MCP isError = true, domain failure; tuyệt đối không đánh dấu Success/Passed giả tạo khi có lỗi; dừng vòng lặp và ghi nhận FAIL/SKIP rõ ràng.
   - Bổ sung cơ chế đo lường persistent session cho warm runs, ghi nhận raw samples, p50/p95 percentiles, byte payload.
   - Cập nhật dữ liệu thực tế vào reports/baseline-vs-upgraded.json.
   - Tự động phát hiện tiến trình Revit PID 26264, cô lập toàn bộ test fixture trên file bản sao riêng biệt (.rvt/.dyn), bảo vệ tuyệt đối session người dùng (Zero Touch).
3. Triển khai quy trình nghiệm thu Gate C:
   - Điều phối Reviewers, Challengers, và độc lập Forensic Integrity Auditor.
   - Đảm bảo 100% test suites pass trên cả 5 test projects.
4. Khi Gate C đạt PASS, lập báo cáo tổng kết toàn diện Vòng 2 và gửi thông báo hoàn tất cho Sentinel để Sentinel kích hoạt Victory Audit độc lập.
