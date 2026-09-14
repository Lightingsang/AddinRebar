# Phase 2 — bridge runtime, verified in AutoCAD 2026 (2026-09-14, 10 harness runs)

**Cách chạy:** `pwsh HPAutoCad/tools/harness/run-bridge-unattended.ps1` (AutoCAD đóng). Harness: `acad.exe /b bridge.scr` (`HPMCPBRIDGE` + `HPMCPSTART`) → SECURELOAD auto-click → chờ pipe → UIA tick "Allow AI code execution" → `pipe-scenarios.py` NDJSON thẳng vào `\\.\pipe\hpautocad-mcp-2026` → COM (`Close(false)`, `Documents.Add()`, `SendCommand('_LINE ')`, `SendCommand('_U ')`) → kill acad. Không có MCP server (phase 3).

## Kết quả run 10 (cuối): 21/21

| # | Kịch bản | Kết quả |
|---|---|---|
| 0 | opt-in OFF → `autocad.execute` | ✅ `-32001` "Code execution is disabled…" |
| 1 | `autocad.ping` | ✅ pong, `revitVersion=2026`, executionEnabled, busy=false |
| 2 | `autocad.context` (selection) | ✅ `host=autocad`, `docTitle=Drawing1.dwg`, units Inches (INSUNITS 1, acad.dwt), `activeView` Model, `autocad{insunits,measurement=English,currentLayout=Model,currentLayer=0,isModelSpace,isQuiescent,isNamedDrawing=false}`, openDocs |
| 3 | read `return db.Filename;` (`none`) | ✅ 7 ms, changed 0 |
| 4 | dryRun tạo Line | ✅ `changed.added=1`, `rolledBack=true`, value `{handle,layer,length}` (serialize trước abort), model space không đổi |
| 5 | commit tạo Line | ✅ added=1, rolledBack=false, count +1, 4 ms |
| 6 | `throw` sau AppendEntity | ✅ isError "InvalidOperationException: boom…", rolledBack=true, changed 0, count không đổi |
| 7 | `none` + tạo Line | ✅ isError "modified the drawing with transaction=\"none\"…", rolledBack=true |
| 8 | script `StartTransaction()` | ✅ guard GUARD ".StartTransaction is not allowed in AutoCAD scripts" (xem Quyết định 2) |
| 9 | `transaction=manual` | ✅ chạy như `auto`, log "manual behaves like auto…", added=1 |
| 10 | modify (ColorIndex) + Erase | ✅ `changed {0,1,1}`, count −1 |
| 11 | guard `ed.GetPoint` | ✅ diagnostics GUARD |
| 12 | guard `tr.Commit()` | ✅ "tr.Commit is not allowed: the bridge owns `tr`…" |
| 13 | compile error | ✅ CS0103 line/col |
| 14 | cancel (`autocad.cancel` sau 1,5 s, script `ct.ThrowIfCancellationRequested()`) | ✅ `cancelled=true,wasRunning=true`; "Script was cancelled"; rolledBack; 1 031 ms |
| 15 | timeout 5 s, vòng lặp 7 s không hợp tác | ✅ `timedOut=true`, rolledBack, 7 012 ms (main thread block 7 s — AutoCAD đứng, đúng thiết kế cooperative) |
| 16 | `progress()` ×3 + `log()` ×3 | ✅ 3 notification `autocad.progress`, logs đúng thứ tự |
| 17 | serializer + `args` | ✅ `ObjectId→{handle,class}`, `Point3d→{x,y,z}`, `args.Int/Str` |
| 18 | Undo: `REGEN` → commit → `U` | ✅ count 1→2→1 (xem Quyết định 4) |
| 19 | đóng drawing (COM) → context / execute | ✅ `openDocs=[]`, execute `-32003` "No drawing is open in AutoCAD" |
| 20 | `LINE` đang chờ điểm → execute | ✅ `-32002` sau đúng 10,0 s grace; ESC + retry = kiểm tay phase 5 |
| — | Status window XAML trong ALC riêng | ✅ log "status window opened (visible=true, dispatcher thread 1)"; UIA tick checkbox được |
| — | Audit | ✅ `%AppData%\HPAutoCad\McpBridge\audit\audit-20260914.log` 176 dòng, `docTitle` đổi theo `DocumentActivated` (Drawing2.dwg) |
| — | Crash | ✅ run 10: 0 event .NET Runtime 1026 (run 5–7 có, xem dưới) |

## Quyết định rút ra từ live run (cập nhật ADR-02/03)

