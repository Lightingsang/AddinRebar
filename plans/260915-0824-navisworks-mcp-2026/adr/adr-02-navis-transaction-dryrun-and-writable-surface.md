# ADR-02 — Transaction / dryRun cho Navisworks và danh sách thao tác GHI được

**Ngày:** 2026-09-15 · **Revised 2026-09-15 (red-team):** `Rollback()` chỉ gọi khi **chính** transaction của bridge để lại undo entry; `manual` ≡ `auto` (guard cấm `BeginTransaction`/`new Transaction`); `none` enforce bằng fingerprint + wrap; `CurrentSelection/CurrentViewpoint` rời W1 cho tới khi spike chứng minh · **Status:** Proposed · **Owner:** HPNavis
**Kế thừa:** [Revit ADR-03 Roslyn in-process execution (transaction policy)](../../260912-1521-dynamic-revit-mcp-server-2026/adr/adr-03-roslyn-in-process-execution.md) · [AutoCAD ADR-03 transaction/undo/dryRun](../../260913-0000-autocad-mcp-bridge-2026/adr/adr-03-autocad-transaction-undo-dryrun-policy.md)
**Bằng chứng:** [evidence §E6, §E8, §E8-bis, §E9](../research/evidence-on-machine-2026-09-15.md) · [researcher-01 §4–§6](../research/researcher-01-navisworks-api-plugin-facts.md)

## Context

- Navisworks là công cụ **tổng hợp & rà soát**: hình học đến từ file nguồn và **chỉ đọc**; API không tạo/sửa geometry. Phần ghi được là *metadata & trạng thái review* (E8). Tool surface nghiêng về **truy vấn + phân tích + báo cáo**; seed AutoCAD không có đối tác.
- `Document.BeginTransaction(displayName)` → `Transaction` với **`Commit()` bắt buộc**; "In the future rollback may be supported" (E6) → **không có rollback in-flight**.
- `Document.Rollback()`: "Rolls back (undoes) the **last completed** transaction. It will not be available for Redo." (E6). **Đây là undo toàn document, không scope vào transaction của bridge** — nếu transaction của bridge rỗng (script throw trước khi sửa, script chỉ đọc dưới `auto`, hoặc loại edit không vào undo stack) thì `Rollback()` sẽ **xoá thao tác cuối của người dùng**, không Redo được. Red-team 2026-09-15 (4/4 reviewer, Critical).
- Bridge **không cầm được** transaction do script mở: `Document` chỉ có `IsActiveTransaction : bool`, không có `ActiveTransaction`; `Transaction.Dispose()` không có tài liệu; `Rollback()` ném khi "active transaction in progress" (E6). `Transaction(Document, string)` là ctor public → `new Transaction(doc, "x")` cũng mở transaction.
- Không có `DocumentChanged` (Revit) hay object events (AutoCAD) để enforce `none`.

## Decision

### 1. Ánh xạ `transaction` × `dryRun` — mọi `Rollback()` đều có **điều kiện**

Ký hiệu: `label = "MCP: <request.Label|script>"`; `before = doc.NextUndo` đọc **trước** `BeginTransaction`; `after = doc.NextUndo` đọc **sau** `Commit()`.
**Điều kiện rollback:** `RollbackOwn() := (after == label && after != before) ? (doc.Rollback(); true) : false`. Nếu `after == label` mà `Rollback()` ném → log + `RolledBack=false`.

| `transaction` | `dryRun=false` | `dryRun=true` |
|---|---|---|
| `auto` (mặc định) | `tx = doc.BeginTransaction(label)` → script → `tx.Commit()`. Undo hiển thị `label` (1 entry) nếu có edit | như trái, rồi `RolledBack = RollbackOwn()`. Transaction rỗng → **không** `Rollback()`, `RolledBack=false`, `Message="dry run: script produced no undoable change; nothing to roll back"` |
| `manual` | **≡ `auto` + 1 dòng log** ("transaction=\"manual\" behaves like \"auto\" in Navisworks: the bridge owns the only transaction") — giống AutoCAD (`AutocadScriptRunner.cs:63`). Guard cấm `BeginTransaction`, identifier `Transaction` (ADR-04 §3) nên script không thể mở transaction riêng | như `auto` |
| `none` | **Vẫn wrap** trong `BeginTransaction(label)`/`Commit()` (để mọi edit lỡ có đều vào undo entry của bridge) + **fingerprint** trước/sau (§2). Sau script: nếu `RollbackOwn()` hoàn lại được **hoặc** fingerprint đổi → `IsError=true` "script declared transaction=none but modified the document (rolled back: yes/no)". Không đổi → `RolledBack=false`, ok | giống `none` |

