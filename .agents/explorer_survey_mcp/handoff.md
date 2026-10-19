# Báo Cáo Khảo Sát Kỹ Thuật: Hạ Tầng Revit MCP Named Pipe, McpShared & Revit API 2026 Cho HPBim

**Tác giả**: teamwork_preview_explorer  
**Ngày khảo sát**: 2026-10-06  
**Vị trí tài liệu**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_mcp\handoff.md`  
**Mục tiêu**: Khảo sát toàn diện cơ chế Named Pipe, JSON-RPC, Roslyn Scripting Executor, Transaction/DryRun rollback của Revit 2026 MCP Bridge và các pattern tạo dựng mô hình BIM (TCVN / NhaPho 5x18m) để phục vụ thiết kế và hiện thực giải pháp **HPBim** độc lập.

---

## 1. Observation (Quan Sát Trực Tiếp Từ Mã Nguồn)

### 1.1. Hạ Tầng Giao Tiếp Named Pipe & Giao Thức JSON-RPC (`McpShared/`)

#### 1.1.1. Định Danh Pipe & Độc Quyền Host (`HPRebar.Mcp.Contracts/PipeNaming.cs`)
- **Tên pipe Revit**: Được định nghĩa tại dòng 13–15, 61–62 của `PipeNaming.cs`:
  ```csharp
  public const string Prefix = "hprebar-mcp-r";
  public const string RevitHost = "revit";
  public static string For(int revitVersion) => Prefix + revitVersion;
  ```
  Với Revit 2026, tên Named Pipe trên Windows luôn là:  
  `\\.\pipe\hprebar-mcp-r2026` (hoặc định danh ngắn `hprebar-mcp-r2026`).
- **Phân tách phiên bản và ứng dụng**: Một máy có thể chạy song song AutoCAD, Civil 3D, Navisworks, Revit. Mỗi host và version có pipe riêng biệt, không xung đột.

#### 1.1.2. Khung Thông Điệp JSON-RPC 2.0 (`HPRebar.Mcp.Contracts/JsonRpc/`)
- **Envelope (`JsonRpcEnvelope.cs`)**:
  - `jsonrpc`: `"2.0"`
  - `id`: `long?` (phân biệt Request vs Notification vs Response)
  - `method`: `string?` (dạng `{host}.{suffix}`, ví dụ `revit.execute`, `revit.context`, `revit.ping`, `revit.cancel`)
  - `params`: `JsonElement?`
  - `result`: `JsonElement?`
  - `error`: `JsonRpcError?` (`Code: int`, `Message: string`)
- **Phương thức chuẩn (`JsonRpcMethods.cs`)**:
  - Request methods: `revit.ping` (ping), `revit.context` (context), `revit.inspect` (inspect), `revit.execute` (execute), `revit.cancel` (cancel), `revit.analyze` (analyze).
  - Notification methods: `revit.progress` (progress), `revit.log` (log), `revit.status` (status).
- **Mã lỗi Bridge chuẩn (`BridgeErrorCode.cs`)**:
  - Chuẩn JSON-RPC: `ParseError` (-32700), `InvalidRequest` (-32600), `MethodNotFound` (-32601), `InternalError` (-32000).
  - Trạng thái Actionable:
    * `ExecutionDisabled` (-32001): Người dùng chưa tick "Allow AI code execution" trong cửa sổ UI MCP Bridge.
    * `Busy` (-32002): Script khác đang chạy trên luồng Revit API.
    * `NoActiveDocument` (-32003): Chưa mở dự án Revit (không có active document).

#### 1.1.3. Cơ Chế Kết Nối Client & Bảo Mật ACL (`HPRebar.Mcp.Server.Core/Services/`)
- **NdjsonPipeTransport (`NdjsonPipeTransport.cs`)**:
  - Giao tiếp hai chiều thông qua `NamedPipeClientStream` với cờ `PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly`.
  - Định dạng luồng dữ liệu: **NDJSON (Newline-Delimited JSON)**, mã hóa `UTF-8` không kèm BOM (`new UTF8Encoding(false)`), ký tự kết thúc dòng `\n`.
  - Bộ đệm đọc/ghi: 64 KB StreamReader, 8 KB StreamWriter (`AutoFlush = true`).
  - Bảo mật Windows ACL: `CurrentUserOnly` đảm bảo phía Client chỉ kết nối được với Named Pipe do chính Windows User Account hiện tại tạo ra (chống elevation privilege attack).
- **RevitBridgeClient (`RevitBridgeClient.cs`)**:
  - Quản lý tương quan yêu cầu/phản hồi (correlation) bằng `ConcurrentDictionary<long, PendingCall>` thông qua `id` tự tăng (`Interlocked.Increment(ref _nextId)`).
  - Kết nối lười (Lazy Connect) thông qua `SemaphoreSlim _connectLock` khi phát sinh cuộc gọi đầu tiên.
  - Vòng lặp đọc bất đồng bộ chạy nền `ReadLoopAsync()`; khi nhận được thông báo phản hồi, kích hoạt `TaskCompletionSource<JsonRpcEnvelope>`.
  - Xử lý Progress notification đồng bộ thông qua `SynchronousProgress<ProgressParams>` để không làm xáo trộn thứ tự sự kiện.

---

### 1.2. Động Cơ Thực Thi Mã Script Trong Revit 2026 (`HPRebar.McpBridge/`)

#### 1.2.1. Điều Phối Luồng Với Revit ExternalEvent (`McpBridgeExternalEventHandler.cs`)
- Revit API là môi trường đơn luồng (Single-Threaded Apartment - STA); mọi thao tác truy cập `Document` bắt buộc phải chạy trên main UI thread của Revit.
- Lớp `McpBridgeExternalEventHandler` kế thừa đồng thời `IExternalEventHandler` và `IBridgeExecutor`.
- **Pipeline xử lý 2 pha tối ưu hóa**:
  1. *Pha 1 (Trên Named Pipe thread pool)*: Kiểm tra an toàn tĩnh với `ScriptGuard.Check(code)` và biên dịch trước bằng Roslyn `_compiler.GetOrCompile(code)`. Nếu có lỗi cú pháp hoặc vi phạm guard, trả về lỗi ngay lập tức mà không cần chiếm dụng luồng Revit!
  2. *Pha 2 (Trên Revit main thread)*: Đóng gói thành `McpBridgeRequest`, đưa vào hàng đợi `ConcurrentQueue<McpBridgeRequest>` và phát tín hiệu `_externalEvent.Raise()`. Revit sẽ kích hoạt hàm `Execute(UIApplication uiapp)` khi nhàn rỗi (quiescent).

#### 1.2.2. Kiểm Soát An Toàn AST Guard (`HPRebar.McpBridge.Core/Scripting/ScriptGuard.cs`)
- Sử dụng Roslyn `CSharpSyntaxTree` duyệt cây cú pháp bằng `CSharpSyntaxWalker`.
- Chặn tuyệt đối các namespace/lớp nguy hiểm:
  - `System.IO` (chỉ cho phép ngoại lệ `System.IO.Path`), `System.Net`, `System.Diagnostics.Process`, `System.Reflection`, `System.Runtime.InteropServices`, `System.Threading.Tasks`, `System.Security`, `Microsoft.Win32`.
  - Chặn các chỉ thị `#r` và `#load` (Directives trivia).
  - Chặn từ khóa `await`, `async lambda` (vì script chạy đồng bộ trên Revit API thread).
  - Chặn `unsafe`, `dynamic`, `Activator`, `Thread`, `Task.Run`.

