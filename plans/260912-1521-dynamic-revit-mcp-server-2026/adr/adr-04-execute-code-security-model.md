# ADR-04 — Security model cho `execute_revit_code`

**Ngày:** 2026-09-12 · **Status:** Accepted (user-confirmed default) · **Owner:** HPRebar

## Context

- `execute_revit_code` = thực thi C# tuỳ ý trong tiến trình Revit, quyền của user đang đăng nhập. Không có sandbox thật: `ScriptOptions` không phải sandbox, reflection/`Assembly.Load` bypass mọi giới hạn import — `research/roslyn-scripting-in-revit-report.md` §7 (Roslyn #10830).
- Sách: mọi input từ LLM/client là **untrusted**; host phải giữ human-in-the-loop cho tool có side effect; least privilege; không lộ secret/PII/stack trace trong error; hạn chế tool theo thư mục/lệnh/endpoint cụ thể — NotebookLM Q12 [3, 8, 18, 25-28, 103-104, 629].
- Metadata `ReadOnly` / `Destructive` / `Idempotent` / `OpenWorld` để host quyết định có hỏi user hay không; host yêu cầu approval cho tool destructive — Q12 [5-8, 183-185]; `[McpServerTool(ReadOnly = true)]` — Q04 [8-9].
- SDK mask exception thường thành "An error occurred invoking 'tool'"; `McpException` đi verbatim; `CallToolResult.IsError` cho lỗi domain — Q02 [15-19], Q12 [221-225].
- Threat model thực: **local dev tool, 1 user, 1 máy**, không multi-tenant, không network. Kẻ tấn công thực tế = prompt-injection vào AI (nội dung file/model chứa chỉ dẫn độc) khiến AI sinh script xoá model / đọc file / gọi mạng.

## Decision — 9 lớp, defense-in-depth (không lớp nào tự nhận là sandbox)

| # | Lớp | Ở đâu | Chi tiết |
|---|---|---|---|
| 1 | **Opt-in runtime switch** | Bridge UI | Checkbox "Allow AI code execution" mặc định **OFF**, reset mỗi lần mở Revit. OFF → `revit.execute` trả `isError` "code execution disabled by user". `revit.context`/`revit.inspect` vẫn chạy. |
| 2 | **Tool annotations** | Server | `execute_revit_code`: `ReadOnly=false`, `Destructive=true`, `Idempotent=false`, `OpenWorld=false` → host hỏi user mỗi lần (Q12 [8]). `get_revit_context`, `inspect_type`: `ReadOnly=true`. `cancel_execution`: `Idempotent=true`. |
| 3 | **Static deny-list** | Bridge `ScriptGuard` (Service/) | `CSharpSyntaxWalker` trên `using`, qualified names, `typeof`, string `"System.Reflection"`. Deny: `System.IO` (trừ `System.IO.Path`), `System.Net`, `System.Diagnostics.Process`, `System.Reflection` (trừ `GetType().Name`/`.FullName`), `System.Reflection.Emit`, `System.Runtime.InteropServices`, `System.Threading.Thread`, `Environment.Exit`, `Assembly.Load*`, `AppDomain`, `dynamic`, `unsafe`, **`await` / `Task.Run` / `async` lambda** (script chạy đồng bộ trên Revit thread — `await` đẩy continuation sang thread khác rồi gọi Revit API sai thread, hoặc deadlock khi bridge block chờ `RunAsync`). Vi phạm → `isError` liệt kê dòng + lý do. Tài liệu ghi rõ: **có thể bypass**, chỉ chặn lỗi vô ý. |
| 4 | **Reference set tối thiểu** | Bridge `ScriptOptions` | Chỉ RevitAPI, RevitAPIUI, `System.Runtime`, `System.Linq`, `System.Collections`, `netstandard`. Không `System.Net.Http`, không `Microsoft.CodeAnalysis`. |
| 5 | **Giới hạn tài nguyên** | Bridge | Source ≤ 32 KB; timeout 30s mặc định (5–120) cooperative (ADR-03); output ≤ 64 KB; `log()` ≤ 200 dòng; cache script ≤ 50. |
| 6 | **Document guards** | Bridge | Không active doc / `IsReadOnly` / `IsFamilyDocument` / đang trong modal → `isError` rõ ràng. `dryRun` luôn rollback. Failure preprocessor không hiện dialog. |
| 7 | **Audit log** | Bridge, Serilog file | `%AppData%\HPRebar\McpBridge\audit-YYYYMMDD.log`: timestamp, user, doc title + path hash, script SHA-256, source đầy đủ, transaction mode, dryRun, kết quả (ok/isError/timeout), duration ms, số element thay đổi (`DocumentChanged` event đếm added/modified/deleted). Không xoá tự động < 30 ngày. |
| 8 | **Pipe ACL** | Bridge | `PipeSecurity` chỉ SID user hiện tại; không network; tên pipe cố định theo Revit version (ADR-02). |
| 9 | **Error hygiene** | Server + bridge | Compile diagnostics / exception message trả verbatim **sau khi** strip đường dẫn tuyệt đối + stack trace; server không giữ secret nào; log server → stderr/file, không stdout (Q08 [14-18]). |

Bổ sung UX: bridge status window hiển thị **script cuối cùng đã chạy + nút "Undo last MCP run"** (gọi `TransactionGroup` name để user Undo bằng Ctrl+Z bình thường — mọi run có tên `MCP: <label>` trong Undo history).

## Alternatives rejected

1. **Sandbox thật (AppDomain / process riêng / WASM)** — AppDomain không có trên .NET 8; process riêng không gọi được Revit API; WASM không có Revit API. Không khả thi cho in-process runtime.
2. **Allowlist API-level (chỉ cho gọi danh sách method Revit đã duyệt)** — chính là quay về tool cố định; loại theo yêu cầu kiến trúc.
3. **Bắt user duyệt từng script trong bridge UI (modal)** — trùng với confirmation của host AI (Q12 [8]); 2 lần hỏi gây mệt. Để làm **option** `RequireLocalApproval` mặc định OFF cho môi trường cần chặt hơn.
4. **Chạy AI script với user Windows riêng quyền thấp** — Revit process là của user; không đổi được token cho 1 thread.

## Consequences

- Residual risk chấp nhận và ghi trong docs + tool description: script có thể bypass lớp 3 bằng reflection; biện pháp cuối là lớp 1 (user tắt) + lớp 7 (truy vết) + Undo của Revit.
- Description của `execute_revit_code` phải nói rõ với AI: "Runs in the user's Revit session with full user privileges. Changes go into one undoable transaction group. Prefer `dryRun` first for destructive operations." (Q12 [12-14, 189]).
- Phase 5 cần test: deny-list walker (xUnit, pure), guard doc read-only (TUnit), audit log format (xUnit).
- Không tương thích ý "Sandboxing… containerized non-root" của sách (Q12 [27-29]) — chỉ áp dụng cho server HTTP; ghi lý do trong docs.
