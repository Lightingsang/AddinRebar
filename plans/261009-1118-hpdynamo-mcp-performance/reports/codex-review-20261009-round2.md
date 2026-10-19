# HPDynamo — đánh giá lại sau vòng sửa Antigravity thứ hai

Ngày: 2026-10-09. Workspace: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar`.

Đối chiếu với `reports/codex-review-20261009.md`. Phạm vi: source hiện tại của HPDynamo, thay đổi cancellation trong McpShared, tests và benchmark mới. Đây là review; không sửa production, không deploy bridge, không chạy graph hoặc batch trên mô hình đang mở. Đã build server, chạy tests, gọi MCP đọc context/tools và đọc registry SQLite bằng chế độ read-only.

## Kết luận

**Có tiến bộ thực chất, nhưng chưa đủ điều kiện nghiệm thu nâng cấp.** Các lỗi queued cancellation, truyền dryRun và nhận diện MirrorData collections đã được sửa đúng hướng. Tuy nhiên, batch vẫn chưa rollback đầy đủ; cancellation có thể báo rollback sai; headless/batch vẫn thiếu chờ evaluation; chính sách transaction mới phụ thuộc chuỗi trong code. Benchmark mới chưa chứng minh tốc độ hoặc độ đúng đắn.

Ưu tiên tiếp theo: sửa các lỗi tính đúng đắn bên dưới, giải quyết compile/deployment/registry thực tế, rồi đo lại. Chưa có cơ sở để bổ sung cache/index/transport mới chỉ nhằm đạt các con số hiệu năng đang ghi trong kế hoạch.

## 1. Bằng chứng mới

### Build và tests do Codex chạy lại

| Bộ kiểm tra | Succeeded | Failed | Skipped |
|---|---:|---:|---:|
| HPDynamo.Mcp.Server.Tests | 127 | 0 | 0 |
| HPDynamo.McpBridge.Tests | 170 | 0 | 0 |
| HPDynamo.E2E.Tests | 209 | 0 | 0 |
| HPRebar.Mcp.Server.Core.Tests | 915 | 0 | 0 |
| HPRebar.McpBridge.Core.Net48Tests | 115 | 0 | 0 |
| **Tổng** | **1.536** | **0** | **0** |

Lệnh: `dotnet test <project>` từ HPDynamo hoặc McpShared tương ứng. Build `dotnet build HPDynamo.Mcp.Server/HPDynamo.Mcp.Server.csproj -c Debug`: thành công, 0 warnings, 0 errors. Tests bridge cũng build dependency bridge; không deploy vào Revit.

### MCP và registry thực tế

Gọi qua `McpShared/tools/mcp-call.py` với server Debug vừa build:

- `get_dynamo_context`: `result.isError=true`, nội dung **Dynamo bridge not connected**.
- `get_active_graph_info`: JSON-RPC `error.code=-32602`, **Unknown tool: 'get_active_graph_info'**; Python vẫn exit 0.
- `tools/list`: **22 tools**, không có `batch_edit_graph`, `search_nodes_library`, `get_active_graph_info`.
- Đọc `%AppData%\HPDynamo\McpServer\tools-library\**\tool.json`: ba tool trên đều `quarantined`, 10 seed còn lại `published`.
- Đọc `registry.db` bằng SQLite URI `mode=ro`: có **15 run thất bại** của ba tool trên trong khoảng 09:29–09:45 UTC / 16:29–16:45 giờ địa phương; lỗi **The script does not compile. Fix the listed errors and retry.** Không có run thành công của ba tool trong 15 record được truy vấn.

Các quan sát này chứng minh trạng thái vận hành hiện tại chưa đáp ứng tool surface kỳ vọng. Chưa có diagnostics compile chi tiết của các run lịch sử trong truy vấn này; không quy kết một nguyên nhân compile cụ thể hoặc tự phục hồi tool. Bridge đang chạy trước đây có thể khác binary hiện build: phải ghi assembly hash/version khi kiểm chứng lại.

## 2. Đối chiếu từng finding trước

| Finding trước | Trạng thái hiện tại | Phần còn thiếu |
|---|---|---|
| RV-01: dryRun và rollback batch | **Sửa một phần** | `globals.dryRun` và merge outer flag đã có; có rollback khi exception. Snapshot/restore vẫn sai cho input value, nhiều replacement và CodeBlock ports. |
| RV-02: bỏ qua kết quả evaluation | **Sửa một phần** | Canvas runners và canvas `run_dyn_file` kiểm tra bool và linked token. Hủy/chờ scheduler kết thúc và correlation evaluation chưa có. |
| RV-03: seed bypass runner sync | **Sửa một phần** | Canvas seed dùng `RunAndWait`; batch và headless vẫn chưa tương đương. |
| RV-04: cancel nhầm request | **Sửa một phần** | ID được truyền qua interface opt-in; stale cancel khác ID trong cùng session được bỏ qua. Chưa xử lý session ID reuse và cancel-of-cancel. |
| RV-05: Completion bị cancel khi work đang chạy | **Sửa đúng nguyên nhân chính** | CAS Queued/Running/Completed ngăn early Completion; Dispose drain queue. Có lỗi mới khi coordinator ghi đè kết quả transaction sau late cancel. |
| RV-06: auto C# transaction bị bỏ hoàn toàn | **Sửa một phần** | C# đơn giản được mở auto transaction trở lại; chọn ownership bằng substring chưa đúng contract. |
| RV-07: MirrorData list/dictionary | **Sửa đúng hướng trong source** | `IsCollection/GetElements`, Dictionary `Keys/ValueAtKey` đã có. Chưa xác nhận production compile/live output, các runner khác vẫn ToString. |
| RV-08: output không giới hạn | **Sửa một phần** | Có depth/breadth/per-port item budget; chưa giới hạn byte/string và metadata cắt dữ liệu chưa tách khỏi payload. |

Không lặp lại kết luận cũ rằng mọi đường chạy đều bỏ qua bool, mọi cancel đều global, hoặc dryRun seed vẫn chỉ đọc args.

## 3. Findings cần sửa

### R2-01 [P1] dryRun không khôi phục value của node input có sẵn

`HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/batch_edit_graph/code.cs:170–175,223–224` — **RV2, R2, T4**.

`origVal` chỉ có giá trị khi node là CodeBlock. Number/String/Boolean input có sẵn được snapshot `null`; restore bỏ qua `null`. Đổi input bằng `properties[].value`, rồi dryRun hoặc lỗi cuối batch: giá trị mới vẫn còn trên canvas. Rollback Revit TransactionGroup không rollback dữ liệu workspace Dynamo.

Sửa: snapshot value thật theo API/serialization phù hợp. Nếu chưa hỗ trợ rollback một node type, từ chối cập nhật trước mutation và báo rõ. Test phải đối chiếu giá trị trước/sau của node input thực sự, cả dryRun và exception sau mutation.

### R2-02 [P1] Thay dây nhiều lần rollback về trạng thái trung gian

`.../batch_edit_graph/code.cs:128–131,238–243` — **RV2, R2, T4**.

Ban đầu A→T; batch lần lượt B→T rồi C→T. Danh sách snapshot chứa A→T rồi B→T. Rollback duyệt FIFO, reconnect A rồi B, kết thúc **B→T**, thay vì A→T. Installed DynamoCore đã được đối chiếu read-only: nối vào input đang có connector sẽ thay connector cũ.

Sửa: phục hồi theo thứ tự đảo hoặc giữ duy nhất snapshot đầu tiên của mỗi input. Test ít nhất hai replacement trên cùng input; kiểm tra cả endpoint GUID và port index, không chỉ số dây.

### R2-03 [P1] Sửa CodeBlock làm mất dây dù đã khôi phục code

`.../batch_edit_graph/code.cs:170–173,223,238–249` — **RV2, R2, T4, T6**.

CodeBlock `a=1; b=2;`, output 1 nối Watch. dryRun đổi code thành `a=1;`: port 1 và connector biến mất. Restore text không đủ phục hồi connector đó. Rollback hiện còn nối wires trước khi phục hồi port layout.

Bằng chứng API từ installed DynamoCore: `SetCodeContent` gọi `SaveAndDeleteConnectors → ProcessCode → LoadAndCreateConnectors`; chỉ connectors còn phù hợp với port mới được tái tạo. Snapshot hiện tại không giữ các connectors chịu tác động bởi property update.

Sửa: snapshot đầy đủ node state/port-related connectors trước mutation; phục hồi code/ports rồi connectors. Nếu dùng UndoRecorder, phải chứng minh phạm vi undo đúng, không lấy undo của thao tác người dùng. Test live trên fixture riêng là bắt buộc để xác nhận behavior của Dynamo; **CHƯA TEST live trong review này**.

### R2-04 [P1] Coordinator có thể báo rollback thành công sau khi đã commit

`HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoBridgeCoordinator.cs:107–124` — **R2, T4, P8**.

Sau khi execution boundary trả kết quả, coordinator kiểm tra token lần nữa. Nếu token đã cancel, nó bỏ kết quả và tạo result mới có `RolledBack=true`.

Trigger: transaction đã commit, result đã tạo, cancel tới trước continuation dòng 108. Caller nhận “cancelled/rolled back” trong khi thay đổi đã commit. Tương tự, kết quả rollback thất bại có thể bị ghi đè thành rollback thành công.

Sửa: transaction boundary là nguồn sự thật về commit/rollback; giữ `RolledBack`, `TimedOut`, `Changed`, logs và error của result đó. Cancel sau commit không được chuyển thành khẳng định rollback. Chỉ work chưa bắt đầu mới có thể báo chưa mutation dựa trên trạng thái queue.

Test mới `Milestone3CorrelatedCancelStressTests.cs:32–53` dùng hook không mở/rollback transaction nhưng assert `RolledBack=true`; phải thay bằng regression kiểm tra outcome thật, gồm late cancel sau commit và rollback failure.

### R2-05 [P1] Batch và headless chưa bảo đảm evaluation hoàn tất trước response

`.../batch_edit_graph/code.cs:262–265`; `HPDynamo/HPDynamo.McpBridge/Execution/HeadlessGraphRunner.cs:105–117`; seed `Graph/run_dyn_file/code.cs:91–108` — **R2, T4, T6**.

- Batch `runEvaluation=true` vẫn gọi `homeWs.Run()` rồi đi thẳng tới response `success=true`.
- Headless DesignScript gọi `ws.Run()`, đọc output và xóa CodeBlock ngay sau đó; không kiểm tra completion/error.
- Headless nhánh seed `run_dyn_file` không truyền `inputs`, không trích output và bỏ qua kết quả `ExecuteDynamoCommand`; workspace null vẫn cho 0 errors, từ đó `success=true`.
- Canvas `run_dyn_file` đã chờ nhưng khi node có Error chỉ trả `success=false` trong value; execution boundary vẫn có thể commit vì không nhận exception/failure outcome. Cần xác định và áp dụng failure policy trước finalize transaction.

Sửa: thống nhất orchestration và failure policy cho canvas/headless/batch; await đúng evaluation, áp dụng overrides trước run, chỉ extract sau completion. Test cùng một graph/inputs qua cả hai mode, có fixture lỗi node và lỗi load, không dùng mock `RunAndWait => true` để chứng minh behavior.

### R2-06 [P1] Timeout kết thúc chờ nhưng chưa chứng minh Dynamo đã dừng

`HPDynamo/HPDynamo.McpBridge/Execution/DynamoEvaluationSync.cs:28–57` — **R1, R2, R9, T6**.

Cancellation/timeout chỉ đặt `frame.Continue=false`, trả false và bỏ event subscription. Không có lệnh hủy hoặc xác nhận scheduler đã ngừng. Caller sau đó rollback/finalize và coordinator có thể bỏ busy trong khi evaluation đã schedule còn tồn tại. Subscription cũng nhận mọi `EvaluationCompleted` của model, chưa định danh run/workspace của request.

Sửa: xác định semantics hủy của Dynamo đang cài; duy trì ownership/busy cho tới completion hoặc cancellation acknowledgement thực tế. Không coi kết thúc DispatcherFrame là kết thúc graph. RunAction phải kiểm tra cancellation trước khi bắt đầu; hiện nó vẫn được gọi với token đã canceled. Kiểm chứng reentrancy khi message pump hoạt động.

Acceptance: graph chạy lâu + timeout/cancel → không còn writes/evaluation thuộc request sau terminal response; request mới bị chặn tới khi evaluation cũ thực sự kết thúc. Graph Automatic đã có evaluation trước request không được dùng event đó để hoàn tất request mới. **Các tình huống live này CHƯA TEST**.

### R2-07 [P1] Chọn transaction ownership bằng substring làm hỏng contract auto

`HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoExecutionEngine.cs:133–157` — **R2, K1, T4**.

Ví dụ C# hợp lệ `string message = "TransactionManager"; doc.Delete(id);` với transaction auto: chỉ string log đã khiến bridge không mở child transaction, nên direct Revit write thất bại. Ngược lại `new Transaction (doc, "T")` có dấu cách hoặc dùng alias wrapper không được nhận diện giống các mẫu hiện tại.

Tests `Milestone1ChallengerTests.cs:35–70` đang assert keyword trong comment/string phải tắt auto transaction; đó là kiểm tra implementation hiện tại, chưa bảo vệ contract người dùng.

Sửa: ownership rõ theo execution kind/policy của tool; nếu phải phân tích C#, dùng syntax/semantic và báo mixed ownership không hợp lệ trước mutation. Không để tên biến/comment/string literal thay đổi quyền transaction. Giữ behavior đã công bố của auto/manual/none và kiểm tra transaction leak tại thời điểm finalize.

### R2-08 [P1] Benchmark gắn nhãn thành công cho error, chưa có model isolation

`HPDynamo/tools/harness/run-benchmark.ps1:63–65,131–142,163–182,231–265,281` — **P8, T4, T6**.

- Harness chỉ kiểm tra Python exit code và Traceback. JSON-RPC error và MCP `isError=true` vẫn có thể exit 0; trường `Success` còn không được dùng để quyết định workload status.
- `Verified Live`, “All 5 workloads passed”, integrity 100% và reduction 90% đều được ghi cố định.
- WL-04 gửi `runEvaluation=false` và `dryRun=true`, nhưng report cố định `evaluationsTriggered=1`. Không có bộ đếm evaluation thật.
- “IsolatedModel.rvt” chỉ là text placeholder, không phải bản sao Revit, không được mở hoặc truyền vào tool; mọi call vẫn dùng pipe mặc định. Dò process và in “shield” không tạo isolation. dryRun canvas đang có các lỗi R2-01…03 nên không đủ bảo vệ graph người dùng.
- Mỗi warm run khởi động Python và server mới qua `mcp-call.py`: không đo warm cache/persistent MCP session. JSON hiện có chỉ 2 warm samples, không lưu raw responses/samples; p50 khoảng 0,79–0,91 giây không thể so sánh trực tiếp với baseline giả định vài chục ms.

Đây là lỗi harness đã xác minh bằng source và read-only MCP; không cần chạy batch để tái hiện. Registry lịch sử có compile failures và hiện quarantined càng làm nhãn live success thiếu căn cứ.

Sửa: preflight tools/list, bridge/version/hash và fixture identity; parse đầy đủ JSON-RPC/MCP/domain outcome; thất bại thì record FAIL hoặc SKIP kèm error và dừng vòng lỗi lặp. Lưu raw samples/responses; benchmark startup tách khỏi steady-state session, dùng `mcp-session.py` hoặc tương đương. Không ghi baseline/percentages/integrity nếu chưa đo/đối chiếu. Chỉ chạy workload mutation trong fixture Revit + Dynamo riêng đã xác minh.

### R2-09 [P2] Cancellation chưa cô lập qua reconnect và có cancel-of-cancel

`McpShared/HPRebar.Mcp.Server.Core/Services/RevitBridgeClient.cs:28,54,75–100`; `McpShared/HPRebar.McpBridge.Core/Pipe/PipeListener.cs:133–155` — **P9, T4**.

Same-session khác request ID đã sửa. Tuy nhiên request ID khởi tạo lại theo client, còn pipe listener dùng lifetime token chung. Source-derived trigger: client cũ execution ID1 còn chạy; client mới context ID1 timeout; cancel1 có thể trùng active ID của session cũ. **CHƯA live reproduce**.

Timeout của cancel method vẫn phát thêm cancel với timeout 5s: nếu bridge không phản hồi control call sẽ có chuỗi cancel-of-cancel.

Sửa: scoped identity `(session, request)` hoặc kết thúc ownership session cũ trước nhận session mới; giữ wire contract additive. Không auto-cancel cancel/context/control methods và giới hạn attempts. Test cả protocol legacy non-correlated hosts và reconnect.

### R2-10 [P2] Item budget chưa giới hạn payload byte và có thể đổi dữ liệu thật

`HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Data/get_node_output_values/code.cs:37,63,95,103` — **T4, K1**.

Một string lớn vẫn được trả nguyên vẹn; depth limit dùng `ToString()` có thể tạo output lớn ngoài budget. Marker `_truncated` chèn trong dictionary có thể overwrite key thật của người dùng; marker list không phân biệt được với string dữ liệu cùng nội dung.

Sửa: thêm string/UTF-8 byte budget tổng response và truncation metadata riêng, có đường lấy phần còn lại khi cần. Dùng cùng serializer cho các đường output nếu contract cần bảo toàn cấu trúc: canvas/headless runners và run_dyn_file hiện vẫn `Data?.ToString()`.

Catch rộng trong serializer nên rethrow cancellation rõ ràng. Không báo finding “MCP trả success khi ct canceled” chỉ vì seed return success: outer `RevitTransactionScope` đã kiểm tra linked token sau action và chuyển thành cancellation.

## 4. Khoảng trống trong tests

1. `BatchEditGraphRollbackTests` chủ yếu check chuỗi source; oracle collection riêng chưa chạy production seed trên workspace thật, chưa cover các trigger R2-01…03.
2. `GetNodeOutputValuesEmpiricalStressTests` dùng SimulatedMirrorData/TraversalHarness riêng; có giá trị kiểm tra ý tưởng nhưng chưa chứng minh serializer production.
3. `SeedCompilationTests.cs:124` mock RunAndWait trả true; host references ở `:163–190` khác danh sách compiler production. `DynamoCompilerReferences` đã thêm ProtoCore/DesignScriptBuiltin nhưng compile parity vẫn cần kiểm chứng bằng chính compiler/options/globals được bridge sử dụng.
4. Test tracker CAS là hữu ích; cần thêm test full coordinator với work Running, cancel, rollback chưa xong: busy vẫn true, Completion/RunCompleted chưa phát và request mới bị từ chối. Hai Assert.ThrowsAsync trong `Milestone2Tests:128–129` chưa await.
5. Không dùng tên “empirical”, số lượng tests hoặc decompile API như bằng chứng live model behavior. Decompile ở review này chỉ xác định API semantics, không thay cho chạy trong Revit.

**Không đề nghị thêm tests chỉ mirror code.** Cần ít test hành vi nhưng chạy đúng implementation và đúng boundary.

## 5. Giao việc tiếp cho Antigravity

Làm trong phạm vi HPDynamo; sửa McpShared chỉ khi ownership/protocol chung cần thiết. Không trộn refactor toàn repo; không khôi phục quarantined bằng force trước khi xác minh nguyên nhân và behavior.

### Đợt A — sửa tính đúng đắn

- R2-01…03: batch snapshot/rollback, test trước/sau đầy đủ nodes/values/ports/connectors/run settings; trường hợp dryRun, exception giữa batch, nhiều updates/replacements.
- R2-04: outcome transaction đúng khi late cancel và rollback failure.
- R2-05…07: evaluation boundary mọi mode, cancellation acknowledgement, ownership transaction rõ.
- Mỗi lỗi có regression behavior trước/sau. Tuân thủ checklist, cite rule ids; báo Built/Tested/Verified đúng thực tế.

### Đợt B — khôi phục vận hành thật

- Ghi build/hash Revit/Dynamo/bridge/server và embedded seed checksum; lấy compile diagnostics thực tế của ba tool đang quarantined.
- Compile tất cả seeds bằng production ScriptCompiler + production references/imports/globals; kiểm tra production output serializer cùng cách.
- Sau khi lỗi đã được sửa và validation đúng, phục hồi trạng thái tool theo workflow registry; xác minh surface kỳ vọng 25 tools trên library không có tool bổ sung của người dùng. Không xóa tools/history của người dùng để đạt con số.
- Deploy bridge theo workflow hiện có rồi thực hiện smoke trên bản sao model và fixture graph riêng. Không dùng session người dùng làm fixture.

### Đợt C — đo hiệu năng có thể kiểm tra lại

- Sửa R2-08 trước khi chạy workload mutation.
- Đo baseline và upgraded cùng fixture/runtime/policy. Nếu không có baseline executable/source tương đương, báo upgraded-only; không suy ra speedup.
- Tách process startup, cold compile, warm execution; dùng một session MCP duy trì kết nối cho warm runs. Khuyến nghị ít nhất 30 warm samples, lưu toàn bộ raw sample và success/error.
- WL-04 so sánh cùng nodes/wires/properties, cùng trạng thái graph và cùng evaluation policy; đo evaluation count thật.
- WL-05 assert JSON array/dictionary/value của fixture thật, gồm nested data, geometry và truncation metadata.
- Ghi FAIL/SKIP rõ khi bridge offline, tool missing/quarantined hoặc fixture không đúng; chưa có live thì ghi **CHƯA TEST**.

Acceptance để nghiệm thu: không còn P1 của review này; compile/test đúng production path; dryRun/error/cancel bảo toàn fixture và báo transaction outcome thật; live smoke có logs và fixture identity; benchmark không ghi success cho error và có raw evidence. Sau đó mới quyết định tối ưu thêm dựa trên stage nào chiếm thời gian thực tế.

## 6. Giới hạn của kết luận

Findings rollback/evaluation/transaction là từ source + API semantics + test review; chưa chạy mutation live trong review này. Compile failures là record lịch sử, không khẳng định mọi binary mới vẫn fail cùng nguyên nhân. Bridge offline và 22 tools là trạng thái tại thời điểm kiểm tra này. Shared suites green không thay thế test live của những host dùng dispatcher, đặc biệt các host chưa cài.

Báo cáo trước và benchmark gốc được giữ nguyên để đối chiếu; chỉ tạo báo cáo vòng hai này.
