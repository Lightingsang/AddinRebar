---
title: "HPGrasshopper — đánh giá sau sửa kế hoạch 261013-0600"
type: code-review-reaudit
date: 2026-10-13
timezone: Asia/Bangkok
baseline: 261013-0600-hpgrasshopper-reaudit.md
status: substantially-improved-partial-acceptance
---

# HPGrasshopper — đánh giá lại lúc 15:31 ngày 13/10/2026

## 1. Kết luận

**Phần lớn lỗi cụ thể trong kế hoạch 261013-0600 đã có sửa đổi đúng hướng và regression tests, nhưng chưa hoàn thành toàn bộ tiêu chí nghiệm thu.** N1 đã chặn đường tạo rebar một phần do invalid set; N2 đã propagate rollback failure; N3 đã có batch scope/token/deferred commit; N6/N7/N8/N9 đã khắc phục nhiều lỗi baseline.

Các phần còn thiếu chủ yếu là coverage GH/Rhino rollback, identity bắt buộc và context freshness trên wire. Có thêm các lỗi cần xử lý ở final outcome merge, output list trùng tên, scope disposal và domain-failure scoring.

Review chỉ đọc production source, chạy test/storage tạm và tạo báo cáo này; không sửa code, deploy, publish hoặc ghi model thật. Evidence paths tương đối với repository root `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar`; số dòng là snapshot đọc trong lần đánh giá này.

Không dùng số test hoặc tên “empirical” để khẳng định host thật đã Verified. Tuân thủ AGENTS.md và bộ governance architecture/core/Revit/workflow/checklist đã đọc.

## 2. Kiểm chứng mới

| Kiểm tra Debug | Passed | Failed | Skipped | Giới hạn |
|---|---:|---:|---:|---|
| HPGrasshopper.Mcp.Server.Tests | 118 | 0 | 0 | Registry/schema/quality/compiler/mapping |
| HPGrasshopper.E2E.Tests | 181 | 0 | 0 | Chủ yếu simulated host/protocol/harness |
| HPGrasshopper.McpBridge.Tests, toàn bộ | 703 | 0 | 0 | Production helpers/queue/scope với fakes/proxies/stubs |
| tools/list server Debug, registry sạch tạm | — | — | — | **27 tool** |
| get_grasshopper_context qua server Debug độc lập | — | — | — | **Bridge not connected**, isError=true |

**1.002 test passed; không excluded.** Baseline 261013-0600 có 928 test. Counts mới xác nhận suites chạy qua, không chứng minh native solve, geometry/model output, Undo/Redo hoặc Rhino rollback.

Lệnh đã chạy từ `HPGrasshopper/`:

```powershell
dotnet test HPGrasshopper.Mcp.Server.Tests/HPGrasshopper.Mcp.Server.Tests.csproj -c Debug -p:DeployAddin=false
dotnet test HPGrasshopper.E2E.Tests/HPGrasshopper.E2E.Tests.csproj -c Debug -p:DeployAddin=false
dotnet test HPGrasshopper.McpBridge.Tests/HPGrasshopper.McpBridge.Tests.csproj -c Debug -p:DeployAddin=false
```

tools/list/context dùng `McpShared/tools/mcp-call.py`, server vừa build và LibraryPath/DbPath thư mục tạm, WatchLibrary=false. Không dùng registry người dùng. Full live harness chưa chạy. Báo cáo không đính kèm full runner logs; kết quả đã đọc từ tool outputs mới.

## 3. Đối chiếu N1–N10

