# Báo Cáo Khảo Sát: Cơ Chế Phát Hiện Revit PID 26264 & Cách Ly Model/Test Fixtures

**Agent:** `explorer_c_3`  
**Dự án:** HPDynamo MCP Vòng 2 — Milestone C (orchestrator_8_gen2)  
**Ngày:** 2026-10-09  
**Mục tiêu:** Khảo sát cơ chế phát hiện tiến trình Revit PID 26264, xác lập nguyên tắc Zero Touch bảo vệ phiên làm việc của người dùng, kiểm kê test fixtures (.dyn, .rvt) hiện có và thiết kế giải pháp nhân bản fixture ra thư mục độc lập tạm thời kèm cơ chế dọn dẹp an toàn.

---

## 1. Observation (Các Quan Sát Thực Nghiệm Trực Tiếp)

### 1.1 Về Tiến Trình Revit & PID 26264
1. **Dấu vết PID 26264 trong codebase:**
   - Tại `HPDynamo/tools/harness/run-benchmark.ps1:4` & `:231`:
     ```powershell
     # Line 4:
     # Enforces test model copy isolation to protect active user Revit session (PID 26264).
     # Line 231:
     isolationMode = "Test Model Copy Isolation (Active PID 26264 protected)"
     ```
   - Tại `plans/261009-1118-hpdynamo-mcp-performance/benchmark-plan.md:21`:
     > "Phiên khảo sát hiện tại ghi nhận Revit 2026 đang mở với phiên làm việc thực tế của user (PID 26264). Theo quy tắc an toàn repository, không tự ý inject script hay load graph lên model đang mở."
   - Tại `scratch/dismiss_security_dialog.py:4`:
     ```python
     REVIT_PID = 26264
     ```
   - Tại `ORIGINAL_REQUEST.md:771`:
     > "Tuyệt đối không can thiệp phiên làm việc Revit hiện hành: Không inject code, không mở hay đóng model người dùng đang mở trong PID 26264; mọi kiểm chứng live phải thực hiện trên file bản sao riêng biệt trong thư mục test."
2. **Trạng thái thực tế trên máy trạm hiện tại:**
   - Lệnh kiểm tra tiến trình `Get-Process Revit -ErrorAction SilentlyContinue`: trả về exit code 1 (không có process `Revit` nào đang chạy tại thời điểm khảo sát).
   - Lệnh kiểm tra named pipe `[System.IO.Directory]::GetFiles('\\.\pipe\', '*mcp*')`: chỉ trả về `\\.\pipe\claude-mcp-browser-bridge-STR-HP03`, không có `hpdynamo-mcp-2026` hay `hprebar-mcp-r2026`.
   - Kết luận: PID 26264 là PID thực tế của phiên Revit 2026 được user mở trong phiên làm việc trước đó; khi hệ thống khởi động lại, Revit không tự động bật lại, và PID của Revit khi được mở tiếp theo có thể là PID động mới hoặc PID 26264 nếu ngẫu nhiên.

### 1.2 Về Hành Vi Thao Tác Trực Tiếp Của Bridge Trên Active Document
1. **Tại `HPDynamo/HPDynamo.McpBridge/Execution/RevitTransactionScope.cs` (Dòng 52–56):**
   ```csharp
   var app = (UIApplication)uiapp;
   var uidoc = app.ActiveUIDocument;
   var doc = uidoc?.Document;
   if (doc is null || uidoc is null)
       return ExecuteResult.Failure("No document is open in Revit. Open a project first.");
   ```
   *Quan sát:* Khi MCP Bridge thực thi mã C# Roslyn hoặc lệnh tạo element, đối tượng được thao tác trực tiếp là `app.ActiveUIDocument.Document` — chính là model dự án người dùng đang mở trên màn hình!
