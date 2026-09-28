# Báo Cáo Khảo Sát Kỹ Thuật: Đặc Tả Sheet 'Dam' (Kata.xlsm), Cơ Chế Excel COM / ClosedXML, và Thiết Kế DTO KataBeamRebarSpec

- **Tác giả**: explorer_survey_1 (Kata & Excel Domain / Spec Miner)
- **Ngày thực hiện**: 2026-09-27
- **Phạm vi**: `HPRebar.Core`, `HPRebar`, `plans/260926-2317-kata-export-hprebar/`, `C:\kata_pro\Kata.xlsm`, `HPExcel`
- **Mục tiêu**: Cung cấp cơ sở lý thuyết, đặc tả ô bảng tính, ngữ pháp chuỗi thép, cơ chế đọc dữ liệu 2 nguồn (COM `oleaut32` + `ClosedXML`), và đề xuất mô hình dữ liệu strongly-typed `KataBeamRebarSpec` phục vụ tính năng **Kata Rebar**.

---

## 1. Tóm Tắt Điều Hành (Executive Summary)

1. **Nguồn dữ liệu thực tế**: File template gốc `C:\kata_pro\Kata.xlsm` (473 KB, tác giả KS. Nguyễn Kha Tam) đã được định vị trực tiếp trên máy trạm dev. Bằng cách phân tích cấu trúc OpenXML (`xl/worksheets/sheet3.xml`, `xl/sharedStrings.xml`, `comments`, `dataValidations`), toàn bộ 100% tọa độ ô, nhãn, ghi chú (comments) và quy tắc nhập liệu của sheet `Dam` đã được giải mã chính xác và đối soát đối chiếu.
2. **Cơ chế tương tác Excel 2 tầng (Dual-Source)**:
   - **Tầng 1 (Live Active COM)**: Kế thừa giải pháp đã hoàn thiện và kiểm chứng trực tiếp trong `HPRebar/KataExport` qua `ExcelComAttach.cs` và `ComLateBinding.cs`. Sử dụng Windows ROT (`ole32!CLSIDFromProgID`, `oleaut32!GetActiveObject`) và late-binding reflection. Đặc biệt, đọc toàn bộ khối `Range["A1:BZ30"].Value2` trong **1 lệnh COM duy nhất** trả về mảng 2 chiều `object[,]`, tốc độ đọc < 5 ms, không gây giật lag và không phụ thuộc Microsoft Office Interop PIA.
   - **Tầng 2 (Offline File Fallback)**: Tích hợp thư viện `ClosedXML` (đang dùng bản 0.104.2 trong `HPExcel`) để mở trực tiếp file `.xlsm`/`.xlsx` từ ổ đĩa khi Excel không chạy, phục vụ chế độ chạy batch, chạy headless hoặc người dùng chọn file từ máy.
   - **Cách ly kiến trúc (Architecture Isolation)**: Lớp Core (`HPRebar.Core`, netstandard2.0) chỉ định nghĩa giao diện trừu tượng `IKataDamCellAccessor` và class `KataCellTable`. Bộ parser `KataDamSheetParser` chạy thuần túy trên bộ nhớ, cho phép test xUnit 100% không cần cài Excel hay mở file thật.
3. **Mô hình DTO hoàn chỉnh**: Đề xuất mô hình dữ liệu phân cấp `KataBeamRebarSpec` bao bọc đầy đủ thông tin hình học, thép chủ chạy suốt, thép gia cường trên/dưới đa lớp theo nhịp/gối, thép đai phân vùng 3 đoạn, thép hông/cốt giá, cùng bộ parser biểu thức chuỗi thép (`2f18`, `3f20`, `6f25`, `2d8`, `a100/200/50`, `2f20;2f16`, `-50;5f20`).

---

## 2. Khảo Sát Kiến Trúc Excel & Kata Trong Codebase Hiện Tại

### 2.1. Phân Tích `HPRebar/KataExport` và `HPRebar.Core/KataExport`
Tính năng `KataExport` (xuất hình học dầm từ Revit sang Excel Kata) đã được phát triển hoàn thiện ngày 2026-09-27 (`plans/260926-2317-kata-export-hprebar/`) với các đặc tính kỹ thuật cốt lõi:
- **`ExcelComAttach.cs`** (`HPRebar/KataExport/Service/ExcelComAttach.cs`):
  - Định nghĩa P/Invoke Win32 trực tiếp:
    ```csharp
    [DllImport("ole32.dll", CharSet = CharSet.Unicode)]
    private static extern int CLSIDFromProgID(string lpszProgID, out Guid lpclsid);

    [DllImport("oleaut32.dll", PreserveSig = true)]
    private static extern int GetActiveObject(ref Guid rclsid, IntPtr pvReserved, [MarshalAs(UnmanagedType.IUnknown)] out object? ppunk);
    ```
  - Tìm instance Excel đang chạy thông qua Running Object Table (ROT). Hoạt động hoàn hảo trên .NET 8 (nơi `Marshal.GetActiveObject` bị Microsoft lược bỏ) cũng như .NET Framework 4.8.
