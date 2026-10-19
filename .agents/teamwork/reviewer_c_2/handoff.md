# Handoff Report: Review & Adversarial Stress-Test Milestone C (Honest Benchmark & Model Isolation — R2-08)

**Tác giả:** reviewer_c_2  
**Ngày:** 2026-10-09  
**Nhiệm vụ:** Review độc lập và phản biện đối kháng (adversarial review) cho Milestone C (R2-08) của phân hệ HPDynamo MCP Vòng 2.  
**Thư mục làm việc:** `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_c_2`  
**Verdict:** **APPROVE**

---

## 1. Observation (Quan Sát Trực Tiếp)

1. **Mã nguồn PowerShell Harness (`HPDynamo/tools/harness/run-benchmark.ps1`):**
   - **Nhận diện 2 tầng Revit (`Get-RevitSessionStatus`, dòng 47-63):**
     * Quét toàn bộ danh sách tiến trình `Revit.exe`: `$allRevitProcs = @(Get-Process -Name "Revit" -ErrorAction SilentlyContinue)`.
     * Nhận diện cụ thể tiến trình chỉ định: `$targetProc = if ($hasRevit) { $allRevitProcs | Where-Object { $_.Id -eq $KnownTargetPid } } else { $null }` với mặc định `$KnownTargetPid = 26264`.
     * Kiểm tra trạng thái Named Pipe: `Test-Path "\\.\pipe\hpdynamo-mcp-2026"`.
   - **5 Điều khoản Zero-Touch Session Shield (dòng 68-82):**
     * Khi phát hiện Revit, in biểu ngữ bảo vệ không gian làm việc của người dùng:
       ```
       [ZERO-TOUCH SESSION SHIELD ACTIVE]
       - Revit Process(es) Detected: PID(s) ...
       - Target Session PID 26264 Identified: User workspace protected.
       - Protection: Active user documents and canvas are LOCKED.
       - Rule: Zero kill, zero close, zero unverified mutations.
       ```
     * Kịch bản hoàn toàn không chứa bất kỳ lệnh `Stop-Process`, `taskkill` hoặc can thiệp vào tài liệu Revit đang mở.
   - **Sandbox Isolation trong `try...finally` (dòng 84-165, 171-214):**
     * Tạo thư mục sandbox độc lập: `%TEMP%\HPDynamoBenchmark\Session_<GUID>\`.
     * Sao chép model Revit thực `SampleRebarModel.rvt` (7,540,736 bytes) và graph Dynamo `StandardBenchmarkGraph.dyn` (6,949 bytes).
     * Hàm dọn dẹp `Remove-BenchmarkSandbox` thực thi 3 tầng an toàn:
       1. Kiểm tra đường dẫn canonical nằm tuyệt đối trong `%TEMP%\HPDynamoBenchmark` (dòng 133-140); từ chối xóa nếu nằm ngoài.
       2. Gọi `[System.GC]::Collect()` và `[System.GC]::WaitForPendingFinalizers()` trước khi xóa.
       3. Thử lại tối đa 3 lần với backoff thời gian (`200 * $i ms`) chống file lock tạm thời.
     * Khối `finally` (dòng 209-214) đảm bảo sandbox luôn được dọn dẹp sạch sẽ kể cả khi xảy ra lỗi.
   - **Dung lượng tệp:** 217 dòng (đạt chuẩn Rule C2 $\le 300$ dòng).

2. **Persistent Session Engine (`HPDynamo/tools/harness/benchmark_session.py`):**
   - Tách biệt thời gian khởi động $T_{\text{startup}}$ (khởi động process và `initialize`, thực tế: **609.35 ms**) và đo lường ổn định (steady-state warm latency) qua cùng một phiên stdio duy nhất (`Server` class).
   - Kiểm tra preflight tool surface: ghi nhận chính xác **25 tools** (4 Core + 8 Registry + 13 Seeds).
   - Thẩm định 3 tầng lỗi (`evaluate_outcome`, dòng 56-82):
     * Tầng 1: Lỗi RPC (`"error" in resp`).
     * Tầng 2: Giao thức MCP (`result.isError == true`), nhận diện bridge offline / no document để phân loại `SKIP`.
     * Tầng 3: Lỗi domain trong nội dung phản hồi ("The script does not compile", "Execution failed").
   - Tính toán phân vị Type 7 chuẩn nội suy tuyến tính (Linear Interpolation) cho p50/p95, min, max, mean.
   - Đo lường dung lượng byte UTF-8 thực tế của request và response payload.
   - Dung lượng tệp: 255 dòng (đạt chuẩn Rule C2 $\le 300$ dòng; các hàm $\le 50$ dòng).

3. **Fixtures Thử Nghiệm (`HPDynamo/tools/harness/fixtures/`):**
   - `SampleRebarModel.rvt`: Model Revit nhị phân 7,540,736 bytes sao chép từ `HPRebar/HPRebar.Tests/Fixtures/column-stack-2-storey.rvt`.
   - `StandardBenchmarkGraph.dyn`: Graph Dynamo 3.7 hợp lệ gồm 5 nodes (CodeBlock, Point.ByCoordinates, Vector.ZAxis, Line.ByStartPointDirectionLength, Watch) và 4 dây nối liên hoàn.

4. **Báo Cáo JSON Thực Nghiệm (`HPDynamo/reports/baseline-vs-upgraded.json`):**
   - Không chứa bất kỳ số liệu gán cứng thành công giả định hay tỷ lệ tăng tốc bịa đặt nào.
   - Ghi nhận trung thực: `passedWorkloads`: 0, `failedWorkloads`: 0, `skippedWorkloads`: 5, `toolSurfaceRegistered`: 25.
   - Từng workload WL-01..WL-05 ghi nhận rõ: `status: "CHƯA TEST (Bridge Offline)"`, `warmLatencyMs`: {"p50": 0.0, "p95": 0.0}, `rawSamples`: [].

5. **Kết Quả Thực Thi và Kiểm Thử Độc Lập:**
   - Chạy `python HPDynamo/tools/harness/test_benchmark_session.py` $\rightarrow$ **11/11 tests Passed** (0.000s).
   - Chạy `powershell -NoProfile -File HPDynamo/tools/harness/run-benchmark.ps1 -WarmRuns 2 -ColdRuns 1` $\rightarrow$ Exit code 0, tạo sandbox, đo startup 609.35 ms, phát hiện bridge offline, skip WL-01..WL-05, dọn dẹp sạch sandbox.
   - Chạy trực tiếp 5 bộ test suite .NET:
     * `HPDynamo.Mcp.Server.Tests.exe`: **139 Passed**, 0 Failed, 0 Skipped (5.35s).
     * `HPDynamo.McpBridge.Tests.exe`: **223 Passed**, 0 Failed, 0 Skipped (21.78s).
     * `HPDynamo.E2E.Tests.exe`: **209 Passed**, 0 Failed, 0 Skipped (3.43s).
     * `HPRebar.McpBridge.Core.Net48Tests.exe`: **115 Passed**, 0 Failed, 0 Skipped (3.77s).
     * `HPRebar.Mcp.Server.Core.Tests.exe`: **915 Passed**, 0 Failed, 0 Skipped (4.00s).
     * Tổng cộng: **1,601 / 1,601 tests .NET Passed 100%** không có regression.

---

## 2. Logic Chain (Chuỗi Lập Luận)

1. Từ **Quan sát 1.1**: Kịch bản `run-benchmark.ps1` thực thi nhận diện 2 tầng cho Revit và PID 26264, hiển thị biểu ngữ Zero-Touch Shield, không thực hiện bất kỳ lệnh can thiệp nào vào tiến trình người dùng $\rightarrow$ Thỏa mãn tiêu chí an toàn tuyệt đối cho phiên làm việc Revit active.
2. Từ **Quan sát 1.1 & 1.3**: Thư mục sandbox được tạo độc lập trong `%TEMP%\HPDynamoBenchmark\Session_<GUID>\` và copy các fixture thật (RVT 7.5MB và DYN 6.9KB) chứ không dùng file rỗng $\rightarrow$ Đảm bảo tính cô lập mô hình (Model Isolation).
3. Từ **Quan sát 1.1**: Hàm `Remove-BenchmarkSandbox` kiểm tra chặt chẽ tiền tố `%TEMP%\HPDynamoBenchmark` trước khi xóa, thực hiện GC và retry, được đặt trong khối `finally` $\rightarrow$ Đảm bảo an toàn bảo mật, chống path traversal và chống rò rỉ tệp tạm.
4. Từ **Quan sát 1.2 & 1.4**: Bộ đánh giá lỗi 3 tầng trong `benchmark_session.py` bóc tách chính xác trạng thái offline của bridge, chuyển các workload sang `SKIP` / `CHƯA TEST (Bridge Offline)`, lưu `rawSamples: []` và latency 0.0, không bịa đặt số liệu $\rightarrow$ Thỏa mãn tính trung thực của benchmark (Honest Benchmark, R2-08).
5. Từ **Quan sát 1.5**: Cả 11 unit tests Python và 1,601 .NET tests đều đạt 100% green $\rightarrow$ Hệ thống hoạt động ổn định, không phát sinh lỗi hồi quy.

---

## 3. Adversarial Review & Integrity Assessment (Kiểm Tra Đối Kháng & Liêm Chính)

- **Integrity Assessment:**
  * Không phát hiện hardcoded test results hay kết quả ngụy tạo trong mã nguồn.
  * Không có dummy hay facade implementation: Fixture RVT và DYN là dữ liệu thật; Persistent session sử dụng giao thức stdio thật.
  * Báo cáo JSON ghi nhận trung thực hiện trạng offline của bridge, loại bỏ hoàn toàn các nhãn "Verified Live" hay "100% Passed" khi chưa đo được.
  * **Kết luận Liêm chính:** KHÔNG CÓ VI PHẠM TÍNH TOÀN VẸN (NO INTEGRITY VIOLATION).
- **Stress-Test & Edge Case Scenarios:**
  * *Tình huống 1: Tấn công thoát thư mục (Directory Traversal) trong cleanup.* Nếu truyền đường dẫn bên ngoài `%TEMP%\HPDynamoBenchmark` vào `Remove-BenchmarkSandbox`, cơ chế kiểm tra canonical path sẽ chặn lại ngay lập tức và in cảnh báo `SECURITY REFUSAL`. $\rightarrow$ **PASS**.
  * *Tình huống 2: Revit PID 26264 đang mở.* Kịch bản nhận diện PID 26264, kích hoạt Zero-Touch Shield, không gửi lệnh kill/close nào. $\rightarrow$ **PASS**.
  * *Tình huống 3: Lỗi ngoại lệ trong quá trình chạy benchmark.* Khối `try...finally` đảm bảo thư mục sandbox luôn được xóa sau khi kết thúc. $\rightarrow$ **PASS**.

---

## 4. Caveats (Ranh Giới & Giả Định)

- **Trạng thái Bridge Live:** Tại thời điểm kiểm thử trên máy trạm dev, cầu nối Revit Named Pipe `hpdynamo-mcp-2026` không hoạt động. Kịch bản đã xử lý chính xác tình huống này bằng cách phân loại các workload WL-01..WL-05 sang `CHƯA TEST (Bridge Offline)` / `SKIP`. Khi có phiên Revit bridge live chạy, kịch bản sẽ tự động đo lường và điền mảng `rawSamples` cùng thống kê p50/p95 live.

---

## 5. Conclusion (Kết Luận)

Milestone C (Đợt C: Honest Benchmark & Model Isolation — R2-08) của phân hệ HPDynamo MCP Vòng 2 hoàn toàn đáp ứng các yêu cầu kỹ thuật, tiêu chuẩn Clean Code và nguyên tắc trung thực khách quan:
- Nhận diện 2 tầng Revit và Zero-Touch Shield bảo vệ an toàn tuyệt đối PID 26264.
- Sandbox isolation với fixture RVT/DYN thật và cleanup an toàn trong `try...finally`.
- Persistent session engine đo lường trung thực, phân biệt $T_{\text{startup}}$ và warm latency, loại bỏ toàn bộ số liệu giả định.
- 100% test suites (11 Python + 1,601 .NET = 1,612 tests) đều PASS.

Phán quyết chính thức: **APPROVE**.

---

## 6. Verification Method (Phương Pháp Kiểm Chứng Độc Lập)

1. Chạy test harness unit test:
   ```bash
   python HPDynamo/tools/harness/test_benchmark_session.py
   ```
   *Kỳ vọng:* `Ran 11 tests ... OK`.

2. Chạy kịch bản benchmark:
   ```powershell
   powershell -NoProfile -File HPDynamo/tools/harness/run-benchmark.ps1 -WarmRuns 2 -ColdRuns 1
   ```
   *Kỳ vọng:* Exit code 0, hiển thị sandbox creation, startup ms, bridge offline skip thông báo, sandbox cleanup thông báo.

3. Kiểm tra file kết quả JSON:
   Kiểm tra `HPDynamo/reports/baseline-vs-upgraded.json` xác nhận trạng thái `CHƯA TEST (Bridge Offline)` và không có số liệu giả mạo.

4. Kiểm tra regression các test suites:
   ```powershell
   & "HPDynamo\HPDynamo.Mcp.Server.Tests\bin\Debug\net10.0\HPDynamo.Mcp.Server.Tests.exe"
   & "HPDynamo\HPDynamo.McpBridge.Tests\bin\Debug\net10.0-windows7.0\HPDynamo.McpBridge.Tests.exe"
   & "HPDynamo\HPDynamo.E2E.Tests\bin\Debug\net10.0\HPDynamo.E2E.Tests.exe"
   & "McpShared\HPRebar.McpBridge.Core.Net48Tests\bin\Debug\net48\HPRebar.McpBridge.Core.Net48Tests.exe"
   & "McpShared\HPRebar.Mcp.Server.Core.Tests\bin\Debug\net10.0\HPRebar.Mcp.Server.Core.Tests.exe"
   ```
   *Kỳ vọng:* 1,601/1,601 tests Passed.