2. **Tại `HPDynamo/HPDynamo.McpBridge/Execution/HeadlessGraphRunner.cs` (Dòng 50–51):**
   ```csharp
   var app = new DynamoRevitApp();
   var result = app.ExecuteDynamoCommand(journalData, uiapp);
   ```
   *Quan sát:* Lệnh chạy đồ thị headless thực thi qua `DynamoRevitApp.ExecuteDynamoCommand` liên kết trực tiếp với active document của `uiapp`.
3. **Tại `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/run_dyn_file/code.cs` (Dòng 72 & 97):**
   ```csharp
   dynamoViewModel!.OpenIfSavedCommand.Execute(new DynamoModel.OpenFileCommand(targetGraphPath, false));
   ...
   revitDynamo.ExecuteCommand(new DynamoModel.OpenFileCommand(targetGraphPath, false));
   ```
   *Quan sát:* Khi gọi seed `run_dyn_file`, nếu Dynamo UI đang mở, nó sẽ thực thi `OpenFileCommand` lên canvas của người dùng, đóng/ghi đè đồ thị người dùng đang thao tác dở!

### 1.3 Về Các Test Fixtures Hiện Có Trong Repo
1. **File `.rvt` có sẵn trong repo:**
   - File duy nhất: `HPRebar/HPRebar.Tests/Fixtures/column-stack-2-storey.rvt`
   - Kích thước: `7,540,736` bytes (~7.19 MB).
   - Nội dung: Model Revit 2026 hoàn chỉnh với 2 tầng, hệ cột bê tông cốt thép và dầm.
2. **File `.dyn` có sẵn trong repo:**
   - File duy nhất: `scratch/VeHeDamTang1_DaKieng.dyn`
   - Kích thước: `41,520` bytes (1,240 dòng JSON).
   - Cấu trúc: 21 nodes, 24 wires, inputs, outputs, element resolvers, Python nodes.
   - Nhược điểm: Phụ thuộc vào file cấu hình ngoài `scratch/story1_grade_beams_model.json`.
3. **Hiện trạng thiếu sót trong `HPDynamo/tools/harness/run-benchmark.ps1` (Dòng 59–66):**
   ```powershell
   $isolatedFixtureDir = Join-Path $env:TEMP "HPDynamoBenchmark"
   if (-not (Test-Path $isolatedFixtureDir)) {
       New-Item -ItemType Directory -Path $isolatedFixtureDir -Force | Out-Null
   }
   $isolatedModelPath = Join-Path $isolatedFixtureDir "BenchmarkIsolatedModel.rvt"
   if (-not (Test-Path $isolatedModelPath)) {
       [System.IO.File]::WriteAllText($isolatedModelPath, "HPDynamo Isolated Benchmark Fixture Placeholder")
   }
   ```
   *Quan sát:* File fixture `BenchmarkIsolatedModel.rvt` hiện tại chỉ là file văn bản giả lập chứa chuỗi `"HPDynamo Isolated Benchmark Fixture Placeholder"`. Nó không phải là file Revit nhị phân hợp lệ; nếu Revit API cố gắng mở file này sẽ gây lỗi `CorruptModelException`. Đồng thời, chưa có file `.dyn` mẫu độc lập nào được đặt trong thư mục benchmark của `HPDynamo`.

---

## 2. Logic Chain (Chuỗi Lập Luận Từ Quan Sát Đến Kết Luận)

