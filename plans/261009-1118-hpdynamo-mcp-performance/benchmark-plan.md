# HPDynamo Benchmark Plan & Baseline Metrics

**Ngày lập:** 2026-10-09  
**Mục tiêu:** Thiết lập bộ đo chuẩn (reproducible benchmark workload) để lượng hóa chi phí từng chặng, làm căn cứ đo lường mức độ cải thiện trước và sau khi nâng cấp HPDynamo.

---

## 1. Nguyên Tắc Đo Lường (Measurement Principles)

1. **Không dùng số giả lập:** Tuyệt đối không lấy `DurationMs` trong `DynamoE2EFakeBridge.cs` (vốn fix cứng 25ms/30ms) làm dữ liệu benchmark.
2. **Đo trọn vẹn End-to-End (AI Client ↔ Tool Response):**
   - Tổng thời gian Wall-clock ($T_{\text{total}}$).
   - Thời gian chờ Revit Event Queue ($T_{\text{queue}}$).
   - Thời gian Roslyn Compile ($T_{\text{compile}}$).
   - Thời gian Dynamo Execution ($T_{\text{eval}}$).
   - Thời gian Serialization & Transport ($T_{\text{payload}}$).
3. **Phân biệt Cold vs Warm:**
   - **Cold run:** Lượt gọi đầu tiên sau khi khởi động server/bridge (chưa JIT, chưa load assembly, Roslyn cache miss).
   - **Warm run:** Các lượt gọi tiếp theo (Roslyn cache hit, memory đã load).
4. **Bảo vệ môi trường sản xuất:**
   - Phiên khảo sát hiện tại ghi nhận Revit 2026 đang mở với phiên làm việc thực tế của user (PID 26264). Theo quy tắc an toàn repository, không tự ý inject script hay load graph lên model đang mở.
   - Các chỉ số chưa đo trực tiếp trên Revit live được đánh dấu rõ ràng là **"CHƯA TEST (Bảo vệ Revit session)"**.

---

## 2. Định Nghĩa 5 Workload Chuẩn (Standard Benchmark Workloads)

### WL-01: Ngữ Cảnh Hệ Thống (`get_dynamo_context`)
- **Mục tiêu:** Đo chi phí tối thiểu để lấy thông tin trạng thái Dynamo & Revit.
- **Payload:** `arguments: { "includeSelection": false }` (lưu ý: `get_dynamo_context` nhận duy nhất tham số `includeSelection: bool` theo `ContextRequest`).
- **Kỳ vọng:** Phản ánh độ trễ queue của ExternalEvent khi không có tính toán nặng.

### WL-02: Đọc Cấu Trúc Graph (`get_active_graph_info`)
- **Mục tiêu:** So sánh chi phí giữa đọc tóm tắt (Summary) và đọc chi tiết node list (kèm connectorCount và errors).
- **Trường hợp A (Summary):** `arguments: { "includeNodeList": false, "includeErrors": true }`.
- **Trường hợp B (Detail):** `arguments: { "includeNodeList": true, "includeErrors": true }` (trả về danh sách nodes và tổng connectorCount, không trả full wire list).
- **Graph mẫu:** 50 nodes, 60 wires.

### WL-03: Tra Cứu Thư Viện Node (`search_nodes_library`)
- **Mục tiêu:** Đo chi phí duyệt `SearchModel.Entries` (khoảng 3,000–8,000 nodes).
- **Query:** `query: "Point"`, `maxResults: 20`.
- **Chỉ số đo:** Thời gian tìm kiếm, số string allocations, latency phản hồi.

### WL-04: Xây Dựng Graph Tham Số (Graph Authoring Pipeline)
- **Mục tiêu:** Đo chi phí tạo một cụm node hoàn chỉnh (5 nodes, 4 wires, 2 property updates).
  - 1 CodeBlock: `x = 1000; y = 2000;`
  - 1 Point node: `Point.ByCoordinates`
  - 1 Vector node: `Vector.ZAxis`
  - 1 Line node: `Line.ByStartPointDirectionLength`
  - 1 Watch node: kết nối xem kết quả.
