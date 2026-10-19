# Báo Cáo Điều Tra Kỹ Thuật: RV-01 & RV-06 (HPDynamo MCP Subsystem)

**Người thực hiện:** `explorer_dynamo_survey_1`  
**Ngày thực hiện:** 2026-10-09  
**Mã kế hoạch liên quan:** `261009-1118-hpdynamo-mcp-performance`  
**Mục tiêu:** Điều tra chuyên sâu và đưa ra phương án giải quyết dứt điểm 2 vấn đề trọng tâm:  
1. **RV-01**: Quản trị giao dịch trong `DynamoExecutionEngine` & `RevitTransactionScope` (phân biệt thao tác Revit DB thuần vs Dynamo wrappers / `TransactionManager`).  
2. **RV-06**: Lan truyền cờ `dryRun` tại execution boundary & Cơ chế Rollback Canvas toàn diện trong `batch_edit_graph`.

---

## 1. Tóm Tắt Cốt Lõi (Executive Summary)

Qua việc đối soát chi tiết mã nguồn giữa `HPDynamo/` (`HPDynamo.McpBridge`, `HPDynamo.Mcp.Server`) và `McpShared/` (`HPRebar.Mcp.Contracts`, `HPRebar.McpBridge.Core`, `HPRebar.Mcp.Server.Core`), chúng tôi phát hiện 2 lỗi nghiêm trọng ở mức kiến trúc execution:

1. **RV-01 (Transaction Auto bị vô hiệu hóa ngầm):**
   - Tại `DynamoExecutionEngine.cs:122`, tham số `startAutoTransaction: false` bị hardcode cho mọi C# Roslyn script.
   - Khi đó, `RevitTransactionScope.cs:228-232` không mở `_autoTx` (child `Transaction`), mà chỉ mở `TransactionGroup`.
   - Trong Revit API, `TransactionGroup` **không cho phép ghi** (`doc.IsModifiable == false`). Bất kỳ script C# nào gọi trực tiếp Revit API (`doc.Create...`, `param.Set(...)`, `doc.Delete(...)`) dưới chế độ `transaction = "auto"` đều bị Revit từ chối với ngoại lệ `InvalidOperationException` ("Modifying the document is not permitted while no transaction is active").
   - Gốc rễ: Cấu hình `startAutoTransaction: false` trước đây được áp dụng để tránh xung đột với Dynamo's `TransactionManager` (vốn tự mở child transaction). Tuy nhiên, việc áp dụng cào bằng đã phá vỡ hợp đồng của tool `execute_dynamo_code`.

2. **RV-06 (Mất cờ dryRun & Canvas không thể rollback):**
   - Tại `DynamicToolRegistrar.cs:53-57` (phía Server), cờ `dryRun` bị tách khỏi tham số `args` (loại bỏ khỏi `payload`) và chỉ được truyền qua `ExecuteRequest.DryRun`.
   - Phía Bridge, `DynamoExecutionEngine.cs:115-120` chỉ bọc `request.Args` vào `ScriptArgs` và không đưa `dryRun` vào `DynamoScriptGlobals`.
   - Do đó, script `batch_edit_graph/code.cs:5` gọi `args.Bool("dryRun", false)` **luôn luôn nhận giá trị `false`** khi được gọi như một dynamic registry tool!
   - Thêm vào đó, hàm `PerformRollback()` trong `batch_edit_graph/code.cs:208` chỉ xoá node mới tạo; hoàn toàn không phục hồi dây (wires) cũ bị đứt và không hoàn tác thuộc tính (properties) đã sửa trên các node có sẵn.
   - Khi phát sinh ngoại lệ hoặc cancel giữa chừng, `batch_edit_graph/code.cs` không có khối `catch`, khiến các thay đổi dở dang bị bỏ lại trên canvas Dynamo vĩnh viễn.

---

## 2. Điều Tra Chuyên Sâu RV-01: Quản Trị Giao Dịch Trong DynamoExecutionEngine & RevitTransactionScope

