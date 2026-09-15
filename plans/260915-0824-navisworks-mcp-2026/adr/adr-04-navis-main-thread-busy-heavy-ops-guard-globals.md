# ADR-04 — Main thread, trạng thái bận, thao tác nặng, guard và globals của script Navisworks

**Ngày:** 2026-09-15 · **Revised 2026-09-15 (red-team):** heavy gate hiện thực **host-side** (cờ in-memory + pre-pass syntax), không đụng `BridgeSettings`/`AuditEntry`/`ToolRecord`; timeout heavy đi qua **3** site engine (additive, default 120); quiescence dùng **depth counter** có reset; `GetContextAsync` trả busy ngay; resolver allow-list; mục §6 mô hình tin cậy · **Status:** Proposed · **Owner:** HPNavis
**Kế thừa:** [AutoCAD ADR-02 main-thread marshalling](../../260913-0000-autocad-mcp-bridge-2026/adr/adr-02-autocad-main-thread-marshalling.md) · [Revit ADR-04 security](../../260912-1521-dynamic-revit-mcp-server-2026/adr/adr-04-execute-code-security-model.md)
**Bằng chứng:** [evidence §E5, E8, E14](../research/evidence-on-machine-2026-09-15.md); `McpShared/HPRebar.McpBridge.Core/Host/MainThreadQueue.cs`; `HPAutoCad/HPAutoCad.McpBridge/MainThreadExecutor.cs:186–193` (`WakeMainThread`/`PostMessage`)

## Context

- Navisworks .NET API chỉ hợp lệ trên main thread (researcher-01 §3); không có `ExternalEvent`/`IsQuiescent`. Có `Application.Idle`, `Application.Progress*` events (kể cả `ProgressSubOperationBegan/Ended` lồng nhau — E5), `Document.IsActiveTransaction`, `Application.Gui.MainWindow : IWin32Window` (`Autodesk.Navisworks.Api.ApplicationParts.IApplicationGui`).
- `MainThreadQueue.OnTick` chạy work **đồng bộ trong Idle handler** và tin tuyệt đối vào `isQuiescent()` (`MainThreadQueue.cs:104–129`). `RequestDispatcher` chỉ chặn `execute` thứ hai khi `IsBusy` (`RequestDispatcher.cs:136`); `context` vào queue không kiểm bận (mẫu AutoCAD `MainThreadExecutor.cs:141–148`); server timeout `context` 15 s (`ContextService.cs:20`) và **mọi** timeout đều gửi `cancel` (`RevitBridgeClient.cs:81–99`); bridge `Cancel()` không có id (`RequestDispatcher.cs:94–95`) → cancel nhầm run đang chạy.
- Timeout 120 s hard-code ở **3** chỗ engine: `ExecuteCodeService.cs:24,59`, `ToolManager.cs:173` (`run_tool`/`test_tool`), `ToolValidator.cs:52` (propose); mô tả `ToolLifecycleTools.cs:34` "5–120 seconds".
- `ScriptGuard` là deny-list cú pháp, **không phải sandbox** (`ScriptGuard.cs:8–13`); thông điệp cho member profile là **một chuỗi cố định** (`ScriptGuard.cs:104`) → không sinh được thông điệp "heavy" riêng từ `GuardProfile`.
- `BridgeSettingsStore.Save` serialize **toàn bộ** `BridgeSettings` (`Options` không có ignore-null, `BridgeSettingsStore.cs:15,75`) và `Load` chỉ ép `ExecutionEnabled=false` (`:36–43`) → **không** thêm field vào `BridgeSettings` (sẽ bị persist và có thể bị tiền-kích-hoạt bằng tay).

## Decision

### 1. Marshal: `Application.Idle` + `MainThreadQueue` + `PostMessage(WM_NULL)` — giống AutoCAD

- `NavisMainThreadExecutor : IBridgeExecutor` tạo trong `EventWatcherPlugin.OnLoaded` (main thread); `Application.Idle += (_, _) => _queue.OnTick()`; gỡ trong `OnUnloading`.
- Wake: `PostMessage(mainHandle, WM_NULL, 0, 0)`; `mainHandle` đọc trên main thread tại `Application.GuiCreated` (`Gui` có thể null ở `OnLoaded`).
- Guard + pre-pass + compile trên pipe thread; chỉ **run** vào queue.
- **`GetContextAsync` trả `BridgeRequestException.Busy` ngay** khi `IsBusy` (host-local, không đổi Core) → server không bao giờ chờ 15 s rồi gửi `cancel` nhầm vào run heavy đang chạy. `Inspect`/`Analyze` vốn không cần main thread.

