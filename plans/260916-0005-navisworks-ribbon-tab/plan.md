---
title: "HPNavis — Ribbon tab \"HPNavis\" ▸ \"MCP\" ▸ \"MCP Bridge\""
description: "Nút Ribbon trong Navisworks Manage 2026 mở cửa sổ bridge — cùng bề mặt 1 nút như ribbon MCP Revit, nhãn English, icon vẽ riêng cho chức năng (cửa sổ + phích cắm) render pixel-exact 16/32"
status: completed
priority: P2
effort: 3h
branch: RebarVersion1
tags: [navisworks, ribbon, mcp, net48, commandhandlerplugin]
created: 2026-09-16
blockedBy: []
blocks: []
---

# HPNavis — Ribbon tab "HPNavis" ▸ "MCP" ▸ "MCP Bridge" — Plan

**Ngày:** 2026-09-16 · **Mode:** plan-mode (2 Explore + 1 Plan agent; không research ngoài máy) · **Branch:** `RebarVersion1` · **Tiền đề:** plan `260915-0824-navisworks-mcp-2026` đã đóng (phases 0–5 verified live); user đã chốt "Ribbon sau phase 5".

## Quyết định user (2026-09-16, AskUserQuestion)
1. **Bộ nút:** tối giản như ribbon MCP Revit — 1 panel `MCP`, 1 nút `MCP Bridge` mở cửa sổ bridge (không phải 10 nút như AutoCAD).
2. **Ngôn ngữ nhãn:** English.
3. Yêu cầu gốc: "icon sắc nét rõ ràng theo đúng chức năng".

## Kiến trúc đã xác nhận trên máy (không khảo sát lại)
- Navisworks 2026 Ribbon plugin = `CommandHandlerPlugin` + `[RibbonLayout("x.xaml")]` + `[RibbonTab(Id)]` + `[Command(Id, Icon=16px, LargeIcon=32px)]`; layout XAML + `.name` strings trong `en-US\` cạnh DLL, icon trong `Images\` (SDK `CustomRibbon`, `Autodesk.Navisworks.Api.xml`; plugin lạ `NavisworksMCPPlugin` trên máy dùng đúng cơ chế với PNG). `Autodesk.Navisworks.Api.dll` v23, `AdWindows.dll` 5.2.0.2 trong thư mục cài; Navisworks 2026 **không có dark theme** (không chuỗi theme trong `navisworks.gui.roamer.dll`).
- Ribbon MCP Revit: tab `HPRebar` / panel `MCP` / nút `MCP Bridge` → `McpBridgeCommand` mở cửa sổ modeless (icon là placeholder Nice3point). AutoCAD: tab 10 nút, icon vector `DrawingImage` trong code (`RibbonIcons.cs`).
- HPNavis hiện có: `HPNavisBridgePlugin` (EventWatcher) + `HPNavisWindowPlugin` (AddIn "HPNavis MCP" trong menu Add-ins → `BridgeEntry.ShowWindow()`); `DeployPlugin` target copy toàn bộ `$(OutDir)**` (trừ `.xml`) giữ thư mục con; máy dev 96 DPI.

## Giải pháp
`HPNavisRibbonPlugin : CommandHandlerPlugin` (`ID_HPNAVIS` / `ID_HPNAVIS_MCP_BRIDGE`, `CallCanExecute.Always`) → `BridgeEntry.ShowWindow()`; `Ribbon/en-US/HPNavisRibbon.xaml` (`<Page Remove>` khỏi WPF markup compile, `None` + `Link` vào `en-US\`) + `.name`; icon PNG 16/32 render pixel-exact từ 1 glyph vector (cửa sổ + phích cắm; ink `#3C3C3C`, accent `#0696D7`) bằng `tools/icons/render-ribbon-icons.ps1`; `HPNavisWindowPlugin` → `AddInLocation.None` (hết nút trùng, còn `ExecuteAddInPlugin`). Không đụng `McpShared/`, `HPRebar/`, `HPAutoCad/`.

## Phases
| # | File | Status | Effort |
|---|---|---|---|
| 1 | [phase-01](phase-01-ribbon-tab-mcp-bridge-button.md) — plugin class, layout/strings, icon script + PNG, csproj, tests, harness `run-ribbon-check.ps1`, docs | **built + tested + verified live (2026-09-16)** — 0 warning, tests 135, ribbon check 14 PASS + 1 MANUAL (icon), hồi quy live-verify 62 + bridge 43; report [reports/ribbon-live-check.md](reports/ribbon-live-check.md); commit `894a4a3` | 3h (≈2h) |

## Quyết định kỹ thuật
| Quyết định | Lựa chọn | Lý do |
|---|---|---|
| Icon | PNG 16/32 từ vector, toạ độ chẵn, `EdgeMode.Aliased` | 96 DPI → pixel-exact sắc hơn vector anti-alias; attribute/XAML của Navisworks chỉ nhận file; swap `DrawingImage` runtime cần AdWindows + rủi ro thứ tự `GuiCreated` |
| Glyph | cửa sổ có title bar + phích cắm | "bảng điều khiển kết nối MCP"; đọc được ở 16 px |
| `CallCanExecute.Always`, không `LoadForCanExecute` | nút không bị làm mờ theo tài liệu; assembly đã nạp từ EventWatcher | KISS |
| Nút Add-ins cũ | `AddInLocation.None` | 1 lối vào hiển thị; harness/`ExecuteAddInPlugin` vẫn dùng được |
| Không có model | chấp nhận Navisworks tự làm mờ mọi tab trên start page | hành vi host; cửa sổ vẫn mở qua env var / `ExecuteAddInPlugin` |

## Rủi ro → kết quả
| Rủi ro | Kết quả live |
|---|---|
| XAML bị WPF compile → không có `en-US\*.xaml` | `<Page Remove>` + test copy-out; build 0 warning |
| Tab không hiện / id lệch | tests pin attribute ⇔ XAML ⇔ `.name`; live: tab đúng 1 lần |
| UIA không thấy Ribbon WinForms-hosted | thấy: header = `Button` AutomationId = tab id; Invoke chọn tab; SelectionItem "thành công" nhưng không hiển thị → harness verify bằng nút hiện trên màn hình |
| Nút mờ khi không có model | Navisworks mờ **mọi** tab trên start page (không phải do plugin) — ghi nhận, không phải lỗi |
| Screenshot chụp cửa sổ khác đè lên | `Set-RoamerMainWindowForeground` trước khi chọn tab/chụp |
