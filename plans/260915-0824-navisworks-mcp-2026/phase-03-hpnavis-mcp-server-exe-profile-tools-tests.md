---
phase: 3
title: "HPNavis.Mcp.Server exe: NavisHostProfile, 4 core tools, prompts, resources, tests, .mcp.json"
status: completed
priority: P1
effort: "6h"
dependencies: [0]
---

# Phase 3: Server exe `HPNavis.Mcp.Server` (net10, stdio) + tests

## Context Links
- [ADR-05 §3](adr/adr-05-navis-plugin-packaging-deploy-identity.md) (định danh) · [ADR-04 §3–4](adr/adr-04-navis-main-thread-busy-heavy-ops-guard-globals.md) (`MaxTimeoutSeconds`, globals, mô tả tool) · [ADR-02 §3](adr/adr-02-navis-transaction-dryrun-and-writable-surface.md) (3 lớp W1/W2/W3 phải xuất hiện trong description)
- Mẫu 1:1: `HPAutoCad/HPAutoCad.Mcp.Server/{Program.cs, Hosts/AutocadHostProfile.cs, Tools/ExecuteAutocadCodeTool.cs, Tools/AutocadContextTool.cs, Prompts/AutocadScriptPrompts.cs, Resources/AutocadDocumentResources.cs, appsettings.json, HPAutoCad.Mcp.Server.csproj}`; tests `HPAutoCad/HPAutoCad.Mcp.Server.Tests/{HostProfileTests.cs, AutocadToolsOverPipeTests.cs, HPAutoCad.Mcp.Server.Tests.csproj}`; `McpShared/HPRebar.Mcp.Server.Core/Bootstrap/McpServerHost.cs`

## Overview
Exe stdio mỏng cho Navisworks: `Program.cs` một dòng `McpServerHost.RunAsync(args, NavisHostProfile.Instance)`, profile Navis, 4 tool core, 2 prompt template, resources `navis://`. Không tham chiếu `Autodesk.*`. Chạy được **độc lập với phase 1–2** (test dùng fake executor qua pipe thật), nên có thể làm song song với phase 2.

