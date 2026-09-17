---
phase: 2
title: "Bridge runtime: EtabsExecutor (tiers từ fixture, snapshot vô điều kiện, fingerprint, path policy, audit), window 2 checkbox, HPEtabs.McpBridge.Tests ≥ 25 (cần ETABS)"
status: completed
priority: P1
effort: "8h"
dependencies: [0, 1]
---

# Phase 2: Runtime bridge ETABS đầy đủ + bridge tests

## Context Links
- [ADR-02 §1–5](adr/adr-02-no-transaction-snapshot-tiers-and-opt-ins.md) (fixture tier, ma trận, `PREVIEW`, `-32001`, fingerprint, snapshot, path policy) · [ADR-04 §1–6](adr/adr-04-sta-worker-busy-cancel-units-guard-globals.md) · [ADR-03 §4](adr/adr-03-etabsv1-reference-and-test-without-etabs.md) (bridge tests cần ETABS) · [red-team](reports/red-team-2026-09-16.md) hàng 1–6, 8, 11, 14c/d/e · `reports/phase-01-spike.md`
- Mẫu: `HPNavis/HPNavis.McpBridge/{NavisMainThreadExecutor.cs, NavisMainThreadExecutor.Audit.cs, Service/NavisHeavyGate.cs, Service/NavisChangeCounter.cs, Service/NavisResultSerializer.cs, Service/BoundedOutputStream.cs, View/NavisBridgeStatusView.xaml, ViewModel/NavisBridgeStatusViewModel.cs:81–91}`; `McpShared/HPRebar.McpBridge.Core/ViewModel/McpBridgeStatusViewModel.cs`; tests `HPNavis/HPNavis.McpBridge.Tests/*`.

## Overview
Thay executor spike bằng runtime thật: tier từ **fixture** `etabs-oapi-tiers.txt` sinh đủ từ CHM index (semantic bind vào `ETABSv1.dll` thật), enforce động (fingerprint add/delete), snapshot **vô điều kiện** cho W/D trong budget (presave có điều kiện, 2 bucket, UNC refused, label sanitize), `PREVIEW`, `-32001` cho D-off, path policy tĩnh + `args` động, audit `started` trước forced save, serializer có giới hạn, context 10 field, cửa sổ đầy đủ (copy Navis view: 2 checkbox; Attach/Detach; warning > 1 ETABS; text forced save; confirm khi đóng lúc busy), ≥ 25 bridge test (**cần ETABS cài** — ADR-03 §4). Live check qua `live-verify.py --phase bridge`.

## Requirements
- Functional (ADR-02 ma trận): R chạy + fingerprint (drift → `changed` + `Logs`, `isError=false`); W: không path/UNC → `-32003`, audit `started`, presave có điều kiện, `Save()`, copy, chạy trong budget, `Snapshot`=tên file, throw → `rolledBack:false`; D: OFF → JSON-RPC `-32001` text destructive; ON → path policy + `started [destructive]` + snapshot + 600 s; `none`/dryRun trên W/D → `PREVIEW` (`isError=true`); `manual` ≡ `auto` + log; `Analyze(code, transaction)` với checkbox OFF → member D và `none`+W → `PREVIEW` trong `GuardViolations`; `Cancel` set token; `GetContextAsync` → `EtabsInfo` 10 field (busy → `-32002` < 1.5 s; chưa attach → `IsAttached=false`); units ép/restore; not-attached/no-model → `-32003`; liveness eager; worker foreground drain.
- Non-functional: mọi call OAPI trên STA worker; timeout luôn fail; `SafeText.StripPaths` (Contracts) cho message; output ≤ 64 KB; collections cap 200; không public static executor/analyzer/snapshot manager; cờ destructive chỉ set qua VM trên UI thread.

