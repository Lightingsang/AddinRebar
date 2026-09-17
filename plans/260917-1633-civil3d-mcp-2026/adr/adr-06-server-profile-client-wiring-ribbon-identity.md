# ADR-06 — Định danh HPCivil3d: server exe, `Civil3dHostProfile`, client wiring, registry root, Ribbon một nút

**Ngày:** 2026-09-17 · **Status:** Proposed · **Owner:** HPCivil3d
**Kế thừa:** [AutoCAD ADR-04 (registry per host, revised)](../../260913-0000-autocad-mcp-bridge-2026/adr/adr-04-registry-per-host-library-and-host-field.md) · [AutoCAD ADR-06 one MCP one folder](../../260913-0000-autocad-mcp-bridge-2026/adr/adr-06-one-mcp-one-folder.md) · [Navis ADR-05 identity](../../260915-0824-navisworks-mcp-2026/adr/adr-05-navis-plugin-packaging-deploy-identity.md) · [ETABS ADR-05 identity](../../260916-2152-etabs-mcp-2026/adr/adr-05-identity-registry-packaging.md)
**Bằng chứng:** `McpShared/HPRebar.Mcp.Server.Core/Hosts/IHostProfile.cs:15–81` (17 member — HostId, DisplayName, ServerName, ProductFolder, EnvPrefix, DefaultVersion, ValidVersions, MethodPrefix, ExecuteToolName, ContextToolName, ResourceScheme, Categories, CoreToolNames, ScriptImports, ScriptContractSummary, HostAssembly, CliExecutable, MaxTimeoutSeconds, BridgeNotConnectedHint, TimeoutSemanticsHint — `verified by grep 2026-09-17`) · `McpShared/HPRebar.Mcp.Contracts/PipeNaming.cs:41–48` (nhánh `_ => "hp" + key + "-mcp-" + version` → `hpcivil3d-mcp-2026` **đã đúng hôm nay không cần sửa**; hằng `Civil3dHost` thêm để có tên) · `HPAutoCad/HPAutoCad.Mcp.Server/Hosts/AutocadHostProfile.cs` (43 dòng — khuôn) · CLAUDE.md § "HPAutoCad MCP Bridge › Ribbon tab" (0.3.0, một nút, icon vector).

## Context

Bốn host cũ đặt tên theo một khuôn: folder top-level `HP<Host>/`, exe `HP<Host>.Mcp.Server` (net10, stdio, single-file), HostId chữ thường, pipe `hp<host>-mcp-<version>`, prefix method `<host>.`, env `HP<HOST>_MCP_`, registry root `%AppData%\HP<Host>\McpServer\`, entry `.mcp.json` `hprebar-<host>` (user tự thêm, untracked), resource scheme `<host>://`. Civil 3D là vertical của AutoCAD 2026 nhưng phải là **host riêng** (ADR-02: pipe riêng, bundle riêng) — nên lấy trọn khuôn, chỉ đổi token. User đã chốt mọi token trong yêu cầu; ADR này ghi lại để 5 phase và docs dùng một nguồn.

## Decision

### 1. Bảng định danh (nguồn duy nhất — mọi file plan/code trích từ đây)

