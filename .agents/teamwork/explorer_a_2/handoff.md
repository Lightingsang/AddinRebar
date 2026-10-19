# Báo Cáo Bàn Giao (Handoff Report) — explorer_a_2

**Agent**: explorer_a_2  
**Hạng mục**: Milestone A (R2-04, R2-05, R2-06)  
**Loại bàn giao**: Hard Handoff (Khảo sát hoàn tất, đầy đủ 5 thành phần)

---

## 1. Observation (Quan Sát Thực Nghiệm)

1. **R2-04: Coordinator ghi đè kết quả transaction**:
   - `HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoBridgeCoordinator.cs`:
     - Dòng 107–109:
       ```csharp
       var result = await DispatchExecutionAsync(request, progress, cancel).ConfigureAwait(false);
       if (cancel.IsCancellationRequested) throw new OperationCanceledException(cancel.Token);
       return Finish(request, result, sw);
       ```
     - Dòng 120–125:
       ```csharp
       catch (OperationCanceledException)
       {
           var res = ExecuteResult.Failure("Execution was cancelled.");
           res.RolledBack = true;
           return Finish(request, res, sw);
       }
       ```
   - Trong `RevitTransactionScope.cs` (dòng 145, 254), khi script chạy thành công không có ngoại lệ và `!request.DryRun`, Revit `TransactionGroup` và auto transaction **đã commit thực tế vào Revit DB** (`AssimilateGroup` và `CommitAutoTransaction`). `ExecuteResult` được tạo với `RolledBack = false` và `Changed` có số lượng elements thực tế.
   - `HPDynamo/HPDynamo.McpBridge.Tests/Coordinator/Milestone3CorrelatedCancelStressTests.cs` (dòng 32–56): Mock hook trả về `{ IsError = false, Message = "Success" }`, nhưng test lại assert `Assert.True(result.RolledBack);` vì dòng 108 ném ngoại lệ và dòng 123 ghi đè `res.RolledBack = true`.

2. **R2-05: Bất nhất Evaluation Sync & Error Handling**:
   - `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/batch_edit_graph/code.cs` (dòng 262–265):
     ```csharp
     if (runEvaluation && homeWs != null && !dryRun)
     {
         homeWs.Run();
     }
     ```
     Gọi `homeWs.Run()` mà không chờ `EvaluationCompleted`, không kiểm tra lỗi node, trả về `success = true` (dòng 285).
   - `HPDynamo/HPDynamo.McpBridge/Execution/HeadlessGraphRunner.cs`:
     - Dòng 108–118: `ExecuteDesignScript` gọi `ws.Run()` đồng bộ, không dùng `RunAndWait`, không kiểm tra `cbn.State == ElementState.Error`, xóa node lập tức và luôn trả về `{ Success = true, Outputs = outputs }`.
     - Dòng 50–68: `RunDynFile` nếu `ws == null` thì gán `errorCount = 0` và không ném ngoại lệ, trả về trạng thái bình thường.
   - `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/run_dyn_file/code.cs`:
     - Dòng 91–108 (Headless branch): Không đưa `inputs` vào `journalDict["dynModelNodesInfo"]`, không chờ evaluation, không trích xuất outputs, và nếu `headlessWorkspace == null` thì trả về `errorNodesCount = 0` dẫn đến `success = true`.
     - Dòng 114 (Canvas branch): Khi có node bị lỗi (`errorNodesCount > 0`), seed chỉ trả về `{ success = false }` trong anonymous object mà không ném ngoại lệ, khiến `RevitTransactionScope` commit toàn bộ thay đổi lỗi dở dang vào Revit DB.

