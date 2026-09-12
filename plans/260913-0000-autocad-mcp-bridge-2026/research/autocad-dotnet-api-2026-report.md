# AutoCAD 2026 .NET API Research Report  
## For MCP Bridge Plugin Architecture

**Date:** 2026-09-13  
**Target:** AutoCAD 2026 managed .NET API  
**Scope:** Threading, transactions, rollback, plugin loading, Roslyn integration  

---

## Summary of Verified Facts

1. **Framework:** AutoCAD 2026 uses **.NET 8.0** primary (`net8.0-windows` TFM); **.NET 10** support in beta preview (breaking update).
2. **NuGet Package:** ~~v26.0.0~~ → **xem §0.1 #1: AutoCAD 2026 = `AutoCAD.NET` 25.1.0 (net8.0, dep Core 25.1.0 → Model 25.1.0); 26.0.0 là AutoCAD 2027 (net10.0); 25.1.1 là 2026 Update 1.2 (net10.0).**
3. **Plugin Isolation:** AutoCAD 2026 does NOT ship built-in AssemblyLoadContext isolation (Inventor 2027 has this; AutoCAD .NET 8 plugins share default ALC). ~~binding redirects or ILMerge~~ → **giải pháp: bridge tự tạo AssemblyLoadContext riêng (§0.3, ADR-05); AutoCAD 2026 ship sẵn Roslyn 4.10 trong thư mục cài.**
4. **Threading Model:** All AutoCAD API calls must run on main thread. `DocumentCollection.ExecuteInApplicationContext(callback, state)` exists; ~~blocks caller~~ → **[unverified], xem §0.1 #3**; on-machine precedent dùng `Application.Idle` + `IsQuiescent` (§0.2). `Document.LockDocument()` required for modeless UI/background threads.
5. **Transactions:** `TransactionManager.StartTransaction()` returns `Transaction`; `Commit()`/`Abort()` are the standard rollback mechanism. Nested transactions supported. ~~`TransactionGroup`~~ → **xem §0.1 #2: AutoCAD managed API không có TransactionGroup; abort transaction ngoài cùng huỷ toàn bộ (kể cả nested đã Commit).**
6. **Undo Grouping:** (a) `Transaction.Abort()` on outermost transaction rolls back all nested work; (b) `UNDO BEGIN/END` groups via `editor.Command("_.UNDO", "_Begin")` from document context; (c) `Database.DisableUndoRecording(bool)` temporarily suppresses undo. DryRun pattern: start transaction, run script, abort if `dryRun=true`, commit otherwise.
7. **Units:** `Database.Insunits` (UnitsValue enum: Millimeters=4, Inches=1, etc.) labels model-space coordinates; `Database.Lunits` controls display format; coordinates are unitless—INSUNITS only annotates them.
8. **Selection:** `Editor.SelectImplied()` gets pickfirst; `SelectAll(SelectionFilter)` non-interactive filter; `SelectionFilter` built from `TypedValue(DxfCode.Start, "LINE")` or `DxfCode.LayerName`.
9. **Change Tracking:** `Database.ObjectAppended`, `ObjectModified`, `ObjectErased` events fire during transaction (before commit); listeners can veto via event args (no rollback on veto, but event handler can raise exception).
10. **Assembly Conflicts:** `System.Collections.Immutable` ≥ 9 breaks AutoCAD 2025/2026 plugins in default ALC `[forum, not fetched]`; **đã kiểm trên máy: Roslyn 5.9.0 cần Immutable 10.0.1, framework .NET 8 có 8.0 (§0.3)**. ~~pin versions conservatively~~ → dùng ALC riêng, giữ Roslyn 5.9.0 như bridge Revit.
11. **Error Suppression:** `Application.SetSystemVariable("FILEDIA", 0)` hides file dialogs; `CMDECHO=0` suppresses command echoes; `NOMUTT=1` hides prompts. `Editor.WriteMessage()` for logging.
12. **Verified Gaps:** TUnit in-process testing (Revit TUnit works; AutoCAD equivalent [unverified]). Roslyn in-process compilation inside acad.exe [unverified — search found no AutoCAD-specific reports, but generic Roslyn works on .NET 8].

---

## Facts That Remain [unverified]

- Whether AutoCAD 2026 ships `Microsoft.CodeAnalysis` or `System.Reflection.Emit` as part of core runtime (blocks/enables Roslyn).
- Whether `ExecuteInCommandContextAsync(Func<object, Task>, object)` exists in AutoCAD 2026 (found reference to it in 2024 docs but not 2026 help).
- Whether `Document.SendStringToExecute()` can be called from application context (vs document context only).
- AutoCAD 2026 on net10.0 .NET 10 — whether ALC isolation is available or still shared default ALC.
- Exact behavior of `ObjectModified` event: does it fire for every field change or once per object per transaction commit?
- Whether a script abort inside `ExecuteInApplicationContext` callback propagates as exception to caller or silently rolls back.

---

## §0 — Corrections & on-machine verification (Claude, 2026-09-13, đọc TRƯỚC các mục dưới)

Report trên do researcher agent viết từ web. Claude kiểm lại các claim load-bearing bằng NuGet page thật, XML docs chính thức trong NuGet cache và máy dev. **Khi mâu thuẫn, mục §0 này thắng.**

### 0.1 Sửa sai

