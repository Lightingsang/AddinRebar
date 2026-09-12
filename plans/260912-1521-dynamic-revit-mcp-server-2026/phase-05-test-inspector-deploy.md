# Phase 05 — Test · Inspector · Deploy

## Context Links
- [architecture.md §6 test projects, §7 lifecycle](architecture.md) · [ADR-02 Consequences: test framing bằng `Pipe` in-memory](adr/adr-02-ipc-named-pipe.md) · [ADR-03 Consequences: test coexistence Dynamo](adr/adr-03-roslyn-in-process-execution.md) · [ADR-04 Consequences: test deny-list / read-only / audit format](adr/adr-04-execute-code-security-model.md)
- NotebookLM Q10 [9-10] Inspector, [22-38] client config (Claude Desktop / VS Code), [35] pitfall path có khoảng trắng, [63-78] in-memory `Pipe` + `WithStreamServerTransport` + `StreamClientTransport` + `McpClient.CreateAsync`, [93-95] breakpoint timeout; Q09 [36-44] `dotnet run` / `PackAsTool`
- `CLAUDE.md` §Build (`dotnet test HPRebar.Core.Tests`, `dotnet run -- pack`, TUnit `Build Project="false"`), §Testing (xUnit v3 vì `Microsoft.Testing.Platform`)
- Verified: `HPRebar/HPRebar.Core.Tests/HPRebar.Core.Tests.csproj:11-19` (xunit.v3 3.1.0, `UseMicrosoftTestingPlatformRunner`); `HPRebar/HPRebar.Tests/HPRebar.Tests.csproj:1-22` (TUnit 1.61.38 + `Nice3point.TUnit.Revit`); `HPRebar/HPRebar.slnx:47-58` (TUnit exclusion); `HPRebar/build/Modules/CreateBundleModule.cs:29` (chỉ `Projects.HPRebar`); `HPRebar/build/Modules/TestProjectModule.cs` (chỉ Core.Tests); `.mcp.json` root (đã có `notebooklm`)
- Skills: `/bs:revit-test`, `/bs:revit-debug`

## Overview
- **Status (thực tế 2026-09-12):** built + verified một phần. Đã làm: `HPRebar.Mcp.Server.Tests` (43 test, đã có từ phase 2/4); `.mcp.json` entry `hprebar-revit` → `HPRebar/output/HPRebar.Mcp.Server/HPRebar.Mcp.Server.exe` (publish single-file 4.9 MB, chạy thật với Revit: progress 1→10 đúng thứ tự, dryRun rollback); `CreateBundleModule` bundle cả `HPRebar` lẫn `HPRebar.McpBridge`; `PublishServerModule` mới (Build.csproj compile OK); `publish/` của bridge Release.R26 có 4 Roslyn DLL + `.addin`; docs `codebase-summary.md`, `system-architecture.md`, `CLAUDE.md` (+AGENTS.md regen), `mcp-architecture.md` đánh dấu reference cũ. Chưa: `HPRebar.McpBridge.Tests` TUnit (transaction paths verified thủ công 15 kịch bản); `dotnet run -- pack` end-to-end (fail ở `Release.R27` do BeamRebar — lỗi có sẵn); Dynamo UI chạy song song; `.vscode/mcp.json` (bỏ, YAGNI).
- **Priority:** P1 · **Status:** pending · **Effort:** 12h
- Test matrix 3 tầng (xUnit server pure/in-memory, TUnit bridge in-process, manual E2E Inspector + Claude Code), client config, đóng gói 2 artefact, F5 smoke, danh sách docs follow-up.

