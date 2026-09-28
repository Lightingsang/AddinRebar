# Báo cáo Điều tra Kỹ thuật: Revit Rebar Creation, Idempotency & UI Architecture cho KataRebar

**Agent:** `explorer_survey_3` (Revit Rebar Creation & UI Explorer)  
**Mã nhiệm vụ:** Survey Revit Rebar Creation, BarType Lookup, Idempotency, TransactionGroup & WPF MVVM UI  
**Ngày:** 2026-09-27  
**Phạm vi:** `HPRebar/BeamRebar/`, `HPRebar/KataExport/`, `HPRebar/FoundationRebar/`, `HPRebar/Application.cs`, `HPRebar/Resources/Themes/`

---

## Tóm tắt Điều hành (Executive Summary)

1. **Revit Rebar Creation**: Sử dụng kết hợp hai API chuẩn của Revit:
   - `Rebar.CreateFromCurves`: Dành cho thép dọc liên tục (Main Bars), thép tăng cường gối/nhịp (Additional Bars), thép mang/cốt giá (Side/Skin Bars) và đai C (Cross-Ties). Đường curve được tính toán dưới dạng `Polyline3` (đơn vị mm), đơn giản hóa qua `Simplify(1.0)` nhằm chống lỗi `Application.ShortCurveTolerance` (~0.78 mm), sau đó chuyển đổi sang Revit internal units (feet). Chặn cảnh báo lỗi thời qua `#pragma warning disable CS0618` để tương thích đa phiên bản Revit 2023–2026.
   - `Rebar.CreateFromRebarShape`: Dành cho cốt đai chính (Stirrups) qua hình dạng đai hộp chữ nhật chuẩn (`M_T1` hoặc `T1`), kết hợp `RebarShapeDrivenAccessor` (`ScaleToBox`, `SetLayoutAsNumberWithSpacing`) để tạo cụm đai có tham số phân bố rải đều hoặc 3 vùng.
2. **RebarBarType & Hook Resolution**:
   - Truy vấn toàn bộ `RebarBarType` trong Document qua `FilteredElementCollector`, trích xuất đường kính danh định bằng `BuiltInParameter.REBAR_BAR_DIAMETER` chuyển sang mm.
   - Thuật toán khớp đường kính thông minh: Khớp tên chính xác $\rightarrow$ Khớp đường kính $\pm 0.5\text{ mm}$ kết hợp phân loại `BarModelType` (chọn `Deformed` cho thép chủ $\ge 12\text{ mm}$, `Plain` hoặc `Deformed` cho thép đai $\le 10\text{ mm}$) $\rightarrow$ Dự phòng khớp đường kính gần nhất kèm hiển thị cảnh báo trên UI Preview.
   - `RebarHookType`: Khớp theo góc uốn (90° = $\pi/2$ rad cho Standard Hook, 135° = $3\pi/4$ rad cho Stirrup/Tie Hook).
3. **Tính Bất biến & Tự làm sạch (Idempotency)**:
   - Gắn nhãn định danh cho toàn bộ thanh thép sinh ra qua tham số `ALL_MODEL_INSTANCE_COMMENTS`: `$"HPRebar_Kata_{BeamName}"` và `NUMBER_PARTITION_PARAM`: `$"Kata_{BeamName}"`.
   - Cơ chế tự dọn dẹp: Trước khi tạo thép mới cho dải dầm, dùng `FilteredElementCollector` tìm tất cả `Rebar` có `GetHostId()` thuộc các dầm được chọn và có `Comments` trùng khớp `$"HPRebar_Kata_{BeamName}"`, thực hiện `document.Delete(rebarIds)` trong sub-transaction đầu tiên.
4. **Quản lý Transaction**:
   - Sử dụng `TransactionGroup("Kata Rebar - {BeamName}")` bao bọc 5 sub-transactions riêng biệt (Xoá thép cũ $\rightarrow$ Tạo đai $\rightarrow$ Tạo thép dọc chính $\rightarrow$ Tạo thép tăng cường $\rightarrow$ Tạo thép mang/cốt giá).
   - Gọi `group.Assimilate()` khi thành công để gộp toàn bộ thành đúng **1 bước Undo duy nhất** trên thanh công cụ Revit. Nếu có bất kỳ ngoại lệ nào phát sinh, gọi `group.RollBack()` để khôi phục trạng thái nguyên vẹn ban đầu.
   - Mỗi sub-transaction đều kích hoạt `RebarFailureHandling.Apply(t)` với `IFailuresPreprocessor` nhằm nuốt các cảnh báo không nghiêm trọng của Revit (như rebar nằm mấp mé biên host hoặc giao cắt nhẹ), tránh bật popup làm gián đoạn người dùng.
