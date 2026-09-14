---
phase: 2
title: "Harness Ribbon, deploy bundle, hồi quy, docs"
status: in-progress
priority: P2
effort: "2h"
dependencies: [1]
---

# Phase 2: Harness Ribbon, deploy bundle, hồi quy, docs

## Overview
Chứng minh tab hoạt động trong AutoCAD 2026 thật bằng harness không cần người (UIA), không phá hai harness cũ, rồi cập nhật README/CLAUDE.md và bàn giao.

## Requirements
- Functional: `tools/harness/run-ribbon-check.ps1` — start AutoCAD với `bridge.scr`; UIA (theo pid) tìm tab "MCP AutoCAD" → đúng 1; COM `_WSCURRENT` sang workspace khác rồi về → vẫn đúng 1 tab; Invoke nút "Bật listener" → `Test-Path \\.\pipe\hpautocad-mcp-2026` true ≤ 10 s; Invoke "Tắt listener" → pipe biến mất ≤ 10 s; Invoke "Bảng điều khiển" → cửa sổ bridge (UIA `AllowExecution`) xuất hiện; Invoke "Trạng thái" không ném lỗi (loader.log không có "failed"); kill AutoCAD; JSON summary. Nếu UIA không thấy Ribbon/nút → in `MANUAL` cho mục đó, exit code 2, không PASS.
- Non-functional: harness chỉ chạm AutoCAD do nó start (pid guard), không sửa bản vẽ; các harness cũ giữ 21/21 và 22/22.

## Architecture
`harness-common.ps1` (Start-AcadWithBridge, Answer-SecureLoad, Find-BridgeWindow) + hàm mới `Find-RibbonTab($pid, $name)` / `Invoke-RibbonButton($pid, $tabName, $buttonName)` dùng `System.Windows.Automation` (TreeScope.Descendants, ControlType.TabItem/Button, Name). Workspace đổi qua COM Windows PowerShell 5.1 pid-guarded (`$a.ActiveDocument.SendCommand('_WSCURRENT "Drafting & Annotation" ')` → về `"3D Modeling"` hoặc workspace ban đầu đọc từ `(getvar WSCURRENT)` qua `$a.GetVariable('WSCURRENT')`).

## Related Code Files
- Create: `HPAutoCad/tools/harness/run-ribbon-check.ps1`
- Modify: `HPAutoCad/tools/harness/harness-common.ps1` (2 hàm UIA), `HPAutoCad/tools/harness/README.md`
- Modify: `HPAutoCad/README.md` (mục Ribbon: bảng nút → command → chức năng; cài/gỡ; NETLOAD thử; trusted location), `CLAUDE.md` (mục HPAutoCad: Ribbon tab, harness) → regen `AGENTS.md`, `docs/codebase-summary.md`, `docs/project-changelog.md`
- Report: `plans/260914-2204-autocad-ribbon-tab/reports/ribbon-live-check.md` + log

## Implementation Steps
1. Viết 2 hàm UIA trong `harness-common.ps1`; viết `run-ribbon-check.ps1` (mẫu: `run-server-smoke.ps1`).
2. Đóng AutoCAD → `dotnet build HPAutoCad/HPAutoCad.slnx -c Debug` (deploy bundle, kiểm `Contents\README.md`) → chạy `run-ribbon-check.ps1` → sửa lỗi phát hiện → chạy lại.
3. Hồi quy: `run-bridge-unattended.ps1` 21/21; publish exe không cần (server không đổi) → `run-server-smoke.ps1` 22/22.
4. Docs + report (bảng ánh xạ, lệnh build, vị trí bundle, cài/gỡ: xoá thư mục bundle; NETLOAD `Contents\HPAutoCad.McpBridge.Loader.dll` để nạp thử; trusted = thư mục bundle; cập nhật phiên bản = build Debug khi AutoCAD đóng), checklist người dùng (tab hiện sau khi mở lại AutoCAD; đổi workspace; nút khi chưa mở bản vẽ; khi AutoCAD bận trong lệnh LINE; Claude Code MCP vẫn chạy).
5. Commit `feat(autocad): ribbon tab for the MCP bridge` (+ `fix` sau review), `docs: …`.

## Success Criteria
- [x] `run-ribbon-check.ps1`: 8/8 PASS, 0 MANUAL (run 3; UIA tìm tab theo AutomationId, workspace qua `SetVariable`) — `reports/ribbon-live-check.md`
- [x] `run-bridge-unattended.ps1` 21/21, `run-server-smoke.ps1` 22/22 (assertion tools/list nới `>= 24` vì registry máy dev có 2 tool đã duyệt)
- [x] loader.log không có exception mới; không AutoCAD sót
- [ ] README/CLAUDE.md/AGENTS.md/changelog cập nhật; report Antigravity phân biệt (a) test tự động, (b) build, (c) harness trong AutoCAD, (d) checklist tay

## Risk Assessment
- UIA của AdWindows có thể không expose tab (AutomationPeer) → thử `TreeScope.Descendants` từ cửa sổ chính; nếu vẫn không, dùng `ComponentManager` không được từ ngoài → ghi MANUAL, người dùng kiểm tay (không claim).
- WSCURRENT qua COM khi cửa sổ bridge đang focus → COM vẫn được (không phải SendKeys); nếu bị RPC_E_CALL_REJECTED, lặp 3 lần.
