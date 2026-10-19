# Forensic Audit Report & Handoff: Milestone C (HPDynamo MCP Vòng 2)

**Auditor:** `auditor_c_1` (Forensic Auditor)  
**Target:** HPDynamo MCP Vòng 2 — Milestone C (Đợt C: Honest Benchmark & Model Isolation — R2-08)  
**Working Directory:** `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_c_1`  
**Profile:** General Project (Development Mode per `ORIGINAL_REQUEST.md`)  
**Verdict:** **INTEGRITY VIOLATION** (Work Product REJECTED)

---

## Forensic Audit Summary

| Check # | Check Name | Status | Details |
|---|---|:---:|---|
| 1 | **Anti-Cheating & Output Honesty** | 🔴 **FAIL** | `benchmark_session.py` (dòng 166–178) trả về `"PASS"` và gắn nhãn `"Verified Live (Persistent Session)"` ngay cả khi 100% warm runs thất bại hoặc bridge bị ngắt kết nối. |
| 2 | **Facade Outcome Evaluation** | 🔴 **FAIL** | `evaluate_outcome` tự động trả về `PASS` cho phản hồi rỗng `{"result": {}}`, thiếu `content`, hoặc thiếu cả `result` và `error`. |
| 3 | **Failure Count Masking** | 🔴 **FAIL** | `build_workload_record` che giấu số lượng lỗi thực tế (`failureCount` bị gán 0 khi có lỗi trong warm runs). |
| 4 | **Adversarial Stress Test** | 🔴 **FAIL** | Thực thi `test_adversary_benchmark.py` ghi nhận **12/17 tests FAILED** (văng lỗi unhandled crash và false positives). |
| 5 | **Zero Touch Revit PID 26264** | 🟢 **PASS** | `run-benchmark.ps1` nhận diện 2 tầng PID 26264, kích hoạt Zero-Touch Shield, sandbox cô lập trong `%TEMP%`, dọn dẹp sạch sẽ 100%. |
| 6 | **Independent Test Execution** | 🟢 **PASS** | 1,601/1,601 .NET tests (5 projects) + 11/11 Python harness unit tests đạt PASS 100%. |
| 7 | **Clean Code Governance** | 🟢 **PASS** | Các deliverables Milestone C đều $\le 300$ dòng (217, 255, 123 dòng), hàm $\le 50$ dòng, 0 dead code, 0 unannotated empty catch. |

---

## 1. Observation (Quan Sát Trực Tiếp)

### 1.1. Bằng chứng vi phạm: False Positive "Verified Live" & "PASS" khi Warm Runs thất bại
- **Tập tin:** `HPDynamo/tools/harness/benchmark_session.py` (và bản sao `HPDynamo/tools/benchmark/benchmark_session.py`), dòng 166–178:
  ```python
  166:     warm_samples = []
  167:     for _ in range(warm_runs):
  168:         tw0 = time.perf_counter()
  169:         w_resp = server.call(wl["tool"], wl["args"])
  170:         w_ms = round((time.perf_counter() - tw0) * 1000.0, 2)
  171:         _, w_ok, _, _ = evaluate_outcome(w_resp)
  172:         if w_ok:
  173:             warm_samples.append(w_ms)
  174: 
  175:     stats = compute_latency_stats(warm_samples)
  176:     print(f"PASSED (p50: {stats['p50']}ms, p95: {stats['p95']}ms, Req/Resp: {req_b}B/{resp_b}B)")
  177:     return "PASS", build_workload_record(wl, "Verified Live (Persistent Session)", stats, cold_ms, startup_ms, warm_samples, "", req_b, resp_b)
  ```
