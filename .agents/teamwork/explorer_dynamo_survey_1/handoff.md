# Handoff Report — explorer_dynamo_survey_1

## 1. Observation

1. **RV-01 (Hardcoded `startAutoTransaction: false`):**
   - File: `HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoExecutionEngine.cs`, lines 109–123:
     ```csharp
     var work = new McpBridgeWorkRequest("csharp", (uiapp, ct) =>
         RevitTransactionScope.Execute(uiapp, request, settings, progress, cancel, (doc, linkedToken, log) =>
         {
             // ...
             return compiled.Script!.RunAsync(globals, linkedToken).GetAwaiter().GetResult().ReturnValue;
         }, startAutoTransaction: false), cancel.Token);
     ```
   - File: `HPDynamo/HPDynamo.McpBridge/Execution/RevitTransactionScope.cs`, lines 228–232:
     ```csharp
     if (mode == TransactionModes.Auto && startAutoTransaction)
     {
         txContext.StartAutoTransaction("MCP Dynamo auto-step");
     }
     ```
     Khi `startAutoTransaction` là `false`, phương thức `txContext.StartAutoTransaction` không bao giờ được gọi.
   - File: `HPDynamo/HPDynamo.McpBridge/Execution/RevitTransactionScope.cs`, lines 223–226:
     ```csharp
     if (mode != TransactionModes.None)
     {
         txContext.StartGroup("MCP Dynamo: " + label);
     }
     ```
     Chỉ có `TransactionGroup` được mở. Revit API quy định: trong `TransactionGroup`, `doc.IsModifiable` là `false`; mọi thao tác ghi DB trực tiếp đều ném ngoại lệ `Autodesk.Revit.Exceptions.InvalidOperationException: Modifying the document is not permitted while no transaction is active`.

2. **RV-06 (`dryRun` bị tước bỏ tại DynamicToolRegistrar):**
   - File: `McpShared/HPRebar.Mcp.Server.Core/Registry/DynamicToolRegistrar.cs`, lines 53–57:
     ```csharp
     if (string.Equals(key, DryRunKey, StringComparison.OrdinalIgnoreCase))
     {
         dryRun = element.ValueKind == JsonValueKind.True || (element.ValueKind == JsonValueKind.String && bool.TryParse(element.GetString(), out var b) && b);
         continue;
     }
     payload[key] = element.ValueKind == JsonValueKind.Undefined ? null : JsonNode.Parse(element.GetRawText());
     ```
     Cờ `dryRun` bị loại bỏ khỏi `payload` (tham số gửi vào tool `args`).
   - File: `McpShared/HPRebar.Mcp.Server.Core/Registry/ToolManager.cs`, line 174:
     ```csharp
     var request = new ExecuteRequest(record.Code, record.Transaction, dryRun, timeout, name, args);
     ```
     `dryRun` được đưa vào trường `ExecuteRequest.DryRun`. `args` không chứa key `dryRun`.
   - File: `HPDynamo/HPDynamo.McpBridge/Support/DynamoScriptGlobals.cs`, lines 14–26:
     Lớp `DynamoScriptGlobals` chỉ có các thuộc tính `dynamo`, `workspace`, `doc`, `uidoc`, `app`, `uiapp`, `ct`, `log`, `progress`, `args`. Hoàn toàn không có thuộc tính `dryRun`.
   - File: `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/batch_edit_graph/code.cs`, line 5:
     ```csharp
     bool dryRun = args.Bool("dryRun", false);
     ```
     Vì `args` không có `dryRun`, `args.Bool("dryRun", false)` luôn trả về `false`.

3. **RV-06 (PerformRollback khiếm khuyết & thiếu catch block):**
   - File: `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/batch_edit_graph/code.cs`, lines 208–214:
     ```csharp
     void PerformRollback()
     {
         foreach (var guid in createdNodeGuids)
         {
             dynamoModel!.ExecuteCommand(new DynamoModel.DeleteModelCommand(guid));
         }
     }
     ```
     Chỉ xoá node mới. Dây cũ bị đứt do cắm dây mới đè lên không được nối lại; thuộc tính node cũ đã sửa không được khôi phục; dây mới nối giữa 2 node cũ không bị xoá.
   - File: `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/batch_edit_graph/code.cs`, lines 216–238:
     ```csharp
     try
     {
         ProcessNodes();
         ProcessWires();
         ProcessProperties();
         if (runEvaluation && homeWs != null && !dryRun) homeWs.Run();
         if (dryRun) PerformRollback();
     }
     finally
     {
         if (homeWs != null && prevRunType.HasValue) homeWs.RunSettings.RunType = prevRunType.Value;
     }
     ```
     Không có khối `catch`. Khi ném lỗi hoặc cancel ở bất kỳ bước nào, `PerformRollback()` bị bỏ qua, để lại các thay đổi dở dang trên canvas.

---

## 2. Logic Chain

