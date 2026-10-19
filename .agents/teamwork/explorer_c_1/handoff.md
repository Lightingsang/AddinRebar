# Handoff Report: Khảo sát Hiện trạng Xử lý Lỗi Benchmark Harness HPDynamo (R2-08)

**Tác giả:** explorer_c_1  
**Ngày:** 2026-10-09  
**Nhiệm vụ:** Khảo sát hiện trạng xử lý lỗi trong benchmark harness của HPDynamo (`run-benchmark.ps1`), tìm lỗ hổng nuốt lỗi / false positive, và đề xuất giải pháp xử lý FAIL/SKIP rõ ràng cho Worker.  
**Thư mục làm việc:** `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_c_1`

---

## 1. Observation (Quan Sát Thực Nghiệm Trực Tiếp)

### 1.1. Mã nguồn hiện tại của `HPDynamo/tools/harness/run-benchmark.ps1`
1. **Kiểm tra thành công ngây thơ tại `Invoke-McpTool` (dòng 131–143):**
   ```powershell
   131:         $sw = [System.Diagnostics.Stopwatch]::StartNew()
   132:         $output = & python $pythonScript $Exe tools/call $ToolName "@$tempArgs" 2>&1
   133:         $sw.Stop()
   134: 
   135:         $durationMs = [Math]::Round($sw.Elapsed.TotalMilliseconds, 1)
   136:         $respText = $output -join [Environment]::NewLine
   137:         $respBytes = [System.Text.Encoding]::UTF8.GetByteCount($respText)
   138:         $isSuccess = ($LASTEXITCODE -eq 0) -and ($respText -notmatch 'Traceback \(most recent call last\):')
   139: 
   140:         return [ordered]@{
   141:             DurationMs = $durationMs; RequestBytes = $reqBytes
   142:             ResponseBytes = $respBytes; ResponseText = $respText; Success = $isSuccess
   143:         }
   ```
   - Lệnh gọi Python chạy script `McpShared\tools\mcp-call.py`.
   - Dòng 138 chỉ kiểm tra: tiến trình Python thoát với exit code 0 (`$LASTEXITCODE -eq 0`) và không có chuỗi `"Traceback (most recent call last):"` trong text output.
   - Không hề deserialize JSON từ `$respText` để kiểm tra JSON-RPC error, `isError: true` hay domain error.

2. **Lặp mù quáng và bỏ qua kết quả `Success` tại `Measure-Workload` (dòng 160–178):**
   ```powershell
   160:     Write-Host "  -> [$WorkloadId] $WorkloadName ($ToolName)..." -NoNewline
   161:     $coldLatencies = @()
   162:     $lastResult = $null
   163:     for ($c = 1; $c -le $ColdCount; $c++) {
   164:         $res = Invoke-McpTool -Exe $ServerExe -ToolName $ToolName -Arguments $Arguments
   165:         $coldLatencies += $res.DurationMs
   166:         $lastResult = $res
   167:     }
   168: 
   169:     $warmLatencies = @()
   170:     for ($w = 1; $w -le $WarmCount; $w++) {
   171:         $res = Invoke-McpTool -Exe $ServerExe -ToolName $ToolName -Arguments $Arguments
   172:         $warmLatencies += $res.DurationMs
   173:         $lastResult = $res
   174:     }
   175: 
   176:     $stats = Compute-LatencyStats $warmLatencies
   177:     Write-Host " p50: $($stats.p50)ms, p95: $($stats.p95)ms, Req/Resp: $($lastResult.RequestBytes)B/$($lastResult.ResponseBytes)B" -ForegroundColor Green
   ```
   - Trường `$res.Success` hoàn toàn **KHÔNG ĐƯỢC SỬ DỤNG**.
   - Dù `$res.Success` là `$false` hoặc server trả về lỗi từ lần gọi đầu tiên, vòng lặp vẫn tiếp tục chạy mù quáng toàn bộ 10 warm runs.
   - Thời gian phản hồi lỗi (khoảng 800ms) được nạp vào `$warmLatencies` và tính p50/p95 như thể đó là hiệu năng thực thi của tool.
   - Dòng 177 in kết quả màu xanh lá (`-ForegroundColor Green`) vô điều kiện, bất kể thành công hay thất bại.