- **Hiện tượng thực nghiệm:**
  - Khi cold run thành công nhưng toàn bộ các lần chạy warm bị lỗi (ví dụ: bridge crash hoặc ném lỗi RPC):
    - `w_ok = False`, không có phần tử nào được thêm vào `warm_samples` (`len(warm_samples) == 0`).
    - Dòng 176 in ra màn hình: `PASSED (p50: 0.0ms, p95: 0.0ms...)`.
    - Dòng 177 trả về trạng thái `"PASS"` và nhãn `"Verified Live (Persistent Session)"`!
    - Báo cáo tổng hợp tăng biến `passedWorkloads`, ghi nhận workload thành công và tiến trình kết thúc với exit code 0.

### 1.2. Bằng chứng vi phạm: Facade Evaluation trên phản hồi rỗng
- **Tập tin:** `HPDynamo/tools/harness/benchmark_session.py`, dòng 56–82:
  ```python
  56: def evaluate_outcome(resp):
  ...
  64:     res = resp.get("result", {})
  65:     texts = [c.get("text", "") for c in res.get("content", []) if isinstance(c, dict) and c.get("type") == "text"]
  66:     joined = "\n".join(texts).strip()
  ...
  82:     return "PASS", True, False, ""
  ```
- **Hiện tượng thực nghiệm:**
  - `resp = {"jsonrpc": "2.0", "id": 1}` (thiếu cả `result` và `error`): trả về `("PASS", True, False, "")`.
  - `resp = {"result": {}}` (thiếu `content`): trả về `("PASS", True, False, "")`.
  - `resp = {"result": {"content": []}}` (`content` rỗng): trả về `("PASS", True, False, "")`.
  - `resp = {"result": {"content": "string"}}`: trả về `("PASS", True, False, "")`.

### 1.3. Bằng chứng vi phạm: Che giấu số lượng lỗi (Failure Masking)
- **Tập tin:** `HPDynamo/tools/harness/benchmark_session.py`, dòng 143:
  ```python
  143: "failureCount": 1 if "FAILED" in status_label else 0,
  ```
- **Hiện tượng thực nghiệm:** Khi một phần warm runs thất bại (ví dụ 3/5 calls thất bại), do `status_label` là `"Verified Live (Persistent Session)"`, `failureCount` được tính là `0` thay vì `3`.

### 1.4. Bằng chứng thực thi Adversarial Test Suite
- **Lệnh thực thi:** `python HPDynamo/tools/harness/test_adversary_benchmark.py`
- **Kết quả verbatim:**
  ```
  FAILED (failures=12)
  Ran 17 tests in 0.006s
  ```
- **Danh sách 12 test thất bại:**
  1. `test_adv_result_is_none` -> Crash: `AttributeError: 'NoneType' object has no attribute 'get'`
  2. `test_adv_error_is_string` -> Crash: `AttributeError: 'str' object has no attribute 'get'`
  3. `test_adv_error_is_none` -> Crash: `AttributeError: 'NoneType' object has no attribute 'get'`
  4. `test_adv_missing_result_and_error_fields` -> False Positive: trả về `'PASS'`
  5. `test_adv_empty_result_dict` -> False Positive: trả về `'PASS'`
  6. `test_adv_empty_content_list` -> False Positive: trả về `'PASS'`
  7. `test_adv_all_warm_runs_fail_must_not_report_pass` -> False Positive: trả về `'PASS'` và `'Verified Live'`
  8. `test_adv_bridge_disconnects_during_warm_runs` -> False Positive: trả về `'PASS'` và `'Verified Live'`
  9. `test_adv_partial_warm_failures_tracked` -> Failure Masking: `failureCount = 0` thay vì `3`
  10. Các test payload dị dạng văng `TypeError` / `AttributeError`.

### 1.5. Bằng chứng kiểm tra độc lập các thành phần hợp lệ
- **Zero Touch Revit PID 26264:**
  - Lệnh `run-benchmark.ps1 -WarmRuns 5 -ColdRuns 1` thực thi độc lập.
  - Tạo sandbox tại `C:\Users\STR-HP03\AppData\Local\Temp\HPDynamoBenchmark\Session_3d8d5a4e00a04e489943d9b97ef37479`.
  - Kết thúc tiến trình, sandbox bị xóa hoàn toàn (`Test-Path` trả về `False`).
  - Không có tiến trình Revit nào bị kill hoặc bị can thiệp.