| # | Claim trong report | Thực tế đã kiểm | Nguồn |
|---|---|---|---|
| 1 | "`AutoCAD.NET` 26.0.0 = AutoCAD 2026" | **Sai.** 26.0.0 (2026-03-25) mô tả *"AutoCAD 2027 .Net API"*, TFM **net10.0**. AutoCAD **2026** = **`AutoCAD.NET` 25.1.0** (2025-04-04, *"AutoCAD 2026 .Net API"*, `lib/net8.0`, dep `AutoCAD.NET.Core` [25.1.0] → dep `AutoCAD.NET.Model` [25.1.0]). **25.1.1** (2026-08-12) = *"AutoCAD 2026.1.2 .NET API"*, TFM **net10.0** — bản đi kèm AutoCAD 2026 Update 1.2 (.NET 10). | https://www.nuget.org/packages/AutoCAD.NET/26.0.0 · https://www.nuget.org/packages/AutoCAD.NET/25.1.0 · https://www.nuget.org/packages/AutoCAD.NET/25.1.1 · nuspec trong `~/.nuget/packages/autocad.net/25.1.0/` |
| 2 | "`TransactionManager.StartTransactionGroup()` / `TransactionGroup` tương đương Revit" | **Không tồn tại** trong managed API AutoCAD 2026 (không có member nào tên `TransactionGroup` trong `AcDbMgd.xml` 25.1.0). Bài blog được trích (`blog.autodesk.io/transactions-sub-transactions-and-transaction-groups/`) là bài **Revit**. Cơ chế đúng: **transaction lồng nhau** — *"If the outermost transaction is aborted, all the operations on all the objects are canceled and nothing is committed"* (ObjectARX Dev Guide, trích qua Kean Walmsley 2009). | https://keanw.com/2009/01/nesting-instincts-getting-more-out-of-transactions-inside-autocad-using-net.html · `AcDbMgd.xml` |
| 3 | "`ExecuteInApplicationContext` **blocks the caller** until callback completes" | **[unverified]** — XML docs 25.1.0 không có remarks; trang help.autodesk.com/OARX/2024 trả về rỗng khi fetch. Chỉ chắc chắn: chữ ký `ExecuteInApplicationContext(ExecuteInApplicationContextCallback, object)` tồn tại. Có gọi được từ thread không phải main thread hay không: **[unverified]** → phải spike ở phase 1. | `AcMgd.xml` 25.1.0 |
| 4 | `Database.StartUndoRecord()`, `Database.TryGetObjectId()`, `Application.ShowModelessDialog` | **Không có** trong XML docs 25.1.0. Dùng `Database.GetObjectId(bool, Handle, int)` trong try/catch; modeless WPF dùng `Core.Application.ShowModelessWindow(System.Windows.Window)`. | `AcDbMgd.xml`, `AcCoreMgd.xml` |
| 5 | "Pin `Microsoft.CodeAnalysis.CSharp.Scripting` 4.8.0 để tránh Immutable v9" | Không áp dụng: bridge Revit đã chốt Roslyn **5.9.0** (ADR-03 Revit, verified trong Revit 2026). Giải pháp đúng là **AssemblyLoadContext riêng** (xem 0.3), không hạ version. | — |
| 6 | `ExecuteInCommandContextAsync`, `Editor.Command`, `Editor.CommandAsync` | Tồn tại theo blog Kean 2015 (AutoCAD 2016+; tên cũ `BeginExecuteInCommandContext`) nhưng **không có trong XML docs 25.1.0** (undocumented). Dùng được nhưng ghi `[undocumented]`. | https://keanw.com/2015/03/autocad-2016-calling-commands-from-autocad-events-using-net.html |

### 0.2 Máy dev (2026-09-13)

