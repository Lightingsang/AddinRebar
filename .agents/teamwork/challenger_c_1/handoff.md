# Handoff Report: Adversarial Stress Testing — Milestone C (Benchmark Engine)

**Agent:** `challenger_c_1`  
**Role:** Empirical Challenger (critic, specialist)  
**Date:** 2026-10-09  
**Verdict:** **REQUEST_CHANGES**  
**Working Directory:** `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_c_1`  
**Code Under Test:** `HPDynamo/tools/harness/benchmark_session.py` (and duplicate in `HPDynamo/tools/benchmark/benchmark_session.py`)  
**Test Suite Created:** `HPDynamo/tools/harness/test_adversary_benchmark.py` (17 tests, 12 failures)

---

## 1. Observation (Quan Sát Trực Tiếp)

Đã thiết lập bộ test đối kháng thực nghiệm `HPDynamo/tools/harness/test_adversary_benchmark.py` và trực tiếp thực thi qua lệnh:
```bash
python HPDynamo/tools/harness/test_adversary_benchmark.py
```
Kết quả ghi nhận: **Ran 17 tests: FAILED (failures=12, passed=5)**.

Cụ thể các quan sát theo dòng code và lỗi verbatim:

### 1.1. Lỗi False Positive "Verified Live" & "PASS" khi 100% warm runs thất bại
- **Vị trí code:** `HPDynamo/tools/harness/benchmark_session.py`, dòng 166–178:
  ```python
  166: warm_samples = []
  167: for _ in range(warm_runs):
  168:     tw0 = time.perf_counter()
  169:     w_resp = server.call(wl["tool"], wl["args"])
  170:     w_ms = round((time.perf_counter() - tw0) * 1000.0, 2)
  171:     _, w_ok, _, _ = evaluate_outcome(w_resp)
  172:     if w_ok:
  173:         warm_samples.append(w_ms)
  174: 
  175: stats = compute_latency_stats(warm_samples)
  176: print(f"PASSED (p50: {stats['p50']}ms, p95: {stats['p95']}ms, Req/Resp: {req_b}B/{resp_b}B)")
  177: return "PASS", build_workload_record(wl, "Verified Live (Persistent Session)", stats, cold_ms, startup_ms, warm_samples, "", req_b, resp_b)
  ```
- **Hiện tượng thực nghiệm:** Khi cold run thành công nhưng **100% warm runs bị lỗi** (ví dụ 5/5 warm calls bị exception hoặc RPC error):
  - `w_ok` luôn là `False`, `warm_samples` rỗng `[]`.
  - `stats` trả về `{p50: 0.0, p95: 0.0, ...}`.
  - Dòng 176 in verbatim ra màn hình console: `PASSED (p50: 0.0ms, p95: 0.0ms, Req/Resp: 111B/93B)`.
  - Dòng 177 trả về `status = "PASS"` và record label: `"Verified Live (Persistent Session)"`!
  - `summary.passedWorkloads` tăng lên, `failedWorkloads` là 0, và exit code của engine là 0!
  - **Verbatim Failure:**
    ```
    AssertionError: 'PASS' == 'PASS' : CRITICAL FALSE POSITIVE: execute_workload returned PASS when 5/5 warm runs failed! Record status: Verified Live (Persistent Session)
    ```

### 1.2. Lỗi False Positive khi Bridge Disconnect giữa chừng trong warm loop
- **Hiện tượng thực nghiệm:** Khi cold run thành công, nhưng tiến trình Revit/Bridge bị mất kết nối trong quá trình chạy warm runs (trả về `"Dynamo bridge not connected"`):
  - Tương tự như trên, vòng lặp không nhận diện bridge offline, nuốt toàn bộ các lần gọi thất bại.
  - Workload vẫn kết luận `"Verified Live (Persistent Session)"` với `status = "PASS"`!
  - **Verbatim Failure:**
    ```
    AssertionError: 'PASS' == 'PASS' : CRITICAL FALSE POSITIVE: Returned PASS when bridge disconnected during warm runs! Record: Verified Live (Persistent Session)
    ```

### 1.3. Che giấu số lượng lỗi thất bại trong báo cáo (Failure Count Masking)
- **Vị trí code:** `HPDynamo/tools/harness/benchmark_session.py`, dòng 143:
  ```python
  143: "failureCount": 1 if "FAILED" in status_label else 0,
  ```
- **Hiện tượng thực nghiệm:** Khi 3/5 warm calls bị lỗi và 2 calls thành công:
  - `status_label` là `"Verified Live (Persistent Session)"` (không chứa chuỗi `"FAILED"`).
  - Dẫn đến `"failureCount": 0`! 3 lần thất bại bị nuốt chửng hoàn toàn, báo cáo ghi nhận `samplesCount: 2, successCount: 2, failureCount: 0`.

