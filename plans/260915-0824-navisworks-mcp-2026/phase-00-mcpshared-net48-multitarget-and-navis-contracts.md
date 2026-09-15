---
phase: 0
title: "McpShared: multi-target net48 + Navis contracts (additive)"
status: completed
priority: P1
effort: "8h"
dependencies: []
---

# Phase 0: `McpShared` đa mục tiêu `net8.0;net48` + hằng/profile Navis (chỉ additive)

## Context Links
- [ADR-01](adr/adr-01-net48-host-multitarget-mcpbridge-core.md) (quyết định, `Net48Tests`, resolver) · [ADR-04 §3](adr/adr-04-navis-main-thread-busy-heavy-ops-guard-globals.md) (`MaxTimeoutSeconds` 3 site; guard Navis) · [ADR-02 §Consequences](adr/adr-02-navis-transaction-dryrun-and-writable-surface.md) (nội dung `GuardProfile.Navis`/`AnalyzerProfile.Navis`) · [ADR-05 §3](adr/adr-05-navis-plugin-packaging-deploy-identity.md) (định danh)
- Bằng chứng: [evidence §E11–E14](research/evidence-on-machine-2026-09-15.md) · Red-team 2026-09-15 (plan.md §Red Team Review)

## Overview
Làm cho engine bridge dùng chung nạp được vào .NET Framework 4.8 **mà không đổi hành vi** Revit/AutoCAD, và thêm các hằng/profile Navis mọi phase sau cần. Không có code Navisworks nào ở phase này. **Bảng dưới là danh sách sửa engine duy nhất của toàn plan** — mọi ADR/phase khác trỏ về đây; thêm gì ngoài bảng = dừng, báo user.

## Danh sách sửa engine (authoritative)

| # | File | Loại | Nội dung | Ảnh hưởng Revit/AutoCAD |
|---|---|---|---|---|
| 1 | `McpShared/HPRebar.McpBridge.Core/HPRebar.McpBridge.Core.csproj` | csproj | `<TargetFrameworks>net8.0;net48</TargetFrameworks>`; `Polyfill 11.0.1 PrivateAssets=all Condition net48` | asset net8 không đổi |
| 2 | `…/Pipe/PipeListener.cs:78–80` | `#if NET48` | `PipeSecurity` + `SetOwner(identity.Owner)` + `PipeAccessRule(identity.Owner, FullControl, Allow)` + ctor overload có `pipeSecurity`; `#else` giữ `CurrentUserOnly` | không |
| 3 | `…/Host/MainThreadQueue.cs:67` | `#if NET48` | clock `Stopwatch.GetTimestamp()*1000/Frequency`; `#else` `Environment.TickCount64` | không |
| 4 | `…/Scripting/GuardProfile.cs:62,65` | `#if NET48` + thêm | property `IReadOnlyCollection<string>` trên net48 (`#else IReadOnlySet<string>`); **thêm** `public static readonly GuardProfile Navis` (nội dung ADR-02 §Consequences: W3 + Forms/Win32/System.Data/Linq.Expressions/Automation/Interop/ComApi/Data; identifiers `MessageBox, Transaction, Expression, Delegate, NavisworksApplication, ComApiBridge, NavisworksCommand, NavisworksConnection, NavisworksDataAdapter`; members `BeginTransaction, Undo, Redo, Rollback, TryUndo, TryRedo, TryRollback, StartDisableUndo, EndDisableUndo, SetModelUnitsAndTransform, SetUserDefined, Database, ToNavisworksConnection, CreateDelegate, Compile`) | không (member mới) |
| 5 | `…/Scripting/AnalyzerProfile.cs:25,28` | `#if NET48` + thêm | như #4; **thêm** `AnalyzerProfile.Navis = (["Transaction"], ["BeginTransaction"])` | không |
| 6 | `McpShared/HPRebar.Mcp.Contracts/PipeNaming.cs` | thêm | `NavisHost = "navis"`; `case NavisHost => "hpnavis-mcp-" + version` (giá trị = nhánh mặc định hiện tại) | không |
| 7 | `…/Contracts/JsonRpc/JsonRpcMethods.cs` | thêm | `NavisPrefix = "navis."` | không |
| 8 | `…/Contracts/HostScriptContracts.cs` | thêm | `NavisImports`, `NavisGlobals` (ADR-04 §4) | không |
| 9 | `…/Contracts/Messages/ContextMessages.cs` | thêm | `ContextResult.Navis : NavisInfo?`; `record NavisInfo(string DocumentUnits, int ModelCount, IReadOnlyList<ModelSummary> Models, int SelectionSetCount, int SavedViewpointCount, int ClashTestCount, bool HasClashModule, bool HeavyOperationsEnabled, bool IsClear, bool IsBusy, bool IsModified)`; `record ModelSummary(string FileName, string Units, string? SourceFileName)` | null → bị bỏ (`WhenWritingNull`) |
| 10 | `McpShared/HPRebar.Mcp.Server.Core/Hosts/IHostProfile.cs` + `HostProfile.cs` | thêm | `int MaxTimeoutSeconds { get; }` / `{ get; init; } = 120`; `WithHostAssembly` copy field | Revit/AutoCAD = 120 |
| 11 | `…/Server.Core/Services/ExecuteCodeService.cs:59` | đổi hằng → profile | `Math.Clamp(timeoutSeconds, MinTimeoutSeconds, profile.MaxTimeoutSeconds)` | 120 → 120 |
| 12 | `…/Server.Core/Registry/ToolManager.cs:173` | đổi hằng → profile | `Math.Clamp(record.TimeoutSeconds, 5, _bridge.Profile.MaxTimeoutSeconds)` | 120 → 120 |
| 13 | `…/Server.Core/Registry/ToolValidator.cs:52` | đổi hằng → profile | `> profile.MaxTimeoutSeconds`; message dùng số của profile | 120 → 120, text giống |
| 14 | `McpShared/McpShared.slnx` | thêm | project `HPRebar.McpBridge.Core.Net48Tests` | không |
| — | **Không đổi** | | `AuditEntry`, `BridgeSettings`, `BridgeSettingsStore`, `IMcpBridgeRunner`, `McpBridgeStatusViewModel`, `McpBridgeHost`, `RequestDispatcher`, `IBridgeExecutor`, `ToolRecord`, `AnalyzeRequest`, `ExecuteRequest`, `ToolLifecycleTools` description "5–120" | |

