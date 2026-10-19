# Handoff Report: Khảo Sát & Giải Pháp Kỹ Thuật (R2-07, R2-09, R2-10)

**From**: explorer_a_3  
**To**: orchestrator_8 (f5a54e1e-e38f-430b-b7e8-2a44388a5da1)  
**Date**: 2026-10-09T14:45:00Z  
**Type**: Hard Handoff (Investigation Complete)  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_a_3\`  

---

## 1. Observation (Quan Sát Trực Tiếp)

1. **R2-07 — Substring matching `TransactionManager` & các từ khóa**:
   - `HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoExecutionEngine.cs`, lines 141–155:
     ```csharp
     if (code.Contains("new Transaction(", StringComparison.Ordinal) ||
         code.Contains("new Autodesk.Revit.DB.Transaction(", StringComparison.Ordinal))
     {
         return false;
     }

     if (code.Contains("Revit.Elements.", StringComparison.Ordinal) ||
         code.Contains("TransactionManager", StringComparison.Ordinal) ||
         code.Contains("Dynamo.Applications", StringComparison.Ordinal) ||
         code.Contains("activeCanvasWs.Run(", StringComparison.Ordinal) ||
         code.Contains("ws.Run(", StringComparison.Ordinal) ||
         code.Contains("homeWs.Run(", StringComparison.Ordinal))
     {
         return false;
     }
     ```
   - `HPDynamo/HPDynamo.McpBridge.Tests/Coordinator/Milestone1ChallengerTests.cs`, lines 35–41:
     Test suite đang hợp thức hóa việc comment chứa từ khóa làm tắt auto-tx:
     ```csharp
     [InlineData("// TransactionManager.Instance\ndoc.Create.NewFamilyInstance(xyz);", "auto", false)]
     [InlineData("// new Transaction(doc);\r\ndoc.Create.NewFamilyInstance(xyz);", "auto", false)]
     ```
     Điều này vi phạm trực tiếp Acceptance Criteria của R2-07 ("Đoạn mã C# chứa chuỗi literal hoặc comment 'TransactionManager' vẫn được mở transaction auto bình thường").

2. **R2-09 — Correlation và Cancellation hiện tại**:
   - `McpShared/HPRebar.McpBridge.Core/Pipe/ICorrelatedCancelExecutor.cs`, lines 18–31:
     Chỉ nhận `long requestId` và `Cancel(long? targetRequestId)` mà không có `sessionId`.
   - `HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoBridgeCoordinator.cs`, lines 158–174:
     ```csharp
     public CancelResult Cancel(long? targetRequestId)
     {
         lock (_cancelLock)
         {
             var cancel = _currentCancel;
             if (cancel == null) return new CancelResult(false, false);

             if (targetRequestId.HasValue && targetRequestId.Value != 0 && targetRequestId.Value != _activeRequestId)
             {
                 Log.Information("Ignored stale cancel request for ID {TargetId} (active request is {ActiveId})",
                     targetRequestId.Value, _activeRequestId);
                 return new CancelResult(false, Volatile.Read(ref _busy) != 0);
             }

             try { cancel.Cancel(); return new CancelResult(true, true); }
             catch (ObjectDisposedException) { return new CancelResult(false, false); }
         }
     }
     ```
     Không có cờ re-entrancy `_isCancelling`; việc gọi `cancel.Cancel()` trong lock có thể kích hoạt các callback đăng ký trên CTS và gây ra đệ quy nếu callback tiếp tục gọi `Cancel()`. Thiếu cơ chế phân tách giữa các session kết nối lại.

3. **R2-10 — Trích xuất payload & thiếu `await` trong tests**:
   - `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Data/get_node_output_values/code.cs`, lines 35–38 và 61–65:
     ```csharp
     if (count++ >= MaxBreadth || portItemCount++ >= MaxTotalItemsPerPort)
     {
         dictResult["_truncated"] = "[Truncated breadth limit]";
         break;
     }
     ```
     Trực tiếp chèn key `"_truncated"` vào `dictResult`, có nguy cơ ghi đè khóa thực của người dùng và làm sai lệch schema.
     Không có giới hạn độ dài ký tự cho string (ví dụ string 10MB) và không có kiểm soát UTF-8 byte budget tích lũy.
   - `HPDynamo/HPDynamo.McpBridge.Tests/Coordinator/Milestone2Tests.cs`, lines 103 và 128–129:
     ```csharp
     public void Coordinator_Dispose_PendingQueueClearedAndCancelled()
     {
         ...
         var ex1 = Assert.ThrowsAsync<OperationCanceledException>(() => req1.Completion.Task);
         var ex2 = Assert.ThrowsAsync<OperationCanceledException>(() => req2.Completion.Task);
         Assert.NotNull(ex1);
         Assert.NotNull(ex2);
     }
     ```
     Phương thức là `public void`, gọi `Assert.ThrowsAsync` nhưng thiếu từ khóa `await`. Cả repo chỉ có duy nhất 2 lời gọi này bị lỗi thiếu `await`.

---

## 2. Logic Chain (Chuỗi Lập Luận Từ Quan Sát Đến Giải Pháp)

1. **Từ Quan sát 1 (R2-07)**:
   - Vì `code.Contains(...)` kiểm tra chuỗi thô $\rightarrow$ Bất kỳ comment nào như `// Note: don't use TransactionManager` hoặc string literal như `var msg = "TransactionManager";` đều khiến `ShouldStartAutoTransaction` trả về `false`.
   - Khi `ShouldStartAutoTransaction == false`, `RevitTransactionScope` không bọc transaction quanh script $\rightarrow$ Lệnh sửa DB Revit (`doc.Create...`, `doc.Delete...`) ném ngoại lệ runtime.
   - $\rightarrow$ **Giải pháp**: Xây dựng `DynamoTransactionDetector` dùng Roslyn `CSharpSyntaxTree` và `CSharpSyntaxWalker`. Syntax tree tự động tách biệt `SyntaxTrivia` (comments) và `StringLiteralExpression` (chuỗi văn bản) ra khỏi `MemberAccessExpressionSyntax` / `ObjectCreationExpressionSyntax`. Nhờ đó, loại bỏ hoàn toàn false positives.