### 2.1 Kiến Trúc Giao Dịch Revit Hiện Tại & Vị Trí Mã Nguồn

| Thành phần | Đường dẫn tệp | Lớp / Phương thức | Dòng | Hành vi hiện tại |
|---|---|---|---|---|
| **Roslyn Dispatcher** | `HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoExecutionEngine.cs` | `DynamoExecutionEngine.RunRoslynCodeAsync` | 109–123 | Gọi `RevitTransactionScope.Execute(..., startAutoTransaction: false)` |
| **DesignScript Dispatcher** | `HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoExecutionEngine.cs` | `DynamoExecutionEngine.RunDesignScriptAsync` | 60–66 | Gọi `RevitTransactionScope.Execute(..., startAutoTransaction: false)` |
| **DynFile Dispatcher** | `HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoExecutionEngine.cs` | `DynamoExecutionEngine.RunDynFileAsync` | 79–85 | Gọi `RevitTransactionScope.Execute(..., startAutoTransaction: false)` |
| **Transaction Scope** | `HPDynamo/HPDynamo.McpBridge/Execution/RevitTransactionScope.cs` | `RevitTransactionScope.BeginTransactions` | 221–232 | `if (mode == TransactionModes.Auto && startAutoTransaction) txContext.StartAutoTransaction(...)` |
| **Transaction Scope Finalize** | `HPDynamo/HPDynamo.McpBridge/Execution/RevitTransactionScope.cs` | `RevitTransactionScope.FinalizeTransactions` | 234–256 | Commit `_autoTx`, sau đó `RollbackGroup()` nếu `request.DryRun` hoặc `AssimilateGroup()` nếu commit |
| **Transaction Context** | `HPDynamo/HPDynamo.McpBridge/Execution/RevitTransactionContext.cs` | `RevitTransactionContext` | 27–48 | Bọc `TransactionGroup` và `Transaction` (`_autoTx`) với `AutoDismissFailurePreprocessor` |

### 2.2 Cơ Chế Revit API: `TransactionGroup` vs `Transaction`

Trong Autodesk Revit API:
- **`TransactionGroup`**: Chỉ là một khung gom nhóm logic bao ngoài. `TransactionGroup` **không cấp quyền ghi** vào Revit Database. Thuộc tính `Document.IsModifiable` vẫn là `false`. Nếu không có một `Transaction` thực sự nào được mở bên trong `TransactionGroup`, mọi lệnh can thiệp hình học hay tham số đều ném ngoại lệ:
  ```text
  Autodesk.Revit.Exceptions.InvalidOperationException: Modifying the document is not permitted while no transaction is active.
  ```
- **`Transaction`**: Cấp quyền ghi trực tiếp vào Revit Database (`doc.IsModifiable == true`). Revit áp đặt nguyên tắc: **tại một thời điểm, một Document chỉ có thể mở tối đa 1 Transaction duy nhất**.
  Nếu một transaction đang mở mà có mã nguồn khác gọi `new Transaction(doc).Start()`, Revit lập tức ném:
  ```text
  Autodesk.Revit.Exceptions.InvalidOperationException: Starting a transaction is not permitted while another transaction is open.
  ```

### 2.3 Xung Đột Giữa Pure Revit DB Calls và Dynamo Wrappers

Trong phân hệ Dynamo, mã nguồn C# thực thi qua `execute_dynamo_code` thuộc 2 nhóm đối tượng hoàn toàn khác nhau:

1. **Nhóm 1: Pure Revit DB Calls (Thao tác Revit API trực tiếp)**
   - Ví dụ: gọi `doc.Create.NewFamilyInstance(...)`, `wall.LookupParameter("Comments").Set("value")`, `doc.Delete(elementId)`.
   - Các lệnh này **không thông qua runtime của Dynamo**. Chúng cần một Revit `Transaction` thực sự đang mở.
   - Nếu `startAutoTransaction: false`, không có transaction nào được mở -> Lỗi `No transaction is active`.

