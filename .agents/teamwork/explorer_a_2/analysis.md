# Báo Cáo Khảo Sát Kỹ Thuật & Đề Xuất Thiết Kế Chi Tiết (R2-04, R2-05, R2-06)

**Agent**: explorer_a_2  
**Dự án**: HPDynamo MCP Vòng 2  
**Mục tiêu**: Khảo sát hiện trạng kỹ thuật và đề xuất giải pháp thiết kế chi tiết (fix strategy) cho 3 hạng mục R2-04, R2-05, R2-06.

---

## 1. R2-04: Bảo Toàn Kết Quả Transaction Thật Sự Tại Coordinator

### 1.1. Hiện Trạng Kỹ Thuật & Vị Trí Mã Nguồn
- **Tập tin**: `HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoBridgeCoordinator.cs`
- **Vị trí cụ thể**: Dòng 107–125
```csharp
107: var result = await DispatchExecutionAsync(request, progress, cancel).ConfigureAwait(false);
108: if (cancel.IsCancellationRequested) throw new OperationCanceledException(cancel.Token);
109: return Finish(request, result, sw);
...
120: catch (OperationCanceledException)
121: {
122:     var res = ExecuteResult.Failure("Execution was cancelled.");
123:     res.RolledBack = true;
124:     return Finish(request, res, sw);
125: }
```
- **Tập tin kiểm thử**: `HPDynamo/HPDynamo.McpBridge.Tests/Coordinator/Milestone3CorrelatedCancelStressTests.cs` (dòng 32–56) và `Milestone3CorrelatedCancelConcurrencyTests.cs` (dòng 106–107).

### 1.2. Phân Tích Nguyên Nhân Gốc Rễ (Root Cause)
1. **Ghi đè sai sự thật khi cancel đến muộn**:
   - `DispatchExecutionAsync` ủy thác thực thi cho `RevitTransactionScope.Execute` trên Revit UI thread.
   - Tại `RevitTransactionScope.cs` (dòng 145, 254), khi script/action hoàn thành mà không có ngoại lệ và `!request.DryRun`, Revit `TransactionGroup` và auto transaction **đã commit thành công vào Revit DB** (`AssimilateGroup` và `CommitAutoTransaction`). Kết quả `ExecuteResult` được tạo với `RolledBack = false`, `IsError = false`, và `Changed` ghi nhận số lượng elements thực tế bị sửa đổi.
   - Khi `DispatchExecutionAsync` trả về `result`, coordinator thực hiện kiểm tra muộn: `if (cancel.IsCancellationRequested) throw new OperationCanceledException(cancel.Token);` (dòng 108).
   - Nếu client phát lệnh hủy (`Cancel()`) hoặc token timeout kích hoạt ngay tại thời điểm continuation (hoặc sau khi transaction đã commit), dòng 108 ném `OperationCanceledException`.
   - Khối `catch (OperationCanceledException)` (dòng 120–125) **vứt bỏ hoàn toàn `result` đã commit**, tạo mới một `ExecuteResult.Failure("Execution was cancelled.")` và gán cứng `res.RolledBack = true`.
2. **Hậu quả nghiêm trọng**:
   - Khách hàng / AI agent nhận được phản hồi: `IsError: true`, `Message: "Execution was cancelled."`, `RolledBack: true`.
   - AI agent tin rằng mô hình Revit chưa bị thay đổi hoặc đã được rollback hoàn toàn, trong khi thực tế **các đối tượng Revit đã được tạo/sửa/xóa và commit vĩnh viễn vào DB**. Điều này dẫn đến sai lệch mô hình, tạo trùng lặp khi chạy lại, và mất dấu vết transaction.
   - Trường hợp rollback thất bại bên trong Revit (`SafeRollback` gặp lỗi), kết quả cũng bị dòng 123 ghi đè thành rollback thành công (`RolledBack = true`).
3. **Trường hợp work item bị hủy khi còn trong hàng đợi (Queued)**:
   - Trong `RunOnRevitThreadAsync`, nếu work item bị cancel trước khi Revit UI thread lấy ra chạy (`WorkItemState.Queued`), `TryCancelIfQueued` đánh dấu canceled và `RunOnRevitThreadAsync` ném `TaskCanceledException`.
   - Lúc này, transaction chưa hề được mở trên Revit DB và không có mutation nào diễn ra. Việc báo `RolledBack = true` là không chính xác về mặt ngữ nghĩa (thực tế là No-Op / Unstarted).

