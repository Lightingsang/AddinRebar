# ADR-05 — Đóng gói plugin Navisworks, triển khai/gỡ, định danh riêng (pipe, env, `.mcp.json`, registry root)

**Ngày:** 2026-09-15 · **Revised 2026-09-15 (red-team):** MVP không có Ribbon tab/theme switcher — `AddInPlugin` trong menu Add-ins mở cửa sổ; Ribbon là bước sau khi Verified (đúng thứ tự AutoCAD đã làm); 4 project (thêm `HPNavis.McpBridge.Tests` net48) · **Status:** Proposed · **Owner:** HPNavis
**Kế thừa:** [AutoCAD ADR-05 packaging](../../260913-0000-autocad-mcp-bridge-2026/adr/adr-05-autocad-plugin-packaging-alc-isolation-multi-version.md) · [AutoCAD ADR-04 registry per host](../../260913-0000-autocad-mcp-bridge-2026/adr/adr-04-registry-per-host-library-and-host-field.md) · [ADR-06 one MCP one folder](../../260913-0000-autocad-mcp-bridge-2026/adr/adr-06-one-mcp-one-folder.md)
**Bằng chứng:** [evidence §E2, E3, E4, E14](../research/evidence-on-machine-2026-09-15.md) · [researcher-01 §1–§2, §7, §9](../research/researcher-01-navisworks-api-plugin-facts.md)

## Context

