# Handoff Report: Adversarial Verification of Milestone C (Honest Benchmark & Sandbox Isolation — R2-08)

**Agent:** challenger_c_2 (Empirical Challenger)  
**Date:** 2026-10-09  
**Target:** HPDynamo MCP Vòng 2 — Milestone C (`worker_c`)  
**Verdict:** **REQUEST_CHANGES**

---

## 1. Observation (Quan Sát Trực Tiếp)

### 1.1. Empirical Adversarial Testing: Sandbox Boundary Enforcement
- Vị trí mã nguồn:
  * `HPDynamo/tools/benchmark/run-benchmark.ps1` (dòng 133–140):
  * `HPDynamo/tools/harness/run-benchmark.ps1` (dòng 133–140):
  ```powershell
  133:     $tempCanonical = [System.IO.Path]::GetFullPath($env:TEMP).TrimEnd('\', '/')
  134:     $targetCanonical = [System.IO.Path]::GetFullPath($SandboxDir).TrimEnd('\', '/')
  135: 
  136:     if (-not $targetCanonical.StartsWith($tempCanonical, [StringComparison]::OrdinalIgnoreCase) -or
  137:         -not $targetCanonical.Contains("HPDynamoBenchmark")) {
  138:         Write-Error "SECURITY REFUSAL: Target directory '$SandboxDir' is outside %TEMP%\HPDynamoBenchmark. Deletion aborted."
  139:         return
  140:     }
  ```
- Kết quả chạy test đối kháng `test_sandbox_isolation.ps1`:
  ```
  [PASS] Boundary Refusal: Root Directory Attack - Target 'C:\Windows\System32' safely rejected with security refusal (stillExists=True).
  [PASS] Boundary Refusal: Repo Root Attack - Target 'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar' safely rejected with security refusal (stillExists=True).
  [PASS] Boundary Refusal: User Profile Attack - Target 'C:\Users\STR-HP03' safely rejected with security refusal (stillExists=True).
  [FAIL] Boundary Refusal: Temp Substring Spoof Attack - Target 'C:\Users\STR-HP03\AppData\Local\Temp\NotHPDynamoBenchmark' safely rejected with security refusal (stillExists=False).
  [PASS] Boundary Refusal: Directory Traversal Attack - Target 'C:\Users\STR-HP03\AppData\Local\Temp\HPDynamoBenchmark\..\OtherDir' safely rejected with security refusal (stillExists=True).
  [PASS] Boundary Refusal: Direct %TEMP% Root - Prevented accidental wiping of entire %TEMP% root directory.
  [PASS] Sandbox Creation: Valid Isolation Folder - Created isolated sandbox at 'C:\Users\STR-HP03\AppData\Local\Temp\HPDynamoBenchmark\Session_07d05149a3ee47a28d9e83f27338d212'
  [PASS] Fixture Integrity: SampleRebarModel.rvt - Size: 7540736 bytes, SHA256: 80F81D50CE7B43761D66C929E700ADCF3651132171578B0AC81F2C92C0DE2818, ReadOnly=False
  [PASS] Fixture Integrity: StandardBenchmarkGraph.dyn - Size: 6949 bytes, Nodes: 5, Connectors: 4, JSON: Valid
  [PASS] In-Bounds Sandbox Cleanup - Cleaned up sandbox folder completely (Exists=False).
  Summary: 9 / 10 Passed (1 Failed)
  ```
- Phát hiện lỗ hổng ranh giới (**SEC-01: Substring Path Boundary Bypass**):
  * Biểu thức kiểm tra `$targetCanonical.Contains("HPDynamoBenchmark")` chỉ so khớp chuỗi con (`substring`).
  * Thực nghiệm chứng minh: Khi truyền một thư mục anh em (sibling) trong `%TEMP%` có chứa từ khoá như `C:\Users\STR-HP03\AppData\Local\Temp\NotHPDynamoBenchmark` hoặc `HPDynamoBenchmark_Backup`, hàm `Remove-BenchmarkSandbox` không hề từ chối mà thực hiện xoá vĩnh viễn thư mục đó (`stillExists=False`).
  * Nguy cơ mở rộng: Nếu người gọi truyền chính thư mục gốc sandbox `$env:TEMP\HPDynamoBenchmark`, hàm cũng không từ chối và sẽ xoá sạch toàn bộ thư mục gốc, gây ảnh hưởng đến các phiên benchmark khác đang chạy song song.

---

### 1.2. Zero-Touch Shield & Revit PID 26264 Isolation
- Đã chạy `test_zero_touch_shield.ps1`:
  ```
  [PASS] AST: Zero Termination Invariant - Zero kill/terminate keywords found in script.
  [PASS] Detection: Zero Revit Processes - IsRunning=False, TargetPidDetected=False
  [PASS] Detection: Specific PID 26264 Active - Target detected=True, ActivePids=26264
  [PASS] Detection: Other Revit PID (No 26264) - IsRunning=True, TargetPidDetected=False (expected false)
  [PASS] Detection: Multi-Instance Cluster including 26264 - ProcessCount=3, TargetPidDetected=True
  [PASS] Dynamic Survival: External Process Non-Interference - Process PID 1812 was alive before and survived full benchmark execution.
  Summary: 6 / 6 Passed (0 Failed)
  ```