### 1.3. Đề Xuất Giải Pháp Thiết Kế Chi Tiết (Fix Strategy)
1. **Xóa bỏ kiểm tra late-cancel sau boundary**:
   - Xóa bỏ dòng 108: `if (cancel.IsCancellationRequested) throw new OperationCanceledException(cancel.Token);`.
   - Một khi `DispatchExecutionAsync` đã trả về `ExecuteResult` hợp lệ từ `RevitTransactionScope`, đó là **nguồn sự thật duy nhất (Single Source of Truth)** về trạng thái giao dịch DB.
2. **Bảo toàn trạng thái transaction thực tế**:
   - Nếu `result.RolledBack == false` và `!result.IsError`: Transaction đã commit thành công. Nếu cancel token đã kích hoạt muộn, có thể gắn thêm cảnh báo vào `result.Logs` hoặc `result.Warnings` (`"Cancellation arrived after transaction committed; changes were persisted."`), nhưng **tuyệt đối không đặt `RolledBack = true`** và **không vứt bỏ `result.Changed`**.
   - Nếu `result.RolledBack == true`: Do `RevitTransactionScope` đã rollback thật sự (do dryRun, lỗi, hoặc cancel diễn ra trong lúc action đang chạy). Trả về `result` nguyên vẹn.
3. **Xử lý `OperationCanceledException` khi work item chưa bắt đầu**:
   - Trong khối `catch (OperationCanceledException)` tại `DynamoBridgeCoordinator.ExecuteAsync`:
     - Phân định trạng thái work item: Nếu work item bị cancel từ hàng đợi (`WorkItemState.Queued`), không có mutation nào diễn ra:
       ```csharp
       var res = ExecuteResult.Failure("Execution was cancelled before starting. No changes were made.", rolledBack: false);
       ```
     - Nếu ngoại lệ ném ra từ execution mà không có `result`: kiểm tra log/tracker, báo lỗi trung thực không khẳng định đã rollback nếu chưa xác nhận từ `RevitTransactionScope`.
4. **Cập nhật Regression Tests**:
   - Sửa các assertions trong `Milestone3CorrelatedCancelStressTests.cs` và `Milestone3CorrelatedCancelConcurrencyTests.cs`:
     - Test kiểm tra late cancel sau commit: Assert `result.RolledBack == false`.
     - Test kiểm tra cancel khi đang queued: Assert `result.RolledBack == false` (hoặc phân định rõ không mutation).

---

## 2. R2-05: Thống Nhất Evaluation Sync & Xử Lý Lỗi Xuyên Suốt Mọi Mode

### 2.1. Hiện Trạng Kỹ Thuật & Vị Trí Mã Nguồn
Hiện có 3 chế độ thực thi đồ thị Dynamo, nhưng cơ chế đồng bộ và xử lý lỗi đang bị phân mảnh và bất nhất:
1. **Batch Mode (`batch_edit_graph`)**:
   - `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/batch_edit_graph/code.cs` (dòng 262–265):
     ```csharp
     if (runEvaluation && homeWs != null && !dryRun)
     {
         homeWs.Run();
     }
     ```
     Gọi `homeWs.Run()` bất đồng bộ và không chờ `EvaluationCompleted`. Chạy thẳng tới kết quả `success = true` (dòng 285).
2. **Headless Graph Runner (`HeadlessGraphRunner`)**:
   - `HPDynamo/HPDynamo.McpBridge/Execution/HeadlessGraphRunner.cs`:
     - `ExecuteDesignScript` (dòng 108–118): Gọi `ws.Run()` trực tiếp, đọc outputs ngay và xóa ephemeral `CodeBlock` lập tức mà không chờ evaluation hay kiểm tra `cbn.State == ElementState.Error`. Luôn trả về `Success = true`.
     - `RunDynFile` (dòng 50–68): Gọi `app.ExecuteDynamoCommand(journalData, uiapp)`. Nếu `ws == null` (do tải file lỗi), `errorCount = 0` và trả về `Status = "Succeeded"`, không ném ngoại lệ nên `RevitTransactionScope` vẫn commit.