- **Exception / timeout / cancel** (mọi mode): bridge `Commit()` (bắt buộc — không có abort) rồi `RolledBack = RollbackOwn()`; `IsError=true`. Bất biến Revit/AutoCAD giữ: *timeout luôn fail*; **khác:** "rollback" chỉ đúng khi undo entry là của bridge — `ExecuteResult.RolledBack=false` + `Message` nói rõ khi không hoàn lại được (đúng nghĩa gốc của field: "know nothing persisted" — `ExecuteResult.cs:31` — false = có thể đã persist).
- **W2 (file/clash run) trong run:** không undo được → bridge **không bao giờ** báo `RolledBack=true` cho run chứa W2 (pre-pass ADR-04 §3 đánh dấu request `HasHeavyCalls`); `dryRun=true` + W2 → từ chối **trước** khi chạy: "dryRun cannot undo AppendFile/SaveFile/Export/TestsRunTest; run with dryRun=false or remove the call".
- **Cancel đến khi call đồng bộ đang chạy** (clash run): token chỉ được kiểm sau khi call trả về → kết quả `IsError=true, Message="cancelled after the synchronous call completed; …persisted"`, `RolledBack = RollbackOwn()` (thường false với W2).
- **Spike gate (phase 1):** S-05 (edit thật → `after == label`, Ctrl+Z hoàn lại), **S-05b** (transaction rỗng: `before == after`, không `Rollback()`, undo của user còn nguyên), **S-05c** (`CurrentSelection.Add`/`CurrentViewpoint.CopyFrom` có tạo undo entry không), **S-05d** (`new Transaction(doc,"x")` không `Commit` → `IsActiveTransaction` sau khi return? `Dispose()` có đóng không? — chỉ để biết cách phục hồi nếu guard lọt), **S-06** (throw giữa script). Kết quả sửa bảng §3 trước phase 2.

### 2. Fingerprint & `Changed`

`NavisChangeCounter.Snapshot(doc)` (rẻ, O(số tập hợp)): `SelectionSets.Value.Count`, `SavedViewpoints.Value.Count`, `Models.Count`, `GetClash(doc)?.TestsData.Tests.Count`, `GetTimeliner(doc)?.TaskTotalTasks`, `CurrentSelection.SelectedItems.Count` + hash 32 `InstanceGuid` đầu, `NextUndo`, `IsModified`, số item hidden ước lượng = `Search{IsHidden}` **cắt 1 kết quả** (chỉ có/không). `Changed.Added/Deleted` = tổng tăng/giảm của các count; `Modified` = 1 nếu fingerprint đổi mà `Added+Deleted==0`. Mô tả tool nói rõ "counts are coarse". `none` fail khi **bất kỳ** trường nào đổi (§1).

### 3. Danh sách thao tác GHI — kiểm chứng (E8), phân lớp