3. **Gán cứng nhãn thành công và kết quả đo trong JSON Report (dòng 235–266):**
   ```powershell
   235:     summary = [ordered]@{
   236:         roundTripsReductionAuthoring = "90.0%"
   237:         evaluationCallsReductionAuthoring = "90.0%"
   238:         dataIntegrityCollectionSuccess = "100%"
   239:         dictionaryExtractionSuccess = "100%"
   240:     }
   ```
   - Dòng 238–239: `dataIntegrityCollectionSuccess = "100%"` và `dictionaryExtractionSuccess = "100%"` là các chuỗi ký tự gán cứng (literal strings), hoàn toàn không tính toán từ kết quả WL-05.
   - Dòng 245, 250, 255, 260: Tình trạng `status` của các workload được gán cứng là `"Verified Live (Read-Only)"`, `"Verified Live (Preflight Guard)"`, `"Verified Live (dryRun: true Rollback Protected)"`.
   - Dòng 263–266 (WL-05 `get_node_output_values`): Kết quả đo `$wl05_exec` bị vứt bỏ hoàn toàn. Báo cáo gán cứng:
     ```powershell
     upgraded = [ordered]@{ collectionIntegrity = "100% Passed (Extracted recursive JSON array)"; dictionaryIntegrity = "100% Passed (Extracted JSON object with Keys/Values)"; status = "Verified with ProtoCore & DesignScriptBuiltin" }
     ```