5. **UI MVVM & Tích hợp Ribbon**:
   - Đăng ký nút bấm `"Kata Rebar"` trên panel `"Rebar"` trong `HPRebar/Application.cs` với icon vector nét căng độc lập DPI viết bằng C# trong `RibbonIcons.cs`.
   - Cửa sổ WPF MVVM hoạt động ở chế độ **Modeless** kết hợp `ExternalEvent` và `IExternalEventHandler`, giúp người dùng vừa xem preview cấu hình thép vừa có thể xoay lật mô hình 3D, chọn lại dầm hoặc xem file Excel.
   - Hệ thống giao diện đồng bộ hoàn toàn với palette màu của Revit qua `MaterialThemeBridge.Attach()`, hỗ trợ tự động đổi theme Dark/Light (Revit 2024+) và thư viện MaterialDesignThemes 5.3.2.

---

## 1. Cơ chế Tạo Rebar trong Revit 2026 (.NET 8)

### 1.1. Tạo Thép Dọc bằng `Rebar.CreateFromCurves`

Trong `HPRebar/BeamRebar/Service/BeamMainBarCreator.cs` (dòng 72–86) và `HPRebar/FoundationRebar/Service/FoundationRebarCreationService.cs` (dòng 54–68), phương thức `Rebar.CreateFromCurves` được triển khai chuẩn xác như sau:

```csharp
#pragma warning disable CS0618 // Multi-version: Rebar.CreateFromCurves / RebarHookOrientation deprecated in Revit 2026, required for Revit 2023-2025 compatibility
var rebar = Rebar.CreateFromCurves(
    document,
    RebarStyle.Standard,
    defaultBarType,
    startHook: null,
    endHook: null,
    host: hostElement,
    norm: stack.NormalDirection,
    curves: curves,
    startHookOrient: RebarHookOrientation.Right,
    endHookOrient: RebarHookOrientation.Right,
    useExistingShapeIfPossible: true,
    createNewShape: true);
#pragma warning restore CS0618
```

#### Phân tích chi tiết các tham số:
| Tham số | Kiểu dữ liệu | Giá trị / Ý nghĩa cho KataRebar |
|---|---|---|
| `document` | `Document` | Active Revit Document hiện hành. |
| `style` | `RebarStyle` | `RebarStyle.Standard` (thép chịu lực dọc, thép tăng cường, thép giá) hoặc `RebarStyle.StirrupTie` (thép đai C). |
| `barType` | `RebarBarType` | Kiểu thanh thép đã được resolve dựa trên đường kính $D$ (ví dụ D18, D20, D25). |
| `startHook`, `endHook` | `RebarHookType?` | Đặt là `null` khi móc neo 90° đã được dựng trực tiếp vào tập đường sinh `curves`. |
| `host` | `Element` | Đối tượng hình học chứa thép (thể hiện qua phần tử dầm `FamilyInstance`). Khi dải dầm có nhiều nhịp, thanh thép liên tục sẽ được gán host là dầm đầu tiên hoặc dầm chứa điểm bắt đầu của thanh. |
| `norm` | `XYZ` | Vector pháp tuyến của mặt phẳng chứa thanh thép. Với dầm chạy dọc trục X, mặt phẳng uốn là X-Z $\rightarrow$ Vector pháp tuyến là vector ngang `NormalDirection` (trục Y dầm). Với đai C hoặc thép đai, pháp tuyến là `BeamDirection` (trục X). |
| `curves` | `IList<Curve>` | Danh sách các đoạn `Line.CreateBound(p0, p1)` liên tục mô tả tim thanh thép (chuyển đổi từ tọa độ mm sang feet). |
| `startHookOrient`, `endHookOrient` | `RebarHookOrientation` | Mặc định `RebarHookOrientation.Right`. |
| `useExistingShapeIfPossible` | `bool` | `true`: Tìm và gán RebarShape có sẵn trong Project nếu hình học thanh thép trùng khớp. |
| `createNewShape` | `bool` | `true`: Nếu không có RebarShape nào khớp, Revit tự động tạo Shape mới thay vì ném ngoại lệ. |

