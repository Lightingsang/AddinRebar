# Phase 1 spike — AutoCAD 2026 (R25.1.0, .NET 8), 2026-09-14 13:14

**Cách chạy:** bundle deploy bằng `dotnet build HPAutoCad/HPAutoCad.slnx -c Debug` → `acad.exe /nologo /product ACAD /language "en-US" /b spike.scr` (script = một dòng `HPMCPSPIKEQUIT`) → report `%LocalAppData%\HPAutoCad\McpBridge\logs\spike-report.md` sau ~200 s (AutoCAD khởi động ~3 phút lần đầu) → harness kill `acad.exe` (chuỗi `_.CLOSE _N _.QUIT` không kết thúc tiến trình — xem ghi chú). Không cần thao tác tay.

## Kết quả (nguyên văn `spike-report.md`)

```
main thread 1; AutoCAD 25.1.0.0; quitWhenDone=True
bridge 'HPAutoCad.McpBridge' · Roslyn 5.9.0.0 'HPAutoCad.McpBridge' · Immutable 10.0.0.0 'HPAutoCad.McpBridge' · AcDbMgd 25.1.0.0 'Default'
PASS 1 load context
script returned: C:\Users\...\AutoCAD 2026\R25.1\enu\Template\acad.dwt | layer LayerTableRecord | 25.4
PASS 2 roslyn with document
window shown, IsVisible=True, dispatcher thread 1
PASS 3 wpf modeless window
background thread 20
idle handler on thread 1 (main=True), quiescent=True, line 25E committed after 963 ms
PASS 4a idle from background thread
call returned after 12914 ms, blocking=True; callback on thread 20 (main=False), quiescent=False, 29 ms after the call
PASS 4b ExecuteInApplicationContext from background thread
during LINE: quiescent=False
after ESC: quiescent=True
PASS 5 busy: command in progress
6 modal dialog while idle: NOT TESTED (unattended run cannot dismiss a dialog)
```

Loader log (`loader.log`): 11 assembly nạp vào context `HPAutoCad.McpBridge` theo đúng deps.json (Core, Contracts, Serilog×2, System.Text.Json 10.0, Roslyn ×4 5.9.0, Immutable 10.0, Reflection.Metadata 10.0); không có `Ac*`/`Ad*` nào bị nạp lại. `MCP scripting self-check OK in 1577 ms: autocad 25.1.0.0 | units Millimeters` — chạy trong `IExtensionApplication.Initialize`, trước khi có drawing.

## Câu hỏi → quyết định

| # | Câu hỏi (ADR) | Kết quả | Quyết định |
|---|---|---|---|
| 1 | Bundle autoloader nạp Loader (ADR-05) | ✅ `LoadOnAutoCADStartup`, `SeriesMin/Max R25.1`, `Platform="AutoCAD"` — nạp, không dialog SECURELOAD (không thấy prompt trong run unattended) | Giữ manifest |
| 2 | Roslyn 5.9 trong ALC riêng, không đụng Roslyn 4.10 / Immutable 8.0 của AutoCAD (ADR-05 §1) | ✅ `Roslyn 5.9.0.0 'HPAutoCad.McpBridge'`, `Immutable 10.0.0.0 'HPAutoCad.McpBridge'`, `AcDbMgd 'Default'`; compile + run có `doc/db/ed/tr/units` OK | **ALC riêng = thiết kế chính thức**; phương án B (compile trong server) không cần |
| 3 | WPF modeless từ ALC riêng | ✅ `Core.Application.ShowModelessWindow(Window)`, dispatcher thread 1 | Status window phase 2 dùng cách này |
| 4a | `Application.Idle +=` từ thread ngoài; handler có chạy trên main thread, quiescent, lock + transaction (ADR-02 đường chính) | ✅ subscribe từ thread 20 không ném; handler trên thread 1, `IsQuiescent=true`, `LockDocument` + `StartTransaction` + `AppendEntity` + `Commit` OK, 963 ms | **Idle one-shot = đường chính** (ADR-02 xác nhận) |
| 4b | `ExecuteInApplicationContext` từ thread ngoài (ADR-02 đường phụ) | ❌ callback chạy **trên chính thread gọi (20)**, không phải main; `IsQuiescent=false`; lời gọi block 12,9 s | **Loại** khỏi thiết kế; giữ trong guard deny-list |
| 5 | AutoCAD đang trong command (`LINE` chờ điểm) | ✅ `IsQuiescent=false` khi LINE chờ; ESC qua `SendStringToExecute("\x1B\x1B")` → `true` | Gate `IsQuiescent` + grace + `-32002` như ADR-02 |
| 6 | Modal dialog khi Idle | ⚪ chưa test (unattended) | Kiểm tay ở phase 5 (mở dialog `OPTIONS` rồi gọi execute) |

## Ghi chú
- `_.CLOSE _N _.QUIT` gửi qua `SendStringToExecute` **không** đóng AutoCAD (tiến trình còn sống > 60 s; có thể prompt lưu hoặc `QUIT` bị chặn) → harness `Stop-Process`. Không ảnh hưởng bridge; phase 2 không cần quit.
- `%AppData%\HPAutoCad\McpBridge\settings.json` chưa tồn tại (chỉ tạo khi Save) — đúng hành vi.
- Lần đầu khởi động AutoCAD với bundle mất ~3 phút (AutoCAD cold start), self-check 1,6 s.
