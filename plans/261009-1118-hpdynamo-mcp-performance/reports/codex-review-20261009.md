# Đánh giá lại HPDynamo sau nâng cấp Antigravity

Ngày: 2026-10-09. Phạm vi: mã HPDynamo hiện tại, đường gọi McpShared liên quan và ba tài liệu trong kế hoạch. Đây là review; không sửa production, không deploy và không chạy graph trên model người dùng. HPDynamo chưa được Git theo dõi nên không có commit/diff tin cậy để quy toàn bộ vấn đề cho lần nâng cấp này.

## Kết luận

Đã có cải tiến thực tế: thêm `batch_edit_graph` (13 seeds, 25 tools), chuyển việc ghép category hierarchy sau bước lọc và dựng tập connector owners cho terminal lookup. Terminal lookup là O(N + C), không phải O(N) độc lập với số connectors. Chưa đạt nghiệm thu về dryRun, cancellation, evaluation completion và output của Dynamo. Một số finding trong audit được đánh dấu ĐÃ GIẢI QUYẾT nhưng đường thực thi vẫn có lỗi.

Chưa có benchmark live trước/sau hoặc dữ liệu đo raw trong thư mục kế hoạch. Không đủ bằng chứng kết luận độ trễ đã giảm hoặc pipe không phải điểm nghẽn.

## Kiểm chứng mới trong lần review này

| Lệnh (chạy từ thư mục chứa global.json tương ứng) | Passed | Failed | Skipped |
|---|---:|---:|---:|
| HPDynamo: `dotnet test HPDynamo.Mcp.Server.Tests` | 99 | 0 | 0 |
| HPDynamo: `dotnet test HPDynamo.McpBridge.Tests` | 46 | 0 | 0 |
| HPDynamo: `dotnet test HPDynamo.E2E.Tests` | 201 | 0 | 0 |
| McpShared: `dotnet test HPRebar.Mcp.Server.Core.Tests` | 899 | 0 | 0 |
| McpShared: `dotnet test HPRebar.McpBridge.Core.Net48Tests` | 115 | 0 | 0 |

Tổng 1.360 passed. Đây là bằng chứng suite hiện tại xanh, không phải xác minh các hành vi live. Test compile/schema/manifest và fake bridge chưa bảo vệ những trường hợp dưới đây. Live Revit/Dynamo: **CHƯA TEST** trong lần review này.

## Findings cần sửa trước nghiệm thu

### RV-01 — P1: Batch đọc sai nguồn dryRun và không phục hồi toàn bộ canvas

- Bằng chứng: `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/batch_edit_graph/code.cs:5` đọc `args.Bool("dryRun", false)`. `McpShared/HPRebar.Mcp.Server.Core/Registry/DynamicToolRegistrar.cs:53` tách key dryRun khỏi args rồi truyền riêng tới manager; `ToolManager.cs:174` đưa cờ vào ExecuteRequest. DynamoScriptGlobals không có cờ dryRun riêng.
- Trigger: gọi trực tiếp tool MCP `batch_edit_graph` với dryRun=true. Seed vẫn thấy false, không gọi PerformRollback; Revit group rollback nhưng canvas additions/edits vẫn còn. Với runEvaluation=true, seed còn có thể chạy evaluation dù caller yêu cầu dryRun.
- Ngay cả khi đưa dryRun vào args bằng đường khác, `code.cs:208` chỉ xóa nodes vừa tạo; thay đổi properties hoặc dây giữa nodes có sẵn không được phục hồi. `code.cs:216` chỉ có try/finally: lỗi ở operation thứ hai để lại operation đầu tiên trên canvas.
- Rules: RV2, R2, M8, T4, T6.
- Điều chỉnh: một nguồn dryRun tại execution boundary; preflight và cơ chế rollback toàn bộ canvas/undo cho dryRun, exception và cancellation. Phải bảo toàn dây cũ bị thay thế, thuộc tính, run mode và trạng thái workspace.
- Regression: direct named tool dryRun; run_tool dryRun; properties/wires trên nodes có sẵn; lỗi sau thao tác hợp lệ; cancel giữa batch. So sánh graph trước/sau, không chỉ đếm Revit elements.