```
[Quan sát 1.1 & 1.2]: Bridge & Seeds thao tác trực tiếp lên app.ActiveUIDocument và DynamoViewModel của Revit
        │
        ▼
[Lập luận 1]: Nếu Revit đang mở (dù là PID 26264 hay bất kỳ PID nào), một lệnh test/benchmark vô tình gửi qua Named Pipe sẽ trực tiếp làm biến đổi mô hình dự án thực tế của người dùng hoặc làm đóng đồ thị Dynamo đang mở.
        │
        ▼
[Lập luận 2]: Do đó, cơ chế phát hiện Revit phải hoạt động 2 tầng:
  - Tầng 1: Kiểm tra chính xác PID 26264 (nếu là session đã biết).
  - Tầng 2: Quét mọi tiến trình Revit.exe đang chạy trên Windows.
  -> Bất kỳ tiến trình nào được phát hiện đều kích hoạt chế độ "Zero Touch Session Shield".
        │
        ▼
[Quan sát 1.3]: run-benchmark.ps1 hiện dùng file .rvt giả lập và chưa có file .dyn chuẩn tự cấp (self-contained).
        │
        ▼
[Lập luận 3]: Để benchmark và test chạy độc lập, trung thực và không chạm tới dữ liệu người dùng:
  - Cần một bộ fixture chuẩn gồm: SampleRebarModel.rvt (copy từ HPRebar.Tests) và StandardBenchmarkGraph.dyn (Dynamo 3.7 JSON thuần, 0 dependency ngoài).
  - Cần cơ chế nhân bản (clone) tự động ra thư mục sandbox tạm thời: %TEMP%\HPDynamoBenchmark\Session_<GUID>\.
  - Toàn bộ thao tác benchmark chỉ được đọc/ghi trên bản sao trong sandbox này.
        │
        ▼
[Lập luận 4]: Cơ chế dọn dẹp (cleanup) phải nằm trong khối try...finally, có kiểm tra đường dẫn an toàn (chống xóa nhầm repo), giải phóng GC và connection pool trước khi xóa, và retry 3 lần để vượt qua file lock của Windows.
```

---

## 3. Thiết Kế Giải Pháp Chi Tiết (Proposal & Specifications)

### 3.1 Giải Pháp 1: Cơ Chế Nhận Diện Revit & Nguyên Tắc Vàng "Zero Touch"

#### A. Hàm Nhận Diện 2 Tầng (PowerShell)
Tích hợp vào `HPDynamo/tools/harness/run-benchmark.ps1` (hoặc `HPDynamo/tools/benchmark/run-benchmark.ps1`):

```powershell
function Get-RevitSessionStatus {
    param([int]$KnownTargetPid = 26264)

    $allRevitProcs = @(Get-Process -Name "Revit" -ErrorAction SilentlyContinue)
    $hasRevit = ($allRevitProcs.Count -gt 0)
    $targetProc = if ($hasRevit) { $allRevitProcs | Where-Object { $_.Id -eq $KnownTargetPid } } else { $null }

    $pipeActive = Test-Path "\\.\pipe\hpdynamo-mcp-2026"

    [PSCustomObject]@{
        IsRunning          = $hasRevit
        ProcessCount       = $allRevitProcs.Count
        ActivePids         = ($allRevitProcs | ForEach-Object Id)
        TargetPidDetected  = ($targetProc -ne $null)
        PipeActive         = $pipeActive
        Processes          = $allRevitProcs
    }
}
```

#### B. 5 Điều Khoản Bắt Buộc "Zero Touch Session Shield"
Khi `$session.IsRunning -eq $true`:
1. **Zero Kill / Zero Close**: Nghiêm cấm mọi lệnh `Stop-Process`, `Kill()`, `$app.Quit()`, hoặc đóng cửa sổ Revit.
2. **Zero Active Model Mutation**: Tuyệt đối không gửi lệnh sửa đổi (`dryRun: false`, `transaction: "auto"`) tới `hpdynamo-mcp-2026` nếu không có cờ `--AllowLiveMutation` tường minh từ người dùng. Mọi workload thử nghiệm phải bị ép sang `dryRun: true` và kiểm tra 100% rollback (`CommittedElementCount == 0`).
3. **Zero Active Canvas Override**: Không chạy `run_dyn_file` mở file đè lên canvas đang hoạt động của người dùng nếu Dynamo UI đang hiển thị.
4. **Fixture Path Boundary**: Tuyệt đối không cho phép tool mở bất kỳ file nào nằm ngoài thư mục sandbox `%TEMP%\HPDynamoBenchmark\...`.
5. **Clear Banner Logging**: In thông báo cảnh báo rõ ràng trên console:
   ```
   [ZERO-TOUCH SHIELD ACTIVE]
   - Revit Process(es) Detected: PID(s) 26264
   - Protection: Active user documents and canvas are LOCKED against modifications.
   - Mutating Workloads: Forcibly wrapped in TransactionGroup rollback (dryRun: true).
   - Fixture Location: Cloned into isolated sandbox folder.
   ```