| Mục | Sửa đổi hiện tại | Kết luận theo tiêu chí cũ |
|---|---|---|
| N1 partial rebar commit | Preflight toàn bộ validatedSets trước creation; creation failure throw; thêm endpoint/coplanarity/connectivity checks | Lỗi partial commit cụ thể đã sửa trong source. Native seed/history vẫn cần kiểm chứng; domain-failure scoring còn thiếu |
| N2 rollback failure ignored | CanvasRollbackResult được đưa vào ExecutionOutcome; failed return/throw tạo IsError và RolledBack=false | Core propagation đã sửa và có unit regression; final native payload merge còn lỗi G1 |
| N3 batch scope/token | RevitTransactionScope, token checkpoints, external canvas scope defer commit | Cơ chế đã triển khai; chưa native Running-cancel proof; scope leak early validation G5 |
| N4 graph/Rhino/net ledger | Baseline GUID set giữ remove→re-add; có TrackModifiedObject | Baseline ownership đã sửa. Existing GH state/Rhino coverage vẫn chưa đầy đủ G3 |
| N5 mandatory identity | Normalize empty paths; RequireTargetIdentity truyền từ engine | Chưa đạt đầy đủ: flag phụ thuộc explicit args, Revit target có thể bỏ GH target G4 |
| N6 assignment/tree partial | Stage toàn input, reject malformed/null/unsupported, kiểm AddVolatileData Boolean | Các lỗi Boolean/silent parse-partial đã sửa. Conversion restore và input-after-solve chưa verified |
| N7 filtered duplicate output | Giữ cả scalar A/B và tạo aggregate/unique keys | Scalar overwrite đã sửa. Output đầu dạng List bị shared-reference mutation G2 |
| N8 diagnostics/casing | lastRun/lastBatch giữ payload; dùng BridgeJson khi materialize | Các lỗi baseline đã sửa; final failure vẫn có thể giữ success=true G1 |
| N9 wrong containers | Validator reject wrong containers/items; runtime guards | Lỗi no-op cụ thể đã sửa. Structured public schema và parameter updates vẫn backlog |
| N10 stale context | Markers/timestamp trong coordinator, invalidation title khác | Chưa đủ: metadata chưa tới wire; same-title/different-path và snapshot consistency G6 |

Không lặp lại các lỗi đã sửa như findings mới: ignored AddVolatileData false, silent malformed-tree parsing, PascalCase native result, Commit canvas trước Revit, thiếu batch scope/token hoặc remove→re-add baseline bị xóa theo logic cũ.

## 4. Findings còn lại và mới

### G1 — P1 — Failed final outcome có thể trở lại MCP success

- **Evidence:** `HPGrasshopper/HPGrasshopper.McpBridge/Execution/GrasshopperExecutionEngine.cs:99–100,142–143` gọi MergeOutcomeValue khi execRes.IsError. Helper `:187` chỉ đặt success=false nếu payload chưa có success.
- **Trigger:** Player/batch handler tạo `{success:true,...}`; sau đó cancellation/deadline hoặc Revit finalize failure làm outer execution rollback. Merge vẫn giữ success=true, thêm error/rolledBack.
- `Host/GrasshopperRpcDispatcher.cs:108–109` trả Value qua JSON-RPC Success dù outer IsError=true; server `Tools/GrasshopperToolResult.cs:33–36` chỉ nhận success=false/isError=true.
- **Impact:** MCP IsError=false có thể xuất hiện cho operation failed/cancelled/rolledBack. Đây là lỗi control flow đã xác định từ source, chưa reproduce trên model thật.
- **Rule:** T4, PCC-290.
- **Sửa/gate:** merge của failed outcome phải force success=false hoặc isError=true; giữ diagnostics và rollback/timedOut chính xác. Test successful handler payload + failed transaction/cancel qua merge → dispatcher → server mapper, không chỉ payload vốn success=false.

### G2 — P2 — Aggregate output làm nhiễm List của output đầu

