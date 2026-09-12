# ADR-02 — IPC Server ↔ Bridge: Named Pipe + JSON-RPC 2.0 newline-delimited

**Ngày:** 2026-09-12 · **Status:** Accepted (user-confirmed default) · **Owner:** HPRebar

## Context

- Hai tiến trình cùng máy (ADR-01). Cần kênh local, không mở port, có ACL theo user.
- Repo tham khảo dùng TCP `localhost:8080` + JSON-RPC 2.0 nhưng **không framing** (đọc buffer 8192 byte) → vỡ với payload lớn; socket thread block `ManualResetEvent.WaitOne(timeout)` — `research/revit-bridge-reference-report.md` Summary + "What to Avoid".
- MCP đã chuẩn hoá JSON-RPC 2.0: request (`id`, `method`, `params`), response (`result` XOR `error`), notification (không `id`) — NotebookLM Q02 [3-10]. Nhiều request in-flight phân biệt bằng `id` — Q02 [5, 12].
- MCP tách **protocol error** (`error` field) khỏi **tool execution error** (`result.isError=true`) — Q02 [13-17]. Bridge cần cùng phân biệt để server map 1:1.

## Decision

- **Transport:** `NamedPipeServerStream` trong bridge (Revit), `NamedPipeClientStream` trong server. Tên pipe `hprebar-mcp-r{RevitVersion}` (vd `hprebar-mcp-r2026`). `PipeSecurity` chỉ current user. `PipeTransmissionMode.Byte`, async duplex.
- **Framing:** UTF-8, **một JSON object mỗi dòng (`\n`)** — cùng mô hình newline-delimited stdio MCP dùng; đọc `ReadLineAsync`, không giới hạn 8 KB. Trần cứng 4 MB/message để chống OOM.
- **Envelope:** JSON-RPC 2.0 nguyên bản (đúng shape Q02) — không phát minh envelope riêng:
  - Request server→bridge: `{"jsonrpc":"2.0","id":n,"method":"revit.execute","params":{…}}`
  - Response: `{"jsonrpc":"2.0","id":n,"result":{…}}` hoặc `{"jsonrpc":"2.0","id":n,"error":{"code":…,"message":…,"data":…}}`
  - Notification bridge→server: `{"jsonrpc":"2.0","method":"revit.progress","params":{"id":n,"progress":3,"total":10,"message":"…"}}` — server forward thành `IProgress<ProgressNotificationValue>` (NotebookLM Q11 [3-8]).
- **Methods:** `revit.ping`, `revit.context`, `revit.inspect`, `revit.execute`, `revit.cancel`. **Notifications:** `revit.progress`, `revit.log`, `revit.status` (bridge on/off, busy).
- **Error mapping (bridge → MCP):**

| Nguồn lỗi | Bridge trả | Server map thành |
|---|---|---|
| Pipe chưa kết nối / Revit không chạy | (không có bridge) | `throw new McpException("Revit bridge not connected. Open Revit 2026 and enable HPRebar MCP Bridge.")` — protocol error, verbatim (Q02 [15]) |
| Compile diagnostics / exception trong script / validation (allowlist, timeout, doc read-only) | `result: { isError: true, diagnostics \| message }` | `CallToolResult { IsError = true, Content = [TextContentBlock] }` (Q02 [13, 17-19]) để AI tự sửa |
| Bridge bug / envelope sai / method lạ | `error: { code: -32601 / -32600 / -32000 }` | `McpException` message ngắn, không path |

- **Correlation & concurrency:** `id` tăng dần phía server; `ConcurrentDictionary<long, TaskCompletionSource<JsonRpcResponse>>`. Bridge xử lý **tuần tự** (1 ExternalEvent queue, Revit single-thread) nhưng nhận song song để `revit.cancel` / `revit.ping` không xếp sau script đang chạy.
- **Timeout:** server chờ `params.timeoutSeconds + 5s`; hết hạn → gửi `revit.cancel` + trả `IsError` "timed out (cooperative — Revit may still be finishing)". Reconnect: backoff 0.5s→8s, tối đa 5 lần rồi báo lỗi; ping mỗi 10s khi idle.

## Ghi chú implement (2026-09-12)
- SDK 2.2.0 tự chuyển `McpException` ném từ tool thành `CallToolResult.IsError=true` (prefix "An error occurred invoking …" + stack trace ra stderr). Vì vậy "bridge not connected" và timeout trả `IsError` trực tiếp (không ném) — AI thấy cùng thông điệp, log sạch; `McpException` chỉ còn cho lỗi nội bộ bridge (`-32700/-32600/-32601/-32000`) để giữ stack trace.
- Tham số `transaction` của tool là `string` ("auto|manual|none", normalize case-insensitive) thay vì enum — tránh phụ thuộc cách SDK sinh casing enum trong schema.
- ACL pipe dùng `PipeOptions.CurrentUserOnly` cả hai đầu thay cho `NamedPipeServerStreamAcl` — cùng hiệu quả, ít code.
- `SafeText.StripPaths` nằm ở Contracts, cả bridge (dispatcher) lẫn server (formatter) đều gọi — không bên nào được quên.

## Alternatives rejected

1. **TCP localhost như repo tham khảo** — port cố định, xung đột nhiều instance, firewall prompt, không ACL. Giữ làm fallback nếu Named Pipe gặp vấn đề với sandbox của host AI.
2. **HTTP localhost (Kestrel trong Revit)** — nặng, cùng nhược điểm ADR-01 alt 1.
3. **Envelope tự chế `{id, kind, payload}`** — JSON-RPC đã có error codes + notification semantics, server map 1:1 sang MCP; Contracts lib chỉ cần vài record.
4. **Length-prefix framing** — hoạt động nhưng khó debug tay; NDJSON đọc được bằng log.

## Consequences

- Multi-instance Revit (2 cửa sổ Revit 2026) → cùng tên pipe, instance thứ 2 fail bind. v1 chấp nhận: bridge báo "another instance already serves MCP". Ghi Unresolved Questions.
- `HPRebar.Mcp.Contracts` `netstandard2.0` + `System.Text.Json` (có sẵn net8/net10) — không Newtonsoft như repo tham khảo.
- Test thuần (xUnit) cho framing/reconnect/timeout bằng `System.IO.Pipelines.Pipe` in-memory — không cần Revit.
