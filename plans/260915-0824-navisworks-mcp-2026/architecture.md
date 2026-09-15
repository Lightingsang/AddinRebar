# HPNavis MCP Bridge 2026 — Architecture

Ngày 2026-09-15 · planned · revised sau red-team 2026-09-15 · nguồn: [evidence](research/evidence-on-machine-2026-09-15.md), ADR-01…05.

## 1. Bốn folder top-level, chiều phụ thuộc một chiều

```
HPRebar/   (Revit add-in + Revit MCP)   ──┐
HPAutoCad/ (AutoCAD MCP)                 ──┼──▶ McpShared/  (HPRebar.Mcp.Contracts netstandard2.0 · HPRebar.McpBridge.Core net8.0;net48 · HPRebar.Mcp.Server.Core net10)
HPNavis/   (Navisworks MCP — plan này)   ──┘        ▲ không bao giờ tham chiếu Autodesk.*; host vào qua IHostProfile / IBridgeExecutor / GuardProfile / AnalyzerProfile
```
Không MCP → MCP. `HPNavis/` chỉ `ProjectReference ..\McpShared\*`.

## 2. Hai tiến trình, một engine — điểm khác duy nhất: runtime của bridge

```
Claude Code ──stdio──▶ HPNavis.Mcp.Server.exe (net10)            ──named pipe hpnavis-mcp-2026, JSON-RPC 2.0 NDJSON──▶ HPNavis.McpBridge.dll (net48, trong Roamer.exe)
                       Program: McpServerHost.RunAsync(args, NavisHostProfile)                                       EventWatcherPlugin.OnLoaded → BridgeEntry.Start
                       4 core tool · 8 registry tool · 12 seed · navis:// resources · prompts                          AssemblyResolve (allow-list) → Roslyn 5.9 (netstandard2.0) → ScriptGuard(Navis) → ScriptCompiler
                       registry %AppData%\HPNavis\McpServer\ (registry.db + tools-library)                            → MainThreadQueue ← Application.Idle (+PostMessage WM_NULL wake)
                                                                                                                       → NavisHeavyGate pre-pass (W2 khi heavy OFF, path policy) → NavisScriptRunner: before=NextUndo → BeginTransaction("MCP: label") → script → Commit() [→ Rollback() CHỈ khi NextUndo == label — dryRun/lỗi/timeout/none-modified]
                                                                                                                       → NavisResultSerializer (mm) → ExecuteResult → audit %AppData%\HPNavis\McpBridge\audit\
```

## 3. Tái dùng vs mới (theo file)

| Lớp | Dùng lại nguyên văn | Thêm additive (McpShared) | Mới (HPNavis/) |
|---|---|---|---|
| Contracts | `JsonRpcEnvelope`, `ExecuteRequest/Result`, `ContextResult`, `PipeNaming.For(host,ver)` (nhánh mặc định đã đúng) | `PipeNaming.NavisHost`, `JsonRpcMethods.NavisPrefix`, `HostScriptContracts.NavisImports/NavisGlobals`, `ContextResult.Navis : NavisInfo` | — |
| Bridge Core | `PipeListener`, `RequestDispatcher`, `MainThreadQueue`, `McpBridgeHost`, `ScriptGuard`, `ScriptCompiler`, `ScriptAnalyzer`, `ScriptArgs`, `ScriptUnits`, `TypeInspector`, `AuditLogger`, `BridgeSettings(Store)`, `McpBridgeStatusViewModel` — **tất cả không đổi hành vi** | TFM `net48` + Polyfill; `#if NET48` ×2 (pipe ACL, clock); `IReadOnlySet`→`IReadOnlyCollection` (net48 only); `GuardProfile.Navis`; `AnalyzerProfile.Navis` — **không** đụng `AuditEntry`, `BridgeSettings`, `IMcpBridgeRunner`, VM | `HPNavisBridgePlugin` (EventWatcher), `HPNavisWindowPlugin` (AddIn menu), `PluginAssemblyResolver` (allow-list), `BridgeEntry`, `NavisMainThreadExecutor` (+ cờ heavy in-memory, context-busy), `NavisScriptRunner` (`RollbackOwn`), `NavisChangeCounter` (fingerprint), `NavisHeavyGate` (pre-pass W2 + path policy + clamp), `NavisResultSerializer`, `NavisContextReader`, `NavisScriptGlobals`, `NavisApp`, `ScriptingSelfCheck` (+ kiểm `Assembly.Location`), `NavisBridgeStatusViewModel` (bao Core VM), `NavisBridgeStatusView` (WPF, 2 checkbox). **Không** Ribbon/theme switcher trong MVP |
| Server Core | bootstrap, pipe client, `ExecuteCodeService`, `ContextService` (Shape đã per-host), registry engine, 8 meta tool, CLI | `IHostProfile.MaxTimeoutSeconds` (default 120) dùng ở `ExecuteCodeService`, `ToolManager.RunAsync`, `ToolValidator` | `NavisHostProfile` (600 s), `ExecuteNavisCodeTool`, `NavisContextTool`, `NavisScriptPrompts`, `NavisDocumentResources`, `Registry/SeedLibrary/**` (12) |
| Tests | `FakeRevitExecutor`, `ScriptGuardTests`, `MainThreadQueueTests` (link) | `HPRebar.McpBridge.Core.Net48Tests` (link 2 file + `ScriptCompilerNet48Tests` + `PipeListenerNet48Tests`) | `HPNavis.McpBridge.Tests` (net48: gate/counter/rollback/clamp + **seed compile-check**), `HPNavis.Mcp.Server.Tests` (net10: profile, pipe, seed structure) |
| Harness | `mcp-call.py`, `mcp-session.py` → bản canonical `McpShared/tools/` (phase 5) | `McpShared/tools/` | `HPNavis/tools/harness/*` (host-specific: `live-verify.py`, `pipe-scenarios.py`, `*.ps1`) |