## Requirements
- Functional: `HPRebar.McpBridge.Core` build `net48` + `net8.0`; `Net48Tests` pass; bảng trên hiện thực đủ.
- Non-functional: `git diff McpShared/HPRebar.McpBridge.Core -- '*.cs'` chỉ chạm #2–#5, mọi dòng `-` nằm trong `#if NET48`/`#else`; 96 + 109 + 58 test pass; `tools/list` Revit 34 / AutoCAD 24 byte-identical **trên exe vừa build**; build R26 + AutoCAD xanh.

## Architecture
- Consumer chọn asset theo nearest-TFM: net8/net10 → `net8.0`; `HPNavis.McpBridge` (net48) → `net48`.
- `HPRebar.McpBridge.Core.Net48Tests` (net48, xunit.v3 3.1.0 có lib `net472`): **link** `ScriptGuardTests.cs`, `MainThreadQueueTests.cs`, `Fakes/FakeRevitExecutor.cs`; **mới** `ScriptCompilerNet48Tests.cs` (references từ `typeof(object).Assembly`, `typeof(Enumerable).Assembly`, `typeof(List<>).Assembly`, `typeof(ScriptArgs).Assembly` — không `Assembly.Load("System.Runtime")`), `PipeListenerNet48Tests.cs` (`PipeListener` + `FakeRevitExecutor` + `NamedPipeClientStream` thô NDJSON: ping/context/execute/-32001). **Không** link `PipeRoundTripTests` (Server.Core net10), `ScriptCompilerTests`/`ScriptArgsAndAnalyzerTests` (partial-name load).

## Related Code Files
- Modify: #1–#14 trên.
- Create: `McpShared/HPRebar.McpBridge.Core.Net48Tests/{HPRebar.McpBridge.Core.Net48Tests.csproj, ScriptCompilerNet48Tests.cs, PipeListenerNet48Tests.cs}`.
- Modify (test mới, additive) `McpShared/HPRebar.Mcp.Server.Core.Tests/`: `PipeNaming.For("navis",2026)=="hpnavis-mcp-2026"`; `GuardProfile.Navis` cấm `BeginTransaction`, `new Transaction(doc,"x")`, `Undo`, `doc.Database`, `NavisworksCommand`, `Expression.Call`, `Delegate.CreateDelegate`, `System.Data`; `AnalyzerProfile.Navis` báo manual cho `BeginTransaction` và `new Transaction`; `HostProfile.Revit.MaxTimeoutSeconds==120` và `AutocadHostProfile` 120; `ExecuteCodeService`/`ToolManager`/`ToolValidator` clamp 120 với profile Revit và 600 với profile giả `MaxTimeoutSeconds=600`; `ContextService.Shape` với `HostId=navis` không có `revitVersion/isFamily` và giữ `navis`; `ContextResult` không có `Navis` serialize không có key `navis`.
- Create: `plans/260915-0824-navisworks-mcp-2026/reports/phase-00-tools-list-{before,after}-{revit,autocad}.json` + `phase-00-core-dll-*.sha256` (+ `snapshot-tools-list.ps1`).