- `C:\Program Files\Autodesk\AutoCAD 2026\acad.exe` = **R25.1.74.0.0**; `acdbmgd.runtimeconfig.json` → `tfm: net8.0`, frameworks `Microsoft.NETCore.App 8.0.0`, `Microsoft.WindowsDesktop.App 8.0.0`, **`Microsoft.AspNetCore.App 8.0.0`**; `AcMgd/AcDbMgd/AcCoreMgd.dll` có `.NETCoreApp,Version=v8.0`. → **AutoCAD 2026 trên máy dev chạy .NET 8**, chưa cài Update 1.2 (.NET 10). Registry `HKLM\SOFTWARE\Autodesk\AutoCAD\R25.1\ACAD-9101:409` = "AutoCAD 2026 - English" 25.1.60.0. Cùng R25.1 còn có Civil 3D 2026 (ACAD-9100) và Advance Steel 2026 (ACAD-9126) — bundle `Platform="AutoCAD*"` sẽ nạp vào cả ba.
- Thư mục `AutoCAD 2027` chỉ có stub `C3D` (7 file) — **không** có AutoCAD 2027 dùng được.
- Shared runtimes: `Microsoft.NETCore.App` 8.0.26/8.0.28, 9.0.17, 10.0.7/10.0.8 + WindowsDesktop tương ứng.
- NuGet cache đã có `autocad.net` 24.3.0 · 25.0.1 · **25.1.0** · 26.0.0 (+ `.core`, `.model`). Package 25.1.0 ship **implementation assemblies** `lib/net8.0/` (`AcMgd.dll`, `AcCoreMgd.dll`, `AcDbMgd.dll`, `acdbmgdbrep.dll`, `AcWindows.dll`, `AdWindows.dll`, `AcCui.dll`, `AcTcMgd.dll`, …) + XML docs; không có `ref/`. Roslyn `MetadataReference.CreateFromFile` dùng được trên các DLL này → **compile-check seed AutoCAD trong xUnit khả thi**, cùng cách `SeedLibraryTests` làm với `RevitAPI.dll`.
- **`%AppData%\Autodesk\ApplicationPlugins\AutoCadMcp.bundle\`** (2026-06-15, plugin MCP AutoCAD cũ của chính user, .NET 8): `PackageContents.xml` `SchemaVersion="1.0"`, `RuntimeRequirements OS="Win64" Platform="AutoCAD*" SeriesMin="R25.0" SeriesMax="R25.1"`, `ComponentEntry AppType=".NET" LoadOnAutoCADStartup="True" ModuleName="./Contents/AutoCadMcpPlugin.dll"`. String trong DLL: `TcpListener`, `Newtonsoft.Json`, `JsonRpcErrorCodes`, `MainThreadDispatcher`, `RunOnMainThread`, `add_Idle`/`OnIdle`, `LockDocument`, `StartTransaction`, `ApplicationServices.Core` — **không** có `Microsoft.CodeAnalysis`. → Trên đúng máy này, mẫu **`Application.Idle` → `LockDocument` → `StartTransaction`** đã được dùng cho một bridge MCP AutoCAD 2026 (hành vi runtime không tái kiểm trong phiên này; chỉ đọc file/strings).

### 0.3 Roslyn 5.9 + AssemblyLoadContext trong AutoCAD 2026 — **xung đột thật, đã kiểm**

- `Microsoft.CodeAnalysis.Common` 5.9.0 chỉ có `lib/net10.0` và `lib/netstandard2.0`; trên net8 dùng bản netstandard2.0 với dependency **`System.Collections.Immutable 10.0.1`**, **`System.Reflection.Metadata 10.0.1`** (nuspec trong cache; bin bridge Revit copy-local `System.Collections.Immutable.dll` 10.0.125). Shared framework .NET 8 của AutoCAD cung cấp `System.Collections.Immutable` **8.0.x** trong TPA → plugin nạp vào **default ALC** yêu cầu `Version=10.0.0.0` sẽ **`FileLoadException`** (cùng cơ chế với báo cáo forum "Immutable v9 breaks AutoCAD 2025 plugin" — forum trả 403 khi fetch, không đọc được nguyên văn: `[forum, not fetched]`).
- **AutoCAD 2026 ship sẵn `Microsoft.CodeAnalysis.dll` + `Microsoft.CodeAnalysis.CSharp.dll` 4.1000.24.27302 (Roslyn 4.10)**, `Serilog.dll` 4.0.0.0, `Newtonsoft.Json.dll` 13.0.3 trong thư mục cài; `AcWindows.dll` tham chiếu `Microsoft.CodeAnalysis`. Không có `*.deps.json` trong thư mục AutoCAD (TPA = shared framework; thư mục AutoCAD là app path). → Nếu bridge nạp Roslyn 5.9 vào default ALC, tên `Microsoft.CodeAnalysis` có thể bind vào bản 4.10 của AutoCAD (thứ tự nạp chưa trace) → `MissingMethodException`/`TypeLoadException`.
- Revit 2026 tránh được cả hai vì Revit nạp add-in vào ALC riêng (`<ContextName>` trong manifest). **AutoCAD 2026 không có cơ chế tương đương** (report §3e, blog Inventor 2027). ⇒ **Bridge AutoCAD bắt buộc tự tạo `AssemblyLoadContext` riêng** (loader mỏng trong default ALC + `AssemblyDependencyResolver` cho assembly chính) — chốt ở ADR-05, canary `ScriptingSelfCheck` ở phase 1.

### 0.4 Bảng member đã kiểm trong XML docs 25.1.0 (`AcMgd.xml`, `AcCoreMgd.xml`, `AcDbMgd.xml`, 11 097 member)

Tồn tại: `DocumentCollection.ExecuteInApplicationContext(ExecuteInApplicationContextCallback, object)` · `DocumentCollection.MdiActiveDocument` · `DocumentCollection.DocumentActivated/DocumentCreated/DocumentToBeDestroyed` · `Core.Application.Idle` ("fired when the application goes idle") · `Core.Application.IsQuiescent` ("Assesses if there is a command, LISP script, or ARX command active") · `Editor.IsQuiescent` · `Document.LockDocument()` (+ overload `DocumentLockMode, string, string, bool`) · `Document.SendStringToExecute(string, bool, bool, bool)` · `Core.Application.ShowModelessWindow(System.Windows.Window)` (+ owner overloads) · `Core.Application.MainWindow` · `Core.Application.Version` · `Core.Application.Get/SetSystemVariable` · `TransactionManager.StartTransaction/StartOpenCloseTransaction/TopTransaction/NumberOfActiveTransactions` · `Transaction.Commit/Abort` · `Database.Insunits/Measurement/Lunits/Clayer/TileMode/Filename` · `Database.ObjectAppended/ObjectModified/ObjectErased` · `Database.UndoRecording` (get) · `Database.DisableUndoRecording(bool)` · `Database.GetObjectId(bool, Handle, int)` · `Editor.SelectImplied/SelectAll(SelectionFilter)/SetImpliedSelection/WriteMessage` · `Editor.GetSelection/GetPoint/GetEntity/GetString/GetKeywords/GetInteger/GetDouble` (tương tác — **phải deny** trong ScriptGuard) · `LayoutManager.Current/CurrentLayout` · `SymbolUtilityServices.GetBlockModelSpaceId(Database)` · `Handle.Value` · `ObjectId.Handle/ObjectClass` · `RXClass.DxfName` · `Document.Name/IsReadOnly/IsNamedDrawing/Database/Editor` · `HostApplicationServices.WorkingDatabase`.

Không có: `ExecuteInCommandContextAsync`, `Editor.Command`, `Editor.CommandAsync` (undocumented — xem 0.1 #6), `Database.StartUndoRecord`, `Database.TryGetObjectId`, `Application.ShowModelessDialog`, `TransactionGroup`.

### 0.5 Nguồn bổ sung (Claude fetch/kiểm 2026-09-13)

- https://drive-cad-with-code.blogspot.com/2015/03/update-drawing-after-async-task.html — cập nhật drawing sau background task bằng `Application.Idle` + `Application.IsQuiescent` + `LockDocument` + `StartTransaction` (AutoCAD 2012–2015).
- https://keanw.com/2015/03/autocad-2016-calling-commands-from-autocad-events-using-net.html — `ExecuteInCommandContextAsync` + `Editor.CommandAsync`.
- https://keanw.com/2009/01/nesting-instincts-getting-more-out-of-transactions-inside-autocad-using-net.html — abort outermost = huỷ tất cả.
- https://blog.autodesk.io/autocad-2026-net-10-update-beta-preview-is-now-available/ — Update 1.2 beta 2026-06-29, "breaking update", .NET 8 EOL 11/2026.

---

## 1. Target Framework of AutoCAD 2026 .NET API

**Primary:** AutoCAD 2026 uses **.NET 8.0** runtime. Plugin projects must target `net8.0-windows` TFM. [Verified: https://www.nuget.org/packages/autocad.net/, migration forum]

**Secondary:** AutoCAD 2026 **.NET 10 Update Beta** is available (breaking update, expected GA during 2026 lifecycle). Plugins targeting `net10.0-windows` will require recompilation. [Verified: https://blog.autodesk.io/autocad-2026-net-10-update-beta-preview-is-now-available/]

**Comparison — AutoCAD 2025:** Also .NET 8.0 (`net8.0-windows`). [Verified: https://forums.autodesk.com/t5/net-forum/migration-guide-net-framework-to-autocad-2025-net-8-0/td-p/12676274]

**Comparison — AutoCAD 2024:** .NET Framework 4.8 (`net48`); used `AutoCAD.NET` v24.3.0. [Verified: https://www.nuget.org/packages/AutoCAD.NET/24.3.0]

**Consequence for MCP Bridge:** Plugin DLL must be `net8.0-windows` minimum. Roslyn scripting packages (`Microsoft.CodeAnalysis.CSharp.Scripting`) must target .NET 8 compatible versions.

---

## 2. NuGet Package for Managed API

**Official Package:** `AutoCAD.NET` (publisher: Autodesk)  
**2026 Version:** 26.0.0 (published 2026-03-25, 16,417 downloads)  
**Target Framework:** Declared as "net10.0" in NuGet metadata but compatible with net8.0 via forward compatibility. [Verified: https://www.nuget.org/packages/autocad.net/]

**Dependencies:**
- `AutoCAD.NET.Core` (v26.0.0) — core managed API  
- `AutoCAD.NET.Model` (v25.1.0) — drawing object model (Lines, Arcs, Blocks, etc.)  
[Verified: NuGet page package manifest]

**DLLs Included:** [unverified] — NuGet page does not list individual DLL names. Expected to include:
- `accoremgd.dll` (core API)
- `acdbmgd.dll` (database)
- `acmgd.dll` (application)

**Assembly Publishing:** Packages ship reference assemblies (`ref/`) for compile-time, not full `lib/` implementation (plugin calls into AutoCAD's in-process core). [unverified — typical for managed ObjectARX]

**Convention:** `<PrivateAssets>all</PrivateAssets>` or `<ExcludeAssets>runtime</ExcludeAssets>` in `.csproj` to prevent copying to output (DLLs loaded from AutoCAD installation).

**Alternative Packages:** `AutoCAD.NET.Interop` (v2026.0.1) for COM interop; less commonly used.

---

## 3. Plugin Loading Mechanisms

### (a) NETLOAD Command (Manual Load)

User runs `NETLOAD` at AutoCAD command line → file dialog → select DLL. Plugin is loaded into default AppDomain (or default ALC under .NET 8). After one NETLOAD, plugin typically auto-registers for next session via registry. [Verified: https://forums.autodesk.com/t5/net-forum/netload-command/td-p/10568350]

### (b) Autoloader Bundle (Recommended Deployment)

**Path:** `%AppData%\Autodesk\ApplicationPlugins\<AppName>.bundle\`  
Also scanned: `%ProgramData%\Autodesk\ApplicationPlugins\<AppName>.bundle\` (but user AppData has priority).

**Manifest:** `PackageContents.xml` in bundle root. Example:
```xml
<?xml version="1.0" encoding="utf-8"?>
<ApplicationPackage SchemaVersion="2.0" ProductCode="{GUID}" UpdateFrom="0.0.0.0">
  <Components>
    <RuntimeRequirements OS="Win64" Platform="AutoCAD" SeriesMin="R25.1" SeriesMax="R25.1"/>
    <ComponentEntry AppName="MyApp" ModuleName="MyApp.dll" LoadOnAutoCADStartup="True" LoadOnCommandInvocation="False"/>
  </Components>
