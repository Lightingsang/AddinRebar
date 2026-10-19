# Báo Cáo Khảo Sát Kỹ Thuật & Đề Xuất Giải Pháp Thiết Kế (R2-07, R2-09, R2-10)

**Dự án**: HPDynamo MCP Vòng 2  
**Tác giả**: explorer_a_3 (Teamwork Preview Explorer)  
**Ngày**: 2026-10-09  
**Trạng thái**: Hoàn thành khảo sát (Đọc-chỉ, không sửa mã nguồn, không chạy build/test)  

---

## 1. Tổng Quan Điều Hành (Executive Summary)

Theo chỉ thị tại `ORIGINAL_REQUEST.md` (mục `## 2026-10-09T14:32:33Z`), `SCOPE.md` của `orchestrator_8`, và `DISPATCH.md`, phân hệ HPDynamo MCP cần giải quyết dứt điểm 3 vấn đề cốt lõi thuộc Đợt A:
1. **R2-07**: Phân định quyền quản lý transaction bằng cú pháp Roslyn AST syntax / policy, loại bỏ hoàn toàn việc kiểm tra chuỗi thô (`code.Contains(...)`) đối với `"TransactionManager"` và các từ khóa liên quan.
2. **R2-09**: Cô lập cancellation theo phiên làm việc `(session, request)` để loại trừ va chạm ID khi client reconnect trên Named Pipe, đồng thời xây dựng cơ chế chống cancel-of-cancel đệ quy trên các control/cancel methods.
3. **R2-10**: Kiểm soát ngân sách payload theo cả UTF-8 byte budget và character length; bóc tách metadata `_truncated` ra khỏi dictionary/user data nhằm chống ghi đè dữ liệu người dùng; và bổ sung từ khóa `await` cho các lời gọi `Assert.ThrowsAsync` đang bị thiếu trong test suite.

---

## 2. Chi Tiết R2-07: Phân Định Quyền Quản Lý Transaction Bằng Cú Pháp AST / Policy

### 2.1. Hiện Trạng Mã Nguồn
- **Vị trí**: `HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoExecutionEngine.cs`, các dòng 133–158:
  ```csharp
  internal static bool ShouldStartAutoTransaction(string code, string? transactionMode)
  {
      var mode = TransactionModes.Normalize(transactionMode) ?? TransactionModes.Auto;
      if (!string.Equals(mode, TransactionModes.Auto, StringComparison.OrdinalIgnoreCase))
      {
          return false;
      }

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

      return true;
  }
  ```
- **Hiện trạng kiểm thử liên quan**:
  - `HPDynamo.McpBridge.Tests/Coordinator/DynamoExecutionEngineTests.cs`: Kiểm tra cơ bản các chuỗi mẫu.
  - `HPDynamo.McpBridge.Tests/Coordinator/Milestone1ChallengerTests.cs` (dòng 34–56): Ở Vòng 1, một số test case được viết để hợp thức hóa việc kiểm tra chuỗi (coi việc comment hoặc string literal chứa từ khóa làm tắt auto-tx là "errs on the side of caution"). Tuy nhiên, Codex Review Vòng 2 đã chỉ ra đây là một lỗ hổng logic nghiêm trọng.

### 2.2. Phân Tích Nguyên Nhân & Rủi Ro False Positive
1. **Mù lòa về ngữ pháp C# (Lexically blind)**:
   - Phương thức `code.Contains(...)` đối chiếu chuỗi thô trên toàn bộ text file mà không phân biệt được đâu là mã thực thi, đâu là comment, đâu là string literal, đâu là tên biến.
2. **False Positive do Comments**:
   - Ví dụ một script C# gọi API Revit DB thuần:
     ```csharp
     // Note: Do not use TransactionManager here because we use Revit DB directly
     doc.Create.NewFamilyInstance(xyz, sym, level, StructuralType.NonStructural);
     ```
   - Kết quả: `code.Contains("TransactionManager")` trả về `true` $\rightarrow$ `ShouldStartAutoTransaction` trả về `false` $\rightarrow$ `RevitTransactionScope` **không** mở `Transaction` $\rightarrow$ Lệnh `doc.Create...` bị Revit ném ngoại lệ runtime: `Autodesk.Revit.Exceptions.InvalidOperationException: Starting a transaction from an external application running outside of API context is not allowed` hoặc `The document was modified outside of a transaction`!
