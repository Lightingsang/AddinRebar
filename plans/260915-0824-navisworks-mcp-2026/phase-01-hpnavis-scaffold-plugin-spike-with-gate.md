---
phase: 1
title: "HPNavis scaffold + plugin spike (gate)"
status: completed
priority: P1
effort: "10h"
dependencies: [0]
---

# Phase 1: Scaffold `HPNavis/` + plugin net48 nạp vào Roamer.exe + spike có gate

## Context Links
- [ADR-01 §5](adr/adr-01-net48-host-multitarget-mcpbridge-core.md) (`AssemblyResolve` hẹp + self-check location) · [ADR-02 §1](adr/adr-02-navis-transaction-dryrun-and-writable-surface.md) (`RollbackOwn`, S-05b/c/d) · [ADR-03 §1](adr/adr-03-navisworks-api-reference-and-test-without-navisworks.md) (`Directory.Build.props`) · [ADR-04 §1–3](adr/adr-04-navis-main-thread-busy-heavy-ops-guard-globals.md) (Idle/wake/quiescence/pre-pass) · [ADR-05 §1–2](adr/adr-05-navis-plugin-packaging-deploy-identity.md)
- Bằng chứng: [evidence §E3–E6, E13](research/evidence-on-machine-2026-09-15.md)
- Mẫu: `HPAutoCad/HPAutoCad.slnx`, `HPAutoCad/global.json`, `HPAutoCad/HPAutoCad.McpBridge/BridgeEntry.cs`, `Service/ScriptingSelfCheck.cs`, `HPAutoCad/tools/harness/pipe-scenarios.py`, `harness-common.ps1` (`Set-OptIn` UIA), `run-bridge-unattended.ps1`

## Overview
Dựng khung `HPNavis/` và chứng minh **trong Roamer.exe thật** những điều chưa xác minh: plugin unsigned nạp (prompt?), Roslyn compile+chạy trong tiến trình với resolver hẹp, `Idle` + `PostMessage` đánh thức, `Commit`→`RollbackOwn` hoàn lại metadata **và không đụng undo của user khi transaction rỗng**, hành vi khi modal/append/clash, plugin lạ bật cùng. Phase 1 **có cửa sổ tối giản** (1 checkbox execution) vì `execute` bị `-32001` khi opt-in OFF (`RequestDispatcher.cs:132`) và opt-in không được bật bằng env/cờ debug. Gate: S-01…S-06, S-09, S-11 pass; S-05b/c/d, S-07, S-08, S-10 có kết luận ghi vào ADR.

## Requirements
- Functional: `HPNavis.slnx` (Debug/Release), `global.json` (copy AutoCAD), `Directory.Build.props` (ADR-03 §1), `HPNavis.McpBridge` (net48, `UseWPF`), `README.md`, `.gitignore`; `EventWatcherPlugin` mở pipe `hpnavis-mcp-2026` với `RequestDispatcher` + executor spike (`ping/context/analyze/execute` tối giản theo ADR-02 §1 `auto`/`none` + `RollbackOwn`); `ScriptingSelfCheck` (compile+run+**kiểm `Assembly.Location`** của Roslyn/Immutable/Metadata trong thư mục plugin) log `MCP scripting self-check OK`; cửa sổ WPF tối giản: trạng thái + checkbox "Allow AI code execution" (mở qua `AddInPlugin` menu Add-ins).
- Non-functional: không tham chiếu `HPRebar/`, `HPAutoCad/`; prefix `HPNavis.*`; không hard-code đường dẫn; deploy khi Roamer đóng.

## Architecture
```
HPNavis/
├── HPNavis.slnx · global.json · Directory.Build.props · README.md · .gitignore
├── HPNavis.McpBridge/                 net48, UseWPF, refs Private=false (ADR-03)
│   ├── HPNavisBridgePlugin.cs         [Plugin("HPNavis.McpBridge","HPNV")] : EventWatcherPlugin — static ctor cài resolver; OnLoaded → BridgeEntry.Start; OnUnloading → Dispose
│   ├── HPNavisWindowPlugin.cs         [Plugin("HPNavis.McpBridge.Window","HPNV")] [AddInPlugin(AddInLocation.AddIn)] : AddInPlugin — Execute() → BridgeEntry.ShowWindow
│   ├── PluginAssemblyResolver.cs      allow-list + RequestingAssembly check + log (ADR-01 §5)
│   ├── BridgeEntry.cs                 logger, settings store (HPNavis/McpBridge), compiler, executor, McpBridgeHost, self-check, window
│   ├── NavisMainThreadExecutor.cs     IBridgeExecutor: Idle + MainThreadQueue + wake + quiescence counter + context-busy (ADR-04 §1–2); spike runner inline
│   ├── Model/NavisScriptGlobals.cs · Model/NavisApp.cs
│   ├── Service/ScriptingSelfCheck.cs · Service/NavisContextReader.cs (spike: title/units/models/IsClear/IsBusy) · Service/NavisVersionMap.cs
│   ├── View/NavisBridgeStatusView.xaml(.cs)  tối giản (phase 2 hoàn thiện: checkbox heavy, last run, nút)
│   └── HPNavis.McpBridge.csproj       + target DeployPlugin (ADR-05 §2)
├── HPNavis.McpBridge.Tests/ (phase 2) · HPNavis.Mcp.Server/ (phase 3) · HPNavis.Mcp.Server.Tests/ (phase 3)
├── tools/harness/pipe-scenarios.py · harness-common.ps1 · run-bridge-spike.ps1
└── output/
```
`/Shared/` trong slnx trỏ `../McpShared/HPRebar.Mcp.Contracts`, `HPRebar.McpBridge.Core`, `HPRebar.Mcp.Server.Core`.

