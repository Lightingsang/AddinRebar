# Codebase Summary — HPRebar + AutoCAD Bridge + Navisworks Bridge

Cập nhật: 2026-09-15

## Repository Layout

Gồm 5 unrelated deliverables + 1 thư mục engine chung (không cross-wire; chiều phụ thuộc duy nhất: thư mục MCP → `McpShared/`):

| Thư mục | Loại | Stack |
|---|---|---|
| `HPRebar/` | Revit add-in + server MCP | C# / Nice3point / WPF / xUnit / TUnit |
| `McpShared/` | Host-neutral MCP engine (+ `tools/` script harness stdio dùng chung) | netstandard2.0 · net48 · net8.0 · net10.0 |
| `HPAutoCad/` | Unified AutoCAD 2026 add-in (HPGeoLink) + MCP bridge + AEC engine | C# / net8.0 · net8.0-windows · net10.0 · net10.0-windows / AutoCAD.NET 25.1 + WebView2 + MaterialDesign |
| `HPNavis/` | Navisworks Manage 2026 plugin + server MCP (phases 0–5 ✅ verified 2026-09-15) | C# / net48 (plugin) · net10 (server) / Navisworks API 23.0 |
| `HPEtabs/` | ETABS 22 MCP — bridge app standalone (COM out-of-process) + server (phases 0–4 ✅ verified 2026-09-17) | C# / net8.0-windows (bridge) · net10 (server) / ETABSv1 2.10 |
| `HPCivil3d/` | Civil 3D 2026 MCP — bridge AutoCAD copy + Civil API, bundle `Platform="Civil3D"` + server (phases 0–5 ✅ verified 2026-09-18) | C# / net8.0-windows (bridge) · net10 (server) / AutoCAD.NET 25.1 + Civil API cài sẵn |
| `revit-market-research/` · `scripts/skill_sync/` · `course-website/` | Tooling | Node / Python / HTML |

## HPRebar Solution

`HPRebar/HPRebar.slnx` + global.json pin .NET SDK `10.0.300`, test runner `Microsoft.Testing.Platform`. Reference McpShared projects qua đường dẫn `../McpShared/...`.

| Project | TFM | Vai trò | Build? |
|---|---|---|---|
| `HPRebar/` | net48 (R23/R24) · net8.0-windows7.0 (R25/R26) · net10.0-windows7.0 (R27) | Add-in. Chạm Revit API | ✅ |
| `HPRebar.Core/` | netstandard2.0 | Toán thuần. **Không** reference Revit | ✅ |
| `HPRebar.Core.Tests/` | net8.0 | xUnit v3 — 334 test | ✅ |
| `HPRebar.Tests/` | R25/R26 | TUnit, load Revit in-process | ❌ |
| `HPRebar.Mcp.Server/` | net10.0 console | Thin exe (`Program.cs` một dòng) + 21 seed nhúng + CLI `registry …` | ✅ |
| `HPRebar.McpBridge/` | net8.0-windows7.0 (R25/R26) | Add-in thứ hai — bridge trong Revit (nút ExternalEvent + cửa sổ) | ✅ R25/R26 |
| `build/` | — | ModularPipelines | ❌ |
| `install/` | — | WixSharp installer | ❌ |

## McpShared Solution

`McpShared/McpShared.slnx` + global.json. Host-neutral engine dùng chung cho Revit + AutoCAD.

| Project | TFM | Vai trò |
|---|---|---|
| `HPRebar.Mcp.Contracts/` | netstandard2.0 · net48 | Envelope JSON-RPC + DTO chung server ↔ bridge. Không Revit, không MCP SDK. Asset net48 vì Roamer.exe reflect mọi type plugin trước khi resolver chạy |
| `HPRebar.McpBridge.Core/` | net8.0 · net48 | Pipe listener/dispatcher, Roslyn `ScriptGuard`/`ScriptCompiler`, `BridgeSettingsStore`, `RequestDispatcher`. Logic không chạm Revit; test xUnit + pipe thật |
| `HPRebar.Mcp.Server.Core/` | net10.0 | `McpServerHost` (builder pattern), `HostProfile` interface + Revit impl, `ExecuteCodeService`/`ContextService`, Registry engine (SQLite/FTS5 + FileSystemWatcher), 4 core tool + registry CLI |
| `HPRebar.Mcp.Server.Core.Tests/` | net10.0 | xUnit v3 — 162 test: host-neutrality, registry per profile, guard/compiler/args/analyzer, HostProfile binding, registry store/db/validator, pipe round-trip fake executor, all Revit + AutoCAD + **ETABS** profiles (2026-09-16 phase 0), stability window |

### Civil 3D MCP (`HPCivil3d/` — plan complete, phases 0–5 2026-09-17/18)

Civil 3D 2026 là AutoCAD vertical (cùng `acad.exe /product C3D`, AutoCAD.NET 25.1, .NET 8) nên `HPCivil3d/` = bridge AutoCAD **copy** với token Civil + Civil API bên trên (ADR-01 = A, user quyết định), không reference `HPAutoCad/`; mirror test giữ hai bản đồng bộ.