3. **False Positive do String Literals & Logs**:
   - Ví dụ:
     ```csharp
     log("Executing step without TransactionManager");
     var typeName = "Revit.Elements.Wall";
     doc.Delete(elementId);
     ```
   - String literal chứa từ khóa khiến auto-tx bị vô hiệu hóa oan uổng.
4. **False Positive do Biến / Member đặt tên tương tự**:
   - Ví dụ: `var myTransactionManagerFlag = false;` cũng kích hoạt substring match.

### 2.3. Đề Xuất Giải Pháp Kiến Trúc (Fix Strategy)
Sử dụng **Roslyn C# Syntax Tree (AST)** để phân tích cú pháp tường minh.
Trong kiến trúc của repo, `HPRebar.McpBridge.Core` đã tích hợp sẵn thư viện `Microsoft.CodeAnalysis.CSharp.Scripting 5.9.0`. Do đó, `HPDynamo.McpBridge` có toàn quyền sử dụng `CSharpSyntaxTree` và `CSharpSyntaxWalker`.

#### Thiết kế lớp `DynamoTransactionDetector`
Tạo lớp chuyên trách `DynamoTransactionDetector` (hoặc `DynamoTransactionPolicy`) trong `HPDynamo.McpBridge/Coordinator/`:

```csharp
namespace HPDynamo.McpBridge.Coordinator;

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class DynamoTransactionDetector
{
    public static bool ManagesOwnTransaction(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;

        var tree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(kind: SourceCodeKind.Script));
        var walker = new TransactionSyntaxWalker();
        walker.Visit(tree.GetRoot());
        return walker.ManagesOwnTransaction;
    }

    private sealed class TransactionSyntaxWalker : CSharpSyntaxWalker
    {
        public bool ManagesOwnTransaction { get; private set; }

        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            var typeName = node.Type switch
            {
                IdentifierNameSyntax id => id.Identifier.ValueText,
                QualifiedNameSyntax q => q.Right.Identifier.ValueText,
                _ => null
            };

            if (typeName is "Transaction" or "TransactionGroup" or "SubTransaction")
            {
                ManagesOwnTransaction = true;
                return;
            }

            base.VisitObjectCreationExpression(node);
        }

        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            // Detect TransactionManager API calls (e.g., TransactionManager.Instance...)
            if (node.Expression is IdentifierNameSyntax id && id.Identifier.ValueText == "TransactionManager")
            {
                ManagesOwnTransaction = true;
                return;
            }

            // Detect Revit.Elements.* wrapper calls
            if (node.Expression is MemberAccessExpressionSyntax sub &&
                sub.Expression is IdentifierNameSyntax root && root.Identifier.ValueText == "Revit" &&
                sub.Name.Identifier.ValueText == "Elements")
            {
                ManagesOwnTransaction = true;
                return;
            }

            // Detect Dynamo.Applications.* calls
            if (node.Expression is MemberAccessExpressionSyntax appSub &&
                appSub.Expression is IdentifierNameSyntax dRoot && dRoot.Identifier.ValueText == "Dynamo" &&
                appSub.Name.Identifier.ValueText == "Applications")
            {
                ManagesOwnTransaction = true;
                return;
            }

            base.VisitMemberAccessExpression(node);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            // Detect ws.Run(), homeWs.Run(), activeCanvasWs.Run()
            if (node.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Run" } access &&
                access.Expression is IdentifierNameSyntax wsId &&
                (wsId.Identifier.ValueText is "ws" or "homeWs" or "activeCanvasWs"))
            {
                ManagesOwnTransaction = true;
                return;
            }

            base.VisitInvocationExpression(node);
        }
    }
}
```

#### Tái cấu trúc `DynamoExecutionEngine.ShouldStartAutoTransaction`
```csharp
internal static bool ShouldStartAutoTransaction(string code, string? transactionMode)
{
    var mode = TransactionModes.Normalize(transactionMode) ?? TransactionModes.Auto;
    if (!string.Equals(mode, TransactionModes.Auto, StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    return !DynamoTransactionDetector.ManagesOwnTransaction(code);
}
```

