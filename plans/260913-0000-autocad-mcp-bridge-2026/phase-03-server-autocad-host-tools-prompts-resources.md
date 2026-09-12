---
title: "Phase 3 — Server host=autocad: profile, execute_autocad_code, get_autocad_context, prompts, resources, .mcp.json"
status: planned
priority: P1
effort: 8h
depends_on: [phase-00]
created: 2026-09-13
---

# Phase 3 — Server AutoCAD host surface

## Context
- [ADR-01](adr/adr-01-reuse-mcp-server-host-profile.md) · [architecture.md §3 tool surface, §4 IPC](architecture.md) · phase 0 đã có `IHostProfile`, `ExecuteCodeService`, `ContextService`, dispatcher suffix.
- Mẫu: `Hosts/Revit/*` sau phase 0 (thin tool classes), `Prompts/RevitScriptPrompts.cs` (persona + few-shot), `Resources/RevitDocumentResources.cs`, `.mcp.json` entry `hprebar-revit`.
- NotebookLM: tool `Name` tường minh snake_case (Q04/Q13), `ToolAnnotations` (Q12), resource URI lowercase self-describing (Q13 §3), prompts tập trung (Q06/Q13).

## Overview
Thêm `AutocadHostProfile` + 4 file host-specific mỏng. Sau phase này, `HPREBAR_MCP_Host=autocad` cho `tools/list` = 4 core + 8 registry (+ 0 seed cho tới phase 4) và `execute_autocad_code` chạy được end-to-end qua stdio với bridge phase 2.

## Key insights
- Description của `execute_autocad_code` là "tài liệu API" duy nhất AI đọc trước khi viết script → phải nêu `tr`, `units`, usings, cấm prompt/Commit, drawing units, handle.
- `ContextResult` dùng chung nhưng AutoCAD điền thêm `Autocad` object; tool `get_autocad_context` mô tả các field đó.
- `.mcp.json` là file untracked, machine-specific → plan chỉ ghi snippet; user thêm entry (User Action ở phase 5).

## Requirements
Functional
- `Hosts/Autocad/AutocadHostProfile.cs`: `HostId="autocad"`, `DisplayName="AutoCAD"`, `ServerName="HPRebar AutoCAD MCP"`, `DefaultVersion=2026`, `ValidVersions={2026}` (2025 mở sau, ADR-05 §4), `PipeName(v)=PipeNaming.For("autocad", v)`, `MethodPrefix="autocad."`, `ExecuteToolName="execute_autocad_code"`, `ContextToolName="get_autocad_context"`, `ResourceScheme="autocad"`, `Categories={Drawing, Layer, Block, Annotation, Layout, Data, Generic}`, `SeedResourcePrefix="SeedLibrary/Autocad/"`, `LibraryPathDefault=…\tools-library-autocad`, `DbPathDefault=…\registry-autocad.db`, `SettingsFolder="McpBridge.Autocad"`, `ScriptContractSummary` (globals + usings + units + tr rule).
- `Hosts/Autocad/ExecuteAutocadCodeTool.cs`: `[McpServerTool(Name="execute_autocad_code", Title="Execute C# in AutoCAD", Destructive=true, ReadOnly=false, Idempotent=false, OpenWorld=false)]`; tham số y hệt Revit (`code, transaction, dryRun, timeoutSeconds, label, args, progress, ct`); thân = `ExecuteCodeService.ExecuteAsync(...)`. Description (rút gọn, đầy đủ trong code): "Runs a C# script inside the open AutoCAD session with the user's privileges. Globals: doc (Document), db (Database), ed (Editor — only WriteMessage/SelectImplied/SelectAll; prompts are blocked), app (DocumentCollection), tr (the outermost Transaction the bridge opened — use tr.GetObject / tr.AddNewlyCreatedDBObject, never Commit/Abort it), units (units.ToDrawing(mm), units.ToMm(du), units.Insunits — coordinates are drawing units), ct, log(string), progress(cur,total,msg), args (…). Default usings: … . End with `return <value>;`; ObjectId → {handle,class}, Point3d → {x,y,z}, Entity → {handle,type,layer}. transaction auto|manual|none, dryRun rolls back, one Undo step per run. Fails with isError + diagnostics when … Requires the user to tick 'Allow AI code execution' in the HPRebar MCP Bridge window (command HPMCPBRIDGE) inside AutoCAD."
- `Hosts/Autocad/AutocadContextTool.cs` (`get_autocad_context`, ReadOnly): mô tả `Autocad{insunits, measurement, currentLayout, currentLayer, isModelSpace, isQuiescent, isNamedDrawing}` + selection `{id=handle value, category=layer, name=dxfName}`.
- `Hosts/Autocad/AutocadDocumentResources.cs`: `autocad://document/info`, `autocad://selection`.
- `Hosts/Autocad/AutocadScriptPrompts.cs`: `autocad_query_template` (few-shot: đếm entity trên layer bằng `ed.SelectAll(filter)`; `transaction:"none"`), `autocad_modify_template` (few-shot: dryRun tạo `Polyline` từ `args.List("points")` với `units.ToDrawing`, rồi real run; nhắc `tr.AddNewlyCreatedDBObject`).
- `Program.cs`: `HostProfileFactory` map `"autocad"` → `AutocadHostProfile`; đăng ký 4 host primitives theo profile.
- `ResultFormatter`/`RevitBridgeClient` thông điệp "… not connected. Open {DisplayName} {version} and enable HPRebar MCP Bridge (pipe …)" — lấy `DisplayName` từ profile (phase 0 đã chuẩn bị).
- `appsettings.json`: `"Host": "revit"` mặc định; comment (trong docs) cách đổi qua env.
- Snippet `.mcp.json` (docs + `plan.md`): entry `hprebar-autocad` → cùng exe, `env: {"HPREBAR_MCP_Host":"autocad","HPREBAR_MCP_Bridge__HostVersion":"2026"}`.
Non-functional
- `mcp_call.py` (harness stdio, tạo lại trong scratchpad nếu chưa có — ~60 dòng: spawn exe với env, `initialize`, `tools/list`, `tools/call`) chạy được cho cả 2 host chỉ bằng đổi env.

