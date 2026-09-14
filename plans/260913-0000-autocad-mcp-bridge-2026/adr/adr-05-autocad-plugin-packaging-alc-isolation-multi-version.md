# ADR-05 — Packaging & deploy plugin AutoCAD: bundle autoloader, loader mỏng + AssemblyLoadContext riêng cho Roslyn, MVP chỉ 2026 (.NET 8)

**Ngày:** 2026-09-13 · **Revised 2026-09-14** (ADR-06: project sống trong `HPAutoCad/`, solution riêng, pipe `hpautocad-mcp-2026`, settings `%AppData%\HPAutoCad\McpBridge\`) · **Status:** **Accepted (2026-09-14)** — §1 (loader + ALC riêng) và §2 (manifest) verified by `reports/phase-01-spike.md`: Roslyn 5.9 + Immutable 10 nạp vào context `HPAutoCad.McpBridge`, AcDbMgd ở Default, self-check 1,6 s, bundle autoload không dialog · **Owner:** HPRebar
**Kế thừa:** [ADR-06 one MCP one folder](adr-06-one-mcp-one-folder.md) · [Revit ADR-01 (bridge = add-in riêng, Roslyn không ILRepack)](../../260912-1521-dynamic-revit-mcp-server-2026/adr/adr-01-two-process-topology.md) · Research: [autocad-dotnet-api-2026-report.md §0.1–0.3, §3, §12](../research/autocad-dotnet-api-2026-report.md) · [reference report Addendum](../research/autocad-bridge-reference-report.md)

## Context (facts đã kiểm 2026-09-13)

- Máy dev: AutoCAD 2026 **R25.1.74**, `.NET 8` (`acdbmgd.runtimeconfig.json`), chưa cài Update 1.2 (.NET 10). `AutoCAD.NET` **25.1.0** = 2026/net8.0 (deps `AutoCAD.NET.Core` [25.1.0] → `AutoCAD.NET.Model` [25.1.0]); **25.1.1 = 2026.1.2/net10.0**; **26.0.0 = 2027/net10.0**. Wildcard `25.1.*` sẽ kéo 25.1.1 (net10) → **phải pin `[25.1.0]`**.
- AutoCAD **không** có ALC per-plugin; mọi plugin vào default ALC. Trong thư mục AutoCAD có sẵn `Microsoft.CodeAnalysis(.CSharp).dll` **4.10**, `Serilog` 4.0.0, `Newtonsoft.Json` 13.0.3. Roslyn 5.9.0 (bản netstandard2.0 khi chạy net8) cần `System.Collections.Immutable` **10.0.1** / `System.Reflection.Metadata` 10.0.1, còn shared framework 8.0 chỉ có 8.0.x → default ALC = `FileLoadException`. Revit tránh được nhờ `<ContextName>` trong `.addin`; AutoCAD không có.
- Bundle autoloader đã chạy trên máy này với plugin cũ của user (`AutoCadMcp.bundle`, `SchemaVersion 1.0`, `SeriesMin R25.0 / SeriesMax R25.1`, `LoadOnAutoCADStartup`). Autodesk blog 2025: **phải** khai `RuntimeRequirements` + `SeriesMax` cho 2025+; AutoCAD 2026 = **R25.1**.
- Repo hiện có: `build/Modules/CreateBundleModule.cs` gom `HPRebar` + `HPRebar.McpBridge` thành bundle Revit; `PublishServerModule` publish exe server. Nice3point SDK (`Debug.R##`) chỉ cho Revit; AutoCAD bridge dùng `Microsoft.NET.Sdk` thường.

## Decision

### 1. Hai assembly, một bundle

```
%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.McpBridge.bundle\
├── PackageContents.xml
└── Contents\
    ├── HPAutoCad.McpBridge.Loader.dll        ← AutoCAD NETLOAD cái này (default ALC); chỉ ref AcMgd/AcCoreMgd/AcDbMgd
    └── Bridge\
        ├── HPAutoCad.McpBridge.dll            ← bridge thật (WPF, Contracts, Core, Roslyn) — nạp vào BridgeLoadContext
        ├── HPAutoCad.McpBridge.deps.json      ← AssemblyDependencyResolver đọc file này
        ├── HPRebar.McpBridge.Core.dll · HPRebar.Mcp.Contracts.dll
        ├── Microsoft.CodeAnalysis*.dll (5.9.0) · System.Collections.Immutable.dll (10.x) · System.Reflection.Metadata.dll (10.x)
        ├── System.Text.Json.dll (10.x) · CommunityToolkit.Mvvm.dll · Serilog*.dll
        └── … (mọi copy-local của project bridge)
```

- **Loader** (`HPAutoCad.McpBridge.Loader`, `net8.0-windows`, không WPF): `[assembly: ExtensionApplication(typeof(BridgeLoaderApplication))]`, `[assembly: CommandClass(typeof(BridgeLoaderCommands))]`. `Initialize()`: tạo `BridgeLoadContext : AssemblyLoadContext("HPAutoCad.McpBridge", isCollectible: false)` với `AssemblyDependencyResolver(<Contents>\Bridge\HPAutoCad.McpBridge.dll)`; `Load(AssemblyName)` → `resolver.ResolveAssemblyToPath(name)` → có path thì `LoadFromAssemblyPath`, **không có thì `null`** (rơi về default ALC: shared framework, WPF, **AutoCAD API** — nhờ `ExcludeAssets="runtime"` nên các assembly Autodesk không nằm trong runtime assets của `deps.json`). `LoadUnmanagedDll` tương tự cho native (không có trong MVP). Sau đó **reflection**: `alc.LoadFromAssemblyPath(main).GetType("HPAutoCad.McpBridge.BridgeEntry")!.GetMethod("Start")!.Invoke(null, [logSink])` — **không** reference compile-time tới assembly chính (nếu có, JIT của loader sẽ nạp nó vào default ALC trước, phá cách ly). Entry trả về một `BridgeHandle` gồm các `Action`/`Func` (`ShowWindow`, `Start`, `Stop`, `Dispose`, `Status`) — delegate type từ CoreLib nên chia sẻ được qua ranh giới ALC.
- **Commands** (trong loader, vì AutoCAD chỉ quét `CommandClass` của assembly nó NETLOAD): `HPMCPBRIDGE` (mở status window), `HPMCPSTART`, `HPMCPSTOP`, `HPMCPSTATUS` (in ra command line). `CommandFlags.Modal` mặc định — chúng chỉ gọi delegate, không đụng document.
- **Bridge thật** (`HPAutoCad.McpBridge`, `net8.0-windows`, `UseWPF=true`, `EnableDynamicLoading=true` để sinh `deps.json` + copy-local đầy đủ, `IsRepackable` không áp dụng): ref `Contracts`, `McpBridge.Core`, `AutoCAD.NET` **[25.1.0]** `ExcludeAssets="runtime"` `PrivateAssets="all"`, `CommunityToolkit.Mvvm` 8.4.0, Serilog.
- **Canary bắt buộc ở `Start()`**: `ScriptingSelfCheck` compile + run `return db.Filename;`-style probe với `ScriptCompiler` (Roslyn 5.9 trong ALC riêng) và log `MCP scripting self-check OK` (mirror Revit `ScriptingSelfCheck.cs`); thêm log `AssemblyLoadContext.GetLoadContext(typeof(CSharpScript).Assembly).Name` để chứng minh Roslyn nằm trong `HPAutoCad.McpBridge` chứ không phải default (phòng bind nhầm vào Roslyn 4.10 của AutoCAD).

### 2. `PackageContents.xml` (MVP)

```xml
<ApplicationPackage SchemaVersion="1.0" AutodeskProduct="AutoCAD" ProductType="Application"
                    Name="HPAutoCad MCP Bridge" AppVersion="0.1.0"
                    Description="MCP bridge: AI-generated C# runs inside AutoCAD (opt-in)"
                    ProductCode="{<GUID mới, cố định>}">
  <CompanyDetails Name="HPRebar" />
  <Components Description="AutoCAD 2026 (.NET 8)">
    <RuntimeRequirements OS="Win64" Platform="AutoCAD" SeriesMin="R25.1" SeriesMax="R25.1" />
    <ComponentEntry AppName="HPAutoCad.McpBridge" Version="0.1.0"
                    ModuleName="./Contents/HPAutoCad.McpBridge.Loader.dll"
                    AppType=".NET" LoadOnAutoCADStartup="True" />
  </Components>
</ApplicationPackage>
```
- `Platform="AutoCAD"` (không `AutoCAD*`): tránh nạp vào Civil 3D 2026 / Advance Steel 2026 cùng R25.1 trên máy dev khi chưa verify (pipe cùng tên sẽ tranh nhau — `PipeListener` fail-fast). Mở `AutoCAD*` là một dòng sửa sau khi verify.
- `LoadOnAutoCADStartup="True"`: listener **không** tự start (setting `AutoStartListener` persisted, mặc định OFF như Revit); startup chỉ tạo ALC + self-check + đăng ký command.
- Dialog "unsigned executable"/`SECURELOAD`: bundle trong `ApplicationPlugins` nằm trong trusted locations mặc định `[unverified — plugin cũ của user nạp được từ đó, không thấy dialog được ghi nhận]`; nếu AutoCAD hỏi, user chọn *Always Load* (giống Revit).

### 3. Build / deploy dev loop
- csproj bridge: property `AutocadVersion` (mặc định `2026`) → `AutocadSeries` `R25.1`, `AutocadPackageVersion` `[25.1.0]`, `PipeName` không cần (bridge đọc `Application.Version.Major/Minor` runtime → `PipeNaming.For("autocad", 2026)`; map series→năm bằng bảng nhỏ trong bridge, fallback `AutocadVersion` build-time constant).
- Target `DeployBundle` (AfterBuild, `Condition="'$(Configuration)' == 'Debug' And '$(DeployBundle)' != 'false'"`) copy `PackageContents.xml` + `Loader.dll` + `Bridge\**` sang `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.McpBridge.bundle\`. AutoCAD đang mở khoá DLL → `-p:DeployBundle=false` (đối xứng `-p:DeployAddin=false` của Revit).
- `Properties/launchSettings.json`: profile `AutoCAD 2026` → `executablePath: C:\Program Files\Autodesk\AutoCAD 2026\acad.exe`, `commandLineArgs: /nologo` để F5 attach.
- **Solution riêng (ADR-06, revised 2026-09-14):** `HPAutoCad/HPAutoCad.slnx` (configurations `Debug`/`Release` thường, không hậu tố `R##`) chứa `HPAutoCad.Mcp.Server`, `HPAutoCad.McpBridge.Loader`, `HPAutoCad.McpBridge`, `HPAutoCad.Mcp.Server.Tests` và reference `../McpShared/HPRebar.Mcp.Contracts`, `../McpShared/HPRebar.McpBridge.Core`, `../McpShared/HPRebar.Mcp.Server.Core`. `HPAutoCad/global.json` copy từ `HPRebar/global.json`. `dotnet build HPAutoCad/HPAutoCad.slnx -c Debug` deploy bundle; **không** đụng `HPRebar.slnx`. `HPAutoCad.Mcp.Server.Tests` không ref project bridge — compile-check seed chỉ cần DLL trong NuGet cache (`~/.nuget/packages/autocad.net.model/25.1.0/lib/net8.0/AcDbMgd.dll` …); project bridge reference package để bảo đảm cache có sẵn.
- Pipeline: MVP **không** có `HPAutoCad/build/` (ModularPipelines) — `dotnet build/publish` + target `DeployBundle`; `HPAutoCad/.run/` (Rider) optional. Pipeline pack (`CreateBundle` cho AutoCAD + `PublishServer`) là bước sau MVP, copy cấu trúc `HPRebar/build/` với Sourcy root riêng (`HPAutoCad/.sourcyroot`).

### 4. Multi-version
| Mục tiêu | Trạng thái trong plan | Cách mở |
|---|---|---|
| **AutoCAD 2026 base (.NET 8, R25.1)** | **MVP** — build + verify live | — |
| AutoCAD 2025 (.NET 8, R25.0) | Không build trong MVP | `AutocadVersion=2025` → `AutoCAD.NET [25.0.1]`, `SeriesMin R25.0`; API managed 2025→2026 được cho là source-compatible `[unverified]`; pipe `hpautocad-mcp-2025`; thêm configuration `Debug.A25`/`Release.A25` nếu cần build song song (property `AutocadVersion` đọc từ hậu tố như Nice3point làm với `R##`) |
| AutoCAD 2026 Update 1.2 / 2027 (.NET 10) | Ngoài scope | TFM `net10.0-windows`, `AutoCAD.NET [25.1.1]` / `[26.0.0]`; Roslyn 5.9 có `lib/net10.0` (deps khớp framework 10) nhưng vẫn cần ALC riêng vì AutoCAD ship Roslyn 4.10; `.NET 8` EOL 11/2026 → đây là bước kế tiếp hợp lý sau MVP |

## Alternatives rejected

- **Một assembly, default ALC, "pin version thấp"** (đề xuất của researcher): phải hạ Roslyn xuống 4.8 (khác Revit bridge, hai cache/guard hành vi khác nhau) **và** vẫn đụng Roslyn 4.10 của AutoCAD theo tên. Loại.
- **ILRepack/ILMerge Roslyn vào bridge**: Revit ADR-01 đã loại vì 15 MB + khó chẩn đoán; thêm nữa `System.Collections.Immutable` 10 vẫn bị TPA 8.0 đè nếu không đổi tên namespace. Loại.
- **Compile trong server, gửi IL qua pipe** (Revit ADR-03 alt 1): server phải giữ AutoCAD API refs; đổi Contracts; vẫn cần chạy IL trong AutoCAD với deps. Giữ làm phương án B nếu ALC riêng thất bại ở phase 1.
- **Registry demand-load (`HKCU\...\R25.1\ACAD-9101:409\Applications`)** thay bundle: hoạt động nhưng phụ thuộc mã ACAD-9101 (khác Civil 3D 9100), khó share; bundle là chuẩn Autodesk. Loại cho MVP, ghi làm fallback nếu autoloader không nạp.

## Consequences

- Phase 1 là **spike có gate**: (1) bundle nạp; (2) `HPMCPBRIDGE` mở WPF window từ ALC riêng (`Core.Application.ShowModelessWindow(Window)`); (3) self-check Roslyn OK trong ALC riêng; (4) `Idle` subscribe từ thread ngoài. Fail (3) → kích hoạt phương án B; fail (4) → subscribe vĩnh viễn ở `Initialize()` (ADR-02).
- Hai dự án mới trong `HPAutoCad/` + `HPAutoCad.slnx` + `global.json`; không `Directory.Build.props` (repo không có) — csproj tự chứa.
- `Contracts` net standard 2.0 + `System.Text.Json` 10 vào ALC riêng — OK (đã chạy trong ALC Revit).
- Log/settings tách khỏi Revit: `%AppData%\HPAutoCad\McpBridge\settings.json`, audit `…\audit\`, log `%LocalAppData%\HPAutoCad\McpBridge\logs\` → `BridgeSettingsStore` (Core) nhận `(vendorFolder, productFolder)` thay vì hằng `("HPRebar", "McpBridge")` (phase 0); AutoCAD truyền `("HPAutoCad", "McpBridge")`.