## Architecture
```
HPEtabs/HPEtabs.McpBridge/
├── EtabsExecutor.cs                       IBridgeExecutor; STA foreground; control lane; liveness; MainThreadQueue(expireWithoutTicks); DestructiveOperationsEnabled (internal, UI-thread only); ExecuteAsync: guard → compile → EtabsTierAnalyzer → ma trận → -32001 | PREVIEW | enqueue
├── EtabsExecutor.Audit.cs                 started (mọi tier ≥ W, trước snapshot, ghi "forced save") · completed/failed + tags [destructive]/[snapshot:<file>]/[tier:R|W|D]
├── Service/EtabsTierAnalyzer.cs           fixture lookup (cInterface.Member → tier, path flag); semantic bind ETABSv1; unbound → D; path-argument shape check (literal | args.Str/Require); PREVIEW/DESTRUCTIVE diagnostics; MaxTimeoutSeconds 120/600
├── Service/EtabsPathPolicy.cs             literal + args screen: UNC, HPEtabs\McpBridge|McpServer, \Computers and Structures\, prefix model dir | %LocalAppData%\HPEtabs\
├── Service/EtabsSnapshotManager.cs        presave (mtime/size ≠ lần ghi của bridge) → File.Save() → prerun copy; buckets 5/10; label [A-Za-z0-9_-]{1,40}, reject "..", GetFullPath prefix assert; returns file name
├── Service/EtabsFingerprint.cs            GetNameList per receiver (theo E13b) + IsLocked + filename; Diff → Added/Deleted (Modified=0)
├── Service/EtabsScriptRunner.cs           budget clock → units → [snapshot] → script → fingerprint → restore units; result assembly
├── Service/EtabsResultSerializer.cs       BoundedOutputStream; ETABSv1 objects summarised; arrays cap 200
├── Service/EtabsContextReader.cs          EtabsInfo 10 field + DocPath/DocTitle/IsModifiable
├── Resources/etabs-oapi-tiers.txt         fixture đầy đủ (EmbeddedResource) — sinh bởi tools/generate-oapi-tier-fixture.py từ CHM index, review tay, commit
├── ViewModel/EtabsBridgeStatusViewModel.cs bao McpBridgeStatusViewModel; IsDestructiveEnabled (ép OFF khi execution OFF; Dispatcher.CheckAccess); Attach/Detach; warning > 1 ETABS; LastRun; audit tail; CopyLastScript; CloseRequested confirm khi busy
└── View/EtabsBridgeStatusView.xaml(.cs)   copy Navis view: status, 2 checkbox (+ text "writing scripts save the model first"), attach panel, last run, audit tail
HPEtabs/HPEtabs.McpBridge.Tests/            net8.0-windows, xunit v3, MTP; ProjectReference bridge + Core; <Error> khi thiếu ETABS
HPEtabs/tools/generate-oapi-tier-fixture.py one-off: CHM index → fixture; không chạy trong build
```

