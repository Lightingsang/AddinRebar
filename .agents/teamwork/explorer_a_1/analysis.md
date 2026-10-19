# Technical Analysis: Milestone A (R2-01, R2-02, R2-03)
**Agent**: explorer_a_1 (Teamwork Preview Explorer)  
**Host Subsystem**: HPDynamo MCP / Revit 2026 / Dynamo 3.7  
**Date**: 2026-10-09  

---

## Executive Summary

Khảo sát hiện trạng kỹ thuật hệ thống HPDynamo MCP (đặc biệt là công cụ hạt nhân `batch_edit_graph` tại `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/batch_edit_graph/code.cs` cùng bộ kiểm thử liên quan) đã xác định chính xác nguyên nhân gốc rễ và cơ chế hỏng hóc cho cả 3 phát hiện thuộc Milestone A:
1. **R2-01**: Cập nhật giá trị node input (Number, String, Boolean...) đang gán cứng `origVal = (node is CodeBlockNodeModel cbn) ? cbn.Code : null;`. Đối với mọi node không phải CodeBlock, `origVal` luôn là `null`, dẫn đến nhánh phục hồi `else if (origVal != null) node.UpdateValue(...)` bị bỏ qua 100%, giá trị trên canvas không được phục hồi khi `dryRun = true` hoặc exception.
2. **R2-02**: Thay thế nhiều dây nối trên cùng một input port (A->T, B->T, C->T) trong cùng một batch ghi nhận tuần tự vào `replacedWires`. Khi rollback, vòng lặp `foreach` duyệt xuôi lần lượt kết nối lại A->T rồi B->T, làm canvas dừng lại ở trạng thái trung gian B->T (dây do batch tự tạo) thay vì trạng thái ban đầu duy nhất.
3. **R2-03**: Khi CodeBlock thay đổi nội dung code qua `cbnModel.SetCodeContent(...)`, Dynamo tự động phân tích lại AST và xóa bỏ toàn bộ connectors (cả InPorts và OutPorts) gắn với CodeBlock. Snapshot hiện hành chỉ lưu chuỗi code mà không lưu connectors, dẫn đến khi rollback `SetCodeContent` chỉ tái tạo port rỗng nhưng mất sạch 100% dây nối liên kết.

Đồng thời, báo cáo cung cấp giải pháp thiết kế chi tiết (fix strategy), mã đề xuất trước/sau (before/after), và chiến lược kiểm soát ngân sách dòng mã (line count budget $\le 300$ dòng theo Rule C2 / ScriptQuality).

---

## 1. Khảo Sát Hiện Trạng Kỹ Thuật Chi Tiết

### 1.1. R2-01: Snapshot & Rollback Node Input Values

#### Vị trí mã nguồn:
`HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/batch_edit_graph/code.cs`:
- Dòng 24: `var modifiedProperties = new List<(Guid NodeGuid, string Property, string? OriginalValue)>();`
- Dòng 167–176 (`ApplyPropertyUpdate`):
```csharp
167: string newValue = propArg.Str("value", "");
168: if (!string.IsNullOrWhiteSpace(newValue))
169: {
170:     string? origVal = (nodeModel is CodeBlockNodeModel cbn) ? cbn.Code : null;
171:     if (isExisting) modifiedProperties.Add((targetGuid, "value", origVal));
172:     if (nodeModel is CodeBlockNodeModel cbnModel)
173:         cbnModel.SetCodeContent(newValue, currentWorkspace.ElementResolver);
174:     else
175:         nodeModel.UpdateValue(new Dynamo.Graph.UpdateValueParams("Value", newValue));
176: }
```
- Dòng 221–225 (`RestoreProperty`):
```csharp
221: else if (prop == "value")
222: {
223:     if (node is CodeBlockNodeModel cbn && origVal != null) cbn.SetCodeContent(origVal, currentWorkspace.ElementResolver);
224:     else if (origVal != null) node.UpdateValue(new Dynamo.Graph.UpdateValueParams("Value", origVal));
225: }
```

