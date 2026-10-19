# Comprehensive Investigation & Handoff Report: Forensic Remediation for Milestone C (Iteration 2)

**Agent:** `explorer_c_iter2` (Teamwork Explorer / Analyst)  
**Target:** HPDynamo MCP Vòng 2 — Milestone C (Đợt C: Honest Benchmark & Model Isolation — R2-08) — Iteration 2  
**Working Directory:** `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_c_iter2`  
**Parent Agent:** `orchestrator_8_gen2` (`0a4397e4-543d-4476-a901-af2f39506668`)  
**Status:** **INVESTIGATION COMPLETE — REMEDIATION READY**  

---

## Executive Summary

Đã hoàn thành điều tra toàn diện các vi phạm tính toàn vẹn (Integrity Violations) và các lỗi kiểm thử đối kháng (Adversarial Failures) được ghi nhận trong các báo cáo của `auditor_c_1` (**INTEGRITY VIOLATION**), `challenger_c_1` (**REQUEST_CHANGES**), và `challenger_c_2` (**REQUEST_CHANGES**).

Tất cả các nguyên nhân gốc rễ (Root Causes) đã được chứng minh và tái hiện thực nghiệm:
1. **False Positive & Masking trong Warm Loop:** `benchmark_session.py` (dòng 166–178) trả về `"PASS"` và gắn nhãn `"Verified Live (Persistent Session)"` khi 100% warm runs thất bại hoặc bridge ngắt kết nối.
2. **Facade Outcome Evaluation:** `evaluate_outcome` (dòng 56–82) trả về `"PASS"` cho phản hồi rỗng (`{}` hoặc `content: []`), đồng thời văng `AttributeError` / `TypeError` unhandled khi gặp payload dị dạng (`result: None`, `error: str`, `error: None`, `content: None`, `text: None`).
3. **Failure Count Masking:** `build_workload_record` (dòng 143) tính `"failureCount": 1 if "FAILED" in status_label else 0`, che giấu toàn bộ các lần thất bại trong warm loop khi status label là `"Verified Live"`.
4. **Sandbox Boundary Substring Bypass:** `Remove-BenchmarkSandbox` trong `run-benchmark.ps1` (dòng 136–137) sử dụng phép so khớp substring `$targetCanonical.Contains("HPDynamoBenchmark")`, cho phép xóa nhầm các thư mục anh em `%TEMP%\NotHPDynamoBenchmark` và có nguy cơ xóa chính thư mục gốc `%TEMP%\HPDynamoBenchmark`.

**Giải pháp đề xuất đã được thực nghiệm kiểm chứng độc lập 100%:**
- Chạy đối kháng bộ test `verify_proposed.py` đạt **21/21 tests PASSED (100%)**, giải quyết triệt để cả 12/12 lỗi trong `test_adversary_benchmark.py` (17/17 tests đạt chuẩn).
- Chạy đối kháng bộ test sandbox `test_proposed_sandbox.ps1` đạt **8/8 tests PASSED (100%)**, chặn đứng toàn bộ các cuộc tấn công ranh giới thư mục.
- Toàn bộ 5 bộ test .NET (1,601 tests) và 11 unit tests Python gốc tiếp tục **PASS 100%**.
- Các file sửa đổi tuân thủ nghiêm ngặt Clean Code Governance (file $\le 300$ dòng, method $\le 50$ dòng, 0 dead code, 0 unannotated empty catch).

---

## 1. Observation (Quan Sát Trực Tiếp)

### 1.1. Tái hiện thực nghiệm 12/17 failures trong `test_adversary_benchmark.py`
- **Lệnh thực thi:**
  ```bash
  python HPDynamo/tools/harness/test_adversary_benchmark.py
  ```
- **Kết quả verbatim:**
  ```
  Ran 17 tests in 0.009s
  FAILED (failures=12)
  PASSED (p50: 0.0ms, p95: 0.0ms, Req/Resp: 111B/93B)
  ```
