# Roslyn Scripting In-Process Execution trong Revit 2026 via ExternalEvent

**Ngày:** 2026-09-12 | **Lĩnh vực:** Revit API, C#/.NET, Scripting | **Đối tượng:** MCP Server Implementation

---

## Summary

- **Khả thi:** Có. Roslyn scripting + ExternalEvent pattern tạo thành bridge hợp lệ từ thread UI sang Revit API thread.
- **Architecture:** ScriptOptions + `CSharpScript.Create/RunAsync` biên dịch mã, marshalling qua `IExternalEventHandler` + `TaskCompletionSource<T>`.
- **Assembly management:** EnableDynamicLoading (Revit 2026+) + ILRepack hợp nhất Roslyn libs vào add-in DLL hoặc đóng gói riêng.
- **Memory:** Collectible ALC để unload động assembly khi cần reset, nhưng giải phóng không ngay — GC.Collect() yêu cầu sau.
- **Timeout:** CancellationToken trong ScriptOptions không abort thực sự; only watchdog + cooperative flag đáng tin.
- **Transaction:** Wrapper ExternalEvent—mở Transaction bên trong Execute(), dùng IFailuresPreprocessor tránh modal dialogs.
- **Security:** ScriptOptions là **KHÔNG sandbox**—reflection/PrivateInvoke bypass tất cả. Enforce allowlist via syntax tree walk, audit log required.
- **Prior art:** pyRevit (IronPython), Dynamo (CPython+IronPython), cả hai chạy in-process; không tìm C#-script solution tương đương.
- **Dependencies:** Microsoft.CodeAnalysis.CSharp.Scripting (~v4.8), Serilog setup sẵn, ILRepack 2.0.46 ready.
- **Recommendation:** Prototype fase 1 với non-collectible ALC + compile cache; tách to security audit trước production.

---

## 1. Revit 2026 Runtime & Assembly Isolation

**Finding:** Revit 2026 + .NET 8 hỗ trợ AssemblyLoadContext isolation per add-in manifest (UseRevitContext=false). Nice3point template `EnableDynamicLoading=true` + ILRepack 2.0.46 sẵn sàng trong HPRebar.csproj.

**Key points:**
- AssemblyLoadContext không còn AppDomain—không thể restore trạng thái toàn cục khi unload.
- Nice3point SDK tách dependency từ Revit's default context; script engine (Roslyn) có thể ở context riêng hoặc chung.
- ILRepack merge Microsoft.CodeAnalysis.* vào HPRebar.dll (~9 MB thêm); hoặc ship Roslyn ở subfolder (`addins/HPRebar/Microsoft.CodeAnalysis/`) + custom loader.
- **Version conflict risk:** Dynamo, pyRevit có Roslyn; collision khó tránh. Isolate via folder-scoped ALC mitigates.