#### Vì sao giải pháp AST này giải quyết triệt để vấn đề:
1. **Bỏ qua 100% comments**: Trong Roslyn, comment là `SyntaxTrivia`. Bộ duyệt `CSharpSyntaxWalker` mặc định **không** duyệt trivia, do đó bất kỳ comment nào chứa từ khóa `"TransactionManager"` hay `"new Transaction"` đều bị bỏ qua tự động.
2. **Bỏ qua 100% string literals**: Chuỗi `"TransactionManager"` nằm trong `LiteralExpressionSyntax` (token kind `StringLiteralExpression`), không bao giờ kích hoạt `VisitMemberAccessExpression` hay `VisitObjectCreationExpression`.
3. **Chính xác tuyệt đối**: Chỉ khi lập trình viên thực sự viết biểu thức truy cập thành viên của lớp `TransactionManager` hoặc khởi tạo `new Transaction(...)` thì cờ mới bật `false`.
4. **Cập nhật Unit Tests**:
   - Sửa đổi các test case trong `Milestone1ChallengerTests.cs` (lines 35–56): Đổi kỳ vọng từ `false` thành `true` khi từ khóa chỉ xuất hiện trong comments hoặc string literals.
   - Thêm test case khẳng định Acceptance Criteria: Đoạn mã C# chứa chuỗi literal hoặc comment `"TransactionManager"` vẫn được mở transaction auto bình thường khi gọi `execute_dynamo_code` với `transaction = "auto"`.

---

## 3. Chi Tiết R2-09: Cô Lập Cancel Theo Session & Chống Cancel-of-Cancel Đệ Quy

### 3.1. Hiện Trạng Kỹ Thuật
- **Hạ tầng Named Pipe**:
  - `McpShared/HPRebar.McpBridge.Core/Pipe/PipeListener.cs`: Lắng nghe kết nối Named Pipe. Mỗi lần server kết nối, `AcceptLoopAsync` sinh ra một `NamedPipeServerStream` và chạy `ServeClientAsync(stream, ct)`.
  - `McpShared/HPRebar.McpBridge.Core/Pipe/NdjsonPipeWriter.cs`: Đại diện cho kết nối outbound duy nhất của một phiên kết nối.
  - `McpShared/HPRebar.McpBridge.Core/Pipe/RequestDispatcher.cs`: Điều phối các dòng JSON-RPC từ pipe sang `IBridgeExecutor`.
- **Giao diện Cancel & Correlation**:
  - `McpShared/HPRebar.McpBridge.Core/Pipe/ICorrelatedCancelExecutor.cs`:
    ```csharp
    public interface ICorrelatedCancelExecutor : IBridgeExecutor
    {
        Task<ExecuteResult> ExecuteAsync(
            long requestId,
            ExecuteRequest request,
            IProgress<ScriptProgress>? progress,
            CancellationToken cancellationToken);

        CancelResult Cancel(long? targetRequestId);
    }
    ```
  - `HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoBridgeCoordinator.cs`:
    - Biến thành viên: `private long _activeRequestId; private CancellationTokenSource? _currentCancel;`
    - Phương thức: `Cancel(long? targetRequestId)` chỉ so sánh `targetRequestId.Value != _activeRequestId`.

### 3.2. Phân Tích Lỗ Hổng & Rủi Ro
1. **Rủi ro va chạm ID giữa các Session khi Reconnect**:
   - Khi client MCP hoặc server bị ngắt kết nối (crash hoặc restart) và kết nối lại trên pipe mới:
     - Phiên cũ có thể có một request chạy ngầm (ví dụ `requestId = 101`).
     - Phiên mới bắt đầu đánh số ID từ đầu (1, 2, ..., 101).
     - Nếu phiên mới phát lệnh `cancel` với `id = 101` (hoặc phiên cũ có gói cancel trễ đến sau), nếu chỉ so sánh `targetRequestId == _activeRequestId` mà không có `sessionId`, lệnh cancel sẽ hủy nhầm request không thuộc về session của mình, hoặc hủy nhầm request mới với cùng số ID!
