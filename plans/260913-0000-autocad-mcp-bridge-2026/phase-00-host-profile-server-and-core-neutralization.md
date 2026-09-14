---
title: "Phase 0 — Tách mã chung ra `McpShared/` (ADR-06) + scaffold folder `HPAutoCad/`; Core host-neutral; hành vi Revit không đổi"
status: planned
priority: P1
effort: 14h
depends_on: []
created: 2026-09-13
revised: 2026-09-14 (ADR-06 — thay "host profile trong một exe" bằng "tách McpShared + folder riêng")
---

# Phase 0 — Extract `McpShared/`, scaffold `HPAutoCad/`, Core neutralization

> Revised 2026-09-14. Bản 2026-09-13 (host switch `HPREBAR_MCP_Host` trong một exe, `Hosts/Autocad/` trong `HPRebar/`) **bị thay** bởi ADR-06. Phase này là **phase di dời không đổi hành vi**: sau khi xong, Revit MCP chạy y hệt (cùng exe name, cùng pipe, cùng registry root, cùng 34 tool, bridge Revit không redeploy).

## Context
- [ADR-06](adr/adr-06-one-mcp-one-folder.md) (ma trận a/b/c → (b) `McpShared/`; fix `ResolveConfigurationsModule`; tên assembly giữ) · [ADR-04 revised](adr/adr-04-registry-per-host-library-and-host-field.md) (root theo product, `SeedInstaller(hostAssembly)`) · [architecture.md §5–6](architecture.md).
- Mã đọc 2026-09-13/14: `HPRebar/HPRebar.slnx` (path tương đối, folder `/Mcp/`, `/Tests/`), `HPRebar/global.json`, `HPRebar/.sourcyroot`, `build/Modules/*` (chỉ ref `Projects.HPRebar*`, `Projects.Installer`, `Solutions.HPRebar`; `ResolveConfigurationsModule.cs:27` `FindFile(".slnx")`), `HPRebar.Mcp.Server/Program.cs` (builder + `AddEnvironmentVariables("HPREBAR_MCP_")` + DI + `WithToolsFromAssembly()` + `registry` CLI branch), `Models/BridgeOptions.cs`, `Tools/*`, `Prompts/*`, `Resources/*`, `Services/*`, `Registry/*`, `HPRebar.McpBridge.Core/*`, `HPRebar.McpBridge/{Service/McpBridgeHost.cs, ViewModel/*}`, `HPRebar.Mcp.Server.Tests/*`.
- Gate hồi quy: tổng test = 159 (chia hai suite) + mới; publish `HPRebar.Mcp.Server` → `tools/list` = 34; `get_revit_context` với bridge Revit **đã deploy** (commit 9b83aee).

## Overview
1. Tạo `McpShared/`; git mv `HPRebar.Mcp.Contracts`, `HPRebar.McpBridge.Core`; tách `HPRebar.Mcp.Server.Core` (class lib) khỏi exe Revit; git mv test engine → `HPRebar.Mcp.Server.Core.Tests`; `McpShared.slnx` + `global.json`.
2. Sửa `HPRebar.slnx` (path `../McpShared/…`), `ResolveConfigurationsModule` (1 dòng), exe Revit còn `Program.cs` mỏng + `Hosts/Revit/*` + seed.
3. Core host-neutral: dispatcher suffix, `IBridgeExecutor`, `GuardProfile`/`AnalyzerProfile`, `ScriptUnits`, `BridgeSettingsStore(vendor, product)`, `McpBridgeHost`/ViewModel vào Core.
4. Scaffold `HPAutoCad/` **rỗng có cấu trúc**: `HPAutoCad.slnx`, `global.json`, `README.md` (project thật đến ở phase 1/3).
5. `CLAUDE.md` § Repository Layout + lệnh build/test (bước cuối phase, ngoài scope plan hiện tại).

