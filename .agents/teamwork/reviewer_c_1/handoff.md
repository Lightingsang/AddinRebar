# Handoff Report: Review & Adversarial Audit Milestone C (R2-08) — HPDynamo MCP Vòng 2

**Tác giả:** reviewer_c_1 (Reviewer & Adversarial Critic)  
**Ngày:** 2026-10-09  
**Nhiệm vụ:** Review Benchmark Engine (`HPDynamo/tools/harness/benchmark_session.py`), thẩm định 3 tầng lỗi, phân vị Type 7, persistent session, UTF-8 payload bytes, PowerShell harness và unit tests.  
**Thư mục làm việc:** `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_c_1`  
**Phán quyết:** **APPROVE**  

---

## 1. Observation (Quan Sát Trực Tiếp)

1. **Khảo sát mã nguồn `HPDynamo/tools/harness/benchmark_session.py` (255 dòng):**
   - **Tách biệt Startup Latency và Persistent Session (`run_benchmark`, dòng 185–198):**
     ```python
     t_start = time.perf_counter()
     server = Server(server_exe, name="benchmark_session")
     server.initialize()
     startup_ms = round((time.perf_counter() - t_start) * 1000.0, 2)
     tools_count = len(server.tools())
     ```
     Phiên `Server` từ `McpShared/tools/harness_common.py` được khởi tạo đúng 1 lần duy nhất, handshake `initialize` được đo đạc riêng biệt thành `$T_{\text{startup}}$`, xác thực preflight đủ 25 tools trước khi bắt đầu đo lường các workloads.
   - **Thẩm định 3 tầng lỗi (`evaluate_outcome`, dòng 56–82):**
     * *Tier 1 (JSON-RPC Error, dòng 60–62):* Bắt `"error" in resp` $\rightarrow$ `FAIL`.
     * *Tier 2 (MCP Protocol `isError`, dòng 68–73):* Bắt `res.get("isError")`. Nếu phát hiện `"Dynamo bridge not connected"` hoặc `"No document is open in Revit"`, phân loại chính xác thành `SKIP` (offline mode); các lỗi khác trả về `FAIL`.
     * *Tier 3 (Domain Failure Text, dòng 75–80):* Bắt các chuỗi lỗi domain nghiệp vụ `"Dynamo bridge not connected"` ($\rightarrow$ `SKIP`), `"The script does not compile"` ($\rightarrow$ `FAIL`), `"Execution failed"` / `"Exception thrown"` ($\rightarrow$ `FAIL`).
     * Chỉ trả về `PASS` khi thỏa mãn đồng thời cả 3 tầng (dòng 82).
   - **Thuật toán phân vị Type 7 chuẩn nội suy tuyến tính (`compute_percentile`, dòng 28–39):**
     ```python
     k = (p / 100.0) * (n - 1)
     i = int(k)
     f = k - i
     val = sorted_s[i] + f * (sorted_s[i + 1] - sorted_s[i]) if i + 1 < n else sorted_s[i]
     ```
     Khớp hoàn toàn với công thức toán học chuẩn của Type 7 (R default / NumPy linear interpolation). Kiểm soát biên chặt chẽ: xử lý `not sorted_s` (trả về 0.0), $n=1$ (trả về giá trị duy nhất), và $i+1 < n$ chống index out of bounds khi $k = n-1$.
   - **Tính toán UTF-8 Payload Bytes (`calculate_payload_bytes`, dòng 85–90):**
     Đo đạc chính xác qua `len(json.dumps(..., ensure_ascii=False).encode("utf-8")) + 1` cho cả request và response NDJSON.
   - **Điều khiển vòng lặp Fail-Fast (`execute_workload`, dòng 159–165):**
     Khi cold run gặp `SKIP` hoặc `FAIL`, hàm lập tức thoát ra và trả về bản ghi với `warmLatencyMs: {p50: 0.0, ...}`, `rawSamples: []`, tuyệt đối không chạy 10 warm runs mù quáng và không gán số liệu ảo.

