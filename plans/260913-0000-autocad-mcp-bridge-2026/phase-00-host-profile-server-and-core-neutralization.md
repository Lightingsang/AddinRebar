---
title: "Phase 0 — Host profile trong server + trung tính hoá McpBridge.Core (không đổi hành vi Revit)"
status: planned
priority: P1
effort: 8h
depends_on: []
created: 2026-09-13
---

# Phase 0 — Host profile & Core neutralization

## Context
- [ADR-01](adr/adr-01-reuse-mcp-server-host-profile.md) (host switch, một instance = một host) · [ADR-04](adr/adr-04-registry-per-host-library-and-host-field.md) (chỉ phần default path theo profile; phần registry còn lại ở phase 4) · [architecture.md §5–6](architecture.md).
- Mã hiện có đọc 2026-09-13: `HPRebar.Mcp.Server/Program.cs`, `Models/BridgeOptions.cs`, `Tools/*.cs`, `Prompts/*.cs`, `Resources/*.cs`, `Services/*.cs`; `HPRebar.McpBridge.Core/Pipe/RequestDispatcher.cs`, `Scripting/ScriptGuard.cs`, `Scripting/ScriptAnalyzer.cs`, `Model/BridgeSettingsStore.cs`; `HPRebar.McpBridge/Service/McpBridgeHost.cs`, `ViewModel/*`.
- Gate hồi quy: `HPRebar.Mcp.Server.Tests` (159 test), publish exe + `tools/list` cho host `revit` vẫn = 34 (4 + 8 + 22 published), bridge Revit **không redeploy**.

## Overview
Đưa khái niệm "host" vào server và Core **mà không thay đổi bất kỳ hành vi nào của host `revit`**. Sau phase này: `HPREBAR_MCP_Host` chưa có giá trị `autocad` nào chạy được (profile AutoCAD đến ở phase 3), nhưng mọi điểm nối đã có: `IHostProfile`, dispatcher chấp nhận prefix, guard/analyzer nhận profile, settings store nhận folder, ViewModel/Host chuyển sang Core.

## Key insights
- Phần phụ thuộc Revit trong server là **text + hằng** (tên tool, description, categories, reserved names, pipe/version), không phải logic → tách thành `IHostProfile` là đủ; không cần extract project (ADR-01 ma trận).
- `[McpServerTool(Name=…)]` là hằng compile-time → mỗi host một tool class mỏng; thân dùng chung qua service. Không đổi tên tool Revit.
- Wire-compat với bridge Revit đã deploy: server host `revit` phải tiếp tục gửi `revit.*` và đọc `revitVersion`; Contracts chỉ **thêm**.
- `WithToolsFromAssembly()` quét mọi `[McpServerToolType]` → tool class host-specific **không** mang attribute type-level (chỉ method attribute) và được đăng ký tường minh theo profile. Kiểm API SDK 2.2.0: `WithTools<T>()`, `WithPrompts<T>()`, `WithResources<T>()` tồn tại? → **verify khi compile**; fallback `WithTools(IEnumerable<McpServerTool>)` + `McpServerTool.Create(MethodInfo, …)`.

## Requirements
Functional
- Config `Host` (top-level, env `HPREBAR_MCP_Host`), mặc định `revit`; giá trị lạ → fail-fast lúc start với thông điệp rõ.
- `IHostProfile` singleton: `HostId`, `DisplayName`, `ServerName`, `DefaultVersion`, `ValidVersions`, `PipeName(int)`, `MethodPrefix`, `Method(string suffix)`, `ExecuteToolName`, `ContextToolName`, `ResourceScheme`, `Categories`, `CoreToolNames`, `SeedResourcePrefix`, `LibraryPathDefault`, `DbPathDefault`, `SettingsFolder`, `ScriptContractSummary` (đoạn text dùng trong prompt `toolify_run`).
- `BridgeOptions.HostVersion` (mới) + `RevitVersion` alias (get/set cùng backing field) — `.mcp.json` hiện tại không phải sửa. `IsValid()` dùng `profile.ValidVersions` (inject qua `IValidateOptions<BridgeOptions>` hoặc validate trong Program sau khi có profile).
- `ExecuteCodeService.ExecuteAsync(code, transaction, dryRun, timeoutSeconds, label, args, progress, ct)` = thân hiện tại của `ExecuteRevitCodeTool` (validate, `ExecuteRequest`, `SendAsync(profile.Method("execute"))`, `RecordAdhoc`, hint). `ContextService.GetAsync(includeSelection, ct)` tương tự.
- Core: `RequestDispatcher` so khớp suffix (`revit.execute` ≡ `autocad.execute`); thông điệp lỗi dùng `hostName` (constructor thêm tham số, default "Revit" để bridge Revit không đổi). `ScriptGuard.Check(code, GuardProfile? profile = null)` — `GuardProfile.Revit` = hành vi hiện tại (messages "in Revit scripts"). `ScriptAnalyzer.Analyze(code, AnalyzerProfile? profile = null)` — mặc định = danh sách type Revit. `BridgeSettingsStore` → instance/`static` với `Configure(productFolder)` hoặc constructor; Revit gọi với `"McpBridge"`.
- `McpBridgeHost`, `IMcpBridgeRunner`, `McpBridgeStatusViewModel` chuyển sang `HPRebar.McpBridge.Core/Host/` và `/ViewModel/` (Core thêm `CommunityToolkit.Mvvm`); `McpBridgeHost` nhận `pipeName`/`hostVersion` tham số thay vì `PipeNaming.For(int.Parse(revitVersion))`. `IRevitExecutor` → `IBridgeExecutor`.
Non-functional
- 0 thay đổi hành vi host `revit`: test cũ pass nguyên, `tools/list` giống hệt, mọi description Revit **giữ nguyên chữ** trừ các tool dùng chung được trung tính hoá (liệt kê trong PR).
- Bridge Revit build xanh dưới `Debug.R26` sau khi đổi namespace (không cần deploy).

