# ADR-04 — STA worker, liveness, busy/modal, cancel/timeout, đơn vị kN_mm_C, guard, globals

**Ngày:** 2026-09-16 · **Revised 2026-09-16 (red-team #1, #6, #7, #11, #14c):** liveness eager; Attach/Detach = control lane; `isQuiescent` true khi `!Attached`; worker foreground drain; snapshot trong budget + `TimeoutSemanticsHint`; guard gọn + cấm namespace bridge/Host; không public static executor · **Status:** Proposed · **Owner:** HPEtabs
**Kế thừa:** [Navis ADR-04](../../260915-0824-navisworks-mcp-2026/adr/adr-04-navis-main-thread-busy-heavy-ops-guard-globals.md) · `HPAutoCad/HPAutoCad.McpBridge/MainThreadExecutor.cs:54` · `HPAutoCad/HPAutoCad.McpBridge/BridgeEntry.cs:41` (`BusyGrace = 8 s`)
**Bằng chứng:** [evidence §E2, §E3, §E8](../research/evidence-on-machine-2026-09-16.md) · [researcher-01 #10–#12, #20–#23, §8, §10](../research/researcher-01-etabs-oapi-facts.md) (CHM im lặng về thread/modal — #22) · [red-team](../reports/red-team-2026-09-16.md) hàng 1, 6, 7, 11, 14 · `McpShared/HPRebar.McpBridge.Core/Host/MainThreadQueue.cs:72` (ctor), `:162` (`Work` chạy inline trên thread tick), `:133–141` (tick không quiescent → chỉ `FailExpired`) · `McpShared/HPRebar.McpBridge.Core/Scripting/ScriptUnits.cs:14–16` · `McpShared/HPRebar.McpBridge.Core/Scripting/ScriptGuard.cs:23,36` (base list đã có `System.Runtime.InteropServices`, `Marshal`) · `ScriptCompiler.cs:39` (`globalsType.Assembly` là reference → mọi type public của bridge exe nameable) · `McpBridgeHost.cs:53` (`Current` public static) · `McpShared/HPRebar.Mcp.Contracts/JsonRpc/SafeText.cs` · `McpShared/HPRebar.Mcp.Server.Core/Services/RevitBridgeClient.cs:85,139` (text timeout/not-connected) · `RequestDispatcher.cs:132–134`

## Context
- Proxy COM out-of-proc là apartment-bound; CHM không nói gì về thread (#22). Ta sở hữu tiến trình → một thread STA chuyên dụng giữ attachment và chạy mọi call OAPI.
- `isQuiescent` dựa vào `IsWindowEnabled(hwnd)` với hwnd chết (ETABS đóng) trả false vĩnh viễn; Attach/Detach xếp cùng `MainThreadQueue` thì không bao giờ tới lượt → bridge kẹt `-32002` (red-team #1, `MainThreadQueue.cs:133–141`).
- `RunAnalysis`/`Save`/`OpenFile` đồng bộ, không cancel (CHM › "cAnalyze.RunAnalysis Method"); `ct` cooperative.
- Text engine "nothing has been committed" (`RevitBridgeClient.cs:85`), "Open {host} {version} and enable the HP MCP Bridge" (`:139`), "window inside {host}" (`RequestDispatcher.cs:133`) sai với ETABS (red-team #7).
- `SetPresentUnits` "affects API data only" (CHM › "cSapModel.SetPresentUnits Method"); `eUnits.kN_mm_C` (E2). `ScriptUnits(label, mmPerUnit, note)` (`ScriptUnits.cs:16`).

## Decision

### 1. Một STA worker **foreground**; hai lane: control (attach/detach) và work (`MainThreadQueue`)
- `Thread` STA (`SetApartmentState`), **`IsBackground=false`**: khi app thoát, worker **drain call hiện tại** (đợi call OAPI trả về, tối đa ceiling) rồi Detach, để proxy không bị bỏ giữa chừng (red-team #14c). Cửa sổ đóng khi `IsBusy` → hộp xác nhận "a script is still running in ETABS — close anyway?".
- **Control lane:** `Attach()`/`Detach()` là `TaskCompletionSource` riêng do vòng lặp worker ưu tiên **trước** `_queue.OnTick()` — **không** qua `MainThreadQueue` (red-team #1). Attach: `Helper.GetObject(...)` (null → "not attached"), lưu `AttachedPid` (từ `GetProcessesByName("ETABS")` — 1 process; > 1 → warning ADR-02 §5), `hwnd = Process.MainWindowHandle`, đăng ký `Process.EnableRaisingEvents=true; Exited += …`.
- **Liveness eager:** `Process.Exited` hoặc `HasExited` kiểm mỗi tick → `Attached=false`, hwnd cleared, proxy null, `StateChanged`; mọi work item đang chờ → `FailAll(BridgeRequestException NoActiveDocument "not attached")`. `COMException` server-unavailable trong call → cùng đường.
- **Work lane:** `MainThreadQueue(isQuiescent: () => !Attached || (!_running && IsWindowEnabled(hwnd)), "ETABS", 8 s, wake: _wake.Set, expireWithoutTicks: true)` (`MainThreadQueue.cs:72`) — `!Attached` ⇒ quiescent ⇒ work chạy ngay và **fail nhanh `-32003`** thay vì kẹt `-32002`. Vòng lặp: `while (!_stop) { _wake.WaitOne(250 ms); DrainControlLane(); CheckLiveness(); _queue.OnTick(); }`; `OnTick` chạy `Work` inline (`:162`) ⇒ mọi call OAPI trên STA. Giả định STA không cần message pump cho call outbound — E14.

### 2. Busy, modal, no-model, not-attached
| Tình huống | Phát hiện | Kết quả |
|---|---|---|
| Script đang chạy | `_running` | `-32002` ngay (`RequestDispatcher.cs:136–137`); `context` trong lúc chạy → `-32002` < 1.5 s |
| ETABS modal | `!IsWindowEnabled(hwnd)` khi `Attached` | chờ ≤ 8 s → `-32002` (`BridgeRequestException.Busy("ETABS")`) |
| Call kẹt (modal **trong** call / `RunAnalysis` dài) | không tick | `expireWithoutTicks` refuse hàng đợi sau 8 s → `-32002`; E15 quan sát block/reject |
| Không model (start screen) | probe E12 (`GetModelFilename()`/`GetModelFilepath()`/`PointObj.GetNameList`) | `-32003` `NoActiveDocument("ETABS", "model (.EDB)")` |
| Chưa attach / ETABS đóng | `!Attached` (liveness) | `-32003` "ETABS not attached — click Attach in the HPEtabs MCP Bridge window"; Attach lại không restart |

### 3. Cancel / timeout / budget — nói thật qua profile hint
- `ct` kiểm **giữa** các call OAPI; không ngắt được một call. Timeout → run fail; worker bận tới khi call trả → request sau `-32002`. `cancel_execution` chỉ set token.
- **Snapshot trong budget** (red-team #6): đồng hồ chạy trước `Save()`; hết budget trước script → fail, script không chạy (ADR-02 §3). Server chờ `timeout + ExtraTimeoutSeconds` (`ExecuteCodeService.cs:77–82`) nên bridge không bao giờ trả muộn vì snapshot.
- **`HostProfile.TimeoutSemanticsHint`** (phase 0, additive, null → text cũ) thay câu "nothing has been committed" ở `RevitBridgeClient.cs:85`: "ETABS may still be running the call; changes made before the timeout persisted (no rollback) — check the snapshot named in the bridge window before retrying". **`BridgeNotConnectedHint`** thay `:139`: "Start HPEtabs.McpBridge.exe beside ETABS 22, click Attach and tick 'Allow AI code execution' (pipe hpetabs-mcp-22)". `RequestDispatcher`/`McpBridgeHost` ctor `executionDisabledMessage = null` thay `:132–134` ("…in the HPEtabs MCP Bridge window (a separate app, not inside ETABS)").
- Ceiling 5–120 s; ≤ 600 s khi destructive ON (`EtabsHeavyMaxTimeoutSeconds`; profile `MaxTimeoutSeconds = 600`; 3 site engine theo profile `ExecuteCodeService.cs:61`, `ToolManager.cs:173`, `ToolValidator.cs:53`). `run_analysis` mô tả: chọn `timeoutSeconds` theo thời gian lần chạy GUI gần nhất; timeout ≠ abort.

### 4. Đơn vị — ép `kN_mm_C` mỗi run, restore `finally`
`saved = GetPresentUnits()` (overload trả `eUnits` — E13); `SetPresentUnits(eUnits.kN_mm_C)` (`ret≠0` → fail trước script); `finally SetPresentUnits(saved)` (lỗi → `Logs` warning). `units = new ScriptUnits("kN_mm_C", 1.0, "present units forced to kN_mm_C for this run; moments kN·mm, stresses kN/mm²")` (`ScriptUnits.cs:16`, identity). Seed ghi trường dẫn xuất có nhãn (`momentKNm`, `fcMPa`). Context: `PresentUnits` (của user), `DatabaseUnits` (`GetDatabaseUnits()` `[chưa xác minh]`).

### 5. Guard — `GuardProfile.Etabs` (Core, data thuần; precedent `GuardProfile.cs:54–81`)
Base list giữ nguyên (đã có `System.IO/Net/Reflection/Process`, **`System.Runtime.InteropServices`**, **`Marshal`**, `System.Linq.Expressions`, `Expression`/`Delegate`/`Compile`, `await/Task/Thread/dynamic/unsafe` — `ScriptGuard.cs:20–40`). ETABS thêm **đúng** (red-team #11):
- identifiers: `Helper`, `MessageBox` (cấm `Helper` ⇒ 17 member `cHelper` không thể tới; không liệt kê lại)
- members (7, `cOAPI`): `ApplicationExit`, `ApplicationStart`, `Hide`, `Unhide`, `SetAsActiveObject`, `UnsetAsActiveObject`, `InternalExec`
- namespaces: `System.Windows.Forms`, **`HPEtabs.McpBridge`** (exe của ta là reference của script — `ScriptCompiler.cs:39`), **`HPRebar.McpBridge.Core.Host`** (`McpBridgeHost.Current`/`Stop()` public static — `McpBridgeHost.cs:53`)
- `AnalyzerProfile.Etabs = ([], [])` — precedent **`AnalyzerProfile.Navis` static tồn tại** (`AnalyzerProfile.cs:23`) → mirror.
- Bridge (phase 2): **không** public static executor/analyzer/snapshot manager; cờ destructive chỉ set qua VM trên UI thread (`Dispatcher.CheckAccess()` assert); guard test: script chứa `HPEtabs.McpBridge.BridgeEntry.Stop()` và `HPRebar.McpBridge.Core.Host.McpBridgeHost.Current` → `GUARD`.
- **Va chạm base list:** `File`, `Directory`, `GetProperty`… là identifier/member bị cấm mọi host; `cAreaObj.GetProperty` (đọc hợp lệ) **bị chặn** → R3 đọc section area qua `DatabaseTables.GetTableForDisplayArray` hoặc bỏ trường; mô tả tool nêu; test `EtabsGuardCollisionTests` = tên member ETABSv1 (fixture) ∩ base deny list → danh sách ghi report; carve-out receiver-scoped = additive tương lai, không MVP. `sapModel.File` (property) — identifier `File` bị cấm base → script dùng `sapModel.File.Save()`? **Member access** `.File` không phải identifier bare → kiểm trong guard test (E20); nếu bị chặn, seed D1/`Save` đi qua bridge (không script) — ghi kết quả spike.

### 6. Globals & imports (`HostScriptContracts`, phase 0)
`EtabsGlobals = { "sapModel", "etabs", "units", "ct", "log", "progress", "args" }`; `EtabsImports = { "System", "System.Linq", "System.Collections.Generic", "ETABSv1", "HPRebar.McpBridge.Core.Scripting" }`. `EtabsScriptGlobals` mirror `HPNavis/HPNavis.McpBridge/Model/NavisScriptGlobals.cs:12–37`. Return-code: seed kiểm mọi `ret` → `InvalidOperationException($"ETABS returned {ret} from X")`; input caller → `ArgumentException`. `SafeText.StripPaths` (Contracts, `JsonRpc/SafeText.cs`) cho mọi message.

## Alternatives rejected
- MTA/thread-pool gọi OAPI; `BlockingCollection` riêng; `IMessageFilter` trước khi E15 chứng minh; đơn vị theo user + chuyển đổi; `helper` global — như bản trước.
- Attach qua `MainThreadQueue` (bản sáng): kẹt khi hwnd chết (red-team #1) — loại.
- Worker background thread: bỏ call giữa chừng khi thoát — loại.

## Consequences
- `EtabsInfo` 10 field (phase 0); `IsModifiable = Attached && !busy && windowEnabled`; `DocPath/DocTitle` = model path/name; `HostVersion` = "22".
- Harness: timeout 5 s trên vòng lặp cooperative; "ETABS closed → `-32003` ngay, Attach lại OK không restart"; modal → manual.

## Open items `[chưa xác minh]` → spike
E14 STA outbound không pump; E15 modal (block/reject/mã); E12 no-model probe; E13 overload units; mã `COMException` khi ETABS đóng; `IsWindowEnabled` có phản ánh modal WPF của ETABS .NET 8; member access `.File` qua guard.