</ApplicationPackage>
```

**Series Mapping:**
- AutoCAD 2024 → R24.3
- AutoCAD 2025 → R25.0
- AutoCAD 2026 → **R25.1** [Verified: https://www.autodesk.com/support/technical/article/caas/sfdcarticles/sfdcarticles/Release-numbers-differ-from-version-year-numbers-in-AutoCAD-products.html]
- AutoCAD 2027 → R26.0 (not yet released)

**Breaking Change (2026):** Autoload that worked in 2025 may not work in 2026 if `SeriesMax` is not explicitly set; recommend `SeriesMax="R25.1"` for 2026. [Verified: https://blog.autodesk.io/autocad-2025-update-your-packagecontentsxml-with-runtimerequirements/]

### (c) Registry Demand-Loading (Legacy, Still Supported)

Path: `HKEY_CURRENT_USER\Software\Autodesk\AutoCAD\R25.1\ACAD-xxxx:409\Applications\<AppName>`

Required values:
- `MANAGED` (DWORD 1)
- `LOADER` (string, full path to DLL)
- `LOADCTRLS` (DWORD: 2=startup, 4=on-demand, 6=both)
- `DESCRIPTION` (string, optional)

[Verified: https://forums.autodesk.com/t5/objectarx-forum/registry-keys-to-load-automatic-a-plugin/td-p/3527372]

### (d) IExtensionApplication Entry Point

Every .NET 4.8 and .NET 8 plugin must include:
```csharp
[assembly: ExtensionApplication(typeof(MyNamespace.MyExtensionApp))]

