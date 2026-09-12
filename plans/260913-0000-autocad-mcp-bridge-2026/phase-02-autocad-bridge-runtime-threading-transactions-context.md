---
title: "Phase 2 — Bridge runtime AutoCAD: executor main thread, script runner (lock/transaction/dryRun), context, serializer, status window"
status: planned
priority: P1
effort: 14h
depends_on: [phase-00, phase-01]
created: 2026-09-13
---

# Phase 2 — AutoCAD bridge runtime

## Context
- [ADR-02](adr/adr-02-autocad-main-thread-marshalling.md) · [ADR-03](adr/adr-03-autocad-transaction-undo-dryrun-policy.md) · [architecture.md §3 script contract, §7 bảng so sánh](architecture.md) · `reports/phase-01-spike.md` (đường marshal đã chốt).
- Mẫu Revit đọc từng dòng: `HPRebar.McpBridge/McpBridgeExternalEventHandler.cs` (queue, `_busy` CAS, `ExecuteAsync` guard→compile→run, `Finish` audit + `RunCompleted`), `McpBridgeRequest.cs`, `Service/ScriptRunner.cs` (guards, timeout linked CTS, `logs` cap, commit/rollback, `RollBack` helper), `Service/DocumentChangeCounter.cs`, `Service/ResultSerializer.cs`, `Service/RevitContextReader.cs`, `Model/ScriptGlobals.cs`, `Application.cs` (`CreateBridge`: reference set + imports), `View/McpBridgeStatusView.xaml` + `ViewModel/McpBridgeStatusViewModel.cs` (đã sang Core ở phase 0).

## Overview
Thay phần "Revit" của bridge bằng phần "AutoCAD" — cùng hình dạng, cùng contract pipe — để `RequestDispatcher` + `PipeListener` + guard/compiler/audit/settings/host/ViewModel (Core) chạy y nguyên. Cuối phase: `mcp_call.py` (host autocad, phase 3) chưa có; kiểm bằng **test harness pipe thuần** (script Python nối `hprebar-mcp-acad2026`, gửi `autocad.ping/context/execute`) hoặc bằng `PipeRoundTripTests` mở rộng với executor giả.

## Key insights
- Chỉ 6 file đụng AutoCAD API: `MainThreadExecutor`, `AutocadScriptRunner`, `AutocadContextReader`, `AutocadResultSerializer`, `DatabaseChangeCounter`, `AutocadScriptGlobals` (+ `AutocadUnits`, `BridgeEntry`). Mọi thứ khác là Core.
- `tr` là global; script không được Commit/Abort nó (guard AutoCAD deny `tr.Commit`/`tr.Abort`/`tr.Dispose`).
- `none` = mở `tr`, luôn Abort; `Changed ≠ 0` → lỗi. `manual` = script mở nested; bridge kiểm `NumberOfActiveTransactions == 1` trước Commit.
- Không có failure preprocessor; rủi ro dialog đến từ prompt → guard.