- **Evidence:** `Tools/PlayerOutputExtractor.cs:73` gán firstUniqueKey = firstMatch.Value; `:79–81` nếu alias đã là List<object?> thì Add(extractedVal) trực tiếp.
- **Trigger:** hai outputs nickname R, requestedOutputs=["R"], output đầu là List `[1,2]`, output sau là scalar `3`.
- **Impact:** alias và firstUniqueKey giữ cùng List; thêm 3 vào aggregate cũng đổi giá trị output đầu thành `[1,2,3]`. Lỗi scalar overwrite cũ đã sửa; đây là aliasing mới.
- **Rules:** M6, T4.
- **Sửa/gate:** tạo aggregate bucket riêng khi có collision; phân biệt list data của một parameter với list nhiều outputs. Test unique keys giữ nguyên từng value, grouping không mutate input list, cả filtered/unfiltered và nhiều-item outputs.

### G3 — P1 — TrackModifiedObject chưa được dùng production; Rhino chưa rollback

- **Evidence:** `Execution/CanvasUndoScope.cs:120` có TrackModifiedObject nhưng tìm trong bridge chỉ có method definition, không production call site. Constructor vẫn chỉ auto listen ObjectsAdded/Deleted và lưu baseline GUIDs, không serialized snapshot mọi object.
- Roslyn globals vẫn actual GH/Rhino documents (`Execution/GrasshopperExecutionEngine.cs:258–266`), không Rhino undo/snapshot.
- **Trigger/impact:** script đổi existing nickname/pivot/persistent data/direct wires hoặc Rhino geometry, dryRun/error chưa bảo đảm restore. Unit test gọi TrackModifiedObject thủ công không chứng minh đường script tự track.
- **Rules:** R2, T4.
- **Sửa/gate:** snapshot/restore supported state trước script hoặc mutation APIs bắt buộc track; Rhino scope/ownership hoặc reject unsupported writes. Test production execution đổi existing values/wires và Rhino objects, so before/after.

### G4 — P1 — Identity enforcement vẫn phụ thuộc explicit target args

- **Evidence:** `Execution/GrasshopperExecutionEngine.cs:51` hasExplicit dựa args; `:107,150,242` truyền flag này cho RequireTargetIdentity. Request không explicit target có flag false.
- `:54` dùng `parsedDefId ?? (hasExplicit ? null : cachedDefId)`; chỉ cung cấp Revit title/path có thể bỏ GH cached DefId.
- **Trigger/impact:** cache identity chưa có hoặc script/native request chỉ target Revit; chuyển GH A→B trước callback có thể không được chặn theo đúng tài nguyên bị sửa. Cached GH switch riêng chưa có lifecycle subscription đầy đủ.
- **Rule:** R4.
- **Sửa/gate:** require identities theo tài nguyên write, không theo việc user có args hay không; Revit target không tắt GH target; normalize/capture snapshot UI đúng lifecycle. Test native default args + null initial cache, explicit Revit title + queued GH switch.
- Empty-path normalization đã sửa, không lặp lỗi unsaved-path baseline.

### G5 — P2 — External canvas scope không dispose khi Revit validation early-return

- **Evidence:** `Execution/GrasshopperExecutionEngine.cs:127` tạo `var scope` không using/finally; `RevitTransactionScope.cs:47–48` readonly/family/modifiable rejection return trước finalize canvas.
- **Impact:** listeners và undo record của scope có thể còn attached sau request bị từ chối; scope tiếp tục nhận graph edits về sau. Operation chưa mutation nhưng resource lifetime không kết thúc.
- **Rules:** R9, T4.
- **Sửa/gate:** validate trước khi tạo mutation scope và dispose mọi exit; tránh cleanup gọi solve khi operation chưa chạy. Test rejected readonly/family request rồi graph edit không có listener/undo record leak.

### G6 — P2 — Stale metadata nội bộ chưa được serialize cho AI

- **Evidence:** IsContextStale/IsContextUnavailable/LastSnapshotTimestamp chỉ ở coordinator/tests, không trong serialized ContextResult/server output. Extractor kiểm cached matching chủ yếu title, rồi dùng current path với cached flags/selection/GH.
- **Trigger/impact:** hai project cùng title khác path, close-doc hoặc UI timeout; fallback có thể trộn/current hóa facts cũ mà AI không biết stale/unavailable.
- **Rules:** R1, T4, P9.
- **Sửa/gate:** additive wire metadata freshness/timestamp; trả nguyên immutable snapshot nhất quán, so path/session/definition identity và invalidate đúng lúc. Test JSON thực tế cached A/current B cùng title khác path, không chỉ coordinator property.

