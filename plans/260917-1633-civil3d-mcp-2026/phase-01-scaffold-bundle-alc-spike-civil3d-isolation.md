---
phase: 1
title: "Scaffold HPCivil3d/ (2 project bridge + props), bundle Platform=Civil3D, spike có gate: nạp chỉ trong Civil 3D, civil resolve, đọc alignment, ghi + dryRun rollback, coexist với AutoCAD 2026"
status: pending
priority: P1
effort: "8h"
dependencies: [0]
---

# Phase 1: Scaffold + spike S-01…S-09, W1–W2 (gate cho ADR-02/03/04)

## Context Links
- [ADR-01 (A: copy + mirror test)](adr/adr-01-code-sharing-with-hpautocad-copy-vs-acadshared.md) · [ADR-02](adr/adr-02-bundle-platform-civil3d-pipe-isolation.md) · [ADR-03](adr/adr-03-globals-units-context.md) · [ADR-04 §3 S-05](adr/adr-04-transactions-rebuilds-guard-civil3d.md) · [ADR-06 §1 token](adr/adr-06-server-profile-client-wiring-ribbon-identity.md)
- Evidence: [E1–E6, E13–E15](research/evidence-on-machine-2026-09-17.md) · [addendum §12 U1–U11](research/reflection-addendum-verified-signatures.md) · [researcher-02 §"Items the Phase-1 Spike Must Prove"](research/researcher-02-civil3d-autoloader-launch-rebuild-facts.md)
- Mẫu mã (tồn tại): `HPAutoCad/HPAutoCad.McpBridge.Loader/{BridgeLoaderApplication.cs, BridgeLoadContext.cs, BridgeLoaderCommands.cs, BridgeActions.cs, LoaderLog.cs, Ribbon/*, Bundle/PackageContents.xml, HPAutoCad.McpBridge.Loader.csproj}` · `HPAutoCad/HPAutoCad.McpBridge/{BridgeEntry.cs, MainThreadExecutor.cs, Model/AutocadScriptGlobals.cs, Service/*, View/*, Resources/Themes/*, HPAutoCad.McpBridge.csproj}` · `HPNavis/Directory.Build.props`, `HPEtabs/Directory.Build.props` (khuôn dò install dir) · `HPEtabs/HPEtabs.McpBridge/HPEtabs.McpBridge.csproj` (`<Reference … Private=false>` + `<Error Condition>`) · harness `HPAutoCad/tools/harness/{harness-common.ps1 (Start-AcadWithBridge :64–79, Answer-SecureLoad :27, Set-OptIn :47), run-live-verify.ps1:116–132, bridge.scr}` · AutoCAD phase-1 spike report `plans/260913-0000-autocad-mcp-bridge-2026/reports/phase-01-spike.md` (khuôn báo cáo).

