---
title: "HPGrasshopper — đánh giá lại R1–R9 sau sửa ngày 13/10"
type: code-review-reaudit
date: 2026-10-13
timezone: Asia/Bangkok
baseline: 261012-2212-hpgrasshopper-reaudit.md
status: improved-partially-remediated-not-live-verified
---

# HPGrasshopper — đánh giá lại ngày 13/10/2026

## 1. Kết luận và phạm vi

**Có sửa đổi thực chất, nhưng chưa đủ điều kiện đóng toàn bộ R1–R9.** Native RPC đã dùng coordinator chung; native failure được map sang MCP IsError; commit canvas đã chuyển sau Revit finalization; port validation, typed input helpers, diameter tolerance và compiler lifecycle đã được bổ sung. Các lỗi còn lại chủ yếu nằm ở atomicity/rollback, đầu vào Player, identity và kiểm chứng production.

Phạm vi: source hiện tại trong `HPGrasshopper/` và các thành phần `McpShared/` liên quan; đối chiếu báo cáo `plans/reports/261012-2212-hpgrasshopper-reaudit.md`. Repository root là `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar`. Evidence paths bên dưới tương đối với root; số dòng thuộc snapshot đọc trong lần này.

Không sửa production code, deploy, publish hoặc ghi model thật. Chỉ chạy các test/storage tạm và tạo báo cáo này. Tuân thủ governance AGENTS.md, architecture, core clean-code, Revit appendix/standard, tool workflow và review checklist. Finding source khác với reproduction native; không báo Verified cho những phần chưa chạy host thật.

## 2. Bằng chứng mới

| Kiểm tra Debug | Passed | Failed | Skipped | Phạm vi |
|---|---:|---:|---:|---|
| HPGrasshopper.Mcp.Server.Tests | 117 | 0 | 0 | Registry/schema/quality/seed compilation/result mapping |
| HPGrasshopper.E2E.Tests | 181 | 0 | 0 | Chủ yếu simulated host/protocol/harness |
| HPGrasshopper.McpBridge.Tests, toàn bộ | 630 | 0 | 0 | Dispatcher/queue/analyzer/transaction/helper logic với fakes/stubs |
| tools/list trên server vừa build, registry tạm | — | — | — | **27 tool** |
| get_grasshopper_context qua app connector | — | — | — | Transport closed |
| get_grasshopper_context qua server Debug mới, registry tạm độc lập | — | — | — | **Bridge not connected**, isError=true |

**928 test passed; không excluded.** Baseline trước là 772 = 77 server + 181 E2E + 514 bridge. Tăng test không chứng minh native solver, Revit commit, Rhino rollback hay actual Undo/Redo.

Lệnh đã chạy từ `HPGrasshopper/`:

```powershell
dotnet test HPGrasshopper.Mcp.Server.Tests/HPGrasshopper.Mcp.Server.Tests.csproj -c Debug -p:DeployAddin=false
dotnet test HPGrasshopper.E2E.Tests/HPGrasshopper.E2E.Tests.csproj -c Debug -p:DeployAddin=false
dotnet test HPGrasshopper.McpBridge.Tests/HPGrasshopper.McpBridge.Tests.csproj -c Debug -p:DeployAddin=false
```

tools/list và context dùng `McpShared/tools/mcp-call.py` với LibraryPath/DbPath trong thư mục tạm, WatchLibrary=false; không ghi registry người dùng. Full live harness chưa chạy. Counts lấy từ output mới; file này không đính kèm full runner logs.

## 3. Đối chiếu R1–R9

| Mục cũ | Sửa đổi được xác minh từ source/tests | Trạng thái |
|---|---|---|
| R1 native lifecycle | Dispatcher → Coordinator.ExecuteAsync; request/session/deadline/correlated cancel dùng chung; Player có scope/token | Phần bypass Player/queue đã sửa; batch solve vẫn thiếu scope và cancellation đang Running |
| R2 native isError | GrasshopperToolResult.From xử lý success=false/isError=true và giữ JSON | Lỗi MCP IsError đã sửa; diagnostics Player dưới Revit và casing còn thiếu |
| R3 commit/rollback | Revit finalize trước canvas Commit qua ICanvasUndoScope | Timing đã sửa; rollback failure result bị bỏ; chưa coverage existing GH/Rhino state |
| R4 ledger/undo | Add→remove guard, wire tracking trước mutation, restore errors được thu thập | Partial: remove→re-add baseline object mất sau rollback; propagation failure chưa đầy đủ |
| R5 identity | Native dùng engine cached title/path/DefId; missing path/definition được reject | Partial: RequireTargetIdentity chưa được dùng production; cached GH lifecycle/null/stale chưa đủ |
| R6 preflight | Upper port bounds, remove+wire conflict; create/resolve failure throw | Các lỗi baseline này đã sửa; wrong container shape vẫn no-op; parameter updates chưa có |
| R7 Player | Numeric triple thông thường giữ list; point object, strict parser, rejected inputs, GUID filters | Partial: AddVolatileData return bỏ qua; partial tree; output filter overwrite; chưa prove input sau solve |
| R8 seed | diameterToleranceMm; normal qua mọi pair; descriptions DirectShape/query trung thực hơn | Các lỗi baseline đã tiến bộ; có partial-commit path; coplanarity chưa kiểm endpoints |
| R9 lifecycle/context | Compiler AssemblyLoad + fingerprint thật; fallback không đọc GH graph background | Compiler invalidation đã nối production; context fallback vẫn stale/unknown không đánh dấu |