## Architecture
```
HPRebar.Mcp.Server/
├── Hosts/IHostProfile.cs · Hosts/HostProfileFactory.cs (đọc config "Host")
├── Hosts/Revit/RevitHostProfile.cs (+ git mv 4 file Revit tool/prompt/resource vào đây)
├── Services/ExecuteCodeService.cs · Services/ContextService.cs
└── Program.cs: builder.Configuration["Host"] → profile → AddSingleton<IHostProfile>; ServerInfo.Name = profile.ServerName;
    .WithToolsFromAssembly() (chỉ shared) + profile.RegisterHostPrimitives(mcpBuilder)
HPRebar.McpBridge.Core/
├── Pipe/RequestDispatcher.cs (suffix) · Pipe/IBridgeExecutor.cs
├── Scripting/GuardProfile.cs · Scripting/AnalyzerProfile.cs
├── Model/BridgeSettingsStore.cs (productFolder)
├── Host/McpBridgeHost.cs · ViewModel/IMcpBridgeRunner.cs · ViewModel/McpBridgeStatusViewModel.cs
HPRebar.Mcp.Contracts/
├── PipeNaming.cs (+ For(string host, int version) → "hprebar-mcp-acad2026"; "revit" → giữ "hprebar-mcp-r2026")
├── JsonRpc/JsonRpcMethods.cs (+ Prefix("autocad") hằng, Suffix(method))
└── Messages/ContextMessages.cs (+ Host, HostVersion, AutocadInfo? Autocad — additive, nullable)
```

## Related code files
- **Tái dùng nguyên:** toàn bộ `Registry/*` (phase 4 mới chạm), `Services/{RevitBridgeClient,NdjsonPipeTransport,ResultFormatter,RegistryStartup,BridgeExceptions}`, Core `Pipe/{PipeListener,NdjsonPipeWriter}`, `Scripting/{ScriptCompiler,ScriptCache,ScriptArgs,TypeInspector}`, `Model/{BridgeSettings,BridgeStatus,AuditLogger}`, Contracts còn lại.
- **Tách ra chung / sửa:** `Program.cs`, `appsettings.json`, `Models/BridgeOptions.cs`, `Tools/ExecuteRevitCodeTool.cs` → `Hosts/Revit/` + `Services/ExecuteCodeService.cs`, `Tools/RevitContextTool.cs` + `Resources/RevitDocumentResources.cs` → `Hosts/Revit/` + `Services/ContextService.cs`, `Prompts/RevitScriptPrompts.cs` → `Hosts/Revit/`, `Tools/{InspectTypeTool,CancelExecutionTool}.cs` (description "the connected CAD host"), `Prompts/ToolifyPrompts.cs` (persona từ profile — text Revit giữ nguyên khi host = revit), Core `RequestDispatcher`, `IRevitExecutor`→`IBridgeExecutor`, `ScriptGuard`, `ScriptAnalyzer`, `BridgeSettingsStore`, `HPRebar.McpBridge/{Service/McpBridgeHost.cs, ViewModel/*}` → Core, `HPRebar.McpBridge/{Application.cs, McpBridgeExternalEventHandler.cs, McpBridgeCommand.cs, View/*.xaml.cs}` chỉ sửa `using`.
- **Viết mới:** `Hosts/IHostProfile.cs`, `Hosts/HostProfileFactory.cs`, `Hosts/Revit/RevitHostProfile.cs`, Core `Scripting/GuardProfile.cs`, `Scripting/AnalyzerProfile.cs`, tests `HostProfileTests.cs`, `DispatcherPrefixTests.cs`, `ExecuteCodeServiceTests.cs`.