1. **Hai transaction lồng nhau** (`doc.TransactionManager`): `outer` = TransactionGroup của Revit (commit/abort quyết định), `inner` = `tr` của script, bridge commit ngay khi script return. Lý do: `Database.ObjectAppended/ObjectModified/ObjectErased` **chỉ bắn khi transaction ngoài cùng commit** (run 3–4: dryRun đếm 0, commit đếm 1) → đếm bằng event không dùng được cho dryRun/none.
2. **Guard deny `StartTransaction`/`StartOpenCloseTransaction`/`TopTransaction` trong script AutoCAD; `manual` = `auto` + log.** Bằng chứng: run 5–7 acad.exe chết (`.NET Runtime 1026`, `AccessViolation` trong `Transaction.CheckTopTransaction` từ `DisposableWrapper.Finalize`) sau kịch bản "script để nested transaction mở" — wrapper `Transaction` script không dispose bị finalize sau khi native transaction đã kết thúc → không thể làm an toàn từ bridge. `tr` là transaction duy nhất script được cầm.
3. **Đếm thay đổi:** `added` = `HANDSEED` sau − trước (mọi object append đều tiêu một handle); `modified`/`deleted` = `ObjectOpenedForModify` (bắn ngay) → `ObjectId.IsErased` đọc trước khi `inner` commit (`ObjectErased` cũng chỉ bắn khi outer commit). Không giữ wrapper `DBObject` nào — run 7 crash `DBObject.DeleteUnmanagedObject` từ finalizer khi thử `Transaction.GetAllObjects()`. Bỏ qua container (`SymbolTable`, `BlockTableRecord`). Xấp xỉ, ghi docs.
4. **Undo:** run ở application context → AutoCAD gộp mọi run MCP liên tiếp **kể từ lệnh cuối của user** thành một bước Undo (`U` sau 2 run commit gỡ cả 2). `LockDocument(ProtectedAutoWrite, "HPMCP", …)` để tên hiện trong UNDO; `db.TransactionManager` (không phải `doc.`) thì `U` không gỡ gì cả. Chấp nhận + ghi docs; per-run undo cần `ExecuteInCommandContextAsync` + `UNDO BE/E` — ngoài MVP.
5. **Idle:** subscribe vĩnh viễn ở `MainThreadExecutor` ctor (main thread), `MainThreadQueue.OnTick` no-op khi queue rỗng; `PostMessage(WM_NULL)` đánh thức loop sau enqueue. Không còn subscribe/unsubscribe chéo thread (ADR-02 §1 cập nhật). Latency thực tế: read 7 ms end-to-end.
6. **`-32002`/`-32003`:** `BridgeRequestException(code)` (Core) → dispatcher trả đúng mã; Revit không ném nên không đổi hành vi.
7. **XAML trong ALC riêng chạy được** với `AssemblyLoadContext.EnterContextualReflection()` quanh `InitializeComponent()` (WPF resolve `;component` bằng `Assembly.Load`).
8. **SECURELOAD:** sau *Always Load* một lần, các bản build sau **không hỏi lại** (run 1–10 không prompt dù loader DLL đổi hash) → *Always Load* tin cả folder, không chỉ hash. Cập nhật ADR-05.

## Sau code review (run 11, 2026-09-14)
- Grace busy 10 s → **8 s** (dưới thời gian chờ ngắn nhất của server 5 + 5 s) và **áp dụng cả khi AutoCAD đã rảnh lại**: request già hơn grace bị từ chối `-32002` thay vì chạy muộn; token cancel trong lúc chờ → không chạy. Refusal (`-32002`/`-32003`) nay có dòng audit + `RunCompleted` như Revit.
- Guard deny thêm `LockDocument`; script commit/abort `tr` qua alias → lỗi "committed or aborted `tr` itself" + rollback.
- Serializer: `ObjectIdCollection`, `SelectionSet`, `PromptSelectionResult`. Lệnh `HPMCP*` có `CommandFlags.Session` (gõ được khi không có bản vẽ).
- Harness từ chối chạy khi có acad.exe khác; COM chỉ đụng acad.exe do harness khởi động (pid). Run 11: 21/21 (busy `-32002` sau 8,0 s).

## Sai lệch so với phase-02 plan
- `AutocadUnitsFactory` → bảng `AutocadInsunits` trong Core (test được, AcDbMgd không load ngoài acad).
- `MainThreadExecutorTests` → `MainThreadQueueTests` (Core, 7 test) + `AutocadInsunits` (2 theory) + dispatcher code test.
- Harness = Python + PowerShell trong `HPAutoCad/tools/harness/` (không phải 40 dòng: 320 + 110 dòng vì SECURELOAD/UIA/COM).
- Kịch bản "manual đúng/sai" thay bằng "guard deny StartTransaction" + "manual = auto".
- Modal dialog khi Idle, ESC rồi retry: kiểm tay phase 5.

## Số liệu
Build `HPAutoCad.slnx` Debug/Release 0 warning; McpShared 85/85 (70 + 15 mới); HPRebar MCP 106/106; `HPRebar.slnx Debug.R26` build OK (Core đổi không ảnh hưởng). AutoCAD warm start → pipe up 14–16 s.