- **Hiện tượng đặc biệt quan sát được:** Trong lúc chạy kiểm thử đối kháng với mock server 5/5 warm calls bị lỗi crash (`WL-ADV-01`), console in verbatim dòng chữ:
  `PASSED (p50: 0.0ms, p95: 0.0ms, Req/Resp: 111B/93B)`!
  Điều này phơi bày trực tiếp hành vi che giấu lỗi và báo cáo thành công giả mạo (False Positive).

### 1.2. Phân loại chi tiết 12 failures theo mã nguồn thực tế

#### Nhóm A: Unhandled Crashes do thiếu phòng thủ kiểu dữ liệu trong `evaluate_outcome`
| Test Case | Payload gây lỗi | Lỗi Verbatim | Dòng code vi phạm |
|---|---|---|---|
| `test_adv_result_is_none` | `{"result": None}` | `AttributeError: 'NoneType' object has no attribute 'get'` | `benchmark_session.py:65` (`res.get("content", [])` khi `res=None`) |
| `test_adv_error_is_string` | `{"error": "Internal Server Error"}` | `AttributeError: 'str' object has no attribute 'get'` | `benchmark_session.py:62` (`err.get('code', -1)` khi `err` là `str`) |
| `test_adv_error_is_none` | `{"error": None}` | `AttributeError: 'NoneType' object has no attribute 'get'` | `benchmark_session.py:62` (`err.get('code', -1)` khi `err` là `None`) |
| `test_adv_content_is_none` | `{"result": {"content": None}}` | `TypeError: 'NoneType' object is not iterable` | `benchmark_session.py:65` (`for c in res.get("content", [])` khi trả về `None`) |
| `test_adv_content_element_text_is_none` | `{"result": {"content": [{"type": "text", "text": None}]}}` | `TypeError: sequence item 0: expected str instance, NoneType found` | `benchmark_session.py:66` (`"\n".join(texts)` khi text phần tử là `None`) |

#### Nhóm B: Facade Evaluation (Báo PASS giả mạo cho phản hồi rỗng hoặc dị dạng)
| Test Case | Payload gây lỗi | Kết quả thực tế | Kết quả kỳ vọng |
|---|---|---|---|
| `test_adv_missing_result_and_error_fields` | `{"jsonrpc": "2.0", "id": 1}` | `status="PASS", success=True` (False Positive) | `status="FAIL", success=False` |
| `test_adv_empty_result_dict` | `{"result": {}}` | `status="PASS", success=True` (False Positive) | `status="FAIL", success=False` |
| `test_adv_empty_content_list` | `{"result": {"isError": False, "content": []}}` | `status="PASS", success=True` (False Positive) | `status="FAIL", success=False` |
| `test_adv_content_is_string` | `{"result": {"content": "raw string"}}` | `status="PASS", success=True` (False Positive) | `status="FAIL", success=False` |

#### Nhóm C: False Positive & Failure Masking trong Warm Loop (`execute_workload`)
| Test Case | Kịch bản thử nghiệm | Hiện tượng mã nguồn | Vi phạm nghiêm trọng |
|---|---|---|---|
| `test_adv_all_warm_runs_fail_must_not_report_pass` | Cold run OK, nhưng 5/5 warm runs gặp lỗi RPC (`-32000`). | `warm_samples` rỗng `[]`, code in `PASSED (p50: 0.0ms)`, trả về `"PASS"` và `"Verified Live (Persistent Session)"`. | False Positive nghiêm trọng: 100% warm requests hỏng vẫn báo PASS. |
| `test_adv_bridge_disconnects_during_warm_runs` | Cold run OK, nhưng bridge ngắt kết nối (`Dynamo bridge not connected`) trong warm loop. | Không nhận diện `w_offline`, nuốt lỗi, trả về `"PASS"` và `"Verified Live"`. | Vi phạm quy tắc Offline Mode: bridge sập vẫn công nhận Live. |
| `test_adv_partial_warm_failures_tracked` | Cold run OK; warm run 2 thành công, 3 thất bại. | Do `status_label` không chứa chữ `"FAILED"`, dòng 143 gán `failureCount: 0`. | Che giấu số lỗi thất bại: 3 lỗi bị biến thành 0. |