2. **Từ Quan sát 2 (R2-09)**:
   - Khi client Named Pipe ngắt kết nối và kết nối lại, một session mới bắt đầu. Số đếm request ID của client mới thường reset về 1, 2, ...
   - Nếu bridge chỉ so sánh `targetRequestId == _activeRequestId` mà không có `sessionId` $\rightarrow$ Lệnh cancel từ session mới có thể hủy nhầm một request trùng ID của session cũ đang chạy dở trên Revit thread (hoặc ngược lại).
   - Khi `cancel.Cancel()` được gọi đồng bộ trong lock, nếu callback đăng ký kích hoạt tiếp một cancel method khác $\rightarrow$ Gây ra cancel-of-cancel đệ quy hoặc re-entrancy lock contention.
   - $\rightarrow$ **Giải pháp**:
     - Cấp phát `SessionId` cho mỗi kết nối `NdjsonPipeWriter` trong `PipeListener`.
     - Bổ sung `SessionId` (default `= null`) vào `ExecuteRequest` và `CancelParams` (100% tương thích ngược).
     - Định danh request tại `DynamoBridgeCoordinator` theo cặp `(SessionId, RequestId)`.
     - Thêm cờ `_isCancelling` để loại bỏ hoàn toàn re-entrancy và từ chối các cancel requests nhắm vào chính control/cancel requests.

3. **Từ Quan sát 3 (R2-10)**:
   - Việc ghi `dictResult["_truncated"] = "[Truncated breadth limit]"` làm biến đổi dữ liệu của dictionary người dùng $\rightarrow$ Nếu người dùng có dữ liệu với key `_truncated`, dữ liệu bị mất; nếu client mong đợi schema cố định, việc xuất hiện key lạ sẽ làm hỏng parser.
   - Thiếu giới hạn string length và UTF-8 byte budget $\rightarrow$ Nguy cơ làm tràn bộ nhớ pipe khi node trả về chuỗi siêu lớn.
   - $\rightarrow$ **Giải pháp**: Tách cờ `truncated` và `truncationReason` ra metadata cấp port (`portOutputDescriptions`), bổ sung `MaxStringLength = 2000` và `MaxPayloadBytesPerPort = 64 * 1024`.
   - `Assert.ThrowsAsync` trả về `Task<T>`. Nếu không có `await`, biến `ex1` và `ex2` chỉ là `Task` chứ không phải Exception, và câu lệnh `Assert.NotNull(ex1)` chỉ kiểm tra đối tượng Task khác null mà không thực sự xác thực xem Task có ném `OperationCanceledException` hay không.
   - $\rightarrow$ **Giải pháp**: Đổi thành `public async Task Coordinator_Dispose_...` và thêm `await` trước `Assert.ThrowsAsync`.