- Navisworks quét plugin ở `%AppData%\Autodesk\Navisworks Manage 2026\Plugins\<Folder>\<Folder>.dll` (đã có thư mục + precedent `NavisworksMCPPlugin` — E3) và `<install>\Plugins\<Folder>\` (rỗng trên máy). Quy tắc **tên thư mục = tên assembly** (researcher-01 §1, nguồn RevitNetAddinWizard blog; khớp E3).
- Bundle `%AppData%\Autodesk\ApplicationPlugins\*.bundle` với `PackageContents.xml` được APS publisher guidelines nhắc cho Navisworks nhưng **[chưa xác minh]** trên 2026 và có bình luận cộng đồng bảo không ổn định (researcher-01 §1).
- Không có bằng chứng về SECURELOAD/"publisher could not be verified" cho Navisworks — **[chưa xác minh]**; `NavisworksMCPPlugin.dll` trên máy có `PublicKeyToken=null` (không ký) và đã từng được nạp (E3 — ribbon `.name`/`.xaml` tồn tại chứng tỏ đã deploy; có nạp thành công không thì spike S-01 kiểm bằng plugin của ta).
- Chỉ **một** loại plugin nạp sớm không cần click: `EventWatcherPlugin` ("not delay loaded", E4) — đúng vai `IExtensionApplication.Initialize` của AutoCAD. Ribbon cần `CommandHandlerPlugin` + `RibbonLayout` (E3/E4).

## Decision

### 1. Một assembly, một thư mục phẳng, không loader/ALC, không Ribbon trong MVP

```
%AppData%\Autodesk\Navisworks Manage 2026\Plugins\HPNavis.McpBridge\
├── HPNavis.McpBridge.dll            ← net48; [Plugin("HPNavis.McpBridge", "HPNV", DisplayName="HPNavis MCP")] : EventWatcherPlugin (listener, executor)
│                                      + [Plugin("HPNavis.McpBridge.Window", "HPNV", DisplayName="HPNavis MCP")] [AddInPlugin(AddInLocation.AddIn)] : AddInPlugin — Execute() mở/activate cửa sổ trạng thái
├── HPNavis.McpBridge.pdb
├── HPRebar.Mcp.Contracts.dll · HPRebar.McpBridge.Core.dll (net48 build)
├── Microsoft.CodeAnalysis*.dll (4) · System.Collections.Immutable · System.Reflection.Metadata · System.Memory · System.Buffers
│   System.Numerics.Vectors · System.Runtime.CompilerServices.Unsafe · System.Threading.Tasks.Extensions · System.Text.Encoding.CodePages
│   System.Text.Json · System.Text.Encodings.Web · Microsoft.Bcl.AsyncInterfaces · CommunityToolkit.Mvvm · Serilog · Serilog.Sinks.File   (24 file, E12)
└── README.md
```
- `DeveloperId = "HPNV"` (4 ký tự; đổi sang GUID nếu xung đột). **Hai** `[Plugin]` trong một assembly: `EventWatcherPlugin` nạp sớm (không delay-load — E4) giữ listener; `AddInPlugin` với `AddInLocation.AddIn` ("Display in the Addin menu" — `Api.xml:20289–20291`) là nút mở cửa sổ **miễn phí**: không XAML ribbon, không `.name`, không icon. Ribbon tab "MCP Navis" (`CommandHandlerPlugin` + `RibbonLayout`, mẫu E3) → **sau** phase 5 Verified, như AutoCAD đã thêm ribbon ở bundle 0.2.0 sau MVP.
- Roslyn dependencies không xung đột kiểu ALC AutoCAD (Roamer không ship Roslyn/Immutable/STJ — E2); `AssemblyResolve` **hẹp** theo ADR-01 §5. `SatelliteResourceLanguages=en`.
- **Không** bundle `ApplicationPlugins` cho MVP.

### 2. Deploy / gỡ

- `HPNavis.McpBridge.csproj` target `DeployPlugin` `AfterTargets=Build`, `Condition Debug && DeployPlugin!=false` → `RemoveDir` + `Copy` toàn bộ `$(OutDir)` sang `$(AppData)\Autodesk\Navisworks Manage $(NavisworksYear)\Plugins\HPNavis.McpBridge\` (mirror `HPAutoCad.McpBridge.Loader.csproj:36–57` `DeployBundle`). `-p:DeployPlugin=false` khi Roamer đang mở (file lock).
- Gỡ: xoá thư mục đó. Không registry, không `.addin`. `HPNavis/README.md` ghi 3 dòng: cài (build Debug), tắt tạm (đổi tên thư mục), gỡ (xoá).
- Bảo mật khi nạp: **[chưa xác minh]** → spike S-01 ghi lại có prompt hay không; nếu có prompt "Always Load"-kiểu → docs như Revit/AutoCAD. Không ký số trong MVP (giống hai host cũ).
- Plugin lạ `NavisworksMCPPlugin` (E3): trước live verify **đổi tên thư mục** thành `NavisworksMCPPlugin.disabled` (không xoá — của user) để kết quả không nhiễu; ghi trong harness README + hỏi user một lần ở phase 5 (thao tác trên máy user → 👤).

### 3. Định danh riêng — không đụng entry đang chạy

| Mục | Giá trị | Nguồn/ghi chú |
|---|---|---|
| `IHostProfile.HostId` | `navis` | `PipeNaming.NavisHost` (hằng mới, additive) |
| Pipe | `hpnavis-mcp-2026` | `PipeNaming.For("navis", 2026)` — nhánh mặc định đã sinh đúng (E14); thêm `case NavisHost` cho tường minh |
| Wire prefix | `navis.` → `navis.execute`… | `JsonRpcMethods.NavisPrefix` (additive); dispatcher đã nhận mọi prefix (E14) |
| Tool core | `execute_navis_code`, `get_navis_context`, `inspect_type`, `cancel_execution` | mirror AutoCAD |
| Resource scheme | `navis://document/info`, `navis://selection`, `navis://models`, `registry://tools[/{name}]` | `ResourceScheme = "navis"` |
| Server exe / `ServerName` | `HPNavis.Mcp.Server.exe` / `HPNavis MCP` | |
| Env prefix | `HPNAVIS_MCP_` → `HPNAVIS_MCP_Bridge__HostVersion=2026` | |
| Registry root | `%AppData%\HPNavis\McpServer\` (`registry.db`, `tools-library\<Category>\<name>\`) | `ProductFolder = "HPNavis"` |
| Bridge settings/log/audit | `%AppData%\HPNavis\McpBridge\settings.json`, `%LocalAppData%\HPNavis\McpBridge\logs\`, `%AppData%\HPNavis\McpBridge\audit\` | `BridgeSettingsStore("HPNavis","McpBridge")` |
| `.mcp.json` entry (user thêm, untracked) | `hprebar-navis` → `HPNavis/output/HPNavis.Mcp.Server/HPNavis.Mcp.Server.exe`, env trên | tên theo gợi ý user; không sửa `hprebar-revit`/`hprebar-autocad` |
| Categories (registry) | `Model, Search, Selection, Viewpoint, Clash, Timeliner, Report, Data, Generic` | khác hẳn Drawing/Layer… của AutoCAD |
| `CoreToolNames` (reserved) | 4 tên trên | |
| Host stamp trong `tool.json` | `"host": "navis"` | `ToolValidator` theo profile (đã per-host từ AutoCAD phase 4) |
| Undo entry | `MCP: <label>` | `BeginTransaction(displayName)` |
| Menu | Add-ins ▸ "HPNavis MCP" (`AddInPlugin`); Ribbon tab sau MVP | |

### 4. Multi-version

MVP **Navisworks Manage 2026 (23.0) duy nhất** — máy dev chỉ có 2026; `ValidVersions = [2026]`. Mở 2025 (22.0, cũng .NET Framework 4.8 theo Autodesk KB "What is the .NET Framework version for Navisworks Manage 2025" — researcher-01 nguồn) chỉ cần `NavisworksMajor=22` + thư mục Plugins 2025 + pipe `hpnavis-mcp-2025`; Simulate 2026 (không Clash) → `HasClashModule=false` (ADR-02 §5). Freedom: không có API → không hỗ trợ, ghi rủi ro.

## Alternatives rejected

- **Loader + ALC/AppDomain tách đôi như AutoCAD:** .NET Framework không có ALC; AppDomain riêng không marshal object Navisworks; và không có xung đột Roslyn cần cách ly (E2). Loại — YAGNI.
- **`AddInPlugin` làm entry:** delay-loaded, chỉ chạy khi user click Add-ins → listener không tự lên; `EventWatcherPlugin` mới nạp sớm. Loại (giữ `ExecuteAddInPlugin` cho harness qua Automation nếu cần kích lệnh — ADR-04).
- **Bundle `ApplicationPlugins`:** chưa xác minh trên 2026; thư mục Plugins per-user đã đủ cho dev + gỡ sạch. Hoãn.
- **Dùng lại pipe/entry của AutoCAD/Revit:** vi phạm "một host một pipe"; hai exe chạy song song cần tên khác. Loại.

## Consequences

- `HPNavis/` = `HPNavis.slnx` + `global.json` + `Directory.Build.props` (ADR-03) + **4** project (`HPNavis.McpBridge` net48, `HPNavis.McpBridge.Tests` net48, `HPNavis.Mcp.Server` net10, `HPNavis.Mcp.Server.Tests` net10) + `tools/harness/` + `output/` + `README.md`. Không project Loader, không project Ribbon.
- `McpShared.slnx` không đổi ngoài project test net48 (ADR-01 §4). `HPNavis.slnx` có folder `/Shared/` trỏ `../McpShared/*` (mirror `HPAutoCad.slnx:15–19`).
- CLAUDE.md: hàng `HPNavis/` trong Repository Layout + mục "HPNavis MCP Bridge"; `AGENTS.md` regen bằng lệnh Python đã có; `docs/codebase-summary.md`, `docs/system-architecture.md`, `docs/project-roadmap.md`, `docs/deployment-guide.md` cập nhật (phase 5).