| Lớp | Thao tác | Undo? | Guard/pre-pass mặc định | Seed MVP |
|---|---|---|---|---|
| **W1 — review metadata** (rẻ; undo *[expected — S-05]*) | `SelectionSets.AddCopy/InsertCopy/ReplaceWithCopy/Remove/EditDisplayName/AddComment`; `SavedViewpoints.AddCopy/ReplaceFromCurrentView/Remove/AddComment`; `Models.OverridePermanentColor/Transparency/ResetPermanentMaterials/SetHidden/SetRequired/SetFrozen/ResetAllHidden`; `CreateCommentWithUniqueId`; `TestsAddCopy/TestsRemove/TestsEditResultStatus/AssignedTo/Comments`; `Timeliner.TaskAddCopy/TaskEdit/TaskRemoveAt` | expected ✅ | cho phép | `create_selection_set_from_search`, `create_viewpoint`, `override_color_by_search` |
| **W1? — chờ S-05c** | `CurrentSelection.Add/AddRange/Remove/Clear/SelectAll/CopyFrom`; `CurrentViewpoint.CopyFrom`; `OverrideTemporaryColor/Transparency` | **U** — có thể không vào undo stack | cho phép nhưng **không** seed nào dùng dưới `none`; nếu S-05c = không undo → mô tả tool ghi "not undoable, excluded from dryRun guarantees" | — |
| **W2 — nặng, không undo** | `Document.AppendFile(s)/MergeFile(s)/RemoveFile/OpenFile/Clear/UpdateFiles` + `Try*`; `SaveFile/ExportToNwd/PublishFile/ExportAsDwf/GenerateImage` + `Try*`; `TestsRunTest/TestsRunAllTests/TestsCompactAllTests` | ❌ (file) / U (clash — S-08) | **pre-pass Navis chặn khi heavy OFF** với thông điệp riêng (ADR-04 §3); heavy ON → cho phép, kèm path policy | `create_and_run_clash_test` (`tags:["heavy"]`) — seed heavy **duy nhất**; append/save ad-hoc khi heavy ON |
| **W3 — cấm luôn** | `Document.Undo/Redo/Rollback/Try*`, `StartDisableUndo/EndDisableUndo`, `BeginTransaction`, `new Transaction(...)`, `SetModelUnitsAndTransform`, `Database`/`ToNavisworksConnection`/`NavisworksCommand`/`NavisworksConnection` (SQLite nhúng — `Document.Database` E8), ComApi (`ComApiBridge`, `SetUserDefined`), Automation (`NavisworksApplication`), redline (không có API) | — | `GuardProfile.Navis` (Core, data thuần) | — |

Mô tả `execute_navis_code` liệt kê đúng bốn lớp này.

### 4. Đơn vị (E9) — không đổi

`units = new ScriptUnits(doc.Units.ToString(), UnitConversion.ScaleFactor(doc.Units, Units.Millimeters))`; mm ở biên; `SetModelUnitsAndTransform` cấm (W3); `units.Note` khi model ghép khai đơn vị khác; đọc lại `doc.Units` mỗi run.

### 5. Điều gì KHÔNG hứa

Không rollback in-flight; `Changed` coarse; không undo W2; `none` là best-effort (fingerprint) — ghi trong mô tả tool; không ComApi; Simulate → `HasClashModule=false`; Freedom không hỗ trợ.

## Alternatives rejected

- **`Rollback()` vô điều kiện** (bản 2026-09-15 sáng): xoá edit cuối của user khi transaction bridge rỗng — Critical, loại.
- **Bỏ dryRun:** mất `test_tool`; commit-then-`RollbackOwn()` giữ được dryRun cho W1.
- **Snapshot/restore thủ công; `StartDisableUndo`:** như bản trước — loại.
- **`manual` thật (script mở transaction):** không có API để bridge đóng transaction script bỏ quên; hậu quả là undo stack chết tới khi restart. Loại — mirror AutoCAD.
- **Đếm `TransactionEnded` để rollback N transaction:** vô nghĩa khi script không mở được transaction; `EventArgs` trống không cho biết transaction nào. Loại.

## Consequences

- `AnalyzerProfile.Navis = transactionTypeNames: ["Transaction"], transactionMethodNames: ["BeginTransaction"]` (chỉ để `analyze` báo đúng lý do; guard vẫn cấm).
- `GuardProfile.Navis` (Core, data): W3 + `System.Windows.Forms`, `Microsoft.Win32`, `System.Data`, `System.Linq.Expressions`, `Autodesk.Navisworks.Api.Automation/Interop/ComApi/Data`; identifiers `MessageBox, Transaction, Expression, Delegate, NavisworksApplication, ComApiBridge, NavisworksCommand, NavisworksConnection, NavisworksDataAdapter`; members `BeginTransaction, Undo, Redo, Rollback, TryUndo, TryRedo, TryRollback, StartDisableUndo, EndDisableUndo, SetModelUnitsAndTransform, SetUserDefined, Database, ToNavisworksConnection, CreateDelegate, Compile`. W2 **không** nằm trong `GuardProfile` — do pre-pass host-side quyết theo cờ heavy (ADR-04 §3).
- `ExecuteResult.RolledBack` giữ đúng nghĩa contract: true = undo entry của bridge đã bị hoàn lại; false = có thể đã persist (kèm `Message`).
- Undo menu Navisworks hiện `MCP: <label>` cho run có edit.
