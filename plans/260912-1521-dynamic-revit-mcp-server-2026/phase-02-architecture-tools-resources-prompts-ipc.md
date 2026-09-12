# Phase 02 — Architecture: Tools · Resources · Prompts · IPC

## Context Links
- [architecture.md §1 component, §3 tool surface, §4 schema, §5 IPC envelope, §6 layout](architecture.md)
- [ADR-02 IPC](adr/adr-02-ipc-named-pipe.md) (framing, error map, correlation, timeout/reconnect) · [ADR-04 lớp 2 annotations, lớp 8 pipe ACL, lớp 9 error hygiene](adr/adr-04-execute-code-security-model.md)
- NotebookLM Q04 (tool attributes, schema, return types), Q05 [8-16, 30-35] (resources), Q06 [16-21] (prompts `ChatMessage[]`), Q08 (DI lifetimes, `IOptions`, `ILogger<T>` stderr), Q09 [9-17] (layout, `snake_case`), Q11 [3-8] (`IProgress<ProgressNotificationValue>`), Q12 [5-8, 226-227] (annotations, actionable errors)
- `research/revit-bridge-reference-report.md` §What to Avoid (8 KB buffer không framing, hardcoded port, blocking `WaitOne`)
- Verified pattern: `HPRebar/HPRebar/ColumnRebar/ColumnRebarRequest.cs:13-34` (`TaskCompletionSource` + `RunContinuationsAsynchronously`); `ColumnRebarViewModel.cs:19-30` (runner interface trong `ViewModel/`)
- `CLAUDE.md` §Feature Folder Convention (namespace explicit, `View` shadow → alias)

## Overview
- **Priority:** P1 · **Status:** built + tested 2026-09-12 (xUnit 10/10 pipe round-trip với fake executor; Inspector CLI verify tool/resource/prompt; chưa chạy trong Revit) · **Effort:** 12h
- Dựng toàn bộ xương: Contracts records, server tools/resources/prompts + `RevitBridgeClient` (NDJSON pipe client), bridge `PipeListener` + `RequestDispatcher` + interface `IRevitExecutor` (phase 4 hiện thực) + `IMcpBridgeRunner` (phase 3 tiêu thụ). Kết thúc phase: server ↔ bridge nói chuyện được `revit.ping`; `execute` trả `-32000 not implemented` cho tới phase 4.

## Key Insights
- Tool class **instance** + constructor injection `IRevitBridgeClient` (Q08 [25-27]) → test thay bằng fake (phase 5). Tool method trả `CallToolResult` để tự đặt `IsError` (Q04 [40-43]).
- SDK sinh JSON schema từ tham số C#: `string code`, `TransactionMode transaction = Auto` (enum → `"enum"` Q04 [28]), `bool dryRun = false`, `int timeoutSeconds = 30`, `string? label = null`; `CancellationToken` + `IProgress<ProgressNotificationValue>` bị loại khỏi schema (Q04 [31-33]). Enum C# `Auto/Manual/None` → SDK serialize tên enum — `[VERIFY]` casing (`auto` theo §4) → có thể cần `[JsonStringEnumConverter]`/`JsonPropertyName`.
- Server validate input trước khi gửi pipe (Q12 [14-18]): `code` non-empty, ≤ 32 KB, `timeoutSeconds` clamp 5–120, `label` ≤ 64 ký tự.
- Bridge nhận song song, xử lý Revit tuần tự (ADR-02): `revit.ping`/`revit.cancel`/`revit.inspect` trả lời trên pipe thread; `revit.context`/`revit.execute` đi qua `IRevitExecutor` (ExternalEvent, phase 4). Busy → `-32002` ngay.
- Ghi pipe phải serialize (`SemaphoreSlim(1,1)`): progress notification từ Revit thread chen với response từ pipe thread.
- `PipeSecurity` trên .NET 8 dùng `NamedPipeServerStreamAcl.Create(...)` (namespace `System.IO.Pipes`, inbox Windows) — không phải ctor `NamedPipeServerStream`.
- `System.Text.Json` trên netstandard2.0: Contracts chỉ khai báo record + `BridgeJson.Options` (camelCase, `DefaultIgnoreCondition = WhenWritingNull`), không source-gen.

