# Handoff Report: Milestone A (R2-01, R2-02, R2-03)
**Agent**: explorer_a_1 (Teamwork Preview Explorer)  
**Parent**: f5a54e1e-e38f-430b-b7e8-2a44388a5da1 (orchestrator_8)  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_a_1\`  
**Date**: 2026-10-09  
**Type**: Hard (Task complete)  

---

## 1. Observation (Quan sát trực tiếp)

### Obs 1: Thuộc tính `value` của node trong `batch_edit_graph/code.cs`
- Đường dẫn: `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/batch_edit_graph/code.cs`
- Dòng 170:
  ```csharp
  string? origVal = (nodeModel is CodeBlockNodeModel cbn) ? cbn.Code : null;
  ```
- Dòng 171:
  ```csharp
  if (isExisting) modifiedProperties.Add((targetGuid, "value", origVal));
  ```
- Dòng 221–225 trong `RestoreProperty`:
  ```csharp
  else if (prop == "value")
  {
      if (node is CodeBlockNodeModel cbn && origVal != null) cbn.SetCodeContent(origVal, currentWorkspace.ElementResolver);
      else if (origVal != null) node.UpdateValue(new Dynamo.Graph.UpdateValueParams("Value", origVal));
  }
  ```
- Kết quả: Khi `nodeModel` là Number (`DoubleInput`), String (`StringInput`), Boolean (`BoolSelector`), Sliders... thì `origVal` luôn là `null`. Do đó điều kiện `origVal != null` tại dòng 224 luôn trả về `false`, không hề gọi `node.UpdateValue(...)` để phục hồi giá trị.

### Obs 2: Cơ chế ghi nhận và phục hồi `replacedWires`
- Đường dẫn: `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/batch_edit_graph/code.cs`
- Dòng 23:
  ```csharp
  var replacedWires = new List<(Guid Source, int SPort, Guid Target, int TPort)>();
  ```
- Dòng 128–131 trong `ConnectSingleWire`:
  ```csharp
  var oldWire = currentWorkspace.Connectors.FirstOrDefault(c =>
      c.End != null && c.End.Owner.GUID == targetGuid && c.End.Index == toPort);
  if (oldWire?.Start != null)
      replacedWires.Add((oldWire.Start.Owner.GUID, oldWire.Start.Index, targetGuid, toPort));
  ```
- Dòng 238–244 trong `PerformRollback`:
  ```csharp
  foreach (var rw in replacedWires)
  {
      dynamoModel?.ExecuteCommand(new DynamoModel.MakeConnectionCommand(
          rw.Source, rw.SPort, PortType.Output, DynamoModel.MakeConnectionCommand.Mode.Begin));
      dynamoModel?.ExecuteCommand(new DynamoModel.MakeConnectionCommand(
          rw.Target, rw.TPort, PortType.Input, DynamoModel.MakeConnectionCommand.Mode.End));
  }
  ```
- Kết quả: Khi thay thế liên tiếp các dây vào cùng một cổng input `toPort` (ví dụ A->T, rồi B->T, rồi C->T), danh sách `replacedWires` ghi nhận lần lượt cả các dây do batch vừa mới tạo (A và B). Khi rollback, vòng lặp `foreach` duyệt xuôi lần lượt áp lại A rồi B, đè mất kết nối gốc ban đầu và dừng lại ở trạng thái trung gian B->T.

### Obs 3: Mất liên kết dây khi sửa CodeBlock
- Đường dẫn: `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/batch_edit_graph/code.cs`
- Dòng 172–174:
  ```csharp
  if (nodeModel is CodeBlockNodeModel cbnModel)
      cbnModel.SetCodeContent(newValue, currentWorkspace.ElementResolver);
  ```
- Dòng 223:
  ```csharp
  if (node is CodeBlockNodeModel cbn && origVal != null) cbn.SetCodeContent(origVal, currentWorkspace.ElementResolver);
  ```
- Kết quả: Khi `cbnModel.SetCodeContent` chạy, Dynamo Core phân tích lại AST và xóa sạch các connectors kết nối vào InPorts/OutPorts của CodeBlock. Rollback chỉ gọi `SetCodeContent(origVal)` mà không có bất kỳ cấu trúc nào lưu trữ hay phục hồi các connectors vào/ra của CodeBlock, làm đứt toàn bộ dây nối.

### Obs 4: Ngân sách dòng mã và kiểm thử hiện hành
- Đường dẫn: `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/batch_edit_graph/code.cs`
  * Tổng số dòng hiện tại: **297 dòng**.
- Đường dẫn: `HPDynamo/HPDynamo.Mcp.Server.Tests/Seeds/BatchEditGraphRollbackTests.cs`
  * Dòng 80–89: Kiểm tra `ScriptQuality.LineCount(code) <= ScriptQuality.MaxScriptLines` (tối đa 300 dòng) và 0 cảnh báo/lỗi.
  * Dòng 42–45: Kiểm tra khớp chuỗi chính xác 4 biến snapshot:
    `var createdNodeGuids = new List<Guid>();`
    `var createdWiresBetweenExistingNodes = new List<Guid>();`
    `var replacedWires = new List<(Guid Source, int SPort, Guid Target, int TPort)>();`
    `var modifiedProperties = new List<(Guid NodeGuid, string Property, string? OriginalValue)>();`

---

## 2. Logic Chain (Chuỗi suy luận)

1. Từ **Obs 1**: Dynamo Core `NodeModel` không có thuộc tính `Value` trên lớp cha mà phân bổ ở các lớp con (CodeBlock có `Code`, các node Input có `Value`/`InputValue` kế thừa từ `BasicInteractive<T>`). Do mã chỉ kiểm tra `nodeModel is CodeBlockNodeModel`, toàn bộ các node kiểu khác đều nhận `origVal = null`, dẫn đến nhánh hoàn tác `RestoreProperty` không bao giờ được thực thi $\rightarrow$ **Kết luận R2-01**: Cần hàm đọc giá trị tổng quát qua reflection (`GetNodeValue`) hỗ trợ cả `Code` và `Value`/`InputValue`/`StringValue`, định dạng `CultureInfo.InvariantCulture`.
2. Từ **Obs 2**: Dynamo Input port chỉ chấp nhận 1 dây nối. Khi nối dây mới vào cổng đã có dây, dây cũ bị ngắt. Nếu một batch nối nhiều lần vào cùng 1 cổng (A->T, B->T, C->T), mỗi lần nối sau sẽ thấy dây của lần nối trước trên canvas. Vì mã ghi nhận mọi `oldWire` mà không lọc xem cổng đó đã từng bị sửa trong batch hay chưa, các dây trung gian bị đưa vào `replacedWires`. Thêm vào đó, việc duyệt xuôi `foreach` khiến dây trung gian cuối cùng ghi đè lên dây gốc $\rightarrow$ **Kết luận R2-02**: Cần duy trì tập hợp `touchedTargetPorts`. Chỉ ghi nhận `replacedWires` cho cổng mục tiêu trong lần chạm đầu tiên (first-seen). Đồng thời duyệt đảo chiều LIFO khi rollback.
3. Từ **Obs 3**: Trong Dynamo, việc biên dịch lại mã nguồn CodeBlock qua `SetCodeContent` thay đổi layout cổng và hủy bỏ toàn bộ connector liên quan. Khôi phục text code chỉ tái tạo lại chân cổng nhưng không tái tạo dây $\rightarrow$ **Kết luận R2-03**: Cần snapshot toàn bộ connectors gắn với CodeBlock (cả InPorts và OutPorts) vào danh sách `codeBlockSavedWires` trước khi gọi `SetCodeContent`. Trong `PerformRollback`, sau khi khôi phục code và tái tạo ports, lập tức chạy vòng lặp tái kết nối các dây nối này.
4. Từ **Obs 4**: Tệp `code.cs` đang ở ngưỡng 297/300 dòng. Mọi mã bổ sung cho R2-01, R2-02, R2-03 nếu không đi kèm thu gọn mã cũ sẽ làm vỡ giới hạn Rule C2 / ScriptQuality và gây trượt bài test $\rightarrow$ **Kết luận Kỹ thuật**: Cần tối ưu và nén gọn các dòng mã hiện hữu trong `code.cs` (rút ngắn các câu lệnh guard, inline các khối lệnh kiểm tra) để toàn bộ giải pháp mới giữ file trong ngưỡng 275–288 dòng.

---

## 3. Caveats (Điểm lưu ý & Ranh giới khảo sát)

- **Không can thiệp tiến trình Revit đang mở**: Quá trình khảo sát hoàn toàn đọc mã tĩnh (static analysis) và tài liệu, không gửi lệnh qua named pipe hoặc can thiệp PID 26264.
- **Ràng buộc Read-Only**: Đại lý explorer không thực hiện chỉnh sửa mã nguồn và không chạy lệnh build/test.
- **Cổng CodeBlock khi thay đổi syntax phức tạp**: Giả định rằng khi rollback về `origVal`, code cũ hợp lệ và sinh ra đúng số lượng/chỉ số cổng như lúc chưa sửa. Trường hợp code cũ nguyên bản đã bị lỗi cú pháp trước đó, Dynamo vẫn giữ ports rỗng hoặc báo warning nhưng không ngăn việc kết nối lại.

---

## 4. Conclusion (Kết luận & Đề xuất hành động cho Implementer)

Kế hoạch triển khai (fix strategy) cho Milestone A bao gồm các bước cụ thể:
1. **R2-01**: Bổ sung hàm `GetNodeValue(NodeModel node)` đọc phản chiếu `Value`/`InputValue`/`StringValue` và `cbn.Code`, áp dụng `IFormattable` với `CultureInfo.InvariantCulture`. Cập nhật `ApplyPropertyUpdate` và `RestoreProperty` để phục hồi 100% giá trị.
2. **R2-02**: Thêm `var touchedTargetPorts = new HashSet<(Guid Target, int TPort)>();`. Trong `ConnectSingleWire`, chỉ ghi nhận `replacedWires` nếu `touchedTargetPorts.Add(...)` trả về `true`. Trong `PerformRollback`, duyệt ngược LIFO.
3. **R2-03**: Thêm `var codeBlockSavedWires = new List<(Guid Src, int SPort, Guid Dst, int DPort)>();`. Trước khi gọi `SetCodeContent`, snapshot toàn bộ connectors nối với CodeBlock. Trong `PerformRollback`, tái kết nối toàn bộ `codeBlockSavedWires` sau khi thuộc tính được hoàn tác.
4. **Clean Code & Line Budget**: Tối ưu hóa các dòng guard/throw trong `code.cs` để duy trì tổng số dòng $\le 300$ dòng và mỗi hàm $\le 50$ dòng.
5. Chi tiết mã nguồn đề xuất trước/sau (before/after) đã được lập chi tiết trong `analysis.md`.

---

## 5. Verification Method (Phương pháp xác thực độc lập)

Khi implementer hoàn thành mã nguồn, tiến hành kiểm chứng qua các bước:
1. **Kiểm tra giới hạn dòng mã tĩnh**:
   - Mở tệp `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/batch_edit_graph/code.cs` và đếm tổng số dòng (phải $\le 300$).
   - Xác nhận không có hàm nào vượt quá 50 dòng.
2. **Chạy bộ kiểm thử tự động của Seed Registry**:
   - Lệnh: `dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests --filter FullyQualifiedName~BatchEditGraphRollbackTests`
   - Xác nhận tất cả các test case đều pass, đặc biệt là `BatchEditGraph_CompliesWithScriptQuality_ZeroErrorsAndWarnings` và `BatchEditGraph_RollbackOracle_VerifiesCompleteStateRestoration`.
3. **Bổ sung test cases mới cho R2-01, R2-02, R2-03 trong `BatchEditGraphRollbackTests`**:
   - Test R2-01: Cập nhật giá trị `Number`, `String`, `Boolean` $\rightarrow$ rollback $\rightarrow$ xác nhận giá trị phục hồi 100%.
   - Test R2-02: Thay thế 3 dây liên tiếp vào cùng 1 port (A->T, B->T, C->T) $\rightarrow$ rollback $\rightarrow$ xác nhận chỉ duy nhất dây ban đầu (Orig->T) được phục hồi, không dừng ở B->T.
   - Test R2-03: Sửa CodeBlock có sẵn 2 dây in và 1 dây out $\rightarrow$ rollback $\rightarrow$ xác nhận phục hồi cả code và toàn bộ 3 dây nối.
4. **Điều kiện vô hiệu hóa (Invalidation Conditions)**:
   - Nếu `code.cs` vượt quá 300 dòng $\rightarrow$ Test `ScriptQuality` thất bại.
   - Nếu đổi tên 4 biến snapshot (`createdNodeGuids`, `createdWiresBetweenExistingNodes`, `replacedWires`, `modifiedProperties`) $\rightarrow$ Test `BatchEditGraph_HasProgrammaticRollbackSnapshotStructures` thất bại.