### G7 — P2 — Seed domain rejection có thể được chấm successful/verified run

- **Evidence:** rebar preflight hiện trả `{success:false,message}` trước creation ở `Registry/SeedLibrary/Revit/generate_rebar_curves/code.cs:130,135,139,146,162`. RevitTransactionScope coi normal return là success; ExecuteResult.IsError dựa outcome.Error.
- `McpShared/HPRebar.Mcp.Server.Core/Registry/ToolLifecycleService.cs:121–122` chấm success và VerifiedRunId theo !result.IsError.
- **Impact:** model không đổi nhờ preflight, nhưng invalid seed input có thể được history/test_tool đánh giá success/verified. Khác lỗi partial commit N1 cũ đã sửa.
- **Rules:** T4, P8.
- **Sửa/gate:** chốt domain-result contract cho seed/registry: reject bằng exception phù hợp trước mutation hoặc explicit mapping trước history/test scoring. Không áp heuristic cho mọi arbitrary script return khi chưa có contract. Test seed invalid input đi qua actual run_tool/test_tool và record outcome.

## 5. Các nghiệm thu còn thiếu

- Input Player vẫn volatile assignment rồi ExpireSolution/NewSolution; chưa có chứng minh native input A/B tạo output tương ứng sau solve. Helper stage/bool fixes không đủ đóng yêu cầu này.
- Native Running batch cancellation/deadline, native Undo/Redo và state restore trên GH/Rhino/Revit chưa được chạy do bridge offline.
- N6 native conversion có thể fail sau một số items đã accepted; helper báo rejected, nhưng chưa restore baseline assignment. Headless=false lifecycle đưa failed/partially bound doc lên canvas cần policy/test rõ.
- Empty branches/tree topology, typed element refs và arbitrary geometry roundtrip chưa đủ contract/proof. Đây là capability limits, không tự mở rộng bugfix thành mọi feature.
- New tests N3 “during solve” vẫn pre-cancel/null document; N1 nhiều tests tự mô phỏng/copy geometry algorithms; N4 baseline injection dùng reflection. Useful unit tests nhưng chưa proof native workflow.
- N8 challenger dùng successful/failed helper data chưa đủ actual server→pipe→native final-outcome failure; cần case G1.
- Missing fixture/native load failure paths trong PlayerRunnerTests vẫn counted pass; mock E2E/hardcoded Player/PNG không chứng minh host thật.
- Chưa tìm thấy report remediation mapping N-id→diff→native logs trong plans/docs/HPGrasshopper. Giữ baseline lịch sử; cập nhật report mới theo findings đã sửa và còn thiếu.

## 6. Thứ tự đề nghị

1. **Sửa G1 trước:** final failed execution tuyệt đối không quay lại success trong MCP.
2. **Hoàn thiện G3/G4:** rollback coverage và mandatory identity trên public production routes.
3. **Sửa G2/G5/G6/G7:** list aliasing, resource cleanup, wire freshness và history/test scoring.
4. **Verification:** test đúng production paths, actual native fixture input→solve→output, cancellation đang Running, restore/undo; báo mock/metadata/native/live/skipped riêng.
5. **Feature riêng:** catalog, parameter edits, save/open/script authoring, geometry roundtrip và stable reconciliation. Không trộn với bugfix.

Kết thúc khi mỗi mục có mapping source→test→result, và các phần cần host thật được Verified trên tài liệu thử nghiệm đã cho phép hoặc giữ CHƯA TEST với giới hạn rõ. Chưa triển khai các đề xuất trong lần review này.