## Requirements
**Functional**
- 4 tool đúng tên/annotations §3; 2 resource `revit://document/info`, `revit://selection`; 2 prompt `revit_query_template`, `revit_modify_template`.
- Server khi bridge chưa nối: `McpException("Revit bridge not connected. Open Revit 2026 and enable HPRebar MCP Bridge (pipe hprebar-mcp-r2026).")`.
- Bridge `-32001/-32002/-32003` và `result.isError` → `CallToolResult.IsError=true`; `-32700/-32600/-32601/-32000` → `McpException` ngắn không path.
- Reconnect backoff 0.5→8s ×5, ping 10s idle; timeout server = `timeoutSeconds + 5`.
**Non-functional**
- File < 300 dòng; server không reference Revit; Contracts không reference Roslyn/MCP SDK. Message ≤ 4 MB, dòng dài hơn → đóng kết nối + log.

## Architecture
```
Server (net10)                                   Bridge (Revit)
Tools/ExecuteRevitCodeTool ─┐                    Service/PipeListener  (NamedPipeServerStreamAcl, WaitForConnectionAsync loop, 1 client)
Tools/RevitContextTool ─────┤ IRevitBridgeClient        │ line → JsonRpcRequest
Tools/InspectTypeTool ──────┼► Services/RevitBridgeClient ══ NDJSON ══► Service/RequestDispatcher (switch method)
Tools/CancelExecutionTool ──┘   └ NdjsonPipeTransport            ├ revit.ping / revit.cancel / revit.inspect  → trả lời tại chỗ
Resources/RevitDocumentResources ┘  (id↔TCS, backoff, ping)      └ revit.context / revit.execute → IRevitExecutor (phase 4)
Prompts/RevitScriptPrompts  (không IPC)                      Service/McpBridgeHost : IMcpBridgeRunner (start/stop/status/settings)
Services/ResultFormatter  (ExecuteResult → CallToolResult)   Model/BridgeSettings (ExecutionEnabled=false, limits)
```
Data flow `execute`: tool validate → `SendRequestAsync<ExecuteResult>("revit.execute", ExecuteRequest, ct, progress)` → transport ghi 1 dòng → bridge dispatcher → executor → response dòng → TCS → `ResultFormatter` → `CallToolResult`. Notification `revit.progress {id}` → transport tra `id` → `IProgress.Report(new ProgressNotificationValue{Progress, Total, Message})`.

**Error mapping (server `ResultFormatter`/`RevitBridgeClient`):**
| Nguồn | Điều kiện | Kết quả MCP |
|---|---|---|
| Không nối được pipe / mất kết nối giữa chừng | `TimeoutException`, `IOException` | `McpException` "Revit bridge not connected…" |
| `response.error.code ∈ {-32001,-32002,-32003}` | disabled / busy / no doc | `CallToolResult{IsError=true, Text=error.message}` |
| `response.error.code` khác | bridge bug, method lạ | `McpException($"Bridge error {code}: {message}")` |
| `response.result.isError` | diagnostics / exception / timeout | `CallToolResult{IsError=true, Text=JSON ExecuteResult}` |
| Server timeout `timeoutSeconds+5` | không có response | gửi `revit.cancel`; `IsError` "timed out (cooperative — Revit may still be finishing)" |