#### Phân tích cơ chế & Nguyên nhân gốc rễ:
1. **Thiếu hỗ trợ đa hình/phản chiếu cho NodeModel**:
   - `CodeBlockNodeModel` có thuộc tính `Code` (kiểu `string`).
   - Các node input cơ bản của Dynamo Core (`CoreNodeModels.Input.DoubleInput`, `StringInput`, `BoolSelector`, `IntegerInput`, `DoubleSlider`, `IntegerSlider`, `Filename`, `Directory`) đều kế thừa từ `BasicInteractive<T>` hoặc mô hình input tương đương, sở hữu thuộc tính public instance `public T Value { get; set; }`.
   - Tuy nhiên tại dòng 170, mã nguồn chỉ kiểm tra `nodeModel is CodeBlockNodeModel cbn`, còn với mọi node khác thì trả về `null`.
2. **Nhánh phục hồi bị vô hiệu hóa hoàn toàn**:
   - Tại dòng 171, giá trị `(targetGuid, "value", null)` được đưa vào `modifiedProperties`.
   - Tại dòng 224, điều kiện `else if (origVal != null)` nhận giá trị `false`, dẫn tới `node.UpdateValue(...)` không bao giờ được gọi.
   - Kết quả: Khi `dryRun = true` hoặc xảy ra ngoại lệ, giá trị của Number, String, Boolean giữ nguyên giá trị mới, không phục hồi được về giá trị ban đầu.
3. **Nguy cơ bỏ sót giá trị rỗng/zero**:
   - Dòng 168 dùng `!string.IsNullOrWhiteSpace(newValue)`, khiến việc cập nhật giá trị chuỗi rỗng `""` hoặc khoảng trắng bị bỏ qua thay vì kiểm tra `propArg.Has("value")`.

---

### 1.2. R2-02: Thứ Tự Phục Hồi Dây Nối (Wire Replacement Ordering)

#### Vị trí mã nguồn:
`HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/batch_edit_graph/code.cs`:
- Dòng 23: `var replacedWires = new List<(Guid Source, int SPort, Guid Target, int TPort)>();`
- Dòng 128–136 (`ConnectSingleWire`):
```csharp
128: var oldWire = currentWorkspace.Connectors.FirstOrDefault(c =>
129:     c.End != null && c.End.Owner.GUID == targetGuid && c.End.Index == toPort);
130: if (oldWire?.Start != null)
131:     replacedWires.Add((oldWire.Start.Owner.GUID, oldWire.Start.Index, targetGuid, toPort));
132: 
133: dynamoModel!.ExecuteCommand(new DynamoModel.MakeConnectionCommand(
134:     sourceGuid, fromPort, PortType.Output, DynamoModel.MakeConnectionCommand.Mode.Begin));
135: dynamoModel.ExecuteCommand(new DynamoModel.MakeConnectionCommand(
136:     targetGuid, toPort, PortType.Input, DynamoModel.MakeConnectionCommand.Mode.End));
```
- Dòng 238–244 (`PerformRollback`):
```csharp
238: foreach (var rw in replacedWires)
239: {
240:     dynamoModel?.ExecuteCommand(new DynamoModel.MakeConnectionCommand(
241:         rw.Source, rw.SPort, PortType.Output, DynamoModel.MakeConnectionCommand.Mode.Begin));
242:     dynamoModel?.ExecuteCommand(new DynamoModel.MakeConnectionCommand(
243:         rw.Target, rw.TPort, PortType.Input, DynamoModel.MakeConnectionCommand.Mode.End));
244: }
```

#### Phân tích cơ chế & Nguyên nhân gốc rễ:
1. **Ô nhiễm Snapshot trung gian (Snapshot Contamination)**:
   - Giả sử ban đầu trên canvas có dây $W_0$: `Orig[0] -> Target[0]`.
   - Batch thực hiện 3 thao tác nối tuần tự vào cùng cổng `Target[0]`:
     * Thao tác 1: `A[0] -> Target[0]`. Lúc này `oldWire` là $W_0$. `replacedWires` ghi nhận `(Orig, 0, Target, 0)`. Dynamo ngắt $W_0$, nối `A[0] -> Target[0]`.
     * Thao tác 2: `B[0] -> Target[0]`. Lúc này `oldWire` là `A[0] -> Target[0]` (vốn là dây do batch tự tạo ở bước 1). `replacedWires` ghi nhận `(A, 0, Target, 0)`.
     * Thao tác 3: `C[0] -> Target[0]`. Lúc này `oldWire` là `B[0] -> Target[0]`. `replacedWires` ghi nhận `(B, 0, Target, 0)`.
   - Kết quả danh sách `replacedWires`: `[(Orig, 0, Target, 0), (A, 0, Target, 0), (B, 0, Target, 0)]`.
