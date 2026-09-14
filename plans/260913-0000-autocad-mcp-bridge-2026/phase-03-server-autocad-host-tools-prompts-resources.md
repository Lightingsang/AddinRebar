---
title: "Phase 3 — Exe `HPAutoCad.Mcp.Server`: AutocadHostProfile, execute_autocad_code, get_autocad_context, prompts, resources, .mcp.json"
status: built + tested + verified (2026-09-14) — exe over stdio with AutoCAD 2026 7/7, two exes side by side 34/12 (reports/phase-03-server-smoke-two-exes.md); 8 AutoCAD tests
priority: P1
effort: 6h (actual ≈ 2h)
depends_on: [phase-00]
created: 2026-09-13
revised: 2026-09-14 (ADR-06 — exe riêng trong `HPAutoCad/`, không host switch)
---

# Phase 3 — AutoCAD MCP server exe

> Revised 2026-09-14: đây là **exe riêng** `HPAutoCad/HPAutoCad.Mcp.Server` (net10, ~15 dòng `Program.cs` gọi `McpServerHost.CreateBuilder` từ `McpShared/HPRebar.Mcp.Server.Core`), không còn `HPREBAR_MCP_Host`/`HostProfileFactory`/`Hosts/Autocad/` trong `HPRebar/`.

## Context
- [ADR-06](adr/adr-06-one-mcp-one-folder.md) · [ADR-04 revised](adr/adr-04-registry-per-host-library-and-host-field.md) · [architecture.md §3 tool surface, §4 IPC, §5 layout](architecture.md) · phase 0 đã có `McpServerHost`, `IHostProfile`, `ExecuteCodeService`, `ContextService`, dispatcher suffix.
- Mẫu: `HPRebar/HPRebar.Mcp.Server/{Program.cs, Hosts/Revit/*}` sau phase 0 (exe mỏng), `.mcp.json` entry `hprebar-revit`.
- NotebookLM: tool `Name` tường minh snake_case (Q04/Q13), `ToolAnnotations` (Q12), resource URI lowercase (Q13 §3), server domain-focused (Q13 §1).

## Overview
Tạo exe AutoCAD với 4 file host-specific + profile + `appsettings.json`. Sau phase này, `HPAutoCad.Mcp.Server.exe` cho `tools/list` = 4 core + 8 registry (+ 0 seed cho tới phase 4) và `execute_autocad_code` chạy end-to-end qua stdio với bridge phase 2.