2. **Nhóm 2: Dynamo Wrappers & Dynamo Evaluation**
   - Ví dụ: gọi `Revit.Elements.Wall.ByCurveAndHeight(...)`, `Revit.Elements.Element.SetParameterByName(...)`, hoặc gọi `Dynamo.Applications.TransactionManager.Instance.EnsureInTransaction(doc)`.
   - Các lớp wrapper của Dynamo (trong thư viện `RevitNodes.dll`) được thiết kế để tự động quản lý transaction con thông qua singleton `Dynamo.Applications.TransactionManager.Instance`. Khi được gọi, `TransactionManager` sẽ tự gọi `Transaction.Start()` trên Revit Document.
   - Nếu Bridge đã mở sẵn `_autoTx` (`startAutoTransaction: true`), khi `TransactionManager` cố mở transaction con, Revit sẽ báo lỗi lồng transaction (`Starting a transaction is not permitted while another transaction is open`).

3. **Nhóm 3: DesignScript & DynFile**
   - Đều chạy qua engine ProtoCore hoặc Dynamo Workspace -> Luôn sử dụng `TransactionManager` của Dynamo. Do đó, với `RunDesignScriptAsync` và `RunDynFileAsync`, giữ `startAutoTransaction: false` là hoàn toàn chính xác.

### 2.4 Giải Pháp Đề Xuất Cho RV-01: Bộ Phân Định Quyền Transaction Thông Minh

Trong `DynamoExecutionEngine.RunRoslynCodeAsync`, không được hardcode `startAutoTransaction: false`. Cần bổ sung hàm kiểm tra tĩnh (static heuristic analysis) trên mã nguồn `request.Code`:

```csharp
private static bool ShouldStartAutoTransaction(string code, string? transactionMode)
{
    var mode = TransactionModes.Normalize(transactionMode) ?? TransactionModes.Auto;
    if (mode != TransactionModes.Auto) return false;

    // 1. Nếu script tự quản lý transaction thủ công trong code: không mở auto
    if (code.Contains("new Transaction(", StringComparison.Ordinal) ||
        code.Contains("new Autodesk.Revit.DB.Transaction(", StringComparison.Ordinal))
    {
        return false;
    }

    // 2. Nếu script sử dụng Dynamo Wrappers hoặc TransactionManager của Dynamo: nhường quyền cho Dynamo
    if (code.Contains("Revit.Elements.", StringComparison.Ordinal) ||
        code.Contains("TransactionManager", StringComparison.Ordinal) ||
        code.Contains("Dynamo.Applications", StringComparison.Ordinal) ||
        code.Contains("activeCanvasWs.Run(", StringComparison.Ordinal) ||
        code.Contains("ws.Run(", StringComparison.Ordinal) ||
        code.Contains("homeWs.Run(", StringComparison.Ordinal))
    {
        return false;
    }

    // 3. Mặc định cho C# Roslyn thao tác Revit Document với mode=auto: mở child transaction
    return true;
}
```

**Lợi ích:**
- Với script tạo element Revit thuần (`doc.Create...`): `ShouldStartAutoTransaction` trả về `true` -> Bridge mở `_autoTx` -> Script chạy trơn tru, được commit tự động, hoặc rollback khi `dryRun = true`.
- Với script gọi `Revit.Elements.*` hoặc seed chạy đồ thị (`run_dyn_file`): `ShouldStartAutoTransaction` trả về `false` -> Dynamo `TransactionManager` tự quản lý transaction con bên trong `TransactionGroup` -> Không xảy ra xung đột lồng transaction.

---

## 3. Điều Tra Chuyên Sâu RV-06: Lan Truyền dryRun & Rollback Canvas Trong batch_edit_graph

### 3.1 Vết Đứt Gãy Lan Truyền `dryRun` Tại Execution Boundary