2. **Duyệt xuôi (Forward Loop) áp đè trạng thái trung gian**:
   - Khi `PerformRollback()` chạy, vòng lặp `foreach (var rw in replacedWires)` duyệt xuôi từ 0 đến 2:
     * Lần lặp 1: Nối `Orig[0] -> Target[0]`.
     * Lần lặp 2: Nối `A[0] -> Target[0]` (ghi đè và ngắt dây `Orig`).
     * Lần lặp 3: Nối `B[0] -> Target[0]` (ghi đè và ngắt dây `A`).
   - Kết thúc rollback: Cổng `Target[0]` bị nối với `B[0]`, là trạng thái trung gian do batch tạo ra. Dây nguyên bản $W_0$ bị mất hoàn toàn!
   - Nếu node B là node mới tạo trong batch (`createdNodeGuids`), bước tiếp theo sẽ xóa node B, khiến cổng `Target[0]` bị mồ côi (không còn dây nào).
3. **Thiếu bộ lọc cổng mục tiêu lần đầu (First-seen per target port)**:
   - Cổng Input trong Dynamo chỉ cho phép tối đa 1 kết nối. Việc ghi nhận đè các dây trung gian vào `replacedWires` là sai lệch bản chất snapshot trạng thái trước giao dịch.

---

### 1.3. R2-03: Bảo Tồn Dây Nối Khi Sửa CodeBlock

#### Vị trí mã nguồn:
`HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/batch_edit_graph/code.cs`:
- Dòng 170–173:
```csharp
string? origVal = (nodeModel is CodeBlockNodeModel cbn) ? cbn.Code : null;
if (isExisting) modifiedProperties.Add((targetGuid, "value", origVal));
if (nodeModel is CodeBlockNodeModel cbnModel)
    cbnModel.SetCodeContent(newValue, currentWorkspace.ElementResolver);
```
- Dòng 223:
```csharp
if (node is CodeBlockNodeModel cbn && origVal != null) cbn.SetCodeContent(origVal, currentWorkspace.ElementResolver);
```

#### Phân tích cơ chế & Nguyên nhân gốc rễ:
1. **Dynamo tái cấu trúc Port và tiêu hủy Connectors khi đổi Code**:
   - Khi `cbnModel.SetCodeContent(newValue, ...)` được gọi, Dynamo Core biên dịch lại mã DesignScript.
   - Danh sách InPorts (biến đầu vào) và OutPorts (biểu thức đầu ra) được tái tạo từ cú pháp mới.
   - Dynamo tự động xóa bỏ toàn bộ `ConnectorModel` đang gắn vào các cổng bị thay đổi hoặc bị đánh số lại.
2. **Snapshot hiện tại chỉ lưu chuỗi mã nguồn rỗng**:
   - `modifiedProperties` chỉ lưu chuỗi code cũ `cbn.Code`.
   - Hoàn toàn KHÔNG lưu danh sách dây nối đầu vào (InPorts) hay đầu ra (OutPorts) gắn với node CodeBlock.
3. **Rollback không tái tạo dây nối**:
   - Khi rollback gọi `cbn.SetCodeContent(origVal, ...)`, Dynamo phục hồi lại cấu trúc cổng theo code cũ.
   - Nhưng các dây nối đã bị xóa từ trước không tự động mọc lại.
   - Kết quả: CodeBlock phục hồi được nội dung text nhưng toàn bộ dây nối vào/ra bị đứt hoàn toàn, làm đồ thị bị gãy mạch.

---

## 2. Đề Xuất Giải Pháp Thiết Kế Chi Tiết (Fix Strategy)

### 2.1. Giải pháp cho R2-01: Phản chiếu Đa hình Giá trị Node Input

Xây dựng hàm trợ giúp gọn nhẹ `GetNodeValue(NodeModel node)` sử dụng reflection an toàn và `CultureInfo.InvariantCulture`:
```csharp
string? GetNodeValue(NodeModel node)
{
    if (node is CodeBlockNodeModel cbn) return cbn.Code;
    var prop = node.GetType().GetProperty("Value")
            ?? node.GetType().GetProperty("InputValue")
            ?? node.GetType().GetProperty("StringValue");
    if (prop != null)
    {
        var val = prop.GetValue(node);
        if (val == null) return null;
        if (val is IFormattable formattable)
            return formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture);
        return val.ToString();
    }
    return null;
}
```

