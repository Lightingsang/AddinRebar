# ADR-02 — Marshal script lên main thread AutoCAD: `Application.Idle` one-shot + `IsQuiescent` gate + `LockDocument`

**Ngày:** 2026-09-13 · **Revised 2026-09-14** (ADR-06: executor sống trong `HPAutoCad/HPAutoCad.McpBridge`; phần Revit-free `IdleQueue` nếu tách thì vào `McpShared/HPRebar.McpBridge.Core`) · **Status:** Proposed · **Owner:** HPRebar
**Kế thừa:** [Revit ADR-03 (ExternalEvent, compile trên pipe thread / run trên Revit thread)](../../260912-1521-dynamic-revit-mcp-server-2026/adr/adr-03-roslyn-in-process-execution.md) · Research: [autocad-dotnet-api-2026-report.md §0.2, §0.4, §4](../research/autocad-dotnet-api-2026-report.md) · [autocad-bridge-reference-report.md Addendum](../research/autocad-bridge-reference-report.md)

## Context

- Mọi API AutoCAD .NET phải gọi trên main thread (report §4; ADN "Use Thread for background processing"). Pipe listener của bridge (`PipeListener.ServeClientAsync`) chạy trên thread-pool thread → cần một điểm marshal tương đương `ExternalEvent.Raise()` của Revit.
- Revit có `IExternalEventHandler.Execute(UIApplication)` — Revit **tự** gọi lại khi idle, trong ngữ cảnh API hợp lệ. AutoCAD không có handler kiểu đó; ba ứng viên (đều verified tồn tại trong `AcMgd.xml`/`AcCoreMgd.xml` 25.1.0):
  1. `Autodesk.AutoCAD.ApplicationServices.Core.Application.Idle` (event, "fired when the application goes idle") + `Core.Application.IsQuiescent` ("Assesses if there is a command, LISP script, or ARX command active").
  2. `DocumentCollection.ExecuteInApplicationContext(ExecuteInApplicationContextCallback, object)` — chữ ký có, **remarks không có** trong XML docs; có gọi được từ thread ngoài hay không: `[unverified]`.
  3. `DocumentCollection.ExecuteInCommandContextAsync(Func<object,Task>, object)` — `[undocumented]` trong 25.1.0 nhưng có theo Kean 2015; cho **command context** (cần khi gọi `Editor.Command`), không cần cho script thuần API.
- Precedent trên máy dev: `AutoCadMcpPlugin.dll` (06/2026, chính user) dùng `Idle` → `RunOnMainThread` → `LockDocument` → `StartTransaction` với AutoCAD 2026 .NET 8 (strings trong DLL; research §0.2). Blog drive-cad-with-code 2015 dùng đúng mẫu này và nhấn mạnh **`IsQuiescent` trước khi chạm document** vì "AutoCAD being idle does not necessarily mean there is no command in progress".
- Khác Revit: main thread AutoCAD bơm message cả khi modal dialog đang mở → `Idle` có thể bắn khi user đang ở dialog; `IsQuiescent == false` khi có command/LISP/ARX đang chạy. Revit thì `ExternalEvent.Raise()` trả `Denied`/`Pending` — bridge Revit đã map thành lỗi "Revit is in a modal dialog…".

## Decision

1. **Đường chính — `MainThreadExecutor` (Revit-free trong `HPAutoCad.McpBridge`, không phải Core vì đụng AutoCAD API):**
   - Pipe thread: guard + compile như Revit (`ScriptGuard`, `ScriptCompiler` — pipe thread, không đụng AutoCAD), rồi `Enqueue(work)` vào `ConcurrentQueue<AutocadBridgeRequest>` (cùng shape `McpBridgeRequest`: `Name`, `Work`, `CancellationToken`, `TaskCompletionSource<object>(RunContinuationsAsynchronously)`), rồi **subscribe `Application.Idle` nếu chưa subscribe** (`Interlocked` flag). Subscribe/unsubscribe event từ thread ngoài: **spike phase 1** — nếu AutoCAD từ chối, subscribe `Idle` **một lần vĩnh viễn ở `Initialize()`** và handler chỉ làm việc khi queue không rỗng (chi phí: một check `IsEmpty` mỗi idle tick, chấp nhận).
   - Main thread (`OnIdle`): `if (queue.IsEmpty) { unsubscribe; return; }` → `if (!Application.IsQuiescent) return;` (giữ subscribe, thử lại tick sau) → `Application.Idle -= OnIdle` → drain queue: mỗi request `Completion.TrySetResult(Work(state, ct))`, exception → `TrySetException`. Giống `McpBridgeExternalEventHandler.Execute` từng dòng.
   - `Work` của execute: `AutocadScriptRunner.Run(...)` (ADR-03) mở `doc.LockDocument()` **trước** transaction; lock là bắt buộc vì đang ở application context (report §4, help OARX 2025 "Lock and Unlock a Document").