## Related Code Files
- Create: mọi file trên; `HPEtabs.McpBridge.Tests/*` (dưới).
- Modify: `BridgeEntry.cs` (wire runtime thật), `View`/`VM` phase 1 → đầy đủ; `tools/harness/live-verify.py` + `run-live-verify.ps1` thêm `--phase bridge` (scenario dưới).
- Test classes (`HPEtabs.McpBridge.Tests`, ≥ 25, **0 skip trên máy dev**, bind `ETABSv1.dll` thật):
  - `EtabsTierFixtureTests` (≥ 3): mọi topic `c*.X Method` trong CHM index (bản snapshot index commit cạnh fixture `etabs-oapi-index.txt`) có dòng fixture; không trùng; tier ∈ {R,W,D}; các member red-team #2 (`ExportFile`, `GetTableForDisplayCSVFile`, `EditGeneral.Move`, `StartDesign`, `ModifyUndeformedGeometry`, `MergeAnalysisResults`, `ShowTablesInExcel`, `ResetOverwrites`, `RenameTower`) đều **D**.
  - `EtabsTierAnalyzerTests` (≥ 8): R cho `GetModelFilename`/`PointObj.GetNameList`/`GetTableForDisplayArray`/`RefreshView`/`AnalysisResults.FrameForce`; W cho `FrameObj.SetSection`/`FrameObj.AddByCoord`/`SetTableForEditingArray`; D cho `SetModelIsLocked`/`Analyze.RunAnalysis`/`DeleteResults`/`ApplyEditedTables`/`File.Save("x")`/`File.OpenFile`/`InitializeNewModel`/`FrameObj.Delete`/`ExportFile`; alias `var fo = sapModel.FrameObj; fo.SetSection(...)` → W; `list.Add(1)` không W; unbound → D; tier = max; `PREVIEW` liệt kê member; `Analyze(code, "none")` với W → `PREVIEW`.
  - `EtabsPathPolicyTests` (≥ 5): literal UNC/`HPEtabs\McpBridge`/`\Computers and Structures\` refused; dưới model dir OK; `%LocalAppData%\HPEtabs\` OK; biểu thức ghép `"\\" + "\\srv"` → refused (shape); `args.Str("out")` chấp nhận tĩnh, giá trị UNC trong `args` → refused động.
  - `EtabsSnapshotManagerTests` (≥ 6, thư mục temp, `Func<int> save` giả): thứ tự audit-started → presave? → save → prerun; presave **không** tạo khi mtime/size = lần ghi trước của bridge, **có** khi user đã save; `save` ≠0 → không copy, lỗi; retention 5/10 theo bucket; label `../x` → sanitized + không thoát bucket (traversal test); trả **tên file**.
  - `EtabsFingerprintTests` (≥ 3): bằng → 0; tên mới → `Added`; tên mất → `Deleted`; `Modified` luôn 0.
  - `EtabsUnitsPolicyTests` (≥ 2): set → chạy → restore kể cả ném; set ret≠0 → fail trước script.
  - `EtabsErrorMappingTests` (≥ 4): not attached → `-32003` "not attached"; no model → `-32003` "No model (.EDB)"; busy → `-32002` (tick giả); D-off → `-32001` text destructive (không `ExecuteResult`).
  - `EtabsGuardCollisionTests` (≥ 2): tên member ETABSv1 (fixture) ∩ base deny list → danh sách (kỳ vọng chứa `GetProperty`, `File`) ghi report; script chứa `HPEtabs.McpBridge.BridgeEntry.Stop()` / `McpBridgeHost.Current` → `GUARD`.
  - `EtabsResultSerializerTests` (≥ 2); `EtabsLivenessTests` (≥ 2: `HasExited` → `Attached=false` + queued item fail `-32003`; `isQuiescent` true khi `!Attached`).

## Implementation Steps
1. `generate-oapi-tier-fixture.py` → fixture + index snapshot; review tay các dòng D/W biên; `EtabsTierFixtureTests`.
2. `EtabsTierAnalyzer` + `EtabsPathPolicy` + tests.
3. `EtabsSnapshotManager` + `EtabsFingerprint` (theo E9/E11/E13b) + tests.
4. `EtabsScriptRunner` (ma trận, budget) + units + serializer + tests.
5. `EtabsExecutor` đầy đủ + audit + context + liveness + drain; VM + View.
6. `live-verify.py --phase bridge`: R read · R + `SetSection` → `PREVIEW` · dryRun W → `PREVIEW` (count không đổi) · W `AddByCoord` (args) → snapshot file tồn tại trong `prerun\`, `Snapshot` = tên file, `Changed.Added=1` · W lần 2 không tạo presave mới; 👤 save trong GUI → W lần 3 tạo presave · W throw sau ghi → `rolledBack:false` + `Snapshot` · D OFF → `-32001` (JSON-RPC error, không `isError`) · D ON (UIA) → `SetModelIsLocked` round-trip · guard ×6 · compile error · cancel đua · timeout 5 s vòng lặp → `TimedOut`, request kế `-32002`/OK · budget: `timeoutSeconds=5` với `Save()` giả chậm? (không giả được — đo thời gian `Save()` thật, ghi) · not-attached · no-model (👤 hoặc skip) · label `../x` → tên file sanitized · UNC model → `-32003` (👤 `-UncModelPath`, else manual) · audit `started` trước `Save()` (thứ tự dòng). Chạy ×2 với ETABS thật 👤.
7. `reports/phase-02-bridge-runtime.md`.

## Todo List
- [x] Fixture + index + `EtabsTierFixtureTests` (reflection trên DLL — `generate-oapi-tier-fixture.ps1`; 1 281 dòng; CHM 1 107/1 107 phủ)
- [x] Tier analyzer (semantic) + path policy (+ tests)
- [x] Snapshot + fingerprint (+ tests)
- [x] Runner ma trận/budget + units + serializer (+ tests)
- [x] Executor + audit + context + liveness + VM/View (probe gỡ; link thư mục snapshot)
- [x] `--phase bridge` 17 + `bridgedestructive` 7 scenario ×2 với ETABS thật (2026-09-17)
- [x] Report `reports/phase-02-bridge-runtime.md`

## Success Criteria
- [x] `dotnet test HPEtabs/HPEtabs.McpBridge.Tests` → **184** pass, 0 fail, **0 skip** trên máy dev (166 trước review round).
- [x] `dotnet build HPEtabs/HPEtabs.slnx -c Debug` xanh; `Helper\.` chỉ trong `EtabsAttachment.cs`; không public static executor/analyzer/snapshot manager (chỉ hàm thuần).
- [x] `run-live-verify.ps1 -Phase bridge -Runs 2` → 48 pass ×2; prerun 10 (retention), presave 2; audit `started` trước forced save (B5c), `[destructive]` (D2), không `started` khi path bị từ chối (D4a).
- [x] D với checkbox OFF: JSON-RPC `-32001` (B9, unit test `A_destructive_member_with_the_second_opt_in_off_is_a_json_rpc_refusal_not_a_run`).
- [x] Fixture: 1 107/1 107 topic CHM có dòng (`Every_documented_method_topic_has_a_row`); va chạm guard ≤ 12 tên, không gồm member thường dùng (`The_guard_denies_few_oapi_member_names…`).

## Risk Assessment
- Fixture sinh sai tier ở member biên (vd `SetPresentUnits` = W đúng; `GetTableForDisplayCSVFile` = D đúng) → review tay + test pin các member red-team #2.
- `Save()` model lớn ăn budget → mô tả tool nói; harness đo thời gian.
- `.File` member access bị base guard chặn (E20) → nếu vậy seed D1 gọi `sapModel.Analyze.RunAnalysis()` không cần `File`; `Save` chỉ bridge gọi.
- Bridge tests cần ETABS → CI không có; README.

## Security Considerations
- Destructive OFF mỗi lần mở, không persist, ép OFF khi execution OFF, chỉ UI thread; `Analyze` luôn OFF; path policy literal + `args`; audit trước ghi đĩa; không public static.

## Next Steps
- Phase 3 seed trên runner này; phase 4 harness `--phase full`.