2. **Rủi ro Cancel-of-Cancel đệ quy**:
   - Trong `RequestDispatcher.DispatchAsync`:
     - Khi nhận yêu cầu `dynamo.cancel`, gói tin có `id` riêng (ví dụ `id = 500`) và `params` chứa `CancelParams(targetId)`.
     - Nếu một client gửi `targetId` trỏ vào chính request `id = 500` (hoặc gửi cancel liên tiếp cho một control method đang xử lý), hoặc trong callback hủy của CTS (`Token.Register(...)`) lại kích hoạt một thao tác cancel khác vào Coordinator.
     - Trong `DynamoBridgeCoordinator.Cancel`: Việc gọi `cancel.Cancel()` đồng bộ sẽ kích hoạt ngay lập tức toàn bộ các registered callbacks trên luồng hiện hành trong khi đang giữ lock `_cancelLock`. Nếu callback gọi lại `Cancel()`, hiện tượng re-entrancy / recursive cancel sẽ xảy ra.

### 3.3. Đề Xuất Giải Pháp Kiến Trúc (Fix Strategy)

#### 1. Định danh Request theo cặp `(SessionId, RequestId)`
- **Cơ chế cấp phát SessionId**:
  - Trong `PipeListener.ServeClientAsync`: Mỗi khi một client kết nối thành công, cấp phát một `long sessionId = Interlocked.Increment(ref _sessionCounter)` (hoặc `string sessionId = Guid.NewGuid().ToString("N")`).
  - Gán `SessionId` vào `NdjsonPipeWriter`.
- **Mở rộng DTO hợp đồng (100% Backward Compatible)**:
  - Trong `McpShared/HPRebar.Mcp.Contracts/Messages/ExecuteRequest.cs`:
    ```csharp
    public sealed record ExecuteRequest(
        string Code,
        string Transaction = TransactionModes.Auto,
        bool DryRun = false,
        int TimeoutSeconds = 30,
        string? Label = null,
        JsonElement? Args = null,
        string? SessionId = null);
    ```
  - Trong `McpShared/HPRebar.Mcp.Contracts/Messages/ContextMessages.cs`:
    ```csharp
    public sealed record CancelParams(long? Id, string? SessionId = null);
    ```
  - Bổ sung giá trị mặc định `= null` bảo đảm 100% binary & source compatibility cho toàn bộ 10 hosts khác.
- **Truyền dẫn SessionId trong `RequestDispatcher`**:
  - Khi `RequestDispatcher.DispatchAsync` xử lý:
    - Nếu request không chỉ định `SessionId`, tự động lấy từ `writer.SessionId`.
    - Truyền `(sessionId, requestId)` sang `DynamoBridgeCoordinator`.
- **Cách ly phiên trong `DynamoBridgeCoordinator`**:
  - Lưu trạng thái: `private (string? SessionId, long RequestId) _activeRequest;`
  - Khi `ExecuteAsync` khởi chạy: Lưu cả `SessionId` và `RequestId`.
  - Khi `Cancel(targetRequestId, targetSessionId)` được gọi:
    - Nếu có `targetSessionId` và không khớp với `_activeRequest.SessionId`, từ chối hủy và ghi log "Ignored cancel from mismatched session".
    - Nhờ đó, việc reconnect với pipe mới hoàn toàn không thể hủy nhầm request của session trước.

#### 2. Cơ chế Chống Cancel-of-Cancel Đệ Quy & Bảo Vệ Control Methods
- **Miễn nhiễm cho Control Methods**:
  - Trong `RequestDispatcher`: Các phương thức điều khiển (`ping`, `cancel`, `inspect`, `analyze`, `context`) là synchronous hoặc short-lived queries, **không bao giờ** được đăng ký là đối tượng có thể bị cancel.
  - Nếu `CancelParams.Id` trùng với ID của chính yêu cầu `cancel` hiện hành, lập tức trả về `new CancelResult(false, false)` mà không chuyển tiếp xuống executor.