## Key insights
- Phần phụ thuộc Revit trong server là **text + hằng + 4 tool/prompt/resource** — tách thành `IHostProfile` + `Hosts/Revit/*` là đủ; phần còn lại (~80 %) là engine → `Server.Core`.
- `WithToolsFromAssembly()` mặc định quét **assembly gọi** → sau khi tách, bootstrap phải gọi hai lần: `WithToolsFromAssembly(typeof(ToolRegistryQueryTools).Assembly)` (meta tools trong Core) + `WithToolsFromAssembly(profile.HostAssembly)`; tương tự prompts/resources. Overload nhận `Assembly` **verify khi compile** (SDK 2.2.0); fallback `McpServerTool.Create(MethodInfo…)`.
- `SeedInstaller.LoadSeeds(assembly ??= typeof(SeedInstaller).Assembly)` — sau tách, mặc định sẽ trỏ vào `Server.Core` (không có seed) → **phải** truyền `profile.HostAssembly` từ `RegistryStartup`.
- `ResolveConfigurationsModule.FindFile(".slnx")` sẽ thấy `McpShared.slnx`/`HPAutoCad.slnx` → pin `Solutions.HPRebar`.
- Wire-compat: Contracts chỉ additive; server Revit vẫn gửi `revit.*`, đọc `revitVersion`.
- Tên assembly/namespace **giữ nguyên** (git rename detection, không churn `using`); rename `HPMcp.*` là bước sau.