3. **Seed `run_dyn_file`**:
   - `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/run_dyn_file/code.cs` (dòng 91–108):
     - Nhánh Headless: Hoàn toàn **bỏ qua tham số `inputs`** (không đưa vào `journalDict`), không chờ evaluation, không trích xuất outputs, và nếu `revitDynamo` hoặc `headlessWorkspace` là `null` thì tính `errorNodesCount = 0` dẫn đến `success = true`.
     - Nhánh Canvas (dòng 114): Khi có node bị lỗi (`errorNodesCount > 0`), seed chỉ trả về `{ success = false }` trong payload JSON mà **không ném ngoại lệ**. Do đó, `RevitTransactionScope` coi như hàm chạy thành công và **commit toàn bộ thay đổi lỗi dở dang vào Revit DB**.

### 2.2. Phân Tích Nguyên Nhân Gốc Rễ (Root Cause)
- Thiếu một chính sách thất bại chung (Unified Failure Policy) ràng buộc với Transaction Boundary.
- `RevitTransactionScope` chỉ rollback khi `request.DryRun == true` hoặc action ném ngoại lệ. Khi một seed phát hiện node bị lỗi (`ElementState.Error`) nhưng chỉ gói nó thành thuộc tính `success = false` trong anonymous object trả về, `RevitTransactionScope` không thể biết rằng đồ thị đã thất bại, dẫn đến việc commit transaction.
- Headless runner thiếu bước lắng nghe `EvaluationCompleted` trước khi đọc kết quả hoặc xóa node.
- Truyền dữ liệu tham số đầu vào (`inputs`) không đồng nhất: Canvas dùng `ApplyOverrides` duyệt nodes trực tiếp, còn Headless seed `run_dyn_file` không serialize `inputs` vào journal data `dynModelNodesInfo`.

### 2.3. Đề Xuất Giải Pháp Thiết Kế Chi Tiết (Fix Strategy)
1. **Thống nhất Failure Policy với Revit Transaction Boundary**:
   - Nguyên tắc: **Bất kỳ mode nào (Canvas, Headless, Batch), nếu evaluation thất bại, timeout, bị cancel, hoặc có ít nhất 1 node ở trạng thái `ElementState.Error`, runner BẮT BUỘC phải ném ngoại lệ** (`InvalidOperationException` hoặc `OperationCanceledException`).
   - Điều này đảm bảo `RevitTransactionScope` lập tức kích hoạt `SafeRollback()`, không để lại dữ liệu rác/lỗi trong mô hình Revit.
2. **Chuẩn hóa `batch_edit_graph`**:
   - Khi `runEvaluation == true` và `!dryRun`:
     ```csharp
     bool evalOk = RunAndWait(() => homeWs.Run());
     if (!evalOk)
     {
         PerformRollback();
         throw new InvalidOperationException("Evaluation failed or timed out after batch edit.");
     }
     int errorCount = homeWs.Nodes.Count(n => n.State == ElementState.Error);
     if (errorCount > 0)
     {
         PerformRollback();
         throw new InvalidOperationException($"Batch edit evaluation resulted in {errorCount} node(s) in error state.");
     }
     ```
3. **Chuẩn hóa `HeadlessGraphRunner`**:
   - Trong `ExecuteDesignScript`:
     - Sử dụng `DynamoEvaluationSync.RunAndWait` trên `model`.
     - Sau khi evaluation hoàn tất, kiểm tra `cbn.State == ElementState.Error`. Nếu lỗi, ném `InvalidOperationException`.
   - Trong `RunDynFile`:
     - Kiểm tra nghiêm ngặt `ws != null`. Nếu null, ném `InvalidOperationException($"Failed to load workspace from '{dynPath}'.")`.
     - Chờ evaluation hoàn tất và kiểm tra `errorCount > 0` thì ném `InvalidOperationException`.
4. **Chuẩn hóa Seed `run_dyn_file/code.cs`**:
   - Nhánh Headless:
     - Serialize `inputOverrides` vào `journalDict["dynModelNodesInfo"]` theo chuẩn JSON array của Dynamo journal format: `[{"Name":"...","Value":"..."}]`.
     - Sau khi chạy, kiểm tra `headlessWorkspace != null`; nếu null, ném `InvalidOperationException`.
     - Nếu `errorNodesCount > 0`, ném `InvalidOperationException($"Graph evaluation completed with {errorNodesCount} node(s) in error state.")` để `RevitTransactionScope` rollback.

---

## 3. R2-06: Đảm Bảo Dynamo Scheduler Đã Dừng Hoàn Toàn Khi Timeout/Cancel

