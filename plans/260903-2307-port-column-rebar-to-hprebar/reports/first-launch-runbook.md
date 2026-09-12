# Runbook — lần chạy đầu tiên trong Revit

2026-09-04. Add-in **chưa từng được Revit load**: 0 hit `HPRebar` trong 3 journal Revit 2026 gần nhất, thư mục `%LocalAppData%\HPRebar\logs\` chưa tồn tại.

Pre-flight đã chạy theo troubleshoot matrix của `/revit-debug`. Mọi thứ kiểm tĩnh được đều xanh; dưới đây là những gì còn lại và cách đọc kết quả.

## Trạng thái sẵn sàng

| Mục kiểm | Kết quả |
|---|---|
| `UseWPF` / `DeployAddin` / `LaunchRevit` / `IsRepackable` / `EnableDynamicLoading` | ✅ đủ 5 trong csproj |
| Manifest trùng ở `%ProgramData%` (gây `CreatePanel` throw) | ✅ không có — chỉ `%AppData%`, mỗi version 1 file |
| `.addin` → `<Assembly>HPRebar\HPRebar.dll` | ✅ file tồn tại, `FullClassName` khớp `HPRebar.Application` |
| DLL deploy vs source | ✅ tươi — `2026-09-04 12:18`, 1.77 MB |
| Modal `Owner = Application.MainWindowHandle` | ✅ `ColumnRebarCommand.cs:91` |
| `[Transaction(TransactionMode.Manual)]` | ✅ |
| `CanExecute` + `[NotifyCanExecuteChangedFor]` | ✅ |
| Style/brush key `DynamicResource` | ✅ 27/27 XAML + 6/6 canvas, có ở cả Dark lẫn Light |
| `cd build && dotnet run` | ✅ 3 module pass, 48s, `0.0.4-RebarVersion1.1` |
| `cd build && dotnet run -- test` | ✅ TestProjectModule pass |
| xUnit | ✅ 102/102 |

## Đã sửa trong lượt này

`Application.OnStartup` không có try/catch. Nếu `CreateRibbon` ném (pack URI sai, trùng tên panel…), exception thoát lên Revit → add-in bị bỏ **không để lại gì ngoài journal**, và Serilog không ghi được dòng nào vì lỗi xảy ra trước mọi lệnh log.

Giờ bọc try/catch: ghi `Log.Fatal` + flush rồi mới rethrow. Lỗi lần đầu sẽ nằm trong log file thay vì bốc hơi.

## Chạy

```bash
# Revit phải ĐÓNG (đang mở sẽ khoá DLL)
cd HPRebar
dotnet build HPRebar.slnx -c Debug.R26
```

Rồi mở Revit 2026 → mở/tạo project bất kỳ → tab **HPRebar**.

Debug có breakpoint: mở `HPRebar.slnx` trong Rider/VS, chọn config `Debug.R26`, F5 (`LaunchRevit=true` tự mở Revit và attach).

## Đọc kết quả theo thứ tự

**1. Có thư mục `%LocalAppData%\HPRebar\logs\` không?**

Đây là phép thử đáng giá nhất và nó trả lời câu hỏi khó nhất một cách gián tiếp.

`deps.json` vẫn liệt kê 9 assembly rời (`CommunityToolkit.Mvvm.dll`, `Serilog.dll`, `HPRebar.Core.dll`…) nhưng thư mục deploy **chỉ có `HPRebar.dll`** — ILRepack đã gộp hết và rewrite IL reference. Đây là hành vi bình thường của Nice3point SDK nhưng **chưa bao giờ được chứng minh lúc chạy**; nếu gộp hỏng thì triệu chứng đúng bằng dòng `FileNotFoundException: CommunityToolkit.Mvvm` trong matrix.

`CreateLogger()` chạy đầu tiên và cần `Serilog` + `Serilog.Sinks.File` — cả hai đều là assembly được gộp. Nên:

- **Log file xuất hiện** → ILRepack gộp OK, assembly loading OK.
- **Không có log file, add-in không hiện** → đọc journal: `%LocalAppData%\Autodesk\Revit\Autodesk Revit 2026\Journals\journal.<n>.txt`, tìm `HPRebar`.

**2. Ribbon có 2 panel không?** Tab HPRebar phải có panel `Commands` (nút `Execute`, cũ) và panel `Rebar` (nút `Column Rebar`).

**3. Click `Column Rebar`** → chọn ≥1 cột kết cấu → dialog 8 tab.

## Thứ tự soi khi có lỗi — xếp theo rủi ro

| # | Thứ | Vì sao đứng đây | Triệu chứng khi hỏng |
|---|---|---|---|
| 1 | ILRepack merge | Chưa chạy lần nào; hỏng là chết ngay lúc load | Add-in không hiện, không log, journal có `FileNotFoundException` |
| 2 | `DimensionCreator.ToLinearReference` | Hack `SURFACE→LINEAR` undocumented, rvtdocs không có gì | Dimension mặt cắt **bị bỏ qua im lặng** — chỉ log warning báo |
| 3 | Hướng rải đai | 3 cờ bool của `SetLayoutAsNumberWithSpacing` được doc mô tả ý nghĩa nhưng không nói tổ hợp nào cho hướng nào | Đai rải ngược trục Z — nhìn mặt đứng thấy ngay |
| 4 | `PointMapper` | Gốc toạ độ = góc Tây-Nam cột đáy chiếu lên datum | Toàn bộ thép lệch hẳn vị trí, không phải lệch nhẹ |
| 5 | `ColumnSpecEditor.Load` thứ tự gán | Đã sửa 1 NRE ở đây; đáng xác nhận dialog mở được lần đầu | Crash ngay khi mở dialog |

Log file là nơi đọc trước tiên: mọi thứ "đã bỏ qua" đều ghi warning ở đó, không hiện lên UI.

## Chưa mở khoá được

16 test TUnit vẫn skip — thiếu `HPRebar/HPRebar.Tests/Fixtures/column-stack-2-storey.rvt`. Spec dựng model ở `HPRebar/HPRebar.Tests/Fixtures/README.md`.