3. **R2-06: Dynamo Scheduler chưa dừng khi Timeout/Cancel**:
   - `HPDynamo/HPDynamo.McpBridge/Execution/DynamoEvaluationSync.cs`:
     - Dòng 38: `using var registration = linkedCts.Token.Register(() => frame.Continue = false);`
     - Dòng 41: `runAction();` được gọi trực tiếp mà không kiểm tra xem `linkedCts.Token.IsCancellationRequested` đã bật hay chưa.
     - Khi cancel/timeout xảy ra, `frame.Continue = false` làm thoát `Dispatcher.PushFrame(frame)`, hàm trả về `false` và dòng 57 hủy đăng ký `model.EvaluationCompleted`. Không hề có lệnh yêu cầu dừng hay kiểm tra Dynamo scheduler.
   - `HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoBridgeCoordinator.cs`:
     - Dòng 133: `finally { Volatile.Write(ref _busy, 0); StateChanged?.Invoke(); }` giải phóng `_busy` ngay lập tức khi khối execution kết thúc.
   - Kiểm tra siêu dữ liệu `DynamoCore.dll` qua ECMA-335:
     `Dynamo.Scheduler.DynamoScheduler` sở hữu các thuộc tính:
     - `public bool HasTaskInProgress { get; }` (RVA 0x9784)
     - `public bool HasPendingTasks { get; }` (RVA 0x980C)
     - `public IEnumerable<AsyncTask> Tasks { get; }` (RVA 0x973C)
     - `public void CompactTaskQueue()` (RVA 0x9888)
     Trong khi các tác vụ `UpdateGraphAsyncTask` vẫn đang thực thi trên scheduler thread, coordinator đã giải phóng `_busy = 0`.

---

## 2. Logic Chain (Chuỗi Lập Luận)

1. **Từ Observation 1 suy ra lỗi R2-04**:
   - `RevitTransactionScope` là execution boundary trực tiếp quản lý Revit `TransactionGroup`. Nếu code đã chạy xong và commit thành công, DB đã thay đổi.
   - Việc `DynamoBridgeCoordinator` kiểm tra `cancel.IsCancellationRequested` tại dòng 108 và ném `OperationCanceledException` đã hủy bỏ kết quả thực tế.
   - Khối `catch` tạo mới `ExecuteResult` với `RolledBack = true` là một khẳng định sai sự thật, làm sai lệch báo cáo trạng thái giao dịch gửi về AI/caller.
   - Kết luận: Transaction boundary là nguồn sự thật duy nhất. Dòng 108 phải bị loại bỏ và kết quả thực tế từ boundary phải được bảo tồn.

2. **Từ Observation 2 suy ra lỗi R2-05**:
   - `RevitTransactionScope` chỉ rollback khi phát sinh ngoại lệ hoặc `request.DryRun == true`.
   - Cả 3 mode (`batch_edit_graph`, `HeadlessGraphRunner`, `run_dyn_file`) khi gặp lỗi đánh giá hoặc lỗi node (`ElementState.Error`) lại không ném ngoại lệ mà âm thầm trả về kết quả hoặc bỏ qua, khiến Revit transaction bị commit sai lầm.
   - Nhánh Headless của `run_dyn_file` làm rơi rụng tham số `inputs` và báo `success = true` ngay cả khi file không load được (`ws == null`).
   - Kết luận: Cần thống nhất quy trình 3 bước cho cả 3 mode: (1) Nạp inputs đầy đủ; (2) Chờ `EvaluationCompleted`; (3) Nếu có bất kỳ node nào bị lỗi hoặc evaluation fail, BẮT BUỘC ném ngoại lệ để rollback transaction.

3. **Từ Observation 3 suy ra lỗi R2-06**:
   - Thoát `DispatcherFrame` chỉ là giải phóng việc chờ trên UI thread, không tác động đến `DynamoScheduler` đang chạy ngầm trong tiến trình.
   - Việc reset `_busy = 0` ngay sau đó cho phép request MCP tiếp theo được tiếp nhận và thực thi đồng thời với tác vụ cũ chưa dứt của Dynamo, gây xung đột reentrancy và vi phạm tính cô lập luồng của Revit API.
   - Kết luận: Coordinator phải duy trì `_busy = 1` và chờ `DynamoScheduler` đạt trạng thái `HasTaskInProgress == false` và `HasPendingTasks == false` trước khi nhả thread ownership.