#### 1.2.3. Trình Biên Dịch Roslyn & Cache (`ScriptCompiler.cs` & `Application.cs`)
- Biên dịch động bằng `Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript.Create<object>()`.
- Bộ cache script theo mã băm SHA256 (`ScriptCache`) dung lượng mặc định 128 script, giúp các lệnh gọi lặp lại (idempotent/batch) đạt tốc độ thực thi tức thì (0ms biên dịch).
- Namespace tự động import sẵn cho mọi script:
  ```csharp
  "System", "System.Linq", "System.Collections.Generic",
  "Autodesk.Revit.DB", "Autodesk.Revit.UI", "Autodesk.Revit.DB.Structure",
  "HPRebar.McpBridge.Core.Scripting"
  ```
- Assemblies được nạp sẵn: `RevitAPI.dll`, `RevitAPIUI.dll`, `System.Runtime`, `System.Collections`, `netstandard`, `System.Text.Json`, `HPRebar.McpBridge.Core`.

#### 1.2.4. Biến Toàn Cục (Script Globals) (`HPRebar.McpBridge/Model/ScriptGlobals.cs`)
Trong thân script C#, các biến toàn cục sau luôn có sẵn để truy cập trực tiếp:
- `doc`: `Autodesk.Revit.DB.Document` (Active project document).
- `uidoc`: `Autodesk.Revit.UI.UIDocument` (Active UI document).
- `app`: `Autodesk.Revit.ApplicationServices.Application`.
- `uiapp`: `Autodesk.Revit.UI.UIApplication`.
- `ct`: `CancellationToken` (cho phép thoát sớm khi gặp timeout hoặc cancel).
- `log`: `Action<string>` (ghi nhật ký trả về trong mảng `logs`).
- `progress`: `Action<int, int, string>` (báo cáo tiến độ: `progress(cur, total, "Thông điệp")`).
- `args`: `HPRebar.McpBridge.Core.Scripting.ScriptArgs` (dữ liệu JSON đầu vào: `args.Double("width")`, `args.Str("name")`, `args.List("data")`, `args.Obj("location")`).
- **Giá trị trả về**: Biểu thức cuối cùng của script hoặc lệnh `return <object>;` sẽ được serialize tự động trả về client.