## Overview
Tạo `HPCivil3d/` với 2 project bridge (copy HPAutoCad theo ADR-01 A, đổi token theo ADR-06), `Directory.Build.props` dò `C3D\`, bundle `Platform="Civil3D"`, và chạy **spike có gate** trả lời mọi `[chưa xác minh]` của ADR-02/03/04 với Civil 3D 2026 thật. Spike đi qua đúng đường bridge (pipe → Idle → runner) bằng một **server tạm** = `HPCivil3d.Mcp.Server` tối giản (Program + profile + 4 core tool, chưa seed) để dùng `McpShared/tools/mcp-session.py` thay lệnh gõ tay — như ETABS phase 1 (`live-verify.py --phase spike`). Kết quả → `reports/phase-01-spike.md`; ADR-02/03/04 cập nhật Status.

**Ràng buộc user:** phase này là nơi **duy nhất** được start/drive/close Civil 3D và AutoCAD (harness unattended; user không cần thao tác trừ SECURELOAD lần đầu nếu UIA không bấm được).

## Key insights
- Civil 3D = acad.exe + `/product C3D` (E13): mọi kết luận ALC/Idle/transaction của AutoCAD spike 2026-09-14 **giả định** đúng — nhưng Roslyn 5.9 trong ALC riêng khi Civil nạp thêm ~30 managed DLL (`Aecc*Mgd`, `AecBaseMgd`) chưa đo (S-03b).
- Bundle Civil phải build được **chỉ** khi có Civil 3D cài (`Civil3dApiAvailable`), như HPNavis/HPEtabs; server + server tests build mọi máy.
- Hai plugin MCP cũ của user nạp vào Civil 3D (E14) — spike ghi nhận dialog/log lạ, không đụng.

## Requirements
Functional
- `HPCivil3d/Directory.Build.props` (NEW): `Civil3dInstallDir` = `-p:Civil3dInstallDir` | env `HPCIVIL3D_C3D_DIR` | `$([MSBuild]::GetRegistryValueFromView('HKEY_LOCAL_MACHINE\SOFTWARE\Autodesk\AutoCAD\R25.1\ACAD-9100:409', 'Location', null, RegistryView.Registry64))` + `C3D\` | `$(ProgramW6432)\Autodesk\AutoCAD 2026\C3D\`; `AecBaseDir` = `..\ACA\` từ đó; `Civil3dApiAvailable = Exists('$(Civil3dInstallDir)AeccDbMgd.dll') And Exists('$(AecBaseDir)AecBaseMgd.dll')`.
- `HPCivil3d.McpBridge.Loader.csproj` (NEW): copy AutoCAD, `RootNamespace HPCivil3d.McpBridge.Loader`, `Version 0.1.0`, `BundleDir …\HPCivil3d.McpBridge.bundle\`, `BridgeOutDir ..\HPCivil3d.McpBridge\bin\…`, `DeployBundle` target y hệt (Error text nêu Civil). Types: `BridgeLoaderApplication` (ALC name `HPCivil3d.McpBridge`, đường `Contents\Bridge\HPCivil3d.McpBridge.dll`, reflection `HPCivil3d.McpBridge.Civil3dBridgeEntry.Start`), `BridgeLoadContext` (`HostAssemblyPrefixes = ["Ac", "Ad", "Aec", "Autodesk."]`), `BridgeLoaderCommands` (`HPC3DMCPBRIDGE`; spike-only `HPC3DMCPSPIKE*` chỉ khi env `HPCIVIL3D_MCP_SPIKE=1`, xoá cuối phase 2), `BridgeActions` (prefix `[HPCivil3d MCP]`), `LoaderLog` (`%LocalAppData%\HPCivil3d\McpBridge\logs\loader.log`), `Ribbon/*` (ids `HPCIVIL3D_MCP_TAB/PANEL/BRIDGE`, title `HPCivil3d`, icon vector copy).
- `Bundle/PackageContents.xml` (NEW): ADR-02 §1 nguyên văn, `ProductCode` GUID mới (`[guid]::NewGuid()` một lần, ghi vào file, không đổi nữa).
- `HPCivil3d.McpBridge.csproj` (NEW): copy AutoCAD **bỏ** `ProjectReference ../HPAutoCad.Aec`; thêm `<Reference Include="AeccDbMgd"><HintPath>$(Civil3dInstallDir)AeccDbMgd.dll</HintPath><Private>false</Private></Reference>` + `AecBaseMgd` (`$(AecBaseDir)`) + `AeccPressurePipesMgd`; `<Error Condition="'$(Civil3dApiAvailable)' != 'true'" Text="Civil 3D 2026 not found — set HPCIVIL3D_C3D_DIR or -p:Civil3dInstallDir"/>` trước build; `ProjectReference ..\..\McpShared\{Contracts, McpBridge.Core}`.
- Bridge (phase 1 = tối thiểu để spike; phase 2 hoàn thiện): `Civil3dBridgeEntry` (copy `BridgeEntry`: `VendorFolder "HPCivil3d"`, `HostName "Civil 3D"`, `PipeNaming.For(Civil3dHost, year)`, `JsonRpcMethods.Civil3dPrefix`, `HostScriptContracts.Civil3dImports`, `typeof(Civil3dScriptGlobals)`, `CompilerReferences` += `typeof(CivilDocument).Assembly`, `typeof(Autodesk.Aec.DatabaseServices.Entity).Assembly`, `typeof(PressurePipeNetwork).Assembly`); `MainThreadExecutor` (copy, `HostName`); `Model/Civil3dScriptGlobals` (+ `civil`); `Service/Civil3dScriptRunner` (copy `AutocadScriptRunner` + `civil = SafeActiveDocument()` + `units = Civil3dUnits.For(civil, db)`); `Service/Civil3dUnits` (NEW ~30 dòng: `DrawingUnits` → `ScriptUnits`; mismatch với `AutocadInsunits.For(db.Insunits)` → note); `Service/ScriptingSelfCheck` (probe `return "civil " + CivilApplication.ActiveProduct + " " + Autodesk.AutoCAD.ApplicationServices.Core.Application.Version;` — không cần document); `Service/{Civil3dResultSerializer, DatabaseChangeCounter, AutocadVersionMap, Civil3dThemeSwitcher, Civil3dContextReader (phase 1: copy AutoCAD, `Civil3d` slot điền `Product/IsCivilDocument/DrawingUnit` — đếm ở phase 2)}`; `View/Civil3dBridgeStatusView.xaml(.cs)` + `Resources/Themes/Civil3dTheme{,Light}.xaml` (copy, đổi key).
- Server tạm `HPCivil3d.Mcp.Server` (NEW, phase 3 hoàn thiện): `Program.cs` = `McpServerHost.RunAsync(args, Civil3dHostProfile.Instance)`; `Hosts/Civil3dHostProfile.cs` theo ADR-06 §1; `Tools/{ExecuteCivil3dCodeTool, Civil3dContextTool}` (copy AutoCAD, đổi tên/mô tả tạm); chưa prompt/resource/seed. Build mọi máy (không ref API).
- `HPCivil3d.slnx` (NEW: Debug/Release; 3 project + `/Shared/` 3 ProjectReference McpShared) + `global.json` (copy) + `README.md` (build/deploy/spike) + `.gitignore` (`output/`, `bin/`, `obj/`).
- **Spike harness** (NEW `HPCivil3d/tools/harness/`): `harness-common.ps1` (copy AutoCAD; `Start-AcadWithBridge` nhận `-Product C3D|ACAD|ADVS` và `-Profile`; pipe `hpcivil3d-mcp-2026`; window title pattern `HPCivil3d`), `spike.scr` (`HPC3DMCPBRIDGE`/`HPC3DMCPSTART` + mở drawing copy), `run-spike.ps1` (Windows PowerShell 5.1 như 3 host; các bước dưới; kills only what it started; copy drawing từ E6 vào `HPCivil3d/output/live-verify/scene/` trước), `spike.py` (stdio qua `../../../McpShared/tools/mcp-session.py`, `harness_common.Checklist`).

Spike matrix (mỗi dòng = check PASS/FAIL; ghi `reports/phase-01-spike.md`):

| # | Câu hỏi (ADR) | Cách đo | Pass = |
|---|---|---|---|
| S-01 | Bundle `Platform="Civil3D"` nạp trong Civil 3D 2026 (ADR-02) | `acad.exe /nologo /ld "…\AecBase.dbx" /p "<<C3D_Metric>>" /product C3D /language en-US /b spike.scr` (E13); chờ pipe `\\.\pipe\hpcivil3d-mcp-2026` ≤ 420 s; `Answer-SecureLoad` | loader.log có `bridge started`; runtime log `MCP scripting self-check OK` + `Roslyn load context = HPCivil3d.McpBridge` + `civil product Civil3D`; pipe up |
| S-02 | AutoCAD 2026 thuần **không** nạp (ADR-02) | `acad.exe /nologo /product ACAD /language "en-US"`; chờ 180 s cửa sổ; đếm dòng `%LocalAppData%\HPCivil3d\McpBridge\logs\loader.log` trước/sau; pipe không tồn tại | dòng không tăng; không pipe; **và** bundle AutoCAD vẫn nạp (pipe `hpautocad-mcp-2026` up — hồi quy) |
| S-02b | Advance Steel 2026 không nạp | `/language "en-US" /product "ADVS" /p "<<ADVS>>"` (E13) | như S-02 |
| S-03 | Coexist: AutoCAD 2026 + Civil 3D 2026 cùng chạy | mở cả hai; `python mcp-call.py HPAutoCad…exe tools/call get_autocad_context` và `HPCivil3d…exe … get_civil3d_context` | hai `host` khác nhau, không lỗi; cửa sổ trạng thái Civil không báo "in use" |
| S-03b | Roslyn 5.9 trong ALC riêng khi Civil nạp thêm `Aecc*Mgd` | log ALC name + version `Microsoft.CodeAnalysis` 5.9 + `System.Collections.Immutable` 10.x (như AutoCAD spike 1) | đúng 3 giá trị |
| S-04 | `civil` resolve; `ActiveDocument` khi drawing trống / khi không phải Civil doc (ADR-03 U1) | execute `return civil == null ? "null" : civil.GetType().Name;` trên (a) `Align-1.dwg` copy, (b) drawing mới từ `acad.dwt` (AutoCAD thuần template) trong Civil 3D | (a) `CivilDocument`; (b) ghi kết quả (null/CivilDocument/exception) → ADR-03 §4 chốt text |
| S-05 | Đọc alignment thật (ADR-05 R2/R3) | `Align-1.dwg`: `var ids = civil.GetAlignmentIds(); … Name, Length, StartingStation, EndingStation, Entities.Count, GetEntityByOrder(0).EntityType, sub[0].StartPoint, GetStationStringWithEquations(StartingStation), StationOffset(e,n,…)` | count ≥ 1; số hợp lý (Length ≈ End−Start); station label dạng `0+00.00`; đơn vị = Feet (tutorial) |
| S-06 | Đơn vị (ADR-03 §3 U10/U11) | `civil.Settings.DrawingSettings.UnitZoneSettings.{DrawingUnits, CoordinateSystemCode, ImperialToMetricConversion}`; `db.Insunits`; `units.Label/MmPerUnit`; `GetCoordinateSystemByCode(code)` với code rỗng và code thật | Feet/304.8 trên `Align-1.dwg`; Meters/1000 trên drawing từ `_Autodesk Civil 3D Corridor Template (Metric).dwt`; mismatch log khi INSUNITS khác; `GetCoordinateSystemByCode("")` kết quả ghi (U11) |
| S-07 | `FindElevationAtXY` ngoài biên (U3) | `Surface-1.dwg` copy: điểm trong biên → số; điểm (1e9, 1e9) → catch → `ex.GetType().FullName` | kiểu exception ghi vào ADR-05 R6 |
| S-08 | Indexer/`Feature.Name`/`StyleBase.Name`/label set root (U6–U8) | `inspect_type Autodesk.Civil.DatabaseServices.Feature`, `…Styles.LabelSetStylesRoot`, `…Styles.StyleBase`, `…CorridorCollection`, `…AlignmentEntityCollection` qua tool `inspect_type` (reflection runtime) | chữ ký indexer/`Name` getter ghi vào addendum §12 |
| S-09 | Diện tích parcel (U5) | `Parcel-1.dwg`: `inspect_type Parcel` + thử `Autodesk.Aec.PropertyData` `[chưa xác minh]`; fallback `GeometricExtents` | có/không nguồn area → ADR-05 R9 |
| W1 | Ghi + dryRun rollback (ADR-04 §1) | `civil.CogoPoints.Add(new Point3d(...), "MCP", true)` ×3: (a) `dryRun:true` → `changed.added == 3`, `rolledBack`, `civil.CogoPoints.Count` không đổi (đọc lại `none`); (b) `auto` → Count +3, `U` qua COM → Count về cũ; (c) `transaction:none` + Add → `isError` "modified the drawing in transaction=none" + rollback | 3/3 |
| W2 | `Alignment.Create` từ polyline (ADR-05 W2, U2) | vẽ `Polyline` 3 đỉnh (script AutoCAD) → `Alignment.Create(civil, new PolylineOptions{PlineId=id}, "MCP-A1", "", db.Clayer name, <style đầu từ civil.Styles.AlignmentStyles>, <label set đầu hoặc "">)` dưới dryRun rồi auto; thử `siteName ""`, `labelSetName ""` | tạo được (handle), `Length` ≈ tổng đoạn; dryRun không để lại alignment; ghi overload nào chạy, `""` có được không |
| S-10 | Abort với dependency (ADR-04 U4) | `Corridor-1.dwg`: (a) `surface.AutoRebuild` đọc; `TinSurface.AddVertices(1 điểm)` dryRun → `NumberOfPoints` không đổi sau; (b) `corridor.Rebuild()` **qua ad-hoc code với guard tạm tắt bằng env `HPCIVIL3D_MCP_SPIKE=1`** dưới dryRun → thời gian, `IsOutOfDate` sau, corridor còn hiển thị (COM `Count` object), `U` không cần; (c) `RebuildAutomatic` bật + sửa `alignment.ReferencePointStation` dryRun → rebuild ngầm xảy ra? thời gian? | ghi kết quả → ADR-04 §3 mở/đóng `Rebuild` |
| S-11 | Busy/no-doc/opt-in (hồi quy đường AutoCAD) | gõ `LINE` qua COM → `-32002` sau 8 s; đóng drawing → `-32003`; opt-in off → `-32001` | 3/3 |

Non-functional
- `dotnet build HPCivil3d/HPCivil3d.slnx -c Debug -p:DeployBundle=false` xanh trên máy có Civil 3D; **trên máy không có**: bridge fail một lỗi đọc được, server xanh (kiểm bằng `-p:Civil3dInstallDir=X:\nowhere\`).
- `dotnet build HPAutoCad/HPAutoCad.slnx -c Debug -p:DeployBundle=false` vẫn xanh và không build gì của `HPCivil3d/`.

## Nếu ADR-01 = B (user chọn)
Phase 1 tách: **1a** extract `AcadShared/{HPAcad.McpBridge.Loader.Core, HPAcad.McpBridge.Runtime}` từ HPAutoCad + refactor `HPAutoCad.McpBridge{,.Loader}` dùng chúng + hồi quy 5 harness AutoCAD (bridge 21, live 65 + isolation, AEC 90 + 109, ribbon 12) + `tools/list` AutoCAD byte-identical; **1b** = phase này với bridge Civil = thin project trên `AcadShared` (không mirror test). +12–16 h; CLAUDE.md chiều phụ thuộc ở phase 5.

## Architecture
Xem [architecture.md §2–3](architecture.md). Spike chạy qua đường thật: `spike.py` → `HPCivil3d.Mcp.Server.exe` (Debug) → pipe → bridge trong Civil 3D.

## Related Code Files
- Create: toàn bộ `HPCivil3d/` liệt kê ở Requirements (≈ 26 file mã + 4 XAML + props/slnx/json + harness 5 file + README).
- Modify: **không có** ngoài `HPCivil3d/` (McpShared đã xong ở phase 0; `.mcp.json` **không** — spike gọi exe trực tiếp).
- Reports: `reports/phase-01-spike.md`, `reports/phase-01-spike-run{1,2}.log`, `reports/code-review-phase-01.md`, `reports/test-report-phase-01.md`.

## Implementation Steps
1. `HPCivil3d/{Directory.Build.props, global.json, HPCivil3d.slnx, .gitignore, README.md}`; kiểm `Civil3dInstallDir` resolve = `C:\Program Files\Autodesk\AutoCAD 2026\C3D\` (`dotnet msbuild -getProperty:Civil3dInstallDir`).
2. Copy Loader (8 `.cs` + csproj + manifest) → đổi token theo ADR-06 §1 bằng script sed một lần (ghi bảng token vào `README.md` — cũng là input của mirror test phase 2); GUID mới; `HostAssemblyPrefixes` += `"Aec"`.
3. Copy Bridge (14 file) → đổi token; bỏ Aec; thêm 3 `<Reference>`; `Civil3dScriptGlobals` + `civil`; `Civil3dUnits`; probe self-check; `CompilerReferences`.
4. Server tạm (Program + profile + 2 tool); build solution; kiểm `bin/Debug/net8.0-windows/` của bridge có `deps.json`, Roslyn 5.9, **không** có `AeccDbMgd.dll`/`Ac*.dll`.
5. Harness: `harness-common.ps1` (+`-Product/-Profile`), `spike.scr`, `run-spike.ps1`, `spike.py`; copy scene E6 (`Align-1.dwg`, `Surface-1.dwg`, `Corridor-1.dwg`, `Parcel-1.dwg`, `Pipe Networks-1.dwg`, `Points-1.dwg` — tên chính xác kiểm bằng `ls` lúc chạy) vào `output/live-verify/scene/`.
6. Đóng mọi acad.exe → `dotnet build -c Debug` (deploy bundle) → `run-spike.ps1` chạy S-01 → S-11, W1–W2 (Civil), rồi S-02/S-02b/S-03 (AutoCAD/ADVS/coexist); mỗi bước log + Checklist JSON.
7. `reports/phase-01-spike.md` (bảng câu hỏi → kết quả → quyết định); cập nhật ADR-02/03/04 Status + addendum §12; ADR-05 R6/R9/W2 chốt.
8. Code review → fix → chạy lại spike (≥ 2 lần pass liên tiếp).

## Todo List
- [ ] props/slnx/json/README · [ ] Loader copy + token + `Aec` prefix + GUID · [ ] Bridge copy + refs + `civil` + units + probe · [ ] Server tạm · [ ] Harness spike + scene copy · [ ] S-01…S-11, W1–W2 pass/kết luận (×2) · [ ] ADR-02/03/04/05 cập nhật · [ ] Report + review

## Success Criteria
- [ ] `dotnet build HPCivil3d/HPCivil3d.slnx -c Debug -p:DeployBundle=false` → 0 error; `-p:Civil3dInstallDir=X:\nowhere\` → đúng 1 error text "Civil 3D 2026 not found…" ở bridge, server vẫn build.
- [ ] `pwsh -File HPCivil3d/tools/harness/run-spike.ps1` → S-01, S-02, S-02b, S-03, S-03b, S-11, W1 **PASS**; S-04…S-10, W2 có **kết luận ghi rõ** (kể cả "không được" là kết luận hợp lệ); Civil 3D/AutoCAD tự thoát; 2 lần liên tiếp cùng kết quả.
- [ ] `reports/phase-01-spike.md` tồn tại; ADR-02 Accepted (hoặc §4 fallback kích hoạt), ADR-03 §4 text chốt, ADR-04 §3 quyết `Rebuild`, ADR-05 R6/R9/W2 điền.
- [ ] 7 suite cũ + `HPRebar.Mcp.Server.Core.Tests` (phase 0) không hồi quy; `HPAutoCad` build xanh; harness AutoCAD `run-bridge-unattended.ps1` 21/21 vẫn pass **sau khi bundle Civil tồn tại** (bundle Civil không nạp vào AutoCAD — chính là S-02 nhìn từ phía AutoCAD).

## Risk Assessment
| Risk | Mitigation |
|---|---|
| Autoloader không lọc theo `Civil3D` (nạp cả AutoCAD) → tranh không, nhưng bridge Civil crash trong AutoCAD vì thiếu `AeccDbMgd` | S-02 phát hiện; fallback ADR-02 §4 demand-load `ACAD-9100:409\Applications`; loader bắt `FileNotFoundException` ở `Start()` → log + không throw ra AutoCAD |
| SECURELOAD trong Civil 3D khác AutoCAD (không bấm được bằng UIA) | `Answer-SecureLoad` copy; nếu fail → user *Always Load* một lần, harness ghi MANUAL |
| Hai plugin MCP cũ (`Civil3dMcp`, `AutoCadMcp`) bật dialog/log khi Civil khởi động | harness chờ 420 s, log window titles lạ; không đụng bundle user; ghi vào report |
| `Corridor.Rebuild()` trong S-10 làm Civil 3D treo/crash | chạy **cuối cùng** trong phiên, trên copy, timeout 120 s, kill acad nếu > 300 s; kết quả "hỏng" = giữ deny |
| Tutorial drawing Imperial → nhầm đơn vị | S-06 đo trên cả Imperial (`Align-1.dwg`) và Metric (template) |
| `civil` null trong drawing mới từ `acad.dwt` | S-04 (b) quyết text/`IsCivilDocument` — không giả định |

## Security Considerations
- Spike-only commands/guard bypass chỉ khi env `HPCIVIL3D_MCP_SPIKE=1` (xoá cuối phase 2 như AutoCAD `HPMCPSPIKE`). Opt-in vẫn OFF mỗi khởi động; harness tick qua UIA trên cửa sổ của ta.

## Next Steps
- Phase 2 hoàn thiện runtime + ribbon + mirror test theo kết luận spike; phase 3 server/seed cần ADR-05 R6/R9/W2 đã chốt.