## Key Insights
- Server test không cần Revit: fake `IRevitBridgeClient` (test double trong test project — hợp lệ; cấm mock chỉ áp cho code sản phẩm) + host in-memory `Pipe` (Q10 [63-78]). Tool discovery + annotations + error mapping test được toàn bộ.
- Bridge test cần Revit thật (TUnit R26). Không dùng fixture `.rvt`: `Application.NewProjectDocument(UnitSystem.Metric)` tạo doc trống trong process → tránh vấn đề `HPRebar.Tests/Fixtures/column-stack-2-storey.rvt` không tồn tại. `[VERIFY]` `Nice3point.TUnit.Revit` expose `Application` cho phép `NewProjectDocument`.
- `ScriptGuard`/`ScriptCompiler`/`ScriptCache` nằm ở `HPRebar.McpBridge.Core` (net8.0, không Revit refs) → test xUnit thuần trong `HPRebar.Mcp.Server.Tests` (net10 reference net8 lib hợp lệ), khớp ADR-04 "xUnit, pure". TUnit chỉ còn phần thật sự cần Revit (`ScriptRunner`, guards, serializer với `Element` thật).
- `dotnet run --project` khởi động chậm (restore+build) → host AI có thể timeout init (Q10 [93-95]) → `.mcp.json` dùng `--no-build` sau khi build, hoặc exe publish. Path có khoảng trắng (`F:\1-CONG VIEC\…`) → mỗi arg một phần tử mảng, escape `\\` (Q10 [35]).
- Bundle: `CreateBundleModule` chỉ gom `bin/**/publish` của `Projects.HPRebar` → cần module riêng cho bridge (bundle riêng, ADR-01 cadence khác). `[VERIFY]` Nice3point SDK có tạo `publish/` cho project `IsRepackable=false` (Roslyn DLL copy-local phải nằm trong).
- `TestProjectModule` chỉ chạy Core.Tests → thêm `HPRebar.Mcp.Server.Tests`; TUnit vẫn ngoài pipeline (cần Revit).

## Requirements
**Functional**
- xUnit: ≥ 12 test xanh (`dotnet test HPRebar/HPRebar.Mcp.Server.Tests`).
- TUnit: ≥ 8 test xanh dưới `Debug.R26` (`dotnet test HPRebar/HPRebar.McpBridge.Tests/HPRebar.McpBridge.Tests.csproj -c Debug.R26`) — Revit 2026 cài trên máy.
- Inspector smoke + Claude Code E2E qua `.mcp.json` đi hết 4 tool.
- Artefact: `output/HPRebar.McpBridge.bundle.zip` + `output/HPRebar.Mcp.Server/` (publish) từ `dotnet run -- pack`.
**Non-functional**
- Không bỏ test fail để qua build; test không dùng network; tổng thời gian xUnit < 30s.

## Architecture
**Test matrix**
| Tầng | Project | Framework | Đối tượng | Cần Revit |
|---|---|---|---|---|
| Unit | `HPRebar.Mcp.Server.Tests` (net10) | xUnit v3 3.1.0 | `ScriptGuard` deny-list (≥12 case: mỗi mục deny-list + `await` + allow `System.IO.Path`/`GetType().Name`), `ScriptCompiler` cache hit + diagnostics line/col (references = mscorlib chỉ, globals type giả), `PipeNaming`, NDJSON framing (`NdjsonPipeTransport` với `System.IO.Pipelines.Pipe` hai chiều), `ResultFormatter` error mapping 5 hàng, path strip, validation clamp | ✗ |
| Integration | `HPRebar.Mcp.Server.Tests` | xUnit + MCP SDK in-memory | `tools/list` = 4 tên + annotations; `execute_revit_code` với fake bridge trả `isError` → `IsError=true`; fake ném `IOException` → `McpException` message; `resources/list` 2 URI; `prompts/get` 2 prompt trả ≥2 `ChatMessage`; progress notification forward | ✗ |
| In-process | `HPRebar.McpBridge.Tests` (R25/R26) | TUnit 1.61.38 + `Nice3point.TUnit.Revit` | `ScriptCompiler` với RevitAPI references thật (smoke 1 case `return doc.Title;`); `ScriptRunner` auto commit → `changed.added==1`; dryRun → element không tồn tại sau run; exception → rollback; `none` + modify → `ModificationOutsideTransactionException` hint; read-only guard; timeout `while(!ct…)` → `timedOut`; `ResultSerializer` `ElementId/XYZ/Element/null`; `AuditLogger` dòng đúng format; manual-mode transaction để mở → isError | ✓ |
| E2E manual | Inspector + Claude Code | — | 5 kịch bản phase 4 bước 14 + Dynamo coexistence + theme swap | ✓ |