Quy trình lan truyền `dryRun` hiện tại diễn ra như sau:

```
[MCP Client Tool Call]
  tools/call: batch_edit_graph
  Arguments: {"nodes": [...], "wires": [...], "dryRun": true}
       │
       ▼
[DynamicToolRegistrar.cs:53-57] (McpShared / Server)
  if (string.Equals(key, DryRunKey, StringComparison.OrdinalIgnoreCase))
  {
      dryRun = element.ValueKind == JsonValueKind.True;
      continue; // <--- CỜ dryRun BỊ BỎ QUA KHÔNG ĐƯA VÀO payload!
  }
  payload[key] = ...;
       │
       ▼
[ToolManager.cs:174] (McpShared / Server)
  var request = new ExecuteRequest(record.Code, record.Transaction, dryRun, timeout, name, args);
  // request.DryRun = true;
  // request.Args (JsonElement từ payload) KHÔNG CÓ KEY "dryRun"!
       │
       ▼ (Named Pipe RPC)
[DynamoExecutionEngine.cs:115-120] (HPDynamo.McpBridge)
  var globals = new DynamoScriptGlobals(
      dynamoGlobal, _coordinator.ActiveWorkspace, (Document)doc, ...,
      new ScriptArgs(request.Args)); // <--- ScriptArgs nhận request.Args thiếu dryRun!
       │
       ▼
[batch_edit_graph/code.cs:5] (C# Script Execution)
  bool dryRun = args.Bool("dryRun", false); // <--- KẾT QUẢ: LUÔN LUÔN LÀ FALSE!
```

**Hậu quả:**
1. Script `batch_edit_graph` tin rằng nó đang chạy thật (`dryRun == false`).
2. Nếu `runEvaluation == true`, script gọi `homeWs.Run()` tính toán đồ thị thật.
3. Script **bỏ qua việc gọi `PerformRollback()`** (dòng 227: `if (dryRun) PerformRollback();`).
4. Khi script kết thúc, `RevitTransactionScope` thấy `request.DryRun == true` nên rollback `TransactionGroup` của Revit.
5. **KẾT QUẢ KHÔNG ĐỒNG NHẤT:** Mô hình Revit thì rollback sạch sẽ, nhưng các Node và Dây mới vẫn nằm trơ trọi trên màn hình Dynamo Canvas!

### 3.2 Cơ Chế Rollback Canvas Dynamo: UndoRecorder vs Programmatic Snapshot

Trong Dynamo, có 2 môi trường hoạt động:
1. **Live UI Mode (`DynamoViewModel != null`):**
   - Dynamo UI có cơ chế `UndoRecorder` / `UndoRedoCommand`.
   - Tuy nhiên, các thao tác tạo node và nối dây qua `dynamoModel.ExecuteCommand(CreateNodeCommand / MakeConnectionCommand)` hoặc sửa property trực tiếp (`node.Name = ...`) không phải lúc nào cũng nhóm thành 1 undo entry nguyên tử (atomic undo group), trừ khi được bọc đặc biệt bằng `UndoRecorder.BeginCreationMarker()`.
2. **Headless Mode (`DynamoViewModel == null`, qua `RevitDynamoModel`):**
   - Hoàn toàn **không có giao diện UI**, không có undo stack cho người dùng.
   - Các lệnh `UndoRedoCommand` của UI không áp dụng được.

**Kết luận kỹ thuật:**  
Giải pháp bền vững, tin cậy 100% trong cả 2 chế độ (Live Canvas và Headless) bắt buộc phải dựa vào **Programmatic Rollback Snapshot (Chụp ảnh trạng thái trước khi sửa)**, kết hợp dọn dẹp có kiểm soát.

### 3.3 Ba Khiếm Khuyết Chí Mạng Trong `PerformRollback` Hiện Tại Của `batch_edit_graph`

