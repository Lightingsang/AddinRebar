---
title: "Dynamic Revit MCP Server 2026"
description: "MCP server C# (net10, stdio) + bridge add-in Revit 2026 (net8) chạy C# script động qua Roslyn, IPC Named Pipe JSON-RPC 2.0"
status: pending
priority: P2
effort: 52h
branch: RebarVersion1
tags: [revit, mcp, roslyn, named-pipe, wpf, nice3point]
created: 2026-09-12
---

# Dynamic Revit MCP Server 2026 — Plan

**Ngày:** 2026-09-12 · **Status:** Phase 0–5 built; verified end-to-end trong Revit 2026 qua `.mcp.json` exe; còn 3 mục phase 5 ghi trong bảng. **Phase 6–9 built + verified live (2026-09-12): AI BIM tự sinh & ghi nhớ tool** — thiết kế: [research/ai-bim-self-extending-tool-registry-design.md](research/ai-bim-self-extending-tool-registry-design.md) · **Branch:** `RebarVersion1` · Template: Stack-Aware 6-phase (Revit add-in)

## Executive summary
- Hai tiến trình: `HPRebar.Mcp.Server` (net10 console, stdio MCP, SDK `ModelContextProtocol`) ↔ `HPRebar.McpBridge` (add-in Nice3point net8, R25/R26) qua Named Pipe `hprebar-mcp-r{ver}` JSON-RPC 2.0 NDJSON; `HPRebar.Mcp.Contracts` netstandard2.0 giữ DTO chung.
- Tool surface tối thiểu: 4 tool (`execute_revit_code`, `get_revit_context`, `inspect_type`, `cancel_execution`) + 2 resource + 2 prompt. **Từ phase 7:** thêm 8 meta-tool registry + tool động từ Tools Library (tool = dữ liệu `tool.json/code.cs/examples.json`, chạy qua đường `revit.execute` chung, không bao giờ là command native trong Revit). AI gửi C# snippet → bridge compile Roslyn trên pipe thread → chạy trên Revit thread qua ExternalEvent trong `TransactionGroup "MCP: <label>"`.
- Không sandbox: 9 lớp defense-in-depth (opt-in OFF mặc định, `Destructive=true`, deny-list, audit log, pipe ACL, Undo group).