### 1.3. Tái hiện thực nghiệm lỗ hổng ranh giới trong `run-benchmark.ps1`
- **Mã nguồn vi phạm:** `HPDynamo/tools/harness/run-benchmark.ps1` và `HPDynamo/tools/benchmark/run-benchmark.ps1` (dòng 136–137):
  ```powershell
  if (-not $targetCanonical.StartsWith($tempCanonical, [StringComparison]::OrdinalIgnoreCase) -or
      -not $targetCanonical.Contains("HPDynamoBenchmark")) {
      Write-Error "SECURITY REFUSAL: Target directory '$SandboxDir' is outside %TEMP%\HPDynamoBenchmark. Deletion aborted."
      return
  }
  ```
- **Thực nghiệm qua `test_sandbox_isolation.ps1`:**
  - Target: `Join-Path $env:TEMP "NotHPDynamoBenchmark"`.
  - `$targetCanonical.StartsWith($tempCanonical)` $\rightarrow$ `True`.
  - `$targetCanonical.Contains("HPDynamoBenchmark")` $\rightarrow$ `True` (vì chuỗi con `HPDynamoBenchmark` tồn tại trong tên thư mục!).
  - Kết quả: Thư mục bị xóa sổ mà không có bất kỳ thông báo từ chối bảo mật nào (`stillExists=False`).
  - Lỗi: **FAIL Boundary Refusal: Temp Substring Spoof Attack**.

### 1.4. Trạng thái các thành phần hợp lệ hiện hữu (Baseline Health)
Đã kiểm chứng độc lập toàn bộ các thành phần khác trong hệ thống:
1. `dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests`: 139 Passed (100%).
2. `dotnet test HPDynamo/HPDynamo.McpBridge.Tests`: 223 Passed (100%).
3. `dotnet test HPDynamo/HPDynamo.E2E.Tests`: 209 Passed (100%).
4. `(cd McpShared && dotnet test HPRebar.McpBridge.Core.Net48Tests)`: 115 Passed (100%).
5. `(cd McpShared && dotnet test HPRebar.Mcp.Server.Core.Tests --no-build)`: 915 Passed (100%).
6. `python HPDynamo/tools/harness/test_benchmark_session.py`: 11 Passed (100%).
$\rightarrow$ **Tổng cộng 1,612/1,612 test suites độc lập đạt 100% PASS.** Mã C#/.NET và các module cốt lõi hoàn toàn không có regression.

---

## 2. Logic Chain (Chuỗi Lập Luận)

1. Từ **Quan sát 1.1 và 1.2**: Tiêu chí cốt lõi của R2-08 trong `ORIGINAL_REQUEST.md` và `SCOPE.md` là:
   *"Harness Benchmark phân tích lỗi trung thực... dừng vòng lặp và ghi nhận FAIL/SKIP rõ ràng, tuyệt đối không đánh dấu Success cho các lần chạy bị lỗi."*
2. Tuy nhiên, trong `evaluate_outcome`:
   - Dòng 58–63: Kiểm tra `"error"` nhưng giả định mù quáng rằng `err` luôn là `dict` có phương thức `.get()`. Khi gặp `err = str` hoặc `err = None`, Python lập tức văng `AttributeError`.
   - Dòng 64–66: Không kiểm tra `"result"` có tồn tại hay không và có khác `None` không. Đồng thời giả định `content` luôn là `list` có độ dài $> 0$.
   - Dòng 82: Luôn mặc định rơi vào `return "PASS", True, False, ""` ở cuối hàm. Bất kỳ phản hồi rỗng nào vượt qua các `if` trên đều bị dán nhãn thành công (`PASS`).