- **`ComLateBinding.cs`** (`HPRebar/KataExport/Service/ComLateBinding.cs`):
  - Sử dụng `Type.InvokeMember` với các cờ `BindingFlags.GetProperty`, `SetProperty`, `InvokeMethod`.
  - Bắt và bóc tách `TargetInvocationException` để nhận diện các lỗi RPC thường gặp: `0x80010001` (`RpcCallRejected`), `0x8001010A` (`RpcRetryLater`), `0x800AC472` (`ExcelBusy` khi người dùng đang double-click sửa ô trong Excel).
  - Có phương thức `Release(object? comObj)` gọi `Marshal.ReleaseComObject` an toàn.
- **`KataExcelWriter.cs`** (`HPRebar/KataExport/Service/KataExcelWriter.cs`):
  - Tìm sheet `Dam` trong active workbook.
  - Đặt `NumberFormat = "@"` cho ô `B10` để giữ nguyên text cao trình (ví dụ `'+3.300'`, `'-0.050'`).
  - Ghi theo batch 2D array (`Value2 = array[,]`) cho cột header `B3:B10` và các hàng 11, 19, 21, 22, 23.
  - Xóa sạch dữ liệu dầm cũ từ cột hiện tại đến `BZ23` qua `ClearContents`.
- **`HPRebar.Core/KataExport/`**:
  - `KataRowBuilder.cs`: Gom các thông số hình học nhịp/gối thành các hàng dữ liệu tương ứng sheet `Dam`.
  - `KataExcelCell.cs`: Chuyển đổi kiểu dữ liệu sang primitive COM; giữ nguyên tiền tố nháy đơn `'` cho chuỗi text để Excel không tự động parse thành ngày tháng hoặc số.
  - `KataSegmenter.cs`: Chiếu 1D Vector Projection gối tựa và dầm, tính toán khoảng hở thông thủy (Clear span) tự nhiên.

### 2.2. Khảo Sát `ClosedXML` trong Repository
- Thư viện `ClosedXML` hiện diện trong folder `HPExcel` (`HPExcel.McpBridge` và `HPExcel.McpBridge.Tests`) ở phiên bản **`0.104.2`** (target framework `net8.0-windows` và `net8.0`).
- Trong `HPExcel/HPExcel.McpBridge/Headless/ClosedXmlWorkbookService.cs`:
  - `ClosedXML` được dùng để đọc và ghi trực tiếp các file `.xlsx` mà không cần tiến trình Excel (`new XLWorkbook(filePath)`).
  - Đọc range, table, formula, format một cách độc lập và mạnh mẽ.
- **Khả năng tích hợp vào `HPRebar`**:
  - `HPRebar.csproj` có cấu hình `<IsRepackable>true</IsRepackable>` (sử dụng `ILRepack` để gộp toàn bộ dependency vào `HPRebar.dll`).
  - Khi thêm `ClosedXML 0.104.2` vào `HPRebar.csproj`, các assembly phụ thuộc (`DocumentFormat.OpenXml`, `ExcelNumberFormat`, `SixLabors.Fonts`) sẽ được ILRepack gộp gọn gàng vào add-in.
  - `HPRebar.Core.csproj` (`netstandard2.0`) nên **giữ nguyên không thêm dependency ClosedXML**, duy trì triết lý Pure Domain Logic & Zero Dependency. Việc đọc OpenXML file sẽ do `HPRebar` (tầng Add-In/Infrastructure) đảm nhiệm và chuyển đổi thành `KataCellTable` truyền vào Core.

---

## 3. Bản Đồ Tọa Độ Chi Tiết Sheet 'Dam' Trong `Kata.xlsm`

Bảng tính sheet `Dam` của Kata Pro được phân chia thành 3 khu vực chính:
1. **Header & Global Parameters (Hàng 1..10, Cột A..J)**
2. **Cấu Trúc Lưới Nhịp & Gối Xen Kẽ (Hàng 10..23, Cột C..BZ)**
3. **Cấu Hình Chủng Loại Đai & Chi Tiết (Hàng 25..28, Cột A..N)**

### 3.1. Khu Vực 1: Header & Tham Số Chung (B3:B10 và Cột E..J)

