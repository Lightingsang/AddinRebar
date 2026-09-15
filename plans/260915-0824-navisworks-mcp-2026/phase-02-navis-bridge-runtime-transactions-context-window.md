---
phase: 2
title: "Navis bridge runtime: runner, transactions/dryRun, context, serializer, heavy gate, window; bridge tests net48"
status: pending
priority: P1
effort: "12h"
dependencies: [0, 1]
---

# Phase 2: Bridge runtime hoàn chỉnh trong Roamer.exe

## Context Links
- [ADR-02](adr/adr-02-navis-transaction-dryrun-and-writable-surface.md) (`RollbackOwn`, fingerprint, W1/W2/W3) · [ADR-04](adr/adr-04-navis-main-thread-busy-heavy-ops-guard-globals.md) (quiescence counter, heavy gate host-side, pre-pass, audit `started`, trust model) · [ADR-03 §3](adr/adr-03-navisworks-api-reference-and-test-without-navisworks.md) (`HPNavis.McpBridge.Tests` net48) · [ADR-05 §1](adr/adr-05-navis-plugin-packaging-deploy-identity.md) (không Ribbon)
- Spike results: `reports/phase-01-spike.md` (S-05b/c/d, S-07, S-08, S-10 quyết định chi tiết)
- Mẫu: `HPAutoCad/HPAutoCad.McpBridge/Service/AutocadScriptRunner.cs`, `AutocadResultSerializer.cs`, `AutocadContextReader.cs`, `View/AutocadBridgeStatusView.xaml(.cs)`, `Resources/Themes/*.xaml`; Core `McpBridgeStatusViewModel`

## Overview
Thay executor spike bằng runtime thật: ma trận transaction × dryRun theo ADR-02 (mọi `Rollback()` có điều kiện), fingerprint/`Changed`, serializer Navisworks an toàn, context đầy đủ (`NavisInfo`), heavy gate host-side (cờ in-memory + pre-pass + path policy + clamp 600 + audit `started`), cửa sổ WPF modeless với **2 checkbox**. **Không** Ribbon, **không** theme switcher (theme sáng cố định, token `DynamicResource` sẵn). Thêm `HPNavis.McpBridge.Tests` (net48) cho lớp thuần. Kết thúc: harness pipe ≥ 22 scenario pass trong Navisworks thật.

## Requirements
- Functional: `navis.execute` đủ ma trận `auto|manual(≡auto)|none × dryRun` với `RollbackOwn`; exception/timeout/cancel → `Commit()`→`RollbackOwn()`; `none` fail khi fingerprint đổi; heavy OFF → pre-pass `HEAVY` diagnostic; heavy ON → W2 chạy, path policy, clamp ≤ 600, audit 2 dòng (`started` + `[heavy]`), `HasHeavyCalls` → không `RolledBack=true`, dryRun từ chối; `GetContextAsync` busy ngay khi `IsBusy`; `NavisInfo` đủ field; `inspect_type` phản chiếu Api/Clash/Timeliner; cửa sổ + Add-ins menu.
- Non-functional: mọi lỗi ra `ExecuteResult`; `SafeText.StripPaths`; C# file < 300 dòng; XAML `DynamicResource`, không màu hard-code ngoài `Resources/Themes/`.

## Architecture
```
pipe thread                                   main thread (Idle tick)
RequestDispatcher ─ execute ─▶ NavisMainThreadExecutor.ExecuteAsync
   NavisHeavyGate.Check(code, heavyEnabled) ── ScriptGuard.Check(code, GuardProfile.Navis) ── compile
   audit "started" (nếu HasHeavyCalls) ── MainThreadQueue.RunAsync ─────────▶ NavisScriptRunner.Run(doc, request, script)
                                                                              ├ before = NextUndo; fp0 = NavisChangeCounter.Snapshot(doc)
                                                                              ├ tx = doc.BeginTransaction(label)   [mọi mode — kể cả none]
                                                                              ├ script.RunAsync(globals) với ct (timeout/cancel)
                                                                              ├ tx.Commit(); after = NextUndo; fp1 = Snapshot
                                                                              ├ dryRun|error|timeout|none-modified → RolledBack = RollbackOwn(before, after, label)
                                                                              ├ Changed = Delta(fp0, fp1); Value = NavisResultSerializer
                                                                              └ ExecuteResult
   audit (Message "[heavy] …" khi HasHeavyCalls) ◀── Finish ◀──────────────────┘
```
Quiescence = depth counter + `ModalOpen()` + `IsActiveTransaction` (ADR-04 §2, chỉnh theo S-07/S-08).