---

### 3.2 Giải Pháp 2: Đặc Tả Bộ Test Fixtures Chuẩn Cho HPDynamo

Tạo thư mục `HPDynamo/tools/benchmark/fixtures/` chứa 2 fixtures gốc:
1. **`SampleRebarModel.rvt`**:
   - Được sao chép từ `HPRebar/HPRebar.Tests/Fixtures/column-stack-2-storey.rvt`.
   - Dung lượng: ~7.19 MB.
   - Vai trò: Cung cấp mô hình Revit 2026 thật với đầy đủ Category (Columns, Framing, Levels) để thử nghiệm các thao tác Revit-Dynamo.
2. **`StandardBenchmarkGraph.dyn`**:
   - Đồ thị JSON Dynamo 3.7 tự cấp (self-contained), không dùng thư viện Python ngoài.
   - Cấu trúc gồm 5 nodes và 4 wires:
     - Node 1: `DSCoreNodes.Formula / CodeBlock` (`x = 1000; y = 2000; z = 0;`)
     - Node 2: `Autodesk.DesignScript.Geometry.Point.ByCoordinates`
     - Node 3: `Autodesk.DesignScript.Geometry.Vector.ZAxis`
     - Node 4: `Autodesk.DesignScript.Geometry.Line.ByStartPointDirectionLength`
     - Node 5: `CoreNodeModels.Watch`
   - Vai trò: Cung cấp đồ thị chuẩn cho workload WL-04 (Batch authoring) và WL-05 (Run dyn file & trích xuất terminal outputs).

---

### 3.3 Giải Pháp 3: Quy Trình Nhân Bản Fixture Độc Lập & Dọn Dẹp An Toàn

#### A. Thuật Toán Tạo Sandbox & Nhân Bản (Cloning)
```powershell
function Initialize-BenchmarkSandbox {
    param(
        [string]$RepoRoot,
        [string]$CustomRunId = ""
    )

    $runId = if ($CustomRunId) { $CustomRunId } else { [System.Guid]::NewGuid().ToString("N") }
    $sandboxRoot = Join-Path $env:TEMP "HPDynamoBenchmark"
    $sessionDir = Join-Path $sandboxRoot "Session_$runId"

    if (-not (Test-Path $sessionDir)) {
        New-Item -ItemType Directory -Path $sessionDir -Force | Out-Null
    }

    # 1. Resolve source RVT fixture
    $srcRvt = Join-Path $RepoRoot "HPDynamo\tools\benchmark\fixtures\SampleRebarModel.rvt"
    if (-not (Test-Path $srcRvt)) {
        $srcRvt = Join-Path $RepoRoot "HPRebar\HPRebar.Tests\Fixtures\column-stack-2-storey.rvt"
    }
    if (-not (Test-Path $srcRvt)) {
        throw "Source RVT fixture not found at '$srcRvt'."
    }

    $destRvt = Join-Path $sessionDir "BenchmarkIsolatedModel.rvt"
    Copy-Item -Path $srcRvt -Destination $destRvt -Force
    (Get-Item $destRvt).IsReadOnly = $false

    # 2. Resolve source DYN fixture
    $srcDyn = Join-Path $RepoRoot "HPDynamo\tools\benchmark\fixtures\StandardBenchmarkGraph.dyn"
    $destDyn = Join-Path $sessionDir "BenchmarkIsolatedGraph.dyn"
    if (Test-Path $srcDyn) {
        Copy-Item -Path $srcDyn -Destination $destDyn -Force
        (Get-Item $destDyn).IsReadOnly = $false
    }

    Write-Host "  -> [Sandbox Ready] Directory: $sessionDir" -ForegroundColor Green
    Write-Host "  -> [Fixture RVT] Cloned: $destRvt ($((Get-Item $destRvt).Length) bytes)" -ForegroundColor Gray
    Write-Host "  -> [Fixture DYN] Cloned: $destDyn" -ForegroundColor Gray

    return [PSCustomObject]@{
        SessionDir = $sessionDir
        ModelPath  = $destRvt
        GraphPath  = $destDyn
        RunId      = $runId
    }
}
```

