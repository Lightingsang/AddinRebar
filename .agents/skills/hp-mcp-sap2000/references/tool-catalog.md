# SAP2000 MCP Tool Catalog (24 Tools)

Danh sách 24 tools có sẵn trên MCP server `hprebar-sap2000`.

## Core Tools (4)

1. **`execute_sap2000_code`**: Chạy script C# Roslyn trực tiếp vào SAP2000 model.
2. **`get_sap2000_context`**: Lấy trạng thái bridge, kết nối, đơn vị, model name, counts.
3. **`inspect_type`**: Tra cứu reflection chữ ký các lớp OAPI (`SAP2000v1.*`).
4. **`cancel_execution`**: Hủy một script đang chạy thông qua cooperative cancellation token.

## Registry Meta Tools (8)

1. **`search_tools`**: Tìm kiếm tool trong registry theo từ khóa hoặc semantic text.
2. **`get_tool`**: Xem chi tiết định nghĩa tool, code, examples, metadata.
3. **`run_tool`**: Chạy tool theo tên từ library.
4. **`get_run`**: Lấy thông tin lần chạy trước đó theo `runId`.
5. **`propose_tool`**: Đề xuất tool mới vào registry dưới dạng draft.
6. **`test_tool`**: Chạy test kiểm thử tool với các examples đã khai báo.
7. **`publish_tool`**: Xuất bản tool từ draft lên trạng thái pending_approval.
8. **`manage_tool`**: Quản lý vòng đời tool (deprecate, quarantine, restore).

## Embedded Seed Tools (12)

1. **`get_model_info`** (Model, R): Lấy thông tin model, số lượng points/frames/areas, đơn vị, phiên bản SAP2000.
2. **`get_coordinate_systems_and_grids`** (Geometry, R): Lấy danh sách hệ tọa độ và đường lưới trục (grid lines).
3. **`get_structural_objects`** (Geometry, R): Lấy danh sách đối tượng kết cấu (points, frames, areas) kèm tọa độ, chiều dài, tiết diện.
4. **`draw_frame_by_coords`** (Geometry, W): Vẽ thanh frame mới theo 2 điểm tọa độ 3D (m).
5. **`assign_joint_restraint`** (Geometry, W): Gán liên kết gối (fixed, pinned, roller, free, custom) cho danh sách joint points.
6. **`get_materials_and_sections`** (Property, R): Lấy danh sách vật liệu (E, G theo MPa) và tiết diện frame (diện tích m², quán tính m⁴).
7. **`assign_frame_section`** (Property, W): Gán tiết diện cho danh sách frame objects.
8. **`get_load_definitions`** (Load, R): Lấy danh sách load patterns, load cases, load combinations.
9. **`assign_frame_load`** (Load, W): Gán tải trọng phân bố hoặc tập trung lên frame objects.
10. **`run_analysis`** (Analysis, D): Chạy phân tích kết cấu SAP2000 cho các load cases chỉ định.
11. **`get_joint_reactions`** (Results, R): Đọc phản lực gối (Fx, Fy, Fz theo kN; Mx, My, Mz theo kN·m).
12. **`get_frame_forces`** (Results, R): Đọc nội lực thanh (P, V2, V3 theo kN; T, M2, M3 theo kN·m) tại các vị trí station.