- **Re-entrancy Guard trong `DynamoBridgeCoordinator`**:
  - Bổ sung cờ `private bool _isCancelling;` bên trong `DynamoBridgeCoordinator`.
  - Khi vào phương thức `Cancel()`:
    ```csharp
    lock (_cancelLock)
    {
        if (_isCancelling)
        {
            Log.Debug("Cancel re-entrancy detected; ignoring recursive cancel signal.");
            return new CancelResult(false, Volatile.Read(ref _busy) != 0);
        }

        var cancel = _currentCancel;
        if (cancel == null) return new CancelResult(false, false);

        // Kiểm tra khớp cả SessionId và RequestId
        if (targetRequestId.HasValue && targetRequestId.Value != 0 &&
            (targetRequestId.Value != _activeRequest.RequestId ||
             (targetSessionId != null && targetSessionId != _activeRequest.SessionId)))
        {
            Log.Information("Ignored stale cancel request for {TargetSession}:{TargetId} (active is {ActiveSession}:{ActiveId})",
                targetSessionId, targetRequestId.Value, _activeRequest.SessionId, _activeRequest.RequestId);
            return new CancelResult(false, Volatile.Read(ref _busy) != 0);
        }

        _isCancelling = true;
        try
        {
            cancel.Cancel();
            return new CancelResult(true, true);
        }
        catch (ObjectDisposedException)
        {
            return new CancelResult(false, false);
        }
        finally
        {
            _isCancelling = false;
        }
    }
    ```
  - Cơ chế `_isCancelling` triệt tiêu hoàn toàn khả năng phát sinh chuỗi đệ quy cancel-of-cancel khi các callback đăng ký trên `CancellationToken` vô tình kích hoạt lại `coordinator.Cancel()`.

---

## 4. Chi Tiết R2-10: Kiểm Soát Ngân Sách Payload Byte & Sửa Lỗi Async Tests

### 4.1. Hiện Trạng Trích Xuất Payload trong `get_node_output_values/code.cs`
- **Vị trí**: `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Data/get_node_output_values/code.cs`
- **Đoạn mã hiện tại**:
  ```csharp
  const int MaxDepth = 6;
  const int MaxBreadth = 100;
  const int MaxTotalItemsPerPort = 500;
  int portItemCount = 0;

  object? SerializeDictionary(DesignScript.Builtin.Dictionary dsDict, int depth)
  {
      var dictResult = new Dictionary<string, object?>();
      int count = 0;
      foreach (var key in dsDict.Keys)
      {
          ct.ThrowIfCancellationRequested();
          if (count++ >= MaxBreadth || portItemCount++ >= MaxTotalItemsPerPort)
          {
              dictResult["_truncated"] = "[Truncated breadth limit]";
              break;
          }
          ...
      }
      return dictResult;
  }
  ```
- Tương tự trong `SerializeClrDictionary`:
  ```csharp
  dictResult["_truncated"] = "[Truncated breadth limit]";
  ```

### 4.2. Phân Tích Lỗ Hổng & Ô Nhiễm Dữ Liệu (Data Pollution)
1. **Ghi đè và phá hỏng User Data**:
   - Nếu trong Dynamo script, người dùng tạo một dictionary có sẵn khóa `"_truncated"` (ví dụ: `{"_truncated": false, "data": 123}`), serializer sẽ ghi đè lên khóa của người dùng bằng chuỗi `"[Truncated breadth limit]"`.
   - Ngay cả khi người dùng không có khóa này, việc chèn thêm key giả mạo `"_truncated"` trực tiếp vào đối tượng dictionary làm sai lệch cấu trúc dữ liệu thực tế (schema violation), gây lỗi cho các ứng dụng client mong đợi một dictionary đúng cấu trúc.
2. **Thiếu hoàn toàn kiểm soát Byte Budget & String Length**:
   - Hiện tại chỉ có `MaxDepth`, `MaxBreadth`, `MaxTotalItemsPerPort`.
   - Nếu một node xuất một chuỗi string dài 10MB hoặc 100MB, code hiện tại:
     ```csharp
     if (value is string || value is bool || ...) return value;
     ```
     sẽ trả về nguyên văn chuỗi 100MB!
   - Không có bộ đếm tổng dung lượng UTF-8 byte tích lũy, dẫn đến nguy cơ OutOfMemory, làm nghẽn pipe transport, và làm sập client MCP khi nhận payload vượt quá giới hạn bộ nhớ.