## Design of record
- Architecture: [architecture.md](architecture.md) — component/sequence §1-2, tool surface §3, schema §4, IPC §5, layout §6, lifecycle §7
- ADR: [01 topology](adr/adr-01-two-process-topology.md) · [02 IPC](adr/adr-02-ipc-named-pipe.md) · [03 Roslyn](adr/adr-03-roslyn-in-process-execution.md) · [04 security](adr/adr-04-execute-code-security-model.md)
- Research: [NotebookLM MCP C#](research/notebooklm-mcp-csharp-report.md) · [Roslyn in Revit](research/roslyn-scripting-in-revit-report.md) · [Bridge reference](research/revit-bridge-reference-report.md)

## Phases
| # | File | Status | Depends on | Effort |
|---|---|---|---|---|
| 0 | [phase-00-scaffold-server-bridge-contracts.md](phase-00-scaffold-server-bridge-contracts.md) | built (2026-09-12) — 4 project, gate R26/R23/Release.R26 xanh, Inspector `tools/list`=[] | — | 4h |
| 1 | [phase-01-multi-version-and-target-frameworks.md](phase-01-multi-version-and-target-frameworks.md) | built (2026-09-12) — PipeNaming, BridgeOptions, R25+R26 xanh | 0 | 2h |
| 2 | [phase-02-architecture-tools-resources-prompts-ipc.md](phase-02-architecture-tools-resources-prompts-ipc.md) | built + tested (2026-09-12) — 10 xUnit pipe round-trip pass, Inspector 4 tool/2 res/2 prompt | 1 | 12h |
| 3 | [phase-03-bridge-status-window-wpf.md](phase-03-bridge-status-window-wpf.md) | built + **verified trong Revit 2026** (cửa sổ mở từ ribbon, opt-in bật thật, stop/start listener) | 2 | 6h |
| 4 | [phase-04-revit-api-execution-transactions-security.md](phase-04-revit-api-execution-transactions-security.md) | built + tested + **verified trong Revit 2026**: 13 kịch bản execute thật (read, dryRun rollback, commit thật, exception rollback, none+modify hint, guard, compile error, cancel_execution, timeout) | 2 | 16h |
| 5 | [phase-05-test-inspector-deploy.md](phase-05-test-inspector-deploy.md) | built + verified (2026-09-12): `.mcp.json` entry → exe publish single-file chạy thật với Revit; bundle module 2 add-in + PublishServerModule; docs/CLAUDE.md/AGENTS.md. Chưa: TUnit in-Revit, `dotnet run -- pack` end-to-end (chặn bởi Release.R27 có sẵn), Dynamo UI chạy song song | 3 + 4 | 12h |
| 6 | [phase-06-typed-tools-from-reference-repo.md](phase-06-typed-tools-from-reference-repo.md) | built + tested (2026-09-12) — `args` slot (`ScriptArgs`), `revit.analyze`, 21 seed tool records; 119 xUnit pass incl. Revit-API compile check of every seed; live verify → phase 9 | 4 + 5 | 14h |
| 7 | [phase-07-tool-registry-core.md](phase-07-tool-registry-core.md) | built + tested (2026-09-12) — Tools Library + SQLite/FTS5 + ToolManager + dynamic MCP tools; 142 xUnit; published exe: `tools/list` = 28, editing tool.json on disk → `list_changed` live; seeds in-Revit → phase 9 | 6 | 20h |
| 8 | [phase-08-auto-tool-generation-and-memory.md](phase-08-auto-tool-generation-and-memory.md) | built + tested (2026-09-12) — runId/hint, `get_run`, propose/test/publish/manage, gate manual/auto + review file, CLI `registry approve`, prompt `toolify_run`, resources; 158 xUnit; exe: 33 tools, lifecycle verified over stdio (bridge offline paths); in-Revit loop → phase 9 | 7 | 16h |
| 9 | [phase-09-verify-live-and-docs.md](phase-09-verify-live-and-docs.md) | **verified (2026-09-12)** — 3 kịch bản live trong Revit 2026 pass (HIT · MISS→propose→test→approve→HIT · HỎNG→quarantine→restore), 21 seed smoke (dryRun), 3 lỗi tìm ra & sửa (import `ScriptArgs`, `RoomTagType` collector, seed upgrade); docs + ADR-05/06 + CLAUDE.md/AGENTS.md — `reports/phase-09-live-verify.md` | 7 + 8 | 8h |

File ownership 3 ∥ 4 disjoint — 3: `McpBridgeCommand.cs`, `View/`, `ViewModel/McpBridgeStatusViewModel.cs`, `Service/ThemeSwitcher.cs`, `Resources/Themes/Theme.xaml`, csproj XAML links · 4: `Application.cs`, `McpBridgeExternalEventHandler.cs`, `McpBridge.Core/Scripting/*`, `Service/ScriptRunner.cs`, `Service/RevitContextReader.cs`, `Service/TypeInspector.cs`, `Service/AuditLogger.cs`, `Service/ResultSerializer.cs`, `Model/ScriptGlobals.cs`.

## Key decisions
- 2 tiến trình, 4 project (`Contracts` netstandard2.0 · `Server` net10 · `McpBridge.Scripting` net8 Revit-free · `McpBridge` add-in); bridge là add-in riêng, không phải feature folder trong `HPRebar/HPRebar/` → ADR-01.
- Named Pipe + JSON-RPC 2.0 NDJSON, trần 4 MB/message; bridge `-32001..-32003` → `CallToolResult.IsError`, còn lại → `McpException` → ADR-02.
- Roslyn compile trên pipe thread, run trên Revit thread; policy `auto|manual|none` + `dryRun`; timeout cooperative 30s (5–120) → ADR-03.
- Opt-in "Allow AI code execution" OFF mỗi lần mở Revit; deny-list `CSharpSyntaxWalker`; audit `%AppData%\HPRebar\McpBridge\audit-*.log` → ADR-04.
- Bridge chỉ `Debug/Release.R25;R26` (net8). R23/R24 (net48) và R27 (net10, HPRebar chưa compile) ngoài scope v1 → phase-01.
- Bridge DI mode `disabled` (khớp HPRebar hiện tại — `Application.cs` dùng static `Log.Logger`, không container); server dùng Generic Host DI → phase-00.

## Top-5 risks
| Risk | L×I | Mitigation |
|---|---|---|
| ~~Roslyn load `HPRebar.McpBridge.dll` lần 2 → `ScriptGlobals` type identity lệch~~ **ĐÃ LOẠI (2026-09-12):** self-check trong Revit 2026 compile+run OK với `RegisterDependency` | — | Giữ `ScriptingSelfCheck` ở startup làm canary |
| `Microsoft.CodeAnalysis` xung đột Dynamo/pyRevit trong Revit | M×H | Bridge ALC riêng (`EnableDynamicLoading`); manual coexistence test phase 5; fallback ADR-03 alt 1 (compile trong server) |
| Script không hợp tác `ct` → Revit treo tới khi xong | M×M | Deny `await`/`Thread`; watchdog + `ct`; nói rõ trong tool description |
| `ModelContextProtocol` 2.2.0 lệch sách 1.3.0 ở phần chưa verify (Resources/Prompts attributes, `IProgress`) | M×M | Đã verify Tools/builder/`McpException` khớp; phần còn lại lộ ngay khi compile phase 2 |
| stdout bị ô nhiễm → MCP client crash | L×H | `AddConsole(LogToStandardErrorThreshold=Trace)` theo SDK sample; Inspector smoke phase 5 |

## Quyết định Claude tự chốt (assumptions — đổi được nếu bạn muốn)
| # | Vấn đề | Chốt | Lý do |
|---|---|---|---|
| 1 | Multi-instance Revit cùng version | v1: pipe cố định `hprebar-mcp-r2026`; instance 2 báo lỗi trong status window | Hiếm gặp; PID suffix + discovery file để v2 |
| 2 | Roslyn ↔ Dynamo ALC | **Xác nhận một phần 2026-09-12:** journal Revit ghi `Microsoft.CodeAnalysis*` load vào context `HPREBAR.MCPBRIDGE` (ALC riêng), Dynamo For Revit load cùng phiên không conflict; chưa chạy Dynamo UI song song với script | ALC isolation hoạt động như thiết kế |
| 3 | R25 cùng R26 | Build cả 2 (cùng net8, miễn phí); **chỉ verify runtime R26** trong MVP; R25 ghi "build-only" | Máy dev có 2025+2026 nhưng ưu tiên 2026 |
| 4 | Package versions | `ModelContextProtocol` 2.2.0 · `Microsoft.Extensions.Hosting` 10.0.12 · `Microsoft.CodeAnalysis.CSharp.Scripting` 5.9.0 (verified NuGet 2026-09-12) | Bản mới nhất; API Tools/builder verify khớp sample SDK |
| 5 | Client config | `.mcp.json` repo (Claude Code) là chính; kèm snippet Claude Desktop + `.vscode/mcp.json` trong docs | Bạn đang dùng Claude Code |
| 6 | TUnit fixture | `Application.NewProjectDocument(UnitSystem.Metric)` thay `.rvt`; verify ở phase 5 | Tránh lặp lỗi fixture thiếu của `HPRebar.Tests` |
| 7 | Code bridge không đụng Revit | Tách `HPRebar.McpBridge.Core` (net8, không Revit): Pipe/ + Model/ + Scripting/ → xUnit thuần; bridge add-in chỉ giữ Host, Handler, Runner, UI | Rule CLAUDE.md: pure logic không đụng Revit API; đã chứng minh bằng 10 test pipe round-trip |
| 8 | Host builder | `Host.CreateApplicationBuilder` + `AddConsole(LogToStandardErrorThreshold=Trace)` theo SDK sample chính thức, `ContentRootPath=AppContext.BaseDirectory` | Sample verified; appsettings tự nạp |
| 9 | `await` trong script | Deny (ADR-04 lớp 3) | Continuation nhảy thread → gọi Revit API sai thread/deadlock |
| 10 | Persist settings | Chỉ `AutoStartListener` persist (`%AppData%\HPRebar\McpBridge\settings.json`); `ExecutionEnabled` không bao giờ persist | Opt-in phải reset mỗi phiên (ADR-04 lớp 1) |
| 11 | Packaging | Bundle riêng `HPRebar.McpBridge.bundle` + server publish riêng | ADR-01: cadence độc lập |
| 12 | `docs/mcp-architecture.md` (TS/TCP tham khảo) | Thay bằng kiến trúc này ở phase 5 docs update | Tránh 2 kiến trúc mâu thuẫn trong docs |
| 13 | `notifications/resources/updated` | Nice-to-have; bỏ nếu SDK 2.2.0 không có API thuận tiện | Không ảnh hưởng tool cốt lõi |
| 14 | Journal replay / doc không active | Ngoài scope v1 | YAGNI |

## Cần người dùng quyết định
- Không có quyết định kỹ thuật nào chờ. Chỉ cần **xác nhận bắt đầu implementation (phase 0)** — bước này tạo project/code thật trong `HPRebar/`.