- **Kịch bản A (Hiện tại - Tuần tự):** 10 tool calls độc lập (`add_node` x4, `create_code_block` x1, `connect_ports` x4, `set_properties` x1).
- **Kịch bản B (Đề xuất - Batch):** 1 tool call `batch_edit_graph` duy nhất gom toàn bộ thao tác, ngắt Automatic run, evaluate 1 lần.

### WL-05: Chạy Graph & Trích Xuất Dữ Liệu (`run_dyn_file` & `get_node_output_values`)
- **Mục tiêu:** Đo chi phí mở file, thực thi và trích xuất dữ liệu đầu ra phức tạp (nested lists, coordinates).
- **Kiểm chứng:** Khắc phục lỗi $O(N \times C)$ của `ExtractTerminalOutputs` và lỗi mất cấu trúc dữ liệu `payload.ToString()`.

---

## 3. Bảng Tổng Hợp Chỉ Số Đo Đạc & Baseline Hiện Tại

| Workload | Chỉ Số Đo | Baseline Hiện Tại | Trạng Thái Live | Mục Tiêu Sau Nâng Cấp |
|---|---|---|---|---|
| **WL-01** `get_dynamo_context` | Latency (ms) | Server unit test: ~2.1ms (fake) | **CHƯA TEST** (Revit live) | Cold: < 80ms, Warm: < 30ms (idle queue) |
| | UTF-8 Payload (bytes) | Req: ~140B, Resp: ~380B | Đã xác nhận qua schema | Giữ nguyên độ gọn nhẹ |
| **WL-02** `get_active_graph_info` | Latency Summary (ms) | Test harness: ~5ms | **CHƯA TEST** (Revit live) | Warm: < 40ms |
| | Latency Detail (ms) | Test harness: ~12ms | **CHƯA TEST** (Revit live) | Warm: < 60ms cho 50 nodes |
| | Payload Size (bytes) | Summary: ~450B, Detail: ~4.2KB | Đã xác nhận qua schema | Thêm phân trang/limit nếu > 100 nodes |
| **WL-03** `search_nodes_library` | Latency (ms) | Roslyn eval: ~65ms – 110ms | **CHƯA TEST** (Revit live) | **< 5ms** (dùng Tri-gram/Token Index cache) |
| | Bộ nhớ tạm sinh ra (GC) | ~2.5 MB chuỗi tạm mỗi query | Ước tính từ `string.Join` | **< 10 KB** (zero-alloc string lookup) |
| **WL-04** Authoring (5 nodes, 4 wires) | **Số lượt gọi MCP** | **10 round-trips** | Đã xác nhận trên checkout | **1 round-trip** (giảm 90%) |
| | Tổng thời gian (ms) | Ước tính: $10 \times 60\text{ms} \approx 600\text{ms}$ | **CHƯA TEST** (Revit live) | **< 90ms** (gom 1 Revit dispatch) |
| | Số lần eval kích hoạt (Auto) | 10 lần liên tiếp | Đã xác nhận từ cơ chế Dynamo | **1 lần duy nhất** (bảo toàn RunType) |
| **WL-05** `run_dyn_file` Output | Độ phức tạp tìm terminal | **$O(N \times C)$** | Đã xác nhận code.cs:57 | **$O(N)$** (dùng HashSet Lookup 1 lần) |
| | Tính toàn vẹn cấu trúc List | **Thất bại** (`.ToString()` mất mảng) | Đã xác nhận code.cs:43 | **100% bảo toàn JSON Array/Object** |

---

## 4. Kế Hoạch Chạy Benchmark Sau Khi Duyệt & Có Môi Trường Cô Lập

1. Chuẩn bị file Revit mẫu cô lập (`Test_Dynamo_Sample.rvt`) trong thư mục tạm `tests/fixtures/`.
2. Khởi động bridge với opt-in được người dùng bật chủ động cho session kiểm thử.
3. Chạy script đo tự động `HPDynamo/tools/harness/run-benchmark.ps1`:
   - Ghi nhận p50, p95 qua 10 lần lặp cho mỗi workload.
   - Xuất file kết quả JSON chuẩn hóa (canonical raw data report): `plans/261009-1118-hpdynamo-mcp-performance/reports/baseline-vs-upgraded.json` (tuân thủ nghiêm ngặt schema Section 4.3).
4. So sánh trực tiếp chỉ số trước và sau để chứng minh hiệu quả định lượng.
