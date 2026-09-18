# Evidence on machine — 2026-09-17 (Civil 3D 2026 MCP plan)

Mọi mục dưới đo trực tiếp trên máy dev bằng lệnh read-only (ls / grep / registry read / wc). **Không** mở Civil 3D hay AutoCAD. Trích dẫn trong plan = `E<n>`. Reflection API → [researcher-01](researcher-01-civil3d-api-reflection.md); web facts → [researcher-02](researcher-02-civil3d-autoloader-launch-rebuild-facts.md); đo mã HPAutoCad → [researcher-03](researcher-03-hpautocad-mirror-shareable-core-and-tests.md).

## E1 — Ba sản phẩm R25.1 chung một `acad.exe`
`HKLM\SOFTWARE\Autodesk\AutoCAD\R25.1\`:
| Key | ProductName | ProductID | Location |
|---|---|---|---|
| `ACAD-9100:409` | Autodesk Civil 3D 2026 - English | **9100** | `C:\Program Files\Autodesk\AutoCAD 2026\` |
| `ACAD-9101:409` | AutoCAD 2026 - English | 9101 | cùng |
| `ACAD-9126:409` | Advance Steel 2026 - English | 9126 | cùng |
→ Một thư mục cài, một exe, ba product key. "Chỉ nạp vào Civil 3D" không thể dựa vào path hay Series (R25.1 cả ba); phải dựa vào token `Platform` của autoloader (ADR-02) hoặc demand-load theo ProductID 9100 (fallback).

## E2 — Runtime .NET 8 (gate đầu phase 1 giữ nguyên như AutoCAD)
`C:\Program Files\Autodesk\AutoCAD 2026\acdbmgd.runtimeconfig.json`: `tfm net8.0`, frameworks `Microsoft.NETCore.App 8.0.0` + `Microsoft.WindowsDesktop.App 8.0.0` + `Microsoft.AspNetCore.App 8.0.0`. → Bridge/Loader `net8.0-windows`, `AutoCAD.NET [25.1.0]` pin y như HPAutoCad; Roslyn 5.9 vẫn cần ALC riêng (AutoCAD ADR-05).

## E3 — Civil API managed DLL nằm trong `C3D\`, không NuGet
`C:\Program Files\Autodesk\AutoCAD 2026\C3D\`: `AeccDbMgd.dll` (**không** có `.runtimeconfig.json` bên cạnh), `AeccPressurePipesMgd.dll`(+rc), `AeccDataShortcutMgd.dll`(+rc), `AeccCogoMgd.dll`(+rc), `AeccUiMgd.dll`(+rc), `AeccLogMgd.dll`, `AeccHydroCalcsMgd.dll`, `AeccAdpMgd.dll`, `AeccMgdReverse.dll`, `AeccFdoArcGisOnlineMgd.dll`; native `AeccDataShortcut.dbx`, `AeccPressurePipes.dbx`, `AeccUi*.arx`.
`C:\Program Files\Autodesk\AutoCAD 2026\ACA\`: `AecBaseMgd.dll` (+`.xml` docs), `AecPropDataMgd.dll` (+`.xml`). → Reference path cho compile = `C3D\` **và** `ACA\` (Autodesk sample csproj E5 xác nhận `AecBaseMgd` là reference bắt buộc).
Root: `acdbmgd.dll`, `acmgd.dll`, `accoremgd.dll`, `acdbmgdbrep.dll`, `AdWindows.dll`.

## E4 — Autoloader `Platform` token đang dùng trên máy
`%AppData%\Autodesk\ApplicationPlugins\`: `AlphaBIM`, `AutoCadMcp.bundle`, `AutocadVidbeCoding.bundle.disabled`, `CadAddinManager.bundle`, **`Civil3dMcp.bundle`**, `HPAutoCad.McpBridge.bundle`. `%ProgramData%\Autodesk\ApplicationPlugins\`: chỉ Navis/3ds Max/Revit/IFC bundle.
Token thống kê qua mọi `PackageContents.xml`: `AutoCAD*` ×19, `AutoCAD` ×2 (HPAutoCad + 1), `NAVMAN|NAVSIM` ×23, `NAVMAN` ×5, `Revit` ×13, `3ds Max|3ds Max Design` ×2. **Không có bundle nào trên máy dùng token riêng cho Civil 3D** → token phải lấy từ docs Autodesk (researcher-02) và **spike phase 1 phải chứng minh** (không có precedent local).
Foreign `Civil3dMcp.bundle` (plugin cũ của user, tiếng Việt): `Platform="AutoCAD*" SeriesMin="R25.0" SeriesMax="R25.1"`, `LoadOnAutoCADStartup="True"`, `Contents\Civil3dMcpPlugin.dll` — **nạp vào cả AutoCAD lẫn Civil 3D**; `AutoCadMcp.bundle` y hệt. → Rủi ro harness: hai plugin lạ này nạp vào Civil 3D 2026 cùng lúc với bundle của ta; nếu chúng mở pipe/port riêng thì không đụng `hpcivil3d-mcp-2026`, nhưng chúng có thể bật dialog/log. Phase 1 ghi nhận; **không** đụng/xoá bundle của user (mirror quy tắc Navis "never touches the foreign plugin").

## E5 — Sample .NET của Autodesk: reference set chuẩn
`C:\Program Files\Autodesk\AutoCAD 2026\C3D\Sample\Civil 3D API\DotNet\GettingStarted\GettingStarted.csproj` và `CSharp\RoadwaySample\*.csproj`: `<Reference Include="acdbmgd|acmgd|accoremgd">` (`$(ArxMgdPath)`), **`AecBaseMgd`** (`$(OMFMgdPath)`), **`AeccDbMgd`** (`$(AeccMgdPath)`). Sample khác trong `DotNet\CSharp\`: `BatchEditLabelTextSample, CommandSettingsSample, CompareStyles, DotNetComInterop, DraggedLabelSample, EditLabelStyleSample, OffsetAlignmentDemo, PipeDataExcel, PointSample, QTOExport, RoadwaySample, Spiral2 Demo, SurfacesWaterdropSample`; `GettingStarted\Class1 step 4..7.cs` (tutorial code đọc alignment/surface — nguồn mẫu cho seed đọc, phase 3).

## E6 — Scene metric bỏ đi cho live verify: có sẵn 163 drawing tutorial
`C:\Program Files\Autodesk\AutoCAD 2026\C3D\Help\Civil Tutorials\Drawings\` — 163 file: `Profile-*` 17, `Surface-*` 14, `Labels-*` 13, `Align-*` 12 (`Align-1.dwg` … `Align-7C.dwg`), `Survey-*` 11, `Points-*` 9, `Parcel-*` 9, `Grading-*` 9, `Corridor-*` 9, `Pipe Networks-*` 8, `Quantities-*` 7, `Assembly-*` 7, `Intersection-*` 6, `Align-Superelevation-*` 5, …, `_Autodesk Civil 3D Corridor Template (Metric).dwt` + `(Imperial).dwt`. Tutorial Autodesk dùng **feet** cho phần lớn (Imperial) — đơn vị từng file phải đọc bằng `civil.Settings.DrawingSettings.UnitZoneSettings` lúc harness chạy `[chưa xác minh đơn vị từng file]`. Ngoài ra `C3D\Sample\Civil 3D API\DotNet\CSharp\Spiral2 Demo\demo.dwg`, `Dynamic Blocks\Parking Tools - Metric.dwg` (AutoCAD thuần). Harness phải **copy** drawing sang `HPCivil3d/output/live-verify/` trước khi mở (Program Files read-only; không sửa file gốc). Thư mục template user `%AppData%\Autodesk\C3D 2026\` + `%LocalAppData%\Autodesk\C3D 2026\` tồn tại (profile Civil đã khởi tạo ít nhất một lần); `%ProgramData%\Autodesk\C3D 2026\` tồn tại (không thấy `.dwt` ở depth 3).

## E7 — Đo `HPAutoCad` (số liệu thô cho ADR-01; phân loại chi tiết → researcher-03)
| Project | File | Dòng |
|---|---|---|
| `HPAutoCad.McpBridge.Loader` | 10 (8 `.cs` + csproj + `PackageContents.xml`) | **565** — `BridgeLoaderApplication` 111, `Ribbon/McpRibbonTab` 151, `BridgeLoadContext` 48, `BridgeActions` 42, `LoaderLog` 30, `BridgeLoaderCommands` 28, `Ribbon/RibbonCommandHandler` 27, `Ribbon/RibbonIcons` 45, csproj 55, manifest 28 |
| `HPAutoCad.McpBridge` | 14 (`Model/ Service/ View/ Resources/Themes/`) | **1 642** — `BridgeEntry` 185, `MainThreadExecutor` 238, `Service/AutocadScriptRunner` 280, `AutocadResultSerializer` 210, `AutocadContextReader` 110, `DatabaseChangeCounter` 73, `ScriptingSelfCheck` 59, `AutocadThemeSwitcher` 43, `AutocadVersionMap` 27, `Model/AutocadScriptGlobals` 55, `View/AutocadBridgeStatusView.xaml` 160 + `.cs` 25, `Resources/Themes/AutocadTheme.xaml` 123 + `Light` 16, csproj 38 |
| `HPAutoCad.Mcp.Server` (không seed) | 8 | **305** — `Program` 8, `Hosts/AutocadHostProfile` 43, `Tools/ExecuteAutocadCodeTool` 59, `Tools/AutocadContextTool` 33, `Prompts/AutocadScriptPrompts` 72, `Resources/AutocadDocumentResources` 21, `appsettings.json` 31, csproj 38 |
| `HPAutoCad.Mcp.Server.Tests` | 3 `.cs` | 678 — `SeedLibraryTests` 439, `AutocadToolsOverPipeTests` 134, `HostProfileTests` 105 |
| `HPAutoCad/tools/harness/` | 13 | `run-live-verify.ps1`, `live-verify.py`, `harness-common.ps1`, `run-bridge-unattended.ps1`, `pipe-scenarios.py`, `run-server-smoke.ps1`, `run-ribbon-check.ps1`, `bridge.scr`, `run-aec-*` ×4, `README.md` |
Loader + Bridge = **2 207 dòng** thô (kể csproj/xaml/manifest). `HPAutoCad.McpBridge.csproj` tham chiếu `../HPAutoCad.Aec` (**AutoCAD-specific**) và `BridgeEntry.cs:113 CompilerReferences` nối Aec vào reference của script; `HostScriptContracts.AutocadImports` (`McpShared/HPRebar.Mcp.Contracts/HostScriptContracts.cs:25–34`) chứa `"HPAutoCad.Aec"` → Civil **không** dùng lại `AutocadImports` nguyên văn.

## E8 — Seam engine hôm nay (đường vào của host thứ năm; số dòng đo 2026-09-17)
| File | Dòng | Nội dung |
|---|---|---|
| `McpShared/HPRebar.Mcp.Contracts/PipeNaming.cs` | 15–26, 41–48 | hằng `RevitHost/AutocadHost/NavisHost/EtabsHost`; `switch` với nhánh mặc định `_ => "hp" + key + "-mcp-" + version` — `For("civil3d", 2026)` **đã** = `hpcivil3d-mcp-2026` |
| `…/JsonRpc/JsonRpcMethods.cs` | 35–38 | `RevitPrefix/AutocadPrefix/NavisPrefix/EtabsPrefix` |
| `…/HostScriptContracts.cs` | 12–34 (imports Revit/AutoCAD), 42–48 Navis, 51–61 globals, 68/93 heavy consts, 75–87 ETABS | append cuối file như ETABS phase 0 #3 |
| `…/Messages/ContextMessages.cs` | 24–30 (`Autocad?`, `Navis?`, `Etabs?` slots), 66–73 `AutocadInfo` 7 field, 106 `EtabsInfo` | slot `Civil3dInfo?` mới sau `:30`; `Shape` (`Server.Core/Services/ContextService.cs:52–65`) bỏ `revitVersion/isFamily` cho non-Revit, null bị bỏ → **không sửa `Shape`** |
| `…/Messages/ExecuteResult.cs` | 11–51 | `Changed`, `RolledBack`, `Snapshot` (ETABS) — Civil không thêm field |
| `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs` | 22–45 `Autocad` (deniedIdentifiers `MessageBox, SystemObjects`; deniedMembers prompt `ed.Get*`/`Select*`, `SendStringToExecute/Command*/ExecuteIn*Context*`, modal, `StartTransaction/StartOpenCloseTransaction/TopTransaction/LockDocument`; `tr.Commit/Abort/Dispose`; namespaces `Autodesk.AutoCAD.Interop`, `System.Windows.Forms`), 54 `Navis`, 85 `Etabs` | `Civil3d` = **superset** của `Autocad` (ADR-04) |
| `…/Scripting/AnalyzerProfile.cs` | 14–16 `Autocad` (`StartTransaction`, `StartOpenCloseTransaction`), 23, 28 | `Civil3d` = copy `Autocad` |
| `McpShared/HPRebar.Mcp.Server.Core/Hosts/IHostProfile.cs` | 15–81 | 17 member — ADR-06 điền |
| `HPAutoCad/HPAutoCad.McpBridge/BridgeEntry.cs` | 34 `HostName="AutoCAD"`, 66 `BridgeSettingsStore(VendorFolder, ProductFolder)`, 70 `ScriptCompiler(CompilerReferences(autocadApi), AutocadImports, typeof(AutocadScriptGlobals), …)`, 80 `new McpBridgeHost(_executor, settings, store, hostVersion, PipeNaming.For(AutocadHost, year), HostName, AutocadPrefix)`, 113 `CompilerReferences` | 4 điểm tiêm host — đúng chỗ Civil đổi token |

## E9 — Harness AutoCAD đã mở Civil 3D 2026 unattended
`HPAutoCad/tools/harness/run-live-verify.ps1:118`: `Start-Process 'C:\Program Files\Autodesk\AutoCAD 2026\acad.exe' -ArgumentList @('/nologo', '/product', 'C3D', '/language', '"en-US"')`; vòng chờ ≤ 300 s, `Answer-SecureLoad` mỗi 5 s, kiểm `loader.log` không tăng dòng (bundle AutoCAD **không** nạp vào Civil 3D — "isolation 2" đã pass 2026-09-14 với `Platform="AutoCAD"`). → Phương pháp start Civil 3D cho harness HPCivil3d: **copy nguyên** (thêm `/p <<C3D_Metric>>` `[chưa xác minh tên profile]` và `/b <script>` để mở drawing + tự thoát như `bridge.scr`).

## E10 — Không có `ck` CLI → plan viết file trực tiếp (như 4 plan trước). Plan folder `plans/260917-1633-civil3d-mcp-2026/` tạo 16:33.

## E11 — Ba plan tham chiếu tồn tại đúng path
`plans/260913-0000-autocad-mcp-bridge-2026/{plan.md, architecture.md, adr/adr-01..06, research/3, reports/…, phase-00..05}` · `plans/260915-0824-navisworks-mcp-2026/{…, adr/adr-01..05, research/2, reports/snapshot-tools-list.ps1, phase-00..05}` · `plans/260916-2152-etabs-mcp-2026/{…, adr/adr-01..05, research/3, reports/regression-tools-list-phase-04.py + run-phase-00-gate.ps1 + snapshot-tools-list.ps1, phase-00..04}` · `plans/templates/{bug-fix,feature-implementation,refactor}-template.md`.

## E12 — Docs/skill hiện có (phase 5 sửa)
`docs/{codebase-summary, project-changelog, system-architecture}.md` đều nhắc `HPEtabs`/`HPNavis` (grep) → thêm HPCivil3d cùng chỗ; `docs/mcp-architecture.md` tồn tại; `.claude/skills/{hp-mcp-autocad, hp-mcp-etabs, hp-mcp-revit}` (skill `hp-mcp-civil3d` = follow-up tuỳ chọn, ngoài scope). `McpShared/tools/{mcp-call.py, mcp-session.py, harness_common.py, README.md}` canonical.

## E13 — Profile + shortcut Civil 3D 2026 thật (registry + `.lnk`, read-only)
`HKCU\Software\Autodesk\AutoCAD\R25.1\ACAD-9100:409\Profiles` = `<<C3D_Imperial>>`, `<<C3D_Metric>>`, `AutoCAD` (gạch dưới, **không** dấu cách — researcher-02 sai). `ACAD-9101:409\Profiles` = `<<Unnamed Profile>>`.
Start Menu: `Civil 3D 2026 Metric.lnk` → `acad.exe /ld "C:\Program Files\Autodesk\AutoCAD 2026\AecBase.dbx" /p "<<C3D_Metric>>" /product C3D /language en-US`; `Civil 3D 2026 Imperial.lnk` tương tự với `<<C3D_Imperial>>`; `AutoCAD 2026 - English.lnk` → `/product ACAD /language "en-US"`; `Advance Steel 2026 - English.lnk` → `/language "en-US" /product "ADVS" /p "<<ADVS>>"`. → Harness HPCivil3d dùng **đúng dòng lệnh shortcut Autodesk** + `/nologo` + `/b <script>`; isolation "Advance Steel không nạp bundle" dùng `/product "ADVS" /p "<<ADVS>>"`.

## E14 — Autoloader ghi bundle vào key demand-load theo product
`HKCU\Software\Autodesk\AutoCAD\R25.1\ACAD-9100:409\Applications` (Civil 3D) chứa `AutoCadMcpPlugin`, `Civil3dMcpPlugin`, `CadAddinManager` (= 3 bundle `Platform="AutoCAD*"` trong `%AppData%`), cùng `AcMap*`, `GISExtension`, `ATRA_AutoTrack*`… → (1) hai plugin MCP cũ của user **đang nạp vào Civil 3D 2026** mỗi lần khởi động; (2) fallback demand-load theo product key `ACAD-9100:409` là có thật (ADR-02 §fallback). `HKLM\…\ACAD-9100:409\Applications`: `AeccImageAssets130`, `AecUiStatusBar80`, `AcLayer`, `AcadVBA`, `AcRVTDWGCoordsSyncAcadClient`, `HelpBrowser`.

## E15 — Token autoloader `Platform="Civil3D"` (nguồn Autodesk, không phải suy diễn)
- Civil 3D 2025 Developer's Guide › "Edit the PackageContents.xml File" (`help.autodesk.com/cloudhelp/2025/ENU/Civil3D-DevGuide/files/GUID-6FDC9D3D-FAB2-453E-A7BF-F1CC82F4AE18.htm`, fetch 2026-09-17): *"be sure to set both SeriesMin and SeriesMax to "R25.0" and Platform to "Civil3D" to ensure that it can be loaded **and only loaded** in Civil 3D 2025"*; ví dụ `<RuntimeRequirements SeriesMin="R25.0" SeriesMax="R25.0" Platform="Civil3D" OS="Win64"/>`. 2026 = `R25.1` (E1/E2).
- AutoCAD "RuntimeRequirements Element Reference" (`help.autodesk.com/cloudhelp/2015/ENU/AutoCAD-Customization/files/GUID-1591CA01-EF87-48CD-952B-772FE26037F1.htm`, bản 2025 trả 404): Platform = *"Target AutoCAD or AutoCAD-based products… Multiple AutoCAD platforms can be specified by separating the values with the '|' symbol."* Giá trị: `ACADE, ACADM, ACLT, ADT, AIP, AIPRS, AIPSIM, AIS, AOEM, AutoCAD, AutoCAD*, Civil, Civil3D, LDT, Map, MEP, Plant3D, PNID`. `Civil` = Autodesk Civil (không phải Civil 3D); `Civil3D` là token đúng. Không có `C3D` trong danh sách (đó là giá trị `/product`).
- Chiều "AutoCAD 2026 thuần **không** nạp bundle `Civil3D`" và "Advance Steel không nạp" là suy diễn từ semantics → **spike S-01/S-02 phải chứng minh** (E4: không bundle nào trên máy dùng token này).

## E16 — ~~`Parcel` .NET 2026 không có `Area`~~ **ĐÍNH CHÍNH 2026-09-18 (spike S-09, `inspect_type` runtime): `Parcel.Area { get; }` CÓ** (kế thừa; probe metadata declared-only bỏ sót). `Perimeter` không có. → `list_parcels` trả `area` (drawing unit²).

## E17 — `Corridor.RebuildAutomatic {get;set;}`, `Surface.AutoRebuild {get;set;}`, `Corridor.Rebuild()`, `CorridorCollection.RebuildAll()`, `Surface.Rebuild()/RebuildSnapshot()` tồn tại (probe) → guard deny `Rebuild`, `RebuildAll`, `RebuildSnapshot` MVP (ADR-04); `RebuildAutomatic`/`AutoRebuild` setter cũng deny (đổi setting persist trong DWG, kích rebuild ngầm).
