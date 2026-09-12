# ADR-03 — Thực thi code AI: Roslyn scripting in-process trong bridge, marshal qua ExternalEvent, TransactionGroup bao ngoài

**Ngày:** 2026-09-12 · **Status:** Accepted (user-confirmed default) · **Owner:** HPRebar

## Context

- Mục tiêu hệ thống: `AI → MCP → Revit Runtime`, không phải catalog tool cố định (`create_wall`, …). Tool cốt lõi `execute_revit_code` nhận C# snippet, trả giá trị `return`.
- Sách: tool là "hands" — action có side effect; giữ số tool ít để không đốt context window (NotebookLM Q06 [3], Q12 [232-233]). Một tool động thay hàng chục tool tĩnh khớp lời khuyên này.
- Revit API chỉ gọi được trên thread Revit; add-in modeless dùng `ExternalEvent` + `IExternalEventHandler`; pattern sẵn có: `ConcurrentQueue<Request>` + `Raise()` + `TaskCompletionSource(RunContinuationsAsynchronously)` + drain queue trong `Execute` — `HPRebar/HPRebar/ColumnRebar/ColumnRebarExternalEventHandler.cs:16-75`.
- Roslyn scripting: `CSharpScript.Create(code, ScriptOptions)` → `Script<T>.RunAsync(globals)`; compile đầu 0.5–2s, cache `Script<T>` → <50 ms; mỗi script = assembly động không unload — `research/roslyn-scripting-in-revit-report.md` §2, §6.
- Không abort được script đang chạy trên thread Revit; chỉ cooperative cancellation — cùng report §5 (Roslyn #23145).
- Repo tham khảo cũng compile Roslyn → MemoryStream → reflection invoke, wrap Transaction trong `Execute` — `research/revit-bridge-reference-report.md` Summary.

## Decision

### Vị trí compile/run
- Roslyn (`Microsoft.CodeAnalysis.CSharp.Scripting`) **trong tiến trình Revit** (project `HPRebar.McpBridge`). Server không reference RevitAPI.dll.
- **Compile trên pipe thread, run trên Revit thread**: `ScriptCompiler` (Service/) biên dịch + cache theo SHA-256(source + options); chỉ `RunAsync(globals)` chạy trong `Execute(UIApplication)`. Giảm thời gian Revit bị block.
- Globals: `doc` (`Document`), `uidoc` (`UIDocument`), `app` (`Application`), `uiapp` (`UIApplication`), `ct` (`CancellationToken`), `log` (`Action<string>` → gom vào output), `progress` (`Action<int,int,string>` → notification `revit.progress`, NotebookLM Q11 [3-8]).
- Imports mặc định: `System`, `System.Linq`, `System.Collections.Generic`, `Autodesk.Revit.DB`, `Autodesk.Revit.UI`, `Autodesk.Revit.DB.Structure`. References: RevitAPI, RevitAPIUI, `System.Runtime`, `System.Linq`, `System.Collections`, `netstandard`.
- Kết quả: `ScriptState.ReturnValue` → serialize JSON (`System.Text.Json`, `ElementId` → `long`, `XYZ` → `{x,y,z}` feet, `Element` → `{id, name, category}`); `null` → `"null"`. Trần 64 KB, cắt + cờ `truncated`.

### Transaction policy (tham số `transaction` của tool)
| Giá trị | Hành vi trong `Execute` |
|---|---|
| `auto` (mặc định) | `TransactionGroup g("MCP: <label>")` → `Transaction t("MCP script")` → run → `t.Commit()` → `g.Assimilate()`. Exception → `t.RollBack()` + `g.RollBack()`. |
| `manual` | Chỉ `TransactionGroup`; script tự mở `Transaction`. Thành công → `Assimilate()`; exception → `RollBack()` toàn nhóm. |
| `none` | Không transaction; script chỉ đọc. Revit ném lỗi "modify outside transaction" → trả `IsError` kèm gợi ý dùng `auto`. |
| `dryRun=true` | Luôn `g.RollBack()` sau khi run, trả kết quả + `rolledBack: true`. |
- Mọi `Transaction` do bridge mở gắn `IFailuresPreprocessor`: warning → `DeleteWarning`, error → `ProceedWithRollBack`; `SetClearAfterRollback(true)` — không bao giờ hiện dialog modal khi AI điều khiển (report §4).
- Guard trước run: không có `ActiveUIDocument` → `IsError`; `doc.IsReadOnly` / `IsFamilyDocument` khi `transaction != none` → `IsError` (trừ khi config cho phép family).

### Timeout / cancel
- `timeoutSeconds` mặc định 30 (user-confirmed), config 5–120. Watchdog phía bridge: hết hạn → `cts.Cancel()` (globals `ct`), ghi `timedOut: true`. **Không kill thread.** Nếu script không hợp tác, bridge báo `revit.status busy` cho tới khi xong; server trả `IsError` "timed out (cooperative)". **Bổ sung 2026-09-12 (verified trong Revit):** timeout đã bắn thì run luôn là thất bại + rollback, kể cả khi script thấy `ct` và `return` bình thường — kết quả cắt ngang không được commit; AI tăng `timeoutSeconds` hoặc chia nhỏ.
- `revit.cancel` từ server (khi MCP client gửi `notifications/cancelled`, NotebookLM Q11 [12-18]) → cùng `cts.Cancel()`.

### Cache & bộ nhớ
- Cache `Script<object>` LRU 50 entries; assembly động không unload (report §6) → ghi metric số script đã compile; nút "Restart bridge" trong UI để khuyến nghị restart Revit khi >500 compile.

## Alternatives rejected

1. **Compile trong server, gửi IL sang bridge** (giữ Roslyn ngoài Revit, tránh xung đột ALC với Dynamo). Loại v1: server phải có RevitAPI.dll đúng version để reference; hai bên phải đồng bộ reference set; tăng surface IPC. **Giữ làm phương án B** nếu Phase 5 phát hiện xung đột `Microsoft.CodeAnalysis` với Dynamo/pyRevit trong ALC.
2. **IronPython / CPython (kiểu pyRevit)** — AI sinh C# khớp Revit API docs/samples hơn; user chỉ định C#.
3. **Sinh tool tĩnh từ Revit API** — chính là mô hình user loại bỏ.
4. **Compile cả trên Revit thread** — đơn giản hơn nhưng block UI Revit thêm 0.5–2s mỗi script mới.

## Consequences

- Bridge phụ thuộc `Microsoft.CodeAnalysis.CSharp.Scripting` (+ `.Common`, `.CSharp`) — ship **không ILRepack**, copy-local trong thư mục add-in; cần test coexistence với Dynamo (report Risk, Phase 5).
- Compile diagnostics trả về AI nguyên văn (line/col/message) trong `CallToolResult.IsError=true` để tự sửa (NotebookLM Q02 [17-19], Q12 [226-227]); không kèm đường dẫn máy.
- Script hang = Revit treo cho tới khi xong; chấp nhận, ghi rõ trong tool description + docs.
- Mở đường cho ADR-04: mọi guard bảo mật là defense-in-depth, không sandbox.