## Requirements
- Functional: `tools/list` = 4 core + 8 registry (+ seed sau phase 4); `serverInfo.name = "HPNavis MCP"`; pipe `hpnavis-mcp-2026`; env `HPNAVIS_MCP_*`; registry root `%AppData%\HPNavis\McpServer\`; `MaxTimeoutSeconds = 600`; mô tả `execute_navis_code` nêu: runtime .NET Framework 4.8, globals, đơn vị mm ở biên, bốn lớp W1/W1?/W2/W3, heavy cần user tick (diagnostic `HEAVY`), `none` best-effort (fingerprint), `rolledBack=false` nghĩa là có thể đã persist, `Search` thay vì duyệt cây, clash không thể ngắt, path policy (không UNC).
- Non-functional: description tool core host-specific; 8 registry tool giữ mô tả host-neutral (không sửa Core); tên/schema Revit & AutoCAD không đổi.

## Architecture
```
HPNavis/HPNavis.Mcp.Server/
├── Program.cs                                   return await McpServerHost.RunAsync(args, NavisHostProfile.Instance);
├── Hosts/NavisHostProfile.cs                    HostProfile { HostId navis, ServerName "HPNavis MCP", ProductFolder HPNavis, EnvPrefix HPNAVIS_MCP_, DefaultVersion 2026, ValidVersions [2026], MethodPrefix navis., ExecuteToolName execute_navis_code, ContextToolName get_navis_context, ResourceScheme navis, Categories [Model, Search, Selection, Viewpoint, Clash, Timeliner, Report, Data, Generic], CoreToolNames ×4, ScriptImports NavisImports, ScriptContractSummary (ADR-04 §4 + ADR-02), MaxTimeoutSeconds 600 (phase 0 #10), CliExecutable HPNavis.Mcp.Server.exe }
├── Tools/ExecuteNavisCodeTool.cs                [McpServerToolType] execute_navis_code (Destructive) → ExecuteCodeService
├── Tools/NavisContextTool.cs                    get_navis_context (ReadOnly) → ContextService
├── Prompts/NavisScriptPrompts.cs                navis_query_template (Search/PropertyCategories/units) · navis_review_template (selection set → viewpoint → comment → clash status)
├── Resources/NavisDocumentResources.cs          navis://document/info · navis://selection · navis://models
├── Registry/SeedLibrary/ (phase 4)
├── appsettings.json                             copy AutoCAD; Bridge.HostVersion 2026
└── HPNavis.Mcp.Server.csproj                    mirror HPAutoCad.Mcp.Server.csproj (InvariantGlobalization, IncludeNativeLibrariesForSelfExtract, seeds EmbeddedResource)
HPNavis/HPNavis.Mcp.Server.Tests/
├── NavisHostProfileTests.cs                     pipe/prefix/tool names/categories/MaxTimeoutSeconds/reserved
├── NavisToolsOverPipeTests.cs                   PipeListener(hpnavis-mcp-<ephemeral>, asset **net8** của Core — test net10) + FakeRevitExecutor (linked) → server → execute/context/inspect/cancel round trip, -32001/-32002/-32003 mapping, `execute_navis_code timeoutSeconds=600` được chấp nhận (Revit profile giả → clamp 120), `run_tool` với record 600 s không bị clamp 120, Shape() không có revitVersion/isFamily, có navis block
└── HPNavis.Mcp.Server.Tests.csproj              net10, xunit.v3, links ..\..\McpShared\HPRebar.Mcp.Server.Core.Tests\Fakes\FakeRevitExecutor.cs
```

## Related Code Files
- Create: mọi file trên; `HPNavis/HPNavis.slnx` thêm 2 project.
- Modify (ngoài repo, untracked): `.mcp.json` thêm entry `hprebar-navis` → `HPNavis/output/HPNavis.Mcp.Server/HPNavis.Mcp.Server.exe`, env `HPNAVIS_MCP_Bridge__HostVersion=2026` — **không** sửa hai entry đang chạy. Lệnh publish ghi trong README: `dotnet publish HPNavis/HPNavis.Mcp.Server -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -o HPNavis/output/HPNavis.Mcp.Server`.

## Implementation Steps
1. Copy skeleton từ AutoCAD, đổi tên/namespace `HPNavis.Mcp.Server.*`, viết `NavisHostProfile` (giá trị bảng ADR-05 §3).
2. Mô tả `execute_navis_code` (≤ 1 400 ký tự): globals; ".NET Framework 4.8 runtime — no Span slicing/Random.Shared/async"; mm boundary (`units`); "Navisworks is a review tool: geometry is read-only. Writable (undoable, one Undo entry `MCP: <label>`): selection sets, saved viewpoints, comments, permanent appearance overrides, hidden/required/frozen, clash tests + result status, TimeLiner tasks. Current selection/viewpoint changes may not be undoable. Heavy (user must tick 'Allow heavy operations' in the bridge window; a HEAVY diagnostic tells you when it is off): AppendFile/MergeFile/SaveFile/Export/TestsRunTest — never undoable, cannot be interrupted, up to 600 s, no UNC paths, save the document first. dryRun rolls back only the bridge's own Undo entry; `rolledBack:false` means changes may have persisted. `transaction:none` is best-effort (a fingerprint check). Prefer `new Search{…}.FindAll(doc,false)` over walking `Descendants`. Never open a Transaction yourself."
3. `get_navis_context` mô tả `NavisInfo` fields; resources; prompts.
4. Tests (bảng trên); `dotnet test` xanh; `python McpShared/tools/mcp-call.py <navis exe> tools/list` (bản canonical host-neutral — phase 5 tạo `McpShared/tools/`; tới lúc đó dùng bản AutoCAD) → 12 tool.
5. Publish + `.mcp.json` (user) + smoke với bridge phase 2 nếu đã có: `get_navis_context` thật qua stdio — **đây là lần đầu client net10 (`CurrentUserOnly`, so owner SID) gặp server net48 (`PipeSecurity.SetOwner`)**; cả hai chạy không elevated. Nếu chưa có bridge → ghi CHƯA TEST.

## Todo List
- [x] Skeleton + profile
- [x] 4 tool + prompts + resources
- [x] Tests ≥ 10 (12)
- [x] Publish + `.mcp.json` (`hprebar-navis`, local, không commit) + tools/list = 12
- [x] Report `reports/phase-03-server.md`

## Success Criteria
- [x] `dotnet build HPNavis/HPNavis.Mcp.Server -c Release` xanh; `grep -rn "Autodesk\." … --include=*.csproj` = 0 (chỉ xuất hiện trong string mô tả/prompt).
- [x] `dotnet test HPNavis/HPNavis.Mcp.Server.Tests` → 12 pass, 0 fail, 0 skip.
- [x] `mcp-call.py … tools/list` → đúng 12 tên; `initialize` → `serverInfo.name == "HPNavis MCP"` (isolated registry root).
- [x] Không có bridge: `"Navisworks bridge not connected. Open Navisworks 2026 and enable the HP MCP Bridge (pipe hpnavis-mcp-2026)."` — có tên host, không path máy (+ unit test).
- [x] Revit/AutoCAD `tools/list` không đổi: `McpShared/` không bị chạm trong phase 3 (`git diff 63d9454 -- McpShared/HPRebar.Mcp.Server.Core McpShared/HPRebar.Mcp.Contracts` rỗng); snapshot phase 1 (33/24 isolated root) vẫn đúng.
- [x] Với bridge phase 2 chạy: `run-server-smoke.ps1` 8/8 — `get_navis_context` qua stdio trả `navis` block trong 0.20 s; execute none/dryRun/heavy-off, inspect_type, search_tools; log bridge 0 `UnauthorizedAccess` (client net10 `CurrentUserOnly` ↔ listener net48 `PipeSecurity` khớp owner SID).

## Risk Assessment
- `McpServerHost` quét `[McpServerToolType]` trong `HostAssembly` → phải là exe Navis (`HostAssembly = typeof(NavisHostProfile).Assembly`) — mirror AutoCAD, thấp.
- `ExecuteCodeService` clamp 600 chỉ có sau phase 0 — dependency đã ghi.
- Exe bị lock khi `hprebar-navis` đang chạy trong Claude Code → publish khi MCP tắt (known gap Revit tương tự).