| Project | TFM | Vai trò |
|---|---|---|
| `HPCivil3d.McpBridge.Loader/` | net8.0-windows | Assembly autoloader NETLOAD (`Bundle/PackageContents.xml`, **`Platform="Civil3D"`** → chỉ nạp trong Civil 3D); ALC riêng (Roslyn 5.9 + Immutable 10; prefix `Ac/Ad/Aec/Autodesk.` dùng chung host), lệnh `HPC3DMCPBRIDGE/HPC3DMCPSTART/HPC3DMCPSTOP/HPC3DMCPSTATUS`, ribbon **HPCivil3d ▸ MCP ▸ MCP Bridge** |
| `HPCivil3d.McpBridge/` | net8.0-windows | Bridge: pipe `hpcivil3d-mcp-2026`, `civil3d.*`; `MainThreadExecutor` (Idle + WM_NULL), outer/inner transaction (`dryRun` = abort, `manual` ≡ auto), `GuardProfile.Civil3d` (thêm `Rebuild*`, data shortcuts, survey, `ExportTo*`/`CreateFrom*`/`ImportPoints`, `AeccUiMgd`, `AECC.Interop`), global `civil` (`CivilDocument`), `Civil3dUnitTable` (Meters 1000 / Feet 304.8 theo **Civil drawing unit**, `insunitsMismatch`), context block `civil3d` 11 field, serializer Civil (AlignmentEntity/SubEntity, CogoPoint, StyleBase theo tên). Reference `AeccDbMgd`/`AeccPressurePipesMgd`/`AecBaseMgd` từ bản cài (`Directory.Build.props`), không copy |
| `HPCivil3d.Mcp.Server/` | net10.0 console | `Civil3dHostProfile` (HostId `civil3d`, 2026, 10 category, CLI), `execute_civil3d_code`/`get_civil3d_context` + `inspect_type`/`cancel_execution` + 8 registry tool + **12 seed nhúng** (sinh bởi `tools/generate-seed-library.py`: 10 `none` + `create_cogo_points`/`create_alignment_from_polyline` `auto`) → `tools/list` **24**; prompts `civil3d_query_template`/`civil3d_modify_template`/`toolify_run`; resources `civil3d://document/info`/`civil3d://selection`. Không reference AutoCAD/Civil |
| `HPCivil3d.McpBridge.Tests/` | net10.0 | xUnit v3 — **55**: MirrorTests trên `tools/mirror-tokens.json` (24 file mirror, 50 token có thứ tự, block `civil-only` chỉ được thêm dòng, 13 file Civil-owned, 10 pin sha256 nguồn AutoCAD; đổi 1 ký tự → đúng 1 test fail), `Civil3dUnitTableTests`. Không reference host — build mọi máy |
| `HPCivil3d.Mcp.Server.Tests/` | net10.0 | xUnit v3 — **106**: profile, hai tool Civil qua pipe thật với fake executor, seed record/validator/guard/analyzer schema ⇔ `args`, forbidden member, unit label + page cap, `Schema_defaults_equal_the_code_fallbacks`, compile từng seed với AutoCAD.NET 25.1.0 + Civil DLL cài sẵn (13 skip rõ khi không có Civil 3D) |
| `tools/harness/` | PS 5.1 · pwsh 7 · Python | `run-live-verify.ps1` + `live-verify.py` (exe publish, registry cách ly, A 4 / E 20 + E' 4 / S 24 / R 21 / X 3 + isolation 9; **3 × 76 + 8/8, sau review 80/80 + 9/9**), `run-bridge-unattended.ps1` 31 ×4, `run-server-smoke.ps1` 28, `run-ribbon-check.ps1` 12 + 1 MANUAL, `run-spike.ps1`; chỉ start/kill acad.exe của chính nó, SECURELOAD *Load Once* theo pid, không lưu drawing, hash 2 registry root thật trước/sau |

**Sự thật đã verify (spike phase 1, không nghiên cứu lại):** mọi DWG mở trong Civil 3D có `CivilDocument` (`acad.dwt` → `DrawingUnits Feet`); `"."` = không zone; `Parcel.Area` **có**; ngoài surface → `PointNotOnEntityException` "Point Outside Surface."; `Alignment.Create` cần label set không rỗng; `Autodesk.Civil.DatabaseServices` có `Entity`/`DBObject` riêng → alias; `Abort()`/`U` hoàn lại CogoPoint/Alignment/TinSurface vertex/corridor rebuild — `Rebuild*` vẫn **deny** (chỉ đo trên corridor tutorial nhỏ); SECURELOAD 1 prompt/**hash DLL**, chặn nạp tới khi trả lời; COM `RPC_E_CALL_REJECTED` hàng chục giây sau đổi workspace.

**Phase 0 (2026-09-17):** hằng số engine trong `McpShared/` (`PipeNaming.Civil3dHost`, `JsonRpcMethods.Civil3dPrefix`, `HostScriptContracts.Civil3dImports/Globals`, `ContextResult.Civil3d`/`Civil3dInfo`, `GuardProfile.Civil3d`/`AnalyzerProfile.Civil3d`) + sửa bypass `?.` trong `ScriptGuard` cho mọi host; gate 4 host `tools/list` byte-identical. **Phase 1:** scaffold + spike 8 run (`reports/phase-01-spike.md`). **Phase 2:** runtime Civil, mirror tests, harness pipe + ribbon. **Phase 3:** server profile, 12 seed, 106 test, smoke 28/28. **Phase 4:** live-verify + registry loop + isolation hai chiều; review 7.5/10 → M1 (root registry AutoCAD của exe bên cạnh phải cách ly) + M2/M3 + 9 Low sửa cùng ngày. **Phase 5:** publish (exe 7.1 MB, `tools/list` 24 từ exe publish), docs, `AGENTS.md` regen bằng engine. Known gaps: modal Civil chưa kích, corridor dự án thật, data shortcut, pressure parts/point group, `test_tool realRun`, Civil 3D 2025/.NET 10, follow-up B `AcadShared/`. Skill người dùng: `.claude/skills/hp-mcp-civil3d/` (+ mirror `.agents/`, 2026-09-18).

### ETABS MCP (`HPEtabs/` — plan complete, phases 0–4 2026-09-16/17)

**Phase 2 (2026-09-17):** the writing runtime in `HPEtabs.McpBridge/Service/` — `EtabsTierTable` + embedded fixture `Resources/etabs-oapi-tiers.txt` (1 281 rows generated by `tools/generate-oapi-tier-fixture.ps1`, CHM index snapshot beside it), `EtabsTierAnalyzer` (semantic binding of every OAPI call to `cInterface.Member`, `PATH` refusals for unreadable path arguments), `EtabsPathPolicy`, `EtabsSnapshotManager` (presave/prerun `.EDB` copies under `%LocalAppData%\HPEtabs\McpBridge\snapshots\<model>\`, keep 5/10), `EtabsFingerprint` (add/delete counts), `EtabsUnitsPolicy`; `EtabsScriptRunner` runs the ADR-02 matrix; `EtabsExecutor` split into `.Audit` and `.Worker` partials. Tests `HPEtabs.McpBridge.Tests` 166 (fixture coverage, analyzer rows, policy, snapshot sequence on a temp folder, executor refusals without ETABS running). Harness phases `bridge` / `bridgedestructive` verified live ×2 on a throw-away model.

**Phase 3 (2026-09-17):** `HPEtabs.Mcp.Server/Registry/SeedLibrary/**` — 12 embedded seeds (8 read-only, 3 writes, 1 destructive `run_analysis`), `tools/list` = 24; `HPEtabs.Mcp.Server.Tests` 80 (structure 38 + compile/tier 25 against the installed `ETABSv1.dll`, skipped without it); harness phases `seeds` / `seedsdestructive` verified live ×3 including a real analysis and results reads.

**Phase 4 (2026-09-17):** harness phase `registry` (the registry loop live: propose → test → publish → CLI approve → `tools/list_changed` → quarantine → restore) and `run-live-verify.ps1 -Phase full -Publish` — everything on one registry root per run from the publish folders: **3 × 102 checks pass**; `reports/regression-tools-list-phase-04.py` proves the other hosts' phase-0 tools unchanged (Revit 33, Navis 24, AutoCAD 37 + 3 changed by the AEC plan itself). Known gaps and manual-only items in `CLAUDE.md`.

**Phase 0 complete (2026-09-16):** Engine constants for ETABS 22 are now additive in `McpShared/` — no `HPEtabs/` folder yet, no bridging code. Constants include `PipeNaming.EtabsHost` (pipe `hpetabs-mcp-22`), `JsonRpcMethods.EtabsPrefix`, `HostScriptContracts.Etabs{Imports, Globals, HeavyMaxTimeoutSeconds=600}`, `ContextResult.Etabs` with `EtabsInfo` (10 fields: attachment status, OAPI version, units, lock state, object counts), `ExecuteResult.Snapshot`, `GuardProfile.Etabs`/`AnalyzerProfile.Etabs`, `IHostProfile` hints (`BridgeNotConnectedHint`, `TimeoutSemanticsHint`) — all tested in 34 new tests (Revit/AutoCAD/Navisworks tool lists byte-identical before/after rebuild). **Phase 1 done (2026-09-17):** `HPEtabs/` exists — `HPEtabs.McpBridge` (standalone WPF app holding the COM attachment, STA worker, tier gate R/W/D, opt-in window), `HPEtabs.Mcp.Server` (profile version 22, 4 core tools), tests 17 + 26, harness; COM spike E9–E20 verified against ETABS 22 (see `plans/260916-2152-etabs-mcp-2026/reports/phase-01-spike.md`). Read-only scripts run; writing/destructive scripts are previewed/refused until phase 2 (snapshot); no seeds until phase 3.

## HPAutoCad Solution

`HPAutoCad/HPAutoCad.slnx` (11 projects) + global.json. Reference McpShared only; không dùng HPRebar. Hợp nhất cả 3 phân hệ trong một bundle duy nhất `HPAutoCad.bundle` (`%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\`): (1) MCP Bridge, (2) AEC Engine, và (3) HPGeoLink geodetic toolkit (Milestone M1–M5, 2026-09-20).

| Project | TFM | Vai trò | Build? |
|---|---|---|---|
| `HPAutoCad.Core/` | net8.0 | Pure domain geodetic library: Snyder TM-3 forward/inverse, Helmert 7-param, 64 province catalogs, KML/KMZ serialization, Web Mercator tile math, bicubic raster warper. Zero AutoCAD API dependencies | ✅ |
| `HPAutoCad/` | net8.0-windows | AutoCAD Add-In UI & feature layer (`HPGeoLink`): WPF MVVM (CommunityToolkit.Mvvm), commands (`HPGEO`, `-HPGEOKMZ`, `HPGEOIMPORT`, `-HPGEOIMPORT`, `-HPGEOIMAGE`, `HPGEOINFO`), CAD readers/writers, nod store, MaterialDesignThemes repacked | ✅ |
| `HPAutoCad.TileFetch/` | net8.0 console | Companion console utility tải tile bản đồ out-of-process (vượt hạn chế socket `WSAEACCES` của main thread) | ✅ |
| `HPAutoCad.Loader/` | net8.0-windows | Unified ALC Autoloader (nạp `HPAutoCad.McpBridge` vào `BridgeLoadContext`, `HPAutoCad` vào `AppLoadContext`); xây dựng Ribbon tab dùng chung `HPAUTOCAD_MCP_TAB` gồm 2 panel: MCP và HPGeoLink | ✅ |
| `HPAutoCad.Tests/` | net10.0-windows | xUnit v3 / MTP — 241 test (238 pass, 3 live-tile skipped): kiểm tra thuật toán trắc địa VN-2000 ↔ WGS84, golden fixtures, KML/KMZ, ViewModels, Loader reflection contract | ✅ |
| `HPAutoCad.McpBridge.Loader/` | net8.0-windows | IExtensionApplication loader của MCP bridge (giữ để tương thích Civil 3D mirror) | ✅ |
| `HPAutoCad.Aec/` | net8.0-windows | **AEC engine (2026-09-16, plan 260916-1140 phase A-I):** Geometry, Spatial, Issues, Model, Cad adapters, Classification, Relationships, BatchEdit, Standards QA/QC, Structural, Architecture (Rooms), Mep, Coordination, ChangeSets. ~11.5k LOC | ✅ |
| `HPAutoCad.Aec.Tests/` | net10.0-windows | xUnit v3 — 225 test: kiểm tra toàn diện module AEC (Geometry, Spatial, Classification, Standards, Structural, Rooms, MEP, Coordination, ChangeSets) | ✅ |
| `HPAutoCad.McpBridge/` | net8.0-windows, UseWPF | Bridge runtime: pipe `hpautocad-mcp-2026`, Roslyn 5.9 script compiler, MainThreadQueue, status window, opt-in toggle, MaterialDesignThemes repacked | ✅ |
| `HPAutoCad.Mcp.Server/` | net10.0 console | Stdio MCP server exe + `AutocadHostProfile` + 24 tools (4 core + 8 registry + 12 seeds). Registry `%AppData%\HPAutoCad\McpServer\` | ✅ |
| `HPAutoCad.Mcp.Server.Tests/` | net10.0-windows | xUnit v3 — 280 test: profile, lifecycle, fake pipe roundtrip, compile toàn bộ AEC seeds & standard seeds đối chiếu AutoCAD API | ✅ |

**Phase 2 additions:** `MainThreadQueue.cs`, `BridgeRequestException.cs`, `AutocadInsunits.cs`, `GuardProfile.AutoCAD` + deny `StartTransaction/StartOpenCloseTransaction/TopTransaction/LockDocument/ed.Get*`.

**Phase 4 additions:** Registry per host profile (`ToolValidator`, `ToolLifecycleService` on `IHostProfile`; categories, reserved names, host stamp from profile); 12 embedded seed tools (categories Drawing/Layer/Block/Annotation/Layout/Data/Generic); `RegistryToolText` host-neutral descriptions (8 engine tools); `SeedLibraryTests` 58 compile-checks + validate. Tests: McpShared 164, HPRebar MCP 109, AutoCAD tests: `HPAutoCad.Tests` 238 pass/3 skip, `HPAutoCad.Aec.Tests` 225 pass, `HPAutoCad.Mcp.Server.Tests` 280 pass.

**Phase 5 additions (harness & stability window):** `HPAutoCad/tools/harness/`: `run-live-verify.ps1`, `live-verify.py` (one stdio session through `McpShared/tools/mcp-session.py` since 2026-09-16, 65 scenarios isolated to ephemeral registry under `output/live-verify/`); matrix 18 (execute variants, guard, `cancel_execution`, busy → ESC → retry automated, timeout, audit), seeds 18 (real block, pickfirst), MISS → propose → test → publish → CLI approve (0.5 s `tools/list_changed`), fragile tool → quarantine → restore + newVersion (stays published on arg errors). Stability window: argument errors excluded, window restarts at lifecycle event so restored tool not re-quarantined. Live run 3: 64 pass + 1 skip + 4/4 isolation + 21/21 regression; xUnit 96/96 engine, 109/109 Revit server, 58/58 AutoCAD server (263 total).

**Ribbon tab reduced to the Revit-style surface (2026-09-16, bundle 0.3.0):** `HPAutoCad` ▸ `MCP` ▸ `MCP Bridge`; `run-ribbon-check.ps1` rewritten (tab once, workspace + COLORTHEME round trips with a screenshot per theme, button opens the window once) 12/12 + icon MANUAL; bridge 21/21, smoke 22/22. History — **Ribbon tab additions (plan 260914-2204, 2026-09-14):** Ribbon UI entry via `run-ribbon-check.ps1` (UI Automation tab finder via `AutomationId`, COM workspace switch, PowerShell 5.1 object binding); 8/8 verification (tab created once, survives workspace round trip, all 9 buttons drive pipe/window/registry, status label updates live, no crashes in logs); regress bridge 21/21 + server 22/22 (smoke now accepts approved tools ≥ 24). Bundle version bumped to 0.2.0; README.md included in Contents for guide access. Loader harness helpers in `harness-common.ps1` (+3 functions: `Find-RibbonTabs`, `Select-RibbonTab`, `Invoke-RibbonButton`); `run-server-smoke.ps1` updated (+5 lines for assertion tweak).

**Phase 3–4 harness & verification:** `run-bridge-unattended.ps1` 21/21 bridge scenarios (context without Revit fields, 12 tools/list unchanged with Revit exe, none/dryRun/real execute through pipe, get_run from registry.db). `run-server-smoke.ps1` 22/22 (every seed by name through `tools/list` + `search_tools` category filter + dryRun + layer round-trip). Registry separate `%AppData%\HPAutoCad\` vs `%AppData%\HPRebar\`. SECURELOAD auto-accept, UI Automation opt-in. Zero crashes, no `.NET Runtime 1026` event.

**Publish:** `dotnet publish HPAutoCad/HPAutoCad.Mcp.Server -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -p:IncludeNativeLibrariesForSelfExtract=true -o HPAutoCad/output/HPAutoCad.Mcp.Server` (7.4 MB). `.mcp.json` entry (user adds) → `hprebar-autocad` → that exe + env `HPAUTOCAD_MCP_Bridge__HostVersion=2026`.

**Theme (2026-09-19, MaterialDesign phase 4):** `HPAutoCad.McpBridge`, `HPCivil3d.McpBridge` (mirror, 7 file theme trong `mirroredFiles`, sha csproj re-pin) và `HPAutoCad` (trước đây là `HPGeo.AutoCad`, nay hợp nhất trong `HPAutoCad`) mỗi cái một bản copy `MaterialThemeBridge`/`IHostTheme`/`ThemeInfo`, `MaterialBridge.xaml` theo vocabulary riêng, palette `ThemeDark/ThemeLight.xaml`, host theme COLORTHEME (`AutocadHostTheme`/`Civil3dHostTheme`) và target `RepackMaterialDesign` — toolkit merge vào từng assembly add-in vì 2 bản rời trong 2 ALC của một acad.exe trộn nhau. `*ThemeSwitcher.cs` + `*ThemeLight.xaml` cũ xoá. HPGeoLink UI: `Resources/Themes/Theme.xaml` merge `MaterialBridge` trước, style re-base lên key toolkit, bỏ implicit `TextBox`/`ComboBox` (popup dark đúng — known gap đóng). Bằng chứng: `plans/260919-1910-materialdesign-xaml-adoption/reports/phase-04-autocad-family.md`.

**Runtime platform:** AutoCAD 2026 base release (R25.1, .NET 8) verified 2026-09-14. Harness in repo: `mcp-call.py`, `run-server-smoke.ps1`, `run-bridge-unattended.ps1`, `harness-common.ps1`.

`HPRebar.Tests` bị loại khỏi solution build có chủ đích: dưới config R23/R24 nó sẽ compile net8 rồi reference `HPRebar.dll` net48 → `CS0433 ReadOnlySpan<T> exists in both` (Polyfill nhúng span type vào assembly). Chạy riêng:

```bash
dotnet build HPRebar.Tests/HPRebar.Tests.csproj -c Debug.R26
```

## Ranh giới Core ↔ Revit

Nguyên tắc: **`HPRebar.Core` không bao giờ reference `Autodesk.Revit.*`**. `Document` là `sealed`, không mock được, nên toán phải tách ra mới test được bằng xUnit.

- Core làm việc bằng **millimét** (`double`).
- Chỉ 2 chỗ đổi đơn vị: [`RevitUnits`](../HPRebar/HPRebar/Column%20Rebar/RevitUnits.cs) (`MmToFt`/`FtToMm`).
- Đọc model → mm: `ColumnStackReader`. mm → Revit: `PointMapper`, `StirrupGeometry`.

ILRepack merge `HPRebar.Core.dll` vào `HPRebar.dll` khi deploy — thư mục add-in chỉ có 1 DLL.

## Feature: Column Rebar

77 file / ~6.5k dòng trong `HPRebar/HPRebar/Column Rebar/`, cộng 25 file / ~1.5k dòng Core.

Port từ `R01_ColumnsRebar` (~19.5k dòng, .NET 4.8, Revit 2021, MVVM tự viết). Plan: [`plans/260903-2307-port-column-rebar-to-hprebar/`](../plans/260903-2307-port-column-rebar-to-hprebar/plan.md).

### Luồng

```
ColumnRebarCommand.Execute
  PickObjects (StructuralColumnSelectionFilter)
  → sort theo cao độ mặt đáy
  → ColumnStackValidator.Validate        14 rule, trả lỗi ĐẦU TIÊN
  → ColumnStackReader.Read               → ColumnStack (mm)
  → DefaultRebarSpecBuilder.Build        spec mặc định
  → ColumnRebarSession + ViewModel + ColumnRebarView.ShowDialog()
       OK → RevitRebarRunner → ColumnRebarOrchestrator.Run
                                 ★ chủ sở hữu DUY NHẤT của TransactionGroup
                                 Tx "Create Detail View"
                                 Tx "Create Section View"
                                 Tx "Create Dimension View"
                                 Tx "Create Dimension Section"
                                 Tx "Create Stirrup Bars"
                                 Tx "Create Main Bars"
                                 Tx "Create Tag Bars"
                                 → Assimilate  (undo 1 bước)
       Cancel → không gọi Run → không có gì để rollback
```

### `HPRebar.Core/ColumnRebar/` — toán thuần

| File | Trách nhiệm |
|---|---|
| `BarLayoutCalculator` | vị trí thép chủ trên tiết diện (chữ nhật + tròn) |
| `BarSideClassifier` | số hiệu thanh → cạnh / phần tư |
| `DefaultOverlap` | nối chồng so le 35d / 70d |
| `SpliceCalculator` | tái phân bố thanh lên tiết diện trên |
| `DefaultUpperPositions` | lưới mặc định của tiết diện trên |
| `BarPolylineBuilder` | đường tim thanh (đáy → móc → bẻ xiên → neo) |
| `BarShapeClassifier` | đường tim → hình dạng uốn + chiều dài nhánh |
| `BarScheduleCalculator` | gom thanh giống nhau thành dòng bảng thống kê |
| `StirrupDistributionCalculator` | chia đai: đều, hoặc dày/thưa/dày |
| `CanvasScaleCalculator` | tỉ lệ vẽ preview |
| `Models/` (13 record/enum) | `ColumnSection`, `BarLayoutSpec`, `SpliceSpec`, `StirrupSpec`… |

### `Column Rebar/` — tầng Revit

| Nhóm | File |
|---|---|
| Đọc model | `ColumnSolidFaceReader`, `ColumnNeighbourFinder`, `ColumnStackReader` |
| Kiểm tra | `ColumnStackValidator` (14 rule), `ValidationMessages` |
| Tạo thép | `RebarTypeCatalog`, `RebarShapeResolver`, `PointMapper`, `MainBarCreator`, `StirrupGeometry`, `StirrupCreator`, `AdditionalTieCreator`, `RebarCreationService` |
| Tạo bản vẽ | `DetailViewCreator`, `SectionViewCreator`, `DimensionCreator`, `RebarTableTagCreator` |
| Điều phối | `ColumnRebarOrchestrator`, `RevitRebarRunner`, `ColumnRebarCommand` |
| UI | `View/` (9 XAML + code-behind ≤ 8 dòng), `View Models/` (13), `View/Controls/` (8 file vẽ canvas) |
| Khác | `RevitUnits`, `RevitDialogs`, `LocalizationService` |

### UI

MVVM Toolkit. `ColumnRebarSession` giữ state dùng chung, 8 tab đọc/ghi vào nó. Preview canvas gọi **đúng** calculator mà service tạo thép dùng — cái user thấy chính là cái sẽ dựng.

Theme: MaterialDesignInXAML 5.3.2 merge vào `HPRebar.dll` (ILRepack) + palette HP trong `Resources/Themes/` (`MaterialBridge.xaml` merge đầu tiên trong `Theme.xaml`, `ThemeDark/Light.xaml` là nguồn `Brush.*`), mọi màu/spacing qua `{DynamicResource}`. `Resources/Themes/MaterialThemeBridge.Attach(window, RevitHostTheme.Instance, icons => icons.X)` đổi palette + brush toolkit bằng **overlay top-level** (swap dictionary lồng nhau không repaint), set icon vector, theo `Application.ThemeChanged` sống. Gate off-Revit: `HPRebar/tools/theme-gallery/` + `HPRebar.Core.Tests/Themes/ThemeTokenCoverageTests`. Chi tiết: `CLAUDE.md` ▸ Theme.

i18n: `UiStrings` record ~110 field, `UiStringsCatalog.English`/`.Vietnamese`, đổi cả record một lần → mọi nhãn refresh.

## Feature: Kata Export

Xây lại tool pyRevit/Dynamo "Kata Export to Excel" (plan `plans/260926-2317-kata-export-hprebar/`). Tool đo một dải dầm thẳng trong Revit rồi ghi vào sheet `Dam` của workbook **Kata Pro** đang mở: B3:B10 và các hàng 11/19/21/22/23 từ cột C, 1 cột = 1 gối hoặc 1 nhịp. Hợp đồng từng ô nằm ở `reports/kata-cell-contract.md` của plan.

```text
Ribbon HPRebar ▸ Rebar ▸ Kata Export → KataExportCommand (selection | PickObjects)
  → Service: KataRunReader → KataSupportCollector (cột / vách / móng / dầm giao, cột trên)
             → KataGridReader → KataHeaderReader → KataExportSession (mm, trạm s dọc trục)
  → HPRebar.Core/KataExport: KataSegmenter (1D gối/nhịp/joint, console) → KataRowBuilder → KataSheet
  → Window: mặt đứng (KataElevationBuilder → KataElevationCanvas) + bảng cột C..BZ, chọn cột đồng bộ 2 chiều
    (Name/Count/Reverse tính lại pure) → KataExcelWriter (COM) → Highlight dầm (ExternalEvent)
```

- **Đo gối:** đường dò chạy dọc tim dầm ở 4 cao độ, cắt với solid của gối (`Solid.IntersectWithCurve`). Dầm được chọn và dầm giao dùng hình học gốc (`GetOriginalGeometry`) để không bị ảnh hưởng bởi join hay cut.
- **Excel:** dùng `oleaut32!GetActiveObject` và late binding `InvokeMember`, không thêm package. Quy tắc text/số của ô nằm trong `KataExcelCell` (core): B10 luôn là text `+3.300`; tên như `1-2` được ghi kèm tiền tố `'`.
- **Mặt đứng:** `KataElevationBuilder` (core) dựng hình từ chính sheet sẽ ghi — mỗi số vẽ là chữ của ô Kata, số cột và hàng 11 phải khớp sheet (sai chiều → từ chối); `KataElevationViewport` (core) lo zoom/pan 2 chiều kiểu CAD (lăn = phóng quanh con trỏ, chuột giữa hoặc Shift+kéo trái = pan, nhấp đúp = toàn dải), tỉ lệ đứng = max(tỉ lệ ngang, 60 px / h dầm) nên toàn dải vẫn đọc được và zoom sâu về 1:1; lệnh đóng khung co vừa chiều cao như ZOOM Extents. `View/Controls/KataElevationCanvas` vẽ bằng `OnRender` (painter + annotations, chữ không vừa thì bỏ), palette từ token `Brush.Canvas.*`, đổi theme qua DP `SurfaceBrush`. Chỉ hình học, không thép.
- **Test:** `HPRebar.Core.Tests/KataExport` gồm segmenter, row builder, ca biên, ô Excel, mặt đứng và viewport. Ảnh ngoài Revit: `HPRebar/tools/theme-gallery` (`KataElevationGallery`, 5 cảnh dark/light).
- **Live:** đã chạy 1 lần trên model BTCT thật, khớp phép đo độc lập (`reports/phase-06-live-verify.md`).

## Feature: Kata Rebar (MVP)

Chiều ngược của Kata Export: đọc sheet `Dam` của workbook Kata đang mở (COM, A1:BZ44) và vẽ thép cho **1 dầm 1 nhịp giữa 2 gối**: thép chủ B11/B12 (neo G2·d / G3·d, thẳng hoặc bẻ 90° ở mép xa gối), thép gia cường gối hàng 13–16 (`KataSupportTopBarLayout`: hàng 13 xen giữa thép chủ, 14–16 xếp lớp dưới, cắt H5/H3 × L, ô `trái;phải`), thép gia cường nhịp hàng 17–18 (`KataSpanBottomBarLayout`: thẳng, cắt L0/7 từ mép gối; hàng 18 xen giữa thép chủ dưới và ngồi trên đai theo Ø riêng, hàng 17 lớp trên khe max(25, d)), đai kín 3 vùng G7/G8 (G6), cover J9 (`a` tới tâm thép chủ / lớp bảo vệ đai). Plan `plans/260928-1259-kata-rebar-mvp/`, bảng ô + rule `reports/rule-table.md`.

```text
Ribbon HPRebar ▸ Rebar ▸ Kata Rebar → KataRebarCommand (selection) → cửa sổ modeless
  → Service/KataDamComReader → HPRebar.Core/KataRebar: KataDamSheetParser (+ KataStirrupSectionParser)
  → Service/KataBeamMatcher (KataRunReader + KataSupportCollector + KataSegmenter của Kata Export) → KataMeasuredBeam
  → Core KataRebarPlanner: KataSheetGeometryCheck (2/50 mm, xuôi/ngược) → số đo Revit thay số sheet
       → KataScopeFilter (Bỏ qua / Chặn theo địa chỉ ô) → KataDetailingRuleBuilder → KataRebarCalculator
  → Generate (đo lại + plan lại) → KataRebarOrchestrator: TransactionGroup "Kata Rebar - {tên}" =
       xoá thép cũ theo tag → KataStirrupSetCreator (bộ đai M_T1 + KataStirrupCoverFit) → thép chủ (CreateFromCurves)
```

- **Tag:** `Comments = HPRebar_Kata:{UniqueId dầm}`; chạy lại chỉ xoá thanh mang đúng tag, thanh vẽ tay không bị đụng.
- **Bộ đai:** Revit ép bộ đai shape-driven về cover của host khi regenerate → `KataStirrupCoverFit` đặt lại khoảng cách tới cover từng cạnh = cover host − lớp đai sheet; kiểm đai đầu (hộp out-to-out ±3 mm) → lệch thì đai lẻ từ curves.
- **Chưa vẽ (báo "Bỏ qua"):** giật cấp 19/21, cốt giá G4/G5 + hàng 20, đai trong hàng 25–44, bước đai riêng 22/23, hàng 24. **Chặn:** ≠ 1 nhịp, console, > 1 dầm, gối là dầm.
- **Test:** `HPRebar.Core.Tests/KataRebar` (planner golden = live check). **Live:** Revit 2026.4 trên model nháp, 4 lần chạy + 3 case âm khớp golden (`reports/phase-live-verify.md`). R25/R24 chỉ compile.

### Kata Export round trip (2026-09-29)
Cửa sổ Kata Export làm trọn vòng: Xuất Excel → user nhập thép sheet Dam → **Đọc thép Excel** (COM, workbook đang kích hoạt) → `KataRebarWorkflow.Prepare` trong ExternalEvent (đo dầm + plan với `KataSettings`) → canvas mặt đứng (`KataElevationRebarPainter`, trạm qua `KataStationMap`) + mặt cắt cột đang chọn (`KataElevationSectionPainter`) → **Tạo thép Revit** (`KataRebarWorkflow.Generate`). Cửa sổ Kata Rebar riêng giữ nguyên, dùng cùng service.
- Nhiều nhịp / nhiều dầm Revit; gối giữa 2 vế khác: so le Y, vế diện tích lớn qua gối bẻ móc mép xa (chân dừng cách thép dưới 1 khe), vế nhỏ thẳng + G2·d; thép chủ liên tục, cảnh báo > `MaxBarLength`.
- Cốt giá G4/G5 (G5 âm = không móc C) + hàng 20 (`2f12` = 1 lớp, `0` = không), neo `SideBarAnchorageFactor`·d (gối giữa ≤ nửa gối); móc C 180° @`SideBarTieSpacing`.
- Đai trong hàng 25–44, cặp cột gối|nhịp k → nhịp k: □ a-b, U a-b (mở trên), C a (đứng); theo vùng đai ngoài +d_đai; hàng 22 bước đai riêng. Tất cả là `KataBarSet` → `KataBarSetCreator` (CreateFromCurves + RebarHookType theo góc, SubTransaction từng bộ).
- `KataSettings` (JSON tự viết `KataSettingsJson`, `%AppData%\HPRebar\KataSettings.json`): max 11700, móc □ 135° 7.5d, C 180° 7.5d, làm tròn cắt gia cường 50, neo cốt giá 10d, móc C a400. Bỏ qua (báo ô): giật cấp 19/21, console G9, hàng 23/24, J7.
- Revit 2026: "Right" của móc tính theo hướng thanh ở cả hai đầu (đo live). Bản cài KataOnly: `tools/build-kata-installer.ps1` (`-p:KataOnly=true`, chung upgrade code với HPRebar).
- Plan `plans/260929-0020-kata-export-rebar-roundtrip/`, live `reports/phase-07-live-verify.md`.

## Multi-version

2 block `#if` trong toàn bộ codebase, cả hai có comment `// Multi-version:`:

| Vị trí | Lý do |
|---|---|
| `MainBarCreator.cs` | `Rebar.CreateFreeForm`: overload `out RebarFreeFormValidationResult` bị xóa ở R27; overload `RebarStyle` chỉ có từ R26. Khác cả return type |
| `Resources/Themes/RevitHostTheme.cs` | `UIThemeManager` chỉ có từ R24; R23 mặc định Dark |

Chi tiết đối chiếu API: [`plans/260903-2307-…/reports/api-surface-check.md`](../plans/260903-2307-port-column-rebar-to-hprebar/reports/api-surface-check.md).

## Lệnh

Mỗi thư mục có `global.json` pin runner, nên test chạy từ trong thư mục đó:

```bash
# HPRebar
cd HPRebar
dotnet build HPRebar.slnx -c Debug.R26          # chính (máy dev có Revit 2026)
dotnet build HPRebar.slnx -c Debug.R23          # net48, bắt lỗi TFM sớm
dotnet test HPRebar.Core.Tests                  # 334 test xUnit
dotnet test HPRebar.Mcp.Server.Tests            # 106 test xUnit — registry per profile, 21 Revit seed thực
dotnet build HPRebar.Tests/HPRebar.Tests.csproj -c Debug.R26   # TUnit, cần Revit

cd build && dotnet run                          # Release cả 5 config
cd build && dotnet run -- pack                  # Bundle → output/

# McpShared
cd McpShared
dotnet test HPRebar.Mcp.Server.Core.Tests       # 95 test xUnit — host-neutral engine, registry per profile

# HPAutoCad
cd HPAutoCad
dotnet build HPAutoCad.slnx -c Release             # deploy bundle → %AppData%\Autodesk\ApplicationPlugins\
dotnet build HPAutoCad.slnx -c Release -p:DeployBundle=false   # khi AutoCAD đang mở (DLL khóa)
dotnet test HPAutoCad.Tests/HPAutoCad.Tests.csproj # 238 passed (3 live-tile skipped) — geodetic & UI tests
dotnet test HPAutoCad.Aec.Tests/HPAutoCad.Aec.Tests.csproj # 225 passed — AEC engine tests
dotnet test HPAutoCad.Mcp.Server.Tests/HPAutoCad.Mcp.Server.Tests.csproj # 280 passed — MCP server & seed checks
```

**Revit đang mở sẽ khóa DLL đã deploy** → thêm `-p:DeployAddin=false` khi chỉ cần verify compile.

## Trạng thái verify

### Revit
| Version | Build | Runtime |
|---|---|---|
| 2023, 2024 | ✅ | ❌ không cài trên máy dev |
| 2025 | ✅ | ⚠️ chưa chạy |
| 2026 | ✅ | ⚠️ chưa chạy |
| 2027 | ✅ | ❌ không cài |

**Add-in rebar chưa có phiên bản nào được verify runtime.** 16 TUnit test đã viết nhưng skip hết vì thiếu model mẫu — xem `HPRebar/HPRebar.Tests/Fixtures/README.md`.

**MCP bridge** thì khác: đã chạy end-to-end trong Revit 2026 ngày 2026-09-12 (self-check Roslyn, `get_revit_context`, `inspect_type`, 15 kịch bản `execute_revit_code` gồm dryRun/commit/exception rollback/cancel/timeout).

### AutoCAD
| Version | Build | Runtime |
|---|---|---|
| 2026 | ✅ | ✅ 2026-09-14 (phases 1–4: 22/22 smoke harness all 12 seeds, 21/21 bridge scenarios, 58/58 tests, registry per profile, zero crashes) |
| 2027 .NET 10 | ❌ scaffold chưa test | ❌ |

## Ngoài add-in — tooling Python

Không thuộc solution `.slnx`, không ảnh hưởng build Revit.

| Đường dẫn | Vai trò | Chạy bằng |
|---|---|---|
| `scripts/skill_sync/` + `tests/skill-sync/` | Đồng bộ config agent `.claude` ↔ `.agents` ↔ `.codex` | system Python |
| `scripts/notebooklm_client.py` | Wrapper mỏng quanh `notebooklm-py` — import lười, nên hàm thuần chạy được ở mọi interpreter | `scripts/.venv` |
| `scripts/build-notebooklm-course-assets.py` + `notebooklm-sources.json` | Nạp docs Revit lên NotebookLM, sinh học liệu về `output/notebooklm/`. Idempotent theo hash | `scripts/.venv` |
| `tests/notebooklm/` | 24 test `unittest`, không chạm mạng | system Python (cố ý — chứng minh không kéo `notebooklm` ở top level) |
| `scripts/generate_revit_api_infographics.py` | Sinh infographic khoá học (one-off) | system Python |

`.mcp.json` khai báo MCP server `notebooklm` (38 tool). Chi tiết: [`notebooklm-integration.md`](notebooklm-integration.md).

## MCP bridge — Revit làm runtime cho AI

Thiết kế gốc: `plans/260912-1521-dynamic-revit-mcp-server-2026/architecture.md` + `adr/`. Hai tiến trình: host AI launch server exe làm child; server không reference Revit; bridge không reference MCP SDK; cầu nối chỉ qua `HPRebar.Mcp.Contracts`.

```
Claude Code ──stdio──▶ HPRebar.Mcp.Server (net10 console) ──named pipe hprebar-mcp-r2026 (JSON-RPC 2.0, 1 object/dòng)──▶ PipeListener (McpShared)
  (any AI host)         (thin exe: Program.cs một dòng)      guard/compile pipe thread                               ↓
                        + 21 seed nhúng                      ScriptGuard → ScriptCompiler (cache SHA-256)             ExternalEvent → Revit thread
                        + Registry engine (McpShared)        revit.analyze (no Revit needed)                           ↓
                                                                                                        McpBridgeExternalEventHandler → TransactionGroup → Revit API
```

| Thành phần | Ở đâu | Vai trò |
|---|---|---|
| **Hợp đồng dây** | | |
| `JsonRpcEnvelope`, `ExecuteRequest/Result`, `SafeText`, `SynchronousProgress` | `McpShared/HPRebar.Mcp.Contracts/` | Envelope strip path, forward progress đúng thứ tự |
| **Host-neutral engine** | | |
| `PipeListener`, `RequestDispatcher`, `IBridgeExecutor` | `McpShared/HPRebar.McpBridge.Core/Pipe/` | Listener 1 instance, ACL user, error code (`-32001`/`-32002`/`-32003`) |
| `ScriptGuard`, `ScriptCompiler`, `ScriptCache`, `TypeInspector`, `ScriptArgs`, `ScriptAnalyzer` | `McpShared/HPRebar.McpBridge.Core/Scripting/` | Deny-list walker, Roslyn cache LRU 50, reflection Revit API; `args` typed; literal/key walker cho `revit.analyze` |
| `BridgeSettingsStore`, `AuditLogger` | `McpShared/HPRebar.McpBridge.Core/Model/` | Settings per product (`%AppData%\<Product>\McpBridge`), audit JSON-lines |
| `IHostProfile`, `HostProfile`, `BridgeOptions`, `Host/*` | `McpShared/HPRebar.Mcp.Server.Core/` | Chọn executor (Revit/AutoCAD/khác), ProductFolder, HostVersion |
| `RegistryEngine`, `ToolRegistry`, `ToolRecord`, registry CLI | `McpShared/HPRebar.Mcp.Server.Core/Registry/` | SQLite WAL+FTS5, files = source, tool lifecycle, auto-quarantine |
| **Revit-cụ thể** | | |
| `RevitHostProfile`, `ExecuteRevitCodeTool`, `RevitContextTool` | `HPRebar/HPRebar.Mcp.Server/Hosts/Revit/` | Revit profile impl, 4 core tool + seed registry |
| `RevitBridgeClient`, `NdjsonPipeTransport`, `ResultFormatter` | `HPRebar.Mcp.Server/Services/` (mapped từ Core) | Ghép id↔response, timeout, reconnect backoff |
| `McpBridgeExternalEventHandler`, `ScriptRunner`, `RevitContextReader` | `HPRebar/HPRebar.McpBridge/` | Chỉ phần này chạm Revit API |
| `McpBridgeStatusView`, `McpBridgeStatusViewModel` | `HPRebar/HPRebar.McpBridge/View|ViewModel/` | Cửa sổ modeless, opt-in listener, last run |

Chính sách transaction: `auto` (bridge mở Transaction trong Group) · `manual` (script tự mở) · `none` (chỉ đọc) · `dryRun` luôn rollback. Timeout 5–120 s cooperative qua `ct`; hết hạn = thất bại + rollback kể cả khi script `return`. Không sandbox — 9 lớp phòng thủ (ADR-04).

Client: `.mcp.json` (untracked) entry `hprebar-revit` → `HPRebar/output/HPRebar.Mcp.Server/HPRebar.Mcp.Server.exe` (từ `dotnet publish … -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true`). Bridge cần bật listener (auto-start theo `settings.json`) và tick "Allow AI code execution" mỗi phiên Revit.

Registry (ADR-05/06): tool = `tool.json + code.cs + examples.json` ở `%AppData%\HPRebar\McpServer\tools-library\` (đổi qua `Registry:LibraryPath`); `registry.db` giữ lịch sử chạy + độ ổn định; policy `manual` — AI dừng ở `pending_approval` + `_review/<name>.md`, người duyệt bằng `HPRebar.Mcp.Server.exe registry approve <name> --by <ai>`; server đang chạy nhận thay đổi qua watcher (không restart). Đã verify live 3 kịch bản ngày 2026-09-12 (`plans/…/reports/phase-09-live-verify.md`).

## HPNavis Solution

`HPNavis/HPNavis.slnx` + global.json + `Directory.Build.props` (API lấy từ bản Navisworks cài trên máy — không NuGet). Reference McpShared only. Phases 0–5 (2026-09-15) verified live trong Navisworks Manage 2026 (23.0.1432.76). Chi tiết: mục "HPNavis MCP Bridge" trong `CLAUDE.md`, plan `plans/260915-0824-navisworks-mcp-2026/`.

| Project | TFM | Vai trò |
|---|---|---|
| `HPNavis.McpBridge/` | net48 | Plugin trong Roamer.exe: `HPNavisBridgePlugin` (EventWatcher: listener + `NavisMainThreadExecutor`) + `HPNavisRibbonPlugin` (CommandHandler: Ribbon tab HPNavis ▸ MCP ▸ MCP Bridge → status window; `Ribbon/en-US/*.xaml|.name`, `Ribbon/Images/*.png`) + `HPNavisWindowPlugin` (Add-in ẩn `AddInLocation.None`, cho `ExecuteAddInPlugin` WPF). `PluginAssemblyResolver` (allow-list + version family cho Roslyn 5.9/Immutable 10 trên .NET Framework), `NavisScriptRunner` + `NavisUndoDecision` (commit rồi `Rollback()` chỉ khi `NextUndo` là entry của mình), `NavisHeavyGate` (opt-in thứ hai, path policy, ceiling 600 s), `NavisQuiescence`, `NavisChangeCounter`, `NavisResultSerializer` (`BoundedOutputStream`, collection cap 200), `ScriptingSelfCheck` |
| `HPNavis.Mcp.Server/` | net10.0 console | `NavisHostProfile` (HostId `navis`, 600 s, categories Model/Search/Selection/Viewpoint/Clash/Timeliner/Report/Data/Generic), `execute_navis_code`/`get_navis_context`, resources `navis://document/info`, `navis://selection`, prompts `navis_query_template`/`navis_review_template`, 12 seed nhúng (`Registry/SeedLibrary/**`) → 24 tools |
| `HPNavis.McpBridge.Tests/` | net48 | xUnit v3 — 124 test: heavy gate, undo decision, change counter, serializer bound (pull-count), resolver, mọi seed compile qua `BridgeEntry.CreateScriptCompiler` (import/reference/globals thật của bridge) |
| `HPNavis.Mcp.Server.Tests/` | net10.0 | xUnit v3 — 49 test: profile, tool surface, tools qua pipe thật với fake executor, cấu trúc seed record dưới profile Navis |
| `tools/harness/` | PowerShell 5.1 + Python | `run-bridge-unattended.ps1` (43 check/run), `run-server-smoke.ps1` (9), `run-seeds-live.ps1` (18 + 2), `run-live-verify.ps1` + `live-verify.py` (phase 5: 62 pass on runs 2–4, run 1 59 + 1 harness-assertion fail, import `McpShared/tools/mcp-session.py`) |

**Sự thật load-bearing:** Navisworks 2026 chạy .NET Framework 4.8 → không có AssemblyLoadContext; `HPRebar.Mcp.Contracts` phải có asset `net48` thật (asset netstandard tham chiếu System.Text.Json 10.0.0.0 không bind được với package asset net462 10.0.0.12). `Application.Idle` im lặng dưới native modal → `MainThreadQueue(expireWithoutTicks: true)` hết hạn busy grace bằng timer. Roamer khởi động qua Automation API tự thoát sau ~15 s trên máy dev → harness chạy `Roamer.exe "<model>"` trực tiếp với `HPNAVIS_MCP_BRIDGE_SHOW_WINDOW=1`.