### 3.1. Hiện Trạng Kỹ Thuật & Vị Trí Mã Nguồn
- **Tập tin**: `HPDynamo/HPDynamo.McpBridge/Execution/DynamoEvaluationSync.cs` (dòng 26–58)
```csharp
26: var frame = new DispatcherFrame();
27: ...
38: using var registration = linkedCts.Token.Register(() => frame.Continue = false);
39: try
40: {
41:     runAction();
42:     if (!completed && !linkedCts.Token.IsCancellationRequested)
43:     {
44:         Dispatcher.PushFrame(frame);
45:     }
46:     return CheckCompletion(...);
47: }
...
57: model.EvaluationCompleted -= OnEvaluationCompleted;
```
- **Tập tin**: `HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoBridgeCoordinator.cs` (dòng 94, 133):
```csharp
94: if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0) return Failure(...);
...
133: Volatile.Write(ref _busy, 0);
```

### 3.2. Phân Tích Nguyên Nhân Gốc Rễ (Root Cause)
1. **Thoát `DispatcherFrame` không đồng nghĩa với Dynamo Scheduler đã dừng**:
   - Khi cancel hoặc timeout kích hoạt, callback dòng 38 gán `frame.Continue = false`.
   - Lệnh `Dispatcher.PushFrame(frame)` kết thúc trên Revit UI thread, `RunAndWait` trả về `false`, và khối `finally` hủy đăng ký `model.EvaluationCompleted`.
   - Tuy nhiên, trong Dynamo Core (`DynamoCore.dll`), việc tính toán đồ thị được quản lý bởi `DynamoScheduler` (`Dynamo.Scheduler.DynamoScheduler`) thông qua các tác vụ nền như `UpdateGraphAsyncTask`.
   - **Không có bất kỳ lệnh nào được gửi tới Dynamo để yêu cầu hủy bỏ hoặc chờ scheduler dừng.**
2. **Giải phóng thread ownership (`_busy = 0`) quá sớm**:
   - Sau khi `RunAndWait` trả về `false`, ngoại lệ được ném ra và `RevitTransactionScope` rollback transaction.
   - Ngay lập tức, `DynamoBridgeCoordinator.ExecuteAsync` đi vào khối `finally` và thực hiện `Volatile.Write(ref _busy, 0)`.
   - Cờ `_busy` trở về 0 báo hiệu coordinator đã sẵn sàng nhận request mới.
   - Trong khi đó, **Dynamo Scheduler vẫn đang âm thầm chạy tác vụ cũ trong tiến trình**.
   - Nếu một request MCP mới tới, nó sẽ chiếm `_busy = 1` và bắt đầu thao tác trên canvas/Revit DB. Tác vụ cũ và tác vụ mới chạy xen lấn (race condition), gây xung đột luồng Revit API, làm hỏng AST của ProtoCore, hoặc làm sập tiến trình Revit.
3. **`runAction()` gọi khi token đã cancel**:
   - Dòng 41 gọi `runAction()` mà không kiểm tra xem `linkedCts.Token.IsCancellationRequested` đã bật hay chưa (ví dụ timeout = 0 hoặc token đã bị cancel từ trước).
4. **Hiện tượng nhận nhầm `EvaluationCompleted` cũ trong Automatic Mode**:
   - Trong chế độ chạy tự động (Automatic Run), một evaluation có thể đang diễn ra từ trước khi request MCP tới. Việc đăng ký trực tiếp `model.EvaluationCompleted` có thể bắt nhầm sự kiện hoàn thành của tác vụ cũ đó.

### 3.3. Bằng Chứng Kiến Trúc Từ DynamoCore API
Khảo sát trực tiếp siêu dữ liệu và cấu trúc nhị phân của `DynamoCore.dll` (Dynamo 3.7 / Revit 2026) cho thấy:
- `DynamoModel.Scheduler` thuộc kiểu `Dynamo.Scheduler.DynamoScheduler`.
- `DynamoScheduler` cung cấp các thuộc tính và phương thức quản lý hàng đợi:
  - `public bool HasTaskInProgress { get; }` (RVA 0x9784): Cho biết đang có tác vụ đang chạy.
  - `public bool HasPendingTasks { get; }` (RVA 0x980C): Cho biết còn tác vụ đang chờ trong hàng đợi.
  - `public IEnumerable<AsyncTask> Tasks { get; }` (RVA 0x973C).
  - `public void CompactTaskQueue()` (RVA 0x9888): Dọn dẹp các task lỗi thời trong queue.
- `DynamoModel` có các sự kiện:
  - `EvaluationStarted` (RVA 0x285C0)
  - `EvaluationCompleted` (RVA 0x28680)