Không lặp lại findings cũ: native dispatcher không còn gọi Player trực tiếp; port 999 đã được kiểm; numeric list triple không còn luôn biến thành Point; GUID output đã hỗ trợ; wire action không còn tạo sau mutation; compiler không còn chỉ invalidation method được tests gọi.

## 4. Findings cần sửa tiếp

### N1 — P1 — Seed rebar có thể commit một phần rồi trả failure

- **Evidence:** `HPGrasshopper/HPGrasshopper.Mcp.Server/Registry/SeedLibrary/Revit/generate_rebar_curves/code.cs:106–127`: CreateFromCurves trong vòng lặp; set sau non-planar trả anonymous `success=false` ở `:124` thay vì throw.
- `HPGrasshopper/HPGrasshopper.McpBridge/Execution/RevitTransactionScope.cs:139–145` coi action return bình thường là success và finalize transactions; ExecuteResult.IsError dựa outcome.Error, không dựa nested payload.
- **Trigger:** set 0 hợp lệ đã tạo rebar, set 1 bị kiểm non-planar và return failure.
- **Impact:** rebar set 0 có thể commit dù batch thất bại; outer run có thể được ghi thành success. Đây là finding từ control flow, chưa tạo model thật để reproduce.
- **Rules:** R2, T4.
- **Sửa/gate:** validate toàn bộ sets trước creation hoặc throw mọi rejection sau mutation; define domain-result contract cho registry scripts. Test valid+invalid sets phải giữ nguyên model và báo lỗi ở outer result/history. Không chỉ đổi description hoặc assert seed chứa CheckCoplanar.

### N2 — P1 — Roslyn canvas rollback failure không ảnh hưởng kết quả cuối

- **Evidence:** `Execution/RevitTransactionScope.cs:165–180` gọi Rollback qua Action, bỏ `CanvasRollbackResult.Success/Errors`, rồi log rolled back cleanly. `Execution/CanvasUndoScope.cs:151` đã trả structured failure nhưng caller không sử dụng.
- **Impact:** restore GH thất bại có thể vẫn IsError=false và RolledBack=true theo Revit group; trường hợp catch cũng không kết hợp trạng thái từng tài nguyên.
- **Rules:** T4, PCC-290.
- **Sửa/gate:** kết hợp Revit/GH rollback statuses trong ExecutionOutcome; expose partial restore/errors; log truthful. Fake ICanvasUndoScope trả Failed không throw phải khiến result báo failure/partial, không clean rollback.

### N3 — P1 — Batch solve vẫn ngoài Revit boundary và bỏ token

- **Evidence:** `Execution/GrasshopperExecutionEngine.cs:123–133` callback có ct nhưng ExecuteBatch không nhận token/scope. `Tools/CanvasEditor.cs:36–37` solve rồi commit trực tiếp.
- **Trigger:** graph solve có node ghi Revit/Rhino; hoặc batch đang Running khi deadline/cancel tới.
- **Impact:** canvas có thể commit và trả success sau cancellation; Revit/Rhino effects không thuộc canvas compensation. CAS queued cancellation không hủy operation đang Running.
- **Rules:** R2, R9, T4.
- **Sửa/gate:** token checkpoints trước mutation và trước final commit; defer canvas commit tới final outcome; scope solver-side Revit writes và giới hạn unsupported Rhino effects. Public native batch cancel/timeout trong solve cần state assertions trên fixture được cho phép.

### N4 — P1 — GH/Rhino mutations và baseline remove→re-add chưa restore đúng

- **Evidence:** CanvasUndoScope chỉ nghe ObjectsAdded/Deleted; script đổi persistent values/NickName/Pivot/direct AddSource không tự populate snapshot/ledger. Roslyn vẫn cấp RhinoDoc thật mà không Rhino scope.
- Với object O tồn tại trước scope: Remove(O) tạo snapshot; Add(O) đưa GUID vào `_addedGuids` (`CanvasUndoScope.cs:66–70`); rollback remove added rồi skip snapshot restore (`:196,215`) → O bị mất.
- **Rules:** R2, T4.
- **Sửa/gate:** ledger theo baseline ownership/net transitions hoặc snapshot state đầy đủ; reject mutations chưa có cơ chế rollback. Test baseline remove→add→dryRun/error, remove→add→remove, thay existing parameter/wire/pivot và Rhino geometry. Các lỗi add→remove cũ đã sửa; chiều ngược lại còn thiếu.