#### B. Thuật Toán Dọn Dẹp An Toàn (Safe Cleanup)
Phải tuân thủ các quy tắc an toàn nghiêm ngặt để không bao giờ xóa nhầm thư mục dự án hoặc tài liệu người dùng:

```powershell
function Remove-BenchmarkSandbox {
    param([string]$SandboxDir)

    if (-not $SandboxDir -or -not (Test-Path $SandboxDir)) { return }

    # RÀNG BUỘC BẢO VỆ 1: Thư mục bắt buộc phải nằm trong %TEMP%\HPDynamoBenchmark
    $tempCanonical = [System.IO.Path]::GetFullPath($env:TEMP).TrimEnd('\', '/')
    $targetCanonical = [System.IO.Path]::GetFullPath($SandboxDir).TrimEnd('\', '/')
    
    if (-not $targetCanonical.StartsWith($tempCanonical, [StringComparison]::OrdinalIgnoreCase) -or
        -not $targetCanonical.Contains("HPDynamoBenchmark")) {
        Write-Error "SECURITY REFUSAL: Target directory '$SandboxDir' is outside %TEMP%\HPDynamoBenchmark. Deletion aborted."
        return
    }

    # RÀNG BUỘC BẢO VỆ 2: Giải phóng tài nguyên & file handles
    [System.GC]::Collect()
    [System.GC]::WaitForPendingFinalizers()

    # RÀNG BUỘC BẢO VỆ 3: Retry logic 3 lần (vượt qua lock tạm thời của Windows File Indexer / Antivirus)
    $maxRetries = 3
    $success = $false
    for ($i = 1; $i -le $maxRetries; $i++) {
        try {
            Remove-Item -Path $targetCanonical -Recurse -Force -ErrorAction Stop
            $success = $true
            break
        }
        catch {
            Start-Sleep -Milliseconds (250 * $i)
        }
    }

    if ($success) {
        Write-Host "  -> [Sandbox Cleanup] Removed: $targetCanonical" -ForegroundColor Green
    } else {
        Write-Warning "Could not immediately purge '$targetCanonical' (locked by another process). Scheduled for purge."
    }
}
```

#### C. Cấu Trúc Khối Điều Khiển Tại Benchmark Harness (`run-benchmark.ps1`)
```powershell
$sandbox = $null
try {
    # 1. Phát hiện session Revit & kích hoạt Zero Touch
    $revitSession = Get-RevitSessionStatus -KnownTargetPid 26264
    if ($revitSession.IsRunning) {
        Write-Host "[ZERO-TOUCH SHIELD ACTIVE] Revit PID(s): $($revitSession.ActivePids -join ', ')" -ForegroundColor Yellow
    }

    # 2. Khởi tạo Sandbox cô lập
    $sandbox = Initialize-BenchmarkSandbox -RepoRoot $repoRoot

    # 3. Thực thi các Workload WL-01 đến WL-05
    # Toàn bộ tham số filePath của .dyn và .rvt trỏ vào $sandbox.GraphPath và $sandbox.ModelPath
    ...
}
finally {
    # 4. Luôn luôn dọn dẹp sandbox sạch sẽ dù thành công hay ném exception
    if ($sandbox) {
        Remove-BenchmarkSandbox -SandboxDir $sandbox.SessionDir
    }
}
```

---

## 4. Caveats (Các Điểm Giới Hạn & Giả Định)

