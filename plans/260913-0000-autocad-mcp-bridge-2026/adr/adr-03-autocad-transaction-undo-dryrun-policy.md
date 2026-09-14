# ADR-03 — Transaction / Undo / dryRun trong AutoCAD: một transaction ngoài cùng do bridge mở, `tr` global, abort = rollback

**Ngày:** 2026-09-13 · **Revised 2026-09-14** (ADR-06: project `HPAutoCad.McpBridge` trong `HPAutoCad/`; `units` là kiểu Core `ScriptUnits`) · **Status:** **Accepted (2026-09-14, revised by live run)** — verified by `reports/phase-02-bridge-runtime.md` 21/21; xem §"Revised after phase 2" cuối file · **Owner:** HPRebar
**Kế thừa:** [Revit ADR-03 §Transaction policy](../../260912-1521-dynamic-revit-mcp-server-2026/adr/adr-03-roslyn-in-process-execution.md) (`auto|manual|none` + `dryRun`, timeout luôn fail + rollback) · Research: [autocad-dotnet-api-2026-report.md §0.1 #2, §5, §6, §9](../research/autocad-dotnet-api-2026-report.md)

## Context

- Contract đã chốt cho Revit và **không đổi** ở Contracts: `ExecuteRequest.Transaction ∈ {auto, manual, none}`, `DryRun`, `ExecuteResult.RolledBack/TimedOut/Changed{Added,Modified,Deleted}`. Registry (`ToolRecord.Transaction`, `ToolValidator` "code opens its own Transaction → manual") và `DynamicToolRegistrar` (thêm `dryRun` khi `transaction != none`) đều đọc các giá trị này. AutoCAD phải **giữ nguyên ngữ nghĩa nhìn từ AI**, chỉ đổi cách hiện thực.
- AutoCAD khác Revit ở 3 điểm (verified §0.1/§0.4):
  1. **Không có `TransactionGroup`.** Nhưng transaction lồng nhau: *abort transaction ngoài cùng huỷ mọi thay đổi kể cả nested đã Commit* (ObjectARX Dev Guide qua Kean 2009). ⇒ transaction ngoài cùng của bridge = TransactionGroup của Revit.
  2. **Đọc cũng cần transaction**: `tr.GetObject(id, OpenMode.ForRead)` — không có "đọc ngoài transaction" như `FilteredElementCollector`. ⇒ mode `none` không thể là "không transaction".
  3. **Script phải cầm object `Transaction`** để `GetObject`/`AddNewlyCreatedDBObject`. Revit không cần vì `doc.Create.*` tự dùng transaction đang mở.
- `Database.ObjectAppended/ObjectModified/ObjectErased` bắn **trong** transaction, trước Commit (§9) → đếm được `Changed` cả khi sau đó Abort (đúng như Revit dryRun đếm rồi rollback).
- Undo: mỗi Commit của transaction ngoài cùng trong application context tạo một mục Undo `[unverified: tên hiển thị]`; `Editor.Command("_.UNDO","_BE")` chỉ chạy trong command context (§6) → không dùng.

## Decision

### Globals (thêm so với danh sách user đưa: `tr`, `units`)
| Global | Kiểu | Ghi chú |
|---|---|---|
| `doc` | `Autodesk.AutoCAD.ApplicationServices.Document` | `MdiActiveDocument` |
| `db` | `Database` | `doc.Database` |
| `ed` | `Editor` | `doc.Editor` — chỉ `WriteMessage`, `SelectImplied`, `SelectAll`; prompt bị guard deny |
| `app` | `DocumentCollection` | `Application.DocumentManager` (`Application` là static class, không có instance để gán) |
| **`tr`** | `Transaction` | transaction ngoài cùng do bridge mở — **luôn non-null** trong cả 3 mode (xem bảng dưới). Script dùng `tr.GetObject(...)`, `tr.AddNewlyCreatedDBObject(...)`; **không** gọi `tr.Commit()/Abort()` (guard deny member `Commit`/`Abort` trên identifier `tr`; `ScriptAnalyzer` báo `UsesTransaction=true` khi thấy `StartTransaction()`/`StartOpenCloseTransaction()` → validator yêu cầu `manual`). |
| **`units`** | `HPRebar.McpBridge.Core.Scripting.ScriptUnits` (Core, host-neutral; bridge tạo từ `db.Insunits`) | `units.Label`, `units.ToDrawing(double mm)`, `units.ToMm(double du)`, `units.MmPerUnit`; `Unitless` → hệ số 1 + `log` cảnh báo. Lý do: 12 seed đều cần mm ↔ drawing unit, ADR-05 Revit cấm chia file trong tool. |
| `ct`, `log`, `progress`, `args` | như Revit | `ScriptArgs` tái dùng nguyên |

### Policy (nhìn từ AI giống hệt Revit)
| `transaction` | Bridge làm gì trên main thread | Thành công | Exception / timeout / cancel | `dryRun=true` |
|---|---|---|---|---|
| `auto` (mặc định) | `LockDocument()` → `tr = db.TransactionManager.StartTransaction()` → run | `tr.Commit()` → 1 mục Undo | `tr.Abort()` → `rolledBack=true` | `tr.Abort()` sau khi run → `rolledBack=true`, `changed` vẫn đếm |
| `manual` | như `auto`; script mở **nested** `StartTransaction()` của riêng nó | kiểm `NumberOfActiveTransactions == 1` (script đã đóng nested của nó; nếu > 1 → `Abort` ngoài cùng + lỗi "script left a transaction open") → `tr.Commit()` | `tr.Abort()` ngoài cùng huỷ cả nested đã Commit | `tr.Abort()` |
| `none` | `LockDocument()` (lock đọc vẫn cần ở application context) → `tr = StartTransaction()` → run | **luôn `tr.Abort()`**; nếu `Changed` ≠ 0 → `IsError` "script modified the drawing in transaction=none; use auto" (tương đương Revit `ModificationOutsideTransactionException`) | `tr.Abort()` | (đã abort) |

- **Timeout luôn thất bại + Abort** kể cả khi script `return` kịp sau khi thấy `ct` — giữ đúng quy tắc Revit đã verified (ScriptRunner: "A script that noticed the timeout and returned early still timed out").
- **Change counting:** `DatabaseChangeCounter` subscribe `db.ObjectAppended/ObjectModified/ObjectErased` trước `StartTransaction`, unsubscribe trong `finally`; đếm `ObjectId` distinct (Modified có thể bắn nhiều lần/object `[unverified: tần suất]` → dùng `HashSet<ObjectId>`; `Appended` rồi `Erased` cùng run → trừ). Đặt trong `HPAutoCad.McpBridge/Service/DatabaseChangeCounter.cs` (mirror `DocumentChangeCounter.cs`).
- **Undo label:** AutoCAD không đặt tên mục Undo qua API `[verified: không có API trong XML docs]`. Bridge ghi `ed.WriteMessage("\n[MCP] <label>: committed, N objects. Undo with U.")` sau Commit để user thấy; `LastRunInfo`/status window hiển thị như Revit. Không cố `UNDO BE/END` (command context).
- **Guards trước run** (mirror `ScriptRunner.Run` Revit): không `MdiActiveDocument` → "No drawing is open in AutoCAD"; `doc.IsReadOnly` và mode ≠ `none` → lỗi; `db.TransactionManager.NumberOfActiveTransactions > 0` trước khi bridge mở → lỗi "AutoCAD already has a transaction open" (tương đương `doc.IsModifiable`).
- **Lỗi AutoCAD:** `Autodesk.AutoCAD.Runtime.Exception` → message `"{ErrorStatus}: {Message}"` (ErrorStatus enum là thứ AI hiểu: `eLockViolation`, `eNotOpenForWrite`, `eWasErased`…), `SafeText.StripPaths` như Revit.

### `ResultSerializer` cho AutoCAD (`AutocadResultSerializer`, mirror `ResultSerializer.cs` Revit)
`ObjectId` → `{ "handle": "2A3F", "class": "AcDbLine" }`; `Handle` → hex string; `Point3d/Point2d/Vector3d` → `{x,y,z}` (drawing units); `Entity` → `{ handle, type: GetType().Name, layer, dxfName }`; `DBObject` khác → `{ handle, type }`; `Extents3d` → `{ min, max }`; enum → string; trần 64 KB + `truncated` như Revit. Không walk object graph (`Database` cyclic, nhiều getter throw ngoài transaction).

## Alternatives rejected

- **`none` = không mở transaction, script tự mở `StartOpenCloseTransaction`** — script có thể Commit thay đổi thật trong mode "read-only"; không enforce được. Loại.
- **dryRun bằng `Database.DisableUndoRecording` hoặc clone `Database`** — không rollback được / quá nặng. `Abort` ngoài cùng là cơ chế chính thức.
- **`tr` ẩn (script dùng `db.TransactionManager.TopTransaction`)** — AI sinh code kiểu sample AutoCAD luôn có biến `tr`; `TopTransaction` ít gặp trong training data → nhiều compile/runtime error hơn. Giữ `tr` tường minh.
- **Bỏ `units`, để mỗi seed tự convert** — lặp 12 lần cùng 8 dòng; ADR-05 Revit cấm chia file tool. Giữ `units`.

## Consequences

- `ScriptAnalyzer.UsesTransaction` (Core) cần hook theo host: Revit = `new Transaction/TransactionGroup/SubTransaction`; AutoCAD = invocation `StartTransaction`/`StartOpenCloseTransaction`. Thêm `AnalyzerProfile` (Core, host-neutral: danh sách tên type/method) — phase 0.
- `ToolValidator` không đổi logic: "UsesTransaction && mode != manual → error" vẫn đúng.
- Description `execute_autocad_code` phải nói rõ `tr` và cấm `tr.Commit()`; prompt template AutoCAD few-shot dùng `tr.GetObject`.
- TUnit/in-process test cho `AutocadScriptRunner` không có (như Revit hiện tại) → live verify phase 5 là gate: 10 kịch bản execute (read, dryRun, commit, exception, `none`+modify, manual đúng/sai, guard, compile error, cancel, timeout, busy).

## Revised after phase 2 (2026-09-14, verified live — `reports/phase-02-bridge-runtime.md`)

Những điểm dưới **thay** bảng Policy ở trên khi mâu thuẫn:

1. **Hai transaction do bridge mở, cả hai qua `doc.TransactionManager`** (không phải `db.TransactionManager` — chỉ transaction của Document mới vào undo stack và flush graphics): `outer` = vai TransactionGroup (commit = giữ, abort = dryRun/none/lỗi/timeout/cancel), `inner` = `tr` của script, **bridge commit `inner` ngay khi script return** rồi mới quyết định `outer`. Lý do: `ObjectAppended/ObjectModified/ObjectErased` chỉ bắn khi transaction ngoài cùng commit → không đếm được dryRun bằng event.
2. **Script không được mở transaction riêng.** Guard AutoCAD deny `StartTransaction`, `StartOpenCloseTransaction`, `TopTransaction`. `transaction=manual` được chấp nhận trên wire (contract chung với Revit) nhưng chạy **y như `auto`** + một dòng `logs` giải thích. Bằng chứng: acad.exe chết (`.NET Runtime 1026`, AccessViolation trong `Transaction.CheckTopTransaction` từ `DisposableWrapper.Finalize`) khi wrapper `Transaction` của script không được Dispose bị GC finalize sau khi native transaction đã kết thúc; bridge không có cách chạm vào wrapper đó. Hệ quả: `ToolValidator` "UsesTransaction → manual" không còn kích hoạt cho AutoCAD (guard chặn trước); mô tả tool `execute_autocad_code` phải nói "dùng `tr`, không `StartTransaction`".
3. **Đếm `Changed`:** `added` = `Database.Handseed` sau − trước; `modified`/`deleted` = `ObjectOpenedForModify` (bắn ngay khi open ForWrite) rồi `ObjectId.IsErased` đọc **trước khi `inner` commit**; bỏ `SymbolTable`/`BlockTableRecord`. Không giữ wrapper `DBObject` nào ngoài transaction (`Transaction.GetAllObjects()` → finalizer crash). Thất bại/rollback → `Changed = 0` như Revit; dryRun → đếm thật + `rolledBack=true`.
4. **Undo:** `LockDocument(DocumentLockMode.ProtectedAutoWrite, "HPMCP", "HPMCP", false)` — tên hiện trong UNDO. Ở application context AutoCAD **gộp mọi run MCP liên tiếp kể từ lệnh cuối của user thành một bước Undo** (`U` sau 2 run commit gỡ cả 2; sau `REGEN` thì `U` gỡ đúng 1). Chấp nhận cho MVP, ghi rõ trong description tool + status window ("U reverts the AI's runs since your last command"). Per-run undo cần `ExecuteInCommandContextAsync` + `UNDO _BE/_E` → ngoài phạm vi.
5. Serialize `Value` **trước** khi `inner` commit (object mở qua `tr` bị dispose khi transaction kết thúc). `ed.WriteMessage` một dòng `[MCP] <label>: ok/…; N added, M modified, K erased; undo with U` sau mỗi run; `FlushGraphics()` + `UpdateScreen()` sau commit (ngoài command AutoCAD không tự vẽ lại).
