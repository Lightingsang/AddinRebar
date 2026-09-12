---
title: "Phase 1 — Scaffold plugin AutoCAD 2026: Loader + AssemblyLoadContext riêng, bundle, spike threading/Roslyn"
status: planned
priority: P1
effort: 8h
depends_on: []
created: 2026-09-13
---

# Phase 1 — AutoCAD plugin scaffold, ALC isolation, spike

## Context
- [ADR-05](adr/adr-05-autocad-plugin-packaging-alc-isolation-multi-version.md) (loader + ALC, bundle, pin `AutoCAD.NET [25.1.0]`) · [ADR-02](adr/adr-02-autocad-main-thread-marshalling.md) (câu hỏi spike) · [research §0.2–0.3](research/autocad-dotnet-api-2026-report.md) (máy dev .NET 8, Roslyn 4.10 trong thư mục AutoCAD, Immutable 10 vs 8).
- Mẫu Revit: `HPRebar.McpBridge/HPRebar.McpBridge.csproj` (IsRepackable=false, EnableDynamicLoading), `Service/ScriptingSelfCheck.cs`, `Application.cs` (CreateBridge: references + imports cho `ScriptCompiler`).
- Precedent trên máy: `%AppData%\Autodesk\ApplicationPlugins\AutoCadMcp.bundle\PackageContents.xml` (SchemaVersion 1.0, R25.0–R25.1) — copy cấu trúc.

## Overview
Tạo 2 project (`HPRebar.McpBridge.Autocad.Loader`, `HPRebar.McpBridge.Autocad`), bundle + target deploy, và **spike có gate** trả lời 4 câu hỏi kỹ thuật chưa verify được ngoài AutoCAD: (1) bundle nạp; (2) Roslyn 5.9 compile + run trong ALC riêng (self-check); (3) WPF modeless từ ALC riêng; (4) cách marshal từ thread ngoài (`Idle` subscribe từ thread ngoài; `ExecuteInApplicationContext` từ thread ngoài). Kết quả ghi `reports/phase-01-spike.md`; ADR-02/05 cập nhật `Status`.

## Key insights
- **Không có** ALC per-plugin trong AutoCAD; default ALC nhìn thấy `Microsoft.CodeAnalysis` 4.10 của AutoCAD và `System.Collections.Immutable` 8.0 của framework → Roslyn 5.9 phải sống trong ALC riêng do loader tạo.
- Loader **không được** reference compile-time assembly bridge; nạp bằng reflection để JIT không kéo bridge vào default ALC.
- `AutoCAD.NET` `25.1.*` sẽ resolve 25.1.1 (net10.0) → build net8 fail. Pin `[25.1.0]`.
- Script probe của self-check không cần document: `return Application.Version.ToString();` (`Core.Application.Version` verified) — chạy được ngay trong `Initialize()`; probe có `db` chạy trong spike command khi có drawing.