#### 1.2.5. Xử Lý Transaction & Cơ Chế dryRun Rollback Tuyệt Đối (`ScriptRunner.cs`)
- `ScriptRunner.Run()` kiểm soát 3 chế độ giao dịch qua tham số `transaction`:
  - `"auto"`: Bridge tự mở một `Transaction` bọc ngoài script.
  - `"manual"`: Script tự mở và commit `Transaction` của riêng mình.
  - `"none"`: Chế độ chỉ đọc (Read-only); nếu script cố tình chỉnh sửa model, Revit sẽ ném ngoại lệ `ModificationOutsideTransactionException`.
- **Cơ chế bọc lồng `TransactionGroup` và `AutoDismissFailurePreprocessor`**:
  - Trước khi chạy, Bridge khởi tạo:
    ```csharp
    group = new TransactionGroup(doc, "MCP: " + label);
    group.Start();
    ```
  - Trong chế độ `"auto"`, Transaction được gán preprocessor tự động xóa cảnh báo:
    ```csharp
    transaction = new Transaction(doc, "MCP script");
    AutoDismissFailurePreprocessor.ApplyTo(transaction);
    transaction.Start();
    ```
    *Nhờ `AutoDismissFailurePreprocessor`, các cảnh báo thông thường (ví dụ: tường đè nhẹ, phòng chưa khép kín) tự động bị xóa bỏ (`DeleteWarning`), ngăn chặn 100% việc Revit bật hộp thoại thông báo modal làm treo tiến trình!*
  - **Mô thức dryRun hoàn hảo**:
    ```csharp
    if (transaction != null && transaction.Commit() != TransactionStatus.Committed)
        throw new InvalidOperationException("Revit rejected the change");

    if (group != null)
    {
        if (request.DryRun)
        {
            group.RollBack(); // ROLLBACK TOÀN BỘ NHÓM GIAO DỊCH
            rolledBack = true;
        }
        else
        {
            group.Assimilate(); // GỘP THÀNH 1 MỤC UNDO DUY NHẤT
        }
    }
    ```
    *Ý nghĩa then chốt*: Khi `DryRun = true`, Transaction con bên trong vẫn được `Commit()`, cho phép Revit tính toán hình học thực tế, sinh `ElementId`, tính toán va chạm và đo đạc thuộc tính trong lúc script đang chạy. Nhưng ngay trước khi kết thúc hàm `Run()`, toàn bộ `TransactionGroup` bị `RollBack()`, hoàn trả Document về trạng thái nguyên bản 100% không để lại dấu vết thay đổi!

#### 1.2.6. Tuần Tự Hóa Kết Quả Trả Về (`ResultSerializer.cs`)
- Revit API chứa đồ thị đối tượng phức tạp và các getter có thể ném exception nếu duyệt sâu.
- `ResultSerializer` tóm tắt an toàn các kiểu dữ liệu Revit:
  - `ElementId`: Ghi số nguyên `id.Value` (kiểu `long` trên Revit 2024+).
  - `XYZ`: Ghi object `{ "x": x, "y": y, "z": z }`.
  - `Element`: Ghi tóm tắt `{ "id": id, "name": name, "category": cat, "type": className }`.
  - Các cấu trúc C# ẩn danh (`new { ... }`), mảng, `Dictionary<string, object>` được serialize thành JSON chuẩn.

---

### 1.3. Khảo Sát Thư Viện Hạt Nhân Revit BIM (`SeedLibrary`) Cho HPBim

Quan sát từ các hạt nhân thực tế trong `HPRebar.Mcp.Server/Registry/SeedLibrary/`:

| Hạt nhân mẫu | Đường dẫn mã nguồn | Revit API Call chính |
|---|---|---|
| `create_level` | `Architecture/create_level/code.cs` | `Level.Create(doc, elevationFt)`, `ViewPlan.Create(doc, typeId, levelId)` |
| `create_grid` | `Architecture/create_grid/code.cs` | `Grid.Create(doc, Line.CreateBound(p0, p1))` |
| `create_line_based_element` | `Generic/create_line_based_element/code.cs` | `Wall.Create(...)`, `doc.Create.NewFamilyInstance(...)` (Beam), `Pipe.Create(...)`, `Duct.Create(...)` |
| `create_point_based_element` | `Architecture/create_point_based_element/code.cs` | `doc.Create.NewFamilyInstance(pt, symbol, level, StructuralType.Column)`, Host placement trên Wall |
| `create_surface_based_element` | `Architecture/create_surface_based_element/code.cs` | `Floor.Create(doc, curveLoops, floorTypeId, levelId, isStructural, ...)`, `Ceiling.Create`, `NewFootPrintRoof` |
| `create_room` | `Architecture/create_room/code.cs` | `doc.Create.NewRoom(level, UV(x, y))`, `room.Area > 0` (kiểm tra enclosed) |
| `get_available_family_types` | `Generic/get_available_family_types/code.cs` | `FilteredElementCollector(doc).OfClass(typeof(FamilySymbol))` |
| `analyze_model_statistics` | `Data/analyze_model_statistics/code.cs` | `WhereElementIsNotElementType()`, thống kê theo Category và Level |
| `ai_element_filter` | `Data/ai_element_filter/code.cs` | `ElementCategoryFilter`, `BoundingBoxIntersectsFilter`, trích xuất thông số |