- **100% Test Suites Độc Lập:**
  - `python HPDynamo/tools/harness/test_benchmark_session.py`: 11 Passed.
  - `dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests`: 139 Passed.
  - `dotnet test HPDynamo/HPDynamo.McpBridge.Tests`: 223 Passed.
  - `dotnet test HPDynamo/HPDynamo.E2E.Tests`: 209 Passed.
  - `dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests`: 115 Passed.
  - `dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests`: 915 Passed.
  - **Tổng cộng: 1,612 tests đạt 100% PASS.**
- **Clean Code Governance:**
  - `run-benchmark.ps1`: 217 dòng (ngưỡng 300).
  - `benchmark_session.py`: 255 dòng (ngưỡng 300).
  - `test_benchmark_session.py`: 123 dòng (ngưỡng 300).
  - Mọi hàm $\le 50$ dòng, 0 dead code, 0 unannotated empty catch.

---

## 2. Logic Chain (Chuỗi Lập Luận)

1. Từ **Quan sát 1.1**: Mục tiêu cốt lõi của Milestone C theo `ORIGINAL_REQUEST.md §R3 (R2-08)` và `SCOPE.md` là:
   *"Harness Benchmark phân tích lỗi trung thực... tuyệt đối không đánh dấu Success cho các lần chạy bị lỗi... loại bỏ hoàn toàn false positive 'Passed' hay 'Verified Live'"*.
2. Báo cáo của `worker_c` khẳng định: *"Loại bỏ toàn bộ các chuỗi giả định thành công... thỏa mãn 100% Acceptance Criteria"*.
3. Tuy nhiên, việc kiểm tra mã nguồn thực tế tại dòng 166–178 chứng minh: nếu một công cụ vượt qua cold run nhưng bridge gặp sự cố, crash hoặc ngắt kết nối trong suốt 10 lần warm runs, động cơ đo lường hoàn toàn phớt lờ lỗi, nuốt mảng mẫu đo rỗng, và gắn nhãn `"Verified Live (Persistent Session)"` với trạng thái `"PASS"`.
4. Từ **Quan sát 1.2**: Hàm `evaluate_outcome` mặc định trả về `"PASS"` cho các phản hồi rỗng hoặc dị dạng mà không kiểm tra cấu trúc `result.content`.
5. Từ **Quan sát 1.3**: Biểu thức tính `"failureCount"` dựa vào chuỗi nhãn `"FAILED"` khiến tỷ lệ lỗi suy giảm trong phiên bị che giấu hoàn toàn.
6. Từ **Quan sát 1.4**: Bộ test đối kháng `test_adversary_benchmark.py` ghi nhận 12/17 failures, chứng minh khách quan các kịch bản lỗi trên xảy ra thực tế chứ không phải trên lý thuyết.
7. Theo nguyên tắc Forensic Integrity: *"Trust nothing — verify everything. If ANY check fails, your verdict is INTEGRITY VIOLATION and you MUST reject the work product."*
8. Do đó, sản phẩm chứa lỗi toàn vẹn (Integrity Violation) dạng **False-Positive Masking** và **Facade Evaluation**, buộc phải đưa ra phán quyết bác bỏ (**INTEGRITY VIOLATION**).

---

## 3. Caveats (Ranh Giới & Giả Định)