## Implementation steps
1. Contracts additive: `PipeNaming.For(host, version)`, `JsonRpcMethods.Suffix/Prefix`, `ContextResult` fields. Build Contracts.
2. Core: `IBridgeExecutor` (rename), `RequestDispatcher` suffix + `hostName`; `GuardProfile`/`AnalyzerProfile` với default Revit; `BridgeSettingsStore(productFolder)`; move `McpBridgeHost` + ViewModel (add `CommunityToolkit.Mvvm` to Core csproj). Build Core + `HPRebar.McpBridge` (`-c Debug.R26 -p:DeployAddin=false`).
3. Server: `IHostProfile` + `RevitHostProfile` (giá trị = hằng hiện tại); `HostProfileFactory` từ config; `BridgeOptions.HostVersion` alias; `ExecuteCodeService`/`ContextService`; git mv 4 file Revit vào `Hosts/Revit/`, đổi thân thành gọi service; `Program.cs` đăng ký host primitives theo profile (verify SDK API); `ServerInfo.Name`.
4. Description trung tính cho tool dùng chung (`InspectTypeTool`, `CancelExecutionTool`, `Tools/Registry/*`, `DynamicToolRegistrar`) — chỉ chữ; giữ nghĩa.
5. Tests: `HostProfileTests` (default revit; env `HPREBAR_MCP_Host=revit` → pipe `hprebar-mcp-r2026`, method `revit.execute`; `bogus` → fail-fast), `DispatcherPrefixTests` (`revit.ping` và `autocad.ping` cùng trả pong qua pipe thật với `FakeRevitExecutor`), `ExecuteCodeServiceTests` (validate size/mode/label như trước). Chạy toàn bộ suite.
6. Publish exe; `mcp_call.py tools/list` (host revit) = 34; `get_revit_context` với Revit 2026 đang mở (bridge cũ) trả kết quả.

## Todo
- [ ] 1 Contracts · [ ] 2 Core + bridge Revit build · [ ] 3 Server profile/services · [ ] 4 descriptions · [ ] 5 tests (159 + mới) · [ ] 6 publish + smoke Revit

## Success criteria
- `dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false` xanh (0 warning mới trong project MCP).
- `dotnet test HPRebar/HPRebar.Mcp.Server.Tests` → 159 cũ pass + ≥ 8 test mới pass.
- `dotnet publish HPRebar/HPRebar.Mcp.Server -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -o HPRebar/output/HPRebar.Mcp.Server` → `mcp_call.py tools/list` = 34 tool, tên không đổi; `get_revit_context` OK với bridge Revit **chưa redeploy**.
- `git diff --stat` không chạm `HPRebar/HPRebar/` (add-in rebar), `docs/`, `CLAUDE.md`.

## Risks
| Risk | Mitigation |
|---|---|
| SDK 2.2.0 không có `WithTools<T>()`/`WithPrompts<T>()` | fallback `McpServerTool.Create(MethodInfo/Delegate)` thêm vào `ToolCollection` như `DynamicToolRegistrar`; prompts/resources tương tự qua `PromptCollection`/`ResourceCollection` |
| Di chuyển `McpBridgeStatusViewModel` sang Core kéo WPF vào Core | ViewModel chỉ dùng CommunityToolkit.Mvvm (kiểm `using`); nếu có `Dispatcher` → giữ ở bridge, chỉ move `IMcpBridgeRunner` + `McpBridgeHost` |
| Đổi tên `IRevitExecutor` phá test/fake | rename toàn solution bằng compile; `FakeRevitExecutor` giữ tên file, implement `IBridgeExecutor` |
| Description trung tính làm AI Revit kém rõ | giữ ví dụ Revit trong prompt Revit; chỉ tool dùng chung đổi chữ |

## Security
Không đổi (ADR-04 Revit). Env `HPREBAR_MCP_Host` không mở thêm quyền: mỗi instance vẫn chỉ nói chuyện với pipe `CurrentUserOnly` của host mình.

## Next steps
Phase 1 (scaffold AutoCAD + spike) có thể chạy **song song** với phase 0 (chỉ cần Core `ScriptCompiler` hiện có); phase 2 và 3 cần phase 0 xong.
