---
title: "Phase 1 — Scaffold plugin AutoCAD 2026: Loader + AssemblyLoadContext riêng, bundle, spike threading/Roslyn"
status: planned
priority: P1
effort: 8h
depends_on: [phase-00 bước 1–2]
created: 2026-09-13
revised: 2026-09-14 (ADR-06 — project trong `HPAutoCad/`, solution riêng)
---

# Phase 1 — AutoCAD plugin scaffold, ALC isolation, spike

> Revised 2026-09-14: mọi project của phase này nằm trong folder top-level **`HPAutoCad/`**, build bằng `HPAutoCad/HPAutoCad.slnx`; tham chiếu mã chung qua `../McpShared/…` (ADR-06). Không có gì trong `HPRebar/` bị chạm.

## Context
- [ADR-05](adr/adr-05-autocad-plugin-packaging-alc-isolation-multi-version.md) (loader + ALC, bundle, pin `AutoCAD.NET [25.1.0]`) · [ADR-02](adr/adr-02-autocad-main-thread-marshalling.md) (câu hỏi spike) · [research §0.2–0.3](research/autocad-dotnet-api-2026-report.md) (máy dev .NET 8, Roslyn 4.10 trong thư mục AutoCAD, Immutable 10 vs 8).
- Mẫu Revit: `HPRebar.McpBridge/HPRebar.McpBridge.csproj` (IsRepackable=false, EnableDynamicLoading), `Service/ScriptingSelfCheck.cs`, `Application.cs` (CreateBridge: references + imports cho `ScriptCompiler`).
- Precedent trên máy: `%AppData%\Autodesk\ApplicationPlugins\AutoCadMcp.bundle\PackageContents.xml` (SchemaVersion 1.0, R25.0–R25.1) — copy cấu trúc.

## Overview
Tạo 2 project (`HPAutoCad.McpBridge.Loader`, `HPAutoCad.McpBridge`), bundle + target deploy, và **spike có gate** trả lời 4 câu hỏi kỹ thuật chưa verify được ngoài AutoCAD: (1) bundle nạp; (2) Roslyn 5.9 compile + run trong ALC riêng (self-check); (3) WPF modeless từ ALC riêng; (4) cách marshal từ thread ngoài (`Idle` subscribe từ thread ngoài; `ExecuteInApplicationContext` từ thread ngoài). Kết quả ghi `reports/phase-01-spike.md`; ADR-02/05 cập nhật `Status`.

## Key insights
- **Không có** ALC per-plugin trong AutoCAD; default ALC nhìn thấy `Microsoft.CodeAnalysis` 4.10 của AutoCAD và `System.Collections.Immutable` 8.0 của framework → Roslyn 5.9 phải sống trong ALC riêng do loader tạo.
- Loader **không được** reference compile-time assembly bridge; nạp bằng reflection để JIT không kéo bridge vào default ALC.
- `AutoCAD.NET` `25.1.*` sẽ resolve 25.1.1 (net10.0) → build net8 fail. Pin `[25.1.0]`.
- Script probe của self-check không cần document: `return Application.Version.ToString();` (`Core.Application.Version` verified) — chạy được ngay trong `Initialize()`; probe có `db` chạy trong spike command khi có drawing.