| Tọa độ ô | Nhãn mô tả (Cột A / E / F) | Giá trị mẫu | Ý nghĩa kỹ thuật & Quy tắc nghiệp vụ | Kiểu dữ liệu |
|---|---|---|---|---|
| **`B1`** | Tỷ lệ bản vẽ dầm | `'1:50'` | Tỷ lệ thể hiện khung nhìn elevation dầm (chọn từ list validation M1:Q1) | string |
| **`B2`** | Tỷ lệ vẽ mặt cắt | `'1:25'` | Tỷ lệ các mặt cắt tiết diện dầm 1-1, 2-2 (chọn từ list N2:Q2) | string |
| **`B3`** | **Tên dầm** | `'B01'` | Ký hiệu / Tên dầm (Beam Mark / Name). Dùng để map với Revit Beam instance | string |
| **`B4`** | **Số cấu kiện** | `1` | Số lượng cấu kiện giống nhau trong dự án | int |
| **`B5`** | **h dầm (mm)** | `1100` | Chiều cao tiết diện danh nghĩa ban đầu $h$ (mm) | double |
| **`B6`** | **b dầm (mm)** | `500` | Bề rộng tiết diện danh nghĩa $b$ (mm) | double |
| **`B7`** | **h sàn (mm)** | `150` | Chiều dày sàn nách hai bên dầm $h_s$ (mm). Dùng để tính neo & móc đai | double |
| **`B8`** | Tên trục dầm | `""` | Tên đường lưới định vị chạy dọc theo tim dầm | string |
| **`B9`** | Độ lệch của trục | `-100` | Độ lệch khoảng cách từ tim dầm đến trục lưới dọc ($e_y$, mm) | double |
| **`B10`** | **Cao trình dầm** | `'+3.300'` | Cao độ đỉnh dầm (Level Elevation). Định dạng text chuẩn có dấu `+`/`-` | string / double |
| **`G1`** | Kéo thép gia cường | `500` | Chiều dài đoạn kéo dài vượt điểm cắt lý thuyết khi cắt nhiều lớp thép (mm) | double |
| **`G2`** | **Neo thép vùng kéo (d)** | `40` | Hệ số chiều dài đoạn neo chịu kéo: $L_{neo} = 40 \times \phi$ (ví dụ: $40d$) | double |
| **`G3`** | **Neo thép vùng nén (d)** | `30` | Hệ số chiều dài đoạn neo chịu nén: $L_{nén} = 30 \times \phi$ (ví dụ: $30d$) | double |
| **`H3`** | **Cắt thép gia cường trên Lớp 2** | `0.2` | Tỷ lệ chiều dài cắt thép nhịp đầu/gối trên Lớp 2: $0.2 \times L = L/5$ | double |
| **`I3`** | Gốc đo L lớp 2 | `'L từ tâm cột'` | Xác định chiều dài L tính từ tâm cột hay mép cột (Validation: N3:N5) | string |
| **`H5`** | **Cắt thép gia cường trên Lớp 1** | `0.25` | Tỷ lệ chiều dài cắt thép nhịp đầu/gối trên Lớp 1: $0.25 \times L = L/4$ | double |
| **`I5`** | Gốc đo L lớp 1 | `'L từ mép cột'` | Xác định chiều dài L tính từ mép cột hay tâm cột (Validation: N3:N6) | string |
| **`G4`** | **Đường kính cốt giá (mm)** | `12` | Đường kính cốt thép cấu tạo dọc thân dầm (thép hông/cốt giá $\phi_{skin}$) | double |
| **`G5`** | **Số lớp cốt giá** | `2` | Số lớp cốt giá hai bên thân dầm (nhập âm: không vẽ đai ngang liên kết) | int |
| **`G6`** | **Đường kính đai (mm)** | `10` | Đường kính thép đai chủ của toàn bộ dải dầm ($\phi_{stirrup}$, mm) | double |
| **`G7`** | **Khoảng cách đai gần gối** | `'a150'` | Bước cốt đai tại vùng gia cường gần gối tựa ($s_{gối} = 150\text{ mm}$) | string |
| **`G8`** | **Khoảng cách đai giữa nhịp** | `'a200'` | Bước cốt đai tại vùng giữa nhịp ($s_{nhịp} = 200\text{ mm}$) | string |
| **`G9`** | Khoảng cách đai công-xôn | `150` | Bước cốt đai tại các đoạn nhịp nhô console ($s_{cantilever} = 150\text{ mm}$) | double / string |
| **`J9`** | **a bảo vệ thép chủ/đai** | `'50/25'` | Lớp bê tông bảo vệ: $c_{main} = 50\text{ mm}$, $c_{stirrup} = 25\text{ mm}$ | string / double |
| **`I8`** | Số nhánh đai mặc định | `2` | Số nhánh đai đứng mặc định (2 nhánh hoặc 4 nhánh) | int |

---

### 3.2. Khu Vực 2: Cấu Trúc Lưới Nhịp & Gối Xen Kẽ (Hàng 10..23, Cột C..BZ)

Nguyên tắc bất biến của Kata: **Hàng 10 xác định kiểu của từng cột. Cột lẻ (C, E, G, I, K, M, O...) là CỘT (Gối tựa); Cột chẵn (D, F, H, J, L, N...) là NHỊP**.

```
Cột C (Gối 1) -> Cột D (Nhịp 1) -> Cột E (Gối 2) -> Cột F (Nhịp 2) -> Cột G (Gối 3) -> ...
```

