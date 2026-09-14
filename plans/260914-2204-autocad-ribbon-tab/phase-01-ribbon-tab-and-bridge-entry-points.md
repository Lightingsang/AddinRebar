---
phase: 1
title: "Ribbon tab trong loader + entry point additive trong bridge"
status: completed
priority: P2
effort: "4h"
dependencies: []
---

# Phase 1: Ribbon tab trong loader + entry point additive trong bridge

## Overview
Tab Ribbon "MCP AutoCAD" (id `HPAUTOCAD_MCP_TAB`) do `HPAutoCad.McpBridge.Loader` tạo bằng `Autodesk.Windows`; mọi nút gọi delegate bridge hiện có hoặc 4 delegate mới (chỉ string/bool/Action qua ALC). Idempotent khi Initialize lại / đổi workspace; dọn khi Terminate.

## Requirements
- Functional: 3 panel — **Kết nối** (Bảng điều khiển → `show`; Bật listener → `start`; Tắt listener → `stop`; Trạng thái → `status` in ra command line hoặc alert khi không có bản vẽ; text trạng thái sống), **Công cụ** (Sao chép script cuối → `copyLastScript`; Mở tools-library → `path("library")`), **Thiết lập** (Mở nhật ký → `path("logs")`; Mở audit → `path("audit")`; toggle Tự khởi động listener → `autoStart.get/set`; Hướng dẫn → `Contents\README.md` trong bundle). Nhãn/tooltip tiếng Việt; tooltip nêu command tương đương.
- Non-functional: không nút giả; không sửa bản vẽ; không chờ đồng bộ; không đụng server exe; tab/panel/nút không trùng sau `Initialize` lặp, WSCURRENT, CUI reload; `Terminate` gỡ tab + handler; file < 300 dòng, namespace theo folder (`HPAutoCad.McpBridge.Loader.Ribbon`), không plan-ref trong comment.

## Architecture
```text
AutoCAD Ribbon (AdWindows, default ALC)
  └─ McpRibbonTab.EnsureCreated()  ── FindTab(id) guard ──► RibbonTab HPAUTOCAD_MCP_TAB
        ├─ panel Kết nối: RibbonButton ×4 + RibbonLabel (status)  ── RibbonCommandHandler(Action)
        ├─ panel Công cụ: RibbonButton ×2
        └─ panel Thiết lập: RibbonButton ×3 + RibbonToggleButton
             │
             ▼  BridgeLoaderApplication.Invoke(key, args)   (delegate, BCL types only)
BridgeEntry (isolated ALC): show | start | stop | status | dispose
                            + status.subscribe: Func<Action<string,string>, Action>  (kind, text) → unsubscribe
                            + copyLastScript: Func<string>
                            + autoStart.get: Func<bool> · autoStart.set: Action<bool>
                            + path: Func<string,string>  ("logs" | "audit" | "library" | "settings")
```
- Trạng thái sống: bridge gọi callback trên `StateChanged` với `kind` = `NotStarted|Stopped|Listening|Connected|Busy|Error` (Connected khi `Status==Connected || HasClient`), `text` = câu ngắn tiếng Việt + pipe + opt-in. Loader cập nhật `RibbonLabel.Text` qua `Dispatcher` của `ComponentManager.Ribbon` (`InvokeAsync`), không block.
- Vòng đời: `Initialize` → nếu `ComponentManager.Ribbon != null` tạo ngay, else `ComponentManager.ItemInitialized += Once`; `Application.SystemVariableChanged` (Name == "WSCURRENT") → `Application.Idle` one-shot → `EnsureCreated()`; `Terminate` → `Remove()` + huỷ subscribe + unhook.
- Lỗi: mọi handler try/catch → `LoaderLog` + `Editor.WriteMessage` khi có document, `Application.ShowAlertDialog` khi không; bridge chưa nạp → nút vẫn hiện nhưng `IsEnabled=false` + tooltip "Bridge chưa khởi động — xem loader.log" (trừ Mở nhật ký / Hướng dẫn).
- Icon: `RibbonIcons` trả `DrawingImage` (GeometryDrawing, 32 px + 16 px) — plug, play, stop, info, clipboard, folder, log, gear/toggle, book. `Freeze()`.