---

## 2. Logic Chain: Từ Quan Sát Đến Kiến Trúc Xây Dựng HPBim

Dưới đây là chuỗi lập luận kỹ thuật liên kết trực tiếp giữa các quan sát hạ tầng và các yêu cầu đặt ra cho `HPBim`:

### 2.1. Kết Nối Trực Tiếp Named Pipe Không Cần MCP Server Trung Gian
- *Quan sát*: `NdjsonPipeTransport` và `RevitBridgeClient` trong `McpShared` cung cấp sẵn giao thức NDJSON và JSON-RPC 2.0 chuẩn với bảo mật `CurrentUserOnly`. Tên pipe cố định `hprebar-mcp-r2026`.
- *Lập luận*:
  1. `HPBim.Client` không cần phải chạy một server MCP trung gian để giao tiếp với Revit.
  2. `HPBim.Client` chỉ cần phụ thuộc vào `HPRebar.Mcp.Contracts` và tầng Pipe Transport của `McpShared`, khởi tạo kết nối trực tiếp đến pipe `\\.\pipe\hprebar-mcp-r2026`.
  3. Mọi lệnh dựng hình từ CLI (`hpbim generate`) hoặc Agent sẽ gửi trực tiếp các payload `revit.execute` có chứa script Roslyn C# xuống Revit Bridge.

### 2.2. Trật Tự Dựng Hình Chuẩn BIM (Strict Geometric Dependency Order)
- *Quan sát*:
  - Wall cần tham chiếu `LevelId`.
  - Column cần `LevelId` và tọa độ tâm.
  - Beam cần `LevelId` và 2 điểm đầu mút (hoặc giao điểm trục).
  - Floor cần `LevelId` và đường biên khép kín `CurveLoop`.
  - Door và Window **bắt buộc** phải có `host` là một đối tượng `Wall` đã tồn tại trước đó.
  - Room **bắt buộc** phải được bao bọc bởi các phần tử có thuộc tính `Room Bounding` (như Wall, Column) và sàn trước khi đặt điểm tính diện tích (`doc.Create.NewRoom`).
  - MEP Fixture / Thiết bị vệ sinh thường được định vị theo tường hoặc sàn đã tạo.
- *Lập luận chuỗi thực thi*:
  `HPBim.Client` phải phân rã quy trình thành **8 Stage tuần tự bất khả biến**:
  $$\text{Levels} \longrightarrow \text{Grids} \longrightarrow \text{Columns/Beams/Floors} \longrightarrow \text{Walls} \longrightarrow \text{Doors/Windows} \longrightarrow \text{Rooms} \longrightarrow \text{MEP} \longrightarrow \text{Verification}$$

### 2.3. Giải Thuật Nhân Bản Type Động (Dynamic Type Duplication & Dimension Setting)
- *Quan sát*:
  - Trong dự án Revit mẫu, có thể chưa có sẵn `WallType` dày 110mm hoặc 220mm, `FamilySymbol` Cột BTCT 220x220mm, hay Cửa đi 900x2200mm.
  - `WallType` và `FloorType` là System Families: Không thể dùng `doc.LoadFamily()`, mà phải dùng `sourceType.Duplicate(newName)` và chỉnh sửa `CompoundStructure`.
  - Cột và Cửa là Component Families (`FamilySymbol`): Kích thước tiết diện là **Type Parameters**, không phải Instance Parameters! Nếu chỉ gán trên Instance thì tham số không có hiệu lực hoặc gây lỗi.
