# Code review — Phase 1 HPFoundation (đọc Excel trên bản sao)

Ngày 2026-11-06. Phạm vi: `HPFoundation.Core/**`, `HPFoundation.App/**`, `HPFoundation.Core.Tests/Sheets/**`, `Directory.Build.props`, `HPFoundation.slnx`. 1 396 dòng C# (App 389, Core 536, test 471). Không sửa file nguồn.

## Kết luận

Trục chính đúng: file gốc chỉ được đọc, Excel là process riêng, không tìm thấy đường nào ghi vào file gốc hay đóng Excel của kỹ sư. Chưa đạt phase: 6 lỗi High, trong đó 4 là yêu cầu plan chưa làm hoặc làm sai (timeout, `!!!`, orphan, MB9).

| Mức | Số |
|---|---|
| Critical | 0 |
| High | 6 |
| Medium | 8 |
| Low | 7 |

## Cách kiểm chứng

Chạy `HPFoundation.App.exe` thật trên bản sao trong scratchpad, không dùng file trên `H:`. Excel của người dùng (PID 23740, `Kata.xlsm`) không bị đụng, còn nguyên sau khi xong. Orphan do thử nghiệm tự tạo đã kill đúng PID, thư mục chạy thử đã xoá.

| Thử nghiệm | Kết quả đo |
|---|---|
| `inspect` sheet không tồn tại | `COMException DISP_E_BADINDEX` ra Main, stack trace thô. Excel **vẫn đóng**, không orphan |
| Force-kill app khi Excel đang mở | Excel ẩn sống ≥ 60 s, phải kill tay |
| File có mật khẩu mở | App treo ≥ 45 s, không tự thoát |
| `dump-grid` sheet 1 ô, sheet rỗng | `RuntimeBinderException` tại `ExcelSession.cs:60`, `:61` |
| `dump-grid` used range bắt đầu từ B3 | Địa chỉ đúng B3, C3, B4, C4 |
| `inspect` file rác `.xls` | 0 sheet, in "Source file: unchanged", không báo lỗi |
| Dump 8 sheet 4 cột | Mọi neo, đơn vị `m`, công thức, bảng lò xo khớp. Duy nhất MB9 có tim cột lệch lưới |

Chưa chạy: unit test (đã biết), lỗi constructor (đọc code), `IgnoreRemoteRequests` (không dám thử khi người dùng đang mở Excel).

## High