Đoạn mã hiện tại trong `batch_edit_graph/code.cs`:
```csharp
// code.cs:208-214
void PerformRollback()
{
    foreach (var guid in createdNodeGuids)
    {
        dynamoModel!.ExecuteCommand(new DynamoModel.DeleteModelCommand(guid));
    }
}
```

1. **Khiếm khuyết 1: Dây cũ bị thay thế (Replaced Wires) bị mất vĩnh viễn:**
   - Trong Dynamo, mỗi cổng Input (`InPort`) chỉ có thể nối với **tối đa 1 dây**.
   - Khi `batch_edit_graph` nối 1 dây mới vào cổng Input của một node có sẵn mà cổng đó đang có dây khác cắm vào, Dynamo sẽ tự động ngắt kết nối dây cũ.
   - Khi rollback, `PerformRollback` hiện tại chỉ xoá các node trong `createdNodeGuids`. Dây cũ bị đứt **không bao giờ được nối lại**, làm hỏng đồ thị gốc của người dùng!

2. **Khiếm khuyết 2: Thuộc tính node có sẵn (Modified Properties) không được hoàn tác:**
   - Khi `batch_edit_graph` sửa `name`, `value`, `isFrozen`, `x`, `y` của một node đã tồn tại từ trước trên canvas, các giá trị cũ không hề được lưu lại.
   - Khi rollback, các node có sẵn giữ nguyên giá trị đã bị sửa!

3. **Khiếm khuyết 3: Dây mới nối giữa 2 node có sẵn không bị xoá:**
   - Nếu một dây mới được nối giữa Node A và Node B (cả 2 đều là node cũ có sẵn), dây này không gắn với node nào trong `createdNodeGuids`. Khi `PerformRollback` chạy, dây này vẫn tồn tại trên canvas!

4. **Khiếm khuyết 4: Thiếu khối `catch` khi có lỗi hoặc Cancellation:**
   - Khối thực thi ở dòng 216-238 chỉ có `try { ... } finally { ... }`.
   - Nếu bước tạo dây ném lỗi (ví dụ cổng out of bounds) hoặc `ct.ThrowIfCancellationRequested()` được kích hoạt, luồng thực thi nhảy thẳng sang `finally`.
   - `PerformRollback()` **không hề được gọi**, để lại đồ thị ở trạng thái vỡ vụn (nửa nạc nửa mỡ).

---

## 4. Thiết Kế Chi Tiết & Phương Án Khắc Phục (Actionable Recommendations)

### 4.1 Khắc Phục Lan Truyền `dryRun` Tại Execution Boundary

Cần thực hiện 2 bổ sung đồng bộ:

1. **Mở rộng `DynamoScriptGlobals.cs`:**
   Bổ sung thuộc tính `public bool dryRun { get; }`:
   ```csharp
   public sealed class DynamoScriptGlobals
   {
       // ... các thuộc tính hiện có ...
       public ScriptArgs args { get; }
       public bool dryRun { get; }

       public DynamoScriptGlobals(
           object dynamoModelOrViewModel,
           WorkspaceModel? activeWorkspace,
           Document document,
           UIDocument uiDocument,
           Application application,
           UIApplication uiApplication,
           CancellationToken cancellationToken,
           Action<string?> logAction,
           Action<int, int, string> progressAction,
           ScriptArgs scriptArguments,
           bool isDryRun = false)
       {
           // ...
           args = scriptArguments;
           dryRun = isDryRun;
       }
   }
   ```