## Implementation Steps
1. **Baseline:** `dotnet build HPRebar/HPRebar.Mcp.Server -c Release` và `dotnet build HPAutoCad/HPAutoCad.Mcp.Server -c Release`; `python HPAutoCad/tools/harness/mcp-call.py <bin/Release/net10.0/…exe> tools/list` với env registry cách ly (`HPREBAR_MCP_Registry__LibraryPath/DbPath`, `HPAUTOCAD_MCP_…`) → lưu JSON + SHA-256 của `HPRebar.Mcp.Server.Core.dll` cạnh exe vào `reports/` (exe chỉ là apphost stub, không đổi khi rebuild). Chạy 3 suite → ghi số baseline (96/109/58).
2. #1: multi-target + Polyfill. Build `-f net48` → kỳ vọng đúng 4 lỗi `IReadOnlySet` + 2 lỗi body (E12); khác → dừng, cập nhật ADR-01.
3. #2–#5 phần `#if NET48`; build hai TFM xanh, không warning mới.
4. #4–#13 phần thêm (Navis profiles, Contracts, `MaxTimeoutSeconds` + 3 site).
5. #14 + `Net48Tests`; `(cd McpShared && dotnet test HPRebar.McpBridge.Core.Net48Tests)`. Nếu MTP không chạy exe net48 → project này dùng `xunit` 2.9 + `Microsoft.NET.Test.Sdk` (ghi rõ).
6. Test mới Server.Core.Tests.
7. **Gate hành vi cũ:** 3 suite cũ nguyên số; rebuild Release hai exe → `tools/list` after = before (diff rỗng) và SHA `HPRebar.Mcp.Server.Core.dll` **khác** before (chứng minh đã rebuild); `dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false`; `dotnet build HPAutoCad/HPAutoCad.slnx -c Debug -p:DeployBundle=false`.
8. `reports/phase-00-report.md`.
9. **Bước 0b (user xác nhận 2026-09-15, commit riêng sau gate #7 xanh):** thêm vào deny-list **gốc** `ScriptGuard.cs` — `DeniedNamespaces += System.Linq.Expressions`; `DeniedIdentifiers += Expression, Delegate`; `DeniedMembers += CreateDelegate, Compile, Method` (`((Func<int>)(() => 1)).Method` trả `MethodInfo` mà không nêu tên type — phát hiện khi viết `NavisProfileTests` 2026-09-15; `Invoke` trên `MethodInfo` cũng cần cân nhắc); test `ScriptGuardTests` mới cho 4 mẫu (`Expression.Call(...).Compile()`, `Delegate.CreateDelegate`, `Expression.Lambda`, literal `"System.Linq.Expressions"`); chạy lại 96+109+58 + snapshot `tools/list` (mô tả tool không đổi → vẫn byte-identical). Ghi vào CLAUDE.md phần security model của Revit/AutoCAD.

## Todo List
- [x] Baseline (exe build + tools/list + SHA + 3 suite)
- [x] #1–#3 multi-target + shims
- [x] #4–#13 additive
- [x] `Net48Tests` xanh
- [x] Test mới Server.Core.Tests
- [x] Gate byte-identical + build R26/AutoCAD
- [x] Report
- [x] 0b: deny-list gốc (commit riêng)

## Success Criteria
- [ ] `dotnet build McpShared/McpShared.slnx -c Debug` xanh; `bin/Debug/net48/HPRebar.McpBridge.Core.dll` tồn tại.
- [ ] `git diff --stat McpShared/HPRebar.McpBridge.Core -- '*.cs'` liệt kê đúng `PipeListener.cs, MainThreadQueue.cs, GuardProfile.cs, AnalyzerProfile.cs`; mọi dòng `-` trong diff nằm giữa `#if NET48`…`#else`…`#endif`.
- [ ] `(cd McpShared && dotnet test HPRebar.Mcp.Server.Core.Tests)` = 96 + N mới pass; `HPRebar.Mcp.Server.Tests` = 109; `HPAutoCad.Mcp.Server.Tests` = 58; 0 fail.
- [ ] `(cd McpShared && dotnet test HPRebar.McpBridge.Core.Net48Tests)` — **mọi** test link + mới pass; runner log ghi `.NET Framework 4.8`.
- [ ] `diff reports/phase-00-tools-list-before-revit.json reports/…-after-revit.json` rỗng (33 tool trên registry root cách ly — 34 của CLAUDE.md gồm 1 tool user đã approve trong registry live) và AutoCAD rỗng (24); SHA `HPRebar.Mcp.Server.Core.dll` after ≠ before.
- [ ] Build R26 + AutoCAD xanh.

## Risk Assessment
- MTP runner với exe net48 chưa thử → fallback xunit v2 riêng project (ghi rõ).
- `IHostProfile` thêm member → implementer ngoài `HostProfile`? Grep 2026-09-15: chỉ `HostProfile`. Thấp.
- `ToolValidator`/`ToolManager` đã nhận profile? `ToolManager` có `_bridge.Profile`; `ToolValidator` nhận categories từ profile (AutoCAD phase 4) — kiểm ctor khi sửa; nếu chưa có → thêm tham số profile với default `HostProfile.Revit` (additive).
- Shim `PipeSecurity` chỉ được test net48↔net48 thô ở phase 0; net10 client ↔ net48 server kiểm thật ở phase 3.