## Related Code Files
**To create (proposed) — Contracts `HPRebar/HPRebar.Mcp.Contracts/`** (namespace `HPRebar.Mcp.Contracts.JsonRpc` / `.Messages`)
- `JsonRpc/JsonRpcRequest.cs`, `JsonRpcResponse.cs`, `JsonRpcNotification.cs`, `JsonRpcError.cs`, `JsonRpcMethods.cs` (const `revit.ping|context|inspect|execute|cancel|progress|log|status`), `BridgeJson.cs` (options)
- `Messages/ExecuteRequest.cs` (code, transaction `"auto"|"manual"|"none"`, dryRun, timeoutSeconds, label), `ExecuteResult.cs` (§4), `ScriptDiagnostic.cs`, `ChangedCounts.cs`, `ContextRequest.cs`, `ContextResult.cs` (§3), `InspectRequest.cs`, `InspectResult.cs` (`typeName, fullName, members[{kind,signature}]`, truncated), `CancelResult.cs`, `ProgressParams.cs`, `StatusParams.cs` (listening, executionEnabled, busy, docTitle), `BridgeErrorCode.cs` (consts §5)
**Server `HPRebar/HPRebar.Mcp.Server/`** (namespace `HPRebar.Mcp.Server.<Folder>`)
- `Services/IRevitBridgeClient.cs`, `Services/RevitBridgeClient.cs` (singleton, lazy connect, id counter, `ConcurrentDictionary<long, TaskCompletionSource<JsonRpcResponse>>`, `Notifications` event), `Services/NdjsonPipeTransport.cs` (connect/backoff, read loop, write lock, 4 MB guard), `Services/ResultFormatter.cs`
- `Tools/ExecuteRevitCodeTool.cs`, `Tools/RevitContextTool.cs`, `Tools/InspectTypeTool.cs`, `Tools/CancelExecutionTool.cs`
- `Resources/RevitDocumentResources.cs`, `Prompts/RevitScriptPrompts.cs`, `Models/TransactionMode.cs` (enum)
**Bridge `HPRebar/HPRebar.McpBridge/`**
- `McpBridgeRequest.cs` (namespace `HPRebar.McpBridge`; `ExecuteRequest` + `IProgress<ProgressParams>?` + `CancellationToken` + `TaskCompletionSource<ExecuteResult>(RunContinuationsAsynchronously)`)
- `Model/BridgeSettings.cs`, `Model/BridgeStatus.cs` (enum Stopped/Listening/Connected/Busy), `Model/LastRunInfo.cs`
- `Service/PipeListener.cs`, `Service/RequestDispatcher.cs`, `Service/IRevitExecutor.cs`, `Service/McpBridgeHost.cs`, `Service/PipeWriter.cs` (write lock + `\n`)
- `ViewModel/IMcpBridgeRunner.cs`
**To modify (proposed):** `Services`-less `Program.cs` — `AddSingleton<IRevitBridgeClient, RevitBridgeClient>()`, `AddSingleton<ResultFormatter>()`; `Models/BridgeOptions.cs` — thêm `ConnectTimeoutMs=2000`, `ExtraTimeoutSeconds=5`, `MaxSourceBytes=32768`, `PingIntervalSeconds=10`, `MaxReconnectAttempts=5`; `appsettings.json` khớp.
**To delete:** none