2. **Không chờ mãi khi AutoCAD bận:** request có `EnqueuedAt`; `OnIdle` thấy `!IsQuiescent` quá **`BusyGraceSeconds` (mặc định 10 s)** → `TrySetException(BridgeBusyException("AutoCAD is running a command or showing a dialog. Press ESC / close the dialog and retry."))` → dispatcher trả `-32002 Busy` (đã có `BridgeErrorCode.Busy`) → server map `IsError` (đã có). Server vẫn giữ timeout tổng `timeoutSeconds + ExtraTimeoutSeconds` như Revit (`RevitBridgeClient.WaitForResponseAsync`).
3. **Một script tại một thời điểm** (`_busy` CAS như Revit); `autocad.cancel` chỉ set `CancellationTokenSource` — script chạy đồng bộ trên main thread nên **không thể abort**; ghi rõ trong description tool như Revit.
4. **Đường phụ (spike phase 1, không phải MVP):** `ExecuteInApplicationContext` gọi từ pipe thread. Nếu spike chứng minh (a) gọi được từ thread ngoài, (b) callback chạy trên main thread khi idle, (c) không deadlock khi có modal — thì có thể thay `Idle` bằng nó (ít code hơn). Kết quả spike ghi vào `reports/phase-01-spike.md`; ADR này cập nhật `Status`.
5. **Không dùng** `SendStringToExecute`/`Editor.Command` cho script (command context, async, không có return value, phá mô hình "một run = một kết quả đồng bộ"). Guard AutoCAD **deny** cả hai + mọi `Editor.Get*` prompt (`GetSelection/GetPoint/GetEntity/GetString/GetKeywords/GetInteger/GetDouble/GetAngle/GetDistance/GetCorner/GetFileNameForOpen/GetFileNameForSave`) và `Application.ShowModalDialog/ShowModalWindow/ShowAlertDialog`, `MessageBox` — vì prompt chờ user trên main thread trong khi server chờ pipe → treo tới timeout, và không rollback được sạch.
6. **Context/selection reads** (`autocad.context`) cũng đi qua executor (cần main thread cho `SelectImplied`, `MdiActiveDocument`); `autocad.inspect`/`autocad.analyze`/`ping`/`cancel` trả lời trên pipe thread như Revit.

## Sequence (rút gọn)

```
pipe thread            main thread (AutoCAD)
──────────             ─────────────────────
guard → compile
enqueue(work)
Idle += OnIdle  ─────▶ OnIdle: IsQuiescent? no → (grace) → retry / Busy
                       yes → Idle -= OnIdle → LockDocument → StartTransaction
                             → script.RunAsync(globals).GetAwaiter().GetResult()
                             → Commit | Abort (dryRun/none/error/timeout)
                       TCS.SetResult(ExecuteResult)
◀──────────────────────
reply over pipe
```

## Alternatives rejected

- **`System.Windows.Forms.Control.Invoke` / WPF `Dispatcher` của main window** (ADN 2012): chạy code trên main thread nhưng **không** đảm bảo AutoCAD ở trạng thái quiescent/application context; có thể chạy giữa một command → `eLockViolation`/crash. `Idle` + `IsQuiescent` là mẫu Autodesk khuyến nghị cho modeless/background.
- **`ExecuteInCommandContextAsync` cho mọi script**: async, command context, cần `await` trong bridge → phức tạp; chỉ cần khi script gọi command AutoCAD (bị guard deny).
- **Thread riêng "STA executor" kiểu COM (U-C4N)**: đó là COM automation ngoài tiến trình; in-process .NET không có apartment riêng để gọi API — không áp dụng.

## Consequences

- Latency: script chờ tới `Idle` tick kế tiếp (thường < 50 ms khi AutoCAD rảnh); cộng guard/compile trên pipe thread như Revit.
- Khi user đang gõ lệnh dở, request chờ tối đa `BusyGraceSeconds` rồi trả `Busy` — AI thấy thông điệp hành động được (giống U-C4N "press ESC").
- Test không cần AutoCAD: `MainThreadExecutor` nhận `Func<bool> isQuiescent` + `Action<EventHandler> subscribeIdle` qua constructor → xUnit giả lập idle tick (đặt phần Revit-free này trong `HPAutoCad.McpBridge` nhưng test qua interface; hoặc tách `IdleQueue` nhỏ vào Core nếu hoàn toàn không đụng AutoCAD API — quyết định khi implement, ưu tiên Core).
- Cần spike phase 1 cho: subscribe `Idle` từ thread ngoài; `ExecuteInApplicationContext` từ thread ngoài; hành vi khi modal dialog mở.
