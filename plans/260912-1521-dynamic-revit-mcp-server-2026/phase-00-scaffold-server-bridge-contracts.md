# Phase 00 — Scaffold: Server + Bridge + Contracts

## Context Links
- [architecture.md §6 layout, §7 lifecycle](architecture.md) · [ADR-01](adr/adr-01-two-process-topology.md)
- NotebookLM Q07 [16-18] (`Program.cs` stdio skeleton), Q08 [14-16] (stderr logging), Q09 [2-5] (csproj + packages) — `research/notebooklm-mcp-csharp-report.md`
- `CLAUDE.md` §"HPRebar — Build, Run, Debug" (configs `Debug.R##`, `-p:DeployAddin=false`, pipeline `ResolveConfigurationsModule`)
- Verified: `HPRebar/build/Modules/ResolveConfigurationsModule.cs:18-19` lọc `Release.R*`; `HPRebar/HPRebar.slnx:73-84` map `HPRebar.Core` → plain `Debug/Release`; `HPRebar/HPRebar/HPRebar.csproj:1-11` (SDK 6.2.3, flags); `HPRebar/HPRebar/HPRebar.addin:5-6` (GUID + `FullClassName`); `HPRebar/global.json:3` SDK 10.0.300
- Skill: `/bs:revit-addin` (template flags, `references/nice3point-toolkit.md`)

## Overview
- **Priority:** P1 · **Status:** built 2026-09-12 (implemented + build gates + Inspector smoke; chưa chạy trong Revit) · **Effort:** 4h
- Tạo 3 project theo ADR-01, đăng ký vào `HPRebar.slnx` bằng tay, `Program.cs`/`appsettings.json` tối thiểu, bridge scaffold từ template Nice3point, build gate xanh. Không có logic nghiệp vụ.

