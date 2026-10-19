# DISPATCH — orchestrator_14

## 2026-10-12T15:24:00Z

You are `orchestrator_14`, the Project Orchestrator dispatched by the Sentinel (`64427e26-5509-42de-84fe-e05a5437451c`).
Your working directory is:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_14\`

### Authority & Source of Truth:
- User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (see section `## 2026-10-12T15:21:07Z`)
- Re-audit Plan: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\plans\reports\261012-2212-hpgrasshopper-reaudit.md`
- Clean Code Governance: `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/clean-code/host-appendix/revit.md`

### Core Mission:
Khắc phục triệt để các phát hiện kiểm toán lại (R1–R9) của hệ thống HPGrasshopper MCP theo kế hoạch tại `plans/reports/261012-2212-hpgrasshopper-reaudit.md`, thống nhất lifecycle thực thi giữa public native RPC và execute engine, bảo đảm an toàn giao dịch, rollback trung thực, preflight toàn diện và dữ liệu Data Tree chuẩn xác.

### Requirements & Scope Decomposition:

#### Milestone M1: Thống nhất Execution Lifecycle, Error Mapping & Document Identity (Findings R1, R2, R5)
- Hợp nhất vòng đời admission/execution cho cả public native RPC (`run_grasshopper_player`, `batch_edit_grasshopper_canvas`, `read_grasshopper_canvas`, `export_grasshopper_canvas_image`) và script execution qua một cơ chế chung trong `Coordinator`/`Dispatcher`.
- Gắn đầy đủ request/session ID, linked `CancellationToken`, deadline và xử lý hủy ngang không để treo cờ bận (`IsBusy`) khi client timeout hoặc host không tick.
- Chuẩn hóa ánh xạ mã lỗi: khi native tool thất bại (file không tồn tại, preflight fail, solve error, missing output), kết quả MCP phải trả về `isError: true` thay vì `CallToolResult.IsError = false`.
- Thực thi nghiêm ngặt xác thực danh tính tài liệu (`TargetDocTitle`, `TargetDocPath`, `TargetDefId`) trên mọi public route trước khi thực hiện thao tác ghi; từ chối khi tài liệu active bị thay đổi hoặc đóng giữa chừng.

#### Milestone M2: Thứ tự Commit Đồng Bộ & Hoàn Thiện Canvas Rollback Ledger (Findings R3, R4)
- Điều chỉnh thứ tự hoàn tất giao dịch: canvas undo scope chỉ được commit sau khi Revit transaction đạt kết quả cuối cùng (`final outcome`), đảm bảo nếu Revit commit thất bại hoặc deadline tới thì toàn bộ thay đổi trên Grasshopper canvas cũng được khôi phục nguyên vẹn.
- Hoàn thiện ledger trong `CanvasUndoScope.cs`: phân biệt giữa đối tượng gốc bị xóa và đối tượng mới thêm trong scope để khi rollback thao tác "thêm rồi xóa trong cùng một scope" không bị tái tạo nhầm đối tượng; tạo snapshot `GH_WireAction` trước khi thực hiện mutation trên wire.
- Báo cáo trung thực trạng thái khôi phục (`rolledBack`), không nuốt lỗi rollback hay báo clean rollback khi việc phục hồi chỉ diễn ra một phần.

#### Milestone M3: Preflight Toàn Diện, Data Tree & Chuẩn Hóa Domain Seeds (Findings R6, R7, R8, R9)
- Nâng cấp `CanvasBatchValidator.cs`: kiểm tra preflight đối chiếu với số lượng port thực tế của component trên đồ thị, phát hiện và từ chối các wire tham chiếu port vượt giới hạn hoặc component bị xóa trong cùng batch; từ chối thay vì âm thầm bỏ qua.
- Hoàn thiện `PlayerDataTreeHelper.cs` và `PlayerRunner.cs`: phân định rõ ràng giữa mảng số và tọa độ 3D theo kiểu tham số mục tiêu, báo cáo danh sách inputs bị từ chối/không khớp (`rejectedInputs`), xuất đúng kiểu GUID/element references, và bắt lỗi solver runtime thực chất.
- Chuẩn hóa seed tools: áp dụng chính sách sai số (tolerance) khi tìm đường kính thép trong `generate_rebar_curves`, xử lý vector pháp tuyến khi có các đoạn collinear trong mặt phẳng nghiêng/đứng; đồng bộ mô tả trong `tool.json` đúng với năng lực thực tế của `directshape_from_geometry` và `query_revit_geometry`.
- Khắc phục `GrasshopperContextExtractor.cs` và `InvalidateCompiler`: phân biệt rõ giữa trạng thái context thực tế và dữ liệu mặc định khi UI bận, kết nối cơ chế xóa cache compiler vào production lifecycle khi references thay đổi.

#### Milestone M4: Kiểm Thử Hồi Quy Public-Route & Cập Nhật Tài Liệu Kỹ Năng
- Xây dựng bộ test hồi quy gọi trực tiếp qua public route (xuyên qua server JSON-RPC và dispatcher) với các kịch bản file thiếu, preflight fail, solve error, và cancellation giữa chừng.
- Đồng bộ hóa file hướng dẫn kỹ năng `.agents/skills/hp-mcp-grasshopper/SKILL.md` khớp chính xác với 27 công cụ thực tế, schema tham số và các cam kết an toàn.

#### Milestone M5: Final Build, Quality Walkers & Forensic Verification
- Full test pass across `HPGrasshopper.slnx` filter-free.
- 1,162 tests regression McpShared pass 100%.
- Zero files modified in `McpShared/`.
- 100% compliance with C2 ($\le 300$ lines), M3 ($\le 50$ lines/method), Q-B1 (0 dead code), Q-B2 (0 unannotated catch).
- Final handoff report written to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_14\handoff.md`.

### Non-Negotiable Orchestrator Constraints:
- You are a PURE ORCHESTRATOR. NEVER write or edit source code files directly.
- NEVER run build/test commands directly — delegate to workers/reviewers.
- Maintain `progress.md` and `BRIEFING.md` in your directory.
- Communicate milestone progress back to Sentinel caller using `send_message`.