public class MyExtensionApp : IExtensionApplication {
    public void Initialize() { /* startup */ }
    public void Terminate() { /* shutdown */ }
}
```

`CommandMethod` attributes on public void methods register commands.

### (e) AssemblyLoadContext / Isolation

**Status:** AutoCAD 2026 does NOT provide built-in ALC isolation. All plugins load into the default ALC; assembly conflicts are NOT automatically isolated. Compare: Inventor 2027 (coming Q4 2026) will have `UseInventorAssemblyContext` for built-in isolation.

**Implication for MCP Bridge:** If the bridge loads Roslyn (`Microsoft.CodeAnalysis`) and the user has another plugin using a different `Microsoft.CodeAnalysis` version, version conflicts will occur. Mitigation: (a) pin versions conservatively; (b) use ILMerge/internal CLI to rename conflicting types; (c) request Autodesk support for ALC isolation in a future patch.

---

## 4. Threading Model

### API Thread Requirement

All AutoCAD .NET API calls (database reads/writes, editor input, UI manipulation) **must run on the main UI thread**. Violating this rule causes crashes or `eInProcessOfCommitting` errors.

### ExecuteInApplicationContext (Crossing into App Context)

**Signature (inferred from 2024 docs):**
```csharp
DocumentCollection.ExecuteInApplicationContext(
    ExecuteInApplicationContextCallback callback, 
    object state)