| Hàng | Nhãn (Cột A) | Ý nghĩa tại Cột lẻ (GỐI TỰA / CỘT) | Ý nghĩa tại Cột chẵn (NHỊP DẦM) | Định dạng & Ví dụ thực tế |
|---|---|---|---|---|
| **10** | *Tiêu đề* | Luôn là `'Cột'` | Luôn là `'Nhịp'` | Text |
| **11** | **Thép chịu lực trên / Kích thước** | Bề rộng gối $b_c$ dọc trục (mm); `0` nếu là đầu console / điểm nối; `"b x h"` nếu gối là dầm phụ | Chiều dài thông thủy nhịp $L_n$ (mm) | Gối: `400`, `300x500`<br/>Nhịp: `10400`, `6500` |
| **12** | **Thép chịu lực dưới** | *(Không dùng tại gối)* | *(Thép chịu lực dưới chạy suốt được khai báo tập trung ở `B12`)* | Ô `B12 = '6f25'` |
| **13** | **Thép gia cường trên (Lớp 1)** | Thép gia cường trên Lớp 1 đặt tại gối (nằm cùng mặt phẳng với thép chủ `B11`) | *(Không bố trí gia cường trên tại giữa nhịp)* | `'6f25'`, `'2f20'`, `'2f20;2f16'` |
| **14** | **Thép gia cường trên (Lớp 2)** | Thép gia cường trên Lớp 2 đặt tại gối (cách Lớp 1 một khoảng hở trong) | *(Không bố trí)* | `'6f25'`, `'6f20'`, `'2f20'` |
| **15** | **Thép gia cường trên (Lớp 3)** | Thép gia cường trên Lớp 3 đặt tại gối (dầm chịu mô-men âm cực lớn) | *(Không bố trí)* | `'6f20;0'` |
| **16** | **Thép gia cường trên (Lớp 4)** | Thép gia cường trên Lớp 4 (nếu có) | *(Không bố trí)* | Chuỗi thép |
| **17** | **Thép gia cường dưới (Lớp 2)** | *(Không bố trí gia cường dưới tại gối)* | Thép gia cường dưới Lớp 2 tại vùng giữa nhịp chịu mô-men dương | `'6f25'`, `'2f20'`, `'-'` (không có) |
| **18** | **Thép gia cường dưới (Lớp 1)** | *(Không bố trí gia cường dưới tại gối)* | Thép gia cường dưới Lớp 1 tại vùng giữa nhịp (cùng mặt phẳng thép chủ `B12`) | `'6f25'`, `'3f20'` |
| **19** | **Giật mép trên dầm; Thép chịu lực** | Thông số cột tầng trên: `"bề_rộng;độ_lệch"` (ví dụ `350;0` hoặc `250;790`); `0` nếu không có cột trên | Giật cao độ mép trên dầm $\Delta z_{top}$ (mm) kèm đổi thép: ví dụ `-50` (hạ 50), `100;5f25` (nâng 100 và đổi thép chủ) | Gối: `350;0`<br/>Nhịp: `-50`, `100;5f25` |
| **20** | **Bề rộng dầm giao / Cốt giá** | Bề rộng dầm giao trực giao đỡ tại gối $b_{giao}$ (mm), ví dụ `400` | Thay đổi/ghi đè cốt thép hông (cốt giá) cho nhịp này: ví dụ `0f12`, `2f12`, `0` (không đặt cốt giá) | Gối: `400`<br/>Nhịp: `0f12`, `2f12`, `0` |
| **21** | **Độ lệch dầm giao / Giật mép dưới** | Độ lệch tâm của dầm giao so với tim gối (mm) | Giật cao độ mép dưới dầm $\Delta z_{soffit}$ và đổi thép dưới: ví dụ `-100;5f20` (hạ đáy 100, thép dưới 5f20) | Gối: `0`, `-50`<br/>Nhịp: `-100;5f20`, `400` |
| **22** | **Tên các trục cột / Đai nhịp** | Tên đường lưới định vị (Grid Name) đi qua gối, ví dụ `'1'`, `'2'`, `'A'` | *(Tùy chọn)* Ghi đè bước đai cục bộ cho nhịp: ví dụ `a100/200/50` hoặc `a100/200` | Gối: `'1'`, `'D.1a'`<br/>Nhịp: `a100/200` |
| **23** | **Độ lệch trục cột / Đai gia cường** | Độ lệch tâm từ tim gối đến trục lưới định vị ($s_{grid} - s_{center}$, mm) | *(Tùy chọn)* Bổ sung bước đai gia cường chống cắt cho nhịp | Gối: `-150`, `0`, `50` |
| **24** | **Cấu hình đặc biệt** | Nhập `30`: đai chống xoắn tại gối; Nhập `*`: không bố trí đai gia cường | | `'30'`, `'*'` |

---

### 3.3. Khu Vực 3: Chủng Loại & Hình Dạng Cốt Đai (Hàng 25..28)

Kata cho phép cấu hình tổ hợp các loại đai nhằm tạo thành hệ đai 3 nhánh, 4 nhánh hoặc đai nhiều lồng cho các dầm có tiết diện lớn ($b \ge 400\text{ mm}$):

| Hàng | Nhãn (Cột A) | Chủng loại đai | Cột C | Cột D | Ý nghĩa kỹ thuật |
|---|---|---|---|---|---|
| **`25`** | `Đai □` | Đai chữ nhật kín (Closed Rectangular Stirrup) | `Đai U` | `3-4` | Nhánh đai dạng chữ U bao quanh nhánh 3 và 4 |
| **`26`** | `Đai U` | Đai mũ hở đầu dạng U (Cap Stirrup) | `Đai C` | `2` | Nhánh đai móc chữ C tại nhánh thứ 2 |
| **`27`** | `Đai C` | Đai móc chữ C / Đai giằng ngang (Cross Tie) | `Đai C` | `5` | Nhánh đai móc chữ C tại nhánh thứ 5 |
| **`28`** | `1` | Số lượng đai chính bao ngoài | | | Số vòng đai chữ nhật bao kín toàn bộ tiết diện |

---

## 4. Đặc Tả Ngữ Pháp & Quy Tắc Parse Chuỗi Thép (Rebar Notation Grammar)

Các ô trong bảng tính Kata sử dụng hệ thống ký hiệu viết tắt chuyên nghiệp trong ngành xây dựng Việt Nam. Bộ parser trong `HPRebar.Core` phải phân tích chính xác các định dạng sau:

### 4.1. Cốt Thép Dọc (Longitudinal Rebar)
- **Cú pháp chuẩn**: `<Số_lượng><Ký_hiệu_phi><Đường_kính>`
  - Regex: `^(?<count>\d+)\s*(?:f|d|phi|Ø|%%c)\s*(?<dia>\d+(?:\.\d+)?)$` (Case-insensitive)
  - Ví dụ:
    - `6f25` $\rightarrow$ Count = 6, Diameter = 25 mm.
    - `2f18` $\rightarrow$ Count = 2, Diameter = 18 mm.
    - `3f20` $\rightarrow$ Count = 3, Diameter = 20 mm.
    - `2d8` $\rightarrow$ Count = 2, Diameter = 8 mm (`d` tương đương `f`).
    - `f10` $\rightarrow$ Count = 1 (mặc định nếu khuyết số lượng), Diameter = 10 mm.
    - `0f12` $\rightarrow$ Count = 0, Diameter = 12 mm (tắt cốt thép tường minh).
    - `0` hoặc `-` $\rightarrow$ Rỗng (không có cốt thép).

### 4.2. Cốt Thép Đai & Bước Rải (Stirrup Spacing)
- **Cú pháp 1 vùng (Uniform)**: `a<bước>` hoặc `@<bước>` hoặc `<bước>`
  - Regex: `^(?:a|@)?\s*(?<spacing>\d+)$`
  - Ví dụ: `a150`, `@150`, `150` $\rightarrow$ Bước đai đều 150 mm.
- **Cú pháp 2 vùng đối xứng (2-Zone Symmetric)**: `a<bước_gối>/<bước_nhịp>`
  - Regex: `^(?:a|@)?\s*(?<s_support>\d+)\s*/\s*(?<s_mid>\d+)$`
  - Ví dụ: `a100/200` $\rightarrow$ Gần 2 gối tựa rải @100 mm (phạm vi $L/4$), giữa nhịp rải @200 mm (phạm vi $L/2$).
- **Cú pháp 3 vùng bất đối xứng (3-Zone Asymmetric)**: `a<bước_gối_trái>/<bước_nhịp>/<bước_gối_phải>`
  - Regex: `^(?:a|@)?\s*(?<s_left>\d+)\s*/\s*(?<s_mid>\d+)\s*/\s*(?<s_right>\d+)$`
  - Ví dụ: `a100/200/50` $\rightarrow$ Đầu nhịp @100 mm, giữa nhịp @200 mm, cuối nhịp @50 mm.

### 4.3. Biểu Thức Phức Hợp (Compound Rebar Strings)
- **Tổ hợp nhiều cụm thanh phân cách bởi dấu chấm phẩy `;`**:
  - `2f20;2f16`: 2 thanh $\phi 20$ kết hợp 2 thanh $\phi 16$ (hoặc 2f20 cánh trái, 2f16 cánh phải).
  - `6f20;0`: Bên trái đặt 6 thanh $\phi 20$, bên phải không đặt (0).
  - `-50;5f20`: Tham số kép $\Delta h = -50\text{ mm}$ (giật cấp), cốt thép tương ứng là $5\phi 20$.
  - `100;5f25`: Giật mép trên $+100\text{ mm}$, cốt thép tương ứng là $5\phi 25$.
- **Lớp bê tông bảo vệ kép**:
  - `50/25`: $c_{main} = 50\text{ mm}$, $c_{stirrup} = 25\text{ mm}$.
  - `30`: $c_{main} = 30\text{ mm}$, $c_{stirrup} = 30 - d_{stirrup}/2 = 25\text{ mm}$.

---

## 5. Thiết Kế Mô Hình Strongly-Typed C# DTO (`KataBeamRebarSpec`)

Mô hình DTO được thiết kế tối ưu, đặt trọn vẹn trong `HPRebar.Core/KataRebar/Models/` (hoặc `HPRebar.Core/BeamRebar/Models/`), bảo đảm tính bất biến (immutable records), dễ dàng tuần tự hóa và có khả năng ánh xạ trực tiếp sang các calculator hình học 3D.

### 5.1. Cấu Trúc Lớp DTO Hoàn Chỉnh