- Quét AST mã nguồn: Hoàn toàn không có bất kỳ lệnh nào như `Stop-Process`, `taskkill`, `TerminateProcess`, `.Kill()`, `CloseMainWindow`.
- Trạng thái hệ thống thực tế: Hiện tại Revit PID 26264 không chạy trên máy dev (chỉ có process nền `RevitAccelerator` PID 13656).

---

### 1.3. Fixture Cloning & Integrity
- `SampleRebarModel.rvt`:
  * Dung lượng: 7,540,736 bytes (khớp 100% với file nguồn tại `HPRebar/HPRebar.Tests/Fixtures/column-stack-2-storey.rvt`).
  * SHA256: `80F81D50CE7B43761D66C929E700ADCF3651132171578B0AC81F2C92C0DE2818` (khớp 100%).
  * Thuộc tính: `IsReadOnly = $false`.
- `StandardBenchmarkGraph.dyn`:
  * Dung lượng: 6,949 bytes (khớp 100% với file nguồn).
  * Cấu trúc JSON: Parse hợp lệ, chứa đúng 5 nodes (`CodeBlock`, `Point.ByCoordinates`, `Vector.ZAxis`, `Line.ByStartPointDirectionLength`, `CoreNodeModels.Watch`) và 4 connectors liên hoàn.

---

### 1.4. Độc Lập Chạy Toàn Bộ 5 .NET Test Suites (Zero Regression)
Đã thực thi độc lập toàn bộ 5 test suites và 1 python suite:
1. `dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests`:
   * Kết quả: **139 Passed, 0 Failed, 0 Skipped** (5.17s)
2. `dotnet test HPDynamo/HPDynamo.McpBridge.Tests`:
   * Kết quả: **223 Passed, 0 Failed, 0 Skipped** (21.65s)
3. `dotnet test HPDynamo/HPDynamo.E2E.Tests`:
   * Kết quả: **209 Passed, 0 Failed, 0 Skipped** (3.46s)
4. `dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests`:
   * Kết quả: **115 Passed, 0 Failed, 0 Skipped** (4.06s)
5. `dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests --no-build`:
   * Kết quả: **915 Passed, 0 Failed, 0 Skipped** (4.20s)
6. `python HPDynamo/tools/harness/test_benchmark_session.py`:
   * Kết quả: **11 Passed, 0 Failed** (0.00s)
- **Tổng cộng: 1,612 / 1,612 tests Passed 100%**. Không có bất kỳ regression nào trong mã C#/.NET.

---

### 1.5. Live Execution & Báo Cáo Trung Thực
- Chạy live `powershell -NoProfile -File HPDynamo\tools\benchmark\run-benchmark.ps1 -WarmRuns 10 -ColdRuns 1`:
  * Thoát với exit code: `0`.
  * Startup handshake: `1217.0 ms`.
  * Tool surface: `25 tools`.
  * Kết quả workloads: `Total=5, Passed=0, Skipped=5, Failed=0`.
  * Cả 5 workloads ghi nhận chính xác: `SKIPPED (Dynamo bridge not connected (offline mode))`.
  * Báo cáo `baseline-vs-upgraded.json` ghi nhận trung thực trạng thái `CHƯA TEST (Bridge Offline)`, không có nhãn giả mạo nào.

---

## 2. Logic Chain (Chuỗi Lập Luận)

1. Từ **Quan sát 1.2 & 1.3**: Zero-Touch Shield hoạt động chính xác theo thiết kế, bảo vệ an toàn tiến trình Revit (kể cả khi có PID 26264 hay các PID khác). File fixture nhị phân `.rvt` và `.dyn` được nhân bản toàn vẹn sang sandbox độc lập trước khi chạy, không gây ảnh hưởng đến dữ liệu gốc.
2. Từ **Quan sát 1.4 & 1.5**: Tính năng đo lường trung thực đã loại bỏ hoàn toàn các số liệu giả mạo; 100% test suites .NET (1,601 tests) và Python (11 tests) đều xanh, chứng minh tính ổn định tuyệt đối của các chức năng nghiệp vụ và wire protocol.
3. Tuy nhiên, từ **Quan sát 1.1**: Rào chắn Sandbox Deletion Boundary trong `Remove-BenchmarkSandbox` tại cả hai file `HPDynamo/tools/benchmark/run-benchmark.ps1` và `HPDynamo/tools/harness/run-benchmark.ps1` đang có lỗ hổng kiểm tra logic:
   - Dòng 137 sử dụng `$targetCanonical.Contains("HPDynamoBenchmark")`.
   - Một thư mục bất kỳ trong `%TEMP%` có tên chứa cụm từ `HPDynamoBenchmark` (ví dụ: `%TEMP%\NotHPDynamoBenchmark`, `%TEMP%\HPDynamoBenchmark_Backup`) sẽ vượt qua điều kiện kiểm tra và bị xoá sạch dữ liệu.
   - Thư mục gốc `%TEMP%\HPDynamoBenchmark` cũng không được bảo vệ khỏi lệnh xoá ngoài ý muốn.
