# Phase 2: AutoCAD Bridge Runtime — 11 Harness Runs to 21/21, Three Crashes Rewrote the Transaction Design

**Date**: 2026-09-14 15:35  
**Severity**: High (three acad.exe crashes, fully remediated)  
**Component**: HPAutoCad.McpBridge runtime / MainThreadExecutor / AutocadScriptRunner  
**Status**: Resolved (fixes committed 150a95a + 4322411)

## Bối cảnh

Phase 1 đưa loader + ALC; phase 2 = executor thực tế. Harness chạy unattended qua SECURELOAD, UIA opt-in checkbox, 21 kịch bản JSON-RPC pipe (execute/context/cancel/timeout/undo/no-drawing). Run 1–7 chết acad.exe 3 lần (`.NET Runtime 1026`, `AccessViolation`). Run 8–10 chạy sạch. Code review 7.5/10 → 16/17 findings fixed. Final run 11: 21/21 pass.

## Tổng Quan — Từng Bước, Từng Crash

### Run 1–2: database events (ước lượng thay đổi)

`ObjectAppended/Modified/Erased` **chỉ bắn khi transaction ngoài cùng commit**. Run 1–2 dryRun đếm 0 objects thêm dù script `AppendEntity` thành công → phải serialize trước commit, sau đó bù bằng `HANDSEED` delta + `ObjectOpenedForModify` + `ObjectId.IsErased`. Thiết kế đếm `added = handseed_after − handseed_before` (mỗi append tiêu handle); `modified`/`deleted` = số item ghi được/xóa được = bóc Observable callbacks. Xấp xỉ, ghi docs.

### Run 3–4: Undo sai kỳ vọng

`U` (Undo) sau 2 run commit gỡ **cả 2 run** (kiếp trước tưởng mỗi run = mỗi Undo). Nguyên nhân: `doc.TransactionManager` (document) gộp mọi run liên tiếp từ **lệnh cuối của user** làm 1 bước. AutoCAD lệnh cuối user + 2 run MCP = 1 entry Undo. Cách khác lưu mỗi run riêng = `ExecuteInCommandContextAsync` + `UNDO _BE/_E` (ngoài MVP). Chấp nhận + ghi docs.

### Run 5–7: Ba Lần Crash — Transaction Wrapper Finalizer

**Run 5:** script để `StartTransaction()` → bridge qua guard → fail. Lại run cùng, script không thoát transaction → wrapper `Transaction` không `Dispose` → finalizer chạy khi GC, native transaction đã chết → `AccessViolation` trong `DisposableWrapper.Finalize`. Tương tự **run 6** (nested `TopTransaction`), **run 7** (GetAllObjects) trả về `Transaction` wrapper không ai dispose.

**Thiết kế mới:** guard deny `StartTransaction`/`StartOpenCloseTransaction`/`TopTransaction`/`LockDocument` trong script (ADR-03 revised). Bridge sở hữu duy nhất `tr`; script dùng `tr` (alias) hoặc không. Nếu script `var t = tr; t.Commit();` thì `tr` đã kết thúc → runner detect `NumberOfActiveTransactions < 2` → rollback + error. Finalizer không còn hold wrapper nguy hiểm.

### Run 8–11: Harness sạch, Code Review Tích hợp

Run 8–10 pass 21/21. Review 7.5/10 → 2 major (harness không guard running acad.exe; queue không track deadline/cancel), 14 minor/nit. Fix run 11: grace 8 s (dưới server wait 5+5 s), pid-guarded COM, cancelled token không start, monotonic clock, serializer thêm `ObjectIdCollection`/`SelectionSet`/`PromptSelectionResult`. Run 11 retry: 21/21 (busy refusal `-32002` sau 8,0 s) + audit dòng "Busy…".

## Số liệu

| Kiểm tra | Kết quả |
|---|---|
| `dotnet build HPAutoCad.slnx -c Debug/Release` | ✅ 0 warnings, 0 errors |
| `dotnet build HPRebar.slnx -c Debug.R26` | ✅ 0 errors (Core additive) |
| McpShared tests | ✅ 88/88 (70 + MainThreadQueue 10 + Insunits 2 + dispatcher 1 + guard 1 + existing 4) |
| HPRebar MCP tests | ✅ 106/106 pass |
| Harness run 10 | ✅ 21/21 pass |
| Harness run 11 (after review fix) | ✅ 21/21 pass + audit |
| `.NET Runtime 1026` events | ⚠️ Runs 5–7 (crashed); runs 10–11 (0 events) |
| Code review | 7.5/10 → 16/17 resolved (1 deferred phase 3) |

## Quyết định & Bài Học

**1. Wrapper lifetime là correctness, không hygiene.** AutoCAD .NET finalizer violations là crash, không leak. Mỗi `Transaction`/`DatabaseLock` wrapper script cấp phải dispose ngay; nếu không → guard deny. Revit cũng có wrapper nhưng Revit transaction scoped (inside ExternalEvent), AutoCAD application-context (ngoài kiểm soát bridge). Cảnh báo không đủ; phải chặn.

**2. Verify live trước tin docs.** Events chỉ fire outer commit ≠ kỳ vọng; undo gộp liên tiếp ≠ per-run. Đọc AutoCAD samples code + run chứng minh = phát hiện sớm. Harness 11 run ≠ quá đắt.

**3. Queue deadline phải trên cạnh dump lịch sử Idle.** Nếu AutoCAD dừng raise `Idle` (lồng message loop, LISP block) → request chờ vô hạn, `-32002` không bao giờ gửi. Cái này chưa bắt in live (xử qua server's `cancel-after-timeout`), phá lưới ở phase 5 nếu cần.

## Tiếp theo

**Phase 3:** server + AutocadHostProfile + tool descriptions (cảnh báo "use `tr` không `StartTransaction`"). Carry forward run 21/21 baseline. Modal dialog + ESC-retry = phase 5 manual.

**Commit:** 150a95a (runtime) + 4322411 (review fixes). Bundle 24 files, 88 + 106 tests pass, harness 21/21 ×2, 0 runtime warnings.

**Status**: DONE  
**File**: F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\docs\journals\2026-09-14-phase-2-autocad-bridge-runtime.md
