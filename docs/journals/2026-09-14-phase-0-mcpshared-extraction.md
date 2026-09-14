# Phase 0: Tách McpShared khỏi HPRebar — Đặt quả bom kiến trúc trước khi phát hiện

**Date**: 2026-09-14 09:45  
**Severity**: Medium  
**Component**: MCP Server / McpShared / HPAutoCad scaffold  
**Status**: Resolved (fixes committed 876c3d6)

## Bối cảnh

Ngày 2026-09-14 user phát hiện ràng buộc mới: mỗi MCP (Revit + AutoCAD) phải ở folder top-level riêng, không chung folder. ADR-01 lên kế hoạch "một exe + host profile", trái ràng buộc này. Phải dịch chuyển: tách mã chung thành `McpShared/`, `HPRebar/` giữ Revit MCP, `HPAutoCad/` mới. Phase 0 thực hiện 9 commits:
- Di chuyển `HPRebar.Mcp.Contracts` + `HPRebar.McpBridge.Core` → `McpShared/`
- Tách `HPRebar.Mcp.Server.Core` (engine mới) khỏi exe, move test
- Tạo scaffold `HPAutoCad/` với `.slnx`, `global.json`
- Fix pipeline `HPRebar/build/` để không bắt nhầm `.slnx` khác

Đầu ra: 34 tools byte-identical (wire compat ✓), 176 test xanh (159 original + 17 new).

## Đã làm

**Commits (9 tổng):**
1. `8a1144f` — move Contracts/McpBridge.Core → McpShared/
2. `96ec3a9` — make McpBridge.Core host-neutral
3. `89c3928` — extract HPRebar.Mcp.Server.Core (registry engine)
4. `8a1144f` — resolve configurations từ HPRebar.slnx
5. `0fa3985` — move engine tests + add HostNeutralityTests
6. `bf680f4` — scaffold HPAutoCad folder
7. `884a229` — docs describe McpShared/HPAutoCad
8-9. `876c3d6` — fix 2 major findings

**Gate:** tools/list byte-identical, 171 test pass (66 McpShared + 105 HPRebar), Debug.R26/R23 build.

## Quyết định

- Tên assembly giữ `HPRebar.*` (không rename sang `HPMcp.*` tại phase 0 — tránh churn 90 file `using`, dành làm cleanup sau)
- McpShared là library trung lập, chỉ chứa Contracts + Bridge.Core + Server.Core (bootstrap, registry, pipe client)
- Seed Revit vẫn nhúng trong exe Revit (không di chuyển)
- Registry root per-exe qua `IHostProfile.ProductFolder` (HPRebar = HPRebar, AutoCAD = HPAUTOCAD)

## Sự cố & Bài học — Hai Sự thật Đau

### Sự cố 1: SynchronizationContext vô hình (F1 — major)

**Vấn đề:**  
Revit's `ExternalCommand` không push `SynchronizationContext` — Nice3point toolkit chỉ dùng `DispatcherFrame` cho async. WPF luôn restore context sau `Dispatcher.Invoke`. Trong pipe thread, `SynchronizationContext.Current` = null, fallback post-sync → `Refresh()` chạy trên pipe thread → `StateChanged?.Invoke()` ở handler gọi `NotifyCanExecuteChanged` cross-thread → exception. **Nhưng exception này ở ngoài `try{}`** — _busy = 1 forever → bridge wedged ("Another script still running") cho đến restart Revit.

Tính toàn: không phát hiện ở phase 0 vì bridge cũ không deploy lại. Nếu AutoCAD server chạy thử qua profile `HostId=autocad`, lỗi này sẽ âm thầm khóa executor ngay lần đầu.

**Fix:**  
- Inject dispatcher làm **required** parameter (`Action<Action>` cho `onUiThread`), loại bỏ silent fallback
- Move `StateChanged?.Invoke()` vào trong `try` ở handler
- Revit command pass `Dispatcher.CurrentDispatcher.InvokeAsync`

**Bài học:** Không bao giờ giả sử `SynchronizationContext.Current` trên Revit main thread nếu caller là `ExternalCommand`. Fallback silently chạy ở sai thread thường tệ hơn compile error.

### Sự cố 2: Options Binder ghi lại computed getter (F2 — major)

**Vấn đề:**  
`Microsoft.Extensions.Configuration` binder (v10.0.12) khi `Bind(section)` enumerates mọi settable property và **ghi getter value lại** nếu section có ≥1 key. Computed getter `LibraryPath` = `$%AppData%\{ProductFolder}\McpServer\…` dùng default `ProductFolder="HPRebar"`, binder ghi lại → pinned. Later `PostConfigure` đổi `ProductFolder="HPAUTOCAD"` không có tác dụng.

Probe:
- AutoCAD profile + `Registry:PublishPolicy=manual` bind → `LibraryPath = %AppData%\**HPRebar**\McpServer` (sai)
- AutoCAD profile + `Bridge:ConnectTimeoutMs` bind → `PipeName = hprebar-mcp-r2026` (Revit pipe, sai)

**Phase 3 (AutoCAD launch) sẽ share registry DB + pipe với Revit silently** — tính toàn hoàn toàn mất.

**Fix:**  
Set profile trước bind: `.Configure(o => o.HostId = profile.HostId).Bind(section).PostConfigure(…)`. Add 4 regression test: bind partial config với product khác → path chứa tên sản phẩm đúng.

**Bài học:** Computed getter + data binder kết hợp hạn chế. Đặc biệt, nếu binder chạy trước `PostConfigure`, giá trị cuối cùng không phải giá trị setter. Phải test cross-concern: config + initialization order + profile swap.

## Số liệu

| Kiểm tra | Kết quả |
|---|---|
| `dotnet build McpShared.slnx -c Debug` | ✅ Pass (2 xUnit1051 warnings) |
| `dotnet test` (HPRebar.Mcp.Server.Core.Tests) | ✅ 66/66 pass |
| `dotnet test` (HPRebar.Mcp.Server.Tests) | ✅ 105/105 pass |
| `tools/list` byte-identical before/after | ✅ 34 tools identical |
| Debug.R26 build | ✅ Pass |
| Debug.R23 build | ✅ Pass |
| Wire compat (`get_revit_context` dryRun) | 🟡 Claimed but not evidenced in reports |
| Code review score | 6.5/10 (2 major + 14 minor findings) |

## Tiếp theo

1. Commit fix F1/F2 lên phase-00 file.
2. Chạy `tools/list` lần 3 để verify sau fix.
3. Phase 1: AutoCAD loader + ALC spike (cần AutoCAD closed để deploy bundle).
4. Dọn nit: rename FakeRevitExecutor → FakeBridgeExecutor, move GuardProfile.Autocad sang HPAutoCad folder.

**Căn cứ:** commit ranges `1931e16..HEAD` (9 commits), code review 6.5/10, test report 176 pass.