2. **Hợp nhất `dryRun` vào `ScriptArgs` trong `DynamoExecutionEngine.cs`:**
   Khi khởi tạo `ScriptArgs`, nếu `request.DryRun == true` mà trong `request.Args` chưa có key `"dryRun"`, tự động hợp nhất `"dryRun": true` vào arguments.
   ```csharp
   var effectiveArgs = MergeDryRunFlag(request.Args, request.DryRun);
   var globals = new DynamoScriptGlobals(
       dynamoGlobal, _coordinator.ActiveWorkspace, (Autodesk.Revit.DB.Document)doc,
       uiapp.ActiveUIDocument, uiapp.Application, uiapp, linkedToken, log,
       (cur, tot, msg) => progress?.Report(new ScriptProgress(cur, tot, msg)),
       new ScriptArgs(effectiveArgs),
       request.DryRun);
   ```
   *Nhờ vậy, bất kể script đọc qua `globals.dryRun` hay qua `args.Bool("dryRun")`, cờ `dryRun` từ `ExecuteRequest` đều được phản ánh chính xác 100%.*

### 4.2 Thiết Kế Programmatic Rollback Snapshot Trong `batch_edit_graph`

Để bảo đảm rollback toàn vẹn 100%, `batch_edit_graph/code.cs` cần lưu trữ 3 danh sách snapshot trước khi thực hiện đột biến:

```csharp
// 1. Snapshot dây bị ngắt kết nối do cắm đè
var replacedWires = new List<(Guid SourceNodeGuid, int SourcePortIndex, Guid TargetNodeGuid, int TargetPortIndex)>();

// 2. Snapshot dây mới tạo nối giữa 2 node có sẵn
var createdWiresBetweenExistingNodes = new List<Guid>();

// 3. Snapshot thuộc tính gốc của các node có sẵn trước khi bị sửa
var originalNodeProps = new Dictionary<Guid, (string Name, string? Value, bool IsFrozen, double X, double Y)>();
```

#### Quy trình ghi nhận Snapshot:
1. **Trước khi nối dây (`ProcessWires`):**
   - Kiểm tra `dstNode.InPorts[toPort]`. Nếu đã có `connectorItem`:
     Ghi nhận `(connectorItem.Start.Owner.GUID, connectorItem.Start.Index, targetGuid, toPort)` vào `replacedWires`.
   - Nếu cả `sourceGuid` và `targetGuid` đều KHÔNG nằm trong `createdNodeGuids`:
     Sau khi tạo kết nối, tìm connector mới tương ứng và thêm GUID vào `createdWiresBetweenExistingNodes`.
2. **Trước khi đổi thuộc tính (`ProcessProperties`):**
   - Nếu `targetGuid` KHÔNG nằm trong `createdNodeGuids` và chưa có trong `originalNodeProps`:
     Lưu `(nodeModel.Name, (nodeModel is CodeBlockNodeModel cbn ? cbn.Code : null), nodeModel.IsFrozen, nodeModel.X, nodeModel.Y)` vào `originalNodeProps`.

#### Quy trình `PerformRollback()` toàn diện:
```csharp
void PerformRollback()
{
    // Bước 1: Xóa các dây mới nối giữa các node có sẵn
    foreach (var wireGuid in createdWiresBetweenExistingNodes)
    {
        dynamoModel?.ExecuteCommand(new DynamoModel.DeleteModelCommand(wireGuid));
    }

    // Bước 2: Tái kết nối các dây cũ bị ngắt
    foreach (var rw in replacedWires)
    {
        dynamoModel?.ExecuteCommand(new DynamoModel.MakeConnectionCommand(
            rw.SourceNodeGuid, rw.SourcePortIndex, PortType.Output, DynamoModel.MakeConnectionCommand.Mode.Begin));
        dynamoModel?.ExecuteCommand(new DynamoModel.MakeConnectionCommand(
            rw.TargetNodeGuid, rw.TargetPortIndex, PortType.Input, DynamoModel.MakeConnectionCommand.Mode.End));
    }

    // Bước 3: Phục hồi thuộc tính gốc của các node có sẵn
    foreach (var (guid, orig) in originalNodeProps)
    {
        var node = currentWorkspace.Nodes.FirstOrDefault(n => n.GUID == guid);
        if (node == null) continue;
        node.Name = orig.Name;
        node.IsFrozen = orig.IsFrozen;
        node.X = orig.X;
        node.Y = orig.Y;
        node.ReportPosition();
        if (orig.Value != null && node is CodeBlockNodeModel cbn)
        {
            cbn.SetCodeContent(orig.Value, currentWorkspace.ElementResolver);
        }
    }

    // Bước 4: Xóa các node mới tạo (theo thứ tự ngược lại)
    for (int i = createdNodeGuids.Count - 1; i >= 0; i--)
    {
        dynamoModel?.ExecuteCommand(new DynamoModel.DeleteModelCommand(createdNodeGuids[i]));
    }
}
```