### 2. Quiescence composite — depth counter, có reset, có staleness

`isQuiescent = () => _progressDepth == 0 && !ModalOpen() && !(doc?.IsActiveTransaction ?? false)`:
- `_progressDepth`: `ProgressBeginning`/`ProgressSubOperationBegan` → `++`; `ProgressEnded`/`ProgressSubOperationEnded` → `--` (không âm). **Reset về 0** khi work item của bridge bắt đầu và kết thúc (append do script gọi tự sinh Progress). **Staleness:** nếu `_progressDepth > 0` quá `ProgressStaleSeconds` (mặc định 120) mà không có Progress event mới **và** `IsWindowEnabled(mainHandle)` → reset + log Warning ("progress depth reset after stale N s"). Mọi chuyển trạng thái log Debug; `NavisInfo.IsBusy` phơi composite để harness assert về idle sau S-08.
- `ModalOpen()`: **`!IsWindowEnabled(mainHandle)` — chỉ vậy** (dialog modal native/WPF/WinForms đều disable owner). *Revised 2026-09-15 (S-07):* `GW_ENABLEDPOPUP` bị bỏ vì nó báo cả **cửa sổ trạng thái modeless của chính bridge** (owned by main) → mọi request `-32002` khi cửa sổ mở.
- **`Idle` KHÔNG bắn khi modal native mở (S-07 verified):** request treo tới khi user đóng dialog. Fix: `MainThreadQueue(…, expireWithoutTicks: true)` — engine **additive, opt-in**, default `false` (Revit/AutoCAD giữ tick-only): `Task.Delay(grace+50 ms)` → `FailExpired()` chỉ đụng item còn trong queue → `-32002` sau 8 s (đo 8 s/request, 16.3 s cho context+execute). Test: `MainThreadQueueTests` +2 (net10 + net48).
- **Progress depth verified (S-08):** load file → depth 1→3→0 trong ~1 s; clash 852 kết quả 61 ms; sau run `IsBusy=false`. Request thứ hai gửi khi một run đang chạy → `-32002` ngay (một run một lúc).
- `RequireDocument()`: `ActiveDocument` null hoặc `IsClear` → `-32003`.
- `BusyGrace = 8 s`; mã lỗi `-32002/-32003/-32001` không đổi.

### 3. Thao tác nặng (W2) — cờ riêng (host-side), timeout riêng (engine additive), audit riêng (Message)

- **Cờ:** `volatile bool HeavyOperationsEnabled` **trên `NavisMainThreadExecutor`** (không ở `BridgeSettings`, không ở `IMcpBridgeRunner`, không persist, OFF mỗi lần Navisworks khởi động; chỉ bật được khi `ExecutionEnabled`). Cửa sổ Navis bind qua `NavisBridgeStatusViewModel` (host-side) **bao** `McpBridgeStatusViewModel` của Core — Core VM không đổi.
- **Pre-pass `NavisHeavyGate.Check(code, heavyEnabled)`** (host-side, Roslyn syntax walker riêng, chạy trước `ScriptGuard.Check`):
  - heavy OFF → mọi member access tên trong **W2** (`AppendFile(s)`, `MergeFile(s)`, `RemoveFile`, `OpenFile`, `Clear` *(trên `doc`)*, `UpdateFiles`, `SaveFile`, `ExportToNwd`, `PublishFile`, `ExportAsDwf`, `GenerateImage`, `TestsRunTest`, `TestsRunAllTests`, `TestsCompactAllTests` + `Try*`) → diagnostic id `HEAVY`: *"`AppendFile` is a heavy operation (not undoable, may take minutes). Ask the user to tick 'Allow heavy operations' in the HPNavis bridge window, then retry."*
  - heavy ON → cho phép W2, nhưng **path policy** trên string literal & interpolated string: literal bắt đầu `\\` (UNC) hoặc chứa `HPNavis\McpBridge`/`HPNavis\McpServer`/`\Autodesk\Navisworks Manage` → `HEAVY`: "network paths and the bridge's own folders are refused". Đường dẫn động (biến) không kiểm được — ghi trong mô tả tool + §6.
  - Kết quả pre-pass gộp vào `ExecuteResult.Diagnostics` cùng guard; `Analyze` (pipe `navis.analyze`) chạy pre-pass với **heavy OFF** → `propose_tool` của tool chứa W2 luôn bị `ToolValidator` từ chối (`ToolValidator.cs:66`) → **heavy tool = seed-only trong MVP** (ghi rõ; đường propose heavy là bước sau, cần `AnalyzeRequest` mở rộng).
  - Request có W2 (heavy ON) được đánh dấu `HasHeavyCalls` → ADR-02 §1 (không `RolledBack=true`, từ chối dryRun).