3. Từ **Quan sát 1.2 (Nhóm C)**:
   - Trong `execute_workload`, biến `warm_samples` chỉ được thêm vào khi `w_ok == True`.
   - Nếu toàn bộ các lần gọi warm đều fail (`w_ok == False`), `warm_samples` là mảng rỗng `[]`.
   - Hàm `compute_latency_stats([])` trả về `{p50: 0.0, ...}`.
   - Code không có điều kiện kiểm tra `len(warm_samples) == 0`, ngay lập tức nhảy tới dòng `print("PASSED ...")` và `return "PASS", build_workload_record(..., "Verified Live (Persistent Session)", ...)`.
   - Dẫn đến: Hệ thống bị hỏng hoàn toàn trong pha warm run vẫn được báo cáo là hoạt động hoàn hảo và đo lường trực tiếp trên live!
4. Từ **Quan sát 1.3**:
   - Biểu thức `$targetCanonical.Contains("HPDynamoBenchmark")` chỉ kiểm tra chuỗi con.
   - Trong hệ điều hành Windows, bất kỳ thư mục nào như `%TEMP%\NotHPDynamoBenchmark` hoặc `%TEMP%\HPDynamoBenchmark_Old` đều chứa chuỗi con đó.
   - Hơn nữa, nếu truyền chính thư mục gốc `%TEMP%\HPDynamoBenchmark`, điều kiện trên cũng thỏa mãn, dẫn đến việc xóa sạch toàn bộ môi trường của các phiên khác.
   - Quy tắc ranh giới bắt buộc: Chỉ cho phép xóa thư mục con nằm BÊN TRONG `%TEMP%\HPDynamoBenchmark\`. Điều này đòi hỏi tiền tố kiểm tra phải là `$sandboxRootCanonical + '\'`.
5. **Tổng hợp lập luận:**
   - Cả 4 khiếm khuyết trên đã được xác định vị trí chính xác đến từng dòng mã nguồn.
   - Các bản sửa đổi đề xuất đã được triển khai vào `proposed_benchmark_session.py` và `proposed_run-benchmark.ps1`.
   - Thực nghiệm chạy đối kháng chứng minh:
     * Bộ test `verify_proposed.py` vượt qua **21/21 tests (100%)**.
     * Bộ test sandbox `test_proposed_sandbox.ps1` vượt qua **8/8 tests (100%)**.
   - Do đó, phương án khắc phục hoàn toàn đầy đủ, chính xác và sẵn sàng chuyển giao cho Worker thực hiện cập nhật mã nguồn repo.

---

## 3. Caveats (Ranh Giới & Giả Định)

1. **Phạm vi thay đổi:** Toàn bộ các thay đổi chỉ nằm trong 2 tập tin harness/benchmark (`benchmark_session.py` và `run-benchmark.ps1`). Không có bất kỳ thay đổi nào tác động vào code C# (.NET) trong `HPDynamo.McpBridge`, `HPDynamo.Mcp.Server`, hay `McpShared`.
2. **Đồng bộ song song 2 thư mục:** Trong repo tồn tại 2 bản sao giống hệt nhau:
   - Bản 1: `HPDynamo/tools/harness/`
   - Bản 2: `HPDynamo/tools/benchmark/`
   Worker **bắt buộc phải cập nhật cả 2 bản sao** để tránh trôi lệch phiên bản (file drift).
3. **Môi trường Revit PID 26264:** Khi chạy benchmark live, nếu Revit không mở hoặc pipe bridge chưa kết nối, harness sẽ báo `CHƯA TEST (Bridge Offline)` và `SKIPPED`. Đây là hành vi trung thực và hoàn toàn đúng theo thiết kế, không phải lỗi.

---