1. **Điều kiện Offline hiện tại:** Trong môi trường thử nghiệm khi Revit Bridge offline ngay từ đầu (cold run), `benchmark_session.py` phân loại đúng `CHƯA TEST (Bridge Offline)` và không sinh báo cáo giả. Khiếm khuyết chỉ bộc lộ khi cold run thành công nhưng warm loop bị lỗi (intermittent/degraded), hoặc khi bridge disconnect giữa chừng. Tuy nhiên, một bộ harness đo lường trung thực (Honest Benchmark) bắt buộc phải bảo đảm tính toàn vẹn trong mọi kịch bản.
2. **Các thành phần khác hoàn toàn sạch:** Toàn bộ 5 bộ test .NET (1,601 tests), logic sandbox isolation, bảo vệ Zero-Touch PID 26264, và Clean Code Governance của repo đều đạt 100% chuẩn mực và không có vi phạm.

---

## 4. Conclusion (Kết Luận & Yêu Cầu Khắc Phục)

**Phán quyết:** **INTEGRITY VIOLATION** (Bác bỏ sản phẩm bàn giao của Milestone C).

### Danh mục yêu cầu sửa đổi (Actionable Remediation):
`worker_c` phải cập nhật `benchmark_session.py` (ở cả 2 thư mục `HPDynamo/tools/harness/` và `HPDynamo/tools/benchmark/`):
1. **Phòng thủ kiểu dữ liệu & Chặn False Positive trong `evaluate_outcome`:**
   - Kiểm tra `isinstance(resp, dict)`.
   - Kiểm tra an toàn cho `"error"`: không gọi `.get()` trên `str` hoặc `None`.
   - Bắt buộc kiểm tra `"result"` tồn tại, là `dict`, có `content` là `list` với độ dài $> 0$.
   - Kiểm tra `joined.strip()` không được rỗng trước khi công nhận `PASS`.
2. **Fail-Fast & Không False Positive trong `execute_workload`:**
   - Theo dõi số lần warm thất bại: `warm_failures`.
   - Nếu bridge disconnect trong warm runs: dừng vòng lặp (`break`) và trả về `SKIP` (`CHƯA TEST (Bridge Offline During Warm)`).
   - Nếu `warm_runs > 0` và `len(warm_samples) == 0`: toàn bộ warm runs thất bại $\rightarrow$ bắt buộc trả về `FAIL` (`FAILED (All warm runs failed)`), cấm trả về `PASS` hoặc `Verified Live`.
   - Tham số hóa `failureCount` trong `build_workload_record` bằng số lỗi thực tế (`cold_fail + warm_failures`).
3. **Tiêu chuẩn nghiệm thu:**
   - Chạy `python HPDynamo/tools/harness/test_adversary_benchmark.py` đạt **17/17 PASSED**.
   - Chạy `python HPDynamo/tools/harness/test_benchmark_session.py` tiếp tục đạt **11/11 PASSED**.
   - Chạy lại `run-benchmark.ps1` kiểm chứng output.

---

## 5. Verification Method (Phương Pháp Kiểm Chứng Độc Lập)

1. **Tái hiện lỗi vi phạm tính toàn vẹn:**
   ```bash
   python HPDynamo/tools/harness/test_adversary_benchmark.py
   ```
   *Hiện trạng:* 12/17 test cases FAILED.
2. **Kiểm chứng các bộ test suites chuẩn:**
   ```bash
   python HPDynamo/tools/harness/test_benchmark_session.py
   dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests
   dotnet test HPDynamo/HPDynamo.McpBridge.Tests
   dotnet test HPDynamo/HPDynamo.E2E.Tests
   (cd McpShared && dotnet test HPRebar.McpBridge.Core.Net48Tests)
   (cd McpShared && dotnet test HPRebar.Mcp.Server.Core.Tests)
   ```
   *Kết quả:* 100% tests PASSED.
3. **Kiểm chứng harness sandbox và Zero Touch:**
   ```powershell
   powershell -NoProfile -File HPDynamo/tools/harness/run-benchmark.ps1 -WarmRuns 5 -ColdRuns 1
   ```
   *Kết quả:* Zero Touch Shield active, sandbox created and cleanly destroyed, exit code 0.
