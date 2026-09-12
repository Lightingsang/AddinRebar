# Phase 04 — Revit API: Execution · Transactions · Security

## Context Links
- [architecture.md §2 sequence, §4 ExecuteResult, §7 busy](architecture.md) · [ADR-03 Roslyn/transaction/timeout/cache](adr/adr-03-roslyn-in-process-execution.md) · [ADR-04 lớp 3-7, 9](adr/adr-04-execute-code-security-model.md)
- `research/roslyn-scripting-in-revit-report.md` §2 (`CSharpScript.Create` + cache), §4 (`IFailuresPreprocessor`), §5 (không abort — cooperative), §6 (assembly không unload), §7 (không sandbox), Recommended Defaults
- NotebookLM Q11 [3-8] progress, [12-18] cancellation; Q12 [226-227] lỗi actionable
- Phase 2: `Service/IRevitExecutor.cs`, `McpBridgeRequest.cs`, `Service/McpBridgeHost.cs`, `Model/BridgeSettings.cs`
- Verified pattern: `HPRebar/HPRebar/ColumnRebar/ColumnRebarExternalEventHandler.cs:19-79` (`ConcurrentQueue` + `ExternalEvent.Create(this)` + drain trong `Execute` + `TrySetResult/TrySetException` + `Dispose`); `ColumnRebarRequest.cs:32-33` (`RunContinuationsAsynchronously`)
- `HPRebar/HPRebar/HPRebar.addin:11` `UseRevitContext=False` → add-in ALC riêng (giống bridge)

## Overview
- **Status (thực tế 2026-09-12):** implemented + built + 42 xUnit (guard 24 case, compiler/cache/inspector/audit) + **verified trong Revit 2026**: self-check `return app.VersionNumber…` OK (2008 ms compile đầu), `get_revit_context`/`inspect_type Wall` qua pipe thật OK, `execute_revit_code` → `-32001` khi opt-in OFF. Sau khi có UI: 13 kịch bản execute chạy thật trong Revit 2026 — `doc.Title`, đếm Level, dryRun `Level.Create` (changed.added=1, rolledBack, đếm không đổi), tạo Level thật (persist), exception → rollback (đếm không đổi), `none`+modify → hint, guard reject (dòng+lý do), compile error CS1061, cancel_execution giữa chừng (2 s, rollback), timeout cooperative. Quyết định thêm: timeout đã bắn → luôn thất bại + rollback kể cả khi script return (ADR-03; verified 12b/12c). Progress notifications tới client OK nhưng bị đảo thứ tự (Progress<T> → thread pool) → thay bằng `SynchronousProgress<T>` (Contracts) ở cả bridge lẫn server; test `Rapid_progress_reports_arrive_in_order` (20 bước) pass; bản bridge đang deploy trong Revit CHƯA có fix này (deploy ở phase 5). Review: 2🔴 (rollback return, cancel race) + 1🟡 (event leak) đã sửa.
- **Priority:** P1 · **Status:** pending · **Effort:** 16h
- Hiện thực `IRevitExecutor` bằng `McpBridgeExternalEventHandler` + 9 service: guard → compile (pipe thread) → queue → `Execute` trên Revit thread → transaction policy → serialize → audit. Wire `McpBridgeHost.Current` trong `Application.OnStartup`.