## Implementation Steps
1. **Contracts records** (bước đầu, cả server/bridge cần): `JsonRpcRequest { string JsonRpc = "2.0"; long? Id; string Method; JsonElement? Params }`, `JsonRpcResponse { long Id; JsonElement? Result; JsonRpcError? Error }`, `JsonRpcError { int Code; string Message; JsonElement? Data }`. Message DTO theo §3/§4 nguyên văn tên field camelCase. `BridgeErrorCode` consts `-32700, -32600, -32601, -32000, -32001, -32002, -32003`.
2. **`NdjsonPipeTransport`** (server): `NamedPipeClientStream(".", options.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous)`; `ConnectAsync(ConnectTimeoutMs, ct)`; `StreamReader(UTF8, detectBOM:false)` `ReadLineAsync(ct)` loop trên background Task; dòng > 4 MB → dispose + log warning; `StreamWriter(new UTF8Encoding(false)) { AutoFlush = true }` + `SemaphoreSlim`. Đứt kết nối → fail mọi TCS pending với `IOException`, backoff 0.5,1,2,4,8s tối đa 5 lần rồi giữ trạng thái Disconnected cho lần gọi sau thử lại.
3. **`RevitBridgeClient`** (singleton): `Task<T> SendAsync<T>(string method, object? @params, TimeSpan timeout, CancellationToken ct, IProgress<ProgressParams>? progress)`; `id = Interlocked.Increment`; timeout → gửi `revit.cancel {id}` best-effort + throw `BridgeTimeoutException` (server-internal) → tool map `IsError`. Notification `revit.progress` route theo `params.id`; `revit.status`/`revit.log` → event `StatusChanged` (log qua `ILogger<RevitBridgeClient>` → stderr). Idle ping mỗi 10s khi đã nối.
4. **Tools** (Q04, Q12): class `[McpServerToolType] public sealed class ExecuteRevitCodeTool(IRevitBridgeClient bridge, ResultFormatter formatter, IOptions<BridgeOptions> options)`. Method `[McpServerTool(Name = "execute_revit_code", Title = "Execute C# in Revit", ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false)] [Description(...)] Task<CallToolResult> ExecuteAsync([Description("C# script body…")] string code, [Description("auto|manual|none…")] TransactionMode transaction = TransactionMode.Auto, [Description] bool dryRun = false, [Description("5–120")] int timeoutSeconds = 30, [Description] string? label = null, IProgress<ProgressNotificationValue> progress = null!, CancellationToken ct = default)`. Description nói rõ (ADR-04 Consequences): chạy với full quyền user trong Revit session, mọi thay đổi vào 1 undoable group, ưu tiên `dryRun` trước. Ba tool còn lại: `get_revit_context` (`ReadOnly=true, Idempotent=true`, param `bool includeSelection = false`), `inspect_type` (`ReadOnly, Idempotent`; `string typeName, string? memberFilter = null, int maxMembers = 100`), `cancel_execution` (`Idempotent=true`, không param).
5. **Validation trong tool** (Q12 [14-18]): rỗng/whitespace → `IsError` "code is empty"; `Encoding.UTF8.GetByteCount(code) > MaxSourceBytes` → `IsError` nêu giới hạn; `timeoutSeconds = Math.Clamp(…, 5, 120)`; `label` cắt 64 ký tự, mặc định `"script"`.
6. **`ResultFormatter`**: `ExecuteResult` → `CallToolResult { IsError = r.IsError, Content = [ new TextContentBlock { Text = JsonSerializer.Serialize(r, BridgeJson.Options) } ] }`; `ContextResult`/`InspectResult` → text JSON. Strip đường dẫn tuyệt đối trong `message`/diagnostics bằng regex `[A-Za-z]:\\[^\s"]+` → `<path>` (ADR-04 lớp 9, phòng hờ bridge sót).
7. **Resources** (Q05): `[McpServerResourceType] public sealed class RevitDocumentResources(IRevitBridgeClient bridge)`; `[McpServerResource(UriTemplate = "revit://document/info", Name = "Revit document info", MimeType = "application/json")]` → `revit.context {includeSelection:false}`; `revit://selection` → `includeSelection:true`, trả mảng `selection`. Bridge chưa nối → `McpException` cùng thông điệp. `notifications/resources/updated` từ `revit.status`: `[VERIFY]` API SDK (`IMcpServer.SendNotificationAsync`); nếu không rõ → bỏ khỏi v1, ghi `plan.md` #13.
8. **Prompts** (Q06): `[McpServerPromptType] public static class RevitScriptPrompts`; `[McpServerPrompt(Name = "revit_query_template")] ChatMessage[] Query([Description] string question)` → System persona (Revit API 2026 C# expert, globals list, `return` giá trị, `transaction:"none"`), User few-shot `"đếm tường"` → Assistant ví dụ gọi tool với `return new FilteredElementCollector(doc).OfClass(typeof(Wall)).GetElementCount();`. `revit_modify_template(string task)` → System nhấn `dryRun:true` trước, rồi `transaction:"auto"` với `label`; nhắc `ct.ThrowIfCancellationRequested()` trong loop.
9. **Bridge `PipeListener`**: `PipeSecurity` = `WindowsIdentity.GetCurrent().User` FullControl, không Everyone; `NamedPipeServerStreamAcl.Create(PipeNaming.For(version), PipeDirection.InOut, maxNumberOfServerInstances: 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 65536, 65536, security)`; loop `WaitForConnectionAsync(ct)` → read lines → `RequestDispatcher.HandleAsync(line)` (fire-and-forget với try/catch, để `cancel` không xếp hàng) → client disconnect → `Disconnect()` + tạo instance mới. Bind fail (`IOException` "All pipe instances are busy") → status `Error("another Revit instance already serves MCP")` (ADR-02 Consequences).
10. **`RequestDispatcher`**: parse `JsonRpcRequest` (lỗi → `-32700`; thiếu `method` → `-32600`); switch: `revit.ping` → `{pong:true, revitVersion, listening, executionEnabled}`; `revit.cancel` → `executor.Cancel()`; `revit.inspect` → `executor.Inspect(req)` (reflection, pipe thread); `revit.context` → `await executor.GetContextAsync(...)`; `revit.execute` → kiểm `settings.ExecutionEnabled` (false → `-32001`), `executor.IsBusy` (→ `-32002`), rồi `await executor.ExecuteAsync(req, progressSink, ct)`; method lạ → `-32601`; exception → `-32000` message không path. Progress sink = `Progress<ProgressParams>` ghi notification `revit.progress {id}` qua `PipeWriter`.
11. **`IRevitExecutor`** (namespace `HPRebar.McpBridge.Service`): `bool IsBusy; int CompiledScriptCount; Task<ExecuteResult> ExecuteAsync(ExecuteRequest, IProgress<ProgressParams>?, CancellationToken); Task<ContextResult> GetContextAsync(bool includeSelection); InspectResult Inspect(InspectRequest); CancelResult Cancel();` — phase 4 hiện thực bằng `McpBridgeExternalEventHandler`.
12. **`McpBridgeHost : IMcpBridgeRunner, IDisposable`**: `static McpBridgeHost? Current`; ctor `(IRevitExecutor executor, BridgeSettings settings, int revitVersion)`; `StartAsync/StopAsync/RestartAsync`, `Status`, `event Action<BridgeStatus>? StatusChanged`, `LastRun`, `ExecutionEnabled` (get/set → `settings`), gửi `revit.status` khi đổi. Phase 4 gán `Current` trong `Application.OnStartup`.
13. **`IMcpBridgeRunner`** (namespace `HPRebar.McpBridge.ViewModel`, không Revit types — như `IColumnRebarRunner`): các member ở bước 12 + `string PipeName`, `string AuditDirectory`, `int CompiledScriptCount`.
14. `BridgeSettings` (Model): `ExecutionEnabled=false`, `AutoStartListener=false`, `RequireLocalApproval=false`, `DefaultTimeoutSeconds=30`, `MaxSourceBytes=32768`, `MaxOutputBytes=65536`, `MaxLogLines=200`, `ScriptCacheSize=50`, `AllowFamilyDocuments=false`. `AutoStartListener` load/save `%AppData%\HPRebar\McpBridge\settings.json` (System.Text.Json); `ExecutionEnabled` không bao giờ persist.
15. Alias rule: mọi file bridge chạm Revit `View` hoặc `Application` type: `using RevitView = Autodesk.Revit.DB.View;` `using RevitApplication = Autodesk.Revit.ApplicationServices.Application;` (bridge có `HPRebar.McpBridge.Application` riêng → CS0104 nếu không alias).
16. Build gate cả hai: `dotnet build HPRebar.slnx -c Debug.R26 -p:DeployAddin=false`; Inspector `tools/list` thấy 4 tool với badge `Destructive`/`ReadOnly`; gọi `get_revit_context` khi Revit đóng → protocol error verbatim "Revit bridge not connected…".

## Todo List
- [x] Contracts: `JsonRpcEnvelope` (1 shape cho request/notification/response) + `JsonRpcError` + `JsonRpcMethods` + `BridgeErrorCode` + `BridgeJson` + `SafeText`; messages theo §3/§4 (`BridgePingResult` — tránh trùng `ModelContextProtocol.Protocol.PingResult`)
- [x] Server: `NdjsonPipeTransport`, `RevitBridgeClient`, `IRevitBridgeClient`, `ResultFormatter`, `BridgeExceptions`, DI + `BridgeOptions` (PipeName override cho test)
- [x] Server: 4 tool + validation + descriptions (ADR-04 wording); `transaction` là string
- [x] Server: 2 resource, 2 prompt (`ChatMessage[]`)
- [x] Bridge: `McpBridgeRequest` (generic work delegate), `McpBridgeHost`, `IMcpBridgeRunner` trong add-in; `BridgeSettings`/`BridgeStatus`/`LastRunInfo`/`BridgeSettingsStore`/`NdjsonPipeWriter`/`PipeListener`/`RequestDispatcher`/`IRevitExecutor` trong `HPRebar.McpBridge.Core` (Revit-free)
- [x] Inspector CLI: 4 tool đúng annotations + schema (`code` required, 5 props); 2 resource; 2 prompt; `get_revit_context` khi Revit đóng → `isError` "Revit bridge not connected … hprebar-mcp-r2026"
- [x] Sớm hơn kế hoạch: `HPRebar.Mcp.Server.Tests` (xUnit v3, net10) — 7 test pipe round-trip thật + 3 test formatter/unavailable, 10/10 pass

## Success Criteria
- `dotnet build HPRebar.slnx -c Debug.R26 -p:DeployAddin=false` → exit 0.
- Inspector Tools tab: đúng 4 tool, `execute_revit_code` schema có `code` required + `transaction` enum 3 giá trị chữ thường + `timeoutSeconds` (không có `progress`/`ct`); Resources tab 2 URI; Prompts tab 2 prompt render `ChatMessage[]`.
- Inspector gọi `get_revit_context` khi Revit đóng → error message chứa `hprebar-mcp-r2026`, server process không chết, gọi lại lần 2 vẫn trả lời (reconnect không treo).
- Bridge: `dotnet build HPRebar/HPRebar.McpBridge/HPRebar.McpBridge.csproj -c Debug.R26` xanh; `IRevitExecutor` chưa có implementation → chưa test runtime (phase 4).
- Mọi file mới < 300 dòng (`wc -l`).

## Risk Assessment
| Risk | L×I | Mitigation |
|---|---|---|
| Tên API SDK khác sách (`CallToolResult.Content` kiểu, `TextContentBlock`, `ProgressNotificationValue`) | H×M | Đọc XML doc package đã verify phase 0; sửa tên, giữ shape §4 |
| Enum `TransactionMode` serialize PascalCase → AI gửi `"auto"` bị reject | M×M | `[VERIFY]` bằng Inspector; fallback nhận `string transaction` + parse case-insensitive |
| `NamedPipeServerStreamAcl` thiếu trên TFM `net8.0-windows7.0` | L×M | Là inbox `System.IO.Pipes` Windows; fallback `PipeOptions.CurrentUserOnly` (net5+) — đơn giản hơn, cân nhắc dùng luôn |
| Notification chen response → JSON hỏng | M×H | `PipeWriter` một lock, một dòng một `WriteAsync` |
| Reconnect loop chạy mãi khi Revit đóng lâu | M×L | Tối đa 5 lần rồi dừng; lần tool gọi kế tiếp mới thử lại |

## Security Considerations
- Pipe ACL current user only (ADR-04 lớp 8); `maxNumberOfServerInstances=1`.
- Tool description không lộ đường dẫn máy; `ResultFormatter` strip path; server không log nội dung `code` ở mức Information (chỉ hash SHA-256 + độ dài) — audit đầy đủ nằm ở bridge (phase 4).
- `execute_revit_code` annotations `Destructive=true` → host hỏi user mỗi lần (ADR-04 lớp 2).

## Next Steps
- Phase 3 (UI) và Phase 4 (executor) chạy song song, đều chỉ phụ thuộc `IMcpBridgeRunner`/`IRevitExecutor` + `McpBridgeHost.Current`.
- Phase 5 test in-memory `Pipe` thay `RevitBridgeClient` bằng fake `IRevitBridgeClient`.