Trong `ApplyPropertyUpdate`:
```csharp
if (propArg.Has("value"))
{
    string newValue = propArg.Str("value", "");
    string? origVal = GetNodeValue(nodeModel);
    if (isExisting) modifiedProperties.Add((targetGuid, "value", origVal));
    if (nodeModel is CodeBlockNodeModel cbnModel)
        cbnModel.SetCodeContent(newValue, currentWorkspace.ElementResolver);
    else
        nodeModel.UpdateValue(new Dynamo.Graph.UpdateValueParams("Value", newValue));
}
```

Trong `RestoreProperty`:
```csharp
else if (prop == "value")
{
    if (node is CodeBlockNodeModel cbn)
    {
        if (origVal != null) cbn.SetCodeContent(origVal, currentWorkspace.ElementResolver);
    }
    else if (origVal != null)
    {
        node.UpdateValue(new Dynamo.Graph.UpdateValueParams("Value", origVal));
    }
}
```

*Lợi ích*:
- Phục hồi 100% cho `DoubleInput`, `IntegerInput`, `StringInput`, `BoolSelector`, `Slider`, `Filename` và `CodeBlock`.
- Đảm bảo định dạng số dấu chấm (`12.34`) nhất quán theo chuẩn InvariantCulture, không bị lỗi dấu phẩy trên Windows tiếng Việt/Pháp.

---

### 2.2. Giải pháp cho R2-02: Bộ lọc Cổng Mục Tiêu Ban Đầu (First-Seen Snapshot) & Đảo Chiều LIFO

Duy trì tập hợp theo dõi cổng mục tiêu đã can thiệp trong batch:
```csharp
var touchedTargetPorts = new HashSet<(Guid Target, int TPort)>();
```

Trong `ConnectSingleWire`:
```csharp
bool isFirstTouch = touchedTargetPorts.Add((targetGuid, toPort));
var oldWire = currentWorkspace.Connectors.FirstOrDefault(c =>
    c.End != null && c.End.Owner.GUID == targetGuid && c.End.Index == toPort);

if (isFirstTouch && oldWire?.Start != null)
{
    replacedWires.Add((oldWire.Start.Owner.GUID, oldWire.Start.Index, targetGuid, toPort));
}
```

Trong `PerformRollback`:
Thực hiện duyệt ngược (LIFO) qua `replacedWires`:
```csharp
for (int i = replacedWires.Count - 1; i >= 0; i--)
{
    var rw = replacedWires[i];
    dynamoModel?.ExecuteCommand(new DynamoModel.MakeConnectionCommand(
        rw.Source, rw.SPort, PortType.Output, DynamoModel.MakeConnectionCommand.Mode.Begin));
    dynamoModel?.ExecuteCommand(new DynamoModel.MakeConnectionCommand(
        rw.Target, rw.TPort, PortType.Input, DynamoModel.MakeConnectionCommand.Mode.End));
}
```

*Cơ chế vận hành*:
1. Với thao tác đầu tiên trên cổng `Target[0]`: `isFirstTouch = true`, ghi nhận dây gốc ban đầu vào `replacedWires`.
2. Với các thao tác thay thế tiếp theo trên cùng cổng đó (A->T, rồi B->T, rồi C->T): `isFirstTouch = false`, không ghi nhận thêm dây trung gian nào.
3. Khi rollback: Chỉ phục hồi duy nhất dây gốc ban đầu. Không có bất kỳ dây trung gian nào bị kết nối lại.

---

### 2.3. Giải pháp cho R2-03: Snapshot & Tái Tạo Toàn Bộ Dây Nối CodeBlock

Khai báo cấu trúc snapshot dây nối CodeBlock:
```csharp
var codeBlockSavedWires = new List<(Guid Src, int SPort, Guid Dst, int DPort)>();
```

Trong `ApplyPropertyUpdate` (khi node là `CodeBlockNodeModel` hiện hữu):
Trước khi gọi `cbnModel.SetCodeContent(...)`:
```csharp
if (nodeModel is CodeBlockNodeModel cbnModel)
{
    if (isExisting)
    {
        foreach (var conn in currentWorkspace.Connectors)
        {
            if (conn.Start?.Owner.GUID == targetGuid || conn.End?.Owner.GUID == targetGuid)
            {
                codeBlockSavedWires.Add((conn.Start!.Owner.GUID, conn.Start.Index, conn.End!.Owner.GUID, conn.End.Index));
            }
        }
    }
    cbnModel.SetCodeContent(newValue, currentWorkspace.ElementResolver);
}
```