## Requirements
Functional — `McpShared/`
- `McpShared/McpShared.slnx` (configurations `Debug`/`Release`): `HPRebar.Mcp.Contracts`, `HPRebar.McpBridge.Core`, `HPRebar.Mcp.Server.Core`, `HPRebar.Mcp.Server.Core.Tests`. `McpShared/global.json` = copy `HPRebar/global.json`. `README.md`: mục đích + rule (không `Autodesk.*`, không tên host trong text; host đến qua `IHostProfile`).
- `HPRebar.Mcp.Server.Core.csproj` (net10.0, class lib): packages `ModelContextProtocol` 2.2.0, `Microsoft.Extensions.Hosting` 10.0.12, `Microsoft.Data.Sqlite` 10.0.12 (chuyển từ exe), `InternalsVisibleTo` hai test project. Nội dung theo [architecture §5](architecture.md#5-layout-ba-folder--mới--sửa--git-mv--giữ).
- `Bootstrap/McpServerHost.CreateBuilder(string[] args, IHostProfile profile)`: thân `Program.cs` hiện tại tham số hoá: `ContentRootPath=AppContext.BaseDirectory`, stderr logging, `AddEnvironmentVariables(profile.EnvPrefix)`, options `Bridge`/`Registry` (+ `PostConfigure` root theo `profile.ProductFolder`), DI (client, formatter, registry, hosted service), `AddMcpServer(ServerInfo.Name = profile.ServerName, ListChanged)`, `WithStdioServerTransport`, tools/prompts/resources từ **hai** assembly, nhánh `registry` CLI → `RegistryCli.RunAsync`. Trả `HostApplicationBuilder`; exe gọi `Build().RunAsync()`.
- `Hosts/IHostProfile` (thuộc tính như architecture §5). `RevitHostProfile` (ở exe Revit): `HostId="revit"`, `DisplayName="Revit"`, `ServerName="HPRebar Revit MCP"`, `ProductFolder="HPRebar"`, `EnvPrefix="HPREBAR_MCP_"`, `DefaultVersion=2026`, `ValidVersions={2025,2026}`, `PipeName(v)=PipeNaming.For(v)`, `MethodPrefix="revit."`, tên tool hiện tại, `Categories` 7 hiện tại, `HostAssembly=typeof(RevitHostProfile).Assembly`.
- `Services/ExecuteCodeService.ExecuteAsync(code, transaction, dryRun, timeoutSeconds, label, args, progress, ct)` = thân `ExecuteRevitCodeTool.ExecuteAsync` (validate, `ExecuteRequest`, `SendAsync(profile.Method("execute"))`, `RecordAdhoc`, hint); `ContextService.GetAsync(includeSelection, ct)`.
- `BridgeOptions.HostVersion` + alias `RevitVersion` (cùng backing field, để `.mcp.json` hiện tại không phải sửa); `IsValid(profile.ValidVersions)`.
- Registry (ADR-04 §1–3, phần tối thiểu cho phase này): `RegistryOptions` root theo product; `SeedInstaller.Install(store, logger, hostAssembly)`; `ToolRecord.Host` (default = host của exe khi thiếu). Phần categories/validator/text theo profile → phase 4.
Functional — Core (`McpShared/HPRebar.McpBridge.Core`)
- `RequestDispatcher(executor, settings, hostVersion, hostName = "Revit")` so khớp `JsonRpcMethods.Suffix(method)`; thông điệp dùng `hostName`.
- `IRevitExecutor` → `IBridgeExecutor` (Revit bridge + fake implement tên mới).
- `ScriptGuard.Check(code, GuardProfile? = null)` với `GuardProfile.Revit` = hành vi hiện tại; `ScriptAnalyzer.Analyze(code, AnalyzerProfile? = null)`.
- `ScriptUnits` (sealed, `Label`, `MmPerUnit`, `ToDrawing(mm)`, `ToMm(du)`; Revit chưa dùng).
- `BridgeSettingsStore` → instance `new BridgeSettingsStore(vendorFolder, productFolder)` hoặc `static Configure(...)`; Revit `("HPRebar","McpBridge")` → path **y hệt** hôm nay.
- `Host/McpBridgeHost` (nhận `pipeName`, `hostVersion`), `ViewModel/{IMcpBridgeRunner, McpBridgeStatusViewModel}` chuyển vào Core; Core thêm `CommunityToolkit.Mvvm` 8.4.0 (ViewModel không dùng WPF — kiểm `using` trước khi move; nếu có `Dispatcher` → giữ VM ở bridge).
Functional — `HPRebar/`
- `HPRebar.slnx`: 3 project path `../McpShared/…`; `HPRebar.Mcp.Server.Tests` giữ (Revit-specific); `Server.Core.Tests` **không** đưa vào `HPRebar.slnx` (chạy từ `McpShared/`) — `TestProjectModule` của pipeline Revit vì thế chỉ chạy test Revit; ghi rõ trong CLAUDE.md. `[unverified]` `.slnx` chấp nhận `..\` — kiểm bước 1; fallback xem ADR-06 §4.
- `build/Modules/ResolveConfigurationsModule.cs`: `Solutions.HPRebar.FullName` thay `FindFile` (1 dòng + bỏ nhánh `.sln`).
- `HPRebar.Mcp.Server/`: còn `Program.cs` (`McpServerHost.CreateBuilder(args, new RevitHostProfile()).Build().RunAsync()` + CLI trả mã), `appsettings.json` (giữ), `Hosts/Revit/{RevitHostProfile,ExecuteRevitCodeTool,RevitContextTool,RevitDocumentResources,RevitScriptPrompts}.cs` (git mv, thân gọi service, **tên tool + description Revit giữ nguyên chữ**), `Registry/SeedLibrary/**` (＝), csproj bỏ package đã chuyển, thêm `ProjectReference ../../McpShared/HPRebar.Mcp.Server.Core`.
- `HPRebar.Mcp.Server.Tests`: còn `SeedLibraryTests` (Revit), `RevitHostProfileTests` (mới, nhỏ), test pipe cho 2 tool Revit (tách từ `PipeRoundTripTests` nếu có case đặc thù Revit); csproj ref `Server.Core` + `McpBridge.Core` + exe Revit.
- `HPRebar.McpBridge`: chỉ sửa `using`/namespace (Host/VM), constructor `McpBridgeHost(..., PipeNaming.For(int.Parse(version)), version)`, `BridgeSettingsStore("HPRebar","McpBridge")`, `IBridgeExecutor`. Build `Debug.R26 -p:DeployAddin=false`; **không** deploy trong phase này.
Functional — `HPAutoCad/` (scaffold)
- `HPAutoCad/HPAutoCad.slnx` (Debug/Release, chưa có project hoặc chỉ folder), `global.json` copy, `README.md` (mục đích, lệnh build/test/publish dự kiến, `.mcp.json` snippet). Project thật: phase 1 (bridge) và phase 3 (server).
Non-functional
- 0 thay đổi hành vi Revit: descriptions Revit giữ chữ; `tools/list` = 34 và JSON schema từng tool **giống hệt** (so sánh output `mcp_call.py tools/list` trước/sau — snapshot lưu `reports/phase-00-tools-list-before.json` / `-after.json`).
- `NoHostLeakTests` (McpShared): assembly `Contracts`/`Core`/`Server.Core` không reference assembly tên `RevitAPI*`, `AcDbMgd`, `AcMgd`, `AcCoreMgd`.

## Architecture
Xem [architecture.md §1, §5, §6](architecture.md) và [ADR-06 §Decision](adr/adr-06-one-mcp-one-folder.md).

## Related code files
- **Tái dùng nguyên (git mv, không sửa nội dung):** Contracts trừ 3 file additive; Core `Pipe/{PipeListener,NdjsonPipeWriter}`, `Scripting/{ScriptCompiler,ScriptCache,ScriptArgs,TypeInspector}`, `Model/{BridgeSettings,BridgeStatus,AuditLogger}`; Server `Services/{RevitBridgeClient,IRevitBridgeClient,NdjsonPipeTransport,ResultFormatter,BridgeExceptions,RegistryStartup}`, `Registry/{ToolLibraryStore,ToolRegistryDb,StabilityScorer,ToolManager,ToolValidator,ToolLifecycleService,DynamicToolRegistrar,RegistryCli}`, `Registry/Model/*`, `Tools/Registry/*`, `Tools/{InspectTypeTool,CancelExecutionTool}`, `Prompts/ToolifyPrompts`, `Resources/ToolRegistryResources`; tests `PipeRoundTripTests`, `BridgeUnavailableTests`, `ScriptCompilerTests`, `ScriptArgsAndAnalyzerTests`, `ScriptGuardTests`, `Registry/*`, `Fakes/FakeRevitExecutor`.
- **Tách ra chung / sửa tối thiểu:** `Program.cs` → `Bootstrap/McpServerHost.cs` + `Program.cs` mỏng; `ExecuteRevitCodeTool` → `Services/ExecuteCodeService` + tool mỏng; `RevitContextTool` + `RevitDocumentResources` → `Services/ContextService` + 2 file mỏng; `Models/BridgeOptions`; `Registry/{RegistryOptions,SeedInstaller,ToolRecord}` (root/product, hostAssembly, Host); Core `RequestDispatcher`, `IRevitExecutor`→`IBridgeExecutor`, `ScriptGuard`, `ScriptAnalyzer`, `BridgeSettingsStore`; `HPRebar.McpBridge/{Service/McpBridgeHost.cs, ViewModel/*}` → Core; `HPRebar.McpBridge/{Application.cs, McpBridgeExternalEventHandler.cs, McpBridgeCommand.cs, View/*.xaml.cs}` chỉ `using`; `HPRebar.slnx`; `build/Modules/ResolveConfigurationsModule.cs`; 3 csproj (Server, Tests, McpBridge) ref path mới.
- **Viết mới:** `McpShared/{McpShared.slnx, global.json, README.md}`, `HPRebar.Mcp.Server.Core/{HPRebar.Mcp.Server.Core.csproj, Bootstrap/McpServerHost.cs, Hosts/IHostProfile.cs, Services/ExecuteCodeService.cs, Services/ContextService.cs}`, Core `Scripting/{GuardProfile,AnalyzerProfile,ScriptUnits}.cs`, Contracts `HostScriptContracts.cs`, `HPRebar/HPRebar.Mcp.Server/Hosts/Revit/RevitHostProfile.cs`, tests `McpShared/…Core.Tests/{TestHostProfile.cs, DispatcherPrefixTests.cs, ScriptUnitsTests.cs, NoHostLeakTests.cs, ExecuteCodeServiceTests.cs}`, `HPRebar/HPRebar.Mcp.Server.Tests/RevitHostProfileTests.cs`, `HPAutoCad/{HPAutoCad.slnx, global.json, README.md}`.

## Implementation steps
1. **Snapshot trước:** publish exe Revit hiện tại → `mcp_call.py tools/list` → `reports/phase-00-tools-list-before.json`; `dotnet test HPRebar/HPRebar.Mcp.Server.Tests` = 159 pass (ghi số).
2. `mkdir McpShared`; `git mv HPRebar/HPRebar.Mcp.Contracts McpShared/`, `git mv HPRebar/HPRebar.McpBridge.Core McpShared/`; `McpShared.slnx` + `global.json`; sửa `HPRebar.slnx` path; sửa `ProjectReference` trong `HPRebar.Mcp.Server.csproj`, `HPRebar.Mcp.Server.Tests.csproj`, `HPRebar.McpBridge.csproj` (`..\..\McpShared\…`). Build `HPRebar.slnx -c Debug.R26 -p:DeployAddin=false` xanh → **kiểm `.slnx` `..\`** [unverified] tại đây.
3. `ResolveConfigurationsModule` → `Solutions.HPRebar`; `dotnet run` trong `HPRebar/build` (Compile) vẫn chạy (không cần pack).
4. Contracts additive (`PipeNaming.For(host,ver)`, `JsonRpcMethods` autocad + `Suffix`, `ContextResult` fields, `HostScriptContracts`).
5. Core: `IBridgeExecutor`, dispatcher suffix + hostName, `GuardProfile`/`AnalyzerProfile` (default Revit), `ScriptUnits`, `BridgeSettingsStore(vendor, product)`, move `McpBridgeHost` + ViewModel (+ Mvvm package). Build Core + `HPRebar.McpBridge` xanh.
6. Tạo `McpShared/HPRebar.Mcp.Server.Core` csproj; **git mv** từng file engine từ exe Revit (commit riêng "refactor(mcp): extract HPRebar.Mcp.Server.Core into McpShared"); `Bootstrap/McpServerHost`, `IHostProfile`, `ExecuteCodeService`, `ContextService`, `BridgeOptions.HostVersion`, `RegistryOptions` root theo product, `SeedInstaller(hostAssembly)`, `ToolRecord.Host`. Exe Revit: `Program.cs` mỏng, `Hosts/Revit/*` (git mv 4 file + `RevitHostProfile`).
7. Tests: git mv test engine → `McpShared/HPRebar.Mcp.Server.Core.Tests` (+ `TestHostProfile`, seed giả nhúng cho `ToolRegistryTests`/`SeedInstaller` tests); test mới (`DispatcherPrefixTests`: `revit.ping` **và** `autocad.ping` qua pipe thật; `ScriptUnitsTests`; `NoHostLeakTests`; `ExecuteCodeServiceTests`); `HPRebar.Mcp.Server.Tests` còn Revit-specific + `RevitHostProfileTests`. Chạy **cả hai** suite: tổng ≥ 159 + mới, 0 fail.
8. **Snapshot sau:** publish exe Revit → `tools/list` → `reports/phase-00-tools-list-after.json`; diff = rỗng (trừ thứ tự); `get_revit_context` + `execute_revit_code` dryRun với Revit 2026 + bridge **cũ** (không redeploy) OK.
9. Scaffold `HPAutoCad/` (`HPAutoCad.slnx`, `global.json`, `README.md`).
10. `CLAUDE.md`: § Repository Layout thêm `McpShared/` (thư viện chung của hai MCP, không phải deliverable riêng) và `HPAutoCad/`; § HPRebar Current State + § MCP Bridge sửa lệnh test (`dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests` + `dotnet test HPRebar/HPRebar.Mcp.Server.Tests`), ghi `ResolveConfigurationsModule` pin solution; regen `AGENTS.md` bằng engine. (Bước này chạm file ngoài plan → thực hiện khi `/bs:cook`, không phải bây giờ.)

## Todo
- [ ] 1 snapshot before · [ ] 2 git mv Contracts/Core + slnx/csproj · [ ] 3 ResolveConfigurations · [ ] 4 Contracts additive · [ ] 5 Core neutral · [ ] 6 Server.Core extract + Program mỏng + Hosts/Revit · [ ] 7 tests split + mới · [ ] 8 snapshot after + smoke Revit · [ ] 9 scaffold HPAutoCad · [ ] 10 CLAUDE.md/AGENTS.md

## Success criteria
- `dotnet build McpShared/McpShared.slnx` xanh; `dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false` xanh (kể cả `Debug.R23` để chắc project `Debug`/`Release` map đúng); `cd HPRebar/build && dotnet run` (Compile) xanh.
- `dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests` + `dotnet test HPRebar/HPRebar.Mcp.Server.Tests`: tổng pass ≥ 159 + mới, 0 fail; `NoHostLeakTests` pass.
- `reports/phase-00-tools-list-before.json` ≡ `-after.json` (34 tool, schema giống); `get_revit_context` OK với bridge Revit chưa redeploy.
- `git status`/`git diff --stat`: không chạm `HPRebar/HPRebar/`, `HPRebar/HPRebar.Core*`, `docs/` (trừ bước 10 CLAUDE.md/AGENTS.md); `git log --follow` thấy rename cho file đã mv.
- `HPAutoCad/` tồn tại với `HPAutoCad.slnx` build xanh (rỗng).

## Risks
| Risk | Mitigation |
|---|---|
| `.slnx` không chấp nhận `..\McpShared\…` | ADR-06 §4: kiểm bước 2; fallback `.sln` cổ điển cho `HPRebar` (Nice3point không phụ thuộc slnx) — `[unverified]`, xác suất thấp |
| SDK 2.2.0 thiếu overload `WithToolsFromAssembly(Assembly)` / `WithPrompts`/`WithResources` theo assembly | fallback `McpServerTool.Create(MethodInfo/Delegate)` + `ToolCollection` như `DynamicToolRegistrar`; verify ở bước 6 |
| Move `McpBridgeStatusViewModel` kéo WPF vào Core | kiểm `using`; nếu cần `Dispatcher` → giữ VM ở bridge, chỉ move `IMcpBridgeRunner` + `McpBridgeHost` |
| Test engine phụ thuộc seed Revit thật (`SeedInstaller`, `ToolRegistryTests`) | seed giả (2–3 tool) nhúng trong test assembly; test seed thật ở lại `HPRebar/` |
| Pipeline Revit `TestProjectModule` không còn chạy test engine | ghi CLAUDE.md; CI/tay chạy thêm `dotnet test McpShared/…`; tuỳ chọn thêm `Server.Core.Tests` vào `HPRebar.slnx` với path `../McpShared/…` nếu muốn một lệnh |
| Sourcy sinh lại `Projects.*` khi mất Contracts/Core trong `HPRebar/` | không module nào ref chúng (verified); build `build/` xanh là gate |

## Security
Không đổi (ADR-04 Revit). Exe Revit vẫn chỉ nói chuyện với pipe `hprebar-mcp-r2026` `CurrentUserOnly`; registry root không đổi.

## Next steps
Phase 1 (bridge AutoCAD, cần Contracts/Core ở `McpShared/` — bước 2 xong là đủ) và phase 3 (server AutoCAD, cần `Server.Core` — bước 6) nối tiếp; phase 1 có thể bắt đầu ngay sau bước 2.