2. **Khảo sát PowerShell Harness `HPDynamo/tools/harness/run-benchmark.ps1` (217 dòng):**
   - **Zero-Touch Session Shield (`Get-RevitSessionStatus`, dòng 47–79):**
     Quét tiến trình Revit, phát hiện cụ thể PID 26264, kích hoạt biểu ngữ cảnh báo và tuân thủ nguyên tắc khóa bất biến (zero kill, zero close, zero mutations đối với session đang mở).
   - **Sandbox Isolation (`Initialize-BenchmarkSandbox` & `Remove-BenchmarkSandbox`, dòng 84–165):**
     Nhân bản file fixture thật `SampleRebarModel.rvt` (7,540,736 bytes) và `StandardBenchmarkGraph.dyn` (6,949 bytes) vào thư mục cách ly `%TEMP%\HPDynamoBenchmark\Session_<GUID>\`.
     Dọn dẹp trong khối `finally` với 3 tầng ràng buộc an toàn: kiểm tra canonical path nằm trong temp benchmark, cưỡng chế GC thu hồi file handle, retry 3 lần chống transient lock.

3. **Chạy thực nghiệm độc lập Benchmark Harness:**
   - Chạy lệnh: `powershell -NoProfile -File HPDynamo/tools/harness/run-benchmark.ps1 -WarmRuns 2 -ColdRuns 1`
   - Kết quả quan sát:
     * Khởi động server exe: `HPDynamo.Mcp.Server.exe`.
     * `Startup Handshake: 654.94 ms | Tool Surface: 25 tools`.
     * Cả 5 workloads WL-01..WL-05 được phân loại trung thực: `SKIPPED (Dynamo bridge not connected (offline mode))`.
     * Báo cáo ghi nhận: `Summary: Total=5, Passed=0, Skipped=5, Failed=0`.
     * Dọn dẹp sandbox thành công: `Sandbox cleaned up: C:\Users\STR-HP03\AppData\Local\Temp\HPDynamoBenchmark\Session_8aa038b265a14e0abb060b3812352bad`.
     * File `baseline-vs-upgraded.json` được cập nhật trung thực với trạng thái `"CHƯA TEST (Bridge Offline)"`, không có bất kỳ chuỗi cứng giả định nào.

4. **Kiểm thử tự động toàn diện:**
   - Unit tests benchmark engine: `python HPDynamo/tools/harness/test_benchmark_session.py` $\rightarrow$ **11/11 Passed** in 0.000s.
   - `dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests` $\rightarrow$ **139 Passed**, 0 Failed, 0 Skipped (Duration: 5s 497ms).
   - `dotnet test HPDynamo/HPDynamo.McpBridge.Tests` $\rightarrow$ **223 Passed**, 0 Failed, 0 Skipped (Duration: 23s 019ms).
   - `dotnet test HPDynamo/HPDynamo.E2E.Tests` $\rightarrow$ **209 Passed**, 0 Failed, 0 Skipped (Duration: 3s 453ms).
   - `dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests` $\rightarrow$ **115 Passed**, 0 Failed, 0 Skipped (Duration: 3s 970ms).
   - `dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests` $\rightarrow$ **915 Passed**, 0 Failed, 0 Skipped (Duration: 4s 411ms).
   - **Tổng cộng: 1,612 tests (1,601 .NET + 11 Python) đạt 100% Passed.**

5. **Kiểm tra Clean Code Governance & Rà soát tính toàn vẹn (Integrity Audit):**
   - Không có hardcoded test results hay kết quả ngụy tạo trong mã nguồn.
   - Không có dummy hay facade implementation: thuật toán Type 7, bộ thẩm định 3 tầng và cơ chế persistent session đều là logic thực.
   - Dung lượng mã nguồn: `benchmark_session.py` 255 dòng ($\le 300$), `run-benchmark.ps1` 217 dòng ($\le 300$). Tất cả các hàm đều $\le 50$ dòng.
   - 0 dead code, 0 unannotated empty catch blocks.

---

## 2. Logic Chain (Chuỗi Lập Luận)

1. Từ **Quan sát 1.1**: Persistent session engine giữ kết nối stdio mở liên tục qua class `Server`, bóc tách riêng rẽ $T_{\text{startup}}$ (đo được ~654 ms) so với độ trễ gọi tool thông thường. Điều này loại bỏ hoàn toàn hiện tượng đo nhầm chi phí spawn tiến trình .NET CLR ở mỗi lượt gọi.
2. Từ **Quan sát 1.1**: Bộ thẩm định 3 tầng `evaluate_outcome` kiểm tra tuần tự từ JSON-RPC protocol error đến MCP `isError` và text lỗi domain. Khi bridge offline, nó phân loại chuẩn xác thành `SKIP` và dừng vòng lặp ngay lập tức (`rawSamples: []`, `p50: 0.0`), loại bỏ hoàn toàn các trường hợp nuốt lỗi hoặc false-positive "Verified Live" khi bridge chưa kết nối.
3. Từ **Quan sát 1.1 & 1.4**: Công thức phân vị Type 7 trong `compute_percentile` khớp chính xác với chuẩn toán học của R/NumPy (được kiểm chứng qua 3 unit test cho tập rỗng, tập 1 phần tử và 10 phần tử).
4. Từ **Quan sát 1.2 & 1.3**: Zero-Touch Session Shield và cơ chế sandbox nhân bản file thật vào `%TEMP%` giải phóng hoàn toàn nguy cơ can thiệp vào tài liệu đang mở của người dùng trên Revit PID 26264.
5. Từ **Quan sát 1.4**: Toàn bộ 1,612 tests trên 5 test suites và harness unit tests đều xanh 100%, chứng minh không có regression trên bất kỳ thành phần nào của solution.
6. Từ **Quan sát 1.5**: Không phát hiện bất kỳ vi phạm tính toàn vẹn (Integrity Violation) nào; giải pháp đáp ứng trọn vẹn và trung thực yêu cầu R2-08 của Milestone C.

---

## 3. Caveats (Ranh Giới & Khuyến Nghị Khảo Sát Thêm)

1. **Khảo sát khi Bridge Live:** Khi không có Revit UI đang mở, harness tự động đánh dấu các workloads đụng tới bridge là `CHƯA TEST (Bridge Offline)`. Đây là hành vi trung thực và an toàn. Khi có phiên Revit bridge live chạy, kịch bản sẽ tự động đo lường và điền mảng `rawSamples` cùng thống kê p50/p95 live.
2. **Khuyến nghị mở rộng cấu trúc JSON domain (Tương lai):** Hiện tại `evaluate_outcome` bắt lỗi qua `isError`, RPC errors và các chuỗi báo lỗi domain ("The script does not compile", "Execution failed", "Exception thrown"). Nếu sau này có tool mới trả về JSON payload lồng bên trong `content[0].text` dạng `{"success": false}` mà không bật cờ `isError`, bộ thẩm định có thể bổ sung thêm bước thử parse JSON nội tại để bắt triệt để hơn nữa.
3. **Đồng bộ 2 bản sao script:** Thư mục `HPDynamo/tools/harness/` và `HPDynamo/tools/benchmark/` hiện lưu giữ 2 bản sao đồng bộ của `benchmark_session.py` và `run-benchmark.ps1`. Cần lưu ý duy trì tính đồng nhất khi có thay đổi trong tương lai.

---

## 4. Conclusion (Kết Luận)

**Phán quyết:** **APPROVE**  

Milestone C (Đợt C: Honest Benchmark & Model Isolation — R2-08) của dự án HPDynamo MCP Vòng 2 hoàn toàn đạt chuẩn nghiệm thu:
- Khắc phục triệt để lỗi nuốt lỗi và kết quả giả định của harness cũ.
- Vận hành chính xác persistent session, đo lường trung thực startup latency, raw samples, linear interpolation Type 7 p50/p95 và UTF-8 payload bytes.
- Cơ chế bảo vệ Zero-Touch Session Shield cho Revit PID 26264 và Sandbox Isolation hoạt động an toàn và tin cậy.
- Mã nguồn sạch sẽ, tuân thủ nghiêm ngặt Clean Code Governance (C2, M3, Q-B1, Q-B2).
- Toàn bộ 1,612 tests xanh 100%, không phát hiện bất kỳ vi phạm tính toàn vẹn nào.

---

## 5. Verification Method (Phương Pháp Kiểm Chứng Độc Lập)

Bất kỳ reviewer hoặc auditor độc lập nào đều có thể kiểm chứng lại bằng các lệnh sau:

1. **Kiểm tra Unit Test Benchmark Engine:**
   ```bash
   python HPDynamo/tools/harness/test_benchmark_session.py
   ```
   *Kỳ vọng:* 11/11 tests Passed.

2. **Chạy Benchmark Harness độc lập:**
   ```powershell
   powershell -NoProfile -File HPDynamo/tools/harness/run-benchmark.ps1 -WarmRuns 2 -ColdRuns 1
   ```
   *Kỳ vọng:*
   - Nhận diện Revit session và tạo sandbox.
   - Báo cáo $T_{\text{startup}}$ (~600-660 ms) và 25 tools.
   - Nhận diện đúng bridge offline và gắn nhãn `SKIPPED (Dynamo bridge not connected (offline mode))`.
   - Sandbox trong `%TEMP%` được dọn dẹp sạch sẽ.
   - Exit code: 0.

3. **Kiểm tra 5 .NET Test Suites (Zero Regressions):**
   ```bash
   dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests
   dotnet test HPDynamo/HPDynamo.McpBridge.Tests
   dotnet test HPDynamo/HPDynamo.E2E.Tests
   (cd McpShared && dotnet test HPRebar.McpBridge.Core.Net48Tests)
   (cd McpShared && dotnet test HPRebar.Mcp.Server.Core.Tests)
   ```
   *Kỳ vọng:* 1,601 tests Passed 100%.