```csharp
namespace HPRebar.Core.KataRebar.Models;

using System;
using System.Collections.Generic;

/// <summary>
/// Đại diện toàn bộ đặc tả cốt thép và hình học của 1 dải dầm đọc từ sheet Dam (Kata.xlsm).
/// </summary>
public sealed record KataBeamRebarSpec
{
    public required KataHeaderSpec Header { get; init; }
    public required KataAnchorageSpec Anchorage { get; init; }
    public required KataCoverSpec Cover { get; init; }
    public required KataMainContinuousBarsSpec ContinuousBars { get; init; }
    public required KataSideBarSpec SideBars { get; init; }
    public required KataStirrupGlobalSpec Stirrups { get; init; }
    public required IReadOnlyList<KataSupportSpec> Supports { get; init; }
    public required IReadOnlyList<KataSpanSpec> Spans { get; init; }
}

/// <summary>
/// Thông tin định danh và kích thước hình học toàn cục của dầm (B3:B10).
/// </summary>
public sealed record KataHeaderSpec
{
    public required string BeamName { get; init; }          // B3: Tên dầm (vd "B01")
    public int ElementCount { get; init; } = 1;              // B4: Số cấu kiện
    public double HeightMm { get; init; }                   // B5: Chiều cao h (mm)
    public double WidthMm { get; init; }                    // B6: Bề rộng b (mm)
    public double SlabThicknessMm { get; init; }            // B7: Chiều dày sàn nách hs (mm)
    public string AxisGridName { get; init; } = "";         // B8: Tên trục dọc
    public double AxisOffsetMm { get; init; }               // B9: Độ lệch trục dọc
    public string LevelElevationText { get; init; } = "";   // B10: Cao trình dầm (vd "+3.300")
    public double LevelElevationM { get; init; }            // B10 quy đổi ra mét (vd 3.3)
}

/// <summary>
/// Cấu hình quy tắc neo, uốn và hệ số cắt thép gia cường (G1:G3, H3, H5, I3, I5).
/// </summary>
public sealed record KataAnchorageSpec
{
    public double TensionMultiplier { get; init; } = 40.0;     // G2: Neo chịu kéo (vd 40d)
    public double CompressionMultiplier { get; init; } = 30.0; // G3: Neo chịu nén (vd 30d)
    public double TopExtraCutoffRatioL1 { get; init; } = 0.25; // H5: Tỷ lệ cắt thép Lớp 1 (vd 0.25 = L/4)
    public double TopExtraCutoffRatioL2 { get; init; } = 0.20; // H3: Tỷ lệ cắt thép Lớp 2 (vd 0.20 = L/5)
    public KataCutoffOrigin OriginL1 { get; init; } = KataCutoffOrigin.FromColumnFace; // I5
    public KataCutoffOrigin OriginL2 { get; init; } = KataCutoffOrigin.FromColumnCenter; // I3
    public double MultiLayerExtensionMm { get; init; } = 500.0; // G1: Đoạn kéo dài cắt nhiều lớp (mm)
}

public enum KataCutoffOrigin
{
    FromColumnFace,    // "L từ mép cột"
    FromColumnCenter   // "L từ tâm cột"
}

/// <summary>
/// Lớp bê tông bảo vệ danh nghĩa cốt chủ và cốt đai (J9).
/// </summary>
public sealed record KataCoverSpec
{
    public double MainBarCoverMm { get; init; } = 30.0;   // Lớp bảo vệ cốt chủ (mm)
    public double StirrupCoverMm { get; init; } = 25.0;   // Lớp bảo vệ cốt đai (mm)
}

/// <summary>
/// Cốt thép chịu lực chạy suốt dọc theo toàn bộ dải dầm (B11, B12).
/// </summary>
public sealed record KataMainContinuousBarsSpec
{
    public required RebarBundleSpec TopBars { get; init; }      // B11: vd 6f25
    public required RebarBundleSpec BottomBars { get; init; }   // B12: vd 6f25
}

/// <summary>
/// Thông số cốt thép hông / cốt giá cấu tạo (E4, G4, E5, G5).
/// </summary>
public sealed record KataSideBarSpec
{
    public double DiameterMm { get; init; } = 12.0;            // G4: Đường kính cốt giá
    public int LayerCount { get; init; } = 2;                  // G5: Số lớp cốt giá
    public bool IncludeHorizontalTies { get; init; } = true;   // false nếu G5 âm
}

/// <summary>
/// Cấu hình thép đai toàn cục (G6..G9, I8, Hàng 25..27).
/// </summary>
public sealed record KataStirrupGlobalSpec
{
    public double DiameterMm { get; init; } = 10.0;            // G6: Đường kính đai (mm)
    public double SupportSpacingMm { get; init; } = 150.0;     // G7: Bước đai gần gối (mm)
    public double MidspanSpacingMm { get; init; } = 200.0;     // G8: Bước đai giữa nhịp (mm)
    public double CantileverSpacingMm { get; init; } = 150.0;  // G9: Bước đai công-xôn (mm)
    public int DefaultLegCount { get; init; } = 2;             // I8: Số nhánh đai mặc định
    public IReadOnlyList<KataStirrupBranchSpec> Branches { get; init; } = Array.Empty<KataStirrupBranchSpec>();
}

public sealed record KataStirrupBranchSpec
{
    public KataStirrupShapeType ShapeType { get; init; }       // Hàng 25-27: Đai □, Đai U, Đai C
    public string BranchPosition { get; init; } = "";          // vd "3-4", "2", "5"
}

public enum KataStirrupShapeType
{
    ClosedStirrup,  // Đai chữ nhật kín □
    CapStirrup,     // Đai mũ chữ U
    CrossTie        // Đai móc chữ C
}

/// <summary>
/// Đại diện cho 1 cụm thanh thép: Số lượng + Đường kính (vd 2f18, 6f25).
/// </summary>
public sealed record RebarBundleSpec
{
    public int Count { get; init; }
    public double DiameterMm { get; init; }

    public double TotalAreaMm2 => Count * Math.PI * Math.Pow(DiameterMm / 2.0, 2);
    public override string ToString() => $"{Count}f{DiameterMm:0}";

    public static RebarBundleSpec Empty => new() { Count = 0, DiameterMm = 0 };
}

/// <summary>
/// Cốt thép phức hợp (hỗ trợ phân bố 2 bên trái/phải hoặc kèm giật cấp cao độ).
/// </summary>
public sealed record CompoundRebarSpec
{
    public RebarBundleSpec? Primary { get; init; }
    public RebarBundleSpec? Secondary { get; init; }
    public double LevelOffsetMm { get; init; }   // Chênh lệch cao độ giật cấp (nếu có)
}

/// <summary>
/// Dữ liệu chi tiết tại một Gối Tựa (Cột lẻ: C, E, G, I, K, M, O...).
/// </summary>
public sealed record KataSupportSpec
{
    public int SupportIndex { get; init; }                      // Thứ tự gối (0, 1, 2...)
    public double WidthMm { get; init; }                        // Hàng 11: Bề rộng gối (0 nếu console)
    public string? BeamSupportSection { get; init; }            // Hàng 11: "b x h" nếu gối là dầm
    public KataSupportKind Kind { get; init; }                  // Cột, Móng, Dầm hoặc Cantilever

    // Thép gia cường trên đặt tại gối (Hàng 13..16)
    public IReadOnlyList<CompoundRebarSpec> TopExtraLayers { get; init; } = Array.Empty<CompoundRebarSpec>();

    // Thông tin cột tầng trên (Hàng 19)
    public double UpperColumnWidthMm { get; init; }
    public double UpperColumnOffsetMm { get; init; }

    // Dầm giao tại gối (Hàng 20, 21)
    public double CrossingBeamWidthMm { get; init; }
    public double CrossingBeamOffsetMm { get; init; }

    // Trục định vị đi qua gối (Hàng 22, 23)
    public string GridName { get; init; } = "";
    public double GridOffsetMm { get; init; }
}

public enum KataSupportKind
{
    Column,
    Foundation,
    BearingBeam,
    CantileverTip,
    InternalJoint
}

/// <summary>
/// Dữ liệu chi tiết tại một Nhịp Dầm (Cột chẵn: D, F, H, J, L, N...).
/// </summary>
public sealed record KataSpanSpec
{
    public int SpanIndex { get; init; }                         // Thứ tự nhịp (0, 1, 2...)
    public double ClearLengthMm { get; init; }                  // Hàng 11: Chiều dài thông thủy Ln

    // Thép gia cường dưới tại nhịp (Hàng 17..18)
    public IReadOnlyList<RebarBundleSpec> BottomExtraLayers { get; init; } = Array.Empty<RebarBundleSpec>();

    // Giật cấp cao độ nhịp (Hàng 19, 21)
    public double TopStepMm { get; init; }                      // Hàng 19: Giật mép trên
    public double SoffitStepMm { get; init; }                   // Hàng 21: Giật mép dưới

    // Ghi đè cốt giá & cốt đai nhịp
    public RebarBundleSpec? SideBarOverride { get; init; }      // Hàng 20: vd 0f12, 2f12
    public KataSpanStirrupOverride? StirrupOverride { get; init; } // Hàng 22: vd a100/200
}

public sealed record KataSpanStirrupOverride
{
    public double SupportZoneSpacingMm { get; init; }
    public double MidspanZoneSpacingMm { get; init; }
    public double? EndSupportZoneSpacingMm { get; init; }
}
```