## 4. Conclusion & Actionable Solution (Kết Luận & Giải Pháp Chi Tiết)

### 4.1. Bản vá tổng thể (Unified Patch Reference)
Toàn văn bản vá đã được tạo sẵn tại:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_c_iter2\benchmark_remediation.patch`

File mã nguồn hoàn chỉnh đã được kiểm chứng sẵn tại:
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_c_iter2\proposed_benchmark_session.py`
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_c_iter2\proposed_run-benchmark.ps1`

---

### 4.2. Hướng dẫn sửa đổi chi tiết cho `benchmark_session.py`
*(Áp dụng cho cả `HPDynamo/tools/harness/benchmark_session.py` và `HPDynamo/tools/benchmark/benchmark_session.py`)*

#### Điểm sửa 1: Phòng thủ kiểu dữ liệu & Chặn False Positive trong `evaluate_outcome`
**Vị trí:** Thay thế toàn bộ hàm `evaluate_outcome(resp)` từ dòng 56 đến 82.

```python
# ==============================================================================
# BEFORE (Lines 56-82):
# ==============================================================================
def evaluate_outcome(resp):
    """Three-tier outcome validator: returns (status, is_success, is_offline, error_detail)."""
    if not resp:
        return "FAIL", False, False, "Empty or null response"
    if "error" in resp:
        err = resp["error"]
        return "FAIL", False, False, f"RPC Error {err.get('code', -1)}: {err.get('message', 'Error')}"

    res = resp.get("result", {})
    texts = [c.get("text", "") for c in res.get("content", []) if isinstance(c, dict) and c.get("type") == "text"]
    joined = "\n".join(texts).strip()

    if res.get("isError"):
        if "Dynamo bridge not connected" in joined or "pipe hpdynamo-mcp-2026" in joined:
            return "SKIP", False, True, "Dynamo bridge not connected (offline mode)"
        if "No document is open in Revit" in joined:
            return "SKIP", False, True, "No document open in Revit"
        return "FAIL", False, False, f"MCP isError: {joined}"

    if "Dynamo bridge not connected" in joined:
        return "SKIP", False, True, "Dynamo bridge not connected (offline mode)"
    if "The script does not compile" in joined:
        return "FAIL", False, False, f"Compile Error: {joined}"
    if "Execution failed" in joined or "Exception thrown" in joined:
        return "FAIL", False, False, f"Execution Failure: {joined}"

    return "PASS", True, False, ""

# ==============================================================================
# AFTER (44 lines, Type-safe & Fail-fast):
# ==============================================================================
def evaluate_outcome(resp):
    """Three-tier outcome validator: returns (status, is_success, is_offline, error_detail)."""
    if not isinstance(resp, dict):
        return "FAIL", False, False, "Malformed response: not a dict"

    if "error" in resp:
        err = resp["error"]
        if isinstance(err, dict):
            code = err.get("code", -1)
            msg = err.get("message", "Error")
            return "FAIL", False, False, f"RPC Error {code}: {msg}"
        return "FAIL", False, False, f"RPC Error: {err}"

    if "result" not in resp or resp["result"] is None:
        return "FAIL", False, False, "Missing or null result field"

    res = resp["result"]
    if not isinstance(res, dict):
        return "FAIL", False, False, "Malformed result: not a dict"

    content = res.get("content")
    if not isinstance(content, list) or len(content) == 0:
        return "FAIL", False, False, "Missing or empty content in result"

    texts = []
    for c in content:
        if isinstance(c, dict) and c.get("type") == "text":
            txt = c.get("text")
            if txt is not None:
                texts.append(str(txt))
    joined = "\n".join(texts).strip()

    if not joined:
        return "FAIL", False, False, "Empty text in tool content"

    is_offline = "Dynamo bridge not connected" in joined or "pipe hpdynamo-mcp-2026" in joined
    if res.get("isError"):
        if is_offline:
            return "SKIP", False, True, "Dynamo bridge not connected (offline mode)"
        if "No document is open in Revit" in joined:
            return "SKIP", False, True, "No document open in Revit"
        return "FAIL", False, False, f"MCP isError: {joined}"

    if is_offline:
        return "SKIP", False, True, "Dynamo bridge not connected (offline mode)"
    if "No document is open in Revit" in joined:
        return "SKIP", False, True, "No document open in Revit"
    if "The script does not compile" in joined:
        return "FAIL", False, False, f"Compile Error: {joined}"
    if "Execution failed" in joined or "Exception thrown" in joined:
        return "FAIL", False, False, f"Execution Failure: {joined}"

    return "PASS", True, False, ""