---

## 3. Caveats (Khu Vực Chưa Khảo Sát & Giả Định)

- **Giả định 1**: Bộ nhớ Named Pipe BufferSize mặc định là 65,536 bytes (64 KB). Giới hạn `MaxPayloadBytesPerPort` đặt ở mức 64 KB hoàn toàn phù hợp với kiến trúc pipe của repo.
- **Giả định 2**: `Microsoft.CodeAnalysis.CSharp.Scripting` đã sẵn sàng trong `HPRebar.McpBridge.Core` nên việc phân tích AST trong `HPDynamo.McpBridge` không đòi hỏi thêm bất kỳ package NuGet mới nào.
- **Khu vực Đợt B và Đợt C**: Các vấn đề biên dịch hạt giống SQLite quarantined (Đợt B) và đo đạc benchmark harness (Đợt C) thuộc phạm vi của các worker/explorer khác, không thuộc scope khảo sát của báo cáo này.
- Không có caveats khác.

---

## 4. Conclusion (Kết Luận Bàn Giao)

1. Đã xác định chính xác nguyên nhân gốc rễ và vị trí mã nguồn cho cả 3 hạng mục R2-07, R2-09, R2-10.
2. Đã thiết kế chi tiết kiến trúc giải pháp tối ưu cho từng hạng mục:
   - **R2-07**: `DynamoTransactionDetector` dùng Roslyn AST syntax walker, loại bỏ 100% substring matches, cập nhật test suite `Milestone1ChallengerTests.cs`.
   - **R2-09**: Định danh `(SessionId, RequestId)` xuyên suốt `PipeListener` $\rightarrow$ `RequestDispatcher` $\rightarrow$ `DynamoBridgeCoordinator`, thêm re-entrancy guard `_isCancelling` chống cancel đệ quy.
   - **R2-10**: Bổ sung `MaxStringLength` và UTF-8 byte budget trong `get_node_output_values/code.cs`, đưa metadata `truncated` ra ngoài cấp port, sửa 2 lời gọi `Assert.ThrowsAsync` thiếu `await` trong `Milestone2Tests.cs`.
3. Tài liệu chi tiết đã được lưu tại `analysis.md` trong thư mục làm việc của agent.

---

## 5. Verification Method (Phương Pháp Xác Minh Độc Lập)

Sau khi implementer thực hiện code changes, có thể xác minh độc lập bằng các bước sau:

1. **Xác minh R2-07**:
   - Mở `HPDynamo/HPDynamo.McpBridge.Tests/Coordinator/Milestone1ChallengerTests.cs`.
   - Chạy test kiểm tra: Các đoạn mã có comment `// TransactionManager.Instance` hoặc chuỗi `"TransactionManager"` khi gọi `ShouldStartAutoTransaction(code, "auto")` phải trả về `true`.
   - Lệnh kiểm tra:
     ```bash
     dotnet test HPDynamo/HPDynamo.McpBridge.Tests/HPDynamo.McpBridge.Tests.csproj --filter "FullyQualifiedName~Milestone1ChallengerTests"
     ```

2. **Xác minh R2-09**:
   - Kiểm tra mã nguồn `ICorrelatedCancelExecutor.cs`, `CancelParams.cs`, `ExecuteRequest.cs`, `DynamoBridgeCoordinator.cs`.
   - Chạy các test case liên quan:
     ```bash
     dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj --filter "FullyQualifiedName~CorrelatedCancellation"
     ```
   - Thêm test case mô phỏng 2 session với cùng request ID 1: Cancel từ session 2 không được hủy request của session 1.

3. **Xác minh R2-10**:
   - Kiểm tra `Milestone2Tests.cs`: dòng 103 là `public async Task`, dòng 128-129 có `await Assert.ThrowsAsync`.
   - Chạy test:
     ```bash
     dotnet test HPDynamo/HPDynamo.McpBridge.Tests/HPDynamo.McpBridge.Tests.csproj --filter "FullyQualifiedName~Milestone2Tests"
     ```
   - Kiểm tra `get_node_output_values/code.cs`: Dictionary trả về không còn chứa key giả `"_truncated"`; port metadata có trường `truncated: true/false`.