### 3.4. Đề Xuất Giải Pháp Thiết Kế Chi Tiết (Fix Strategy)
1. **Kiểm tra token trước khi `runAction()`**:
   - Trong `DynamoEvaluationSync.RunAndWait`:
     ```csharp
     if (linkedCts.Token.IsCancellationRequested)
     {
         log?.Invoke("Dynamo evaluation cancelled prior to execution.");
         return false;
     }
     ```
2. **Khắc phục nhận nhầm sự kiện bằng Correlation (EvaluationStarted)**:
   - Đặt cờ `evaluationStarted = false`.
   - Đăng ký `model.EvaluationStarted += (_, _) => evaluationStarted = true;`.
   - Chỉ xử lý `OnEvaluationCompleted` khi `evaluationStarted == true`.
3. **Cơ chế Drain / Await Dynamo Scheduler khi Timeout/Cancel**:
   - Khi `linkedCts.Token.IsCancellationRequested` kích hoạt:
     - Không thoát đột ngột. Gọi cơ chế yêu cầu dừng/dọn dẹp: nếu model có `Scheduler`, gọi kiểm tra trạng thái.
     - Thực hiện vòng lặp drain có giới hạn thời gian (Drain Grace Period, ví dụ tối đa 2000ms):
       ```csharp
       private static void DrainScheduler(DynamoModel model, TimeSpan drainBudget)
       {
           var scheduler = model.Scheduler;
           if (scheduler == null) return;
           var sw = Stopwatch.StartNew();
           while (sw.Elapsed < drainBudget && (scheduler.HasTaskInProgress || scheduler.HasPendingTasks))
           {
               // Bơm dispatcher frame ngắn để xử lý các callback dọn dẹp còn sót
               DoEvents(TimeSpan.FromMilliseconds(50));
           }
       }
       ```
4. **Giữ vững Thread Ownership (`_busy`) tại Coordinator cho tới khi Scheduler nhàn rỗi**:
   - Trong `DynamoBridgeCoordinator.ExecuteAsync`:
     Khối `finally` chỉ reset `_busy = 0` sau khi đảm bảo Dynamo Scheduler đã hoàn toàn nhàn rỗi:
     ```csharp
     finally
     {
         EnsureSchedulerIdle();
         Volatile.Write(ref _busy, 0);
         StateChanged?.Invoke();
     }
     ```
   - Hàm `EnsureSchedulerIdle()` kiểm tra cả `_liveViewModel?.Model?.Scheduler` lẫn headless `RevitDynamoModel?.Scheduler`. Nếu scheduler còn tác vụ đang chạy, giữ `_busy = 1` và chờ drain (với timeout trần an toàn) trước khi nhả thread.

---

## 4. Tóm Tắt Kế Hoạch Hiện Thực Hóa (Implementation Plan cho Implementer)

| Hạng mục | Tập tin tác động | Thao tác chính |
|---|---|---|
| **R2-04** | `HPDynamo.McpBridge/Coordinator/DynamoBridgeCoordinator.cs` | Xóa `if (cancel.IsCancellationRequested) throw ...` tại dòng 108. Giữ nguyên `ExecuteResult` từ `DispatchExecutionAsync`. Phân định cancellation khi queued vs running. |
| **R2-04** | `HPDynamo.McpBridge.Tests/Coordinator/Milestone3CorrelatedCancel*.cs` | Cập nhật assertions không assert sai `RolledBack = true` trên transaction đã commit. |
| **R2-05** | `batch_edit_graph/code.cs` | Bọc `homeWs.Run()` trong `RunAndWait`, kiểm tra `ElementState.Error`, ném exception để kích hoạt rollback transaction khi fail. |
| **R2-05** | `HeadlessGraphRunner.cs` | Chờ evaluation trong `ExecuteDesignScript`, ném exception khi node lỗi hoặc workspace null. |
| **R2-05** | `run_dyn_file/code.cs` | Bổ sung `inputs` vào journal data cho Headless mode, ném exception khi `errorNodesCount > 0` thay vì trả payload success=false. |
| **R2-06** | `DynamoEvaluationSync.cs` | Check token trước `runAction()`, tương quan `EvaluationStarted` tránh stale events, bổ sung `DrainScheduler` khi timeout/cancel. |
| **R2-06** | `DynamoBridgeCoordinator.cs` | Bổ sung `EnsureSchedulerIdle()` trước khi `Volatile.Write(ref _busy, 0)` trong `finally`. |