- *Lập luận giải pháp*:
  Script thực thi của `HPBim.Client` phải tích hợp cơ chế tự động tìm nạp và nhân bản:
  1. **Với Wall (Tường 110mm, 220mm)**:
     - Lọc `WallType` dạng `WallKind.Basic`.
     - Tìm xem đã có type có `Width == thicknessMm` chưa ($\pm 0.5\text{ mm}$).
     - Nếu chưa: Lấy type cơ bản gần nhất, gọi `copy = source.Duplicate($"Wall {thicknessMm}mm")`.
     - Lấy `cs = copy.GetCompoundStructure()`, lấy `core = cs.GetFirstCoreLayerIndex()`.
     - Đặt lại độ dày: `cs.SetLayerWidth(core, UnitUtils.ConvertToInternalUnits(thicknessMm, UnitTypeId.Millimeters))`.
     - Gán lại: `copy.SetCompoundStructure(cs)`.
  2. **Với Column (Cột 220x220mm)**:
     - Lọc `FamilySymbol` thuộc `BuiltInCategory.OST_StructuralColumns`.
     - Tìm type có kích thước $220 \times 220\text{ mm}$.
     - Nếu chưa: Chọn một Rectangular Column symbol bất kỳ, gọi `copy = source.Duplicate("C220x220")`.
     - Duyệt tìm và gán các tham số kích thước: `b`, `h` hoặc `Width`, `Depth` hoặc BuiltInParameter `STRUCTURAL_SECTION_COMMON_WIDTH`, `STRUCTURAL_SECTION_COMMON_HEIGHT`.
     - Kích hoạt type: `if (!copy.IsActive) copy.Activate()`.
  3. **Với Door (Cửa D2 900x2200mm) & Window (Cửa sổ S1 1600x1400mm)**:
     - Lọc `FamilySymbol` thuộc `BuiltInCategory.OST_Doors` hoặc `OST_Windows`.
     - Nhân bản: `copy = source.Duplicate("D_900x2200")`.
     - Gán tham số: `Width` / `DOOR_WIDTH` = 900mm; `Height` / `DOOR_HEIGHT` = 2200mm.

### 2.4. Đối Soát Kiểm Tra (Element Verification) Độc Lập
- *Quan sát*:
  - Sau khi dựng xong, script truy vấn có thể dùng `FilteredElementCollector(doc).WhereElementIsNotElementType()`.
  - Phân loại theo category hoặc lọc theo danh sách ElementId đã tạo trong từng Stage.
- *Lập luận*:
  Lệnh `hpbim verify` sẽ chạy một script truy vấn `revit.execute` với `transaction="none"`, trả về số lượng element thực tế trong Document (Levels, Grids, Columns, Walls, Doors, Windows, Rooms, Floors, MEP Pipes) đối chiếu 1:1 với dữ liệu đầu vào `NhaPho_5x18m.json`.

---

## 3. Caveats (Các Điểm Lưu Ý & Giới Hạn Cần Chú Ý)

1. **Revit API Units luôn là Feet**:
   Mọi hàm tạo hình trong Revit API (`Level.Create`, `XYZ`, `Line.CreateBound`, `Wall.Create`, `SetLayerWidth`) đều nhận đơn vị chiều dài là **Feet** (Imperial). Mọi dữ liệu mm từ `HPBim.Core` khi đưa vào script bắt buộc phải chuyển đổi qua `UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters)` (hoặc nhân với hệ số $1.0 / 304.8$).
2. **Cooperative Cancellation & Timeout**:
   Script chạy trên luồng Revit API có timeout tối đa mặc định là 120 giây. Đối với công trình lớn, nếu gom toàn bộ lệnh dựng hình vào 1 script duy nhất có thể vượt quá timeout hoặc làm đơ giao diện người dùng. Do đó, `HPBim.Client` nên gửi lệnh dựng hình theo từng Stage rời rạc (hoặc batch theo tầng).
3. **Mô thức dryRun trong Script**:
   Khi `dryRun = true`, toàn bộ `TransactionGroup` bị rollback ở cấp Bridge. Do đó, nếu Client thực hiện nhiều lệnh gọi `revit.execute` riêng biệt trong chế độ dry-run, thì mỗi lệnh gọi sẽ bị rollback ngay sau khi kết thúc lệnh đó!
   *Hệ quả*: Để dry-run toàn bộ mô hình từ Levels đến MEP mà các phần tử sau vẫn thấy phần tử trước, Client có 2 phương án:
   - *Phương án A*: Gom kịch bản dry-run thành một script tổng thể duy nhất gửi qua `revit.execute(dryRun: true)`.
   - *Phương án B*: Trong kịch bản kiểm thử, Client chạy dry-run trên từng module đơn lập để xác thực khả năng tạo hình và kiểm tra xung đột mà không lưu vào DB.
4. **Room Bounding và Enclosed Geometry**:
   Lệnh `doc.Create.NewRoom` chỉ trả về diện tích `room.Area > 0` khi các đường tâm tường tạo thành đa giác khép kín hoàn chỉnh tại cao độ đặt phòng. Nếu tường bị hở dù chỉ 1mm hoặc có lỗ mở chạm đáy không có ngưỡng, phòng sẽ ở trạng thái `Not Enclosed`. Cần gọi `doc.Regenerate()` trước khi đọc `room.Area`.
5. **AutoDismissFailurePreprocessor**:
   Khi tạo các phần tử tự động, Revit thường phát sinh warning (ví dụ: "Highlighted walls overlap", "Room is not in a properly enclosed region"). Nhờ `AutoDismissFailurePreprocessor` trong Bridge, các warning này không làm hiện modal dialog làm kẹt tiến trình. Tuy nhiên, logic QC phía C# của `HPBim.Core` phải chủ động kiểm tra khoảng cách và clash trước để tránh tạo ra mô hình lỗi hình học.

