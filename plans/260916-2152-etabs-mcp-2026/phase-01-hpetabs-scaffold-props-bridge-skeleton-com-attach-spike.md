---
phase: 1
title: "HPEtabs scaffold (bridge app + server exe + 2 test projects) + Directory.Build.props + COM attach spike qua stdio WITH GATE"
status: pending
priority: P1
effort: "8h"
dependencies: [0]
---

# Phase 1: Khung `HPEtabs/` (cả hai exe), dò `ETABSv1.dll`, spike E9…E20 với ETABS 22 thật qua `live-verify.py --phase spike` (gate)

## Context Links
- [ADR-01 §1](adr/adr-01-standalone-bridge-app-vs-in-process-server.md) (app WPF, `McpBridgeHost` 7+1 tham số) · [ADR-03 §1–3](adr/adr-03-etabsv1-reference-and-test-without-etabs.md) (props CLSID, resolver, self-check `Location`, `EtabsApiLocator`) · [ADR-04 §1–3](adr/adr-04-sta-worker-busy-cancel-units-guard-globals.md) (STA, control lane, liveness, hints) · [ADR-05 §1–3](adr/adr-05-identity-registry-packaging.md) (định danh, publish folder) · [ADR-02 open items](adr/adr-02-no-transaction-snapshot-tiers-and-opt-ins.md) (E9, E10, E13b) · [red-team](reports/red-team-2026-09-16.md) hàng 1, 9, 13
- Mẫu: `HPAutoCad/HPAutoCad.slnx`, `HPNavis/global.json`, `HPNavis/Directory.Build.props:13–24`, `HPAutoCad/HPAutoCad.McpBridge/BridgeEntry.cs:32–80`, `HPNavis/HPNavis.McpBridge/Service/ScriptingSelfCheck.cs`; server 1:1 `HPNavis/HPNavis.Mcp.Server/{Program.cs, Hosts/NavisHostProfile.cs, Tools/*, Prompts/*, Resources/*, appsettings.json, *.csproj}`, tests `HPNavis/HPNavis.Mcp.Server.Tests/{NavisHostProfileTests.cs, NavisToolsOverPipeTests.cs, *.csproj:26–33}`; harness `HPNavis/tools/harness/{live-verify.py, run-live-verify.ps1, harness-common.ps1}` + `McpShared/tools/{mcp-session.py, harness_common.py}`.
- Bằng chứng: [evidence §E1–E5, §E8](research/evidence-on-machine-2026-09-16.md).