---

## 3. Caveats (Các Điểm Lưu Ý & Giới Hạn)

1. **Môi trường Read-Only Explorer**:
   - Không thực hiện sửa đổi mã nguồn hoặc chạy build/test commands theo đúng System Instructions. Toàn bộ phân tích dựa trên kiểm tra mã nguồn, đối soát cấu trúc đối tượng ECMA-335 trong `DynamoCore.dll` và kiểm tra logic hiện hữu.
2. **Dynamo Cancel Command Semantics**:
   - Dynamo không cung cấp API `Abort()` bắt buộc ngay lập tức cho scheduler tasks đang chạy dở giữa chừng (vì lý do an toàn bộ nhớ C++ LibG geometry). Do đó, cơ chế xử lý khi cancel/timeout là: (a) Yêu cầu dọn dẹp hàng đợi (`CompactTaskQueue` / `ForceRunCancelCommand`); (b) Duy trì `_busy = 1` và chờ scheduler hoàn tất dứt điểm tác vụ hiện tại trong một khoảng thời gian chờ an toàn (grace period) trước khi cho phép request mới chạy.

---

## 4. Conclusion (Kết Luận)

Cả 3 vấn đề R2-04, R2-05, R2-06 đều đã được xác định nguyên nhân gốc rễ rõ ràng, kèm vị trí dòng mã chính xác và giải pháp thiết kế khả thi 100%:
1. **R2-04**: Loại bỏ việc kiểm tra token muộn sau boundary tại `DynamoBridgeCoordinator.cs:108`. Transaction boundary là chân lý duy nhất. Chỉ báo không mutation khi work item bị cancel từ hàng đợi.
2. **R2-05**: Ràng buộc failure policy chung: Ném ngoại lệ khi có node lỗi hoặc evaluation fail để rollback Revit transaction group; truyền đầy đủ `inputs` trong headless mode và kiểm tra chặt chẽ `ws != null`.
3. **R2-06**: Tương quan `EvaluationStarted`, bổ sung bước drain scheduler khi timeout/cancel và giữ `_busy = 1` tại coordinator cho tới khi `DynamoScheduler.HasTaskInProgress == false` và `HasPendingTasks == false`.

---

## 5. Verification Method (Phương Pháp Xác Minh Độc Lập)

Sau khi Implementer hoàn tất sửa mã nguồn, người kiểm thử có thể xác minh độc lập qua các phương pháp sau:

1. **Kiểm tra biên dịch & Test Suites**:
   ```bash
   dotnet test HPDynamo.McpBridge.Tests
   dotnet test HPDynamo.Mcp.Server.Tests
   dotnet test HPDynamo.E2E.Tests
   ```
2. **Xác minh R2-04 (Late Cancel Test)**:
   - Chạy test case mô phỏng một transaction commit thành công và cancel token kích hoạt ngay sau khi action trả về.
   - Kiểm tra `Assert.False(result.RolledBack)` và `Assert.False(result.IsError)`.
3. **Xác minh R2-05 (Evaluation Sync & Error Rollback Test)**:
   - Chạy `run_dyn_file` và `batch_edit_graph` với đồ thị chứa 1 node chia cho 0 hoặc lỗi cú pháp.
   - Kiểm tra kết quả trả về `result.IsError == true`, `result.RolledBack == true`, và không có đối tượng rác nào được commit vào Revit DB.
   - Chạy `run_dyn_file` chế độ headless với tham số `inputs`: Xác minh các giá trị override được áp dụng chính xác vào đồ thị.
4. **Xác minh R2-06 (Scheduler Drain & Busy Hold Test)**:
   - Mô phỏng một graph chạy lâu bị timeout: Xác minh `IsBusy` vẫn giữ giá trị `true` cho tới khi scheduler task hoàn thành hẳn, và request gửi xen lấn bị từ chối với thông báo "Another Dynamo script or graph is still running".