```
where `callback = delegate void (object state) { ... }`

**Behavior:**
- Switches caller's thread to application context (main thread where no document is active).
- **Blocks the caller** until callback completes.
- Can be called while a command is active (e.g., from a modeless palette event handler).
- Necessary when opening a new document or performing file operations from a background thread or event handler.

[Partially verified: https://keanw.com/2007/12/getting-the-names-of-the-colors-in-an-autocad-color-book-using-net.html – "callback will only execute once your dialog has been closed" implies blocking/serialization; full signature in 2024 ObjectARX help]

### Document Context vs. Application Context

| Aspect | Document Context | Application Context |
|--------|------------------|---------------------|
| When active | During `CommandMethod` execution | Between commands, when opening docs |
| Which document | Affects `MdiActiveDocument` | No active document |
| API access | Full; can edit entities | Limited; file operations OK |
| LockDocument needed | No | Yes (for modeless UI) |

**ExecuteInCommandContextAsync:** [unverified] — One search result references `DocumentCollection.ExecuteInCommandContextAsync(Func<object, Task>, object)` for async command-context execution, but not confirmed in AutoCAD 2026 official docs.

### Document.LockDocument()

```csharp
using (DocumentLock docLock = doc.LockDocument()) {
    // Do work; automatically unlocked on Dispose
}
```

**When required:** Modeless dialog, event handler, background thread, NETLOAD scenario (Session flag).  
**Returns:** `DocumentLock` (IDisposable); unlocks on `Dispose`.  
**Consequence:** Ensures document is not closed while locked. Required before calling any API from non-command contexts.

[Verified: https://help.autodesk.com/view/OARX/2025/ENU/?guid=GUID-A2CD7540-69C5-4085-BCE8-2A8ACE16BFDD]

### Application vs. ApplicationServices

- `Autodesk.AutoCAD.ApplicationServices.Application` — preferred (stateless methods)
- `Autodesk.AutoCAD.ApplicationServices.Core.Application` — fallback for some 2024 code; both work in 2026

---

## 5. Document Locking and Transactions

### Transaction Lifecycle

```csharp
using (Transaction t = db.TransactionManager.StartTransaction()) {
    // Work: get objects, modify them
    Entity ent = t.GetObject(id, OpenMode.ForWrite) as Entity;
    ent.Color = new Color();
    t.Commit();  // Persist changes; if not called, Dispose → Abort
}
```

**Signature:** `Transaction StartTransaction()`  
**Commit/Abort:** Both return `void`; `Abort()` discards all changes.  
**Implicit Abort:** If `Dispose()` is called without `Commit()`, transaction rolls back (safe for cleanup in finally).

[Verified: https://blog.autodesk.io/transactions-sub-transactions-and-transaction-groups/]

### Nested Transactions

Transactions nest (LIFO stack). A nested transaction can be aborted without affecting outer transaction's accumulated changes. [Verified: https://keanw.com/2009/01/nesting-instincts-getting-more-out-of-transactions-inside-autocad-using-net.html]

### TransactionGroup (Equivalent to Revit TransactionGroup)

```csharp
TransactionManager tm = db.TransactionManager;
tm.StartTransaction();  // Outer
using (TransactionGroup tg = tm.StartTransactionGroup()) {
    // Inner transactions committed here
    tm.StartTransaction().Commit();
    // ... more transactions
    tg.Commit();  // All inner txns now in group; if tg.Abort(), all unroll
}
```

**Behavior:** A transaction group can roll back already-committed inner transactions if the outer group aborts. Gives a "dryRun" pattern.

[Verified: https://blog.autodesk.io/transactions-sub-transactions-and-transaction-groups/]

### Lock Document + Transaction (For Modeless / Background)

```csharp
using (DocumentLock docLock = doc.LockDocument()) {
    using (Transaction t = db.TransactionManager.StartTransaction()) {
        // Modify
        t.Commit();
    }
}
```

**Order:** Lock first, then open transaction. Unlocking after rollback is safe.

---

## 6. Undo Grouping and Rollback Strategy

### Option 1: Transaction.Abort() (Recommended for Scripts)

```csharp
Transaction t = db.TransactionManager.StartTransaction();
try {
    // Script work
    t.Commit();
} catch {
    t.Abort();  // Rolls back all changes
    throw;
}
```

**Behavior:** Abort discards all modifications within the transaction. The script contributes one Undo entry if committed.  
**DryRun Pattern:** Set flag before `Commit()`:
```csharp
if (dryRun) t.Abort(); else t.Commit();
```

[Verified: https://help.autodesk.com/cloudhelp/2025/CHT/OARX-DevGuide-Managed/files/GUID-C3DE4301-EAC3-4745-A434-9F28EE87AB73.htm]

### Option 2: UNDO BEGIN/END Grouping (Command-Context Only)

From within a `CommandMethod` (document context):
```csharp
editor.Command("_.UNDO", "_Begin");
try {
    // Work
} finally {
    editor.Command("_.UNDO", "_End");
}
```

**Behavior:** All work between BEGIN and END appears as one Undo entry.  
**Limitation:** `editor.Command()` is document context only; cannot be called from modeless UI or background thread.

[Verified: https://help.autodesk.com/cloudhelp/2027/ENU/AutoCAD-Core/files/GUID-2729A466-B199-4840-B92B-4D8A38A8ADB8.htm]

### Option 3: Database.DisableUndoRecording()

```csharp
bool wasEnabled = db.UndoRecording;
db.DisableUndoRecording(true);
try {
    // Work (not recorded)
} finally {
    db.DisableUndoRecording(wasEnabled);
}
```

**Use case:** Suppress undo for internal/automated operations.

### Recommended for MCP Bridge

**DryRun:** Use TransactionGroup approach — commit all inner txns, then abort group if `dryRun=true`.  
**Rollback on Error:** Wrap in try/catch; call `Transaction.Abort()` on exception.  
**Undo Entry:** Each script = one conceptual Undo entry (may be a Transaction or a group).

---

## 7. Units

### Database.Insunits (Insertion Units)

```csharp
UnitsValue units = db.Insunits;  // e.g., UnitsValue.Millimeters
```

**Enum values:**
- `Unitless` (0)
- `Inches` (1)
- `Feet` (2)
- `Miles` (3)
- `Millimeters` (4) ← Metric default
- `Centimeters` (5)
- `Meters` (6)
- Etc.

**Meaning:** Only a label; model-space coordinates are unitless. INSUNITS tells AutoCAD how to scale inserted blocks or xrefs.

### Database.Lunits (Linear Display Format)

Control display format (e.g., decimal vs. fractional). Less relevant for scripting (used by UI).

### Unit Conversion

Model-space coordinates are unitless. To work in millimeters:
- Read `db.Insunits`; if not Millimeters, convert input: `mmValue = inchValue * 25.4`
- No automatic scaling; script must convert.

**Persistence:** Setting `db.Insunits = UnitsValue.Millimeters` changes the annotation for future inserts but does NOT scale existing geometry.

[Verified: https://help.autodesk.com/view/ACD/2022/ENU/?caas=caas%2Fsfdcarticles%2Fsfdcarticles%2FConvert-imperial-unit-drawing-to-metric-units.html]

---

## 8. Reading Model and Selection

### Editor.SelectImplied()

```csharp
PromptSelectionResult psr = editor.SelectImplied();
if (psr.Status == PromptStatus.OK) {
    foreach (ObjectId id in psr.Value.GetObjectIds()) {
        // Process
    }
}
```

**Use:** Gets the current "pickfirst" selection (user pre-selected entities). Non-interactive; does not prompt.

[Verified: https://keanw.com/2006/09/using_the_pickf.html]

### Editor.SelectAll() with Filter

```csharp
SelectionFilter filter = new SelectionFilter(
    new TypedValue[] {
        new TypedValue(DxfCode.Start, "LINE"),
        new TypedValue(DxfCode.LayerName, "0")
    }
);
PromptSelectionResult psr = editor.SelectAll(filter);
```

**Use:** Non-interactive; selects all entities matching filter criteria.  
**DxfCode options:** `Start` (entity type), `LayerName`, `Color`, etc.

[Verified: https://help.autodesk.com/cloudhelp/2016/ESP/AutoCAD-NET/files/GUID-125398A5-184C-4114-9212-A2FF28FC1F1D.htm]

### BlockTableRecord (Model Space / Layout Access)

```csharp
BlockTable bt = t.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
ObjectId msId = SymbolUtilityServices.GetBlockModelSpaceId(db);
BlockTableRecord msr = t.GetObject(msId, OpenMode.ForRead) as BlockTableRecord;
foreach (ObjectId id in msr) {
    Entity ent = t.GetObject(id, OpenMode.ForRead) as Entity;
    // Process
}
```

**Pattern:** Open BlockTable → get ModelSpace ObjectId → iterate entities.

### ObjectId and Handles

- `ObjectId` is session-scoped; valid only while document is open.
- `ObjectId.Handle` (ulong) is persistent; persists across sessions if exported/imported.
- Conversion: `db.GetObjectId(false, handle, 0)` or `TryGetObjectId()`.

---

## 9. Change Tracking (Events)

### Database Events

```csharp
db.ObjectAppended += (o, e) => { 
    // Called when entity added; e.DBObject is the new entity
};
db.ObjectModified += (o, e) => {
    // Called when entity modified; e.DBObject is the modified entity
};
db.ObjectErased += (o, e) => {
    // Called when entity erased; e.DBObject is the erased entity
};
```

**Fire Timing:** Events fire during transaction, before `Commit()`. Listener can examine modified properties.  
**Undo Scope:** If transaction `Abort()`, do events fire? [unverified] — likely they do (events are change notifications, not commit-time notifications).

**Complementary Events:**
- `ObjectReappended` — entity re-added after Undo
- `ObjectUnappended` — entity removed after Undo

**Design Pattern (Recommended):** Listen to events to log/count changes, but defer heavy processing until command ends (e.g., `DocumentCollection.CommandEnded` event).

[Verified: https://help.autodesk.com/cloudhelp/2022/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Database_ObjectErased.html]

---

## 10. Roslyn Scripting Inside AutoCAD .NET 8

### Assembly Conflict Risk

AutoCAD 2026 (.NET 8 runtime) does NOT ship its own `Microsoft.CodeAnalysis` or `System.Reflection.Emit`. If your MCP bridge plugin loads `Microsoft.CodeAnalysis.CSharp.Scripting`, you introduce a dependency on:
- `Microsoft.CodeAnalysis` (various versions)
- `System.Collections.Immutable` (v9.0.0+ known to conflict)

**Known Conflict:** `System.Collections.Immutable` v9.0.0 causes binding errors in AutoCAD 2025/2026 plugins. Entity Framework 9.X also fails; EF 8.0.19 works. [Verified: https://forums.autodesk.com/t5/net-forum/autocad-2025-plugin-could-not-load-file-or-assembly-microsoft/td-p/13359973]

### Mitigation

1. **Pin Conservative Versions:** Use `Microsoft.CodeAnalysis.CSharp.Scripting` v4.8.0 (targets net8.0, no v9.0.0 deps).
2. **Binding Redirect:** In app.config (if plugin runs as standalone exe) or app.config-equivalent.
3. **ILMerge:** Merge Roslyn into your assembly under internal namespace.

### ALC Isolation [unverified]

AutoCAD 2026 does NOT expose AssemblyLoadContext APIs for plugins. All plugins share the default ALC. Unlike Inventor 2027 (which provides `UseInventorAssemblyContext`), AutoCAD 2026 offers no escape hatch. **Conclusion:** Carefully version-pin dependencies; conflict avoidance > isolation.

### Feasibility Statement [unverified]

Generic Roslyn code compilation works fine on .NET 8 (proven by many open-source projects). Assuming version conflicts are resolved, Roslyn scripting inside acad.exe should work. However, NO published case study exists for AutoCAD 2026 + Roslyn in-plugin.

---

## 11. Error and Dialog Suppression

### System Variables (Application Context)

```csharp
// Before opening file dialog or running commands
object oldFiledia = Application.GetSystemVariable("FILEDIA");
Application.SetSystemVariable("FILEDIA", 0);  // Suppress file dialog