- **Timeout riêng — engine additive, default 120:** `IHostProfile.MaxTimeoutSeconds` + `HostProfile.MaxTimeoutSeconds { get; init; } = 120`; dùng ở **3** site: `ExecuteCodeService.cs:59`, `ToolManager.cs:173` (`_bridge.Profile` đã có), `ToolValidator.cs:52` (validator đã nhận profile từ phase 4 AutoCAD). `NavisHostProfile.MaxTimeoutSeconds = 600`. Test Server.Core: Revit/AutoCAD vẫn clamp 120; navis 600. Mô tả `ToolLifecycleTools.cs:34` "5–120 seconds" **giữ nguyên** (đổi = đổi byte mô tả tool Revit/AutoCAD) → known gap: text nói 120, validator Navis cho 600.
  Bridge clamp: heavy OFF → 5–120; heavy ON → 5–600. Timeout vẫn cooperative; `TestsRunTest`/`AppendFile` không nhận `ct` → chỉ có tác dụng sau khi call trả về (mô tả tool: "cannot be interrupted; keep selections small; save before running").
- **Audit riêng:** không đổi `AuditEntry`. Run heavy ghi **hai** dòng: `outcome:"started"` **trước** khi enqueue (nếu Roamer bị kill giữa clash vẫn còn vết) và dòng kết quả với `Message` prefix `[heavy] ` + tên file/số test. Revit/AutoCAD không đổi.
- **Không có** trên Ribbon/Add-ins menu; không có field `allowHeavy` trong `ExecuteRequest`.
- **Seed heavy duy nhất:** `create_and_run_clash_test` với `tags:["heavy"]` (`ToolRecord.Tags` có sẵn, `ToolRecord.cs:34`, sống qua rewrite). `test_tool` (dryRun mặc định) trên seed heavy → bridge từ chối trước khi chạy với thông điệp rõ; harness kiểm heavy bằng `run_tool` khi heavy ON.

### 4. Globals của script

`NavisScriptGlobals` (net48, `HPNavis.McpBridge/Model/`), `HostScriptContracts.NavisGlobals = { doc, app, units, ct, log, progress, args }`:

| Global | Kiểu | Ghi chú |
|---|---|---|
| `doc` | `Autodesk.Navisworks.Api.Document` | `Application.ActiveDocument` |
| `app` | wrapper `NavisApp` { `Version`, `Documents`, `MainDocument`, `IsAutomated`, `HasClashModule`, `IsModified` } — `Application` là static class | không lộ `Gui` |
| `units` | `HPRebar.McpBridge.Core.Scripting.ScriptUnits` | ADR-02 §4 |
| `ct` | `CancellationToken` | cooperative |
| `log(string)` | `Action<string>` | `MaxLogLines` |
| `progress(cur,total,msg)` | `Action<int,int?,string?>` | → `navis.progress` |
| `args` | `ScriptArgs` | như Revit/AutoCAD |

`state` (ComApi) **không** có trong MVP (ADR-02 W3). Imports `NavisImports = { System, System.Linq, System.Collections.Generic, Autodesk.Navisworks.Api, Autodesk.Navisworks.Api.DocumentParts, Autodesk.Navisworks.Api.Clash, Autodesk.Navisworks.Api.Timeliner, HPRebar.McpBridge.Core.Scripting }`. **Không** import `Autodesk.Navisworks.Api.ApplicationParts` (chỉ bridge dùng `IApplicationGui`), `Plugins`, `Interop`, `Automation`, `ComApi`, `Data`. Compiler references: `Autodesk.Navisworks.Api/Clash/Timeliner` + `mscorlib`, `System`, `System.Core`, `netstandard` (**không** `System.Data`), `HPRebar.McpBridge.Core` (net48), `System.Text.Json` — chốt ở phase 1 bằng `ScriptingSelfCheck`.

### 5. Điều gì có thể làm treo Navisworks (và cách chặn)