## Requirements
Functional
- `HPRebar.McpBridge.Autocad.Loader.csproj`: `net8.0-windows`, `AutoCAD.NET [25.1.0]` (`ExcludeAssets=runtime; PrivateAssets=all`), không WPF, không ref project bridge. Types: `BridgeLoaderApplication : IExtensionApplication`, `BridgeLoadContext : AssemblyLoadContext`, `BridgeLoaderCommands` (`HPMCPBRIDGE`, `HPMCPSTART`, `HPMCPSTOP`, `HPMCPSTATUS`, spike-only `HPMCPSPIKE`).
- `HPRebar.McpBridge.Autocad.csproj`: `net8.0-windows`, `UseWPF`, `EnableDynamicLoading=true`, refs Contracts + Core + `AutoCAD.NET [25.1.0]` (ExcludeAssets=runtime) + `CommunityToolkit.Mvvm` 8.4.0 + Serilog; `BridgeEntry.Start(Action<string> log) : BridgeHandle` (handle = record of delegates); `ScriptingSelfCheck`; placeholder `View/AutocadBridgeStatusView.xaml` (chỉ 1 TextBlock trong phase này).
- Bundle: `Bundle/PackageContents.xml` (ADR-05 §2) + MSBuild target `DeployBundle` → `%AppData%\Autodesk\ApplicationPlugins\HPRebar.McpBridge.Autocad.bundle\{PackageContents.xml, Contents\Loader.dll, Contents\Bridge\**}`; `-p:DeployBundle=false` tắt. `launchSettings.json` profile acad.exe.
- `.slnx`: 2 project trong `/Mcp/`, map R## → Debug/Release.
- Spike command `HPMCPSPIKE` (loader → bridge delegate) chạy tuần tự và ghi log `%LocalAppData%\HPRebar\McpBridge.Autocad\logs\`:
  1. ALC check: `AssemblyLoadContext.GetLoadContext(typeof(Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript).Assembly).Name` == `HPRebar.McpBridge.Autocad`; version `Microsoft.CodeAnalysis` = 5.9; `System.Collections.Immutable` = 10.x.
  2. Roslyn: `ScriptCompiler` với refs `AcMgd/AcCoreMgd/AcDbMgd` + Core; probe `return db.Filename + "|" + Application.Version;` với globals thật (`doc/db/ed` từ `MdiActiveDocument`) → log `MCP scripting self-check OK`.
  3. WPF: `Core.Application.ShowModelessWindow(new Window{...})` từ bridge (ALC riêng) → hiện được, đóng được.
  4. Threading: từ `Task.Run` (thread ngoài): (a) `Application.Idle += handler` rồi trong handler đọc `Application.IsQuiescent`, `doc.LockDocument()`, `StartTransaction` → tạo 1 `Line` → `Commit`; (b) `Application.DocumentManager.ExecuteInApplicationContext(cb, null)` từ thread ngoài → log thread id, có block caller không (đo `Stopwatch` hai phía), có chạy khi modal (`ShowModalDialog` thử nghiệm) không.
  5. Busy: gõ `LINE` dở rồi chạy spike (a) → `IsQuiescent == false` được ghi nhận, không crash.
Non-functional
- Build không có Revit config: `dotnet build HPRebar/HPRebar.McpBridge.Autocad -c Debug` và qua `HPRebar.slnx -c Debug.R26`.

## Architecture
Xem [architecture.md §5](architecture.md#5-layout-project-mới--sửa--giữ-) và [ADR-05 §1](adr/adr-05-autocad-plugin-packaging-alc-isolation-multi-version.md).

## Related code files
- **Tái dùng nguyên:** `HPRebar.McpBridge.Core` (`ScriptCompiler`, `ScriptCache`), `HPRebar.Mcp.Contracts`; `HPRebar.McpBridge/Service/ScriptingSelfCheck.cs` làm mẫu; `HPRebar.McpBridge/HPRebar.McpBridge.csproj` làm mẫu PackageReference/Serilog.
- **Tách ra chung:** không (phase 0 lo).
- **Viết mới:** `HPRebar.McpBridge.Autocad.Loader/{HPRebar.McpBridge.Autocad.Loader.csproj, BridgeLoaderApplication.cs, BridgeLoadContext.cs, BridgeLoaderCommands.cs}`, `HPRebar.McpBridge.Autocad/{HPRebar.McpBridge.Autocad.csproj, BridgeEntry.cs, BridgeHandle.cs, Service/ScriptingSelfCheck.cs, Service/SpikeRunner.cs (xoá cuối phase 2), View/AutocadBridgeStatusView.xaml(.cs), Bundle/PackageContents.xml, Properties/launchSettings.json}`, `.slnx` entries, `reports/phase-01-spike.md`.

## Implementation steps
1. Tạo 2 csproj bằng tay (không `dotnet new` template Revit); pin package; `Directory` layout như ADR-05. Build `-c Debug` → kiểm `bin/Debug/net8.0-windows/` có `HPRebar.McpBridge.Autocad.deps.json`, `Microsoft.CodeAnalysis*.dll` 5.9, `System.Collections.Immutable.dll` 10.x, **không** có `AcDbMgd.dll` (ExcludeAssets).
2. `BridgeLoadContext`: `AssemblyDependencyResolver`; `Load` trả `LoadFromAssemblyPath` khi resolver có path, else `null`. Log mọi lần `Load` ở Debug để đọc thứ tự nạp.
3. `BridgeLoaderApplication.Initialize`: đường dẫn `Contents\Bridge\HPRebar.McpBridge.Autocad.dll` từ `Assembly.GetExecutingAssembly().Location`; tạo ALC; reflection `BridgeEntry.Start`; lưu `BridgeHandle`; `Terminate` → `handle.Dispose()`.
4. `BridgeEntry.Start`: Serilog file log (folder `McpBridge.Autocad`), `ScriptingSelfCheck.Run(compiler)` với probe không cần document; trả handle.
5. Bundle + `DeployBundle` target + `launchSettings.json`; `.slnx`.
6. Đóng AutoCAD → build Debug (deploy) → mở AutoCAD 2026 → xem log `self-check OK` + ALC name; gõ `HPMCPSPIKE` → chạy 4–5 kịch bản spike, ghi log; ghi `reports/phase-01-spike.md` (bảng câu hỏi → kết quả → quyết định).
7. Cập nhật `Status`/Decision của ADR-02 (đường chính Idle hay ExecuteInApplicationContext) và ADR-05 (ALC OK / phương án B).

## Todo
- [ ] 1 csproj + build · [ ] 2 ALC · [ ] 3 loader/entry · [ ] 4 self-check · [ ] 5 bundle/deploy/slnx · [ ] 6 spike trong AutoCAD + report · [ ] 7 ADR update

## Success criteria
- `dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false -p:DeployBundle=false` xanh (cả Revit lẫn AutoCAD project).
- AutoCAD 2026 khởi động **không** dialog lỗi; log có `MCP scripting self-check OK` và `Roslyn load context = HPRebar.McpBridge.Autocad`.
- `HPMCPSPIKE` pass 1–3; mục 4 có kết quả rõ ràng cho cả (a) và (b) (kể cả "không gọi được từ thread ngoài" cũng là kết quả hợp lệ); mục 5 không crash.
- `reports/phase-01-spike.md` tồn tại; ADR-02/05 ghi quyết định cuối.
- `dotnet test HPRebar/HPRebar.Mcp.Server.Tests` không hồi quy (không có test mới ở phase này ngoài build).

## Risks
| Risk | Mitigation |
|---|---|
| Roslyn trong ALC riêng vẫn bind `Microsoft.CodeAnalysis` 4.10 của AutoCAD (đã nạp trước vào default) | Custom ALC `Load` **luôn** ưu tiên resolver path cho mọi assembly có trong deps.json → không hỏi default; log thứ tự nạp để chứng minh. Fail → phương án B (compile trong server, gửi IL) |
| `AssemblyDependencyResolver` trả path cho `AcDbMgd` (nếu deps.json liệt kê runtime asset) → 2 bản AutoCAD API → `InvalidCastException` | `ExcludeAssets=runtime` + kiểm `deps.json` không có `runtime` cho Autodesk packages; thêm allowlist "never load from plugin folder: Ac*.dll, Ad*.dll" trong `Load` |
| `Application.Idle` không subscribe được từ thread ngoài | subscribe vĩnh viễn ở `Start()` (ADR-02 fallback) |
| AutoCAD hỏi "unsigned" / SECURELOAD chặn | user *Always Load*; ghi vào report; nếu chặn cứng → thêm bundle path vào `TRUSTEDPATHS` (docs) |
| AutoCAD 2026 Update 1.2 (.NET 10) được cài giữa chừng | `acdbmgd.runtimeconfig.json` là gate đầu phase; nếu net10 → đổi TFM + `[25.1.1]` (ADR-05 §4) |

## Security
Spike command tạo 1 `Line` thật trong drawing → chỉ chạy trên drawing trống (`Drawing1.dwg`). Không mở pipe trong phase này.

## Next steps
Phase 2 dùng kết quả spike để chốt `MainThreadExecutor`; xoá `SpikeRunner` + `HPMCPSPIKE` cuối phase 2.
