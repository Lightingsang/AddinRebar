# Báo Cáo Khảo Sát & Thiết Kế Kiến Trúc: R2 (P1 — Phục Hồi & Idempotency Qua Các Phiên)

**Tác giả**: `explorer_survey_pipeline_2` (Teamwork Preview Explorer)  
**Ngày thực hiện**: 2026-10-07  
**Phạm vi khảo sát**: Kho mã nguồn `HPBim` (`HPBim.Core`, `HPBim.Client`, `HPBim.Cli`), `McpShared`, và hạ tầng `HPRebar.McpBridge` / Revit API 2026.  
**Tập trung nhiệm vụ**: R2 (P1) trong `ORIGINAL_REQUEST.md` (## 2026-10-07T13:49:28Z).

---

## 1. Tóm Tắt Điều Hành (Executive Summary)

Khảo sát mã nguồn hiện hữu cho thấy hạ tầng dựng hình và soát lỗi của `HPBim` đã bước đầu thiết lập các thành phần nền tảng:
- `FailFastGate` và `BimCoordinationLedger` (theo dõi vòng đời 7 bước và dung sai kỹ thuật nghiêm ngặt).
- `RevitPipeClient` giao tiếp qua Named Pipe (`\\.\pipe\hprebar-mcp-r2026`) với `HPRebar.McpBridge`.
- Các lệnh dựng hình (`GenerateFoundationCommand`, `GenerateGradeBeamsCommand`) và lệnh kiểm toán (`AuditFoundationCommand`, `AuditColumnsCommand`, `RevitFoundationQueryService`).

Tuy nhiên, đối chiếu với yêu cầu P1 (Phục hồi & Idempotency qua các phiên), hệ thống hiện tại bộc lộ **5 lỗ hổng kiến trúc cốt tử**:
1. **Thiếu định danh tài liệu & kiểm tra độ tươi (Freshness Gate)**: Client gửi lệnh Roslyn script trực tiếp mà không xác thực `Document.Title`, `PathName` hay `DocumentVersion.VersionGUID`. Nếu người dùng đổi dự án hoặc đang mở file khác, lệnh sẽ can thiệp sai mô hình.
2. **Thiếu Composite Source Key & Nguy cơ nhân bản phần tử (Duplication Risk)**: Cấu kiện được định danh bằng chuỗi tự do (như `"DK"`, `"DC-1_001"`), không có định dạng chuẩn `[DocIdentifier]#[Category]:[PrimaryHandle]` và không lưu `ProvenanceHandles`. Việc chạy lại cùng một lệnh sẽ tạo trùng lặp 100% đối tượng trong Revit.
3. **Thiếu Intent Journal & Biên nhận Extensible Storage**: Không có nhật ký ghi ý định cục bộ trước khi truyền pipe. Revit không ghi nhận `OperationId` hay bảng ánh xạ `SourceKey -> UniqueId` vào phần tử ẩn `DataStorage` trong cùng transaction dựng hình.
4. **Hòa lẫn Timeout & Mất kiểm soát trạng thái (OutcomeUnknown)**: Timeout truyền thông của pipe client (ví dụ 60s/120s) bị chập làm một với thời gian Revit chờ thread và thời gian chạy script. Khi pipe bị ngắt do timeout, client không thể biết Revit đã commit hay rollback, dẫn tới rủi ro retry mù quáng.
5. **Kiểm toán phụ thuộc số lượng phần tử (Count Flaw)**: `RevitModelVerifier` và một số bước trong `FoundationQcEngine` dựa trên `GetElementCount()` hoặc so sánh số lượng đơn thuần. Ngay cả khi 100% số lượng cấu kiện khớp, mô hình vẫn có thể sai lệch tọa độ, cao độ hoặc tiết diện mà vẫn báo PASS.

Báo cáo này phân tích chi tiết cơ chế hiện hữu và đề xuất giải pháp kiến trúc toàn diện cho cả 5 vấn đề trên.

---

## 2. Phân Tích & Giải Pháp Kiến Trúc 5 Trọng Tâm

### Trọng Tâm 1: Định Danh Tài Liệu & Kế Hoạch (Document & Plan Identity)

#### 1.1. Hiện trạng Mã Nguồn (As-Is State)
- **Trong `RevitContextReader.cs`** (`HPRebar/HPRebar.McpBridge/Service/RevitContextReader.cs:28-32`):
  Context hiện tại chỉ đọc:
  ```csharp
  result.DocTitle = doc.Title;
  result.DocPath = string.IsNullOrEmpty(doc.PathName) ? null : doc.PathName;
  result.IsFamily = doc.IsFamilyDocument;
  result.IsReadOnly = doc.IsReadOnly;
  result.IsModifiable = doc.IsModifiable;
  result.Units = new UnitsInfo(LengthUnitLabel(doc));
  ```
- **Trong `ScriptRunner.cs`** (`HPRebar/HPRebar.McpBridge/Service/ScriptRunner.cs:34-45`):
  Khi thực thi lệnh `revit.execute`, bridge chỉ kiểm tra:
  ```csharp
  var uidoc = uiapp.ActiveUIDocument;
  var doc = uidoc?.Document;
  if (doc is null || uidoc is null) return ExecuteResult.Failure("No document is open in Revit...");
  if (mode != TransactionModes.None) {
      if (doc.IsReadOnly) return ExecuteResult.Failure("The active document is read-only...");
      if (doc.IsModifiable) return ExecuteResult.Failure("Revit already has a transaction open...");
  }
  ```
- **Lỗ hổng**: Không có cơ chế ràng buộc giữa `BimExecutionPlan` và `Document`. Nếu client lập kế hoạch cho file `KC-HT-90-TLTT-16.rvt`, nhưng người dùng đang active file `Project1.rvt`, script vẫn âm thầm chạy và chèn cấu kiện vào `Project1.rvt`. Ngoài ra, nếu tài liệu bị chỉnh sửa thủ công trên Revit giữa thời điểm lập plan và thời điểm commit, client không phát hiện được sự thay đổi này.

#### 1.2. Cơ chế Revit API cho Revision/Hash & Document Identity
Trong Revit API chính thức:
1. **`Document.GetDocumentVersion(doc)`**:
   Phương thức tĩnh `public static DocumentVersion GetDocumentVersion(Document doc)` trả về đối tượng `DocumentVersion`:
   - `VersionGUID` (`Guid`): Mã định danh duy nhất của phiên bản tài liệu hiện tại, tự động thay đổi sau mỗi lần chỉnh sửa / save.
   - `NumberOfSaves` (`int`): Tổng số lần tài liệu được ghi lưu vào đĩa.
2. **`Document.GetWorksharingCentralGUID()`**:
   Với dự án Workshared (BIM collaboration), `doc.GetWorksharingCentralGUID()` trả về GUID duy nhất của Central Model, bất biến trên mọi máy trạm.
3. **`Document.Title` & `Document.PathName`**:
   Tên file và đường dẫn tuyệt đối trên ổ đĩa.
4. **`ExportUtils.GetGBXMLDocumentId(doc)`** hoặc `doc.ProjectInformation.UniqueId`:
   Định danh tĩnh của Project Information Element.

#### 1.3. Thiết kế Kiến Trúc Đề Xuất
1. **DTO Định Danh Tài Liệu (`DocumentIdentitySnapshot`)**:
   ```csharp
   public sealed record DocumentIdentitySnapshot
   {
       public string Title { get; init; } = string.Empty;
       public string? PathName { get; init; }
       public Guid VersionGuid { get; init; }
       public int NumberOfSaves { get; init; }
       public Guid CentralGuid { get; init; }
       public string ActiveViewName { get; init; } = string.Empty;
       public long ActiveViewId { get; init; }
       public DateTime SnapshotAtUtc { get; init; } = DateTime.UtcNow;
   }
   ```
2. **Plan Hash trong `BimExecutionPlan`**:
   - `PlanId`: Định danh kế hoạch (UUID).
   - `PlanHash`: Mã SHA-256 tính toán chuẩn hóa trên JSON kinh điển (Canonical JSON) chứa danh sách các thao tác cấu kiện, tham số hình học và hệ tọa độ.
   - `TargetDoc`: Chứa `DocumentIdentitySnapshot` làm cơ sở đối soát.
3. **On-Thread Freshness Pre-Flight Gate (Chạy trực tiếp trên Revit API Thread)**:
   Trước khi mở bất kỳ Transaction hay TransactionGroup nào trong script thực thi, script phải thực thi khối kiểm tra tiền trạm:
   ```csharp
   // Pre-flight check on Revit thread
   if (!string.Equals(doc.Title, targetDocTitle, StringComparison.OrdinalIgnoreCase))
   {
       return ExecuteResult.Failure($"PREFLIGHT_FAIL: Document Title mismatch. Expected '{targetDocTitle}', but active document is '{doc.Title}'.");
   }
   if (!string.IsNullOrEmpty(targetDocPath) && !string.Equals(doc.PathName, targetDocPath, StringComparison.OrdinalIgnoreCase))
   {
       return ExecuteResult.Failure($"PREFLIGHT_FAIL: Document Path mismatch. Expected '{targetDocPath}', but active is '{doc.PathName}'.");
   }
   var currentVersion = Document.GetDocumentVersion(doc);
   if (requireExactVersion && currentVersion.VersionGUID != expectedVersionGuid)
   {
       return ExecuteResult.Failure($"PREFLIGHT_FAIL: Document is stale. Expected VersionGUID '{expectedVersionGuid}', but active is '{currentVersion.VersionGUID}'. Model was modified concurrently.");
   }
   ```
   Nếu kiểm tra không đạt, script ném lỗi và kết thúc ngay lập tức mà không sửa đổi bất kỳ byte nào trong mô hình.

---

### Trọng Tâm 2: Khóa Nguồn Hợp Thành & Ánh Xạ Xuất Xứ (Composite Source Key & Provenance Mapping)

#### 2.1. Hiện trạng Mã Nguồn (As-Is State)
- **Trong `CoordinationElementRecord.cs`** (`HPBim/src/HPBim.Core/Coordination/CoordinationElementRecord.cs:11-18`):
  ```csharp
  public string Id { get; init; } = string.Empty;
  public CoordinationCategory Category { get; init; }
  public string Code { get; init; } = string.Empty;
  public CadSourceSpec CadSourceSpec { get; init; } = new();
  public RevitTargetSpec? RevitTargetSpec { get; init; }
  ```
- **Trong `CadSourceSpec.cs`** (`HPBim/src/HPBim.Core/Coordination/CadSourceSpec.cs:11-13`):
  Chỉ có 1 handle duy nhất: `public string Handle { get; init; } = string.Empty;`.
- **Trong `GenerateGradeBeamsCommand.cs`** (`HPBim/src/HPBim.Cli/Commands/GenerateGradeBeamsCommand.cs:203-233`):
  Các dầm được tạo mới bằng `doc.Create.NewFamilyInstance(line, sym, levelMong, ...)` mà không kiểm tra dầm đã tồn tại hay chưa. Tham số gán chỉ là `ALL_MODEL_MARK` = `name`.
  Nếu người dùng chạy lệnh 2 lần, Revit sẽ sinh thêm 205 dầm trùng đè lên 205 dầm cũ!

#### 2.2. Đặc Tả Khóa Nguồn Hợp Thành (Composite Source Key Specification)
Trong quy trình BIM từ bản vẽ CAD:
- Một bản vẽ kết cấu DWG có thể có nhiều sheet (`KC01_MB_MONG`, `KC02_MC_MONG`).
- Một Handle của AutoCAD (ví dụ `2A1B`) là duy nhất trong file DWG đó nhưng có thể trùng giữa các file khác nhau nếu không kèm `DocIdentifier`.
- Một cấu kiện BIM (như Đài Móng `DC-1` hay Giằng Móng `GM-1`) thường được hình thành từ việc tổng hợp nhiều thực thể CAD:
  1. Đường bao hình học (Polyline Handle: `2A1B`).
  2. Text tên cấu kiện (Text Handle: `2A1C`).
  3. Dim kích thước / Section Tag (Handles: `2A1D`, `2A1E`).

**Đặc tả định dạng Composite Source Key**:
$$\text{SourceKey} = \texttt{[DocIdentifier]\#[Category]:[PrimaryHandle]}$$
- `DocIdentifier`: Mã sheet hoặc mã hash tài liệu nguồn CAD (ví dụ `KC01_MB_MONG`).
- `Category`: Phân loại cấu kiện chuẩn (`Footing`, `GradeBeam`, `Pile`, `Column`, `Wall`, `Slab`).
- `PrimaryHandle`: Handle thực thể hình học chủ đạo trong AutoCAD (Hex string).
- **Ví dụ cụ thể**:
  - `KC01_MB_MONG#Footing:2A1B`
  - `KC01_MB_MONG#GradeBeam:3B2A`
  - `KC01_MB_MONG#Pile:1F08`
  - `KC02_COT#Column:4C12`

**Danh sách ProvenanceHandles**:
`ProvenanceHandles = ["2A1B", "2A1C", "2A1D"]`
Chứa tất cả các handle phụ trợ đã tham gia vào quá trình trích xuất và đối soát 6 nguồn của kỹ sư CAD.

#### 2.3. Bảng Ánh Xạ `SourceKey -> Revit UniqueId` & Cơ Chế Idempotency
- **Revit ElementId vs UniqueId**:
  - `ElementId` (long): Có thể thay đổi khi copy/paste, đồng bộ worksharing hoặc upgrade phiên bản Revit.
  - `UniqueId` (string dạng `GUID-HexElementId`): Bất biến và duy nhất vĩnh viễn trên toàn bộ vòng đời của Element trong document.
- **Cấu trúc lưu trữ trong `BimCoordinationLedger` (`state/bim_coordination_ledger.json`)**:
  ```json
  {
    "sourceKey": "KC01_MB_MONG#GradeBeam:3B2A",
    "provenanceHandles": ["3B2A", "3B2B"],
    "category": "GradeBeam",
    "code": "DK-1",
    "step": "Step3_ModelRevit",
    "technicalGroup": "Group1_ReadyToModel",
    "revitTargetSpec": {
      "elementId": 108420,
      "uniqueId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890-0001a784",
      "familyName": "Concrete-Rectangular Beam",
      "typeName": "B_200x400",
      "levelName": "Tầng Móng",
      "modeledAt": "2026-10-07T14:30:00Z"
    }
  }
  ```
- **Thuật toán Idempotent Execution**:
  Khi thực thi kế hoạch `BimExecutionPlan`:
  1. Với mỗi cấu kiện có `SourceKey`:
  2. Tra cứu `SourceKey` trong `CoordinationLedger`:
     - **Trường hợp 1: Đã có `UniqueId`**:
       Truy vấn Revit: `var el = doc.GetElement(uniqueId);`
       - Nếu `el` tồn tại và hình học/tham số khớp với Plan trong sai số `CoordinationTolerances` $\rightarrow$ Đánh dấu trạng thái **`NoOp` (Skipped)**, không tạo mới.
       - Nếu `el` tồn tại nhưng tham số (b, h, level) có thay đổi trong bản vẽ cập nhật $\rightarrow$ Cập nhật tham số của `el`, đánh dấu **`Updated`**.
       - Nếu `el == null` (bị người dùng xóa thủ công trong Revit) $\rightarrow$ Tạo mới lại phần tử, cập nhật lại `UniqueId` mới vào Ledger, đánh dấu **`Recreated`**.
     - **Trường hợp 2: Chưa có `UniqueId` (Cấu kiện mới)**:
       Tạo mới phần tử trong Revit $\rightarrow$ Lấy `instance.UniqueId` $\rightarrow$ Ghi biên nhận vào Revit Extensible Storage $\rightarrow$ Cập nhật Ledger $\rightarrow$ Đánh dấu **`Created`**.
  3. **Kết quả**: Chạy plan $N$ lần liên tiếp cho kết quả đồng nhất 100%, số lượng phần tử tạo thêm sau lần 1 luôn bằng 0.

---

### Trọng Tâm 3: Nhật Ký Ý Định & Biên Nhận Extensible Storage (Intent Journal & Extensible Storage Receipt)

#### 3.1. Hiện trạng Mã Nguồn (As-Is State)
- Trước khi truyền pipe, `RevitPipeClient.cs` (`HPBim/src/HPBim.Client/Connection/RevitPipeClient.cs:95-102`) gửi trực tiếp payload JSON-RPC qua stream mạng mà không có bước ghi log ý định cục bộ bền vững (persistent intent log).
- Trong `HPRebar`, cơ chế Extensible Storage đã được triển khai rất thành công tại `KataRebarStorage.cs` (`HPRebar/HPRebar/KataRebar/Service/KataRebarStorage.cs:12-53`) với `SchemaBuilder`, GUID tĩnh `6F2C8B4E-1A7D-4C3B-9E5F-2D8A0B6C4E71`, lưu `HostUniqueId`, `KataNumber`, `Beam` trên từng thanh Rebar.
- Tuy nhiên, trong phân hệ `HPBim`, chưa có cơ chế Extensible Storage nào được định nghĩa để lưu trữ biên nhận cấp Document cho các mẻ dựng cấu kiện (Batch Operations).

#### 3.2. Cơ Chế Nhật Ký Ý Định Cục Bộ (Local Intent Journal)
Trước khi bất kỳ byte nào được đẩy vào Named Pipe, Client phải ghi một bản ghi ý định vào tệp nhật ký `state/bim_intent_journal.json` (sử dụng FileStream flush tức thì):
```csharp
public sealed record OperationIntentRecord
{
    public string OperationId { get; init; } = string.Empty; // e.g. "OP-20261007-143000-01"
    public string PlanId { get; init; } = string.Empty;
    public string PlanHash { get; init; } = string.Empty;
    public string TargetDocTitle { get; init; } = string.Empty;
    public Guid ExpectedVersionGuid { get; init; }
    public OperationIntentStatus Status { get; init; } = OperationIntentStatus.PendingTransmission;
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public IReadOnlyList<string> PlannedSourceKeys { get; init; } = Array.Empty<string>();
}

public enum OperationIntentStatus
{
    PendingTransmission = 1,
    InFlight = 2,
    Committed = 3,
    RolledBack = 4,
    Failed = 5,
    OutcomeUnknown = 6
}
```

#### 3.3. Thiết Kế Schema Extensible Storage & Phần Tử `DataStorage`
Revit API cung cấp lớp `Autodesk.Revit.DB.ExtensibleStorage.DataStorage`, là một element phi hình học được lưu vĩnh viễn trong file RVT mà người dùng không thể vô tình xóa trên viewport đồ họa.

1. **Định nghĩa Schema `HPBimBatchReceipt`**:
   - `SchemaGuid`: `D4E5F6A7-8B9C-0D1E-2F3A-4B5C6D7E8F90`
   - `SchemaName`: `"HPBimBatchReceipt"`
   - `Fields`:
     * `OperationId` (`string`): Khóa định danh của operation.
     * `PlanHash` (`string`): Hash của plan đã thực thi.
     * `CommittedAt` (`string`): Timestamp ISO-8601.
     * `ReceiptJson` (`string`): Chuỗi JSON nén chứa danh sách các cặp `{ "sourceKey": "...", "uniqueId": "..." }`.
       *(Revit Extensible Storage cho phép string field lên tới 16 MB, một mẻ 200 cấu kiện chỉ chiếm ~15 KB)*.
2. **Thực thi Ghi Biên Nhận Nguyên Tử (Atomicity Guarantee)**:
   Việc ghi biên nhận vào `DataStorage` phải diễn ra bên trong **CÙNG MỘT TRANSACTION** với lệnh tạo dựng các cấu kiện BIM:
   ```csharp
   using (var tx = new Transaction(doc, "HPBim: Apply " + plan.PlanId))
   {
       tx.Start();
       
       // 1. Tạo hoặc cập nhật các elements trong Revit
       var mappings = new List<ElementReceiptMapping>();
       foreach (var item in plan.Items)
       {
           var instance = CreateOrUpdateElement(doc, item);
           mappings.Add(new ElementReceiptMapping(item.SourceKey, instance.UniqueId));
       }
       
       // 2. Tìm hoặc tạo DataStorage element ẩn
       var dataStorage = new FilteredElementCollector(doc)
           .OfClass(typeof(DataStorage))
           .Cast<DataStorage>()
           .FirstOrDefault(ds => ds.Name == "HPBim_Coordination_DataStorage");
       
       if (dataStorage == null)
       {
           dataStorage = DataStorage.Create(doc);
           dataStorage.Name = "HPBim_Coordination_DataStorage";
       }
       
       // 3. Ghi Entity Extensible Storage
       var schema = Schema.Lookup(ReceiptSchemaId) ?? BuildReceiptSchema();
       var entity = new Entity(schema);
       entity.Set("OperationId", plan.OperationId);
       entity.Set("PlanHash", plan.PlanHash);
       entity.Set("CommittedAt", DateTime.UtcNow.ToString("O"));
       entity.Set("ReceiptJson", JsonSerializer.Serialize(mappings));
       dataStorage.SetEntity(entity);
       
       // 4. Commit nguyên tử
       tx.Commit();
   }
   ```
   **Nguyên tắc bảo toàn nguyên tử**: Nếu có bất kỳ lỗi nào xảy ra trong quá trình dựng hoặc commit bị hủy, toàn bộ cấu kiện VÀ biên nhận trong `DataStorage` sẽ cùng bị rollback 100%. Nếu commit thành công, cả cấu kiện lẫn biên nhận đều hiện diện vĩnh viễn trong RVT.

---

### Trọng Tâm 4: Phân Tách Timeout & Tự Động Reconcile (Timeout Separation & Auto-Reconcile)

#### 4.1. Hiện trạng Mã Nguồn (As-Is State)
- Trong `RevitPipeClient.cs:98-99, 133-141`:
  Client sử dụng một bộ đếm timeout duy nhất:
  ```csharp
  var timeout = TimeSpan.FromSeconds(request.TimeoutSeconds > 0 ? request.TimeoutSeconds : _options.DefaultExecuteTimeoutSeconds);
  using var timeoutCts = new CancellationTokenSource(timeout);
  // Khi timeout kích hoạt:
  tcs.TrySetException(new RevitPipeException($"Request {id} ({method}) timed out after {timeout.TotalSeconds}s.", isTimeout: true));
  ```
- Trong `ScriptRunner.cs:50`:
  Bridge Revit sử dụng:
  ```csharp
  using var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Clamp(request.TimeoutSeconds, 5, 120)));
  ```
- **Xung đột Timeout**: Nếu client timeout ở giây 60, nhưng Revit đang bận ở hàng đợi ExternalEvent hoặc script xử lý dở dang đến giây 61 mới commit, client sẽ ngắt kết nối pipe và quăng lỗi ngoại lệ.
  Lúc này, client rơi vào tình trạng **mù trạng thái**: Không biết Revit đã commit hay rollback. Nếu client retry, nó sẽ tạo thêm một bộ cấu kiện nữa. Nếu client bỏ qua, sổ cái ledger bị lệch khỏi mô hình thật.

#### 4.2. Phân Tách 3 Tầng Timeout (3-Tier Timeout Separation)
Để triệt tiêu lỗi mất đồng bộ, kiến trúc phải phân định rạch ròi 3 tầng timeout:

| Tầng Timeout | Thời Lượng | Phạm Vi Kiểm Soát | Hành Động Khi Hết Hạn |
|---|---|---|---|
| **1. Transport Timeout ($T_{\text{trans}}$)** | 5 – 10 giây | Thời gian mở kết nối pipe stream và hoàn tất ghi dòng NDJSON sang server. | Chắc chắn lệnh **chưa bao giờ chạy** trên Revit. Hủy request an toàn, ghi nhận `OutcomeNeverSent`. Được phép retry kết nối. |
| **2. Queue Deadline ($T_{\text{queue}}$)** | 15 – 30 giây | Thời gian yêu cầu nằm trong hàng đợi `_pending` của `McpBridgeExternalEventHandler` chờ Revit UI thread rảnh để nhặt (`Execute(application)`). | Nếu người dùng đang mở hộp thoại modal trong Revit, event không được gọi. Bridge chủ động hủy item khỏi hàng đợi và trả về mã lỗi `BridgeErrorCode.Busy` hoặc `QueueDeadlineExpired`. Tài liệu chưa bị sửa đổi. |
| **3. Execution Budget ($T_{\text{exec}}$)** | 30 – 120 giây | Thời gian cấp phép cho Roslyn C# script chạy bên trong `ScriptRunner.Run` trên Revit thread. | Script sử dụng cooperative cancellation token. Nếu hết hạn, `ScriptRunner` chủ động Rollback Transaction/TransactionGroup và trả về `TimedOut = true, RolledBack = true`. |

**Quy tắc phối hợp thời gian**:
Thời gian timeout tối đa mà Pipe Client chờ đợi ($T_{\text{client}}$) **bắt buộc phải lớn hơn** tổng thời gian hàng đợi và ngân sách thực thi:
$$T_{\text{client}} \ge T_{\text{trans}} + T_{\text{queue}} + T_{\text{exec}} + T_{\text{grace}} \quad (\text{với } T_{\text{grace}} \approx 10\text{s})$$
Nhờ đó, Client không bao giờ tự ý cúp kết nối trước khi Revit kịp Rollback và báo cáo kết quả.

#### 4.3. Xử Lý `OutcomeUnknown` & Quy Trình Tự Động Reconcile Khi Kết Nối Lại
Nếu xảy ra sự cố phần cứng, sập nguồn, Revit crash hoặc đứt pipe đột ngột trong khi trạng thái đang là `InFlight`:
1. **Ghi nhận `OutcomeUnknown`**: Client đánh dấu trạng thái của `OperationId` trong `IntentJournal` là `OutcomeUnknown`.
2. **Quy trình Auto-Reconcile khi Kết Nối Lại**:
   Khi Client kết nối lại tới Revit Named Pipe:
   - Client kiểm tra `IntentJournal`, phát hiện thao tác `OP-xxx` có trạng thái `OutcomeUnknown`.
   - **Cấm gửi lệnh dựng tiếp theo**. Client lập tức phát lệnh truy vấn đọc biên nhận:
     ```csharp
     var checkReq = new ExecuteRequest(
         Code: BuildQueryReceiptScript(opId),
         Transaction: TransactionModes.None,
         DryRun: false,
         TimeoutSeconds: 30,
         Label: "Reconcile Operation " + opId);
     ```
   - Script trên Revit tìm kiếm `DataStorage` có schema `HPBimBatchReceipt` và kiểm tra `OperationId == "OP-xxx"`:
     - **Nhánh A: Tìm thấy biên nhận trong Revit**:
       - Chứng minh Transaction **đã commit thành công 100%** trước khi đứt kết nối!
       - Revit trả về nội dung `ReceiptJson` chứa danh sách các cặp `{ SourceKey, UniqueId }`.
       - Client cập nhật `IntentJournal` thành `Committed`.
       - Client cập nhật `bim_coordination_ledger.json`, điền toàn bộ `UniqueId` tương ứng và chuyển cấu kiện sang `Step3_ModelRevit`.
       - Thông báo: "Tự động phục hồi thành công thao tác OP-xxx từ Extensible Storage."
     - **Nhánh B: Không tìm thấy biên nhận trong Revit**:
       - Chứng minh Transaction **đã bị Rollback** (hoặc chưa bao giờ được chạy).
       - Trong Revit không hề có cấu kiện rác hay dầm ma nào từ thao tác này.
       - Client cập nhật `IntentJournal` thành `RolledBack`.
       - Client thông báo an toàn và cho phép chạy lại Kế hoạch (Re-dispatch Plan).

---

### Trọng Tâm 5: Kiểm Toán Hình Học Đo Đạc Thực Nghiệm (Measured Geometric Verification)

#### 5.1. Hiện trạng Mã Nguồn & Khiếm Khuyết Đo Số Lượng (The Count Metric Flaw)
- **Trong `RevitModelVerifier.cs`** (`HPBim/src/HPBim.Client/Verification/RevitModelVerifier.cs:48-75`):
  ```csharp
  var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).GetElementCount();
  var grids = new FilteredElementCollector(doc).OfClass(typeof(Grid)).GetElementCount();
  var columns = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_StructuralColumns).GetElementCount();
  var beams = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).OfCategory(BuiltInCategory.OST_StructuralFraming).GetElementCount();
  ...
  return CompareCounts(expectedModel, result.Value.Value);
  ```
  Đây là **khiếm khuyết nghiêm trọng nhất**: Chỉ đếm tổng số lượng phần tử (`GetElementCount()`).
  - Nếu mô hình đã có sẵn 5 cột từ trước, và ta dựng 10 cột nhưng 5 cột bị lệch tọa độ và 5 cột bị trùng, tổng số đếm vẫn là 15 hoặc khớp ngẫu nhiên, hệ thống vẫn báo PASS.
- **Trong `FoundationQcEngine.cs:54-67`**:
  Bước 2 kiểm tra số lượng: `if (piles != 196) ... if (footings != 72) ...`
- **Trong `AuditColumnsCommand.cs:177`**:
  Kiểm tra số lượng cứng: `bool s1Pass = ccCount == 72 && c1Count == 72 && liveCols.Count == 144;`.

#### 5.2. Phương Pháp Đo Đạc Hình Học Trực Tiếp từ Revit API
Để loại bỏ triệt để các metric đếm số lượng, quy trình kiểm toán hình học độc lập (Measured Geometric Verification) phải đo đạc trực tiếp các thông số hình học của từng phần tử cụ thể thông qua `UniqueId` đã ánh xạ:

1. **Vị Trí Mặt Bằng & Tâm Cấu Kiện**:
   - **Cột & Cọc**:
     ```csharp
     var locPt = element.Location as LocationPoint;
     XYZ centerPt = locPt.Point;
     double measuredX = centerPt.X * 304.8;
     double measuredY = centerPt.Y * 304.8;
     ```
   - **Dầm & Tường**:
     ```csharp
     var locCrv = element.Location as LocationCurve;
     Curve crv = locCrv.Curve;
     XYZ p0 = crv.GetEndPoint(0);
     XYZ p1 = crv.GetEndPoint(1);
     XYZ midPt = (p0 + p1) * 0.5;
     ```
   - **Đài Móng & Sàn**:
     Trích xuất Polygon từ mặt phẳng đáy hoặc đỉnh (`PlanarFace` có normal $\pm Z$) và tính tọa độ trọng tâm (Centroid):
     ```csharp
     // Xem mẫu trích xuất thực tế tại RevitFoundationQueryService.cs:160-193
     ```
2. **Cao Trình Đỉnh & Đáy ($z_{\text{top}}, z_{\text{bot}}$)**:
   - Không được chỉ tin vào tham số Offset của Family (vì family có thể bị rỗng hoặc offset đảo chiều).
   - Đo trực tiếp từ hình học Solid:
     ```csharp
     double solidTopZ = double.MinValue;
     double solidBotZ = double.MaxValue;
     foreach (GeometryObject obj in element.get_Geometry(new Options()))
     {
         if (obj is Solid s && s.Volume > 0.0001)
         {
             foreach (Face f in s.Faces)
             {
                 if (f is PlanarFace pf)
                 {
                     if (pf.FaceNormal.IsAlmostEqualTo(XYZ.BasisZ)) solidTopZ = Math.Max(solidTopZ, pf.Origin.Z * 304.8);
                     if (pf.FaceNormal.IsAlmostEqualTo(-XYZ.BasisZ)) solidBotZ = Math.Min(solidBotZ, pf.Origin.Z * 304.8);
                 }
             }
         }
     }
     ```
   - Đối soát chéo với `BoundingBox.Max.Z` và `BoundingBox.Min.Z`.
3. **Kích Thước Tiết Diện ($b \times h, D$)**:
   - Đọc tham số Symbol của Type: `LookupParameter("b")`, `LookupParameter("h")`, `LookupParameter("Width")`, `LookupParameter("Height")`.
   - Đối soát với diện tích mặt cắt ngang của Solid:
     $$A_{\text{solid}} = \iint dx dy \approx b_{\text{param}} \times h_{\text{param}}$$

#### 5.3. Ma Trận Đánh Giá So Sánh với `BimExecutionPlan`
Với mỗi cấu kiện trong kế hoạch `BimExecutionPlan`:

| Tiêu Chí Đo | Công Thức Sai Số | Ngưỡng Dung Sai Cho Phép (`CoordinationTolerances`) | Kết Luận Nếu Vượt Ngưỡng |
|---|---|---|---|
| **Tiết diện ($b, h$)** | $\Delta_{\text{dim}} = \max(\|b_{\text{meas}} - b_{\text{plan}}\|, \|h_{\text{meas}} - h_{\text{plan}}\|)$ | $\le 0.0\text{ mm}$ (`SectionDimensionMm`) | **FAIL / DEFECT** (Sai kích thước chế tạo) |
| **Cao độ đỉnh/đáy** | $\Delta_{\text{elev}} = \max(\|z_{\text{top,meas}} - z_{\text{top,plan}}\|, \|z_{\text{bot,meas}} - z_{\text{bot,plan}}\|)$ | $\le 1.0\text{ mm}$ (`ElevationMm`) | **FAIL / DEFECT** (Lệch cao trình cốt đỉnh/đáy) |
| **Tọa độ tim/trọng tâm** | $\Delta_{\text{pos}} = \text{Distance}(P_{\text{meas}}, P_{\text{plan}})$ | $\le 1.0\text{ mm}$ (`PlanPositionMm`)<br>($\le 2.0\text{ mm}$ cho tim cọc) | **FAIL / DEFECT** (Lệch vị trí mặt bằng) |
| **Góc xoay / Hướng** | $\Delta_{\theta} = \|\theta_{\text{meas}} - \theta_{\text{plan}}\|$ | $\le 0.05^\circ$ | **FAIL / DEFECT** (Xoay sai trục cấu kiện) |

#### 5.4. Loại Bỏ Triệt Để "Count PASS": Nguyên Tắc Kiểm Toán Độc Lập Mới
1. **Không dùng Element Count để kết luận PASS**:
   - Nếu dự án có 72 đài móng, nhưng 1 đài bị lệch cao độ 1.5mm ($\Delta_{\text{elev}} > 1.0\text{ mm}$):
     **KẾT LUẬN TOÀN DIỆN LÀ FAIL** (Báo cáo chỉ rõ: Đài `DC-3` tại trục `(4, B)` sai cao độ đỉnh -1598.5mm thay vì -1600.0mm).
   - Tuyệt đối cấm kết luận PASS khi chỉ dựa vào: "Đã tìm thấy đủ 72 đài móng trong mô hình".
2. **Quy tắc "Chưa đủ cơ sở để kết luận" (Inconclusive / ChuaDuCoSo)**:
   - Nếu cấu kiện trong Revit bị thiếu dữ liệu hình học Solid (ví dụ: Family rỗng không sinh Solid, hoặc không lấy được BoundingBox), hoặc thiếu bản vẽ chi tiết đối chiếu từ CAD:
     Hệ thống ném trạng thái `AuditStatus.ChuaDuCoSo`, cấm tự động nghiệm thu hoặc phỏng đoán.

---

## 3. Bản Đồ Tương Tác Giữa Các Thành Phần (Component Interaction Map)

```
[ CAD Drawings / DWG ]
        │
        ▼ (Extract with PrimaryHandle & ProvenanceHandles)
[ CadSourceSpec ] ──► [ Composite Source Key: Doc#Cat:Handle ]
        │
        ▼ (FailFastGate Check: Zero Speculation)
[ CoordinationElementRecord (Group1_ReadyToModel) ]
        │
        ▼ (Build Deterministic IR)
[ BimExecutionPlan (PlanId, PlanHash, TargetDocIdentity) ]
        │
        ├─────────────────────────────────────────────────┐
        ▼ (1. Write Local Journal synchronously)          ▼
[ IntentJournal (PendingTransmission) ]                   │
        │                                                 │
        ▼ (2. Send over Named Pipe)                       │
[ RevitPipeClient (T_transport / T_client) ]               │
        │                                                 │
        ▼ (Named Pipe Stream)                             │
[ HPRebar.McpBridge ]                                     │
        │                                                 │
        ▼ (3. On-Thread Pre-Flight Check)                 │
        ├── Title / Path matches TargetDoc?                │
        └── VersionGUID freshness valid?                  │
        │                                                 │
        ▼ (4. Atomic Execution inside single Transaction) │
        ├── Create/Update Elements (Idempotent by Key)    │
        └── Write Receipt & Mappings to DataStorage ◄─────┘
        │
        ▼ (Reply or Disconnect/Timeout)
┌───────┴────────────────────────┐
│ Pipe Reply Received?           │
├───────────────┬────────────────┤
│ YES (Success) │ NO / Timeout   │
│               │                │
│ Update Ledger │ Mark Journal:  │
│ (Step3)       │ OutcomeUnknown │
│               │                │
│               │ Upon Reconnect:│
│               │ Query Receipt  │
│               │ from DataStorage
│               │ Reconcile!     │
└───────┬───────┴────────────────┘
        │
        ▼
[ Measured Geometric Verification ]
  ├── Query live Solid / Curve / Params by UniqueId
  ├── Compare Dimensions (tol = 0.0mm)
  ├── Compare Elevations (tol = 1.0mm)
  └── Compare Position   (tol = 1.0/2.0mm)
        │
        ▼
[ AuditRecord: Dung / SaiCanSua / ChuaDuCoSo ]
```

---

## 4. Bằng Chứng & Trích Dẫn Mã Nguồn (Evidence Citations)

| Thành Phần Khảo Sát | Tệp Mã Nguồn | Vị Trí Dòng | Quan Sát Thực Tế |
|---|---|---|---|
| **Extensible Storage Mẫu** | `HPRebar/HPRebar/KataRebar/Service/KataRebarStorage.cs` | Dòng 12–52 | Triển khai mẫu `SchemaBuilder`, `Schema.Lookup`, `AddSimpleField`, lưu trữ trên Revit element. Cần mở rộng sang `DataStorage`. |
| **Xóa & Dựng Idempotent** | `HPRebar/HPRebar/KataRebar/Service/KataRebarCleanupService.cs` | Dòng 14–41 | Lọc element cũ qua Extensible Storage (`KataRebarStorage.Read`) để tránh xóa nhầm đối tượng vẽ tay. |
| **Named Pipe Client & Timeout** | `HPBim/src/HPBim.Client/Connection/RevitPipeClient.cs` | Dòng 95–144 | Timeout duy nhất cho cả send và wait; quăng `RevitPipeException` làm mất trạng thái thực của Revit. |
| **Bridge Executor & Context** | `HPRebar/HPRebar.McpBridge/McpBridgeExternalEventHandler.cs` | Dòng 64–113 | Nhận `ExecuteRequest`, chạy trên Revit thread qua `ExternalEvent.Raise()`, có thể bị trễ trong hàng đợi nếu Revit busy. |
| **Transaction & Rollback** | `HPRebar/HPRebar.McpBridge/Service/ScriptRunner.cs` | Dòng 74–136 | TransactionGroup rollback sạch sẽ khi `dryRun: true` hoặc khi ném `OperationCanceledException`. |
| **Context Snapshot** | `HPRebar/HPRebar.McpBridge/Service/RevitContextReader.cs` | Dòng 17–40 | Đọc `DocTitle`, `DocPath`, `Units`, `ActiveView`. Chưa đọc `DocumentVersion.VersionGUID`. |
| **Ledger & FailFastGate** | `HPBim/src/HPBim.Core/Coordination/FailFastGate.cs` | Dòng 48–190 | Kiểm soát không phỏng đoán: bắt buộc có handle, cao độ đỉnh/đáy, tiết diện và điểm tựa liên kết. |
| **Lệnh Dựng Thiếu Idempotency** | `HPBim/src/HPBim.Cli/Commands/GenerateGradeBeamsCommand.cs` | Dòng 201–235 | Dựng dầm mới bằng `NewFamilyInstance` không kiểm tra trùng, lặp lại sẽ sinh dầm đè dầm. |
| **Truy Vấn Hình Học Solid** | `HPBim/src/HPBim.Cli/Commands/RevitFoundationQueryService.cs` | Dòng 83–126, 159–193 | Trích xuất Solid planar face đỉnh/đáy và Polygon đường bao từ Revit API. Mẫu tảng rất tốt để phát triển engine đo đạc. |
| **Khiếm Khuyết Đếm Số Lượng** | `HPBim/src/HPBim.Client/Verification/RevitModelVerifier.cs` | Dòng 48–75 | Chỉ dùng `GetElementCount()` để so sánh tổng số phần tử. Dễ báo PASS ảo khi số lượng ngẫu nhiên khớp. |

---

## 5. Kết Luận & Khuyến Nghị Thực Hiện Cho Orchestrator

1. **Khuyến nghị 1 (Core Models)**:
   Bổ sung vào `HPBim.Core`:
   - `CompositeSourceKey` cấu trúc chuẩn `[DocIdentifier]#[Category]:[PrimaryHandle]`.
   - Cập nhật `CoordinationElementRecord` với `SourceKey` và `ProvenanceHandles`.
   - Bổ sung `DocumentIdentitySnapshot` và `OperationIntentRecord`.
2. **Khuyến nghị 2 (Client & Pipe)**:
   - Cập nhật `RevitPipeClient` và pipeline để ghi `IntentJournal` trước khi gửi pipe.
   - Thêm phương thức `QueryReceiptAsync(string operationId)` và cơ chế `ReconcileAsync()` khi kết nối lại.
3. **Khuyến nghị 3 (Revit Script Generator)**:
   - Nhúng khối lệnh kiểm tra tiền trạm (Document Freshness Gate) vào đầu script.
   - Nhúng khối ghi biên nhận `HPBimBatchReceipt` vào `DataStorage` trong cùng transaction với lệnh dựng hình.
   - Triển khai logic kiểm tra tồn tại cấu kiện trước khi tạo mới để đạt 100% Idempotency.
4. **Khuyến nghị 4 (Independent Geometric QA/QC Engine)**:
   - Thay thế hoàn toàn `RevitModelVerifier` đếm số lượng bằng `BimGeometricAuditor`.
   - Đối soát từng cấu kiện theo `SourceKey -> UniqueId` với các dung sai nghiêm ngặt: Section 0.0mm, Elevation 1.0mm, Position 1.0mm (cọc 2.0mm).

Báo cáo khảo sát hoàn tất, sẵn sàng bàn giao cho Orchestrator lập kế hoạch chi tiết và phân công các worker thực thi.