#### Chú ý quan trọng về Hình học (Geometric Guardrails):
- **Ngưỡng dung sai đoạn ngắn (`Application.ShortCurveTolerance`)**: Trong Revit API, bất kỳ curve nào có chiều dài ngắn hơn ~0.78 mm (1/32 inch) sẽ làm `CreateFromCurves` văng lỗi ngoại lệ.
- **Giải pháp**: Tất cả các polyline tính toán trong `HPRebar.Core` đều bắt buộc chạy qua hàm `polyline.Simplify(1.0)` (loại bỏ các điểm cách nhau dưới 1.0 mm và các điểm thẳng hàng) trước khi chuyển đổi sang `Line.CreateBound` trong Revit.
- **Đơn vị đo lường**: Tọa độ đầu vào tính toán trong domain `HPRebar.Core` luôn là **milimet (mm)**. Khi đưa vào Revit API, phải chia cho $304.8$ qua `RevitUnits.MmToFt(val)`.

---

### 1.2. Tạo Cốt Đai bằng `Rebar.CreateFromRebarShape`

Trong `HPRebar/BeamRebar/Service/BeamStirrupCreator.cs` (dòng 101–114), cốt đai chữ nhật khép kín được tạo thông qua `RebarShape`:

```csharp
var shape = shapes.MainStirrup(); // Tìm shape M_T1 hoặc T1
var rebar = Rebar.CreateFromRebarShape(doc, shape, barType, host, originXyz, xVec, yVec);
var accessor = rebar.GetShapeDrivenAccessor();

accessor.ScaleToBox(originXyz, xVec * RevitUnits.MmToFt(widthMm), yVec * RevitUnits.MmToFt(heightMm));
if (run.Count == 1)
{
    accessor.SetLayoutAsSingle();
}
else
{
    accessor.SetLayoutAsNumberWithSpacing(
        Math.Clamp(run.Count, 2, 1002), RevitUnits.MmToFt(run.Spacing), true, true, true);
}
```

#### Ưu điểm vượt trội của phương pháp này:
1. Đai sinh ra là đối tượng **Shape-driven Rebar Set**, giữ nguyên các tham số bẻ móc 135° tiêu chuẩn chống động đất của Family `RebarShape`.
2. Hỗ trợ rải cụm đai (`SetLayoutAsNumberWithSpacing`) với số lượng và bước rải xác định chỉ bằng 1 element Rebar duy nhất thay vì sinh hàng chục thanh đơn lẻ, giúp mô hình nhẹ hơn gấp 10 lần.
3. Kích thước đai tự động co giãn chính xác theo bề rộng và chiều cao tiết diện dầm trừ lớp bê tông bảo vệ qua `ScaleToBox`.

---

## 2. Truy vấn & Khớp RebarBarType và RebarHookType

### 2.1. Tra cứu RebarBarType trong Document

Trong `HPRebar/BeamRebar/Service/RebarTypeCatalog.cs`:

```csharp
public static IReadOnlyList<RebarTypeInfo> LoadBarTypes(Document doc)
{
    return new FilteredElementCollector(doc)
        .OfClass(typeof(RebarBarType))
        .Cast<RebarBarType>()
        .Select(b => new RebarTypeInfo
        {
            Name = b.Name,
            DiameterMm = RevitUnits.FtToMm(b.get_Parameter(BuiltInParameter.REBAR_BAR_DIAMETER).AsDouble()),
            BarType = b
        })
        .OrderBy(b => b.DiameterMm)
        .ToList();
}
```

### 2.2. Thuật toán Khớp Đường kính Thông minh cho KataRebar

Khi đọc dữ liệu từ sheet `Dam` của `Kata.xlsm`:
- Thép dọc: `2f18`, `3f20`, `2f22`, `4f25` $\rightarrow$ Trích xuất đường kính $D = 18, 20, 22, 25\text{ mm}$.
- Thép đai: `d8`, `f8`, `f10`, `a150`, `a200` $\rightarrow$ Trích xuất đường kính $D = 8, 10\text{ mm}$.
- Thép giá: `2f12`, `2f14` $\rightarrow$ Trích xuất đường kính $D = 12, 14\text{ mm}$.

