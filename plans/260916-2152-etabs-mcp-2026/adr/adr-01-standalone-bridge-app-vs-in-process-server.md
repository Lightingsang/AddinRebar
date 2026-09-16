# ADR-01 — Bridge cho ETABS: app WPF độc lập giữ pipe (option B′), không phải server in-process (option A), không phải plugin trong ETABS

**Ngày:** 2026-09-16 · **Revised 2026-09-16 (red-team #7, #13):** bỏ `AutoLaunchBridge`; giảm nhẹ chi phí = README + `.mcp.json` note + hint engine nêu tên exe · **Status:** Proposed · **Owner:** HPEtabs
**Kế thừa:** [Revit ADR-01 two processes](../../260912-1521-dynamic-revit-mcp-server-2026/adr/) · [AutoCAD plan](../../260913-0000-autocad-mcp-bridge-2026/plan.md) · [Navis ADR-01](../../260915-0824-navisworks-mcp-2026/adr/adr-01-net48-host-multitarget-mcpbridge-core.md)
**Bằng chứng:** [evidence §E2, §E3, §E4, §E7](../research/evidence-on-machine-2026-09-16.md) · [researcher-01 §2, §9](../research/researcher-01-etabs-oapi-facts.md) · [researcher-02 §1–§3](../research/researcher-02-mcpshared-seam-and-autocad-mirror.md) (khuyến nghị "in-process, no pipe" của researcher-02 = option A, **bị loại** ở đây) · CLAUDE.md "every Claude Code session spawns one" (mục HPAutoCad › Publish & deployment)

## Context

- ETABS.exe là **COM LocalServer** (`HKLM\SOFTWARE\Classes\CLSID\{e4f6d00f-…}\LocalServer32 = …\ETABS 22\ETABS.exe`, E3). Client attach qua `Helper.GetObject("CSI.ETABS.API.ETABSObject")` → oleaut32 `GetActiveObject`/ROT bằng P/Invoke bên trong `ETABSv1.dll` (E3) — chạy được từ .NET 5+ không cần shim. Wrapper `ETABSv1.dll` là netstandard2.0, 0 `[ComImport]` (E2) → ranh giới COM nằm **trong** wrapper.
- Không có cơ chế add-in nạp lúc khởi động như Revit/AutoCAD/Navisworks. `cPluginContract` tồn tại nhưng: chạy trong host .NET 8 của ETABS (E4), gọi từ menu Tools, **phải** gọi `cPluginCallback.Finish` trước khi trả về (CHM › "Information for Plugin Developers" § "The cPlugin Class"), OAPI trong-process **giống hệt** OAPI ngoài-process và cũng **không có transaction** → không thêm được gì.
- Engine đã có seam ở **hai** phía (E7 + researcher-02 §1): server tiêu thụ `IRevitBridgeClient` (đăng ký `McpShared/HPRebar.Mcp.Server.Core/Bootstrap/McpServerHost.cs:49`, inject `Services/ExecuteCodeService.cs:21`); bridge engine tiêu thụ `IBridgeExecutor` (`McpShared/HPRebar.McpBridge.Core/Pipe/IBridgeExecutor.cs:10–38`).
- Câu hỏi của user "`IBridgeExecutor` có phải seam cho option A không?" → **CÓ** (theo đúng hai citation trên): A là khả thi về kỹ thuật. Vấn đề của A không phải seam mà là **vòng đời tiến trình**.

## Decision

### 1. Chọn **B′**: `HPEtabs.McpBridge` = app WPF desktop (net8.0-windows, `UseWPF`), user tự mở bên cạnh ETABS

- Host `McpBridgeHost` (ctor 7 tham số `McpShared/HPRebar.McpBridge.Core/Host/McpBridgeHost.cs:35–36`: executor, settings, store, hostVersion "22", pipeName `PipeNaming.For("etabs", 22)`, hostName "ETABS", methodPrefix "etabs.") → tự tạo `PipeListener` + `RequestDispatcher` (`McpBridgeHost.cs:46`; `RequestDispatcher` ctor `Pipe/RequestDispatcher.cs:31`, dispatch theo suffix nên `etabs.execute` ≡ `revit.execute`).
- `EtabsExecutor : IBridgeExecutor` sở hữu **một** attachment COM tới ETABS đang chạy (ADR-02 §5, ADR-04 §1).
- Server `HPEtabs.Mcp.Server` = bản sao hình dạng `HPAutoCad.Mcp.Server` (profile + 4 core tool + prompts/resources + seeds), nói chuyện qua pipe bằng `BridgeClient` **không sửa**; **không bao giờ** tham chiếu `ETABSv1.dll` (ADR-03 §2).
- Cửa sổ của bridge app = cửa sổ trạng thái (mirror `HPNavis/HPNavis.McpBridge/View/NavisBridgeStatusView.xaml`, 2 checkbox) + nút Attach/Detach (`GetObject` only, không pid picker — ADR-02 §5, phase 2).

### 2. Option A (server host engine in-process, không pipe) — khả thi, **bị loại**

Những gì A cần (để ghi nhận là có thể làm): một `IRevitBridgeClient` in-process gọi thẳng `RequestDispatcher.HandleLineAsync` (hoặc bỏ qua envelope, gọi `IBridgeExecutor` trực tiếp); một cửa sổ WPF (opt-in, trạng thái) **bên trong exe console stdio**; một named mutex liên tiến trình để N server không attach song song; `<Reference ETABSv1>` có điều kiện trong server **và** server tests. Vì sao thua:

1. **N phiên Claude Code = N server** ("every Claude Code session spawns one" — CLAUDE.md) → N attachment COM, N cửa sổ opt-in, N Roslyn cache, và **không có gì tuần tự hoá** các run lên một model **không có transaction**. B′: một chủ sở hữu, một hàng đợi (`MainThreadQueue`), một opt-in.
2. Cổng opt-in/destructive phải nằm trong **tiến trình do người dùng sở hữu và tự khởi động**, không phải tiến trình do harness AI spawn/restart tuỳ ý (settings `ExecutionEnabled` OFF mỗi lần khởi động chỉ có nghĩa khi "khởi động" là hành động của người — `BridgeSettings.cs:10`).
3. Server + server tests build/chạy trên máy **không có ETABS** (chỉ bridge và seed compile-check cần `ETABSv1.dll`) — cùng văn hoá test 3 host cũ (ADR-03 §2–3).
4. Tái dùng nguyên văn `McpBridgeHost`, `PipeListener`, `RequestDispatcher`, `McpBridgeStatusViewModel` (`McpShared/HPRebar.McpBridge.Core/ViewModel/McpBridgeStatusViewModel.cs`), cửa sổ WPF của bridge AutoCAD/Navis (copy), `BridgeSettingsStore`, `AuditLogger` — **0 sửa engine ngoài hằng/profile/hint** (phase 0).
5. Cùng mô hình tinh thần với Revit/AutoCAD/Navis cho user và cho mô tả tool.

### 3. Plugin trong ETABS (`cPluginContract`) — bị loại

Phải là assembly .NET 8 nạp vào host của ETABS (E4, researcher-01 §9); chỉ khởi động từ menu Tools; phải gọi `Finish` (ETABS chờ vô hạn nếu không); OAPI trong-process không khác ngoài-process, cũng không có transaction → chi phí (load context của ETABS, vòng đời Finish, không tách được khỏi ETABS treo) không mua được gì.

### 4. Chi phí B′ và giảm nhẹ

- Thêm **một exe phải mở** bằng tay. Giảm nhẹ (red-team #13 bỏ `AutoLaunchBridge`: server spawn exe từ path trong repo mâu thuẫn §2 #2): `HPEtabs/README.md` + ghi chú cạnh entry `.mcp.json` + hint engine `HostProfile.BridgeNotConnectedHint` (phase 0, red-team #7) nêu **tên exe** — "Start HPEtabs.McpBridge.exe beside ETABS 22, click Attach and tick 'Allow AI code execution' (pipe hpetabs-mcp-22)".
- Không mất gì so với 3 host cũ: checkbox "Allow AI code execution" (OFF mỗi lần mở app, không persist — `BridgeSettingsStore.Load` ép `ExecutionEnabled=false`, `BridgeSettingsStore.cs:43`), cửa sổ trạng thái, audit tail — đều là cửa sổ của bridge app.

## Consequences

- (+) Engine `McpShared` chỉ thêm hằng/profile/DTO/hint (phase 0); `BridgeClient`, `RequestDispatcher`, `McpBridgeHost` không đổi hành vi → gate byte-identical 3 host dễ chứng minh.
- (+) Bridge app kiểm soát được thread COM của mình (ADR-04 §1) — không phụ thuộc Idle event của host.
- (+) Server/test không cần ETABS; harness live chỉ cần ETABS + bridge app.
- (−) Ba tiến trình thay vì hai: thêm bước "mở bridge" trong README/`.mcp.json`; lỗi "bridge not connected" phải nêu tên app chứ không phải "enable add-in".
- (−) Bridge app đóng = attachment mất; ETABS đóng = proxy COM chết → `-32003` "not attached" tới khi Attach lại (ADR-02 §5).
- (−) Nếu user mở 2 bridge app → pipe in use; cửa sổ thứ hai báo "already in use" (mirror AutoCAD `RequestDispatcher.HostName/HostVersion` message; log sink `shared: true`).
- (−) Bridge publish **folder** (không single-file — Roslyn cần `Assembly.Location`, ADR-05 §3).

## Alternatives rejected

- **A — in-process trong server exe:** §2 trên.
- **Plugin `cPluginContract` trong ETABS:** §3 trên.
- **Bridge = Windows service / tray app tự khởi động cùng Windows:** vi phạm "opt-in OFF mỗi lần khởi động do người dùng"; YAGNI.
- **Bridge `CreateObject` tự mở ETABS ẩn:** tốn seat/instance ẩn `[chưa xác minh]`, AI làm việc trên model không ai nhìn — loại (ADR-02 §5).

## Open items `[chưa xác minh]`

- Hai bridge app cùng user: pipe thứ hai fail như Navis (`PipeListener` net8 `CurrentUserOnly`) — phase 4 manual.
