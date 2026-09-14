# ADR-01 — (Superseded) Tái dùng `HPRebar.Mcp.Server` cho AutoCAD: host profile trong một exe, một instance = một host

**Ngày:** 2026-09-13 · **Status:** **Superseded by [ADR-06](adr-06-one-mcp-one-folder.md) (2026-09-14) ở phần server** — ràng buộc mới của user: mỗi MCP một folder top-level, không dùng chung exe. Ma trận A/B/C dưới giữ làm lịch sử; phần còn sống: khái niệm `IHostProfile` (seam compile-time trong `McpShared/HPRebar.Mcp.Server.Core`), tách `ExecuteCodeService`/`ContextService`, Contracts chỉ additive, wire-compat với bridge Revit đã deploy. **Không còn** `HPREBAR_MCP_Host`, không còn `Hosts/Autocad/` trong `HPRebar/`. · **Owner:** HPRebar
**Kế thừa:** [Revit ADR-01 topology](../../260912-1521-dynamic-revit-mcp-server-2026/adr/adr-01-two-process-topology.md) · [Revit ADR-02 IPC](../../260912-1521-dynamic-revit-mcp-server-2026/adr/adr-02-ipc-named-pipe.md) · [Revit ADR-06 registry](../../260912-1521-dynamic-revit-mcp-server-2026/adr/adr-06-tool-registry-and-publish-gate.md)

## Context

- Vòng lặp registry (`execute_*_code` → `runId`/`hint` → `get_run` → `toolify_run` → `propose_tool` → `test_tool` → `publish_tool` → `registry approve` → `tools/list_changed` → `search_tools`/gọi theo tên → `runs`/stability/quarantine) đã verified live trên Revit 2026. Yêu cầu: AutoCAD dùng **cùng mã** cho toàn bộ 8 registry tool, prompt `toolify_run`, CLI — không viết bản thứ hai.
- Mã server hiện tại đã gần host-neutral. Đọc 2026-09-13, phần **phụ thuộc Revit** trong `HPRebar/HPRebar.Mcp.Server/` chỉ gồm:
  - `Program.cs` — `WithToolsFromAssembly()` nạp mọi `[McpServerToolType]`; không có khái niệm host.
  - `Models/BridgeOptions.cs:15,53` — `RevitVersion` + `IsValid()` chỉ chấp nhận 2025/2026; `PipeName` = `PipeNaming.For(RevitVersion)` → `hprebar-mcp-r2026`.
  - `Tools/ExecuteRevitCodeTool.cs`, `Tools/RevitContextTool.cs`, `Tools/InspectTypeTool.cs`, `Tools/CancelExecutionTool.cs` — tên tool + description (globals `doc/uidoc/app/uiapp`, usings Revit, feet) là **hằng trong attribute** → không thể đổi theo runtime.
  - `Prompts/RevitScriptPrompts.cs` — persona Revit; `Prompts/ToolifyPrompts.cs:17` — "packaging a C# Revit script".
  - `Resources/RevitDocumentResources.cs` — URI `revit://…`.
  - `Registry/ToolValidator.cs:22-28` — `Categories` (Architecture…MEP) và `ReservedNames` (`execute_revit_code`…) là hằng Revit.
  - `Registry/DynamicToolRegistrar.cs:35` — description "run stored, reviewed C# inside Revit — prefer them over execute_revit_code".
  - `Registry/Model/ToolRecord.cs:53` `RevitVersions`; `RunRecord.RevitVersion`; cột `runs.revit_version`.
  - `Services/RevitBridgeClient.cs`, `IRevitBridgeClient` — chỉ tên; logic pipe/JSON-RPC không có gì Revit.
  - `Registry/SeedLibrary/**` nhúng với `LogicalName="SeedLibrary/<Category>/<name>/<file>"`; `SeedInstaller.LoadSeeds` parse `parts.Length == 3`.
- Sách (NotebookLM Q13, [research/notebooklm-addendum-q13-server-granularity.md](../research/notebooklm-addendum-q13-server-granularity.md)): **server nên domain-focused** (mỗi client 1:1 một server; tool list nhỏ tránh đốt context) **và** một codebase nên tái dùng qua `IOptions<T>` + environment variables. Hai ý này không mâu thuẫn: một exe, nhiều *instance* cấu hình khác nhau.
- Host AI (Claude Code `.mcp.json`) đã có entry `hprebar-revit` với `env.HPREBAR_MCP_Bridge__RevitVersion=2026`. Thêm entry thứ hai với env khác là thao tác cấu hình thuần.