Do các template Revit tại Việt Nam có nhiều cách đặt tên khác nhau (`D18`, `f18`, `phi 18`, `T18`, `18M`, `CB400-V D18`), thuật toán khớp 3 cấp độ đề xuất như sau:

```csharp
public RebarTypeInfo? ResolveBarType(string rawNotation, double targetDiameterMm, bool isLongitudinal)
{
    // Bước 1: Khớp chính xác theo tên (Name Match)
    var exactName = _barTypes.FirstOrDefault(b => 
        b.Name.Equals(rawNotation, StringComparison.OrdinalIgnoreCase) ||
        b.Name.Equals($"D{targetDiameterMm:0}", StringComparison.OrdinalIgnoreCase) ||
        b.Name.Equals($"f{targetDiameterMm:0}", StringComparison.OrdinalIgnoreCase));
    if (exactName is not null) return exactName;

    // Bước 2: Khớp đường kính số trong dung sai ± 0.5 mm
    var candidates = _barTypes
        .Where(b => Math.Abs(b.DiameterMm - targetDiameterMm) <= 0.5)
        .ToList();

    if (candidates.Count == 1) return candidates[0];

    if (candidates.Count > 1)
    {
        // Ưu tiên theo BarModelType: Thép dọc dùng Deformed, Thép đai nhỏ có thể dùng Plain hoặc Deformed
        if (isLongitudinal)
        {
            var deformed = candidates.FirstOrDefault(b => b.BarType.BarModelType == BarModelType.Deformed);
            if (deformed is not null) return deformed;
        }

        // Ưu tiên tên chứa chữ số đường kính
        var nameMatch = candidates.FirstOrDefault(b => b.Name.Contains(targetDiameterMm.ToString("0")));
        if (nameMatch is not null) return nameMatch;

        return candidates[0];
    }

    // Bước 3: Dự phòng đường kính gần nhất và đánh dấu cảnh báo để người dùng chọn trên UI
    return _barTypes.OrderBy(b => Math.Abs(b.DiameterMm - targetDiameterMm)).FirstOrDefault();
}
```

### 2.3. Khớp RebarHookType

```csharp
public RebarHookType? FindHook(int angleDegrees, RebarStyle style = RebarStyle.Standard)
{
    double targetAngleRad = angleDegrees * Math.PI / 180.0;
    return _hookTypes.FirstOrDefault(h =>
        h.Style == style &&
        (Math.Abs(h.HookAngle - targetAngleRad) < 1e-2 || h.Name.Contains(angleDegrees.ToString())));
}
```
- Móc 90°: `angleDegrees = 90`, `RebarStyle.Standard`.
- Móc đai 135°: `angleDegrees = 135`, `RebarStyle.StirrupTie`.

---

## 3. Cơ chế Đảm bảo Tính Bất biến (Idempotency) & Làm sạch

### 3.1. Nhu cầu Thực tế
Người dùng thường xuyên xuất hình học sang Kata, tinh chỉnh thép trong Excel (thay đổi số lượng thanh, bước đai), sau đó nhấn tạo lại trong Revit. Nếu không có cơ chế nhận diện và dọn dẹp, các thanh thép mới sẽ đè chồng lên thép cũ, làm sai lệch bảng thống kê cốt thép và xung đột mô hình.

### 3.2. Quy ước Gắn Nhãn Định danh (Tagging Contract)
Khi tạo mới bất kỳ thanh Rebar nào, gán 2 tham số:
1. `BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS`:  
   Giá trị: `$"HPRebar_Kata_{beamName}"` (ví dụ: `HPRebar_Kata_D1`).
2. `BuiltInParameter.NUMBER_PARTITION_PARAM`:  
   Giá trị: `"KataRebar"` hoặc `$"Kata_{beamName}"`.

### 3.3. Thuật toán Quét & Xoá Thép Cũ

```csharp
public static class KataRebarCleanupService
{
    public static IReadOnlyList<ElementId> FindExistingKataRebars(
        Document doc,
        IReadOnlyList<Element> hostBeams,
        string beamName)
    {
        var hostIds = new HashSet<ElementId>(hostBeams.Select(b => b.Id));
        var expectedTag = $"HPRebar_Kata_{beamName}";

        return new FilteredElementCollector(doc)
            .OfClass(typeof(Rebar))
            .WhereElementIsNotElementType()
            .Cast<Rebar>()
            .Where(r =>
            {
                // Kiểm tra rebar có thuộc host beam được chọn hay không
                if (!hostIds.Contains(r.GetHostId())) return false;

                // Kiểm tra tham số Comments
                var comment = r.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.AsString();
                return string.Equals(comment, expectedTag, StringComparison.OrdinalIgnoreCase);
            })
            .Select(r => r.Id)
            .ToList();
    }
}
```