### RV-02 — P1: RunAndWait thất bại nhưng caller vẫn trả thành công

- Bằng chứng: `Execution/DynamoEvaluationSync.cs:58,66` trả false khi timeout/exception. `Execution/CanvasGraphRunner.cs:35,78,128` bỏ qua giá trị trả về; DesignScript trả Success=true tại dòng 144.
- Trigger: DesignScript evaluation lỗi hoặc vượt timeout nội bộ 30 giây nhưng chưa vượt request timeout. Caller vẫn đọc output và transaction scope có thể assimilate group. `DynamoExecutionEngine.cs:61` truyền token của work item (`ct`), bỏ token timeout đã linked mà transaction scope đưa vào callback.
- Helper thoát message pump/unsubscribe khi timeout; chưa có yêu cầu dừng evaluation và xác nhận engine đã dừng trước cleanup. Event dùng toàn model, chưa đối chiếu evaluation/workspace của request.
- Rules: R2, T4, T6.
- Điều chỉnh: kết quả có trạng thái completion/failure/cancel/timeout rõ ràng; propagate lỗi tới transaction boundary; dùng đúng token/deadline. Chỉ finalise transaction và trả kết quả sau khi evaluation thuộc request kết thúc hoặc cancellation được xác nhận. Đánh giá reentrancy của Dispatcher.PushFrame trên model copy.
- Regression: failed evaluation, internal timeout, request timeout, cancellation, completion event không thuộc run hiện tại; không trả success/commit trong các trường hợp lỗi.

### RV-03 — P1: Tool run_dyn_file và batch vẫn chưa đi qua đường sync mới

- Bằng chứng: `Registry/SeedLibrary/Graph/run_dyn_file/code.cs:78` gọi activeCanvasWs.Run rồi đọc state/output ngay; nhánh headless tại dòng 96 đọc workspace sau journal command, không có completion tracking ở seed. `Execution/HeadlessGraphRunner.cs:110` vẫn Run rồi đọc/xóa CodeBlock. `batch_edit_graph/code.cs:224` cũng Run rồi trả kết quả.
- ToolManager gắn label bằng tên tool (`ToolManager.cs:174`); `DynamoExecutionEngine.cs:46` chỉ vào runner DynFile khi label bắt đầu bằng DynFile. Label run_dyn_file đi qua Roslyn seed, không qua CanvasGraphRunner.RunDynFile đã sửa.
- Rules: K1, K2, T4, T6.
- Điều chỉnh: một đường completion tracking thực sự được core tools và seeds sử dụng, tương thích script guard. Kiểm chứng headless journal semantics trên runtime cài đặt; không kết luận journal command luôn đồng bộ chỉ từ return code.
- Regression: gọi qua MCP registry thật; output mới khác output run trước; batch trả đúng trạng thái evaluation; cả canvas và headless.

### RV-04 — P1: Cancel chưa đối chiếu request ID

- Bằng chứng: `Services/RevitBridgeClient.cs:83,91,99` gửi cancel với id. `McpShared/HPRebar.McpBridge.Core/Pipe/RequestDispatcher.cs:111` gọi executor.Cancel mà không sử dụng id. `Coordinator/DynamoBridgeCoordinator.cs:193` hủy _currentCancel bất kể ID. AUD-DYN-06 ở audit.md:73 vẫn chưa được giải quyết.
- Trigger: A đã xong, B đang chạy, cancel của A đến muộn; hoặc context timeout trong khi execution chạy. Lệnh có thể hủy B.
- Rules: P9, T4.
- Điều chỉnh: cancellation có phạm vi request/session với compatibility rõ ràng; giữ explicit global cancel khi đó là ý định caller. Không replay writes sau reconnect.
- Regression: stale cancel A không hủy B; context timeout không hủy execution; cancel request không tự phát sinh vòng cancel.