## Related Code Files
**To create (proposed)**
- `HPRebar/HPRebar.Mcp.Server.Tests/HPRebar.Mcp.Server.Tests.csproj` (copy `HPRebar.Core.Tests.csproj`, TFM net10.0, ref Server + Contracts), `Fakes/FakeRevitBridgeClient.cs`, `McpServerHostFixture.cs` (Pipe pair + host + client), `ScriptGuardTests.cs`, `ScriptCompilerTests.cs`, `PipeNamingTests.cs`, `NdjsonPipeTransportTests.cs`, `ResultFormatterTests.cs`, `ToolDiscoveryTests.cs`, `ExecuteRevitCodeToolTests.cs`, `ResourcesAndPromptsTests.cs`
- `HPRebar/HPRebar.McpBridge.Tests/HPRebar.McpBridge.Tests.csproj` (copy `HPRebar.Tests.csproj`, ref Bridge), `TestsConfiguration.cs` (copy từ `HPRebar.Tests`), `EmptyDocumentFixture.cs` (`NewProjectDocument`), `ScriptCompilerRevitReferencesTests.cs`, `ScriptRunnerTransactionTests.cs`, `ScriptRunnerTimeoutTests.cs`, `RevitContextReaderTests.cs`, `ResultSerializerTests.cs`, `AuditLoggerTests.cs`
- `HPRebar/build/Modules/CreateBridgeBundleModule.cs` (clone `CreateBundleModule` với `Projects.HPRebar_McpBridge`, tên bundle `HPRebar.McpBridge.bundle`), `HPRebar/build/Modules/PublishServerModule.cs` (`dotnet publish` server → `output/HPRebar.Mcp.Server/`)
- `.vscode/mcp.json` (repo root, Q10 [36-38])
**To modify (proposed)**
- `HPRebar/HPRebar.slnx` — 2 test project (server tests map plain; bridge tests khối như `HPRebar.Tests:52-58`)
- `HPRebar/build/Program.cs` — `pack` thêm 2 module; `HPRebar/build/Modules/TestProjectModule.cs` — thêm `Projects.HPRebar_Mcp_Server_Tests`
- `.mcp.json` (root) — thêm entry `hprebar-revit`
- `HPRebar/HPRebar.Mcp.Server/HPRebar.Mcp.Server.csproj` — `PackAsTool`/`ToolCommandName hprebar-mcp` (chỉ khi chọn dotnet tool — xem bước 9)
**To delete:** none

