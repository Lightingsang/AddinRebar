---
title: "HPGrasshopper MCP — đánh giá lại sau sửa Antigravity"
type: code-review-reaudit
date: 2026-10-12
timezone: Asia/Bangkok
status: partially-remediated-not-live-verified
baseline: 261012-1229-hpgrasshopper-antigravity-handoff.md
---

# HPGrasshopper MCP — đánh giá lại sau sửa

## 1. Kết luận

**Có cải thiện thực chất, nhưng chưa đủ căn cứ nghiệm thu hoàn tất.** Routing được bổ sung, queue chung có cancellation, batch solve trước commit, undo có native actions, fixture đã cách ly, Player có kiểm runtime errors và typed data/tree helpers. Tuy nhiên public native RPC và execute+label vẫn đi qua hai đường khác nhau; nhiều bảo vệ mới chỉ nằm ở đường execute.

Ưu tiên tiếp theo là thống nhất execution lifecycle và bảo đảm rollback/error/identity đúng trên public native tools. Không cần viết lại toàn bộ kiến trúc hoặc thêm tool trùng chức năng.

Repository: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar`.

Đường dẫn evidence bên dưới tương đối với repository root; số dòng thuộc source hiện tại trong lần đánh giá này. Đây là review chỉ đọc source: không sửa production code, deploy, publish hoặc ghi model thật. Chỉ tạo báo cáo này và chạy build/test với storage tạm.

Baseline là báo cáo `plans/reports/261012-1229-hpgrasshopper-antigravity-handoff.md`. Các lỗi baseline không còn tồn tại được ghi nhận là đã sửa, không lặp lại như finding mới.

## 2. Kiểm chứng mới

| Kiểm tra | Passed | Failed | Skipped | Phạm vi bằng chứng |
|---|---:|---:|---:|---|
| HPGrasshopper.Mcp.Server.Tests | 77 | 0 | 0 | Registry/schema/quality/seed compilation |
| HPGrasshopper.E2E.Tests | 181 | 0 | 0 | Chủ yếu simulated host/protocol/Roslyn harness |
| HPGrasshopper.McpBridge.Tests, toàn bộ | 514 | 0 | 0 | Bridge logic, dispatcher/queue/analyzer/helpers với fakes/stubs |
| tools/list của server Debug, registry sạch tạm | — | — | — | **27 tool**, native schemas đã đọc thực tế |
| get_grasshopper_context live | — | — | — | **Bridge not connected** |

**772 test passed; không có excluded trong lần chạy bridge này.** Lớp fixture hiện dùng IsolatedFixtureDirectory trong thư mục tạm nên không cần filter loại 13 test như baseline. Số test tăng không chứng minh native solver/transaction/undo đã hoạt động đúng.

Chạy từ `HPGrasshopper/`, có build theo `dotnet test`, configuration Debug:

```powershell
dotnet test HPGrasshopper.Mcp.Server.Tests/HPGrasshopper.Mcp.Server.Tests.csproj -c Debug -p:DeployAddin=false
dotnet test HPGrasshopper.E2E.Tests/HPGrasshopper.E2E.Tests.csproj -c Debug -p:DeployAddin=false
dotnet test HPGrasshopper.McpBridge.Tests/HPGrasshopper.McpBridge.Tests.csproj -c Debug -p:DeployAddin=false
```

tools/list dùng `McpShared/tools/mcp-call.py`, server Debug mới build, `HPGRASSHOPPER_MCP_Registry__LibraryPath`/`DbPath` trỏ tới thư mục tạm và WatchLibrary=false. Không ghi registry người dùng. Không chạy full live harness. Báo cáo này không đính kèm log file đầy đủ; counts là kết quả mới đã đọc từ tool output.

## 3. Đối chiếu F1–F13

| Finding | Điều đã sửa | Phần còn lại / trạng thái |
|---|---|---|
| F1 routing | Có GrasshopperRpcDispatcher cho bốn native RPC | Routing đã triển khai; public route còn bypass protections và error mapping — R1/R2/R3 bên dưới |
| F2 dual rollback | Roslyn tạo CanvasUndoScope; engine Player có Revit scope | Partial: Rhino/existing GH mutations chưa restore; native Player bypass scope; commit ordering sai |
| F3 none read-only | Chặn explicit/target-typed Transaction; không báo rollback group không tồn tại như trước | Partial: syntax detection/counter không bảo đảm mọi GH/Rhino/solver side effect read-only; cần native state tests |
| F4 atomicity/undo | Batch NewSolution trước Commit; record có AddAction; archive bytes được dùng | Partial: wire snapshot tạo sau mutation; compensation vẫn bỏ một số errors; Roslyn ledger chưa đúng net changes |
| F5 queue | Kiểm Raise result; CAS cancellation cho queued request; timeout cho execute | Đã sửa queue chung/execute; native RPC chưa đăng ký request/CTS/deadline |
| F6 identity | WorkRequest có validator title/path/DefId | Partial: native không điền identity; engine không tự capture GH DefId; missing/path cases còn fail-open |
| F7 context/compiler | Thử UI-thread context; có InvalidateCompiler | Partial: fallback defaults vẫn như facts; invalidation chưa nối production lifecycle |
| F8 seed contracts | Facade/stirrup/curtain/export mô tả calculate/plan; count export dùng collector | Cải thiện lớn; DirectShape/query geometry còn hứa quá implementation; một số input semantics chưa đúng |
| F9 ignored inputs | Structural flag áp dụng; Solid/Mesh có nhánh; diameter được dùng | Partial: diameter nearest không tolerance; collinear plane fallback còn lỗi |
| F10 batch | Preflight, tempId→GUID output, disconnect port indices | Partial: upper-bound ports/mixed plan chưa validate; parameter editing chưa có |
| F11 Player/reader | Points/tree/typed primitives; suffix chống overwrite; errors/missing outputs checks; previews | Partial: native isError, rejected inputs, type ambiguity, GUID output/geometry roundtrip còn thiếu |
| F12 tests | Fixture source-write đã sửa; nhiều test production helper mới | Isolation đã sửa; native unavailability-as-pass và simulated workload/dual rollback chưa chứng minh host thật |
| F13 image/headless | Focus modes, zoom .1–5, viewport restore; visible doc vào DocumentServer | Implemented; chưa Verified live về extents/resolution và failure lifecycle |

## 4. Findings còn lại, theo ưu tiên

### R1 — P1 — Native Player/batch chưa dùng boundary và cancellation của engine

- **Evidence:** `HPGrasshopper/HPGrasshopper.McpBridge/Host/GrasshopperRpcDispatcher.cs:196–197` gọi thẳng PlayerRunner.Run, bỏ callback token. `:204–207` gọi trực tiếp CanvasEditor.
- Engine Player ở `Execution/GrasshopperExecutionEngine.cs:90–106` mới có RevitTransactionScope và linkedToken. Public `run_grasshopper_player` không đi qua đường này.
- Dispatcher `:80–94` chỉ acquire/release busy. Coordinator chỉ set active request/session tại `Coordinator/GrasshopperBridgeCoordinator.cs:110`, CTS tại `:126` trong đường ExecuteAsync; cancel target ID bị từ chối ở `:248`, cancel không ID gọi null CTS ở `:254` nhưng vẫn có thể báo accepted.
- **Trigger:** native Player/batch chạy graph có side effects, hoặc queued request khi host không tick; client timeout gửi correlated cancel.
- **Impact:** public tool chưa có rollback/validation/deadline protections đã bổ sung cho engine; có thể tiếp tục giữ busy sau timeout. Canvas scope không thể restore Revit/Rhino side effects của solve.
- **Rules:** R2, R9, T4.
- **Sửa:** một admission/execution lifecycle chung cho native RPC và script/engine, gắn request/session ID, linked cancellation/deadline và transaction policy rõ cho solver có thể ghi Revit. Native Player schema hiện không có dryRun: không tự tuyên bố supported.
- **Gate:** public native request → production dispatcher → timeout/cancel khi không host tick phải hoàn tất và release busy; Player lỗi giữa solve phải restore tài nguyên theo contract trên fixture được cho phép.

### R2 — P2 — Native failure payload vẫn là MCP success

- **Evidence:** `Host/GrasshopperRpcDispatcher.cs:128–135` chỉ map khi result là ExecuteResult; native handlers trả anonymous objects rồi bị JsonRpcEnvelope.Success.
- Server native tools gọi formatter.Text; `McpShared/HPRebar.Mcp.Server.Core/Services/ResultFormatter.cs:17` đặt IsError=false. MapToExecuteResult mới trong engine không được public native route dùng.
- **Trigger:** file Player không tồn tại, batch preflight fail, image không có canvas, graph runtime Error/missing output.
- **Impact:** payload `success:false` nhưng CallToolResult.IsError=false; AI có thể coi thao tác thành công. Test `McpBridge.Tests/Host/GrasshopperRpcDispatcherTests.cs` còn assert Error=null cho Player file không tồn tại.
- **Rule:** T4.
- **Sửa/gate:** map result thống nhất qua public route; test xuyên server/dispatcher với missing file, failed preflight, solve error và missing output; giữ operation diagnostics/rollback status thay vì chỉ generic message.

### R3 — P1 — GH commit trước Revit final outcome; rollback chưa phủ mutations

- **Evidence:** `Execution/GrasshopperExecutionEngine.cs:239–252` canvas scope commit bên script action. `Execution/RevitTransactionScope.cs:142–146` sau action mới kiểm cancellation/finalize; auto commit ở `:257`.
- **Trigger:** script đổi GH rồi return; Revit commit bị từ chối hoặc deadline đã tới.
- **Impact:** Revit rollback nhưng canvas đã completed, không restore.
- CanvasUndoScope chỉ nghe ObjectsAdded/Deleted ở `Execution/CanvasUndoScope.cs:42–43`; script đổi persistent values, NickName, Pivot hoặc trực tiếp AddSource không nằm trong ledger. Không có Rhino snapshot/undo trong Roslyn execution.
- **Rules:** R2, T4.
- **Sửa/gate:** giữ các scopes tới khi có final outcome; commit tất cả sau thành công cuối; snapshot/restore hoặc reject unsupported mutation. Test đổi existing params/wires/pivot, Rhino geometry, failed Revit commit và cancellation sau script return bằng state trước/sau, không chỉ log/counters.

### R4 — P1/P2 — Canvas ledger add→remove và wire undo chưa đúng

- **Evidence:** `Execution/CanvasUndoScope.cs:70–89` track cả added/deleted; rollback ở `:148–150` remove added rồi re-add removed tại `:221`.
- **Trigger/impact:** thêm A rồi xóa A trong cùng scope; dryRun có thể re-add A dù baseline không có A. Cần phân biệt removed-original với added-in-scope và net changes.
- `Tools/CanvasEditor.cs:216–217,246–247` mutate wire trước khi Track; CanvasUndoScope `:130,137` mới tạo GH_WireAction. Snapshot native action được tạo sau thay đổi, chưa đủ bằng chứng Ctrl+Z restore trạng thái trước.
- Một số restore wire/archive failures vẫn chỉ log (`CanvasUndoScope.cs:176,185,213,235`), caller có thể báo clean rollback.
- **Rule:** T4, PCC-290.
- **Sửa/gate:** ledger theo baseline ownership; tạo undo action trước mutation; surface partial restore; test add→remove→rollback, direct wire changes và actual Undo/Redo Sources, không chỉ ActionCount.

### R5 — P1 — Identity validation vẫn không bảo vệ public native requests

- **Evidence:** `Host/GrasshopperRpcDispatcher.cs:179,196,204,214` tạo WorkRequest không target identity; `Coordinator/McpBridgeWorkRequest.cs:25` early return khi các field đều null.
- Engine ExtractTargetIdentity dùng default title, path/DefId optional; không capture active GH DocumentID. Validator `McpBridgeWorkRequest.cs:52` bỏ path mismatch khi current PathName rỗng; `:66` không reject khi expected DefId có nhưng active GH doc null.
- **Trigger:** đọc A rồi chuyển active definition/document sang B trước callback, hoặc đóng definition.
- **Impact:** vẫn có thể ghi nhầm definition/document. Có validator không đồng nghĩa đã dùng ở public route.
- **Rule:** R4.
- **Sửa/gate:** capture identity ở đúng UI snapshot và bắt buộc kiểm lại trước write trên mọi route; missing target fail; tests chuyển A→B và close/reopen cùng tên trong khoảng enqueue/callback.

### R6 — P2 — Preflight chưa bảo đảm atomic complete batch

- **Evidence:** `Tools/CanvasBatchValidator.cs:195–202` chỉ kiểm port integer/nonnegative; không so với actual port count. `Tools/CanvasEditor.cs:275` trả null khi vượt count, ConnectWires bỏ operation; `:77` vẫn success=true.
- **Trigger:** valid component creation + wire sourceOutput=999; hoặc wire tham chiếu object đồng thời nằm trong componentsToRemove.
- **Impact:** preflight pass; mutation giữ component nhưng không nối wire; atomic success không đúng. Catalog empty được coi canCreate=true; EmitObject null vẫn continue ở `CanvasEditor.cs:117`.
- Signature/ApplyMutations vẫn chưa expose update parameter/position của object hiện hữu, dù description public còn hứa.
- **Rules:** M8, T4.
- **Sửa/gate:** validate operation plan theo graph sau batch, resolve actual ports/types và fail thay skip; test valid+invalid mixed batch, remove+connect, missing plugin/catalog. Triển khai updates hoặc thu hẹp contract trong scope bugfix.

### R7 — P2 — Player typed input/output mới đạt một phần

- **Evidence:** `Tools/PlayerDataTreeHelper.cs:52–60` mọi numeric array 3 phần tử thành point, không xét target type; `:145` malformed tree path chuyển lỗi parse thành index 0; null/unsupported items bị bỏ.
- `Tools/PlayerRunner.cs:191` không báo rejected/unmatched inputs; GUID output filter chưa hỗ trợ, còn theo NickName tại `:223,236`. Helper xuất point `{x,y,z}`, nhưng input object chỉ xử lý tree keys; chưa symmetric roundtrip.
- Revit element identifiers vẫn chỉ numeric GH_Number, chưa binding typed element reference như description. Brep/curve/mesh chưa serialization roundtrip.
- **Impact:** ba giá trị số có thể thành Point; sai input âm thầm bị bỏ; kết quả có tree nhưng chưa bảo đảm type/path/null semantics đầy đủ.
- **Rules:** M8, T4.
- **Sửa/gate:** discriminator hoặc parameter-aware input binding; strict paths; applied/rejected inputs và lỗi; GUID identity; typed points/element references. Test list `[1,2,3]` trên numeric target, point trên point target, malformed path, point output→input và yêu cầu output GUID.
- **Chưa khẳng định lỗi volatile injection:** source vẫn AddVolatileData trước NewSolution; cần native test cho thấy thay input thực sự thay output sau solve, không chỉ inputsApplied tăng.

### R8 — P2 — Diameter/plane và seed description còn lệch

- **Evidence:** `Registry/SeedLibrary/Revit/generate_rebar_curves/code.cs:35–46` chọn nearest diameter không tolerance; request 16mm với model chỉ 12mm vẫn tạo 12mm. `:72–80` cross zero ở hai segment đầu giữ normal Z; fallback không chạy vì Z có length=1.
- **Trigger:** nhiều segment đứng/collinear hoặc hai segment đầu collinear rồi rẽ trong XZ; đường kính không tồn tại.
- **Impact:** có thể tạo diameter khác yêu cầu hoặc Revit từ chối plane; không đủ để đóng F9.
- DirectShape đã có Solid/Mesh box, nhưng tool description còn hứa complex Rhino Brep/Mesh; query_revit_geometry còn hứa stream geometry vào GH trong khi implementation JSON summary/top-level solids. Curtain U/V/layoutRule semantics chưa đầy đủ.
- **Rules:** M8, T4, P8.
- **Sửa/gate:** exact/tolerance policy, explicit fallback được người dùng hiểu; tìm pair không collinear, verify toàn plane; descriptions và schema khớp implementation. Test 16mm chỉ có 12mm; collinear prefix/XZ plane; unsupported geometry.
- Export count đã dùng collector thật, không lặp finding fixed 5/10 cũ. Validate category defaults và invalid category handling riêng. Stable create/update identity và geometry arbitrary vẫn là feature backlog.

### R9 — P2 — Context fallback/compiler invalidation chưa hoàn tất

- **Evidence:** `Coordinator/GrasshopperBridgeCoordinator.cs:267–279` context deadline 500ms rồi fallback Extract(null). Extractor vẫn defaults readonly=false, units=m, selection=[] và đọc GH từ background.
- `InvalidateCompiler` chỉ có method definition và tests, chưa production call sites theo tìm kiếm C# hiện tại.
- **Impact:** UI bận → context unknown bị trình bày như facts; compiler tạo trước Rhino readiness có thể vẫn thiếu refs khi host sẵn sàng.
- **Rules:** R1, T4.
- **Sửa/gate:** explicit unavailable/stale + UI cached snapshot; không enumerate GH trên pipe fallback; nối lifecycle readiness/reference fingerprint. Test không gọi InvalidateCompiler thủ công để giả lập việc production không thực hiện.

## 5. Test và tài liệu cần cập nhật

- Giữ tests helper/queue/analyzer mới có giá trị, nhưng thêm public-route tests. Các test “BothLayers/BothRevitAndCanvas” trong `Regression/F1_F7_CoreSafetyRegressionTests.cs:114–161` vẫn chỉ assert SimulatedTransactionContext counters; không xác nhận GH/Rhino state.
- `tools/PlayerRunnerTests.cs:232–275` vẫn có missing fixture/native initialization paths được tính pass thay vì skip rõ. Cần assert computed outputs trên native fixture hoặc báo skipped đúng.
- E2E Tier4 vẫn dùng fake executor counter/delay/synthetic element; Player/PNG harness outputs vẫn hardcoded. Báo mock/protocol/native/live riêng.
- FixtureBuilder tạo archive metadata/custom chunks; fixture cách ly đã đúng nhưng tính runnable/solve thật chưa được chứng minh.
- `.agents/skills/hp-mcp-grasshopper/SKILL.md:68–77` vẫn count 24, schema cũ `includeParams/includeErrors`, Player dryRun/extractOutputs và batch components/wires/parameters khác runtime 27 tool. Skill còn cam kết rollback 100% và capabilities vượt implementation. Cập nhật theo schema thực tế sau khi chốt các sửa chữa.
- Chưa tìm thấy report remediation mapping F-id→diff→verification trong phạm vi HPGrasshopper/docs/plans; handoff gốc vẫn unchecked. Giữ baseline lịch sử và tạo báo cáo tiến độ mới, không ghi đè kết luận cũ thành “đã verified”.

## 6. Thứ tự đề nghị

1. **Bugfix ưu tiên:** một execution lifecycle cho public native + coordinator; request/session/deadline/cancel; transaction/error mapping/identity (R1/R2/R5).
2. **Bugfix rollback:** final commit ordering, net-change ledger, existing graph/Rhino coverage và honest restore status (R3/R4).
3. **Bugfix input/domain:** full operation plan validation, typed input/path/rejected input, diameter/plane và context/lifecycle (R6–R9).
4. **Verification:** production-path regression và ba native acceptance fixtures S1–S3 từ handoff, trên tài liệu được cho phép; state assertions thay log/counters.
5. **Feature riêng:** catalog/port discovery, persistent parameter editing, save/open/script-component authoring, arbitrary geometry và rerun reconciliation. Không gộp mọi feature vào bugfix để chữa thiếu bằng chứng.

### Điều kiện đủ để đóng các findings

- [ ] Native RPC và execute có cùng policies, cancellation và kết quả lỗi nhất quán.
- [ ] Public timeout/cancel không để busy/queued work treo và không chạy muộn sau cancellation.
- [ ] Expected document/definition identity được điền và enforce thực tế.
- [ ] dryRun/error/commit failure restore state đúng trên từng tài nguyên được contract cho phép.
- [ ] Batch không âm thầm bỏ operation đã yêu cầu; Player không âm thầm bỏ input.
- [ ] Domain inputs quyết định đúng sản phẩm hoặc được reject/warn theo policy rõ.
- [ ] Test mock, compilation, native, live và skipped được báo riêng.
- [ ] Skill/schema/descriptions/examples cập nhật khớp implementation cuối.
- [ ] Live trên fixture được cho phép đạt criteria, hoặc giữ trạng thái CHƯA TEST với giới hạn cụ thể.

**Không sửa source trong lần đánh giá này. Các findings còn lại là yêu cầu sửa/kiểm chứng tiếp theo, không phải tuyên bố đã tái hiện mọi tác động trên host thật.**
