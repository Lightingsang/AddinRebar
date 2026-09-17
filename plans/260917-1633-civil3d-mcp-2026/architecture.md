# HPCivil3d MCP 2026 — Architecture

Ngày 2026-09-17 · **planned** (chưa đổi source) · nguồn: [evidence E1–E17](research/evidence-on-machine-2026-09-17.md), [reflection addendum](research/reflection-addendum-verified-signatures.md), ADR-01…06. Mọi khẳng định API không kèm nguồn = `[chưa xác minh]`. Viết theo **ADR-01 = A** (copy + mirror test); B chỉ đổi §3 (xem ADR-01).

## 1. Sáu folder top-level, chiều phụ thuộc một chiều (không đổi quy tắc)

```
HPRebar/    (Revit add-in + Revit MCP)        ──┐
HPAutoCad/  (AutoCAD MCP + AEC engine)        ──┼──▶ McpShared/  (HPRebar.Mcp.Contracts netstandard2.0;net48 · HPRebar.McpBridge.Core net8.0;net48 · HPRebar.Mcp.Server.Core net10)
HPNavis/    (Navisworks MCP, net48)           ──┤        ▲ không bao giờ tham chiếu Autodesk.*/ETABSv1; host vào qua IHostProfile / IBridgeExecutor / GuardProfile / AnalyzerProfile
HPEtabs/    (ETABS MCP, app WPF + COM)        ──┤
HPCivil3d/  (Civil 3D MCP — plan này)         ──┘        HPCivil3d/ ⇏ HPAutoCad/ (copy có mirror test, không ProjectReference — ADR-01 A)
```
`HPCivil3d/` chỉ `ProjectReference ..\McpShared\*` (mirror `HPAutoCad/HPAutoCad.slnx` `/Shared/`). Nếu user chọn ADR-01 B: thêm `AcadShared/` giữa `HPAutoCad/`+`HPCivil3d/` và `McpShared/` — CLAUDE.md sửa chiều phụ thuộc (ngoại lệ: `AcadShared/` tham chiếu `Autodesk.AutoCAD.*`).

## 2. Hai tiến trình, cùng hình dạng AutoCAD MCP — khác host token và một global

```
Claude Code ──stdio──▶ HPCivil3d.Mcp.Server.exe (net10, single-file, không ref AutoCAD/Civil API)
                        McpServerHost.RunAsync(args, Civil3dHostProfile)  ·  4 core (execute_civil3d_code / get_civil3d_context / inspect_type / cancel_execution) · 8 registry · 12 seed · civil3d:// · prompts
                        registry %AppData%\HPCivil3d\McpServer\{registry.db, tools-library}
            │ named pipe hpcivil3d-mcp-2026, JSON-RPC 2.0 NDJSON, methods civil3d.*  (dispatch theo suffix — RequestDispatcher không sửa)
            ▼
acad.exe /product C3D  ──autoloader (Platform="Civil3D", R25.1)──▶ %AppData%\Autodesk\ApplicationPlugins\HPCivil3d.McpBridge.bundle\
   Contents\HPCivil3d.McpBridge.Loader.dll   (default ALC; IExtensionApplication; lệnh HPC3DMCPBRIDGE; Ribbon HPCivil3d ▸ MCP ▸ MCP Bridge; BridgeLoadContext refuses Ac*/Ad*/Aec*/Autodesk.*)
   Contents\Bridge\HPCivil3d.McpBridge.dll   (ALC riêng "HPCivil3d.McpBridge": Roslyn 5.9 + Immutable 10 + Contracts + Core + Serilog + Mvvm; WPF status window)
        Civil3dBridgeEntry.Start → BridgeSettingsStore("HPCivil3d","McpBridge") → ScriptCompiler(refs: acdbmgd/acmgd/accoremgd + C3D\AeccDbMgd + ACA\AecBaseMgd + C3D\AeccPressurePipesMgd + Core; imports Civil3dImports; globals Civil3dScriptGlobals)
        → MainThreadExecutor (Application.Idle + PostMessage(WM_NULL), IsQuiescent, busy 8 s → -32002, no drawing → -32003)
        → Civil3dScriptRunner (LockDocument "HPMCP" → outer → inner=tr → script(doc,db,ed,app,tr,units,civil,ct,log,progress,args) → inner.Commit → Changed (HANDSEED/ObjectOpenedForModify) → outer.Commit | Abort(dryRun/none/error/timeout))
        → Civil3dResultSerializer → ExecuteResult → audit %AppData%\HPCivil3d\McpBridge\audit\ · log %LocalAppData%\HPCivil3d\McpBridge\logs\ · ScriptingSelfCheck "MCP scripting self-check OK" + "civil product Civil3D"
```
AutoCAD 2026 (`/product ACAD`) và Civil 3D 2026 mở cùng lúc = hai bundle (Platform khác), hai pipe, hai server, hai registry root — không tài nguyên chung (ADR-02).

## 3. Bố cục `HPCivil3d/` (NEW toàn bộ)