## Ma trận 3 phương án

| Tiêu chí | **A — Host switch trong `HPRebar.Mcp.Server`** (`HPREBAR_MCP_Host=autocad`, pipe `hprebar-mcp-acad2026`) | **B — Server riêng `HPRebar.Mcp.AutocadServer`** (copy Program + tools + registry) | **C — Extract `HPRebar.Mcp.Server.Core` (class lib) + 2 exe mỏng** |
|---|---|---|---|
| Registry dùng chung mã (yêu cầu cứng) | ✅ cùng assembly, cùng DI | ❌ copy → 2 bản registry drift (hoặc reference exe→exe, xấu) | ✅ cùng class lib |
| Tool list của một instance chỉ 1 host (sách Q13) | ✅ profile chọn tool class đăng ký; `tools/list` = 4 core + 8 registry + seed của host đó | ✅ | ✅ |
| Thay đổi mã Revit hiện có | 🟡 nhỏ, cơ học, compile-checked: `Program.cs` đăng ký tool theo profile; `BridgeOptions.IsValid`; `ToolValidator` đọc categories/reserved từ profile; description trung tính; `SeedInstaller` prefix theo host | ✅ gần 0 (nhưng copy 30 file) | 🔴 di chuyển ~40 file sang project mới, sửa namespace, csproj, tests, publish module |
| Artefact publish / `.mcp.json` | 1 exe, 2 entry (`hprebar-revit`, `hprebar-autocad`) khác `env` | 2 exe, 2 entry | 2 exe, 2 entry |
| Build pipeline (`PublishServerModule`, `CreateBundle`) | không đổi | +1 publish module | +1 publish module + project mới |
| Rủi ro sai host (env thiếu) | 🟡 server Revit nói chuyện pipe AutoCAD → mitigated: `ServerInfo.Name` theo host, `get_*_context` trả `host`, default `revit` giữ hành vi cũ | ✅ không thể nhầm | ✅ không thể nhầm |
| Mở đường host thứ 3 (Civil 3D, Navisworks) | ✅ thêm một `IHostProfile` + folder `Hosts/<Host>/` | ❌ copy lần 3 | ✅ thêm exe mỏng |
| Effort ước tính | **~8h** (phase 0) | ~6h ban đầu, nợ kỹ thuật vĩnh viễn | ~14h + regression toàn bộ Revit server |
| Khớp CLAUDE.md "không refactor mã Revit" | 🟡 chạm nhưng hành vi Revit không đổi, 159 xUnit + smoke Revit là gate | ✅ | ❌ |

**Khuyến nghị: A**, với ràng buộc nội bộ để C trở thành bước cơ học nếu sau này cần: mã host-specific đặt trong `Hosts/Revit/` và `Hosts/Autocad/`; mọi thứ ngoài hai folder đó không được `using` Revit/AutoCAD gì cả (đã gần đúng hôm nay).

## Decision

