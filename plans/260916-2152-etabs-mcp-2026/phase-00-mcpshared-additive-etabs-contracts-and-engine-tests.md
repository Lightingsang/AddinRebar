---
phase: 0
title: "McpShared: hằng/profile/DTO/hint Etabs additive + engine tests + gate byte-identical 3 host"
status: completed
priority: P1
effort: "4h"
dependencies: []
---

# Phase 0: `McpShared` — chỉ thêm, không đổi hành vi Revit/AutoCAD/Navis

## Context Links
- [ADR-02 §1, §Consequences](adr/adr-02-no-transaction-snapshot-tiers-and-opt-ins.md) (`ExecuteResult.Snapshot` tên file, `AnalyzeRequest.Transaction`, `-32001` cho D-off) · [ADR-04 §3, §5–6](adr/adr-04-sta-worker-busy-cancel-units-guard-globals.md) (hints, guard, globals, imports) · [ADR-05 §1](adr/adr-05-identity-registry-packaging.md) (định danh, text hint) · [red-team](reports/red-team-2026-09-16.md) hàng 3, 4, 5, 7, 11, 12, 14a/b
- Mẫu: Navis phase 0 (`../260915-0824-navisworks-mcp-2026/phase-00-…md`, commit `fb65f25`); `plans/260915-0824-navisworks-mcp-2026/reports/snapshot-tools-list.ps1` (tồn tại) cho snapshot `tools/list`.
- Seam: `McpShared/HPRebar.Mcp.Contracts/PipeNaming.cs:20,40` · `JsonRpc/JsonRpcMethods.cs:37` · `HostScriptContracts.cs:42–68` · `Messages/ContextMessages.cs:27,78–89` · `Messages/ExecuteResult.cs:11–44` · `Messages/AnalyzeMessages.cs:6` (`AnalyzeRequest(string Code)`) · `JsonRpc/BridgeJson.cs:18` (`WhenWritingNull`) · `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs:54–81` · `AnalyzerProfile.cs:23–25` (`Navis` static — precedent) · `Pipe/RequestDispatcher.cs:31,132–134` · `Host/McpBridgeHost.cs:35–46` · `McpShared/HPRebar.Mcp.Server.Core/Hosts/HostProfile.cs:14–35,95–101` (`WithHostAssembly` copy) · `Services/RevitBridgeClient.cs:64,85,139` · `Bootstrap/McpServerHost.cs:94–104` · `Models/BridgeOptions.cs:17` (`HostVersion = 2026`) · `Registry/ToolLifecycleService.cs:50` (`new AnalyzeRequest(code)`) · `Registry/ToolManager.cs:178–186` · `Services/ContextService.cs:52–65` (`Shape`)

## Overview
Thêm mọi hằng/profile/DTO/hint mà phase 1–4 cần, kèm test engine, và chứng minh 3 host cũ **byte-identical** (`tools/list`, input schema) + nguyên số test. Không code ETABS, không tham chiếu `ETABSv1`. **Bảng dưới là danh sách sửa engine duy nhất của toàn plan** — thêm gì ngoài bảng = dừng, báo user. Audit `Shape` (`ContextService.cs:52–65`): non-Revit → bỏ `revitVersion`/`isFamily`, giữ block khác; `etabs` null bị bỏ (`BridgeJson.cs:18`) → **không sửa `Shape`**, chỉ test.

## Danh sách sửa engine (authoritative)

