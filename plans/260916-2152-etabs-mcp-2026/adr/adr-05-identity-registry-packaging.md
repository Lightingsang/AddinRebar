# ADR-05 — Định danh, registry, đóng gói (ngắn)

**Ngày:** 2026-09-16 · **Revised 2026-09-16 (red-team #7, #9, #13):** bridge publish folder; hint text qua profile; bỏ `AutoLaunchBridge` · **Status:** Proposed · **Owner:** HPEtabs
**Kế thừa:** [Navis ADR-05](../../260915-0824-navisworks-mcp-2026/adr/adr-05-navis-plugin-packaging-deploy-identity.md) · `HPNavis/HPNavis.Mcp.Server/Hosts/NavisHostProfile.cs:20–44` · `HPAutoCad/HPAutoCad.McpBridge/BridgeEntry.cs:32–33,66` (`new BridgeSettingsStore(VendorFolder, ProductFolder)`)
**Bằng chứng:** [evidence §E1](../research/evidence-on-machine-2026-09-16.md) (ETABS **22** v22.7.0.4095 — CSI đánh số, không phải năm) · `McpShared/HPRebar.Mcp.Contracts/PipeNaming.cs:40` (nhánh mặc định `hp{host}-mcp-{version}`) · `McpShared/HPRebar.Mcp.Server.Core/Hosts/HostProfile.cs:14–35` (mẫu record)

## Context
Ba host cũ dùng năm (2026); ETABS dùng số phiên bản (22). Registry engine per-host đã data-driven qua `IHostProfile` (researcher-02 §4g–§4i; `ToolValidator`, `ToolLibraryStore` dùng `profile.ProductFolder/Categories/CoreToolNames`). Không có gì cài vào ETABS (ADR-01) → đóng gói = 2 exe publish.

## Decision

### 1. Định danh (một bảng, mọi phase trỏ về đây)
| Khoá | Giá trị |
|---|---|
| HostId / `PipeNaming.EtabsHost` | `etabs` |
| HostVersion | **22** — `DefaultVersion 22`, `ValidVersions [22]`; env `HPETABS_MCP_Bridge__HostVersion=22` (👤 user xác nhận, plan.md) |
| Pipe | `hpetabs-mcp-22` = `PipeNaming.For("etabs", 22)` — nhánh mặc định đã đúng (`PipeNaming.cs:40`); thêm hằng `EtabsHost` + `case` tường minh như Navis |
| MethodPrefix / `JsonRpcMethods.EtabsPrefix` | `etabs.` (`etabs.execute/context/inspect/analyze/cancel/ping`; notifications `etabs.progress/log/status`) |
| DisplayName / ServerName | `ETABS` / `HPEtabs MCP` |
| ProductFolder / EnvPrefix | `HPEtabs` / `HPETABS_MCP_` |
| Core tools | `execute_etabs_code` (Destructive), `get_etabs_context` (ReadOnly), `inspect_type`, `cancel_execution` — = reserved names |
| Resources / prompts | `etabs://model/info`, `etabs://selection`, `registry://tools[/{name}]`; `etabs_query_template`, `etabs_modify_template`, `toolify_run` |
| Categories | `Model, Geometry, Property, Load, Analysis, Results, Table, Data, Generic` |
| CLI | `HPEtabs.Mcp.Server.exe registry list|pending|show|approve <name> --by <who>|reject|deprecate|quarantine|restore|stats|export|import` |
| MaxTimeoutSeconds | 600 (= `HostScriptContracts.EtabsHeavyMaxTimeoutSeconds`) |
| BridgeNotConnectedHint / TimeoutSemanticsHint (phase 0, additive) | "Start HPEtabs.McpBridge.exe beside ETABS 22, click Attach and tick 'Allow AI code execution' (pipe hpetabs-mcp-22)" / "ETABS may still be running the call; changes made before the timeout persisted (no rollback) — check the snapshot named in the bridge window before retrying" |
| executionDisabledMessage (bridge ctor) | "Code execution is disabled. Ask the user to tick 'Allow AI code execution' in the HPEtabs MCP Bridge window (a separate app, not inside ETABS)." |
| Registry root | `%AppData%\HPEtabs\McpServer\` (`registry.db` + `tools-library\<Category>\<name>\`), relocatable `HPETABS_MCP_Registry__LibraryPath` / `__DbPath` |
| Bridge settings | `%AppData%\HPEtabs\McpBridge\settings.json` (`AutoStartListener` only; `ExecutionEnabled` bị strip — `BridgeSettingsStore.cs:64`) — store tạo host-side `new BridgeSettingsStore("HPEtabs", "McpBridge")` như AutoCAD/Navis, **không** thêm static vào Core |
| Audit / logs / snapshots | `%AppData%\HPEtabs\McpBridge\audit\audit-YYYYMMDD.log` · `%LocalAppData%\HPEtabs\McpBridge\logs\` (Serilog, `shared: true`) · `%LocalAppData%\HPEtabs\McpBridge\snapshots\<model>\` |
| Bridge window title | `HPEtabs MCP Bridge` (attached pid + model; warning khi > 1 ETABS; text cạnh checkbox 1: "writing scripts save the model first") |
| Assembly names | `HPEtabs.McpBridge` (exe WPF), `HPEtabs.McpBridge.Tests`, `HPEtabs.Mcp.Server` (exe), `HPEtabs.Mcp.Server.Tests` |

### 2. `.mcp.json` (user thêm, untracked; **không** đụng `hprebar-revit`/`hprebar-autocad`/`hprebar-navis`)
`hprebar-etabs` → `HPEtabs/output/HPEtabs.Mcp.Server/HPEtabs.Mcp.Server.exe`, env `HPETABS_MCP_Bridge__HostVersion=22`. Exe bị lock khi một phiên Claude Code đang chạy nó → publish lại sau khi tắt (known gap chung 3 host).

### 3. Publish (README)
```
dotnet publish HPEtabs/HPEtabs.Mcp.Server -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -o HPEtabs/output/HPEtabs.Mcp.Server
dotnet publish HPEtabs/HPEtabs.McpBridge  -c Release -r win-x64 -p:SelfContained=false -o HPEtabs/output/HPEtabs.McpBridge
```
Server: single-file (như 3 host). **Bridge: thư mục** (framework-dependent, **không** `PublishSingleFile` — Roslyn `ScriptCompiler` cần `Assembly.Location` của reference/globals, `ScriptCompiler.cs:34–52`; single-file làm `Location == ""` → `CantCreateReferenceToAssemblyWithoutLocation`; red-team #9); self-check assert `typeof(EtabsScriptGlobals).Assembly.Location` khác rỗng. `ETABSv1.dll` **không** bao giờ trong output (`Private=false`, ADR-03). Bridge mở **bằng tay** (README + `.mcp.json` note + hint engine nêu tên exe — ADR-01 §4); **không installer**; gỡ = xoá `output/` + 3 thư mục `%AppData%/%LocalAppData%\HPEtabs\`.

### 4. Version
ETABS 22 duy nhất (E1). Wrapper 2.10 tương thích tiến/lùi theo CHM › "Release Notes" nhưng chưa thử → ETABS 21/23 = `[chưa xác minh]`, ngoài scope.

## Alternatives rejected
- **HostVersion = 2026** cho đồng nhất với 3 host: sai sự thật (E1), làm `ValidVersions` vô nghĩa; loại.
- **Categories = AutoCAD 8 mục:** không khớp miền (Load/Analysis/Results); user chốt 9 mục trên.
- **Installer/tray; `AutoLaunchBridge`; pid picker:** YAGNI MVP (red-team #13).

## Consequences
- Phase 0 thêm `PipeNaming.EtabsHost`, `JsonRpcMethods.EtabsPrefix`, hai hint; phase 1 tạo `EtabsHostProfile` với bảng §1 nguyên văn; phase 4 README + CLAUDE.md dùng cùng bảng.
- Tool surface sau phase 4: **24** = 4 core + 8 registry + 12 seed.

## Open items `[chưa xác minh]`
- 👤 "22" là đích (không phải bản khác user có).
- Kích thước thư mục publish bridge (Roslyn ~ 30 MB) — phase 1 đo.