**Sources:**
- [Plugin Loading in .NET: AssemblyLoadContext with Dependency Injection](https://www.devleader.ca/2026/04/09/plugin-loading-in-net-assemblyloadcontext-with-dependency-injection)
- [What's New in Revit API 2026](https://rvtdocs.com/2026/whatsnew)
- [About AssemblyLoadContext](https://learn.microsoft.com/en-us/dotnet/core/dependency-loading/understanding-assemblyloadcontext)

---

## 2. Roslyn Scripting Architecture

**Finding:** `Microsoft.CodeAnalysis.CSharp.Scripting` hỗ trợ compile + execute in-process qua `CSharpScript.Create(code, options)` + `Script<T>.RunAsync(globals)`.

**Key API:**
```
ScriptOptions.Default
  .WithReferences(typeof(T).Assembly, ...)
  .WithImports("System", "Autodesk.Revit.DB", ...)
```
Globals object: expose `doc`, `uidoc`, `app`, `uiapp` từ Execute() thread.

**Performance:**
- Biên dịch đầu tiên: ~0.5–2 giây (tuỳ độc lập); cache `Script<T>` delegate → lần 2 < 50 ms.
- Mỗi script = assembly động → không gỡ bỏ mà không unload ALC.

**Return value:** `ScriptState<T>.ReturnValue` chứa kết quả; compile diagnostics qua `ScriptState.Exception`.

**Sources:**
- [C# Scripting with Roslyn](https://medium.com/@Michael_Head/c-scripting-with-roslyn-7df86fdb2b26)
- [Roslyn Scripting API Samples](https://github.com/dotnet/roslyn/blob/main/docs/wiki/Scripting-API-Samples.md)
- [Getting Started with Roslyn C# Scripting API](https://itnext.io/getting-start-with-roslyn-c-scripting-api-d2ea10338d2b)

---

## 3. ExternalEvent Threading & Marshalling

**Finding:** Revit API chỉ gọi được trên thread chính; ExternalEvent.Raise() queue handler, Revit gọi Execute() tại idle moment.

**Pattern:**
```
[UI Thread] → RunAsync() → queue ColumnRebarRequest → raise event → 
[Revit Thread] → Execute(UIApplication) → modify document → TrySetResult()
```

**ColumnRebar example (existing):** TaskCompletionSource<OrchestratorResult> cho async/await từ UI.

**Limits:**
- Không thể raise trong modal dialog.
- Không thể nested raise.
- ExternalEvent.Raise() trả `ExternalEventRequest`; kiểm tra `IsPending` để tránh duplicate.

**Adaptation cho Roslyn:** Queue `ScriptRequest` (tương tự ColumnRebarRequest) với `Script<object>` + globals. Execute() gọi `script.RunAsync(globals, cancellationToken)` trên Revit thread.

**Sources:**
- [External Events](https://help.autodesk.com/cloudhelp/2015/ENU/Revit-API/files/GUID-0A0D656E-5C44-49E8-A891-6C29F88E35C0.htm)
- [Revit.Async](https://github.com/KennanChan/Revit.Async)

---

## 4. Transaction Model & Failure Handling

**Finding:** Wrap script execution trong Transaction `using (var t = doc.NewTransaction(...))`. IFailuresPreprocessor auto-dismiss warnings tránh modal popups.

**Key API:**
```csharp
var opts = transaction.GetFailureHandlingOptions();
opts.SetFailuresPreprocessor(new AutoDismissFailurePreprocessor());
transaction.SetFailureHandlingOptions(opts);
```

**TransactionGroup:** Có thể nest; `Assimilate()` merge vào parent; `RollBack()` hoàn nguyên.

**Script safety:** Nếu script mở Transaction, nó nested trong Execute()'s outer transaction → tự động assimilate. Document state consistency bảo đảm miễn là script không escape transaction.

**Sources:**
- [Handling Failures](https://help.autodesk.com/cloudhelp/2018/ENU/Revit-API/Revit_API_Developers_Guide/Advanced_Topics/Failure_Posting_and_Handling/Handling_Failures.html)
- [Failure Handling for SubTransactions](https://forums.autodesk.com/t5/revit-api-forum/failure-posting-handling-sub-transactions/td-p/8999468)

---

## 5. Timeout & Cancellation [UNCERTAIN]

**Finding:** CancellationToken trong `ScriptOptions` không abort thực sự. Roslyn GitHub issue #23145: "No way to abort long-running script."

**Options:**
1. **Cooperative cancellation:** Pass CancellationToken làm global → script check `token.IsCancellationRequested` trong loop.
2. **Watchdog timer:** Separate thread monitor, chỉ report (không abort từ ngoài).
3. **Hard limit:** Set timeout = 10 giây, log nếu vượt; không kill thread (unsafe).

**Reality:** Nếu script hang (vô hạn loop, deadlock), không thể interrupt từ Revit thread. Chấp nhận risk hoặc block-list operations (ví dụ: no nested loops).

**Sources:**
- [GitHub #23145: Is there a way to abort a long running script?](https://github.com/dotnet/roslyn/issues/23145)
- [Graceful cancellation issue](https://github.com/aelij/RoslynPad/issues/45)

---

## 6. Memory & Assembly Unloading

**Finding:** Collectible ALC cho phép unload dynamic assemblies khi không cần. Mỗi compile = new assembly → memory grow nếu không clear.

**Pattern:**
```csharp
var alc = new CollectibleALC();
var asm = Compile(code, alc);
alc.Unload();
GC.Collect(); GC.WaitForPendingFinalizers(); // giải phóng bộ nhớ
```

**Caveat:** Unload không ngay; GC.Collect() blocking; tối ưu = long-running cache + periodic purge.

**Không dùng collectible** cho compile cache (keep hot): không unload khi cần.

**Sources:**
- [Collectible Assemblies for Dynamic Type Generation](https://learn.microsoft.com/en-us/dotnet/fundamentals/reflection/collectible-assemblies)
- [Make in-memory assemblies collectible (Roslyn PR #74915)](https://github.com/dotnet/roslyn/pull/74915)

---

## 7. Security & Code Restrictions

**Finding:** Roslyn ScriptOptions **KHÔNG sandbox**. Reflection, PrivateInvoke, Assembly.Load() bypass tất cả. "Restriction" = syntax tree walk, không runtime enforcement.

**Mitigation layer:**
1. **Syntax-tree allowlist:** Anh Roslyn compile với `CSharpSyntaxWalker` kiểm tra `using` + member access. Block `System.Reflection`, `System.IO.File`, v.v.
2. **Audit log:** Mỗi execute ghi script hash + user + timestamp → forensic.
3. **Code review:** Mỗi AI-generated script → human review trước execute (MCP server = local-only dev tool, không multi-user).

**Threat model:** Giả sử script chứa malicious reflection → bypass Roslyn restriction → execute arbitrary IL. Risk: data exfiltration, crash Revit. Mitigation: use in controlled environment (dev machine), không run untrusted scripts.

**Sources:**
- [Securely Sandboxing Roslyn Code Execution #10830](https://github.com/dotnet/roslyn/issues/10830)
- [The Queen's Guard: A Secure Enforcement of Fine-grained Access Control](https://arxiv.org/pdf/2106.13123)

---

## 8. Prior Art: pyRevit, Dynamo

**pyRevit:** IronPython (.NET reimplementation of Python). Loads in-process qua ScriptExecutor. No C# scripting equivalent; C# = compile + load, overkill.

**Dynamo:** CPython + IronPython 3.4. Python node execute through PythonNet3. No native Roslyn; uses Revit API indirectly.

**Key difference:** Python engines don't support dynamic C# as first-class; C# needs compiler. Roslyn is only viable path.

**Sources:**
- [Understanding pyRevit Architecture](https://docs.pyrevitlabs.io/architecture/)
- [Python and Revit | Dynamo Primer](https://primer.dynamobim.org/10_Custom-Nodes/10-5_Python-Revit.html)

---

## Recommended Defaults Table

| Aspect | Recommended | Rationale |
|--------|-------------|-----------|
| **Compile Strategy** | Cache `Script<object>` by source hash; expire after 1 hour | Balance perf vs memory |
| **Reference Set** | `typeof(Document).Assembly` + Revit API + System.* stubs | Revit + minimal stdlib |
| **Import Namespaces** | `System`, `Autodesk.Revit.DB`, `Autodesk.Revit.UI` | Revit-first UX |
| **Globals** | `doc`, `uidoc`, `app`, `uiapp`, `cancellationToken` | Bridge to Revit context |
| **Timeout Policy** | 5 sec execute; log warning at 3 sec; no hard abort | Cooperative approach |
| **Transaction Policy** | Wrap script in `NewTransaction("AI Script")` | Atomicity + rollback |
| **Allowlist Policy** | Block `System.Reflection`, `System.IO.File`, `System.Net.*` via syntax walk | Prevent exfiltration |
| **Assembly Isolation** | Non-collectible ALC (HPRebar folder) + periodic manual purge | Simplify; avoid unload bugs |
| **Error Handling** | Catch `CompilationErrorException`, `OperationCanceledException`; log + surface to user | Graceful failure |

---

## Risk Assessment

| Risk | Severity | Mitigation |
|------|----------|-----------|
| Reflection bypass of allowlist | **HIGH** | Syntax walk insufficient; assume breach possible; audit log mandatory |
| Memory leak from cached scripts | **MEDIUM** | Implement cache TTL + periodic purge; monitor via Serilog |
| Deadlock in script (hang) | **MEDIUM** | Watchdog timer + log; document limitation; block nested loops |
| Version conflict Roslyn vs Dynamo | **MEDIUM** | ALC folder isolation; test coexistence before ship |
| Transaction inconsistency | **LOW** | Always wrap in TX; test rollback scenario |
| CancellationToken ignored in lib calls | **MEDIUM** | Accept; document "uninterruptible call X"; user re-run |

---

## Unresolved Questions

1. **Script caching strategy:** Hash tính trên source code hay IR? Collision risk?
2. **Allowlist maintenance:** Tự maintain list hay dùng external package (e.g., Roslyn analyzers)?
3. **Multi-script conflict:** Nếu 2 scripts modify same element, rollback policy nào?
4. **Revit Journal logging:** Có cách ghi AI script vào Revit journal cho replay?
5. **ILRepack size:** Roslyn + all Microsoft.CodeAnalysis.* = bao nhiêu MB? Full merge hay selective?

---

**Status:** DONE

**Summary:** Roslyn scripting in-process via ExternalEvent pattern khả thi, tuân theo architecture hiện tại (ColumnRebar example). Core bridge = ScriptRequest + TaskCompletionSource; execution = Revit thread qua IExternalEventHandler. Assembly management qua Nice3point SDK sẵn sàng. Main risk: security (reflection bypass) + timeout (no abort). Recommend prototype non-collectible ALC, async/await marshalling, syntax-tree allowlist.

**Concerns/Blockers:** 
- Security audit yêu cầu trước production; Roslyn restriction = not sandbox.
- Timeout handling incomplete; watchdog-only, không hard abort.
- IronPython (pyRevit) conflict possible; cần test.