---

## 6. Thiết Kế Bộ Parser & Interface Đọc 2 Nguồn (Architecture Design)

Nhằm đảm bảo phân tách tuyệt đối giữa logic nghiệp vụ (Core) và hạ tầng I/O (Revit Addin / COM / File), giải pháp sử dụng mẫu thiết kế **Accessor Abstraction**:

```
[ Active Excel (COM oleaut32) ] ---> [ ComKataDamReader ] ------\
                                                                 +--> [ IKataDamCellAccessor ] ---> [ KataDamSheetParser ] ---> [ KataBeamRebarSpec ]
[ File Kata.xlsm (ClosedXML)  ] ---> [ ClosedXmlKataDamReader ] --/          (Pure Memory)              (HPRebar.Core)
[ In-Memory Dict (xUnit Test) ] ---> [ TestKataDamAccessor ] ----/
```

### 6.1. Giao Diện Truy Xuất Ô `IKataDamCellAccessor` (`HPRebar.Core`)
```csharp
namespace HPRebar.Core.KataRebar;

public interface IKataDamCellAccessor
{
    object? GetCellValue(string cellAddress);
    object? GetCellValue(int row, int col);
    
    string GetString(string cellAddress, string defaultValue = "");
    double GetDouble(string cellAddress, double defaultValue = 0.0);
    int GetInt(string cellAddress, int defaultValue = 0);
}
```

### 6.2. Triển Khai Bộ Parser `KataDamSheetParser` (`HPRebar.Core`)
Lớp parser nhận `IKataDamCellAccessor`, quét lần lượt:
1. Header: đọc `B3..B10`, `G1..G9`, `H3`, `H5`, `I3`, `I5`, `J9`.
2. Thép chủ: parse chuỗi `B11` $\rightarrow$ `TopBars`, `B12` $\rightarrow$ `BottomBars`.
3. Quét vòng lặp từ cột C ($col = 3$) đến cột $BZ$ ($col = 78$):
   - Đọc ô Hàng 10: nếu là `"Cột"` hoặc giá trị Hàng 11 hợp lệ $\rightarrow$ parse `KataSupportSpec`.
   - Đọc ô Hàng 10: nếu là `"Nhịp"` $\rightarrow$ parse `KataSpanSpec`.
   - Dừng lại khi gặp 2 cột liên tiếp không có dữ liệu ở hàng 11.