## 4. Tool surface (24 sau phase 4)

4 core (`execute_navis_code` Destructive, `get_navis_context` ReadOnly, `inspect_type`, `cancel_execution`) · 8 registry (host-neutral) · **12 seed** = 8 read-only (`get_model_info, get_selected_item_properties, find_items_by_property, list_selection_sets, list_viewpoints, get_clash_results, get_timeliner_tasks, summarize_by_category`) + 3 ghi nhẹ W1 (`create_selection_set_from_search, create_viewpoint, override_color_by_search`) + 1 heavy W2 (`create_and_run_clash_test`, `tags:["heavy"]`, seed-only). Resources `navis://document/info|selection|models`, `registry://tools[/{name}]`. Prompts `navis_query_template`, `navis_review_template`, `toolify_run`.

## 5. Chính sách an toàn (defense-in-depth, không sandbox)

Opt-in "Allow AI code execution" OFF mỗi lần mở Navisworks, không persist → opt-in thứ hai "Allow heavy operations" (cờ in-memory trên executor, chỉ bật khi cái trên bật) → pre-pass `NavisHeavyGate` (W2 khi heavy OFF, UNC/thư mục HPNavis luôn) + `ScriptGuard` deny-list chung + `GuardProfile.Navis` (W3, Forms/MessageBox/Win32, Automation/Interop/ComApi, `System.Data`/`Document.Database`, `System.Linq.Expressions`/`Delegate`, `BeginTransaction`/`Transaction`) → timeout cooperative 5–120 s (600 s heavy, qua profile) → mọi run có edit = 1 undo entry `MCP: <label>`; lỗi/timeout/dryRun → `Rollback()` **chỉ khi** entry đó là của bridge → audit JSON-lines (heavy: `started` + `[heavy]`) → pipe ACL owner = user hiện tại (`PipeSecurity.SetOwner` trên net48). **Mô hình tin cậy:** hàng rào chống tai nạn, không chống agent hợp tác trên cùng desktop (ADR-04 §6).

## 6. Bảng so sánh ba host (điểm khác cần nhớ)

| | Revit 2026 | AutoCAD 2026 | Navisworks 2026 |
|---|---|---|---|
| Runtime bridge | .NET 8 | .NET 8 (ALC riêng) | **.NET Framework 4.8** (AssemblyResolve) |
| Marshal | `ExternalEvent` | `Application.Idle` + `IsQuiescent` | `Application.Idle` + quiescence composite (Progress events, modal check, `IsActiveTransaction`) |
| Rollback | `TransactionGroup.RollBack()` (scoped) | outer `Transaction.Abort()` (scoped) | **`Commit()` rồi `Document.Rollback()` — undo toàn document, chỉ gọi khi `NextUndo == "MCP: <label>"`** |
| Đơn vị API | feet | INSUNITS | `Document.Units` (đã quy đổi model ghép) |
| Ghi được | geometry + params | geometry + tables | **metadata review** (W1), current selection/viewpoint chưa rõ undo (W1?), file/clash = heavy (W2), SQL/ComApi/transaction riêng cấm (W3) |
| Deploy | `.addin` `%AppData%\Autodesk\Revit\Addins\` | bundle `ApplicationPlugins` | thư mục `%AppData%\Autodesk\Navisworks Manage 2026\Plugins\HPNavis.McpBridge\` |
| API ref | NuGet Nice3point | NuGet AutoCAD.NET | **thư mục cài** (registry key); seed compile-check trong test net48 — không build được khi không cài |
| Pipe | `hprebar-mcp-r2026` | `hpautocad-mcp-2026` | `hpnavis-mcp-2026` |