#### Quy trình bọc `try / catch`:
```csharp
try
{
    ProcessNodes();
    ProcessWires();
    ProcessProperties();

    if (runEvaluation && homeWs != null && !dryRun)
    {
        homeWs.Run();
    }

    if (dryRun)
    {
        PerformRollback();
    }
}
catch (Exception)
{
    PerformRollback();
    throw;
}
finally
{
    if (homeWs != null && prevRunType.HasValue)
    {
        homeWs.RunSettings.RunType = prevRunType.Value;
    }
}
```

---

## 5. Phương Pháp Kiểm Chứng Độc Lập (Verification Method)

Khi tiến hành triển khai sửa lỗi, các kịch bản kiểm thử sau đây bắt buộc phải được thực thi và xác nhận:

1. **Kiểm thử RV-01 (Direct C# Auto Transaction vs Dynamo Wrapper):**
   - Viết test trong `HPDynamo.McpBridge.Tests`:
     - Test 1: Thực thi script C# gọi `doc.Create.NewFamilyInstance(...)` với `transaction = "auto"`. Xác nhận `startAutoTransaction` được bật, mã thực thi thành công, không gặp lỗi `No transaction is active`.
     - Test 2: Thực thi script C# có chứa chuỗi `"Revit.Elements.Wall"` hoặc `"TransactionManager"`. Xác nhận `startAutoTransaction` tắt, nhường quyền cho Dynamo.
     - Test 3: Thực thi script với `transaction = "none"` ghi dữ liệu; xác nhận bị từ chối sạch sẽ ngay từ khâu validate.

2. **Kiểm thử RV-06 (dryRun Propagation qua DynamicToolRegistrar):**
   - Viết test mô phỏng chu trình thực tế từ `DynamicToolRegistrar.InvokeCoreAsync` -> `ToolManager.RunAsync` -> `DynamoExecutionEngine.DispatchAsync`.
   - Truyền arguments `{"nodes": [...], "dryRun": true}`.
   - Xác nhận:
     - `args.Bool("dryRun")` trong script nhận `true`.
     - Sau khi lệnh kết thúc, canvas Dynamo không còn bất kỳ node mới nào.
     - Dây cũ trên canvas được bảo tồn nguyên vẹn.
     - Thuộc tính của node cũ được phục hồi nguyên trạng.

3. **Kiểm thử RV-06 (Rollback khi gặp lỗi giữa chừng):**
   - Tạo batch gồm 3 thao tác: Thao tác 1 (tạo Node A hợp lệ), Thao tác 2 (sửa property Node cũ B), Thao tác 3 (cố nối dây vào cổng -99 gây exception).
   - Xác nhận:
     - Lệnh ném ngoại lệ.
     - Node A bị xoá khỏi canvas.
     - Node B phục hồi lại thuộc tính ban đầu.
     - Trạng thái `RunType` của workspace được khôi phục về giá trị trước batch.

4. **Kiểm thử hồi quy toàn bộ Suite:**
   - Chạy các lệnh kiểm thử chuẩn của repo:
     ```bash
     dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests
     dotnet test HPDynamo/HPDynamo.McpBridge.Tests
     dotnet test HPDynamo/HPDynamo.E2E.Tests
     dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests
     dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests
     ```
   - Xác nhận 100% test suites tiếp tục PASS mà không gây hồi quy (regressions).