## Related Code Files
- Modify: `HPAutoCad/HPAutoCad.McpBridge.Loader/HPAutoCad.McpBridge.Loader.csproj` (`UseWPF`; copy `..\README.md` → `Contents\README.md` trong `DeployBundle`)
- Modify: `HPAutoCad/HPAutoCad.McpBridge.Loader/BridgeLoaderApplication.cs` (gọi `McpRibbonTab.Install()/Uninstall()`)
- Modify: `HPAutoCad/HPAutoCad.McpBridge.Loader/BridgeLoaderCommands.cs` (tách `Run` thành `BridgeActions.Run(key, args)` dùng chung với Ribbon)
- Create: `HPAutoCad/HPAutoCad.McpBridge.Loader/BridgeActions.cs`
- Create: `HPAutoCad/HPAutoCad.McpBridge.Loader/Ribbon/McpRibbonTab.cs`, `Ribbon/RibbonCommandHandler.cs`, `Ribbon/RibbonIcons.cs`, `Ribbon/RibbonStatusPresenter.cs`
- Modify: `HPAutoCad/HPAutoCad.McpBridge/BridgeEntry.cs` (4 entry point; nếu > 300 dòng tách `BridgeEntry.Ribbon.cs` partial)
- Không đụng: `McpShared/`, `HPRebar/`, `HPAutoCad.Mcp.Server`, `Bundle/PackageContents.xml` (Platform/Series giữ nguyên)

## Implementation Steps
1. csproj loader: `<UseWPF>true</UseWPF>`; DeployBundle copy `README.md` (HPAutoCad) vào `Contents\`. Build `-p:DeployBundle=false` xác nhận `Autodesk.Windows` resolve từ NuGet (AdWindows).
2. `BridgeEntry`: thêm 4 entry point; `status.subscribe` đăng ký `host.StateChanged`, gọi ngay lần đầu, trả `Action` huỷ; `copyLastScript` dùng `Clipboard.SetText(host.LastRun.Source)` trên main thread; `autoStart.*` qua `host.AutoStartListener`; `path` trả thư mục từ `store.Directory`/`LogDirectory`/`%AppData%\HPAutoCad\McpServer\tools-library`.
3. `BridgeActions.Run` (loader): gộp logic của `BridgeLoaderCommands.Run`; commands và Ribbon cùng gọi.
4. `Ribbon/RibbonIcons.cs`, `Ribbon/RibbonCommandHandler.cs` (ICommand, `CanExecute` = bridge đã nạp trừ nút "always").
5. `Ribbon/McpRibbonTab.cs`: `Install()` (Ribbon null → ItemInitialized), `EnsureCreated()` (FindTab guard, build 3 panel), `Remove()`, hook WSCURRENT → Idle one-shot. `Ribbon/RibbonStatusPresenter.cs`: subscribe, Dispatcher update, unsubscribe.
6. `BridgeLoaderApplication.Initialize` cuối cùng gọi `McpRibbonTab.Install()` (bọc try/catch riêng — Ribbon lỗi không được làm bridge không nạp); `Terminate` gọi `Uninstall()` trước `dispose`.
7. Build Debug `-p:DeployBundle=false` + Release: 0 warning. `dotnet test HPAutoCad.Mcp.Server.Tests` 58/58 (không đổi).

## Success Criteria
- [x] Build Debug/Release 0 warning; tests 58/58
- [x] `git diff --stat -- McpShared HPRebar` rỗng; PackageContents.xml chỉ đổi version 0.1.0 → 0.2.0 (README thêm vào Contents)
- [x] Mỗi nút map tới delegate có thật (bảng ánh xạ trong `HPAutoCad/README.md`); không nút giả; opt-in không có trên Ribbon
- [x] Code: `FindTab` guard, ItemInitialized một lần, WSCURRENT → Idle, Terminate dọn; handler try/catch; label cập nhật qua Dispatcher — verified live: tab tự tạo lại sau 2 lần đổi workspace, luôn đúng 1

## Risk Assessment
- `RibbonToggleButton`/`RibbonLabel` API khác kỳ vọng → fallback `RibbonButton` với text đổi theo trạng thái.
- `Clipboard.SetText` ngoài STA → gọi trong delegate trên main thread (Ribbon click = main thread).
- `Application.SystemVariableChanged` không bắn khi CUI reload bằng `CUILOAD` → chấp nhận: người dùng chạy `HPMCPBRIDGE`/command bất kỳ → không tự tạo lại; ghi vào giới hạn (hoặc thêm `ComponentManager.ItemInitialized` lâu dài với FindTab guard — chọn khi implement, đo bằng harness).