## Implementation Steps
1. `HPRebar.Mcp.Server.Tests` csproj: net10.0, `xunit.v3` 3.1.0 + `xunit.runner.visualstudio` 3.1.5, `UseMicrosoftTestingPlatformRunner true`, `OutputType Exe`, ref `..\HPRebar.Mcp.Server` + Contracts. Server `Program.cs` không test trực tiếp; `McpServerHostFixture` tự dựng `Host.CreateEmptyApplicationBuilder(null)` + `AddSingleton<IRevitBridgeClient>(fake)` + `AddMcpServer().WithStreamServerTransport(clientToServer.Reader.AsStream(), serverToClient.Writer.AsStream()).WithTools<ExecuteRevitCodeTool>()…WithResources<…>().WithPrompts<…>()` + `StreamClientTransport` + `McpClient.CreateAsync` (Q10 [63-72]).
2. `FakeRevitBridgeClient : IRevitBridgeClient`: script hoá response theo method (`Func<string, object?, JsonRpcResponse>`), ghi lại request; chế độ `ThrowNotConnected`.
3. Test framing: `NdjsonPipeTransport` nhận `Stream` (thêm ctor internal nhận stream để test — `InternalsVisibleTo("HPRebar.Mcp.Server.Tests")`): 2 message một lần ghi, message 4 MB+1 → đóng, notification không `id` → event, response `id` lạ → log bỏ qua.
4. Test tool: `ListToolsAsync` → tên `execute_revit_code|get_revit_context|inspect_type|cancel_execution`; annotations `Destructive/ReadOnly/Idempotent` đúng bảng §3 (`[VERIFY]` property path trong SDK `Tool.Annotations`); `CallToolAsync("execute_revit_code", {code:""})` → `IsError` "empty"; code 40 KB → `IsError` limit; fake `isError` diagnostics → `IsError` + text chứa `CS0103`; fake `-32002` → `IsError` busy; fake throw `IOException` → `McpException` chứa `hprebar-mcp-r2026`.
5. `HPRebar.McpBridge.Tests` csproj: copy `HPRebar.Tests.csproj` đổi ref → `..\HPRebar.McpBridge`; `Configurations` R25/R26; `.slnx` `Build Project="false"`. `EmptyDocumentFixture`: `app.NewProjectDocument(UnitSystem.Metric)`, `Close(false)` sau class. Test `ScriptRunner` gọi trực tiếp trên thread test (TUnit Revit chạy trong Revit context — `[VERIFY]` Nice3point.TUnit.Revit đảm bảo API context cho Transaction).
6. TUnit cases theo bảng; timeout test dùng `timeoutSeconds=5` → assert `timedOut && durationMs < 7000`.
7. Inspector: `npx @modelcontextprotocol/inspector dotnet run --project HPRebar/HPRebar.Mcp.Server/HPRebar.Mcp.Server.csproj` — checklist: 4 tool badge, 2 resource đọc được khi Revit + listener bật, 2 prompt render, History không có dòng non-JSON trên stdout.
8. Client config — `.mcp.json` root thêm:
   ```json
   "hprebar-revit": { "command": "dotnet", "args": ["run", "--no-build", "--project", "F:\\1-CONG VIEC\\05-AI\\01_Revit\\02_Csharp\\AddinRebar\\HPRebar\\HPRebar.Mcp.Server\\HPRebar.Mcp.Server.csproj"] }
   ```
   (cần `dotnet build` trước; path tuyệt đối máy dev → cân nhắc `${workspaceFolder}` không được Claude Code hỗ trợ → ghi README cách sửa path). `.vscode/mcp.json` `servers.hprebar-revit {type:"stdio", …}` (Q10 [36-38]); `%APPDATA%\Claude\claude_desktop_config.json` hướng dẫn trong docs, không commit (ngoài repo). Quyết định client chính → `plan.md` #5.