| Nguy cơ | Chặn |
|---|---|
| Dialog (`MessageBox`, `OpenFileDialog`) | guard: `System.Windows.Forms`, `MessageBox`, `Microsoft.Win32` |
| Automation spawn Roamer thứ hai | guard: `Autodesk.Navisworks.Api.Automation`, `NavisworksApplication` |
| Reflection-by-expression (`Expression.Call(...).Compile()`, `Delegate.CreateDelegate`) vượt tên member | guard Navis: `System.Linq.Expressions`, `Expression`, `Delegate`, `CreateDelegate`, `Compile` (**base list chưa có** — xem §6) |
| SQL vào SQLite nhúng (`doc.Database` → `NavisworksCommand`) — ghi/đọc file tuỳ ý qua `ATTACH` | guard Navis: `System.Data`, `Autodesk.Navisworks.Api.Data`, `Database`, `ToNavisworksConnection`, `NavisworksCommand/Connection/DataAdapter`; không reference `System.Data` |
| Duyệt `Descendants` hàng triệu item | `ct` cooperative; mô tả tool khuyến nghị `Search`; seed giới hạn `maxResults` |
| `TestsRunTest` không nhận `ct` | heavy gate + 600 s + mô tả; audit `started` |
| Script ném giữa transaction | `Commit()` → `RollbackOwn()` (ADR-02 §1) |
| Script mở transaction riêng và quên đóng | guard cấm `BeginTransaction`/`Transaction` |

### 6. Mô hình tin cậy — nói thẳng

- Hai checkbox (execution, heavy) là **hàng rào chống tai nạn**, không phải chống một agent hợp tác trên cùng desktop: UI Automation (như `harness-common.ps1:47–58` của AutoCAD) tick được cả hai; `ScriptGuard` + pre-pass là deny-list cú pháp (`ScriptGuard.cs:8–13`). Cùng mô hình với Revit/AutoCAD hiện tại; không hứa hơn.
- `RequireLocalApproval` trong `BridgeSettings` (`BridgeSettings.cs:16`) hiện **không được đọc** ở host nào — pre-existing; không đưa vào MVP Navis (modal trên main thread trái ADR-02 AutoCAD).
- **Cần user quyết (không tự làm):** có đưa `System.Linq.Expressions`/`Expression`/`Delegate`/`CreateDelegate`/`Compile` vào **deny-list gốc** (`ScriptGuard.cs:20–46`) cho cả Revit/AutoCAD không — tăng an toàn nhưng đổi hành vi guard hai host đã verified (không đổi tool surface). Plan này chỉ đặt vào `GuardProfile.Navis`.

## Alternatives rejected

- **Field `HeavyOperationsEnabled` trong `BridgeSettings`:** bị persist + có thể pre-authorise bằng file (`BridgeSettingsStore.cs:15,36–43,75`). Loại.
- **`AuditEntry.Tags`:** thêm field vào record dùng chung khi `Message` prefix đủ và giữ audit hai host byte-identical. Loại.
- **`ToolRecord.Heavy` / `AnalyzeRequest.Heavy`:** cần cho đường propose heavy; MVP heavy = seed-only → hoãn; `Tags` đủ để đánh dấu.
- **Cancel theo id:** cần đổi `IBridgeExecutor.Cancel()` (interface dùng chung, không additive). Không cần khi `context` trả busy ngay và dispatcher đã chặn `execute` thứ hai.
- **Chunk clash run qua nhiều tick:** H1 chỉ chạy một `TestsRunTest`; không có gì để chia. Loại.
- **Từ chối heavy khi `doc.IsModified`:** tài liệu review hầu như luôn modified → chặn mọi phiên thật. Thay bằng `app.IsModified` + `NavisInfo.IsModified` để AI cảnh báo user save trước.
- **`Control.Invoke`**, **`SetRaiseIdle`**, thread nền, `allowHeavy` field: như bản trước — loại.

## Consequences

- Core additive: `GuardProfile.Navis`, `AnalyzerProfile.Navis`, `IHostProfile/HostProfile.MaxTimeoutSeconds` (+ 3 site clamp), hằng Contracts. **Không đổi:** `MainThreadQueue`, `RequestDispatcher`, `AuditEntry`, `BridgeSettings`, `IMcpBridgeRunner`, `McpBridgeStatusViewModel`, `ToolRecord`, `AnalyzeRequest`, `IBridgeExecutor`.
- Host-side mới: `NavisHeavyGate` (pre-pass + path policy + clamp), `NavisBridgeStatusViewModel` (bao Core VM + cờ heavy), quiescence counter, audit `started`.
- ~~Spike S-03/S-07/S-08 xác minh~~ **Đã xác minh 2026-09-15** (`reports/phase-01-spike.md`): S-03 ping 0.01 s/context 0.08 s không chạm chuột (wake `WM_NULL` đủ); S-07 → `-32002` sau 8 s (cần `expireWithoutTicks`); S-08 (script-driven) depth về 0, clash chạy từ GUI chưa tự động hoá; S-11 7 guard + 3 heavy case đều `GUARD`/`HEAVY`, không chạy.
