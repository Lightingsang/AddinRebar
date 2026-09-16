# HPEtabs MCP 2026 — Architecture

Ngày 2026-09-16 · planned · revised sau red-team 2026-09-16 · nguồn: [evidence E1–E8](research/evidence-on-machine-2026-09-16.md), ADR-01…05, [red-team](reports/red-team-2026-09-16.md). Mọi khẳng định OAPI không kèm nguồn = `[chưa xác minh]`.

## 1. Năm folder top-level, chiều phụ thuộc một chiều

```
HPRebar/   (Revit add-in + Revit MCP)   ──┐
HPAutoCad/ (AutoCAD MCP + AEC engine)    ──┼──▶ McpShared/  (HPRebar.Mcp.Contracts netstandard2.0;net48 · HPRebar.McpBridge.Core net8.0;net48 · HPRebar.Mcp.Server.Core net10)
HPNavis/   (Navisworks MCP)              ──┤        ▲ không bao giờ tham chiếu Autodesk.*/ETABSv1; host vào qua IHostProfile / IBridgeExecutor / GuardProfile / AnalyzerProfile
HPEtabs/   (ETABS MCP — plan này)        ──┘
```
Không MCP → MCP. `HPEtabs/` chỉ `ProjectReference ..\McpShared\*` (mirror `HPAutoCad/HPAutoCad.slnx` `/Shared/`); không `HPRebar/`, `HPAutoCad/`, `HPNavis/`, `HPCivil3D/` (không tồn tại hôm nay).

## 2. Ba tiến trình — bridge là app của user, không phải add-in

```
Claude Code ──stdio──▶ HPEtabs.Mcp.Server.exe (net10, single-file)   ──pipe hpetabs-mcp-22, JSON-RPC NDJSON──▶ HPEtabs.McpBridge.exe (net8.0-windows, WPF, publish FOLDER, user tự mở)  ──COM out-of-proc (oleaut32 GetActiveObject)──▶ ETABS.exe (LocalServer32, .NET 8)
                       McpServerHost.RunAsync(args, EtabsHostProfile)                                        McpBridgeHost + PipeListener + RequestDispatcher("ETABS", "etabs.", executionDisabledMessage)          cOAPI → cSapModel (ETABSv1.dll 2.10, netstandard2.0)
                       4 core · 8 registry · 12 seed · etabs:// · prompts · hints (not-connected / timeout)     EtabsExecutor : IBridgeExecutor — STA foreground worker; control lane Attach/Detach; liveness Process.Exited
                       registry %AppData%\HPEtabs\McpServer\                                                 ScriptGuard(Etabs) → ScriptCompiler → EtabsTierAnalyzer (fixture R/W/D, semantic) → units kN_mm_C → [W/D: audit started → snapshot] → script → fingerprint → restore
                       KHÔNG tham chiếu ETABSv1.dll                                                          → EtabsResultSerializer → ExecuteResult (+Snapshot tên file) → audit %AppData%\HPEtabs\McpBridge\audit\
```
Khác 3 host cũ: engine bridge chạy trong tiến trình của ta — tick của `MainThreadQueue` là vòng lặp STA worker; không Idle event của host.

## 3. Bố cục `HPEtabs/`

```
HPEtabs/
├── HPEtabs.slnx · global.json (verbatim: sdk 10.0.300, MTP) · Directory.Build.props (EtabsInstallDir: env → CLSID LocalServer32 → ProgramW6432) · README.md · .gitignore
├── HPEtabs.McpBridge/            net8.0-windows, UseWPF, WinExe, <Reference ETABSv1 Private=false>, <Error> khi thiếu — App.xaml, BridgeEntry, EtabsExecutor(+Audit), Service/{EtabsAssemblyResolver, EtabsApiLocator, EtabsAttachment, EtabsTierAnalyzer, EtabsPathPolicy, EtabsSnapshotManager, EtabsFingerprint, EtabsScriptRunner, EtabsResultSerializer, EtabsContextReader, ScriptingSelfCheck}, Model/EtabsScriptGlobals, Resources/etabs-oapi-tiers.txt, View/EtabsBridgeStatusView, ViewModel/EtabsBridgeStatusViewModel
├── HPEtabs.McpBridge.Tests/      net8.0-windows, xunit v3 — CẦN ETABS 22 cài (bind ETABSv1.dll thật); ≥ 25 test, 0 skip
├── HPEtabs.Mcp.Server/           net10 stdio — Program.cs (1 dòng), Hosts/EtabsHostProfile.cs, Tools/, Prompts/, Resources/, Registry/SeedLibrary/**, appsettings.json
├── HPEtabs.Mcp.Server.Tests/     net10 — profile, pipe round trip (FakeRevitExecutor linked), SeedLibraryCompileTests (ETABSv1.dll tìm lúc test → Assert.SkipWhen), SeedLibraryStructureTests; build/test mọi máy
├── tools/harness/                live-verify.py (--phase spike|bridge|seeds|full) · run-live-verify.ps1 · harness-common.ps1 · README.md (import ../../../McpShared/tools/{mcp-session.py, harness_common.py})
├── tools/generate-oapi-tier-fixture.py   one-off: CHM index → fixture
└── output/                       HPEtabs.Mcp.Server (single-file) · HPEtabs.McpBridge (folder) · live-verify/{registry, model.EDB 👤}
```

## 4. Vòng đời `execute_etabs_code` (tier quyết định trước khi chạy)