### N5 — P1/P2 — Identity chưa bắt buộc; unsaved path và GH switch chưa đồng nhất

- **Evidence:** `Coordinator/McpBridgeWorkRequest.cs:19` default RequireTargetIdentity=false; không có production caller truyền true. Engine default identity từ coordinator cache.
- Snapshots cập nhật qua ViewActivated/context, không bảo đảm GH active-definition switch riêng. Null DefId có thể bỏ bảo vệ GH; stale DefId có thể reject thao tác hợp lệ tới definition mới.
- `Host/GrasshopperAddinApplication.cs:127` cache raw PathName rỗng; validator reject empty current path khi target non-null. Context extraction lại normalize empty→null, nên unsaved doc có thể hoạt động khác trước/sau gọi context.
- **Rules:** R4, M8.
- **Sửa/gate:** bắt buộc identity theo tài nguyên write, UI capture/session token ổn định; normalize path tại source; missing/stale identity explicit. Test initial null cache, GH switch không đổi Revit view, unsaved document và close/reopen cùng tên.

### N6 — P2 — Player input có thể báo applied dù assignment thất bại

- **Evidence:** `Tools/PlayerDataTreeHelper.cs:20,25,30,41,58,68,97,107` bỏ Boolean của AddVolatileData rồi return true/tăng count.
- Tree mixed valid/malformed branch ở `:83–86` chỉ log/continue; có bất kỳ item valid thì return true tại `:112`. Unsupported/null items bị bỏ và indices bị dồn.
- **Impact:** incompatible conversion hoặc invalid branch có thể bị bỏ nhưng inputsApplied tăng, rejectedInputs không ghi lỗi; data pairing có thể thay đổi.
- **Rules:** M8, T4.
- **Sửa/gate:** stage/validate toàn input trước mutation; kiểm từng AddVolatileData return; reject hoặc báo partial có structured diagnostics. Test target từ chối conversion, tree `{"{0}":[1],"{a}":[2]}`, null ở giữa branch và partial bind.
- API McNeel nói AddVolatileData có thể trả false khi conversion thất bại; không thể coi parse JSON thành công là assignment thành công.

### N7 — P2 — Requested output trùng tên lại ghi đè dữ liệu

- **Evidence:** `Tools/PlayerOutputExtractor.cs:54–63` suffix key cho output thứ hai, rồi ghi lại alias `result[matchedKey]`.
- **Trigger:** hai outputs nickname R với giá trị A/B và requestedOutputs=["R"]. Sau output thứ hai, R=B và R#guid=B; A mất.
- **Rule:** T4.
- **Sửa/gate:** full GUID identity hoặc list/map giữ mọi output; alias không overwrite khi ambiguous. Test cả filtered duplicate names và GUID requests. Unfiltered path đã cải thiện, không đồng nghĩa filtered path đúng.

### N8 — P2 — Player lỗi dưới Revit mất diagnostics; native JSON đổi casing

- **Evidence diagnostics:** `Execution/GrasshopperExecutionEngine.cs:96–107` lấy error string rồi throw, reconstruct chỉ success/error/rolledBack; original errors/warnings/rejectedInputs/duration bị bỏ.
- **Evidence casing:** `MapToExecuteResult` ở `:164` default SerializeToElement đóng băng record Document/Components/Wires và nested DTO PascalCase. BridgeJson camelCase không đổi keys của JsonElement đã materialize.
- **Impact:** AI mất dữ liệu sửa graph; clients dùng document/components/wires cũ có thể hỏng dù routing đã thống nhất.
- **Rules:** P9, T4.
- **Sửa/gate:** giữ original diagnostic payload khi cần throw để rollback; dùng BridgeJson.Options khi materialize; exact-shape public tool test và compatibility, không chỉ case-insensitive deserialize.

### N9 — P2 — Wrong operation container vẫn silently no-op

- **Evidence:** CanvasBatchValidator và CanvasEditor return khi property không Array thay vì báo lỗi; nested parameters vẫn JsonElement.
- **Trigger:** componentsToAdd là object thay vì array, cùng operation khác hợp lệ.
- **Impact:** operation sai bị bỏ, phần còn lại commit; success không tương ứng đầy đủ yêu cầu.
- **Rules:** M8, T4.
- **Sửa/gate:** absent/null optional khác với supplied wrong kind; schema structured arrays/items; test mixed valid+wrong-container. Upper port bounds/remove+wire conflict đã sửa và không nên lặp finding cũ.