## Overview
Dựng **toàn bộ** khung `HPEtabs/` (bridge app + server exe + 2 test project — red-team #13 gộp server vào đây: server là `Program.cs` một dòng + profile + copy) và chứng minh **với ETABS 22 thật** những gì chưa xác minh, **qua stdio** (`live-verify.py --phase spike` → `HPEtabs.Mcp.Server.exe` → pipe → bridge → COM): attach `GetObject`, resolve `ETABSv1.dll` qua CLSID, Roslyn compile `ETABSv1` trong tiến trình của ta, `Assembly.Location` khác rỗng, STA outbound, units get/set/restore, no-model, modal, `Save(path)` save-as, unlock xoá kết quả, `GetNameList` từng receiver, liveness khi ETABS đóng. Cửa sổ **tối giản** (1 checkbox execution + Attach/Detach). **Gate:** attach + self-check OK + E16/E17/E18/E19/E20 pass và E9…E15 có kết luận → mới sang phase 2.

👤 **Trước spike user phải:** mở ETABS 22 với **model bỏ đi** (`HPEtabs/output/live-verify/model.EDB` — vài frame, 1 story, đã save, đã chạy analysis trong GUI); **duyệt** hai probe ghi: E9 `File.Save(<tmp trong thư mục model>)`, E10 `SetModelIsLocked(false/true)`. Mọi probe khác read-only; harness không `OpenFile/New*`.

## Requirements
- Functional: `HPEtabs.slnx` (Debug/Release; `/Solution Items/` = `global.json`, `Directory.Build.props`, `README.md`; `/Shared/` = 3 project `../McpShared/*`), `global.json` copy verbatim `HPNavis/global.json`, `Directory.Build.props` (ADR-03 §1), `README.md`, `.gitignore`.
  - **Bridge** `HPEtabs.McpBridge` (net8.0-windows, `UseWPF`, WinExe, `<Reference ETABSv1 Private=false>`, `<Error>` thiếu API): `App.xaml` → `BridgeEntry.Start()` (resolver → Serilog `%LocalAppData%\HPEtabs\McpBridge\logs\` shared → `new BridgeSettingsStore("HPEtabs","McpBridge")` → compiler → `EtabsExecutor` spike (STA worker + control lane + liveness + `MainThreadQueue`; execute **R-only**, tier từ fixture rút gọn; W/D → `-32001`-style refused "not implemented until phase 2") → `McpBridgeHost(executor, settings, store, "22", PipeNaming.For("etabs",22), "ETABS", JsonRpcMethods.EtabsPrefix, executionDisabledMessage)` → self-check (assert `Location`) → cửa sổ).
  - **Server** `HPEtabs.Mcp.Server` (net10 stdio): `Program.cs` = `McpServerHost.RunAsync(args, EtabsHostProfile.Instance)`; `EtabsHostProfile` = bảng ADR-05 §1 nguyên văn (`DefaultVersion 22`, `ValidVersions [22]`, `MaxTimeoutSeconds = EtabsHeavyMaxTimeoutSeconds`, 2 hint); 4 core tool: `execute_etabs_code` (Destructive) mô tả ≤ 1 500 ký tự **phải** nêu: globals; "present units forced to kN_mm_C (mm, kN, kN·mm, kN/mm²) and restored"; "no transaction/undo — three tiers: R read-only (allow-list: Get*/Is*/Has*/Count/RefreshView/AnalysisResults*/GetTableForDisplayArray; `transaction:none`), W write (`transaction:auto`; the bridge **saves your model** and copies a .EDB snapshot first — `snapshot` names the file; unsaved or UNC models are refused; `rolledBack:false` after an exception means changes persisted), D destructive (SetModelIsLocked, RunAnalysis, DeleteResults, OpenFile/New*/Save(path), ApplyEditedTables, Start*/Modify*/Merge*/Reset*/Clear*/Rename*/Show*/Export*/Import*, any path-taking member: the user must tick 'Allow destructive operations' in the HPEtabs MCP Bridge window, else the call is refused with error -32001; up to 600 s)"; "dryRun or transaction:none on a writing script = static preview: nothing runs, PREVIEW diagnostic lists the members"; "manual runs like auto"; "Changed counts additions/deletions only"; "cancel/timeout cannot interrupt a running ETABS call — the snapshot save counts against your timeout"; "path arguments must be a string literal or args.Str(\"key\"), never UNC"; "every OAPI call returns int: check `ret` and throw InvalidOperationException($\"ETABS returned {ret} from X\"); caller-input problems → ArgumentException"; "no Helper/ApplicationExit; base guard also blocks `File`/`GetProperty` identifiers". `get_etabs_context` (ReadOnly) mô tả 10 field `EtabsInfo` + `docPath/docTitle/isModifiable`; `inspect_type`, `cancel_execution` ("cannot interrupt an ETABS call"); prompts `etabs_query_template`, `etabs_modify_template`, `toolify_run`; resources `etabs://model/info`, `etabs://selection`; `appsettings.json` copy Navis (`Bridge.HostVersion 22`).
  - **Tests** `HPEtabs.Mcp.Server.Tests` (net10, xunit v3, link `FakeRevitExecutor.cs`): `EtabsHostProfileTests` (pipe/prefix/tool names/categories/600/reserved/`ValidVersions [22]`/hints non-null), `EtabsToolsOverPipeTests` (PipeListener ephemeral + fake → execute/context/inspect/cancel; `-32001/-32002/-32003`; `timeoutSeconds=600` chấp nhận; `run_tool` 600 không clamp; `Shape` không revitVersion/isFamily, có `etabs`; bridge vắng → message = `BridgeNotConnectedHint` (nêu exe, không path máy); timeout → `TimeoutSemanticsHint`; description `execute_etabs_code` ≤ 1 500 ký tự và chứa "saves your model", "-32001", "PREVIEW"). `HPEtabs.McpBridge.Tests` tạo **rỗng** (csproj + 1 test `ScriptingSelfCheck` compile — cần ETABS, ADR-03 §4).
  - **Harness** `HPEtabs/tools/harness/{live-verify.py, run-live-verify.ps1, harness-common.ps1, README.md}` — **một** py + **một** ps1, mọi phase thêm `--phase` (`spike|bridge|seeds|full`) và switch; import `../../../McpShared/tools/mcp-session.py` + `harness_common.py` (đường tương đối như HPNavis); ps1 Windows PowerShell 5.1: start bridge exe guarded, UIA tick checkbox + click Attach trên cửa sổ WPF của ta, **không** start/kill ETABS, kill chỉ pid mình mở.
- Non-functional: không tham chiếu `HPRebar/`, `HPAutoCad/`, `HPNavis/`, `HPCivil3D/` (không tồn tại hôm nay — quy tắc vẫn giữ); prefix `HPEtabs.*`; không hard-code đường dẫn; không bypass opt-in (`grep HPETABS_SPIKE\|ENABLE_EXECUTION` = 0); `McpShared` không bị chạm.

## Architecture
```
HPEtabs/
├── HPEtabs.slnx · global.json · Directory.Build.props · README.md · .gitignore
├── HPEtabs.McpBridge/                          net8.0-windows, UseWPF, WinExe
│   ├── App.xaml(.cs)                           OnStartup → BridgeEntry.Start(); OnExit → drain + Dispose; close while busy → confirm
│   ├── BridgeEntry.cs                          VendorFolder "HPEtabs", ProductFolder "McpBridge", HostName "ETABS", HostVersion "22", BusyGrace 8 s; không public static executor
│   ├── EtabsExecutor.cs                        IBridgeExecutor: STA foreground worker, control lane Attach/Detach, liveness (Process.Exited), MainThreadQueue(expireWithoutTicks); spike scope: execute R-only
│   ├── Service/EtabsAssemblyResolver.cs        AssemblyLoadContext.Default.Resolving → env → CLSID LocalServer32 → LoadFromAssemblyPath
│   ├── Service/EtabsApiLocator.cs              Find(): env HPETABS_ETABS_DIR → CLSID → ProgramW6432 (dùng lại trong server tests qua link file)
│   ├── Service/EtabsAttachment.cs              Helper.GetObject; Detach; Process.Exited; hwnd; > 1 ETABS → warning
│   ├── Service/ScriptingSelfCheck.cs           assert Assembly.Location; resolved path + GetOAPIVersionNumber; compile/run read-only script
│   ├── Service/EtabsContextReader.cs           EtabsInfo 10 field + DocPath/DocTitle/IsModifiable
│   ├── Model/EtabsScriptGlobals.cs             sapModel, etabs, units, ct, log, progress, args
│   ├── Resources/etabs-oapi-tiers.txt          fixture rút gọn (phase 2 sinh đủ)
│   ├── View/EtabsBridgeStatusView.xaml(.cs)    tối giản: status, checkbox execution, Attach/Detach, warning > 1 ETABS
│   └── HPEtabs.McpBridge.csproj
├── HPEtabs.McpBridge.Tests/                    net8.0-windows (cần ETABS); phase 2 điền
├── HPEtabs.Mcp.Server/                         Program.cs · Hosts/EtabsHostProfile.cs · Tools/{ExecuteEtabsCodeTool, EtabsContextTool}.cs · Prompts/EtabsScriptPrompts.cs · Resources/EtabsDocumentResources.cs · Registry/SeedLibrary/ (phase 3) · appsettings.json · *.csproj
├── HPEtabs.Mcp.Server.Tests/                   EtabsHostProfileTests.cs · EtabsToolsOverPipeTests.cs · (phase 3: SeedLibraryCompileTests, SeedLibraryStructureTests) · *.csproj (link FakeRevitExecutor.cs, EtabsApiLocator.cs)
├── tools/harness/live-verify.py · run-live-verify.ps1 · harness-common.ps1 · README.md
└── output/live-verify/model.EDB (👤) · output/live-verify/registry (isolated)
```

## Related Code Files
- Create: cây trên. Modify (untracked, 👤): `.mcp.json` thêm `hprebar-etabs` → `HPEtabs/output/HPEtabs.Mcp.Server/HPEtabs.Mcp.Server.exe`, env `HPETABS_MCP_Bridge__HostVersion=22` (+ ghi chú "start HPEtabs.McpBridge.exe first") — **không** sửa 3 entry kia.
- Không sửa `McpShared`; thiếu → dừng, cập nhật bảng phase 0.

## Implementation Steps
1. Scaffold + slnx (mirror AutoCAD) + `global.json` + `.gitignore` (`bin/ obj/ output/ *.dll`).
2. `Directory.Build.props` (ADR-03 §1); `dotnet build HPEtabs/HPEtabs.McpBridge -p:EtabsInstallDir=C:\nope\` → đúng 1 lỗi có hướng dẫn nêu 2 project cần ETABS.
3. Bridge: resolver + locator + attachment (control lane, liveness) + self-check + executor spike + cửa sổ tối giản; build Debug; chạy exe → cửa sổ + log `ETABSv1.dll resolved from … (2.10.0.0)`.
4. Server: copy skeleton Navis → `EtabsHostProfile` + 4 tool + prompts + resources; tests; `python McpShared/tools/mcp-call.py <Debug exe> tools/list` (registry cách ly) → 12 tool.
5. Harness: `live-verify.py --phase spike` (một stdio session tới server Debug exe, registry cách ly) + `run-live-verify.ps1 -Phase spike` (start bridge, UIA tick + Attach).
6. **Spike scenarios** (qua stdio; bằng chứng = log bridge + `ExecuteResult`; 👤 ETABS mở với model bỏ đi):

| # | Scenario | Pass khi | Nếu fail |
|---|---|---|---|
| E9 | `Save(tmp)` rồi `GetModelFilename()`/`GetModelFilepath()` (👤 duyệt; chạy qua nút "run probe" trong cửa sổ, không qua tier) | ghi tên hiện tại có đổi không → quyết `restore_model_snapshot` | — |
| E10 | `Analyze.GetCaseStatus` (chữ ký CHM › "cAnalyze Interface" `[chưa xác minh]`) trước → `SetModelIsLocked(false)` → sau (👤) | ghi status → ADR-02 hàng D `[chưa xác minh trong CHM]` | — |
| E11 | `File.Save()` khi model có path | `ret==0`, không dialog; thời gian | dialog → cảnh báo snapshot |
| E12 | No-model (👤 đóng model): `GetModelFilename()`, `GetModelFilepath()`, `PointObj.GetNameList` | ghi rỗng/`ret≠0`/exception → phép thử `-32003` | — |
| E13 | Units: overload `GetPresentUnits()`; `SetPresentUnits(kN_mm_C)`; `PointObj.GetCoordCartesian` `[chưa xác minh]` → mm; restore → GUI không đổi (👤) | `ret==0` ×3 | — |
| E13b | `GetNameList(ref n, ref names)` trên PointObj/FrameObj/AreaObj/Story/LoadPatterns/LoadCases/RespCombo/PropFrame/PropMaterial | ghi receiver nào có/không → fingerprint | — |
| E14 | STA (worker) vs MTA (thread pool) cùng proxy | ghi MTA ném gì → khẳng định "mọi call trên worker" | — |
| E15 | Modal: 👤 mở dialog trong ETABS → `execute` đọc | `IsWindowEnabled` false → `-32002` sau ~8 s trước call; hoặc call block/reject mã COM; đóng → OK | pre-check không bắt → `GetForegroundWindow` owner |
| E16 | Resolver + `Location`: log path `ETABSv1.dll` 2.10.0.0; `bin/` không có `ETABSv1.dll`; `typeof(EtabsScriptGlobals).Assembly.Location` khác rỗng (Debug và **publish folder**) | pass | copy nhầm → `Private`; single-file lọt → csproj |
| E17 | Roslyn compile `return sapModel.PointObj.GetNameList(ref n, ref names);` (+ `using ETABSv1`) qua stdio | compile OK, trả int | reference thiếu |
| E18 | Attach `GetObject` → `GetOAPIVersionNumber()=="2.10.0.0"`; 👤 mở 2 ETABS → cửa sổ + `context` warning "use Tools › Active Instance for API"; **không** pid picker | pass | `GetObject` null → ghi |
| E19 | 👤 đóng ETABS → `execute` **ngay** → `-32003` "not attached" (< 1 s, không đợi 8 s); mở lại ETABS → **Attach lại không restart bridge** → OK | pass (red-team #1) | hwnd chết kẹt → sửa liveness |
| E20 | Guard: `etabs.ApplicationExit(false)`, `new Helper()`, `Marshal.GetActiveObject("x")`, `Expression.Call(...)`, `HPEtabs.McpBridge.BridgeEntry.Stop()`, `McpBridgeHost.Current`; + kiểm `sapModel.File.Save()` (member access `.File`) qua guard | mỗi cái `GUARD`, không chạy; ghi kết quả `.File` | bổ sung deny (phase 0 bảng) |

7. Publish 2 exe (server single-file, bridge folder — ADR-05 §3) → chạy lại E16 trên bản publish; `.mcp.json` 👤; smoke `get_etabs_context` qua Claude Code (nếu tiện).
8. `reports/phase-01-spike.md`: bảng E9…E20 + trích log + mã COM + thời gian; **cập nhật ADR-02/03/04** mục `[chưa xác minh]`.

## Todo List
- [ ] Scaffold + slnx + props + README + .gitignore
- [ ] Bridge: resolver/locator/attachment/self-check/executor spike/cửa sổ tối giản
- [ ] Server: profile + 4 tool + prompts/resources + tests ≥ 14
- [ ] Harness `--phase spike`
- [ ] 👤 ETABS + model bỏ đi + duyệt E9/E10
- [ ] E9…E20 ≥ 2 lần; publish + E16 trên publish; `.mcp.json` 👤
- [ ] Report + ADR update

## Success Criteria
- [ ] `dotnet build HPEtabs/HPEtabs.slnx -c Debug` xanh trên máy dev; `-p:EtabsInstallDir=C:\nope\` fail đúng 1 lỗi; `HPEtabs/HPEtabs.McpBridge/bin/Debug/net8.0-windows/ETABSv1.dll` không tồn tại.
- [ ] `grep -rn "HPRebar/\|HPAutoCad/\|HPNavis/\|HPCivil3D" HPEtabs --include=*.csproj --include=*.slnx` = 0; `grep -rn "ETABSv1\|Autodesk\." HPEtabs/HPEtabs.Mcp.Server HPEtabs/HPEtabs.Mcp.Server.Tests --include=*.csproj` = 0.
- [ ] `dotnet test HPEtabs/HPEtabs.Mcp.Server.Tests` → **≥ 14** pass, 0 fail, 0 skip (không cần ETABS).
- [ ] `python McpShared/tools/mcp-call.py <server exe> tools/list` (registry cách ly) → đúng 12 tên; `initialize` → `serverInfo.name == "HPEtabs MCP"`; không bridge → message chứa "HPEtabs.McpBridge.exe" và "hpetabs-mcp-22", không path máy.
- [ ] Log bridge: `ETABSv1.dll resolved from … (2.10.0.0)`, `MCP scripting self-check OK` (Debug **và** publish folder); cửa sổ "Attached: pid N — model.EDB".
- [ ] `powershell.exe -File HPEtabs/tools/harness/run-live-verify.ps1 -Phase spike -Runs 2` → E16, E17, E18, E19, E20 pass 2/2; E9–E15, E13b có kết luận trong `reports/phase-01-spike.md`.
- [ ] `grep -rn "HPETABS_SPIKE\|ENABLE_EXECUTION" HPEtabs` = 0; `execute` trước tick → `-32001` với text "(a separate app, not inside ETABS)".
- [ ] `git diff --stat McpShared/` rỗng sau phase 1.

## Risk Assessment
- `GetObject` null khi ETABS chạy dưới user/elevation khác — cùng user, không elevated; README.
- `IsWindowEnabled` không phản ánh modal WPF của ETABS .NET 8 → E15 thử `GetForegroundWindow`; không có cách tĩnh → chấp nhận `expireWithoutTicks`.
- Description > 1 500 ký tự → cắt; test đo.
- Exe server bị lock khi `hprebar-etabs` đang chạy trong Claude Code → publish khi MCP tắt.

## Security Considerations
- Chỉ 2 probe ghi có duyệt, chạy qua nút trong cửa sổ (không qua pipe); không `ApplicationExit`, không `CreateObject`; audit từ phase 1.

## Next Steps
- Phase 2 dùng E9–E15/E13b để chốt snapshot, no-model, fingerprint, quiescence; phase 3 seeds cần phase 2.