1. **Một exe `HPRebar.Mcp.Server`, một instance phục vụ đúng một host.** Host chọn bằng cấu hình top-level `Host` (`revit` mặc định — giữ nguyên hành vi hiện tại; `autocad`), tức env `HPREBAR_MCP_Host=autocad` (prefix env `HPREBAR_MCP_` đã có ở `Program.cs:24`).
2. **`IHostProfile`** (singleton DI, `Hosts/IHostProfile.cs`) gom mọi thứ khác nhau giữa host: `HostId`, `DisplayName`, `DefaultVersion`, `ValidVersions`, `PipeName(version)`, `MethodPrefix` (`revit.` / `autocad.`), tên tool core (`execute_revit_code` / `execute_autocad_code`, `get_revit_context` / `get_autocad_context`), `ResourceScheme` (`revit://` / `autocad://`), `Categories`, `ReservedNames`, `SeedResourcePrefix`, `LibraryPathDefault`, `DbPathDefault`, `ServerInfo.Name`. Hai implementation: `Hosts/Revit/RevitHostProfile.cs`, `Hosts/Autocad/AutocadHostProfile.cs`.
3. **Đăng ký tool theo profile** trong `Program.cs`: tool/prompt/resource **dùng chung** (8 registry tool, `toolify_run`, `registry://…`) giữ `[McpServerToolType]` + `WithToolsFromAssembly()`; tool/prompt/resource **host-specific** (4 core, 2 prompt template, 2 resource) **bỏ** attribute type-level khỏi assembly scan và đăng ký tường minh `.WithTools<T>()` / `.WithPrompts<T>()` / `.WithResources<T>()` theo `profile` (SDK 2.2.0 có các overload generic này — **verify khi compile phase 0**; fallback: `McpServerTool.Create(...)` thủ công như `DynamicToolRegistrar` đang làm).
4. **Thân tool core dùng chung:** phần validate/gửi pipe/ghi run history của `ExecuteRevitCodeTool.ExecuteAsync` tách thành `Services/ExecuteCodeService.cs`; `ExecuteRevitCodeTool` và `ExecuteAutocadCodeTool` chỉ còn attribute + description + gọi service. Tương tự `ContextService` cho 2 context tool. `InspectTypeTool`, `CancelExecutionTool` giữ nguyên tên/logic, chỉ description trung tính ("the connected CAD host").
5. **Wire-compat với bridge Revit đã deploy (commit 9b83aee):** server host `revit` vẫn gửi `revit.*`, đọc `revitVersion`; mọi thay đổi Contracts chỉ **additive** (`ContextResult.Host`, `HostVersion`, `Autocad`; `PipeNaming.For(host, version)` overload mới; `JsonRpcMethods` thêm hằng `autocad.*` + helper `Suffix()`). Không đổi tên field JSON nào đang có.
6. **`.mcp.json`:** thêm entry `hprebar-autocad` cùng `command` với `hprebar-revit`, `env: { HPREBAR_MCP_Host: "autocad", HPREBAR_MCP_Bridge__HostVersion: "2026" }`. `Bridge:RevitVersion` giữ làm alias của `Bridge:HostVersion` để entry cũ không phải sửa.
7. **`ServerInfo.Name`** = `HPRebar Revit MCP` / `HPRebar AutoCAD MCP` (sách Q13 §3) để host AI và log phân biệt.

## Alternatives rejected

- **B — server riêng:** vi phạm yêu cầu "registry dùng chung mã"; mọi fix registry phải làm 2 lần; ADR-06 (files là sự thật, watcher, CLI approve) sẽ có 2 CLI.
- **C — extract Core ngay:** đúng về kiến trúc nhưng chi phí regression cao trên mã Revit vừa verified; không mang lại hành vi mới. Giữ làm bước sau nếu xuất hiện host thứ 3 hoặc khi cần publish server Revit và AutoCAD với cadence khác nhau.
- **Một instance phục vụ cả hai host cùng lúc** (`tools/list` gộp 34 + N tool): trái lời khuyên sách (tool list nhỏ, domain-focused); tên tool registry có thể trùng giữa host (`create_layer` vs Revit `create_level` không trùng nhưng `get_selected_elements`/`get_selected_entities` dễ nhầm); `search_tools` phải lọc host mỗi lần; một bridge rớt kéo theo cả hai. Loại.

## Consequences

- Phase 0 là **refactor không đổi hành vi** trên server Revit: gate = `dotnet test HPRebar.Mcp.Server.Tests` (159 hiện có, thêm test profile) + publish exe + `mcp_call.py tools/list` cho host `revit` vẫn = 34 + smoke `get_revit_context` với Revit 2026 đang chạy.
- Description của tool/prompt dùng chung không được nêu "Revit" cứng nữa; wording trung tính: "the connected CAD host (Revit or AutoCAD)"; ví dụ few-shot Revit chuyển vào `Hosts/Revit/` prompt.
- Hai server process có thể chạy song song trong một phiên Claude Code (mỗi process một pipe); registry mỗi host độc lập (ADR-04) nên không tranh chấp file/DB.
- Cần test mới: `HostProfileTests` (default `revit`, `autocad` chọn đúng pipe/tool set/prefix), `RequestDispatcher` chấp nhận `autocad.execute` (Core thay đổi kèm phase 0).