### RV-05 — P1: Hủy task completion giải phóng busy trước khi work thực sự kết thúc

- Bằng chứng: `Coordinator/DynamoBridgeCoordinator.cs:270` đăng ký TrySetCanceled cho cả queued/running work; `:223` chạy work trên Revit thread; await kết thúc sớm rồi `:146,147,158` xóa/dispose current CTS và đặt busy=0.
- Trigger: cancel khi work chưa trả về hoặc chưa rollback. Request tiếp theo có thể được nhận trong khi giao dịch/evaluation trước còn chạy; RunCompleted cũng có thể được phát trước cleanup. DispatcherFrame làm vấn đề ownership/reentrancy cần được kiểm tra kỹ.
- Rules: R2, R9, T4.
- Điều chỉnh: state queued/running/completed; chỉ cancel completion ngay với queued work. Running work giữ ownership tới khi execution và rollback kết thúc. Dispose phải drain/fail pending context và execution; hiện dòng 235 chỉ cancel current execution và dispose raiser.
- Regression: giữ work đang chạy, cancel, kiểm tra busy vẫn true và chưa công bố completion cho tới khi cleanup xong; queued context không treo sau Dispose.

### RV-06 — P1: Bỏ auto transaction cho mọi C# làm thay đổi hợp đồng execute_dynamo_code

- Bằng chứng: `Coordinator/DynamoExecutionEngine.cs:122` luôn startAutoTransaction=false, kể cả Roslyn script ghi trực tiếp Revit Document. Tool description vẫn cam kết transaction=auto.
- Trigger: C# script đơn giản gọi doc.Create hoặc Set parameter, dựa vào auto transaction của bridge và không dùng Dynamo TransactionManager. TransactionGroup không thay thế child Transaction; thao tác có thể bị Revit từ chối vì không có transaction mở.
- Rules: RV2, R2, T4, T6.
- Điều chỉnh: phân biệt execution policy của direct C# Revit operations và Dynamo-managed evaluation; xác định rõ transaction ownership. Không giải quyết xung đột Dynamo bằng cách vô hiệu auto cho toàn bộ loại script.
- Regression: direct C# write auto; Dynamo element creation; manual transaction còn mở; none không ghi; dryRun rollback. Native host behavior cần live test trên copy.

### RV-07 — P1: Serializer mới chưa bảo toàn list/dictionary thực của DesignScript

- Bằng chứng: `Registry/SeedLibrary/Data/get_node_output_values/code.cs:70` loại mirror khi Data=null, trước khi xử lý collection. Đã decompile read-only ProtoCore.dll cài đặt: MirrorData.IsCollection = svData.IsArray; GetElements đọc DSArray.Values; GetData không có AddressType.ArrayPointer và rơi vào null. DesignScript.Builtin.Dictionary trong DesignScriptBuiltin.dll không implement IDictionary/IEnumerable; Keys và ValueAtKey là API truy cập cấu trúc.
- Trigger suy ra trực tiếp từ API/code: CodeBlock trả [1,2,3] hoặc nested list sẽ bị serialize thành null; dictionary rơi vào ToString tại dòng 59. Chưa thực thi graph live trong review này.
- Rules: T4, T6, K1.
- Điều chỉnh: serialize MirrorData.IsCollection/GetElements trước Data, hỗ trợ DesignScript dictionary. Không đánh đồng Dynamo collections với CLR IEnumerable/IDictionary.
- Regression: scalar, empty/nested list, null trong list, list geometry, dictionary nested; chạy production serializer trên output Dynamo thật.
- Tái lập kiểm chứng metadata: `ilspycmd -t ProtoCore.Mirror.MirrorData "C:\Program Files\Autodesk\Revit 2026\AddIns\DynamoForRevit\ProtoCore.dll"`; tương tự `-t DesignScript.Builtin.Dictionary` với DesignScriptBuiltin.dll trong cùng thư mục.