## Key insights
- Description của `execute_autocad_code` là "tài liệu API" duy nhất AI đọc trước khi viết script → phải nêu `tr`, `units`, usings, cấm prompt/Commit, drawing units, handle.
- `ContextResult` dùng chung nhưng AutoCAD điền thêm `Autocad` object; `get_autocad_context` mô tả các field đó; `revitVersion` không lộ ra AI khi `Host != revit` (serializer bỏ qua — quyết định khi implement, ưu tiên ẩn).
- `.mcp.json` untracked, machine-specific → plan chỉ ghi snippet; user thêm entry (User Action ở phase 5).
- Registry root của exe này: `%AppData%\HPAutoCad\McpServer\` (ADR-04) — tạo tự động khi start; `registry.db` riêng.

## Requirements
Functional
- `HPAutoCad/HPAutoCad.Mcp.Server/HPAutoCad.Mcp.Server.csproj`: `Exe`, `net10.0`, `InvariantGlobalization`, `IncludeNativeLibrariesForSelfExtract`, `ProjectReference ../../McpShared/HPRebar.Mcp.Server.Core`, `Content appsettings.json`, `EmbeddedResource Registry/SeedLibrary/**` với `LogicalName="SeedLibrary/%(RecursiveDir)%(Filename)%(Extension)"` + `Compile Remove` (copy pattern từ csproj Revit; seed đến ở phase 4).
- `Program.cs`: `return await McpServerHost.RunAsync(args, new AutocadHostProfile());` (helper bao `CreateBuilder` + nhánh CLI + `RunAsync`).
- `Hosts/AutocadHostProfile.cs`: `HostId="autocad"`, `DisplayName="AutoCAD"`, `ServerName="HPAutoCad MCP"`, `ProductFolder="HPAutoCad"`, `EnvPrefix="HPAUTOCAD_MCP_"`, `DefaultVersion=2026`, `ValidVersions={2026}` (2025 mở sau, ADR-05 §4), `PipeName(v)=PipeNaming.For("autocad", v)` → `hpautocad-mcp-2026`, `MethodPrefix="autocad."`, `ExecuteToolName="execute_autocad_code"`, `ContextToolName="get_autocad_context"`, `ResourceScheme="autocad"`, `Categories={Drawing, Layer, Block, Annotation, Layout, Data, Generic}`, `CoreToolNames`, `ScriptContractSummary` (globals + usings từ `HostScriptContracts.AutocadImports` + units + tr rule), `HostAssembly`.
- `Tools/ExecuteAutocadCodeTool.cs`: `[McpServerTool(Name="execute_autocad_code", Title="Execute C# in AutoCAD", Destructive=true, ReadOnly=false, Idempotent=false, OpenWorld=false)]`; tham số y hệt Revit; thân = `ExecuteCodeService.ExecuteAsync(...)`. Description (rút gọn): "Runs a C# script inside the open AutoCAD session with the user's privileges. Globals: doc (Document), db (Database), ed (Editor — only WriteMessage/SelectImplied/SelectAll; prompts are blocked), app (DocumentCollection), tr (the outermost Transaction the bridge opened — use tr.GetObject / tr.AddNewlyCreatedDBObject, never Commit/Abort it), units (units.ToDrawing(mm), units.ToMm(du), units.Label — coordinates are drawing units), ct, log(string), progress(cur,total,msg), args (…). Default usings: … . End with `return <value>;`; ObjectId → {handle,class}, Point3d → {x,y,z}, Entity → {handle,type,layer}. transaction auto|none (manual is accepted but runs like auto — never call StartTransaction, the guard rejects it), dryRun rolls back, U in AutoCAD reverts the AI's runs since the user's last command. isModifiable in get_autocad_context means "quiescent and not read-only" (Revit: a transaction is open) — say so in both descriptions. Fails with isError + diagnostics when … Requires the user to tick 'Allow AI code execution' in the HPAutoCad MCP Bridge window (command HPMCPBRIDGE) inside AutoCAD."
- `Tools/AutocadContextTool.cs` (`get_autocad_context`, ReadOnly): mô tả `Autocad{insunits, measurement, currentLayout, currentLayer, isModelSpace, isQuiescent, isNamedDrawing}` + selection `{id=handle value, category=layer, name=dxfName}`; thân = `ContextService`.
- `Resources/AutocadDocumentResources.cs`: `autocad://document/info`, `autocad://selection`.
- `Prompts/AutocadScriptPrompts.cs`: `autocad_query_template` (few-shot: đếm entity trên layer bằng `ed.SelectAll(filter)`; `transaction:"none"`), `autocad_modify_template` (few-shot: dryRun tạo `Polyline` từ `args.List("points")` với `units.ToDrawing`, rồi real run; nhắc `tr.AddNewlyCreatedDBObject`).
- `appsettings.json`: `Bridge: { HostVersion: 2026, … }` (copy Revit, bỏ `RevitVersion`), `Registry: { PublishPolicy: manual, … }` (không đặt `LibraryPath`/`DbPath` → default theo product).
- `HPAutoCad.slnx` thêm exe + `HPAutoCad.Mcp.Server.Tests` (xUnit v3, net10, `UseMicrosoftTestingPlatformRunner`, ref exe + `Server.Core` + `McpBridge.Core` cho fake executor/pipe).
- Snippet `.mcp.json` (docs + `HPAutoCad/README.md`): entry `hprebar-autocad` → `command: "<repo>\\HPAutoCad\\output\\HPAutoCad.Mcp.Server\\HPAutoCad.Mcp.Server.exe"`, `env: { "HPAUTOCAD_MCP_Bridge__HostVersion": "2026" }` (tuỳ chọn).
Non-functional
- `mcp_call.py` (harness stdio, scratchpad; ~60 dòng: spawn exe, `initialize`, `tools/list`, `tools/call`) nhận đường dẫn exe → dùng cho cả hai exe.

## Architecture
Xem [architecture.md §1, §3, §5](architecture.md). Không thay đổi `McpShared/` ở phase này (mọi seam đã có từ phase 0).

## Related code files
- **Tái dùng nguyên (`McpShared/HPRebar.Mcp.Server.Core`):** `McpServerHost`, `ExecuteCodeService`, `ContextService`, `RevitBridgeClient`, `ResultFormatter`, `NdjsonPipeTransport`, `InspectTypeTool`, `CancelExecutionTool`, toàn bộ `Registry/*`, `Tools/Registry/*`, `ToolifyPrompts`, `ToolRegistryResources`, `RegistryCli`.
- **Tách ra chung / sửa:** không (nếu `McpServerHost` thiếu tham số nào → sửa ở Core, kèm test).
- **Viết mới (`HPAutoCad/`):** `HPAutoCad.Mcp.Server/{HPAutoCad.Mcp.Server.csproj, Program.cs, appsettings.json, Hosts/AutocadHostProfile.cs, Tools/ExecuteAutocadCodeTool.cs, Tools/AutocadContextTool.cs, Resources/AutocadDocumentResources.cs, Prompts/AutocadScriptPrompts.cs}`; `HPAutoCad.Mcp.Server.Tests/{csproj, HostProfileTests.cs (pipe `hpautocad-mcp-2026`, method `autocad.execute`, `tools/list` chứa `execute_autocad_code` và **không** chứa `execute_revit_code`, root `%AppData%\HPAutoCad\McpServer`), AutocadToolsOverPipeTests.cs (pipe thật + `FakeRevitExecutor` trả `ContextResult` có `Autocad` → tool trả JSON đúng field; `execute_autocad_code` gửi `autocad.execute` với `ExecuteRequest` đúng)}`; `HPAutoCad.slnx` cập nhật.

## Implementation steps
1. csproj exe + `Program.cs` + `appsettings.json`; build.
2. `AutocadHostProfile`; test profile.
3. 4 file host-specific; description theo architecture §3.
4. Test project + 2 test class; chạy `dotnet test HPAutoCad/HPAutoCad.Mcp.Server.Tests`.
5. Publish exe (lệnh ở architecture §5); `mcp_call.py <exe> tools/list` = 12 (4 + 8); `initialize` → `serverInfo.name = "HPAutoCad MCP"`; với AutoCAD 2026 + bridge phase 2: `get_autocad_context`; `execute_autocad_code` `return db.Filename` (none) và dryRun tạo Line; kiểm `runId` + `hint` (registry rỗng nhưng `runs` ghi vào `%AppData%\HPAutoCad\McpServer\registry.db`).
6. Song song: `mcp_call.py <exe Revit> tools/list` = 34 với Revit 2026 → ghi `reports/phase-03-two-exes.md`.

## Todo
- [x] 1 exe (`HPAutoCad.Mcp.Server`, Program 1 dòng, appsettings `Bridge.HostVersion`) · [x] 2 profile (`AutocadHostProfile`, ValidVersions {2026}) · [x] 3 host files (execute/context tool, `autocad://` resources, 2 prompt) · [x] 4 tests (`HostProfileTests` 4 + `AutocadToolsOverPipeTests` 4) · [x] 5 publish + stdio smoke (`tools/harness/run-server-smoke.ps1` 7/7) · [x] 6 two-exe smoke (Revit 34 không đổi, AutoCAD 12)
- Sai lệch: `revitVersion` ẩn cho host ≠ revit ở `ContextService` (Core, +test Revit giữ nguyên); `IsModifiable` doc trong Contracts XML + description; harness stdio `mcp-call.py` + `run-server-smoke.ps1` vào repo; `get_run` record vẫn có `revitVersion` (engine DTO) → phase 4.

## Success criteria
- `dotnet build HPAutoCad/HPAutoCad.slnx -c Debug -p:DeployBundle=false` xanh; `dotnet test HPAutoCad/HPAutoCad.Mcp.Server.Tests` xanh (≥ 6 test).
- `tools/list` AutoCAD = 12 tool đúng tên; Revit = 34 (không đổi); `serverInfo.name` đúng theo exe.
- `execute_autocad_code` end-to-end qua stdio: none-read OK; dryRun `rolledBack=true`; real run → Line trong drawing; `runId` trả về.
- Hai exe chạy cùng lúc trong một phiên `mcp_call.py`/Claude Code không ảnh hưởng nhau; `%AppData%\HPRebar\McpServer\` không đổi.

## Risks
| Risk | Mitigation |
|---|---|
| Description quá dài (token) | giữ ≤ 1 200 ký tự; chi tiết chuyển sang prompt template |
| AI nhầm `Application` (2 namespace) | usings mặc định không có `.Core`; description nói rõ |
| `ContextResult.RevitVersion` lộ ra AI trong JSON AutoCAD | `ContextService` ẩn field khi `Host != revit` (`JsonIgnore` có điều kiện hoặc DTO chiếu) |
| Hai exe cùng tên tool registry (`search_tools`…) trong một phiên Claude | host AI prefix theo server (`hprebar-revit:`/`hprebar-autocad:`); `ServerName` khác nhau |

## Security
Annotations giữ như Revit; `execute_autocad_code` `Destructive=true` → host AI hỏi user mỗi lần (Q12). Không lộ path (`SafeText`).

## Next steps
Phase 4: registry theo profile + seed AutoCAD nhúng trong exe này để `tools/list` có tool thật và vòng lặp propose/test/publish chạy trên AutoCAD.