1. Server: clamp timeout tới `profile.MaxTimeoutSeconds` (600 — `ExecuteCodeService.cs:61`) → `etabs.execute`.
2. Dispatcher: opt-in OFF → `-32001` (text `executionDisabledMessage`); busy → `-32002` (`RequestDispatcher.cs:132–137`).
3. Executor (pipe thread): `ScriptGuard.Check(code, GuardProfile.Etabs)` → compile → **`EtabsTierAnalyzer`** trên semantic model: mọi member ETABSv1 tra fixture (R/W/D; không có/không bind → D); tier = max.
   - `none`/`dryRun` mà tier ≥ W → **static preview**: `ExecuteResult{isError=true, rolledBack=true, Diagnostics[PREVIEW: members]}` — không chạy.
   - Tier D mà checkbox OFF → **JSON-RPC `-32001`** "Destructive operations are disabled — tick 'Allow destructive operations'…" (không thành run của registry).
   - Member path-taking: đối số phải là literal hoặc `args.Str("k")`; literal + mọi string trong `args` qua path policy (UNC, thư mục bridge/cài, prefix model dir | `%LocalAppData%\HPEtabs\`).
4. Enqueue `MainThreadQueue` (grace 8 s, `expireWithoutTicks`; quiescent = `!Attached || (!running && IsWindowEnabled)`) → worker tick: `!Attached` → `-32003` "not attached"; không model → `-32003`.
5. Worker (budget bắt đầu): units save/set; fingerprint trước; **W/D:** không file path / UNC model → `-32003`; audit `started` ("forced save"); `-presave` nếu file trên đĩa không do bridge ghi; `File.Save()`; copy `prerun\<ts>-<label>.EDB`; chạy script (`ct`); fingerprint sau; `finally` restore units.
6. Kết quả: R drift → `changed` + `Logs` warning (`isError=false`); W/D throw → `rolledBack:false` + `Snapshot` (tên file); timeout → fail (worker bận tới khi call trả); `Changed` = add/delete only.
7. Audit; `LastRunInfo` lên cửa sổ; `ExecuteResult` → server → registry `runs`.

## 5. Bảng mã lỗi / diagnostic

| Code | Khi | Message | Nguồn |
|---|---|---|---|
| -32001 | execution OFF | "…tick 'Allow AI code execution' in the HPEtabs MCP Bridge window (a separate app, not inside ETABS)." | `RequestDispatcher.cs:132–134` + ctor param (phase 0) |
| -32001 | tier D, destructive OFF | "Destructive operations are disabled — tick 'Allow destructive operations' in the HPEtabs MCP Bridge window" | `BridgeRequestException(ExecutionDisabled, …)` host-side |
| -32002 | busy > 8 s / modal / call kẹt | `BridgeRequestException.Busy("ETABS")` | `BridgeRequestException.cs:21` |
| -32003 | không model | `NoActiveDocument("ETABS", "model (.EDB)")` | `:24` |
| -32003 | chưa attach / ETABS đóng (liveness) | "ETABS not attached — click Attach in the HPEtabs MCP Bridge window" | ctor public `:13` |
| -32003 | W/D: model chưa có file path / trên UNC | "model has no file path — save it in ETABS first" / "model is on a UNC share — copy it locally first" | ctor public `:13` (ADR-02 §3) |
| bridge vắng (server) | pipe không có | `HostProfile.BridgeNotConnectedHint` — nêu `HPEtabs.McpBridge.exe`, Attach, pipe | `RevitBridgeClient.cs:139` (phase 0) |
| timeout (server) | không reply | `HostProfile.TimeoutSemanticsHint` — "changes before the timeout persisted (no rollback)" | `RevitBridgeClient.cs:85` (phase 0) |
| diag `GUARD` | guard | "… is not allowed in ETABS scripts." | `ScriptGuard.cs:60` |
| diag `PREVIEW` | `none`/dryRun với tier ≥ W; `analyze` với `transaction:none` + W | "would call … — nothing ran" | mới (ADR-02 §1) |
| diag `DESTRUCTIVE` | path policy / đối số path không phải literal\|`args.Str` | "path arguments must be a literal or args.Str(\"key\"); no UNC" | mới (ADR-02 §4) |

## 6. Khác gì ba host cũ

| | Revit 2026 | AutoCAD 2026 | Navisworks 2026 | **ETABS 22** |
|---|---|---|---|---|
| Bridge chạy ở đâu | add-in trong Revit.exe | bundle trong acad.exe (ALC) | plugin trong Roamer.exe (net48) | **app WPF riêng** (publish folder), COM out-of-proc |
| Marshal | `ExternalEvent` | `Application.Idle` | `Application.Idle` + quiescence | **STA worker của ta**; control lane attach; liveness `Process.Exited` |
| Rollback | `TransactionGroup.RollBack()` | `Transaction.Abort()` | `Commit()` rồi `Rollback()` có điều kiện | **không** — snapshot `.EDB` vô điều kiện trước W/D (forced save); R = allow-list + fingerprint; preview không chạy |
| Opt-in thứ hai | — | — | heavy (`HEAVY`, 600 s) | destructive (`-32001` khi OFF, 600 s) |
| Đơn vị API | feet | INSUNITS | `Document.Units` | ép `kN_mm_C` mỗi run, restore |
| Ghi được | geometry + params | geometry + tables | metadata review | W = mọi member ETABSv1 ngoài allow-list R và D; D = path-taking + Start/Modify/Merge/Reset/Clear/Rename/Show/Export/Import/Replicate + lock/analysis/file/delete |
| Deploy | `.addin` | bundle | thư mục Plugins | **không cài gì** — chạy exe |
| API ref | NuGet | NuGet | thư mục cài (registry Autodesk) | thư mục cài qua CLSID `LocalServer32`; runtime `AssemblyLoadContext.Resolving`; bridge tests cần ETABS; seed compile-check skip quan sát được |
| Version key | năm | năm | năm | **22** |
| Pipe | `hprebar-mcp-r2026` | `hpautocad-mcp-2026` | `hpnavis-mcp-2026` | `hpetabs-mcp-22` |