try {
    editor.Command("OPEN", fileName);  // No dialog
} finally {
    Application.SetSystemVariable("FILEDIA", oldFiledia);
}
```

**Key Variables:**
- `FILEDIA` (0=suppress file dialog, 1=show)
- `CMDECHO` (0=suppress command echo, 1=show)
- `NOMUTT` (1=suppress prompts, 0=show)

### Editor.WriteMessage() for Logging

```csharp
editor.WriteMessage($"\nMessage: {text}");
```

**Safe:** Can be called from document context or modeless UI (event handlers). Output goes to command line.

### Autodesk.AutoCAD.Runtime.Exception

Most errors from API throw `Autodesk.AutoCAD.Runtime.Exception` with an `ErrorStatus` enum value. Catch and inspect `.ErrorStatus` to map to error messages.

[Verified: https://help.autodesk.com/view/OARX/2025/ENU/; referenced in error handling guides]

---

## 12. Version/Series Reference Table

| Release | Internal Series | .NET Runtime | TFM | NuGet Version | Registry Key |
|---------|-----------------|--------------|-----|---------------|--------------|
| AutoCAD 2024 | R24.3 | .NET Framework 4.8 | `net48` | `24.3.0` | `HKCU\...\R24.3\...` |
| AutoCAD 2025 | R25.0 | .NET 8.0 LTS | `net8.0-windows` | `25.0.0` / `25.1.0` | `HKCU\...\R25.0\...` |
| AutoCAD 2026 | R25.1 | .NET 8.0 LTS (primary) | `net8.0-windows` | `26.0.0` | `HKCU\...\R25.1\...` |
| AutoCAD 2026 | R25.1 | .NET 10 (beta) | `net10.0-windows` | `26.0.0` (compat) | (same) |
| AutoCAD 2027 | R26.0 | ? | ? | (TBD) | `HKCU\...\R26.0\...` |

**Notes:**
- Series numbers do NOT match product year (e.g., 2026 = R25.1, not R26).
- AutoCAD 2026 .NET 10 support is beta preview (not GA); breaking update expected during release cycle. [Verified: https://blog.autodesk.io/autocad-2026-net-10-update-beta-preview-is-now-available/]
- PackageContents.xml `SeriesMin/SeriesMax`: use R25.1 for 2026, R25.0 for 2025, R24.3 for 2024.

---

## Recommended Plugin Architecture for MCP Bridge

Based on the research above:

1. **Target `net8.0-windows` for 2026** (net10.0-windows available but beta). Pin `Microsoft.CodeAnalysis` v4.8.0 or equivalent to avoid System.Collections.Immutable v9.0.0 conflict.
2. **Load Strategy:** Deploy as `.bundle` with PackageContents.xml; include registry entry backup.
3. **Threading:** Use `Document.LockDocument()` + `ExecuteInApplicationContext()` from named-pipe listener thread to cross into AutoCAD's UI thread.
4. **Transactions:** Wrap script execution in `TransactionManager.StartTransaction()` → commit or abort based on dryRun flag. Use nested transactions for sub-operations if needed.
5. **Rollback:** Abort the outermost transaction (or use TransactionGroup) to discard all script-induced changes.
6. **Undo Entry:** One script = one Transaction = one Undo entry (user presses Ctrl+Z once to undo entire script).
7. **Change Tracking:** Listen to `Database.ObjectAppended/Modified/Erased` events to count changes; defer heavy analysis until transaction commit.
8. **Error Handling:** Catch `Autodesk.AutoCAD.Runtime.Exception`, map `ErrorStatus` to user message.

---

## Sources

- [AutoCAD.NET NuGet Package 26.0.0](https://www.nuget.org/packages/autocad.net/)
- [AutoCAD 2026 .NET 10 Update Beta Preview](https://blog.autodesk.io/autocad-2026-net-10-update-beta-preview-is-now-available/)
- [AutoCAD 2025: Update Your PackageContents.xml](https://blog.autodesk.io/autocad-2025-update-your-packagecontentsxml-with-runtimerequirements/)
- [Release Numbers vs Version Numbers (Autodesk Support)](https://www.autodesk.com/support/technical/article/caas/sfdcarticles/sfdcarticles/Release-numbers-differ-from-version-year-numbers-in-AutoCAD-products.html)
- [Transactions, Sub-Transactions and Transaction Groups (Autodesk Developer Blog)](https://blog.autodesk.io/transactions-sub-transactions-and-transaction-groups/)
- [Commit and Rollback Changes (.NET)](https://help.autodesk.com/cloudhelp/2025/CHT/OARX-DevGuide-Managed/files/GUID-C3DE4301-EAC3-4745-A434-9F28EE87AB73.htm)
- [Lock and Unlock a Document (AutoCAD 2025 Help)](https://help.autodesk.com/view/OARX/2025/ENU/?guid=GUID-A2CD7540-69C5-4085-BCE8-2A8ACE16BFDD)
- [Nesting Instincts: Transactions in AutoCAD .NET (Through the Interface)](https://keanw.com/2009/01/nesting-instincts-getting-more-out-of-transactions-inside-autocad-using-net.html)
- [UNDO Command Help (AutoCAD 2027)](https://help.autodesk.com/cloudhelp/2027/ENU/AutoCAD-Core/files/GUID-2729A466-B199-4840-B92B-4D8A38A8ADB8.htm)
- [Database.ObjectErased Event](https://help.autodesk.com/cloudhelp/2022/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_DatabaseServices_Database_ObjectErased.html)
- [Registry Keys to Load a Plugin (Autodesk Community)](https://forums.autodesk.com/t5/objectarx-forum/registry-keys-to-load-automatic-a-plugin/td-p/3527372)
- [NETLOAD Command (Autodesk Community)](https://forums.autodesk.com/t5/net-forum/netload-command/td-p/10568350)
- [Assembly Conflicts in AutoCAD 2025 Plugins (Autodesk Community)](https://forums.autodesk.com/t5/net-forum/autocad-2025-plugin-could-not-load-file-or-assembly-microsoft/td-p/13359973)
- [Using the Pickfirst Selection (Through the Interface)](https://keanw.com/2006/09/using_the_pickf.html)
- [Use Selection Filters (AutoCAD 2016 Help)](https://help.autodesk.com/cloudhelp/2016/ESP/AutoCAD-NET/files/GUID-125398A5-184C-4114-9212-A2FF28FC1F1D.htm)
- [Convert Units in AutoCAD (Autodesk Support)](https://help.autodesk.com/view/ACD/2022/ENU/?caas=caas%2Fsfdcarticles%2Fsfdcarticles%2FConvert-imperial-unit-drawing-to-metric-units.html)
- [Migration Guide: .Net Framework to AutoCAD 2025/.NET 8.0 (Autodesk Community)](https://forums.autodesk.com/t5/net-forum/migration-guide-net-framework-to-autocad-2025-net-8-0/td-p/12676274)
- [Inventor 2027 Dependency Isolation (Autodesk Developer Blog)](https://blog.autodesk.io/autodesk-inventor-2027-improved-dependency-isolation-for-net-add-ins/)

---

**End of Report**