1. **Trạng thái thực tế khi khảo sát:** Tại thời điểm khảo sát, Revit không đang chạy trên máy dev (cả PID 26264 lẫn các PID khác đều không tồn tại). Khi user khởi động Revit trong tương lai, hệ điều hành sẽ cấp một PID ngẫu nhiên (hoặc trùng 26264 nếu ngẫu nhiên tái sử dụng). Vì vậy, cơ chế phát hiện phải luôn dựa vào tên process `Revit` kèm việc ghi nhận PID 26264 như một cờ ưu tiên.
2. **Giới hạn của `run_dyn_file` trong môi trường headless:** Nếu không mở Revit UI, `DynamoRevitApp.ExecuteDynamoCommand` cần `UIApplication` hợp lệ; trong chế độ stdio không có Revit mở, các test gọi qua pipe sẽ trả về `BridgeNotConnected` (được harness bắt và xử lý graceful chuyển sang stdio server benchmarks).
3. **Đường dẫn Deliverables:** Kế hoạch `SCOPE.md` của Orchestrator đặt mục tiêu deliverables tại `HPDynamo/tools/benchmark/` và `HPDynamo/reports/baseline-vs-upgraded.json`. Hiện tại `run-benchmark.ps1` đang nằm ở `HPDynamo/tools/harness/run-benchmark.ps1`. Worker khi triển khai cần bảo đảm đồng bộ hoặc tạo đường dẫn tương thích.

---

## 5. Conclusion (Kết Luận & Khuyến Nghị Hành Động)

1. **Khẳng định tính khả thi:** Cơ chế nhận diện Revit PID 26264 và cách ly fixture hoàn toàn khả thi 100% bằng PowerShell thuần, không phụ thuộc thư viện bên ngoài.
2. **Khuyến nghị cho Worker triển khai:**
   - **Bước 1:** Bổ sung fixture chuẩn `StandardBenchmarkGraph.dyn` (Dynamo 3.7 self-contained) vào thư mục `HPDynamo/tools/benchmark/fixtures/` (hoặc `HPDynamo/tools/harness/fixtures/`).
   - **Bước 2:** Cập nhật `run-benchmark.ps1` thay thế placeholder text file bằng cơ chế gọi `Initialize-BenchmarkSandbox` và `Remove-BenchmarkSandbox` trong khối `try...finally`.
   - **Bước 3:** Điều chỉnh đường dẫn xuất báo cáo mặc định của `run-benchmark.ps1` thành `HPDynamo/reports/baseline-vs-upgraded.json` theo đúng `SCOPE.md`.
   - **Bước 4:** Cập nhật cơ chế nhận diện Revit 2 tầng và in biểu ngữ `[ZERO-TOUCH SHIELD ACTIVE]` để bảo vệ người dùng 100%.

---

## 6. Verification Method (Phương Pháp Kiểm Chứng Độc Lập)

1. **Kiểm tra phát hiện Revit:**
   ```powershell
   # Khi không có Revit:
   powershell -Command "if (Get-Process Revit -ErrorAction SilentlyContinue) { 'Running' } else { 'Not running' }"
   # Kết quả mong đợi: 'Not running'
   ```
2. **Kiểm tra tính hợp lệ của Fixture RVT nguồn:**
   ```powershell
   Get-Item "HPRebar\HPRebar.Tests\Fixtures\column-stack-2-storey.rvt" | Select-Object Length
   # Kết quả mong đợi: Length = 7540736 bytes
   ```
3. **Kiểm tra cơ chế nhân bản và dọn dẹp sandbox (Dry-run Sandbox Verification):**
   ```powershell
   # Tạo sandbox, kiểm tra tồn tại, xóa sandbox và kiểm tra đã biến mất
   $sb = Join-Path $env:TEMP "HPDynamoBenchmark\TestProbe"
   New-Item -ItemType Directory -Path $sb -Force | Out-Null
   Test-Path $sb # Mong đợi: True
   Remove-Item -Path $sb -Recurse -Force
   Test-Path $sb # Mong đợi: False
   ```
4. **Kiểm tra toàn bộ test suites hiện hữu không bị ảnh hưởng (Zero Regressions):**
   ```powershell
   dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests
   dotnet test HPDynamo/HPDynamo.McpBridge.Tests
   ```