---

## 4. Conclusion & Kiến Trúc Đề Xuất Cho HPBim

Dựa trên kết quả khảo sát chi tiết, dưới đây là kiến trúc kỹ thuật đề xuất cho deliverable **HPBim**:

### 4.1. Cấu Trúc Solution & Phân Bố Dự Án (`HPBim.slnx`)

```
HPBim/
├── HPBim.slnx                                   # XML Solution format (.slnx)
├── global.json                                  # Pin SDK 10.0.300 + Microsoft.Testing.Platform
├── Directory.Build.props                        # Quản lý version, compile constants, Clean Code
├── src/
│   ├── HPBim.Core/                              # [R1] Domain thuần C# (target: netstandard2.0)
│   │   ├── Model/                               # AxisGrid, LevelDef, WallDef, ColumnDef, BeamDef, SlabDef, OpeningDef, RoomDef, MepDef
│   │   ├── Geometry/                            # Vector2D, Polyline2D, Liang-Barsky, Spatial Index, Offsets
│   │   ├── Standards/                           # TCVN standards, concrete grades, minimum cover, room area rules
│   │   ├── Qc/                                  # Pure C# clash detection, gap checking, boundary validator
│   │   └── Parser/                              # NhaPho JSON schema parser & validator
│   │
│   ├── HPBim.Client/                            # [R2] Revit Pipe Orchestrator (target: net8.0;net10.0)
│   │   ├── Connection/                          # Named Pipe Client nối \\.\pipe\hprebar-mcp-r2026
│   │   ├── ScriptGenerators/                    # Sinh Roslyn scripts cho 8 Stage (Levels -> Grids -> ... -> MEP)
│   │   ├── TypeResolver/                        # Logic kiểm tra & nhân bản FamilySymbol/WallType/FloorType
│   │   └── Pipeline/                            # BimGenerationPipeline điều phối Stage & dryRun
│   │
│   └── HPBim.Cli/                               # [R3] Console Executable CLI (target: net10.0)
│       ├── Commands/                            # ValidateCommand, GenerateCommand, VerifyCommand, EstimateCommand
│       └── Program.cs                           # CLI Entry point
│
└── tests/
    ├── HPBim.Core.Tests/                        # [R4] Unit tests xUnit v3 / MTP độc lập (0 tham chiếu Revit)
    │   ├── Fixtures/NhaPho5x18mFixture.cs       # Bộ dữ liệu Ground-Truth 5x18m port từ layout.py
    │   ├── GeometryTests.cs                     # Kiểm tra tính toán hình học, offset, Liang-Barsky
    │   └── QcEngineTests.cs                     # Kiểm tra phát hiện va chạm và chuẩn TCVN
    │
    └── HPBim.Client.Tests/                      # Integration tests Named Pipe round-trip
        ├── NamedPipeConnectionTests.cs
        └── ScriptGenerationTests.cs
```

### 4.2. Khung Mẫu Mã Nguồn Sinh Cho Từng Stage Của HPBim.Client

Dưới đây là các mẫu mã nguồn Roslyn Script tối ưu, tương thích hoàn toàn với Revit 2026 MCP Bridge:

#### Stage 1: Tạo Levels & Mặt Bằng Kế Thừa
```csharp
double Ft(double mm) => UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);
var existing = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().ToList();
var planType = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType))
    .Cast<ViewFamilyType>().FirstOrDefault(v => v.ViewFamily == ViewFamily.FloorPlan);

var created = new List<object>();
foreach (var item in args.List("levels"))
{
    string name = item.Require("name");
    double elevMm = item.RequireDouble("elevation");
    var match = existing.FirstOrDefault(l => string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase));
    if (match != null) { created.Add(new { id = match.Id.Value, name = match.Name, status = "exists" }); continue; }

    var level = Level.Create(doc, Ft(elevMm));
    level.Name = name;
    level.get_Parameter(BuiltInParameter.LEVEL_IS_BUILDING_STORY)?.Set(1);
    if (planType != null) ViewPlan.Create(doc, planType.Id, level.Id);
    created.Add(new { id = level.Id.Value, name = level.Name, status = "created" });
}
return new { count = created.Count, levels = created };
```

#### Stage 2: Tạo Hệ Trục Grids
```csharp
double Ft(double mm) => UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);
var existing = new FilteredElementCollector(doc).OfClass(typeof(Grid)).Cast<Grid>().Select(g => g.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
var created = new List<object>();

foreach (var item in args.List("grids"))
{
    string name = item.Require("name");
    if (existing.Contains(name)) continue;
    var p0 = new XYZ(Ft(item.Double("x0")), Ft(item.Double("y0")), 0);
    var p1 = new XYZ(Ft(item.Double("x1")), Ft(item.Double("y1")), 0);
    var line = Line.CreateBound(p0, p1);
    var grid = Grid.Create(doc, line);
    grid.Name = name;
    created.Add(new { id = grid.Id.Value, name = grid.Name });
}
return new { count = created.Count, grids = created };
```