| Khía cạnh | Giá trị | Ghi chú |
|---|---|---|
| Folder top-level | `HPCivil3d/` (NEW) | chữ `d` thường như user viết; ETABS plan từng viết `HPCivil3D/` — coi là cùng thứ, dùng `HPCivil3d` |
| Solution | `HPCivil3d/HPCivil3d.slnx` + `global.json` (copy `HPAutoCad/global.json`: sdk 10.0.300, MTP) + `Directory.Build.props` (ADR-01/ADR-02: dò `C3D\` từ registry `ACAD-9100:409`) | configurations `Debug`/`Release` thường |
| Projects | `HPCivil3d.McpBridge.Loader` · `HPCivil3d.McpBridge` · `HPCivil3d.Mcp.Server` · `HPCivil3d.Mcp.Server.Tests` · `HPCivil3d.McpBridge.Tests` (ADR-01 A: `MirrorTests` + units/guard — phase 2) | mirror `HPAutoCad.slnx` `/Shared/` folder trỏ `../McpShared/*` |
| HostId | `civil3d` | `PipeNaming.Civil3dHost = "civil3d"` (phase 0) |
| DisplayName / ServerName | `Civil 3D` / `HPCivil3d MCP` | |
| ProductFolder | `HPCivil3d` → settings `%AppData%\HPCivil3d\McpBridge\settings.json`, audit `…\audit\`, log `%LocalAppData%\HPCivil3d\McpBridge\logs\`, registry `%AppData%\HPCivil3d\McpServer\{registry.db, tools-library}` | `BridgeSettingsStore("HPCivil3d","McpBridge")` như AutoCAD `BridgeEntry.cs:66` |
| EnvPrefix | `HPCIVIL3D_MCP_` → `HPCIVIL3D_MCP_Bridge__HostVersion=2026`, `HPCIVIL3D_MCP_Registry__LibraryPath/DbPath` | harness dùng 2 biến Registry để cách ly |
| DefaultVersion / ValidVersions | `2026` / `[2026]` | Civil 3D 2025 (R25.0) ngoài MVP — ADR-02 §4 |
| Pipe | `hpcivil3d-mcp-2026` = `PipeNaming.For("civil3d", 2026)` | nhánh mặc định đã sinh đúng; thêm `case Civil3dHost` để tự tài liệu (phase 0) |
| MethodPrefix | `civil3d.` (`JsonRpcMethods.Civil3dPrefix`) — `civil3d.execute` ≡ `autocad.execute` (dispatch theo suffix) | phase 0 |
| Core tools | `execute_civil3d_code` (Destructive) · `get_civil3d_context` (ReadOnly) · `inspect_type` (ReadOnly) · `cancel_execution` | + 8 registry tool engine (mô tả host-neutral) |
| Resources / prompts | `civil3d://document/info`, `civil3d://selection`, `registry://tools[/{name}]` · `civil3d_query_template`, `civil3d_modify_template`, `toolify_run` | mirror `HPAutoCad.Mcp.Server/Resources`, `Prompts` |
| Categories | `Document, Alignment, Profile, Surface, Corridor, Pipe, Parcel, Point, Data, Generic` (10) | user chốt; seed folder = category (ADR-05) |
| MaxTimeoutSeconds | **120** (mặc định engine) — không heavy tier trong MVP | `Corridor.Rebuild()` dài → ADR-04 quyết (dự kiến bị guard deny trong MVP, không cần 600 s) |
| CliExecutable | `HPCivil3d.Mcp.Server.exe` (`registry approve <name> --by <who>` …) | text review file nêu đúng exe |
| BridgeNotConnectedHint | "Civil 3D 2026 bridge not connected — open Civil 3D 2026 (acad.exe /product C3D), the HPCivil3d MCP Bridge bundle loads at startup; open ribbon HPCivil3d ▸ MCP ▸ MCP Bridge and tick 'Allow AI code execution' (pipe hpcivil3d-mcp-2026). Plain AutoCAD 2026 does not load this bundle." | phân biệt với AutoCAD MCP — lỗi hay gặp nhất: user mở nhầm sản phẩm |
| TimeoutSemanticsHint | `null` → text engine cũ (rollback qua `outer.Abort()` như AutoCAD) | ADR-04 |
| `.mcp.json` | `hprebar-civil3d` → `HPCivil3d/output/HPCivil3d.Mcp.Server/HPCivil3d.Mcp.Server.exe` + env `HPCIVIL3D_MCP_Bridge__HostVersion=2026` — **user tự thêm, không commit** (file tracked mang path máy) | 👤 phase 5 |
| Publish | server: `dotnet publish HPCivil3d/HPCivil3d.Mcp.Server -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -o HPCivil3d/output/HPCivil3d.Mcp.Server` (≈7.4 MB như 3 exe kia); bridge: `dotnet build HPCivil3d/HPCivil3d.slnx -c Debug` deploy bundle (`-p:DeployBundle=false` khi Civil 3D mở) | exe bị lock khi một server `hprebar-civil3d` đang chạy → restart Claude Code trước khi publish (CLAUDE.md § Publish AutoCAD) |
| Bundle | `%AppData%\Autodesk\ApplicationPlugins\HPCivil3d.McpBridge.bundle\` — chi tiết ADR-02 | ProductCode GUID mới cố định (sinh ở phase 1, ≠ `7B4E9C2D-…` của AutoCAD) |
| Ribbon | tab `HPCivil3d` (id `HPCIVIL3D_MCP_TAB`) ▸ panel `MCP` (`HPCIVIL3D_MCP_PANEL`) ▸ nút `MCP Bridge` (`HPCIVIL3D_MCP_BRIDGE`) → `BridgeActions.Run("show")`; command `HPC3DMCPBRIDGE` (khác `HPMCPBRIDGE` của AutoCAD — hai bundle không bao giờ cùng process nhưng tên lệnh riêng để log/harness không nhầm); icon = cùng vector `DrawingImage` (window + plug) của AutoCAD 0.3.0, ink theo COLORTHEME | mirror `HPAutoCad/HPAutoCad.McpBridge.Loader/Ribbon/{McpRibbonTab.cs,RibbonIcons.cs,RibbonCommandHandler.cs}` |
| Version bundle/assembly | `0.1.0` | AutoCAD ở 0.3.0 — số riêng |

### 2. `Civil3dHostProfile` (server) — điền theo bảng, `WithHostAssembly(typeof(Program).Assembly)`
- `ScriptImports = HostScriptContracts.Civil3dImports` (phase 0: AutoCAD imports + `Autodesk.Civil`, `Autodesk.Civil.ApplicationServices`, `Autodesk.Civil.DatabaseServices`, `Autodesk.Civil.DatabaseServices.Styles`, `Autodesk.Civil.Settings` — tên namespace chốt sau reflection, ADR-03).
- `ScriptContractSummary`: globals `doc/db/ed/app/tr/units/civil/ct/log/progress/args`; "`tr` là transaction của bridge, không `StartTransaction`/`Commit`"; "XY/đoạn dài ở biên tool = mm qua `units`; station/elevation = đơn vị bản vẽ, mọi field ghi rõ đơn vị" (ADR-03); "Civil object phải rebuild → ghi rõ trong description" (ADR-04).
- Mô tả `execute_civil3d_code` ≤ 1 800 ký tự (trần đã dùng cho ETABS 1 704) — kiểm bằng test như `HPEtabs.Mcp.Server.Tests`.

### 3. Server không tham chiếu API host
`HPCivil3d.Mcp.Server` chỉ `ProjectReference ../McpShared/*` + `ModelContextProtocol` (cùng version 3 exe kia); **không** `AeccDbMgd`, **không** `AutoCAD.NET`. Seed compile-check nằm trong `HPCivil3d.Mcp.Server.Tests` và **skip quan sát được** khi máy không có Civil 3D (ADR-03 §test).

## Alternatives rejected
- **HostId `c3d` / pipe `hpc3d-mcp-2026`:** ngắn hơn nhưng khác tên folder/exe; khuôn 4 host cũ là HostId = tên folder bỏ `HP` → giữ `civil3d`.
- **Dùng chung registry root với AutoCAD (`%AppData%\HPAutoCad\McpServer\`):** seed Civil dùng `civil`/`AeccDbMgd` không chạy trong AutoCAD; `host` field trong `tool.json` là hàng rào mềm; root riêng = zero rủi ro (ADR-04 AutoCAD revised đã chọn root theo product).
- **Command `HPMCPBRIDGE` trùng AutoCAD:** không xung đột runtime (khác process) nhưng harness/log/README dễ nhầm sản phẩm → tên riêng.
- **Skill `.claude/skills/hp-mcp-civil3d/` (NEW, chưa tồn tại):** hữu ích (3 host đã có) nhưng ngoài scope user nêu cho phase 5 → ghi "follow-up tuỳ chọn".

## Consequences
- Phase 0 thêm 4 hằng/profile (ADR-01 phase 0 table); phase 3 tạo profile + 4 tool + prompts/resources theo bảng §1; phase 5 viết CLAUDE.md/README/`.mcp.json` note từ bảng này — không phát sinh tên mới ở phase sau.
- Hai server (`hprebar-autocad`, `hprebar-civil3d`) chạy cùng lúc trong một phiên Claude Code là bình thường: pipe khác, registry root khác, exe khác.

## Open items `[chưa xác minh]`
- Không có — mọi giá trị là quy ước nội bộ; chỉ `Civil3dImports` chờ tên namespace từ reflection (ADR-03).
