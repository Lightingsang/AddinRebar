# Orchestration Plan — HPGrasshopper MCP Re-audit Remediation (R1–R9)

## Architecture & Scope
- **Target Deliverable**: `HPGrasshopper/` (`HPGrasshopper.McpBridge`, `HPGrasshopper.Mcp.Server`, `HPGrasshopper.E2E.Tests`, `HPGrasshopper.McpBridge.Tests`, `HPGrasshopper.Mcp.Server.Tests`)
- **Shared Reference**: `McpShared/` (Strict READ-ONLY: 0 modifications permitted)
- **Baseline Report**: `plans/reports/261012-2212-hpgrasshopper-reaudit.md`
- **Original User Request**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`

## Milestones

### Milestone M1: Unified Execution Lifecycle, Error Mapping & Document Identity (Findings R1, R2, R5)
- **Scope**:
  1. Hợp nhất vòng đời admission/execution cho cả public native RPC (`run_grasshopper_player`, `batch_edit_grasshopper_canvas`, `read_grasshopper_canvas`, `export_grasshopper_canvas_image`) và script execution qua `Coordinator`/`Dispatcher`.
  2. Gắn đầy đủ request/session ID, linked `CancellationToken`, deadline và giải phóng cờ bận (`IsBusy`) khi client timeout hoặc host không tick.
  3. Chuẩn hóa ánh xạ mã lỗi: khi native tool thất bại (missing file, failed preflight, solve error, missing output), MCP tool result trả về `isError: true` với error code và diagnostics rõ ràng.
  4. Thực thi xác thực danh tính tài liệu (`TargetDocTitle`, `TargetDocPath`, `TargetDefId`) trên mọi public route trước khi thực hiện ghi dữ liệu.
- **Affected Components**: `GrasshopperRpcDispatcher.cs`, `GrasshopperBridgeCoordinator.cs`, `GrasshopperExecutionEngine.cs`, `McpBridgeWorkRequest.cs`, `GrasshopperToolResult.cs`.
- **Status**: DONE (Gate passed, 824+ tests passed)

### Milestone M2: Synchronous Commit Ordering & Canvas Rollback Ledger (Findings R3, R4)
- **Scope**:
  1. Điều chỉnh thứ tự hoàn tất giao dịch: canvas undo scope chỉ được commit sau khi Revit transaction đạt kết quả cuối cùng (`final outcome`). Nếu Revit commit thất bại hoặc deadline tới, toàn bộ thay đổi trên Grasshopper canvas phải được rollback nguyên vẹn.
  2. Hoàn thiện ledger trong `CanvasUndoScope.cs`: phân biệt giữa đối tượng gốc bị xóa và đối tượng mới thêm trong scope; ngăn chặn việc tái tạo nhầm đối tượng tạm thời trên rollback (add -> remove in same scope).
  3. Tạo snapshot `GH_WireAction` trước khi thực hiện mutation trên wire trong `CanvasEditor.cs`.
  4. Báo cáo trung thực trạng thái khôi phục (`rolledBack`), không nuốt lỗi rollback hay báo clean rollback khi việc phục hồi chỉ diễn ra một phần.
- **Affected Components**: `GrasshopperExecutionEngine.cs`, `RevitTransactionScope.cs`, `CanvasUndoScope.cs`, `CanvasEditor.cs`, `ICanvasUndoScope.cs`, `CanvasRollbackResult.cs`, `RemovedComponentSnapshot.cs`.
- **Status**: DONE (Gate passed, 860 tests passed, C2 max 288 lines)

### Milestone M3: Comprehensive Preflight, Data Tree & Domain Seeds (Findings R6, R7, R8, R9)
- **Scope**:
  1. Nâng cấp `CanvasBatchValidator.cs`: kiểm tra preflight đối chiếu với số lượng port thực tế của component trên đồ thị, phát hiện và từ chối các wire tham chiếu port vượt giới hạn hoặc component bị xóa trong cùng batch; từ chối nguyên tử (atomic fail).
  2. Hoàn thiện `PlayerDataTreeHelper.cs` và `PlayerRunner.cs`: phân định rõ giữa mảng số và tọa độ 3D theo target parameter type; báo cáo danh sách inputs bị từ chối/không khớp (`rejectedInputs`); xuất đúng kiểu GUID/element references; bắt lỗi runtime solver thực chất.
  3. Chuẩn hóa seed tools: áp dụng chính sách sai số (tolerance) khi tìm đường kính thép trong `generate_rebar_curves`; xử lý vector pháp tuyến khi có các đoạn collinear trong mặt phẳng nghiêng/đứng; đồng bộ mô tả trong `tool.json` đúng với năng lực thực tế của `directshape_from_geometry` và `query_revit_geometry`.
  4. Khắc phục `GrasshopperContextExtractor.cs` và compiler invalidation: phân biệt rõ giữa context thực tế và dữ liệu mặc định khi UI bận; kết nối cơ chế xóa cache compiler vào production lifecycle khi references thay đổi.
- **Affected Components**: `CanvasBatchValidator.cs`, `CanvasEditor.cs`, `PlayerDataTreeHelper.cs`, `PlayerRunner.cs`, `Registry/SeedLibrary/Revit/generate_rebar_curves/code.cs`, `Registry/SeedLibrary/Revit/directshape_from_geometry/tool.json`, `Registry/SeedLibrary/Revit/query_revit_geometry/tool.json`, `GrasshopperContextExtractor.cs`.
- **Status**: DONE (Gate passed, 901/901 tests passed, C2 max 284 lines)

### Milestone M4: Public-Route Regression Testing & Skill Documentation
- **Scope**:
  1. Xây dựng bộ test hồi quy gọi trực tiếp qua public route (xuyên qua server JSON-RPC và dispatcher) với các kịch bản: missing file, preflight fail, solve error, cancellation giữa chừng.
  2. Bổ sung các test xác thực trạng thái thực tế của Canvas và Revit elements sau rollback/dryRun.
  3. Đồng bộ hóa file hướng dẫn kỹ năng `.agents/skills/hp-mcp-grasshopper/SKILL.md` khớp chính xác với 27 công cụ thực tế, schema tham số và cam kết an toàn.
- **Affected Components**: `HPGrasshopper.McpBridge.Tests/`, `HPGrasshopper.Mcp.Server.Tests/`, `HPGrasshopper.E2E.Tests/`, `.agents/skills/hp-mcp-grasshopper/SKILL.md`.
- **Status**: DONE (Gate passed, 928 solution tests passed, 1,975 total tests green, C2/M3 satisfied, SKILL.md & reflection 100% synchronized)

### Milestone M5: Final Build, Quality Walkers, McpShared Invariance & Forensic Verification
- **Scope**:
  1. Full test pass across `HPGrasshopper.slnx` filter-free.
  2. 1,047+ tests regression McpShared pass 100%.
  3. Zero files modified in `McpShared/`.
  4. 100% compliance with Clean Code Governance: C2 (<= 300 lines), M3 (<= 50 lines/method), Q-B1 (0 dead code), Q-B2 (0 unannotated catch), ADR-0007 seed quality compliance.
  5. Forensic Auditor verification and comprehensive final handoff report written to `handoff.md`.
- **Status**: DONE (Gate passed, 928/928 solution tests passed, 1,162 engine tests passed, 2,090 total green, C2/M3 clean, McpShared untouched, CLEAN audit)