## Key Insights
- `global.json` đã pin SDK 10.0.300 → net10 console build được ngay; không đổi global.json.
- Server/Contracts chỉ có `Debug;Release` → map trong `.slnx` giống `HPRebar.Core` (mọi `*.R##|*` → `Debug`/`Release`). Pipeline `dotnet run` build solution với từng `Release.R*` → server build 5 lần, chấp nhận (KISS) thay vì tách pipeline.
- Bridge: template `revit-addin`, `--addinDiMode disabled` (HPRebar không có container — `Application.cs:75` dùng static `Log.Logger`), WPF on, Serilog on, manifest `application`.
- **Verified 2026-09-12** (SDK `samples/QuickstartWeatherServer/Program.cs`): sample chính thức dùng `Host.CreateApplicationBuilder(args)` + `builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace)` — khác sách (`CreateEmptyApplicationBuilder(null)` + appsettings). Chọn theo sample: config/appsettings tự nạp, chỉ cần ép stderr bằng code. Vẫn set `ContentRootPath = AppContext.BaseDirectory` vì host AI launch server với cwd bất kỳ (Q10 [96]).
- **Verified 2026-09-12** NuGet: `ModelContextProtocol` 2.2.0, `Microsoft.Extensions.Hosting` 10.0.12, `Microsoft.CodeAnalysis.CSharp.Scripting` 5.9.0. Attribute/builder API trong sample 2.x vẫn là `[McpServerToolType]`, `[McpServerTool]`, `[Description]`, `AddMcpServer().WithStdioServerTransport().WithTools<T>()`, `McpException` — khớp sách.
- Pure logic của bridge (`ScriptGuard`, `ScriptCompiler`, `ScriptCache`) tách vào `HPRebar.McpBridge.Core` (net8.0, chỉ Roslyn, **không** Revit refs) để xUnit test được — theo nguyên tắc CLAUDE.md "pure logic never touches Revit API".
- Bridge không dùng `Polyfill` (chỉ net8) — tránh CS0433 như ghi chú `.slnx:48-51`.
- Hai add-in cùng ghi Serilog file → thư mục log riêng `%LocalAppData%\HPRebar\McpBridge\logs\` để không khoá file lẫn nhau.
- Roslyn package vào csproj ngay phase này (phase 4 không sửa csproj → tránh đụng phase 3 sửa XAML links).

## Requirements
**Functional**
- `dotnet build HPRebar.slnx -c Debug.R26 -p:DeployAddin=false` xanh với 3 project mới.
- Server chạy `dotnet run` im lặng chờ stdin (Q10 [91-92]); Inspector connect được, `tools/list` rỗng.
- Bridge deploy `.addin` + DLL vào `%AppData%\Autodesk\Revit\Addins\2026\` khi build `Debug.R26` (Revit đóng).
**Non-functional**
- Không đụng `HPRebar/HPRebar/`, không đổi `HPRebar.addin` GUID cũ. Namespace file-scoped, explicit. `nullable enable` mọi project.

## Architecture
```
HPRebar/
├── HPRebar.Mcp.Contracts/   netstandard2.0  (System.Text.Json)          ← không ref Revit, không ref MCP SDK
├── HPRebar.Mcp.Server/      net10.0 Exe     (ModelContextProtocol, Hosting) → ref Contracts
└── HPRebar.McpBridge/       Nice3point SDK  (net8.0-windows7.0 R25/R26)  → ref Contracts, Roslyn Scripting
Reference graph: Server → Contracts ← Bridge.  Server ✗ Bridge.  Bridge ✗ HPRebar.csproj.
```
Data flow phase này: chưa có — chỉ host khởi động và bridge nạp ribbon.

## Related Code Files
**To create (proposed)**
- `HPRebar/HPRebar.Mcp.Contracts/HPRebar.Mcp.Contracts.csproj`, `Compat/IsExternalInit.cs` (record trên netstandard2.0)
- `HPRebar/HPRebar.Mcp.Server/HPRebar.Mcp.Server.csproj`, `Program.cs`, `appsettings.json`
- `HPRebar/HPRebar.McpBridge/HPRebar.McpBridge.csproj`, `HPRebar.McpBridge.addin`, `Application.cs`, `McpBridgeCommand.cs` (stub TaskDialog, phase 3 thay), `Resources/Icons/McpBridge16.png`, `McpBridge32.png`
**To modify (proposed)**
- `HPRebar/HPRebar.slnx` — thêm 3 project + BuildType mapping (XML tay, không IDE)
**To delete (proposed)**
- File template thừa trong `HPRebar.McpBridge/`: `Commands/StartupCommand.cs`, `Views/*`, `ViewModels/*`, `Resources/Themes/*` template (bridge dùng theme HPRebar — phase 3)

## Implementation Steps
1. **Package versions** — chốt: `ModelContextProtocol` 2.2.0, `Microsoft.Extensions.Hosting` 10.0.12, `Microsoft.CodeAnalysis.CSharp.Scripting` 5.9.0 (verified 2026-09-12 bằng `dotnet package search --exact-match`). Chạy lại lệnh này lúc implement; nếu có bản mới hơn → dùng bản mới, chỉ cần build + Inspector smoke pass. `System.Text.Json` cho Contracts: bản 10.x (netstandard2.0 compatible).
2. **Contracts:** `dotnet new classlib -n HPRebar.Mcp.Contracts -f netstandard2.0 -o HPRebar/HPRebar.Mcp.Contracts`. Sửa csproj: `LangVersion latest`, `Nullable enable`, `RootNamespace HPRebar.Mcp.Contracts`, `Configurations Debug;Release`, `PackageReference System.Text.Json`. Thêm `Compat/IsExternalInit.cs` (`namespace System.Runtime.CompilerServices; internal static class IsExternalInit;`). Xoá `Class1.cs`.
3. **Server:** `dotnet new console -n HPRebar.Mcp.Server -f net10.0 -o HPRebar/HPRebar.Mcp.Server`. csproj theo Q09 [2]: `OutputType Exe`, `ImplicitUsings enable`, `Nullable enable`, `RootNamespace HPRebar.Mcp.Server`, packages `ModelContextProtocol` + `Microsoft.Extensions.Hosting` (version bước 1), `<Content Include="appsettings.json" CopyToOutputDirectory="PreserveNewest"/>`, `ProjectReference ..\HPRebar.Mcp.Contracts`.
4. **`Program.cs`** — theo SDK sample chính thức (verified 2026-09-12), stderr ép bằng code như sách yêu cầu (Q07 [34-39], Q08 [14-18]):
   ```csharp
   var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
   {
       Args = args,
       ContentRootPath = AppContext.BaseDirectory, // host AI launches with arbitrary cwd
   });
   builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
   builder.Configuration.AddEnvironmentVariables("HPREBAR_MCP_");
   builder.Services.AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly()
       .WithResourcesFromAssembly().WithPromptsFromAssembly();
   await builder.Build().RunAsync();
   ```
   Không `Console.WriteLine` ở bất kỳ đâu. `appsettings.json` tự nạp từ ContentRoot.
4b. **Scripting lib:** `dotnet new classlib -n HPRebar.McpBridge.Scripting -f net8.0 -o HPRebar/HPRebar.McpBridge.Scripting`; packages `Microsoft.CodeAnalysis.CSharp.Scripting` 5.9.0; `Configurations Debug;Release` map trong `.slnx` như Contracts. Chứa `ScriptGuard`, `ScriptCompiler` (nhận `IEnumerable<Assembly>` references + `Type globalsType` từ bridge), `ScriptCache`. Không reference Revit.
5. **`appsettings.json`** (Q08 [14-16]): `Logging.LogLevel.Default=Information`, `Logging.Console.LogToStandardErrorThreshold=Trace`; section `Bridge` rỗng (phase 1-2 điền).
6. **Bridge scaffold:** trong `HPRebar/` chạy `mkdir HPRebar.McpBridge`, `cd HPRebar.McpBridge`, `dotnet new revit-addin --addinManifestType application --addinUiWpf true --addinDiMode disabled --addinLogging true`. Đối chiếu file sinh ra với `HPRebar.csproj`.
7. **Bridge csproj** (sửa tay): `UseWPF true`, `DeployAddin true`, `LaunchRevit true`, `IsRepackable false` (ADR-03: Roslyn không ILRepack), `EnableDynamicLoading true`, `RootNamespace HPRebar.McpBridge`. `<Configurations>` tạm `Debug.R26;Release.R26` (phase 1 chốt R25). Packages: `Nice3point.Revit.Toolkit/Extensions/Api.RevitAPI/Api.RevitAPIUI` `$(RevitVersion).*`, `CommunityToolkit.Mvvm 8.4.0`, `Serilog 4.4.0`, `Serilog.Sinks.File 7.0.0`, `Serilog.Sinks.Debug 3.0.0`, `Microsoft.CodeAnalysis.CSharp.Scripting` (bước 1), `JetBrains.Annotations` PrivateAssets=all. **Không** `ILRepack`, **không** `Polyfill`. `ProjectReference ..\HPRebar.Mcp.Contracts`.
8. **`.addin`:** GUID mới (PowerShell `[guid]::NewGuid()`), `<Name>HPRebar MCP Bridge</Name>`, `<Assembly>HPRebar.McpBridge\HPRebar.McpBridge.dll</Assembly>`, `<FullClassName>HPRebar.McpBridge.Application</FullClassName>`, `<VendorId>Development</VendorId>`, `ManifestSettings UseRevitContext=False`, `ContextName HPRebar.McpBridge` (ALC riêng — cần cho Roslyn isolation).
9. **`Application.cs`:** giữ block-scoped như template (CLAUDE.md không churn file template). `OnStartup`: `CreateLogger()` → `%LocalAppData%\HPRebar\McpBridge\logs\mcpbridge-.log`, rolling day, retain 7; `CreateRibbon()`: panel `"MCP"` tab `"HPRebar"`, `AddPushButton<McpBridgeCommand>("MCP Bridge")` icon `/HPRebar.McpBridge;component/Resources/Icons/McpBridge16.png|32.png`. `OnShutdown`: `Log.CloseAndFlush()`. Try/catch + `Log.Fatal` như `HPRebar/HPRebar/Application.cs:22-35`.
10. **`McpBridgeCommand.cs`** stub: `[Transaction(TransactionMode.Manual)] sealed class McpBridgeCommand : ExternalCommand` → `TaskDialog.Show("MCP Bridge", "Status window arrives in a later build.")`. Namespace `HPRebar.McpBridge`.
11. **Prune template** (mục To delete). Tạo thư mục `Model/ Service/ View/ ViewModel/` khi có file đầu tiên (phase 2-4), không `.gitkeep`.
12. **`.slnx`** sửa tay: Contracts + Server dưới root với 10 dòng `<BuildType Solution="X|*" Project="Debug|Release"/>` (copy khối `HPRebar.Core:73-84`); Bridge: `<BuildType Solution="Debug.R25|*" Project="Debug.R25"/>`, R26, `Release.R25/R26` tương tự, cộng `<Build Solution="Debug.R23|*" Project="false"/>` cho R23, R24, R27 × Debug/Release (6 dòng). `[VERIFY]` cú pháp `Build Solution=` bằng build `-c Debug.R23`.
13. **Sourcy:** build một lần rồi kiểm tra `HPRebar/build/obj/**/Sourcy*.g.cs` sinh `Projects.HPRebar_McpBridge`, `Projects.HPRebar_Mcp_Server` — cần cho phase 5 bundle. Không sửa `build/` ở phase này.
14. **Build gate:** `dotnet build HPRebar.slnx -c Debug.R26 -p:DeployAddin=false`; rồi `-c Debug.R23` (bridge phải bị skip); rồi `dotnet build HPRebar.slnx -c Release.R26 -p:DeployAddin=false`.
15. Inspector smoke: `npx @modelcontextprotocol/inspector dotnet run --project HPRebar/HPRebar.Mcp.Server/HPRebar.Mcp.Server.csproj` → Connect OK, Tools tab rỗng, không lỗi parse stdout.

## Todo List
- [x] Version chốt: `ModelContextProtocol`=2.2.0, `Microsoft.Extensions.Hosting`=10.0.12, `Microsoft.CodeAnalysis.CSharp.Scripting`=5.9.0, `System.Text.Json`=10.0.12
- [x] Contracts csproj (Polyfill PrivateAssets=all như HPRebar.Core thay vì `IsExternalInit` tay)
- [x] Server csproj + `Program.cs` + `appsettings.json`
- [x] Bridge viết tay theo HPRebar (không chạy template → không cần prune) + csproj + `.addin` GUID `A1F50652-…` + `Application.cs` (file-scoped) + `McpBridgeCommand.cs` stub + icons placeholder
- [x] `HPRebar.McpBridge.Core` csproj (rỗng, phase 4 điền)
- [x] Prune template files — không cần (viết tay)
- [x] `.slnx` 4 project trong folder `/Mcp/` + mappings; `<Build Solution=… Project="false"/>` verified bằng gate Debug.R23
- [x] Build gate Debug.R26 / Debug.R23 / Release.R26 + deploy R26 (4 Roslyn DLL) + Inspector CLI `tools/list` → `[]` + Sourcy sinh 4 constant

## Success Criteria
- `dotnet build HPRebar.slnx -c Debug.R26 -p:DeployAddin=false` → exit 0, 0 warning mới từ 3 project.
- `dotnet build HPRebar.slnx -c Debug.R23 -p:DeployAddin=false` → exit 0, log không có `HPRebar.McpBridge` (bị skip).
- `dotnet build HPRebar/HPRebar.McpBridge/HPRebar.McpBridge.csproj -c Debug.R26` (Revit đóng) → tồn tại `%AppData%\Autodesk\Revit\Addins\2026\HPRebar.McpBridge.addin` và thư mục `HPRebar.McpBridge\` chứa `Microsoft.CodeAnalysis*.dll`.
- Inspector connect → `initialize` OK trong History panel; `tools/list` trả `[]`.
- Grep `^namespace .*;$` trên mọi `.cs` mới trong `HPRebar.McpBridge/` (trừ `Application.cs` template) → 100% file-scoped.

## Risk Assessment
| Risk | L×I | Mitigation |
|---|---|---|
| `ModelContextProtocol` 2.2.0 có API lệch sách (1.3.0) ngoài phần đã verify (Resources/Prompts attributes, `IProgress`, notifications) | M×M | Compile lỗi = phát hiện ngay; đối chiếu `samples/` trong repo SDK; sample net8.0 xác nhận multi-target |
| Log lọt ra stdout → MCP client crash | L×H | `AddConsole(LogToStandardErrorThreshold = Trace)` theo sample; success criterion Inspector |
| Template Nice3point sinh file khác kỳ vọng (tên folder `Views/`, DI file) | M×L | Bước 11 prune, đối chiếu `HPRebar.csproj` |
| `.slnx` `Build Solution=` cú pháp sai → R23 build fail | M×M | Success criterion 2; fallback `<Build Project="false"/>` toàn cục + build bridge theo project path như `HPRebar.Tests` |
| Revit đang mở khoá DLL khi deploy | H×L | Luôn `-p:DeployAddin=false` cho build kiểm tra |

## Security Considerations
- Server chưa có tool → không có surface. Không commit `appsettings.*.json` chứa đường dẫn máy cá nhân.
- `.addin` GUID mới bắt buộc — trùng GUID với `HPRebar.addin` làm Revit từ chối nạp một trong hai.

## Next Steps
- Phase 1 chốt `<Configurations>` R25/R26 + pipe naming theo version.
- Ghi version package chốt vào `plan.md` Unresolved #4 khi đã verify.