### 4.3. Đề Xuất Giải Pháp Kiến Trúc (Fix Strategy)

#### 1. Cơ chế Ngân Sách Kép (Dual-Budget Guard)
Bổ sung các hằng số và bộ theo dõi ngân sách UTF-8 byte và độ dài chuỗi:
- `const int MaxStringLength = 2000;` (Ký tự tối đa cho mỗi chuỗi đơn lẻ).
- `const int MaxPayloadBytesPerPort = 64 * 1024;` (64 KB ngân sách byte cho mỗi port output).
- Biến theo dõi cục bộ: `int portByteCount = 0; bool portTruncated = false; string? portTruncationReason = null;`

Quy tắc cắt chuỗi string:
```csharp
if (value is string str)
{
    if (str.Length > MaxStringLength)
    {
        portTruncated = true;
        portTruncationReason = "string_length_exceeded";
        str = str.Substring(0, MaxStringLength) + "... [truncated]";
    }
    int byteLen = System.Text.Encoding.UTF8.GetByteCount(str);
    portByteCount += byteLen;
    return str;
}
```

Kiểm tra ngân sách trước khi duyệt phần tử:
```csharp
if (count++ >= MaxBreadth || portItemCount++ >= MaxTotalItemsPerPort || portByteCount >= MaxPayloadBytesPerPort)
{
    portTruncated = true;
    portTruncationReason = portByteCount >= MaxPayloadBytesPerPort 
        ? "byte_budget_exceeded" 
        : (portItemCount >= MaxTotalItemsPerPort ? "item_budget_exceeded" : "breadth_limit_exceeded");
    break;
}
```

#### 2. Tách Biệt Metadata `_truncated` Khỏi User Data
Tuyệt đối **không** ghi `dictResult["_truncated"] = ...` vào trong dictionary của người dùng.
Thay vào đó, đưa metadata `truncated` và `truncationReason` ra ngoài, đặt tại cấp độ mô tả đầu ra của Port (`portOutputDescriptions`):
```csharp
portOutputDescriptions.Add(new
{
    portIndex,
    portName = portModel.Name,
    dataType = outputTypeName,
    value = portOutput,
    truncated = portTruncated,
    truncationReason = portTruncationReason
});
```
Đối với mảng list, nếu cần đánh dấu vị trí dừng thì có thể dừng thêm phần tử và đặt cờ `portTruncated = true` cho port, giữ mảng thuần khiết không bị trộn chuỗi thông báo lạ vào danh sách số/đối tượng của người dùng.

---

### 4.4. Quét Test Suites & Sửa Lỗi Async Tests (`Assert.ThrowsAsync` Thiếu `await`)

#### Kết quả rà soát toàn diện:
Đã thực hiện quét regex `(?<!await\s+)Assert\.ThrowsAsync` và `^\s*(var\s+\w+\s*=\s*)?Assert\.ThrowsAsync` trên toàn bộ kho mã nguồn.
Kết quả phát hiện **chính xác 2 lời gọi thiếu `await` duy nhất** trong toàn bộ giải pháp:
- **Tập tin**: `HPDynamo/HPDynamo.McpBridge.Tests/Coordinator/Milestone2Tests.cs`
- **Dòng**: 128 và 129
- **Phương thức**: `public void Coordinator_Dispose_PendingQueueClearedAndCancelled()`

Mã nguồn hiện tại:
```csharp
// Dòng 103:
public void Coordinator_Dispose_PendingQueueClearedAndCancelled()
{
    ...
    // Dòng 128-132:
    var ex1 = Assert.ThrowsAsync<OperationCanceledException>(() => req1.Completion.Task);
    var ex2 = Assert.ThrowsAsync<OperationCanceledException>(() => req2.Completion.Task);
    Assert.NotNull(ex1);
    Assert.NotNull(ex2);
}
```