## Key Insights
- **Type identity qua ALC (risk #1 plan.md):** Roslyn `InteractiveAssemblyLoader` có thể load `HPRebar.McpBridge.dll` lần 2 theo path → `ScriptGlobals` của script ≠ của host → `InvalidCastException` khi `RunAsync(globals)`. Bắt buộc `var loader = new InteractiveAssemblyLoader(); loader.RegisterDependency(typeof(ScriptGlobals).Assembly); loader.RegisterDependency(typeof(Document).Assembly); …` và truyền vào `CSharpScript.Create<object>(code, options, typeof(ScriptGlobals), loader)`. **Spike bước 1 trước mọi thứ.** `[UNVERIFIED — chưa có bằng chứng thực nghiệm trong repo]`
- `Application` bị shadow: bridge có `HPRebar.McpBridge.Application` → alias `using RevitApplication = Autodesk.Revit.ApplicationServices.Application;` trong `ScriptGlobals`, `RevitContextReader`, handler.
- Globals phải `public` class + `public` field tên chữ thường (`doc`, `uidoc`, …) — tên hiển thị cho AI; comment giải thích tại sao vi phạm PascalCase.
- `Script.RunAsync` gọi đồng bộ trên Revit thread (`.GetAwaiter().GetResult()`) — script có `await` → continuation không có thread → deadlock. **Deny `await` trong ScriptGuard** (bổ sung ADR-04 lớp 3, ghi `plan.md` #9).
- `ExternalEvent.Raise()` trả `ExternalEventRequest.Denied/TimedOut` khi Revit ở modal → trả `isError` "Revit is in a modal state" ngay, không treo TCS.
- `manual` mode: script để `Transaction` mở → `doc.IsModifiable == true` sau run → `TransactionGroup.RollBack()` ném; phải phát hiện, báo `isError` "script left a transaction open", và `RollBack` group trong try/catch.
- `DocumentChanged` bắn khi `t.Commit()` — đăng ký trước run, huỷ trong `finally`; với `dryRun` đếm vẫn có (commit rồi group rollback) → hữu ích: "would have changed".
- `System.Text.Json` serialize `Element` thô sẽ duyệt property ném exception → `ResultSerializer` phải chặn mọi `Autodesk.Revit.*` không có converter → `{type, text: ToString()}`; `MaxDepth=8`, `ReferenceHandler.IgnoreCycles`.

## Requirements
**Functional**
- `revit.execute` đúng policy bảng ADR-03 (`auto`/`manual`/`none`/`dryRun`); kết quả `ExecuteResult` §4 đầy đủ field; diagnostics có `line/column/id/message`.
- Timeout cooperative: `cts.CancelAfter(timeoutSeconds)`; hết hạn → `timedOut:true`, script vẫn chạy tới khi hợp tác; `revit.cancel` cùng `cts`.
- `revit.context` và `revit.inspect` hoạt động độc lập opt-in (ADR-04 lớp 1).
- Audit mỗi run vào `%AppData%\HPRebar\McpBridge\audit-.log` (Serilog rolling day, không xoá <30 ngày).
- Không bao giờ hiện dialog Revit khi AI điều khiển (`AutoDismissFailurePreprocessor` + `SetClearAfterRollback(true)`).
**Non-functional**
- Compile lần đầu ≤ 2s, cache hit < 50 ms; cache LRU 50; output ≤ 64 KB (`truncated`); `log()` ≤ 200 dòng; file < 300 dòng.

## Architecture
```
pipe thread                                   Revit API thread (ExternalEvent.Execute)
RequestDispatcher.revit.execute
 → handler.ExecuteAsync(req, progress, ct)
    ScriptGuard.Check(code)  ─deny→ ExecuteResult{isError, diagnostics[{line,col,id:"GUARD",message}]}
    ScriptCompiler.GetOrCompile(code) ─diag→ ExecuteResult{isError, diagnostics}
    enqueue McpBridgeRequest{req, script, progress, cts} ; Raise() ─Denied→ isError "modal"
                                              Execute(uiapp): while TryDequeue
                                                RevitContextReader.Guard(uiapp, req) → -32003 / isError read-only / family
                                                DocumentChangeCounter.Begin(app)
                                                ScriptRunner.Run(doc, script, globals, policy, cts) ─► TransactionGroup/Transaction table
                                                ResultSerializer.Serialize(returnValue, 64KB)
                                                AuditLogger.Write(entry) ; LastRun = …
                                                request.Completion.TrySetResult(result)
 ← await Completion.Task → JSON-RPC response
revit.context → handler.GetContextAsync → enqueue ContextRequest → Execute → RevitContextReader.Read(uiapp, includeSelection)
revit.inspect → TypeInspector.Inspect (reflection, pipe thread, không cần Revit thread)
revit.cancel  → handler.Cancel() → _currentCts?.Cancel()
```
Transaction policy trong `ScriptRunner.Run` (ADR-03 bảng, nguyên văn): `auto` → `TransactionGroup g(doc, "MCP: "+label)`, `g.Start()`, `Transaction t(doc, "MCP script")` + failure options, `t.Start()`, run, `t.Commit()`, `dryRun ? g.RollBack() : g.Assimilate()`; exception → `t.RollBack()` nếu `t.HasStarted()`, `g.RollBack()`. `manual` → chỉ `g`; sau run `doc.IsModifiable` → lỗi. `none` → không transaction; `ModificationOutsideTransactionException` → `isError` + hint "use transaction:\"auto\"".

## Related Code Files
**To create (proposed) — `HPRebar/HPRebar.McpBridge/`**
- `McpBridgeExternalEventHandler.cs` (namespace `HPRebar.McpBridge`; `: IExternalEventHandler, IRevitExecutor, IDisposable`)
- `Model/ScriptGlobals.cs`, `Model/ExecutionOutcome.cs` (kết quả nội bộ trước serialize), `Model/AuditEntry.cs`
- `HPRebar.McpBridge.Core/Scripting/ScriptGuard.cs` (`CSharpSyntaxWalker`), `HPRebar.McpBridge.Core/Scripting/ScriptCompiler.cs` (options + LRU cache SHA-256; Revit-free, nhận references + globals type từ bridge), `HPRebar.McpBridge.Core/Scripting/ScriptCache.cs`, bridge `Service/ScriptRunner.cs` (policy + watchdog), `Service/AutoDismissFailurePreprocessor.cs`, `Service/DocumentChangeCounter.cs`, `Service/RevitContextReader.cs`, `Service/TypeInspector.cs`, `Service/ResultSerializer.cs` (+ converters `ElementId`, `XYZ`, `Element`, fallback Revit object), `Service/AuditLogger.cs`
**To modify (proposed)**
- `HPRebar/HPRebar.McpBridge/Application.cs` — `OnStartup`: tạo handler (`ExternalEvent.Create` hợp lệ trong `OnStartup`), `McpBridgeHost.Current = new McpBridgeHost(handler, settings, version)`; `AutoStartListener` → `StartAsync()`; `OnShutdown`: `Current?.Dispose()` (dừng pipe, dispose ExternalEvent), `Log.CloseAndFlush()`
**To delete:** none

## Implementation Steps
1. **Spike type identity (0.5 ngày, chặn mọi bước sau):** trong bridge tạm thêm nút/log ở `OnStartup`: compile `"return doc.Title;"` với `InteractiveAssemblyLoader` + `RegisterDependency` như Key Insights, chạy với globals thật → ghi log OK/`InvalidCastException`. Nếu fail: thử `ScriptOptions.WithMetadataResolver`/`Assembly.Location` reference cùng instance; nếu vẫn fail → escalate (ADR-03 alt 1). Xoá code spike sau khi có kết luận; ghi kết luận vào `plan.md` risk #1.
2. `Model/ScriptGlobals.cs`: `public sealed class ScriptGlobals { public Document doc; public UIDocument uidoc; public RevitApplication app; public UIApplication uiapp; public CancellationToken ct; public Action<string> log; public Action<int,int,string> progress; }` (+ ctor). Comment: tên chữ thường là API cho script.
3. `HPRebar.McpBridge.Core/Scripting/ScriptGuard.cs` (lib Revit-free, xUnit test được): `CSharpSyntaxTree.ParseText(code, CSharpParseOptions(kind: SourceCodeKind.Script))`; walker kiểm `UsingDirectiveSyntax`, `QualifiedNameSyntax`/`MemberAccessExpressionSyntax` chuỗi đầy đủ, `TypeOfExpressionSyntax`, `LiteralExpressionSyntax` string chứa `"System.Reflection"`, `AwaitExpressionSyntax`, `UnsafeStatementSyntax`, `IdentifierName "dynamic"`. Deny-list ADR-04 lớp 3 nguyên văn (`System.IO` trừ `System.IO.Path`, `System.Net`, `System.Diagnostics.Process`, `System.Reflection*` trừ `.GetType().Name/.FullName`, `System.Runtime.InteropServices`, `System.Threading.Thread`, `Environment.Exit`, `Assembly.Load*`, `AppDomain`, `dynamic`, `unsafe`) + `await`. Trả `IReadOnlyList<ScriptDiagnostic>` (id `"GUARD"`, line/col 1-based). Docstring: có thể bypass — chỉ chặn lỗi vô ý.
4. `HPRebar.McpBridge.Core/Scripting/ScriptCompiler.cs` (Revit-free; bridge truyền `IEnumerable<Assembly>` + `typeof(ScriptGlobals)`): `ScriptOptions.Default.WithReferences(<references từ bridge: typeof(Document).Assembly, typeof(UIDocument).Assembly>, typeof(object).Assembly, typeof(Enumerable).Assembly, typeof(List<>).Assembly, Assembly.Load("netstandard"), Assembly.Load("System.Runtime")).WithImports("System","System.Linq","System.Collections.Generic","Autodesk.Revit.DB","Autodesk.Revit.UI","Autodesk.Revit.DB.Structure").WithEmitDebugInformation(false).WithOptimizationLevel(OptimizationLevel.Release)`; `CSharpScript.Create<object>(code, options, typeof(ScriptGlobals), loader)`; `script.Compile()` → diagnostics `Severity == Error` → `ScriptDiagnostic{line+1, col+1, Id, GetMessage()}`; cache `Dictionary<string, Script<object>>` key SHA-256(code + options hash), LRU 50 (LinkedList), lock; `CompiledCount` tăng mỗi compile thật. Chạy trên pipe thread (không đụng Revit).
5. `Service/AutoDismissFailurePreprocessor.cs`: `PreprocessFailures`: `foreach f in a.GetFailureMessages()`: `Severity==Warning` → `a.DeleteWarning(f)`; else → `return FailureProcessingResult.ProceedWithRollBack`; cuối `Continue`. Áp cho mọi `Transaction` bridge mở: `var o = t.GetFailureHandlingOptions().SetFailuresPreprocessor(pre).SetClearAfterRollback(true); t.SetFailureHandlingOptions(o);`.
6. `Service/DocumentChangeCounter.cs`: `Begin(RevitApplication app)` subscribe `app.DocumentChanged` → cộng `GetAddedElementIds().Count` v.v.; `End()` unsubscribe, trả `ChangedCounts`. Dùng `using` pattern (IDisposable).
7. `Service/RevitContextReader.cs`: `Guard(uiapp, request)` → `ActiveUIDocument null` → `BridgeErrorCode.NoActiveDocument`; `transaction != none && doc.IsReadOnly` → isError "document is read-only"; `doc.IsFamilyDocument && !settings.AllowFamilyDocuments` → isError. `Read(uiapp, includeSelection)` → `ContextResult`: `revitVersion=int.Parse(app.VersionNumber)`, `docTitle`, `docPath` (`doc.PathName` rỗng nếu chưa lưu), `isFamily`, `isReadOnly`, `units.length = LabelUtils.GetLabelForUnit(doc.GetUnits().GetFormatOptions(SpecTypeId.Length).GetUnitTypeId())`, `activeView {id=.Id.Value, name, type=ViewType.ToString()}` (alias `RevitView`), `selection` = `uidoc.Selection.GetElementIds()` → `{id, category = Category?.Name, name}`, `openDocs` = `app.Documents` titles. Chạy trên Revit thread qua queue.
8. `Service/TypeInspector.cs`: resolve `typeName` (đơn hoặc full) trong `typeof(Document).Assembly` + `typeof(UIDocument).Assembly` (`GetExportedTypes()` so `Name`/`FullName`, case-insensitive; nhiều match → trả danh sách ứng viên isError); members `GetMembers(Public|Instance|Static|DeclaredOnly)` → `{kind: Method|Property|Field|Event, signature}` (method: return type + params); `memberFilter` substring; `maxMembers` clamp 1–500; `truncated`. Pipe thread, không Revit context.
9. `Service/ResultSerializer.cs`: `JsonSerializerOptions` từ `BridgeJson.Options` + converters: `ElementId → .Value (long)`, `XYZ → {x,y,z}` feet, `Element → {id, name, category}` (mọi subclass qua `JsonConverterFactory`), fallback factory cho `Autodesk.Revit.*` → `{type: FullName, text: ToString()}`; `IEnumerable` bình thường; `null` → `"null"`; `valueType = value?.GetType().FullName`; serialize vào `Utf8JsonWriter` + `ArrayBufferWriter`, quá `MaxOutputBytes` → cắt UTF-8 an toàn + `truncated:true`.
10. `Service/AuditLogger.cs`: logger Serilog riêng (`new LoggerConfiguration().WriteTo.File(Path.Combine(%AppData%, "HPRebar","McpBridge","audit-.log"), rollingInterval: Day, retainedFileCountLimit: null, outputTemplate "{Timestamp:o} {Message:lj}{NewLine}")`); `Write(AuditEntry)`: timestamp, `Environment.UserName`, doc title, SHA-256(`doc.PathName`), script SHA-256, source đầy đủ (escaped 1 dòng), transaction mode, dryRun, outcome (ok/error/timeout/guard/compile), durationMs, changed counts. Lưu ý: `%AppData%` (Roaming) theo ADR-04, khác thư mục log runtime `%LocalAppData%`.
11. `Service/ScriptRunner.cs`: `ExecutionOutcome Run(UIApplication uiapp, McpBridgeRequest r)`: build globals (`log` → `List<string>` cap 200 rồi bỏ + đếm; `progress` → `r.Progress?.Report(new ProgressParams{Id, Progress, Total, Message})`); `cts = CancellationTokenSource.CreateLinkedTokenSource(r.Cancellation)`; `cts.CancelAfter(timeout)`; đặt `_currentCts` (volatile) cho `Cancel()`; policy như Architecture; `Stopwatch`; bắt `CompilationErrorException` (không xảy ra — đã compile), `OperationCanceledException` → `timedOut = cts.IsCancellationRequested && !r.Cancellation.IsCancellationRequested ? true : false` + rollback, `Autodesk.Revit.Exceptions.ModificationOutsideTransactionException` → hint, `Exception` → `message = ex.GetType().Name + ": " + ex.Message` strip path (regex như server), không stack trace. `finally`: `_currentCts = null`, counter End, dispose `t`/`g`.
12. `McpBridgeExternalEventHandler.cs`: mirror `ColumnRebarExternalEventHandler`: fields `ConcurrentQueue<McpBridgeRequest> _pending`, `ExternalEvent _externalEvent = ExternalEvent.Create(this)`, `ScriptGuard`, `ScriptCompiler`, `ScriptRunner`, `AuditLogger`, `BridgeSettings`; `IsBusy` = `Interlocked` flag đặt khi enqueue, bỏ khi hoàn tất. `ExecuteAsync`: guard → compile → nếu diagnostics trả ngay (audit outcome guard/compile) → `Raise()` kiểm `ExternalEventRequest.Accepted` → `await request.Completion.Task.ConfigureAwait(false)` (không `true` — không có sync context trên pipe thread). `Execute(uiapp)`: drain; mỗi request `try { outcome = runner.Run; result = serializer + audit; TrySetResult } catch { Log.Error; TrySetResult(isError) }` — **không** TaskDialog (khác ColumnRebar — AI điều khiển, không dialog). `GetContextAsync` dùng queue riêng `ConcurrentQueue<ContextRequest>` cùng `Execute` (drain cả hai). `GetName() => "HPRebar MCP Bridge"`. `Dispose` → `_externalEvent.Dispose()`.
13. `Application.cs` wire (mục To modify). Version: `int.Parse(Application.VersionNumber)` từ `ExternalApplication.Application` (UIControlledApplication `ControlledApplication.VersionNumber`).
14. Build gate `dotnet build HPRebar.slnx -c Debug.R26 -p:DeployAddin=false`; F5 → bật listener → Inspector `execute_revit_code {code:"return doc.Title;", transaction:"none"}` với opt-in OFF → `IsError` "code execution disabled by user"; bật → trả title; `{code:"var l = new FilteredElementCollector(doc).OfClass(typeof(Level)).First(); return Level.Create(doc, 99).Id;", dryRun:true}` → `rolledBack:true`, `changed.added ≥ 1`, Undo history Revit không đổi; `dryRun:false, label:"level"` → Undo history có "MCP: level"; `{code:"while(!ct.IsCancellationRequested){} return 1;", timeoutSeconds:5}` → `timedOut:true` sau ~5s, Revit sống.

## Todo List
- [ ] Spike type identity → kết luận ghi plan.md
- [ ] `ScriptGlobals`, `ExecutionOutcome`, `AuditEntry`
- [ ] `ScriptGuard` (+ `await`), `ScriptCompiler` (LRU 50), `AutoDismissFailurePreprocessor`, `DocumentChangeCounter`
- [ ] `RevitContextReader`, `TypeInspector`, `ResultSerializer` (converters + 64 KB), `AuditLogger`
- [ ] `ScriptRunner` policy table + watchdog + manual-mode open-transaction check
- [ ] `McpBridgeExternalEventHandler` + `Application.cs` wire + `OnShutdown` dispose
- [ ] Build gate + 5 kịch bản Inspector bước 14

## Success Criteria
- `dotnet build HPRebar.slnx -c Debug.R26 -p:DeployAddin=false` → exit 0; mọi file `Service/*.cs` < 300 dòng.
- 5 kịch bản Inspector bước 14 đúng như mô tả (opt-in OFF, title, dryRun rollback, Undo group tên, timeout cooperative).
- `grep -n "TaskDialog\|MessageBox" HPRebar/HPRebar.McpBridge/McpBridgeExternalEventHandler.cs HPRebar/HPRebar.McpBridge/Service/*.cs` → 0.
- Audit file xuất hiện tại `%AppData%\HPRebar\McpBridge\audit-<date>.log` với 5 dòng tương ứng 5 run, có SHA-256 + source.
- Script `return new FilteredElementCollector(doc).OfClass(typeof(Wall)).Cast<Wall>().Select(w => new { id = w.Id, loc = ((LocationCurve)w.Location).Curve.GetEndPoint(0) }).ToList();` → JSON `id` là số, `loc` là `{x,y,z}`, không exception serializer.
- Guard: `using System.IO; File.Delete("x");` → `isError` với diagnostics id `GUARD` dòng 1, không compile.

## Risk Assessment
| Risk | L×I | Mitigation |
|---|---|---|
| Type identity ALC (spike fail) | M×H | Bước 1 chặn; fallback ADR-03 alt 1 → escalate user, không tự đổi ADR |
| Script không hợp tác → Revit treo, `Restart bridge` vô dụng | M×M | Deny `await`/`Thread`; description tool + docs nói rõ; user kill Revit; audit ghi trước khi chạy (entry "started") để truy vết |
| `DocumentChanged` không bắn cho `dryRun` theo kỳ vọng / bắn thêm từ regen | M×L | Test TUnit phase 5 xác nhận count; docs "approximate" |
| Serializer đụng property Revit ném `InvalidObjectException` | M×M | Fallback factory cho mọi `Autodesk.Revit.*`; `MaxDepth 8`; test với `Wall`, `XYZ`, `ElementId`, `null` |
| Memory tăng theo số script compile (không unload) | H×L | `CompiledScriptCount` UI + cảnh báo >500; LRU chỉ giữ 50 `Script<object>` nhưng assembly vẫn ở lại — ghi docs |
| `ExternalEvent.Create` ngoài Revit context (nếu gọi từ pipe thread) | L×H | Chỉ tạo trong `OnStartup` (Revit thread); comment nêu ràng buộc |

## Security Considerations
- Toàn bộ 9 lớp ADR-04: lớp 1 kiểm ở dispatcher (phase 2) **và** lại ở handler (defense-in-depth, tránh gọi thẳng); lớp 3 `ScriptGuard`; lớp 4 reference set tối thiểu (không `System.Net.Http`, không `Microsoft.CodeAnalysis`); lớp 5 giới hạn; lớp 6 guards; lớp 7 audit; lớp 9 strip path/stack.
- Không có sandbox — docstring `ScriptGuard`/`ScriptRunner` nói rõ; tool description server đã cảnh báo (phase 2).
- Audit log chứa source script đầy đủ → không chứa secret trừ khi AI/user đưa vào; thư mục per-user Roaming, không share.

## Next Steps
- Phase 5: TUnit rollback/read-only/timeout/guard; Dynamo coexistence; đóng gói Roslyn DLL copy-local vào bundle.
- Backlog: ghi script vào Revit journal (`plan.md` #14), multi-document targeting, `RequireLocalApproval`.