### N10 — P2 — Context cached facts bị trộn với identity hiện tại

- **Evidence:** `Coordinator/GrasshopperContextExtractor.cs:48–57` title/path từ current coordinator nhưng flags/units/selection/GH từ cached context; không timestamp/stale marker. Khi chưa cache còn defaults m/false.
- **Trigger:** context A cached, chuyển sang B, UI deadline không lấy được snapshot mới.
- **Impact:** AI nhận title B nhưng units/selection/definition A; unknown được trình bày như facts.
- **Rules:** R1, T4.
- **Sửa/gate:** immutable consistent snapshot, timestamp/stale/unavailable; không mix resources. Compiler AssemblyLoad/fingerprint đã sửa production wiring và không thuộc lỗi còn lại này.

## 5. Phần cần native verification, chưa kết luận lỗi thực nghiệm

- Player hiện ClearData → AddVolatileData → ExpireSolution(false), sau đó NewSolution(true), chưa có persistent parameter/slider binding. Cần chứng minh input thực sự thay output sau solve. AddVolatileData test với proxies chỉ counts không đủ; chưa chạy probe native hoặc fixture thật trong lần này.
- Rebar CheckCoplanar kiểm tangents dot normal, chưa kiểm endpoint point-plane distance/connectedness. Hai segment parallel ở các plane khác có thể pass preflight nhưng native API reject; chưa khẳng định sẽ tạo hình học sai.
- Actual Undo/Redo, Rhino state restoration và S1–S3 acceptance từ handoff chưa được chạy do bridge offline.
- Image modes/viewport restore đã có source, còn cần ảnh native để xác nhận extents/selection/resolution/lifecycle.

## 6. Test và tài liệu

- Tests mới có giá trị cho public coordinator/cancel/error helper, commit-ordering với TrackingCanvasUndoScope, validator và typed helper. Phải giữ, nhưng không gán native Verified cho simulated contexts.
- R8 geometry/diameter challenger vẫn tự tính ResolveBarType/ComputeCurveNormal/CheckCoplanar trong test, chưa chạy seed thật. Test partial-commit cần đi qua production script và native/fake execution phù hợp.
- PlayerRunnerTests vẫn có missing fixture/native load failure paths counted pass; E2E workload/Player/PNG còn mock/hardcoded. Số skipped=0 không có nghĩa mọi native requirement đã chạy.
- Fixture archive metadata/custom chunks chưa chứng minh authentic runnable graph.
- Skill đã cập nhật 27 tool, schema/registry names và descriptions summary; không lặp finding tài liệu vẫn count24. Nhưng còn cam kết Revit rollback 100% cần thu hẹp/cập nhật theo coverage thực tế, nhất là batch.
- Chưa tìm thấy remediation report mapping R-id→diff→logs riêng trong phạm vi plans/docs/HPGrasshopper. Giữ baseline lịch sử; dùng báo cáo này để cập nhật trạng thái.

## 7. Thứ tự sửa đề nghị

1. **P1 trước:** N1 partial rebar commit, N2 truthful rollback result, N3 batch scope/cancel, N4 resource/net-ledger coverage, N5 identity.
2. **P2:** N6/N7 binding/output correctness, N8 diagnostics/casing, N9 structured input, N10 stale context.
3. **Verification:** regression đúng production paths + actual native fixture cho input-after-solve, commit failure, cancel và undo; báo mock/metadata/native/live riêng.
4. **Feature riêng:** catalog/port discovery, parameter editing, save/open/script component, geometry roundtrip và stable update/reconciliation. Không gộp feature backlog vào bugfix để tăng scope.

Antigravity phải đọc governance, giữ changes khác, không ghi live model; cập nhật mỗi N-id bằng source location, test mới, commands và limits. Không đóng findings chỉ bằng test name, log text hoặc copied formula.

## 8. API đối chiếu

- [McNeel — IGH_Param.AddVolatileData](https://developer.rhino3d.com/api/grasshopper/html/M_Grasshopper_Kernel_IGH_Param_AddVolatileData.htm): assignment có Boolean success/failure.
- [McNeel — GH_Document.NewSolution](https://developer.rhino3d.com/api/grasshopper/html/M_Grasshopper_Kernel_GH_Document_NewSolution.htm): true yêu cầu tính lại mọi object.
- [McNeel — GH_ActiveObject.ExpireSolution](https://developer.rhino3d.com/api/grasshopper/html/M_Grasshopper_Kernel_GH_ActiveObject_ExpireSolution.htm): expiration đặt trạng thái Blank và propagate xuống downstream.

Docs online có thể hiển thị Rhino mới hơn version local; phải đối chiếu native fixture/version cài trước khi khẳng định semantics thực nghiệm. Không trích dẫn docs thay cho bằng chứng đã chạy chương trình.