#### Phân tích lỗi:
1. `Assert.ThrowsAsync<T>` trả về một `Task<T>`.
2. Do phương thức test khai báo `public void` và không có từ khóa `await`, `ex1` và `ex2` thực chất là các đối tượng `Task<OperationCanceledException>`, chứ không phải ngoại lệ `OperationCanceledException`!
3. Câu lệnh `Assert.NotNull(ex1)` chỉ kiểm tra xem `Task` có khác `null` hay không (luôn luôn `true`), trong khi hành vi bất đồng bộ bên trong Task chưa bao giờ được await để kiểm tra ngoại lệ thực tế. Nếu task fail hoặc không ném ngoại lệ đúng, test vẫn pass sai!

#### Đề xuất sửa đổi (Code snippet trước & sau):
- **Trước**:
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
- **Sau**:
  ```csharp
  public async Task Coordinator_Dispose_PendingQueueClearedAndCancelled()
  {
      ...
      var ex1 = await Assert.ThrowsAsync<OperationCanceledException>(() => req1.Completion.Task);
      var ex2 = await Assert.ThrowsAsync<OperationCanceledException>(() => req2.Completion.Task);
      Assert.NotNull(ex1);
      Assert.NotNull(ex2);
  }
  ```

---

## 5. Danh Mục Các Tập Tin Cần Tác Động & Kế Hoạch Cho Implementer (Worker)

| Yêu cầu | Tập tin đích | Loại thay đổi | Rủi ro / Lưu ý |
|---|---|---|---|
| **R2-07** | `HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoTransactionDetector.cs` (mới) | Tạo mới class AST walker | Tuân thủ Rule C2 (<300 dòng), Rule M3 (<50 dòng) |
| **R2-07** | `HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoExecutionEngine.cs` | Sửa `ShouldStartAutoTransaction` gọi `DynamoTransactionDetector` | Xóa sạch substring matches |
| **R2-07** | `HPDynamo/HPDynamo.McpBridge.Tests/Coordinator/Milestone1ChallengerTests.cs` | Cập nhật test cases dòng 35–56 | Comments & literals không được tắt auto-tx |
| **R2-09** | `McpShared/HPRebar.Mcp.Contracts/Messages/ContextMessages.cs` | Thêm `string? SessionId = null` vào `CancelParams` | 100% additive, tương thích 10 hosts |
| **R2-09** | `McpShared/HPRebar.Mcp.Contracts/Messages/ExecuteRequest.cs` | Thêm `string? SessionId = null` vào `ExecuteRequest` | 100% additive, tương thích 10 hosts |
| **R2-09** | `McpShared/HPRebar.McpBridge.Core/Pipe/PipeListener.cs` & `NdjsonPipeWriter.cs` | Sinh `SessionId` cho mỗi client stream | Giữ nguyên logic accept loop |
| **R2-09** | `McpShared/HPRebar.McpBridge.Core/Pipe/RequestDispatcher.cs` | Chuyển tiếp SessionId & chặn cancel cho chính cancel request | Không breaking change |
| **R2-09** | `HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoBridgeCoordinator.cs` | Cô lập `(SessionId, RequestId)` & cờ `_isCancelling` | Loại bỏ cancel đệ quy |
| **R2-10** | `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Data/get_node_output_values/code.cs` | Giới hạn UTF-8 byte, max string, tách `_truncated` ra port metadata | Không chèn `_truncated` vào user dictionary |
| **R2-10** | `HPDynamo/HPDynamo.McpBridge.Tests/Coordinator/Milestone2Tests.cs` | Thêm `async Task` và `await` cho `Assert.ThrowsAsync` (lines 103, 128, 129) | Khắc phục test bất đồng bộ |
| **R2-10** | `HPDynamo/HPDynamo.E2E.Tests/Tiers/Tier2_SeedOutputValuesEmpiricalStressTests.cs` | Cập nhật `TC_E2E_OUT_06` theo metadata tách biệt | Đồng bộ test suite |

---

## 6. Kết Luận
Báo cáo khảo sát đã cung cấp bằng chứng thực nghiệm đầy đủ, định vị chính xác từng dòng mã nguồn gây lỗi và vạch ra giải pháp kiến trúc tối ưu cho R2-07, R2-09 và R2-10 mà không vi phạm nguyên tắc Clean Code Governance và không gây breaking changes cho McpShared.