9. Packaging server — **quyết định:** v1 `dotnet publish HPRebar/HPRebar.Mcp.Server -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o output/HPRebar.Mcp.Server` (`PublishServerModule`); `.mcp.json` "prod" trỏ `output/HPRebar.Mcp.Server/HPRebar.Mcp.Server.exe`. `PackAsTool` (Q09 [39-44]) khi có feed NuGet — backlog.
10. Packaging bridge: `CreateBridgeBundleModule` (clone, `Projects.HPRebar_McpBridge`, kiểm `publish/` chứa `Microsoft.CodeAnalysis.CSharp.Scripting.dll` + `.addin`); `Program.cs` `pack` thêm module. Chạy `dotnet run -- pack` từ `HPRebar/build/` → 2 zip.
11. `TestProjectModule` thêm server tests; `dotnet run -- test` xanh.
12. F5 smoke (`/bs:revit-debug`): build `Debug.R26` (Revit đóng) → F5 → ribbon → bật listener → Claude Code `claude mcp list` thấy `hprebar-revit` → hỏi "đếm số tường trong model" → `get_revit_context` + `execute_revit_code` (host hỏi approve) → kết quả; kiểm audit file; kiểm Undo history.
13. Dynamo coexistence manual: Revit 2026 mở Dynamo (Manage → Dynamo) trước và sau khi bật bridge + chạy 1 script; không `FileLoadException`/`TypeLoadException` trong `%LocalAppData%\HPRebar\McpBridge\logs` và journal Revit. Ghi kết quả `plan.md` #2. Nếu xung đột → escalate ADR-03 alt 1.
14. Docs follow-up (không làm trong plan này, giao `docs-manager` sau merge): `docs/system-architecture.md` (thêm topology 2 process), `docs/codebase-summary.md` (3+2 project), `CLAUDE.md` bảng layout + lệnh build/test mới + `.mcp.json` entry, `docs/mcp-architecture.md` (thay/đánh dấu reference TS cũ — `plan.md` #12), `docs/project-changelog.md`.

## Todo List
- [ ] Server tests project + fixture + fake + 6 file test
- [ ] Bridge TUnit project + `EmptyDocumentFixture` + 7 file test
- [ ] `.slnx` 2 test project; `TestProjectModule` + `pack` modules
- [ ] Inspector checklist; `.mcp.json` + `.vscode/mcp.json`
- [ ] `PublishServerModule` + `CreateBridgeBundleModule`; `dotnet run -- pack` ra 2 artefact
- [ ] F5 smoke Claude Code E2E; Dynamo coexistence; ghi kết quả plan.md #2/#5
- [ ] Docs follow-up ticket

## Success Criteria
- `dotnet test HPRebar/HPRebar.Mcp.Server.Tests` → exit 0, ≥ 12 passed, 0 skipped, < 30s.
- `dotnet test HPRebar/HPRebar.McpBridge.Tests/HPRebar.McpBridge.Tests.csproj -c Debug.R26` → exit 0, ≥ 8 passed, 0 skipped (không skip vì thiếu fixture).
- `dotnet test HPRebar/HPRebar.Core.Tests` vẫn 334 passed (không regress).
- `cd HPRebar/build && dotnet run -- test` → xanh; `dotnet run -- pack` → `output/HPRebar.McpBridge.bundle.zip` chứa `Contents/2026/HPRebar.McpBridge/Microsoft.CodeAnalysis.CSharp.Scripting.dll`; `output/HPRebar.Mcp.Server/HPRebar.Mcp.Server.exe` tồn tại.
- `claude mcp list` (Claude Code, cwd repo) liệt kê `hprebar-revit` connected; 4 tool gọi được E2E với Revit 2026 mở.
- Dynamo mở + chạy script → 0 exception load assembly trong log.

## Risk Assessment
| Risk | L×I | Mitigation |
|---|---|---|
| `Nice3point.TUnit.Revit` không cho `NewProjectDocument` / không có API context cho Transaction | M×H | `[VERIFY]` sớm bước 5; fallback: fixture `.rvt` trống tạo tay 1 lần từ Revit (commit vào `Fixtures/`) — ghi `plan.md` #6 |
| MCP SDK in-memory API (`WithStreamServerTransport`, `StreamClientTransport`) đổi tên | M×M | Đọc package XML doc; test compile là gate |
| `dotnet run` init chậm → Claude Code timeout | H×M | `--no-build` + build trước; hoặc exe publish |
| Bundle module không tìm thấy `publish/` cho `IsRepackable=false` | M×M | `[VERIFY]` sau build Release.R26; fallback gom `bin/Release.R26/` trực tiếp |
| Dynamo xung đột Roslyn | M×H | Manual test bước 13; escalate, không tự đổi ADR |
| Test TUnit chạy trong Revit chậm (mỗi lần launch Revit) | H×L | Gom test nhiều case/class; xUnit gánh phần pure |

## Security Considerations
- Test không ghi vào audit thật: `AuditLogger` nhận đường dẫn qua ctor → test dùng temp dir.
- `.mcp.json` chứa đường dẫn máy dev — không chứa secret; `claude_desktop_config.json` ngoài repo.
- E2E dùng doc trống/mẫu, không mô hình khách hàng; opt-in bật tay cho từng phiên test.

## Next Steps
- Sau merge: docs-manager cập nhật 5 tài liệu bước 14; `project-changelog.md` entry.
- Backlog: `PackAsTool`/`dnx` (Q09 [45-46]); multi-instance pipe naming; `notifications/resources/updated`; R27 khi HPRebar xanh.