#### Stage 3: Nhân Bản & Bố Trí Cột BTCT (Columns)
```csharp
double Ft(double mm) => UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);
var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().ToDictionary(l => l.Name, l => l, StringComparer.OrdinalIgnoreCase);

// 1. Resolve / Duplicate Column FamilySymbol 220x220
var colSymbols = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol))
    .OfCategory(BuiltInCategory.OST_StructuralColumns).Cast<FamilySymbol>().ToList();
if (colSymbols.Count == 0) throw new InvalidOperationException("No structural column family loaded.");

string typeName = "220x220mm";
var symbol = colSymbols.FirstOrDefault(s => string.Equals(s.Name, typeName, StringComparison.OrdinalIgnoreCase));
if (symbol == null)
{
    var baseSym = colSymbols.FirstOrDefault(s => s.IsActive) ?? colSymbols[0];
    symbol = (FamilySymbol)baseSym.Duplicate(typeName);
    void SetDim(string paramName, double valFt) {
        var p = symbol.LookupParameter(paramName);
        if (p != null && !p.IsReadOnly && p.StorageType == StorageType.Double) p.Set(valFt);
    }
    SetDim("b", Ft(220)); SetDim("h", Ft(220));
    SetDim("Width", Ft(220)); SetDim("Depth", Ft(220));
}
if (!symbol.IsActive) symbol.Activate();

// 2. Place Columns
var created = new List<object>();
foreach (var item in args.List("columns"))
{
    var baseLevel = levels[item.Require("baseLevel")];
    var topLevel = levels[item.Require("topLevel")];
    var pt = new XYZ(Ft(item.RequireDouble("x")), Ft(item.RequireDouble("y")), baseLevel.Elevation);
    var col = doc.Create.NewFamilyInstance(pt, symbol, baseLevel, Autodesk.Revit.DB.Structure.StructuralType.Column);
    col.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM)?.Set(topLevel.Id);
    col.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM)?.Set(Ft(item.Double("baseOffset", 0)));
    col.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM)?.Set(Ft(item.Double("topOffset", 0)));
    created.Add(new { id = col.Id.Value, x = item.Double("x"), y = item.Double("y") });
}
return new { count = created.Count, columns = created };
```

#### Stage 4 & 5: Nhân Bản WallType & Tạo Tường
```csharp
double Ft(double mm) => UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);
var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().ToDictionary(l => l.Name, l => l, StringComparer.OrdinalIgnoreCase);

// Resolve / Duplicate WallType
WallType GetOrCreateWallType(double thicknessMm)
{
    var types = new FilteredElementCollector(doc).OfClass(typeof(WallType)).Cast<WallType>().Where(t => t.Kind == WallKind.Basic).ToList();
    var match = types.FirstOrDefault(t => Math.Abs(UnitUtils.ConvertFromInternalUnits(t.Width, UnitTypeId.Millimeters) - thicknessMm) < 0.5);
    if (match != null) return match;

    var source = types.FirstOrDefault(t => t.GetCompoundStructure()?.LayerCount == 1) ?? types[0];
    var copy = (WallType)source.Duplicate($"Wall {thicknessMm:0}mm");
    var cs = copy.GetCompoundStructure();
    int core = cs.GetFirstCoreLayerIndex(); if (core < 0) core = 0;
    for (int i = 0; i < cs.LayerCount; i++) if (i != core) cs.SetLayerWidth(i, 0);
    cs.SetLayerWidth(core, Ft(thicknessMm));
    copy.SetCompoundStructure(cs);
    return copy;
}

var type110 = GetOrCreateWallType(110);
var type220 = GetOrCreateWallType(220);

var created = new List<object>();
foreach (var item in args.List("walls"))
{
    var level = levels[item.Require("level")];
    double thk = item.RequireDouble("thickness");
    var wt = Math.Abs(thk - 110) < 5 ? type110 : type220;
    var p0 = new XYZ(Ft(item.RequireDouble("x0")), Ft(item.RequireDouble("y0")), level.Elevation);
    var p1 = new XYZ(Ft(item.RequireDouble("x1")), Ft(item.RequireDouble("y1")), level.Elevation);
    double h = Ft(item.Double("height", 3650));
    var line = Line.CreateBound(p0, p1);
    var wall = Wall.Create(doc, line, wt.Id, level.Id, h, 0, false, false);
    created.Add(new { id = wall.Id.Value, thickness = thk, length = Math.Round(p0.DistanceTo(p1) * 304.8, 1) });
}
return new { count = created.Count, walls = created };
```