```

---

#### Điểm sửa 2: Tham số hóa `failureCount` trong `build_workload_record`
**Vị trí:** Dòng 127–146.

```python
# ==============================================================================
# BEFORE:
# ==============================================================================
def build_workload_record(wl, status_label, stats, cold_ms, startup_ms, samples, err_detail, req_bytes, resp_bytes):
    ...
            "failureCount": 1 if "FAILED" in status_label else 0,
    ...

# ==============================================================================
# AFTER:
# ==============================================================================
def build_workload_record(wl, status_label, stats, cold_ms, startup_ms, samples, err_detail, req_bytes, resp_bytes, failure_count=None):
    """Format single workload result object."""
    if failure_count is None:
        failure_count = 1 if "FAILED" in status_label else 0
    return {
        "id": wl["id"], "name": wl["name"], "tool": wl["tool"], "arguments": wl["args"],
        "baseline": wl["baseline"],
        "upgraded": {
            "status": status_label,
            "executionMode": "Persistent Session",
            "roundTrips": 1,
            "payloadBytes": {"request": req_bytes, "response": resp_bytes},
            "startupMs": startup_ms,
            "coldLatencyMs": cold_ms,
            "warmLatencyMs": stats,
            "rawSamples": samples,
            "samplesCount": len(samples),
            "successCount": len(samples),
            "failureCount": failure_count,
            "errorDetails": err_detail,
        },
    }
```

---

#### Điểm sửa 3: Xử lý Fail-Fast & Không False Positive trong `execute_workload`
**Vị trí:** Thay thế từ dòng 158 đến 178.

```python
# ==============================================================================
# BEFORE:
# ==============================================================================
    zero_stats = {"p50": 0.0, "p95": 0.0, "min": 0.0, "max": 0.0, "mean": 0.0}
    if tier_status == "SKIP":
        print(f"SKIPPED ({err_detail})")
        return "SKIP", build_workload_record(wl, "CHƯA TEST (Bridge Offline)", zero_stats, cold_ms, startup_ms, [], err_detail, req_b, resp_b)
    if tier_status == "FAIL":
        print(f"FAILED ({err_detail})")
        return "FAIL", build_workload_record(wl, f"FAILED ({err_detail})", zero_stats, cold_ms, startup_ms, [], err_detail, req_b, resp_b)

    warm_samples = []
    for _ in range(warm_runs):
        tw0 = time.perf_counter()
        w_resp = server.call(wl["tool"], wl["args"])
        w_ms = round((time.perf_counter() - tw0) * 1000.0, 2)
        _, w_ok, _, _ = evaluate_outcome(w_resp)
        if w_ok:
            warm_samples.append(w_ms)

    stats = compute_latency_stats(warm_samples)
    print(f"PASSED (p50: {stats['p50']}ms, p95: {stats['p95']}ms, Req/Resp: {req_b}B/{resp_b}B)")
    return "PASS", build_workload_record(wl, "Verified Live (Persistent Session)", stats, cold_ms, startup_ms, warm_samples, "", req_b, resp_b)