## Related Code Files
- Create `HPNavis/HPNavis.McpBridge/`:
  - `Service/NavisScriptRunner.cs` (ma trận, `RollbackOwn`, clamp theo heavy, `HasHeavyCalls`)
  - `Service/NavisChangeCounter.cs` (`Snapshot`/`Delta` ADR-02 §2; interface `IDocumentSnapshotSource` để test không cần Navisworks)
  - `Service/NavisHeavyGate.cs` (pre-pass W2 + path policy + clamp + `HasHeavyCalls`; Roslyn `CSharpSyntaxWalker` riêng)
  - `Service/NavisResultSerializer.cs` (+ `.Items.cs`, `.Clash.cs` nếu > 300 dòng): `ModelItem` → `{displayName, className, path, guid, model, bboxMm, propertyCount}`; collection cắt `MaxOutputBytes`; `SavedViewpoint/SelectionSet/ClashTest/ClashResult/TimelinerTask` summary; `Point3D/BoundingBox3D` mm; **không** đệ quy `Descendants`
  - `Service/NavisContextReader.cs` (`ContextResult` + `NavisInfo`; `IsModifiable = !IsClear && quiescent`; `Selection` ≤ 50 `ElementInfo(Id=index-path hash, Category=ClassDisplayName, Name=DisplayName)`)
  - `Model/NavisScriptGlobals.cs` · `Model/NavisApp.cs` (+ `IsModified`)
  - `ViewModel/NavisBridgeStatusViewModel.cs` (bao `McpBridgeStatusViewModel` Core + `HeavyOperationsEnabled` bind vào executor; heavy disabled khi execution OFF)
  - `View/NavisBridgeStatusView.xaml(.cs)` (hoàn thiện: 2 checkbox, last run, nút Bật/Tắt listener, Sao chép script cuối, mở Thư viện/Log/Audit, Tự khởi động; owner = `Gui.MainWindow.Handle` qua `WindowInteropHelper`) · `Resources/Themes/NavisTheme.xaml` (sáng)
  - `BridgeEntry.cs`, `NavisMainThreadExecutor.cs` (hoàn thiện; `GetContextAsync` busy-ngay; audit `started`)
- Create `HPNavis/HPNavis.McpBridge.Tests/` (net48, xunit; `ProjectReference` bridge + Core → asset net48; refs Navisworks qua `Directory.Build.props`): `NavisHeavyGateTests` (W2 chặn khi OFF với id `HEAVY` + text đúng; cho qua khi ON; UNC/thư mục HPNavis bị chặn; `HasHeavyCalls`), `NavisChangeCounterTests` (delta với snapshot fake), `RollbackDecisionTests` (`RollbackOwn` bảng ADR-02 §1 với `Func<string?> nextUndo` fake), `TimeoutClampTests` (120/600). ≈ 15 test. `SeedLibraryTests` vào đây ở phase 4.
- Create `HPNavis/tools/harness/pipe-scenarios.py` mở rộng (≥ 22), `run-bridge-unattended.ps1` (mở Roamer + gatehouse; UIA tick execution/heavy trên cửa sổ của ta; scenario; kill own pid).
- **Không** tạo: `Ribbon/*`, `en-US/*`, `Images/*`, `NavisThemeSwitcher.cs`.

## Implementation Steps
1. Runner + counter + heavy gate + tests net48 (không cần Roamer chạy để test lớp thuần).
2. Serializer + context reader; `ScriptingSelfCheck` mở rộng (`Search` + `units` + `progress`).
3. Cửa sổ hoàn thiện + VM bao Core VM; `BridgeEntry` entries: `show/start/stop/status/dispose`, `copyLastScript`, `path(library|audit|logs)`, `autoStart.get/set`.
4. Harness pipe scenarios (Navisworks thật, `gatehouse_pub.nwd` + `Getting Started`):