### 1.4. Lỗi False Positive "PASS" khi phản hồi rỗng hoặc thiếu trường bắt buộc
- **Vị trí code:** `HPDynamo/tools/harness/benchmark_session.py`, dòng 56–82:
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
  - `resp = {"jsonrpc": "2.0", "id": 1}` (thiếu cả `result` và `error`): `evaluate_outcome` trả về `("PASS", True, False, "")`!
  - `resp = {"result": {}}` (thiếu `content`): `evaluate_outcome` trả về `("PASS", True, False, "")`!
  - `resp = {"result": {"isError": False, "content": []}}` (`content` rỗng): `evaluate_outcome` trả về `("PASS", True, False, "")`!
  - `resp = {"result": {"content": "raw string"}}`: `evaluate_outcome` trả về `("PASS", True, False, "")`!

### 1.5. Văng ngoại lệ không kiểm soát (Unhandled Crash) khi nhận payload dị dạng
- **Hiện tượng thực nghiệm:**
  - `resp = {"result": None}`: Dòng 64 gán `res = None`. Dòng 65 gọi `res.get("content", [])` $\rightarrow$ Văng `AttributeError: 'NoneType' object has no attribute 'get'`.
  - `resp = {"error": "Internal Server Error"}`: Dòng 61 gán `err = "Internal Server Error"`. Dòng 62 gọi `err.get('code', -1)` $\rightarrow$ Văng `AttributeError: 'str' object has no attribute 'get'`.
  - `resp = {"error": None}`: Dòng 62 gọi `err.get(...)` $\rightarrow$ Văng `AttributeError: 'NoneType' object has no attribute 'get'`.
  - `resp = {"result": {"content": None}}`: Dòng 65 lặp qua `None` $\rightarrow$ Văng `TypeError: 'NoneType' object is not iterable`.
  - `resp = {"result": {"content": [{"type": "text", "text": None}]}}`: Dòng 66 gọi `"\n".join([None])` $\rightarrow$ Văng `TypeError: sequence item 0: expected str instance, NoneType found`.

### 1.6. Tính toán thống kê Percentile Type 7 & Latency Stats (5/5 PASSED)
- `compute_percentile` và `compute_latency_stats` hoạt động chính xác và an toàn:
  - Mảng rỗng `[]` $\rightarrow$ trả về `0.0` / zeros.
  - `None` $\rightarrow$ trả về `0.0` / zeros.
  - 1 phần tử `[42.0]` $\rightarrow$ `p50 = 42.0, p95 = 42.0, min = 42.0, max = 42.0, mean = 42.0`.
  - Không phát sinh lỗi chia cho 0 (`ZeroDivisionError`).

---

## 2. Logic Chain (Chuỗi Lập Luận)

1. **Từ Quan sát 1.1 & 1.2:** Nhiệm vụ của Benchmark Engine là phản ánh trung thực hiện trạng vận hành. Tiêu chí cốt lõi trong `ORIGINAL_REQUEST.md §R3 (R2-08)` và `SCOPE.md` nêu rõ:
   *"tuyệt đối không đánh dấu Success cho các lần chạy bị lỗi... chứng minh không bao giờ xảy ra false positive 'Passed' hay 'Verified Live'"*.
2. Tuy nhiên, logic hiện tại trong `execute_workload` chỉ kiểm tra tính hợp lệ của `cold_resp`. Trong vòng lặp warm runs (`warm_runs`), nếu toàn bộ các lời gọi đều thất bại hoặc bridge ngắt kết nối, code chỉ đơn giản là không append vào `warm_samples`, rồi ngay lập tức in `PASSED (p50: 0.0ms...)` và trả về nhãn `"Verified Live (Persistent Session)"` với status `"PASS"`.
3. Điều này dẫn tới: Một hệ thống có bridge bị crash hoặc sập ngay sau cold run sẽ vẫn được báo cáo là **"Verified Live"** và **"PASS"** 100% trong `baseline-vs-upgraded.json`!
4. **Từ Quan sát 1.3:** Việc tính `failureCount` dựa vào điều kiện `"FAILED" in status_label` khiến mọi lỗi phát sinh trong warm loop bị giấu nhẹm (báo cáo hiển thị 0 failures dù có 50% số requests bị lỗi).
5. **Từ Quan sát 1.4:** Hàm `evaluate_outcome` mặc định fallback về `return "PASS", True, False, ""` ở dòng cuối cùng (line 82). Bất kỳ phản hồi JSON-RPC nào thiếu trường bắt buộc (`result`, `content`) hoặc trả về `result: {}` đều rơi vào dòng 82 và được công nhận là `PASS`.
6. **Từ Quan sát 1.5:** Hàm `evaluate_outcome` thiếu các lớp phòng thủ kiểu dữ liệu (`isinstance(err, dict)`, `isinstance(res, dict)`, `isinstance(content, list)`), dẫn tới crash văng ngoại lệ `AttributeError` / `TypeError` khi gặp payload dị dạng từ các mock server hoặc proxy trung gian.
7. **Kết luận logic:** Benchmark Engine hiện tại vi phạm trực tiếp yêu cầu không được false positive "Passed" / "Verified Live" và không có khả năng tự phục hồi trước payload dị dạng. Do đó, sản phẩm của `worker_c` chưa đủ điều kiện nghiệm thu. Phán quyết bắt buộc là **REQUEST_CHANGES**.