## Architecture
Xem [architecture.md §1, §5](architecture.md). Không có thay đổi Contracts/Core ở phase này.

## Related code files
- **Tái dùng nguyên:** `Services/ExecuteCodeService`, `Services/ContextService`, `Services/{RevitBridgeClient,ResultFormatter,NdjsonPipeTransport}`, `Tools/{InspectTypeTool,CancelExecutionTool}`, toàn bộ `Registry/*`, `Tools/Registry/*`, `Prompts/ToolifyPrompts` (persona từ profile).
- **Tách ra chung / sửa:** `Program.cs`/`Hosts/HostProfileFactory.cs` (+autocad), `Models/BridgeOptions.cs` (valid versions từ profile — đã phase 0).
- **Viết mới:** `Hosts/Autocad/{AutocadHostProfile,ExecuteAutocadCodeTool,AutocadContextTool,AutocadDocumentResources,AutocadScriptPrompts}.cs`; tests `HostProfileTests` (+autocad: pipe `hprebar-mcp-acad2026`, method `autocad.execute`, tool list chứa `execute_autocad_code` và **không** chứa `execute_revit_code`), `AutocadToolsOverPipeTests` (pipe thật + `FakeRevitExecutor` trả `ContextResult` có `Autocad` → tool trả JSON đúng field).

## Implementation steps
1. `AutocadHostProfile` + factory; test profile.
2. 4 file host-specific; description theo architecture §3.
3. `Program.cs` đăng ký; `ServerInfo.Name`.
4. Tests pipe: `execute_autocad_code` gửi `autocad.execute` với `ExecuteRequest` đúng (mode normalize, args), `get_autocad_context` gửi `autocad.context`; `tools/list` theo host (dùng `McpServerOptions.ToolCollection` + registered names sau build host — kiểm qua `IServiceProvider`).
5. Publish exe; `mcp_call.py` với env autocad: `tools/list` = 12 (4 + 8), `get_autocad_context` với AutoCAD 2026 + bridge phase 2 đang chạy; `execute_autocad_code` `return db.Filename` (none) và dryRun tạo Line; kiểm `runId` + `hint` xuất hiện (registry AutoCAD rỗng nhưng `runs` ghi vào `registry-autocad.db`).
6. Chạy song song: `mcp_call.py` host revit vẫn hoạt động với Revit 2026 (2 server, 2 pipe) → ghi vào `reports/phase-03-dual-host.md`.

## Todo
- [ ] 1 profile · [ ] 2 host files · [ ] 3 Program · [ ] 4 tests · [ ] 5 publish + stdio smoke AutoCAD · [ ] 6 dual-host smoke

## Success criteria
- `dotnet test HPRebar/HPRebar.Mcp.Server.Tests` xanh (+ ≥ 6 test mới).
- `HPREBAR_MCP_Host=autocad mcp_call.py tools/list` = 12 tool đúng tên; `HPREBAR_MCP_Host=revit` = 34 (không đổi).
- `execute_autocad_code` end-to-end qua stdio: none-read OK; dryRun `rolledBack=true`; real run → Line trong drawing; `runId` trả về; `initialize` trả `serverInfo.name = "HPRebar AutoCAD MCP"`.
- Hai server (revit + autocad) chạy cùng lúc trong một phiên `mcp_call.py`/Claude Code không ảnh hưởng nhau.

## Risks
| Risk | Mitigation |
|---|---|
| Description quá dài (token) | giữ ≤ 1 200 ký tự; chi tiết chuyển sang prompt template |
| AI nhầm `Application` (2 namespace) | usings mặc định không có `.Core`; description nói rõ |
| `ContextResult.RevitVersion` gây hiểu nhầm trong JSON trả AI | `ExecuteCodeService`/`ContextService` không lộ field này: `ContextResult` serialize thêm `host`, `hostVersion`; `revitVersion` giữ nhưng docs ghi "legacy" — hoặc `[JsonIgnore]` khi `Host != revit` (quyết định khi implement, ưu tiên ẩn) |

## Security
Annotations giữ như Revit; `execute_autocad_code` `Destructive=true` → host AI hỏi user mỗi lần (Q12). Không lộ path (`SafeText`).

## Next steps
Phase 4: registry theo host + seed AutoCAD để `tools/list` có tool thật và vòng lặp propose/test/publish chạy trên AutoCAD.