4. **In thông báo thành công vô điều kiện và không có exit code lỗi (dòng 281):**
   ```powershell
   281: Write-Host "`nBenchmark verification complete (All 5 workloads passed)." -ForegroundColor Cyan
   ```
   - Câu `"All 5 workloads passed"` luôn được in ra dù không có workload nào chạy thành công.
   - Script không có lệnh `exit 1` khi có lỗi. Script luôn kết thúc với exit code 0.

5. **Cách ly giả lập (Fake Model Isolation) (dòng 63–66):**
   ```powershell
   63: $isolatedModelPath = Join-Path $isolatedFixtureDir "BenchmarkIsolatedModel.rvt"
   64: if (-not (Test-Path $isolatedModelPath)) {
   65:     [System.IO.File]::WriteAllText($isolatedModelPath, "HPDynamo Isolated Benchmark Fixture Placeholder")
   66: }
   ```
   - Tệp `$isolatedModelPath` chỉ là một file text chứa nội dung `"HPDynamo Isolated Benchmark Fixture Placeholder"`. Tệp này không phải file Revit thật, không bao giờ được truyền vào lệnh hay được Revit mở. Tool calls vẫn nhắm vào pipe mặc định của phiên Revit đang chạy.

6. **Tham số chết `$DryRun` (dòng 12):**
   - Được khai báo trong `param([switch]$DryRun)` ở dòng 12 nhưng hoàn toàn không xuất hiện ở bất kỳ dòng mã nào khác trong file.

### 1.2. Bằng chứng thực nghiệm (Empirical Proof)
Đã chạy lệnh kiểm chứng trực tiếp khi named pipe `\\.\pipe\hpdynamo-mcp-2026` đang offline:
- **Lệnh 1:** `python McpShared/tools/mcp-call.py HPDynamo/HPDynamo.Mcp.Server/bin/Debug/net10.0/HPDynamo.Mcp.Server.exe tools/call get_dynamo_context '{"includeSelection": false}'`
  - **Kết quả trả về:**
    ```json
    {
      "result": {
        "content": [
          {
            "type": "text",
            "text": "Dynamo bridge not connected. Open Revit 2026 with Dynamo and ensure HPDynamo MCP Bridge is running (pipe hpdynamo-mcp-2026)."
          }
        ],
        "isError": true
      },
      "id": 2,
      "jsonrpc": "2.0"
    }
    ```
  - **Exit Code của Python:** `0`
  - **Đánh giá logic của `run-benchmark.ps1`:**
    `$LASTEXITCODE -eq 0` $\rightarrow$ **True**.
    `$respText -notmatch 'Traceback'` $\rightarrow$ **True**.
    $\rightarrow$ `$isSuccess` được tính là **`$true`**! Lỗi kết nối bridge bị biến thành thành công!

- **Lệnh 2:** Xem file kết quả đã tạo trước đó `plans/261009-1118-hpdynamo-mcp-performance/reports/baseline-vs-upgraded.json`:
  - Dòng 87: WL-02 trả về response chỉ vỏn vẹn **133 bytes** (chính là độ dài thông báo lỗi bridge offline).
  - Dòng 155: WL-04 trả về response chỉ vỏn vẹn **128 bytes** (chính là thông báo lỗi bridge offline).
  - Nhưng dòng 164 vẫn ghi: `status: "Verified Live (dryRun: true Rollback Protected)"`!
  - Summary vẫn ghi: `dictionaryExtractionSuccess: "100%"`, `dataIntegrityCollectionSuccess: "100%"`!

---

## 2. Logic Chain (Chuỗi Lập Luận Suy Luận)

1. **Từ Quan sát 1.1 §1 và 1.2:**
   Khi server trả về lỗi JSON-RPC (`"error": { ... }`) hoặc lỗi MCP (`"isError": true`):
   - `mcp-call.py` đọc phản hồi hợp lệ qua stdout, parse JSON và in ra màn hình bình thường, sau đó thoát với exit code 0.
   - `run-benchmark.ps1` chỉ kiểm tra `$LASTEXITCODE -eq 0` và không thấy `Traceback`.
   - **Hệ quả 1:** Harness kết luận `$isSuccess = $true`. Cả lỗi JSON-RPC và lỗi MCP protocol đều bị nuốt trọn ở bước đầu tiên.

2. **Từ Quan sát 1.1 §2:**
   Khi một workload gọi `Measure-Workload`:
   - Hàm này không kiểm tra `$res.Success`.
   - Vòng lặp `for ($w = 1; $w -le $WarmCount; $w++)` tiếp tục chạy đủ 10 lần.
   - Mỗi lần chạy, server từ chối ngay lập tức vì không có pipe, trả về chuỗi lỗi 128 bytes trong khoảng 800ms (thời gian khởi động tiến trình).
   - Hàm gom toàn bộ thời gian trả về lỗi này để tính `p50` và `p95`, rồi in ra dòng chữ xanh lá `-ForegroundColor Green`.
   - **Hệ quả 2:** Latency của thông báo lỗi thất bại bị ngụy tạo thành "latency của workload khi tối ưu", đánh lừa người vận hành rằng chức năng đã chạy cực nhanh.

3. **Từ Quan sát 1.1 §3 và 1.1 §4:**
   Khi tập hợp JSON report và kết thúc kịch bản:
   - Các trường phần trăm tối ưu (90%), độ toàn vẹn dữ liệu (100%), và trạng thái live ("Verified Live") là chuỗi gán cứng trong file `.ps1`, không hề phụ thuộc vào kết quả thực tế của lệnh gọi.
   - Kết quả thực tế của WL-05 bị bỏ qua hoàn toàn.
   - Cuối script in `"All 5 workloads passed"` và trả về exit code 0.
   - **Hệ quả 3:** Đây là lỗ hổng False Positive nghiêm trọng (báo Passed khi hệ thống FAILED 100%).

4. **Từ Quan sát 1.1 §1 (dòng 132) và đối chiếu với `mcp-session.py`:**
   - Mỗi lần gọi `Invoke-McpTool`, script thực thi `python mcp-call.py`, spawn một tiến trình .NET `HPDynamo.Mcp.Server.exe` mới.
   - Mỗi lần chạy warm run mất ~800ms là do chi phí khởi tạo CLR, JIT và MCP handshake, chứ không phải thời gian thực thi của bridge.
   - **Hệ quả 4:** Kịch bản không hề đo được "Warm runs" (phiên làm việc liên tục / cache Roslyn), vi phạm nguyên tắc đo lường đã nêu trong `benchmark-plan.md`.

5. **Từ Quan sát 1.1 §5 và Chỉ thị An toàn Revit PID 26264:**
   - Script không hề có cơ chế mở model cô lập trong Revit hay Dynamo.
   - Việc ghi một file text rỗng vào `$env:TEMP` không bảo vệ được model của người dùng nếu bridge thực sự nhận lệnh mutation.

---

## 3. Caveats (Ranh Giới & Giả Định)

1. **Môi trường Live Bridge:** Tại thời điểm khảo sát, named pipe `\\.\pipe\hpdynamo-mcp-2026` đang offline và phiên Revit PID 26264 của người dùng đang được bảo vệ nghiêm ngặt (Zero Touch, không can thiệp hay inject code). Khảo sát được thực hiện 100% read-only qua phân tích mã nguồn và kiểm thử giao tiếp stdio của `HPDynamo.Mcp.Server.exe`.
2. **Hạ tầng McpShared:** Thư viện `McpShared/tools/mcp-session.py` và `harness_common.py` đã có sẵn logic phân tích `parse_tool_result` và kiểm tra `ok(r)` rất chuẩn xác; lỗi nằm ở việc script `run-benchmark.ps1` tự viết lại logic kiểm tra kết quả một cách ngây thơ bằng PowerShell thay vì tái sử dụng hạ tầng dùng chung của repo.

---

## 4. Conclusion (Kết Luận & Đề Xuất Giải Pháp Cho Worker)

### Kết luận
Benchmark harness `HPDynamo/tools/harness/run-benchmark.ps1` hiện tại chứa các lỗ hổng nghiêm trọng:
1. **Nuốt toàn bộ lỗi**: Không kiểm tra JSON-RPC error, không kiểm tra MCP `isError=true`, không kiểm tra domain error trong payload text.
2. **False Positive 100%**: Khi không có kết nối bridge hoặc tool throw exception, harness vẫn in xanh lá, gán cứng nhãn `"Verified Live"`, `"100% Passed"`, in `"All 5 workloads passed"`, và thoát với exit code 0.
3. **Đo sai Warm Latency**: Gọi process mới cho mỗi warm run thay vì duy trì persistent session.
4. **Cô lập mô hình giả lập**: File test placeholder không có giá trị cô lập thực tế.

---

### Đề xuất giải pháp cụ thể cho Worker (Actionable Blueprint for Worker)

#### Bước 1: Xây dựng cơ chế thẩm định lỗi 3 tầng (Three-Tier Result Validator)
Worker cần viết hàm kiểm tra kết quả trả về từ MCP Server theo 3 tầng nghiêm ngặt:
1. **Tầng 1 (JSON-RPC Error):**
   - Parse `$respText` thành JSON object.
   - Nếu tồn tại thuộc tính `error` (hoặc `rpcError = true`):
     $\rightarrow$ Trích xuất `$json.error.code`, `$json.error.message`.
     $\rightarrow$ Kết luận: **FAIL** (hoặc **SKIP** nếu mã lỗi là Bridge Unavailable trong chế độ offline). Dừng ngay lập tức.
2. **Tầng 2 (MCP Protocol Error):**
   - Nếu `$json.result.isError -eq $true`:
     $\rightarrow$ Trích xuất toàn bộ text từ `$json.result.content`.
     $\rightarrow$ Nếu nội dung chứa `"bridge not connected"` hoặc tương đương và harness đang chạy ở chế độ offline / no-bridge: Đánh dấu **SKIP** kèm lý do rõ ràng.
     $\rightarrow$ Nếu là lỗi khác (ví dụ: compiler error, validation error, exception): Đánh dấu **FAIL**. Dừng ngay lập tức.
3. **Tầng 3 (Domain / Content Failure):**
   - Parse `$json.result.content[0].text` nếu nó là chuỗi JSON:
     - Nếu có thuộc tính `success -eq $false` hoặc `errors` chứa phần tử: Đánh dấu **FAIL**.
     - Đối với WL-04 (`batch_edit_graph`): Phải kiểm tra số lượng node tạo được > 0 và `rolledBack -eq $true` (khi `dryRun = true`).
     - Đối với WL-05 (`get_node_output_values`): Phải kiểm tra dữ liệu trích xuất thực tế (mảng JSON hoặc object JSON), không được chứa thông báo lỗi.

#### Bước 2: Cơ chế Dừng Vòng Lặp & Phân Định FAIL/SKIP (Fail-Fast Loop Control)
Trong hàm `Measure-Workload`:
1. Chạy lượt đầu tiên (Cold run hoặc Test probe).
2. Gọi hàm thẩm định 3 tầng ở Bước 1:
   - **Nếu kết quả là SKIP:**
     - Ghi nhận trạng thái `Status = "SKIPPED"`, `Reason = <chi tiết>`.
     - In ra màn hình: `[SKIP] <WorkloadName>: <Reason>` bằng màu Vàng (`-ForegroundColor Yellow`).
     - **KHÔNG CHẠY TIẾP** các warm runs còn lại. Không tính latency giả mạo.
   - **Nếu kết quả là FAIL:**
     - Ghi nhận trạng thái `Status = "FAILED"`, `Error = <chi tiết>`.
     - In ra màn hình: `[FAIL] <WorkloadName>: <Error>` bằng màu Đỏ (`-ForegroundColor Red`).
     - **DỪNG VÒNG LẶP NGAY LẬP TỨC**. Không chạy tiếp 10 warm runs để nạp latency lỗi.
   - **Nếu kết quả là PASS:**
     - Cho phép tiếp tục thực hiện các warm runs.
     - Sau khi hoàn tất, in `[PASS]` bằng màu Xanh lá (`-ForegroundColor Green`).

#### Bước 3: Nâng cấp Warm Runs sử dụng Persistent MCP Session
Để đo lường trung thực tốc độ warm run (tránh chi phí spawn process ~800ms mỗi lần):
- Tái sử dụng `McpShared/tools/mcp-session.py` và `harness_common.py`.
- Tạo một helper script (ví dụ `HPDynamo/tools/harness/benchmark_runner.py` hoặc PowerShell gọi persistent session):
  - Khởi động tiến trình server một lần duy nhất qua class `Server`.
  - Thực hiện handshake `initialize`.
  - Giữ session mở và gửi N requests liên tiếp qua cùng một stdio stream.
  - Đo thời gian round-trip từng request thực tế trên session ấm.
  - Đóng session khi hoàn tất.

#### Bước 4: Lắp ráp Báo cáo JSON Trung thực & Thiết lập Exit Code Chuẩn
1. **Bỏ toàn bộ các giá trị gán cứng:**
   - Không gán cứng `dataIntegrityCollectionSuccess = "100%"`. Nếu WL-05 bị SKIP hoặc FAIL, ghi nhận `"Not Verified (Skipped/Failed)"`.
   - `status` của từng workload phải lấy động từ biến `$exec.Status`: `"Verified Live"`, `"Skipped (Bridge Offline)"`, hoặc `"Failed: <Error>"`.
   - Lưu trữ danh sách mẫu đo thô (`rawSamples: [...]`) để có thể kiểm toán khách quan.
2. **Tổng kết và Exit Code:**
   - Đếm tổng số: `$passedCount`, `$failedCount`, `$skippedCount`.
   - In bảng tổng kết rõ ràng: `Summary: Passed=$passedCount, Failed=$failedCount, Skipped=$skippedCount`.
   - Nếu `$failedCount -gt 0`: In thông báo thất bại bằng màu Đỏ và gọi `exit 1`.
   - Nếu `$passedCount -gt 0 -and $failedCount -eq 0`: In thông báo thành công và gọi `exit 0`.
   - Nếu toàn bộ bị SKIP: In thông báo cảnh báo và gọi `exit 0` (hoặc exit code riêng cho skip) kèm ghi chú không được dùng kết quả này làm bằng chứng live.

---

## 5. Verification Method (Phương Pháp Kiểm Chứng Độc Lập)

Người tiếp nhận (Worker / Reviewer) có thể kiểm chứng độc lập báo cáo này bằng các phương pháp sau:

1. **Kiểm tra trực quan mã nguồn:**
   - Mở file `HPDynamo/tools/harness/run-benchmark.ps1`:
     - Kiểm tra dòng 138: xem định nghĩa `$isSuccess`.
     - Kiểm tra dòng 163–174: xem vòng lặp for không kiểm tra `$res.Success`.
     - Kiểm tra dòng 235–266: xem các chuỗi gán cứng `"100%"`, `"Verified Live"`.
     - Kiểm tra dòng 281: xem dòng `"All 5 workloads passed"`.

2. **Tái hiện lỗi nuốt phản hồi lỗi:**
   Chạy lệnh sau trong PowerShell (khi không bật bridge Dynamo):
   ```powershell
   python McpShared/tools/mcp-call.py HPDynamo/HPDynamo.Mcp.Server/bin/Debug/net10.0/HPDynamo.Mcp.Server.exe tools/call get_dynamo_context '{"includeSelection": false}'
   Write-Host "Python Exit Code: $LASTEXITCODE"
   ```
   *Điều kiện kiểm chứng:* Python in JSON chứa `"isError": true` nhưng exit code vẫn bằng `0`.

3. **Chạy thử kịch bản hiện tại và quan sát false positive:**
   Chạy lệnh:
   ```powershell
   pwsh -NoProfile -File HPDynamo/tools/harness/run-benchmark.ps1 -WarmRuns 2 -ColdRuns 1
   ```
   *Điều kiện kiểm chứng:*
   - Toàn bộ 5 workloads đều in chữ xanh lá (`Green`) dù bridge hoàn toàn không chạy.
   - Cuối cùng in `"Benchmark verification complete (All 5 workloads passed)."`
   - File JSON sinh ra tại `plans/261009-1118-hpdynamo-mcp-performance/reports/baseline-vs-upgraded.json` ghi nhận status `"Verified Live"` và `"100% Passed"`.