## Related Code Files
- Create: cây trên (trừ phần phase 2/3); `HPNavis/tools/harness/pipe-scenarios.py` (copy từ AutoCAD, prefix `navis.`, thêm scenario S-05b/c/d/S-11); `harness-common.ps1` (copy phần UIA `Set-OptIn` + start/kill guarded; bỏ SECURELOAD); `run-bridge-spike.ps1` (mở Roamer với `Samples\gatehouse\gatehouse_pub.nwd`, chờ pipe, UIA tick execution qua cửa sổ mở bằng `ExecuteAddInPlugin`? — **[chưa xác minh]** Automation `ExecuteAddInPlugin("HPNavis.McpBridge.Window.HPNV")` từ PowerShell COM; fallback: UIA click menu Add-ins; đóng Roamer; kill chỉ process mình mở).

## Implementation Steps
1. Scaffold; `HPNavis.slnx` mirror `HPAutoCad.slnx`.
2. `Directory.Build.props` + `<Error>`; kiểm `dotnet build HPNavis/HPNavis.McpBridge -p:NavisworksInstallDir=C:\nope\` → 1 lỗi rõ.
3. Resolver hẹp + 2 plugin class + self-check (script: `return doc == null ? -1 : doc.Models.Count + args.Int("x", 0);` + assert Location).
4. Executor spike: guard `GuardProfile.Navis` + pre-pass heavy OFF (chặn W2, path policy), compile, queue; `auto`/`none` với `RollbackOwn` + fingerprint tối giản (counts + `NextUndo`); `GetContextAsync` → busy khi `IsBusy`; quiescence depth counter + log.
5. Cửa sổ tối giản + `DeployPlugin`; build Debug (Roamer đóng) → thư mục plugin có 24+ file.
6. **Spike scenarios** (`pipe-scenarios.py` qua pipe; bằng chứng = log bridge + trả lời pipe):

| # | Scenario | Pass khi | Nếu fail |
|---|---|---|---|
| S-01 | Mở Roamer → plugin nạp | log `HPNavis MCP bridge starting…`; **ghi lại** có prompt bảo mật hay không; menu Add-ins có "HPNavis MCP" | sửa tên thư mục/attribute; Navisworks chặn unsigned → **dừng, báo user** |
| S-02 | Self-check | `MCP scripting self-check OK`; log resolve (kỳ vọng ~4 như E13); mọi `Microsoft.CodeAnalysis*`/Immutable/Metadata `Location` trong thư mục plugin | dump `FileLoadException` → sửa resolver |
| S-03 | `ping`, `context` khi Roamer idle không chạm chuột | < 1 s | wake dự phòng `Control.BeginInvoke` |
| S-04 | `execute` `return doc.Title;` `none` (sau UIA tick opt-in; trước tick → `-32001`) | `Value="gatehouse_pub"`, `Changed=0` | |
| S-05 | `auto` + edit thật (`SelectionSets.AddCopy` + `SavedViewpoints.AddCopy` + `OverridePermanentColor`) — chạy `dryRun=false` rồi `dryRun=true` | không dryRun: count +1/+1, `NextUndo=="MCP: spike"`, Ctrl+Z hoàn lại cả 3; dryRun: count không đổi, `RolledBack=true` | loại edit không hoàn lại khỏi W1 |
| **S-05b** | **Transaction rỗng:** user (harness) tạo viewpoint bằng script trước (undo entry X); rồi `auto`+`dryRun` với script `return 1;` | `RolledBack=false`, `NextUndo` vẫn = X, viewpoint còn | sửa `RollbackOwn` |
| **S-05c** | `CurrentSelection.Add(...)` / `CurrentViewpoint.CopyFrom` trong `auto` | ghi có tạo undo entry không → cập nhật ADR-02 §3 hàng "W1?" | — |
| **S-05d** | (thí nghiệm, guard tắt riêng cho scenario) `new Transaction(doc,"x")` không `Commit`, return | ghi `IsActiveTransaction` sau return; `Dispose()` có đóng? `Rollback()` ném gì? → ADR-02 phần phục hồi | — |
| S-06 | `throw` giữa script trong `auto` sau 1 edit | `IsError=true`, `RolledBack=true`, count không đổi | |
| S-07 | Mở dialog modal (Options) rồi `execute` | `-32002` sau ~8 s **hoặc** Idle không bắn → `-32002`; **không** chạy script khi modal mở | thêm/chỉnh `ModalOpen()` |
| S-08 | Chạy clash lớn từ GUI (Architecture vs MEP, tol 0) rồi `execute` giữa lúc chạy | `-32002`; ghi `ProgressBeginning/Ended` có bắn, depth về 0 sau khi xong (`context.IsBusy=false`), thời gian clash | Progress không bắn → quiescence dùng `IsWindowEnabled` + staleness |
| S-09 | Đóng Roamer | `OnUnloading` → `Dispose`, pipe đóng, không exception | |
| **S-10** | Bật lại plugin lạ `NavisworksMCPPlugin` cùng lúc | self-check vẫn OK; log resolve của ta không trả lời requester ngoài thư mục; ghi có xung đột không | thu hẹp allow-list |
| **S-11** | Pre-pass/guard: script `Expression.Call(...)`, `new NavisworksCommand(...)`, `doc.AppendFile(@"\\srv\x.nwd")` (heavy OFF), `doc.BeginTransaction("x")` | mỗi cái 1 diagnostic đúng id (`GUARD`/`HEAVY`) và **không chạy** | bổ sung deny-list |

7. `reports/phase-01-spike.md`: bảng S-01…S-11 với trích log, số resolve, thời gian clash; **cập nhật ADR-02/ADR-04** các mục `[chưa xác minh]`.

## Todo List
- [x] Scaffold + slnx + props + README
- [x] Resolver + 2 plugin + self-check
- [x] Executor spike + quiescence + pre-pass
- [x] Cửa sổ tối giản + Deploy target
- [x] S-01…S-11 ≥ 2 lần unattended
- [x] Report + ADR update

## Success Criteria
- [x] `dotnet build HPNavis/HPNavis.slnx -c Debug -p:DeployPlugin=false` xanh; `-p:NavisworksInstallDir=C:\nope\` fail đúng 1 lỗi có hướng dẫn.
- [x] `grep -rn "HPRebar/\|HPAutoCad/\|HPCivil3D" HPNavis --include=*.csproj --include=*.slnx` = 0 (chỉ `..\McpShared\`).
- [x] `powershell.exe -File HPNavis/tools/harness/run-bridge-spike.ps1 -Runs 2 -WithModal` (Windows PowerShell 5.1, không pwsh) → S-01…S-06, S-07, S-08 (script-driven), S-09, S-11 **pass 2/2**; S-05b/c, S-10 kết luận; S-05d bỏ (guard) — `reports/phase-01-spike.md`; log `MCP scripting self-check OK in 2762 ms`.
- [x] Không prompt bảo mật (S-01 ×2). Gỡ sạch: đổi tên thư mục → mở Roamer → không log HPNavis (control run cho Automation probe, 13:38).
- [x] Không có bypass opt-in nào trong code (grep `HPNAVIS_SPIKE`/`ENABLE_EXECUTION` = 0). Env duy nhất `HPNAVIS_MCP_BRIDGE_SHOW_WINDOW` chỉ mở cửa sổ.

## Risk Assessment
- Roslyn bind trong Roamer khác probe console → resolver log; **không** eager-LoadFrom (vô nghĩa với bind đúng version — E13); S-02 + S-10 quyết định.
- `Application.Gui` null tại `OnLoaded` → handle ở `GuiCreated`.
- `Idle` không bắn khi Roamer nằm im → S-03 quyết định wake dự phòng.
- UIA tick checkbox trên cửa sổ WPF của ta — ✅; mở cửa sổ unattended qua Automation `ExecuteAddInPlugin` **không kiểm được** (Roamer mở qua Automation thoát sau ~15 s trên máy dev) → dùng env `HPNAVIS_MCP_BRIDGE_SHOW_WINDOW=1` + `Roamer.exe "<model>"` trực tiếp.
- Plugin lạ của user: **không đụng** — spike chạy với nó bật, không xung đột (S-10). Tắt tạm cho live verify phase 5 = 👤 hỏi user.