---

## 3. Caveats (Ranh Giới & Giả Định)

1. Các lỗi false positive này xảy ra trong các kịch bản đối kháng (adversarial stress scenarios: mạng chập chờn, bridge crash giữa chừng, payload dị dạng hoặc proxy lỗi). Trong điều kiện lý tưởng (bridge offline ngay từ đầu trên cold run), harness cũ phân loại đúng `CHƯA TEST (Bridge Offline)`. Nhưng sứ mệnh của kiểm thử đối kháng là bảo đảm hệ thống vững chắc trước mọi sự cố bất ngờ.
2. Bộ tính toán phân vị Type 7 (`compute_percentile`) và các hàm thống kê (`compute_latency_stats`) đã được kiểm chứng độc lập là hoạt động hoàn hảo và không cần sửa đổi.

---

## 4. Conclusion (Kết Luận & Phán Quyết)

**Phán quyết:** **REQUEST_CHANGES**

Yêu cầu `worker_c` thực hiện điều chỉnh kỹ thuật trên `benchmark_session.py` (cả ở `HPDynamo/tools/harness/benchmark_session.py` và `HPDynamo/tools/benchmark/benchmark_session.py`) theo các hạng mục cụ thể sau:

### Danh mục yêu cầu sửa đổi (Actionable Remediations):

1. **Sửa `evaluate_outcome(resp)` (Phòng thủ kiểu dữ liệu & Chặn False Positive):**
   - Kiểm tra `isinstance(resp, dict)`. Nếu không, trả về `"FAIL"`.
   - Nếu `"error" in resp`: kiểm tra nếu `isinstance(err, dict)` thì lấy code/message; nếu không thì ép kiểu str. Tuyệt đối không gọi `.get()` trên `None` hoặc `str`.
   - Kiểm tra `"result" in resp` và `resp["result"] is not None`. Nếu thiếu hoặc null, trả về `"FAIL"`.
   - Kiểm tra `res = resp["result"]` phải là `dict`.
   - Kiểm tra `content = res.get("content")` phải là `list` và có độ dài $> 0$. Nếu rỗng hoặc không phải list, trả về `"FAIL"`.
   - Khi duyệt `texts`: bảo đảm `c.get("text")` khác `None` trước khi append.
   - Kiểm tra `joined`: nếu sau khi strip chuỗi `joined` rỗng (empty text), trả về `"FAIL"`.
   - Thêm kiểm tra `"pipe hpdynamo-mcp-2026" in joined` cả ở nhánh `isError=False` nếu bridge trả về plain text.

2. **Sửa `execute_workload(...)` (Fail-Fast & Không False Positive Warm Loop):**
   - Đếm số lần thất bại warm: `warm_failures = 0`.
   - Trong vòng lặp warm runs:
     - Nếu `w_offline`: Đánh dấu `bridge_offline_during_warm = True` và dừng vòng lặp ngay (`break`).
     - Nếu `not w_ok`: Tăng `warm_failures += 1`.
   - Xử lý sau vòng lặp:
     - Nếu `bridge_offline_during_warm`: Trả về `"SKIP"`, status: `"CHƯA TEST (Bridge Offline During Warm)"`.
     - Nếu `warm_runs > 0 and len(warm_samples) == 0`: Toàn bộ warm runs thất bại $\rightarrow$ Bắt buộc trả về `"FAIL"`, status: `f"FAILED (All {warm_runs} warm runs failed)"`.
     - Nếu `warm_failures > 0`: Cần ghi nhận trung thực tỷ lệ suy giảm hoặc đánh dấu degraded.
   - Cập nhật `build_workload_record`: Tham số hóa `failureCount` bằng tổng số lần thất bại thực tế (`cold_fail + warm_failures`), không dùng biểu thức chuỗi `1 if "FAILED" in status_label else 0`.

3. **Chạy lại xác thực:**
   Chạy `python HPDynamo/tools/harness/test_adversary_benchmark.py` cho tới khi đạt **17/17 PASSED (100%)**.

---

## 5. Verification Method (Phương Pháp Kiểm Chứng Độc Lập)

Bất kỳ kiểm toán viên hoặc worker nào cũng có thể kiểm chứng các phát hiện trên bằng lệnh:

```bash
python HPDynamo/tools/harness/test_adversary_benchmark.py
```

- **Hiện trạng:** 12 tests FAIL (chứng minh sự tồn tại của 12 lỗ hổng false positive và unhandled crashes).
- **Điều kiện hoàn thành:** Sau khi worker áp dụng bản vá, chạy lệnh trên phải đạt:
  ```
  Ran 17 tests ... OK
  ```
- **Đồng thời xác thực lại bộ test gốc:**
  ```bash
  python HPDynamo/tools/harness/test_benchmark_session.py
  ```
  phải tiếp tục đạt **11/11 PASSED**.