## Requirements
Functional
- `IBridgeExecutor` implement đầy đủ: `IsBusy`, `CompiledScriptCount`, `ActiveDocumentTitle` (cập nhật từ `DocumentCollection.DocumentActivated/DocumentToBeDestroyed`), `StateChanged`, `RunCompleted`, `ExecuteAsync`, `GetContextAsync`, `Inspect` (TypeInspector với `AcMgd/AcCoreMgd/AcDbMgd`), `Analyze` (`ScriptAnalyzer.Run(compiler, code, AnalyzerProfile.Autocad)`), `Cancel`.
- `MainThreadExecutor` theo ADR-02 (queue + Idle one-shot + `IsQuiescent` + `BusyGraceSeconds` → `-32002`); nếu spike chốt `ExecuteInApplicationContext` thì thay thân `Enqueue`, giữ interface.
- `AutocadScriptRunner.Run(doc, request, script, progress, cancelSource)` theo bảng ADR-03: `LockDocument` → `StartTransaction` → globals (`doc, db, ed, app, tr, units, ct, log, progress, args`) → `RunAsync(...).GetAwaiter().GetResult()` → timeout check → mode handling → `Commit`/`Abort` → `ExecuteResult` (Value/ValueType/Truncated qua serializer; `Changed` từ counter; `RolledBack`; `TimedOut`; `DurationMs`; logs cap `MaxLogLines`).
- `AutocadUnits`: `Insunits` (enum → string), `Factor` (mm per drawing unit: Millimeters 1, Centimeters 10, Meters 1000, Inches 25.4, Feet 304.8, Unitless 1 + warning), `ToDrawing(mm)`, `ToMm(du)`.
- `AutocadContextReader.Read(doc, includeSelection, executionEnabled)` → `ContextResult { Host="autocad", HostVersion="2026", RevitVersion="2026" (wire), DocTitle=doc.Name, DocPath=db.Filename, IsReadOnly, IsModifiable=!IsReadOnly && IsQuiescent, Units=UnitsInfo(insunits label), ActiveView=ViewInfo(layout handle, LayoutManager.Current, TileMode ? "Model" : "Layout"), Selection=SelectImplied() → ElementInfo(Handle.Value, Layer, DxfName), OpenDocs=DocumentManager names, ExecutionEnabled, Autocad=AutocadInfo(insunits, measurement, currentLayout, currentLayer, isModelSpace, isQuiescent, isNamedDrawing) }`.
- `AutocadResultSerializer`: converters ADR-03; trần 64 KB + `truncated`.
- `DatabaseChangeCounter.Begin(db)` → `Counts` (`ChangedCounts`), `HashSet<ObjectId>` cho appended/modified/erased, subscribe trước transaction, unsubscribe `Dispose`.
- Guard AutoCAD (`GuardProfile.Autocad`, Core, phase 0 đã có khung): deny identifiers/members bổ sung: `GetSelection, GetPoint, GetEntity, GetString, GetKeywords, GetInteger, GetDouble, GetAngle, GetDistance, GetCorner, GetFileNameForOpen, GetFileNameForSave, GetNestedEntity, SelectWindow, SelectCrossingWindow, SelectFence, SelectPolygon` (prompt/tương tác), `SendStringToExecute, Command, CommandAsync, ExecuteInApplicationContext, ExecuteInCommandContextAsync, ShowModalDialog, ShowModalWindow, ShowAlertDialog, MessageBox, Quit, Close` (trên `Application`/`Document`), `Commit, Abort, Dispose` khi receiver là identifier `tr`; namespaces deny thêm `Autodesk.AutoCAD.Interop`, `Autodesk.AutoCAD.Runtime.SystemObjects`? (chỉ nếu cần), `System.Windows.Forms`.
- `BridgeEntry.Start`: settings (`BridgeSettingsStore("McpBridge.Autocad")`, `ExecutionEnabled=false`), compiler (refs + imports architecture §3), inspector, audit (`%AppData%\HPRebar\McpBridge.Autocad\audit\`), executor, `McpBridgeHost(executor, settings, pipeName: PipeNaming.For("autocad", year), hostVersion: year)`, self-check, `AutoStartListener` → `Start()`. Series→năm: `Application.Version` 25.1 → 2026 (bảng nhỏ; unknown → `AutocadVersion` build constant + warning).
- Status window `View/AutocadBridgeStatusView.xaml` (WPF, ≤ 150 dòng, DynamicResource với `Resources/Themes/AutocadTheme.xaml` tối giản: Brush.Background/Foreground/Accent/Border, Spacing.4/8/16; không link theme HPRebar) bind `McpBridgeStatusViewModel` (Core): status, pipe name, opt-in checkbox, auto-start, start/stop/restart, last run, audit folder link, compiled count. Mở qua `Core.Application.ShowModelessWindow(window)`; `static` field giữ instance (Activate khi đã mở); `Closed` → không dispose host (host sống theo AutoCAD).
- Loader commands: `HPMCPBRIDGE` → `handle.ShowWindow()`; `HPMCPSTART/STOP` → `handle.Start/Stop`; `HPMCPSTATUS` → `ed.WriteMessage(handle.Status())`.
Non-functional
- Bridge không bao giờ ném ra khỏi `OnIdle`/`Execute` (mọi lỗi → `TrySetException` → dispatcher → JSON-RPC error).
- `ed.WriteMessage` một dòng sau mỗi run (`[MCP] label: ok/error, N changed, rolledBack`).

## Architecture
```
HPRebar.McpBridge.Autocad/
├── BridgeEntry.cs · BridgeHandle.cs
├── MainThreadExecutor.cs (IBridgeExecutor) · AutocadBridgeRequest.cs
├── Model/AutocadScriptGlobals.cs · Model/AutocadUnits.cs
├── Service/AutocadScriptRunner.cs · Service/AutocadContextReader.cs · Service/AutocadResultSerializer.cs
├── Service/DatabaseChangeCounter.cs · Service/ScriptingSelfCheck.cs · Service/AutocadVersionMap.cs
├── View/AutocadBridgeStatusView.xaml(.cs) · Resources/Themes/AutocadTheme.xaml
└── (ViewModel dùng Core.ViewModel.McpBridgeStatusViewModel; IMcpBridgeRunner = Core.Host.McpBridgeHost)
HPRebar.McpBridge.Core/Scripting/GuardProfile.cs   ✎ + GuardProfile.Autocad
HPRebar.McpBridge.Core/Scripting/AnalyzerProfile.cs ✎ + AnalyzerProfile.Autocad (StartTransaction/StartOpenCloseTransaction)
```

## Related code files
- **Tái dùng nguyên:** Core `Pipe/*`, `Scripting/{ScriptCompiler,ScriptCache,ScriptArgs,TypeInspector}`, `Model/{BridgeSettings,BridgeStatus,AuditLogger}`, `Host/McpBridgeHost`, `ViewModel/*`; Contracts.
- **Tách ra chung / sửa:** Core `GuardProfile` (+Autocad), `AnalyzerProfile` (+Autocad); tests `ScriptGuardTests` thêm theory AutoCAD.
- **Viết mới:** tất cả file trong `HPRebar.McpBridge.Autocad/` liệt kê trên; `HPRebar.Mcp.Server.Tests/GuardProfileAutocadTests.cs`, `MainThreadExecutorTests.cs` (nếu executor tách được phần Revit-free — `IdleQueue` trong Core với `Func<bool> isQuiescent`, `Action subscribe/unsubscribe` giả).

## Implementation steps
1. Core: `GuardProfile.Autocad`, `AnalyzerProfile.Autocad` + tests (deny `ed.GetPoint`, `tr.Commit()`, `doc.SendStringToExecute`; allow `ed.WriteMessage`, `tr.GetObject`; analyzer thấy `db.TransactionManager.StartTransaction()` → `UsesTransaction`).
2. `AutocadScriptGlobals`, `AutocadUnits` (+ xUnit cho `Factor` bằng enum int values — `UnitsValue` là enum trong AcDbMgd; test tham chiếu DLL từ NuGet cache qua `MetadataReference`? Không cần: `AutocadUnits` nhận `int insunits` để test thuần).
3. `DatabaseChangeCounter`, `AutocadResultSerializer` (converter factory như Revit).
4. `AutocadScriptRunner` theo bảng ADR-03; `RollBack` helper: `tr.Abort()` trong try/catch, trả `rolledBack` đúng như Revit ("never reads rolledBack for a drawing that still changed").
5. `MainThreadExecutor` (kết quả spike): queue, `_busy`, Idle one-shot, grace, `Cancel`, `RunCompleted` → audit (`AuditEntry` tái dùng: `DocTitle=doc.Name`).
6. `AutocadContextReader`, `AutocadVersionMap`.
7. `BridgeEntry.Start` wiring + `ScriptingSelfCheck` probe có document; xoá `SpikeRunner`/`HPMCPSPIKE`.
8. Status window + theme + loader commands.
9. Kiểm trong AutoCAD bằng harness pipe thuần (Python ~40 dòng: `win32pipe`/`open(r'\\.\pipe\hprebar-mcp-acad2026','r+b')` NDJSON): `autocad.ping` → pong; `autocad.context` → JSON; `autocad.execute` 10 kịch bản: read (`return db.Filename`), dryRun tạo Line (`changed.added=1, rolledBack=true`, drawing không đổi), commit thật (Line xuất hiện, `U` undo được), exception (`throw`) → rolledBack, `none` + tạo Line → `IsError` + rolledBack, `manual` đúng (nested Commit) → commit, `manual` sai (nested để mở) → lỗi + rollback, guard (`ed.GetPoint`) → diagnostics, compile error → diagnostics, cancel (`cancel_execution` qua `autocad.cancel` giữa vòng lặp `ct`) → rolledBack, timeout 5 s với vòng lặp không hợp tác → `timedOut` + rolledBack, busy (gõ `LINE` dở) → `-32002` sau grace.

## Todo
- [ ] 1 guard/analyzer profile · [ ] 2 globals/units · [ ] 3 counter/serializer · [ ] 4 runner · [ ] 5 executor · [ ] 6 context · [ ] 7 entry/self-check · [ ] 8 UI/commands · [ ] 9 harness pipe 10 kịch bản → `reports/phase-02-bridge-runtime.md`

## Success criteria
- `dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false -p:DeployBundle=false` xanh.
- `dotnet test HPRebar/HPRebar.Mcp.Server.Tests` xanh (+ guard/analyzer/units tests).
- Trong AutoCAD 2026: `HPMCPBRIDGE` mở window; bật listener + opt-in; harness pipe pass 10/10 kịch bản; mỗi run có dòng audit JSON trong `%AppData%\HPRebar\McpBridge.Autocad\audit\`; Undo (`U`) gỡ đúng một run.
- Tắt opt-in → `autocad.execute` trả `-32001`; đóng drawing → `-32003`.

## Risks
| Risk | Mitigation |
|---|---|
| `ObjectModified` bắn cho object bảng (BlockTableRecord) khi thêm entity → đếm sai | `HashSet` + chỉ đếm `Entity`/`DBObject` không phải `SymbolTable*` (lọc theo `DBObject is Entity`); ghi rõ "đếm xấp xỉ" trong docs |
| Abort transaction ngoài cùng khi script để nested mở → `eTransactionAborted`/crash | kiểm `NumberOfActiveTransactions` trước; abort nested trước bằng `TopTransaction.Abort()` lặp tới khi còn 1 |
| Script chạy `ed.SelectAll` rất lớn → JSON 64 KB truncated | serializer cắt + `truncated=true` như Revit; seed dùng `limit` |
| WPF window trong ALC riêng: `pack://` resource/theme không resolve | theme tối giản inline trong XAML; không dùng `pack://application` với assembly khác |
| `Application.Idle` bắn quá thường xuyên → CPU | subscribe chỉ khi queue không rỗng; unsubscribe ngay khi drain |

## Security
9 lớp ADR-04 Revit giữ nguyên; lớp 3 mở rộng cho AutoCAD (prompt/command/modal deny). `tr` không Commit được từ script. Audit riêng folder AutoCAD.

## Next steps
Phase 3 nối server (host profile AutoCAD) để thay harness pipe thuần bằng `mcp_call.py` qua stdio.
