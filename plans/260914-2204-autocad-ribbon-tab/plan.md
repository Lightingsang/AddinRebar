---
title: "HPAutoCad — Ribbon tab \"MCP AutoCAD\""
description: "Tab Ribbon trong AutoCAD 2026 cho các chức năng bridge hiện có (cửa sổ, listener, trạng thái sống, script cuối, thư mục log/audit/library, tự khởi động, hướng dẫn) — Autodesk.Windows API trong loader, không CUIx, không server"
status: in-progress
priority: P2
effort: 6h
branch: RebarVersion1
tags: [autocad, ribbon, mcp, loader, adwindows]
created: 2026-09-14
blockedBy: []
blocks: []
---

# HPAutoCad — Ribbon tab "MCP AutoCAD" — Plan

**Ngày:** 2026-09-14 · **Mode:** fast (không research; kiến trúc đã verified live trong plan `260913-0000-autocad-mcp-bridge-2026`) · **Branch:** `RebarVersion1`

## Kiến trúc đã xác nhận (không khảo sát lại)
- `HPAutoCad/HPAutoCad.McpBridge.Loader` (net8.0-windows, **default ALC**, `IExtensionApplication BridgeLoaderApplication`, 4 command `HPMCPBRIDGE/START/STOP/STATUS` Session) — assembly duy nhất AutoCAD nạp; nhận từ bridge một `IReadOnlyDictionary<string, Delegate>` (show/start/stop/status/dispose). Mọi thứ qua ranh giới ALC phải là kiểu BCL.
- `HPAutoCad/HPAutoCad.McpBridge` (ALC riêng): `BridgeEntry.Start` → `McpBridgeHost` (`Status` enum Stopped/Listening/Connected/Busy/Error, `StatusMessage`, `ExecutionEnabled`, `AutoStartListener` (persist `settings.json`), `LastRun.Source`, `StateChanged`), cửa sổ WPF `AutocadBridgeStatusView` (opt-in `AllowExecution` — OFF mỗi phiên).
- Server exe `HPAutoCad.Mcp.Server` do Claude Code sở hữu (stdio) — plugin **không** start/stop. Không có command chạy tool → Ribbon không có nút chạy tool (ghi rõ lý do).
- `AutoCAD.NET [25.1.0]` NuGet cache **có `AdWindows.dll`** (`lib/net8.0/`) → `Autodesk.Windows` Ribbon API dùng được qua PackageReference hiện có (ExcludeAssets=runtime, PrivateAssets=all ⇒ không copy DLL AutoCAD vào bundle). Loader cần `UseWPF` để compile `ImageSource`/`Dispatcher`.
- Bundle `DeployBundle` → `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.McpBridge.bundle\` (Platform="AutoCAD", R25.1). SECURELOAD đã trust folder. Môi trường: AutoCAD 2026 R25.1.74 đầy đủ (.NET 8), Windows 11, Revit 2026 đang mở (không build HPRebar), AutoCAD phải đóng khi deploy.
- Harness: `tools/harness/harness-common.ps1` (UIA by pid, SECURELOAD, `Start-AcadWithBridge`), `run-bridge-unattended.ps1` 21/21, `run-server-smoke.ps1` 22/22 — phải giữ nguyên.

## Giải pháp
Mở rộng **loader** bằng `Autodesk.Windows` (RibbonTab id `HPAUTOCAD_MCP_TAB`, 3 panel), nút gọi **cùng** entry point delegate như các command (không `SendStringToExecute`, không cần document, hoạt động cả khi AutoCAD bận vì không đụng bản vẽ). Bridge thêm **4 entry point additive** (`status.subscribe`, `copyLastScript`, `autoStart.get/set`, `path`) — chỉ string/bool/Action. Icon = vector `DrawingImage` vẽ trong code (không file, không đường dẫn). Không CUIx, không sửa acad.cuix, không McpShared.

## Phases
| # | File | Status | Effort |
|---|---|---|---|
| 1 | [phase-01](phase-01-ribbon-tab-and-bridge-entry-points.md) — entry point mới trong `BridgeEntry`, `Ribbon/` trong loader (tab, handler, icon, status presenter), idempotent + workspace-safe | **built + tested + verified (2026-09-14)** — 0 warning, 58 test, live 8/8 | 4h (≈1.5h) |
| 2 | [phase-02](phase-02-harness-deploy-verify-docs.md) — `run-ribbon-check.ps1` (UIA), deploy bundle, hồi quy 21/21 + 22/22, README/CLAUDE.md, report | in-progress — harness + hồi quy + README done; tester/review/docs/journal pending | 2h |

## Quyết định
| Quyết định | Lựa chọn | Lý do |
|---|---|---|
| Nơi đặt Ribbon | Loader (default ALC) | AdWindows nạp ở default ALC; loader đã là `IExtensionApplication`; bridge ALC không được đụng UI framework của AutoCAD |
| Gọi chức năng | delegate trực tiếp (`BridgeLoaderApplication.Invoke`) | cùng luồng với command; không cần document; không xếp hàng sau lệnh đang chạy |
| Opt-in AI code execution | **không** lên Ribbon | ADR-04: tick tay trong cửa sổ, OFF mỗi phiên |
| Nút chạy tool/seed | **không có** | không có command chạy tool; tool chạy qua server của Claude Code |
| Icon | `DrawingImage` vector trong code | đóng gói cùng DLL, không phụ thuộc file/đường dẫn, sắc nét mọi DPI |
| Trạng thái | 4 mức từ `BridgeStatus` + `HasClient`: chưa nạp / sẵn sàng (Stopped) / đang lắng nghe / client kết nối (+Busy/Error) | không gộp "Connected" thiếu căn cứ |

## Rủi ro
| Rủi ro | Xử lý |
|---|---|
| `ComponentManager.Ribbon` null lúc Initialize (startup) | đăng ký `ComponentManager.ItemInitialized`, tạo một lần, huỷ đăng ký |
| Đổi workspace xoá tab lập trình | `Application.SystemVariableChanged` (WSCURRENT) → `Application.Idle` one-shot → `EnsureCreated()` với `FindTab` guard |
| UIA không thấy Ribbon (peer AdWindows) | harness ghi FAIL/manual rõ, không PASS giả |
| Loader compile `UseWPF` kéo thêm reference | chỉ compile-time; bundle không đổi |