Quá trình xoá thép cũ được thực hiện ở sub-transaction đầu tiên. Nếu các bước tạo thép sau đó thất bại, toàn bộ `TransactionGroup` sẽ Rollback và các thanh thép cũ được phục hồi nguyên vẹn.

---

## 4. Quản trị Giao dịch (Transaction Architecture)

Sử dụng mô hình **Master TransactionGroup + Phased Sub-Transactions + Failure Handling**:

```
TransactionGroup: "Kata Rebar - {BeamName}"
│
├── Sub-Transaction 1: "Delete Prior Kata Rebar" (Xoá các thanh thép cũ có tag trùng khớp)
├── Sub-Transaction 2: "Create Kata Stirrups" (Tạo các cụm đai gối và nhịp)
├── Sub-Transaction 3: "Create Kata Main Bars" (Tạo các thanh thép dọc trên & dưới)
├── Sub-Transaction 4: "Create Kata Additional Bars" (Tạo thép gia cường gối L/4, L/3 và nhịp)
└── Sub-Transaction 5: "Create Kata Side Bars" (Tạo thép giá, thép chống phình, đai C)
│
└── group.Assimilate() (Gộp thành 1 Undo record duy nhất trên Revit Ribbon)
    hoặc group.RollBack() (Khôi phục nguyên trạng nếu xảy ra lỗi)
```

### 4.1. Nuốt Cảnh báo Không Nghiêm trọng (`IFailuresPreprocessor`)
Trong `HPRebar/BeamRebar/Service/RebarFailureHandling.cs`:
```csharp
public static void Apply(Transaction transaction)
{
    var options = transaction.GetFailureHandlingOptions();
    options = options.SetFailuresPreprocessor(new SwallowWarnings());
    options = options.SetClearAfterRollback(true);
    transaction.SetFailureHandlingOptions(options);
}
```
Lớp `SwallowWarnings` tự động kiểm tra `FailureSeverity.Warning` và gọi `DeleteWarning()`, ngăn không cho Revit dừng tiến trình để hiện các popup cảnh báo hình học nhỏ.

---

## 5. Tích hợp UI MVVM & Ribbon

### 5.1. Đăng ký Ribbon Button trong `Application.cs`

Trong `HPRebar/Application.cs`:
```csharp
private void CreateRibbon()
{
    var rebarPanel = Application.CreatePanel("Rebar", "HPRebar");
    Track(rebarPanel.AddPushButton<ColumnRebarCommand>("Column Rebar"), icons => icons.ColumnRebar);
    Track(rebarPanel.AddPushButton<BeamRebarCommand>("Beam Rebar"), icons => icons.BeamRebar);
    Track(rebarPanel.AddPushButton<FoundationRebarCommand>("Foundation Rebar"), icons => icons.FoundationRebar);
    Track(rebarPanel.AddPushButton<KataExportCommand>("Kata Export"), icons => icons.KataExport);
    Track(rebarPanel.AddPushButton<KataRebarCommand>("Kata Rebar"), icons => icons.KataRebar); // <--- Tích hợp mới

    ApplyIcons();
    ...
}
```

### 5.2. Biểu tượng Vector trong `RibbonIcons.cs`

Tạo icon vector `KataRebar` độc lập DPI bằng mã C#, tự thích ứng nền sáng/tối:
```csharp
// Kata Rebar: Dầm bê tông + cốt đai + thanh thép chủ và mũi tên tạo thép
KataRebar = Glyph(
    (ink, "M2,4 H30 V28 H2 Z M4,6 H28 V26 H4 Z"),
    (Steel, "M6,8 H26 V10 H6 Z M6,22 H26 V24 H6 Z"),
    (Steel, "M8,10 H10 V22 H8 Z M14,10 H16 V22 H14 Z M20,10 H22 V22 H20 Z M26,10 H28 V22 H26 Z"),
    (Steel, "M12,14 L18,14 L18,12 L22,16 L18,20 L18,18 L12,18 Z"));
```