| Nhóm | Scenario | Kỳ vọng |
|---|---|---|
| Đọc | `none`: `doc.Models.Select(m=>m.FileName)` · `Search` theo `Item.Name` (`DisplayStringContains`) · `CurrentSelection.SelectedItems` sau khi harness chọn bằng script `auto` | `IsError=false`, `Changed=0/0/0`, mm/labels đúng, `RolledBack=false` |
| Ghi W1 | `auto` add set / viewpoint / override color · cùng script `dryRun=true` · `auto` **rỗng** `dryRun=true` (S-05b lại) · `manual` → log "behaves like auto" | undo `MCP: …`; dryRun → count không đổi, `RolledBack=true`; rỗng → `RolledBack=false`, undo user còn |
| `none` | `none` + `SelectionSets.AddCopy` | `IsError=true` "declared none but modified", `RolledBack=true`, count không đổi |
| Lỗi | compile error · guard `MessageBox`/`Undo`/`BeginTransaction`/`Expression.Call`/`NavisworksCommand` · pre-pass `AppendFile` heavy OFF (`HEAVY` text nhắc tick checkbox) · exception sau 1 edit · timeout 5 s (`while(!ct.IsCancellationRequested)`) · `cancel_execution` đua | mã/`Message` đúng; timeout → `RolledBack=true`, count không đổi |
| Busy/doc | modal Options → `-32002` · `context` **trong lúc** execute dài chạy → `-32002` ngay (< 1 s) · `IsClear` → `-32003` · opt-in OFF → `-32001` | |
| Heavy | heavy ON: `AppendFile(MEP.nwc)` `dryRun=true` → từ chối trước khi chạy; `dryRun=false` → `Models.Count +1`, audit 2 dòng (`started` + `[heavy]`), `RolledBack=false` · `AppendFile(@"\\srv\x")` → `HEAVY` path policy · `TestsAddCopy`+`TestsRunTest` (Arch vs MEP) → clash > 0, thời gian · heavy OFF lại → `HEAVY` | |
| Context | `context` sau append: `ModelCount +1`, `Models[].units`, `IsBusy=false`, `HeavyOperationsEnabled` đúng | |
| Cửa sổ | UIA: tick execution → status `execution ENABLED`; heavy checkbox disabled khi execution OFF; Bật/Tắt listener → pipe lên/xuống; Add-ins ▸ "HPNavis MCP" mở/activate cửa sổ (đúng 1 instance) | |

5. `reports/phase-02-bridge-runtime.md` (bảng pass/fail, thời gian clash, RAM Roamer trước/sau append, kết luận S-05c → hàng "W1?" ADR-02).

## Todo List
- [ ] Runner/counter/gate + `HPNavis.McpBridge.Tests` ≥ 15
- [ ] Serializer/context/self-check
- [ ] Cửa sổ 2 checkbox + VM bao Core
- [ ] Harness ≥ 22 scenario pass ×2
- [ ] Report + cập nhật ADR-02 §3

## Success Criteria
- [ ] `dotnet build HPNavis/HPNavis.slnx -c Debug -p:DeployPlugin=false` xanh; `dotnet test HPNavis/HPNavis.McpBridge.Tests` ≥ 15 pass (runner log `.NET Framework 4.8`).
- [ ] `pwsh HPNavis/tools/harness/run-bridge-unattended.ps1` → **≥ 22/22 pass, 2 lần liên tiếp**, Roamer tự thoát; report trích log `RollbackOwn` cho dryRun/timeout/rỗng.
- [ ] Undo menu Navisworks hiện `MCP: <label>` sau run `auto` có edit; **không** có entry sau run `auto` rỗng.
- [ ] Heavy OFF: script chứa `AppendFile` **không** chạy (diagnostic id `HEAVY`, text nhắc checkbox); heavy ON: chạy, audit có dòng `"outcome":"started"` trước và dòng `Message` bắt đầu `[heavy]` sau; run thường không có `[heavy]`.
- [ ] `grep -rn "HeavyOperationsEnabled" McpShared/` = 0 (cờ chỉ ở HPNavis); `wc -l` mọi `.cs` mới < 300; `grep -c '#[0-9A-Fa-f]\{6\}' HPNavis/HPNavis.McpBridge/View/*.xaml` = 0.

## Risk Assessment
- S-05c cho `CurrentSelection` không undo → hàng "W1?" thành "không undo, ghi trong mô tả"; không chặn.
- Serializer đụng `Descendants` → chỉ serialize thứ script `return`; giới hạn `MaxOutputBytes`.
- `IsWindowEnabled` false do dialog của chính plugin khác → `-32002` giả; log Debug + staleness 120 s.
- Theme sáng cố định — known gap (Navisworks không có API theme rõ).