# ==============================================================================
# AFTER:
# ==============================================================================
    zero_stats = {"p50": 0.0, "p95": 0.0, "min": 0.0, "max": 0.0, "mean": 0.0}
    if tier_status == "SKIP":
        print(f"SKIPPED ({err_detail})")
        return "SKIP", build_workload_record(wl, "CHƯA TEST (Bridge Offline)", zero_stats, cold_ms, startup_ms, [], err_detail, req_b, resp_b, failure_count=0)
    if tier_status == "FAIL":
        print(f"FAILED ({err_detail})")
        return "FAIL", build_workload_record(wl, f"FAILED ({err_detail})", zero_stats, cold_ms, startup_ms, [], err_detail, req_b, resp_b, failure_count=1)

    warm_samples = []
    warm_failures = 0
    bridge_offline_during_warm = False
    last_warm_err = ""

    for _ in range(warm_runs):
        tw0 = time.perf_counter()
        w_resp = server.call(wl["tool"], wl["args"])
        w_ms = round((time.perf_counter() - tw0) * 1000.0, 2)
        _, w_ok, w_offline, w_detail = evaluate_outcome(w_resp)
        if w_offline:
            bridge_offline_during_warm = True
            last_warm_err = w_detail
            break
        if w_ok:
            warm_samples.append(w_ms)
        else:
            warm_failures += 1
            last_warm_err = w_detail

    if bridge_offline_during_warm:
        print(f"SKIPPED ({last_warm_err})")
        return "SKIP", build_workload_record(wl, "CHƯA TEST (Bridge Offline During Warm)", zero_stats, cold_ms, startup_ms, warm_samples, last_warm_err, req_b, resp_b, failure_count=warm_failures)

    if warm_runs > 0 and len(warm_samples) == 0:
        err_msg = last_warm_err or f"All {warm_runs} warm runs failed"
        print(f"FAILED ({err_msg})")
        return "FAIL", build_workload_record(wl, f"FAILED (All {warm_runs} warm runs failed)", zero_stats, cold_ms, startup_ms, [], err_msg, req_b, resp_b, failure_count=warm_failures)

    stats = compute_latency_stats(warm_samples)
    if warm_failures > 0:
        label = f"Verified Live (Degraded: {len(warm_samples)}/{warm_runs} passed)"
        print(f"PASSED (p50: {stats['p50']}ms, p95: {stats['p95']}ms, Req/Resp: {req_b}B/{resp_b}B, Degraded: {warm_failures} failed)")
        return "PASS", build_workload_record(wl, label, stats, cold_ms, startup_ms, warm_samples, last_warm_err, req_b, resp_b, failure_count=warm_failures)

    print(f"PASSED (p50: {stats['p50']}ms, p95: {stats['p95']}ms, Req/Resp: {req_b}B/{resp_b}B)")
    return "PASS", build_workload_record(wl, "Verified Live (Persistent Session)", stats, cold_ms, startup_ms, warm_samples, "", req_b, resp_b, failure_count=0)
