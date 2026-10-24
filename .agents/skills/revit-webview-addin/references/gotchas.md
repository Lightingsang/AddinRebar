# Bẫy đã gặp thật (Revit 2026, HP BIM Toolkit)

| # | Hiện tượng | Nguyên nhân | Cách xử lý |
|---|---|---|---|
| 1 | Log add-in ghi vào file của add-in khác | Các add-in mang Serilog dạng DLL rời dùng chung một bản trong Revit → `Log.Logger` ai ghi sau thắng | `Serilog.Core.Logger` riêng truyền qua constructor; không dùng `Log.Logger` (HPRebar không bị vì Serilog đã ILRepack) |
| 2 | Revit treo "Not Responding" khi pane nhận message đầu tiên (ngẫu nhiên) | JIT lần đầu bind assembly lười qua `AddInLoadContext.Load` của Revit, hàm này duyệt mọi assembly và kẹt khi add-in khác đang nạp DLL (dump: `System.Text.Json` ↔ `EPPlus.Interfaces`) | `DependencyPreloader` ở `OnStartup`: nạp reference, resolve mọi TypeRef của assembly trong thư mục toolkit, JIT trước code toolkit (best effort, ~0,4 s) + `Dispatcher.BeginInvoke` ra khỏi callback COM. Thêm assembly mới vào thư mục toolkit là tự được phủ |
| 3 | Mọi request TIMEOUT liên tiếp | `ShowElements` mở hộp thoại modal "search closed views" | Chỉ zoom phần tử view hiện hành thấy |
| 4 | Revit đứng 18 s | `SetElementIds` ~20 000 phần tử | Trang xác nhận trên 5 000 |
| 5 | Category lạ đứng đầu thống kê (Materials, `<Sketch>`…) | Đếm cả category ẩn/bookkeeping | `IsVisibleInUI`, không subcategory, Model/Annotation, loại Materials/ProjectInformation/Sheets/ColorFillSchema/PipeSegments |
| 6 | Deploy lỗi "file in use" | Revit đã nạp add-in giữ DLL | Đóng Revit hoặc đổi tên DLL khóa `.locked-<giờ>` |
| 7 | Harness chờ mãi | `Start-Process -Wait` chờ cả cây tiến trình; WMI start → "Failed to open document" | `cmd /c start` |
| 8 | UIA treo | Duyệt cây Chromium của pane đang mở | Kiểm cổng CDP trước; `revit-uia.ps1` bỏ qua cây Chromium |
| 9 | Mở nhầm file dự án thật | SendKeys vào hộp thoại Open lọt vào danh sách file | `fill-dialog` (WM_SETTEXT + BM_CLICK) |
| 10 | Đóng tài liệu kẹt | Bản sao bị Dynamo/edit sửa → hộp thoại *Save File* | Trả lời *No* |
| 11 | Drift test TS hỏng trên máy khác | Raw string C# mang CRLF của file nguồn | `ReplaceLineEndings("\n")` khi sinh |
| 12 | MSI không có cây feature | WixSharp 2.14 bỏ Feature khi file ở cấp 3 | Cây 2 cấp năm → add-in |
| 13 | Bước MSI của `pack` lỗi WIX0103 | Icon dùng đường dẫn tương đối git root | Icon copy cạnh `Installer.exe` |
| 14 | `xUnit2014` khi test hàm trả Task ném đồng bộ | Analyzer coi lambda là async | `Record.Exception(Action)` |
| 15 | Log Dynamo báo `FileLoadException DSPythonNet3Extension` | Package PythonNet3 lệch DynamoCore, có trước toolkit | Không liên quan; so log cũ trước khi đổ lỗi cho add-in |