```
HPCivil3d/
├── HPCivil3d.slnx · global.json (copy HPAutoCad: sdk 10.0.300, MTP) · Directory.Build.props (Civil3dInstallDir: -p/env HPCIVIL3D_C3D_DIR → HKLM\SOFTWARE\Autodesk\AutoCAD\R25.1\ACAD-9100:409\Location + "C3D\" → %ProgramW6432%\Autodesk\AutoCAD 2026\C3D\; Civil3dApiAvailable = Exists(AeccDbMgd.dll)) · README.md · .gitignore
├── HPCivil3d.McpBridge.Loader/      net8.0-windows, UseWPF (AdWindows), AutoCAD.NET [25.1.0] ExcludeAssets=runtime — BridgeLoaderApplication, BridgeLoadContext (+"Aec"), BridgeLoaderCommands (HPC3DMCPBRIDGE), BridgeActions, LoaderLog, Ribbon/{McpRibbonTab (HPCIVIL3D_MCP_*), RibbonIcons, RibbonCommandHandler}, Bundle/PackageContents.xml (Platform="Civil3D"), DeployBundle target
├── HPCivil3d.McpBridge/             net8.0-windows, UseWPF, EnableDynamicLoading, AutoCAD.NET [25.1.0] ExcludeAssets=runtime, <Reference Include="AeccDbMgd|AecBaseMgd|AeccPressurePipesMgd" HintPath=$(Civil3dInstallDir)… Private=false>, <Error Condition="!Civil3dApiAvailable">
│     Civil3dBridgeEntry.cs · MainThreadExecutor.cs · Model/Civil3dScriptGlobals.cs (+civil) · Service/{Civil3dScriptRunner, Civil3dResultSerializer, Civil3dContextReader (+Civil3dInfo counts), DatabaseChangeCounter, ScriptingSelfCheck (probe civil), Civil3dThemeSwitcher, AutocadVersionMap, Civil3dUnits (DrawingUnits → ScriptUnits)} · View/Civil3dBridgeStatusView.xaml(.cs) · Resources/Themes/Civil3dTheme{,Light}.xaml
├── HPCivil3d.McpBridge.Tests/       net8.0-windows, xunit v3 — MirrorTests (file ⇔ HPAutoCad twin sau thay token), Civil3dUnitsTests, guard/analyzer Civil3d qua Core (không cần Civil 3D cài trừ khi ref AeccDbMgd — giữ test project KHÔNG ref Civil API)
├── HPCivil3d.Mcp.Server/            net10 stdio — Program.cs (1 dòng), Hosts/Civil3dHostProfile.cs, Tools/{ExecuteCivil3dCodeTool, Civil3dContextTool}, Prompts/Civil3dScriptPrompts, Resources/Civil3dDocumentResources, Registry/SeedLibrary/{Document,Alignment,Profile,Surface,Corridor,Pipe,Parcel,Point}/<name>/{tool.json,code.cs,examples.json} + _seeds.json, appsettings.json
├── HPCivil3d.Mcp.Server.Tests/      net10 — HostProfileTests, Civil3dToolsOverPipeTests (FakeRevitExecutor linked từ McpShared tests), SeedLibraryStructureTests, SeedLibraryCompileTests (Assert.SkipWhen không có Civil 3D)
├── tools/harness/                   run-live-verify.ps1 · live-verify.py · harness-common.ps1 · run-bridge-unattended.ps1 · pipe-scenarios.py · run-server-smoke.ps1 · run-ribbon-check.ps1 · bridge.scr · README.md  (import ../../../McpShared/tools/{mcp-session.py, mcp-call.py, harness_common.py})
└── output/                          HPCivil3d.Mcp.Server (single-file) · live-verify/{registry, scene/*.dwg copy từ C3D\Help\Civil Tutorials\Drawings}
```

## 4. Vòng đời `execute_civil3d_code` (= AutoCAD + `civil` + guard Civil)

1. Server: clamp timeout ≤ 120 (`Civil3dHostProfile.MaxTimeoutSeconds` = mặc định engine) → `civil3d.execute` qua `BridgeClient` (hint không kết nối: `BridgeNotConnectedHint` nêu "Civil 3D 2026 (acad.exe /product C3D)… plain AutoCAD does not load this bundle").
2. Dispatcher: opt-in OFF → `-32001`; busy → `-32002` (`RequestDispatcher` nguyên).
3. Executor (pipe thread): `ScriptGuard.Check(code, GuardProfile.Civil3d)` (AutoCAD deny + `Rebuild*`, data-shortcut, survey, file-taking, `AeccUiMgd`, `AECC.Interop`) → `ScriptCompiler` (Civil3dImports, refs §2, globals `Civil3dScriptGlobals`) → enqueue `MainThreadQueue`.
4. Idle tick: không `MdiActiveDocument` → `-32003`; `IsQuiescent` false quá 8 s → `-32002`; `LockDocument(ProtectedAutoWrite, "HPMCP")`; `civil = CivilApplication.ActiveDocument` (try/catch → null, U1); `units` từ `civil.Settings…DrawingUnits` (Meters 1000 / Feet 304.8), so `db.Insunits` → log mismatch.
5. `outer = doc.TransactionManager.StartTransaction()`, `inner = tr`; script; serialize `Value` **trước** `inner.Commit()`; `Changed`; `outer.Commit()` hoặc `Abort()` (dryRun/none+modify/exception/timeout/cancel); `ed.WriteMessage("[MCP] <label>: …; undo with U")`; `FlushGraphics/UpdateScreen`.
6. Audit + `LastRunInfo`; `ExecuteResult` → server → registry `runs`.