1. Từ **Observation 1**, `DynamoExecutionEngine.RunRoslynCodeAsync` truyền `startAutoTransaction: false` một cách cố định cho mọi Roslyn script. Do đó `RevitTransactionScope.Execute` không mở transaction con cho `transaction = "auto"`. Kết quả là các script C# gọi trực tiếp Revit API (`doc.Create`, set parameter) bị Revit từ chối vì thiếu transaction. Mặt khác, các script gọi Dynamo wrappers (`Revit.Elements.*`) hoặc Dynamo `TransactionManager` lại cần `startAutoTransaction: false` để tránh lỗi lồng transaction. Do đó, cần một bộ phân định tự động (heuristic analysis) kiểm tra mã nguồn script để chọn `startAutoTransaction: true` cho pure Revit DB calls và `false` cho Dynamo wrappers.
2. Từ **Observation 2**, khi một caller gọi tool `batch_edit_graph` qua MCP với tham số `dryRun: true`, `DynamicToolRegistrar` bóc tách `dryRun` ra khỏi `args` và chỉ truyền trong `ExecuteRequest.DryRun`. Khi vào bridge, `DynamoExecutionEngine` không truyền `dryRun` vào `ScriptArgs` hay `DynamoScriptGlobals`. Do đó, `batch_edit_graph/code.cs` đọc `args.Bool("dryRun", false)` và luôn nhận được `false`. Kết quả là `batch_edit_graph` thực thi như một lần chạy thật, thậm chí có thể chạy evaluation, và bỏ qua bước `PerformRollback()`. Sau đó `RevitTransactionScope` rollback `TransactionGroup` trên Revit nhưng canvas Dynamo không được rollback.
3. Từ **Observation 3**, ngay cả khi `dryRun` được phát hiện, hàm `PerformRollback()` hiện tại chỉ xoá các node trong `createdNodeGuids`. Việc này bỏ qua 3 trạng thái quan trọng: (a) các dây cũ bị đứt khi cắm dây mới vào Input port, (b) các thuộc tính (Name, Value/Code, IsFrozen, X, Y) của các node cũ đã bị ghi đè, (c) các dây mới nối giữa 2 node cũ. Hơn nữa, việc thiếu khối `catch` đồng nghĩa với việc bất kỳ lỗi runtime nào (hoặc cancellation) trong quá trình batch sẽ để lại canvas ở trạng thái bị sửa đổi một phần mà không được dọn dẹp.
4. Từ các bước 1–3, giải pháp toàn diện bao gồm:
   - Thêm bộ heuristic trong `DynamoExecutionEngine` để bật `startAutoTransaction: true` khi script C# thao tác Revit DB thuần.
   - Thêm `dryRun` vào `DynamoScriptGlobals` và hợp nhất `dryRun: true` vào `ScriptArgs` trong `DynamoExecutionEngine`.
   - Nâng cấp `batch_edit_graph/code.cs` với Programmatic Rollback Snapshot ghi nhận replaced wires, modified properties, created wires giữa existing nodes, và bọc khối `try / catch (Exception) { PerformRollback(); throw; }`.

---

## 3. Caveats

- **Phạm vi khảo sát:** Khảo sát tập trung vào các luồng giao dịch và rollback của `execute_dynamo_code` và seed `batch_edit_graph`. Không khảo sát các tool RV-02 đến RV-05, RV-07 đến RV-08 (các vấn đề đó thuộc các track khác).
- **Môi trường Live:** Khảo sát dựa trên đối soát mã nguồn tĩnh và phân tích IL/metadata của thư viện Autodesk Dynamo 3.7 / Revit 2026. Do quy tắc bảo toàn phiên làm việc của người dùng, không inject mã vào tiến trình Revit đang mở.

---

## 4. Conclusion

- **RV-01** được xác định chính xác tại `DynamoExecutionEngine.cs:122` do hardcode `startAutoTransaction: false`. Giải pháp là bổ sung static heuristic kiểm tra `Revit.Elements.` và `TransactionManager` để bật `startAutoTransaction: true` cho direct C# Revit API script dưới `transaction = "auto"`.
- **RV-06** được xác định gồm 2 vấn đề:
  1. Vết đứt gãy lan truyền `dryRun` từ `DynamicToolRegistrar.cs:53` -> `ToolManager.cs:174` -> `DynamoExecutionEngine.cs:115` -> `batch_edit_graph/code.cs:5`. Giải pháp là đưa `dryRun` vào `DynamoScriptGlobals` và hợp nhất vào `ScriptArgs`.
  2. Canvas rollback khiếm khuyết trong `batch_edit_graph/code.cs`. Giải pháp là Programmatic Rollback Snapshot đầy đủ (replaced wires, modified properties, created wires giữa existing nodes) kèm bọc `try / catch { PerformRollback(); throw; }`.
- Chi tiết báo cáo và mã nguồn đề xuất đã được ghi nhận đầy đủ tại `report.md`.

---

## 5. Verification Method

1. **Kiểm tra mã nguồn:**
   - Xem `report.md` tại `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_dynamo_survey_1\report.md`.
2. **Kiểm tra unit tests:**
   - Biên dịch và chạy bộ test hiện hành:
     ```bash
     dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests
     dotnet test HPDynamo/HPDynamo.McpBridge.Tests
     dotnet test HPDynamo/HPDynamo.E2E.Tests
     dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests
     dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests
     ```
3. **Điều kiện vô hiệu hóa (Invalidation Conditions):**
   - Kết luận về RV-01 bị vô hiệu nếu có cơ chế khác trong Dynamo tự động nhận diện transaction ngoài của Revit mà không xung đột.
   - Kết luận về RV-06 bị vô hiệu nếu `DynamicToolRegistrar` thay đổi wire contract để giữ lại `dryRun` trong `payload` (điều này vi phạm nguyên tắc bảo toàn 10 host khác của McpShared).