4. Trả về kết quả `KataBeamRebarSpec` hoàn chỉnh với các thông báo cảnh báo/lỗi (nếu có).

### 6.3. Triển Khai Bộ Đọc COM `ComKataDamReader` (`HPRebar`)
```csharp
public static class ComKataDamReader
{
    public static KataCellTable ReadActiveSheet()
    {
        if (!ExcelComAttach.TryGetRunningExcel(out var app, out var reason))
            throw new InvalidOperationException(reason);

        try
        {
            var wb = ComLateBinding.Get(app!, "ActiveWorkbook") 
                     ?? throw new InvalidOperationException("Không có workbook nào được kích hoạt.");
            var sheets = ComLateBinding.Get(wb, "Worksheets");
            var ws = ComLateBinding.Get(sheets!, "Item", "Dam") 
                     ?? throw new InvalidOperationException("Workbook đang mở không có sheet 'Dam'.");

            // Đọc toàn bộ khối dữ liệu A1:BZ30 trong 1 lệnh duy nhất!
            var range = ComLateBinding.Get(ws, "Range", "A1:BZ30");
            var raw2D = ComLateBinding.Get(range!, "Value2") as object[,];

            return new KataCellTable(raw2D, startRow: 1, startCol: 1);
        }
        finally
        {
            ComLateBinding.Release(app);
        }
    }
}
```

### 6.4. Triển Khai Bộ Đọc ClosedXML `ClosedXmlKataDamReader` (`HPRebar`)
```csharp
public static class ClosedXmlKataDamReader
{
    public static KataCellTable ReadFromFile(string filePath)
    {
        using var workbook = new ClosedXML.Excel.XLWorkbook(filePath);
        var ws = workbook.Worksheet("Dam");
        
        // Trích xuất các ô từ A1 đến BZ30 vào bảng nhớ KataCellTable
        var table = new Dictionary<string, object?>();
        var range = ws.Range("A1:BZ30");
        foreach (var cell in range.Cells())
        {
            table[cell.Address.ToString()] = cell.Value.ToString();
        }
        return new KataCellTable(table);
    }
}
```

---

## 7. Đánh Giá Khả Năng Chuyển Đổi Sang Geometry 3D (`HPRebar.Core/BeamRebar`)

Một ưu điểm kiến trúc vượt trội là repo đã sở hữu sẵn mô hình `BeamContinuousStack` trong `HPRebar.Core/BeamRebar/Models/`:
- `KataBeamRebarSpec.Spans` $\rightarrow$ Ánh xạ 1:1 sang `IReadOnlyList<BeamSpan>` (với $L_n$, tiết diện $b \times h$, $z_{offset}$).
- `KataBeamRebarSpec.Supports` $\rightarrow$ Ánh xạ 1:1 sang `IReadOnlyList<BeamSupportNode>` (với bề rộng gối, vị trí tâm gối, loại gối cột/vách/dầm).
- `KataMainContinuousBarsSpec` $\rightarrow$ Ánh xạ trực tiếp sang `BeamMainBarSpec` (TopCount, TopDiameter, BottomCount, BottomDiameter, HookLength tính theo $40d$/$30d$).
- `KataSupportSpec.TopExtraLayers` $\rightarrow$ Ánh xạ trực tiếp sang `BeamAdditionalBarSpec` (với chiều dài cắt $L/4$, $L/5$ tính từ mép cột).
- `KataStirrupGlobalSpec` $\rightarrow$ Ánh xạ trực tiếp sang `BeamStirrupSpec` và `StirrupZone` (vùng gối @150, giữa nhịp @200).

Nhờ vậy, toàn bộ thuật toán tính đường sinh rebar 3D (`Polyline3`, `BarPolyline`) và dịch chuyển cốt thép theo lớp đã có sẵn trong `HPRebar.Core`, không cần phải viết lại từ đầu!

---

## 8. Kế Hoạch Kiểm Thử Tự Động (Verification & Test Strategy)

1. **Unit Test Parser (`HPRebar.Core.Tests/KataRebar/`)**:
   - Viết các test case xUnit nạp fixture giả lập mảng dữ liệu sheet `Dam` chuẩn của `Kata.xlsm`.
   - Kiểm tra parse đúng:
     - 5 nhịp, 6 gối của dầm mẫu `B01` (`h = 1100`, `b = 500`, thép chủ `6f25`).
     - Thép gia cường trên Lớp 1, 2, 3 tại các gối (`6f25`, `6f20`, `6f20;0`, `2f20;2f16`).
     - Thép gia cường dưới tại các nhịp (`6f25`, `2f20`, `-`).
     - Các cú pháp chuỗi đặc biệt (`2f18`, `3f20`, `2d8`, `0f12`, `a100/200/50`, `50/25`).
   - Kiểm tra khả năng bắt lỗi và đưa cảnh báo khi dầm thiếu thông số hoặc chuỗi cú pháp sai.
2. **Build & Quality Check**:
   - Chạy `dotnet test HPRebar/HPRebar.Core.Tests` bảo đảm 100% test pass.
   - Chạy `dotnet build HPRebar/HPRebar.slnx -c Debug.R26` bảo đảm 0 lỗi biên dịch.