Trong `PerformRollback`:
Thực hiện phục hồi dây nối CodeBlock ngay sau khi thuộc tính được hoàn tác (sau khi `SetCodeContent(origVal)` tái tạo lại layout cổng):
```csharp
// Hoàn tác thuộc tính (CodeBlock được phục hồi layout ports)
for (int i = modifiedProperties.Count - 1; i >= 0; i--)
{
    var (guid, prop, origVal) = modifiedProperties[i];
    RestoreProperty(guid, prop, origVal);
}

// Tái tạo lại toàn bộ dây nối In/Out của CodeBlock đã phục hồi
for (int i = codeBlockSavedWires.Count - 1; i >= 0; i--)
{
    var cw = codeBlockSavedWires[i];
    dynamoModel?.ExecuteCommand(new DynamoModel.MakeConnectionCommand(
        cw.Src, cw.SPort, PortType.Output, DynamoModel.MakeConnectionCommand.Mode.Begin));
    dynamoModel?.ExecuteCommand(new DynamoModel.MakeConnectionCommand(
        cw.Dst, cw.DPort, PortType.Input, DynamoModel.MakeConnectionCommand.Mode.End));
}
```

*Lợi ích*:
- Bảo toàn 100% các kết nối đầu vào (InPorts) và đầu ra (OutPorts) của CodeBlock.
- Trật tự rollback chuẩn xác: Khôi phục mã nguồn CodeBlock $\rightarrow$ Dynamo sinh lại ports $\rightarrow$ Tái kết nối dây $\rightarrow$ Xóa node mới tạo.

---

## 3. Ràng Buộc Kỹ Thuật & Ngân Sách Dòng Mã (Clean Code Governance)

### 3.1. Giới hạn 300 dòng mã (Rule C2 / ScriptQuality.MaxScriptLines)
- Tệp `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/batch_edit_graph/code.cs` hiện có **297 dòng**.
- Giới hạn tối đa là **300 dòng**.
- Để bổ sung logic R2-01, R2-02, R2-03 mà không vi phạm Rule C2, implementer cần tối ưu hóa gọn gàng các đoạn mã hiện có:
  - Rút gọn các dòng throw ngoại lệ guard validation (viết inline 1 dòng thay vì tách 2 dòng).
  - Gộp các dòng khởi tạo và gán tọa độ X/Y.
  - Sau khi tối ưu, tổng số dòng dự kiến sẽ nằm trong khoảng **275 – 288 dòng** (dưới 300 dòng tuyệt đối).

### 3.2. Giới hạn 50 dòng cho phương thức (Rule M3 / ScriptQuality.MaxBlockLines)
- `ConnectSingleWire`: Hiện đang 46 dòng. Cần rút gọn các khối kiểm tra guard ranh giới port để giữ dưới 40 dòng.
- `ApplyPropertyUpdate`: Hiện đang 42 dòng. Bổ sung `GetNodeValue` bên ngoài giúp hàm giữ nguyên dưới 45 dòng.
- `PerformRollback`: Hiện đang 22 dòng. Thêm vòng lặp khôi phục CodeBlock dây nối tăng lên ~28 dòng (an toàn dưới 50 dòng).

### 3.3. Tương thích với `BatchEditGraphRollbackTests.cs`
Bộ test kiểm tra cấu trúc qua chuỗi ký tự cứng:
- `Assert.Contains("var createdNodeGuids = new List<Guid>();", code);`
- `Assert.Contains("var createdWiresBetweenExistingNodes = new List<Guid>();", code);`
- `Assert.Contains("var replacedWires = new List<(Guid Source, int SPort, Guid Target, int TPort)>();", code);`
- `Assert.Contains("var modifiedProperties = new List<(Guid NodeGuid, string Property, string? OriginalValue)>();", code);`
- `Assert.Contains("if (prop == \"name\")", code);`
- `Assert.Contains("else if (prop == \"isFrozen\"", code);`
- `Assert.Contains("else if (prop == \"value\")", code);`
- `Assert.Contains("else if ((prop == \"x\" || prop == \"y\")", code);`
- `Assert.Contains("node.ReportPosition();", code);`
Tất cả các định danh trên phải được bảo toàn nguyên vẹn trong bản sửa đổi.