### 5.3. Kiến trúc Cửa sổ Modeless & ExternalEvent

Kế thừa mẫu kiến trúc từ `KataExportCommand.cs` và `BeamRebarCommand.cs`:
1. `KataRebarCommand`:
   - Kiểm tra `_window`: nếu đang mở thì gọi `_window.Activate()` để tránh mở nhiều cửa sổ.
   - Nhận diện dầm chọn trước hoặc nhắc người dùng chọn qua `PickObjects`.
   - Đọc dữ liệu từ file Excel đang mở qua COM (`ExcelComAttach`) hoặc ClosedXML fallback.
   - Khởi tạo `KataRebarExternalEventHandler`, `KataRebarViewModel`, `KataRebarView`.
   - Gán `WindowInteropHelper(view).Owner = Application.MainWindowHandle`.
   - Mở giao diện bằng `view.Show()`.
2. `KataRebarExternalEventHandler`:
   - Hiện thực `IExternalEventHandler` và `IKataRebarRunner`.
   - Dùng `ConcurrentQueue<KataRebarRequest>` kèm `TaskCompletionSource` để các lệnh async trên UI có thể gửi yêu cầu tạo thép hoặc chọn lại dầm (`RepickAsync`) vào Revit main thread an toàn.

### 5.4. Giao diện Động Đồng bộ Theme (ThemeDark/ThemeLight & MaterialDesign 5.3.2)
- Trong XAML:
  ```xml
  Background="{DynamicResource Brush.Background}"
  Foreground="{DynamicResource Brush.Foreground.Primary}"
  FontFamily="{DynamicResource Font.Family.Default}"
  FontSize="{DynamicResource Font.Size.Body}"
  ```
- Nạp tài nguyên:
  ```xml
  <ResourceDictionary Source="pack://application:,,,/HPRebar;component/Resources/Themes/Theme.xaml"/>
  ```
- Trong Code-behind:
  ```csharp
  MaterialThemeBridge.Attach(this, RevitHostTheme.Instance, dark => new RibbonIcons(dark).KataRebar);
  ```

---

## 6. Đề xuất Kiến trúc Toàn diện cho `KataRebar`

Cấu trúc cây thư mục tuân thủ tuyệt đối quy ước feature-folder và tính độc lập của `HPRebar.Core`:

```
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\
├── HPRebar.Core/
│   └── KataRebar/
│       ├── Models/
│       │   ├── KataBeamRebarSpec.cs            # DTO đầy đủ thông số thép dầm đọc từ Excel
│       │   ├── KataMainBarSpec.cs              # Thép dọc liên tục trên/dưới (count, diameter, lap)
│       │   ├── KataSupportTopBarSpec.cs        # Thép gia cường gối (lớp 1, lớp 2, chiều dài cắt)
│       │   ├── KataSpanBottomBarSpec.cs        # Thép gia cường nhịp (lớp 1, lớp 2, vị trí cắt)
│       │   ├── KataStirrupSpec.cs              # Thép đai (đường kính, bước gối a1, bước nhịp a2)
│       │   ├── KataSideBarSpec.cs              # Thép giá / chống phình và đai C
│       │   └── KataRebarGeometryResult.cs      # Kết quả hình học 3D (Polyline3 & StirrupRuns)
│       └── Calculators/
│           ├── KataDamSheetParser.cs           # Parser bóc tách dữ liệu ô sheet Dam (B3:B12, rows 13-27)
│           ├── KataBarNotationParser.cs        # Parser chuỗi ký hiệu thép ("2f18", "3f20+2f18", "a150")
│           └── KataRebarCalculator.cs          # Thuật toán tính toán 3D polylines, neo, nối và phân bố đai
│
├── HPRebar.Core.Tests/
│   └── KataRebar/
│       ├── KataDamSheetParserTests.cs          # Kiểm thử xUnit parser dữ liệu sheet Dam
│       ├── KataBarNotationParserTests.cs       # Kiểm thử parser chuỗi thép ("2f18", "a150")
│       └── KataRebarCalculatorTests.cs         # Kiểm thử thuật toán hình học thanh thép và đai
│
└── HPRebar/
    └── KataRebar/
        ├── KataRebarCommand.cs                 # ExternalCommand entry point [Transaction(TransactionMode.Manual)]
        ├── KataRebarExternalEventHandler.cs    # IExternalEventHandler + IKataRebarRunner
        ├── KataRebarRequest.cs                 # DTO yêu cầu giao tiếp giữa UI và Revit API
        ├── KataRebarSelectionFilter.cs         # Bộ lọc chọn dầm OST_StructuralFraming
        ├── Model/
        │   ├── KataRebarSession.cs             # Session chứa dầm, Document, Spec, Catalog
        │   ├── KataBarTypeMappingItem.cs       # DTO map đường kính -> RebarBarType hiển thị trên UI
        │   └── CreatedKataRebar.cs             # DTO lưu danh sách Rebar elements đã sinh
        ├── Service/
        │   ├── ExcelDamReader.cs               # Đọc sheet Dam từ Excel đang mở (COM) hoặc ClosedXML
        │   ├── KataRebarOrchestrator.cs        # Quản lý TransactionGroup("Kata Rebar - {BeamName}")
        │   ├── KataRebarCreationService.cs     # Điều phối tạo đai, thép chủ, thép tăng cường
        │   ├── KataStirrupCreator.cs           # Tạo Rebar đai qua CreateFromRebarShape
        │   ├── KataMainBarCreator.cs           # Tạo thép chủ trên/dưới qua CreateFromCurves
        │   ├── KataAdditionalBarCreator.cs     # Tạo thép gia cường gối/nhịp qua CreateFromCurves
        │   ├── KataSideBarCreator.cs           # Tạo thép mang và đai C qua CreateFromCurves
        │   ├── KataRebarCleanupService.cs      # Nhận diện & xoá sạch thép Kata cũ (Idempotency)
        │   ├── KataRebarTypeResolver.cs        # Khớp đường kính mm sang RebarBarType & RebarHookType
        │   ├── RebarFailureHandling.cs         # Nuốt warning Revit qua IFailuresPreprocessor
        │   └── RevitUnits.cs                   # Tiện ích đổi mm <-> feet
        ├── View/
        │   ├── KataRebarView.xaml              # Modeless WPF Dialog (ThemeDark/ThemeLight, MaterialDesign)
        │   └── KataRebarView.xaml.cs           # Code-behind kết nối MaterialThemeBridge
        └── ViewModel/
            ├── IKataRebarRunner.cs             # Interface runner cho ViewModel
            └── KataRebarViewModel.cs           # CommunityToolkit.Mvvm ObservableObject, RelayCommands
```

---

## 7. Ma trận Đối chiếu với Yêu cầu Nghiệp vụ (Requirement Verification Matrix)

| Yêu cầu từ Authoritative Request (R3, R4) | Giải pháp Kiến trúc Đề xuất | Trạng thái Khả thi |
|---|---|:---:|
| **Rebar Creation** (`CreateFromCurves` / `CreateFromRebarShape`) | Dùng `CreateFromRebarShape` cho cụm đai (Shape-driven set) và `CreateFromCurves` (kèm `#pragma CS0618` + `Simplify(1.0)`) cho thép dọc. | ✅ 100% Khả thi |
| **Khớp RebarBarType & HookType** | Truy vấn qua `FilteredElementCollector`, khớp đường kính $\pm 0.5\text{ mm}$, phân loại `BarModelType` (Deformed/Plain), hỗ trợ dropdown trên UI cho phép người dùng ghi đè thủ công. | ✅ 100% Khả thi |
| **Tính Bất biến (Idempotency)** | Gắn tag `ALL_MODEL_INSTANCE_COMMENTS = "HPRebar_Kata_{BeamName}"`. Quét và xoá toàn bộ rebar cũ của dầm trước khi tạo thép mới. | ✅ 100% Khả thi |
| **Quản trị Transaction** | Dùng `TransactionGroup.Assimilate()` tạo đúng 1 Undo duy nhất; RollBack tự động khi lỗi; nuốt warning qua `IFailuresPreprocessor`. | ✅ 100% Khả thi |
| **UI & Ribbon Integration** | Modeless WPF Window kết hợp `ExternalEvent`, đồng bộ theme tự động qua `MaterialThemeBridge`, icon vector crisp chuẩn DPI trong `RibbonIcons.cs`. | ✅ 100% Khả thi |

---
*Báo cáo được hoàn thành bởi `explorer_survey_3` và sẵn sàng bàn giao cho Orchestrator để tiến hành lập kế hoạch chi tiết.*