## 5. Bảng mã lỗi / diagnostic (delta so AutoCAD in đậm)

| Code | Khi | Message | Nguồn |
|---|---|---|---|
| -32001 | opt-in OFF | text engine cũ (add-in; `executionDisabledMessage` null) | `RequestDispatcher.cs:132–134` |
| -32002 | busy > 8 s / lệnh đang chạy / modal | `Busy("Civil 3D")` | `BridgeRequestException.cs:21` |
| -32003 | không drawing | `NoActiveDocument("Civil 3D", "drawing")` | `:24` |
| bridge vắng | pipe không có | **`BridgeNotConnectedHint`** (ADR-06) | `RevitBridgeClient.cs:139` |
| timeout | không reply | text engine cũ (rollback qua Abort) | `:85` |
| diag `GUARD` | guard | "… is not allowed in Civil 3D scripts." (**+ Rebuild/DataShortcuts/Export…**) | `ScriptGuard.cs:60` |
| result `isError` | `Autodesk.Civil.*Exception` | **`"{TypeName}: {Message}"`** (SafeText) | serializer (phase 2) |
| result `isError` | `none` + `Changed ≠ 0` | "script modified the drawing in transaction=none" | runner (copy) |

## 6. Khác gì bốn host cũ

| | Revit 2026 | AutoCAD 2026 | Navisworks 2026 | ETABS 22 | **Civil 3D 2026** |
|---|---|---|---|---|---|
| Bridge chạy ở đâu | add-in trong Revit.exe | bundle trong acad.exe (ALC) | plugin Roamer (net48) | app WPF riêng (COM) | **bundle trong acad.exe `/product C3D` (ALC) — copy AutoCAD** |
| Lọc host | `.addin` | `Platform="AutoCAD"` | thư mục Plugins | — | **`Platform="Civil3D"`** (Autodesk DevGuide) |
| Marshal | `ExternalEvent` | `Application.Idle` | Idle + quiescence | STA worker | **= AutoCAD** |
| Rollback | TransactionGroup | outer `Abort()` | commit-then-Rollback | snapshot `.EDB` | **= AutoCAD**; rebuild bị chặn MVP |
| Opt-in thứ hai | — | — | heavy | destructive | **—** (rebuild = follow-up nếu S-05 sạch) |
| Đơn vị API | feet | INSUNITS | `Document.Units` | ép kN_mm_C | **Civil `DrawingUnits` (Meters/Feet)**; mm cho hình học phẳng, drawing unit cho station/elevation |
| Globals | doc/uidoc/app/uiapp | doc/db/ed/app/tr/units | doc/app/units | sapModel/etabs/units | **AutoCAD + `civil`** |
| Ghi được (MVP) | geometry + params | geometry + tables + AEC | metadata review | W/D theo tier | **COGO points, alignment từ polyline** (+ mọi Civil API không bị deny qua ad-hoc code) |
| API ref | NuGet | NuGet | thư mục cài | thư mục cài (CLSID) | **NuGet AutoCAD.NET + thư mục cài `C3D\`/`ACA\` (registry `ACAD-9100:409\Location`)** — bridge + compile-check cần Civil 3D; server không |
| Deploy | `.addin` | bundle | Plugins | exe | **bundle** `HPCivil3d.McpBridge.bundle` |
| Tool | 34 | 62 | 24 | 24 | **24** |

## 7. Gate xuyên plan
- **Phase 0:** `tools/list` 4 exe (Revit 33, AutoCAD 62, Navis 24, ETABS 24 — đo lại lúc chạy) byte-identical trước/sau (`plans/260915-0824-navisworks-mcp-2026/reports/snapshot-tools-list.ps1` + `plans/260916-2152-etabs-mcp-2026/reports/regression-tools-list-phase-04.py` mở rộng 4 host); 7 suite test cũ nguyên số.
- **Phase 1 (spike, gate):** S-01…S-09 + W1/W2 (phase-01) — kết quả quyết ADR-02/03/04 Accepted/revised; fail S-01/S-02 → ADR-02 §4.
- **Phase 4:** harness `run-live-verify.ps1` ≥ 60 scenario pass ×3 trên registry cách ly; isolation hai chiều; hồi quy 4 host. **Chỉ sau phase 4 mới nói Verified.**