| # | File | Loại | Nội dung | Ảnh hưởng 3 host |
|---|---|---|---|---|
| 1 | `Contracts/PipeNaming.cs` | thêm | `EtabsHost = "etabs"` + `case EtabsHost => "hpetabs-mcp-" + version` (= nhánh mặc định `:40`) | không |
| 2 | `Contracts/JsonRpc/JsonRpcMethods.cs` | thêm | `EtabsPrefix = "etabs."` sau `:37` | không |
| 3 | `Contracts/HostScriptContracts.cs` | thêm (append cuối; **merge caution** — diff uncommitted của plan AEC tại `AutocadImports`) | `EtabsImports = { "System", "System.Linq", "System.Collections.Generic", "ETABSv1", "HPRebar.McpBridge.Core.Scripting" }`; `EtabsGlobals = { "sapModel", "etabs", "units", "ct", "log", "progress", "args" }`; `const int EtabsHeavyMaxTimeoutSeconds = 600` | không |
| 4 | `Contracts/Messages/ContextMessages.cs` | thêm | `public EtabsInfo? Etabs { get; set; }` sau `:27`; `record EtabsInfo(bool IsAttached, int AttachedPid, string OapiVersion, bool IsLocked, string PresentUnits, string DatabaseUnits, bool DestructiveOperationsEnabled, int PointCount, int FrameCount, int AreaCount)` — **10 field** (red-team #12); path/name/version/busy qua `DocPath/DocTitle/HostVersion/IsModifiable` | null → bị bỏ |
| 5 | `Contracts/Messages/ExecuteResult.cs` | thêm | `public string? Snapshot { get; set; }` — **tên file** snapshot (không thư mục; red-team #3) | null → bị bỏ |
| 6 | `Contracts/Messages/AnalyzeMessages.cs:6` | thêm tham số cuối | `AnalyzeRequest(string Code, string? Transaction = null)` — bridge ETABS trả `PREVIEW` khi `none` + tier ≥ W (ADR-02 §1; dẫn xuất red-team #4); bridge cũ bỏ qua field | server 3 host gửi thêm key `transaction` trên `*.analyze` (wire bridge; **không** phải `tools/list`) — bridge cũ ignore |
| 7 | `Server.Core/Registry/ToolLifecycleService.cs:50` | sửa 1 đối số | `new AnalyzeRequest(code, candidate.Transaction)` (truyền transaction của proposal) | hành vi 3 host không đổi (bridge của họ không đọc) |
| 8 | `Core/Scripting/GuardProfile.cs` | thêm sau `:74` | `Etabs = new GuardProfile("ETABS", deniedIdentifiers: ["Helper", "MessageBox"], deniedMembers: ["ApplicationExit", "ApplicationStart", "Hide", "Unhide", "SetAsActiveObject", "UnsetAsActiveObject", "InternalExec"], deniedNamespaces: ["System.Windows.Forms", "HPEtabs.McpBridge", "HPRebar.McpBridge.Core.Host"])` — **không** `Marshal`/`InteropServices` (base `ScriptGuard.cs:23,36`), không member `cHelper` (red-team #11) | không |
| 9 | `Core/Scripting/AnalyzerProfile.cs` | thêm sau `:25` | `Etabs = new AnalyzerProfile([], [])` — mirror precedent `Navis` static (`:23`) | không |
| 10 | `Server.Core/Hosts/IHostProfile.cs` + `HostProfile.cs` | thêm | `string? BridgeNotConnectedHint { get; }`, `string? TimeoutSemanticsHint { get; }` (`init`, mặc định null; `WithHostAssembly` copy `:95–101`) — text ETABS ở ADR-05 §1 | Revit/AutoCAD/Navis null |
| 11 | `Server.Core/Services/RevitBridgeClient.cs:85,139` | null-coalesce | `:85` message = `profile.TimeoutSemanticsHint ?? <câu cũ>`; `:139` = `profile.BridgeNotConnectedHint ?? <câu cũ>` (red-team #7) | text cũ nguyên văn khi null |
| 12 | `Core/Pipe/RequestDispatcher.cs:31,132–134` + `Host/McpBridgeHost.cs:35–46` | ctor param cuối | `string? executionDisabledMessage = null` → `:133` dùng `?? <câu cũ>`; `McpBridgeHost` truyền xuống | text cũ khi null |
| 13 | `Server.Core/Bootstrap/McpServerHost.cs:94–104` | thêm 1 `.Configure` | `.Configure(options => options.HostVersion = profile.DefaultVersion)` **trước** `.Bind` (red-team #14b: `BridgeOptions.HostVersion` mặc định 2026 `BridgeOptions.cs:17`, không env/appsettings → ETABS "out of range") | Revit/AutoCAD/Navis `DefaultVersion` = 2026 = mặc định cũ |
| 14 | test (không code) | chứng minh | `run_tool` với bridge trả `-32001` → **không** ghi run: trace `RevitBridgeClient.cs:64` ném `BridgeErrorException` → `ToolManager.RunAsync` chỉ bắt `BridgeTimeoutException` (`:178–186`) → `Record` không chạy. Test pin bằng fake executor ném `BridgeRequestException(ExecutionDisabled)`; **nếu** test thấy có ghi → thêm hàng "filter additive trong `Record`" vào bảng này (red-team #5) | — |
| 15 | `Core/Scripting/ScriptGuard.cs` `IsDeniedNamespace` | **thêm sau code review 2026-09-16** (1 dòng) | strip tiền tố `global::` trước khi so namespace — `global::System.IO.File.WriteAllText(...)`, `global::System.Diagnostics.Process.Start(...)`, `global::HPRebar.McpBridge.Core.Host.McpBridgeHost.Current` lọt qua mọi profile vì chuỗi bắt đầu bằng `global::` và `File`/`Process` ở vị trí member bị `VisitIdentifierName` bỏ qua. Lỗ có sẵn từ trước (3 host); vá làm denial `HPRebar.McpBridge.Core.Host` của ETABS có tác dụng. **Ngoài bảng gốc — user có thể revert 1 dòng** | script hợp lệ không dùng `global::` → không đổi; script cố lách → giờ bị chặn |
| 16 | `Server.Core/Services/ResultFormatter.cs` `FromExecute` | **thêm sau code review** (1 dòng) | `result.Snapshot = Path.GetFileName(result.Snapshot)` khi khác null — "last line of defence" như `StripPaths` cho `Message`; bridge lỡ gửi full path cũng không lọt username | null → không đổi (3 host) |
| — | **Không đổi** | | `ExecuteRequest` (**không** `Snapshot`), `BridgeSettingsStore` (tạo host-side như `HPAutoCad/HPAutoCad.McpBridge/BridgeEntry.cs:66`), `BridgeSettings`, `IBridgeExecutor`, `MainThreadQueue`, `PipeListener`, `ScriptGuard` base deny-list (chỉ #15 chuẩn hoá alias), `ScriptCompiler`, `ScriptUnits`, `ContextService.Shape`, `ExecuteCodeService`, `ToolManager`, `ToolValidator`, `HostProfile.Revit` (mặc định mọi ctor), meta tool descriptions, `Net48Tests` | |

Ghi chú sau review: `EtabsInfo` — 3 field `OapiVersion/PresentUnits/DatabaseUnits` là `string?` (null khi chưa attach; precedent `AutocadInfo.CurrentLayout`), số/tên field không đổi.

## Requirements
- Functional: #1–#13 hiện thực; #14 chứng minh; test mới xanh.
- Non-functional: `git diff McpShared -- '*.cs'` chỉ thêm + 4 sửa có kiểm soát (#7, #11, #12, #13); 5 suite cũ nguyên số; `tools/list` 3 host byte-identical **trên exe rebuild**; 3 build Debug host xanh.

## Architecture
- Contracts net48 asset (Navis) tự nhận `EtabsInfo`/`Snapshot`/`AnalyzeRequest.Transaction` — record/property thuần.
- `EtabsInfo` positional record như `NavisInfo` (`ContextMessages.cs:78`); `IsAttached=false` cho phép `context` trả về khi chưa attach (execute → `-32003`).

## Related Code Files
- Modify: #1–#13.
- Create (test) `McpShared/HPRebar.Mcp.Server.Core.Tests/EtabsProfileTests.cs` (mirror `NavisProfileTests.cs:48–223`): `PipeNaming.For("etabs",22)=="hpetabs-mcp-22"` (= nhánh mặc định); `JsonRpcMethods.For(EtabsPrefix,"execute")`/`Suffix`; `RequestDispatcher(FakeRevitExecutor, …, "22", "ETABS", executionDisabledMessage: "…HPEtabs MCP Bridge window (a separate app…)")` → `-32001` mang text mới; ctor không tham số → text cũ byte-identical; `GuardProfile.Etabs` cấm `etabs.ApplicationExit(false)`, `new Helper()`, `Helper.GetObject(...)`, `HPEtabs.McpBridge.BridgeEntry.Stop()`, `HPRebar.McpBridge.Core.Host.McpBridgeHost.Current`, `MessageBox.Show`; base vẫn cấm `Marshal.GetActiveObject`/`System.Runtime.InteropServices`; cho phép `sapModel.PointObj.GetNameList(...)`, `sapModel.FrameObj.SetSection(...)`, `sapModel.Analyze.RunAnalysis()`; `AnalyzerProfile.Etabs.UsesTransaction==false`; profile giả `MaxTimeoutSeconds=600` → clamp 600 (`ExecuteCodeService`, `ToolValidator`, `ToolManager`); hint null → `RevitBridgeClient` text cũ; hint set → text mới; `ConfigureOptions` với profile `DefaultVersion=22`, không config → `HostVersion==22`, `IsValid`; với appsettings `Bridge:HostVersion=22` → 22; profile Revit không config → 2026; `Shape` `HostId="etabs"` bỏ `revitVersion/isFamily`, giữ `etabs`; không `Etabs` → không key; `ExecuteResult.Snapshot` null → không key; `AnalyzeRequest` serialize không `Transaction` → không key, có → key `transaction`; **#14** `ToolManager.RunAsync` + fake executor ném `BridgeRequestException(ExecutionDisabled)` → `BridgeErrorException` ném ra, `runs` count không đổi.
- Create: `reports/phase-00-baseline.md` (số test 5 suite + số tool `tools/list` 3 host + SHA Core.dll **đo lúc chạy**; hôm nay đo: AutoCAD server tests 127, AutoCAD tools 37 — plan AEC còn đổi) + `reports/phase-00-tools-list-{before,after}-{revit,autocad,navis}.json`.

## Implementation Steps
1. **Baseline:** rebuild Release 3 exe; `python McpShared/tools/mcp-call.py <exe> tools/list` (registry cách ly `HPREBAR_MCP_Registry__LibraryPath/DbPath`, `HPAUTOCAD_MCP_…`, `HPNAVIS_MCP_…`) → JSON + SHA `HPRebar.Mcp.Server.Core.dll`; chạy 5 suite → ghi `reports/phase-00-baseline.md`.
2. #1–#6 Contracts; #7 `ToolLifecycleService`; build `McpShared/McpShared.slnx`.
3. #8–#9 Core profiles; #10–#13 Server.Core + dispatcher/host ctor.
4. `EtabsProfileTests.cs`; `cd McpShared && dotnet test HPRebar.Mcp.Server.Core.Tests` (không `--nologo`).
5. **Gate:** 5 suite = baseline; rebuild Release 3 exe → `tools/list` after = before (diff rỗng), SHA Core.dll khác; 3 build Debug host xanh.
6. `reports/phase-00-report.md`.

## Todo List
- [x] `reports/phase-00-baseline.md` + 3 `tools/list` + SHA (128/60/109/136/49; 33/37/24)
- [x] #1–#7 Contracts + lifecycle
- [x] #8–#13 Core/Server.Core (+ #15 `global::`, #16 `Snapshot` strip sau review)
- [x] 34 test: `EtabsProfileTests.cs` (18) + `EtabsBridgeMessagesTests.cs` (16, incl. #14) + `EtabsTestProfile.cs`
- [x] Gate byte-identical + nguyên số (2 lần: sau implement, sau review)
- [x] Report — `reports/phase-00-report.md`, `code-review-phase-00.md` (9/10), `test-report-phase-00.md`

## Success Criteria
- [x] `cd McpShared && dotnet test HPRebar.Mcp.Server.Core.Tests` → **162** = 128 + 34, 0 fail, 0 skip (3 lần liên tiếp, không flaky).
- [x] `cd McpShared && dotnet test HPRebar.McpBridge.Core.Net48Tests` → 60 = baseline.
- [x] Revit 109 = baseline · Navis 49 = baseline · AutoCAD 153 ≠ 136 — **không do phase này**: session AEC song song thêm 3 seed untracked (`cad_standards_check`, `audit_aec_drawing`, `create_issue_markup`) + `SeedLibraryTests` rows sau baseline; 0 fail.
- [x] `tools/list` Revit 33 + Navis 24 byte-identical; AutoCAD 37 → 40 = đúng 3 seed AEC mới, **37 tool chung byte-identical** (so theo tên, `reports/phase-00-report.md`); SHA Core.dll `A546DECE…` → `40C1FEE3…`.
- [x] `git diff --stat McpShared -- '*.cs'` = 17 file code (#1–#13 = 15 file vì #10 và #12 mỗi hàng 2 file; + #15 `ScriptGuard.cs`, #16 `ResultFormatter.cs`) + test (`EtabsProfileTests.cs`, `EtabsBridgeMessagesTests.cs`, `EtabsTestProfile.cs` mới; `Fakes/FakeRevitExecutor.cs` +`LastAnalyzeRequest`); `grep -rn "ETABSv1\|Autodesk\." McpShared --include=*.csproj` = 0.
- [x] `dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false` · `HPAutoCad/HPAutoCad.slnx -c Debug -p:DeployBundle=false` · `HPNavis/HPNavis.slnx -c Debug -p:DeployPlugin=false` → 0 error ×3.

## Risk Assessment
- #6 đổi chữ ký positional record `AnalyzeRequest`: caller positional vẫn compile; wire thêm key `transaction` tới bridge Revit/AutoCAD/Navis đã deploy → System.Text.Json bỏ qua mặc định (1 test deserialize).
- #13: `Configure` trước `Bind` để env/appsettings vẫn thắng — test 3 trường hợp.
- Merge với plan AEC trên `HostScriptContracts.cs` → append cuối, review diff bằng mắt.

## Security Considerations
- Guard ETABS chỉ thêm deny; base list không đổi. Hint text không path máy.

## Next Steps
- Phase 1 (scaffold 2 exe + spike) sau phase 0; phase 2 cần kết luận spike.