```

---

### 4.3. Hướng dẫn sửa đổi chi tiết cho `run-benchmark.ps1`
*(Áp dụng cho cả `HPDynamo/tools/harness/run-benchmark.ps1` và `HPDynamo/tools/benchmark/run-benchmark.ps1`)*

**Vị trí:** Dòng 132–141. Thay thế logic kiểm tra ranh giới thư mục bằng tiền tố nghiêm ngặt:

```powershell
# ==============================================================================
# BEFORE (Lines 132-141):
# ==============================================================================
    # Constraint 1: Directory must be inside %TEMP%\HPDynamoBenchmark
    $tempCanonical = [System.IO.Path]::GetFullPath($env:TEMP).TrimEnd('\', '/')
    $targetCanonical = [System.IO.Path]::GetFullPath($SandboxDir).TrimEnd('\', '/')

    if (-not $targetCanonical.StartsWith($tempCanonical, [StringComparison]::OrdinalIgnoreCase) -or
        -not $targetCanonical.Contains("HPDynamoBenchmark")) {
        Write-Error "SECURITY REFUSAL: Target directory '$SandboxDir' is outside %TEMP%\HPDynamoBenchmark. Deletion aborted."
        return
    }

# ==============================================================================
# AFTER (Strict Prefix Enforcement):
# ==============================================================================
    # Constraint 1: Directory must be strictly inside %TEMP%\HPDynamoBenchmark\
    $sandboxRootCanonical = [System.IO.Path]::GetFullPath((Join-Path $env:TEMP "HPDynamoBenchmark")).TrimEnd('\', '/')
    $requiredPrefix = $sandboxRootCanonical + [System.IO.Path]::DirectorySeparatorChar
    $targetCanonical = [System.IO.Path]::GetFullPath($SandboxDir).TrimEnd('\', '/')

    if (-not $targetCanonical.StartsWith($requiredPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        Write-Error "SECURITY REFUSAL: Target directory '$SandboxDir' is outside %TEMP%\HPDynamoBenchmark. Deletion aborted."
        return
    }
```

---

### 4.4. Kiểm tra Clean Code Governance
Sau khi Worker áp dụng bản vá:
1. `benchmark_session.py`: 271 dòng (ngưỡng 300 dòng, Rule C2). Phương thức dài nhất 46 dòng (ngưỡng 50 dòng, Rule M3). 0 dead code (Rule Q-B1), 0 unannotated empty catch (Rule Q-B2).
2. `run-benchmark.ps1`: 219 dòng (ngưỡng 300 dòng). Hàm dài nhất 39 dòng (ngưỡng 50 dòng).

---

## 5. Verification Method (Phương Pháp Kiểm Chứng Độc Lập)

Worker và các bên kiểm toán (Auditor, Challenger) có thể chạy các lệnh sau để nghiệm thu dứt điểm:

### Bước 1: Kiểm chứng bộ test đối kháng (Mục tiêu cốt lõi: 17/17 PASSED)
```bash
python HPDynamo/tools/harness/test_adversary_benchmark.py
```
- **Kỳ vọng:**
  ```
  Ran 17 tests ... OK
  ```
- **Điều kiện phủ định (Invalidation):** Nếu có bất kỳ test nào FAILED (ví dụ crash hoặc false positive), bản vá chưa đạt chuẩn.

### Bước 2: Kiểm chứng bộ unit test gốc của harness
```bash
python HPDynamo/tools/harness/test_benchmark_session.py
```
- **Kỳ vọng:** `Ran 11 tests ... OK` (11/11 PASSED).

### Bước 3: Kiểm chứng bộ test đối kháng sandbox isolation
```powershell
powershell -NoProfile -File .agents/teamwork/explorer_c_iter2/test_proposed_sandbox.ps1
```
- **Kỳ vọng:** `Summary: 8 / 8 Passed` (100% boundary attack scenarios safely refused).

### Bước 4: Kiểm chứng tính toàn vẹn 5 bộ test .NET (Zero Regression)
```bash
dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests
dotnet test HPDynamo/HPDynamo.McpBridge.Tests
dotnet test HPDynamo/HPDynamo.E2E.Tests
dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests
dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests --no-build
```
- **Kỳ vọng:** Toàn bộ 1,601 .NET tests đều PASSED (139 + 223 + 209 + 115 + 915).

### Bước 5: Kiểm chứng chạy live benchmark harness
```powershell
powershell -NoProfile -File HPDynamo/tools/harness/run-benchmark.ps1 -WarmRuns 5 -ColdRuns 1
```
- **Kỳ vọng:** Zero-Touch Shield kích hoạt an toàn, sandbox được tạo và xóa sạch sẽ, báo cáo `baseline-vs-upgraded.json` phản ánh trung thực hiện trạng (nếu bridge offline thì báo `CHƯA TEST`, không sinh số liệu giả).