## Requirements
Functional
- `HPAutoCad.McpBridge.Loader.csproj`: `net8.0-windows`, `AutoCAD.NET [25.1.0]` (`ExcludeAssets=runtime; PrivateAssets=all`), không WPF, không ref project bridge. Types: `BridgeLoaderApplication : IExtensionApplication`, `BridgeLoadContext : AssemblyLoadContext`, `BridgeLoaderCommands` (`HPMCPBRIDGE`, `HPMCPSTART`, `HPMCPSTOP`, `HPMCPSTATUS`, spike-only `HPMCPSPIKE`).
- `HPAutoCad.McpBridge.csproj`: `net8.0-windows`, `UseWPF`, `EnableDynamicLoading=true`, refs Contracts + Core + `AutoCAD.NET [25.1.0]` (ExcludeAssets=runtime) + `CommunityToolkit.Mvvm` 8.4.0 + Serilog; `BridgeEntry.Start(Action<string> log) : BridgeHandle` (handle = record of delegates); `ScriptingSelfCheck`; placeholder `View/AutocadBridgeStatusView.xaml` (chỉ 1 TextBlock trong phase này).
- Bundle: `Bundle/PackageContents.xml` (ADR-05 §2) + MSBuild target `DeployBundle` → `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.McpBridge.bundle\{PackageContents.xml, Contents\Loader.dll, Contents\Bridge\**}`; `-p:DeployBundle=false` tắt. `launchSettings.json` profile acad.exe.
- `HPAutoCad/HPAutoCad.slnx` (mới, Debug/Release) + `HPAutoCad/global.json` (copy từ `HPRebar/global.json`); 2 project bridge + (từ phase 3) server/tests; `ProjectReference` `../McpShared/HPRebar.Mcp.Contracts`, `../McpShared/HPRebar.McpBridge.Core`. Phase 1 chờ phase 0 bước 1–2 (git mv Contracts/Core sang `McpShared/`) xong — không reference tạm sang `../HPRebar/` dù chỉ để spike (tránh cross-wire).
- Spike command `HPMCPSPIKE` (loader → bridge delegate) chạy tuần tự và ghi log `%LocalAppData%\HPAutoCad\McpBridge\logs\`:
  1. ALC check: `AssemblyLoadContext.GetLoadContext(typeof(Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript).Assembly).Name` == `HPAutoCad.McpBridge`; version `Microsoft.CodeAnalysis` = 5.9; `System.Collections.Immutable` = 10.x.
  2. Roslyn: `ScriptCompiler` với refs `AcMgd/AcCoreMgd/AcDbMgd` + Core; probe `return db.Filename + "|" + Application.Version;` với globals thật (`doc/db/ed` từ `MdiActiveDocument`) → log `MCP scripting self-check OK`.
  3. WPF: `Core.Application.ShowModelessWindow(new Window{...})` từ bridge (ALC riêng) → hiện được, đóng được.
  4. Threading: từ `Task.Run` (thread ngoài): (a) `Application.Idle += handler` rồi trong handler đọc `Application.IsQuiescent`, `doc.LockDocument()`, `StartTransaction` → tạo 1 `Line` → `Commit`; (b) `Application.DocumentManager.ExecuteInApplicationContext(cb, null)` từ thread ngoài → log thread id, có block caller không (đo `Stopwatch` hai phía), có chạy khi modal (`ShowModalDialog` thử nghiệm) không.
  5. Busy: gõ `LINE` dở rồi chạy spike (a) → `IsQuiescent == false` được ghi nhận, không crash.
Non-functional
- Build không có Revit config: `dotnet build HPAutoCad/HPAutoCad.slnx -c Debug` (deploy bundle) / `-p:DeployBundle=false`.

## Architecture
Xem [architecture.md §5](architecture.md#5-layout-project-mới--sửa--giữ-) và [ADR-05 §1](adr/adr-05-autocad-plugin-packaging-alc-isolation-multi-version.md).

## Related code files
- **Tái dùng nguyên:** `HPRebar.McpBridge.Core` (`ScriptCompiler`, `ScriptCache`), `HPRebar.Mcp.Contracts`; `HPRebar.McpBridge/Service/ScriptingSelfCheck.cs` làm mẫu; `HPRebar.McpBridge/HPRebar.McpBridge.csproj` làm mẫu PackageReference/Serilog.
- **Tách ra chung:** không (phase 0 lo — Contracts/Core đã ở `McpShared/`).
- **Viết mới:** `HPAutoCad.McpBridge.Loader/{HPAutoCad.McpBridge.Loader.csproj, BridgeLoaderApplication.cs, BridgeLoadContext.cs, BridgeLoaderCommands.cs}`, `HPAutoCad.McpBridge/{HPAutoCad.McpBridge.csproj, BridgeEntry.cs, BridgeHandle.cs, Service/ScriptingSelfCheck.cs, Service/SpikeRunner.cs (xoá cuối phase 2), View/AutocadBridgeStatusView.xaml(.cs), Bundle/PackageContents.xml, Properties/launchSettings.json}`, `HPAutoCad/HPAutoCad.slnx`, `HPAutoCad/global.json`, `HPAutoCad/README.md` (cách build/deploy), `reports/phase-01-spike.md`.

## Implementation steps
1. Tạo 2 csproj bằng tay (không `dotnet new` template Revit); pin package; `Directory` layout như ADR-05. Build `-c Debug` → kiểm `bin/Debug/net8.0-windows/` có `HPAutoCad.McpBridge.deps.json`, `Microsoft.CodeAnalysis*.dll` 5.9, `System.Collections.Immutable.dll` 10.x, **không** có `AcDbMgd.dll` (ExcludeAssets).
2. `BridgeLoadContext`: `AssemblyDependencyResolver`; `Load` trả `LoadFromAssemblyPath` khi resolver có path, else `null`. Log mọi lần `Load` ở Debug để đọc thứ tự nạp.
3. `BridgeLoaderApplication.Initialize`: đường dẫn `Contents\Bridge\HPAutoCad.McpBridge.dll` từ `Assembly.GetExecutingAssembly().Location`; tạo ALC; reflection `BridgeEntry.Start`; lưu `BridgeHandle`; `Terminate` → `handle.Dispose()`.
4. `BridgeEntry.Start`: Serilog file log (`%LocalAppData%\HPAutoCad\McpBridge\logs\`), `ScriptingSelfCheck.Run(compiler)` với probe không cần document; trả handle.
5. Bundle + `DeployBundle` target + `launchSettings.json`; `HPAutoCad.slnx` + `global.json`.
6. Đóng AutoCAD → build Debug (deploy) → mở AutoCAD 2026 → xem log `self-check OK` + ALC name; gõ `HPMCPSPIKE` → chạy 4–5 kịch bản spike, ghi log; ghi `reports/phase-01-spike.md` (bảng câu hỏi → kết quả → quyết định).
7. Cập nhật `Status`/Decision của ADR-02 (đường chính Idle hay ExecuteInApplicationContext) và ADR-05 (ALC OK / phương án B).

## Todo
- [ ] 1 csproj + build · [ ] 2 ALC · [ ] 3 loader/entry · [ ] 4 self-check · [ ] 5 bundle/deploy/slnx · [ ] 6 spike trong AutoCAD + report · [ ] 7 ADR update

## Success criteria
- `dotnet build HPAutoCad/HPAutoCad.slnx -c Debug -p:DeployBundle=false` xanh; `dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false` vẫn xanh và **không** build gì của `HPAutoCad/`.
- AutoCAD 2026 khởi động **không** dialog lỗi; log có `MCP scripting self-check OK` và `Roslyn load context = HPAutoCad.McpBridge`.
- `HPMCPSPIKE` pass 1–3; mục 4 có kết quả rõ ràng cho cả (a) và (b) (kể cả "không gọi được từ thread ngoài" cũng là kết quả hợp lệ); mục 5 không crash.
- `reports/phase-01-spike.md` tồn tại; ADR-02/05 ghi quyết định cuối.
- `dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests` + `dotnet test HPRebar/HPRebar.Mcp.Server.Tests` không hồi quy (không có test mới ở phase này ngoài build).

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