4. Điều này vi phạm tiêu chí ranh giới: "Thử nghiệm xem hàm dọn dẹp có khả năng xóa nhầm file ngoài sandbox `%TEMP%\HPDynamoBenchmark` không (kiểm tra boundary enforcement)".
5. Theo quy định vai trò Empirical Challenger: Khi phát hiện và tái hiện thành công lỗi thực nghiệm (reproducible vulnerability), Challenger **bắt buộc phải đưa ra phán quyết REQUEST_CHANGES** và chuyển giao phương án khắc phục cho Worker, không được tự ý sửa đổi mã nguồn triển khai.

---

## 3. Caveats (Ranh Giới & Giả Định)

1. **Phạm vi lỗi:** Lỗ hổng nằm ở hàm dọn dẹp trong script PowerShell benchmark (`run-benchmark.ps1`), không ảnh hưởng đến logic runtime của MCP C# Bridge hay Server.
2. **Khả năng kích hoạt:** Trong kịch bản chạy bình thường của script, biến `$sandbox.SessionDir` được lấy từ GUID ngẫu nhiên sinh ra bởi `Initialize-BenchmarkSandbox`, nên lỗi này không xảy ra trong chu trình tự động khép kín. Tuy nhiên, nếu hàm `Remove-BenchmarkSandbox` được gọi độc lập hoặc truyền tham số bên ngoài, nguy cơ xoá nhầm thư mục người dùng là có thật.

---

## 4. Conclusion (Kết Luận & Yêu Cầu Chỉnh Sửa)

**Phán quyết:** **REQUEST_CHANGES**

### Yêu cầu Worker khắc phục:
Sửa điều kiện kiểm tra ranh giới tại `Remove-BenchmarkSandbox` trong cả hai file:
- `HPDynamo/tools/benchmark/run-benchmark.ps1`
- `HPDynamo/tools/harness/run-benchmark.ps1`

**Mã nguồn hiện tại:**
```powershell
    $tempCanonical = [System.IO.Path]::GetFullPath($env:TEMP).TrimEnd('\', '/')
    $targetCanonical = [System.IO.Path]::GetFullPath($SandboxDir).TrimEnd('\', '/')

    if (-not $targetCanonical.StartsWith($tempCanonical, [StringComparison]::OrdinalIgnoreCase) -or
        -not $targetCanonical.Contains("HPDynamoBenchmark")) {
        Write-Error "SECURITY REFUSAL: Target directory '$SandboxDir' is outside %TEMP%\HPDynamoBenchmark. Deletion aborted."
        return
    }
```

**Mã nguồn đề xuất sửa đổi:**
```powershell
    $sandboxRootCanonical = [System.IO.Path]::GetFullPath((Join-Path $env:TEMP "HPDynamoBenchmark")).TrimEnd('\', '/')
    $requiredPrefix = $sandboxRootCanonical + [System.IO.Path]::DirectorySeparatorChar

    if (-not $targetCanonical.StartsWith($requiredPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        Write-Error "SECURITY REFUSAL: Target directory '$SandboxDir' is outside %TEMP%\HPDynamoBenchmark. Deletion aborted."
        return
    }
```
*Lợi ích:* Đảm bảo chỉ những thư mục con bên trong `%TEMP%\HPDynamoBenchmark\` mới được phép xoá. Từ chối hoàn toàn các thư mục anh em cùng tên chuỗi con và ngăn chặn việc xoá chính thư mục gốc `HPDynamoBenchmark`.

---

## 5. Verification Method (Phương Pháp Kiểm Chứng Độc Lập)

1. **Chạy bộ test đối kháng Zero-Touch Shield:**
   ```powershell
   powershell -NoProfile -File .agents\teamwork\challenger_c_2\test_zero_touch_shield.ps1
   ```
   *Kỳ vọng:* 6 / 6 Passed (100%).

2. **Chạy bộ test đối kháng Sandbox Isolation Boundary:**
   ```powershell
   powershell -NoProfile -File .agents\teamwork\challenger_c_2\test_sandbox_isolation.ps1
   ```
   *Hiện tại:* 9 / 10 Passed (1 Failed tại Temp Substring Spoof Attack).  
   *Kỳ vọng sau khi Worker sửa:* 10 / 10 Passed (100%).

3. **Chạy lại 5 test suites .NET:**
   ```bash
   dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests
   dotnet test HPDynamo/HPDynamo.McpBridge.Tests
   dotnet test HPDynamo/HPDynamo.E2E.Tests
   dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests
   dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests --no-build
   ```
   *Kỳ vọng:* 1,601 / 1,601 Passed 100%.
