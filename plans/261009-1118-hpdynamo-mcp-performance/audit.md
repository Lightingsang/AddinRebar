# HPDynamo Performance & Architecture Audit

**Ngày khảo sát:** 2026-10-09  
**Môi trường:** Revit 2026 (Build 26.5.0.55, Runtime .NET 10.0), Dynamo for Revit 3.7.0.11777  
**Phạm vi:** `HPDynamo/`, `McpShared/`, contracts, seed library, pipeline giao tiếp và bộ test harness.

---

## 1. Chuỗi Gọi Thực Tế & Phân Bổ Chi Phí (Latency Breakdown)

Chuỗi giao tiếp thực tế giữa Antigravity và Dynamo trong Revit diễn ra theo sơ đồ sau:

```text
Antigravity (AI Client)
       │ stdio JSON-RPC (1)
       ▼
HPDynamo.Mcp.Server.exe (.NET 10.0)
       │ Named Pipe \\.\pipe\hpdynamo-mcp-2026 (NDJSON) (1)
       ▼
HPDynamo.McpBridge.dll (Loaded inside Revit.exe)
       │
       ├─ ConcurrentQueue<McpBridgeWorkRequest> _pending
       │ ExternalEvent.Raise() (2)
       ▼
Revit Main UI Thread (Revit ExternalEvent Handler)
       │
       ├─ RevitTransactionScope (Begin TransactionGroup / AutoTx)
       ├─ Roslyn Script Execution (Compile Cache Hit/Miss) (3)
       │
       ├─ Dynamo Engine Evaluation (HomeWorkspaceModel.Run / Journal) (3)
       │
       ├─ Serialize Value / Node Outputs Extraction (4)
       └─ Finalize TransactionGroup (Commit / DryRun Rollback)
       │
       ▼ Response qua Named Pipe ──▶ MCP Server ──▶ Antigravity
```

### Phân tích chi phí 5 chặng (Phân định Giả thuyết vs Đo đạc Thực nghiệm):
1. **Độ trễ Transport (stdio + Named Pipe):**
   - Giả thuyết ước tính: `< 1.5ms` cho 1 round-trip nhờ Named Pipe cục bộ duplex NDJSON và connection pool duy trì thường trực (`RevitBridgeClient`). (Đây là *giả thuyết ước tính* dựa trên độ trễ loopback IPC, không phải số đo live benchmark).
   - *Kết luận:* Named pipe **không phải điểm nghẽn**.
2. **Thời gian chờ Revit API Thread (ExternalEvent Queueing):**
   - Phụ thuộc hoàn toàn vào chu kỳ Revit event loop (`ExternalEvent`).
   - Trạng thái idle: `15ms – 45ms` (*giả thuyết dựa trên event loop idle latency*, chưa đo trực tiếp trong live benchmark).
   - Trạng thái user đang tương tác UI, vẽ view, hoặc đang mở native modal dialog: `vô hạn` cho đến khi user thoát modal / nhả chuột.
   - *Kết luận:* Mỗi round-trip qua Revit thread tốn ước tính ít nhất 15-45ms chỉ để chờ dispatch. Nếu một workflow gọi 15 tools tuần tự, tổng thời gian chờ queue đã là `300ms – 700ms` dù chưa tính thời gian chạy.
3. **Khởi tạo Dynamo, Compile Roslyn và Evaluation:**
   - **Roslyn Script Compile:** Đo đạc thực nghiệm trên offline test runner: Lần đầu (cold): `120ms – 250ms`. Lần sau (warm hit cache): `< 0.2ms`.
   - **Dynamo Evaluation:**
     - Ephemeral CodeBlock / DesignScript nhỏ: `30ms – 80ms` (ước tính).
     - Graph trung bình (30–100 nodes): `150ms – 800ms` (ước tính).
     - Graph lớn (>200 nodes hoặc Revit element generation): `1.5s – 10s+` (ước tính).
   - **Vấn đề Automatic Mode:** Nếu workspace ở chế độ `Automatic`, mỗi lần add node hoặc nối dây đều kích hoạt 1 lần evaluation toàn phần! 10 thao tác = 10 lần evaluation liên tiếp, làm nghẽn hoàn toàn Revit main thread.
4. **Đọc output, serialize và kích thước response:**
   - Hiện tại thuật toán `ExtractTerminalOutputs` trong `run_dyn_file` duyệt `O(N * C)` với $N$ nodes và $C$ connectors (`canvasWorkspace.Connectors.Any(w => w.Start.Owner == node)` cho từng node).
   - `get_node_output_values` ép mọi data phức tạp (List, Array, Dictionary) thành `.ToString()`, trả về chuỗi vô nghĩa như `"System.Collections.Generic.List`1[...]"`.
   - `NdjsonPipeTransport` ngắt kết nối đột ngột nếu payload vượt `4MB` (`MaxMessageBytes`).