### H1 — Không có timeout, Excel ẩn treo là treo vĩnh viễn
[ExcelSession.cs:103](../../../HPFoundation/HPFoundation.App/Excel/ExcelSession.cs#L103), mọi lời gọi COM khác. Rule: plan Risk "timeout, chỉ kill đúng PID", CO3.

- **Kịch bản:** workbook có mật khẩu mở. `DisplayAlerts = false` không tắt hộp thoại mật khẩu, nó hiện trong Excel ẩn, `Workbooks.Open` không bao giờ trả về. Đã đo: treo ≥ 45 s. Với WPF sau này là cửa sổ đứng im, không có nút huỷ.
- **Sửa:** lời gọi COM chặn luồng STA nên watchdog phải ở luồng khác: quá hạn thì kill PID của session, lời gọi đang chặn trả `COMException`, luồng chính đi `Dispose`. Thêm tham số `Password` giả vào `Open` để file khoá báo lỗi ngay thay vì hỏi (CHƯA TEST).

### H2 — App chết là Excel ẩn mồ côi
[ExcelSession.cs:29](../../../HPFoundation/HPFoundation.App/Excel/ExcelSession.cs#L29). Rule: plan Success "Không còn EXCEL.EXE mồ côi".

- **Kịch bản:** kỹ sư đóng cửa sổ console, Ctrl+C, hoặc app crash. Không có `finally` nào chạy. Đã đo với force-kill: Excel sống ≥ 60 s, giữ RAM và giữ file trong thư mục chạy. Ctrl+C đi cùng đường, CHƯA TEST riêng.
- **Sửa:** Job Object `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE`. Excel do DCOM sinh ra, không phải process con của app, nên phải `AssignProcessToJobObject` theo PID ngay sau khi lấy Hwnd. Đây là cách duy nhất còn hiệu lực khi app bị kill.

### H3 — Excel sót lại trên các đường lỗi trong `ExcelSession`
Rule: D2, CO3.

| Vị trí | Sự thật |
|---|---|
| [ExcelSession.cs:29-34](../../../HPFoundation/HPFoundation.App/Excel/ExcelSession.cs#L29-L34) | Sau `CreateInstance` có 5 lời gọi COM không có `try`. Một lỗi ở đây: constructor ném, `using` chưa có gì để dispose, Excel ở lại. `RPC_E_CALL_REJECTED` lúc Excel đang khởi động là khả thi vì không có message filter (Med-4) |
| [ExcelSession.cs:105-110](../../../HPFoundation/HPFoundation.App/Excel/ExcelSession.cs#L105-L110) | `_excel.Quit()` nằm trong `catch`, không được bảo vệ. Excel từ chối gọi thì `Quit` ném, `EnsureProcessEnded` không chạy, Excel sót, lỗi gốc của `Open` bị mất. Chỉ bắt `COMException` |
| [ExcelSession.cs:95-96](../../../HPFoundation/HPFoundation.App/Excel/ExcelSession.cs#L95-L96) | `CloseExcel` ném lỗi khác `COMException` (ví dụ `InvalidComObjectException`) thì bỏ qua `EnsureProcessEnded` |
| [ExcelSession.cs:163](../../../HPFoundation/HPFoundation.App/Excel/ExcelSession.cs#L163) | `Kill` ném từ `Dispose` đè mất lỗi gốc khi đang unwind |

**Sửa:** constructor trivial, chuyển sang factory; dọn dẹp một đường duy nhất:

```csharp
public static ExcelSession Open(string workbookPath)
{
    dynamic excel = Activator.CreateInstance(ExcelType())!;
    var processId = ProcessIdOf((int)excel.Hwnd);
    try
    {
        Configure(excel);
        return new ExcelSession(excel, excel.Workbooks.Open(workbookPath, DontUpdateLinks, false), processId);
    }
    catch
    {
        Terminate(excel, processId); // never throws: Quit guarded, release, wait, kill
        throw;
    }
}
```

`Dispose` gọi cùng `Terminate` trong `try/finally`.

### H4 — Kiểm tra `!!!` và ô lỗi không đọc đúng ô
[FoundationInputBuilder.cs:22-36](../../../HPFoundation/HPFoundation.Core/Sheets/FoundationInputBuilder.cs#L22-L36), [FoundationInputBuilderTests.cs:56-65](../../../HPFoundation/HPFoundation.Core.Tests/Sheets/FoundationInputBuilderTests.cs#L56-L65). Rule: plan "Kiểm tra: chữ `!!!`, ô lỗi", T4.

- **Sự thật:** trên MB1 `!!!` chỉ có ở `E110, H110, E111, H111` (fixture). Không dòng nào đọc 4 ô đó (grep rỗng). Test đặt `!!!` vào `D24`, ô nhập tay không thể ra `!!!`, nên test xanh mà yêu cầu không được bảo vệ.
- **Kịch bản:** kỹ sư gõ `C110 = B30`. `E110`, `H110` thành `!!!`, toàn bộ phần thép hỏng. Tool báo `B30`, không issue, exit 0, ghi `input.json`. Ô `#DIV/0!` ngoài các ô được đọc cũng lọt tương tự.
- **Sửa:** sau khi tính lại, quét cả grid: `grid.FindAddresses(cell => cell.ErrorText is not null || cell.Text?.Contains("!!!") == true)`, mỗi ô thành một `SheetIssue`. 8 sheet hiện 0 ô lỗi nên không gây báo động giả. Sửa test sang `E110`.

### H5 — MB9 lọt cổng dù tim cột không nằm trên lưới 0.1 m
[InputValidator.cs:68-72](../../../HPFoundation/HPFoundation.Core/Sheets/InputValidator.cs#L68-L72). Rule: doc của `InputValidator`, plan phase 2 "lưới 0.1 m có điểm tại tim cột".

- **Sự thật:** chỉ tổng chiều dài được kiểm tra chia hết. `MB9- 4COT` có nhịp 3.85, trục `20` tại 4.15 m = 41.5 bước. Tổng 12 m chia hết nên 0 issue. 1 trên 8 sheet mục tiêu.
- **Ảnh hưởng:** phase 2 sẽ đặt tải lệch 5 cm, hoặc chèn nút làm lệch khoảng cách lò xo `F128`, `F129`, hoặc sai test 118 nút. Hàm khẳng định input "ETABS model can represent", nhưng không đúng ở đây.
- **Sửa:** kiểm tra từng `AxisPositionM` với cùng công thức. Cách xử lý MB9 cần người dùng chọn, xem cuối báo cáo.

### H6 — `PreparedSheet.Input` khác hợp đồng đã ghi
[SheetPreparation.cs:9](../../../HPFoundation/HPFoundation.App/Excel/SheetPreparation.cs#L9), [:37-43](../../../HPFoundation/HPFoundation.App/Excel/SheetPreparation.cs#L37-L43), [Program.cs:82-90](../../../HPFoundation/HPFoundation.App/Program.cs#L82-L90). Rule: API contract, N1.

- **Sự thật:** doc nói "Input stays null when the engineer has something to fix first", nhưng lỗi từ `InputValidator` vẫn trả `Input` khác null, và `Report` vẫn ghi `input.json`.
- **Kịch bản:** phase 2 viết `if (prepared.Input is { } input) Build(input)` theo doc, thế là dựng ETABS từ footing không hợp lệ.
- **Sửa:** `new PreparedSheet(sheetName, issues.Count == 0 ? built.Input : null, ...)`. Muốn giữ input để in thì tách tên rõ ràng.

## Medium

| Id | Vị trí | Rule | Sự thật và kịch bản | Sửa |
|---|---|---|---|---|
| Med-1 | [InputValidator.cs:27-38](../../../HPFoundation/HPFoundation.Core/Sheets/InputValidator.cs#L27-L38) | plan "chiều dài âm" | Chỉ kiểm tra trục trong `(0, L)`. Nhịp `1.6, 3.7, -1, 2.4, 5` cho trục 1.6, 5.3, 4.3, 6.7 đều trong móng, 0 issue, thứ tự trục sai im lặng. Nhịp 0 cũng lọt | Mọi nhịp giữa > 0, console ≥ 0; hoặc trục tăng ngặt |
| Med-2 | [SheetLayoutLocator.cs:162-166](../../../HPFoundation/HPFoundation.Core/Sheets/SheetLayoutLocator.cs#L162-L166) | ADR-0015 §4 | Regex khớp mọi `D\d+:D\d+`, không buộc với hàng tải. Sheet lỡ có `SUM(D17:D19)` thì F34 mới bỏ sót một cột, không ai báo. 8 sheet hiện đúng | Sau `Locate` so số hàng trong `SUM` với `FirstLoadRow..+LoadRowCount-1` |
| Med-3 | [ExcelSession.cs:60-61](../../../HPFoundation/HPFoundation.App/Excel/ExcelSession.cs#L60-L61) | M8, CO10 | Used range 1 ô trả scalar, sheet rỗng trả `""`: đã đo `RuntimeBinderException`. Danh sách sheet của WPF sẽ để người dùng chọn đúng sheet này. Thông báo vô nghĩa với kỹ sư | Bọc scalar thành mảng 1×1 có cận 1; `null` hoặc `""` thành grid rỗng |
| Med-4 | [ExcelSession.cs](../../../HPFoundation/HPFoundation.App/Excel/ExcelSession.cs) | CO3 | Không có `IMessageFilter`. Excel đang bận từ chối gọi thì lỗi ngẫu nhiên, và đổ vào đúng các đường H3 | Dùng lại mẫu `ComInteropHelper` của HPExcel, đăng ký trên luồng STA |
| Med-5 | [Program.cs:43-60](../../../HPFoundation/HPFoundation.App/Program.cs#L43-L60) | plan "file có thể đổi giữa chừng" | Không có sheet nào cũng exit 0 (đã đo in ra thành công). "CHANGED DURING THE RUN" chỉ in, exit vẫn 0, `input.json` vẫn dùng được. `IsSourceUnchanged` chạm ổ `H:`, mất mạng là ném sau khi đã xong việc | 0 sheet và nguồn đổi đều trả mã lỗi; I/O lỗi báo "không kiểm tra được" |
| Med-6 | [ExcelSession.cs:31-34](../../../HPFoundation/HPFoundation.App/Excel/ExcelSession.cs#L31-L34) | yêu cầu không đụng Excel của kỹ sư | Thiếu `IgnoreRemoteRequests = true`. GIẢ ĐỊNH CHƯA XÁC MINH: kỹ sư double-click `.xls` khi Excel ẩn đang sống, DDE có thể đẩy file vào instance ẩn, `Quit` với `DisplayAlerts = false` bỏ thay đổi chưa lưu. Phiên kéo dài hơn khi có phase ETABS | Đặt `true` khi khởi tạo. Thuộc tính này là cấp ứng dụng, phải tắt trước `Quit` |
| Med-7 | [FoundationInput.cs:29-36](../../../HPFoundation/HPFoundation.Core/Model/FoundationInput.cs#L29-L36), [WorkbookCopy.cs:6](../../../HPFoundation/HPFoundation.App/Excel/WorkbookCopy.cs#L6) | plan "ghi SHA-256 và thời gian sửa", `nguồn` | Record không có trường nguồn. SHA chỉ in console, `SourceLastWriteUtc` không ai đọc. `input.json` không truy được về bản file nào, trong khi file trên `H:` đã đổi một lần hôm nay | Thêm record nguồn vào `FoundationInput` hoặc ghi `run.json` |

### Med-8 — Test: phần quan trọng nhất không có test
Rule: T4, T7. Test đang ở trạng thái Implemented, CHƯA TEST.

| Khoảng trống | Vì sao đáng kể |
|---|---|
| `SheetPreparation.Prepare` không test nào | Thứ tự đọc trước, viết, tính lại, đọc sau là lý do tồn tại của phase. Plan ghi "Hệ quả cần có test". Sửa: seam mỏng `ISheetWorkbook` (D5) rồi test với fake |
| MB1 `42.85 → 50.55`, `18.44 → 10.74` | Không chỗ nào assert, kể cả live: `inspect` chỉ in. Cần harness có so sánh |
| `RealSheetTests.cs:51-52` chỉ `StartsWith` | Không bảo vệ phần còn lại của công thức. Dùng `Assert.Equal` chuỗi đầy đủ |
| Chỉ có fixture MB1 | `MB10- 4COT` lệch 2 hàng là lý do có neo, chỉ được chứng minh bằng chạy tay. Thêm fixture MB10, MB9 |
| `ToCell`, `WorkbookCopy` nằm ở App | Không có project test cho App. `ToCell` là logic thuần, nên chuyển sang Core (rule M7, T7) |
| Lỗi giả | `FoundationInputBuilderTests.cs:47` dùng `"#DIV/0!"`, production sinh `"Excel error -2146826281"` |
| Assert yếu | `InputValidatorTests.cs:35` `Contains("4")` khớp cả số `11.4` |
| Thiếu case | Neo trùng trong `ValueBeside`, nhiều `x1`, thiếu header `ki`, nhịp âm, trục lệch lưới |

Các test còn lại có giá trị thật: dịch hàng trong `SheetLayoutLocatorTests`, neo trùng công thức, `RealSheetTests` trên dump thật.

## Low

| Id | Vị trí | Rule | Sự thật | Sửa |
|---|---|---|---|---|
| L1 | [ExcelSession.cs:16-17](../../../HPFoundation/HPFoundation.App/Excel/ExcelSession.cs#L16-L17), [:123-126](../../../HPFoundation/HPFoundation.App/Excel/ExcelSession.cs#L123-L126) | | Dải dừng ở `#GETTING_DATA`. `#SPILL!`, `#CALC!` rơi vào nhánh text, thành số âm dạng chuỗi. Kỹ sư đọc "Excel error -2146826281" thay vì `#DIV/0!` | `Value2` không trả `int` thường, coi mọi `int` là lỗi, ánh xạ tên ô lỗi |
| L2 | [ExcelSession.cs:24](../../../HPFoundation/HPFoundation.App/Excel/ExcelSession.cs#L24), [Program.cs:104](../../../HPFoundation/HPFoundation.App/Program.cs#L104) | K1 | Bảo đảm "chỉ mở bản sao" hiện dựa vào quy ước: constructor nhận `string`, `Save()` public. `dump-grid a.xls MB1 a.xls` ghi đè file gốc | Nhận `WorkbookCopy`; từ chối output trùng nguồn |
| L3 | [ExcelSession.cs:21](../../../HPFoundation/HPFoundation.App/Excel/ExcelSession.cs#L21), [:160](../../../HPFoundation/HPFoundation.App/Excel/ExcelSession.cs#L160) | | Giữ số PID. Excel thoát sớm, Windows cấp lại số, `Kill` trúng process khác. Xác suất rất thấp nhưng hậu quả đúng điều cần tránh. `_processId == 0` thì không dọn được | Giữ `Process` mở từ đầu (handle chặn tái dùng PID), kiểm tra `ProcessName == "EXCEL"` |
| L4 | [Program.cs:21-30](../../../HPFoundation/HPFoundation.App/Program.cs#L21-L30) | CO10, CO14 | Không có catch cấp cao. Sheet sai ra stack trace chứa đường dẫn máy dev. `Dispose` đã chạy trong lần đo và probe .NET 8, nhưng CLR không bảo đảm `finally` khi exception không được xử lý | `try/catch` trong `Main`, sheet sai thành `ArgumentException` có tên sheet |
| L5 | [SheetLayoutLocator.cs:162-166](../../../HPFoundation/HPFoundation.Core/Sheets/SheetLayoutLocator.cs#L162-L166) và [MomentFormulaRewriter.cs:27-31](../../../HPFoundation/HPFoundation.Core/Sheets/MomentFormulaRewriter.cs#L27-L31); hằng cột ở Locator và Builder | K1 | Cùng hình dạng công thức viết hai lần, cùng cột viết hai nơi. Đổi một bên thì lỗi ở lúc chạy | Một nơi định nghĩa, `SheetLayout` mang luôn cột |
| L6 | [InputValidator.cs:54-73](../../../HPFoundation/HPFoundation.Core/Sheets/InputValidator.cs#L54-L73) | | Không kiểm tra `EndTributaryLengthM` = nửa bước, `ki` cuối = nửa `ki` giữa, `ColumnWidthM > 0`. 8 sheet đang đúng. Hàng lò xo cuối tìm theo vị trí +1 | Thêm 2 dòng kiểm tra |
| L7 | [Program.cs:112](../../../HPFoundation/HPFoundation.App/Program.cs#L112) | | Thư mục chạy theo giây, tên workbook lặp hai lần trong đường dẫn: tên dài chạm giới hạn 218 ký tự của Excel, hai lần chạy cùng giây ghi đè | Thêm mã ngẫu nhiên ngắn, tên file ngắn cố định |

Lệch plan, không phải lỗi: `SheetProfile` thành `SheetLayoutLocator`, `SheetReader` thành `SheetPreparation`, thư mục chạy theo workbook thay vì sheet. Nên cập nhật plan. `HPFoundation/` chưa có trong phần phạm vi Clean Code và Repository Layout của `CLAUDE.md`.

## Đã kiểm tra, không có lỗi

| Câu hỏi | Kết luận |
|---|---|
| File gốc bị ghi? | Không. `ReadSharedBytes` mở `FileAccess.Read`, chia sẻ đủ, không khoá người khác. Băm đúng byte đã chép, không có race |
| Excel của kỹ sư bị dính? | `CreateInstance` tạo process mới `/automation -Embedding`, PID lấy từ `Hwnd` của chính nó. PID 23740 không đổi qua mọi lần chạy |
| Exception trong `Prepare` có leak? | Không, đã đo với sheet sai |
| Offset khi used range không từ A1 | Đúng, đã đo. Cận mảng 1, `-1` đúng với Excel |
| Mã lỗi ô | `#N/A`, `#DIV/0!` nhận đúng, đã đo |
| Regex rewrite | Neo `^`, `$1F` đúng cú pháp .NET, công thức đã rewrite không khớp lại nên không rewrite hai lần. Không có đường khớp chéo F34 với D39 |
| Neo khớp nhầm | Khớp nguyên văn, đúng cột, phải duy nhất. Không tìm được cách khớp sai trên 8 sheet |
| Lệch nhịp, tham số record | `Take(index + 1)` đúng, `LoadRowCount + 1` đúng, thứ tự tham số `ColumnLoad`, `BeamSection`, `SpringSupport` đúng |
| Đơn vị | Ô đơn vị cạnh `h, b, bc, bd, h1` đều `m` trên 8 sheet. `F128` là T/m |
| Dấu phẩy động trong kiểm tra bước | Đúng. Sai số 1e-13 nhỏ hơn nhiều dung sai 1e-6, `11.700000000000001` qua được |
| `Directory.Build.props`, `HPFoundation.slnx` | Không có vấn đề. `bin`, `obj` đã được `.gitignore` |

## Việc cần làm theo thứ tự

1. H3 + H2 + H1: gom vào factory `ExcelSession.Open`, Job Object, watchdog. Một đợt sửa.
2. H6, H4: nhỏ, sửa ngay, kèm test đúng ô `E110`.
3. Med-1, Med-2, Med-3, H5 phần kiểm tra.
4. Med-4, Med-5, Med-6, Med-7.
5. Med-8 khi test chạy lại được trên máy này.

## Cần người dùng quyết định

1. **MB9 trục 4.15 m:** chèn nút phụ tại tim cột (lò xo giữ lưới 0.1, thêm nút không lò xo) hay dừng và báo kỹ sư. **Khuyến nghị:** chèn nút phụ, vì tải đặt đúng vị trí và lò xo không đổi; validator đổi thành cảnh báo. Quyết định này ảnh hưởng test 118 nút ở phase 2.

**Status:** DONE_WITH_CONCERNS
**Summary:** 0 Critical, 6 High, 8 Medium, 7 Low. File gốc và Excel của kỹ sư an toàn; Excel ẩn có thể mồ côi hoặc treo, kiểm tra `!!!` không đọc đúng ô, MB9 lọt cổng.
**Concerns/Blockers:** Chưa nên đóng phase 1 khi còn High. Unit test vẫn CHƯA TEST. Med-6 là GIẢ ĐỊNH CHƯA XÁC MINH. Cần người dùng chốt cách xử lý MB9.

## Xử lý sau review (2026-11-06)

| Mục | Trạng thái | Kiểm tra |
|---|---|---|
| H1 watchdog | Đã sửa | CHƯA TEST |
| H2 Job Object | Đã sửa | CHƯA TEST |
| H3 `ExcelSession.Open` | Đã sửa | Sai tên sheet OK; lỗi lúc mở CHƯA TEST |
| H4 quét `!!!` và lỗi toàn sheet | Đã sửa | 8 sheet không báo nhầm; ca B30 CHƯA TEST |
| H5 trục lệch lưới | Đã sửa, chặn | MB9 bị chặn đúng |
| H6 `IsReady` | Đã sửa | MB9 không ghi json |
| Med-1, 2, 3, 5, 7, L1, L2, L4 | Đã sửa | Build; hồi quy 8 sheet |
| Med-4 message filter | Để phase 5 | |
| Med-6 `IgnoreRemoteRequests` | Không làm | Có thể lưu vào tùy chọn Excel của người dùng |
| Med-8, L3, L5, L6, L7 | Để sau | |