### RV-08 — P2: Payload traversal chưa có giới hạn số phần tử hoặc cancellation

- Bằng chứng: serializer `get_node_output_values/code.cs:49` materialize mọi phần tử của enumerable; depth cap không chặn breadth, limit tại dòng 84 chỉ giới hạn số nodes. Recursion không kiểm tra ct. Các runners và run_dyn_file vẫn dùng ToString cho output.
- Rules: M8, T4.
- Điều chỉnh: item/byte/depth budgets, cancellation trong traversal, preview có metadata rõ ràng và paging khi cần. Kiểm tra framing UTF-8 và slow client/progress trước khi tăng message limits.
- Regression: một port với output rất lớn, nested lists, cancellation khi serialize, tiếng Việt/emoji, không làm mất terminal response.

## Bằng chứng hiệu năng và tài liệu phải điều chỉnh

- `audit.md:40,44` đưa <1.5ms transport và 15–45ms queue như số đo nhưng benchmark-plan ghi live CHƯA TEST. Chuyển thành giả thuyết/ước tính nếu không có raw data.
- `HPDynamo/tools/harness/run-benchmark.ps1` và `reports/baseline-vs-upgraded.json` được nhắc trong benchmark-plan chưa tồn tại lúc review. Chưa có bằng chứng p50/p95 hoặc before/after.
- Phase 4 ghi Token Index hoàn thành nhưng implementation chỉ trì hoãn string.Join; không có index. Không cần thêm index nếu baseline không chứng minh có lợi.
- Phase 5 có test counts đúng nhưng chưa có benchmark; không gộp Tested với Verified live. Các tuyên bố 100% loại bỏ lỗi hoặc tăng tốc 10–50 lần chưa đủ căn cứ.
- Benchmark WL-01 dùng includeActiveGraph nhưng core tool hiện chỉ nhận includeSelection; WL-02 mô tả wire detail nhưng seed trả counts và node details, không connector list.
- Skill hp-mcp-dynamo vẫn nói 24 tools và chưa hướng dẫn batch; cập nhật sau khi sửa contract/behavior để Antigravity thực sự sử dụng batch đúng cách.
- SeedCompilationTests dùng wrapper/imports/references khác production DynamoCompilerReferences/ScriptCompiler. Compile test xanh chưa chứng minh seed compile trong bridge; cần kiểm tra bằng đúng compiler contract, đặc biệt ProtoGeometry, RevitNodes và DynamoApplications.
- Batch fallback `Nodes.LastOrDefault()` tại dòng 60 có thể chọn nhầm node cũ khi không tìm thấy GUID vừa tạo. Bỏ fallback, validate tempId trùng và coordinates của property updates trước mutation.

## Thứ tự xử lý đề xuất

1. DryRun/atomic canvas rollback, request-scoped cancel và ownership khi cancellation.
2. Một đường evaluation completion đúng cho core tools + registry seeds; transaction policy cho direct C# và Dynamo; output MirrorData đúng.
3. Regression qua named MCP tool và production compiler, rồi live trên model copy. Không chấp nhận chỉ fake bridge/compile test.
4. Tạo benchmark harness thật, đo cold/warm end-to-end, queue/compile/evaluation/serialization, round trips và UTF-8 bytes; lưu raw data.
5. Sau khi baseline có thật, chọn pagination/progress coalescing/cache có lợi ích; cập nhật audit, plan và skill. Chưa có lý do đo được để thay transport hoặc nâng framework tiếp.

Trạng thái: **Tested** đối với 5 suites trên; **chưa đạt spec acceptance**, **CHƯA TEST live**. Không sửa production trong review này.