5. **Số lượt gọi MCP để hoàn thành một tác vụ:**
   - Để tạo một cụm 5 nodes và nối 4 dây: Hiện tại tốn **10 lượt gọi tool độc lập** (5 x `add_node_to_workspace`, 4 x `connect_node_ports`, 1 x `set_node_properties`).
   - Tổng thời gian: $10 \times (30ms\text{ queue} + 50ms\text{ eval}) \approx 800ms - 2000ms$ (ước tính tuần tự).

---

## 2. Bảng Findings Chi Tiết (Audit Findings Matrix)

| ID | Ưu tiên | Vị trí (File:Line) | Bằng chứng mã nguồn | Tác động | Trạng thái | Giải pháp đã áp dụng & Kiểm chứng |
|---|---|---|---|---|---|---|
| **AUD-DYN-01** | **P0 (Tính Đúng Đắn)** | `HPDynamo.McpBridge/Execution/RevitTransactionScope.cs:175-226`, `DynamoExecutionEngine.cs:66,85,122` | `if (mode == TransactionModes.Auto) txContext.StartAutoTransaction("...");` mở sẵn 1 Transaction của Revit. Khi Dynamo tạo Revit element, Dynamo's `TransactionManager.Instance.EnsureInTransaction(doc)` cố mở transaction thứ 2 gây `InvalidOperationException`. | Gây crash transaction khi Dynamo tạo Revit element dưới `transaction=auto`. | **ĐÃ GIẢI QUYẾT** | Bổ sung `startAutoTransaction: false` khi dispatch Dynamo script, chỉ bọc ngoài bằng `TransactionGroup`. Nhường quyền mở child transaction cho Dynamo `TransactionManager`. |
| **AUD-DYN-02** | **P0 (Tính Đúng Đắn)** | `HPDynamo.McpBridge/Execution/DynamoEvaluationSync.cs:12-73`, `CanvasGraphRunner.cs:35,78,128` | `ws.Run();` được gọi và ngay dòng tiếp theo đọc outputs. Dynamo đánh giá bất đồng bộ qua `EvaluationScheduler`. | Đọc output cũ từ lần chạy trước; TransactionGroup đóng trước khi Dynamo evaluation hoàn tất. | **ĐÃ GIẢI QUYẾT** | Tạo `DynamoEvaluationSync.RunAndWait` dùng `DispatcherFrame` và `EvaluationCompleted` event với cooperative timeout, bơm message pump mà không block Revit UI thread. |
| **AUD-DYN-03** | **P1 (Hiệu Năng/Lượt gọi)** | `HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/batch_edit_graph/` | Mỗi node add và mỗi wire connect là 1 tool MCP riêng. Mỗi thao tác kích hoạt cascade evaluation nếu đang ở `RunType.Automatic`. | Tốn 10–20 round-trips và kích hoạt hàng loạt evaluation dở dang gây giật lag Revit UI. | **ĐÃ GIẢI QUYẾT** | Thêm seed tool mới `batch_edit_graph`: tạo nodes, nối dây, gán thuộc tính với ID tạm ($n1, $n2); tự động pause sang `Manual` trong khi dựng và khôi phục ở `finally`. |
| **AUD-DYN-04** | **P1 (Tính Đúng Đắn)** | `SeedLibrary/Data/get_node_output_values/code.cs:35-75` | `return payload.ToString();` khi `payload` không phải Point hoặc Curve. | Mọi danh sách (`List<Element>`, `List<double>`), Dictionary, hay primitives đều bị ép thành string mất cấu trúc. | **ĐÃ GIẢI QUYẾT** | Triển khai đệ quy `SerializeOutputValue` hỗ trợ `IEnumerable`, `IDictionary`, primitives và geometry types thành JSON Array/Object thật. |
| **AUD-DYN-05** | **P1 (An Toàn Queue)** | `HPDynamo.McpBridge/Coordinator/DynamoBridgeCoordinator.cs:212-225,257-268` | `_pending.Enqueue(request)` không đăng ký `CancellationToken.Register`. Khi Revit modal/busy, request timeout trên client nhưng vẫn nằm trong queue và chạy trễ sau đó. | Thao tác trễ chạy ngầm sau khi client đã timeout, làm biến dạng model Revit ngoài tầm kiểm soát. | **ĐÃ GIẢI QUYẾT** | Kiểm tra `request.CancellationToken.IsCancellationRequested` trước khi chạy trong `Execute()`; đăng ký `request.CancellationToken.Register(() => request.Completion.TrySetCanceled())`. |
| **AUD-DYN-06** | **P1 (An Toàn Queue)** | `DynamoBridgeCoordinator.cs:193-206` | Client gửi cancel kèm id, nhưng hủy toàn bộ `_currentCancel` bất kể request nào đang chạy. | Hủy nhầm request mới nếu cancel của request cũ đến muộn. | **ĐÃ GIẢI QUYẾT** | Kiểm tra `IsCancellationRequested` trên từng work item; dọn dẹp đăng ký token cancellation khi request hoàn thành. |
| **AUD-DYN-07** | **P2 (Hiệu Năng/Thuật toán)** | `SeedLibrary/Graph/run_dyn_file/code.cs:54-68` | `!canvasWorkspace.Connectors.Any(wireItem => wireItem.Start.Owner == nodeItem)` lặp trong vòng lặp node ($O(N \times C)$). | Tốn $500 \times 1,000 = 500,000$ so sánh trên graph lớn. | **ĐÃ GIẢI QUYẾT** | Dựng trước `HashSet<NodeModel>` chứa tất cả start owners của connectors một lần duy nhất, giảm độ phức tạp xuống $O(N)$. |
| **AUD-DYN-08** | **P2 (Hiệu Năng/GC)** | `SeedLibrary/Library/search_nodes_library/code.cs:29-55` | Gọi `string.Join(" > ", searchEntry.Categories)` cho từng entry trong số 5,000+ entries trước khi kiểm tra match query. | Cấp phát 5,000+ chuỗi tạm trong GC heap cho mỗi lượt tìm kiếm node. | **ĐÃ GIẢI QUYẾT** | Trì hoãn định dạng `categoryHierarchy` và `string.Join` chỉ cho các matching entries thực sự trả về; lọc trực tiếp trên list mà không cấp phát chuỗi. |
| **AUD-DYN-09** | **P2 (Tương Thích TFM)** | `HPDynamo.McpBridge/HPDynamo.McpBridge.csproj:4` | Dự án target `net10.0-windows7.0`. Thực nghiệm biên dịch `net8.0` bị lỗi `CS1705` do các DLL Revit 2026 cài đặt trên dev workstation (`RevitAPIUI.dll 26.5.0.0`, `DynamoCore.dll 3.7.0.11777`) phụ thuộc vào `System.Runtime 10.0.0.0`. | Không thể build `net8.0` khi reference trực tiếp binary .NET 10 của bản cài đặt Revit 2026 này. | **ĐÃ LÀM RÕ** | Target bắt buộc là `net10.0-windows7.0` cho bridge vì bản cài Revit 2026 trên máy dev dùng `System.Runtime 10.0.0.0`; ghi nhận rõ ràng để phân biệt với Revit 2026 net8 RTM ban đầu. |
| **AUD-DYN-10** | **P2 (Mã Thừa)** | `DynamoExecutionEngine.cs:46-49`, `CanvasGraphRunner.cs:55-94` | Nhánh `request.Label.StartsWith("DynFile")` không được seed nào gọi do `run_dyn_file` tự có Roslyn script riêng. | Code runner dư thừa. | **ĐÃ GIẢI QUYẾT** | `CanvasGraphRunner.RunDynFile` được chuẩn hóa và tái sử dụng đồng bộ evaluation với `DynamoEvaluationSync`. |
| **AUD-DYN-11** | **P2 (Tính Đúng Đắn/Rollback)** | `batch_edit_graph/code.cs:208-214` | `dryRun=true` chỉ rollback TransactionGroup của Revit mà không dọn node trên canvas Dynamo. | Canvas Dynamo còn node mồ côi với binding trỏ vào element Revit đã bị hủy. | **ĐÃ GIẢI QUYẾT** | Trong `batch_edit_graph` và scripts, khi `dryRun=true`, thực hiện xóa toàn bộ các node vừa tạo bằng `DeleteModelCommand` và rollback transactions. |
| **AUD-DYN-12** | **P3 (Độ Tin Cậy Test)** | `HPDynamo.E2E.Tests/` | Bộ 201 test E2E chạy trên Fake bridge giả lập. | Cần bổ sung test biên dịch thực tế của seeds. | **ĐÃ GIẢI QUYẾT** | Mở rộng `SeedCompilationTests` và `SeedSchemaValidationTests` (99 tests) kiểm tra Roslyn compilation thật của 100% 13 seeds với API Revit/Dynamo. |