#### Stage 6: Tạo Cửa Hosted Trên Tường
```csharp
double Ft(double mm) => UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);
var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().ToDictionary(l => l.Name, l => l, StringComparer.OrdinalIgnoreCase);

var doorSymbols = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol))
    .OfCategory(BuiltInCategory.OST_Doors).Cast<FamilySymbol>().ToList();
var defaultDoor = doorSymbols.FirstOrDefault(s => s.IsActive) ?? doorSymbols.FirstOrDefault();
if (defaultDoor != null && !defaultDoor.IsActive) defaultDoor.Activate();

var created = new List<object>();
foreach (var item in args.List("openings"))
{
    long wallId = item.RequireLong("hostWallId");
    var hostWall = doc.GetElement(new ElementId(wallId)) as Wall;
    if (hostWall == null) continue;

    var level = levels[item.Require("level")];
    var pt = new XYZ(Ft(item.RequireDouble("x")), Ft(item.RequireDouble("y")), level.Elevation);
    
    // Project exactly onto wall curve
    if (hostWall.Location is LocationCurve lc)
    {
        var proj = lc.Curve.Project(pt);
        if (proj != null) pt = new XYZ(proj.XYZPoint.X, proj.XYZPoint.Y, pt.Z);
    }

    var inst = doc.Create.NewFamilyInstance(pt, defaultDoor, hostWall, level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
    created.Add(new { id = inst.Id.Value, wallId });
}
return new { count = created.Count, doors = created };
```

#### Stage 7: Tạo Phòng (Rooms) & Kiểm Tra Diện Tích Khép Kín
```csharp
double Ft(double mm) => UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);
var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().ToDictionary(l => l.Name, l => l, StringComparer.OrdinalIgnoreCase);

var created = new List<object>();
foreach (var item in args.List("rooms"))
{
    var level = levels[item.Require("level")];
    var uv = new UV(Ft(item.RequireDouble("x")), Ft(item.RequireDouble("y")));
    var room = doc.Create.NewRoom(level, uv);
    room.get_Parameter(BuiltInParameter.ROOM_NAME)?.Set(item.Require("name"));
    if (item.Has("number")) room.get_Parameter(BuiltInParameter.ROOM_NUMBER)?.Set(item.Str("number"));
    doc.Regenerate();

    double areaM2 = Math.Round(UnitUtils.ConvertFromInternalUnits(room.Area, UnitTypeId.SquareMeters), 2);
    bool enclosed = room.Area > 0;
    created.Add(new { id = room.Id.Value, name = room.Name, area = areaM2, enclosed });
}
return new { count = created.Count, rooms = created };
```

---

## 5. Verification Method (Phương Pháp Xác Thực Độc Lập)

Để kiểm chứng toàn bộ các kết luận và phát hiện trong báo cáo khảo sát này:

1. **Xác thực mã nguồn McpShared**:
   - Kiểm tra `PipeNaming.cs` tại `McpShared/HPRebar.Mcp.Contracts/PipeNaming.cs:13-15`: Xác nhận pipe prefix `hprebar-mcp-r2026`.
   - Kiểm tra `ScriptRunner.cs` tại `HPRebar/HPRebar.McpBridge/Service/ScriptRunner.cs:101-111`: Xác nhận logic `group.RollBack()` khi `request.DryRun == true`.
   - Kiểm tra `ScriptGlobals.cs` tại `HPRebar/HPRebar.McpBridge/Model/ScriptGlobals.cs:16-32`: Xác nhận danh sách biến toàn cục (`doc`, `uidoc`, `app`, `uiapp`, `ct`, `log`, `progress`, `args`).
2. **Kiểm tra Unit Test hiện hữu của McpShared**:
   Chạy lệnh kiểm thử sau trong terminal:
   ```powershell
   dotnet test "McpShared/HPRebar.Mcp.Server.Core.Tests"
   ```
   *Kết quả kỳ vọng*: Toàn bộ 793 xUnit tests chạy PASS 100%, kiểm chứng trọn vẹn Named Pipe transport, ScriptGuard AST analyzer, và JSON-RPC envelope.
3. **Kiểm tra biên dịch HPRebar Revit 2026 MCP**:
   ```powershell
   dotnet test "HPRebar/HPRebar.Mcp.Server.Tests"
   ```
   *Kết quả kỳ vọng*: Toàn bộ 118 xUnit tests chạy PASS 100%, xác nhận 12 embedded seeds (gồm `create_level`, `create_grid`, `create_point_based_element`, `create_surface_based_element`, `create_room`, `create_line_based_element`) biên dịch sạch và hợp chuẩn Revit API 2026.
4. **Xác thực dữ liệu NhaPho 5x18m Ground-Truth**:
   Mở và đối chiếu dữ liệu hình học trong `vn-autocad-skill-main\examples\nha_pho_5x18m\layout.py` (5 Levels, 7 Grids, 10 Cột 220x220, Tường 110/220, Cửa D1-D6/S1-S3, Các phòng Tầng 1-3).
